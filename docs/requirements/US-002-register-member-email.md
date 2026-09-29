# US-002 - Register a member's email to an organization

[Requirements](README.md)

**ID and title:** US-002 - Register a member's email to an organization.
**Status:** backend implemented (caller from the bearer token, per [ADR-006](../decisions/ADR-006-authentication.md); no frontend yet).
**Story:** As an admin, I want to register a person's email against my organization, so that they become eligible to create their own account.

**Preconditions:** The admin has an account (per [US-001](US-001-create-organization-admin.md)) scoped to an organization.

**Acceptance criteria:**

- Given a valid email not yet registered to any organization, when the admin submits it, then a pending account is created for that email, scoped to the organization, with no credentials until the person activates it, and the person is emailed an invitation with a link and a one-time invitation token that expires after 7 days.
- Given an email already pending (not yet activated) in the same organization, when the admin submits it again, then the invitation is replaced with a fresh link token, code, and expiry, and a new email is sent, so an admin can recover from an expired or lost invitation without a separate re-invite action.
- Given a pending invitation, when the person themselves asks for it to be resent (`POST /accounts/activation/resend`, not the admin re-inviting), then only the code and expiry are refreshed. The link token is left as is, so an activation page the person already has open (with the original token in its URL) keeps working once they enter the new code. The resend email contains only the code, no link.
- Given the organization's plan limit is reached (e.g., 20 active accounts on the Free plan), when the admin tries to register another email, then the system rejects it with a limit-reached error.
- Given an email already registered to this or another organization, whether pending or already active, when the admin submits it, then the system rejects it with a conflict error.
- Given a user without the admin role, when they attempt to register an email, then the system rejects the request with an authorization error.

**Exceptions:**

- Invalid email format.
- Organization plan limit reached.
- Email already registered elsewhere.
- Unauthorized user attempting the action.

**Related artifacts:** [Domain model](../domain-models/README.md#accounts-and-organizations), [HLD](US-002-HLD.md), [LLD](US-002-LLD.md), [SSD/SD diagrams](../us/US-002/README.md), [database design](../database/README.md) [backend tests](../testing/README.md).
