---
status: implementing
review: done
signed-off: 2026-10-29
signed-off-hash: b8b05a1ae15342c2656b1de7564278aa7fcf0d0211dfc4062311b5dfb80149db
environment: null
depth: quick
supersedes: null
---

# M33: Fixes from the review of 2026-10-29

## Goal

Fix the 13 findings of the independent review of `main` at `df04828` (2026-10-29; 3 medium, 10 low, no high). Two reviewers agreed every finding, with its evidence and a recommendation; the review report is kept outside the repo, and each finding is restated below so that this plan stands alone. The user asked on 2026-10-29 to fix all 13, including U-2, which `docs/plans/m28-write-paths/decisions.md` Q2 had accepted. No feature is added and no tenant is read or written.

Done when:

- Each finding below is fixed as its row says, with a test that fails without the fix where the row names one, and the coordinating session has re-run the review's evidence for it and seen the problem gone.
- The fix PR is green on Linux, Windows and macOS and squash-merged; then a second PR moves every PowerOps pin in the docs to that merge commit, is green and is merged, and the new warning job ran on both pushes to `main`: it warns after the first and is quiet after the second.
- `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` and `powerops plan status .` are clean before each push.

## Users

- Someone who copies the README or `docs/GITHUB-ACTION.md` snippets into CI (F-1, U-1, F-5, F-6).
- An agent that runs the planning skills (S-2) or the MCP server (X-1).
- A person reading a `deploy apply` report after a timeout (X-2), or `settings` text output in a CI log (S-1).
- A person who mistypes `--out` in a command that scans a folder (U-2), or runs `powerops`, `deploy gate --help` or `analyze` in an empty folder (F-2 to F-4).

## Data

The findings, the change for each, and its test. Line numbers are those of `df04828`.

