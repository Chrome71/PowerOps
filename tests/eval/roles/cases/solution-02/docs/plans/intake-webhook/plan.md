---
status: implementing
review: skipped
signed-off: 2026-11-01
signed-off-hash: 371382354be78310289ed487145a9652f19d8f3a62c0764bb79d566d8cbdc7a8
environment: null
depth: quick
supersedes: null
---

# Intake webhook: tell the intake log about every new work item

## Goal

Every new work item is reported to the intake log of the service desk, so that its staff see new work without opening the app. This release adds the report and the two settings it needs.

Done when:

- Within a minute of a work item being created, the intake log receives its name.
- The log's address and token are not stored in any flow or table.
- The report covers work items of every business unit.

## Users

- Service desk staff: they read the intake log, which is an external page. They have no access to Dataverse.

## Data

| Change | Name | Detail |
|---|---|---|
| New environment variable | `pops_IntakeLogUrl` | text, no default; the log's address for each environment |
| New environment variable | `pops_IntakeLogToken` | Secret, no default; the settings file holds a reference to the organisation's vault for each environment |
| Version | `PowerOpsSeed` | 1.1.0.0 |

## Automation

One new flow, `Report new work item to the intake log`:

1. Trigger: the Dataverse trigger "When a row is added, modified or deleted", change type Create, table Work Item, scope Organization, because the log covers every business unit.
2. Post `{"name": <work item name>}` to the log with the HTTP connector. The address is `pops_IntakeLogUrl`, and the credential travels in the `Authorization` header from `pops_IntakeLogToken`.

The trigger uses the existing connection reference `pops_SeedDataverse`. The flow is turned on in the export. The flow `Stamp work item status` stays as it is.

## Screens

None.

## Environments and route

Dev (unmanaged), Test (managed) and Prod (managed). The solution is built in Dev and moves to Test and Prod through the pipeline. The operations manager approves the move to Prod.

## Solution and publisher

Solution `PowerOpsSeed`, publisher `powerops` with prefix `pops`. The version goes from 1.0.0.0 to 1.1.0.0.

## Constraints

- Licensing: the HTTP connector is premium. The service account that owns the flow holds a Power Automate Premium per-user licence, paid from the service desk budget.
- Data policy: checked by the platform team for all three environments. Dataverse and HTTP are in the Business group, and no other connector is used.
- Secrets: the log token is a Secret environment variable. Its value in the settings file is a reference to the vault, never the token itself.
- Access: no new role and no new access. The report carries the work item name only.

## Assumptions

- Environment facts were not read from a tenant.
- The intake log accepts a JSON post and a bearer credential.

## Open questions

## Questions for your admin

## Steps

1. Add the two environment variables.
2. Build the flow `Report new work item to the intake log` and turn it on.
3. Write a flow test for it: for the work item `Pump check` the post carries that name and an `Authorization` header built from the token variable.
4. Fill in the settings file: the log's address, and the vault reference for the token.
5. Raise the solution version to 1.1.0.0.
6. Run `powerops analyze`, `powerops test`, `powerops diff` and `powerops settings validate`.
