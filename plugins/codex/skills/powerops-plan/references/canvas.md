# Canvas apps

Read this when a plan names a canvas app or a custom page. Write the answers in `Users`, `Data`, `Screens` and `Steps`.

## 1. The plan

- **Personas and form factor.** Name each persona the app serves and its device: phone, tablet or browser. Several personas with different tasks in one app need a reason, or the app splits.
- **Rows per source.** For each data source, the rows now and in three years, from the ledger. A source above 500 rows needs a delegation note: which queries delegate, or why the data row limit is enough (`data.md`, gate 2).
- **Components.** Reuse a component library before planning a new component, and say which library was checked.
- **Theme.** Write the theme as the YAML that Paste theme takes, so a person can paste it into Studio. Learn's theming page gives its shape: under `Themes:`, the theme's name with the required Font, BasePaletteColor, HueTorsion and Vibrancy. It is the canvas part of the plan's one Theme block (`design.md`).
- **The first step** creates the blank app inside the plan's solution, with coauthoring turned on for the app. Don't rely on the setting that adds new apps to a solution by default: it is in preview.
- **Save and publish.** A person saves and publishes in Studio. A compile through the Canvas Authoring MCP server isn't a save, so the step that compiles names who saves, and the step after it checks the saved time.
- **Microsoft's gate.** `canvas-app` has its own planning and approval stage. It comes after this plan's sign-off, as the second, product-specific gate. Add no third.

## 2. The sketch: what doesn't map

On 2026-11-05 Learn's modern controls reference listed 30 controls, 7 in preview (Attachments, Card, Copilot answer, Dropdown, Persona, Stream, Table). None of these has a modern control: accordion, breadcrumb, dialog, drawer, menu, nav, toast, toolbar and tree. A sketch that shows one names its workaround beside it, or drops it:

- Dialog or drawer: a separate screen.
- Toast: the `Notify` function.
- Accordion, breadcrumb, menu, nav, toolbar or tree: a row or column of buttons, or the Creator Kit, named as a dependency in the plan. Microsoft doesn't support the Creator Kit officially (its issues go to GitHub), and Learn says to use it only when no modern control fits.

Check the reference again before relying on this list: the preview list moves.

Fluent UI (v8) controls in custom pages and Teams canvas apps are deprecated: plan modern controls. A page that already has v8 controls gets a step for Studio's Update action, which covers Button, Text input, Combo box, Date picker, Radio group, Rating and Slider and drops the properties it can't map.

## Sources

Checked on 2026-11-03:

- [Use modern themes in canvas apps](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/modern-controls/modern-theming): Paste theme and Copy theme, in YAML, and the required fields.
- [Add canvas apps and cloud flows to a solution by default](https://learn.microsoft.com/power-apps/maker/canvas-apps/add-app-solution-default): the canvas setting is in preview.
- [Coauthoring and copresence](https://learn.microsoft.com/power-apps/maker/canvas-apps/copresence-power-apps-studio): turned on per app.
- [Delegation overview](https://learn.microsoft.com/power-apps/maker/canvas-apps/delegation-overview): the data row limit, 500 by default.
- [Component library](https://learn.microsoft.com/power-apps/maker/canvas-apps/component-library) and [Creator Kit](https://learn.microsoft.com/power-platform/guidance/creator-kit/overview).
- [Notify function](https://learn.microsoft.com/power-platform/power-fx/reference/function-showerror).
- [canvas-apps plugin](https://github.com/microsoft/power-platform-skills/tree/96d16d0/plugins/canvas-apps) (MIT) at `96d16d0`: `canvas-app`'s approval stage.

Checked on 2026-11-05:

- [Modern controls and properties in canvas apps](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/modern-controls/modern-controls-reference): the 30 controls and the 7 in preview.
- [The Creator Kit explained](https://learn.microsoft.com/power-platform/guidance/creator-kit/creator-kit-explained): not officially supported; modern controls first.
- [Upgrade Fluent UI controls to modern controls](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/modern-controls/upgrade-fluent-ui-controls-to-modern): v8 deprecated in Teams canvas apps and custom pages; the seven controls Update covers.
