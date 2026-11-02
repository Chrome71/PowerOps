---
status: implementing
review: skipped
signed-off: 2026-11-01
signed-off-hash: be7f2d9719da6196a66de88c8741f6b4595e81ee2b8c07dea04c0bb5fd10cbfa
environment: null
depth: quick
supersedes: null
---

# Daily summary mail: open work items for the team lead

## Goal

The team lead gets one email each morning with the number of work items that are still open, so the day can be planned without opening the app. This release adds the mail and a read-only role for the team lead.

Done when:

- At 07:00 UTC every day the team lead's mailbox receives an email titled `Open work items: <number>`.
- The team lead can read the work items of the business unit and cannot change them.
- The mailbox address is not stored in the flow.

## Users

- Team lead: 1, who gets the new role `Work Item Team Lead` and the daily email.

## Data

| Change | Name | Detail |
|---|---|---|
| New environment variable | `pops_SummaryMailbox` | text, no default; the team lead's mailbox for each environment |
| New connection reference | `pops_SeedOutlook` | Office 365 Outlook, standard connector; the flow's email action uses it |
| New security role | `Work Item Team Lead` | Read on `pops_workitem` at business unit depth and no other privilege |
| Version | `PowerOpsSeed` | 1.1.0.0 |

## Automation

One new flow, `Email daily work item summary`:

1. Trigger: every day at 07:00 UTC.
2. List the work items with `statecode` 0.
3. Send an email through the connection reference `pops_SeedOutlook` to the address in `pops_SummaryMailbox`, with the subject `Open work items: <number>`.

The flow uses the existing connection reference `pops_SeedDataverse` for the list and is turned on in the export. The flow `Stamp work item status` stays as it is.

## Screens

None.

## Environments and route

Dev (unmanaged), Test (managed) and Prod (managed). The solution is built in Dev and moves to Test and Prod through the pipeline. The operations manager approves the move to Prod.

## Solution and publisher

Solution `PowerOpsSeed`, publisher `powerops` with prefix `pops`. The version goes from 1.0.0.0 to 1.1.0.0.

## Constraints

- Licensing: Outlook is a standard connector, so no new licence is needed.
- Data policy: checked by the platform team for all three environments. Dataverse and Office 365 Outlook are in the Business group, and no other connector is used.
- Connections: the flow uses connection references only, and its connections belong to the dedicated service account of each environment, not to a maker.
- Access: the new role is read-only and is the only new access.

## Assumptions

- Environment facts were not read from a tenant.
- The service account has a mailbox that can send as the flow.

## Open questions

## Questions for your admin

## Steps

1. Add the environment variable `pops_SummaryMailbox` and the connection reference `pops_SeedOutlook`.
2. Add the security role `Work Item Team Lead` with Read on `pops_workitem` at business unit depth.
3. Build the flow `Email daily work item summary` and turn it on.
4. Write a flow test for it: with three open items the subject is `Open work items: 3` and the recipient comes from the variable.
5. Fill in the settings file with the mailbox and the Outlook connection.
6. Raise the solution version to 1.1.0.0.
7. Run `powerops analyze`, `powerops test`, `powerops diff` and `powerops settings validate`.
