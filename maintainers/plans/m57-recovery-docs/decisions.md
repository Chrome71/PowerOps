# Decisions: m57-recovery-docs

## Round 1

### Q1. Where does the recovery text live?
1. A new page, `docs/RECOVERY.md`, linked from HANDOFF step 8 and from the example's "no rollback" section (recommended)
2. A longer section in `docs/examples/README.md`
3. In your own words
Answer: 1. The improvement plan names the page. An import runs by three routes (the Action, the MCP form, the person's CLI), and the example covers only the Action; HANDOFF step 8 is where every route's reader arrives. (answered by: agent m57-recovery-docs)

### Q2. What does the runbook offer as the way back?
1. Forward repair by a new, reviewed plan; say that an older zip is `skip` and imports nothing; name an environment restore as the admin's only route to deleted data, outside PowerOps (recommended)
2. Describe re-importing the previous release's zips as a rollback
3. In your own words
Answer: 1. `deploy plan` makes a same-or-lower version `skip` (`DeployPlan.cs:229`, `docs/COMMANDS.md:83`), and Learn says an update needs a higher version, so option 2 would describe a command that does nothing. Learn's safe-deployment page lists rolling back, rolling forward and redeploying the last working configuration, and warns that rolling back data and schema changes is complex; for a solution, the data a managed upgrade deleted is gone, and only an environment restore, which overwrites the environment, brings it back. The improvement plan rejected a generic rollback for the same reason (its M64 is a forward redeploy). A `_Upgrade` holding solution is named as the person's decision, because applying it deletes components and no run has shown what `deploy plan` does beside one. (answered by: agent m57-recovery-docs)

### Q3. Where does the test read the exit codes from?
1. The backticked digits of the "Exit codes:" sentence in the `## deploy apply` section of `docs/COMMANDS.md` (recommended)
2. A list in the test (`0`, `1`, `2`, `3`)
3. The code's return values
Answer: 1. COMMANDS is the documented contract and is already the page the docs checks hold to the code. A new exit code written there fails the test until the runbook has its row; a list in the test would not move. The code returns named constants through several helpers, which no simple read can list. (answered by: agent m57-recovery-docs)

### Q4. Which schema values must the runbook name?
1. Every `import` value of `solutions.items` as a table row, and every top-level `status` value at least once as a code span (recommended)
2. Only the `import` values
Answer: 1. The brief asks for every row status; the top-level `status` is what a reader sees first and costs one more line of the check. Rows make the check strict for the values a reader looks up; `status` values sit inside the exit-code table's cells. (answered by: agent m57-recovery-docs)

### Q5. How does the test see that a page is linked?
1. A Markdown link to the file (`](RECOVERY.md`, `](../RECOVERY.md`, `](HARNESS.md`) in the named page, on the step 8 line for HANDOFF, plus both files existing (recommended)
2. Any mention of the file name
Answer: 1. A bare name is not a link a reader can follow, and HANDOFF names many files. The repo has no general link checker, and adding one is outside this milestone. (answered by: agent m57-recovery-docs)

### Q6. What does the harness page claim about each client?
1. Only what a doc page of this repo or a recorded run (M19, M20, M21, the M39 client checks) shows, with the run named; every other cell says "not checked" (recommended)
2. Fill the table from the clients' own docs
Answer: 1. The improvement plan asks for the contract and the degraded path, not new client checks (its live rerun, M63, checks the client used). The repo's rule is to state only what a source or a run supports. (answered by: agent m57-recovery-docs)

### Q7. Which primitives are in the contract?
1. The five the improvement plan names: questions, forms, blocking hooks, environment passthrough and a long-call timeout; a line points to PLANNING-SKILLS's agent table for the skill folder and the fresh context of the build roles (recommended)
2. Add the skill folder and subagents as primitives
Answer: 1. The five are the ones a safety step fails without. The skill folder and subagents are already in PLANNING-SKILLS's table, and copying them would be a second place to keep in step. (answered by: agent m57-recovery-docs)

### Q8. Does PowerOps read the solution history for the person?
1. No; the runbook sends the person to Power Apps' **See history**, and the history read is listed under Won't build (recommended)
2. Add a read to `deploy apply` or a new command
Answer: 1. The milestone is docs (S), and the improvement plan gives the reconciliation to the person. A read would be new code, a new Web API route and a schema change. (answered by: agent m57-recovery-docs)

## Round 2 (from the review)

### Q9. What evidence may a cell of the harness table cite? (R1, R2)
A run of the verification record, the client's docs, the client's code read but not run, or PLANNING-SKILLS's Agent support table, each named in the cell; anything else is "not checked". This is what USE-WITH-AGENTS and HANDOFF already cite, and naming the kind keeps a code reading from reading as a run. (answered by: agent m57-recovery-docs)

### Q10. Which clients get a row? (R11)
Claude Code (interactive and `claude -p`) and Codex (TUI and `codex exec`), the two supported clients, in the modes the runs used. Copilot CLI, Antigravity CLI, Pi and Gemini CLI share one sentence: only their question tools are known. (answered by: agent m57-recovery-docs)

### Q11. How does the runbook say that an upgrade's deletion loses data? (R4)
As an inference: Learn says an upgrade deletes the components the new version lacks, and that a table or column deleted by hand loses its data, recoverable only by a restore; no page read says the second of a removal by upgrade, so the runbook says that and treats a `deletes` item for a table as data loss. The plan's own `deletes` message already says "a deleted table loses its data". (answered by: agent m57-recovery-docs)

## Log

- 2026-11-04: plan started at the request of the session that started this one: build M57 of the live-test improvement plan (answered by: person, through that session's brief)
- read on 2026-11-04: AGENTS.md; the M57 section of the improvement plan, A-11 and C-6 in both reviews, the decisions file; `docs/HANDOFF.md`, `docs/USE-WITH-AGENTS.md`, `docs/examples/README.md`, `docs/COMMANDS.md` (deploy plan, deploy apply), `docs/GITHUB-ACTION.md` (the apply step), `docs/PLANNING-SKILLS.md` (agent support), the M19 to M21 verification records in `docs/BUILD-PLAN.md`; `docs/schemas/deploy-apply.schema.json`; `DeployCommand.cs` (the dry run, the import loop), `SettingsCommand.cs` (the timeout), `DeployPlan.cs` (skip, deletes); `DocsDriftTests`, `HandoffDocTests`; the M52, M54 and M56 plan folders (layout)
- Learn checked 2026-11-04 with the Learn MCP tools; the pages are in Assumptions
- depth: quick: the improvement plan gives the scope and the files
- questions Q1 to Q8 answered by the building session under the user's delegation of 2026-10-22 (autonomous milestone mode); no D1 to D9 answer applies to M57
- areas not asked: users, screens, solution and publisher; the change is docs and one test in this repo, and the plan answers each or marks it not applicable
- approved 2026-11-04 by the building session under the user's delegation of 2026-10-22, for a fresh-context review (answered by: agent m57-recovery-docs)
- fresh review (a general-purpose subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-11-04 at plan-hash `3fba0321`: 0 High, 7 Medium, 6 Low (R1 to R13); every reference and code claim held; answered in `plan.md` (Done when 1 to 4, Data rows 1, 2, 4, 6, 7 and 8, Assumptions) and here (Round 2), with the resolutions in `review.md`. The pages and the test were written while it ran (plan step 2, test first: both new tests failed before the pages existed) and changed with the fixes
- R3's `-1` checked in `PacRunner` (the 16 MB cap returns exit code `-1`); R5's and R12's Learn pages read 2026-11-04 with the Learn MCP tools
- the same reviewer's second pass 2026-11-04 at plan-hash `7ef5a6b0`, with the build in the tree: R1 to R13 hold in the plan, the pages and the test; it checked each page claim against the code, the runs and the Learn pages, the anchors, and ran the docs and handoff tests; 1 Medium and 3 Low new (R14 to R17), each in the pages' text, not the plan. All four fixed in the pages (R15's sentence removed), so the plan body and its hash did not change
- signed off 2026-11-04 by the building session, outside the skills, under the user's delegation of 2026-10-22, with no High, Medium or Low finding open (`powerops plan sign-off`; `signed-off-hash` equals `review.md`'s `plan-hash`, `7ef5a6b0`) (answered by: agent m57-recovery-docs)
