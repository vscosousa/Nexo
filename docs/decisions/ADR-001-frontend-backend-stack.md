# ADR-001 - Frontend and backend stack

[Documentation index](../README.md) · [Architecture decisions](README.md)

**Status:** Accepted.
**Date:** 2026-09-17.

## Context

Nexo is a local prototype for a small association, run on a single machine, with no requirement for global scale, offline mode, or real-time collaboration. The developer is building it solo, for a personal portfolio, with no fixed deadline. A frontend and backend technology needed to be selected.

## Options

- **React (Vite, TypeScript) + ASP.NET Core Web API.** Two separate projects communicating over a JSON API. Most widely recognized pairing in the job market; clear separation of concerns; the API can serve other clients later.
- **Blazor + ASP.NET Core.** Single .NET codebase end to end, no separate JavaScript toolchain. Less market recognition than React; smaller component ecosystem.
- **ASP.NET Core MVC/Razor Pages (server-rendered), no SPA.** Simplest option: one project, server-rendered HTML. No modern SPA experience to show in a portfolio.
- **Next.js (frontend only) + ASP.NET Core Web API.** Adds SSR/SSG and file-based routing on top of React. None of Next.js's distinguishing features (SSR, SEO, static generation) apply to an internal, authenticated CRUD tool, so the added complexity (rendering mode, Server/Client Component boundary) has no payoff here.

## Decision

React (Vite, TypeScript) as a separate SPA, consuming an ASP.NET Core Web API (controllers) over JSON.

## Consequences

- Two toolchains to run and maintain (`npm` and `dotnet`), two `.gitignore` sections, and CORS between the two origins during local development.
- Authentication must be token-based from the start (the SPA and API are different origins), matching the requirement for synthetic identities and authorization rules.
- The API contract (DTOs, error shape) must be defined before frontend screens are built against it, to avoid rework.

## Related artifacts

[Requirements](../requirements/README.md), [ADR-002](ADR-002-modular-monolith-architecture.md), [ADR-003](ADR-003-postgresql-database.md).
