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
2. "An unmanaged import does not delete this from the target; the zip no longer has it." (recommended after research 1 and 2)

Decided: 2. Learn supports that nothing is deleted from the environment, and says nothing about whether the solution's component list keeps the component (research 2). "Does not delete" claims only the supported half and doesn't suggest the import touches the component; neither wording says anything about solution membership.

### Q5. A dedicated golden
1. Yes, `deploy-plan-stays.json`, from the seed as an unmanaged zip over an unmanaged install (recommended)
2. No; Core and MCP tests only

Decided: 1. `OutputSchemaTests` validates every `deploy-plan*` golden against the schema, so the new golden is what proves the enum accepts `stays`; it costs one test, as M37's `deploy-plan-same-version.json` did.

## Assumptions and research

Claims research (a `powerops-researcher` subagent, Opus, Learn and open sources only, read 2026-11-12). Ecosystem: the `pac` row of `docs/ecosystem/compatibility.md` (pinned 2.14.1) and the open entry ECO-0012 (no documented change to `pac solution import` from 2.13.1 to 2.14.1) were verified 2026-11-10, inside 90 days; the claim is Dataverse import behaviour, not `pac`'s, so neither changes it and no refresh is needed.

1. An unmanaged import over an installed unmanaged solution deletes nothing from the environment: **confirmed in part**. *Upgrade or update a solution*: under Update, a component the new version lacks is not deleted and remains in the system; only upgrade and apply-upgrade delete (the page speaks of managed solutions). *Solution concepts*: solution upgrades delete components no longer included; update and patch can't. *Import solutions* describes an unmanaged import as adding the zip's components and overwriting their customizations, and names no removal. Only the on-premises 9.1 page *Create, export, or import an unmanaged solution* states it outright ("an additive process"). The upgrade-failure troubleshooting page quotes "The imported solution and the existing solution must both be managed for upgrade". Confidence medium to high; flows, environment variables and connection references are not named separately on any page.
2. Whether the target *solution* keeps the component in its component list: **not stated** on any page read. Recorded as `unproven:` in the plan; the message (Q4) avoids it.
3. An unmanaged zip over a managed install: the import fails, "Import can only update solutions when the modes match" (*The solution is installed as a managed one*, https://learn.microsoft.com/troubleshoot/power-platform/dataverse/working-with-solutions/the-solution-is-installed-a-managed-one). Changed Q1.
4. No `pac solution import` option deletes missing components on an unmanaged import: `--force-overwrite` overwrites customizations of components in the zip only; `--import-as-holding`, `--stage-and-upgrade` and `pac solution upgrade` are the managed upgrade path; `--convert-to-managed` is deprecated (*Performance recommendations*) and absent from the current *pac solution* reference, and doesn't delete either way.
5. The upgrade plan's citation "ALM-066" is Learn's warning against clone patch and clone solution, unrelated to this claim; the research note's practitioner findings ALM-F12, F40 and F47 agree with the claim in practice.

## Log

- 2026-11-12: planned from the M106 section of `reports/practitioner-research/upgrade-plan.md` (revision 2, approved by the Fable plan review 2026-11-12), its header, sections 3, 4, 6, 7 ("Defaults you can change") and 11, the chain rules' "Upgrade milestones" section and `m98-done-handover.md`, in its own worktree at `e185b56`.
- read on 2026-11-12: `DeployPlan.cs` (`PlanKind`, `Build`, `AddUpgrade`, `AddSameVersion`, `HashText`); `DeployPlanFile.cs`; `JsonOutput.cs`; `DeployPlanReport.cs`; `deploy-plan.schema.json`; `docs/schemas/README.md`; `docs/COMMANDS.md` (`deploy plan`, the `diff` version aside); `DeployCommand.cs` (the refusal and the `agent` read); `McpCommand.cs` (the two deploy tools); `action/*.sh` and `action.yml`; `skills/powerops-implement/SKILL.md`; the frozen roles (read only); `docs/RECOVERY.md`; decisions 48 and 65 in `docs/BUILD-PLAN.md`; M37's and M73's plans (the two earlier kind additions); the tests named in the plan's row 4 and `tests/fixtures/drift/seed-test.json`.
- depth: quick: the upgrade plan gives the design.
- areas not asked: users, automation, environments and ALM, screens, solution and publisher; the change is to this repo's report and docs, and the plan answers each or marks it not applicable.
- 2026-11-12: claims research back (see above): Q1 changed to no `stays` over a managed install, Q4 to the "does not delete" wording, and an `unproven:` line for solution membership.
- approved 2026-11-12 by the building session, under the owner's approval of the upgrade plan ("Before trial 4", 2026-11-12), for the Fable plan review and a fresh-context review.
