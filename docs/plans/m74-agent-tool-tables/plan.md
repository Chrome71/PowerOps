---
status: done
review: done
environment: null
depth: quick
supersedes: null
signed-off: 2026-11-08
signed-off-hash: 2a6acee63de94b91cd3495476f2bc86bc1329d019409891011d58a4900feb461
---

# M74: Read agent tools with OData keys, and let PO034 see the tables of agent tools and business rules

## Goal

Fix two findings of the PowerOps 1.0 trial (2026-11-08), as section 3 of the agreed 1.0 fix plan sets out (Fable and Astra, six rounds):

| Id | Finding | This plan |
|---|---|---|
| F34 | The four Dataverse tools of the trial's agent were not read. Each `data` file holds `"@odata.nextLink":` or `"@odata.id":` keys whose values are mappings (in `dynamicOutputSchema`), and `YamlXml.Mapping` treats a key starting with `@` as an XML attribute and refuses a mapping value (`YamlXml.cs:70-74`). So all four tools landed in `UnreadFiles`: PO027, PO030 and PO031 didn't check them and `diff` didn't compare them | agent YAML is read with every key an element (rows 1, 2) |
| F19 | PO034 gave "not checked" for every role, because the solution held business rules (workflows with no `ClientData`) and an agent (`PO034RoleWritesUnusedTable.cs:28-33`) | PO034 reads a business rule's table and an agent tool's table where they are known, and names the exact component that still blocks it (rows 3 to 7) |

Done when:

