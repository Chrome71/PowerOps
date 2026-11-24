plan-hash: 317495ffcd9c5e1889f3c21eedfcf5d62b4821145c0f94ba37f0baf19bbfa31d
model: claude-sonnet-5-5
fresh-context: yes

The plan-hash was updated after the text fixes for R1 to R12 below (the practice of the M7 to M20 reviews). Reviewed hash: 8b09c696a9b5141e2bd0cd2b8dc14ee1fd3203cd51f49e19429a0ce62994db32. The reviewer (powerops-reviewer, Sonnet, fresh context) found 1 High, 5 Medium and 6 Low; the coordinating session relayed the findings, and the planning session wrote this file from that relay and added the resolutions. The fixes changed the Goal and Done when (R2, R8, R12), Data (the glue, the stand-ins, the two tests, the runner counts: R1, R3, R6, R7), Automation (the `Tools` helper and its tests, the `ci.yml` test, the runbook step 2, the not-verified marks: R4, R5, R8, R9, R10) and Steps (the per-step deltas and the hand checks: R6, R11), plus `decisions.md` (Q1 and Q2 wording, the Log). The reviewer contacted no tenant and ran no client.

Most of the Power Platform checklist does not apply: this is a CI, test and docs milestone with no solution, no environment and no screen. Open questions: 1 (the README Install block in Windows PowerShell 5.1). No URL that is not a public docs or GitHub page (each carries the `plan status` allow marker), GUID, email or person's name in the plan or `decisions.md`.

## Findings

### High

#### R1. The stand-in `pac` prints only its arguments, but both consumers parse its stdout
- Area: tests (Step 3, Data: what each Windows test checks, 2)
- Finding: `PlanFacts.Run` looks for a `Friendly Name:` line in the stdout of `org who`; with only an echo it returns Unavailable and never calls `solution list`. `DriftLiveReader.Who` throws "printed no environment name" before `az` runs. So the test as drafted could not reach the second `pac` call or the `az` call it was meant to check.
- Suggested fix: The function given to each consumer runs the real `PacRunner` on the stand-in, records the echo, and returns canned stdout with the real `Started` and `ExitCode`: `tests/fixtures/pac/org-who.txt` for `org who`, `PlanFactsTests`' `SolutionList` text for `solution list`. Write the glue so it also runs on Linux and macOS with a `.sh` stand-in; only the `.cmd` and `.bat` files and PATHEXT stay Windows-only.
- Resolution: fixed (Data now has "The glue": the wrapper keeps the arguments and the real result, and returns the real `Started`, `TimedOut` and `ExitCode` with canned stdout from `org-who.txt` and `PlanFactsTests.SolutionList`, which becomes `internal const`; `az` passes the real result through so `Token` returns the echo. The argument tests run on every system with `.sh` stand-ins on Linux and macOS and `.cmd` files on Windows, so the glue is proven on every CI run. Both tests assert that the real result started with exit 0, that the echo equals the recorded list joined by spaces, and that the second call was reached: `PlanFacts` made both calls and returned `available: true`, `Read` made the `pac` and `az` calls. The class is `StandInTests`; Step 3 and its delta follow)

### Medium

#### R2. The macOS test must assert on the bash the script tests start
- Area: tests (Done when, Automation: Tests)
- Finding: The test checked a literal `/bin/bash`, while every script test starts whatever `Tools.Bash` names. A change in the helper could leave the test green and the scripts under another bash.
- Suggested fix: Assert on `Tools.Bash`, and say so in the done-when.
- Resolution: fixed (the first Done when bullet and the Tests bullet name `Tools.Bash`; the check by hand runs the same body, without the operating-system guard, against the local bash 5; `decisions.md` Q1 says the same)

#### R3. `Resolve` returns the extension as PATHEXT spells it
- Area: tests (Data: what each Windows test checks, 1)
- Finding: With PATHEXT `.COM;.EXE;.BAT;.CMD`, `Resolve` returns `pac.BAT` for a file written as `pac.bat`, so an exact file-name comparison fails, and "the one the real PATHEXT lists first" was not derived from anything.
- Suggested fix: Compare file names ignoring case, and derive which of `.bat` and `.cmd` is expected from their positions in the real PATHEXT.
- Resolution: fixed (Data, test 2: the test splits the real PATHEXT, finds `.bat` and `.cmd` ignoring case, requires both, expects the earlier one, and compares names with the case ignored; the same for `az.cmd`)

