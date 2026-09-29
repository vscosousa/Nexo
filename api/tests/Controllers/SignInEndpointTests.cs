using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Persistence;
using Nexo.Api.Tests.Infrastructure;
using Xunit;

namespace Nexo.Api.Tests.Controllers;

/// <summary>US-005 acceptance tests for password sign-in, run against PostgreSQL.</summary>
public class SignInEndpointTests(PostgresApiFactory factory)
    : IClassFixture<PostgresApiFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GivenCorrectCredentials_WhenTheUserSignsIn_ThenAnHttpOnlySessionCookieWithTheirClaimsIsSet()
    {
        var account = await AddAccountAsync("ana@example.com", Role.Admin, AccountStatus.Active, TestData.StrongPassword);

        var response = await SignInAsync(" Ana@Example.com ", TestData.StrongPassword);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var cookie = SessionCookie.In(response);
        Assert.True(cookie.HttpOnly);
        Assert.True(cookie.Secure);
        Assert.Equal(Microsoft.Net.Http.Headers.SameSiteMode.None, cookie.SameSite);
        var token = new JsonWebToken(cookie.Value.ToString());
        Assert.Equal(account.Id.ToString(), token.Subject);
        Assert.Equal(account.OrganizationId.ToString(), token.GetClaim("orgId").Value);
        Assert.Equal("Admin", token.GetClaim("role").Value);
        Assert.InRange(cookie.Expires!.Value - DateTimeOffset.UtcNow, TimeSpan.FromHours(7.9), TimeSpan.FromHours(8.1));
        Assert.Equal(cookie.Expires.Value.ToUnixTimeSeconds(), new DateTimeOffset(token.ValidTo).ToUnixTimeSeconds());
    }

    [Fact]
    public async Task GivenAWrongPassword_WhenTheUserSignsIn_ThenItRejectsWithTheGenericError()
    {
        await AddAccountAsync("ana@example.com", Role.Admin, AccountStatus.Active, TestData.StrongPassword);

        var wrong = await SignInAsync("ana@example.com", "Wr0ng!Passw0rd");
        var unknown = await SignInAsync("nobody@example.com", TestData.StrongPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal(await BodyWithoutTraceAsync(unknown), await BodyWithoutTraceAsync(wrong));
    }

    [Fact]
    public async Task GivenAnAccountWithoutAPassword_WhenTheUserSignsInWithOne_ThenItRejectsWithTheGenericError()
    {
        await AddAccountAsync("sso@example.com", Role.Member, AccountStatus.Active, password: null);

        var response = await SignInAsync("sso@example.com", TestData.StrongPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GivenAnInvitedAccount_WhenTheUserSignsIn_ThenItRejectsWithTheGenericError()
    {
        await AddAccountAsync("bob@example.com", Role.Member, AccountStatus.Invited, TestData.StrongPassword);

        var response = await SignInAsync("bob@example.com", TestData.StrongPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("", TestData.StrongPassword)]
    [InlineData("ana@example.com", "")]
    public async Task GivenMissingFields_WhenTheUserSignsIn_ThenItRejectsWithValidationErrors(string email, string password)
    {
        var response = await SignInAsync(email, password);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<string> BodyWithoutTraceAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        return $"{problem!.Status}|{problem.Title}|{problem.Detail}";
    }

    private async Task<Account> AddAccountAsync(string email, Role role, AccountStatus status, string? password)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NexoDbContext>();
        var plan = await db.Plans.SingleAsync(p => p.Name == Plan.Free);
        var organization = new Organization { Name = "Local Club", PlanId = plan.Id };
        var account = new Account { Email = email, Role = role, Status = status, OrganizationId = organization.Id };
        if (password is not null)
            account.PasswordHash = new PasswordHasher<Account>().HashPassword(account, password);
        db.AddRange(organization, account);
        await db.SaveChangesAsync();
        return account;
    }

    private Task<HttpResponseMessage> SignInAsync(string email, string password) =>
        factory.CreateClient().PostAsJsonAsync("/auth/sign-in", new SignInDto { Email = email, Password = password });
}
