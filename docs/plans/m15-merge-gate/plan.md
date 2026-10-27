---
status: signed-off
review: done
signed-off: 2026-10-26
signed-off-hash: ae37bdcc453568b86ee5dc97b84843f32682da328655c8765d0505bae28d0b5f
environment: null
depth: quick
supersedes: null
---

# M15: the merge gate

## Goal

Build BUILD-PLAN milestone M15 (build-order item 40, release v0.15.0): the PowerOps Action runs `powerops test` and fails its job when a flow test does not pass, and that job, made a required status check, blocks the merge. That is Phase 4's done-when: "a failing flow assertion blocks a merge".

Done when:

- `tests/PowerOps.Cli.Tests/ActionScriptTests.cs` runs `action/test.sh` with the stub `dotnet` (as for `apply.sh`), from a canned Markdown and JSON report, and passes:
  - exit 0: the exact argument list (`test <path> --tests <folder> --format markdown --out <file>`), the Markdown in the job summary, `exit-code=0`, no annotation and no second run;
  - exit 1: one `::error` annotation per test file that did not pass, at the tests folder plus the report's file path, none for a passed one; the message holds the flow, the test's message and each failed assertion; an `actual` of `null` is shown as `null`; values over 300 characters are cut;
  - the tests folder given as `.`, `./`, with a trailing slash, or as an absolute path under `GITHUB_WORKSPACE` gives a path from the repository root; a path with a space works; `%`, `,`, `:` and a line break in a file name or message are escaped;
  - exit 2, 3 and another code (137): a fixed text in the summary, the exit code recorded, nothing else run;
  - exit 0 or 1 without the report file, and a JSON report that does not parse: a `::warning::`, and the script still exits 0 with the exit code recorded.
- `tests/PowerOps.Cli.Tests/ActionYmlTests.cs` passes with a new test: the `Flow tests` step runs only when `tests` is set, has no fork or event condition and no `continue-on-error`, and comes after `Upload SARIF` and before `Deploy plan` and `Deploy apply`; `Apply test result` is the last step, runs on `always()`, reads the test step's exit code and has no `continue-on-error`; `Deploy apply` does not run when that exit code is set and not `0`.
- The repository's own CI `action` job (`.github/workflows/ci.yml`, `uses: ./`) runs with `tests: tests/fixtures/flowtests/seed` and passes on ubuntu, an end-to-end pass of the real step.
- `SkillsCommandTests` pins that `powerops-implement` writes or updates the tests of each flow a plan changes, after the export, with fake values only, and runs `powerops test` before the pull request; `HandoffDocTests` pins that the hand-off page's step 4 runs `powerops test` and that the page still has steps 1 to 10.
- Live, in the test repo `Chrome71/powerops-action-test`, GitHub only, after the user's yes in this session (Environments and route): with a ruleset that requires the check, a PR that breaks a flow assertion fails the check, with an annotation on the test file read through the check-runs API, and once the run has finished `gh pr view --json mergeStateStatus` shows `BLOCKED`; a fix commit makes the check pass and the state `CLEAN`; a PR from a fork with the same break runs the tests and fails the same way. Recorded in `decisions.md` and the BUILD-PLAN verification record, with no tenant URL, id or email. Without the yes, both say "not run, waiting on the user".
- `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` and `powerops plan status` are clean.

## Users

- Repository owners who want a failing flow test to block a merge, on pull requests from the repository and from forks.
- Reviewers, who read the report in the job summary and the annotations on the check run.
- Coding agents using `powerops-implement` and the hand-off page, which write the tests and run them before the pull request.

## Data

No new file format. The Action reads the test files of M14 (`*.flowtest.json`) and the solutions under `path`, and uses `powerops test`'s Markdown and JSON reports (`docs/schemas/test.schema.json`) as they are. The reports quote values from the test files and the solutions (expected and actual values, messages), so the job summary and the annotations show what those files hold. For a solution and tests committed to the repository, that is what the repository already shows; the examples therefore run the tests only on committed files, never on an export from an environment (R4).

New input in `action.yml`: `tests`, a folder of `*.flowtest.json` files, relative to the workspace; empty (the default) means no tests.

## Automation

**`action/test.sh`**, a new script in the style of `plan.sh` and `apply.sh`. Inputs come only from the environment: `PO_DLL`, `PO_PATH`, `PO_TESTS`, `GITHUB_OUTPUT`, `GITHUB_STEP_SUMMARY`, `GITHUB_WORKSPACE`. It works in bash 3.2 (macOS runners): no `mapfile`, `${var,,}` or associative arrays.

