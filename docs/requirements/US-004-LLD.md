# US-004 - LLD - Register a resource

[Requirements](README.md) · [US-004](US-004-register-resource.md) · [HLD](US-004-HLD.md)

**Status:** proposed; not implemented. See [design review gaps](README.md#design-review-gaps) before implementing this contract.

Full technical detail, building on the [HLD](US-004-HLD.md)'s contract and structure. See the [level 3 sequence diagram](../us/US-004/README.md#level-3---backend) for the call sequence.

## Domain

**`Resource`**

| Field | Type | Rule |
| --- | --- | --- |
| `Id` | Guid | Generated on creation |
| `Name` | string | Required, non-empty |
| `Type` | string | Required, non-empty |
| `Description` | string, nullable | Optional |
| `Status` | enum (`Available`, ...) | `Available` on creation |
| `OrganizationId` | Guid | Foreign key to `Organization`; taken from the caller's account, not from client input |

## Service logic (`ResourceService.Register`)

1. Validate `RegisterResourceDto`: `Name` and `Type` non-empty. Fail with a validation error (→ 400) otherwise.
2. Call `IAccountRepository.GetByIdAsync(callerAccountId)`. If the caller lacks permission to manage resources, fail with an authorization error (→ 403). The exact role rule for this permission is not yet decided (see [domain model](../domain-models/README.md#resources) open questions); this step is a placeholder for that check.
3. Map the DTO and `caller.OrganizationId` to a new `Resource` (`ResourceMapper.ToResource`), with `Status = Available`.
4. Add via `IResourceRepository.Add`.
5. Call `NexoDbContext.SaveChangesAsync()`.
6. Map the persisted `Resource` to `ResourceDto` and return it (→ 201).

## Error handling

| Case | Detection point | Response |
| --- | --- | --- |
| Missing/invalid fields | Step 1 (service validation) | 400, field-level errors |
| Caller lacks permission to manage resources | Step 2 (authorization check) | 403, no write attempted |
| Database failure on save | Step 5 | 500; no partial resource persisted |

## Related artifacts

[HLD](US-004-HLD.md), [SSD/SD diagrams](../us/US-004/README.md), [Domain model](../domain-models/README.md#resources), tests (not yet created, required before implementation per [AGENTS.md](../../AGENTS.md#working-rules)).
