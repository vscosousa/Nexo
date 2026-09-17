# ADR-007 - Docker Compose for local startup

[Documentation index](../README.md) · [Architecture decisions](README.md)

**Status:** Proposed; implementation available for review.
**Date:** 2026-09-17.

## Context

Starting Nexo manually requires dependency installation, database setup, migrations, and separate frontend/API commands. The requested workflow starts the full local stack with one command and preserves database data between runs.

## Options

- Keep separate host commands: requires Node.js, .NET, and a configured PostgreSQL service.
- Containerize only PostgreSQL: reduces database setup but still requires separate application commands.
- Use Docker Compose for all three services: provides one startup command and containerized dependencies.

## Decision

The implementation uses `docker compose up --build` with Node.js 24, .NET SDK 10, and PostgreSQL 17. The API waits for PostgreSQL health, applies migrations, then starts Kestrel. A named volume retains database data. The frontend uses Vite's development server; source edits require rebuilding images.

This supplements ADR-003's local service setup; the database engine and EF Core architecture remain the same. Manual execution remains documented.

## Consequences

- Docker Desktop with Linux containers replaces host toolchain requirements for running the stack.
- The first build downloads images and packages; later builds reuse cached dependency layers.
- The SDK remains in the API image to run EF tooling. This and Vite are intended for local development, not production deployment.
- Database credentials are fixed, disposable development values. Only frontend/API ports are exposed on loopback.
- Migration failure prevents API startup; logs show the failure. Deleting the data volume is an explicit destructive reset.
- This setup does not implement the frontend/API integration or business features.

## Related artifacts

[Local setup](../../README.md#run-locally), [Compose configuration](../../compose.yaml), [database lifecycle](../database/README.md#migrations-and-lifecycle), [Docker smoke checks](../testing/README.md#docker-smoke-checks), [Compose startup ordering](https://docs.docker.com/compose/how-tos/startup-order/).
