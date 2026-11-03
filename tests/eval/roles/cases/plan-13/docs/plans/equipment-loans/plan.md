---
status: approved
review: pending
signed-off: null
signed-off-hash: null
environment: null
depth: standard
supersedes: null
---

# Equipment loans at the four depots

## Goal

Give the council's facilities department one record of the equipment that staff borrow from its four depots (laptops, projectors, cameras and power tools), so that items stop going missing between stock checks. Store keepers work in a canvas app at each depot counter, and staff see their own loans in the same app.

Goal lines:

1. A store keeper records each loan and each return at the counter, with who has which item and until when.
2. Each member of staff sees their own open loans, and gets a reminder when one is overdue.
3. Each team lead gets the overdue loans of their team by email every morning.
4. A stock check once a term finds every item that is neither in its depot nor on loan.

### Won't build

Nothing was cut.

## Users

- Staff: about 300. Read the equipment list and the loans; the app shows each person their own.
- Store keepers: 4, one per depot. Record loans and returns, edit the equipment of their depot and run the stock check.
- Team leads: 8. Read all loans, and get the overdue loans of their team by email each morning.
- Facilities manager: 1. Owns the lists and approves releases.

## Data

| Artefact | Kind | Store | Goal line | Reuse, Extend or Create | Why not a column or choice | Rows now, 3 years |
|---|---|---|---|---|---|---|
| Equipment | List | SharePoint; all eight gates hold (Constraints, Store gates) | 1, 4 | Extend: the store keepers' existing Equipment list on the depots site gains the columns Depot, Condition, On loan and Last checked | Each item has attributes of its own and many loans | 600, 700 |
| Loans North | List | SharePoint; all eight gates hold | 1, 2, 3 | Create: checked the Equipment list (one row per item, no history) | One list per depot keeps each list small and under the 5,000-item view threshold | 0, 825 |
| Loans South | List | SharePoint; all eight gates hold | 1, 2, 3 | Create: checked the Equipment list (one row per item, no history) | As Loans North | 0, 825 |
| Loans East | List | SharePoint; all eight gates hold | 1, 2, 3 | Create: checked the Equipment list (one row per item, no history) | As Loans North | 0, 825 |
| Loans West | List | SharePoint; all eight gates hold | 1, 2, 3 | Create: checked the Equipment list (one row per item, no history) | As Loans North | 0, 825 |
| Loan statuses | List | SharePoint; all eight gates hold | 1 | Create: checked the Equipment list's columns first | Lists are the council's habit for values like these; no other app uses them | 4, 4 |
| Equipment categories | List | SharePoint; all eight gates hold | 1, 4 | Create: checked the Equipment list's columns first | As Loan statuses | 5, 6 |
| Conditions | List | SharePoint; all eight gates hold | 1 | Create: checked the Equipment list's columns first | As Loan statuses | 3, 3 |
| Depots | List | SharePoint; all eight gates hold | 1, 4 | Create: checked the Equipment list's columns first | As Loan statuses | 4, 4 |
| Loan feedback | List | SharePoint; all eight gates hold | 1 | Create: checked the Equipment list and the council's staff survey tool, which has no form per loan | Staff can say how the loan went, and the facilities manager can read it | 0, 2,000 |

Columns:

