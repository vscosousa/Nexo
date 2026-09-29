# ADR-011 - Multi-tenancy: shared schema with organization query filters

[Documentation index](../README.md) · [Architecture decisions](README.md)

**Status:** Proposed; implemented for review.
**Date:** 2026-09-29.

## Context

Every organization's data lives in one PostgreSQL database ([ADR-003](ADR-003-postgresql-database.md)). Org-owned rows carry an `OrganizationId` foreign key, and until now each service compared it with the caller's organization by hand (for example, `AccountInvitationService.Invite`). A forgotten check would leak one organization's data to another. [US-004](../requirements/US-004-register-resource.md) adds the first business data (resources, and custom resource types per [ADR-012](ADR-012-resource-types.md)), which raised the question of whether each organization should get its own database, to isolate tenants and spread load.

Some data is global by nature: plans, and account lookup by email at sign-in, which has to find the account before its organization is known.

## Options

- **Shared schema, `OrganizationId` column (row-level tenancy).** One database, one connection pool, one migration run. Isolation is logical only, so it depends on every query filtering by organization. Scales by indexing and later partitioning on `OrganizationId`, then read replicas.
- **Schema per organization, one database.** Medium isolation through `search_path`. Migrations run once per schema, and thousands of schemas slow the catalog. Global data needs a shared schema.
- **Database per organization.** Strong isolation and no noisy neighbours, but one connection pool and database per organization (managed hosting often bills per database), migrations run N times and can fail halfway, every request must resolve its database and `DbContext`, org creation must provision a database, and a central catalog database is still needed for plans and sign-in. Foreign keys cannot cross databases.
- **Hybrid.** Most organizations share a pooled database; a large tenant (e.g., Enterprise) gets a dedicated one. Carries all of the database-per-organization machinery, but only pays for dedicated databases where needed.

## Decision

Shared schema with `OrganizationId`, hardened with EF Core global query filters: every org-owned entity added from now on gets a filter on the current caller's organization (the session token's `orgId` claim), so a query that forgets to filter returns nothing from other organizations instead of leaking it. Global tables (plans, system resource types) stay clearly separated from org-owned ones, so the hybrid model remains possible later as a data move rather than a redesign.

Nexo's tenants are associations: many and small. Per-tenant databases would multiply cost and operations work for isolation nobody has required yet.

## Consequences

- `NexoDbContext` reads the caller's organization from the current HTTP request. With no signed-in caller (migrations, background work, tests that build the context directly) the filtered sets return no org-owned rows, so they fail closed.
- The filters apply to `Resources` and `ResourceTypes` first. `Accounts` is not filtered yet: sign-in and invitation lookups are intentionally cross-organization, and retrofitting it (with `IgnoreQueryFilters()` at those call sites) is a separate change.
- Code that genuinely needs cross-organization data must call `IgnoreQueryFilters()`, which makes those places easy to find and review.
- Postgres row-level security can be added later as defense in depth. If load or isolation requirements grow, the next steps are partitioning by `OrganizationId`, then moving large tenants to dedicated databases (the hybrid option), recorded in a new ADR.

## Related artifacts

[ADR-002](ADR-002-modular-monolith-architecture.md), [ADR-003](ADR-003-postgresql-database.md), [ADR-012](ADR-012-resource-types.md), [Database design](../database/README.md), [Architecture](../architecture/README.md).
