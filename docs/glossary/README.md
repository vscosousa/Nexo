# Glossary

[Documentation index](../README.md)

Define business terms consistently across requirements, models, code, and tests.

## Domain terms

| Term | Definition | Synonyms or terms to avoid | Related artifact |
| --- | --- | --- | --- |
| Organization | A single association using the system, with its own admin, members, and resources | Association, org | |
| Plan | A simulated tier on an organization that caps its member account count (e.g., Free: up to 20); no real payment is processed | Tier, subscription | |
| Admin | The person who registers an organization, creates its first account, and registers member emails against it | Staff (when used loosely) | |
| Account | A synthetic identity signed into the system, belonging to exactly one organization, with a role (admin or member); has a password, one or more linked external logins, or both | User | |
| External login | A link between an Account and a social provider (Google, Microsoft) identity, used to sign in without a password | SSO, social login | |
| Resource | A room, tool, or piece of equipment the association manages and can be reserved or borrowed | Equipment, space | |
| Reservation | A commitment of a resource for a period, only valid when the resource is available and eligible | Booking | |
| Loan | The request, delivery, and return cycle of a resource to a member or activity owner | Borrow | |
| Incident | A reported problem affecting a resource, room, safety, or stock, tracked from report to verified resolution (or reopened) | Issue | |
| Maintenance | Scheduled or incident-triggered work that makes a resource unavailable | | |
| Activity | An event (workshop, solidarity drive, cultural session) that requires resources and volunteers | | |
| Member | A person who can request loans of resources | | |
| Volunteer | A person who supports an activity | | |
| AI lab | The optional, disableable feature that converts a natural-language request into structured search filters for user confirmation | | |

## Documentation abbreviations

| Abbreviation | Meaning |
| --- | --- |
| SSD | System sequence diagram: interactions between external actors and the system |
| SD | Sequence diagram: interactions between internal participants |
| HLD | High-level design: a feature's requirements recap, folder/file structure, and API contract |
| LLD | Low-level design: a feature's full technical detail (domain rules, step-by-step logic, error handling) |
| ADR | Architecture decision record |
| ERD | Entity relationship diagram |
| US | User story |
| NFR | Non-functional requirement |
