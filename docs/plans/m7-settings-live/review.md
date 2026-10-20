plan-hash: d5216cad508f539c97ded9a66348d52bd8e54e2da4efc61990becd91fa94c2cb
model: claude-sonnet-5-5
fresh-context: yes

Scope: `plan.md` and `decisions.md` of `m7-settings-live`, the offline half `docs/plans/m7-settings/plan.md`, `AGENTS.md`, the M7 row and line 61 of `docs/BUILD-PLAN.md`, and the code the plan names (`SettingsCommand.cs`, `Program.cs`, `PlanCommand.cs`, `PacRunner.cs`, `PlanFacts.cs`, `tests/PowerOps.Cli.Tests/PlanFactsTests.cs` and `SettingsCommandTests.cs`, `Solution.cs`, `SolutionParser.cs`, `Settings/DeploymentSettings.cs`, `Output/SettingsReport.cs`, `OutputSchemaTests.cs`, `tests/Shared/SolutionZip.cs`, the seed fixture). `powerops plan status` reports `approved`, `openQuestions` 3 (the three admin questions; the `Open questions` section is empty), `review: pending`, no violations. `powerops plan facts` was NOT run and `pac` was never called: contacting the tenant is forbidden for this review, so there are no environment facts and the overlap check of item 1 was not done. Two Microsoft Learn pages were read in full (`pac solution`, `pac power-automate`) because R2 and R6 depend on them.

Checklist items that do not apply: 1 (no solution or publisher is created; the existing seed `pops` is reused), 2 (connection ids and variable values live in a settings file kept outside the repo; nothing is hard-coded), 7 (no app, no row counts; the flow list is one call per solution flow). Items 3 and 4 (licence, DLP) follow the user's Q6 choice and the admin questions; not raised again. Tenant data: no URL with a scheme, GUID, email or person's name in `plan.md` or `decisions.md` (the Learn paths are public, relative).

Earlier review (11 findings, all marked fixed or accepted) re-checked against the current plan. R1 (seed already installed) is resolved by Q8: the live run removes the seed and its components first and item 3 covers the re-import; one residue remains, see R5. R2 (nothing ties `--apply` to the approved environment) is resolved by `--target` plus the environment id on every later call; the agent rule exists only in the README, see R4. R3 (predictions for updates, flow cycling) is resolved: the dry run reads each flow and reports `exists` or `new`. R4 (pac text in the report) is resolved by Q9. R5 (one runner for 30 s reads and a 60-minute import) is resolved in design, but the 60-minute figure is justified by an option the plan does not pass, see R2. R6 (Preview command, `--workflow-id`, capture) is resolved except that the plan cites `--json` as documented, see R6 below. R7 (`managed`) is resolved; the import defaults the earlier finding asked to list in the README are still not in Step 6, see R2. R8 (dry run changes nothing) is resolved with `pac solution list` before and after. R9 (paths printed as typed), R10 (owner and role) and R11 (public Learn paths) are resolved or accepted correctly.

## Findings

### High

No findings.

### Medium

#### R1. CLI tests cannot inject a fake `pac` through `Program.Run`
- Area: 9. Testability
- Finding: Constraints say the command takes the `pac` runner as a function "the way `PlanFacts.Run` does", and Step 4 lists many CLI tests with a fake `pac`. But `PlanFacts.Run` gets its runner from `PlanCommand` (`new PacRunner().Run`, inline), and the only tests that fake `pac` call `PlanFacts.Run` directly. `Program.Run(args, out, err)` and `SettingsCommand.Create(output, error)` have no runner parameter, and AGENTS.md's "Add a command" recipe tests commands through `Program.Run`. As written, Step 4 cannot be built: a test that goes through the parser (`--apply` without `--target`, exit 2) has no way to supply a fake runner.
- Suggested fix: Name the seam in Step 4: for example `SettingsCommand.Create(output, error, pac)` with `Program` passing the real runner and an optional `Program.Run` overload or an internal factory the tests use. Say which tests call the command directly and which go through `Program.Run`.
- Resolution: fixed (Step 4 names the seam: `SettingsCommand.Create(output, error, pac)` and an internal `Program.Run` overload)

