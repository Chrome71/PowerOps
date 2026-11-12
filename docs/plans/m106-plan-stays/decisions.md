# Decisions: m106-plan-stays

The authority for this milestone is the owner's approval of the upgrade plan drawn from the practitioner research: asked "When should the upgrade tools land?", having been told that their answer on timing is the go-ahead to build, the owner answered "Before trial 4" (2026-11-12), which is the plan's Q1 (a): M98 to M109 land before the fourth trial. Under the owner's standing instructions, sessions sign off their own milestone plans after clean reviews and merge their own verified pull requests when all checks pass (2026-10-22 and 2026-10-21, repeated 2026-11-09 as "Sessions sign off" and "Sessions merge"). Their direction for this work (2026-11-11): "as autonomous as possible", so routine choices below were made by the building session (Opus) and logged. The trial-3 answers (Q1 to Q4 of 2026-11-11) don't apply: this milestone asks for no yes, prints no URL or value, and touches no canvas package or kept role.

The upgrade plan lists this milestone's visible change under "Defaults you can change": "`deploy plan` (M106) moves to `schemaVersion` 3, so plan files saved before it are planned again."

## Round 1 (planning)

### Q1. An unmanaged zip over a managed install
1. Give the `stays` items too, beside the `managed-conflict` block
2. Give none: that import fails, so nothing is "left" (recommended after research 3)

Decided: 2. The first draft took 1; the claims research found that Dataverse refuses the import when the modes differ ("Import can only update solutions when the modes match", research 3), so an item saying what that import leaves would be false. `managed-conflict` already blocks the plan and says why.

### Q2. Which flows count
1. The same set as `deletes`: every type-29 component of the target's solution whose id is not a workflow of the zip, named by the target read's flow name, else `29 <id>` (recommended)
2. Cloud flows only, as `same-version-differs` compares

Decided: 1. `stays` is the unmanaged twin of `deletes`, so a reader comparing a managed and an unmanaged plan of the same zip sees the same subjects. A classic workflow or business rule left behind is as real as a cloud flow.

### Q3. Where `stays` sorts
1. Last in `PlanKind.Order` and the schema's enum (recommended)
2. Right after `deletes`

Decided: 1. M37 (`same-version-differs`) and M73 (`agent`) both appended; appending keeps the enum diff one value and every existing golden's item order unchanged. Items sort by import order first, so a zip's `stays` items still follow its other items.

