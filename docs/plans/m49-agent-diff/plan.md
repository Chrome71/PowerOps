---
status: done
review: done
signed-off: 2026-11-04
signed-off-hash: 8e8d8dfae220336b2d3bd9975d17fba3c32fcd778886823689c783b4e1efaa4b
environment: null
depth: quick
supersedes: null
---

# M49: Agent `diff`, gate and docs

## Goal

Show agent changes in `diff`, gate `pac copilot init`, and write the agent hand-off. This is M49 of Phase 7 ([plan](../phase7-power-platform/plan.md), stage two, build-order item 84, release label v0.32.0), the last milestone of the phase, with its "Stops if" rule and D9 (BUILD-PLAN decision 48). M48 is built, so `Solution.Agents` holds each agent's instructions and tools.

D9 is answered: the person took its first option on 2026-11-02 (Phase 7 decisions Q9), so `diff`'s `schemaVersion` goes to 2 with the first new enum value. No eval case holds an agent: `find tests/eval tests/fixtures -path '*bots*' -o -path '*botcomponents*'` finds nothing at `d113fc9`, so no new `diff` line can reach an eval baseline. Neither stop rule fires.

`JsonOutput.SchemaVersion` is one constant, 1, that every JSON output prints, and the plan file of `deploy plan` is checked against it. D9 moves `diff` alone, so `diff` gets its own constant and every other output, the plan file and the snapshot file stay at 1 (decisions Q5).

Done when:

1. `diff` compares each agent's instructions (`agent`, risk `agent-instructions` when they changed) and each agent tool (`agentTool`, risk `agent-tool` when one is added or removed), in text, JSON and Markdown.
2. `diff --format json` prints `schemaVersion` 2; every other output still prints 1. `docs/schemas/diff.schema.json` has `const: 2`, the two component values and the two risk values, and validates the goldens.
3. `deploy gate` blocks `pac copilot init` with or without `--environment`, as a route that asks for no rewrite, and exits only 0 or 2 (decision 39). This is wider than the M49 section's `init --environment` on purpose (decisions Q7).
4. `docs/HANDOFF.md` has a Copilot Studio agent row and step 11, the post-import checklist: Application Insights, manual authentication, Direct Line and web channel security, channels, sharing, a publish before sharing, and a republish after an environment variable changes. It says which live checks are waiting.
5. The docs say what the gate can't see for agents: Apply in VS Code and the Copilot Studio MCP server's `manage_agents`, and that a skill's push or publish runs in Bash with no route, so it passes (decisions Q8).
6. A migration line in `docs/schemas/README.md` and in the `diff` section of `docs/COMMANDS.md`.
7. Tests and goldens for all of it; `RoleEvalTests` passes with no eval file changed, and `powerops-verify` and `powerops-review-security` keep their blob ids (`69208a2ed044a346bbae9877c8cd74d9e4794df9` and `6a6a6a1fdb71eb16d09cf3ed99406d04a0982a1f`).
8. BUILD-PLAN marks M49 built and Phase 7 done, and the FOUNDATION roadmap row says so.
9. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan are clean, and one `feat!:` PR is open (decisions Q11).

### Won't build

- `copilot_*` passthroughs, a remote `powerops mcp`, any evaluation run (M17): the M49 section's "Deliberately doesn't".
- Other agent fields in `diff`: authentication, generative orchestration, the recognizer, topics and knowledge. `analyze` judges the end state of the first three (PO027, PO029, the harness note), and the M49 section asks for tools and instructions only (decisions Q4).
- The instructions text in `diff` output: it can run to 8,000 characters, so a change says "instructions changed", as a flow says "definition changed" (decisions Q3).
- A tool that M48's reader leaves in `Agent.UnreadFiles` (its YAML keys can't be mapped): `diff` doesn't see it, and `analyze`'s PO027 note names it.
- A gate route for a skill's `manage-agent.bundle.js push` or `publish`: outside the M49 section, and the bundle is experimental with no stable argument list; the docs say it passes (decisions Q8).
- Routes for `pac copilot create` or `delete`: deletes and creates stay behind `powerops-implement` step 7's yes, as M40 decided.
- A hook matcher change: pac's MCP server has no `copilot_init` tool, and the `Bash` group already sends `pac copilot init` to the gate (decisions Q7).
- A change to `powerops-plan`'s `references/copilot-studio.md`: it already says `init` writes with `--environment` or an active auth profile and needs a yes.
- A version field or migration code: consumers read the schema, and nothing in the repo reads `diff`'s JSON back.

