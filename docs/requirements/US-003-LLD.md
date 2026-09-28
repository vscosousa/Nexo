# US-003 - LLD - Create a member account

[Requirements](README.md) · [US-003](US-003-create-member-account.md) · [HLD](US-003-HLD.md)

**Status:** backend implemented for the password path; the response stops at `AccountDto` (no sign-in or session yet) and OAuth activation is not implemented; frontend password page implemented at `/activate` (redirects to sign-in, no session). See [design review gaps](README.md#design-review-gaps).

Full technical detail, building on the [HLD](US-003-HLD.md)'s contract and structure. See the [level 3 sequence diagram](../us/US-003/README.md#level-3---backend) for the call sequence.

## Domain

Reuses the existing `Account` (per [US-001-LLD](US-001-LLD.md#domain), extended per [US-002-LLD](US-002-LLD.md)); adds nullable `Account.PasswordHash` (per [ADR-006](../decisions/ADR-006-authentication.md)) and `Account.InvitationTokenHash`/`InvitationCodeHash`/`InvitationExpiresAt` (migrations `AddAccountCredentials`, `AddInvitationExpiry`, `AddInvitationCode`). This feature mutates an existing `Invited` row rather than inserting a new one: sets `Name` (trimmed), sets `PasswordHash`, clears `InvitationTokenHash`, `InvitationCodeHash`, and `InvitationExpiresAt`, and flips `Status` to `Active`. PostgreSQL's `xmin` system column is the row's concurrency token, so an update of a row that changed since it was read fails instead of overwriting.

## Service logic (`AccountActivationService.Activate`)

1. Validate `ActivateAccountDto`: `Email` a valid email format of at most 320 characters; `Name` non-empty and at most 200 characters; `Password` non-empty and at most 128 characters; `LinkToken` non-empty; `Code` non-empty. Fail with a validation error (→ 400) otherwise.
2. Call `IAccountRepository.FindByEmailAsync` with the normalized (trimmed, lowercased) email. If no account is found, fail with an authorization error (→ 403); the email was never registered to any organization.
3. If the found account's `Status` is already `Active`, fail with a conflict error (→ 409); do not proceed.
   Otherwise compare the SHA-256 of `LinkToken` with `Account.InvitationTokenHash`, and the SHA-256 of the upper-cased `Code` with `Account.InvitationCodeHash`, both in constant time, and check `InvitationExpiresAt` is in the future; on any mismatch or expiry fail with the same authorization error as step 2 (→ 403), so which one failed is not revealed. Both are required — the link token alone (it sits in a URL, which can leak through browser history or a forwarded link) is not enough. An admin re-invites the email ([US-002](US-002-register-member-email.md)) to issue a fresh link token, code, and expiry.
   Call `IOrganizationRepository.GetByIdAsync(account.OrganizationId)` and `IAccountRepository.CountByOrganizationAndStatusAsync(organizationId, Active)`. If the count has reached `MemberLimit`, fail with a conflict error (→ 409). Then check the rest of the [password rules](#password-rules) with the organization name and `Name` as forbidden content; fail with a validation error (→ 400) if any is broken. The limit check counts without locking, so concurrent activations of different accounts can still overshoot it; that remains an open [design review gap](README.md#design-review-gaps).
4. Apply the DTO to the existing `Account` (`AccountMapper.ApplyActivation`): set `Name` (trimmed), set `PasswordHash` via `PasswordHasher<Account>.HashPassword`, clear `InvitationTokenHash` and `InvitationCodeHash`, and set `Status = Active`. Since the account was loaded through `IAccountRepository` (tracked by `NexoDbContext`), no explicit `Update` call is needed.
5. Call `NexoDbContext.SaveChangesAsync()` to persist the mutation. If another request changed the row in between (the `xmin` check fails), fail with a conflict error (→ 409).
6. Map the now-`Active` `Account` to `AccountDto` and return it (→ 200). Signing the account in per [ADR-006](../decisions/ADR-006-authentication.md) is not implemented; the session response is an open decision.

## Password rules

Shared by [US-001](US-001-LLD.md#service-logic-organizationserviceregister) and this feature, implemented in `PasswordPolicy`: at least 8 and at most 128 characters, with an uppercase letter, a lowercase letter, a digit, and a symbol, and no organization name or person name (or word of either) in any form, ignoring case, spaces, punctuation, and look-alike characters such as `0`/`o`, `1`/`i`/`l`, `3`/`e`, `4`/`@`/`a`, `5`/`$`/`s`, `7`/`t`. Words shorter than 3 characters are ignored. Length is measured in characters, and a submitted password is never trimmed.

## Error handling

| Case | Detection point | Response |
| --- | --- | --- |
| Missing/invalid fields | Step 1 (service validation) | 400, field-level errors |
| No account exists for the email | Step 2 (repository lookup) | 403, no write attempted |
| Link token or invitation code wrong, or expired | Step 3 (token/code comparison / expiry check) | 403 with the same message as the case above, no write attempted |
| Organization's active-member limit reached | Step 3 (repository count, `Active` only) | 409, no write attempted |
| Password breaks the rules or contains the organization or person name | Step 3 | 400, field-level errors, no write attempted |
| Account already `Active` | Step 3 (status check) | 409, no write attempted |
| Account activated concurrently (concurrency-token failure on save) | Step 5 | 409, same as the step 3 conflict; the first activation stands |
| Other database failure on save | Step 5 | 500; no partial mutation persisted |

## Related artifacts

[HLD](US-003-HLD.md), [SSD/SD diagrams](../us/US-003/README.md), [Domain model](../domain-models/README.md#accounts-and-organizations), [ADR-006](../decisions/ADR-006-authentication.md), [backend tests](../testing/README.md) (`AccountActivationsEndpointTests`).
