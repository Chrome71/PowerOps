plan-hash: 4e9b5eac5bbb1e6f7348e509bcda46ed382046ffb37025da7cfd41db71b44195
model: claude-opus-5-5
fresh-context: yes

Round 2: this review rechecks R1 to R5 of round 1 against plan revision 2 and the code, and adds R6 and R7. The planning chat is not in this context; `decisions.md` and its Log were read.

Environment facts were not read: the request gave no confirmation that `pac` is signed in, and the plan's `environment:` is null (tenant: offline). No `pac` or `az` call was made. `plan status` ran from the worktree build (`dotnet run --project src/PowerOps.Cli`): status `approved`, 0 open questions, 0 admin questions, no violation.

This plan changes PowerOps itself, not a Power Platform solution. Checklist items 1 (solution and publisher), 2 (connection references, environment variables), 3 (licences), 4 (DLP), 5 (Dev to Test to Prod), 6 (managed or unmanaged), 7 (delegation), 8 (security roles) and the per-product parts of 11 do not apply: the plan names no table, list, flow, screen, app, connector or environment, and says "None" under Automation, Screens, Environments and route, and Solution and publisher. No finding is raised for them.

Tools the steps name, checked with `command -v` and `ls`: `dotnet`, `git` and `gh` are on PATH; `ci-watch.sh` and `leakscan.sh` are in the main checkout's git-ignored `reports/` folder, as Assumptions says. The Fable reviewer and the Opus executor are agents, not checked.

Code read: `src/PowerOps.Core/Deploy/DeployReviews.cs`, `src/PowerOps.Core/Plans/PlanChecker.cs` (`PlanReportFile`, `PlanReviews`, `Check`, `Reviews`, `ReadReports`, `ReadScannable`), `src/PowerOps.Core/Plans/PlanDocument.cs` (statuses, body hash), `src/PowerOps.Core/Plans/PlanSignOff.cs`, `src/PowerOps.Core/Plans/PlanStatusReport.cs` (the JSON fields are listed by name, so a new record member is not printed), `src/PowerOps.Cli/DeployCommand.cs` (`WarnUnreviewed`, the `--require-reviews` description), `src/PowerOps.Cli/McpCommand.cs` (the `deploy_apply` description), `tests/PowerOps.Cli.Tests/DeployFakes.cs`, the review cases of `DeployApplyCommandTests.cs`, `DeployReviewWarningTests.cs`, `DeployGateCommandTests.cs` and `McpCommandTests.cs`, `tests/PowerOps.Core.Tests/Deploy/DeployReviewsTests.cs`, the docs lines row 6 names (`docs/COMMANDS.md` `deploy plan`, `deploy apply` and `plan sign-off`, `docs/HANDOFF.md` step 8, `docs/GITHUB-ACTION.md` `require-reviews`, `docs/USE-WITH-AGENTS.md`, `action.yml`), the two kept roles' findings format and `ready` definition, and `skills/powerops-implement/SKILL.md` line 87. Only `tests/golden/mcp-tools.txt` holds one of the two descriptions. The next free BUILD-PLAN decision number is 70, as row 7 says.

## Findings

### High