## Users

- A reviewer who reads `diff` on a pull request, through the Action's Markdown comment or the CLI: an added tool and changed instructions now show as risk items.
- A consumer of `diff --format json`, such as the MCP `diff` tool's caller or a script: it now gets `schemaVersion` 2, and one that validates against the published schema rejects the old file until it takes the new one.
- An agent or user with the documented hooks on: `pac copilot init` is now blocked, also without `--environment`.
- A maker who moves an agent to Test or Prod with `deploy apply`, who now has the post-import steps in the hand-off.

## Data

Line numbers are those of `d113fc9`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | `Risk` gains `AgentTool = "agent-tool"` ("An agent tool was added or removed.") and `AgentInstructions = "agent-instructions"` ("An agent's instructions changed."), with their `Describe` lines | `src/PowerOps.Core/Diff/SolutionDiff.cs:19-33` | row 3 |
| 2 | Two blocks at the end of `Compare`, after `canvasApp`. `agent`: `Keyed` over `Agents`, key `SchemaName`, no name, no add-or-remove risk; compare gives `instructions changed` when `Instructions` differ by ordinal compare (null counts as a value), else null; change risk `agent-instructions` when it does. `agentTool`: `Keyed` over every agent's `Tools`, key the tool's `SchemaName`, no name, add-or-remove risk `agent-tool`; compare gives the `Differences` of `action`, `mcp` (`true` or `false`) and `mode`, with no change risk. Removed items keep `removed-component` first, as every `Keyed` block does | `src/PowerOps.Core/Diff/SolutionDiff.cs:80-86` | row 3 |
| 3 | Tests in `SolutionDiffTests` with `B.Agent` and `B.Tool`: an added and a removed tool (`agent-tool`; `removed-component,agent-tool`); changed instructions (`agent-instructions`), from and to null too, and `""` against null; the same instructions, no change; an added agent with two tools (one `agent` line and two `agentTool` lines with `agent-tool`) and the same agent removed (`removed-component`, and `removed-component,agent-tool` for each tool); two tools, and two agents, with the same key in one solution (the first counts, as in every `Keyed` block); agent keys matched ignoring case; a tool moved to another agent under the same schema name (no change); a tool's mode, MCP flag and action changed (one detail, no risk); an added and a removed agent (no risk; `removed-component`); tools of two agents compared as one set; tool keys matched ignoring case; a solution with no agents unchanged. `Risk.Describe` of the two new ids. A reader test in `AgentReaderTests`: a GPT `data` file with `\r\n` line breaks in its instructions reads as the same file with `\n` | `tests/PowerOps.Core.Tests/Diff/SolutionDiffTests.cs`, `tests/PowerOps.Core.Tests/Readers/AgentReaderTests.cs` | itself |
| 4 | `JsonOutput` gains `public const int DiffSchemaVersion = 2;` with a one-line why (decision 48); `SchemaVersion` stays 1 and its summary says every output but `diff` | `src/PowerOps.Core/Output/JsonOutput.cs:5` | row 6 |
| 5 | `DiffReport.WriteJson` writes `JsonOutput.DiffSchemaVersion` | `src/PowerOps.Core/Output/DiffReport.cs:44` | row 6 |
| 6 | `docs/schemas/diff.schema.json`: `schemaVersion` `const: 2`; `component` gains `agent` and `agentTool`; `risks` gains `agent-tool` and `agent-instructions`, and its description names them | `docs/schemas/diff.schema.json:9,20,28-29` | `OutputSchemaTests` |
| 7 | CLI tests in `DiffCommandTests`: two copies of `powerops-seed/v1` in a temp folder with a hand-built agent (`bots/` and `botcomponents/`, M48's shapes): old with instructions `Answer questions about orders.` and an Invoker tool `demo_agent.topic.Orders`; new with other instructions, that tool in Maker mode and a second, MCP tool `demo_agent.topic.Refunds`. Exit 1 and goldens `diff-agents.txt`, `diff-agents.json` and `diff-agents.md`. The `--out` test expects `"schemaVersion": 2` | `tests/PowerOps.Cli.Tests/DiffCommandTests.cs:64` | itself |
| 8 | Goldens by "Update goldens": `diff-seed.json` changes only its `schemaVersion` line; `diff-seed.txt` and `.md` don't change; the three `diff-agents.*` are new | `tests/golden/` | `OutputSchemaTests`, `DiffCommandTests` |
| 9 | `ImportCall`: `RouteCopilotInit = "pac copilot init may import a solution that no plan holds"`, in `Routes`; `case "copilot init"` adds it; the marker `copilot{Gap}(?:push|publish|init)` | `src/PowerOps.Core/Deploy/ImportCall.cs:36,43,79,308-313` | row 10 |
| 10 | `ImportCallTests`: `pac copilot init`, `pac copilot init --environment <id>`, `pac copilot init -env <id> --agent-name x` and `pac.exe copilot init` give the route, which isn't unreadable; `sh -c 'pac copilot init'` and `git commit -m "pac copilot init"` give the can't-read line; `pac copilot pack` and `pac copilot list` still pass. A Bash case of `pac copilot init --environment <id>` in the gate's write-route theory, which gains a command parameter for it: exit 2, the route line and the import guidance on stderr, and the target not read | `tests/PowerOps.Core.Tests/Deploy/ImportCallTests.cs:106-113,319-327,627`; `tests/PowerOps.Cli.Tests/DeployGateCommandTests.cs:199-215` | itself |
| 11 | `docs/HANDOFF.md`: a "Copilot Studio agent" row (built with the `copilot-studio` plugin of microsoft/skills-for-copilot-studio; then `analyze`, `diff` and the import gate; to Test and Prod `deploy apply`, then step 11); step 11, "An agent was imported", with the checklist of Done when 4, the asks-first line and the waiting live checks; the gate's always-blocked list gains `pac copilot init`, with or without `--environment`, and the no-route list loses it and names `pac copilot delete` beside `create` (both write, per Learn's `pac copilot` page); the can't-see line says a skill's push or publish runs `node …/manage-agent.bundle.js` in Bash with no route, so it passes, beside Apply in VS Code and `manage_agents` | `docs/HANDOFF.md:16-25,50,93,99-100` | row 12 |
| 12 | `HandoffDocTests`: the per-product table gains the agent row, which names `` `copilot-studio` ``; the steps run 1 to 11, step 11 names Application Insights, manual authentication, channels, sharing and a republish after an environment variable change | `tests/PowerOps.Cli.Tests/HandoffDocTests.cs:123-152` | itself |
| 13 | `docs/COMMANDS.md`: the `diff` paragraph names agents and agent tools, the two risk labels, and the migration line (`schemaVersion` is 2: version 2 added the agent values, and nothing was removed or renamed); the `deploy gate` paragraph adds `pac copilot init` to the always-blocked routes and one sentence on what it can't see for agents | `docs/COMMANDS.md:49,105` | `DocsDriftTests` if it covers them |
| 14 | `docs/schemas/README.md` "Versioning": a new value in a closed list bumps that output's `schemaVersion`, from `diff` 2 on, with the earlier widenings at version 1 named (decisions Q12); each output's version moves on its own (`diff` 2, the rest 1); and the migration line from `diff` 1 to 2 | `docs/schemas/README.md:23-28` | none, docs only |
| 15 | `docs/FOUNDATION.md`: the Phase 7 roadmap row ends "(met by M40 to M49)"; section 8 gains an open item, "(Phase 7, waiting)", for the agent live checks: data policy, publish state, connection binding and evaluations, which need a Copilot Studio environment | `docs/FOUNDATION.md:85,186` | none, docs only |
| 16 | `docs/BUILD-PLAN.md`: M49 marked built in the Phase 7 table and item 84, whose label becomes `feat!:` (decisions Q11), a verification record line after M48's, the Phase 7 status line set to done; `docs/plans/phase7-power-platform/plan.md`'s header `status: done` (header only, so its sign-off hash holds) | `docs/BUILD-PLAN.md:514,536,553`; the Phase 7 plan's header | `powerops plan status .` |

