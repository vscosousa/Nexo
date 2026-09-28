using Nexo.Api.Domain.Dtos;

namespace Nexo.Api.Services;

public interface IAccountInvitationService
{
    /// <summary>Registers the email as a pending member account of the organization and emails it a one-time invitation token.</summary>
    /// <param name="callerAccountId">The requesting account; null when the caller is unidentified.</param>
    /// <exception cref="Domain.Exceptions.ValidationException">The email is missing or malformed.</exception>
    /// <exception cref="Domain.Exceptions.ForbiddenException">The caller is not an admin of the organization.</exception>
    /// <exception cref="Exception">The invitation email could not be sent; the pending account is removed again.</exception>
    /// <exception cref="Domain.Exceptions.ConflictException">The active-member limit is reached or the email already has an account.</exception>
    Task<AccountDto> Invite(Guid organizationId, Guid? callerAccountId, InviteMemberDto dto);
}
