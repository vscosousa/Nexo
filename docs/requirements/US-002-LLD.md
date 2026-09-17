# US-002 - LLD - Register a member's email to an organization

[Requirements](README.md) · [US-002](US-002-register-member-email.md) · [HLD](US-002-HLD.md)

Full technical detail, building on the [HLD](US-002-HLD.md)'s contract and structure. See the [level 3 sequence diagram](../us/US-002/README.md#level-3---sd-detailed) for the call sequence.

## Domain

**`EligibleEmail`**

| Field | Type | Rule |
| --- | --- | --- |
| `Id` | Guid | Generated on creation |
| `Email` | string | Required, valid format, unique across all accounts and eligible emails |
| `OrganizationId` | Guid | Foreign key to `Organization` |

## Service logic (`EligibleEmailService.Register`)

1. Validate `RegisterEligibleEmailDto`: `Email` non-empty and a valid email format. Fail with a validation error (→ 400) otherwise.
2. Call `IAccountRepository.GetByIdAsync(callerAccountId)`. If the caller's `Role` is not `Admin` or their `OrganizationId` does not match the route's `organizationId`, fail with an authorization error (→ 403).
3. Call `IOrganizationRepository.GetByIdAsync(organizationId)` for `MemberLimit`, and `IAccountRepository.CountByOrganizationAsync(organizationId)` for the current account count. If the count has reached `MemberLimit`, fail with a conflict error (→ 409); do not proceed.
4. Call `IAccountRepository.FindByEmailAsync(dto.Email)` and `IEligibleEmailRepository.FindByEmailAsync(dto.Email)`. If either finds a match, fail with a conflict error (→ 409); do not proceed.
5. Map the DTO and `organizationId` to a new `EligibleEmail` (`EligibleEmailMapper.ToEligibleEmail`).
6. Add via `IEligibleEmailRepository.Add`.
7. Call `NexoDbContext.SaveChangesAsync()`.
8. Map the persisted `EligibleEmail` to `EligibleEmailDto` and return it (→ 201).

## Error handling

| Case | Detection point | Response |
| --- | --- | --- |
| Missing/invalid email | Step 1 (service validation) | 400, field-level errors |
| Caller is not the organization's admin | Step 2 (authorization check) | 403, no write attempted |
| Organization's member limit reached | Step 3 (repository count) | 409, no write attempted |
| Email already an account or already eligible (this or another organization) | Step 4 (repository lookup) | 409, no write attempted |
| Database failure on save | Step 7 | 500; no partial state persisted |

## Related artifacts

[HLD](US-002-HLD.md), [SSD/SD diagrams](../us/US-002/README.md), [Domain model](../domain-models/README.md#accounts-and-organizations), tests (not yet created, required before implementation per [AGENTS.md](../../AGENTS.md#working-rules)).
