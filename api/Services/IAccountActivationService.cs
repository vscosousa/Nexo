using Nexo.Api.Domain.Dtos;

namespace Nexo.Api.Services;

public interface IAccountActivationService
{
    /// <summary>Checks the email, link token, and invitation code without activating anything, so the frontend can gate the account form on it.</summary>
    /// <exception cref="Domain.Exceptions.ForbiddenException">No account is registered for the email, or the link token or code is wrong or expired.</exception>
    /// <exception cref="Domain.Exceptions.ConflictException">The account is already active.</exception>
    Task VerifyInvitation(string email, string linkToken, string code);

    /// <summary>Activates the invited account for the email, setting its name and password.</summary>
    /// <exception cref="Domain.Exceptions.ValidationException">A field is missing or the email is malformed.</exception>
    /// <exception cref="Domain.Exceptions.ForbiddenException">No account is registered for the email, or the link token or code is wrong or expired.</exception>
    /// <exception cref="Domain.Exceptions.ConflictException">The account is already active or the organization's member limit is reached.</exception>
    Task<AccountDto> Activate(ActivateAccountDto dto);
}
