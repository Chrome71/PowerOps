---
name: powerops-prototype
description: Use when powerops-plan starts it at approval for a plan whose Screens names anything, or when the person picks D (sketch the screens first). Writes one static HTML sketch to docs/plans/<name>/mockups/prototype.html and checks it with powerops plan status. Never asks the user questions.
license: Apache-2.0
compatibility: Needs the powerops CLI on PATH. pac is optional.
---

# powerops-prototype

Draw the plan's screens as one static HTML sketch the person can approve before sign-off. The input is the plan name, and sometimes the person's notes on an earlier sketch. You work alone: do not ask the user anything. Anything unclear becomes an assumption in your report. Write only `docs/plans/<name>/mockups/prototype.html`, replacing any earlier one. Change no other file, and never publish or upload it.

`powerops plan sign-off` refuses a plan whose `Screens` names something until this file passes the rules below, or a person skips the sketch with a reason. A sketch proves intent, not the build.

## 1. Read

Read only:

- `docs/plans/<name>/plan.md`: `Screens` (the route, each screen's task, form factor and four states, and the `### Theme` block), `Users`, `Data` and `Constraints`.
- `skills/powerops-plan/references/design.md` (the Theme block, WCAG 2.2 AA, the states of a screen) and `route.md` section 2 (which products get a sketch), from the `references/` folder of the installed `powerops-plan` skill when the repo has no `skills/` folder.
- The product file for each route: the "doesn't map" list of `canvas.md` (custom pages and Teams apps too), `power-pages.md`, `power-bi.md`, `model-driven.md` and `code-apps.md`.
- The `Data` ledger's display names, and the security roles in the plan, for the "who sees this" notes.
- Your request's notes from the person, if any: apply them.

Read nothing from `decisions.md` answers, environment facts or the tenant, and run no `pac`. Without a Theme block, say so in the report and use the neutral pairs of section 4.

## 2. Pick the screens

1. Sketch each screen `Screens` names that the maker lays out freely: canvas and custom pages, Power Pages, generative pages, one Power BI page, and for a model-driven app its theme, forms (tabs and sections) and views (columns). A code app gets no sketch: its first slice is hand-written mock data. If that is the only route, write no file and report "code app: the person logs `prototype: skipped (mock-data slice)`". Flows, tables and agents have no screens.
2. One to eight screens, distinct layouts only. Past eight, keep the screens that carry the most Goal lines and say which you left out.
3. Each `data-screen` value is the screen's name as written in `Screens`, whole words, so the tool finds it there.

## 3. File rules

The tool checks these as text, after comments are removed. It never renders the file.

- **Size and shape.** At most 40,960 bytes (aim for 32,000). Plain ASCII; write other characters as character references.
- **First HTML comment.** A line that starts with `route:` naming each product sketched (canvas, custom page, Bootstrap 3 or 5 site, code site, generative page, model-driven, Power BI), then the colour pairs (section 4). For canvas, add the theme as the YAML that Paste theme takes; for a code site, name the UI library.
- **Label.** A visible "Sketch" label in body text, in a banner at the top.
- **Screens.** Each screen is an element with `data-screen="<name>"`. Inside it, after the normal view, four blocks with `data-state="empty"`, `data-state="loading"`, `data-state="error"` and `data-state="failed-save"`. A state the plan says does not exist (a read-only report cannot fail to save) says "none" with the plan's reason.
- **Who sees this.** One line per screen: the roles or personas and why, from the plan. If the plan names none, say "all signed-in users; the plan names no roles".
- **Navigation.** Only `href="#..."` to an `id` in the file. No other link.
- **Banned.** Tags: `script`, `link`, `meta`, `base`, `iframe`, `frame`, `object`, `embed`, `img`, `picture`, `svg`, `form`, `video`, `audio`, `source`. Attributes: `src`, `srcset`, `action` and any `on...` event. CSS: `url(`, `@import`, `@font-face`, `image-set(`, `expression(`. So no `meta` charset or viewport, no `form` tag (draw fields with `label`, `input` and `div`), and no icons: use words.
- **Style.** Inline CSS in one `style` element, with the Theme block's values as CSS variables, and system fonts only: the Theme font first, then `"Segoe UI", system-ui, sans-serif`. Flex and `gap`, no absolute positions or fixed container widths, no media queries (they need `@`).

## 4. Design

The point of the sketch is to show the screen the plan describes, clearly and accessibly, so the build has something better than a guess.

- **Colour.** Take the seed colour, the header pair and the font from the Theme block, and nothing else. Add only white, near-black and greys. Calculate each text and background pair with WCAG's relative luminance, and list it in the first comment as `pair: <use> <text hex> on <background hex> = <ratio>:1`. Text needs 4.5:1; large text, icons used as buttons, input and focus borders and chart marks need 3:1. For a model-driven app list the header pair at rest, hover, pressed and selected (4.5:1 each). If a Theme value fails, use the nearest passing one, mark the pair `adjusted`, and say so in your report: you do not edit the plan.
- **Layout.** One `h1` for the file, one `h2` as each screen's main heading and `h3` for its states. Use `header`, `nav`, `main` and labelled `section` landmarks. DOM order is reading order, no `tabindex`. Show a visible `:focus-visible` outline of 3 px at 3:1. Every control has a visible label; every control and link is at least 32 px high and 24 px wide. Colour is never the only signal: an error says "Error" in words.
- **Density.** Fit the job. Data-entry grids and forms: tighter rows (about 32 px), many fields, grouped in sections. Overviews and queues: calmer, larger type, one clear primary action. Dashboards and Power BI: a box per visual, at most 12 a page, each with its title and the fields it shows.
- **States.** Each block says what the user sees and the one thing they do next, in plain words. Empty: why it is empty and the action that fills it. Loading: what is loading, with a status line and a way to cancel or wait. Error: what happened, that their work is safe, and a retry or who to ask. Failed save: that nothing was saved, that their entries are kept, and a button to try again or save as a draft. A line like "Something went wrong" alone fails.
- **Modern first.** Use only controls the product has. For canvas, a screen that needs a control on the "doesn't map" list names its workaround beside it, or drops it. Custom pages and Teams apps use modern controls, not Fluent UI (v8). Power Pages: a small hand-written CSS subset with Bootstrap's class names, never the stylesheet itself. Power BI: one 1920 by 1080 page, no chart library.
- **No brand or tenant.** A logo is a labelled box with a placeholder name, never an image or a real brand.

## 5. Data

- Fake values under the ledger's display names, and nothing from answers, environment facts or the tenant. Make them plausible and varied: five to eight rows, a mix of statuses and one long value, so the layout is tested.
- Columns under column security or holding personal data (national IDs, phone numbers, dates of birth, home addresses, pay, health) show a mask such as `*****`, with a note in the screen's "who sees this" line.
- A fake person is a name such as "Sample Person A", never an email or a real or customer name.
- Write no `@` anywhere (not in CSS either), no `http` (not even in text), no GUID, no `svg`. The tenant-data scan reads this file and flags them.
- Do not copy `<url>`, `<email>` or similar placeholders from the plan: write the words instead.

## 6. Check

1. Count bytes with `wc -c`.
2. Run `powerops plan status docs/plans/<name>`. If `powerops` is not found, say "not checked" and why.
3. Fix every prototype warning that names the file and run it again, at most three runs. A warning that no `prototype:` approve or skip line exists yet is expected: `powerops-plan` writes that line after the person answers. Do not write it.
4. If a warning remains that you cannot fix, report it. Never weaken a screen, state or pair to silence a check.

## 7. Report

Reply to the caller in a few lines: the path, the size in bytes, the screens by name, each state left "none" and why, each colour pair you adjusted, the screens you left out, any assumptions, and the last `plan status` result. Say the next step is the person's approval, changes or skip.

## Per agent

- **Claude Code:** the installed copy of this skill runs in a forked context on the top model.
- **Codex and other agents:** `powerops-plan` starts a subagent that runs this skill with the plan name. Without subagents, it follows these steps in its own session.
