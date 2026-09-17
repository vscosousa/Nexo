# Nexo documentation

[Back to the project overview](../README.md)

This is the starting point for Nexo's project and technical documentation. Use the topic folders below to explore requirements, models, design decisions, and development guides.

**Status:** documentation scaffolding. Topic pages contain templates to complete as the project develops.

## Documentation map

| Area | Contents |
| --- | --- |
| [Requirements](requirements/README.md) | Scope, stakeholders, user stories, HLD/LLD design docs, acceptance criteria, and quality requirements |
| [Glossary](glossary/README.md) | Shared domain terms and abbreviations |
| [Domain models](domain-models/README.md) | Business concepts, relationships, and rules |
| [User story design (US)](us/README.md) | SSD and SD diagrams grouped by story (levels 1-3) |
| [System sequence diagrams (SSD)](ssd/README.md) | Convention and template for level 1 diagrams |
| [Architecture](architecture/README.md) | System boundaries, components, dependencies, and deployment |
| [Sequence diagrams (SD)](sd/README.md) | Convention and template for level 2-3 diagrams |
| [Database design](database/README.md) | Data models, schema, constraints, indexes, and migrations |
| [Architecture decisions](decisions/README.md) | Decision records, alternatives, and consequences |
| [Design](design/README.md) | User journeys, wireframes, and interface conventions |
| [Guides](guides/README.md) | Tutorials, task instructions, and troubleshooting |
| [Technical reference](reference/README.md) | Configuration, commands, and API contracts |
| [Testing](testing/README.md) | Test strategy, scenarios, performance checks, and evidence |

## Reading paths

- **Understand the problem:** requirements → glossary → domain models.
- **Follow a feature:** requirement → HLD → SSD/SD (user story design) → LLD → database design → test evidence.
- **Understand implementation choices:** architecture → decision records.
- **Work with the project:** guides → technical reference → testing.

## Maintaining the documentation

Keep documentation in English. Replace bracketed placeholders with confirmed information and label proposed designs clearly. Keep this page introductory; add detailed content to its topic folder.

Use stable identifiers such as `US-001` for user stories and `ADR-001` for decisions. Link related artifacts rather than duplicating them. Store editable diagram sources beside their documentation; add rendered exports only when needed.

