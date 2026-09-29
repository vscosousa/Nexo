using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Exceptions;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Email;
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
    IEmailSender email,
    IOptions<EmailOptions> emailOptions,
    NexoDbContext db) : IOrganizationService
{
    private const string EmailInUse = "An account already exists for this email.";
    private const string ExternalInUse = "An account already uses this email or social login.";

    public async Task Register(RegisterOrganizationDto dto)
    {
        var errors = OrganizationAndAdminErrors(dto.OrganizationName, dto.AdminFirstName, dto.AdminLastName, dto.PlanId);
        FieldRules.Email(errors, nameof(dto.AdminEmail), dto.AdminEmail);
        FieldRules.Password(errors, nameof(dto.Password), dto.Password, dto.OrganizationName, dto.AdminFirstName, dto.AdminLastName);
        ThrowIfAny(errors);
        var plan = await FindPlanAsync(dto.PlanId!.Value);

        var (token, tokenHash) = InvitationTokens.CreateLinkToken();
        var organization = OrganizationMapper.ToOrganization(dto, plan);
        var admin = OrganizationMapper.ToAdminAccount(
            dto, organization, hasher, tokenHash, DateTime.UtcNow + InvitationTokens.ConfirmationLifetime);
        var webBaseUrl = emailOptions.Value.WebBaseUrl;

        if (await accounts.FindByEmailAsync(admin.Email) is { } existing)
        {
            if (existing.Status != AccountStatus.Unverified || existing.InvitationExpiresAt > DateTime.UtcNow)
            {
                await email.SendAsync(AccountEmails.AlreadyRegistered(admin.Email, webBaseUrl));
                return;
            }
            await RemoveUnverifiedAsync(existing);
        }

        organizations.Add(organization);
        accounts.Add(admin);
        await db.SaveChangesOrConflictAsync(EmailInUse);

        try
        {
            await email.SendAsync(AccountEmails.Confirmation(admin.Email, organization.Name, webBaseUrl, token));
        }
        catch
        {
            db.Accounts.Remove(admin);
            db.Organizations.Remove(organization);
            await db.SaveChangesAsync();
            throw;
        }
    }

    public async Task<(OrganizationDto Organization, SessionDto Session)> RegisterExternal(
        RegisterOrganizationExternalDto dto, ExternalIdentity identity)
    {
        ThrowIfAny(OrganizationAndAdminErrors(dto.OrganizationName, dto.AdminFirstName, dto.AdminLastName, dto.PlanId));
        if (!identity.EmailVerified || string.IsNullOrWhiteSpace(identity.Email))
            throw new ForbiddenException("The social login did not provide a verified email.");
        var plan = await FindPlanAsync(dto.PlanId!.Value);

        var address = Account.NormalizeEmail(identity.Email);
        if (await accounts.FindByEmailAsync(address) is { } existing)
        {
            if (existing.Status != AccountStatus.Unverified)
                throw new ConflictException(EmailInUse);
            await RemoveUnverifiedAsync(existing);
        }

        var organization = OrganizationMapper.ToOrganization(dto, plan);
        var admin = OrganizationMapper.ToExternalAdminAccount(dto, address, organization);
        organizations.Add(organization);
        accounts.Add(admin);
        externalLogins.Add(new ExternalLogin { AccountId = admin.Id, Provider = identity.Provider, ProviderKey = identity.ProviderKey });
        await db.SaveChangesOrConflictAsync(ExternalInUse);

        try
        {
            await email.SendAsync(AccountEmails.Welcome(admin.Email, organization.Name, emailOptions.Value.WebBaseUrl));
        }
        catch
        {
        }

        return (OrganizationMapper.ToDto(organization), tokens.GenerateToken(admin));
    }

    /// <summary>
    /// Marks an unconfirmed registration (its admin and organization) for deletion in the next save. Nobody proved
    /// they own its email, so a registration that does prove it (a later one after the link expired, or a verified
    /// social login) takes its place.
    /// </summary>
    private async Task RemoveUnverifiedAsync(Account account)
    {
        db.Accounts.Remove(account);
        if (await organizations.GetByIdAsync(account.OrganizationId) is { } organization)
            db.Organizations.Remove(organization);
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
