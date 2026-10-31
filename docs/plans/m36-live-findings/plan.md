---
status: implementing
review: done
signed-off: 2026-10-31
signed-off-hash: 2b6ecc02f9156221a47f02394b5d9e41b71c46efab6ef39802a9ccdc109dbadf
environment: null
depth: quick
supersedes: null
---

# M36: Findings of the M35 tenant run

## Goal

Build what the [M35 report](../m35-live-checks/report.md) decided: W-12 and W-13, and the warnings its Studio work earned. Four changes: a new error rule for a Create-only trigger with filter columns, a caveat on PO012's page, a `drift` notice for a component that is also in another solution, and warnings in `powerops-implement`.

Done when:

- Each change below is made as its row says, with the tests the row names.
- `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` and `powerops plan status .` are clean, and the leak scan finds nothing.
- The built `drift` has run once against `M35Build` in the test environment, read-only: one `also-in-solution` item naming `M35Other` for each of the three flows that joined it, none for the flow that didn't, and none naming a system solution. If the user has removed those objects first, this line is marked not verified, because the fixtures show only the parser and the fakes.
- When the user asks for a PR: the PR is green on Linux, Windows and macOS and squash-merged, then a second PR moves every PowerOps pin in the docs to that merge commit, records the CI results and marks this plan done.

## Users

- A maker whose flow has filter columns on a Create-only trigger, and the agent or reviewer who runs `powerops analyze` on it.
- A person who runs `powerops drift`, by hand, in CI or through the MCP tool, and wants to know that a component also sits in another solution.
- An agent that follows `powerops-implement` to build a canvas app with Canvas Authoring, or to create tables with the Dataverse Python client.

## Data

Line numbers are those of `c76dd98`.

