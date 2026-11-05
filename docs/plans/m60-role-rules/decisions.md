# Decisions: m60-role-rules

## Round 1

### Q1. How does PO032 tell an admin role from another role?
1. By the role template ids Learn documents: System Administrator and System Customizer are admin, the other 15 documented templates are known non-admin roles, a role-map id equal to a role's `id` in the solution is that role, and any other id is unknown (recommended)
2. By the role name
3. In your own words
Answer: 1. D6 settled it ("Template ids"), and Learn says template ids are constant in all environments while role ids are not. The scrubbed export's role map holds the two admin template ids (Assumptions), so the design rests on an observed shape. A name can be renamed and differs by language. (answered by: agent m60 build session)

### Q2. Which privileges are the app-opening baseline that PO033 looks for?
1. `prvReadAppModule`, `prvReadUser` and `prvReadUserQuery`: the three whose names Learn prints, for Read on Model-driven App (app visibility), Read User and Read Saved View (minimum privileges); a role that holds any one of them is quiet (recommended)
2. App Opener's whole privilege set
3. Only `prvReadAppModule`
Answer: 1. The brief asks for "none of the baseline privileges", and Learn names these three. Learn doesn't list App Opener's privileges by name, so option 2 would rest on a guess. Firing only when a role holds none keeps the rule quiet on a role that copies part of App Opener; the "may" covers users who hold App Opener or Basic User beside it. (answered by: agent m60 build session)

### Q3. What does PO032 do with a mixed role map?
1. Quiet when any id is a role in the solution or a non-admin documented template; a finding when every id is an admin template; a note when the rest are unknown ids (recommended)
2. A finding whenever an admin template is mapped
3. A finding when the admin ids outnumber the others
Answer: 1. The M60 section's rule is "names only admin templates". An app also shared with a custom role or Basic User is open to non-admin users, which is the fix S1 asked for, so keeping the admin roles beside them is no finding. An unknown id may be a custom role from another solution or Environment Maker, which has no template, so the rule can't tell and says so. (answered by: agent m60 build session)

## Round 2

### Q4. A role file with no `id` or `name`?
1. Refused with the path (`InvalidDataException`), as M46 refuses an app module with no unique name (recommended)
2. Read with null fields, and the rules skip it
Answer: 1. Every observed role has both, so a role without them is a malformed file, and M46's decision Q12 refuses the same kind of gap. The rules then read non-null values without checks of their own. (answered by: agent m60 build session)

### Q5. Which tables does PO034 count as used by a component?
1. The type-1 root components, the type-1 `Required` nodes of `MissingDependencies`, the canvas and code apps' `CdsDependencies` tables, and any table whose name is a substring of a workflow's `ClientData`, all ignoring case (recommended)
2. Only the type-1 root components
3. Option 1 plus the app modules' type-1 components and the site maps' `Entity` targets
Answer: 1. A table the solution carries, or declares as a dependency, is one its components use. Canvas metadata lists the tables an app uses. A cloud flow names its tables by entity set name (`pops_loans`) or logical name, which hold the table name as a substring; matching text can only make the rule quieter, never louder. Option 3 adds nothing: the run's export writes `schemaName="entity"` for its app's tables, and an app's tables are in the solution or its dependencies already. (answered by: agent m60 build session)

### Q6. When does PO034 say "not checked" instead of firing?
1. When a role has Create or Write on a table left over and the solution holds a plugin assembly, a canvas app with no readable `CdsDependencies`, a workflow with no readable definition, or an agent: one note per role, no finding (recommended)
2. Never; fire on every table left over
3. Only for a canvas app with unreadable metadata
Answer: 1. Plugin code, classic workflows and agent tools can write any table, and PowerOps reads none of them for tables. A finding there could be wrong, so the rule says what it couldn't check, as the other rules do for inputs outside what they read. (answered by: agent m60 build session)

### Q7. What goes into the fixture?
1. Only what the three rules need: the two role files with fake ids, the app module without its site map component and web resource id, and a manifest trimmed to the Loan table, the two roles and the app, with the publisher renamed (recommended)
2. The whole unpacked solution, scrubbed
Answer: 1. The brief says copy only what the acceptance needs. The canvas app, the flow, the forms and the views add findings and notes of other rules to every golden and carry more to scrub; the `.msapp` holds the environment URL. Keeping the privileges exactly as exported is what makes the fixture evidence for B-1 and B-3. (answered by: agent m60 build session)

### Q8. The eval baselines will change (PO033 fires on six eval roles). Does the build change the rule to avoid that?
1. No: build the rule as the M60 section asks, run `RoleEvalTests`, and if a baseline changes, stop before `tests/eval` and report (recommended)
2. Scope PO033 to roles an app maps, which skips `solution-01` and `solution-06`
Answer: 1. The brief's step 6 says a changed baseline is the person's decision, and option 2 would still change `solution-07` and `solution-08` and would miss the run's own roles, which no app maps, so the fixture would not fire PO033. Learn says every security role must include a minimum set of privileges, so the finding on the eval roles is correct. (answered by: agent m60 build session)

