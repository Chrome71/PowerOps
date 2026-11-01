---
status: approved
review: pending
signed-off: null
signed-off-hash: null
environment: null
depth: standard
supersedes: null
---

# Volunteer shifts: scheduling for the food bank

## Goal

Replace the shared spreadsheet that the food bank uses to staff its sorting and delivery shifts. Coordinators publish shifts in a canvas app, volunteers sign up through a form, and volunteers get a text message to confirm and another the day before.

Done when:

- A coordinator publishes a month of shifts in under ten minutes.
- A volunteer who signs up gets a confirmation text within two minutes and a reminder the day before.
- The coordinators get a weekly list of shifts that are short of volunteers.

## Users

- Coordinators: 5. Create shifts and edit signups.
- Site leads: 8. See the shifts of their site and mark attendance.
- Volunteers: about 600. They sign up with a Microsoft Forms form and receive text messages. They do not open the app.

## Data

| Table | Holds | Volume | Classification |
|---|---|---|---|
| `vol_Volunteer` | name, mobile number, skills, availability | about 600 rows | Confidential |
| `vol_Shift` | date, slot, site, role, capacity | about 150 rows a month | Internal |
| `vol_Signup` | a volunteer on a shift: status, attended | about 6,000 rows a year | Internal |

## Automation

1. `Confirm a signup`. Trigger: a `vol_Signup` row is created. Action: send a confirmation text through the text message gateway's web interface with the HTTP connector. The address is in environment variable `vol_SmsGatewayAddress` and the gateway token in environment variable `vol_SmsGatewayToken`, of type Text, whose value is set at each import. Connection reference `vol_DataverseService`.
2. `Remind volunteers`. Trigger: every day at 16:00 UTC. Action: find the signups for tomorrow and send each volunteer a text the same way.
3. `Weekly shortage list`. Trigger: every Monday at 07:00 UTC. Action: post the shifts with fewer signups than capacity to the coordinators' Teams channel. Connection reference `vol_TeamsService`. Environment variable `vol_CoordinatorsChannel` holds the channel for each environment.

The flows run under the dedicated service account of their environment through connection references.

## Screens

- Shifts: shifts of the next 30 days, grouped by site, with Open and Back.
- Shift form: date, slot, site, role, capacity. Buttons: Save, Cancel and Back. A failed save keeps the form open and shows the error.
- Signups: the volunteers on a shift, with attendance toggles for site leads. Buttons: Save, Cancel and Back.

## Environments and route

Dev (unmanaged), Test (managed) and Prod (managed), each a separate environment. The solution is built in Dev. The platform team exports it, runs the solution checker and imports it as managed into Test through the pipeline. After the head coordinator approves the Test checks, the pipeline imports the same package into Prod.

## Solution and publisher

Solution `VolunteerShifts`, publisher `Food Bank Operations` with prefix `vol`. One deployable unit: the app, the three tables, the roles and the three flows.

## Constraints

- Licensing: the HTTP connector is premium. The head coordinator holds the Power Automate Premium per-user licence that owns the flow connections, paid from the food bank's grant for volunteer systems. The 13 app users hold Power Apps plans from the same grant.
- Data policy: checked against the tenant policy for all three environments by the platform team. Dataverse, Teams and HTTP are in the Business group in all three, and no other connector is used.
- Security roles: `Coordinator` reads and writes volunteers, shifts and signups and cannot delete volunteers. `Site Lead` reads shifts and signups of the user's site and updates attendance. The platform team holds System Administrator in Dev only.
- Sharing, app users, column security: the canvas app is shared with the Coordinators and Site Leads groups only. No application user. Mobile numbers are readable by the Coordinator role only, through table permissions, and no column needs field security.
- Standards and lifecycle: the plan follows the food bank's data protection policy and the Power Platform Well-Architected Security checklist. Changes go through source control with a second maker's review, and the pipeline service principal deploys to Test and Prod. The threat model is a one-page list kept in the repo.
- Identity and segmentation: each environment has its own service account. Access is by security group. Makers have no role in Prod. The head coordinator reviews Prod role assignments every quarter.
- Encryption: the platform's own encryption at rest and in transit. The gateway call uses TLS. No custom encryption.
- Monitoring and incidents: Dataverse auditing is on for `vol_Volunteer`. A failed text emails the coordinators' shared mailbox, set in environment variable `vol_CoordinatorsMailbox`, and the audit log is reviewed monthly. The head coordinator owns incident response. The runbook is to turn off the flows, revoke the gateway token and report to the data protection lead, and it is rehearsed once before go-live.
- Testing: the coordinators test the app and the flows in Test with their own accounts, which hold System Administrator there, and write the result of each check in a test log. The Site Lead restrictions are not tested separately.
- Deadline: live before the winter appeal in November.

## Assumptions

- Environment facts were not read from a tenant. The publisher and the three environments exist.
- The text message gateway is already under contract and has a test number.

## Open questions

- Does `vol_Shift` replace the Event table of the existing Volunteer Hub solution, or sit beside it?
- Which channel receives the weekly list in Prod?

## Questions for your admin

## Steps

1. Create the solution `VolunteerShifts` with publisher prefix `vol` in Dev.
2. Create the three tables and the two security roles.
3. Build the canvas app with its three screens. Check that every form has a working Save, Cancel and Back.
4. Build the three flows with their connection references and environment variables. Run `powerops test` on each.
5. Export from Dev, run the solution checker and fix what it reports.
6. Import as managed into Test with the pipeline. Sign up with a test volunteer and check both texts.
7. After the head coordinator approves, import the same package into Prod and set the environment variables.
8. Run the incident drill once and turn on the flows.
