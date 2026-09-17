# US-002 - HLD - Register a member's email to an organization

[Requirements](README.md) · [US-002](US-002-register-member-email.md) · [LLD](US-002-LLD.md)

## Requirements recap

As an admin, I want to register a person's email against my organization, so that they become eligible to create their own account. Full acceptance criteria: [US-002](US-002-register-member-email.md).

## Folder structure

New files this feature adds to `api/`:

```text
api/
├── Controllers/EligibleEmailsController.cs
├── Domain/
│   ├── Models/EligibleEmail.cs
│   └── Dtos/RegisterEligibleEmailDto.cs
├── Dtos/EligibleEmailDto.cs
├── Mappers/EligibleEmailMapper.cs
├── Services/
│   ├── IEligibleEmailService.cs
│   └── EligibleEmailService.cs
└── Infrastructure/Repositories/
    ├── IEligibleEmailRepository.cs
    └── EligibleEmailRepository.cs
```

## API contract

**`POST /organizations/{organizationId}/eligible-emails`**

Request body (`RegisterEligibleEmailDto`):

```json
{
  "email": "string, required, valid email"
}
```

Responses:

| Status | Body | Condition |
| --- | --- | --- |
| 201 Created | `EligibleEmailDto` (id, email, organizationId) | Email added to the organization's eligible list |
| 400 Bad Request | Validation errors | Missing or invalid email format |
| 403 Forbidden | Error detail | Caller is not the admin of `organizationId` |
| 409 Conflict | Error detail | Organization's member limit reached, or email already registered to this or another organization |

## Related artifacts

[LLD](US-002-LLD.md), [SSD/SD diagrams](../us/US-002/README.md), [Domain model](../domain-models/README.md#accounts-and-organizations).
