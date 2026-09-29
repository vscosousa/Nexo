# US-006 - HLD - Choose a plan before registering an organization

[Requirements](README.md) · [US-006](US-006-choose-plan-before-registering.md) · [LLD](US-006-LLD.md)

**Status:** proposed; not implemented. See [design review gaps](README.md#design-review-gaps) before implementing this contract.

## Requirements recap

As a prospective admin, I want to see and pick a plan before entering my organization's details, so that I know what I'm signing up for before creating an account. Full acceptance criteria: [US-006](US-006-choose-plan-before-registering.md).

## Folder structure

New files proposed for this feature in `api/` (`Plan` itself already exists, per [US-001](US-001-HLD.md)):

```text
api/
├── Controllers/PlansController.cs
├── Domain/Dtos/PlanDto.cs
├── Mappers/PlanMapper.cs
├── Services/
│   ├── IPlanService.cs
│   └── PlanService.cs
└── Infrastructure/Repositories/
    ├── IPlanRepository.cs   (add GetAllAsync, GetByIdAsync)
    └── PlanRepository.cs
```

This also changes two already-implemented US-001 files: `Domain/Dtos/RegisterOrganizationDto.cs` gains a required `PlanId`, and `Services/OrganizationService.cs` resolves the plan by that id instead of always looking up `Plan.Free`.

## API contract

**`GET /plans`**

Responses:

| Status | Body | Condition |
| --- | --- | --- |
| 200 OK | `PlanDto[]` (id, name, memberLimit) | Always; an empty array if no plans are seeded |

**`POST /organizations`** (existing, per [US-001](US-001-HLD.md); request body gains one field)

Request body (`RegisterOrganizationDto`):

```json
{
  "organizationName": "string, required, at most 200 characters",
  "adminFirstName": "string, required, at most 200 characters",
  "adminLastName": "string, required, at most 200 characters",
  "adminEmail": "string, required, valid email, at most 320 characters",
  "password": "string, required, must satisfy the password rules",
  "planId": "guid, required, must match a plan from GET /plans"
}
```

Responses: as in [US-001](US-001-HLD.md#api-contract), plus:

| Status | Body | Condition |
| --- | --- | --- |
| 400 Bad Request | `ValidationProblemDetails` (field errors) | `planId` missing or does not match an existing plan |

## Related artifacts

[LLD](US-006-LLD.md), [SSD/SD diagrams](../us/US-006/README.md), [Domain model](../domain-models/README.md#accounts-and-organizations), [US-001](US-001-create-organization-admin.md).
