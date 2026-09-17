# Sequence diagrams (SD)

[Documentation index](../README.md)

Describe how internal participants collaborate to implement a scenario, at two levels of detail per story, with level 3 split by side:

- **Level 2**: the coarse interaction: actor, `Web UI`, and `Nexo API` (the backend as a whole, not a named controller; that's level 3). This is where the frontend and backend first connect, before either side's internals are shown.
- **Level 3 - Backend**: the full backend collaboration, every participant involved (controller, service, repository, mapper, `NexoDbContext`), matching the layers in [architecture](../architecture/README.md).
- **Level 3 - Frontend**: the full frontend collaboration (view, feature component, feature API service, shared `HttpClient`), ending at the same entry-point participant the backend diagram starts from.

See [designing SSD/SD diagrams](../guides/sequence-diagrams.md) for the activation-bar and naming convention. Instances live grouped by user story under [`docs/us/`](../us/README.md) (see there for the full folder layout and the list of stories).

## Scenario template

Inside `docs/us/US-001/sd/level-2/`, `.../level-3/backend/`, and `.../level-3/frontend/`, document:

**Requirement and SSD:** [links].
**Status:** [proposed / implemented].
**Participants (level 3 backend):** [interfaces, services, domain objects, repositories, or external systems].
**Participants (level 3 frontend):** [view, component, feature service, shared HTTP client].
**Level 2 diagram:** `[![Level 2](level-2/svg/US-001-level-2.svg)](level-2/puml/US-001-level-2.puml)`
**Level 3 - Backend diagram:** `[![Level 3 backend](level-3/backend/svg/US-001-level-3-backend.svg)](level-3/backend/puml/US-001-level-3-backend.puml)`
**Level 3 - Frontend diagram:** `[![Level 3 frontend](level-3/frontend/svg/US-001-level-3-frontend.svg)](level-3/frontend/puml/US-001-level-3-frontend.puml)`

| Step | Sender → receiver | Operation | Outcome |
| --- | --- | --- | --- |
| [1] | [Participants] | [Call or event] | [Response or state change] |

**Failure handling:** [errors, retries, and recovery, if applicable].
**Transaction boundaries:** [atomic operations and consistency expectations].
**Related design:** [links to architecture, database design, and decisions].

Keep actor-level behavior in the [SSD](../ssd/README.md) and reuse participants defined by the [architecture](../architecture/README.md).
