# Decisions: m46-app-modules

## Round 1

### Q1. Which rule ids?
1. PO024 for the Error and PO025 for the warning, the next free ids after M45's PO023 (recommended)
2. In your own words
Answer: 1. The recipe's "one after the highest in `docs/rules/`" gives PO024, and neither the Phase 7 plan nor BUILD-PLAN names an id for M46.

### Q2. What do the new `Solution` fields hold?
1. Three records with only what the two rules read: an app module's name, site map components and `appelements` pages; a site map's name, `Page` and `GenPageId` targets; a canvas app's name and its `ConnectionReferences` keys and connector ids (recommended)
2. The raw XML of each app module and site map, parsed again in the rules
3. In your own words
Answer: 1. The M46 section asks for app modules and site maps in new `Solution` fields. Parsing in the parser keeps every refusal where the path is known; the rules then read lists without checks of their own. `CanvasAppTexts` stays as it is for PO005 and `diff`; the canvas name and connections are parsed once from the elements the parser already builds, so no file is read twice. `CanvasAppType`, `CdsDependencies` and `DatabaseReferences` are left for M47, which reads them.

### Q3. Which file does a folder layout read when both `AppModule.xml` and `AppModule_managed.xml` are present?
1. `AppModule.xml`, and `AppModule_managed.xml` only when the folder has no `AppModule.xml`; the same for site maps (recommended)
2. Both
3. The one that matches the manifest's `Managed` value
Answer: 1. The local unpack shows an unmanaged unpack writes only `AppModule.xml`, a managed one only `AppModule_managed.xml`, and the fixture (unpacked as Both) has both, byte-identical. Reading both would count every app twice. Option 3 needs the manifest's `2` (Both) mapped too and gains nothing on the observed exports.

### Q4. What is a `Page` target?
1. A `SubArea` at any depth under `SiteMap` whose `Page` attribute is set and which has neither `GenPageId` nor `Entity`; a `GenPageId` makes it a generative page target whatever else it has (recommended)
2. Any `SubArea` with a `Page` attribute
3. In your own words
Answer: 1. The MIT power-platform-skills source records the order the platform's SDK types a `SubArea` in: `GenPageId`, then `Entity`, then `Page`. Following it keeps an `Entity` entry with a stray `Page` from being an Error. The SDK takes a `SubArea` at any depth, so the reader does too. Attribute and element names are exact, as XML is case-sensitive and the SDK reads them that way.

### Q5. When does a missing `Page` target fire, and when is it "not checked"?
1. It fires when the solution holds at least one canvas app's metadata (a `CanvasApps/*.meta.xml` in a folder or a `CanvasApp` element in a zip's `customizations.xml`) with a `Name`; with none, it is a note (recommended)
2. It fires whenever the page is not found
3. In your own words
Answer: 1. The M46 section says a `Page` target fires only when the solution carries `CanvasApps/`, and D10 makes a layout without the evidence a note. A trimmed folder such as `alm-makers`, which dropped `CanvasApps/`, would otherwise give false Errors. Known limit, accepted: metadata whose XML is malformed gives no name, so a page whose only metadata is malformed counts as missing; the parser already tolerates such metadata only for `diff`, and no observed export has it.

### Q6. Which pages are an app's custom pages?
1. The union of the app module's `appelements` pages and the `Page` targets of the site maps the app names as `type="62"` components, by name ignoring case (recommended)
2. The site map's `Page` targets only
3. The `appelements` only
Answer: 1. Learn says a custom page can be opened from the site map or from `navigateTo` and Power Fx, so an app can hold pages its site map doesn't show; the observed Creator Kit app lists every page as an `appelement`. A site map entry whose page is missing from `appelements` is still a page the app opens. The union counts each page once.

### Q7. How are connectors and connection references counted?
1. Over the app's custom pages whose metadata the solution holds: connectors are the distinct connector `id` values (ignoring case), connection references the distinct keys of the pages' `ConnectionReferences` objects (ignoring case) (recommended)
2. The sum of each page's entries
3. In your own words
Answer: 1. Learn gives the limits "across all custom pages" and says the pages share one connection set, so a connection that two pages use counts once; the observed gov-apptemplates pages share keys. Learn doesn't say how to count from an export: this is the plan's reading, the rule page says so.

### Q8. What does PO025 say about pages it can't read?
1. One note per app with the number of its pages that have no metadata in the solution or whose `ConnectionReferences` isn't a JSON object; the page count still includes them (recommended)
2. Nothing
3. One note per page
Answer: 1. D10: evidence outside the export is "not checked", with a note. A page's name is known even when its metadata isn't, so it counts toward 25; only its connections are unknown. One line per app keeps the notes short, and `TextReport` caps them anyway.

