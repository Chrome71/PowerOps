# Route: which product, and the sketch

Read this when a plan names an app, a page, a site, a report or an agent. Section 1 picks the product and names the file to read next. Section 2 says when to offer **D**, "sketch the screens first", and how to write the sketch.

## 1. The route

These are PowerOps's planning rules. Each product file cites Learn for what Learn states.

- Records to create, read and edit: a model-driven form and view (`model-driven.md`).
- An overview, a queue or guided work over Dataverse: a generative page in a model-driven app (`model-driven.md`).
- A custom page only for a canvas control or a generally available connector that a form or a generative page can't give (`model-driven.md`, `canvas.md`).
- A code component (PCF) only for a reusable field or dataset control (`model-driven.md`).
- A Code App only when all four hold: every user is in the tenant, every user has a premium licence, a development team owns TypeScript and CI, and the UI needs npm libraries or automated tests (`code-apps.md`).
- A standalone app laid out freely, for users in the tenant: a canvas app (`canvas.md`).
- Anonymous or external users: Power Pages (`power-pages.md`).
- A report or dashboard over a semantic model: Power BI, which lives outside the solution (`power-bi.md`).
- An agent that answers or acts in chat: a Copilot Studio agent, on the standard harness unless the job needs another (`copilot-studio.md`).

Write the route in `Screens`, one line per app or page with its reason. When `Screens` names any screen, page, site or report, also read `design.md`: the plan's one Theme block, its accessibility target and the states of each screen. Where an app's business rules, status columns or approvals live: `logic.md`.

## 2. The sketch

Offer **D** whenever `Screens` names anything, and start `powerops-prototype` at approval (it holds the file rules):

| Product | Offer |
|---|---|
| Canvas | Yes, with the "doesn't map" list in `canvas.md` |
| Custom pages | Yes: the canvas list applies, custom pages don't use the modern theme, and their Fluent UI (v8) controls are deprecated (`canvas.md`) |
| Power Pages, Bootstrap site | Yes, after the Bootstrap version is known (`power-pages.md`) |
| Power Pages, code site | Yes: name the UI library |
| Generative pages | Yes: the user attaches a screenshot of the sketch in the page builder |
| Power BI | Yes: one 1920 by 1080 page, a box per visual (`power-bi.md`) |
| Code Apps | No sketch: a mock-data first slice instead (`code-apps.md`), and the person logs `prototype: skipped (mock-data slice)` |
| Model-driven apps | Yes: the theme (`CustomTheme` values and the header pair), each form's tabs and sections, and each view's columns. Metadata lays the rest out, so the sketch shows intent only (`model-driven.md`) |
| Flows, tables, agents | No: no screens |

The file:

- One file, `docs/plans/<name>/mockups/prototype.html`: static HTML with inline CSS from the Theme block, no scripts, fonts, images or external links, fake data under the ledger's display names.
- Distinct layouts only, at most 8 screens and 40 KB (40,960 bytes).
- Markers the tool checks: the first HTML comment holds a `route:` line (canvas, custom page, Bootstrap 3 or 5 site, code site, generative page, model-driven or Power BI) and the colour pairs; body text holds a visible "Sketch" label; each screen is a `data-screen` element named as in `Screens`, with `data-state` blocks `empty`, `loading`, `error` and `failed-save` (a block may say "none" and why), and a note of who sees it; navigation is `href="#..."` only.
- For canvas, the header comment also holds the theme as the YAML that Paste theme takes: the one part that transfers directly.
- Flex and `gap`, no absolute positions or fixed container widths.
- Text contrast of 4.5:1 and non-text contrast of 3:1, visible labels, DOM order equal to reading order, no positive `tabindex`.
- Every screen in the sketch is already in `Screens`. Draw its empty, loading, error and failed-save states as `data-state` panels by the look below, not as notes; each says what the user does next.

The look. A designer reviews the sketch, so it shows a professionally built app, not a wireframe. Canvas modern controls, the model-driven modern look and Power BI's default base theme all follow Fluent 2, so this style transfers; the product file says how each rule maps to its controls and theme:

- **Tokens.** A `:root` block of CSS custom properties opens the `<style>`, and every colour, size and space after it is a `var()`. No raw hex values elsewhere.
- **Colour.** Brand, with its hover and the text colour on it, from the theme's seed; neutrals for the page background, card surface, subtle fill, stroke, and primary and secondary text; success, warning, danger and info, each a text-and-tint pair. The header comment lists each text-on-background pair with its contrast ratio.
- **Type.** The theme's font in at most five sizes from one scale, for example 12, 14, 16, 20 and 28 px (caption, body, subtitle, section title, page title), line height 1.3 to 1.5, weights 400 and 600. One page title per screen.
- **Spacing.** An 8 px grid with 4 px steps for tight pairs: every padding, gap and margin is 4, 8, 12, 16, 24, 32 or 48 px. Cards have 16 px padding and 16 px gaps; page margins are 16 px on a phone and 24 to 32 px wider. Related items sit closer than unrelated ones.
- **Surfaces.** Three levels: the page on the neutral background; cards on the surface colour with an 8 px radius and one light shadow or a 1 px stroke; raised layers (a confirm screen standing in for a dialog) with a stronger shadow. No dashed boxes, and no border on every element.
- **Header and navigation.** The same header on every screen: the app or page title, the user, and the screen's primary action in the same place. Top-level sections use one pattern (tabs on a phone, a left rail on a desktop) and mark the current one by more than colour.
- **Status.** A labelled pill: a short word (Draft, Submitted, Approved) in a fully rounded tint, coloured by meaning from the status tokens, with the same word and colour wherever it shows. Never colour alone, and never a bare "Status: X" line.
- **Content.** A list row or card has a title line, two or three facts in secondary text, the status pill, and amounts aligned right. One filled primary button per region, secondary actions outlined, a destructive action set apart. Fake data looks real: plausible names, dates and amounts, 3 to 6 rows.
- **Responsive.** Show the phone layout at 360 px in one column, and the tablet and desktop layout as master-detail: the list about a third wide on the left, the selected record on the right. Use `flex-wrap` or one `@media (min-width: 600px)` rule. Targets are at least 24 by 24 px, and 44 px high on the phone.
- **States.** Empty: a heading, one sentence on why, and the action that fills it. Loading: skeleton rows in the subtle fill, or a spinner with a text label. Error: a message bar in the danger tint with the cause in plain words and a retry or a way out. Failed save: that bar above the form, the input kept, and the field in error named in text with a 3:1 stroke. A row of state panels isn't a screen, but it counts toward the 40,960 bytes.

A passing sketch proves intent, not the build: nothing shows that a build skill follows it. Keep visual detail out of `Screens` and `Steps`, and write no Step that says "match the sketch": the Verifier compares the build with those sections. A control map and control names in the sketch wait until one canvas build has tested whether `canvas-app` uses a sketch.

## Sources

Checked on 2026-11-03:

- [Overview of custom pages](https://learn.microsoft.com/power-apps/maker/model-driven-apps/model-app-page-overview) and the [modern look FAQ](https://learn.microsoft.com/power-apps/user/modern-fluent-design): custom pages bring canvas into model-driven apps and don't use the modern theme.
- [Generate a page using natural language](https://learn.microsoft.com/power-apps/maker/model-driven-apps/generative-pages): attached images, and "clear and detailed sketches or wireframes" work best.
- [pac pcf](https://learn.microsoft.com/power-platform/developer/cli/reference/pcf): the `field` and `dataset` templates.
- [Power Apps code apps overview](https://learn.microsoft.com/power-apps/developer/code-apps/overview): the admin setting and Microsoft Entra sign-in.
- [What is Power Pages?](https://learn.microsoft.com/power-pages/introduction) and [Power Pages security](https://learn.microsoft.com/power-pages/security/power-pages-security): external-facing sites and anonymous users.
- [Use modern themes in canvas apps](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/modern-controls/modern-theming): Paste theme takes YAML.

Checked on 2026-11-05:

- [Upgrade Fluent UI controls to modern controls](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/modern-controls/upgrade-fluent-ui-controls-to-modern): Fluent UI (v8) controls in custom pages are deprecated.

Checked on 2026-11-08:

- [Overview of modern controls and themes in canvas apps](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/modern-controls/overview-modern-controls), [Use modern themes in model-driven apps](https://learn.microsoft.com/power-apps/maker/model-driven-apps/modern-theme-overrides) and [Visual defaults in Power BI reports](https://learn.microsoft.com/power-bi/create-reports/power-bi-reports-visual-defaults): each follows Fluent 2, and new Power BI reports use the Fluent 2 base theme by default.
- The type scale, the 8 px grid, the 44 px phone target and the rest of the look are PowerOps's rules; the 24 by 24 px minimum is WCAG 2.2's target size.
