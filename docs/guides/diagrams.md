# Diagrams

[Guides](README.md)

**Task:** add or update a PlantUML diagram and regenerate its SVG.
**Starting conditions:** PowerShell 7+, a Java runtime on `PATH`, and Graphviz installed (required for some diagram types).

1. Add or edit a `.puml` file in a `puml/` folder next to where the diagram is used (e.g. `docs/guides/puml/my-diagram.puml`), with exactly one `@startuml` / `@enduml` block.
2. Run the generator from the repo root:
   ```
   ./tools/generate/generate-plantuml-diagrams.ps1
   ```
3. Check the matching `svg/` folder for the rendered output (e.g. `docs/guides/svg/my-diagram.svg`).
4. Commit both the `.puml` source and the generated `.svg`.

**Alternatives and limitations:** Do not hand-edit generated SVGs; the generator overwrites them and removes ones whose source was deleted. Hand-drawn SVGs (without the generator's marker comment) are left alone. See [libs/README.md](../../libs/README.md) for how the PlantUML JAR is pinned and verified.

## Troubleshooting

| Symptom | Possible cause | Diagnostic check | Solution |
| --- | --- | --- | --- |
| Generator throws a checksum error | `libs/plantuml.json` was edited or the cached JAR is corrupt | Compare `libs/plantuml.json` to the official release checksum | Fix the pin or delete `.cache/plantuml/` and rerun |
| "Place diagram sources in a puml/ folder" | Source file is not inside a `puml/` directory | Check the file's parent folder name | Move the `.puml` file into a `puml/` folder |
| Rendering fails on `java` not found | Java runtime missing | `java -version` | Install a JDK/JRE and ensure it is on `PATH` |
| CI's diagram job has no changes to commit | Generated SVGs already match sources | Compare local run output to committed SVGs | Nothing to do; the workflow only commits when output differs |

The CI workflow ([diagrams.yml](../../.github/workflows/diagrams.yml)) runs the same generator on pushes and pull requests, and commits regenerated SVGs on the default branch.
