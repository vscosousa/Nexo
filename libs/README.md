# Diagram tooling

[Documentation index](../docs/README.md)

`plantuml.json` pins the PlantUML release and SHA-256 checksum. The generator downloads the official JAR to `.cache/plantuml/` and verifies it before every use. Binary dependencies are not committed.

To upgrade, update both values from an [official PlantUML release](https://github.com/plantuml/plantuml/releases), run the generator tests, and review regenerated SVGs.

See the [diagram guide](../docs/guides/diagrams.md) for commands and folder conventions.