1. An agent tool file whose YAML has a key starting with `@`, or a `#text` key, with a mapping value is read as a tool in both an unpacked folder and a zip; a complex key (`? [a, b]`) or an empty key still leaves the file in `UnreadFiles`. The YAML layout's converter refuses such keys as today.
2. A category-2 workflow (a business rule) with a `PrimaryEntity` adds that table to PO034's used tables and no longer blocks PO034. Any other workflow with no readable definition still blocks it.
3. An agent no longer blocks PO034 when it has no unread file, no other component, no topic or child agent with an action node, and every tool is known for tables (rows 4 and 6). Otherwise the `note:` line names the agent, the component and why, instead of "a Copilot Studio agent".
4. A fixture `tests/fixtures/agents/dataverse-tools/`, built from fake names in the trial export's shape, loads with every tool read; `analyze` on it gives exactly two findings, PO030 once and PO034 once, and no `note:` line. `diff` of it against a copy with one tool's mode changed reports that tool.
5. `docs/rules/PO034.md`, `docs/COMMANDS.md` (`analyze`, `diff`) and `tests/fixtures/NOTICE.md` say what changed; the `analyze-fixtures.*` goldens are updated by the recipe.
6. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m74-agent-tool-tables` and the leak scan are clean; committed locally on the session's branch.

### Won't build

- A list of known OData key names. Every key an element covers them all and any new one.
- Guessing a table from an entity set name (dropping an `s`). Only a table whose exported `EntitySetName` equals the tool's value counts.
- Reading the XAML of a business rule or a classic workflow. A business rule is taken to act on its primary table (Assumptions); every other category still blocks.
- Reading which tables an MCP tool, a connected agent, or a flow outside the solution uses.
- Parsing topics. A topic or child agent is read only for its node kinds (row 4); one with an action node blocks PO034.
- Agents in the YAML layout. `YamlReader` reads no agents today; it gets the workflow change only through the shared `ReadWorkflow`.
- A `diff` risk for a changed tool table.

## Users

- A maker or an agent running `analyze` on a solution with a Copilot Studio agent whose tools call Dataverse, who needs PO027, PO030 and PO031 to check those tools.
- A reviewer reading `diff` between two versions of such a solution.
- A security reviewer reading PO034 on a solution with business rules or an agent, who needs findings, or a note that names what still blocks them.

## Data

Line numbers are those of `517226e`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | `YamlXml.Read(yaml, count, attributes)`: when `attributes` is false, every key is an element, named by the existing `Name` (`XmlConvert.EncodeLocalName`, so `@odata.id` becomes `_x0040_odata.id`); a non-scalar key and an empty key still throw `YamlShapeException`. `Convert` passes true, so the YAML layout keeps its `'@name'` attribute and `'#text'` rules and refusals. Nodes are counted as for any element | `src/PowerOps.Core/Readers/YamlXml.cs:21-93` | `AgentReaderTests` rows below; `YamlReaderTests:152-153` (the layout still refuses `'@a'` and `'#text'` mappings) passes unchanged |
| 2 | `SolutionParser.ReadAgentYaml` passes false. The text pre-check (`text.Contains("TaskDialog")`, `:239`) and the `McpTool` and `IQCapability` paths are unchanged | `src/PowerOps.Core/Readers/SolutionParser.cs:320-333` | `AgentReaderTests.OddKeys` (`:317`) splits: `'@odata.nextLink':` and `'#text':` with a mapping now give one tool and no unread file; `? [a, b]` and `""` still give an unread file (`:326`). The Custom GPT theory (`:338`) splits the same way: the first two now give the instructions. Zip and folder agree (`One`, `:65`). A tool file of many `'@odata.id':` mappings over a small `maxNodes` is refused with its path (as `:390`). Four existing tests change with it: `OddKeyAttribute` (`:315`) becomes `OddKey` with the value `"? [a, b]\n: c\n"`, so `An_unread_file_is_listed_under_its_own_agent_only` (`:225`) and `Unread_files_are_the_tool_data_paths_in_ordinal_order_and_a_mappable_tool_is_not_one` (`:351`) keep testing an unread file; `A_tool_with_a_reserved_xml_name_key_is_skipped_as_unread_not_refused` (`:371`) becomes "a scalar `'@xmlns'` key is read as an element, so the tool is read" (the `catch (ArgumentException or XmlException)` in `Read` stays for the YAML layout, where `YamlReaderTests:84` covers `'@xmlns'`); `AnalyzeCommandTests.A_tool_file_whose_keys_cannot_be_mapped_prints_the_not_checked_note_on_stderr_and_exits_as_before` (`tests/PowerOps.Cli.Tests/AnalyzeCommandTests.cs:660-674`) switches its key to `? [a, b]` |
| 3 | `Workflow` gains `PrimaryEntity` (string?, last positional parameter, default null): the `<PrimaryEntity>` text, null when missing, empty or `none` (cloud flows export `none`) | `src/PowerOps.Core/Model/Solution.cs:79`; `SolutionParser.ReadWorkflow` (`:692-708`) | `XmlUnpackReaderTests` and `ZipReaderTests`: a category-2 workflow's `PrimaryEntity` is read; a cloud flow's `none` is null |
| 4 | `AgentTool` gains `Table` (`ToolTable?`, default null) and `FlowId` (string?, default null), set for a `TaskDialog` only. `ToolTable(string? EntitySet, string? Name)`: null when no item of the tool's top-level `inputs` list (a sibling of `action`, as exported) has `propertyName: entityName`; `EntitySet` is that item's `value` when the item's `kind` is `ManualTaskInput` and the value is plain text not starting with `=`, else null (an `AutomaticTaskInput`, a formula, a mapping, or no value); `Name` is the `Name` attribute of the solution's `entity` element whose `EntitySetName` equals `EntitySet` ignoring case, else null. `FlowId` is the normalized `action.flowId` (null when missing). Entity set names come from each `EntityInfo/entity` element that has an `EntitySetName` child: first the `Entities/*/Entity.xml` files in ordinal path order, then the `Entities/Entity` elements of the customizations file already loaded (`customizations.xml` in a zip, `Other/Customizations.xml` unpacked); the first table to claim an entity set wins, and only `Name` and `EntitySetName` are used. The `Entity.xml` files are loaded, through `LoadXml` and so charged to `ReadBudget` in full, only when some tool has a literal `entityName`; a solution with no such tool reads nothing more. A malformed, over-large or over-deep `Entity.xml` then refuses the solution with its path, as any other file `LoadXml` reads. `Agent` gains `ActionDialogs`, a list of `AgentComponent` (`Kind` the first such node kind, `File` the `data` path) in ordinal order of file: its type-9 components whose first column-0 `kind:` is `AdaptiveDialog` (a topic) or `AgentDialog` (a child agent) and whose text has a line `kind: <k>` where `<k>` starts with `Invoke` or is `HttpRequestAction`, at any indent, after an optional `- `. It is read from the text the reader already holds, before the `TaskDialog` pre-check (`:239`) and its `continue`, so a topic that mentions `TaskDialog` and one that doesn't are both scanned, and no file more is read | `src/PowerOps.Core/Model/Solution.cs:112-146`; `SolutionParser.ReadAgents` (`:205-311`), `Parse` (`:60-160`) | `AgentReaderTests`: a literal `entityName` gives `EntitySet` and the table's `Name` from an `Entity.xml` (folder) and from `customizations.xml` (a zip built with one); when both hold the entity set, the `Entity.xml` table wins; an `AutomaticTaskInput`, a `=` value and a mapping value give a null `EntitySet`; an entity set with no table gives a null `Name`; no `entityName` input gives a null `Table`; a flow tool's `flowId` is read and normalized. A malformed `Entity.xml` is refused with its path when a tool has a literal `entityName`, and not read at all when none has; an `Entity.xml` over a small `maxNodes` is refused with its path. A topic holding `kind: InvokeFlowAction`, a topic whose text also holds `TaskDialog` (in a message) beside a `kind: InvokeConnectorAction` node, and a child agent holding `kind: HttpRequestAction`, are listed in `ActionDialogs`; a topic of only `SendActivity` and `Question` nodes is not. `One` (`:65`) compares `ActionDialogs` apart, as it does `Tools`. Existing `new AgentTool(...)` expectations (`:113-115`) hold, as the new members default to null |
| 5 | PO034's used tables add each category-2 workflow's `PrimaryEntity`, and each tool's `Table.Name`. In the exports read, a table a tool can map is already a root component and so already used; the tool line stays as a one-line guard for a table whose `Entity.xml` is present without its root component (not seen) | `src/PowerOps.Core/Rules/PO034RoleWritesUnusedTable.cs:36-41` | `WarningRuleTests` (PO034): fires: a role with Create on X, Write on Y and Write on Z, a business rule on Y and a tool on Z's entity set give one finding, on X, and no note; quiet: a tool on X's entity set |
| 6 | PO034's `Unreadable` keeps its order (plugin assembly, canvas app, workflow, agent) and changes two branches. The workflow branch skips a category-2 workflow with a `PrimaryEntity`. The agent branch, in place of "a Copilot Studio agent", takes the agents in ordinal order of their `bot.xml` and returns the reason of the first agent that has a blocker, checking within it, in this order: its first unread file, `agent '<a>' file '<f>' (YAML keys PowerOps can't map)`; its first other component, `agent '<a>' file '<f>' (a component of kind '<k>')`, or `(a component with no kind)`; its first action dialog, `agent '<a>' file '<f>' (a topic or child agent with a '<k>' node)`; its first tool, in ordinal order of file, that is not known for tables, `agent '<a>' tool '<t>' (<why>)`. A tool is classed in this order: `Kind` `McpTool` or `Mcp` true: why is `an MCP tool`; `Action` `InvokeConnectorTaskAction`: known when `Table.Name` is set, else why is `it names no table` (`Table` null), `its entityName is not a literal entity set name` (`EntitySet` null) or `its entity set '<s>' names no table in the solution`; `Action` `InvokeFlowTaskAction`: known when `FlowId` equals some workflow's `Id` (that workflow is then read by the existing branches: its text by the flow check, or, with no text, it blocks as a workflow first), else why is `its flow is not in the solution`; any other `Action`: why is `its action is '<k>'`, or `it has no action kind` when null. Every echoed kind and entity set is cut to 100 characters with `…`, as `diff`'s notes are. The note's sentence stays: `... and the solution holds <reason>, whose table use isn't read.` Work IQ capabilities don't block | `src/PowerOps.Core/Rules/PO034RoleWritesUnusedTable.cs:22-33` | `WarningRuleTests`: the existing note theory (`:1938`) keeps its first three cases, and its two agent cases use an agent whose tool names no table; new cases, one per reason above, each with the exact note, plus a long kind cut at 100; two agents, the first clean and the second with a blocker, name the second; an agent with both an unread file and a blocking tool names the file; a category-0 workflow with no definition still gives the workflow note; a category-2 workflow with no `PrimaryEntity` gives it too. Flow tools: one whose flow is a cloud flow naming the table in its text gives no finding on that table and no note; one whose flow is a cloud flow not naming it gives the finding; one whose flow is a category-0 workflow with no text gives the workflow note |
| 7 | `docs/rules/PO034.md`: "What it finds" adds business rules and agent tools to what makes a table used, and the note's reasons; it says entity set names are matched to exported `EntitySetName`s, and that a business rule's table is its primary table (observed in exports, not stated by Learn) | `docs/rules/PO034.md:9-17` | `RuleDocsTests` (unchanged) |
| 8 | The fixture `tests/fixtures/agents/dataverse-tools/`: an unpacked solution made for this project, in the shape of the trial's export, with fake names only (publisher prefix `demo`, a solution `PowerOpsAgentTools`, tables `demo_Ticket` and `demo_Queue`, nothing named after the trial's tables). `Other/Solution.xml` (the two tables, the role, the workflow and the agent as root components) and `Other/Customizations.xml` (with the agent's Dataverse connection reference); `Assets/botcomponent_connectionreferenceset.xml`. Two tables: `Entities/demo_Ticket/Entity.xml` keeps one `attributes` block of two fake columns before `EntitySetName`, as an export has, and `Entities/demo_Queue/Entity.xml` is trimmed to `Name`, `EntityInfo/entity/@Name` and `EntitySetName`; one business rule (`Workflows/*.xaml.data.xml`, category 2, `PrimaryEntity` `account`, a system table that is not a root component; the XAML file is left out, as the reader doesn't read it); one custom role with Create and Write on both tables, on `account` and on `SharePointData` (the role editor's default), so `account` is quiet only through the business rule; an agent (`bots/demo_Helper/bot.xml` and `configuration.json`, generative orchestration on, authentication not None, instructions in a Custom GPT component `botcomponents/demo_Helper.gpt.default/`, and one topic of only `SendActivity` and `Question` nodes) with four `MicrosoftDataverse-…` tools as the trial had (List rows twice, Add a new row, Update a row in selected environment), each a `botcomponents/<name>/` folder with `botcomponent.xml` and `data`, each with a literal `entityName`, `organization: current`, `outputs` with an `"@odata.nextLink"` or `"@odata.id"` property name, and a `dynamicOutputSchema` holding `"@odata.nextLink":` and `"@odata.id":` mappings and an `item.'<lookup>@odata.bind'` input (a lookup column set by OData bind); one tool's mode is `Maker`, the others `Invoker`. Fake GUIDs only (`00000000-0000-0000-0000-0000000000xx`), no URL, user, email or environment name. Described in `tests/fixtures/NOTICE.md` with the scrub list | `tests/fixtures/agents/dataverse-tools/`; `tests/fixtures/NOTICE.md` | `FixtureRuleTests`: loads with four tools, each with its `Table.Name`, no unread file, no other component and no action dialog; the analyzer's findings are exactly PO030 once (the `Maker` tool) and PO034 once (`SharePointData`), and no rule gives a note; the fixture carries what the other rules need to stay quiet (the agent's connection reference in both files, authentication not None, instructions under 8,000 characters). The tables' tool logic is proven by row 5's tests, because in an export a table a tool can map is in the solution and so already used; the fixture proves the agent no longer blocks and the business rule's table counts. `DiffCommandTests`: the fixture against a temp copy with another tool's mode changed gives `changed agentTool <name>` with the `agent-tool` risk and no "not read as tools" note |
| 9 | `docs/COMMANDS.md`: `analyze` and `diff` say that agent YAML keys starting with `@` or `#` are read as names, and only complex or empty keys leave a tool file unread. The `analyze-fixtures.*` goldens gain the fixture's findings by the recipe "Update goldens" | `docs/COMMANDS.md:43, 55`; `tests/golden/analyze-fixtures.*` | `ReportTests`; the golden diff is read |

## Automation

None. Nothing new runs on its own.

## Screens

None.

## Environments and route

None. Readers, one rule and their tests change; no tenant read or write.

## Solution and publisher

None. The fixture is made for this project with fake names.

## Constraints

- AGENTS.md: the smallest change, new logic with tests, no new write, Conventional Commits, no version or changelog edits, no tenant URLs, ids, emails, user names or environment names in commits. The fixture copies no value from the trial's export, only its shape.
- The kept roles (`powerops-verify`, `powerops-review-security`), their plugin copies and `tests/eval` stay untouched.
- `ReadBudget` keeps charging every node, and the `Entity.xml` files are read only when a tool needs them.
- Rule output keeps its form: PO034's finding text and note sentence are unchanged except for the reason; PO027, PO030 and PO031 are unchanged.
- No schema change: `analyze` JSON has no new field (the model's new members are not serialized).
- Platforms are Windows and Linux; entity set and table names are compared ignoring case.

## Assumptions

- A business rule acts only on its own primary table. Learn's *Create a business rule for a table* (read 2026-11-08) creates the rule on the table and describes only actions on that table's columns, but never says it can't touch another table: implied, not stated.
- The Dataverse connector's `entityName` input holds the table's entity set name. Learn's connector reference (*Microsoft Dataverse*, read 2026-11-08) calls it "Table name" and doesn't say which name; the trial's export held each table's entity set name (`<prefix>_<table>s`) for both of its tables. Observed, unsourced.
- An exported table's `EntitySetName` is its Web API collection name: *EntityMetadata.EntitySetName Property* and *Web API service documents* on Learn (read 2026-11-08). That `Entity.xml` and `customizations.xml` carry it as `EntityInfo/entity/EntitySetName` is observed in the trial's export and in `tests/fixtures/powerops-seed`, not documented.
- A connector tool with no `entityName` input may still write a table (an unbound action, or another connector that reaches Dataverse), so it blocks rather than counts as using none; this is stricter than the fix plan's wording (decisions log).
- A tool counts as using the table it names whatever its operation (list, add, update, a bound action, relate). A bound action may reach other tables; counting the named table can only hide a finding, never add one, as the flow text check can.
- A topic or child agent reaches tables only through an action node, whose kind starts with `Invoke` (a flow, a connector, a skill or an AI model) or is `HttpRequestAction`; message, question, condition and variable nodes don't. Not checked on Learn: if another node kind can write a table, PO034 could give a wrong finding on an agent with such a topic. The trial's topics held only message, question, condition, variable, dialog and search nodes.
- The trial's tool files are 9 to 26 KB; reading their keys as elements adds a few thousand nodes each, far under `ReadBudget`'s 15,000,000.

## Open questions

## Questions for your admin

## Steps

1. This plan and `decisions.md`, a fresh-context review into `review.md`, fixes, and the person's sign-off.
2. Rows 1 and 2, tests first.
3. Rows 3 and 4, tests first.
4. Rows 5 and 6, tests first, then row 7.
5. Row 8 (fixture and NOTICE) and row 9, then the goldens by the recipe "Update goldens".
6. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m74-agent-tool-tables` and the leak scan; commit locally.