1. Run `dotnet "$PO_DLL" test "$PO_PATH" --tests "$PO_TESTS" --format markdown --out <temp file>` and record its exit code as `exit-code=<n>` in `GITHUB_OUTPUT`. Its stderr goes to the log unfiltered: the command is offline, so its errors name local paths and the committed files, never a value from an environment.
2. Exit 0 or 1: append the Markdown report to the job summary (a `::warning::` instead when the file is missing). Any other exit: append `## Flow tests`, then "The tests could not run (exit code <n>). See the job log."
3. Exit 1 only: run the same command with `--format json`. That run also exits 1, which is expected; any other exit, or JSON that `jq` cannot read, gives a `::warning::` that the annotations could not be written, and the script goes on (the summary and the exit code already hold the result). Then print one `::error file=<path>,title=Flow test <result>::<message>` line for each test whose `result` is not `passed`. `<path>` is the tests folder (without a `GITHUB_WORKSPACE/` prefix, a leading `./` or a trailing `/`; nothing when it is `.`) joined with the report's `file`, so the annotation names the test file from the repository root. `<message>` is the flow's name, then the test's `message` and each failed assertion in the text report's words (`assertion <index>, <description>: expected <value>, got <value>`, or its message when the assertion has no `actual`; `has("actual")` decides, so an `actual` of `null` prints `null`). Values are `jq`'s `tojson`, cut at 300 characters, so they can differ in number format from the text report; the tests use a canned report. Escaping as in the Annotations step of `action.yml` (`%`, CR, LF in the message; also `:` and `,` in the properties). GitHub shows at most 10 error annotations per step (Checks API docs); the job summary lists every test.
4. Always exit 0; the action's last step fails the job.

**`action.yml`**:

- Input `tests` (default `''`): "Folder of `*.flowtest.json` files, relative to the workspace, to run with `powerops test` against the solutions under path. Empty means no tests. It needs no tenant and no secret, so it also runs on fork pull requests."
- Step `Flow tests` (`id: test`), after `Upload SARIF` and before `Deploy plan`, `if: inputs.tests != ''`, with no fork or event condition and no `continue-on-error`, running `bash "$GITHUB_ACTION_PATH/action/test.sh"` with the env above.
- `Deploy apply` gets `&& (steps.test.outputs.exit-code == '' || steps.test.outputs.exit-code == '0')`, so with `plan-file` set a failing test stops the import, as the `fail-on` gate does (Q2). The README's `plan-file` bullet says so.
- Step `Apply test result`, the last step: `if: always() && steps.test.outputs.exit-code != '' && steps.test.outputs.exit-code != '0'`; one `::error::` per case (1 a test failed, is `unmocked` or uses something unsupported; 2 usage: path or tests folder not found, or no test file or solution; anything else, with 3 an unreadable solution or test file), then `exit 1`. It is a step of its own, not part of `Apply plan result`, so each step's name says what failed.

**Tests** (offline): `ActionScriptTests` gets a `test.sh` section and a `RunTest` helper like `RunApply`. The stub `dotnet` copies `markdown.md` to `--out` today; it gets a `--format` case that copies `report.json` instead for `--format json` when that file exists, so the existing tests are unchanged. `ActionYmlTests` gets the test under Goal. `ci.yml`'s `action` job gets `tests: tests/fixtures/flowtests/seed` (its `path` is already `tests/fixtures/powerops-seed/v2`, the seed flow those tests run against), in a `ci:` commit.

**Skills and hand-off** (`skills/powerops-implement/SKILL.md`, `docs/HANDOFF.md`, `docs/PLANNING-SKILLS.md`):

- `powerops-implement` section 4 (Finish), a new step 2, after `analyze`, which also needs the exported solution: before you open a pull request, for each cloud flow the plan adds or changes, write or update its tests once the solution is exported and unpacked into the repository (hand-off step 3), and commit them with that export: `*.flowtest.json` files in the folder the repository's PowerOps Action `tests` input names (else `flowtests/` at the repository root), one test for each path through the flow that the plan describes, with a mock for each connector action (README, Flow tests). Use fake values in triggers and mocks, as in fixtures; never paste a real run's raw outputs without replacing tenant URLs, ids, emails and names. Run `powerops test <solution> --tests <folder>` and report the result. A test that does not pass blocks the merge in CI, so fix the flow or the test, and never delete or weaken a test to make it pass; an `unsupported` test is reported to the user, who decides. If the solution is not in the repository yet, say "not verified" and why, as for `analyze`. The later steps of section 4 move down by one, and the last one reports the test result too; nothing cites their numbers. Section 3 is unchanged, so the step 7 that the BUILD-PLAN M12 row and decision 16 cite stays step 7.
- Hand-off step 4 becomes **Analyze and test**: `powerops analyze src/ContosoApp` as now, and `powerops test src/ContosoApp --tests flowtests` runs the tests `powerops-implement` wrote for each flow the plan changed; fix errors and failing tests in Dev and export again. One sentence links the README's merge-gate section. No step is renumbered.
- `docs/PLANNING-SKILLS.md`: the two lines that say `powerops-implement` runs `powerops analyze` at the end also name `powerops test`.
- `SkillsCommandTests` pins the implement wording ("write or update its tests", "fake values", "`powerops test`", "before you open a pull request"); `HandoffDocTests` adds `test` to the commands the page must name, and a test that the line of step 4 holds `powerops test` and that the numbered steps are exactly 1 to 10.
- The plugin copies (`plugins/*/skills/`) are regenerated as AGENTS.md "Update goldens" says: `POWEROPS_UPDATE_GOLDEN=1 dotnet test`, `git checkout tests/golden/selftest.txt`, a normal `dotnet test`, then `git diff tests/golden plugins` shows only the skill change.

