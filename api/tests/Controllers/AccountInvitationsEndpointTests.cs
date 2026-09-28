using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Persistence;
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
        Assert.Null(dto.Name);
        Assert.Equal(Role.Member, dto.Role);
        Assert.Equal(AccountStatus.Invited, dto.Status);
        Assert.Equal(organizationId, dto.OrganizationId);

        await using var db = NewDbContext();
        var account = await db.Accounts.SingleAsync(a => a.Id == dto.Id);
        Assert.Equal(AccountStatus.Invited, account.Status);
        Assert.Equal(organizationId, account.OrganizationId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public async Task GivenAMissingOrInvalidEmail_WhenTheAdminInvites_ThenItRejectsAndCreatesNothing(string email)
    {
        var (organizationId, adminId) = await SeedOrganizationAsync();

        var response = await InviteAsync(organizationId, adminId, email);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertAccountCountAsync(1);
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

    [Theory]
    [InlineData(null)]
    [InlineData("00000000-0000-0000-0000-000000000001")]
    public async Task GivenNoOrUnknownCaller_WhenInviting_ThenItRejectsWithForbidden(string? header)
    {
        var (organizationId, _) = await SeedOrganizationAsync();
        var request = new HttpRequestMessage(HttpMethod.Post, $"/organizations/{organizationId}/invitations")
        {
            Content = JsonContent.Create(new InviteMemberDto { Email = "bob@example.com" }),
        };
        if (header is not null) request.Headers.Add("X-Account-Id", header);

        var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(AccountStatus.Invited)]
    [InlineData(AccountStatus.Active)]
    public async Task GivenAnEmailAlreadyRegistered_WhenInviting_ThenItRejectsWithConflict(AccountStatus status)
    {
        var (organizationId, adminId) = await SeedOrganizationAsync();
        await AddAccountAsync(organizationId, "bob@example.com", Role.Member, status);

        var response = await InviteAsync(organizationId, adminId, "BOB@example.com");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertAccountCountAsync(2);
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
    public async Task GivenTheLimitIsReachedOnlyCountingInvitedAccounts_WhenInviting_ThenItStillCreatesTheAccount()
    {
        var (organizationId, adminId) = await SeedOrganizationAsync();
        for (var i = 1; i < 20; i++)
            await AddAccountAsync(organizationId, $"m{i}@example.com", Role.Member, AccountStatus.Invited);

        var response = await InviteAsync(organizationId, adminId, "bob@example.com");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<(Guid OrganizationId, Guid AdminId)> SeedOrganizationAsync(
        string name = "Local Club", string adminEmail = "ana@example.com")
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/organizations", new RegisterOrganizationDto
        {
            OrganizationName = name,
            AdminName = "Ana Admin",
            AdminEmail = adminEmail,
        });
        var organization = (await response.Content.ReadFromJsonAsync<OrganizationDto>())!;
        await using var db = NewDbContext();
        var admin = await db.Accounts.SingleAsync(a => a.Email == adminEmail);
        return (organization.Id, admin.Id);
    }

    private async Task<Guid> AddAccountAsync(Guid organizationId, string email, Role role, AccountStatus status)
    {
        await using var db = NewDbContext();
        var account = new Account { Email = email, Role = role, Status = status, OrganizationId = organizationId };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();
        return account.Id;
    }

    private Task<HttpResponseMessage> InviteAsync(Guid organizationId, Guid callerId, string email)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/organizations/{organizationId}/invitations")
        {
            Content = JsonContent.Create(new InviteMemberDto { Email = email }),
        };
        request.Headers.Add("X-Account-Id", callerId.ToString());
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
