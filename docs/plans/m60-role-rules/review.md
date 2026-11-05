plan-hash: 5dffb0946ab075d15c5a8fbc489100d8ad383dcda46db728dcc5d20b029afbf4
model: claude-sonnet-5-5
fresh-context: yes

Environment facts were not read: this plan changes this repo and targets no environment, so `pac` was not used and `powerops plan facts` was not run. Checklist items 1 to 7 (solution boundaries, connection references, licences, DLP, route, managed or unmanaged, delegation) do not apply; the review covers items 8 to 11 and whether the plan is correct, complete and buildable. Open questions: 0. The plan body hash from `plan status --json` matches the hash above, and the working-tree plan equals the one committed in `132f22b`.

Checked and found fine:
- Learn: the page "Security roles and templates" lists 17 template ids, of which `SystemAdmin` and `Customizer` are the first two, and says they are unique and constant in all environments while role ids are not. The two ids in the fixture's role map equal them ignoring case and braces. "Privileges required to view and access apps" says what the plan quotes (Basic User read only; System Administrator, System Customizer and Environment Maker open every app). The App for Outlook page lists `prvReadAppModule`, `prvReadUser` and `prvReadUserQuery`. "Create or edit a security role" lists the minimum privileges as the plan says.
- The fixture holds what Data row 9 says. It is scrubbed (publisher renamed, no URL, email, user name or environment id; role ids fake; the app's web resource id and site map component removed; the only other GUIDs are the two public template ids). Run unmodified at `132f22b`, `analyze` gives "no findings" on it, so its goldens change only by the new rules. By reading it, PO032 fires once, PO033 twice (no role holds any of the three privileges), PO034 twice (SharePointData, Create and Write, since `pops_loan` is a root component), and no note applies, as Data row 9 says.
- The predicted deltas hold. Existing role-bearing inputs are `alm-sample` (role plus an app mapping only the two admin ids; a type-91 root component, so the PO034 note), the two `ColumnLevelSecuritySample` zips (one role each, no app module, no plugin, so PO033 and a PO034 finding), and the eval cases 01, 06, 07 and 08 (six roles, none holding the three privileges, all Write on `pops_workitem`, a root component; the two apps map their own role ids). No other fixture, zip or eval case, `Contoso_sample` and `polymorphiclookup` included, holds a role or a role map. The baselines are produced by `RoleEvalTests.LiveBaseline` as stdout plus a `stderr:` block, so only PO033 findings change `analyze` there (none of the four cases would emit a note); the `test`, `diff` and `settings validate` sections do not change. None of the keyed flaws in those four cases is about app-opening privileges, so the key's `baseline` fields do not need to change.
- The recipes: rule class, registration, rule page with Sources line, README row, `RuleDocsTests`, reader tests, fixture with `NOTICE.md` and `FixtureRuleTests`, and the golden steps are all covered. No `Role` or `Roles` type exists in `PowerOps.Core.Model` to collide with. The notes follow the `INotChecked` and `Analyzer.NotChecked` pattern of PO025.
- The plan and decisions hold no URL, GUID, email or person's name.

## Findings

### High

No findings.

### Medium

#### R1. Adding the fixture breaks existing tests that the plan does not list
- Area: 9 Testability (Data row 11, Steps 3 and 5)
- Finding: I ran the existing suite on a copy of `132f22b` (fixture present, no code change). Eight tests fail beyond the golden ones the plan names (`ReportTests`): `SolutionDiscoveryTests.Finds_fixture_folders_and_zips_sorted` (a hard-coded list), `AnalyzeCommandTests.Json_output_parses_and_has_sixteen_solutions`, `AnalyzeCommandTests` markdown `StartsWith("## PowerOps analyze\n\n**16 solutions, ")` and its 16-element array check, and `PackedToolTests.Packed_tool_analyzes_fixtures` (16 becomes 17). Row 11 says only the goldens change, and "Update goldens" mode does not fix these. The build would stop at Step 5 with failing tests the plan never mentions.
- Suggested fix: add to row 11 (or a row 13) the edits to these four tests (list, count 17 and the markdown prefix), and say that the "analyze-fixtures" goldens also gain one solution and the summary line changes.
- Resolution: open

#### R2. "The three privileges Learn names for opening a model-driven app" overstates the source
- Area: 8 Security roles (Done when 3, Goal, rule page)
- Finding: The three `prv` names come from the table of the Dynamics 365 App for Outlook troubleshooting page, which lists what that add-in needs. "Privileges required to view and access apps" names only "Read on the Model-driven App table", and "Create or edit a security role" names "Read Saved View" and "Read User" in words, with no `prv` names; both pages say App Opener is how a user opens an app. The mapping of those words to `prvReadAppModule`, `prvReadUserQuery` and `prvReadUser` is the plan's inference (the Assumptions say Learn "doesn't list App Opener's privileges by name", but Done when 3 and the Goal say Learn names them for opening an app). The rule page's "Why it matters" must not state it as Learn's. Also "a role that holds any one of the three is quiet" means the rule is far weaker than Learn's "minimum" (all of them); the page should say so.
- Suggested fix: reword Done when 3 to "the three privileges whose names appear on Learn's App for Outlook page and that match the minimum privileges in words on the create-or-edit page", state in the PO033 page which part is Learn's and which is the inference, and say that holding any one of them silences the rule.
- Resolution: open

#### R3. PO034 will fire on a privilege pair that every observed role-editor role holds
- Area: 8 Security roles, 11 Size (rows 7, 9, 10)
- Finding: Create and Write on SharePointData appear in all four role-bearing exports read (the run's two roles, `alm-sample`, both `ColumnLevelSecuritySample` zips); only `alm-sample` is quiet, and only because it has a plugin assembly. So PO034 in practice flags every role made in the editor, whatever it does with the app. That is what S2 asked for, but the plan never says it, the Fix tells the maker to "remove" privileges that the plan has not checked can be removed, and nothing says whether PO033's own Fix ("copy the role from App Opener") avoids the PO034 finding: App Opener's privilege list is not known, and a role copied from it may carry Create or Write on system tables other than the one excluded (`UserEntityUISettings`). The create-or-edit page also says a new role includes App Opener privileges by default, which the observed roles evidently lack.
- Suggested fix: say in the plan and in the PO034 page that SharePoint Data Create and Write is expected on editor-made roles and is a "may", reword the Fix to "review", not "remove", and add to Questions or Won't build that PowerOps has not checked what App Opener holds, so a role copied from it may still give a PO034 finding on a system table.
- Resolution: open

### Low

#### R4. A stop under Done when 8 leaves one test failing on the branch
- Area: 9 Testability (Done when 8, Step 6)
- Finding: The four baselines change (six PO033 findings), so `RoleEvalTests.Every_solution_case_has_a_baseline_file_that_matches_the_live_output` fails once PO033 exists, and the plan forbids editing `tests/eval`. The plan says "commit locally", but not that this one test is the expected failure, that `dotnet test` therefore cannot be clean, or that no PR is opened.
- Suggested fix: add to Step 6 and Done when 8 that the report names that test as the one expected failure, lists the exact `analyze` lines per case (I predict: PO033 warning lines for 01, 06, 07 (two roles) and 08 (two roles), "no findings" replaced, "1 warning" and so on), and that no PR is opened.
- Resolution: open

#### R5. Done when 1 and Done when 7 read as contradicting
- Area: 10 Unanswered areas
- Finding: Done when 1 says "the existing rules give the same findings on every solution as before"; the Data section expects the goldens to change on four solutions. It means PO001 to PO031, but as written a checker could read it as "no golden changes".
- Suggested fix: write "PO001 to PO031 give the same findings".
- Resolution: open

#### R6. `UserEntityUISettings` rests on a weak Learn source
- Area: 8 Security roles (Assumptions, row 7)
- Finding: Learn's User Settings reference mentions the table only inside a column description; it gives no privilege name. The exclusion rests on the create-or-edit page ("Create/Read/Write User Entity UI Settings"), and the privilege names `prvCreate…` and `prvWriteUserEntityUISettings` are inferred from the `prv<verb><table>` pattern.
- Suggested fix: cite the create-or-edit page for the exclusion, and say the privilege name is inferred from the pattern.
- Resolution: open

#### R7. PO032 treats every other documented template as a non-admin user role
- Area: 8 Security roles (row 5, Q1, Q3)
- Finding: Of the 15 "non-admin" templates two (`Support` and `Proxy`) are service roles that no user is shared an app through, so an app mapped to {SystemAdmin, Support} is quiet though it is shared with admins only in practice. The effect is a missed finding, never a false one.
- Suggested fix: keep it, and say on the rule page that the 15 templates all count as "other roles" without judging their privileges; or count only the user-facing templates.
- Resolution: open

#### R8. `_managed` role files and duplicates are not covered
- Area: 9 Testability (rows 2 and 3)
- Finding: The app-module reader prefers `AppModule.xml` over `AppModule_managed.xml` so a folder holding both reads once (`FolderFiles`). Row 2 matches `Roles/<file>.xml` ignoring case, with no such rule; whether `pac solution unpack --packagetype Both` writes a `_managed` role file was not observed, and if it does each role would be read twice and each PO033 and PO034 finding repeated. The same role can also appear in both `Roles/` and `customizations.xml`.
- Suggested fix: add a reader test (a role in both places, and a `Name_managed.xml` file) and either dedupe by role id in the reader or note in Assumptions that no observed layout holds both.
- Resolution: open

#### R9. A role that can create and write Model-driven App rows opens every app
- Area: 8 Security roles (row 6)
- Finding: "Privileges required to view and access apps" says a user with Create and Write on the Model-driven App table is a maker who can view and open all apps. PO033 checks only for Read-type privileges, so a role with `prvCreateAppModule` and `prvWriteAppModule` but no `prvReadAppModule` would still fire (unusual, since Read normally goes with them).
- Suggested fix: either treat `prvCreateAppModule` plus `prvWriteAppModule` as quiet, or list it in the PO033 page as a known limit.
- Resolution: open
