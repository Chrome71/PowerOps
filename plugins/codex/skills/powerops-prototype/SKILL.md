---
name: powerops-prototype
description: Use when powerops-plan starts it at approval for a plan whose Screens names anything, or when the person picks D (sketch the screens first). Writes three static HTML sketches in different design directions to docs/plans/<name>/mockups/, then the picked one as prototype.html, and checks them with powerops plan status. Never asks the user questions.
license: Apache-2.0
compatibility: Needs the powerops CLI on PATH. pac is optional.
---

# powerops-prototype

Draw the plan's screens as static HTML sketches the person can choose between before sign-off: three options in three design directions, then the picked one as the plan's prototype. You work alone: do not ask the user anything. Anything unclear becomes an assumption in your report. Write only the files section 2 names for your mode, in `docs/plans/<name>/mockups/`. Change no other file, and never publish or upload a sketch.

`powerops plan sign-off` refuses a plan whose `Screens` names something until `mockups/prototype.html` passes the rules below beside at least three options that pass them, or a person skips the sketch with a reason. A sketch proves intent, not the build.

## 1. Read

Read only:

- `docs/plans/<name>/plan.md`: `Goal`, `Users`, `Data`, `Constraints` and `Screens` (the route, each screen's task, form factor and four states, and the `### Theme` block).
- From the `references/` folder of the `powerops-plan` skill (in the repo's `skills/` folder, else beside the installed skill): `look.md` in full (it is the bar you are graded against), `design.md` (the Theme block, WCAG 2.2 AA, the states) and section 2 of `route.md` (which products get a sketch).
- The product file for each route: the "doesn't map" list and the look's mapping in `canvas.md` (custom pages and Teams apps too), `power-pages.md`, `power-bi.md`, `model-driven.md` and `code-apps.md`.
- The `Data` ledger's display names, and the security roles in the plan, for the "who sees this" notes.
- The mode and the notes in your request, and the sketches already in `mockups/`.

Read nothing from `decisions.md` answers, environment facts or the tenant, and run no `pac`. Without a Theme block, say so in the report and use neutral greys with one blue that passes contrast.

## 2. Modes

The request names the plan and one mode. With no mode, it is `options`.

- **`options`**: write `option-a.html`, `option-b.html` and `option-c.html`, replacing earlier ones. Write no `prototype.html`.
- **`pick <letter>`**, with the person's notes if any: write `prototype.html` from that option. Copy the option, apply the notes, and add every screen of `Screens` the option left out, in the option's direction. Leave the three options as they are: they are the record of what was not chosen.
- **`change <file>`**, with notes: edit that one file in place. Change what the notes ask and nothing else; don't regenerate the file.

**The screens.** Sketch each screen `Screens` names that the maker lays out freely: canvas and custom pages, Power Pages, generative pages, one Power BI page, and for a model-driven app its theme, forms (tabs and sections) and views (columns). A code app gets no sketch: its first slice is hand-written mock data. If that is the only route, write no file and report "code app: the person logs `prototype: skipped (mock-data slice)`". Flows, tables and agents have no screens.

- An option shows the screens that carry the most Goal lines, at most three, so the directions can be compared side by side on the same screens. `prototype.html` shows them all, up to eight distinct layouts; past eight, keep the ones that carry the most Goal lines and say which you left out.
- Each `data-screen` value is the screen's name as written in `Screens`, whole words, so the tool finds it there.

**The three directions.** Same plan, same Theme block, same screens, same fake data: only the design differs, so the person compares designs and not content. First write who uses each screen and its one job (`look.md`, section 1): these are the same in all three. Then choose three directions that answer the job differently, each with its own first look, and give each a short name and its bet: what it makes fast or clear, and what it gives up.

- Two directions differ when at least two of these differ: the layout pattern (landing, dashboard, list with detail, form, wizard), the navigation, the density, and what the first look is. A different colour, font, radius or wording alone isn't a direction.
- A useful spread: one that a maker of this product would expect; one that makes the most frequent task take the fewest steps; one that gives the person who supervises the best overview. Use another spread when the plan calls for it, and say why. A direction uses only the personas and screens the plan names: it invents no screen.
- Every direction must be buildable in the plan's route: only controls the product has (section 4), and a preview control only when the plan records that choice. In a model-driven app, metadata lays the screens out, so the directions differ in what the maker sets: what the app opens on (a view or a dashboard), each form's tabs, section order and header fields, and each view's columns and sort.
- Recommend one in your report, with the Goal lines and the persona it serves best. The person, or an agent their brief lets choose, picks.

## 3. File rules

Every sketch file obeys these. The tool checks them as text, after comments are removed. It never renders the file.

- **Size and shape.** At most 81,920 bytes; a screen with its four states takes about 8,000. Plain ASCII; write other characters as character references.
- **First HTML comment.** A line that starts with `route:` naming each product sketched (canvas, custom page, Bootstrap 3 or 5 site, code site, generative page, model-driven, Power BI). Then a line `direction: <name>: <the bet>`, one line per screen `screen: <name> | who: ... | job: ... | first look: ...`, and the colour pairs (section 4). For canvas, add the theme as the YAML that Paste theme takes; for a code site, name the UI library.
- **Label.** A banner at the top, as visible text and not a comment, that starts with the word "Sketch": "Sketch, option A of 3: <direction>" in an option, "Sketch, from option <letter>: <direction>" in `prototype.html`.
- **Screens.** Each screen is an element with `data-screen="<name>"`. Inside it, after the normal view, four blocks with `data-state="empty"`, `data-state="loading"`, `data-state="error"` and `data-state="failed-save"`. A state the plan says does not exist (a read-only report cannot fail to save) says "none" with the plan's reason; when the plan gives none, say so there and in your report.
- **Who sees this.** One line per screen: the roles or personas and why, from the plan. If the plan names none, say "all signed-in users; the plan names no roles".
- **Navigation.** Only `href="#..."` to an `id` in the file. No other link.
- **Banned.** Tags: `script`, `link`, `meta`, `base`, `iframe`, `frame`, `object`, `embed`, `img`, `picture`, `svg`, `form`, `video`, `audio`, `source`. Attributes: `src`, `srcset`, `action` and any `on...` event. CSS: `url(`, `@import`, `@font-face`, `image-set(`, `expression(`. So no `meta` charset or viewport, no `form` tag (draw fields with `label`, `input` and `div`), and no image: draw a logo as a labelled box, a chart in CSS, and an icon as a character reference beside its word.
- **Style.** Inline CSS in one `style` element that opens with the `:root` tokens, and system fonts only: the Theme font first, then `"Segoe UI", system-ui, sans-serif`. Flex and `gap`; no `position: absolute` or `fixed` and no fixed container widths (`max-width`, `flex-basis` and `position: sticky` are fine). The one `@` allowed in the file is a single `@media (min-width: 600px)` rule, so nothing is animated: a spinner and skeleton rows are drawn still.

## 4. Design

`look.md` is the bar: design from each screen's job line, and grade the result with its section 5. These points are specific to a sketch:

- **Colour.** Take the seed colour, the header pair and the font from the Theme block. Derive the brand hover and the neutrals from them, take the status pairs from `look.md`, and add no other hue. Calculate each text and background pair with WCAG's relative luminance, and list it in the first comment as `pair: <use> <text hex> on <background hex> = <ratio>:1`. For a model-driven app list the header pair at rest, hover, pressed and selected (4.5:1 each). If a Theme value fails, use the nearest passing one, mark the pair `adjusted`, and say so in your report: you do not edit the plan.
- **Structure.** One small `h1` for the file, one `h2` as each screen's page title, and `h3` for its states and for groups inside a view. For canvas, when the Theme gives a header pair, the header is a container in that pair, because the Header control takes no custom colours. Use `header`, `nav`, `main` and labelled `section` landmarks. DOM order is reading order, no `tabindex`. Show a visible `:focus-visible` outline of 3 px at 3:1. Every control has a visible label.
- **States.** Each block is drawn as the user would see it, not described, and says the one thing they do next. Empty: why it is empty and the action that fills it. Loading: a status line that says what is loading, and a way to cancel only where the load can be cancelled. Error: what happened, that what they entered is kept when they entered something, and a retry, or who to ask when the plan names someone. Failed save: that nothing was saved, that their entries are kept, and a button to try again or save as a draft.
- **Only what the product has.** For canvas, a screen that needs a control on the "doesn't map" list names its workaround beside it, or drops it. Custom pages and Teams apps use modern controls, not Fluent UI (v8). Power Pages: a small hand-written CSS subset with Bootstrap's class names, never the stylesheet itself. Power BI: one 1920 by 1080 page, at most 12 visuals, each drawn in CSS with its title and the fields it shows.
- **No brand or tenant.** A logo is a labelled box with a placeholder name, never a real brand.

## 5. Data

- Fake values under the ledger's display names, and nothing from answers, environment facts or the tenant, the same rows in all three options.
- Columns under column security or holding personal data (national IDs, phone numbers, dates of birth, home addresses, pay, health) show a mask such as `*****`, with a note in the screen's "who sees this" line. A column the plan puts on no screen isn't drawn.
- A fake person is a name such as "Sample Person A", never an email or a real or customer name.
- Write no `@` anywhere but the one media rule, no `http` (not even in text), no GUID, no `svg`. The tenant-data scan reads these files and flags them.
- Do not copy `<url>`, `<email>` or similar placeholders from the plan: write the words instead.

## 6. Look, grade and check

1. **Look at it.** If you have a tool that opens a local file and returns a screenshot (a browser pane, Playwright, a headless browser), open each file you wrote from disk at 360 px, 700 px and 1280 px wide and look at every screen and state. Fix what you see: overlaps, clipped or wrapped labels, uneven gaps, edges that line up with nothing, a first look that doesn't win. Open the local file only; never serve or upload it. Without such a tool, say "not rendered" in the report: don't claim a look you haven't had. If the Theme font isn't installed where you render, say that too.
2. **Grade it.** Score each file with section 5 of `look.md`, screen by screen, and check it against section 4 ("What fails"). Fix anything under 4 and score again. A file's score on a line is its lowest screen's. In `options` mode, also check that the three still differ as section 2 asks.
3. Count bytes with `wc -c`.
4. Run `powerops plan status docs/plans/<name>`. If `powerops` is not found, say "not checked" and why.
5. Fix every prototype warning that names a file you wrote and run it again, at most three runs. `plan status` says nothing about a sketch that passes: no warning that names the file means it passed. A warning that no `prototype:` line exists yet is expected: `powerops-plan` writes that line after the person answers. Do not write it.
6. If a warning remains that you cannot fix, report it. Never weaken a screen, state or pair to silence a check.

## 7. Report

Reply to the caller in a few lines per file: the path, the size in bytes, the direction's name and bet, the screens by name, the scores with the weakest point, and whether you rendered it. Then: your recommended option and why, each state left "none" and why, each colour pair you adjusted, the screens you left out, any assumptions, and the last `plan status` result. Say the next step: after `options`, the person's pick, three new options, or a skip; after `pick` or `change`, the person's approval or changes.

## Per agent

- **Claude Code:** the installed copy of this skill runs in a forked context on the top model.
- **Codex and other agents:** `powerops-plan` starts a subagent that runs this skill with the plan name and the mode. Without subagents, it follows these steps in its own session.
