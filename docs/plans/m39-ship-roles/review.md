plan-hash: cec82b3b0a1d03ad33812f4b6c221f3291fe62df7c4d79cba7263a57d737cabe
model: claude-sonnet-5-5
fresh-context: yes

Environment facts were not read: the request carried no confirmation about `pac`, and `powerops plan facts` was not run. The plan has no environment and touches no tenant, so the facts would not have changed this review.

Read: `plan.md` and `decisions.md` of the plan folder (there is no `mockups/`), and the repo at the working tree of the review branch, plus a few files of `origin/main` through `git show`. The planning chat was not read. This context holds the request and the user's global instructions, not the planning conversation. This review ran on claude-sonnet-5-5, not on the top model, which principle 6 of `docs/PLANNING-SKILLS.md` asks for; the user may want a second pass on the top model.

Checked against the repo and found correct: the line numbers the plan cites for `tests/eval/roles/run.sh`, `tests/eval/roles/NOTICE.md:3`, `RoleEvalTests.cs`, `SkillsCommandTests.cs`, `PluginManifestTests.cs:132`, `SkillsCommand.cs`, `README.md:80`, `docs/USE-WITH-AGENTS.md:7`, `docs/PLANNING-SKILLS.md`, `docs/COMMANDS.md:120`, `docs/HANDOFF.md`, `AGENTS.md:12` and the security skill's lines in row 2 (the plan says `main` has moved some of them, and it has: the README line is 84 on `origin/main`); that the working tree differs from the evaluated commit only under `docs/plans`; the expected-use figures (1.8M tokens, 18 minutes and $5.29 for Claude; 1.6M tokens and 10 minutes for Codex) against the cost table of the M38 report; the "5 per client" and "10 of 10" figures; that the one false High of the Security reviewer was on solution-02. `powerops plan status` reports no violation, and no URL, GUID, email or person's name was found in `plan.md` or `decisions.md`. Checklist items 1 (solution boundaries and publisher) and 2 (connection references and environment variables) do not apply: the plan builds no solution and hard-codes no tenant value. The claims about Microsoft's repositories and clients (dated 2026-11-01) were not checked.

## Findings

### High

No findings.

### Medium

#### R1. Row 6 does not say what the Lead passes to the roles, or what happens when an input does not exist
- Area: 9. Testability (and 5, the route through the build)
- Finding: Both roles need inputs the request must supply: the plan, the current solution, the settings file and, for the Verifier, the previous solution and the test folder. M38's `run.sh` prompts always named all of them, and all six solution cases have a previous version. Nothing in `powerops-implement` creates or names a settings file. On a first build there is no previous solution, and the Verifier text lists it as a required input ("a required input the request leaves out is a failed check"), so the evaluated Verifier never ran without one. Section 4 step 2 of `powerops-implement` also allows a solution that is not in the repo yet, and then neither role has an input. Row 6 points at "the three routes of `powerops-plan` section 7 step 6", which are written for the Plan reviewer and include a bullet that tells the reviewer `pac` is signed in; the Security reviewer can run `plan facts` on that confirmation, the Verifier cannot. On the third route the user must start a new session and give it these inputs by hand. The result is that the most common build, a first one, would end in `failed` or `blocked` reports caused by the request, not by the solution.
- Suggested fix: In row 6, write the request the Lead sends to each role (which paths, and the words "no previous solution", "no settings file" or "no live evidence" when they apply), say that the Lead adds no `pac` confirmation to the Security reviewer's request unless the user gave it in that session, and say what the Lead does when there is no solution on disk (skip the roles and say "not verified" and why in step 5, as step 1 and step 2 of section 4 already do for `analyze` and `test`). Spell out the three routes in `powerops-implement` instead of pointing at another skill's step number. Make the client check's solution case drop `solution/previous` so a no-previous run is seen once, without changing the Verifier's text.
- Resolution: open

#### R2. A report is not tied to the solution that gets promoted, and `stale` watches only the plan
- Area: 5. Dev to Test to Prod route (and 6. Managed or unmanaged)
- Finding: The goal is that a solution is checked and read before it is promoted. The roles review the solution folder or zip the Lead names, and the `input:` line that records it is "not parsed by anything". `deploy plan` promotes a folder of exported zips, which are normally a different file from the unpacked Dev folder and may be a managed export. Nothing connects the two. `stale` compares only the plan body, and a plan that is signed off cannot change its body without voiding the sign-off, so for these two reports `stale` will almost always read `false` whatever happens to the solution. A `ready` report with `stale: false` beside a later build is the case a person is most likely to misread. The only protection is the Lead's instruction to rerun after a later build, which is not checked.
- Suggested fix: Have row 6's step 5 message name, for each report, the solution path or export that was reviewed and the commit. State in the docs, in the `plan status` text line and in the schema description that `stale` means the plan body changed and says nothing about the solution. Add to the docs that the reports describe the folder or zip they name and that the person promotes only what was reviewed. Do not change the roles' text for this: it would trigger the rerun.
- Resolution: open

