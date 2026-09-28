using Microsoft.AspNetCore.Identity;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Exceptions;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Persistence;
using Nexo.Api.Infrastructure.Repositories;

namespace Nexo.Api.Services;

public class AuthService(
    IAccountRepository accounts,
    IExternalLoginRepository externalLogins,
    PasswordHasher<Account> hasher,
    ITokenService tokens,
    NexoDbContext db) : IAuthService
{
    private const string InvalidCredentials = "The email or password is not correct.";

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
        if (account is not { Status: AccountStatus.Active, PasswordHash: not null }
            || hasher.VerifyHashedPassword(account, account.PasswordHash, dto.Password!) == PasswordVerificationResult.Failed)
            throw new UnauthorizedException(InvalidCredentials);

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
        if (account is not { Status: AccountStatus.Active })
            throw new UnauthorizedException(InvalidCredentials);

        if (link is null)
        {
            externalLogins.Add(new ExternalLogin { AccountId = account.Id, Provider = provider, ProviderKey = providerKey });
            await db.SaveChangesOrConflictAsync("This social login was just linked; sign in again.");
        }
        return tokens.GenerateToken(account);
    }
}
