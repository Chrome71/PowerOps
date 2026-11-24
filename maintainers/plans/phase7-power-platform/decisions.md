# Decisions: power-platform-plan

The named decisions from the research standard and the review, as question rounds for the person. Asked on 2026-11-02, after the review agreed; the person took the recommended option on each.

## Round 1

### Q1. Does an output change in `analyze`, `diff`, `test` or `settings validate` rerun the pinned roles, and does the ledger's change to `plan.md`'s layout count as an input change, for both `powerops-verify` and `powerops-review-security`? (D1)
1. Adopt the ruling: a change that reaches no eval baseline needs no rerun, one that does needs one; and the ledger is not an input change (recommended)
2. Adopt the ruling, and the ledger is an input change: M41 runs the full pass of both roles (24 runs) with one solution case's plan in the ledger layout, and renews the decision
3. Every new rule reruns both pinned roles, whatever it reaches
4. In your own words
Answer: Adopt the ruling: a change that reaches no eval baseline needs no rerun, one that does needs one; and the ledger is not an input change. The recommended option, taken by the person on 2026-11-02.

### Q2. Does `deploy gate` stay a deny-list, extended with M40's routes and hook matchers, or become an allow-list? (D2)
1. Deny-list, with M40's routes, the matchers and the docs' list of what it can't see (recommended)
2. Allow-list of reads, planned as its own milestone, with M40 keeping only the PO005 fix and docs
3. In your own words
Answer: Deny-list, with M40's routes, the matchers and the docs' list of what it can't see. The recommended option, taken by the person on 2026-11-02.

### Q3. Does PO016 adopt 50 actions and 4 levels from the CAT OverFlow skill, with the rule page naming the provenance? (D3)
1. Yes, as a warning (recommended)
2. No: keep 50 and 4 as a review item only, and M43 ships the nested-loop rule alone
3. In your own words
Answer: Yes, as a warning. The recommended option, taken by the person on 2026-11-02.

## Round 2

### Q4. Power BI's visual-count review threshold? (D4)
1. 12 data visuals a page, Microsoft's skill's number, as a review item (recommended)
2. 15, the community rule set's number
3. 20, the tool default
4. In your own words
Answer: 12 data visuals a page, Microsoft's skill's number, as a review item. The recommended option, taken by the person on 2026-11-02.

### Q5. Is the held `pbip check` ever built? (D5)
1. Not now; revisit when someone produces a PBIP, and run `powerbi-report-author validate` first to see whether it already checks field references (recommended)
2. Plan it now, with the Windows machine producing the PBIP
3. Never; Power BI stays skills and docs
4. In your own words
Answer: Not now; revisit when someone produces a PBIP, and run `powerbi-report-author validate` first to see whether it already checks field references. The recommended option, taken by the person on 2026-11-02.

### Q6. Do the oversized and compact plan cases join `tests/eval/roles`? (D6)
1. Yes, in M41: `plan-13` oversized and `plan-14` compact, through the existing `review-plan` pass, once per supported client, scored for planted lists found and for false Highs (recommended)
2. No; the ledger ships unmeasured
3. In your own words
Answer: Yes, in M41: `plan-13` oversized and `plan-14` compact, through the existing `review-plan` pass, once per supported client, scored for planted lists found and for false Highs. The recommended option, taken by the person on 2026-11-02.

## Round 3

### Q7. The agent fixture for M48? (D7)
1. Pack the MIT workspace fixture from the vscode-copilotstudio repo with `pac copilot pack`, local and unmodified, if it holds no tenant values (recommended)
2. Scrub Microsoft's order-management zip, and accept that it breaks "public samples stay unmodified"
3. Build an own agent fixture by hand in tests only, no committed fixture
4. In your own words
Answer: Pack the MIT workspace fixture from the vscode-copilotstudio repo with `pac copilot pack`, local and unmodified, if it holds no tenant values. The recommended option, taken by the person on 2026-11-02.

### Q8. Run one canvas build with and without a prototype, in Dev, through `canvas-app`? (D8)
1. Yes, once, after M42 and while Dev is available, with the person saving and publishing (recommended)
2. No; the prototype stays skippable and unmeasured, and no control map is ever added
3. In your own words
Answer: Yes, once, after M42 and while Dev is available, with the person saving and publishing. The recommended option, taken by the person on 2026-11-02.

### Q9. How do the `diff` schema's closed enums grow when M49 adds agent tools and instruction risks? (D9)
1. Bump `diff`'s `schemaVersion` to 2 with the first new value: a consumer that validates against the published schema updates it once (recommended)
2. Keep `schemaVersion` 1 and widen the enums: such a consumer rejects output that holds agent items until it refreshes the schema, so the schemas README carries a migration note
3. Keep agents out of `diff` and drop that part of M49
4. In your own words
Answer: Bump `diff`'s `schemaVersion` to 2 with the first new value: a consumer that validates against the published schema updates it once. The recommended option, taken by the person on 2026-11-02.