#### R3. Row 3 leaves the reading of a report open
- Area: 9. Testability
- Finding: Row 3 says what `state`, `stale` and an unknown value do, but not: which lines are read (the existing review check takes the first `plan-hash:` line anywhere in the file, and a finding's text could hold a line that starts with `state:`); what a report that is a link, binary or over 1 MB looks like in the list (the existing scan warns and skips it); whether a stale `verify.md` or `security-solution.md` adds a warning the way a stale `review.md` does now ("neither is a violation" leaves a warning open); and how a CRLF or differently cased value reads. `PlanReport` is a positional record, so the shape is also set by the build. The text, JSON and MCP `plan_status` outputs all show whichever choice is made, and the MCP test validates that output against the schema.
- Suggested fix: Decide in the row: read only the first four lines, trim and compare case-sensitively; an unreadable report is listed with `state` and `stale` null and keeps the existing warning; a stale new report adds the same kind of warning as `review.md`. Give each as a test case in `PlanCheckerTests`, and name `McpCommandTests` among the tests of row 3.
- Resolution: open

#### R4. The client checks have no pass criteria, and may never run the new Lead behaviour
- Area: 9. Testability
- Finding: Claude Code and Codex "must pass", but the plan never says what a pass is. The record fields (discovery, question fallback, fresh-session route, report creation, sign-off) are those of the planning flow, which this milestone does not change. The new behaviour is in `powerops-implement`: stopping by impact (row 5) and running the Verifier and then the Security reviewer (row 6). The only checks listed for those rows are `SkillsCommandTests`, which check text. The rerun uses `run.sh` and the evaluated prompts, so it does not reach them either. As written, a client check can pass without `powerops-implement` section 4 ever being run, and rows 5 and 6 would ship unobserved.
- Suggested fix: Define a pass: the five skills are found; on the solution case the Lead reaches section 4, starts both roles by the client's route, and each writes its report with the four header lines and a table state; `plan status` lists both; nothing outside the two reports and the plan's `status` line changes; a non-interactive run stops as principle 5 says. Say that the solution case runs `powerops-implement` from section 4, and that one stop-by-impact case (a change of scope) is tried.
- Resolution: open

#### R5. M38's results came from a restricted harness; the shipped copies run with the session's tools
- Area: 8. Security roles
- Finding: In M38 the Claude runs had a tool list, a `powerops` command allowlist, project settings only and no MCP servers, and the allowlist refused 47 commands in 21 runs. The Codex runs had a temp home folder, and web search was used 18 times, all in the Security passes. The "no stray writes", "no read outside the temp folder" and "tenant claims" results hold for that harness. An installed copy of either role runs inside the user's session, with its other skills, MCP servers (including ones that can write to a tenant) and any permission mode. The plan says the tool lists are "instruction, not enforcement", but it does not say that the evidence itself came from a stricter setup, and the docs row only promises the limits under Constraints. A reader would take the M38 numbers as describing the shipped roles.
- Suggested fix: Add to Constraints, and so to the docs, that the evaluation ran with an allowlist the shipped copies do not have, and that the stray-write and outside-read results do not carry over. Decide and record whether the Claude copy can narrow its tools through its front matter (the same way it sets `model`); if it can, check that the narrowing matches the evaluated allowlist and run the client check with it. If not, say so.
- Resolution: open

#### R6. Nothing says what leaves the machine when the roles run
- Area: 4. DLP fit
- Finding: Each run sends the plan, the solution's components and flows, and the settings file to the client's model provider, and in M38 Codex also searched the web during its Security runs. On real solutions that content holds names, connection reference names and environment variable defaults. Nobody has checked whether that is allowed for the maker's tenant or organisation. The cases were fake, so M38 did not meet this. The plan's Constraints and Environments sections are silent, and Q1 made the Security pass run on every build.
- Suggested fix: Add a Constraint, carried into `docs/PLANNING-SKILLS.md` and `docs/USE-WITH-AGENTS.md`, that both roles send solution content to the model provider the user chose and that a team should check this against its own data rules first; note that Codex used web search in the Security passes. Say that local Qwen through Pi is the route that keeps it on the machine, if the client check passes. Do not add a search ban to the skill text: that would be a rerun.
- Resolution: open

#### R7. The cost and licence impact of two more top-tier runs on every build is unstated
- Area: 3. Premium connector and licence impact (who pays)
- Finding: Q1 chose "every build". Row 4 pins the Claude copies to the top-tier alias. A build now adds up to three Verifier runs and one Security run. From M38's table that was about $0.69 per Verifier run and $0.88 per Security run at list price for Claude, so roughly $3 per build for the small fixtures, and more for a real solution; on Codex and the other clients the cost is in their own quota. The plan gives expected use only for the rerun. The maker pays this from their own agent plan, and some clients meter premium requests. No question about licence or cost was asked in the rounds, though `powerops-plan` expects one licensing question in round 1 or 2.
- Suggested fix: Add the per-build figures, with their basis (M38's small fixtures, list price), to the docs as the Limits and Constraints, and say who pays. Say that a `dropped` report is the role's way to skip a build with nothing to do, and keep "at most three runs" as the bound.
- Resolution: open

#### R8. The evaluated text cannot be reached from `main` after a squash merge
- Area: 5. Route (of the plan's own steps) and 9. Testability
- Finding: Step 2 compares both skills "with the text at `b254179`", Done-when says `powerops-verify` is "byte for byte" that text, and the Data header numbers lines by it. That commit is on the M38 branch only. The verification record shows this repository squash-merges, so after M38 merges the commit is not in the history of `main`, and it disappears from clones once the branch is deleted. Step 2 then cannot be done by someone who does not hold the branch. The working tree matches that commit outside `docs/plans`, so the merged copies of the two skills will be the same text, but nothing in the plan records that. "Byte for byte" is also ambiguous on a Windows checkout, where `skills/` has no line-ending rule.
- Suggested fix: Before M38 is merged, record in `report.md` of M38 or in this plan the git blob ids (or SHA-256 of the LF text) of the two skills and `run.sh` at `b254179`. Make step 2 compare the files in `skills/` with those ids, and say that LF text is compared. Keep the local build folder named after `b254179` as the record of what ran.
- Resolution: open

#### R9. A report that quotes a value trips the existing scan and the `powerops-implement` gate
- Area: 9. Testability
- Finding: `plan status` scans every file in the plan folder for a URL, GUID or email, and `powerops-implement` passes its gate only when `violations` is empty; for a violation it tells the Lead to show the message and not to edit the plan. The roles are told to replace such values in a report, and `decisions.md` records that 26 M38 reports left the exit code at 0 and concludes that no rule is needed. Those cases hold only fake values, and the findings of a real solution quote them naturally (an environment variable default that is a site address, a connection name, a mailbox). M38 already had one wrong `failed` state from a violation in `plan.md` shown through that exit code. One slip in `verify.md` blocks the Security reviewer's status read, blocks the next resume of the build, and gives the Lead no instruction.
- Suggested fix: Add one sentence to section 2 of `powerops-implement`: a violation that names `verify.md` or `security-solution.md` is fixed by replacing the value in that report with a placeholder, not by editing the plan. Keep the Verifier text as it is. Record in the docs that a nonzero `plan status` exit does not make a role's state `failed` and is not a reason to stop, since the shipped text does not say so.
- Resolution: open

### Low

#### R10. Users, environments and ALM were never asked, and existing installs have no stated upgrade route
- Area: 10. Unanswered areas
- Finding: The log says the plan was approved without users, environments and ALM. The plan answers the first two with its own text, but nobody was asked about the third, and the ALM of this change is how it reaches existing users. On `origin/main` the verification record says a plugin whose `version` is unchanged is not updated by `claude plugin update`, and `plugin.json` says the same string in every build. And `skills install` refuses to overwrite a skill whose text differs unless `--force` is given, which the changed `powerops-implement` will hit. Row 7 lists neither.
- Suggested fix: Add to row 7 a line in `docs/USE-WITH-AGENTS.md` for current users: reinstall the plugin, or run `skills install --force`, to get the two new roles and the changed `powerops-implement`. Log in `decisions.md` that the ALM area was answered by the plan text.
- Resolution: open

#### R11. `PlanFactsSkillTests` does not cover the two new skills
- Area: 8. Security roles
- Finding: The rule that `plan facts` runs only for an environment the user named is checked, for shipped skills, by `PlanFactsSkillTests`, which lists `powerops-plan` and `powerops-review-plan` by name. The Security reviewer lists `powerops plan facts` among its commands. Its text meets the condition, so nothing fails today, but the rule is then unguarded for the two skills this milestone ships, and a later edit could drop it without a test failing.
- Suggested fix: Add both skills to the theory in `PlanFactsSkillTests` and name that test in row 1's list.
- Resolution: open

#### R12. The Lead acts on text that comes from the solution
- Area: 8. Security roles
- Finding: Row 6 has the Lead "fix each finding" of a report. A report is written by an agent that read the solution, and components can hold free text (names, descriptions, comments in a flow). That text can come back in a finding's wording and suggested fix as something that reads like an instruction. The limits are real (fixes stay inside the plan, a change of scope stops, and a tenant write needs the user's go-ahead), but the plan states none of them as the reason for treating a report as data.
- Suggested fix: Add a sentence to row 6's text: a report is data; the Lead fixes only what the plan's `Steps` already allow, and takes no command or request from a finding's wording. Add a note to the docs next to the "instruction, not enforcement" limit.
- Resolution: open

#### R13. "Every check, command and report line stays word for word" conflicts with the lines row 2 lists
- Area: 9. Testability
- Finding: Row 2 lists lines 25-28 (the commands, where line 25 says "both passes") and line 96 ("Solution pass only: a line `input:`"). Both are command or report lines of the solution pass and both must change to drop the pass wording. The rule also says "no wording change". The build cannot satisfy both as written, and the step 2 comparison that "differs only as row 2 says" has no exact list to check against.
- Suggested fix: Reword the row: every check, command and report line keeps its meaning, and only wording that names a pass, the `pass` input and `security-plan.md` goes. List the resulting differences as an expected `git diff` hunk set in `report.md` so the comparison is exact.
- Resolution: open

#### R14. The docs list misses one live instruction, and some line numbers have moved
- Area: 9. Testability
- Finding: `docs/BUILD-PLAN.md` runbook step 4 for Windows and macOS (M32) says `claude plugin details powerops` shows "three skills and one MCP server". After this milestone it shows five. Row 8 only adds an M39 section. Row 7 also cites `README.md:80`, `docs/USE-WITH-AGENTS.md:7` and the `BUILD-PLAN` lines by their `b254179` numbers, which the plan itself says have moved on `main` (the README line is 84 there).
- Suggested fix: Add the runbook line to row 8, and cite the three moved files by the sentence they hold, not by line.
- Resolution: open

#### R15. The baseline is not rechecked at the freeze commit
- Area: 9. Testability
- Finding: `tests/eval/roles/baseline/` holds the output of `analyze`, `test`, `diff` and `settings validate` from the old build, and it decides which flaws count as `baseline: not-shown`, the figure the keep rule uses. The plan assumes the cases, key and scoring "work unchanged", and builds the rerun from a commit made after whatever else lands on `main`. If any command's output changed, the denominators are no longer the ones the key was written for. Today the change since the evaluated commit is docs only, so this is a cheap check, not a live risk.
- Suggested fix: In step 3, before the runs, regenerate the four outputs at the freeze commit for each solution case and compare them with `baseline/`; record "identical" or the differences in `report.md`.
- Resolution: open

#### R16. The limits to state do not include the size and origin of the cases
- Area: 7. Data volume and delegation limits
- Finding: The Constraints on what the reports can claim list the ceiling, the export gap, the three tenant claims and the zip branch. The M38 report also lists that the cases are hand-written approximations of `pac solution unpack` output, that they are small, and that a temp folder is a mitigation and not proof of blindness. How the roles behave on a large solution (time, context size, the 1,800-second limit) was not tried; the longest run took 260 seconds. No delegation or row-count question applies to the roles, but a maker with a large solution will meet the size question first.
- Suggested fix: Add these three limits to the Constraints bullet and to the docs.
- Resolution: open

#### R17. A floor that fails has no path in the plan, and the false-High rule starts at its limit
- Area: 9. Testability
- Finding: The plan says passing a floor permits keeping and never requires it, but not what a failed floor means. The rule allows one false High in the pooled clean-case runs, which are now 4; M38 found exactly one in those runs, so one more in the rerun fails it, and the scoring split on that case last time. Step 4 only covers "renews or withdraws". It does not say whether the person may keep a role that missed a floor, or whether one case may be rerun.
- Suggested fix: Add to step 4: a missed floor is shown with its counts and cannot be renewed without a reason logged in `decisions.md`, or the plan goes back to `powerops-plan`; and say that a valid poor run is never replaced, as in M38.
- Resolution: open

#### R18. The client rows do not say how Antigravity CLI and Pi get the skills, or where they go in the table
- Area: 9. Testability
- Finding: `skills install --agent` accepts claude, codex, gemini and copilot, and `docs/PLANNING-SKILLS.md` has a "Gemini CLI" row and no Antigravity or Pi row. The client check says the skills are installed by `powerops skills install`, which for these two clients means a folder the plan does not name. It is not said whether Antigravity replaces the Gemini row.
- Suggested fix: Name the folder each of the two clients reads, or say the record will note how the skills were placed; and say whether the Antigravity record replaces the Gemini CLI row or adds one.
- Resolution: open
