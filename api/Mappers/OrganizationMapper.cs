using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Models;

namespace Nexo.Api.Mappers;

public static class OrganizationMapper
{
    public static Organization ToOrganization(RegisterOrganizationDto dto, Plan plan) => new()
    {
        Name = dto.OrganizationName!,
        PlanId = plan.Id,
        Plan = plan,
    };

    public static Account ToAdminAccount(RegisterOrganizationDto dto, Organization organization) => new()
    {
        Email = Account.NormalizeEmail(dto.AdminEmail!),
        Name = dto.AdminName,
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
