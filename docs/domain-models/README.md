# Domain models

[Documentation index](../README.md)

Describe business concepts independently of database tables and implementation classes.

## Accounts and organizations

**Scope:** Organization and account creation ([US-001](../requirements/US-001-create-organization-admin.md), [US-002](../requirements/US-002-register-member-email.md), [US-003](../requirements/US-003-create-member-account.md)).
**Status:** proposed.
**Diagram:** not yet added.

| Concept | Meaning | Relationships | Business rules |
| --- | --- | --- | --- |
| Organization | A single association using the system (per [glossary](../glossary/README.md)) | Has one admin Account (the creator) and member Accounts (0..*); has eligible emails awaiting an account (0..*); has Resources (0..*) | Starts on the "Free" plan (limit: 20 member accounts) on creation |
| Account | A synthetic identity signed into the system | Belongs to exactly one Organization; has role "admin" or "member" | Created with role "admin" only via organization creation (US-001); created with role "member" only for an email already registered to the organization (US-003) |

**Assumptions and open questions:**

- Only one admin per organization is assumed for now (the creator). Whether additional admins can be promoted later is open.
- Plan tiers beyond "Free" (e.g., up to 50, unlimited) are named in conversation but not yet specified as enforced rules; only the Free limit (20) is a confirmed business rule so far.

## Resources

**Scope:** Resource registration and lifecycle ([US-004](../requirements/US-004-register-resource.md)).
**Status:** proposed.
**Diagram:** not yet added.

| Concept | Meaning | Relationships | Business rules |
| --- | --- | --- | --- |
| Resource | A room, tool, or piece of equipment the association manages (per [glossary](../glossary/README.md)) | Belongs to exactly one Organization; will be reserved (0..*) via Reservation, borrowed (0..*) via Loan, and affected (0..*) by Incident, once those areas are modeled | Must have a name and a type; starts in status "Available" on registration |

**Assumptions and open questions:**

- Does a resource need a unique code/identifier, or is the name alone sufficient? Not yet decided.
- Can two physically identical resources (e.g., two projectors) be registered as separate entries, or does one entry represent a quantity? Assumed separate entries for now, since reservations and loans target one specific item.

## Model template

**Scope:** [business area and related requirements].
**Status:** [proposed / accepted].
**Diagram:** [add a conceptual model with named relationships and multiplicities].

| Concept | Meaning | Relationships | Business rules |
| --- | --- | --- | --- |
| [Concept] | [Definition] | [Related concepts and multiplicities] | [Invariants] |

**Assumptions and open questions:** [unresolved domain questions].

Use the [glossary](../glossary/README.md) for terminology. Document persistence mappings separately in [database design](../database/README.md). Add one named model file per business area when needed.
