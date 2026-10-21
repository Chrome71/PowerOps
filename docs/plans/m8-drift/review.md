plan-hash: 1683b1506d9a975366dfdd4563e5aac520ca26337210c5c03958523fdeabcabb
model: claude-sonnet-5-5
fresh-context: yes

Scope: `plan.md` and `decisions.md` of `m8-drift`, `AGENTS.md`, the M8 row, the Phase 3 section (environments, "What PowerOps reads", research basis), decisions 9 to 14 and build-order tasks 22 to 24 of `docs/BUILD-PLAN.md`, the house example `docs/plans/m7-settings-live/review.md`, and the code the plan names (`Program.cs`, `SettingsCommand.cs`, `PacRunner.cs`, `PlanFacts.cs`, `OutputSchemaTests.cs`, `tests/Shared/Golden.cs`, `Output/JsonOutput.cs`, `Settings/FlowState.cs`, `Model/Solution.cs`, the `tests/fixtures/pac` samples, `docs/schemas/README.md`). `powerops plan status` reports `approved`, `openQuestions` 2 (the two admin questions; the `Open questions` section is empty), `review: done` (already set in the header; left as it is) and one warning: the earlier review is stale for this hash. `powerops plan facts` was NOT run and `pac` was never called: contacting the tenant is forbidden for this review, so there are no environment facts and the overlap check of item 1 could not be done. No Microsoft Learn page was re-read; where a finding rests on Dataverse behavior that the plan's Sources do not state, it says so and asks Step 1 to settle it. Tenant data: no URL with a scheme, GUID, email or person's name in `plan.md` or `decisions.md` (the Sources are public documentation paths and `Contoso Dev` is a fake name, so neither is a finding).

Checklist items that do not apply: 1 (no solution or publisher is created; the existing seed `PowerOpsSeed`, prefix `pops`, is only read), 2 (no hard-coded value: environments come in as `--env`, and values are hashed; the only related gap is R5), 3 and 4 (no connector or table is added; the premium-licence and DLP questions are the same two admin questions as in `m7-settings-live` and are not raised again), 5 and 6 (the route is the user's Dev to Test and the tool only reads; managed in Test is shown as information by Q3), 7 (no app or delegation; the row limit is the 5,000-row page, which the plan states as out of scope), 8 (no roles are created; the reading account's role is the first admin question, and R7 notes a runtime effect of it). Items 9 (testability) and 10 (unanswered areas) apply and carry the findings. Beyond the checklist the plan was checked against the BUILD-PLAN row, AGENTS.md (read-only by default, JSON contract, `pac` as a subprocess with list arguments, smallest change, tests first, no tenant data in fixtures) and the existing code. The deviations from the row that the plan does state (no `--show-values`, flows from `workflow`, no `pac solution list`, the layer fallback) match decisions Q2 to Q4 and are not findings; R10 is one it does not state.

Earlier review (16 findings, 0 High, 7 Medium, 9 Low, written for plan hash eba24ff5), re-checked against the current plan. Fourteen are really resolved in the text: the layer read fallbacks and the offline wording (earlier R2 to R4), the nowhere-installed case with exit 2 (R5), the streams forwarded from a failed read and the timeout rule (R6, but see R1 below: its path rule is wrong), `DriftQueries` in Core with the name guard and the templates filled from read 2 (R7), the "same report" wording (R8), the by-design variable difference in Out of scope and the README (R9), the load-snapshots-first order (R12), the match keys, sort keys, `layersRead` and `--save` without a live environment (R13), the named `internal` helpers (R14), the schemas, `Map` entries and goldens moving into the CLI step (R15), and the Step 1 records of number or label output and `--environment` (R16). Two are only partly resolved. Earlier R1: the double reporting is gone, but excluding the whole of component type 29 from the component kinds opens a hole (R2 below), and the ids-differ risk is covered only by an Assumption with a type-level fallback. Earlier R10: the plan now says "not safe" in the Constraints, README and schema, but `decisions.md` Q2 still says "safe to keep as CI artifacts" and the plan narrows the user's recorded answer in a parenthesis (see R14). Earlier R11 is resolved for positions and name collisions; the wider file-name problem (trailing dots and spaces on Windows) is left alone as too small to raise.

## Findings

### High

