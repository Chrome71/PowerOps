---
status: implementing
review: skipped
signed-off: 2026-11-01
signed-off-hash: bdfef3e3fc0d8d738c734e7031096ea3eb7099f0a3603659f002ffb468b6324c
environment: null
depth: quick
supersedes: null
---

# Stale item escalation: tell the on-call board about work items left open

## Goal

Work items that stay open for more than 14 days are counted once a day and reported to the on-call board, so that a supervisor can reassign them. This release adds that report and the role the supervisors use.

Done when:

- At 06:00 UTC every day the board receives the number of work items that have been open for more than 14 days.
- A supervisor can read and edit the work items of their business unit, and cannot delete, assign or share them.
- The board's address and token are not stored in any flow or table.

## Users

- Supervisors: 6, who get the new role `Work Item Supervisor`.
- The on-call board: an external status page that receives a JSON post. It is not a person and has no Dataverse access.

## Data

| Change | Name | Detail |
|---|---|---|
| New environment variable | `pops_OnCallBoardUrl` | text, no default; the board's address for each environment |
| New environment variable | `pops_OnCallBoardToken` | Secret, no default; the settings file holds a reference to the organisation's vault for each environment |
| New security role | `Work Item Supervisor` | Read, Write, Append and Append To on `pops_workitem` at business unit depth. No Delete, Assign or Share |
| Version | `PowerOpsSeed` | 1.1.0.0 |

## Automation

One new flow, `Escalate stale work items`:

1. Trigger: every day at 06:00 UTC.
2. List the work items with `statecode` 0 that were created more than 14 days ago.
3. If there is at least one, post `{"staleCount": <number>}` to the board with the HTTP connector. The address is `pops_OnCallBoardUrl`, and the credential travels in the `Authorization` header from `pops_OnCallBoardToken`.

It uses the existing connection reference `pops_SeedDataverse` and is turned on in the export. Nothing else changes: the flow `Stamp work item status` stays as it is.

## Screens

None.

## Environments and route

Dev (unmanaged), Test (managed) and Prod (managed). The solution is built in Dev and moves to Test and Prod through the pipeline. The operations manager approves the move to Prod.

## Solution and publisher

Solution `PowerOpsSeed`, publisher `powerops` with prefix `pops`. The version goes from 1.0.0.0 to 1.1.0.0.

## Constraints

- Licensing: the HTTP connector is premium. The service account that owns the flow holds a Power Automate Premium per-user licence, paid from the operations budget.
- Data policy: checked by the platform team for all three environments. Dataverse and HTTP are in the Business group, and no other connector is used.
- Secrets: the board token is a Secret environment variable. Its value in the settings file is a reference to the vault, never the token itself.
- Access: the new role is the only new access. Nobody gets System Administrator in Test or Prod because of this release.

## Assumptions

- Environment facts were not read from a tenant.
- The board accepts a JSON post and a bearer credential.

## Open questions

## Questions for your admin

## Steps

1. Add the two environment variables.
2. Add the security role `Work Item Supervisor` with the privileges above.
3. Build the flow `Escalate stale work items` and turn it on.
4. Write a flow test for it: with two stale items, the post carries `staleCount` 2 and an `Authorization` header built from the token variable.
5. Fill in the settings file: the board's address, and the vault reference for the token.
6. Raise the solution version to 1.1.0.0.
7. Run `powerops analyze`, `powerops test`, `powerops diff` and `powerops settings validate`.
