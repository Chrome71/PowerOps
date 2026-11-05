plan-hash: 7ef5a6b0a18c2131f0103a607ca45784dd28b375c67add650d2759dacce52098
model: claude-sonnet-5-5
fresh-context: yes

Environment facts were not read: the plan names no environment (`environment: null`), the request did not say that `pac` is signed in, and the plan changes this repository's docs and one test file, not a Power Platform solution. I ran no `powerops plan facts`, `pac` or `az` and made no tenant call. I used the Microsoft Learn MCP tools (search only, read-only). Nothing in the worktree was changed except this file; `plan.md` was not edited, so its header still reads `review: pending` (the request made `review.md` the only writable file). `plan status` reports `status: draft`, while the Log of `decisions.md` says "approved 2026-11-04" (see R13); the skill stops on a non-approved plan, and I reviewed anyway because the request asked for it. The review is against `431df9b`. `docs/RECOVERY.md` and `docs/HARNESS.md` did not exist when I read the tree, and `tests/PowerOps.Cli.Tests/DocsDriftTests.cs` already holds the draft of rows 6 and 7 (uncommitted), which I read as a check of the test design.

Checklist items 1 to 8 do not apply (no solution, publisher, connection reference, connector, DLP, route, managed state, delegation or role: the change is docs and one test). Item 9 (testability) applies and is covered by R8 to R10. Item 10: no open questions, and the Log records the areas not asked with a reason. Item 11 (size): every row traces to a Done-when line (1: row 1; 2: row 2; 3: rows 3, 4, 5, 8; 4: rows 6, 7; 5: Steps), and `Won't build` is present; the one size remark is R11.

References checked and found true at `431df9b`: `docs/examples/README.md:13` ("There is no rollback...") and `:121-123` (heading at 121, text at 123); `docs/HANDOFF.md:49` (step 8) and `:54-108` (the gate section); `docs/USE-WITH-AGENTS.md:79` (the "Answer the form yourself" paragraph, with the `claude -p` sentence) through `:85`; `docs/PLANNING-SKILLS.md:271-283` (Agent support table and the "Supported" paragraph); `docs/BUILD-PLAN.md:116-118` (M19 to M21); `README.md` Documentation list (heading 130, items 132-143); `docs/COMMANDS.md:83` (skip rule) and `:91` (the `deploy apply` paragraph, with a single "Exit codes:" sentence listing `0` to `3`); the schema enums for `status` (4 values) and `import` (6 values); `SettingsCommand.ImportTimeout` (60 minutes) and `PacRunner` (`Kill(entireProcessTree: true)` on a timeout); `DeployPlan.cs:229` (same-or-lower is `Skip`). Code claims that held: exit `2` (usage, target mismatch) and exit `3` before the imports return `Stopped(code)` with no name, so no report (`DeployCommand.cs` 205-235, 339); refusals are exit `1` with a report; a `failed` report is exit `3`; an `imported` report with "The imports succeeded." is exit `3` and only when the plan has cloud flows (`flowRefs.Count > 0`); `pacExitCode` is set only for a `failed` row after a non-zero `pac` exit, null for `unknown`, a changed file and a missing `pac`; every row after a stop is `not-run`, and `skip` rows are `skipped`.

## Findings

### High

No findings.

### Medium

