# Key

The verdict each row must get. A `not at baseline` row must also fail the gates named here; other gates may fail too. Paths are from the repo root.

| Row | Sketch | Map | Verdict | Gates that must fail |
|---|---|---|---|---|
| 1 | `tests/sketch-eval/row-1/mockups/prototype.html` | none | not at baseline | 1 (lead), 7 (not the product) |
| 2 | `skills/powerops-plan/references/samples/canvas-repairs-desk.html` | `skills/powerops-plan/references/samples/canvas-repairs-desk.map.md` | at baseline | none |
| 3 | `skills/powerops-plan/references/samples/powerbi-repairs-performance.html` | `skills/powerops-plan/references/samples/powerbi-repairs-performance.map.md` | at baseline | none |
| 4 | `tests/sketch-eval/row-4/mockups/prototype.html` | `tests/sketch-eval/row-4/mockups/prototype.map.md` | not at baseline | 8 (could belong to anyone) |
| 5 | `skills/powerops-plan/references/samples/pages-tenant-repairs.html` | `skills/powerops-plan/references/samples/pages-tenant-repairs.map.md` | at baseline | none |

Each row's plan is `tests/sketch-eval/row-<n>/plan.md`.

Where each verdict comes from:

- Row 1: the trial's picked sketch, graded "average quality at best" and 5 of 10 for design in trial 3.
- Rows 2, 3 and 5: the owner's "Pass" on the three samples (2026-11-11; row 5 on the second version).
- Row 4: the owner's "Fail" on the first Power Pages version: "Too plain, No imagery, Colours or type". The first judge passed it; gate 8 exists because of that miss.

Recorded, not a pass rule: the amendment expects row 1 to score 2 or under on dimensions 1 (first look and lead), 2 (hierarchy and type), 4 (layout, spacing and fill), 7 (product fit and frame) and 9 (identity).
