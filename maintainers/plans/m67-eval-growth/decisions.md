# Decisions: m67-eval-growth

This milestone's plan is not written yet. The answer below came first: it is one of the six open questions (section 6) of the PowerOps 1.0 fix plan that Fable and Astra agreed on 2026-11-08, asked of the person in one round by the session that began the fixes. The plan that follows starts its own rounds after this one.

## Round 0 (the 1.0 fix plan)

### Q5. D6 (the trial's two reports misread the Desk profile's Create rights and the table audit flag) and D8 ("proven in Dev" too broad)?
1. Correct both trial reports (Desk Create; the table audit flag is in the export), log the two role misses in M67's ledger, change no kept role now; D8 by the second trial's brief rule (recommended by both reviewers)
2. A frozen verify check line, with an eval rerun

Answer: 1, "Correct + log" (answered by: person, 2026-11-08). For M67's ledger: `powerops-verify` said the Desk field security profile had no Create where the export gives it Create on all six columns, and `powerops-review-security` said the table audit flag wasn't in the export where it is (and contradicted itself on the profile). The trial's reports are outside this repository; correcting them is the trial record's step.

## Log

- 2026-11-08: asked together with the fix plan's other five questions, in one round, recommended option first where the plan recommends one; where the plan states two positions (Q1, Q6), both were shown as the plan words them, neither marked recommended.
