plan-hash: 4a2dcef883f51cfd82fcaed3fdf299a5207fce0116027cf9e1ba2099e312f2d8
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: nobody confirmed in this session that `pac` is signed in to an environment, and the plan's `environment:` is null, so `powerops plan facts` and `pac` were not run (tenant: offline). `plan status` was run with the build at `src/PowerOps.Cli/bin/Debug/net10.0/powerops.dll` (status `approved`, 0 open questions, 0 admin questions). The fix plan of 2026-11-08 that the Goal cites is not in the repository and was not read. This review reads `plan.md`, `decisions.md` and the code, tests, fixtures and docs at `d903298` (the same as `278b136` outside `docs/plans/m76-drift-names/`). One claim was checked on Learn: the `botcomponent` table reference lists the intersect tables `botcomponent_workflow`, `botcomponent_environmentvariabledefinition`, `botcomponent_msdyn_aimodel` and `botcomponent_dvtablesearch`, and the `botcomponent_workflow` reference gives it the solution-aware columns (`solutionid`, `componentstate`, `ismanaged`) (read 2026-11-09; R2).

Checklist items that do not apply, because this plan changes the PowerOps CLI and builds no Power Platform solution:

- 1 Solution boundaries and publisher prefix: not applicable; no solution is built. Tests use hand-made snapshots, the existing samples and captures, and new samples with fake values.
- 2 Connection references and environment variables: not applicable; no solution is built. The plan and `decisions.md` hold no URL, email or person's name; the only id-like values are the fake `00000000-…` prefix and commit ids.
- 3 Premium connectors and licences: not applicable; no connector or table is used.
- 4 DLP fit: not applicable; no connector is used.
- 5 Dev to Test to Prod route: not applicable; the change ships through the repo's branch and pull request.
- 6 Managed or unmanaged: not applicable as a packaging choice.
- 7 Data volume and delegation: not applicable as delegation; each new read is one page of at most 5,000 rows, refused past that as today (row 3).
- 8 Security roles: not applicable as roles of a built solution; the read rights `drift` itself needs are reviewed under R1 and R3.
- 11 Size: no table, list, flow or screen. Each Data row maps to the Goal's F40 line or a Done line, and `Won't build` is present.

## Findings

### High

No findings.

### Medium

#### R1. Connection references are a type above 10000, so read 9 runs for nearly every solution, the cost the plan leaves out of `deploy plan` for that reason
- Area: 8 Security roles (read rights of `drift`), 9 Testability
- Finding: row 4 makes read 9 (`solutioncomponentdefinition`) whenever read 3 holds a type of 10000 or above. In both captures the only such type is 10164, the connection references (`capture/dev-read3.json`, `test-read3.json`), which `ComparePair` already leaves out by id. So any solution with a connection reference makes read 9, and a role without read on `solutioncomponentdefinition` (no source covers it, Not checked) turns today's working `drift` into exit `3`. Done line 1 ("A solution with no such component makes none of these reads, so its cost and the role it needs are as today") holds only for solutions with no connection reference. `decisions.md` refinement 3 keeps read 9 out of `deploy plan` for exactly this reason. Read 9's result is never used for a reference: the reference is left out by id before any name is looked at.
- Suggested fix: in row 4, make read 9 after read 6 and only when a component of type 10000 or above has an id that is not one of read 6's `connectionreferenceid`s. Say in Done line 1 and `COMMANDS.md` that the definitions read runs only for a solution with another dynamic type (an agent, app settings). The capture then makes no read 9, so the two capture companions of row 8 (`capture/dev-read9.json`, `test-read9.json`) and the `DriftCaptureTests.cs` change drop out. Add a row 4 test: a solution whose only type above 10000 is a connection reference makes no read 9.
- Resolution: open

#### R2. Other `botcomponent_*` intersect tables are not left out, so an agent with a flow or variable tool still drifts after an import
- Area: 9 Testability, 10 Unanswered areas
- Finding: row 6 leaves out `appsetting` and `botcomponent_connectionreference` by name. Learn's `botcomponent` reference also lists the intersect tables `botcomponent_workflow`, `botcomponent_environmentvariabledefinition`, `botcomponent_msdyn_aimodel` and `botcomponent_dvtablesearch`, and `botcomponent_workflow` has the same solution-aware columns. These are the same kind of row as `botcomponent_connectionreference`, a link between two components with an id of its own. They are keyed by `(name, id)`. If their ids change on import as the trial record suggests for `botcomponent_connectionreference`, they give `component-missing` and `component-extra` and exit `1`. That would happen for any agent that calls a flow, which is the common case Goal line 2 targets. The trial's agent had only connection-reference links, so the record could not show this. `Won't build` accepts that unknown families show as drift, but these are known from Learn.
- Suggested fix: in row 6, leave out and count every name that starts with `botcomponent_` (not `botcomponent` itself), in place of the one name. Add a row 6 test with a `botcomponent_workflow` pair under other numbers and ids that gives no item and a not-compared count. Name the prefix in `COMMANDS.md` and Assumptions, with the Learn pages.
- Resolution: open

