# US-006 - LLD - Choose a plan before registering an organization

[Requirements](README.md) · [US-006](US-006-choose-plan-before-registering.md) · [HLD](US-006-HLD.md)

**Status:** implemented: `GET /plans` (backend and frontend) and the required `planId` on registration. See [design review gaps](README.md#design-review-gaps) for what is still open.

Full technical detail, building on the [HLD](US-006-HLD.md)'s contract and structure. See the [level 3 sequence diagram](../us/US-006/README.md#level-3---backend) for the call sequence.

## Domain

`Plan` is already defined (see [US-001's LLD](US-001-LLD.md#domain)); this story adds a read path, a DTO, and on `Plan`: a `ResourceLimit` field, a `MonthlyPrice`, and five feature flags (`HasIncidentTracking`, `HasExpenseTracking`, `HasDecisionHistory`, `HasAiInsights`, `HasPrioritySupport`) — migrations required — plus two more seeded tiers, so plans returned by `GET /plans` differ in limits, price, and included functionality, not only their name.

`Plan.ResourceLimit`: int, the maximum number of Resources the organization can register (mirrors `MemberLimit`'s shape). `Plan.Unlimited` (`int.MaxValue`) marks a limit as uncapped, used by the seeded "Enterprise" tier for both limits.

`Plan.MonthlyPrice`: `decimal?`, euros/month; null means a custom/"contact us" plan (the seeded "Enterprise" tier). Display-only — the requirement's scope note says plans are simulated, no real payment processing.

`Plan.Has*` flags: booleans marking which of the app's known (mostly not-yet-built) functionality areas the plan includes. Space & equipment bookings (Resource/Reservation/Loan, in scope from US-004 on) is included on every tier and so is not a flag; the flags cover Incident tracking, Expense tracking, Decision history (`HistoryEntry`), AI-powered insights, and priority support — none of these areas are implemented yet (see [domain model](../../domain-models/README.md)), so the flags are marketing/pricing-page data only, not an enforced entitlement check.

Seeded tiers:

| Tier | Member limit | Resource limit | Price | Incidents | Expenses | History | AI | Priority support |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Free | 20 | 10 | €0 | – | – | – | – | – |
| Team | 100 | 100 | €29/month | ✓ | ✓ | ✓ | – | – |
| Enterprise | Unlimited | Unlimited | null (contact us) | ✓ | ✓ | ✓ | ✓ | ✓ |

**`PlanDto`**

| Field | Type | Source |
| --- | --- | --- |
| `Id` | Guid | `Plan.Id` |
| `Name` | string | `Plan.Name` |
| `MemberLimit` | int | `Plan.MemberLimit` |
| `ResourceLimit` | int | `Plan.ResourceLimit` |
| `MonthlyPrice` | decimal? | `Plan.MonthlyPrice` |
| `HasIncidentTracking` | bool | `Plan.HasIncidentTracking` |
| `HasExpenseTracking` | bool | `Plan.HasExpenseTracking` |
| `HasDecisionHistory` | bool | `Plan.HasDecisionHistory` |
| `HasAiInsights` | bool | `Plan.HasAiInsights` |
| `HasPrioritySupport` | bool | `Plan.HasPrioritySupport` |

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

[HLD](US-006-HLD.md), [SSD/SD diagrams](../us/US-006/README.md), [US-001 LLD](US-001-LLD.md), tests: `PlansEndpointTests`, `OrganizationsEndpointTests` (chosen plan, missing or unknown `planId`), `RegisterForm.test.tsx`, `RequireSearchParam.test.tsx`.