**README**:

- A new section after the Quick start's action example, `### Merge gate: flow tests`:
  - What the `tests` input does, and its limits: it gates only the flows that have test files, and only when `tests` is set; with `tests` empty the action runs no tests and the job summary has no "Flow tests" section; an action release without the input ignores it with a warning, so use a release from v0.15.0 on (R1).
  - An example workflow `PowerOps flow tests` on `pull_request` with no filter, one job `powerops-flow-tests` (checkout pinned to its SHA as in the other examples, then the action with `path`, `tests: flowtests`, `fail-on: none` and `upload-sarif: 'false'`, so the check means flow tests only and needs only `contents: read`; drop `fail-on: none` to gate the analysis too) (R3).
  - How to make it block a merge: run the workflow once, so the check is known, then a ruleset (Settings, Rules, Rulesets) or a branch protection rule on the default branch with "Require status checks to pass" and the check `powerops-flow-tests` from GitHub Actions.
  - The rules, each with its reason from GitHub's docs: the check is named after the job (its `name:`, else its id), and the same job name in several workflows makes the result ambiguous and can block pull requests, so give it a name no other workflow uses; a workflow skipped by a path, branch or commit-message filter leaves the required check pending, so put no filter on it; a job skipped by an `if:` reports success, so put no `if:` on the job; with a merge queue, add `merge_group` to `on:`, or the required check is never reported for the queued merge.
  - The pull request runs its own workflow file and test files, so a PR can remove the gate: protect `.github/workflows/` and the tests folder with CODEOWNERS and required code-owner review, or a ruleset (R2). Fork PRs may wait for a maintainer's approval before their workflows run (the repository's Actions setting for fork pull request workflows); until then the check is pending.
  - The annotations show on the check run, and beside the line in "Files changed" only when the test file is in the PR's diff.
- The `plan-file` bullet under "Pull request comment" gets: with `tests` set too, a failing test also stops the apply (R13).
- The example workflows get the input where they run on committed files: the Quick start example (a commented `tests:` line pointing to the new section) and the README's `Deploy plan` example (`tests: flowtests`; its `path` is the committed unpacked solution, and the step also runs on fork PRs there). `docs/examples/promote-dev-to-test.yml` does not get it: its plan job's `path` is Dev's managed export, so the report would show values from Dev's export in a possibly public job summary, and the job is skipped on fork PRs. `docs/examples/README.md` says so in one line and points to the merge-gate workflow, which tests the committed source. The ephemeral recipe (`ephemeral-pr.yml`) does not get it either: its `paths` filter would leave a required check pending, and it was never run end to end (Q2).
- The `### Flow tests` section gets one line pointing to the merge gate.

## Screens

None. The job summary and annotations are GitHub's.

## Environments and route

