# Decisions: m34-windows-handover

## Round 1

### Q1. What does `drift --save` do with one environment?
1. A live environment is read and saved, the report covers that one environment, and the exit code is 0, or 1 when it holds an unbound reference or an unmanaged layer. One environment without `--save`, one snapshot file with `--save`, and two environments that would be saved to the same file name stay usage errors 2 (recommended)
2. Exit 0 whenever the file is saved
Answer: 1. The checks that run on one environment are the ones a comparison runs on each environment, so the exit code means the same thing. The user accepted this on 2026-10-30 ("Yes, start here", in answer to a question that named this behaviour).

### Q2. What happens when the solution isn't installed in the one environment?
1. Usage error 2, as today: "not installed in any environment" (recommended)
2. Write a "not installed" snapshot
Answer: 1. It's the smaller change. The test run needed that snapshot, by hand, so this can be revisited if a snapshot of Test before its first import is wanted.

### Q3. Should the MCP `drift` tool gain a save input?
1. No: decision 25 stays, and single-environment capture is CLI-only (recommended)
2. Yes, with `deploy_plan`'s rule for new files
Answer: 1. The report that reached this milestone recommends keeping decision 25, and the user accepted it with the rest.

### Q4. Should all-zero connection ids be refused?
1. No: `ConnectionId` stays a presence check, and the docs say so (recommended)
2. Refuse an all-zero id as a sentinel
Answer: 1. The contract is "missing, null or blank" (`docs/plans/m7-settings/plan.md:49`), and a shape check would still pass a wrong id.

## Log

- 2026-10-30: plan started from the review of the Windows handover, at the user's request ("Yes, start here", in answer to "Should I start the 11 fixes here, using the recommended `drift` behaviour?", which also said the PR would merge when CI is green)
- read on 2026-10-30: the review report; AGENTS.md; the M33 plan (layout); every source line the report names
- depth: quick
- approved 2026-10-30 by the building session under the user's delegation of 2026-10-22 (autonomous milestone mode)
- areas not asked: users, data, automation, environments and ALM, and constraints; this is a repo change with no tenant, and the plan answers each or marks it not applicable
- fresh review (general-purpose subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-10-30: 0 High, 7 Medium, 9 Low (R1 to R16), all fixed in `plan.md` and this file (see `review.md`). The fixes reach the `deploy apply` help text and MCP description (R4) and make a request that names an environment enough to write it in the fast path (R5)
- the same reviewer re-read the fixed plan 2026-10-30: R1 to R16 resolved, no new High or Medium; three new Low (R17 to R19), all fixed by the building session
- built 2026-10-30: #66 squash-merged as `248c832` after CI was green on Linux, Windows and macOS on its first run. The `pin-drift` job warned on that push, as designed; the pin PR moved every PowerOps pin to `248c832` and marked this plan done
