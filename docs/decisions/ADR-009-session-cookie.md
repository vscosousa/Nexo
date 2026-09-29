# ADR-009 - Session in an httpOnly cookie with an anti-forgery token

[Documentation index](../README.md) · [Architecture decisions](README.md)

**Status:** Proposed; implemented for review.
**Date:** 2026-09-29.

## Context

[ADR-006](ADR-006-authentication.md) has the API issue a JWT per session. The web app first kept that JWT in `localStorage` and sent it as a bearer token; after Google sign-in the API passed it through the URL fragment. Any script running on the page (an injected dependency, a future XSS bug) could read `localStorage` and take the session away. The web app and API may also end up on different origins, or different sites, once deployed, so the design cannot assume the dev server's same-origin `/api` proxy.

## Options

- **Keep the token in `localStorage`.** Least work; the session stays readable by any script on the page.
- **httpOnly cookie, `SameSite=Lax`, same origin only.** Script cannot read the session, and `Lax` blocks most cross-site requests, but it only works while the web app and API share a site.
- **httpOnly cookie, `SameSite=None; Secure`, CORS with credentials, and an anti-forgery token.** Works across origins and sites; since the browser then sends the cookie on cross-site requests, changes need a token only the web app can obtain.

## Decision

Use the third option. The API sets the JWT as the `nexo_session` cookie (`HttpOnly`, `Secure`, `SameSite=None`, expiring with the token) on password sign-in (`204`, no token in the body) and on the social sign-in callback (redirect to `/login/callback`, no token in the URL). JWT bearer authentication reads the cookie when no `Authorization` header is sent, so bearer tokens keep working for non-browser clients and tests.

`GET /auth/csrf` returns an ASP.NET Core anti-forgery request token (its paired cookie is also `SameSite=None; Secure; HttpOnly`). Any state-changing request authenticated by the cookie must send it back in `X-XSRF-TOKEN`, or the API answers `400`. CORS answers only the configured origins, with credentials, so another site's scripts cannot read the token. Requests authenticated with an `Authorization` header skip the check, since a browser never attaches that header on its own.

`GET /auth/me` tells the web app whether it has a session, since it can no longer look at the token; `POST /auth/sign-out` clears the cookie.

## Consequences

- The web app keeps no token. `AuthProvider` asks `GET /auth/me` on load; any `401` afterwards marks the user signed out, and the route guard sends them to `/login` without reloading the page.
- The shared Axios client sends credentials, fetches the anti-forgery token once before the first change, and forgets it after sign-in or sign-out, because the API ties the token to the session's identity.
- `Secure` cookies need HTTPS, except on `localhost`, which browsers treat as secure, so local development over `http://localhost` keeps working.
- A browser that blocks third-party cookies will drop a `SameSite=None` cookie when the web app and API are on different *sites* (not just different subdomains). If deployment ends up cross-site, serve both from one site or revisit this.
- `VITE_API_BASE_URL` points the web app at the API directly instead of the `/api` proxy; the API's `Cors:AllowedOrigins` must then list the web app's origin.

## Related artifacts

[ADR-004](ADR-004-frontend-architecture.md), [ADR-006](ADR-006-authentication.md), [US-005](../requirements/US-005-sign-in.md), [Reference](../reference/README.md).
