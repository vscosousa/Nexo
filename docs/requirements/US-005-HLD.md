# US-005 - HLD - Sign in to an existing account

[Requirements](README.md) · [US-005](US-005-sign-in.md) · [LLD](US-005-LLD.md)

**Status:** implemented (password and Google). See [design review gaps](README.md#design-review-gaps) for what remains open.

## Requirements recap

As an admin or member with an existing account, I want to sign in with my email and password, or with Google/Microsoft, so that I can access the system. Full acceptance criteria: [US-005](US-005-sign-in.md).

This HLD/LLD covers the email/password path and the Google/Microsoft redirect flow ([ADR-006](../decisions/ADR-006-authentication.md): the same kind of session, and the provider linked to the matched account). Microsoft is coded but its logins are rejected (see [design review gaps](README.md#design-review-gaps)).

## Folder structure

Proposed files for this feature in `api/`:

```text
api/
├── Controllers/AuthController.cs
├── Domain/Dtos/SignInDto.cs
├── Domain/Dtos/SessionDto.cs
├── Domain/Models/ExternalLogin.cs
├── Domain/Exceptions/UnauthorizedException.cs
├── Services/
│   ├── IAuthService.cs
│   ├── AuthService.cs
│   ├── ITokenService.cs
│   └── TokenService.cs
└── Infrastructure/Repositories/
    ├── IAccountRepository.cs (existing, reused)
    └── IExternalLoginRepository.cs
```

Frontend, in `web/src/auth/`: `LoginPage.tsx`, `SignInForm.tsx`, `authService.ts`, `LoginCallbackPage.tsx`; the dev server proxies `/api` to the API (`web/vite.config.ts`).

```text
```

## API contract

**`POST /auth/sign-in`**

Request body (`SignInDto`):

```json
{
  "email": "string, required, valid email",
  "password": "string, required"
}
```

Responses:

| Status | Body | Condition |
| --- | --- | --- |
| 204 No Content | none; sets the `nexo_session` cookie | Credentials correct; session issued |
| 400 Bad Request | Validation errors | Missing email or password |
| 401 Unauthorized | Generic error detail | No account for the email, account not `Active`, account has no password set, account locked, or password incorrect (same response in every case, so as not to reveal which). The 5th wrong password in a row locks the account and emails the owner an unlock link |
| 429 Too Many Requests | None | More than `RateLimit:SignInPermitLimit` attempts per minute from the address |

The cookie holds an HS256 JWT valid for 8 hours with `sub` (account id), `orgId`, `role`, and `sv` (session version) claims, issued for `Jwt:Issuer`/`Jwt:Audience` and signed with the `Jwt:Key` setting. `POST /auth/sign-out` ends every session of the account; `POST /auth/unlock` (`{ email, token }` → 200, or 403) lifts a lock ([ADR-010](../decisions/ADR-010-account-security-hardening.md)). It is `HttpOnly`, `Secure`, `SameSite=None`, and expires with the token, per [ADR-009](../decisions/ADR-009-session-cookie.md).

**`GET /auth/external/{provider}`** (`google` or `microsoft`): redirects the browser to the provider (`302`) with `prompt=select_account`, so the provider always shows its account chooser; `404` if the provider is unknown or not configured.

**`GET /auth/external/callback`**: the provider returns here (through the provider handler's `/signin-{provider}` endpoint). It always answers `302` to the web app: `{Email:WebBaseUrl}/login/callback` on success, having set the same session cookie (the token never appears in a URL), or `{Email:WebBaseUrl}/login?error=oauth` on any failure.

## Related artifacts

[LLD](US-005-LLD.md), [SSD/SD diagrams](../us/US-005/README.md), [Domain model](../domain-models/README.md#accounts-and-organizations), [ADR-006](../decisions/ADR-006-authentication.md).
