---
status: implementing
review: skipped
signed-off: 2026-11-01
signed-off-hash: f4076e5904a6a29e57871be5837d9713e86abbb3f149d7f7c1642ee171e97155
environment: null
depth: quick
supersedes: null
---

# Reassignment notice: tell the supervisors when a work item changes hands

## Goal

When the assignee of a work item changes, the supervisors of the Operations business unit hear about it in their Teams channel, so that nothing is moved without them knowing. Changes in other business units must not reach that channel.

Done when:

- A change of assignee on a work item of the Operations business unit posts `Reassigned: <work item name>` to the supervisors' channel.
- A change of any other column posts nothing.
- A change of assignee in another business unit posts nothing.

## Users

- Supervisors of the Operations business unit: 6, who read the channel.
- Other staff: they change the assignee in the app and see no difference.

## Data

| Change | Name | Detail |
|---|---|---|
| New column | `pops_assignee` | text on Work Item, the supervisor a work item is assigned to; the trigger filters on it, because its column filter does not take a lookup such as the owner |
| New environment variable | `pops_SupervisorChannel` | text, no default; the supervisors' channel for each environment |
| New connection reference | `pops_SeedTeams` | Microsoft Teams, standard connector |
| Version | `PowerOpsSeed` | 1.1.0.0 |

## Automation

One new flow, `Announce reassigned work item`:

1. Trigger: the Dataverse trigger "When a row is added, modified or deleted", change type Update, table Work Item, scope Business Unit, filter column `pops_assignee`.
2. Post the message `Reassigned: <work item name>` to the channel in `pops_SupervisorChannel` through the connection reference `pops_SeedTeams`.

The trigger uses the existing connection reference `pops_SeedDataverse`. The flow is turned on in the export. The flow `Stamp work item status` stays as it is.

## Screens

None.

## Environments and route

Dev (unmanaged), Test (managed) and Prod (managed). The solution is built in Dev and moves to Test and Prod through the pipeline. The operations manager approves the move to Prod.

## Solution and publisher

Solution `PowerOpsSeed`, publisher `powerops` with prefix `pops`. The version goes from 1.0.0.0 to 1.1.0.0.

## Constraints

- Licensing: Teams is a standard connector, so no new licence is needed.
- Data policy: checked by the platform team for all three environments. Dataverse and Microsoft Teams are in the Business group, and no other connector is used.
- Segmentation: the trigger scope is Business Unit, which is relative to the identity behind `pops_SeedDataverse`. That service account sits in the Operations business unit, so the flow only sees rows of the Operations business unit. Other business units get their own flow later if they ask.
- Access: no new role and no new access.

## Assumptions

- Environment facts were not read from a tenant.
- The supervisors' channel exists in each environment.

## Open questions

## Questions for your admin

## Steps

1. Add the column `pops_assignee`, the environment variable `pops_SupervisorChannel` and the connection reference `pops_SeedTeams`.
2. Build the flow `Announce reassigned work item` with the trigger above and turn it on.
3. Write a flow test for it: for the work item `Pump check` the message is `Reassigned: Pump check`, posted to the channel from the variable.
4. Fill in the settings file with the channel and the Teams connection.
5. Raise the solution version to 1.1.0.0.
6. Run `powerops analyze`, `powerops test`, `powerops diff` and `powerops settings validate`.
