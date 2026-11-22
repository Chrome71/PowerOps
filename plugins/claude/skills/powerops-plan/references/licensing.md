# Licences: what needs which, and the standard alternative

Read this when a plan names a connector, table, app, flow, site, report or agent. It fills the `### Licences` table under `Constraints`: section 1 says how, section 2 lists the needs, section 3 maps the part numbers the licence read returns to products.

## 1. The table

`| Need | Product or feature | Who needs it | Standard alternative | Held |`, one row per need of section 2 that the plan has. A need Microsoft 365's standard rights cover gets no row.

For an existing solution, `powerops inventory` fills the Need column: `http-action` and `custom-connector` are row 1 of section 2, `dataverse-in-canvas-app` row 2, `model-driven-app` row 3, `desktop-flow-call` the desktop-flow row, `agent-flow` the agent-flow row and `ai-builder-action` the AI Builder row. A connector under `notClassified` gets a row only once its reference page says premium. A solution with no driver may still need a premium licence, and a `notRead` part is not checked.

`Held` is about the signed-in account only. Other people's licences, test users' included, are the person's answer, in `Who needs it` or under Open questions. Its values:

- `yes (read)`: a part number the read returned maps, in section 3, to a product that covers the row.
- `no (read)`: every part number the read returned is in section 3, and none covers the row.
- `person says yes` or `person says no`: the answer to the neutral question.
- `unknown`: anything else. A part number not in section 3 is `unknown`, never held.

The Developer Plan covers the maker in Dev only. When the person says Dev is an unmanaged developer environment, a row it covers gets `Held` `yes (read)` with `Who needs it` saying `the maker, in Dev only`, and the row's other users, Test and Prod's included, go in `Who needs it` or under Open questions. When Dev is managed, or its kind isn't said, the Developer Plan makes no row `yes (read)`.

The cost tier, asked in round 1, picks the product of each row:

- **Not sure: standard first** takes the standard alternative wherever a row has one. A row with none keeps its premium product with `Held` read or asked, and becomes an Open question when `Held` is `no (read)` or `person says no`.
- **Standard licences only** takes every standard alternative. A row with none becomes a question: drop the need, or change the tier.
- **Premium where needed** keeps the premium rows, with `Held` still read or asked.

## 2. Needs

