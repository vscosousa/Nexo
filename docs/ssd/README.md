# System sequence diagrams (SSD)

[Documentation index](../README.md)

Describe observable interactions between actors and Nexo. Treat the system as a single participant; internal components belong in [sequence diagrams](../sd/README.md).

## Scenario template

Create `US-001-short-title.md` for each documented scenario, matching its requirement ID.

**Requirement:** [link to the user story].
**Actor:** [external user or system].
**Preconditions:** [starting state].
**Trigger:** [event that starts the scenario].

| Step | Actor input | System response |
| --- | --- | --- |
| [1] | [Request or event] | [Observable result] |

**Alternative and failure flows:** [validation failures and other outcomes].
**Postconditions:** [observable state after completion].
**Diagram:** [add the SSD using the actor and a single Nexo participant].
**Internal design:** [link to the corresponding SD when available].
