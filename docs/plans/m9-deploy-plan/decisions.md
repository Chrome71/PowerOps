# Decisions: m9-deploy-plan

## Round 1

### Q1. What does `deploy plan --target` take?
1. An environment as `pac` takes it (an id or URL), passed to `pac org who --environment` and never printed, exactly like `drift --env`; the output names the target by its friendly name and the plan hash holds its environment id (recommended)
2. The friendly name of `pac`'s current environment, checked against `pac org who`, as `settings apply --target` does
3. In your own words
Answer: An environment as `pac` takes it (an id or URL), passed to `pac org who --environment` and never printed, exactly like `drift --env`; the output names the target by its friendly name and the plan hash holds its environment id. Picked by this session under the user's delegation of 2026-10-22 (see Log).

### Q2. The target snapshot names tables and canvas apps by id, the zip by schema name. Which deletions does the plan check?
1. Only what both sides name: workflows by id, environment variables and connection references by name; other component types get one `components-not-checked` information item with their count and type numbers; no new reads (recommended)
2. Add reads of table and canvas app names to M8's snapshot so every root component can be matched (changes the snapshot format and M8's reads)
3. In your own words
Answer: Only what both sides name: workflows by id, environment variables and connection references by name; other component types get one `components-not-checked` information item with their count and type numbers; no new reads. Picked by this session under the user's delegation of 2026-10-22.

### Q3. How does the GitHub Action comment?
1. Opt-in input `comment`; with `target` set the action runs `deploy plan` itself (after the workflow signs in `pac` and `az`), else the offline `diff` of the PR's base and head; one comment found by a marker and edited with `gh api`; fork PRs and API failures go to the job summary (recommended)
2. Comment by default on every `pull_request` run (existing workflows then need `pull-requests: write`)
3. In your own words
Answer: Opt-in input `comment`; with `target` set the action runs `deploy plan` itself (after the workflow signs in `pac` and `az`), else the offline `diff` of the PR's base and head; one comment found by a marker and edited with `gh api`; fork PRs and API failures go to the job summary. Picked by this session under the user's delegation of 2026-10-22.

## Log

- plan started 2026-10-22 from the master session's brief: the BUILD-PLAN Phase 3 section (M9 row, research basis, decisions 9 to 14, build-order tasks 25 to 27) and the M8 plan (the snapshot and the live reader M9 reuses)
- `powerops plan facts` not run: planning must not contact the tenant (it runs `pac org who`); `environment` stays null
- depth: quick (from `m8-drift`, the newest plan)
- the user delegated their parts on 2026-10-22, plan sign-off included ("the rest you can do including my parts ... as permissions have been lifted", relayed by the master session); this session therefore answered Q1 to Q3 with the recommended options and signs the plan off itself with `powerops plan sign-off`
- research verified on Microsoft Learn, docs.github.com and open repos on 2026-10-22 by powerops-researcher (Sonnet); no tenant contacted
- approved without: users, environments and ALM, constraints (BUILD-PLAN decisions 9 to 14, the Phase 3 research basis and the M8 plan cover them)
- approved (2026-10-22)
- fresh review (Sonnet) 2026-10-22: 0 High, 10 Medium, 9 Low; all Medium and Low fixed in the plan text except R19 (accepted) and parts of R9 (no linters), R11 (`@` mentions) and R12 (two reads of a file), accepted with reasons in review.md; no user decision needed; review.md plan-hash updated to the final body with a note
- master session's rule on settings values (2026-10-22): environment secrets, one per value, the settings file built at run time and never printed or uploaded, values never in any output; a sentinel test added to Step 2
- signed off 2026-10-22 by this session under the user's delegation (see above)
- environments changed by the master session on 2026-10-22 (decision 9 was not possible in the tenant: no Dataverse capacity for a sandbox, no developer environment left): Dev is the existing unmanaged developer environment (suspended from 2026-12-15), Test a new Trial (standard) environment that expires 30 days after creation; the plan's "sandbox, managed environment" wording for Test is superseded, nothing in the plan's behaviour depends on the type, and the live checks must finish within the trial's 30 days
- live checks 2026-10-23 in the test repo (PR 2 and fork PR 3): item 1 one plan comment against Test (upgrade 1.0.0.0 -> 1.0.0.1, plan ok), the same plan hash as a local run under another identity; item 2 a push that changed the zip to 1.0.0.2 edited the same comment (a push that left the plan equal left the comment unchanged); item 4 a skipped pac sign-in replaced the comment with "could not run (exit code 3)" and failed the job; item 3 the fork run got no secrets, skipped sign-in and plan, posted no comment and wrote the diff to the job summary; the run logs of all 19 runs hold no tenant, app, environment or connection id, no host in any form and no setting value; the plan artifact holds the zip and the plan file only; the Environment ID that pac printed is Test's; two local plans gave the same hash
