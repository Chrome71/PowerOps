plan-hash: e3907ba342584813b13a74697c589bcd57c7690cadc209fb1b7ae39715cbb069
model: claude-sonnet-5-5
fresh-context: yes

Environment facts were not read: the plan's `environment:` is null, no sign-in was confirmed, and no tenant call of any kind was made. The tools the steps name were checked with `command -v`: `dotnet` and `powerops` are on PATH; `pac` and the leak scan are not used or not callable (see R2). The plan changes PowerOps itself, so the checklist items on solutions, connection references, licences, DLP, route, managed state, volume, roles, size and screens are not applicable where the plan says None. The code claims were checked against the source at `e664e7f`: `DeployCommand.Apply` (the `flowRefs.Count == 0` return, the second read and its catch), `DeployApply.cs` (`FlowsAfter`, `ZipOf`), `DeployPlan.EqualVersions` (internal, same assembly as `DeployApply`; it needs four numeric parts on both sides, and `Prepare` already refuses a zip whose version is not), `DeployPlan.Build` (a skipped solution carries no flows, so "imports.Count == 0 keeps today's return" holds), `DriftLiveReader.ReadTarget` (a not-installed snapshot has `Installed: false`) and `DeployFakes.cs` (`After`, `FailAfter`, `Calls`). Completeness against the M98 section of the upgrade plan: every change, file and test it lists is in the plan; the plan adds "at least one import ran", which is consistent with it. Second pass (revised plan, body hash above): each resolution was checked against the revised plan and the code, no new finding came from the revision, and `review: done` was set in `plan.md`.

## Findings

### High

No findings.

### Medium

