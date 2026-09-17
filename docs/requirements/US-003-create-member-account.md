# US-003 - Create a member account

[Requirements](README.md)

**ID and title:** US-003 - Create a member account.
**Status:** draft.
**Story:** As a person whose email was registered by an admin, I want to create my account with either email/password or Google/Microsoft, so that I can use the system as a member of that organization.

**Preconditions:** The person's email was registered to an organization (per [US-002](US-002-register-member-email.md)) and has no account yet.

**Acceptance criteria:**

- Given an email registered to an organization and no existing account, when the person completes account creation with that email, then the system creates an account with the member role scoped to that organization and signs them in.
- Given an email not registered to any organization, when someone tries to create an account with it, then the system rejects it, since it cannot create an account for an org it does not belong to.
- Given an email that already has an account, when someone tries to create another account with it, then the system rejects it with a conflict error.
- Given a Google or Microsoft account whose verified email is registered to an organization and has no existing account, when the person completes the OAuth flow, then the system creates a member account linked to that email, with no password set.
- Given a Google or Microsoft account whose verified email is not registered to any organization, when the person completes the OAuth flow, then the system rejects it, same as the password path.

**Exceptions:**

- Email not registered to any organization.
- Email already has an account.

**Related artifacts:** [Domain model](../domain-models/README.md#accounts-and-organizations), [ADR-006](../decisions/ADR-006-authentication.md), [HLD](US-003-HLD.md), [LLD](US-003-LLD.md), [SSD/SD diagrams](../us/US-003/README.md), [database design](../database/README.md) (not yet created), tests (not yet created).
