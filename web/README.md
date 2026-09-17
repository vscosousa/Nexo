# Nexo frontend

[Project overview](../README.md) · [Documentation](../docs/README.md)

React 19, TypeScript, and Vite SPA. The current scaffold includes routing, a login placeholder, a local token context, an Axios client, and a route-guard test. Business screens and backend authentication are not implemented.

## Development

To start the frontend, API, and database together, run `docker compose up --build` from the repository root and open `http://localhost:5173`. Docker builds install dependencies automatically. Rebuild after source edits; see [Docker setup](../README.md#docker-recommended).

Use Node.js 24.x and npm 11.x. From this directory:

```sh
npm ci
npm run dev
```

Open the URL Vite prints. A browser without a stored token redirects to `/login`; the placeholder does not accept credentials. See the [project setup](../README.md#run-locally) for the API and database.

## Commands

| Command                | Purpose                                     |
| ---------------------- | ------------------------------------------- |
| `npm test`             | Run Vitest component/unit tests once        |
| `npm run lint`         | Run oxlint                                  |
| `npm run build`        | Check TypeScript and build into `dist/`     |
| `npm run preview`      | Serve the existing production build locally |
| `npm run format`       | Apply Prettier to this project              |
| `npm run format:check` | Check formatting without writing            |

## Source layout

| Directory                        | Responsibility                                 |
| -------------------------------- | ---------------------------------------------- |
| `src/app/`                       | App shell and router                           |
| `src/auth/`                      | Token context, route guard, login placeholder  |
| `src/shared/http/`               | Shared Axios client                            |
| `src/test/`                      | Vitest setup                                   |
| `src/features/<name>/` (planned) | Business screens, feature API calls, and types |

The Axios base URL is `/api`; Vite has no API proxy yet. The API's sample route is `/WeatherForecast`, so the scaffold does not provide a connected frontend/API workflow. A token's presence controls navigation only; the guard does not validate expiry or permissions.

Follow [ADR-004](../docs/decisions/ADR-004-frontend-architecture.md) for structure and [testing](../docs/testing/README.md) for TDD. The [pre-commit hook](../docs/guides/pre-commit-hook.md) can format staged frontend files and run lint/build checks.
