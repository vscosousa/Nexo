using Microsoft.AspNetCore.Identity;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Models;

namespace Nexo.Api.Mappers;

public static class AccountMapper
{
    public static Account ToInvitedAccount(InviteMemberDto dto, Guid organizationId, string invitationTokenHash) => new()
    {
        Email = Account.NormalizeEmail(dto.Email!),
        Role = Role.Member,
        Status = AccountStatus.Invited,
        OrganizationId = organizationId,
        InvitationTokenHash = invitationTokenHash,
    };

    /// <summary>Turns a tracked invited account into an active one with the given name and hashed password.</summary>
    public static void ApplyActivation(Account account, ActivateAccountDto dto, PasswordHasher<Account> hasher)
    {
        account.Name = dto.Name!.Trim();
        account.PasswordHash = hasher.HashPassword(account, dto.Password!);
        account.Status = AccountStatus.Active;
        account.InvitationTokenHash = null;
    }

    public static AccountDto ToDto(Account account) => new()
    {
        Id = account.Id,
        Email = account.Email,
        Name = account.Name,
        Role = account.Role,
        Status = account.Status,
        OrganizationId = account.OrganizationId,
    };
}
