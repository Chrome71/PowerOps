# Canvas apps

Read this when a plan names a canvas app or a custom page. Write the answers in `Users`, `Data`, `Screens` and `Steps`.

## 1. The plan

- **Personas and form factor.** Name each persona the app serves and its device: phone, tablet or browser. Several personas with different tasks in one app need a reason, or the app splits.
- **Rows per source.** For each data source, the rows now and in three years, from the ledger. A source above 500 rows needs a delegation note: which queries delegate, or why the data row limit is enough (`data.md`, gate 2).
- **Components.** Reuse a component library before planning a new component, and say which library was checked.
- **Theme.** Write the theme as the YAML that Paste theme takes, so a person can paste it into Studio. Learn's theming page gives its shape: under `Themes:`, the theme's name with the required Font, BasePaletteColor, HueTorsion and Vibrancy. It is the canvas part of the plan's one Theme block (`design.md`). Paste theme reads the theme from the clipboard, and **Add a theme** also offers **Create custom theme**, a dialog with the seed colour, font, torsion and vibrancy. Where the build runs in a browser pane that lets no page read the clipboard, as observed in one run (2026-11-11), the step uses Create custom theme with the plan's seed, and the plan's Theme says so.
- **The first step** creates the blank app inside the plan's solution, with coauthoring turned on for the app. Don't rely on the setting that adds new apps to a solution by default: it is in preview.
- **Save and publish.** A person saves and publishes in Studio. A compile through the Canvas Authoring MCP server isn't a save, so the step that compiles names who saves, and the step after it checks the saved time.
- **Microsoft's gate.** `canvas-app` has its own planning and approval stage. It comes after this plan's sign-off, as the second, product-specific gate. Add no third.

## 2. The sketch: what doesn't map

On 2026-11-08 Learn's modern controls reference listed 30 controls, 7 marked preview (Attachments, Card, Copilot answer, Dropdown, Persona, Stream, Table), and in that list the Card gallery, the Form and the Data Grid are not marked preview. Learn disagrees with itself here (seen 2026-11-14): the Card gallery, Metrics card and Avatar group pages carry a preview banner that the list doesn't show, and the Dropdown page carries none though the list marks it preview. This file takes the stricter reading: a control is preview where either page says so. Form, Badge, Tab list and Avatar carry no banner and no mark. None of these has a modern control: accordion, breadcrumb, drawer, menu, nav, toast, toolbar and tree, and a dialog only through a function. A sketch that shows one names its workaround beside it, or drops it:

- **Icons** (checked 2026-11-14). The modern Icon control draws about 180 Fluent icons, Outline (the default) or Filled, in a colour (`IconColor`). It isn't in the reference list, and its page carries no preview banner, so this file treats it as GA. A sketch draws Fluent icons, never a character reference.
- **Dialog** (checked 2026-11-14). `Confirm()` shows a confirm and a cancel button, in the Fluent dialog when modern controls are on; Learn marks it preview in canvas apps. Without a recorded preview choice, the fallback is a separate screen, or an overlay container, which can't trap focus or close on Esc. A drawer: a separate screen.
- **Badges in a list** (checked 2026-11-14). The Data Grid's columns are Text, Number, Phone, Email, URL and Button (its property list also names an Image column): no badge column, all rows one height, and no per-column text styling. A list with badges is a classic Gallery with modern controls (Text, Badge, Avatar, icon Buttons) in its row.
- Toast: the `Notify` function, a banner at the top of the app, not a corner toast.
- Accordion, breadcrumb, menu, nav, toolbar or tree: a Tab list for top-level sections, a row or column of buttons, or the Creator Kit, named as a dependency in the plan. Microsoft doesn't support the Creator Kit officially (its issues go to GitHub), and Learn says to use it only when no modern control fits.

Check the reference again before relying on this list: the preview list moves.

Fluent UI (v8) controls in custom pages and Teams canvas apps are deprecated: plan modern controls. A page that already has v8 controls gets a step for Studio's Update action, which covers Button, Text input, Combo box, Date picker, Radio group, Rating and Slider and drops the properties it can't map.