#### R1. The `--env` path rule rejects environment URLs, which the same paragraph accepts as input
- Area: 9. Testability
- Finding: Data says an `--env` is "an environment id or URL, as `pac` takes them", and also that an `--env` "that ends in `.json` or contains a path separator but names no file is a usage error". Every environment URL contains `/`, so a URL (the form most people copy from the browser or from `pac org who`) is a usage error with exit 2 and is never sent to `pac`. A builder following the rule literally makes URLs unusable, and one following the first sentence breaks the rule. The test list covers only "a missing path that ends in `.json`", so nothing catches it. The live done-when passes `<Dev>` and `<Test>` without saying which form they are, so the defect can stay hidden until the live run. This rule came in with the fix for the earlier R6.
- Suggested fix: Make a value a path only when it ends in `.json` (case-insensitive) or exists as a file; treat a value that starts with a URL scheme as an environment even if it contains `/`. Drop the "contains a path separator" clause or restrict it to values with no `://`. Add a CLI test that passes a URL-shaped `--env` to the fake `pac` and asserts exit 0 and that the value reaches `--environment`.
- Resolution: fixed (a snapshot path is a value ending in .json or an existing file; URLs are environments; URL test added)

### Medium

#### R2. Leaving all of component type 29 out of `component-missing` and `component-extra` hides every workflow that is not a cloud flow
- Area: 9. Testability
- Finding: The table says cloud flows, environment variable definitions and values (types 380 and 381) and connection references "have their own kinds, so they are left out" of the component kinds. Type 29 is every workflow row: business rules, classic workflows, business process flows, desktop flows and actions, not only cloud flows (category 5). Read 4 takes only `category` 5. A missing or extra business rule or desktop flow therefore appears under no kind, and the run exits 0 with "no drift". That is a false negative on exactly the question the command exists to answer, and none of Steps 2 to 4 tests it. Which type number the connection reference has is also still unknown (Assumptions, Step 1), so the exclusion list cannot be written yet.
- Suggested fix: Exclude from the component kinds only the type 29 rows whose id is in the cloud-flow list (or compare the other workflow categories, or say plainly in Out of scope and the README that other workflow categories are not compared). Add a Core test: a type 29 component that is not a cloud flow, missing in the second environment, gives `component-missing`. Let Step 1 record the connection reference type number before the exclusion list is fixed.
- Resolution: fixed (only type 29 rows in the cloud-flow list are left out; other workflows compared; test added)

#### R3. "The solution's workflows, definitions and references" has no stated filter, and the `solutionid` column may not mean "in this solution" in an unmanaged Dev
- Area: 9. Testability
- Finding: Reads 4 to 6 say "the solution's workflows", "for the solution's definitions", "for the solution's references", and read 3 plus the templates say "filled with each environment's solution id". The only membership relation the plan names is the `solutioncomponent` list (read 3). Whether `workflow`, `environmentvariabledefinition` and `connectionreference` rows can be filtered by their own `solutionid` column is not in the plan's Sources, and that column is not guaranteed to hold the id of the solution that lists the component (it can point to another or the base solution, and rows that sit in several solutions have one value). If it does not hold in the unmanaged Dev, reads 4 to 6 return too few rows there, and live item 1 fails with `flow-missing`, `variable-missing` and `reference-missing` for a seed that is installed correctly. The plan's Step 1 records filters only for the layer table.
- Suggested fix: Say in Data which relation each read uses: a join through `solutioncomponent` (`objectid` equal to the row id, `solutionid` equal to the solution) is the one the plan already trusts. In Step 1, run reads 4 to 6 both ways in Dev (unmanaged) and in Test (managed), and record whether the counts agree. Add "the `solutionid` filter returns the seed's flows, variables and references in both environments" to the go-back Assumptions, with the join as the named fallback.
- Resolution: fixed (reads 4 to 6 link through solutioncomponent, stated in Data)

#### R4. Step 2 lists tests for every outcome of Step 1 as if all were built
- Area: 9. Testability
- Finding: Step 2 tests "layers read and not read; the 50-read cap; the component-name map" and Data read 7 describes three modes (batched per type, one component per query with a cap of 50, not readable). Step 1 will show which one exists; the other two have no capture, so their parser and cap tests can only run on hand-written text, and their code is speculative. The cap test needs a managed solution with more than 50 components, which no fixture has. AGENTS.md asks for the smallest change that solves the task. The offline done-when already allows a hand-written snapshot for the layer case, but the plan does not say that the unused modes are not built.
- Suggested fix: Add one sentence to Step 2: after Step 1, build only the layer mode Step 1 found (batched, per component with the cap, or not read) and its tests; the other modes and their README lines are dropped from the plan text before Step 2 starts. Name which Step 2 tests are conditional on that.
- Resolution: fixed (only the layer mode Step 1 found is built and tested)

