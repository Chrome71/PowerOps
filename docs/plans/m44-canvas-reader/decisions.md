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
Answer: 1. The Phase 7 plan says the shortcut "covers the XML layout"; it already does, through `CanvasApps/*.meta.xml` in a folder and the `CanvasApp` elements of `customizations.xml` in a zip, which hold the app's connection references. Formulas name a data source by its display name, never by a connection reference's logical name or a connector's API id, so matching them adds nothing. The BUILD-PLAN M44 row is reworded to say so when M44 is marked built (review R9).

### Q3. Is an app whose `.msapp` isn't in the solution "not checked" too?
1. Yes: a `DocumentUri` that names no listed `.msapp` gives the note too, with its own text (recommended)
2. No: only a `.msapp` with no `Src`
3. In your own words
Answer: 2, changed during the build from 1. The first answer was 1 (decision 49: a check that can't see its input says so; the seed fixtures keep their metadata without the `.msapp`). The build showed that all 7 eval solution cases are copies of the seed solution whose metadata names a `.msapp` they don't hold, so the note changed every eval baseline, which decision 40 answers with a rerun of the pinned roles on all cases. The Phase 7 section asks only for the no-`Src` note. So a `.msapp` the solution doesn't hold gets no note, and the PO014 and PO018 pages say in one sentence that such an app isn't read. A later milestone can add the note together with the eval rerun.

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
- PO018 is M44's id because M45's Q1 left it free for M44, as the Phase 7 plan and BUILD-PLAN name it; the recipe's next free id would be PO024
- the code was built alongside the review, before sign-off, by an implementer subagent (Sonnet) in this worktree, test first (the new tests failed to compile before the types existed); read by the building session
- 2026-11-03: the build found that Q3's first answer changed all seven eval baselines (`RoleEvalTests` failed), so the build stopped as Constraints says; Q3 changed to option 2 before any baseline was touched, and the eval stays unchanged
- fresh review (general-purpose subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-11-03, on the body at hash `cca0c45b…`: 1 High, 4 Medium, 5 Low (R1 to R10); R1 is the eval conflict above
- R1 to R10 answered in `plan.md`, the code, the tests, the pages and here (Q2 and Q3 changed; see review.md). R3 was answered from the F1 export before it was deleted: element names only
- the reader was run on the F1 export with a throwaway console harness in the scratch folder (counts only): 89 formulas from 10 holders in the one `.msapp`, read through its `\` entry names, no app without source, and the rule ids PO005, PO006 and PO014, the same as the YAML fixture `powerops-seed-yaml/v2` of the same app
- 2026-11-03: the F1 export was deleted from the scratch folder after the reader was run on it again with the review fixes (same counts and rule ids)
- the same reviewer's second pass 2026-11-03, with the build in the tree: R1 to R10 hold, 3 new Low (R11 to R13), all fixed: a test that only the `.msapp` zip guard can pass, a bit-flip test over a built `.msapp` with `Src` entries (seed 27 of `MutationTests` draws no `.msapp`), and wording on the two pages and rows 2, 2a and 10
- the same reviewer's third pass 2026-11-03: R11 to R13 hold (R11 and R12 by mutation in a copy), 1 new Low (R14), fixed: row 2a's charge list and the pages' source for the environment-URL cache
- the same reviewer's fourth pass 2026-11-03: R14 holds, nothing new; no finding open (1 High, 4 Medium and 9 Low in all, every one fixed)
- signed off 2026-11-03 by the building session under the user's delegation of 2026-10-22, with no finding open (`powerops plan sign-off`); status set to done with the build in this branch, before the PR's CI, which the master session checks before it merges