### Q4. The message
1. "An unmanaged import leaves this in the target; the zip no longer has it." (the upgrade plan's wording)
2. "An unmanaged import does not delete this from the target; the zip does not have it." (recommended after research 1 and 2 and the Fable plan review)

Decided: 2. Learn supports that nothing is deleted from the environment, and says nothing about whether the solution's component list keeps the component (research 2). "Does not delete" claims only the supported half and doesn't suggest the import touches the component; neither wording says anything about solution membership. "Does not have it" replaces "no longer has it" (Fable plan review, finding 3): an unmanaged solution in a target can hold components added there directly that were never in any zip, and the plan can't see that history.

### Q5. A dedicated golden
1. Yes, `deploy-plan-stays.json`, from the seed as an unmanaged zip over an unmanaged install (recommended)
2. No; Core and MCP tests only

Decided: 1. `OutputSchemaTests` validates every `deploy-plan*` golden against the schema, so the new golden is what proves the enum accepts `stays`; it costs one test, as M37's `deploy-plan-same-version.json` did.

### Q6. Severity and the summary line
1. `info`, and the summary line stays `plan ok` (recommended)
2. `warning`, or a summary-line note as `agent` has

Decided: 1. Nothing is lost or changed by the import: the component is in the target before and after, so the item is information for the person deciding whether to remove it by hand, not a risk of the import. The upgrade plan's M106 section sets `info`. The `agent` note exists because the import may change the agent's published state; `stays` describes no change.

## Assumptions and research

Claims research (a `powerops-researcher` subagent, Opus, Learn and open sources only, read 2026-11-12). Ecosystem: the `pac` row of `docs/ecosystem/compatibility.md` (pinned 2.14.1) and the open entry ECO-0012 (no documented change to `pac solution import` from 2.13.1 to 2.14.1) were verified 2026-11-10, inside 90 days; the claim is Dataverse import behaviour, not `pac`'s, so neither changes it and no refresh is needed.

1. An unmanaged import over an installed unmanaged solution deletes nothing from the environment: **confirmed in part**. *Upgrade or update a solution*: under Update, a component the new version lacks is not deleted and remains in the system; only upgrade and apply-upgrade delete (the page speaks of managed solutions). *Solution concepts*: solution upgrades delete components no longer included; update and patch can't. *Import solutions* describes an unmanaged import as adding the zip's components and overwriting their customizations, and names no removal. Only the on-premises 9.1 page *Create, export, or import an unmanaged solution* states it outright ("an additive process"). The upgrade-failure troubleshooting page quotes "The imported solution and the existing solution must both be managed for upgrade". Confidence medium to high; flows, environment variables and connection references are not named separately on any page.
2. Whether the target *solution* keeps the component in its component list: **not stated** on any page read. Recorded as `unproven:` in the plan; the message (Q4) avoids it.
3. An unmanaged zip over a managed install: the import fails, "Import can only update solutions when the modes match" (*The solution is installed as a managed one*, Learn troubleshooting). Changed Q1.
4. No `pac solution import` option deletes missing components on an unmanaged import: `--force-overwrite` overwrites customizations of components in the zip only; `--import-as-holding`, `--stage-and-upgrade` and `pac solution upgrade` are the managed upgrade path; `--convert-to-managed` is deprecated (*Performance recommendations*) and absent from the current *pac solution* reference, and doesn't delete either way.
5. The upgrade plan's citation "ALM-066" is Learn's warning against clone patch and clone solution, unrelated to this claim; the research note's practitioner findings ALM-F12, F40 and F47 agree with the claim in practice.

## Log

- 2026-11-12: planned from the M106 section of `reports/practitioner-research/upgrade-plan.md` (revision 2, approved by the Fable plan review 2026-11-12), its header, sections 3, 4, 6, 7 ("Defaults you can change") and 11, the chain rules' "Upgrade milestones" section and `m98-done-handover.md`, in its own worktree at `e185b56`.
- read on 2026-11-12: `DeployPlan.cs` (`PlanKind`, `Build`, `AddUpgrade`, `AddSameVersion`, `HashText`); `DeployPlanFile.cs`; `JsonOutput.cs`; `DeployPlanReport.cs`; `deploy-plan.schema.json`; `docs/schemas/README.md`; `docs/COMMANDS.md` (`deploy plan`, the `diff` version aside); `DeployCommand.cs` (the refusal and the `agent` read); `McpCommand.cs` (the two deploy tools); `action/*.sh` and `action.yml`; `skills/powerops-implement/SKILL.md`; the frozen roles (read only); `docs/RECOVERY.md`; decisions 48 and 65 in `docs/BUILD-PLAN.md`; M37's and M73's plans (the two earlier kind additions); the tests named in the plan's row 4 and `tests/fixtures/drift/seed-test.json`.
- depth: quick: the upgrade plan gives the design.
- areas not asked: users, automation, environments and ALM, screens, solution and publisher; the change is to this repo's report and docs, and the plan answers each or marks it not applicable.
- 2026-11-12: claims research back (see above): Q1 changed to no `stays` over a managed install, Q4 to the "does not delete" wording, and an `unproven:` line for solution membership.
- approved 2026-11-12 by the building session, under the owner's approval of the upgrade plan ("Before trial 4", 2026-11-12), for the Fable plan review and a fresh-context review.
- 2026-11-12: plan reviews of the first version. Fable (PLAN REVIEW, Fable 5.2): REJECTED, 6 findings (2 Medium, 4 Low). Fresh-context `powerops-review-plan` (Sonnet): 0 High, 0 Medium, 6 Low (`review.md`, R1 to R6). Answered in `plan.md`: test (h), a managed zip over an unmanaged install keeps today's `deletes` and gets no `stays`, and (f) asserts the full list of kinds (Fable 1, R1); Done when 5 requires the `deploy plan` paragraph to say `stays` covers only three types, so no item does not mean the target matches, and to name the Learn pages with the read date and say the claim is documented, not observed, and silent on solution membership (Fable 2, R3; the decision says the same); "the zip does not have it" (Fable 3, Q4); row 2 fixes the two `JsonOutput` comments (Fable 4); a schema-enum-equals-`PlanKind.Order` test in `OutputSchemaTests` (Fable 5); the exact `kind` description and a required version-2 `deploy apply` case (Fable 6); the separate version-2 `DeployPlanFileTests` case folded into the existing `InlineData` (R2, in part); the probe of the `unproven:` line recorded as a follow-up for trial 4 (R4); Done when 1 to 7 named as the whole requirement (R5); Q6 records why `info` and `plan ok` (R6). Not taken: R2's suggestion to drop the MCP case: the upgrade plan's section 8 and this milestone's brief require a `deploy_plan` MCP case returning `stays`. Fable's WATCH notes are binding on the executor.
- 2026-11-12: plan review round 2. Fable: APPROVED; steps 2 to 4 to sonnet, 1 and 5 to the building session (opus); its WATCH notes bind the executor. Fresh review second pass: R1 to R6 closed, a new Low R7 (the guard tests can't be red first; no step wrote the hand-over note), answered in Steps 2 and 5.
- 2026-11-12: fresh review third pass: R7 closed, a new Low R8 (Done when 6 did not name the guard tests), answered in Done when 6.
- 2026-11-12: fresh review fourth pass: R1 to R8 closed, nothing open (`review.md` current).
- signed off 2026-11-12 by the building session under the owner's approval of the upgrade plan (asked "When should the upgrade tools land?", answered "Before trial 4", 2026-11-12) and the standing instruction that sessions sign off their own milestone plans after clean reviews, with Fable's approval and no finding open (`powerops plan sign-off`, run from this worktree's build).
- 2026-11-12: build (a `powerops-implementer` subagent, Sonnet, test-first; 2d3d2d0 tests, d903ee3 code and goldens, 80e8d6d docs): baseline 5160 tests green at `e185b56`; red run 9 failures, all on assertions, and the six guard tests green before the change; green 5176 (5170 passed, 6 skipped). Golden diff: the `schemaVersion` line of the three `deploy-plan*` JSON goldens and the new `deploy-plan-stays.json` (one `stays` item). Leak scan 0 hits, control found. Fable WORK REVIEW: PASS (its own build, tests, `RoleEvalTests` 20/20, the red run reproduced, a trial merge with `origin/main`, five mutants killed; `snap.Managed != true` survives only for a hand-built installed snapshot with `Managed` null, which a live read never gives, left as is).
- 2026-11-12: under the merge lock, rebased on `727d095` (M99); decision 66 written in `docs/BUILD-PLAN.md`; goldens by the recipe (no change); 5250 tests, 0 failed, 0 warnings, format clean; `tests/eval/`, `skills/` and `plugins/` unchanged. Status done before the merge.
