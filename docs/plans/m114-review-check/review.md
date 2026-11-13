plan-hash: 46b1a134eee6f7a031e5ca092dd7386be9e5e91e168a950d6b2dc7cae47b85d0
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: the request gave no confirmation that `pac` is signed in, and the plan's `environment:` is null (tenant: offline). No `pac` or `az` call was made.

This plan changes PowerOps itself, not a Power Platform solution. Checklist items 1 (solution and publisher), 2 (connection references, environment variables), 3 (licences), 4 (DLP), 5 (Dev to Test to Prod), 6 (managed or unmanaged), 7 (delegation), 8 (security roles) and the per-product parts of 11 do not apply: the plan names no table, list, flow, screen, app, connector or environment, and says "None" under Automation, Screens, Environments and route, and Solution and publisher. No finding is raised for them.

Tools the steps name, checked with `command -v` and `ls`: `dotnet`, `powerops`, `git` and `gh` are on PATH; `ci-watch.sh` and the leak scan (`leakscan.sh`) exist only in the main checkout's git-ignored `reports/` folder, not in this worktree (see R5). The Fable reviewer and the Opus executor are agents, not checked.

Code read: `src/PowerOps.Core/Deploy/DeployReviews.cs`, `src/PowerOps.Core/Plans/PlanChecker.cs` (`Check`, `Reviews`, `ReadReports`, `ReadScannable`), `src/PowerOps.Core/Plans/PlanSignOff.cs`, `src/PowerOps.Core/Plans/PlanDocument.cs` (statuses), `src/PowerOps.Cli/DeployCommand.cs` (`WarnUnreviewed`), `tests/PowerOps.Cli.Tests/DeployFakes.cs`, and the review cases of `DeployApplyCommandTests.cs`, `DeployReviewWarningTests.cs`, `DeployGateCommandTests.cs` and `McpCommandTests.cs`.

## Findings

### High

#### R1. The existing "stale" route tests can't pass unchanged once the fixture is a valid sign-off
- Area: 13 (buildability: two plan lines that can't both hold), 9 (testability)
- Finding: Done 5 says every test that used `ReviewedPlanText` "passes unchanged otherwise", and step 3 says "the existing cover tests stay green on the fixed fixture". Two existing theories that use the fixture make a report stale by editing the plan body (`Edit(..., "plan.md"), "# Plan", "# Plan, edited")`): the `stale` case of `Uncovered` in `DeployApplyCommandTests.cs` (expected sentence `docs/plans/m: verify.md was not written for the current plan; security-solution.md was not written for the current plan`) and the `stale` row of `A_report_that_does_not_cover_the_zip_gives_one_warning_and_the_same_plan` in `DeployReviewWarningTests.cs`. Once row 5 sets `signed-off-hash` to the body hash of `# Plan\n`, that edit also makes the plan "changed since sign-off", so by row 3 (c) both sentences become `docs/plans/m: plan.md is not signed off (changed since sign-off); verify.md was not written ...`. Done 5 and row 3 (c) can't both hold. It also means the stale-report sentence alone is now reachable only when a plan was signed again after its reports were written, which the plan doesn't say.
- Suggested fix: Name these two cases in row 5 and tests 8 (c) and (h) with their new expected sentences, and reword Done 5 to "passes, with only these expected sentences changed". Or keep each case testing the stale report alone by re-signing the edited body (set `signed-off-hash` to the edited body's hash) and add the changed-body sentence as its own case. Say which in the plan.
- Resolution: open

### Medium

#### R2. The sign-off warning reads a `state:` line that `review.md` never has
- Area: 13 (buildability), 11 (size)
- Finding: Goal, Done 4 and Data row 4 have `plan sign-off` warn when `review.md` has state `blocked` or `failed`. `ReadReports` takes a report's state from a `state:` line in its first six lines, but `review.md`, as `skills/powerops-review-plan/SKILL.md` section 4 and `docs/PLANNING-SKILLS.md` ("`review.md` starts with three lines") prescribe it, has only `plan-hash:`, `model:` and `fresh-context:`; no `state:` line. None of the `review.md` files under `docs/plans/` has a `state:` header line. So the warning never fires on a review the role wrote, and test 8 (g) passes only on a hand-made `review.md`. The plan gives no source that any writer puts `state: blocked` or `failed` in `review.md` (the packet's E-7 is cited but not checked against the role's format, unlike row 2's 122-report check).
- Suggested fix: Either move the sign-off warning to Won't build, or base it on what `review.md` does carry: an open High under `### High`, counted with row 2's rule (and drop "`review.md` stays 0" from row 2), with the warning text and tests changed to match. Record the choice in `decisions.md`.
- Resolution: open

### Low

#### R3. The docs inventory misses three places that describe the check or sign-off
- Area: 9 (testability), 13 (buildability)
- Finding: Row 6 names `COMMANDS.md` `deploy apply`, `HANDOFF.md`, `USE-WITH-AGENTS.md`, the schemas and `PLANNING-SKILLS.md`. Not named: `skills/powerops-implement/SKILL.md` line 87, which lists the cover conditions an agent relies on ("ready, were written for the current plan, and are linked to that zip") and would then omit an open High and the plan's sign-off; `docs/GITHUB-ACTION.md`, the `require-reviews` input, which says when "a report covers a zip"; and `COMMANDS.md`'s `plan sign-off` paragraph ("`plan sign-off` refuses unless ..."), where the new warning would be documented. Row 6 (e)'s anchor, a sign-off paragraph listing its warnings "near warn-only", doesn't exist in `PLANNING-SKILLS.md`: "warn-only" appears only in "There is no warn-only mode" under sign-off by hand. Editing the implement skill regenerates `plugins/*/skills/`, which step 4's empty `git diff --stat ... plugins` check forbids.
- Suggested fix: Add these lines to row 6, each changed or recorded as left as is with the reason, give row 6 (e) an anchor that exists, and narrow step 4's empty-diff check to the two frozen roles' plugin copies if the implement skill changes.
- Resolution: open

#### R4. "No unpack call" holds only for zips linked by solution-hash
- Area: 9 (testability)
- Finding: Done 1 says each refused case makes "no pac import or unpack call", while row 3 (d) keeps `NeedsUnpack` unchanged. `DeployCommand.WarnUnreviewed` unpacks a zip whenever some report records it as a `deploy-hash:`, before `Problem` is asked, so a zip linked by deploy-hash under a plan that isn't signed off is still unpacked (read-only, into a temp folder) before the refusal. The tests pass because `Reviewed` links by solution-hash by default; the plan doesn't say that the six cases do.
- Suggested fix: Say in tests 8 (c) to (e) that the six cases link by solution-hash, and in Done 1 or row 3 (d) that a deploy-hash link under an unsigned plan still unpacks before it is refused.
- Resolution: open

#### R5. Steps don't say which `powerops` build runs, or where the chain's scripts are
- Area: 12 (tools)
- Finding: Steps 1 and 4 run `powerops plan sign-off` and `powerops plan status`. The `powerops` on PATH reports version `0.0.0+b065f23...`, a build older than this branch's base `22dc11f`, so it has neither the new sign-off warning nor any plan-status change made since. Step 4 runs the leak scan of "chain rule step 9" and step 5 runs `ci-watch.sh`; both exist only in the main checkout's git-ignored `reports/` folder, not in this worktree. Assumptions names neither.
- Suggested fix: Say that step 4 runs the worktree's own build (`dotnet run --project src/PowerOps.Cli -- plan status ...`) or reinstalls the tool first, and add an Assumptions line giving where `ci-watch.sh` and the leak scan are run from.
- Resolution: open
