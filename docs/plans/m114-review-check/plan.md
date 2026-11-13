---
status: draft
review: pending
environment: null
depth: standard
supersedes: null
signed-off: null
signed-off-hash: null
---

# M114: The shared review check reads the plan's sign-off and open High findings

## Goal

Carry out decision 1(a) of the review-enforcement audit (2026-11-13; `decisions.md` Q1): the one evaluator every guarded import route uses, `DeployReviews` (Core), refuses a zip whose covering plan folder's `plan.md` is not validly signed off, and refuses a `ready` `verify.md` or `security-solution.md` that lists a High finding that is still open. Every route reaches it through the same call (`DeployCommand` for `deploy plan`, `deploy apply --require-reviews`, the MCP tool `deploy_apply` and `deploy gate`; the Action runs `deploy apply --require-reviews` by default), so the change is made once. `plan sign-off` also warns when the folder's `review.md` is `blocked` or `failed`.

Done when:

1. With `--require-reviews` (CLI), through the MCP tool `deploy_apply` with `apply: true`, and through `deploy gate` on an import call, a zip whose only covering folder has otherwise valid reports is refused, with no `pac` import or unpack call, when that folder's `plan.md` is: `draft` (`review: pending`); `approved` and never signed; a malformed header (`status: nonsense`); `signed-off` with a `signed-off-hash` that is not the body hash; `signed-off` with `signed-off-hash: null`. These are the audit's `[R-A ...]` rows (`evidence/repro/R/scratch-tests.txt`) plus the fixture's own old form.
2. On the same three routes, a zip is refused when one of the two reports is `state: ready` and lists, under `### High`, a finding whose `Resolution:` is `open` (the audit's `[R-B]`).
3. On the same three routes, a folder whose `plan.md` is `signed-off`, `implementing` or `done` with a `signed-off-hash` equal to its body hash, and whose two reports are `ready`, current, linked and list no open High, still covers: the CLI and MCP import through the fakes, and the gate passes (exit 0). A report with no `## Findings` section, as every test fixture today, has no open High.
4. `plan sign-off` on an `approved` plan whose `review.md` says `state: blocked` or `state: failed` signs off (exit 0) and writes one `warning:` line naming the state; `ready` and `dropped` give no such line.
5. The test fixture `ReviewedPlanText` (`tests/PowerOps.Cli.Tests/DeployFakes.cs` and its copy in `McpCommandTests.cs`) is a valid sign-off, and every test that used it passes unchanged otherwise.
6. The docs say what the check now reads (Data row 6), and `docs/BUILD-PLAN.md` has a decision that reverses M75's two "Won't build" lines (row 7).
7. `tests/eval/` is byte-for-byte unchanged, the two kept roles' text and plugin copies are unchanged, `RoleEvalTests` passes, no golden changes, and the checks of step 4 pass.

### Won't build

- Blocking on `not verified` checks, a stage or target policy, recorded exceptions (an acceptance a route reads), any check of who ran `plan sign-off`, the settings file (`settings-hash:` is M96's), gate rules for PowerOps's own commands (M115), and everything under the packet's "Not proposed".
- Any edit to `skills/powerops-verify`, `skills/powerops-review-security`, their plugin copies or `tests/eval/`. The findings are read in the format those roles already prescribe; if that proved unreadable without a role change, the milestone would stop and report (it did not: Assumptions).
- A new command, option, file format, report header line, JSON field or `schemaVersion`. `plan status` output (text and JSON) is unchanged: the new fields are not printed.
- A refusal for a superseded plan (Q3): a superseded folder whose own plan is validly signed off still covers its zips.
- Reading `review.md`'s state or the plan's `review:` header in the deploy check (Q2): only status and sign-off hash.
- A change to the `Evidence` notes of `deploy plan` (they describe ready reports of the same solution; they are evidence, not a cover), or to the warning-only path of a bare `deploy apply`, beyond its warning lines now naming the new reasons.
- Refusing `plan sign-off` over a blocked or failed `review.md`: a warning only (packet E-7).

## Users

The person who deploys, and the agent that runs the guarded routes for them. Nothing they type changes; a refusal message gains two reasons.

## Data

Code shapes and rules. `<file>` is `verify.md` or `security-solution.md`.

1. **The plan's sign-off, per folder.** `PlanReviews` (`src/PowerOps.Core/Plans/PlanChecker.cs`) gains one positional member `string? NotSignedOff`, required (no default), so every constructor states it. `PlanChecker.Reviews` fills it from the folder's `plan.md`, using the conditions of the existing block in `PlanChecker.Check` (status, `signed-off` and `signed-off-hash` against the body hash):
   - (a) `plan.md` can't be read or parsed: `could not be read`; malformed header: `malformed header`;
   - (b) status `draft` or `approved`: `status <status>`;
   - (c) status `signed-off`, `implementing` or `done` with `signed-off` or `signed-off-hash` null: `signed off without the tool`;
   - (d) the same statuses with `signed-off-hash` not the body hash: `changed since sign-off`;
   - (e) otherwise null (valid).
   One `internal static` helper computes it and `PlanChecker.Check` uses the same helper for its two existing violations (c) and (d), so `plan status` output stays byte-identical (its tests pin the texts). The `review:` header and `review.md` are not part of it (Q2). `supersedes` is not read (Q3).
2. **Open High findings, per report.** `PlanReportFile` gains `int OpenHigh = 0` (last parameter), filled by `ReadReports` for the two solution reports from the text it already reads (`ReadScannable`, capped at 1 MB): the count of findings in a `### High` section that are open. Rule, line by line over the whole text, each line trimmed: a line starting `## ` (two hashes and a space) ends any section; a line starting `### ` starts a section, which is the High section when the rest, trimmed, is `High` (ordinal, ignoring case); inside the High section a line starting `#### ` starts a finding; a finding is open when no line starting `- Resolution:` follows it before the next `####`, `###` or `##` line, or when the first word of that line's value, with trailing `.`, `,`, `:` and `;` removed, is `open` (ignoring case). Lines inside a fenced code block (between lines starting with three backticks) are skipped. A report with no `### High` section has 0. `review.md` stays 0. `plan status` doesn't print it.
3. **The faults.** In `src/PowerOps.Core/Deploy/DeployReviews.cs`:
   - (a) `Cover` takes a folder only when `NotSignedOff` is null and both report faults are null.
   - (b) `Ready` gains one last branch, after the stale check: `report.OpenHigh > 0` gives `<file> is ready but lists <n> open High finding(s)` (`1 open High finding`, `2 open High findings`, by the file's `Count` helper).
   - (c) `Problem` puts the plan's fault first: `docs/plans/<plan>: plan.md is not signed off (<reason>); <report faults>`; when both reports are fine the sentence is `docs/plans/<plan>: plan.md is not signed off (<reason>)`. The first folder is chosen as today (ordinal, among those recording the hash).
   - (d) `NeedsUnpack`, `Recorded` and `Evidence` are unchanged.
   So the refusal stays `no review covers <zip>: <sentence>; nothing was imported.`, status `refused`, exit 1 for `deploy apply`, the MCP tool's `refused` result, and the gate's exit 2 with its existing "the dry run did not pass" prefix.
4. **Sign-off warning.** `PlanSignOff.Run` keeps the list `PlanChecker.ReadReports` returns and, when its `review.md` entry has state `blocked` or `failed`, adds the warning `review.md is <state>: read its open findings before the build`. It never adds an error.
5. **The fixture.** `ReviewedPlanText` becomes a `static readonly string` whose `signed-off-hash` is the body hash of the same text, computed with `PlanDocument.Parse(...).BodyHash`, in `DeployFakes.cs` and in `McpCommandTests.cs`. The status stays `signed-off`, the body stays `# Plan\n`, so the reports' `plan-hash` is unchanged.
6. **Docs.**
   - (a) `docs/COMMANDS.md`, `deploy apply`: the sentences "`ready` is the role's own word: ... covers the zip all the same." and "It doesn't read the plan's `status:`, its sign-off or its `review.md` either: only the plan body's hash." become: the check also reads the findings under `### High` and refuses a `ready` report that lists one whose `Resolution:` is `open`; it reads no other finding, no Medium or Low, no check line, so a `ready` report with every check `not verified` still covers; and it refuses when the folder's `plan.md` is not signed off with `powerops plan sign-off` for its current body (status `signed-off`, `implementing` or `done` and a matching `signed-off-hash`), but doesn't read `review.md` or who signed. "The check reads each report's header lines only" is corrected to match. Any other line of that section or of `deploy gate`'s section that says only header lines are read is corrected the same way.
   - (b) `docs/HANDOFF.md` step 8: "both are `ready`, were written for the current plan and are linked to the zip" gains "list no open High finding, and the folder's plan is signed off".
   - (c) `docs/USE-WITH-AGENTS.md`: read for the same picture (lines 16 and 70 say the routes "require both"); change a line only if it says the check reads headers only or ignores sign-off.
   - (d) `docs/schemas/deploy-apply.schema.json`, `status` description: it names "not covered by both review reports of one plan folder", not the reasons, so it stays; checked, not changed. The same for `docs/schemas/deploy-plan.schema.json` and the MCP schema text if they name reasons.
   - (e) `docs/PLANNING-SKILLS.md`: the sign-off paragraph that lists its warnings (near "warn-only") gains the `blocked` or `failed` `review.md` warning.
7. **BUILD-PLAN decision.** A new section `## Decisions, M114 review check (2026-11-13)` with the next free decision number, taken under the merge lock: `--require-reviews` (and so the MCP tool, the gate and the Action) refuses a zip whose folder's plan isn't validly signed off, and a `ready` report that lists an open High; this reverses M75's "Won't build" line "A check of the plan's `status`" (`docs/plans/m75-review-evidence/plan.md:41`) and, in part, "Proof that a role wrote a report. The check reads what the two files say" (`:35`): the check now reads the High findings too, and still proves no authorship. It quotes the owner's "All recommended" (2026-11-13), names what a refusal says, says what stays unread (Medium and Low findings, `not verified` checks, `review.md`, who signed, the settings file until M96) and that a superseded plan still covers (Q3).
8. **Tests, written first, each run and seen failing before the code change.**
   - (a) `tests/PowerOps.Core.Tests/Plans/PlanCheckerTests.cs`: `Reviews` fills `NotSignedOff` for each case of row 1 (a) to (e), including `implementing` and `done` with a good hash (null) and a superseded folder (null); `ReadReports` counts `OpenHigh`: 0 with no findings section, 0 with `No findings.` under `### High`, 1 for an open High, 0 for `Resolution: fixed` and `Resolution: accepted`, 1 for a High with no `Resolution:` line, 0 for an open Medium, 2 for two open Highs, 0 for a `### High` inside a fenced block, 1 for `- Resolution: Open.`; and the `plan status` texts for an unsigned and a changed plan are unchanged (the existing tests).
   - (b) `tests/PowerOps.Core.Tests/Deploy/DeployReviewsTests.cs`: the sentences of row 3 for each `NotSignedOff` reason, alone and with a report fault; open High alone; a stale report with an open High gives the stale sentence; a valid folder covers; a second folder that is valid covers when the first (ordinal) is unsigned, and `Problem` is null then.
   - (c) `tests/PowerOps.Cli.Tests/DeployApplyCommandTests.cs`: a theory over the six refusing cases (row 1's five plans, row 2's open High), each refused with status `refused`, exit 1, the exact message, and no import and no unpack in the fake `pac`; the existing cover tests stay green on the fixed fixture.
   - (d) `tests/PowerOps.Cli.Tests/McpCommandTests.cs`: the same six cases through `deploy_apply` with `apply: true`, refused before the form, with no import.
   - (e) `tests/PowerOps.Cli.Tests/DeployGateCommandTests.cs`: the same six cases as new rows of `An_import_no_review_covers_is_blocked_by_the_dry_run` (exit 2, exact stderr, no import).
   - (f) A valid plan with clean reports, one carrying a `## Findings` section with `### High` `No findings.` and an open Medium, still imports on the CLI and MCP and passes the gate (one case each).
   - (g) `PlanCheckerTests` (or the sign-off tests beside it): row 4's warning for `blocked` and `failed`, none for `ready` and `dropped`, exit 0 each time, the plan signed.
   - (h) `DeployReviewWarningTests`: a bare `deploy apply` (no option) with an unsigned plan imports and prints the warning line with the new sentence (one case).

## Automation

None.

## Screens

None.

## Environments and route

None. No tenant call of any kind (tenant: offline). Every route is exercised through the test seams (`FakePac`, `FakeTarget`, the MCP test server); `deploy plan` and `deploy apply` are never run against a real target.

## Solution and publisher

None.

## Constraints

- AGENTS.md: the smallest change that fully solves the task; new logic with tests, written first; `dotnet format --verify-no-changes` clean; Conventional Commits; no version or changelog edits; fake values only.
- Contracts (packet M114): the existing `refused` status and exit codes as M75 used them (exit 1 for `deploy apply`, the gate's 0 or 2); no `schemaVersion` bump; no new JSON field; `plan status` output byte-identical.
- Old reports and plan folders that are validly signed off and list no open High cover exactly as before.
- Frozen: `skills/powerops-verify`, `skills/powerops-review-security`, their plugin copies, `tests/eval/`. `RoleEvalTests` passes. If a change would alter one of them, stop and report.
- One writer per file at a time. The executor writes only the files of steps 2 and 3; the main session writes the plan folder, the BUILD-PLAN decision number at merge, and the records.
- `DeployGateCommandTests.cs` is also edited by M108, which starts after this merges and rebases over it: keep the new cases inside the existing theory and helpers, with no reshuffle of the file.
- M96 adds `settings-hash:` to the same fault chain in `DeployReviews.Fault`; keep `Fault` and `Ready` as single expressions it can extend.

## Assumptions

- The kept roles' findings format is readable without a role change (checked 2026-11-13): both `SKILL.md` texts prescribe `## Findings`, `### High`, `#### V1.`/`#### S1.` entries with `- Resolution: open`, "No findings." under an empty severity, and define `ready` as "no High finding is open". All 122 `verify.md` and `security-solution.md` written by the role eval runs on this machine (`~/.local/state/powerops-role-eval/`, Claude and Codex, runs of 2026-11-01, 02, 05 and 10, M83's own runs included) parse under row 2: every report has a `### High` heading, every High finding has a `Resolution:` line, all 95 `blocked` reports have an open High, and none of the 27 `ready` reports has one. The findings block of both roles is unchanged since they shipped (`git diff 2b25cc3 HEAD` of the two files touches only the header lines and a step number). `tests/eval/` holds the cases and baselines, not role output, so no eval file changes.
- The packet's reproductions (`evidence/repro/R/scratch-tests.txt`, built from `ccf9833`) show each `[R-A]` row and `[R-B]` importing today; `PlanChecker.Reviews` and `DeployReviews` are unchanged between `ccf9833` and `22dc11f` (M112 was docs only).
- No Microsoft service, CLI or API behaviour is claimed or changed, so no `powerops-researcher` pass and no ecosystem row applies (chain rule step 4).
- `ReadReports` is called by `plan status`, `plan sign-off` and `Reviews`; counting High findings adds a pass over text already read, capped at 1 MB.

## Open questions

## Questions for your admin

## Steps

1. **Plan.** This plan, `decisions.md`, a fresh-context `powerops-review-plan` into `review.md`, the Fable plan review, fixes, sign-off by the building session. Files: `docs/plans/m114-review-check/`. Done when: Fable APPROVED, `review.md` has no open finding, `powerops plan sign-off` exit 0, `powerops plan status docs/plans/m114-review-check` no violation.
2. **Core, tests first.** Data rows 1 to 4 and tests 8 (a), (b), (g). Files: `src/PowerOps.Core/Plans/PlanChecker.cs`, `src/PowerOps.Core/Plans/PlanSignOff.cs`, `src/PowerOps.Core/Deploy/DeployReviews.cs`, `tests/PowerOps.Core.Tests/Plans/PlanCheckerTests.cs`, `tests/PowerOps.Core.Tests/Deploy/DeployReviewsTests.cs`. Executor: Opus. Depends on: 1. Done when: the new tests were run before the code and failed (the report says how many), then pass; `dotnet test --project tests/PowerOps.Core.Tests` green.
3. **Routes and fixture, tests first.** Data row 5 and tests 8 (c) to (f), (h). Files: `tests/PowerOps.Cli.Tests/DeployFakes.cs`, `DeployApplyCommandTests.cs`, `McpCommandTests.cs`, `DeployGateCommandTests.cs`, `DeployReviewWarningTests.cs`. Executor: Opus, the same agent after step 2. Depends on: 2. Done when: the new CLI, MCP and gate cases were run red before step 2's code (the executor writes them first, or runs them against `22dc11f` and reports which failed), then pass; the full `dotnet test` is green.
4. **Docs, records and checks.** Data rows 6 and 7. Files: `docs/COMMANDS.md`, `docs/HANDOFF.md`, `docs/USE-WITH-AGENTS.md` (only if row 6 (c) finds a stale line), `docs/PLANNING-SKILLS.md`, `docs/BUILD-PLAN.md`, `docs/plans/m114-review-check/decisions.md` (Log), the plan's `status:` line. Executor: the main session (docs) after the Fable work review of steps 2 and 3. Done when, on a clean tree: `dotnet build` 0 warnings; `dotnet test` 0 failed; `dotnet format --verify-no-changes` clean; `powerops plan status docs/plans/m114-review-check` no violation; `git diff --stat origin/main -- tests/eval skills/powerops-verify skills/powerops-review-security plugins tests/golden` empty; the leak scan of chain rule step 9 (0 hits, control found); `DocsDriftTests` and `HandoffDocTests` green.
5. **Merge and hand-over.** The chain's lock protocol, the BUILD-PLAN decision number taken under the lock, the pull request, `ci-watch.sh`, squash merge, lock released; `m114-done-handover.md` in the chain folder.
