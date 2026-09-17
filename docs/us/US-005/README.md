# US-005 - Sign in to an existing account

[User story design index](../README.md) · [Requirement](../../requirements/US-005-sign-in.md) · [HLD](../../requirements/US-005-HLD.md) · [LLD](../../requirements/US-005-LLD.md)

## Level 1 - SSD

**Actor:** Account holder (admin or member, per [US-001](../../requirements/US-001-create-organization-admin.md) or [US-003](../../requirements/US-003-create-member-account.md)).
**Preconditions:** An account already exists.
**Trigger:** The account holder submits email and password.

| Step | Actor input | System response |
| --- | --- | --- |
| 1 | Email, password | Verifies credentials |
| 2 | n/a | Issues a session; signs the user in |

**Alternative and failure flows:** Incorrect password, or no matching account, reject without revealing which.
**Postconditions:** The user holds a valid session.
**Diagram:** [![SSD](ssd/level-1/svg/US-005-level-1.svg)](ssd/level-1/puml/US-005-level-1.puml)

## Level 2 - SD (coarse)

**Participants:** `Web UI`, `Nexo API` (the backend, as a whole).
**Diagram:** [![SD level 2](sd/level-2/svg/US-005-level-2.svg)](sd/level-2/puml/US-005-level-2.puml)

## Level 3 - Backend

**Participants:** `Web App` (the frontend, as a whole), `AuthController`, `AuthService`, `IAccountRepository`, `NexoDbContext`, `Database`, `PasswordHasher<Account>`, `ITokenService`.
**Diagram:** [![SD level 3 backend](sd/level-3/backend/svg/US-005-level-3-backend.svg)](sd/level-3/backend/puml/US-005-level-3-backend.puml)

| Step | Sender → receiver | Operation | Outcome |
| --- | --- | --- | --- |
| 1 | Web App → Controller | `POST /auth/sign-in` | Delegates to `AuthService.SignIn` |
| 2 | Service → AccountRepository → DbContext → Database | `FindByEmail` | Looks up the account; rejects if missing or SSO-only |
| 3 | Service → PasswordHasher | `VerifyHashedPassword` | Rejects on mismatch, generic error either way |
| 4 | Service → TokenService | `GenerateToken` | Issues the JWT |

**Failure handling:** Unknown email, SSO-only account, and wrong password all return the same generic 401, so as not to reveal which condition applied.
**Transaction boundaries:** Read-only; no persistence changes.

## Level 3 - Frontend

**Participants:** `LoginPage`, `SignInForm`, `authService`, `HttpClient`, `Nexo API` (the backend, as a whole).
**Diagram:** [![SD level 3 frontend](sd/level-3/frontend/svg/US-005-level-3-frontend.svg)](sd/level-3/frontend/puml/US-005-level-3-frontend.puml)

| Step | Sender → receiver | Operation | Outcome |
| --- | --- | --- | --- |
| 1 | Actor → View → Component | Submit email and password | View forwards the actor's input to the form component |
| 2 | Component → Service | `authService.signIn(email, password)` | Feature module builds the request |
| 3 | Service → HttpClient | `POST /auth/sign-in` | Shared Axios instance sends the request |
| 4 | HttpClient → Nexo API | HTTP request | Reaches the backend (detailed in [Level 3 - Backend](#level-3---backend)) |

**Failure handling:** A thrown 401 propagates back through the service to the form, which renders the same generic sign-in error either way.
**Related design:** [Architecture](../../architecture/README.md), [Domain model](../../domain-models/README.md#accounts-and-organizations), [ADR-006](../../decisions/ADR-006-authentication.md), [ADR-004](../../decisions/ADR-004-frontend-architecture.md).
