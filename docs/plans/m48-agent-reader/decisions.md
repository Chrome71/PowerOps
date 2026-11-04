# Decisions: m48-agent-reader

## Round 1

### Q1. `pac copilot pack` refuses D7's MIT workspace fixture. What is the fixture?
1. No committed fixture: tests build the layout by hand in the shapes that the public samples and a local `pac copilot pack` show, option 3 of the Phase 7 decisions' Q7 (recommended)
2. Commit a workspace scaffolded by `pac copilot init` and packed by `pac copilot pack`, with fake names
3. Pack a copy of the MIT fixture trimmed until `pack` accepts it
4. Stop M48 and ask the person
Answer: 1. The person chose the Phase 7 Q7's option 1 on 2026-11-02, the packed fixture "if it holds no tenant values", which holds only if `pack` accepts it; `pack` refuses it and writes nothing, so D7 gives no fixture, and F6's own test (does that fixture pack into the sample's layout?) has no answer. What F6 compared instead is a template workspace from `pac copilot init`, which has topics and a Custom GPT component but no tool: its paths and files match the sample's, and the tool shapes rest on the public exports alone. The M48 stop rule needs a mismatch shown as well, and none was, so M48 goes on, taking that question's option 3 ("Build an own agent fixture by hand in tests only, no committed fixture") under the delegation of 2026-10-22; the PR body names the departure so the person can overrule it at the merge (amended in Round 2, R1). Option 2 would commit `pac`'s template text, which is under `pac`'s licence terms, not an open licence. Option 3 breaks "public samples stay unmodified", the reason D7 turned down the scrubbed order-management zip. M47 shipped the same way, on public exports read for shape only.

### Q2. Which rule ids?
1. PO027 to PO031, the next free ids after M47's PO026, the Error first (recommended)
2. In your own words
Answer: 1. The recipe's "one after the highest in `docs/rules/`" gives PO027, and neither the Phase 7 plan nor BUILD-PLAN names ids for M48.

### Q3. Where do agents live in the model?
1. A new `Solution.Agents` list, parsed in `SolutionParser` for both XML layouts, with the tools and instructions on each agent; `AgentConnectionReferences` stays as M40 parsed it (recommended)
2. Raw component texts on `Solution`, parsed in each rule
Answer: 1. The M48 section asks for one reader into a `Solution` field, and the "Add a reader" recipe says a new kind of component is a new parse step in `SolutionParser.cs` plus a new field on `Solution`. Parsing once keeps the five rules set lookups. The mapping file needs no second read: no new rule uses it, and a tool's own `data` names its connection reference.

### Q4. How is an agent placed in the standard harness?
1. Placed when its `configuration.json` is a JSON object whose `recognizer.$kind` is absent, `GenerativeAIRecognizer` or `VirtualAgentsRecognizer`; any other agent gets one note naming PO027 to PO031 and the kind it found (recommended)
2. Not placed only for the two `CLI…Recognizer` kinds seen, placed otherwise
3. Don't place agents; run the rules on every agent
Answer: 1. The Phase 7 plan scopes stage two to the standard harness and says a check that can't place an agent says "not checked". Learn doesn't say which recognizer each harness writes. Observed: `pac copilot init` in its default classic mode and every standard-looking export write `GenerativeAIRecognizer`; `init --authoring-mode cli-copilot` writes `CLICopilotRecognizer`, and the order-management sample `CLIAgentRecognizer`, and Learn says the GitHub Copilot harness runs on "an enhanced orchestration runtime that replaces the standard model". Some workspace settings in vscode-copilotstudio's test data name no recognizer, so an absent kind is placed. A classic agent's kind wasn't seen; that repo's schema calls `VirtualAgentsRecognizer` the "Default recognizer implementation which uses Power Virtual Agents recognition", and Learn separates the harnesses by orchestration runtime, not by recognizer, so it is placed too, or PO027, whose finding needs classic orchestration, might never reach the agents it is for (amended in Round 2, R4). A recognizer that is there but names no string kind is `""` and not placed. Option 2 would place an unknown future kind, such as the Customer Conversation harness's, and give findings the rules weren't written for. Option 3 ignores the plan. The note is one line per agent from PO027, and names all five rules, so it doesn't print five times.

### Q5. What counts as a tool?
1. A type-9 component of the agent whose `data` has `kind: TaskDialog`; PO031 counts them all except `InvokeConnectedAgentTaskAction`; a tool is MCP when `action.operationDetails.kind` is `ModelContextProtocolMetadata` (recommended)
2. Every type-9 component
3. Every `TaskDialog`, connected agents included
Answer: 1. Every export and workspace observed writes tools as `TaskDialog` components and topics as `AdaptiveDialog`, so option 2 would count topics, which Learn limits separately (1,000 per agent). In Copilot Studio a connected agent is added as an agent, not a tool, and Learn's 25 to 30 is about tools, so option 3 could warn early. The MCP test is the M48 section's ("a component with `ModelContextProtocolMetadata`"). Each tool on the page is named with what the count includes. Tools a child agent (`AgentDialog`) uses count with the root agent's: their components name the root agent in `parentbotid`, as every component does, and the child names them only in its instructions text (Round 2, Q13).

### Q6. What does the reader do with agent files it can't parse?
1. Refuse malformed XML, JSON or YAML with an `InvalidDataException` naming the path, as for every other file the parser opens; well-formed text of an unknown shape gives null fields and notes (recommended)
2. Never refuse: give null fields for malformed text too, as M46 and M47 do for the canvas metadata's JSON
Answer: 1. M46 and M47 tolerate malformed JSON inside canvas metadata because that text was already loaded by every command before they parsed it; `bots/` and `botcomponents/` are new files, like M45's Power Pages components and M46's app modules, which refuse malformed text. The recipe asks for errors that start with the path. The shape stays tolerant: no command stops on an agent whose fields are missing or of another kind. To keep topics, which the rules don't read, from adding refusals, a type-9 `data` is parsed only when its text contains `TaskDialog`, which every tool's does. Valid YAML that the YAML layout's converter can't map, because it reads `@` keys as attributes and `#text` as text and refuses keys that aren't text or are empty, is a shape, not malformed text: such a tool is skipped with a note from PO027 that names the file and says PO030 and PO031 don't check it either, and such a GPT component gives null instructions and PO028's note (amended in Round 2, R14 and R18). YAML-layout files still refuse these keys, with the same messages.

### Q7. Which instructions does PO028 read, and how are characters counted?
1. The top-level `instructions` of the agent's one type-15 (Custom GPT) component; a missing key is empty; none, several, or `instructions` that isn't text gives the note; characters are UTF-16 code units (`string.Length`) after trailing line breaks are removed, and whitespace only is empty (recommended)
2. The component named by `gPTSettings.defaultSchemaName` in `configuration.json`
Answer: 1. Every export observed has one type-15 component per agent. Option 2 adds a second lookup for a case no export shows; with several components the note says so. Learn gives the limit in characters without a unit; the page says how PowerOps counts, which differs from code points only for characters outside the Basic Multilingual Plane. A YAML block scalar keeps a trailing line break (`|+` all of them), which the person didn't type, so 8,000 typed characters stored with one would fire; trailing `\r` and `\n` are removed before counting (amended in Round 2, R9).

### Q8. What does PO029 read?
1. `authenticationmode` in `bot.xml`: 1 is None (Learn's bot table); missing or not an integer gives the note; 0 (Unspecified) is quiet (recommended)
2. Also fire on 0
Answer: 1. Learn names 1 as None. 0 is the column's form default, and no export shows what the platform does with it, so a finding on it would rest on nothing.

### Q9. Does PO030 skip agents with event triggers, which Learn says use the maker's credentials?
1. No: fire on every tool in Maker mode, compared ignoring case, and say on the page that tools a trigger calls must use the maker's credentials, so such findings are expected and can be baselined (recommended)
2. Skip tools of agents that have a trigger component
Answer: 1. Copilot Studio's own security scan warns whenever a tool uses maker-provided credentials, so this rule says what Microsoft says before publishing. Which tools a trigger calls isn't in the export, and the trigger component's type differs between the bot table (17) and a test-data export (100000000), so option 2 would rest on a guess and would hide tools that users call.

### Q10. When does PO031 fire, and when does it say "not checked"?
1. More than 30 counted tools fires, whatever the MCP servers; 30 or fewer with at least one MCP server gives the note (recommended)
2. Never fire when an MCP tool is present
Answer: 1. The M48 section: "more than 30 tools, or 'not checked' where an MCP tool is present, because offline can't count a server's tools". Each MCP server counts as one tool, a lower bound, so a count over 30 is over 30 whatever the servers publish. The note gives no count since Round 2 (Q13).

### Q11. Does `pac copilot init --environment` write to a tenant?
1. Yes: Learn's `pac copilot` reference says the bootstrap mode imports a solution into the environment and creates server-side objects; M49 adds the gate route, for `init` with `--environment` and for `init` while an auth profile is active (recommended)
2. No
Answer: 1. Read from Learn and `pac copilot init help` (2.14.1) only; it was not run against an environment. The two disagree with themselves: the page says the presence of `--environment` decides the mode, and the flag's description, on the page and in the help, says the active auth profile's organization is used when it isn't given, as it is for `clone` and `publish`. F6 ran `init` only with no profile visible, where it wrote local files. So the plan, the reference file and the PR body say `init` writes with `--environment` and may with a profile active, and M49 should route it in both cases unless a live run shows otherwise (amended in Round 2, R5). `pack` is local.

## Round 2 (from the review)

### Q12. Does PO028 fire on empty instructions whatever the orchestration? (R3)
1. Only with generative orchestration on; quiet when it is off or the configuration doesn't say; the 8,000 half for every placed agent (recommended)
2. As built, on every placed agent
3. Move the empty half to the review items in `copilot-studio.md`
Answer: 1. The M48 section asks for both halves, and the Phase 7 Rules bullet asks a rule for a stated number and low noise. Learn states the 8,000. For the empty half, Learn says instructions steer generative orchestration, and that with classic orchestration tools are called only from topics, so an empty text on a classic agent steers nothing. Measured on the five placed public agents: all have generative orchestration on and three have empty instructions, so option 1 still fires on three; the Goal's Assumptions and the page say so, and the finding can be baselined. Option 3 departs further from the section.

### Q13. What do PO031 and the MCP messages say, given child agents and MCP servers? (R2, R7)
1. Keep the count; message `has <n> tools, more than 30; Learn advises 25 to 30 at most.`; Fix `Remove tools the agent doesn't need.`; PO027's message and Fix and PO031's note say "MCP server", and the note gives no count (recommended)
2. Split the count by child agent, reading tool names from each `AgentDialog`'s instructions text
Answer: 1. In the account lookup sample every tool component names the root agent in `parentbotid`, and each child agent names its tools only in its instructions, so the export gives no per-agent split, and the old Fix, "move groups of them to child agents", couldn't clear the finding. Option 2 would parse free text. One MCP component connects to one server, which publishes its own tools, so "MCP tool" undersold it. vscode-copilotstudio's schema documents a `knownTools` cache and `triggerCondition`; no export holds them, so the page says they aren't read. A count-free note also avoids "1 MCP servers".

## Log

- 2026-11-03: plan started at the request of the session that started this one: build M48 of Phase 7, build-order item 83, from the M48 section of `docs/plans/phase7-power-platform/plan.md`, D7, F6 and D10 (decision 49), and the BUILD-PLAN M48 row
- read on 2026-11-03: AGENTS.md; the Phase 7 plan's stage two section, the M48 and M49 sections, D1, D7, D9, D10, D11 and the Dev facts table; its decisions Q7; the BUILD-PLAN M48 row and item 83; the M47 plan folder (layout) and its build hand-over; the Phase 7 Copilot Studio and MCP research report; `Solution`, `SolutionParser`, `YamlXml`, `YamlRoot`, `ReadBudget`, `ZipReader`, `XmlUnpackReader`, `INotChecked`, `Analyzer`, `PO026`, `RuleHelpers`, `Builders`, `SkillsCommandTests`, `docs/rules/PO026.md`, `skills/powerops-plan/references/route.md` and `code-apps.md`
- F6 run 2026-11-03 in the session's scratch folder with `pac` 2.14.1 (the newest on NuGet), with `HOME` set to an empty scratch folder so no auth profile was visible: the MIT workspace fixture `all-the-things-clean-workspace` of microsoft/vscode-copilotstudio at `9f6151fb7d22b2bedfa61182cc8f715b7c890542` was fetched through the GitHub API (licence MIT, read the same way) and packed with `pac copilot pack --publisher-prefix test`; `pack` refused it as not a valid agent workspace, naming 11 unsupported files and folders, and wrote nothing. `pac copilot init` (no `--environment`), in its default mode and in `cli-copilot` mode, then `pac copilot pack` on the default workspace and `pac solution unpack` on that zip and on the order-management zip, all local; the paths are in the Goal and Assumptions. Nothing from these runs is committed
- the public samples in Assumptions were fetched through the GitHub API into the scratch folder and read with a scratch script, for shape only; nothing from them is committed
- Learn facts checked 2026-11-03 with the Learn MCP tools; the pages are in Assumptions and go on the rule pages
- `tests/eval` and `tests/fixtures` hold no `bots/` or `botcomponents/` path (checked with `find`), as the Phase 7 plan's M48 "Touches" says
- depth: quick, as M47's was: one reader and five rules from a signed-off section
- questions answered by the building session, under the user's delegation of 2026-10-22 (autonomous milestone mode), with the Phase 7 plan's scope and the master session's brief
- areas not asked: users, automation, environments and ALM, and constraints; the change is to this repo, and the plan answers each or marks it not applicable
- approved 2026-11-03 by the building session under the user's delegation of 2026-10-22, for a fresh-context review; approved without asking: users, automation, environments and ALM, constraints
- the code was built alongside the review, before sign-off, by an implementer subagent (Sonnet) in this worktree, test first (the tests could not compile before the record change, so no red run was seen); the review fixes by a second implementer subagent (Sonnet), which changed the rules before their new tests, so no red run was seen for those; the rule pages, the reference file, the `analyze` paragraph and the BUILD-PLAN row, item and verification line by the building session; read by the building session
- fresh review (a subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-11-03, on the body at hash `f4260ab7…`: 0 High, 6 Medium, 7 Low (R1 to R13)
- R1 to R13 answered in `plan.md`, the code, the tests, the pages, `copilot-studio.md` and here (Round 2: Q12 and Q13 added; Q1, Q4, Q5, Q7, Q10 and Q11 amended); see review.md
- sanity run 2026-11-03 with the build after the review fixes, on scratch copies outside the repo: the order-management zip and its unpacked folder give the harness note for both agents (`'CLIAgentRecognizer'`) and no agent finding, the protocol clients zip the harness note (`'CLICopilotRecognizer'`), the account lookup zip four PO030, the resume-matching zip one PO030, the handover and responses-analyzer zips and the local `init` and `pack` (zip and folder) one PO028 each, the Dataverse indexer zip no agent finding or note; every run exits 0, as Steps item 5 expected
- the same reviewer's second pass 2026-11-03, with the build in the tree, tested in a copy outside the repo: R1 to R13 hold (55 mutants, 49 failing a test, 1 not compiling); 1 Medium and 3 Low new (R14 to R17), fixed: valid YAML whose keys the YAML layout's converter can't map no longer refuses an agent file (Q6 amended), the five surviving mutants now fail tests, the BUILD-PLAN row's old fixture bullets, and a sentence on the PO031 page
- the same reviewer's third pass 2026-11-03: R14 to R17 hold and R1 to R13 still do (27 mutants, 4 surviving); 2 Low new (R18, R19), fixed: a tool file that can't be mapped now gives a `note:` from PO027 instead of being left out silently (Q6 amended, `Agent.UnreadFiles`), and tests for the YAML layout's key messages, an agent file's depth and budget refusals and its `@xmlns` skip
- the same reviewer's fourth pass 2026-11-03: R18 and R19 hold and R1 to R17 still do (22 mutants, 3 surviving: two answered by R21's tests, one an equivalent change); 3 Low new (R20 to R22), fixed: the unread-file note now begins with one rule id, so the cap line past 20 notes names PO027 cleanly, two `UnreadFiles` tests, and row 7 names the CLI test
