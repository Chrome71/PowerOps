# Applied: m97-visual-baseline

One line per Data row of `plan.md`, with its mark and the commit that holds it. "This commit" is `docs: record M97`. A decision is named where it departs from the amendment's words; a choice the amendment left open is named in the note.

1. The loop in `powerops-prototype` section 6: applied (e7338f2). Who starts the judge when the prototype runs forked is decisions Q9. Section 5's line on fake people's names was made to agree with S8 after the verification run (8110568).
2. The judge, `powerops-review-sketch` and `measure.py`: applied with a difference (decisions Q2, Q6, Q11, Q14, Q15, Q16) (79107c6; the avatar skip in af1f540; the gate 1 sentence of step 4 in 07e6606). The judge file's format is decisions Q7.
3. The scale, in `baseline.md`: applied (3862358).
4. The gates, in `baseline.md`: applied with a difference (decisions Q14, Q15, Q16, Q17) (3862358; gate 6's avatar exemption in af1f540; gate 1 covers the title in 07e6606; gate 1's docked phone action in bb0670b).
5. `baseline.md`: applied (3862358).
6. `look.md`: applied (3862358).
7. `canvas.md`: applied (3862358).
8. `power-bi.md`: applied (3862358).
9. `power-pages.md`: applied (3862358).
10. The samples and the kit: applied with a difference (decisions Q8, Q16, Q17) (ade787e; the prototype skill's reading and kit lines in e7338f2). Q8 shipped them byte-identical; the canvas and Power Pages samples now carry the fixes of Q16 (07e6606) and Q17 (bb0670b), and the Power BI sample is unchanged.
11. `design.md`'s Theme block: applied (3862358).
12. `powerops-design-lead`: applied (e7338f2; its plugin copy in ada7924).
13. The file rules and checks: applied with a difference (decisions Q4, Q5, Q11, Q12, Q13, Q14) (1856c53; the prose in e7338f2). Item (b) has no code change: the tenant-data scan is unchanged and the prose says to write no `xmlns`. The tag stack is decisions Q3 and the judge file's format Q7.
14. Options are three lead frames: applied (e7338f2; the option check in 1856c53).
15. The pick, the pass and the end states: applied (e7338f2; the plan skill's word ceiling in 3f67f21).
16. Calibration: applied with a difference (decision Q1): fixtures applied (`79107c6`, which holds the trial sketch with its one real-looking company name already replaced by a made-up one); run: three rounds (`36ae40d`, `2e65252`, and round 3 in `calibration.md`); the samples fixed by decisions Q16 and Q17 (`07e6606`, `bb0670b`), and rows 2, 3 and 5 recorded as not at baseline by Q17's condition.
17. The owner's seven decisions, quoted in `decisions.md`: applied (36a6bb3).
18. Not in M97: the fresh end-to-end trial is trial 4, and the layer-2 check that two numbers with one label agree is left to the judge's eyes (decisions Q2).
19. Beyond the amendment: applied (`powerops-implement` section 4, the docs, `powerops-plan` section 6, `AGENTS.md` Layout and the smoke script in e7338f2; `route.md` in 3862358; the pins in 3f67f21; the plugin copies and manifest descriptions in ada7924; the BUILD-PLAN decision in this commit). After the smoke's first local Claude run (exit 1, 121 minutes, at aacdcbc: the pass after a fail was asked only at approval), `route.md` asks it again in the next round, and the workflow's timeout is 150 minutes (ca8e4ed); the rerun's result is in the Log of `decisions.md`.
