# US-004 - Register a resource

[Requirements](README.md)

**ID and title:** US-004 - Register a resource.
**Status:** draft.
**Story:** As association staff, I want to register a new resource with its details, so that it becomes available for search, reservation, and loan.

**Preconditions:** The user has an account (per [US-003](US-003-create-member-account.md)) with permission to manage resources. The resource is registered against the user's organization.

**Acceptance criteria:**

- Given a signed-in staff member and valid resource details (name, type, description), when they submit the registration, then the resource is created with status "Available" and appears in the resource list.
- Given required fields are missing or invalid, when the staff member submits the registration, then the system rejects it with validation errors and no resource is created.
- Given a user without permission to manage resources, when they attempt to register a resource, then the system rejects the request with an authorization error and no resource is created.

**Exceptions:**

- Missing or invalid required fields (empty name, unrecognized type).
- Unauthorized user attempting registration.

**Related artifacts:** [Domain model](../domain-models/README.md#resources), [HLD](US-004-HLD.md), [LLD](US-004-LLD.md), [SSD/SD diagrams](../us/US-004/README.md), [database design](../database/README.md) (not yet created), tests (not yet created).
