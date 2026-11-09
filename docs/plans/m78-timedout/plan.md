---
status: implementing
review: done
signed-off: 2026-11-08
signed-off-hash: 1c7a1f4655ae83151e3843049df27f29ba26dafcefff8eedd4c5ffdb280723d8
environment: null
depth: quick
supersedes: null
---

# M78: `TimedOut` in mocks and action-status assertions

## Goal

The 1.0 trial (findings F18 and V1) wanted a flow test that checks a flow's timeout handling. That can't be written today. A mock takes only `Succeeded` or `Failed` (`FlowTest.cs:27`, `FlowSimulator.cs:242`), an `actionStatus` assertion takes no `TimedOut` (`FlowTest.cs:28`), and a block counts only `Failed` as unhandled (`FlowSimulator.cs:499-501`), although `runAfter` already accepts `TimedOut` (`:60`, `:517`). This plan lets a test mock a timeout and follow the flow's `runAfter` from it. The simulator doesn't wait or time anything: a timeout happens only when a mock says so. This is milestone M78 of the agreed 1.0 fix plan.

Done when:

1. A mock with `"status": "TimedOut"` is accepted by `FlowTestFile.Parse`, by the simulator's mock check and by `flow-test.schema.json`, with or without `error`. `error` with `Succeeded` is still refused. The `status` messages of the parser and of the simulator name the three statuses, and the `error` message names `Failed` and `TimedOut`. Any other status (`Skipped`, `Cancelled`) is still refused.
2. An `actionStatus` assertion takes `"equals": "TimedOut"` in the parser and the schema. `Cancelled` is still refused there, and the `runStatus` list doesn't change.
3. In each block, an action that ended `Failed` or `TimedOut` is handled when an action of the same block that ran (not `Skipped`) lists that same status in its `runAfter` for it. A block with an unhandled one is `Failed`. The run takes its top block's status as today, so an unhandled timeout fails the run. The rule for `Failed` gives what it gives today.
4. A Scope, If, Switch, Foreach or Until whose block failed only through unhandled timeouts is recorded `Failed`. That covers a timed-out action, or a container in the block that failed this way itself. The run is `unsupported` when an action after such a container lists one of `Failed` and `TimedOut` for it but not the other. The message names both actions and says to list both statuses (decisions Q1).
5. The tests of Data row 7 are written first and pass. Each of these edits, undone alone, fails at least one of them: row 3; row 4's guard; row 4's third value in `RunBlock`; and its use in Scope, `Branch`, Foreach and Until, each on its own. The build records this check in the Log.
6. The schema's two "never times out" sentences are gone, `docs/FLOW-TESTS.md` says how timeouts are simulated and cites Learn, and one new fixture test in `tests/fixtures/flowtests/alm-makers` passes and is in the `test-fixtures.*` goldens.
7. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m78-timedout` and the leak scan are clean, and the work is committed locally.

### Won't build

- Simulating the wait: a clock, an action's `limit.timeout`, a connector's request limit or the run's duration limit. The fix plan rejected this. A timeout happens only when a mock says so.
- `TimedOut` as a run status. On Learn it is the run's duration limit (decisions, research), which an offline run has no clock for.
- A `Cancelled` mock with the code `ActionTimedOut`. One Learn page gives an asynchronous action that passes its `limit.timeout` that result (decisions, research). A test that needs it can mock `Failed` with that code.
- A rule in `analyze` that flags a handler listing `Failed` without `TimedOut`. That would be a new rule id and its own milestone.
- A `schemaVersion` change. `flow-test.schema.json` is a file format with no version, and the report's `runStatus` list doesn't change.

## Users

- A maker or an agent who writes a flow test for a flow's timeout path (the trial's F18), and reads `docs/FLOW-TESTS.md` and the schema to write it.
- A reviewer of a flow test who needs `powerops test` to fail, or say `unsupported`, when Learn doesn't settle how the cloud would run the timeout path.

## Data

Line numbers are those of `517226e`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | `MockStatuses` and `ActionStatuses` add `TimedOut`, and `RunStatuses` stays as it is. The messages become `'status' must be 'Succeeded', 'Failed' or 'TimedOut'.` and `'error' is only for a mock with status 'Failed' or 'TimedOut'.` | `src/PowerOps.Core/Flows/FlowTest.cs:27-28, 128-142` | `FlowTestTests`, `OutputSchemaTests` (row 7) |
| 2 | The mock check accepts `TimedOut`, and its message says "use Succeeded, Failed or TimedOut". `FlowMock`'s summary names the three statuses | `src/PowerOps.Core/Flows/FlowSimulator.cs:7, 242-244` | `FlowSimulatorTests` (row 7) |
| 3 | The end of `RunBlock` counts an action that ended `Failed` or `TimedOut` as handled only when an action of the block that ran lists that same status for it. The `"Failed"` literal in `Handled` becomes the action's own status, and the filter takes both statuses | `FlowSimulator.cs:499-501` | row 7, first four simulator tests |
| 4 | `RunBlock` returns a third value, `TimedOut`, when every unhandled action in the block either timed out or is a timeout-only container (below). One helper turns that value into `Failed` and sets the field `_timeoutOnly` from it, also when the value is `Succeeded` or `Failed`. Every container calls it once, as its last step: Scope and Until (its last iteration) with their block's result, `Branch` (If, Switch) with the taken block's result or `Succeeded`, and Foreach with `Failed` when an iteration is `Failed`, else `TimedOut` when one is `TimedOut`, else `Succeeded`. So a value an inner container left behind never stays. The run maps the value to `Failed`. `RunAction` clears `_timeoutOnly` before `Perform` and again in its `ExpressionException` catch. So the field describes the action just run, and a container that fails for its own reason after an inner timeout isn't timeout-only. Right after `RunAction`, `RunBlock` adds the action's name to a set local to that run of the block if the field is set. Nothing carries over from one iteration to the next, so a container skipped in a later iteration is never in it. Before an action runs or is skipped, `RunBlock` stops the run as `unsupported` if, for a predecessor in that set, the action lists exactly one of `Failed` and `TimedOut`. The message: `Action '<next>' runs after '<container>' on <status> only, and '<container>' failed because an action in it timed out: Learn doesn't say whether '<container>' is then Failed or TimedOut. List both in its runAfter.` | `FlowSimulator.cs:95-96, 466-477, 499-501, 526-547, 584-586, 618-625, 790-816, 831, 845` | row 7, the guard tests |
| 5 | The mock's `status` enum adds `TimedOut`, and its description becomes "The action's status. TimedOut stands for a timeout; the run doesn't wait." `error`'s description says "Only with status 'Failed' or 'TimedOut'.", and the `if`/`then` stays. `actionStatus`'s enum adds `TimedOut`, and the sentence "TimedOut never occurs in a simulated run." goes | `docs/schemas/flow-test.schema.json:61, 66, 106` | `OutputSchemaTests` |
| 6 | `FLOW-TESTS.md`: the "Not simulated" bullet keeps retry, concurrency, secure inputs and waiting for a timeout, and drops "`TimedOut` never occurs". A new bullet: a mock can be `TimedOut`. `runAfter` treats it as a status of its own, so a handler must list `TimedOut` to run after it (Learn's *Handle workflow errors and exceptions*). An unhandled timeout fails its block and the run, as an unhandled failure does. That is the simulator's reading of Learn's run-status table, which defines `Failed` as an action that failed with nothing to handle it and keeps `TimedOut` for the run's duration limit. A container that fails only through a timeout reads `Failed`, the simulator's choice, because Learn doesn't say. A handler after it that lists only one of the two is `unsupported`, so list both. `result()` lists a timed-out action with status `TimedOut`, so a Catch that filters it on `Failed` should take `TimedOut` too | `docs/FLOW-TESTS.md:29` | none (prose) |
| 7 | Tests, written first. `FlowSimulatorTests`: (a) a `TimedOut` mock runs the `["TimedOut"]` successor and skips the `["Succeeded"]` and `["Failed"]` ones, and the run is `Succeeded`; (b) an unhandled timeout makes the run `Failed`; (c) a handler that is itself skipped doesn't handle a timeout; (d) a timeout inside a Scope, with a Catch inside the Scope (Scope `Succeeded`) and with a Catch after it on `["Failed","TimedOut"]` (Scope `Failed` with `ActionFailed`, run `Succeeded`, and a Query over `result()` in the Catch finds the child with status `TimedOut`), and the same Catch ending in a Terminate with `runStatus` `Failed` (run `Failed`); (e) a Catch after that Scope on `["Failed"]` alone and on `["TimedOut"]` alone is `unsupported` and names both actions; the same for an If, an Until and a Foreach with an unhandled timeout inside (each recorded `Failed`; a Catch on both statuses runs), and through a Scope nested in a Scope; (e2) quiet cases, where no `unsupported` occurs and today's behaviour holds: a Scope that failed for a real failure, with a Catch on `["Failed"]`; a Foreach whose first iteration fails and whose last ends on a timeout-only Scope, with a Catch on `["Failed"]`; and an Until whose body times out and whose condition then fails; (f) a `TimedOut` mock passes the mock check, and `Skipped` stays refused there (`:969`). The existing tests `:204` (`TimedOut` in `runAfter`), `:235`, `:321`, `:458` and `:495` stay unchanged. `FlowTestTests`: a `TimedOut` mock with `error` and an `actionStatus` `TimedOut` assertion are read. `OutputSchemaTests`: `Mock({"status":"TimedOut"})` (`:137`) and the `actionStatus` `TimedOut` assertion (`:159`) move from the invalid list into the valid case, `Mock({"status":"Cancelled"})` takes `:137`'s place, and `actionStatus` `Cancelled` (`:160`) and `runStatus` `TimedOut` (`:152`) stay invalid | `tests/PowerOps.Core.Tests/Flows/FlowSimulatorTests.cs`, `FlowTestTests.cs`; `tests/PowerOps.Cli.Tests/OutputSchemaTests.cs:137, 159` | itself |
| 8 | The fixture test `read-environment-variable-timed-out.flowtest.json` runs the public sample flow `ReadEnvironmentVariable`. Its `GetDefinitions_2` mock is `TimedOut` with an `error`. `Scope_2` then fails, and its `Response`, which runs after `Scope_2` on `Skipped`, `Failed` and `TimedOut`, answers 409. It asserts `runStatus` `Succeeded`, `GetDefinitions_2` `TimedOut`, `Respond_to_a_PowerApp_or_flow` `Skipped` and a `response` `statusCode` of 409. It doesn't assert `Scope_2`'s own status, which row 6 calls the simulator's choice. The `test-fixtures.txt`, `.json` and `.md` goldens gain the test by the recipe "Update goldens" | `tests/fixtures/flowtests/alm-makers/`; `tests/golden/test-fixtures.*` | `TestCommandTests` goldens; `A_changed_condition_fails_…` (`TestCommandTests.cs:163`): its count at `:176` moves to 5 tests, 4 passed |

## Automation

None. The simulator runs cloud flows offline, and this plan adds no flow.

## Screens

None.

## Environments and route

None. `powerops test` stays offline and read-only, and nothing reads or writes a tenant.

## Solution and publisher

None. The fixture test runs a flow from the public `alm-makers` sample, which is unchanged.

## Constraints

- AGENTS.md: the smallest change, new logic with tests, no new write, Conventional Commits, no version or changelog edit, no tenant data in Git.
- Fix plan section 4: `COMMANDS.md`, `schemas/README.md` and golden regeneration land one PR at a time. This plan touches neither of the two docs, and its goldens are only `test-fixtures.*`. If M74's PR is ready at the same time, whichever lands second rebases.
- The kept roles `powerops-verify` and `powerops-review-security`, their plugin copies and `tests/eval` stay untouched.
- Platforms are Windows and Linux. Nothing here depends on the platform.

## Assumptions

- A test writer takes a timed-out action's status and error from the run history, so a mock with the status `TimedOut` is enough. Whether the cloud gives a given action `TimedOut` or `Cancelled` with `ActionTimedOut` is not the simulator's to decide (Won't build).
- `MutationTests` picks files from the fixtures with a fixed seed, so a new fixture file changes which mutants run. A mutant that then fails shows an existing bug, which the build fixes or reports.
- No Learn page says how the cloud runs an unhandled timeout, at the run or the container level (decisions, research). Rows 3, 4 and 6 are written to that gap.

## Open questions

## Questions for your admin

## Steps

1. This plan and `decisions.md`, a fresh-context review into `review.md`, fixes, and the person's sign-off.
2. Row 7's tests, run and seen to fail.
3. Rows 1 to 4 until they pass, then the undo check of Done when 5.
4. Rows 5, 6 and 8, then the goldens by the recipe "Update goldens", reading the diff.
5. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m78-timedout` and the leak scan; a local commit. No push or PR without the person's yes.
