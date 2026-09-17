# US-001 - Create an organization and its admin account

[Requirements](README.md)

**ID and title:** US-001 - Create an organization and its admin account.
**Status:** draft.
**Story:** As a prospective admin, I want to register my organization and create my admin account in one step, so that I can start inviting members and managing resources.

**Preconditions:** None. This is the entry point; no account or organization exists yet for this admin.

**Acceptance criteria:**

- Given valid organization details and admin account details, when the prospective admin submits registration, then the system creates the organization on the default "Free" plan (up to 20 member accounts), creates an account with the admin role scoped to it, and signs the admin in.
- Given required fields are missing or invalid, when the prospective admin submits registration, then the system rejects it with validation errors and creates neither the organization nor the account.
- Given an email already used by an existing account, when someone submits registration with that email, then the system rejects it with a conflict error.

**Exceptions:**

- Missing or invalid required fields (empty organization name, invalid email).
- Email already associated with an existing account.

**Related artifacts:** [Domain model](../domain-models/README.md), SSD (not yet created), SD (not yet created), [database design](../database/README.md) (not yet created), tests (not yet created).