## Round 3 (from the review)

### Q9. A role read twice? (R8)
1. Keep each role once per id, the first in reading order: folder files in ordinal path order (`X.xml` before `X_managed.xml`), then `customizations.xml` (recommended)
2. Read `X_managed.xml` only when `X.xml` is missing, as the app-module reader does by folder
3. Leave it; no observed layout holds a role twice
Answer: 1. No observed export holds a role twice, but `pac solution unpack` writes `_managed` files for app modules, so a role read twice would double every PO033 and PO034 finding. Role files are not in a folder per role, so option 2 would have to pair file names; keeping the first per id is one line and covers the zip case too. (answered by: agent m60 build session)

### Q2, amended (R2)
The three names stay, but the plan and the PO033 page say which part is Learn's: "Privileges required to view and access apps" names Read on the Model-driven App table, and "Create or edit a security role" names Read User and Read Saved View, in words; the `prv` names are from the App for Outlook troubleshooting page. Holding any one of them silences the rule, which is far weaker than Learn's minimum, and the page says so. (answered by: agent m60 build session)

### Q5 and Q6, amended (R3, R6, R9)
PO034's fix says to check whether the role's users need the privilege before removing it, not to remove it. The plan and the PO034 page say that Create and Write on SharePointData, the role editor's defaults, is expected on most editor-made roles, and that a role copied from App Opener may still give findings on system tables, because App Opener's privileges were not read. The `UserEntityUISettings` exclusion cites "Create or edit a security role", and its privilege names are inferred from the pattern. A role with Create and Write on the Model-driven App table but no `prvReadAppModule` is a listed limit of PO033, not a quiet case: no observed role has that shape. (answered by: agent m60 build session)

### Q1, kept (R7)
The 15 other documented templates count as non-admin roles, `Support` and `Proxy` included. The effect is a missed finding, never a false one, and judging each template's privileges would need lists Learn doesn't give. The PO032 page says so. (answered by: agent m60 build session)

## Log

- Learn checked 2026-11-05 with the Learn MCP tools: "Security roles and templates", "Privileges required to view and access apps", "Create or edit a security role" (minimum privileges), "Configure user security in an environment", the App for Outlook troubleshooting page (privilege names) and the User Settings table reference
- public exports read 2026-11-05 through the GitHub API: microsoft/powercat-automation-kit (MIT) at the commit in Assumptions; the role maps of microsoft/gov-apptemplates and microsoft/Nonprofits for the template ids
- the fixture was scrubbed before the design was settled, and the template ids were confirmed on the scrubbed copy (Assumptions)
- depth: quick: the improvement plan gives the design, D6 answers its one decision
- questions answered by the building session under the user's delegation of 2026-10-22 (autonomous milestone mode); the D6 answer itself is the person's (2026-11-04)
- areas not asked: users, screens, environments, solution and publisher; this is a change to this repo
- approved 2026-11-05 by the building session under the user's delegation of 2026-10-22, for a fresh-context review
- fresh review (a general-purpose subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-11-05 at `132f22b`: 0 High, 3 Medium, 6 Low (R1 to R9); answered in `plan.md` (Done when 1, 3 and 8, Won't build, rows 2, 3, 5, 7, 11 and the new row 13, the PO034 fix, Assumptions, Step 6) and here (Round 3)
- the code built by an implementer subagent (Sonnet, `powerops-implementer`) in this worktree from rows 1 to 10, 12 and 13, test first; then the PO034 note was reworded so it reads for one table too (`<n> of the tables it can create or write are used by no component PowerOps reads`), in the plan, the rule page and the tests
- goldens updated 2026-11-05 by "Update goldens", and the diff read: `rules.*` gain PO032 to PO034; `analyze-fixtures.*` go from 16 solutions and 50 warnings to 17 and 61. `alm-sample` gains PO032 (its app) and PO033 (`ALM Accelerator Sample Role`), and its PO034 note (a plugin assembly) is in the JSON, SARIF and Markdown `notChecked`; each `ColumnLevelSecuritySample` zip, which had no findings, gains PO033 and PO034 (SharePointData, Create and Write); `equipment-checkout` gives PO032 once, PO033 twice and PO034 twice. Every other fixture is unchanged, no SARIF `ruleIndex` moved, and `selftest.txt` was restored
- eval check 2026-11-05 with the built CLI on each case, as `RoleEvalTests.LiveBaseline` runs it: `solution-02` to `solution-05` unchanged; in `solution-01`, `solution-06`, `solution-07` and `solution-08` only the `analyze` section changes, `no findings` and `0 warnings` giving way to one PO033 line per role (1, 1, 2 and 2), exit code 0 as before; `test`, `diff` and `settings validate` unchanged. So the build stops under Done when 8; nothing under `tests/eval` was edited
