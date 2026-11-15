---
name: powerops-prototype
description: Use when powerops-plan starts it for a plan whose Screens names anything (in the question rounds or at approval), or when the person picks D (sketch the screens first). Writes three one-frame static HTML options in different design directions to docs/plans/<name>/mockups/, then the picked one as prototype.html, renders it, has it judged by powerops-review-sketch and fixes it, and checks them with powerops plan status. Never asks the user questions.
license: Apache-2.0
compatibility: Needs the powerops CLI on PATH, and Python 3 and Chrome or Chromium to render. pac is optional.
---

# powerops-prototype

Draw the plan's screens as static HTML sketches the person can choose between before sign-off: three options in three design directions, then the picked one as the plan's prototype, rendered, judged and fixed. You work alone: do not ask the user anything. Anything unclear becomes an assumption in your report. Write only the files section 2 names for your mode, in `docs/plans/<name>/mockups/`, and your pictures in a folder outside `docs/plans/`. Change no other file, never edit the judge's file, and never publish or upload a sketch.

`powerops plan sign-off` refuses a plan whose `Screens` names something until `mockups/prototype.html` passes the rules below, with its map and a current judge verdict, beside at least three options that pass them, or a person skips the sketch with a reason. An approved plan whose sketches predate these rules needs a new `options` run, or a person's `prototype: skipped (sketched before the baseline)`. A sketch proves intent, not the build.

## 1. Read

Read only:

