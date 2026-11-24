# Decisions: m47-code-app-deps

## Round 1

### Q1. Which rule id?
1. PO026, the next free id after M46's PO025 (recommended)
2. In your own words
Answer: 1. The recipe's "one after the highest in `docs/rules/`" gives PO026, and neither the Phase 7 plan nor BUILD-PLAN names an id for M47.

### Q2. Where do the dependencies live in the model?
1. Three more fields on `CanvasAppMeta`: `File`, `Tables` and `Connectors`, parsed once in the parser for every canvas metadata (recommended)
2. A new `Solution` field for code apps only
3. Parse the JSON in the rule from `CanvasAppTexts`
Answer: 1. The M47 section says to read the `CanvasApps/*.meta.xml` the parser already loads, and M46 left `CdsDependencies` and `DatabaseReferences` for M47 on the same record. Parsing in the parser keeps the rule a set lookup; a second list would duplicate the metadata. `File` comes back because a rule now reports one (M46 dropped it in review R8 only because no rule did). Parsing every type, not only type 4, keeps "null" meaning one thing: text that couldn't be read. Weighed again after review R4 and R9: the parse can't throw (the type is read with `TryGetInt32`, and the JSON exceptions are caught, each with a test), so parsing the other types adds no refusal; their lists are unused.

### Q3. Which entries are tables and connectors?
1. `CdsDependencies` entries with `componenttype` 1 (tables) and 372 (connectors) by `logicalname`, and every data source's `logicalName` in `DatabaseReferences` (tables); entries of other types are skipped (recommended)
2. Also check connection reference and workflow entries
Answer: 1 (amended by Q9: `DatabaseReferences` is not read; and by Q10 and Q11: entries of types other than 1, 372 and 29 give a note). The four public code apps hold only types 1 and 372, and the M47 section names tables and connectors. `alm-sample`'s canvas app holds type-29 workflow entries named only by `dependencyid`, and no export shows a connection reference entry, so checking those would rest on no observed shape. The page says which entries are skipped.

### Q4. What about standard tables such as `systemuser`?
1. Not checked: a table name with no underscore is skipped, with no note (recommended)
2. Checked like any other table
3. A note for each
Answer: 1. Learn says a custom table's name must have a customization prefix and an underscore (Customize table definitions), and that standard tables are part of the system solution and every environment is expected to have them (Tables and metadata in Microsoft Dataverse), as the Phase 7 plan says of the `Entity` targets M46 leaves alone. No underscore is not the same as standard: Dynamics 365 app tables without a prefix, such as `incident`, are skipped too, and the page says so (review R7). Whether an export declares a standard table depends on the environment's layers (the agentic export declares `systemuser` and `team` from two Dynamics solutions), so option 2 would fire on apps that import fine. A note per standard table would print on most code apps for no risk. First-party tables with a prefix (`msdyn_`, `adx_`) are checked, and exports declare them when another solution holds them.

### Q5. What holds a table or connector?
1. A root component of the same type (1 or 372) with that `schemaName`, or a `MissingDependencies` `Required` node with that `schemaName` or id, compared ignoring case; the sets are built once per solution (recommended)
2. Any root component with that name
Answer: 1. A root component of another type with the same name doesn't hold a table; typing the sets costs one line. Logical names are lower case and a schema name may be mixed case (Learn's table names page), so case is ignored. `RuleHelpers.DeclaredNames` is M46's one-set-per-solution helper, so the rule stays linear.

### Q6. What does the reader do with dependency text it can't read?
1. Keep the app with null `Tables` and `Connectors`; PO026 gives one note per code app; nothing is refused (recommended)
2. Refuse the file
3. Skip the unreadable entries and check the rest
Answer: 1 (amended by Q10 and Q11: missing text also gives null lists, and entries of other types give a second note instead). Every command that loads a solution reads canvas metadata, and before M47 nothing parsed these fields, so a refusal would stop `diff`, `deploy` and `test` on files they read today (M46 Q9 and Q14 for `ConnectionReferences`). Option 3 would check part of an app silently; D10 says a check that can't see its input says "not checked". The JSON exceptions caught are M46's: `JsonException` and `InvalidOperationException` (an unpaired surrogate).

