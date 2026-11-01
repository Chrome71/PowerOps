---
status: approved
review: pending
signed-off: null
signed-off-hash: null
environment: null
depth: standard
supersedes: null
---

# Site inspections: a tablet app for network inspectors

## Goal

Replace the paper inspection sheets with a canvas app that inspectors fill in on site. They record the condition of valves, hydrants and pumping stations and attach findings, and urgent findings go to the maintenance planners. The utility's information policy treats the layout of the supply network and the details of its faults as Restricted.

Done when:

- An inspector records an inspection and its findings in under five minutes on a tablet with a mobile data connection.
- A finding marked Urgent reaches the planners' Teams channel within five minutes.
- Planners see and close the findings of their region, and inspectors see only their own region.

## Users

- Inspectors: about 40, each assigned to one of four regions.
- Maintenance planners: 6, one or two per region. They close findings.
- Operations manager: 1. Reads the reports of all regions and changes nothing.
- Platform team: 3 people who administer the environments and the solution.

## Data

| Table | Holds | Volume | Classification |
|---|---|---|---|
| `wtr_Site` | one valve, hydrant or station: name, type, region, coordinates | 9,000 rows | Restricted |
| `wtr_Inspection` | one visit to a site: site, date, inspector, result | about 180,000 rows a year, kept for two years, so 360,000 | Restricted |
| `wtr_Finding` | a fault found during a visit: description, severity, status, photo reference | about 12,000 rows a year | Internal |

Classification follows the utility's labels. Restricted tables are readable only through the roles below. Photos are stored in a Dataverse file column, not in a separate store.

## Automation

1. `Notify planners of an urgent finding`. Trigger: a `wtr_Finding` row is created. Condition: severity is Urgent. Action: post a message to the planners' Teams channel. Connection reference `wtr_TeamsPlanners` (Microsoft Teams). Environment variable `wtr_PlannersChannel` holds the channel for each environment.
2. `Remind planners of open findings`. Trigger: every weekday at 07:00 UTC. Action: list the open Urgent findings older than two days and email one summary to the operations mailbox. Connection reference `wtr_OutlookService` (Office 365 Outlook). Environment variable `wtr_OperationsMailbox` holds the address for each environment.

Both flows use the dedicated service account for their connections, and nothing is hard-coded in them. Both are standard-licence connectors.

## Screens

- Inspection list: the inspector's inspections, newest first, with a search box. The gallery filters with `Search(wtr_Inspection, txtSearch.Text, SiteLabel)`, where `SiteLabel` is a calculated column that joins the site name and the region. The app keeps the default data row limit of 500.
- Inspection form: site picker, date, result, notes. Buttons: Save, Cancel and Back. A failed save keeps the form open and shows the error.
- Finding form: description, severity, photo. Buttons: Save, Cancel and Back. A failed save keeps the form open and shows the error.
- Planner board: open findings of the planner's region, with a Close action that asks for confirmation.

## Environments and route

Dev (unmanaged), Test (managed) and Prod (managed), each a separate environment. The solution is built and edited in Dev only. The platform team exports it from Dev, runs the solution checker, and imports it as managed into Test through the deployment pipeline. After the Test checks pass, the operations manager approves the move and the pipeline imports the same managed package into Prod. Environment variable values and connection references are set at each import.

## Solution and publisher

Solution `SiteInspections`, publisher `Water Operations` with prefix `wtr`. One deployable unit: the app, the tables, the roles and the two flows.

## Constraints

- Licensing: the 40 inspectors and 6 planners hold Power Apps per-user plans, paid from the operations budget. The two flows use only standard connectors (Dataverse, Teams, Outlook), so no extra flow licence is needed.
- Data policy: checked against the tenant policy for Dev, Test and Prod by the platform team. Dataverse, Teams and Office 365 Outlook are in the Business group in all three environments, and no other connector is used.
- Security roles: `Inspector` reads and writes sites, inspections and findings of the user's business unit and cannot delete. `Planner` reads and updates findings of the business unit and closes them. `Manager` reads all rows in all business units and writes nothing. The platform team holds System Administrator in Dev only and has no role in Prod data.
- Sharing: the canvas app is shared with Everyone, so staff find it in their app list, and the Inspectors group has Co-owner permission, so that it can add contractors without asking the platform team. Access to data still depends on the roles above.
- Column security, app users, secrets: no column needs field security because Restricted tables are protected by the roles. There is no application user. The flows hold no secrets, only the two environment variables above.
- Standards and lifecycle: the plan follows the utility's information security standard and the Power Platform Well-Architected Security checklist. Changes go through source control with a second maker's review, and the pipeline service principal deploys to Test and Prod. The threat model is a one-page list kept in the repo.
- Identity and segmentation: each environment has its own service account. Access is by security group, never by user. Makers have no role in Prod. The operations manager reviews the Prod role assignments every quarter.
- Encryption: the platform's own encryption at rest and in transit. No custom encryption and no exports of the tables.
- Monitoring and incidents: Dataverse auditing is on for all three tables. A failed run of either flow emails the operations mailbox, and the audit log is reviewed monthly. The platform team lead owns incident response. The runbook is to turn off both flows, remove the affected role assignments and report to the information security office, and it is rehearsed once before go-live.
- Testing: one test user per role in Test, including a user with no role who must be denied. The flows are tested with `powerops test` before each import.
- Deadline: live before the winter inspection season starts in November.

## Assumptions

- Environment facts were not read from a tenant. The publisher and the three environments exist.
- Photos stay under 2 MB each, so the Dataverse file column is enough.

## Open questions

## Questions for your admin

## Steps

1. Create the solution `SiteInspections` with publisher prefix `wtr` in Dev.
2. Create the three tables and the three security roles. Check with a test user that an inspector cannot read another region.
3. Build the canvas app with the four screens. Check that every form has a working Save, Cancel and Back.
4. Build the two flows with their connection references and environment variables. Run `powerops test` on both.
5. Export from Dev, run the solution checker and fix what it reports.
6. Import as managed into Test with the pipeline. Run the role checks with one user per role and the urgent finding path end to end.
7. After the operations manager approves, import the same package into Prod and set the environment variables.
8. Run the incident drill once and turn on the flows.
