# Testing

[Documentation index](../README.md)

This page describes test selection, current coverage, and recorded validation. Passing scaffold tests does not mean the proposed business stories are implemented.

## Strategy

Test-driven development is required (see [AGENTS.md](../../AGENTS.md#working-rules)): a failing test comes first for every feature or fix, then the implementation follows until it passes.

See [ADR-005](../decisions/ADR-005-testing-frameworks.md) for the full reasoning behind the tools below. Summary:

| Kind | Tool | Location |
| --- | --- | --- |
| Unit (backend) | xUnit | `api/tests/` |
| Integration (backend) | xUnit + `Microsoft.AspNetCore.Mvc.Testing` | `api/tests/` |
| Unit / component (frontend) | Vitest + React Testing Library | `web/src/` (colocated with source) |
| E2E | Playwright | `tests/e2e/` |
| Acceptance | Plain xUnit, named in Given/When/Then form | `api/tests/` |
| Functional | Covered by integration/E2E tests of the same behavior; not a separate suite | n/a |
| Smoke | A tagged subset of the integration/E2E suites covering only critical paths | n/a |
| Performance | Deferred; no measurable target exists yet | n/a |

Backend integration tests run against the Compose PostgreSQL, in a separate `nexo_test` database created and migrated by the test factory. Start it first with `docker compose -f compose.yaml -f compose.test.yaml up -d db` ([compose.test.yaml](../../compose.test.yaml) publishes the port on `127.0.0.1:5433`; set `NEXO_TEST_DB` to use another connection string). Run `dotnet test Nexo.slnx` (backend), `npm test` in `web/` (frontend unit), and `npm test` in `tests/e2e/` (E2E, starts the Vite dev server automatically). See [technical reference](../reference/README.md#commands).

## Scenario template

Current application coverage consists of the frontend route guard, the browser smoke test, and the US-001 backend tests (`OrganizationMapperTests`, and the Given/When/Then acceptance tests in `OrganizationsEndpointTests`: successful registration, four missing/invalid-field cases, email conflict, case-insensitive email conflict, and US-006's chosen plan and missing or unknown `planId`). US-002 backend tests are `AccountMapperTests` and the acceptance tests in `AccountInvitationServiceTests` and `OrganizationServiceTests` (the lost email race reports a conflict) and `AccountInvitationsEndpointTests` (success, invalid email, non-admin and other-organization callers, missing/unknown caller, invited and active email conflicts, member limit reached, invited accounts not counting toward it). US-003 backend tests (password path) are `AccountMapperTests` (activation), the acceptance tests in `AccountActivationsEndpointTests` (success, missing/invalid fields and password-rule cases, unregistered email, wrong token, already-active conflict, member limit reached) and `AccountActivationServiceTests` (a lost activation race reports a conflict; two activations racing for the last seat, held in step by a test repository, let only one through). `PasswordPolicyTests` and `InvitationTokensTests` cover the shared password rules and invitation tokens; the US-001 and US-002 endpoint tests also cover the password, token, and length rules. The fake mail server has its own tests (`SmtpReceiverTests`, `InboxApiTests`, `MailStoreTests`, in `api/tests/FakeSmtp/`) and `SmtpEmailSenderTests` sends through the real SMTP sender into it; `InvitationEmailTests` covers the invitation template. API endpoint tests replace `IEmailSender` with `CapturingEmailSender`, so they read the invitation token from the captured email and never use SMTP. Endpoint test classes share one database, so test parallelization is disabled. US-005 backend tests are `SignInEndpointTests` (success sets an httpOnly, `Secure`, `SameSite=None` session cookie with the token claims, wrong password and unknown email giving one identical 401, SSO-only and `Invited` accounts, missing fields) and `ExternalSignInEndpointTests`, which start at the callback with a forged external cookie because the Google handshake cannot run offline (linking on first use, repeat sign-in, unverified email, no matching account, `Invited` account, no cookie, redirect to Google, unknown or unconfigured provider), and `SessionEndpointTests` ([ADR-009](../decisions/ADR-009-session-cookie.md): `GET /auth/me` with and without a session, including the account's email and name, sign-out with the anti-forgery token, a cookie-authenticated change without the token or with one fetched before signing in rejected). `HttpSecurityEndpointTests` checks CORS allows credentials for the configured origin. `SessionEndpointTests` also checks the anti-forgery token works over plain HTTP, as in local development. `ExternalRegistrationEndpointTests` (US-001/US-003 with Google, starting from a forged external cookie that carries the intent) covers the callback returning to the form or with an error for an unverified email, the pending details, registration with edited names (passwordless admin, linked, signed in), an email already registered, a missing name, a missing anti-forgery token, nothing pending, the wrong intent, and activation (success, a Google email other than the invited one, a wrong code, the member limit). US-005 frontend tests (Vitest) are `SignInForm.test.tsx`, `LoginCallbackPage.test.tsx` (opens the app when the session check succeeds), `RequireAuth.test.tsx` (no session, session, and a 401 sending the user to `/login` without a reload), and `client.test.ts` (credentials sent, anti-forgery token fetched once for changes and refetched after a reset, 401 handler). `ActivateAccountForm.test.tsx` covers a failed resend showing an error, the Google link carrying the invitation, activating from pending Google details with edited names, and a Google account that is not the invited one. `RegisterForm.test.tsx` covers the Google link carrying the plan and organization name, registering from pending Google details with edited names, a failed Google sign-in, and expired Google details. The real Google handshake is verified manually only. [ADR-010](../decisions/ADR-010-account-security-hardening.md) security behavior is covered by: `OrganizationsEndpointTests` (registration answers `202` for a new and a taken email alike and emails the owner instead, the admin stays `Unverified` and cannot sign in until the emailed link confirms it, wrong and expired links, an expired registration replaced); `ExternalRegistrationEndpointTests` (a Google registration replacing an unconfirmed one); `ExternalSignInEndpointTests` (no Google sign-in or linking for an unconfirmed or locked account); `AccountActivationsEndpointTests` (5 wrong codes with the link lock the invitation, wrong link tokens are not counted, a resent code unlocks it, resend keeps the expiry and ignores expired invitations, an active account answers like any mismatch); `AccountInvitationsEndpointTests` (pending invitations count toward the member limit, re-inviting a pending email still works, a token of an unknown account is rejected); `AccountLockoutEndpointTests` (lockout after 5 wrong passwords with one unlock email, the count resetting after a correct password, unlocking, wrong and expired unlock links, the link expiring after a day, a sign-in attempt emailing a fresh link once the old one expired, sessions ended by the lock); `SessionEndpointTests` (sign-out ending the session on another device, a token for another audience rejected); and `HttpSecurityEndpointTests` (the public rate limit on each endpoint, HSTS only outside Development). `EmailLinkPage.test.tsx` covers the web `/confirm-email` and `/unlock` pages (success sends the token once, an invalid link explains itself). `apiError.test.ts` covers the shared failure messages (no connection, `429` with and without `Retry-After`, `5xx`, endpoint-specific statuses, field errors); `SignInForm.test.tsx` adds the rate-limit and no-connection messages, `RegisterForm.test.tsx` a server field error shown under its field, and `ActivateAccountForm.test.tsx` a rate-limited code check. `SignInForm.test.tsx` also checks that a success notice from an earlier step disappears once a sign-in fails, and `ScrollToTop.test.tsx` that navigating to another page starts it at the top. `PasswordPolicyTests` and `passwordStrength.test.ts` rate the same table of passwords, so the API and the strength bar cannot drift apart; `RegisterForm.test.tsx` checks the four-step bar updating as the password is typed, its hints (a missing symbol, the next length step), and flagging the organization's name; `passwordStrength.test.ts` also covers which improvement is suggested. `Notice.test.tsx` covers the shared message component (alert for errors, status for successes, decorative icon). `HttpSecurityEndpointTests` checks the `Retry-After` header. Sign-in timing for unknown emails and trusting `ForwardedHeaders:KnownProxies` have no automated test. US-004 backend tests are `ResourceMapperTests` and the acceptance tests in `ResourcesEndpointTests` (an admin and a staff member registering, a blank description stored as null, a member or a not-yet-active staff account rejected before validation, missing or invalid name, description, and type, another organization's custom type rejected by the [ADR-011](../decisions/ADR-011-multi-tenancy.md) query filter, the organization's own custom type accepted, the Free plan's resource limit, another organization's resources not counting, no session). `ResourcesEndpointTests` also covers `GET /resource-types` (system types plus only the caller's organization's custom ones, and no session). US-004 frontend tests (Vitest) are `ResourcesPage.test.tsx` (registering and the success notice, translated system type names, no register button for a member, missing name and type marked without a request, a server field error under its field, the 403 and 409 messages, types that fail to load, the `?new=1` dashboard shortcut, cancelling) and `AppLayout.test.tsx` (only built sections in the navigation, the person's name and role, the dashboard shortcut shown to admin and staff only, sign-out); `AuthContext.test.tsx` covers the signed-in account read on load and after signing in, and cleared on sign-out. `AccountInvitationsEndpointTests` covers inviting with the `Staff` role, a re-invite changing the role, and a missing or non-invitable role; `PlansEndpointTests` and `PlanMapperTests` cover the `HasCustomResourceTypes` flag. US-001 to US-004 have no browser tests yet. For a feature or fix, reproduce its behavior with a failing test at the narrowest useful level, implement it, then run the relevant suite.

**ID:** [TEST-001].
**Requirement:** [US or NFR link].
**Environment and version:** [reproducible setup].
**Data and preconditions:** [synthetic fixtures and initial state].
**Steps:** [actions or commands].
**Expected result:** [pass criteria].
**Actual result and evidence:** [outcome and report link when executed].

## Performance checks

No performance scenarios have been executed or targets agreed. Define workload, metric, threshold, and environment with a measurable requirement before selecting a tool or recording results.

## Execution

Diagram-tool tests run separately with `pwsh -File tools/generate/test-plantuml-diagrams.ps1`; see the [diagram guide](../guides/diagrams.md). The only current CI workflow renders/tests diagrams; application checks and the pre-commit hook remain local.

Validation results from this documentation/tooling update are recorded below. Application unit/integration tests, browser E2E tests, and diagram generation were not run because application code and diagram sources were unchanged.

| Check (2026-09-17, Windows) | Result |
| --- | --- |
| Local Markdown link/heading scan | 48 files, 537 local links checked; no missing files or anchors (external URLs not checked) |
| `git diff --check` | Passed |
| `npm run lint` in `web/` | Passed with the existing `react(only-export-components)` warning in `AuthContext.tsx` |
| `npm run build` in `web/` | Passed |
| `dotnet build Nexo.slnx -warnaserror` | Passed, zero warnings/errors |

Validation for US-004 (register a resource, backend) with [ADR-011](../decisions/ADR-011-multi-tenancy.md) and [ADR-012](../decisions/ADR-012-resource-types.md):

| Check (2026-09-29, Windows) | Result |
| --- | --- |
| `dotnet test Nexo.slnx` (Compose PostgreSQL on port 5433) | 235 passed, 0 failed |
| `dotnet build Nexo.slnx -warnaserror` | Passed, zero warnings/errors |
| Removing the `ResourceTypes` query filter, then running the other-organization custom type test | The test failed, so it guards the filter (filter restored) |
| After adding `GET /resource-types`: `dotnet test Nexo.slnx` | 237 passed, 0 failed |
| `npx vitest run` in `web/` | 119 passed in 20 files |
| `npm run lint`, `npm run build`, `npx prettier --check src` in `web/` | Passed; lint shows only the three existing warnings |
| Manual run with `docker compose up --build` (Chrome, 1568×777 and a 390×844 frame, dark and light themes) | Signed in as an admin, opened the register dialog from the dashboard, types loaded from the API in Portuguese, registered a room, and the row was stored as `Available`; no horizontal scroll at 390px, 44px tap targets, 16px inputs |
| After the app shell revision (`/auth/me` with name and email): `dotnet test Nexo.slnx`; `npx vitest run`, lint, build, `prettier --check src` in `web/` | 237 passed; 119 passed; lint shows only the three existing warnings; build and Prettier passed |
| Manual check of the revised shell (Chrome at 1920×951, dark theme, and a 390×844 frame) | Sidebar darker than the page, person block with initials, name, and role, full-width top bar and content, register dialog opens and closes with Escape; no horizontal scroll at 390px |
| Spaces split from resources (`Room` type replaced by `Utensil`): `dotnet test Nexo.slnx`, `npx vitest run`, lint, build | 237 passed; 119 passed; lint shows only the three existing warnings; build passed |
| `ReplaceRoomWithUtensilResourceType` applied to the local dev database by the API on start | System types became Equipment, Other, Utensil, Vehicle; the one resource of type Room moved to Other |

Validation for the [ADR-010](../decisions/ADR-010-account-security-hardening.md) security hardening:

| Check (2026-09-29, Windows) | Result |
| --- | --- |
| `dotnet test Nexo.slnx` (Compose PostgreSQL on port 5433) | 211 passed, 0 failed |
| `dotnet build Nexo.slnx -warnaserror` | Passed, zero warnings/errors |
| `npx vitest run` in `web/` | 101 passed in 17 files |
| `npm run lint` in `web/` | Passed with three existing warnings (`AuthContext.tsx`, `Preferences.tsx`, `useScrollReveal.ts`) |
| `npm run build` and `npx prettier --check src` in `web/` | Passed |
| `docker build --target production api` | Built; the container runs as UID 1654, contains `Nexo.Api.dll` and `efbundle`, and starts in Production |
| `efbundle --connection` against `nexo_test` | Ran; reported the database already up to date |
| `docker build --target dev api` | Built |
| `tools/generate/generate-plantuml-diagrams.ps1` | Rendered 28 diagrams; only the edited US-001, US-005, and domain-model SVGs changed |
| Local Markdown link/anchor scan | 59 files, 797 local links; 11 broken, all in files this change did not touch (`ADR-007`, `US-006` requirement and LLD, `docs/sd/README.md`, `docs/ssd/README.md`) |
| `git diff --check` | Passed |

The browser E2E suite, the real Google handshake, and a real SMTP provider were not exercised.

See [requirements](../requirements/README.md) for acceptance criteria and [guides](../guides/README.md) for setup.
