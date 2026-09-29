using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexo.Api.Infrastructure.Email;
using Nexo.Api.Infrastructure.Persistence;
using Xunit;

namespace Nexo.Api.Tests.Infrastructure;

/// <summary>
/// Hosts the API against the Compose PostgreSQL (started with <c>compose.test.yaml</c>), using a
/// separate <c>nexo_test</c> database so development data is untouched. Override the connection
/// string with the <c>NEXO_TEST_DB</c> environment variable.
/// </summary>
public class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5433;Database=nexo_test;Username=nexo;Password=nexo-local-development";

    /// <summary>Every email the API "sent" since the last reset; tests never talk to a real SMTP server.</summary>
    public CapturingEmailSender Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(
            "ConnectionStrings:NexoDb",
            Environment.GetEnvironmentVariable("NEXO_TEST_DB") ?? DefaultConnectionString);
        builder.UseSetting("Jwt:Key", TestData.JwtKey);
        builder.UseSetting("RateLimit:SignInPermitLimit", "1000");
        builder.ConfigureTestServices(services => services.Replace(ServiceDescriptor.Singleton<IEmailSender>(Emails)));
    }

    /// <summary>Creates/migrates the test database and empties the business tables.</summary>
    public async Task InitializeAsync() => await ResetAsync();

    public new Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Empties the business tables so each test starts from a known state.</summary>
    public async Task ResetAsync()
    {
        Emails.Clear();
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NexoDbContext>();
        await db.Database.MigrateAsync();
        await db.ExternalLogins.ExecuteDeleteAsync();
        await db.Accounts.ExecuteDeleteAsync();
        await db.Organizations.ExecuteDeleteAsync();
    }
}
