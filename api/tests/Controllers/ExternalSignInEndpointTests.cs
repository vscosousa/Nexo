using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Persistence;
using Nexo.Api.Tests.Infrastructure;
using Xunit;

namespace Nexo.Api.Tests.Controllers;

/// <summary>
/// US-005 acceptance tests for Google/Microsoft sign-in. The provider handshake itself cannot run offline, so
/// these tests start at the callback with the external cookie the provider handler would have issued.
/// </summary>
public class ExternalSignInEndpointTests(PostgresApiFactory factory)
    : IClassFixture<PostgresApiFactory>, IAsyncLifetime
{
    private const string WebBaseUrl = "http://localhost:5173";

    private readonly WebApplicationFactory<Program> _configured = factory.WithWebHostBuilder(b =>
    {
        b.UseSetting("Authentication:Google:ClientId", "test-client-id");
        b.UseSetting("Authentication:Google:ClientSecret", "test-client-secret");
    });

    public Task InitializeAsync() => factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GivenAVerifiedGoogleEmailOfAnActiveAccount_WhenTheCallbackRuns_ThenASessionIsIssuedAndTheProviderLinked()
    {
        var account = await AddAccountAsync("ana@example.com", AccountStatus.Active);

        var response = await CallbackAsync("google", "g-1", " Ana@Example.com ", verified: true);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.ToString();
        Assert.StartsWith($"{WebBaseUrl}/login/callback#token=", location);
        var token = new JsonWebToken(location[(location.IndexOf("token=") + 6)..]);
        Assert.Equal(account.Id.ToString(), token.Subject);
        await using var db = NewDbContext();
        var link = await db.ExternalLogins.SingleAsync();
        Assert.Equal((account.Id, "google", "g-1"), (link.AccountId, link.Provider, link.ProviderKey));
    }

    [Fact]
    public async Task GivenAnAlreadyLinkedLogin_WhenTheCallbackRunsAgain_ThenItSignsInWithoutLinkingTwice()
    {
        await AddAccountAsync("ana@example.com", AccountStatus.Active);
        await CallbackAsync("google", "g-1", "ana@example.com", verified: true);

        var response = await CallbackAsync("google", "g-1", "ana@example.com", verified: true);

        Assert.StartsWith($"{WebBaseUrl}/login/callback#token=", response.Headers.Location!.ToString());
        await using var db = NewDbContext();
        Assert.Equal(1, await db.ExternalLogins.CountAsync());
    }

    [Fact]
    public async Task GivenAnUnverifiedEmail_WhenTheCallbackRuns_ThenItRejectsAndLinksNothing()
    {
        await AddAccountAsync("ana@example.com", AccountStatus.Active);

        var response = await CallbackAsync("google", "g-1", "ana@example.com", verified: false);

        await AssertRejectedAsync(response);
    }

    [Fact]
    public async Task GivenNoMatchingAccount_WhenTheCallbackRuns_ThenItRejectsWithoutCreatingAnAccount()
    {
        await AddAccountAsync("ana@example.com", AccountStatus.Active);

        var response = await CallbackAsync("google", "g-9", "stranger@example.com", verified: true);

        await AssertRejectedAsync(response);
        await using var db = NewDbContext();
        Assert.Equal(1, await db.Accounts.CountAsync());
    }

    [Fact]
    public async Task GivenAnInvitedAccount_WhenTheCallbackRuns_ThenItRejectsAndLinksNothing()
    {
        await AddAccountAsync("bob@example.com", AccountStatus.Invited);

        var response = await CallbackAsync("google", "g-2", "bob@example.com", verified: true);

        await AssertRejectedAsync(response);
    }

    [Fact]
    public async Task GivenNoExternalCookie_WhenTheCallbackRuns_ThenItRejects()
    {
        var response = await _configured.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false })
            .GetAsync("/auth/external/callback");

        await AssertRejectedAsync(response);
    }

    [Fact]
    public async Task GivenAConfiguredProvider_WhenTheUserStartsSignIn_ThenItRedirectsToTheProvider()
    {
        var response = await _configured.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false })
            .GetAsync("/auth/external/google");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://accounts.google.com/", response.Headers.Location!.ToString());
        // Always show the account chooser, so a shared browser never signs in as the previous account.
        Assert.Contains("prompt=select_account", response.Headers.Location.Query);
    }

    [Theory]
    [InlineData("microsoft")] // recognized but not configured
    [InlineData("github")] // not a supported provider
    public async Task GivenAnUnavailableProvider_WhenTheUserStartsSignIn_ThenItIsNotFound(string provider)
    {
        var response = await _configured.CreateClient().GetAsync($"/auth/external/{provider}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task AssertRejectedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"{WebBaseUrl}/login?error=oauth", response.Headers.Location!.ToString());
        await using var db = NewDbContext();
        Assert.Empty(await db.ExternalLogins.ToListAsync());
    }

    /// <summary>Calls the callback carrying the cookie the provider handler would have set after a successful login.</summary>
    private async Task<HttpResponseMessage> CallbackAsync(string provider, string key, string email, bool verified)
    {
        var options = _configured.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get("External");
        // The provider handler stamps its scheme name (the provider) as the claims' issuer.
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, key, null, provider), new Claim(ClaimTypes.Email, email, null, provider),
                new Claim("email_verified", verified ? "true" : "false", null, provider)],
            "External");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), new AuthenticationProperties(), "External");
        var request = new HttpRequestMessage(HttpMethod.Get, "/auth/external/callback");
        request.Headers.Add("Cookie", $"{options.Cookie.Name}={options.TicketDataFormat.Protect(ticket)}");
        return await _configured.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false })
            .SendAsync(request);
    }

    private async Task<Account> AddAccountAsync(string email, AccountStatus status)
    {
        await using var db = NewDbContext();
        var plan = await db.Plans.SingleAsync();
        var organization = new Organization { Name = "Local Club", PlanId = plan.Id };
        var account = new Account { Email = email, Role = Role.Member, Status = status, OrganizationId = organization.Id };
        db.AddRange(organization, account);
        await db.SaveChangesAsync();
        return account;
    }

    private NexoDbContext NewDbContext() =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<NexoDbContext>();
}
