# Architecture

[Documentation index](../README.md)

This page distinguishes the implemented backend and frontend scaffold from the intended application structure.

## Context

Nexo is a modular monolith: a single React SPA and a single ASP.NET Core Web API, run locally on one machine. See [ADR-001](../decisions/ADR-001-frontend-backend-stack.md) and [ADR-002](../decisions/ADR-002-modular-monolith-architecture.md) for the reasoning.

## Components

The layers below exist in the US-001 to US-003 backend (organizations, account invitations and activation, with `Organization`, `Account`, and `Plan`), and in US-005 sign-in (`AuthController`, `AuthService`, `TokenService`, `ExternalLogin`; frontend `auth/`). US-001 (register) and US-003 (activate) also have a frontend, in `auth/` alongside sign-in; US-002 (invite a member) has no UI, only the API. US-004 (register a resource) is implemented on both sides: `ResourcesController`, `ResourceTypesController`, `ResourceService`, and `IResourceRepository` in the API, and `features/resources/` under the signed-in app shell (`app/AppLayout`, `app/DashboardPage`) in the web app.

| Component | Responsibility | Dependencies | Interface |
| --- | --- | --- | --- |
| `web` `app/` | App shell, routing, route guards | `web` `auth/`, `features/*` | React Router |
| `web` `auth/` | Session state, login, route guard (`RequireAuth`) | `web` `shared/http` | React Context |
| `web` `shared/http` | Single Axios instance; attaches auth token, handles 401 | `api` Controllers | JSON over HTTP |
| `web` `features/<name>/` | Per-module UI, API calls, types (created as modules are built) | `web` `shared/http` | JSON over HTTP |
| `api` Controllers | HTTP endpoints, request/response shaping | `api` Services | Web API (JSON) |
| `api` Services | Application logic | `api` Infrastructure/Repositories, Mappers | Internal (C# interfaces) |
| `api` Infrastructure/Repositories | Data access | `api` Infrastructure/Persistence | Internal (C# interfaces) |
| `api` Infrastructure/Persistence | EF Core `DbContext`, migrations | PostgreSQL | SQL (Npgsql) |
| `api` Mappers | Domain model ↔ DTO conversion | `api` Domain/Models, Domain/Dtos | Internal (static methods) |
| `api` Infrastructure/Email | Sends outgoing email (`IEmailSender`, `SmtpEmailSender`) using the `Email` settings | SMTP server (fake `mail` service or a real provider) | SMTP |
| `tools/fake-smtp` | Local fake SMTP server: stores received mail as `.eml` files and serves a webmail-style inbox | Host directory `MAIL_DATA_DIR` | SMTP (1025), HTTP (8025) |

`web/` folder layout: `app/` (shell, router), `auth/` (session, guard, login), `shared/` (HTTP client, reusable UI), `features/<name>/` (one per business module, see [ADR-004](../decisions/ADR-004-frontend-architecture.md)).

`api/` folder layout: `Controllers/`, `Domain/Models/`, `Domain/Dtos/`, `Mappers/`, `Services/`, `Infrastructure/Repositories/`, `Infrastructure/Persistence/`, `Migrations/`.

## Deployment

The local stack starts with `docker compose up --build`: `web` runs Vite, `api` runs ASP.NET Core Kestrel, `db` runs PostgreSQL 17 with a named data volume, and `mail` runs the [fake SMTP server](../guides/fake-mail-server.md) that receives the API's emails (stored in the git-ignored `.data/mail` folder). The API waits for database health and applies migrations before serving HTTP. Images include source snapshots; rebuild after edits. Only the frontend, API, and mail ports are published, on loopback. See [ADR-007](../decisions/ADR-007-local-docker-compose.md) for the setup rationale and review status.

Manual execution remains available through `npm run dev`, `dotnet run`, and a local PostgreSQL service. No hosted/remote deployment exists yet. See [ADR-003](../decisions/ADR-003-postgresql-database.md) for the database choice.

## Cross-cutting concerns

- **Authentication:** email/password and Google/Microsoft sign-in with an API-issued JWT are planned in [ADR-006](../decisions/ADR-006-authentication.md). The API issues the JWT at `POST /auth/sign-in` and after Google sign-in (US-005), and validates it via JWT bearer middleware; `AccountInvitationsController` is the first protected endpoint (`[Authorize]`, caller read from the `sub` claim). The token travels in the httpOnly `nexo_session` cookie (bearer headers still work for non-browser clients), and cookie-authenticated changes must carry the anti-forgery token from `GET /auth/csrf`, per [ADR-009](../decisions/ADR-009-session-cookie.md). The web app keeps no token: `AuthProvider` asks `GET /auth/me` on load, and `auth/RequireAuth.tsx` redirects to `/login` when there is no session.
- **Development HTTP routing:** the shared Axios client uses the relative `/api` base URL. A Vite API proxy is not configured yet, so API integration still needs routing configuration.
- **Configuration:** manual execution uses `dotnet user-secrets`; Compose supplies a connection string with disposable development credentials through the environment. See [technical reference](../reference/README.md#configuration).
- **Email:** the API sends through SMTP using the `Email` settings; Compose points them at the fake `mail` service, and configuration alone repoints them at a real provider. See [ADR-008](../decisions/ADR-008-fake-smtp-server.md).
- **Persistence:** EF Core against PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL`; schema changes go through EF Core Migrations (`api/Migrations/`).

## Risks and trade-offs

- The presence-only route guard is UI scaffolding; the frontend does not yet attach the stored JWT to its own authorization decisions beyond navigation.
- `NexoDbContext` holds `Plans`, `Organizations`, `Accounts`, `ExternalLogins`, `ResourceTypes`, and `Resources`. All organizations share one schema; `Resources` and `ResourceTypes` have EF query filters on the signed-in caller's organization (read from `IHttpContextAccessor`), while `Accounts` is still scoped by hand in each service ([ADR-011](../decisions/ADR-011-multi-tenancy.md)).
- Story contracts have unresolved details, including registration sessions and plan-limit enforcement. See [design review gaps](../requirements/README.md#design-review-gaps) before implementation.
- `api/Dockerfile` builds a production image (Release, non-root, migrations as a separate `efbundle` step; see [ADR-010](../decisions/ADR-010-account-security-hardening.md)), but hosting, backup/recovery, and measurable performance targets are not defined for this prototype.
- Each authenticated request reads the account's `SessionVersion` so sign-out and lockout end sessions immediately; this costs one primary-key query per request.

Link to [decision records](../decisions/README.md), [sequence diagrams](../sd/README.md), and [database design](../database/README.md) for details.
