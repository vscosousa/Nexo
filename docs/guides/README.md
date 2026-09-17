# Guides

[Documentation index](../README.md)

## Start here

Follow [Run locally](../../README.md#run-locally) to install dependencies, configure PostgreSQL, and launch the scaffold. The expected first screen is the login placeholder. For frontend-only work, use the [frontend guide](../../web/README.md).

## Development tasks

| Task | Guide |
| --- | --- |
| Format staged code and check builds before committing | [Pre-commit hook](pre-commit-hook.md) |
| Design a story's four sequence diagrams | [Sequence diagram conventions](sequence-diagrams.md) |
| Render PlantUML sources as SVGs | [Diagram generation](diagrams.md) |
| Update the local database | [Database migrations](../database/README.md#migrations-and-lifecycle) |
| Choose and run tests | [Testing](../testing/README.md) |
| Look up commands, settings, and endpoints | [Technical reference](../reference/README.md) |

## Troubleshooting

| Symptom | Check | Next step |
| --- | --- | --- |
| Browser opens a placeholder login page | No session token is stored | Expected scaffold behavior; real sign-in is planned |
| Frontend requests to `/api` fail | `web/vite.config.ts` has no proxy | API integration requires routing work; verify the sample API directly |
| EF cannot connect to PostgreSQL | Service, database, and local connection-string configuration | Follow [configuration](../reference/README.md#configuration); keep credentials outside the repo |
| `dotnet ef` is unavailable | Local tools have not been restored | Run `dotnet tool restore` in `api/` |
| Playwright cannot launch Chromium | Browser binary is not installed | Run `npx playwright install chromium` in `e2e/` |
| Commit stops because a selected file has unstaged edits | `git diff` and `git diff --cached` | Review the hunks and stage or stash as described in the [hook guide](pre-commit-hook.md) |

For a new guide, state its goal and prerequisites, give commands with their working directory, and describe the observable result. Keep shared command details in the technical reference.
