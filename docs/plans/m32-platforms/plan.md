---
status: signed-off
review: done
signed-off: 2026-10-28
signed-off-hash: c5dc6865e35523d045704061b6e50db08183344c365ae4b8c2158276e3a3a4d7
environment: null
depth: quick
supersedes: null
---

# M32: Windows and macOS

## Goal

Build BUILD-PLAN milestone M32 (build-order item 57, no release label: CI, tests and docs only). The spec is the Phase 6 plan's P-1 row, its decisions Q2 and Q10, and the BUILD-PLAN M32 row, whose done-when is the bar. P-1 says that nothing has run on Windows or macOS beyond `dotnet test` and the packed-tool test, and names four gaps: the clients starting a bare `powerops` from PATH, `pac`'s sign-in seen from a server a client started, `az.cmd`'s argument path, and the S-1 resolver's PATHEXT order; plus a check that `ActionScriptTests` run under macOS's bash 3.2 rather than skip. A hosted runner can check the last two and the bash point. It cannot check the clients or `pac`'s sign-in: they need a person's sign-in, which FOUNDATION §4 keeps out of CI. S-9's guard (a host holding `&` never reaches `az`) is already tested on every system by M30's theory, because `Token` throws before it calls `az`; this plan adds no copy of it.

This plan settles four things the row leaves open: how macOS pins bash 3.2, how a skipped test is kept from passing for one that ran, how the stand-in test gets the exact argument lists without copying them, and where the manual runbook lives and what the docs say until someone runs it. The user answered on 2026-10-28 that no Windows or macOS machine is available and that they run Linux, so the runbook is written and not run, and its points are marked not verified with that reason (decision Q10 of the Phase 6 plan).

Done when:

- On macOS, the script tests start `Tools.Bash`, the helper's name for the bash they run (`/bin/bash` on macOS, `bash` elsewhere), and a macOS-only test fails unless `Tools.Bash` reports version 3.2.
- With `POWEROPS_REQUIRE_TOOLS=1`, a missing `bash`, `jq` or `git` fails the script tests instead of skipping them. CI sets it on the Linux and macOS test steps and not on Windows, and a test fails if that line leaves `ci.yml`.
- A test class of stand-in `pac` and `az` files runs on all three systems (`.cmd` and `.bat` files on Windows, `.sh` files elsewhere). It runs each through `PacRunner` with the argument lists that `PlanFacts` and `DriftLiveReader` pass, and checks that each echoes exactly those arguments. On Windows one more test checks which of `pac.bat` and `pac.cmd` the real PATHEXT makes `Resolve` pick.
- BUILD-PLAN's "Live testing runbook" has a "Windows and macOS" subsection, for a person with such a machine. Offline tools only.
- README, the MCP section of `docs/USE-WITH-AGENTS.md` and FOUNDATION §8 say which points are not verified on Windows and macOS, and why, until a person runs the runbook and records it.
- The job names in `ci.yml` do not change, so the six required checks of the `main` ruleset (M23) hold.
- `dotnet build --no-incremental` (0 warnings), `dotnet test`, `dotnet format --verify-no-changes` and `powerops plan status .` are clean, and the PR's CI is green on Linux, Windows and macOS.

## Users

- Contributors on any system, whose `dotnet test` should not skip a script test without saying so.
- A person with a Windows or macOS machine who runs the runbook. Nobody has one today.
- The maintainer, who reads the three CI results and the required-check names.

## Data

**What the runners are** (read 2026-10-28, from a CI run of this repository and the runner-images repository):

