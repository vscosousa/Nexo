# ADR-006 - Authentication: password and social login

[Documentation index](../README.md) · [Architecture decisions](README.md)

**Status:** Accepted.
**Date:** 2026-09-17.

## Context

Nexo needs sign-up and sign-in for accounts (per [US-001](../requirements/US-001-create-organization-admin.md), [US-003](../requirements/US-003-create-member-account.md)), plus social login (Google, Microsoft) alongside email/password, without contradicting decisions already made: no new paid service ([ADR-001](ADR-001-frontend-backend-stack.md)), stay within the modular monolith ([ADR-002](ADR-002-modular-monolith-architecture.md)).

## Options

- **ASP.NET Core Identity, `Account` as `IdentityUser`.** Least code, but `Account` inherits Identity's full field set (phone number, lockout, etc.) beyond what the domain model defines.
- **ASP.NET Core Identity's authentication pieces only** (password hasher, external OAuth login handlers for Google/Microsoft), with `Account` staying exactly as already modeled, backed by a custom `IUserStore<Account>` (or an equivalent minimal store) instead of Identity's own user table.
- **Auth-as-a-service** (Auth0, Clerk). Rejected: adds an external dependency and a free-tier ceiling, which the requirements' "no mandatory paid integrations" constraint argues against.
- **Fully custom** (hand-rolled password hashing and OAuth handshake, no Identity code at all). Rejected: reimplements security-sensitive code (hashing, external login token exchange) that Identity already provides and has been widely tested.

## Decision

Use ASP.NET Core Identity's authentication building blocks only: `PasswordHasher<Account>` for password hashing, and the built-in Google/Microsoft external authentication handlers for social login. `Account` is not an `IdentityUser` and gains no Identity-specific fields; a custom store wires Identity's pieces to the existing `Account` table.

The API issues its own JWT after a successful sign-in (password or social), independent of the provider. The `web/` SPA stores it the same way regardless of how the user signed in, using the shared Axios client already wired in [ADR-004](ADR-004-frontend-architecture.md).

Social login is available on both entry points: creating the first admin account ([US-001](../requirements/US-001-create-organization-admin.md)) and signing in to an existing account ([US-005](../requirements/US-005-sign-in.md)). The organization-membership rule from [US-002](../requirements/US-002-register-member-email.md)/[US-003](../requirements/US-003-create-member-account.md) (an email must be registered to an organization, as a pending account, before that account can be activated) applies identically whether the email arrives via a password sign-up form or a verified OAuth callback.

Token strategy: a single JWT per session, with a fixed expiry (no refresh token). Simpler to build and reason about for a local prototype; add refresh tokens only if session length becomes a real problem.

## Consequences

- New domain fields: `Account.PasswordHash` (nullable, an SSO-only account has none) and a new `ExternalLogin` concept (`Provider`, `ProviderKey`, linked `AccountId`) recording which OAuth identities are linked to which account.
- OAuth apps must be registered manually with Google and Microsoft (client ID/secret), documented as a setup step, and the secrets stored via `dotnet user-secrets` (same convention as [ADR-003](ADR-003-postgresql-database.md)).
- The invite-gate check (organization membership) must run identically in both the password sign-up path and the OAuth callback path; it lives once in `OrganizationService`/`AccountService`, not duplicated per auth method.
- No refresh tokens for now: a user's session ends when the JWT expires, requiring sign-in again. Acceptable for a local prototype without a "remember me for weeks" requirement.

## Related artifacts

[ADR-001](ADR-001-frontend-backend-stack.md), [ADR-004](ADR-004-frontend-architecture.md), [US-001](../requirements/US-001-create-organization-admin.md), [US-003](../requirements/US-003-create-member-account.md), [US-005](../requirements/US-005-sign-in.md), [Domain model](../domain-models/README.md#accounts-and-organizations).