### Q9. What does the reader do with a `ConnectionReferences` text that isn't a JSON object?
1. Keep the app with `Connections` null, which PO025 notes; no refusal (recommended)
2. Refuse the file
Answer: 1. Canvas metadata is read by every command that loads a solution, and before M46 nothing parsed this JSON, so a refusal would stop `diff`, `deploy` and `test` on a file they read today. PO025 is the only reader, and a note says what it couldn't count.

### Q10. How does the zip CLI test get a zip with app modules in `customizations.xml`?
1. Build it in the test from a temp copy of `alm-sample` as `pac solution pack` does: move the two root elements into `<AppModules />` and `<AppModuleSiteMaps />` of `Other/Customizations.xml`, drop the two folders, then `SolutionZip.Create` (recommended)
2. Commit the packed copy as a fixture
3. Change `SolutionZip.Create` to move them for every test
Answer: 1. The packed copy showed exactly this mapping. A committed zip would be a modified Microsoft sample and a new solution in every fixture-wide golden; changing the shared helper would change other tests' zips for no check that needs it.

## Round 2 (from the review)

### Q11. Is PO024's premise good enough for an Error, and what else proves a page is held? (R3)
1. Keep the Error, accept the risk that an export doesn't declare a page from another solution, say so on the page, and count a type-300 root component with the page's `schemaName` as held (recommended)
2. Make PO024 a warning until an export with a page in another solution is read
3. In your own words
Answer: 1. The M46 section and D10 make a proven missing component an Error, as PO001 and PO004 are. The Creator Kit export shows exports declaring canvas apps outside the solution as type-300 `Required` nodes with `schemaName`, which is the mechanism the rule reads; only the app-module-to-page edge is unobserved. A root component is the manifest's own list, so a partly trimmed folder with the page listed but its metadata gone is not an Error. A wrong Error is baselined or fixed by declaring the dependency, both of which the page says.

### Q12. What does the reader do with an app module or site map that has no unique name? (R7)
1. Refuse it: an `InvalidDataException` starting with the path, in both layouts (recommended)
2. Skip the record
3. Keep it with an empty name
Answer: 1. Every observed file has the name, and a record without one can't be joined to its site map or named in a message; an empty name would join every such app to every such site map. The M45 reader refuses a component with no id the same way, and `Users` already says a malformed file stops a command.

### Q13. How does PO025 tell a component library from a page, given Q2 left `CanvasAppType` out? (R1)
1. Read `CanvasAppType` into the canvas record and skip an `appelement` whose held metadata has a type other than 2; a name with no held metadata, or no type, still counts (recommended)
2. Count every `appelement`, and say so on the page
Answer: 1. The gov-apptemplates export lists a component library among the six `appelement`s of an app with five custom pages, so option 2 overcounts on a real shape. One integer is the smallest field that fixes it, and M47 reads the same field. This amends Q2: the record is `CanvasAppMeta(Name, Type, Connections)`, renamed so it doesn't share `SolutionDiff`'s method name `CanvasApp`, and without a file, which no rule reports (R8).

### Q14. Which JSON failures give a null `Connections`? (R5)
1. Any `JsonException` or `InvalidOperationException` while the text is read, including a depth over 64 and an `id` with an unpaired surrogate; an entry that is not an object is an entry with an empty `id` (recommended)
2. `JsonException` only
Answer: 1. Q9 says this text never stops a command. `GetString()` raises `InvalidOperationException` on an unpaired surrogate, which the review reproduced, and a refusal there would have no path. This amends Q9.

### Q15. What does PO025 count for an app that names several site maps? (R15)
1. Only the first known one by ordinal name, with a note naming how many it names and which one is counted (recommended)
2. The union of their pages per app
3. The largest as the basis, the others' pages added per app, cached by name set
Answer: 1. Learn says an app can have its own site map, and every observed export (alm-sample, the Creator Kit's two apps, gov-apptemplates' two apps) names one. Option 2 is quadratic on a crafted solution, which needs only a second type-62 component per app (the review measured 6.4 s for 1,000 apps); option 3 still costs the second map per app and caches a set per name set. The note keeps the result honest (D10) for a shape no observed export has.

The other findings needed no choice: R2 (the zip test now moves the `CanvasApp` element and drops the `.meta.xml` and `CanvasApps/src/`, as the packed copy shows; this amends Q10), R4 (the declared names are one set per solution, with a time-bound test), R6 (eight more test cases), R9 (two Assumptions corrected), R10 (two locations corrected), R11 (a sentence on the PO025 page), R12 (one tally per site map), R13 (leftovers), R14 (page sentences held to their sources), R16 (a shared-key test), R17 (Learn's "can have its own site map" quoted as it is), R18 (a note for a named site map the solution lacks, so PO025 doesn't pass silently on pages it couldn't count, as Q8 does for unread pages; the several-site-maps note counts the held ones) and R19 (the plan says that note is a stopgap: a missing site map is D10's second case, an Error no M46 rule gives).

