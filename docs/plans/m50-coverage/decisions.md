# Decisions: m50-coverage

## Round 1

### Q1. What does `diff` list as not compared?
1. Only what these two exports hold that `diff` means to compare and can't read: an agent's instructions with no single Custom GPT component, and an agent's type-9 components it doesn't read as tools (recommended)
2. Also every component kind it never compares (tables, forms, views, topics, knowledge sources) whenever the solution has one
Answer: 1. Nearly every solution has a table, so option 2 would make the list never empty, and an empty list would no longer mean "everything `diff` compares was read". The kinds `diff` never compares are the same for every input, so the docs say them once. The list is about this input: it shrinks when M51 reads the new layout and stays empty for a standard agent.

### Q2. What shape do the coverage lists have?
1. An array of strings in all three outputs, each a sentence that names the component and the reason (recommended)
2. Objects with `component`, `key` and `reason`
Answer: 1. `analyze` already holds its notes as strings (`SolutionResult.NotChecked`) and prints them as `note:` lines; one shape across the three keeps the `note:` line and the JSON item the same text. The first reader is a person or a review role that reads the sentence. Turning a string into an object later would bump the schema version, but nothing yet needs the parts.

### Q3. How does the reader tell a tool it can't read from a topic?
1. By the first line starting with `kind:`: `TaskDialog` is read as today; `AdaptiveDialog` (topic) and `AgentDialog` (child agent) are not tools; any other kind, or none, is listed (recommended)
2. Parse every type-9 `data`
3. List only `McpTool`
Answer: 1. M48 kept topics from adding refusals by parsing a type-9 `data` only when its text holds `TaskDialog` (M48 decisions Q6); reading one line keeps that. Every export M48 read writes tools as `TaskDialog`, topics as `AdaptiveDialog` and child agents as `AgentDialog`, so those three are known. Option 3 would hide the next unknown kind, such as the six `IQCapability` components of the live export, which may carry access settings. A `data` with no `kind:` line is listed, because nothing says it isn't a tool.

### Q4. Where does SARIF carry `notChecked`?
1. `runs[0].invocations[0].toolExecutionNotifications`, one notification of `level` `note` per line, located at the solution, with `executionSuccessful: true`, always written (recommended)
2. The run's `properties` bag
Answer: 1. SARIF defines `toolExecutionNotifications` as "a list of runtime conditions detected by the tool during the analysis", which is what a check not done is; a property bag has no meaning a SARIF reader knows. Both are valid against the vendored schema, and GitHub accepts "any valid SARIF 2.1.0 output file". Writing the invocation every time makes an empty list mean "everything was checked", as in JSON. `executionSuccessful` is true because a run that fails exits `3` with no report.

### Q5. Does the text output or the exit code change?
1. No: the lists go to stderr as `note:` lines for every format, as `analyze`'s do; the text report on stdout and the exit codes stay as they are (recommended)
2. Add the lines to the text report
3. Exit `1` (or another code) when something was not compared
Answer: 1. `analyze` already prints its notes on stderr and keeps its exit code (`docs/COMMANDS.md`, analyze section), and the joint plan's acceptance says `diff` of the live exports exits 0 until M51. A new exit code would break every pipeline that runs `diff` on a solution with an agent of the new layout; the JSON field is how a pipeline finds out.

### Q6. Do the Markdown reports change?
1. Yes, for `diff`, `drift` and `analyze`: a short list after the summary (`diff`, `drift`) or under each solution (`analyze`), placed so a cut table can't drop it (recommended)
2. Only `diff`, as the joint plan names
Answer: 1. The Markdown is the pull request comment and the Action's job summary, where the false "no changes" would mislead a reviewer. The joint plan names `analyze`'s JSON and SARIF; its Markdown is one more output with the same gap, and the change is a few lines.

### Q7. What does `drift` list?
1. One line when fewer than two environments have the solution installed; each dropped type (1, 62, 80, 300, 10087) that a compared snapshot holds; each managed environment whose layers were not read (recommended)
2. Only the dropped types and `layers-not-read`, as the joint plan names
Answer: 1. With one environment nothing is compared between environments, and the docs say "`no drift` then means those checks found no drift, not that anything was compared"; one line makes the JSON say it too. Environment variables (380, 381) and connection references are left out of the comparison by id but compared by name under their own kinds, so they are not listed. The types are listed only when a compared snapshot holds one, so the list is about this input.

### Q8. Do schema versions move?
1. No: new fields keep `schemaVersion` (`diff` 2, the others 1), and each is required, because every output writes it (recommended)
2. Bump them
Answer: 1. `docs/schemas/README.md`: adding a field keeps the version, and the schemas don't forbid unknown properties. M52 made its new fields required the same way.

