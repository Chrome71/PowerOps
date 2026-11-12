# Licences: what needs which, and the standard alternative

Read this when a plan names a connector, table, app, flow, site, report or agent. It fills the `### Licences` table under `Constraints`: section 1 says how, section 2 lists the needs, section 3 maps the part numbers the licence read returns to products.

## 1. The table

`| Need | Product or feature | Who needs it | Standard alternative | Held |`, one row per need of section 2 that the plan has. A need Microsoft 365's standard rights cover gets no row.

`Held` is about the signed-in account only. Other people's licences, test users' included, are the person's answer, in `Who needs it` or under Open questions. Its values:

- `yes (read)`: a part number the read returned maps, in section 3, to a product that covers the row.
- `no (read)`: every part number the read returned is in section 3, and none covers the row.
- `person says yes` or `person says no`: the answer to the neutral question.
- `unknown`: anything else. A part number not in section 3 is `unknown`, never held.

The cost tier, asked in round 1, picks the product of each row:

- **Not sure: standard first** takes the standard alternative wherever a row has one. A row with none keeps its premium product with `Held` read or asked, and becomes an Open question when `Held` is `no (read)` or `person says no`.
- **Standard licences only** takes every standard alternative. A row with none becomes a question: drop the need, or change the tier.
- **Premium where needed** keeps the premium rows, with `Held` still read or asked.

## 2. Needs

