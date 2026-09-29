# US-005 - Sign in to an existing account

[Requirements](README.md)

**ID and title:** US-005 - Sign in to an existing account.
**Status:** implemented (password and Google; Microsoft logins are rejected, see the [design review gaps](README.md#design-review-gaps)).
**Story:** As an admin or member with an existing account, I want to sign in with my email and password, or with Google/Microsoft, so that I can access the system.

**Preconditions:** An account already exists (created via [US-001](US-001-create-organization-admin.md) or [US-003](US-003-create-member-account.md)).

**Acceptance criteria:**

- Given a correct email and password for an existing account, when the user signs in, then the system issues a session and signs them in.
- Given an incorrect password, when the user signs in, then the system rejects it without revealing whether the email exists, including through the response time.
- Given 5 wrong passwords in a row for an account, when the fifth is rejected, then the system locks the account, ends all its sessions, and emails the owner an unlock link. While locked, every sign-in (password or social) is rejected with the same generic error, even with the right password. Opening the unlock link (`/unlock`, valid 24 hours) lifts the lock; once it has expired, the next sign-in attempt emails a fresh link. A correct password before the fifth mistake resets the count ([ADR-010](../decisions/ADR-010-account-security-hardening.md)).
- Given a signed-in user, when they sign out, then every session of the account ends, on all devices.
- Given a Google or Microsoft account whose verified email matches an existing account, when the user completes the OAuth flow, then the system issues a session and signs them in, linking that provider to the account if not already linked. Only an `Active` account can be matched, and only through an email the provider reports as verified.
- Given a Google or Microsoft account whose verified email has no matching account, when the user completes the OAuth flow, then the system rejects sign-in (it does not silently create an account).

**Exceptions:**

- Wrong password.
- OAuth email with no matching account.
- Account exists but was created SSO-only (no password) and the user attempts a password sign-in.
- Account is still `Invited` (pending activation) or `Unverified` (email not confirmed): password and OAuth sign-in are both rejected with the same generic error; OAuth never links a provider to it.
- Account locked by wrong passwords: rejected with the same generic error until unlocked; a wrong or stale unlock link is rejected.
- The user cancels at the provider or the provider returns no usable identity: the browser returns to the sign-in page with a generic error.

**Related artifacts:** [ADR-006](../decisions/ADR-006-authentication.md), [Domain model](../domain-models/README.md#accounts-and-organizations), [HLD](US-005-HLD.md), [LLD](US-005-LLD.md), [SSD/SD diagrams](../us/US-005/README.md), tests (`SignInEndpointTests`, `ExternalSignInEndpointTests`, `AccountLockoutEndpointTests`, `SessionEndpointTests`, and the frontend `SignInForm`, `LoginCallbackPage`, and `client` tests).
