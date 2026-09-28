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

        var response = await ActivateAsync(" Bob@Example.com ", "Bob", TestData.StrongPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(dto);
        Assert.Equal("bob@example.com", dto.Email);
        Assert.Equal("Bob", dto.Name);
        Assert.Equal(Role.Member, dto.Role);
        Assert.Equal(AccountStatus.Active, dto.Status);
        Assert.Equal(organizationId, dto.OrganizationId);

        await using var db = NewDbContext();
        var account = await db.Accounts.SingleAsync(a => a.Id == dto.Id);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Null(account.InvitationTokenHash);
        Assert.Equal(
            PasswordVerificationResult.Success,
            new PasswordHasher<Account>().VerifyHashedPassword(account, account.PasswordHash!, TestData.StrongPassword));
    }

    [Theory]
    [InlineData("", "Bob", TestData.StrongPassword, TestData.InvitationToken)]
    [InlineData("not-an-email", "Bob", TestData.StrongPassword, TestData.InvitationToken)]
    [InlineData("bob@example.com", "", TestData.StrongPassword, TestData.InvitationToken)]
    [InlineData("bob@example.com", "Bob", "", TestData.InvitationToken)]
    [InlineData("bob@example.com", "Bob", TestData.StrongPassword, "")]
    [InlineData("bob@example.com", "Bob", "weak", TestData.InvitationToken)]
    [InlineData("bob@example.com", "Bob", "Xx!LocalClub9", TestData.InvitationToken)]
    [InlineData("bob@example.com", "Bob", "Xx!L0c@l Cl_ub9", TestData.InvitationToken)]
    [InlineData("bob@example.com", "Bob Builder", "Xx!Bu1lder-9zq", TestData.InvitationToken)]
    [InlineData("bob@example.com", "Bob", "Xx!BOB9zqvw", TestData.InvitationToken)]
    [InlineData("OVERLONG_NAME", "Bob", TestData.StrongPassword, TestData.InvitationToken)]
    public async Task GivenMissingOrInvalidFields_WhenThePersonActivates_ThenItRejectsAndChangesNothing(
        string email, string name, string password, string token)
    {
        var organizationId = await SeedOrganizationAsync();
        await AddAccountAsync(organizationId, "bob@example.com", AccountStatus.Invited);
        if (email == "OVERLONG_NAME") (email, name) = ("bob@example.com", new string('n', 201));

        var response = await ActivateAsync(email, name, password, token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertStillInvitedAsync("bob@example.com");
    }

    [Fact]
    public async Task GivenAnEmailNotRegistered_WhenSomeoneActivates_ThenItRejectsWithForbidden()
    {
        await SeedOrganizationAsync();

        var response = await ActivateAsync("stranger@example.com", "Sam", TestData.StrongPassword);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var db = NewDbContext();
        Assert.Equal(1, await db.Accounts.CountAsync());
    }

    [Fact]
    public async Task GivenAWrongInvitationToken_WhenSomeoneActivates_ThenItRejectsWithForbiddenAndChangesNothing()
    {
        var organizationId = await SeedOrganizationAsync();
        await AddAccountAsync(organizationId, "bob@example.com", AccountStatus.Invited);

        var response = await ActivateAsync("bob@example.com", "Impostor", TestData.StrongPassword, "guessed-token");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertStillInvitedAsync("bob@example.com");
    }

    [Fact]
    public async Task GivenAnAlreadyActiveAccount_WhenSomeoneActivates_ThenItRejectsWithConflict()
    {
        var organizationId = await SeedOrganizationAsync();
        await AddAccountAsync(organizationId, "bob@example.com", AccountStatus.Active);

        var response = await ActivateAsync("bob@example.com", "Impostor", TestData.StrongPassword);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await using var db = NewDbContext();
        var account = await db.Accounts.SingleAsync(a => a.Email == "bob@example.com");
        Assert.Null(account.Name);
        Assert.Null(account.PasswordHash);
    }

    [Fact]
    public async Task GivenTheActiveLimitIsReached_WhenAPendingAccountActivates_ThenItRejectsWithConflict()
    {
        var organizationId = await SeedOrganizationAsync();
        for (var i = 1; i < 20; i++)
            await AddAccountAsync(organizationId, $"m{i}@example.com", AccountStatus.Active);
        await AddAccountAsync(organizationId, "bob@example.com", AccountStatus.Invited);

        var response = await ActivateAsync("bob@example.com", "Bob", TestData.StrongPassword);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertStillInvitedAsync("bob@example.com");
    }

    private async Task<Guid> SeedOrganizationAsync()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/organizations", new RegisterOrganizationDto
        {
            OrganizationName = "Local Club",
            AdminName = "Ana Admin",
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
            InvitationTokenHash = status == AccountStatus.Invited ? InvitationTokens.Hash(TestData.InvitationToken) : null,
        });
        await db.SaveChangesAsync();
    }

    private Task<HttpResponseMessage> ActivateAsync(
        string email, string name, string password, string token = TestData.InvitationToken) =>
        factory.CreateClient().PostAsJsonAsync(
            "/accounts/activation",
            new ActivateAccountDto { Email = email, Name = name, Password = password, InvitationToken = token });

    private async Task AssertStillInvitedAsync(string email)
    {
        await using var db = NewDbContext();
        var account = await db.Accounts.SingleAsync(a => a.Email == email);
        Assert.Equal(AccountStatus.Invited, account.Status);
        Assert.Null(account.PasswordHash);
        Assert.NotNull(account.InvitationTokenHash);
    }

    private NexoDbContext NewDbContext() =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<NexoDbContext>();
}
