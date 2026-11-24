---
status: done
review: done
signed-off: 2026-11-03
signed-off-hash: cb993bc0cd68aa99027f2805ee1588d9863d06dd725e6d9837162a6795ba407e
environment: null
depth: quick
supersedes: null
---

# M47: Code app dependencies

## Goal

Warn in `analyze` when a code app (`CanvasAppType` 4) names a table or connector in its `CdsDependencies` that neither the solution nor a declared dependency holds. This is M47 of Phase 7 ([plan](../phase7-power-platform/plan.md), build-order item 82, release label v0.30.0), with its "Stops if" rule and decision 49 (D10) for inputs an export doesn't hold. F3 (a code app through `pac solution unpack` in Dev) has not been read, so the rule rests on public exports (Assumptions).

Two departures from the M47 section, each with its reason in `decisions.md`. The rule reads `CdsDependencies` only, not `DatabaseReferences`: in the public exports the second field named no table the first didn't, and its one extra data source is no table the solution could hold (Q9). It doesn't check connection reference entries, the case of the issue that motivates the section: no export shows one, so when a code app's `CdsDependencies` names a component type the rule doesn't read, those entries say "not checked" and its tables and connectors are still checked (Q10, Q11).

PO026 is a warning, as the M47 section sets, and not D10's Error: an export of one solution can't show that a sibling solution, imported first by a package, will be in the target, and import fails only if it isn't. The public result is two warnings on one of three independent code app solutions, a layered design (decisions Q7); a warning changes no exit code by default and can be baselined.

Done when:

