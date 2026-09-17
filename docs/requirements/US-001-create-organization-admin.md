# US-001 - Create an organization and its admin account

[Requirements](README.md)

**ID and title:** US-001 - Create an organization and its admin account.
**Status:** draft.
**Story:** As a prospective admin, I want to register my organization and create my admin account in one step, using either email/password or Google/Microsoft, so that I can start inviting members and managing resources.

**Preconditions:** None. This is the entry point; no account or organization exists yet for this admin.

**Acceptance criteria:**

- Given valid organization details and admin account details, when the prospective admin submits registration, then the system creates the organization on the default "Free" plan (up to 20 member accounts), creates an account with the admin role scoped to it, and signs the admin in.
- Given required fields are missing or invalid, when the prospective admin submits registration, then the system rejects it with validation errors and creates neither the organization nor the account.
- Given an email already used by an existing account, when someone submits registration with that email, then the system rejects it with a conflict error.
- Given the prospective admin completes registration via Google or Microsoft instead of a password, when the OAuth callback returns a verified email not yet in use, then the system creates the organization and an admin account linked to that email, with no password set.

**Exceptions:**

- Missing or invalid required fields (empty organization name, invalid email).
- Email already associated with an existing account, whether that account has a password, an SSO link, or both.

**Related artifacts:** [Domain model](../domain-models/README.md#accounts-and-organizations), [HLD](US-001-HLD.md), [LLD](US-001-LLD.md), [SSD/SD diagrams](../us/US-001/README.md), [ADR-006](../decisions/ADR-006-authentication.md), [database design](../database/README.md) (not yet created), tests (not yet created).
