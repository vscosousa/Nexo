using Microsoft.Net.Http.Headers;
using Nexo.Api.Controllers;
using Xunit;

namespace Nexo.Api.Tests.Infrastructure;

internal static class SessionCookie
{
    /// <summary>The session cookie the response sets; fails the test when there is none.</summary>
    public static SetCookieHeaderValue In(HttpResponseMessage response)
    {
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var values), "The response sets no cookie.");
        return Assert.Single(
            SetCookieHeaderValue.ParseList(values.ToList()),
            c => c.Name == AuthController.SessionCookie);
    }
}
