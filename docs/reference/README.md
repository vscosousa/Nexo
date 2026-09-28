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
| `Microsoft.AspNetCore.Mvc.Testing` | `api/tests/` | Backend integration tests (`WebApplicationFactory`) |
| `@playwright/test` | `tests/e2e/` | End-to-end tests against the running app |
| `oxlint` | `web/` | Frontend linting |
| `prettier` | `web/` | Frontend formatting |

See [ADR-004](../decisions/ADR-004-frontend-architecture.md) and [ADR-005](../decisions/ADR-005-testing-frameworks.md) for why these were chosen over native alternatives.

## Configuration

| Setting | Description | Type | Required | Default |
| --- | --- | --- | --- | --- |
| `ConnectionStrings:NexoDb` | PostgreSQL connection string used by `NexoDbContext` | String | Yes | None, must be set locally |
| `Email:*` | Outgoing email; see [Email](#email) | Section | No | Fake SMTP server at `localhost:1025` |

With Docker, [compose.yaml](../../compose.yaml) supplies `ConnectionStrings__NexoDb` using the internal `db:5432` address. PostgreSQL creates the `nexo` database and user on first startup, using disposable development credentials. Data lives in the `postgres-data` named volume; the database port is not published. The API runs in Development on container port 8080, published at `127.0.0.1:5122`; Vite is published at `127.0.0.1:5173`. No host user secrets are mounted.

For manual execution, the connection string is not stored in `appsettings.json` or `appsettings.Development.json`. Set it locally, once, from the `api/` directory:

```sh
dotnet user-secrets set "ConnectionStrings:NexoDb" "Host=localhost;Port=5432;Database=nexo;Username=<user>;Password=<password>"
```

Use dummy values above; set your own local PostgreSQL credentials. `dotnet user-secrets` stores the value outside the repository, keyed by the `UserSecretsId` in `Nexo.Api.csproj`.

### Email

The API sends email through SMTP. The defaults in `appsettings.json` target the [fake mail server](../guides/fake-mail-server.md); Compose overrides them from `.env` (template: [.env.example](../../.env.example)). A real provider needs only these values, and credentials belong in `.env`, user secrets, or environment variables, never in committed files.

| Setting | Compose variable | Description | Default |
| --- | --- | --- | --- |
| `Email:Host` | `EMAIL_HOST` | SMTP server host | `localhost` (`mail` in Compose) |
| `Email:Port` | `EMAIL_PORT` | SMTP port | `1025` |
| `Email:EnableSsl` | `EMAIL_ENABLE_SSL` | Upgrade the connection with STARTTLS (implicit TLS on 465 is not supported) | `false` |
| `Email:Username` | `EMAIL_USERNAME` | SMTP login; empty means none | empty |
| `Email:Password` | `EMAIL_PASSWORD` | SMTP password | empty |
| `Email:From` | `EMAIL_FROM` | Sender address | `Nexo <no-reply@nexo.local>` |
| `Email:WebBaseUrl` | `EMAIL_WEB_BASE_URL` | Web app address used for links in emails | `http://localhost:5173` |
| `Email:TimeoutSeconds` | none | SMTP timeout | `15` |

The `mail` service settings are `Smtp:Address` (`0.0.0.0` in the container, `127.0.0.1` otherwise), `Smtp:Port` (`1025`), `Store:Directory` (`/data` in the container, mounted from `MAIL_DATA_DIR`, default `./.data/mail`), and `urls` (`http://localhost:8025` outside Docker). Its inbox API is under `/api` (`/messages`, `/messages/{id}`, `/raw`, `/attachments/{index}`, `/read`, `DELETE`) and is unauthenticated: run it on loopback only.

## Commands

| Command | Working directory | Inputs | Result |
| --- | --- | --- | --- |
| `docker compose up --build` | repository root | Running Docker Desktop (Linux containers) | Builds and starts frontend, database, fake mail server, and API; applies migrations before the API starts |
| `docker compose up --build -d` | repository root | Docker | Starts the same stack in the background; rebuild after source edits |
| `docker compose logs -f` | repository root | Running stack | Follows service logs |
| `docker compose down` | repository root | Compose stack | Removes containers/network; retains database volume |
| `docker compose down -v` | repository root | Disposable database only | Removes containers/network and **deletes database data** |
| `docker compose config --quiet` | repository root | Docker Compose | Validates Compose configuration |
| `npm install` | `web/` | None | Installs frontend dependencies |
| `npm ci` | `web/` or `tests/e2e/` | Committed lockfile | Installs the locked dependency tree for that project |
| `npm run dev` | `web/` | None | Starts the Vite dev server |
| `dotnet restore` | `api/` | None | Restores backend NuGet packages |
| `dotnet restore Nexo.slnx` | repository root | None | Restores API and test project dependencies |
| `dotnet run` | `api/` | None | Starts the ASP.NET Core Web API |
| `dotnet run --project tools/fake-smtp` | repository root | None | Starts the fake mail server (inbox `http://localhost:8025`, SMTP `127.0.0.1:1025`) without Docker |
| `dotnet run --launch-profile http` | `api/` | Local configuration | Starts the development API at `http://localhost:5122` |
| `dotnet tool restore` | `api/` | None | Restores local .NET tools (`dotnet-ef`) |
| `dotnet ef migrations add <Name>` | `api/` | Migration name | Generates a new EF Core migration in `Migrations/` |
| `dotnet ef database update` | `api/` | None | Applies pending migrations to the configured PostgreSQL database |
| `docker compose -f compose.yaml -f compose.test.yaml up -d db` | repository root | Docker | Starts the Compose PostgreSQL with port `127.0.0.1:5433` published, as backend integration tests require |
| `dotnet test Nexo.slnx` | repository root | Test database running (see above); optional `NEXO_TEST_DB` connection string | Runs backend unit and integration tests; integration tests use a separate `nexo_test` database |
| `npm test` | `web/` | None | Runs frontend unit and component tests |
| `npm install` | `tests/e2e/` | None | Installs Playwright |
| `npx playwright install chromium` | `tests/e2e/` | None | Downloads the Chromium browser used by tests |
| `npm test` | `tests/e2e/` | None | Runs E2E tests (starts the Vite dev server automatically) |
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
| `POST /organizations/{organizationId}/invitations` | US-002: registers `{ "email" }` as an `Invited` member account; emails the person an invitation with a one-time token and returns `201` with `AccountDto`, or `400`, `403`, `409`; `500` if the email cannot be sent (no account is kept). The caller is the account id in the temporary `X-Account-Id` header until JWT authentication exists |
| `POST /accounts/activation` | US-003: activates the invited account for `{ "email", "name", "password", "invitationToken" }`; returns `200` with `AccountDto`, or `400`, `403` (no account for the email, or wrong token), `409` (already active, or member limit reached). No sign-in or session yet |
| `GET http://localhost:8025/` | Fake mail server inbox (separate service, not the API) |
| `GET /openapi/v1.json` | Generated OpenAPI document, exposed only in Development |
| `GET /scalar/v1` | Scalar UI for manually exercising the API, exposed only in Development |

The forecast response is an array with `date` (date string), `temperatureC` (integer), `temperatureF` (integer), and nullable `summary` (string). Values vary by request. See the [controller](../../api/Controllers/WeatherForecastController.cs) and [DTO](../../api/Domain/Dtos/WeatherForecastDto.cs).

The HTTP launch profile uses port 5122; the HTTPS profile also uses `https://localhost:7110`. The SPA's Axios base URL is `/api`, but neither a Vite proxy nor that API route prefix is configured. The frontend and backend therefore require separate verification today.

Resource and sign-in endpoints in [story HLDs](../requirements/README.md#user-stories) are proposed contracts. OAuth settings and JWT configuration are not yet implemented; no secret names or defaults have been selected for them.

For step-by-step instructions, see [guides](../guides/README.md).
