# Power BI

Read this when a plan names a Power BI semantic model, report or dashboard. Power BI has no Dataverse solution of its own and moves by its own route. Write the answers in `Data`, `Screens`, `Environments and route` and `Constraints`.

## 1. Efficiency

- **One topic per model,** with the questions it answers named. No model for everything.
- **Storage mode,** with a reason. Import is the default.
- **Star schema.** Each table is a dimension or a fact.
- **Relationships** filter one way. Each bi-directional one has a reason.
- **Auto date/time** off; use a date table.
- **Numeric types** that fit each column's precision and range: Fixed decimal, exact to four places, where rounding would introduce errors, such as money; Decimal (floating point) where its range or precision is needed.
- **Columns:** only those that serve reporting or the model's structure.
- **Incremental refresh** for large fact tables that grow.
- **Licence fit.** Pro gives a 1 GB model, 8 scheduled refreshes a day, no XMLA write and no deployment pipelines. More needs Premium Per User or Fabric capacity: put that under Constraints. Who needs which licence to share and view is in `licensing.md`.
- **Security.** Name the route: workspace roles, or row-level security with its roles.
- **Files and route.** The model and report are a Power BI project (PBIP) in Git, with one named deploy route: fabric-cicd, Git integration or deployment pipelines. Deployment pipelines don't support PBIR reports, and PBIR is the default report format, so a PBIR report goes by fabric-cicd or Git integration; Git integration marks reports and semantic models as preview. Deployment pipelines also don't support a DirectQuery or composite model that uses auto date/time or variation tables.
- **Sensitivity labels** aren't supported in a PBIP: a plan that needs one sets it in the service after the deploy, a write a Step names.
- **The solution** holds only an embedded dashboard and its environment variable, when a model-driven app shows the report.

## 2. Design

Five tests, for the plan's page list and later for screenshots:

1. **Purpose:** each page answers one stated question.
2. **Hierarchy:** one element dominates.
3. **Consistency:** type, colours and slicer positions match across pages.
4. **Restraint:** every visual adds a fact nothing else shows.
5. **Access:** alt text, a text contrast of 4.5:1, a reading tab order, and colour never the only signal.

Plan at most 12 data visuals a page, or give the reason. Learn says to limit visuals and gives no number; 12 is the warning in Microsoft's report skill.

## 3. The sketch

One 1920 by 1080 page per report page, in a `report` frame, each visual drawn in its place with its title, its subtitle and the fields it shows, and each chart drawn in inline SVG from the fake values, as `look.md` says under Charts. A report stores each visual's position, so the places map directly; how a chart renders beyond the table below can't be promised. New pages under the Fluent 2 defaults are 1920 by 1080 (generally available since the August 2026 update), with a grey background and padded, rounded visuals. The page follows the look in `look.md` and B1 to B12 of `baseline.md`, as a report:

- **Title row** (B1, B9). At most 72 px high, text on the canvas with no filled band: the report title, a subtitle with the page's question and the data's as-of date, the page navigator when there is more than one page, and the slicers at its right end. The canvas is light grey and the visuals white.
- **Grid** (B1). 24 to 32 px outer margins and 16 to 24 px gutters on 12 columns, on an 8 px grid. Every visual's edges sit on the grid, and visuals in a row share their top and height. Build with gridlines and snap to grid on.
- **KPIs** (B4). One Card visual with 3 to 6 measures under the title row: per measure the value, its label, and a reference label with the comparison (target, prior period or change) and its signed difference in words and sign, never colour alone; a sparkline when the trend matters.
- **Slicers** (B2). One to three, at the right end of the title row; four or more in a rail. The same place and order on every page.
- **Reading order** (B3). The lead number or visual top-left, the breakdowns in the middle, the table last and small. A bare single-value card is never the largest region.
- **Type** (B8). The theme's text classes carry B8's sizes: `title` and `largeTitle` for titles, `callout` for card values, `label` for the rest.
- **Charts** (B5 to B7, B10, S15). Each visual has a title and a subtitle that states the finding; one hue with greys, one series highlighted, the good and bad pair for variance with a sign; plain charts, sorted and from zero, a dashed target line with its label.
- **Only what is confirmed** (gate 3). The main frame draws only what the table below marks GA, unless the plan records a preview choice. A part the table marks not confirmed or not native is listed in the map under "Left out because not confirmed", and where it has a GA fallback, the fallback is drawn.
- **Numbers and access** (B11, B12). K, M and bn with one decimal, an as-of stamp in the title row; alt text planned for each data visual and a tab order equal to the reading order.
- **Base theme.** Learn says Microsoft manages a report's base theme and names Fluent 2, Classic 2026 and Classic 2018, but gives neither the base theme file's name nor its content: those are seen only in what Power BI Desktop saves (observed in exports, not on Learn), and Desktop runs on Windows only. So on a machine without Desktop a plan doesn't build over a named base theme, and its custom theme sets the colour of every text class: observed in one run (2026-11-11), contrast couldn't be measured until the report rendered.