### Q7. TALXIS's code app solution would get two warnings: its tables are in a sibling solution and its `MissingDependencies` is empty. Is that a finding?
1. Yes: keep it, and say on the page that a solution whose tables sit in a sibling solution with no declared dependency gets the warning, and how to clear it (recommended)
2. Skip tables whose prefix is the solution publisher's
3. In your own words
Answer: 1. Learn says import fails when a required component is neither in the solution nor in the target, and Pipelines check dependencies before a deploy; the solution declares none, so `analyze` can't tell the sibling solution will be there. The TALXIS manifests are kept as source in that repo (all five have an empty `MissingDependencies`), so they don't show what the platform would declare; the platform-written agentic export declares its standard tables (review R5). TALXIS orders its imports with a package, and its `Solutions.UI` includes the same tables as shells (root components with behavior 2), which this rule counts as held. The warning changes no exit code by default and can be baselined. Option 2 would hide the case the rule is for: a table of the app's own publisher that isn't in the solution.

### Q8. How do the CLI tests get a code app without a fixture?
1. A temp copy of `alm-sample` whose canvas metadata gets `CanvasAppType` 4 (and one fake table entry for the firing case), and the zip built by M46's `PackedLikePac` helper (recommended)
2. A new fixture with a code app
Answer: 1. The M47 section has the tests build the metadata by hand, and a code app bundle can't go in a fixture (Microsoft Software License Terms). The `alm-sample` canvas metadata already has a connector held as a type-372 root component and four type-29 entries, so the quiet case also covers the skipped entries; `PackedLikePac` builds the zip as `pac solution pack` does.

## Round 2 (from the review)

### Q9. Does the rule read tables from `DatabaseReferences`? (R1, R2)
1. No: tables come from `CdsDependencies` type-1 entries only, and the plan records the departure from the M47 section (recommended)
2. Yes, but only data sources that `CdsDependencies` also lists
3. Yes, but only `default.cds` entries of the observed `Environmental` form
Answer: 1. In the agentic export `DatabaseReferences` holds a data source with entity set `Environment` that `CdsDependencies` doesn't list and the solution doesn't hold, so reading it gave a false warning on a Microsoft sample (R1). In every export its real tables are also `CdsDependencies` type-1 entries, so option 2 adds no finding and is option 1 with more code. Option 3 still reads the `Environment` entry, which is in that form. `CdsDependencies` is the field the issue that motivates the section shows the platform enforcing on a deploy. Dropping the second field also removes R2 (entries for another environment or an environment variable).

### Q10. What does the reader do with a `CdsDependencies` entry kind it hasn't seen, or no `CdsDependencies` at all? (R3, R8)
1. Null lists and the note, for any entry type other than 1, 372 and 29, for no text, and for an object without the `cdsdependencies` array; type 29 is skipped silently (recommended)
2. Skip unknown types silently, and say on the page that connection reference entries aren't covered
Answer: 1 (amended by Q11 for entries of other types). Q6 rejects checking part of an app silently, and D10 says a check that can't see its input says "not checked". The issue's connection reference entries would be such an unknown kind, so a code app holding them now prints the note instead of looking clean. Type 29 is observed (a workflow, by `dependencyid`), is outside "a table or connector", and is skipped as the page says. All four observed code apps carry the element with the array, so its absence is a shape PowerOps hasn't seen, not proof of no dependencies; an empty array is the app saying it has none. This amends Q3 and Q6.

### Q11. Does an entry of another type stop the check of the app's tables and connectors? (R10)
1. No: the other entries are still read and checked, and a second note names the types that weren't (recommended)
2. Yes, as Q10 first said: null lists and one note
Answer: 1. With option 2, one connection reference entry, the case of the issue that motivates the section, would hide a missing table in the same app, so the rule would go blind on the apps most likely to have the problem. Q6 rejects checking part of an app silently; a note naming the types it skipped is not silent, and D10 asks for exactly that line. Malformed text still gives null lists and the first note, since nothing in it can be trusted. This amends Q10.

