---
status: approved
review: pending
signed-off: null
signed-off-hash: null
environment: null
depth: standard
supersedes: null
---

# Complaint handling: one record for every customer complaint

## Goal

Give the insurer one model-driven app for customer complaints. Service agents log a complaint, a handler investigates it and replies, team leads see the ones that are running late, and the compliance officer reports to the regulator. The regulator requires every complaint record to be kept for six years.

Done when:

- Every complaint has an owner, a due date and a full history of actions.
- A complaint without a reply after five working days is raised with the team lead.
- The compliance officer can produce the complaint report for any month without asking IT.

## Users

- Service agents: about 80. Log complaints and read the ones they logged.
- Complaint handlers: 25. Investigate, reply and close.
- Team leads: 4. Read the complaints of their team and reassign them.
- Compliance officer: 1. Reads all complaints and runs the monthly report.

## Data

| Table | Holds | Volume | Classification |
|---|---|---|---|
| `cmp_Complaint` | customer reference, product, description, channel, owner, due date, status | about 9,000 rows a year, kept six years | Confidential |
| `cmp_Action` | a step on a complaint: type, note, date, author | about 45,000 rows a year, kept six years | Confidential |

## Automation

1. `Assign a new complaint`. Trigger: a `cmp_Complaint` row is created. Action: set the owner to the handler with the fewest open complaints and the due date to five working days ahead. Connection reference `cmp_DataverseService`.
2. `Raise a late complaint`. Trigger: every weekday at 07:00 UTC. Action: find open complaints past their due date and post them to the team leads' Teams channel. Connection reference `cmp_TeamsService`. Environment variable `cmp_TeamLeadsChannel` holds the channel for each environment.
3. `Monthly report`. Trigger: the first weekday of the month. Action: email the compliance officer the counts of opened, closed and late complaints for the month before. Connection reference `cmp_OutlookService`. Environment variable `cmp_ComplianceMailbox` holds the address for each environment.

All three flows use the dedicated service account of their environment through connection references, and use standard connectors only.

## Screens

- Complaint form: customer reference, product, description, channel. Save and Close. A failed save keeps the form open and shows the platform's error.
- Complaint list views: My complaints, My team's complaints, Late complaints.
- Action form: type, note, date. Save and Close.

The app is a model-driven app, so every form opens in the standard shell with its own back control.

## Environments and route

Dev (unmanaged), Test (managed) and Prod (managed), each a separate environment. The solution is built in Dev. The platform team exports it, runs the solution checker and imports it as managed into Test through the pipeline. After the compliance officer approves the Test checks, the pipeline imports the same package into Prod.

## Solution and publisher

Solution `ComplaintHandling`, publisher `Customer Operations` with prefix `cpl`. One deployable unit: the app, the two tables, the roles and the three flows.

## Constraints

- Licensing: Power Apps per-user plans for the 110 users, paid from the customer operations budget. The flows use only standard connectors (Dataverse, Teams and Outlook).
- Data policy: checked against the tenant policy for all three environments by the platform team. Dataverse, Teams and Office 365 Outlook are in the Business group, and no other connector is used.
- Security roles: `Agent` creates complaints and reads those the user logged. `Handler` reads and updates complaints of the business unit, and also deletes them at organisation level, so that duplicate entries can be cleared. `Team Lead` reads and reassigns the complaints of the team. `Compliance Officer` reads all complaints. The platform team holds System Administrator in Dev only.
- Sharing, app users, secrets, column security: the app is assigned to the four roles and nobody else. No application user, no secret and no secured column, because every handler may read the whole complaint.
- Standards and lifecycle: the plan follows the insurer's information security policy, the regulator's record-keeping rule and the Power Platform Well-Architected Security checklist. Changes go through source control with a second maker's review, and the pipeline service principal deploys to Test and Prod. The threat model is a one-page list kept in the repo.
- Identity and segmentation: each environment has its own service account. Access is by security group, never by user. Makers have no role in Prod. The compliance officer reviews Prod role assignments every quarter.
- Encryption: the platform's own encryption at rest and in transit. No custom encryption and no exports of the tables.
- Monitoring and incidents: Dataverse auditing is on for both tables. A failed run of any flow emails the platform team mailbox, and the audit log is reviewed monthly. Incident response: the complaints team deals with an incident as it comes up and tells the platform team when it is over.
- Testing: one test user per role in Test, including a user with no role who must be denied. The flows are tested with `powerops test` before each import.
- Deadline: live before the regulator's reporting date in the first quarter.

## Assumptions

- Environment facts were not read from a tenant. The publisher and the three environments exist.
- Six years of complaints fit within the environment's Dataverse capacity.

## Open questions

## Questions for your admin

## Steps

1. Create the solution `ComplaintHandling` with publisher prefix `cpl` in Dev.
2. Create the two tables and the four security roles. Check with a test user per role what each can read and change.
3. Build the model-driven app with its views and forms. Check that each form saves and closes.
4. Build the three flows with their connection references and environment variables. Run `powerops test` on each.
5. Export from Dev, run the solution checker and fix what it reports.
6. Import as managed into Test with the pipeline. Log a complaint and follow it through assignment, a late raise and the report.
7. After the compliance officer approves, import the same package into Prod and set the environment variables.
8. Turn on the flows.
