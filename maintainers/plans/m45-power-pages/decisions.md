# Decisions: m45-power-pages

## Round 1

### Q1. Which rule ids?
1. PO019 to PO023, leaving PO018 to M44 (recommended)
2. PO018 to PO022, the next free ids, and renumber M44's rule
3. In your own words
Answer: 1. The Phase 7 plan and the BUILD-PLAN row and item 79 name PO018 as M44's rule, and both are signed off; M44 waits on F1, not on a decision to drop it. The recipe's "one after the highest" would make those lines wrong. A gap in `docs/rules/` until M44 is built, or deferred under decision 50, costs nothing.

### Q2. What does the new `Solution` field hold?
1. One generic row per component: id, type, name, site, active, `content` as a JSON object, file; the reader checks the shape and the kinds of the keys the rules read (recommended)
2. Typed records per component type (settings, roles, permissions)
3. In your own words
Answer: 1. The Phase 7 plan asks for one field. A row per component keeps the platform's own form and doesn't add a record per type the rules read. Checking the read keys' kinds in the reader puts every "unknown shape" refusal where the path is known, as the M45 section asks, so the rules read without checks of their own.

### Q3. Do inactive components count?
1. No: only components whose `statecode` is absent or `0` count, and PO022 reads the parent's state to report it (recommended)
2. Yes, all of them
3. In your own words
Answer: 1. Learn's table permissions page says a deactivated table permission becomes ineffective, and that a deactivated parent leaves its children not in effect. It says nothing of inactive site settings or roles; reading them the same way keeps the rules from warning about a row that is off. The Contoso Real Estate export holds four inactive components, so the case is real.

### Q4. What counts as the wildcard?
1. A `fields` value that is `*` after trimming, as Learn's wildcard page identifies it (recommended)
2. Any comma-separated entry that is `*`
3. In your own words
Answer: 1. Learn's page names settings whose value is `*`, and all its examples are whole values. Whether `name,*` is a wildcard isn't stated, and the rule doesn't guess.

### Q5. What does PO020 do when `UseFieldsFromView` is True and no fields setting is set?
1. No finding, and one `note:` line saying it is not checked, because the view's columns aren't read (recommended)
2. Read the "Power Pages Web API Columns" view from the solution's saved queries and check it has a column
3. In your own words
Answer: 1. The M45 section says the rule says "not checked" with no readable view, and decision 49 makes that a note. Reading views is a new reader for both layouts, for one check, and the view often lives with the table outside the site's solution. A later milestone can read it if a user asks.

### Q6. How are a child permission's roles read?
1. A child's effective roles are its own, or, when it has none, its parent's, up the chain. PO021 fires only for a permission with no parent. PO022 compares the child's own roles with the parent's effective roles. PO023 reads effective roles (recommended)
2. Each permission's own roles only
3. In your own words
Answer: 1. Learn says a child's roles are inherited from its parent, and the studio errors when a child has a role its parent lacks. Own roles only would warn about every child without roles (PO021) and miss an anonymous grant through a child (PO023). A chain stops at a parent not in the solution (the note) or one already visited.

### Q7. How does a rule say "not checked"?
1. A small `INotChecked` interface; `Analyzer.NotChecked` collects the lines, `SolutionResult` carries them, and `AnalyzeCommand.Run` prints `note:` lines on stderr, so `analyze`, `baseline` and the MCP tool show them (recommended)
2. Only on the rule pages
3. In your own words
Answer: 1. Decision 49 is decided: one `note:` line on stderr, no finding, no exit-code change. M45 is the first milestone to need it, so it adds the rule-side seam that M46 and M48 can use; M44's note comes from the reader (no `Src` in an `.msapp`), so its own plan decides how that reaches this output. Reports and schemas don't change. The notes have four texts: the `UseFieldsFromView` view (PO020, Q5); a parent, or a permission above it, not found in the solution (PO022, and PO023 for a permission with create, write or delete whose roles come through that chain); and a web role not in the solution on a permission with create, write or delete (PO023), which is decision 49's own example.

### Q8. Is the fixture copied whole?
1. All but the ten `filecontent/` files, the bodies of the site's web files: five CSS files (among them Bootstrap's and USWDS's, under their own licences), three PNG and two SVG, about 316 KB; the kept 126 files are byte-identical (recommended)
2. All 136 files
3. In your own words
Answer: 1. The reader never opens `filecontent/`, and dropping it keeps third-party licences out of the fixture and leaves under a third of its bytes. NOTICE.md records the trim, as it does for `alm-sample/` and `alm-makers/`. The component files that name the web files are kept, so the reader sees every component.

### Q9. How do the CLI tests show the reader runs in both layouts, when the fixture has no finding?
1. The fixture folder and its `SolutionZip` zip are quiet; a temp copy with one added table permission gives the same PO023 finding in both, with the same `File` (recommended)
2. Only the quiet fixture runs
3. In your own words
Answer: 1. A quiet run would pass with no reader at all. The added file is the test's own, with fake ids; the fixture stays unmodified in the repo. The packed copy shows `SolutionZip`'s mapping is pac's for every entry the fixture has.

