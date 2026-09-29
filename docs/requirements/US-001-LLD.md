# US-001 - LLD - Create an organization and its admin account

[Requirements](README.md) · [US-001](US-001-create-organization-admin.md) · [HLD](US-001-HLD.md)

**Status:** backend and frontend implemented. Google registration is implemented (below): it signs the admin in with the session cookie. Password registration still redirects to `/login`, since `POST /organizations` returns no session. See [design review gaps](README.md#design-review-gaps).

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
| `PlanId` | Guid | Foreign key to `Plan`; the plan chosen before registering ([US-006](US-006-LLD.md)) |

**`Account`**

| Field | Type | Constraint |
| --- | --- | --- |
| `Id` | Guid | Generated on creation |
| `Email` | string | Required, valid format, stored trimmed and lowercased (`Account.NormalizeEmail`), unique across all accounts |
| `Name` | string, nullable | Required for this feature (at most 200 characters, stored trimmed); nullability rule per domain model (`Invited` accounts) |
| `PasswordHash` | string, nullable | `PasswordHasher<Account>` hash of the submitted password; null for invited accounts and for accounts registered or activated with Google |
| `Role` | enum (`Admin`, `Member`) | `Admin` when created via this feature |
| `Status` | enum (`Invited`, `Active`) | `Active` immediately for an admin account (this feature); see domain model for the member lifecycle and limit counting |
| `OrganizationId` | Guid | Foreign key to `Organization` |

## Service logic (`OrganizationService.Register`)

1. Validate `RegisterOrganizationDto`: `OrganizationName`, `AdminFirstName`, `AdminLastName` non-empty and at most 100 characters each; `AdminEmail` a valid email format of at most 320 characters; `Password` present and satisfying the [password rules](US-003-LLD.md#password-rules) with the organization and admin names as forbidden content. Fail with a validation error (→ 400) otherwise.
2. Normalize the email (`Account.NormalizeEmail`: trim, lowercase) and call `IAccountRepository.FindByEmailAsync(email)`. If an account already exists, fail with a conflict error (→ 409); do not proceed.
3. Call `IPlanRepository.GetByIdAsync(dto.PlanId)`. If no plan matches, fail with a validation error on `PlanId` (→ 400); a missing `PlanId` already failed step 1 ([US-006](US-006-LLD.md)).
4. Map the DTO and the plan to a new `Organization` (`OrganizationMapper.ToOrganization`).
5. Map the DTO and the new organization to a new `Account` (`OrganizationMapper.ToAdminAccount`), with the normalized email, trimmed names, the hashed password (`PasswordHasher<Account>`), `Role = Admin` and `Status = Active`.
6. Add both via `IOrganizationRepository.Add` and `IAccountRepository.Add`.
7. Call `NexoDbContext.SaveChangesAsync()` once, committing both inserts atomically.
8. Map the persisted `Organization` to `OrganizationDto` and return it (→ 201).

## Service logic (`OrganizationService.RegisterExternal`)

The web form sends the admin to `GET /auth/external/google?intent=register&planId=…&organizationName=…`. The provider handler carries those values through the handshake; `AuthController`'s callback then keeps the verified identity in the 5-minute `External` cookie and redirects to `/register/organization?planId=…&external=google` (or `…&error=oauth` without a verified email). The form reads `GET /auth/external/pending` (email, names from Google, organization name), shows the names for editing with the email read-only and no password, and posts `POST /auth/external/register` with `{ organizationName, adminFirstName, adminLastName, planId }` and the anti-forgery header.

1. Validate the organization name, both names, and `PlanId` as in `Register` (no email or password). Fail with a validation error (→ 400).
2. Require a verified provider email (→ 403 otherwise; the callback already filters this).
3. Resolve the plan by `PlanId` (→ 400 if unknown).
4. If the normalized provider email already has an account, fail with a conflict error (→ 409).
5. Create the `Organization`, an `Active` admin `Account` with the provider email, the confirmed names and no `PasswordHash`, and an `ExternalLogin` for the provider identity; save. A unique violation on the email or the provider identity is a conflict (→ 409).
6. Issue a session token; `AuthController` clears the `External` cookie, sets the session cookie, and answers 201 with `OrganizationDto`.

## Error handling

| Case | Detection point | Response |
| --- | --- | --- |
| Missing/invalid fields | Step 1 (service validation) | 400, field-level errors |
| Email already has an account | Step 2 (repository lookup) | 409, no write attempted; the same email in a different case counts as the same account |
| Missing or unknown `PlanId` | Step 1 or 3 (validation, plan lookup) | 400, field error on `PlanId`, no write attempted |
| Same email registered concurrently (unique-index violation on save) | Step 7 | 409, same as the step 2 conflict; no partial organization/account persisted |
| Other database failure on save | Step 7 | 500; no partial organization/account persisted, since both inserts are in one `SaveChangesAsync` |

## Related artifacts

[HLD](US-001-HLD.md), [SSD/SD diagrams](../us/US-001/README.md), [Domain model](../domain-models/README.md#accounts-and-organizations), tests ([`OrganizationMapperTests`](../../api/tests/Mappers/OrganizationMapperTests.cs), [`OrganizationsEndpointTests`](../../api/tests/Controllers/OrganizationsEndpointTests.cs)).
