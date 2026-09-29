# ADR-010 - Account security hardening

[Documentation index](../README.md) · [Architecture decisions](README.md)

**Status:** Proposed; implemented for review.
**Date:** 2026-09-29.

## Context

A security review of the authentication flows ([ADR-006](ADR-006-authentication.md), [ADR-009](ADR-009-session-cookie.md), [US-001](../requirements/US-001-create-organization-admin.md) to [US-005](../requirements/US-005-sign-in.md)) found these gaps:

- **Account takeover by pre-registration.** Registering an organization with a password created an `Active` admin for any email, without proving the registrant owned it. When the real owner later signed in with Google, [US-005](../requirements/US-005-sign-in.md) linked Google to that account by its verified email, and the attacker's password kept working on it.
- **Brute force.** Only password sign-in was rate limited. The invitation code (6 characters, about 887 million values) could be guessed without limit by anyone holding the invitation link. `resend` also pushed the invitation's expiry forward on every call, so an invitation never expired.
- **Email enumeration.** `POST /organizations` and the invitation check answered `409` for a registered email, and sign-in skipped password hashing for unknown emails, so its response time gave them away.
- **No session revocation.** Sign-out only cleared the cookie; a copied session token stayed valid for its full 8 hours.
- **Unbounded invitations.** Only active members counted toward the plan's member limit, so an admin could send an unlimited number of invitation emails.
- **Deployment hygiene.** JWT issuer and audience were not validated, there was no HSTS, rate limits were keyed on the direct peer (one bucket for everyone behind a proxy), and the only Docker image was a Debug SDK image running as root that migrated on start.

## Options

- **Takeover:** stop auto-linking social logins to accounts that have a password; wipe the password when auto-linking; or require email confirmation for password registration. The first loses a documented convenience and the second silently removes a legitimate password; only confirmation also stops an attacker from occupying an email.
- **Brute force:** per-IP rate limits only; or per-IP limits plus a per-invitation cap on wrong codes. Per-IP limits alone do not stop guesses spread over many addresses.
- **Enumeration:** fix only the cheap leaks (the invitation check and sign-in timing); or also make registration answer the same whether or not the email is taken, emailing the owner instead.
- **Revocation:** a session version stored on the account and checked per request; a shorter token lifetime; or accept the risk.
- **Lockout after wrong passwords:** a timed lockout, or a lock that only the owner can lift through an emailed link.

## Decision

