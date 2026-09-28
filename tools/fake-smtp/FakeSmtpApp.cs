namespace Nexo.FakeSmtp;

/// <summary>Composes the fake mail server: an SMTP receiver plus the inbox API and page on one HTTP port.</summary>
public static class FakeSmtpApp
{
    /// <summary>Builds the server. Settings: <c>Smtp:Address</c>, <c>Smtp:Port</c>, <c>Store:Directory</c>, and <c>urls</c>.</summary>
    public static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory,
            WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot"),
        });
        if (builder.Configuration["urls"] is null && builder.Configuration["ASPNETCORE_URLS"] is null)
            builder.WebHost.UseUrls("http://localhost:8025");

        builder.Services.AddSingleton(new MailStore(builder.Configuration["Store:Directory"] ?? "data"));
        builder.Services.AddSingleton<SmtpReceiver>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<SmtpReceiver>());

        var app = builder.Build();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        MapInbox(app);
        return app;
    }

    private static void MapInbox(WebApplication app)
    {
        var api = app.MapGroup("/api");
        api.MapGet("/info", (SmtpReceiver smtp) => new { smtpPort = smtp.Port });
        api.MapGet("/messages", (MailStore store, string? q) => store.List(q));
        api.MapGet("/messages/{id:guid}", (Guid id, MailStore store) =>
            store.Get(id) is { } message ? Results.Ok(message) : Results.NotFound());
        api.MapGet("/messages/{id:guid}/raw", (Guid id, MailStore store) =>
            store.RawPath(id) is { } path ? Results.File(Path.GetFullPath(path), "text/plain; charset=utf-8") : Results.NotFound());
        api.MapGet("/messages/{id:guid}/attachments/{index:int}", (Guid id, int index, MailStore store) =>
            store.GetAttachment(id, index) is var (content, type, name) ? Results.File(content, type, name) : Results.NotFound());
        api.MapPost("/messages/{id:guid}/read", (Guid id, MailStore store, bool read = true) =>
            store.MarkRead(id, read) ? Results.NoContent() : Results.NotFound());
        api.MapDelete("/messages/{id:guid}", (Guid id, MailStore store) =>
            store.Delete(id) ? Results.NoContent() : Results.NotFound());
        api.MapDelete("/messages", (MailStore store) =>
        {
            store.Clear();
            return Results.NoContent();
        });
    }
}
