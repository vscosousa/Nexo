# US-003 - Create a member account

[Requirements](README.md)

**ID and title:** US-003 - Create a member account.
**Status:** draft.
**Story:** As a person whose email was registered by an admin, I want to create my account with either email/password or Google/Microsoft, so that I can use the system as a member of that organization.

**Preconditions:** The person's email was registered to an organization (per [US-002](US-002-register-member-email.md)) as a pending account that has not been activated yet.

**Acceptance criteria:**

- Given a pending account for an email registered to an organization, when the person completes account creation with that email, the invitation token emailed to them ([US-002](US-002-register-member-email.md)), and a password that satisfies the [password rules](US-003-LLD.md#password-rules), then the system activates the account with the member role scoped to that organization and signs them in.
- Given a pending account but a missing or wrong invitation token, when someone tries to create the account, then the system rejects it, so knowing the email alone is not enough.
- Given the organization already has the maximum number of active accounts for its plan, when a pending account tries to activate, then the system rejects it with a limit-reached error.
- Given an email not registered to any organization, when someone tries to create an account with it, then the system rejects it, since there is no pending account to activate.
- Given an email whose account is already active, when someone tries to create another account with it, then the system rejects it with a conflict error.
- Given a Google or Microsoft account whose verified email has a pending, not-yet-activated account, when the person completes the OAuth flow, then the system activates that account linked to the verified email, with no password set.
- Given a Google or Microsoft account whose verified email is not registered to any organization, when the person completes the OAuth flow, then the system rejects it, same as the password path.

**Exceptions:**

- Email not registered to any organization (no pending account exists).
- Email already has an active account.
- Invitation token missing or wrong.
- Organization plan limit reached.
- Weak password, or fields longer than the stored limits.

**Related artifacts:** [Domain model](../domain-models/README.md#accounts-and-organizations), [ADR-006](../decisions/ADR-006-authentication.md), [HLD](US-003-HLD.md), [LLD](US-003-LLD.md), [SSD/SD diagrams](../us/US-003/README.md), [database design](../database/README.md), [backend tests](../testing/README.md) (password path only).
