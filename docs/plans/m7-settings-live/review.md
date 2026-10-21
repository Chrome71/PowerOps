plan-hash: 6b966fc580ee5e0ce4f9b2677939612b02eb42e2891802cb84b238b91bac2012
model: claude-sonnet-5-5
fresh-context: yes

The plan-hash was updated after the text fixes for R1 to R8 below (the same practice as the offline M7 review). They changed only plan.md's Automation (import arguments, forwarded `pac` output, the runner seam), live item 5, one Assumption and Steps 1, 3, 4 and 6; nothing else in the body changed. Reviewed hash: 06265042198946da8727761ac4231367f102a252b9c1f6641265096e4b3dc8e7.

Scope: `plan.md` and `decisions.md` of `m7-settings-live`, the offline half `docs/plans/m7-settings/plan.md`, `AGENTS.md`, the M7 row and line 61 of `docs/BUILD-PLAN.md`, and the code the plan names (`SettingsCommand.cs`, `PacRunner.cs`, `PlanFacts.cs`, `PlanCommand.cs`, `Program.cs`, `PowerOps.Cli.csproj`, `PlanFactsTests.cs`, `SettingsCommandTests.cs`, `OutputSchemaTests.cs`, `tests/Shared/SolutionZip.cs`, `Model/Solution.cs`, `SolutionParser.cs`, `Settings/DeploymentSettings.cs`, `Output/SettingsReport.cs`, `skills/powerops-implement/SKILL.md`, the seed fixture). `powerops plan status` reports `approved`, `openQuestions` 3 (the three admin questions; the `Open questions` section is empty), `review: pending`, no violations. `powerops plan facts` was NOT run and `pac` was never called: contacting the tenant is forbidden for this review, so there are no environment facts and the overlap check of item 1 was not done. One Microsoft Learn page was re-read (`pac power-automate`): it confirms `--workflow-id` and `--environment` on `list-cloud-flows`, `--json` only in the `--columns` text, and that `--environment` takes a Guid or an https URL.

