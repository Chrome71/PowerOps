plan-hash: 2a6acee63de94b91cd3495476f2bc86bc1329d019409891011d58a4900feb461
model: claude-sonnet-5-5
fresh-context: yes

Environment facts were not read: the plan names no environment (`environment: null`) and changes this repo's own code, one rule, tests and a fixture, and the request allowed no `pac` or tenant call. Checklist items 1 to 9 and 11 (solution boundaries, connection references, licences, DLP, route, managed state, delegation, security roles as a design question, tables and flows as built artefacts) do not apply and are not reported; item 8 is only touched where PO034 reads roles. Item 9 (testability) and item 10 (open questions: 0; areas never asked are listed in the log and are all "not applicable") do apply, and so does the URL/GUID/email/name scan. `plan.md` was not edited (`review: pending` stays), because this request allowed no change outside `review.md`. Checked against the code at `517226e` by reading; no tests were run. `powerops plan status` was run with `--json` (there is no `--format` option); it reported `bodyHash` above, `openQuestions` 0 and no violations.

## Findings

### High

No findings.

### Medium

#### R1. Four existing tests break that the plan does not list
- Area: Data rows 1 and 2, Done when 6 (`dotnet test` clean), testability
- Finding: The plan names only `AgentReaderTests.OddKeys` (`:317`) and the Custom GPT theory (`:338`) as tests that split. Reading every key as an element also changes these, which use the mapping `'@odata.nextLink'` or a scalar `'@xmlns'` and expect an unread file: `AgentReaderTests.An_unread_file_is_listed_under_its_own_agent_only` (`:225`, uses `OddKeyAttribute`; the "odd" agent would now hold a tool and no unread file), `Unread_files_are_the_tool_data_paths_in_ordinal_order_and_a_mappable_tool_is_not_one` (`:351`, same constant), `A_tool_with_a_reserved_xml_name_key_is_skipped_as_unread_not_refused` (`:371`; with `attributes` false `'@xmlns'` becomes the element `_x0040_xmlns`, so the tool is read and the `ArgumentException` path is no longer reachable from an agent file), and `AnalyzeCommandTests.A_tool_file_whose_keys_cannot_be_mapped_prints_the_not_checked_note_on_stderr_and_exits_as_before` (`tests/PowerOps.Cli.Tests/AnalyzeCommandTests.cs:660-674`, writes `'@odata.nextLink':` with a mapping and expects the PO027 note). `ErrorRuleTests:496` and `SolutionDiffTests:697` build `Agent` records directly and are not affected. A builder following the plan would find these only when `dotnet test` fails.
- Suggested fix: list the four in rows 1 and 2 with what each becomes (switch the odd key to `? [a, b]` or `""` in `:225`, `:351` and the analyze test; turn the `@xmlns` test into "a scalar `@`-key is read as an element, so the tool is read", or delete it and say why the `catch (ArgumentException)` stays for the layout).
- Resolution: Answered. Row 2 lists the four tests and what each becomes (`:225`, `:351`, `:371`, `AnalyzeCommandTests:660-674`); `YamlReaderTests:84` does cover `'@xmlns'` for the layout, as the plan says.

