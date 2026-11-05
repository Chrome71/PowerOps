---
status: signed-off
review: done
environment: null
depth: quick
supersedes: null
signed-off: 2026-11-04
signed-off-hash: 7ef5a6b0a18c2131f0103a607ca45784dd28b375c67add650d2759dacce52098
---

# M57: A recovery runbook and a harness contract

## Goal

Say in one place what a person does after `deploy apply` stops part way, and say in one place what an agent client must provide for PowerOps's safety steps to work. This is M57 of the improvement plan written after the live test of 2026-11-04, part of its P0 block. That plan, its two reviews and the person's answers to its decisions D1 to D9 are kept outside the repo, because they quote a private test run; its finding ids (C-6, A-11, R15) stay in this folder and out of the two pages. The reviews found no written recovery after a partial import (C-6: `docs/examples/README.md:13` and `:121-123` say only "there is no rollback ... check the environment's import history, then plan again"), that the form route of the run was never reached because an interactive session waited on a start-up prompt (A-11), and that the per-client degraded path the run fell back to was written nowhere (R15). M57 has no decision of its own among D1 to D9; it depends on no other milestone.

Done when:

1. `docs/RECOVERY.md` exists and says what to do after each outcome of `deploy apply`: a table of its exit codes (`0` to `3`, each with the report `status` values it can carry) and a table of the row statuses (`ok`, `failed`, `unknown`, `not-run`, `planned`, `skipped`), each with its next step. It has sections on an import that timed out (`unknown`), on partial success, on reconciling the report with the environment's solution history, on forward repair by a new plan, and on what no import brings back: the components an upgrade deletes, with the data Learn says a deleted table or column loses, which only an environment restore, an admin's decision outside PowerOps, recovers. Every claim about Power Platform cites a Microsoft Learn page that supports it, with the date it was read, and an inference from two pages says so.
2. `docs/HARNESS.md` exists and names the five primitives a client must provide (questions, forms, blocking hooks, environment passthrough, a long-call timeout), what PowerOps uses each for, and the degraded path without it. A per-client table covers Claude Code (interactive and `claude -p`) and Codex (TUI and `codex exec`); each cell names its evidence (a run of the verification record, the client's docs, the client's code read but not run, or PLANNING-SKILLS's Agent support table) or says "not checked". The four provisional clients, whose only known primitive is the question tool, share one sentence. `claude -p` cancels forms (M19) and `codex exec` cancels them in Codex's code (read, not run), so their import goes by the Action route or the person's own command.
3. `RECOVERY.md` is linked from step 8 of `docs/HANDOFF.md`, from `docs/examples/README.md` (`:13` and the "no rollback" section, `:121-123`), from `HARNESS.md` (the timeout path) and from the README's Documentation list; `HARNESS.md` from `docs/USE-WITH-AGENTS.md` (the paragraph that says `claude -p` cancels forms, `:79`) and from the README.
4. `DocsDriftTests` fails when one of the links of Done-when 3 is missing (for HANDOFF, when it is not on the step 8 line), when `RECOVERY.md` lacks a row for an exit code that `docs/COMMANDS.md` lists for `deploy apply`, when it lacks a row for an `import` value of `docs/schemas/deploy-apply.schema.json`, or when a `status` value of that schema is in no exit code's row. Each check takes text and has a planted-wrong-text case. The checks prove that the links and rows exist, not what the rows say.
5. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan (0 hits) are clean; the PR is green on all eight checks.

### Won't build

- A `deploy rollback` command or any change to `deploy apply`, `deploy plan` or their messages (improvement plan M64; decisions Q2).
- A PowerOps read of the solution history (`msdyn_solutionhistory` or any other): the runbook sends the person to Power Apps' **See history** (decisions Q8).
- A check for a `_Upgrade` holding solution, or any statement of what `deploy plan` does next to one: not observed; the runbook says to resolve it before planning again (decisions Q2).
- New client checks or verification runs: the harness page writes down what earlier runs, docs and code readings recorded and marks the rest "not checked" (decisions Q6). The live rerun is M63.
- Azure DevOps or Power Platform Pipelines routes (the rest of C-6, improvement plan P1 and later).

## Users

- The person who promotes a plan and must decide what to do after an apply that stopped, timed out or left a flow off.
- The reviewer or admin who checks the environment against the report.
- A person or orchestrating agent who sets up an agent client to run the skills and the MCP server, and needs to know what that client lacks.

## Data

Line numbers are those of `431df9b`.

| # | Severity | Change | Where | Test |
|---|---|---|---|---|
| 1 | medium | New page `docs/RECOVERY.md`. Sections: first steps (keep the report; don't apply again until the state is known); read the report (exit code table: `0` `planned` or `imported` with every flow as expected; `1` `refused` or `imported` with a flow not as expected; `2` no report, nothing imported; `3` no report before any import, `failed` when the run stopped at a `failed` or `unknown` row, which may be the first so nothing may have been imported, or `imported` with "The imports succeeded." when the second read failed; row table for the six `import` values: a `failed` row with a `pacExitCode`, `-1` included (the 16 MB output cap), may have started an import and is checked in the history first, like `unknown`; with `pacExitCode` null, `pac` never ran for it); check the solution history (Power Apps, **See history**: start and end time, version, operation, result, error code and exception message; rows older than 180 days are deleted), then `powerops deploy plan` against the target, where each solution that imported shows `skip` and each that didn't shows `install` or `upgrade`; an import that timed out (wait for a result in the history before any new plan; with none, ask the admin); partial success (the `ok` rows stay; the order puts dependencies first, so an imported solution never needs one that did not run, but a `not-run` solution may use a component the imported upgrade deleted: check the plan's `deletes` items); a `_Upgrade` solution beside the base means an upgrade was staged and not applied or did not complete; while it exists a new upgrade or patch fails; applying it deletes components and deleting it may lose data, so it is the person's decision; forward repair (fix in Dev, export a higher version or fix the settings file, plan again, review, a new yes by the same route; an older zip is `skip` and imports nothing); what no import brings back (an upgrade deletes the components the zip no longer has; Learn says a table or column deleted by hand loses its data and only a restore recovers it, the pages don't say it of an upgrade, so a `deletes` item for a table is treated as data loss; `deploy plan` warns with `deletes` first; deleting a managed solution deletes its data; a restore is the admin's: backups kept seven days by default, up to 28 for a production managed environment, a restore overwrites the environment, restores only to sandbox, developer or Teams environments, flows off and connections to set again afterwards; PowerOps has no command for it and an agent never starts one); by route (Action, MCP form, CLI); Sources | `docs/RECOVERY.md` (new) | `DocsDriftTests`: rows 6 and 7 |
| 2 | medium | New page `docs/HARNESS.md`. A table of the five primitives (what PowerOps uses it for; without it). A per-client table for Claude Code interactive, `claude -p`, Codex TUI and `codex exec`, each cell with its evidence. Sources and the cells they fill: M19 (`docs/BUILD-PLAN.md:116`): `claude -p` forms; M20 (`:117`): forms of both interactive clients, Claude Code's passthrough (its `deploy_plan` read the target on Linux), Codex's passthrough and timeout; M21 (`:118`): hooks in `claude -p` and `codex exec`; the clients' docs as cited in `docs/HANDOFF.md:54-108` and `docs/USE-WITH-AGENTS.md:79-85`: hooks of the interactive clients, `onFailure`, Claude Code's 30-minute limit, Codex's documented timeout; Codex's code at `rust-v0.163.0` as read in USE-WITH-AGENTS (not run): `codex exec` and the `never` policy on forms, Codex's 300-second timeout; the Agent support table (`docs/PLANNING-SKILLS.md:271-283`) and M39 (`docs/BUILD-PLAN.md:131`): the question tools. Not checked: questions in `claude -p` and `codex exec`, passthrough and timeouts of both headless modes, and every primitive but the question tool for Copilot CLI, Antigravity CLI, Pi and Gemini CLI, which share one sentence. The degraded paths sit in the primitives table's "Without it" column, each linking the paragraph that holds the detail. A section for runs without a person: headless runs import by the Action route or the person's own command; an orchestrator that starts an interactive client answers or approves its start-up prompts first (M19: Claude Code's folder-trust prompt stopped its session before the form); an orchestrator's answers are marked `agent` (M54) | `docs/HARNESS.md` (new) | `DocsDriftTests`: row 6; the existing command, flag and tool-name checks read every page under `docs/` |
| 3 | low | Link `RECOVERY.md` at the end of step 8 (one sentence: after an apply that stops, times out or leaves a row `unknown`) | `docs/HANDOFF.md:49` | row 6 |
| 4 | low | Link `RECOVERY.md` after the first "There is no rollback" sentence and after the "no rollback" section's text (one sentence each) | `docs/examples/README.md:13`, `:123` | row 6 |
| 5 | low | Link `HARNESS.md` after the sentence on `claude -p` | `docs/USE-WITH-AGENTS.md:79` | row 6 |
| 6 | low | `DocsDriftTests.LinkProblems(Func<string, string> page)`: each page, read through `page`, holds a Markdown link to its target: HANDOFF `](RECOVERY.md` on its step 8 line; the example `](../RECOVERY.md`; USE-WITH-AGENTS `](HARNESS.md`; HARNESS `](RECOVERY.md`; README `](docs/RECOVERY.md` and `](docs/HARNESS.md`. The positive test also needs `docs/HARNESS.md` among the pages. Planted text: HANDOFF with the step 8 link moved to another line, USE-WITH-AGENTS without its link, and README without the HARNESS link each fail | `tests/PowerOps.Cli.Tests/DocsDriftTests.cs` | the new facts |
| 7 | low | `DocsDriftTests.RecoveryProblems(runbook, commands, schema)`: the exit codes are the backticked numbers of the "Exit codes:" sentence in COMMANDS's `## deploy apply` section (none found is a problem); each code and each value of the `import` enum of `solutions.items` needs a runbook line that starts with `` | `N` | ``; each value of the top `status` enum needs a code span on one of the exit codes' lines. Planted text: a runbook without the `unknown` row, without the `3` row, without `refused`, with `planned` dropped from the `0` row, a COMMANDS sentence with a fifth code, and an empty COMMANDS each fail | `tests/PowerOps.Cli.Tests/DocsDriftTests.cs` | the new facts |
| 8 | low | Two lines in the Documentation list | `README.md:132-143` | row 6 |

## Automation

None.

## Screens

None.

## Environments and route

No tenant. Docs and one test file. Nothing is imported, read or restored; the pages tell a person what to do and name every write in them as the person's or the admin's decision.

## Solution and publisher

None.

## Constraints

- AGENTS.md: smallest change, new logic with tests, Conventional Commits, no version or changelog edits, no tenant URLs, ids, emails or user names. Commands in the pages use placeholders (`<test>`, `deploy/`).
- `DocsDriftTests` already checks every command, flag and MCP tool name on every page under `docs/`, and that no page names a PowerOps release; the new pages pass them.
- Docs state only what a cited page, the code or a recorded run supports; a client fact names its evidence or is "not checked".
- Other P0 lanes run in parallel (M51, M55, M58 part 2). This milestone's files are the two new pages, one sentence each in `docs/HANDOFF.md` and `docs/USE-WITH-AGENTS.md`, two in `docs/examples/README.md`, two lines of `README.md`, and `DocsDriftTests.cs`. The pinned roles `powerops-verify` and `powerops-review-security` and `tests/eval` are untouched.
- Platforms are Windows and Linux.

## Assumptions

- Learn, read 2026-11-04 with the Learn MCP tools. "Recommendations for safe deployment practices" (OE:10): recovery can roll back to the last known working configuration, roll forward with a fix, or deploy the last known working configuration anew; rolling back data, schema or other stateful changes "can be complex", and the guidelines should say how to handle data changes; version build artifacts so you can roll back and forward; environments "provide point-in-time restore functionality that can help you roll back".
- Learn, read 2026-11-04. "View the history of a solution": **See history** in Power Apps' **Solutions** lists import, export and uninstall operations with start and end time, version, publisher, operation, suboperation and result (Success or Failure); a failure has an error code, an exception message and an activity id under **More details**; the system deletes history rows older than 180 days; the single-step upgrade no longer uses a temporary `_Upgrade` solution.
- Learn, read 2026-11-04. "Upgrade or update a solution": an upgrade deletes the components of the previous version that the new one doesn't have; after "Stage for Upgrade", or "if the system had an issue completing an upgrade", the original and a `<name>_Upgrade` solution are both installed, and **Apply Solution Upgrade** deletes the components missing from it; an update must have a higher version than the installed one; deleting a managed solution deletes all its components "as well as any related data".
- Learn, read 2026-11-04. "Solution upgrade fails due to a previously pending upgrade": while `<name>_Upgrade` exists, a new upgrade or patch is blocked ("Holding solution ... exists. Cannot update solution"); the workaround is to complete it or delete it, and deleting it "might lead to data loss if new data comes through tables and relationships in the upgrade".
- Learn, read 2026-11-04. "Delete columns" and "Create and edit tables using solution explorer": a column or table deleted by a system administrator (one not in a managed solution) loses its data, and the only way to recover it is to restore the database from a point before the delete. No page read says the same of a table or column removed by a managed upgrade; the runbook treats it as data loss and says it is an inference.
- Learn, read 2026-11-04. "Back up and restore environments": system and manual backups are kept seven days by default, extendable to 28 days for production managed environments; a restore overwrites the target environment; backups restore only to sandbox, developer or Teams environments, so a production environment is switched to sandbox first; after a restore, solution flows are off, and connection references need new connections.
- Code at `431df9b`: `deploy apply` stops pac after 60 minutes (`SettingsCommand.ImportTimeout`, killing its process tree in `PacRunner`) and marks the row `unknown`; `PacRunner` stops a `pac` that prints more than 16 MB and returns `-1`, which the apply loop reports as a `failed` row with `pacExitCode` `-1`; `pac` never runs for a `failed` row with no `pacExitCode` (pac not found, or a file changed); exit `2` and an exit `3` before the imports write no report (`DeployCommand.DryRun` returns a code without a name); the plan's order is a topological sort of the folder's zips (`docs/COMMANDS.md:83`), and a same-or-lower version is `skip`.

## Open questions

## Questions for your admin

## Steps

1. This plan and `decisions.md`, a fresh-context review into `review.md`, fixes, and sign-off under the delegation of 2026-10-22 once no High or Medium finding is open.
2. Rows 6 and 7, test first, against the current docs (they fail), then rows 1 to 5 and 8 (they pass).
3. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .`, the leak scan; push, PR, CI.
