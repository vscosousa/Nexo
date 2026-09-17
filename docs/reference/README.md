# Technical reference

[Documentation index](../README.md)

Document exact supported versions, settings, commands, and interfaces here.

## Supported environment

| Dependency or platform | Supported version | Purpose |
| --- | --- | --- |
| Node.js | 24.x | Runs the frontend toolchain (Vite, TypeScript) |
| npm | 11.x | Frontend package management |
| .NET SDK | 10.x | Builds and runs the backend |
| PostgreSQL | 17.x | Relational database (local Windows service) |

## Key dependencies

| Package | Where | Purpose |
| --- | --- | --- |
| `react-router-dom` | `web/` | Client-side routing and the `RequireAuth` route guard |
| `axios` | `web/` | Shared HTTP client (`shared/http/client.ts`), attaches the auth token, handles `401` |
| `vitest`, `@testing-library/react` | `web/` | Frontend unit and component tests |
| `Microsoft.AspNetCore.Mvc.Testing` | `api.Tests/` | Backend integration tests (`WebApplicationFactory`) |
| `@playwright/test` | `e2e/` | End-to-end tests against the running app |

See [ADR-004](../decisions/ADR-004-frontend-architecture.md) and [ADR-005](../decisions/ADR-005-testing-frameworks.md) for why these were chosen over native alternatives.

## Configuration

| Setting | Description | Type | Required | Default |
| --- | --- | --- | --- | --- |
| `ConnectionStrings:NexoDb` | PostgreSQL connection string used by `NexoDbContext` | String | Yes | None, must be set locally |

The connection string is not stored in `appsettings.json` or `appsettings.Development.json`. Set it locally, once, from the `api/` directory:

```sh
dotnet user-secrets set "ConnectionStrings:NexoDb" "Host=localhost;Port=5432;Database=nexo;Username=<user>;Password=<password>"
```

Use dummy values above; set your own local PostgreSQL credentials. `dotnet user-secrets` stores the value outside the repository, keyed by the `UserSecretsId` in `Nexo.Api.csproj`.

## Commands

| Command | Working directory | Inputs | Result |
| --- | --- | --- | --- |
| `npm install` | `web/` | None | Installs frontend dependencies |
| `npm run dev` | `web/` | None | Starts the Vite dev server |
| `dotnet restore` | `api/` | None | Restores backend NuGet packages |
| `dotnet run` | `api/` | None | Starts the ASP.NET Core Web API |
| `dotnet tool restore` | `api/` | None | Restores local .NET tools (`dotnet-ef`) |
| `dotnet ef migrations add <Name>` | `api/` | Migration name | Generates a new EF Core migration in `Migrations/` |
| `dotnet ef database update` | `api/` | None | Applies pending migrations to the configured PostgreSQL database |
| `dotnet test Nexo.slnx` | repository root | None | Runs backend unit and integration tests |
| `npm test` | `web/` | None | Runs frontend unit and component tests |
| `npm install` | `e2e/` | None | Installs Playwright |
| `npx playwright install chromium` | `e2e/` | None | Downloads the Chromium browser used by tests |
| `npm test` | `e2e/` | None | Runs E2E tests (starts the Vite dev server automatically) |

## Interfaces

[Document authentication, operations, request and response schemas, examples, and errors when interfaces exist. Link machine-readable contracts when available.]

For step-by-step instructions, see [guides](../guides/README.md).
