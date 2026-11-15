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

1. `measure.py measure <sketch>` prints JSON: the `sketch-hash` for your header, the page height, and per frame (each `data-screen` element) its box and class, text that overflows its box, pairs of text boxes that overlap, text under the product's floor (avatar initials, class `av` or `avatar`, are skipped), the contrast of each text colour on its background (`unknown` over an image or gradient; a disabled control's text is skipped), each font asked for and the one that rendered, the share of the content area that is empty (an estimate), the largest text and whether it is inside the `data-lead` element (`lead: not marked` when the frame has none), and the distinct font sizes. Each frame's `faults` lists what crossed a line.
2. `measure.py shoot <sketch> <scratch folder>` writes a full-page picture at 1:1 (1440 wide, or 1968 for a file with a Power BI `report` frame), then one picture per frame and per state tile, cut into tiles of at most 1400 by 1200. It lists every file.
3. Open every frame and state tile picture, and the full page once. Look at each frame for what the baseline asks: the lead, the type, colour and meaning, alignment and gaps, the content and its numbers, the states, the product's frame and chrome, the finish, the identity.
4. Treat each measured fault as a lead, not a verdict: confirm it in the picture, or say why it is a false alarm. Numbers come from the CSS or from measured pixels; contrast from the hex values. Where the JSON maps to a gate: overflow, overlaps, text under the floor and contrast are gate 6; an empty share over 0.15, a frame whose class has no size class, or a box not at the product's size is gate 7; the lead is gate 1, and with `lead: not marked` you decide gate 1 by looking. Gate 1 covers the title first, so a largest text that is the frame's title is not a gate 1 fault on its own. More than five font sizes in a frame is S4. A font that fell back changes widths and wrapping: say which font rendered, and judge wrapping knowing it.

`measure.py` never changes the sketch. It renders a copy in a temp folder, which it removes, with Chrome's background networking, component updates and sync off, a fresh profile, and a host-resolver rule that resolves no host name. It serves the copy to Chrome from a loopback server that answers only that page, sends all of Chrome's requests through that server, and allows no script but its own and no image or font but inline `data:`, so the picture shows no other file. It does not check what Chrome itself sends.

**No render, no pass.** If no Python 3 is found, `measure.py` exits 77 (no Chrome, or Chrome can't render), or it fails on the sketch, do not judge from the source. Write the round with `verdict: not at baseline` and, under `not checked`, `not rendered: <the message>`. Give no scores.

## 3. Judge

Check the sketch against every statement of `baseline.md` that applies to its product: S1 to S16, then the product's own (C, P or B). Score the nine dimensions of "The scale" and decide each of the eight gates of "The gates". Gate 3 reads the map against the product's table and the plan's `Assumptions`. Gate 8 applies to a Power Pages site only. The verdict is `at baseline` only when "The scale" says so; otherwise it is `not at baseline`.

## 4. Write the judge file

The file opens with two lines:

```
judge: fresh context
sketch-hash: <the sketch-hash measure.py printed>
```

Write `judge: fresh context` only if this context holds neither the planning chat nor the author's work on the sketch. Otherwise write `judge: self-judged`. Be honest: if unsure, write `self-judged`.

Each run adds one section, `## Round <n>`, after the last one; the first round is 1. Never change an earlier round. A later round starts with its own `judge:` and `sketch-hash:` lines, because `powerops plan status` reads the last of each and the sketch changed between rounds. Then, in this order:

1. **Baseline table.** One row per statement that applies: `| ID | met, partly, not met or not applicable | evidence: the frame and the element | fix |`. Skip none; give the reason for `not applicable`.
2. **Scores.** The nine dimensions, 0 to 4, one line each, naming a frame and an element.
3. **Gates.** The eight gates, each `pass` or `fail` with its evidence.
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