## Round 4

### Q10. What does `analyze` say when a check's input isn't in the export? (D10)
1. Two cases: evidence the export layout doesn't carry says "not checked" (one `note:` line on stderr, no finding, no exit-code change); a reference of a kind the layout carries that neither the solution nor a declared dependency holds is an Error, as PO001 and PO004 are today (recommended)
2. "Not checked" for every absence and no Error on one, which turns M46's site-map Error into a note
3. A warning finding that names the missing input
4. In your own words
Answer: Two cases: evidence the export layout doesn't carry says "not checked" (one `note:` line on stderr, no finding, no exit-code change); a reference of a kind the layout carries that neither the solution nor a declared dependency holds is an Error, as PO001 and PO004 are today. The recommended option, taken by the person on 2026-11-02.

### Q11. What does an accepted deferral mean for stage completion? (D11)
1. A milestone is built, or deferred by the person's named acceptance recorded in BUILD-PLAN with its reason; a stage is done when every milestone is one or the other, and a deferral doesn't block the next stage (recommended)
2. Every milestone must be built; a deferral blocks the stage
3. In your own words
Answer: A milestone is built, or deferred by the person's named acceptance recorded in BUILD-PLAN with its reason; a stage is done when every milestone is one or the other, and a deferral doesn't block the next stage. The recommended option, taken by the person on 2026-11-02.

## Log

