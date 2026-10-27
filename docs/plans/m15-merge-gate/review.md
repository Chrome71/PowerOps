plan-hash: ae37bdcc453568b86ee5dc97b84843f32682da328655c8765d0505bae28d0b5f
model: claude-sonnet-5-5
fresh-context: yes

The plan-hash was updated after the text fixes for R1 to R17 below (the practice of the M7 to M14 reviews). Reviewed hash: e4b14656c83482e6d9773dcc520f51e06c001dedfe5ed881b83863a9be395e57. The reviewer (powerops-reviewer, Sonnet, fresh context) could not write files, so the planning session wrote this file from its reply and added the resolutions. The fixes changed Goal (done-when), Users, Data, Automation, Environments and route, Constraints, Assumptions and Steps 2, 3 and 6, and decision Q2.

## Findings

Checklist items 1 to 8 barely apply to a GitHub Action change; the findings are about testability (item 9), coverage of the BUILD-PLAN M15 row and the design's failure modes.

### High

No findings.

### Medium

#### R1. The gate fails open when `tests` is empty or the action ref is too old
- Area: 9 (testability) and coverage of the M15 row
- Finding: `Flow tests` runs only when `inputs.tests != ''`. A workflow with `tests` empty or omitted skips it and the required check goes green with nothing run; so does a copied example with an action ref from before this change, since GitHub only warns about an unknown input. A flow with no test file is never checked, and the plan doesn't say so.
- Suggested fix: In the README section, state the minimum version that has `tests`, that a job summary without a "Flow tests" section ran nothing, and that only flows with a test file are gated; add a warning if an easy signal exists, otherwise accept the gap under Constraints.
- Resolution: fixed (the README section states all three limits; no warning: empty `tests` is the documented "off", and an old release can't warn about an input it doesn't know)

