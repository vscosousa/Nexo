using Microsoft.AspNetCore.Identity;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Exceptions;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Persistence;
using Nexo.Api.Infrastructure.Repositories;
using Nexo.Api.Mappers;

namespace Nexo.Api.Services;

public class AccountActivationService(
    IAccountRepository accounts,
    IOrganizationRepository organizations,
    PasswordHasher<Account> hasher,
    NexoDbContext db) : IAccountActivationService
{
    private const string AlreadyActive = "This account is already active.";

    public async Task<AccountDto> Activate(ActivateAccountDto dto)
    {
        var errors = new Dictionary<string, string[]>();
        FieldRules.Email(errors, nameof(dto.Email), dto.Email);
        FieldRules.Text(errors, nameof(dto.Name), "Name", dto.Name, Account.NameMaxLength);
        if (string.IsNullOrEmpty(dto.Password))
            errors[nameof(dto.Password)] = ["A password is required."];
        else if (dto.Password.Length > PasswordPolicy.MaxLength)
            errors[nameof(dto.Password)] = [$"The password must be at most {PasswordPolicy.MaxLength} characters."];
        if (string.IsNullOrWhiteSpace(dto.InvitationToken))
            errors[nameof(dto.InvitationToken)] = ["An invitation token is required."];
        if (errors.Count > 0)
            throw new Domain.Exceptions.ValidationException(errors);

        var account = await accounts.FindByEmailAsync(Account.NormalizeEmail(dto.Email!))
            ?? throw new ForbiddenException("The email or invitation token is not valid.");
        if (account.Status == AccountStatus.Active)
            throw new ConflictException(AlreadyActive);
        if (!InvitationTokens.Matches(dto.InvitationToken!, account.InvitationTokenHash))
            throw new ForbiddenException("The email or invitation token is not valid.");

        var organization = await organizations.GetByIdAsync(account.OrganizationId)
            ?? throw new InvalidOperationException("The account's organization does not exist.");
        var active = await accounts.CountByOrganizationAndStatusAsync(organization.Id, AccountStatus.Active);
        if (active >= organization.Plan!.MemberLimit)
            throw new ConflictException("The organization's member limit has been reached.");

        FieldRules.Password(errors, nameof(dto.Password), dto.Password, organization.Name, dto.Name);
        if (errors.Count > 0)
            throw new Domain.Exceptions.ValidationException(errors);

        AccountMapper.ApplyActivation(account, dto, hasher);
        // A concurrent activation of the same account may have committed after the status check.
        await db.SaveChangesOrConflictAsync(AlreadyActive);

        return AccountMapper.ToDto(account);
    }
}
