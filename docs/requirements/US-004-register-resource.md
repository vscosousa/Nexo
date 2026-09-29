# US-004 - Register a resource

[Requirements](README.md)

**ID and title:** US-004 - Register a resource.
**Status:** implemented (backend and frontend; caller from the session token, per [ADR-006](../decisions/ADR-006-authentication.md)).
**Story:** As association staff, I want to register a new resource (an item such as equipment, a utensil, or a vehicle) with its details, so that it becomes available for search and loan. Spaces (rooms, halls, fields) are a separate concept, registered through [US-007](US-007-register-space.md).

**Preconditions:** The user has an `Active` account with role `Admin` or `Staff` (staff accounts are invited with that role, per [US-002](US-002-register-member-email.md)). The resource is registered against the user's organization.

**Acceptance criteria:**

- Given a signed-in admin or staff member and valid resource details (name, a type available to the organization, optional description), when they submit the registration, then the resource is created with status "Available" in their organization.
- Given required fields are missing or invalid (empty name, unknown type, or a type belonging to another organization), when they submit the registration, then the system rejects it with validation errors and no resource is created.
- Given a user without permission to manage resources (role `Member`), when they attempt to register a resource, then the system rejects the request with an authorization error and no resource is created, before validating the details.
- Given the organization has reached its plan's resource limit (e.g., 10 on the Free plan), when they submit the registration, then the system rejects it with a limit-reached error and no resource is created.

**Types:** a type is either a system type available to every organization (`Equipment`, `Utensil`, `Vehicle`, `Other`) or a custom type of the organization ([ADR-012](../decisions/ADR-012-resource-types.md)). Creating custom types is a later story.

**Exceptions:**

- Missing or invalid required fields (empty name, unrecognized type).
- Unauthorized user attempting registration.
- Plan resource limit reached.

**Related artifacts:** [Domain model](../domain-models/README.md#resources), [HLD](US-004-HLD.md), [LLD](US-004-LLD.md), [SSD/SD diagrams](../us/US-004/README.md), [database design](../database/README.md), [ADR-011](../decisions/ADR-011-multi-tenancy.md), [ADR-012](../decisions/ADR-012-resource-types.md), [backend tests](../testing/README.md).
