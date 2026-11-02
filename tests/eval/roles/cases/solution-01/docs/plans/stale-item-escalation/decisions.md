# Decisions: stale-item-escalation

## Round 1

### Q1. How is the route to Test and Prod set up?
1. Unmanaged in Dev, then managed to Test and Prod through the pipeline (recommended)
2. Unmanaged everywhere
3. In your own words
Answer: Unmanaged in Dev, then managed to Test and Prod through the pipeline

### Q2. Which prefix and publisher does the change use?
1. The existing publisher `powerops` and prefix `pops` of the solution (recommended)
2. A new publisher
3. In your own words
Answer: The existing publisher `powerops` and prefix `pops` of the solution

## Log

- 2026-11-01: plan started by the build team for the operations manager
- depth: quick
- areas not asked: users, data, automation and constraints; the change adds one flow, two variables and one role, and the plan answers each
- approved 2026-11-01, review skipped
- signed off 2026-11-01 with `powerops plan sign-off`
