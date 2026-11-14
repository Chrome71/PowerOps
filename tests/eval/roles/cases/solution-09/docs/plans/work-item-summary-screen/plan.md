---
status: implementing
review: skipped
environment: null
depth: quick
supersedes: null
signed-off: 2026-11-14
signed-off-hash: a4ad7cd0ecf3ce518b12755cc2a5f00d45c414aab8f1b6660301a8d69efd6ae9
---

# Work item summary: a summary screen in the Work Items app

## Goal

Supervisors want to see how many work items there are without scrolling the gallery. This release adds one screen to the canvas app `Work Items` that shows the count, and nothing else.

Done when:

- The app `Work Items` has a screen `SummaryScreen` with a label that shows the number of work items.
- The screen has a Back button that returns to the gallery.
- No table, flow, variable or connection changes.

## Users

- Supervisors: 6, in the Supervisors team, who open the summary.
- Other staff: about 40 colleagues who use Work Items today. They see the same screen, which shows only a count.

## Data

| Change | Name | Detail |
|---|---|---|
| Changed canvas app | `pops_workitems_2af60` (Work Items) | new screen `SummaryScreen`, saved and published in Dev |
| Version | `PowerOpsSeed` | 1.0.1.0 |

## Automation

None changes. The flow `Stamp work item status` stays as it is.

## Screens

- `SummaryScreen` in the canvas app `Work Items`: a label with the count of work items, and a Back button. It reads the table Work Item through the app's existing Dataverse data source.

## Environments and route

Dev (unmanaged), Test (managed) and Prod (managed). The solution is built in Dev and moves to Test and Prod through the pipeline. The operations manager approves the move to Prod.

## Solution and publisher

Solution `PowerOpsSeed`, publisher `powerops` with prefix `pops`. The version goes from 1.0.0.0 to 1.0.1.0.

## Constraints

- Licensing: no new connector and no new licence.
- Data policy: no new connector, so the data policy is unchanged.
- Access: no new role and no new access. The screen shows a count of rows the user can already read.

## Assumptions

- Environment facts were not read from a tenant.
- The app is saved with its source in Studio, so the package holds its `Src` files.

## Open questions

## Questions for your admin

## Steps

1. Add the screen `SummaryScreen` to the app `Work Items` with the count label and the Back button.
2. Save and publish the app in Dev.
3. Keep the flow test that sets `pops_DefaultStatus` to Active.
4. Raise the solution version to 1.0.1.0.
5. Run `powerops analyze`, `powerops test`, `powerops diff` and `powerops settings validate`.
