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

One 1920 by 1080 page per report page, each visual drawn in CSS in its place with its title and the fields it shows, and no chart library. A report stores each visual's position, so the places map directly; how the charts look can't be promised. New pages under the Fluent 2 base theme are 1920 by 1080 by default, with a grey background and padded, rounded visuals. The page follows the look in `look.md`, as a report:

- **Header band.** A band across the top, about 96 px high: the report title, a subtitle with the page's question and the data's as-of date, and room for the page navigator when there is more than one page.
- **Grid.** 24 px outer margins and 16 or 24 px gutters on 12 columns. Every visual's edges sit on the grid, and visuals in a row share their top and height. Build with gridlines and snap to grid on.
- **KPI cards with context.** A row of 3 to 5 cards under the header: the value, its label, and a reference label with the comparison (target, prior period or change) in words and with its sign, never colour alone.
- **Slicers.** In the same place and order on every page: a row under the header band, a left rail, or the filter pane.
- **Reading order.** The KPIs, then the trend, then the breakdown, then the detail. The visual that answers the page's question is the largest.
- **Type.** The theme's text classes carry the type scale: `title` and `largeTitle` for titles, `callout` for card values, `label` for the rest.
- **Boxes.** A stand-in shape in each box (a few bars or a line drawn with `div`s) so each visual's weight shows.
- **Base theme.** Learn says Microsoft manages a report's base theme and names Fluent 2, Classic 2026 and Classic 2018, but gives neither the base theme file's name nor its content: those are seen only in what Power BI Desktop saves (observed in exports, not on Learn), and Desktop runs on Windows only. So on a machine without Desktop a plan doesn't build over a named base theme, and its custom theme sets the colour of every text class: observed in one run (2026-11-11), contrast couldn't be measured until the report rendered.

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

Checked on 2026-11-12:

- [Use report themes in Power BI](https://learn.microsoft.com/power-bi/create-reports/desktop-report-themes): Microsoft manages the base theme. [Visual defaults in Power BI reports](https://learn.microsoft.com/power-bi/create-reports/power-bi-reports-visual-defaults): Fluent 2, Classic 2026 and Classic 2018.
- [The report folder](https://learn.microsoft.com/power-bi/developer/projects/projects-report): no base theme file or folder is named.
- [Download Power BI Desktop](https://learn.microsoft.com/power-bi/fundamentals/desktop-get-the-desktop): Windows 10 or Windows Server 2016 or later, no other system.
- The powerbi-authoring plugin above, read on its `main` branch: `powerbi-report-author` is the CLI that its `powerbi-report-cli` skill documents, with `powerbi-report-author validate <path>`.
