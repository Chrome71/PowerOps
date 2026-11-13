# Decisions: m114-review-check

Q1 is the owner's. Q2 to Q5 were decided by the building session (Fable orchestrating, 2026-11-13) inside Q1 and the brief, under the owner's direction for this chain to decide routine choices and log them; none needed the owner.

## Round 1 (planning)

### Q1. Should `--require-reviews` also refuse a plan that isn't validly signed off and a `ready` report that lists an open High?
(a) yes, in the one evaluator every route uses; (b) a doc line only.

Decided: (a), by the owner. Asked in chat "Take the recommended option on all eight decisions?" of the review-enforcement audit's decision packet (`reports/review-enforcement-audit-2026-11-13/decision-packet.md`, git-ignored), the owner answered "All recommended" (2026-11-13); decision 1(a) there is this milestone. Asked "Start M114 now, ahead of M108?", the owner answered "Start M114 now" (2026-11-13).

### Q2. Which plan states count as validly signed off?
1. Status `signed-off`, `implementing` or `done`, with `signed-off` and `signed-off-hash` set and the hash equal to the body hash: the conditions `plan status` already holds for those statuses. `draft`, `approved`, a malformed or unreadable header, a null sign-off and a changed body refuse.
2. The same, plus the plan's `review:` header and `review.md` (pending, missing, blocked).

Decided: 1. A plan is deployed while `implementing` and after `done`, so both must pass with their hash. The brief limits the change to "status and `signed-off-hash` against the body hash"; `plan sign-off` already refuses a pending review, and a `review:` edited after sign-off sits in the header, outside the hash, like every other header edit (packet E-8). `plan status` still reports review problems as violations.

### Q3. Does a superseded plan's folder still cover?
1. Yes, when its own plan is validly signed off.
2. No: a folder that a newer plan's `supersedes:` names covers nothing.

Decided: 1. The audit's `[R-A2]` case is a zip reviewed under plan `m`, deployed after plan `n` names `m` in `supersedes:`. A new version is "left as it is" by the planning skill (`docs/PLANNING-SKILLS.md`, Versions), so the old plan stays the signed record of what its zip was built from, and the zip's SHA-256 binds the reports to those bytes. Refusing it would block promoting an already reviewed zip (Test to Prod) as soon as a v2 draft exists, and a draft names `supersedes:` before anyone has approved it. The deployment yes stays the person's (decision 67). Recorded in the BUILD-PLAN decision as what stays unread.

### Q4. How is an open High read?
Decided: the format both kept roles prescribe (`## Findings`, `### High`, `#### <id>.` entries, `- Resolution: open`), as plan Data row 2 gives it: a High entry is open when its `Resolution:` value's first word is `open`, or when it has no `Resolution:` line; any other value (`fixed`, `accepted`, ...) is not open. Checked against the 122 role reports of the eval runs on this machine (plan Assumptions). A missing `### High` section reads as no open High, so reports without a findings block, as written by the test fixtures and by hand, cover as before (the brief's compatibility contract). Fenced code is skipped so a quoted template is not read as a finding.

### Q5. Where do the new reasons appear?
Decided: in the existing refusal sentence (`no review covers <zip>: docs/plans/<plan>: ...; nothing was imported.`), the plan's reason first, then the report faults, so `deploy plan`'s and a bare `deploy apply`'s `warning:` lines name them too. No new status, field or exit code (the brief's contracts).

## Log

- 2026-11-13: started by the M114 milestone session (Fable orchestrating) in worktree branch `claude/m114-review-check-27a82d`, reset to `origin/main` `22dc11f`. Depth `standard`.
- read on 2026-11-13: `AGENTS.md`; `CLAUDE.md`; `reports/trial-3/chain/chain-rules.md` (whole); `m112-done-handover.md`; the decision packet's first paragraphs, E-1 to E-9, B-7, sections 4, 5 and 8; `evidence/repro/R/scratch-tests.txt`; `evidence/lanes/R.md` (R-2); `src/PowerOps.Core/Deploy/DeployReviews.cs`; `src/PowerOps.Core/Plans/PlanChecker.cs` (`Check`, `Reviews`, `ReadReports`, `ReviewProblems`); `src/PowerOps.Core/Plans/PlanSignOff.cs`; `tests/PowerOps.Cli.Tests/DeployFakes.cs` (`ReviewedPlanText`, `Reviewed`); the review cases of `DeployGateCommandTests.cs`; `skills/powerops-verify/SKILL.md` and `skills/powerops-review-security/SKILL.md` (report format, read only); `docs/COMMANDS.md` `deploy apply`; `docs/HANDOFF.md` step 8; `docs/USE-WITH-AGENTS.md`; `docs/schemas/deploy-apply.schema.json`; `docs/plans/m75-review-evidence/plan.md` Won't build; `docs/plans/m85-gate-routes/` (format).
- 2026-11-13: findings format check (plan Assumptions): a scratch script outside the repository read the 122 `verify.md` and `security-solution.md` under `~/.local/state/powerops-role-eval/` with plan Data row 2's rule; result 95 `blocked` with an open High, 27 `ready` with none, no report without `### High`, no High without `Resolution:`. `git diff 2b25cc3 HEAD` of both role files changes no findings line.
- 2026-11-13: no Microsoft claim; no `powerops-researcher` pass (chain rule step 4).
