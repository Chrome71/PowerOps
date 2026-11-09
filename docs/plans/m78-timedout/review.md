plan-hash: 1c7a1f4655ae83151e3843049df27f29ba26dafcefff8eedd4c5ffdb280723d8
model: claude-sonnet-5-5
fresh-context: yes

Environment facts were not read: the plan names no environment (`environment: null`), it changes this repo's own code (the offline flow simulator) and docs, and the user did not confirm a `pac` sign-in. Checklist items 1 to 8 (solution boundaries and prefix, connection references, licences, DLP, route, managed state, delegation, security roles) do not apply and are not reported. Items 9 (testability), 10 (open questions: 0, nothing under "Questions for your admin") and 11 (size; the plan has a "Won't build" list and no table, list, flow or screen) do apply. The scan for URLs, GUIDs, emails and names found nothing in `plan.md`; `decisions.md` holds two `learn.microsoft.com` paths in its research line, which the request allows. Every line number of the plan was checked against `517226e` by reading; no test was run and no code changed. `plan.md` was not edited (`review: pending` stays), because this request allowed no change outside `review.md`.

Checked and found right: `FlowTest.cs:27-28` and `128-142`; `FlowSimulator.cs:7`, `60`, `95-96`, `242-244`, `499-501`, `517`, `584-586`, `625`, `790-816`, `831`, `845`; the schema lines `61`, `66` and `106` (the only two "never times out" sentences in the schema, and `FLOW-TESTS.md:29` the only one in the docs; no other doc, skill or golden says `TimedOut` never occurs); `RunBlock` has exactly the five callers the plan lists (`Execute`, Scope, `Branch`, `Foreach`, `Until`); `OutputSchemaTests.cs:137, 152, 159, 160`; the existing `Failed` tests at `FlowSimulatorTests.cs:235, 321, 458, 495`. The fixture claim of row 8 holds: in `ReadEnvironmentVariable`, `GetDefinitions_2` is the first action of `Scope_2`, `Get_Value_2` and `Condition` follow on `Succeeded` only (so they skip), `Respond_to_a_PowerApp_or_flow` runs after `Scope_2` on `Succeeded` only (skipped, and it lists neither `Failed` nor `TimedOut`, so the guard stays quiet), and `Response` lists `Skipped`, `Failed` and `TimedOut` and has a plain 409 body, so the walk gives run `Succeeded`, `Response` 409 and `Get_Value_2` needing no mock. The golden `test-fixtures.txt` shows 4 tests today, the only count that moves is `TestCommandTests.cs:176` (4 tests: 3 passed, 1 failed, becomes 5 and 4), and `MutationTests` picks files with `rng.Next(files.Count)`, so a new file does change the sequence, as the Assumptions say. The undo check of Done when 5 can work: undoing row 3 fails test (b), and undoing row 4 fails (e).

Second pass: `plan.md` was revised to answer R1 to R7 (new `bodyHash` above, same commit `517226e`), and each resolution below was checked against the revised plan and the code. R1, R3, R4, R5, R6 and R7 hold (the rows and the line numbers `:163`, `:176`, `:204`, `:526-547`, `:618-625`, `:969` match the code). R2 is withdrawn: its scenario cannot occur, see R2. Row 4's new design (a field cleared by `RunAction`, a set local to each run of `RunBlock`) gives R8 to R10. Terminate (`_ended`) and the `pending` loop raise no finding: after a Terminate the loop stops, `pending` actions are skipped by `SkipAction` without `RunAction` (they touch neither the field nor the local set, and the guard is not applied to them, which cannot change a run that has ended), `_runStatus` still overrides the mapped status, and each nested `RunBlock` has its own set, so nesting is safe; R10 asks only for a test. Open after this pass: 2 Medium (R8, R9) and 1 Low (R10).

