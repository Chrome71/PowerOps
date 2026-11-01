---
status: approved
review: pending
signed-off: null
signed-off-hash: null
environment: null
depth: standard
supersedes: null
---

# Invoice approval: from capture to the finance system

## Goal

Replace the emailed PDF invoices with a flow that starts when an accounts payable clerk captures an invoice, sends it to the cost-centre approver, and posts the approved invoice to the finance system through its REST interface.

Done when:

- An approver can approve or reject an invoice from Teams or email within one working day.
- An approved invoice appears in the finance system within ten minutes, and a rejected one returns to the clerk with the reason.
- The finance controller sees every invoice and its state.

## Users

- Accounts payable clerks: 10. Capture invoices and correct rejected ones.
- Cost-centre approvers: about 60. Approve or reject invoices of their own cost centre.
- Finance controller: 1. Reads all invoices and changes nothing.
- Finance systems team: 2 people who own the finance system interface and administer the solution.

## Data

| Table | Holds | Volume | Classification |
|---|---|---|---|
| `fin_Invoice` | supplier reference, amount, currency, cost centre, purchase order number, status, decision | about 30,000 rows a year | Confidential |
| `fin_InvoiceLine` | the lines of an invoice: description, amount, account | about 120,000 rows a year | Confidential |

## Automation

1. `Start approval`. Trigger: `fin_Status` of a `fin_Invoice` row becomes Submitted. Action: start an approval for the approver of the cost centre, then set the status to Approved or Rejected with the reason. Connection reference `fin_ApprovalsService`.
2. `Post to the finance system`. Trigger: `fin_Status` becomes Approved. Action: a call with the HTTP connector to the finance system's invoice interface, with the invoice and its lines as JSON. The address comes from environment variable `fin_FinanceApiAddress` and the credential from environment variable `fin_FinanceApiCredential` (type Secret, backed by the organisation's vault). On success, set the status to Posted. On failure, set it to PostFailed and email the finance systems mailbox. Connection reference `fin_OutlookService`. Environment variable `fin_FinanceSystemsMailbox` holds the mailbox for each environment.

Both flows run under the dedicated service account of their environment.

## Screens

- Capture invoice (canvas app for clerks): supplier reference, amount, currency, cost centre, purchase order, lines. One button, Submit. After a successful submit the form clears for the next invoice.
- Invoice list (clerks): own invoices with status, with Open and Back.
- Invoice review (model-driven form for the controller): read-only, with the standard back control.

## Environments and route

Dev (unmanaged), Test (managed) and Prod (managed), each a separate environment. The solution is built in Dev. The finance systems team exports it, runs the solution checker and imports it into Test through the pipeline. Test posts to the finance system's test company. After the Test checks pass, the finance controller approves the move and the pipeline imports the same managed package into Prod.

## Solution and publisher

Solution `InvoiceApproval`, publisher `Finance Systems` with prefix `fin`. One deployable unit: the app, the tables, the roles and the two flows.

## Constraints

- Licensing: the HTTP connector is premium. The finance systems team holds the Power Automate Premium per-user licence that owns the flows' connections, paid from the finance budget. Clerks and the controller hold Power Apps per-user plans, and approvers use Approvals through their Microsoft 365 licence.
- Data policy: the platform team gave this list for all three environments. Business group: Dataverse, Approvals and Office 365 Outlook. Blocked: HTTP, SQL Server and every custom connector.
- Security roles: `AP Clerk` reads and writes invoices and lines of the business unit and cannot delete. `Approver` reads invoices of the user's cost centre. `Controller` reads all invoices. The finance systems team holds System Administrator in Dev only.
- Sharing, app users, column security: the canvas app is shared with the AP Clerks group only, with no co-owner outside the finance systems team. No application user. No column needs field security.
- Secrets: the finance system credential is issued once by the finance system's vendor and sent to the lead maker by email. It stays valid for the three-year contract and is not rotated. It is entered once per environment into the Secret environment variable, which reads from the vault.
- Standards and lifecycle: the plan follows the company's financial controls policy and the Power Platform Well-Architected Security checklist. Changes go through source control with a second maker's review, and the pipeline service principal deploys to Test and Prod. The threat model is a one-page list kept in the repo.
- Identity and segmentation: each environment has its own service account and its own finance system company. Access is by security group, never by user. Makers have no role in Prod. The finance controller reviews Prod role assignments every quarter.
- Encryption: the platform's own encryption at rest and in transit. The call to the finance system uses TLS. No custom encryption.
- Monitoring and incidents: Dataverse auditing is on for both tables. A failed post emails the finance systems mailbox, and the audit log is reviewed monthly. The finance systems team lead owns incident response. The runbook is to turn off both flows, revoke the finance credential at the vendor and report to the information security office, and it is rehearsed once before go-live.
- Testing: one test user per role in Test, including a clerk of another cost centre who must be denied. The flows are tested with `powerops test` before each import, and the post is checked against the finance system's test company.
- Deadline: live before the financial year ends in March.

## Assumptions

- Environment facts were not read from a tenant. The publisher and the three environments exist.
- The finance system's invoice interface is stable for the contract period.

## Open questions

## Questions for your admin

## Steps

1. Create the solution `InvoiceApproval` with publisher prefix `fin` in Dev.
2. Create the two tables and the three security roles. Check with a test user per role what each can read.
3. Build the Capture invoice app and the invoice list. Check that Submit saves an invoice and shows the error when a save fails.
4. Build the two flows with their connection references and environment variables. Run `powerops test` on both.
5. Export from Dev, run the solution checker and fix what it reports.
6. Import as managed into Test with the pipeline. Capture an invoice, approve it, and check that it posts to the finance system's test company.
7. After the finance controller approves, import the same package into Prod and set the environment variables.
8. Run the incident drill once and turn on the flows.
