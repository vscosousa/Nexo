# US-006 - Choose a plan before registering an organization

[User story design index](../README.md) · [Requirement](../../requirements/US-006-choose-plan-before-registering.md) · [HLD](../../requirements/US-006-HLD.md) · [LLD](../../requirements/US-006-LLD.md)

## Implementation context

**Read:**

1. [Requirement](../../requirements/US-006-choose-plan-before-registering.md)
2. [LLD](../../requirements/US-006-LLD.md)
3. This file (diagrams and levels 1-3)
4. [ADR-002](../../decisions/ADR-002-modular-monolith-architecture.md), [ADR-004](../../decisions/ADR-004-frontend-architecture.md)
5. [Domain model - accounts and organizations](../../domain-models/README.md#accounts-and-organizations)
6. [US-001](../../requirements/US-001-create-organization-admin.md) (the form this story hands off to, and whose contract it changes)

**Do not read unless needed:** the full domain model, unrelated user stories, other ADRs, global sequence diagrams.

**Open decisions** (see [design-review gaps](../../requirements/README.md#design-review-gaps)):

- What happens when no plans are seeded.

Implementation must not assume answers to these.

**Status:** `GET /plans` (backend) and the plans page (frontend) are implemented. `ChoosePlanPage` is now the register-org entry point at `/register`; the organization/admin form moved to `/register/organization?planId=<id>`. Plans carry a display-only monthly price and five feature flags (incident tracking, expense tracking, decision history, AI-powered insights, priority support) alongside the member/resource limits, shown per card and in a full comparison table below the cards. `RegisterForm` submits the `planId` from its URL, `POST /organizations` requires it (400 if missing or unknown), and `/register/organization` without a `planId` redirects to `/register`. Still open: what happens with zero seeded plans (the page shows an empty message; no policy decision was made). Review the [open contract details](../../requirements/README.md#design-review-gaps) alongside these diagrams.

## Diagram scope

`GET /plans` is a plain read with no validation or authorization branch. The plan hand-off to the organization/admin form is shown as a URL-carried `planId`; that form's own submission (`POST /organizations`) is documented in [US-001](../US-001/README.md), not repeated here.

All diagrams are numbered and use explicit outcome branches where one exists. Backend SDs show input validation before reads, EF tracking separately from save, and persistence failure responses where the LLD defines them. Operation tables summarize the collaboration; their row numbers are not diagram message numbers.

## Level 1 - SSD

**Actor:** Prospective admin (no account yet).
**Preconditions:** none.
**Trigger:** The person opens the register-org link.

| Step | Actor input | System response |
| --- | --- | --- |
| 1 | Open the register-org link | Available plans shown (name, member limit) |
| 2 | Pick a plan and continue | Organization/admin form shown, with the plan carried through |

**Alternative and failure flows:** not yet defined (see [US-006 exceptions](../../requirements/US-006-choose-plan-before-registering.md)).
**Postconditions:** the person is on the organization/admin form with a plan selected; no organization exists yet.
**Diagram:** [![SSD](ssd/level-1/svg/US-006-level-1.svg)](ssd/level-1/puml/US-006-level-1.puml)

## Level 2 - SD (coarse)

**Participants:** `Web UI`, `Nexo API` (the backend, as a whole).
**Diagram:** [![SD level 2](sd/level-2/svg/US-006-level-2.svg)](sd/level-2/puml/US-006-level-2.puml)

## Level 3 - Backend

**Participants:** `Web App` (the frontend, as a whole), `PlansController`, `PlanService`, `IPlanRepository`, `PlanMapper`, `NexoDbContext`, `Database`.
**Diagram:** [![SD level 3 backend](sd/level-3/backend/svg/US-006-level-3-backend.svg)](sd/level-3/backend/puml/US-006-level-3-backend.puml)

| Step | Sender → receiver | Operation |
| --- | --- | --- |
| 1 | Web App → Controller | `GET /plans` |
| 2 | Service → PlanRepository | `GetAllAsync` |
| 3 | Service → Mapper | Map each `Plan` to `PlanDto` |

See the [LLD service logic](../../requirements/US-006-LLD.md#service-logic-planservicegetall) for what each step does. This diagram covers `GET /plans` only; the `planId` handling in `POST /organizations` is described in the [US-001 LLD](../../requirements/US-001-LLD.md).

## Level 3 - Frontend

**Participants:** `ChoosePlanPage`, `PlanPicker`, `PlanComparisonTable`, `authService`, `HttpClient`, `Nexo API` (the backend, as a whole). `PlanPicker` renders the cards (price, limits, included features, the middle tier by member limit as recommended); `PlanComparisonTable`, rendered below it, lists every plan's limits and feature flags side by side.
**Routes:** `ChoosePlanPage` is now `/register` (the register-org link); the organization/admin form (US-001's `RegisterPage`) moved to `/register/organization`, with `planId` carried in its query string. `RegisterForm` reads that `planId` and submits it with the registration; the route redirects to `/register` when it is missing.
**Diagram:** [![SD level 3 frontend](sd/level-3/frontend/svg/US-006-level-3-frontend.svg)](sd/level-3/frontend/puml/US-006-level-3-frontend.puml)

| Step | Sender → receiver | Operation | Outcome |
| --- | --- | --- | --- |
| 1 | Actor → View | Open the register-org link | View asks the service for plans |
| 2 | View → Service | `authService.listPlans()` | Feature module fetches available plans |
| 3 | Service → HttpClient | `GET /plans` | Shared Axios instance sends the request |
| 4 | HttpClient → Nexo API | HTTP request | Reaches the backend (detailed in [Level 3 - Backend](#level-3---backend)) |
| 5 | Actor → View | Pick a plan and continue | View reads the pick from the component and navigates, `planId` in the URL |

**Related design:** [Architecture](../../architecture/README.md), [Domain model](../../domain-models/README.md#accounts-and-organizations), [ADR-002](../../decisions/ADR-002-modular-monolith-architecture.md), [ADR-004](../../decisions/ADR-004-frontend-architecture.md).
