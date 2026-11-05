plan-hash: a9304686fb673ab73f09f06722ec160236b63cbbfb2807cbbf743e427f3ad772
model: claude-sonnet-5-5
fresh-context: yes

Environment facts were not read: the plan names no environment (`environment: null`) and changes this repo's own code, not a Power Platform solution. Checklist items 1 to 9 and 11 (solution boundaries, connection references, licences, DLP, route, managed state, delegation, security roles, tables and flows) do not apply and are not reported. Items 10 (open questions: 0) and the URL/GUID/email/name scan do apply. `plan.md` was not edited (`review: pending` stays), because this request allowed no change outside `review.md`.

## Findings

### High

No findings.

### Medium

#### R1. MCP server tests assert one content block for `diff` and `drift` and will fail
- Area: tests and callers the plan misses (Data rows 3, 5)
- Finding: `McpCommand.Result` forwards every stderr line that starts with `note:` as an extra text block (`src/PowerOps.Cli/McpCommand.cs:150-167`). `McpCommandTests` runs `drift` on `tests/fixtures/drift/seed-dev.json` and `seed-test.json` (both hold component types 1, 29, 380) and asserts `Content.Count == 1` (`tests/PowerOps.Cli.Tests/McpCommandTests.cs:259`, drift row at 250); the live-plus-snapshot drift test asserts `Assert.Single(result.Content)` at 349 (and compares `expected.Out` with `Text(result)`, which stays equal). The new type-1 drift note makes these 2. The plan names `DriftCommandTests` and the golden tests but not `McpCommandTests`. The plan also does not say that MCP callers now get each coverage line twice (JSON `notCompared` and a `note:` block). The tool descriptions (`McpCommand.cs:194-243`, golden `tests/golden/mcp-tools.txt`) need no change, but that decision is unstated.
- Suggested fix: add `McpCommandTests` (lines 259 and 349, and any live drift test) to Data rows 3 and 5 as tests to update, and state in the plan that MCP forwarding of `note:` lines is accepted as it is (or that the JSON is the only carrier there).
- Resolution: Fixed (decisions Q14). Rows 3 and 5 say the MCP server forwards the lines as a second block, accepted; `McpCommandTests` line 259 now expects two blocks for `drift`, and row 5 names it with `WritePathsTests`. The live drift test at 331 compares only the JSON and keeps passing.

#### R2. Drift `notCompared` will be non-empty for almost every real solution, which Decision Q1 gives as the reason not to list tables in `diff`
- Area: design coherence (Data row 5, decisions Q1 and Q7)
- Finding: Q1 rejects a static list because "nearly every solution has a table, so ... the list never empty, and an empty list would no longer mean everything was read". Q7 does the opposite for `drift`: any compared snapshot holding a table (type 1) or canvas app (300) gets a line, so `notCompared` is non-empty on almost every run (the two seed snapshots already hold type 1) and `drift` prints a `note:` on stderr every time. A pipeline therefore cannot use the field to tell a complete comparison from a partial one, and the stderr gets permanent noise. The two decisions need one stated rule (for example: drift lists dropped types because they are dropped from the comparison by design, and the field is "what was left out of this input", not a completeness flag), and the docs row must say it.
- Suggested fix: add one sentence to Q7 that reconciles it with Q1 and say in the `docs/COMMANDS.md` drift text that these lines appear on nearly every run. If that is not the intent, keep only the one-environment and layers-not-read lines in `notCompared` and leave the dropped types to the docs.
- Resolution: Fixed (decisions Q7, amended). One rule for both commands: a list holds what the command's own claim covers but this input made it skip; `drift` claims "the components" and gives no item for a missing table, `diff` claims no table content and reports a table's add or remove. The docs and the schema say the type lines appear on most runs.

