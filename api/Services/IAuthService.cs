using Nexo.Api.Domain.Dtos;

namespace Nexo.Api.Services;

public interface IAuthService
{
    /// <summary>
    /// Signs in with email and password, issuing a session. A wrong password counts toward
    /// <see cref="AuthService.MaxSignInAttempts"/>; reaching it locks the account and emails the owner an unlock link.
    /// </summary>
    /// <exception cref="Domain.Exceptions.ValidationException">The email or password is missing.</exception>
    /// <exception cref="Domain.Exceptions.UnauthorizedException">The credentials do not match an active, unlocked account; the reason (including a lock) is deliberately not revealed.</exception>
    Task<SessionDto> SignIn(SignInDto dto);

    /// <summary>Signs in with a verified social login, linking the provider identity to the matching account on first use.</summary>
    /// <exception cref="Domain.Exceptions.UnauthorizedException">The email is unverified or missing, or no active, unlocked account matches; no account is ever created.</exception>
    /// <exception cref="Domain.Exceptions.ConflictException">The identity was linked concurrently; signing in again succeeds.</exception>
    Task<SessionDto> SignInExternal(string provider, string providerKey, string? email, bool emailVerified);

    /// <summary>Unlocks an account locked by too many wrong passwords, given the token from the emailed unlock link.</summary>
    /// <exception cref="Domain.Exceptions.ForbiddenException">No locked account matches the email and token.</exception>
    Task Unlock(string email, string token);

    /// <summary>Ends every session of the account, on every device, by bumping the session version its tokens carry.</summary>
    Task EndSessions(Guid accountId);
}
