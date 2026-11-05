# Decisions: m53-plan-diff

Answered by the building agent under the delegation of 2026-10-22: agents answer product choices and sign off plans, and the person keeps keys and deletions. Every answer takes the recommended option unless it says otherwise.

## Round 1

### Q1. How does `deploy plan` say that tables and canvas apps are checked by count only?
1. One `components-not-checked` item per type the target's solution holds, subject `<n> table(s)` or `<n> canvas app(s)`, beside the existing `deletes` lower bound (recommended)
2. One item for both types
3. Fold them into the existing item for other types
4. Match them by identity (a new read of the target's schema names)

Answer: 1. The improvement plan asks for an item whenever the target holds any, naming the count. One per type mirrors the `deletes` item, which is also per type and uses the same subject form, so a reader can put the two side by side. Option 3 would merge two different claims: other types are not checked at all, while these are checked by count. Option 4 is the real fix, but it needs a new Web API read and a schema change, which is more than this S milestone; the improvement plan keeps the lower bound. (answered by: agent m53-plan-diff)

### Q2. Which actions get the item, and does it block?
1. Only a managed zip's upgrade, where the `deletes` check runs; severity `info` (recommended)
2. Every action whose target holds tables
3. A warning, or a block unless a flag accepts it

Answer: 1. Learn's "Upgrade or update a solution" says an upgrade deletes the components the new version lacks, and that the Update action leaves them in place. A skip imports nothing, and an install has nothing installed to delete. The upgrade of an unmanaged zip deletes nothing either, which is why `AddUpgrade` already returns before its deletion checks for one. An item on those actions would claim an unchecked deletion that cannot happen. The kind is already `info` by `PlanKind.SeverityOf`, and the improvement plan settles CX-2 at medium, documented, with `plan ok` not promising no deletions. A refusal policy is CX-2's second half and is not in M53. (answered by: agent m53-plan-diff)

### Q3. Where is `operationMetadataId` ignored in `diff`?
1. Only in the `metadata` object of each trigger and each action under `properties.definition`, through `actions`, `else.actions`, `default.actions` and `cases.*.actions` to any depth (recommended)
2. In any object named `metadata`, anywhere
3. Everywhere, as today

Answer: 1. This is the improvement plan's rule: actions and triggers, nested actions included, never inputs. The holders are the ones `FlowDefinition.WalkActions` and `FlowSimulator.Blocks` already walk, so `diff`, `analyze` and the simulator agree on what an action is. Option 2 would still drop the key from a Compose input that holds a `metadata` object. Option 3 is the defect. Every `operationMetadataId` in the repo's fixtures sits at a path option 1 covers (plan, Assumptions), so no existing equal pair becomes a change. Other designer-written keys, such as the rest of `metadata`, keep being compared, as they are today. (answered by: agent m53-plan-diff)

### Q4. Where does `same-version` go in `diff`'s output?
1. In the `risks` of every change, so that a consumer that reads `risks` sees it without new code (recommended)
2. A new root field, such as `risks: ["same-version"]`, printed once
3. A pseudo-change `changed solution version` that carries it
4. On the first change only

Answer: 1. Risks live only on changes today; option 1 adds no new place for them. A consumer built on `changes[].risks` sees the label at once. With option 2, a consumer that ignores unknown fields, as the schema README tells it to, would miss it. Option 3 counts a change that didn't happen in `summary` and the text. Option 4 is arbitrary. The cost is repetition: every row of a same-version diff carries the label. That includes the pull request comment of a team that sets the version at build time and never in source; for them the label is always there. This is accepted. The label is true for the files compared, because `deploy plan` would `skip` a zip built from them unchanged. The Markdown explains it once, in its list of labels above the table. (answered by: agent m53-plan-diff)

### Q5. When are two solutions "the same solution at the same version"?
1. Unique names equal ignoring case, and versions equal as text or as four numeric parts compared as numbers, the way `deploy plan` compares them; at least one change (recommended)
2. Versions equal as text only, whatever the unique names

Answer: 1. The risk exists because `deploy plan` would skip the import. `deploy plan` matches a zip to its target by unique name ignoring case, and it compares versions as numbers (`Equal_numeric_versions_in_another_spelling_count_as_the_same_version`). With two unique names, the import installs a second solution and skips nothing. With `1.0.0.0` → `1.0.0.00`, `diff` prints a version change, yet `deploy plan` would still skip, so that change carries the label too. The numeric test is one new internal method on `DeployPlan` over its existing `ParseVersion` and `Compare`, so the two commands cannot disagree. (answered by: agent m53-plan-diff)

### Q6. Does `diff`'s `schemaVersion` move?
1. Yes, to 3, and the PR title is `feat!:` (recommended)
2. No

Answer: 1. `docs/schemas/README.md` says adding a value to a closed list such as `risks` bumps `schemaVersion`, from `diff` 2 on. M51 left the version at 2 for M53 to move. M49's PR, which moved it to 2, was titled `feat!:`. `deploy-plan` stays at 1: its `kind` enum already has `components-not-checked`, and the new item adds no field. (answered by: agent m53-plan-diff)

### Q7. Where is this recorded?
1. Here, with no `docs/BUILD-PLAN.md` section or numbered decision (recommended)

Answer: 1. The brief for this milestone says so, as M51 did. `JsonOutput`'s comment names M53 beside BUILD-PLAN decision 48. (answered by: agent m53-plan-diff)

## Log

- Learn checked 2026-11-04 with the Learn MCP tools: "Upgrade or update a solution" and "Solution concepts" (the section "Solution lifecycle"). The quotes are in the plan's Assumptions
- the paths of `operationMetadataId` in the fixtures counted 2026-11-04 with a scratch script, kept outside the repo
- depth: quick: the improvement plan gives the design, and the brief gives the three items
- questions answered by the building session under the user's delegation of 2026-10-22 (autonomous milestone mode)
- areas not asked: automation, screens, environments, solution and publisher; M53 changes this repo's code and reads no tenant
- approved 2026-11-04 by the building session under the user's delegation of 2026-10-22, for a fresh-context review
