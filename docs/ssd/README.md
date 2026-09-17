# System sequence diagrams (SSD)

[Documentation index](../README.md)

Describe observable interactions between actors and Nexo. Treat the system as a single participant; internal components belong in [sequence diagrams](../sd/README.md).

Instances live grouped by user story under [`docs/us/`](../us/README.md), as level 1 of that story's design (see there for the full folder layout and the list of stories). See [designing SSD/SD diagrams](../guides/sequence-diagrams.md) for the activation-bar and naming convention.

Each SSD now separates success and rejected-request outcomes, with numbered messages and a single system activation. Validation and persistence are not self-calls in this view. US-001 shows the required registration outcome; US-003 and US-005 show password scenarios. OAuth exchanges remain undesigned and are labeled accordingly.

## Scenario template

Inside `docs/us/US-001/ssd/level-1/`, document:

**Requirement:** [link to the user story].
**Actor:** [external user or system].
**Preconditions:** [starting state].
**Trigger:** [event that starts the scenario].

| Step | Actor input | System response |
| --- | --- | --- |
| [1] | [Request or event] | [Observable result] |

**Alternative and failure flows:** [validation failures and other outcomes].
**Postconditions:** [observable state after completion].
**Diagram:** `[![SSD](svg/US-001-level-1.svg)](puml/US-001-level-1.puml)`
**Internal design:** [link to the corresponding SD levels].
