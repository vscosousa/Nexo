# US-004 - HLD - Register a resource

[Requirements](README.md) · [US-004](US-004-register-resource.md) · [LLD](US-004-LLD.md)

**Status:** backend implemented; the caller is the account in the session token's `sub` claim ([ADR-006](../decisions/ADR-006-authentication.md)); frontend not implemented.

## Requirements recap

As association staff, I want to register a new resource with its details, so that it becomes available for search, reservation, and loan. Full acceptance criteria: [US-004](US-004-register-resource.md).

## Folder structure

Files for this feature in `api/`:

```text
api/
├── Controllers/ResourcesController.cs
├── Domain/
│   ├── Models/Resource.cs (Resource, ResourceStatus)
│   ├── Models/ResourceType.cs
│   └── Dtos/RegisterResourceDto.cs, ResourceDto.cs
├── Mappers/ResourceMapper.cs
├── Services/
│   ├── IResourceService.cs
│   └── ResourceService.cs
└── Infrastructure/Repositories/
    ├── IResourceRepository.cs
    └── ResourceRepository.cs
```

Reuses `IAccountRepository` and `IOrganizationRepository` from [US-001](US-001-HLD.md).

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

## Related artifacts

[LLD](US-004-LLD.md), [SSD/SD diagrams](../us/US-004/README.md), [Domain model](../domain-models/README.md#resources), [ADR-011](../decisions/ADR-011-multi-tenancy.md), [ADR-012](../decisions/ADR-012-resource-types.md).
