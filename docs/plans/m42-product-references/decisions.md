# Decisions: m42-product-references

## Round 1

### Q1. How does `powerops-plan/SKILL.md` stay within the M41 ceiling of 2,783 words?
1. Move the six data questions to `references/data.md`, and keep the sketch's rules in `route.md`; the core keeps the menu letter, the log line and the safety rules (recommended)
2. Raise the ceiling, as M41's row 5 allows, and record why
3. In your own words
Answer: 1. The brief from the master session keeps the M41 ceiling and says detail goes in the reference files. The skill was 16 words under it, and the pointers, the menu letter on two menus and the log line need about 75. M41's row 5 named the data questions as the first text to shorten, and `data.md` is already read whenever a plan names new data, which is when the round is asked. The `quick` and fast-path rules stay in the core.

### Q2. Where do the sketch's rules live?
1. In `route.md`, beside the route, with each product's detail in its own file (recommended)
2. A seventh reference file, `prototype.md`
3. In your own words
Answer: 1. The Phase 7 plan names six reference files, and `route.md` is read for every app, page, site or report, which is exactly when a sketch can be offered. The canvas "doesn't map" list, the Bootstrap note, the Power BI page size and the code-app first slice sit in their product files.

### Q3. What do the three log values mean, and when is the line written?
1. At approval, on either path: `approved mockups/prototype.html` when a sketch exists, `skipped` when the user chose D and then dropped the sketch, `not chosen` when D was shown and never picked; no line when D was never shown (recommended)
2. A line each time the menu is shown
3. In your own words
Answer: 1. One `## Log` line per plan, as the Phase 7 plan asks, written when the outcome is known. "Not chosen" is then only written after the option was shown, and a plan with no freely laid-out screens gets no line.

## Round 2

### Q4. What severity do the new item 11 lines get?
1. Medium each, the sketch line included (recommended)
2. High for Global scope and for roles that can't read a table
3. In your own words
Answer: 1. Medium is "a gap the build would hit", which each line is, and the Phase 7 plan caps the sketch line at Medium. The security cases also fall under item 8, where the reviewer can rate them higher. No new High keeps M41's pass rule on `plan-14` (no item 11 High) true as worded, so no rerun of the `review-plan` pass is in scope.

### Q5. HANDOFF has no table today. What are its "rows"?
1. A "Per product" section with one table, a row per product, beside the steps (recommended)
2. A sentence per product in step 2
3. In your own words
Answer: 1. The Phase 7 Done when asks for "a row per product, `pcf`, Power BI and Code Apps included", and Power BI's route differs from every step, so a table shows it in one place. Step 2 keeps its list and gains `pcf`.

### Q6. Learn's cloud page gives a dashboard default of six components, and 20 only on the on-premises page. Which number goes in `model-driven.md` and item 11?
1. Six, Learn's cloud default, with the on-premises 20 named as such (recommended)
2. 20, as the Phase 7 plan words it
3. In your own words
Answer: 1. The fact check found 20 only on the Dynamics 365 on-premises version of the page, where a setting raises the default. The plan line and the review should state the number the cloud page gives. This departs from the Phase 7 text, which wrote 20 from the research report.

## Round 3

Asked after the review (R4, R10), answered by the building session.

### Q7. Does the Power Pages Global scope line stay Medium?
1. High; the other product lines and the sketch line stay Medium (recommended)
2. Medium, with table-permission scope added to item 8
3. In your own words
Answer: 1. The reviewer is right that Q4's second reason doesn't cover it: the line fires only on a plan that names a site, so it can't reach `plan-13` or `plan-14`, and Global scope on anything but read-only public data exposes every row to the web role, which is what the review skill calls High. Q4 stands for the other lines, with its reason corrected: the lines that can fire on the two cases (canvas, the sketch) are Medium.

