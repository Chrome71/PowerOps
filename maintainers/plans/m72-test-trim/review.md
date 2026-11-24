plan-hash: d76757e99937e30a1d47897c3c64e25171564f12c34c36c6bb30e11f932ae268
model: claude-sonnet-5-5
fresh-context: yes

Environment facts were not read: the plan names no environment (`environment: null`) and changes only this repo's tests and one AGENTS.md line. Checklist items 1 to 9 (solution boundaries, connection references, licences, DLP, route, managed state, delegation, security roles) do not apply and are not reported. Item 10: 0 open questions. Item 11: the Goal names its Done-when lines and a `Won't build` list; no table, list, flow or screen is created. The plan and `decisions.md` hold no URL, GUID, email or person's name. `plan.md` was not edited (`review: pending` stays), because this request allowed no change outside `review.md`; the tests were not run. Each Data row was checked by opening the test at the given line, its named cover, and the `src/` code both exercise (`FlowSimulator.cs`, `ImportCall.cs`, `ExpressionFunctions.cs`, `DeployPlanFile.cs`, `DeployCommand.cs`, `DriftReport.cs`, `ManifestReport.cs`, `PowerPages.cs`, `InstallLineTests.cs`). Rows 1 to 3 are writable as described: the blob-id table of `m55-role-freeze/plan.md` has three rows (path in backticks, 40 hex digits in backticks) and `git hash-object` gives those three ids today; `FlowSimulator.cs:473` matches a runAfter status with `Contains` against the predecessor's result, and `TimedOut` is accepted at `:515-517` and has no test; `ImportCall.Printable` is public and `ImportCall.cs:98` cuts at 99 when the character at index 99 is a high surrogate.

## Findings

### High

No findings.

### Medium

#### R1. Row 6 drift report cuts lose the "layers not read" branches, and the cover cell is wrong
- Area: Data row 6, Done when 3
- Finding: The cover for `DriftReportTests` `:28`, `:137` and the shrink of `:70` is the `drift-seed.*` and `drift-none.json` goldens. Those goldens hold only `live` environments with `layersRead: true` (no `layers-not-read` item, no `source: snapshot`, no `layersRead: false`). The plan says `notCompared` is in no golden, but `drift-seed.json:54` and `drift-none.json:45` have it (with other text). The cut tests are the only ones that fail if `DriftReport.cs:31` loses the `, layers not read` suffix in text, or `DriftReport.cs:80` stops writing `no` in the "Layers read" cell of Markdown, or JSON stops writing `layersRead: false` and `source: snapshot`. `DriftCompareTests:71` and `OutputHygieneTests:240` check only the `notCompared` sentence. A mutation of either line passes after the cut, so the Done when 3 mutation check would fail for this group.
- Suggested fix: keep `:28` and `:137` (they are short), or shrink `:70` to the second environment and the `layers-not-read` item and add the text suffix and the Markdown `no` cell to a kept test. Correct the sentence about `notCompared`.
- Resolution: Fixed (decisions Log). Row 6 no longer lists any `DriftReportTests` test, so `:28`, `:70` and `:137` stay.

#### R2. Row 7 cut of `DeployApplyCommandTests:353` leaves the plan-file error path with no CLI test
- Area: Data row 7
- Finding: The kept rows `over-1-mb` and `symbolic-link` both return inside `DeployCommand.ReadPlanFile` (`DeployCommand.cs:182-201`) before `DeployPlanFile.Read` runs. Every other row (`not-json`, `no-hash`, `bad-hash`, `empty-target`, `no-inputs`, `unsafe-zip-name`) reaches the outer `catch (InvalidDataException)` at `DeployCommand.cs:205-209`, which prints `error: {Clean(ex.Message)}`. The plan says the kept rows are "the ones only the CLI reaches", but the cut rows are the only ones that reach this catch. The named covers (`DeployPlanFileTests:154` and `:168`) call `DeployPlanFile.Read` directly: they check the message, not that the CLI prints one clean line, exits 3, reads no target, and does not leak the value (the `SECRET-VALUE` assert exists only for `bad-hash`). A mutation that prints `ex.ToString()` or the value passes after the cut.
- Suggested fix: keep `bad-hash` (it carries the `SECRET-VALUE` check) as a third row.
- Resolution: Fixed. Row 7 keeps `bad-hash`, `over-1-mb` and `symbolic-link` of `:353`, and its cover names `bad-hash` for the outer catch (`DeployCommand.cs:205-209`).

#### R3. Row 11 `InstallLineTests` keeps 3 of 13 rows, but the rows take different branches
- Area: Data row 11
- Finding: The rule guards a supply-chain risk (an install of the unregistered id `PowerOps` by name alone). The 13 rows are not same-branch rows: the `\` and the backtick continuation (`InstallLineTests.cs:18` joins both; `A_continued_install_is_reported_at_its_first_line` covers only `\`), `--add-source` not counting as a source (the third row; the code accepts only `--source`), the `update` verb of the regex, the lower-case id, and `Microsoft.PowerApps.CLI.Tool` (a different id). "One it flags, one with a source, one that installs something else" loses the backtick join, the `--add-source` case and `update`; a mutation that treats `--add-source` as a source, or drops the backtick join, passes.
- Suggested fix: keep the rows `--add-source ... PowerOps` (flagged), the backtick-continuation flagged row, `update -g PowerOps` and the `--source` row, plus one other-id row (5 rows), and name them in the row.
- Resolution: Fixed. `InstallLineTests` is out of the Data table; all 13 rows and the continuation test stay.

#### R4. Row 8 cuts pins that its own rule says stay
- Area: Data row 8
- Finding: The rule is "a pin stays when its sentence is a safety rule, a routing trigger, or a step number other pages link to". Cut tests that hold such sentences: `SkillsCommandTests:320` pins "Run the check and the commit as separate commands, so a failed check can't be committed" and "Check each table's membership, and ask before moving one" (commit gate; ask first). `:495` is the only pin of the section-5 routing pointer ("When it names an app, page, site, report or agent, read `references/route.md`", with the order check against section 6), of the Power BI branch commands in the implement skill, and of the sketch entries on both menus. `:384` pins the review checklist's way-out item (checklist item 9 of the reviewer, which is a behaviour of the reviewer, not a style). `HandoffDocTests:123` pins `needs a yes` for Power BI and `(step 11)` for Copilot Studio, and the skill per product (routing); `:158` pins `ask first` and `Publish the agent before you share it` in step 11. Only date pins (`Checked on 2026-11-03`), the word ceiling (`:436`) and `12 data visuals` are what the rule calls cuttable.
- Suggested fix: cut the date, count and ceiling assertions only: keep the safety and routing strings of `:320` and `:495`, `needs a yes` and `(step 11)` of `:123`, and `ask first` of `:158` in smaller tests, or move those few strings into the kept tests. Decide `:384` explicitly in `decisions.md`.
- Resolution: Fixed. Row 8 cuts only `:436`, `:64` and `:75`; the safety and routing pins of `:320`, `:384`, `:495`, `:123` and `:158` stay. One residue: see R17 (`:436` also holds the ASCII-only assert).

#### R5. Row 8 cuts a docs-checker self-test that "Won't build" forbids, and the cover is partial
- Area: Data row 8 against "Won't build"
- Finding: "Won't build" bars any cut in "the self-tests that show a docs checker can fail". Row 8 cuts `HandoffDocTests:81` `The_check_reports_unknown_words_and_accepts_known_ones`, which is that self-test for the page's own checker (`Unknown`, `WordLists`). The reason given (the docs scan covers `docs/**/*.md`) holds only in part: `DocsDriftTests.Pages()` includes `docs/HANDOFF.md`, but `UnknownCommands` looks only at inline code spans and `sh`, `bash`, `shell` fences that start with `powerops `, while `:55` reads every `powerops <words>` in the page. Today the only mentions outside those places are four `powerops deploy gate` commands in the JSON hook block, and `HandoffDocTests:94` pins that exact command, so the loss is small, but a future prose or table mention is no longer checked.
- Suggested fix: either keep `:55` and `:81` (and cut only `:64` and `:75`), or add the two sentences to "Won't build"'s exceptions with the reason (the JSON block is pinned by `:94`; prose mentions are no longer checked).
- Resolution: Fixed. `HandoffDocTests` `:55` and `:81` stay, so "Won't build" is no longer contradicted by row 8.

#### R6. Rows 4 and 5 do not name the rows they cut, so the covers cannot be checked
- Area: Data rows 4 and 5
- Finding: "Cut the rows that take a branch another row of the same test takes" (`Equals_`, `And`, `Or`, `Not`, `Conversion_is_unsupported_for`) and "keeps 3 of its 6 rows" (`Odd_setting_names...`) leave the choice to the build, and the cover cell says "the rows kept". Some cuts that look like repeats are not: in `And` and `Or`, the 3-argument rows (`and(true, true, false)`, `or(false, false, true)`) are the only ones that fail if the function reads only the first two arguments; in `Odd_setting_names...` each row hits a different clause of `PowerPages.cs:33` (`Parts.Length == 3`, `Parts[1].Length > 0`, `Parts[0]` = `Webapi`), and only `Webapi//enabled` fails if the `Parts[1].Length > 0` clause is removed, and only `Webapi/error/innererror` has three parts; `PO015_quiet...` rows 4 to 7 are the values that contain a create (the code compares `t.Message == 1` and says the other types "were not tested, so they stay quiet"), so a change to `Message is 1 or 4 or 5 or 7` is caught only by them; `A_bad_field...` "kinds of fault (wrong type, empty, bad hex, too long)" lists a kind that has no row (there is no length check in `DeployPlanFile`), and omits the rows for the settings/hash pairing rule (`inputs.zips.0.settings` null, `inputs.zips.1.settingsSha256`) and the unsafe name rows (newline, slash, `::error::`), each its own branch (`DeployPlanFile.cs` pairing check, `SafeText`, `IsValidName`).
- Suggested fix: list the rows kept in each test in the row (or in `decisions.md`), starting from: a 3-argument row in `And` and `Or`; `Webapi//enabled`, `Webapi/error/innererror` and one row of 1, 2 or 4 parts; `PO015` rows 2, 3 and one of 4 to 7 (or keep all); and for `:154` one row per field and per distinct check (type, missing, hex, name pattern, the pairing rule).
- Resolution: Fixed. `Equals_`, `And`, `Or`, `Not`, `Conversion_is_unsupported_for`, `:154`, `PO015` and `Odd_setting_names` are no longer cut. Row 4 names each `add*` row it cuts; that list has one gap, see R15.

