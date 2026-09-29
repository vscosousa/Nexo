# US-007 - Register a space

[Requirements](README.md)

**ID and title:** US-007 - Register a space.
**Status:** draft; not designed or implemented.
**Story:** As association staff, I want to register a space (a room, hall, or field) with its details, so that members can reserve it for a period.

**Preconditions:** The user has an `Active` account with role `Admin` or `Staff`, as for [US-004](US-004-register-resource.md). The space is registered against the user's organization.

**Acceptance criteria:**

- Given a signed-in admin or staff member and valid space details, when they submit the registration, then the space is created and can be reserved.
- Given required fields are missing or invalid, when they submit the registration, then the system rejects it with validation errors and no space is created.
- Given a user without permission to manage spaces (role `Member`), when they attempt to register a space, then the system rejects the request with an authorization error and no space is created.

**Open decisions:** which details a space has (capacity, location, opening hours, photos), whether spaces have types, and whether they count toward the plan's resource limit. See the [domain model](../domain-models/README.md#spaces).

**Related artifacts:** [Domain model](../domain-models/README.md#spaces), [US-004](US-004-register-resource.md) (resources, which are items, not spaces).
