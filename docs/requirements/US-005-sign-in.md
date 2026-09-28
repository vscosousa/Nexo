# US-005 - Sign in to an existing account

[Requirements](README.md)

**ID and title:** US-005 - Sign in to an existing account.
**Status:** implemented (password and Google; Microsoft logins are rejected, see the [design review gaps](README.md#design-review-gaps)).
**Story:** As an admin or member with an existing account, I want to sign in with my email and password, or with Google/Microsoft, so that I can access the system.

**Preconditions:** An account already exists (created via [US-001](US-001-create-organization-admin.md) or [US-003](US-003-create-member-account.md)).

**Acceptance criteria:**

- Given a correct email and password for an existing account, when the user signs in, then the system issues a session and signs them in.
- Given an incorrect password, when the user signs in, then the system rejects it without revealing whether the email exists.
- Given a Google or Microsoft account whose verified email matches an existing account, when the user completes the OAuth flow, then the system issues a session and signs them in, linking that provider to the account if not already linked. Only an `Active` account can be matched, and only through an email the provider reports as verified.
- Given a Google or Microsoft account whose verified email has no matching account, when the user completes the OAuth flow, then the system rejects sign-in (it does not silently create an account).

**Exceptions:**

- Wrong password.
- OAuth email with no matching account.
- Account exists but was created SSO-only (no password) and the user attempts a password sign-in.
- Account is still `Invited` (pending activation): password and OAuth sign-in are both rejected with the same generic error; OAuth never links a provider to it.
- The user cancels at the provider or the provider returns no usable identity: the browser returns to the sign-in page with a generic error.

**Related artifacts:** [ADR-006](../decisions/ADR-006-authentication.md), [Domain model](../domain-models/README.md#accounts-and-organizations), [HLD](US-005-HLD.md), [LLD](US-005-LLD.md), [SSD/SD diagrams](../us/US-005/README.md), tests (`SignInEndpointTests`, `ExternalSignInEndpointTests`, and the frontend `SignInForm`, `LoginCallbackPage`, and `client` tests).
