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
| `Role` | enum (`Admin`, `Member`, `Staff`) | `Admin` when created via this feature |
| `Status` | enum (`Invited`, `Active`, `Unverified`) | `Unverified` for an admin registered with a password until the emailed link confirms the address, then `Active`; `Active` immediately for an admin registered with Google. See the domain model for the member lifecycle and limit counting |
| `InvitationTokenHash`, `InvitationExpiresAt` | string / timestamp, nullable | For an `Unverified` admin: SHA-256 of the email confirmation link's token and its expiry (24 hours); cleared on confirmation |
| `OrganizationId` | Guid | Foreign key to `Organization` |

## Service logic (`OrganizationService.Register`)

1. Validate `RegisterOrganizationDto`: `OrganizationName`, `AdminFirstName`, `AdminLastName` non-empty and at most 100 characters each; `AdminEmail` a valid email format of at most 320 characters; `Password` present and satisfying the [password rules](US-003-LLD.md#password-rules) with the organization and admin names as forbidden content. Fail with a validation error (→ 400) otherwise.
2. Call `IPlanRepository.GetByIdAsync(dto.PlanId)`. If no plan matches, fail with a validation error on `PlanId` (→ 400); a missing `PlanId` already failed step 1 ([US-006](US-006-LLD.md)).
3. Create a confirmation link token (`InvitationTokens.CreateLinkToken`), then map the DTO and the plan to a new `Organization` (`OrganizationMapper.ToOrganization`) and a new `Account` (`OrganizationMapper.ToAdminAccount`), with the normalized email (`Account.NormalizeEmail`: trim, lowercase), trimmed names, the hashed password (`PasswordHasher<Account>`), `Role = Admin`, `Status = Unverified`, the token's hash, and an expiry of now + `InvitationTokens.ConfirmationLifetime` (24 hours). The password is hashed before the lookup below, so both outcomes take about the same time.
4. Call `IAccountRepository.FindByEmailAsync(email)`. If an account exists and is not an `Unverified` one whose link has expired, email the owner `AccountEmails.AlreadyRegistered` and stop (→ 202, nothing written). If it is an expired `Unverified` registration, mark it and its organization for deletion; the new registration replaces it in the same save.
5. Add the organization and account and call `NexoDbContext.SaveChangesOrConflictAsync` once, committing the inserts (and any replacement) atomically.
6. Email the confirmation link (`AccountEmails.Confirmation`, `/confirm-email?email=…&token=…`). If sending fails, delete the organization and account again and fail (→ 500).
7. Respond 202 with no body.

## Service logic (`AccountActivationService.ConfirmEmail`)

`POST /accounts/activation/confirm-email` with `{ email, token }`, sent by the web app's `/confirm-email` page as soon as it opens.

1. Find the account by normalized email. If there is none, it is not `Unverified`, the token does not match `InvitationTokenHash` (constant-time comparison), or `InvitationExpiresAt` has passed, fail with a forbidden error (→ 403, the same for every cause).
2. Set `Status = Active`, clear the token hash and expiry, and save (→ 200). The admin then signs in ([US-005](US-005-LLD.md)).

## Service logic (`OrganizationService.RegisterExternal`)

The web form sends the admin to `GET /auth/external/google?intent=register&planId=…&organizationName=…`. The provider handler carries those values through the handshake; `AuthController`'s callback then keeps the verified identity in the 5-minute `External` cookie and redirects to `/register/organization?planId=…&external=google` (or `…&error=oauth` without a verified email). The form reads `GET /auth/external/pending` (email, names from Google, organization name), shows the names for editing with the email read-only and no password, and posts `POST /auth/external/register` with `{ organizationName, adminFirstName, adminLastName, planId }` and the anti-forgery header.

1. Validate the organization name, both names, and `PlanId` as in `Register` (no email or password). Fail with a validation error (→ 400).
2. Require a verified provider email (→ 403 otherwise; the callback already filters this).
3. Resolve the plan by `PlanId` (→ 400 if unknown).
4. If the normalized provider email already has an account, fail with a conflict error (→ 409), unless it is `Unverified`: then mark it and its organization for deletion, since the verified provider email proves the ownership that the unconfirmed registration never did.
5. Create the `Organization`, an `Active` admin `Account` with the provider email, the confirmed names and no `PasswordHash`, and an `ExternalLogin` for the provider identity; save. A unique violation on the email or the provider identity is a conflict (→ 409).
6. Email the admin a welcome (`AccountEmails.Welcome`: the organization name and a sign-in link, no token), best-effort: a delivery failure is ignored, since the organization is already saved and the admin is signed in. There is nothing to confirm, because the provider verified the email.
7. Issue a session token; `AuthController` clears the `External` cookie, sets the session cookie, and answers 201 with `OrganizationDto`.

## Error handling

| Case | Detection point | Response |
| --- | --- | --- |
| Missing/invalid fields | Step 1 (service validation) | 400, field-level errors |
| Email already has an account | Step 4 (repository lookup) | 202, no write attempted, the owner is emailed a notice; the same email in a different case counts as the same account |
| Missing or unknown `PlanId` | Step 1 or 2 (validation, plan lookup) | 400, field error on `PlanId`, no write attempted |
| Same email registered concurrently (unique-index violation on save) | Step 5 | 409; no partial organization/account persisted |
| Other database failure on save | Step 5 | 500; no partial organization/account persisted, since both inserts are in one save |
| Confirmation email cannot be sent | Step 6 | 500; the organization and account are deleted again |
| Too many requests from one address | Rate limiter, before the action | 429 (`RateLimit:PublicPermitLimit` per minute, default 10) |

## Related artifacts

[HLD](US-001-HLD.md), [SSD/SD diagrams](../us/US-001/README.md), [Domain model](../domain-models/README.md#accounts-and-organizations), [ADR-010](../decisions/ADR-010-account-security-hardening.md), tests ([`OrganizationMapperTests`](../../api/tests/Mappers/OrganizationMapperTests.cs), [`OrganizationsEndpointTests`](../../api/tests/Controllers/OrganizationsEndpointTests.cs)).
