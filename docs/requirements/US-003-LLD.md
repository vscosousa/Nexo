# US-003 - LLD - Create a member account

[Requirements](README.md) · [US-003](US-003-create-member-account.md) · [HLD](US-003-HLD.md)

Full technical detail, building on the [HLD](US-003-HLD.md)'s contract and structure. See the [level 3 sequence diagram](../us/US-003/README.md#level-3---backend) for the call sequence.

## Domain

Reuses the existing `Account` (per [US-001-LLD](US-001-LLD.md#domain), extended per [US-002-LLD](US-002-LLD.md)); no new fields. This feature mutates an existing `Invited` row rather than inserting a new one: sets `Name` and `PasswordHash`, and flips `Status` to `Active`.

## Service logic (`AccountActivationService.Activate`)

1. Validate `ActivateAccountDto`: `Email` a valid email format; `Name` and `Password` non-empty. Fail with a validation error (→ 400) otherwise.
2. Call `IAccountRepository.FindByEmailAsync(dto.Email)`. If no account is found, fail with an authorization error (→ 403); the email was never registered to any organization.
3. If the found account's `Status` is already `Active`, fail with a conflict error (→ 409); do not proceed.
4. Apply the DTO to the existing `Account` (`AccountMapper.ApplyActivation`): set `Name`, set `PasswordHash` via `PasswordHasher<Account>.HashPassword`, and set `Status = Active`. Since the account was loaded through `IAccountRepository` (tracked by `NexoDbContext`), no explicit `Update` call is needed.
5. Call `NexoDbContext.SaveChangesAsync()` to persist the mutation.
6. Map the now-`Active` `Account` to `AccountDto` and return it (→ 200). The controller then signs the account in, per [ADR-006](../decisions/ADR-006-authentication.md).

## Error handling

| Case | Detection point | Response |
| --- | --- | --- |
| Missing/invalid fields | Step 1 (service validation) | 400, field-level errors |
| No account exists for the email | Step 2 (repository lookup) | 403, no write attempted |
| Account already `Active` | Step 3 (status check) | 409, no write attempted |
| Database failure on save | Step 5 | 500; no partial mutation persisted |

## Related artifacts

[HLD](US-003-HLD.md), [SSD/SD diagrams](../us/US-003/README.md), [Domain model](../domain-models/README.md#accounts-and-organizations), [ADR-006](../decisions/ADR-006-authentication.md), tests (not yet created, required before implementation per [AGENTS.md](../../AGENTS.md#working-rules)).
