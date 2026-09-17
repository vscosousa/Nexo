# US-001 - HLD - Create an organization and its admin account

[Requirements](README.md) · [US-001](US-001-create-organization-admin.md) · [LLD](US-001-LLD.md)

## Requirements recap

As a prospective admin, I want to register my organization and create my admin account in one step, so that I can start inviting members and managing resources. Full acceptance criteria: [US-001](US-001-create-organization-admin.md).

## Folder structure

New files this feature adds to `api/`:

```text
api/
├── Controllers/OrganizationsController.cs
├── Domain/
│   ├── Models/Organization.cs
│   ├── Models/Account.cs
│   └── Dtos/RegisterOrganizationDto.cs
├── Dtos/OrganizationDto.cs
├── Mappers/OrganizationMapper.cs
├── Services/
│   ├── IOrganizationService.cs
│   └── OrganizationService.cs
└── Infrastructure/Repositories/
    ├── IOrganizationRepository.cs
    ├── OrganizationRepository.cs
    ├── IAccountRepository.cs
    └── AccountRepository.cs
```

## API contract

**`POST /organizations`**

Request body (`RegisterOrganizationDto`):

```json
{
  "organizationName": "string, required",
  "adminName": "string, required",
  "adminEmail": "string, required, valid email"
}
```

Responses:

| Status | Body | Condition |
| --- | --- | --- |
| 201 Created | `OrganizationDto` (id, name, plan) | Organization and admin account created |
| 400 Bad Request | Validation errors | Missing or invalid fields |
| 409 Conflict | Error detail | `adminEmail` already has an account |

## Related artifacts

[LLD](US-001-LLD.md), [SSD/SD diagrams](../us/US-001/README.md), [Domain model](../domain-models/README.md#accounts-and-organizations).
