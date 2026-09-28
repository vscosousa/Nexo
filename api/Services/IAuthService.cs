using Nexo.Api.Domain.Dtos;

namespace Nexo.Api.Services;

public interface IAuthService
{
    /// <summary>Signs in with email and password, issuing a session.</summary>
    /// <exception cref="Domain.Exceptions.ValidationException">The email or password is missing.</exception>
    /// <exception cref="Domain.Exceptions.UnauthorizedException">The credentials do not match an active account; the reason is deliberately not revealed.</exception>
    Task<SessionDto> SignIn(SignInDto dto);

    /// <summary>Signs in with a verified social login, linking the provider identity to the matching account on first use.</summary>
    /// <exception cref="Domain.Exceptions.UnauthorizedException">The email is unverified or missing, or no active account matches; no account is ever created.</exception>
    /// <exception cref="Domain.Exceptions.ConflictException">The identity was linked concurrently; signing in again succeeds.</exception>
    Task<SessionDto> SignInExternal(string provider, string providerKey, string? email, bool emailVerified);
}
