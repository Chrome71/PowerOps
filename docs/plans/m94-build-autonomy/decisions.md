# Decisions: m94-build-autonomy

The owner's finding behind this milestone (trial 3, 2026-11-11): "the work done here from planning to execution should be as autonomous as possible once the initial planning session with the user has ended with execution picking up without lag and without calling the user overly unless it genuinely requires attention like sign-ins that you can't access otherwise." Their answer to the fix plan's Q1, "After sign-off, does Test or Prod still get a yes?", given in a question form on 2026-11-11: "One yes per deployment" (option a). Q2 "Hide by default", Q3 "Keep out of Git" and Q4 "Rerun it" don't apply here. The routine choices below were made by the building session under the owner's direction of 2026-11-11, "as autonomous as possible".

## Round 1 (planning)

### Q1. How does `plan status` find the Authority cell of a journal row?
1. By the header row of each table: a table with an `Authority` header cell is checked, any other table is not (recommended)
2. By position: the fourth cell of every row
3. In your own words
Answer: By the header. The existing tests and older journals hold tables such as `| # | Cleanup |` with no Authority column, which must read as before; a positional rule would warn on them. (answered by: agent m94 session, on the owner's standing direction)

### Q2. How does `skills/powerops-plan/SKILL.md` fit the sign-off's two lines, with its word ceiling used up?
1. Raise the ceiling by the words added, at most 60 (recommended)
2. Cut the same number of words elsewhere
3. In your own words
Answer: Raise it. M91 and M93 found every sentence pinned or carrying a rule; the two lines are the person's view of what the sign-off authorizes, which the fix plan asks for. (answered by: agent m94 session, on the owner's standing direction)

### Q3. How does the smoke test cover the five stop classes?
1. One scenario that walks all five in sequence, with a simulated user who refuses every question (recommended)
2. One scenario per class
3. In your own words
Answer: One scenario. The whole run took 82 of the job's 97 minutes in M93, and five builds would multiply the turns; a user who refuses every yes makes a Dev write that ran proof it ran without a question, and each stop class shows by its absence from the tenant log. (answered by: agent m94 session, on the owner's standing direction)

## Log

- 2026-11-12: started by the M94 building session (Opus) in its own worktree at `e664e7f` (`origin/main`). Depth `standard`: skill and doc text, one `PlanChecker` warning with tests, one smoke scenario.
- 2026-11-12: checked before planning: the frozen roles `skills/powerops-verify/SKILL.md` and `skills/powerops-review-security/SKILL.md` hold no match for `writes\.md|authority|consent|go-ahead|ask each|person's yes|user's yes|--require-reviews`, so neither needs a change and nothing moves to M96. The fix plan's Done grep on `e664e7f` prints 13 lines outside `docs/BUILD-PLAN.md` (rows 11 to 15 cover each). The weekly smoke job skipped its last run (2026-11-09, 3 seconds, no API key), so M93's follow-up about its wall time and scenario 6's order check can't be read from CI.
- 2026-11-12: approved for review by the building session; all five areas are covered by the plan's sections (no person-facing rounds: this is a PowerOps milestone plan). (answered by: agent m94 session)
