# US-001 - Create an organization and its admin account

[Requirements](README.md)

**ID and title:** US-001 - Create an organization and its admin account.
**Status:** implemented for email/password (the admin confirms the email, then signs in) and Google (signs in); Microsoft is coded but rejected until its emails can be verified.
**Story:** As a prospective admin, I want to register my organization and create my admin account in one step, using either email/password or Google/Microsoft, so that I can start inviting members and managing resources.

**Preconditions:** None. This is the entry point; no account or organization exists yet for this admin.

**Acceptance criteria:**

- Given valid organization details and admin account details, when the prospective admin submits registration, then the system creates the organization on the chosen plan and an `Unverified` account with the admin role scoped to it, and emails a link (valid 24 hours) to confirm the address ([ADR-010](../decisions/ADR-010-account-security-hardening.md)).
- Given the confirmation link, when the admin opens it (`/confirm-email`), then the account becomes `Active` and the admin can sign in. A wrong or expired link is rejected and the account stays `Unverified`, unable to sign in or be linked to a social login.
- Given required fields are missing or invalid (including an over-long field or a password that breaks the [password rules](US-003-LLD.md#password-rules)), when the prospective admin submits registration, then the system rejects it with validation errors and creates neither the organization nor the account.
- Given an email already used by an existing account, when someone submits registration with that email, then the system responds exactly as for a new email (`202`), creates nothing, and emails the account's owner a notice instead, so registration cannot be used to find out which emails are registered. An unconfirmed registration whose link has expired does not count: the new registration replaces it.
- Given the prospective admin completes registration via Google or Microsoft instead of a password, when the OAuth callback returns a verified email not yet in use, then the system creates the organization and an admin account linked to that email, with no password set. A verified email that only has an unconfirmed password registration counts as not in use: the Google registration replaces it.

**Exceptions:**

- Missing or invalid required fields (empty organization name, invalid email, weak password, fields longer than the stored limits).
- Email already associated with an existing account, whether that account has a password, an SSO link, or both (a notice is emailed for the password path; the Google path answers with a conflict, since the caller proved ownership of the email).
- Confirmation link wrong or expired.
- More than `RateLimit:PublicPermitLimit` registration or confirmation requests per minute from one address (`429`).

**Related artifacts:** [Domain model](../domain-models/README.md#accounts-and-organizations), [HLD](US-001-HLD.md), [LLD](US-001-LLD.md), [SSD/SD diagrams](../us/US-001/README.md), [ADR-006](../decisions/ADR-006-authentication.md), [database design](../database/README.md), [backend tests](../testing/README.md).
