# Decisions: asset-requests

## Round 1

### Q1. Who takes part?
1. Employees request, cost-centre owners approve and the service desk fulfils (recommended)
2. Employees and the service desk only
3. In your own words
Answer: Employees request, cost-centre owners approve and the service desk fulfils

### Q2. How does an approved request reach the service desk tool?
1. A flow calls the tool's ticket interface (recommended)
2. The service desk copies it by hand
3. In your own words
Answer: A flow calls the tool's ticket interface, and the tool reads approved requests with its own application user

### Q3. Where do owners answer?
1. In Teams or email, through the Approvals connector (recommended)
2. In the app
3. In your own words
Answer: In Teams or email, through the Approvals connector

## Round 2

### Q4. Which environments does the solution move through?
1. Dev, Test and Prod, managed in Test and Prod, through the pipeline (recommended)
2. Dev and Prod only
3. In your own words
Answer: Dev, Test and Prod, managed in Test and Prod, through the pipeline

### Q5. Licensing?
1. A Power Automate Premium licence for the service account that owns the HTTP flow, and per-app plans for users (recommended)
2. Standard connectors only
3. In your own words
Answer: A Power Automate Premium licence for the service account that owns the HTTP flow, and per-app plans for users

### Q6. How are the credential, the roles and the data policy handled?
1. A Secret environment variable rotated every 90 days, roles per user group, and the tenant data policy as listed by the platform team (recommended)
2. A text environment variable
3. In your own words
Answer: A Secret environment variable rotated every 90 days, roles per user group, and the tenant data policy as listed by the platform team

## Log

- 2026-10-19: plan started at the request of the service desk manager
- depth: standard
- approved 2026-10-21 by the requester, with all five areas asked
