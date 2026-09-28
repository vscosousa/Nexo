using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Models;
using Nexo.Api.Mappers;
using Xunit;

namespace Nexo.Api.Tests.Mappers;

public class OrganizationMapperTests
{
    private static readonly RegisterOrganizationDto Registration = new()
    {
        OrganizationName = "Local Club",
        AdminName = "Ana Admin",
        AdminEmail = "ana@example.com",
    };

    private static readonly Plan FreePlan = new() { Name = Plan.Free, MemberLimit = 20 };

    [Fact]
    public void GivenRegistrationDetailsAndAPlan_WhenMappedToOrganization_ThenItBelongsToThatPlan()
    {
        var organization = OrganizationMapper.ToOrganization(Registration, FreePlan);

        Assert.Equal("Local Club", organization.Name);
        Assert.Equal(FreePlan.Id, organization.PlanId);
        Assert.Same(FreePlan, organization.Plan);
    }

    [Fact]
    public void GivenRegistrationDetailsAndOrganization_WhenMappedToAccount_ThenItIsAnActiveAdminOfThatOrganization()
    {
        var organization = OrganizationMapper.ToOrganization(Registration, FreePlan);

        var account = OrganizationMapper.ToAdminAccount(Registration, organization);

        Assert.Equal("ana@example.com", account.Email);
        Assert.Equal("Ana Admin", account.Name);
        Assert.Equal(Role.Admin, account.Role);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Equal(organization.Id, account.OrganizationId);
    }

    [Fact]
    public void GivenAnEmailWithSurroundingSpacesAndUppercase_WhenMappedToAccount_ThenItIsStoredTrimmedAndLowercased()
    {
        var dto = new RegisterOrganizationDto
        {
            OrganizationName = "Local Club",
            AdminName = "Ana Admin",
            AdminEmail = "  Ana.Admin+Club@Example.COM ",
        };
        var organization = OrganizationMapper.ToOrganization(dto, FreePlan);

        var account = OrganizationMapper.ToAdminAccount(dto, organization);

        Assert.Equal("ana.admin+club@example.com", account.Email);
    }

    [Fact]
    public void GivenAnOrganization_WhenMappedToDto_ThenItCarriesIdNameAndPlan()
    {
        var organization = OrganizationMapper.ToOrganization(Registration, FreePlan);

        var dto = OrganizationMapper.ToDto(organization);

        Assert.Equal(organization.Id, dto.Id);
        Assert.Equal("Local Club", dto.Name);
        Assert.Equal("Free", dto.Plan);
    }
}
