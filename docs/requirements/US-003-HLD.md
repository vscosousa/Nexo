# US-003 - HLD - Create a member account

[Requirements](README.md) · [US-003](US-003-create-member-account.md) · [LLD](US-003-LLD.md)

**Status:** proposed; not implemented. See [design review gaps](README.md#design-review-gaps) before implementing this contract.

## Requirements recap

As a person whose email was registered by an admin, I want to create my account with either email/password or Google/Microsoft, so that I can use the system as a member of that organization. Full acceptance criteria: [US-003](US-003-create-member-account.md).

This activates the pending `Account` row that [US-002](US-002-HLD.md) already created (`Status = Invited`): it sets `Name` and credentials and flips `Status` to `Active`. It does not insert a new `Account`. This HLD/LLD covers the email/password path in full detail. The Google/Microsoft path is decided at [ADR-006](../decisions/ADR-006-authentication.md) (same rule: an `Invited` account must exist for the OAuth email); its concrete endpoint is not yet designed.

## Folder structure

Proposed files for this feature in `api/`:

```text
api/
├── Controllers/AccountActivationsController.cs
├── Domain/Dtos/ActivateAccountDto.cs
├── Mappers/AccountMapper.cs (existing, extended: ApplyActivation)
└── Services/
    ├── IAccountActivationService.cs
    └── AccountActivationService.cs
```

Reuses `Domain/Dtos/AccountDto.cs` and `Infrastructure/Repositories/IAccountRepository` from [US-002](US-002-HLD.md).

## API contract

**`POST /accounts/activation`**

Request body (`ActivateAccountDto`):

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
| 200 OK | `AccountDto` (id, email, name, role, status: Active, organizationId) | Pending account activated |
| 400 Bad Request | Validation errors | Missing or invalid fields |
| 403 Forbidden | Error detail | No account (`Invited` or otherwise) exists for `email` |
| 409 Conflict | Error detail | The account for `email` is already `Active` |

## Related artifacts

[LLD](US-003-LLD.md), [SSD/SD diagrams](../us/US-003/README.md), [Domain model](../domain-models/README.md#accounts-and-organizations), [ADR-006](../decisions/ADR-006-authentication.md).
