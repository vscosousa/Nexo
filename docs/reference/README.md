# Technical reference

[Documentation index](../README.md)

Commands and configuration below describe the current scaffold. Proposed business endpoints remain in the stories' HLD documents.

## Supported environment

| Dependency or platform | Supported version | Purpose |
| --- | --- | --- |
| Docker Desktop + Compose | Linux containers, Compose v2 | Runs the full local stack without host Node.js, .NET, or PostgreSQL |
| Node.js | 24.x | Runs the frontend toolchain (Vite, TypeScript) |
| npm | 11.x | Frontend package management |
| .NET SDK | 10.x | Builds and runs the backend |
| PostgreSQL | 17.x | Relational database (Compose container or local service) |
| Git + Bash | Git Bash on Windows | Local pre-commit hook and its regression tests |
| PowerShell | 7+ | Diagram generator and its tests |
| Java + Graphviz | Available on `PATH` | PlantUML rendering; see [diagram tooling](../../libs/README.md) for the pinned JAR |

## Key dependencies

| Package | Where | Purpose |
| --- | --- | --- |
| `react-router-dom` | `web/` | Client-side routing and the `RequireAuth` route guard |
| `axios` | `web/` | Shared HTTP client (`shared/http/client.ts`), attaches the auth token, handles `401` |
| `vitest`, `@testing-library/react` | `web/` | Frontend unit and component tests |
| `Microsoft.AspNetCore.Mvc.Testing` | `api.Tests/` | Backend integration tests (`WebApplicationFactory`) |
| `@playwright/test` | `e2e/` | End-to-end tests against the running app |
| `oxlint` | `web/` | Frontend linting |
| `prettier` | `web/` | Frontend formatting |

See [ADR-004](../decisions/ADR-004-frontend-architecture.md) and [ADR-005](../decisions/ADR-005-testing-frameworks.md) for why these were chosen over native alternatives.

## Configuration

| Setting | Description | Type | Required | Default |
| --- | --- | --- | --- | --- |
| `ConnectionStrings:NexoDb` | PostgreSQL connection string used by `NexoDbContext` | String | Yes | None, must be set locally |

With Docker, [compose.yaml](../../compose.yaml) supplies `ConnectionStrings__NexoDb` using the internal `db:5432` address. PostgreSQL creates the `nexo` database and user on first startup, using disposable development credentials. Data lives in the `postgres-data` named volume; the database port is not published. The API runs in Development on container port 8080, published at `127.0.0.1:5122`; Vite is published at `127.0.0.1:5173`. No host user secrets are mounted.

For manual execution, the connection string is not stored in `appsettings.json` or `appsettings.Development.json`. Set it locally, once, from the `api/` directory:

```sh
dotnet user-secrets set "ConnectionStrings:NexoDb" "Host=localhost;Port=5432;Database=nexo;Username=<user>;Password=<password>"
```

Use dummy values above; set your own local PostgreSQL credentials. `dotnet user-secrets` stores the value outside the repository, keyed by the `UserSecretsId` in `Nexo.Api.csproj`.

## Commands

| Command | Working directory | Inputs | Result |
| --- | --- | --- | --- |
| `docker compose up --build` | repository root | Running Docker Desktop (Linux containers) | Builds and starts frontend, database, and API; applies migrations before the API starts |
| `docker compose up --build -d` | repository root | Docker | Starts the same stack in the background; rebuild after source edits |
| `docker compose logs -f` | repository root | Running stack | Follows service logs |
| `docker compose down` | repository root | Compose stack | Removes containers/network; retains database volume |
| `docker compose down -v` | repository root | Disposable database only | Removes containers/network and **deletes database data** |
| `docker compose config --quiet` | repository root | Docker Compose | Validates Compose configuration |
| `npm install` | `web/` | None | Installs frontend dependencies |
| `npm ci` | `web/` or `e2e/` | Committed lockfile | Installs the locked dependency tree for that project |
| `npm run dev` | `web/` | None | Starts the Vite dev server |
| `dotnet restore` | `api/` | None | Restores backend NuGet packages |
| `dotnet restore Nexo.slnx` | repository root | None | Restores API and test project dependencies |
| `dotnet run` | `api/` | None | Starts the ASP.NET Core Web API |
| `dotnet run --launch-profile http` | `api/` | Local configuration | Starts the development API at `http://localhost:5122` |
| `dotnet tool restore` | `api/` | None | Restores local .NET tools (`dotnet-ef`) |
| `dotnet ef migrations add <Name>` | `api/` | Migration name | Generates a new EF Core migration in `Migrations/` |
| `dotnet ef database update` | `api/` | None | Applies pending migrations to the configured PostgreSQL database |
| `dotnet test Nexo.slnx` | repository root | None | Runs backend unit and integration tests |
| `npm test` | `web/` | None | Runs frontend unit and component tests |
| `npm install` | `e2e/` | None | Installs Playwright |
| `npx playwright install chromium` | `e2e/` | None | Downloads the Chromium browser used by tests |
| `npm test` | `e2e/` | None | Runs E2E tests (starts the Vite dev server automatically) |
| `npm run lint` | `web/` | None | Lints the frontend (oxlint) |
| `npm run build` | `web/` | None | Checks TypeScript and builds the production frontend |
| `npm run format` / `npm run format:check` | `web/` | None | Formats the frontend (Prettier), or checks without writing |
| `dotnet format Nexo.slnx` | repository root | None | Formats the backend in place |
| `dotnet build Nexo.slnx -warnaserror` | repository root | None | Builds the backend, failing on any warning |
| `git config core.hooksPath tools/git-hooks` | repository root | Explicit confirmation if run by an agent | One-time setup: enables the [pre-commit hook](../guides/pre-commit-hook.md) for staged formatting, frontend lint/build, and backend builds |
| `pwsh -File tools/generate/test-plantuml-diagrams.ps1` | repository root | Diagram prerequisites | Tests rendering, stale-output removal, and malformed-source handling |
| `pwsh -File tools/generate/generate-plantuml-diagrams.ps1` | repository root | Diagram prerequisites | Regenerates SVGs from PlantUML sources |

## Interfaces

| Method and path | Current behavior |
| --- | --- |
| `GET /WeatherForecast` | Returns five generated forecasts from the sample controller; no authentication or database access |
| `GET /openapi/v1.json` | Generated OpenAPI document, exposed only in Development |
| `GET /scalar/v1` | Scalar UI for manually exercising the API, exposed only in Development |

The forecast response is an array with `date` (date string), `temperatureC` (integer), `temperatureF` (integer), and nullable `summary` (string). Values vary by request. See the [controller](../../api/Controllers/WeatherForecastController.cs) and [DTO](../../api/Domain/Dtos/WeatherForecastDto.cs).

The HTTP launch profile uses port 5122; the HTTPS profile also uses `https://localhost:7110`. The SPA's Axios base URL is `/api`, but neither a Vite proxy nor that API route prefix is configured. The frontend and backend therefore require separate verification today.

Organization, invitation, activation, resource, and sign-in endpoints in [story HLDs](../requirements/README.md#user-stories) are proposed contracts. OAuth settings and JWT configuration are not yet implemented; no secret names or defaults have been selected for them.

For step-by-step instructions, see [guides](../guides/README.md).
