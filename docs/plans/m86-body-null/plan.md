---
status: done
review: done
environment: null
depth: quick
supersedes: null
signed-off: 2026-11-11
signed-off-hash: 8a0933c3185e83a395e643b4b3a99d12a2c8853dcbf75731023d6ab443874c3c
---

# M86: `body()` of an action that did not run gives null

## Goal

In trial 3 (2026-11-11, record finding P5, staged case N8), `powerops test` failed two of 17 flow tests on correct expressions, and a specialist reported two High findings against correct flows on the simulator's word. Both expressions read `body()` of an action that did not run. Today `body` reads the property `body` from `outputs(x)` with `optional: false` (`ExpressionFunctions.cs`, the `body` entry), so `Expression.Read` throws "Cannot read a property or item of null." whenever the outputs are null. `FlowSimulator.Outputs` gives null for a Skipped action, and a `Failed` or `TimedOut` mock with no `outputs` has null outputs too. A live run in the trial's Dev environment showed the service gives null for `body()` of a skipped action. This is milestone M86 of the fix plan after trial 3.

Done when:

1. `body(x)` gives null for any action `x` whose outputs are null, and only then: the read is optional only for a null target. Outputs are null, for example, for a skipped action, for a mock of any status with no `outputs` (`FlowSimulator.cs:697`), for Terminate, Wait and the variable actions (`:632`, `:642`, `:739`, `:785`), for a Compose whose value is null (`:599`), for an action that failed on an expression error (`:562`), for an invalid Parse JSON (`:903`) and for containers (If, Switch, Scope, Foreach and Until through `Status()` at `:648`, and a failed Scope at `:617`). A run settled this only for a skipped action; everywhere else it is the simulator's choice. Outputs that exist but hold no `body` property still throw "The property 'body' does not exist.", and `Body_is_outputs_body_and_errors_when_there_is_none` stays as it is. `body('A')['x']` of a skipped `A` still throws "Cannot read a property or item of null.". An action that has not run still throws "has not run". `triggerBody()`, `Expression.Read` and `FlowSimulator.Outputs` are unchanged.
2. The tests of Data row 3 are written first. The red set fails before row 1 and passes after it: the two new `ExpressionTests` cases, `FlowSimulatorTests` (a), (b) and (f), and the skipped and timed-out tests of the fixture (row 4). The guard set passes before and after: `FlowSimulatorTests` (c), (d), (e), the fixture's failed-with-body test, and the existing `Body` and `TriggerBody` tests. Undoing row 1 alone fails the whole red set and nothing else; the build records this check in `decisions.md`.
3. `docs/FLOW-TESTS.md` says it, as Data row 2 lists.
4. With the worktree's build (`dotnet run --project src/PowerOps.Cli --`, not the installed `powerops`), `powerops test tests/fixtures/flowtests/skipped-body --tests tests/fixtures/flowtests/skipped-body` exits 0 with 3 tests, 3 passed, and a `TestCommandTests` case runs it in `dotnet test`. The fixture is in `tests/fixtures/NOTICE.md`, and `analyze` of it gives no finding (a `FixtureRuleTests` case).
5. The frozen eval set's flow tests give the same output, run with the worktree's build both times (`dotnet src/PowerOps.Cli/bin/Debug/net10.0/powerops.dll`): for each of the eight cases `tests/eval/roles/cases/solution-0N`, `powerops test <case>/solution/current --tests <case>/flowtests --format json` is byte-identical before and after row 1, recorded in `decisions.md`. A difference stops the build and goes to the person, because it touches the pinned evaluation.
6. `dotnet build` (0 warnings), `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m86-body-null` (the worktree's build) and the leak scan are clean; merged by the chain's merge protocol (both named under Assumptions).

### Won't build

- `body()` as the full `outputs(x)?['body']`. The fix plan rejected it: it would also give null for outputs that exist without a `body` property, a behaviour no run settled.
- A change to `outputs()`, `triggerBody()`, `Expression.Read` or the skipped-action outputs in `FlowSimulator.Outputs`.
- A live run of a timed-out or failed action. Their null is the simulator's choice (Assumptions).
- A `schemaVersion` change: the test report's fields do not change.

## Users

- A maker or an agent whose flow reads `body()` of an action that may be skipped, time out or fail without outputs, and who needs `powerops test` to pass the flow when its logic is right.
- A specialist or reviewer who reads a failed flow test as a finding against the flow.

## Data

Line numbers are those of `b065f23`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | The `body` entry reads `body` with `optional` true only when the outputs are null: `Expression.Read(o, JsonValue.Create("body"), o is null)`, with `o` the outputs of the named action | `src/PowerOps.Core/Flows/ExpressionFunctions.cs:26` | row 3 |
| 2 | A new item after "Settled by the capture", of its own: "Seen in one more live run, in a developer environment on 2026-11-11, whose record is not kept in this repository: `body(x)?['k']` of a skipped action gives no error, so the simulator gives `null` for `body()` of a skipped action, as for `outputs()`." The "compared with one real run" sentence stays: the capture is still the one run compared action by action, and the new item says the 2026-11-11 run was a single observation. A new item under Limits says that `body()` of any action whose outputs are null gives `null`, as `outputs()` does: settled by a run only for a skipped action, and the simulator's choice everywhere else, for example a mock of any status (`Failed` and `TimedOut` included) with no `outputs`; outputs without a `body` property still give an error; a test that needs another shape mocks `outputs` | `docs/FLOW-TESTS.md:31-33` | reading |
| 3 | Tests first. `ExpressionTests`: the fake context gains an action whose outputs are null; `body()` of it gives null, and `body(x)?['code']` gives null; in the same case, `body(x)['code']` still throws "Cannot read a property or item of null." (a guard); the existing `Body_is_outputs_body_and_errors_when_there_is_none` (`:317-322`) and `TriggerBody` tests stay unchanged. `FlowSimulatorTests`: (a) a skipped action, then a Compose of `coalesce(body('A')?['x'], 'd')` gives `d`; (b) `body('A')` of a `TimedOut` mock without `outputs` gives null; (c) `body('A')` of a `Failed` mock with `outputs` `{"body":{"code":1}}` gives `{"code":1}`; (d) `body('A')` of a Succeeded mock whose outputs have no `body` still fails the run with "The property 'body' does not exist."; (e) `body('B')` of an action that has not run yet still fails with "has not run"; (f) `body('A')` of a Succeeded mock with no `outputs` gives null. Red before row 1: the two `ExpressionTests` cases, (a), (b), (f); the rest are guards (Done when 2) | `tests/PowerOps.Core.Tests/Flows/ExpressionTests.cs`, `FlowSimulatorTests.cs` | itself |
| 4 | Fixture `tests/fixtures/flowtests/skipped-body/`: a hand-written unpacked solution `PowerOpsSkippedBody` (publisher `demopub`, prefix `demo`, ids `00000000-…`), layout as `flow-navigation/`, with one cloud flow "Skipped body": a manual trigger with a boolean `skip`; an If on it whose false branch holds an HTTP action `Get_status` (a GET on `https://api.contoso.com/status`, neither a SharePoint nor a Dataverse host) <!-- plan-status: allow -->; then a Compose `Pick_code` that runs after the If on `Succeeded`, `Failed` and `TimedOut` with `@coalesce(body('Get_status')?['code'], 'd')`. The flow is on: its `.json.data.xml` has `StateCode` 1, as in `flow-navigation/`. Three tests in the folder: `skip` true (`Get_status` Skipped, `Pick_code` `d`, run Succeeded); a `TimedOut` mock with no outputs (`Pick_code` `d`); a `Failed` mock with `outputs` `{"body":{"code":1}}` (`Pick_code` 1). No connection reference. Each test file has the `$schema` line of `flowtests/alm-makers`. A `NOTICE.md` paragraph says it is made for this project, hand-written and never imported, and that `analyze` finds nothing in it. `SolutionDiscoveryTests.Finds_fixture_folders_and_zips_sorted` gains `flowtests/skipped-body` after `flowtests/capture/solution`. `TestCommandTests` gains a case that runs the folder and asserts exit 0, empty stderr and `3 tests: 3 passed, 0 failed, 0 unmocked, 0 unsupported`. `FixtureRuleTests` gains a case that loads it and asserts no finding | `tests/fixtures/flowtests/skipped-body/`, `tests/fixtures/NOTICE.md`, `tests/PowerOps.Core.Tests/Readers/SolutionDiscoveryTests.cs:17-37`, `tests/PowerOps.Cli.Tests/TestCommandTests.cs`, `tests/PowerOps.Core.Tests/Rules/FixtureRuleTests.cs` | Done when 4 |
| 5 | Goldens by the recipe "Update goldens": the `analyze-fixtures.txt`, `.json` and `.md` goldens (`ReportTests` analyzes all of `tests/fixtures`); `.sarif` is expected unchanged, because the fixture has no finding. The diff holds one new solution entry with no findings and the changed counts; the `test-*` goldens are unchanged; `tests/golden/selftest.txt` is restored; no diff in `plugins/`, `skills/` or `tests/eval/` | `tests/golden/` | `ReportTests` |

## Automation

None. The simulator runs cloud flows offline; the fixture's flow is never imported.

## Screens

None.

## Environments and route

None. `powerops test` stays offline and read-only; nothing reads or writes a tenant.

## Solution and publisher

The fixture solution `PowerOpsSkippedBody`, publisher `demopub` (prefix `demo`), hand-written and never imported.

## Constraints

- AGENTS.md: the smallest change, tests first, no new write, Conventional Commits, no version or changelog edit, no tenant value in Git (fixture values are fake).
- Chain rules: tenant offline; the kept roles, their plugin copies and `tests/eval/` stay untouched; one PR at a time under the merge lock; goldens regenerated after the rebase, not hand-merged.
- Fix plan section 4: this plan owns `Flows/ExpressionFunctions.cs`; `analyze-fixtures.*` is shared with M87 and M92, so whichever merges later regenerates it.
- Platforms are Windows and Linux; nothing here depends on the platform.

## Assumptions

- The service gives no error for `body(x)?['k']` of a skipped action: observed in trial 3's live run in Dev (record P5, E88; Add the supplier Skipped, the `coalesce(body(...)?[…], …)` in the next action Succeeded). That `body(x)` alone is `null` (not, say, an empty object) is the simulator's reading of it, matching `outputs()` of a skipped action. `outputs()` of a skipped action gives null: the capture run of 2026-11-05.
- `body()` of other actions with null outputs (for example a mock of any status with no `outputs`, Terminate, Wait, the variable actions, a Compose of null, a container, an action that failed on an expression error) gives null by the same rule; no run settled these, so it is the simulator's choice, and a test that needs another shape mocks `outputs`.
- `MutationTests` copies all of `tests/fixtures` and picks files by a seeded index, so new files shift its mutants. A mutant that then fails shows an existing defect: the build stops, releases the merge lock if held, and goes to the person with the mutant's line; the seed and the fixture are not changed to avoid it, and Done when 6 is met only after the person decides.
- Tools outside the repository: the leak scan is `reports/leakscan.sh` in the main checkout (git-ignored), run as `bash reports/leakscan.sh origin/main <branch>`; the merge protocol and the chain's other rules are in `reports/trial-3/chain/chain-rules.md` there (git-ignored), and the milestone's spec is section M86 of `reports/trial-3/fix-plan.md`.
- Learn on `body()` of a skipped, failed and timed-out action: see `decisions.md`, Research, checked 2026-11-11 through `powerops-researcher`. Where Learn says nothing, null for a `Failed` or `TimedOut` mock with no `outputs` is the simulator's choice, and the docs say so.
- Ecosystem (mode B): `docs/ecosystem/compatibility.md` and `changes.md` hold no row or open entry for the expression language or the simulator; the evidence used is from 2026-11-05 and 2026-11-11, under 90 days old.

## Open questions

## Questions for your admin

## Steps

1. This plan and `decisions.md`; the Fable plan review and `powerops-review-plan` in a fresh context into `review.md`; fixes; sign-off by the building session on the person's standing instruction.
2. Row 3's tests, run and seen to fail.
3. Row 1 until they pass.
4. Rows 2, 4 and 5, reading the golden diff.
5. The undo check of Done when 2 (after the fixture and its `TestCommandTests` case exist), the eval comparison of Done when 5 (its "before" run taken at step 2, before row 1), the checks of Done when 6, reading the golden diff; `status: done`; the merge protocol.
