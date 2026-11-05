plan-hash: 1ae33c167b35361ed356ad1e5ad3354a03011169448a3a24255f9082956dc46a
model: claude-sonnet-5-5
fresh-context: yes

Environment facts were not read: the plan names no environment (`environment: null`) and changes this repo's own code, not a Power Platform solution; `powerops plan facts` and `pac` were not run. Checklist items 1 to 9 and 11 (solution boundaries, connection references, licences, DLP, route, managed state, delegation, security roles, tables and flows) do not apply and are not reported. Items 10 (open questions: 0, nothing under `Questions for your admin`) and the URL/GUID/email/name scan apply: the plan and `decisions.md` hold none. `plan.md` was not edited (`review: pending` stays), because this request allowed no change outside `review.md`.

The review is mostly a check of the plan against the code at `431df9b` (the reader, model, diff, the four agent rules, their tests, `docs/COMMANDS.md`, the diff schema, M50's plan and review). The three fixture zips were already present in the worktree (untracked); with the built CLI, `analyze` of each prints the harness note and the two PO009 warnings the plan expects, `v1` and `v2` differ only in the segment text, and `v1-no-learn-tool` lacks only the Learn tool's folder, so Done when 3 matches. Today `diff` prints the two M50 `note:` lines for them, as the plan says.

## Findings

### High

No findings.

### Medium

#### R1. An `McpTool` whose YAML can't be mapped is skipped by PO027, PO030 and PO031 with no `note:`, and the reader's branch order contradicts Data row 3
- Area: Data rows 3 and 6, decisions Q6 and Q7 (silent false equality in `analyze`, ordering in the reader)
- Finding: In `ReadAgents`, a type-9 component whose `yaml` is null goes to `agent.Unread` (`UnreadFiles`) before the kind is looked at (`SolutionParser.cs:237-240`), and `UnreadFiles` is what PO027's not-checked line names ("'f' is valid YAML whose keys PowerOps can't map ... and PO030 and PO031 don't check it either"). Row 3 says unmappable `McpTool` YAML stays in `OtherComponents` with its kind. That is a different branch than the code has, and the plan does not say how the implementer gets there (parse-then-test-kind, or test the line-read kind first). The consequence: a standard-harness agent whose `McpTool` data can't be mapped is not read by PO027, PO030 or PO031, and no rule prints a note, because only `UnreadFiles` and the harness note feed `NotChecked`; `OtherComponents` shows up only in `diff`'s `notCompared`. Row 6 and the rule docs say that rules read `McpTool` tools but not that a skipped one is silent in `analyze`. If the implementer instead sends it to `UnreadFiles`, PO027's note and the `diff` line say "'TaskDialog' with keys PowerOps can't map", which is wrong for an `McpTool`.
- Suggested fix: write the branch order in row 3 (line-read kind first for data without `TaskDialog`; an unmappable `McpTool` or `IQCapability` goes to `OtherComponents`, never `UnreadFiles`), and either add an `OtherComponents` of kind `McpTool` to PO027's not-checked line for placed agents or state in Q7 and the rule pages that it is silent in `analyze`, with a test of whichever is chosen.
- Resolution: Fixed (decisions Q6, amended). Row 3 gives the order: only `McpTool` and `IQCapability` are parsed among kinds read from the line; an unmappable one goes to `OtherComponents` with its kind, never `UnreadFiles`, with a test. `analyze` stays silent for it, as for other kinds since M50: the new layout is the GitHub Copilot harness's (decisions Q7), so its agent already gets the harness note.

#### R2. Two Work IQ components with the same capability: the second is dropped with no `notCompared` line
- Area: Data rows 4 and 5 (silent false equality)
- Finding: Row 5 matches workloads by `Capability` ignoring case and takes "the first of a duplicate". A second component for the same capability (a maker clears and re-adds a workload, so a new draft component appears beside the old one, or `mail` and `Mail`) is never compared, and nothing lists it. The change of its `access` is reported as "no changes". This is the same kind of gap M50 closed for agents and tools, and it contradicts Done when 1's meaning of an empty `notCompared`.
- Suggested fix: make a duplicate capability in either version a `notCompared` line (agent, capability, count, "only the first by file order is compared"), or key the comparison on `SchemaName` plus capability; add the case to the row 5 test list. Say whether `access` is compared ordinally or ignoring case (the capability key is ignoring case).
- Resolution: Fixed (decisions Q5, amended). Workloads are matched by schema name, as tools are; two components for one workload are two parts, a re-added one shows removed and added, and access is compared ordinal. Row 5's tests include the re-add.

#### R3. Fields of an `McpTool` that the plan reads past or does not name are unreported: `connectorId` and the component's `name` and `description`
- Area: Data rows 3 and 5, Won't build (silent false equality)
- Finding: The observed `McpTool` data holds `authMode`, `connectionReference`, `connectorId` and `operationId`. The plan reads three and drops `connectorId` without saying why; `Assumptions` lists `connectorId` as observed but nothing compares it (the `connectionReference` component compares `connectorId` of the reference, which is a different record). The same components' `botcomponent.xml` carries a `name` and, for the Learn tool, a `description` that Copilot Studio shows the model when it chooses a tool, and the plan does not read or compare them either. A changed tool description or a swapped connector with the same reference name is "no changes" with an empty `notCompared`. The `TaskDialog` reader has the same gap for `modelDescription`, but M51 is the milestone whose acceptance says an empty `notCompared` means the tools were read.
- Suggested fix: either compare `connectorId` (cheap, same row as `operationId`) and the component `name` and `description` in the `agentTool` change, or add them to Won't build and to the `diff` text in `docs/COMMANDS.md` ("a tool's display name and description are not compared"), so the limit is written down.
- Resolution: Fixed (decisions Q12). Won't build and the `diff` docs name `connectorId` (the connection reference's connector is compared under `connectionReference`) and tool names and descriptions, which `diff` has not compared since M49.