## 3. The look in modern controls

How the look in `look.md` and the canvas statements C1 to C10 of `baseline.md` map when the app is built. The table below is what a sketch may draw; the map names each element's row, status and page, and a part that is preview, Kit or unsupported is drawn only when the plan records that choice (gate 3), else its GA fallback is.

- **C1.** A `desktop` frame of 1366 by 768, the 16:9 default, and a `phone` frame about 390 px wide. `App.SizeBreakpoints` is 600, 900 and 1200 by default in tablet and web apps: one column below 600, list with detail above it, as the Sidebar and Split-screen layouts give. Turn Scale to fit off.
- **C2.** The header is a horizontal container with a fill, a Text and an Avatar: the Header control takes no custom colours or navigation and is added to each screen by hand. When the plan's Theme gives a header pair, the container takes it. Navigation is a vertical container of Buttons, or a Tab list.
- **C3.** Summary tiles are containers with Text controls (and a Badge for the trend) unless the plan chooses the preview Metrics card.
- **C4.** Horizontal and vertical containers take the spacing in Gap and padding; a container's fill, corner radius and drop shadow make a card.
- **C5.** A list row is a classic Gallery row (section 2). Long titles wrap to two lines, because an ellipsis on overflow is not confirmed.
- **C6 and C7.** Forms: the modern Form control, for edit and display forms, or containers with inputs. The inputs have no helper-text or error-text property, so each is a Text control. Learn says how to choose and order the Form's columns, but nothing about writing it in `.pa.yaml`, which Learn calls in active development; observed in one run (2026-11-11), a Form and its cards written only in YAML compiled, but Studio left them uninitialised until the fields were added with Studio's **Edit fields**. So the plan's step adds the fields in Studio first, and a compile changes the cards after that.
- **C8.** The modern Button is 96 by 32 by default; a phone layout raises it to 44 px or more.
- **C9.** Charts are the classic Column and Line charts: at most 9 series, and not inside a layout container. A ring, donut or sparkline names its route from the table.
- **C10.** The map takes each control's status from the table.
- **Colour.** The Paste-theme seed and font make the brand and neutral palette. Status colours come from a control's intent, so the sketch's status tokens are named after the Badge intents: Positive, Caution, Critical, Informative and Neutral.
- **Focus.** Learn's `SetFocus` page names Button, Icon, Image, Label and Text input, and no modern control, and rules out a control inside a gallery, an edit form, a component or a container; observed in one run (2026-11-11), Studio refused `SetFocus` on a modern Text input. So a plan promises no focus move on a modern input.

`CA/` is `learn.microsoft.com/power-apps/maker/canvas-apps/`, `MC/` its `controls/modern-controls/` and `CC/` its `controls/`; `FX/` is `learn.microsoft.com/power-platform/power-fx/reference/` and `KIT/` `learn.microsoft.com/power-platform/guidance/creator-kit/`. Status as Learn stated it on 2026-11-11, spot-checked 2026-11-14.

