---
status: implementing
review: done
environment: null
depth: quick
supersedes: null
signed-off: 2026-11-09
signed-off-hash: 155d6386e2d0f830b795263caf03007f2a6059b013bc13e82d2c0cfc6dcc2a2a
---

# M80: Deploy-folder warning, hooks before the session with a gate probe, build identity

## Goal

Fix findings F42, F43, F1 and F2 of the PowerOps 1.0 trial (2026-11-08), as section 3 ("M80") of the agreed 1.0 fix plan sets out (Fable and Astra, six rounds; amendments A10, F-6):

| Id | Finding | This plan |
|---|---|---|
| F42 | A plan file saved one folder up from the zips broke `deploy apply`, which reads the zips and settings files from the plan file's own folder | `deploy plan --save` to a file whose folder is not the deploy folder prints one `warning:` line on stderr and still writes it (rows 1 and 2) |
| F43 | Hooks added during the deployment session never ran, and nothing showed it; the gate compares hashes, not folders, so with an inactive hook a real `pac` import would have run | `docs/examples/gate-probe/`: a `pac` stand-in that never calls a host, writes a marker file and exits 1, and a README with the probe, which calls the stand-in by its absolute path so the real `pac` can't run. HANDOFF: hooks go in before the deployment session starts, and the probe runs before the first import (rows 3 to 6) |
| F1, F2 | Updating the CLI and the plugin kept an old `0.0.0` build silently | INSTALL's Update and USE-WITH-AGENTS' plugin update say how to check the build: the commit in `powerops --version` is the one built, and the plugin cache's `gitCommitSha` names the same one; the version string alone proves nothing (rows 7 and 8) |

Done when:

1. `powerops deploy plan <folder> --target <t> --save <file>` prints `warning: the plan file is not saved in the deploy folder; deploy apply reads the zips and settings files from the plan file's own folder` on stderr when the full path of `<file>`'s folder is not the full path of `<folder>` (one folder up, a subfolder, or elsewhere), and still writes the file; stdout, the plan file, its hash and the exit code are the same as without the line. A `--save` directly in the folder prints no such line. The line names no path.
2. `docs/examples/gate-probe/pac` (bash, git mode 100755, LF endings) appends one line to `gate-probe.marker` beside itself, prints one `gate probe:` line on stderr and exits 1, whatever its arguments; it runs no other program that could reach a host. A test runs it by its absolute path and checks the marker, the stderr line, no stdout and exit 1. A gate test feeds `deploy gate` the README's probe command and checks exit 2, the exact block line, and no `pac` or target call.
3. `docs/examples/gate-probe/README.md` gives the probe step by step; `docs/HANDOFF.md` step 7 says to save the plan file in the deploy folder, and its hooks bullet says to install the hooks before the deployment session starts and to run the probe before the first import, with a link the tests check; `docs/INSTALL.md` (Update), `docs/USE-WITH-AGENTS.md` (the plugin update line) and `docs/COMMANDS.md` (`deploy plan`) say what changed.
4. `dotnet build` (0 warnings), `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m80-deploy-folder-probe` and the leak scan are clean.

### Won't build