#### R1. "`codex exec` cancels forms" is a code reading, not an observation
- Area: Testability / claims (Done-when 2, row 2)
- Finding: Done-when 2 states as fact that "`claude -p` and `codex exec` cancel forms". `claude -p` was seen (M19, Claude Code 2.1.303: `cancel` within 15 ms). For `codex exec` the repo says "read in the code, not run" (`docs/USE-WITH-AGENTS.md` line 79) and M19 only reports Codex's code at `rust-v0.163.0` ("auto-cancels it in `codex exec`"); M19 ran `codex exec` to see what the client declared, not to answer a form. The plan's own rule is that a client cell is "a recorded observation (with its run) or 'not checked'", which leaves no place for a third kind of evidence the repo already uses.
- Suggested fix: Name the evidence kinds the page may use (run, code reading with the tag and file, vendor docs) and mark each cell with its kind. Word Done-when 2 as "`claude -p` cancels forms (M19); `codex exec` cancels them in Codex's code at `rust-v0.163.0` (read, not run)". Repeat the same limit in the HARNESS table cell.
- Resolution: fixed. Done-when 2 says `claude -p` cancels forms (M19) and `codex exec` cancels them in Codex's code (read, not run); the page names four kinds of evidence (a run of the verification record, the client's docs, its code read but not run, the Agent support table), and the `codex exec` cell says "read in its code at `rust-v0.163.0`, not run".

#### R2. The sources named for the client table do not hold most of the cells, and M39 is missing from them
- Area: Testability / claims (row 2)
- Finding: Row 2 builds eight client rows by five primitives from PLANNING-SKILLS 271-283, USE-WITH-AGENTS 79-85, HANDOFF 54-108 and M19 to M21. Those hold: forms, timeout and passthrough for Claude Code and Codex (M19, M20, `tool_timeout_sec`, `env_vars`), and hooks for both (M21). They hold nothing on forms, hooks, passthrough or timeouts for Copilot CLI, Antigravity CLI, Gemini CLI or Pi, and only the question tool for them (the PLANNING-SKILLS table). The "question" cells for Copilot, Antigravity and Pi rest on M39 (the client checks), but Q6 cites M39 while row 2's source list stops at M21 (`BUILD-PLAN.md:116-118`; M39 is at line 131). Also, Claude Code's passthrough has no record at all (the passthrough text is about Codex), and Claude Code's question tool (`AskUserQuestion`) is a table entry, not a run. As written, about two thirds of the table is "not checked" and the builder has to guess which cells may be filled.
- Suggested fix: Add M39 to row 2's sources and say which cells each source fills. List the cells that will say "not checked" now (Claude Code passthrough; hooks, forms, passthrough and timeouts of the four non-Claude, non-Codex clients). Consider dropping the rows with no recorded primitive (see R11).
- Resolution: fixed. Row 2 lists each source with the cells it fills, adds M39 (`docs/BUILD-PLAN.md:131`) and the Agent support table for the question tools, and lists the cells that say "not checked". Claude Code's passthrough cites M20, where its `deploy_plan` read the target on Linux (`docs/BUILD-PLAN.md:117`); `AskUserQuestion` cites the Agent support table.

#### R3. The exit-code and row tables can mislead on `failed`
- Area: Testability / claims (row 1)
- Finding: Three points in the code that the row text does not cover. (a) "`3` ... `failed` after one [import]": a `failed` report also comes from the first import (a changed file or `pac` not found, message "Nothing was imported."), so `failed` does not mean an import ran; `DeployCommand.cs` 360-383. (b) "`pacExitCode` null on a `failed` row means `pac` never ran for it" is true (null only for a missing `pac` or a changed file), but a `failed` row with a number does not mean the environment is untouched: `pac` exited non-zero after it may have started the import, and the output cap in `PacRunner` (16 MB) kills `pac` and returns exit code `-1` (`TooLarge`), which the apply loop reports as `failed` with `pacExitCode` `-1`. The runbook should treat `failed` with a number, and `-1`, like `unknown` for reconciling: check the history first. (c) The `unknown` row is exit `3` with status `failed` at the top, not a status of its own; the exit-code table cell for `3` should say so, or a reader looks for an `unknown` status.
- Suggested fix: Reword the `3` cell (`failed` with or without an import before it; `unknown` is a row value under `failed`) and add one sentence that a `failed` row with a `pacExitCode`, `-1` included, still needs the history check. Row 6's rule on `pacExitCode` null stays as it is.
- Resolution: fixed. The `3` row says `failed` means the run stopped at a `failed` or `unknown` row, which may be the first, so nothing may have been imported; `unknown` is named there as a row value. The `failed` row says `-1` is the 16 MB cap and that a row with any `pacExitCode` is checked in the history first, like `unknown`.

