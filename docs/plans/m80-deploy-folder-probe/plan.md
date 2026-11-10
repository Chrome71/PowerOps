---
status: approved
review: pending
environment: null
depth: quick
supersedes: null
signed-off: null
signed-off-hash: null
---

# M80: Deploy-folder warning, hooks before the session with a gate probe, build identity

## Goal

Fix findings F42, F43, F1 and F2 of the PowerOps 1.0 trial (2026-11-08), as section 3 ("M80") of the agreed 1.0 fix plan sets out (Fable and Astra, six rounds; amendments A10, F-6):

| Id | Finding | This plan |
|---|---|---|
| F42 | A plan file saved one folder up from the zips broke `deploy apply`, which reads the zips and settings files from the plan file's own folder | `deploy plan --save` to a file whose folder is not `<folder>` prints one `warning:` line on stderr and still writes it (rows 1 and 2) |
| F43 | Hooks added during the deployment session never ran, and nothing showed it; the gate compares hashes, not folders, so with an inactive hook a real `pac` import would have run | `docs/examples/gate-probe/`: a `pac` stand-in that never calls a host, writes a marker file and exits 1, and a README with the probe. HANDOFF: hooks go in before the deployment session starts, and the probe runs before the first import (rows 3 to 6) |
| F1, F2 | Updating the CLI and the plugin kept an old `0.0.0` build silently | INSTALL's Update and USE-WITH-AGENTS' plugin update say how to check the build: `powerops --version` names the commit built, and the plugin cache's `gitCommitSha` names the same one; the version string alone proves nothing (rows 7 and 8) |

Done when:

1. `powerops deploy plan <folder> --target <t> --save <file>` prints `warning: the plan file is not saved in <folder>; deploy apply reads the zips and settings files from the plan file's own folder` on stderr when the full path of `<file>`'s folder is not the full path of `<folder>` (one folder up, a subfolder, or elsewhere), and still writes the file; the report, the plan file, the hash and the exit code don't change. A `--save` directly in the folder prints no such line.
2. `docs/examples/gate-probe/pac` (bash, mode 755, LF endings) appends one line to `gate-probe.marker` beside itself, prints one `gate probe:` line on stderr and exits 1, whatever its arguments; it runs no other program that could reach a host. A test runs it through a `PATH` lookup and checks the marker, the stderr line, no stdout and exit 1. A gate test feeds `deploy gate` the probe's hook input and checks exit 2.
3. `docs/examples/gate-probe/README.md` gives the probe step by step; `docs/HANDOFF.md` step 7 says to save the plan file in the deploy folder, and its hooks bullet says to install the hooks before the deployment session starts and to run the probe before the first import; `docs/INSTALL.md` (Update), `docs/USE-WITH-AGENTS.md` (the plugin update line) and `docs/COMMANDS.md` (`deploy plan`) say what changed.
4. `dotnet build` (0 warnings), `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m80-deploy-folder-probe` and the leak scan are clean.

### Won't build

- Refusing `--save` outside the folder (rejected in the fix plan): a plan file may be written elsewhere on purpose, for a review copy.
- A version or commit check inside `deploy apply` (rejected in the fix plan): `deploy apply` already refuses a plan made by another `powerops --version`.
- A gate rule on folders: the gate's hash check already blocks a call whose zip isn't the plan's; the probe shows whether the gate runs at all.
- A Windows (`pac.cmd`) or PowerShell stand-in, a Codex variant, or a stand-in that passes other `pac` calls through to the real `pac` (that one could reach a host). The probe covers Claude Code's `Bash` tool; the README says what it doesn't cover.
- A `SessionStart` hook that sets `PATH` through `CLAUDE_ENV_FILE`: starting the session with `PATH` set does the same with nothing new to trust, and the probe checks which `pac` the shell finds before it imports.
- Any change to the kept roles, their plugin copies, `tests/eval`, the version files or the changelog.

## Users

- A maker or agent that runs `deploy plan --save` by hand (the MCP tool `deploy_plan` and the Action always save inside the folder).
- The person who starts the deployment session and records the probe, and the trial's graders, who read its output first.
- A person updating the CLI or the plugin from a clone or from `main`.

## Data

