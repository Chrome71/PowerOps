# Route: which product, and the sketch

Read this when a plan names an app, a page, a site, a report or an agent. Section 1 picks the product and names the file to read next. Section 2 says when to offer **D**, "sketch the screens first", and where the sketch's rules live.

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

Offer **D** whenever `Screens` names anything, and start `powerops-prototype` at approval. It writes three options in different design directions to `mockups/`; the person, or an agent their brief lets choose, picks one, and the pick becomes `mockups/prototype.html`. Sign-off needs that file and the three options, or a person's skip with a reason.

| Product | Offer |
|---|---|
| Canvas | Yes, with the "doesn't map" list in `canvas.md` |
| Custom pages | Yes: the canvas list applies, custom pages don't use the modern theme, and their Fluent UI (v8) controls are deprecated (`canvas.md`) |
| Power Pages, Bootstrap site | Yes, after the Bootstrap version is known (`power-pages.md`) |
| Power Pages, code site | Yes: name the UI library |
| Generative pages | Yes: the user attaches a screenshot of the sketch in the page builder |
| Power BI | Yes: one 1920 by 1080 page, each visual drawn in CSS (`power-bi.md`) |
| Code Apps | No sketch: a mock-data first slice instead (`code-apps.md`), and the person logs `prototype: skipped (mock-data slice)` |
| Model-driven apps | Yes: the theme (`CustomTheme` values and the header pair), each form's tabs and sections, and each view's columns. Metadata lays the rest out, so the sketch shows intent only (`model-driven.md`) |
| Flows, tables, agents | No: no screens |

Each rule is written once:

- The file rules, the three directions and the checks: the `powerops-prototype` skill.
- How a screen must look, in a sketch and in the build: `look.md`. Planning doesn't read it.
- The Theme block, the accessibility target and the states of a screen: `design.md`.

A passing sketch proves intent, not the build. Keep visual detail out of `Screens` and `Steps`, and write no Step that says "match the sketch": the Verifier compares the build with those sections. `powerops-implement` compares each built screen with the picked sketch instead, and writes `design-check.md`. A control map and control names in the sketch wait until one canvas build has tested whether `canvas-app` uses a sketch.

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