#### R4. Agent parts the new layout holds but M51 never reads change silently: the type-18 component and every `agentSettings` key except `instructions`
- Area: Won't build, Data row 9, Users (silent false equality)
- Finding: The fixture's `configuration.json` also holds `agentSettings.model.series`, `agentSettings.web.enableWebSearch` and `settings.WorkIqAddedByDefault`, and the export holds a type-18 component (the feedback setting). `ReadAgents` skips every component type but 9 and 15 (`SolutionParser.cs:213`). Switching the model, turning web search on or changing the feedback setting gives "no changes", `notCompared` empty, exit 0, for the exact layout this milestone says is now read. Won't build says the authentication mode, recognizer, model and "other settings" were not read before either, but for the new layout the model and web search are the settings a reviewer would care about next to the instructions, and the type-18 component is not mentioned in Won't build at all. The existing docs sentence only names authentication mode, recognizer and settings.
- Suggested fix: add the type-18 component and non-9/15 types to Won't build and to the `docs/COMMANDS.md` diff text and the `notCompared` description in `diff.schema.json` ("an empty list means instructions, tools and Work IQ workloads were read, not that the agent is equal"); or compare `model.series` and `enableWebSearch` inside the `agent` change. If left out, record it as a decision with its reason.
- Resolution: Fixed (decisions Q12). Won't build, the `diff` docs and the schema say an empty `notCompared` means the instructions, tools and Work IQ workloads were read, not that the agent is equal, and name the settings and other component types, the type-18 component among them.

#### R5. PO030 would flag an `McpTool` as using maker credentials on the strength of an unverified reading of `authMode`
- Area: Data row 6, Assumptions, decisions Q7
- Finding: `Mode` of an `McpTool` is its `authMode`, and PO030 (a Warning) fires when `Mode` is `Maker`. The only observed value is `Invoker`; the plan's last Assumption says the meaning is "not documented for this file". Rule pages state facts from Learn or observed exports (AGENTS.md, "Add a rule" step 5), and this finding would claim "uses the maker's credentials" from a guess. The row 6 test uses an `McpTool` of `authMode: Maker`, a value nobody has seen.
- Suggested fix: keep `Mode` for the diff (a change in the string is a change either way) but make PO030 read `Mode` of `TaskDialog` tools only until a `Maker` value is observed or documented; or keep it and put the unverified status in `docs/rules/PO030.md` (what it says today is "`connectionProperties.mode`"). Say which in Q7.
- Resolution: Fixed (decisions Q7, amended). No rule change: the open `vscode-copilotstudio` sync doc says `McpTool` is a CLI-only tool type and the Custom GPT metadata is retired for CLI agents, so the layout is the GitHub Copilot harness's, which PO027 to PO031 don't read. No page claims a meaning for `authMode`.

