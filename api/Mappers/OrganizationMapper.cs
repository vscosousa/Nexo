using Microsoft.AspNetCore.Identity;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Models;

namespace Nexo.Api.Mappers;

public static class OrganizationMapper
{
    public static Organization ToOrganization(RegisterOrganizationDto dto, Plan plan) => new()
    {
        Name = dto.OrganizationName!.Trim(),
        PlanId = plan.Id,
        Plan = plan,
    };

    public static Organization ToOrganization(RegisterOrganizationExternalDto dto, Plan plan) => new()
    {
        Name = dto.OrganizationName!.Trim(),
        PlanId = plan.Id,
        Plan = plan,
    };

    public static Account ToAdminAccount(
        RegisterOrganizationDto dto, Organization organization, PasswordHasher<Account> hasher)
    {
        var account = new Account
        {
            Email = Account.NormalizeEmail(dto.AdminEmail!),
            FirstName = dto.AdminFirstName!.Trim(),
            LastName = dto.AdminLastName!.Trim(),
            Role = Role.Admin,
            Status = AccountStatus.Active,
            OrganizationId = organization.Id,
        };
        account.PasswordHash = hasher.HashPassword(account, dto.Password!);
        return account;
    }

    /// <summary>An active admin with no password, who signs in only through the linked social login.</summary>
    public static Account ToExternalAdminAccount(
        RegisterOrganizationExternalDto dto, string normalizedEmail, Organization organization) => new()
        {
            Email = normalizedEmail,
            FirstName = dto.AdminFirstName!.Trim(),
            LastName = dto.AdminLastName!.Trim(),
            Role = Role.Admin,
            Status = AccountStatus.Active,
            OrganizationId = organization.Id,
        };

    public static OrganizationDto ToDto(Organization organization) => new()
    {
        Id = organization.Id,
        Name = organization.Name,
        Plan = organization.Plan!.Name,
    };
}
