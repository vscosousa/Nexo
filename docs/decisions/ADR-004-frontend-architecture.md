# ADR-004 - Frontend architecture

[Documentation index](../README.md) · [Architecture decisions](README.md)

**Status:** Accepted.
**Date:** 2026-09-17.

## Context

The frontend SPA needed an internal organization, and a decision on whether route guards and HTTP interceptors were justified given the requirement for synthetic identities and per-role authorization ([requirements](../requirements/README.md)).

## Options

- **Feature-based (vertical slice).** One folder per business module (`features/resources/`, `features/reservations/`, ...), each owning its own components, API calls, and types.
- **Layered by type.** Flat `components/`, `pages/`, `hooks/`, `services/` folders shared across all modules.
- **Atomic Design.** UI organized by composition level (`atoms/`, `molecules/`, `organisms/`), independent of business domain.

For the HTTP layer:

- **Native `fetch` wrapper.** Zero dependencies, a thin function attaching the auth token and handling `401`.
- **Axios with interceptors.** A dependency, but with request/response interceptor chaining, errors carrying the response body, and built-in request cancellation.

## Decision

Feature-based organization, with Axios for HTTP.

Feature-based mirrors the backend's module boundaries (resources, reservations, loans, incidents) and keeps each module self-contained. Axios was chosen over a native `fetch` wrapper for its richer interceptor API and more common usage in production React codebases, at the cost of one added dependency.

Route guards and a single shared HTTP client are justified, not speculative: the requirements call for authorization rules per synthetic identity, meaning some routes are genuinely protected and every authenticated request genuinely needs the same token attached.

## Consequences

- Folder layout: `app/` (shell, router), `auth/` (session, guard, login), `shared/` (cross-cutting: HTTP client, reusable UI), `features/<name>/` (created per module as it is built).
- A single Axios instance (`shared/http/client.ts`) must be created once and reused. Creating it per render would re-register interceptors and duplicate side effects.
- `react-router-dom` was added as a dependency to support the guard (`RequireAuth`), since there is no native browser primitive for SPA client-side routing.
- `auth/LoginPage.tsx` is a placeholder: it has no real form or API call yet, because the backend has no authentication endpoint to call against.

## Related artifacts

[ADR-001](ADR-001-frontend-backend-stack.md), [Architecture](../architecture/README.md).