Line numbers are those of `278b136`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | In `Run`, after `Prepare` succeeds and before the report is written: when `save` is set and `Path.TrimEndingDirectorySeparator(Path.GetDirectoryName(Path.GetFullPath(save)))` differs from `Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder))` (ordinal, case ignored on Windows, as `:163-164` compares), write `warning: the plan file is not saved in <folder>; deploy apply reads the zips and settings files from the plan file's own folder` to `error`, `<folder>` as given on the command line, through `Clean`. The full path is never printed (`:140`). Two spellings through a linked folder are compared as text, with a `shortcut:` comment as at `:162` | `src/PowerOps.Cli/DeployCommand.cs:136-206` | `Cli.Tests/DeployCommandTests.cs`: `--save ../p.json` (one folder up) warns, writes the file, keeps exit and stdout; `--save <folder>/sub/p.json` warns; `--save <folder>/p.json` and `--save <folder>/./p.json` and a folder argument with a trailing separator don't warn; no `--save`, no line |
| 2 | `COMMANDS.md` `deploy plan`: one sentence after the usage paragraph (`:106`): the warning text, that the file is still written and nothing else changes, and why | `docs/COMMANDS.md:106` | `DocsDriftTests` as it stands |
| 3 | The stand-in: `docs/examples/gate-probe/pac`, `#!/usr/bin/env bash`, comment lines saying what it is and that it never calls a host; appends `<UTC time> pac <arguments>` to `gate-probe.marker` in its own folder (`$(dirname "$0")`), prints `gate probe: the pac stand-in ran, so no hook blocked this call; the gate is not active in this session (marker: <path>)` on stderr, exits 1, also when the marker can't be written. Git mode 100755. `.gitattributes`: `docs/examples/gate-probe/pac text eol=lf` | `docs/examples/gate-probe/pac`, `.gitattributes` | `Cli.Tests/GateProbeTests.cs` (bash; skipped on Windows like the other script tests, through `Tools.Require("bash")`): copy the file into a temp folder, run `bash -c 'pac solution import --path /nonexistent/powerops-gate-probe.zip'` with that folder first on `PATH`; exit 1, no stdout, one `gate probe:` line, the marker holds one line ending in `pac solution import --path /nonexistent/powerops-gate-probe.zip`; a second run appends a second line; on Unix the repo file has the user-execute bit. A test that `.gitattributes` names the file |
| 4 | The gate half of the probe: the hook input `{"tool_name":"Bash","tool_input":{"command":"pac solution import --path <absolute path that does not exist>"}}` against a plan file that doesn't exist gives exit 2 and a reason on stderr | `DeployCommand.cs` (no change expected) | `Cli.Tests/DeployGateCommandTests.cs`: one case, through the existing helpers |
| 5 | `docs/examples/gate-probe/README.md`: what it proves and doesn't; the steps: (a) copy `pac` into a folder outside the repository and `chmod +x` it; (b) put the hooks of HANDOFF's [Gate Microsoft's imports](../../HANDOFF.md#gate-microsofts-imports) into `.claude/settings.json` before the deployment session starts; (c) start that session with the folder first on `PATH` (`PATH="<folder>:$PATH" claude`); (d) first command through the agent's `Bash` tool: `command -v pac`, which must print the stand-in's path; if it prints anything else, stop and don't run step (e), because the real `pac` would run; (e) `pac solution import --path <an absolute path that does not exist>`; (f) read the result: blocked with `error: deploy gate:` lines and no `gate-probe.marker` means the hook is active; the `gate probe:` line and the marker mean it isn't, and the deployment session must not import (restart it with the hooks in place and probe again); (g) record both outputs; (h) delete the stand-in (`rm <folder>/pac`) and check `command -v pac` names the real `pac` before `deploy plan`, which calls `pac`. What it doesn't cover: the MCP matcher, `Monitor`, `PowerShell`, Codex, Windows (untested). The page links the fix plan's re-trial use only through HANDOFF | `docs/examples/gate-probe/README.md` | `DocsDriftTests` (links); `GateProbeTests` checks the README names the file `gate-probe.marker` and the commands of steps (d) and (e) |
| 6 | `HANDOFF.md`: step 7 (`:50`) gains `Save the plan file in the deploy folder: deploy apply reads the zips and settings files from the plan file's own folder, and deploy plan warns when --save names another folder.`; the hooks bullet (`:89`) `Turn the hooks on when you start step 7 …` becomes: put the hooks into `.claude/settings.json` before you start the session that runs steps 7 and 8, and build in Dev in another session without them; in the first trial, hooks added during the session never ran, although Claude Code's settings page says hook edits are normally picked up while a session runs; so before the first import, run the [gate probe](examples/gate-probe/README.md), which shows whether the hook is active without calling a tenant; the rest of the bullet (Dev imports blocked, several solutions through `deploy apply`) stays | `docs/HANDOFF.md:50, 89` | `HandoffDocTests` as it stands (it reads the page's one JSON block and its `powerops` commands; nothing new is added to either) |
| 7 | `INSTALL.md` Update, after the code block (`:38`): check the build after an update: `powerops --version` prints `<version>+<commit>` for a build from a clone, and the commit must be the one you built (`git rev-parse HEAD` in the clone); the version alone proves nothing, because every build between two releases has the same one | `docs/INSTALL.md:30-38` | none (docs) |
| 8 | `USE-WITH-AGENTS.md` (`:29`, the fix plan's `:28`): after "…restart Claude Code.", one sentence: then check that the plugin cache names the commit you meant: `gitCommitSha` of `powerops@powerops` in `~/.claude/plugins/installed_plugins.json` (a field Claude Code writes but doesn't document), and that `powerops --version` names the same commit. Only that line changes (M82 edits the same file) | `docs/USE-WITH-AGENTS.md:29` | none (docs) |