### Q8. How is the other half of M41's pass rule (item 11 names the three keyed problems on `plan-13`) covered?
1. One unscored spot check on Claude Code: `run.sh claude review-plan` on `plan-13` and `plan-14` on the shipped text, read by the building session against F40 to F42 and for item 11 Highs, recorded here (recommended)
2. Record it as unmeasured in BUILD-PLAN only
3. In your own words
Answer: 1. Item 11 grows by about ten lines, which could shift what a reviewer files under it, and M41's twelve runs cost $3.00 at list price, so one run per case is cheap. It is not M41's blind scoring and not on Codex, so the record says so. A miss follows M41's rule: one revision of item 11's new lines only, both runs again, and a second miss stops the build for the master session (corrected after R12: the first wording skipped the revision).

## Log

- 2026-11-03: plan started at the request of the session that started this one: build M42 of Phase 7, build-order item 77, from the M42 section of `docs/plans/phase7-power-platform/plan.md`, its Screens section and its BUILD-PLAN row, with decisions 43 and 47
- read on 2026-11-03: AGENTS.md; the Phase 7 plan's M42, Screens and Won't build sections, D4 and D8; the M41 plan folder (layout and the hand-over note); `powerops-plan`, `references/data.md`, `powerops-implement`, `powerops-review-plan`; `docs/HANDOFF.md`; `skills-smoke.sh`; `SkillsCommandTests`, `HandoffDocTests`, `PluginManifestTests`; `plan-14`'s Users and Screens; the research reports on canvas, model-driven, Power Pages, Power BI, Code Apps and prototyping, and the combined standard
- facts checked on Learn on 2026-11-03 by two researcher subagents (Sonnet), and in the MIT repos skills-for-fabric (`4c3986d`) and power-platform-skills (`96d16d0`) by reading files; the results are under Assumptions in `plan.md`
- depth: standard, as the Phase 7 plan sets for the skills milestones
- questions answered by the building session, under the user's delegation of 2026-10-22 (autonomous milestone mode), with the Phase 7 plan's scope and the master session's brief
- areas not asked: users, automation, environments and ALM, and constraints; the change is to this repo, and the plan answers each or marks it not applicable
- approved 2026-11-03 by the building session under the same delegation
- 2026-11-03: the modern controls reference lists 28 controls, not the 27 of the Phase 7 `## Screens` table; `canvas.md` writes 28 with the date
- fresh review (general-purpose subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-11-03: 0 High, 4 Medium, 7 Low (R1 to R11). All fixed in `plan.md`, the skills and the smoke script, with the reasons in `review.md`; Round 3 records the two choices the fixes needed. The log rule moved to section 7 step 4 (R3), and the plan skill is 2,770 words after it
- the same reviewer's second pass 2026-11-03: R1 to R11 hold (corrections appended to R1 and R10), 1 new Medium and 4 new Low (R12 to R16), all fixed in `plan.md`, Q8 and the plan skill (a sketch asked for without D is logged too, R15)
- spot check (Q8) 2026-11-03 on Claude Code 2.1.316, `opus`, high: `plan-13` at `1bf0931` and `plan-14` at `d146ad3` (which differs only in the smoke script), both exit 0. Read by the building session, unscored and not blind: on `plan-13` item 11 named F40 (R1, High), F41 (R3, Medium) and F42 (R2, High); on `plan-14` no item 11 finding is High (the Highs are under items 2 and 5, the gaps M41's hand-over names). The new canvas line fired as Medium on both. Passes; not measured on Codex. The review skill is unchanged since `1bf0931`
- the same reviewer's third pass 2026-11-03: R12 to R16 hold, the spot-check record matches the two reports, one new Low (R17), accepted with no change because `app-builder`'s SKILL.md states the rebuild claim at line 885; no finding open
- signed off 2026-11-03 by the building session under the user's delegation of 2026-10-22, with no finding open (`powerops plan sign-off`, body hash `0baa5a0a…`); status set to implementing for the build, which was written alongside the review and fixed with it