Third pass: R8, R9 and R10 are withdrawn (answered by the revised row 4 and rows 7 (d) and (e2), checked against the code: every container writes the field last, an `ExpressionException` or a mocked container leaves it cleared by `RunAction`, the Terminate case ends in `_runStatus`, and (b) covers the mapping at `Execute`); no new finding, none open.

## Findings

### High

No findings.

### Medium

#### R1. Done when 4 covers If, Switch and Until, but no test in row 7 reaches them
- Area: 9 (testability), Done when 4 and 5
- Finding: Done when 4 says a Scope, If, Switch, Foreach or Until whose block failed only through unhandled timeouts is recorded `Failed`, and row 4 changes `Branch` (`FlowSimulator.cs:625`), `Foreach` (`:808`, `:816`) and `Until` (`:831`, `:845`). Test (e) of row 7 names only a nested Scope and a Foreach iteration; (d) only a Scope. `Branch` and `Until` pass `RunBlock`'s result straight into `Status(...)`, so a slip there (no mapping of the new third value) records the container as status `TimedOut`, a status the plan never means a container to have. Nothing in row 7 would catch it, and Done when 5's undo check is made on the two `RunBlock` changes as wholes, so it cannot show that each container's own edit is covered.
- Suggested fix: add to row 7 one case each for an If (or a Switch case) and for an Until with an unhandled timeout inside, asserting the container is `Failed`, that a Catch on `["Failed"]` alone is `unsupported`, and that one on both statuses runs. Say in Done when 5 that undoing the edit in `Branch`, in `Foreach` or in `Until` alone also fails a test.
- Resolution: Answered. Row 7 (e) adds an If, an Until and a Foreach with an unhandled timeout inside, each `Failed`, with a Catch on one status `unsupported` and on both running; Done when 5 now undoes the edit in Scope, `Branch`, Foreach and Until each on its own.

#### R2. A name left in `_timeoutOnly` by an earlier iteration can raise the guard for a container that was skipped
- Area: 9 (testability), Data row 4
- Finding: Row 4 removes a container's name from `_timeoutOnly` when it "ends any other way", and the guard looks only at whether a predecessor's name is in the set. Names are unique in the flow (`_actions` is keyed by name), but a Foreach or Until runs the same block once per iteration, and a container in it can be skipped in a later iteration by `SkipAction` (`FlowSimulator.cs:475`, or the `pending` loop at `:494`), which is not "ends". The name then stays in the set from the iteration where it timed out, and in the later iteration the dependent that lists `["Failed"]` alone is stopped as `unsupported` for a container that did not run. With mocks that differ per call (`[Succeeded, ...]` lists are consumed one per call, `FlowSimulator.cs:639-657`) this is easy to build: iteration 1 times out inside `X`, iteration 2's earlier action fails so `X` skips.
- Suggested fix: make the guard require both that the name is in the set and that the predecessor's `done` status in this block run is `Failed`, or clear the set's entry when the container is skipped; write down which in row 4. Add the two-iteration case to row 7 (a quiet case: no `unsupported` when the container was skipped).
- Resolution: Withdrawn (second pass). The scenario of this finding cannot happen: a dependent that lists exactly one of `Failed` and `TimedOut` is stopped by the guard in the first iteration, where the container times out, so a later iteration never gets to read a stale name. The plan's local set is still the simpler design and is kept, but its test (e2, fourth case) cannot be written, see R8.