- 2026-11-02: drafted from the brief and the checked standard, at `main` `2b25cc3`, outside the repo. Status `draft`, review `pending`. Nothing in the repo changed, no tenant was touched, no `pac`, flowagent or canvas-authoring tool was called.
- Read: the brief; the combined standard and its refute pass; reports 01 to 09 in full; AGENTS.md; `docs/plans/phase6-hardening/plan.md` (the precedent); `docs/plans/m39-ship-roles/plan.md` and its `decisions.md`; `skills/powerops-plan/SKILL.md`, `skills/powerops-review-plan/SKILL.md`, `skills/powerops-implement/SKILL.md`; `docs/PLANNING-SKILLS.md`; `docs/HANDOFF.md`; the milestone table, the verification record, the Phase 6 section, the M38 and M39 sections, the build order and the decisions of `docs/BUILD-PLAN.md`. In code: `RoleEvalTests` (the baseline test and `LiveBaseline`), `tests/eval/roles/baseline/solution-01.txt`, the eval case layout, `key.json`'s head, `ImportCall.cs` (the routes and the absence of any `pa app push` match), `SkillsCommand.cs` (the embedded `skills/**` loop), `Analyzer.cs`, `Solution.cs`, `PO005UnusedConnectionReference.cs`, the `PowerOps.Cli.csproj` embedded-resource glob, `docs/rules/README.md` and `tests/fixtures/NOTICE.md`.
- Format: the phase6 plan's header and section order, `powerops-plan`'s section list, milestones named `M40. <title>` with a Conventional Commit type and a release label as the Phase 6 table does, decisions numbered from 40 (M21's decision 39 is the last) and build-order items from 75 (M39 ended at 74). Labels order the milestones and don't fix versions (decisions 29 and 38).

### Why this order

- The standard's section 9 orders by what reaches the eval baselines: gate routes, the PO005 fix and the plan skills first; the flow size and loop rules next; direct connection after that; anything touching Data or showing a new flaw last. The plan keeps it. M40 (gate and PO005) and M41 and M42 (plan skills) touch no baseline; M43's two rules fire on none of the 18 eval flows; the readers come after and are ordered cheapest first. M40 to M43 are the first release checkpoint, as the review suggested.
- The readers' order: canvas (M44) first because it unlocks two existing rules on the layout HANDOFF already commits, and needs one `.msapp` zip read; Power Pages (M45) second because its rules guard a public-facing security boundary and a public MIT fixture exists; model-driven (M46) third because it depends on F2; Code Apps (M47) last because it is one rule on metadata already read.
- The plan skills are split in two (M41 size brake, M42 products and prototype) so the brief's first ask, the bloat checks, ships alone and is reviewed alone.

### Where this plan departs from the standard, and why

- **The direct-connection flow rule is dropped, not placed third.** It fires on `solution-04`, whose planted flaw is already shown, so the not-shown count doesn't move, but the baseline file changes, and under the proposed D1 ruling that is a rerun. It is an ALM risk, not a size or efficiency one, and the brief asked for efficiency.
- **The own-table flow rule is dropped.** The refute pass narrowed it to update triggers, where it fires on nothing; Learn says the product warns on save.
- **Pages ships five rules, not nine, and all are warnings.** Global scope beyond read has no documented line; the role-count, local-login, bearer and `*Secret` rows are review items, and the `*Secret` rule depends on a serialisation nobody has observed. The wildcard rule is a warning, not an Error, after review finding A2.
- **Pages reads one root, not two.** The `.powerpages-site/` layout has no solution identity, and discovery finds solutions by `Other/Solution.xml` (review finding A6).
- **Model-driven ships two rules, not six.** Footer controls rest on a deprecated page; 150 business rules has no public sample near it; the role rule can't tell an unexported role from a missing privilege (A7); the dashboard Error can't fire on an export the designer refused to save (A6). Form and role parsing go with them.
- **Canvas ships one rule, not three.** `ForAll` around `Patch` and nested `ForAll` are one rule with one fix. The library rule is covered by the review.
- **Code Apps ships one rule and no naming.** `CodeAppPackageUris` against files has no known failure. Naming code apps as a kind in `diff` would change a closed enum (A5) for no check that needs it.
- **Data metrics in `analyze --json` are cut.** The standard's own test says a metric ships only if a named report reads it, and no shipped role reads `analyze --json`.
- **M40 doesn't gate deletes or settings changes.** They aren't imports, `powerops-implement` step 7 already requires a yes for every tenant write, and widening the gate's purpose is the allow-list question (D2).
- **`Won't build` goes under `## Goal`, not in a new section.** A new section changes the plan template that every tool and reviewer reads.
- **The store decision checks constraints first, then reuse, then the store** (A10). The standard's first-match order could pick a list before a SQL mandate, or stop at a system of record without checking security or offline needs.
- **The agent transport Error is a review item** (A3). Streamable applies to MCP connections only, and the declaration sits in the connector definition, which may not be in the export; the join isn't a small predicate.
- **No `double` rule anywhere** (A11). The right numeric type depends on precision and range.
- **The `pbip check` is a held item outside the sequence**, not M48 (the review's closing note). Stage two is M48 and M49.
- **The 12-visual threshold is written into M42 pending D4.** The plan ships 12 as a review line and changes one line on another answer.

### Rules the plan rests on

- The eval baselines are text: `LiveBaseline` runs `analyze`, `test`, `diff` and `settings validate` and captures stdout, stderr and the exit code. So `--json`-only output can't reach them, and any new text line on a case does. A `note:` line on stderr would too, but only on a case holding the incomplete input, and none does.
- The pin covers skill text (AGENTS.md line 40). A new rule in `analyze` changes a report, not the text, so the real constraint is the baseline test and the 4-of-6 floor. Both pinned roles read the plan files, so D1 covers both.
- `skills install` copies every embedded file under `skills/`, and the csproj embeds `skills/**/*`, so reference files beside `SKILL.md` reach clients without a code change.
- `deploy gate` matches commands by route lines in `ImportCall.cs` and has no match for `pa`, `npx` or `power-apps`; its MCP branch reads only a tool name ending in `__solution_import`, and the documented hooks match only that tool, which `HandoffDocTests` pins. So a new MCP route needs the recognition, the matcher and the test together (A1).
- Hooks are off during Dev builds (HANDOFF), so blocking `pac pages upload` and `pa app push` in the gate doesn't block the Dev loop.
- `analyze` defaults to `--fail-on error`, so warnings change no exit code.
- `Webapi/<table>/enabled` defaults to `False`, and the wildcard removal has a one-time exemption route an export can't show; both read on Learn's Web API overview and the wildcard migration page (A2).
- `run.sh` already has a `review-plan` pass, so D6 adds cases, not a pass (A13).
- `diff.schema.json` closes `component` and `risks` as enums, and `DiffReport` serialises the same values text output uses (A5, D9).

### What is least certain

- **D1's second half.** Whether either pinned role behaves the same on a ledger-shaped plan is unknown; all eval cases use the old layout. The D6 cases measure `powerops-review-plan` only. Option 2 of Q1 buys the evidence for 24 runs.
- **The hook matcher form per client.** Claude Code matchers take a pattern; whether Codex's take several names in one group wasn't checked, so M40's plan confirms it and falls back to one group per tool.
- **The zip layout of Pages components and app modules.** Stated from how the parser already reads `customizations.xml`, and confirmed only by packing the fixtures in M45 and M46.
- **The `quick` rule for the data round.** One data question in the single round may be too few to fill a ledger; the smoke-test scenario shows whether the Assumptions route works.
- **The 15 KB and 6-screen prototype limits** come from two small samples.
- **Whether `pac copilot pack` is available at the pinned `pac` versions** and writes the sample's layout. F6 is offline, so cheap to check.
- **Whether any consumer validates `diff` output against its schema.** None is known. D9's bump costs each one update; with none, it costs nothing and keeping version 1 breaks nobody, but the plan can't assume that.

### Round 1 of the review (2026-11-02)

- 3 High, 9 Medium, 1 Low, verdict "changes needed". Each finding was checked against the repo at `2b25cc3` and against Learn before answering, and the answers went back to the reviewer. Accepted: A1 to A13, and the closing notes on the held `pbip check`, the compact plan in D6 and the three missing decisions. Rejected: none in full; A6's dashboard rule was answered by a cut where the reviewer suggested keeping minimal parsing, with the reason given there.
- Changes to `plan.md`: M40 gained the `ImportCall` MCP recognition, the hook matchers in both clients' examples, the `HandoffDocTests` change, a migration line and the repeated toy-server check (A1). M41's data round gained its `quick` rule (A9), the store decision checks constraints first (A10), D6 gained the compact case and reuses the existing `review-plan` pass (A13, closing note). M42 gained the Power BI branch in `powerops-implement` (A4), the numeric-type line in place of "no `double`" (A11), the prototype offer on both paths with no fallback (A9), the generative-page hand-off exception (A13), review items for the cut model-driven rules (A6, A7) and a smoke-test scenario (A9). M45 reads one root, its wildcard rule is a warning conditioned on `enabled`, and it has zero Errors (A2, A6). M46 keeps two rules and drops form, role and dashboard parsing (A6, A7). M47 drops the naming in `analyze` and `diff` (A5). The `pbip check` left the sequence; stage two is M48 and M49. M48's transport Error became a review item (A3). The Goal's done-when is delivered outcomes or deferrals accepted by name (A12), and the live-service sentence in Environments was corrected. The pinned-role section and D1 cover both roles (A8). The Compatibility list, the schema statement and the harness naming were corrected (A5, A13). D9, D10 and D11 were added.
- Rule count after the round: stage one 11 (was 13), stage two 5 (was 6), held item up to 7 (was 8). Milestones: 10 numbered (M40 to M49) plus one held item (was 11).

### Round 2 of the review (2026-11-02)

- 10 findings settled; A9, A10 and A12 reopened as Medium; new A14, A15 and A16 (Medium) and A17 and A18 (Low); verdict "changes needed". Each open finding was checked against the repo at `2b25cc3` and against Learn, and the answers went back to the reviewer. Accepted: all eight. Rejected: none.
- Changes to `plan.md`: the data round fills successive rounds of at most three under `standard` and `deep`, and the smoke test gains a `standard` scenario (A9). The store decision collects every constraint first, keeps data with a system of record where it is through a named integration, and sends conflicting constraints to Open questions (A10). The Dev facts are classed as one required prerequisite (F1, for M44), optional evidence (F2, F3, F5) and one optional experiment (F4); the Goal bullet, the Environments paragraph and table, the Assumptions line, M46's stop rule and Steps 2, 7 and 9 follow that, and the builds F2 and F4 need are named as separately approved tenant writes (A12). D9 says a closed-enum addition breaks schema-validating consumers and now recommends bumping `diff`'s `schemaVersion` to 2, with keep-and-document and drop-the-`diff`-part as the alternatives; Q9 above was rewritten to match (A14). D10 and the Constraints bullet separate evidence the layout doesn't carry ("not checked") from a proven missing component (an Error, as PO001 and PO004); M46's Error fires for `Page` targets when `CanvasApps/` is carried and for `GenPageId` targets only once F2 is in; Q10 above was rewritten (A15). The model-driven role line and review item read each persona's roles together against the tables its tasks use, with split roles quiet (A16). The visual count left the held list, which now holds seven (A17). The dashboard cut rests on no fixture, an unconfirmed layout and Learn naming no refusing step, and the plan line says "at most 20" (A18). The "How this plan was made" paragraph no longer names a review file; it gives the two rounds and their finding counts.
- Counts after the round: unchanged. Stage one 11 rules, stage two 5, held item up to 7; one Error per stage; 10 milestones; 11 decisions; F1 to F6; build-order items 75 to 84. The plan is no larger than after round 1.

### Round 3 of the review (2026-11-02)

- A9, A10, A12 and A14 to A18 confirmed fixed, no new finding, verdict "agree". `plan.md` is unchanged from the answer to round 2, which ended "agree", so author and reviewer agree in writing on the same text with no High or Medium open. The named decisions stay with the person.
- 2026-11-02: the person answered Q1 to Q11, taking the recommended option on each.
- 2026-11-02: the person signed off the plan; `powerops plan sign-off` wrote the header (body sha256 527940df…).
