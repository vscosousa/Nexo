# Glossary

[Documentation index](../README.md)

Use these terms across requirements, models, code, and tests. They describe the intended domain; only the organization, account, and plan entities are implemented so far.

## Domain terms

| Term | Definition | Synonyms or terms to avoid | Related artifact |
| --- | --- | --- | --- |
| Organization | A single association using the system, with its own admin, members, and resources | Association, org | |
| Plan | A simulated tier, defined once and referenced by organizations, that caps their member account count (e.g., Free: up to 20); no real payment is processed | Tier, subscription | |
| Admin | The person who registers an organization, creates its first account, and invites members and staff | | |
| Staff | An account role, invited by the admin, that may manage the organization's resources | | [US-004](../requirements/US-004-register-resource.md) |
| Account | A synthetic identity belonging to exactly one organization, with an admin, member, or staff role; invited accounts have no credentials, while active accounts have a password, linked external login, or both | User | [Accounts and organizations](../domain-models/README.md#accounts-and-organizations) |
| Invited | Pending account state created when an admin registers a member's email; the account has not been activated | Pending | [US-002](../requirements/US-002-register-member-email.md) |
| Invitation token | A random one-time secret emailed to the invited person when an admin registers their email; only its hash is stored, and it must accompany the email to activate the account | Activation code | [US-002](../requirements/US-002-register-member-email.md), [US-003](../requirements/US-003-create-member-account.md) |
| Fake SMTP server | The local `mail` service that accepts the application's outgoing email, stores it, and shows it in a webmail-style inbox without delivering it anywhere | Mailtrap, smtp4dev | [ADR-008](../decisions/ADR-008-fake-smtp-server.md) |
| Active | Account state after registration or activation; it does not mean the person currently has a signed-in session | Signed in (when describing account status) | [US-003](../requirements/US-003-create-member-account.md) |
| External login | A link between an Account and a social provider (Google, Microsoft) identity, used to sign in without a password | SSO, social login | |
| Resource | An item the association manages and lends: equipment, a utensil, a vehicle; borrowed through a loan. Not a space | Item, equipment | [US-004](../requirements/US-004-register-resource.md) |
| Space | A room, hall, or field the association manages and books out for periods through a reservation. Not a resource | Room, venue | [US-007](../requirements/US-007-register-space.md) |
| Resource type | A kind of resource: a system type shared by every organization (`Equipment`, `Utensil`, `Vehicle`, `Other`) or an organization's custom type | Category | [ADR-012](../decisions/ADR-012-resource-types.md) |
| Reservation | A commitment of a space for a period, only valid when the space is available and eligible | Booking | |
| Loan | The request, delivery, and return cycle of a resource to a member or activity owner | Borrow | |
| Incident | A reported problem affecting a resource, a space, safety, or stock, tracked from report to verified resolution (or reopened) | Issue | |
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
