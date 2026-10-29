# Decisions: m32-platforms

## Round 1

### Q1. How do the script tests run under macOS's bash 3.2?
1. On macOS the tests start `/bin/bash` explicitly, through one helper (`Tools.Bash`) that `ActionScriptTests` and `EphemeralScriptTests` both use, and one macOS-only test fails unless `Tools.Bash` reports version 3.2 (recommended)
2. Keep starting `bash` from PATH, and add a macOS-only test that fails unless the `bash` on PATH reports 3.2
3. In your own words
Answer: On macOS the tests start `/bin/bash` explicitly, through one helper, and a macOS-only test asserts the version of `Tools.Bash`, the value every script test starts. The runner's facts changed the reason, not the answer. The image README for `macos-latest` (macos-26-arm64, image 20261013.0424.1) lists one bash, 3.2.57, its toolset installs no Homebrew `bash`, and the job log shows `shell: /bin/bash -e {0}` and no skipped tests, so the script tests already run under 3.2.57 today. Nothing pins that. Option 2 would pin it on the runner and no further: on a contributor's Mac with Homebrew's bash first on PATH, the tests would run under bash 5 or, with the version test, fail for a setup that is not wrong. The explicit start gives the same result on every Mac, and the version test fails if Apple or a new image changes the bash that `Tools.Bash` names. The stubs' `#!/usr/bin/env bash` lines stay, because the stubs are test doubles and not the scripts under test. `EphemeralScriptTests` is included because it holds a copy of the same helper and its script says it avoids what bash 3.2 lacks.

### Q2. Does a missing program skip the script tests or fail them in CI?
1. A test-only variable, `POWEROPS_REQUIRE_TOOLS=1`, set on the Linux and macOS test steps in `ci.yml`: `Require` fails when a program is missing and skips otherwise, so a contributor without `jq` is not blocked (recommended)
2. A CI step that fails when the test summary reports any skipped test
3. In your own words
Answer: The variable. It names what CI wants (every script test runs) in the one place that decides, `Require`, and it follows `POWEROPS_SKIP_E2E`, an environment variable compared with `"1"`. Option 2 cannot work as written: the Windows step skips 140 tests by design in the run read (an older run than this branch's base), the new Windows-only and macOS-only tests skip on the other systems, and the count would have to be parsed from the runner's output. The variable is not set on Windows, where the script tests skip by design (Git for Windows has a bash, but the tests do not use it). A test reads `ci.yml` and fails if the line leaves the test step, because nothing else would notice.

### Q3. Where does the Windows and macOS runbook live?
1. A subsection "Windows and macOS" of BUILD-PLAN's "Live testing runbook", with its result in the verification record beside it; README, `docs/USE-WITH-AGENTS.md` and FOUNDATION §8 carry only the not-verified marks (recommended)
2. A new page under `docs/` for users, linked from the README
3. In your own words
Answer: BUILD-PLAN's runbook. It is a maintainer's test procedure, like the live runbook above it, and its result belongs in the verification record, so one place holds the steps and the evidence. A page for users would put steps nobody has run next to the install instructions, and `DocsDriftTests` would hold every command in it to the code, including `claude` and `codex` commands that the test cannot check. The docs that users read say what is not verified and why, and link to the runbook.

## Log

- 2026-10-28: plan started from the Phase 6 plan (the P-1 row, the Automation section that gives M32 its own quick plan, decisions Q2 and Q10), at the user's request to build Phase 6 in risk order
- the user's answer, 2026-10-28: no Windows or macOS machine is available, and the user runs Linux. So the runbook is written and not run, its manual steps are marked not verified with that reason, and CI covers what a hosted runner can (decision Q10)
- read on 2026-10-28: the Phase 6 plan (P-1, Automation, Questions for your admin), its decisions Q2 and Q10, the BUILD-PLAN M32 row, decision 37 and the live testing runbook, the verification records of M13 and M20 (the install and `/mcp` steps the runbook repeats), `ci.yml`, `ActionScriptTests` and `EphemeralScriptTests` (`Require`, the `bash` starts and the stub shebangs), `PackedToolTests` (`POWEROPS_SKIP_E2E`), `PacRunner` and `PacRunnerResolveTests` (the S-1 resolver), `PlanFacts` (`org who`, `solution list`), `DriftLiveReader` and `DriftLiveReaderTests` (`org who --environment <env>`, the `az account get-access-token` list, `LiveFakes`, the M30 check of `&`), `PlanChecker` (what `plan status` scans), the README, `docs/USE-WITH-AGENTS.md`, `docs/FOUNDATION.md` and `CONTRIBUTING.md` for where platforms are mentioned, and the M20 plan's notes on what was not verified on Windows and macOS
- read on 2026-10-28, for Q1 and Q2: the runner-images repository (the label table, the macOS 26 Arm64 image README, `toolset-26.json`, the report generator) and the job logs of `build (macos-latest)`, `build (ubuntu-latest)` and `build (windows-latest)` in one CI run of this repository (the M25 branch, before M26 to M31, so its counts show the pattern of skips and are no baseline): image `macos-26-arm64` 20261013.0424.1, `shell: /bin/bash -e {0}`, 2241 tests with 0 skipped on macOS and Linux and 140 skipped on Windows. This corrected the brief's premise that Homebrew's bash 5 comes first on the macOS runner. The job does not print its PATH, so that the tests ran under 3.2.57 is inferred from the README and the shell line. Learn's page on PowerShell's `&&` operator, for the Open question
- depth: quick
- the user delegated their parts on 2026-10-22 (relayed by the master session): this session answers with the recommended options and signs the plan off itself once a fresh review leaves no High or Medium finding; the runbook, any sign-in and any tenant step still need a person
- approved without: environments and route, screens (nothing is read or written in a tenant, and there is no UI)
- approved (2026-10-28)
- fresh review (powerops-reviewer, Sonnet) 2026-10-28: 1 High, 5 Medium, 6 Low; all fixed in `plan.md` and this file (`review.md` has each finding and its resolution)
- signed off 2026-10-28 by the building session under the user's delegation of 2026-10-22: after the fixes no High or Medium finding is open