- Equipment: Title, Category (lookup to Equipment categories), Depot (lookup to Depots), Condition (lookup to Conditions), On loan (yes/no), Last checked (date).
- Loans North, Loans South, Loans East and Loans West, each: Item (lookup to Equipment), Borrower email (text), Borrower name (text), Team (choice of the eight teams), Borrowed (date), Due (date), Returned (date), Status (lookup to Loan statuses).
- Loan statuses, Equipment categories, Conditions and Depots, each: Title.
- Loan feedback: Loan (text, the loan's list and item number), Rating (number, 1 to 5), Comment (text).

All lists are classified Internal: who borrowed what is visible to all staff, which the council's privacy officer agreed. A retention policy deletes loans three years after their return date, and version history is on for every list.

## Automation

| Flow | Goal line |
|---|---|
| Overdue reminders North | 2, 3 |
| Overdue reminders South | 2, 3 |
| Overdue reminders East | 2, 3 |
| Overdue reminders West | 2, 3 |
| Ask for feedback | 1 |

**Overdue reminders North, South, East and West**, one per depot's loans list, each:

- Goal lines 2 and 3. No existing flow sends reminders, so they are new.
- Trigger: a schedule, once a day at 07:00, so one run a day each.
- Rows per run: the open loans of that depot past their due date, about 5 and at most 40. Get items filters on Status equal to Out and Due before today, with Pagination on and a threshold of 5,000.
- Steps: 1. Get the overdue loans. 2. For each loan, send the borrower a reminder from the depots' shared mailbox. 3. Group the loans by team and send each team lead one list. About 8 actions, nesting depth 1.
- Connectors: SharePoint and Office 365 Outlook, both standard, through the connection references `fac_SharePointService` and `fac_OutlookService`. The environment variable `fac_RemindersMailbox` holds the shared mailbox for each environment.
- Failure path: a failed run emails the facilities systems mailbox, and the next run picks up the same loans.
- Retry and concurrency: the defaults.
- Tested with `powerops test` against fake loans before each import.

**Ask for feedback.**

- Goal line 1. New.
- Trigger: an item in any of the four loans lists is modified and its Status becomes Returned, about 5 runs a day.
- Rows per run: one loan.
- Steps: 1. Read the returned loan. 2. Email the borrower a link to the Feedback screen. About 3 actions, nesting depth 0.
- Connectors and failure path: as the reminder flows. Retry and concurrency: the defaults.
- Tested with `powerops test` before each import.

## Screens

| Screen | Goal line |
|---|---|
| Equipment | 1 |
| Loan | 1 |
| Return | 1 |
| My loans | 2 |
| Stock check | 4 |
| Feedback | 1 |

- Equipment: search by name with `StartsWith(Title, txtSearch.Text)` and filter by depot with `Depot.Value = drpDepot.Selected.Title`, both delegable to SharePoint. Buttons: Lend (store keepers only) and Back.
- Loan (store keepers): borrower, team and due date for the chosen item, saved to the loans list of the store keeper's depot. Buttons: Save, Cancel and Back. A failed save keeps the form open and shows the error.
- Return (store keepers): the item's open loan, with the condition on return. Buttons: Confirm, Cancel and Back. A failed save keeps the screen open and shows the error.
- My loans: the open loans of the four loans lists, each read with the delegable `Filter(<list>, 'Borrower email' = varMyEmail && Status.Value = "Out")` and shown in one gallery. Buttons: Back.
- Stock check (store keepers): the equipment of one depot with On loan equal to no, where the store keeper ticks each item found; the screen then lists the items not ticked. Buttons: Finish, Cancel and Back.
- Feedback: a rating and a comment for one returned loan. Buttons: Send, Cancel and Back. A failed save keeps the form open and shows the error.

## Environments and route

Dev (unmanaged), Test (managed) and Prod (managed), each with its own depots site. The solution is built in Dev. An Azure DevOps pipeline exports it as managed, runs `powerops analyze` and the solution checker, and imports it into Test with `pac solution import` under the pipeline's service principal. After the store keepers' acceptance test in Test, the facilities manager approves the release in the pipeline, which imports the same package into Prod. Power Platform pipelines aren't used, because they need Managed Environments (Constraints, Store gates).

The lists aren't in the solution. A provisioning script in the repo creates their columns in each environment's site, and the app and the flows reach them through one data source environment variable per list and `fac_DepotsSite` for the site.

## Solution and publisher

Solution `EquipmentLoans`, publisher `Facilities Systems` with prefix `fac`. One deployable unit: the canvas app, the five flows, the two connection references and the twelve environment variables.

## Constraints

- Licensing: every user holds a Microsoft 365 E3 licence, which covers canvas apps and flows that use standard connectors. Every connector here is standard. None of the three environments is a Managed Environment, and the plan keeps them so.
- Data policy: checked by the platform team for all three environments. SharePoint, Office 365 Outlook and Office 365 Users are in the Business group, and no other connector is used.
- Permissions: on each loans list, staff have Read and the app shows them only their own loans; store keepers have a permission level that adds and edits items but can't delete them; team leads have Read. On Equipment and the four lists of values, staff and team leads have Read, and store keepers add and edit items without delete. On Loan feedback, staff add items and the facilities manager reads them. The facilities systems team owns each site. Access is by security group, never by user.
- Sharing, app users, secrets: the app is shared with the Staff, Store keepers and Team leads groups, with no co-owner outside the facilities systems team. The flows run as the dedicated service account of each environment through the connection references. No application user and no secret.
- Store gates, all eight checked: each loans list stays under 1,000 rows (about 275 loans a year per depot, deleted three years after return), and every other list is smaller; every app query above delegates; Equipment has three lookups and each loans list two; each reminder flow runs once a day, with Pagination on, over at most 40 rows, and the feedback flow about five times a day over one row; list permission is enough, because every loan is Internal and readable by all staff; no screen works offline, since every depot has Wi-Fi; the licence saving is real, since no environment is a Managed Environment and every user's E3 licence covers the app; the facilities systems team owns every list's columns and the provisioning script.
- Monitoring and incidents: a failed run of any flow emails the facilities systems mailbox. The facilities manager reviews the lists' version history when an item goes missing.
- Testing: one test user per group in Test, including a staff user who must not edit a loan. The flows are tested with `powerops test` before each import.
- Deadline: live before the spring stock check.

## Assumptions

- Environment facts were not read from a tenant. The three environments and their depots sites exist.
- The store keepers' existing Equipment list holds every item, and its 600 rows are correct.

## Open questions

## Questions for your admin

## Steps

1. Create the solution `EquipmentLoans` with publisher prefix `fac` in Dev.
2. Run the provisioning script on the Dev site: add the columns to Equipment and create the nine new lists. Check the columns and the permissions with a test user per group.
3. Add the twelve environment variables and the two connection references.
4. Build the canvas app with its six screens. Check that every query shows no delegation warning, and that every form has a working Save, Send or Confirm, Cancel and Back.
5. Build the five flows and run `powerops test` on each.
6. Run the pipeline to Test. Run the provisioning script on the Test site, lend and return an item at each depot, let the reminders run, send feedback, and do a stock check of one depot.
7. After the facilities manager approves, release the same package to Prod, run the provisioning script on the Prod site and set the environment variables.
