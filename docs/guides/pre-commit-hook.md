# Pre-commit hook

[Guides](README.md)

**Task:** set up the local pre-commit hook that formats and checks staged code before it's committed.
**Starting conditions:** repository cloned, Bash (included with Git for Windows), Node.js/npm and the .NET SDK installed per [technical reference](../reference/README.md#supported-environment), and dependencies restored (`npm install` in `web/`, `dotnet restore Nexo.slnx` at the repository root).

## What it does

On every `git commit`, the hook selects work from the Git index:

- **Frontend:** any changed path under `web/` triggers `npm run lint` and `npm run build` (TypeScript and Vite). Added/modified files with extensions `ts`, `tsx`, `js`, `jsx`, `mjs`, `cjs`, `json`, `css`, `md`, `html`, `yaml`, or `yml` are formatted with Prettier and re-staged first.
- **Backend:** any changed path under `api/` or `api.Tests/`, any `.sln`, `.slnx`, `.props`, or `.targets` file, and root `global.json`, `NuGet.Config`/`nuget.config`, or `.editorconfig` trigger `dotnet build Nexo.slnx -warnaserror`. Added/modified C# files under the two backend directories are formatted with `dotnet format --include` and re-staged first.
- **Deletions and renames:** removed paths still trigger their side's checks but are never sent to a formatter. Renames are evaluated as a removal and addition, so moving a file between sides checks both.

Before formatting, the hook checks all selected files for unstaged changes. If any exist, it stops without changing files or the index. Stage the remaining changes if they belong in this commit, or stash them before retrying.

Only regular staged files may be formatted; symlinks are rejected. Unmerged entries also stop the hook. Filenames are read with NUL delimiters and passed as literal paths to Git.

Formatting is applied and re-staged only for the selected paths. Paths containing spaces or quotes remain intact. Formatter failures, lint errors, and build errors or warnings abort the commit. Formatting already applied before a later check fails remains in the working tree/index; review `git diff` and `git diff --cached` before retrying.

The hook prints a status message even when there is nothing to check, such as a commit that only touches `docs/`. Root backend configuration files do trigger checks. No hook output may mean it is not installed.

## One-time setup

The hook script lives in the repository at [`tools/git-hooks/pre-commit`](../../tools/git-hooks/pre-commit) (tracked, unlike the untracked `.git/hooks/` directory). From the repository root, point Git at it once per clone:

```sh
git config core.hooksPath tools/git-hooks
```

If an agent performs this setup, [AGENTS.md](../../AGENTS.md#working-rules) requires explicit confirmation for this configuration change. Editing the hook does not install it.

The repository's `.gitattributes` keeps the hook's line endings as LF so its Bash shebang works after Windows checkouts.

## Alternatives and limitations

- This is local-only: it does not run in CI. If a commit is made without the hook installed (e.g., a fresh clone before running the setup command), nothing catches it until the next `dotnet build`/`npm run lint` you run yourself.
- The hook does not run tests (`dotnet test`, `npm test`), only formatting, linting, and the build. Run those yourself, or add a CI workflow, before pushing.
- Frontend lint/build and the backend build examine the working tree, including unrelated unstaged files. They do not validate an isolated staged snapshot. Only files selected for formatting receive the partial-staging protection.
- `dotnet build -warnaserror` builds the whole solution, not just the staged files' project, so an unrelated pre-existing warning elsewhere in the solution will also block the commit; keep the solution warning-free.
- A passing build is not a passing test suite. Documentation, E2E files, and diagram sources do not trigger application checks by themselves. Validate their links, tests, or rendered output separately.
- Formatters must already be available locally. Prettier runs with `npx --no-install`; install frontend dependencies before committing. The backend format/build can restore NuGet dependencies if needed.

## Troubleshooting

| Message or symptom | Action |
| --- | --- |
| Selected file has unstaged changes | Compare `git diff -- <path>` with `git diff --cached -- <path>`; stage the remaining edits only if they belong in this commit |
| Formatting failed | Review `git diff` and `git diff --cached`; formatting may have changed files before failing |
| Lint or build failed | Run the displayed command in `web/` or the repository root, fix the reported issue, and retry |
| No hook output | Confirm setup was performed for this clone; use the regression command above to test the script independently |

The regression suite does not install the hook, create commits in this repository, or change Git configuration.

See [technical reference](../reference/README.md#commands) for the underlying commands, and [diagrams.md](diagrams.md) for the separate diagram-rendering workflow.