#### R2. The 60-minute import runs without the option that makes it wait, and the import defaults are unstated
- Area: 9. Testability, 6. Managed or unmanaged
- Finding: Automation step 4 gives the import a 60-minute timeout "matching `pac`'s own default wait", with "no other options". Learn lists that default (60 minutes) on `--max-async-wait-time`, which belongs to `--async`; without `--async` the import is a single synchronous call, and Learn does not say how long it may take. So the timeout may not match what `pac` does, and a long import may fail in a way the plan does not model. Learn also lists `--force-overwrite` (unmanaged customisations) and `--publish-changes`, whose absence changes what an unmanaged import over an existing solution does; the earlier review asked for these defaults to be named in the README, and Step 6 still lists nothing about them.
- Suggested fix: Decide in the plan whether the import passes `--async` (and `--max-async-wait-time`) or stays synchronous, and say why; if no option is added, say the 60 minutes is powerops' own limit, not `pac`'s. Add the import defaults (no force-overwrite, changes not published, no upgrade) to the Step 6 README list. A live item should observe a real import through the chosen route.
- Resolution: fixed (synchronous import, 60 minutes is powerops' own limit; the pac defaults listed in Automation and the Step 6 README list)

#### R3. Live done-when item 5 may not produce the failure it expects
- Area: 9. Testability
- Finding: Item 5 expects a fake `ConnectionId` to make the import fail with exit 3. Learn says nothing about how an unknown connection id is handled at import; it may import and leave the flow off (powerops then exits 1, not 3). Then the exit-3 path with `pac`'s text on stderr and none in the report is never seen live, and item 5 would count as a failed check of the plan rather than a result. Item 5 also does not say what state it starts from (item 4 leaves the seed installed with its flow off), and a successful import would overwrite that state.
- Suggested fix: Write item 5 as an observation with two accepted outcomes (exit 3 with the report holding `pac`'s exit code and no text, or exit 1 with the flow `off`), say it starts from the seed installed after item 4, and state that the exit-3 path is covered offline by the fake `pac` when the live import does not fail. Or name another reliable way to make a real import fail.
- Resolution: fixed (item 5 starts from item 4's state and accepts exit 3 or exit 1; offline tests cover exit 3 otherwise)

#### R4. The agent rule for `--apply` lives only in the README
- Area: 8. Security roles, agent safety
- Finding: The plan's Users include agents, and the safety of `--apply` rests on "dry run, show the target line, `--apply --target <name>` only after a yes". Nothing enforces it: `--target` is the name printed by the dry run, so an agent can copy it with no yes. Step 6 puts the rule in the README only. The repo's agent files do not carry it: AGENTS.md has the read-only rule but not this one, and the installed skills say "never run `pac` yourself" without mentioning a `powerops` command that runs `pac solution import`. An agent working in this repo (or using the skills) reads those, not the README paragraph.
- Suggested fix: Add a Step (or extend Step 6) that adds the rule to AGENTS.md under Rules ("never run `settings apply --apply` unless the user said yes to that environment this session") and to the skills that mention `pac`. Optionally record in `decisions.md` that no technical gate (a prompt, a token) is added, so the choice is explicit.
- Resolution: fixed (Step 6 adds the rule to AGENTS.md and the powerops-implement skill; no gate beyond `--target`, per Q7)

#### R5. The `new` prediction in the dry run is never checked live
- Area: 9. Testability
- Finding: Item 1 runs before the seed is removed, so every flow is `exists` there. The `new` prediction ("expected on if on at export") is the claim that matters for the "flows come back on" done-when, but the live items first see `new` only in the `--apply` report of items 2 and 4, after the import has happened. Nothing compares the prediction made before the import with the result after it.
- Suggested fix: Add a dry run after the seed is removed and before items 2 and 4, and have the user note that the predicted state equals the reported state after the import.
- Resolution: fixed (dry runs after each removal predict `new`; items 2 and 4 compare the prediction with the result)

### Low

#### R6. The plan cites `--json` for `list-cloud-flows` as documented
- Area: 9. Testability
- Finding: Sources say the Learn reference lists `--workflow-id`, `--environment` and `--json` for `pac power-automate list-cloud-flows`. The Learn page lists no `--json` parameter; it appears only in the `--columns` description ("Use --json for the complete response"). Assumptions cover the field names, not whether the flag exists, and Steps 2 to 6 depend on JSON output. Step 1 would find out, but the gate wording ("the plan goes back to Q4") is only attached to the fields.
- Suggested fix: Say in Sources that `--json` appears in the `--columns` text only and that `pac` 2.13.1's local help is the evidence, and make the Assumption cover the flag too.
- Resolution: fixed (Sources and the Assumption now say `--json` is only in the `--columns` text and the local help)

#### R7. Adding a field to the `PacResult` record changes every constructor call
- Area: 9. Testability
- Finding: `PacResult` is a positional record used in `new PacResult(true, false, 0, "...")` throughout `PlanFactsTests`. Step 3 says "gains stderr" and that `PlanFacts` and its tests keep passing unchanged; that holds only if the new member is a trailing parameter with a default (`string Stderr = ""`) or a settable property.
- Suggested fix: State the form in Step 3.
- Resolution: fixed (trailing `string Stderr = ""` named in Step 3)

#### R8. Usage-error order and `--target` without `--apply` are not defined
- Area: 9. Testability
- Finding: Automation puts the missing or wrong `--target` check in step 2, after validate. With validate problems and no `--target` on `--apply`, the exit is 1 or 2 depending on order, and no test names it. `--target` given without `--apply` (a dry run) is also unspecified: ignored, checked, or an error.
- Suggested fix: Say that option usage errors (`--apply` without `--target`, a folder instead of a zip, a missing file) come first with exit 2, and that `--target` without `--apply` is either checked against `pac org who` or rejected; add one CLI test each.
- Resolution: fixed (usage errors first with exit 2, `--target` without `--apply` is exit 2, tests added)

#### R9. JSON mode has no "first line" and no output until the end
- Area: 9. Testability, agent safety
- Finding: Screens say that with `--apply` the first line is the target name, printed before the import starts. With `--format json` the report is one document written at the end (as `SettingsReport` does), so for the agents the Users section describes, nothing shows the target during a run of up to an hour, and a killed process leaves no output at all.
- Suggested fix: Say what JSON mode does before the import (nothing, or the name on stderr) and keep the first-line rule for text mode only.
- Resolution: fixed (text mode first line; JSON mode writes the target line to stderr before the import)

#### R10. Edge cases of the flow list: zero flows, and a flow the user cannot see
- Area: 8. Security roles, 9. Testability
- Finding: A solution with no cloud flows has an empty `flows` list, so "every flow on" holds and the run exits 0; the plan does not say so or test it. Separately, a flow the importing user cannot see reads as absent, so the dry run predicts `new` (and "comes back on") for a flow that exists and is off; the plan and README mention `missing` after the import but not this false `new` before it.
- Suggested fix: Add a zero-flow case to Step 4 and say in the README that `new` also means "not visible to this account".
- Resolution: fixed (zero flows exits 0 with a test; `new` also means not visible, in Automation and the README step)

#### R11. The test fixtures for `apply` are not named
- Area: 9. Testability
- Finding: `apply` takes a zip only, but the repo holds the seed as an unpacked folder (`tests/fixtures/powerops-seed/v1`) and no seed zip. Step 5's goldens need a flow-on and a flow-off export (the seed's `StateCode` is 1); `SolutionZip.Create` zips a folder, so the flow-off case needs an edited copy. The plan does not say how the tests get either, or that no new binary is committed.
- Suggested fix: Add to Step 4 or 5 that tests build the zip from `powerops-seed/v1` in a temp folder with `SolutionZip.Create`, and the flow-off zip from a copy with `StateCode` changed to 0.
- Resolution: fixed (test zips built with `SolutionZip.Create`, flow-off from a temp copy, none committed)

#### R12. Other docs still say writes through `pac` do not exist
- Area: 5. Route
- Finding: `docs/BUILD-PLAN.md` line 61 says the CLI calls `pac` only read-only and "writes through `pac` wait until M7"; Step 7 updates only the M7 row. The README status line and the `plan facts` wording ("read-only") sit near the new paragraph.
- Suggested fix: Extend Step 7 to update line 61 and any README sentence that says the CLI never writes through `pac`.
- Resolution: fixed (Step 7 updates the BUILD-PLAN line and README wording)
