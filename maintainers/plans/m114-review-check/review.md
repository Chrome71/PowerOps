plan-hash: c841d33a7f387cb2ba3975cf047410d4fd446a9bde380d32068a6b9e1ff209c6
model: claude-opus-5-5
fresh-context: yes

Round 3: this review checks R6 and R7 of round 2 against plan revision 3 (`db9145a`), rechecks R1 to R5, and adds R8 and R9. The planning chat is not in this context; `plan.md`, `decisions.md` (its Log included) and the round 2 `review.md` were read. The working tree holds one uncommitted edit of `decisions.md` by the main session (Q4's count of `Resolution:` lines, 1,113 to 1,112), which now matches the lines under `docs/plans/` (1,112, first words as Q4 lists them).

Environment facts were not read: the request gave no confirmation that `pac` is signed in, and the plan's `environment:` is null (tenant: offline). No `pac` or `az` call was made. `plan status` ran from the worktree build (`dotnet run --project src/PowerOps.Cli`): status `approved`, review `done`, 0 open questions, 0 admin questions, no violation; its one warning is this file's old plan-hash, which this rewrite replaces.

This plan changes PowerOps itself, not a Power Platform solution. Checklist items 1 (solution and publisher), 2 (connection references, environment variables), 3 (licences), 4 (DLP), 5 (Dev to Test to Prod), 6 (managed or unmanaged), 7 (delegation), 8 (security roles) and the per-product parts of 11 do not apply: the plan names no table, list, flow, screen, app, connector or environment, and says "None" under Automation, Screens, Environments and route, and Solution and publisher. No finding is raised for them.

Tools the steps name, checked with `command -v` and `ls`: `dotnet`, `git` and `gh` are on PATH; `ci-watch.sh` and `leakscan.sh` are in the main checkout's git-ignored `reports/` folder, as Assumptions says. The Fable reviewer and the Opus executor are agents, not checked.

Code and docs read for this round: `src/PowerOps.Core/Plans/PlanSignOff.cs` and `PlanChecker.ReviewProblems` (what a re-sign needs), the three `docs/COMMANDS.md` sentences row 6 (b) replaces (lines 725 to 727, present), and the first words of every `- Resolution:` line under `docs/plans/`. No `verify.md` or `security-solution.md` in the repository, and no test or source file, has a `### High` section today, so the new count changes no existing fixture.

## Findings

### High

#### R1. The existing "stale" route tests can't pass unchanged once the fixture is a valid sign-off
- Area: 13 (buildability: two plan lines that can't both hold), 9 (testability)
- Finding: Done 5 said every test that used `ReviewedPlanText` passes unchanged, while the two cases that make the reports stale by editing the plan body (`Uncovered("stale")` in `DeployApplyCommandTests.cs`, the `stale` row of `DeployReviewWarningTests.cs`) would also make the signed plan "changed since sign-off" and so gain the plan's fault in their sentence.
- Suggested fix: Name these two cases with their new expected sentences, and keep the stale-report-alone sentence pinned by a case that re-signs the edited body.
- Resolution: fixed. Data row 5 names both cases with the sentence `docs/plans/m: plan.md has no valid sign-off (changed since sign-off); verify.md ...; security-solution.md ...`, which is what row 3 (c) gives through `Problem`'s `"; "` join; Done 5 says only those two expected sentences change; tests 8 (b), (c) and (h) add the re-signed stale-report-alone cases. Revision 3 left rows 3, 5 and 8 (b), (c), (h) unchanged, so this still holds.

### Medium

#### R2. The sign-off warning reads a `state:` line that `review.md` never has
- Area: 13 (buildability), 11 (size)
- Finding: The warning fired on a `state: blocked` or `failed` line, which no `review.md` the review role writes carries.
- Suggested fix: Base the warning on an open High under `### High` in `review.md`, counted with row 2's rule, or drop it.
- Resolution: fixed. Decisions Q6 picks the open-High count; row 2 counts `OpenHigh` for all three reports, row 4 warns when `review.md`'s count is above 0, and test 8 (g) uses a `review.md` in the review role's format. Still holds under revision 3: `accepted` is a closing word, so a High a person accepts in `review.md` stops the warning, and test 8 (g) pins `fixed` and `accepted`.

### Low

#### R3. The docs inventory misses three places that describe the check or sign-off
- Area: 9 (testability), 13 (buildability)
- Finding: Row 6 missed `skills/powerops-implement/SKILL.md` line 87, `docs/GITHUB-ACTION.md`'s `require-reviews` input and `COMMANDS.md`'s `plan sign-off` paragraph, and pointed at a `PLANNING-SKILLS.md` anchor that doesn't exist.
- Suggested fix: Add them to row 6 or record them as left with the reason, and give row 6 an anchor that exists.
- Resolution: fixed. Row 6 (a), (c), (d) and (f) name `deploy plan`'s cover list, the `plan sign-off` paragraph, both descriptions with the golden recipe, and the `GITHUB-ACTION.md` sentence (line 80); the implement skill and `docs/RECOVERY.md` are under Won't build with reasons that hold. Rechecked: the three `COMMANDS.md` sentences row 6 (b) quotes are at lines 725 to 727, unchanged.

#### R4. "No unpack call" holds only for zips linked by solution-hash
- Area: 9 (testability)
- Finding: `WarnUnreviewed` unpacks a zip that any report names as its deploy-hash before `Problem` runs, so a deploy-hash link under an unsigned plan is unpacked before the refusal.
- Suggested fix: Say the six route cases link by solution-hash, and state the unpack before the refusal.
- Resolution: fixed. Done 1, Data row 3 (d) and tests 8 (c) say so, Won't build keeps the unpack, and test 8 (b) pins a deploy-hash link under an unsigned plan in Core; unchanged in revision 3.

#### R5. Steps don't say which `powerops` build runs, or where the chain's scripts are
- Area: 12 (tools)
- Finding: The `powerops` on PATH is older than the branch, and `ci-watch.sh` and the leak scan are not in the worktree.
- Suggested fix: Run the worktree's build, and name where the scripts run from.
- Resolution: fixed. Constraints names `dotnet run --project src/PowerOps.Cli -- <args>` for `plan status` and `plan sign-off`; Assumptions says both scripts run from the main checkout's `reports/` folder, where they exist; unchanged in revision 3.

#### R6. An unrecognised resolution wording reads as not open
- Area: 13 (buildability), 9 (testability)
- Finding: Row 2 read a High finding as open only when its first `Resolution:` word was `open` or `reopened` (or the value was empty or missing); any other first word, such as a hand-written "Not done", read as resolved, and Q4 didn't say that unknown wording fails open, or why.
- Suggested fix: Record that unknown wording reads as resolved, with the reason, or read such wording as open and add parser cases to 8 (a).
- Resolution: fixed. Revision 3 takes the stronger form: Data row 2 closes a finding only on a listed closing word and reads everything else as open (`not done`, `no change needed`, `(a) accepted ...`, unknown words), and Q4 gives the reason (a fail-closed refusal or warning is cleared by writing a closing word; fail-open would let an oddly worded High cover a zip). Test 8 (a) adds `Not done (reason)`, an unknown word (`later`) and every closing word. The list matches the first words of the 1,112 `Resolution:` lines under `docs/plans/` except `done` (R8), and the 122 role reports parse the same under it, since the roles write only `open` before a person edits them.

#### R7. The recovery line doesn't fit the "changed since sign-off" reason
- Area: 9 (testability: each refusal has a way out)
- Finding: Row 6 (b)'s single recovery line, "set `status: approved` and run `plan sign-off`", left both reports stale for a changed body, so the zip stayed refused.
- Suggested fix: Split the line: for a changed body, revert the edits, or re-sign and run both roles again; for the other reasons, sign off with `plan sign-off`.
- Resolution: fixed. Row 6 (b) now gives recovery by reason: `status draft`, `status approved` and `signed without the tool` re-sign an unchanged body; `changed since sign-off` reverts, or re-signs and reruns both roles; an open High is fixed and the role rerun, or a person accepts it and deploys without `--require-reviews`. Checked against `PlanSignOff.Run`: a re-sign needs `status: approved` and a review that isn't pending, and a stale `review.md` is a warning there, not an error, so each path ends in a valid sign-off; where a step is missing (a draft's pending review), `plan sign-off` names it in its own error.

#### R8. `done` is in the closing-word list, but no `Resolution:` line uses it
- Area: 13 (buildability: a plan line that its own data doesn't support)
- Finding: Data row 2 lists `done` among the closing words and says the list is "the closing words the `Resolution:` lines under `docs/plans/` use (Q4)"; Q4 says the same and gives the counted first words, which include `kept` but not `done`. None of the 1,112 lines starts with `done`. The rule is safe either way (a closing word nobody writes changes no result today), but the stated source of the list is not true of one entry, and a reader of the docs sentence of row 6 (a) can't tell why `done` closes a High.
- Suggested fix: In Q4 (outside the plan hash), say `done` is added beyond the observed words and why (for example, it is the plan status word for finished work); or drop `done` from row 2, Q4 and test 8 (a).
- Resolution: fixed. Q4 in `decisions.md` now says why `done` is on the list (no plan-body change): the kept roles and the plan skill use `done` as the word for a finished check or step, so a person closing a finding may write it; leaving it off would only make that a fail-closed refusal.

#### R9. Two of the six sign-off reasons have no recovery line
- Area: 9 (testability: each refusal has a way out)
- Finding: Row 6 (a) gives six reasons for "plan.md has no valid sign-off", and row 6 (b) gives "Recovery, by reason" for four of them and for an open High. `could not be read` and `malformed header` have none, and the refusal sentence echoes no header text (row 1), so the person isn't told where the fault is.
- Suggested fix: Add one clause to row 6 (b): for `could not be read` or `malformed header`, run `powerops plan status` on the folder, which names the parse error, and fix the file.
- Resolution: accepted (a Low, by the building session, logged in `decisions.md`): the plan body stays as reviewed so this review stays current; the step 4 brief to the executor carries the clause ("for `could not be read` or `malformed header`, run `powerops plan status` on the folder, which names the problem, and fix the file"), and the Fable work review checks it is in `docs/COMMANDS.md`.
