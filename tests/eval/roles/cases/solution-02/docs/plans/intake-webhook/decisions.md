# Decisions: intake-webhook

## Round 1

### Q1. How does the intake log learn about new work items?
1. A flow posts the name when a work item is created (recommended)
2. The log polls Dataverse
3. In your own words
Answer: A flow posts the name when a work item is created

### Q2. How is the log's credential kept?
1. A Secret environment variable that reads from the vault (recommended)
2. A text environment variable
3. In your own words
Answer: A Secret environment variable that reads from the vault

## Log

- 2026-11-01: plan started by the build team for the service desk manager
- depth: quick
- areas not asked: users, data, environments and constraints; the change adds one flow and two variables, and the plan answers each
- approved 2026-11-01, review skipped
- signed off 2026-11-01 with `powerops plan sign-off`
