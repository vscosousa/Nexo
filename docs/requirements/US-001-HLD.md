# US-001 - HLD - Create an organization and its admin account

[Requirements](README.md) · [US-001](US-001-create-organization-admin.md) · [LLD](US-001-LLD.md)

**Status:** backend and frontend implemented (`RegisterForm` two-step wizard at `/register/organization`). Google registration is implemented (see the [LLD](US-001-LLD.md#service-logic-organizationserviceregisterexternal)): it signs the admin in with the session cookie. Password registration still redirects to `/login`, since `POST /organizations` returns no session. See [design review gaps](README.md#design-review-gaps).

## Requirements recap

As a prospective admin, I want to register my organization and create my admin account in one step, so that I can start inviting members and managing resources. Full acceptance criteria: [US-001](US-001-create-organization-admin.md).

## Folder structure

Proposed files for this feature in `api/`:

```text
api/
├── Controllers/OrganizationsController.cs
├── Domain/
│   ├── Models/Plan.cs
│   ├── Models/Organization.cs
│   ├── Models/Account.cs
│   ├── Exceptions/ValidationException.cs
│   ├── Exceptions/ConflictException.cs
│   └── Dtos/RegisterOrganizationDto.cs
├── Domain/Dtos/OrganizationDto.cs
├── Mappers/OrganizationMapper.cs
├── Services/
│   ├── IOrganizationService.cs
│   └── OrganizationService.cs
└── Infrastructure/Repositories/
    ├── IOrganizationRepository.cs
    ├── OrganizationRepository.cs
    ├── IPlanRepository.cs
    ├── PlanRepository.cs
    ├── IAccountRepository.cs
    └── AccountRepository.cs
```

## API contract

**`POST /organizations`**

Request body (`RegisterOrganizationDto`):

```json
{
  "organizationName": "string, required, at most 200 characters",
  "adminName": "string, required, at most 200 characters",
  "adminEmail": "string, required, valid email, at most 320 characters",
  "password": "string, required, must satisfy the password rules"
}
```

Responses:

| Status | Body | Condition |
| --- | --- | --- |
| 201 Created | `OrganizationDto` (id, name, plan name) | Organization and admin account created |
| 400 Bad Request | `ValidationProblemDetails` (field errors) | Missing or invalid fields, over-long fields, or a password that breaks the [password rules](US-003-LLD.md#password-rules) |
| 409 Conflict | `ProblemDetails` | `adminEmail` already has an account (compared trimmed and case-insensitively) |

## Related artifacts

[LLD](US-001-LLD.md), [SSD/SD diagrams](../us/US-001/README.md), [Domain model](../domain-models/README.md#accounts-and-organizations).
