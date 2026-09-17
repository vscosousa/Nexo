# Pre-commit hook

[Guides](README.md)

**Task:** set up the local pre-commit hook that formats and checks staged code before it's committed.
**Starting conditions:** repository cloned, Node.js/npm and the .NET SDK installed per [technical reference](../reference/README.md#supported-environment).

## What it does

On every `git commit`, for whichever side you actually staged files in:

- **Frontend** (`web/**/*.{ts,tsx,js,jsx,json,css,md}` staged): runs `prettier --write` on those files, re-stages them, then runs `oxlint` (`npm run lint`). A lint error aborts the commit.
- **Backend** (`api/**/*.cs` or `api.Tests/**/*.cs` staged): runs `dotnet format Nexo.slnx --include <staged files>`, re-stages them, then runs `dotnet build Nexo.slnx -warnaserror`. A build error or warning aborts the commit.

Formatting is always applied and folded into the commit automatically; only a real lint error, build error, or warning stops the commit, so you can fix it before it lands in history.

The hook only touches files you already staged. It re-adds exactly those paths after formatting, never anything else in your working tree, so unrelated in-progress edits are never swept into the commit.

## One-time setup

The hook script lives in the repository at [`tools/git-hooks/pre-commit`](../../tools/git-hooks/pre-commit) (tracked, unlike the untracked `.git/hooks/` directory). Point Git at it once per clone:

```sh
git config core.hooksPath tools/git-hooks
```

## Alternatives and limitations

- This is local-only: it does not run in CI. If a commit is made without the hook installed (e.g., a fresh clone before running the setup command), nothing catches it until the next `dotnet build`/`npm run lint` you run yourself.
- The hook does not run tests (`dotnet test`, `npm test`), only formatting, linting, and the build. Run those yourself, or add a CI workflow, before pushing.
- `dotnet build -warnaserror` builds the whole solution, not just the staged files' project, so an unrelated pre-existing warning elsewhere in the solution will also block the commit; keep the solution warning-free.

See [technical reference](../reference/README.md#commands) for the underlying commands, and [diagrams.md](diagrams.md) for the separate diagram-rendering workflow.