| Need | Licence | Standard alternative | Source |
|---|---|---|---|
| A premium, custom or on-premises-gateway connector, or the HTTP action, in a canvas app or cloud flow | Power Apps Premium or per app for the app's users; Power Automate Premium or Process for the flow | Standard connectors, covered by Microsoft 365 | [Power Apps and Power Automate licensing FAQ](https://learn.microsoft.com/power-platform/admin/powerapps-flow-licensing-faq), [license designation](https://learn.microsoft.com/power-apps/maker/canvas-apps/license-designation) |
| Dataverse as a canvas app's data source | Power Apps Premium or per app | A canvas app over a SharePoint list, when `data.md`'s gates hold | [Power Apps licensing FAQ](https://learn.microsoft.com/power-platform/admin/powerapps-licensing-faq) |
| A model-driven app | Power Apps Premium or per app; from February 2027 users without one can't open it | A canvas app over a SharePoint list, when `data.md`'s gates hold | [Power Apps licensing FAQ](https://learn.microsoft.com/power-platform/admin/powerapps-licensing-faq) |
| A Copilot Studio agent | Makers: a Copilot Studio user licence (with a tenant's prepaid Copilot Credit pack), the authors role, a Microsoft 365 Copilot licence, or a trial, which can't publish. Use is billed in Copilot Credits; agents that Microsoft 365 Copilot users use in Copilot Chat, Teams or SharePoint, running as the user, aren't charged for classic answers, generative answers or Graph grounding | None | [Copilot Studio billing and licensing](https://learn.microsoft.com/microsoft-copilot-studio/billing-licensing) |
| Power BI sharing: creating or sharing outside My workspace | Pro or PPU for the creator. Viewers need Pro, PPU or a trial, unless the content is on F64 or larger, or a P SKU, where a free licence with the Viewer role is enough. A PPU workspace needs PPU for every user | None | [Fabric licences](https://learn.microsoft.com/fabric/enterprise/licenses) |
| A Power Pages site | Capacity per site per month (authenticated users, 100 per pack; anonymous users, 500 per pack) or pay-as-you-go; internal users with Power Apps Premium aren't counted. A tenant question: always ask it | None for external users | [Power Apps and Power Automate licensing FAQ](https://learn.microsoft.com/power-platform/admin/powerapps-flow-licensing-faq#power-pages) |
| A Managed Environment | Every active user of its apps and flows, standard apps included: Power Apps Premium, Power Automate Premium, a qualifying Dynamics 365 licence, capacity (per app, Process) or pay-as-you-go; enforced from February 2027. The Developer Plan doesn't cover running assets there | No Managed Environment (`data.md` gate 7) | [Licensing requirements for managed environments](https://learn.microsoft.com/power-platform/admin/managed-environment-licensing) |
| Standard rights: canvas apps and cloud flows with standard connectors, SharePoint included (not model-driven apps) | Microsoft 365, so no row in the table | Itself | [Deep dive on specific licenses](https://learn.microsoft.com/power-platform/admin/power-automate-licensing/deep-dive-on-specific-license), [share a canvas app with guests](https://learn.microsoft.com/power-apps/maker/canvas-apps/share-app-guests) |

Two notes:

- Dataverse for Teams is no alternative: it is deprecated, with no new environments from 2026-11-15 ([Important changes coming](https://learn.microsoft.com/power-platform/important-changes-coming)).
- Unmanaged developer environments, the Developer Plan's included, can't be created from 2026-11-15; managed developer environments remain, one per user ([Important changes coming](https://learn.microsoft.com/power-platform/important-changes-coming)).

## 3. Part numbers

The licence read returns part numbers (`skuPartNumber`). Write the product, never the part number, in the plan. A free, trial or developer plan is listed as itself and covers no row.

| Part number | Product | Covers |
|---|---|---|
| `POWERAPPS_PER_USER` | Power Apps Premium | Premium connectors in apps, Dataverse, model-driven apps, Managed Environment use, for this user |
| `POWERAPPS_PER_APP_NEW` | Power Apps per app | The same, for one app or site |
| `POWERAUTOMATE_ATTENDED_RPA` | Power Automate Premium | Premium connectors in flows, Managed Environment use, for this user |
| `FLOW_PER_USER` | Power Automate per user (legacy) | Premium connectors in flows, for this user |
| `Power_Automate_per_process` | Power Automate Process | Premium connectors in one flow |
| `FLOW_BUSINESS_PROCESS` | Power Automate per flow | Premium connectors in one flow |
| `POWER_BI_PRO` | Power BI Pro | Power BI sharing |
| `PBI_PREMIUM_PER_USER` | Power BI Premium Per User | Power BI sharing |
| `VIRTUAL_AGENT_USL` | Copilot Studio User License | Making agents |
| `POWERAPPS_DEV` | Power Apps Developer Plan | Covers no row: development and test in a developer environment only, not production |
| `POWERAPPS_VIRAL` | Power Apps trial | Covers no row: a trial ends |
| `FLOW_FREE` | Power Automate Free | Covers no row |
| `POWER_BI_STANDARD` | Fabric (free) | Covers no row: viewing content on F64 or larger, or a P SKU, is a capacity question |
| `CCIBOTS_PRIVPREV_VIRAL` | Copilot Studio viral trial | Covers no row: a trial can't publish |

A part number not in this table, a bundle such as Microsoft 365 E3 included, is `unknown`. No part number is listed for Power Pages capacity packs or the Copilot Studio credit pack, so those rows are `unknown` too: ask.

## Sources

Checked on 2026-11-11:

- [Power Apps and Power Automate licensing FAQ](https://learn.microsoft.com/power-platform/admin/powerapps-flow-licensing-faq) and [license designation](https://learn.microsoft.com/power-apps/maker/canvas-apps/license-designation): premium, custom and gateway connectors, the HTTP action, and Power Pages capacity.
- [Power Apps licensing FAQ](https://learn.microsoft.com/power-platform/admin/powerapps-licensing-faq): Dataverse, model-driven apps, February 2027.
- [Copilot Studio billing and licensing](https://learn.microsoft.com/microsoft-copilot-studio/billing-licensing): makers, Copilot Credits, what isn't charged, a trial can't publish.
- [Fabric licences](https://learn.microsoft.com/fabric/enterprise/licenses): Pro, PPU, F64 and P SKUs, the free licence.
- [Licensing requirements for managed environments](https://learn.microsoft.com/power-platform/admin/managed-environment-licensing): every active user, February 2027, the Developer Plan.
- [Deep dive on specific licenses](https://learn.microsoft.com/power-platform/admin/power-automate-licensing/deep-dive-on-specific-license) and [share a canvas app with guests](https://learn.microsoft.com/power-apps/maker/canvas-apps/share-app-guests): Microsoft 365's standard rights.
- [Important changes coming](https://learn.microsoft.com/power-platform/important-changes-coming): Dataverse for Teams, unmanaged and managed developer environments.
- [Power Apps Developer Plan](https://learn.microsoft.com/power-platform/developer/plan): the Developer Plan is for development and test only, not production; the Power Apps trial runs 30 days.
- [Product names and service plan identifiers for licensing](https://learn.microsoft.com/entra/identity/users/licensing-service-plan-reference): the part numbers.
