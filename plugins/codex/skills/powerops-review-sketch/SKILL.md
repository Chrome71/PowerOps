---
name: powerops-review-sketch
description: Use to judge a PowerOps sketch in docs/plans/<name>/mockups/ against the visual baseline, in a fresh context. Renders it with a headless browser, scores it, checks the gates and writes mockups/<file>.judge.md with the verdict. Works alone and never asks the user questions.
license: Apache-2.0
compatibility: Needs Python 3 and Chrome or Chromium (Edge also works) to render the sketch. The powerops CLI is optional.
---

# powerops-review-sketch

You did not draw this sketch. Your job is to find what is still average or wrong in it, with evidence. Praise is not useful.

The input is the plan name and the sketch file, as `powerops-review-sketch <name> <file>`; the file is `prototype.html` unless the request names another one in `docs/plans/<name>/mockups/`. You work alone: do not ask the user anything. Anything unclear becomes a fault or a line under `not checked`. Write only `docs/plans/<name>/mockups/<file without .html>.judge.md`, for example `prototype.judge.md`, and pictures in a scratch folder outside the repo.

## 1. Read

Read only these:

- `baseline.md` in full, from the `references/` folder of the `powerops-plan` skill (in the repo's `skills/` folder, else beside the installed skill): the statements, section 5 "The scale" and section 6 "The gates". Judge by them; this skill does not restate them.
- The capability table of the product the sketch draws, where `baseline.md` points to it: `canvas.md` section 3, `power-pages.md` section 2 or `power-bi.md` section 3. A model-driven sketch has no table: judge it on the shared S statements.
- From `docs/plans/<name>/plan.md`: `Screens` (each screen and its job line), the Theme block if there is one, and `Assumptions` (a part the plan chose to draw though it is preview or not confirmed, for gate 3).
- The sketch and its map, `mockups/<file without .html>.map.md`. An option has no map: say so in the map check.
- The judge file if it exists, only for the number of its last round.

Read nothing from the author's report or the planning chat. If either is in your context, the header line is `judge: self-judged` (section 4).

## 2. Render and look

You judge from pictures, not from the source. `measure.py` is in this skill's folder (in the repo's `skills/powerops-review-sketch/`, else beside the installed `SKILL.md`). Start it with `python3`, else `py -3`, else `python` after `python --version` shows 3. Put the pictures in a scratch folder outside the repo: `measure.py` refuses a folder under `docs/plans/`.

1. `measure.py measure <sketch>` prints JSON: the `sketch-hash` for your header, the page height, and per frame (each `data-screen` element) its box and class, text that overflows its box (`"ellipsis": true` when the clipping box has `text-overflow: ellipsis`), pairs of text boxes that overlap, text under the product's floor (the initials of an avatar, class `av` or `avatar`, are skipped only when the avatar's parent holds drawn text outside it, the name beside it; a visually hidden label is not drawn text), the contrast of each text colour on its background (`unknown` over an image or gradient; a disabled control's text is skipped), each font asked for and the one that rendered, the share of the content area that is empty, the largest text and whether it is inside the `data-lead` element (`lead: not marked` when the frame has none), and the distinct font sizes. It also gives per frame its `title` (its first `h1`, else its first `h2`, else its first `[role=heading]`, or null), its `top-text` (the five largest texts outside nested tiles in the frame's first screen: the top 768 px of a desktop or browser frame, the whole of a phone or report frame; each with `size`, `weight`, `text`, `element` and the flags `title`, `chrome` and `inLead`), its `size` (`ok`, `no size class`, or the reason), and its `colours`: each non-neutral colour with its `hex`, its hue `group` and its `uses` (`role` text, fill, stroke or svg, `element`, `count`; an svg use adds `svg` icon, chart or imagery, its box and the `background` under it). Each frame's `faults` lists what crossed a line; the `sizes` fault names its statement (S4, P4 or B8), and an ellipsis is counted in the `ellipsis` fault, not in `overflow`. The top-level `states` list holds every `[data-state]` tile: its box, `data-state`, `data-for` (or null), whether it is inside a frame, and for a tile outside every frame the same overflow, overlap, floor, contrast, font and `colours` fields and its faults. The top-level `faults` counts the frames' and the tiles' faults. A tile's own first `header` is its caption, not the product, and is not measured; in a tile, text hidden under an opaque box drawn above it is no overlap. `colours` leaves out the fill and border of the frame or tile itself and blurred shadows. Per frame and tile, `marks` lists each shape of an icon or chart svg, neutral colours included: its `element`, its `paint`, the paint it is measured against (`against`), the `ratio`, and `ratio-background`, its ratio against the svg's background. An entry counts (`counts: true`) only when it is topmost, with nothing drawn over its centre, is not a stroke line of 1 px or less, and draws something other than what is under it. A mark whose box runs past the shape under it is also measured against the svg's background (`against-outside`, `ratio-outside`), and passes on either. The `marks` fault lists the counting entries under 3:1.
2. `measure.py shoot <sketch> <scratch folder>` writes a full-page picture at 1:1 (1440 wide, or 1968 for a file with a Power BI `report` frame), then one picture per frame and per state tile, cut into tiles of at most 1400 by 1200. It lists every file.
3. Open every frame and state tile picture, and the full page once. Look at each frame for what the baseline asks: the lead, the type, colour and meaning, alignment and gaps, the content and its numbers, the states, the product's frame and chrome, the finish, the identity.
4. The figures decide gate 1's sizes and weights, gate 6's 3:1 for marks and gate 7's size and empty share: where you think such a figure is a false alarm, write a fault line, and the gate still follows the figure. Confirm each other measured fault in the picture, or say why it is a false alarm. Numbers come from the CSS or from measured pixels; contrast from the hex values. Where the JSON maps to a gate: overflow, overlaps, text under the floor, contrast and the `marks` fault are gate 6, in a frame or a tile; an ellipsis is never gate 6 (section 3 says where it goes); an empty share over 0.15 or a `size` other than `ok` (`no size class` included) is gate 7; `top-text` is gate 1, by section 3. The `sizes` fault is a `not met` of the statement it names. A font that fell back changes widths and wrapping: say which font rendered, and judge wrapping knowing it.

