plan-hash: 38b0b6c060841a77f83bb73fc859f42185b0620552608da1a2c5fffa989f3de2
model: claude-sonnet-5-5
fresh-context: yes

Environment facts were not read: this plan changes this repo and targets no environment, so `pac` was not used and `powerops plan facts` was not run. Checklist items 1 to 7 (solution boundaries, connection references, licences, DLP, route, managed or unmanaged, delegation) do not apply; the review covers items 8 to 11 and whether the plan is correct, complete and buildable. Open questions: 0. The plan body hash from `plan status --json` matches the hash above, and the working-tree plan equals the one committed in `132f22b`.

Third pass, at `cd94770` (after the person's Q10 and Q11 answers; the plan body hash above matches `plan status --json`). Checked in this pass:
- Tests in a scratch copy of `cd94770`: 3928 run, 0 failed, 3923 passed, the rest skipped; `dotnet format --verify-no-changes` is clean. The built CLI on `solution-07` prints the two PO033 lines of the new baseline.
- `git diff --stat 0fb8f3d cd94770` under `tests/eval`, `skills/` and `plugins/` shows only `tests/eval/roles/baseline/solution-07.txt` and `solution-08.txt`, each changed in its `analyze` section only (`no findings` and `0 warnings` become two PO033 warning lines and `2 warnings`); no version file or changelog changed. The cases, the key, the pilot, `run.sh` and the other baselines are untouched, as row 14 and Won't build say.
- Code: PO033 gates on `solution.AppModules.Count > 0`; its note names only PO034 without an app; `WithRoles` now adds an app and a new test covers the no-app case. The goldens lose exactly the two PO033 lines of the two `ColumnLevelSecuritySample` zips (text, Markdown, JSON and SARIF; the SARIF `ruleIndex` values of the other results do not move), and the warnings go from 61 to 59. The PO033 and PO034 pages carry the gate and the note wording, and say a role whose app is in another solution is missed, never wrongly reported.
- The rerun section, read against `docs/plans/m55-role-freeze/plan.md` and `report.md`: the 16 + 16 runs, the freeze, the blind scoring by two scorers, the keep rules (15% of `not-shown` flaws pooled, at most one false High on `solution-02` and `solution-06`, denominator at least 5) and the cost cap (USD 30; M55 spent about USD 8 of its Claude estimate, so the cap is safe) are carried over correctly. Dropped on purpose and consistent: M55's own-new-case result and the "unchanged statements" count, which belong to M55's new skill text, which this milestone does not change. The key's `baseline` fields stay right: no keyed flaw of `solution-07` or `solution-08` is about app-opening privileges.

Second pass, at `3d2e032` (code, tests, rule pages and goldens built). The plan body hash is the one above (confirmed with `plan status --json`; `review` is still `pending`, so `status` calls this review stale until it is set). Checks of this pass:
- Tests in a scratch copy of `3d2e032`: 3927 run, 3920 pass, 2 fail, and the two are exactly `RoleEvalTests.Every_solution_case_has_a_baseline_file_that_matches_the_live_output` and `RoleEvalTests.Analyze_test_and_settings_validate_report_nothing_on_the_clean_solution_cases`, the expected stop under Done when 8. `dotnet format --verify-no-changes` is clean.
- `git diff --stat 0fb8f3d 3d2e032` touches nothing under `tests/eval`, `skills/` or `plugins/`, no version file and no changelog.
- Code against the plan: the titles, messages, fixes and the three note texts of PO032 to PO034 equal the plan's tables character for character (the same strings are in the rule pages and the goldens). `SecurityRoles` holds 17 template ids that equal Learn's list. Reading in `SolutionParser` is path-ordinal, with `DistinctBy` on the normalized id, so `X.xml` precedes `X_managed.xml` and the folder precedes `customizations.xml`. PO032 sorts apps by file, ignores case and braces on both sides, and its finding and note conditions cannot both hold. PO034 matches the used set ignoring case, takes the first spelling of a table in file order, orders tables ordinal-ignore-case, names the verbs per table, and gives findings or one note per role, never both. I found no false-finding or ordering bug.
- Goldens: `rules.*` gain three entries; `analyze-fixtures.*` go from 16 solutions and 50 warnings to 17 and 61, which is 2 (alm-sample) + 5 (equipment-checkout) + 4 (two zips); the PO034 note of alm-sample is in the JSON, SARIF and Markdown `notChecked`; no other fixture changed. This equals the Log line.

Checked and found fine at the first pass:
- Learn: the page "Security roles and templates" lists 17 template ids, of which `SystemAdmin` and `Customizer` are the first two, and says they are unique and constant in all environments while role ids are not. The two ids in the fixture's role map equal them ignoring case and braces. "Privileges required to view and access apps" says what the plan quotes (Basic User read only; System Administrator, System Customizer and Environment Maker open every app). The App for Outlook page lists `prvReadAppModule`, `prvReadUser` and `prvReadUserQuery`. "Create or edit a security role" lists the minimum privileges as the plan says.
- The fixture holds what Data row 9 says. It is scrubbed (publisher renamed, no URL, email, user name or environment id; role ids fake; the app's web resource id and site map component removed; the only other GUIDs are the two public template ids). Run unmodified at `132f22b`, `analyze` gives "no findings" on it, so its goldens change only by the new rules. By reading it, PO032 fires once, PO033 twice (no role holds any of the three privileges), PO034 twice (SharePointData, Create and Write, since `pops_loan` is a root component), and no note applies, as Data row 9 says.
- The predicted deltas hold. Existing role-bearing inputs are `alm-sample` (role plus an app mapping only the two admin ids; a type-91 root component, so the PO034 note), the two `ColumnLevelSecuritySample` zips (one role each, no app module, no plugin, so PO033 and a PO034 finding), and the eval cases 01, 06, 07 and 08 (six roles, none holding the three privileges, all Write on `pops_workitem`, a root component; the two apps map their own role ids). No other fixture, zip or eval case, `Contoso_sample` and `polymorphiclookup` included, holds a role or a role map. The baselines are produced by `RoleEvalTests.LiveBaseline` as stdout plus a `stderr:` block, so only PO033 findings change `analyze` there (none of the four cases would emit a note); the `test`, `diff` and `settings validate` sections do not change. None of the keyed flaws in those four cases is about app-opening privileges, so the key's `baseline` fields do not need to change.
- The recipes: rule class, registration, rule page with Sources line, README row, `RuleDocsTests`, reader tests, fixture with `NOTICE.md` and `FixtureRuleTests`, and the golden steps are all covered. No `Role` or `Roles` type exists in `PowerOps.Core.Model` to collide with. The notes follow the `INotChecked` and `Analyzer.NotChecked` pattern of PO025.
- The plan and decisions hold no URL, GUID, email or person's name.

## Findings

### High

No findings.

### Medium

No findings.

### Low

#### R12. The rerun is defined by pointers into M55's plan, which in turn points at M38
- Area: 9 Testability (Automation, "The rerun")
- Finding: The section says the roles run "as M55's plan sets it out" and lists differences, so the executing session must read M55's plan and, for failures and replacements and the keep rules, M55's "as M38 set them". Everything needed is there, and nothing contradicts it, but the runner would have to carry over by hand these items that the new section does not restate: run order (Verifier first, one run at a time per client, same case order on both clients), client and model versions recorded and compared with M55's report (the `opus` and `gpt-6-astra` aliases may point at newer models than in M55, which affects comparability), the 30-minute limit, one valid run per case, pass and family, the replacement rule (one replacement for an infrastructure failure or a read outside the temp folder, none for a poor run), and that `report.md` calls its conclusions provisional.
- Suggested fix: add one line listing these as carried over, and a line saying that `report.md` records the model ids and client versions next to M55's.
- Resolution: open

#### R13. The decision after a missed floor has no stated options
- Area: 10 Unanswered areas (Done when 9, "A missed floor")
- Finding: M55's missed floor had a lever: the role's new skill text went back to its M39 pin. Here the skill text does not change, so a missed floor has nothing to roll back, and the plan says only that it "stops for the person". Without options the person decides ad hoc, and the build session cannot tell what each result means for the PR (for example, whether PO033's gate or the two baselines would change, or the PR ships regardless).
- Suggested fix: state in the plan what a missed floor can lead to (for example: ship as it is and record it; change PO033 and rerun the affected passes; or drop the baseline change) and that a passed floor also permits, not requires, merging.
- Resolution: open

### Resolved from the second pass

#### R10. PO034's Sources line cites a page the Log doesn't list as checked
- Area: 9 Testability (row 10, the recipe's Sources rule)
- Finding: `docs/rules/PO034.md` cited "Security roles and privileges for Dataverse" with `verified: 2026-11-05`, which the Log didn't list.
- Suggested fix: read it and log it, or drop it.
- Resolution: fixed: decisions R10 (Round 4) records that page and "Verifying access in code" as read on 2026-11-05, with what each supports; the PO034 Sources line cites both and the two Learn pages in the PO033 and PO034 pages use them for the naming pattern.

#### R11. The PO034 note reads wrongly for one table
- Area: 9 Testability (note text)
- Finding: "1 of the tables it can create or write are used by no component" does not agree in number.
- Suggested fix: reword or accept.
- Resolution: accepted: decisions R11 keeps one sentence for every count, as PO025's note does (`<n> of its custom pages have`); the text is unchanged in the plan, page, code and goldens.

### Resolved from the first pass

#### R1. Adding the fixture breaks existing tests that the plan does not list
- Area: 9 Testability (Data row 11, Steps 3 and 5)
- Finding: Eight existing tests (`SolutionDiscoveryTests`, three `AnalyzeCommandTests`, `PackedToolTests`) hard-code 16 solutions or the fixture list.
- Suggested fix: add them to the plan.
- Resolution: fixed: row 13 lists them. The build changed the discovery list, the three count checks and the packed-tool count to 17, moved `Clean_zip_exits_zero` to `polymorphiclookupexamplesolution.zip`, and changed `McpCommandTests.Baseline_returns_the_file_the_cli_writes`. All pass in the scratch run.

#### R2. "The three privileges Learn names for opening a model-driven app" overstates the source
- Area: 8 Security roles (Done when 3, Goal, rule page)
- Finding: The `prv` names come from the App for Outlook page, the others name them in words, and "any one of the three" is weaker than Learn's minimum.
- Suggested fix: say which part is Learn's and that any one silences the rule.
- Resolution: fixed: Done when 3, Assumptions and decisions Q2 (amended) say it, and `docs/rules/PO033.md` has a "Learn's part" paragraph and a "Limits" paragraph with the same words.

#### R3. PO034 will fire on a privilege pair that every observed role-editor role holds
- Area: 8 Security roles, 11 Size (rows 7, 9, 10)
- Finding: Create and Write on SharePointData are on every observed editor-made role, the fix said "remove", and a role copied from App Opener may still fire.
- Suggested fix: say it is expected, reword the fix to "check", list App Opener as not read.
- Resolution: fixed: the fix text is "Check whether the role's users need to create or write that table, and remove those privileges from the role if they don't." in the plan, rule, page and goldens; Assumptions, Won't build and the PO034 page say it is expected and that App Opener was not read.

#### R4. A stop under Done when 8 leaves one test failing on the branch
- Area: 9 Testability (Done when 8, Step 6)
- Finding: The baseline test fails once PO033 exists; the plan did not say so.
- Suggested fix: name it as the expected failure and open no PR.
- Resolution: fixed: Done when 8 and Step 6 name `Every_solution_case_has_a_baseline_file_that_matches_the_live_output`, and `Analyze_test_and_settings_validate_report_nothing_on_the_clean_solution_cases` for `solution-06`, as expected failures, and open no PR. The scratch run fails exactly those two. The Log lists the per-case lines (1, 1, 2, 2 PO033 lines; the other four cases unchanged). Superseded at the third pass: Q10 and Q11 replaced the stop (PO033 now checks only solutions with an app, the two baselines are updated, and the full rerun is part of the milestone); the suite is green at `cd94770`.

#### R5. Done when 1 and Done when 7 read as contradicting
- Area: 10 Unanswered areas
- Finding: "The existing rules give the same findings" could read as "no golden changes".
- Suggested fix: write "PO001 to PO031".
- Resolution: fixed: Done when 1 reads "PO001 to PO031 give the same findings on every solution as before".

#### R6. `UserEntityUISettings` rests on a weak Learn source
- Area: 8 Security roles (Assumptions, row 7)
- Finding: Learn's User Settings page doesn't give the privilege name.
- Suggested fix: cite the create-or-edit page and say the names are inferred.
- Resolution: fixed: row 7 and the PO034 page cite "Create or edit a security role" and say the privilege names are inferred from the pattern (Assumptions still names the User Settings reference for the table name, which is true of that page).

#### R7. PO032 treats every other documented template as a non-admin user role
- Area: 8 Security roles (row 5, Q1, Q3)
- Finding: `Support` and `Proxy` are service roles, so {SystemAdmin, Support} is quiet.
- Suggested fix: keep it and say so on the page.
- Resolution: accepted: row 5, decisions Q1 (kept) and `docs/rules/PO032.md` say it is a missed finding, never a false one. The code (`IsTemplate` makes any of the 15 an "other" role) matches.

#### R8. `_managed` role files and duplicates are not covered
- Area: 9 Testability (rows 2 and 3)
- Finding: A role in `X.xml`, `X_managed.xml` and `customizations.xml` could be read three times.
- Suggested fix: dedupe by id and test it.
- Resolution: fixed: `DistinctBy(r => r.Id)` after an ordinal path sort and the `customizations.xml` roles (row 2, decisions Q9) and `RoleReaderTests.A_role_in_a_managed_file_and_in_customizations_is_read_once_from_the_first_file`.

#### R9. A role that can create and write Model-driven App rows opens every app
- Area: 8 Security roles (row 6)
- Finding: PO033 would fire on a role with Create and Write on the Model-driven App table and no Read.
- Suggested fix: make it quiet or list it as a limit.
- Resolution: accepted: a listed limit in Won't build and on the PO033 page ("Limits"), because no observed role has that shape; the code does not special-case it.