### Low

#### R7. Row 7 hash tests: only `:572` checks that the CLI hashes the real file bytes
- Area: Data row 7
- Finding: The named covers (`DeployPlanTests:846`, `:873`, `DeployPlanFileTests`) use stub hashes `H1` to `H3`; the real hashing is `DeployCommand.cs:878` and `:893`. `DeployCommandTests:572` is the only test that compares a plan's `settingsSha256` with `Sha256Hex(File.ReadAllBytes(path))`, and the plan goldens scrub every 64-hex value. `DeployApplyCommandTests` "changed since the plan" rows do show that a change is noticed, not that the value is the SHA-256 of the file. `:590` (CLI wires the target id into the hash) has no CLI-level cover either.
- Suggested fix: keep `:572` (or its `Sha256Hex` assert in another kept test), and say in the row which test shows the target id reaches the hash through the CLI.
- Resolution: Fixed. Row 7 keeps `DeployCommandTests` `:572` and `:590`; only `:543` and `:551` go, with `DeployPlanTests:846` as cover (the sort and determinism hold there).

#### R8. Row 7 `ProgramTests:120` and `:126` are the only tests that run the gate as a child process on the allow path
- Area: Data row 7
- Finding: `DeployGateCommandTests` runs in process. `:120` (a call that is not an import: exit 0, no output, as a real process with stdin) and `:126` (missing plan file: exit 2, guidance, no path leak) are the only ones with a real `dotnet` child; `:149` (empty stdin) stays, so exit 2 is still shown, but exit 0 with an empty stdout, which a hook relies on, is not.
- Suggested fix: keep `:120`; cut `:126` and `:159` only.
- Resolution: Fixed. `ProgramTests` `:120`, `:126` stay; only `:159` goes, and its parse-error path is the one `:87` takes.

