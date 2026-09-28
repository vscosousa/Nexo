using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Models;

namespace Nexo.Api.Mappers;

public static class AccountMapper
{
    public static Account ToInvitedAccount(InviteMemberDto dto, Guid organizationId) => new()
    {
        Email = Account.NormalizeEmail(dto.Email!),
        Role = Role.Member,
        Status = AccountStatus.Invited,
        OrganizationId = organizationId,
    };

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
