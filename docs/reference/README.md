# Technical reference

[Documentation index](../README.md)

Commands and configuration below describe the current scaffold. Proposed business endpoints remain in the stories' HLD documents.

## Supported environment

| Dependency or platform | Supported version | Purpose |
| --- | --- | --- |
| Docker Desktop + Compose | Linux containers, Compose v2 | Runs the full local stack without host Node.js, .NET, or PostgreSQL |
| Node.js | 24.x | Runs the frontend toolchain (Vite, TypeScript) |
| npm | 11.x | Frontend package management |
| .NET SDK | 10.x | Builds and runs the backend |
| PostgreSQL | 17.x | Relational database (Compose container or local service) |
| Git + Bash | Git Bash on Windows | Local pre-commit hook and its regression tests |
| PowerShell | 7+ | Diagram generator and its tests |
| Java + Graphviz | Available on `PATH` | PlantUML rendering; see [diagram tooling](../../libs/README.md) for the pinned JAR |

## Key dependencies

| Package | Where | Purpose |
| --- | --- | --- |
| `react-router-dom` | `web/` | Client-side routing and the `RequireAuth` route guard |
| `axios` | `web/` | Shared HTTP client (`shared/http/client.ts`), attaches the auth token, handles `401` |
| `vitest`, `@testing-library/react` | `web/` | Frontend unit and component tests |
| `Microsoft.AspNetCore.Mvc.Testing` | `api/tests/` | Backend integration tests (`WebApplicationFactory`) |
| `@playwright/test` | `tests/e2e/` | End-to-end tests against the running app |
| `oxlint` | `web/` | Frontend linting |
| `prettier` | `web/` | Frontend formatting |

See [ADR-004](../decisions/ADR-004-frontend-architecture.md) and [ADR-005](../decisions/ADR-005-testing-frameworks.md) for why these were chosen over native alternatives.

## Configuration

