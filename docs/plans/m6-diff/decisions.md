# Decisions: m6-diff

## Round 1

### Q1. A change can carry more than one risk (a removed connection reference is also a removed component). How are risks recorded?
1. A list of risk ids per change (recommended)
2. One label per change, the most severe
3. In your own words
Answer: A list of risk ids per change

### Q2. Which removals get `removed-component`?
1. Every removal except a missing dependency, whose removal is a fix (recommended)
2. Root components only
3. In your own words
Answer: Every removal except a missing dependency, whose removal is a fix

### Q3. Which workflows get `flow-state` when turned on or off?
1. Cloud flows only (category 5); other workflows show the state change without a label (recommended)
2. Every workflow category
3. In your own words
Answer: Cloud flows only (category 5); other workflows show the state change without a label

## Round 2

### Q4. Environment variable current values may hold tenant data and the Markdown goes into pull-request comments. How are they shown?
1. Never printed: only "added", "removed" or "changed" (recommended)
2. Printed, cut at 100 characters, like default values
3. In your own words
Answer: Never printed: only "added", "removed" or "changed"

### Q5. A canvas app edit changes the `.msapp`, which is not read. How is it detected?
1. Compare `AppVersion` in the meta XML and report "republished" (recommended)
2. Ignore `AppVersion` as a timestamp
3. In your own words
Answer: Compare `AppVersion` in the meta XML and report "republished"

### Q6. Classic workflows and business rules store their logic in XAML, which the model does not read. What is compared?
1. Name, category and state only, and the README says so (recommended)
2. Read the XAML and compare it as XML
3. In your own words
Answer: Name, category and state only, and the README says so

## Round 3

### Q7. How is the Markdown kept inside one pull-request comment (GitHub limit 65,536 characters)?
1. Cut at a whole row below 60,000 characters, with a note on how many changes were left out; cells capped at 200 characters (recommended)
2. Fail when the output is too long
3. In your own words
Answer: Cut at a whole row below 60,000 characters, with a note on how many changes were left out; cells capped at 200 characters

## Log

- Plan written after the fact (2026-10-20): M6 was built autonomously from the BUILD-PLAN M6 row. Q1 to Q7 were decided by the implementing session during the build, not asked of the user; the user confirms them at sign-off.
- approved for review (2026-10-20), on the master session's relayed request: plans after the fact for M6 and M7, a fresh review, then the user signs off
- approved without: users, automation (no question asked about them; both are covered in the plan)
