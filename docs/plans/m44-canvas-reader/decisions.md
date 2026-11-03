# Decisions: m44-canvas-reader

## Round 1

### Q1. Where does the "not checked" note come from?
1. The reader lists the apps it couldn't read in a `Solution` property, and PO018 implements `INotChecked` to give one line per app naming PO014 and PO018 (recommended)
2. The reader writes notes into `LoadContext`, as it does for skipped links, and every command prints them
3. In your own words
Answer: 1. M45 built `INotChecked` for this, and `analyze`, `baseline` and the MCP tool already print, clean and cap its lines. Only PO014 and PO018 read the formulas, so the note belongs to `analyze` and not to `diff`, `test` or `deploy`, which don't use them. One rule gives the line so an app gets one note, not two; the count line names its first rule, PO014, which is accepted for a cap of 20.

### Q2. What happens to PO005's canvas shortcut?
1. Nothing: it reads `CanvasAppTexts`, which both XML layouts already fill from the app's metadata, so the `.msapp` read adds no match (recommended)
2. Also match connection references in the formulas
3. In your own words
Answer: 1. The Phase 7 plan says the shortcut "covers the XML layout"; it already does, through `CanvasApps/*.meta.xml` in a folder and the `CanvasApp` elements of `customizations.xml` in a zip, which hold the app's connection references. Formulas name a data source by its display name, never by a connection reference's logical name or a connector's API id, so matching them adds nothing.

### Q3. Is an app whose `.msapp` isn't in the solution "not checked" too?
1. Yes: a `DocumentUri` that names no listed `.msapp` gives the note too, with its own text (recommended)
2. No: only a `.msapp` with no `Src`
3. In your own words
Answer: 1. Decision 49: a check that can't see its input says so. A repo that keeps the unpacked folder without its `.msapp` (the seed fixtures do, scrubbed of the environment URL the file caches) would otherwise pass PO014 and PO018 silently, now that the PO014 page says the XML layout is read. The `DocumentUri` is only looked up among the listed paths, never opened.

### Q4. What is a formula's `File` in the XML layouts?
1. The `.msapp` path, which is a file in the folder and an entry in the zip (recommended)
2. A path inside the package, such as `CanvasApps/x_DocumentUri.msapp/Src/Screen1.pa.yaml`
3. In your own words
Answer: 1. SARIF and code scanning point at a file that exists in the repo; a path inside a binary doesn't. The message already names the control and property. PO014's de-duplication key, the first two segments of `File`, is then the `.msapp` path, one per app, as the YAML layout's is the app folder. A baseline matches on solution, rule, component and message, not file, so the choice doesn't affect baselines.

### Q5. How does PO018 match names?
1. `ForAll` and `Patch` ignoring case, outside strings, quoted names and comments, in one linear pass (recommended)
2. Exact case only
3. In your own words
Answer: 1. Studio saves built-in names in one case, and no user name can take a built-in function's name, so ignoring case costs nothing and covers hand-edited YAML. One pass with a stack keeps a hostile formula linear; a scan per `ForAll` would be quadratic on deep nesting.

## Log

- 2026-11-03: plan started at the request of the session that started this one: build M44 of Phase 7, build-order item 79, from the M44 section of `docs/plans/phase7-power-platform/plan.md`, its BUILD-PLAN row and decision 49, after F1
- 2026-11-03: F1 read: one unmanaged `pac solution export` (pac 2.14.1) of the seed solution from Dev, approved by the person in the master session ("Yes, export F1"), into the session's scratch folder outside the repo; entry names and layout recorded in Assumptions; nothing created, saved or published in Dev. `Src/*.pa.yaml` present, `AppCheckerResult.sarif` present, no `.msapr`: no stop
- read on 2026-11-03: AGENTS.md; the Phase 7 plan's M44 section, Automation, D10 and the "Environments and route" table; the BUILD-PLAN M44 row, item 79 and decision 49; the M45 plan folder (layout); the Phase 7 canvas research report; `Solution`, `SolutionParser`, `ZipReader`, `XmlUnpackReader`, `YamlReader`, `YamlXml`, `ZipGuard`, `ReadBudget`, `AnalysisResult`, `TextReport`, `Analyzer`, `INotChecked`, `PO005`, `PO014`, `docs/rules/PO014.md`, `SolutionZip`, `Builders`, `tests/fixtures/NOTICE.md`
- Learn facts checked 2026-11-03 by the building session with the Learn MCP tools; the pages are in Assumptions and go on the PO018 and PO014 pages
- depth: standard, as the Phase 7 plan's Automation section sets for the readers
- questions answered by the building session, under the user's delegation of 2026-10-22 (autonomous milestone mode), with the Phase 7 plan's scope and the master session's brief
- areas not asked: users, automation, environments and ALM, and constraints; the change is to this repo, and the plan answers each or marks it not applicable
- approved 2026-11-03 by the building session under the user's delegation of 2026-10-22, for a fresh-context review; approved without asking: users, automation, environments and ALM, constraints
