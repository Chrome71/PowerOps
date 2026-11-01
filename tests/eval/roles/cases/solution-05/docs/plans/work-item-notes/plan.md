---
status: implementing
review: skipped
signed-off: 2026-11-01
signed-off-hash: 4141bc5a4d8e6b1e76a1924f0dcadc0093c14c6fb875f500c1c956b85b0028ed
environment: null
depth: quick
supersedes: null
---

# Work item notes: a confidential notes column for supervisors

## Goal

Supervisors need to write confidential notes on a work item, for example why it was reassigned. Only supervisors may read or change those notes. This release adds the column and its protection, and nothing else.

Done when:

- Work Item has a multi-line column `pops_notes`.
- Only the members of the `Work Item Note Readers` profile can read or change `pops_notes`. Everyone else sees the column as locked.
- Every change to `pops_notes` is in the audit log.

## Users

- Supervisors: 6, in the Supervisors team, who read and write the notes.
- Other staff: about 40 colleagues who use Work Items today. They must not read or change notes.

## Data

| Change | Name | Detail |
|---|---|---|
| New column | `pops_notes` on `pops_workitem` | multiple lines of text, 2,000 characters, classified Confidential, column security on |
| New field security profile | `Work Item Note Readers` | Read and Update on `pops_notes` for the Supervisors team, and nobody else |
| Auditing | `pops_notes` | auditing on for the table and for the column |
| Version | `PowerOpsSeed` | 1.2.0.0 |

## Automation

None changes. The flow `Stamp work item status` stays as it is.

## Screens

None. Forms are unchanged in this release, so the column is not shown on a form yet.

## Environments and route

Dev (unmanaged), Test (managed) and Prod (managed). The solution is built in Dev and moves to Test and Prod through the pipeline. The operations manager approves the move to Prod.

## Solution and publisher

Solution `PowerOpsSeed`, publisher `powerops` with prefix `pops`. The version goes from 1.0.0.0 to 1.2.0.0.

## Constraints

- Licensing: no new connector and no new licence.
- Data policy: no new connector, so the data policy is unchanged.
- Classification: the notes are Confidential. Column security and auditing follow from that.
- Access: the profile names the Supervisors team only. Membership of the team is managed by security group, by the platform team.

## Assumptions

- Environment facts were not read from a tenant.
- The Supervisors team already exists in each environment.

## Open questions

## Questions for your admin

## Steps

1. Add the column `pops_notes` to Work Item with column security on.
2. Add the field security profile `Work Item Note Readers` with Read and Update on `pops_notes`.
3. Turn on auditing for the table and for the column.
4. Keep the flow test that sets `pops_DefaultStatus` to Active, and add one for the default: with the variable not set, the flow writes `New`.
5. Raise the solution version to 1.2.0.0.
6. Run `powerops analyze`, `powerops test`, `powerops diff` and `powerops settings validate`.
