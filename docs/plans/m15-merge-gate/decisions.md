# Decisions: m15-merge-gate

## Round 1

### Q1. How does the Action run the tests and report them?
1. A script, `action/test.sh`, that runs `powerops test` with Markdown for the job summary and, on exit 1, again with JSON for one error annotation per test file that did not pass; it records the exit code and always exits 0, and a last step `Apply test result` fails the job, as `plan.sh` and `Apply plan result` do (recommended)
2. One inline step that runs `powerops test` and fails at once
3. In your own words
Answer: A script, `action/test.sh`, that runs `powerops test` with Markdown for the job summary and, on exit 1, again with JSON for one error annotation per test file that did not pass; it records the exit code and always exits 0, and a last step `Apply test result` fails the job, as `plan.sh` and `Apply plan result` do. A failing inline step would stop the steps after it (the plan comment, the diff) and leave no summary; a script can be tested offline with a stub `powerops`, as the BUILD-PLAN row asks.

### Q2. Which workflows get the `tests` input, and does a failing test stop an apply in the same call?
1. The README's Quick start and `Deploy plan` examples, the new merge-gate workflow, and the promotion example's plan job; not the ephemeral recipe, whose `paths` filter would leave a required check pending and which was never run end to end. With `plan-file` set, a failing test stops `Deploy apply`, as the `fail-on` gate does (recommended)
2. Only the new merge-gate workflow, and the apply runs whatever the tests say
3. In your own words
Answer: The README's Quick start and `Deploy plan` examples, the new merge-gate workflow, and the promotion example's plan job; not the ephemeral recipe, whose `paths` filter would leave a required check pending and which was never run end to end. With `plan-file` set, a failing test stops `Deploy apply`, as the `fail-on` gate does. An import after a failed test in the same job would contradict the gate, and the condition is one line. Changed after review R4: the promotion example's plan job does not get the input either, because its `path` is Dev's managed export and the test report would show values from it in a possibly public job summary; `docs/examples/README.md` points to the merge-gate workflow, which tests the committed source.

### Q3. How does the live check run in the test repo without pushing this branch?
1. After the user's yes in this session: copy this branch's action into the test repo (`.github/actions/powerops/`) and call it as a local action; open the PRs into a new `gate` branch, so the repo's promotion workflow (PRs into `main` only), which signs in to the tenant, does not run; a ruleset on `gate` requires the check; the fork PR comes from `chrome71-test/powerops-action-test`; afterwards delete the ruleset and close the PRs (recommended)
2. Ask the master session to push this branch and pin `Chrome71/PowerOps@<commit>`
3. In your own words
Answer: After the user's yes in this session: copy this branch's action into the test repo (`.github/actions/powerops/`) and call it as a local action; open the PRs into a new `gate` branch, so the repo's promotion workflow (PRs into `main` only), which signs in to the tenant, does not run; a ruleset on `gate` requires the check; the fork PR comes from `chrome71-test/powerops-action-test`; afterwards delete the ruleset and close the PRs. The brief says not to push; the copy runs the same files, and the checks are about GitHub's required checks, not about how the action is referenced.

## Log

- plan started 2026-10-26 from the master session's brief (the BUILD-PLAN M15 row, build-order item 40, decisions 19 and 22)
- `powerops plan facts` not run: M15 needs no environment; `environment` stays null
- depth: quick (from `m14-flowtest`)
- the user delegated their parts on 2026-10-22 (relayed by the master session): this session answers with the recommended options and signs the plan off itself after a clean fresh review; the live check in the test repo still needs the user's yes in this session (Q3)
- test repo read on 2026-10-26 (read-only, `gh api`): public, default branch `main`, workflows `powerops.yml` (analyze on every PR, job `analyze`) and `powerops-promote.yml` (PRs into `main`, signs in to the tenant), no rulesets, fork `chrome71-test/powerops-action-test` with admin permission for this account, fork-PR approval policy `first_time_contributors`
- GitHub's docs read on 2026-10-26: "Troubleshooting required status checks" (a workflow skipped by a path, branch or commit-message filter leaves its checks pending and blocks merging; a job skipped by a conditional reports success) and "About protected branches" (make job names unique across all workflows, or status check results are ambiguous and can block pull requests)
- approved without: users, constraints (the BUILD-PLAN M15 row and Phase 4 text cover them; licensing is in Constraints)
- approved (2026-10-26)
- fresh review (powerops-reviewer, Sonnet) 2026-10-26: 0 High, 9 Medium, 8 Low; all fixed in the plan text or accepted with a reason in review.md (R4 changed Q2; R7 deletes the test repo's workflows on `gate`); no user decision needed
- signed off 2026-10-26 by this session under the user's delegation (see above), after every review finding was fixed or accepted
- 2026-10-26: built steps 2 and 3 (`feat(action)`, `ci:`, `feat(skills)`); 2057 tests pass; a code review (powerops-reviewer, Sonnet) found 0 High, 0 Medium, 5 Low, all fixed in those commits: backslashes and the bare workspace in annotation paths, parentheses around the `jq` `as` binding for older `jq`, the step's env pinned in `ActionYmlTests`, the "after the export" wording pinned in `SkillsCommandTests`, and a README sentence to run the tests on committed files only
- 2026-10-27: the user said yes in this session to the live check in the test repo; it passed (details in the BUILD-PLAN verification record): PR 6 (same repository) failed the required check with an annotation on each test file and was `BLOCKED`, a fix commit made it `SUCCESS` and `CLEAN`, and PR 7 (from the fork) ran the tests without an approval wait, failed the same way and was `BLOCKED`. Two deviations from Environments and route: the fixture copy replaced the repo's existing `solutions/powerops-seed/` (only `Solution.xml` differed) instead of a new `solutions/PowerOpsSeed/`, because two copies of the flow would make the tests ambiguous; the ruleset was created after the first run of PR 6, as planned. Cleanup: ruleset deleted, PRs closed, PR branches deleted, `gate` kept
