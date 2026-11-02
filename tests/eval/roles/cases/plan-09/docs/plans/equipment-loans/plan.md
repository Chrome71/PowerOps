---
status: approved
review: pending
signed-off: null
signed-off-hash: null
environment: null
depth: standard
supersedes: null
---

# Equipment loans: cameras and laptops on loan to staff

## Goal

Give the audio-visual department one place to track which camera, laptop or projector is out, who has it and when it is due back. Staff request equipment with a form, technicians hand it out and take it back in a canvas app, and overdue loans are chased automatically.

Done when:

- A technician records a loan or a return in under 30 seconds.
- A borrower gets a confirmation when the loan is recorded and a reminder one day before it is due.
- The coordinator sees all overdue loans on one screen.

## Users

- Technicians: 6. Use the canvas app to hand out and take back equipment.
- Coordinator: 1. Reads all loans and edits the equipment list.
- Borrowers: about 800 staff. They send a request with a Microsoft Forms form and receive emails. They do not open the app.

## Data

| Table | Holds | Volume | Classification |
|---|---|---|---|
| `avl_Equipment` | one item: asset tag, type, condition, status | about 2,400 rows | Internal |
| `avl_Loan` | a loan: item, borrower, out date, due date, returned date, status | about 30,000 rows a year | Internal |

## Automation

1. `Record a loan request`. Trigger: a response is submitted to the request form. Action: create an `avl_Loan` row with status Requested, and email the borrower a confirmation. Connections: Microsoft Forms, Dataverse and Office 365 Outlook, all taken from the lead maker's own account, which created the flows. No connection references are added to the solution.
2. `Remind borrowers`. Trigger: every weekday at 08:00 UTC. Action: find loans due the next day and not returned, and email each borrower. Connections as in flow 1.
3. `Check the asset register`. Trigger: an `avl_Equipment` row is created. Action: a call with the HTTP connector to the university's asset register interface to fetch the item's purchase date and warranty end, written back to the row. The address is in environment variable `avl_AssetRegisterAddress`.

## Screens

- Loans out: loans with status Out, sorted by due date. The gallery uses `Filter(avl_Loan, avl_Status = 'Out')`, which delegates, and the data row limit is raised to 2,000. Buttons: Open, Back.
- Hand out: pick an item and a borrower, set the due date. Buttons: Save, Cancel and Back. A failed save keeps the form open and shows the error.
- Take back: scan or pick the item, set its condition. Buttons: Save, Cancel and Back. A failed save keeps the form open and shows the error.
- Overdue: loans past the due date for the coordinator, with a Remind now button.

## Environments and route

Dev (unmanaged), Test (managed) and Prod (managed). The solution is built in Dev. The lead maker exports it, runs the solution checker and imports it as managed into Test. After the coordinator approves the Test checks, the lead maker imports the same package into Prod.

## Solution and publisher

Solution `EquipmentLoans`, publisher `AV Services` with prefix `avl`. One deployable unit: the app, the two tables, the roles and the three flows.

## Constraints

- Licensing: all staff and the makers hold Microsoft 365 A3. The canvas app, the flows and the form use standard connectors only, so no extra licence is bought. The 7 app users hold Power Apps per-user plans from the department budget.
- Data policy: checked against the tenant policy for all three environments by the platform team. Dataverse, Forms, Office 365 Outlook and HTTP are in the Business group, and no other connector is used.
- Security roles: `Technician` reads and writes loans and equipment of the business unit and cannot delete. `Coordinator` also edits the equipment list. The lead maker holds System Administrator in Dev, Test and Prod.
- Sharing, app users, secrets, column security: the canvas app is shared with the Technicians group and the coordinator only. No application user, no secret and no secured column.
- Standards and lifecycle: the plan follows the university's IT security policy and the Power Platform Well-Architected Security checklist. Changes go through source control with a second maker's review. The threat model is a one-page list kept in the repo.
- Identity and access: deployments to Prod are made with the lead maker's own account. The account keeps System Administrator in Prod after the import, so that it can fix data quickly. Technicians are assigned their role through a security group.
- Segmentation and encryption: Dev, Test and Prod are separate environments with separate data. The platform's own encryption at rest and in transit is used, and nothing is exported.
- Monitoring and incidents: Dataverse auditing is on for both tables. A failed run of any flow emails the department's operations mailbox, set in environment variable `avl_OperationsMailbox`, and the audit log is reviewed monthly. The coordinator owns incident response. The runbook is to turn off the flows, remove the affected role assignments and report to the IT security office, and it is rehearsed once before go-live.
- Testing: one test user per role in Test, including a user with no role who must be denied. The flows are tested with `powerops test` before each import.
- Deadline: live before the new academic year.

## Assumptions

- Environment facts were not read from a tenant. The publisher and the three environments exist.
- The asset register's read-only interface is public and needs no sign-in.

## Open questions

## Questions for your admin

## Steps

1. Create the solution `EquipmentLoans` with publisher prefix `avl` in Dev.
2. Create the two tables and the two security roles. Check with a test user per role what each can read.
3. Build the canvas app with its four screens. Check that every form has a working Save, Cancel and Back.
4. Build the three flows with their environment variables. Run `powerops test` on each.
5. Export from Dev, run the solution checker and fix what it reports.
6. Import as managed into Test. Record a loan from a form response and check the confirmation and the reminder.
7. After the coordinator approves, import the same package into Prod and set the environment variables.
8. Run the incident drill once and turn on the flows.