### Q9. Is a fixture made from the live exports committed?
1. No: the tests build a synthetic agent of the same shape, and the acceptance runs the built CLI on the live exports by hand, recorded here with no tenant detail (recommended)
2. A scrubbed copy under `tests/fixtures/agents/new-ui/`
Answer: 1. The joint plan gives the new-layout fixture to M51 (C-4), which reads that layout and needs the real file; M50 needs only the shape (no GPT component, `McpTool` and `IQCapability` components, a recognizer off the standard harness), which a test builds in a few lines. Not committing the export keeps scrubbing and the leak risk out of this milestone.

### Q10. Does `analyze` get new notes for agents with other component kinds?
1. No: an agent off the standard harness already gets the PO027 to PO031 line, which is what the live agent got; a standard-harness agent with `McpTool` components was not seen (recommended)
2. PO027 notes each other component of a standard-harness agent
Answer: 1. Nothing observed needs it, and M51 reads `McpTool` for the rules. Recorded as a follow-up for M51.

### Q11. Which agents get which lines?
1. The instructions line only for an agent in both versions (the only case where `diff` compares instructions); the components line for an agent in either version, counting each component once (recommended)
2. Both lines for every agent in either version
Answer: 1. `diff` compares instructions only for an agent it matched; an added or removed agent is reported as such. Tools are matched across all agents, so a component it can't read is a missed `agentTool` change whether its agent was added, removed or kept. A component in both versions is one component; one that was removed is still counted, because its removal is what went unreported in the live test.

### Q12. Does `docs/BUILD-PLAN.md` get an M50 section?
1. No: the improvement plan (M50 to M68) is not in the repo, and parallel sessions would collide on its numbers (recommended)
Answer: 1. As M52 decided (its decisions Q11).

## Round 2 (from the review)

### Q7, amended (R2, R5)
Q1 and Q7 follow one rule: a list holds what the command's own claim covers but this input made it skip. `diff` never claims to compare a table's content, and a table added or removed still shows as a `rootComponent` change, so tables aren't listed. `drift` says it compares "the components", yet a table that is missing from one environment gives no item at all, because tables are left out by id; so a type that a compared environment holds is listed, though that is most runs, and the docs and the schema say so. A pipeline that wants "complete apart from the documented types" can ignore lines that start with `component type`. The one-environment line splits in two: one snapshot (`--save` with one environment), and several with no pair installed in both, beside the `solution-missing` items that already exit `1`. 10087 is named by number only, because the source notes that codes above 10000 may differ per org. The layers line repeats the `layers-not-read` item on purpose. The writers compute the list from the snapshots they are given, so their shared signature doesn't change.

### Q3, amended (R3)
The line must begin at column 0, so a nested `kind:` never counts, and the first such line anywhere in the text is taken, so blank, comment and `---` lines before it are passed over. The byte order mark is dropped by `BoundedRead` before the text is seen. A parsed `data` (its text holds `TaskDialog`) takes its YAML top-level `kind`, null when absent. A type-9 component with no `data` file is skipped as before and not listed: there is nothing to read, and no export seen has one.

### Q13. Are the new `note:` lines capped? (R4)
1. At 20, then one line with the count, as `analyze`'s notes are; JSON and Markdown hold every line, Markdown within its 60,000-character limit (recommended)
2. Not capped
Answer: 1. A solution with hundreds of agents would print hundreds of lines, and the Action forwards every `note:` line to the log. The agent name is cut to 100 characters, as the kind is, so one line stays short. Every line goes through `TextReport.Clean` on stderr and `MarkdownReport.Escape` in Markdown, and `OutputHygieneTests` checks a hostile name in both.

### Q14. What do MCP callers get? (R1)
1. The JSON with the new field, and the `note:` lines as a second text block, as the server already forwards them for `analyze` (recommended)
2. Drop the note block for these lines
Answer: 1. The server's forwarding is one rule for every tool, and a client that shows only text blocks still sees the gap. `McpCommandTests` now expects two blocks for `drift`, as for `analyze`; the tool descriptions don't change.

### R6 to R9
The GitHub sentence in Assumptions now says that the one invocation property GitHub reads is `workingDirectory.uri`, which the report doesn't write, and a test checks that. A solution at `.` gets a notification with no location. Row 2 states the duplicate-name rule and that a file in both versions counts with the old version's kind; rows 3 and 4 say that stderr shows 20 lines and JSON all of them; the SARIF schema check lives in `SarifReportTests`, where the loader is, and `AnalyzeCommandTests` checks the shape; the `diff` docs say an agent's authentication mode, recognizer and settings are not compared. The Learn URL is replaced by the page's title.

## Round 3 (from the second pass)

