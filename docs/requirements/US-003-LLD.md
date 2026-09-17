# US-003 - LLD - Create a member account

[Requirements](README.md) · [US-003](US-003-create-member-account.md) · [HLD](US-003-HLD.md)

Full technical detail, building on the [HLD](US-003-HLD.md)'s contract and structure. See the [level 3 sequence diagram](../us/US-003/README.md#level-3---sd-detailed) for the call sequence.

## Domain

**`Account`** (existing, per [US-001-LLD](US-001-LLD.md#domain))

| Field | Type | Rule |
| --- | --- | --- |
| `PasswordHash` | string, nullable | Set via `PasswordHasher<Account>` for a password-based account; null for an SSO-only account (per [ADR-006](../decisions/ADR-006-authentication.md)) |
| `Role` | enum (`Admin`, `Member`) | `Member` when created via this feature |
| `OrganizationId` | Guid | Taken from the matched `EligibleEmail`, not from client input |

## Service logic (`AccountService.Register`)

1. Validate `CreateMemberAccountDto`: `Email` a valid email format; `Name` and `Password` non-empty. Fail with a validation error (→ 400) otherwise.
2. Call `IEligibleEmailRepository.FindByEmailAsync(dto.Email)`. If no match is found, fail with an authorization error (→ 403); the email is not registered to any organization.
3. Call `IAccountRepository.FindByEmailAsync(dto.Email)`. If an account already exists, fail with a conflict error (→ 409); do not proceed.
4. Map the DTO and the matched `EligibleEmail.OrganizationId` to a new `Account` (`AccountMapper.ToMemberAccount`), with `Role = Member` and `PasswordHash` set via `PasswordHasher<Account>.HashPassword`.
5. Add the account via `IAccountRepository.Add`, and remove the consumed `EligibleEmail` via `IEligibleEmailRepository.Remove`.
6. Call `NexoDbContext.SaveChangesAsync()` once, committing both the insert and the removal atomically.
7. Map the persisted `Account` to `AccountDto` and return it (→ 201). The controller then signs the new account in, per [ADR-006](../decisions/ADR-006-authentication.md).

## Error handling

| Case | Detection point | Response |
| --- | --- | --- |
| Missing/invalid fields | Step 1 (service validation) | 400, field-level errors |
| Email not registered to any organization | Step 2 (repository lookup) | 403, no write attempted |
| Email already has an account | Step 3 (repository lookup) | 409, no write attempted |
| Database failure on save | Step 6 | 500; no partial state persisted, since the insert and removal are in one `SaveChangesAsync` |

## Related artifacts

[HLD](US-003-HLD.md), [SSD/SD diagrams](../us/US-003/README.md), [Domain model](../domain-models/README.md#accounts-and-organizations), [ADR-006](../decisions/ADR-006-authentication.md), tests (not yet created, required before implementation per [AGENTS.md](../../AGENTS.md#working-rules)).
