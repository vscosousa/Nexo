# ADR-005 - Testing frameworks

[Documentation index](../README.md) · [Architecture decisions](README.md)

**Status:** Accepted.
**Date:** 2026-09-17.

## Context

Several kinds of test were requested: unit, integration, functional, acceptance, E2E, performance, and smoke. Not all of these need a distinct tool. This decision maps each requested kind to a concrete framework, or to an existing one reused differently.

## Options

For unit and integration tests (backend):

- **xUnit.** The framework used in current ASP.NET Core templates and Microsoft's own documentation. Pairs with `Microsoft.AspNetCore.Mvc.Testing`'s `WebApplicationFactory` for integration tests against an in-memory `TestServer`.
- **NUnit.** Equivalent capability, more common in older .NET projects.
- **MSTest.** Microsoft's own framework, integrates natively with Visual Studio Test Explorer.

For unit tests (frontend):

- **Vitest.** Built by the Vite team; reuses the existing `vite.config.ts` with no separate transform configuration.
- **Jest.** More established historically in React, but needs `ts-jest` or `babel-jest` to understand the Vite pipeline, duplicating configuration the project already has.

For E2E:

- **Playwright.** One API across Chromium, Firefox, and WebKit, with built-in parallelization; maintained by Microsoft.
- **Cypress.** Mature ecosystem and strong visual debugging, but full WebKit support is experimental and each test runs in a single browser at a time.

For acceptance tests (tied to the Given/When/Then criteria in the user story template in [requirements](../requirements/README.md)):

- **Reqnroll + Gherkin.** Scenarios written in plain text, readable by non-programmers. Only pays off when someone non-technical reviews the scenarios directly.
- **Plain xUnit**, with test names mirroring Given/When/Then. Same acceptance criteria coverage, no added dependency.

For performance tests:

- **k6.** The de facto standard for HTTP load testing, independent of the project's stack.
- **NBomber.** Native to .NET, stays in the same language as the backend.
- **Deferred.** The requirements explicitly exclude global scale, and there is no measurable performance target yet (e.g., "search responds in under 200ms").

Functional and smoke tests were not treated as separate options: functional testing overlaps with integration/E2E testing of the same behavior, and a smoke test is a small, tagged subset of the integration/E2E suite covering only the critical paths, not a distinct framework.

## Decision

- Backend unit and integration: **xUnit** + `Microsoft.AspNetCore.Mvc.Testing`, in `api.Tests/`.
- Frontend unit: **Vitest** + React Testing Library, colocated with source in `web/src/`.
- E2E: **Playwright**, in `e2e/`, running against the Vite dev server.
- Acceptance: plain xUnit tests named in Given/When/Then form, no separate framework.
- Performance: deferred until a measurable target exists.
- Functional and smoke tests: covered by tagging a subset of the integration/E2E suites, not by new tooling.

## Consequences

- Solution file `Nexo.slnx` ties `api/` and `api.Tests/` together for `dotnet test`.
- `Program.cs` exposes a `public partial class Program` so `WebApplicationFactory<Program>` can reference it from the test assembly.
- `e2e/` has its own `package.json`, separate from `web/`, since it exercises the running app rather than being part of it.
- When the first real feature (e.g., resources) is built, its acceptance and integration tests follow the same Given/When/Then naming already established by the example tests.

## Related artifacts

[ADR-001](ADR-001-frontend-backend-stack.md), [ADR-004](ADR-004-frontend-architecture.md), [Testing](../testing/README.md), [Technical reference](../reference/README.md).