### R10 to R12
The duplicate-name rule and the agent order now have a test. A `#` after white space on the `kind:` line starts a YAML comment and is cut, so `kind: AdaptiveDialog # a topic` is a topic; an anchor on that line is still read as part of the kind, which can only add a false note, and no export seen has one. The notes about links inside a solution that a load didn't follow stay on stderr only, as before: they are not a check a rule or a comparison skipped, the joint plan doesn't name them, and the `docs/COMMANDS.md` sentence about `notChecked` now says it doesn't cover them. A later milestone can carry them in the JSON if a reader needs them. These are code, test and docs fixes; `plan.md` doesn't change, so the reviewed hash holds.

## Log

- 2026-11-04: plan started at the request of the session that started this one: build M50 of the live-test improvement plan
- read on 2026-11-04: AGENTS.md; the M50 section of the improvement plan, its "Not doing" list and D1 to D9 with the answers; the live-test record's F6 and rows 48 and 49; the two reviews' C-1, C-3, C-4 and CX-1; the M48, M49 and M52 plan folders; `Solution` (agents), `SolutionParser.ReadAgents`, `Rules/Agents`, `Analyzer`, the PO027 and PO028 notes, `SolutionDiff`, `DiffReport`, `DiffCommand`, `AnalysisResult`, `Baseline.Apply`, `JsonReport`, `SarifReport`, `MarkdownReport`, `TextReport`, `DriftCompare`, `DriftReport`, `DriftCommand`, `Snapshot`; the three schemas and their README; `docs/COMMANDS.md` (analyze, diff, drift); `DiffCommandTests`, `DriftCommandTests`, `SarifReportTests`, `AgentReaderTests`, `RuleDocsTests`; the vendored SARIF schema (`invocation`, `notification`)
- baseline on 2026-11-04 with the CLI built at `b8ad904`, on the live exports (outside the repo): `diff v1 v2` printed "no changes", exit 0, nothing on stderr; `analyze v1 --format json` printed the PO027 to PO031 harness note on stderr only, and its JSON had no coverage field
- GitHub, "SARIF support for code scanning", read 2026-11-04 through a fetch subagent (Haiku): "Any valid SARIF 2.1.0 output file can be uploaded"; invocations and notifications are not among the properties it uses
- Learn checked 2026-11-04 with the Learn MCP tools: "Export and import agents using solutions"
- depth: quick: the improvement plan gives the design
- questions answered by the building session under the user's delegation of 2026-10-22 (autonomous milestone mode); D1 to D9 don't touch M50
- areas not asked: users, screens, solution and publisher; the change is to this repo, and the plan answers each or marks it not applicable
- approved 2026-11-04 by the building session under the user's delegation of 2026-10-22, for a fresh-context review
- fresh review (a general-purpose subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-11-04: 0 High, 5 Medium, 4 Low (R1 to R9); answered in `plan.md` (Done when 3, rows 1 to 5 and 7, Assumptions) and here (Round 2)
- the code built by the building session in a scratch copy of `b8ad904` while the review ran, then brought into this worktree; the reader and diff tests were written beside the code, not before it, so no red run was seen for them
- acceptance on 2026-11-04 with the CLI built from this worktree, on the live exports (outside the repo; agent name shown as `<agent>`): `diff v1 v2`, `diff v2 v1`, and `diff` of v1 against a re-zipped copy without the MCP tool's folder, in both orders, each printed `note: agent '<agent>': instructions not compared: neither version has a single Custom GPT component whose instructions are text` and `note: agent '<agent>': 8 components not read as tools, so a change to them is not compared: 6 'IQCapability', 2 'McpTool'`, then "no changes", exit 0; the JSON had no changes and two `notCompared` items, and the Markdown listed both under "Not compared". `analyze v1 --format json` had the PO027 to PO031 harness line in `notChecked`, and its SARIF had one notification of level `note`
- local checks 2026-11-04 in this worktree: `dotnet build` (0 warnings), `dotnet format --verify-no-changes`; the full `dotnet test` passed in the scratch copy (3,497 tests, 5 skipped)
- the same reviewer's second pass 2026-11-04, with the build in the tree, tested in a scratch copy outside the repo: R1 to R9 hold; about 45 mutants of the new logic, all killed but equivalent ones and two that R10 names; a re-zipped copy of the live export with a hostile agent name gave clean `note:` lines, one escaped Markdown line each, and escaped JSON and SARIF; 3 Low new (R10 to R12), each fixed with a test or a docs line. In all: 0 High, 5 Medium and 7 Low, every one fixed, with R11's anchor case and R12's link notes recorded as not built
- signed off 2026-11-04 by the building session under the user's delegation of 2026-10-22, with no High or Medium finding open (`powerops plan sign-off`; `signed-off-hash` equals `review.md`'s `plan-hash`); status then set to done, header only, so the hash holds
- final local checks 2026-11-04 in this worktree after the second pass's fixes: `dotnet build` (0 warnings), `dotnet test` (3,499 tests, 5 skipped), `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan (0 hits)
