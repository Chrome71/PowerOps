# M117 calibration

## Step 5: compare and dry run

Run by step 3's executor, and rerun after the orchestrator's decisions below, on the final `skills/powerops-review-sketch/measure.py` (sha256 `e1a8add045d5a28d33694572b4926d6e6060ee2593addaa5928f5561fed07b7a`), on top of `28b3c65`, against the script of `daa56f6` (#166). Linux, headless Chrome, Segoe UI not installed (the font fallbacks are the same before and after). The five sketches are the ones `tests/sketch-eval/key.md` names.

### The compare

- Control, old against old: `measure` gave byte-equal JSON on all five sketches; `shoot` gave the same file names and pixel-equal pictures (row 1: 53 pictures, row 2: 12, row 3: 13, row 4: 15, row 5: 16).
- `shoot`, old against new: the same names and pixel-equal pictures, all 109.
- `measure`, old against new: every difference is one of Data row 3's; no other field of any frame changed. Per sketch:
  - Row 1: `states` added (32 tiles, all inside frames, so none measured again); every frame gains `colours`, `title`, `top-text` and `size` (`no size class` on all eight); My requests' overflow entry ("Kestrel Facilities Management and Bui...") gains `ellipsis: true`, and its fault moves from `overflow` to `ellipsis`. Top-level `faults` stays 9.
  - Row 2: `states` added (6 tiles, all outside the frames, each with one font fault); the new frame fields. Top-level `faults` 5 to 11 (the tiles' six).
  - Row 3: `states` added (6 tiles outside, each one font fault); the new frame fields. `faults` 2 to 8.
  - Row 4: `states` added (6 tiles outside, each one font fault); the new frame fields; Your repairs loses its `sizes` fault (6 sizes: P4 allows six on a site, S4 five). `faults` 6 to 11.
  - Row 5: `states` added (6 tiles outside, no fault); the new frame fields; Home loses its `sizes` fault (P4). `faults` 4 to 3.
  - No lone avatar is under the floor in any of the five (G13 changes nothing here). Row 1's entry is the only `overflow` entry of the five.

### G14's chroma threshold

The `:root` tokens of the five sketches have a chroma (max minus min channel, 0 to 255) of 0 or 4, or 9 and up: nothing lies between. The neutrals are the greys, white and the two off-white surfaces (`--page` `#F6F5F2` and `--board` `#E2E6E6`, chroma 4); the lowest-chroma colour with a meaning is `--pos-bg` `#F1FAF1` (9), a status tint. The dark ink of the headers (`--ink`, `--hdr`) and the tinted text on them (`--on-ink-muted`, `--on-deep`) are brand-hue surfaces and text, not neutrals. Any threshold from 4 to 8 separates them; `measure.py` uses 6 (`NEUTRAL_CHROMA`), the middle. Every token is on its side:

| Token | Value | Chroma | Class | Rows |
|---|---|---|---|---|
| `--divider` | `#E0E0E0` | 0 | neutral | 1 |
| `--fg1` | `#242424` | 0 | neutral | 2+3+4+5 |
| `--fg2` | `#424242` | 0 | neutral | 2+3+4+5 |
| `--fg3` | `#616161` | 0 | neutral | 2+3+4+5 |
| `--fg4` | `#707070` | 0 | neutral | 2+3+4+5 |
| `--fill` | `#F0F0F0` | 0 | neutral | 2+3+4+5 |
| `--fill-2` | `#E6E6E6` | 0 | neutral | 2+3+4+5 |
| `--grid` | `#EBEBEB` | 0 | neutral | 2+3+4+5 |
| `--mark-3` | `#616161` | 0 | neutral | 1 |
| `--mark-ctx` | `#949494` | 0 | neutral | 2+3+4+5 |
| `--mark-ctx-strong` | `#767676` | 0 | neutral | 3 |
| `--mark-target` | `#707070` | 0 | neutral | 2+3+4+5 |
| `--neu` | `#424242` | 0 | neutral | 2+3+4+5 |
| `--neu-bd` | `#D1D1D1` | 0 | neutral | 2+3+4+5 |
| `--neu-bg` | `#F0F0F0` | 0 | neutral | 2+3+4+5 |
| `--neutral-bg` | `#F5F5F5` | 0 | neutral | 1 |
| `--neutral-fg` | `#424242` | 0 | neutral | 1 |
| `--on-brand` | `#FFFFFF` | 0 | neutral | 1+2+3+4+5 |
| `--on-hdr` | `#FFFFFF` | 0 | neutral | 1 |
| `--on-ink` | `#FFFFFF` | 0 | neutral | 2+3+4+5 |
| `--page` | `#F5F5F5` | 0 | neutral | 1 |
| `--stroke` | `#8A8A8A` | 0 | neutral | 1 |
| `--stroke` | `#E0E0E0` | 0 | neutral | 2+3+4+5 |
| `--stroke-input` | `#8A8A8A` | 0 | neutral | 2+3+4+5 |
| `--stroke-strong` | `#D1D1D1` | 0 | neutral | 2+3+4+5 |
| `--subtle` | `#EBEBEB` | 0 | neutral | 1 |
| `--subtle` | `#FAFAFA` | 0 | neutral | 2+3+4+5 |
| `--surface` | `#FFFFFF` | 0 | neutral | 1+2+3+4+5 |
| `--text` | `#242424` | 0 | neutral | 1 |
| `--text-2` | `#616161` | 0 | neutral | 1 |
| `--board` | `#E2E6E6` | 4 | neutral | 2+3+4+5 |
| `--page` | `#F6F5F2` | 4 | neutral | 2+3+4+5 |
| `--pos-bg` | `#F1FAF1` | 9 | hue | 1+2+3+4+5 |
| `--caution-bg` | `#FFF9F5` | 10 | hue | 1 |
| `--crit-bg` | `#FDF3F4` | 10 | hue | 1+2+3+4+5 |
| `--warn-bg` | `#FFF9F5` | 10 | hue | 2+3+4+5 |
| `--sel` | `#E6F1F2` | 12 | hue | 1 |
| `--brand-tint` | `#E4F1F2` | 14 | hue | 2+3+4+5 |
| `--info-bg` | `#EBF3FC` | 17 | hue | 1 |
| `--brand-tint-2` | `#CBE4E7` | 28 | hue | 2+3+4+5 |
| `--on-deep` | `#CBE4E7` | 28 | hue | 5 |
| `--on-ink-muted` | `#B7D0D5` | 30 | hue | 2+3+4+5 |
| `--accent-tint` | `#FDF3DC` | 33 | hue | 5 |
| `--ink` | `#0E2A30` | 34 | hue | 2+3+4+5 |
| `--ink-2` | `#17383F` | 40 | hue | 2+3+4+5 |
| `--hdr-selected` | `#06303A` | 52 | hue | 1 |
| `--hdr-pressed` | `#083B41` | 57 | hue | 1 |
| `--pos-bd` | `#9FD89F` | 57 | hue | 2+3+4+5 |
| `--deep-3` | `#083F47` | 63 | hue | 5 |
| `--crit-bd` | `#EEACB2` | 66 | hue | 2+3+4+5 |
| `--brand-press` | `#06434A` | 68 | hue | 2+3+4+5 |
| `--warn-bd` | `#FDCFB4` | 73 | hue | 2+3+4+5 |
| `--hdr` | `#0B4F57` | 76 | hue | 1 |
| `--mark-2` | `#0B4F57` | 76 | hue | 1 |
| `--deep` | `#0A4F58` | 78 | hue | 5 |
| `--mark-4` | `#3D8A91` | 84 | hue | 1 |
| `--brand-hover` | `#095861` | 88 | hue | 2+3+4+5 |
| `--brand-hover` | `#0E5F68` | 90 | hue | 1 |
| `--hdr-hover` | `#0E5F68` | 90 | hue | 1 |
| `--deep-2` | `#0D5E68` | 91 | hue | 5 |
| `--pos` | `#0E700E` | 98 | hue | 2+3+4+5 |
| `--pos-fg` | `#0E700E` | 98 | hue | 1 |
| `--brand` | `#0F6E78` | 105 | hue | 1 |
| `--mark-1` | `#0F6E78` | 105 | hue | 1 |
| `--brand` | `#0B6A75` | 106 | hue | 2+3+4+5 |
| `--mark` | `#0B6A75` | 106 | hue | 2+3+4+5 |
| `--info-fg` | `#0F548C` | 125 | hue | 1 |
| `--caution-fg` | `#8A3707` | 131 | hue | 1 |
| `--warn` | `#8A3707` | 131 | hue | 2+3+4+5 |
| `--accent-ink` | `#8A5A00` | 138 | hue | 5 |
| `--crit` | `#B10E1C` | 163 | hue | 2+3+4+5 |
| `--crit-fg` | `#B10E1C` | 163 | hue | 1 |
| `--accent` | `#F4B740` | 180 | hue | 5 |
| `--mark-attn` | `#C2570C` | 182 | hue | 2+3+4+5 |
| `--accent-hover` | `#E5A52B` | 186 | hue | 5 |

### svg classes, checked against the pictures

- Row 1: no `svg` (its Power BI frame draws its bars with `div`s).
- Row 2: 19 icons, right: the 16 and 20 px glyphs, and the 32 px check in a ring in the Empty tile (`svg.i.s32`, one colour, at most 48 px; `imagery` by the 24 px test before decision 3).
- Row 3: 13 charts, right: the trend, the trade bars and the open-repairs bars (`svg.chart`), the sparklines (`svg.spk`, 96 by 24) and the tooltip chart; 3 icons. The report's mark (`svg.mk`) is classed a chart, because the title row is a `div`, not a `header`; its only colour is white, so it is not in `colours` (Open item 4).
- Row 4: 25 icons, right, the two 32 px one-colour glyphs (`svg.i.s32`, the check in the Confirmation tile and the icon in the Empty tile) included since decision 3. Its three bullet bars (`svg.bul`) draw only neutral colours here, so they are not in `colours`.
- Row 5: 38 icons, right; 3 charts (the bullet bars, below); 22 drawings classed `imagery`, right: the hero scene, the eleven pictograms (`svg.pic`, 64 and 80 px, with the amber lit detail), the three skylines (`svg.sky`, 1366 by 72, one colour `#17383F`: over 48 px, so not an icon, decision 8), the three building pieces, the phone street and the three state pictures (`svg.spic`, 96 px). The three bullet bars under 86.4%, 9.2 and 78.3% (`svg.bul`, 298 by 16: a track, a value bar and a target tick) are data visuals: G10 as written (a chart only when it holds text) classed them `imagery`, so `measure.py` now also classes as a chart an svg drawn only of rectangles and lines (a bar, a bullet or a meter), and they are `chart`. That is a measurement beyond G10's words: the rule text needs it (Open item 1).

### The dry run

Gate 1 is G15 applied to `top-text` and the job line's first look in the row's `plan.md` (row 1: the trial plan's `Screens`). Gate 6 is from `overflow` (an `ellipsis` entry is never gate 6, G4), `overlaps`, `under-floor`, `contrast` and, for marks, the `colours` of each chart against the svg's `background`. Gate 7 is `size` and `empty-share` (S14's chrome is not measured and is not in this column). R3 is M97's round 3 (row level: its result on gates 1, 6 and 7); S166 is #166's rerun, per frame from its judge file (its Gates lines are copied below), where a frame its Gates line doesn't name is read as passing.

| Row | Frame | First look (plan) | Gate 1 measured | Gate 6 | Gate 7 | R3 (row) | S166 (frame) |
|---|---|---|---|---|---|---|---|
| 1 | My requests | the selected request's status and the next-step sentence | pass: the selected row carries the frame's only selected-row style (the selected tab and nav item don't count) | pass (the ellipsis is gate 3, G4) | fail: no size class | 1 fail, 6 fail, 7 fail | 1 pass, 6 fail (the ellipsis), 7 fail |
| 1 | New request | the first empty field, then submit | fail: position only, no focus ring; the field's label 14/600 against "States of New request" h3, the caption `p.kind` and `span.wm` at 16/600 | pass | fail: no size class | as above | 1 fail, 7 fail |
| 1 | Request detail | the status and the decision sentence | pass: the sentence (20/600) is the largest outside the covered title (28) | pass | fail: no size class | as above | 1 pass, 7 fail |
| 1 | Supplier requests | the oldest open request | fail: a row by position, no focus ring; its 14/600 text against 16/600 texts | pass | fail: no size class | as above | 1 pass, 7 fail |
| 1 | Supplier request | the decision comment field | fail: the field is on the Decision tab, not shown (G6) | pass | fail: no size class | as above | 1 fail, 7 fail |
| 1 | Supplier | the supplier's name and general fields | pass: the name is the title (28/600), the largest | pass | fail: no size class | as above | 1 pass, 7 fail |
| 1 | Supplier documents | the earliest expiry date | fail: a row's field by position, no focus ring; 14 against 16 | pass | fail: no size class | as above | 1 pass, 7 fail |
| 1 | Supplier onboarding overview | the summary cards, then the trend | pass: the card values (28/600) tie only the covered title | pass | fail: no size class | as above | 1 pass, 7 fail |
| 2 | Repairs | the time left on the selected emergency | pass: the selected row carries the only selected-row style (tab and rail don't count); "2 h 10 min" 32/700 is also the largest | pass | pass: ok, empty 0.022 | 1 fail, 4 fail | 1 pass |
| 2 | Log a repair | the first field of the form | fail: the only focus ring is on the third field; "Trade" 14/600 against "The problem" h3 16/600 and three 14/700 texts | pass | pass: ok, 0.010 | as above | 1 fail |
| 2 | Repairs (phone) | the top emergency, then the docked action | **fail**: a row by position, no focus ring; its title 14/600 against the chips "3", "14", "38" at 20/600 | pass | pass: ok, 0.076 | as above | 1 pass (read so: "Repairs and Repair (phone) pass") |
| 2 | Repair (phone) | the time left before the attend-by time | pass: "2 h 10 min" 32/700 is the largest | pass | pass: ok, 0.071 | as above | 1 pass |
| 2 | Log a repair (phone) | the first field of the form | fail: the focus ring is on "What is wrong?"; "Trade" 14/600 ties "The problem" 14/600, which comes first (the docked Continue is excluded) | pass | pass: ok, 0.107 | as above | 1 fail |
| 3 | Overview | the share completed on time against the 90% target | pass: "86.4%" 80/700, the largest | pass: text clean; highlighted marks 6.30 and 7.12:1, context grey `#949494` 3.03:1 | pass: ok, 0.053 | 6 fail | 1, 6, 7 pass |
| 3 | Overview (phone layout) | the same number | pass: 64/700, the largest | pass | pass: ok, 0.003 | as above | 1, 6, 7 pass |
| 4 | Home | the heading and the "Report a repair" button | pass since decision 2: the heading is the title and the first look, 48/600, the largest of the first screen; "86.4%" (48/700, 1,400 px lower) is no longer in `top-text` (it failed before decision 2) | pass | pass: ok, 0.104 | 4 fail, 8 fail | 1 pass ("does not compete") |
| 4 | Report a repair | the question of the current step | pass: the question (h1, 40/600) is the title since decision 7, so it is not covered, and the largest | pass | pass: ok, 0.102 | as above | 1 pass |
| 4 | Your repairs | the next visit's date and time | pass: the visit 40/700 against the covered title 40/600 | pass | pass: ok, 0.099 | as above | 1 pass |
| 4 | Home (phone) | the heading and the button | pass: the heading is the title, 28/600, the largest | pass | pass: ok, 0.011 | as above | 1 pass |
| 4 | Your repairs (phone) | the next visit's date and time | pass: the visit 20/700 outweighs "All repairs" 20/600 | pass | pass: ok, 0.087 | as above | 1 pass ("weak") |
| 5 | Home | the heading and the button | pass: the h1 64/700 is the title and the largest | pass | pass: ok, 0.093 | 4 fail | 1 pass |
| 5 | Report a repair | the question of the current step | pass: the question 32/700 against the covered title 44/700 | pass | pass: ok, 0.097 | as above | 1 pass |
| 5 | Your repairs | the next visit's date and time | pass: the visit ties "3" and "5" at 44/700 and comes first in reading order (same line, left) | pass | pass: ok, 0.076 | as above | 1 pass ("narrow") |
| 5 | Home (phone) | the heading and the button | pass: the h1 36/700, the largest | pass | pass: ok, 0 | as above | 1 pass |
| 5 | Your repairs (phone) | the next visit's date and time | pass: the visit 22/700 outweighs "All repairs" 22/600 | pass | pass: ok, 0.018 | as above | 1 pass ("narrow") |

Per row, the measured rules give:

- Row 1: gate 1 fail (four frames), gate 7 fail (all frames): its key holds. Gate 6 passes: its only measured fault is the ellipsis, which G4 makes gate 3 (C5); both before-runs failed gate 6 there, as the plan's Open questions already name. State tiles are all inside frames.
- Row 2: gate 1 fail (three frames), gate 4 fail (below), gates 6 and 7 pass: the same failed gates as both before-runs. Tiles: one font fault each, no other.
- Row 3: gates 1, 6 and 7 pass. Gate 6 rests on G9: in the Selected tile the trade bars are full `#B10E1C` (7.12:1) and dimmed `#E09FA4` (2.17:1), two tones of one hue beside a selected state, so the dimmed part is exempt; the dimmed grey bars `#C8C8C8` (1.67:1) and `#D4D4D4` (1.48:1) are neutral, so they are not in `colours` (Open item 5). R3 failed gate 6 on these, S166 passed it.
- Row 4: gate 4 fail; gates 1, 6 and 7 pass (gate 1 on Home since decision 2). Gate 8 is not measured. Its key (gate 8) is not touched by any measured rule.
- Row 5: gates 1, 6 and 7 pass; gate 4 fails (below), which matches the key's verdict; R3 failed gate 4 on another part, S166 passed it.

Gate 4, the hue groups (numbered per file) and their uses, from `colours`:

- Row 1: red (`#B10E1C`, `#FDF3F4`): Critical pills, required marks, error banner and field borders (bad). Orange (`#8A3707`, `#FFF9F5`): Caution pills. Green: one Positive pill. Teal (`#0F6E78`, `#0B4F57`, `#3D8A91`, `#06303A`, `#E6F1F2`): primary button, links, selected row and nav item, header band, avatars, and the overview's bars (`i` fills). Blue (`#0F548C`, `#EBF3FC`): Informative pills. The judge reads whether the overview's status bars give a status a second hue (S166's "narrow" pass on Escalated).
- Row 2: red: Critical badges, the emergencies count on the phone (`b.num.crit`), the time left in the Success tile (`span.left.crit`), the adverse difference `span.d.crit`, the error field. Orange (`#8A3707`, `#C2570C`, tints): Urgent badges, `span.d.warn`, the header's attention dot (`i.dot`), a warning dot. Green: Positive and the good difference. Teal: primary buttons, the selected row's bar, nav, avatar, header ink, spinner. By G14's last sentence, RP-20421's time left (red in the Success tile, ink in Repairs) and the count "3" (red on the phone, ink on the desktop) are one meaning in two colours: fail, as both before-runs.
- Row 3: red: bad values, Critical badge, the bars and sparklines under target. Orange: one Caution badge. Green: good values. Teal: the mark, the focus ring, the selected tab, the trend line (B6's highlighted series). One meaning each: pass, as both before-runs.
- Row 4: teal carries selection (`div.ro.on` on `#E4F1F2`) and the Informative status (`span.badge.info` "Scheduled", `#095861` on `#E4F1F2`): fail, as both before-runs.
- Row 5: amber `#F4B740` fills only the primary action (`a.btn.primary`) outside the drawings, whose colours are imagery (G10): R3's amber-tick failure is gone. Teal carries selection, links and the header, and the Informative "Scheduled" badge (`span.badge.info`, `#095861`): fail. S166 passed gate 4 noting this badge ("watch"); R3 failed it on the amber tick. The bullet bars (now charts) use teal for the value and ink for the target.

### #166's judge files, Gates lines

Matched to rows by their `sketch-hash:` line (the sketch's sha256). Copied as written.

#### Row 1 (`sketch-hash: c5bbacbba3756d2619d1fbf3a030aae54a94f0f42387a7261a3378e33e9f62d4`)

1. Lead: fail. New request: with the title covered, the filled Submit request is the strongest item, not the first empty field (Category) the job line names. Supplier request: the decision comment field is not visible in the default (Request tab) view at all.
2. Contradiction: fail. Overview "Requests this month 14" and November's bar of 14 against the request numbers: SR-01043 was filed 28 Oct and the newest open request is SR-01047 (10 Nov), so November has 4 by 11 Nov. Overview "Open requests 6" (5 Submitted, 1 Escalated) against the Open requests view, which lists 4 (3 Submitted, 1 Escalated). The overview empty tile, dated 1 Dec, says "Down 11 on last month", "Down 12 points" and "Last month: 3.0 days", while the main frame already shows 14, 14% and 2.5 days for November on 11 Nov. The refresh-failed tile shows the 11 Nov numbers under a 9 Nov as-of date.
3. Unbuildable: fail. My requests draws "Kestrel Facilities Management and Bui…" cut with an ellipsis; canvas.md section 3 (C5) says an ellipsis on overflow is not confirmed and long titles wrap; the plan's Assumptions are "None". No map exists to state any status.
4. One meaning, two colours: pass, narrowly. Escalated is orange on every pill but a lighter teal bar in the overview status chart; the chart's bars read as a data series, so this is held under B6, not here.
5. More than one filled primary: pass. One filled button per view.
6. Unreadable: fail. measure.py reports the Kestrel title clipped on My requests (`span.name.trunc`), confirmed in the picture. Contrast and the 12 px floor pass everywhere.
7. Not the product: fail. No frame has a size class; canvas frames are 1216 wide and not 1366 by 768, the report is not 1920 by 1080; the canvas frames lack navigation and the report has a filled title band (S14). Measured empty shares are 0 to 0.022, under 0.15.
8. Could belong to anyone: pass (not applicable: no Power Pages site; Identity scored 2).

#### Row 2 (`sketch-hash: ca77a2ccc470bed77437068e65e40977b3419d345aa6906135d0ca7e3ec0b89b`)

1. Lead: fail. Log a repair (desktop) and Log a repair (phone): the plan's first look is "the first field of the form" (Trade), but the strongest item is the focused third field "What is wrong?", with the second field "Where in the home" still empty above or beside it. Repairs and Repair (phone) pass.
2. Contradiction: pass. Counts, times, references and the 64% bar agree across the frames and the Success tile.
3. Unbuildable: pass. The main frames draw the tab counts and grey bar fills the plan records; the SVG mark looks the same as its PNG fallback; the bottom navigation is drawn on its GA route; `Confirm()` appears only in a state tile.
4. One meaning, two colours: fail. RP-20421 "5 h 40 min left" is ink in Repairs and critical red in the Success tile with the same status; the emergencies count "3" is ink on desktop and red on the phone; an adverse difference is caution on "Need triage" and critical on "Completed on time".
5. One filled primary per view: pass. One filled button in every frame and tile.
6. Unreadable: pass. No overflow, overlap or text under 12 px measured; all contrast pairs pass; the state tiles, not measured, show no faint or clipped text in the pictures.
7. Not the product: pass. Frames at 1366 by 768 and 390 by 844, header and navigation present, empty share at most 0.107.
8. Could belong to anyone: not applicable (canvas app; scored under Identity).

#### Row 3 (`sketch-hash: aaec8123fd96270559a7b430d6d3ce0e66b58afda2fa4091c898287d29205eec`)

1. Lead: pass. With the title covered, "86.4%" is the largest text (80 px, measured; `lead: not marked`, so decided by looking) and the strongest item; nothing else competes with it.
2. Contradiction: pass. Lead, comparison rows, line chart, insight, bar, table, tooltip, selected and error states agree (checked: weighted totals, 126 late repairs, 45% = 57, Hartwell Heating 221 jobs at 74.2%, column sums 412 and 71, 431 − 412 = 19 and 431 − 418 = 13).
3. Unbuildable: pass. The main Overview frame draws only parts the map marks GA; the phone frame's 390 px width is not confirmed and recorded in the plan's Assumptions.
4. One meaning, two colours: pass. Red means under target or overdue (bad) everywhere, green good, petrol brand and focus. The two greys and the petrol-versus-red company rate are B6 faults, not a status shown in two colours.
5. More than one filled primary action: pass. No filled button on any frame.
6. Unreadable: pass. No overflow, overlap or text under 9 pt (measured); every measured pair passes; state tiles checked by eye.
7. Not the product: pass. 1920 by 1080 and 390 by 844; the report's chrome is there; empty share 5.3% and 0.3%.
8. Could belong to anyone: pass (not applicable: a Power BI report, so it is the Identity score).

#### Row 4 (`sketch-hash: 5977fe7d913e9e1d0105ad3ff163921c8faf694334355e34843afe8c51265a23`)

1. Lead: pass. Home: the h1 (48 px, two lines) and the filled button lead the first screen; the 48 px "86.4%" is 1400 px lower and does not compete. Report a repair: the 40 px question leads. Your repairs: the 40 px visit time leads once the title is covered; on the phone it is weak (S1) but still the top, boldest item.
2. Contradiction: pass. 86.4/90, 9.2/8 and 78.3/75 match their differences and bars; reported 08:30 plus 6 hours gives "by 14:30 today", matching the 13:00 to 14:30 visit on Home, the list, the tracker and the phone; 3 open plus 6 completed fits "Showing 1 to 6 of 9".
3. Unbuildable: pass. The not-confirmed parts drawn (Noto Serif, the check-your-answers look) are recorded in the plan's Assumptions with fallbacks; the anonymous track card is Custom with a sign-in fallback; the empty state is the classic list's GA message.
4. One meaning, two colours, or one colour with two meanings: fail. The brand tint and brand hue mean selection (selected row, selected radio box) and also the status "Scheduled" (badge info on brand tint), so on Your repairs the selected RP-20417 row and its status badge are the same colour.
5. More than one filled primary action: pass. One filled button in every view and state.
6. Unreadable: pass. No overflow, overlap or text under 14 px (initials exempt); lowest contrast 4.95:1.
7. Not the product: pass. 1366 and 390 frames, product chrome on every page, empty share at most 0.104.
8. Could belong to anyone: fail. No imagery (no hero picture, line icons instead of pictograms) and the text face is the system default sans; only the mark and the serif heading carry the brand.

#### Row 5 (`sketch-hash: 7424bffa82d86ef65513b5197d214c5472beba061997f981160ec2a045b87a99`)

1. Lead: pass. Home: heading and button (the plan's first look) are the largest and strongest. Report: "What is the problem?" is the largest once the title is covered. Your repairs: "Today, 13:00 to 14:30" is the widest 44 px item, first in reading order, in the card with the petrol top stroke; the 44 px counts are weaker (single digits) but close. Phone: the date is 22 px 700 against "All repairs" 22 px 600, first and boxed; the docked button is the second look (Q17). Narrow pass on both Your repairs frames.
2. Contradiction: pass. Dates, references, times and counts agree across screens (RP-20417, 12 Nov 08:30, visit 13:00 to 14:30, 6-hour target by 14:30, 3 open of 9). "Dealt with at once" (Home step 2) against "within 6 hours" (step 4) is loose wording, not a broken promise, since the aside reads "We look at it ... emergencies at once".
3. Unbuildable: pass. The two not-confirmed parts in main frames (the Google Fonts and the check-your-answers look) are recorded in the plan's Assumptions with fallbacks; icons are drawn as inline SVG, which is the map's fallback for Font Awesome.
4. One meaning, two colours: pass. Amber is the main action only (and lit details in drawings, as the look rules allow); red and green are bad and good consistently. Watch the "Scheduled" badge in the brand family (S3).
5. More than one filled primary action: pass. One per view in every frame and state.
6. Unreadable: pass. No overflow, overlap or under-floor text; every measured pair passes; the 12 px initials are exempt.
7. Not the product: pass. 1366 browser and 390 phone frames, product chrome on every frame, empty share at most 0.097.
8. Could belong to anyone: pass. Own mark, own drawings, Noto Serif and Fira Sans rendered.

### Open items for the orchestrator

Each names the frame, the figure and a recommendation. None contradicts the key of row 1 or row 4: row 1 still fails gates 1 and 7, and no measured rule reaches row 4's gate 8.

1. **G10's words, bullet bars (fixed in `measure.py`; the rule text needs the same).** Row 5 Home's three `svg.bul` (298 by 16, no text) are bullet charts; "a chart when it holds text, imagery otherwise" classed them imagery, which the plan forbids for a data visual. `measure.py` now also classes as a chart an svg drawn only of rectangles and lines. Recommended G10 wording: "elsewhere it is a chart when it holds text or is drawn only of rectangles and lines (a bar, a bullet or a meter), and imagery otherwise."
2. **G15, a rival far below the first look (row 4 Home, gate 1).** Both before-runs passed gate 1 on row 4; the measured rule fails it: the first look is the h1 (48/600, the title), and "86.4%" (48/700), about 1,400 px lower in the performance section, has its size and a greater weight. S166 passed it because it "does not compete". Recommendation: count a rival only in the frame's first screen (the top 768 px of any frame but a phone, the whole phone frame), which is what S1's first look means; it keeps row 1's gate 1 fails (their rivals are within the top 768 px, and Supplier request fails on the hidden field, G6) and passes row 4 Home. If the rule is kept, row 4 gains a gate 1 fail; its verdict and keyed gate 8 don't change.
3. **G10's icon size (rows 2 and 4, `svg.i.s32`).** A 32 px one-colour glyph (the check in a ring, the empty-state icon) is an icon in the picture and `imagery` by the 24 px test, so its colour (green `#0E700E`, teal `#0B6A75`) leaves gate 4. No gate result changes on these rows. Recommendation: an svg drawn in one colour is an icon at any size (S7 then judges the size).
4. **G10's "inside the header" in a report (row 3, `svg.mk`).** The report's mark sits in a `div.titlerow`, not a `header`, so it is a chart. Its only colour is white (neutral), so nothing changes. Recommendation: read "the header" in a report as the page's title row, or leave it as a known case.
5. **G9 and neutral marks (row 3, Selected tile).** The dimmed grey bars `#C8C8C8` (1.67:1) and `#D4D4D4` (1.48:1) are the figures R3 failed gate 6 on; they are neutral, so `colours` doesn't list them, and G9 speaks of "two tones of one hue". The red pair (`#B10E1C` 7.12:1, `#E09FA4` 2.17:1) makes the visual cross-highlighted. Recommendation: G9 says the whole dimmed part of a cross-highlighted visual is exempt, grey context included, and SKILL says a neutral mark's ratio is read from the picture's hex, since `colours` lists only non-neutral colours.
6. **G15 on row 2 Repairs (phone) (the frame Fable named).** The first look "the top emergency" names a row by position; it has no focus ring, so the size test applies, and the summary chips "3", "14", "38" (20/600) outweigh the row's title (14/600): fail. S166 passed the frame (R3 named only Log a repair). Row 2 fails gate 1 on Log a repair anyway, so its result doesn't change. Recommendation: keep the rule; the row is drawn selected, not focused, and the chips are larger.
7. **The title can be a side panel's h2 (row 4 Report a repair).** The first h1, h2 or heading in reading order is "Your home" (an h2 at the top of the side column), above the step's h1. Gate 1 still passes there (the h1 question is the largest). Recommendation: "the frame's first h1, else its first h2 or heading".

8. **Decision 3 classes a long one-colour decoration as an icon (row 5, `svg.sky`).** The skylines at the foot of Home, Report a repair and Your repairs (1366 by 72, one colour `#17383F`) are drawings, now `icon`, so their ink-2 counts in the teal group for gate 4. Row 5's gate 4 already fails on the Informative badge, so no result changes. Recommendation: one colour makes an icon only up to 48 px on its longer side; a larger one-colour svg stays imagery. Closed by decision 8 below.

### Decisions on the open items

The orchestrator's answers (2026-11-15), and what was applied:

1. Bullet bars as charts: accepted as built.
2. A rival far below the first look: accepted. `top-text` lists only texts in the frame's first screen: the top 768 px of a desktop or browser frame, all of a phone or report frame (a frame with no size class is read whole). Self-test case: a 40 px text below 768 px in a browser frame is not in `top-text`. Row 4 Home now passes gate 1.
3. One-colour svg: accepted: an svg drawn in one colour (its painted colours, neutrals included) is an icon (at any size, as first decided; capped at 48 px by decision 8). It is decided after the chart tests (text, rectangles and lines only, a report), so a one-colour bar or sparkline stays a chart. Self-test case: a 40 by 40 one-colour triangle is an icon, and the drawing beside it (two colours) stays imagery. Consequence in Open item 8.
4. The report mark in a `div`: left as a known case.
5. G9 and grey context: a text rule only, written by the other executor: the whole dimmed part of a cross-highlighted visual is exempt, grey included.
6. Row 2 Repairs (phone): the rule is kept; it goes to the owner's Open questions as a frame S166 passed, with row 2's gate 1 result unchanged.
7. The title: accepted: `title` is the frame's first h1, else its first h2, else its first `[role=heading]`, each in reading order. Self-test case: an h2 left of the h1 on one line; the h1 is the title. Row 4 Report a repair's title is now its h1 question.
8. Open item 8, the one-colour size: accepted: an svg drawn in one colour is an icon only when its longer side is at most 48 px; the chart tests still come first. Self-test case: an 80 by 20 one-colour svg stays imagery. Row 5's three skylines are imagery again; nothing else changed class.
9. The selected-row wording below: accepted as proposed.

Each new case was red with its code reverted (round 2 of the red-first log: 25 cases; round 3, after decision 8: 26). After the decisions, the compare was rerun: the pictures of all five sketches are pixel-equal to the old script's (109), and the `measure` JSON still differs only by Data row 3's changes, as listed above. Against the run before the decisions, only these changed: the title of row 4 Report a repair; `top-text` of rows 4 and 5 Home and row 5 Report a repair (texts below 768 px left out); the class of the 32 px glyphs (rows 2 and 4, now icons). After decision 8 the compare was rerun again with the same result (pictures pixel-equal, JSON only Data row 3's changes, top-level faults 9, 11, 8, 11 and 3), and only row 5's skylines changed class, back to imagery.

### "The frame's only selected-row style" beside a selected tab

Proposed wording for G15: "A selected-row style is the fill, bar or check that marks one row of a list, table or gallery as the selected one; it is the frame's only one when no other row of the frame is drawn selected. A selected tab, pivot, segment or navigation item is navigation, not a row: it neither counts as the selected row nor competes with it." On row 2 Repairs this passes the selected emergency (`div.r.sel`) beside the selected tab "Need triage" (`span.tab.on`) and the rail's Repairs item (`span.nav.on`); on row 1 My requests, the selected request beside its selected tab and nav item.

### What `measure.py` measures beyond the plan's words

For SKILL's description of the fields; each was needed to keep a frame where both before-runs agree:

- A tile's own `header` (its first child of that tag) is the sketch's caption of the state, not the product, and is left out of the tile's measurement. Without it, rows 4 and 5 failed gate 6 on every tile (the 12 px caption under the site's 14 px floor), where both before-runs passed.
- In a tile, two texts don't overlap where an opaque box drawn above one of them hides it (a dialog over the dimmed screen). Without it, row 2's Confirm tile had two overlaps, where both before-runs passed gate 6. Frames are unchanged.
- `top-text` breaks equal size and weight by reading order, where two texts whose boxes share at least half the lower one's height are on one line and read left to right. Without it, row 5 Your repairs ordered the counts "3" and "5" before the visit, 7 px lower on the same line, and failed gate 1, where both before-runs passed it.
- `top-text` holds only the first screen (decision 2); `title` takes an h1 before an h2 (decision 7); a one-colour svg up to 48 px is an icon (decisions 3 and 8).
- G10's report rule covers a report's phone layout and its state tiles (tiles outside the frames of a file whose frames are a report and its phone layout).
- A shape cloned by `<use>` takes its symbol's own paint, else the use element's; a `line`'s fill and a `text` painted by its `tspan`s are not counted.
- `colours` leaves out the fill and border of the frame or tile itself (the surface; the phone's bezel), and blurred box shadows; it counts unblurred box shadows (rings and bars), outlines and `::before` and `::after`.

## Step 9: visible names beside avatars, and mark contrast

Two changes to `measure.py` after the judge runs found a gate 6 flip on row 4, on top of `f1dbebc` (sha256 before `e1a8add045d5a28d33694572b4926d6e6060ee2593addaa5928f5561fed07b7a`, after `52d0e222f64a16f155b488441cdde60e74edfe4ef5ed28887ab56ba99571fe76`):

1. G13: a name beside an avatar must be drawn text. An avatar's initials skip the floor only when its parent holds a measured text atom outside the avatar that no clipping box hides and that is not inside a box of 1 px or less that clips (`overflow`, `clip` or `clip-path`: a visually hidden label). Row 4's phone header holds the avatar beside an icon button whose only text is a visually hidden "Menu", so its "MS" was exempt.
2. Marks: per frame and measured tile, `marks: [{element, paint, against, ratio, svg, count}]`, for each svg classed icon or chart. A mark is a shape of the svg (or a `<use>` with the shapes it clones): its fill, or its stroke when it has no fill, and a line's or polyline's stroke; text is not a mark. `against` is the solid paint under the shape's centre: the last earlier filled shape of the svg whose box holds it, else the svg's `background`. Neutral colours are included; entries are de-duplicated by paint and against, with a count. A `marks` fault counts the pairs under 3:1.

Which marks count (the orchestrator's decision after the first listing): every mark stays in `marks`, with `counts: true|false`; only a counting mark under 3:1 makes the `marks` fault. A mark counts when (a) no later shape of the svg is drawn over its centre, (b) it is not a stroke-only line or polyline of 1 px or less on screen (a gridline), and (c) it is under 3:1 against every paint it touches: the paint under its centre and, when its box reaches past the box of the shape under it, the svg's background too (`against-outside`, with `ratio-outside`). One more rule came from the figures, as a measurement fix: a shape painted exactly as what is under it draws nothing and doesn't count (row 3's white patches behind the value labels, 1:1).

Self-test: the avatar frame's third avatar ("ZZ") sits beside only a visually hidden "Menu" and is under the floor. In the colours frame a red value bar is measured against the pink track under it, and a bullet bar gives: a track under a value bar that doesn't count (a), a 1 px gridline that doesn't count (b), a grey value bar under 3:1 on its track that counts and makes the fault, a tick under 3:1 on the bar that reaches the background and passes there (c), and a patch in the background colour that doesn't count; no drawing gives a mark. Each case was red with its code reverted (round 5 of the red-first log: 34 cases).

Compare against `daa56f6`: the pictures of all five sketches are pixel-equal (109); the JSON differs by Data row 3's changes and, beyond them, only by `marks`, the `marks` faults and row 4's new avatar line. Top-level faults: row 1 9, row 2 11, row 3 9, row 4 13, row 5 3 (before step 9: 9, 11, 8, 11, 3).

Marks under 3:1, per row (a tick that passes on the background is listed with it):

| Row | Frame or tile | Mark | Against | Ratio | Counts |
|---|---|---|---|---|---|
| 1 | none | no svg | | | |
| 2 | Loading tile | spinner track `circle.trk` `#CBE4E7` | `#FFFFFF` | 1.33 | no (the arc is drawn over it) |
| 3 | Overview, its phone layout, Error tile | gridlines `line` `#EBEBEB` (6 each) | `#FFFFFF` | 1.19 | no (1 px lines) |
| 3 | Selected tile | dimmed bars `#E09FA4` (3), `#C8C8C8` (3), `#D4D4D4` (4) | `#FFFFFF` | 2.18, 1.67, 1.48 | yes: G9's dimmed part, which the text exempts |
| 3 | Selected tile | highlighted part `#949494` (4) over its dimmed bar | `#D4D4D4` | 2.05 | yes (see below) |
| 3 | Selected tile | label patches `#FFFFFF` (6); one dimmed bar under a patch | `#FFFFFF` | 1.00; 2.18 | no |
| 3 | Loading tile | spinner track `#E6E6E6` | `#FFFFFF` | 1.25 | no |
| 4 | Home | grey value bar `rect.v` `#949494` (3) | track `#F0F0F0` | 2.66 | **yes: the `marks` fault** |
| 4 | Home | track `rect.tr` `#F0F0F0` (3) | `#FFFFFF` | 1.14 | no (the value bar is over it) |
| 5 | Home | target tick `line.tg` `#0E2A30` (2) | value bar `#0B6A75` | 2.39 | listed, passes: 15.08 on the background `#FFFFFF` |
| 5 | Home | track `rect.tr` `#E6E6E6` (3) | `#FFFFFF` | 1.25 | no |

Avatars: row 4 Your repairs (phone), "MS" (`span.av`, 12 px), is now under the site's 14 px floor. No other row changed.

So the `marks` fault fires on row 4 Home (the 2.66:1 value bar) and on row 3's Selected tile only. In that tile, besides the dimmed bars G9 exempts, the second chart draws each highlighted part (`#949494`) over its own dimmed bar (`#D4D4D4`, the same grey at 40%), inside its box, so it is read at 2.05:1 against the dimmed bar; G9 measures a highlighted mark against the svg's background, where `#949494` is 3.03:1. A judge applying G9 to the tile reads the highlighted part on the background; the text should say so if the figure is to decide it.

`ratio-background` (the orchestrator's decision: in a cross-highlighted visual the highlighted marks are read against the svg's background, G9): every `marks` entry also gives the mark's ratio against the svg's `background`. `counts` and the fault are unchanged; the judge applies G9's exemption and reading. On row 3's Selected tile the highlighted part `#949494` (2.05:1 against its dimmed bar) has `ratio-background` 3.03, so under G9 it meets 3:1. The field is on every entry, so row 4 Home's grey value bar shows 3.03 too; that visual is not cross-highlighted, so G9 doesn't apply and its counting 2.66:1 against the track stands. Self-test: the grey value bar on its track (1.27:1) gives `ratio-background` 1.47 against the card; red with its code reverted (round 6 of the red-first log: 35 cases). The compare was rerun: pictures pixel-equal (109), and the JSON differs from the run before only by the new field on each `marks` entry (top-level faults 9, 11, 9, 13, 3).

A bug found on the way: a line drawn before a filled shape gave the next shape a wrong paint under it (an array's own `fill` method was read as a fill). Fixed; it affected only `marks`, and only in the first listing above this one.

## Judge runs (step 8, round A at f1dbebc; step 9, round B at 82b1878)

### Round A

| Row | Run | judge | hash ok | measure.py sha256 (first 12) | Verdict | Failed gates | Scores 1-9 |
|---|---|---|---|---|---|---|---|
| 1 | 1 | fresh context | yes | e1a8add045d5 | not at baseline | 1, 2, 3, 4, 7 | 2 2 2 1 1 2 1 1 2 |
| 1 | 2 | fresh context | yes | e1a8add045d5 | not at baseline | 1, 2, 3, 4, 7 | 2 2 2 1 1 2 1 1 2 |
| 1 | 3 | fresh context | yes | e1a8add045d5 | not at baseline | 1, 2, 3, 4, 7 | 2 2 3 1 1 2 1 1 2 |
| 2 | 1 | fresh context | yes | e1a8add045d5 | not at baseline | 1, 4 | 2 2 3 2 2 2 1 3 3 |
| 2 | 2 | fresh context | yes | e1a8add045d5 | not at baseline | 1, 4 | 2 2 3 2 2 2 1 2 3 |
| 2 | 3 | fresh context | yes | e1a8add045d5 | not at baseline | 1, 4 | 2 2 3 2 2 2 2 2 3 |
| 3 | 1 | fresh context | yes | e1a8add045d5 | not at baseline | none | 3 2 3 2 2 3 2 3 3 |
| 3 | 2 | fresh context | yes | e1a8add045d5 | not at baseline | none | 3 2 2 2 2 3 3 3 3 |
| 3 | 3 | fresh context | yes | e1a8add045d5 | not at baseline | none | 3 2 2 2 1 3 2 3 3 |
| 4 | 1 | fresh context | yes | e1a8add045d5 | not at baseline | 4, 8 | 3 2 2 2 2 2 1 2 2 |
| 4 | 2 | fresh context | yes | e1a8add045d5 | not at baseline | 4, 6, 8 | 3 2 3 2 2 2 1 2 2 |
| 4 | 3 | fresh context | yes | e1a8add045d5 | not at baseline | 4, 6, 8 | 3 2 2 3 2 2 2 2 2 |
| 5 | 1 | fresh context | yes | e1a8add045d5 | not at baseline | 4 | 2 2 2 2 3 2 2 2 3 |
| 5 | 2 | fresh context | yes | e1a8add045d5 | not at baseline | 4 | 2 2 2 3 2 2 2 2 3 |
| 5 | 3 | fresh context | yes | e1a8add045d5 | not at baseline | 4 | 3 2 2 3 3 2 2 2 3 |

### Round B

| Row | Run | judge | hash ok | measure.py sha256 (first 12) | Verdict | Failed gates | Scores 1-9 |
|---|---|---|---|---|---|---|---|
| 1 | 1 | fresh context | yes | 52d0e222f64a | not at baseline | 1, 2, 3, 4, 7 | 2 2 3 1 1 2 1 1 1 |
| 1 | 2 | fresh context | yes | 52d0e222f64a | not at baseline | 1, 2, 3, 4, 7 | 2 2 2 1 1 2 1 1 2 |
| 1 | 3 | fresh context | yes | 52d0e222f64a | not at baseline | 1, 2, 3, 4, 7 | 2 2 2 1 1 2 1 1 1 |
| 2 | 1 | fresh context | yes | 52d0e222f64a | not at baseline | 1, 3, 4 | 2 2 3 3 2 2 2 2 3 |
| 2 | 2 | fresh context | yes | 52d0e222f64a | not at baseline | 1, 4 | 2 2 3 2 2 2 2 2 3 |
| 2 | 3 | fresh context | yes | 52d0e222f64a | not at baseline | 1, 4 | 2 2 3 2 2 2 1 2 3 |
| 3 | 1 | fresh context | yes | 52d0e222f64a | not at baseline | 7 | 3 2 2 1 2 2 2 3 3 |
| 3 | 2 | fresh context | yes | 52d0e222f64a | not at baseline | none | 3 2 3 2 1 3 2 3 3 |
| 3 | 3 | fresh context | yes | 52d0e222f64a | not at baseline | none | 3 2 2 2 2 3 2 3 3 |
| 4 | 1 | fresh context | yes | 52d0e222f64a | not at baseline | 4, 6, 8 | 3 2 2 2 2 2 2 2 2 |
| 4 | 2 | fresh context | yes | 52d0e222f64a | not at baseline | 4, 6, 8 | 3 2 3 3 2 2 2 2 2 |
| 4 | 3 | fresh context | yes | 52d0e222f64a | not at baseline | 4, 6, 8 | 3 2 2 3 2 2 1 2 2 |
| 5 | 1 | fresh context | yes | 52d0e222f64a | not at baseline | 4 | 2 2 2 3 2 2 2 2 3 |
| 5 | 2 | fresh context | yes | 52d0e222f64a | not at baseline | 4 | 3 2 2 2 2 2 2 2 3 |
| 5 | 3 | fresh context | yes | 52d0e222f64a | not at baseline | 4 | 3 2 3 2 2 2 2 2 3 |