- `macos-latest` is the image `macos-26-arm64`, version 20261013.0424.1, macOS 26.6.2. The job log says so, and the runner-images README lists `macos-latest` as "macOS 26 Arm64". That image's README lists one bash, `3.2.57(1)-release`; its report generator runs `bash -c 'echo ${BASH_VERSION}'`, and the image's brew toolset (`common_packages`) installs no `bash`. The same job's log shows `shell: /bin/bash -e {0}`, which is the runner's own lookup of `bash` on the step's PATH, and its test step skipped nothing. So the premise that Homebrew's bash 5 comes first on PATH is not what the image shows: `ActionScriptTests` already run under bash 3.2.57 on macOS today, and nothing pins that. (Inferred in part: the job does not print its PATH.) The README says a `-latest` label moves to a new image in a gradual migration of one to two months, which is the change the version test is there to notice.
- `windows-latest` is `windows-2025-vs2026`, version 20261021.278.1. Its test step skipped 140 tests, by the tests' own `SkipWhen(OperatingSystem.IsWindows())` guards (the script tests skip by design on Windows, and link tests need privileges); the Linux and macOS steps skipped none. That run is of the M25 branch, before M26 to M31, so its counts show the pattern of skips and are not a baseline (Steps).
- Sources, read 2026-10-28:
  - the macOS 26 Arm64 image README: https://github.com/actions/runner-images/blob/main/images/macos/macos-26-arm64-Readme.md <!-- plan-status: allow -->
  - the runner-images README, for the label table: https://github.com/actions/runner-images/blob/main/README.md <!-- plan-status: allow -->
  - the image's brew toolset (no `bash` formula): https://github.com/actions/runner-images/blob/main/images/macos/toolsets/toolset-26.json <!-- plan-status: allow -->
  - the report generator (`Get-BashVersion`): https://github.com/actions/runner-images/blob/main/images/macos/scripts/docs-gen/SoftwareReport.Common.psm1 <!-- plan-status: allow -->
  - the job log of `build (macos-latest)` in CI run 40797558098, read with `gh run view --job <id> --log`

**Where the argument lists come from.** Nothing is copied into the test; it records what the code passes.

- `PlanFacts.Run` takes the `pac` function as a parameter. Its two lists, `org who` and `solution list`, are private arrays that only the function it is given ever sees.
- `DriftLiveReader` takes `pac` and `az` functions too (production builds them in `Program.Root`: `new PacRunner(DriftLiveReader.AzExecutable(OperatingSystem.IsWindows()), timeout).Run(azArgs)`). `Who` builds `org who --environment <env>` and `Token` builds `account get-access-token --resource <url> --query accessToken -o tsv` inline, with `<url>` the organization's scheme and host, without a path, after `DriftParsers.EnvironmentUrl` has checked it.
- `LiveFakes` (in `DriftLiveReaderTests.cs`) already records every list in `PacCalls` and `AzCalls` and has a default `PacFn` that answers `org who` from `tests/fixtures/pac/org-who.txt`.

**The stand-ins.** In a temp folder, each prints its arguments joined by spaces and nothing else. On Windows they are `pac.cmd`, `pac.bat` and `az.cmd`, each:

```
@echo off
echo %*
```

On Linux and macOS they are `pac` and `az`, executable, each `#!/bin/sh` and `printf '%s\n' "$*"`.

**The glue.** The echo is not what the consumers parse. `PlanFacts.Run` looks for a `Friendly Name:` line in the stdout of `org who`; without one it returns Unavailable and never runs `solution list`. `DriftLiveReader.Who` throws "printed no environment name" before `az` runs. So the function given to each consumer runs the real `PacRunner` on the stand-in, records the arguments and the real result, and returns a `PacResult` with the real `Started`, `TimedOut` and `ExitCode` and canned stdout: `tests/fixtures/pac/org-who.txt` for `org who`, and `PlanFactsTests.SolutionList` for `solution list` (its `private const` becomes `internal const`). For `az` the real result passes through unchanged, so `Token` returns the echo as its token. The glue is plain C# over `Process`, so it runs on Linux and macOS with the `.sh` stand-ins and is proven there on every CI run before it carries the `.cmd` files on Windows.

**What the tests check.**

