---
status: approved
review: done
environment: null
depth: standard
supersedes: null
signed-off: null
signed-off-hash: null
---

# M114: The shared review check reads the plan's sign-off and open High findings

## Goal

Carry out decision 1(a) of the review-enforcement audit (2026-11-13; `decisions.md` Q1): the one evaluator every guarded import route uses, `DeployReviews` (Core), refuses a zip whose covering plan folder's `plan.md` has no valid sign-off, and refuses a `ready` `verify.md` or `security-solution.md` that lists a High finding that is still open. Every route reaches it through the same call (`DeployCommand` for `deploy plan`, `deploy apply --require-reviews`, the MCP tool `deploy_apply` and `deploy gate`; the Action runs `deploy apply --require-reviews` by default), so the change is made once. `plan sign-off` also warns when the folder's `review.md` lists an open High finding (Q6).

Done when:

1. With `--require-reviews` (CLI), through the MCP tool `deploy_apply` with `apply: true` (refused before the form), and through `deploy gate` on an import call, a zip whose only covering folder has otherwise valid reports, linked by solution-hash, is refused with no `pac` call at all when that folder's `plan.md` is: `draft` (`review: pending`); `approved` and never signed; a malformed header (`status: nonsense`); `signed-off` with a `signed-off-hash` that is not the body hash; `signed-off` with `signed-off-hash: null`. These are the audit's `[R-A ...]` rows (`evidence/repro/R/scratch-tests.txt`) plus the fixture's own old form. A zip linked by deploy-hash under such a plan is still unpacked locally (a temp folder `pac solution unpack` makes and the run deletes) before the refusal, as today; nothing is imported (Data row 3 (d)).
2. On the same three routes, a zip is refused, with no `pac` call, when one of the two reports is `state: ready` and lists, under `### High`, a finding that row 2 reads as open (the audit's `[R-B]`).
3. On the same three routes, a folder whose `plan.md` is `signed-off`, `implementing` or `done` with a `signed-off-hash` equal to its body hash, and whose two reports are `ready`, current, linked and list no open High, still covers: the CLI and MCP import through the fakes, and the gate passes (exit 0). A report with no `## Findings` section, as every test fixture today, has no open High; a report whose checks are all `not verified` and whose only open finding is Medium still covers.
4. `plan sign-off` on an `approved` plan whose `review.md`, in the review role's format, lists an open High signs off (exit 0, the plan signed) and writes one `warning:` line with the count; a `review.md` whose High findings are all `fixed` or `accepted`, or that has `No findings.` under `### High`, gives no such line. `plan status` output is unchanged.
5. The fixture `ReviewedPlanText` (`tests/PowerOps.Cli.Tests/DeployFakes.cs` and its copy in `McpCommandTests.cs`) is a valid sign-off that also passes `plan status`. Every test that used it passes; the only expected sentences that change are the two body-edit cases of Data row 5, and a new stale-report-alone case keeps the old sentence pinned.
6. The docs and the two option descriptions say what the check now reads (Data row 6), and `docs/BUILD-PLAN.md` has a decision that reverses M75's two "Won't build" lines (row 7).
7. `tests/eval/` is byte-for-byte unchanged, the two kept roles' text and plugin copies are unchanged, `RoleEvalTests` passes, `plan status` text and JSON are byte-identical for existing plans, the golden diff is only the changed description lines (`tests/golden/mcp-tools.txt` and any help golden that holds `--require-reviews`), and the checks of step 5 pass.

### Won't build

- Blocking on `not verified` checks, a stage or target policy, recorded exceptions (an acceptance a route reads), any check of who ran `plan sign-off`, the settings file (`settings-hash:` is M96's), gate rules for PowerOps's own commands (M115), and everything under the packet's "Not proposed".
- Any edit to `skills/powerops-verify`, `skills/powerops-review-security`, their plugin copies or `tests/eval/`. The findings are read in the format those roles already prescribe; if that proved unreadable without a role change, the milestone would stop and report (it did not: Assumptions).
- An edit of `skills/powerops-implement/SKILL.md` (its section 3 lists the cover conditions as "ready, were written for the current plan, and are linked"). That sentence stays true as a list of necessary conditions: the implement skill already requires a signed-off plan, and the roles write `ready` only with no open High. An edit would regenerate both plugins' copies for no change in what an agent does.
- `docs/RECOVERY.md`'s exit-1 row: "get both reports of one plan folder to cover the zip" stays true; the new recovery sentences go in `docs/COMMANDS.md`, which it links.
- A new command, option, file format, report header line, JSON field or `schemaVersion`. `plan status` output (text and JSON) is unchanged: the new fields are not printed.
- A refusal for a superseded plan (Q3): a superseded folder whose own plan has a valid sign-off still covers its zips.
- Reading `review.md` or the plan's `review:` header in the deploy check (Q2): only status and sign-off hash.
- Skipping the unpack of a deploy-hash-linked zip under an unsigned plan (Data row 3 (d)).
- A change to the `Evidence` notes of `deploy plan` (they describe ready reports of the same solution; they are evidence, not a cover), or to the warning-only path of a bare `deploy apply`, beyond its warning lines now naming the new reasons.
- Refusing `plan sign-off` over an open High in `review.md`: a warning only (packet E-7), and a `state:` line in `review.md` (Q6).

## Users

The person who deploys, and the agent that runs the guarded routes for them. Nothing they type changes; a refusal message gains two reasons.

## Data

Code shapes and rules. `<file>` is `verify.md` or `security-solution.md`.

1. **The plan's sign-off, per folder.** `PlanReviews` (`src/PowerOps.Core/Plans/PlanChecker.cs`) gains one positional member `string? NoSignOff`, required (no default), so every constructor states it. `PlanChecker.Reviews` fills it from the folder's `plan.md`, using the conditions of the existing block in `PlanChecker.Check` (status, `signed-off` and `signed-off-hash` against the body hash):
   - (a) `plan.md` can't be read or parsed: `could not be read`; malformed header: `malformed header`;
   - (b) status `draft` or `approved`: `status <status>`;
   - (c) status `signed-off`, `implementing` or `done` with `signed-off` or `signed-off-hash` null: `signed without the tool`;
   - (d) the same statuses with `signed-off-hash` not the body hash: `changed since sign-off`;
   - (e) otherwise null (valid).
   The reason strings are fixed; no header text is echoed. One `internal static` helper computes (c) and (d) and `PlanChecker.Check` uses it for its two existing violations, whose texts stay as they are, so `plan status` output stays byte-identical (its tests pin the texts). The `review:` header and `review.md` are not part of it (Q2). `supersedes` is not read (Q3).
2. **Open High findings, per report.** `PlanReportFile` gains `int OpenHigh = 0` (last parameter), filled by `ReadReports` for all three reports (`review.md` too, for row 4) from the text it already reads (`ReadScannable`, capped at 1 MB): the count of findings in a `### High` section that are open. Rule, line by line over the whole text (CRLF or LF), each line trimmed:
   - lines between two lines starting with three backticks (a fenced block) are skipped;
   - a line starting `## ` ends any section; a line starting `### ` starts a section, which is the High section when the rest, trimmed, is `High` ignoring case;
   - inside the High section a line starting `#### ` starts a finding, which ends at the next `####`, `###` or `##` line;
   - the first line of a finding starting `- Resolution:` decides; a finding is open when it has no such line, when the value after the colon is empty, or when the value's first word, lowercased, with trailing `.`, `,`, `:`, `;` and `)` removed, is `open` or `reopened` (`docs/plans/m51-agent-layout/review.md` writes `Reopened`). Any other value (`fixed`, `fixed.`, `accepted (...)`, `resolved`, `withdrawn`) is not open.
   A report with no `### High` section has 0. `plan status` doesn't print it.
3. **The faults.** In `src/PowerOps.Core/Deploy/DeployReviews.cs`:
   - (a) `Cover` takes a folder only when `NoSignOff` is null and both report faults are null.
   - (b) `Ready` gains one last branch, after the stale check: `report.OpenHigh > 0` gives `<file> is ready but lists <n> open High finding(s)` (`1 open High finding`, `2 open High findings`, by the file's `Count` helper). `Fault` and `Ready` stay single expressions (M96 extends `Fault`).
   - (c) `Problem` puts the plan's fault first: `docs/plans/<plan>: plan.md has no valid sign-off (<reason>); <report faults>`; when both reports are fine the sentence is `docs/plans/<plan>: plan.md has no valid sign-off (<reason>)`. The first folder is chosen as today (ordinal, among those recording the hash).
   - (d) `NeedsUnpack`, `Recorded` and `Evidence` are unchanged, so a deploy-hash link is unpacked before the plan's fault is read; the six route cases of row 8 link by solution-hash, and one Core case pins a deploy-hash link under an unsigned plan.
   So the refusal stays `no review covers <zip>: <sentence>; nothing was imported.`, status `refused`, exit 1 for `deploy apply`, the MCP tool's `refused` result, and the gate's exit 2 with its existing "the dry run did not pass" prefix.
4. **Sign-off warning (Q6).** `PlanSignOff.Run` keeps the list `PlanChecker.ReadReports` returns and, when its `review.md` entry has `OpenHigh > 0`, adds the warning `review.md lists <n> open High finding(s): only a person accepts one`. It never adds an error. `plan status` gives no such warning (its output stays byte-identical).
5. **The fixture.** `ReviewedPlanText` becomes a `static readonly string` with `review: skipped` and a `signed-off-hash` equal to the body hash of the same text, computed with `PlanDocument.Parse(...).BodyHash`, in `DeployFakes.cs` and in `McpCommandTests.cs`. The status stays `signed-off`, the body stays `# Plan\n`, so the reports' `plan-hash` is unchanged. Two existing cases edit the plan body to make the reports stale (`Uncovered("stale")` in `DeployApplyCommandTests.cs`, used by its two theories, and the `stale` row in `DeployReviewWarningTests.cs`); their expected sentences gain the plan's fault first: `docs/plans/m: plan.md has no valid sign-off (changed since sign-off); verify.md was not written for the current plan; security-solution.md was not written for the current plan`. A new CLI case and a Core case re-sign the edited body (its new hash written as `signed-off-hash`) with the old reports, and keep the stale-report-alone sentence pinned.
6. **Docs and descriptions.** Each item changed, or checked and recorded in the step's report as left with the reason.
   - (a) `docs/COMMANDS.md`, `deploy plan` (the cover bullets and the list of sentences): a cover also needs the folder's `plan.md` to have a valid sign-off (status `signed-off`, `implementing` or `done`, with the `signed-off-hash` that `powerops plan sign-off` wrote for the current body) and neither report to list an open High finding (row 2's rule in words); the sentence list gains `plan.md has no valid sign-off (<reason>)` with its six reasons, first in the sentence, and `<file> is ready but lists <n> open High finding(s)`.
   - (b) `docs/COMMANDS.md`, `deploy apply`: the sentences "The check reads each report's header lines only ...", "`ready` is the role's own word: ... covers the zip all the same." and "It doesn't read the plan's `status:`, its sign-off or its `review.md` either: only the plan body's hash." become: it reads each report's header lines and the findings under `### High`; it refuses a `ready` report that lists an open High; it reads no other finding, no check line, so a `ready` report with every check `not verified`, or with an open Medium or Low, still covers; it refuses a folder whose `plan.md` has no valid sign-off, but doesn't read `review.md` or who signed. Recovery, in one line: for a plan with no valid sign-off, set `status: approved` and run `plan sign-off` (a re-sign of an unchanged body keeps the reports current); for an open High, fix it and run that role again, or a person accepts it and runs `deploy apply` without `--require-reviews`. Any other line of the `deploy apply` or `deploy gate` section that says only header lines are read is corrected the same way.
   - (c) `docs/COMMANDS.md`, `plan sign-off` (the paragraph listing its warnings): add the open-High warning.
   - (d) `src/PowerOps.Cli/DeployCommand.cs`, the `--require-reviews` description, and `src/PowerOps.Cli/McpCommand.cs`, the `deploy_apply` description: add "from a plan with a valid sign-off" and "and list no open High finding" to the conditions they list; regenerate the goldens by the "Update goldens" recipe and read the diff.
   - (e) `docs/HANDOFF.md` step 8: "both are `ready`, were written for the current plan and are linked to the zip" gains "list no open High finding, and the folder's plan has a valid sign-off".
   - (f) `docs/GITHUB-ACTION.md`, the `require-reviews` input: the "A report covers a zip when ..." sentence gains the two conditions.
   - (g) `docs/USE-WITH-AGENTS.md`: read for the same picture (lines 16 and 70 say the routes "require both"); change a line only if it says the check reads headers only or ignores sign-off.
   - (h) `docs/PLANNING-SKILLS.md`: change only a line that lists `plan sign-off`'s warnings or says the deploy routes ignore the plan's status, if one exists.
   - (i) `docs/schemas/deploy-apply.schema.json`, `status` description: it names "not covered by both review reports of one plan folder", not the reasons, so it stays; checked. The same for `docs/schemas/deploy-plan.schema.json` if it names reasons.
7. **BUILD-PLAN decision.** A new section `## Decisions, M114 review check (2026-11-13)` with the next free decision number (70 at `22dc11f`; renumbered under the merge lock if another lands first): `--require-reviews` (and so the MCP tool, the gate and the Action) refuses a zip whose folder's plan has no valid sign-off, and a `ready` report that lists an open High; this reverses M75's "Won't build" line "A check of the plan's `status`" (`docs/plans/m75-review-evidence/plan.md:41`) and, in part, "Proof that a role wrote a report. The check reads what the two files say" (`:35`): the check now reads the High findings too, and still proves no authorship. It quotes the owner's "All recommended" (2026-11-13), names what a refusal says, says what stays unread (Medium and Low findings, `not verified` checks, `review.md`, who signed, the settings file until M96), that a superseded plan still covers (Q3), and that `plan sign-off` warns on an open High in `review.md` (Q6).
8. **Tests.**
   - (a) `tests/PowerOps.Core.Tests/Plans/PlanCheckerTests.cs`: `Reviews` fills `NoSignOff` for each case of row 1 (a) to (e), including `implementing` and `done` with a good hash (null) and a superseded folder (null). `ReadReports` counts `OpenHigh`: 0 with no findings section; 0 with `No findings.` under `### High`; 1 for an open High; 0 for `Resolution: fixed`, `- Resolution: fixed. <reason>` and `- Resolution: Accepted (reason)`; 1 for a High with no `Resolution:` line followed by a Medium with `Resolution: fixed`; 1 for an empty value; 1 for `Reopened`; 1 for `- Resolution: Open.`; 0 for an open Medium; 2 for two open Highs; 0 for a `### High` inside a fenced block; 1 for `### high` in lower case; 1 for CRLF text; and `review.md` in the review role's format with an open High gives 1. The existing `plan status` tests for an unsigned and a changed plan keep their texts.
   - (b) `tests/PowerOps.Core.Tests/Deploy/DeployReviewsTests.cs`: the sentences of row 3 for each `NoSignOff` reason, alone and with a report fault; open High alone; a stale report with an open High gives the stale sentence; a valid folder covers; a second folder that is valid covers when the first (ordinal) has no valid sign-off, and `Problem` is null then; a deploy-hash link under an unsigned plan with a matching unpack is refused with the plan's sentence; the stale-report-alone sentence (row 5).
   - (c) `tests/PowerOps.Cli.Tests/DeployApplyCommandTests.cs`: a theory over the six refusing cases (row 1's five plans and row 2's open High, reports linked by solution-hash), each refused with status `refused`, exit 1, the exact message, and an empty `pac.Calls`; the two body-edit cases with their new sentences (row 5); a stale-report-alone case on a re-signed plan.
   - (d) `tests/PowerOps.Cli.Tests/McpCommandTests.cs`: the same six cases through `deploy_apply` with `apply: true`, refused before the form, with an empty `pac.Calls`.
   - (e) `tests/PowerOps.Cli.Tests/DeployGateCommandTests.cs`: the same six cases as new rows of `An_import_no_review_covers_is_blocked_by_the_dry_run` (exit 2, exact stderr, no `pac` call), with no other change to the file's layout.
   - (f) A valid plan whose reports carry a `## Checks` section with every check `not verified`, and a `## Findings` section with `No findings.` under `### High` and an open Medium, still imports on the CLI and MCP and passes the gate (one case each).
   - (g) Sign-off (`PlanCheckerTests` beside the existing sign-off tests): a role-format `review.md` with one open High warns `review.md lists 1 open High finding: only a person accepts one`, exit 0, the plan signed; all High `fixed` or `accepted`, or `No findings.`, gives no such line; `plan status` on the same folder prints no new line.
   - (h) `DeployReviewWarningTests`: a bare `deploy apply` (no option) with a plan that has no valid sign-off imports and prints the warning line with the new sentence (one case), and the `stale` row's new sentence (row 5).

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
- Old reports and plan folders that have a valid sign-off and list no open High cover exactly as before.
- Frozen: `skills/powerops-verify`, `skills/powerops-review-security`, their plugin copies, `tests/eval/`. `RoleEvalTests` passes. If a change would alter one of them, stop and report.
- One writer per file at a time. The executor writes the files of steps 2 to 4; the main session writes the plan folder records, renumbers the BUILD-PLAN decision under the lock if needed, and merges.
- `DeployGateCommandTests.cs` is also edited by M108, which starts after this merges and rebases over it: keep the new cases inside the existing theory and helpers, with no reshuffle of the file.
- M96 adds `settings-hash:` to the same fault chain in `DeployReviews.Fault`; keep `Fault` and `Ready` as single expressions it can extend.
- `powerops` commands in this plan (`plan status`, `plan sign-off`) run from this worktree's build, `dotnet run --project src/PowerOps.Cli -- <args>`: the tool on PATH is `0.0.0+b065f23`, older than the branch.

## Assumptions

- The kept roles' findings format is readable without a role change (checked 2026-11-13): both `SKILL.md` texts prescribe `## Findings`, `### High`, `#### V1.`/`#### S1.` entries with `- Resolution: open`, "No findings." under an empty severity, and define `ready` as "no High finding is open". All 122 `verify.md` and `security-solution.md` written by the role eval runs on this machine (`~/.local/state/powerops-role-eval/`, Claude and Codex, runs of 2026-11-01, 02, 05 and 10, M83's own runs included) parse under row 2: every report has a `### High` heading, every High finding has a `Resolution:` line, all 95 `blocked` reports have an open High, and none of the 27 `ready` reports has one (rerun independently by the Fable plan review). The findings block of both roles is unchanged since they shipped (`git diff 2b25cc3 HEAD` of the two files touches only the header lines and a step number). `skills/powerops-review-plan` writes `review.md` in the same findings format (`### High`, `#### R1.`, `- Resolution: open`) with no `state:` line; the 88 `review.md` files under `docs/plans/` all use the `- Resolution:` form.
- The packet's reproductions (`evidence/repro/R/scratch-tests.txt`, built from `ccf9833`) show each `[R-A]` row and `[R-B]` importing today; `PlanChecker.Reviews` and `DeployReviews` are unchanged between `ccf9833` and `22dc11f` (M112 was docs only).
- No Microsoft service, CLI or API behaviour is claimed or changed, so no `powerops-researcher` pass and no ecosystem row applies (chain rule step 4).
- `ReadReports` is called by `plan status`, `plan sign-off` and `Reviews`; counting High findings adds a pass over text already read, capped at 1 MB.
- The leak scan (`reports/leakscan.sh`) and `reports/ci-watch.sh` exist only in the main checkout's git-ignored `reports/` folder; they are run from there, as chain rule step 9 and the merge protocol give them, without touching that checkout's working tree.

## Open questions

## Questions for your admin

## Steps

1. **Plan.** This plan, `decisions.md`, a fresh-context `powerops-review-plan` into `review.md`, the Fable plan review, fixes, sign-off by the building session. Files: `docs/plans/m114-review-check/`. Done when: Fable APPROVED, `review.md` has no open finding, `plan sign-off` exit 0 and `plan status docs/plans/m114-review-check` with no violation, both from the worktree build.
2. **Tests first, red.** Executor: Opus (one agent for steps 2 to 4, in order). Write every test of row 8, the fixture change of row 5 and compile-only stubs (`NoSignOff` set to null by `Reviews`, `OpenHigh` left 0, no new branch in `DeployReviews` or `PlanSignOff`), so the solution builds. Files: the test files of row 8, `tests/PowerOps.Cli.Tests/DeployFakes.cs`, `src/PowerOps.Core/Plans/PlanChecker.cs` (the two record members only). Done when: `dotnet build` passes and `dotnet test` runs; the report lists the new tests that fail (expected: every refusing case, the counting cases above 0, the sign-off warning, the two body-edit sentences, the deploy-hash case) and how many, and any new test that passes against the stubs with the reason (expected: the cover-still-holds cases and the counting cases that expect 0).
3. **Code, green.** Data rows 1 to 4. Files: `src/PowerOps.Core/Plans/PlanChecker.cs`, `src/PowerOps.Core/Plans/PlanSignOff.cs`, `src/PowerOps.Core/Deploy/DeployReviews.cs`. Done when: every test of step 2 passes; full `dotnet test` green; `dotnet format --verify-no-changes` clean; `dotnet build` 0 warnings. One commit for steps 2 and 3 (or two, red then green).
4. **Docs and descriptions.** Data rows 6 and 7, with the "Update goldens" recipe for row 6 (d). Files: `docs/COMMANDS.md`, `docs/HANDOFF.md`, `docs/GITHUB-ACTION.md`, `docs/USE-WITH-AGENTS.md` and `docs/PLANNING-SKILLS.md` (only if row 6 (g) or (h) finds a stale line), `docs/BUILD-PLAN.md`, `src/PowerOps.Cli/DeployCommand.cs`, `src/PowerOps.Cli/McpCommand.cs`, `tests/golden/` (regenerated). Done when: each sentence the docs list matches the code string character for character; `DocsDriftTests`, `HandoffDocTests`, `McpCommandTests` and the golden tests green; the golden diff is only the description lines; the report lists each item of row 6 as changed or left with the reason. One commit.
5. **Work review and checks.** The Fable work review of steps 2 to 4; fixes go back to the executor. Then, on a clean tree, by the main session: `dotnet build` 0 warnings; `dotnet test` 0 failed; `dotnet format --verify-no-changes` clean; `plan status docs/plans/m114-review-check` no violation (worktree build); `git diff --stat origin/main -- tests/eval skills/powerops-verify skills/powerops-review-security plugins` empty; the golden diff as Done 7; the leak scan of chain rule step 9 (0 hits, control found). Then `status: done` (header line only) and the Log.
6. **Merge and hand-over.** The chain's lock protocol (the BUILD-PLAN decision number checked under the lock), the pull request, `ci-watch.sh`, squash merge, lock released; `m114-done-handover.md` in the chain folder.
