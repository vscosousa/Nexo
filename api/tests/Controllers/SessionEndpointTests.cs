using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Persistence;
using Nexo.Api.Tests.Infrastructure;
using Xunit;

namespace Nexo.Api.Tests.Controllers;

/// <summary>Session cookie, current-account, sign-out and anti-forgery acceptance tests, run against PostgreSQL.</summary>
public class SessionEndpointTests(PostgresApiFactory factory)
    : IClassFixture<PostgresApiFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GivenASessionCookie_WhenAskingWhoIsSignedIn_ThenItReturnsTheAccount()
    {
        var account = await AddAccountAsync();
        var browser = await SignedInBrowserAsync();

        var response = await browser.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<CurrentAccountDto>();
        Assert.Equal((account.Id, account.OrganizationId, "Admin"), (me!.Id, me.OrganizationId, me.Role));
        Assert.Equal(("ana@example.com", "Ana", "Ribeiro"), (me.Email, me.FirstName, me.LastName));
    }

    [Fact]
    public async Task GivenNoSession_WhenAskingWhoIsSignedIn_ThenItRejectsWithUnauthorized()
    {
        var response = await Browser().GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GivenASessionAndTheAntiforgeryToken_WhenSigningOut_ThenTheSessionEnds()
    {
        await AddAccountAsync();
        var browser = await SignedInBrowserAsync();

        var response = await browser.SendAsync(await WithAntiforgeryTokenAsync(browser, SignOutRequest()));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(SessionCookie.In(response).Expires < DateTimeOffset.UtcNow);
        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.GetAsync("/auth/me")).StatusCode);
    }

    [Fact]
    public async Task GivenSessionsOnTwoDevices_WhenOneSignsOut_ThenTheOtherSessionEndsToo()
    {
        await AddAccountAsync();
        var laptop = await SignedInBrowserAsync();
        var phone = await SignedInBrowserAsync();

        await laptop.SendAsync(await WithAntiforgeryTokenAsync(laptop, SignOutRequest()));

        Assert.Equal(HttpStatusCode.Unauthorized, (await phone.GetAsync("/auth/me")).StatusCode);
    }

    [Fact]
    public async Task GivenATokenSignedWithTheRightKeyButForAnotherAudience_WhenAskingWhoIsSignedIn_ThenItRejects()
    {
        var account = await AddAccountAsync();
        var token = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler().CreateToken(
            new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Issuer = "nexo",
                Audience = "another-service",
                Claims = new Dictionary<string, object>
                {
                    ["sub"] = account.Id.ToString(),
                    ["orgId"] = account.OrganizationId.ToString(),
                    ["role"] = "Admin",
                    ["sv"] = 0,
                },
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(TestData.JwtKey)),
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256),
            });
        var request = new HttpRequestMessage(HttpMethod.Get, "/auth/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task GivenACookieSession_WhenAChangeIsSentWithoutTheAntiforgeryToken_ThenItIsRejected()
    {
        await AddAccountAsync();
        var browser = await SignedInBrowserAsync();

        var response = await browser.SendAsync(SignOutRequest());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/auth/me")).StatusCode);
    }

    [Fact]
    public async Task GivenATokenFetchedBeforeSigningIn_WhenAChangeIsSentAfterwards_ThenItIsRejected()
    {
        await AddAccountAsync();
        var browser = Browser();
        var stale = await AntiforgeryTokenAsync(browser);
        await SignInAsync(browser);

        var request = SignOutRequest();
        request.Headers.Add("X-XSRF-TOKEN", stale);
        var response = await browser.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GivenPlainHttpLikeLocalDevelopment_WhenFetchingTheAntiforgeryToken_ThenASecureCookieIsStillIssued()
    {
        var response = await factory.CreateClient().GetAsync("/auth/csrf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookie = Assert.Single(
            Microsoft.Net.Http.Headers.SetCookieHeaderValue.ParseList(response.Headers.GetValues("Set-Cookie").ToList()));
        Assert.True(cookie.Secure);
        Assert.Equal(Microsoft.Net.Http.Headers.SameSiteMode.None, cookie.SameSite);
    }

    /// <summary>A client that keeps cookies like a browser; HTTPS so it sends the <c>Secure</c> session cookie back.</summary>
    private HttpClient Browser() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    private async Task<HttpClient> SignedInBrowserAsync()
    {
        var browser = Browser();
        await SignInAsync(browser);
        return browser;
    }

    private static async Task SignInAsync(HttpClient browser)
    {
        var response = await browser.PostAsJsonAsync(
            "/auth/sign-in", new SignInDto { Email = "ana@example.com", Password = TestData.StrongPassword });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static HttpRequestMessage SignOutRequest() => new(HttpMethod.Post, "/auth/sign-out");

    private static async Task<string> AntiforgeryTokenAsync(HttpClient browser) =>
        (await browser.GetFromJsonAsync<AntiforgeryTokenDto>("/auth/csrf"))!.Token;

    private static async Task<HttpRequestMessage> WithAntiforgeryTokenAsync(HttpClient browser, HttpRequestMessage request)
    {
        request.Headers.Add("X-XSRF-TOKEN", await AntiforgeryTokenAsync(browser));
        return request;
    }

    private async Task<Account> AddAccountAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NexoDbContext>();
        var plan = await db.Plans.SingleAsync(p => p.Name == Plan.Free);
        var organization = new Organization { Name = "Local Club", PlanId = plan.Id };
        var account = new Account
        {
            Email = "ana@example.com",
            FirstName = "Ana",
            LastName = "Ribeiro",
            Role = Role.Admin,
            Status = AccountStatus.Active,
            OrganizationId = organization.Id,
        };
        account.PasswordHash = new PasswordHasher<Account>().HashPassword(account, TestData.StrongPassword);
        db.AddRange(organization, account);
        await db.SaveChangesAsync();
        return account;
    }
}
