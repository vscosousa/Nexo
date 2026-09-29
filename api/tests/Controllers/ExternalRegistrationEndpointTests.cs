using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.Net.Http.Headers;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Persistence;
using Nexo.Api.Services;
using Nexo.Api.Tests.Infrastructure;
using Xunit;

namespace Nexo.Api.Tests.Controllers;

/// <summary>
/// US-001/US-003 acceptance tests for registering an organization, or activating an invited account, with Google.
/// The provider handshake cannot run offline, so these tests forge the external cookie the Google handler would have
/// set, carrying the intent the web app started the handshake with.
/// </summary>
public class ExternalRegistrationEndpointTests(PostgresApiFactory factory)
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
    public async Task GivenARegistrationIntent_WhenTheUserStartsGoogleSignIn_ThenItRedirectsToGoogle()
    {
        var response = await Client().GetAsync(
            $"/auth/external/google?intent=register&planId={NexoDbContext.TeamPlanId}&organizationName=Local%20Club");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://accounts.google.com/", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task GivenARegistrationIntentAndAVerifiedEmail_WhenTheCallbackRuns_ThenItReturnsToTheFormWithTheGoogleDetailsPending()
    {
        var response = await GetAsync("/auth/external/callback", RegisterCookie());

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(
            $"{WebBaseUrl}/register/organization?planId={NexoDbContext.TeamPlanId}&external=google",
            response.Headers.Location!.ToString());
        await AssertNothingCreatedAsync();
    }

    [Fact]
    public async Task GivenARegistrationIntentAndAnUnverifiedEmail_WhenTheCallbackRuns_ThenItReturnsToTheFormWithAnError()
    {
        var response = await GetAsync("/auth/external/callback", RegisterCookie(verified: false));

        Assert.Equal(
            $"{WebBaseUrl}/register/organization?planId={NexoDbContext.TeamPlanId}&error=oauth",
            response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task GivenAnActivationIntent_WhenTheCallbackRuns_ThenItReturnsToTheActivationPage()
    {
        var response = await GetAsync("/auth/external/callback", ActivateCookie());

        Assert.Equal(
            $"{WebBaseUrl}/activate?email=bob%40example.com&token={TestData.InvitationLinkToken}&external=google",
            response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task GivenPendingGoogleDetails_WhenTheFormAsksForThem_ThenItGetsTheNameEmailAndOrganization()
    {
        var response = await GetAsync("/auth/external/pending", RegisterCookie());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pending = await response.Content.ReadFromJsonAsync<PendingExternalDto>();
        Assert.Equal(
            ("register", "ana@example.com", "Ana", "Silva", "Local Club"),
            (pending!.Intent, pending.Email, pending.FirstName, pending.LastName, pending.OrganizationName));
    }

    [Fact]
    public async Task GivenNoPendingGoogleDetails_WhenTheFormAsksForThem_ThenItRejectsWithUnauthorized()
    {
        var response = await Client().GetAsync("/auth/external/pending");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GivenPendingGoogleDetails_WhenRegisteringWithEditedNames_ThenTheOrganizationAndAPasswordlessAdminAreCreatedAndSignedIn()
    {
        var response = await PostAsync("/auth/external/register", ValidRegistration(), RegisterCookie());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var organization = await response.Content.ReadFromJsonAsync<OrganizationDto>();
        Assert.Equal(("Local Club", "Team"), (organization!.Name, organization.Plan));
        await using var db = NewDbContext();
        var admin = await db.Accounts.SingleAsync();
        Assert.Equal(
            ("ana@example.com", "Anabela", "Silva", Role.Admin, AccountStatus.Active, (string?)null),
            (admin.Email, admin.FirstName, admin.LastName, admin.Role, admin.Status, admin.PasswordHash));
        var link = await db.ExternalLogins.SingleAsync();
        Assert.Equal((admin.Id, "google", "g-1"), (link.AccountId, link.Provider, link.ProviderKey));
        Assert.Equal(admin.Id.ToString(), new JsonWebToken(SessionCookie.In(response).Value.ToString()).Subject);
    }

    [Fact]
    public async Task GivenPendingGoogleDetails_WhenRegistering_ThenTheAdminIsEmailedAWelcomeWithoutAnyLinkToken()
    {
        await PostAsync("/auth/external/register", ValidRegistration(), RegisterCookie());

        var email = Assert.Single(factory.Emails.Sent);
        Assert.Equal("ana@example.com", email.To);
        Assert.Contains("Welcome", email.Subject);
        Assert.Contains("Local Club", email.Subject);
        Assert.Contains("/login", email.Text);
        Assert.DoesNotContain("token=", email.Text);
    }

    [Fact]
    public async Task GivenTheGoogleEmailAlreadyHasAnAccount_WhenRegistering_ThenItRejectsWithConflictAndCreatesNothingMore()
    {
        await AddInvitedAccountAsync("ana@example.com");

        var response = await PostAsync("/auth/external/register", ValidRegistration(), RegisterCookie());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await using var db = NewDbContext();
        Assert.Equal(1, await db.Organizations.CountAsync());
        Assert.Empty(await db.ExternalLogins.ToListAsync());
    }

    [Fact]
    public async Task GivenTheGoogleEmailHasAnUnconfirmedRegistration_WhenRegistering_ThenGoogleProvesOwnershipAndItIsReplaced()
    {
        await factory.CreateClient().PostAsJsonAsync("/organizations", new RegisterOrganizationDto
        {
            OrganizationName = "Squatted Club",
            AdminFirstName = "Eve",
            AdminLastName = "Squatter",
            AdminEmail = "ana@example.com",
            Password = TestData.StrongPassword,
            PlanId = NexoDbContext.FreePlanId,
        });

        var response = await PostAsync("/auth/external/register", ValidRegistration(), RegisterCookie());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var db = NewDbContext();
        Assert.Equal("Local Club", (await db.Organizations.SingleAsync()).Name);
        var admin = await db.Accounts.SingleAsync();
        Assert.Equal((AccountStatus.Active, (string?)null), (admin.Status, admin.PasswordHash));
    }

    [Fact]
    public async Task GivenAMissingName_WhenRegistering_ThenItRejectsWithValidationErrors()
    {
        var dto = ValidRegistration();
        dto.AdminFirstName = "";

        var response = await PostAsync("/auth/external/register", dto, RegisterCookie());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertNothingCreatedAsync();
    }

    [Fact]
    public async Task GivenNoAntiforgeryToken_WhenRegistering_ThenItIsRejected()
    {
        var response = await PostAsync("/auth/external/register", ValidRegistration(), RegisterCookie(), withToken: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertNothingCreatedAsync();
    }

    [Fact]
    public async Task GivenNoPendingGoogleDetails_WhenRegistering_ThenItRejectsWithUnauthorized()
    {
        var response = await PostAsync("/auth/external/register", ValidRegistration(), externalCookie: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertNothingCreatedAsync();
    }

    [Fact]
    public async Task GivenAnActivationIntent_WhenRegistering_ThenItRejectsWithUnauthorized()
    {
        var response = await PostAsync("/auth/external/register", ValidRegistration(), ActivateCookie());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GivenAMatchingInvitation_WhenActivatingWithGoogle_ThenTheAccountIsActiveLinkedAndSignedIn()
    {
        var invited = await AddInvitedAccountAsync("bob@example.com");

        var response = await PostAsync(
            "/auth/external/activate",
            new ActivateAccountExternalDto { FirstName = "Roberto", LastName = "Builder" },
            ActivateCookie());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var db = NewDbContext();
        var account = await db.Accounts.SingleAsync(a => a.Id == invited.Id);
        Assert.Equal(
            ("Roberto", "Builder", AccountStatus.Active, (string?)null, (string?)null),
            (account.FirstName, account.LastName, account.Status, account.PasswordHash, account.InvitationTokenHash));
        Assert.Equal(account.Id, (await db.ExternalLogins.SingleAsync()).AccountId);
        Assert.Equal(account.Id.ToString(), new JsonWebToken(SessionCookie.In(response).Value.ToString()).Subject);
    }

    [Fact]
    public async Task GivenAGoogleEmailOtherThanTheInvitedOne_WhenActivating_ThenItRejectsAndStaysInvited()
    {
        await AddInvitedAccountAsync("bob@example.com");

        var response = await PostAsync(
            "/auth/external/activate",
            new ActivateAccountExternalDto { FirstName = "Eva", LastName = "Other" },
            ActivateCookie(googleEmail: "eva@example.com"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertStillInvitedAsync();
    }

    [Fact]
    public async Task GivenAWrongInvitationCode_WhenActivatingWithGoogle_ThenItRejectsAndStaysInvited()
    {
        await AddInvitedAccountAsync("bob@example.com");

        var response = await PostAsync(
            "/auth/external/activate",
            new ActivateAccountExternalDto { FirstName = "Bob", LastName = "Builder" },
            ActivateCookie(code: "ZZZ999"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertStillInvitedAsync();
    }

    [Fact]
    public async Task GivenTheMemberLimitIsReached_WhenActivatingWithGoogle_ThenItRejectsWithConflict()
    {
        var invited = await AddInvitedAccountAsync("bob@example.com");
        await using (var db = NewDbContext())
        {
            var limit = (await db.Plans.SingleAsync(p => p.Name == Plan.Free)).MemberLimit;
            db.Accounts.AddRange(Enumerable.Range(0, limit).Select(i => new Account
            {
                Email = $"member{i}@example.com",
                Role = Role.Member,
                Status = AccountStatus.Active,
                OrganizationId = invited.OrganizationId,
            }));
            await db.SaveChangesAsync();
        }

        var response = await PostAsync(
            "/auth/external/activate",
            new ActivateAccountExternalDto { FirstName = "Bob", LastName = "Builder" },
            ActivateCookie());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertStillInvitedAsync();
    }

    private static RegisterOrganizationExternalDto ValidRegistration() => new()
    {
        OrganizationName = "Local Club",
        AdminFirstName = "Anabela",
        AdminLastName = "Silva",
        PlanId = NexoDbContext.TeamPlanId,
    };

    private string RegisterCookie(bool verified = true) => ExternalCookie(
        "ana@example.com",
        verified,
        new()
        {
            ["intent"] = "register",
            ["planId"] = NexoDbContext.TeamPlanId.ToString(),
            ["organizationName"] = "Local Club",
        });

    private string ActivateCookie(string googleEmail = "bob@example.com", string code = TestData.InvitationCode) => ExternalCookie(
        googleEmail,
        verified: true,
        new()
        {
            ["intent"] = "activate",
            ["email"] = "bob@example.com",
            ["token"] = TestData.InvitationLinkToken,
            ["code"] = code,
        });

    /// <summary>The cookie the Google handler would have set after a successful login started with these items.</summary>
    private string ExternalCookie(string email, bool verified, Dictionary<string, string?> items)
    {
        var options = _configured.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get("External");
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "g-1", null, "google"),
                new Claim(ClaimTypes.Email, email, null, "google"),
                new Claim("email_verified", verified ? "true" : "false", null, "google"),
                new Claim(ClaimTypes.GivenName, "Ana", null, "google"),
                new Claim(ClaimTypes.Surname, "Silva", null, "google"),
            ],
            "External");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), new AuthenticationProperties(items), "External");
        return $"{options.Cookie.Name}={options.TicketDataFormat.Protect(ticket)}";
    }

    private HttpClient Client() => _configured.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = false,
        BaseAddress = new Uri("https://localhost"),
    });

    private Task<HttpResponseMessage> GetAsync(string path, string externalCookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("Cookie", externalCookie);
        return Client().SendAsync(request);
    }

    private async Task<HttpResponseMessage> PostAsync(string path, object body, string? externalCookie, bool withToken = true)
    {
        var client = Client();
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        var cookies = new List<string>();
        if (externalCookie is not null)
            cookies.Add(externalCookie);
        if (withToken)
        {
            var csrf = await client.GetAsync("/auth/csrf");
            var cookie = Assert.Single(SetCookieHeaderValue.ParseList(csrf.Headers.GetValues("Set-Cookie").ToList()));
            cookies.Add($"{cookie.Name}={cookie.Value}");
            request.Headers.Add("X-XSRF-TOKEN", (await csrf.Content.ReadFromJsonAsync<AntiforgeryTokenDto>())!.Token);
        }
        if (cookies.Count > 0)
            request.Headers.Add("Cookie", string.Join("; ", cookies));
        return await client.SendAsync(request);
    }

    private async Task<Account> AddInvitedAccountAsync(string email)
    {
        await using var db = NewDbContext();
        var plan = await db.Plans.SingleAsync(p => p.Name == Plan.Free);
        var organization = new Organization { Name = "Local Club", PlanId = plan.Id };
        var account = new Account
        {
            Email = email,
            Role = Role.Member,
            Status = AccountStatus.Invited,
            OrganizationId = organization.Id,
            InvitationTokenHash = InvitationTokens.HashToken(TestData.InvitationLinkToken),
            InvitationCodeHash = InvitationTokens.HashCode(TestData.InvitationCode),
            InvitationExpiresAt = DateTime.UtcNow.AddDays(1),
        };
        db.AddRange(organization, account);
        await db.SaveChangesAsync();
        return account;
    }

    private async Task AssertNothingCreatedAsync()
    {
        await using var db = NewDbContext();
        Assert.Empty(await db.Organizations.ToListAsync());
        Assert.Empty(await db.Accounts.ToListAsync());
    }

    private async Task AssertStillInvitedAsync()
    {
        await using var db = NewDbContext();
        Assert.Equal(AccountStatus.Invited, (await db.Accounts.SingleAsync(a => a.Email == "bob@example.com")).Status);
        Assert.Empty(await db.ExternalLogins.ToListAsync());
    }

    private NexoDbContext NewDbContext() =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<NexoDbContext>();
}
