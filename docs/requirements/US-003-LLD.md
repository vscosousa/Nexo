# US-003 - LLD - Create a member account

[Requirements](README.md) · [US-003](US-003-create-member-account.md) · [HLD](US-003-HLD.md)

**Status:** backend and frontend (`/activate`) implemented. Google activation is implemented (below) and signs the member in with the session cookie; password activation still stops at `AccountDto` and redirects to sign-in. See [design review gaps](README.md#design-review-gaps).

Full technical detail, building on the [HLD](US-003-HLD.md)'s contract and structure. See the [level 3 sequence diagram](../us/US-003/README.md#level-3---backend) for the call sequence.

## Domain

Reuses the existing `Account` (per [US-001-LLD](US-001-LLD.md#domain), extended per [US-002-LLD](US-002-LLD.md)); adds nullable `Account.PasswordHash` (per [ADR-006](../decisions/ADR-006-authentication.md)) and `Account.InvitationTokenHash`/`InvitationCodeHash`/`InvitationExpiresAt` (migrations `AddAccountCredentials`, `AddInvitationExpiry`, `AddInvitationCode`). This feature mutates an existing `Invited` row rather than inserting a new one: sets `Name` (trimmed), sets `PasswordHash`, clears `InvitationTokenHash`, `InvitationCodeHash`, and `InvitationExpiresAt`, and flips `Status` to `Active`. PostgreSQL's `xmin` system column is the row's concurrency token, so an update of a row that changed since it was read fails instead of overwriting.

## Service logic (`AccountActivationService.Activate`)

1. Validate `ActivateAccountDto`: `Email` a valid email format of at most 320 characters; `Name` non-empty and at most 200 characters; `Password` non-empty and at most 128 characters; `LinkToken` non-empty; `Code` non-empty. Fail with a validation error (→ 400) otherwise.
2. Call `IAccountRepository.FindByEmailAsync` with the normalized (trimmed, lowercased) email. If no account is found, or it is not `Invited` (for example, already `Active`), fail with an authorization error (→ 403); the response is the same as for a wrong token or code, so it does not reveal which emails have active accounts.
3. Compare the SHA-256 of `LinkToken` with `Account.InvitationTokenHash` in constant time, check `InvitationExpiresAt` is in the future, and check `InvitationFailedAttempts` is below `InvitationTokens.MaxCodeAttempts` (5); on any failure fail with the same authorization error as step 2 (→ 403). Then compare the SHA-256 of the upper-cased `Code` with `Account.InvitationCodeHash` in constant time; on a mismatch, increment `InvitationFailedAttempts` in the database (`ExecuteUpdate`, so concurrent guesses are all counted) and fail with the same 403. A wrong link token is not counted, so someone without the link cannot use up the attempts. Both secrets are required. The link token alone (it sits in a URL, which can leak through browser history or a forwarded link) is not enough. An admin re-invites the email ([US-002](US-002-register-member-email.md)) to issue a fresh link token, code, and expiry.
   Begin a transaction and lock the organization's row (`IOrganizationRepository.LockAsync`, `SELECT ... FOR UPDATE`), so concurrent activations in the same organization count and save one after another. Call `IOrganizationRepository.GetByIdAsync(account.OrganizationId)` and `IAccountRepository.CountByOrganizationAndStatusAsync(organizationId, Active)`. If the count has reached `MemberLimit`, fail with a conflict error (→ 409). Then check the rest of the [password rules](#password-rules) with the organization name and `Name` as forbidden content; fail with a validation error (→ 400) if any is broken.
4. Apply the DTO to the existing `Account` (`AccountMapper.ApplyActivation`): set `Name` (trimmed), set `PasswordHash` via `PasswordHasher<Account>.HashPassword`, clear `InvitationTokenHash` and `InvitationCodeHash`, and set `Status = Active`. Since the account was loaded through `IAccountRepository` (tracked by `NexoDbContext`), no explicit `Update` call is needed.
5. Call `NexoDbContext.SaveChangesAsync()` to persist the mutation, then commit, releasing the organization lock. If another request changed the row in between (the `xmin` check fails), fail with a conflict error (→ 409).
6. Map the now-`Active` `Account` to `AccountDto` and return it (→ 200). Signing the account in per [ADR-006](../decisions/ADR-006-authentication.md) is not implemented; the session response is an open decision.

## Service logic (`AccountActivationService.ActivateExternal`)

Once the code is confirmed, the page offers Google: `GET /auth/external/google?intent=activate&email=…&token=…&code=…`. The provider handler carries the invitation through the handshake (inside its protected state and then the 5-minute `External` cookie, never in a web URL); the callback redirects to `/activate?email=…&token=…&external=google` (or `…&error=oauth` without a verified email). The page reads `GET /auth/external/pending`, skips the code, shows the names from Google for editing and no password, and posts `POST /auth/external/activate` with `{ firstName, lastName }` and the anti-forgery header.

1. Validate both names (→ 400).
2. Check the invitation carried in the `External` cookie exactly as steps 2-3 of `Activate` (→ 403; a wrong code is counted).
3. Require the provider's verified email to equal the invited email (→ 403 otherwise), so one person cannot activate another's invitation with their own Google account.
4. In a transaction, lock the organization row and check the member limit, as in `Activate` (→ 409).
5. Set the names and `Status = Active`, clear the invitation hashes, leave `PasswordHash` null, and add an `ExternalLogin` for the provider identity; save and commit. A concurrent change or an identity already linked elsewhere is a conflict (→ 409).
6. Issue a session token; `AuthController` clears the `External` cookie, sets the session cookie, and answers 200 with `AccountDto`.

## Password rules

Shared by [US-001](US-001-LLD.md#service-logic-organizationserviceregister) and this feature, implemented in `PasswordPolicy` (`Measure` rates, `Check` refuses) and mirrored in the web app's `passwordStrength.ts`, which drives the strength bar under the password field. A password is rated by its length and by how many kinds of character it mixes (uppercase, lowercase, digits, symbols, where anything else, spaces included, counts as a symbol). It is Weak, and refused, when it is under 8 characters, contains a person's or the organization's name, or mixes fewer than 3 kinds without being a passphrase of 16+ characters. Otherwise it scores one point for each length step it reaches (12, 16, 20, 24 characters) plus (kinds − 3), so each kind of character is worth about four characters: up to 1 is Reasonable (Razoável), 2 is Strong (Forte), 3 or more is Very strong (Muito forte).

| Kinds | Reasonable | Strong | Very strong |
| --- | --- | --- | --- |
| 4 | 8 to 11 | 12 to 15 | 16+ |
| 3 | 8 to 15 | 16 to 19 | 20+ |
| 2 (passphrase) | 16 to 19 | 20 to 23 | 24+ |
| 1 (passphrase) | 16 to 23 | 24+ | Never |

Below Very strong, the bar's hint names the quickest improvement: a missing kind of character (symbols first, then digits, uppercase, lowercase), otherwise the next length step.

At most 128 characters. Names are matched as the whole name and each of its words, ignoring case, spaces, punctuation, and look-alike characters such as `0`/`o`, `1`/`i`/`l`, `3`/`e`, `4`/`@`/`a`, `5`/`$`/`s`, `7`/`t`; words shorter than 3 characters are ignored. Length is measured in characters, and a submitted password is never trimmed. The activation form knows only the person's names, so a password containing the organization's name is caught by the API and shown under the password field.

## Error handling

| Case | Detection point | Response |
| --- | --- | --- |
| Missing/invalid fields | Step 1 (service validation) | 400, field-level errors |
| No account exists for the email | Step 2 (repository lookup) | 403, no write attempted |
| Account not `Invited` (for example, already `Active`) | Step 2 (status check) | 403 with the same message as the case above, no write attempted |
| Link token wrong, invitation expired, or 5 wrong codes already tried | Step 3 | 403 with the same message as the case above, no write attempted |
| Right link token, wrong code | Step 3 (code comparison) | 403 with the same message; `InvitationFailedAttempts` incremented |
| Too many requests from one address | Rate limiter, before the action | 429 (`RateLimit:PublicPermitLimit` per minute, default 10) |
| Organization's active-member limit reached | Step 3 (repository count, `Active` only) | 409, no write attempted |
| Password breaks the rules or contains the organization or person name | Step 3 | 400, field-level errors, no write attempted |
| Account activated concurrently (concurrency-token failure on save) | Step 5 | 409; the first activation stands |
| Other database failure on save | Step 5 | 500; no partial mutation persisted |

## Related artifacts

[HLD](US-003-HLD.md), [SSD/SD diagrams](../us/US-003/README.md), [Domain model](../domain-models/README.md#accounts-and-organizations), [ADR-006](../decisions/ADR-006-authentication.md), [backend tests](../testing/README.md) (`AccountActivationsEndpointTests`).
