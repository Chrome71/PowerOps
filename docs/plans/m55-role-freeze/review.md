plan-hash: 71c00e0c0086591816c5e6354da3df38ac6d6759ac01dcacfe248a7ee946ca7b
model: claude-sonnet-5-5
fresh-context: yes

Environment facts were not read: this plan changes the repo, not a solution, and no `plan facts` or `pac` call was made. The product checklist (publisher, connection references, licences, DLP, Dev to Test to Prod, managed or unmanaged, delegation, security roles, mockups) is not applicable and is not scored. Size (checklist item 11) was read as: each row maps to a line of Goal, and `Won't build` exists.

Second pass: read the working tree after the build commit. I built, ran RoleEvalTests (19 of 19 pass), the Core tests (all pass) and the Cli tests (only the two expected `Plugin_skills_are_the_shipped_skills` cases fail), ran `manifest` on both new cases and on scratch folders (CRLF, link, missing path, file as second path), and checked the plan body hash with `plan status`. Not checked: the model runs, cost figures, `dotnet format`, and Windows behaviour.

## Findings

### High

No findings.

### Medium

#### R1. Row 6 misses the "first four lines are exactly" sentence of the Security skill
- Area: Rows 5 and 6 (skill text, consistency between the two skills)
- Finding: Row 5 changes both "The first four lines of `verify.md`" and the "These are the first four lines" sentence of the Verifier. Row 6 says "the header block and line 82 change as in row 5". The Security skill has a second such sentence at line 73 ("The first four lines are exactly:"), directly above the block. Left alone it contradicts the six-line block, and it would sit in the text that is frozen and evaluated, so fixing it later reruns everything.
- Suggested fix: name line 73 in row 6 (and widen the Where range check: the text to change is at 73, 82 and 95, plus the block).
- Resolution: resolved (both skills say "first six lines" at the sentence before the block and after it; row 6 names line 73)

#### R2. The role is not told which files make up a security role, table, column, form, view or app role map
- Area: Rows 5 and 6 (new step 4 text)
- Finding: Step 4 lets a role write "unchanged" only when the manifest comparison lists "none of its files" as added, removed or changed. Nothing says where each component lives. A role could read `Roles/` and `AppModules/` correctly in one run and treat `Other/Customizations.xml` or `Entities/<t>/Entity.xml` as someone else's in the next. A column sits inside the table's `Entity.xml`, and the role map is one element of `AppModule.xml`, so one unrelated edit to the file makes "changed" for the whole file (safe, but it sends the role to re-read). The only link between a component and its files is the role's own knowledge, which is the weakness the change is meant to remove.
- Suggested fix: add one sentence to the shared paragraph that maps each kind to its files in the unpacked layout (roles: `Roles/<name>.xml`; app role map: `AppModules/<name>/AppModule.xml`; table and column: `Entities/<table>/Entity.xml`; forms and views: their folders under the table), and say that a changed `Other/Customizations.xml` also blocks "unchanged" for any of them.
- Resolution: resolved (paragraph B maps roles, app modules and tables to their files and counts a changed Other/Customizations.xml)

#### R3. A zip input leaves the role no way to read the component either
- Area: Rows 5 and 6 (commands list and step 4)
- Finding: The second `manifest` form is skipped when either solution is a zip. Step 4 then says "otherwise read that component's files". The roles run only `powerops` commands, so they cannot open a zip. The result is undefined for a zip: the role cannot compare and cannot re-read. Also, the Verifier records the skip in `## Checks` (not applicable), but the Security skill has no such place and row 6 puts the reason "under the header lines", while row 5 says nothing equivalent for the Verifier's skipped second form; the two skills differ.
- Suggested fix: in the shared step 4 add "With a zip, write that the component is not verified". Make both skills record the skip the same way (the Verifier in `## Checks`, the Security reviewer under the header lines, naming `manifest`).
- Resolution: resolved (paragraph B says a zip means not verified; C2V records the skip in `## Checks`, C2S under the header lines)

