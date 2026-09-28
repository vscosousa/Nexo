# US-001 - LLD - Create an organization and its admin account

[Requirements](README.md) · [US-001](US-001-create-organization-admin.md) · [HLD](US-001-HLD.md)

**Status:** backend implemented up to the `OrganizationDto` contract, with the password path (SSO registration and session delivery remain open, see [design review gaps](README.md#design-review-gaps)); frontend not implemented.

Full technical detail, building on the [HLD](US-001-HLD.md)'s contract and structure. See the [level 3 sequence diagram](../us/US-001/README.md#level-3---backend) for the call sequence.

## Domain

Business rules (defaults, limits, status transitions) are defined once in the [domain model](../domain-models/README.md#accounts-and-organizations); the tables below map fields to C# types only.

**`Plan`**

| Field | Type | Constraint |
| --- | --- | --- |
| `Id` | Guid | Fixed for the seeded `Free` plan |
| `Name` | string | Required, unique |
| `MemberLimit` | int | Maximum active member accounts; see domain model |

**`Organization`**

| Field | Type | Constraint |
| --- | --- | --- |
| `Id` | Guid | Generated on creation |
| `Name` | string | Required, non-empty |
| `PlanId` | Guid | Foreign key to `Plan`; `Free` when created via this feature |

**`Account`**

| Field | Type | Constraint |
| --- | --- | --- |
| `Id` | Guid | Generated on creation |
| `Email` | string | Required, valid format, stored trimmed and lowercased (`Account.NormalizeEmail`), unique across all accounts |
| `Name` | string, nullable | Required for this feature (at most 200 characters, stored trimmed); nullability rule per domain model (`Invited` accounts) |
| `PasswordHash` | string, nullable | `PasswordHasher<Account>` hash of the submitted password; null for invited accounts and, later, SSO-only accounts |
| `Role` | enum (`Admin`, `Member`) | `Admin` when created via this feature |
| `Status` | enum (`Invited`, `Active`) | `Active` immediately for an admin account (this feature); see domain model for the member lifecycle and limit counting |
| `OrganizationId` | Guid | Foreign key to `Organization` |

## Service logic (`OrganizationService.Register`)

1. Validate `RegisterOrganizationDto`: `OrganizationName`, `AdminFirstName`, `AdminLastName` non-empty and at most 100 characters each; `AdminEmail` a valid email format of at most 320 characters; `Password` present and satisfying the [password rules](US-003-LLD.md#password-rules) with the organization and admin names as forbidden content. Fail with a validation error (→ 400) otherwise.
2. Normalize the email (`Account.NormalizeEmail`: trim, lowercase) and call `IAccountRepository.FindByEmailAsync(email)`. If an account already exists, fail with a conflict error (→ 409); do not proceed.
3. Call `IPlanRepository.FindByNameAsync("Free")`. The plan is seeded by migration; if it is missing, fail with an unexpected error (→ 500).
4. Map the DTO and the plan to a new `Organization` (`OrganizationMapper.ToOrganization`).
5. Map the DTO and the new organization to a new `Account` (`OrganizationMapper.ToAdminAccount`), with the normalized email, trimmed names, the hashed password (`PasswordHasher<Account>`), `Role = Admin` and `Status = Active`.
6. Add both via `IOrganizationRepository.Add` and `IAccountRepository.Add`.
7. Call `NexoDbContext.SaveChangesAsync()` once, committing both inserts atomically.
8. Map the persisted `Organization` to `OrganizationDto` and return it (→ 201).

## Error handling

| Case | Detection point | Response |
| --- | --- | --- |
| Missing/invalid fields | Step 1 (service validation) | 400, field-level errors |
| Email already has an account | Step 2 (repository lookup) | 409, no write attempted; the same email in a different case counts as the same account |
| Free plan not seeded | Step 3 (plan lookup) | 500, no write attempted |
| Same email registered concurrently (unique-index violation on save) | Step 7 | 409, same as the step 2 conflict; no partial organization/account persisted |
| Other database failure on save | Step 7 | 500; no partial organization/account persisted, since both inserts are in one `SaveChangesAsync` |

## Related artifacts

[HLD](US-001-HLD.md), [SSD/SD diagrams](../us/US-001/README.md), [Domain model](../domain-models/README.md#accounts-and-organizations), tests ([`OrganizationMapperTests`](../../api/tests/Mappers/OrganizationMapperTests.cs), [`OrganizationsEndpointTests`](../../api/tests/Controllers/OrganizationsEndpointTests.cs)).
