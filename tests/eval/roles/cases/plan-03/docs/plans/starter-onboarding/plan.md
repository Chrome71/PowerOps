---
status: approved
review: pending
signed-off: null
signed-off-hash: null
environment: null
depth: standard
supersedes: null
---

# Starter onboarding: checklists for new employees

## Goal

Give HR a model-driven app that runs the checklist for each new employee. HR coordinators register a starter, tasks for IT, facilities and payroll are created from a template, task owners complete them, and the line manager is told when everything is ready for the first day.

Done when:

- A registered starter gets its full task list within one minute.
- Task owners see only their own team's tasks, and a line manager sees only the starters of their own team.
- The manager is notified when the last task of a starter is complete.

## Users

- HR coordinators: 8. Register starters and edit any starter or task.
- Line managers: about 120. Read the starters of their own team and nothing else.
- Task owners in IT and facilities: 15. Update the tasks assigned to their team.
- Payroll officers: 4. Read starters and complete the payroll tasks.

## Data

| Table | Holds | Volume | Classification |
|---|---|---|---|
| `hrs_Starter` | name, start date, manager, job title, team, bank account number, national ID number | about 600 rows a year | Confidential |
| `hrs_Task` | a checklist task: starter, owning team, due date, status | about 9,000 rows a year | Internal |
| `hrs_TaskTemplate` | the standard tasks created for every starter | 40 rows | Internal |

The bank account and national ID columns are Restricted: only HR coordinators and payroll officers may read them. Managers and task owners never see them.

## Automation

1. `Create onboarding tasks`. Trigger: a `hrs_Starter` row is created. Action: for each row of `hrs_TaskTemplate`, create a `hrs_Task` row with the due date counted back from the start date. Connection reference `hrs_DataverseService`.
2. `Remind task owners`. Trigger: every weekday at 08:00 UTC. Action: find tasks due within two days and not complete, and email each owning team's shared mailbox. Connection reference `hrs_OutlookService`. Environment variable `hrs_ReminderSender` holds the sender mailbox for each environment.
3. `Tell the manager everything is ready`. Trigger: a `hrs_Task` row is updated to Complete. Condition: no open task is left for the starter. Action: email the starter's manager. Connection reference `hrs_OutlookService`.

All three flows use the service account of their environment through connection references, and use standard connectors only.

## Screens

- Starter form: all starter fields, with Save and Close. A failed save shows the platform's error and keeps the form open.
- Starter views: All starters (HR only), My team's starters (managers).
- Task views: My tasks (task owners), Tasks due this week (HR).

The app is a model-driven app, so every form opens in the standard shell with its own back and close controls.

## Environments and route

Dev (unmanaged) and Prod (managed). The lead maker builds in Dev and tests there with the HR coordinators, then exports a managed solution. The HR systems owner approves the import and the lead maker imports it into Prod. Environment variable values and connection references are set at the import.

## Solution and publisher

Solution `StarterOnboarding`, publisher `HR Systems` with prefix `hrs`. One deployable unit: the app, the three tables, the roles, the field security profile and the three flows.

## Constraints

- Licensing: Power Apps per-app plans for the 120 line managers and 15 task owners, and per-user plans for the 12 HR and payroll staff, paid from the HR budget. The flows use only standard connectors (Dataverse and Outlook).
- Data policy: checked against the tenant policy for both environments by the platform team. Dataverse and Office 365 Outlook are in the Business group, and no other connector is used.
- Security roles: `HR Coordinator` has full access to starters and tasks of the business unit. `Line Manager` reads starters whose manager is the user's team. `Task Owner` reads and updates tasks of the user's team. `Payroll Officer` reads starters and updates payroll tasks. No role can delete a starter. The platform team holds System Administrator in Dev only.
- Sharing: the model-driven app is assigned to the four roles and nobody else. Nothing is shared by link.
- Column security: the bank account and national ID columns are secured. The field security profile `Starter Sensitive` gives Read and Update on both columns to the HR coordinators team, the payroll officers team and the line managers team.
- App users and secrets: no application user and no secret. The flows hold only the environment variable above.
- Standards and lifecycle: the plan follows the company's HR data protection standard and the Power Platform Well-Architected Security checklist. Changes are made through source control with a second maker's review, and the lead maker deploys. The threat model is a one-page list kept in the repo.
- Identity and segmentation: each environment has its own service account. Access is by security group, never by user. Makers have no role in Prod. The HR systems owner reviews the Prod role assignments every quarter.
- Encryption: the platform's own encryption at rest and in transit. No custom encryption and no exports of the tables.
- Auditing and incidents: Dataverse auditing is off for all tables, to save database capacity. Flow failures are visible in each flow's run history. The HR systems owner owns incident response. The runbook is to turn off the flows, remove the affected role assignments and report to the data protection officer, and it is rehearsed once before go-live.
- Testing: one test user per role in Dev, including a user with no role who must be denied. The flows are tested with `powerops test` before each import.
- Deadline: live before the January intake.

## Assumptions

- Environment facts were not read from a tenant. The publisher, Dev and Prod exist.
- The team template of 40 tasks is complete and HR maintains it.

## Open questions

## Questions for your admin

## Steps

1. Create the solution `StarterOnboarding` with publisher prefix `hrs` in Dev.
2. Create the three tables, the four security roles and the field security profile. Check with one test user per role what each can read.
3. Build the model-driven app with the starter and task views. Check that the form saves and closes, and that a failed save keeps the form open.
4. Build the three flows with their connection references and the environment variable. Run `powerops test` on each.
5. Export from Dev, run the solution checker and fix what it reports.
6. Test with the HR coordinators in Dev: register two starters and complete their tasks.
7. After the HR systems owner approves, import the managed solution into Prod and set the environment variable.
8. Run the incident drill once and turn on the flows.
