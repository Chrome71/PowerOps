plan-hash: 2219a9d06ab63660d9e2eeb44f1ee02847f4e0918a457486d1190208b95857c8
model: claude-opus-5-5
fresh-context: yes

Third review, of the signed-off plan body, asked for to replace the stale second review. The plan header was not changed: it already says `review: done`, and the plan is `signed-off`, not `approved`. The resolutions of the second review (R1-R11) were checked against the current plan and `src/` and hold. R2's resolution raises a new question about the cache's scope (R2 below). `powerops plan facts` was not run, because it queries the tenant through `pac` and this review must not touch a Power Platform environment.

Checklist items 2 (hard-coded values), 3 (licence), 4 (DLP), 5 (route), 6 (managed or unmanaged) and 8 (roles) apply only to how the fixture is made, not to the offline tool. The plan covers each of them for the fixture, and the open points (trial end date, DLP not checked) are accepted on purpose in the Log, so they give no finding. Item 7 (data volume) applies to the reader's input limits. The plan and `decisions.md` contain no URL, GUID, email or person's name. The only host named is the fake placeholder `contoso.sharepoint.com`.

## Findings

### High

No findings.

### Medium

#### R1. PO013 uses an empty `SourceFolders` to mean "not YAML", so it stays quiet when every folder is missing
- Area: 9. Testability
- Finding: Step 5 says PO013 "is quiet when `SourceFolders` is empty (XML and zip)". But a YAML solution has an empty `SourceFolders` exactly when none of the folders its `solutioncomponents.yml` lists exist. For example, a solution with one root table whose `entities/<name>` folder was deleted. That is the worst case of what PO013 is meant to catch: pack leaves out every component and exits 0. The rule can't tell "XML or zip layout" from "YAML layout with nothing present", and Step 5's two tests (one missing folder among others, one case mismatch) don't cover it.
- Suggested fix: Mark the layout separately from the folder list. For example, make `SourceFolders` null for XML and zip and an empty list for YAML, or check that `ManifestFile` ends in `solution.yml`. Add a Step 5 test where the only root component's folder is missing and PO013 fires.
- Resolution: accepted as a build note for Step 5: SourceFolders is null for XML and zip and a list (possibly empty) for YAML, with a test where the only folder is missing

#### R2. The scope of the shared file cache and budget is not defined, and a static cache breaks parallel tests
- Area: 7. Data volume (input limits)
- Finding: Assumptions and Step 4 say the loader "keeps the root's file cache and budget across calls for the same root", while `AnalysisResult.Run` still calls `SolutionLoader.Load(path)` once per `solution.yml`. `SolutionLoader` is a static class, so the obvious way to do this is a process-wide dictionary of roots. That state is never cleared. In tests, xUnit runs test classes in parallel, and `ReportTests`, `SarifReportTests` and `FixtureRuleTests` all load `tests/fixtures` at the same time. `ReadBudget.Consume` and a plain `Dictionary` cache are not thread-safe, so these tests would share and corrupt one root's state and fail at random. A process-wide cache also returns stale text if a file under the same root changes between two calls.
- Suggested fix: In Step 4, scope the cache and budget to one `AnalysisResult.Run` call. For example, `Run` creates a per-run loader context, keyed by YAML root, and passes it to `Load`. A plain `Load(path)` call gets a fresh context. Say that nothing is static. The two-solution test should also check that a shared file is read, and counted against the budget, only once.
- Resolution: accepted as a build note for Step 4: the shared cache and budget live for one AnalysisResult.Run call, nothing static

### Low

#### R3. The "no solutions found" error still says the YAML layout is not supported
- Area: 9. Testability
- Finding: `AnalyzeCommand` prints "no solutions found under <path> (the YAML layout is not supported yet)" when discovery finds nothing. After M5 that hint is wrong. It also shows up in the case Step 4 creates on purpose: pointing `analyze` at `solutions/<name>` below a YAML root finds nothing. Step 9 updates the README sentence, but no step changes the CLI message, and Screens says the text output is unchanged.
- Suggested fix: In Step 4, change the message, for example to say that a YAML layout must be analyzed from the folder that holds `solutions/`. Assert the new text in the "path below the YAML root" test.
- Resolution: accepted as a build note for Step 4: drop "(the YAML layout is not supported yet)" from the no-solutions message

#### R4. The plan doesn't say what happens when `publisher.yml` or `solutioncomponents.yml` is missing
- Area: 1. Solution boundaries
- Finding: The XML layout has the publisher inside `Solution.xml`, so it can't go missing. In the YAML layout, a `solution.yml` with no matching `publishers/<name>/publisher.yml` would give an empty prefix if the reader just fills in blanks. That makes PO009 fire on every component and PO010 report a nameless publisher. A missing `solutioncomponents.yml` would make every component silently disappear, so every rule goes quiet.
- Suggested fix: In Step 3 or Step 4, make both cases an `InvalidDataException` that starts with the `solution.yml` path, and add a test for each.
- Resolution: accepted as a build note for Step 3: a missing publisher.yml or solutioncomponents.yml is an InvalidDataException starting with the path

#### R5. Step 3 doesn't say how a `workflow.yml` finds its definition `.json`
- Area: 9. Testability
- Finding: Step 3 reuses "the XML parser's element logic", and `SolutionParser.ReadWorkflow` finds the definition through `JsonFileName` among the solution's paths. It throws if a cloud flow declares a file that is missing. The Data table says only "`modernflows/<id>/workflow.yml` plus the definition `.json`". If Git integration keeps the XML-layout value (`/Workflows/<name>.json`) in `workflow.yml`, the reused logic either throws or leaves `ClientData` empty. Then PO001, PO004, PO005, PO006, PO011 and PO012 go quiet for every flow. Step 8 would catch this on the real fixture, but the implementer has to decide it in Step 3.
- Suggested fix: In the Data table or Step 3, state from the public exports how the `.json` is located: `JsonFileName` resolved against the flow's folder, or the single `.json` next to `workflow.yml`. Add a reader test with a cloud flow whose definition is found and parsed.
- Resolution: accepted as a build note for Step 3: real exports give JsonFileName as /modernflows/<id>/<file>.json, relative to the YAML root; ReadWorkflow already trims the leading slash
