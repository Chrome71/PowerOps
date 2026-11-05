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

## Log

- Learn checked 2026-11-05 with the Learn MCP tools: "Security roles and templates", "Privileges required to view and access apps", "Create or edit a security role" (minimum privileges), "Configure user security in an environment", the App for Outlook troubleshooting page (privilege names) and the User Settings table reference
- public exports read 2026-11-05 through the GitHub API: microsoft/powercat-automation-kit (MIT) at the commit in Assumptions; the role maps of microsoft/gov-apptemplates and microsoft/Nonprofits for the template ids
- the fixture was scrubbed before the design was settled, and the template ids were confirmed on the scrubbed copy (Assumptions)
- depth: quick: the improvement plan gives the design, D6 answers its one decision
- questions answered by the building session under the user's delegation of 2026-10-22 (autonomous milestone mode); the D6 answer itself is the person's (2026-11-04)
- areas not asked: users, screens, environments, solution and publisher; this is a change to this repo
- approved 2026-11-05 by the building session under the user's delegation of 2026-10-22, for a fresh-context review
