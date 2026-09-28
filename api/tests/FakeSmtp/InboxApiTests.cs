using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Nexo.Api.Tests.FakeSmtp;

public class InboxApiTests : IClassFixture<FakeSmtpHarness>
{
    private readonly FakeSmtpHarness _server;

    public InboxApiTests(FakeSmtpHarness server) => _server = server;

    [Fact]
    public async Task GivenAMessageWithHtmlTextAndAnAttachment_WhenRead_ThenTheDetailExposesAllParts()
    {
        var id = await SendAsync("Invoice", "Plain text body", "<p>HTML <b>body</b></p>", attachment: ("note.txt", "attached!"));

        var detail = await _server.Http.GetFromJsonAsync<JsonElement>($"/api/messages/{id}");

        Assert.Equal("Invoice", detail.GetProperty("subject").GetString());
        Assert.Contains("HTML", detail.GetProperty("html").GetString());
        Assert.Contains("Plain text body", detail.GetProperty("text").GetString());
        var attachment = Assert.Single(detail.GetProperty("attachments").EnumerateArray());
        Assert.Equal("note.txt", attachment.GetProperty("fileName").GetString());

        var download = await _server.Http.GetByteArrayAsync($"/api/messages/{id}/attachments/0");
        Assert.Equal("attached!", Encoding.UTF8.GetString(download));
        var raw = await _server.Http.GetStringAsync($"/api/messages/{id}/raw");
        Assert.Contains("Subject: Invoice", raw);
    }

    [Fact]
    public async Task GivenSeveralMessages_WhenListedWithASearch_ThenOnlyMatchesAreReturnedNewestFirst()
    {
        await SendAsync("Alpha report", "first");
        await SendAsync("Beta report", "second");
        await SendAsync("Gamma", "unrelated");

        var all = await _server.Http.GetFromJsonAsync<JsonElement[]>("/api/messages");
        var reports = await _server.Http.GetFromJsonAsync<JsonElement[]>("/api/messages?q=REPORT");

        Assert.Equal("Gamma", all![0].GetProperty("subject").GetString());
        Assert.Equal(
            ["Beta report", "Alpha report"], reports!.Select(m => m.GetProperty("subject").GetString()!).ToArray());
    }

    [Fact]
    public async Task GivenAnUnreadMessage_WhenMarkedReadAndUnread_ThenTheFlagFollows()
    {
        var id = await SendAsync("Flag me", "body");

        await _server.Http.PostAsync($"/api/messages/{id}/read", null);
        Assert.True(await IsReadAsync(id));
        await _server.Http.PostAsync($"/api/messages/{id}/read?read=false", null);
        Assert.False(await IsReadAsync(id));
    }

    [Fact]
    public async Task GivenMessages_WhenOneIsDeletedAndThenAll_ThenTheInboxEmpties()
    {
        var first = await SendAsync("Delete one", "a");
        await SendAsync("Delete all", "b");

        Assert.Equal(HttpStatusCode.NoContent, (await _server.Http.DeleteAsync($"/api/messages/{first}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _server.Http.GetAsync($"/api/messages/{first}")).StatusCode);

        await _server.Http.DeleteAsync("/api/messages");
        Assert.Empty((await _server.Http.GetFromJsonAsync<JsonElement[]>("/api/messages"))!);
    }

    [Fact]
    public async Task GivenAnUnknownMessage_WhenRequested_ThenItIs404()
    {
        var response = await _server.Http.GetAsync($"/api/messages/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GivenTheServerIsRunning_WhenTheRootIsRequested_ThenTheInboxPageIsServed()
    {
        var html = await _server.Http.GetStringAsync("/");

        Assert.Contains("Nexo Mail", html);
    }

    private async Task<Guid> SendAsync(
        string subject, string text, string? html = null, (string Name, string Content)? attachment = null)
    {
        using var client = new SmtpClient("127.0.0.1", _server.SmtpPort);
        using var message = new MailMessage("nexo@example.com", "bob@example.com", subject, text);
        if (html is not null)
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, null, "text/html"));
        if (attachment is { } a)
            message.Attachments.Add(new Attachment(new MemoryStream(Encoding.UTF8.GetBytes(a.Content)), a.Name, "text/plain"));
        await client.SendMailAsync(message);
        await Task.Delay(15); // keeps received-at timestamps strictly ordered
        return _server.Store.List(subject).First().Id;
    }

    private async Task<bool> IsReadAsync(Guid id) =>
        (await _server.Http.GetFromJsonAsync<JsonElement[]>("/api/messages"))!
            .Single(m => m.GetProperty("id").GetGuid() == id).GetProperty("read").GetBoolean();
}
