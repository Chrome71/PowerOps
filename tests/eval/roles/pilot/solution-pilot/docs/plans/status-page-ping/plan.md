---
status: implementing
review: skipped
signed-off: 2026-11-01
signed-off-hash: 4d15d1178a688ffaadd4c3ad209a0e598a54758fc2049cc2aac90cab6a556e3d
environment: null
depth: quick
supersedes: null
---

# Status page ping: tell the status page about every new work item

## Goal

Every new work item is reported to the department's status page, so that the people who watch it see new work without opening the app. This release adds the report and the two settings it needs.

Done when:

- Within a minute of a work item being created, the status page receives its name.
- The page's address and token are not stored in any flow or table.

## Users

- Status page readers: they read an external page and have no access to Dataverse.

## Data

| Change | Name | Detail |
|---|---|---|
| New environment variable | `pops_StatusPageUrl` | text, no default; the page's address for each environment |
| New environment variable | `pops_StatusPageToken` | Secret, no default; the settings file holds a reference to the organisation's vault for each environment |
| Version | `PowerOpsSeed` | 1.1.0.0 |

## Automation

One new flow, `Ping the status page`:

1. Trigger: the Dataverse trigger "When a row is added, modified or deleted", change type Create, table Work Item, scope Organization.
2. Post `{"name": <work item name>}` to the page with the HTTP connector. The address is `pops_StatusPageUrl`, and the credential travels in the `Authorization` header from `pops_StatusPageToken`.

The trigger uses the existing connection reference `pops_SeedDataverse`. The flow is turned on in the export. The flow `Stamp work item status` stays as it is.

## Screens

None.

## Environments and route

Dev (unmanaged), Test (managed) and Prod (managed). The solution is built in Dev and moves to Test and Prod through the pipeline. The operations manager approves the move to Prod.

## Solution and publisher

Solution `PowerOpsSeed`, publisher `powerops` with prefix `pops`. The version goes from 1.0.0.0 to 1.1.0.0.

## Constraints

- Licensing: the HTTP connector is premium. The service account that owns the flow holds a Power Automate Premium per-user licence, paid from the operations budget.
- Data policy: checked by the platform team for all three environments. Dataverse and HTTP are in the Business group, and no other connector is used.
- Secrets: the page token is a Secret environment variable. Its value in the settings file is a reference to the vault, never the token itself, and it appears in no flow.
- Access: no new role and no new access. The report carries the work item name only.

## Assumptions

- Environment facts were not read from a tenant.
- The status page accepts a JSON post and a bearer credential.

## Open questions

## Questions for your admin

## Steps

1. Add the two environment variables.
2. Build the flow `Ping the status page` and turn it on.
3. Write a flow test for it: for the work item `Pump check` the post carries that name.
4. Fill in the settings file: the page's address, and the vault reference for the token.
5. Raise the solution version to 1.1.0.0.
6. Run `powerops analyze`, `powerops test`, `powerops diff` and `powerops settings validate`.
