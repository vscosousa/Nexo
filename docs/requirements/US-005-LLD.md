# US-005 - LLD - Sign in to an existing account

[Requirements](README.md) · [US-005](US-005-sign-in.md) · [HLD](US-005-HLD.md)

**Status:** implemented (password and Google). See [design review gaps](README.md#design-review-gaps) for what remains open.

Full technical detail, building on the [HLD](US-005-HLD.md)'s contract and structure. See the [level 3 sequence diagram](../us/US-005/README.md#level-3---backend) for the call sequence.

## Domain

Reuses `Account` (per [US-001-LLD](US-001-LLD.md#domain), extended per [US-003-LLD](US-003-LLD.md#domain)); no new fields. A null `Account.PasswordHash` means password sign-in is unavailable; this applies to invited accounts as well as active SSO-only accounts. Only `Status = Active` accounts can sign in, by any method.

Adds `ExternalLogin` (`Id`, `AccountId`, `Provider`, `ProviderKey`): `Provider` is the lowercase provider name (`google`, `microsoft`), `ProviderKey` is the account's stable id at the provider, and (`Provider`, `ProviderKey`) is unique.

## Service logic (`AuthService.SignIn`)

1. Validate `SignInDto`: `Email` and `Password` non-empty. Fail with a validation error (→ 400) otherwise.
2. Call `IAccountRepository.FindByEmailAsync` with the trimmed, lowercased email. If no account is found, the account is not `Active`, or its `PasswordHash` is null (SSO-only), fail with a generic authorization error (→ 401); do not reveal which condition applied.
3. Verify the password via `PasswordHasher<Account>.VerifyHashedPassword(account, account.PasswordHash, dto.Password)`. If it does not match, fail with the same generic authorization error (→ 401).
4. Call `ITokenService.GenerateToken(account)` to issue a JWT (HS256, 8 hours, `sub`/`orgId`/`role` claims, key from `Jwt:Key`), per [ADR-006](../decisions/ADR-006-authentication.md).
5. Return a `SessionDto` with the token; `AuthController` sets it as the `nexo_session` cookie and answers 204 without a body.

## Service logic (`AuthService.SignInExternal`)

Called by `AuthController` after the provider handler authenticated the user. The provider is the claims issuer (the provider handler's scheme name), never a route value.

1. Call `IExternalLoginRepository.FindAsync(provider, providerKey)`.
2. If a link exists, load its account by id. Otherwise, only when the email is present and verified, call `IAccountRepository.FindByEmailAsync` with the normalized email. Otherwise there is no account.
3. If there is no account, or it is not `Active`, fail with the same generic authorization error (→ redirect with `error=oauth`). No account is ever created and no provider is linked to an `Invited` account.
4. With no existing link, add an `ExternalLogin` for the account and save; a unique-index violation (a concurrent first sign-in) is reported as a conflict (→ redirect with `error=oauth`, and signing in again succeeds).
5. Call `ITokenService.GenerateToken(account)` and return the `SessionDto`.

Email verification comes from the provider's `email_verified` claim. Google supplies it; Microsoft does not, so Microsoft logins never match an account (a [design review gap](README.md#design-review-gaps)).

## Error handling

| Case | Detection point | Response |
| --- | --- | --- |
| Missing email/password | Step 1 (service validation) | 400, field-level errors |
| No account for the email | Step 2 (repository lookup) | 401, generic (same body as wrong password) |
| Account has no password set (SSO-only) | Step 2 (repository lookup) | 401, generic (same body as wrong password) |
| Password does not match | Step 3 (hash verification) | 401, generic (same body as unknown email) |
| Account is not `Active` | Step 2 (status gate) | 401, generic |
| OAuth: unverified or missing email, no matching account, or account not `Active` | `SignInExternal` step 3 | Redirect to `/login?error=oauth` |
| OAuth: no provider identity in the external cookie (cancelled at the provider) | Callback | Redirect to `/login?error=oauth` |
| OAuth: concurrent first sign-in links the identity twice | `SignInExternal` step 4 | Redirect to `/login?error=oauth`; retry succeeds |

## Related artifacts

[HLD](US-005-HLD.md), [SSD/SD diagrams](../us/US-005/README.md), [Domain model](../domain-models/README.md#accounts-and-organizations), [ADR-006](../decisions/ADR-006-authentication.md), tests (`SignInEndpointTests`, `ExternalSignInEndpointTests`, `SignInForm.test.tsx`, `LoginCallbackPage.test.tsx`, `client.test.ts`).