#### R4. "Data deleted by an upgrade is not restored" is not what the cited pages say
- Area: Assumptions (Learn support)
- Finding: The assumption cites "Delete columns" and "Create and edit tables using solution explorer" for "a deleted column's or table's data is lost, and the only way to recover it is to restore the database". Both pages hold that sentence, but they are about a system administrator deleting a custom column or table that is not part of a managed solution (the pages say so in their first sentence). They do not say that removing a table or column by a managed upgrade deletes its data. "Upgrade or update a solution" says an upgrade deletes components the new version lacks, and that deleting a managed solution deletes components "as well as any related data", which is a different action. The `deletes` item in the plan (`DeployPlan.cs` 315-338, warning) also says "a deleted table loses its data" with no source. The conclusion may well be right, but it is an inference from two pages, and the runbook is supposed to cite pages that support each claim.
- Suggested fix: In the runbook and in Assumptions, say "an upgrade removes components the new version lacks (Upgrade or update a solution); for a table or column that is removed by hand, Learn says the data is lost and only a restore from before the delete recovers it; whether an upgrade's removal of a table or column deletes its data is not stated on the pages read". Or cite a page that says it. Keep "only an environment restore" out of the runbook as a fact about upgrades unless a page supports it.
- Resolution: fixed. The runbook says an upgrade deletes the components the zip lacks (Upgrade or update a solution), that Learn says a table or column deleted by hand loses its data, that the pages read don't say this of an upgrade, and so to treat a `deletes` item for a table as data loss. Assumptions say the same; the restore sentence is scoped to the data of a deleted table or column.

#### R5. `_Upgrade` handling is under-sourced and slightly narrower than Learn
- Area: Assumptions (Learn support), row 1
- Finding: The plan says a `_Upgrade` solution next to the base means "the upgrade did not complete". "Upgrade or update a solution" says the pair appears "if you chose to stage for upgrade, or if the system had an issue completing an upgrade", so the presence also follows a staged upgrade that nobody applied. The troubleshooting page for a pending upgrade ("Solution upgrade fails due to a previously pending upgrade") is not in the Assumptions and bears directly on the runbook's "forward repair": while the holding solution exists, a new upgrade or patch fails ("Holding solution ... exists. Cannot update solution"), so forward repair by a new plan fails until the person completes or deletes it; that page also says deleting the holding solution can lose data from new tables and relationships. The runbook's advice ("resolve it before planning again; it's the person's decision") is right but gives the reader no reason. PowerOps passes `--stage-and-upgrade` for a managed upgrade (`SettingsCommand.ImportArguments`); whether that run ends with the holding solution gone has no recorded observation, which the plan does admit under Won't build.
- Suggested fix: Add the pending-upgrade troubleshooting page to the Assumptions with the two facts above, widen the cause to "staged or not completed", and keep the "not observed" sentence about `deploy plan` next to a holding solution.
- Resolution: fixed. The cause reads "staged and not applied, or did not complete"; the runbook says a new upgrade or patch fails while the holding solution exists, and that deleting it may lose data; the troubleshooting page is in the Assumptions and the Sources. The "not run beside such a solution" sentence stays.

