# Agent guidelines

## Before starting

- Read [README.md](README.md) for project context and [docs/README.md](docs/README.md) for the documentation map.
- Read the topic pages relevant to the task, including their linked requirements and decisions.

## Working rules

- Keep changes focused, follow existing patterns, and preserve unrelated work.
- Write documentation per [docs/README.md](docs/README.md#maintaining-the-documentation); match the user's language in conversation.
- Do not include secrets or invent project details, commands, or test results.
- Follow TDD: write a failing test before implementing a feature or fix. See [docs/testing/README.md](docs/testing/README.md) for which kind of test applies and how to run it.

## Keep documentation current

A change is not done until its matching topic page (per [docs/README.md](docs/README.md#documentation-map)) reflects it. Tests, behavior, models, architecture, decisions, and configuration each have one. Record actual test results only when executed, and significant technical decisions in `docs/decisions/`.

## Before finishing

- Run the relevant available checks (see [docs/reference/README.md](docs/reference/README.md#commands)) and verify changed documentation links.
- Summarize what changed, what was checked, and anything left unverified.
