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

        var existing = await accounts.FindByEmailAsync(Account.NormalizeEmail(dto.Email!));
        // Inviting an email that is still pending in this organization re-invites it: a fresh token, code, and
        // expiry replace the old ones, which is how an admin recovers from an expired or lost invitation.
        var reinvite = existing is { Status: AccountStatus.Invited } && existing.OrganizationId == organizationId;
        if (existing is not null && !reinvite)
            throw new ConflictException(EmailInUse);

        var (token, tokenHash) = InvitationTokens.CreateLinkToken();
        var (code, codeHash) = InvitationTokens.CreateCode();
        var expiresAt = DateTime.UtcNow + InvitationTokens.Lifetime;
        Account account;
        if (reinvite)
        {
            account = existing!;
            account.InvitationTokenHash = tokenHash;
            account.InvitationCodeHash = codeHash;
            account.InvitationExpiresAt = expiresAt;
        }
        else
        {
            account = AccountMapper.ToInvitedAccount(dto, organizationId, tokenHash, codeHash, expiresAt);
            accounts.Add(account);
        }
        // A concurrent invitation for the same email may have passed the lookup and saved first.
        await db.SaveChangesOrConflictAsync(EmailInUse);

        try
        {
            await email.SendAsync(InvitationEmail.Create(
                account.Email, organization.Name, caller.FullName is { Length: > 0 } ? caller.FullName : caller.Email, emailOptions.Value.WebBaseUrl, token, code));
        }
        catch
        {
            // The token exists only in the email, so an invitation that was never delivered cannot be used and
            // would block a new one for the same address; remove it and let the caller retry. A re-invited
            // account stays, since the admin can simply re-invite it again.
            if (!reinvite)
            {
                db.Accounts.Remove(account);
                await db.SaveChangesAsync();
            }
            throw;
        }

        return AccountMapper.ToDto(account);
    }

    public async Task Resend(string requestedEmail)
    {
        if (string.IsNullOrWhiteSpace(requestedEmail)) return;
        var account = await accounts.FindByEmailAsync(Account.NormalizeEmail(requestedEmail));
        if (account is not { Status: AccountStatus.Invited }) return;
        var organization = await organizations.GetByIdAsync(account.OrganizationId);
        if (organization is null) return;

        // Only the code is rotated, not the link token: the person likely already has the activation page
        // open with the original link's token in its URL, and rotating it would strand that open page.
        var (code, codeHash) = InvitationTokens.CreateCode();
        account.InvitationCodeHash = codeHash;
        account.InvitationExpiresAt = DateTime.UtcNow + InvitationTokens.Lifetime;
        await db.SaveChangesAsync();

        // Best-effort: the refreshed code is already saved, so a delivery failure here just leaves the
        // person to try resending again, same as any other transient email failure.
        try
        {
            await email.SendAsync(InvitationEmail.CreateCodeReminder(account.Email, organization.Name, code));
        }
        catch
        {
            // ponytail: swallowed, no retry/backoff; fine for a person-initiated resend they can click again.
        }
    }
}
