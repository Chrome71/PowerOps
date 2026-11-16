# Key

The verdict each row must get. A `not at baseline` row must also fail the gates named here; other gates may fail too. Paths are from the repo root.

| Row | Sketch | Map | Verdict | Gates that must fail |
|---|---|---|---|---|
| 1 | `tests/sketch-eval/row-1/mockups/prototype.html` | none | not at baseline | 1 (lead), 7 (not the product) |
| 2 | `skills/powerops-plan/references/samples/canvas-repairs-desk.html` | `skills/powerops-plan/references/samples/canvas-repairs-desk.map.md` | not at baseline (decision Q17) | 1 (lead), 4 (one meaning, two colours), 7 (not the product) |
| 3 | `skills/powerops-plan/references/samples/powerbi-repairs-performance.html` | `skills/powerops-plan/references/samples/powerbi-repairs-performance.map.md` | not at baseline (decision Q17) | none keyed (fails on scores) |
| 4 | `tests/sketch-eval/row-4/mockups/prototype.html` | `tests/sketch-eval/row-4/mockups/prototype.map.md` | not at baseline | 4 (one meaning, two colours), 6 (unreadable), 8 (could belong to anyone) |
| 5 | `skills/powerops-plan/references/samples/pages-tenant-repairs.html` | `skills/powerops-plan/references/samples/pages-tenant-repairs.map.md` | not at baseline (decision Q17) | 4 (one meaning, two colours) |

Each row's plan is `tests/sketch-eval/row-<n>/plan.md`.

Where each verdict comes from:

- Row 1: the trial's picked sketch, graded "average quality at best" and 5 of 10 for design in trial 3.
- Rows 2, 3 and 5: the owner passed the three samples (2026-11-11; row 5 on the second version), but M97's calibration did not reproduce that pass: after fixes in two rounds (decisions Q16 and Q17), its third round still failed all three rows, on parts an earlier round's judge passed. By the person's answer to Q17 of M97, the verdicts are still the judge's, not the owner's pass. Every run since has agreed: each run of M117's three rounds and of M118's rerun gave these rows `not at baseline`.
- Row 4: the owner's "Fail" on the first Power Pages version: "Too plain, No imagery, Colours or type". The first judge passed it; gate 8 exists because of that miss.

Where the keyed gates come from: they are the gates that failed in all three runs of M117's round C and of M118's rerun, keyed on the owner's answer to M117's Q2 ("Take all recommended", 2026-11-16). Row 1's 1 and 7 and row 4's 8 were keyed before M118, which confirmed them; row 4 gains 4 and 6, and rows 2 and 5 gain theirs. Row 4's gate 7 failed in round C on its phone frames' missing footer and passes since M118's phone-frame rule, so it is not keyed. Row 3 fails no gate in any of those runs and fails on its scores. Row 1 also fails gates 2, 3 and 4 in all six of those runs: recorded, not keyed. The runs are in the calibration files of the M97, M117 and M118 plans.

Recorded, not a pass rule: the amendment expects row 1 to score 2 or under on dimensions 1 (first look and lead), 2 (hierarchy and type), 4 (layout, spacing and fill), 7 (product fit and frame) and 9 (identity).
