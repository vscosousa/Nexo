using Microsoft.AspNetCore.Identity;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Models;

namespace Nexo.Api.Mappers;

public static class AccountMapper
{
    public static Account ToInvitedAccount(
        InviteMemberDto dto, Guid organizationId, string invitationTokenHash, string invitationCodeHash, DateTime invitationExpiresAt) => new()
        {
            Email = Account.NormalizeEmail(dto.Email!),
            Role = Role.Member,
            Status = AccountStatus.Invited,
            OrganizationId = organizationId,
            InvitationTokenHash = invitationTokenHash,
            InvitationCodeHash = invitationCodeHash,
            InvitationExpiresAt = invitationExpiresAt,
        };

    /// <summary>Turns a tracked invited account into an active one with the given name and hashed password.</summary>
    public static void ApplyActivation(Account account, ActivateAccountDto dto, PasswordHasher<Account> hasher)
    {
        account.FirstName = dto.FirstName!.Trim();
        account.LastName = dto.LastName!.Trim();
        account.PasswordHash = hasher.HashPassword(account, dto.Password!);
        account.Status = AccountStatus.Active;
        account.InvitationTokenHash = null;
        account.InvitationCodeHash = null;
        account.InvitationExpiresAt = null;
    }

    public static AccountDto ToDto(Account account) => new()
    {
        Id = account.Id,
        Email = account.Email,
        FirstName = account.FirstName,
        LastName = account.LastName,
        Role = account.Role,
        Status = account.Status,
        OrganizationId = account.OrganizationId,
    };
}
