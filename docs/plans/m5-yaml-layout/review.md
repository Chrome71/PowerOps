plan-hash: 57847f8b8b1ebb1bc39fc448df784c6e1b01ad1259c2352a967f6075d2af610d
model: claude-opus-5-5
fresh-context: yes

Quick review. `powerops` is not on PATH, so `plan status` ran from a locally built CLI: status `approved`, review `done`, 0 open questions, and the `bodyHash` above. Its only warning is the stale review, which this file replaces. `plan facts` was not run, because this review must not touch an environment. The plan header was not changed.

All twelve findings of the fourth review (R1 to R12) are resolved in the plan text: the canvas-formula route and its premium caveat (Constraints guidance 3), one shared `Fix` sentence, `SourceFolders` null versus an empty list with a firing test, a per-run cache and budget, empty URL defaults with PO002, the listed Sources and the FAQ wording for relational configuration, the new "no solutions found" message, errors for a missing `publisher.yml` or `solutioncomponents.yml`, the three shapes the fixture settles, Step 1 marked done with its cleanup kept open, the file sort order with a two-file test, and the PO014 row in BUILD-PLAN. The guidance's premium caveat and relational-data wording were checked again in "Environment variables frequently asked questions" on 2026-10-20 and match. The plan and `decisions.md` contain no URL, GUID, email or person's name. Checklist items 2 to 8 apply only to how the fixture was made, which is done.

## Findings

### High

No findings.

### Medium

No findings.

### Low

#### R1. PO014's message names an `<app>`, but the plan doesn't say which name
- Area: 9. Testability
- Finding: Step 7's message starts "Canvas app '<app>'". `canvasapp.yml` holds both `Name` (the schema name, `pops_workitems_2af60`) and `DisplayName` ("Work Items"), and today's model has no canvas app name at all: `CanvasAppTexts` is raw text and `CanvasFormulas` carries only file and property path. The message is compared by the parity test, stored in baselines and written to the goldens, so the choice matters.
- Suggested fix: In Data ("Model changes") or Step 7, say which value `<app>` is (for example `DisplayName`, as PO006 uses the flow's display name) and that `CanvasFormulas` carries it.
- Resolution: open

#### R2. Step 5 cites the wrong pages for "exit code 0"
- Area: 10. Unanswered areas
- Finding: Step 5 sources PO013's Error severity ("pack leaves the component out with exit code 0") from the pac solution reference and the YAML format page. On Learn, "pac solution" says pack "still succeeds but omits" the component, and "Solution YAML source control format reference" is about missing `solutioncomponents.yml` paths. Only the troubleshooting section of "SolutionPackager tool" states the exit code 0 for a root component without source files.
- Suggested fix: In Step 5, cite "SolutionPackager tool" (troubleshooting) for the exit code and the drop, and keep "pac solution" as the second source.
- Resolution: open

#### R3. PO013 checks only that the folder exists, while Learn's fix asks for the app file
- Area: 9. Testability
- Finding: For a canvas app, PO013 passes when `canvasapps/<schemaName>` exists. The "SolutionPackager tool" troubleshooting section says the app's file must exist in that folder. The committed fixture keeps only `canvasapp.yml` and `Src/*.pa.yaml` (NOTICE.md lists the trimmed files), so a stricter check would fire on the fixture and break the v1 "no PO013" goal. The plan's folder-level choice is right for M5, but nothing tells `PO013.md` to say so, and its title ("has no source files") promises more.
- Suggested fix: In Step 5, say that `PO013.md` describes the check as folder-level, and that a folder with incomplete app files is not detected.
- Resolution: open

#### R4. Learn pages now disagree on whether `clone` and `sync` write the YAML layout
- Area: 10. Unanswered areas
- Finding: "Environments and route" says only Git integration writes YAML, citing the Git integration FAQ. On 2026-10-20, "FAQs about source code integration" still says `unpack`, `clone` and `sync` don't support YAML, but "Solution YAML source control format reference" lists `pac solution clone` and `sync` as producing it and recommends `clone` for a complete YAML folder. The fixture is done, so M5's build isn't affected. Step 9, however, ticks the FOUNDATION section 8 item that asks exactly how `unpack`, `clone` and `sync` behave with YAML.
- Suggested fix: In Step 9, leave that FOUNDATION item open, or record both Learn pages and that they conflict. Do not state "only Git integration writes YAML" in the README.
- Resolution: open

#### R5. Step 6 names no tests
- Area: 9. Testability
- Finding: Step 6 adds a parser for `.pa.yaml` (nested `Children`, block-scalar formulas, `ComponentDefinitions`, files without `Properties` such as `_EditorState.pa.yaml`) but lists no test. Step 7's tests cover ordering only through PO014.
- Suggested fix: In Step 6, add reader tests: a block-scalar formula, a nested control's property path, a value without a leading `=` that is skipped, and a file with no controls.
- Resolution: open

#### R6. The Step 1.6 cleanup has no owner and isn't part of Done-when
- Area: 5. Route
- Finding: Step 1 keeps disconnecting the solution and deleting the Azure DevOps repo open, and that repo holds the unscrubbed commits. No later step or Done-when item closes it, so v0.6.0 can ship with the repo still there.
- Suggested fix: Add the cleanup to Done-when, or as a last step with a Log line when it is confirmed.
- Resolution: open