#### R4. The diff list in the skills leaves out the classic workflows and business rules sentence
- Area: Rows 5 and 6 (against `COMMANDS.md#diff`, line 49)
- Finding: Line 49 also says classic workflows and business rules are compared by name, category and state only, and that agents are compared by instructions and tools only. The skill list says "flows, canvas apps, and the instructions and tools of Copilot Studio agents" and "does not compare security roles, tables, columns, forms, views or app modules". A role that sees a business rule or classic workflow edit with "no changes" gets no warning that only name, category and state were compared, which is the same over-reading the milestone is meant to stop. Q10 says the list is taken from line 49, so the text should agree with it.
- Suggested fix: add "classic workflows and business rules by name, category and state only" to the list, and say that a `Not compared:` or `note:` line means the part named was not compared.
- Resolution: resolved (paragraph A adds classic workflows and business rules and the `note:` sentence; text matches the plan's quote)

#### R5. The new cases never reproduce the original failure, because the diff is never empty
- Area: Rows 8 and 9 (eval design)
- Finding: The live failure was a role reading an empty `diff` as "roles and app unchanged". In both new cases `Other/Solution.xml` goes from 1.1.0.0 to 1.2.0.0 and the plan changes an existing component, so `diff` always prints changes. The cases test whether a role notices a role or app edit, not whether it resists a "no changes" result. The recorded "unchanged" statements in the reports may be zero for that reason, and a zero would then say nothing about rule (a).
- Suggested fix: either say in the plan that this limit is accepted and `report.md` states it, or make one new case close to the original trap (same version, no plan change in the compared items, role and app edited), with `SolutionCaseProblems` relaxed for it.
- Resolution: resolved (solution-07's diff prints only the version change, checked by running the CLI and in its baseline; solution-08 keeps compared changes)

#### R6. Row 9 does not list the code it has to change, and one cited range is wrong
- Area: Row 9 (`RoleEvalTests`)
- Finding: (1) `HeaderLineProblems` is at lines 448 to 455, not in `:400-430`, which holds the end of `LiveBaseline` and `FrontMatterProblems`. (2) `CountProblems` (lines 156 to 205) requires every flawed case to have exactly `perCase` flaws, so "3 for 01 to 06 and 2 for 07 and 08" cannot pass without changing it and `KeyChecks` (line 484). Neither is in the Where column. (3) The test at line 512 is named "at least 4 of 6" and calls `NotShownProblems(least: 4)`; with 8 flaws per pass it still passes, but the name is false. (4) `Analyze_test_and_settings_validate_report_nothing_on_the_clean_solution_cases` skips keyed cases, so nothing enforces "analyze and settings validate report nothing" on 07 and 08; only the baseline text does. (5) `MarkerProblems` runs on every case file, role and app XML included, and rejects the words flaw, key, keys, baseline, eval and planted; the writer must know that.
- Suggested fix: correct the ranges (448-455, 156-205, 484-493, 512-520), say how `CountProblems` takes a per-case count, add an assertion for point (4) or accept the baseline as the proof, and note (5) for the case writer.
- Resolution: resolved (CountProblems and KeyChecks take per-case counts, ranges corrected, RoleEvalTests pass 19 of 19; the baseline files hold the clean-output proof)

#### R7. Nothing fixes the bar for the milestone's own purpose, and a missed floor has no defined result
- Area: Automation (keep rules, scoring fixed before the first run)
- Finding: The keep rules are M38's: recall of `not-shown` flaws and false Highs on two clean cases. Neither measures what M55 is for. The new-case results and every "unchanged" statement are only "recorded", so after 32 runs a text with false "unchanged" statements could still pass both floors, and the person has no stated threshold. The plan says scoring is fixed before the run, but how a statement is judged false, and what count the person is shown, is open. Also, "a missed floor permits no keeping" does not say what ships then: M38's rule drops the pass, which here would drop a shipped role over a text change, while the command and `plan status` change are already built.
- Suggested fix: define before the run a count of "unchanged" statements about a role, table, column, form, view or app role map that are false against the case files (and a second count of true ones that skipped the comparison), shown beside the keep rules with a stated reading (for example, any false statement blocks a keep recommendation). State what happens on a missed floor: the previous skill text stays, M55 ships the command and the reader only, and the roles are not dropped.
- Resolution: resolved (Automation defines false, unsupported and supported unchanged statements, their reading, and the missed-floor fallback)

#### R8. The run does not record whether the header the roles wrote is right
- Area: Automation (what is recorded) and Done when 3 and 4
- Finding: M62 will compare `solution-hash:` with the disk, and `plan status` now parses `input:`. The run records wrong `state` and stray writes but not whether each report's six header lines have the right shape, whether `solution-hash:` equals the digest of the case's `solution/current` at the freeze, and whether `plan status` reads `input:` as a locator and the hash as 64 hex characters. A role can write `solution-hash: unknown` every time and nothing would show it.
- Suggested fix: add a recorded (not a keep rule) check per valid report: six lines in order, hash equal to `powerops manifest` on the case folder, `input:` accepted by `plan status`; list failures in `report.md`.
- Resolution: resolved (Automation records the six keys, the hash against the freeze build's digest, and the plan status read)

#### R9. The cost cap is stated twice with two numbers
- Area: Automation (Cost, fixed)
- Finding: "The fixed cap is USD 30, below the approved 40" and, in the same paragraph, "If the Claude spend reaches USD 40 before the runs end, they stop and the person is asked." The runs between 30 and 40 are neither allowed nor stopped. Done when and Q7 repeat the same pair. Also the expected total (22) adds the Sonnet scorer to the Claude runs but the cap is described only for "the Claude spend".
- Suggested fix: one number. Either stop and ask at USD 30 (and say the approved range is the room for a rerun), or call 30 the estimate and 40 the stop. Say what the cap covers (runs only, or runs plus scorer).
- Resolution: resolved (one cap, USD 30, covering runs, replacements and the Sonnet scorer, with a stop at it)

### Low

#### R10. Exit codes of `manifest` are given twice and can disagree
- Area: Rows 1 and 3
- Finding: Row 1 makes `Read` throw `InvalidDataException` (message starting with the path) for a link as the path, a missing path and a file that is not a zip. Row 3 wants exit 2 for those. `DiffCommand` maps the same exception to exit 3 and does its own `File.Exists` check for exit 2. If the command only calls `Read`, those cases exit 3. The CLI must pre-check, and the link-as-path check needs the reparse-point attribute.
- Suggested fix: say that `ManifestCommand` checks existence, extension and link before `Read` (as `DiffCommand` does), and keep `InvalidDataException` for unreadable or over-cap input only. Use `TextReport.Clean` on every path in `error:` and `note:` lines (`ErrorLineHolesTests` fails on an unwrapped hole in an `error:` line).
- Resolution: resolved (ManifestCommand pre-checks existence, link, extension and the two-path rule before Read and exits 2; Read errors exit 3; paths go through Clean; checked by running it)

#### R11. The text golden would carry a machine path
- Area: Row 3 (`manifest-seed-v1.txt`, `manifest-seed-v1-v2.txt`)
- Finding: The first line prints `<display>`, the path as given. `Golden.Verify` only normalises line endings. A test that passes the absolute repo path puts a machine-specific path in the golden. Other tests use a `Scrub` step.
- Suggested fix: say how the tests fix the display (scrub the repo root, as the deploy tests do).
- Resolution: resolved (goldens start at powerops-seed/v1, no machine path)

#### R12. A digest of a folder that holds the plan folder changes when the report is written
- Area: Rows 1 and 5 (M62 use)
- Finding: `manifest` hashes every file under the path it is given. If a maker passes the repository folder or a folder that contains `docs/plans/<name>/`, writing `verify.md` changes the digest it recorded. The skill says "the current solution", but nothing says the folder must be the solution's own.
- Suggested fix: add one line to the `COMMANDS.md` section and to the skills: give the solution's own folder, not a folder that holds the reports.
- Resolution: resolved (COMMANDS.md says to give the solution's own folder)

#### R13. Smaller text and reference points
- Area: Done when 2, Data rows 4 and 10, Steps
- Finding: (a) Done when 2 says "both new commands"; there is one command with two forms, and the allowlist entry covers both. (b) Row 4 cites `PlanStatusReport.cs:68`; the JSON `reports` line is 69 (68 is `bodyHash`). (c) A legacy report with a free-text `input:` (the old placeholder allowed spaces and a colon) will now add a warning on every `plan status` run; the doc line should say so. (d) The `plan-status.json` golden holds only a `review.md` report, so the new fields are shown only as null; add a solution report to its fixture to show a real hash. (e) The Update-goldens run (`POWEROPS_UPDATE_GOLDEN=1`) also rewrites the plugin copies of skills; step 2 runs before the skill edit so it is safe, but a refresh at or after step 3 would copy the frozen text into `plugins/`; say so in step 3.
- Suggested fix: apply (a) to (e).
- Resolution: resolved (done-when wording, :69, legacy-warning sentence, golden with a verify.md, step 3 forbids the golden update)

#### R14. A role's run on the other role's case is unscored
- Area: Automation (scoring)
- Finding: Each role also runs on the case keyed to the other (Security reviewer on 07, Verifier on 08). The files hold real flaws for that role too (a widened role is a security flaw; an unplanned role map is a plan deviation). The plan counts the 07 and 08 runs of the other role as unkeyed findings, but does not say so, and the "false High" rule is only for 02 and 06.
- Suggested fix: add one line: a finding on 07 or 08 from the unkeyed role is recorded as unkeyed, not a false High.
- Resolution: resolved (Automation: an unkeyed role's finding on 07 or 08 is unkeyed)

#### R15. Row 9 lets `solution/previous` of the two new cases differ from the seed in `Other/Solution.xml` without limit
- Area: Row 9 (`SolutionCaseProblems`, second pass)
- Finding: For 07 and 08 the check requires every seed file but `Other/Solution.xml` to be byte-equal and compares the root components of the two versions, but nothing checks that the previous `Other/Solution.xml` is the seed's apart from the version and the added role and app root components. A stray edit there would not be caught. Not a validity problem for the run today (both baselines and the manifest comparison look right), a guard for later reruns.
- Suggested fix: optional; compare the previous file with the seed's after removing the version and the added `RootComponent` elements.
- Resolution: resolved. `RoleEvalTests.PreviousManifestProblems` does that for 07 and 08, with good and bad samples, before the freeze

#### R16. Paragraph B says "either solution is a zip" but not "no previous solution"
- Area: Rows 5 and 6 (paragraph B, second pass)
- Finding: The Security reviewer may run with no previous solution (C2S skips the comparison and says so under the header lines), yet B's last sentence names only the zip case. A role with no previous solution reaches "otherwise read the component", which is a sound result, but the wording leaves the case implicit. Text is frozen for the eval, so only worth changing if another change to the text is made anyway.
- Suggested fix: none needed before the rerun; note the wording in `report.md` if a role stumbles on it.
- Resolution: accepted. The result is sound as written, and every case of the rerun has a previous solution; `report.md` notes it if a role stumbles on it (answered by: agent m55-role-freeze)

