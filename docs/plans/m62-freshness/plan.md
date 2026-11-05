---
status: done
review: done
environment: null
depth: quick
supersedes: null
signed-off: 2026-11-05
signed-off-hash: 748fc2a0db33bc75b496579a6781b888cc2520c7532bace3881088cc94961cae
---

# M62: Report freshness checks

## Goal

Tell a review report that describes the solution on disk from one written for another version of it. This is M62 of the improvement plan written after the live test of 2026-11-04 (P1 block), kept outside the repo because it quotes a private test run. Today `plan status` marks a report stale only when the plan body changed (review finding CX-6): a changed solution keeps `stale: false` without a new review. M55 gave `verify.md` and `security-solution.md` a six-line header that ends `input:` (the solution's path, relative to the repository root) and `solution-hash:` (the digest `powerops manifest` prints), and `plan status` reads both but compares neither (M55 decisions Q5). This milestone compares them, in `plan status` for the solution a report names and in `deploy plan` for the zips it is about to plan.

Done when:

1. `plan status` compares each solution report's `solution-hash:` with the digest of the solution its `input:` names, read with `Manifest.Read` (the manifest digest of an unpacked folder, the SHA-256 of a zip). JSON gains `solutionStale` on each report: `true` when they differ, `false` when they match, `null` when nothing was compared. The text line says `stale (plan) <value>` where it said `stale <value>`, and a solution report's line ends `, stale (solution) <true|false|unknown>`. A legacy report (no valid `solution-hash:` or no valid `input:`) reads `unknown`, with no new warning.
2. A solution report whose hash differs warns `<file> was written for a different version of the solution in '<input>' (stale solution)`. A report with both values valid whose solution can't be compared reads `unknown` and warns why, except a solution that is not there, such as a build output a clone doesn't hold, which reads `unknown` with no warning (decisions Q11). Exit codes don't change.
3. `deploy plan` prints `warning: <zip>: no ready verify.md or security-solution.md under docs/plans has this zip's SHA-256 as its solution-hash` on stderr for each zip in its folder whose SHA-256 is the `solution-hash:` of no report with `state: ready` in `docs/plans/*/` of the repository root: the nearest folder that holds `docs/plans`, starting at the deploy folder and going up, no further than the folder that holds `.git` (decisions Q12, Q15). It prints nothing about reports when no such folder is found. The plan, its hash, its JSON and its exit code don't change. The Action's plan step logs these lines.
4. `PlanCommandTests` cover a changed, an unchanged and a legacy report in text and JSON; the `deploy plan` tests cover a zip with and without a ready report, from the root and from a subfolder; the `plan-status.json` golden shows a compared report; the schema, `docs/COMMANDS.md` and the docs that say `stale` says nothing about the solution are current.
5. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan are clean; the PR is green on all eight checks.

### Won't build

- Settings and test-folder digests. The improvement plan has `plan status` read them "from the header when present", but the M55 header holds no such line, and adding one changes the pinned text of both roles, which this milestone may not touch (decisions Q4).
- A `deploy plan` item, field or Markdown line for an unreviewed zip. An item of a new kind adds a value to the closed `kind` list, which bumps the `deploy-plan` schema version that `deploy apply` and the Action read; the warning goes to stderr, where the MCP server forwards it (decisions Q5).
- A gate. Neither check blocks anything, as the reports gate nothing today.
- Matching a zip with a report by solution name and version, or a managed zip with the unmanaged folder it was exported beside. A match names exactly the bytes a role reviewed (decisions Q9).

## Users

- A person who decides whether to promote a build and reads `plan status` to see whether each report describes the solution now on disk.
- A reviewer of a deploy plan, in the CLI, the MCP tool or the Action's job log, who should know that no ready report reviewed the zip about to be imported.
- An agent that reads `plan status --json` and must not take a report about an older build for one about this build.

## Data

Line numbers are those of `0fb8f3d`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | `PlanReportFile` gains `bool? SolutionStale = null` after `SolutionHash`. `Check` sets it on each report with a non-null `SolutionHash` (the two solution reports) after `ReadReports`, through a new private `SolutionStale`. It is null with no warning when `Input` is null or `SolutionHash` is `unknown`. The repository root is the folder that holds `docs/plans/<name>/`: the plan folder's parent must be named `plans` and its parent `docs` (ignoring case); otherwise null and the warning `<file>: the plan folder is not in docs/plans, so the solution was not compared`. The solution path is the root joined with each `/`-separated name of `Input` (a locator, so it holds no `..`, `:`, `\` or leading `/`). Walking from the root, a name that is a symbolic link gives null and `<file>: input '<input>' passes through a symbolic link, so the solution was not compared`. On Windows, a name that is a reserved device name (`CON`, `PRN`, `AUX`, `NUL`, `COM1` to `COM9`, `LPT1` to `LPT9`, before its first `.`, ignoring case) or ends in `.` or a space is not opened: null and the could-not-be-read warning below (decisions Q13). A missing path gives null with no warning (decisions Q11). Otherwise `Manifest.Read(path)`; the walk, the existence test and the read share one `try`, so a name the file system rejects gives the could-not-be-read warning below, not exit 3 (R11). `Manifest.Read` skips the links inside a folder, as `powerops manifest` does, so the digests agree. An `InvalidDataException`, `IOException` or `UnauthorizedAccessException` (a file that isn't a `.zip`, a cap) gives null and `<file>: input '<input>' could not be read, so the solution was not compared`. Else `SolutionStale` is `digest != SolutionHash`, and true adds `<file> was written for a different version of the solution in '<input>' (stale solution)`. Within one `Check`, each distinct `Input` is read once, and each report warns under its own name (decisions Q14). No message holds a full path | `src/PowerOps.Core/Plans/PlanChecker.cs:26`, `:152` | `PlanCheckerTests`: a folder and a zip, same and changed; a legacy report (no `solution-hash:`, and a free-text `input:`) reads null with only the M55 warning; not found reads null with no warning, so `A_six_line_report_gives_its_input_and_solution_hash_and_review_md_gives_none` (`:222-232`, whose `solutions/demo` and `out/Demo.zip` don't exist) keeps `Assert.Empty(reports.Warnings)`; a link on the way (skipped on Windows, as the other link tests); the reserved-name test; a plan folder outside `docs/plans`; a `.txt` input that can't be read; `review.md` stays null |
| 2 | `PlanChecker.ReadyHashes(string root)` returns null when `<root>/docs/plans` is not a folder, and otherwise the set of `SolutionHash` values (ordinal) of every `verify.md` and `security-solution.md` with `state: ready` and a 64-hex hash in `docs/plans/*/`, read with `ReadReports` (no body hash, warnings dropped, no solution read). The `docs/plans` enumeration moves out of `Discover` into a private `PlansUnder(root, skippedLinks)` that both call, so a `plan.md` in the root (which `Discover` takes as the one plan) doesn't hide `docs/plans` here. It throws `InvalidDataException` as `Discover` does when `docs` or `docs/plans` is a link | `src/PowerOps.Core/Plans/PlanChecker.cs:57-95` | `PlanCheckerTests`: null without `docs/plans`; a ready report counts, a `blocked` one, an `unknown` hash and `review.md` don't; a root with its own `plan.md` still gives the hashes of `docs/plans` |
| 3 | `PlanStatusReport`: the text line prints `stale (plan) <true|false|null>` for every report, and the two solution reports end `, stale (solution) <true|false|unknown>` after `solution-hash`. JSON writes `solutionStale` after `solutionHash` | `src/PowerOps.Core/Plans/PlanStatusReport.cs:29-30`, `:70` | `PlanStatusReportTests`: the text expectations at `:44-50` and `:72` change; `Json_matches_golden` (`:11-30`) gains a `solutions/demo` folder whose digest, computed in the test with `Manifest.Read`, its `verify.md` records, so `plan-status.json` shows `solutionStale: false` and `Assert.Single(plans[2].Warnings)` at `:27` holds |
| 4 | `deploy plan`: after `Prepare` succeeds in `DeployCommand.Run`, the root is the first of the deploy folder's full path and its parents that holds a `docs/plans` folder, and the walk stops after a folder that holds `.git` (the repository's top folder, decisions Q15); with none, nothing is printed. Then `PlanChecker.ReadyHashes(root)`; one `warning:` line (row 3 of Done when, the zip name through `TextReport.Clean`) for each zip of `prepared.Zips` whose `Sha256` isn't in it, in the folder's order. An `InvalidDataException`, `IOException` or `UnauthorizedAccessException` prints `warning: docs/plans could not be read, so the zips were not matched with review reports` instead. Nothing else changes: not the report, the plan file, the hash or the exit code; `deploy apply` and `deploy gate` don't run it | `src/PowerOps.Cli/DeployCommand.cs:99` | A `deploy plan` test class with a temp root that holds `docs/plans` and the deploy folder, so no test changes the current folder: a ready report with the zip's hash gives no warning; a `blocked` one, or a ready one with another hash, gives the line; a deploy folder two levels below the root finds the reports; a `docs/plans` above the folder holding `.git` is not read; a `docs` that is a link gives the could-not-be-read line; the plan's stdout is the same with and without the line; the existing `Assert.Equal("", r.Err)` (`DeployCommandTests.cs:144`), whose temp folder has no `docs/plans` above it, and the MCP `deploy_plan` test's single content block (`McpCommandTests.cs:350`) hold the no-`docs/plans` case |
| 5 | The Action's plan step prints PowerOps' `warning:` lines with its `error:` lines (`grep -E '^(error\|warning):'`); forwarded tool text stays hidden | `action/plan.sh:19` | `ActionScriptTests`: a stub run with a `warning:` line logs it and still hides a forwarded line |
| 6 | Schema: each report gains required `solutionStale`, `boolean` or `null`, with its description; `solutionHash`'s description drops "It is not compared with the solution yet"; `stale`'s description points to `solutionStale` for the solution, and the `reports` description names the stale-solution warning, never a violation. `schemaVersion` stays 1 (an added field) | `docs/schemas/plan-status.schema.json:31-37` | `OutputSchemaTests`; `McpCommandTests.Plan_status_carries_the_reports_list_and_it_matches_the_schema` (`:266-283`) asserts `solutionStale` null for both reports, whose `solutions/demo` doesn't exist |
| 7 | Docs. `docs/COMMANDS.md`: `plan status` describes the comparison, the root, `solutionStale`, the text and each warning, and drops "Neither value is compared"; `deploy plan` gets the warning, where it looks (the root found from the deploy folder), that it changes nothing else, that the Action shows it in the job log only (not the PR comment) and `deploy apply` doesn't repeat it, and that in the HANDOFF route the reports read the unpacked folder while the import takes a managed zip, so the line stays until a role reviews the zip that ships. `docs/USE-WITH-AGENTS.md:15`, `:49`, `docs/PLANNING-SKILLS.md:269`, `:334` and `docs/HANDOFF.md:48` stop saying `stale` says nothing about the solution | those files | `DocsDriftTests` |
| 8 | `PlanCommandTests`: `A_stale_verify_report_is_a_warning_and_a_listed_report_and_the_exit_code_stays_zero` (`:150-170`) expects `stale (plan) true` and `, stale (solution) unknown`; new tests through `plan status` text and `--json`: an unchanged solution (`stale (solution) false`, `solutionStale` false), a changed one (`true`, the warning, exit 0) and a legacy four-line report (`unknown`, `solutionHash` `unknown`, `solutionStale` null, no stale-solution warning) | `tests/PowerOps.Cli.Tests/PlanCommandTests.cs` | itself |

