plan-hash: 5e935d06393576c49fa9621b10dc832632f0aa57b9caeb45fcaf9e654615b9a9
model: claude-opus-5-5
fresh-context: yes

Second pass, 2026-11-15, on revision 2 (commit `cbdb0aa`, body hash above): R1 to R13 checked against the revised plan and resolved below; R14 to R16 are new. `powerops plan status` on revision 2 reports one violation, the subject of R14.

Environment facts were not read: tenant offline, and no confirmation that `pac` is signed in to the plan's environment (`environment: null`).

Tools the steps name, checked with `command -v`: `claude` (2.1.325, as Data row 7 says), `dotnet`, `jq` and `git` are on PATH; `powerops` was run from this branch's build. The leak scan is not in this worktree (R12). The PowerOps MCP tool `inventory` is in `src/PowerOps.Cli/McpCommand.cs` on this branch; the installed plugin this review ran beside lists no `inventory` tool, which is expected of a stale install and is why Data row 7 puts a wrapper on PATH.

Checklist items 1, 2, 4, 5, 6, 7 and 8 do not apply: this plan changes the PowerOps repo (skills, a specialist, tests and docs), not a Power Platform solution, and reads or writes no tenant. Item 3 is complete (the `### Licences` heading holds the standard line). Item 10: 0 open questions and 0 admin questions; the Log covers the upgrade plan's authority, the research and the routine choices. Item 11: `Won't build` is present.

## Findings

### High

No findings.

### Medium

#### R14. The plan holds URLs, and `plan status` fails on them
- Area: URLs, GUIDs, emails and names in the plan; 9 (testability)
- Finding: Data row 3 (plan line 51) now spells out the desktop-flow row with two full Learn links. `powerops plan status` reports a violation on that line ("looks like a URL"), so Done when 9's clean `plan status` can't hold as written, and the rest of the plan names Learn pages by title and path only.
- Suggested fix: In Data row 3, give the two pages by title and Learn path as Assumptions does (the links go into `licensing.md`, not the plan), or add the allow marker to that line with a reason.
- Resolution: open

#### R1. The 750 flow-run limit is checked per flow, but it is the environment's total
- Area: 3 (licence impact), 7 (data volume)
- Finding: Data row 3 has `licensing.md` check "each flow card's runs a day times 30" against 750, and Data row 4, Done when 4 and Done when 8(c) make "a flow whose expected runs a day, times 30, pass 750" the review finding. The Developer Plan page lists 750 flow runs a month as a capacity limit "for the developer environment" (its capacity table), so it is shared by every flow there. Three flows at 10 runs a day (900 a month) pass every per-flow check and still hit the limit. Done when 8(c) uses one flow, so it can't catch this.
- Suggested fix: Word the check as the sum over the plan's flow cards of runs a day, times 30, against 750, in Data rows 3 and 4 and Done when 4; make the 8(c) scratch plan two or more flows that pass only together (for example 15 and 15 a day), and pin the "sum" wording.
- Resolution: fixed (revision 2: Data rows 3 and 4 and Done when 4 sum the plan's flows; the 8(c) model run is dropped for a pin, Won't build)

#### R2. The Developer Plan's conditional coverage doesn't fit the `Held` values
- Area: 3 (licence impact), 13 (buildability)
- Finding: Data row 3 makes the `POWERAPPS_DEV` row cover Dataverse, premium and custom connectors and the gateway "in an unmanaged developer environment only", and no row in a managed one. `licensing.md` section 1 defines `yes (read)` as a part number that maps to a product covering the row, with no condition on the environment, and the licence read (`powerops-plan` section 3 step 8) learns nothing about which kind of developer environment Dev is. The plan doesn't say how the planner learns that (a question, a fact, the person's answer), what `Held` is when it isn't known, or that the read must not give `yes (read)` for a route whose Test or Prod the plan can't cover. Unmanaged developer environments are also suspended from 2026-12-15, a month from now, and the plan adds no line telling a plan that builds in one to say so under `Constraints`.
- Suggested fix: In Data row 3, say that `POWERAPPS_DEV` gives `yes (read)` only when the person said Dev is an unmanaged developer environment (logged as their answer), and `unknown` otherwise; that it never covers Test or Prod; and that a plan building in an unmanaged developer environment puts its suspension date under `Constraints`. Pin each line.
- Resolution: fixed (revision 2: Data row 3 gives the three `Held` cases, `the maker, in Dev only`, Test and Prod users in `Who needs it` or Open questions, and the suspension date under `Constraints`)

#### R3. `desktop-flow-call` gets "a row of its own" that section 2 doesn't have
- Area: 3 (licence impact), 13 (buildability)
- Finding: Data row 3 maps `desktop-flow-call` to "a row of its own with the driver's `need` and sources", but `licensing.md` section 1 says the table has "one row per need of section 2", and Data row 3 adds no section 2 row for desktop flows. That row then has no `Standard alternative`, no cost-tier behaviour, and no part number in section 3 that covers it (`POWERAUTOMATE_ATTENDED_RPA` lists premium connectors and Managed Environment use only; an unattended run also needs Process or the add-on), so its `Held` can only be `unknown` and the planner has no rule for it.
- Suggested fix: Add a section 2 row for running a desktop flow from a cloud flow (licence as the driver's `need` says, standard alternative "None", its Learn source checked 2026-11-15), and say which section 3 part numbers cover it; or map the driver to an existing row and say why.
- Resolution: fixed (revision 2: a section 2 desktop-flow row from `inventory`'s need and sources, and `POWERAUTOMATE_ATTENDED_RPA` covers attended desktop flows)

#### R4. The agent check's flags aren't proven to load the plugin's tools or to limit the others
- Area: 9 (testability), 13 (buildability)
- Finding: Data row 7 runs `claude -p` with `--plugin-dir` and `--strict-mcp-config` and no `--mcp-config`. Claude Code 2.1.325's help says `--strict-mcp-config` uses "only MCP servers from --mcp-config, ignoring all other MCP configurations", so the plugin's `.mcp.json` server may not start and no PowerOps tool would exist. No source or earlier run in the repo shows the two together. It also passes no `--tools`, so the built-in read-only tools (Read, Glob, Grep) stay available without a permission prompt, and the pass line "calls no other tool" can fail for a reason the skills don't control (`tests/eval/roles/run.sh` limits them with `--tools`). And it names no output format, while the pass line reads tool calls from "the transcript" (`--output-format stream-json --verbose`, or the session file).
- Suggested fix: Add `unproven: --strict-mcp-config keeps a --plugin-dir plugin's MCP server; probed by step <n>` under Assumptions with a cheap probe before step 4 (or pass `--mcp-config` naming the plugin's `.mcp.json` instead), add `--tools ""` or the smallest built-in set, and name how the transcript is captured.
- Resolution: fixed (revision 2: Data row 8 passes the plugin's `.mcp.json` as `--mcp-config`, `--tools ""`, stream-json capture, an init check and a defined pass; the coordinator's probe of 2026-11-15 showed strict mode drops the plugin's server without it and lists the 13 tools with it)

#### R5. The agent check would pass without this milestone's edits
- Area: 9 (testability)
- Finding: The Goal says agents don't run the new tools because the skills and specialists don't name them. Data row 7's prompt says "Use the PowerOps tools only" and allows only PowerOps MCP tools, whose descriptions already say what `inventory` and `analyze` do; it runs no plan and no `powerops-implement` section 4. So Done when 8(a) can pass on `main`'s plugin too, and proves the tools work, not that the edited text makes an agent run them.
- Suggested fix: Either run the same check once on `main`'s plugin and record both results, or change the prompt so only the edited text leads to `inventory` (for example a signed-off scratch plan and "finish the build", with no tool named), or log that 8(a) checks tool use only and is not evidence for Done when 1 or 5.
- Resolution: accepted (revision 2 states it under Assumptions, "Not verified by this milestone": the agent check exercises the MCP tools and rules, not the skill text, because the upgrade plan's prompt names the tools; recorded by the building session, not a person)

#### R6. The smoke scenario `draft` can't be run alone, and its pin can't be "seen to fail"
- Area: 9 (testability), 13 (buildability)
- Finding: Done when 8(b) asks for "one local run of that scenario", but `tests/smoke/skills-smoke.sh` takes only `claude|codex` and runs every scenario in order, at the model cost of the whole set. No Data row adds a way to run one scenario. Data row 8 also lists the smoke scenario among pins "written first and seen to fail", but the script isn't run by `dotnet test`, so a failing run before the edit costs credits and isn't in Step 2. The shared `check_decisions` fails with "no rounds logged" when a draft leaves nothing to ask, which the scenario's draft must avoid.
- Suggested fix: Say how the one scenario runs (a scenario argument or variable added to the script, as its own Data row with its usage line, or a scratch copy cut to the one scenario), say whether the red run happens or is skipped, and give the draft an open item so a round is asked.
- Resolution: fixed (revision 2: `SMOKE_ONLY=draft`, the scenario first with an exit after it, its checks listed, one round answered, and "seen to fail" scoped to `dotnet test`)

#### R7. `docs/PLANNING-SKILLS.md` has no Data row and no Step
- Area: 9 (testability), 13 (buildability)
- Finding: Done when 6 changes `docs/PLANNING-SKILLS.md` and Data row 8 pins "its lines", but no Data row says the new wording or where it goes, and Step 3 builds "Data rows 1 to 6", which leave it out. "The question rules" names no heading of that file: it has a `## Skills` table (the `powerops-implement` row) and a `## Questioning protocol` section.
- Suggested fix: Add a Data row with the two lines' wording and their places (the `powerops-implement` row of `## Skills`, and `## Questioning protocol`), and include it in Step 3.
- Resolution: fixed (revision 2: Data row 7 gives both lines and their places; Step 3 builds rows 1 to 7 and 9)

### Low

#### R8. The draft step comes after a jump, and the fast path still asks the route
- Area: 10 (unanswered areas)
- Finding: Data row 2 adds the draft as section 2 step 5, but step 4 ends "Otherwise create the folder and go to section 3", so an agent reading in order can leave before step 5. The fast path (section 8, used when `powerops-implement` hands over) always asks the route (its step 2) and isn't touched, so a draft handed over with a build request still gets the route question.
- Suggested fix: Put the draft step before the jump (or make step 4 point to it), and say whether a draft that settles the route skips section 8 step 2.
- Resolution: fixed (revision 2: step 4 points to step 5, and section 8's cost tier and route yield to the draft)

#### R9. Rule 7 says what a mismatch means, not what a failed read-back means
- Area: 9 (testability)
- Finding: Data row 1 covers exit 1 with "pac reported success, but the target shows". `docs/COMMANDS.md` also documents exit 3 with status `imported` and "The imports succeeded. Reading the target again failed": the imports stand, unverified. The new text doesn't say what the agent does then.
- Suggested fix: Add one clause: on that exit 3, tell the person the import ran but wasn't read back, point to the import history, and don't count it verified.
- Resolution: fixed (revision 2: Data row 1 reads exit 3 after "The imports succeeded." as a version not verified)

#### R10. The specialist's line describes five behaviours for six rules
- Area: 13 (buildability)
- Finding: Data row 6's parenthesis names a silent catch (PO037), a static result (PO038), the 90-day schedule (PO039), the long approval (PO040) and secrets without secure inputs or outputs (PO042). PO041, a text environment variable that looks like it holds a secret, is not described.
- Suggested fix: Add "a text environment variable that looks like a secret" to the list.
- Resolution: fixed (revision 2: Data row 6 follows the six rule titles, PO041 included)

#### R11. ECO-0022 lists one affected file
- Area: 13 (buildability)
- Finding: Data row 5 gives `Affects` as `skills/powerops-plan/references/licensing.md` only, but the review-plan item 3 sentence (Data row 4) rests on the same Developer Plan limits.
- Suggested fix: Add `skills/powerops-review-plan/SKILL.md` to `Affects`.
- Resolution: fixed (revision 2: ECO-0022 affects `skills/powerops-review-plan/SKILL.md` too)

#### R12. Paths outside the repo are not named
- Area: 9 (testability)
- Finding: Done when 10 runs "the leak scan" with no path; it is not in this worktree (it is in the main checkout's git-ignored `reports/`). The upgrade plan, named as the authority in the Goal and Data rows 5 and 7, has no path in the plan either, so a reader can't open the section it cites.
- Suggested fix: Name both paths (the leak scan as the main checkout runs it).
- Resolution: fixed (revision 2: Constraints names both paths; see R15 for how)

#### R13. The cost of the smoke and review-plan runs isn't stated
- Area: 9 (testability)
- Finding: The Log records the person's yes for the agent check at "About USD 1 to 3, once", capped by `--max-budget-usd 3`. Done when 8(b) (a smoke conversation with a simulated user, up to 12 turns) and 8(c) (a headless review) also run on the person's subscription, with no cost line and no cap.
- Suggested fix: Give each run an expected cost and a cap (`--max-budget-usd` or `SMOKE_MAX_TURNS`), and say which yes covers it.
- Resolution: fixed (revision 2: one USD 3 total for every `claude -p` call, a cap per run, a capped run counts as failed; the review-plan model run is dropped)

#### R15. Constraints holds absolute local paths with the account's home folder
- Area: URLs, GUIDs, emails and names in the plan
- Finding: The two paths R12 asked for are written as absolute paths that hold the local account's home folder name. Plans are committed, and that name identifies the machine's user.
- Suggested fix: Write them relative to the main checkout (`reports/trial-3/chain/chain-rules.md`, `reports/practitioner-research/upgrade-plan.md`, `reports/leakscan.sh`), saying they are git-ignored there.
- Resolution: open

#### R16. Step 1 names the previous solution by a request built later
- Area: 13 (buildability)
- Finding: Data row 1's new step 1 text says "Run `inventory` on the previous solution too (step 3's request names it)", but the request to the two roles is built after step 3's design check, after step 1 runs. Section 4 does define it ("the last export before this plan, from the repo's history", or "no previous solution"), so the step can be done, but the pointer sends an agent forward to a request that doesn't exist yet.
- Suggested fix: Point to the definition instead: "the previous solution (the last export before this plan, from the repo's history), or none".
- Resolution: open