- Refusing `--save` outside the folder (rejected in the fix plan): a plan file may be written elsewhere on purpose, for a review copy.
- A version or commit check inside `deploy apply` (rejected in the fix plan): `deploy apply` already refuses a plan made by another `powerops --version`.
- A gate rule on folders: the gate's hash check already blocks a call whose zip isn't the plan's; the probe shows whether the gate runs at all.
- Putting the stand-in first on `PATH`, as the fix plan wrote it. The gate reads the program by its file name, so calling the stand-in by its absolute path goes through the same `Bash` hook and the same gate reading, and no `PATH` order, shell profile or MCP server started under the stand-in can bring the real `pac` in (Fable's plan review, finding 2).
- A Windows (`pac.cmd`) or PowerShell stand-in, a Codex variant, or a stand-in that passes other calls through to the real `pac` (that one could reach a host). The probe covers Claude Code's `Bash` tool; the README says what it doesn't cover.
- Any change to the kept roles, their plugin copies, `tests/eval`, the version files or the changelog.

## Users

- A maker or agent that runs `deploy plan --save` by hand (the MCP tool `deploy_plan` and the Action always save inside the folder).
- The person who starts the deployment session and records the probe, and the trial's graders, who read its output first.
- A person updating the CLI or the plugin from a clone or from `main`.

## Data

Line numbers are those of `278b136`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | In `Run`, after `Prepare` succeeds and before the report is written: when `save` is set and `Path.TrimEndingDirectorySeparator(Path.GetDirectoryName(Path.GetFullPath(save)))` differs from `Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder))` (ordinal, case ignored on Windows, as `:163-164` compares; the same folder `deploy apply` takes at `:357`), write the fixed line `warning: the plan file is not saved in the deploy folder; deploy apply reads the zips and settings files from the plan file's own folder` to `error`. It names no path, so the rule at `:140` holds. Two spellings through a linked folder are compared as text, with a `shortcut:` comment as at `:162` | `src/PowerOps.Cli/DeployCommand.cs:136-206` | `Cli.Tests/DeployCommandTests.cs`: `--save Path.Combine(Folder, "..", "p.json")` warns, writes the file, and stdout, exit code and the file's bytes equal a run that saves in the folder; `--save Path.Combine(Folder, "sub", "p.json")` (folder made first) warns; `--save Path.Combine(Folder, "p.json")`, `Path.Combine(Folder, ".", "p.json")` and a folder argument with a trailing separator don't warn; no `--save`, no line. `The_target_value_and_the_folder_path_appear_in_no_output_and_no_file` (`:481`, which saves into the sibling `Out` folder) stays unchanged and green |
| 2 | `COMMANDS.md` `deploy plan`: one sentence after the usage paragraph (`:106`): the warning text, that the file is still written and nothing else changes, and why | `docs/COMMANDS.md:106` | `DocsDriftTests` as it stands |
| 3 | The stand-in: `docs/examples/gate-probe/pac`, `#!/usr/bin/env bash`, comment lines saying what it is and that it never calls a host; appends `<UTC time> pac <arguments>` to `gate-probe.marker` in its own folder (`$(dirname "$0")`), prints `gate probe: the pac stand-in ran, so no hook blocked this call; the gate is not active in this session (marker: <path>)` on stderr, exits 1, also when the marker can't be written; runs nothing but `date`, `dirname` and `printf`. Git mode 100755. `.gitattributes`: `docs/examples/gate-probe/pac text eol=lf` | `docs/examples/gate-probe/pac`, `.gitattributes` | `Cli.Tests/GateProbeTests.cs` (bash; skipped on Windows like the other script tests, through `Tools.Require("bash")`): copy the file into a temp folder with `/bin/cp`, as `StandInTests.cs:33-36` does and for its reason (ETXTBSY); run `<temp>/pac solution import --path /nonexistent/powerops-gate-probe.zip` by that absolute path; exit 1, no stdout, one `gate probe:` line, the marker holds one line ending in `pac solution import --path /nonexistent/powerops-gate-probe.zip`; a second run appends a second line; on Unix the repo file has the user-execute bit. `.gitattributes` names the file |
| 4 | The gate half of the probe: `ImportCall` reads the program by its file name, so the hook input `{"tool_name":"Bash","tool_input":{"command":"<absolute folder>/pac solution import --path <absolute path that does not exist>"}}` is read as a `pac solution import` and blocked as unreadable, because it has no `--environment` (`ImportCall.cs:458`), before the plan file, the zip or the target is read | `src/PowerOps.Core/Deploy/ImportCall.cs` (no change expected) | `Cli.Tests/DeployGateCommandTests.cs`: one case with the README's command form and an absolute stand-in path: exit 2, the exact line `error: deploy gate: --environment is missing`, and the fake `pac` and the target read get no call |
| 5 | `docs/examples/gate-probe/README.md`: what it proves and what it doesn't; the steps: (a) copy `pac` into a folder outside the repository, by its absolute path, and `chmod +x` it; (b) put the hooks of HANDOFF's [Gate Microsoft's imports](../../HANDOFF.md#gate-microsofts-imports) into `.claude/settings.json` before the deployment session starts; (c) start the deployment session; (d) delete `gate-probe.marker` in that folder if it is there; (e) first command, through the agent's `Bash` tool: `<absolute folder>/pac solution import --path <an absolute path that does not exist>`, exactly so: don't add `--environment` or reword it, and don't retry it after a block; (f) read the result from the call's own output: `error: deploy gate:` lines mean the hook is active (the pass); the `gate probe:` line means it isn't, and the session must not import: restart it with the hooks in place and probe again from (d); (g) record the output, and that `gate-probe.marker` is absent, as the second record. The stand-in is never on `PATH` and the real `pac` is never named, so no outcome can reach a tenant. It doesn't cover the MCP matcher, `Monitor`, `PowerShell`, Codex, or Windows (untested) | `docs/examples/gate-probe/README.md` | `GateProbeTests`: the README holds the step (e) command form used by the gate test, names `gate-probe.marker`, and its HANDOFF link names a heading that exists in `docs/HANDOFF.md` |
| 6 | `HANDOFF.md`: step 7 (`:50`) gains `Save the plan file in the deploy folder: deploy apply reads the zips and settings files from the plan file's own folder, and deploy plan warns when --save names another folder.`; the hooks bullet (`:89`) `Turn the hooks on when you start step 7 …` becomes: put the hooks into `.claude/settings.json` before you start the session that runs steps 7 and 8; the file is shared by every session in the repository, so build in Dev before the hooks go in or after they come out, never while that session runs, and run the probe again before the next import if the hooks were edited during it; in the first trial, hooks added during the session never ran, although Claude Code's settings page says hook edits are normally picked up while a session runs; so before the first import, run the [gate probe](examples/gate-probe/README.md), which shows whether the hook is active without calling a tenant; the rest of the bullet (Dev imports blocked, several solutions through `deploy apply`) stays. The `Sources:` line (`:110`) gains a link to the Claude Code settings page, titled `Claude Code settings`, with the date it was read | `docs/HANDOFF.md:50, 89, 110` | `HandoffDocTests`: the hooks section links `](examples/gate-probe/README.md)` and that file exists; step 7 holds the save-in-the-deploy-folder sentence. The page keeps steps 1 to 11 and its one JSON block |
| 7 | `INSTALL.md` Update, after the code block (`:38`): after an update, compare the commit that `powerops --version` prints (see `:28`) with `git rev-parse HEAD` in the clone; the version alone proves nothing, because every build between two releases has the same one | `docs/INSTALL.md:30-38` | none (docs) |
| 8 | `USE-WITH-AGENTS.md` (`:29`, the fix plan's `:28`): after "…restart Claude Code.", one sentence: then check that `gitCommitSha` of `powerops@powerops` in `~/.claude/plugins/installed_plugins.json` (a field Claude Code writes but doesn't document) is the commit you meant, and, for a CLI built from a clone, that `powerops --version` names the same commit. Only that line changes (M82 edits the same file) | `docs/USE-WITH-AGENTS.md:29` | none (docs) |

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
- The stand-in writes only its marker beside itself and runs only `date`, `dirname` and `printf`.
- The kept roles, their plugin copies and `tests/eval` stay untouched.
- Platforms are Windows and Linux; the script test skips on Windows as the other script tests do.

## Assumptions

- Claude Code and hooks (checked 2026-11-09 against the Claude Code settings page, "When edits take effect", and the hooks page): hook edits in settings files are normally picked up by a file watcher while a session runs, also in a project `.claude/` folder made during the session. That contradicts the fix plan's claim that hooks load only at session start. The trial observed hooks added during the session that never ran (F43), and the cause is not known. Installing the hooks before the session and probing first is right either way, and the probe shows the state, so HANDOFF states the observation and the docs' claim, not a cause.
- The hook input: a `PreToolUse` hook gets `tool_name` and `tool_input.command` on stdin, and exit 2 blocks the call with stderr given to Claude (the hooks page, checked 2026-11-09). The gate already relies on this.
- The gate reads a path-qualified `pac` as `pac`: Fable's plan review ran the built gate on `/tmp/probe/pac solution import --path /nonexistent/powerops-gate-probe.zip` and on a quoted path with a space, both exit 2 with `error: deploy gate: --environment is missing` (2026-11-09). Row 4's test pins it.
- `gitCommitSha` in `~/.claude/plugins/installed_plugins.json`: the file is documented (the plugins loading page), the field is not; observed on this machine on 2026-11-09 holding the full 40-character commit, the same form `powerops --version` prints after `+`.
- On Windows the stand-in would need Git Bash, which Claude Code's `Bash` tool uses there; not tested.

## Open questions

## Questions for your admin

## Steps

1. This plan and `decisions.md`, the two plan reviews, fixes, and the sign-off.
2. Rows 1 and 2 (`deploy plan` warning and its COMMANDS sentence), tests first.
3. Rows 3, 4 and 5 (the stand-in, its tests, the gate case and the README), tests first. Steps 2 and 3 share no file and may run in parallel.
4. Rows 6, 7 and 8 (HANDOFF and its tests, INSTALL, USE-WITH-AGENTS), after step 3, whose README the HANDOFF link names.
5. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m80-deploy-folder-probe` and the leak scan; commit.
