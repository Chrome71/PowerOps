---
status: implementing
review: skipped
signed-off: 2026-11-04
signed-off-hash: 19bfc06d8247eab80bc0a03f7abbeaf27042f2be257ebbf1e5f26f9623ddfd7e
environment: null
depth: quick
supersedes: null
---

# Work item modified column: show when an item last changed

## Goal

The list of active work items shows a Modified column, so that the team sees which items have gone quiet. Nothing else about access or the app changes in this release.

Done when:

- The view `Active Work Items` of the table `pops_workitem` shows the column Modified next to Created.
- The security roles and the model-driven app are the same as in version 1.1.0.0.

## Users

- Editors: 12, who use the role `Work Item Editor` in the app `Work Item Desk`.
- Auditors: 3, who use the role `Work Item Auditor`. They read work items and are not given the app.

## Data

| Change | Name | Detail |
|---|---|---|
| Changed view | `Active Work Items` of `pops_workitem` | the column `modifiedon` is added after `createdon`; the filter and the sort stay |
| Existing security role, unchanged | `Work Item Editor` | Read, Write, Append and Append To on `pops_workitem` at business unit depth. No Delete, Assign or Share |
| Existing security role, unchanged | `Work Item Auditor` | Read on `pops_workitem` at business unit depth and no other privilege |
| Version | `PowerOpsSeed` | 1.2.0.0 |

## Automation

None. The flow `Stamp work item status` and the environment variable `pops_DefaultStatus` stay as they are.

## Screens

The view `Active Work Items` gets the extra column. The model-driven app `Work Item Desk` stays as it is in 1.1.0.0: it contains the table `pops_workitem`, and its role map lists the role `Work Item Editor` only. The auditors read through other means and are not in the app.

## Environments and route

Dev (unmanaged), Test (managed) and Prod (managed). The solution is built in Dev and moves to Test and Prod through the pipeline. The operations manager approves the move to Prod.

## Solution and publisher

Solution `PowerOpsSeed`, publisher `powerops` with prefix `pops`. The version goes from 1.1.0.0 to 1.2.0.0.

## Constraints

- Licensing: no new licence is needed.
- Data policy: no connector is added; checked by the platform team for all three environments.
- Connections: unchanged.
- Access: this release adds no access. The roles and the app's role map are the same as in 1.1.0.0.

## Assumptions

- Environment facts were not read from a tenant.
- The column `modifiedon` is a system column that every user of the table can already read.

## Open questions

## Questions for your admin

## Steps

1. Add the column `modifiedon` to the view `Active Work Items`, in its layout and its query.
2. Raise the solution version to 1.2.0.0.
3. Keep the roles and the app as they are.
4. Run `powerops analyze`, `powerops test`, `powerops diff` and `powerops settings validate`.