#### R9. Row 6 `ManifestReportTests:42` is the only test of `removed` lines, their order, and "1 file the same"
- Area: Data row 6
- Finding: `manifest-seed-v1-v2.txt` has only `changed` lines and `21 files the same`. `:54` has `2 files the same`, and `:71` has an `added` line only. Reordering the `added`, `removed`, `changed` kinds at `ManifestReport.cs:21`, or dropping the `removed` line, passes after the cut.
- Suggested fix: keep `:42`; cut `:23` only.
- Resolution: Fixed. `ManifestReportTests:42` stays; only `:23` goes, covered by `manifest-seed-v1.txt` (folder line, per-file lines).

#### R10. Cover cells that name a test which does not fail on the same break
- Area: Data rows 6 and 7
- Finding: (a) `DeployPlanReportTests:162` (plannedBy with a value): the named goldens all hold `"plannedBy": null`; the real cover is `DeployCommandTests:519-522` and `McpCommandTests:355`, which the plan does not name. (b) `BaselineCommandTests:36` (no BOM, `}\n`): `BaselineTests` has no BOM check; the cover is `SettingsCommandTests:32` and `SkillsCommandTests:123` (both go through `SafeWrite.Utf8`). (c) The `deploy-apply-dry-run.*` goldens, not `deploy-apply.*`, hold the null `stateAfter` and `appliedAt` of `DeployApplyReportTests:35`. The cuts hold, but the "names the test that still fails" rule of Done when 3 is only as good as the cell.
- Suggested fix: correct the three cells.
- Resolution: Fixed. Row 6's cover cell names `DeployCommandTests:519`, `McpCommandTests:355` and `deploy-apply-dry-run.*`; checked: `:519-522` asserts the key order and the value, the dry-run golden holds null `appliedAt`, `approval` and `stateAfter`. `BaselineCommandTests:36` stays, so (b) no longer applies. New cover-cell slips: see R16.

