---
status: approved
review: pending
signed-off: null
signed-off-hash: null
environment: null
depth: standard
supersedes: null
---

# Asset requests: laptops and phones for new and existing staff

## Goal

Give staff one place to request a laptop, phone or accessory, send the request to the cost-centre owner for approval, and pass approved requests to the IT service desk tool. Today requests arrive by email and are re-typed into the service desk tool.

Done when:

- An employee submits a request in under two minutes and sees its status.
- A cost-centre owner approves or rejects from Teams or email, and a request waits no longer than three working days.
- An approved request exists in the service desk tool within ten minutes, and the service desk can read its approval trail.

## Users

- Employees: about 1,400. Create requests and read their own.
- Cost-centre owners: about 90. Approve or reject the requests of their cost centre.
- IT service desk: 9 people. Read approved requests and mark them fulfilled.
- Service desk tool: an external system that reads approved requests. It is not a person.

## Data

| Table | Holds | Volume | Classification |
|---|---|---|---|
| `its_Request` | requester, item type, justification, cost centre, status, decision, fulfilment date | about 6,000 rows a year | Internal |
| `its_ItemType` | the catalogue of items with a standard price | 60 rows | Internal |

Nothing in the tables is above Internal: no personal data beyond the requester's name and cost centre, and no financial detail beyond a standard price. The classification label is applied to both tables, and it is why no column needs field security.

## Automation

1. `Start approval`. Trigger: `its_Status` of an `its_Request` row becomes Submitted. Action: start an approval for the owner of the cost centre, then set the status to Approved or Rejected with the reason. Connection reference `its_ApprovalsService`.
2. `Pass to the service desk`. Trigger: `its_Status` becomes Approved. Action: a call with the HTTP connector to the service desk tool's ticket interface with the request as JSON, then set the status to Sent. The address comes from environment variable `its_ServiceDeskAddress` and the credential from environment variable `its_ServiceDeskCredential` (type Secret, backed by the organisation's vault). On failure, set the status to SendFailed and email the service desk mailbox. Connection reference `its_OutlookService`. Environment variable `its_ServiceDeskMailbox` holds the mailbox for each environment.
3. `Read approved requests`. The service desk tool reads approved requests through the Dataverse web interface as an application user. It needs to read `its_Request` rows and nothing else.

Flows 1 and 2 run under the dedicated service account of their environment.

## Screens

- New request: item type, justification, cost centre. Buttons: Submit and Cancel. A failed submit keeps the form open and shows the error.
- My requests: the employee's requests with status, and Open and Back.
- Service desk view: approved requests not yet fulfilled, with a Mark fulfilled button that asks for confirmation.

The app is a model-driven app, so every form opens in the standard shell with its own back control.

## Environments and route

Dev (unmanaged), Test (managed) and Prod (managed), each a separate environment. The solution is built in Dev. The platform team exports it, runs the solution checker and imports it as managed into Test through the pipeline. Test calls the service desk tool's test instance. After the service desk manager approves the Test checks, the pipeline imports the same package into Prod.

## Solution and publisher

Solution `AssetRequests`, publisher `IT Services` with prefix `its`. One deployable unit: the app, the two tables, the roles and the two flows.

## Constraints

- Licensing: the HTTP connector is premium. The service account that owns flow 2 holds a Power Automate Premium per-user licence, paid from the IT budget. The employees, owners and service desk use Power Apps per-app plans from the same budget. Approvals needs no extra licence.
- Data policy: checked against the tenant policy for all three environments by the platform team. Dataverse, Approvals, Office 365 Outlook and HTTP are in the Business group in all three, and no other connector is used.
- Security roles: `Employee` creates requests and reads the user's own. `Cost Centre Owner` reads requests of the user's cost centre. `Service Desk` reads approved requests and updates the fulfilment fields. `Service Desk Integration` is the role of the application user: Read on `its_Request` and no other privilege. No role can delete a request. The platform team holds System Administrator in Dev only.
- Sharing: the model-driven app is assigned to the first three roles and nobody else. Nothing is shared by link.
- Column security: not used, as the Data section explains. There are no secrets in the tables.
- App users: one application user per environment, with the `Service Desk Integration` role only. It uses a certificate held by the service desk tool's vendor.
- Secrets: the service desk credential is a Secret environment variable that reads from the vault, with the value set per environment at import and never written to a flow, a table or a log. The service desk manager rotates it every 90 days, and at once if it may have been exposed. The rotation date is kept in the vault.
- Standards and lifecycle: the plan follows the company's information security standard and the Power Platform Well-Architected Security checklist. Changes go through source control with a second maker's review, and the pipeline service principal deploys to Test and Prod. The threat model is a one-page list kept in the repo.
- Identity and segmentation: each environment has its own service account, its own application user and its own service desk tool instance. Access is by security group, never by user. Makers have no role in Prod. The service desk manager reviews Prod role assignments every quarter.
- Encryption: the platform's own encryption at rest and in transit. The call to the service desk tool uses TLS. No custom encryption and no exports of the tables.
- Monitoring and incidents: Dataverse auditing is on for both tables. A failed send emails the service desk mailbox, and the audit log is reviewed monthly. The service desk manager owns incident response. The runbook is to turn off both flows, disable the application user, rotate the credential and report to the information security office, and it is rehearsed once before go-live.
- Testing: one test user per role in Test, including a user with no role who must be denied, and the application user, which must be unable to write. The flows are tested with `powerops test` before each import, and the send is checked against the test instance of the service desk tool.
- Deadline: live before the next hardware refresh round in the second quarter.

## Assumptions

- Environment facts were not read from a tenant. The publisher and the three environments exist.
- The service desk tool's ticket interface accepts a JSON body and a bearer credential.

## Open questions

## Questions for your admin

## Steps

1. Create the solution `AssetRequests` with publisher prefix `its` in Dev.
2. Create the two tables, the four security roles and the application user. Check with a test user per role what each can read, and that the application user cannot write.
3. Build the model-driven app with its forms and views. Check that each form saves and closes, and that Cancel returns to the list.
4. Build the two flows with their connection references and environment variables. Run `powerops test` on both.
5. Export from Dev, run the solution checker and fix what it reports.
6. Import as managed into Test with the pipeline. Submit a request, approve it, and check that it reaches the test instance of the service desk tool.
7. After the service desk manager approves, import the same package into Prod and set the environment variables.
8. Run the incident drill once and turn on the flows.