## Automation

None. `diff` and `deploy gate` change, with no new command, option or tool.

## Screens

None.

## Environments and route

None. No step reads or writes a tenant, and no `pac` or `az` call is made against an environment. The live checks for agents (data policy, publish state, connection binding, evaluations) stay waiting: no Copilot Studio environment is available.

## Solution and publisher

None.

## Constraints

- AGENTS.md: the "Add a command" recipe's JSON steps for the contract change (the schema, the README row, `OutputSchemaTests`, a golden) and "Update goldens"; tests first; no dependency; no write site; fake values in tests; no version or changelog edits; Conventional Commits; `dotnet format` clean.
- A hook command exits only 0 or 2 (decision 39); the new route is one more always-blocked line, so `deploy gate`'s exit codes don't change.
- No eval baseline may change. If one does, the build stops and records what D1 requires before going on.
- No change to `powerops-verify` or `powerops-review-security`, or to any skill text.
- `README.md`'s install section is being changed in another pull request; this plan doesn't touch `README.md`.

## Assumptions

- Learn, checked 2026-11-04. ALM guidance for Copilot Studio: items that aren't solution-aware and need post-deployment steps in the target, namely Azure Application Insights settings, manual authentication settings, Direct Line and web channel security settings, deployed channels, and sharing with makers or users. Export and import agents using solutions: after an import, configure user authentication again, and publish the imported agent before it can be shared. Variables overview: a published agent keeps the environment variable values it was published with, so after an administrator changes one, republish; a secret is read at run time and needs no republish. pac copilot: `init` with `--environment` imports a solution and creates the agent; without it the page calls `init` a local scaffold that needs no sign-in, while the flag's description says it defaults to the active auth profile's environment (M48 decisions Q11).
- The MIT `skills-for-copilot-studio` repo (microsoft, at `9c42d3a612c6e06389b6e5b4eed579aaed1a8b63`, the commit M48 read): its plugin is named `copilot-studio`; its `manage-agent` skill runs `node ${CLAUDE_SKILL_DIR}/../../scripts/manage-agent.bundle.js push` and `publish` through Bash (`allowed-tools: Bash(node *manage-agent.bundle.js *)`), calling the VS Code extension's language server; the skill says the scripts are under active development.
- pac's MCP server as listed in this session has `copilot_publish` and no `copilot_init` tool.
- No eval case or fixture holds an agent (Goal), and the eval baselines hold text output only (Phase 7 Assumptions).
- Nothing in the repo reads `diff`'s JSON back: `action/diff.sh` runs `--format markdown`, the only Action script that reads JSON reads `analyze`'s or `test`'s, the MCP `diff` tool passes the CLI's JSON through, and the skills run `diff` as text (decisions Q5 lists each).
- Environment facts were not read: this plan changes this repo and targets no environment.

## Open questions

## Questions for your admin

## Steps

1. This plan, a review in a fresh context, fixes, sign-off.
2. Rows 3, 7, 10 and 12 and see them fail; then rows 1, 2, 4, 5, 6 and 9 until they pass.
3. Row 8 by "Update goldens", reading the diff; `RoleEvalTests` passes with no eval file changed.
4. Rows 11 to 16; `HandoffDocTests` and `DocsDriftTests` pass.
5. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan (`bash reports/leakscan.sh origin/main <this branch's commit>` from the main checkout); push and open one `feat!:` PR whose body names the `diff` contract change and its migration line, the new gate route, the hand-off row and the waiting live checks. The master session merges.
