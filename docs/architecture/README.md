# Architecture

[Documentation index](../README.md)

Describe the system structure and the constraints that shape it.

## Context

Nexo is a modular monolith: a single React SPA and a single ASP.NET Core Web API, run locally on one machine. See [ADR-001](../decisions/ADR-001-frontend-backend-stack.md) and [ADR-002](../decisions/ADR-002-modular-monolith-architecture.md) for the reasoning.

## Components

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

Both projects run as separate local processes during development: `web` via `npm run dev` (Vite dev server) and `api` via `dotnet run` (ASP.NET Core Kestrel), with PostgreSQL running as a local Windows service. No hosted/remote deployment exists yet. See [ADR-003](../decisions/ADR-003-postgresql-database.md) for why the database choice keeps a remote deployment possible later without a data-layer rewrite.

## Cross-cutting concerns

- **Authentication:** synthetic identities per the requirements; token-based, since the SPA and API are different origins (CORS applies). The frontend guard (`auth/RequireAuth.tsx`) protects routes; the login page is a placeholder pending a backend authentication endpoint.
- **Configuration:** local secrets (e.g., the PostgreSQL connection string) are stored via `dotnet user-secrets`, not in `appsettings.json`. See [technical reference](../reference/README.md#configuration).
- **Persistence:** EF Core against PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL`; schema changes go through EF Core Migrations (`api/Migrations/`).

## Risks and trade-offs

[Record known limitations, unresolved questions, and mitigation plans.]

Link to [decision records](../decisions/README.md), [sequence diagrams](../sd/README.md), and [database design](../database/README.md) for details.