1. Arguments, on every system. The stand-ins are found with the production names and `PacRunner.Resolve`: `"pac"`, and `DriftLiveReader.AzExecutable(OperatingSystem.IsWindows())`, with the real PATHEXT on Windows. Then, for `PlanFacts.Run` and for `LiveFakes.Reader.Read` (two tests):
   - every real result has `Started`, exit 0, and a stdout that, trimmed of `\r\n`, equals its recorded list joined by spaces;
   - `PlanFacts` made both calls and returned `available: true`, and `Read` made the `pac` call and the `az` call, so the canned replies were parsed.
   None of the values holds a space, which .NET would quote. On Windows this sends the lists through `cmd.exe`, and, as far as the tests show, is the first time `PacRunner.Run` starts a child process there (the existing tests that do so skip on Windows).
2. PATHEXT order, Windows only. With `pac.cmd`, `pac.bat` and `az.cmd` in the folder, the test splits the real PATHEXT, finds the positions of `.bat` and `.cmd` ignoring case (both must be there), and expects `Resolve("pac", <folder>, windows: true, <real PATHEXT>)` to return the file whose extension comes first. `Resolve` returns the extension as PATHEXT spells it (`pac.BAT`), so the file names are compared ignoring case. `Resolve` of the `AzExecutable(true)` name returns `az.cmd` the same way. The resolver already ran on Windows with temp paths and explicit PATHEXT strings (`PacRunnerResolveTests`); what this adds is the host's own PATHEXT value and order.

## Automation

**Files.**

- `tests/PowerOps.Cli.Tests/Tools.cs` (new, in the project that uses it): `Tools.Bash` (`/bin/bash` on macOS, `bash` elsewhere), and `Tools.Require(params string[] programs)` with the pure core it calls, which move out of `ActionScriptTests` and `EphemeralScriptTests`, where the same `Require` and `OnPath` are copied.
  - `Tools.Missing(programs, path)` returns the first program with no file on `path`, or null. It is what the tests assert on.
  - `Tools.FailMessage(program)` is the text of the failure: it names the program and `POWEROPS_REQUIRE_TOOLS`.
  - `Require` skips on Windows ("the scripts skip there by design"), then asks `Missing` about the environment's PATH. For a program that is missing it calls `Assert.Fail(FailMessage(...))` when `POWEROPS_REQUIRE_TOOLS` is `1` and `Assert.Skip` otherwise. The variable follows `POWEROPS_SKIP_E2E` (`PackedToolTests`): an environment variable compared with `"1"`.
  - `Require("bash")` checks the file `Tools.Bash` names. `ActionScriptTests.SkipUnless("git")` uses the same verdict without the Windows skip.
- `ActionScriptTests.cs` and `EphemeralScriptTests.cs`: `new ProcessStartInfo(Tools.Bash)` in place of `"bash"` (`ActionScriptTests.Run`, and the two places in `EphemeralScriptTests`). The stub `gh`, `dotnet` and `pac` files keep `#!/usr/bin/env bash`: they are test doubles, not the scripts under test. `ExampleWorkflowTests` starts `bash -c` on a snippet cut from a workflow and stays as it is.
- `ToolsTests.cs` (new): the macOS version test, the `Missing` and `FailMessage` tests, and the `ci.yml` test (Tests).
- `StandInTests.cs` (new): the three stand-in tests (Data). `PlanFactsTests.cs`: `SolutionList` becomes `internal const`.
- `.github/workflows/ci.yml`: the `dotnet test` step of the `build` job gets `env: POWEROPS_REQUIRE_TOOLS: ${{ runner.os != 'Windows' && '1' || '' }}`. No job is added, renamed or split, so `build (ubuntu-latest)`, `build (windows-latest)`, `build (macos-latest)`, `package`, `action` and `playwright-example` stay as the ruleset requires them. The M23 row says the check list is revisited after M29 and M32: after M32 nothing in it changes. Windows skips the script tests whatever the variable says, because they skip there by design.
- `CONTRIBUTING.md`: the line about tests that skip says that CI sets `POWEROPS_REQUIRE_TOOLS=1` on Linux and macOS so a missing program fails there, and that a contributor can set it too.
- `docs/BUILD-PLAN.md`: the runbook, the built marks and the verification record (Steps 4 and 6). `README.md`, `docs/USE-WITH-AGENTS.md`, `docs/FOUNDATION.md`: the not-verified marks (Step 5).

