# US-003 - Create a member account

[User story design index](../README.md) · [Requirement](../../requirements/US-003-create-member-account.md) · [HLD](../../requirements/US-003-HLD.md) · [LLD](../../requirements/US-003-LLD.md)

**Status:** proposed design; not implemented. Review the [open contract details](../../requirements/README.md#design-review-gaps) alongside these diagrams.

## Diagram scope

The password scenario updates the existing Invited account. The mapper uses PasswordHasher<Account> as specified in the LLD. The SSD includes required sign-in, but SDs stop at AccountDto because session delivery and OAuth endpoints remain unspecified.

All diagrams are numbered and use explicit outcome branches. Backend SDs show input validation before reads, EF tracking separately from save, and persistence failure responses where the LLD defines them. Operation tables summarize the collaboration; their row numbers are not diagram message numbers.

## Level 1 - SSD

**Actor:** Invited person (has a pending, `Invited` account, per [US-002](../../requirements/US-002-register-member-email.md)).
**Preconditions:** The person's email was registered to an organization (per [US-002](../../requirements/US-002-register-member-email.md)) and its account has not been activated yet.
**Trigger:** The person submits account details.

| Step | Actor input | System response |
| --- | --- | --- |
| 1 | Email, name, password | Existing account activated and signed in, or activation rejected |

**Alternative and failure flows:** No account exists for the email, or it is already active, reject with no change made.
**Postconditions:** The account is `Active`, scoped to the organization with the `Member` role; the person is signed in.
**Diagram:** [![SSD](ssd/level-1/svg/US-003-level-1.svg)](ssd/level-1/puml/US-003-level-1.puml)

## Level 2 - SD (coarse)

**Participants:** `Web UI`, `Nexo API` (the backend, as a whole).
**Diagram:** [![SD level 2](sd/level-2/svg/US-003-level-2.svg)](sd/level-2/puml/US-003-level-2.puml)

## Level 3 - Backend

**Participants:** `Web App` (the frontend, as a whole), `AccountActivationsController`, `AccountActivationService`, `IAccountRepository`, `AccountMapper`, `PasswordHasher<Account>`, `NexoDbContext`, `Database`.
**Diagram:** [![SD level 3 backend](sd/level-3/backend/svg/US-003-level-3-backend.svg)](sd/level-3/backend/puml/US-003-level-3-backend.puml)

| Step | Sender → receiver | Operation | Outcome |
| --- | --- | --- | --- |
| 1 | Web App → Controller | `POST /accounts/activation` | Delegates to `AccountActivationService.Activate` |
| 2 | Service → AccountRepository | `FindByEmailAsync` | Loads the pending account for the email |
| 3 | Service → Mapper | `ApplyActivation` | Sets name and hashed password, flips `Status` to `Active` |
| 4 | Service → DbContext → Database | `SaveChangesAsync` | Persists the mutation on the existing row |

**Failure handling:** No account for the email, or an already-`Active` account, short-circuits before any persistence and returns its respective error to the controller.
Validation errors return 400 before repository access. A failed save returns 500 with no partial persisted write, as specified in the LLD; the detailed error body remains unspecified. The diagrams show response mapping only after a successful save.

**Transaction boundaries:** The activation is a single-row update in one `SaveChangesAsync` call.

## Level 3 - Frontend

**Participants:** `ActivateAccountPage`, `ActivateAccountForm`, `accountsService`, `HttpClient`, `Nexo API` (the backend, as a whole).
**Diagram:** [![SD level 3 frontend](sd/level-3/frontend/svg/US-003-level-3-frontend.svg)](sd/level-3/frontend/puml/US-003-level-3-frontend.puml)

| Step | Sender → receiver | Operation | Outcome |
| --- | --- | --- | --- |
| 1 | Actor → View → Component | Submit email, name, and password | View forwards the actor's input to the form component |
| 2 | Component → Service | `accountsService.activate(email, name, password)` | Feature module builds the request |
| 3 | Service → HttpClient | `POST /accounts/activation` | Shared Axios instance sends the request |
| 4 | HttpClient → Nexo API | HTTP request | Reaches the backend (detailed in [Level 3 - Backend](#level-3---backend)) |

**Failure handling:** A thrown 403/409 propagates back through the service to the form, which renders the eligibility or conflict message.
**Related design:** [Architecture](../../architecture/README.md), [Domain model](../../domain-models/README.md#accounts-and-organizations), [ADR-006](../../decisions/ADR-006-authentication.md), [ADR-004](../../decisions/ADR-004-frontend-architecture.md).
