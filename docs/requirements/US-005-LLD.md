# US-005 - LLD - Sign in to an existing account

[Requirements](README.md) · [US-005](US-005-sign-in.md) · [HLD](US-005-HLD.md)

**Status:** implemented (password and Google). See [design review gaps](README.md#design-review-gaps) for what remains open.

Full technical detail, building on the [HLD](US-005-HLD.md)'s contract and structure. See the [level 3 sequence diagram](../us/US-005/README.md#level-3---backend) for the call sequence.

## Domain

Reuses `Account` (per [US-001-LLD](US-001-LLD.md#domain), extended per [US-003-LLD](US-003-LLD.md#domain)). A null `Account.PasswordHash` means password sign-in is unavailable; this applies to invited accounts as well as active SSO-only accounts. Only `Status = Active` accounts that are not locked can sign in, by any method.

[ADR-010](../decisions/ADR-010-account-security-hardening.md) adds `FailedSignInAttempts` (wrong passwords in a row), `UnlockTokenHash` (SHA-256 of the emailed unlock link's token; non-null means locked), `UnlockExpiresAt` (when that link stops working, 24 hours after it was sent), and `SessionVersion` (carried in every token as the `sv` claim; incrementing it ends all the account's sessions).

Adds `ExternalLogin` (`Id`, `AccountId`, `Provider`, `ProviderKey`): `Provider` is the lowercase provider name (`google`, `microsoft`), `ProviderKey` is the account's stable id at the provider, and (`Provider`, `ProviderKey`) is unique.

## Service logic (`AuthService.SignIn`)

1. Validate `SignInDto`: `Email` and `Password` non-empty. Fail with a validation error (→ 400) otherwise.
2. Call `IAccountRepository.FindByEmailAsync` with the trimmed, lowercased email. The account can sign in with a password only if it is `Active`, has a `PasswordHash`, and is not locked.
3. Verify the password via `PasswordHasher<Account>.VerifyHashedPassword`, against the account's hash, or against a fixed dummy hash when step 2 found no eligible account, so an unknown email costs the same hashing time as a registered one. If there was no eligible account, fail with a generic authorization error (→ 401); do not reveal which condition applied. When the account is locked and its unlock link has expired, first replace the link (a conditional update that only matches an expired link, so concurrent attempts email once) and email it; if sending fails, mark the new link expired so the next attempt retries.
4. If the password does not match, increment `FailedSignInAttempts` in the database, then, in one conditional update, lock the account if it has reached `AuthService.MaxSignInAttempts` (5) and is not already locked: set `UnlockTokenHash`, set `UnlockExpiresAt` to now + `AuthService.UnlockLinkLifetime` (24 hours), and increment `SessionVersion`. When that update locked it, email the owner `AccountEmails.Unlock` (`/unlock?email=…&token=…`); if sending fails, undo the lock so the owner is never locked out without a link. Fail with the same generic error (→ 401). A correct password resets `FailedSignInAttempts` to 0.
5. Call `ITokenService.GenerateToken(account)` to issue a JWT (HS256, 8 hours, `sub`/`orgId`/`role`/`sv` claims, `iss`/`aud` from `Jwt:Issuer`/`Jwt:Audience`, key from `Jwt:Key`), per [ADR-006](../decisions/ADR-006-authentication.md).
6. Return a `SessionDto` with the token; `AuthController` sets it as the `nexo_session` cookie and answers 204 without a body.

## Service logic (`AuthService.Unlock` and `EndSessions`)

`POST /auth/unlock` with `{ email, token }`, sent by the web app's `/unlock` page as soon as it opens: if the account for the normalized email is locked, its link has not expired, and the token matches `UnlockTokenHash` (constant time), clear the lock and reset `FailedSignInAttempts` (→ 200); otherwise → 403, the same for every cause. The endpoint is rate limited by the `public` policy.

`POST /auth/sign-out` calls `EndSessions(sub)`, incrementing `SessionVersion`. Every authenticated request (`JwtBearerEvents.OnTokenValidated`) reads the account's current `SessionVersion` and rejects the token (→ 401) when the account no longer exists or its `sv` claim differs.

## Service logic (`AuthService.SignInExternal`)

Called by `AuthController` after the provider handler authenticated the user. The provider is the claims issuer (the provider handler's scheme name), never a route value.

1. Call `IExternalLoginRepository.FindAsync(provider, providerKey)`.
2. If a link exists, load its account by id. Otherwise, only when the email is present and verified, call `IAccountRepository.FindByEmailAsync` with the normalized email. Otherwise there is no account.
3. If there is no account, it is not `Active`, or it is locked, fail with the same generic authorization error (→ redirect with `error=oauth`). No account is ever created and no provider is linked to an `Invited` or `Unverified` account.
4. With no existing link, add an `ExternalLogin` for the account and save; a unique-index violation (a concurrent first sign-in) is reported as a conflict (→ redirect with `error=oauth`, and signing in again succeeds).
5. Call `ITokenService.GenerateToken(account)` and return the `SessionDto`.

Email verification comes from the provider's `email_verified` claim. Google supplies it; Microsoft does not, so Microsoft logins never match an account (a [design review gap](README.md#design-review-gaps)).

## Error handling

| Case | Detection point | Response |
| --- | --- | --- |
| Missing email/password | Step 1 (service validation) | 400, field-level errors |
| No account for the email | Step 2 (repository lookup) | 401, generic (same body as wrong password) |
| Account has no password set (SSO-only) | Step 2 (repository lookup) | 401, generic (same body as wrong password) |
| Password does not match | Step 4 (hash verification) | 401, generic (same body as unknown email); counted toward the lockout |
| Account locked | Step 2 | 401, generic, even with the right password |
| Account is not `Active` | Step 2 (status gate) | 401, generic |
| OAuth: unverified or missing email, no matching account, or account not `Active` or locked | `SignInExternal` step 3 | Redirect to `/login?error=oauth` |
| OAuth: no provider identity in the external cookie (cancelled at the provider) | Callback | Redirect to `/login?error=oauth` |
| OAuth: concurrent first sign-in links the identity twice | `SignInExternal` step 4 | Redirect to `/login?error=oauth`; retry succeeds |

## Related artifacts

[HLD](US-005-HLD.md), [SSD/SD diagrams](../us/US-005/README.md), [Domain model](../domain-models/README.md#accounts-and-organizations), [ADR-006](../decisions/ADR-006-authentication.md), [ADR-010](../decisions/ADR-010-account-security-hardening.md), tests (`SignInEndpointTests`, `ExternalSignInEndpointTests`, `AccountLockoutEndpointTests`, `SessionEndpointTests`, `SignInForm.test.tsx`, `EmailLinkPage.test.tsx`, `LoginCallbackPage.test.tsx`, `client.test.ts`).
