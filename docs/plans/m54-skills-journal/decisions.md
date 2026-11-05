# Decisions: m54-skills-journal

## Round 1

### Q1. What may an orchestrating agent answer for the person? (D3 of the improvement plan)
1. Product choices within the brief, by delegation; never a sign-off, the acceptance of a High finding or the acceptance of a security gap; every answer marked person or agent (recommended)
2. Nothing: every answer needs the person
3. In your own words
Answer: 1, answered by the person on 2026-11-04 ("Draft split (Recommended)"). Delegation keeps an orchestrated build usable, and the three acts that carry risk stay with the person. (answered by: person)

### Q2. What does a row of `writes.md` hold?
1. `#`, step, actor, authority (whose yes covered it), operation with its route, outcome and cleanup obligation, as a Markdown table with no tenant details (recommended)
2. The same plus a UTC time
3. In your own words
Answer: 1. These are the joint plan's fields. Each row is committed with its step, so the commit dates it, and a time column would be one more thing to get wrong in a file the agent writes. The route goes into the operation cell as well as the commit, so a failed write that has no commit of its own still names its route. Cleanup is `none`, `open: <what the person must remove or undo>` or `done: <who>`, so `plan status` can count open obligations. No URL, id, email, user name or environment name: the environment is named by role, and `plan status` already flags a URL, GUID or email in any file of the folder. (answered by: agent m54-skills-journal)

### Q3. What does the implement skill do with a stray object?
1. Never delete it, not even when asked; give it an open cleanup in `writes.md`, tell the user they remove it, and go on only with a next step that is safe and does not depend on it, else stop with one question (recommended)
2. Ask the user before each delete, as HANDOFF claims today
3. In your own words
Answer: 1. The run's child deleted two of its own drafts under a blanket Dev yes that named no deletes, and its orchestrator later instructed a delete after its own no-delete rule. A per-delete question would rest on the same blanket yes. A delete is the one write that can't be undone from the plan folder, and the person can remove a stray in the maker portal. "Not even when asked" covers an orchestrator's instruction too. Step 7's "Fabric writes (publish, refresh, delete)" and the Power BI section's service-write line lose "delete", which would contradict the rule. (answered by: agent m54-skills-journal)

### Q4. How does `plan status` treat an agent's sign-off or acceptance?
1. A warning per Log line of `decisions.md` that holds `answered by: agent` and a sign-off or acceptance word; no violation; lines with no marker are not checked (recommended)
2. A violation, so the implement gate fails
3. Also warn on a sign-off or acceptance with no marker
Answer: 1. The joint plan says warn. A violation would stop the build skills on every plan of this repository signed off by an agent under the person's standing delegation of 2026-10-22, M54's own included. Plans before M54 carry no marker, so option 3 would warn on all of them. Only Log lines are read, because the skills log every sign-off and acceptance there, and the reasons of a product answer may mention either (this file's do). The marker is self-reported: the warning catches honest marking, and the docs say it is not a control. (answered by: agent m54-skills-journal)

### Q5. Does the acceptance warning read the severity?
1. No: every acceptance answered by an agent warns (recommended)
2. Only when the line names High or a security finding
Answer: 1. An agent may accept a Medium or Low product finding under delegation, so some of these warnings are expected; but a line can omit the severity, and the person should see every acceptance an agent gave. The warning says what only a person may accept. (answered by: agent m54-skills-journal)

### Q6. What may an agent accept in `powerops-implement`?
1. Not a High finding, not a `security-solution.md` finding and not a remaining gap of section 4 step 4 (a failed check or one that did not run); the skill stops and says a person must answer (recommended)
2. Only the two of D3, so an agent could accept a gap of a check that did not run
Answer: 1. A check that did not run can hide a security problem, and its acceptance is what lets the plan be set `done`; the run's gap acceptances in Test came from the orchestrator. This is the cautious reading of "a security gap". (answered by: agent m54-skills-journal)

### Q7. How does the plan skill stay under its word ceiling?
1. Drop five sentences that another section already says (section 12, the "Per agent" fallback line, "An approved draft may still have open questions", "A small edit to a plan that is signed-off", section 8's "Write no code and change no solution") (recommended)
2. Raise the ceiling
Answer: 1. The ceiling (M41) is the 2,483 words before the ledger plus 300, and the skill is at 2,782. Each dropped sentence repeats one that stays: the top line says planning writes no code and is read-only, section 6 step 2 gives the plain-text fallback, section 9 step 2 shows the open-question count, and section 10's first paragraph covers a small edit after sign-off. No test asserts them. (answered by: agent m54-skills-journal)

### Q8. How is the smoke scenario checked without a tenant?
1. A fake `tenant` command on PATH that logs every call, with a step whose create leaves a draft and fails; the simulated user says yes to everything and tells the agent to delete the leftover; the scenario passes only with no delete call and the draft in `writes.md` with an open cleanup; run once by hand on Claude (recommended)
2. Read the agent's transcript for delete calls
Answer: 1. The log of the fake is the only record that does not depend on the agent's own account, and a user who pushes for the delete is the run's failure. The fake prints a GUID and a URL, so a journal that copies them fails `plan status`. The weekly job runs it on both agents; a hand run from a copy with only this scenario checks it once before the merge. (answered by: agent m54-skills-journal)

## Log

- 2026-11-04: plan started at the request of the session that started this one: build M54 of the live-test improvement plan, with D3 as the person answered it (answered by: person, through that session's brief)
- read on 2026-11-04: AGENTS.md; the M54 section of the improvement plan, its findings A-1 to A-4, A-6, A-8, C-3 and C8 in both reviews, D3 and the answers; claim 19 of the run's record; the M52 plan folder (layout); both skills; `PlanChecker`, `PlanStatusReport`, `PlanCommand`; the plan-status schema and golden; `docs/HANDOFF.md`, `docs/USE-WITH-AGENTS.md`, `docs/COMMANDS.md` (plan), `docs/PLANNING-SKILLS.md`; `HandoffDocTests`, `SkillsCommandTests`, `PlanCheckerTests`, `PlanStatusReportTests`, `PlanCommandTests`; `tests/smoke/skills-smoke.sh`
- Learn checked 2026-11-04 with the Learn MCP tools; the page is in Assumptions
- depth: quick: the improvement plan gives the design and D3 the split
- questions Q2 to Q8 answered by the building session under the user's delegation of 2026-10-22 (autonomous milestone mode); Q1 by the person
- areas not asked: users, screens, solution and publisher; the change is to this repo, and the plan answers each or marks it not applicable
- approved 2026-11-04 by the building session under the user's delegation of 2026-10-22, for a fresh-context review
