# Database design

[Documentation index](../README.md)

PostgreSQL 17 is accessed through EF Core and Npgsql, as selected in [ADR-003](../decisions/ADR-003-postgresql-database.md). The [domain model](../domain-models/README.md) describes proposed business concepts; only the tables listed under [Data model](#data-model) are implemented.

## Data model

**Status:** the [US-001](../requirements/US-001-create-organization-admin.md) tables (also used by [US-002](../requirements/US-002-register-member-email.md), which needs no schema change: an invited member is an `Accounts` row with `Status = Invited` and null `Name`) (`Plans`, `Organizations`, `Accounts`) are implemented in the `AddOrganizationsAndAccounts` migration, plus `Accounts.PasswordHash` (US-001, US-003) and `Accounts.InvitationTokenHash` (US-002, US-003) in `AddAccountCredentials`, `ExternalLogins` (US-005) in `AddExternalLogins`, `Plans.ResourceLimit` plus the `Team`/`Enterprise` seed rows (US-006) in `AddPlanResourceLimitAndTiers`, and `Plans.MonthlyPrice` and its five feature flags (US-006) in `AddPlanPricingAndFeatures`; resource tables and the tables of other stories are not. The weather sample generates values in memory and does not use PostgreSQL.

Entities are configured in [`NexoDbContext`](../../api/Infrastructure/Persistence/NexoDbContext.cs). Enums are stored as their names (text) so rows stay readable and survive reordering.

| Table | Column | Type | Constraint |
| --- | --- | --- | --- |
| `Plans` | `Id` | uuid | Primary key |
| | `Name` | varchar(50) | Not null, unique |
| | `MemberLimit` | integer | Not null; maximum active member accounts (`int.MaxValue` means unlimited) |
| | `ResourceLimit` | integer | Not null; maximum resources the organization can register (`int.MaxValue` means unlimited) |
| | `MonthlyPrice` | numeric(8,2) | Nullable; display-only euros/month, null means a custom/"contact us" plan (plans are simulated, no real billing) |
| | `HasIncidentTracking` | boolean | Not null |
| | `HasExpenseTracking` | boolean | Not null |
| | `HasDecisionHistory` | boolean | Not null |
| | `HasAiInsights` | boolean | Not null |
| | `HasPrioritySupport` | boolean | Not null |
| `Organizations` | `Id` | uuid | Primary key |
| | `Name` | varchar(200) | Not null |
| | `PlanId` | uuid | Not null, foreign key to `Plans`, on delete restrict, indexed |
| `Accounts` | `Id` | uuid | Primary key |
| | `Email` | varchar(320) | Not null, unique; stored trimmed and lowercased |
| | `Name` | varchar(200) | Nullable |
| | `PasswordHash` | varchar(200) | Nullable; set at registration (US-001) or activation (US-003), null for invited accounts |
| | `InvitationTokenHash` | varchar(64) | Nullable; SHA-256 (hex) of the one-time invitation token, set while `Invited` and cleared on activation |
| | `xmin` | xid | PostgreSQL system column used as the EF concurrency token; not created by a migration |
| | `Role` | varchar(20) | Not null (`Admin`, `Member`) |
| | `Status` | varchar(20) | Not null (`Invited`, `Active`) |
| | `OrganizationId` | uuid | Not null, foreign key to `Organizations`, on delete restrict, indexed |

| `ExternalLogins` | `Id` | uuid | Primary key |
| | `AccountId` | uuid | Not null, foreign key to `Accounts`, on delete cascade, indexed |
| | `Provider` | varchar(20) | Not null; lowercase provider name (`google`, `microsoft`) |
| | `ProviderKey` | varchar(200) | Not null; the account's stable id at the provider; unique together with `Provider` |

Plan rules live in `Plans` rather than on each organization. Migrations seed three tiers with fixed ids: `Free` (member limit 20, resource limit 10, €0, no feature flags), `Team` (member limit 100, resource limit 100, €29/month, incident tracking + expense tracking + decision history), `Enterprise` (both limits unlimited, no fixed price, every feature flag). The five feature flags mark functionality areas the app does not implement yet (see [domain model](../domain-models/README.md#accounts-and-organizations)); they are pricing-page data, not an enforced entitlement check.

## Schema template

When a feature introduces persistence, document its tables, fields, database types, nullability, keys, constraints, and indexes here or in a linked topic file. Derive these from the reviewed story and domain model; do not treat proposed C# field tables as a finalized schema.

## Integrity and access patterns

The proposed stories require account email uniqueness, organization ownership, and atomic organization/admin creation. Email uniqueness is enforced by a unique index on the normalized (trimmed, lowercased) email, which also stops two concurrent registrations from both succeeding; how that race is reported to the client (US-001 and US-002 map the unique-index violation to 409, so the loser of two concurrent requests for one email conflicts like a sequential duplicate), plan-limit concurrency (the US-002 limit check counts `Active` rows without locking), and deletion rules still need a reviewed design. See the [requirements review gaps](../requirements/README.md#design-review-gaps).

## Migrations and lifecycle

With Docker, `docker compose up --build` creates the `nexo` database on first startup and applies pending EF migrations each time the API container starts. The API starts only if migration succeeds. Data persists in the Compose `postgres-data` volume across `docker compose down`; `docker compose down -v` deletes it. The database is internal to Compose; inspect it with `docker compose exec db psql -U nexo -d nexo`.

For manual execution, create the local `nexo` database and configure `ConnectionStrings:NexoDb` using the [setup instructions](../../README.md#run-locally). From `api/`:

```sh
dotnet tool restore
dotnet ef database update
```

After changing a reviewed model, generate a migration with `dotnet ef migrations add <Name>`, inspect its `Up`/`Down` operations and model snapshot, then apply it to a local test database. Commit the migration with the corresponding model and documentation changes. Generated files live in `api/Migrations/`.

Backup/restore, retention, and production migration procedures are not defined. Use synthetic data for the local prototype.
