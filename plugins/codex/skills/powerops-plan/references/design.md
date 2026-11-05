# Design: one theme, one accessibility target, the states of each screen

Read this when `Screens` names a screen, page, site or report (`route.md`). Write the answers in `Screens`. These are PowerOps's planning rules; the sources say what Microsoft and W3C state.

## 1. One Theme block

`Screens` holds one `### Theme` block for the whole plan, and every app, page, site and report in the plan takes its values from it. Where a product can't take a value, the block says so.

- **Brand values.** The seed colour as hex, the font, and the logo by a placeholder web resource name. No customer brand files go in the repo.
- **Canvas apps.** The YAML that Paste theme takes: under `Themes:`, the theme's name with Font, BasePaletteColor, HueTorsion and Vibrancy, and ColorOverrides only where needed (`canvas.md`).
- **Model-driven apps.** The attributes of the `CustomTheme` web resource that the app's Custom theme definition setting names: BasePaletteColor, Font, LockPrimary and the AppHeaderColors pairs (`model-driven.md`). Custom pages don't use it.
- **Power Pages.** The theme from the Styling workspace (one of its 13 presets, or the brand colours) and the site's Bootstrap version (`power-pages.md`).
- **Power BI.** The report theme JSON file by name. Only `name` is required in it (`power-bi.md`).
- **Code apps and generative pages.** The UI library and how it takes the brand values (its tokens), so that no screen hard-codes them.

## 2. The accessibility target

WCAG 2.2 AA for every screen, unless the plan gives a reason. Microsoft's pages differ: the canvas theming page asks WCAG 2.1 AA at least, and Power Pages states WCAG 2.2 conformance for the product, while what a maker adds is the maker's to check. WCAG 2.2 AA adds these to 2.1 AA:

- Targets at least 24 by 24 CSS pixels, within the criterion's exceptions (2.5.8).
- A focused control never entirely hidden by other content, such as a sticky header (2.4.11).
- A single-pointer way to do what a drag does (2.5.7).
- Help in the same place on every screen that offers it (3.2.6).
- What the user already entered in the process is filled in or offered, not typed again (3.3.7).
- No cognitive test, such as recalling a password, to sign in, unless there is an alternative or help such as paste (3.3.8).

Contrast: at least 4.5:1 for text, and 3:1 for large text, interactive components and data visualizations.

## 3. Each screen

One line per screen in `Screens`, beside its route:

- **Task.** What the user comes to the screen to do.
- **Form factor.** Phone, tablet or browser.
- **States.** Empty, loading, error and failed save: what the user sees in each, and what they do next. A screen without one of them says "none" for it.

The sketch, when there is one, shows the same states (`route.md`).

## Sources

Checked on 2026-11-05:

- [Use modern themes in canvas apps](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/modern-controls/modern-theming): the Paste theme YAML, its required fields, and WCAG 2.1 AA at least.
- [Modern theme overrides](https://learn.microsoft.com/power-apps/maker/model-driven-apps/modern-theme-overrides): the `CustomTheme` web resource, the Custom theme definition setting and the AppHeaderColors pairs.
- [Style a site](https://learn.microsoft.com/power-pages/getting-started/style-site): the 13 preset themes. [Accessibility in Power Pages](https://learn.microsoft.com/power-pages/admin/accessibility): WCAG 2.2.
- [Create a custom report theme](https://learn.microsoft.com/power-bi/create-reports/report-themes-create-custom): only `name` is required.
- [What's new in WCAG 2.2](https://www.w3.org/WAI/standards-guidelines/wcag/new-in-22/) (W3C): the six A and AA criteria and the 24 by 24 CSS pixel target.
- [Recommendations for design standards](https://learn.microsoft.com/power-platform/well-architected/experience-optimization/design-standards) (XO:02): the contrast ratios. [Recommendations for feedback and guidance](https://learn.microsoft.com/power-platform/well-architected/experience-optimization/feedback-guidance) (XO:05): errors with the next step. It names no loading or empty state; asking for them is PowerOps's rule.
