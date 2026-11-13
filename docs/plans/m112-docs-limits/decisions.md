# Decisions: m112-docs-limits

The authority for this milestone is the owner's answers to the decision packet of the review-enforcement and intake audit of 2026-11-13 (a git-ignored file outside the repository). Asked in chat "Take the recommended option on all eight decisions?", the owner answered "All recommended" (2026-11-13) (answered by: person). Of those, two are carried out here: 7(a), "no reviewer check in the Action; doc lines", and 8(a), "no stage-specific release policy now; the docs say reviews are stage-agnostic". Asked "May the milestone sessions for this work (M112, M114, M115, M116 and the two additions to M96) sign off their own plans after clean reviews and merge their own green pull requests, as in the M86 to M111 chains?", the owner answered "Yes, same as the chains" (2026-11-13) (answered by: person). Asked which milestones start before the weekly reset, the owner answered "M112 now, M96 after reset" (2026-11-13) (answered by: person).

No question in this milestone needed the owner: the packet settles what each line says, and the routine choices below were made by the building session.

## Round 1 (planning)

- depth: quick: documentation lines only, no code, no test behaviour.
- areas not asked: users, automation, environments and ALM, screens, solution and publisher; the change is to this repo's docs, and the plan answers each or marks it not applicable.
- 2026-11-13, routine choices by the building session: the five `docs/COMMANDS.md` findings (E-1, E-3, E-4, E-8, C-3) go in one place, right after the existing line in `deploy apply` that says the check reads what the reports say, so the reader finds the limits of the check together; H-6 names no GitHub plan that the read pages don't name (they don't say Enterprise); B-8 points Prod to the Action's apply job only, because a sign-in in another shell of the same user is not shown to be out of the agent's reach.
- 2026-11-13, H-6 research: `powerops-researcher` (Opus), against GitHub's *Deployments and environments* and *Managing environments for deployment*, read 2026-11-13: claims on Free, Pro and Team (reviewers, wait timers, bypass setting public-only), Free (no environments for private repositories), missing environments (created unprotected), "Prevent self-review" and the default administrator bypass supported; "private needs Enterprise" not stated (implied only). Recorded in the plan's Assumptions.

## Log
