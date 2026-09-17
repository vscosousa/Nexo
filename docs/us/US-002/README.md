# US-002 - Register a member's email to an organization

[User story design index](../README.md) · [Requirement](../../requirements/US-002-register-member-email.md) · [HLD](../../requirements/US-002-HLD.md) · [LLD](../../requirements/US-002-LLD.md)

## Implementation context

**Read:**

1. [Requirement](../../requirements/US-002-register-member-email.md)
2. [LLD](../../requirements/US-002-LLD.md)
3. This file (diagrams and levels 1-3)
4. [ADR-002](../../decisions/ADR-002-modular-monolith-architecture.md), [ADR-004](../../decisions/ADR-004-frontend-architecture.md)
5. [Domain model - accounts and organizations](../../domain-models/README.md#accounts-and-organizations)
6. [US-001](../../requirements/US-001-create-organization-admin.md) (precondition: admin account exists)

**Do not read unless needed:** the full domain model, unrelated user stories, other ADRs, global sequence diagrams.

**Open decisions** (see [design-review gaps](../../requirements/README.md#design-review-gaps)):

- Whether the plan limit counts active members or all active accounts.
- Concurrent activations against the limit are unspecified.

Implementation must not assume answers to these; the diagrams check the Active count only as currently drawn.

**Status:** proposed design; not implemented. Review the [open contract details](../../requirements/README.md#design-review-gaps) alongside these diagrams.

## Diagram scope

The invitation design checks the current Active count before creating an Invited account; see the [plan-limit gap](../../requirements/README.md#design-review-gaps) for what remains open.

All diagrams are numbered and use explicit outcome branches. Backend SDs show input validation before reads, EF tracking separately from save, and persistence failure responses where the LLD defines them. Operation tables summarize the collaboration; their row numbers are not diagram message numbers.

## Level 1 - SSD

**Actor:** Admin.
**Preconditions:** The admin has an account (per [US-001](../../requirements/US-001-create-organization-admin.md)) scoped to an organization.
**Trigger:** The admin submits a member's email to register.

| Step | Actor input | System response |
| --- | --- | --- |
| 1 | Member email | Pending member account created, or validation/authorization/limit/conflict error |

**Alternative and failure flows:** Invalid email, unauthorized caller, active-member limit reached, or email already registered elsewhere, reject with no change made.
**Postconditions:** A pending (`Invited`) account exists for the email, scoped to the organization, without credentials.
**Diagram:** [![SSD](ssd/level-1/svg/US-002-level-1.svg)](ssd/level-1/puml/US-002-level-1.puml)

## Level 2 - SD (coarse)

**Participants:** `Web UI`, `Nexo API` (the backend, as a whole).
**Diagram:** [![SD level 2](sd/level-2/svg/US-002-level-2.svg)](sd/level-2/puml/US-002-level-2.puml)

## Level 3 - Backend

**Participants:** `Web App` (the frontend, as a whole), `AccountInvitationsController`, `AccountInvitationService`, `IAccountRepository`, `IOrganizationRepository`, `AccountMapper`, `NexoDbContext`, `Database`.
**Diagram:** [![SD level 3 backend](sd/level-3/backend/svg/US-002-level-3-backend.svg)](sd/level-3/backend/puml/US-002-level-3-backend.puml)

| Step | Sender → receiver | Operation |
| --- | --- | --- |
| 1 | Web App → Controller | `POST /organizations/{organizationId}/invitations` |
| 2 | Service → AccountRepository | `GetByIdAsync` |
| 3 | Service → OrganizationRepository, AccountRepository | `GetByIdAsync`, `CountByOrganizationAndStatusAsync(Active)` |
| 4 | Service → AccountRepository | `FindByEmailAsync` |
| 5 | Service → Mapper → repository → DbContext → Database | `ToInvitedAccount`, `Add`, `SaveChangesAsync` |

See the [LLD service logic](../../requirements/US-002-LLD.md#service-logic-accountinvitationserviceinvite) for what each step does and its [error handling](../../requirements/US-002-LLD.md#error-handling) for failure responses. The diagrams show response mapping only after a successful save.

## Level 3 - Frontend

**Participants:** `InviteMemberPage`, `InviteMemberForm`, `invitationsService`, `HttpClient`, `Nexo API` (the backend, as a whole).
**Diagram:** [![SD level 3 frontend](sd/level-3/frontend/svg/US-002-level-3-frontend.svg)](sd/level-3/frontend/puml/US-002-level-3-frontend.puml)

| Step | Sender → receiver | Operation | Outcome |
| --- | --- | --- | --- |
| 1 | Actor → View → Component | Submit member email | View forwards the actor's input to the form component |
| 2 | Component → Service | `invitationsService.invite(organizationId, email)` | Feature module builds the request |
| 3 | Service → HttpClient | `POST /organizations/{organizationId}/invitations` | Shared Axios instance sends the request with the caller's token |
| 4 | HttpClient → Nexo API | HTTP request | Reaches the backend (detailed in [Level 3 - Backend](#level-3---backend)) |

**Failure handling:** A thrown 403/409 propagates back through the service to the form, which renders the authorization or conflict message.
**Related design:** [Architecture](../../architecture/README.md), [Domain model](../../domain-models/README.md#accounts-and-organizations), [ADR-002](../../decisions/ADR-002-modular-monolith-architecture.md), [ADR-004](../../decisions/ADR-004-frontend-architecture.md).