#### R6. The comparison that sets `InstructionsConflict` will report a conflict for the same text because of a trailing line break
- Area: Data row 2, decisions Q3
- Finding: Row 2 sets `InstructionsConflict` when `configuration.json`'s instructions "differ (ordinal)" from the component's. A Custom GPT component's `instructions` is usually a YAML block scalar that keeps a trailing line break, which PO028 already trims for that reason (`TrimEnd('\r', '\n')`, `PO028...cs:22`); segment values from JSON may use `\n` where the YAML text has `\r\n`. Equal text in two forms would be flagged as a conflict on every such agent, and `diff` would add a `notCompared` line that says nothing true. It also isn't stated whether the same normalization applies to the `a.Instructions == b.Instructions` comparison between versions when one version reads from the component and the other from segments.
- Suggested fix: define the equality in row 2 (compare after trimming trailing `\r` and `\n`, and normalizing `\r\n` to `\n`, or say ordinal exact on purpose) and add a test with a block scalar beside equal segments.
- Resolution: Fixed (decisions Q3, amended). The conflict test compares after replacing `\r\n` with `\n` and removing trailing `\n`; row 2 adds the block-scalar test. The comparison between versions stays exact, as stated.

#### R7. The scrub maps random name parts to fixed fake values; the plan doesn't record whether the real exports differed
- Area: Data row 7, Assumptions (fixture soundness)
- Finding: Row 7 replaces "the random parts of the agent's and the Work IQ draft's schema names" with fake values. Tool identity in `diff` is the component's schema name, and the Work IQ tool's name carries a draft id that differs from the agent's. If those parts differed between the live test's two exports, the real `diff` would show the Work IQ tool as removed and added (with the `agent-tool` risk) while the scrubbed zips, which use one fake value in both, show nothing. The plan says "v2 differs from v1 only in the segment's text" but does not say that the schema names were identical before the scrub, and Done when 1 (an exit `1` with only "instructions changed") is true of the fixtures regardless.
- Suggested fix: add to Assumptions whether the draft and agent ids were identical in both real exports (the maintainer can check the kept originals); if they differ, say so and decide whether the Work IQ tool should be keyed some other way.
- Resolution: Fixed (decisions Q8, amended). Assumptions: the two real exports hold the same paths, so the same schema names, and the scrub maps each real value to one fake value.

#### R8. Existing tests that the change turns red are not all named in the plan
- Area: Data rows 3, 6, 8; Steps (tests and callers)
- Finding: Row 8 handles one DiffCommandTests test, but these existing assertions also change: `AgentReaderTests.Components_of_another_kind_are_listed_with_their_kind_in_file_order_and_are_not_tools` (about `AgentReaderTests.cs:240-262`) expects `McpTool` (including the quoted one that holds `TaskDialog`) and `IQCapability` in `OtherComponents` and `Tools` to hold only the `TaskDialog`; `WarningRuleTests.cs:1570` asserts PO028's old wording; and any `SolutionDiffTests`/`DiffCommandTests` assertion of the old instructions line ("... single Custom GPT component whose instructions are text", `DiffCommandTests.cs:192`). Row 5 says "the M50 tests that use other kinds stay", which is true only of tests that use a kind the reader still doesn't know. `B.Agent` in `tests/PowerOps.Core.Tests/Rules/Builders.cs:95` has no way to set configuration instructions, capabilities or tool fields, which the row 6 tests need.
- Suggested fix: list those tests under the rows as rewritten (not new), and name the `B.Agent`/`B.Tool` change.
- Resolution: Fixed. Row 8 lists the rewritten tests (`AgentReaderTests` components test, the M50 `DiffCommandTests` test, the old wording in `SolutionDiffTests`); with no rule change (Q7), `WarningRuleTests` and `Builders.cs` stay as they are.