#### R2. The PR can edit away the gate, and the README rules don't say so
- Area: 9 and security
- Finding: A required check runs the workflow and test files from the PR itself, so a PR can remove `tests:`, the workflow or the test files and stay green. `docs/examples/README.md` already says to protect `.github/workflows/`; the new section doesn't. Merge queues are left out: without `merge_group`, a required check never reports for a queued merge.
- Suggested fix: Add a README rule to protect `.github/workflows/` and the tests folder with CODEOWNERS or a ruleset, and to add `merge_group` to `on:` with a merge queue.
- Resolution: fixed (both rules in the README section; the `merge_group` rule is from GitHub's "Events that trigger workflows" page, read 2026-10-26)

#### R3. The merge-gate example runs the whole analysis too, and the check name hides it
- Area: 9
- Finding: With the action's defaults the check also fails on analysis errors (`fail-on: error`), while `Apply test result` names only flow tests, and a repo that analyzes elsewhere does the work twice. The plan doesn't decide it.
- Suggested fix: Set `fail-on: none` in the example, or say the check also enforces `fail-on`.
- Resolution: fixed (the example sets `fail-on: none`, so the check means flow tests only, and says to drop it to gate the analysis too)

#### R4. "No tenant data" is wrong for the promote example
- Area: security and data (Data, Constraints)
- Finding: The promotion example's plan job would test Dev's export in `$RUNNER_TEMP/deploy`; the report's values go unfiltered into the job summary and annotations, and a Dev export can hold hard-coded URLs (PO006) and current values (PO008), which `powerops test` uses. That is the leak the example's masking exists to avoid.
- Suggested fix: Point the example at the committed source, or warn in `docs/examples/README.md`; correct Data and Constraints either way.
- Resolution: fixed (the promotion example doesn't get the input, since its `path` must stay Dev's export; `docs/examples/README.md` points to the merge-gate workflow on the committed source; Data and Constraints say the reports quote the files under test, and the examples test committed files only; Q2 updated)

#### R5. The skill tells agents to write mocks but not to keep real values out of them
- Area: security, AGENTS.md rule on secrets and tenant URLs
- Finding: M14 says a run's raw outputs paste into a mock unchanged, and the new wording says only "a mock for each connector action", so an agent could commit tenant URLs, ids and emails in a test file.
- Suggested fix: Add "use fake values in triggers and mocks, never paste a real run's raw outputs unscrubbed" and pin it.
- Resolution: fixed (in the Finish step's wording, pinned by `SkillsCommandTests`)

#### R6. Writing and running flow tests in the build step can't work as ordered
- Area: 9
- Finding: The flow is built in Dev and reaches the repo only at hand-off step 3, after the build step, and the mocks need the unpacked flow's action names, so section 3 step 4 has nothing to run the tests on.
- Suggested fix: Write and run the tests after the export and unpack, committed with it; without an export, report "not verified".
- Resolution: fixed (the wording moved to a new Finish step 2, after `analyze`, which needs the export too: the tests are written once the solution is exported and unpacked and committed with it; without it, "not verified" and why; section 3 is unchanged)

#### R7. The live check copies the tenant-signing workflow onto `gate`, and the plan relies on one filter
- Area: 5 (route) and live-check design
- Finding: `gate` is cut from `main` and carries `powerops-promote.yml`, which signs in to the tenant; the only protection is its `branches: [main]` filter, from one read.
- Suggested fix: Delete `powerops-promote.yml` in the first commit on `gate`, or read both workflows' `on:` blocks again before the push.
- Resolution: fixed (the first commit on `gate` deletes both workflows, and their `on:` blocks are read again right before the first push)

#### R8. The live-check expectations can't be read as written
- Area: 9
- Finding: `BLOCKED` also shows while a required check is pending; `CLEAN` needs every other check done and green (`UNSTABLE` otherwise); the annotation on a file outside the diff isn't in "Files changed"; the fork check has nothing to observe; the ruleset body is incomplete (`strict_required_status_checks_policy`, the GitHub Actions app id) and must come after the push.
- Suggested fix: Wait for the run, read `statusCheckRollup`, read annotations through the check-runs API, give the fork PR the same break, list the full ruleset body with the app id and create it after the push, and say when `UNSTABLE` is possible.
- Resolution: fixed (all six in Environments and route; the app id 15368 was read from `gh api /apps/github-actions`; `UNSTABLE` is ruled out by deleting the other workflows on `gate`)

#### R9. The wiring of `action.yml` to `test.sh` is only checked live
- Area: 9
- Finding: The stub tests and `ActionYmlTests` don't run the real step; the repo's CI `action` job already runs `uses: ./` against `tests/fixtures/powerops-seed/v2`, where the seed tests pass.
- Suggested fix: Add `tests: tests/fixtures/flowtests/seed` to that step as a `ci:` change.
- Resolution: fixed (Goal, Automation and Step 2)

### Low

#### R10. Behaviours of the JSON run that the build could get wrong
- Area: 9
- Finding: The JSON run exits 1 by design; a `null` `actual` needs `has("actual")`; `jq`'s number format can differ from the C# writer; the warning path is not in the done-when.
- Suggested fix: State these in Automation and add tests for the warning path and `actual: null`.
- Resolution: fixed (Automation step 3 and Goal)

#### R11. Offline test list is missing cases
- Area: 9
- Finding: Missing: the exact argument list; another exit code (137); exit 0 or 1 without the report file; an absolute tests folder under the workspace; a path with a space; more than ten failing files, which GitHub caps per step.
- Suggested fix: Add the cases or accept them; strip a `GITHUB_WORKSPACE` prefix.
- Resolution: fixed (each case is in Goal; the prefix is stripped; the cap is 10 error annotations per step per the Checks API docs, stated in Automation and the README, and the job summary lists every test)

#### R12. `ActionYmlTests` pins strings, not behaviour
- Area: 9
- Finding: The test doesn't say it pins that neither step has `continue-on-error`, or the step order around `Upload SARIF`, `Deploy plan` and `Deploy apply`.
- Suggested fix: Add those assertions.
- Resolution: fixed (Goal)

#### R13. Undocumented gating of `Deploy apply`, and two deviations from the BUILD-PLAN row
- Area: coverage of the M15 row, unneeded scope
- Finding: The `Deploy apply` condition is not in the row or the README's `plan-file` bullet, and the ephemeral recipe's omission is not in the row.
- Suggested fix: Add a clause to the `plan-file` bullet and a note to the M15 row when it is marked built.
- Resolution: fixed (README bullet in Automation; Step 6 records the apply gate and both examples left out in the row)

#### R14. The live-check copy of the action is not "the same files"
- Area: 9
- Finding: `PowerOps.Cli.csproj` embeds `skills/` and packs `README.md`, which the copy list left out.
- Suggested fix: Add them or drop the claim.
- Resolution: fixed (both added to the copy)

#### R15. README wording and example details
- Area: 9
- Finding: The fork-approval "default" is unsourced; a check usually must have run before it can be picked; the example should pin by SHA; two README examples have a `plan` job; annotations outside the diff show on the check run.
- Suggested fix: Fix these in the README text.
- Resolution: fixed (approval is described as the repository's Actions setting; "run the workflow once" first; checkout pinned to its SHA as in the other README examples, and the action as `Chrome71/PowerOps@v1` like them until a release SHA exists; the job to require is named; where annotations show is stated)

#### R16. Goldens procedure is incomplete
- Area: 9
- Finding: Step 3 omits `git checkout tests/golden/selftest.txt` and the normal `dotnet test` after the update run.
- Suggested fix: Add them.
- Resolution: fixed (Skills and hand-off, and Step 3)

#### R17. Account and repository names in the plan
- Area: 10 and the URL/GUID/email/name rule
- Finding: The plan names the owner account, the test repository and a fork account; these are public project handles, and no tenant URL, GUID or email appears.
- Suggested fix: None if they are accepted as public project names.
- Resolution: accepted (they are the project's public handles, as in the README and earlier plans; noted under Assumptions)