### Q10. Does the fixture keep the site's primary domain and the `agency.gov` addresses?
1. Yes, unmodified, recorded in NOTICE.md as Microsoft's public sample data without writing the host out again (recommended)
2. Replace them with fake values
3. In your own words
Answer: 1. AGENTS.md keeps public Microsoft samples unmodified, and NOTICE.md already keeps a Microsoft sample host (`powercattools.sharepoint.com`) on the same grounds. The host is in Microsoft's public MIT repo, it isn't this project's tenant, and the leak patterns, built from the person's own tenant, match no file of the fixture. The upstream `site/` folder, which holds an environment URL, is not copied. Plan text and NOTICE.md name "the site's primary domain" rather than the host.

### Q11. What does the reader do with a `parententitypermission` that isn't a string?
1. Nothing: it loads, the value matches no permission, so PO021 stays quiet for it and PO022 prints its "parent not found" note (recommended)
2. Refuse the file, like the other read keys
3. In your own words
Answer: 1. The only public export with permissions has it `null` on all nine, and a code search found no export with it set, so its kind when set is unobserved. A refusal would make every command unreadable on a common setup if the real kind differs; a note says what wasn't checked and keeps the other rules running. The keys whose kinds were observed keep their refusal.

## Log

- 2026-11-03: plan started at the request of the session that started this one: build M45 of Phase 7, build-order item 80, from the M45 section of `docs/plans/phase7-power-platform/plan.md`, its BUILD-PLAN row and decision 49
- read on 2026-11-03: AGENTS.md; the Phase 7 plan's M44, M45 and M46 sections, D10 and its Data; the BUILD-PLAN rows and decisions 40, 49 and 50; the M43 plan folder (layout); the Phase 7 Power Pages research report; `Solution`, `SolutionParser`, `ZipReader`, `XmlUnpackReader`, `SolutionLoader`, `LoadContext`, `AnalysisResult`, `AnalyzeCommand`, `Analyzer`, `RuleHelpers`, `PO011`, `Builders`, `TempFolder`, `SolutionZip`, `tests/fixtures/NOTICE.md`
- gov-apptemplates cloned (sparse, `portals/core-portal`) at `64dabc2` into a scratch folder outside the repo; MIT (repo `LICENSE`); packed with `pac solution pack` (pac 2.14.1) on a copy; the entry list and the byte comparison are in Assumptions. No stop: the zip layout maps to the unpacked one without a guess
- contoso-real-estate-power-platform cloned (sparse, `src/portal/solution`) at `dbd5f76` for shape only; MIT (GitHub licence API and `LICENSE`)
- Learn facts checked 2026-11-03 by a researcher subagent (Sonnet) with the Learn MCP tools; the pages are in Assumptions and go on the rule pages' Sources lines
- today's `analyze` on the fixture: no findings, folder and packed zip; on the Contoso solution: no findings
- the leak patterns matched no file of the fixture
- depth: standard, as the Phase 7 plan's Automation section sets for the readers (first written as quick; changed after review R14)
- questions answered by the building session, under the user's delegation of 2026-10-22 (autonomous milestone mode), with the Phase 7 plan's scope and the master session's brief
- areas not asked: users, automation, environments and ALM, and constraints; the change is to this repo, and the plan answers each or marks it not applicable
- fresh review (general-purpose subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-11-03, on the draft at body hash `0a2ba2f6…`: 0 High, 8 Medium, 6 Low (R1 to R14); its own scripts over the fixture, the packed copy and the Contoso export agree with the plan's counts; neither "Stops if" condition applies, and no eval baseline or pinned role is reached
- the code and the pages were built alongside the review, before sign-off, by an implementer subagent and a docs subagent (both Sonnet) in this worktree, test first (the new tests failed to compile before the types existed); read by the building session
- R1 to R14 answered in `plan.md`, the code, the tests, the pages and here (Q3, Q7, Q8 changed; Q10 and Q11 added); R9 accepted with its reason. `depth` set to `standard`, as the Phase 7 plan's Automation section sets for the readers; the earlier "step 6" reason was M43's
- the same reviewer's second pass 2026-11-03: R1 to R14 hold, 5 new Low (R15 to R19), all fixed: the PR sentence in Step 6, five more tests (an inactive role, a missing permission above the parent, two settings with one key, the note through `baseline` and the MCP tool), a PO023 note behind an unresolved parent with both notes reworded, a constant for a parent that isn't a string, and a count line that names the hidden notes' rules
- the same reviewer's third pass 2026-11-03: R15 to R19 hold, 3 new Low (R20 to R22), all fixed: the BUILD-PLAN line's two placeholders filled, Q7, this Log and row 6 brought up to date, and an allocation-bounded test that fails with the old per-call copy (171 MB) and passes with the constant
- the same reviewer's fourth pass 2026-11-03: R20 to R22 hold, nothing new; no finding open (0 High, 8 Medium and 14 Low in all, every one fixed but R9, accepted)
- approved 2026-11-03 by the building session under the user's delegation of 2026-10-22
- signed off 2026-11-03 by the building session under the user's delegation of 2026-10-22, with no finding open (`powerops plan sign-off`, body hash `64bba91c…`); status set to done with the build in this branch, before the PR's CI, which the master session checks before it merges