#### R6. "An imported solution never needs one that did not run" hides the risky direction
- Area: Testability / claims (row 1, partial success)
- Finding: The ordering claim is true for dependencies among the zips of the folder (`COMMANDS.md:83`, topological sort), and a dependency outside the folder is read from the target. What a person needs after a partial run is the other direction: the solutions that did not run (`not-run`) still hold the old version while an earlier one was upgraded, and an upgrade deletes components the new version dropped. A `not-run` dependent may use a component that the imported dependency removed. The sentence as planned reads as "partial success is safe to leave".
- Suggested fix: Say both: the `ok` rows stay and need nothing undone by PowerOps, and a `not-run` solution that depends on an imported one should be checked against that solution's `deletes` items in the reviewed plan before deciding to finish or to leave the state. Keep it a person's decision, as the plan does elsewhere.
- Resolution: fixed. Partial success says the other direction can break, and to check the imported solutions' `deletes` items against the solutions that did not run.

#### R7. The harness line on start-up prompts cites a private record
- Area: Testability / claims (row 2, Goal)
- Finding: Row 2 ends with "answer its start-up prompts first (A-11)", and the Goal and Won't build use C-6, R15, M63 and M64. `A-11`, `C-6`, `R15` and the improvement plan live outside the repo, so a reader of `HARNESS.md` cannot follow them, and the page must not state the A-11 finding as if observed in a recorded run. The repo does record the fact behind it: M19 (`docs/BUILD-PLAN.md:116`) says Claude Code 2.1.303's interactive session "stopped at its folder-trust prompt for the scratch folder, so it sent no form".
- Suggested fix: Cite M19 for the start-up prompt and word the line as advice ("answer or pre-approve the client's start-up prompts, such as the folder-trust prompt, before relying on a form"). Keep improvement-plan ids out of the two pages; the plan folder may keep them.
- Resolution: fixed. HARNESS cites M19 (Claude Code 2.1.303's interactive session stopped at its folder-trust prompt, so it sent no form) and words the line as advice; neither page names a finding id of the improvement plan.

#### R15. The runbook says a re-added table comes back empty, which no page read says
- Area: Assumptions (Learn support), `docs/RECOVERY.md` "What no import brings back"
- Finding: The second bullet says "A new version that adds a deleted table or column back brings back an empty one, not its data." None of the Learn pages in the Sources or the Assumptions says this, and neither does the plan (row 1 and the Assumptions stop at "an inference ... says so" for the deletion itself). It is probably right, but it is stated flat, in the same section where R4 asked for inferences to be marked as such, and the page is the only place it appears.
- Suggested fix: Remove the sentence, or word it as the same kind of inference as the bullet above it ("expect ... no page read says what happens to the data"), and add it to the plan's Assumptions.
- Resolution: fixed. The sentence is removed from the runbook; the plan never had it.

### Low

#### R8. The `status` check can pass for the wrong reason
- Area: Testability (rows 7 and Done-when 4)
- Finding: Row 7 needs each top `status` value "in a code span anywhere in the page". `planned` and `failed` are also `import` values, so their rows already put them in code spans; only `refused` and `imported` are checked by this rule. Q4 says the status values "sit inside the exit-code table's cells", but the check does not enforce that. The exit-code and `import` row checks are sound (the table rows are keyed to `` | `N` | ``, a missing row and a fifth code in COMMANDS each fail in the written test) and, as in the existing tests, they check that a row exists, not what it says.
- Suggested fix: Either require the status values in the exit-code table's lines (a code span on a line that starts with `` | `N` | ``), or say in row 7 that the status rule is a presence check and note the overlap. Add that the check proves a row exists, not that its content is right.
- Resolution: fixed. Each `status` value must be a code span on an exit code's row line, and the test plants a `0` row without `planned`. Done-when 4 and row 7 say the checks prove rows exist, not what they say.

#### R9. The README link and the HARNESS-to-RECOVERY links are promised but not tested
- Area: Testability (Done-when 3 and 4, rows 6 and 8)
- Finding: Done-when 3 lists four link sources (HANDOFF, the example, USE-WITH-AGENTS, README for both) and Done-when 4 says the test fails "when either link is missing". Row 6 tests three links and row 8 (the README) has "none (prose)". Nothing checks that the README lists the pages or that `HARNESS.md` points to `RECOVERY.md` (row 2 says it should, for the timeout path).
- Suggested fix: Make Done-when 4 name the three tested links, or add the README lines and the HARNESS to RECOVERY link to `Links` in the test (the `Func<string,string>` form of the written test takes them as two more tuples).
- Resolution: fixed. `Links` holds the README's two links and HARNESS's link to RECOVERY; the test plants a README without the HARNESS link. Row 8's test is row 6.

#### R10. Row 6's signature and the example's first "no rollback" sentence
- Area: Testability / claims (rows 4 and 6)
- Finding: Row 6 names `LinkProblems(text, page)`; the written test uses `LinkProblems(Func<string,string> page)`, which is a better fit (it reads three pages) but is not what the row says, and the Data table should match the code. Separately, `docs/examples/README.md:13` is where a reader first meets "There is no rollback" (Goal cites it), but row 4 links only from `:121-123`, and the link check for that page accepts any line.
- Suggested fix: Align the row text with the signature. Add the link to `:13` too, or say why the section at `:121-123` is enough.
- Resolution: fixed. Row 6 names `LinkProblems(Func<string, string> page)`. The example links the runbook at `:13` too, and the link check for that page accepts either line.

#### R11. Size: client rows with no recorded content, and repeated degraded paths
- Area: Size (item 11)
- Finding: Row 2's table has a row for Copilot CLI, Antigravity CLI, Gemini CLI and Pi where, per R2, four or five primitives each say "not checked". The "degraded paths" list repeats text that `USE-WITH-AGENTS.md` (forms, timeouts, Codex environment) and `HANDOFF.md` (deny rules) already hold. Q7 says the page should not copy PLANNING-SKILLS's table, yet the per-client table does so for the question tool. No Goal line fails if the empty rows go: Done-when 2 asks only for "a per-client table for the clients PLANNING-SKILLS names".
- Suggested fix: Keep the per-client table to the clients with a recorded cell, name the others in one sentence ("not checked"), and let each degraded path be one line that links the existing paragraph.
- Resolution: fixed. The per-client table keeps the four Claude Code and Codex modes; the four provisional clients share one sentence. The degraded paths stay in the primitives table's "Without it" column, with links to the paragraphs that hold the detail, and the question tool cells cite the Agent support table instead of copying the rest of it.

#### R12. The restore advice leaves out the retention limits
- Area: Assumptions (Learn support), row 1 "not restored"
- Finding: "Back up and restore environments" gives limits a reader needs before counting on a restore: system and manual backups are kept seven days by default (28 for production managed environments), and a production environment switched to sandbox for a restore can use only a backup from the past seven days. The plan names sandbox-only, flows off and connections set again, which the page supports, but not the retention. Without it the runbook can promise a restore point that no longer exists.
- Suggested fix: Add the seven-day default and the 28-day maximum to the Assumptions and to the runbook's sentence, as the admin's question to answer.
- Resolution: fixed. The runbook and the Assumptions give the seven-day default and the 28-day maximum for production managed environments.

#### R13. Header and Log disagree, and the review flag is not set
- Area: Unanswered areas (item 10)
- Finding: `plan.md` has `status: draft` and `review: pending`, but the Log of `decisions.md` says "approved 2026-11-04 ... for a fresh-context review". `powerops plan status` reports `draft`, and the skill's step 2 stops on anything but `approved`. This run did not set `review: done` in `plan.md` (writing only `review.md` was the instruction).
- Suggested fix: Set `status: approved` in the header before the review is counted, then `review: done` after the findings are resolved, as the skill's step 5 describes. Do not change the body: that changes the hash and makes this review stale.
- Resolution: fixed. The header reads `status: approved` and `review: done`; the body changed with these fixes, so a second pass records the new `plan-hash`.

#### R14. The Claude Code passthrough cell says more than M20 shows
- Area: Testability / claims (`docs/HARNESS.md`, per-client table)
- Finding: The cell reads "None needed on Linux in M20: `deploy_plan` read the target". M20 shows that `deploy_plan` worked in Claude Code 2.1.312 in a session driven through tmux; it does not record a run without the two variables, and Claude Code's environment handling is not described in any page read. Only Codex's need was reproduced (`env -i`).
- Suggested fix: Word it as what was seen: "`deploy_plan` read the target in the M20 session without extra variables; whether Claude Code passes them on was not tested".
- Resolution: fixed. The cell says what M20 saw: `deploy_plan` read the target on Linux in that session with no added variables, and a run without them was not tested.

#### R16. "Stopping `pac` doesn't stop an import" is stated as fact
- Area: Testability / claims (`docs/RECOVERY.md`, "An import that timed out")
- Finding: The page says flatly that stopping `pac` doesn't stop an import the environment is running. The code's own message says the import "may still be running", the schema says "may have finished or still be running", and `USE-WITH-AGENTS.md` marks the related case "not tested". No run or Learn page shows what the environment does when `pac` is killed.
- Suggested fix: Say "may not stop" or "does not necessarily stop", as the report's message does. The advice that follows does not change.
- Resolution: fixed. The runbook says stopping `pac` "may not stop" an import the environment is running, as the report's message does.

#### R17. The `appliedAt` match uses UTC against a history that may show local time
- Area: Testability / claims (`docs/RECOVERY.md`, "Check the solution history")
- Finding: The step tells the reader to find the import that "started at or after the report's `appliedAt` (UTC)". `appliedAt` is UTC (schema). Learn's page on the history does not say what time zone the **See history** list shows, and the history table's start-time column is user-local in Dataverse's reference, so a reader may compare a local time with a UTC one.
- Suggested fix: Add "convert it to the time zone the history shows" to that sentence.
- Resolution: fixed. The sentence says `appliedAt` is UTC and to convert it to the time zone the history shows.

## Second pass

Read at `431df9b` plus the uncommitted build (5 changed files, `docs/RECOVERY.md`, `docs/HARNESS.md`), with `plan-hash` updated to the current `bodyHash` (`plan.md` header: `status: approved`, `review: done`).

Earlier resolutions: R1 to R13 each hold in `plan.md` and in the pages or the test; I checked every one against the text. R1: Done-when 2 and the `codex exec` cell tag the code reading. R2: row 2 lists sources and cells, adds M39 and the Agent support table, and names the "not checked" cells (see R14 for one cell). R3: the `3` row and the `failed` row say what the code does (first-row `failed`, `-1`). R4: Q11 and the runbook mark the upgrade-deletion data loss as an inference (see R15 for a new sentence outside it). R5, R6, R7, R12, R13: present as stated; neither page names a finding id (grep finds none). R8 and R9: the `status` rule is now "a code span on an exit code's row", with a planted `planned` case, and the README and HARNESS links are tested. R10 and R11: matching signature, link at `:13`, four client rows plus one sentence. The 23 tests of `DocsDriftTests` and `HandoffDocTests` pass.

What I checked in the build: every `deploy apply` claim in both pages against `DeployCommand.cs`, `PacRunner.cs`, `SettingsCommand.cs`, `action/apply.sh` and `action.yml` (exit codes, reports, `pacExitCode`, `-1`, the 60 minutes, `not-run`, the Action's debug-only `pac` output, `NoFormLine` for a client without forms); each client fact against M19 to M21, M39, the Agent support table and `USE-WITH-AGENTS.md`/`HANDOFF.md`; each Learn claim against the pages (history, upgrade, pending upgrade, delete column and table, backup and restore, safe deployment); the anchors (`#the-steps`, `#when-an-apply-refuses`, `#mcp-server`, `#planning-skills`, `#verification-record`, `#agent-support`) exist; the test reads as the plan says, including the planted cases. I made no planted edits to the worktree. Not run: any tenant, `pac`, `az` or `plan facts`.
