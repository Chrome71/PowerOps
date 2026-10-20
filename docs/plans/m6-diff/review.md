plan-hash: 4bf715e05e06ddd2f381ed5260c80f61f32b66e92f385b8d1c1cd3688125b202
model: claude-opus-5-5
fresh-context: yes

Checked against the code and tests on `claude/m6-diff-41283c` (`git diff main...HEAD`), not only the prose. `dotnet test` passes (357 tests). Checklist items 1 to 8 (solution and publisher, connection references and environment variables in the built thing, licensing, DLP, route, managed or unmanaged, delegation, security roles) do not apply to an offline CLI that creates nothing in a tenant; the plan says so for most of them (see R7 for the two it does not state).

## Findings

### High

#### R1. An unpacked folder and its real exported zip do not compare equal when the solution has a canvas app
- Area: 9. Testability (and the Data claim "so zip and folder inputs compare equal")
- Finding: An unpacked folder reads canvas app metadata from `CanvasApps/*.meta.xml`, whose root `<CanvasApp>` declares the `xsi` namespace itself. A real export keeps the same `<CanvasApp>` element inside `customizations.xml`, where `xsi` is declared on `<ImportExportXml>`; `SolutionParser.cs:85` serializes that child with `e.ToString(...)`, which places the namespace declarations differently, and `SolutionDiff.cs:188` then compares the serialized text, so `SolutionDiff.cs:82` reports "definition changed". Reproduced: `tests/fixtures/powerops-seed/v1` against a zip of the same files in export layout (`solution.xml`, `customizations.xml` holding the `<CanvasApp>` element without its own `xmlns:xsi`, no `CanvasApps/*.meta.xml`) prints `changed canvasApp pops_workitems_2af60: definition changed` and exits 1; adding the declaration to the element gives "no changes", exit 0. The tests that claim folder and zip compare equal (`SolutionDiffTests.An_unpacked_folder_and_its_exported_zip_have_no_changes`, `DiffCommandTests.Unpacked_folder_and_its_exported_zip_exit_zero`) use `SolutionZip.Create`, which keeps `CanvasApps/*.meta.xml` in the zip, so the export path is never exercised. The common CI case (an exported zip against the unpacked folder in the repo) would show a false change and exit 1 for every canvas app.
- Suggested fix: Compare canvas metadata independent of where namespaces are declared (for example drop namespace declaration attributes before comparing, or compare with `XNode.DeepEquals` after normalizing them), add a test with a zip whose canvas app lives only in `customizations.xml`, and keep the plan's "zip and folder compare equal" claim only once that test passes.
- Resolution: fixed (196f815: namespace declarations are ignored when canvas metadata is compared; a Core test diffs a zip in real export layout against powerops-seed/v1 and expects no changes)

### Medium

#### R2. The Constraints say no tenant data reaches pull-request comments, but default values are printed
- Area: 2. Connection references and environment variables; 10. Open questions
- Finding: Constraints state "Output must not leak tenant data into pull-request comments", yet `SolutionDiff.cs:66` prints environment variable default values (up to 100 characters via `Show`, `SolutionDiff.cs:104`) in every format, including Markdown. Default values commonly hold site or service addresses (the seed v2 change itself hard-codes a SharePoint address in a flow). The plan leaves this as open question 2 while stating the constraint as met.
- Suggested fix: Resolve open question 2 before sign-off. Either hide default values like current values (and add a test like `Environment_variable_current_values_are_never_in_the_output`), or narrow the constraint to "current values" and say default values are printed.
- Resolution: open

#### R3. The BUILD-PLAN M6 row asks to ignore timestamps and volatile IDs; the plan does not map or flag this
- Area: 9. Testability (BUILD-PLAN M6 row)
- Finding: The M6 deliverable "Ignores noise (timestamps, ordering, volatile IDs)" is not addressed line by line. The build deliberately reports the canvas `AppVersion` timestamp as "republished" (decision Q5), which is a deviation from the row that the plan does not call out. The plan never says which IDs are volatile or how they are ignored. The canvas meta comparison (`SolutionDiff.cs:79-83`) also counts studio version stamps (`CreatedByClientVersion`, `MinClientVersion`, `publisherVersion` in `Tags`) as "definition changed", so a republish with a newer studio shows two changes. The "Done when" bullets are met and tested; this deliverable is not.
- Suggested fix: Add the BUILD-PLAN deliverables to "How the build meets it", state that `AppVersion` is reported on purpose (so the row's "timestamps" is met only for flows and paths), say what "volatile IDs" means here and which test covers it, and decide whether studio version stamps should be ignored.
- Resolution: open

### Low

#### R4. Markdown cells can exceed the stated 200-character cap
- Area: 9. Testability (Screens)
- Finding: `DiffReport.cs:104` truncates to 200 characters and then escapes, and escaping doubles every `_`, `|`, `*`, `` ` ``, `[`, `]`, `<`, `>` and `\`. A connection reference logical name of 200 underscores gives a 400-character cell. The 60,000 limit still holds because it is checked on the escaped row.
- Suggested fix: Reword the plan to "values are cut at 200 characters before escaping", or escape first and cut without splitting an escape sequence.
- Resolution: fixed (196f815: cells are escaped first, then capped at 200 characters; tested with 300 underscores)

#### R5. Duplicate keys and unnamed canvas apps are silently collapsed
- Area: 9. Testability (Data)
- Finding: `Keyed` keeps the first item per key (`SolutionDiff.cs:130-131`), and a canvas app whose meta XML has no `<Name>` or does not parse gets the key `(unnamed)` (`SolutionDiff.cs:188`, `SolutionDiff.cs:192`). Failing input: old and new each with two canvas meta files lacking `<Name>`; change only the second app between versions; `diff` reports no changes and exits 0. The Data table also does not say that workflows without an id fall back to the name as key, or that keys match case-insensitively.
- Suggested fix: Document the key rules in the Data table and either report duplicates (for example in the detail) or include the file name in the fallback key for unnamed apps, with a test.
- Resolution: open

#### R6. The README `diff` paragraph is out of date with the code
- Area: 9. Testability (docs)
- Finding: The paragraph lists "the solution version, managed flag and publisher" but not the unique name, which the code compares since d55cecf (`SolutionDiff.cs:44`) and the plan's Data table lists. It also states "a folder and its exported zip compare equal", which R1 shows is false for canvas apps. Step 3 says "Each fix has a test"; the README fix has none, which is fine for docs but the wording overclaims.
- Suggested fix: Add the unique name to the README list, fix or qualify the folder-and-zip sentence with R1, and say "each code fix has a test" in Step 3.
- Resolution: fixed (196f815: the README lists the unique name; the folder-and-zip claim now holds per the R1 fix)

#### R7. Open questions mix follow-ups with decisions, and the decisions were never asked of the user
- Area: 10. Unanswered areas and the open-question count
- Finding: Of the 4 open questions, two are decisions (compare tables, views and forms; hide default values) and two are follow-up tasks (YAML layout after M5; the README merge with M7). The `## Log` says Q1 to Q7 were decided by the implementing session, not asked of the user, and users and automation were approved without questions. Security roles and managed or unmanaged are not stated as not applicable. `Questions for your admin` is empty, which is correct for an offline tool.
- Suggested fix: Move the two follow-ups to a "Follow-ups" list, and at sign-off have the user confirm Q1 to Q7 explicitly (Q4, Q5 and R2 matter most). Add one line that security roles and managed or unmanaged do not apply.
- Resolution: open
