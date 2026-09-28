# Database design

[Documentation index](../README.md)

PostgreSQL 17 is accessed through EF Core and Npgsql, as selected in [ADR-003](../decisions/ADR-003-postgresql-database.md). The [domain model](../domain-models/README.md) describes proposed business concepts; it is not an implemented database schema.

## Data model

**Status:** the [US-001](../requirements/US-001-create-organization-admin.md) tables (`Plans`, `Organizations`, `Accounts`) are implemented in the `AddOrganizationsAndAccounts` migration; resource tables and the tables of other stories are not. The weather sample generates values in memory and does not use PostgreSQL.

Entities are configured in [`NexoDbContext`](../../api/Infrastructure/Persistence/NexoDbContext.cs). Enums are stored as their names (text) so rows stay readable and survive reordering.

| Table | Column | Type | Constraint |
| --- | --- | --- | --- |
| `Plans` | `Id` | uuid | Primary key |
| | `Name` | varchar(50) | Not null, unique |
| | `MemberLimit` | integer | Not null; maximum active member accounts |
| `Organizations` | `Id` | uuid | Primary key |
| | `Name` | varchar(200) | Not null |
| | `PlanId` | uuid | Not null, foreign key to `Plans`, on delete restrict, indexed |
| `Accounts` | `Id` | uuid | Primary key |
| | `Email` | varchar(320) | Not null, unique; stored trimmed and lowercased |
| | `Name` | varchar(200) | Nullable |
| | `Role` | varchar(20) | Not null (`Admin`, `Member`) |
| | `Status` | varchar(20) | Not null (`Invited`, `Active`) |
| | `OrganizationId` | uuid | Not null, foreign key to `Organizations`, on delete restrict, indexed |

Plan rules live in `Plans` rather than on each organization. The migration seeds the `Free` plan (limit 20) with a fixed id; further tiers would be additional rows.

## Schema template

When a feature introduces persistence, document its tables, fields, database types, nullability, keys, constraints, and indexes here or in a linked topic file. Derive these from the reviewed story and domain model; do not treat proposed C# field tables as a finalized schema.

## Integrity and access patterns

The proposed stories require account email uniqueness, organization ownership, and atomic organization/admin creation. Email uniqueness is enforced by a unique index on the normalized (trimmed, lowercased) email, which also stops two concurrent registrations from both succeeding; how that race is reported to the client (the LLD currently maps save failures to 500), plan-limit concurrency, and deletion rules still need a reviewed design. See the [requirements review gaps](../requirements/README.md#design-review-gaps).

## Migrations and lifecycle

With Docker, `docker compose up --build` creates the `nexo` database on first startup and applies pending EF migrations each time the API container starts. The API starts only if migration succeeds. Data persists in the Compose `postgres-data` volume across `docker compose down`; `docker compose down -v` deletes it. The database is internal to Compose; inspect it with `docker compose exec db psql -U nexo -d nexo`.

For manual execution, create the local `nexo` database and configure `ConnectionStrings:NexoDb` using the [setup instructions](../../README.md#run-locally). From `api/`:

```sh
dotnet tool restore
dotnet ef database update
```

After changing a reviewed model, generate a migration with `dotnet ef migrations add <Name>`, inspect its `Up`/`Down` operations and model snapshot, then apply it to a local test database. Commit the migration with the corresponding model and documentation changes. Generated files live in `api/Migrations/`.

Backup/restore, retention, and production migration procedures are not defined. Use synthetic data for the local prototype.
