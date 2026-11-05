---
status: done
review: done
environment: null
depth: quick
supersedes: null
signed-off: 2026-11-04
signed-off-hash: a9304686fb673ab73f09f06722ec160236b63cbbfb2807cbbf743e427f3ad772
---

# M50: Coverage is explicit in every output

## Goal

Say in every output of `diff`, `analyze` and `drift` what was not compared or not checked, so that "no changes" or "no findings" can't stand for "not read". This is M50 of the improvement plan written after the live test of 2026-11-04, kept outside the repo because it quotes a private test run. The test exported an agent made in Copilot Studio's new authoring UI twice, with one sentence added to its instructions, and `powerops diff` said "no changes", exit 0, `changes: []`, with no `note:` line (finding F6). The same happened with a copy whose MCP tool folder was removed. The reader takes instructions only from a Custom GPT component and tools only from `kind: TaskDialog` data (`src/PowerOps.Core/Readers/SolutionParser.cs:213`), and this agent has neither: its instructions are in `configuration.json` and its tools are `kind: McpTool`. `analyze` prints a `note:` for the agent, but only on stderr: its JSON and SARIF carry no coverage (review finding CX-1), and the `diff` docs name no limit (C-3). Reading the new layout is M51; this milestone makes the gap visible.

Done when:

1. `powerops diff` of the live test's v1 and v2 exports, in both orders, and of v1 against a copy without the MCP tool's folder, prints `note:` lines on stderr naming the agent and the reason, and still prints "no changes" and exits 0; its JSON has a non-empty `notCompared`; its Markdown lists the same lines. A synthetic agent of the same shape in `DiffCommandTests` checks this in CI.
2. `powerops analyze` JSON has `notChecked` on each solution with the lines it prints as notes (for the live export, the PO027 to PO031 harness line); SARIF carries the same lines as `toolExecutionNotifications` of `level` `note`, and validates against the vendored SARIF 2.1.0 schema; Markdown lists them under each solution.
3. `powerops drift` JSON has `notCompared`: the component types it leaves out of the comparison when a compared environment holds one, the environments whose layers were not read, and a line when only one environment was read or no environment has the solution installed together with the first. Text prints them as `note:` lines on stderr and Markdown lists them.
4. The schemas of `analyze`, `diff` and `drift` gain the fields with their versions kept; `docs/COMMANDS.md` says what each command lists; the goldens change only by the new fields and lines.
5. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan are clean; the PR is green on all eight checks.

### Won't build

- Reading `configuration.json` instructions or `kind: McpTool` tools: M51. Exit codes don't change; `diff` of the live exports exits 0 until M51 (decisions Q5).
- A static list in `diff` JSON of the component kinds it never compares (tables, forms, topics and so on): the docs say that, and the list holds only what these two exports have that `diff` meant to compare and couldn't read (decisions Q1).
- New `analyze` notes for a standard-harness agent with components of other kinds: no such agent was seen; M51 reads the layout (decisions Q10).
- A committed fixture made from the live exports: M51 owns the new-layout fixture (C-4). The tests build a synthetic agent of the same shape (decisions Q9).
- Coverage lines in the text reports on stdout: they go to stderr as `note:` lines, as `analyze`'s do (decisions Q5).
- A `docs/BUILD-PLAN.md` section for M50 to M68 (decisions Q12).

## Users

- A reviewer who reads a `diff` comment on a pull request and must not take "no changes" for an agent that was not read.
- A pipeline or agent that reads the JSON or SARIF and has to tell a complete check from a partial one.
- A person who compares environments with `drift` and needs to know which component types the comparison left out.

## Data

Line numbers are those of `b8ad904`.