- The build: none. Offline, checked in CI on ubuntu, macOS and windows (the script tests skip on Windows, as the others do).
- The live check: GitHub only, in the user's public test repo `Chrome71/powerops-action-test` and its fork, after the user's yes in this session (Q3). No tenant, no secret, no sign-in.
  - This branch is not pushed, so the test repo gets a copy of this branch's action: `action.yml`, `action/`, `global.json`, `Directory.Build.props`, `.editorconfig`, `README.md`, `skills/` and `src/` (what the action builds from, `skills/` and `README.md` included because the CLI project embeds and packs them), from `git archive` of this branch's commit, in `.github/actions/powerops/`, called as `uses: ./.github/actions/powerops`. The action's files are the same as `Chrome71/PowerOps@<commit>` would give; only the reference differs.
  - A new branch `gate`, cut from `main`. Its first commit deletes both of the repo's workflows (`powerops-promote.yml`, which signs in to the tenant, and `powerops.yml`, analyze only), so no tenant workflow can run on `gate` by any trigger and the merge-gate job is the only check (R7). Right before the first push, the `on:` blocks of both files on `main` are read again; the PRs go into `gate`, and `powerops-promote.yml` on `main` triggers only on PRs into `main`.
  - Also on `gate`: the solution `solutions/PowerOpsSeed/` (a copy of `tests/fixtures/powerops-seed/v2`, fake values only), its two tests in `flowtests/` (copies of `tests/fixtures/flowtests/seed/`), the action copy and the merge-gate workflow from the README with `uses: ./.github/actions/powerops`.
  - After `gate` is pushed and the workflow has run once on a PR, a ruleset: `gh api repos/Chrome71/powerops-action-test/rulesets -X POST` with `{"name": "powerops-flow-tests", "target": "branch", "enforcement": "active", "conditions": {"ref_name": {"include": ["refs/heads/gate"], "exclude": []}}, "rules": [{"type": "required_status_checks", "parameters": {"strict_required_status_checks_policy": false, "required_status_checks": [{"context": "powerops-flow-tests", "integration_id": 15368}]}}]}` (15368 is the GitHub Actions app, read from `gh api /apps/github-actions` on 2026-10-26). If the API refuses a field, the result says what was changed.
  - Check 1: a same-repository PR into `gate` that changes the seed flow's `Update_a_row` input. After its run has finished (`gh run watch`), the required check's conclusion in `statusCheckRollup` is `FAILURE`, the check run's annotations (`gh api repos/<owner>/<repo>/check-runs/<id>/annotations`) name `flowtests/<file>` and the failed assertion, and `mergeStateStatus` is `BLOCKED`. Check 2: a fix commit that restores the input; after its run the check is `SUCCESS` and `mergeStateStatus` is `CLEAN` (`UNSTABLE` would mean another check failed, which deleting the other workflows on `gate` rules out); the PR is then closed without merging. Check 3: a PR into `gate` from the fork `chrome71-test/powerops-action-test` with the same break: the check runs and fails with the same annotation, and the state is `BLOCKED` (if the run waits for approval, it is approved through `POST /repos/<owner>/<repo>/actions/runs/<id>/approve`, and the result says so).
  - Afterwards: the ruleset is deleted, the PRs closed and their branches deleted; the `gate` branch stays for a rerun. The results go into `decisions.md` (Log) and the BUILD-PLAN verification record.

## Solution and publisher

None. The live check uses the existing seed fixture (prefix `pops`) as a file in the test repo; nothing is imported.

## Constraints

- Licensing: none expected. The test repo is public; if GitHub refuses the ruleset for the account's plan, the live check records that and uses a branch protection rule instead.
- No tenant and no secret in the Action's new step, the example workflows or the live check; the examples test committed files only (Data).
- Sources: GitHub's docs pages "Troubleshooting required status checks" (skipped workflow pending, skipped job success), "About protected branches" (unique job names), "Events that trigger workflows" (`merge_group`) and the Checks API reference (10 error annotations per step, the annotations endpoint), read on 2026-10-26; the rest is this repository's own code and observed output.
- No new dependency. bash 3.2 on macOS CI. No version or changelog edits; no push or PR from this session.

## Assumptions

- The check's name on a pull request is the job's `name:`, else its id, and a ruleset matches it with the GitHub Actions app as the source (observed in the live check).
- `chrome71-test/powerops-action-test` is still a fork of the test repo that this account can push to (read on 2026-10-26: admin permission).
- `jq` is on GitHub-hosted runners, as the Annotations and comment steps already assume.
- The account and repository names in this plan are the project's public handles, as in the README (R17).

## Open questions

## Questions for your admin

## Steps

1. `docs:` this plan, its decisions and review, signed off (`docs/plans/m15-merge-gate/`).
2. `feat(action):` test-first: the `test.sh` tests in `ActionScriptTests` and the `ActionYmlTests` test, seen failing; then `action/test.sh` and the `action.yml` input and steps; then the README merge-gate section, the `plan-file` bullet, the example inputs and the `docs/examples/README.md` line. Then `ci:` the `tests` input in `ci.yml`'s `action` job.
3. `feat(skills):` the `powerops-implement`, hand-off and PLANNING-SKILLS wording with their `SkillsCommandTests` and `HandoffDocTests` pins, seen failing first; regenerate the plugin copies as under Skills and hand-off and check `git diff tests/golden plugins`.
4. Run `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` and `powerops plan status`.
5. Ask the user for a yes to the live check in the test repo. On yes, run it (Environments and route) and record the results; without it, record it as waiting on the user.
6. `docs:` BUILD-PLAN: the M15 row marked built with a link to this plan and the three changes from the row (the apply gate, the promotion example left out because it tests Dev's export, the ephemeral recipe left out), build-order item 40, the M15 verification-record line (the live result, or "waiting on the user"), the "Last updated" date; this plan's status.
