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
    IExternalLoginRepository externalLogins,
    PasswordHasher<Account> hasher,
    ITokenService tokens,
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

        await using var transaction = await db.Database.BeginTransactionAsync();
        var organization = await LockOrganizationWithASeatAsync(account.OrganizationId);

        FieldRules.Password(errors, nameof(dto.Password), dto.Password, organization.Name, dto.FirstName, dto.LastName);
        if (errors.Count > 0)
            throw new Domain.Exceptions.ValidationException(errors);

        AccountMapper.ApplyActivation(account, dto, hasher);
        await db.SaveChangesOrConflictAsync(AlreadyActive);
        await transaction.CommitAsync();

        return AccountMapper.ToDto(account);
    }

    public async Task<(AccountDto Account, SessionDto Session)> ActivateExternal(
        ActivateAccountExternalDto dto, string email, string linkToken, string code, ExternalIdentity identity)
    {
        var errors = new Dictionary<string, string[]>();
        FieldRules.Text(errors, nameof(dto.FirstName), "First name", dto.FirstName, Account.NameMaxLength);
        FieldRules.Text(errors, nameof(dto.LastName), "Last name", dto.LastName, Account.NameMaxLength);
        if (errors.Count > 0)
            throw new Domain.Exceptions.ValidationException(errors);

        var account = await FindInvitedAccount(email, linkToken, code);
        if (!identity.EmailVerified || string.IsNullOrWhiteSpace(identity.Email)
            || Account.NormalizeEmail(identity.Email) != account.Email)
            throw new ForbiddenException("The social login's email does not match the invitation.");

        await using var transaction = await db.Database.BeginTransactionAsync();
        await LockOrganizationWithASeatAsync(account.OrganizationId);
        AccountMapper.ApplyExternalActivation(account, dto);
        externalLogins.Add(new ExternalLogin { AccountId = account.Id, Provider = identity.Provider, ProviderKey = identity.ProviderKey });
        await db.SaveChangesOrConflictAsync("This account is already active, or the social login is linked to another account.");
        await transaction.CommitAsync();

        return (AccountMapper.ToDto(account), tokens.GenerateToken(account));
    }

    /// <summary>
    /// Locks the organization's row for the rest of the current transaction, so concurrent activations count and
    /// save one after another, then checks it still has room for one more active member.
    /// </summary>
    private async Task<Organization> LockOrganizationWithASeatAsync(Guid organizationId)
    {
        await organizations.LockAsync(organizationId);
        var organization = await organizations.GetByIdAsync(organizationId)
            ?? throw new InvalidOperationException("The account's organization does not exist.");
        var active = await accounts.CountByOrganizationAndStatusAsync(organization.Id, AccountStatus.Active);
        if (active >= organization.Plan!.MemberLimit)
            throw new ConflictException("The organization's member limit has been reached.");
        return organization;
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