#### R5. The scrub list in Step 1 does not cover what the captures will contain, and no check catches a miss
- Area: 2. Environment variables (tenant values), 9. Testability
- Finding: Step 1 scrubs "environment ids and URLs, user names and ids, connection ids" and keeps "the seed's own component ids". The captures also hold the solution row's own `solutionid` (different in Dev and Test, so tenant-specific), the real environment friendly names from read 1, any environment variable current value the user set (Step 1 asks for one with a space, a comma and a line break, which may be a real address or path) and the stdout and stderr of a failed or malformed query, which can echo the environment URL or the user's name. The component ids in a managed environment may be per-environment for metadata types, so "the seed's own" ids is not a safe blanket. There is no step that checks the result: no scan of `tests/fixtures/pac/drift/` for URLs, GUIDs outside an allowed list, emails, and no statement that the user reviews the scrubbed files before the `test:` commit (build-order task 22). Fixtures are committed to a public repo, so this is the one place the plan can leak tenant data.
- Suggested fix: Extend the scrub list (solution ids, friendly names, variable values, error text), say the seed variable's test value is a made-up one the user sets for the capture, and add a check in Step 1: a grep, or a small test, that fails on a URL, an email or a GUID in `tests/fixtures/pac/drift/` that is not on a named allow list of the seed's own ids. Say the user reads the scrubbed files before the commit.
- Resolution: fixed (scrub list extended, made-up variable value, grep against an allow list, user reads the files)

### Low

#### R6. The go-back Assumptions promise a named fallback, but two have none
- Area: 10. Unanswered areas
- Finding: Assumptions open with "The first four are go-back Assumptions: if Step 1 refutes one, the plan goes back to planning before Step 2, with the fallback named." Assumptions 2 and 3 name a fallback. Assumption 1 (`pac env fetch` output can be parsed) and Assumption 4 (`--environment` reads the named environment on both commands) do not. With the Dataverse SDK and Web API out of scope, nothing is left if either fails, which the plan should say.
- Suggested fix: Name the fallback for each, even if it is "M8 stops and the BUILD-PLAN row is revised": for 1, `--xmlFile` or another `pac` output option if one exists; for 4, `pac org select` before each environment's reads, which writes pac's current-environment setting and so needs a new decision.
- Resolution: fixed (fallbacks named: M8 stops and the BUILD-PLAN row is revised)

#### R7. A failed layer read at run time is exit 3 for the whole run, and the plan does not say so
- Area: 8. Security roles
- Finding: Q4 decides at build time, from the capture, that layers are "not read". At run time read 7 is a read like the others, so a role that lacks access to the layer table (the first admin question, and the application user M10 will use) gives a failed `pac` call and exit 3 for the whole run, with the other six reads wasted. The plan's text and the README step do not tell users that.
- Suggested fix: State in Automation step 2 that a failed read 7 is exit 3 like any other, and in the README which privilege the reads need, from the answer to the admin question. Do not add a runtime fallback (that would be new scope).
- Resolution: fixed (failed read 7 is exit 3, stated; README names the privilege)

#### R8. `schemaVersion` of a snapshot file is not said to be separate from the global output version
- Area: 9. Testability
- Finding: A snapshot with a schema version other than "the current" is exit 3. `JsonOutput.SchemaVersion` is one constant for every output, and `docs/schemas/README.md` bumps it when any output removes or renames a field. If the snapshot carries that constant, an unrelated change to another command's JSON invalidates every saved snapshot, including CI artifacts that M9 will read.
- Suggested fix: Say whether the snapshot has its own constant (for example `SnapshotModel.SchemaVersion` = 1) that moves only with the snapshot file, and say in `docs/schemas/README.md` that the snapshot file is a file format like the baseline, versioned on its own, even though `OutputSchemaTests` validates it.
- Resolution: fixed (snapshot file has its own schemaVersion constant)

