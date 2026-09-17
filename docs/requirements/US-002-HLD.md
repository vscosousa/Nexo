# US-002 - HLD - Register a member's email to an organization

[Requirements](README.md) · [US-002](US-002-register-member-email.md) · [LLD](US-002-LLD.md)

## Requirements recap

As an admin, I want to register a person's email against my organization, so that they become eligible to create their own account. Full acceptance criteria: [US-002](US-002-register-member-email.md).

This registers the email as a pending `Account` (`Status = Invited`, `Role = Member`, no credentials yet), rather than a separate entity. See [Domain model](../domain-models/README.md#accounts-and-organizations). [US-003](US-003-create-member-account.md) later activates that same `Account` row; it does not insert a new one.

## Folder structure

New files this feature adds to `api/`:

```text
api/
├── Controllers/AccountInvitationsController.cs
├── Domain/Dtos/InviteMemberDto.cs
├── Dtos/AccountDto.cs
├── Mappers/AccountMapper.cs
└── Services/
    ├── IAccountInvitationService.cs
    └── AccountInvitationService.cs
```

Reuses `Infrastructure/Repositories/IAccountRepository` and `IOrganizationRepository` from [US-001](US-001-HLD.md).

## API contract

**`POST /organizations/{organizationId}/invitations`**

Request body (`InviteMemberDto`):

```json
{
  "email": "string, required, valid email"
}
```

Responses:

| Status | Body | Condition |
| --- | --- | --- |
| 201 Created | `AccountDto` (id, email, name: null, role: Member, status: Invited, organizationId) | Pending account created |
| 400 Bad Request | Validation errors | Missing or invalid email format |
| 403 Forbidden | Error detail | Caller is not the admin of `organizationId` |
| 409 Conflict | Error detail | Organization's `MemberLimit` reached (counting `Active` accounts only), or the email already has an account (`Invited` or `Active`) anywhere |

## Related artifacts

[LLD](US-002-LLD.md), [SSD/SD diagrams](../us/US-002/README.md), [Domain model](../domain-models/README.md#accounts-and-organizations).