Learn pages are under `learn.microsoft.com/power-bi/`. Status as Learn stated it on 2026-11-11, spot-checked 2026-11-14.

| Pattern | Built with | Status | Learn page |
|---|---|---|---|
| Page size, canvas colour | Page settings or the theme: 1920 by 1080 under the Fluent 2 defaults; background and wallpaper colour | GA | `create-reports/power-bi-reports-visual-defaults`, `create-reports/power-bi-report-display-settings` |
| Title row | Text box (with dynamic values for an as-of date) on the canvas | GA | `create-reports/power-bi-reports-add-text-and-shapes` |
| Page navigation | Page navigator, style preset Tab | GA | `create-reports/button-navigators` |
| KPI row | One Card visual with several measures: callout value, label, reference labels (title, value, detail), per-part colour by rule or measure; 12 px default padding | GA (since November 2025) | `visuals/power-bi-visualization-card` |
| Sparkline in a KPI or table cell | A measure that returns an SVG data URI, data category Image URL; or the table's built-in sparklines (5 per visual, 52 points) | GA | `visuals/power-bi-visualization-card`, `create-reports/power-bi-sparklines-tables` |
| Signed difference with colour | A format string with a positive and a negative section; font colour by a measure that returns the theme's `good` or `bad` | GA | `create-reports/desktop-custom-format-strings`, `visuals/power-bi-visualization-conditional-formatting` |
| Title and subtitle on every visual, also from a measure | General, Title: title, subtitle, divider, spacing; `fx` for a text measure | GA | `visuals/power-bi-visualization-customize-title-background-and-legend` |
| One bar highlighted, the rest grey | Colour by rule or by a measure | GA | `visuals/power-bi-visualization-conditional-formatting` |
| Target line | Analytics pane: a constant line with a data label | GA | `transform-model/desktop-analytics-pane` |
| Shaded band on a target line | Appears only in the theme schema, not in the format pane | Not confirmed | `transform-model/desktop-analytics-pane` |
| Labels at the end of lines instead of a legend | Line chart, Series labels | GA | `visuals/power-bi-line-chart` |
| Small multiples | Bar, column, line and area charts, up to 6 by 6 | GA | `visuals/power-bi-visualization-small-multiples` |
| Table with data bars, icons and sparklines | Table or matrix: conditional formatting (background, font colour, data bars, icons), sparklines, style presets; a pill in a cell needs an SVG image measure | GA | `create-reports/desktop-conditional-table-formatting`, `create-reports/power-bi-images-tables` |
| Slicers as buttons | Button slicer (horizontal, vertical or grid; states) | GA (since October 2025) | `visuals/power-bi-visualization-button-slicer` |
| List slicer | List slicer | Preview | `visuals/power-bi-visualization-list-slicer` |
| White rounded visuals on a grey canvas | Effects: background, border, radius, shadow; padding; or once in the theme's `visualStyles` | GA | `visuals/power-bi-visualization-format-pane-overview`, `create-reports/report-themes-create-custom` |
| Exact positions | PBIR: each `visual.json` holds x, y, width, height, z and tab order | GA | `developer/projects/projects-report` |
| Phone layout | Mobile layout view; nothing is inherited from the desktop layout | GA | `create-reports/power-bi-create-mobile-optimized-report-mobile-layout-view` |
| Alt text, tab order | General, Alt text (also by measure); Selection pane, Tab order | GA | `create-reports/desktop-accessibility-creating-reports` |
| Bullet chart | No native visual: a bar with a constant line, or an SVG measure | Not native | none |
| Notes drawn on a chart | Not in reports: a text box over the chart, or a constant line's label | Not native | none |
| A message when a filter leaves no data | No built-in message: a measure that returns text into the title or a card | Not confirmed as native | none |

