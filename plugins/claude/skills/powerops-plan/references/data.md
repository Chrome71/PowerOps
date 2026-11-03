# Data: the store, the gates, table design and the flow card

Read this when a plan names new data (a table, a list or a store) or a flow. It fills the ledger in `Data` and the cards in `Automation`, and section 6 holds the data round's questions.

## 1. The store

First list every constraint the data carries, before any choice:

- It can't move, or SQL is mandated.
- It needs a model-driven app, Power Pages, native offline, row-level security roles, column security or enforced relationships.

Then the route:

1. Data that has a system of record stays there. Reach it through an integration that meets the constraints: a connector, a virtual table for external data that stays external, or a copy with a stated reason.
2. New data under a SQL mandate goes to Azure SQL, with Microsoft Entra sign-in and row-level security on the server.
3. New data under any other constraint goes to Dataverse.
4. New data under none goes to a SharePoint list only when every gate in section 2 holds. Otherwise Dataverse.

Constraints that conflict (a SQL mandate and native offline, say) go under Open questions for the person. Don't settle them by taking the first branch that fits.

An elastic table only for high-volume data that needs no complex joins, cross-table transactions or many-to-many relationships. A virtual table only for external data that stays external. Dataverse for Teams never: no new environments from 2026-11-15, and it is retired in 2027.

## 2. The SharePoint gates

A list is the store only when all eight hold. In the list's `Store` cell, say that they hold, or give the reason for each one that doesn't.

1. **Rows.** Under 5,000 at three years, or every view and query filters on an indexed column and returns at most 5,000 items. An index helps only when the filtered result is that small. Add the indexes before the list reaches 20,000 items, the limit for adding or removing one.
2. **Delegation.** Every app query delegates. A canvas app fetches at most its data row limit (500 by default, up to 10,000) for a query that doesn't.
3. **Few lookups.** A query joins at most 12 lookup, person or workflow status columns, SharePoint's list view lookup threshold.
4. **Small flow volume.** Get items returns 100 items by default and at most 5,000 a call. A filtered Get items without Pagination returns nothing when no match is in the first 5,000 items. The SharePoint connector allows 600 calls per connection every 60 seconds, shared by every flow that uses the connection.
5. **Permissions.** Site or list permission is enough: no row-level or column security.
6. **No native offline.** Canvas offline mode doesn't support SharePoint.
7. **A real licence saving.** In a Managed Environment every user of an app needs a qualifying licence, standard apps included, and from February 2027 users without one can't open the app.
8. **An owner for the schema.** Lists sit outside the solution, and SharePoint doesn't fully support ALM, so someone owns each list's columns in every environment.

## 3. Table design

- Reuse, then extend, then create. A Create names the existing tables, lists or standard tables it checked.
- A local choice unless the values are shared. A lookup table only when its values have attributes, an owner, security or business editing.
- Normalize facts, then stop.
- State the ownership of each table (user or team, or organization): it can't change after the table is created.
- Keep primary names free of sensitive data.
- One publisher and prefix before any metadata.
- State audit and retention for each table.

## 4. The ledger

| Column | What it holds |
|---|---|
| Artefact | The table or list, with the prefix for a Dataverse table |
| Kind | Table, elastic table, virtual table, list, or a connector to a system of record |
| Store | Dataverse, SharePoint, Azure SQL or the system of record; for a list, the gates (section 2) |
| Goal line | The line of `Goal` that would fail without it |
| Reuse, Extend or Create | Which one, and for a Create what was checked first |
| Why not a column or choice | Why this needs a table or list of its own |
| Rows now, 3 years | Two numbers from the data round, or an Assumption |

## 5. The flow card

One card per flow under `Automation`:

- Goal line, and the existing flow it reuses or why none does
- Trigger, and expected runs a day
- Rows per run, and the filter
- Numbered steps, with an estimated action count and nesting depth
- Each connector or tool: premium or standard, its connection reference, its rate limit
- Failure path
- Retry and concurrency, only if not the default
- How it is tested

For cost, a reference line, not a required one: actions a day = runs a day x (1 + straight actions + for each loop, 1 + inner actions x items), against the daily action limit of the owner's licence. Every trigger and action counts, and an action inside a loop counts once per item.

## 6. The data round

Ask these when the plan names new data, recommended answer first, and skip what the request or the facts already answer. `powerops-plan` section 6 says how they fill the rounds.

1. Where does the data live today? Connect to it; don't copy it.
2. How many rows in the largest table now and in three years, and what is the widest filter?
3. Who sees which rows?
4. Which existing table, list or standard table covers each thing named?
5. Is offline needed, and is the environment a Managed Environment?
6. What is out of this version? (the `Won't build` list)

## Sources

Checked on 2026-11-03:

- [List view threshold](https://learn.microsoft.com/troubleshoot/sharepoint/lists-and-libraries/items-exceeds-list-view-threshold) and [filtering on an indexed column](https://learn.microsoft.com/troubleshoot/sharepoint/lists-and-libraries/fails-filtering-sharepoint-column): 5,000 items, 20,000 for an index.
- [Delegation and query limits](https://learn.microsoft.com/power-apps/maker/canvas-apps/delegation-overview): the data row limit.
- [SharePoint Online list (Power Query)](https://learn.microsoft.com/power-query/connectors/sharepoint-online-list): 12 joins in a query, the list view lookup threshold.
- [Get items and Get files](https://learn.microsoft.com/sharepoint/dev/business-apps/power-automate/guidance/working-with-get-items-and-get-files): 100, 5,000 and Pagination.
- [SharePoint connector](https://learn.microsoft.com/connectors/sharepointonline/): 600 calls per connection every 60 seconds.
- [Power Automate licensing FAQ](https://learn.microsoft.com/power-platform/admin/power-automate-licensing/faqs): what counts as an action, loops included.
- [Mobile offline limitations for canvas apps](https://learn.microsoft.com/power-apps/mobile/limitations-canvas-apps): no SharePoint offline.
- [Licensing requirements for managed environments](https://learn.microsoft.com/power-platform/admin/managed-environment-licensing): every user, February 2027.
- [Data source references based on environment variables](https://learn.microsoft.com/troubleshoot/power-platform/dataverse/working-with-solutions/dataverse-environment-variables): SharePoint and ALM.
- [Important changes coming](https://learn.microsoft.com/power-platform/important-changes-coming): Dataverse for Teams.
- [Elastic tables](https://learn.microsoft.com/power-apps/maker/data-platform/create-edit-elastic-tables), [virtual tables](https://learn.microsoft.com/power-apps/maker/data-platform/create-edit-virtual-entities) and [types of tables](https://learn.microsoft.com/power-apps/maker/data-platform/types-of-entities): when to use each, and ownership.
- [Use SQL Server securely with Power Apps](https://learn.microsoft.com/power-apps/maker/canvas-apps/connections/sql-server-security): Entra sign-in and row-level security.