#### R1. The unproven platform capability is not in Assumptions in the required form, and no step probes it
- Area: 13 (Buildability)
- Finding: The change rests on a capability no source shows: a completed staged upgrade, and a completed plain import (managed or unmanaged), leaves the solution at the zip's version. `decisions.md` (Log, claims 1 and 2) marks it unproven and names trial 4's R-readback line as the first chance, but `plan.md` Assumptions only says it is "not documented" and has no `unproven: <capability>; probed by step <n>` line, and none of the five Steps probes it. If the capability fails in some case, a good import makes `deploy apply` exit 1 after an irreversible import, and the Action fails the job.
- Suggested fix: Add to Assumptions `unproven: a completed staged upgrade or plain import leaves the solution at the zip's version; probed by step <n>`, and add that probe as a step (or name the trial-4 observation as the step that closes it, with who reads it and what is recorded). Keep the "false exit 1, never a false exit 0" consequence beside it.
- Resolution: fixed: Assumptions holds the `unproven:` line, probed by step 6 (trial 4's R-readback), with why no build step depends on it and its failure direction. Second pass: confirmed. The plan's Assumptions now holds the `unproven:` line in the required form, probed by step 6 (trial 4's R-readback), with the reason no build step depends on it (every test uses fakes) and the failure direction (a false exit 1, never a false exit 0); the probe is a tenant observation, which this offline milestone cannot make, so it follows the milestone and is outside its Done when.

#### R2. The leak scan is a step the worktree cannot run, and Assumptions does not say where it is
- Area: 12 (Tools)
- Finding: Done when 10 and step 5 require "the leak scan". It is `reports/leakscan.sh`, an untracked local tool of the main checkout; `reports/` does not exist in this worktree, so the step is not callable from where the plan is built. Assumptions names no tool. The sibling plan M89 states this in its Assumptions.
- Suggested fix: Add an Assumptions line that the leak scan is `reports/leakscan.sh` in the main checkout, run from there against the committed branch.
- Resolution: fixed: Assumptions names `reports/leakscan.sh` in the main checkout and how it runs. Second pass: confirmed. Assumptions names `reports/leakscan.sh` in the main checkout and the command line; it also names the worktree build for `plan status`, and `dotnet run --project src/PowerOps.Cli -- plan status` gave the same body hash as the installed `powerops`.

#### R3. The test plan understates how many existing tests flip to exit 1, and misses the MCP fakes
- Area: 9 (Testability)
- Finding: Row 5 says existing apply tests get an `After` "at the zip's version" and edits `DeployFakes.cs` "only if a fake needs a setter". Checked in the code: (a) `SeedAfter` loads `seed-test.json`, which is version `1.0.0.0`, while `SeedZip` makes `1.0.0.1`, so every `SeedAfter("on")` test, the `deploy-apply*` golden tests and the agent tests would show a mismatch until `SeedAfter` itself is changed; (b) most of the 33 `SmallZip` and `Chain()` plans are flowless, have no `After` today, and now make a second read that shows nothing installed; (c) `McpCommandTests.cs` builds its second-read fakes as lambdas at three places that return the pre-import snapshot or a not-installed one for every solution, with no `After`, so its 12 `--apply` calls flip too. Row 5 names only `DeployFakes.cs` for the fakes and says nothing of these three. Hand-writing an `After` per test invites exactly the looser assertion Done when 9 forbids.
- Suggested fix: Name the shared fix in row 5: a `FakeTarget` default (or helper) that answers the second read with each plan zip installed at its zip's version and managed state unless a test sets `After`, `SeedAfter` returning version `1.0.0.1`, and the same for the three `McpCommandTests` fakes. List `McpCommandTests.cs` fakes as files touched.
- Resolution: fixed: row 5 moves `SeedAfter` to 1.0.0.1, gives every flipped test an explicit `After` (Fable's WATCH: no auto-default), and changes the `ImportFakes` and `CliApply` second reads in `McpCommandTests` with a good-path and a mismatch MCP test. Second pass: confirmed against the code. `SeedAfter` is in the test file and is set to 1.0.0.1; every flipped test gets its own `After` (the choice of no auto-default is reasoned and accepted); `ImportFakes` and `CliApply` exist in `McpCommandTests.cs` and their second reads are named; `McpCommand.Result` gives `IsError` false for exit 1 with output, as test row 5 pins; the skip-only test's `Calls` of 2 is its current value; `Apply plan result` is the step name in `action.yml`.

### Low

#### R4. "No new tenant query" and the Q2 cost line understate what the flowless read costs
- Area: 7 (Data volume), 9 (Testability)
- Finding: Done when 1 and decisions Q2 say the read is "the second read it already makes" and only spends `pac org who` and Web API reads. `DriftLiveReader.ReadTarget` reads each solution in full: components, flows, variables, references, up to 50 layer reads per managed solution (`MaxLayerReads`), and each dependency. For a flowless plan this is new cost, and any failure among them (a throttled layer read, say) now turns a good flowless import from exit 0 into exit 3. The decision for `BUILD-PLAN.md` names the exit changes but not this cost.
- Suggested fix: Say in Done when 1 or the decision that the read is the full target read and what it adds for flowless plans; keep "Won't build: a narrower or new query" explicit, so the choice is recorded.
- Resolution: fixed: Done when 1 states the full read and its new cost and exit 3; Won't build names a narrower read; the decision and COMMANDS.md say so. Second pass: confirmed. Done when 1 states the full read, its cost and the exit 3; Done when 8 and the BUILD-PLAN decision carry it; Won't build names a narrower read.

#### R5. A tenant-read version string reaches the JSON message and `after.version` unchecked
- Area: 8 (Security of the report)
- Finding: `DriftParsers.Solution` returns the version as plain text with no check, and Done when 3 puts `<v>` in `message` and Done when 2 in `after.version`. Done when 6 cleans the text report only; the JSON carries the raw string, and the message is an agent-facing field. The plan's own constraint says the report holds a version and two booleans, which a non-numeric string would break.
- Suggested fix: Put the version in the message and `after` only when `EqualVersions`' parser accepts it (four numeric parts), else use `unknown` and null; add a hostile-version test for JSON as well as text.
- Resolution: fixed: `after.version` holds the read version only when it parses as four numeric parts, else null and `unknown` in the message; test (k) covers JSON and text. Second pass: confirmed. Done when 2, 3 and 7 and test (k) make a non-parsing version null in JSON and `unknown` in the message, with the schema description saying so.
