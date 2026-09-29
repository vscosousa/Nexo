using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Persistence;
using Nexo.Api.Services;
using Nexo.Api.Tests.Infrastructure;
using Xunit;

namespace Nexo.Api.Tests.Controllers;

/// <summary>US-002 acceptance tests, run against PostgreSQL.</summary>
public class AccountInvitationsEndpointTests(PostgresApiFactory factory)
    : IClassFixture<PostgresApiFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GivenAValidNewEmail_WhenTheAdminInvites_ThenAPendingMemberAccountIsCreated()
    {
        var (organizationId, adminId) = await SeedOrganizationAsync();

        var response = await InviteAsync(organizationId, adminId, " Bob@Example.com ");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(dto);
        Assert.Equal("bob@example.com", dto.Email);
        Assert.Null(dto.FirstName);
        Assert.Null(dto.LastName);
        Assert.Equal(Role.Member, dto.Role);
        Assert.Equal(AccountStatus.Invited, dto.Status);
        Assert.Equal(organizationId, dto.OrganizationId);

        await using var db = NewDbContext();
        var account = await db.Accounts.SingleAsync(a => a.Id == dto.Id);
        Assert.Equal(AccountStatus.Invited, account.Status);
        Assert.Equal(organizationId, account.OrganizationId);

        var email = Assert.Single(factory.Emails.Sent);
        Assert.Equal("bob@example.com", email.To);
        Assert.Contains("Local Club", email.Subject);
        Assert.Contains("Ana Admin", email.Text);
        var token = CapturingEmailSender.TokenIn(email);
        var code = CapturingEmailSender.CodeIn(email);
        Assert.True(InvitationTokens.TokenMatches(token, account.InvitationTokenHash));
        Assert.True(InvitationTokens.CodeMatches(code, account.InvitationCodeHash));
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(token, body);
        Assert.DoesNotContain(code, body);
    }

    [Fact]
    public async Task GivenTheInvitationEmail_WhenTheMemberActivatesWithItsLinkTokenAndCode_ThenTheAccountIsActive()
    {
        var (organizationId, adminId) = await SeedOrganizationAsync();
        await InviteAsync(organizationId, adminId, "bob@example.com");
        var email = Assert.Single(factory.Emails.Sent);
        var token = CapturingEmailSender.TokenIn(email);
        var code = CapturingEmailSender.CodeIn(email);

        var response = await factory.CreateClient().PostAsJsonAsync("/accounts/activation", new ActivateAccountDto
        {
            Email = "bob@example.com",
            FirstName = "Bob",
            LastName = "Builder",
            Password = TestData.StrongPassword,
            LinkToken = token,
            Code = code,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GivenTheInvitationEmail_WhenActivatingWithTheLinkTokenButAGuessedCode_ThenItRejectsAsForbidden()
    {
        var (organizationId, adminId) = await SeedOrganizationAsync();
        await InviteAsync(organizationId, adminId, "bob@example.com");
        var token = CapturingEmailSender.TokenIn(Assert.Single(factory.Emails.Sent));

        var response = await factory.CreateClient().PostAsJsonAsync("/accounts/activation", new ActivateAccountDto
        {
            Email = "bob@example.com",
            FirstName = "Bob",
            LastName = "Builder",
            Password = TestData.StrongPassword,
            LinkToken = token,
            Code = "GUESS1",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("TOO_LONG")]
    public async Task GivenAMissingOrInvalidEmail_WhenTheAdminInvites_ThenItRejectsAndCreatesNothing(string email)
    {
        var (organizationId, adminId) = await SeedOrganizationAsync();

        if (email == "TOO_LONG") email = new string('a', 310) + "@example.com";
        var response = await InviteAsync(organizationId, adminId, email);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertAccountCountAsync(1);
        Assert.Empty(factory.Emails.Sent);
    }

    [Fact]
    public async Task GivenAMemberCaller_WhenInviting_ThenItRejectsWithForbidden()
    {
        var (organizationId, _) = await SeedOrganizationAsync();
        var memberId = await AddAccountAsync(organizationId, "member@example.com", Role.Member, AccountStatus.Active);

        var response = await InviteAsync(organizationId, memberId, "bob@example.com");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertAccountCountAsync(2);
    }

    [Fact]
    public async Task GivenAnAdminOfAnotherOrganization_WhenInviting_ThenItRejectsWithForbidden()
    {
        var (organizationId, _) = await SeedOrganizationAsync();
        var (_, otherAdminId) = await SeedOrganizationAsync("Other Club", "other@example.com");

        var response = await InviteAsync(organizationId, otherAdminId, "bob@example.com");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertAccountCountAsync(2);
    }

    [Fact]
    public async Task GivenNoToken_WhenInviting_ThenItRejectsWithUnauthorized()
    {
        var (organizationId, _) = await SeedOrganizationAsync();
        var response = await factory.CreateClient().PostAsJsonAsync(
            $"/organizations/{organizationId}/invitations", new InviteMemberDto { Email = "bob@example.com" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertAccountCountAsync(1);
    }

    [Fact]
    public async Task GivenATokenSignedWithAnotherKey_WhenInviting_ThenItRejectsWithUnauthorized()
    {
        var (organizationId, adminId) = await SeedOrganizationAsync();
        var forged = new TokenService(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = "another-signing-key-at-least-32-bytes!!" })
            .Build()).GenerateToken(new Account { Email = "ana@example.com", Id = adminId, OrganizationId = organizationId, Role = Role.Admin }).Token;

        var response = await InviteAsync(organizationId, forged, "bob@example.com");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertAccountCountAsync(1);
    }

    [Fact]
    public async Task GivenAValidTokenOfAnUnknownAccount_WhenInviting_ThenTheSessionIsRejected()
    {
        var (organizationId, _) = await SeedOrganizationAsync();

        var response = await InviteAsync(organizationId, Guid.NewGuid(), "bob@example.com");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GivenAnActiveEmailAlreadyRegistered_WhenInviting_ThenItRejectsWithConflict()
    {
        var (organizationId, adminId) = await SeedOrganizationAsync();
        await AddAccountAsync(organizationId, "bob@example.com", Role.Member, AccountStatus.Active);

        var response = await InviteAsync(organizationId, adminId, "BOB@example.com");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertAccountCountAsync(2);
        Assert.Empty(factory.Emails.Sent);
    }

    [Fact]
    public async Task GivenAnEmailAlreadyPendingInAnotherOrganization_WhenInviting_ThenItRejectsWithConflict()
    {
        var (organizationId, adminId) = await SeedOrganizationAsync();
        var (otherOrganizationId, _) = await SeedOrganizationAsync("Other Club", "other@example.com");
        await AddAccountAsync(otherOrganizationId, "bob@example.com", Role.Member, AccountStatus.Invited);

        var response = await InviteAsync(organizationId, adminId, "BOB@example.com");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertAccountCountAsync(3);
        Assert.Empty(factory.Emails.Sent);
    }

    [Fact]
    public async Task GivenAnEmailAlreadyPendingInTheSameOrganization_WhenInviting_ThenItReplacesTheInvitationInstead()
    {
        var (organizationId, adminId) = await SeedOrganizationAsync();
        var accountId = await AddAccountAsync(organizationId, "bob@example.com", Role.Member, AccountStatus.Invited);

        var response = await InviteAsync(organizationId, adminId, "BOB@example.com");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await AssertAccountCountAsync(2);
        var email = Assert.Single(factory.Emails.Sent);
        var token = CapturingEmailSender.TokenIn(email);
        var code = CapturingEmailSender.CodeIn(email);
        await using var db = NewDbContext();
        var account = await db.Accounts.SingleAsync(a => a.Id == accountId);
        Assert.True(InvitationTokens.TokenMatches(token, account.InvitationTokenHash));
        Assert.True(InvitationTokens.CodeMatches(code, account.InvitationCodeHash));
    }

    [Fact]
    public async Task GivenTheActiveLimitIsReached_WhenInviting_ThenItRejectsWithConflict()
    {
        var (organizationId, adminId) = await SeedOrganizationAsync();
        for (var i = 1; i < 20; i++)
            await AddAccountAsync(organizationId, $"m{i}@example.com", Role.Member, AccountStatus.Active);

        var response = await InviteAsync(organizationId, adminId, "bob@example.com");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertAccountCountAsync(20);
    }

    [Fact]
    public async Task GivenTheLimitIsReachedCountingPendingInvitations_WhenInvitingSomeoneNew_ThenItRejectsWithConflict()
    {
        var (organizationId, adminId) = await SeedOrganizationAsync();
        for (var i = 1; i < 20; i++)
            await AddAccountAsync(organizationId, $"m{i}@example.com", Role.Member, AccountStatus.Invited);

        var response = await InviteAsync(organizationId, adminId, "bob@example.com");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertAccountCountAsync(20);
    }

    [Fact]
    public async Task GivenTheLimitIsReachedCountingPendingInvitations_WhenReinvitingAPendingEmail_ThenItStillSendsAFreshInvitation()
    {
        var (organizationId, adminId) = await SeedOrganizationAsync();
        for (var i = 1; i < 20; i++)
            await AddAccountAsync(organizationId, $"m{i}@example.com", Role.Member, AccountStatus.Invited);

        var response = await InviteAsync(organizationId, adminId, "m1@example.com");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<(Guid OrganizationId, Guid AdminId)> SeedOrganizationAsync(
        string name = "Local Club", string adminEmail = "ana@example.com")
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/organizations", new RegisterOrganizationDto
        {
            OrganizationName = name,
            AdminFirstName = "Ana",
            AdminLastName = "Admin",
            AdminEmail = adminEmail,
            Password = TestData.StrongPassword,
            PlanId = NexoDbContext.FreePlanId,
        });
        response.EnsureSuccessStatusCode();
        factory.Emails.Clear();
        await using var db = NewDbContext();
        await db.Accounts.Where(a => a.Email == adminEmail)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, AccountStatus.Active));
        var admin = await db.Accounts.SingleAsync(a => a.Email == adminEmail);
        return (admin.OrganizationId, admin.Id);
    }

    private async Task<Guid> AddAccountAsync(Guid organizationId, string email, Role role, AccountStatus status)
    {
        await using var db = NewDbContext();
        var account = new Account { Email = email, Role = role, Status = status, OrganizationId = organizationId };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();
        return account.Id;
    }

    private Task<HttpResponseMessage> InviteAsync(Guid organizationId, Guid callerId, string email) =>
        InviteAsync(organizationId, factory.Services.GetRequiredService<ITokenService>()
            .GenerateToken(new Account { Email = "caller@example.com", Id = callerId, OrganizationId = organizationId, Role = Role.Admin }).Token, email);

    private Task<HttpResponseMessage> InviteAsync(Guid organizationId, string token, string email)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/organizations/{organizationId}/invitations")
        {
            Content = JsonContent.Create(new InviteMemberDto { Email = email }),
        };
        request.Headers.Authorization = new("Bearer", token);
        return factory.CreateClient().SendAsync(request);
    }

    private async Task AssertAccountCountAsync(int expected)
    {
        await using var db = NewDbContext();
        Assert.Equal(expected, await db.Accounts.CountAsync());
    }

    private NexoDbContext NewDbContext() =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<NexoDbContext>();
}
