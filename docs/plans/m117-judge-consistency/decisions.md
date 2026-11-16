# Decisions: m117-judge-consistency

## Authority

- The owner chose on 2026-11-15 to make judge consistency its own milestone before trial 4, after #166's rerun judged row 5 "at baseline" against a key of "not at baseline" on pixel-identical input. The milestone runs under the chain rules in `reports/trial-3/chain/chain-rules.md` (sessions sign off their own plans after clean reviews: "Start the build", 2026-11-11; "Sessions sign off", 2026-11-09), and it stops before merge for the owner's yes in chat, as M97 did, because it touches the calibration evidence.
- M97's decisions Q14 to Q17 stand. In particular the key's verdicts for rows 2, 3 and 5 are the owner's to change (Q17); this milestone only brings the evidence.

## Round 1 (planning)

### Q1. Where do the rules go?
Decided by the planner: a rule that says what a gate or a dimension's words mean goes in `baseline.md`, which every role reads (the author, the judge, the design lead, `powerops-implement`); how the judge reads `measure.py`'s fields goes in the judge's `SKILL.md`. A measurement goes in `measure.py`. So each rule is in one place, and the author fixes against the same words the judge applies.

### Q2. Which reading does each rule pick?
Each G row of the plan's Data row 2 picks the reading that a measurement or a named element can settle, and, where two readings were equally checkable, the one the samples' own captions and the look rules support:
- G9 and G10 follow what the product draws and what P9 allows (imagery with the accent), so the author is not failed for the product's own rendering or for a drawing.
- G14 uses its own meaning set (from `look.md`'s status intents, S3 and B6), reads a use's meaning from the word, sign or control beside it, and keeps the reading both before-runs agreed on for row 2: a status in a status colour in one place and in ink in another is one meaning in two colours. The set is the rule's own, from `look.md`'s status intents, S3 and B6: a Critical status and an adverse difference are one meaning (bad), a Positive status and a good difference one (good), because the samples put each pair on one hue; two adverse differences are one meaning unless their words differ in severity.
- G15 compares text by size and weight only, from `measure.py`, leaves the chrome (`header`, `nav`, `footer`) out of the rivals, lets a row or field first look pass on the state it alone carries, and breaks an exact tie by reading order, the reading S166's judge used ("first in reading order"). Colour and fills don't decide gate 1: a filled button is gate 5's and S9's. This narrows "strongest" to weight; it is named for the owner in Open questions.
- G16 lets `measure.py`'s figures decide gate 7, because the figure is the same in every run and the eye was not.
- G17 drops `partly`, which nothing defined, and ties a score under 3 to a `not met` statement with its element, because "The scale" says every score cites a frame and an element, and the runs disagreed most on scores with no statement behind them. A sketch whose only misses break no number now scores 3; that is named for the owner in Open questions.

## Round 1 plan review (Fable, REJECTED, 11 findings)

All 11 answered in the plan's second draft: markers the five sketches lack (no `data-lead`, no `data-for`) are no longer relied on (G12 floor fallback, `inLead: null`, G5 from `Screens`, nested tiles); gate 1's procedure is complete with the title flagged, not removed (G15); G14 has a colour model, a threshold set from the tokens, an evidence rule for meanings and the full brand set; an ellipsis is gate 3 in canvas by `canvas.md` C5 (G4); `partly` is gone (G17); imagery is told from icons and charts by size and text (G10); A1 is verified and A2 restated; a dry run comes before the judge runs (step 5); any text change after the runs reruns all 15; the chain's review and sign-off steps are in; G16 covers kind `none`.

## Round 2 plan review (Fable, REJECTED, 4 findings)

Answered in the third draft: chrome text is no rival and a row or field first look passes on its state (G15); step 5 fixes or lists every frame where a rule disagrees with both before-runs, and checks every svg's class against the picture (G10 classes a report's text-free svgs as charts); Critical and bad, Positive and good are one meaning each, and the set is called the rule's own (G14); Open questions name each narrowing and G17's detection residual, and "0 when the frame doesn't render" is gone.

## Log

