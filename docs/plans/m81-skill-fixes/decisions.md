# Decisions: m81-skill-fixes

Questions already answered for this milestone, with the date and who answered.

## Round 0 (the 1.0 fix chain)

### Q1. For M76 and M79-M83, who signs off each milestone's plan before it is built?
1. Sessions sign off: each session signs its own plan once the Fable plan review approves it and `powerops-review-plan` leaves no finding open, and records that it did so on the person's instruction of 2026-11-09 (recommended)
2. The person signs off each plan

Answer: "Sessions sign off (Recommended)" (answered by: person, 2026-11-09)

### Q2. Who merges each milestone's PR once all 8 CI checks pass?
1. Sessions merge: each session rebases on main, reruns its checks, opens its PR and squash-merges it when green, one PR at a time (recommended)
2. The person merges each PR

Answer: "Sessions merge (Recommended)" (answered by: person, 2026-11-09)

## Log

- 2026-11-09: the person's instruction, quoted in the chain rules: "Once 75 is finished with its PR continue with the other M sessions in new sessions each until you wrap up M83 autonomously." M81 runs in Wave A beside M76, M79, M80 and M82, on `main` `278b136`. The fix plan's section 3 ("M81"), read with sections 4 and 5, is the base.
- read on 2026-11-09: the fix plan (sections 1 to 7); `skills/powerops-implement/SKILL.md` (sections 1 to 4), `skills/powerops-plan/SKILL.md` (sections 5 and 6), `skills/powerops-review-plan/SKILL.md`; `SkillsCommandTests` (the pinned wording and the word ceiling); `tests/smoke/skills-smoke.sh`; `docs/PLANNING-SKILLS.md` (skills table, Depth, checklist, Environment facts); M77's record of the smoke script (`bash -n`, not run)
- depth: quick: the fix plan gives the design
- areas not asked: users, data, automation, environments and ALM, constraints; the change is to this repo's skill text, and the plan answers each or marks it not applicable
- researcher (a `powerops-researcher` subagent, Sonnet, Learn and open sources, no tenant call) 2026-11-09: `pac auth list` is documented as listing the auth profiles stored on this computer, columns Index, Active, Kind, Name, Friendly Name, Url, User (a UPN), no tenant column (Learn, "pac auth" reference); its output and exit code with no profile, and whether a listed profile has a live token: not documented, and the CLI is closed source. `az account show` reads the local cache only (Azure/azure-cli `profile/custom.py` `show_subscription`, `_profile.py` `get_subscription`), raises "Please run 'az login' to setup account." with no account, and doesn't validate the token; `--output none` prints "no output other than errors and warnings" (Learn, "Output formats for Azure CLI commands"); the exit code under `none` is inferred. Whether `pac auth who` or `pac env who` calls the environment: not documented. Result: row 1 counts a probe that fails or lists nothing as missing
- approved 2026-11-09 by the building session for the two plan reviews
- fresh review (a general-purpose subagent following `powerops-review-plan`, Opus, fresh context) 2026-11-09: 1 High, 4 Medium, 6 Low (R1 to R11). Answered in `plan.md`: the word ceiling is set from a measured count, 2,832 under 2,833 (R1); row 1 names the five sign-ins again (R2) and gives the wait an exit, with one more probe (R3); item 12 checks the plan's Assumptions and has a "can't check" branch, and section 1 allows the lookup (R4); scenario 10 runs until `Steps` names the tool and needs a "not found / not callable / not checked" line (R5); row 9 updates the 2,783 sentence (R6); row 3 rewrites the "never see" sentence (R7); row 2 keeps and pins the Security-reviewer line (R8) and makes the export name the person's environment (R9); the scenario 7 and 8 assumption is corrected (R10); the two URLs are now page titles (R11)
- plan review under the model routing (a `fable-reviewer` subagent, Fable) 2026-11-09: REJECTED with seven findings, the first six matching R1, R3 (with FlowAgent kept), R4, R5, R7 and R8, all answered as above; the seventh, step 2's "the tests pass" can't hold while the plugin copies differ, is answered by scoping step 2 to `SkillsCommandTests`
- the same fresh reviewer's second pass 2026-11-09: R1 to R11 resolved; one new Low, R12 (the Assumptions line called `pac auth list`'s exit 0 with no profile "observed" in the M32 runbook, which only expects it), answered by rewording that line; the skill text already covers both exit codes
- plan review under the model routing, second pass 2026-11-09: APPROVED; step 2 to Sonnet, step 3 to Opus (in parallel with step 2), step 4 to Haiku, step 5 to Sonnet. The R12 rewording after it changes one Assumptions line only
