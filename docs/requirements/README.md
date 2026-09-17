# Requirements

[Documentation index](../README.md)

Document the problem, scope, and verifiable requirements here.

## Context and scope

**Problem:** Associations need to coordinate resources, spaces, activities, and incidents without relying on scattered messages and documents, while preserving availability, responsibility, and history. The system supports multiple organizations, each with its own admin, members, and resources.

**Stakeholders:** organization admins, members, volunteers.

**Goals (core features):**

- An admin can register their organization and account; the admin can then register member emails, and those people can create their own accounts (authorization rules apply per organization and role).
- Register, browse, and update resources.
- Search and availability.
- Reservation and cancellation.
- Loan request, delivery, and return.
- Incident communication, handling, and closure.
- One scheduled job and one simulated notification.
- History sufficient to reconstruct relevant changes.

**Out of scope:** real payments, certified accounting, real personal data, offline mode, real-time collaboration, global scale, custom/fine-tuned models, mandatory paid integrations. Organization plans (e.g., a Free tier limited to 20 member accounts) are simulated as a data field; no payment processing is built.

**Constraints:**

- **Technical:** runs on a single local machine (no distributed deployment); the core must function without any AI model API.
- **Business:** no budget for paid licenses or mandatory paid integrations; the stack (React, ASP.NET Core, PostgreSQL) is open source.
- **Delivery:** built solo, for a personal portfolio, with no fixed deadline.

## User stories

| ID | Title | Status | HLD | LLD |
| --- | --- | --- | --- | --- |
| [US-001](US-001-create-organization-admin.md) | Create an organization and its admin account | draft | [HLD](US-001-HLD.md) | [LLD](US-001-LLD.md) |
| [US-002](US-002-register-member-email.md) | Register a member's email to an organization | draft | [HLD](US-002-HLD.md) | [LLD](US-002-LLD.md) |
| [US-003](US-003-create-member-account.md) | Create a member account | draft | [HLD](US-003-HLD.md) | [LLD](US-003-LLD.md) |
| [US-004](US-004-register-resource.md) | Register a resource | draft | [HLD](US-004-HLD.md) | [LLD](US-004-LLD.md) |
| [US-005](US-005-sign-in.md) | Sign in to an existing account | draft | | |

## User story template

Create one file per story using `US-001-short-title.md` when requirements are defined.

**ID and title:** [US-001 - title].
**Status:** [draft / agreed / implemented].
**Story:** As a [role], I want [capability], so that [benefit].
**Preconditions:** [required starting state].
**Acceptance criteria:** Given [context], when [action], then [observable outcome].
**Exceptions:** [invalid inputs and failure cases].
**Related artifacts:** [links to SSD, SD, data design, and tests].

## Quality requirements

| ID | Quality attribute | Scenario | Measurable target | Verification |
| --- | --- | --- | --- | --- |
| [NFR-001] | [Performance, accessibility, etc.] | [Conditions and action] | [Threshold] | [Check] |

Use terms from the [glossary](../glossary/README.md).
