plan-hash: 2b360bff3a07c4606c2fd075cbc90c5be7a94e0db33dd2ded6c4656866d80af7
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read. The request did not say the person confirmed a signed-in `pac`, so `powerops plan facts` was not run, and no `pac`, `az` or tenant call was made. `powerops` was run as `dotnet run --project src/PowerOps.Cli --`. This is a new review from scratch, and it replaces the review of plan-hash `c6656af`. HEAD is `70e1ada`, which differs from `b065f23` only in this plan folder, so the Data rows were checked against the files at HEAD. The line numbers and quoted current text match: `SKILL.md:5`, `:36-46`, `:87`, `:117-145`, `:130`, `:139`, `:152`, `:168-174`, `:186`, and the cross-references at `:170`, `:173`, `:174` and `:208`; `references/data.md:33`; `powerops-review-plan/SKILL.md:37`; `docs/PLANNING-SKILLS.md:35`, `:153`, `:157-174`, `:178`, `:186`, `:227` and `:249`; `README.md:21` and `:109`; `SkillsCommandTests.cs:462` and `:621`; smoke `:249`. The only mismatch is the fake `pac` (R9). Tools the steps name were found: `dotnet`, `claude`, `jq`, `git` and `bash`. The leak scan is not in this worktree (R11). Checklist items 1, 2, 4, 5, 6, 7 and 8 don't apply: the plan changes skill text, docs and tests, and names no solution, connector, environment or role.

The earlier findings, checked against the plan as it stands:

