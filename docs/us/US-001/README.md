# US-001 - Create an organization and its admin account

[User story design index](../README.md) · [Requirement](../../requirements/US-001-create-organization-admin.md) · [HLD](../../requirements/US-001-HLD.md) · [LLD](../../requirements/US-001-LLD.md)

## Level 1 - SSD

**Actor:** Prospective admin (no account yet).
**Preconditions:** None; this is the entry point.
**Trigger:** The prospective admin submits organization and admin account details.

| Step | Actor input | System response |
| --- | --- | --- |
| 1 | Organization name, admin email, admin name | Validates details and email uniqueness |
| 2 | n/a | Creates the organization (Free plan) and the admin account, signs the admin in |

**Alternative and failure flows:** Missing/invalid fields, or email already in use, reject with no organization or account created.
**Postconditions:** Organization and admin account exist; the admin is signed in.
**Diagram:** [![SSD](ssd/level-1/svg/US-001-level-1.svg)](ssd/level-1/puml/US-001-level-1.puml)

## Level 2 - SD (coarse)

**Participants:** `Web UI`, `Nexo API` (the backend, as a whole).
**Diagram:** [![SD level 2](sd/level-2/svg/US-001-level-2.svg)](sd/level-2/puml/US-001-level-2.puml)

## Level 3 - Backend

**Participants:** `Web App` (the frontend, as a whole), `OrganizationsController`, `OrganizationService`, `IAccountRepository`, `IOrganizationRepository`, `OrganizationMapper`, `NexoDbContext`, `Database`.
**Diagram:** [![SD level 3 backend](sd/level-3/backend/svg/US-001-level-3-backend.svg)](sd/level-3/backend/puml/US-001-level-3-backend.puml)

| Step | Sender → receiver | Operation | Outcome |
| --- | --- | --- | --- |
| 1 | Web App → Controller | `POST /organizations` | Delegates to `OrganizationService.Register` |
| 2 | Service → AccountRepository | `FindByEmail` | Checks the admin email is not already in use |
| 3 | Service → Mapper | `ToOrganization`, `ToAdminAccount` | Builds the domain `Organization` (Free plan) and admin `Account` |
| 4 | Service → repositories → DbContext → Database | `Add`, `SaveChangesAsync` | Persists both in one transaction |

**Failure handling:** An email already in use short-circuits before any persistence and returns a conflict to the controller.
**Transaction boundaries:** Organization and admin account are created together in a single `SaveChangesAsync` call; no partial state is possible.

## Level 3 - Frontend

**Participants:** `RegisterOrganizationPage`, `RegisterOrganizationForm`, `organizationsService`, `HttpClient`, `Nexo API` (the backend, as a whole).
**Diagram:** [![SD level 3 frontend](sd/level-3/frontend/svg/US-001-level-3-frontend.svg)](sd/level-3/frontend/puml/US-001-level-3-frontend.puml)

| Step | Sender → receiver | Operation | Outcome |
| --- | --- | --- | --- |
| 1 | Actor → View → Component | Submit organization and admin details | View forwards the actor's input to the form component |
| 2 | Component → Service | `organizationsService.register(...)` | Feature module builds the request |
| 3 | Service → HttpClient | `POST /organizations` | Shared Axios instance sends the request |
| 4 | HttpClient → Nexo API | HTTP request | Reaches the backend (detailed in [Level 3 - Backend](#level-3---backend)) |

**Failure handling:** A thrown error from the API call propagates back through the service to the component, which renders the validation or conflict message.
**Related design:** [Architecture](../../architecture/README.md), [Domain model](../../domain-models/README.md#accounts-and-organizations), [ADR-002](../../decisions/ADR-002-modular-monolith-architecture.md), [ADR-004](../../decisions/ADR-004-frontend-architecture.md).