| Pattern | Built with | Status | Learn page |
|---|---|---|---|
| Brand header with app name and user | Horizontal container with a fill, a Text and an Avatar. The modern Header takes no custom colours and no navigation, and is added per screen. | GA (the Header page seen in search excerpts only on 2026-11-14) | `MC/modern-controls-header`, `CC/control-horizontal-container`, `MC/modern-control-avatar` |
| Left navigation rail | Vertical container of modern Buttons (icon before the label, or icon only with a Tooltip), the selected one in another Appearance; or the Kit's Nav | GA; Kit | `MC/modern-control-button`, `KIT/components` |
| Tabs | Tab list (Appearance Underline, Subtle, Filled or Transparent), 2 to 7 tabs; you show and hide the content yourself | GA | `MC/modern-control-tabs-or-tabs-list` |
| Icons | Icon control: about 180 Fluent icons, Outline or Filled, a colour | GA (not in the reference list) | `MC/modern-control-icon` |
| Status pill | Badge: Intent, Appearance Tint, Shape Circular, an optional icon | GA | `MC/modern-controls-badge` |
| KPI tile with trend | Container with Text controls and a Badge; or the Metrics card (title, value, description, trend value and text, trend type; radius 16 by default; no per-field text colour, no sparkline) | Container GA; Metrics card preview | `MC/modern-control-metrics-card` |
| Card grid | Classic Gallery with a wrap count and any controls inside; or the Card gallery (minimum card width 240, gap 16, one card height, built-in empty and loading text, no child controls) | Gallery GA; Card gallery preview | `CC/control-gallery`, `MC/modern-control-card-gallery` |
| List or table with pills and row actions | Classic Gallery whose row holds Text, Badge, Avatar and icon Buttons | GA | `CC/control-gallery`, `MC/modern-control-data-grid` |
| Plain sortable table | Data Grid: search, sort, paging; columns Text, Number, Phone, Email, URL, Button (and Image) | GA | `MC/modern-control-data-grid` |
| List with detail | Horizontal container: gallery left, detail container right; one column below 600 | GA | `CA/build-responsive-apps`, `CA/create-responsive-layout` |
| Form with sections and errors | Modern Form, or containers with Text input, Combobox, Date picker and Number input; inputs Outline or Filled, ValidationState None or Error; helper and error text as Text controls. Sections in the modern Form: not confirmed | GA | `MC/modern-control-form`, `MC/modern-control-text-input` |
| Search box | Text input with Type Search; whether it draws an icon: not confirmed | GA | `MC/modern-control-text-input` |
| Filter chips, segmented choice | Row of Buttons whose Appearance follows the selection; a horizontal Radio group; or a Filled Tab list | GA | `MC/modern-control-button`, `MC/modern-controls-radio-group` |
| Stepper | Horizontal gallery of numbered circles and labels; or the Kit's SubwayNav | GA; Kit preview | `CC/control-gallery`, `KIT/components` |
| Progress bar | Progress bar: value, max, colour Brand, Success, Warning or Error; horizontal only, no label | GA (page seen in search excerpts only on 2026-11-14) | `MC/modern-control-progress-bar` |
| Spinner, skeleton | Spinner with a label; skeleton rows as grey containers, or the Kit's Shimmer | GA (Spinner page seen in search excerpts only on 2026-11-14); Kit | `MC/modern-control-spinner`, `KIT/components` |
| Notification | `Notify`: a banner at the top of the app, four types, 10 s by default | GA | `FX/function-showerror` |
| Confirm dialog | `Confirm()` (two buttons, the Fluent dialog when modern controls are on); or an overlay container, which can't trap focus or close on Esc | `Confirm` preview; overlay GA | `FX/function-confirm`, `CA/accessible-apps-limitations` |
| Elevated card | Container: fill, radius, drop shadow | GA | `CC/control-horizontal-container` |
| Bar and line chart | Classic Column chart and Line chart: at most 9 series; not inside a layout container | GA | `CC/control-column-line-chart`, `CA/build-responsive-apps` |
| Donut, gauge, horizontal bars | The Kit's DonutChart, GaugeChart and HorizontalBarChart; the classic Pie chart has no donut option | Kit preview | `KIT/components`, `CC/control-pie-chart` |
| Sparkline, progress ring, custom drawing, gradient | An SVG data URI in an Image control; it needs an accessible label and has no hover state. The only Microsoft page on it is an archived 2017 blog that calls it not officially supported | Unsupported | `learn.microsoft.com/archive/blogs/davidni/advanced-powerapps-visualizations-with-dynamic-svgs` |
| Theme | Theme YAML: font, one seed colour, hue torsion, vibrancy, 8 overridable slots; `App.Theme.Colors` in formulas. One ramp only: a second hue is set by hand per control. No dark mode | GA | `MC/modern-theming` |
| Focus and labels | `AccessibleLabel`; the focus border stays visible; headings and live regions only on the classic Label | GA | `CC/properties-accessibility`, `CA/accessibility-checker` |

