# Key

The verdict each row must get. A `not at baseline` row must also fail the gates named here; other gates may fail too. Paths are from the repo root.

| Row | Sketch | Map | Verdict | Gates that must fail |
|---|---|---|---|---|
| 1 | `tests/sketch-eval/row-1/mockups/prototype.html` | none | not at baseline | 1 (lead), 7 (not the product) |
| 2 | `skills/powerops-plan/references/samples/canvas-repairs-desk.html` | `skills/powerops-plan/references/samples/canvas-repairs-desk.map.md` | not at baseline (decision Q17) | none keyed |
| 3 | `skills/powerops-plan/references/samples/powerbi-repairs-performance.html` | `skills/powerops-plan/references/samples/powerbi-repairs-performance.map.md` | not at baseline (decision Q17) | none keyed |
| 4 | `tests/sketch-eval/row-4/mockups/prototype.html` | `tests/sketch-eval/row-4/mockups/prototype.map.md` | not at baseline | 8 (could belong to anyone) |
| 5 | `skills/powerops-plan/references/samples/pages-tenant-repairs.html` | `skills/powerops-plan/references/samples/pages-tenant-repairs.map.md` | not at baseline (decision Q17) | none keyed |

Each row's plan is `tests/sketch-eval/row-<n>/plan.md`.

Where each verdict comes from:

- Row 1: the trial's picked sketch, graded "average quality at best" and 5 of 10 for design in trial 3.
- Rows 2, 3 and 5: the owner passed the three samples (2026-11-11; row 5 on the second version), but M97's calibration did not reproduce that pass. Round 1 found four faults in rows 2 and 5, which were fixed (decision Q16), and round 2 a docked phone action, which gate 1 now allows (decision Q17). Round 3 still failed all three rows, each only on parts that an earlier round's judge passed and that no fix touched. By the person's answer to Q17, the key records the judge's verdict instead of chasing it, and judge consistency is a follow-up. No gate is keyed for these rows, because fresh judges failed different gates on the same parts. Round 3 failed: row 2, gates 1 (Log a repair's focus is on the second field, where this row's plan names the first field; the sample's caption says "the field in focus") and 4 (the time left is red in the Success tile but black in Repairs; two bad differences in the summary strip use two colours); row 3, gate 6 (the faded bars of the Selected tile are under 3:1, where Power BI's own cross-highlight draws them); row 5, gate 4 (an amber tick inside the confirmation and step 1 drawings reads as a status colour). The record is in the calibration file of the M97 plan.
- Row 4: the owner's "Fail" on the first Power Pages version: "Too plain, No imagery, Colours or type". The first judge passed it; gate 8 exists because of that miss.

Recorded, not a pass rule: the amendment expects row 1 to score 2 or under on dimensions 1 (first look and lead), 2 (hierarchy and type), 4 (layout, spacing and fill), 7 (product fit and frame) and 9 (identity).
