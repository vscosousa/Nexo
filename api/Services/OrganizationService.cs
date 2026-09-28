using Microsoft.AspNetCore.Identity;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Exceptions;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Persistence;
using Nexo.Api.Infrastructure.Repositories;
using Nexo.Api.Mappers;

namespace Nexo.Api.Services;

public class OrganizationService(
    IAccountRepository accounts,
    IOrganizationRepository organizations,
    IPlanRepository plans,
    PasswordHasher<Account> hasher,
    NexoDbContext db) : IOrganizationService
{
    private const string EmailInUse = "An account already exists for this email.";

    public async Task<OrganizationDto> Register(RegisterOrganizationDto dto)
    {
        Validate(dto);

        var email = Account.NormalizeEmail(dto.AdminEmail!);
        if (await accounts.FindByEmailAsync(email) is not null)
            throw new ConflictException(EmailInUse);

        var plan = await plans.FindByNameAsync(Plan.Free)
            ?? throw new InvalidOperationException("The Free plan is not seeded.");
        var organization = OrganizationMapper.ToOrganization(dto, plan);
        organizations.Add(organization);
        accounts.Add(OrganizationMapper.ToAdminAccount(dto, organization, hasher));
        // A concurrent registration of the same email may have passed the lookup and saved first.
        await db.SaveChangesOrConflictAsync(EmailInUse);

        return OrganizationMapper.ToDto(organization);
    }

    private static void Validate(RegisterOrganizationDto dto)
    {
        var errors = new Dictionary<string, string[]>();
        FieldRules.Text(errors, nameof(dto.OrganizationName), "Organization name", dto.OrganizationName, Organization.NameMaxLength);
        FieldRules.Text(errors, nameof(dto.AdminName), "Admin name", dto.AdminName, Account.NameMaxLength);
        FieldRules.Email(errors, nameof(dto.AdminEmail), dto.AdminEmail);
        FieldRules.Password(errors, nameof(dto.Password), dto.Password, dto.OrganizationName, dto.AdminName);

        if (errors.Count > 0)
            throw new Domain.Exceptions.ValidationException(errors);
    }
}