#### R9. Friendly environment names appear in reports, file names and CI summaries
- Area: 2. Environment variables (tenant values)
- Finding: BUILD-PLAN Phase 3 says environment names and ids "live only in environment variables or gitignored files". `drift` prints each environment's friendly name in every report line, in the JSON and Markdown (which M9 turns into PR comments) and in `--save` file names. The plan treats the name as safe and the `--env` value as secret, and says only that the live results in the PR are written by hand. A Markdown report pasted into a public PR carries the names.
- Suggested fix: Say in the README that names are printed and saved, and that the report is for private places like the snapshot files. Optionally add one line to Out of scope that a position-only mode is not built.
- Resolution: fixed (README says names are printed and saved; position-only mode out of scope)

#### R10. The BUILD-PLAN update in Step 5 leaves out two reads and `pac org who`
- Area: 9. Testability
- Finding: BUILD-PLAN "What PowerOps reads" is a list of allowed reads: `solution`, `solutioncomponent`, `environmentvariablevalue`, `connectionreference`, `msdyn_componentlayer`, `pac solution list` and `list-cloud-flows`. The plan adds `workflow`, `environmentvariabledefinition` and `pac org who` per environment, and drops the other two. Only the flow and `solution list` changes are named in the Step 5 update, so the Phase 3 text would stay wrong about what is read, and the admin question is the only other place that lists the new tables.
- Suggested fix: Add the two tables and `org who` to the Step 5 BUILD-PLAN update, together with the row's other changes.
- Resolution: fixed (Step 5 update names the added and dropped reads)

#### R11. The reason given for not using `list-cloud-flows` does not match the existing fixtures
- Area: 9. Testability
- Finding: Data says `pac power-automate list-cloud-flows` "is Preview, cannot tell a cloud flow from another workflow, and gives no name". The existing samples in `tests/fixtures/pac` show its output has a `name` and a `modernFlowType` per flow, and the command lists every flow in an environment in one call when `--workflow-id` is left out. The conclusion (one `env fetch` for all flows, matched by id) still holds, but the stated reason is wrong, and a reviewer of M9 could rely on it. The state mapping also already exists in `FlowState.Read` for the `stateCode` labels; read 4 adds a second mapping from numbers or labels.
- Suggested fix: Replace the reason with the true one (one read gives name and state with the other `workflow` rows, no Preview command, and the same parser as the other reads), and say read 4 maps the same state names as `FlowState`.
- Resolution: fixed (reason replaced; read 4 maps the FlowState names)

#### R12. It is not said whether the per-environment checks still run when `solution-missing` fires
- Area: 9. Testability
- Finding: `solution-missing` says "nothing else is compared for this pair", while `unmanaged-layer` and `reference-unbound` are "checked in every environment". For an environment that has the solution while the first has not, it is unclear whether those two items are reported. A Step 3 test for either behavior cannot be written from the text.
- Suggested fix: One sentence: the checks of a single environment run for every environment where the solution is installed, independent of the pair comparison (or not).
- Resolution: fixed (single-environment checks run wherever the solution is installed)

#### R13. Live item 3 assumes the flow can be edited in Test and that the edit makes an unmanaged layer
- Area: 9. Testability
- Finding: Item 3 needs the user to edit the managed seed flow in the maker portal and a layer to appear. If the managed properties block the edit, or a saved edit leaves no unmanaged layer, the item fails although layers can be read. The plan records the item as "not supported" only when layers cannot be read, and Step 1's read "after editing the flow" has no stated outcome for that case.
- Suggested fix: Add to Step 1 and to item 3: if the flow cannot be edited in Test, or the edit leaves no unmanaged layer on it, record that and the item as not supported in the PR; say whether a changed flow state after the edit is expected to add a `flow-state` item.
- Resolution: fixed (Step 1 records editability and the layer; item 3 not supported otherwise; flow stays on)

#### R14. "Hash in no output" conflicts with the snapshot file, and the recorded Q2 answer still says "safe"
- Area: 9. Testability
- Finding: Step 3 tests that "no value or hash appears in any output" while the snapshot file written by `--save` holds `valueHash`. The test must mean reports only. Separately, `decisions.md` Q2 records the user's answer as "snapshot files are safe to keep as CI artifacts", while the Constraints say the opposite and note the narrowing in a parenthesis; the approval log says no user decision was needed. That is a change to a recorded decision, kept in a place a later reader of `decisions.md` will not look.
- Suggested fix: Word the Step 3 test as "no value or hash in the text, JSON or Markdown report"; add one line to the `decisions.md` log that the Q2 wording was narrowed to "hashes, kept in private folders" and why.
- Resolution: fixed (test wording limited to reports; log line added for the Q2 wording)
