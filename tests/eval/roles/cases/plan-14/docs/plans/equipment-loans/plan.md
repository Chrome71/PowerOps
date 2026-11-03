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

- Booking an item in advance: staff ask the depot by email today, and no goal line needs it in this version.
- A damage report workflow: the store keeper sets the item's condition on return.
- Barcode labels and scanning: 600 items are found quickly by name, and labels can come in a later version.

## Users

- Staff: about 300. Read the equipment list and the loans; the app shows each person their own.
- Store keepers: 4, one per depot. Record loans and returns, edit the equipment of their depot and run the stock check.
- Team leads: 8. Read all loans, and get the overdue loans of their team by email each morning.
- Facilities manager: 1. Owns the two lists and approves releases.

## Data

| Artefact | Kind | Store | Goal line | Reuse, Extend or Create | Why not a column or choice | Rows now, 3 years |
|---|---|---|---|---|---|---|
| Equipment | List | SharePoint; all eight gates hold (Constraints, Store gates) | 1, 4 | Extend: the store keepers' existing Equipment list on the depots site gains the columns Depot, Condition, On loan and Last checked | Each item has attributes of its own and many loans | 600, 700 |
| Loans | List | SharePoint; all eight gates hold (Constraints, Store gates) | 1, 2, 3 | Create: checked the Equipment list (one row per item, no history) and the council's asset register (fixed assets over 5,000 only, with no loans) | A loan is a fact with its own borrower and dates, many per item | 0, 3,300 |

Columns:

- Equipment: Title, Category (choice: Laptop, Projector, Camera, Tool, Other), Depot (choice: North, South, East, West), Condition (choice: Good, Worn, Damaged), On loan (yes/no), Last checked (date).
- Loans: Item (lookup to Equipment), Borrower email (text), Borrower name (text), Team (choice of the eight teams), Borrowed (date), Due (date), Returned (date), Open (yes/no, true until the return).

Both lists are classified Internal: who borrowed what is visible to all staff, which the council's privacy officer agreed. A retention policy deletes loans three years after their return date, and version history is on for both lists.

## Automation

| Flow | Goal line |
|---|---|
| Overdue reminders | 2, 3 |

**Overdue reminders.**

- Goal lines 2 and 3. No existing flow sends reminders, so it is new.
- Trigger: a schedule, once a day at 07:00, so one run a day.
- Rows per run: the open loans past their due date, about 20 and at most 150. Get items filters on Open equal to yes and Due before today, with Pagination on and a threshold of 5,000.
- Steps: 1. Get the overdue loans. 2. For each loan, send the borrower a reminder from the depots' shared mailbox. 3. Group the loans by team and send each team lead one list. About 8 actions, nesting depth 1.
- Connectors: SharePoint and Office 365 Outlook, both standard, through the connection references `fac_SharePointService` and `fac_OutlookService`. The environment variable `fac_RemindersMailbox` holds the shared mailbox for each environment. At most 160 mails a day, well inside Outlook's limits.
- Failure path: a failed run emails the facilities systems mailbox, and the next run picks up the same loans.
- Retry and concurrency: the defaults.
- Tested with `powerops test` against fake loans before each import.

## Screens

| Screen | Goal line |
|---|---|
| Equipment | 1 |
| Loan | 1 |
| Return | 1 |
| My loans | 2 |
| Stock check | 4 |

- Equipment: search by name with `StartsWith(Title, txtSearch.Text)` and filter by depot with `Depot.Value = drpDepot.Selected.Value`, both delegable to SharePoint. Buttons: Lend (store keepers only) and Back.
- Loan (store keepers): borrower, team and due date for the chosen item. Buttons: Save, Cancel and Back. A failed save keeps the form open and shows the error.
- Return (store keepers): the item's open loan, with the condition on return. Buttons: Confirm, Cancel and Back. A failed save keeps the screen open and shows the error.
- My loans: `Filter(Loans, 'Borrower email' = varMyEmail && Open = true)`, delegable. Buttons: Back.
- Stock check (store keepers): the equipment of one depot with On loan equal to no, where the store keeper ticks each item found; the screen then lists the items not ticked. Buttons: Finish, Cancel and Back.

## Environments and route

Dev (unmanaged), Test (managed) and Prod (managed), each with its own depots site. The solution is built in Dev. An Azure DevOps pipeline exports it as managed, runs `powerops analyze` and the solution checker, and imports it into Test with `pac solution import` under the pipeline's service principal. After the store keepers' acceptance test in Test, the facilities manager approves the release in the pipeline, which imports the same package into Prod. Power Platform pipelines aren't used, because they need Managed Environments (Constraints, Store gates).

The two lists aren't in the solution. A provisioning script in the repo creates their columns in each environment's site, and the app and the flow reach them through the data source environment variables `fac_DepotsSite`, `fac_EquipmentList` and `fac_LoansList`.

## Solution and publisher

Solution `EquipmentLoans`, publisher `Facilities Systems` with prefix `fac`. One deployable unit: the canvas app, the flow, the two connection references and the four environment variables.

## Constraints

- Licensing: every user holds a Microsoft 365 E3 licence, which covers canvas apps and flows that use standard connectors. Every connector here is standard. None of the three environments is a Managed Environment, and the plan keeps them so.
- Data policy: checked by the platform team for all three environments. SharePoint, Office 365 Outlook and Office 365 Users are in the Business group, and no other connector is used.
- Permissions: on each Loans list, staff have Read and the app shows them only their own loans; store keepers have a permission level that adds and edits items but can't delete them; team leads have Read. On each Equipment list, staff and team leads have Read, and store keepers add and edit items without delete. The facilities systems team owns each site. Access is by security group, never by user.
- Sharing, app users, secrets: the app is shared with the Staff, Store keepers and Team leads groups, with no co-owner outside the facilities systems team. The flow runs as the dedicated service account of each environment through the connection references. No application user and no secret.
- Store gates, all eight checked: Loans stays under 5,000 rows (about 1,100 loans a year, deleted three years after return), and Equipment holds about 700; every app query above delegates; Loans has one lookup; the flow runs once a day, with Pagination on, over at most 150 rows; list permission is enough, because every loan is Internal and readable by all staff; no screen works offline, since every depot has Wi-Fi; the licence saving is real, since no environment is a Managed Environment and every user's E3 licence covers the app; the facilities systems team owns both lists' columns and the provisioning script.
- Monitoring and incidents: a failed run of the flow emails the facilities systems mailbox. The facilities manager reviews the lists' version history when an item goes missing.
- Testing: one test user per group in Test, including a staff user who must not edit a loan. The flow is tested with `powerops test` before each import.
- Deadline: live before the spring stock check.

## Assumptions

- Environment facts were not read from a tenant. The three environments and their depots sites exist.
- The store keepers' existing Equipment list holds every item, and its 600 rows are correct.

## Open questions

## Questions for your admin

## Steps

1. Create the solution `EquipmentLoans` with publisher prefix `fac` in Dev.
2. Run the provisioning script on the Dev site: add the four columns to Equipment and create Loans. Check the columns and the permissions with a test user per group.
3. Add the four environment variables and the two connection references.
4. Build the canvas app with its five screens. Check that every query shows no delegation warning, and that every form has a working Save or Confirm, Cancel and Back.
5. Build the flow and run `powerops test` on it.
6. Run the pipeline to Test. Run the provisioning script on the Test site, lend and return an item, let the reminder run, and do a stock check of one depot.
7. After the facilities manager approves, release the same package to Prod, run the provisioning script on the Prod site and set the environment variables.
