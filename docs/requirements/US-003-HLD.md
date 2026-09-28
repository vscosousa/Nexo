# US-003 - HLD - Create a member account

[Requirements](README.md) · [US-003](US-003-create-member-account.md) · [LLD](US-003-LLD.md)

**Status:** backend implemented for the password path; the response stops at `AccountDto` (no sign-in or session yet) and OAuth activation is not implemented; frontend password page implemented at `/activate` (redirects to sign-in, no session). See [design review gaps](README.md#design-review-gaps).

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
  "email": "string, required, valid email, at most 320 characters",
  "name": "string, required, at most 200 characters",
  "password": "string, required, must satisfy the password rules",
  "invitationToken": "string, required, the one-time token from the invitation email"
}
```

Responses:

| Status | Body | Condition |
| --- | --- | --- |
| 200 OK | `AccountDto` (id, email, name, role, status: Active, organizationId) | Pending account activated |
| 400 Bad Request | Validation errors | Missing or invalid fields, over-long fields, or a password that breaks the [password rules](US-003-LLD.md#password-rules) |
| 403 Forbidden | Error detail | No account exists for `email`, or `invitationToken` does not match (same message for both, so it does not reveal which) |
| 409 Conflict | Error detail | The account for `email` is already `Active` (also when another request activated it first), or the organization's `MemberLimit` of active accounts is reached |

## Related artifacts

[LLD](US-003-LLD.md), [SSD/SD diagrams](../us/US-003/README.md), [Domain model](../domain-models/README.md#accounts-and-organizations), [ADR-006](../decisions/ADR-006-authentication.md).
