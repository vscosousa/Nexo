# User story design

[Documentation index](../README.md)

Design artifacts grouped by user story: system sequence diagrams (SSD) and sequence diagrams (SD), at three levels of detail matching the [LAPR5](https://github.com/vscosousa/LAPR5_3DC_G15) template:

- **Level 1** (in `ssd/`): the SSD, actor versus Nexo as a single black-box participant.
- **Level 2** (in `sd/`): a coarse SD, actor, UI, and the entry-point participant only.
- **Level 3** (in `sd/`): the detailed SD, every participant in the collaboration (controller, service, repository, mapper, domain object, persistence).

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
            ├── puml/US-001-level-3.puml
            └── svg/US-001-level-3.svg
```

## Stories

| Story | SSD (level 1) | SD (levels 2-3) | HLD | LLD |
| --- | --- | --- | --- | --- |
| [US-001](US-001/README.md) | [level-1](US-001/ssd/level-1/puml/US-001-level-1.puml) | [level-2](US-001/sd/level-2/puml/US-001-level-2.puml), [level-3](US-001/sd/level-3/puml/US-001-level-3.puml) | [HLD](../requirements/US-001-HLD.md) | [LLD](../requirements/US-001-LLD.md) |
| [US-002](US-002/README.md) | [level-1](US-002/ssd/level-1/puml/US-002-level-1.puml) | [level-2](US-002/sd/level-2/puml/US-002-level-2.puml), [level-3](US-002/sd/level-3/puml/US-002-level-3.puml) | [HLD](../requirements/US-002-HLD.md) | [LLD](../requirements/US-002-LLD.md) |