| Id | Severity | Finding | Change | Test |
|---|---|---|---|---|
| F-1 | medium | README.md:35 and docs/GITHUB-ACTION.md:14, :46 and :152 pin `b4abce4`, 20 commits before main and before all of Phase 6 | The fix PR sets every PowerOps pin to `df04828` (main now, after all of Phase 6); the second PR moves them to the fix PR's merge commit. The pins are the `git -C PowerOps checkout <sha>` line of README.md and every `Chrome71/PowerOps@<40 hex>` in README.md, docs/GITHUB-ACTION.md and docs/examples/*.yml (no other file: docs/plans and BUILD-PLAN keep old SHAs as history). A new script `.github/scripts/pin-drift.sh`, run by a new workflow `pin-drift.yml` on push to `main` only (and `workflow_dispatch`), finds those pins and writes a `::warning::` line and a job-summary line when the pins differ, when a pin is not an ancestor of HEAD, or when `git log <pin>..HEAD -- src action action.yml skills global.json Directory.Build.props` is not empty (the files the Action builds from; there is no `Directory.Packages.props` or `nuget.config`). It exits 0 in every case. Checkout with `fetch-depth: 0`, `persist-credentials: false`, every `uses:` at a full SHA (the repo requires SHA pins), a job timeout, `contents: read`, and a job name unlike the six required checks | the script in temp git repos, as `WorkflowTests` tests `vulnerable.sh`: current pin quiet, stale pin warns, two pins warn, a pin not on HEAD's history warns; `pin-drift.yml` added to `WorkflowTests.Workflows`; the two runs on `main` |
| U-1 | medium | README.md:34-38 clones PowerOps into the workspace, then runs `analyze` with no path, so PowerOps's own fixtures are analyzed too | Set `po="$(mktemp -d)"` once (POSIX `sh`; the snippet is for any CI system, not only GitHub), clone into `"$po/PowerOps"`, build and install from there, and give `analyze` an explicit solutions path | a docs test that the snippet clones outside the checkout, uses the one variable for the clone, the pack and the install, and names a path |
| S-2 | medium | skills/powerops-plan/SKILL.md:36 and skills/powerops-review-plan/SKILL.md:18 run `plan facts` before the user names an environment, against docs/COMMANDS.md:121 and the MCP `plan_facts` description | `powerops-plan` section 3: ask which environment the plan targets and whether `pac` is signed in to it; run `plan facts` only after the user names it and says yes. If the facts name another environment, say only that they differ, use none of them and write the name nowhere. Without a named environment, plan from repo evidence and add the Assumptions line. The fast path (section 8, step 1 and :157) never runs `plan facts`, since no environment is named there. `powerops-review-plan` runs `plan facts` only when the parent's request says the user confirmed, in that session, that `pac` is signed in to the plan's `environment:`; otherwise it reviews from repo evidence and says so (also its checklist item at :32). The planning skill's review step passes that confirmation when it has it. docs/PLANNING-SKILLS.md (:166-167, :177, :204) says the same. Plugin copies and goldens regenerated | the skill-copy goldens; a docs test that each skill states the condition before its `plan facts` line |
| S-1 | low | SettingsReport.cs:29, SettingsApplyReport.cs:41, :45, :48, :58 and :86, and AnalyzeCommand.cs:92 and :99 print names, messages and the user's path raw: a `LogicalName` of `demo\n::warning::FORGED\u001b[31mRED` printed a `::warning::` line and an ESC | `TextReport.Clean` at those sites and at every other `error:` line that echoes an argument raw: SettingsCommand.cs:83, :158, :164 and :359, SkillsCommand.cs:28, PlanCommand.cs:32, :38 and :72, DriftCommand.cs:129, TestCommand.cs:40, :46-48, :61 and :78 (the reviewer's list; the implementer re-greps) | `settings validate` and `analyze` with a newline and an ESC: no raw control character, no line starting with `::`; and a source scan, like `WriteSitesTests`, that fails on an `error:` interpolation hole not wrapped in `Clean`, with a short allowlist of holes that are numbers or already-clean text |
| U-2 | low | TestCommand.cs:52 and :112 pass only the named path to `SafeWrite`; DeployCommand.cs:117 and :122 pass none, so `test --out` can replace a `*.flowtest.json` it read | Every command that reads files it found passes them, with the files named, to `SafeWrite.Refusal` and `WriteText` (decisions Q2). Each lists them offline before its refusal, which stays before its work and before any tenant read: `analyze` and `baseline` call `SolutionDiscovery.Find` inside the existing `try` first (the zips found), passing the result into the analysis if that is a small change, otherwise accepting the second folder walk; `diff` passes the solution each folder resolved to as well as the paths named (DiffCommand.cs:58); `test` lists its flow test files and solution paths first (TestCommand.cs:59 and :70 move before :52); `deploy plan` adds the refusals of `--save` and `--out` to `problems` right after `ListZips`, before `Prepare`'s `read`, with the zips and settings files of the folder, on the plan path only (`Prepare` is shared with the apply dry run, DeployCommand.cs:219); `deploy apply --out` adds the zips and settings files of the plan's folder (DeployCommand.cs:271). A report written into the scanned folder still works. docs/COMMANDS.md:32 states the new rule: the files a scan found count; the files a reader opens inside an unpacked folder do not | per command, one refused case (exit 2, the target unchanged; for `deploy plan` with zero target reads) and one report into the scanned folder still written |
| X-2 | low | after an import timeout, DeployCommand.cs:354-355 appends "Nothing was imported." to a message that says the import may still be running | `Import()` returns the kind of failure with its text; the timed-out solution's `import` is a new value `unknown`, and the message says its outcome is unknown and to check the import history, then names what was imported before it ("No import before it finished." when none). `status` stays `failed`, exit 3 (decisions Q1). docs/schemas/deploy-apply.schema.json: `unknown` added to the enum and described, "or timed out" dropped from `failed`; docs/COMMANDS.md's deploy apply section names the value | a fake `pac` that times out on the first and on the second zip: text, JSON, `OutputSchemaTests` with a golden; the existing assertions at DeployApplyCommandTests.cs:632, :648, :667 and :705 updated |
| X-1 | low | McpCommand.cs:148 keeps only `error:` lines, so an agent never sees `note: ... a link inside the solution` or `plan status` warnings | A second list keeps the stderr lines that start with `note:` or `warning:`; they go in the last text block, after the stdout JSON (which stays first) and after the `error:` lines. The generic `error: the command failed (exit code N)` still depends on `error:` lines only. Forwarded `pac`, `az` and Web API text is always written as `  | <line>` (DeployCommand.Forward) and stays out | MCP `analyze` on a solution with a link inside returns the note; `Result(3, "", "note: x")` still carries the generic error line; a forwarded `  | note:` line stays out |
| X-3 | low | action/test.sh:23 appends the whole flow-test report to the job summary, past the 1 MiB cap that action/summary.sh enforces | `test.sh` pipes the report to `"$(dirname "${BASH_SOURCE[0]}")/summary.sh"` | an over-1 MiB report through `test.sh` leaves a summary under the cap with the cut line |
| F-2 | low | `deploy gate --help` prints `error: Required argument missing` and exits 2 (a parse error in Program.cs:78-86); with a plan file, `RunGate("-h")` says "the plan file must not start with '-'" | Both paths print one `error:` line that says the gate has no help, because it is a hook and help would exit 0 and let a call through, and points at `docs/COMMANDS.md#deploy-gate`; still exit 2 | the existing theory at DeployGateCommandTests.cs:547-562 asserts the new text, with and without a plan file, for each help alias |
| F-3 | low | AnalyzeCommand.cs:134 and TestCommand.cs:78 print `no solutions found under ..` for `.` | Quote and clean each path, in the quoting style the codebase already uses for a path in a message | an empty folder; OutputHygieneCommandTests.cs:85 updated |
| F-4 | low | a bare `powerops` prints only `error: Required command was not provided.` (Program.cs:78-86) | Print the root help on stderr, then the error line; still exit 2. Only for no arguments at all | `Program.Run([])`: exit 2, stdout empty, the root help first on stderr, then the `error:` line |
| F-5 | low | docs/GITHUB-ACTION.md:128 installs `pac` with no version, :131 and :161 use `<full commit SHA>`, and the examples pin PowerOps at `0000000...` | One style in every snippet, the one docs/examples/promote-dev-to-test.yml already uses: every `uses:` at a full commit SHA with a `# vX.Y.Z` comment (`azure/login@a641126d1b8aa4d1fa005f4f92df94a3a4c4c906 # v3.1.0`, `actions/upload-artifact@cf430e030ddbb5b0abf93d22962f4752f3646cd9 # v7.0.2`), PowerOps at the same commit as the README with one comment, `pac` with `--version "$PAC_VERSION"` and `PAC_VERSION: 2.14.1`. The "replace with the full commit SHA" comments and docs/examples/README.md:88 reworded | a docs test that finds no placeholder SHA and that every PowerOps pin names the same commit (it catches a half update); `ExampleWorkflowTests` keeps one ref across jobs |
| F-6 | low | README.md:11-19 has no update step, and every build packs version `0.0.0`, so a second `dotnet tool install` refuses | An "Update" paragraph of its own (not chained to the install, since `uninstall` fails when the tool is absent): uninstall, then pack and install. The fresh reviewer tried it (SDK 10.0.113, scratch `DOTNET_CLI_HOME` and `NUGET_PACKAGES`, two 0.0.0 builds): `install` and `update --source` both keep the old build, `uninstall` then `install` takes the new one. The implementer re-runs it before documenting, with a scratch `--tool-path` and `NUGET_PACKAGES`, comparing the installed `powerops.dll`'s sha256 with the newer build (`--version` cannot tell them apart: `IncludeSourceRevisionInInformationalVersion` is false) | the run, with the SDK version, recorded in `decisions.md` |

