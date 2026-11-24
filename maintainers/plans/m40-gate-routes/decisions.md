# Decisions: m40-gate-routes

## Round 1

### Q1. How does the gate treat the four new MCP tools?
1. Blocked by name, always, without reading their arguments (recommended)
2. Read their arguments and check them against the plan
3. In your own words
Answer: 1. A plan holds solution imports only, so there is nothing to check a site, page or agent upload against. This is how the gate treats the other routes it can't check, and the Phase 7 plan names them as deny-list routes (decision 41).

### Q2. What form do the hook matchers take?
1. One group per client, the five tool names in one `|` list (recommended)
2. One group per tool
3. One regex for every tool of the server (`mcp__pac-cli__.*`)
Answer: 1. Both clients read a matcher of plain name characters and `|` as a list of exact names: Claude Code's hooks page says so for letters, digits, `_`, `-` and `|`, and Codex's `matcher.rs` (at `rust-v0.163.0` and `rust-v0.166.0`) for letters, digits, `_` and `|`, which its sanitized tool names always are. One group keeps the example short and the old matcher easy to find and replace. A regex would run the gate on every pac MCP call, reads included, and catch no new write tool the gate doesn't know. The Phase 7 plan's fallback, one group per tool, isn't needed.

### Q3. How far does the shell route reach beyond the named spellings?
1. Recognise the named spellings as routes, and add the routes' words to the markers so any other form blocks as unreadable (recommended)
2. Recognise only the named spellings
3. In your own words
Answer: 1. The existing pac routes do the same, so `sh -c 'pa app push'` blocks like `sh -c 'pac code push'`, and a mention in a commit message blocks as an import mention does today. `npx` with its options skipped and the pages aliases `powerpages` and `paportal` are recognised as routes, because Learn says the aliases keep working and the npx options are the common case. Rarer forms (`npx -p <package> pa`, `pa.cmd`) block through the markers.

## Round 2

Asked after the review (R1, R3, R8), answered by the building session.

### Q4. Bare route words as markers, or markers anchored to `pa`?
1. Bare words, with no word character after them, and the false blocks pinned in tests (recommended)
2. Anchored to a `pa` or `power-apps` program word
3. In your own words
Answer: 1. Bare words catch `$PA app push`, the form Microsoft's code-apps skills write, which an anchored marker misses; the existing `code push` marker is bare too. The cost is that `git -C apps push`, `mv pages upload` or a commit message with "app push" block while the hooks are on, which is only steps 7 to 9, with the gate's usual advice to reword. The trailing guard keeps `pushed`, `uploads` and `publisher` quiet. The false blocks the review found are pinned as `Blocks()` rows, so a later change is deliberate.

### Q5. A malformed `Assets/botcomponent_connectionreferenceset.xml`: refuse or ignore?
1. Refuse: the solution is unreadable, exit 3, as for a malformed workflow file (recommended)
2. Ignore the file, as canvas metadata is ignored
3. In your own words
Answer: 1. The file is solution XML that the parser reads for a value, like the workflow and customizations files, which are read strictly. Canvas metadata is kept as text for another reader, which is why it is tolerant. A loader test pins the refusal.

### Q6. Block the flat `$PA push` of the retired CLI?
1. No: document it as a miss and pin it in a test (recommended)
2. Yes, through a `power-apps` word marker
3. In your own words
Answer: 1. The review first suggested a `power-apps` word marker (R1), and its second pass showed the cost (R11): `cd power-apps`, `git -C power-apps status`, `grep -rn power-apps src` and every other verb of the old CLI would block while the hooks are on. It buys little: `shared/cli-binary.md` of Microsoft's code-apps skills resolves `PA` once and caches it, so the probe and the push rarely share a command, and `create-code-app` writes the old CLI's push literally as `power-apps push`, which is a route. `push` alone can't be a marker (`git push`). HANDOFF names `$PA push` among the variable forms the gate doesn't see, a test pins it, and npm's `.cmd` shims are read as routes instead.

## Log

- 2026-11-02: plan started at the request of the session that started this one: build M40 of Phase 7, build-order item 75, from the M40 section of `docs/plans/phase7-power-platform/plan.md` and its BUILD-PLAN row, with decisions 40 to 50
- read on 2026-11-02: AGENTS.md; `powerops-plan`; the Phase 7 plan, its BUILD-PLAN row and decisions 40 to 50; the M37 and M21 plans (layout and the toy-server check); `ImportCall`, `ImportCallTests`, `DeployGateCommandTests`, `HandoffDocTests`, `SolutionParser`, `Solution`, `PO005UnusedConnectionReference` and its tests; `docs/HANDOFF.md`, `docs/COMMANDS.md`'s `deploy gate`, `docs/rules/PO005.md`, `powerops-implement` step 7; Learn's `pac copilot`, `pac pages`, `pac model` and Power Apps CLI references and the `botcomponent_connectionreference` table page; Claude Code's hooks page; Codex's `matcher.rs`, `tools.rs`, `mcp/mod.rs` and `handlers/mcp.rs` at `rust-v0.166.0`; the mapping file in Microsoft's CopilotStudioSamples order-management sample and in vscode-copilotstudio's test data
- depth: quick
- questions answered by the building session, under the user's delegation of 2026-10-22 (autonomous milestone mode), with the Phase 7 plan's scope as the brief
- areas not asked: users, data, automation, environments and ALM, and constraints; the change is to this repo, and the plan answers each or marks it not applicable
- approved 2026-11-02 by the building session under the same delegation
- fresh review (general-purpose subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-11-02: 0 High, 3 Medium, 7 Low (R1 to R10). R1, R2, R3, R4, R5, R6, R7, R8 and R9 fixed in `plan.md`, the code and the docs; R10 partly fixed (the line for existing hook users, `pa.cmd` and a Monitor row are tested) and its docs-list check accepted in Assumptions. Round 2 records the three choices the fixes needed
- built 2026-11-02 before sign-off, by two implementer subagents in their own worktrees from rows 1 to 6 and 11 to 12 as first written, then cherry-picked; the review's fixes were applied on top in this worktree
- the same reviewer's second pass 2026-11-02: R1 to R10 hold, no High or Medium open, 3 new Low (R11 to R13). R11 fixed by dropping the `power-apps` marker (Q6 changed from the first answer, which the review's R1 had suggested), R12 and R13 fixed
- the same reviewer's third and fourth passes 2026-11-02: R11 to R13 hold as fixed, one new Low (R14, an example in Assumption 3) fixed and re-checked; no finding open
- signed off 2026-11-02 by the building session under the user's delegation of 2026-10-22, with no High or Medium open (`powerops plan sign-off`, body hash `e9942659…`); status set to done with the build, the toy-server check and the record line in this branch, before the PR's CI, which the master session checks before it merges
