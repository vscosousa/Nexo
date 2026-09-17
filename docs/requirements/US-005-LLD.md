# US-005 - LLD - Sign in to an existing account

[Requirements](README.md) · [US-005](US-005-sign-in.md) · [HLD](US-005-HLD.md)

Full technical detail, building on the [HLD](US-005-HLD.md)'s contract and structure. See the [level 3 sequence diagram](../us/US-005/README.md#level-3---backend) for the call sequence.

## Domain

Reuses the existing `Account` (per [US-001-LLD](US-001-LLD.md#domain), extended per [US-003-LLD](US-003-LLD.md#domain)); no new fields. `Account.PasswordHash` being null identifies an SSO-only account.

## Service logic (`AuthService.SignIn`)

1. Validate `SignInDto`: `Email` and `Password` non-empty. Fail with a validation error (→ 400) otherwise.
2. Call `IAccountRepository.FindByEmailAsync(dto.Email)`. If no account is found, or the account's `PasswordHash` is null (SSO-only), fail with a generic authorization error (→ 401); do not reveal which condition applied.
3. Verify the password via `PasswordHasher<Account>.VerifyHashedPassword(account, account.PasswordHash, dto.Password)`. If it does not match, fail with the same generic authorization error (→ 401).
4. Call `ITokenService.GenerateToken(account)` to issue a JWT, per [ADR-006](../decisions/ADR-006-authentication.md).
5. Return a `SessionDto` with the token (→ 200).

## Error handling

| Case | Detection point | Response |
| --- | --- | --- |
| Missing email/password | Step 1 (service validation) | 400, field-level errors |
| No account for the email | Step 2 (repository lookup) | 401, generic (same body as wrong password) |
| Account has no password set (SSO-only) | Step 2 (repository lookup) | 401, generic (same body as wrong password) |
| Password does not match | Step 3 (hash verification) | 401, generic (same body as unknown email) |

## Related artifacts

[HLD](US-005-HLD.md), [SSD/SD diagrams](../us/US-005/README.md), [Domain model](../domain-models/README.md#accounts-and-organizations), [ADR-006](../decisions/ADR-006-authentication.md), tests (not yet created, required before implementation per [AGENTS.md](../../AGENTS.md#working-rules)).