#### R4. A test cannot observe an xunit v3 skip
- Area: tests (Automation: Tests)
- Finding: The plan had tests assert that `Require` "skips" with the variable off. A skip is an exception the runner consumes; the test cannot see it.
- Suggested fix: A parameterised core returns a verdict (the missing program's name, or null); the public `Require` turns it into `Assert.Fail` or `Assert.Skip`. The tests assert on the verdict and on the failure message.
- Resolution: fixed (Automation: `Tools.Missing(programs, path)` and `Tools.FailMessage(program)` are the tested core; `Require` is the thin wrapper; the tests assert that `Missing` returns the name or null and that the message names the program and `POWEROPS_REQUIRE_TOOLS`. The plan says which two lines no unit test reaches and what covers them instead: the `ci.yml` test and the log check)

#### R5. Nothing fails if the `POWEROPS_REQUIRE_TOOLS` line is deleted from `ci.yml`
- Area: tests (Automation: ci.yml)
- Finding: The variable is the only thing that turns a missing tool into a failure in CI, and the plan had no check that the workflow still sets it.
- Suggested fix: A small test that reads the build job's `dotnet test` step `env` in `ci.yml`, as `ActionYmlTests` reads `action.yml`; keep a log check in Step 2.
- Resolution: fixed (Tests: a `ToolsTests` test reads `ci.yml` with YamlDotNet and requires `env.POWEROPS_REQUIRE_TOOLS` on the `dotnet test` step of the `build` job to equal the expression, so deleting it or setting it for Windows fails; Step 2 also reads the printed `env:` block in the Linux and macOS logs)

#### R6. The 2241 and 140 baseline is old, and the arithmetic was wrong
- Area: checks (Data: the runners, Steps 3)
- Finding: The counts came from a run of the M25 branch, before M26 to M31, and the step said Windows would still skip 140 although the macOS-only test also skips there. Passing tests are not named in the default log, so totals were the only evidence.
- Suggested fix: Check by name in the CI log that the new tests passed, or take the baseline from the branch's base and state a per-step delta.
- Resolution: fixed (the counts are kept only as the pattern of skips and are marked as no baseline; Steps now give a per-step, per-system delta against the run before the step: step 1 is 1 test more, skipped +1 on Linux and Windows and +0 on macOS; step 2 is 4 more, skipped +0; step 3 is 3 more, skipped +1 on Linux and macOS and +0 on Windows. The base `96908ed` has no CI run yet, so the PR's first run is its own baseline. The log does not name passing tests, which the plan says)

### Low

#### R7. The resolver already ran on Windows
- Area: Data (what each Windows test checks, 1)
- Finding: `PacRunnerResolveTests` already run on Windows with temp paths and explicit PATHEXT strings, so "the first time the resolver sees real paths" overstated what is new.
- Suggested fix: Say that only the real PATHEXT string and order are new.
- Resolution: fixed (Data, test 2 says so)

#### R8. Where `Tools.cs` lives, `EphemeralScriptTests`, and Windows test 3
- Area: Automation (Files)
- Finding: `tests/Shared` compiles into both test projects, and only `PowerOps.Cli.Tests` uses the helper. `EphemeralScriptTests` should stay in scope. Windows test 3 (S-9) and the move of M30's `InlineData` array add nothing, because `Token` throws before `az` on every system and the existing test already runs on Windows.
- Suggested fix: Put `Tools.cs` in `tests/PowerOps.Cli.Tests/`; keep `EphemeralScriptTests`; drop test 3 and the array move.
- Resolution: fixed (Automation lists `tests/PowerOps.Cli.Tests/Tools.cs`; `EphemeralScriptTests` stays; the S-9 test and the array move are gone, and the Goal says M30's theory already covers S-9 on every system)

#### R9. The not-verified marks
- Area: docs (Automation: the not-verified marks)
- Finding: One claim had no source (that neither client documents starting a bare name from a `dotnet tool` shim on Windows), C-10b was missing, and a PATH holding only `pac.bat` or `pac.cmd` would put a free-text `--environment` through `cmd.exe` without any check.
- Suggested fix: Source or cut the claim; add C-10b; add the `cmd.exe` point as not checked.
- Resolution: fixed (the claim is cut from the marks and kept in Assumptions with its source, BUILD-PLAN's Phase 6 research basis, Windows bullet, checked 2026-10-28; the marks list C-10b, the guard around `Console.OutputEncoding` for a server a GUI client started with no console, and the `pac.bat` or `pac.cmd` shim with free-text `--env` as not checked, noting that S-9 guards only `az`'s URL)

#### R10. `where` is an alias in PowerShell
- Area: runbook (step 2)
- Finding: In PowerShell `where` is `Where-Object`, so `where powerops` does not find the program.
- Suggested fix: `Get-Command powerops`, or `where.exe`.
- Resolution: fixed (step 2 names `Get-Command powerops` for PowerShell, `where.exe powerops` for cmd and `command -v powerops` for macOS)

#### R11. Nothing tests the anchors or the marks
- Area: checks (Step 5)
- Finding: Step 5 said `DocsDriftTests` checks the link to the runbook and the marks. It checks the commands and flags in code spans, not these sentences or an anchor.
- Suggested fix: Say they are read by hand.
- Resolution: fixed (Step 5: checked by hand against the list of marks, with `DocsDriftTests` still required to pass)

#### R12. Wording
- Area: plan form and Constraints
- Finding: "Questions for your admin" held a sentence although nothing is open, which the planning skill leaves empty; and "the script tests cannot run on Windows" overstated it, since Git for Windows ships a bash and the tests skip there by design.
- Suggested fix: Leave the section empty; say "skip by design".
- Resolution: fixed (the section is empty; the Data, Automation, marks and `decisions.md` Q2 say the script tests skip by design on Windows)
