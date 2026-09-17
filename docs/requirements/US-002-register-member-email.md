# US-002 - Register a member's email to an organization

[Requirements](README.md)

**ID and title:** US-002 - Register a member's email to an organization.
**Status:** draft.
**Story:** As an admin, I want to register a person's email against my organization, so that they become eligible to create their own account.

**Preconditions:** The admin has an account (per [US-001](US-001-create-organization-admin.md)) scoped to an organization.

**Acceptance criteria:**

- Given a valid email not yet registered to any organization, when the admin submits it, then a pending account is created for that email, scoped to the organization, with no credentials until the person activates it.
- Given the organization's plan limit is reached (e.g., 20 active accounts on the Free plan), when the admin tries to register another email, then the system rejects it with a limit-reached error.
- Given an email already registered to this or another organization, whether pending or already active, when the admin submits it, then the system rejects it with a conflict error.
- Given a user without the admin role, when they attempt to register an email, then the system rejects the request with an authorization error.

**Exceptions:**

- Invalid email format.
- Organization plan limit reached.
- Email already registered elsewhere.
- Unauthorized user attempting the action.

**Related artifacts:** [Domain model](../domain-models/README.md#accounts-and-organizations), [HLD](US-002-HLD.md), [LLD](US-002-LLD.md), [SSD/SD diagrams](../us/US-002/README.md), [database design](../database/README.md) (not yet created), tests (not yet created).
