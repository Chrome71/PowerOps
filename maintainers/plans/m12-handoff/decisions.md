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
- 2026-10-24: built steps 1 to 3 (db9bd45, b00a1de); `bash -n` clean under bash 3.2.57 (local `bash:3.2` image, no network) and 5.3; the unguarded `"${resume[@]}"` failed under 3.2 with `set -u`, the guard works
- smoke run 1, 2026-10-24, cases 3, 4 and the router-only control from a temporary copy in `tests/smoke/` (deleted before commit). Claude Code 2.1.303 (user-scope `power-platform-router` skill present): case 3 passed; case 4 reached the fast path (plan drafted, numbered route options, status draft, no file outside `docs/plans/`) but failed "no lettered A/B/C menu": the agent asked the publisher prefix as free text ("Answer like `1 contoso`"), the scripted `1 1` gave the prefix "1", and the agent asked again instead of guessing, so no menu came; control: the router alone wrote `src/<app>/app.json` and `sitemap.json`. Codex 0.163.0 (user-scope Power Platform skills present): all checks passed; control: the router alone wrote `src/<app>/app.json` and `sitemap.json` and edited `README.md`
- one deviation from the plan text (Automation says the answer `1 1`): the scripted answer is now `Route option 1. Publisher prefix: cso.`, which works whether the prefix question is numbered or free text; cases 3 and 4 run again in both agents
- smoke run 2, 2026-10-24, cases 3 and 4 with the new answer. Codex 0.163.0: all checks passed. Claude Code 2.1.303: case 3 passed; case 4 reached the fast path, said it followed the plan-first rule instead of `platform-router` ("says to start building straight away"), changed no file outside `docs/plans/`, and showed the menu as `**A.**`, `**B.**`, `**C.**`, which `menu_letters` did not match, so it failed "no lettered A/B/C menu". `menu_letters` now also takes `**A.**` (checked offline on that reply: `ABC`; `**D.**` and `**Answer**` give nothing); case 3 and 4 run again in Claude Code
- smoke run 3, 2026-10-24, Claude Code 2.1.303, cases 3 and 4: all checks passed; in case 4 the agent named both skills and said it followed the plan rule over `platform-router`
- 2026-10-24: steps 4 and 5: the manual run not run (Q1: it needs the user's yes to Dev and Test in this session; asked at the end of the build); BUILD-PLAN (the M12 row, task 32, the verification record) and FOUNDATION section 8 updated; status done
- 2026-10-24: the user said yes in this session to the manual run ("build a small model-driven app in Dev with app-builder and import it into Test with powerops deploy apply --apply"); Q1's ask is answered. Run in a scratch repo outside PowerOps (not committed), Dev and Test as in M10 (named by role here). Result in the BUILD-PLAN verification record: the build went to `app-builder` in Dev, the only import was `deploy apply --apply` into Test, and `drift` reported the model-driven app's components (types 80, 62, 10087) missing and extra because they get new ids on import (a `drift` gap, not fixed here)
- 2026-11-01: personal and tenant facts removed from the plan body, with no decision changed; re-signed with `powerops plan sign-off`, and `review.md`'s `plan-hash` moved to the new body
