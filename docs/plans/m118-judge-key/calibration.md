# M118 calibration

## Summary

- 15 fresh judges on the five rows at `188beaf`, the text round of Q5 and Q6: in every row all three runs gave the same verdict and the same failed gates; all 15 verdicts `not at baseline`.
- Failed gates per row: 1: 1, 2, 3, 4, 7; 2: 1, 4, 7; 3: none (it fails on scores); 4: 4, 6, 8; 5: 4. Rows 2 to 5 equal the list the owner approved (Q2 with Q5), and row 1 fails its keyed 1 and 7, so Done lines 3 and 4 hold and `key.md` was edited.
- No run of row 4 or row 5 failed gate 7 on a phone frame's footer.
- "No flip seen in 3 runs", not proof. Scores still differ by one point on single dimensions between runs; no verdict turns on them.

## Setup

- Judge text: commit `188beaf187c237d91bb474b6192914f337a0191e` (step 3). `measure.py` sha256 `52d0e222f64a16f155b488441cdde60e74edfe4ef5ed28887ab56ba99571fe76`, byte-for-byte as on `61fc080`.
- Judges: 15 runs, each a fresh Opus 5.5 subagent at high effort, run 2026-11-16.
- Each run in its own scratch repo outside this repo, with a neutral folder and plan name not used by M97, #166 or M117's rounds A to C, rows shuffled. Each repo held the row's `plan.md`, its sketch and map as `mockups/prototype.html` and `prototype.map.md`, and the two skills from the commit under test, with no `samples/` folder. The brief was M117's round C brief: it names no row, key or test.
- Verdict, failed gates, the nine scores and `sketch-hash` were extracted from each judge file and checked against the files by the main session. Every `sketch-hash` equals the SHA-256 of the row's sketch (CRLF made LF). The judge files are kept outside the repo.

## Judge runs

| Row | Run | Verdict | Failed gates | Scores 1-9 | sketch-hash |
|---|---|---|---|---|---|
| 1 | 1 | not at baseline | 1, 2, 3, 4, 7 | 2 2 2 1 1 2 1 1 2 | `c5bbacbb` |
| 1 | 2 | not at baseline | 1, 2, 3, 4, 7 | 2 2 2 1 1 2 1 1 1 | `c5bbacbb` |
| 1 | 3 | not at baseline | 1, 2, 3, 4, 7 | 2 2 3 1 1 2 1 2 1 | `c5bbacbb` |
| 2 | 1 | not at baseline | 1, 4, 7 | 2 2 3 2 2 2 1 3 3 | `ca77a2cc` |
| 2 | 2 | not at baseline | 1, 4, 7 | 2 2 3 2 2 2 1 3 3 | `ca77a2cc` |
| 2 | 3 | not at baseline | 1, 4, 7 | 2 2 3 2 2 2 1 3 3 | `ca77a2cc` |
| 3 | 1 | not at baseline | none | 3 2 2 2 2 2 2 2 3 | `aaec8123` |
| 3 | 2 | not at baseline | none | 3 2 2 2 2 3 2 3 3 | `aaec8123` |
| 3 | 3 | not at baseline | none | 3 2 2 2 2 3 2 3 3 | `aaec8123` |
| 4 | 1 | not at baseline | 4, 6, 8 | 3 2 2 2 2 2 1 2 2 | `5977fe7d` |
| 4 | 2 | not at baseline | 4, 6, 8 | 3 2 3 2 2 2 2 2 2 | `5977fe7d` |
| 4 | 3 | not at baseline | 4, 6, 8 | 3 2 2 3 2 2 2 2 2 | `5977fe7d` |
| 5 | 1 | not at baseline | 4 | 3 2 3 2 2 2 2 2 3 | `7424bffa` |
| 5 | 2 | not at baseline | 4 | 3 2 2 3 2 2 2 2 3 | `7424bffa` |
| 5 | 3 | not at baseline | 4 | 3 2 2 3 2 2 2 2 3 | `7424bffa` |

## Per row

| Row | Verdict, 3 runs | Failed gates, 3 runs | Approved list | Scores that differ (dimension: runs 1/2/3) |
|---|---|---|---|---|
| 1 | not at baseline | 1, 2, 3, 4, 7 | at least 1, 7 | 3: 2/2/3; 8: 1/1/2; 9: 2/1/1 |
| 2 | not at baseline | 1, 4, 7 | 1, 4, 7 | none |
| 3 | not at baseline | none | none | 6: 2/3/3; 8: 2/3/3 |
| 4 | not at baseline | 4, 6, 8 | 4, 6, 8 | 3: 2/3/2; 4: 2/2/3; 7: 1/2/2 |
| 5 | not at baseline | 4 | 4 | 3: 3/2/2; 4: 2/3/3 |

Row 1's gates 2, 3 and 4 fail beyond its key, as in round C: recorded, not a stop (Done line 4).

## Gate 7 on rows 4 and 5

All six runs passed gate 7, and each stated why for the phone frames. In short:

- Row 4, run 1: every frame `size: ok`, empty share at most 0.104; the browser frames have the header and the footer; the phone frames are 844 px tall (at most 932) and have the header.
- Row 4, run 2: all five frames `size: ok`, empty share at most 0.104; the three browser frames have the header and the footer; both phone frames are 844 px tall and have the header.
- Row 4, run 3: every frame `size: ok`, empty shares 0.104, 0.102, 0.099, 0.011 and 0.087, all under 0.15; the three browser frames have the header and the footer; both phone frames (844 tall) have the header.
- Row 5, run 1: size ok on all five frames, empty share at most 0.097; the browser frames have the header and the footer; both phones (844 tall) have the header.
- Row 5, run 2: every frame size `ok`, empty share at most 0.097; the three browser frames have the header and the footer; both phone frames (844 tall) have the header.
- Row 5, run 3: every frame `size: ok`, empty share at most 0.097; the browser frames have the header and the footer; both phone frames are 844 tall and have the header.

## Against M117's round C

Round C (at `59b2a6f`) and this rerun agree on every gate of every row except row 4's gate 7: round C failed it in all 3 runs on the phone frames' missing footer, and this rerun passed it in all 3, which is what Q5's sentence was meant to change. Q6's gate 1 sentence changed no gate result: row 2's gate 1 still fails in all runs, as round C's judges already applied the `SKILL.md` rule it copies.

## Residual

- Scores differ by one point on single dimensions between runs (the table above), as in M117. No verdict turns on them: every row except row 3 has a failed gate in every run.
- Row 3 fails on its scores alone, so it is the row most exposed: in this rerun it had five to seven dimensions under 3 per run, against a pass that needs all nine at 3 or more (Identity may be 2 for Power BI). A one-point drift on several dimensions at once would be needed to flip it; none was seen in 3 runs.