`measure.py` never changes the sketch. It renders a copy in a temp folder, which it removes, with Chrome's background networking, component updates and sync off, a fresh profile, and a host-resolver rule that resolves no host name. It serves the copy to Chrome from a loopback server that answers only that page, sends all of Chrome's requests through that server, and allows no script but its own and no image or font but inline `data:`, so the picture shows no other file. It does not check what Chrome itself sends.

**No render, no pass.** If no Python 3 is found, `measure.py` exits 77 (no Chrome, or Chrome can't render), or it fails on the sketch, do not judge from the source. Write the round with `verdict: not at baseline` and, under `not checked`, `not rendered: <the message>`. Give no scores.

## 3. Judge

Check the sketch against every statement of `baseline.md` that applies to its product: S1 to S16, then the product's own (C, P or B). Score the nine dimensions of "The scale" and decide each of the eight gates of "The gates". Gate 3 reads the map against the product's table and the plan's `Assumptions`. Gate 8 applies to a Power Pages site only. The verdict is `at baseline` only when "The scale" says so; otherwise it is `not at baseline`.

Each frame is judged on the product its screen has in the plan's `Screens` line, or, where the plan doesn't say, on the first comment's `route:` line.

**Evidence.** A gate fails only on a named element and a value it has, or a rule of this section or of `baseline.md` that it breaks. "Narrow", "close" or "reads as" is a fault line, not a gate fail.

**Statements and scores.** A statement is `met`, `not met` or `not applicable`, by "The scale": `not met` names the element, in a main frame or a state tile, and the value or the missing part. Score each dimension from its statements in the map of "The scale" alone: under 3 only for a `not met`, 2 for one or two, 1 for three or more, else 3, or 4 when you can name no improvement. Identity follows its three checks there.

**Gate 1.** Find the element the job line names as the first look. If the frame doesn't show it, or the line names a state (selected, focused) that isn't drawn on it, gate 1 fails. A text inside a row, a field or a control names that row, field or control. When the first look is a row, a field or a control: with a named state, it passes when the element carries the frame's only focus ring or its only selected-row style; with only a position ("the first field"), it passes when the element carries the frame's only focus ring, and else the size test applies to its text. A frame with two focus rings or two selected rows fails gate 1. A selected-row style is the fill, bar or check that marks one row of a list, table or gallery as the selected one; it is the frame's only one when no other row of the frame is drawn selected. A selected tab, pivot, segment or navigation item is navigation, not a row: it neither counts as the selected row nor competes with it. Otherwise use the size test on `top-text`, which lists only the frame's first screen, so a text below it is no rival: leave out the title (unless the first look is the title), every entry with `chrome: true` and one docked phone action; gate 1 fails when a text left is larger than the first look's largest text, or has its size and a greater weight. On equal size and weight the first in reading order (top, then left) leads, and the gate fails only when the rival comes first. Two texts whose boxes share at least half the lower one's height are on one line and read left to right. Colour and fills don't decide gate 1; a filled button outside the first look is gate 5's and S9's.

**Gate 3.** With no map, read each part a main frame draws against the product's table: a part the table marks preview, not native or not confirmed fails gate 3 unless the plan's `Assumptions` records it. A part the table doesn't name is a map-check line, not a gate 3 fail. A model-driven frame has no table: gate 3 fails it only on a part the plan marks not confirmed. Gate 3 covers main frames only; a part only a state tile draws is a map-check line. An ellipsis in a canvas main frame fails gate 3, because `canvas.md` C5 says an ellipsis on overflow is not confirmed: that line is the evidence, and a map that doesn't list the ellipsis doesn't pass it. In a state tile an ellipsis is a map-check line; outside canvas it is an S8 line. With a map, a part the map marks not confirmed, not native or preview passes gate 3 when the map names a fallback that draws the same picture (for example a PNG for an SVG logo): write it as a map-check line. A fallback that would look different still fails gate 3, unless the plan's `Assumptions` record the choice.

**Gate 4.** Use each frame's and tile's `colours`; `measure.py` already left out the neutrals. An svg whose box is at most 24 by 24 px is an `icon`; else, in a `report` frame or tile, it is a `chart` unless it is inside the `header` (the mark); elsewhere it is a `chart` when it holds text or is drawn only of rectangles and lines (a bar, a bullet or a meter); else an svg drawn in one colour whose longer side is at most 48 px is an `icon` (S7 judges its size), and any other is `imagery`. A report's phone layout and its state tiles count as `report` here, and a report's mark outside a `header` is classed a chart. Icon and chart uses count; imagery uses don't. Check each svg's class against the picture, and name a wrong one in a fault line. Leave out the fills of the page and of each frame, which are surfaces. Give each use its meaning from the set in gate 4 of `baseline.md`, from the word, sign or icon beside it, else the control it is on (a filled button: action; a selected row: selection; link text: link; a focus ring: focus). Gate 4 fails when one `group` carries two meanings, or one meaning two groups: name each element and hex.

**Gate 6.** In a `report` frame or tile, a dimmed bar or column that gate 6 of `baseline.md` exempts is not a fault. The 3:1 for marks follows the counting entries of `marks`, not a sample of the picture's pixels: a counting entry under 3:1 fails gate 6. In a cross-highlighted visual, the dimmed entries are exempt, and the highlighted marks are read by their `ratio-background`, not their `ratio`, because a highlighted part drawn inside its own dimmed bar is otherwise measured against that bar. Text keeps 4.5:1. `marks` lists neutral marks too, so a grey mark's ratio comes from its entry, not from the picture.

**Gate 7.** It follows the figures: `size` and the empty share decide it, as section 2 says. A model-driven frame is checked at 1366 by 768. On chrome, gate 7 fails only when the frame lacks its product's frame element: canvas, the header with the app name (C2); model-driven, the header and the site map (S14); a site, the header and the footer (P3); a report page, the title row (B1); a report's phone layout, its title. A missing navigation item, title-row action or page navigator is an S14, C2, P3 or B1 line under the scores, not gate 7.

## 4. Write the judge file

The file opens with two lines:

```
judge: fresh context
sketch-hash: <the sketch-hash measure.py printed>
```

Write `judge: fresh context` only if this context holds neither the planning chat nor the author's work on the sketch. Otherwise write `judge: self-judged`. Be honest: if unsure, write `self-judged`.

Each run adds one section, `## Round <n>`, after the last one; the first round is 1. Never change an earlier round. The file's two opening lines are round 1's, so round 1 doesn't repeat them under its heading. A later round starts with its own `judge:` and `sketch-hash:` lines under its heading, because `powerops plan status` reads the last of each and the sketch changed between rounds. Then, in this order:

1. **Baseline table.** One row per statement that applies: `| ID | met, not met or not applicable | evidence: the frame and the element | fix |`. Skip none; give the reason for `not applicable`.
2. **Scores.** The nine dimensions, 0 to 4, one line each, naming a frame and an element, and for a score under 3 each statement `not met`.
3. **Gates.** The eight gates, each `pass` or `fail` with its evidence. Outside a Power Pages site, write gate 8 as `not applicable (<product>)`; it fails nothing. Only gate 8 may be not applicable.
4. **Faults.** Ranked, most harmful first, at most 15. Each names the frame, the element and the fix.
5. **Map check.** Each element drawn and not in the map, each status that contradicts the product's table, and each preview or unconfirmed part with no fallback.
6. **Verdict.** One line, exactly `verdict: at baseline` or `verdict: not at baseline`, then `### Not checked`: what you could not check and why.

`plan status` reads the last `verdict:` line, so write no other line that starts with `verdict:`. The verdict counts only for the file whose `sketch-hash` it names: any edit to the sketch needs a new round.

## 5. Finish

Reply to the caller in a few lines: the judge file's path, the round, the nine scores on one line, each gate's result, the three weakest points, the verdict, and what was not checked. The author fixes the sketch; you don't.

## Never

- Never edit the sketch, the plan or the map. Write only the judge file.
- Never reach a tenant, and never use the web: everything you need is in the files above.
- Never write `http`, a GUID or an email address in the judge file. Name a Learn page by its path, as `learn.microsoft.com/power-apps/...`.
- Never pass a sketch you have not seen rendered.

## Per agent

- **Claude Code:** the installed copy of this skill runs in a forked context on the top model, so the header is `judge: fresh context`.
- **Codex and other agents:** a subagent with no chat history gives `judge: fresh context`. Without subagents, the session that drew the sketch follows these steps itself and writes `judge: self-judged`, says "self-judged" in its report, and only a person may approve that sketch.
