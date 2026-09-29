using System.Net;
using System.Net.Http.Json;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Tests.Infrastructure;
using Xunit;

namespace Nexo.Api.Tests.Controllers;

/// <summary>CORS and rate-limiting acceptance tests, run against PostgreSQL.</summary>
public class HttpSecurityEndpointTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    private const string AllowedOrigin = "http://localhost:5173";

    private HttpClient CreateClient(int signInLimit = 1000) =>
        factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("Cors:AllowedOrigins:0", AllowedOrigin);
            b.UseSetting("RateLimit:SignInPermitLimit", signInLimit.ToString());
        }).CreateClient();

    private static HttpRequestMessage Preflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/plans");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        return request;
    }

    [Fact]
    public async Task GivenAnAllowedOrigin_WhenItSendsAPreflight_ThenItIsAllowed()
    {
        var response = await CreateClient().SendAsync(Preflight(AllowedOrigin));

        Assert.Equal(AllowedOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Equal("true", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Credentials")));
    }

    [Fact]
    public async Task GivenAnUnknownOrigin_WhenItSendsAPreflight_ThenItIsNotAllowed()
    {
        var response = await CreateClient().SendAsync(Preflight("https://evil.example"));

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task GivenTooManySignInAttempts_WhenTheLimitIsExceeded_ThenItRespondsTooManyRequests()
    {
        var client = CreateClient(signInLimit: 3);
        var attempt = new SignInDto { Email = "nobody@example.com", Password = "Wr0ng!Passw0rd" };

        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/auth/sign-in", attempt)).StatusCode);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/auth/sign-in", attempt)).StatusCode);
    }

}
