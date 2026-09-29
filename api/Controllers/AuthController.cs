using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Exceptions;
using Nexo.Api.Infrastructure.Email;
using Nexo.Api.Services;

namespace Nexo.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(
    IAuthService service,
    IOrganizationService organizations,
    IAccountActivationService activations,
    IAuthenticationSchemeProvider schemes,
    IOptions<EmailOptions> emailOptions,
    IAntiforgery antiforgery)
    : ControllerBase
{
    /// <summary>The cookie scheme where provider handlers park the identity between the provider and our callback.</summary>
    public const string ExternalScheme = "External";

    /// <summary>The httpOnly cookie that carries the session token.</summary>
    public const string SessionCookie = "nexo_session";

    /// <summary>Signs in with email and password, setting the session cookie.</summary>
    /// <response code="204">Credentials correct; the httpOnly session cookie is set.</response>
    /// <response code="400">The email or password is missing.</response>
    /// <response code="401">No active account matches; the same response for every cause.</response>
    /// <response code="429">Too many attempts from this address; the limit is <c>RateLimit:SignInPermitLimit</c> per minute (default 10).</response>
    [HttpPost("sign-in")]
    [EnableRateLimiting("sign-in")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SignIn(SignInDto dto)
    {
        try
        {
            AppendSessionCookie(await service.SignIn(dto));
            return NoContent();
        }
        catch (ValidationException e)
        {
            return ValidationProblem(new ValidationProblemDetails(e.Errors));
        }
        catch (UnauthorizedException e)
        {
            return Problem(e.Message, statusCode: StatusCodes.Status401Unauthorized);
        }
    }

    /// <summary>Returns the account the session cookie (or bearer token) belongs to.</summary>
    /// <response code="200">Signed in.</response>
    /// <response code="401">No valid session.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<CurrentAccountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me() => Ok(new CurrentAccountDto
    {
        Id = Guid.Parse(User.FindFirstValue("sub")!),
        OrganizationId = Guid.Parse(User.FindFirstValue("orgId")!),
        Role = User.FindFirstValue("role")!,
    });

    /// <summary>
    /// Signs out: clears the session cookie and ends every session of the account, on every device, so a copied or
    /// stolen session token stops working too.
    /// </summary>
    /// <response code="204">The cookie is cleared, whether or not a session existed.</response>
    [HttpPost("sign-out")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> EndSession()
    {
        if (Guid.TryParse(User.FindFirstValue("sub"), out var accountId))
            await service.EndSessions(accountId);
        Response.Cookies.Delete(SessionCookie, SessionCookieOptions(null));
        return NoContent();
    }

    /// <summary>Unlocks an account locked by too many wrong passwords, with the token from the emailed unlock link.</summary>
    /// <response code="200">Unlocked; the owner can sign in again.</response>
    /// <response code="403">No locked account matches the email and token; the same response for every cause.</response>
    /// <response code="429">Too many requests from this address; the limit is <c>RateLimit:PublicPermitLimit</c> per minute (default 10).</response>
    [HttpPost("unlock")]
    [EnableRateLimiting("public")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Unlock(UnlockAccountDto dto)
    {
        try
        {
            await service.Unlock(dto.Email ?? "", dto.Token ?? "");
            return Ok();
        }
        catch (ForbiddenException e)
        {
            return Problem(e.Message, statusCode: StatusCodes.Status403Forbidden);
        }
    }

    /// <summary>
    /// Issues the anti-forgery token that changes made with the session cookie must carry in the <c>X-XSRF-TOKEN</c> header.
    /// The token is tied to the current session, so fetch a new one after signing in or out.
    /// </summary>
    /// <response code="200">The request token; its paired cookie is set alongside.</response>
    [HttpGet("csrf")]
    [ProducesResponseType<AntiforgeryTokenDto>(StatusCodes.Status200OK)]
    public IActionResult Csrf() =>
        Ok(new AntiforgeryTokenDto { Token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken! });

    /// <summary>
    /// Starts sign-in with a social provider by redirecting the browser to it. With <paramref name="intent"/>
    /// <c>register</c> (plus <paramref name="planId"/> and <paramref name="organizationName"/>) or <c>activate</c>
    /// (plus the invitation's <paramref name="email"/>, <paramref name="token"/>, and <paramref name="code"/>), the
    /// callback keeps the provider identity pending instead of signing in, for the web form to confirm.
    /// </summary>
    /// <response code="302">Redirects to the provider.</response>
    /// <response code="404">The provider is unknown or not configured.</response>
    [HttpGet("external/{provider}")]
    public async Task<IActionResult> ExternalSignIn(
        string provider,
        string? intent = null,
        Guid? planId = null,
        string? organizationName = null,
        string? email = null,
        string? token = null,
        string? code = null)
    {
        if (provider is not ("google" or "microsoft") || await schemes.GetSchemeAsync(provider) is null)
            return NotFound();
        var properties = new AuthenticationProperties { RedirectUri = "/auth/external/callback" };
        properties.SetParameter("prompt", "select_account");
        if (intent == RegisterIntent && planId is { } plan)
        {
            properties.Items[IntentItem] = RegisterIntent;
            properties.Items["planId"] = plan.ToString();
            properties.Items["organizationName"] = organizationName;
        }
        else if (intent == ActivateIntent && !string.IsNullOrWhiteSpace(email)
            && !string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(code))
        {
            properties.Items[IntentItem] = ActivateIntent;
            properties.Items["email"] = email;
            properties.Items["token"] = token;
            properties.Items["code"] = code;
        }
        return Challenge(properties, provider);
    }

    /// <summary>
    /// Finishes social sign-in. Plain sign-in sets the session cookie and sends the browser to the web app's callback page.
    /// A registration or activation keeps the provider identity in the short-lived external cookie and sends the browser
    /// back to the form it started from, with <c>external=&lt;provider&gt;</c>, or <c>error=oauth</c> when the provider
    /// did not attest a verified email.
    /// </summary>
    /// <response code="302">Redirects to the web app.</response>
    [HttpGet("external/callback")]
    public async Task<IActionResult> ExternalCallback()
    {
        var web = emailOptions.Value.WebBaseUrl.TrimEnd('/');
        var external = await HttpContext.AuthenticateAsync(ExternalScheme);
        var identity = ReadIdentity(external);
        var items = external.Properties?.Items ?? new Dictionary<string, string?>();
        var intent = external.Succeeded ? Item(items, IntentItem) : null;

        if (intent is RegisterIntent or ActivateIntent)
        {
            var back = intent == RegisterIntent
                ? $"{web}/register/organization?planId={Uri.EscapeDataString(items["planId"]!)}"
                : $"{web}/activate?email={Uri.EscapeDataString(items["email"]!)}&token={Uri.EscapeDataString(items["token"]!)}";
            if (identity is not { EmailVerified: true, Email: not null })
            {
                await HttpContext.SignOutAsync(ExternalScheme);
                return Redirect($"{back}&error=oauth");
            }
            return Redirect($"{back}&external={Uri.EscapeDataString(identity.Provider)}");
        }

        await HttpContext.SignOutAsync(ExternalScheme);
        if (identity is null)
            return Redirect($"{web}/login?error=oauth");
        try
        {
            AppendSessionCookie(await service.SignInExternal(
                identity.Provider, identity.ProviderKey, identity.Email, identity.EmailVerified));
            return Redirect($"{web}/login/callback");
        }
        catch (Exception e) when (e is UnauthorizedException or ConflictException)
        {
            return Redirect($"{web}/login?error=oauth");
        }
    }

    /// <summary>Returns what the provider reported for the registration or activation waiting to be confirmed.</summary>
    /// <response code="200">A verified provider identity is pending.</response>
    /// <response code="401">Nothing is pending, or it expired (after 5 minutes); start with the provider again.</response>
    [HttpGet("external/pending")]
    [ProducesResponseType<PendingExternalDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PendingExternal()
    {
        var pending = await PendingAsync();
        if (pending is null)
            return Problem(NothingPending, statusCode: StatusCodes.Status401Unauthorized);
        return Ok(new PendingExternalDto
        {
            Intent = pending.Intent,
            Email = pending.Identity.Email!,
            FirstName = pending.Principal.FindFirstValue(ClaimTypes.GivenName),
            LastName = pending.Principal.FindFirstValue(ClaimTypes.Surname),
            OrganizationName = Item(pending.Items, "organizationName"),
        });
    }

    /// <summary>
    /// Registers an organization whose admin signs in with the pending provider identity (no password), then signs the admin in.
    /// The admin's email is the provider's verified email; the names are the ones confirmed on the form.
    /// </summary>
    /// <response code="201">Created; the session cookie is set.</response>
    /// <response code="400">A field is missing or invalid, the plan does not exist, or the anti-forgery token is missing or invalid.</response>
    /// <response code="401">No registration is pending with a provider.</response>
    /// <response code="409">The email or the provider identity already belongs to an account.</response>
    [HttpPost("external/register")]
    [EnableRateLimiting("public")]
    [ProducesResponseType<OrganizationDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterExternal(RegisterOrganizationExternalDto dto)
    {
        if (!await antiforgery.IsRequestValidAsync(HttpContext))
            return Problem(AntiforgeryInvalid, statusCode: StatusCodes.Status400BadRequest);
        if (await PendingAsync() is not { Intent: RegisterIntent } pending)
            return Problem(NothingPending, statusCode: StatusCodes.Status401Unauthorized);

        return await CompletePendingAsync(async () =>
        {
            var (organization, session) = await organizations.RegisterExternal(dto, pending.Identity);
            return (Created($"/organizations/{organization.Id}", organization), session);
        });
    }

    /// <summary>
    /// Activates the invited account the pending provider identity was started for (no password), linking the identity,
    /// then signs the member in. The provider's verified email must be the invited email.
    /// </summary>
    /// <response code="200">Activated; the session cookie is set.</response>
    /// <response code="400">A name is missing or invalid, or the anti-forgery token is missing or invalid.</response>
    /// <response code="401">No activation is pending with a provider.</response>
    /// <response code="403">The invitation is not valid, or the provider's email is not the invited one.</response>
    /// <response code="409">The account is already active, the member limit is reached, or the identity is linked to another account.</response>
    [HttpPost("external/activate")]
    [EnableRateLimiting("public")]
    [ProducesResponseType<AccountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ActivateExternal(ActivateAccountExternalDto dto)
    {
        if (!await antiforgery.IsRequestValidAsync(HttpContext))
            return Problem(AntiforgeryInvalid, statusCode: StatusCodes.Status400BadRequest);
        if (await PendingAsync() is not { Intent: ActivateIntent } pending)
            return Problem(NothingPending, statusCode: StatusCodes.Status401Unauthorized);

        return await CompletePendingAsync(async () =>
        {
            var (account, session) = await activations.ActivateExternal(
                dto, pending.Items["email"]!, pending.Items["token"]!, pending.Items["code"]!, pending.Identity);
            return (Ok(account), session);
        });
    }

    private const string IntentItem = "intent";
    private const string RegisterIntent = "register";
    private const string ActivateIntent = "activate";
    private const string NothingPending = "No social login is waiting to be confirmed, or it expired; start again.";
    private const string AntiforgeryInvalid = "The anti-forgery token is missing or invalid.";

    private sealed record Pending(
        string Intent, ExternalIdentity Identity, ClaimsPrincipal Principal, IDictionary<string, string?> Items);

    private static string? Item(IDictionary<string, string?>? items, string key) =>
        items is not null && items.TryGetValue(key, out var value) ? value : null;

    private static ExternalIdentity? ReadIdentity(AuthenticateResult external)
    {
        var id = external.Principal?.FindFirst(ClaimTypes.NameIdentifier);
        if (!external.Succeeded || id is null)
            return null;
        return new ExternalIdentity(
            id.Issuer,
            id.Value,
            external.Principal!.FindFirstValue(ClaimTypes.Email),
            string.Equals(external.Principal.FindFirstValue("email_verified"), "true", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The verified provider identity parked by the callback for a registration or activation, if any.</summary>
    private async Task<Pending?> PendingAsync()
    {
        var external = await HttpContext.AuthenticateAsync(ExternalScheme);
        var identity = ReadIdentity(external);
        var items = external.Properties?.Items;
        var intent = Item(items, IntentItem);
        if (identity is not { EmailVerified: true, Email: not null } || intent is not (RegisterIntent or ActivateIntent))
            return null;
        return new Pending(intent, identity, external.Principal!, items!);
    }

    /// <summary>Runs the registration or activation, then swaps the external cookie for the session cookie.</summary>
    private async Task<IActionResult> CompletePendingAsync(Func<Task<(IActionResult Result, SessionDto Session)>> complete)
    {
        try
        {
            var (result, session) = await complete();
            await HttpContext.SignOutAsync(ExternalScheme);
            AppendSessionCookie(session);
            return result;
        }
        catch (ValidationException e)
        {
            return ValidationProblem(new ValidationProblemDetails(e.Errors));
        }
        catch (ForbiddenException e)
        {
            return Problem(e.Message, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (ConflictException e)
        {
            return Problem(e.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }

    private void AppendSessionCookie(SessionDto session) =>
        Response.Cookies.Append(SessionCookie, session.Token, SessionCookieOptions(session.ExpiresAt));

    /// <summary><c>SameSite=None</c> (which requires <c>Secure</c>) so the cookie also works when the web app is served from another site.</summary>
    private static CookieOptions SessionCookieOptions(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.None,
        Path = "/",
        Expires = expires,
    };
}
