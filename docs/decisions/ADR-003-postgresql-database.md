# ADR-003 - PostgreSQL as the relational database

[Documentation index](../README.md) · [Architecture decisions](README.md)

**Status:** Accepted.
**Date:** 2026-09-17.

## Context

A relational database was required, accessed through EF Core from the ASP.NET Core backend. The prototype runs on a single local machine today, but a future remote demo was not ruled out. No specific database brand was required for the portfolio; the choice was to be made on technical merit.

## Options

- **SQL Server (Developer Edition / LocalDB).** Native pairing with the .NET ecosystem and the most mature EF Core provider, but LocalDB is Windows-only, and the full Developer Edition is a heavier install.
- **PostgreSQL.** Cross-platform, open-source, mature EF Core provider (Npgsql), no licensing concerns, and broad low-cost managed hosting options if a remote demo is ever needed (Supabase, Neon, Render, Railway) beyond Azure SQL alone.
- **SQLite.** Embedded file, zero external service, simplest possible setup. Rejected because it is not a client-server database: it cannot be moved to a remote host later without migrating to a different engine, and it has weaker write concurrency and schema-alteration support.

## Decision

PostgreSQL, accessed via `Npgsql.EntityFrameworkCore.PostgreSQL`.

## Consequences

- A PostgreSQL service must be installed and running locally before the backend starts; there is no "clone and run" without it. This was accepted, since local service installation was confirmed as acceptable.
- Moving to a remote database later requires only a connection string change, since PostgreSQL is a client-server database from the start.
- The local development connection string is stored via `dotnet user-secrets` (see [technical reference](../reference/README.md#configuration)), not committed to source control.
- EF Core Migrations output lives in `api/Migrations/`.

## Related artifacts

[ADR-007](ADR-007-local-docker-compose.md) adds a containerized local startup option; the original rationale above describes the manual setup.

[ADR-001](ADR-001-frontend-backend-stack.md), [Technical reference](../reference/README.md), [Database design](../database/README.md).
