# Decisions: m51-agent-layout

Answered by the building agent under the delegation of 2026-10-22 (agents answer product choices and sign off plans; the person keeps keys and deletions). Every answer takes the recommended option unless it says otherwise.

## Round 1

### Q1. Where do an agent's instructions come from?
1. A Custom GPT component, as today, whenever the agent has one; `configuration.json`'s `agentSettings.instructions.segments` only when it has none (recommended)
2. `configuration.json` whenever its instructions are readable, the component otherwise
3. Whichever is not empty
Answer: 1. The improvement plan's rule: a Custom GPT component wins. Every standard-harness export M48 read keeps them in the component, and the component is what that harness reads; the new UI's export has no component. An agent with several Custom GPT components keeps null instructions as today, and `configuration.json` doesn't stand in for them, because the components would then go uncompared without a line saying so.

### Q2. Which segments are read, and how are they joined?
1. Only items of `$kind` `StaticSegment` with a string `value`, joined with no separator; any other item makes the instructions unread (recommended)
2. Any item with a string `value`
Answer: 1. Only `StaticSegment` was seen. An item of another kind may hold a reference (a tool or a variable) whose `value` is not the text the model reads, and reading it as text could make two different prompts look equal. Unread instructions are a `notCompared` line, never a silent "no changes". The export had one segment; with several, the values are joined as they stand, since nothing documents a separator. Shortcut: if an export with several segments shows a separator, the join changes then.

### Q3. When are the two sources ambiguous, and what does `diff` say?
1. When the instructions come from a Custom GPT component and `configuration.json` also has instructions that differ or can't be read: the agent gets `InstructionsConflict`, and `diff` lists it as not compared for an agent in both versions; the component's text is still compared (recommended)
2. Refuse the solution
3. Compare both
Answer: 1. The improvement plan's rule: both present and different is a `notCompared` item, never silent. Unread `configuration.json` instructions count, because PowerOps can't tell that they agree. An added or removed agent gets no line, as M50 decided for the instructions line (M50 decisions Q11): `diff` compares instructions only for an agent in both versions. Option 2 would stop `analyze` on a layout Studio may write during a migration; option 3 would report changes nobody can tell apart.

### Q4. What does an `McpTool` become?
1. An `AgentTool` with `Mcp` true, `Mode` its `authMode`, `Action` null, and two new fields, `ConnectionReference` and `OperationId`, which `diff` compares; a `TaskDialog` tool leaves them null (recommended)
2. Also read the connection reference and operation id of `TaskDialog` tools
Answer: 1. The improvement plan names these four values. A `TaskDialog` keeps its operation id in two places (`action.operationId`, `action.operationDetails.operationId`), and M48 chose what it reads; reading more there changes `diff` for standard-harness agents, which is not this milestone's gap. `Action` is null because an `McpTool` has no `action`; PO031 counts any tool whose action is not a connected agent, so it counts these.

