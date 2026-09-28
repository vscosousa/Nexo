using Microsoft.Extensions.Options;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Exceptions;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Email;
using Nexo.Api.Infrastructure.Persistence;
using Nexo.Api.Infrastructure.Repositories;
using Nexo.Api.Mappers;

namespace Nexo.Api.Services;

public class AccountInvitationService(
    IAccountRepository accounts,
    IOrganizationRepository organizations,
    IEmailSender email,
    IOptions<EmailOptions> emailOptions,
    NexoDbContext db) : IAccountInvitationService
{
    private const string EmailInUse = "An account already exists for this email.";

    public async Task<AccountDto> Invite(Guid organizationId, Guid? callerAccountId, InviteMemberDto dto)
    {
        var errors = new Dictionary<string, string[]>();
        FieldRules.Email(errors, nameof(dto.Email), dto.Email);
        if (errors.Count > 0)
            throw new Domain.Exceptions.ValidationException(errors);

        var caller = callerAccountId is { } id ? await accounts.GetByIdAsync(id) : null;
        if (caller is not { Role: Role.Admin } || caller.OrganizationId != organizationId)
            throw new ForbiddenException("Only an admin of the organization can invite members.");

        var organization = await organizations.GetByIdAsync(organizationId)
            ?? throw new InvalidOperationException("The caller's organization does not exist.");
        var active = await accounts.CountByOrganizationAndStatusAsync(organizationId, AccountStatus.Active);
        if (active >= organization.Plan!.MemberLimit)
            throw new ConflictException("The organization's member limit has been reached.");

        if (await accounts.FindByEmailAsync(Account.NormalizeEmail(dto.Email!)) is not null)
            throw new ConflictException(EmailInUse);

        var (token, tokenHash) = InvitationTokens.Create();
        var account = AccountMapper.ToInvitedAccount(dto, organizationId, tokenHash);
        accounts.Add(account);
        // A concurrent invitation for the same email may have passed the lookup and saved first.
        await db.SaveChangesOrConflictAsync(EmailInUse);

        try
        {
            await email.SendAsync(InvitationEmail.Create(
                account.Email, organization.Name, caller.Name ?? caller.Email, emailOptions.Value.WebBaseUrl, token));
        }
        catch
        {
            // The token exists only in the email, so an invitation that was never delivered cannot be used and
            // would block a new one for the same address; remove it and let the caller retry.
            db.Accounts.Remove(account);
            await db.SaveChangesAsync();
            throw;
        }

        return AccountMapper.ToDto(account);
    }
}
