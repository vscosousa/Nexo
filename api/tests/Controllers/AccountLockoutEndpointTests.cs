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

/// <summary>Account lockout after repeated wrong passwords, and unlocking it through the emailed link, run against PostgreSQL.</summary>
public class AccountLockoutEndpointTests(PostgresApiFactory factory)
    : IClassFixture<PostgresApiFactory>, IAsyncLifetime
{
    private const string WrongPassword = "Wr0ng!Passw0rd";

    public Task InitializeAsync() => factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GivenTooManyWrongPasswords_WhenTheRightOneIsTried_ThenTheAccountStaysLockedAndTheOwnerIsEmailedAnUnlockLink()
    {
        await AddAccountAsync();

        for (var i = 0; i < AuthService.MaxSignInAttempts; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await SignInAsync(WrongPassword)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await SignInAsync(TestData.StrongPassword)).StatusCode);
        var email = Assert.Single(factory.Emails.Sent);
        Assert.Equal("ana@example.com", email.To);
        Assert.Contains("/unlock?email=ana%40example.com&token=", email.Text);
    }

    [Fact]
    public async Task GivenALockedAccount_WhenMoreWrongPasswordsAreTried_ThenNoFurtherEmailIsSent()
    {
        await AddAccountAsync();
        for (var i = 0; i < AuthService.MaxSignInAttempts + 3; i++)
            await SignInAsync(WrongPassword);

        Assert.Single(factory.Emails.Sent);
    }

    [Fact]
    public async Task GivenACorrectPasswordBetweenMistakes_WhenSigningIn_ThenTheCountStartsOver()
    {
        await AddAccountAsync();
        for (var i = 0; i < AuthService.MaxSignInAttempts - 1; i++)
            await SignInAsync(WrongPassword);
        Assert.Equal(HttpStatusCode.NoContent, (await SignInAsync(TestData.StrongPassword)).StatusCode);

        for (var i = 0; i < AuthService.MaxSignInAttempts - 1; i++)
            await SignInAsync(WrongPassword);

        Assert.Equal(HttpStatusCode.NoContent, (await SignInAsync(TestData.StrongPassword)).StatusCode);
        Assert.Empty(factory.Emails.Sent);
    }

    [Fact]
    public async Task GivenTheEmailedUnlockLink_WhenUnlocking_ThenTheOwnerCanSignInAgain()
    {
        await AddAccountAsync();
        var token = await LockAsync();

        var response = await UnlockAsync(" Ana@Example.com ", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await SignInAsync(TestData.StrongPassword)).StatusCode);
        await using var db = NewDbContext();
        var account = await db.Accounts.SingleAsync();
        Assert.Equal((0, (string?)null), (account.FailedSignInAttempts, account.UnlockTokenHash));
    }

    [Theory]
    [InlineData("ana@example.com", "wrong-token")]
    [InlineData("nobody@example.com", null)]
    public async Task GivenAWrongEmailOrToken_WhenUnlocking_ThenItRejectsAndTheAccountStaysLocked(string email, string? token)
    {
        await AddAccountAsync();
        token ??= await LockAsync();
        if (token == "wrong-token")
            await LockAsync();

        var response = await UnlockAsync(email, token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await SignInAsync(TestData.StrongPassword)).StatusCode);
    }

    [Fact]
    public async Task GivenAnExpiredUnlockLink_WhenUnlocking_ThenItRejectsAndTheAccountStaysLocked()
    {
        await AddAccountAsync();
        var token = await LockAsync();
        await ExpireUnlockLinkAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await UnlockAsync("ana@example.com", token)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await SignInAsync(TestData.StrongPassword)).StatusCode);
    }

    [Fact]
    public async Task GivenAnExpiredUnlockLink_WhenTheOwnerTriesToSignIn_ThenAFreshLinkIsEmailedAndOnlyItWorks()
    {
        await AddAccountAsync();
        var oldToken = await LockAsync();
        await ExpireUnlockLinkAsync();
        factory.Emails.Clear();

        Assert.Equal(HttpStatusCode.Unauthorized, (await SignInAsync(TestData.StrongPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await SignInAsync(TestData.StrongPassword)).StatusCode);

        var freshToken = CapturingEmailSender.TokenIn(Assert.Single(factory.Emails.Sent));
        Assert.Equal(HttpStatusCode.Forbidden, (await UnlockAsync("ana@example.com", oldToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await UnlockAsync("ana@example.com", freshToken)).StatusCode);
    }

    [Fact]
    public async Task GivenALock_WhenTheLinkIsEmailed_ThenItExpiresInADay()
    {
        await AddAccountAsync();
        await LockAsync();

        await using var db = NewDbContext();
        var expiresAt = (await db.Accounts.SingleAsync()).UnlockExpiresAt;
        Assert.NotNull(expiresAt);
        Assert.InRange(expiresAt.Value - DateTime.UtcNow, TimeSpan.FromHours(23.9), TimeSpan.FromHours(24.1));
    }

    [Fact]
    public async Task GivenASignedInSession_WhenTheAccountIsLocked_ThenTheSessionStopsWorking()
    {
        await AddAccountAsync();
        var session = SessionCookie.In(await SignInAsync(TestData.StrongPassword)).Value.ToString();

        await LockAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/auth/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session);
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().SendAsync(request)).StatusCode);
    }

    /// <summary>Locks the account with wrong passwords and returns the token from the emailed unlock link.</summary>
    private async Task<string> LockAsync()
    {
        factory.Emails.Clear();
        for (var i = 0; i < AuthService.MaxSignInAttempts; i++)
            await SignInAsync(WrongPassword);
        return CapturingEmailSender.TokenIn(Assert.Single(factory.Emails.Sent));
    }

    private async Task ExpireUnlockLinkAsync()
    {
        await using var db = NewDbContext();
        await db.Accounts.ExecuteUpdateAsync(s => s.SetProperty(a => a.UnlockExpiresAt, DateTime.UtcNow.AddMinutes(-1)));
    }

    private Task<HttpResponseMessage> SignInAsync(string password) =>
        factory.CreateClient().PostAsJsonAsync("/auth/sign-in", new SignInDto { Email = "ana@example.com", Password = password });

    private Task<HttpResponseMessage> UnlockAsync(string email, string token) =>
        factory.CreateClient().PostAsJsonAsync("/auth/unlock", new UnlockAccountDto { Email = email, Token = token });

    private async Task AddAccountAsync()
    {
        await using var db = NewDbContext();
        var plan = await db.Plans.SingleAsync(p => p.Name == Plan.Free);
        var organization = new Organization { Name = "Local Club", PlanId = plan.Id };
        var account = new Account
        {
            Email = "ana@example.com",
            Role = Role.Admin,
            Status = AccountStatus.Active,
            OrganizationId = organization.Id,
        };
        account.PasswordHash = new PasswordHasher<Account>().HashPassword(account, TestData.StrongPassword);
        db.AddRange(organization, account);
        await db.SaveChangesAsync();
    }

    private NexoDbContext NewDbContext() =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<NexoDbContext>();
}
