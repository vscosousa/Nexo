# US-005 - HLD - Sign in to an existing account

[Requirements](README.md) · [US-005](US-005-sign-in.md) · [LLD](US-005-LLD.md)

## Requirements recap

As an admin or member with an existing account, I want to sign in with my email and password, or with Google/Microsoft, so that I can access the system. Full acceptance criteria: [US-005](US-005-sign-in.md).

This HLD/LLD covers the email/password path in full detail. The Google/Microsoft path is decided at [ADR-006](../decisions/ADR-006-authentication.md) (issues the same kind of session, links the provider to the matched account); its concrete endpoint is not yet designed.

## Folder structure

New files this feature adds to `api/`:

```text
api/
├── Controllers/AuthController.cs
├── Domain/Dtos/SignInDto.cs
├── Dtos/SessionDto.cs
├── Services/
│   ├── IAuthService.cs
│   ├── AuthService.cs
│   ├── ITokenService.cs
│   └── TokenService.cs
└── Infrastructure/Repositories/
    └── IAccountRepository.cs (existing, reused)
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
| 200 OK | `SessionDto` (token, expiresAt) | Credentials correct; session issued |
| 400 Bad Request | Validation errors | Missing email or password |
| 401 Unauthorized | Generic error detail | No account for the email, account has no password set, or password incorrect (same response in all three cases, so as not to reveal which) |

## Related artifacts

[LLD](US-005-LLD.md), [SSD/SD diagrams](../us/US-005/README.md), [Domain model](../domain-models/README.md#accounts-and-organizations), [ADR-006](../decisions/ADR-006-authentication.md).
