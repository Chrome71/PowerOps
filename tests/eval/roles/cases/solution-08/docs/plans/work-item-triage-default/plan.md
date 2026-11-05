---
status: implementing
review: skipped
signed-off: 2026-11-04
signed-off-hash: b186138a7e20cefc4b39763e5abedb423d18449b60ecef170bca86b68bfc738e
environment: null
depth: quick
supersedes: null
---

# Work item triage default: a shared board for readers

## Goal

New work items start as `Triage`, and the employees who only look at work items use the model-driven app `Work Item Board` to read them. Contractors do not use the board.

Done when:

- A new work item stamped by the flow carries the status label `Triage` when no value is set for the environment.
- A reader can read work items they own in the app and cannot change, delete, assign or share any.
- A contractor cannot open the app.

## Users

- Readers: 20 employees, who get the role `Work Item Reader` and use the app `Work Item Board`.
- Contractors: 8 outside contractors, who have the role `Work Item Contractor`. They update work items they own through the existing flows and exports and are not given the app.

## Data

| Change | Name | Detail |
|---|---|---|
| Changed environment variable | `pops_DefaultStatus` | the default value goes from `New` to `Triage` |
| Existing security role, unchanged | `Work Item Reader` | Read on `pops_workitem` at user depth (Basic) and no other privilege |
| Existing security role, unchanged | `Work Item Contractor` | Read and Write on `pops_workitem` at user depth (Basic) and no other privilege |
| Version | `PowerOpsSeed` | 1.2.0.0 |

## Automation

The existing flow `Stamp work item status` stays as it is, apart from the default of its `pops_DefaultStatus` parameter, which follows the variable: `Triage`. No new flow is added.

## Screens

The model-driven app `Work Item Board` stays as it is in 1.1.0.0: it contains the table `pops_workitem`, and its role map lists the role `Work Item Reader` only. The role `Work Item Contractor` is kept out of the app.

## Environments and route

Dev (unmanaged), Test (managed) and Prod (managed). The solution is built in Dev and moves to Test and Prod through the pipeline. The operations manager approves the move to Prod.

## Solution and publisher

Solution `PowerOpsSeed`, publisher `powerops` with prefix `pops`. The version goes from 1.1.0.0 to 1.2.0.0.

## Constraints

- Licensing: no new licence is needed.
- Data policy: no connector is added; checked by the platform team for all three environments.
- Connections: unchanged.
- Access: this release adds no access. Readers keep read-only access at user depth, and contractors stay out of the app.

## Assumptions

- Environment facts were not read from a tenant.
- No environment holds its own value for `pops_DefaultStatus` that would hide the new default: the settings file sets `Triage` for every environment.

## Open questions

## Questions for your admin

## Steps

1. Change the default value of the environment variable `pops_DefaultStatus` to `Triage`.
2. Change the default of the flow parameter `pops_DefaultStatus` in `Stamp work item status` to `Triage`.
3. Write a flow test: with no value for the variable, the flow writes `Triage`.
4. Raise the solution version to 1.2.0.0.
5. Keep the two roles and the app's role map as they are.
6. Run `powerops analyze`, `powerops test`, `powerops diff` and `powerops settings validate`.
