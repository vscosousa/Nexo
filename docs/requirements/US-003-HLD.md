# US-003 - HLD - Create a member account

[Requirements](README.md) · [US-003](US-003-create-member-account.md) · [LLD](US-003-LLD.md)

## Requirements recap

As a person whose email was registered by an admin, I want to create my account with either email/password or Google/Microsoft, so that I can use the system as a member of that organization. Full acceptance criteria: [US-003](US-003-create-member-account.md).

This HLD/LLD covers the email/password path in full detail. The Google/Microsoft path is decided at [ADR-006](../decisions/ADR-006-authentication.md) (same invite-gate rule, no password set, an `ExternalLogin` linked instead); its concrete endpoint is not yet designed.

## Folder structure

New files this feature adds to `api/`:

```text
api/
├── Controllers/AccountsController.cs
├── Domain/
│   ├── Models/Account.cs (existing, extended: nullable PasswordHash)
│   └── Dtos/CreateMemberAccountDto.cs
├── Dtos/AccountDto.cs
├── Mappers/AccountMapper.cs
├── Services/
│   ├── IAccountService.cs
│   └── AccountService.cs
└── Infrastructure/Repositories/
    ├── IEligibleEmailRepository.cs (existing, reused)
    └── IAccountRepository.cs (existing, reused)
```

## API contract

**`POST /accounts`**

Request body (`CreateMemberAccountDto`):

```json
{
  "email": "string, required, valid email",
  "name": "string, required",
  "password": "string, required"
}
```

Responses:

| Status | Body | Condition |
| --- | --- | --- |
| 201 Created | `AccountDto` (id, email, name, role, organizationId) | Member account created |
| 400 Bad Request | Validation errors | Missing or invalid fields |
| 403 Forbidden | Error detail | `email` is not registered to any organization |
| 409 Conflict | Error detail | `email` already has an account |

## Related artifacts

[LLD](US-003-LLD.md), [SSD/SD diagrams](../us/US-003/README.md), [Domain model](../domain-models/README.md#accounts-and-organizations), [ADR-006](../decisions/ADR-006-authentication.md).
