# Nexo

Nexo is a local prototype for associations to coordinate resources, spaces, activities, and incidents, with availability, responsibility, and history in one system.

**Status:** React and ASP.NET Core scaffolds, PostgreSQL configuration, and example tests are in place. Business features remain proposed; the frontend and API are not integrated yet.

[Run locally](#run-locally) · [Features](#features) · [Architecture](docs/architecture/README.md) · [Documentation](docs/README.md)

## Demo

There is no complete business workflow or published demo yet. Running the frontend without a stored token displays a placeholder login page. The API independently exposes a sample weather endpoint.

## Features

| Area | Intended capability | Status |
| --- | --- | --- |
| Access | Organization/admin registration, member invitations and activation, password or Google/Microsoft sign-in | [US-001–US-005 designs](docs/requirements/README.md#user-stories); not implemented |
| Resources | Register, browse, and update rooms and equipment | Registration designed in US-004; not implemented |
| Search and availability | Find resources and available periods | Planned |
| Reservations | Reserve and cancel a resource for a period | Planned |
| Loans | Request, deliver, and return resources | Planned |
| Incidents | Report, handle, close, and reopen incidents | Planned |
| Automation and history | One scheduled job, a simulated notification, and a record of relevant changes | Planned |
| AI-assisted search | Convert natural language into filters for user confirmation | Optional, planned, disabled by default |

The core must work without an AI model API. Real payments, certified accounting, real personal data, offline mode, real-time collaboration, and global scale are out of scope. See [requirements](docs/requirements/README.md).

## Run locally

### Docker (recommended)

Install Docker Desktop with Linux containers and Docker Compose v2, then run from the repository root:

```sh
docker compose up --build
```

This builds the frontend and API, starts PostgreSQL 17, and starts the API. No host Node.js, .NET SDK, PostgreSQL installation, or user-secrets setup is needed for this path. The first run downloads images and dependencies. See [database migrations and lifecycle](docs/database/README.md#migrations-and-lifecycle) for what happens to the database on startup, `down`, and `down -v`.

- Frontend: `http://localhost:5173` (login placeholder).
- API sample: `http://localhost:5122/WeatherForecast`.
- Development OpenAPI: `http://localhost:5122/openapi/v1.json`, browsable at `http://localhost:5122/scalar/v1`.

Use `docker compose up --build -d` to run in the background and `docker compose logs -f` to follow logs. Stop with Ctrl+C in attached mode, or `docker compose down` in either mode.

Source is copied into the images; rerun `docker compose up --build` after edits. Ports 5173 and 5122 must be free. PostgreSQL is accessible only inside the Compose network, so an existing local PostgreSQL service can keep running. The fixed database credentials in [compose.yaml](compose.yaml) are disposable development values, not production credentials. This setup uses HTTP and development servers on loopback only; it is not a production deployment.

The frontend and API still expose separate scaffolds; authentication and a Vite API proxy are not implemented. See [configuration and commands](docs/reference/README.md).

### Manual requirements

- Node.js 24.x and npm 11.x.
- .NET SDK 10.x.
- PostgreSQL 17.x, with a local `nexo` database and credentials.
- Git; Bash is needed for the optional pre-commit hook.

Development currently uses Windows and a local PostgreSQL service. Diagram tooling additionally needs PowerShell 7+, Java, and Graphviz; see [diagrams](docs/guides/diagrams.md).

### Manual setup

Clone the repository:

```sh
git clone https://github.com/vscosousa/Nexo.git
cd Nexo
```

Install the frontend dependencies from `web/`:

```sh
npm ci
```

Restore the solution from the repository root:

```sh
dotnet restore Nexo.slnx
```

From `api/`, restore EF tooling and configure the connection string. Replace the credential placeholders with your local values; do not commit credentials.

```sh
dotnet tool restore
dotnet user-secrets set "ConnectionStrings:NexoDb" "Host=localhost;Port=5432;Database=nexo;Username=<user>;Password=<password>"
dotnet ef database update
```

The initial migration contains no business tables. See [database design](docs/database/README.md) for the current persistence state.

Start two terminals:

| Directory | Command | Expected result |
| --- | --- | --- |
| `api/` | `dotnet run --launch-profile http` | API at `http://localhost:5122` |
| `web/` | `npm run dev` | Vite prints the frontend URL, normally `http://localhost:5173` |

Open the frontend URL. Without a stored token, it redirects to the login placeholder. To verify the API independently, open `http://localhost:5122/WeatherForecast`; it returns five generated forecasts. The development OpenAPI document is at `http://localhost:5122/openapi/v1.json`, and a Scalar UI for manually exercising endpoints is at `http://localhost:5122/scalar/v1`.

Authentication endpoints and a Vite API proxy are not implemented. Stop each process with Ctrl+C.

For browser tests, install dependencies and Chromium from `e2e/`:

```sh
npm ci
npx playwright install chromium
npm test
```

### Commands

| Command | Directory | Purpose |
| --- | --- | --- |
| `npm test` | `web/` | Frontend unit/component tests |
| `npm run lint` | `web/` | Frontend lint |
| `npm run build` | `web/` | TypeScript check and production build |
| `npm run format:check` | `web/` | Formatting check |
| `dotnet test Nexo.slnx` | Repository root | Backend unit/integration tests |
| `dotnet build Nexo.slnx -warnaserror` | Repository root | Backend build with warnings as errors |

The [technical reference](docs/reference/README.md#commands) lists all commands. Enable the optional [pre-commit hook](docs/guides/pre-commit-hook.md) separately for each clone.

## How it works

See [architecture](docs/architecture/README.md) for component boundaries, the current scaffold's request flow, and data/state handling.

## Design

Product wireframes, shared controls, colors, and typography are pending review. Existing Vite assets and styles are scaffold content. The [interface design page](docs/design/README.md) maps the planned journeys and records what remains to be designed.

## Tech stack

| Area | Technology |
| --- | --- |
| Frontend | React 19, Vite, TypeScript 6, React Router, Axios |
| Backend | ASP.NET Core 10 Web API |
| Persistence | PostgreSQL 17, EF Core, Npgsql |
| Tests | xUnit, Vitest, React Testing Library, Playwright, Node.js test runner |
| Quality | oxlint, Prettier, dotnet format |
| Diagrams | PlantUML, Graphviz, PowerShell |

The [decision index](docs/decisions/README.md) explains the selected stack and architecture.

## Project structure

| Path | Contents |
| --- | --- |
| `web/` | React SPA; see the [frontend guide](web/README.md) |
| `api/` | Controllers, domain/DTOs, services, mappers, repositories, EF context and migrations |
| `api.Tests/` | Backend unit/integration tests |
| `e2e/` | Playwright configuration and smoke test |
| `docs/` | Requirements, domain models, story designs, decisions, guides, and reference |
| `tools/git-hooks/` | Pre-commit hook and regression suite |
| `tools/generate/` | PlantUML generator and its tests |
| `libs/` | Pinned diagram-tool metadata |
| `Nexo.slnx` | API and test solution |

## Contributing

This is a solo portfolio project. Follow [AGENTS.md](AGENTS.md) for how to work in this repo, use TDD for features and fixes, and keep the matching documentation topic current. Run the relevant [checks](docs/testing/README.md), and include actual results and limitations in the review.

Review the diff before committing: the hook can format and re-stage selected files.

## Acknowledgements

Scaffolded from the React + TypeScript Vite template and ASP.NET Core Web API template.

## License

[MIT](LICENSE) © 2026 Vasco Sousa.