Not confirmed for Power BI: the exact padding, radius and colour values of the Fluent 2 base theme (Learn doesn't publish them), the size limit of an SVG measure, and whether custom fonts can be embedded (a font missing on the viewer's machine falls back). Don't design around the Q&A visual and R or Python visuals (being retired), Bing maps (to be deprecated), or the List slicer and the Gantt visual without a recorded preview choice.

## 4. Microsoft's gates

`powerbi-report-cli` asks for approval of its report plan and design brief before it builds, and `semantic-model-authoring` builds the model. Both come after this plan's sign-off.

`powerbi-report-author` is the CLI that `powerbi-report-cli` documents, and its `validate` checks a report. When the named skill or that CLI isn't installed, the plan says so, and the step's result is "validator: not run". A render-and-measure step then comes right after the first deploy to the Dev workspace: a screenshot of each page, and the text contrast of each visual's title and labels, read from the rendered report with a browser tool when the person is signed in (the page's computed colours, since a scaled screenshot can't give the ratio), otherwise from the person. With neither, the step says "not checked" and why.

Some routes delete by design: `semantic-model-authoring`'s cleanup of same-named models after a retried deploy, fabric-cicd's orphan cleanup (and its `enable_hard_delete` flag), and a deployment pipelines deploy with `allowPurgeData`, which deletes the target's data when the schemas don't match. A plan that uses one makes the delete a step for the person: `powerops-implement` never deletes.

## Sources

Checked on 2026-11-03:

- [Star schema](https://learn.microsoft.com/power-bi/guidance/star-schema), [bi-directional relationships](https://learn.microsoft.com/power-bi/guidance/relationships-bidirectional-filtering), [auto date/time](https://learn.microsoft.com/power-bi/guidance/auto-date-time) and [data reduction](https://learn.microsoft.com/power-bi/guidance/import-modeling-data-reduction).
- [Semantic model modes](https://learn.microsoft.com/power-bi/connect-data/service-dataset-modes-understand): Import is the default.
- [Data types in Power BI](https://learn.microsoft.com/power-bi/transform-model/desktop-data-types): Fixed decimal and Decimal.
- [Incremental refresh](https://learn.microsoft.com/power-bi/connect-data/incremental-refresh-overview).
- [Subscriptions, licenses and trials](https://learn.microsoft.com/power-bi/guidance/powerbi-implementation-planning-subscriptions-licenses-trials) and [scheduled refresh](https://learn.microsoft.com/power-bi/connect-data/refresh-scheduled-refresh): Pro's 1 GB, 8 refreshes, XMLA and pipelines.
- [Row-level security guidance](https://learn.microsoft.com/power-bi/guidance/rls-guidance).
- [Power BI Desktop projects](https://learn.microsoft.com/power-bi/developer/projects/projects-overview), [the report folder](https://learn.microsoft.com/power-bi/developer/projects/projects-report) and [fabric-cicd](https://learn.microsoft.com/power-bi/developer/projects/projects-deploy-fabric-cicd).
- [Design reports for accessibility](https://learn.microsoft.com/power-bi/create-reports/desktop-accessibility-creating-reports) and the [optimization guide](https://learn.microsoft.com/power-bi/guidance/power-bi-optimization): limit the visuals on a page.
- [Power BI embedded system dashboard](https://learn.microsoft.com/power-apps/maker/model-driven-apps/create-edit-powerbi-embedded-page): the environment variable.
- [Power BI agentic overview](https://learn.microsoft.com/power-bi/developer/agentic/power-bi-agentic-overview): the two skills.
- [powerbi-authoring plugin](https://github.com/microsoft/skills-for-fabric/tree/4c3986d/plugins/powerbi-authoring) (MIT) at `4c3986d`: the 12-visual warning in `skills/powerbi-report-cli/references/design/anti-patterns.md`, and the approval gate.

Checked on 2026-11-05:

- [Understand the deployment process](https://learn.microsoft.com/fabric/cicd/deployment-pipelines/understand-the-deployment-process): PBIR reports not supported, and the DirectQuery or composite model with auto date/time or variation tables.
- [The report folder](https://learn.microsoft.com/power-bi/developer/projects/projects-report): PBIR generally available and the default. [Fabric Git integration](https://learn.microsoft.com/fabric/cicd/git-integration/intro-to-git-integration): Report and Semantic model in preview.
- [Power BI Desktop projects](https://learn.microsoft.com/power-bi/developer/projects/projects-overview): sensitivity labels not supported.
- [fabric-cicd](https://learn.microsoft.com/power-bi/developer/projects/projects-deploy-fabric-cicd) and [its optional features](https://microsoft.github.io/fabric-cicd/latest/how_to/optional_feature/): orphan cleanup and `enable_hard_delete`. [Pipelines - Deploy All](https://learn.microsoft.com/rest/api/power-bi/pipelines/deploy-all): `allowPurgeData`.
- `semantic-model-authoring` at the same commit: "delete every duplicate" after a retried deploy.

Checked on 2026-11-08:

- [Visual defaults in Power BI reports](https://learn.microsoft.com/power-bi/create-reports/power-bi-reports-visual-defaults): the Fluent 2 base theme for new reports, 1920 by 1080 new pages, grey backgrounds, padding and rounded corners.
- [Use gridlines and snap-to-grid](https://learn.microsoft.com/power-bi/create-reports/desktop-gridlines-snap-to-grid).
- [Create a card visual](https://learn.microsoft.com/power-bi/visuals/power-bi-visualization-card): callout and reference labels in each card.
- [Create custom report themes](https://learn.microsoft.com/power-bi/create-reports/report-themes-create-custom): the `title`, `largeTitle`, `callout` and `label` text classes.

Checked on 2026-11-11 by the design-research session, and spot-checked on 2026-11-14 for M97 (its plan's Assumptions A9 and A10):

- [Visual defaults in Power BI reports](https://learn.microsoft.com/power-bi/create-reports/power-bi-reports-visual-defaults): new pages 1920 by 1080 under the Fluent 2 defaults, generally available in the August 2026 update.
- [Create a card visual](https://learn.microsoft.com/power-bi/visuals/power-bi-visualization-card): several measures and reference labels in one visual, generally available since November 2025.
- [Button slicer](https://learn.microsoft.com/power-bi/visuals/power-bi-visualization-button-slicer): generally available since October 2025. [List slicer](https://learn.microsoft.com/power-bi/visuals/power-bi-visualization-list-slicer): preview.
- [The Analytics pane](https://learn.microsoft.com/power-bi/transform-model/desktop-analytics-pane): constant lines with a data label. [Small multiples](https://learn.microsoft.com/power-bi/visuals/power-bi-visualization-small-multiples): bar, column, line and area charts, up to 6 by 6.
- [Customize titles, backgrounds and legends](https://learn.microsoft.com/power-bi/visuals/power-bi-visualization-customize-title-background-and-legend) and [conditional formatting](https://learn.microsoft.com/power-bi/visuals/power-bi-visualization-conditional-formatting): the subtitle, a title from a measure, colour by rule.

Checked on 2026-11-12:

- [Use report themes in Power BI](https://learn.microsoft.com/power-bi/create-reports/desktop-report-themes): Microsoft manages the base theme. [Visual defaults in Power BI reports](https://learn.microsoft.com/power-bi/create-reports/power-bi-reports-visual-defaults): Fluent 2, Classic 2026 and Classic 2018.
- [The report folder](https://learn.microsoft.com/power-bi/developer/projects/projects-report): no base theme file or folder is named.
- [Download Power BI Desktop](https://learn.microsoft.com/power-bi/fundamentals/desktop-get-the-desktop): Windows 10 or Windows Server 2016 or later, no other system.
- The powerbi-authoring plugin above, read on its `main` branch: `powerbi-report-author` is the CLI that its `powerbi-report-cli` skill documents, with `powerbi-report-author validate <path>`.