## Automation

None.

## Screens

None.

## Environments and route

None. No tenant read or write runs in this milestone; the stand-in calls no host, and no real `pac solution import` runs.

## Solution and publisher

None. The tests use the existing seed fixture and `DeployFakes` targets.

## Constraints

- AGENTS.md: the smallest change, new logic with tests, writes only through `SafeWrite` (none new in `src/`), Conventional Commits, no version or changelog edits, no tenant values.
- The warning changes no output a consumer parses: stdout, the plan file, its hash and the exit code stay as they are, so no golden or schema moves.
- The stand-in writes only its marker beside itself and runs only `date`, `dirname` and `printf` (shell built-ins or coreutils).
- The kept roles, their plugin copies and `tests/eval` stay untouched.
- Platforms are Windows and Linux; the script test skips on Windows as the other script tests do.

## Assumptions

- Claude Code and hooks (checked 2026-11-09 by a `claude-code-guide` subagent against code.claude.com): the settings page ("When edits take effect") and the hooks page say hook edits in settings files are normally picked up by a file watcher while a session runs, and a file created mid-session is loaded if its folder existed at startup. That contradicts the fix plan's claim that hooks load only at session start. The trial observed hooks added mid-session that never ran (F43); why is not known (one possible cause: `.claude/` did not exist when the session started). Installing the hooks before the session and probing first is right either way, and the probe shows the state, so HANDOFF states the observation and the docs' claim, not a cause.
- A `PATH`-first stand-in reaching the `Bash` tool's shell: not documented. The tools reference says each command runs in a shell set up from the user's profile (`~/.zshrc`, `~/.bashrc` or `~/.profile`), which may put another folder before the stand-in's; so the probe checks `command -v pac` first and stops if it names anything else. Unconfirmed until a trial runs it.
- The hook input: a `PreToolUse` hook gets `tool_name` and `tool_input.command` on stdin, and exit 2 blocks the call with stderr given to Claude (hooks page, checked 2026-11-09). The gate already relies on this.
- `gitCommitSha` in `~/.claude/plugins/installed_plugins.json`: the file is documented (plugins loading page), the field is not; it was observed on this machine on 2026-11-09 holding the full 40-character commit, the same form `powerops --version` prints after `+`.
- `powerops --version` prints `<version>+<commit>` for a build from a clone (`AnalyzeCommand.ToolVersion`, the informational version; observed `0.0.0+<40 hex>` on 2026-11-09).
- On Windows the stand-in would need Git Bash, which Claude Code's `Bash` tool uses there; not tested.

## Open questions

## Questions for your admin

## Steps

1. This plan and `decisions.md`, the two plan reviews, fixes, and the sign-off.
2. Rows 1 and 2 (`deploy plan` warning and its COMMANDS sentence), tests first.
3. Rows 3, 4 and 5 (the stand-in, its tests, the gate case and the README), tests first.
4. Rows 6, 7 and 8 (HANDOFF, INSTALL, USE-WITH-AGENTS).
5. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m80-deploy-folder-probe` and the leak scan; commit.