#### R3. Agent rows the reader cannot see become id comparisons and silent drift
- Area: 8 Security roles, 9 Testability
- Finding: Assumptions say Environment Maker reads only its own agents. A `bot` or `botcomponent` row that read 10 or 11 doesn't return keeps `SchemaName` null and is compared by id (row 4, refinement 7). After an import the ids differ, so each one gives `component-missing` and `component-extra` with exit `1`. Nothing in the report says that rows were not read. The user sees drift where only read rights are missing, which is the exact failure F40 set out to remove. The Assumptions line says such an agent "shows as `agent-missing`", which holds only when one side read it.
- Suggested fix: in row 6, add a `notCompared` line for each compared snapshot with names that holds `bot` or `botcomponent` components without a schema name: `<position> <environment>: <n> agent or agent component rows were not readable, so they were compared by id`. Test it in `DriftCompareTests`, and say in `COMMANDS.md` that the reading account needs organization read on `bot` and `botcomponent`. Correct the Assumptions line.
- Resolution: open

#### R4. An existing `NotCompared` test changes, and the fallback line also appears where nothing was mixed
- Area: 9 Testability
- Finding: `DriftCompareTests.Dropped_types_in_a_compared_pair_are_listed_once_in_type_order` (`DriftCompareTests.cs:46-55`) compares snapshots made by `Snap`, whose `Agents` is null, and one of them holds type 10087. Under row 6 the fallback rule ("a compared snapshot has `Agents` null while a compared snapshot holds a type of 10000 or above") adds three lines, so its exact `Assert.Equal` fails. Row 6 says only that tests with no type above 10000 stay as they are. The same rule prints one fallback line per snapshot when two version-1 files are compared offline, a pair compared exactly as before.
- Suggested fix: limit the fallback line to a pair that mixes a snapshot with names and one without, which is the only case where matching changes. The existing test then stays as it is. Otherwise name the test and its three new lines in row 6. Either way, add the case "two version-1 snapshots with type 10087: no fallback line" (or its opposite) as a test.
- Resolution: open

### Low

#### R5. An old plan file is refused with a reason that blames the target
- Area: 9 Testability, 10 Unanswered areas
- Finding: after this change `SnapshotFile.Write` writes version 2 with `name`, `schemaName` and `agents`, so the digest that `DeployPlan.Inputs` hashes moves for every solution. `DeployApply` (`DeployApply.cs:118-121`) then refuses an old plan with "the target's state of the solution in <zip> changed since the plan", which points the person at the tenant when only the file format moved. The plan documents the refusal (Done line 5) but not the wrong reason.
- Suggested fix: have `Write` keep the version-1 text when `Agents` is null and no component has `Name` or `SchemaName`. That is always true for `ReadTarget`, and it is a valid version-1 file. The deploy digest then doesn't move: no refusal, no release note, and `deploy-plan-*` stay unchanged by design. Otherwise add a docs line that this message after an upgrade means "plan again".
- Resolution: open

#### R6. A version-2 snapshot file can hold two agent components with one schema name, and the comparison would merge them
- Area: 9 Testability
- Finding: row 3 rejects duplicate schema names in reads, "so two rows can never share a key". Row 5's `Read` checks uniqueness only on `type/id`. A hand-edited or merged file with two `botcomponent` rows of one schema name (ignoring case) passes, and row 6's key set folds them into one, so one is never compared.
- Suggested fix: in row 5, reject a version-2 file where two components share `(name, schemaName)` ignoring case when `schemaName` is set, and test it.
- Resolution: open

#### R7. The new not-compared line states as fact what the Assumptions mark unverified
- Area: 10 Unanswered areas
- Finding: row 6's line says `its ids change on import` for `appsetting` and `botcomponent_connectionreference`. Assumptions and Not checked say the second was matched only by count and neither is shown to be the same component.
- Suggested fix: word it `its ids can change on import, so it is counted, not compared by id`. Keep the old 10087 wording only for the by-number line.
- Resolution: open

#### R8. Some line citations are off by a few lines
- Area: 9 Testability
- Finding: at `278b136`, `SnapshotFile.SchemaVersion` is line 14 (the plan says 15). The `morerecords` refusal is `DriftParsers.cs:110` (108). `Read` is `DriftLiveReader.cs:24-28` (27-30), `ReadTarget`'s call is line 40 (46) and `ReadSolution` is 70-101 (69-98). `DriftReport.WriteJson`'s version is line 50 (48).
- Suggested fix: correct the numbers, or cite by member name.
- Resolution: open

#### R9. The version docs are only partly listed, and no test checks the new drift docs
- Area: 9 Testability
- Finding: row 7 changes only `docs/schemas/README.md:28`. Every earlier bump there has its own paragraph (`diff` 1 to 2, 2 to 3, `deploy plan` 1 to 2), and `JsonOutput`'s summary ("every JSON output but `diff` and `deploy plan`") becomes wrong. Row 8 names `DocsDriftTests` as the test, but that test checks commands, flags, rules, Action inputs and tool names, not the drift kinds, reads or snapshot text.
- Suggested fix: add to row 7 a "`drift` from 1 to 2" and a "snapshot file from 1 to 2" paragraph, and the `JsonOutput` summary. In row 8, say that the `COMMANDS.md` text is checked by reading in the review, not by `DocsDriftTests`.
- Resolution: open
