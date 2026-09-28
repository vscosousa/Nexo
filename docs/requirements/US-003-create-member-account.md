# US-003 - Create a member account

[Requirements](README.md)

**ID and title:** US-003 - Create a member account.
**Status:** backend implemented for the password path and the frontend `/activate` page (no OAuth activation or sign-in session yet).
**Story:** As a person whose email was registered by an admin, I want to create my account with either email/password or Google/Microsoft, so that I can use the system as a member of that organization.

**Preconditions:** The person's email was registered to an organization (per [US-002](US-002-register-member-email.md)) as a pending account that has not been activated yet.

**Acceptance criteria:**

- Given a pending account for an email registered to an organization, when the person completes account creation with that email, the invitation link token and the 6-character invitation code emailed to them ([US-002](US-002-register-member-email.md)), and a password that satisfies the [password rules](US-003-LLD.md#password-rules), then the system activates the account with the member role scoped to that organization and signs them in.
- Given a pending account but a missing or wrong link token or invitation code, when someone tries to create the account, then the system rejects it, so knowing the email alone is not enough, and neither is the link alone.
- Given a pending account whose invitation code has expired (7 days after it was sent, per [US-002](US-002-register-member-email.md)), when someone tries to activate with it, then the system rejects it the same way as a wrong code. The person can ask for a fresh code themselves (`POST /accounts/activation/resend`), which always responds the same way whether or not the email has a pending invitation, so it cannot be used to check which emails are registered.
- Given the frontend `/activate` page, when it is opened, then it only renders with `token` and `email` query parameters present (otherwise it redirects home); the email is never asked for again, and the code is entered masked, one character per box. The link token from the URL is sent automatically and never shown; the code is a separate secret the person types in from the email body, so having the link alone (which can leak through browser history or a forwarded URL) is not enough. The page checks both against `POST /accounts/activation/resend`'s sibling read-only endpoint, `POST /accounts/activation/verify`, and only once the API confirms them does the code entry give way to the name/password fields.
- Given the organization already has the maximum number of active accounts for its plan, when a pending account tries to activate, then the system rejects it with a limit-reached error.
- Given an email not registered to any organization, when someone tries to create an account with it, then the system rejects it, since there is no pending account to activate.
- Given an email whose account is already active, when someone tries to create another account with it, then the system rejects it with a conflict error.
- Given a Google or Microsoft account whose verified email has a pending, not-yet-activated account, when the person completes the OAuth flow, then the system activates that account linked to the verified email, with no password set.
- Given a Google or Microsoft account whose verified email is not registered to any organization, when the person completes the OAuth flow, then the system rejects it, same as the password path.

**Exceptions:**

- Email not registered to any organization (no pending account exists).
- Email already has an active account.
- Link token or invitation code missing, wrong, or expired.
- Organization plan limit reached.
- Weak password, or fields longer than the stored limits.

**Related artifacts:** [Domain model](../domain-models/README.md#accounts-and-organizations), [ADR-006](../decisions/ADR-006-authentication.md), [HLD](US-003-HLD.md), [LLD](US-003-LLD.md), [SSD/SD diagrams](../us/US-003/README.md), [database design](../database/README.md), [backend tests](../testing/README.md) (password path only).
