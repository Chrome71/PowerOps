# Decisions: m71-m63-follow-ups

## Round 1

### Q1. How does an accepted apply of a skip-only plan say that nothing was imported?
1. The status stays `imported`, and the `message` says "nothing was imported: every solution was skipped". The text report says the same, and RECOVERY gets a line. No schema change (recommended)
2. A new `status` value, `skipped`, in the schema, RECOVERY and goldens. Any script that checks for `imported` has to handle it

Answer: 1 (answered by: person, 2026-11-07). The rows already say `skipped`, so the top level needs only to say so in words. A new value would add to a closed list that the Action, RECOVERY and other callers read.

### Q2. What happens to follow-ups 4 and 7, which aren't PowerOps bugs?
1. Note 4 in `powerops-implement`, which already lists steps that Microsoft's tools can't do. Drop 7, because no PowerOps doc uses a transcript (recommended)
2. Drop both, and leave them only in the M63 record

Answer: 1 (answered by: person, 2026-11-07). Learn shows that the admin-roles part of 4 can't be done at all: System Administrator, System Customizer and Environment Maker open every model-driven app (Learn's page *Privileges required to view and access apps*, read 2026-11-07). So the note says to plan access by the users' roles, rather than to remove those roles.

## Log

- 2026-11-07: the person asked for the M63 follow-ups to be planned in the session that finished M63 ("Yes, plan them here"). The milestone is M71: M64 to M68 are reserved by the improvement plan, and M69 and M70 are on an unpushed branch.
- 2026-11-07: Q1's message is written as a sentence, "Nothing was imported: every solution was skipped.", like the other messages of an imported run.
- read on 2026-11-07: the M63 record's seven follow-ups; AGENTS.md; `DeployCommand` (`Apply`, `DryRun`, `Prepare`), `DeployApply.Imports` and `FlowIds`, `DeployApplyReport`, `DriftLiveReader`, `McpCommand`'s `deploy_plan` and `deploy_apply`; `docs/COMMANDS.md` (`drift`, `deploy plan`, `deploy apply`); `docs/RECOVERY.md`; `docs/schemas/deploy-apply.schema.json`; the cost notes in `docs/PLANNING-SKILLS.md` and `docs/USE-WITH-AGENTS.md`; `skills/powerops-implement/SKILL.md`; `docs/examples/playwright/README.md`; `tests/golden/mcp-tools.txt`
- depth: quick: the follow-ups give the design
- areas not asked: users, automation, environments and ALM, screens, solution and publisher; the change is to this repo, and the plan answers each or marks it not applicable
- approved 2026-11-07 by the building session under the user's delegation of 2026-10-22, for a fresh-context review; sign-off is left to the person
- fresh review (a general-purpose subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-11-07: 1 High, 2 Medium, 4 Low (R1 to R7). R1 to R6 answered in `plan.md`: a skipped solution carries no planned flow (`DeployPlan.cs:162-183`), so a skip-only apply ends at one return and row 3 changes only that one (R1); the apply tests are in `DeployApplyCommandTests` (R2); only `Apply` passes the plan's target, so `deploy gate` prints what it prints today (R3); the `status` text, `COMMANDS.md` and HANDOFF step 8 are added (R4, R5); the docs rows say "none (prose)" (R6). R7 not changed: `--target`'s description is at `DeployCommand.cs:56` and the MCP text at `mcp-tools.txt:295` in `15b8d9d`, as the plan says
- the same reviewer's second pass 2026-11-07: R1 to R6 answered, R7 withdrawn (the plan's line numbers are right), no new finding; `plan-hash` set to the revised plan. In all: 1 High, 2 Medium and 4 Low, none open; sign-off is left to the person
- signed off 2026-11-07 by the person ("Yes, M71 first", answering whether to sign off and build M71 before 0.1.0), with no finding open (`powerops plan sign-off`; `signed-off-hash` equals `review.md`'s `plan-hash`)
