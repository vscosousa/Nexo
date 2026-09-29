using Nexo.Api.Domain.Dtos;

namespace Nexo.Api.Services;

public interface IAccountInvitationService
{
    /// <summary>Registers the email as a pending account of the organization, with the requested role (<c>Member</c> or <c>Staff</c>), and emails it a one-time invitation token that expires after <see cref="InvitationTokens.Lifetime"/>. An email already pending in the organization is re-invited with a new token and the requested role.</summary>
    /// <param name="callerAccountId">The requesting account; null when the caller is unidentified.</param>
    /// <exception cref="Domain.Exceptions.ValidationException">The email is missing or malformed, or the role is missing or not <c>Member</c>/<c>Staff</c>.</exception>
    /// <exception cref="Domain.Exceptions.ForbiddenException">The caller is not an admin of the organization.</exception>
    /// <exception cref="Exception">The invitation email could not be sent; the pending account is removed again.</exception>
    /// <exception cref="Domain.Exceptions.ConflictException">
    /// The member limit is reached (active and pending accounts count toward it, except when re-inviting a pending
    /// email), or the email already has an account.
    /// </exception>
    Task<AccountDto> Invite(Guid organizationId, Guid? callerAccountId, InviteMemberDto dto);

    /// <summary>
    /// Re-sends a fresh code, for a person who lost the original or used up its wrong attempts. The link token and
    /// the expiry do not change, so an already-open activation page (which has the token in its URL) keeps working
    /// once the new code is entered, and resending cannot keep an invitation alive past its expiry. Silently does
    /// nothing when the email has no unexpired pending invitation, so this never reveals whether an email is
    /// registered anywhere.
    /// </summary>
    Task Resend(string email);
}
