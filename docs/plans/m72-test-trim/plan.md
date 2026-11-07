---
status: implementing
review: done
environment: null
depth: quick
supersedes: null
signed-off: 2026-11-07
signed-off-hash: d76757e99937e30a1d47897c3c64e25171564f12c34c36c6bb30e11f932ae268
---

# M72: Trim the test suite and pin the kept roles

## Goal

On 2026-11-07, five reviewers sorted the 4,274 test cases. About 85% fail on a real break that nothing else catches. About 12% are padding: Theory rows that take a branch another row already takes, unit tests that assert what a golden already holds, CLI tests that repeat a Core test, wording pins on docs and skills, and counts pinned on third-party fixtures. Two branches have no test, and one AGENTS.md rule has none either: the kept roles' pinned text. The suite runs in about 21 s, so speed is not the aim. The aim is less test code to read and keep current, without losing any test that fails on a real break. The plan's review read each proposed cut against its cover and found that many "duplicates" check something no other test checks (decisions, Log). So this plan makes only the cuts whose cover held up, about 60 cases. The rest of the audit's padding estimate does not stand.

Done when:

1. A test fails when `skills/powerops-verify/SKILL.md`, `skills/powerops-review-security/SKILL.md` or `tests/eval/roles/run.sh` is not the text pinned in `docs/plans/m55-role-freeze/plan.md`, or a later text that `docs/plans/m55-role-freeze/decisions.md` records (decisions Q1). AGENTS.md says where that record goes.
2. `runAfter: ["TimedOut"]` and a surrogate pair at `ImportCall`'s name cut each have a test.
3. Every cut in the Data table is made, or kept with the reason in `decisions.md`. Each commit names, for every test it removes, the test or golden that still fails on the same break. For each group, one mutation of `src/` that a removed test caught is shown to fail its covering test (mutate, run, see it fail, revert).
4. No file in `src/`, `tests/golden/`, `tests/fixtures/`, `tests/eval/`, `docs/schemas/`, `skills/` or `plugins/` changes. Outside the plan folder, only test files and the AGENTS.md line of row 1 change.
5. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m72-test-trim` and the leak scan are clean. The PR, when the person asks for one, says the test lines and the case count before and after.

### Won't build

- No cut in the deploy gate's `ImportCallTests` rows, the `SafeWriteTests` link rows, `DriftLiveReaderTests`' per-read error rows, `RoleEvalTests`' shape checks, the self-tests that show a docs checker can fail, `PlanFactsSkillTests`, the md, txt and sarif goldens, or `tests/eval`. The audit checked each: they guard a safety check or a separate branch, prove a checker can fail, or are the only end-to-end test of a renderer, and AGENTS.md needs the eval set to rerun a kept role.
- No new shared test helper, base class, attribute or config file. A private method inside a test class is fine.
- No change to how fast the suite runs.

## Users

- A maintainer (a person or an agent) who changes a command or a report and has to update the tests that pin it.
- A person who relies on the kept roles staying as evaluated.

## Data

Line numbers are those of `15b8d9d`. "Cover" is the test or golden that still fails on the same break; the build confirms it by reading both, and the mutation check of Done when 3.

| # | Change | Where | Cover |
|---|---|---|---|
| 1 | Add one test. It reads the three rows of the blob-id table in `m55-role-freeze/plan.md` (a path in backticks, then 40 hex digits in backticks), and for each file computes the git blob id of its text with CRLF made LF (SHA-1 of `blob <length>\0<bytes>`, as `git hash-object`). It passes when the id is the pinned one, or one that a log line of `m55-role-freeze/decisions.md` records in this form: ``- yyyy-MM-dd: comparison recorded: `<path>` is now blob `<id>` `` followed by what changed. The same test shows that the check can fail: one changed byte gives another id. AGENTS.md's kept-role rule names that line as the place a typo or formatting edit records its comparison, in that form | `tests/PowerOps.Cli.Tests/RoleEvalTests.cs`; `AGENTS.md:40` | new; the three ids match today |
| 2 | Add a `FlowSimulatorTests` case where an action runs after `A` with `["TimedOut"]` and `A` succeeds, so the action doesn't run, as for any status that `A`'s result doesn't match. The status is accepted at `FlowSimulator.cs:60` and never tested | `tests/PowerOps.Core.Tests/Flows/FlowSimulatorTests.cs` | new |
| 3 | Add an `ImportCallTests` fact that calls `ImportCall.Printable` (public) on 99 `x` and then a surrogate pair, so the pair straddles `MaxName` (100). The cut drops the high surrogate (`ImportCall.cs:98`): the result is the 99 `x`, with no lone surrogate | `tests/PowerOps.Core.Tests/Deploy/ImportCallTests.cs` | new |
| 4 | `addHours`, `addMinutes` and `addSeconds` call the same `Add` as `addDays` (`ExpressionFunctions.cs:76-79`); only the multiplier differs. Cut the rows that repeat an `addDays` row: in `AddHours_AddMinutes_AddSeconds` (`:964`) the rows at `:954`, `:956`, `:958`, `:959`, `:962` and `:963`; in `_with_a_format` (`:972`) `:968` and `:971`; in `_errors` (`:994`) `:976` to `:978` and `:980` to `:987`; in `_with_a_format_is_unsupported_for` (`:1001`) `:998` to `:1000`. Each function keeps a value row with a non-zero step (`:953`, `:955`, `:957`) and a format row (`:967`, `:969`, `:970`), so a wrong multiplier still fails; `:960`, `:961`, `:975`, `:979` and `:997` stay, and so does every overflow and boundary row (`:988` to `:993`), because the overflow depends on the multiplier. Also cut `A_windows_time_zone_name_is_taken` (`:1059`): its five zones are rows of `Every_supported_zone_gives_its_offset_from_its_first_year_to_2030` (`:1180`) | `tests/PowerOps.Core.Tests/Flows/ExpressionTests.cs` | `AddDays` (`:800`), `AddDays_errors` (`:810`), `AddDays_with_a_format` (`:820`), `AddDays_with_a_format_errors` (`:826`), `AddDays_with_a_format_is_unsupported_for` (`:833`), each with its rows above it; `:1180` |
| 5 | `A_string_that_is_not_valid_unicode_is_not_a_plan_file_and_names_no_value` (`:219`) keeps one row, because one `catch` at `DeployPlanFile.cs:33` takes them all | `tests/PowerOps.Core.Tests/Deploy/DeployPlanFileTests.cs` | the row kept |
| 6 | Report tests that assert what a golden or another test holds: `ReportTests` `:84` and `:93`; `DeployPlanReportTests` `:99`, `:162` and `:174`; `DeployApplyReportTests` `:35` and `:60`; `ManifestReportTests` `:23`; `SettingsApplyReportTests` `:78`; `SarifReportTests` `:125` | `tests/PowerOps.Core.Tests/Output/` | `analyze-fixtures.*`; `deploy-plan-seed.*`, with `DeployCommandTests` `:519` and `McpCommandTests` `:355` for `plannedBy`; `deploy-apply.json`, `deploy-apply.txt` and `deploy-apply-dry-run.*` (null `stateAfter` and `appliedAt`); `manifest-seed-v1.txt`; `settings-apply-off.json` (`SettingsApplyReportTests:98`) |
| 7 | CLI tests that repeat a Core test: `DeployCommandTests` `:543` and `:551`; in `DeployApplyCommandTests.An_unreadable_plan_file_exits_3_naming_the_problem_before_the_target_is_read` (`:353`) the rows `not-json`, `not-an-object`, `no-hash`, `empty-target`, `no-inputs` and `unsafe-zip-name` (`bad-hash` stays for the outer `catch` at `DeployCommand.cs:205-209`, and so do `over-1-mb` and `symbolic-link`); `BaselineCommandTests` `:76`; `DriftCaptureTests` `:74`, `:92`, `:105` and `:114` (the `Capture` helper above them stays, because `DriftCommandTests` uses it); `ProgramTests` `:159` | `tests/PowerOps.Cli.Tests/` | `DeployPlanTests` `:846`; `DeployPlanFileTests` `:154` and `:168`, with `bad-hash` for the CLI path; `BaselineTests`; the `drift-seed.*`, `drift-none.json` and `snapshot-seed.json` goldens (`DriftCommandTests` `:102-123`, `:333-342`); `ProgramTests.Another_parse_error_prints_no_help` (`:87`) |
| 8 | Wording pins that guard no rule: the word count of `SkillsCommandTests.Plan_skill_stays_under_its_word_ceiling` (`:436`), whose ASCII-only check stays (the test is renamed for it); `HandoffDocTests.The_page_names_each_command_of_the_flow` (`:64`) and `The_readme_links_the_page` (`:75`). Every other pin in both files stays | `tests/PowerOps.Cli.Tests/SkillsCommandTests.cs`, `HandoffDocTests.cs` | none needed |
| 9 | Counts pinned on third-party fixtures: `XmlUnpackReaderTests` `:358` and `:398`; `ZipReaderTests` `:43` and `:68`; `FixtureRuleTests` `:16`, `:27` and `:70` | `tests/PowerOps.Core.Tests/` | the `analyze-fixtures.*` goldens, which hold every fixture's findings (`ReportTests`); `XmlUnpackReaderTests` `:35-38` (managed); `ZipReaderTests` `:52` |
| 10 | Dead code: `ToolsTests.Bash_on_macOS_is_version_3_2` (`:13`) and the macOS branch and comment of `Tools.cs:3-7`, since macOS was dropped (decision 51); the test skips wherever CI runs | `tests/PowerOps.Cli.Tests/ToolsTests.cs`, `Tools.cs` | none needed |
| 11 | `AnalyzeCommandTests.Json_output_parses_and_has_seventeen_solutions` (`:175`) and any other literal count of the fixture solutions compare with the number that `SolutionDiscovery` finds in `tests/fixtures`, so a new fixture doesn't break them | `tests/PowerOps.Cli.Tests/AnalyzeCommandTests.cs` | the same test |
| 12 | Every helper, field or `using` that a cut leaves unused goes too (for example `XmlUnpackReaderTests.Fixture`, `using System.Diagnostics` in `ToolsTests`), and nothing still used goes | the files of rows 4 to 11 | `dotnet build` with no new warning |

## Automation

None.

## Screens

None.

## Environments and route

None. No tenant read or write.

## Solution and publisher

None.

## Constraints

- AGENTS.md: the smallest change, Conventional Commits (`test:` for the cuts, `test:` or `docs:` for row 1's AGENTS.md line), no version or changelog edits, and no tenant data in commits.
- The kept roles' text, their plugin copies and `tests/eval` don't change. Row 1 only reads them.
- A cut whose cover can't be shown stays, with the reason in `decisions.md`.
- M71 (`docs/plans/m71-m63-follow-ups/`, not yet signed off, in another worktree) will edit `DeployCommand.cs`, `DeployApplyCommandTests.cs` (`:311`, `:866`, new tests), `DeployCommandTests.cs` and goldens. This plan cuts other tests in two of those files, so whichever merges second rebases.
- Platforms are Windows and Linux. Row 1 makes CRLF LF before hashing, so a Windows checkout with `core.autocrlf` gives the same ids.

## Assumptions

- The three pinned ids match today (`git hash-object` gave each, 2026-11-07).
- The covers named in the Data table fail on the same break as the tests cut. The build checks each one, and the mutation check of Done when 3 checks one per group.

## Open questions

## Questions for your admin

## Steps

1. This plan and `decisions.md`, a fresh-context review into `review.md`, fixes, and the person's sign-off.
2. Rows 1 to 3, test first.
3. Rows 4 to 11, one commit per row, each naming its covers, with the mutation check for each group.
4. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m72-test-trim` and the leak scan; then the counts before and after.
