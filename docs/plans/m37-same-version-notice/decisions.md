# Decisions: m37-same-version-notice

## Round 1

### Q1. How many items does a difference give?
1. One for each component that differs, as `deletes` does (recommended)
2. One per solution, with counts in the message
3. In your own words
Answer: 1. The name is what the reader acts on, and `Subject` already holds a flow, variable or reference name. The list is as long as the difference, which is short in the case this serves.

### Q2. Does a zip with a lower version get the item?
1. No: only a version equal to the installed one (recommended)
2. Yes: every `skip`
3. In your own words
Answer: 1. An older zip is expected to differ, so an item there says nothing. The equal case is the one where a maker forgot to raise the version. The comparison is numeric, the one that picks the action, so two spellings of one version count as equal.

### Q3. What counts as a difference?
1. Only which components exist: flows by id, variables and references by name, ignoring case (recommended)
2. Also the connector of a reference with the same name
3. Also a flow's name and state
Answer: 1. That is what the review asked for, and it's what `deletes` compares. A connector needs both sides in one format, and nothing shows yet that they are. A flow's state isn't a change to the zip, and `drift` reports it. A rename is a real change that stays unseen, and the docs say so with the changed flow action.

### Q4. Does the item enter the plan hash?
1. No: the hash stays over the inputs (recommended)
2. Yes
Answer: 1. The hash covers the inputs, and the item comes only from them, so the same inputs give the same item. Changing the hash would void plans for no gain.

## Log

- 2026-10-31: plan started at the request of the session that started this one, which asked for one small milestone for W-4 on the review's advice: an information item, no new reads, exit 0, one new `kind`
- read on 2026-10-31: AGENTS.md; `powerops-plan`; `powerops-review-plan`; the M34 and M36 plans (layout); `DeployPlan`, `DeployPlanFile`, `DeployPlanReport`, `DriftLiveReader.ReadTarget`, read 4's query, `Snapshot`, `FlowState.CloudFlows`, `SolutionParser`'s references and variables, the schema, the `deploy plan` paragraphs of `docs/COMMANDS.md`, and the deploy tests
- depth: quick
- approved 2026-10-31 by the building session under the user's delegation of 2026-10-22 (autonomous milestone mode)
- areas not asked: users, data, automation, environments and ALM, and constraints; the change is to this repo, and the plan answers each or marks it not applicable
- fresh review (general-purpose subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-10-31: 0 High, 0 Medium, 5 Low (R1 to R5). R1 to R4 fixed in `plan.md`; R5 accepted in Assumptions, with the reason in `review.md`. The reviewer also tried row 1 in a scratch copy outside the repo: only the two tests the plan names failed
- the same reviewer re-read the fixed plan 2026-10-31: R1 to R5 hold, no new findings
- signed off 2026-10-31 by the building session under the user's delegation of 2026-10-22, with no High or Medium open
