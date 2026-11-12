plan-hash: 16fe843eeed1dc889e3db6af8c0778a3df635d9a406ca14a49e234fc4665420f
model: claude-sonnet-5-5
fresh-context: yes

Environment facts were not read: `powerops plan facts` was not run (no pac sign-in confirmed) and no `pac` or tenant call was made. The plan names no environment (`environment: null`), so none was needed.

Scope: the plan changes the PowerOps repo itself (one report field, its schema, tests and docs). Checklist items 1 to 8 (solution boundaries and publisher, connection references and environment variables, premium connectors and licences, DLP, Dev to Test to Prod route, managed or unmanaged, data volume and delegation, security roles) do not apply: the plan builds no solution, table, flow, app or connector, and its Environments and route, Solution and publisher, Screens and Automation sections say "None". Items 9 to 12 were checked. The Data rows were checked against `b065f23`: `DeployApplyResult` (`DeployApply.cs:18-21`), `Apply`, `Finish` and `ReadAgents` (`DeployCommand.cs:489-615`), `DeployApplyReport.cs`, `deploy-apply.schema.json`, `COMMANDS.md` (`deploy apply` section), `HARNESS.md:20`, the tests it names and the five `deploy-apply*.json` goldens. The mechanism in row 2 holds: `Finish`'s local function reads the captured `agentsReadAt` after `ReadAgents()` has run as an argument, and the goldens have `"agents": []` only, so the `.json` goldens gain a null and the `.txt` goldens are unchanged.

Tools (item 12): `dotnet` and `powerops` were found on PATH. `pac` was not run, by instruction. The Fable review, `powerops-researcher` and the Update goldens recipe are not commands, so they were not checked. The leak scan is under finding R3.

Open questions: 0 (`Open questions` and `Questions for your admin` are empty). The areas the `decisions.md` Log lists as not asked are not applicable here.

Second pass (plan-hash above): the revised plan was re-read against `b065f23`; R1 to R8 are answered, no new finding was added, and no finding is open.

## Findings

### High

No findings.

### Medium

#### R1. `agentsReadAt` would be set when no read was made
- Area: 9 (testability) and the Done when 1 and 2 definitions
- Finding: Done when 1 says the field is null when nothing was read, and Data row 2 sets it "right after the `deployAgents` call returns". But `DriftLiveReader.ReadAgents` (`DriftLiveReader.cs:52-59`) returns an empty list without any `pac` or Web API call when none of the names is valid (`IsValidAgentName`), and `AgentsAfter` then gives every agent `not-read`. In that case the call returns, so the field would hold a time and the text report would print `note: agents read at <time>` over agents that were never read. The schema text "null when no agent was read" and the Q1 decision ("a time for a read that failed would date a result the report doesn't have") say the opposite. The row 6 cases (a) to (d) don't include it, and the existing `not-read` tests don't assert the field.
- Suggested fix: decide the rule in the plan: set the field only when at least one expected name is valid (`DriftQueries.IsValidAgentName`, the same test `AgentsAfter` uses), or say in the schema and `COMMANDS.md` that a time means the read-back call returned, also when it sent no name. Add a case (e) to row 6 for a zip whose only agent has an invalid name: field and note line match the chosen rule.
- Resolution: Answered (second pass). Done when 1 and Data row 2 now set the field only when `AgentsAfter` holds a `found` or `missing` agent, which covers the failed read and the all-unsendable names alike; row 4's schema text and Q1 say the same; row 6 (e) adds the unsendable-name case through the real reader (`LiveAgents` exists in `DeployFakes.cs`) with no web call. Checked against `AgentsAfter` (`DeployApply.cs:197-209`): an invalid name gives `not-read` and a valid one `found` or `missing`, so the rule holds for a mix as well.

#### R2. The evidence the docs and schema would cite is not in the repository
- Area: 10 (unanswered areas) and the plan's own Constraints (AGENTS.md ecosystem rule)
- Finding: Done when 3 and 4 and Data rows 4 and 5 put "trial 3", "2.1.322 on Linux", "40 seconds after the import" and "an agent `found`" into `COMMANDS.md`, the schema's `agents` description and `HARNESS.md`. `HARNESS.md`'s opening says each client fact names its evidence: a run of the verification record, the client's docs, code read, or the Agent support table. A search of `docs/` and the README finds no "trial 3" and nothing recording 2.1.322's hooks in the desktop Code tab; the record (P8, P9, P10, D18, N3) is private. A reader of the public docs can't open "trial 3", and the repo's drift tests can't hold the claim. Assumptions says the numbers come from the record but not where a reader finds it.
- Suggested fix: add a short entry for the 2026-11-11 run to the verification record in `docs/BUILD-PLAN.md` (or an entry in `docs/ecosystem/changes.md`) with what was observed and the client version, and have the three places point to it; or word the three places as "observed once on 2026-11-11" without the label "trial 3". Say which in Assumptions.
- Resolution: Answered (second pass). `decisions.md` now has an Evidence section (client, hooks, the detached import, the 40 seconds, with the cause marked as inferred, no evidence file opened); Done when 3 and 4 and row 5 link the public docs to it, and `HARNESS.md:3` gains the new kind of evidence. It is a plan record, not a verification-record entry, but the claim is now findable and worded "in one run".

