plan-hash: d4f915ac8bd163d5de4c4fe17828044ba53014b1f1723f0467e3b74f49ac245f
model: claude-sonnet-5-5
fresh-context: yes

Environment facts were not read: the plan names no environment (`environment: null`) and changes this repo's own code and docs, and the user did not confirm a `pac` sign-in. Checklist items 1 to 9 and 11 (solution boundaries, connection references, licences, DLP, route, managed state, delegation, security roles, tables and flows) do not apply and are not reported. Item 10 (open questions: 0) and the URL/GUID/email/name scan do apply; the plan and `decisions.md` hold none. `plan.md` was not edited (`review: pending` stays), because this request allowed no change outside `review.md`. Checked against the code at `15b8d9d` by reading; no tests were run.

## Findings

### High

#### R1. A skip-only plan never has flows, so two of the three end paths and three of the five tests of row 3 cannot exist
- Area: Data row 3, Done when 2, testability
- Finding: Row 3 says "`FlowIds` lists the flows of skipped solutions too (`DeployApply.cs:149-162`), so a skip-only plan with flows reaches the flow check". `FlowIds` does read every `plan.Solutions` entry with `Flows.Count > 0`, but `DeployPlan.Build` fills a solution's `flows` list only inside `if (e.Action != PlanAction.Skip)` (`src/PowerOps.Core/Deploy/DeployPlan.cs:162-183`), so a `skip` row always has an empty `Flows`. With every solution skipped, `flowRefs.Count == 0` and the run always ends at the first return (`DeployCommand.cs:430`). The flow-check return (`:451`) and the failed-second-read return (`:447`) are unreachable for a skip-only plan. So Done when 2 ("on each of its three end paths") is wrong, and the tests "once with a flow in its expected state, once with a flow off (exit 1, the text keeps the flow line), once with a second read that fails (exit 3)" cannot be written. The same goes for the `DeployApplyReport.cs:66-69` change (a message with a flow off never comes together for a skip-only run) and for the RECOVERY rows for exit `1` and `3`, which have no skip-only case.
- Suggested fix: reduce row 3 to the one return at `:430` (message when `imports` is empty), drop the `:447` and `:451` changes, the `DeployApplyReport` change, and the flow and second-read tests, and name only the exit `0` row of RECOVERY. Say in the Log that skipped solutions carry no flows, as the code shows.
- Resolution: Answered. Row 3 now changes only the `:430` return, says a skipped solution carries no planned flow (`DeployPlan.cs:162-183`, checked), and Done when 2 and the tests drop the flow and second-read cases; the one test extends `:866`.

### Medium

#### R2. The apply tests are not in `DeployCommandTests`
- Area: Data rows 1 and 3, Done when 4, testability
- Finding: Rows 1 and 3 and Done when 4 put the new apply tests in `DeployCommandTests`. That class (`tests/PowerOps.Cli.Tests/DeployCommandTests.cs`) tests `deploy plan` and holds no `deploy apply` test. The apply tests, with the `Apply(...)` helper, `MakePlan`, `FakePac` and `FakeTarget.Fail` that row 1's failing read needs, are in `tests/PowerOps.Cli.Tests/DeployApplyCommandTests.cs`, which is in the "deploy apply" collection. The existing neighbours to extend are `A_failed_re_plan_exits_as_deploy_plan_does_and_imports_nothing` (`:311`, which asserts the exact `Err` text and has `--target` set to the environment URL, so it must keep printing no hint) and `A_plan_of_only_skips_imports_nothing_reads_nothing_again_and_exits_0` (`:866`, which checks `status` `imported`, and not the message). Only the "`deploy plan` with a failing read prints no hint" test belongs in `DeployCommandTests`.
- Suggested fix: name `DeployApplyCommandTests` for the apply tests of rows 1 and 3 and `DeployCommandTests` for the `deploy plan` one; extend `:866` with the message and the text report.
- Resolution: Answered. Rows 1 and 3 and Done when 4 name `DeployApplyCommandTests` (with `:311` kept as is and `:866` extended) and `DeployCommandTests` for `deploy plan` only; both files are as the plan says.