| Setting | Description | Type | Required | Default |
| --- | --- | --- | --- | --- |
| `ConnectionStrings:NexoDb` | PostgreSQL connection string used by `NexoDbContext` | String | Yes | None, must be set locally |
| `Email:*` | Outgoing email; see [Email](#email) | Section | No | Fake SMTP server at `localhost:1025` |
| `Jwt:Key` | HS256 signing key for session tokens (at least 32 bytes); sign-in fails with a 500 if missing or short | String | Yes for sign-in | None locally (set with user secrets); Compose supplies a development value |
| `Cors:AllowedOrigins` | Browser origins allowed to call the API (any header and method, with credentials so the session cookie is sent; see [ADR-009](../decisions/ADR-009-session-cookie.md)) | String array | No | `["http://localhost:5173"]` |
| `RateLimit:SignInPermitLimit` | Sign-in attempts per client IP per minute on `POST /auth/sign-in`; extra attempts get `429` | Integer | No | `10` |
| `RateLimit:PublicPermitLimit` | Requests per client IP per minute, shared across the other unauthenticated endpoints that change state (registration, `/accounts/activation/*`, Google registration and activation, unlock); extra requests get `429` | Integer | No | `10` |
| `Jwt:Issuer`, `Jwt:Audience` | `iss` and `aud` of session tokens; tokens with other values are rejected | String | No | `nexo` |
| `ForwardedHeaders:KnownProxies` | IP addresses of reverse proxies whose `X-Forwarded-For`/`X-Forwarded-Proto` are trusted, so rate limits see the real client address; leave empty when the API is reached directly | String array | No | Empty (headers ignored) |
| `Authentication:Google:*` | Google sign-in `ClientId` and `ClientSecret`; see [Google sign-in](#google-sign-in) | Section | No | Absent: Google sign-in is off and returns 404 |
| `Authentication:Microsoft:*` | Microsoft `ClientId` and `ClientSecret`; the provider registers, but its logins are rejected (see [design review gaps](../requirements/README.md#design-review-gaps)) | Section | No | Absent |
| `VITE_API_URL` | Frontend dev server: API address the `/api` proxy forwards to (prefix stripped) | Environment | No | `http://localhost:5122`; Compose sets `http://api:8080` |
| `VITE_API_BASE_URL` | Frontend: base URL for API calls; set it to call the API directly (cross-origin, listed in `Cors:AllowedOrigins`) instead of the dev server's `/api` proxy | Environment | No | `/api` |
| `VITE_API_ORIGIN` | Frontend: public API address the Google sign-in link opens (must match the redirect URI host) | Environment | No | `http://localhost:5122` |

With Docker, [compose.yaml](../../compose.yaml) supplies `ConnectionStrings__NexoDb` using the internal `db:5432` address. PostgreSQL creates the `nexo` database and user on first startup, using disposable development credentials. Data lives in the `postgres-data` named volume; the database port is not published. The API runs in Development on container port 8080, published at `127.0.0.1:5122`; Vite is published at `127.0.0.1:5173`. No host user secrets are mounted.

For manual execution, the connection string is not stored in `appsettings.json` or `appsettings.Development.json`. Set it locally, once, from the `api/` directory:

```sh
dotnet user-secrets set "ConnectionStrings:NexoDb" "Host=localhost;Port=5432;Database=nexo;Username=<user>;Password=<password>"
```

Use dummy values above; set your own local PostgreSQL credentials. `dotnet user-secrets` stores the value outside the repository, keyed by the `UserSecretsId` in `Nexo.Api.csproj`.

### Email

The API sends email through SMTP. The defaults in `appsettings.json` target the [fake mail server](../guides/fake-mail-server.md); Compose overrides them from `.env` (template: [.env.example](../../.env.example)). A real provider needs only these values, and credentials belong in `.env`, user secrets, or environment variables, never in committed files.

| Setting | Compose variable | Description | Default |
| --- | --- | --- | --- |
| `Email:Host` | `EMAIL_HOST` | SMTP server host | `localhost` (`mail` in Compose) |
| `Email:Port` | `EMAIL_PORT` | SMTP port | `1025` |
| `Email:EnableSsl` | `EMAIL_ENABLE_SSL` | Upgrade the connection with STARTTLS (implicit TLS on 465 is not supported) | `false` |
| `Email:Username` | `EMAIL_USERNAME` | SMTP login; empty means none | empty |
| `Email:Password` | `EMAIL_PASSWORD` | SMTP password | empty |
| `Email:From` | `EMAIL_FROM` | Sender address | `Nexo <no-reply@nexo.local>` |
| `Email:WebBaseUrl` | `EMAIL_WEB_BASE_URL` | Web app address used for links in emails | `http://localhost:5173` |
| `Email:TimeoutSeconds` | none | SMTP timeout | `15` |

The `mail` service settings are `Smtp:Address` (`0.0.0.0` in the container, `127.0.0.1` otherwise), `Smtp:Port` (`1025`), `Store:Directory` (`/data` in the container, mounted from `MAIL_DATA_DIR`, default `./.data/mail`), and `urls` (`http://localhost:8025` outside Docker). Its inbox API is under `/api` (`/messages`, `/messages/{id}`, `/raw`, `/attachments/{index}`, `/read`, `DELETE`) and is unauthenticated: run it on loopback only.

## Commands

| Command | Working directory | Inputs | Result |
| --- | --- | --- | --- |
| `docker compose up --build` | repository root | Running Docker Desktop (Linux containers) | Builds and starts frontend, database, fake mail server, and API; applies migrations before the API starts |
| `docker build -t nexo-api api` | repository root | Docker | Builds the production API image (Release, ASP.NET runtime, non-root user, port 8080); it does not migrate on start |
| `docker run --rm --entrypoint ./efbundle nexo-api --connection "<connection string>"` | repository root | The production image; a reachable database | Applies pending migrations with the image's migrations bundle, as a deploy step before starting the API |
| `docker compose up --build -d` | repository root | Docker | Starts the same stack in the background; rebuild after source edits |
| `docker compose logs -f` | repository root | Running stack | Follows service logs |
| `docker compose down` | repository root | Compose stack | Removes containers/network; retains database volume |
| `docker compose down -v` | repository root | Disposable database only | Removes containers/network and **deletes database data** |
| `docker compose config --quiet` | repository root | Docker Compose | Validates Compose configuration |
| `npm install` | `web/` | None | Installs frontend dependencies |
| `npm ci` | `web/` or `tests/e2e/` | Committed lockfile | Installs the locked dependency tree for that project |
| `npm run dev` | `web/` | None | Starts the Vite dev server |
| `dotnet restore` | `api/` | None | Restores backend NuGet packages |
| `dotnet restore Nexo.slnx` | repository root | None | Restores API and test project dependencies |
| `dotnet run` | `api/` | None | Starts the ASP.NET Core Web API |
| `dotnet run --project tools/fake-smtp` | repository root | None | Starts the fake mail server (inbox `http://localhost:8025`, SMTP `127.0.0.1:1025`) without Docker |
| `dotnet run --launch-profile http` | `api/` | Local configuration | Starts the development API at `http://localhost:5122` |
| `dotnet tool restore` | `api/` | None | Restores local .NET tools (`dotnet-ef`) |
| `dotnet ef migrations add <Name>` | `api/` | Migration name | Generates a new EF Core migration in `Migrations/` |
| `dotnet ef database update` | `api/` | None | Applies pending migrations to the configured PostgreSQL database |
| `docker compose -f compose.yaml -f compose.test.yaml up -d db` | repository root | Docker | Starts the Compose PostgreSQL with port `127.0.0.1:5433` published, as backend integration tests require |
| `dotnet test Nexo.slnx` | repository root | Test database running (see above); optional `NEXO_TEST_DB` connection string | Runs backend unit and integration tests; integration tests use a separate `nexo_test` database |
| `npm test` | `web/` | None | Runs frontend unit and component tests |
| `npm install` | `tests/e2e/` | None | Installs Playwright |
| `npx playwright install chromium` | `tests/e2e/` | None | Downloads the Chromium browser used by tests |
| `npm test` | `tests/e2e/` | None | Runs E2E tests (starts the Vite dev server automatically) |
| `npm run lint` | `web/` | None | Lints the frontend (oxlint) |
| `npm run build` | `web/` | None | Checks TypeScript and builds the production frontend |
| `npm run format` / `npm run format:check` | `web/` | None | Formats the frontend (Prettier), or checks without writing |
| `dotnet format Nexo.slnx` | repository root | None | Formats the backend in place |
| `dotnet build Nexo.slnx -warnaserror` | repository root | None | Builds the backend, failing on any warning |
| `git config core.hooksPath tools/git-hooks` | repository root | Explicit confirmation if run by an agent | One-time setup: enables the [pre-commit hook](../guides/pre-commit-hook.md) for staged formatting, frontend lint/build, and backend builds |
| `pwsh -File tools/generate/test-plantuml-diagrams.ps1` | repository root | Diagram prerequisites | Tests rendering, stale-output removal, and malformed-source handling |
| `pwsh -File tools/generate/generate-plantuml-diagrams.ps1` | repository root | Diagram prerequisites | Regenerates SVGs from PlantUML sources |

## Google sign-in

Google sign-in is enabled only when `Authentication:Google:ClientId` and `ClientSecret` are set. Setup, once per developer:

1. In the Google Cloud console, create a project, configure the OAuth consent screen (External, testing mode) and add your Google account as a test user.
2. Create an OAuth client of type Web application with the authorized redirect URI `http://localhost:5122/signin-google`.
3. With Docker, put `GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET` in `.env` (see [.env.example](../../.env.example)). Without Docker, run `dotnet user-secrets set "Authentication:Google:ClientId" "<id>"` and the same for `ClientSecret` from `api/`.

Only an existing `Active` account whose Google email is verified can sign in; Google never creates an account. The provider identity is linked on first use.

## Interfaces

| Method and path | Current behavior |
| --- | --- |
| `POST /organizations` | US-001/US-006: registers an organization and an `Unverified` admin account (password path) on the plan in `planId` (required, from `GET /plans`) and emails a confirmation link; returns `202` with no body, also when the email is already registered (then nothing is created and the owner is emailed a notice), or `400` (including a missing or unknown `planId`), `409` (the same email registered at the same moment), `429`. No session is returned |
| `POST /accounts/activation/confirm-email` | US-001: confirms an admin's email with `{ email, token }` from the emailed link; `200`, or `403` (wrong, expired, or already used) |
| `POST /organizations/{organizationId}/invitations` | US-002: registers `{ "email", "role" }` (role `Member` or `Staff`, required; as a number, `1` or `2`) as an `Invited` account with that role; emails the person an invitation with a link token and code and returns `201` with `AccountDto`, or `400`, `401` (missing, invalid, or ended session), `403`, `409` (member limit reached counting pending invitations, or email already used); `500` if the email cannot be sent (no account is kept). Requires the session cookie (plus the `X-XSRF-TOKEN` header) or a bearer token; the caller is the account in its `sub` claim |
| `POST /accounts/activation` | US-003: activates the invited account for `{ email, firstName, lastName, password, linkToken, code }`; returns `200` with `AccountDto`, or `400`, `403` (no pending invitation matches: unknown or already active email, wrong link or code, expired, or 5 wrong codes tried), `409` (member limit reached, or activated concurrently), `429`. No sign-in or session yet |
| `POST /accounts/activation/verify` | US-003: checks `{ email, linkToken, code }` without activating; `200`, or `403` for every mismatch. A wrong code with the right link token counts toward the 5-attempt cap |
| `POST /accounts/activation/resend` | US-003: emails a fresh code for `{ email }` if it has an unexpired pending invitation (the link and expiry do not change; the attempt count resets); always `202` |
| `POST /auth/sign-in` | US-005: signs in with `{ "email", "password" }`; returns `204` and sets the httpOnly `nexo_session` cookie (`Secure`, `SameSite=None`; an HS256 JWT valid for 8 hours with `sub`, `orgId`, `role`, and `sv` claims), `400` (missing field), `401` (one generic body for an unknown email, no password set, an account that is not `Active`, a locked account, or a wrong password; the 5th wrong password in a row locks the account and emails an unlock link), or `429` (rate limit exceeded) |
| `GET /auth/me` | Returns the signed-in account (`id`, `organizationId`, `role`, `email`, `firstName`, `lastName`), read from the database for the session's account so the role is current, or `401` |
| `POST /auth/sign-out` | Clears the session cookie and the `nexo_xsrf` anti-forgery cookie, and ends every session of the account on all devices; returns `204` |
| `POST /auth/unlock` | US-005: unlocks a locked account with `{ email, token }` from the emailed link (valid 24 hours; a sign-in attempt after that emails a new one); `200`, or `403` (wrong or expired) |
| `GET /auth/csrf` | Returns `{ "token" }`, the anti-forgery token that changes made with the session cookie must send in `X-XSRF-TOKEN` (otherwise `400`); fetch a new one after signing in or out |
| `GET /auth/external/{provider}` | US-005: starts Google (`google`) or Microsoft (`microsoft`) sign-in by redirecting to the provider; `404` if the provider is unknown or not configured. With `intent=register&planId=…&organizationName=…` (US-001) or `intent=activate&email=…&token=…&code=…` (US-003), the callback keeps the identity pending for the form instead of signing in |
| `GET /auth/external/callback` | US-005: finishes social sign-in and sets the session cookie and redirects the browser to `{Email:WebBaseUrl}/login/callback` on success; for a registration or activation it keeps the identity in the 5-minute `External` cookie and redirects back to `/register/organization?planId=…&external=google` or `/activate?email=…&token=…&external=google` (`…&error=oauth` without a verified email) or `{Email:WebBaseUrl}/login?error=oauth` on any failure |
| `GET /plans` | US-006: lists the plans an organization can register on |
| `POST /resources` | US-004: registers `{ name, typeId, description? }` as an `Available` resource of the caller's organization; returns `201` with `ResourceDto`, or `401`, `403` (not an active admin or staff account, checked first), `400` (invalid fields, or a type the organization cannot use), `409` (plan resource limit reached). Requires the session cookie (plus `X-XSRF-TOKEN`) or a bearer token |
| `GET /resource-types` | US-004: lists the resource types the caller's organization can use (the system types `Equipment`, `Utensil`, `Vehicle`, `Other`, plus its own custom types), by name; `200` with `[{ id, name }]`, or `401` |
| `GET /auth/external/pending` | US-001/US-003: returns the pending Google details `{ intent, email, firstName, lastName, organizationName }`, or `401` when nothing is pending or it expired |
| `POST /auth/external/register` | US-001: registers `{ organizationName, adminFirstName, adminLastName, planId }` with the pending Google identity (no password), sets the session cookie, returns `201` with `OrganizationDto`; `400`, `401` (nothing pending), `409` (email or Google account already used). Requires `X-XSRF-TOKEN` |
| `POST /auth/external/activate` | US-003: activates the pending invitation with `{ firstName, lastName }` and the Google identity (no password), sets the session cookie, returns `200` with `AccountDto`; `400`, `401`, `403` (invalid invitation or Google email not the invited one), `409`. Requires `X-XSRF-TOKEN` |
| `GET http://localhost:8025/` | Fake mail server inbox (separate service, not the API) |
| `GET /openapi/v1.json` | Generated OpenAPI document, exposed only in Development |
| `GET /scalar/v1` | Scalar UI for manually exercising the API, exposed only in Development |

Every `429` carries `Retry-After` (seconds until the limit resets), exposed to cross-origin callers by CORS, so the web app can say how long to wait. Outside Development the API also sends `Strict-Transport-Security`. Every authenticated request checks the token's `sv` claim against the account, so a signed-out or locked account's tokens stop working at once ([ADR-010](../decisions/ADR-010-account-security-hardening.md)).

The HTTP launch profile uses port 5122; the HTTPS profile also uses `https://localhost:7110`. The SPA's Axios base URL is `/api`, but neither a Vite proxy nor that API route prefix is configured. The frontend and backend therefore require separate verification today.

Endpoints for other business stories in the [story HLDs](../requirements/README.md#user-stories) are proposed contracts.

For step-by-step instructions, see [guides](../guides/README.md).
