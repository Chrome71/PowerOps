# Decisions: m41-size-brake

## Round 1

### Q1. Where does the flow card live?
1. In `references/data.md`, read when a plan names new data or a flow (recommended)
2. In the core skill, under section 5
3. In your own words
Answer: 1. BUILD-PLAN row 76 lists the flow card with the data references, and the card is about the rows a flow reads and writes. The core keeps a one-line pointer, which is all the 300-word budget allows beside the ledger and the data round. A plan with flows and no new data still reads the file, so no flow goes without a card.

### Q2. How does the review know the SharePoint gates?
1. Item 11 carries the eight gates in one sentence (recommended)
2. The review reads the plan skill's `references/data.md`
3. In your own words
Answer: 1. `powerops-review-plan` reads only the plan, the repo and the facts, and `run.sh` copies only its `SKILL.md` into a run, so it can't rely on another skill's file. The numbers are then in two places; the wording test checks both.

### Q3. What is the word ceiling, and how are words counted?
1. 2,783: today's 2,483 plus 300, counted as whitespace-separated words, as `wc -w` counts them (recommended)
2. 2,800, a round number
3. In your own words
Answer: 1. The Phase 7 plan says "within about 300 words of today", and the count matches the 2,483 the plan quotes, so the ceiling is the number the plan names.

## Round 2

### Q4. What do the two plan cases hold, and how are they scored?
1. `plan-13`: per-team copies of one list (High), four title-only lists (Medium) and a list no Goal line needs (High), three key entries; `plan-14`: the same goal at its right size, no entry. Two blind scorers, one per family, as M39 did (recommended)
2. One blind scorer
3. In your own words
Answer: 1. `CountProblems` needs a flawed case to hold exactly three entries and a clean one none, and each entry tests one line of item 11. Same goal in both cases, so the only difference a reviewer sees is size. The compact case states every gate as holding, so it also tests that the store-gate line doesn't fire on a list that passes.

### Q5. How does the data round fit the round order?
1. The licensing and environments rules win; a data question waits a round when one of them is due (recommended)
2. The data round comes first, and the two rules move
3. In your own words
Answer: 1. The two rules exist so that late areas aren't skipped, and the data round can take two rounds by itself. Keeping the rules keeps `quick`'s one round as it is: licensing, environments and the first data question.

### Q6. How does item 11 report many artefacts with the same problem?
1. One finding per problem, listing every artefact it covers (recommended)
2. One finding per artefact
3. In your own words
Answer: 1. An oversized plan can have twenty lists with the same fault, and twenty findings would bury the rest of the review. A plan written before the ledger gets one finding for its missing Goal lines, not one per table.

## Round 3

Asked after the review (R2, R4), answered by the building session.

### Q7. What happens when the evaluation misses?
1. One revision of item 11's wording, then all four runs again on that commit; a second miss stops the build for the master session (recommended)
2. Rerun only the failing client
3. In your own words
Answer: 1. Item 11's text is the same for both clients, so a revision voids the passing client's result too; rerunning all four keeps `report.md` about the text that ships. The cases, the key and the pass rule stay fixed, so a revision can't pass by moving the target. Whether a second miss ships or defers is a scope call, which belongs to the master session under D11.

### Q8. Where does the pointer to `references/data.md` go?
1. Section 5, beside the ledger bullets (recommended)
2. Section 8 step 1, and section 6
3. In your own words
Answer: 1. Section 5 is the file format that both the question rounds and the fast path write, so one sentence reaches both, and it cost no extra words. The wording test pins it before section 6.

## Round 4

Asked after the first evaluation round, answered by the building session.

### Q9. Item 11 asks every artefact to be marked Reuse, Extend or Create, but the plan skill marks only tables and lists. Which gives?
1. Item 11's line names tables and lists, as the ledger does; rerun all four cases on that text (recommended)
2. The plan skill adds a Reuse, Extend or Create column to `Automation` and `Screens`
3. In your own words
Answer: 1. Both clients raised it as a Medium on both cases' screens and flows, so as worded it fires on every plan the skill writes. The ledger is where the Phase 7 plan puts the column, and the flow card already asks for the flow it reuses; a column on screens would cost words the core skill doesn't have. Item 11 keeps its seven severities, so `plan.md` is unchanged, and all four runs repeat on the new text so `report.md` describes what ships.

### Q10. Round 2 missed on `plan-14`: each client raised a supported High that it filed under item 11 (a column that repeats another; no source for the team leads' addresses). What is the one revision?
1. Scope item 11 to what the plan has and doesn't need, say that something missing or a column that repeats another belongs under the other items, and name the artefacts in the "same concept" line; rerun all four (recommended)
2. Change `plan-14` to remove the two gaps
3. In your own words
Answer: 1. The plan allows one revision of item 11's wording after a miss, and forbids changing the cases, the key or the pass rule, so option 2 is out. Both Highs are real gaps in `plan-14`, but neither is the plan being too big, which is what item 11 measures; filing them there is the item's wording at fault. They stay findings under items 2 and 7 to 9, which the pass rule records and doesn't score.

## Log

- 2026-11-03: plan started at the request of the session that started this one: build M41 of Phase 7, build-order item 76, from the M41 section of `docs/plans/phase7-power-platform/plan.md` and its BUILD-PLAN row, with decisions 40 and 45
- read on 2026-11-03: AGENTS.md; the Phase 7 plan's M41 section, D1, D6, Automation and Won't build; BUILD-PLAN row 76 and decisions 40 and 45; the M40 plan folder (layout); the M38 and M39 reports (how the runs were recorded); `powerops-plan` and `powerops-review-plan`; `RoleEvalTests`, `run.sh`, `key.json`, `NOTICE.md`; `SkillsCommand`, `SkillsCommandTests`, `PluginManifestTests`, the csproj's embedded-resource line; `PlanDocument`'s open-question count; `docs/PLANNING-SKILLS.md`; the research standard's sections 3.1 to 3.5 and its refute pass
- facts checked on Learn on 2026-11-03 by a researcher subagent (Sonnet) and by search: the gate numbers in Assumptions; not found: the connector's 600 calls per 60 seconds and the "non-lookup" index qualifier, so both are left out
- depth: standard, as the Phase 7 plan sets for the skills milestones
- questions answered by the building session, under the user's delegation of 2026-10-22 (autonomous milestone mode), with the Phase 7 plan's scope as the brief
- areas not asked: users, automation, environments and ALM, and constraints; the change is to this repo, and the plan answers each or marks it not applicable
- approved 2026-11-03 by the building session under the same delegation
- fresh review (general-purpose subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-11-03: 0 High, 4 Medium, 8 Low (R1 to R12). All twelve fixed in `plan.md`, the skills, the cases and the tests, with the reasons in `review.md`; Round 3 records the two choices the fixes needed. R1 corrected the earlier fact check, which had fetched another connector page
- the same reviewer's second pass 2026-11-03: R1 to R12 hold (R7's claim about `plan-13` corrected), 2 new Medium and 2 new Low (R13 to R16), all fixed in the cases, item 11's title-only rule, `data.md` and the key
- the same reviewer's third pass 2026-11-03: R13 to R16 hold, no honest item 11 High on `plan-14`, one new Low (R17) fixed in the case and key wording; no finding open
- signed off 2026-11-03 by the building session under the user's delegation of 2026-10-22, with no finding open (`powerops plan sign-off`, body hash `aaefe527…`); status set to implementing for the build, which was written alongside the review and fixed with it
