# M97 calibration

The judge, `powerops-review-sketch`, run on the five rows of `tests/sketch-eval/key.md` (plan row 16, step 7).

## Setup

- Judge text at commit `3e53433` (with the gate 6 avatar exemption of decision Q15), built in Release (`0.0.0+3e53433`).
- Five scratch git repos outside this one, each with a neutral plan folder (`sample-a` to `sample-e`) holding only the row's `plan.md`, the sketch as `mockups/prototype.html` and its map, and `powerops skills install . --agent claude` from that build. The skills were read from each repo's `.claude/skills/`, not from the user's plugin.
- Each row in its own fresh Opus context (effort high), told to follow the installed `SKILL.md` exactly and to read nothing outside its repo; none saw `key.md`, the README or another row. Rendering: Chrome headless through `measure.py` on Linux, without Segoe UI (fallback fonts).

| Row | Folder | Key | Round 1 verdict | Gates failed | Scores (dimensions 1 to 9) | Match |
|---|---|---|---|---|---|---|
| 1, trial sketch | sample-c | not at baseline; 1, 7 | not at baseline | 1, 2, 3, 4, 6, 7 | 1 2 2 2 1 2 1 2 2 | yes |
| 2, canvas sample | sample-e | at baseline | not at baseline | 1 | 2 3 3 3 3 3 3 3 3 | **no** |
| 3, Power BI sample | sample-a | at baseline | at baseline | none | 3 3 3 3 3 3 3 3 3 | yes |
| 4, Pages first version | sample-d | not at baseline; 8 | not at baseline | 1, 4, 6, 8 | 2 2 2 3 3 3 3 3 1 | yes |
| 5, Pages sample | sample-b | at baseline | not at baseline | 1, 4, 6 | 2 3 2 3 3 3 3 3 3 | **no** |

Row 1's recorded expectation (dimensions 1, 2, 4, 7 and 9 at 2 or under) holds: 1, 2, 2, 1, 2.

## The two misses

Both are gate faults the judge found in samples the owner passed, confirmed in the pictures:

- **Row 2, gate 1.** On Repairs (phone), the filled "Log a repair" button, drawn with its focus ring at rest, and the 24 px red "3" outweigh the 12 px time left that the row's plan names as the first look. The row's plan line is ours (step 5) and gives the phone frame the desktop's first look; the sample's own caption says "the top emergency, then the docked action".
- **Row 5, gate 1.** On Report a repair and Your repairs, the 44 px title in the brand band (P4's inner page title) outweighs the plan's first look. Three judges (rows 1, 2 and 5) said gate 1 ("the largest or strongest item") reads against S1's own check ("cover the title").
- **Row 5, gate 4.** The accent amber numbers the four steps on Home, though P9 keeps it for the main action; and the tint `#E4F1F2` fills both the selected row and the "Scheduled" badge (row 4 has the same).
- **Row 5, gate 6.** On Your repairs (phone), the avatar's "MS" is 12 px with no name beside it (the greeting is under the title); decision Q15 exempts initials beside the name, and was asked about the desktop header, where "Maria S." is beside them. `measure.py` skips every avatar, so only the judge's eye caught it.

By the plan (Done line 3), a confirmed gate fault on a passed sample is reported to the person; neither the samples nor the judge are changed to hide it. The building session stopped here and asked.

## Other notes from the judges

- Every judge's `plan status` stopped on the row plans' missing `---` header ("malformed header"), so no run had the tool read the verdict back; rows 4 and 5's maps also hold a URL on line 5. Fixture gaps, not judge faults.
- The skill doesn't say whether round 1 repeats the `judge:` and `sketch-hash:` lines under its heading; all five wrote them once at the top.
- Gate 8 on a canvas or report sketch: judges wrote "pass (not applicable)".
- `measure.py` checks S4's five sizes on a site and a report, where P4 and B8 replace S4; it measures only `data-screen` frames, not state tiles.
- Row 3: the Selected tile's dimmed cross-highlight marks are about 1.5 to 2.2:1; the judge passed gate 6 as product-rendered context with labelled values, and said a literal reading fails it.

## measure.py on the three samples (step 5)

- Font fallbacks: confirmed (Segoe UI isn't installed on Linux); no gate.
- Pages Home: six font sizes against S4; not a gate, and P4 replaces S4 on a site.
- Pages "MS" initials at 12 px: exempt by decision Q15 beside "Maria S."; the phone header instance is the gate 6 miss above.

## Round 2

The person's answer to round 1 (decision Q16, "Fix samples, rerun"): the four faults fixed in the canvas and Power Pages samples, gate 1 reworded to cover the title, the judge's measuring step told so, and row 2's plan given the phone frame's own first look (`449db1a`). Same setup, at `449db1a` (`0.0.0+449db1a`), in five new scratch repos with new neutral names (`sample-g`, `-h`, `-k`, `-m`, `-p`) and five new fresh Opus contexts.

| Row | Folder | Key | Round 2 verdict | Gates failed | Scores (dimensions 1 to 9) | Match |
|---|---|---|---|---|---|---|
| 1, trial sketch | sample-k | not at baseline; 1, 7 | not at baseline | 1, 2, 3, 4, 6, 7 | 2 2 2 2 1 3 1 2 2 | yes |
| 2, canvas sample | sample-m | at baseline | not at baseline | 1, 4 | 2 3 2 3 3 3 3 3 3 | **no** |
| 3, Power BI sample | sample-h | at baseline | at baseline | none | 3 3 3 3 3 3 3 3 3 | yes |
| 4, Pages first version | sample-p | not at baseline; 8 | not at baseline | 4, 6, 8 | 3 2 2 3 3 3 3 2 1 | yes |
| 5, Pages sample | sample-g | at baseline | not at baseline | 1 | 2 3 3 3 3 3 3 3 3 | **no** |

Row 1's recorded expectation holds again (2, 2, 2, 1, 2).

### The second miss

- **Row 5, gate 1, only.** On Your repairs (phone), with the title covered, the next visit (22 px) ties the "All repairs" heading and the full-width docked amber "Report a repair" is the strongest item. The judge said that if a docked primary action doesn't count as competing, the sketch is at baseline. Gates 4 and 6, which round 1 failed, pass: the phone initials are now at the floor and the selection no longer uses the badge tint.
- **Row 2, gates 1 and 4.** Gate 1 on Repairs (phone): the full-width docked "Log a repair" (docked by the round 1 fix, as the sample's caption says) is the strongest item, and the top emergency's time left is 14 px like rows 2 and 3. Gate 4: the "Scheduled" badge in the Log a repair home card uses the selection's tint pair, and the caution colour means Urgent, Awaiting parts and a worse trend. Round 1's judge passed gate 4 on the same, then unchanged, parts.

Both remaining gate 1 failures are the same reading: a docked full-width primary action on a phone outweighs the lead. Gate 4 on row 2 shows that two fresh judges disagree on unchanged parts.

By plan step 7, a second miss stops the milestone and is reported. The building session stopped here: nothing more was changed, and no third round ran.
