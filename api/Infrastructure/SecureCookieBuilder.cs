namespace Nexo.Api.Infrastructure;

/// <summary>
/// Builds cookies that are always <c>Secure</c> (so <c>SameSite=None</c> is accepted) without the HTTPS-only
/// check that <see cref="CookieSecurePolicy.Always"/> triggers in antiforgery. Browsers treat
/// <c>http://localhost</c> as secure, so local development over plain HTTP keeps working.
/// </summary>
public sealed class SecureCookieBuilder : CookieBuilder
{
    public override CookieOptions Build(HttpContext context, DateTimeOffset expiresFrom)
    {
        var options = base.Build(context, expiresFrom);
        options.Secure = true;
        return options;
    }
}
