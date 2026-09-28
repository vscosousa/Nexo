using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Persistence;
using Nexo.Api.Services;
using Nexo.Api.Tests.Infrastructure;
using Xunit;

namespace Nexo.Api.Tests.Controllers;

/// <summary>US-003 acceptance tests for the password path, run against PostgreSQL.</summary>
public class AccountActivationsEndpointTests(PostgresApiFactory factory)
    : IClassFixture<PostgresApiFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GivenAPendingAccount_WhenThePersonActivates_ThenItBecomesAnActiveMemberWithAPassword()
    {
        var organizationId = await SeedOrganizationAsync();
        await AddAccountAsync(organizationId, "bob@example.com", AccountStatus.Invited);

        var response = await ActivateAsync(" Bob@Example.com ", "Bob", "Builder", TestData.StrongPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(dto);
        Assert.Equal("bob@example.com", dto.Email);
        Assert.Equal("Bob", dto.FirstName);
        Assert.Equal("Builder", dto.LastName);
        Assert.Equal(Role.Member, dto.Role);
        Assert.Equal(AccountStatus.Active, dto.Status);
        Assert.Equal(organizationId, dto.OrganizationId);

        await using var db = NewDbContext();
        var account = await db.Accounts.SingleAsync(a => a.Id == dto.Id);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Null(account.InvitationTokenHash);
        Assert.Null(account.InvitationCodeHash);
        Assert.Equal(
            PasswordVerificationResult.Success,
            new PasswordHasher<Account>().VerifyHashedPassword(account, account.PasswordHash!, TestData.StrongPassword));
    }

    [Theory]
    [InlineData("", "Bob", "Builder", TestData.StrongPassword)]
    [InlineData("not-an-email", "Bob", "Builder", TestData.StrongPassword)]
    [InlineData("bob@example.com", "", "Builder", TestData.StrongPassword)]
    [InlineData("bob@example.com", "Bob", "", TestData.StrongPassword)]
    [InlineData("bob@example.com", "Bob", "Builder", "")]
    [InlineData("bob@example.com", "Bob", "Builder", "weak")]
    [InlineData("bob@example.com", "Bob", "Builder", "Xx!LocalClub9")]
    [InlineData("bob@example.com", "Bob", "Builder", "Xx!L0c@l Cl_ub9")]
    [InlineData("bob@example.com", "Bob", "Builder", "Xx!Bu1lder-9zq")]
    [InlineData("bob@example.com", "Bob", "Builder", "Xx!BOB9zqvw")]
    [InlineData("OVERLONG_NAME", "Bob", "Builder", TestData.StrongPassword)]
    public async Task GivenMissingOrInvalidFields_WhenThePersonActivates_ThenItRejectsAndChangesNothing(
        string email, string firstName, string lastName, string password)
    {
        var organizationId = await SeedOrganizationAsync();
        await AddAccountAsync(organizationId, "bob@example.com", AccountStatus.Invited);
        if (email == "OVERLONG_NAME") (email, firstName) = ("bob@example.com", new string('n', 101));

        var response = await ActivateAsync(email, firstName, lastName, password);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertStillInvitedAsync("bob@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("missing")]
    public async Task GivenAMissingLinkTokenOrCode_WhenThePersonActivates_ThenItRejectsAndChangesNothing(string missing)
    {
        var organizationId = await SeedOrganizationAsync();
        await AddAccountAsync(organizationId, "bob@example.com", AccountStatus.Invited);

        var response = await ActivateAsync(
            "bob@example.com", "Bob", "Builder", TestData.StrongPassword,
            token: missing == "missing" ? "" : TestData.InvitationLinkToken,
            code: missing == "missing" ? TestData.InvitationCode : "");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertStillInvitedAsync("bob@example.com");
    }

    [Fact]
    public async Task GivenAnEmailNotRegistered_WhenSomeoneActivates_ThenItRejectsWithForbidden()
    {
        await SeedOrganizationAsync();

        var response = await ActivateAsync("stranger@example.com", "Sam", "Stranger", TestData.StrongPassword);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var db = NewDbContext();
        Assert.Equal(1, await db.Accounts.CountAsync());
    }

    [Fact]
    public async Task GivenAWrongLinkToken_WhenSomeoneActivates_ThenItRejectsWithForbiddenAndChangesNothing()
    {
        var organizationId = await SeedOrganizationAsync();
        await AddAccountAsync(organizationId, "bob@example.com", AccountStatus.Invited);

        var response = await ActivateAsync(
            "bob@example.com", "Impostor", "Impostor", TestData.StrongPassword, token: "guessed-link-token");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertStillInvitedAsync("bob@example.com");
    }

    [Fact]
    public async Task GivenTheRightLinkTokenButAWrongCode_WhenSomeoneActivates_ThenItRejectsWithForbiddenAndChangesNothing()
    {
        // The link token alone (e.g. lifted from a forwarded URL) must not be enough on its own.
        var organizationId = await SeedOrganizationAsync();
        await AddAccountAsync(organizationId, "bob@example.com", AccountStatus.Invited);

        var response = await ActivateAsync(
            "bob@example.com", "Impostor", "Impostor", TestData.StrongPassword, code: "GUESS1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertStillInvitedAsync("bob@example.com");
    }

    [Fact]
    public async Task GivenAnAlreadyActiveAccount_WhenSomeoneActivates_ThenItRejectsWithConflict()
    {
        var organizationId = await SeedOrganizationAsync();
        await AddAccountAsync(organizationId, "bob@example.com", AccountStatus.Active);

        var response = await ActivateAsync("bob@example.com", "Impostor", "Impostor", TestData.StrongPassword);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await using var db = NewDbContext();
        var account = await db.Accounts.SingleAsync(a => a.Email == "bob@example.com");
        Assert.Null(account.FirstName);
        Assert.Null(account.LastName);
        Assert.Null(account.PasswordHash);
    }

    [Fact]
    public async Task GivenAnExpiredInvitation_WhenActivating_ThenItRejectsAsForbidden()
    {
        var organizationId = await SeedOrganizationAsync();
        await using (var db = NewDbContext())
        {
            db.Accounts.Add(new Account
            {
                Email = "bob@example.com",
                Role = Role.Member,
                Status = AccountStatus.Invited,
                OrganizationId = organizationId,
                InvitationTokenHash = InvitationTokens.HashToken(TestData.InvitationLinkToken),
                InvitationCodeHash = InvitationTokens.HashCode(TestData.InvitationCode),
                InvitationExpiresAt = DateTime.UtcNow.AddDays(-1),
            });
            await db.SaveChangesAsync();
        }

        var response = await ActivateAsync("bob@example.com", "Bob", "Builder", TestData.StrongPassword);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertStillInvitedAsync("bob@example.com");
    }

    [Fact]
    public async Task GivenTheActiveLimitIsReached_WhenAPendingAccountActivates_ThenItRejectsWithConflict()
    {
        var organizationId = await SeedOrganizationAsync();
        for (var i = 1; i < 20; i++)
            await AddAccountAsync(organizationId, $"m{i}@example.com", AccountStatus.Active);
        await AddAccountAsync(organizationId, "bob@example.com", AccountStatus.Invited);

        var response = await ActivateAsync("bob@example.com", "Bob", "Builder", TestData.StrongPassword);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertStillInvitedAsync("bob@example.com");
    }

    [Fact]
    public async Task GivenAPendingAccount_WhenResendIsRequested_ThenOnlyTheCodeChangesAndTheOriginalLinkStillWorksWithIt()
    {
        var organizationId = await SeedOrganizationAsync();
        await AddAccountAsync(organizationId, "bob@example.com", AccountStatus.Invited);

        var response = await ResendAsync("bob@example.com");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var email = Assert.Single(factory.Emails.Sent);
        Assert.Equal("bob@example.com", email.To);
        // No link in a resend email: the original one (already open in the person's browser) still works.
        Assert.DoesNotContain("/activate?", email.Text);
        var newCode = CapturingEmailSender.CodeIn(email);
        Assert.NotEqual(TestData.InvitationCode, newCode);

        var oldCodeAttempt = await ActivateAsync("bob@example.com", "Bob", "Builder", TestData.StrongPassword);
        Assert.Equal(HttpStatusCode.Forbidden, oldCodeAttempt.StatusCode);

        // The original link token (unchanged) plus the freshly emailed code activates the account.
        var newAttempt = await ActivateAsync(
            "bob@example.com", "Bob", "Builder", TestData.StrongPassword, TestData.InvitationLinkToken, newCode);
        Assert.Equal(HttpStatusCode.OK, newAttempt.StatusCode);
    }

    [Theory]
    [InlineData("nobody@example.com")] // not registered anywhere
    public async Task GivenAnEmailWithNoPendingInvitation_WhenResendIsRequested_ThenItStillAcceptsButSendsNothing(
        string email)
    {
        await SeedOrganizationAsync();

        var response = await ResendAsync(email);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Empty(factory.Emails.Sent);
    }

    [Fact]
    public async Task GivenAnAlreadyActiveAccount_WhenResendIsRequested_ThenItStillAcceptsButSendsNothing()
    {
        var organizationId = await SeedOrganizationAsync();
        await AddAccountAsync(organizationId, "bob@example.com", AccountStatus.Active);

        var response = await ResendAsync("bob@example.com");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Empty(factory.Emails.Sent);
    }

    private Task<HttpResponseMessage> ResendAsync(string email) =>
        factory.CreateClient().PostAsJsonAsync("/accounts/activation/resend", new ResendInvitationDto { Email = email });

    [Fact]
    public async Task GivenAPendingAccountAndItsLinkTokenAndCode_WhenVerifying_ThenItSucceedsAndChangesNothing()
    {
        var organizationId = await SeedOrganizationAsync();
        await AddAccountAsync(organizationId, "bob@example.com", AccountStatus.Invited);

        var response = await VerifyAsync("bob@example.com");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertStillInvitedAsync("bob@example.com");
    }

    [Fact]
    public async Task GivenAWrongCode_WhenVerifying_ThenItRejectsAsForbidden()
    {
        var organizationId = await SeedOrganizationAsync();
        await AddAccountAsync(organizationId, "bob@example.com", AccountStatus.Invited);

        var response = await VerifyAsync("bob@example.com", code: "GUESS1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GivenAWrongLinkToken_WhenVerifying_ThenItRejectsAsForbidden()
    {
        var organizationId = await SeedOrganizationAsync();
        await AddAccountAsync(organizationId, "bob@example.com", AccountStatus.Invited);

        var response = await VerifyAsync("bob@example.com", token: "guessed-link-token");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GivenAnAlreadyActiveAccount_WhenVerifying_ThenItRejectsWithConflict()
    {
        var organizationId = await SeedOrganizationAsync();
        await AddAccountAsync(organizationId, "bob@example.com", AccountStatus.Active);

        var response = await VerifyAsync("bob@example.com");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private Task<HttpResponseMessage> VerifyAsync(
        string email, string token = TestData.InvitationLinkToken, string code = TestData.InvitationCode) =>
        factory.CreateClient().PostAsJsonAsync(
            "/accounts/activation/verify", new VerifyInvitationDto { Email = email, LinkToken = token, Code = code });

    private async Task<Guid> SeedOrganizationAsync()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/organizations", new RegisterOrganizationDto
        {
            OrganizationName = "Local Club",
            AdminFirstName = "Ana",
            AdminLastName = "Admin",
            AdminEmail = "ana@example.com",
            Password = TestData.StrongPassword,
        });
        return (await response.Content.ReadFromJsonAsync<OrganizationDto>())!.Id;
    }

    private async Task AddAccountAsync(Guid organizationId, string email, AccountStatus status)
    {
        await using var db = NewDbContext();
        db.Accounts.Add(new Account
        {
            Email = email,
            Role = Role.Member,
            Status = status,
            OrganizationId = organizationId,
            InvitationTokenHash = status == AccountStatus.Invited ? InvitationTokens.HashToken(TestData.InvitationLinkToken) : null,
            InvitationCodeHash = status == AccountStatus.Invited ? InvitationTokens.HashCode(TestData.InvitationCode) : null,
            InvitationExpiresAt = status == AccountStatus.Invited ? DateTime.UtcNow.AddDays(1) : null,
        });
        await db.SaveChangesAsync();
    }

    private Task<HttpResponseMessage> ActivateAsync(
        string email, string firstName, string lastName, string password,
        string token = TestData.InvitationLinkToken, string code = TestData.InvitationCode) =>
        factory.CreateClient().PostAsJsonAsync(
            "/accounts/activation",
            new ActivateAccountDto
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Password = password,
                LinkToken = token,
                Code = code,
            });

    private async Task AssertStillInvitedAsync(string email)
    {
        await using var db = NewDbContext();
        var account = await db.Accounts.SingleAsync(a => a.Email == email);
        Assert.Equal(AccountStatus.Invited, account.Status);
        Assert.Null(account.PasswordHash);
        Assert.NotNull(account.InvitationTokenHash);
        Assert.NotNull(account.InvitationCodeHash);
    }

    private NexoDbContext NewDbContext() =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<NexoDbContext>();
}
