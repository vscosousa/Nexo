# US-002 - Register a member's email to an organization

[User story design index](../README.md) · [Requirement](../../requirements/US-002-register-member-email.md) · [HLD](../../requirements/US-002-HLD.md) · [LLD](../../requirements/US-002-LLD.md)

## Level 1 - SSD

**Actor:** Admin.
**Preconditions:** The admin has an account (per [US-001](../../requirements/US-001-create-organization-admin.md)) scoped to an organization.
**Trigger:** The admin submits a member's email to register.

| Step | Actor input | System response |
| --- | --- | --- |
| 1 | Member email | Validates the email, the admin's authorization, the active-member limit, and uniqueness |
| 2 | n/a | Creates a pending account for the email (`Status = Invited`) |

**Alternative and failure flows:** Invalid email, unauthorized caller, active-member limit reached, or email already registered elsewhere, reject with no change made.
**Postconditions:** A pending (`Invited`) account exists for the email, scoped to the organization, without credentials.
**Diagram:** [![SSD](ssd/level-1/svg/US-002-level-1.svg)](ssd/level-1/puml/US-002-level-1.puml)

## Level 2 - SD (coarse)

**Participants:** `Web UI`, `Nexo API` (the backend, as a whole).
**Diagram:** [![SD level 2](sd/level-2/svg/US-002-level-2.svg)](sd/level-2/puml/US-002-level-2.puml)

## Level 3 - Backend

**Participants:** `Web App` (the frontend, as a whole), `AccountInvitationsController`, `AccountInvitationService`, `IAccountRepository`, `IOrganizationRepository`, `AccountMapper`, `NexoDbContext`, `Database`.
**Diagram:** [![SD level 3 backend](sd/level-3/backend/svg/US-002-level-3-backend.svg)](sd/level-3/backend/puml/US-002-level-3-backend.puml)

| Step | Sender → receiver | Operation | Outcome |
| --- | --- | --- | --- |
| 1 | Web App → Controller | `POST /organizations/{organizationId}/invitations` | Delegates to `AccountInvitationService.Invite` |
| 2 | Service → AccountRepository | `GetById` | Checks the caller is the organization's admin |
| 3 | Service → OrganizationRepository, AccountRepository | `GetById`, `CountByOrganizationAndStatus(Active)` | Checks the active-member limit is not reached |
| 4 | Service → AccountRepository | `FindByEmail` | Checks the email has no account yet, `Invited` or `Active` |
| 5 | Service → Mapper → repository → DbContext → Database | `ToInvitedAccount`, `Add`, `SaveChangesAsync` | Persists the pending account |

**Failure handling:** Authorization, limit, and conflict checks each short-circuit before any persistence and return their respective error to the controller.
**Transaction boundaries:** The pending account is created in a single `SaveChangesAsync` call; no partial state is possible.

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
