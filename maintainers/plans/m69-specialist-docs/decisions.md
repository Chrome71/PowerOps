# Decisions: m69-specialist-docs

## Round 1

### Q1. Which milestone number?
1. M69: M63 is in flight (PR #106) and the live-test improvement plan reserves M64 to M68 (recommended)
2. M64
3. In your own words
Answer: M69: M63 is in flight (PR #106) and the live-test improvement plan reserves M64 to M68 (answered by: agent m69-specialist-docs)

### Q2. Where do the Power BI delete traps go on the hand-off page?
1. A short "Power BI deletes" paragraph under the product table, pointed to from the Power BI row, which keeps its pinned "needs a yes" and "never runs a delete" (recommended)
2. All of it in the row's last cell
3. In your own words
Answer: A short "Power BI deletes" paragraph under the product table, pointed to from the Power BI row, which keeps its pinned "needs a yes" and "never runs a delete" (answered by: agent m69-specialist-docs)

### Q3. How are the references' sources dated?
1. Keep the `Checked on 2026-11-03:` list for what wasn't checked again, and add a `Checked on 2026-11-05:` list for each source checked again or new (recommended)
2. Re-date the whole list to 2026-11-05
3. In your own words
Answer: Keep the `Checked on 2026-11-03:` list for what wasn't checked again, and add a `Checked on 2026-11-05:` list for each source checked again or new (answered by: agent m69-specialist-docs)

## Round 2

### Q4. Learn names the agent tag for a Key Vault secret two ways (`AllowedAgents` in Copilot Studio's Variables page, `AllowedBots` in the Power Apps Key Vault page). What does step 11 say?
1. Name both, with the page that uses each, and `AllowedEnvironments`, which both pages share (recommended)
2. Only `AllowedEnvironments`
3. In your own words
Answer: Name both, with the page that uses each, and `AllowedEnvironments`, which both pages share (answered by: agent m69-specialist-docs)

### Q5. The research says code apps went GA on 2026-02-05; no Learn page says so. What does `code-apps.md` say?
1. No GA claim: say that Learn's overview carries no preview label and sends bugs to standard Microsoft support, with no date (recommended)
2. "Generally available" without a date
3. In your own words
Answer: No GA claim: say that Learn's overview carries no preview label and sends bugs to standard Microsoft support, with no date (answered by: agent m69-specialist-docs)

### Q6. The research names `purgeData`; Learn documents `allowPurgeData` (Power BI REST API, Pipelines Deploy). Which name?
1. `allowPurgeData`, with Learn's meaning: it deletes the target item's data on a schema mismatch (recommended)
2. Both names
3. In your own words
Answer: `allowPurgeData`, with Learn's meaning: it deletes the target item's data on a schema mismatch (answered by: agent m69-specialist-docs)

## Round 3

### Q7. Do the new docs get test pins?
1. Yes, small ones in the existing style: step 10's publish line, step 11's four new items, the nine unrouted writes, the two new reference files and their pointers (recommended)
2. No: docs only
3. In your own words
Answer: Yes, small ones in the existing style: step 10's publish line, step 11's four new items, the nine unrouted writes, the two new reference files and their pointers (answered by: agent m69-specialist-docs)

## Round 4

### Q8. The review (R1) asks which MCP tools the deny guidance names. Is the Power BI Authoring MCP server denied too?
1. No: list it among the writes no gate sees, and deny only FlowAgent's `copy_flow`, `publish_flow` and `update_flow` and the Copilot Studio MCP server's `manage_agents` and `manage_workflows`, because the Power BI server's reads and writes share tools (recommended)
2. Deny the whole Power BI Authoring MCP server while the hooks are on
3. In your own words
Answer: No: list it among the writes no gate sees, and deny only FlowAgent's `copy_flow`, `publish_flow` and `update_flow` and the Copilot Studio MCP server's `manage_agents` and `manage_workflows`, because the Power BI server's reads and writes share tools. Narrowed after R11 to what Learn's page says: it groups the tools by object type, not by read and write (answered by: agent m69-specialist-docs)

### Q9. The review (R4) found the "no route" list repeated in `docs/COMMANDS.md` and the reference list in `docs/PLANNING-SKILLS.md`. Change them here?
1. Yes, one sentence each, so neither goes stale (recommended)
2. No, a later docs pass
3. In your own words
Answer: Yes, one sentence each, so neither goes stale (answered by: agent m69-specialist-docs)

## Log

- 2026-11-05: plan started at the request of the session that started this one: apply backlog row A and the docs part of row B of the specialist-team research (answered by: person, through that session's brief)
- read on 2026-11-05: AGENTS.md; the private research summary and section 4 of the apps, automate, bi, copilot and design reports and section 5 of the workflow report (from the main checkout, git-ignored); `docs/HANDOFF.md`; `skills/powerops-plan/SKILL.md` and its eight references; `SkillsCommandTests`, `HandoffDocTests`, `DocsDriftTests`, `PluginManifestTests`; `ImportCall.cs` (the route regex); the M57 and M62 plan folders (layout)
- every Microsoft claim checked again on 2026-11-05: four Sonnet researcher subagents on Learn (Power BI, Copilot Studio, Power Apps and Power Automate, design and logic), and the orchestrating session with `gh api` on copilot-studio-plugin `4014ae6`, power-platform-skills `4076781` and skills-for-fabric `4c3986d`, and with the Learn MCP tools on code apps and generative pages. Corrected from the research: rollup limits, `allowPurgeData`, the agent tag's two names, the generative pages GA date, no GA statement for code apps, the narrower DirectQuery condition, and XO:05's states
- depth: quick: the brief gives the scope and the files
- questions answered by the building session under the user's delegation of 2026-10-22 (autonomous milestone mode)
- areas not asked: users, data, automation, environments; the change is docs and tests in this repo, and the plan answers each or marks it not applicable
- approved 2026-11-05 by the building session under the user's delegation of 2026-10-22, for a fresh-context review (answered by: agent m69-specialist-docs)
- fresh review (a general-purpose subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-11-05: 0 High, 4 Medium, 6 Low (R1 to R10); it ran the four unrouted `pa` and `pac copilot` commands through `powerops deploy gate` (all exit 0) and spot-checked the Learn and GitHub claims. All ten answered in `plan.md` (Done when 4 and 5, Won't build, rows 1 to 5, 8 and 13, Assumptions), here (Round 4) and in the docs and tests, with the resolutions in `review.md`. The docs and tests were written while it ran (plan step 2, test first: the five new or changed tests failed before the text existed)
- R7's developer page checked by the orchestrating session with the Learn MCP tools: "Specialized columns using code" gives 100 an organization and 10 a table
- the same reviewer's second pass 2026-11-05, with the build in the tree: R1 to R10 hold; 2 Low new (R11, R12), fixed in the deny paragraph, `logic.md`'s first row, plan row 5 and Q8; the third pass 2026-11-05: R11 and R12 hold, 1 Low new (R13), fixed in the deny paragraph only, so the plan body and its hash did not change. In all: 0 High, 4 Medium and 9 Low, none open; `plan-hash` is the plan as signed
- R11's and R13's wording checked by the orchestrating session on Learn's "Power BI Authoring MCP server" page 2026-11-05: tools grouped by object type, create, update and delete among what it does, Build permission alone allows DAX queries only, and the "Work safely" advice
- signed off 2026-11-05 by the building session, outside the skills, under the user's delegation of 2026-10-22, with no High, Medium or Low finding open (`powerops plan sign-off`; `signed-off-hash` equals `review.md`'s `plan-hash`, `5b09f52d`) (answered by: agent m69-specialist-docs)
- status set to done after the build, header only, so the hash holds
