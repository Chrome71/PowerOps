---
status: done
review: done
environment: null
depth: quick
supersedes: null
signed-off: 2026-11-12
signed-off-hash: e3907ba342584813b13a74697c589bcd57c7690cadc209fb1b7ae39715cbb069
---

# M98: `deploy apply` reads back what it installed

## Goal

`powerops deploy apply --apply` reports a plan with no cloud flows as `imported`, exit `0`, on `pac`'s exit code alone: the second read of the target runs only when the plan has flows (`DeployCommand.Apply`, the `flowRefs.Count == 0` return). Practitioners report `pac solution import` exiting `0` after a failed import, and an agent reads exit `0` as done. This is milestone M98 of the upgrade plan drawn from the practitioner research (candidate 4); the plan's M98 section is the authority for the change.

Done when:

1. After an `--apply` run in which at least one import ran and every import went through (`ok`), `deploy apply` reads the target once, flowless plans included. That read is the second read it already makes for flows: `read(...)` returns a `Snapshot` per solution of the zips with `Installed`, `Version` and `Managed` (`src/PowerOps.Core/Drift/Snapshot.cs`), and `DriftLiveReader` returns `Installed: false` for a solution that is not in the environment. No new kind of query, but for a flowless plan this is new cost: the read is the full target read `deploy plan` makes (`DriftLiveReader.ReadTarget`: each solution's components, flows, variables, references, up to 50 layer reads per managed solution, and each dependency), and any failure in it now gives exit `3` after good imports (Done when 5). A narrower read is not built (Won't build). A run that imported nothing (every solution `skip`) reads nothing, as today.
2. Each solution row of the report gains `after`, the last property of the row: `null` when not read, else `{ "installed": bool, "version": string|null, "managed": bool|null }`, taken from that read. `version` is the read version only when it parses as four numeric parts (the parser behind `DeployPlan.EqualVersions`), else null, so no other text from the target reaches the JSON (R5). It is set only on rows whose `import` is `ok`; every other row (`skipped`, `planned`, `not-run`, `failed`, `unknown`) has `after: null`.
3. A row whose `after` shows the solution not installed, or at another version than the zip's (`DeployPlan.EqualVersions`, so `1.0.0.0` equals `1.0.0.00`, and a null or unparsable version is "another version"), makes the run exit `1` with status `imported` and the message `pac reported success, but the target shows <solution> not installed; check the solution's import history.` or `pac reported success, but the target shows <solution> at version <v>; check the solution's import history.` (`<v>` is `after.version`, or `unknown` when it is null, which includes a version that doesn't parse). With two or more such rows, one sentence each, in plan order, joined by one space. `managed` is reported and not compared (decisions Q3).
4. When flows are in the plan, the flows and the versions come from that same one read. Exit `1` when a flow is not in its expected state or a version differs; the message is the version message when a version differs, else null as today. The text summary then reads `applied: <message>` (a version mismatch) or the flow sentence (flows only), as `DeployApplyReport.Summary` already does.
5. A failed read after good imports gives today's report and exit `3` (`The imports succeeded. Reading the target again failed...`), now for flowless plans too, with every `after` null; the agents are still read and stderr keeps its order. A later import that failed or timed out makes no read, as today, and leaves the earlier `ok` rows' `after` null.
6. The text report adds, right after each solution line whose `after` is set, the line `  after: <version> (managed|unmanaged)`, or `  after: not installed`; `version not read` stands for a null version and `managed not read` for a null `managed`. Every value goes through `TextReport.Clean`.
7. `docs/schemas/deploy-apply.schema.json` declares `after` in each solution item (required, `["object","null"]` with `installed`, `version`, `managed` all required), with its own description (null unless the row is `ok` and the read ran; `version` is the target's, compared with the zip's, null when it doesn't parse; `managed` is reported, not compared); the descriptions of `status`, `message` and the `solutions` array name the version check; and the schema's top-level `description`, which lists what the report holds, adds each imported solution's version and managed flag as read from the target. `schemaVersion` stays 1 (an added field keeps it, `docs/schemas/README.md` Versioning; M89 did the same).
8. Docs name the version mismatch: `docs/COMMANDS.md` `deploy apply` ("What it does", Output and the exit-code line), saying that the read is the full target read and that a completed import leaving the zip's version was not yet observed in a tenant, so a mismatch is a prompt to check the history and a wrong one would be a false exit `1`, never a false exit `0`; `docs/RECOVERY.md` rows `0`, `1` and `3` (row `1` sends a version mismatch to [Check the solution history](#check-the-solution-history), not to HANDOFF step 10; row `3`'s `imported` case no longer speaks only of flows); `action.yml`'s exit-1 message becomes `powerops deploy apply refused, a flow is not in its expected state, or the target doesn't show the imported version`, held by a test in `tests/PowerOps.Cli.Tests/ActionYmlTests.cs`; `docs/GITHUB-ACTION.md`'s exit-1 description ("1 refused or a flow not as expected"). A new decision in `docs/BUILD-PLAN.md`, numbered at merge, names both exit changes (a version mismatch exits `1` instead of `0`; a failed read after a flowless import exits `3` instead of `0`), the new cost of the flowless read, and that the version claim is unobserved, with its failure direction.
9. The tests of Data row 5 are written first and seen to fail, then pass. The `deploy-apply*` goldens are regenerated by the "Update goldens" recipe and their diff holds only the `after` fields (and the `after:` text line in `deploy-apply.txt`).
10. `dotnet build` (0 warnings), `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m98-apply-readback` and the leak scan are clean; `tests/eval/`, `skills/powerops-verify`, `skills/powerops-review-security` and their plugin copies are byte-for-byte unchanged.

### Won't build

- A new or narrower tenant query, a read of the import history (`importjob`), or polling: the read stays the one second read.
- Comparing `managed`, layers, components or anything but presence and version.
- A read when nothing was imported, or after a failed or timed-out import (the agent read-back keeps its own rules).
- A `schemaVersion` bump, a change to `deploy plan`, `drift`, `settings apply`, any skill, specialist or plugin file, or the MCP tool's description.

## Users

- An agent or person that reads the apply report or its exit code after `--apply` and decides whether the deployment is done.
- A GitHub Actions workflow that runs the composite action's apply and fails the job on a non-zero exit.

## Data

Line references are names at `e664e7f`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | `public sealed record SolutionAfter(bool Installed, string? Version, bool? Managed)`; `AppliedSolution` gains `SolutionAfter? After = null` as its last parameter (the default keeps the existing positional calls) | `src/PowerOps.Core/Deploy/DeployApply.cs` | row 5 |
| 2 | `DeployApply.SolutionsAfter(IReadOnlyList<AppliedSolution> rows, IReadOnlyList<DeployZip> zips, IReadOnlyList<Snapshot> after)`, beside `FlowsAfter`: returns the rows with `After` set on each `ok` row from the snapshot of its solution (matched by unique name, ignoring case; no snapshot counts as not installed), and the mismatch message of Done when 3 or null. The zip's version is the version of the zip the row names (`row.Zip`), compared by `DeployPlan.EqualVersions` | `src/PowerOps.Core/Deploy/DeployApply.cs` | row 5 (Core) |
| 3 | `Apply`: after the import loop, when no import failed and at least one ran, make the second read whether or not the plan has flows; on success call `SolutionsAfter`, and for flows `FlowsAfter` on the same snapshots; exit `1` when either is off, else `0`; message per Done when 4. The read's catch is today's (exit `3`, every `after` null, flows `planned`). The `imports.Count == 0` case keeps today's return with no read | `src/PowerOps.Cli/DeployCommand.cs` (`Apply`, the returns after the import loop) | row 5 (CLI) |
| 4 | `WriteText` writes the `after:` line of Done when 6 after its solution line; `WriteJson` needs no change (the record carries `after`). Schema per Done when 7 | `src/PowerOps.Core/Output/DeployApplyReport.cs`; `docs/schemas/deploy-apply.schema.json` | row 5; `OutputSchemaTests` with the goldens |
| 5 | Tests, written first. `DeployApplyCommandTests` (fakes: `FakeTarget.After` for the second read, `FailAfter` for its failure): (a) flowless plan, the read shows the zip's version: `after` set (`installed`, `version`, `managed`), exit `0`, no message, and the read is made (`Calls` counts two); (b) flowless, the solution missing after: exit `1`, status `imported`, the exact not-installed message, text summary `applied: ...` and the `  after: not installed` line; (c) flowless, a lower version after: exit `1`, the exact `at version` message; (d) flowless, `FailAfter`: exit `3`, message starting `The imports succeeded.`, every `after` null; (e) a staged upgrade (managed upgrade, `--stage-and-upgrade` in the pac call) whose read shows the zip's version: exit `0`; (f) dry run: every `after` null, no second read; (g) every solution skipped: no second read, exit `0`, `after` null; (h) a plan with flows whose flows are as expected but a version differs: exit `1`, the version message, flows reported; a plan with flows at the zip's version and a flow off: exit `1`, null message, the flow sentence as today; (i) two solutions, both mismatched: two sentences in plan order; a failed second import: the first (`ok`) row's `after` is null and no read is made beyond today's. (j) a flow off and a version mismatch together: exit `1`, the version message, the flow rows listed; (k) a hostile version from the read (not four numeric parts): `after.version` null and `unknown` in the message, in JSON and text. Existing tests: `SeedAfter` returns version `1.0.0.1` (the seed zip's); every other existing apply test whose second read would now show the pre-import state gets an explicit `After` at its zip's version, in that test, and keeps every assertion (the agent tests included, those built on `new FakeTarget()` and the one whose `After` holds only `Flows`). No default in `FakeTarget` or the MCP fakes returns the zip's version by itself (Fable WATCH). The skip-only test keeps `Calls` at 2. `DeployApplyReportTests`: the `after:` line text for installed managed, unmanaged, not installed, null version and null managed, a hostile version cleaned, and the JSON order with `after` last in a row. Core `DeployApplyTests` (or the file that tests `FlowsAfter`): `SolutionsAfter` for equal (`1.0.0.0` vs `1.0.0.00`), lower, null version, missing snapshot, a `skipped` row left null. `McpCommandTests`: the reads of `ImportFakes` and `CliApply` made after an import has run (the pac fake was called, not a read count: through the form the target is read twice before any import) return the zip's version (1.0.0.1), so the existing `--apply` calls stay on the good path and the swap and changed-target tests still refuse with nothing imported; a new test calls `deploy_apply` through the form and asserts `after` equals `{ installed: true, version: "1.0.0.1", managed: true }`, `message` null and `IsError` false; one more case whose second read shows the old version asserts the exact `at version 1.0.0.0` message, with `IsError` false as `McpCommand.Result` gives for exit 1 today. `ActionYmlTests`: the exit-1 case line of the `Apply plan result` step is the new message. Goldens `deploy-apply*` by the recipe | `tests/PowerOps.Cli.Tests/DeployApplyCommandTests.cs`, `DeployFakes.cs` (`SeedAfter` lives in the test file; the fake only if a setter is needed), `McpCommandTests.cs` (its fakes and tests), `ActionYmlTests.cs`; `tests/PowerOps.Core.Tests/Output/DeployApplyReportTests.cs`, the Core test of `DeployApply`; `tests/golden/deploy-apply*` | itself |
| 6 | Docs of Done when 8 and the decision | `docs/COMMANDS.md` (`deploy apply` only), `docs/RECOVERY.md` (rows `0`, `1`, `3`), `action.yml` (the exit-1 line only), `docs/GITHUB-ACTION.md` (the exit-1 parenthesis), `docs/BUILD-PLAN.md` (one decision, numbered at merge) | none (prose); `ActionYmlTests` |

## Automation

None.

## Screens

None.

## Environments and route

None. Nothing in this milestone reads or writes a tenant; every test uses the fakes of `DeployFakes.cs` and the MCP tests' fakes.

## Solution and publisher

None.

## Constraints

- AGENTS.md: the smallest change, new logic with tests, no new dependency, writes only through `SafeWrite` (unchanged), no version or changelog edit, no tenant value in Git, Conventional Commits.
- The chain rules and their "Upgrade milestones" section: tenant offline; `tests/eval/`, the kept roles and their plugin copies untouched and `RoleEvalTests` passing; one pull request at a time under the merge lock. M98 changes no `analyze`, `test` or `diff` output, so the M96 marker rule doesn't bind its merge.
- Shared files: `docs/COMMANDS.md` (only the `deploy apply` section), `docs/BUILD-PLAN.md` (one decision, numbered under the lock), the `deploy-apply*` goldens; M106 follows in the same lane (`Deploy/*`, `DeployCommand.cs`).
- The report never holds the target's id or URL: `after` holds a version and two booleans of the solution the plan names.

## Assumptions

- unproven: a completed staged upgrade, or a completed plain import, leaves the solution at the zip's version (Learn says the upgrade "upgrades your solution to the latest version" but no page states the row's version; decisions, research 1 and 2); probed by step 6. No build step depends on it, because every test uses fakes; it decides only whether a good import can be reported as a mismatch, which would be a false exit `1` (the Action fails its job after an import that stands) and never a false exit `0`.
- The leak scan is `reports/leakscan.sh`, an untracked local tool of the main checkout; it runs from there against the committed branch (`bash reports/leakscan.sh origin/main <branch>`).
- The installed `powerops` may be stale; `plan status` and `plan sign-off` run with this worktree's build (`dotnet run --project src/PowerOps.Cli --`).
- The second read already returns presence, version and managed for every solution of the zips (`DeployCommand.Apply` passes the unique names of `now.Zips`; `DriftLiveReader` builds `Installed: false` for an absent one): checked in the code at `e664e7f`.
- That `pac` exits `0` after a failed import is reported by practitioners (two GitHub issues outside Microsoft), not documented; the research with sources and read dates is in `decisions.md`.
- `EqualVersions` treats an unparsable version as unequal, so a target that shows a version powerops can't parse is reported as a mismatch: a false alarm sends the reader to the history, never a false "done".

## Open questions

## Questions for your admin

## Steps

1. This plan and `decisions.md`, the claims research, the Fable plan review and a fresh-context `powerops-review-plan` into `review.md`, fixes, and the sign-off.
2. A baseline `dotnet build` and `dotnet test` at `e664e7f`, recorded; the `SolutionAfter` record and an empty `SolutionsAfter` so the project compiles; then row 5's tests, run and seen to fail on their assertions (not on a compile error).
3. Rows 1 to 4 and the one-line `action.yml` edit, until every test passes; then the goldens by the recipe "Update goldens", reading the diff.
4. Row 6's other docs, worded against the code of step 3.
5. The checks of Done when 9 and 10, the Fable work review, and the merge under the chain's protocol.
6. The probe of the unproven line, after this milestone: trial 4's R-readback line (upgrade plan section 8) records, for each imported solution, the apply report's `after` against the zip's version; a false mismatch there reopens this design. Not part of this milestone's Done when.
