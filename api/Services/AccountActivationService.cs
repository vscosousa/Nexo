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
    private const string InvalidInvitation = "The email, link, or invitation code is not valid.";

    public async Task VerifyInvitation(string email, string linkToken, string code) =>
        await FindInvitedAccount(email, linkToken, code);

    public async Task<AccountDto> Activate(ActivateAccountDto dto)
    {
        var errors = new Dictionary<string, string[]>();
        FieldRules.Email(errors, nameof(dto.Email), dto.Email);
        FieldRules.Text(errors, nameof(dto.FirstName), "First name", dto.FirstName, Account.NameMaxLength);
        FieldRules.Text(errors, nameof(dto.LastName), "Last name", dto.LastName, Account.NameMaxLength);
        if (string.IsNullOrEmpty(dto.Password))
            errors[nameof(dto.Password)] = ["A password is required."];
        else if (dto.Password.Length > PasswordPolicy.MaxLength)
            errors[nameof(dto.Password)] = [$"The password must be at most {PasswordPolicy.MaxLength} characters."];
        if (string.IsNullOrWhiteSpace(dto.LinkToken))
            errors[nameof(dto.LinkToken)] = ["An invitation link is required."];
        if (string.IsNullOrWhiteSpace(dto.Code))
            errors[nameof(dto.Code)] = ["An invitation code is required."];
        if (errors.Count > 0)
            throw new Domain.Exceptions.ValidationException(errors);

        var account = await FindInvitedAccount(dto.Email!, dto.LinkToken!, dto.Code!);

        var organization = await organizations.GetByIdAsync(account.OrganizationId)
            ?? throw new InvalidOperationException("The account's organization does not exist.");
        var active = await accounts.CountByOrganizationAndStatusAsync(organization.Id, AccountStatus.Active);
        if (active >= organization.Plan!.MemberLimit)
            throw new ConflictException("The organization's member limit has been reached.");

        FieldRules.Password(errors, nameof(dto.Password), dto.Password, organization.Name, dto.FirstName, dto.LastName);
        if (errors.Count > 0)
            throw new Domain.Exceptions.ValidationException(errors);

        AccountMapper.ApplyActivation(account, dto, hasher);
        // A concurrent activation of the same account may have committed after the status check.
        await db.SaveChangesOrConflictAsync(AlreadyActive);

        return AccountMapper.ToDto(account);
    }

    /// <summary>
    /// Looks up the pending account for the email and checks both the link token and the code against it,
    /// without mutating anything. Both are required: the link token alone (it sits in a URL, which can leak
    /// through browser history or a forwarded link) is not enough to prove the person actually has the email.
    /// </summary>
    private async Task<Account> FindInvitedAccount(string email, string linkToken, string code)
    {
        var account = await accounts.FindByEmailAsync(Account.NormalizeEmail(email))
            ?? throw new ForbiddenException(InvalidInvitation);
        if (account.Status == AccountStatus.Active)
            throw new ConflictException(AlreadyActive);
        if (!InvitationTokens.TokenMatches(linkToken, account.InvitationTokenHash)
            || !InvitationTokens.CodeMatches(code, account.InvitationCodeHash)
            || account.InvitationExpiresAt is not { } expiresAt || expiresAt <= DateTime.UtcNow)
            throw new ForbiddenException(InvalidInvitation);
        return account;
    }
}
