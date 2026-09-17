# Requirements

[Documentation index](../README.md)

This page defines the intended scope and indexes draft stories. None of the five business stories is implemented; their HLD/LLD and diagrams describe proposed behavior.

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
| [US-005](US-005-sign-in.md) | Sign in to an existing account | draft | [HLD](US-005-HLD.md) | [LLD](US-005-LLD.md) |

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

No measurable quality targets have been agreed. Before adding an NFR, define a stable ID, the conditions and action, an observable threshold, and a reproducible verification method. Performance testing remains deferred per [ADR-005](../decisions/ADR-005-testing-frameworks.md).

## Design review gaps

These are inconsistencies or missing details in the current proposals, not new decisions. Resolve them with the project owner and update the affected stories and diagrams together before implementation.

| Area | Gap to resolve |
| --- | --- |
| US-001 registration | Acceptance criteria require password/SSO registration and automatic sign-in, but the HLD only supplies organization/name/email fields and returns `OrganizationDto`. Credential input and session delivery are missing. |
| US-003 activation | The story requires sign-in after activation, but the HLD returns only `AccountDto`; the session response is unspecified. |
| US-002/US-003 plan limits | Text alternates between active members and all active accounts. The invitation design checks the limit, while activation does not; concurrent activations are unspecified. |
| US-004 resource permissions | The permitted role and recognized resource types are not defined. "Staff" is not a modeled role. |
| US-005 sign-in | A null password hash also occurs on invited accounts, not only SSO-only accounts. The active-status gate and provider-linking behavior need explicit treatment. The scaffold redirects on every HTTP 401, whereas the sign-in design needs to render a generic credential error; these behaviors must be reconciled. |
| Shared contracts | API prefix, error bodies, email normalization, OAuth endpoints, and database uniqueness/concurrency handling need consistent specifications. |

The [database page](../database/README.md) tracks persistence status. [ADR-006](../decisions/ADR-006-authentication.md) records the accepted authentication direction, while the concrete contracts remain incomplete.

Use terms from the [glossary](../glossary/README.md).
