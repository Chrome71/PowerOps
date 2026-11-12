plan-hash: 060b49fdf1603c929eebf42669afb97f20577dc4d48eb8ee366d4607178e8a3d
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read. The request did not say the person confirmed a signed-in `pac`, so `powerops plan facts` was not run, and no `pac`, `az` or tenant call was made. `powerops` was run as `dotnet run --project src/PowerOps.Cli --`. This is a new review from scratch, and it replaces the review of plan-hash `2b360bf`. HEAD is `8338c5a`, which differs from `b065f23` only in this plan folder, so the Data rows were checked against the files at HEAD. The line numbers and quoted current text match: `SKILL.md:5`, `:36-46`, `:40`, `:87`, `:117-145`, `:130`, `:139`, `:152`, `:168-174`, `:186`, and the cross-references at `:170`, `:173`, `:174` and `:208`; `references/data.md:33`; `powerops-review-plan/SKILL.md:37`; `docs/PLANNING-SKILLS.md:35`, `:153` (which holds the 2,873 ceiling), `:155`, `:157-174`, `:178`, `:186`, `:212`, `:222`, `:227` and `:249`; `README.md:21` and `:109`; `SkillsCommandTests.cs:462` (2,873 plus 470 is 3,343), `:597` (the install test's list) and `:621`; smoke `:36-38` and `:249`. Every phrase row 12 pins appears in the quoted wording of rows 1 to 9. The new reference ships without a project change, because the CLI embeds `skills/**/*`. `PlanFactsSkillTests` still passes against the planned text: step 8 doesn't name `powerops plan facts`, and the new fast-path text has no "environment facts" read. The compatibility rows of row 11 fit the six columns `EcosystemDocsTests` checks. Tools the steps name were found: `dotnet`, `claude`, `jq`, `git` and `bash`; `reports/leakscan.sh` and the cited `reports/trial-3/` files are in the main checkout, which ignores `reports/`, as step 6 says. Checklist items 1, 2, 4, 5, 6, 7, 8 and 11 don't apply: the plan changes skill text, docs and tests, and names no solution, connector, environment, role, table, flow or screen to build.

The earlier findings, checked against the plan as it stands:

- R1 (the quoted wording against the word budget): closed in substance. The ceiling is now 3,343, from a counted draft, and Data lets the executor shorten the wording as long as it keeps the pinned phrases. The quoted text is still about 60 words over (R2 below).
- R2 (`quick` keeps the defaults): closed. Row 4 says "Under `quick`, ask nothing: the approval keeps them", and row 12 (d) pins it.
- R3 (two triggers for the table): closed. Goal 4, row 2 and row 9 use the same list, and row 12 (b) pins it. The new "No need" line brings a different mismatch (R1 below).
- R4 (free, trial and developer part numbers): closed. Row 7 gives section 3 a Covers column and lists those plans as themselves, covering no row; row 12 (f) pins "covers no row".
- R5 (a read never settles a standard row): closed. The table now holds only needs beyond Microsoft 365's standard rights, so standard rows are not in it.
- R6 (the `Held` values and tier rules unpinned): closed. Row 12 (f) pins the five values and each tier's rule.
- R7 (the cost tier as a default under `quick`): closed for `quick`. Row 3 now makes the tier never a default at any depth, but the fast path writes it as one (R3 below).
- R8 (the neutral question after a successful read): closed. Row 1 says "After a failed read".
- R9 (small mismatches): closed. Row 7's wording matches its pin, row 14 and step 4 name `PluginManifestTests`, and the fake `pac` is at `:36-38`.
- R10 (doc lines left out): closed. Row 10 names `PLANNING-SKILLS.md:155`, `:212` and `:222`.
- R11 (the leak scan and reports): closed. Step 6 names `reports/leakscan.sh` and says it runs from the main checkout; the files are there.
- R12 (URLs): accepted by the building session. The same content is still there, so this role reports it again (R4 below).

## Findings

### High

No findings.

### Medium

#### R1. The "No need" line has no `### Licences` heading rule, so the smoke and the review item disagree with Goal 4
- Area: 3. Premium connector and licence impact; 9. Testability
- Finding: Goal 4 and row 2 say a plan carries a `### Licences` table, "else the line "No need beyond Microsoft 365's standard rights."". They don't say whether the `### Licences` heading stays above that line. Row 9 makes "no `### Licences` table under `Constraints`" a Medium finding. That doesn't exempt the line, so a reviewer would flag a plan that followed Goal 4. Row 13 fails scenarios 5 and 6 when `^### Licences` is missing. Both scenarios plan a canvas app over new SharePoint lists, and the recommended tier keeps that standard. So the line is the expected outcome there, and an agent that writes it without the heading fails the smoke. Row 5 also has the fast path always write "a first step that checks the held licences", even when there is no row to check.
- Suggested fix: In row 2 and Goal 4, say the `### Licences` heading is always written, holding either the table or the line. In row 9, write "no `### Licences` heading under `Constraints`", or say the line counts as the table. In row 5, write the check step only when a row exists. Pin the heading rule in row 12 (b).
- Resolution: fixed (the heading always stays and holds the "No need" line when nothing needs more; row 9 accepts that line; the fast path writes its check step only when the table has a row; rows 2, 5, 9 and 12 pin it)

### Low

#### R2. The quoted wording is still about 60 words over the new ceiling
- Area: 9. Testability (step 3's done-condition)
- Finding: `SKILL.md` has 2,869 words, so the ceiling of 3,343 leaves 474. Counted from the quoted text of rows 1 to 6, the new wording adds about 560 words and replaces about 25, a net gain of about 535. Written as quoted, step 3 fails `wc -w` and the ceiling test by about 60 words. Data allows shortening and cites a tighter draft (461 words), but that draft is not in the plan.
- Suggested fix: None is required before the build. Tell step 3's executor to expect a cut of about 60 words that keeps every row 12 phrase, for example the repeated guards in row 1 that `licensing.md` section 1 already holds.
- Resolution: fixed (the ceiling is 3,363 from Fable's counted draft of 494 words; the executor trims connective wording only)

#### R3. The fast path writes the cost tier as a default, which row 3 forbids
- Area: 10. Unanswered areas
- Finding: Row 3 says "The cost tier, `Held`, the environments question, security roles and the data round's questions are never defaults". Row 5 has the fast path write `default: Not sure: standard first` under Assumptions, and has A or B keep it. The skill would contradict itself. An agent that follows row 3 could ask the tier in the fast path, where step 3 says "Ask nothing else". Or it could leave the tier out of Assumptions.
- Suggested fix: In row 3, write "The cost tier outside the fast path, ...". Or in row 5, write the tier as a plain Assumptions line, not a `default:` line.
- Resolution: fixed (the fast path writes `cost tier: Not sure: standard first (not asked)` under Assumptions, not a `default:` line)

#### R4. The plan and decisions.md hold URLs
- Area: Tenant data
- Finding: Row 1 in `plan.md` spells out the Graph host and path, without the scheme. The research line of the `## Log` in `decisions.md` holds about ten host-and-path references to public Learn and GitHub pages. They are public documentation, not tenant values, and `plan status` reports no violation. The earlier R15 and R12 on the same content were accepted, but this role reports every URL.
- Suggested fix: Keep the earlier acceptance, or replace them with page titles and keep the paths in `licensing.md`'s Sources and the compatibility rows.
- Resolution: accepted (public documentation hosts, not tenant values; `plan status` shows no violation; the Graph URL in row 1 is written without its scheme)
