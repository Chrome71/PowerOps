# Decisions: m83-zip-linked-reviews

Round 0 came first: it is one of the six open questions (section 6) of the PowerOps 1.0 fix plan that Fable and Astra agreed on 2026-11-08, asked of the person in one round by the session that began the fixes. Round 1 is the plan's own.

## Round 0 (the 1.0 fix plan)

### Q1. When does M83 land: the kept roles review the managed zip's unpacked form, bound to the zip with checked provenance?
1. Fable: M75's diff evidence ships in 1.0; M83 (frozen roles, with its own cost estimate) is in 1.0 if the person funds the eval now, else the first 1.0.x, with the gap written as a release exception the person accepts in the record
2. Astra: complete the zip linkage (M83) before the release-qualifying trial; a deferred release must explicitly accept the gap, and a clean partial `diff` can't close it

Both agree on M83's design and that `diff` is evidence, not a link.

Answer: 1, "1.0.x, with exception" (answered by: person, 2026-11-08). M83 is the first 1.0.x milestone. 1.0.0 is cut without it only with the person's written release exception, in the second trial's record, accepting that no report is bound to the deployed zip by hash; M75's evidence note doesn't close the gap.

## The person's instructions of 2026-11-09

Round 0's answer made M83 the first 1.0.x milestone. On 2026-11-09 the person pulled it forward, with the instruction "Once 75 is finished with its PR continue with the other M sessions in new sessions each until you wrap up M83 autonomously." (answered by: person, 2026-11-09). The chain session then asked three questions; the answers that apply here, each with its question, as `chain-rules.md` records them:

1. "For M76 and M79–M83, who signs off each milestone's plan before it is built?" Answer: "Sessions sign off (Recommended)": each session signs its own plan once the Fable plan review approves it and `powerops-review-plan` leaves no finding open, and records that it did so on the person's instruction of 2026-11-09. (answered by: person, 2026-11-09)
2. "Who merges each milestone's PR once all 8 CI checks pass, starting with M75's open PR #128?" Answer: "Sessions merge (Recommended)": each session rebases on main, reruns its checks, opens its PR and squash-merges it when green, one PR at a time. (answered by: person, 2026-11-09)
3. "M83 changes both frozen review roles, so it must rerun their eval (M55 recorded about USD 22 expected, USD 30 cap) and renew your keep decision. How should M83 run?" Answer: "Keep if rules hold (Recommended)": run the eval; if every keep rule holds for both roles, keep them, pin the new ids and merge; if any rule fails, stop and ask the person. (answered by: person, 2026-11-09)

## Round 1 (the plan)

Settled by the building session, each inside what the fix plan and the answers above give it.

### Q1. How is the reviewed folder's provenance checked, and how is byte-stability established?
1. `deploy plan` and `--require-reviews` run one `pac solution unpack` of the imported zip into a temp folder, with the zip's own package type, and compare its `manifest` digest with the report's `solution-hash:`; only for a zip a report names as its `deploy-hash:` (recommended)
2. Unpack twice per check and require equal digests before comparing
3. Trust the report's `solution-hash:` once `deploy-hash:` matches
Answer: 1. The reviewer's folder came from its own unpack of the same zip, so the comparison is itself the stability check: an unstable unpack, or another `pac` version that writes other bytes, gives another digest and the check refuses. It can only fail closed, so a second unpack adds time and nothing safe. Stability itself was checked on both managed seed zips before sign-off (Assumptions). Option 3 is the "asserted, not checked" provenance the fix plan rejects. The unpack runs only when a report claims the zip, so plans with no such report call no `pac`. (answered by: agent m83-zip-linked-reviews)

### Q2. Which previous solution do the roles compare the managed unpack with?
1. The last export before this plan, as today (recommended)
2. The unmanaged folder of the same build, as the fix plan's route sentence words it
Answer: 1. The roles review what this plan changed, which needs the release before it. Against the same build's unmanaged folder, `diff` would show only the managed flag and the file comparison only the renamed forms, and every change the plan made would look unplanned or vanish. That the managed zip matches the reviewed build is what the provenance check and M75's evidence note show. The roles see `diff`'s managed-flag change and re-read the forms renamed `_managed` (rule B), which is safe. (answered by: agent m83-zip-linked-reviews)

