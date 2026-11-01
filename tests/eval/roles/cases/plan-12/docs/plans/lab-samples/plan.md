---
status: approved
review: pending
signed-off: null
signed-off-hash: null
environment: null
depth: standard
supersedes: null
---

# Lab samples: tracking from reception to report

## Goal

Track every sample that an environmental testing laboratory receives, from the barcode scan at reception through each analysis step to the client report. Analysts work on a tablet canvas app with a barcode scanner, and the client contact is told when the results are released.

Done when:

- A sample is registered at reception with one scan and a client reference.
- Every hand-over of a sample between stations is recorded with who and when.
- The lab manager releases the results of a batch, and the client contact is notified within ten minutes of release.

## Users

- Reception staff: 5. Register samples and print labels.
- Analysts: 22. Record results for the samples at their station.
- Lab manager: 1. Reviews and releases results.
- Quality officer: 1. Reads all samples, results and the hand-over history, and changes nothing.

## Data

| Table | Holds | Volume | Classification |
|---|---|---|---|
| `lab_Client` | client name and contact address | about 400 rows | Confidential |
| `lab_Batch` | a group of samples that are released together: name, status (Open or Released), release date | about 600 rows a year, kept for ten years | Internal |
| `lab_Sample` | barcode, client, client reference, matrix, received date, station, batch, status | about 150,000 rows a year, kept for ten years | Confidential |
| `lab_Result` | sample, analysis, value, unit, analyst, status | about 900,000 rows a year, kept for ten years | Restricted |
| `lab_Handover` | a hand-over of a sample: from, to, who, time | about 450,000 rows a year, kept for ten years | Internal |

Result values are Restricted because clients treat them as commercially sensitive, and the laboratory's accreditation requires them to be kept ten years.

## Automation

1. `Record a hand-over`. Trigger: the station of a `lab_Sample` row changes. Action: create a `lab_Handover` row with the previous and new station, the user who made the change and the time. Connection reference `lab_DataverseService`.
2. `Tell the client`. Trigger: the status of a `lab_Batch` row becomes Released. Action: email the client contact of each sample in the batch that the report is ready, using the contact address stored in `lab_Client`. Connection reference `lab_OutlookService`. Environment variable `lab_ReportsSender` holds the sender mailbox for each environment.

Both flows use the dedicated service account of their environment through connection references. Dataverse is a premium connector, so that account holds a Power Automate Premium licence; Outlook is standard.

## Screens

- Scan: scan or type a barcode, then open the sample. The search uses the delegable `Filter(lab_Sample, lab_Barcode = txtScan.Text)` on the indexed barcode column, and the app lists only the open samples of the analyst's station, with the data row limit raised to 2,000.
- Sample: details and status, with Register result and Hand over. Buttons: Back.
- Register result: analysis, value, unit. Buttons: Save, Cancel and Back. A failed save keeps the form open and shows the error.
- Hand over: choose the next station. Buttons: Confirm, Cancel and Back. A failed hand-over keeps the screen open and shows the error.
- Release (lab manager): a batch's results with Release, which asks for confirmation.

## Environments and route

Dev (unmanaged), Test (managed) and Prod (managed), each a separate environment. The solution is built in Dev. The platform team exports it, runs the solution checker and imports it as managed into Test through the pipeline. After the quality officer approves the Test checks, the pipeline imports the same package into Prod.

## Solution and publisher

Solution `LabSamples`, publisher `Laboratory Systems` with prefix `lab`. One deployable unit: the app, the five tables, the roles, the field security profile and the two flows.

## Constraints

- Licensing: Power Apps per-user plans for the 29 users, paid from the laboratory budget, cover Dataverse in the app. Dataverse is a premium connector, so the flows' service account holds a Power Automate Premium licence, also paid from the laboratory budget; Outlook is a standard connector.
- Data policy: checked against the tenant policy for all three environments by the platform team. Dataverse and Office 365 Outlook are in the Business group, and no other connector is used.
- Security roles: `Reception` creates samples and reads them, the clients and the open batches, and picks a sample's batch when it registers the sample. `Analyst` reads and updates samples of the user's station, and creates and updates results for them. `Lab Manager` reads all samples and results, edits the clients, and creates and releases batches. `Quality Officer` reads everything, and edits nothing. No role can delete a sample, a result or a hand-over. The platform team holds System Administrator in Dev only.
- Column security: `lab_Value` on `lab_Result` is secured with the field security profile `Result Values`, which gives Create, Read and Update to the Analyst team, Read and Update to the Lab Manager team and Read to the Quality Officer team. Reception has no access.
- Sharing, app users, secrets: the canvas app is shared with the Reception, Analysts, Lab Manager and Quality Officer groups only, with no co-owner outside the platform team. No application user and no secret.
- Standards and lifecycle: the plan follows the laboratory's accreditation rules for record keeping, the company's information security standard and the Power Platform Well-Architected Security checklist. Changes go through source control with a second maker's review, and the pipeline service principal deploys to Test and Prod. The threat model is a one-page list kept in the repo.
- Identity and segmentation: each environment has its own service account. Access is by security group, never by user. Makers have no role in Prod. The quality officer reviews Prod role assignments every quarter.
- Encryption: the platform's own encryption at rest and in transit. No custom encryption and no exports of the tables.
- Monitoring and incidents: Dataverse auditing is on for the sample, result and hand-over tables, including the result value. A failed run of either flow emails the laboratory systems mailbox, and the audit log is reviewed monthly. The quality officer owns incident response. The runbook is to turn off the flows, remove the affected role assignments and report to the information security office and the accreditation body, and it is rehearsed once before go-live.
- Testing: one test user per role in Test, including a reception user who must not read a result value and a user with no role who must be denied. The flows are tested with `powerops test` before each import.
- Deadline: live before the accreditation audit in the third quarter.

## Assumptions

- Environment facts were not read from a tenant. The publisher and the three environments exist.
- The client list is loaded once from the laboratory's customer file, and every client has a contact address.

## Open questions

## Questions for your admin

## Steps

1. Create the solution `LabSamples` with publisher prefix `lab` in Dev.
2. Create the five tables, the four security roles and the field security profile. Check with a test user per role what each can read, including the result value.
3. Build the canvas app with its five screens. Check that every form has a working Save, Cancel and Back, and that the scan finds a sample in a table of 150,000 rows.
4. Build the two flows with their connection references and environment variable. Run `powerops test` on both.
5. Export from Dev, run the solution checker and fix what it reports.
6. Import as managed into Test with the pipeline. Register a sample, hand it over, release a batch and check the client mail.
7. After the quality officer approves, import the same package into Prod and set the environment variable.
8. Run the incident drill once and turn on the flows.
