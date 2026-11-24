# Decisions: m98-apply-readback

The authority for this milestone is the owner's approval of the upgrade plan drawn from the practitioner research: asked "When should the upgrade tools land?", having been told that their answer on timing is the go-ahead to build, the owner answered "Before trial 4" (2026-11-12), which is the plan's Q1 (a): M98 to M109 land before the fourth trial. Under the owner's standing instructions, sessions sign off their own milestone plans after clean reviews and merge their own verified pull requests when all checks pass (2026-10-22 and 2026-10-21, repeated 2026-11-09 as "Sessions sign off" and "Sessions merge"). Their direction for this work (2026-11-11): "as autonomous as possible", so routine choices below were made by the building session (Opus) and logged. The trial-3 answers (Q1 to Q4 of 2026-11-11) don't apply: this milestone asks for no yes, prints no URL or value, and touches no canvas package or kept role.

The upgrade plan lists this milestone's two visible changes under "Defaults you can change": a version mismatch now exits `1` instead of `0`, and a failed read after a flowless import exits `3` instead of `0`.

## Round 1 (planning)

### Q1. Which rows get `after`?
1. Only rows whose import is `ok`; every other row `null` (recommended)
2. Every row of the plan when the read ran, compared only for `ok` rows

Decided: 1. The read answers "did what pac reported happen", which is a question only for an import that ran. A `skip` row's target state is already in the plan's hash; a value there that is never compared would invite a reader to judge it by hand. The trial-4 brief line (upgrade plan section 8, R-readback) asks for `after` on every imported solution, which this meets.

### Q2. A run that imported nothing
1. No read, exit `0`, as today (recommended)
2. Read anyway

Decided: 1. With every solution a `skip`, nothing changed and the plan read the target moments before; a read would only spend a `pac org who` and Web API reads.

### Q3. Compare `managed` too?
1. Report it, don't compare it (recommended)
2. A managed zip that shows unmanaged is a mismatch

Decided: 1. The upgrade plan's M98 section compares presence and version only, and `deploy plan` already blocks a managed import over an unmanaged solution of the same name. The value is shown so a reader can see it.

### Q4. A version that is null or doesn't parse
1. A mismatch, with `unknown` in the message for a null one (recommended)
2. Skip the comparison

Decided: 1. A version that doesn't parse as four numeric parts is stored as null (after review R5), so only a version reaches the report. `DeployPlan.EqualVersions` treats it as unequal already. A false alarm sends the reader to the import history; skipping would let a failed import read as done, which is what this milestone stops.

### Q5. Where the `after:` text line goes
1. Indented under its solution line: `  after: 1.0.0.1 (managed)` (recommended)
2. A separate block after the flows

Decided: 1. The upgrade plan says "per row"; under the row keeps the pair together for a reader of a long plan.

## Assumptions and research

Claims research (a `powerops-researcher` subagent, Sonnet, open sources only, read 2026-11-12). Ecosystem: the `pac` row of `docs/ecosystem/compatibility.md` and the open entry ECO-0012 were verified 2026-11-10, inside 90 days; no refresh needed.

1. A completed staged upgrade leaves the solution at the zip's version: **not stated outright** on Learn. *Upgrade or update a solution* says the upgrade "upgrades your solution to the latest version" and that the apply step renames the holding solution to the base name. `(unproven: a staged upgrade observed in a tenant, the solution's version read after it; first chance is trial 4's R-readback line)`.
2. A completed plain import of a higher version leaves that version: **not stated outright**; the same page says an update "replaces your solution with this version" (managed); nothing for unmanaged. Same probe.
3. The solution history lists each import with its version and result: **confirmed**, *View the history of a solution* (Name, Start and End time, Version, Operation, Result Success or Failure; records older than 180 days are deleted). `docs/RECOVERY.md` already cites it.
4. `pac solution import` exiting `0` after a failed import: **reported, not documented**: two GitHub issues outside Microsoft (one open since 2026-10-10, one closed 2026-08-12, the second hedged); no Learn statement either way. The docs keep "reported".
5. `pac solution import` has `--async` and `--max-async-wait-time` (default 60 minutes) on the *pac solution* reference page; it doesn't say what happens without `--async`. Not needed for this change.

Claims 1 and 2 decide only whether a good import can ever be reported as a mismatch. If either is false in some case, the result is a false exit `1` that sends the reader to the import history, never a false exit `0`; the decision in `BUILD-PLAN.md` says so, and trial 4's R-readback line is the first observation.