| Need | Licence | Standard alternative | Source |
|---|---|---|---|
| A premium, custom or on-premises-gateway connector, or the HTTP action, in a canvas app or cloud flow | Power Apps Premium or per app for the app's users; Power Automate Premium or Process for the flow | Standard connectors, covered by Microsoft 365 | [Power Platform licensing FAQs](https://learn.microsoft.com/power-platform/admin/powerapps-flow-licensing-faq), [license designation](https://learn.microsoft.com/power-apps/maker/canvas-apps/license-designation) |
| Dataverse as a canvas app's data source | Power Apps Premium or per app | A canvas app over a SharePoint list, when `data.md`'s gates hold | [Power Apps licensing FAQ](https://learn.microsoft.com/power-platform/admin/powerapps-licensing-faq) |
| A model-driven app | Power Apps Premium or per app; from February 2027 users without one can't open it | A canvas app over a SharePoint list, when `data.md`'s gates hold | [Power Apps licensing FAQ](https://learn.microsoft.com/power-platform/admin/powerapps-licensing-faq) |
| A cloud flow that runs a desktop flow | Power Automate Premium for the connection's user; an unattended run also needs a Process licence or the unattended RPA add-on | None | [Desktop flows](https://learn.microsoft.com/connectors/uiflow/), [Types of Power Automate licenses](https://learn.microsoft.com/power-platform/admin/power-automate-licensing/types) |
| An agent flow (a Copilot Studio flow) | Billed in Copilot Credits, 13 per 100 actions, each trigger run counted as one action; no Power Automate licence, and premium connectors are allowed; not under Power Platform request limits. When the environment has no credits, new runs are blocked with `InsufficientMcsCredits` and runs in progress finish. Not charged: test runs, and runs through the "When an agent calls the flow" trigger while a signed-in user with a Microsoft 365 Copilot licence uses the agent, but not when the agent runs on its own. It can't call a desktop flow | A cloud flow under Microsoft 365's rights, when its connectors are standard | [Billing rates and management](https://learn.microsoft.com/microsoft-copilot-studio/requirements-messages-management), [Agent flows overview](https://learn.microsoft.com/microsoft-copilot-studio/flows-overview), [Agent flows FAQ](https://learn.microsoft.com/microsoft-copilot-studio/flows-faqs) |
| An AI Builder action in an app or flow | AI Builder credits first, then Copilot Credits, then blocked; in an agent flow, Copilot Credits only. Seeded and add-on AI Builder credits reach end of life on 2026-11-01 (end of sales of the add-ons was 2025-11-01), existing credits last to the end of their contract, and AI Builder trials ended. An app that uses it is premium. For a flow, Learn disagrees: [Overview of licensing](https://learn.microsoft.com/ai-builder/administer-licensing) (2026-09-09) says a flow with any AI Builder action is premium, [Licensing and AI Builder credits](https://learn.microsoft.com/ai-builder/credit-management) (2026-10-09) says the action doesn't make it premium, and [Types of Power Automate licenses](https://learn.microsoft.com/power-platform/admin/power-automate-licensing/types) (2026-09-22) still lists 5,000 AI Builder credits a month with Premium, without the end date. So plan for premium and for credits | None | [End of AI Builder credits](https://learn.microsoft.com/ai-builder/endofaibcredits), [Licensing and AI Builder credits](https://learn.microsoft.com/ai-builder/credit-management), [Overview of licensing](https://learn.microsoft.com/ai-builder/administer-licensing) |
| A Copilot Studio agent | Makers: a Copilot Studio user licence (with a tenant's prepaid Copilot Credit pack), the authors role, a Microsoft 365 Copilot licence, or a trial, which can't publish. Use is billed in Copilot Credits; agents that Microsoft 365 Copilot users use in Copilot Chat, Teams or SharePoint, running as the user, aren't charged for classic answers, generative answers or Graph grounding | None | [Copilot Studio billing and licensing](https://learn.microsoft.com/microsoft-copilot-studio/billing-licensing) |
| Power BI sharing: creating or sharing outside My workspace | Pro or PPU for the creator. Viewers need Pro, PPU or a trial, unless the content is on F64 or larger, or a P SKU, where a free licence with the Viewer role is enough. A PPU workspace needs PPU for every user | None | [Fabric licences](https://learn.microsoft.com/fabric/enterprise/licenses) |
| A Power Pages site | Capacity per site per month (authenticated users, 100 per pack; anonymous users, 500 per pack) or pay-as-you-go; internal users with Power Apps Premium aren't counted. A tenant question: always ask it | None for external users | [Power Platform licensing FAQs](https://learn.microsoft.com/power-platform/admin/powerapps-flow-licensing-faq#power-pages) |
| A Managed Environment | Every active user of its apps and flows, standard apps included: Power Apps Premium, Power Automate Premium, a qualifying Dynamics 365 licence, capacity (per app, Process) or pay-as-you-go; enforced from February 2027. The Developer Plan doesn't cover running assets there | No Managed Environment (`data.md` gate 7) | [Licensing requirements for managed environments](https://learn.microsoft.com/power-platform/admin/managed-environment-licensing) |
| Standard rights: canvas apps and cloud flows with standard connectors, SharePoint included (not model-driven apps) | Microsoft 365, so no row in the table | Itself | [Deep dive on specific licenses](https://learn.microsoft.com/power-platform/admin/power-automate-licensing/deep-dive-on-specific-license), [share a canvas app with guests](https://learn.microsoft.com/power-apps/maker/canvas-apps/share-app-guests) |

Three notes:

- Dataverse for Teams is no alternative: it is deprecated, with no new environments from 2026-11-15 ([Important changes coming](https://learn.microsoft.com/power-platform/important-changes-coming)).
- Unmanaged developer environments, the Developer Plan's included, can't be created from 2026-11-15, and existing ones are suspended from 2026-12-15 and deleted from 2027-01-15, which `Constraints` records when Dev is one; managed developer environments remain, one per user, and running apps and flows there needs a premium licence on top of the plan ([Important changes coming](https://learn.microsoft.com/power-platform/important-changes-coming), [Licensing requirements for managed environments](https://learn.microsoft.com/power-platform/admin/managed-environment-licensing)).
- The Developer Plan's limits: 750 flow runs a month and 2 GB of database, not raised by add-ons ([Power Apps Developer Plan](https://learn.microsoft.com/power-platform/developer/plan)). The page doesn't say whether they hold in a managed developer environment. When Dev is a developer environment, both go under `Constraints`, and the plan's flows' expected runs a day, summed, times 30 are checked against 750.

## 3. Part numbers

The licence read returns part numbers (`skuPartNumber`). Write the product, never the part number, in the plan. A free or trial plan is listed as itself and covers no row; the Developer Plan's row says what it covers.

| Part number | Product | Covers |
|---|---|---|
| `POWERAPPS_PER_USER` | Power Apps Premium | Premium connectors in apps, Dataverse, model-driven apps, Managed Environment use, for this user |
| `POWERAPPS_PER_APP_NEW` | Power Apps per app | The same, for one app or site |
| `POWERAUTOMATE_ATTENDED_RPA` | Power Automate Premium | Premium connectors in flows, attended desktop flows, Managed Environment use, for this user |
| `FLOW_PER_USER` | Power Automate per user (legacy) | Premium connectors in flows, for this user |
| `Power_Automate_per_process` | Power Automate Process | Premium connectors in one flow |
| `FLOW_BUSINESS_PROCESS` | Power Automate per flow | Premium connectors in one flow |
| `POWER_BI_PRO` | Power BI Pro | Power BI sharing |
| `PBI_PREMIUM_PER_USER` | Power BI Premium Per User | Power BI sharing |
| `VIRTUAL_AGENT_USL` | Copilot Studio User License | Making agents |
| `POWERAPPS_DEV` | Power Apps Developer Plan | For the maker's development and test in an unmanaged developer environment only: Dataverse, premium and custom connectors, the on-premises gateway; not dataflows, business process flows, desktop flows, AI Builder or Dynamics 365 apps; in a managed developer environment no row; never production |
| `POWERAPPS_VIRAL` | Power Apps trial | Covers no row: a trial ends |
| `FLOW_FREE` | Power Automate Free | Covers no row |
| `POWER_BI_STANDARD` | Fabric (free) | Covers no row: viewing content on F64 or larger, or a P SKU, is a capacity question |
| `CCIBOTS_PRIVPREV_VIRAL` | Copilot Studio viral trial | Covers no row: a trial can't publish |

A part number not in this table, a bundle such as Microsoft 365 E3 included, is `unknown`. No part number is listed for Power Pages capacity packs or the Copilot Studio credit pack, so those rows are `unknown` too: ask.

## Sources

Checked on 2026-11-11:

- [Power Platform licensing FAQs](https://learn.microsoft.com/power-platform/admin/powerapps-flow-licensing-faq) and [license designation](https://learn.microsoft.com/power-apps/maker/canvas-apps/license-designation): premium, custom and gateway connectors, the HTTP action, and Power Pages capacity.
- [Power Apps licensing FAQ](https://learn.microsoft.com/power-platform/admin/powerapps-licensing-faq): Dataverse, model-driven apps, February 2027.
- [Copilot Studio billing and licensing](https://learn.microsoft.com/microsoft-copilot-studio/billing-licensing): makers, Copilot Credits, what isn't charged, a trial can't publish.
- [Fabric licences](https://learn.microsoft.com/fabric/enterprise/licenses): Pro, PPU, F64 and P SKUs, the free licence.
- [Licensing requirements for managed environments](https://learn.microsoft.com/power-platform/admin/managed-environment-licensing): every active user, February 2027, the Developer Plan.
- [Deep dive on specific licenses](https://learn.microsoft.com/power-platform/admin/power-automate-licensing/deep-dive-on-specific-license) and [share a canvas app with guests](https://learn.microsoft.com/power-apps/maker/canvas-apps/share-app-guests): Microsoft 365's standard rights.
- [Important changes coming](https://learn.microsoft.com/power-platform/important-changes-coming): Dataverse for Teams, unmanaged and managed developer environments.
- [Power Apps Developer Plan](https://learn.microsoft.com/power-platform/developer/plan): the Developer Plan is for development and test only, not production; the Power Apps trial runs 30 days.
- [Product names and service plan identifiers for licensing](https://learn.microsoft.com/entra/identity/users/licensing-service-plan-reference): the part numbers.

Checked on 2026-11-12:

- [Power Platform licensing FAQs](https://learn.microsoft.com/power-platform/admin/powerapps-flow-licensing-faq): the page's title, which the links above use.

Checked on 2026-11-15:

- [Power Apps Developer Plan](https://learn.microsoft.com/power-platform/developer/plan): what the Developer Plan covers, for development and test only, sharing for them included, never production; no dataflows; no AI Builder or RPA use rights without a trial; 750 flow runs a month and 2 GB of database, not raised by add-ons.
- [Deep dive on specific licenses](https://learn.microsoft.com/power-platform/admin/power-automate-licensing/deep-dive-on-specific-license): on the Developer Plan, no business process flows or desktop flows.
- [Important changes coming](https://learn.microsoft.com/power-platform/important-changes-coming): no new unmanaged developer environments from 2026-11-15, existing ones suspended from 2026-12-15 and deleted from 2027-01-15, managed developer environments remain, one per user.
- [Licensing requirements for managed environments](https://learn.microsoft.com/power-platform/admin/managed-environment-licensing): running apps and flows in a managed developer environment needs a premium licence on top of the Developer Plan.
- [Desktop flows](https://learn.microsoft.com/connectors/uiflow/) and [Types of Power Automate licenses](https://learn.microsoft.com/power-platform/admin/power-automate-licensing/types): the desktop-flow row, as `powerops inventory`'s `desktop-flow-call` driver gives it (its sources, read for M107 on 2026-11-12).

Checked on 2026-11-22:

- [Billing rates and management](https://learn.microsoft.com/microsoft-copilot-studio/requirements-messages-management): agent flows bill 13 Copilot Credits per 100 actions, each trigger run one action; test runs and runs through "When an agent calls the flow" while a signed-in user with a Microsoft 365 Copilot licence uses the agent aren't charged, and the exemption doesn't hold when the agent runs on its own. It is a source of `powerops inventory`'s `agent-flow` driver.
- [Agent flows overview](https://learn.microsoft.com/microsoft-copilot-studio/flows-overview) and [Copilot Studio usage limit and agent unavailable errors](https://learn.microsoft.com/troubleshoot/power-platform/copilot-studio/licensing/throttling-errors-agents): with no credits in the environment, new agent flow runs are blocked (`InsufficientMcsCredits`) and runs in progress finish. The overview is a source of the `agent-flow` driver.
- [Agent flows FAQ](https://learn.microsoft.com/microsoft-copilot-studio/flows-faqs): an agent flow needs no Power Automate licence and may use premium connectors, and can't call a desktop flow. It is a source of the `agent-flow` driver.
- [Requests limits and allocations](https://learn.microsoft.com/power-platform/admin/api-request-limits-allocations): agent flows aren't under Power Platform request limits.
- [End of AI Builder credits](https://learn.microsoft.com/ai-builder/endofaibcredits): end of sales of the capacity add-ons 2025-11-01; end of life of the add-ons and of seeded credits 2026-11-01; existing credits last to contract end; apps and flows use AI Builder credits, then Copilot Credits, then are blocked. It is a source of `powerops inventory`'s `ai-builder-action` driver.
- [Licensing and AI Builder credits](https://learn.microsoft.com/ai-builder/credit-management) (2026-10-09): an AI Builder action doesn't make a flow premium; seeded credits are removed on 2026-11-01; an app using AI Builder is premium. It is a source of the `ai-builder-action` driver.
- [Overview of licensing](https://learn.microsoft.com/ai-builder/administer-licensing) (2026-09-09): a Power Automate flow that uses any AI Builder action is premium; agents and agent flows use Copilot Credits only; AI Builder trials are discontinued. It is a source of the `ai-builder-action` driver.
- [Types of Power Automate licenses](https://learn.microsoft.com/power-platform/admin/power-automate-licensing/types) (2026-09-22): still lists 5,000 AI Builder credits a month with Premium, Process and Hosted Process, without the end date.
