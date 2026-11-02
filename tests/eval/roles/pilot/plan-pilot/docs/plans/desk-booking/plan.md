---
status: approved
review: pending
signed-off: null
signed-off-hash: null
environment: null
depth: standard
supersedes: null
---

# Desk booking: shared desks in the head office

## Goal

Let employees book a shared desk for a day from a canvas app, and let the facilities team see which desks are taken and which employees need an adjusted desk. Bookings that are not used are released automatically.

Done when:

- An employee books a desk for a day in under 30 seconds.
- A booking not checked in by 10:00 is released and the desk shows as free.
- Facilities sees the desks with adjusted equipment that are booked for the next five days.

## Users

- Employees: about 600 in the head office. Book and cancel their own desks.
- Facilities team: 4. Maintain the desk list and see the bookings.
- Office manager: 1. Reads the usage report.

## Data

| Table | Holds | Volume | Classification |
|---|---|---|---|
| `dsk_Desk` | desk code, floor, zone, equipment | 320 rows | Internal |
| `dsk_Booking` | a booking: desk, employee, date, status, adjusted desk needed | about 90,000 rows a year, kept one year | Internal |
| `dsk_Need` | an employee's recorded need for an adjusted desk, with the reason | about 40 rows | Restricted |

## Automation

1. `Release unused bookings`. Trigger: every weekday at 10:00 UTC. Action: set the status of today's bookings without a check-in to Released. Connection reference `dsk_DataverseService`.
2. `Weekly usage report`. Trigger: every Monday at 07:00 UTC. Action: email the office manager the booking counts per zone for the week before. Connection reference `dsk_OutlookService`. Environment variable `dsk_ReportsMailbox` holds the address for each environment.

Both flows use the dedicated service account of their environment through connection references, and use standard connectors only.

## Screens

- Floor plan: the desks of one floor with their state for a day. Buttons: Book, Back.
- Book: confirm the desk and the date. Buttons: Confirm, Cancel and Back. A failed booking keeps the screen open and shows the error.
- My bookings: the employee's bookings with a Cancel booking button that asks for confirmation.
- Facilities view: the bookings for the next five days that need an adjusted desk. Buttons: Back.

## Environments and route

Dev (unmanaged), Test (managed) and Prod (managed), each a separate environment. The solution is built in Dev. The platform team exports it, runs the solution checker and imports it as managed into Test through the pipeline. After the office manager approves the Test checks, the pipeline imports the same package into Prod.

## Solution and publisher

Solution `DeskBooking`, publisher `Workplace Services` with prefix `dsk`. One deployable unit: the app, the three tables, the roles, the field security profile and the two flows.

## Constraints

- Licensing: Power Apps per-user plans for the 5 facilities and office staff and a pay-as-you-go plan for the 600 employees, billed to the facilities budget. The flows use only standard connectors (Dataverse and Outlook).
- Data policy: checked against the tenant policy for all three environments by the platform team. Dataverse and Office 365 Outlook are in the Business group, and no other connector is used.
- Security roles: `Employee` creates and cancels the user's own bookings and reads desks. `Facilities` reads and updates desks and reads bookings. `Office Manager` reads bookings. No role can delete a booking.
- Column security: the `dsk_Reason` column of `dsk_Need` is secured with the field security profile `Adjusted Desk Needs`, which gives Read to the Employees team and Read and Update to the Facilities team.
- Sharing, app users, secrets: the canvas app is shared with the Employees group, and nobody has co-owner rights outside the platform team. No application user and no secret.
- Standards and lifecycle: the plan follows the company's information security standard and the Power Platform Well-Architected Security checklist. Changes go through source control with a second maker's review, and the pipeline service principal deploys to Test and Prod. The threat model is a one-page list kept in the repo.
- Identity and segmentation: each environment has its own service account. Access is by security group, never by user. Makers have no role in Prod. The office manager reviews Prod role assignments every quarter.
- Encryption: the platform's own encryption at rest and in transit. No custom encryption and no exports of the tables.
- Monitoring and incidents: Dataverse auditing is on for `dsk_Need`. A failed run of either flow emails the facilities mailbox, and the audit log is reviewed monthly. The office manager owns incident response. The runbook is to turn off the flows, remove the affected role assignments and report to the information security office, and it is rehearsed once before go-live.
- Testing: one test user per role in Test, including a user with no role who must be denied. The flows are tested with `powerops test` before each import.
- Deadline: live before the office reopens after the refit in March.

## Assumptions

- Environment facts were not read from a tenant. The publisher and the three environments exist.
- Employees check in at the desk with a button in the app.

## Open questions

## Questions for your admin

## Steps

1. Create the solution `DeskBooking` with publisher prefix `dsk` in Dev.
2. Create the three tables, the three security roles and the field security profile. Check with a test user per role what each can read.
3. Build the canvas app with its four screens. Check that every form has a working Confirm, Cancel and Back.
4. Build the two flows with their connection references and environment variable. Run `powerops test` on both.
5. Export from Dev, run the solution checker and fix what it reports.
6. Import as managed into Test with the pipeline. Book a desk, let it lapse and check the release.
7. After the office manager approves, import the same package into Prod and set the environment variable.
8. Run the incident drill once and turn on the flows.
