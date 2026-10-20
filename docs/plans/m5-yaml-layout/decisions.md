# Decisions: m5-yaml-layout

## Round 1

### Q1. Where should the YAML fixture come from? (pac can't write the YAML layout offline; only Git integration does.)
1. Git integration commit: connect powerops-seed in the test environment to Git, commit v1 and the broken v2, then scrub (recommended)
2. Hand-written YAML translated from the XML fixture, following the public exports
3. In your own words
Answer: Git integration commit: connect powerops-seed in the test environment to Git, commit v1 and the broken v2, then scrub

### Q2. How should hard-coded URLs in canvas app formulas be reported?
1. New rule PO014, a separate warning; PO006 stays flow-only (recommended)
2. Extend PO006 to cover flows and canvas apps
3. In your own words
Answer: New rule PO014, a separate warning; PO006 stays flow-only

### Q3. Git integration needs a managed environment plus Azure DevOps or GitHub. Which licence route for making the fixture?
1. Managed Environments on the test environment plus Azure DevOps Basic (recommended)
2. Managed Environments plus the GitHub connection (preview, needs Azure Key Vault)
3. In your own words
Answer: Managed Environments on the test environment plus Azure DevOps Basic

## Round 2

### Q4. Which environment should the YAML fixture come from? (The tool itself stays offline; there is no Dev to Test to Prod route in M5.)
1. The environment found by plan facts, which holds PowerOpsSeed and is now managed (recommended)
2. A new managed developer environment just for the fixture
3. In your own words
Answer: The environment found by plan facts, which holds PowerOpsSeed and is now managed

### Q5. A Git integration repo can hold several solutions/*/solution.yml that share component folders. How should analyze treat that?
1. One solution each, its components picked by its solutioncomponents.yml (recommended)
2. Single-solution roots only, with a clear error for more
3. In your own words
Answer: One solution each, its components picked by its solutioncomponents.yml

### Q6. Which root component types should PO013 check?
1. Types with a folder known from real exports: table, cloud flow, canvas app, environment variable; others skipped (recommended)
2. Every type, with a full type-to-folder map
3. In your own words
Answer: Types with a folder known from real exports: table, cloud flow, canvas app, environment variable; others skipped

## Round 3

### Q7. R1: Is an Azure DevOps organisation available for the fixture repo, and what if the fixture slips?
1. Yes, with a fallback: if the YAML fixture is not committed by 2026-12-01, use hand-written YAML from the public exports and narrow Done-when (recommended)
2. Yes, no fallback
3. No, use the GitHub connection
4. In your own words
Answer: Yes, with a fallback: if the YAML fixture is not committed by 2026-12-01, use hand-written YAML from the public exports and narrow Done-when

### Q8. R2: The environment probably holds v2 now. How does it get back to v1 for the first commit?
1. Undo v2 by hand, and define parity as an explicit list of expected findings per version (recommended)
2. Re-import a kept unscrubbed v1 export
3. In your own words
Answer: Undo v2 by hand, and define parity as an explicit list of expected findings per version

### Q9. R10: Which licence covers Managed Environments in the test environment?
1. Power Apps Premium
2. Developer Plan only
3. In your own words
Answer: Developer Plan only

## Round 4

### Q10. Managed Environments use rights are not in the Developer Plan; running apps or flows in a managed developer environment needs a premium licence, and trials count for 30 days. How does the plan cover licensing for Step 1?
1. A 30-day premium trial started right before Step 1 (recommended)
2. Buy a premium licence
3. Hand-written YAML now
4. In your own words
Answer: No preference; the recommended option applies: a 30-day premium trial started right before Step 1

## Log

- depth: standard -> quick (option picks only, no comments)
- Managed Environments enabled on the test environment by the maintainer (2026-10-19)
- approved (2026-10-19)
- approved without: users, automation
- review findings R1-R19 resolved in plan.md (2026-10-19); review now stale, plan body changed
- second review (fresh context, Opus): R1-R11 resolved in plan.md (2026-10-19); no third review, by the user's choice
- signed off by the user with powerops plan sign-off (2026-10-19)
- licence line corrected after a read-only check of the account's licences (Power Apps Plan 2 Trial assigned); status back to approved for re-sign-off (2026-10-19)
- signed off again by the user with powerops plan sign-off (2026-10-19)