Checklist items that do not apply: 1 (no solution or publisher is created; the existing seed `pops` is reused), 2 (connection ids and variable values live in a settings file kept outside the repo), 7 (no app; one `list-cloud-flows` call per solution flow, see R6), 5 and 6 (the tool imports whatever zip it is given; the user's choice, and `managed` is shown). Items 3 and 4 (licence, DLP) follow the user's Q6 choice and the admin questions, not raised again. Tenant data: no URL with a scheme, GUID, email or person's name in `plan.md` or `decisions.md`.

Earlier review (12 findings, 0 High, 5 Medium, 7 Low, written for plan hash d5216cad), re-checked against the current plan. All twelve are really resolved in the plan text: the seam is named (Step 4; but see R1 for what it still lacks), the import is synchronous with 60 minutes as powerops' own limit and the pac defaults listed in Automation and Step 6, item 5 has two accepted outcomes and starts from item 4's state (but see R5), the agent rule is added to AGENTS.md and the `powerops-implement` skill in Step 6, dry runs after each removal compare `new` with the result, Sources and the Assumption say `--json` is only in the `--columns` text and the local help, `Stderr = ""` is a trailing default, usage errors come first and `--target` without `--apply` is exit 2, JSON mode writes the target line to stderr, the zero-flow case and "`new` also means not visible" are in, test zips come from `SolutionZip.Create`, and Step 7 covers BUILD-PLAN line 61 and the README. Nothing from that review is still open. The review file was stale for the current hash because the plan was edited after it; this review replaces it.

## Findings

### High

No findings.

### Medium

#### R1. The runner seam is not buildable as written: no `InternalsVisibleTo`, and the timeout argument breaks the existing runner type
- Area: 9. Testability
- Finding: Step 4 adds an `internal` `Program.Run` overload "visible to the test project". `PowerOps.Cli.csproj` has no `InternalsVisibleTo` (only `PowerOps.Core` has one, for `PowerOps.Core.Tests`), and the plan does not add one. Separately, Step 3 says "the runner function takes a timeout" while `PlanFacts.Run` takes `Func<IReadOnlyList<string>, PacResult>`, `PlanCommand` passes `new PacRunner().Run` as a method group, and `PlanFactsTests` fakes are one-argument lambdas. If `PacRunner.Run` gains a second parameter, the method group no longer converts and the fakes no longer compile, so "`PlanFacts` and its tests keep passing unchanged" does not hold. The plan does not say which type `SettingsCommand.Create(output, error, pac)` takes.
- Suggested fix: In Step 3 or 4, say that `PlanFacts.Run` keeps its one-argument type and `PlanCommand` adapts (for example `args => runner.Run(args, 30 s)`), that `SettingsCommand` takes a new delegate (`args, timeout`), and that Step 4 adds `<InternalsVisibleTo Include="PowerOps.Cli.Tests" />` to `PowerOps.Cli.csproj` (or makes the overload public).
- Resolution: fixed (`PacRunner.Run` and `PlanFacts` keep their types; `apply` takes a `(args, timeout)` function built in `Program`; a public `Program.Run` overload, no `InternalsVisibleTo`)

#### R2. The report shows the import argument list, but the environment id must never be printed
- Area: 9. Testability, agent safety
- Finding: Data says the environment id "is held in memory and passed to `pac` as `--environment`, never printed or stored". Automation step 3 and the JSON contract say the report shows "the exact argument list of the import", and step 4 defines that list as `pac solution import --environment <id> ...`. Both cannot hold. A builder will either print the id (tenant data in agent logs and PR text, against the plan's own rule) or print something that is not exact. No test or golden is named that would catch it.
- Suggested fix: Say the argument list is printed with a placeholder (`--environment <environment id>`) and that the zip and settings paths are printed as typed. Add a CLI test that runs with a fake `org who` holding a known id and asserts the id is absent from stdout, stderr (powerops' own lines) and the JSON report.
- Resolution: fixed (the import arguments are printed with the `<environment id>` placeholder; a CLI test asserts the id appears nowhere)

#### R3. A failed import's text is probably on `pac`'s stdout, but the plan keeps only stderr for it
- Area: 9. Testability
- Finding: Q9 and Automation step 4 say "`pac`'s own output goes unchanged to powerops' stderr", and Step 3 adds only `Stderr` to `PacResult`. `PacRunner` already captures stdout, and `pac` prints its progress and most error messages to stdout (the `PlanFactsTests` samples are stdout). A builder following Step 3 forwards the new `Stderr` and drops the text the user needs to see why the import failed. On a timeout `PacRunner` returns empty output for both, so the "may still be running" case has nothing to forward either; the plan does not say that is fine.
- Suggested fix: State that both `Stdout` and `Stderr` of the failed import are written to powerops' stderr (stdout first, unchanged), that a timeout forwards nothing, and add the stdout case to the import-failure CLI test and to the Step 1 capture notes (which stream carries an error).
- Resolution: fixed (stdout then stderr of a failed import go to powerops' stderr; a timeout forwards nothing; Step 1 records which stream carries errors)

### Low

#### R4. A flow that was off at export makes every run exit 1
- Area: 9. Testability
- Finding: Exit 0 needs every flow `on` (the user's Q3). A solution that deliberately ships a flow off (a manual or retired flow) can therefore never pass in CI, although the dry run predicted `off` and the import did what it should. The report holds `expected` per flow, so a consumer can tell, but the README step does not say that exit 1 includes this case.
- Suggested fix: Say in the README step that a flow off at export gives exit 1 with `expected: off`, and that pipelines should read `flows[].expected` if they accept that. No change to the Q3 rule.
- Resolution: fixed (README step: a flow off at export always exits 1, by design)

#### R5. Live item 5's "import succeeds, flow stays off" outcome tells nothing about the fake connection id
- Area: 9. Testability
- Finding: Item 5 starts from the seed installed by item 4 with the flow off. An import over an existing flow keeps its state (the plan's own premise), so the flow stays off with a real or a fake connection id, and exit 1 is then not evidence about how `pac` handles an unknown id. The item still records which outcome was seen, so it is not wrong, but only the exit-3 outcome is informative.
- Suggested fix: Say in item 5 that exit 1 only means the import was accepted and proves nothing about the id; or start item 5 from a removed seed with the flow-on zip, where a fake id that is accepted shows as `off` after the import.
- Resolution: fixed (item 5 says what its second outcome does and does not show)

#### R6. The flow reads cost two `pac` calls per flow, sequentially
- Area: 7. Data volume
- Finding: Dry run and `--apply` call `list-cloud-flows` once per cloud flow, before and after the import, each a new `pac` process with a 30-second limit. A solution with dozens of flows takes minutes, and one slow call stops the run at step 3 or 5 (exit 3). Q4 chose per-id matching, so the shape is fine; the cost and the failure mode are not stated.
- Suggested fix: Add one sentence to the README step (about 2N `pac` calls for N flows, and a timeout on one read is exit 3). Whether to batch is a later change.
- Resolution: fixed (README step: two reads per flow, 30-second read limit)

#### R7. Step 1 is a live user step that gates the whole build, and the fallback for a missing `Environment ID:` line is not stated
- Area: 9. Testability
- Finding: Steps 2 to 7 need the captured samples, so an agent cannot start the build until the user has run `pac` against the developer environment. The Assumptions say the plan goes back to Q4 if `--json` or the id field is missing, but not what happens if `pac org who` prints no `Environment ID:` line on the real output (the existing fake has one, the plan only guesses the real format). The `--environment` pinning of Q7 depends on it. The 2026-12-15 deadline makes a late discovery costly.
- Suggested fix: Say Step 1 is the first thing to do and may need the user's yes before the build session starts; add `Environment ID:` to the list of things that send the plan back (with the `--json` flag and the state field).
- Resolution: fixed (a missing `Environment ID:` line sends the plan back to Q7 before Step 2)

#### R8. Environment friendly names are not unique, so `--target` is a weak confirmation
- Area: 8. Security roles, agent safety
- Finding: `--target` is compared with the friendly name from `pac org who`. Two environments (a Dev and its copy, or Test and a restored Test) can share a name, and a profile switch between the dry run and `--apply` to a same-named environment passes the check. The id pinned for the run protects the run itself, not the user's approval. Q7 chose the name; the residual risk is not stated.
- Suggested fix: Add one sentence to the README step: names are not unique, so check the pac profile before saying yes. No code change.
- Resolution: fixed (README step: names are not unique; check `pac auth list` before saying yes)