#### R2. Reading `Entity.xml` adds a refusal and parse cost the plan does not describe
- Area: Data row 4, Constraints (`ReadBudget`), testability
- Finding: Nothing in `src/` reads `Entities/*/Entity.xml` today. Loading them in `ReadAgents` (through `LoadXml`, `SolutionParser.cs:778`) means a malformed, over-large or over-deep `Entity.xml` now makes the whole solution unreadable (`InvalidDataException` with the path) whenever any agent tool has a literal `entityName`, although the file was never read before and has nothing to do with the tool's own YAML. The plan says only that the files are "loaded only when some tool has a literal `entityName`" and that nodes are charged. It does not say whether a bad file refuses the solution or is skipped (leaving `Name` null and the note "names no table in the solution"), has no test for either, and does not say what is read from a real `Entity.xml` (tens to hundreds of KB per table; the seed's `Entity.xml` runs past 700 lines): a full parse of every table just to take one child element. Both sources are also not stated to be merged: an unpacked folder can hold an `Other/Customizations.xml` as well, and a zip has `customizations.xml` only. If an entity set appears in both, or in two tables, the plan does not say which wins.
- Suggested fix: decide the failure mode (the repo's convention is to refuse with the path, as for `bot.xml`) and add a `Refused`-style test for a malformed `Entity.xml` in a folder and a node-budget test; say that `EntitySetName` is looked up by name from both sources, union, first in ordinal path order wins, and that only the `entity` element's `Name` and `EntitySetName` are used.
- Resolution: Answered. Row 4: `Entity.xml` loaded through `LoadXml`, charged in full, refused with its path when malformed, merged with the customizations file (`Entity.xml` first, first claim wins), only `Name` and `EntitySetName` used; tests for a malformed file, a file unread when no tool needs it, and `maxNodes`.

#### R3. A topic or an HTTP node can still write a table, but an agent with one no longer blocks PO034
- Area: Data row 6, Done when 3, security roles (item 8)
- Finding: The rule's own comment says agent tools "can write any table" and PO034 used to block on every agent. Row 6 makes an agent known when its tools, unread files and "other components" are known, and `KnownKind` (`SolutionParser.cs:~200`) keeps topics (`AdaptiveDialog`) and child agents (`AgentDialog`) out of `OtherComponents` and `UnreadFiles`. Topics are never parsed. A topic can hold an `InvokeFlowAction` node (a flow that may not be in the solution) or an HTTP request node, either of which can write a Dataverse table; a child agent can hold its own action. After this change an agent whose tools are all read and known, plus any such topic, gives role findings instead of a note. A wrong finding is what the "stricter" decisions in the log say they avoid. The plan's Assumptions do not mention topics and no source is cited that topics cannot reach tables.
- Suggested fix: either keep blocking on topics that the reader has not looked into (a topic whose text mentions `InvokeFlowAction`, `HttpRequestAction` or `InvokeConnectorAction`, with a note naming it), or state in Assumptions and in `PO034.md` that a topic's actions are not read and add the claim to `powerops-researcher` hand-offs; also add a test with a topic beside known tools.
- Resolution: Answered with a new finding (R9). Rows 4 and 6, Done when 3 and Assumptions: `Agent.ActionDialogs` (an `Invoke…` or `HttpRequestAction` node in a topic or child agent) blocks PO034; the residual risk is stated.

#### R4. Rows 4 and 6 leave the tool shape and the note's precedence undefined
- Area: Data rows 4 and 6, Done when 3, testability
- Finding: (a) Row 4 says `Table` is null "when the tool has no input whose `propertyName` is `entityName`" but not where `inputs` sits in the YAML (top level or under `action`), nor which tools count as a "connector tool" (row 6 uses the term; is it `Action == InvokeConnectorTaskAction`, or any tool with an `entityName` input?). A flow tool, a tool with `Action` null, or a connected agent with an `entityName` input have no stated outcome. (b) Row 6 chooses the "first blocking component of the agents in file order", but does not say whether that is the first agent that has any blocker (then unread, other, tool within it) or the first unread file of any agent before any tool of any agent; the two give different note texts for two agents. (c) The reason for a tool whose `Action` is null reads `its action is ''`. (d) For an `Other` component, "(a component of kind '<k>')" and "with no kind" show a kind that can be up to the file size (the diff note cuts it at 100, `SolutionDiff.cs:~175`); the entity set in "its entity set '<s>'" is also user text. The tests "one per reason, each with the exact note" cannot be written until these are fixed.
- Suggested fix: add the `inputs` path and one sentence defining each tool class and its order of tests (MCP, then by `Action`: connector, flow, other); say agent first, then unread, other, tool; give the null `Action` text; truncate echoed kind and entity set to 100 characters as the diff does.
- Resolution: Answered. Row 4 gives the `inputs` path and when `Table` and `FlowId` are set; row 6 gives the tool classes in order, agent-then-component order, the null-action text and the 100-character cut; the tests are listed.

### Low

#### R5. A tool on a bound action or relate/unrelate counts its table as used
- Area: Data rows 4 and 5, assumptions
- Finding: The plan takes any Dataverse tool with a literal `entityName` as a use of that table, including a bound-action tool (the action's logic may be a custom API or plug-in that writes other tables) and Relate/Unrelate rows. Only the "unbound" case (no `entityName`) is treated as blocking. This can only silence a finding, as the existing flow substring check does, but the Assumptions list it nowhere.
- Suggested fix: add one Assumptions line saying a tool counts as using the table it names whatever its operation, and why that is accepted (it can only hide a finding), or limit `Table` to the operations the trial's tools showed.
- Resolution: Answered. Assumptions add the line that a tool counts as using the table it names whatever its operation.

#### R6. The Assumptions copy a table name from the trial's export
- Area: privacy / leak scan, Assumptions
- Finding: The second Assumption says the trial's export held the entity set name "`<prefix>_triprequests`". The prefix is a placeholder but `triprequests` is a table name read from the person's own export, which Constraints say must not be copied ("only its shape"). The fixture description (row 8) uses `demo` and the placeholder fake names and is fine; the plan and `decisions.md` hold no URL, GUID, email or person's name (the `00000000-…0xx` form is a fake pattern).
- Suggested fix: write `<prefix>_<table>s` (the table name plus `s`) instead, and keep the fixture's two table names unrelated to the trial's (not `trip`-anything).
- Resolution: Answered. Assumptions use `<prefix>_<table>s`, the fixture tables are `demo_Ticket` and `demo_Queue`; no `trip` text is left in `plan.md` or `decisions.md`.

#### R7. Done when 4 and row 8 do not say the finding set is exact, nor list the files that make the fixture load
- Area: Data row 8, Done when 4, testability
- Finding: "`analyze` gives PO030 once and PO034 once, and no PO027 note" does not say whether those are the only findings and notes. The fixture could trip PO028/PO029/PO031 or a role rule and still pass. The row lists the tables, business rule, role and agent files but not `Other/Solution.xml` (needed for discovery by `SolutionDiscovery.Find`), `Other/Customizations.xml`, or the `botcomponent.xml` for each of the four tools. A trimmed hand-written `Entity.xml` also differs from a real one (BOM, many `attribute` elements), so the test only proves the reader on its own trim.
- Suggested fix: make the test assert the exact finding list and note list (two findings, no notes); list `Other/Solution.xml`, `Other/Customizations.xml` and the component folders; keep one real-looking `attributes` block in one `Entity.xml` (fake names) so the reader is exercised on more than the trim.
- Resolution: Answered. Row 8 now has the files, the `attributes` block, the clause gone, and "exactly PO030 once and PO034 once, no rule gives a note" with the other rules' inputs named (connection reference in both files, authentication, instructions length).

#### R8. Row 6's "known flow tool" and the flow text check are not tied together in a test
- Area: Data row 6, testability
- Finding: A flow tool is "known" when its `FlowId` matches a workflow `Id`, on the grounds that "that flow's text is read by the existing flow check". That check (`InFlow`) is a substring test on `ClientData`, and a matching workflow with a null `ClientData` (a classic workflow with the same id, or a cloud flow whose JSON is missing) still blocks through the workflow branch. The plan's single test ("a flow tool whose flow is in the solution gives findings, not a note") does not show that a flow tool whose flow has the table in its text silences the finding, or that one whose flow is a category-0 workflow keeps the workflow note.
- Suggested fix: add those two cases next to the one listed.
- Resolution: Answered. Row 6 lists the three flow-tool cases (flow names the table, flow does not, category-0 flow with no text).

## Second pass

R1 to R8 are checked against the revised plan and the code; R7 is only partly answered. The revision adds three findings.

### Low

#### R9. `ActionDialogs` must be read on both code paths of `ReadAgents`, and the plan names only one
- Area: Data row 4, testability
- Finding: Row 4 says `ActionDialogs` is "read from the text the reader already holds". In `ReadAgents` a type-9 component takes one of two paths: a topic or child agent whose text lacks `TaskDialog` goes to the early `continue` (`SolutionParser.cs:239-246`), and one whose text holds `TaskDialog` is parsed and, being a known kind, falls through with no further step. A builder who adds the scan on one path misses the other; the listed tests (a topic with `InvokeFlowAction`, a child agent with `HttpRequestAction`) pass on the first path only. A topic whose YAML can't be mapped lands in `UnreadFiles` and already blocks.
- Suggested fix: say the scan runs before the early `continue` for every type-9 component whose `lineKind` is `AdaptiveDialog` or `AgentDialog`, and add a case with a topic whose text holds both `TaskDialog` and an `Invoke…` node.
- Resolution: Answered. Row 4's tests now include a topic holding both `TaskDialog` and an `Invoke…` node, so both paths are covered (see R12).

#### R10. The fixture's root components make the tables "used" before the tool and business-rule logic runs
- Area: Data row 8, Done when 4, testability
- Finding: Row 8 lists the two tables as root components in `Other/Solution.xml`. PO034 already adds root components of type 1 to its used set (`PO034RoleWritesUnusedTable.cs:~40`), so the fixture's one finding (`SharePointData`) comes out the same whether or not tools and the business rule supply table use. The fixture then proves only that the agent no longer blocks. The tool `Table.Name` assertion in the loader test covers the tools; the business rule's `PrimaryEntity` is covered only by the rule test in row 5.
- Suggested fix: either leave the tables out of the root components (the reader doesn't need them there, only the `Entities` files) so the finding set depends on the new logic, or say in row 8 that the fixture covers unblocking only.
- Resolution: Answered. Row 8: the business rule is on `account`, which is not a root component, and the role writes it, so only the rule keeps it quiet; the plan says the tool-table logic is proven by row 5, not the fixture. The role-privilege names (`prvCreateaccount`, `prvWriteaccount`) match the used set ignoring case.

#### R11. Row 8's escape clause contradicts Done when 4's "exactly two findings"
- Area: Data row 8, Done when 4
- Finding: Done when 4 says `analyze` gives exactly PO030 once and PO034 once and no `note:` line. Row 8 adds "If another rule fires on the trial's shape, the fixture keeps the shape and the test lists that finding". That turns the exact check into whatever the fixture gives, and Done when 4 can then be met by any set.
- Suggested fix: pick one: drop the clause (adjust the fixture until exactly two findings), or change Done when 4 to "PO030 once, PO034 once, and the other findings listed in the test with a reason each".
- Resolution: Answered. The escape clause is gone; row 8 and Done when 4 both say exactly two findings.

## Third pass

R7, R10 and R11 are answered; R9 is answered in part. The edit adds two Low findings.

#### R12. No test covers a topic that holds both `TaskDialog` and an action node
- Area: Data row 4, testability
- Finding: Row 4 now scans before the `TaskDialog` pre-check, which is the point of R9, but its tests list only a topic with `kind: InvokeFlowAction` and a child agent with `kind: HttpRequestAction`, with no word on whether either mentions `TaskDialog`. If neither does, only the early-`continue` path is tested and a scan that moved after the pre-check would still pass.
- Suggested fix: add a case in which a topic holds `TaskDialog` (for example in a comment or a `BeginDialog` target) and an `Invoke…` node, and expect it in `ActionDialogs`.
- Resolution: Answered. Row 4 adds the topic that holds both `TaskDialog` and an `Invoke…` node.

#### R13. Adding a tool's `Table.Name` to PO034's used tables can never change a result
- Area: Data rows 4 and 5, size
- Finding: Row 8 now says that "in an export a table a tool can map is in the solution and so already used": `Table.Name` resolves only from the solution's own `Entity.xml` and `customizations.xml`, and those tables are root components, which `Left` already adds to `used` (`PO034RoleWritesUnusedTable.cs:~40`). So the `used.UnionWith(tools' Table.Name)` line in row 5, and its fires/quiet test, change nothing for a real export; what the table lookup does change is whether a tool is known or blocks ("names no table in the solution"). AGENTS.md asks for the smallest change.
- Suggested fix: keep the lookup for the known/blocking decision, and either drop the `used` addition and its test or say in row 5 that it is kept for a hand-built solution whose table is not a root component.
- Resolution: Answered. Row 5 keeps the `Table.Name` line as a one-line guard for a table whose `Entity.xml` is present without its root component, and says so.
