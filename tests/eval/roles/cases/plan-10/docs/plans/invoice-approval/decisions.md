# Decisions: invoice-approval

## Round 1

### Q1. Who takes part?
1. Clerks capture, cost-centre approvers decide, and the controller reads everything (recommended)
2. Clerks and the controller only
3. In your own words
Answer: Clerks capture, cost-centre approvers decide, and the controller reads everything

### Q2. How does an approved invoice reach the finance system?
1. A flow posts it through the finance system's REST interface (recommended)
2. A nightly file import
3. In your own words
Answer: A flow posts it through the finance system's REST interface

### Q3. Where do approvers answer?
1. In Teams or email, through the Approvals connector (recommended)
2. In a model-driven app
3. In your own words
Answer: In Teams or email, through the Approvals connector

## Round 2

### Q4. Which environments does the solution move through?
1. Dev, Test and Prod, managed in Test and Prod, through the pipeline (recommended)
2. Dev and Prod only
3. In your own words
Answer: Dev, Test and Prod, managed in Test and Prod, through the pipeline

### Q5. Licensing?
1. A Power Automate Premium per-user licence for the HTTP connector, paid by finance (recommended)
2. A per-flow plan
3. In your own words
Answer: A Power Automate Premium per-user licence for the HTTP connector, paid by finance

### Q6. How are the finance system credential and the data policy handled?
1. A Secret environment variable backed by the vault, and the tenant data policy as listed by the platform team (recommended)
2. A text environment variable
3. In your own words
Answer: A Secret environment variable backed by the vault, and the tenant data policy as listed by the platform team

## Log

- 2026-10-05: plan started at the request of the finance controller
- depth: standard
- approved 2026-10-08 by the requester, with all five areas asked
