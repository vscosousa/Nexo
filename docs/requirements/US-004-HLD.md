# US-004 - HLD - Register a resource

[Requirements](README.md) · [US-004](US-004-register-resource.md) · [LLD](US-004-LLD.md)

## Requirements recap

As association staff, I want to register a new resource with its details, so that it becomes available for search, reservation, and loan. Full acceptance criteria: [US-004](US-004-register-resource.md).

## Folder structure

New files this feature adds to `api/`:

```text
api/
├── Controllers/ResourcesController.cs
├── Domain/
│   ├── Models/Resource.cs
│   └── Dtos/RegisterResourceDto.cs
├── Dtos/ResourceDto.cs
├── Mappers/ResourceMapper.cs
├── Services/
│   ├── IResourceService.cs
│   └── ResourceService.cs
└── Infrastructure/Repositories/
    ├── IResourceRepository.cs
    └── ResourceRepository.cs
```

## API contract

**`POST /resources`**

Request body (`RegisterResourceDto`):

```json
{
  "name": "string, required",
  "type": "string, required",
  "description": "string, optional"
}
```

Responses:

| Status | Body | Condition |
| --- | --- | --- |
| 201 Created | `ResourceDto` (id, name, type, description, status, organizationId) | Resource created with status "Available" |
| 400 Bad Request | Validation errors | Missing or invalid fields |
| 403 Forbidden | Error detail | Caller lacks permission to manage resources |

## Related artifacts

[LLD](US-004-LLD.md), [SSD/SD diagrams](../us/US-004/README.md), [Domain model](../domain-models/README.md#resources).