| # | Severity | Change | Where | Test |
|---|---|---|---|---|
| 1 | medium | `Agent` gains `IReadOnlyList<AgentComponent> OtherComponents` (default empty, like `UnreadFiles`), with `record AgentComponent(string? Kind, string File)`: the type-9 components of the agent whose top-level kind is not `TaskDialog`, `AdaptiveDialog` (a topic) or `AgentDialog` (a child agent), in ordinal order of `File` (their `data`). For a `data` whose text doesn't hold `TaskDialog`, the reader takes the kind from the first line anywhere in the text that begins at column 0 with `kind:` (so an indented, nested `kind:` never counts, and blank, comment and `---` lines before it are passed over), with spaces and quotes trimmed and null when there is no such line or its value is empty, without parsing the YAML, so topics still add no refusals; the byte order mark is already dropped when the file is read (`BoundedRead`). A `data` whose text holds `TaskDialog` is parsed as today, and when its YAML top-level `kind` (null when absent) is none of the three it is listed with that kind. A type-9 component with no `data` file is skipped as today and not listed: there is nothing to read. `Tools` and `UnreadFiles` are unchanged | `src/PowerOps.Core/Model/Solution.cs:93-104`, `src/PowerOps.Core/Readers/SolutionParser.cs:163-251` | `AgentReaderTests`: an `McpTool` and an `IQCapability` component are listed with their kind and are not tools; `AdaptiveDialog` and `AgentDialog` components are not listed; a `data` with no `kind:` line is listed with a null kind and is not parsed (no refusal for malformed YAML); a `TaskDialog`-mentioning `data` of kind `McpTool` is listed; a quoted kind is unquoted; a nested `kind:` before the top-level one, a leading comment and `---`, and a byte order mark each leave a topic unlisted, and a topic with only a nested `kind:` is listed with a null kind; a folder and its zip read the same list |
| 2 | high | `DiffResult` gains `IReadOnlyList<string> NotCompared { get; init; } = []`, filled by `SolutionDiff.Compare`, distinct, in this order: by agent schema name (ordinal, ignoring case, matched as the `agent` block matches: ignoring case, the first of a duplicate name), the name cut to 100 characters with `SolutionDiff.Truncate`, for each agent (a) when it is in both versions and its `Instructions` is null in either: `agent '<name>': instructions not compared: <the old version / the new version / neither version> has <no / a> single Custom GPT component whose instructions are text`; (b) when its `OtherComponents` or `UnreadFiles` in either version are not empty: `agent '<name>': <n> component(s) not read as tools, so a change to them is not compared: <count> '<kind>', ...`, counting each `data` path once across both versions (ignoring case, with the old version's kind when the two differ), the kinds sorted ordinal, a null kind written `with no kind`, an unread file `'TaskDialog' with keys PowerOps can't map`, and each kind cut to 100 characters with `SolutionDiff.Truncate` | `src/PowerOps.Core/Diff/SolutionDiff.cs:16,45-101` | `SolutionDiffTests`: an agent with null instructions on both sides, on one side and on neither; an added agent gets no instructions line but does get a components line; other components counted once across versions (one removed in the new version still counted); an unread file; a null kind; a long kind and a long agent name cut; a standard agent with a GPT component and `TaskDialog` tools gives none |
| 3 | high | `diff`: `DiffCommand` prints `note: <item>` on stderr for each `NotCompared` item through a new `TextReport.WriteNotCompared`, which cleans each line with `TextReport.Clean` and, like `WriteNotChecked`, prints at most 20 and then `note: and <n> more not compared (--format json lists them all)`; after the link notes and before the report, for every format and with `--out`. `DiffReport.WriteJson` writes `notCompared` (array of strings) after `changes`. `WriteMarkdown` writes, when the list is not empty, `**Not compared:**` and one `- <item>` per line, each escaped with `MarkdownReport.Escape` (not cut at 200 characters as a cell is), right after the summary, before the risk legend and the table, so a cut table never drops it; the lines count against the 60,000-character limit, and when they would pass it the list stops at a whole line with `- and <n> more. Use --format json for the full list.` `WriteText` is unchanged. The exit code stays `1` for changes and `0` without | `src/PowerOps.Cli/DiffCommand.cs:66-93`, `src/PowerOps.Core/Output/DiffReport.cs:41-85,128` | `DiffCommandTests.An_unread_agent_is_listed_in_notCompared`: a synthetic pair shaped like the live export (no GPT component, `configuration.json` with `CLICopilotRecognizer` and `agentSettings.instructions`, two `McpTool` and one `IQCapability` component; the new one has another instruction sentence and one `McpTool` removed) exits 0 with "no changes", two `note:` lines on stderr, the same two strings in JSON `notCompared`, and in the Markdown. `DiffReportTests`: the Markdown lines are escaped and sit before the table; with `maxChars` that cuts rows they are still there; a long list is cut at a whole line with its count; JSON has `notCompared` between `changes` and `summary`. `OutputHygieneTests`: a line with a line break, `::error::` and a bidirectional control prints as one cleaned `note:` line, more than 20 lines end in the count line, and a line with `@`, `<`, `|` and `#` stays one escaped Markdown line (for `diff`, and for `drift` with a hostile environment name). Goldens `diff-seed.json` and `diff-agents.json` gain `"notCompared": []`; `.txt` and `.md` goldens unchanged |
| 4 | medium | `analyze`: `JsonReport` writes `notChecked` (array of strings, `SolutionResult.NotChecked`) after `findings` in each solution. `SarifReport` adds to the run `invocations: [{ executionSuccessful: true, toolExecutionNotifications: [...] }]`, always (an empty list means nothing was left unchecked), one notification per solution and line in report order: `level` `note`, `message.text` the line, `locations` the solution's `artifactLocation.uri` (its path, each segment percent-encoded as for findings), left out for a solution at `.`. No `workingDirectory` is written. JSON and SARIF hold every line; stderr still prints at most 20. `MarkdownReport` writes `Not checked:` and one `- <line>` per item under each solution that has any, after its findings table or "No findings." | `src/PowerOps.Core/Output/JsonReport.cs:22-46`, `src/PowerOps.Core/Output/SarifReport.cs:25-69`, `src/PowerOps.Core/Output/MarkdownReport.cs:10-29` | `AnalyzeCommandTests.NotChecked_reaches_json_and_sarif`: a solution with an agent off the standard harness has the PO027 to PO031 line in JSON `notChecked`, in a SARIF notification of `level` `note` with `executionSuccessful` true, and in the Markdown, with and without a baseline. `SarifReportTests` (which holds the schema loader): notes validate against `tests/schemas/sarif-schema-2.1.0.json`, a solution path with a space is encoded, no `region` and no `workingDirectory` are written, a solution at `.` has no location, and no notes gives an empty list that still validates. Goldens `analyze-fixtures.json`, `.sarif` and `.md` |
| 5 | medium | `drift`: `DriftCompare.NotCompared(IReadOnlyList<Snapshot>)` returns, in this order. A pair is the first snapshot and another (`Compare` compares each with the first), and a compared pair is one with the solution installed in both (`ComparePair` stops at `solution-missing` otherwise). With one snapshot: `only one environment was read, so nothing was compared between environments; only its own checks ran`. With several and no compared pair: `no environment has the solution installed together with the first, so only whether it is installed was compared` (beside the `solution-missing` items and exit `1` that already say what differs). Else, for each type that `ComparePair` leaves out by id and compares under no kind of its own (1 table, 62 site map, 80 model-driven app, 300 canvas app, 10087, named by number only because codes above 10000 may differ per org; not 380 and 381, which are compared by schema name as variables) that a snapshot of a compared pair holds: `component type <n> (<name>) not compared: its ids change on import, and drift compares components by id`; the type list is one array that `ComparePair`'s filter uses too. Then, for each installed managed snapshot whose `LayersRead` is false, `<position> <environment>: layers not read, so unmanaged layers there are not checked`, which repeats its `layers-not-read` item on purpose: the items say what differs, this list what was not checked. `DriftReport.WriteJson` writes it as `notCompared` after `items` and `WriteMarkdown` as `**Not compared:**` and `- <item>` lines (escaped with `MarkdownReport.Escape`) after the summary, each computing it from the snapshots it is given, so the writers' shared signature doesn't change; `DriftCommand` prints each item through `TextReport.WriteNotCompared`. `WriteText` is unchanged. The MCP server forwards stderr's `note:` lines as a second text block, so the `drift` and `diff` tools return these lines twice, in the JSON and as notes, as `analyze`'s tool already does for its notes; accepted | `src/PowerOps.Core/Drift/DriftCompare.cs:38-125`, `src/PowerOps.Core/Output/DriftReport.cs:47-88`, `src/PowerOps.Cli/DriftCommand.cs:137-163` | `DriftCompareTests`: one snapshot (and none); no compared pair, with the first installed or not; three with a table, a canvas app and type 10087 (one line each, once across pairs, in type order); two with only types 29, 380 and 381 (no line); a managed snapshot with layers not read, and an unmanaged one that is not listed. `DriftReportTests`: the JSON and Markdown expectations gain the list. `DriftCommandTests`, `WritePathsTests` and `McpCommandTests` (line 259: `drift` now returns two blocks, like `analyze`): the tests that asserted an empty stderr or one block now assert the notes their snapshots give. Goldens `drift-seed.json`, `drift-none.json`, `drift-one-env.json`, `drift-seed.md` |
| 6 | low | Schemas, versions kept (adding a field keeps `schemaVersion`, `docs/schemas/README.md`): `analyze` gains required `notChecked` on each solution; `diff` (still 2) and `drift` gain required `notCompared`; each an array of strings with a description of what it lists | `docs/schemas/analyze.schema.json`, `docs/schemas/diff.schema.json`, `docs/schemas/drift.schema.json` | `OutputSchemaTests` validates every golden |
| 7 | medium | Docs. `docs/COMMANDS.md`: `analyze` says the notes are also in JSON `notChecked`, SARIF `toolExecutionNotifications` and Markdown; `diff` says that for an agent it compares only the instructions and tools, not the authentication mode, recognizer or settings, names the agents whose instructions or tools it can't read (no single Custom GPT component; type-9 components of another kind, such as the new UI's `McpTool`), the `note:` lines (at most 20), JSON `notCompared` and the Markdown lines, and that the exit code does not change; `drift` describes `notCompared` and its lines, and that the type lines appear on most runs | `docs/COMMANDS.md:37,49,67-71` | `DocsDriftTests` (unchanged) |

