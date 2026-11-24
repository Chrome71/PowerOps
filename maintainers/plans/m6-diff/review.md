plan-hash: bdb45b7c9044243d6b5af83d05563b7443846ff87bfb359ffbf8bb05e9b17f8f
model: claude-opus-5-5
fresh-context: yes

Checked against the code and tests on `claude/m6-diff-41283c` at 6b39d7e, not only the prose. The diff tests pass (`dotnet test --filter Diff`: 44 passed). The plan has 0 open questions and no admin questions. Checklist items 1 to 8 do not apply to an offline CLI that creates nothing in a tenant, and the plan now says so (Constraints). The plan and `decisions.md` contain no URL, GUID, email or person's name.

## Earlier findings

- R1 (folder and real export zip differ for canvas apps): resolved. `SolutionDiff.cs:189` drops namespace declarations before comparing, and `SolutionDiffTests.cs:288` diffs a zip with the app inside `customizations.xml` against `powerops-seed/v1` with no changes.
- R2 (constraint said no tenant data, but default values are printed): resolved. Q9 is answered, and the constraint (plan Constraints) now says only current values are hidden and default values are printed, cut at 100 characters (`SolutionDiff.cs:104-105`).
- R3 (BUILD-PLAN noise deliverable not mapped): resolved. The plan maps timestamps, ordering and volatile IDs to the build, calls out `AppVersion` as deliberate, and says studio version stamps add "definition changed".
- R4 (Markdown cells could pass 200 characters after escaping): resolved. `DiffReport.cs:104-121` escapes and then caps, tested at `DiffReportTests.cs:96`. See R1 below for one wording detail.
- R5 (duplicate keys and unnamed canvas apps collapse): resolved in the plan text. Data documents the key rules and accepts the limit. The code is unchanged (`SolutionDiff.cs:130-131`, `190`, `194`).
- R6 (README out of date): resolved. README line 57 lists the unique name, and Step 3 says "Each code fix has a test".
- R7 (open questions mixed with follow-ups; Q1 to Q7 not asked): resolved. The follow-ups moved to `## Follow-ups`, Constraints says security roles and managed or unmanaged do not apply, and the Goal says the user confirms Q1 to Q7 at sign-off.

## Findings

### High

No findings.

### Medium

No findings.

### Low

#### R1. A cut Markdown cell is 201 characters, not 200
- Area: 9. Testability (Screens)
- Finding: Screens says "Cells are capped at 200 characters". `DiffReport.cs:111-113` stops at 200 escaped characters and then appends "…", so a cut cell is 201 characters long, and `DiffReportTests.cs:103` asserts `200 + 1`. The same cap also applies to the bold header line and the summary line (`DiffReport.cs:55`), which the plan does not mention. The 60,000 limit is unaffected.
- Suggested fix: Change Screens to "cells are cut at 200 characters plus an ellipsis (the header and summary lines too)".
- Resolution: open

#### R2. File paths are compared inside canvas app metadata
- Area: 9. Testability (How the build meets it, volatile IDs)
- Finding: The plan says file paths and file names "are not identity and never compared". Canvas meta XML holds paths in `BackgroundImageUri` and `DocumentUri` (for example in `tests/fixtures/powerops-seed/v1/CanvasApps/*.meta.xml`), and `SolutionDiff.cs:82` and `190` compare the whole meta (minus `AppVersion` and namespace declarations) as "definition changed". This causes no false change between a folder and its zip, because both sides carry the same text, but the claim is broader than the code.
- Suggested fix: Narrow the wording to "the paths of the input files are never compared", or strip the two `*Uri` elements before comparing and add a test.
- Resolution: open

#### R3. The export-layout canvas test is built from the unpacked file, not from a real export
- Area: 9. Testability (Data claim "zip and folder inputs compare equal")
- Finding: The plan says the folder-against-zip check includes "a zip in real export layout with a canvas app" (How the build meets it, Step 4). `SolutionDiffTests.cs:303-308` builds that zip by copying the children of the unpacked `*.meta.xml` into `customizations.xml`, and no committed fixture is a real export containing a canvas app (none of `tests/fixtures/zips/*.zip` contains one). The layout is real, but the test cannot detect any content difference that `pac solution unpack` introduces between the exported `<CanvasApp>` element and the meta file. Equality in the common CI case (a fresh export against the unpacked folder in the repo) therefore rests on an untested assumption that the plan does not list.
- Suggested fix: Add the assumption to Assumptions ("`pac solution unpack` writes the exported `<CanvasApp>` element to the meta file unchanged apart from namespace declarations"), and add a scrubbed real export zip with a canvas app as a fixture in a later milestone.
- Resolution: open
