# Sketch judge calibration

These five rows measure the judge, `skills/powerops-review-sketch/SKILL.md`: a judge is right when it separates sketches that already have a verdict from the owner or the trial's grader. The expected verdicts are in `key.md`, and only there. Rows 2, 3 and 5 hold the judge's own verdict, not the owner's pass (decision Q17 of M97). Judge consistency is no longer a follow-up: since M117 and M118, fresh judges give each row one verdict and one result per gate in 3 runs, so `key.md` also names the gates each row must fail, on the owner's answer to M117's Q2, except row 3, which fails on its scores.

A change to the judge's text or `measure.py`, or to the scale or a gate in `skills/powerops-plan/references/baseline.md`, reruns all five rows, and every row must keep its verdict and its failed gates. This folder is not part of `tests/eval/`, and nothing here ships.

## The rows

Each row folder holds a short `plan.md`, with the `Screens` lines and any `Assumptions` line the judge needs. Rows 1 and 4 also hold their sketch, as `mockups/prototype.html` (row 4 with its map, `mockups/prototype.map.md`). Rows 2, 3 and 5 take their sketch and map from `skills/powerops-plan/references/samples/`, as `key.md` says.

- Row 1's sketch is the picked sketch of trial 3 (its plan's `mockups/prototype.html` of 2026-11-11). It holds made-up data only and has no map.
  Its one real-looking company name was replaced with a made-up one of the same length; nothing else changed.
- Row 4's sketch and map are the first version of the Power Pages sample, which the owner failed.

## How to run a row

1. Make a scratch plan folder outside this repo, with a neutral name that says nothing about the row (for example `docs/plans/sample-a/` in an empty scratch repo).
2. Copy the row's `plan.md` into it, and the row's sketch and map into its `mockups/` folder as `prototype.html` and `prototype.map.md`.
3. Start `powerops-review-sketch <name> prototype.html` there in a fresh context, on the judge text under test. The judge must see no other file of this folder: not `key.md`, not this README, not another row.
4. Compare the judge file's last `verdict:` line and its gates with the row in `key.md`.

Record each run, with the commit of the judge text, the model and each row's verdict and failed gates, where the change that asked for it records its checks.

`SketchMeasureTests` checks that every file `key.md` names exists.