## Automation

Each finding's change is in the Data table. The implementation is split into six parts, each built and tested in its own git worktree from `df04828` and committed there, then cherry-picked onto the M33 branch in this order: (1) S-1, F-2, F-3, F-4; (2) U-2; (3) X-2; (4) X-1 and X-3; (5) S-2; (6) F-1's workflow and the interim pin, U-1, F-5 and F-6. Parts touch some of the same files (`AnalyzeCommand`, `TestCommand`, `DeployCommand`, `docs/COMMANDS.md`); a conflict is resolved on the branch and the full checks run there after the last pick.

## Screens

None. Only CLI text, JSON and docs change.

## Environments and route

None. No step reads or writes a tenant, and no step runs `pac`, `az`, `plan facts` or any `--apply`.

## Solution and publisher

None.

## Constraints

- AGENTS.md: new logic comes with tests; every write goes through `SafeWrite`; `dotnet format --verify-no-changes` stays clean; Conventional Commits; no version or changelog edits; a JSON change follows the schema rules (schema file, `OutputSchemaTests`, goldens).
- The commands, options and exit codes stay as documented, except the additions in the Data table: the `unknown` import value, the help text of a bare run and of the gate, and the refusals of U-2.
- The new workflow is not a required check and runs on no pull request: a PR cannot pin its own merge commit.

## Assumptions

- Adding the value `unknown` to an enum of the `deploy-apply` schema keeps `schemaVersion` 1: nothing has been released, so no consumer exists (decisions Q1).
- `pac` and `az` text reaches stderr only through `DeployCommand.Forward`, which prefixes every line with `  | ` (checked on `df04828`; a grep of `src/` for `warning:` and `note:` finds only PowerOps's own lines).
- The files inside an unpacked solution folder that a reader opens are not compared with the target (decisions Q2): naming one as `--out` stays possible.
- The weekly skills smoke run (`tests/smoke/skills-smoke.sh`, `max_turns` 12) is the check that the new environment question does not push a full-plan scenario past its turns; if it does, the turn limit is raised then. The fast-path scenario does not change.
- A fresh subagent that reviews a plan has no conversation with the user, so it has no environment named in its session unless its parent passes the user's confirmation (decisions Q3).

## Open questions

None.

## Questions for your admin

None.

## Steps

1. This plan, a fresh review, fixes, sign-off.
2. The six parts, each by an implementer subagent in its own worktree, each reviewed.
3. Cherry-pick onto the M33 branch; the checks and the leak scan; the coordinating session re-runs each finding's evidence.
4. The fix PR; CI green on Linux, Windows and macOS; squash-merge.
5. The pin PR: every PowerOps pin to the merge commit of step 4, this plan marked done, the CI results recorded; CI green; merge. Check the warning job's runs on both pushes.

The fix PR also adds a short M33 section to docs/BUILD-PLAN.md after Phase 6 (whose heading stays M22–M32): one milestone row and build-order items 58 (the fixes) and 59 (the pin PR).
