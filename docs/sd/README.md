# Sequence diagrams (SD)

[Documentation index](../README.md)

Describe how internal participants collaborate to implement a scenario, at two levels of detail per story:

- **Level 2**: the coarse interaction, actor, UI, and the entry-point participant (e.g., a controller) only.
- **Level 3**: the full collaboration, every participant involved (controller, service, repository, mapper, domain object, persistence), matching the layers in [architecture](../architecture/README.md).

Instances live grouped by user story under [`docs/us/`](../us/README.md) (see there for the full folder layout and the list of stories).

## Scenario template

Inside `docs/us/US-001/sd/level-2/` and `.../level-3/`, document:

**Requirement and SSD:** [links].
**Status:** [proposed / implemented].
**Participants (level 3):** [interfaces, services, domain objects, repositories, or external systems].
**Level 2 diagram:** `[![Level 2](level-2/svg/US-001-level-2.svg)](level-2/puml/US-001-level-2.puml)`
**Level 3 diagram:** `[![Level 3](level-3/svg/US-001-level-3.svg)](level-3/puml/US-001-level-3.puml)`

| Step | Sender → receiver | Operation | Outcome |
| --- | --- | --- | --- |
| [1] | [Participants] | [Call or event] | [Response or state change] |

**Failure handling:** [errors, retries, and recovery, if applicable].
**Transaction boundaries:** [atomic operations and consistency expectations].
**Related design:** [links to architecture, database design, and decisions].

Keep actor-level behavior in the [SSD](../ssd/README.md) and reuse participants defined by the [architecture](../architecture/README.md).