## Automation

The Action's plan step (row 5). Nothing else runs these checks automatically.

## Screens

None.

## Environments and route

None. Both checks are offline: `plan status` reads the repo, and `deploy plan` already reads the target; this adds no tenant read.

## Solution and publisher

None.

## Constraints

- AGENTS.md: smallest change, new logic with tests, no new write (no `SafeWrite` call), Conventional Commits, no version or changelog edits, no tenant URLs, ids, emails or user names in commits.
- The pinned roles `powerops-verify` and `powerops-review-security`, their plugin copies and `tests/eval` are untouched; no skill text changes. The eval baselines record `analyze`, `test`, `diff` and `settings validate`, which this doesn't change.
- Platforms are Windows and Linux. `Manifest` hashes text with CRLF as LF, so a folder checked out on either gives one digest. Nothing else is evened out: `input:` is matched as written, so on Linux its case must match the folder's, and a zip rebuilt from the same source is other bytes.
- Other P1 sessions run at the same time (M59, M60, M61); this touches only the files above.

## Assumptions

- The roles write `input:` relative to the repository root and `solution-hash:` as the digest `powerops manifest <current solution>` prints (M55 plan, lines H5 and H6), and the plan folder is `docs/plans/<name>/` (the plan skills write it there, and `plan status` discovers it there).
- `deploy plan` and the MCP server run from the repository root: the Action runs in the workspace, and the docs show `deploy plan deploy` from the root.
- The SHA-256 `deploy plan` records for a zip equals the digest `Manifest.Read` gives it (`ManifestTests` holds this since M55).

## Open questions

## Questions for your admin

## Steps

1. This plan and `decisions.md`, a fresh-context review into `review.md`, fixes, and sign-off under the delegation of 2026-10-22 once no High or Medium finding is open.
2. Rows 1 to 3, test first; then rows 4 and 5; then row 6 and the goldens by the recipe "Update goldens".
3. Row 7.
4. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .`, the leak scan; push, PR, CI.