### Low

#### R9. Mutation test corpus changes
- Area: Data rows 7 and 3 (callers and tests)
- Finding: `MutationTests` copies all of `tests/fixtures`, picks files with a fixed seed and expects only `InvalidDataException` with the path. Three more zips, plus the new JSON and YAML reads (segments, `McpTool`, `IQCapability`), reshuffle which files the 1,000 mutants hit. A new refusal or an unhandled exception in the new code (a mutated segment value, a mutated `McpTool` file) would fail there, and the plan lists the test nowhere.
- Suggested fix: name `MutationTests` in Steps 5 as expected to run over the new zips, with any new path-less message fixed in the reader.
- Resolution: Fixed. Done when 5 names `MutationTests` as running over the new zips.

#### R10. A workload change carries no risk label
- Area: decisions Q5
- Finding: Adding a Work IQ workload with `Write` access widens what the agent reaches, but the change has no risk label, so a consumer that gates on risk labels sees nothing (exit `1` only). Q5 chose this to keep `schemaVersion` at 2 and says so.
- Suggested fix: say in `docs/COMMANDS.md` that workload changes carry no risk label, and note it as input for M53 in decisions.
- Resolution: Fixed (decisions Q5, amended). The docs say a workload change carries no risk label, left for M53 to weigh.

#### R11. Wording gaps in the diff text for workloads
- Area: Data row 5
- Finding: Only the capability is cut to 100 characters, not the access (the existing `Show` helper truncates). A workload whose component is in `OtherComponents` (no text `access`) on one side shows as `(none)` on that side, which reads as "cleared" while the `notCompared` line says it wasn't read. The conflict line wording "differ from the Custom GPT component's or can't be read" does not tell which of the two applies.
- Suggested fix: use `Show` for both values; say in row 5 that an unread side is reported through `notCompared` only (or add a test of the combination).
- Resolution: Fixed (decisions Q5, amended). Both sides go through `Show`, the schema name through `Truncate`; an unread side shows `(none)` and is in the components line, as row 5 says.

