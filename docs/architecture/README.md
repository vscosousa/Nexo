# Architecture

[Documentation index](../README.md)

This page distinguishes the implemented scaffold from the intended application structure.

## Context

Nexo is a modular monolith: a single React SPA and a single ASP.NET Core Web API, run locally on one machine. See [ADR-001](../decisions/ADR-001-frontend-backend-stack.md) and [ADR-002](../decisions/ADR-002-modular-monolith-architecture.md) for the reasoning.

## Components

The layers below exist in the weather sample. Organization, account, and resource classes shown in story designs are proposed; their business modules have not been implemented.

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

`web/` folder layout: `app/` (shell, router), `auth/` (session, guard, login), `shared/` (HTTP client, reusable UI), `features/<name>/` (one per business module, see [ADR-004](../decisions/ADR-004-frontend-architecture.md)).

`api/` folder layout: `Controllers/`, `Domain/Models/`, `Domain/Dtos/`, `Mappers/`, `Services/`, `Infrastructure/Repositories/`, `Infrastructure/Persistence/`, `Migrations/`.

## Deployment

The local stack starts with `docker compose up --build`: `web` runs Vite, `api` runs ASP.NET Core Kestrel, and `db` runs PostgreSQL 17 with a named data volume. The API waits for database health and applies migrations before serving HTTP. Images include source snapshots; rebuild after edits. Only frontend/API ports are published, on loopback. See [ADR-007](../decisions/ADR-007-local-docker-compose.md) for the setup rationale and review status.

Manual execution remains available through `npm run dev`, `dotnet run`, and a local PostgreSQL service. No hosted/remote deployment exists yet. See [ADR-003](../decisions/ADR-003-postgresql-database.md) for the database choice.

## Cross-cutting concerns

- **Authentication:** email/password and Google/Microsoft sign-in with an API-issued JWT are planned in [ADR-006](../decisions/ADR-006-authentication.md). The scaffold stores a token in `localStorage`; `auth/RequireAuth.tsx` checks its presence to control navigation. This is not server-side authorization or token validation. The login page is a placeholder, and backend authentication is not implemented.
- **Development HTTP routing:** the shared Axios client uses the relative `/api` base URL. A Vite API proxy is not configured yet, so API integration still needs routing configuration.
- **Configuration:** manual execution uses `dotnet user-secrets`; Compose supplies a connection string with disposable development credentials through the environment. See [technical reference](../reference/README.md#configuration).
- **Persistence:** EF Core against PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL`; schema changes go through EF Core Migrations (`api/Migrations/`).

## Risks and trade-offs

- The presence-only route guard is UI scaffolding. API authentication, authorization, JWT validation, and organization isolation still need implementation.
- The frontend `/api` base URL has no matching development proxy; the sample API route is `/WeatherForecast`.
- `NexoDbContext` has no business entities and the initial migration is empty. The weather repository generates data without database access.
- Story contracts have unresolved details, including registration sessions and plan-limit enforcement. See [design review gaps](../requirements/README.md#design-review-gaps) before implementation.
- Deployment, backup/recovery, and measurable performance targets are not defined for this local prototype.

Link to [decision records](../decisions/README.md), [sequence diagrams](../sd/README.md), and [database design](../database/README.md) for details.