The other findings needed no choice: R4 (the type is read with `TryGetInt32`, as the build already did; reader tests for `1.0`, `1.5` and `1e10`), R5 (a Goal paragraph on why a warning, and Q7 reworded), R6 (the YAML layout in Won't build with Learn's no-source-integration line, and the `Users` bullet names the two XML readers), R7 (the Tables and metadata page in Assumptions and on the Sources line, Q4 reworded, and the `CanvasAppSubtype` sentence on the page), R8 (a firing connector without an underscore; (b) is Q10; the page says no export declares a connector) and R9 (Q2 says the wider parse was weighed after R4). From the second pass: R11 (the BUILD-PLAN M47 row states the two departures, and the verification line is written), R12 (Q3 is marked amended, and the rule's comment says "no customization prefix") and R13 (the depth test nests inside a valid object). From the third pass: R14 (the second note names at most five types, then how many more) and R15 (the tests give the types in descending order, so the order read is pinned). From the fourth pass: R16 (the five-type boundary is tested).

## Log

- 2026-11-03: plan started at the request of the session that started this one: build M47 of Phase 7, build-order item 82, from the M47 section of `docs/plans/phase7-power-platform/plan.md`, its BUILD-PLAN row and decision 49 (D10), without F3
- read on 2026-11-03: AGENTS.md; the Phase 7 plan's M47 section, D1, D10 and the Dev facts table; the BUILD-PLAN M47 row and item 82; the M46 plan folder (layout) and its build hand-over; the Phase 7 Code Apps research report; `Solution`, `SolutionParser`, `RuleHelpers`, `INotChecked`, `PO001`, `PO004`, `PO024`, `Builders`, `AnalyzeCommandTests`, `RuleDocsTests`, `docs/rules/PO024.md`
- the public exports in Assumptions were downloaded through the GitHub API into the session's scratch folder and read with a scratch script, for shape only; nothing from them is committed. Issue #342 and its comments were read through the GitHub API
- Learn facts checked 2026-11-03 by the building session with the Learn MCP tools; the pages are in Assumptions and go on the PO026 page
- `tests/eval` and `tests/fixtures` hold no canvas metadata with a `CanvasAppType` other than 0 (checked with `grep`), as the Phase 7 plan's M47 "Touches" says
- depth: quick, as the BUILD-PLAN M47 row asks ("Its own quick plan first")
- questions answered by the building session, under the user's delegation of 2026-10-22 (autonomous milestone mode), with the Phase 7 plan's scope and the master session's brief
- areas not asked: users, automation, environments and ALM, and constraints; the change is to this repo, and the plan answers each or marks it not applicable
- approved 2026-11-03 by the building session under the user's delegation of 2026-10-22, for a fresh-context review; approved without asking: users, automation, environments and ALM, constraints
- the code was built alongside the review, before sign-off, by an implementer subagent (Sonnet) in this worktree, test first (the tests were written before the code, but could not compile before the record change, so no red run was seen); the rule page, the rule table row, the `analyze` paragraph and the BUILD-PLAN row and item by the building session; read by the building session
- fresh review (general-purpose subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-11-03, on the body at hash `13edbb47…`: 1 High, 3 Medium, 5 Low (R1 to R9); its rerun of rows 2 and 4 on the four exports found the false finding R1
- R1 to R9 answered in `plan.md`, the code, the tests, the page and here (Round 2; Q2, Q3, Q4, Q6 and Q7 amended); see review.md
- sanity run 2026-11-03 with the build after the review fixes, on scratch copies outside the repo: the two Release Planner zips as published, and the agentic and TALXIS code app solutions as minimal folders (`Other/Solution.xml`, `Other/Customizations.xml` and the app's `.meta.xml`, fetched at the pinned commits). At `c4dcf52` all four give no finding. With this build the Release Planner zips and the agentic export give none and no note, and TALXIS's `Solutions.CodeApp` gives two PO026 warnings (its two tables), exit 0, as Steps item 5 expected
- the same reviewer's second pass 2026-11-03, with the build in the tree, tested in a copy outside the repo: R1 to R9 hold (about 30 mutants, each failing a test but one); 4 Low new (R10 to R13), fixed: entries of other types no longer hide the app's other findings (Q11), the BUILD-PLAN row and verification line, two stale sentences, and a depth test that tests the limit
- the same reviewer's third pass 2026-11-03: R10 to R13 hold (15 of 16 mutants fail a test); 2 Low new (R14, R15), fixed: the second note names at most five types, and the tests pin the order read
- the same reviewer's fourth pass 2026-11-03: R11, R14 and R15 hold (7 of 8 mutants fail a test); 1 Low new (R16), fixed by the building session with a test of the five-type boundary, which fails on the reviewer's surviving mutant; no plan body change, so the review's hash holds. In all: 1 High, 3 Medium and 12 Low, every one fixed but R9, accepted in Q2
- sanity run repeated on the final code 2026-11-03: the same results, and no PO026 note on any of the four exports
- signed off 2026-11-03 by the building session under the user's delegation of 2026-10-22, with no finding open (`powerops plan sign-off`, on the body hash of the fourth pass); status set to done with the build in this branch, before the PR's CI, which the master session checks before it merges