#### R3. The leak scan in Done when 6 can't be run from this worktree
- Area: 12 (tools)
- Finding: Done when 6 and Step 5 require "the leak scan" without a path. `reports/leakscan.sh` and `reports/leak-patterns.txt` exist only as untracked files in the main checkout; the worktree has no `reports/` folder and no tracked leak scan, and the plan's Assumptions doesn't name it. The step as written fails here, or is skipped.
- Suggested fix: name the command with its absolute or main-checkout path in Done when 6 and Step 5, and add to Assumptions that it is an untracked local tool, so it runs against the committed branch from the main checkout.
- Resolution: Answered (second pass). Done when 6 and Assumptions give `bash reports/leakscan.sh origin/main <branch>`, run from the main checkout as an untracked local tool. The script's usage is `<base> <head>` and it diffs in the main checkout, which shares the worktree's branch.

### Low

#### R4. Adding a last positional parameter breaks the existing constructor calls
- Area: 9 (testability)
- Finding: `new DeployApplyResult(...)` is called positionally in `DeployApplyReportTests.cs:76` and, through the `Result(...)` helper, in the report tests whose exact text (`:131`: `...agent A/a_Four: not read\napplied\n`) and exact count of newlines (`:84`) must not change. Data row 1 gives no default, so these fail to compile until the plan's row 6 touches them, and the row doesn't say so.
- Suggested fix: say in row 1 whether `AgentsReadAt` has a default of `null` (the smallest change) or that the helper and `:76` are edited; keep the existing exact-text tests passing unchanged with the null.
- Resolution: Answered (second pass). Row 1 gives `AgentsReadAt` a default of null, so `DeployApplyReportTests.cs:76`, its `Result` helper and the exact-text tests at `:131` stay unchanged; row 6 adds the JSON order test at `:148-156` with a value and with null.

#### R5. Time scrubs stay `appliedAt`-only
- Area: 9 (testability)
- Finding: `Scrub` (`DeployApplyCommandTests.cs:1789`) and `WithoutTime` (`McpCommandTests.cs:1256`) replace only `appliedAt` and `Applied at:`. Today every golden and the CLI-versus-MCP comparison (`:1401`) run with no agent read, so `agentsReadAt` is null on both sides and nothing flakes. A later golden or comparison of a run with an agent read would differ across a second boundary.
- Suggested fix: extend both scrubs to `agentsReadAt` and the `note: agents read at` line in this change (a few characters each), or note in row 6 that no golden or comparison reads an agent.
- Resolution: Answered (second pass). Row 6 keeps `Scrub` and `WithoutTime` as they are and says why: every `deploy-apply*.json` golden has `"agents": []`, and the CLI-against-MCP comparison (`McpCommandTests.cs:1401`) has no agent zip. Test (a) now asserts inside one output and never compares times across two runs, which removes the second-boundary risk the finding raised.

#### R6. Several line references of the Data rows are off
- Area: 9 (testability)
- Finding: Checked against `b065f23`. Row 3: `Doc` is `DeployApplyReport.cs:88-90` (not 84-86), `WriteJson` is `:64-68`, the agent loop `:48-58`, the summary write `:60`. Row 5: `COMMANDS.md:675-676` is a blank line and the "Otherwise the dry run" paragraph; the paragraph on a timed-out import is `:679-680`, and `:307` is the `settings apply` sentence that already says "up to an hour". The `publishedOn` paragraph (`:685-689`) and `:688`, row 1, row 2 (`:489`, `:537`, and `ReadAgents` at `:593-615`) and `HARNESS.md:20` are right.
- Suggested fix: correct the two rows' line numbers so the implementer edits the right paragraph.
- Resolution: Answered (second pass). Row 3 is now `DeployApplyReport.cs:48-60, 64-68, 88-90` and row 5 `COMMANDS.md:679-689` with the paragraph on a timed-out import named as the anchor, and `HARNESS.md:3, 20`; all checked against `b065f23`.

#### R7. `decisions.md` holds documentation URLs
- Area: Plan and `decisions.md` hygiene (the URL rule)
- Finding: the 2026-11-11 research entry in the `decisions.md` Log writes out host and path strings of public vendor documentation pages. They hold no tenant data, but the review rule says to report any URL; the repo's own convention is a `Sources:` line with page titles.
- Suggested fix: keep the page titles and "read 2026-11-11" and drop the host paths, or leave them as public documentation on purpose. No GUID, email or person's name was found in `plan.md` or `decisions.md`.
- Resolution: Answered (second pass). `plan.md` and `decisions.md` hold no URL, host path, GUID, email or person's name (searched); the sources are page titles with read dates, and the `COMMANDS.md` links in row 5 are relative.

#### R8. The note says "check with drift" without saying how
- Area: 9 (testability) and Users
- Finding: the note line and the `COMMANDS.md` sentence send the reader to `drift`. `drift` compares an environment with a snapshot or two environments, and reports `agent-published` only for a pair; it has no single "is this agent published now" report. A reader who runs `drift` after the note may not know what to compare.
- Suggested fix: say in the `COMMANDS.md` sentence which `drift` run shows it (for instance against a snapshot taken before the import), or change the note's wording to "check in Copilot Studio, or with `drift`" and keep the detail in `COMMANDS.md`.
- Resolution: Answered (second pass). The note reads "check in Copilot Studio, or with drift", and row 5's `COMMANDS.md` sentence says which run: `drift` between the source environment and the target, which reports `agent-published` while the agent is published in only one (matches `COMMANDS.md:359` and `:399`).
