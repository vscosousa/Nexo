# Architecture decisions

[Documentation index](../README.md)

Keep one record per significant technical decision. Name records `ADR-001-short-title.md` and link superseding decisions rather than rewriting historical reasoning.

**Accepted** means the approach was selected; it does not mean all consequences are implemented. The [architecture page](../architecture/README.md) describes current implementation and the [requirements review gaps](../requirements/README.md#design-review-gaps) identify unfinished design details. The records below retain their original rationale.

## Decision index

| ID | Title | Status | Date |
| --- | --- | --- | --- |
| [ADR-001](ADR-001-frontend-backend-stack.md) | Frontend and backend stack | Accepted | 2026-09-17 |
| [ADR-002](ADR-002-modular-monolith-architecture.md) | Modular monolith backend architecture | Accepted | 2026-09-17 |
| [ADR-003](ADR-003-postgresql-database.md) | PostgreSQL as the relational database | Accepted | 2026-09-17 |
| [ADR-004](ADR-004-frontend-architecture.md) | Frontend architecture | Accepted | 2026-09-17 |
| [ADR-005](ADR-005-testing-frameworks.md) | Testing frameworks | Accepted | 2026-09-17 |
| [ADR-006](ADR-006-authentication.md) | Authentication: password and social login | Accepted | 2026-09-17 |
| [ADR-007](ADR-007-local-docker-compose.md) | Docker Compose for local startup | Proposed; implemented for review | 2026-09-17 |

## Record template

**ID and title:** [ADR-001 - title].
**Status:** [proposed / accepted / superseded].
**Date:** [YYYY-MM-DD].

### Context

[Problem, constraints, and relevant requirements.]

### Options

[Alternatives considered and their advantages and drawbacks.]

### Decision

[Selected approach and rationale.]

### Consequences

[Benefits, trade-offs, risks, and follow-up work.]

### Related artifacts

[Links to requirements, affected models, and any superseding record.]