#### R12. Leak check in the acceptance names no patterns
- Area: Done when 5, Data row 7
- Finding: "a search of the zips for the real export's names and ids" can't be run from the repo, since the real values aren't kept there. The fixtures hold fake-looking values (`cr123`, zero GUIDs, a generic publisher), which suggests the scrub was sound, but the acceptance should say what the search is: any GUID other than the fake ones, any host name or `@`, any user or owner field, the real prefix and ids from the kept originals (listed in the maintainer's script, not in the repo).
- Suggested fix: write the checks as patterns (GUID regex with an allow-list of the fake values, `dynamics.com`, `.crm`, `@`) and as a diff of the unzipped fixture against the kept original with the scrub map applied.
- Resolution: Fixed (decisions Q8, amended). Done when 5 lists the patterns and the comparison with the kept originals after the scrub map.

#### R13. The new rule reading of `configuration.json` instructions for standard-harness agents rests on no observation
- Area: Data row 6, decisions Q1 and Q7
- Finding: Q1 says the standard harness reads the Custom GPT component; Q7 then has PO028 check `configuration.json` instructions for a placed agent with no such component, an input never seen for that harness (the plan's Assumptions are all from the new-UI export). It changes no existing finding but could produce a PO028 for text the runtime doesn't read. `InstructionsConflict` is also surfaced only by `diff`; `analyze` says nothing when both sources exist and differ.
- Suggested fix: state in Q7 that the rule follows the model on purpose and why, or limit PO028 to the component until an export shows the standard harness using segments.
- Resolution: Reopened (round 2): the resolution says PO028 doesn't change, but the rule reads `Agent.Instructions` (`PO028AgentInstructionsLength.cs`), which row 2 now fills from `configuration.json` for any agent with no Custom GPT component, so for a placed agent PO028's behaviour does change; see R15 below, which covers the rule side. (Medium) Fixed in round 3 with R15 (decisions Q7, amended again): PO028 reads through `Agents.Instructions`, which drops instructions from `configuration.json`, so its findings, page and not-checked line stay as they are.

#### R14. `diff` will print a connection reference name that, in Studio-named references, embeds a connection id
- Area: Data row 5 (privacy)
- Finding: The `agentTool` detail will show `connectionReference: <old> → <new>`. In the fixture the logical name ends in a connection id (made fake by the scrub). The `connectionReference` component of `diff` already prints logical names, so this adds no new class of value, but the PR comment now repeats it, and the environment-variable rule ("never printed") shows the project cares about this.
- Suggested fix: add one line to `docs/COMMANDS.md` saying the reference's logical name is printed, or print only that it changed.
- Resolution: Fixed. Row 9: the `diff` docs say an `McpTool`'s connection reference logical name is printed, as `connectionReference` changes already print it.

## Round 2 (re-review of the revised plan)

Environment facts were not read (`environment: null`, and `pac` and `powerops plan facts` were not run). Re-checked: every resolution above against the revised `plan.md` and `decisions.md` (R1 to R12, R14 hold; R13 is reopened), then the plan against `SolutionParser.ReadAgents` and `ReadAgentConfiguration`, `Solution.cs`, `SolutionDiff.cs`, `Agents.cs`, PO027 and PO028, the tests the rows name, `MutationTests`, `docs/COMMANDS.md`, `docs/rules/PO028.md` and the diff schema, and the three fixture zips (unzipped to a scratch folder: stored entries, only the segment text differs between `v1` and `v2`, only the Learn tool's folder between `v1` and `v1-no-learn-tool`, no GUID or 32-digit id but the two fake ones, no `@`, `dynamics.com` or `.crm`). No URL, GUID, email or person's name is in `plan.md` or `decisions.md`. The planning chat was not read.

### High

No findings.

### Medium

#### R15. Row 6 says the rules don't change, but PO028 and the other rules read the model that rows 2 and 3 change, and no row, test or doc page names the new behaviour
- Area: Data rows 2, 3 and 6, decisions Q7 (reopens R13)
- Finding: `Agents.InStandardHarness` places an agent whose `configuration.json` has no `recognizer` or a `GenerativeAIRecognizer` one. Such an agent with no type-15 component used to have `Instructions` null, so PO028 printed "not checked ... no single Custom GPT component". After row 2 its `Instructions` come from `agentSettings.instructions.segments`, so PO028 now fires on them: "has instructions of N characters" for a long text, and, with generative orchestration on and `segments: []`, "has no instructions" (an agent that has instructions in another source PowerOps doesn't read, or none). The finding's file is `configuration.json`. `docs/rules/PO028.md` still says the rule reads "the agent's Custom GPT component", and its not-checked line ("it has no single Custom GPT component whose instructions are text", `WarningRuleTests.cs:1570`) is now incomplete: it is also printed for unread segments or when the component's text is not text. The same model change makes PO027 (Error severity) and PO030 read an `McpTool` of a placed agent (Q7 admits this and defers it). Row 6 says "no change ... the existing rule tests stay green", Q7 says "no rule change", and the Won't build list says "New rule behaviour for the new layout"; an implementer who follows row 2 ships rule behaviour that the plan says it does not ship, with no test and a page that is wrong.
- Suggested fix: pick one and write it in row 6 and Q7. Either (a) keep the rules on the old layout: PO028 reads `Instructions` only when `InstructionsFile` is not `configuration.json` (and `Agents.Tools` skips `Mcp`-true tools with a null `Action`, or PO027, PO030 and PO031 skip tools that are `McpTool`), with a test for each; or (b) let the rules read the model on purpose, and then list the changed rule behaviour as rows: PO028's page and not-checked wording, tests with `B.Agent(recognizer: null, instructions: ...)` for a segment-sourced text, and a decision on `segments: []` giving "has no instructions". Option (a) is the smaller change and matches Q7's reason.
- Resolution: Fixed (decisions Q7, amended again). Option (a): row 6 keeps the rules on today's layout in code, with `AgentTool.Kind` and `Agent.InstructionsFromConfiguration` (rows 2 and 3) and `Agents.Tools` and a new `Agents.Instructions` that PO027, PO028, PO030 and PO031 read through; tests for a placed agent with an `McpTool` and with instructions only in `configuration.json`; the pages and lines stay true.

### Low

#### R16. Row 3's branch order gives two homes to an unmappable `McpTool` whose text holds `TaskDialog`
- Area: Data row 3, decisions Q6
- Finding: Row 3 says a parsed `data` whose keys can't be mapped goes to `OtherComponents` "when it has a line kind (`McpTool`, `IQCapability`)" and "to `UnreadFiles` as today when its text holds `TaskDialog`". For a data that holds `TaskDialog` (a description that mentions it, as the existing test with `note: TaskDialog` does) and has a line kind of `McpTool`, both clauses apply; Q6 says an `McpTool` is never sent to `UnreadFiles`, whose `diff` line and PO027 note name `TaskDialog`. No test covers the combination.
- Suggested fix: say which clause wins (the YAML or line kind first, `TaskDialog` text only when neither is `McpTool` or `IQCapability`) and add the case to row 3's test list.
- Resolution: Fixed (decisions Q6, amended again). Row 3 takes the line kind of every type-9 `data`; an unmappable one of line kind `McpTool` or `IQCapability` goes to `OtherComponents` even when its text holds `TaskDialog`, with a test.

#### R17. Q8 says the fixture comparison "must leave only the icon", but the scrub list has more than the icon
- Area: decisions Q8, Done when 5, Data row 7
- Finding: Row 7 and NOTICE list the scrub as prefix, random name parts, two connection ids, the publisher display name and the icon. Q8 says that comparing the unzipped fixtures with the kept originals after the scrub map "must leave only the icon", while Done when 5 says "equals the kept original exports with the scrub map applied". A reader applying Q8 literally would count the publisher display name as a difference or leave it out of the map.
- Suggested fix: word Q8 as Done when 5 does: the comparison after the whole scrub map (the icon removal and the display name included) must leave no difference.
- Resolution: Fixed (decisions Q8, worded again). The whole scrub map, display name and icon included, must leave no difference.

## Round 3 (re-review of the revised plan)

Environment facts were not read (`environment: null`; `pac` and `powerops plan facts` were not run). Re-checked: the resolutions of R13 and R15 to R17 against the current `plan.md` and `decisions.md`, and R1 to R12 and R14 for damage from the round-3 edits; then the plan against `SolutionParser.ReadAgents` and `ReadAgentConfiguration`, `Solution.cs`, `SolutionDiff.cs`, `Agents.cs`, PO027, PO028, PO030, PO031, the tests the rows name (`AgentReaderTests`, `DiffCommandTests`, `MutationTests`), `docs/COMMANDS.md`, the diff schema and goldens, and the three fixture zips (scanned: only the two fake ids, no `@`, `dynamics.com` or `.crm`; with the built CLI, `diff` of `v1` against `v2` and `v1-no-learn-tool` today prints the two M50 `note:` lines, as the plan says). R13 and R15 to R17 hold, and R1 to R12 and R14 still hold. The planning chat was not read. `plan.md` and `decisions.md` hold no URL, GUID, email or person's name.

### High

No findings.

### Medium

No findings.

### Low

#### R18. Test lists of rows 1, 2 and 4 leave out cases the rows specify
- Area: Data rows 1, 2 and 4 (tests)
- Finding: Row 1 says a `value` that is "not valid UTF-16" leaves the instructions unread, and `$kind` must be text, but its test list names only a numeric `value`; a lone-surrogate string makes `GetString()` throw `InvalidOperationException` (the reason `JsonString` exists), and `MutationTests` can't reach it: the new layout exists only as zips, which get bit flips, not the JSON mutations. Row 3's test list refuses malformed `McpTool` YAML with the path but row 4 has no such test for `IQCapability`, which Q6 also refuses. Row 2's tests have no case for the commonest input, a Custom GPT component beside a `configuration.json` with no `agentSettings` or no `instructions` key (no conflict), and its wording "has instructions that are unread or that differ" doesn't say that absent is not a conflict; a null-versus-text comparison would flag every standard-harness agent. "InstructionsFile the component's data" is null for several components, as today, which the row doesn't say.
- Suggested fix: add to the test lists a `value` of `"\ud800"`, a `$kind` that is a number, malformed `IQCapability` YAML refused with the path, and a component beside a `configuration.json` with no instructions (no conflict); write "absent is not a conflict" in row 2.
- Resolution: Accepted, handled in the build: the tests add a lone-surrogate `value`, a `$kind` that is a number, malformed `IQCapability` YAML refused with the path, and a Custom GPT component beside a `configuration.json` with no instructions, which is not a conflict (row 2's "has instructions" already excludes absent ones). The plan body is not edited, so its hash stays the reviewed one.

#### R19. Row 5 gives no separator or example for the combined `agent` detail, and row 9's "whose logical name it prints" is ambiguous
- Area: Data rows 5 and 9 (R14)
- Finding: The `agent` detail joins `instructions changed` and one part per workload, but the separator isn't named (`Join` and `Differences` use "; "), and the tests assert the text. Row 9 says an `McpTool`'s docs entry has "a connection reference and operation id ..., whose logical name it prints"; R14's fix was that the logical name of the connection reference is printed, as `connectionReference` changes already print it, and the sentence can be read as the tool's own name.
- Suggested fix: write one example detail line in row 5 (with "; "), and word row 9 as "the logical name of its connection reference is printed".
- Resolution: Accepted, handled in the build: the parts are joined with "; ", as `Join` does (`instructions changed; IQCapability <schema>: mail Write → (none)`), with a test; the `diff` docs say the logical name of an `McpTool`'s connection reference is printed.

#### R20. An unmappable `McpTool` or `IQCapability` whose text holds `TaskDialog` loses PO027's not-checked line for a placed agent
- Area: Data row 3, decisions Q6 (R16)
- Finding: Today such a `data` goes to `UnreadFiles`, and PO027 prints "'f' is valid YAML whose keys PowerOps can't map ... PO030 and PO031 don't check it either" for a placed agent. Row 3 moves it to `OtherComponents`, which `analyze` never prints, so that note disappears. The plan says `analyze` prints no line for it, but argues from "its agent is not on the standard harness", which isn't true of a placed agent, and no row names the lost note. No source or export shows the case, and R16 chose it on purpose.
- Suggested fix: add one sentence to row 3 or Q6 saying the case is silent in `analyze` for a placed agent and why that is accepted (no export holds one).
- Resolution: Fixed (decisions Q6, round 3): the lost PO027 line for such a `data` on a placed agent is written down and accepted, since no export or source shows one.

#### R21. Superseded answers stay in `decisions.md` beside their amendments
- Area: decisions Q5, Q7, Q8 (R17)
- Finding: Round 2's Q8 still says the comparison with the originals "must leave only the icon", and round 3 words it again as "no difference"; round 1's Q5 (`IQCapability <capability>: ...`) and Q7 (rules read the model) are replaced by later rounds. An implementer who reads Q8 of round 2 alone gets the sentence R17 reported.
- Suggested fix: edit round 2's Q8 to "no difference" and mark round 1's Q5 format and Q7 as superseded.
- Resolution: Fixed: round 2's Q8 now says "no difference", and round 1's Q5 format and Q7 are marked superseded.

#### R22. Won't build and the docs don't mention a tool's or workload's `statecode`, and M50's components line says "not read as tools" for a workload
- Area: Won't build, Data row 9 (silent false equality)
- Finding: `botcomponent.xml` of every component carries `statecode` and `statuscode`; a component set inactive is "no changes" with an empty `notCompared`, like the names and descriptions that Won't build lists. No export shows one inactive, so this is theoretical. Separately, an `IQCapability` that row 4 leaves in `OtherComponents` is reported by M50's line as a component "not read as tools", which reads oddly for a workload.
- Suggested fix: add state to the list of things not compared in Won't build and the `diff` docs; optionally say "not read" for workloads, or leave the line as it is.
- Resolution: Accepted, handled in the build: the `diff` docs add a component's state to what is not compared. M50's "not read as tools" line stays as it is: an unread workload is one of the type-9 components that line counts.
