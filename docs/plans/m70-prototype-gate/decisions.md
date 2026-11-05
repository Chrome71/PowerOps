# Decisions: m70-prototype-gate

## Round 1

### Q1. Which plans are blocked at sign-off until they have an HTML prototype or a person's logged reason to skip?
1. Any plan whose `Screens` names anything: canvas, custom pages, Power Pages, generative pages, Power BI and model-driven apps (theme, forms and views); code apps count their mock-data slice; a plan with no screens logs `not needed` (recommended)
2. Only the freely laid out routes; model-driven apps stay without a sketch
3. In your own words
Answer: Any plan whose `Screens` names anything: canvas, custom pages, Power Pages, generative pages, Power BI and model-driven apps (theme, forms and views); code apps count their mock-data slice; a plan with no screens logs `not needed` (answered by: person)

### Q2. Who writes the prototype file during planning?
1. A new shipped skill, `powerops-prototype`, that `powerops-plan` starts at approval, forked on Claude Code like the review roles, with its own word budget (recommended)
2. `powerops-plan` itself, following `route.md`
3. The repo's Design Lead agent
4. In your own words
Answer: A new shipped skill, `powerops-prototype`, that `powerops-plan` starts at approval, forked on Claude Code like the review roles, with its own word budget (answered by: person)

### Q3. How detailed may the prototype be, still static HTML with inline CSS, no scripts, fonts, images or outside links, and fake data only?
1. 8 screens and 40 KB: the theme's real colours, four states per screen and a "who sees this" note (recommended)
2. Today's 6 screens and 15 KB
3. In your own words
Answer: 8 screens and 40 KB: the theme's real colours, four states per screen and a "who sees this" note (answered by: person)

## Round 2

### Q4. Data: what may the HTML prototype contain? It is committed with the plan.
1. Fake values with the ledger's display names; columns under column security or holding personal data shown masked; nothing taken from answers, environment facts or the tenant (recommended)
2. The same, plus each field's schema name, so the build can map fields
3. In your own words
Answer: Fake values with the ledger's display names; columns under column security or holding personal data shown masked; nothing taken from answers, environment facts or the tenant (answered by: person)

### Q5. Environments and ALM: when M70 ships, what happens to plans already in progress in a user's repo?
1. Check them too: sign-off refuses a `draft` or `approved` plan with screens until it has a prototype or a person's skip; signed-off plans are never rechecked (recommended)
2. Only plans started after the upgrade, with a new header marker and a schema change
3. In your own words
Answer: Check them too: sign-off refuses a `draft` or `approved` plan with screens until it has a prototype or a person's skip; signed-off plans are never rechecked (answered by: person)

### Q6. Automation: should anything outside the agent run the prototype check?
1. Sign-off and `plan status` only; the GitHub Action stays as it is (recommended)
2. A new Action step that runs `plan status` on the pull request and fails on prototype problems
3. In your own words
Answer: Sign-off and `plan status` only; the GitHub Action stays as it is (answered by: person)

## Round 3

### Q7. Constraints, cost: on Claude Code `powerops-prototype` runs in a fresh context on the top model, like the review roles, one extra top-model run per plan with screens. Keep that?
1. Top model: design quality is the point; the same pin as the review roles (recommended)
2. The session's own model, with no pin
3. In your own words
Answer: Top model: design quality is the point; the same pin as the review roles (answered by: person)

### Q8. Constraints, verification budget: how many smoke runs (a real client with a simulated user, about 8 to 9 minutes and some quota each)?
1. One run on Claude Code; Codex is covered by the unit tests and the weekly smoke run (recommended)
2. Claude Code and Codex
3. Unit tests only
4. In your own words
Answer: One run on Claude Code; Codex is covered by the unit tests and the weekly smoke run (answered by: person)

## Round 4

### Q9. Review R9: who may write `prototype: approved`? An agent answering under a brief could approve a sketch it wrote.
1. Anyone; `plan status` warns when the line isn't the person's, as it warns on an agent's sign-off (recommended)
2. Only a person; sign-off refuses an agent's approval
3. In your own words
Answer: Anyone; `plan status` warns when the line isn't the person's, as it warns on an agent's sign-off (answered by: person)

### Q10. Review R7: what happens to menu letter **D** now that approval starts the prototype skill?
1. Keep D as "sketch now": it starts the same skill before approval (recommended)
2. Drop D from both menus
3. In your own words
Answer: Keep D as "sketch now": it starts the same skill before approval (answered by: person)

## Log

- 2026-11-05: plan started at the person's request, after the M69 session found why the sketch didn't trigger: "Yes, plan M70" (answered by: person)
- read on 2026-11-05: `PlanSignOff.cs`, `PlanChecker.cs`, `PlanDocument.cs`, `SkillsCommand.cs`; `powerops-plan` sections 6 to 11, `route.md` section 2, `powerops-implement` step 3, `powerops-review-plan` item 11; the M42 decisions and smoke runs; the Phase 7 plan's prototype section, F4 and D8; the live run's `equipment-checkout` plan and decisions (read-only)
- depth: quick: the newest plan's depth
- B chosen, then "Ask about them first" for the four areas never asked (data, automation, environments and ALM, constraints): rounds 2 and 3 (answered by: person)
- all five areas asked (rounds 1 to 3); 0 open questions
- prototype: not needed (no screens): this plan changes the repo, and its `Screens` is "None"
- approved 2026-11-05 by the person: "B: approve draft", for a fresh-context review (answered by: person)
- review 1 (Sonnet 5.5, fresh context): 3 High, 7 Medium, 3 Low; R7 and R9 asked as round 4, the rest fixed in the plan
- review 2 (second pass): R1 to R13 closed; R14 (Low) accepted, since `Screens` names have no fixed shape; R15 (Low) fixed in row 9 (answered by: agent this session)
- signed off 2026-11-05 by the person: "A: sign off now", R14 acceptance included (answered by: person)