### Q3. Does M75's link (a report whose `solution-hash:` is the zip's SHA-256) stay?
1. Yes, beside the new one (recommended)
2. No, only `deploy-hash:` with provenance
Answer: 1. It is a true link: the role read that zip. M75's Round 1 Q2 made it the route until M83, and reports written that way stay valid. (answered by: agent m83-zip-linked-reviews)

### Q4. Where do the new blob ids go?
1. This plan's Data section, with `AGENTS.md` and `RoleEvalTests` pointed here, as M55 moved the pointer from M39 to its own plan (recommended)
2. The Data section of `docs/plans/m55-role-freeze/plan.md`, as the session brief words it
Answer: 1. M55's plan is signed off; an edit to its body makes `powerops plan status` report `changed since sign-off` (tried on a copy, 2026-11-09), so option 2 would need M55 re-reviewed and re-signed by a session that didn't build it, and the brief also requires `plan status` with no violation. Recorded comparisons for later typo edits move to this folder's `decisions.md` with the pointer. (answered by: agent m83-zip-linked-reviews)

### Q5. What does the eval's request give as the deployed zip?
1. `run.sh` zips `solution/current` into `deploy/current.zip` in the run's temp folder, records its digest in `meta.txt`, and the prompt names it (recommended)
2. "There is no deployed zip", so every report writes `deploy-hash: unknown`
3. Add a real managed export to each case
Answer: 1. It runs the new line's path in every solution run and gives a value to check `deploy-hash:` against. Option 2 never runs that path. Option 3 changes the frozen cases, which M55 kept comparable with M38 and M39. The zip is not a `pac` export; the roles only hash it, and the cases' folders are what they review, as before. (answered by: agent m83-zip-linked-reviews)

### Q6. What does the eval cost, and what stops it?
1. M55's measured $0.51 a Claude run, plus 30%, replacements and the Sonnet scorer: about USD 16 expected, under the person's USD 30 cap, counted from each run's `total_cost_usd`; a stop and a question if it would pass USD 30 (recommended)
Answer: 1. (answered by: agent m83-zip-linked-reviews)

## Log