#### R3. What a Foreach does with iterations that differ is not defined
- Area: 9 (testability), Data row 4
- Finding: Row 4 says "Foreach (over its iterations)" and "Until (its last iteration)", and `RunBlock` now has three results. Today the Foreach takes `failed |= RunBlock(...) == "Failed"` (`FlowSimulator.cs:808`), which is false for the new third value, so left as is a Foreach whose only bad iteration timed out would end `Succeeded` and not stop the run. The plan does not say what a mix gives: one iteration `Failed` and another timed out (the container must not be in `_timeoutOnly`, since a real failure is in it and the guard's Learn reasoning does not hold), or one timed out and the rest `Succeeded` (timeout-only). No test names this.
- Suggested fix: add a sentence to row 4: a Foreach is timeout-only when no iteration is `Failed` and at least one is `TimedOut`; otherwise `Failed` as today. Add a row 7 case with two iterations, one timed out and one failed, asserting the Foreach is not timeout-only (a Catch on `["Failed"]` alone then runs).
- Resolution: Answered. Row 4: a Foreach is `Failed` when an iteration is `Failed`, else timeout-only when one timed out, else `Succeeded`; row 7 (e2) adds the mixed case with a Catch on `["Failed"]` that runs.

### Low

#### R4. Done when 1 says "both messages name the three statuses", but the `error` message names two
- Area: 9 (testability), Done when 1 and Data row 1
- Finding: Row 1's new `error` message is `'error' is only for a mock with status 'Failed' or 'TimedOut'.`, which names two statuses, while Done when 1 says both messages name three. The three-status messages are the parser's `status` message and the simulator's mock-check message (row 2), so "both" is also unclear (two or three messages?). A test written from Done when 1 would not match row 1.
- Suggested fix: write "the `status` messages of the parser and the simulator name the three statuses, and the `error` message names `Failed` and `TimedOut`".
- Resolution: Answered. Done when 1 now says the `status` messages of the parser and the simulator name the three statuses and the `error` message names `Failed` and `TimedOut`.

#### R5. `result()` now returns `TimedOut` entries, and neither a test nor the docs say so
- Area: 9 (testability), Data rows 6 and 7
- Finding: `result('Scope')` (`FlowSimulator.cs:131-139`) copies each child's status, so a Catch that filters `result('Try')` on `status eq 'Failed'` (the pattern of the test at `FlowSimulatorTests.cs:458`) silently finds nothing for a timed-out child. The plan's row 6 explains `runAfter` but not this; a maker reading `FLOW-TESTS.md` to write the F18 test will meet it first.
- Suggested fix: in test (d), add a Query over `result()` that selects the `TimedOut` child, and add one clause to the new bullet of row 6 ("`result()` lists a timed-out action with status `TimedOut`, so filter on both").
- Resolution: Answered. Test (d) adds a Query over `result()` that finds the `TimedOut` child, and row 6's bullet says to filter on both statuses.

#### R6. A few references of rows 7 and 8 name a different line than the one meant
- Area: 9 (testability), Data rows 7 and 8
- Finding: `TestCommandTests.cs:176` is the assertion line (the 4 tests count); the method `A_changed_condition_fails_…` starts at `:163`. `FlowSimulatorTests.cs:970` is the method line of the mock-check theory; the `Skipped` case is the `InlineData` at `:969`. Also, an existing test, `An_action_that_runs_after_TimedOut_is_skipped_when_its_predecessor_succeeds` (`FlowSimulatorTests.cs:204`), is about the status this plan touches and is not in the list of tests that stay unchanged. It does stay green (the predecessor succeeds, so no guard and no unhandled status), but the plan should list it so the builder does not edit it.
- Suggested fix: use `:163` and `:969`, and add `:204` to the unchanged list in row 7.
- Resolution: Answered. Row 8 names `:163` (the method) and `:176` (its count), row 7 names `:969`, and `:204` is in the unchanged list.

#### R7. `Branch` and `Until` have no action name to add to `_timeoutOnly`
- Area: 9 (testability), Data row 4
- Finding: Row 4 has each container "add its own name". `Branch(JsonObject action, JsonNode? taken)` (`FlowSimulator.cs:618`) receives the definition only, and `Until(ActionInfo action)` and `Foreach(ActionInfo action)` receive the `ActionInfo`. The name is at `Perform`'s caller (`RunAction`, which has `action.Name`), so the set update fits best in `RunAction` from a flag that `Perform` returns or sets, not in each container method. The plan's line list (`625`, `831`, `845`) points to the container methods.
- Suggested fix: say in row 4 where the set is updated (one place in `RunAction`, after `Perform`, from what the container returned), so the change stays small and the removal "any other way" happens once.
- Resolution: Answered. Row 4 sets the flag in one helper that the containers return through and records names in `RunBlock` right after `RunAction`, which has the action's name; the line list adds `RunAction` (`:526-547`) and `Branch` (`:618-625`).

### Added in the second pass

#### R8. The fourth quiet case of (e2) would be `unsupported` in its first iteration
- Area: 9 (testability), Data row 7
- Finding: (e2) lists "a Foreach whose Scope times out in its first iteration and is skipped in its second, with an action on `["Failed"]` after it" as a case where no `unsupported` occurs. In the first iteration the Scope is timeout-only and the action after it lists `Failed` alone, so the guard stops the run as `unsupported` there (the same definition runs in every iteration). The case cannot be quiet, and with the local set there is no state it could check.
- Suggested fix: drop the case. If a skipped-container case is wanted, make it a plain one: a Scope skipped in iteration 2 and one action on `["Failed","TimedOut"]` after it, asserting run `Succeeded`.
- Resolution: Withdrawn (third pass). Answered. The case is dropped from (e2): a dependent sees its container in the same iteration, so a stale name can't reach it (as R2's withdrawal says).