## Log

- 2026-11-03: plan started at the request of the session that started this one: build M46 of Phase 7, build-order item 81, from the M46 section of `docs/plans/phase7-power-platform/plan.md`, its BUILD-PLAN row and decision 49 (D10), without F2
- read on 2026-11-03: AGENTS.md; the Phase 7 plan's M46 section, D1, D10 and the Dev facts table; the BUILD-PLAN M46 row and item 81; the M44 and M45 plan folders (layout); the Phase 7 model-driven research report; `Solution`, `SolutionParser`, `RuleHelpers`, `INotChecked`, `PO001`, `PO004`, `PO005`, `PO020`, `SolutionZip`, `Builders`, `PowerPagesReaderTests`, `AnalyzeCommandTests`, `docs/rules/PO004.md` and `PO020.md`
- 2026-11-03: the packed copy made and unpacked locally in the session's scratch folder (pac 2.14.1, no sign-in), as Assumptions records; deleted after the build
- the public exports in Assumptions were read through the GitHub API, file by file, for shape only; nothing from them is committed
- Learn facts checked 2026-11-03 by the building session with the Learn MCP tools; the pages are in Assumptions and go on the PO024 and PO025 pages
- `tests/eval` holds no `AppModule`, `SiteMap`, `GenPageId` or `uxagentprojects` text (checked with `grep`), as the Phase 7 plan's M46 "Touches" says
- depth: standard, as the Phase 7 plan's Automation section sets for the readers
- questions answered by the building session, under the user's delegation of 2026-10-22 (autonomous milestone mode), with the Phase 7 plan's scope and the master session's brief
- areas not asked: users, automation, environments and ALM, and constraints; the change is to this repo, and the plan answers each or marks it not applicable
- approved 2026-11-03 by the building session under the user's delegation of 2026-10-22, for a fresh-context review; approved without asking: users, automation, environments and ALM, constraints
- the code was built alongside the review, before sign-off, by an implementer subagent (Sonnet) in this worktree, test first (the new tests failed to compile before the types existed); the two rule pages, the rule table rows, the `analyze` paragraph and the BUILD-PLAN row and item by the building session; read by the building session
- fresh review (general-purpose subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-11-03, on the body at hash `0528a7d9…`: 0 High, 5 Medium, 6 Low (R1 to R11); its local `pac solution pack` of `alm-sample` also showed where a packed zip puts the canvas metadata (R2)
- R1 to R11 answered in `plan.md`, the code, the tests, the pages and here (Round 2; Q2, Q9 and Q10 amended by Q13, Q14 and R2); see review.md
- sanity run 2026-11-03, with the build before the review fixes and again after them, on scratch copies outside the repo of the Creator Kit reference solution and gov-apptemplates' `federal/process-and-tasking`, each as the unpacked folder and as a local `pac solution pack` zip: the existing rules give the same findings as at `471af36` (0 and 18), PO025 fires once on the Creator Kit's `cat_CreatorKitReferenceApp` (27 custom pages) in both layouts, and PO024 fires on neither; no PO024 or PO025 note is printed. gov-apptemplates' Process and Tasking app counts 5 custom pages (its sixth `appelement` is a component library), 3 connectors and 12 connection references
- the same reviewer's second pass 2026-11-03, with the build in the tree: R1 to R11 hold (each new test fails on a mutant in a copy outside the repo); 1 Medium and 2 Low new (R12 to R14), all fixed: PO025 tallies each site map once, leftovers in the plan and BUILD-PLAN, and page sentences held to their sources
- the same reviewer's third pass 2026-11-03: R12 to R14 hold (the 5,000-app test takes 2 min 7 s on the old per-app union and fails); 1 Medium and 1 Low new (R15, R16), fixed: an app naming several site maps counts only the first (Q15), and a shared-key test
- the same reviewer's fourth pass 2026-11-03: R15 and R16 hold (the 1,000-app test takes 49.5 s on the old shape; five mutants each fail tests); 2 Low new (R17, R18), fixed: Learn's "can have its own site map" quoted as it is, and a note for a named site map the solution lacks
- the same reviewer's fifth pass 2026-11-03: R17 and R18 hold (five more mutants each fail tests); 1 Low new (R19), fixed in wording: that note is called a stopgap under D10's second case
- the same reviewer's sixth pass 2026-11-03: R19 holds, nothing new; no finding open (0 High, 7 Medium and 12 Low in all, every one fixed)
- sanity run repeated on the final code 2026-11-03: the same results, and no PO024 or PO025 note on either export
- signed off 2026-11-03 by the building session under the user's delegation of 2026-10-22, with no finding open (`powerops plan sign-off`, body hash `be2b849c…`, the hash of the sixth pass); status set to done with the build in this branch, before the PR's CI, which the master session checks before it merges