- 2026-11-08: asked together with the fix plan's other five questions, in one round, recommended option first where the plan recommends one; where the plan states two positions (Q1, Q6), both were shown as the plan words them, neither marked recommended.
- 2026-11-09: plan started by the chain's M83 session (worktree `musing-mclaren-aa1da3`) on `fca4c57`, after M73 to M82 merged
- read on 2026-11-09: `chain-rules.md`; the fix plan, sections 3 (M75, M83), 4, 5 and 6; the hand-over notes of M75, M76, M80 and M81; `AGENTS.md`; `docs/plans/m55-role-freeze/` (`plan.md`, `decisions.md`, `report.md`); `tests/eval/roles/run.sh`; both kept roles' `SKILL.md`; `skills/powerops-implement/SKILL.md` sections 3 and 4; `DeployReviews.cs`, `DeployCommand.cs` (`WarnUnreviewed`, `Run`, `DryRun`, the gate), `PlanChecker.cs` (`PlanReportFile`, `ReadReports`), `PacRunner.cs`, `McpCommand.cs` (`deploy_apply`); `RoleEvalTests.cs` (header, pins); `DeployFakes.cs`, `DeployReviewWarningTests.cs`, `Tools.cs`; `docs/HANDOFF.md` steps 3 to 8, `docs/COMMANDS.md` (`deploy plan`, `deploy apply`), `docs/USE-WITH-AGENTS.md`, `docs/PLANNING-SKILLS.md`
- 2026-11-09: the fix plan's claim checked locally on `tests/fixtures/zips` with pac 2.14.1 (no tenant): see Assumptions; Learn checked by powerops-researcher (Opus) the same day
- depth: standard: the rerun is a quota spend, and the keep decision is renewed by the person's instruction
- areas not asked: users, screens, solution and publisher, environments; the change is to this repo, and the plan answers each or marks it not applicable
- plan review under the model routing (a `fable-reviewer` subagent, Fable, fresh context) 2026-11-09: REJECTED with seven findings, all answered in `plan.md`: the temp folder's two write calls join `WriteSitesTests`, AGENTS.md's read-only rule and the three descriptions that say nothing is written (1); the real-`pac` test skips without `Tools.Require`, which fails under CI's `POWEROPS_REQUIRE_TOOLS=1` (2); row 1 keeps the six-line window for the four keys and reads only `deploy-hash:` from line 7 (3); row 6 is spelled out, step 3 gains a stub-client check of `run.sh` from the clean clone and a Fable work review of the freeze before any paid run (4); the note prints only when both reports link by deploy-hash (5); cross-OS and cross-version digests are listed as not checked, and COMMANDS and HANDOFF name the causes of a mismatch and the recovery, keeping M75's route as the fallback (6); `mcp-tools.txt` updates in step 2, all plugin copies in step 6 (7)
- plan review resubmission (Fable) 2026-11-09 at `6b67025`: APPROVED; executors: steps 2 and 3 Opus, row 8 and step 6 Sonnet, the rest the main session
- approved 2026-11-09 by the building session for a fresh-context review
- fresh review (a general-purpose subagent following `powerops-review-plan`, Opus, fresh context) 2026-11-09 at `d6d297f`: 0 High, 4 Medium, 5 Low (R1 to R9), answered in `plan.md`: C3 tells the role to run nothing else on the zip and that the managed flag and `_managed` forms differ by design (R1); the gate's deadline bounds each unpack's timeout and no unpack starts in its last 10 seconds (R2); the review folder is kept out of Git, and HANDOFF says so (R3); the Windows and runner digest comparison is the second trial's first check, and the recovery names the runner's `pac` (R4); the tests that quote reworded sentences are named (R5); an unpack with no file fails (R6); `Recorded` stays M75's, so evidence notes still print when a deploy-hash link fails (R7); the recovery unpacks into a new or emptied folder (R8); `run.sh`'s digest guard runs under `set -e`, and the stub check covers it (R9)
- second pass of the same review 2026-11-09 at `83ed2b4`: R1 to R9 resolved, 1 new Low (R10), answered in row 8 (five failure sentences); row 3 counts the gate's time left from `RunGate`'s clock, as the pass noted
- third pass of the same review 2026-11-09 at `50493f7`: R10 resolved, no new finding; 0 High, 0 Medium, 0 Low open
- signed off 2026-11-09 by the building session on the person's instruction of 2026-11-09 ("Sessions sign off (Recommended)", answer 1 above), after Fable approved and `powerops-review-plan` left no finding open (`powerops plan sign-off`; `signed-off-hash` equals `review.md`'s `plan-hash`) (answered by: agent m83-zip-linked-reviews)
- rows 1 to 3 built by an implementer subagent (Opus), `4ccaec0`; rows 4 to 7 by another (Opus) in its own worktree, brought in as `825ba70`. Deviation recorded: while debugging, the first ran `dotnet run -- deploy plan <scratch>/deploy --target x`, whose live reader called the real `pac org who --environment x`; pac refused the value locally ("Text matching is not supported by this verb") before contacting any host, so no tenant was reached, but it was a `pac` command that could have reached one, against the milestone's no-tenant limit. It stopped and debugged through the tests only
- frozen 2026-11-09 at `6b67efb`: a clean clone outside the repo built with 0 warnings, kept format clean and failed only the three tests the plan names; the stub check passed its 21 assertions; the Fable work review of rows 1 to 7 at the freeze: PASS, no corrections
- run 2026-11-10 03:56 to 04:23 UTC: 32 runs, both roles on solution-01 to solution-08 on Claude (`opus`, Claude Code 2.1.319) and Codex (`gpt-6-astra`, codex-cli 0.166.0), effort high, through `run.sh` from the clean clone; all valid, none replaced; Claude $8.28 at list price
- scored blind by a Sonnet subagent and by Codex in a read-only sandbox: 64 of 64 pairs agreed; the two split on all five Highs of the clean-case runs (all Codex's), settled by the building session as supported, unkeyed findings: three by M55's and M38's precedent (placeholder settings), two because the "unmanaged deployment input" is true on the inputs the eval gave (its zip is a plain zip of an unmanaged folder); under the Sonnet scorer's reading the Verifier would have 2 false Highs and miss that floor. Unchanged statements judged by a Sonnet subagent: 2 false (both Claude), 10 unsupported on the strict reading. Results and keep rules in `report.md`
- 2026-11-10: every keep rule holds for both roles and both families (Verifier 12 of 12, Security reviewer 14 of 14 own not-shown flaws; 0 false Highs each as settled; denominators 12 and 14). The keep decision is renewed on the person's instruction of 2026-11-09, answer 3 above: "Keep if rules hold (Recommended)": run the eval; if every keep rule holds for both roles, keep them, pin the new ids and merge; if any rule fails, stop and ask the person. Both roles' new text is kept (answered by: agent m83-zip-linked-reviews, on the person's instruction of 2026-11-09)
- fourth pass of the same review 2026-11-10 at `d936680`, with the build in the tree: the Data edit, rows 1 to 9 and the shipped text hold; 1 new Low (R11), answered: the PR body and the hand-over quote `report.md`'s line on the false-High settlement, and for the next rerun of these roles: the eval's zip is a plain zip of each case's unmanaged folder while the skill text calls it "the managed zip", which made Codex's Verifier raise "deployment input is unmanaged" on both clean cases; a rerun should give a zip whose solution is managed (or say in the prompt that the eval's zip is not a managed export), so that the false-High floor doesn't rest on a settlement
- correction 2026-11-10 to the "scored blind" line above: the Sonnet scorer marked all five clean-case Highs unsupported, so its reading gives the Verifier 4 false Highs and the Security reviewer 1 (2 for the Verifier counting only the two V1s); either way the Verifier would miss the floor. The settlement stands
- final work review (Fable) 2026-11-10 at `d936680`: FAIL with three numbers in `report.md` (the false-High counts under the other reading, the Codex scorer's tenant-claim count 7, Codex run tokens 4.3M), corrected; it judged the settlement of the five clean-case Highs sound after reading both V1 reports, both plans, the request and both `Solution.xml` files, and passed rows 8 and 9. Two optional wording points applied: GITHUB-ACTION says another `pac` version may give another digest (not checked); USE-WITH-AGENTS names M83's report
- fifth pass of the same review 2026-11-10 at `23dfbd6`: R11 resolved, no new finding; 0 High, 0 Medium, 0 Low open
- final work review (Fable) resubmission 2026-11-10 at `23dfbd6`: PASS
- signed off again 2026-11-10 by the building session on the person's instruction of 2026-11-09 ("Sessions sign off (Recommended)", answer 1 above), after the Data edit, the fourth and fifth review passes and the Fable work review, with no finding open (`powerops plan sign-off`; `signed-off-hash` equals `review.md`'s `plan-hash`) (answered by: agent m83-zip-linked-reviews)
- status done 2026-11-10: rows 1 to 9 built, the eval run and kept, the checks of Done when 7 to follow on the rebased branch
- 2026-11-10: the person was shown the settlement of the false-High row (the scorers split on all five clean-case Highs; under the Sonnet scorer's reading the Verifier has 4 false Highs and misses the floor, as settled it has 0) and asked "Do you accept M83's keep decision for the Verifier role?". Answer: "Accept the keep". Both roles stay at the pins in the Data section of `plan.md` (answered by: person, 2026-11-10)
