# Sequence diagrams (SD)

[Documentation index](../README.md)

Describe how internal participants collaborate to implement a scenario.

## Scenario template

Create `US-001-short-title.md` using the same ID as the requirement and SSD.

**Requirement and SSD:** [links].
**Status:** [proposed / implemented].
**Participants:** [interfaces, services, domain objects, repositories, or external systems].
**Diagram:** [show ordered calls, responses, and relevant alternative flows].

| Step | Sender → receiver | Operation | Outcome |
| --- | --- | --- | --- |
| [1] | [Participants] | [Call or event] | [Response or state change] |

**Failure handling:** [errors, retries, and recovery, if applicable].
**Transaction boundaries:** [atomic operations and consistency expectations].
**Related design:** [links to architecture, database design, and decisions].

Keep actor-level behavior in the [SSD](../ssd/README.md) and reuse participants defined by the [architecture](../architecture/README.md).
