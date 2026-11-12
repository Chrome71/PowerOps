---
status: done
review: done
signed-off: 2026-11-11
signed-off-hash: 085beaa7f6ddd235e1b5c5ebb73cd394ff4859eca6bd5c6f75bd5cc8edb556d4
environment: null
depth: standard
supersedes: null
---

# M88: `drift` reports no false component items after a first import

## Goal

After a clean first managed import of a solution from Dev into Test (trial 3, 2026-11-11, record N9, P7, E113), `drift` exited `1` with four items: a global choice as `component-missing` and `component-extra` (the import gave it a new id, and type 9 is compared by id), a setting definition as `component-missing` (a component of the solution in Dev but not in Test, where Microsoft's own managed definition exists), and one `variable-value` for a value the deployment settings file set. This milestone stops the first three items (the choice twice and the setting definition) from being component items, keeps the fourth (`variable-value`) as drift, and says in `docs/COMMANDS.md` what each now means. It is item M88 of the fix plan after trial 3.

Done when:

1. A pair whose global choices (component type 9) have different ids gives no item, by name and by number, and `notCompared` holds `component type 9 (global choice) not compared: its ids change on import, and drift compares components by id` once, in type order between tables (1) and site maps (62).
2. In a pair compared by name, a component whose definition name is `settingdefinition` (any case) is not compared; it gives no item on either side, and `notCompared` holds one line `component type settingdefinition not compared: Microsoft may own a setting's definition in the target, so a solution can list it in one environment and not another; counted (<position> <environment>: <count>, ...)`, counted in every environment of the pairs compared by name, in the same name order as the other counted lines; the name is printed as the read gave it (its first occurrence), as the other counted lines print theirs, so `component type settingdefinition` is one casing of it, not the rule. The other counted lines keep today's text.
3. A changed variable value still gives `variable-value` (drift), in the same run as items 1 and 2 (the trial's shape: three of its four items go, one stays), and a missing flow still gives `flow-missing` there, as a control that a real component difference still shows.
4. The test `Two_components_with_one_number_but_other_definition_names_never_match` keeps its purpose with another compared definition name in place of `settingdefinition`.
5. `docs/COMMANDS.md`, section `drift`, lists global choices (9) among the types left out by id, gives the setting-definition line and says "a custom setting definition missing from the target now shows only in the count", and says a variable whose value the settings file set shows as `variable-value`, so `drift` exits `1` for it after an import with a settings file, and to check each such item against the settings file.
6. `dotnet test --filter DriftCompareTests` passes; `dotnet build` (0 warnings), `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m88-drift-choices` (0 violations) and the leak scan are clean. The leak scan runs from the main checkout, where the git-ignored `reports/` folder is: `cd "<main checkout>" && bash reports/leakscan.sh origin/main <this branch>`, 0 hits and the control found.

### Won't build

- Comparing global choices by name (a metadata read of `GlobalOptionSetDefinitions`, which a FetchXML read can't make) and a `drift --settings <file>` that treats the file's values as expected: both 1.0.x, as the fix plan says.
- Any change to how a pair compared by number (a version-1 snapshot on one side) treats a setting definition: it has no definition name there, so it stays compared by type number and id, as today.
- Any change to `variable-value`, to `DriftKind`, to the shape of the JSON or snapshot schemas, to the readers (`DriftQueries`, `DriftParsers`, `SnapshotFile`) or to `deploy plan`'s target read.
- Edits to `skills/`, `plugins/`, `.claude/agents/`, `tests/eval/`, the kept roles, the version files or the changelog.

## Users

- A person or agent that runs `drift` between Dev and Test right after a first managed import, and reads its items, its `notCompared` lines and its exit code.
- A CI job that fails on `drift`'s exit code `1`.

## Data

Code is named by member; line numbers are those of `b065f23`. Every changed output line has a test.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | `Dropped` gains `(9, "global choice")` between `(1, "table")` and `(62, "site map")`, so `Keep` leaves type 9 out in both pair kinds (9 is below 10000) and `NotCompared` names it with the existing `TypeLine` text. The comment in `Keep` gains one line: trial 3 (2026-11-11) saw a global choice (9) get a new id on a first managed import. | `src/PowerOps.Core/Drift/DriftCompare.cs:94` (`Dropped`), `:233-235` (the comment in `Keep`) | row 4 (a), (b) |
| 2 | `CountedByName` also holds `settingdefinition` (ignoring case), so `Keep` leaves it out in a pair compared by name and `NotCompared` counts it. Its comment names it. In `NotCompared`, the counted line's middle text is chosen by name: for `settingdefinition`, `Microsoft may own a setting's definition in the target, so a solution can list it in one environment and not another; counted`; for every other counted name, today's `its ids can change on import, so it is counted, not compared by id`. The counts and their format are unchanged. | `DriftCompare.cs:126-128` (the counted line in `NotCompared`), `:222-224` (`CountedByName`) | row 4 (c), (d) |
| 3 | `Two_components_with_one_number_but_other_definition_names_never_match`: `settingdefinition` becomes `customapi`, a name nothing leaves out, so the item reads `customapi <id>` and the `AppSetting` count line stays. | `tests/PowerOps.Core.Tests/Drift/DriftCompareTests.cs:365-373` | itself |
| 4 | Core tests, written first and seen failing before rows 1 and 2. (a) The theory `Types_whose_ids_change_on_import_are_left_out_of_the_component_kinds` gains `[InlineData(9)]`. (b) `Dropped_types_in_a_compared_pair_are_listed_once_in_type_order` gains a type 9 component, and its expected list the line `component type 9 (global choice) ...` after the table line. (c) A new test: a pair compared by name where only Dev (the first) holds a `settingdefinition` component gives no item, and `NotCompared` is exactly the setting-definition line with `1 Dev: 1, 2 Test: 0`; a pair where only Test (the second) holds it gives no item either, and the line with `1 Dev: 0, 2 Test: 1`. (e) A new test: in a pair compared by name (`agents: []` on both), a type 9 component in only one of the two snapshots, either one, gives no item, and `NotCompared` holds the type 9 line. (d) A new test of the trial's shape, compared by name: Dev holds a type 9 component, a component named `SettingDefinition` (that casing), an `appsetting` component, a variable `v` with hash `h1` and a flow; Test holds a type 9 component with another id, an `appsetting` component with another id, no setting definition, `v` with hash `h2` and no flow. The items are exactly `variable-value v` and `flow-missing` with the flow's name, both with `Drift` true, and `DriftReport.HasDrift` is true; `NotCompared` is exactly the type 9 line, then `CountLine("appsetting", "1 Dev: 1, 2 Test: 1")` with today's text, then the setting-definition line naming `SettingDefinition` with `1 Dev: 1, 2 Test: 0`. | `DriftCompareTests.cs` | itself |
| 5 | `docs/COMMANDS.md`, section `drift`. (a) The paragraph on types with new ids (`:371`) gains "global choices (9)"; its reason is observed (trial 3, 2026-11-11), since Learn says nothing on whether a choice's id survives an import. (b) The `notCompared` list (`:385`) reads "(1, 9, 62, 80 and 300, ...)". (c) After the counted paragraph (`:402-404`): a setting definition (`settingdefinition`) is counted the same way in a pair compared by name, with the line of Done when 2; after a first managed import a solution's setting definition was seen listed in Dev and not in Test, where Microsoft's own managed definition exists (trial 3), so a custom setting definition missing from the target now shows only in the count; in a pair compared by number it is compared by number and id. (d) The settings-file sentence (`:379-380`) says: right after an import with a deployment settings file, a variable whose value the file sets shows as `variable-value` against an environment the file didn't set it in, so `drift` exits `1` for it; check each such item against the settings file. The words "and that value's record" go: `Keep` leaves out type 381 (the value record) since the step 1 capture of 2026-10-22 (its comment), so it never shows as a component item. | `docs/COMMANDS.md` | `dotnet test` (no doc test reads these lines; checked by the work review) |
| 5a | `docs/schemas/drift.schema.json`, the `notCompared` description only (`:52`): the list of types left out by id reads "(1, 9, 62, 80, 300, and 10087 where an environment holds no type names)", and the kinds left out by name gain `settingdefinition`, counted because Microsoft may own a setting's definition in the target. Description text only: no shape change, `schemaVersion` stays. The MCP `drift` tool sends agents to this schema (`src/PowerOps.Cli/McpCommand.cs:241`). | `docs/schemas/drift.schema.json` | `OutputSchemaTests` and `McpCommandTests` still pass |
| 6 | `docs/BUILD-PLAN.md`: an `## M88` section in M85's form (one paragraph, one table row, a build-order line) and one decision, numbered at merge under the chain's merge lock: "`drift` leaves global choices out by id and counts setting definitions; `variable-value` stays drift". | `docs/BUILD-PLAN.md` | none (prose) |

No golden holds type 9 or `settingdefinition` (grep of `tests/golden` on 2026-11-11), so no golden changes; the `notCompared` lists exist only in the schema description, `docs/COMMANDS.md` and older plans (grep of the repository on 2026-11-11). The CLI test `A_version_1_file_with_a_dynamic_type_against_a_version_2_file_falls_back_to_number_and_id_in_both_orders` (`tests/PowerOps.Cli.Tests/DriftCommandTests.cs:247`) names a `settingdefinition` on one side of a pair compared by number; row 2 changes only pairs compared by name, so it is unchanged and must still pass. `DriftCompare`'s only callers are `DriftCommand` and `DriftReport` (grep of `src` on 2026-11-11); `Dropped` and `CountedByName` are private, and `deploy plan` uses neither.

## Automation

None.

## Screens

None.

## Environments and route

None. No tenant call: every test runs on built snapshots.

## Solution and publisher

None.

## Constraints

- AGENTS.md: the smallest change that fully solves the task; tests first; `dotnet format --verify-no-changes` clean; fake values only (`demo_` names, `00000000-` ids from the test file's own constants).
- Reuse `Dropped`, `CountedByName`, `TypeLine` and the counted line's format. No new type, option, kind or schema field.
- Output changes, each pinned by row 4, and no other: a type 9 component no longer gives `component-missing` or `component-extra`, and its `notCompared` line appears; in a pair compared by name a `settingdefinition` component no longer gives an item, and its counted line appears.
- Commit type: `fix:` for rows 1 to 4 (it changes `drift`'s output), `docs:` for rows 5 and 6.
- One writer: the executor writes the source, the tests, `docs/COMMANDS.md` and the schema description; the orchestrating session writes this folder and `docs/BUILD-PLAN.md`.
- Chain limits (`reports/trial-3/chain/chain-rules.md`): no tenant call, no `--apply`, nothing under the trial's evidence folder, no version or changelog edit, the frozen roles untouched; push and merge only under the merge lock.

## Assumptions

- Verified on 2026-11-11 by `powerops-researcher` (Learn): component type 9 is "Option Set" in the `solutioncomponent` table's `componenttype` choice ([Solution Component table reference](https://learn.microsoft.com/power-apps/developer/data-platform/reference/entities/solutioncomponent)); `GlobalOptionSetDefinitions` is a Web API metadata path with no `$filter`, read one at a time by MetadataId or name ([Query table definitions](https://learn.microsoft.com/power-apps/developer/data-platform/webapi/query-metadata-web-api)); a setting definition is a solution component that a maker can add to a solution as an existing setting, with "Include setting definition" ([Use settings](https://learn.microsoft.com/power-apps/maker/data-platform/create-edit-configure-settings)). <!-- plan-status: allow -->
- Not on Learn (2026-11-11), so observed only, once, in trial 3 (E113): that a global choice gets a new id on import, and that a setting definition Microsoft owns is not listed as a component of the managed solution after the import. The docs say "seen", not "always".
- The definition name the live read returns for a setting definition (`SnapshotComponent.Name`, the definition's `primaryentityname`) was observed once, in trial 2: M76's plan (`docs/plans/m76-drift-names/plan.md:19`, F40/D5) records type 10090 as `AppSetting` in Test and `SettingDefinition` in Dev, casing as recorded and not verified. The match ignores case. The trial-3 record names the item but does not quote its subject, so a live run is the only check of that item's subject, and it is not part of this milestone (tenant: offline).
- The chain rules (`reports/trial-3/chain/chain-rules.md`), the fix plan and `reports/leakscan.sh` are git-ignored and live only in the main checkout's `reports/` folder, not in this worktree; the sessions read and run them there.
- Ecosystem rule: `docs/ecosystem/compatibility.md` has no row for the `solutioncomponent` read and `changes.md` no open entry for `drift` (read 2026-11-11); the claims above were verified this day.

## Open questions

## Questions for your admin

## Steps

1. **Plan.** This plan and `decisions.md`, the Fable plan review, a fresh-context `powerops-review-plan` into `review.md`, fixes, and the sign-off. Files: `docs/plans/m88-drift-choices/`. Done when: Fable approved, the review has no open finding, and `powerops plan sign-off` ran. Check: `powerops plan status docs/plans/m88-drift-choices`.
2. **Code and tests, tests first.** Rows 1 to 4. Files: `src/PowerOps.Core/Drift/DriftCompare.cs`, `tests/PowerOps.Core.Tests/Drift/DriftCompareTests.cs`. Depends on: 1. Done when: the row 4 tests were seen failing before rows 1 and 2, then `dotnet test --filter DriftCompareTests` and the whole `dotnet test` pass. Check: the failing and passing runs in the executor's report.
3. **Docs.** Rows 5 and 5a. Files: `docs/COMMANDS.md`, `docs/schemas/drift.schema.json`. Depends on: 2. Done when: each line of Done when 5 is in the `drift` section and nothing outside it changed; the schema diff is the one description line; the new `notCompared` line in the docs matches the code string character for character; `OutputSchemaTests` and `McpCommandTests` pass. Check: `git diff docs/COMMANDS.md docs/schemas/drift.schema.json`.
4. **Checks and record.** Row 6 at merge; the checks of Done when 6; the Fable work review; the `decisions.md` Log; `status: done` by hand. Files: `docs/BUILD-PLAN.md`, this folder. Depends on: 2, 3. Done when: every check is clean and the Fable work review passed. Check: the Log lines.