- `CanvasAppMeta` carries the metadata's `File` and the `Tables` and `Connectors` its `CdsDependencies` names, read from the canvas metadata the parser already loads (a `CanvasApps/*.meta.xml` in a folder, a `CanvasApp` element of `customizations.xml` in a zip). Text that is missing or isn't in the observed shape gives null lists and never stops a command. The existing rules give the same findings on every solution as before.
- **PO026, Warning:** for each code app, one finding per table whose name has a customization prefix (an underscore) and per connector that no root component of that type, and no `MissingDependencies` `Required` node, names. The message names the app as a code app. A code app whose `CdsDependencies` can't be read gives one `note:` line on stderr instead, and one whose entries include other component types gives one `note:` line naming them besides its findings; neither changes the exit code (D10).
- Firing and quiet tests for the rule, reader tests for the folder and the zip layout, and one CLI-level test per layout, all with metadata built by hand. A page with a dated Learn Sources line and a row in `docs/rules/README.md`.
- No fixture and no eval case holds a code app, so the goldens change only in the rule lists (Data row 9). `RoleEvalTests` passes with no eval file changed, and `powerops-verify` and `powerops-review-security` keep their blob ids (`69208a2ed044a346bbae9877c8cd74d9e4794df9` and `6a6a6a1fdb71eb16d09cf3ed99406d04a0982a1f`).
- `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan are clean, and one `feat:` PR is open.

### Won't build

- Code apps as a kind in `analyze` or `diff` output, a 500 kB chunk rule, a `CodeAppPackageUris` check against files, source checks on `power.config.json` or TypeScript, and anything that runs `pa` (the M47 section's "Deliberately doesn't").
- `DatabaseReferences` as a source of tables (decisions Q9).
- Connection reference entries of `CdsDependencies`, the case of issue #342 in microsoft/PowerAppsCodeApps: no public export shows their shape. Entries of any type other than 1 (table), 372 (connector) or 29 (workflow, observed in `alm-sample`'s canvas app) give a note naming their types, and the app's tables and connectors are still checked (decisions Q10, Q11). Workflow entries are not checked.
- Tables without a customization prefix (no underscore), such as `systemuser` or `team`: Learn says standard tables are part of the system solution and every environment is expected to have them, and that a custom table's name has a prefix and an underscore (decisions Q4). Dynamics 365 app tables without a prefix, such as `incident`, are skipped too, and the page says so.
- The YAML layout (`solutions/<name>/`): its reader doesn't go through `SolutionParser`, and Learn's ALM page for code apps says they don't support source code integration, so that layout doesn't hold a code app.
- A check of canvas apps, custom pages or component libraries: the M47 section names code apps only.
- A new fixture: the SDK, CLI and Vite plugin are under Microsoft Software License Terms, and an exported `iconUri` carried a signed URL, so public exports are read for shape only and tests build the metadata by hand.
- A `--json` or SARIF field for the note, or any schema change.

## Users

- A maker or agent who runs `analyze` on an exported or unpacked solution that holds a code app, before it goes to Test or Production.
- CI: the Action runs `analyze` with `--fail-on error` by default, so PO026 changes no exit code there; a job set to `fail-on: warning` fails on it until it is fixed or baselined. A `note:` line on stderr changes no exit code.
- An agent through the MCP `analyze` tool, which passes stderr `note:` lines on.
- Every command that loads a solution through the two XML readers (folder and zip) now parses the `CdsDependencies` JSON of each canvas metadata. It never refuses on that text, so no command that loads a solution today stops on it.

## Data

Line numbers are those of `c4dcf52`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | `CanvasAppMeta(string Name, int? Type, IReadOnlyDictionary<string, string>? Connections, string File, IReadOnlyList<string>? Tables, IReadOnlyList<string>? Connectors, IReadOnlyList<int>? OtherTypes)`. `File` is the metadata's path (`CanvasApps/<name>.meta.xml`, or `customizations.xml` in a zip). `Tables` and `Connectors` hold logical names, trimmed, distinct ignoring case with the first kept, in the order read; `OtherTypes` the distinct component types of entries other than 1, 372 and 29, in the order read (empty when there are none). All three are null when `CdsDependencies` isn't in the shape of row 2. The parameters are required, so `B.Canvas` and `B.CanvasUnreadable` pass a file and empty lists | `src/PowerOps.Core/Model/Solution.cs:82-83`; `tests/PowerOps.Core.Tests/Rules/Builders.cs:76-80` | row 3 |
| 2 | `SolutionParser`: `CheckedMeta` returns the path with the text and element, and the `customizations.xml` `CanvasApp` elements carry `customizations.xml`, so `ReadCanvasApp` gets the file. It reads `CdsDependencies` for every canvas metadata whatever its type (decisions Q2): a JSON object with a `cdsdependencies` array of objects, each with an integer `componenttype` (read with `TryGetInt32`, so `1.0`, `1.5` and `1e10` aren't integers). Type 1 is a table and 372 a connector, named by a non-empty string `logicalname`; type 29 (a workflow) is skipped; any other type goes into `OtherTypes`, and the other entries are still read (decisions Q11). Anything else gives null lists: no text, not JSON, not an object, no `cdsdependencies` property or one that isn't an array, an entry not an object or without an integer `componenttype`, a type 1 or 372 entry without its name, or a `JsonException` or `InvalidOperationException`. Nothing is refused (decisions Q6). `DatabaseReferences` is not read (decisions Q9) | `src/PowerOps.Core/Readers/SolutionParser.cs:90-107,204-207,453-464` | row 3 |
| 3 | Reader tests with `TempFolder` and `SolutionZip`, in a new `tests/PowerOps.Core.Tests/Readers/CodeAppReaderTests.cs`: metadata built by hand in the observed shapes with fake names. A folder `.meta.xml` and a zip `CanvasApp` element give the same `Name`, `Type` 4, `Tables` and `Connectors`, with `File` `CanvasApps/<name>.meta.xml` and `customizations.xml`. `CdsDependencies` with a table, a connector carrying `componenttypeplatformname`, a type-29 entry with `dependencyid` only (skipped), and one name twice in different case (listed once); an empty `cdsdependencies` array (empty lists); a `DatabaseReferences` whose data sources include a name no entry of `CdsDependencies` has (not a table). Null lists for: no `CdsDependencies` element, an empty one, text that isn't JSON, a JSON array, `{}`, `cdsdependencies` as an object, an entry that is a string, an entry with `componenttype` as a string, missing, `1.0`, `1.5` or `1e10`, a type 1 entry with no `logicalname` or an empty one, a type 372 entry with no `logicalname`, 100 levels of nesting inside an entry of an otherwise valid object (so the depth limit, not the root shape, refuses it), and a `logicalname` of `\ud800`. Every such solution still loads. Entries of types 10001 and 10000 beside a table give `OtherTypes` `[10001, 10000]` (a repeat listed once, in the order read, not sorted) and keep the table | new file | itself |
| 4 | PO026, `Warning`, `IRule` and `INotChecked`. Held tables are the `schemaName`s of root components of type `1`, held connectors those of type `372`, and both sets add `RuleHelpers.DeclaredNames`, each built once per solution and compared ignoring case (decisions Q5). For each `CanvasAppMeta` with `Type` 4 and non-null lists, in ordinal order of `File` then `Name`: one finding per table that has an underscore and isn't held, then one per connector that isn't held, whatever its name. A code app with null lists gives the first note; one with a non-empty `OtherTypes` gives the second, besides its findings. Title, message, fix and note are in the table below | new `src/PowerOps.Core/Rules/PO026CodeAppDependencyMissing.cs`; `src/PowerOps.Core/Rules/Analyzer.cs:33` | row 5 |
| 5 | Tests in `WarningRuleTests` with a new `B.CodeApp(name, tables, connectors, type = 4, otherTypes)` builder. Fires for a missing table and a missing connector, with the message, component and file; fires for a missing connector whose name has no underscore; quiet for a table held as a type-1 root component (in another case), a connector held as a type-372 root component, and each declared by `schemaName` in `MissingDependencies`; fires for a table named only by a type-372 root component, and a connector named only by a type-1 one; quiet for `systemuser` (no underscore); quiet for the same missing names in apps of type 0, 2 and null; the first note for a code app with null lists, and none for a type-2 app with null lists; a code app with a missing table and `OtherTypes` `[10001, 10000]` gives the finding and the second note with the types in that order; seven other types give a note naming the first five and "and 2 more" (review R14); two code apps with the same missing table give two findings, in `File` order; 100,000 missing tables against 100,000 root components and 100,000 declared names run within a time bound, as `WarningRuleTests` bounds M45 and M46 | `tests/PowerOps.Core.Tests/Rules/WarningRuleTests.cs`, `Builders.cs` | itself |
| 6 | `alm-sample` in `FixtureRuleTests`: its one canvas app has `Type` 0, `Tables` empty and `Connectors` `cat_5Fenvironmentvariableconnector` (its four type-29 entries skipped), and no PO026 finding or note | `tests/PowerOps.Core.Tests/Rules/FixtureRuleTests.cs:143-160` | itself |
| 7 | CLI tests in `AnalyzeCommandTests`: a temp copy of `alm-sample` whose canvas metadata gets `CanvasAppType` 4 and a type-1 entry `demo_missingtable` in its `CdsDependencies` exits 0 with one PO026 finding, `File` `CanvasApps/cat_devopskitsamplecanvasapp_c7ec5.meta.xml`; the same copy packed by the existing `PackedLikePac` gives the same message with `File` `customizations.xml`. The copy with only `CanvasAppType` 4 changed gives no PO026 finding (its connector is a root component, its workflow entries are skipped). A copy with `CanvasAppType` 4 and `CdsDependencies` `[]` prints the PO026 note on stderr and gives the same exit code and stdout as the plain fixture | `tests/PowerOps.Cli.Tests/AnalyzeCommandTests.cs:409-478` | itself |
| 8 | `docs/rules/PO026.md` in the recipe's sections, its Sources line verified on the day it is written; a row in the rule table. The page names the input it reads and the exact notes; which entries it skips (workflows, tables without a prefix, Dynamics 365 app tables among them) and which give a note; that it doesn't read `DatabaseReferences` and why; that it keys on `CanvasAppType` 4, which every observed code app has (Learn also lists a `CanvasAppSubtype` column none of them carries); that its shape comes from public exports and not from Learn or an unpack in Dev (F3); that no export declares a connector as a `Required` node, so that case rests on a hand-built test; and that a solution whose tables are in a sibling solution with no declared dependency gets the warning (decisions Q7) | new page; `docs/rules/README.md:31` | `RuleDocsTests`, `DocsDriftTests` |
| 9 | Goldens by "Update goldens": `rules.txt`, `rules.json` and the `analyze-fixtures.sarif` rule descriptors gain PO026. Nothing else changes: no fixture holds a code app | `tests/golden/` | `ReportTests`, `RulesCommandTests`, `OutputSchemaTests` |
| 10 | `docs/COMMANDS.md`: the `analyze` paragraph says the dependencies of a code app are read from its canvas metadata's `CdsDependencies` in an unpacked folder or a zip, not from the YAML layout (PO026) | `docs/COMMANDS.md:37` | `DocsDriftTests` if it covers it |
| 11 | `docs/BUILD-PLAN.md`: M47 marked built in the Phase 7 table and item 82, with the plan link, and an M47 line in the verification record after M46's | `docs/BUILD-PLAN.md:150,532,549` | none, docs only |

The rule (row 4). The message is one sentence; `<…>` are values.

| Rule | Title | Message (component, file) | Fix |
|---|---|---|---|
| PO026 | Code app depends on a table or connector that is not in the solution | `Code app '<app>' depends on table '<name>', which is not in the solution or its dependencies.` and `Code app '<app>' depends on connector '<name>', which is not in the solution or its dependencies.` (the table or connector, the app's file) | Add the table or connector to the solution, or add the solution that holds it as a dependency. |

The notes: `PO026 not checked for code app '<app>': its CdsDependencies is missing or not in a shape PowerOps reads.` and `PO026 not checked for code app '<app>': its CdsDependencies has entries of other component types (<n>, <m>), which are not checked.`, which names at most five types and then `and <k> more`

Measured before the build, with a scratch script outside the repo: every eval case and fixture with canvas metadata holds `CanvasAppType` 0 (the eval cases' `pops_workitems_2af60`, `powerops-seed`, `alm-sample`), so PO026 can't fire or note on them. On the public exports (Assumptions), rows 2 and 4 give no finding and no note on the two Release Planner zips and the agentic business process export, and two findings on TALXIS's `Solutions.CodeApp` (decisions Q7). Reading `DatabaseReferences` as the first draft of this plan did would have added one false finding on the agentic export (review R1).

## Automation

None. The rule runs inside `analyze`, with no new command, option or tool.

## Screens

None.

## Environments and route

None. No step reads or writes a tenant. F3 needs a code app in Dev, which needs the admin toggle the Phase 7 plan's admin question asks about, and the person's yes; it is not part of this build.

## Solution and publisher

None.

## Constraints

- AGENTS.md: the "Add a rule" and "Add a reader" recipes, tests first; no dependency; no write site; fake values in tests; no version or changelog edits; Conventional Commits; `dotnet format` clean.
- Readers: the metadata is already read under `ReadBudget` (`CheckedMeta`); the JSON is parsed with `JsonDocument`'s default depth limit; nothing new is opened.
- No eval baseline may change. If the new rule fires on an eval case anyway, the build stops and records here what D1's ruling requires (a rerun of the affected passes, costed) before going on.
- No change to `powerops-verify` or `powerops-review-security`.
- Nothing from the public exports goes into the repo: no names, ids, URLs or files.

## Assumptions

- Learn, checked 2026-11-03. The CanvasApp table: `CanvasAppType` 4 is "Code App"; `CdsDependencies` is "Internal use. The app dependency details."; `DatabaseReferences` is "The database references of the application."; a `CanvasAppSubtype` column is "The subtype of Canvas apps, especially, that of Code Apps". Customize table definitions: custom tables must have a customization prefix, and an underscore must be included between the prefix and the name. Tables and metadata in Microsoft Dataverse: standard tables are part of the system solution and every Dataverse environment is expected to have them. Dependency tracking for solution components: import fails if the solution doesn't include all required components and the target environment doesn't already have them, and export warns about missing components. ALM for code apps: a code app is added to a solution, Pipelines deploy it with preflight checks for dependencies, and code apps don't support source code integration.
- Observed shapes, not documented, read for shape only in a scratch folder: microsoft/PowerAppsCodeApps at `3daf77f295f98dbab13bebe8a791dd2cbe7b44db` (MIT), `powercat/release-planner/ReleasePlannerLite_1_0_0_4.zip` and `ReleasePlanner_1_0_0_3.zip` (one app in two versions); microsoft/copilot-agentic-business-process at `3eb9736d25bd054c6765bf0814af9f6c92749ec2` (MIT), `core/solution/export`; TALXIS/docs-patterns-practices at `d02cdb9acd8a5a644900218417e957c813ed46b6` (MIT), `sample-repo/src/Solutions.CodeApp`. All four apps have `CanvasAppType` 4, no `CanvasAppSubtype`, and the same 27 metadata children. `CdsDependencies` is `{"cdsdependencies":[…]}` with entries `{"logicalname", "componenttype"}`: type 1 for tables, and type 372 with `"componenttypeplatformname":"connector"` for the Release Planner's custom connector, whose `logicalname` equals its root component's `schemaName`. The zips hold the metadata as `CanvasApp` elements of `customizations.xml`, as M46 found for `pac solution pack`; the other two are unpacked folders. The exports agree on this shape, so the "Stops if" rule doesn't apply.
- `DatabaseReferences` is `{}` in the Release Planner, and one `default.cds` entry with `databaseDetails` and `dataSources` (each `{"entitySetName", "logicalName"}`) in the other two. In TALXIS its data sources are the two `CdsDependencies` tables. In the agentic export they are the eight `CdsDependencies` tables plus a ninth with entity set `Environment`, which `CdsDependencies` doesn't list and the solution doesn't hold: a data source, not a dependency the export records (decisions Q9).
- Where the tables and connectors are held: the Release Planner zips list the connector as a type-372 root component; the agentic export lists its six prefixed tables as type-1 root components, and `systemuser` and `team` both as root components and as `MissingDependencies`; TALXIS's `Solutions.CodeApp` holds only the app and has an empty `MissingDependencies`, while its tables are type-1 root components of the sibling `Solutions.DataModel` (and shells in `Solutions.UI`), deployed by a package. The TALXIS manifests are kept as source in that repo, and all five have an empty `MissingDependencies`, so they don't show what the platform would declare; the platform-written agentic export declares its standard tables (decisions Q7). No export declares a connector as a `Required` node.
- `alm-sample`'s canvas app (type 0) shows a third entry kind: `{"dependencyid", "componenttype": 29, "componenttypeplatformname": "Workflow"}`.
- Issue #342 in microsoft/PowerAppsCodeApps (closed 2026-06-25 as by design): after connection references were renamed, the app record's `cdsdependencies` kept the old connection reference names and a Pipelines deploy failed with "The dependent component connectionreference … does not exist". The issue reads the Dataverse record, not an export; no export read here holds a connection reference entry.
- F3 (a code app round-tripped through `pac solution unpack` at the pinned `pac` versions) was not read: the rule rests on these public exports, and the page says so.
- Environment facts were not read: this plan changes this repo and targets no environment.

## Open questions

## Questions for your admin

- Is "Enable code apps" turned on in the Dev environment? F3 needs it; without it M47 rests on public exports (carried from the Phase 7 plan; it doesn't block this build).

## Steps

1. This plan, a review in a fresh context, fixes, sign-off.
2. Rows 3, 5, 6 and 7 and see them fail to compile or fail; then rows 1, 2 and 4 until they pass.
3. Rows 8 and 10; `RuleDocsTests` and `DocsDriftTests` pass.
4. Row 9 by "Update goldens", reading the diff; `RoleEvalTests` passes with no eval file changed.
5. Row 11, and a sanity run of the built `analyze` on scratch copies of the public exports (Assumptions), recorded in the Log: expected no finding and no note on the two Release Planner zips and the agentic export, and two findings on TALXIS's `Solutions.CodeApp`.
6. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan (`bash reports/leakscan.sh origin/main <this branch's commit>` from the main checkout); push and open one `feat:` PR whose body names the new warning, the note, that it changes no exit code under the default `--fail-on error`, the two departures from the M47 section, and that every command that loads a solution now parses `CdsDependencies` without refusing. The master session merges.
