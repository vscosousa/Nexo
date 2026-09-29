using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Exceptions;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Email;
using Nexo.Api.Infrastructure.Persistence;
using Nexo.Api.Infrastructure.Repositories;

namespace Nexo.Api.Services;

public class AuthService(
    IAccountRepository accounts,
    IExternalLoginRepository externalLogins,
    PasswordHasher<Account> hasher,
    ITokenService tokens,
    IEmailSender email,
    IOptions<EmailOptions> emailOptions,
    NexoDbContext db) : IAuthService
{
    /// <summary>Wrong passwords in a row that lock the account until the owner uses the emailed unlock link.</summary>
    public const int MaxSignInAttempts = 5;

    /// <summary>How long an unlock link works; after that, the next sign-in attempt on the locked account emails a fresh one.</summary>
    public static readonly TimeSpan UnlockLinkLifetime = TimeSpan.FromDays(1);

    private const string InvalidCredentials = "The email or password is not correct.";

    /// <summary>
    /// Checked when no account can match, so a sign-in for an unknown email costs the same password hashing as one
    /// for a registered email, and the response time does not reveal which emails are registered.
    /// </summary>
    private static readonly string DummyHash =
        new PasswordHasher<Account>().HashPassword(new Account { Email = "" }, "Dummy!Passw0rd");

    public async Task<SessionDto> SignIn(SignInDto dto)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(dto.Email))
            errors[nameof(dto.Email)] = ["An email is required."];
        if (string.IsNullOrEmpty(dto.Password))
            errors[nameof(dto.Password)] = ["A password is required."];
        if (errors.Count > 0)
            throw new Domain.Exceptions.ValidationException(errors);

        var account = await accounts.FindByEmailAsync(Account.NormalizeEmail(dto.Email!));
        var candidate = account is { Status: AccountStatus.Active, PasswordHash: { } hash, UnlockTokenHash: null }
            ? (Account: account, Hash: hash)
            : (Account: new Account { Email = "" }, Hash: DummyHash);
        var matches = hasher.VerifyHashedPassword(candidate.Account, candidate.Hash, dto.Password!)
            != PasswordVerificationResult.Failed;
        if (!ReferenceEquals(candidate.Account, account))
        {
            if (account is { UnlockTokenHash: not null })
                await ReissueExpiredUnlockLinkAsync(account);
            throw new UnauthorizedException(InvalidCredentials);
        }

        if (!matches)
        {
            await RecordFailedSignInAsync(account);
            throw new UnauthorizedException(InvalidCredentials);
        }
        if (account.FailedSignInAttempts > 0)
            await db.Accounts.Where(a => a.Id == account.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.FailedSignInAttempts, 0));

        return tokens.GenerateToken(account);
    }

    public async Task<SessionDto> SignInExternal(string provider, string providerKey, string? email, bool emailVerified)
    {
        var link = await externalLogins.FindAsync(provider, providerKey);
        var account = link is not null
            ? await accounts.GetByIdAsync(link.AccountId)
            : emailVerified && !string.IsNullOrWhiteSpace(email)
                ? await accounts.FindByEmailAsync(Account.NormalizeEmail(email))
                : null;
        if (account is not { Status: AccountStatus.Active, UnlockTokenHash: null })
            throw new UnauthorizedException(InvalidCredentials);

        if (link is null)
        {
            externalLogins.Add(new ExternalLogin { AccountId = account.Id, Provider = provider, ProviderKey = providerKey });
            await db.SaveChangesOrConflictAsync("This social login was just linked; sign in again.");
        }
        return tokens.GenerateToken(account);
    }

    public async Task Unlock(string requestedEmail, string token)
    {
        var account = await accounts.FindByEmailAsync(Account.NormalizeEmail(requestedEmail));
        if (account is not { UnlockTokenHash: { } unlockHash, UnlockExpiresAt: { } expiresAt }
            || expiresAt <= DateTime.UtcNow || !InvitationTokens.TokenMatches(token, unlockHash))
            throw new ForbiddenException("The unlock link is not valid or has expired.");

        account.UnlockTokenHash = null;
        account.UnlockExpiresAt = null;
        account.FailedSignInAttempts = 0;
        await db.SaveChangesOrConflictAsync("The account was just unlocked; sign in.");
    }

    public Task EndSessions(Guid accountId) =>
        db.Accounts.Where(a => a.Id == accountId)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.SessionVersion, a => a.SessionVersion + 1));

    /// <summary>
    /// Counts a wrong password and, on reaching <see cref="MaxSignInAttempts"/>, locks the account, ends its sessions,
    /// and emails the owner an unlock link. Both updates run in the database, so concurrent attempts neither lose a
    /// count nor lock (and email) twice. If the email cannot be sent, the lock is undone, so the owner is never left
    /// locked out with no link; the next wrong password locks it again.
    /// </summary>
    private async Task RecordFailedSignInAsync(Account account)
    {
        await db.Accounts.Where(a => a.Id == account.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.FailedSignInAttempts, a => a.FailedSignInAttempts + 1));

        var (token, tokenHash) = InvitationTokens.CreateLinkToken();
        var locked = await db.Accounts
            .Where(a => a.Id == account.Id && a.UnlockTokenHash == null && a.FailedSignInAttempts >= MaxSignInAttempts)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.UnlockTokenHash, tokenHash)
                .SetProperty(a => a.UnlockExpiresAt, DateTime.UtcNow + UnlockLinkLifetime)
                .SetProperty(a => a.SessionVersion, a => a.SessionVersion + 1));
        if (locked == 0)
            return;

        try
        {
            await email.SendAsync(AccountEmails.Unlock(account.Email, emailOptions.Value.WebBaseUrl, token));
        }
        catch
        {
            await db.Accounts.Where(a => a.Id == account.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.UnlockTokenHash, (string?)null)
                    .SetProperty(a => a.UnlockExpiresAt, (DateTime?)null));
            throw;
        }
    }

    /// <summary>
    /// Replaces a locked account's expired unlock link with a fresh one and emails it, so an owner whose link ran out
    /// is never locked out for good. The update only matches an expired link, so concurrent attempts email once, and
    /// an unexpired link is never replaced (no email flood). If the email cannot be sent, the new link is marked
    /// expired again, so the next attempt retries.
    /// </summary>
    private async Task ReissueExpiredUnlockLinkAsync(Account account)
    {
        var now = DateTime.UtcNow;
        var (token, tokenHash) = InvitationTokens.CreateLinkToken();
        var reissued = await db.Accounts
            .Where(a => a.Id == account.Id && a.UnlockTokenHash != null
                && (a.UnlockExpiresAt == null || a.UnlockExpiresAt <= now))
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.UnlockTokenHash, tokenHash)
                .SetProperty(a => a.UnlockExpiresAt, now + UnlockLinkLifetime));
        if (reissued == 0)
            return;

        try
        {
            await email.SendAsync(AccountEmails.Unlock(account.Email, emailOptions.Value.WebBaseUrl, token));
        }
        catch
        {
            await db.Accounts.Where(a => a.Id == account.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.UnlockExpiresAt, now));
            throw;
        }
    }
}
