# Decisions: permit-portal

## Round 1

### Q1. Who applies, and how do they sign in?
1. Residents, with a personal account through the city's identity provider (recommended)
2. Residents with a username and password held in the site
3. In your own words
Answer: Residents, with a personal account through the city's identity provider

### Q2. Where are the applications and documents kept?
1. Dataverse tables, with documents in a file column (recommended)
2. SharePoint libraries
3. In your own words
Answer: Dataverse tables, with documents in a file column

### Q3. What notifies whom?
1. The resident is emailed on each status change and officers work from the Permit Desk app (recommended)
2. Officers get an email for each application
3. In your own words
Answer: The resident is emailed on each status change and officers work from the Permit Desk app

## Round 2

### Q4. Which environments, and how does the portal move?
1. Dev, Test and Prod, with the portal content in the solution (recommended)
2. Dev and Prod only
3. In your own words
Answer: Dev, Test and Prod, with the portal content in the solution. The web team may adjust wording in Prod.

### Q5. Licensing?
1. Power Pages authenticated-user capacity for 5,000 users a month, per-user plans for the 16 internal users and a Power Automate Premium licence for the flows' service account (recommended)
2. Anonymous capacity only
3. In your own words
Answer: Power Pages authenticated-user capacity for 5,000 users a month, per-user plans for the 16 internal users and a Power Automate Premium licence for the flows' service account

### Q6. Which security rules apply?
1. The city's information security policy and the tenant data policy as it stands (recommended)
2. Only the tenant defaults
3. In your own words
Answer: The city's information security policy and the tenant data policy as it stands

## Log

- 2026-09-14: plan started at the request of the head of digital services
- depth: standard
- approved 2026-09-17 by the requester, with all five areas asked
