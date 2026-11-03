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
- **Licence fit.** Pro gives a 1 GB model, 8 scheduled refreshes a day, no XMLA write and no deployment pipelines. More needs Premium Per User or Fabric capacity: put that under Constraints.
- **Security.** Name the route: workspace roles, or row-level security with its roles.
- **Files and route.** The model and report are a Power BI project (PBIP) in Git, with one named deploy route: fabric-cicd, Git integration or deployment pipelines.
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

One 1920 by 1080 page per report page, a box per visual with its title and the fields it shows, and no chart library. A report stores each visual's position, so the boxes map directly; how the charts look can't be promised.

## 4. Microsoft's gates

`powerbi-report-cli` asks for approval of its report plan and design brief before it builds, and `semantic-model-authoring` builds the model. Both come after this plan's sign-off.

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
