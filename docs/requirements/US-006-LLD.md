# US-006 - LLD - Choose a plan before registering an organization

[Requirements](README.md) · [US-006](US-006-choose-plan-before-registering.md) · [HLD](US-006-HLD.md)

**Status:** proposed; not implemented. See [design review gaps](README.md#design-review-gaps) before implementing this contract.

Full technical detail, building on the [HLD](US-006-HLD.md)'s contract and structure. See the [level 3 sequence diagram](../us/US-006/README.md#level-3---backend) for the call sequence.

## Domain

`Plan` is already defined (see [US-001's LLD](US-001-LLD.md#domain)); this story only adds a read path and a DTO. No new field or table.

**`PlanDto`**

| Field | Type | Source |
| --- | --- | --- |
| `Id` | Guid | `Plan.Id` |
| `Name` | string | `Plan.Name` |
| `MemberLimit` | int | `Plan.MemberLimit` |

## Service logic (`PlanService.GetAll`)

1. Call `IPlanRepository.GetAllAsync()`.
2. Map each `Plan` to `PlanDto` (`PlanMapper.ToDto`).
3. Return the list (→ 200 OK; an empty array if no plans are seeded).

## Impact on US-001 (`OrganizationService.Register`)

The current implementation always resolves `Plan.Free` (see [US-001-LLD](US-001-LLD.md#service-logic-organizationserviceregister)); this story replaces that lookup:

1. Validate `RegisterOrganizationDto`, now including `PlanId` (must be a non-empty guid). Fail with a validation error (→ 400) otherwise, same as today's other fields.
2. Call `IPlanRepository.GetByIdAsync(dto.PlanId)` instead of `FindByNameAsync(Plan.Free)`. If no plan matches, fail with a validation error on `planId` (→ 400).
3. Continue exactly as today: map the DTO and the resolved plan to a new `Organization`, etc.

## Error handling

| Case | Detection point | Response |
| --- | --- | --- |
| No plans seeded | `PlanService.GetAll`, step 1 | 200 OK, empty array (not an error) |
| Missing/unknown `planId` at registration | `OrganizationService.Register`, step 2 (impact above) | 400, field error on `planId` |

## Related artifacts

[HLD](US-006-HLD.md), [SSD/SD diagrams](../us/US-006/README.md), [US-001 LLD](US-001-LLD.md), tests (not yet created, required before implementation per [AGENTS.md](../../AGENTS.md#working-rules)).