**Tests.** A skip cannot be seen from inside an xunit v3 test, so the tests assert on the verdict and on the message, never on a skip.

- `ToolsTests`, macOS only (`Assert.SkipUnless(OperatingSystem.IsMacOS(), ...)`): `Tools.Bash -c 'echo ${BASH_VERSION}'` starts with `3.2.`. It asserts on `Tools.Bash`, the value every script test starts, not on a literal path. Its failing case is checked by running the same body, with the operating-system guard bypassed, against this machine's bash 5 before the commit: it must fail.
- `ToolsTests`, every system:
  - `Missing` returns the name of a program that is not in a temp folder used as PATH, and null when the file is there;
  - `FailMessage` names the program and `POWEROPS_REQUIRE_TOOLS`.
  The one branch these do not reach, that `Require` reads the variable and fails instead of skipping, is two lines; the `ci.yml` test and the log check in Step 2 cover that the variable is set where it matters.
- `ToolsTests`, every system, `ci.yml`: the file is read with YamlDotNet as `ActionYmlTests` reads `action.yml`. The step of the `build` job whose `run` is `dotnet test -c Release --no-build` must have `env.POWEROPS_REQUIRE_TOOLS` equal to the expression above. Deleting the line, or setting it for Windows, fails it.
- `StandInTests`: Data, tests 1 and 2. The two argument tests run everywhere; the PATHEXT test skips on Linux and macOS because of the operating system, not because a tool is missing, so `Require`'s variable does not apply to it.
- No change under `src/`. If the stand-in test finds that `PacRunner` cannot start a `.cmd` or that the arguments change on the way, that is a bug: it gets a `fix:` commit with this test as its check, and the test is not loosened.

**The runbook** (BUILD-PLAN, "Live testing runbook", a new subsection "Windows and macOS"). For a person with such a machine. It reads and writes no tenant; the only `pac` call is `pac auth list`, which reads the local profile store. Every client session uses a throwaway config folder, deleted afterwards, as M13 and M20 did.