#### R3. The hint also reaches `deploy gate`, which the plan does not mention
- Area: Data row 1, exit codes
- Finding: `Prepare` is called from `DryRun` (`DeployCommand.cs:268`), and `DryRun` is shared with `deploy gate` (`GateCheckCall`, `:636`). Row 1 passes the plan's target on that call, so a gate whose `call.Environment` equals the plan's target (ignoring case) also prints the hint, into the gate's report writer, which the hook shows. The gate exits only `0` or `2` (a hook blocks on `2` only), and the hint text says "`--target` takes ...", while the gate has no `--target`. `DeployGateCommandTests.cs:380` asserts the exact stderr lines of a failing read, so a gate test with the plan's target would now differ. Row 1 and Done when 1 say "the CLI and the MCP tool" only.
- Suggested fix: decide it: either pass null for the gate (the hint is not printed there) and add a gate case with `call.Environment` equal to the plan's target to show the lines are unchanged, or keep it and say so in the plan, with the gate test. The simplest is to add the parameter to `DryRun` and let only `Apply` pass the plan's target.
- Resolution: Answered. `DryRun` and `Prepare` take the plan's target, only `Apply` (`:378`) passes it, `deploy plan` (`:140`) and the gate (`:636`) pass null, and Done when 4 keeps the gate tests unchanged.

### Low

#### R4. The `status` description and the other docs that say what `imported` means are not on the list
- Area: Data row 3, docs
- Finding: Row 3 changes the schema's `message` description, but `status` in `docs/schemas/deploy-apply.schema.json:16` still says "imported: the imports ran". The `deploy apply` section of `docs/COMMANDS.md` ("`0` applied") and `docs/RECOVERY.md`'s exit `0` row say the same. A reader of those lines can still take `imported` to mean that something changed.
- Suggested fix: add the skip-only sentence to the `status` description and to the `deploy apply` section of `docs/COMMANDS.md`, next to the RECOVERY row.
- Resolution: Answered. Row 3 lists the `status` and `message` descriptions, `COMMANDS.md:117` and RECOVERY's exit-`0` row.

#### R5. Other places say `--target` without saying that it is an id or URL
- Area: Data row 2, docs
- Finding: Row 2 changes the two option descriptions and `COMMANDS.md:117`. `docs/HANDOFF.md` step 8 (`deploy apply ... --target <test>`), the GitHub Action docs (`target`) and the `powerops-implement` text use a placeholder or the word "target" with no hint that the plan file's `target` is not the value. An agent reading those is the case the follow-up was about.
- Suggested fix: add one clause to HANDOFF step 8 ("an id or URL, not the plan file's `target`"), or note in the plan that only the tool and option descriptions are changed.
- Resolution: Answered. Row 2 adds the clause to HANDOFF step 8 (`docs/HANDOFF.md:49`, checked).

#### R6. "Test: DocsDriftTests" and "the skill-copy golden test" test no sentence
- Area: Data rows 4 to 7, testability
- Finding: `DocsDriftTests` checks commands and options against the docs, and the golden recipe only copies `skills/` into `plugins/*/skills/`. Neither asserts the new sentences, so rows 4 to 7 have no check that they stay. That is fine for prose (AGENTS.md asks for tests of new logic), but the table says they are tested.
- Suggested fix: write "none (prose); the copies are checked by the golden recipe" in the Test column, or add one `Assert.Contains` for row 6 next to the existing `powerops-implement` wording tests in `SkillsCommandTests`.
- Resolution: Answered. Rows 4 to 7 say "none (prose)"; row 6 names the recipe that regenerates the copies.

#### R7. Row 2 line numbers
- Area: Data row 2
- Finding: The `--target` description of `deploy apply` is at `DeployCommand.cs:57`, not `:56` (checked against `15b8d9d`). The golden lines for the MCP text are `tests/golden/mcp-tools.txt:293-296`.
- Suggested fix: correct the two numbers; the golden is rewritten by the recipe anyway.
- Resolution: Withdrawn. In `15b8d9d` the `--target` description is at `DeployCommand.cs:56` and the MCP text at `mcp-tools.txt:295` (checked with `git show`); my numbers came from a different reading and the plan is right.

## Second pass

R1 to R7 hold in the revised plan and the code. New in the revision: the `DryRun` parameter (null from `deploy plan` and the gate, the plan's target only from `Apply`), HANDOFF step 8 and the one-return row 3 were checked against the code and raise no new finding.

## Checked and found right

Row 1's catch (`DeployCommand.cs:792-798`) and its exit codes (`ex.ExitCode`, 3 or 2); `deploy plan` (`Run`, `:140`) passes no reviewed target; no new `pac` text parse and no new write (AGENTS.md rules hold); the comparison with `reviewed.Target` at `:274` is ordinal, so the hint's `OrdinalIgnoreCase` is a deliberate difference. Row 3's first return (`:430`) gives `null` today, and a mixed plan keeps its null message. Row 4: the three plugin copies (`powerops-review-plan`, `-verify`, `-review-security`) carry `context: fork` and `model: opus` in `plugins/claude/skills`, Codex's copies carry none, and the kept roles' sources are not touched. Row 6: `powerops-implement` is not a kept role, and `SkillsCommandTests` pin other sentences of it, not the one that changes.
