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
(The text half of option 1 is superseded by round 2's R4.)
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

## Round 2: the fresh review's findings

The first review found 0 High, 2 Medium and 6 Low (R1 to R8). Each is answered here and in `plan.md`.

- R1 (Medium): the seed goldens gain only `components-not-checked 1 table(s)`, because the seed zip names as many tables as the target holds; the pairing with `deletes 1 table(s)` belongs to the unit test with no root components. The blocked and same-version goldens don't change. Fixed in Done when 1 and row 6. (answered by: agent m53-plan-diff)
- R2 (Medium): row 6 names the tests whose expectations gain `same-version` (`SolutionDiffTests`, `DiffCommandTests` with its lines, `DiffReportTests`). The helpers stay as they are, so each expectation keeps showing the real output; giving the new side another version in the `Diff` helper would add a version line to every expectation instead. (answered by: agent m53-plan-diff)
- R3 (Low): the hash half of row 1's test is dropped; the hash is over the inputs, and the existing hash tests cover it. (answered by: agent m53-plan-diff)
- R4 (Low): equal versions now means both are four numeric parts and equal as numbers. Equal text that isn't four numbers gets no label, because `deploy plan` refuses such a zip rather than skipping it, and the label would then say something false. A test is added. (answered by: agent m53-plan-diff)
- R5 (Low): the description now reads "The two solutions differ but carry the same version number, so importing the new one is not an upgrade: deploy plan skips it; raise the version." (answered by: agent m53-plan-diff)
- R6 (Low): row 3 states that the parser's depth limit bounds the walk, and that a flow of another layout keeps the key (it can only show a change, never hide one). `docs/COMMANDS.md` says so. (answered by: agent m53-plan-diff)
- R7 (Low): the Goal states that the item goes only to a managed zip's upgrade, and that the milestone's "non-upgrade action" case is tested as its absence. (answered by: agent m53-plan-diff)
- R8 (Low): `docs/COMMANDS.md` says that the label belongs to the pair, so a consumer may read it from any change. The build doesn't change, as Q4 chose. (answered by: agent m53-plan-diff)
- R9 (Low, second pass): row 6's line numbers in `DiffCommandTests` are examples. Every expectation that lists a change over a pair at one version gains the label, including lines with no risk today. This is recorded here and not in `plan.md`, so the reviewed plan body stays as it is. (answered by: agent m53-plan-diff)

## Log

- Learn checked 2026-11-04 with the Learn MCP tools: "Upgrade or update a solution" and "Solution concepts" (the section "Solution lifecycle"). The quotes are in the plan's Assumptions
- the paths of `operationMetadataId` in the fixtures counted 2026-11-04 with a scratch script, kept outside the repo
- depth: quick: the improvement plan gives the design, and the brief gives the three items
- questions answered by the building session under the user's delegation of 2026-10-22 (autonomous milestone mode)
- areas not asked: automation, screens, environments, solution and publisher; M53 changes this repo's code and reads no tenant
- approved 2026-11-04 by the building session under the user's delegation of 2026-10-22, for a fresh-context review
- fresh review (a general-purpose subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-11-04 at `9cb7b7a`: 0 High, 2 Medium, 6 Low (R1 to R8); answered in `plan.md` (Goal, Done when 1, rows 1, 3 to 6) and here (Round 2)
- code built in parallel with the review by an implementer subagent (Sonnet, `powerops-implementer`) in a separate worktree, test first, one commit per item, then changed to Round 2's R3 to R6 and R8
- the same reviewer's second pass 2026-11-04 at `79b02a7`: R1 to R8 hold; 0 High, 0 Medium, 1 Low new (R9), answered here (Round 2). In all: 0 High, 2 Medium and 7 Low, every one fixed or accepted
- signed off 2026-11-04 by the building session under the user's delegation of 2026-10-22, with no High or Medium finding open (`powerops plan sign-off`; `signed-off-hash` equals `review.md`'s `plan-hash`) (answered by: agent m53-plan-diff)