#### R9. The field must be written on every way out of a container, or an inner timeout-only container leaks into the outer one
- Area: 9 (testability), Data row 4
- Finding: Row 4 says one helper "turns that value into `Failed` and sets the field", `RunAction` clears the field only before `Perform`, and the field is read right after `RunAction`. A child container that is timeout-only leaves the field true when its `RunAction` ends. If the outer container then ends through a path that does not write the field, it keeps the child's value: a Foreach whose last iteration ended with a timeout-only Scope but whose result is `Failed` because an earlier iteration failed; a Foreach that breaks on `_ended`; a Scope or If whose block result is `Succeeded` or `Failed` if the helper writes only for `TimedOut`. The outer container would then be put in the local set and the guard could stop a run that should go on (a Catch on `["Failed"]` after a Foreach with a real failure). The e2 mixed-Foreach case in row 7 does not catch it, since its timed-out iteration probably holds a plain mocked action, which leaves the field false.
- Suggested fix: say in row 4 that every container writes the field once, as its last step, from the value it returns (true only for `TimedOut`), including the Foreach and Until loops and the paths that run no block; and make the mixed-Foreach case use a timeout-only Scope in the last iteration and a real failure in the first, with a Catch on `["Failed"]` that runs.
- Resolution: Withdrawn (third pass). Answered. Row 4: every container (Scope, `Branch` taken or not, Foreach, Until) calls the helper once as its last step, which sets the field from the value it passes, also for `Succeeded` and `Failed`. The mixed-Foreach quiet case now fails in its first iteration and ends on a timeout-only Scope in its last.

#### R10. No test of Terminate after a timeout
- Area: 9 (testability), Done when 3 and Data row 7
- Finding: The usual shape of a timeout path is a Catch that ends in a Terminate (`_ended`). No case in row 7 or in the fixture runs it. By the reading above it works (the Catch handles the timeout, `_runStatus` from the Terminate wins, the unrun actions are skipped), but nothing would show it if the mapping of the third value at `Execute` (`Finish(_runStatus ?? status)`, `:96`) were done in the wrong order and `TimedOut` reached the report as a run status.
- Suggested fix: add to (d) a Catch scope on `["Failed","TimedOut"]` that holds a Terminate with `runStatus` `Failed`, asserting run `Failed`; and one with an unhandled timeout and a later Terminate `Succeeded`, asserting run `Succeeded`.
- Resolution: Withdrawn (third pass). Answered. Test (d) adds the Catch on both statuses ending in a Terminate with `runStatus` `Failed`, which makes the run `Failed`; (b) already covers the mapping at `Execute`.
