# US-004 - HLD - Register a resource

[Requirements](README.md) · [US-004](US-004-register-resource.md) · [LLD](US-004-LLD.md)

**Status:** implemented (backend and frontend); the caller is the account in the session token's `sub` claim ([ADR-006](../decisions/ADR-006-authentication.md)).

## Requirements recap

As association staff, I want to register a new resource (equipment, a utensil, a vehicle) with its details, so that it becomes available for search and loan. Spaces are separate ([US-007](US-007-register-space.md)). Full acceptance criteria: [US-004](US-004-register-resource.md).

## Folder structure

Files for this feature in `api/`:

```text
api/
├── Controllers/ResourcesController.cs, ResourceTypesController.cs
├── Domain/
│   ├── Models/Resource.cs (Resource, ResourceStatus)
│   ├── Models/ResourceType.cs
│   └── Dtos/RegisterResourceDto.cs, ResourceDto.cs, ResourceTypeDto.cs
├── Mappers/ResourceMapper.cs
├── Services/
│   ├── IResourceService.cs
│   └── ResourceService.cs
└── Infrastructure/Repositories/
    ├── IResourceRepository.cs
    └── ResourceRepository.cs
```

Reuses `IAccountRepository` and `IOrganizationRepository` from [US-001](US-001-HLD.md).

Files in `web/src/` ([ADR-004](../decisions/ADR-004-frontend-architecture.md)):

```text
web/src/
├── app/AppLayout.tsx, DashboardPage.tsx, app.css (signed-in shell and dashboard)
└── features/resources/
    ├── ResourcesPage.tsx
    ├── RegisterResourceForm.tsx (modal dialog)
    ├── resourcesService.ts
    └── permissions.ts (admin or staff)
```

## API contract

**`POST /resources`**

The organization is the caller's; it is never taken from the request. With the session cookie, the request must also carry the `X-XSRF-TOKEN` header from `GET /auth/csrf`.

Request body (`RegisterResourceDto`):

```json
{
  "name": "string, required, at most 200 characters",
  "typeId": "uuid, required, a system type or one of the organization's custom types",
  "description": "string, optional, at most 2000 characters"
}
```

Responses:

| Status | Body | Condition |
| --- | --- | --- |
| 201 Created | `ResourceDto` (id, name, typeId, typeName, description, status: Available, organizationId) | Resource created |
| 400 Bad Request | Validation errors | Missing or invalid fields, or a type the organization cannot use |
| 401 Unauthorized | none | The session is missing, invalid, or expired |
| 403 Forbidden | Error detail | Caller's role is not `Admin` or `Staff` (checked before validation) |
| 409 Conflict | Error detail | The organization's plan `ResourceLimit` is reached |

**`GET /resource-types`**

Lists the types the caller's organization can use, for the form's type select: the system types and the organization's custom types (never another organization's), ordered by name. Any signed-in account may call it.

| Status | Body | Condition |
| --- | --- | --- |
| 200 OK | `ResourceTypeDto[]` (id, name) | Always, for a valid session |
| 401 Unauthorized | none | The session is missing, invalid, or expired |

## Related artifacts

[LLD](US-004-LLD.md), [SSD/SD diagrams](../us/US-004/README.md), [Domain model](../domain-models/README.md#resources), [ADR-011](../decisions/ADR-011-multi-tenancy.md), [ADR-012](../decisions/ADR-012-resource-types.md).