- `docs/plans/<name>/plan.md`: `Goal`, `Users`, `Data`, `Constraints` and `Screens` (the route, each screen's task, form factor and four states, and the `### Theme` block).
- From the `references/` folder of the `powerops-plan` skill (in the repo's `skills/` folder, else beside the installed skill): `baseline.md` in full (the statements, the scale and the gates your sketch is judged against), `look.md` in full (how to design from the job), `design.md` (the Theme block, WCAG 2.2 AA, the states) and section 2 of `route.md` (which products get a sketch).
- The product file for each route, with its capability table: the "doesn't map" list and the look's mapping in `canvas.md` (custom pages and Teams apps too), `power-pages.md`, `power-bi.md`, `model-driven.md` and `code-apps.md`.
- The sample for each product you sketch that has one, in `references/samples/`: `canvas-repairs-desk.html`, `pages-tenant-repairs.html` or `powerbi-repairs-performance.html`, with its `.map.md`. Render it (section 6, step 1) and look at it before you draw: it is the level, not a template: copy its discipline, never its layout, content or brand. `samples/NOTICE.md` says what the samples lack, because they predate section 3: follow section 3, not them.
- The `Data` ledger's display names, and the security roles in the plan, for the map's "Who sees what".
- The mode and the notes in your request, and the sketches already in `mockups/`.

Read nothing from `decisions.md` answers, environment facts or the tenant, and run no `pac`. Without a Theme block (an early options run), say so in the report and use neutral greys with one blue that passes contrast; `pick` redraws with the Theme once the plan has one.

## 2. Modes

The request names the plan and one mode. With no mode, it is `options`.

- **`options`**: write `option-a.html`, `option-b.html` and `option-c.html`, replacing earlier ones, then render the three lead frames side by side at one scale into one picture, `measure.py side-by-side <a.html> <b.html> <c.html> <out.png>` (section 6), which is what the person picks from. Write no `prototype.html`.
- **`pick <letter>`**, with the person's notes if any: write `prototype.html` from that option. Start from the option's frame, apply the notes, and add every screen of `Screens`, the states and `prototype.map.md`, in the option's direction. Then run section 6 on it. Leave the three options as they are: they are the record of what was not chosen.
- **`change <file>`**, with notes or a judge file: edit that one file in place. Change what the notes or the judge's findings ask and nothing else; don't regenerate the file. A changed `prototype.html` goes through section 6 again from step 1.

**The screens.** Sketch each screen `Screens` names that the maker lays out freely: canvas and custom pages, Power Pages, generative pages, one Power BI page, and for a model-driven app its theme, forms (tabs and sections) and views (columns). A code app gets no sketch: its first slice is hand-written mock data. If that is the only route, write no file and report "code app: the person logs `prototype: skipped (mock-data slice)`". Flows, tables and agents have no screens.

- An option holds one frame: the screen that carries the most Goal lines, at the product's size, plus the direction's name and its bet. No states, no second screen, no map. Only the picked direction gets every screen, the states, the map and the judge. `prototype.html` shows every screen, up to eight distinct names; past eight, keep the ones that carry the most Goal lines and say which you left out.
- Each `data-screen` value is the screen's name as written in `Screens`, whole words, so the tool finds it there.

**The three directions.** Same plan, same Theme block, same lead screen, same fake data: only the design differs, so the person compares designs and not content. First write who uses the screen and its one job (`look.md`, section 1): these are the same in all three. Then choose three directions that answer the job differently, each with its own first look, and give each a short name and its bet: what it makes fast or clear, and what it gives up.

- Two directions differ when at least two of these differ: the layout pattern (landing, dashboard, list with detail, form, wizard), the navigation, the density, and what the first look is. A different colour, font, radius or wording alone isn't a direction.
- A useful spread: one that a maker of this product would expect; one that makes the most frequent task take the fewest steps; one that gives the person who supervises the best overview. Use another spread when the plan calls for it, and say why. A direction uses only the personas and screens the plan names: it invents no screen.
- Every direction must be buildable in the plan's route: only controls the product has (section 4), and a preview control only when the plan records that choice. In a model-driven app, metadata lays the screens out, so the directions differ in what the maker sets: what the app opens on (a view or a dashboard), each form's tabs, section order and header fields, and each view's columns and sort.
- Recommend one in your report, with the Goal lines and the persona it serves best. The person, or an agent their brief lets choose, picks.

## 3. File rules

Every sketch file obeys these. `powerops plan status` checks them as text, after comments are removed. It never renders the file; the judge does.

- **Size.** `prototype.html` at most 163,840 bytes; an option at most 61,440 bytes. Plain ASCII; write other characters as character references.
- **First HTML comment.** A line that starts with `route:` naming each product sketched (canvas, custom page, Bootstrap 3 or 5 site, code site, generative page, model-driven, Power BI). Then a line `direction: <name>: <the bet>`, one line per screen `screen: <name> | who: ... | job: ... | first look: ...`, and the colour pairs (section 4). For canvas, add the theme as the YAML that Paste theme takes; for a code site, name the UI library.
- **Label.** A banner at the top, as visible text and not a comment, that starts with the word "Sketch": "Sketch, option A of 3: <direction>" in an option, "Sketch, from option <letter>: <direction>" in `prototype.html`.
- **Frames.** Each screen is an element with `data-screen="<name>"` in a frame at the product's size: the `data-screen` element, or its parent, has the class `frame` and one of `desktop`, `phone`, `browser` or `report` (the kit's frame classes). A screen name may repeat across frames, as a desktop and a phone frame of one screen; a file holds 1 to 8 distinct names. Each frame holds exactly one element marked `data-lead`, its first look, and at most one element with the classes `btn primary`, the filled button. Inside a frame, `position: absolute` and fixed sizes are fine.
- **States.** Each state is a tile in a states gallery, drawn as a crop of the region that changes, marked `data-state="empty"`, `data-state="loading"`, `data-state="error"` or `data-state="failed-save"`, and `data-for="<screen name>"` naming a `data-screen` value in the file. In `prototype.html` each distinct screen has all four. A state the plan says does not exist (a read-only report cannot fail to save) still has its tile, which says "none" with the plan's reason; when the plan gives none, say so there and in your report. Extra states (confirm, success) are fine.
- **Options.** An option holds exactly one `data-screen` and no `data-state`.
- **Map.** Beside `prototype.html`, `mockups/prototype.map.md`, not empty, naming every `data-screen` value. It holds, per element, the control or component, its status and its Learn page by path, as `learn.microsoft.com/power-apps/...`, never with `http`; the GA fallback of each preview or unconfirmed part; the Theme; "Left out because not confirmed"; and "Who sees what" (S16): per screen the roles or personas and why, which rows, and which sensitive data it shows, from the plan. If the plan names no roles, say "all signed-in users; the plan names no roles".
- **Judge file.** `mockups/prototype.judge.md` is written by `powerops-review-sketch` only (section 6): a `judge:` line, `fresh context` or `self-judged`; a `sketch-hash:` line, the SHA-256 of `prototype.html`'s text with CRLF made LF; a `## Round <n>` section per round; and a last line `verdict: at baseline` or `verdict: not at baseline`. A missing or odd file, or a hash that doesn't match, is no current verdict. `plan sign-off` refuses it, refuses `not at baseline` unless the last `prototype:` line holds `(below baseline: <why>)` and ends `(answered by: person)`, and refuses `self-judged` unless the approval line ends `(answered by: person)`.
- **Navigation.** Only `href="#..."` to an `id` in the file. No other link.
- **SVG.** Inline `<svg>` is fine for the icon sprite, charts and illustration. Inside an `svg`, never `script`, `foreignObject`, `image`, `a`, `style`, `animate`, `set`, `animateMotion` or `animateTransform`. `href` and `xlink:href` go only to `#...`. No attribute anywhere in the file holds `url(`. Write no `xmlns` attribute: inline SVG in HTML needs none, and the scan flags its `http`.
- **Banned.** Tags: `script`, `link`, `meta`, `base`, `iframe`, `frame` (the tag; the class is the frame rule), `object`, `embed`, `img`, `picture`, `form`, `video`, `audio`, `source`. Put `<meta name="viewport" content="width=device-width, initial-scale=1">` in each sketch's `head`, so a phone shows the narrow layout; it is the one `meta` allowed. Attributes: `src`, `srcset`, `action` and any `on...` event. CSS: `url(`, `@import`, `@font-face`, `image-set(`, `expression(`. So no `meta` charset, no `form` tag (draw fields with `label`, `input` and `div`), and no image file: draw a logo, an icon, a chart or a picture as inline SVG.
- **Style.** Inline CSS in one `style` element that opens with the `:root` tokens. No hex colour in a CSS declaration outside `:root`, in the `style` element or in a `style` attribute: use the tokens. An SVG presentation attribute (`fill`, `stroke`, `stop-color`) may hold hex. Sizes are in rem with 1rem = 10 design px, as in the kit, so no media rule is needed. Nothing is animated (section 5): a spinner and skeleton rows are drawn still.

## 4. Design

`baseline.md` is the level and `look.md` the way to it: design from each screen's job line. The judge grades the result, not you (section 6). These points are specific to a sketch:

- **Start from the kit.** Start each file from `references/samples/kit.css`, pasted at the top of the one `style` element; the plan's Theme block replaces the kit's brand tokens.
- **Colour.** Take the seed colour, the header pair and the font from the Theme block. Derive the brand hover and the neutrals from them, and take the status pairs from `look.md`. Calculate each text and background pair with WCAG's relative luminance, and list it in the first comment as `pair: <use> <text hex> on <background hex> = <ratio>:1`. For a model-driven app list the header pair at rest, hover, pressed and selected (4.5:1 each). If a Theme value fails, use the nearest passing one, mark the pair `adjusted`, and say so in your report: you do not edit the plan.
- **Fonts.** The Theme font, then `"Segoe UI"`, then a named fallback the session has. Your report says which font rendered.
- **Structure.** One small `h1` for the file, one `h2` as each screen's page title, and `h3` for its states and for groups inside a view. For canvas, when the Theme gives a header pair, the header is a container in that pair, because the Header control takes no custom colours. Use `header`, `nav`, `main` and labelled `section` landmarks. DOM order is reading order, no `tabindex`. Keep the kit's visible `:focus-visible` outline at 3:1. Every control has a visible label.
- **States.** Each tile is drawn as the user would see it, not described, and says the one thing they do next. Empty: why it is empty and the action that fills it. Loading: a status line that says what is loading, and a way to cancel only where the load can be cancelled. Error: what happened, that what they entered is kept when they entered something, and a retry, or who to ask when the plan names someone. Failed save: that nothing was saved, that their entries are kept, and a button to try again or save as a draft.
- **Only what the product has.** A main frame draws only what the product file confirms, or a preview part the plan records, with its GA fallback in the map (gate 3). For canvas, a screen that needs a control on the "doesn't map" list names its workaround beside it, or drops it. Custom pages and Teams apps use modern controls, not Fluent UI (v8). Power Pages: a small hand-written CSS subset with Bootstrap's class names, never the stylesheet itself. Power BI: one 1920 by 1080 page, at most 12 visuals, each with its title and the fields it shows.
- **No brand or tenant.** A logo may be a simple made-up mark in inline SVG with a placeholder name, never a real brand. Imagery is drawn as inline SVG; the Theme block names where the final art comes from.

## 5. Data

- Fake values under the ledger's display names, and nothing from answers, environment facts or the tenant, the same rows in all three options.
- Columns under column security or holding personal data (national IDs, phone numbers, dates of birth, home addresses, pay, health) show a mask such as `*****`, with a note in the map's "Who sees what". A column the plan puts on no screen isn't drawn.
- A fake person is a name such as "Sample Person A", never an email or a real or customer name.
- Write no `@` in text; the only `@` rule allowed is `@media`, and nothing is animated: no `@keyframes`, no animation. Write no `http` (not even in text) and no GUID. The tenant-data scan reads every file in the plan folder and flags them.
- Do not copy `<url>`, `<email>` or similar placeholders from the plan: write the words instead.

## 6. Render, judge and check

`measure.py` is in the `powerops-review-sketch` skill folder (in the repo's `skills/` folder, else beside the installed skill). Start it with `python3`, else `py -3`, else `python` after `python --version` shows 3. Put every picture in a folder outside `docs/plans/`: the plan-folder scan warns on a binary file there.

1. **Render it.** Open each file you wrote from disk in a headless browser at 1:1 (canvas and Power Pages 1440 wide, Power BI 1968 wide), with `measure.py shoot <sketch> <out-folder> --width <N>` or any tool that gives the whole page. Open the local file only; never serve or upload it. Look at every frame, state tile and phone frame yourself and fix what you see: clipped or overlapping text, a label that wraps badly, dead space, edges that line up with nothing, a lead that is not the largest thing. Do this at least three times. In `options` mode, check that the three still differ as section 2 asks, then write the side-by-side picture. Without a tool that renders, stop and report "not rendered: no sketch can be approved from this session"; do not hand over an unseen sketch.
2. **Have it judged.** Only `prototype.html` is judged, not the options. Start `powerops-review-sketch <name> prototype.html` in a fresh context. It renders the file again, checks it against `baseline.md`, scores it and writes `mockups/prototype.judge.md`. You never judge your own file, outside the self-judged case (Per agent), and never edit the judge's file. If you can't start a fresh context, end your report with `judge needed: mockups/prototype.html`: `powerops-plan` starts the judge and passes its file back to you with `change prototype.html` for the next round.
3. **Fix and repeat.** Fix every gate the judge's last round failed and every dimension under 3, render and look again, then have it judged again. At most three rounds. If the verdict is still `not at baseline` after the third, report the sketch as "not at the baseline" with the judge's open findings; do not present it as ready.
4. Count bytes with `wc -c`.
5. Run `powerops plan status docs/plans/<name>`. If `powerops` is not found, say "not checked" and why.
6. Fix every prototype warning that names a file you wrote and run it again, at most three runs. `plan status` says nothing about a sketch that passes: no warning that names the file means it passed. A warning that no `prototype:` line exists yet is expected: `powerops-plan` writes that line after the person answers. Do not write it. Any edit to `prototype.html` after the last judge round, a fix here included, needs a new round (step 2), because the verdict is bound to the file's hash.
7. If a warning remains that you cannot fix, report it. Never weaken a screen, state or pair to silence a check.

## 7. Report

Reply to the caller in a few lines per file: the path, the size in bytes, the direction's name and bet, the screens by name, the judge's scores per round, each gate's result, the three weakest points the judge still names, and the path of each picture (after `options`, the side-by-side picture). Then: which font rendered, your recommended option and why, each state left "none" and why, each colour pair you adjusted, the screens you left out, any assumptions, and the last `plan status` result. Say the end state when there is one: "not rendered", "not at the baseline", "judge needed: <file>" or "self-judged". Say the next step: after `options`, the person's pick, three new options, or a skip; after `pick` or `change`, the person's pass or fail of the judged sketch.

## Per agent

- **Claude Code:** the installed copy of this skill runs in a forked context on the top model. Start `powerops-review-sketch`, which runs forked too; where that doesn't start, end with "judge needed" (section 6, step 2).
- **Codex and other agents:** `powerops-plan` starts a subagent that runs this skill with the plan name and the mode, and the judge runs in another subagent with no chat history. Without subagents, it follows these steps in its own session, and the judge's file says `judge: self-judged`: say "self-judged" in the report, because only the person may approve that sketch.