#### R11. Row 3 wording: these are Facts, and the code drops the pair
- Area: Data row 3
- Finding: `ImportCallTests:436-444` and `:752-756` are `[Fact]`s, not Theories with rows. `ImportCall.cs:98` does not "keep the pair whole": with a high surrogate at index 99 it cuts to 99 characters, so the result has no half pair and is one character shorter than the limit.
- Suggested fix: say "add a Fact beside `:752` that calls `ImportCall.Printable` (public) with 99 characters and a pair after them, and asserts length 99 and no lone surrogate", plus the pair that ends exactly at index 99 (kept, length 100).
- Resolution: Fixed. Row 3 adds a Fact that calls `Printable` with 99 `x` and a pair and says the cut drops the high surrogate (checked against `ImportCall.cs:98`).

#### R12. Cuts leave unused helpers and usings
- Area: Data rows 8, 9, 10, 11 ("a place a change must reach")
- Finding: After row 8 nothing uses `HandoffDocTests.Mention`, `WordLists` and `Unknown` (no other file refers to them) or `using System.CommandLine`. After row 10 `ToolsTests` no longer uses `using System.Diagnostics;`, and `Tools.Bash` is the constant `"bash"`. After row 9 `XmlUnpackReaderTests.Fixture` has no caller if all three tests go. None breaks the build (no analyzer enforces IDE0005 or IDE0051 here), but the aim is less code to keep.
- Suggested fix: name them in rows 8, 9 and 10 so the commits remove them.
- Resolution: Fixed. Row 12 removes what a cut leaves unused, including `using System.Diagnostics` in `ToolsTests` and `XmlUnpackReaderTests.Fixture`; the `HandoffDocTests` helpers now stay with their tests.

#### R13. Row 1: the acceptance rule is loose, and "no new helper" is unclear
- Area: Data row 1, Won't build
- Finding: The test passes if any line of `m55-role-freeze/decisions.md` holds the computed id and the path. A line that quotes an id for a path (for example in a comparison that was rejected) also passes. The blob-id computation and the table reader are new private methods; "no new test helper" should say that private methods inside `RoleEvalTests` are fine. The AGENTS.md line must name the format of the recorded line (path, new blob id, one comparison sentence), or two people will write it differently.
- Suggested fix: require the id and the path to be in the same backtick pair of a line that starts with a fixed word (for example `Role text:`), and state in "Won't build" that private methods in the test class are allowed.
- Resolution: Fixed. Row 1 fixes the form of the `decisions.md` line (date, path, `is now blob`, id) and "Won't build" allows private methods in a test class.

