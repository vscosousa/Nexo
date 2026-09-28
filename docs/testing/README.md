# Testing

[Documentation index](../README.md)

This page describes test selection, current coverage, and recorded validation. Passing scaffold tests does not mean the proposed business stories are implemented.

## Strategy

Test-driven development is required (see [AGENTS.md](../../AGENTS.md#working-rules)): a failing test comes first for every feature or fix, then the implementation follows until it passes.

See [ADR-005](../decisions/ADR-005-testing-frameworks.md) for the full reasoning behind the tools below. Summary:

| Kind | Tool | Location |
| --- | --- | --- |
| Unit (backend) | xUnit | `api/tests/` |
| Integration (backend) | xUnit + `Microsoft.AspNetCore.Mvc.Testing` | `api/tests/` |
| Unit / component (frontend) | Vitest + React Testing Library | `web/src/` (colocated with source) |
| E2E | Playwright | `tests/e2e/` |
| Acceptance | Plain xUnit, named in Given/When/Then form | `api/tests/` |
| Functional | Covered by integration/E2E tests of the same behavior; not a separate suite | — |
| Smoke | A tagged subset of the integration/E2E suites covering only critical paths | — |
| Performance | Deferred; no measurable target exists yet | — |

Backend integration tests run against the Compose PostgreSQL, in a separate `nexo_test` database created and migrated by the test factory. Start it first with `docker compose -f compose.yaml -f compose.test.yaml up -d db` ([compose.test.yaml](../../compose.test.yaml) publishes the port on `127.0.0.1:5433`; set `NEXO_TEST_DB` to use another connection string). Run `dotnet test Nexo.slnx` (backend), `npm test` in `web/` (frontend unit), and `npm test` in `tests/e2e/` (E2E, starts the Vite dev server automatically). See [technical reference](../reference/README.md#commands).

## Scenario template

Current application coverage consists of the weather mapper and endpoint examples, the frontend route guard, the browser smoke test, and the US-001 backend tests (`OrganizationMapperTests`, and the Given/When/Then acceptance tests in `OrganizationsEndpointTests`: successful registration, four missing/invalid-field cases, email conflict, and case-insensitive email conflict). US-001 has no frontend or browser tests yet; other stories have none. For a feature or fix, reproduce its behavior with a failing test at the narrowest useful level, implement it, then run the relevant suite.

**ID:** [TEST-001].
**Requirement:** [US or NFR link].
**Environment and version:** [reproducible setup].
**Data and preconditions:** [synthetic fixtures and initial state].
**Steps:** [actions or commands].
**Expected result:** [pass criteria].
**Actual result and evidence:** [outcome and report link when executed].

## Performance checks

No performance scenarios have been executed or targets agreed. Define workload, metric, threshold, and environment with a measurable requirement before selecting a tool or recording results.

## Execution

Diagram-tool tests run separately with `pwsh -File tools/generate/test-plantuml-diagrams.ps1`; see the [diagram guide](../guides/diagrams.md). The only current CI workflow renders/tests diagrams; application checks and the pre-commit hook remain local.

Validation results from this documentation/tooling update are recorded below. Application unit/integration tests, browser E2E tests, and diagram generation were not run because application code and diagram sources were unchanged.

| Check (2026-09-17, Windows) | Result |
| --- | --- |
| Local Markdown link/heading scan | 48 files, 537 local links checked; no missing files or anchors (external URLs not checked) |
| `git diff --check` | Passed |
| `npm run lint` in `web/` | Passed with the existing `react(only-export-components)` warning in `AuthContext.tsx` |
| `npm run build` in `web/` | Passed |
| `dotnet build Nexo.slnx -warnaserror` | Passed, zero warnings/errors |

See [requirements](../requirements/README.md) for acceptance criteria and [guides](../guides/README.md) for setup.