## Log

- 2026-11-12: planned from the M98 section of `reports/practitioner-research/upgrade-plan.md` (revision 2, approved by the Fable plan review 2026-11-12), its header, sections 3, 4, 6, 7 ("Defaults you can change") and 11 (WATCH for M98), and the chain rules' "Upgrade milestones" section, in its own worktree at `e664e7f`.
- read on 2026-11-12: `DeployCommand.Apply` (the import loop, `Finish`, the flowless return, the second read and its catch, `ReadAgents`); `DeployApply.cs`; `DeployPlan.EqualVersions`; `Snapshot.cs`; `DriftLiveReader` (the not-installed snapshot); `DeployApplyReport.cs`; `deploy-apply.schema.json`; `DeployFakes.cs` (`FakeTarget.After`, `FailAfter`); `DeployApplyCommandTests.cs`; `docs/COMMANDS.md` `deploy apply`; `docs/RECOVERY.md`; `action.yml` (`Apply plan result`); `docs/GITHUB-ACTION.md`; `docs/BUILD-PLAN.md` decisions 60 to 64; M89's plan folder.
- depth: quick: the upgrade plan gives the design.
- areas not asked: users, automation, environments and ALM, screens, solution and publisher; the change is to this repo's report and docs, and the plan answers each or marks it not applicable.
- approved 2026-11-12 by the building session, under the owner's approval of the upgrade plan ("Before trial 4", 2026-11-12), for the Fable plan review and a fresh-context review.
- 2026-11-12: plan reviews of the first version. Fable (PLAN REVIEW, Fable 5.2): REJECTED, 4 findings (1 Medium, 3 Low). Fresh-context `powerops-review-plan` (Sonnet): 0 High, 3 Medium, 2 Low (`review.md`, R1 to R5). Answered in `plan.md`: the MCP fakes' second reads return the zip's version, with a good-path and a mismatch MCP test pinned to `IsError` false (Fable 1, R3); the step is `Apply plan result` (Fable 2); the schema's top-level and `solutions` descriptions and an `after` description (Fable 3); the `action.yml` line moves into step 3 so step 3 ends green (Fable 4); the `unproven:` line probed by step 6 (R1); the leak scan's place (R2); `SeedAfter` at 1.0.0.1 and an explicit `After` per flipped test, following Fable's WATCH over R3's suggested default (a default that answers the zip's version by itself would hide a test that never set the read); the full read's cost (R4); a version that doesn't parse is null (R5). Fable's WATCH notes added to the plan: the baseline run and a red on assertions, the flow-off-and-mismatch test, the skip-only read count, honesty about the unobserved claim in COMMANDS.md and the decision.
- 2026-11-12: plan review round 2. Fable: APPROVED, executor opus for steps 2 to 5 in sequence, the building session for 1, step 6 a post-milestone probe. Its two tidy-ups were applied (the Assumptions bullet that stated the version claim as fact now says only what is reported; this Log names `Apply plan result`), and its WATCH on the MCP fakes was written into row 5 (keyed on an import having run, not on a read count). Fresh review second pass: R1 to R5 confirmed, no new finding.
- 2026-11-12: fresh review third pass on the tidied body: no finding open, `review.md` current.
- signed off 2026-11-12 by the building session under the owner's approval of the upgrade plan (asked "When should the upgrade tools land?", answered "Before trial 4", 2026-11-12) and the standing instruction that sessions sign off their own milestone plans after clean reviews, with Fable's approval and no finding open (`powerops plan sign-off`, run from this worktree's build).
- 2026-11-12: build (a `powerops-implementer` subagent, Opus, test-first; b2ae80b code and tests, 64179b3 docs): baseline 5134 tests green; red with the record and an empty `SolutionsAfter`, failing on assertions only; green 5160 (5154 passed, 6 skipped); golden diff the six `deploy-apply*` files, `after` fields and one `after:` line only. Leak scan 0 hits, control found. Fable WORK REVIEW: PASS (its own build, tests, `RoleEvalTests`, leak scan, a reproduced red, three mutants killed, and a probe showing no existing test sits on the mismatch path). Its wording note on COMMANDS.md applied ("reads the target no more"). The pull request is titled `feat(deploy)!:` because two exit codes change, as Fable agreed. Status done before the merge.