### Q5. What about the six `IQCapability` components?
(The matching key and part format are superseded by round 2's Q5.)
1. Read each one's `capability` and `access` and compare them inside the `agent` change, `IQCapability <capability>: <old> → <new>`, with no new component kind or risk label (recommended)
2. A new `diff` component `agentCapability`
3. Treat `IQCapability` as a known kind that is not a tool, not compared, and say so in the docs
4. Leave them in M50's not-compared line
Answer: 1. The acceptance asks for no not-compared line on these exports, so option 4 fails it. Learn says each is a Work IQ workload, selected by default with write on, that a maker can clear; clearing one changes what the agent can reach, which a reviewer of the pull request should see, so option 3 would hide a change that matters. Option 2 adds a value to `diff`'s closed `component` list, which bumps `schemaVersion` to 3 (`docs/schemas/README.md`); M53 changes the same schema next, and the agent change already carries a free `detail`. No risk label is added for the same reason; a workload change still counts as a change and exits `1`. They are not tools: they have no action or connection, and PO031 would otherwise count six tools that are one MCP server's scope.

### Q6. Does parsing `McpTool` and `IQCapability` data add refusals?
1. Yes for malformed YAML, refused with the path as a `TaskDialog`'s is; valid YAML whose keys can't be mapped, or an `IQCapability` without text `capability` and `access`, stays in `OtherComponents` and gets M50's line (recommended)
2. Read them from lines, as M50 reads `kind:`
Answer: 1. M48 refuses malformed tool YAML with the path (M48 plan, Data row 2), and these are tools or tool scope. M50 read only `kind:` from lines so that topics, which the reader never needs, add no refusals; topics still aren't parsed. A shape PowerOps can't map stays visible in `notCompared`.

### Q7. Do the agent rules read the new layout?
(Superseded by round 3's Q7: the rules keep today's layout.)
1. Yes: they read the model, so a standard-harness agent's `McpTool` tools and `configuration.json` instructions count; the rule pages and PO028's not-checked line say so (recommended)
2. Keep the rules on `TaskDialog` and Custom GPT components only
Answer: 1. One model for `diff` and the rules keeps them from disagreeing about what an agent holds. The new-UI agents seen are not on the standard harness, so no finding changes for them; for a standard-harness agent with `McpTool` tools, PO027 (MCP needs generative orchestration), PO030 (maker credentials, if `authMode` means what `mode` means, plan Assumptions) and PO031 (the count) apply as they do to a `TaskDialog` MCP server. This answers M50's follow-up (M50 decisions Q10).

### Q8. Where and how are the fixtures kept?
1. Three zips under `tests/fixtures/agents/new-ui/`, stored without compression and with fixed timestamps, scrubbed by a script kept outside the repo; `v1-no-learn-tool` lacks only the Learn tool's `botcomponents/` folder (recommended)
2. Unpacked folders, zipped by the tests
3. Under `tests/fixtures/zips/`
Answer: 1. The improvement plan and the task name `tests/fixtures/agents/new-ui/` and the `.zip` names; an exported zip is what the live test diffed and what a user has. Discovery finds zips under `tests/fixtures`, so `ReportTests` reads them, and an unpacked export without `pac solution unpack` has no `Other/Solution.xml` and isn't found. Stored entries keep the text readable to a search of the files, which the leak check relies on, since `git diff` shows a zip as binary. Removing only the folder repeats the live test; its connection reference stays in `customizations.xml`, as it would if a maker removed the tool's component by hand.

### Q9. What happens to M50's synthetic diff test?
1. Keep it as the test of the `note:`, JSON and Markdown path, with a shape M51 still doesn't read (segments without `$kind`, a component of an unknown kind) (recommended)
2. Delete it; the fixtures cover the new layout
Answer: 1. The fixtures now give an empty `notCompared`, so nothing else would run the whole path from the reader to stderr, JSON and Markdown.

### Q10. Does `docs/BUILD-PLAN.md` get an M51 section?
1. No: the improvement plan (M50 to M68) is not in the repo, and parallel sessions would collide on its numbers (recommended)
Answer: 1. As M50 and M52 decided.

### Q11. Is a confirming export from Copilot Studio made?
1. No: the two exports of 2026-11-04 are the evidence, and the improvement plan marks a new one optional (recommended)
2. Ask the person to export again before the Dev deadline
Answer: 1. M51 needs no tenant; a new export would need the person's sign-in.

## Round 2 (from the review)

### Q3, amended (R6)
The conflict test compares the two texts after replacing `\r\n` with `\n` and removing trailing `\n`, because a YAML block scalar keeps a line break the maker didn't type (PO028 trims it for the same reason) and JSON may write line breaks differently; a line that says nothing true would teach readers to ignore the list. The comparison between two versions stays exact: a change of source between versions is a change in the files, and its text is never printed.

### Q5, amended (R2, R10, R11)
Workloads are matched by component schema name, as tools are, not by the `capability` value, so two components for one workload are two parts, and one re-added under a new name shows as removed and added; nothing is dropped. Each side is written `<capability> <access>`, cut by `Show`, and compared ordinal, as every field in `diff` is. A side PowerOps can't read shows `(none)` and is also in M50's components line. The docs say a workload change carries no risk label; M53, which moves `diff`'s schema next, can weigh one.

### Q6, amended (R1)
The plan's row 3 gives the order. An `McpTool` whose YAML keys can't be mapped goes to `OtherComponents` as `McpTool`, never to `UnreadFiles`, whose `diff` line and PO027 note name `TaskDialog`. `analyze` prints no line for it, as for every other kind since M50 (M50 decisions Q10): its agent is on the GitHub Copilot harness and already gets the harness note (Q7).

### Q7, amended (R5, R13)
1. No rule change: the new layout is the GitHub Copilot harness's, whose agents the rules don't read (recommended)
2. The rules read the model, and their pages say so
Answer: 1, replacing round 1's answer. The open `vscode-copilotstudio` sync doc says the Custom GPT metadata is retired for CLI agents and that `McpTool` is a CLI-only tool type, and Learn says Work IQ belongs to the GitHub Copilot harness. Such an agent has a recognizer other than the standard harness's, so PO027 to PO031 print the harness note and read nothing else; the live exports confirm it. The rules' code reads the model, so in the case no source describes, a standard-harness agent holding an `McpTool`, they would read it as a tool, with `authMode` as its mode; no page claims anything about it, and an export that shows one is the time to decide. Round 1's option would have put a reading of `authMode` that no source documents into PO030's page.

### Q8, amended (R7, R12)
The two real exports hold the same paths, so the agent's and the Work IQ draft's schema names are the same in both, and the scrub maps each real value to one fake value; the fixtures keep the comparison the live test made. The fixture check before the push: the leak scan's pattern file against the zips' text, a search for any GUID or 32-digit hex id but the two fake ones, `@`, `dynamics.com` and `.crm`, and a comparison of the unzipped fixtures with the kept originals after the whole scrub map (the publisher's display name and the icon's removal included), which must leave no difference.

### Q12. What else does an agent of the new layout hold that `diff` doesn't compare? (R3, R4)
1. Say it in the docs and the schema: an empty `notCompared` means the instructions, tools and Work IQ workloads were read, not that the agent is equal (recommended)
2. Also compare the model, web search, `connectorId`, the type-18 component and tool names and descriptions
Answer: 1. `notCompared` lists what `diff` means to compare and couldn't read (M50 decisions Q1); `diff` has never compared an agent's settings, other component types or a tool's name and description, for the standard harness either, and the docs say so. Comparing them is new scope with no acceptance behind it. `connectorId` repeats the connection reference's connector, which `diff` compares under `connectionReference`. The other tool kinds of the GitHub Copilot harness (`ConnectorTool`, `WorkflowTool`, `ConnectedAgentTool`) were not seen in an export and stay in M50's components line until one is.

## Round 3 (from the second review)

### Q7, amended again (R13, R15)
1. Keep the rules on the layout they read today, in code: `AgentTool.Kind` and `Agent.InstructionsFromConfiguration` mark what the new layout added, and `Agents.Tools` and a new `Agents.Instructions` hand the rules only `TaskDialog` tools and Custom GPT instructions (recommended)
2. Let the rules read the model, and add rows, tests and page text for their new behaviour
Answer: 1. Round 2's answer said "no rule change" while rows 2 and 3 changed what the rules read; this makes it true in code, with tests. The new layout is the GitHub Copilot harness's (round 2), so the rules' pages, findings and not-checked lines stay true as written: an agent with only `configuration.json` instructions has no single Custom GPT component whose instructions are text, and an `McpTool` is a kind the rules don't read, as every kind but `TaskDialog` hasn't been since M50. Option 2 would put claims no source supports into PO028's and PO030's pages. Two small fields beat a test of file paths in the rules, and `diff` doesn't read them.

### Q6, amended again (R16)
The line kind is taken for every type-9 `data`, so an unmappable `data` whose line kind is `McpTool` or `IQCapability` goes to `OtherComponents` even when its text holds `TaskDialog` (as the M50 test's quoted `McpTool` with `note: TaskDialog` does); only the rest go to `UnreadFiles`. Row 3 adds the test. For an agent on the standard harness, such a `data` used to get PO027's not-checked line through `UnreadFiles` and now gets none in `analyze` (`diff` still lists it); accepted, because no export or source shows an `McpTool` or `IQCapability` on that harness, and a line naming `TaskDialog` would be wrong for it (R20).

### Q8, worded again (R17)
The comparison applies the whole scrub map, the publisher's display name and the icon's removal included, and must leave no difference; done before the push.

## Log
- questions answered by the building session under the user's delegation of 2026-10-22 (autonomous milestone mode); D1 to D9 don't touch M51
- signed off 2026-11-04 by the building session under the user's delegation of 2026-10-22, after three fresh-context reviews (Sonnet 5.5), the third with no High or Medium finding open (R18 to R22 Low, resolved in `review.md`) (`powerops plan sign-off`; `signed-off-hash` equals `review.md`'s `plan-hash`)