- R1 (renumbered steps): closed. Rows 4 and 6 add sentences to section 7 step 2 and section 9 step 3. Row 5 has the fast path's A and B keep the defaults, and row 12 (e) pins it.
- R2 (the Held question under `quick`): closed. Row 1 says "Under `quick` ask nothing", and row 12 (a) pins it.
- R3 (where the batches are logged): closed. Rows 4 and 6 log both batches as `## Log` lines with marks and no `### Q`. Row 6 excludes High findings, sets `Resolution: accepted` and leaves the body unchanged. Row 12 (d) pins this.
- R4 (smoke turn budgets): closed. Row 13 adds one turn each to scenarios 1 and 6. Step 5 fixes any other overrun the new questions cause in the script.
- R5 (the recommended tier with no standard alternative): closed in the text of row 7. The rule is not pinned (R6).
- R6 (safe default): closed. Row 3 defines it and lists what is never a default, and row 12 (c) pins it.
- R7 (Developer Plan note): closed. Row 7 and Assumptions say only unmanaged developer environments stop and managed ones remain, sourced to ECO-0006. The pinned phrase differs from row 7's wording (R9).
- R8 (two homes, no budget): partly closed. `licensing.md` section 1 is now the only home of the `Held` values and tier rules, and the ceiling is named (3,123). The quoted wording does not fit that ceiling (R1).
- R9 (no fake `az`): closed. Row 13 adds the fake `az`, and Environments and route says so.
- R10 (docs on `az` and planning reads): closed. Row 10 names principle 7 and `README.md:21` and `:109`, and row 1 names `az` in the front matter. Other doc lines are still missing (R10 below).
- R11 (the Graph row's "where" cell): closed by row 11.
- R12 (Power Pages part numbers): closed. Assumptions says neither the Power Pages packs nor the credit pack is listed, so both read `unknown`.
- R13 (four or five headings): closed. Row 12 (f) names the five headings of row 7.
- R14 (commits): closed. Steps 3 and 4 each commit locally.
- R15 (URLs): accepted by the building session. The same content is still there, so this role reports it again (R12 below).

## Findings

### High

No findings.

### Medium

#### R1. The quoted wording is about twice the word budget
- Area: 9. Testability (step 3's done-condition)
- Finding: `SKILL.md` has 2,869 words, and the new ceiling of 3,123 leaves 254. The new text quoted in rows 1 to 6 adds about 520 words and removes about 18, so the net growth is about 500. Step 3 is done only at 3,123 words or fewer, and every phrase row 12 pins must stay. Data says "the executor may shorten it", but halving the wording means redrafting skill text during the build, after both reviews have read the plan. The quoted text would then not be the text that ships.
- Suggested fix: Before sign-off, cut the quoted text in rows 1, 3, 4 and 6 so that it fits 250 words, and count it. For example: in row 1, drop the repeated guards that row 3 or `licensing.md` already holds; in row 3, keep "**Budget.**" to its two pinned sentences. Or set the ceiling to the measured size, and give the reason in row 12's note.
- Resolution: open

#### R2. Goal 2's rule that `quick` keeps the defaults is in no Data row
- Area: 9. Testability
- Finding: Goal 2 says "under `quick` and in the fast path, the approval keeps them", and Round 1's Q2 says the same. Row 5 covers only the fast path. Row 4 puts "Keep these N defaults?" into section 7 step 2, which a `quick` plan reaches through B, and it names no exception. As written, `quick` would ask the defaults question, and row 12 pins nothing about `quick`. Scenario 5's budget (`skills-smoke.sh:242`) has room for the extra turn, so the smoke would not catch the gap.
- Suggested fix: Add to row 4: "Under `quick`, B keeps them: log `defaults kept: <N>`." Pin it in row 12 (d). Or, if `quick` should ask after all, drop `quick` from Goal 2 and Q2.
- Resolution: open

#### R3. The skill and the review item use different triggers for the `### Licences` table, and the smoke relies on it
- Area: 3. Premium connector and licence impact; 9. Testability
- Finding: Row 2 (and Goal 4) writes the table "when the plan names anything to license". Row 9 makes a missing table a Medium finding when the plan names "a connector, table, app, flow, site, report or agent". Row 13 fails scenarios 5 and 6 without the table. Those scenarios plan a canvas app over new SharePoint lists, which the recommended tier keeps standard. An agent can fairly read a standard-only canvas app on lists as "nothing to license" and write no table. The smoke would then fail, and the reviewer would raise a Medium the planner had no rule for.
- Suggested fix: Use row 9's list in row 2 and in Goal 4 ("when the plan names a connector, table, app, flow, site, report or agent"), and pin that sentence in row 12 (b).
- Resolution: open

#### R4. Section 3 maps free, trial and developer part numbers with no rule for what they cover
- Area: 3. Premium connector and licence impact
- Finding: Assumptions lists `POWERAPPS_DEV`, `POWERAPPS_VIRAL`, `FLOW_FREE`, `CCIBOTS_PRIVPREV_VIRAL` and `POWER_BI_STANDARD` among the part numbers for `licensing.md` section 3, whose table is only `| Part number | Product |`. Section 1 writes `yes (read)` when a part number "maps to a product that covers the row". Nothing says which section 2 need each product covers, or for whom, and the `Who needs it` column separates the builder from the users. An executor could map the Developer Plan or a trial to the premium product a row needs and write `yes (read)` for a licence the account doesn't hold for that use. That is the trial-3 failure this milestone exists to fix, and no pin or smoke check would catch it.
- Suggested fix: In row 7, give section 3 a column that names the section 2 needs each part number covers. Write a free, trial or developer plan as itself, never as the paid product, and say which use it covers: for example, the Developer Plan covers the builder's own use in a developer environment only. Have `powerops-researcher` confirm those scopes, put them in Assumptions, and pin the rule in row 12 (f).
- Resolution: open

### Low

#### R5. A successful read never settles a standard row
- Area: 3. Premium connector and licence impact
- Finding: Microsoft 365 bundles are not in section 3, so they read `unknown` (Assumptions). The standard rows of section 2 (standard connectors, and SharePoint lists with Microsoft 365) are what "Not sure: standard first" picks most often. Even after a successful read, they stay `unknown` with a check step. The owner asked for a licence check at the start, and for the most common plan the read settles nothing.
- Suggested fix: Say this in `licensing.md` section 1 and in the docs paragraph of row 10, so that the check step is expected. Or, after a researcher check, list the bundle part numbers that include Power Apps and Power Automate for Microsoft 365, mapped to the standard rows only.
- Resolution: open

#### R6. Nothing pins the `Held` values and tier rules in `licensing.md`, though the smoke greps their spelling
- Area: 9. Testability
- Finding: Row 12 (f) pins the headings, the date, "`unknown`" and the developer-environment note, but not the four `Held` values (`yes (read)`, `no (read)`, `person says yes`, `person says no`) or the three tier rules. Row 13's smoke fails on `(read)` and `person says yes`, so a rewording in `licensing.md` would silently make those checks blind. The rule that closed the earlier R5 is unpinned too.
- Suggested fix: Pin the four `Held` values and one key phrase of each tier rule in row 12 (f).
- Resolution: open

#### R7. "The cost tier under `standard` and `deep`" lets `quick` treat the tier as a default
- Area: 10. Unanswered areas
- Finding: In row 3's never-defaults list, the cost tier is never a default only under `standard` and `deep`. The same row has the `quick` line (`:139`) ask the cost tier, and smoke scenario 5 (`:249`) needs a licensing question in the `quick` round. The intended exception is the fast path (row 5), not `quick`.
- Suggested fix: Write "the cost tier outside the fast path" in row 3.
- Resolution: open

#### R8. Row 1 has the neutral question asked after a successful read too
- Area: 9. Testability
- Finding: Row 1 says "Then, and when the condition doesn't hold, ask neutrally which of the table's products the account holds". After "Write product names only" and the failed-read sentence, "Then" can mean after any read. Goal 5 asks it only when the read fails or the condition doesn't hold.
- Suggested fix: Write "After a failed read, and when the condition doesn't hold, ask neutrally...".
- Resolution: open

#### R9. Small mismatches in the Data rows
- Area: 9. Testability
- Finding: Row 12 (f) pins "managed developer environments remain", but row 7's wording is "while managed ones remain", so writing row 7 as quoted fails its own pin. Row 14 names `SkillsCommandTests` as the plugin-copy check, but that check is `PluginManifestTests.Plugin_skills_are_the_shipped_skills`, which step 3's filter doesn't run. Its "apart from the plugin-copy checks" is therefore empty, and step 4 doesn't name the test. Row 13 puts the fake `pac` at `skills-smoke.sh:35-37`; it is at `:36-38`.
- Suggested fix: Align row 7's wording with the pin, name `PluginManifestTests` in row 14 and in step 4's done-condition, and correct the line range.
- Resolution: open

#### R10. Row 10 leaves out doc lines that describe what changes
- Area: 10. Unanswered areas
- Finding: Goal 8 says the docs "say the same". `docs/PLANNING-SKILLS.md` still has, unchanged by row 10: the Review checklist's "premium connector and licence impact" bullet (`:212`), which doesn't mention the `### Licences` table check; the "Product references" paragraph (`:155`), which lists every reference but `licensing.md`; and the Review "Output" line (`:222`), which says the user resolves or accepts each finding, with no mention of the assumptions batch at sign-off.
- Suggested fix: Add these three places to row 10.
- Resolution: open

#### R11. The leak scan and the cited reports are not in this worktree
- Area: 12. Tools
- Finding: Goal 10 and step 6 need "the leak scan" but give no path. The script is `reports/leakscan.sh` in the main checkout, which is git-ignored and not in this worktree. Goal and Constraints cite `reports/trial-3/fix-plan.md` and `reports/trial-3/chain/chain-rules.md`, which are not here either. Assumptions doesn't say so.
- Suggested fix: Name the script and where it runs in step 6 or Assumptions, and note that the `reports/` sources are in the main checkout only.
- Resolution: open

#### R12. The plan and decisions.md hold URLs
- Area: Tenant data
- Finding: Row 1 in `plan.md` spells out the Graph host and path, without the scheme. The research line of the `## Log` in `decisions.md` holds about ten host-and-path references to public Learn and GitHub pages. They are public documentation, not tenant values, `plan status` reports no violation, and the earlier R15 on the same content was accepted. This role reports every URL.
- Suggested fix: Keep the earlier acceptance, or replace them with page titles and keep the paths in `licensing.md`'s Sources and the compatibility rows.
- Resolution: open
