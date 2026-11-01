---
status: approved
review: pending
signed-off: null
signed-off-hash: null
environment: null
depth: standard
supersedes: null
---

# Supplier portal: onboarding for new suppliers

## Goal

Let a new supplier register its company on a Power Pages site, add its contacts, upload its certificates and give its bank details. The procurement team approves the supplier in a model-driven app, and the finance team receives the approved bank details. Today suppliers send forms by email and procurement re-types them.

Done when:

- A supplier completes registration without emailing procurement.
- A supplier user sees only the records of its own company, and never sees bank details after they are saved.
- An approved supplier appears for the finance team with its bank details, and a certificate close to expiry triggers a reminder to the supplier.

## Users

- Supplier staff: about 3,000 contacts at about 1,500 supplier companies, who sign in to the site. Each company has one `Supplier Admin` and any number of `Supplier User` contacts.
- Procurement officers: 6. Review and approve suppliers.
- Finance team: 5. Read the bank details of approved suppliers.
- Head of procurement systems: 1. Administers the solution.

## Data

| Table | Holds | Volume | Classification |
|---|---|---|---|
| `sup_Supplier` | company name, registration number, address, status, approval | about 1,500 rows | Internal |
| `sup_Certificate` | a certificate: supplier, type, expiry date, file | about 4,000 rows | Internal |
| `sup_BankDetail` | supplier, account holder, bank, account number | about 1,500 rows | Restricted |
| `Contact` | the sign-in record and address of a supplier user | about 3,000 rows | Internal |

The account number is the only Restricted column. It is the reason for column security below.

## Automation

1. `Ask procurement to approve`. Trigger: the status of a `sup_Supplier` row becomes Submitted. Action: start an approval for the procurement team, then set the status to Approved or Rejected with the reason. Connection reference `sup_ApprovalsService`.
2. `Tell finance`. Trigger: the status of a `sup_Supplier` row becomes Approved. Action: email the finance team's shared mailbox that a supplier is ready for payment set-up. The mail holds no bank details. Connection reference `sup_OutlookService`. Environment variable `sup_FinanceMailbox` holds the address for each environment.
3. `Warn about expiring certificates`. Trigger: every Monday at 07:00 UTC. Action: find certificates that expire within 60 days and email the supplier admin of each company, using the contact's address stored in Dataverse. Connection reference `sup_OutlookService`.

All three flows use the dedicated service account of their environment through connection references. Dataverse is a premium connector, so that account holds a Power Automate Premium licence; Approvals and Outlook are standard.

## Screens

Portal pages:

- Home and sign-in. Cancel on the sign-in page returns to Home.
- My company: the company's details, with Edit for the supplier admin. A new supplier registers its company here and becomes its supplier admin. Buttons: Save, Cancel and Back. A failed save keeps the form open with the error.
- Certificates: the company's certificates with an Upload form. Buttons: Upload, Cancel and Back.
- Bank details: a form for the supplier admin to enter or replace the details. Buttons: Save and Cancel. The page never displays saved details.

Model-driven app `Supplier Desk`: the supplier form with Save and Close for procurement, and a read-only bank details view for finance.

## Environments and route

Dev (unmanaged), Test (managed) and Prod (managed), each a separate environment. The site and the solution are built in Dev. The head of procurement systems exports them, runs the solution checker and imports them as managed into Test through the pipeline. After the head of procurement approves the Test checks, the pipeline imports the same package into Prod. Wording changes on the site go through the same route.

## Solution and publisher

Solution `SupplierPortal`, publisher `Procurement Systems` with prefix `sup`. One deployable unit: the site, the tables, the roles, the field security profile and the three flows.

## Constraints

- Licensing: Power Pages authenticated-user capacity for 3,000 users a month, bought from the procurement budget. The 12 internal users hold Power Apps per-user plans, which cover Dataverse in the app. Dataverse is a premium connector, so the flows' service account holds a Power Automate Premium licence, also paid from the procurement budget; Approvals and Outlook are standard connectors.
- Data policy: checked against the tenant policy for all three environments by the platform team. Dataverse, Approvals and Office 365 Outlook are in the Business group, and no other connector is used.
- Web roles and table permissions: the web roles are `Supplier Admin`, `Supplier User` and `Anonymous`. `Anonymous` has no table permission. For `sup_Supplier` and `sup_Certificate`, `Supplier User` has Account scope with Read, and `Supplier Admin` has Account scope with Read and Write, plus Create on suppliers and on certificates. For `sup_BankDetail`, `Supplier Admin` has Account scope with Create and Write and no Read. For `Contact`, both supplier roles have Self scope with Read and Write.
- Security roles: `Procurement Officer` reads and updates suppliers and certificates of the business unit and does not read bank details. `Finance` reads suppliers and bank details. No role can delete a supplier. The platform team holds System Administrator in Dev only.
- Column security: `sup_AccountNumber` is secured with the field security profile `Supplier Bank`, which gives Read and Update to the Finance team only.
- Sharing, app users, secrets: the Supplier Desk app is assigned to the Procurement Officer and Finance roles. No application user and no secret.
- Standards and lifecycle: the plan follows the company's information security standard and the Power Platform Well-Architected Security checklist. Changes go through source control with a second maker's review, and the pipeline service principal deploys to Test and Prod. The threat model is a one-page list kept in the repo.
- Identity and segmentation: each environment has its own service account and its own sign-in configuration. Access is by security group, never by user. Makers have no role in Prod. The head of procurement reviews Prod role assignments every quarter.
- Encryption: the platform's own encryption at rest and in transit. No custom encryption and no exports of the tables.
- Monitoring and incidents: Dataverse auditing is on for the supplier, certificate and bank detail tables, and the site's sign-in log is kept for 90 days. A failed run of any flow emails the procurement systems mailbox, and the audit log is reviewed monthly. The head of procurement systems owns incident response. The runbook is to turn off the site and the flows, remove the affected role assignments and report to the information security office, and it is rehearsed once before go-live.
- Testing: one test user per role in Test, including a supplier user of company A who must see nothing of company B and an anonymous visitor who must see no record. The flows are tested with `powerops test` before each import.
- Deadline: live before the next supplier review cycle in April.

## Assumptions

- Environment facts were not read from a tenant. The publisher and the three environments exist.
- Sign-in uses the company's external identity provider, which is configured for all three environments.

## Open questions

## Questions for your admin

## Steps

1. Create the solution `SupplierPortal` with publisher prefix `sup` in Dev.
2. Create the three tables, the two security roles and the field security profile. Check with a test user per role what each can read.
3. Create the portal pages, the web roles and the table permissions. Check that a supplier of company A sees nothing of company B, and that no page shows saved bank details.
4. Build the Supplier Desk app and the three flows. Run `powerops test` on each.
5. Export from Dev, run the solution checker and fix what it reports.
6. Import as managed into Test with the pipeline. Register a test supplier and follow it through approval and the finance mail.
7. After the head of procurement approves, import the same package into Prod and set the environment variables.
8. Run the incident drill once and open the site.
