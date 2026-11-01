# Decisions: m35-live-checks

## Round 1

### Q1. Which machine runs the tests?
1. A Windows machine: its tools are current, Python and the Dataverse CLI are installed, and the failures were seen there (recommended)
2. A Linux machine: the session can drive and check it directly, and it's the first Linux evidence, but `pac` and FlowAgent need updating and the Dataverse CLI and Python client need installing
Answer: 2.

### Q2. Which environment?
1. The one the Windows run used, in two new disposable solutions (recommended)
2. A different developer environment
Answer: 1.

### Q3. Can the run change the preferred solution once, then restore it?
1. Yes, once, for T7 (recommended)
2. Skip T7
Answer: 1. The review (R1) showed that one change isn't safe: the preferred solution has to stay on a test solution for the whole of stage 2, so a stray can't reach a real solution. That's two changes by the user, then a restore. The user is asked again before stage 2.

### Q4. Is a Dev-to-Test import part of this run?
1. No: it can follow as its own run (recommended)
2. Yes
Answer: 1.

## Log

- 2026-10-30: plan started at the user's request ("Yes, plan it"), to cover the new `powerops-implement` checks and one-environment `drift --save`, and to say what is needed from the user before anything touches a tenant
- read on 2026-10-30: the review report of the Windows handover; the M7, M9 and M12 live notes; `docs/BUILD-PLAN.md` decision 3; the local versions of `pac`, `az`, Python and FlowAgent's plugin (no tenant call)
- depth: quick
- the user answered Q1 to Q4 on 2026-10-30
- approved 2026-10-30 by the building session under the user's delegation of 2026-10-22 (autonomous milestone mode)
- approved without: users, data, automation and constraints; this is a test plan for the repo's own tools, and the plan answers each
- fresh review (general-purpose subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-10-30: 2 High, 10 Medium, 4 Low (R1 to R16). The plan was rewritten for them: the preferred solution stays on a test solution through stage 2, T9 separates lag, registration and the save and repeats the handover's path, T10 has a script, deny rules and its own solution. To stay small it drops two tests, FlowAgent `create_flow` and `create-connection-reference.js` (see Assumptions)
- the same reviewer re-read the rewrite 2026-10-30: R1 to R16 resolved; new 1 High, 4 Medium, 2 Low (R17 to R23). Fixed: the header test runs first, then the preferred solution moves to `M35Other` before the first FlowAgent save, so that save can show a join; the first setup writes wait for the first preferred-solution change; T9 turns flows on one at a time and saves only `Marker`; T10's deny rules, script and limits are spelled out
- the same reviewer checked R17 to R23 2026-10-30: all resolved, no new High or Medium; one new Low (R24), fixed by the building session. Signed off with no High or Medium open, under the user's delegation of 2026-10-22. The tenant run itself still needs the user's yes for stage 0 and stage 2
- the run, 2026-10-30 and 2026-10-31: results in [report.md](report.md). T1 to T3 and T8 passed, and T4 to T7 and T9 answered their questions. T10 stopped once at the user's word, then resumed in a Playwright browser at the user's word: nine lines passed and line 3 failed once
- a control was added to T9 after R5, with no new component, because the done line asks for the save to be kept apart from the filter
- W-12: yes, with T8's read in `drift`'s live read, in a plan of its own
- W-13: yes, a new error rule for filter columns on a Create-only trigger, and one caveat in PO012's advice
- T10 line 3: fixed in section 4 step 2 of `powerops-implement`, which now names the export route and the zip fallback
- cleanup, 2026-10-31: the user deleted the run's 13 remaining objects (the flows, the app, the connection references, the tables and then the solutions), and the preferred solution went back to the one it was on before the run. A read by name found none left, and no id the run recorded is still a solution component. The sign-in profile the run added is removed, and this plan is marked done
- 2026-11-01: personal and tenant facts removed from the plan body, with no decision changed; re-signed with `powerops plan sign-off`, and `review.md`'s `plan-hash` moved to the new body
