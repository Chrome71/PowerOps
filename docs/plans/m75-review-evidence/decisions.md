# Decisions: m75-review-evidence

This milestone's plan is not written yet. The answer below came first: it is one of the six open questions (section 6) of the PowerOps 1.0 fix plan that Fable and Astra agreed on 2026-11-08, asked of the person in one round by the session that began the fixes. The plan that follows starts its own rounds after this one.

## Round 0 (the 1.0 fix plan)

### Q2. Does review coverage fail closed on the agent and CI routes?
1. MCP `deploy_apply`, `deploy gate` and the Action require both reports (`verify.md`, `security-solution.md`) ready, current and linked; the person-run CLI warns (recommended by both reviewers)
2. Warnings everywhere (today only `deploy plan` warns)

Answer: 1, "Block agent + CI" (answered by: person, 2026-11-08). M75 builds `--require-reviews` on `deploy apply`, and the three routes always pass it.

## Log

- 2026-11-08: asked together with the fix plan's other five questions, in one round, recommended option first where the plan recommends one; where the plan states two positions (Q1, Q6), both were shown as the plan words them, neither marked recommended.