- 2026-11-15: plan drafted by the building session (Opus 5.5, high).
- 2026-11-15: A1 verified by powerops-researcher (opus) on Learn: built-in visuals dim the unselected data on cross-highlight; bar and column only; no normal-mode contrast figure. Recorded under Assumptions.
- 2026-11-15: Fable plan review round 3: APPROVED, with assignments (all executor steps opus; haiku only for the table extraction; no codex-sol) and WATCH notes; its two text slips fixed.
- 2026-11-15: `powerops-review-plan` (fresh opus context) wrote `review.md`: 1 High, 6 Medium, 5 Low. All 12 answered in the plan: R1 G9 and G10 name SKILL for the fields, baseline keeps the words; R2 which first looks pass on their state, a selected tab excluded; R3 a canvas ellipsis is gate 3 on C5's line even with the map silent, a tile's is a map-check line; R4 what shows a visual is cross-highlighted; R5 step 5 copies S166's per-frame gate lines into `calibration.md`; R6 step 3 owns the `measure.py` comment; R7 no merge while a row contradicts the key as kept; R8 Learn paths without the host; R9 a deterministic hue grouping; R10 the top-level faults count listed; R11 a gate still differing after step 9 stops the milestone; R12 where the row 2 answer lands.
- 2026-11-15: `powerops-review-plan` rerun: 0 High, 1 Medium, 3 Low; R1–R12 of the first run closed or carried. Answered: the named-row rule (a text inside a row names the row; a named state not drawn fails gate 1; a position-only line passes on the only focus ring, else the size test), Done line 5 stops and reports instead of a second step 9, the hue scan starts after the largest gap, and step 5 is handed the #166 files by path and matches them by `sketch-hash:`.
- 2026-11-15: `powerops-review-plan` third run: 0 High, 0 Medium, 1 Low (R1: a second focus ring or selected row beside the named first look). Answered without a plan change, which would only restate G15's "the frame's only": step 4 writes gate 1 so that a frame with two focus rings or two selected rows fails it, because two items compete for the first look (gate 1's words).
- 2026-11-15: signed off by the building session (`powerops plan sign-off`) after Fable's APPROVED and `powerops-review-plan` with no High or Medium open, on the chain's standing instruction ("Start the build", 2026-11-11; "Sessions sign off", 2026-11-09) and the owner's choice of 2026-11-15 to run this milestone. Merge still waits for the owner's yes.
- 2026-11-16: step 5's open items decided by the building session (recorded in `calibration.md`): bullet bars and rect-and-line svgs are charts; a one-colour svg is an icon at any size; gate 1 rivals count only in the frame's first screen (top 768 px of a desktop or browser frame, all of a phone or report frame), because S1's first look is the first screen; the title is the first `h1`, else `h2`, else heading; G9 exempts the whole dimmed part, grey context included; the report mark in a `div` stays a known case (no colour, no effect); row 2 Repairs (phone) keeps the rule and goes to the owner's questions; the selected-row wording as step 5 proposed; Identity "1 when two or three are missing" (step 4's pick).

## Step 8 and 9 results, and the stop (2026-11-16)

Round A (15 fresh Opus judges at `f1dbebc`): every row kept one verdict (`not at baseline`) and rows 1 and 4 their keyed gates; one gate differed, row 4's gate 6 (pass, fail, fail), from an avatar beside a visually hidden label (a `measure.py` bug) and a grey bar read at 2.66:1 by eye. Step 9 measured both (`marks`, drawn names only) at `82b1878`.

Round B (15 new fresh judges at `82b1878`, the final text): every row kept one verdict, rows 1 and 4 their keyed gates, and row 4's gate 6 now fails in all three runs. Two other gates differed:
- Row 2, gate 3 (fail, pass, pass): the header logo's SVG route, which the map marks not confirmed with a PNG fallback that draws the same mark. One judge failed it by the letter of gate 3; five of the six round A and B judges made it a map-check line.
- Row 3, gate 7 (fail, pass, pass): one judge failed the report's phone layout for having no page navigator (S14); gate 7's size and empty share are measured, its chrome check is not.

By plan step 9 this second miss stops the work: Done line 4 is not met, nothing is pushed or merged, and the runs go to the owner.

### Questions for the owner

1. **The two gates that still differ.** Recommended: one more text round with two rules, then 15 runs on the final text:
   - Gate 3: a part the map marks not confirmed passes gate 3 when the map names a fallback that draws the same picture (a PNG for an SVG logo); it is a map-check line. Five of six judges already read it so.
   - Gate 7's chrome: name per product what the chrome is (canvas: C2's header and one navigation; site: P3's header and footer; report: B1's title row; a report's phone layout: its title only, since Power BI's mobile layout shows no page navigator, a claim to verify on Learn first).
   Alternatives: accept both as recorded residuals and ship, or stop here.
2. **The key for rows 2, 3 and 5.** In all 30 runs (and both before-runs) they were `not at baseline`, as `key.md` says. Recommended: keep the verdicts and key the gates that held in every run of both rounds: row 2 gates 1 and 4; row 5 gate 4; row 3 none (it fails on scores only). Row 4 gains gates 4 and 6 beside 8. The owner decides (Q17 of M97).
3. **Row 2's plan line** ("the first field" against the sample's "the field in focus"): every run fails gate 1 on it. Recommended: leave the row plan as it is; the sample, not the line, would need the change, and that is a design call.
4. **The narrowings named in Open questions** (G4, G9, G10, G15, G16, G17), each observed in the runs: row 1's ellipsis moved from gate 6 to gate 3 in all runs; row 3's dimmed cross-highlight passed gate 6 in all runs; row 5's tick drawings left gate 4 in all runs; ties broken by reading order in all row 5 runs; gate 7 followed the figures in all runs; scores now cite a `not met` statement, and scores still differ between runs by one point on single dimensions (a detection residual), with no verdict changed by it.
- 2026-11-16: the owner answered question 1, asked "Run one more text round to close gates 3 and 7, then 15 new judge runs?": "Yes, one more round". Questions 2 to 4 stay open.
