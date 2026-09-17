# Database design

[Documentation index](../README.md)

Document how domain concepts are stored and how data integrity is maintained.

## Data model

**Storage technology:** [to be selected].
**Status:** [proposed / implemented].
**Related domain model:** [link].
**ERD or equivalent model:** [add entities, keys, relationships, and cardinalities].

## Schema template

Create a file per schema or bounded area when needed.

| Entity or table | Field | Type | Required | Key or constraint | Purpose |
| --- | --- | --- | --- | --- | --- |
| [Name] | [Field] | [Type] | [Yes / no] | [PK, FK, unique, etc.] | [Meaning] |

## Integrity and access patterns

[Describe validation, relationship rules, indexes, expected queries, and transaction boundaries.]

## Migrations and lifecycle

[Describe schema versioning, migration execution and verification, recovery, retention, and deletion.]

Use synthetic example data. Link significant storage choices to [decision records](../decisions/README.md).
