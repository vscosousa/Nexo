# User story design

[Documentation index](../README.md)

Design artifacts grouped by user story: system sequence diagrams (SSD) and sequence diagrams (SD), at three levels of detail, adapted from the [LAPR5](https://github.com/vscosousa/LAPR5_3DC_G15) template:

- **Level 1** (in `ssd/`): the SSD, actor versus Nexo as a single black-box participant.
- **Level 2** (in `sd/`): a coarse SD showing how everything connects: actor, `Web UI`, and `Nexo API` (the backend, as a whole, not a named controller), neither side's internals shown yet.
- **Level 3** (in `sd/`): each side in full detail, as two separate diagrams: **Backend** (`Web App` as the caller, then controller, service, repository, mapper, `NexoDbContext`, `Database`) and **Frontend** (actor, view, feature component, feature service, shared HTTP client, then `Nexo API` as the callee).

See [designing SSD/SD diagrams](../guides/sequence-diagrams.md) for the activation-bar and naming convention used throughout.

Each story folder also has an HLD and LLD document in [requirements](../requirements/README.md) (`US-001-HLD.md`, `US-001-LLD.md`): the HLD captures the feature's requirements, folder/file structure, and API contract; the LLD adds the full technical detail.

## Folder layout

```text
us/
└── US-001/
    ├── ssd/
    │   └── level-1/
    │       ├── puml/US-001-level-1.puml
    │       └── svg/US-001-level-1.svg   (generated, see [diagrams guide](../guides/diagrams.md))
    └── sd/
        ├── level-2/
        │   ├── puml/US-001-level-2.puml
        │   └── svg/US-001-level-2.svg
        └── level-3/
            ├── backend/
            │   ├── puml/US-001-level-3-backend.puml
            │   └── svg/US-001-level-3-backend.svg
            └── frontend/
                ├── puml/US-001-level-3-frontend.puml
                └── svg/US-001-level-3-frontend.svg
```

## Stories

| Story | SSD (level 1) | SD (level 2) | SD (level 3) | HLD | LLD |
| --- | --- | --- | --- | --- | --- |
| [US-001](US-001/README.md) | [level-1](US-001/ssd/level-1/puml/US-001-level-1.puml) | [level-2](US-001/sd/level-2/puml/US-001-level-2.puml) | [backend](US-001/sd/level-3/backend/puml/US-001-level-3-backend.puml), [frontend](US-001/sd/level-3/frontend/puml/US-001-level-3-frontend.puml) | [HLD](../requirements/US-001-HLD.md) | [LLD](../requirements/US-001-LLD.md) |
| [US-002](US-002/README.md) | [level-1](US-002/ssd/level-1/puml/US-002-level-1.puml) | [level-2](US-002/sd/level-2/puml/US-002-level-2.puml) | [backend](US-002/sd/level-3/backend/puml/US-002-level-3-backend.puml), [frontend](US-002/sd/level-3/frontend/puml/US-002-level-3-frontend.puml) | [HLD](../requirements/US-002-HLD.md) | [LLD](../requirements/US-002-LLD.md) |
| [US-003](US-003/README.md) | [level-1](US-003/ssd/level-1/puml/US-003-level-1.puml) | [level-2](US-003/sd/level-2/puml/US-003-level-2.puml) | [backend](US-003/sd/level-3/backend/puml/US-003-level-3-backend.puml), [frontend](US-003/sd/level-3/frontend/puml/US-003-level-3-frontend.puml) | [HLD](../requirements/US-003-HLD.md) | [LLD](../requirements/US-003-LLD.md) |
| [US-004](US-004/README.md) | [level-1](US-004/ssd/level-1/puml/US-004-level-1.puml) | [level-2](US-004/sd/level-2/puml/US-004-level-2.puml) | [backend](US-004/sd/level-3/backend/puml/US-004-level-3-backend.puml), [frontend](US-004/sd/level-3/frontend/puml/US-004-level-3-frontend.puml) | [HLD](../requirements/US-004-HLD.md) | [LLD](../requirements/US-004-LLD.md) |
| [US-005](US-005/README.md) | [level-1](US-005/ssd/level-1/puml/US-005-level-1.puml) | [level-2](US-005/sd/level-2/puml/US-005-level-2.puml) | [backend](US-005/sd/level-3/backend/puml/US-005-level-3-backend.puml), [frontend](US-005/sd/level-3/frontend/puml/US-005-level-3-frontend.puml) | [HLD](../requirements/US-005-HLD.md) | [LLD](../requirements/US-005-LLD.md) |
