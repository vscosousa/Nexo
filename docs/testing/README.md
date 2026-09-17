# Testing

[Documentation index](../README.md)

Define how requirements are verified and where results are recorded.

## Strategy

Test-driven development is required (see [AGENTS.md](../../AGENTS.md#working-rules)): a failing test comes first for every feature or fix, then the implementation follows until it passes.

See [ADR-005](../decisions/ADR-005-testing-frameworks.md) for the full reasoning behind the tools below. Summary:

| Kind | Tool | Location |
| --- | --- | --- |
| Unit (backend) | xUnit | `api.Tests/` |
| Integration (backend) | xUnit + `Microsoft.AspNetCore.Mvc.Testing` | `api.Tests/` |
| Unit / component (frontend) | Vitest + React Testing Library | `web/src/` (colocated with source) |
| E2E | Playwright | `e2e/` |
| Acceptance | Plain xUnit, named in Given/When/Then form | `api.Tests/` |
| Functional | Covered by integration/E2E tests of the same behavior; not a separate suite | — |
| Smoke | A tagged subset of the integration/E2E suites covering only critical paths | — |
| Performance | Deferred; no measurable target exists yet | — |

Run `dotnet test Nexo.slnx` (backend), `npm test` in `web/` (frontend unit), and `npm test` in `e2e/` (E2E, starts the Vite dev server automatically). See [technical reference](../reference/README.md#commands).

## Scenario template

**ID:** [TEST-001].
**Requirement:** [US or NFR link].
**Environment and version:** [reproducible setup].
**Data and preconditions:** [synthetic fixtures and initial state].
**Steps:** [actions or commands].
**Expected result:** [pass criteria].
**Actual result and evidence:** [outcome and report link when executed].

## Performance checks

| Scenario | Workload | Metric | Target | Result |
| --- | --- | --- | --- | --- |
| [Scenario] | [Concurrency, duration, data volume] | [Latency, throughput, etc.] | [Threshold] | [Not run / measured value] |

## Execution

[Document commands, required services, and report locations once tooling exists. Distinguish planned checks from executed checks.]

See [requirements](../requirements/README.md) for acceptance criteria and [guides](../guides/README.md) for setup.
