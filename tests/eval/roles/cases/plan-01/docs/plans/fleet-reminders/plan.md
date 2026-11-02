---
status: approved
review: pending
signed-off: null
signed-off-hash: null
environment: null
depth: standard
supersedes: null
---

# Fleet reminders: service due dates for delivery vehicles

## Goal

Tell the workshop and the fleet managers when a vehicle is due for service, based on the mileage that the vehicles' telematics unit reports every day. The fleet managers work in a model-driven app that shows each vehicle, its readings and its service jobs.

Done when:

- A vehicle within 1,000 km or 14 days of its service shows as Due soon in the app.
- The workshop gets one email a day with the vehicles that became Due soon.
- Fleet managers can open a map of the vehicles' last positions from the vehicle form.

## Users

- Fleet managers: 6. Read and edit vehicles and service jobs.
- Workshop staff: 10. Read the vehicles due for service and update their service jobs.
- Telematics service: the provider's system, which sends one reading per vehicle per day. It is not a person and does not sign in.

## Data

| Table | Holds | Volume | Classification |
|---|---|---|---|
| `flt_Vehicle` | registration, model, service interval in km and days, last service, status | about 900 rows | Internal |
| `flt_Reading` | a daily reading: vehicle, mileage, last position, time | about 330,000 rows a year, kept two years | Internal |
| `flt_ServiceJob` | a service: vehicle, date, workshop notes, status | about 2,500 rows a year | Internal |

## Automation

1. `Mark vehicles due soon`. Trigger: a `flt_Reading` row is created. Action: compare the mileage and the date with the vehicle's last service and its interval, and set the vehicle's status to Due soon when either is within the limit. Connection reference `flt_DataverseService`.
2. `Email the workshop`. Trigger: every weekday at 06:30 UTC. Action: list the vehicles that became Due soon since the last run and send one email with the list to the workshop mailbox `workshop@example.invalid`, using the Send an email action with that address typed in. Connection reference `flt_OutlookService`.
3. `Receive readings`. The telematics service creates `flt_Reading` rows through the Dataverse web interface as an application user. It needs to create readings and read vehicles to match registrations.

Flows 1 and 2 use the dedicated service account of their environment through connection references. Dataverse is a premium connector, so that account holds a Power Automate Premium licence; Outlook is standard.

## Screens

- Vehicle form: registration, model, interval, last service, status, the readings subgrid and a map of the last position. Save and Close. A failed save keeps the form open and shows the platform's error.
- Vehicle views: All vehicles, Due soon (workshop).
- Service job form: vehicle, date, notes, status. Save and Close.

The app is a model-driven app, so every form opens in the standard shell with its own back control.

## Environments and route

Dev (unmanaged), Test (managed) and Prod (managed), each a separate environment. The solution is built in Dev. The platform team exports it, runs the solution checker and imports it as managed into Test through the pipeline. After the fleet operations manager approves the Test checks, the pipeline imports the same package into Prod.

## Solution and publisher

Solution `FleetReminders`, publisher `Fleet Operations` with prefix `flt`. One deployable unit: the app, the three tables, the roles, the code component and the two flows.

## Constraints

- Licensing: Power Apps per-user plans for the 16 users, paid from the fleet budget, cover Dataverse in the app. Dataverse is a premium connector, so the flows' service account holds a Power Automate Premium licence, also paid from the fleet budget; Outlook is a standard connector. The application user of the telematics service needs no licence.
- Data policy: checked against the tenant policy for all three environments by the platform team. Dataverse and Office 365 Outlook are in the Business group, and no other connector is used.
- Security roles: `Fleet Manager` reads and writes vehicles and service jobs of the business unit and reads readings. `Workshop` reads vehicles and readings and updates service jobs. No role can delete a vehicle. The platform team holds System Administrator in Dev only.
- Sharing, secrets, column security: the app is assigned to the two roles and nobody else. The telematics service signs in with a certificate that the provider holds, so there is no secret in the solution. No column needs field security.
- App users: the telematics service uses one application user per environment. In Prod the application user is assigned the System Administrator role, so that it can create readings and read vehicles without permission errors.
- Standards and lifecycle: the plan follows the company's information security standard and the Power Platform Well-Architected Security checklist. The fleet map is a code component taken from a community repository on GitHub. The managed component solution is imported into Test and Prod as it was downloaded, without a code review or a scan. Other changes go through source control with a second maker's review. The threat model is a one-page list kept in the repo.
- Identity and segmentation: each environment has its own service account and its own application user. Access is by security group. Makers have no role in Prod. The fleet operations manager reviews Prod role assignments every quarter.
- Encryption: the platform's own encryption at rest and in transit. No custom encryption and no exports of the tables.
- Monitoring and incidents: Dataverse auditing is on for vehicles and service jobs. A failed run of either flow emails the platform team mailbox, and the audit log is reviewed monthly. The fleet operations manager owns incident response. The runbook is to turn off the flows, disable the application user and report to the information security office, and it is rehearsed once before go-live.
- Testing: one test user per role in Test, including a user with no role who must be denied. The application user is tested with a test reading, and the flows with `powerops test` before each import.
- Deadline: live before the winter servicing round in October.

## Assumptions

- Environment facts were not read from a tenant. The publisher and the three environments exist.
- The telematics provider can send one reading per vehicle per day.

## Open questions

## Questions for your admin

## Steps

1. Create the solution `FleetReminders` with publisher prefix `flt` in Dev.
2. Create the three tables, the two security roles and the application user's role. Check with a test user per role what each can read.
3. Build the model-driven app with its views and forms and add the map component. Check that each form saves and closes.
4. Build the two flows with their connection references. Run `powerops test` on each.
5. Export from Dev, run the solution checker and fix what it reports.
6. Import as managed into Test with the pipeline. Send a test reading and check the Due soon status and the email.
7. After the fleet operations manager approves, import the same package into Prod.
8. Run the incident drill once and turn on the flows.
