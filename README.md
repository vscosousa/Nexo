# Nexo

<!-- Add a banner here once a real project image is available. -->

![React](https://img.shields.io/badge/React-19-61DAFB?logo=react&logoColor=white)
![TypeScript](https://img.shields.io/badge/TypeScript-5-3178C6?logo=typescript&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-4169E1?logo=postgresql&logoColor=white)
![License](https://img.shields.io/badge/license-MIT-green)

Nexo is a local prototype that helps a small association coordinate its resources, spaces, activities, and incidents. It replaces scattered messages and spreadsheets with a single system that tracks availability, responsibility, and history.

[Demo](#demo) · [Features](#features) · [Run locally](#run-locally) · [How it works](#how-it-works) · [Design](#design) · [Project structure](#project-structure)

**Status:** stack scaffolded (frontend, backend, database wired), no features implemented yet.

<!--
Replace bracketed placeholders and remove sections that do not apply.
Command placeholders are editing instructions, not executable commands.
Keep all project documentation in English.
-->

## Demo

[Add an image, GIF, or video link showing the main workflow and its outcome.]

<details>
<summary>View the main screens or examples</summary>

| Step | What to show |
| --- | --- |
| [Getting started] | [Image or example of the initial experience] |
| [Main action] | [Image or example of the core feature] |
| [Result] | [Image or example of the final outcome] |

<!-- Replace placeholders with real content and accessible descriptions. -->

</details>

## Features

<!-- Label each feature as planned, in progress, or available. -->

- **Access and authorization.** Sign in with a synthetic identity and act within your assigned permissions. - Planned
- **Resources.** Register, browse, and update resources such as rooms and equipment. - Planned
- **Search and availability.** Find resources and see when they are free. - Planned
- **Reservations.** Reserve and cancel a resource for a period. - Planned
- **Loans.** Request, deliver, and return a resource. - Planned
- **Incidents.** Report, handle, and close incidents affecting a resource; reopen if unresolved. - Planned
- **Automation.** One scheduled job and one simulated notification. - Planned
- **History.** A record sufficient to reconstruct relevant changes. - Planned
- **AI-assisted search.** Turn a natural-language request into structured search filters for the user to confirm before searching. - Planned, optional, disabled by default

The core must work without any AI model API. Real payments, certified accounting, real personal data, offline mode, real-time collaboration, and global scale are out of scope.

## Run locally

### Requirements

- Node.js 24.x and npm 11.x (frontend).
- .NET SDK 10.x (backend).
- PostgreSQL 17.x, running locally as a service.
- Windows, macOS, or Linux.

### Setup

Clone the repository and enter its directory:

```sh
git clone https://github.com/vscosousa/Nexo.git
cd Nexo
```

1. **Install dependencies:**

   ```sh
   cd web && npm install
   cd ../api && dotnet tool restore
   cd .. && dotnet restore Nexo.slnx
   cd e2e && npm install && npx playwright install chromium
   ```

2. **Configure:** create the `nexo` PostgreSQL database, then set the connection string as a local secret (see the [configuration reference](docs/reference/README.md#configuration)):

   ```sh
   dotnet user-secrets set "ConnectionStrings:NexoDb" "Host=localhost;Port=5432;Database=nexo;Username=<user>;Password=<password>"
   dotnet ef database update
   ```

3. **Start:**

   ```sh
   # terminal 1, from api/
   dotnet run
   # terminal 2, from web/
   npm run dev
   ```

4. **Verify:** open the Vite dev server URL printed in terminal 2; the SPA calls the API started in terminal 1.

### Commands

| Command | Purpose |
| --- | --- |
| `npm run dev` (in `web/`) | Start the frontend development server |
| `dotnet run` (in `api/`) | Start the backend API |
| `dotnet ef database update` (in `api/`) | Apply pending database migrations |
| `dotnet test Nexo.slnx` (repository root) | Run backend unit and integration tests |
| `npm test` (in `web/`) | Run frontend unit and component tests |
| `npm test` (in `e2e/`) | Run end-to-end tests |
| [To be defined] | Check formatting and code quality |

## How it works

### Main workflow

[Explain the journey from the user's action to the result, identifying the components involved.]

[Add a concrete input and output example once the feature is implemented.]

### Data and state

| Data | Storage | Behavior |
| --- | --- | --- |
| [Data type] | [Storage or service] | [When data is created, read, updated, and deleted] |

[Describe external dependencies, persistence, and failure behavior where applicable.]

See the [architecture](docs/architecture/README.md) and [decision records](docs/decisions/README.md) for detailed explanations.

## Design

[Describe the project's visual direction and interaction principles.]

| Element | Definition | Usage |
| --- | --- | --- |
| Primary color | [To be defined] | [Actions and highlights] |
| Surface and text colors | [To be defined] | [Backgrounds, content, and contrast] |
| Typography | [To be defined] | [Headings, body text, and controls] |
| Shared components | [To be defined] | [Reusable interface patterns] |

<details>
<summary>Wireframes and interface decisions</summary>

[Link to wireframes when available and explain key navigation, hierarchy, and accessibility decisions.]

</details>

## Tech stack

<!-- Include only selected technologies. Adjust the areas to fit the project. -->

| Area | Technology | Purpose |
| --- | --- | --- |
| Frontend | React 19 + Vite + TypeScript, React Router, Axios | Single-page application UI |
| Backend | ASP.NET Core 10 Web API (controllers) | HTTP API, application and domain logic |
| Data | PostgreSQL 17 + EF Core (Npgsql) | Relational persistence |
| Testing and quality | xUnit, Vitest + React Testing Library, Playwright | Unit, integration, component, and E2E tests |
| Distribution | [To be defined, if applicable] | [Responsibility] |

See [ADR-001](docs/decisions/ADR-001-frontend-backend-stack.md), [ADR-002](docs/decisions/ADR-002-modular-monolith-architecture.md), [ADR-003](docs/decisions/ADR-003-postgresql-database.md), [ADR-004](docs/decisions/ADR-004-frontend-architecture.md), and [ADR-005](docs/decisions/ADR-005-testing-frameworks.md) for the reasoning behind these choices.

## Project structure

```text
Nexo/
├── AGENTS.md        # Agent guidelines
├── CLAUDE.md        # Claude instructions
├── .gitignore       # Version control exclusions
├── README.md        # Overview and getting started
├── Nexo.slnx        # Solution file (api + api.Tests)
├── web/             # React + Vite + TypeScript SPA
│   └── src/
│       ├── app/         # Shell, router
│       ├── auth/        # Session, route guard, login
│       ├── shared/      # HTTP client, reusable UI
│       ├── features/    # One folder per business module (created as built)
│       └── test/        # Vitest setup
├── api/             # ASP.NET Core Web API
│   ├── Controllers/
│   ├── Domain/
│   │   ├── Models/
│   │   └── Dtos/
│   ├── Mappers/
│   ├── Services/
│   ├── Infrastructure/
│   │   ├── Repositories/
│   │   └── Persistence/
│   └── Migrations/
├── api.Tests/       # xUnit unit and integration tests
├── e2e/             # Playwright end-to-end tests
└── docs/
    ├── README.md    # Documentation introduction and index
    ├── requirements/
    ├── glossary/
    ├── domain-models/
    ├── ssd/
    ├── architecture/
    ├── sd/
    ├── database/
    ├── decisions/
    ├── design/
    ├── guides/
    ├── reference/
    └── testing/
```

<!-- Update the tree as project components are added. -->

See the [documentation](docs/README.md) for the tutorial, how-to guides, and technical reference.

## Contributing

This is a solo portfolio project; contributions follow the usual GitHub flow:

1. Open an issue describing the problem or proposal before starting non-trivial work.
2. Branch from `main`, make focused changes, and follow [AGENTS.md](AGENTS.md) (keep documentation current, don't invent project details).
3. Before opening a pull request, run the available checks: `dotnet build Nexo.slnx`, `dotnet test Nexo.slnx`, `npm run build` and `npm test` in `web/`, and `npm test` in `e2e/`. See [testing](docs/testing/README.md).
4. Describe what changed and what was checked in the pull request.

When reporting an issue, include your environment, reproduction steps, and the expected and actual results.

## Acknowledgements

- Scaffolded with the official [Vite](https://vite.dev/) React + TypeScript template and the [ASP.NET Core](https://learn.microsoft.com/aspnet/core/) Web API template.

## License

[MIT](LICENSE) © 2026 Vasco Sousa.
