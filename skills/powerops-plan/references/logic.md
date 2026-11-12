# Logic: where each rule lives, the state model and approvals

Read this when a plan names a business rule, a status column, an approval or a flow that writes (`data.md`, `route.md`). Write the answers in `Data` and `Automation`. These are PowerOps's planning rules; the sources say what Learn states.

## 1. Where it lives

| Need | Place | Why |
|---|---|---|
| A rule that must hold on every write, not only on a form or in one app | A business rule with table (Entity) scope for a simple check, otherwise a synchronous plug-in | Table scope runs on the server as well as on forms; a form-scope rule runs only on forms, and a canvas app runs only table-scope rules |
| A rule that keeps a record valid at create: a starting status, or columns only a later role fills | At the write: a synchronous plug-in on Create for a starting status, because column security can't secure `statecode` or `statuscode` and status reason transitions are documented only for changes; column security with no Create for that role on a column only a later role fills (not a lookup or the primary name) | A flow on the save runs after the row is stored and rolls nothing back: it reports or repairs, a second line, not the control. System administrators bypass column security, and a caller with `prvBypassCustomBusinessLogic` can skip synchronous plug-ins |
| Show, hide, lock or require a column on a form | A form-scope business rule, or Power Fx in the canvas app | These actions apply only to forms |
| A value derived from the row or its parent, always current | A formula column | It can't trigger workflows or plug-ins, and shows no value in mobile offline |
| A total or count over child rows | A rollup column if a value refreshed by a scheduled job is enough, otherwise a plug-in | Rollups are calculated asynchronously, by default at most 50 a table and 200 an environment (the developer page on specialized columns still gives 10 and 100) |
| Changes that succeed or fail together | A plug-in step in PreOperation or PostOperation, inside the transaction; in a flow, a changeset request | Asynchronous steps run outside the transaction. A changeset holds only add, update and delete row actions, and nothing inside it reads an earlier action's output |
| Side effects after the save: notify, call another system, create follow-up work | An automated cloud flow on the Dataverse trigger, with filter columns and a failure path (`data.md`, section 5) | PO012 checks the filter columns |
| A record that moves through named states | Status reasons with status reason transitions, and the card in section 2 | With `EnforceStateTransitions` on, a change to a status reason the list doesn't allow fails |
| A human approval | The Approvals connector with an expiration and a timeout branch (section 3) | A run ends after 30 days, and a pending approval with it |

## 2. The state-model card

One card per status column, under `Data` after the ledger:

- The table and the column.
- Each state with its status reasons.
- Each allowed transition, and who or what moves it: a person on which screen, a flow or a plug-in.
- The status reason a row starts in, and what refuses a create with another one.
- Where the transitions are enforced: status reason transitions with `EnforceStateTransitions`, or the plug-in that checks them. A transition enforced only in an app or on a form doesn't hold for a flow or an API call.
- How the history of each change is kept: auditing on the column, or a log table.

## 3. Lines in each flow card

- **Expiration.** Each approval names its expiration and what the timeout branch does. The action's Timeout setting takes an ISO 8601 duration, such as `P7D`.
- **Longer than 30 days.** Keep the approval in Dataverse: one flow sends it (Create an approval (v2)), and a second flow acts on the response.
- **Retries.** Each write that a retry or a resubmitted run repeats is safe to repeat: an alternate key with an upsert, or a check before the write. Say which.
- **Together or apart.** Changes that must all succeed run in one transaction (section 1), not as separate steps of a flow.

## Sources

Checked on 2026-11-05:

- [Create a business rule for a table](https://learn.microsoft.com/power-apps/maker/data-platform/data-platform-create-business-rule): the scopes, canvas apps on table scope, and the form-only actions.
- [Formula columns](https://learn.microsoft.com/power-apps/maker/data-platform/formula-columns): no workflows or plug-ins, and no value in mobile offline.
- [Define rollup columns](https://learn.microsoft.com/power-apps/maker/data-platform/define-rollup-fields): asynchronous, and the default limits of 50 a table and 200 an environment. [Specialized columns using code](https://learn.microsoft.com/power-apps/developer/data-platform/specialized-columns#rollup-columns) gives 10 and 100.
- [Event framework](https://learn.microsoft.com/power-apps/developer/data-platform/event-framework): PreOperation and PostOperation in the transaction, asynchronous steps outside it.
- [Perform a changeset request](https://learn.microsoft.com/power-automate/dataverse/change-set): all or nothing, the three actions, and no earlier outputs inside.
- [Define custom state model transitions](https://learn.microsoft.com/power-apps/developer/data-platform/define-custom-state-model-transitions): `EnforceStateTransitions`. It doesn't say the check covers every write path, so the card asks where it is enforced.
- [Limits and configuration](https://learn.microsoft.com/power-automate/limits-and-config): runs end after 30 days.
- [Modern approvals](https://learn.microsoft.com/power-automate/modern-approvals): approvals longer than 30 days in Dataverse, with two flows.
- [Error reference](https://learn.microsoft.com/power-automate/error-reference): an expiration and a timeout branch for an approval, and Timeout as an ISO 8601 duration.

Checked on 2026-11-12:

- [Column-level security to control access](https://learn.microsoft.com/power-platform/admin/field-level-security): separate Read, Read unmasked, Update and Create permissions; `statecode`, `statuscode`, lookup, formula and primary name columns can't be secured; system administrators aren't limited by it.
- [Web service error codes](https://learn.microsoft.com/power-apps/developer/data-platform/reference/web-service-error-codes): `AttributePrivilegeCreateIsMissing` when a create sets a secured column without Create.
- [Handle exceptions in plug-ins](https://learn.microsoft.com/power-apps/developer/data-platform/handle-exceptions): an exception in a synchronous step cancels and rolls back the operation.
- [Bypass custom Dataverse logic](https://learn.microsoft.com/power-apps/developer/data-platform/bypass-custom-business-logic): a caller with `prvBypassCustomBusinessLogic`, by default system administrators, can skip custom synchronous logic.
- [Understand callback registration for Dataverse triggers](https://learn.microsoft.com/power-automate/dataverse/powerautomate-callbackregistration-flow): the trigger runs asynchronously, outside the transaction, so a flow failure rolls nothing back.
- [Define custom state model transitions](https://learn.microsoft.com/power-apps/developer/data-platform/define-custom-state-model-transitions): transitions are described for changes to an existing row; no rule is given for the status reason at create.