#### R3. The `kind:` line rule in row 1 is under-specified and can misclassify a topic
- Area: Data row 1, reader design
- Finding: "the first line that starts with `kind:`" does not say whether a line may be indented. Read with leading spaces trimmed, a topic whose first top-level key is `beginDialog:` would take a nested `  kind: OnRecognizedIntent` as its kind and be listed as an unread component, adding a false "not read as tools" note for a standard agent (the existing tests' `Topic` constant puts `kind:` first, so they would not catch it). The row also does not say how the three cases that already parse (text holds `TaskDialog`) take their kind: the first-line rule or `YamlText(yaml, "kind")` (a topic that mentions `TaskDialog` and whose YAML kind is absent would be listed with a null kind, or not). It also skips a type-9 component with no `data` file (`SolutionParser.cs:208-210`), so that one is never listed. Comments, a BOM or a `---` line before `kind:` are not covered.
- Suggested fix: write the rule as: a line that begins at column 0 with `kind:` (after an optional BOM), skipping blank and `#` lines, not the YAML; define the kind for parsed data as the YAML top-level `kind` (null when absent); state that a component with no `data` file is not listed; add tests for an indented nested `kind:`, a BOM and a leading comment.
- Resolution: Fixed (decisions Q3, amended). Row 1 gives the column-0 rule, the parsed case (YAML top-level `kind`, null when absent), the byte order mark (dropped by `BoundedRead`) and the component with no `data` (skipped, not listed); tests cover a nested `kind:` first, a comment and `---` first, a byte order mark, and only a nested `kind:`.

#### R4. Hostile or long text in the new lines has no hygiene tests, and the agent name is not cut
- Area: Data rows 2, 3, 4, 5, tests
- Finding: The new lines carry solution-controlled text (agent schema name, component kind, drift environment name, solution path). `src/PowerOps.Cli.Tests/OutputHygieneCommandTests.cs` (lines 89-120) and `tests/PowerOps.Core.Tests/Output/OutputHygieneTests.cs` (175-215) test exactly this for the existing notes (one clean line, no forged `::error::`, caps at 20 then a count), and `action/powerops.sh` and `action/diff.sh` forward only `^(error|note):` lines to the log, so one line per message is a security property. The plan routes stderr through `TextReport.Clean` and Markdown through escaping but adds no hygiene test, and it cuts the kind to 100 characters but not the agent name. It does not say whether the new stderr lines are capped as `WriteNotChecked` caps at 20 (a solution with hundreds of agents prints hundreds of lines), and row 3 says "escaped like a cell, not cut" while `DiffReport.Escape` (private, `DiffReport.cs:104`) cuts at 200: the implementer must call `MarkdownReport.Escape` directly.
- Suggested fix: add to the tests a hostile agent name and kind (line break, `::error::`, `@`, `<`) for `diff` (stderr, Markdown, JSON) and a hostile environment name for `drift`; cut agent names with `SolutionDiff.Truncate` too; say the new `note:` lines are capped at 20 with a count line (as analyze) or are not capped and why; name `MarkdownReport.Escape` in row 3 and row 5.
- Resolution: Fixed (decisions Q13). The agent name is cut to 100 characters; stderr goes through a new `TextReport.WriteNotCompared` (cleaned, at most 20, then a count); Markdown uses `MarkdownReport.Escape` and stops at a whole line within the 60,000 limit; `OutputHygieneTests` checks a line break, `::error::` and a bidirectional control on stderr and `@`, `<`, `|` and `#` in the `diff` and `drift` Markdown.

#### R5. Drift: "layers not read" is already an item, and the "compared pair" wording is loose
- Area: Data row 5
- Finding: `DriftCompare.Compare` already emits `layers-not-read` for every installed managed snapshot with `LayersRead == false` (`DriftCompare.cs:63-66`; `DriftKind.IsDrift` marks it information), and the text report already prints "layers not read" (`DriftReport.cs:31`). Row 5's third kind of line has the same condition, so the JSON would hold the same fact in `items` and in `notCompared`. That may be intended (the joint plan names it), but the plan does not say so. Also "an installed snapshot of a compared pair" should say what a pair is: `ComparePair` returns before comparing anything when `Installed` differs (`DriftCompare.cs:90-94`), so a type line is right only when both snapshots of a pair are installed; and with one installed snapshot plus one not installed, the "only one environment has the solution installed, so nothing was compared" line sits beside a `solution-missing` drift item and an exit code `1`. Last, the `DriftReport` writers share one signature (`DriftReport.cs:23`, tests call them in `DriftReportTests.cs:16,55`): the row does not say whether `NotCompared` is passed in or computed inside from the snapshots. Naming 10087 "app setting" states as fact a code the source itself flags as org-dependent (`DriftCompare.cs:116`).
- Suggested fix: say that the layers line repeats the `layers-not-read` item on purpose (or drop it from `notCompared`); define a compared pair as both snapshots installed; adjust the one-environment wording for the not-installed case; say the writers compute `notCompared` from the snapshots (no signature change) or list the signature change in the tests; word 10087 as "component type 10087".
- Resolution: Fixed (decisions Q7, amended). Row 5 defines a pair and a compared pair, splits the one-environment line (one snapshot; no pair installed in both), says the layers line repeats its item on purpose, keeps the writers' signature (they compute the list from their snapshots) and names 10087 by number only.

### Low

#### R6. GitHub's SARIF page does use `invocations[0]`; the Assumptions sentence is not exact
- Area: Assumptions (SARIF and GitHub)
- Finding: The Assumptions say code scanning's listed properties "don't include invocations". The page the plan cites does list `runs[].invocations[0].workingDirectory.uri` (used as the source root when no checkout path is given). The design is still safe, because the new invocation sets only `executionSuccessful` and `toolExecutionNotifications` and no `workingDirectory`, but the statement should be true. I checked the design against the vendored schema: `invocation` requires only `executionSuccessful`, `toolExecutionNotifications` is an array of `notification` (only `message` required) with `locations` of `location`, and an empty array is valid (the SARIF schema parts read with a script; no validator was run on a full document).
- Suggested fix: reword to "the only invocation property it reads is `workingDirectory.uri`, which the report does not write", and add to the SARIF test that no `workingDirectory` is written.
- Resolution: Fixed. Assumptions say the one invocation property GitHub reads is `workingDirectory.uri`, which the report doesn't write; `SarifReportTests` checks it is absent.

#### R7. Notification location for a solution at `.` or an absolute path
- Area: Data row 4
- Finding: Findings use `f.File` as the URI when the solution path is `.` (`SarifReport.cs:35`). The notification uses "its path", so a solution at the current folder gets the URI `.`, and a path outside the base is an absolute path (a Windows drive letter becomes `C%3A`). Both are valid SARIF but unhelpful, and the plan has no case for them.
- Suggested fix: for `.` omit `locations` (or use the solution file's path), and add a case to `SarifReportTests`.
- Resolution: Fixed. A solution at `.` gets no location; `SarifReportTests` has the case. An absolute path stays as it is, as for findings.

#### R8. Smaller wording and test-helper gaps
- Area: Data rows 2, 3, 4
- Finding: (a) Row 2: a component whose `data` path is in both versions with different kinds has no stated kind for the count; "matched as the `agent` block matches" does not say that duplicate schema names keep the first (`SolutionDiff.cs:146`). (b) Row 3 (and Done when 1): "the same two strings" in JSON and stderr: say whether stderr lines carry any prefix other than `note: `. (c) Row 4: JSON `notChecked` holds all lines, while stderr caps at 20 (`TextReport.cs:55-70`); "the lines it prints as notes" is not exact. (d) The agent synthetic builder (`SeedWithAgent`, `DiffCommandTests.cs:150`) is private to that class, and SARIF schema validation is a private loader in `SarifReportTests.cs:20`; the plan's `AnalyzeCommandTests` and `DiffCommandTests` cases need either a shared helper in `tests/Shared` or a different home (Core tests can build `Solution` records directly with `B.Empty()`). (e) Row 7 should say what `diff` does compare for an agent (instructions and tools only, not authentication mode, recognizer or settings, which the reader does read): the same "silent partial read" the milestone fixes. (f) Markdown lists are unbounded; the limit logic (`DiffReport.cs:67-79`) counts them against `maxChars` but cannot shorten them.
- Suggested fix: add one sentence or test case for each of (a) to (f).
- Resolution: Fixed. (a) Row 2: a file in both versions counts with the old version's kind; duplicate names keep the first. (b) Each stderr line is `note: ` and the item. (c) Rows 3 and 4: JSON holds every line, stderr at most 20. (d) The schema check is in `SarifReportTests`, `AnalyzeCommandTests` checks the shape, and the new-layout builder sits beside `SeedWithAgent` in `DiffCommandTests`. (e) Row 7 and the docs say an agent's authentication mode, recognizer and settings are not compared. (f) The `diff` Markdown list stops within the limit; `drift`'s list is a few lines, and `analyze`'s Markdown has no limit today.

#### R9. A URL in the plan
- Area: URL, GUID, email and name scan
- Finding: `plan.md` line 79 holds one public Microsoft Learn URL (written without a scheme). No GUID, email, person's name, tenant, environment or agent schema name was found in `plan.md` or `decisions.md`.
- Suggested fix: replace it with the page title only (the Learn page is named in the Log).
- Resolution: Fixed. The URL is replaced by the page's title.

## Checked and found sound

- Every cited location exists at `b8ad904` and says what the plan says: `SolutionParser.cs:213` is the `type == 9 && !text.Contains("TaskDialog")` line; `ReadAgents` runs 163-252; `Solution.cs:93-104` is the `Agent` record; `SolutionDiff.cs:16,45-101`; `DiffCommand.cs:66-93`; `DiffReport.cs:41-85,128`; `DriftCompare.cs:38-125`; `DriftReport.cs:47-88`; `DriftCommand.cs:137-163`; `JsonReport.cs:22-46`; `SarifReport.cs:25-69`; `MarkdownReport.cs:10-29`; `docs/COMMANDS.md` lines 37, 49, 67-71.
- The live-export shape: unzipped to a scratch folder, v1 has one agent, no type-15 component, six `IQCapability` and two `McpTool` type-9 components (none holding `TaskDialog`) with the right parent, and `configuration.json` holds `CLICopilotRecognizer` and the instructions under `agentSettings`; v2 differs from v1 only in that file's instruction text. So `diff` of the exports reads no instructions and no tools, as the plan says.
- `Baseline.Apply` uses `s with { Findings = kept }`, so `NotChecked` survives a baseline (`Baseline.cs:86-96`).
- The type-9 `data` is already read for every component (`SolutionParser.cs:212`), so the `kind:` scan adds no read; `UnreadFiles` and `OtherComponents` do not overlap as the row defines them, and topics still add no refusals.
- The Action scripts already forward `note:` lines (`action/diff.sh`, `action/powerops.sh`) and the Markdown goes to the job summary and PR comment, so `action/` needs no change; `annotate.sh` reads only `.findings`. `deploy plan` does not use `SolutionDiff` or `DriftCompare` (it has its own `components-not-checked`), so it needs no change. The pinned roles and `tests/eval` are untouched by these changes, and the new JSON fields do not alter their inputs.
- Existing `diff` tests that assert an empty stderr (`DiffCommandTests.cs:45,86,167`) use standard-shape agents and fixtures, so they stay empty; the drift tests that assert `""` stderr are the ones the plan already says it updates. The `analyze-fixtures` goldens change because `alm-sample` has an existing note (checked by running `analyze` on `tests/fixtures`).
- Schema rules: adding a field keeps `schemaVersion` (`docs/schemas/README.md`), fields written every time can be `required`, and `OutputSchemaTests` validates every golden. Decision Q8 matches that page.
- The SARIF choice (`invocations[].toolExecutionNotifications`, `level: note`, `executionSuccessful: true`) is valid against the vendored schema's definitions; `locations` items are unique per notification.
- Constraints: smallest change, new logic with tests, no new write site, no version or changelog edit, no skill text change, and the PR targets Windows and Linux. Open questions: 0.

## Counts

First pass: High 0, Medium 5, Low 4 (R1 to R9, all answered; see the second pass). Open now: see "Second pass". Open questions in the plan: 0.

## Second pass

Run against the worktree's working tree on top of `b8ad904` (plan, decisions Round 2, code, tests, goldens and docs), with the `plan-hash` line now naming the current plan. Nothing was run in the worktree: I built a scratch copy outside the repo (`git archive b8ad904` plus `git diff b8ad904`), where `PowerOps.Core.Tests` (2,215 tests, 2 skipped) and `PowerOps.Cli.Tests` (1,282 tests, 3 skipped) pass. The worktree's `DriftCompare.cs` has since lost one blank line against my copy, which changes nothing else. No `pac`, `az`, tenant or GitHub call.

### Resolutions of R1 to R9

All hold in the plan, the code and the docs. R1 cited line 349 as a drift test; that line is the `deploy_plan` test, and only the `drift` row at 259 needed the change (now expects two blocks for `analyze` and `drift`); the live drift test compares only the JSON.
- R1: `McpCommandTests` line 259 updated; the decision (Q14) and row 5 say the `note:` block is accepted.
- R2: Q7 and Q1 now follow one stated rule; `docs/COMMANDS.md` and `drift.schema.json` say the type lines appear on most runs.
- R3: `TopKind()` is `^kind:(.*?)\r?$` with `Multiline`, so only column 0 counts and CRLF text works; a data holding `TaskDialog` takes the YAML top-level `kind`; a component with no `data` file is skipped. Mutants: allowing an indented `kind:` fails 3 tests; dropping `AgentDialog` fails 1; dropping `AdaptiveDialog` fails 5; removing the quote trim fails 1; skipping the parsed-kind branch fails 1.
- R4: `TextReport.WriteNotCompared` cleans and caps at 20 (mutants: no cap and a wrong cap threshold each fail 1; no `Clean` fails 1); Markdown uses `MarkdownReport.Escape` (mutants: no escape fails 2 and 1 for diff and drift) and stops at a whole line with a count (no cut, no count line and no reserve each fail 1); the agent name and the kind are cut (each fails 1). I ran the built CLI on a re-zipped copy of the live export whose agent name held a line break, `::error::`, `@`, `#`, `<`, `|` and a bidirectional control: stderr was two clean `note:` lines, Markdown one escaped line each, JSON escaped.
- R5: `NotCompared(snapshots)` defines the pair as the first snapshot with another installed one; `ComparePair`'s filter and the notes share the `Dropped` array; the writers compute the list themselves (signature unchanged); 10087 is named by number. Mutants: ignoring whether the first is installed fails 1; a pair threshold of 1 fails 1; dropping 10087 fails 3; dropping the 380/381 exclusion fails 2; layers line without `Managed` fails 1.
- R6: `SarifReportTests` asserts `workingDirectory` is absent; the plan's Assumptions sentence is exact.
- R7: a solution at `.` gets no location (mutant fails 1); an unencoded path fails 1; `executionSuccessful` false fails 2.
- R8: old version's kind wins (case-insensitive `DistinctBy`, mutants fail 2 and 1); the instructions side wording and the "neither" wording fail 1 and 2 when changed; `COMMANDS.md` says what `diff` compares for an agent. Exception: the duplicate-name rule and the agent order are not tested (R10).
- R9: the Learn URL is gone; no URL, GUID, email, name or live-export name is left in `plan.md` or `decisions.md`.

### Code against the plan

What I checked and found sound:
- Row 1 to 7 are in the code as written: `OtherComponents` and `AgentComponent` on `Agent`; `DiffResult.NotCompared` filled by `Compare`, JSON between `changes` and `summary`; Markdown list after the summary and before the legend and table; `analyze` JSON `notChecked`, SARIF `invocations[0]` (always written, empty list valid), Markdown `Not checked:` after the table or "No findings."; `drift` JSON `notCompared` after `items`, Markdown list after the summary; the three schemas gain a required array; `docs/COMMANDS.md` covers the three commands.
- A baseline keeps `NotChecked` (`s with { Findings = kept }`); the CLI test covers it.
- Hostile input: stderr lines start with `note: ` and pass `TextReport.Clean`, so a forged `::error::` sits mid-line and the Action's `^(error|note):` filter forwards one line; the MCP server forwards only lines that start with `note:`. SARIF and JSON messages are escaped by the serializer.
- AGENTS.md: no new write site (`WriteSitesTests` unchanged and passing), no version or changelog edit, no skill, `tests/eval` or pinned-role change, goldens change only by the new fields and lines, new logic comes with tests (the mutants above show they bite). The log says the reader and diff tests were written beside the code, not first; the mutation run shows that did not leave the logic unguarded.
- Equivalent survivors (no behavior difference): a `KnownKind` without `"TaskDialog"` (a text without that word can't have it as a kind, and the parsed branch takes tools first); taking dropped types from all snapshots instead of the compared ones, and the layers line without `Installed: true` (a not-installed snapshot holds no components and no managed flag); the `OrderBy` of `OtherComponents` (the reader's path order already gives it, and the counts don't depend on it).

### Findings

#### High

No findings.

#### Medium

No findings.

#### Low

##### R10. The duplicate-name rule and the order of agents in `notCompared` are not tested
- Area: Data row 2, tests
- Finding: Row 2 says the first of a duplicate schema name is used and that agents come in ordinal order ignoring case. Mutants that use the last duplicate (`LastOrDefault`) and that drop the `Order(OrdinalIgnoreCase)` of the agent names both leave all 2,213 Core tests passing. (A case-sensitive match is caught.)
- Suggested fix: add to `SolutionDiffTests` one case with two agents of the same name (different spelling and different components, only the first counted) and one with two agents whose names sort differently than they were listed.
- Resolution: Fixed. `SolutionDiffTests.Agents_are_listed_in_name_order_and_a_duplicate_name_uses_the_first`: two agents of one name in two spellings with different components (only the first's counted, its spelling shown) and an agent listed last that sorts first; it fails with `LastOrDefault` and without the name order.

##### R11. A trailing YAML comment or an anchor on the `kind:` line becomes part of the kind
- Area: Data row 1
- Finding: `TopKind()` keeps everything after `kind:` to the end of the line, then trims spaces and quotes. `kind: AdaptiveDialog # a topic` is read as the kind `AdaptiveDialog # a topic`, which is not a known kind, so the topic is listed as an unread component (a false "not read as tools" note). I confirmed the odd text with a re-zipped copy of the live export: `kind: "X"  # c` printed as `X"  # c`. No exported file seen has such a line, so the effect is a false note, never a missed component; it is not tested.
- Suggested fix: cut the value at ` #` (a `#` after white space) before trimming, or say in row 1 that a comment on the `kind:` line is not handled and add a test that pins the behavior.
- Resolution: Fixed in the code (decisions Round 3). The `kind:` pattern stops at a `#` that follows white space, so a trailing comment is not part of the kind; `AgentReaderTests` has a topic with a trailing comment (not listed) and an `IQCapability` line with one (listed as `IQCapability`). An anchor on the line is still not handled, which can only give a false note; no export seen has one.

##### R12. Skipped-link notes stay on stderr only, and the docs sentence can be read as covering them
- Area: Goal, `docs/COMMANDS.md` analyze text
- Finding: `analyze` and `diff` also print `note: <link>: a link inside the solution, not followed and not read` (`TextReport.WriteNotes`), which is a "not read" fact like the ones M50 moves into JSON, SARIF and Markdown. It is not in `notChecked`, `notCompared` or SARIF, and the plan does not list it in Won't build. The new `COMMANDS.md` sentence "All of these lines are also in the JSON as `notChecked`..." follows the paragraph that mentions notes in general and could be read to include the link notes.
- Suggested fix: add one line to Won't build (links not read stay stderr-only, as before) and start the docs sentence with "The `note:` lines of rules that did not check something (not the notes about links)".
- Resolution: Fixed (decisions Round 3). The `docs/COMMANDS.md` sentence now starts "The `note:` lines of rules that did not check something, not the notes about links"; the link notes staying on stderr only is recorded as not built.

## Counts

Open now: High 0, Medium 0, Low 3 (R10, R11, R12). First pass R1 to R9: all resolved and re-checked above. Open questions in the plan: 0.
