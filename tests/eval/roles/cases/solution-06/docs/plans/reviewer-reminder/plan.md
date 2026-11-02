---
status: implementing
review: skipped
signed-off: 2026-11-01
signed-off-hash: 91ea2f5c30286821b50917c3ffeb7992bdfd836e4e7bd2e83fbf1875d1ce2b32
environment: null
depth: quick
supersedes: null
---

# Reviewer reminder: a weekly mail about work items left open

## Goal

Every Monday the reviewers get one email that says how many work items have been open for more than 14 days, so that old items are looked at. This release adds the mail and a read-only role for the reviewers.

Done when:

- At 07:00 UTC every Monday the reviewers' mailbox receives an email titled `Stale work items: <number>`, but only when there is at least one.
- A reviewer can read the work items of the business unit and cannot change them.
- The mailbox address is not stored in the flow.

## Users

- Reviewers: 4, who get the new role `Work Item Reviewer` and the weekly email.

## Data

| Change | Name | Detail |
|---|---|---|
| New environment variable | `pops_ReminderMailbox` | text, no default; the reviewers' mailbox for each environment |
| New connection reference | `pops_SeedOutlook` | Office 365 Outlook, standard connector; the flow's email action uses it |
| New security role | `Work Item Reviewer` | Read on `pops_workitem` at business unit depth and no other privilege |
| Version | `PowerOpsSeed` | 1.1.0.0 |

## Automation

One new flow, `Remind reviewers of stale items`:

1. Trigger: every Monday at 07:00 UTC.
2. List the work items with `statecode` 0 that were created more than 14 days ago.
3. If there is at least one, send an email through the connection reference `pops_SeedOutlook` to the address in `pops_ReminderMailbox`, with the subject `Stale work items: <number>`.

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

1. Add the environment variable `pops_ReminderMailbox` and the connection reference `pops_SeedOutlook`.
2. Add the security role `Work Item Reviewer` with Read on `pops_workitem` at business unit depth.
3. Build the flow `Remind reviewers of stale items` and turn it on.
4. Write two flow tests: with two stale items the subject is `Stale work items: 2` and the recipient comes from the variable, and with none the email is skipped.
5. Fill in the settings file with the mailbox and the Outlook connection.
6. Raise the solution version to 1.1.0.0.
7. Run `powerops analyze`, `powerops test`, `powerops diff` and `powerops settings validate`.
