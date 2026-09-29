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
    IExternalLoginRepository externalLogins,
    PasswordHasher<Account> hasher,
    ITokenService tokens,
    NexoDbContext db) : IOrganizationService
{
    private const string EmailInUse = "An account already exists for this email.";
    private const string ExternalInUse = "An account already uses this email or social login.";

    public async Task<OrganizationDto> Register(RegisterOrganizationDto dto)
    {
        var errors = OrganizationAndAdminErrors(dto.OrganizationName, dto.AdminFirstName, dto.AdminLastName, dto.PlanId);
        FieldRules.Email(errors, nameof(dto.AdminEmail), dto.AdminEmail);
        FieldRules.Password(errors, nameof(dto.Password), dto.Password, dto.OrganizationName, dto.AdminFirstName, dto.AdminLastName);
        ThrowIfAny(errors);
        var plan = await FindPlanAsync(dto.PlanId!.Value);

        if (await accounts.FindByEmailAsync(Account.NormalizeEmail(dto.AdminEmail!)) is not null)
            throw new ConflictException(EmailInUse);

        var organization = OrganizationMapper.ToOrganization(dto, plan);
        organizations.Add(organization);
        accounts.Add(OrganizationMapper.ToAdminAccount(dto, organization, hasher));
        await db.SaveChangesOrConflictAsync(EmailInUse);

        return OrganizationMapper.ToDto(organization);
    }

    public async Task<(OrganizationDto Organization, SessionDto Session)> RegisterExternal(
        RegisterOrganizationExternalDto dto, ExternalIdentity identity)
    {
        ThrowIfAny(OrganizationAndAdminErrors(dto.OrganizationName, dto.AdminFirstName, dto.AdminLastName, dto.PlanId));
        if (!identity.EmailVerified || string.IsNullOrWhiteSpace(identity.Email))
            throw new ForbiddenException("The social login did not provide a verified email.");
        var plan = await FindPlanAsync(dto.PlanId!.Value);

        var email = Account.NormalizeEmail(identity.Email);
        if (await accounts.FindByEmailAsync(email) is not null)
            throw new ConflictException(EmailInUse);

        var organization = OrganizationMapper.ToOrganization(dto, plan);
        var admin = OrganizationMapper.ToExternalAdminAccount(dto, email, organization);
        organizations.Add(organization);
        accounts.Add(admin);
        externalLogins.Add(new ExternalLogin { AccountId = admin.Id, Provider = identity.Provider, ProviderKey = identity.ProviderKey });
        await db.SaveChangesOrConflictAsync(ExternalInUse);

        return (OrganizationMapper.ToDto(organization), tokens.GenerateToken(admin));
    }

    private static Dictionary<string, string[]> OrganizationAndAdminErrors(
        string? organizationName, string? adminFirstName, string? adminLastName, Guid? planId)
    {
        var errors = new Dictionary<string, string[]>();
        FieldRules.Text(errors, nameof(RegisterOrganizationDto.OrganizationName), "Organization name", organizationName, Organization.NameMaxLength);
        FieldRules.Text(errors, nameof(RegisterOrganizationDto.AdminFirstName), "First name", adminFirstName, Account.NameMaxLength);
        FieldRules.Text(errors, nameof(RegisterOrganizationDto.AdminLastName), "Last name", adminLastName, Account.NameMaxLength);
        if (planId is null || planId == Guid.Empty)
            errors[nameof(RegisterOrganizationDto.PlanId)] = ["A plan is required."];
        return errors;
    }

    private static void ThrowIfAny(Dictionary<string, string[]> errors)
    {
        if (errors.Count > 0)
            throw new Domain.Exceptions.ValidationException(errors);
    }

    private async Task<Plan> FindPlanAsync(Guid planId) =>
        await plans.GetByIdAsync(planId)
        ?? throw new Domain.Exceptions.ValidationException(new Dictionary<string, string[]>
        {
            [nameof(RegisterOrganizationDto.PlanId)] = ["The chosen plan does not exist."],
        });
}
