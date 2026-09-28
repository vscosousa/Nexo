using Microsoft.AspNetCore.Identity;
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
        AdminFirstName = "Ana",
        AdminLastName = "Admin",
        AdminEmail = "ana@example.com",
        Password = "Str0ng!Passw0rd",
    };

    private static readonly PasswordHasher<Account> Hasher = new();

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

        var account = OrganizationMapper.ToAdminAccount(Registration, organization, Hasher);

        Assert.Equal("ana@example.com", account.Email);
        Assert.Equal("Ana", account.FirstName);
        Assert.Equal("Admin", account.LastName);
        Assert.Equal(Role.Admin, account.Role);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Equal(organization.Id, account.OrganizationId);
        Assert.Equal(
            PasswordVerificationResult.Success,
            Hasher.VerifyHashedPassword(account, account.PasswordHash!, "Str0ng!Passw0rd"));
    }

    [Fact]
    public void GivenNamesWithSurroundingSpaces_WhenMapped_ThenTheyAreStoredTrimmed()
    {
        var dto = new RegisterOrganizationDto
        {
            OrganizationName = "  Local Club ",
            AdminFirstName = " Ana ",
            AdminLastName = " Admin  ",
            AdminEmail = "ana@example.com",
            Password = "Str0ng!Passw0rd",
        };

        var organization = OrganizationMapper.ToOrganization(dto, FreePlan);
        var account = OrganizationMapper.ToAdminAccount(dto, organization, Hasher);

        Assert.Equal("Local Club", organization.Name);
        Assert.Equal("Ana", account.FirstName);
        Assert.Equal("Admin", account.LastName);
    }

    [Fact]
    public void GivenAnEmailWithSurroundingSpacesAndUppercase_WhenMappedToAccount_ThenItIsStoredTrimmedAndLowercased()
    {
        var dto = new RegisterOrganizationDto
        {
            OrganizationName = "Local Club",
            AdminFirstName = "Ana",
            AdminLastName = "Admin",
            AdminEmail = "  Ana.Admin+Club@Example.COM ",
            Password = "Str0ng!Passw0rd",
        };
        var organization = OrganizationMapper.ToOrganization(dto, FreePlan);

        var account = OrganizationMapper.ToAdminAccount(dto, organization, Hasher);

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
