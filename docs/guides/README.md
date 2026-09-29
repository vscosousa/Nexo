# Guides

[Documentation index](../README.md)

## Start here

Follow [Run locally](../../README.md#run-locally) to install dependencies, configure PostgreSQL, and launch the scaffold. The expected first screen is the sign-in form. For frontend-only work, use the [frontend guide](../../web/README.md).

## Development tasks

| Task | Guide |
| --- | --- |
| Format staged code and check builds before committing | [Pre-commit hook](pre-commit-hook.md) |
| Read Nexo's emails locally, or switch to a real email service | [Fake mail server](fake-mail-server.md) |
| Design a story's four sequence diagrams | [Sequence diagram conventions](sequence-diagrams.md) |
| Render PlantUML sources as SVGs | [Diagram generation](diagrams.md) |
| Update the local database | [Database migrations](../database/README.md#migrations-and-lifecycle) |
| Choose and run tests | [Testing](../testing/README.md) |
| Look up commands, settings, and endpoints | [Technical reference](../reference/README.md) |

## Troubleshooting

| Symptom | Check | Next step |
| --- | --- | --- |
| Browser opens the sign-in page | No valid session cookie (`GET /auth/me` answers 401) | Expected; sign in with email and password, or configure [Google sign-in](../reference/README.md#google-sign-in) |
| Frontend requests to `/api` fail | `web/vite.config.ts` has no proxy | API integration requires routing work; verify the sample API directly |
| EF cannot connect to PostgreSQL | Service, database, and local connection-string configuration | Follow [configuration](../reference/README.md#configuration); keep credentials outside the repo |
| `dotnet ef` is unavailable | Local tools have not been restored | Run `dotnet tool restore` in `api/` |
| Playwright cannot launch Chromium | Browser binary is not installed | Run `npx playwright install chromium` in `tests/e2e/` |
| Commit stops because a selected file has unstaged edits | `git diff` and `git diff --cached` | Review the hunks and stage or stash as described in the [hook guide](pre-commit-hook.md) |

For a new guide, state its goal and prerequisites, give commands with their working directory, and describe the observable result. Keep shared command details in the technical reference.
