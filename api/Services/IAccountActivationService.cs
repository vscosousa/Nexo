using Nexo.Api.Domain.Dtos;

namespace Nexo.Api.Services;

public interface IAccountActivationService
{
    /// <summary>
    /// Checks the email, link token, and invitation code without activating anything, so the frontend can gate the account
    /// form on it. A wrong code with the right link token counts toward <see cref="InvitationTokens.MaxCodeAttempts"/>.
    /// </summary>
    /// <exception cref="Domain.Exceptions.ForbiddenException">No pending invitation matches: the email, link token, or code is wrong, expired, or used up (also for an already active account).</exception>
    Task VerifyInvitation(string email, string linkToken, string code);

    /// <summary>Activates the invited account for the email, setting its name and password.</summary>
    /// <exception cref="Domain.Exceptions.ValidationException">A field is missing or the email is malformed.</exception>
    /// <exception cref="Domain.Exceptions.ForbiddenException">No pending invitation matches: the email, link token, or code is wrong, expired, or used up.</exception>
    /// <exception cref="Domain.Exceptions.ConflictException">The organization's member limit is reached, or the account was activated concurrently.</exception>
    Task<AccountDto> Activate(ActivateAccountDto dto);

    /// <summary>
    /// Activates the invited account with the given name and no password, linking the social login, whose verified
    /// email must be the invited one. Returns the account and its session.
    /// </summary>
    /// <exception cref="Domain.Exceptions.ValidationException">A name is missing or too long.</exception>
    /// <exception cref="Domain.Exceptions.ForbiddenException">The invitation does not match, or the social login's verified email is not the invited one.</exception>
    /// <exception cref="Domain.Exceptions.ConflictException">The member limit is reached, the account was activated concurrently, or the social login is linked to another account.</exception>
    Task<(AccountDto Account, SessionDto Session)> ActivateExternal(
        ActivateAccountExternalDto dto, string email, string linkToken, string code, ExternalIdentity identity);

    /// <summary>Makes a newly registered admin's account active, given the token from the emailed confirmation link.</summary>
    /// <exception cref="Domain.Exceptions.ForbiddenException">No unconfirmed registration matches the email and token, or the link expired.</exception>
    Task ConfirmEmail(string email, string token);
}