- **Email confirmation.** Password registration creates the admin as `Unverified` with a single-use link token (hashed, 24 hours) and emails `/confirm-email?email=…&token=…`; `POST /accounts/activation/confirm-email` makes the account `Active`. Only `Active` accounts can sign in or have a social login linked. An expired unconfirmed registration is replaced by the next registration of that email; a registration with a verified Google email replaces an unconfirmed one immediately.
- **Registration does not reveal registered emails.** `POST /organizations` answers `202` with no body in every valid case; when the email already has an account it creates nothing and emails the owner a notice. `POST /accounts/activation/verify` and activation answer the same `403` for an active account as for any other mismatch. Sign-in verifies a dummy password hash when no account can match.
- **Rate limits.** Rejections answer `429` with `Retry-After`. A per-IP `public` policy (`RateLimit:PublicPermitLimit` per minute, default 10) covers registration, invitation verify/activate/resend/confirm-email, Google registration and activation, and unlock; `sign-in` keeps its own.
- **Invitation code attempts.** A wrong code with the right link token increments `InvitationFailedAttempts`; at 5 the invitation stops working until `resend` (or a re-invite) issues a new code. `resend` no longer changes the expiry and does nothing for an expired invitation.
- **Pending invitations count toward the member limit.** An invitation to a new email is rejected when active plus pending accounts reach the plan's limit; re-inviting a pending email is still allowed.
- **Sign-in lockout.** 5 wrong passwords in a row lock the account: it gets an unlock token (hashed), all its sessions end, and the owner is emailed `/unlock?email=…&token=…`. A locked account cannot sign in with a password or a social login, and the response is the same `401` as for wrong credentials. Only `POST /auth/unlock` with the emailed token lifts the lock. Like every emailed link, the unlock link expires (24 hours); a sign-in attempt on a locked account whose link has expired emails a fresh one, so an owner is never locked out for good, while an unexpired link is never replaced (no email flood). If the email cannot be sent the lock is undone, so an owner is never locked out without a link.
- **Every emailed link expires.** Invitation links after 7 days, email confirmation and unlock links after 24 hours. The other emails (the new-code reminder and the already-registered notice) carry no token.
- **Session revocation.** Tokens carry the account's `SessionVersion` (`sv` claim). Every authenticated request checks it against the database; sign-out and lockout increment it, ending the account's sessions on every device.
- **Token audience.** Tokens are issued with and validated against `Jwt:Issuer` and `Jwt:Audience` (both default `nexo`).
- **Proxies and HSTS.** `ForwardedHeaders:KnownProxies` lists the reverse proxies whose `X-Forwarded-For`/`X-Forwarded-Proto` are trusted (none by default), so rate limits key on the real client. HSTS is sent outside `Development`.
- **Password strength instead of a checklist.** Passwords are rated Weak, Reasonable, Strong, or Very strong from a score that counts both length and variety, each kind of character worth about four characters (see the [password rules](../requirements/US-003-LLD.md#password-rules)); Weak is refused. The web app shows the rating as a four-step bar with a one-line hint naming the quickest improvement (a missing kind, symbols first, then length), instead of a list of composition rules; a long passphrase is accepted without symbols or digits.
- **Production image.** `api/Dockerfile` has a `dev` stage (used by Compose) and a default `production` stage: Release build on the ASP.NET runtime image as its non-root user, with an EF Core migrations bundle (`efbundle`) run as a separate deploy step.

## Consequences

- A new admin cannot sign in until the confirmation email arrives; if the email is lost, the registration can be repeated once the link expires (24 hours), or the admin can register with Google instead.
- Anyone who knows an account's email can lock it with 5 wrong passwords (the per-IP rate limit slows but does not prevent this). The owner is emailed immediately and can unlock with one click; this was chosen over a timed lockout.
- Each authenticated request makes one extra primary-key read for the session version.
- A token of an account that no longer exists is now `401` (session ended) rather than reaching the action.
- Inviting an email that already has an account elsewhere still answers `409`; hiding it would mean inventing a pending account that does not exist. Only admins (who must confirm their own email) can reach it.
- The migrations `AddAccountSecurity` and `AddUnlockExpiry` add `InvitationFailedAttempts`, `FailedSignInAttempts`, `UnlockTokenHash`, `SessionVersion`, and `UnlockExpiresAt` to `Accounts`.
- Responses are not encrypted beyond HTTPS. The browser must be able to read what it displays, so any extra encryption key would ship to the same browser and anyone at that browser could read the data anyway. What keeps data safe is TLS, the httpOnly session cookie, and returning only what the signed-in user may see (no hashes, tokens, or counters in any response).
- The `WeatherForecast` template controller and its layers were removed.
- Rate limits and the lockout are invisible unless the web app explains them: every form maps failures through one helper (`describeApiError`), so a `429` gives the longest wait (`Retry-After` from the fixed-window limiter is the whole window, an upper bound), an unreachable API or a `5xx` says so, and server field errors appear under their field. Messages are one short sentence in a shared `Notice` (icon, faint tint, no border). A Google-only account has no password and can never be locked; the "Continuar com Google" button sits directly under the sign-in error.

## Related artifacts

[ADR-006](ADR-006-authentication.md), [ADR-009](ADR-009-session-cookie.md), [US-001](../requirements/US-001-create-organization-admin.md), [US-002](../requirements/US-002-register-member-email.md), [US-003](../requirements/US-003-create-member-account.md), [US-005](../requirements/US-005-sign-in.md), [Reference](../reference/README.md), [Database](../database/README.md).
