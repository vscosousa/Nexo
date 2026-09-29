# US-004 - LLD - Register a resource

[Requirements](README.md) · [US-004](US-004-register-resource.md) · [HLD](US-004-HLD.md)

**Status:** backend implemented; frontend not implemented.

Full technical detail, building on the [HLD](US-004-HLD.md)'s contract and structure. See the [level 3 sequence diagram](../us/US-004/README.md#level-3---backend) for the call sequence.

## Domain

Business rules (starting status, required fields) are defined once in the [domain model](../domain-models/README.md#resources); the tables below map fields to C# types only.

**`Resource`**

| Field | Type | Constraint |
| --- | --- | --- |
| `Id` | Guid | Generated on creation |
| `Name` | string | Required, non-empty, at most 200 characters, stored trimmed |
| `TypeId` | Guid | Foreign key to `ResourceType`; must be visible to the organization |
| `Description` | string, nullable | Optional, at most 2000 characters; blank is stored as null |
| `Status` | enum `ResourceStatus` (`Available`) | Starts `Available` |
| `OrganizationId` | Guid | Foreign key to `Organization`; taken from the caller's account, not from client input |

**`ResourceType`** ([ADR-012](../decisions/ADR-012-resource-types.md))

| Field | Type | Constraint |
| --- | --- | --- |
| `Id` | Guid | Fixed for seeded system types |
| `Name` | string | Required, at most 50 characters |
| `OrganizationId` | Guid, nullable | Null for a system type; the owning organization for a custom type |

Both entities have an EF global query filter on the caller's organization ([ADR-011](../decisions/ADR-011-multi-tenancy.md)): `Resource` rows of the caller's organization, and `ResourceType` rows that are system types or the caller's organization's.

## Service logic (`ResourceService.Register`)

1. `ResourcesController` requires a valid session (`[Authorize]`, → 401 otherwise) and reads the caller's account id from its `sub` claim. Call `IAccountRepository.GetByIdAsync(callerAccountId)`. If the caller is missing, not `Active`, or its `Role` is neither `Admin` nor `Staff`, fail with an authorization error (→ 403). This runs before validation, so a caller without permission learns nothing about the field rules.
2. Validate `RegisterResourceDto`: `Name` non-empty and at most 200 characters, `Description` at most 2000 characters, `TypeId` present and found by `IResourceRepository.FindTypeAsync` (the query filter hides other organizations' custom types, so they count as not found). Fail with a validation error (→ 400) otherwise.
3. In a transaction, lock the organization's row (`IOrganizationRepository.LockAsync`), load it with its plan, and count its resources (`IResourceRepository.CountByOrganizationAsync`). If the count has reached `Plan.ResourceLimit`, fail with a conflict error (→ 409). The lock stops two concurrent registrations from both taking the last slot.
4. Map the DTO and `caller.OrganizationId` to a new `Resource` (`ResourceMapper.ToResource`), with `Status = Available`.
5. Add via `IResourceRepository.Add`.
6. Call `NexoDbContext.SaveChangesAsync()` and commit.
7. Map the persisted `Resource` and its type to `ResourceDto` and return it (→ 201).

## Error handling

| Case | Detection point | Response |
| --- | --- | --- |
| Missing/invalid/expired session | Step 1 (controller `[Authorize]`) | 401, no write attempted |
| Caller's role is not `Admin` or `Staff` | Step 1 (authorization check) | 403, no write attempted |
| Missing/invalid fields, or a type the organization cannot use | Step 2 (service validation) | 400, field-level errors |
| Plan resource limit reached | Step 3 | 409, no write attempted |
| Database failure on save | Step 6 | 500; the transaction rolls back, no partial resource persisted |

## Related artifacts

[HLD](US-004-HLD.md), [SSD/SD diagrams](../us/US-004/README.md), [Domain model](../domain-models/README.md#resources), [backend tests](../testing/README.md) (`ResourcesEndpointTests`).