#### R14. Row 9 shrinks check values read from real exports, not only counts
- Area: Data row 9
- Finding: `XmlUnpackReaderTests:386` is not a count: it checks `DefaultValue`, `CurrentValue` and `ValuesFile` of `alm-makers`; the named cover `FixtureRuleTests:39` checks only that PO007 and PO008 fire, so `ValuesFile` has no other check. `FlowDefinitionTests:190` and `:211` pin the child-flow ids, connection references and nested loop read from a real flow; the synthetic tests (`:69`, `:147`) use hand-made shapes, and an absence in `analyze-fixtures.*` cannot show that an id was lost. `ZipReaderTests:30` is the only test of `Managed == false` and `ManifestFile` from a real zip.
- Suggested fix: keep `:386` and the `ChildFlowIds` asserts of `:190` and `:211` (shrink the counts and depth only).
- Resolution: Fixed. `XmlUnpackReaderTests:386`, `ZipReaderTests:30` and `FlowDefinitionTests` `:190`, `:211` stay. The row 9 cuts that remain were checked: `analyze-fixtures.txt` shows each fixture's unique name, version, publisher, prefix, `requires` line and findings (coe PO005, PO009, PO011; alm-sample PO011, PO032, PO033; Contoso PO005), `XmlUnpackReaderTests:38` covers managed, `ZipReaderTests:52` covers a managed zip, and `XmlUnpackReaderTests:199` covers canvas app text. Cover holds.

#### R15. Row 4 leaves `addSeconds` with no check of its multiplier, and no format row
- Area: Data row 4
- Finding: Row 4 cuts the `addSeconds` rows `:957`, `:958` and `:970`. The `addSeconds` rows that stay are `:963` (adds 0, so the multiplier never matters) and the error rows `:978`/`:981`/`:987` (cut or not value-bearing), `:990` and `:993` (overflow). A change of `TicksPerSecond` at `ExpressionFunctions.cs:79` to any other positive value, or to 1, still overflows at `:990` and `:993`, and passes `:963`, so no test fails. The `addDays` rows named as cover cannot catch it, because `Add` takes the multiplier from the caller. The claim "one value row, one format row and one error row per function stay" is also false for `addSeconds` (no format row: `:970` is its only one). Checked and fine: `addHours` keeps `:954`, `:959`, `:960`, `:968`, `:971`; `addMinutes` keeps `:956` and `:969`; the overflow and boundary rows `:988` to `:993` and the cut rows `:953`, `:955`, `:961`, `:962`, `:967`, `:976` to `:978`, `:980` to `:987` and `:998` to `:1000` each repeat an `addDays` row for the branch they take; the five zones of `:1059` are rows of `:1180`.
- Suggested fix: keep `:957` (or `:958`) and `:970` in row 4.
- Resolution: Fixed. Row 4 keeps `:953`, `:955`, `:957` (non-zero steps) and `:967`, `:969`, `:970` (one format row per function). Checked row by row against `ExpressionTests.cs`: the cut rows `:954`, `:956`, `:958`, `:959`, `:962`, `:963`, `:968`, `:971`, `:976` to `:978`, `:980` to `:987` and `:998` to `:1000` each repeat an `AddDays` row (`:794`, `:795`, `:796`, `:803` to `:809`, `:815`, `:817`, `:822` onward); `:975`, `:979`, `:997` and `:960`, `:961` stay; every overflow and boundary row `:988` to `:993` stays. Any wrong multiplier fails `:953`, `:955` or `:957`.

#### R16. Row 4's cover line ranges are off
- Area: Data row 4
- Finding: The `addDays` tests are at `:793-799` (`AddDays`), `:803-809` (`_errors`) and `:814-819` (`_with_a_format`), not `:791-798`, `:799-805` and `:806-812`. The build would find them by name, but the commit message must name the right lines.
- Suggested fix: correct the ranges, or cite the test names only.
- Resolution: Fixed. The covers are named by method line (`:800`, `:810`, `:820`, `:826`, `:833`), and I checked all five.

#### R17. Row 8 `:436` also holds the only ASCII-only check of the plan skill
- Area: Data row 8
- Finding: `Plan_skill_stays_under_its_word_ceiling` asserts every character of `powerops-plan/SKILL.md` is below 128; no other test does that (searched `tests/`). The row says the test pins a count, which is true of the ceiling only.
- Suggested fix: cut only the `InRange` word count and keep the ASCII assert (or say in `decisions.md` that the ASCII rule is dropped).
- Resolution: Fixed. Row 8 cuts the word count only and keeps the ASCII check (test renamed).
