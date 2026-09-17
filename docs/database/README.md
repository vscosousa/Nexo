# Database design

[Documentation index](../README.md)

PostgreSQL 17 is accessed through EF Core and Npgsql, as selected in [ADR-003](../decisions/ADR-003-postgresql-database.md). The [domain model](../domain-models/README.md) describes proposed business concepts; it is not an implemented database schema.

## Data model

**Status:** persistence infrastructure scaffolded; business tables not implemented.

[`NexoDbContext`](../../api/Infrastructure/Persistence/NexoDbContext.cs) currently declares no entity sets. The initial migration's `Up` and `Down` methods are empty. Applying it records migration history but creates no organization, account, or resource tables. The weather sample generates values in memory and does not use PostgreSQL.

## Schema template

When a feature introduces persistence, document its tables, fields, database types, nullability, keys, constraints, and indexes here or in a linked topic file. Derive these from the reviewed story and domain model; do not treat proposed C# field tables as a finalized schema.

## Integrity and access patterns

The proposed stories require account email uniqueness, organization ownership, and atomic organization/admin creation. Database constraints, email normalization, plan-limit concurrency, and deletion rules still need a reviewed physical design. See the [requirements review gaps](../requirements/README.md#design-review-gaps).

## Migrations and lifecycle

With Docker, `docker compose up --build` creates the `nexo` database on first startup and applies pending EF migrations each time the API container starts. The API starts only if migration succeeds. Data persists in the Compose `postgres-data` volume across `docker compose down`; `docker compose down -v` deletes it. The database is internal to Compose; inspect it with `docker compose exec db psql -U nexo -d nexo`.

For manual execution, create the local `nexo` database and configure `ConnectionStrings:NexoDb` using the [setup instructions](../../README.md#run-locally). From `api/`:

```sh
dotnet tool restore
dotnet ef database update
```

After changing a reviewed model, generate a migration with `dotnet ef migrations add <Name>`, inspect its `Up`/`Down` operations and model snapshot, then apply it to a local test database. Commit the migration with the corresponding model and documentation changes. Generated files live in `api/Migrations/`.

Backup/restore, retention, and production migration procedures are not defined. Use synthetic data for the local prototype.