Not confirmed for canvas: custom web fonts, sections in the modern Form, hover styling of Data Grid rows, which modern controls work in a custom page beyond Learn's list, and a documented number of controls per screen.

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

Checked on 2026-11-08:

- [Modern controls and properties in canvas apps](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/modern-controls/modern-controls-reference): the 30 controls and the 7 marked preview, with Badge, Card gallery, Data Grid, Header, Metrics card, Spinner and Tabs or tab list ("select a tab to move screens").
- [Badge](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/modern-controls/modern-controls-badge): Intent, Appearance Tint, Shape Circular ("pill-shaped") and AccessibleLabel.
- [Header](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/modern-controls/modern-controls-header): added to each screen by hand; no custom colours or app navigation.
- [Metrics card (preview)](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/modern-controls/modern-control-metrics-card): TrendValue, TrendText and TrendType.
- [Horizontal container](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/control-horizontal-container): Gap, padding, drop shadow, fill and corner radius.
- [Create responsive layouts](https://learn.microsoft.com/power-apps/maker/canvas-apps/create-responsive-layout) and [Building responsive canvas apps](https://learn.microsoft.com/power-apps/maker/canvas-apps/build-responsive-apps): `App.SizeBreakpoints` 600, 900 and 1200, Scale to fit off, and the Sidebar and Split-screen layouts.

Checked on 2026-11-11 by the design-research session, and spot-checked on 2026-11-14 for M97 (its plan's Assumptions A1 to A6):

- [Modern controls and properties in canvas apps](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/modern-controls/modern-controls-reference): the list's preview marks, and the pages that disagree with it (Card gallery, Metrics card, Avatar group, Dropdown).
- [Icon](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/modern-controls/modern-control-icon): about 180 icons, `IconStyle` Outline or Filled, `IconColor`; no preview banner.
- [Confirm function](https://learn.microsoft.com/power-platform/power-fx/reference/function-confirm): a confirm and a cancel button, the Fluent dialog with modern controls on, preview in canvas apps.
- [Data Grid](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/modern-controls/modern-control-data-grid): its column types, one row height, no per-column text styling.
- [Card gallery](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/modern-controls/modern-control-card-gallery), [Tab list](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/modern-controls/modern-control-tabs-or-tabs-list), [Button](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/modern-controls/modern-control-button) and [Text input](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/modern-controls/modern-control-text-input): the rows of the table.
- [Create responsive layouts](https://learn.microsoft.com/power-apps/maker/canvas-apps/create-responsive-layout): the 1366 by 768 default and the breakpoints. [Building responsive canvas apps](https://learn.microsoft.com/power-apps/maker/canvas-apps/build-responsive-apps): charts aren't supported in layout containers. [Column and line chart](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/control-column-line-chart): at most 9 series.
- [Accessibility limitations in canvas apps](https://learn.microsoft.com/power-apps/maker/canvas-apps/accessible-apps-limitations): an overlay container doesn't trap focus.

Checked on 2026-11-12:

- [Modern controls and properties in canvas apps](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/modern-controls/modern-controls-reference): the Card gallery, Form and Data Grid are not marked preview.
- [Form modern control in canvas apps](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/modern-controls/modern-control-form): edit and display forms, and choosing and ordering columns; nothing on its YAML.
- [View source code files for canvas apps](https://learn.microsoft.com/power-apps/maker/canvas-apps/power-apps-yaml): `.pa.yaml` is in active development and may be incomplete.
- [SetFocus function](https://learn.microsoft.com/power-platform/power-fx/reference/function-setfocus): the controls it supports, and not inside a gallery, an edit form, a component or a container.
- [Use modern themes in canvas apps](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/modern-controls/modern-theming): **Add a theme** offers **Create custom theme** or **Paste theme**, whose first step copies the YAML to the clipboard.
