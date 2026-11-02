---
status: approved
review: pending
signed-off: null
signed-off-hash: null
environment: null
depth: standard
supersedes: null
---

# Training registration: courses and sessions for employees

## Goal

Let employees browse the company's training courses, register for a session and cancel, and let trainers manage sessions and attendance. The course list comes from the external training vendor's catalogue, and virtual sessions get a Teams meeting automatically.

Done when:

- An employee registers for a session in under a minute and receives a confirmation.
- A trainer sees the attendee list of each session and marks attendance.
- Virtual sessions have a Teams meeting link in the confirmation, and the course list matches the vendor's catalogue within a day.

## Users

- Employees: about 2,000. Browse courses, register and cancel.
- Trainers: 12. Create sessions, see attendees and mark attendance.
- HR training lead: 1. Reads all registrations and exports nothing.

## Data

| Table | Holds | Volume | Classification |
|---|---|---|---|
| `trn_Course` | title, description, vendor code, format, duration | about 300 rows | Public |
| `trn_Session` | a date of a course: course, trainer, capacity, place or meeting link | about 3,000 rows a year | Internal |
| `trn_Registration` | an employee on a session: status, attended | about 30,000 rows a year | Internal |

## Automation

1. `Import the catalogue`. Trigger: every night at 02:00 UTC. Action: read the vendor's course list with the custom connector `Course Catalogue`, then create or update `trn_Course` rows in Dataverse by vendor code. Connection references `trn_CatalogueVendor` and `trn_DataverseService`. Environment variable `trn_CatalogueAddress` holds the vendor's address for each environment.
2. `Confirm a registration`. Trigger: a `trn_Registration` row is created. Action: create a Teams meeting if the session is virtual, and email the employee the confirmation. Connection references `trn_TeamsService` and `trn_OutlookService`. Environment variable `trn_TrainingMailbox` holds the sender for each environment.
3. `Remind attendees`. Trigger: every weekday at 07:00 UTC. Action: email each employee registered for a session the next day. Connection reference `trn_OutlookService`.

## Screens

- Courses: course cards for the next 90 days. The gallery uses the delegable `Filter(trn_Session, trn_Date >= Today() && trn_Status = 'Open')`. Buttons: Open, Back.
- Session detail: date, trainer, places left, a Register button. Buttons: Register, Back. A failed registration shows the error and keeps the screen open.
- My registrations: the employee's sessions, with a Cancel registration button that asks for confirmation.
- Trainer view: attendees of a session with attendance toggles. Buttons: Save, Cancel and Back.

## Environments and route

Dev (unmanaged), Test (managed) and Prod (managed), each a separate environment. The solution is built in Dev. The platform team exports it, runs the solution checker and imports it as managed into Test through the pipeline. After the HR training lead approves the Test checks, the pipeline imports the same package into Prod.

## Solution and publisher

Solution `TrainingRegistration`, publisher `HR Learning` with prefix `trn`. One deployable unit: the app, the three tables, the roles, the custom connector and the three flows.

## Constraints

- Licensing: a pay-as-you-go plan linked to the Prod environment and billed to the HR budget covers the 2,000 employees' use of the app. Trainers and the HR training lead hold Power Apps per-user plans. The custom connector is premium, so the service account that owns the flows holds a Power Automate Premium per-user licence, paid from the HR budget.
- Data policy: the platform team gave this list for all three environments. Business group: Dataverse, Microsoft Teams and Office 365 Outlook. Non-business group: the custom connector `Course Catalogue` and every connector not listed.
- Security roles: `Employee` creates and cancels registrations and reads the sessions and courses; it reads only the user's own registrations. `Trainer` reads and updates sessions and registrations of the trainer's courses. `Training Lead` reads everything. No role can delete a course. The platform team holds System Administrator in Dev only.
- Sharing, app users, secrets, column security: the canvas app is shared with the Employees group, and trainers have no co-owner right. The vendor catalogue is public, so there is no secret. No application user and no secured column.
- Standards and lifecycle: the plan follows the company's information security standard and the Power Platform Well-Architected Security checklist. Changes go through source control with a second maker's review, and the pipeline service principal deploys to Test and Prod. The threat model is a one-page list kept in the repo.
- Identity and segmentation: a single service account owns the connection references in Dev, Test and Prod, to keep the number of accounts small. Access is by security group, never by user. Makers have no role in Prod. The HR training lead reviews Prod role assignments every quarter.
- Encryption: the platform's own encryption at rest and in transit. No custom encryption and no exports of the tables.
- Monitoring and incidents: Dataverse auditing is on for `trn_Registration`. A failed run of any flow emails the platform team mailbox, and the audit log is reviewed monthly. The HR training lead owns incident response. The runbook is to turn off the flows, remove the affected role assignments and report to the information security office, and it is rehearsed once before go-live.
- Testing: one test user per role in Test, including a user with no role who must be denied. The flows are tested with `powerops test` before each import.
- Deadline: live before the January intake.

## Assumptions

- Environment facts were not read from a tenant. The publisher and the three environments exist.
- The vendor's catalogue interface is public and read-only.

## Open questions

## Questions for your admin

## Steps

1. Create the solution `TrainingRegistration` with publisher prefix `trn` in Dev.
2. Create the three tables and the three security roles. Check with a test user per role what each can read.
3. Build the canvas app with its four screens. Check that every form has a working Save, Cancel and Back.
4. Build the custom connector and the three flows with their connection references and environment variables. Run `powerops test` on each.
5. Export from Dev, run the solution checker and fix what it reports.
6. Make the booking screens intuitive for employees.
7. Ensure good performance when many employees register at once.
8. Import as managed into Test, and after the HR training lead approves, into Prod. Set the environment variables, run the incident drill once and turn on the flows.
