# US-002 - HLD - Register a member's email to an organization

[Requirements](README.md) · [US-002](US-002-register-member-email.md) · [LLD](US-002-LLD.md)

**Status:** backend implemented; the caller is the account in the bearer token's `sub` claim ([ADR-006](../decisions/ADR-006-authentication.md)); frontend not implemented. See [design review gaps](README.md#design-review-gaps).

## Requirements recap

As an admin, I want to register a person's email against my organization, so that they become eligible to create their own account. Full acceptance criteria: [US-002](US-002-register-member-email.md).

This registers the email as a pending `Account` (`Status = Invited`, `Role = Member`, no credentials yet), rather than a separate entity. See [Domain model](../domain-models/README.md#accounts-and-organizations). [US-003](US-003-create-member-account.md) later activates that same `Account` row; it does not insert a new one.

## Folder structure

Proposed files for this feature in `api/`:

```text
api/
├── Controllers/
│   ├── AccountInvitationsController.cs
│   └── AccountActivationsController.cs (Resend lives here alongside US-003's Activate/Verify)
├── Domain/Dtos/
│   ├── InviteMemberDto.cs
│   ├── ResendInvitationDto.cs
│   └── AccountDto.cs
├── Mappers/AccountMapper.cs
└── Services/
    ├── IAccountInvitationService.cs (Invite and Resend)
    ├── AccountInvitationService.cs
    └── InvitationTokens.cs, InvitationEmail.cs (shared with US-003)
```

Reuses `Infrastructure/Repositories/IAccountRepository` and `IOrganizationRepository` from [US-001](US-001-HLD.md).

## API contract

**`POST /organizations/{organizationId}/invitations`**

Request body (`InviteMemberDto`):

```json
{
  "email": "string, required, valid email, at most 320 characters"
}
```

Responses:

| Status | Body | Condition |
| --- | --- | --- |
| 201 Created | `AccountDto` (id, email, firstName: null, lastName: null, role: Member, status: Invited, organizationId); the link token and code are only emailed, never returned | Pending account created (or, for an email still pending in this organization, re-invited in place with a fresh link token and code) and invitation email sent |
| 400 Bad Request | Validation errors | Missing or invalid email, or longer than 320 characters |
| 401 Unauthorized | none | The bearer token is missing, invalid, or expired |
| 403 Forbidden | Error detail | Caller is not the admin of `organizationId` |
| 409 Conflict | Error detail | Organization's `MemberLimit` reached (counting `Active` accounts only), or the email already has an `Active` account, or an `Invited` one in a *different* organization |
| 500 Internal Server Error | `ProblemDetails` | The invitation email could not be sent; a new pending account is removed again so the invitation can be retried, a re-invited one is left as is |

**`POST /accounts/activation/resend`**

Lets the invited person themselves ask for a fresh code. No authentication, and distinct from the admin re-inviting above: only the code is rotated, never the link token, so an activation page the person already has open (with the original link's token in its URL) keeps working once they enter the new code.

Request body (`ResendInvitationDto`):

```json
{
  "email": "string"
}
```

Responses:

| Status | Body | Condition |
| --- | --- | --- |
| 202 Accepted | none | Always, whether or not the email has a pending invitation. An email with a fresh code (no link) is sent if, and only if, one does; this response never reveals which |

## Related artifacts

[LLD](US-002-LLD.md), [SSD/SD diagrams](../us/US-002/README.md), [Domain model](../domain-models/README.md#accounts-and-organizations).