#### R1. The existing "stale" route tests can't pass unchanged once the fixture is a valid sign-off
- Area: 13 (buildability: two plan lines that can't both hold), 9 (testability)
- Finding: Done 5 said every test that used `ReviewedPlanText` passes unchanged, while the two cases that make the reports stale by editing the plan body (`Uncovered("stale")` in `DeployApplyCommandTests.cs`, the `stale` row of `DeployReviewWarningTests.cs`) would also make the signed plan "changed since sign-off" and so gain the plan's fault in their sentence.
- Suggested fix: Name these two cases with their new expected sentences, and keep the stale-report-alone sentence pinned by a case that re-signs the edited body.
- Resolution: fixed. Data row 5 names both cases with the sentence `docs/plans/m: plan.md has no valid sign-off (changed since sign-off); verify.md ...; security-solution.md ...`, which is what row 3 (c) gives through `Problem`'s `"; "` join; Done 5 now says only those two expected sentences change; tests 8 (b), (c) and (h) add the re-signed stale-report-alone cases. A search of `tests/` finds no other test that edits a reviewed plan's body or writes a covering report outside `Reviewed` and the MCP copy, so no other expected sentence moves.

### Medium

#### R2. The sign-off warning reads a `state:` line that `review.md` never has
- Area: 13 (buildability), 11 (size)
- Finding: The warning fired on a `state: blocked` or `failed` line, which no `review.md` the review role writes carries.
- Suggested fix: Base the warning on an open High under `### High` in `review.md`, counted with row 2's rule, or drop it.
- Resolution: fixed. Decisions Q6 picks the open-High count; row 2 now counts `OpenHigh` for all three reports, row 4 warns when `review.md`'s count is above 0, and test 8 (g) uses a `review.md` in the review role's format. `PlanSignOff.Run` already calls `ReadReports` and discards the list, so keeping it is the whole change there; `plan status` doesn't print the new member (`PlanStatusReport` lists its JSON fields by name).

### Low

#### R3. The docs inventory misses three places that describe the check or sign-off
- Area: 9 (testability), 13 (buildability)
- Finding: Row 6 missed `skills/powerops-implement/SKILL.md` line 87, `docs/GITHUB-ACTION.md`'s `require-reviews` input and `COMMANDS.md`'s `plan sign-off` paragraph, and pointed at a `PLANNING-SKILLS.md` anchor that doesn't exist.
- Suggested fix: Add them to row 6 or record them as left with the reason, and give row 6 an anchor that exists.
- Resolution: fixed. Row 6 (a), (c), (d) and (f) name `deploy plan`'s cover list, the `plan sign-off` paragraph (`COMMANDS.md` lines 912 to 913), both descriptions with the golden recipe, and the `GITHUB-ACTION.md` sentence (line 80); each anchor exists. The implement skill and `docs/RECOVERY.md` are under Won't build with reasons that hold: both kept roles define `ready` as "no High finding is open", and the skill's gate already requires a signed-off plan. With the skill unchanged, step 5's empty `plugins` diff check stays consistent. Row 6 (h) is now conditional on a line that exists.

#### R4. "No unpack call" holds only for zips linked by solution-hash
- Area: 9 (testability)
- Finding: `WarnUnreviewed` unpacks a zip that any report names as its deploy-hash before `Problem` runs, so a deploy-hash link under an unsigned plan is unpacked before the refusal.
- Suggested fix: Say the six route cases link by solution-hash, and state the unpack before the refusal.
- Resolution: fixed. Done 1, Data row 3 (d) and tests 8 (c) say so, Won't build keeps the unpack, and test 8 (b) pins a deploy-hash link under an unsigned plan in Core.

#### R5. Steps don't say which `powerops` build runs, or where the chain's scripts are
- Area: 12 (tools)
- Finding: The `powerops` on PATH is older than the branch, and `ci-watch.sh` and the leak scan are not in the worktree.
- Suggested fix: Run the worktree's build, and name where the scripts run from.
- Resolution: fixed. Constraints names `dotnet run --project src/PowerOps.Cli -- <args>` for `plan status` and `plan sign-off`; Assumptions says both scripts run from the main checkout's `reports/` folder, where they exist.

#### R6. An unrecognised resolution wording reads as not open
- Area: 13 (buildability), 9 (testability)
- Finding: Row 2 reads a High finding as open only when its first `Resolution:` word is `open` or `reopened` (or the value is empty or missing); any other first word reads as resolved. The roles write `open`, and Assumptions shows the 122 role reports parse, so the deploy routes are not at risk from role output. But reports and reviews are also edited by hand: among the `review.md` files under `docs/plans/` one finding's resolution starts "Not done" (a Low in m62), and others start "no change needed" and "kept on purpose", so a person's "Not done" or "still open" on a High would count as resolved, with no refusal and no sign-off warning. Decisions Q4 lists the closed words but doesn't say that unknown wording fails open, or why.
- Suggested fix: Either record in Q4 and in the docs sentence of row 6 (b) that any other wording reads as resolved, with the reason (a hand edit can change `state:` or remove the finding just as easily, and the check proves no authorship), or read `not` and `still` as open too and add one parser case for each to 8 (a).
- Resolution: open

#### R7. The recovery line doesn't fit the "changed since sign-off" reason
- Area: 9 (testability: each refusal has a way out)
- Finding: Row 6 (b)'s recovery for a plan with no valid sign-off is "set `status: approved` and run `plan sign-off` (a re-sign of an unchanged body keeps the reports current)". For the reason `changed since sign-off` the body is not unchanged, so a re-sign alone leaves both reports stale and the zip still refused. `plan status`'s own violation for the same state says "or revert the edits".
- Suggested fix: Give the line two parts: for a changed body, revert the edits, or re-sign and run both roles again; for the other reasons, sign off with `plan sign-off`.
- Resolution: open
