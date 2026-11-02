---
status: approved
review: pending
signed-off: null
signed-off-hash: null
environment: null
depth: standard
supersedes: null
---

# Permit portal: building permit applications online

## Goal

Let residents apply for a building permit on a Power Pages site, upload their drawings and follow the status of their application. Permit officers review applications in a model-driven app, and the chief building inspector decides.

Done when:

- A resident signs in, submits an application with documents and sees its status without calling the permit desk.
- A resident sees only their own applications.
- An officer receives each new application in the Permit Desk app and the resident is emailed when the status changes.

## Users

- Residents: about 4,000 a year, who sign in to the site with a personal account. They see only their own applications.
- Permit officers: 12, who review and update applications.
- Chief building inspector: 1, who approves or refuses.
- Records office: 3 people, who read approved permits.

## Data

| Table | Holds | Volume | Classification |
|---|---|---|---|
| `cty_Permit` | one application: applicant, address, type, status, decision, national ID number | about 4,000 rows a year | Confidential |
| `cty_PermitDocument` | a drawing or form of an application, in a file column | about 20,000 rows a year | Confidential |
| `Contact` | the resident's sign-in record | about 6,000 rows | Confidential |

## Automation

1. `Receive an application`. Trigger: a `cty_Permit` row is created from the portal. Action: encode the resident's national ID number with `base64()` before it is stored in `cty_IdNumber`, then assign the row to the permit officers' queue. Connection reference `cty_DataverseService`.
2. `Tell the resident about a status change`. Trigger: `cty_Status` of a `cty_Permit` row changes. Action: email the resident. Connection reference `cty_OutlookService`. Environment variable `cty_PermitDeskSender` holds the sender mailbox for each environment.

Both flows use the dedicated service account of their environment through connection references. Dataverse is a premium connector, so that account holds a Power Automate Premium licence; Outlook is standard.

## Screens

Portal pages:

- Home and sign-in. Cancel on the sign-in page returns to Home.
- My applications: the resident's applications and their status.
- Apply: an application form with a document upload. Buttons: Submit and Cancel. A failed submit keeps the form open with the error.
- Application detail: read-only, with a Back link.

Model-driven app `Permit Desk`: the review form with Save and Close.

## Environments and route

Dev (unmanaged), Test (managed) and Prod (unmanaged). The portal and the solution are built in Dev and exported by the web team. The portal content moves with the solution. Prod stays unmanaged so that the web team can adjust page text and form labels directly in Prod without a release. The chief building inspector approves each move to Prod.

## Solution and publisher

Solution `PermitPortal`, publisher `City Digital Services` with prefix `cty`. One deployable unit: the site, the tables, the roles and the two flows.

## Constraints

- Licensing: Power Pages capacity for 5,000 authenticated users a month, bought from the city IT budget. The 16 internal users (12 officers, the chief building inspector and 3 records office staff) hold Power Apps per-user plans, which cover Dataverse in the app. Dataverse is a premium connector, so the flows' service account holds a Power Automate Premium licence, paid from the city IT budget; Outlook is a standard connector.
- Data policy: checked against the tenant policy for all three environments by the platform team. Dataverse and Office 365 Outlook are in the Business group, and no other connector is used.
- Web roles and table permissions: the web roles are `Applicant`, given to every resident on first sign-in, and `Anonymous`. The `Anonymous` role has no table permission. For the `Applicant` role, `cty_Permit` has Global scope with Read, Create and Write, so that the My applications page can list rows quickly, and `cty_PermitDocument` has Contact scope with Read and Create.
- Security roles: `Permit Officer` reads and updates permits and documents of the business unit. `Chief Building Inspector` does the same and sets the decision. `Records Reader` reads approved permits. No role can delete a permit. The platform team holds System Administrator in Dev only.
- Sharing, app users, secrets: the Permit Desk app is assigned to the officer, inspector and Records Reader roles. No application user and no secret.
- Column security: `cty_IdNumber` is secured with the field security profile `Permit Sensitive`, which gives Read to the Permit Officer and Chief Building Inspector teams only.
- Standards and lifecycle: the plan follows the city's information security policy and the Power Platform Well-Architected Security checklist. Changes go through source control with a second maker's review, and the web team deploys through the pipeline. The threat model is a one-page list kept in the repo.
- Identity and segmentation: each environment has its own service account and its own sign-in configuration. Access is by security group. Makers have no role in Prod. The chief building inspector reviews Prod role assignments every quarter.
- Encryption: the base64 step in the first flow protects the national ID number in `cty_IdNumber`, and the platform encrypts data at rest and in transit. Nothing is exported from the tables.
- Monitoring and incidents: Dataverse auditing is on for `cty_Permit` and `cty_PermitDocument`. A failed run of either flow emails the permit desk mailbox, and the audit log is reviewed monthly. The head of digital services owns incident response. The runbook is to turn off the site and the flows, remove the affected role assignments and report to the city's data protection officer, and it is rehearsed once before go-live.
- Testing: one test user per role in Test, including a resident who must see only their own application and an anonymous visitor who must see none. The flows are tested with `powerops test` before each import.
- Deadline: live before the spring building season.

## Assumptions

- Environment facts were not read from a tenant. The publisher and the three environments exist.
- Sign-in uses the city's external identity provider, which is already configured for Prod.

## Open questions

## Questions for your admin

## Steps

1. Create the solution `PermitPortal` with publisher prefix `cty` in Dev.
2. Create the tables, the three security roles and the field security profile.
3. Create the portal pages, the web roles and the table permissions. Check with a test resident that only their own applications are listed.
4. Build the Permit Desk app and the two flows. Run `powerops test` on both.
5. Export from Dev, run the solution checker and fix what it reports.
6. Import as managed into Test with the pipeline. Run the role checks with one test user per role.
7. After the chief building inspector approves, import into Prod and set the environment variable.
8. Run the incident drill once and open the site.
