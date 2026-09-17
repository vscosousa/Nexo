# Agent guidelines

## Before starting

- Read [README.md](README.md) for project context and [docs/README.md](docs/README.md) for the documentation map.
- Read the topic pages relevant to the task, including their linked requirements and decisions.

## Working rules

- Keep changes focused, follow existing patterns, and preserve unrelated work.
- Write documentation per [docs/README.md](docs/README.md#maintaining-the-documentation); match the user's language in conversation.
- Do not include secrets or invent project details, commands, or test results.
- Follow TDD: write a failing test before implementing a feature or fix. See [docs/testing/README.md](docs/testing/README.md) for which kind of test applies and how to run it.
- When creating or editing a user story's SSD/SD diagrams, follow [docs/guides/sequence-diagrams.md](docs/guides/sequence-diagrams.md) for the level/activation-bar/naming convention, and [docs/guides/diagrams.md](docs/guides/diagrams.md) for rendering.
- Ask for feedback before deciding anything non-trivial (scope, design/schema choices, wording of decisions) and before committing. Do the work, then pause for review; do not commit on your own judgment even if the user asked for the underlying work.
- When the user points to their own prior work or an external template as the model to follow (e.g., "like I did in project X"), fetch and read it before building the parallel structure. Don't infer the shape from the term alone — a term like "database design" can map to a conceptual domain model in one project's convention and a physical schema in another.
- Never run `git config` (including `core.hooksPath`, aliases, user identity) without explicit confirmation for that specific change, even when it's local-only and reversible.
- TDD applies to application code. Infra/tooling scripts (Dockerfiles, Compose, Git hooks) don't need their own regression test suite unless asked; adding one is scope creep the user may then have to ask you to remove.
- When adding or removing dev tooling (Docker, hooks, scripts), grep the repo for every doc that references it (README, `docs/reference/`, `docs/testing/`, `docs/guides/`) — these tend to be mentioned in multiple places and are easy to leave stale.
- Code comments: frontend (`web/`) uses TSDoc (`/** ... */` with `@param`/`@returns`/`@throws`) on exported functions/hooks whose behavior isn't obvious from the signature alone — see [github.com/vscosousa/little-lemon-react-native](https://github.com/vscosousa/little-lemon-react-native) for the reference style. Backend (`api/`) uses standard .NET `///` XML doc comments on public API surface (controllers, service/repository interfaces). Skip docs on trivial pass-through code, and never leave a comment that only restates what the code already says.

## Keep documentation current

A change is not done until its matching topic page (per [docs/README.md](docs/README.md#documentation-map)) reflects it. Tests, behavior, models, architecture, decisions, and configuration each have one. Record actual test results only when executed, and significant technical decisions in `docs/decisions/`.

Renaming or merging a domain concept, or renaming a heading, is not done until every reference to it is updated: `#anchor` links to that heading (anchors change when heading text changes), acceptance-criteria wording that describes the old shape (e.g., "no account yet" after a concept merge that means an account now exists in a pending state), and any diagram or table using the old name. Grep the repo for the old name/anchor before considering the rename finished.

## Before finishing

- Run the relevant available checks (see [docs/reference/README.md](docs/reference/README.md#commands)) and verify changed documentation links.
- After renaming or restructuring diagrams, headings, or folders, regenerate diagrams (per [docs/guides/diagrams.md](docs/guides/diagrams.md)) and grep the repo for the old name/path before calling the change done.
- Summarize what changed, what was checked, and anything left unverified.