0. Have: the .NET 10 SDK (`global.json`), git, Claude Code, Codex and `pac`; write down their versions, the operating system's version and architecture, and the shell.
1. Install from a clone, with the lines in the README's Install section, run as written in the shell you use (on Windows, PowerShell 7, and Windows PowerShell 5.1 if you have it; on macOS, zsh). Write down what you had to change. (See Open questions.)
2. Open a new shell and run the bare `powerops --version`; then `Get-Command powerops` (PowerShell), `where.exe powerops` (cmd) or `command -v powerops` (macOS). Expected: the version, and a path in the dotnet tools folder.
3. `powerops analyze tests/fixtures` in the clone. Expected: exit 0, and a last line equal to the last line of `tests/golden/analyze-fixtures.txt` (`11 solutions, 0 errors, 36 warnings` when this was written).
4. Claude Code, with `CLAUDE_CONFIG_DIR` pointing to a new empty folder: `claude plugin marketplace add <the clone>`, `claude plugin install powerops@powerops`, `claude plugin details powerops` (three skills and one MCP server), `claude mcp list` from the new shell (`plugin:powerops:powerops` connected). Then start `claude` in the clone, load the plugin as M20 did (`--plugin-dir plugins/claude`) if you do not want a new sign-in, and run `/mcp`: the server lists 12 tools.
5. Codex, with `CODEX_HOME` pointing to a new empty folder outside the system temp folder: `codex plugin marketplace add <the clone>`, `codex plugin add powerops@powerops`, `codex mcp get powerops --json` (enabled, `tool_timeout_sec` 3600.0), then `codex login`, `codex` in the clone, and `/mcp`: `powerops` with 12 tools. Then `codex logout`.
6. The 12 names are those in `tests/golden/mcp-tools.txt`. Optionally ask each agent to call `analyze` on the absolute path of `tests/fixtures` (a `C:\` path on Windows): its result matches step 3. Do not call `plan_facts`, `drift` against an environment, `deploy_plan` or `deploy_apply`: they read a tenant. Never set `apply`.
7. `pac auth list`. Expected: exit 0 and the profiles you know are there, or none. Do not sign in. If `pac` is not installed, write that down.

The record goes in BUILD-PLAN's verification record as a line "M32 (date)": the system and versions, each step as passed, failed or changed, and what you had to change. No machine name, user name, path outside the clone or tenant data. Each point that passed is then removed from the not-verified marks, and each failure becomes a finding.

**The not-verified marks** (Step 5), until the runbook is run. One short paragraph in the README's Install section, one sentence in the "Register it" paragraph of the MCP section in `docs/USE-WITH-AGENTS.md` (which now holds the only Linux-only claims: the M20 run and Codex's environment), and one open item in FOUNDATION §8. Together they say:

- CI builds and tests PowerOps on Linux, Windows and macOS; it runs the Action and example script tests under macOS's bash 3.2, and runs stand-in `pac` and `az` files, `.cmd` files on Windows, through `PacRunner`.
- Nobody has run it by hand on Windows or macOS, because no such machine was available on 2026-10-28 (the user runs Linux). Not verified there:
  - the install from a clone and the bare `powerops` from a new shell;
  - Claude Code and Codex starting the plugin's `powerops`;
  - the real `az.cmd` and the real `pac`;
  - C-10b, the guard around `Console.OutputEncoding` for a server a GUI client started with no console (`Program.Main` catches the `IOException`; no test has run it without a console);
  - a free-text `--environment` through `cmd.exe`: the real `pac` is a `pac.exe`, which does not go through it, but if the PATH held only a `pac.bat` or `pac.cmd` shim the text of `--env` would, and .NET's quoting is no escape for `cmd.exe` (S-9 guards only `az`'s URL). Not checked.
- `pac`'s saved sign-in as seen from a server a client started stays not verified even after the runbook: on Linux, Codex's reduced environment broke it (M20), and no offline tool shows it, because the only tools that run `pac` read a tenant. The runbook's `pac auth list` runs in a normal shell and shows only that `pac` starts there.
- The Action's scripts and the example scripts are not run on Windows (their tests skip there by design).

## Screens

None.

## Environments and route

None. No tenant is read or written. The stand-in tests start files from a temp folder by their full path; they are not the real `pac` or `az`, and this session never runs either. The runbook's `pac auth list` is for a person to run.

## Solution and publisher

None.

## Constraints

- AGENTS.md: the smallest change that fully solves each point, tests for new logic, `dotnet format --verify-no-changes` clean, Conventional Commits (`test:`, `ci:`, `docs:`), no attribution lines, no secrets, tenant URLs or credentials, no edit of a version or the changelog, no push or pull request from this session.
- No code under `src/` and no new dependency, unless the stand-in test finds a bug (Automation, Tests).
- Arguments to a subprocess stay a list. The stand-ins exist to prove that the list reaches the file unchanged.
- Offline: nothing here calls a tenant, `pac` or `az`. The runbook's steps are offline except `pac auth list`, which reads only the local profile store.
- No new CI job and no renamed job: the six required check names in the `main` ruleset hold.

## Assumptions

- The image facts above hold for the next CI run: `macos-latest` is `macos-26-arm64` with a system bash 3.2.57 first on PATH. The version test is what fails if that changes. The `-latest` label moves between images; the test is the notice.
- A stand-in `pac.cmd` runs through `cmd.exe`, and the real `pac` (a `pac.exe` from the dotnet tool) does not. So the `pac` half of the Windows run covers the resolver and the transport of the argument list, and the quoting question belongs to `az.cmd` (S-9). The real `pac` and `az` are not run by this plan.
- `PacRunner` has not been seen starting a `.cmd` on Windows. If the first CI run fails there, that is the finding, and the test is not loosened to pass.
- BUILD-PLAN's Phase 6 research basis (Windows bullet, checked 2026-10-28) records that neither client documents starting a bare-name stdio server from a `dotnet tool` shim on Windows. That is why the clients' start of `powerops` is a runbook step; the marks list it as not verified without repeating the claim.
- M29 also edits `ci.yml` (locked restores, the vulnerable-package step, telemetry). The two edits touch different lines of the `build` job; whichever merges second rebases. Neither renames a job.
- Tests that skip because of the operating system (the PATHEXT test, the macOS version test, and every Windows guard) are not a missing tool. The Linux and macOS steps will report skipped tests where the Windows-only or macOS-only test skips there; the count is no longer a signal of a missing tool and nothing reads it. A skip count is checked only as a delta between two runs (Steps).
- The hosted Linux and macOS runners have `bash`, `jq` and `git` (the macOS README lists jq 1.8.2 and git 2.56.0; the macOS and Linux test steps skipped nothing in the run read above). If an image drops one, the step fails, which is the point of the variable.

## Open questions

- Whether the README's Install block runs in Windows PowerShell 5.1. It chains two commands with `&&`, and Learn says PowerShell implements that operator beginning in PowerShell 7 (https://learn.microsoft.com/powershell/module/microsoft.powershell.core/about/about_pipeline_chain_operators; inferred, nobody has tried it). <!-- plan-status: allow --> The runbook's first step records what happened; a PowerShell form of the block is a separate `docs:` change if it failed.

## Questions for your admin

## Steps

Each step's CI check is a difference between two runs, not a total. The run to subtract from is the one for the commit before the step (the previous step's push, or the PR's base `96908ed` on `claude/m31-docs-drift`, which has no CI run yet, so the first run of the PR is its own baseline). The Windows job's log shows no test names on success, so the per-system delta in the number of tests and of skipped tests is the evidence that the new tests ran where they should.

1. `test:` `Tools` (`Bash`, `Missing`, `FailMessage`, `Require` moved out of the two copies), `ActionScriptTests` and `EphemeralScriptTests` start `Tools.Bash`, and the macOS-only version test. Check: on this machine the suite passes with the same counts plus the one new test (skipped); the version test's body fails when run against the local bash 5. CI delta: 1 test more on each system; skipped +1 on Linux and Windows, +0 on macOS (it ran, which is the check).
2. `ci:` `Require` fails instead of skipping when `POWEROPS_REQUIRE_TOOLS` is `1`; the Linux and macOS test steps set it; `CONTRIBUTING.md` says so; the `Missing`, `FailMessage` and `ci.yml` tests. Check: here, `POWEROPS_REQUIRE_TOOLS=1 dotnet test` passes with the script tests running, and the same run with a PATH that lacks `jq` fails them instead of skipping; the `ci.yml` test fails when the line is removed; in the PR's Linux and macOS logs the test step's printed `env:` block shows `POWEROPS_REQUIRE_TOOLS: 1`. CI delta: 4 tests more on each system, skipped +0.
3. `test:` `StandInTests` and `PlanFactsTests.SolutionList` made `internal`. Check: the two argument tests pass on this machine with the `.sh` stand-ins and the PATHEXT test skips. CI delta: 3 tests more on each system; skipped +1 on Linux and macOS (the PATHEXT test), +0 on Windows (all three ran).
4. `docs:` the "Windows and macOS" subsection of BUILD-PLAN's "Live testing runbook". Check: every command in it exists (`claude plugin`, `codex plugin`, `codex mcp get`, `pac auth list`), as in M13 and M20; the expected strings match `tests/golden/analyze-fixtures.txt` and `tests/golden/mcp-tools.txt`.
5. `docs:` the not-verified marks in README, `docs/USE-WITH-AGENTS.md` and FOUNDATION §8. Check by hand: the link to the runbook's anchor resolves, and each mark matches the list under "The not-verified marks". `DocsDriftTests` must still pass, but it checks the commands and flags in code spans, not these sentences or that anchor.
6. `docs:` mark M32 built in BUILD-PLAN: the M32 row (with a link to this plan), build-order item 57, the Phase 6 Status line, a verification-record line saying the CI checks passed and the runbook is not run (and why), and a note on the M23 row that the job names are unchanged. This plan's status.

The release label is none (CI, tests and docs). The commit types are `test:`, `ci:` and `docs:`.
