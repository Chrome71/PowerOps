# Decisions: m12-handoff

## Round 1

### Q1. Does M12 get the manual run (a build in Dev with `app-builder`, then an import into Test through `deploy apply --apply`)?
1. Ask the user in this session for a yes to Dev (the build) and Test (the import); without it, record the run as not run, with the reason (recommended)
2. Run it now under the delegation of 2026-10-22
3. In your own words
Answer: Ask the user in this session for a yes to Dev (the build) and Test (the import); without it, record the run as not run, with the reason. The master session's brief says to stop before any `--apply` and ask the user for that environment in this session (AGENTS.md).

### Q2. How does the test check the commands on the page?
1. Run each `powerops <words>` found on the page through `Program.Run` with `--help`, so the parser built in `Program` decides, with no change to `Program` (recommended)
2. Expose the root command from `Program` and walk its subcommands in the test
3. In your own words
Answer: Run each `powerops <words>` found on the page through `Program.Run` with `--help`, so the parser built in `Program` decides, with no change to `Program`. Changed to option 2 after review R1: with `--help` present System.CommandLine 2.0.13 reports no parse error, so `powerops deploy nope --help` exits 0 (checked on the built CLI).

### Q3. What routing skill does smoke case 4 install?
1. A fake `platform-router` written by the script in our own words, claiming every Power Platform build request and telling the agent to write the app files at once, so a win for the router shows as files outside `docs/plans/`; the request names no skill (recommended)
2. A copy of Microsoft's or a workspace's router skill
3. In your own words
Answer: A fake `platform-router` written by the script in our own words, claiming every Power Platform build request and telling the agent to write the app files at once, so a win for the router shows as files outside `docs/plans/`; the request names no skill.

## Log

- plan started 2026-10-24 from the master session's brief (the BUILD-PLAN M12 row and build-order task 32; the skill wording was done in f3da510)
- `powerops plan facts` not run: M12 creates no solution; `environment` stays null
- depth: quick (from `m11-ephemeral-env`)
- the user delegated their parts on 2026-10-22 (relayed by the master session); this session answers with the recommended options and signs the plan off itself after a clean fresh review; environment writes still need the user's yes in this session (Q1)
- skill names rechecked on 2026-10-24 against power-platform-skills `aed70bf` (the repo tree, and the front matter of `debug-flow` and `manage-flows`); the rest from the shared Phase 3b source notes
- approved without: users, data, automation, constraints (the BUILD-PLAN M12 row and Phase 3b text cover them)
- approved (2026-10-24)
- fresh review (Sonnet) 2026-10-24: 1 High, 5 Medium, 7 Low; all fixed in the plan text (R1 changed Q2 to option 2); no user decision needed; review.md plan-hash updated to the final body with a note
- signed off 2026-10-24 by this session under the user's delegation (see above)
