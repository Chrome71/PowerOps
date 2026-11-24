# Decisions: m43-flow-rules

## Round 1

### Q1. Where are actions, depth and nested loops counted?
1. In `FlowDefinition.Parse`, as three new fields, in one walk of the document it already holds (recommended)
2. In each rule, parsing the flow's JSON again
3. In your own words
Answer: 1. The rules already get the parsed definition from `RuleHelpers.CloudFlows`, and `Parse` is where the JSON is read under its depth and size limits. One walk fills all three, and the rules stay one line of logic each.

### Q2. What is a level of nesting, and does a Switch case add one or two?
1. Any action that holds `actions`, `else.actions`, `default.actions` or `cases.*.actions` is a container, whatever its type; the actions it holds are one level deeper, and a Switch case is one level, like a Condition branch (recommended)
2. Count by type (Scope, If, Switch, Foreach, Until), with a Switch and its case as two levels
3. In your own words
Answer: 1. This is the Phase 7 plan's definition ("a Switch case adding one"), from the research. Learn's Scopes page lists "scopes, conditions, switch cases, and apply-to-each loops" as the levels, which reads as one level per case, and the research counted 19 CoE flows at exactly 8 and none over by this reading, which fits Learn's limit. Reading the structure and not the type also counts a container type the list doesn't name.

### Q3. What does PO017 report for loops three deep, and what is the component?
1. One finding per inner loop, naming its nearest enclosing loop; the component is the inner loop's action name (recommended)
2. One finding per flow
3. In your own words
Answer: 1. Each inner loop is a place to fix, and the baseline matches on component and message, so a finding per loop stays stable when another loop is added elsewhere. PO012 reports per trigger in the same way.

## Log

- 2026-11-03: plan started at the request of the session that started this one: build M43 of Phase 7, build-order item 78, from the M43 section of `docs/plans/phase7-power-platform/plan.md` and its BUILD-PLAN row, with decisions 40 and 42
- read on 2026-11-03: AGENTS.md; the Phase 7 plan's M43, "The pinned roles" and "Won't build" sections, D1 and D3; the M40 and M42 plan folders (layout); the Power Automate research report and its sources list; `FlowDefinition`, `RuleHelpers`, `PO004`, `PO011`, `PO012`, `Analyzer`, `Builders`, `WarningRuleTests`, `RuleDocsTests`, `DocsDriftTests`; `docs/rules/PO012.md` and the rules README; Learn's Scopes, Limits and anti-patterns pages; OverFlow's `SKILL.md` and licence at `f898c77`
- `RoleEvalTests` at `3597789`, before any change: 19 passed
- counts measured before the build with a scratch script applying Q2's definition: 6 of 28 fixture flows over the PO016 line and 1 with a nested loop; no eval flow over 4 actions or depth 1, and none with a nested loop
- depth: quick, as the Phase 7 plan's step 6 sets
- questions answered by the building session, under the user's delegation of 2026-10-22 (autonomous milestone mode), with the Phase 7 plan's scope and the master session's brief
- areas not asked: users, automation, environments and ALM, and constraints; the change is to this repo, and the plan answers each or marks it not applicable
- approved 2026-11-03 by the building session under the same delegation
- built 2026-11-03 before sign-off: rows 1 to 7 by an implementer subagent (Sonnet) in this worktree, test first (the Core tests failed to compile before the rules existed), read by the building session; row 8 by "Update goldens", whose diff added exactly the six PO016 and one PO017 fixture results and the two rule entries, with no skill, plugin or eval file changed
- fresh review (general-purpose subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-11-03: 0 High, 2 Medium, 5 Low (R1 to R7); its own script matched row 8's counts and found no eval flow that fires. R1, R2, R4, R5, R6 and R7 fixed in `plan.md`, the code, the tests and the pages; R3 fixed but for one point, answered in its Resolution. The two Fix sentences changed, so the goldens were updated again by the recipe
- the same reviewer's second pass 2026-11-03: R1 to R7 hold (its R3 point on OverFlow's "triggers count" withdrawn), 2 new Low (R8, R9), both fixed: the walk no longer allocates a list per action, and the pages no longer say a reason is written into the baseline
- the same reviewer's third pass 2026-11-03: R8 and R9 hold, nothing new; no finding open
- signed off 2026-11-03 by the building session under the user's delegation of 2026-10-22, with no finding open (`powerops plan sign-off`, body hash `99bb61ce…`); status set to done with the build in this branch, before the PR's CI, which the master session checks before it merges