| # | Severity | Change | Where | Test |
|---|---|---|---|---|
| 1 | medium | New rule PO015 by the recipe "Add a rule", severity `Error` (decisions Q1). Its condition is PO012's shape: `t.OperationId == "SubscribeWebhookTrigger"`, `t.Message == 1` and non-blank `t.FilteringAttributes` (decisions Q2). Title `Dataverse row-created trigger has filter columns`. `Fix` `Clear the trigger's filter columns, or change its change type to Create or Update.` Message `Flow '{w.Name}' has filter columns on a create-only trigger of '{t.Entity}'; in observed runs such a flow never started.` Its page says this comes from two runs in one environment, on Windows and on Linux, with their dates; that Learn says filter columns apply to the Update condition only and doesn't state this result; and that Delete-only and Create or Delete triggers, a managed solution and a production environment were not tested, and the cause on the service's side isn't known. The implementer re-reads Learn's trigger page when writing the page, and dates the Sources line that day | new `src/PowerOps.Core/Rules/PO015CreateTriggerWithFilterColumns.cs`; `src/PowerOps.Core/Rules/Analyzer.cs`; new `docs/rules/PO015.md`; `docs/rules/README.md` | a fires case and quiet cases in `ErrorRuleTests.cs` (message 1 with a blank filter, messages 2 to 7 with a filter, another operation with message 1 and a filter); `RuleDocsTests`, `RulesCommandTests` and `DocsDriftTests`; goldens by the recipe. No fixture has filter columns, so no `analyze-fixtures` finding should change |
| 2 | low | PO012's "How to fix" gains one sentence: don't set filter columns on a trigger whose change type is Create only, see PO015. Its `Fix`, message and severity stay, because the rule only fires on change types that include Update | `docs/rules/PO012.md` | `RuleDocsTests` |
| 3 | medium | `drift` gains read 8, `tests/fixtures/pac/drift/queries/read8-other-solutions.xml`, sent through `DriftLiveReader.Read<T>` once per 50 of the solution's component ids, with the table label `solutioncomponent (other solutions)` so that a failure can be told from read 3. A new information kind `also-in-solution` (decisions Q3), last in `DriftKind.Order` and false in `IsDrift`, gives one item per component and other solution: `Subject` as `unmanaged-layer` builds it (the flow's name, else `Component(type, id)`), `Expected` null, `Actual` the other solution's unique name. A new record `SnapshotAlsoIn(int Type, Guid Id, string Solution)` holds a row; `DriftParsers.OtherSolutions` returns a list of them and refuses a duplicate of type, id and solution. The list is a new optional last parameter of `Snapshot`, `AlsoIn`, null by default and never written to a snapshot file (decisions Q5). `ReadSolution` takes a flag that only `Read`, the path of `drift`, sets, and `ReadTarget` doesn't (decisions Q4). `Compare` gains `.ThenBy(x => x.Actual, StringComparer.Ordinal)` so the order doesn't depend on the service. `DriftReport.WriteText` prints `: <actual>` when `Expected` is null and `Actual` isn't | `src/PowerOps.Core/Drift/DriftCompare.cs:6-32,48-65,73-77`; `DriftQueries.cs`; `DriftParsers.cs`; `Snapshot.cs`; `src/PowerOps.Cli/DriftLiveReader.cs:22-26,48-71`; `src/PowerOps.Core/Output/DriftReport.cs:37`; `docs/schemas/drift.schema.json:32,40-41,43,45-46`; `docs/COMMANDS.md:67,69,75` (the checks on one environment; the kind, that only unmanaged, visible solutions are looked at, that the report names the other solution, and that a saved snapshot holds none; one more read per 50 components, at most 100 by read 3's limit, that a large solution wasn't tested, and the friendly-name filter's limit) and the line that says `deploy plan` uses `drift`'s reads, which gains that it doesn't make this one | `DriftQueriesTests` compares the query with the stored file. `DriftParsersTests` reads `dev-read8.json`, and refuses a duplicate row and `morerecords`. `DriftCompareTests` covers the item, its `Drift` false and its order, and its kinds test goes to 15 kinds and three information kinds. `DriftReportTests` covers the new text branch. `DriftLiveReaderTests` covers the batches (0, 1, 50 and 51 components), no read 8 for a solution that isn't installed, and the new label on a failed read 8. `LiveFakes.Sample` and the overrides in `ManagedWith`, `A_connection_reference_is_read_by_its_own_name_whatever_its_type_number` and `The_lists_come_out_sorted` answer read 8 with an empty list, through one shared check that tells it from read 3 (its query has `operator="in"`). The five-call assertions at `DriftLiveReaderTests.cs:304,318` become six. The `ReadTarget` tests' counts stay, which proves `ReadTarget` makes no read 8. `Capture.Fakes` answers read 8 from `dev-read8.json` for Dev and an empty list for Test. `SnapshotFileTests` checks that `Write` leaves `AlsoIn` out, comparing elements, because record equality compares a list by reference. `DocsDriftTests` |
| 4 | medium | Warnings in `powerops-implement`, as text in the existing steps. Section 3 step 3: a Canvas Authoring compile isn't a save, because after one Studio can keep Save greyed out and an older "All changes are saved" time, and a reload then loses the compile. So before committing the step, look at the saved time. If it didn't move, change a property and undo it to turn Save on, save, and confirm that the time moved. Reload only after that. In one run, two compiles of a whole screen with its gallery crashed Studio while the tool reported success, and the screen first with the gallery alone after it didn't; the cause isn't known. Modern controls can show as empty blocks until a reload. Also step 3: the Python client's table create can land in the preferred solution although `solution=` was passed, so check each table's membership and ask before moving one. Section 3 step 4: run the check and the commit as separate commands, so a failed check can't be committed. Section 4 step 3: if the published app shows an old-version banner, select Refresh on it, because the first load can still be the old version. Each statement says it was seen once where that's the evidence | `skills/powerops-implement/SKILL.md:39-42,52`; `plugins/*/skills/` by the recipe "Update goldens" | new assertions in `SkillsCommandTests` on each warning's wording; its test for the section headings stays green |

`tests/fixtures/pac/drift/queries/read8-other-solutions.xml` and `tests/fixtures/pac/drift/capture/dev-read8.json` are committed with this plan, with their `tests/fixtures/NOTICE.md` entry, so the capture outlives the test objects. No test reads them yet.

The goldens that should change, for the last step of the recipe "Update goldens": `rules.json`, `rules.txt` and `analyze-fixtures.sarif` gain PO015; `drift-seed.txt`, `.json` and `.md` and `drift-none.json` gain one Dev item; `plugins/*/skills/powerops-implement/SKILL.md` gains row 4's text. Nothing else.

## Automation

None in a tenant. `analyze` can exit `1` on a solution that passed before, for a flow PO015 finds. `drift` gains no exit code, but a failed read 8 exits `3` like the other reads, so a read that only informs can fail a run that used to pass.

## Screens

None. Only CLI text, JSON and docs change.

## Environments and route

Building needs no tenant. Planning read the M35 Dev environment on 2026-10-31, read-only, for the capture (decisions Q6 to Q8). The last check reads it once more through the built `drift`. That environment is named only in the private run list, never in this repo. No step writes to a tenant, and no step runs any `--apply`.

## Solution and publisher

None created. The last check reads `M35Build`, and `M35Other` shows up in its result.

## Constraints

- AGENTS.md: new logic comes with tests, `dotnet format --verify-no-changes` stays clean, Conventional Commits, no version or changelog edits. Rule `Fix` text has no Markdown and no URL. A skill edit is followed by the "Update goldens" recipe, so `plugins/*/skills/` regenerate.
- PO012's `Fix` and message stay: the `Fix` is in five goldens, and the message is in baselines.
- The snapshot file format, `DeployTarget`, `deploy plan` and its hash don't change. The MCP `drift` tool's description stays, and it gets the new kind through the same read.
- Section and step numbers of `powerops-implement` stay, because `docs/BUILD-PLAN.md` refers to them.
- `drift` takes its token from `az`'s active account and `pac`'s active profile. For the last check, this machine's existing sign-ins are selected with `az account set` and `pac auth select`, and both are restored straight after. No agent signs in. If the needed sign-in is gone, the line is marked not verified.
- No tenant URL, id, user name or credential in the repo. The capture's ids and solution name are replaced with the fixture set's fake values.

## Assumptions

- PO015 rests on two runs in one environment, one on Windows and one on Linux, with one kind of table row. Microsoft can change it, so the page says what was observed and when. An `Error` is right while the flow doesn't run.
- The reads were made as a system administrator. The minimum role wasn't tested, as for the rest of `drift`.
- The default solution has a generated unique name and a publisher prefix that Learn calls random, so it's filtered by friendly name, in the two spellings Learn uses. An environment that stores another name shows it as an `also-in-solution` item, which is noise and not a wrong result (decisions Q7).
- A large solution wasn't tested, and T8's arithmetic is from a small one. Read 3 takes one page of at most 5,000 rows, so read 8 makes at most 100 calls (decisions Q9).
- `schemaVersion` stays 1 with a new kind: nothing is released, so no consumer exists (as in M33 decisions Q1).
- A skill's text can tell an agent what to check but can't make it. The canvas warnings come from one app in one run.

## Open questions

## Questions for your admin

## Steps

1. This plan and its capture, a fresh review, fixes, sign-off.
2. Rows 1 and 2 by an implementer subagent, then reviewed.
3. Row 3 by an implementer subagent, then reviewed.
4. Row 4 by an implementer subagent, then reviewed; `plugins/*/skills/` regenerated.
5. The checks. The coordinating session reads the diff and runs `powerops drift --solution M35Build --env <env> --save <a folder outside the repo> --format json`, read-only, as the Done line says. The check is read from the `also-in-solution` items: exit 0 or 1 is expected, 1 when an item of the environment's own, like T1's `reference-unbound`, is still there, and only exit 2 or 3 means it didn't run. The report and the snapshot stay outside the repo. It confirms with `az account show` and `pac auth list` that both are back as they were, then adds the M36 verification line to `docs/BUILD-PLAN.md`, with counts and kinds only. The leak scan runs last.
6. When the user asks: the PR, which also carries the M35 report and skill fix from this branch; CI green on Linux, Windows and macOS; squash-merge.
7. The pin PR: every PowerOps pin to the merge commit of step 6, the CI results recorded, this plan marked done; CI green; merge.

Step 1 also adds an M36 section to `docs/BUILD-PLAN.md` after M34, with one milestone row and build-order items 62 and 63.
