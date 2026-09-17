# US-001 - LLD - Create an organization and its admin account

[Requirements](README.md) · [US-001](US-001-create-organization-admin.md) · [HLD](US-001-HLD.md)

**Status:** proposed; not implemented. See [design review gaps](README.md#design-review-gaps) before implementing this contract.

Full technical detail, building on the [HLD](US-001-HLD.md)'s contract and structure. See the [level 3 sequence diagram](../us/US-001/README.md#level-3---backend) for the call sequence.

## Domain

**`Organization`**

| Field | Type | Rule |
| --- | --- | --- |
| `Id` | Guid | Generated on creation |
| `Name` | string | Required, non-empty |
| `Plan` | enum (`Free`, ...) | Defaults to `Free` on creation |
| `MemberLimit` | int | `20` for `Free` |

**`Account`**

| Field | Type | Rule |
| --- | --- | --- |
| `Id` | Guid | Generated on creation |
| `Email` | string | Required, valid format, unique across all accounts |
| `Name` | string, nullable | Required for this feature; null for an account still `Invited` (see [US-002-LLD](US-002-LLD.md)) |
| `Role` | enum (`Admin`, `Member`) | `Admin` when created via this feature |
| `Status` | enum (`Invited`, `Active`) | `Active` immediately for an admin account (this feature); a member account starts `Invited` (see [US-002-LLD](US-002-LLD.md)) and becomes `Active` on activation (see [US-003-LLD](US-003-LLD.md)). Only `Active` accounts count toward the organization's `MemberLimit` |
| `OrganizationId` | Guid | Foreign key to `Organization` |

## Service logic (`OrganizationService.Register`)

1. Validate `RegisterOrganizationDto`: `OrganizationName`, `AdminName` non-empty; `AdminEmail` a valid email format. Fail with a validation error (→ 400) otherwise.
2. Call `IAccountRepository.FindByEmailAsync(dto.AdminEmail)`. If an account already exists, fail with a conflict error (→ 409); do not proceed.
3. Map the DTO to a new `Organization` (`OrganizationMapper.ToOrganization`), with `Plan = Free` and `MemberLimit = 20`.
4. Map the DTO and the new organization to a new `Account` (`OrganizationMapper.ToAdminAccount`), with `Role = Admin` and `Status = Active`.
5. Add both via `IOrganizationRepository.Add` and `IAccountRepository.Add`.
6. Call `NexoDbContext.SaveChangesAsync()` once, committing both inserts atomically.
7. Map the persisted `Organization` to `OrganizationDto` and return it (→ 201).

## Error handling

| Case | Detection point | Response |
| --- | --- | --- |
| Missing/invalid fields | Step 1 (service validation) | 400, field-level errors |
| Email already has an account | Step 2 (repository lookup) | 409, no write attempted |
| Database failure on save | Step 6 | 500; no partial organization/account persisted, since both inserts are in one `SaveChangesAsync` |

## Related artifacts

[HLD](US-001-HLD.md), [SSD/SD diagrams](../us/US-001/README.md), [Domain model](../domain-models/README.md#accounts-and-organizations), tests (not yet created, required before implementation per [AGENTS.md](../../AGENTS.md#working-rules)).
