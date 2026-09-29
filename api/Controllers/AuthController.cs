using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Exceptions;
using Nexo.Api.Services;

namespace Nexo.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IAuthService service, IAuthenticationSchemeProvider schemes, IConfiguration configuration)
    : ControllerBase
{
    /// <summary>The cookie scheme where provider handlers park the identity between the provider and our callback.</summary>
    public const string ExternalScheme = "External";

    /// <summary>Signs in with email and password.</summary>
    /// <response code="200">Credentials correct; the session token is returned.</response>
    /// <response code="400">The email or password is missing.</response>
    /// <response code="401">No active account matches; the same response for every cause.</response>
    [HttpPost("sign-in")]
    [ProducesResponseType<SessionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SignIn(SignInDto dto)
    {
        try
        {
            return Ok(await service.SignIn(dto));
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

    /// <summary>Starts sign-in with a social provider by redirecting the browser to it.</summary>
    /// <response code="302">Redirects to the provider.</response>
    /// <response code="404">The provider is unknown or not configured.</response>
    [HttpGet("external/{provider}")]
    public async Task<IActionResult> ExternalSignIn(string provider)
    {
        if (provider is not ("google" or "microsoft") || await schemes.GetSchemeAsync(provider) is null)
            return NotFound();
        var properties = new AuthenticationProperties { RedirectUri = "/auth/external/callback" };
        properties.SetParameter("prompt", "select_account");
        return Challenge(properties, provider);
    }

    /// <summary>Finishes social sign-in and sends the browser back to the web app with the session token in the URL fragment.</summary>
    /// <response code="302">Redirects to the web app: with a token on success, or to the sign-in page with an error.</response>
    [HttpGet("external/callback")]
    public async Task<IActionResult> ExternalCallback()
    {
        var web = configuration["Email:WebBaseUrl"]!.TrimEnd('/');
        var external = await HttpContext.AuthenticateAsync(ExternalScheme);
        await HttpContext.SignOutAsync(ExternalScheme);
        var id = external.Principal?.FindFirst(ClaimTypes.NameIdentifier);
        if (!external.Succeeded || id is null)
            return Redirect($"{web}/login?error=oauth");

        try
        {
            var session = await service.SignInExternal(
                id.Issuer,
                id.Value,
                external.Principal!.FindFirstValue(ClaimTypes.Email),
                string.Equals(external.Principal.FindFirstValue("email_verified"), "true", StringComparison.OrdinalIgnoreCase));
            return Redirect($"{web}/login/callback#token={session.Token}");
        }
        catch (Exception e) when (e is UnauthorizedException or ConflictException)
        {
            return Redirect($"{web}/login?error=oauth");
        }
    }
}