## Automation

None. The Action's job summary and PR comment show the Markdown, which gains the lines.

## Screens

None.

## Environments and route

None. The build reads and writes no tenant. The acceptance check of Done when 1 and 2 runs the built CLI on the live test's exports, which stay outside the repo; the record in `decisions.md` names no tenant, environment, agent schema name or publisher.

## Solution and publisher

None.

## Constraints

- AGENTS.md: smallest change, new logic with tests, writes through `SafeWrite` (no new write), Conventional Commits, no version or changelog edits, no tenant URLs, ids, emails or user names in commits.
- The pinned roles `powerops-verify` and `powerops-review-security` and `tests/eval` are untouched; no skill text changes.
- Platforms are Windows and Linux.
- M51 extends the reader and the diff after this; M53 and M55 come later. The new fields keep their shape when M51 reads more, so the lists only get shorter.

## Assumptions

- Observed in the live test's exports (2026-11-04): the agent has no type-15 component, its instructions are in `configuration.json` under `agentSettings.instructions`, and its type-9 components are of kind `McpTool` (two) and `IQCapability` (six), none holding `TaskDialog`. Observed in every export and workspace M48 read: tools are `TaskDialog`, topics `AdaptiveDialog` and child agents `AgentDialog` (M48 decisions Q5).
- SARIF 2.1.0 (the vendored schema): `invocation` requires `executionSuccessful`; `toolExecutionNotifications` is "a list of runtime conditions detected by the tool during the analysis", of `notification` objects that require `message` and take `level` `note` and `locations`. GitHub, read 2026-11-04 ("SARIF support for code scanning"): "Any valid SARIF 2.1.0 output file can be uploaded", and code scanning uses only the properties it lists; the one invocation property it lists is `workingDirectory.uri`, which the report doesn't write.
- Microsoft Learn, "Export and import agents using solutions" (read 2026-11-04 with the Learn MCP tools): "Not all agent components and properties are included", and "The agent instructions (main prompt) are included". So the instructions are in the export, and a diff that says nothing about them has not read them; what the export leaves out, no offline command can compare, and the docs say so.

## Open questions

## Questions for your admin

## Steps

1. This plan and `decisions.md`, a fresh-context review into `review.md`, fixes, and sign-off under the delegation of 2026-10-22 once no High or Medium finding is open.
2. Rows 1 to 3, test first; then rows 4 and 5; then row 6 and the goldens by the recipe "Update goldens".
3. Row 7.
4. The acceptance run on the live exports, recorded in `decisions.md`; `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .`, the leak scan; push, PR, CI.
