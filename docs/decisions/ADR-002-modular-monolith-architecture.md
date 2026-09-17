# ADR-002 - Modular monolith backend architecture

[Documentation index](../README.md) · [Architecture decisions](README.md)

**Status:** Accepted.
**Date:** 2026-09-17.

## Context

The backend needed an internal structure. A layered/DDD-style folder layout (Controllers, Domain, Infrastructure, Mappers) was identified as desirable. Splitting the backend into independent microservices was also considered.

## Options

- **Microservices.** Each domain (resources, reservations, incidents, ...) as an independent .NET process with its own database, communicating over HTTP. Would demonstrate distributed-systems skills, but multiplies infrastructure (one Postgres instance and one process per service), requires solving cross-service transactions and API composition, and needs several processes and databases running simultaneously just to test one feature locally.
- **Modular monolith.** A single deployable process and a single database, organized internally by layer (`Controllers`, `Domain`, `Infrastructure/Repositories`, `Infrastructure/Persistence`, `Mappers`, `Services`, `Dtos`).

## Decision

Modular monolith: a single ASP.NET Core Web API project, organized by layer as above.

Microservices solve problems of team scale and independent traffic scaling (Conway's law, independent deploy cadence, per-module load). None of these apply here: this is a solo project, on a single machine, with no scale requirement (explicitly out of scope per [requirements](../requirements/README.md)), and every module in scope (resources, reservations, loans, incidents) moves at the same pace. The "clean structure" goal is achieved by folder/namespace organization, not by process boundaries.

## Consequences

- One process to run and debug (`dotnet run`), one Postgres database.
- If a genuine need for independent scaling or team ownership appears later, a module can be extracted into its own service at that point. This decision does not preclude it, it just avoids paying the cost upfront.

## Related artifacts

[ADR-001](ADR-001-frontend-backend-stack.md), [Architecture](../architecture/README.md).
