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
- researcher (a `powerops-researcher` subagent, Sonnet, Learn and open sources, no tenant call) 2026-11-09: `pac auth list` is documented as listing the auth profiles stored on this computer, columns Index, Active, Kind, Name, Friendly Name, Url, User (a UPN), no tenant column (learn.microsoft.com/power-platform/developer/cli/reference/auth); its output and exit code with no profile, and whether a listed profile has a live token: not documented, and the CLI is closed source. `az account show` reads the local cache only (Azure/azure-cli `profile/custom.py` `show_subscription`, `_profile.py` `get_subscription`), raises "Please run 'az login' to setup account." with no account, and doesn't validate the token; `--output none` prints "no output other than errors and warnings" (learn.microsoft.com/cli/azure/format-output-azure-cli); the exit code under `none` is inferred. Whether `pac auth who` or `pac env who` calls the environment: not documented. Result: row 1 counts a probe that fails or lists nothing as missing
- approved 2026-11-09 by the building session for the two plan reviews
