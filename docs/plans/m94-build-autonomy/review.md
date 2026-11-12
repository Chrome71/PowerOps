plan-hash: 2d0107e5d7396192787bee9dd7c23f92d79bbd0d5296f3214b0be1e06c20f4d5
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: the request did not say that `pac` is signed in to the plan's environment, and the plan's `environment:` is null (this milestone changes PowerOps itself and calls no tenant).

`plan status` was run with the worktree's build (`dotnet src/PowerOps.Cli/bin/Debug/net10.0/powerops.dll`), because the `powerops` on PATH is stale: status `approved`, review `pending`, 0 open questions, 0 admin questions, no violation, no warning.

Read besides the plan: the fix plan's M94 section and sections 5 to 8 (`reports/trial-3/fix-plan.md` in the main checkout, ignored by Git), `skills/powerops-implement/SKILL.md`, `skills/powerops-plan/SKILL.md` sections 9 and 10, `CLAUDE.md`, the six specialists, `docs/HANDOFF.md`, `docs/PLANNING-SKILLS.md`, `docs/COMMANDS.md`, `docs/RECOVERY.md`, `src/PowerOps.Core/Plans/PlanChecker.cs`, `tests/smoke/skills-smoke.sh` and the tests that pin the changed text. Tools the steps name, found by `command -v`: `dotnet`, `git`, `bash`, `jq`, `claude`, `codex`, `powerops` (stale build). The leak scan is not in this worktree (see R17).

Not applicable to a PowerOps milestone, with no Power Platform solution: item 1 (solution and publisher), 2 (connection references and environment variables), 3 (licences: the plan names no connector, table, app, flow, site, report or agent of its own; the smoke scenario's seeded plan is test data), 4 (DLP), 5 and 6 (Dev to Test to Prod route, managed or unmanaged: the plan's route is Git and the merge protocol), 7 (delegation), 8 (security roles of a table). Item 11 (size): each Data row maps to a Done line, and `Won't build` exists; no finding. Item 10: 0 open and 0 admin questions; the Log covers the three routine choices; no finding.

## Findings

### High

#### R1. The Power BI service write is covered outside Dev
- Area: 13 Buildability (two plan lines that can't both hold); the consent model
- Finding: Row 7 rewrites section 5 to "A service write (publish, refresh) that a Step names, in the plan's workspace, is covered by the sign-off". The sign-off of row 4 and Done line 1 authorize only "each write in Dev", and stop-list item (6) stops "a write outside Dev that no deployment yes covers". A plan whose workspace is a Test or production workspace (Power BI has no Dataverse environment, and section 5 says to name "only the one the plan names") is covered by row 7 and stopped by item (6) at once. Read as written, row 7 lets a sign-off publish to a production workspace with no question, which widens the model the Constraints say must not widen ("Each authority names its environment").
- Suggested fix: Row 7: "A service write (publish, refresh) that a Step names, in the plan's Dev workspace, is covered by the sign-off (rule 7 of section 3); any other workspace is a deployment and needs the deployment yes." Add a pin to row 9 (f) for "Dev workspace".
- Resolution: fixed: row 8 covers only the Dev workspace the plan names; any other workspace or stage needs the deployment yes; pinned (9 f).

### Medium

#### R2. No rule says which wins when a write is both authorized and on the stop list
- Area: 13 Buildability; the consent model (authorized list against stop list)
- Finding: Only item (5) says "even one a Step names", and item (3) is settled by rule 9. Nothing says the stop list wins over the sign-off or the deployment yes for the other items: a Step that names buying or assigning a licence (item 4: the preflight's "start no trial" covers trials only), a Step that shares with or messages people (item 7), or a Step-named `settings apply --apply` in Dev (covered by the sign-off's "each write in Dev that a Step names", yet the kept sentence of row 4 and `AGENTS.md:34` forbid it without a yes to that environment). The deployment yes also covers "the plan's named steps in that environment", which can include a licence assignment or a share in Test. The smoke's step 5 ("start a Power Apps Premium trial", Step-named) relies on a precedence the text doesn't state.
- Suggested fix: In row 4, open the stop list with "The stop list wins over both authorities above: an item on it stops even when a Step names it or a deployment yes was given." Also say that `settings apply --apply` and `deploy apply --apply` always need the deployment yes, in Dev too. Pin "wins over both authorities" in row 9 (d), and do the same in the decision text of row 16.
- Resolution: fixed: rule 7 opens with "The stop list wins over both", pinned (9 d); settings apply --apply needs the deployment yes in any environment.

#### R3. The deployment yes has no end once its import is done
- Area: Consent model; Constraints ("Each authority names ... its end")
- Finding: Row 4 ends the deployment yes on "a refusal, a declined, cancelled or failed form, a changed `signed-off-hash` or a `blocked` report". A successful import is not an end. A build that changes the zip after the import (a fix, then both roles again, then a new `deploy plan`) leaves the hash and the reports unchanged in kind, so the same yes would cover a second import of different bytes to the same environment in the same session. "Per deployment" is not defined anywhere the agent reads.
- Suggested fix: Define the deployment in row 4 and row 16: "A deployment is one `deploy plan` file to one target. The yes ends when that import has run (whatever its outcome), on a refusal, a declined, cancelled or failed form, a changed `signed-off-hash` or a `blocked` report." Pin it in row 9 (d).
- Resolution: fixed: a deployment is one saved deploy plan file to one target, and its yes ends once that import and its steps are done (row 5, decision 65).

#### R4. Stop item (7) names "a person the plan doesn't name", but plans name no people
- Area: Consent model
- Finding: Plans and reviews may not hold people's names (`powerops-review-plan` reports any name as a finding; AGENTS.md forbids them in fixtures), so a plan names roles, groups and test accounts, never a person. Read literally, item (7) stops every write that reaches anyone, including section 4 step 3's mandated trigger fire of an approval flow to a test account; read loosely, it lets a Step-named share reach a whole role. The preflight's "test accounts the plan's checks name" shows the intended meaning, but item (7) doesn't say it.
- Suggested fix: Row 4 item (7) and Done line 1: "a write that reaches a person outside the roles, groups and test accounts the plan names (a share, a message, an approval request)". Same wording in row 16; adjust the pin in row 9 (d).
- Resolution: fixed: class 7 says "a person, group or account the plan doesn't name".

#### R5. Undoing a slip "after their yes" can mean a delete, which rule 9 never allows
- Area: 13 Buildability; consent model
- Finding: Row 4 adds "undo it only after their yes" for a slip. Rule 9 says never delete, even when the user says so, and an object a slip created can be undone only by a delete. The two lines don't contradict outright ("only after" restricts), but an agent can read the yes as permission to delete what it created by mistake.
- Suggested fix: Row 4: "undo it only after their yes, and never by a delete: an object a slip created is a stray with an `open:` cleanup (rule 9)." Pin "never by a delete" in row 9 (d).
- Resolution: fixed: rule 6 adds "an undo that deletes is the person's (rule 9)".

#### R6. The Dev setting is authorized only if "the sign-off question listed" it, which the build can't check
- Area: 13 Buildability; 9 Testability
- Finding: Row 4's sign-off bullet covers "an environment-wide Dev setting a Step names (auditing, search), which the sign-off question listed". `Won't build` keeps the question's text out of `decisions.md`, and the build may run in another session or after a resume, so the agent can't tell whether the question listed the setting. Read as a condition, it asks or stops; read as a description, it is redundant with the Step. The smoke's seeded plan is signed off with `powerops plan sign-off`, not through the question, so scenario 11's step 2 ("a Step-named Dev setting goes ahead") depends on which reading the agent takes.
- Suggested fix: State in row 4 that the Step is the authority, and move the listing to row 10 as the plan skill's duty: "an environment-wide Dev setting a Step names (auditing, search); the sign-off question lists these for the person (powerops-plan section 9)". Keep Done line 1 in step with it.
- Resolution: fixed: Dev settings are covered only when the `sign-off covers:` Log line lists them; powerops-plan logs it at sign-off (row 10); the smoke seeds it.

#### R7. An agent's sign-off, or one given before M94, becomes authority to write without asking
- Area: Consent model
- Finding: The fix plan keeps amendments person-signed because "an agent that signs its own amendment would widen its own authority". The plan doesn't apply the same rule to the first sign-off: `powerops-implement`'s gate says "Warnings do not block", and `plan status` reports `sign-off answered by an agent` only as a warning, so a plan an agent signed now authorizes every Step-named Dev write with no question. A plan signed off before this milestone (the person saw no two-line disclosure and expected a question per write) also gains that authority silently.
- Suggested fix: In row 4's sign-off bullet: "The sign-off is your authority only when `plan status` shows no `sign-off answered by an agent` warning for it; otherwise ask once, at the preflight, whether the person signed it." Add one line to the preflight message of row 3: "Say in one line that you will now build the Steps in Dev without asking." Pin both in row 9.
- Resolution: fixed: without a `sign-off covers:` line marked (answered by: person), the preflight asks once (row 3, decision 65, Assumptions).

#### R8. The skill's own post-build writes are outside both authorities
- Area: Consent model; 9 Testability
- Finding: Section 4 of the implement skill requires writes the plan's Steps usually don't name: fixes of `powerops-verify` findings ("Fix each of its findings inside the plan"), a rebuild after a security finding, and the trigger fire of every changed flow ("Fire each flow's real trigger once with a synthetic input"). Row 6 covers design-check fixes only, and a flow run only "when its Step's check names it, with the test rows that check names". A plan signed off without those check lines makes every such write a stop-list question (item 2), which works against Done line 3 and the fix plan's ceiling of 25 person-calls.
- Suggested fix: Either cover them in row 4 and row 6 ("a fix of a verify finding to a component a Step built, in Dev; each flow's trigger fire with the synthetic input section 4 step 3 names, its rows journaled as `open:` cleanups"), or make row 10's sign-off lines and the plan skill's Steps guidance require each Step's check to name its test run and rows. Pin whichever is chosen.
- Resolution: fixed: the sign-off covers the checks and fixes section 4 names for its Steps (rows 5, 7).

#### R9. The gate hooks always block several routes the sign-off now covers without a question
- Area: 13 Buildability; consent model
- Finding: `docs/HANDOFF.md:101` says the gate always blocks `pac pcf push`, `pa app push`, `pac copilot push` and the `manage-agent` push, because "No plan check covers these": "the user runs it, or turns the hooks off for that step after saying yes to it". Row 4 names exactly these as covered by the sign-off with no question. With the hooks on, the agent is told it may go ahead and is then blocked; neither the skill nor the plan says what it does then. The plan doesn't say that a block stays a stop for the person, nor that the agent never edits the hooks or the client's settings to get past it. Row 15's rewrite of the product table (`:20`) risks dropping "and the gate always blocks it".
- Suggested fix: Add to row 4: "When the gate or a deny rule blocks a covered write, stop under item (1): the person runs it or turns the hook off for that step; never change hooks, permissions or settings yourself." In row 15, keep "the gate always blocks it" in the `:20` row and say the same for `:23` and `:25`. Pin the sentence in row 9 (d).
- Resolution: fixed: rule 7 says a gate hook's block is a stop, and the agent never edits a hook, permission rule or setting to get past it; pinned.

#### R10. Nothing starts the build right after sign-off
- Area: Goal (Done line 3); 9 Testability
- Finding: Done line 3 and the fix plan say `powerops-implement` "starts right after the sign-off ... in the same session". Row 2 puts "start in the same session" in the implement skill only, which an agent loads when it is asked to build. `powerops-plan` section 9 step 4 ends with "show the result", and row 10 changes only the text before the menu, so the session that signs off has no step that starts the build. No row 9 pin and no smoke check covers the hand-over: scenario 11 starts from an already signed-off plan with "Use the powerops-implement skill".
- Suggested fix: Add to row 10: section 9 step 4 ends "then start `powerops-implement` on this plan in the same session", with a row 9 (g) pin. If a smoke check is wanted, let scenario 1 (rounds) assert that the sign-off reply starts the preflight; otherwise name it as not checked.
- Resolution: fixed: section 9 step 4 starts powerops-implement after the sign-off unless the person said to stop at the plan (row 10).

#### R11. Repo text the plan misses that keeps the old model
- Area: Goal (Done line 6); the Done grep
- Finding: (a) `CLAUDE.md:43`: the `copilot-studio` plugin's "manage and test agents push, publish, run evaluations and chat with the agent in the tenant, so they run only after the person's yes". It matches the Done grep, is not in row 13 (lines 26, 27, 30, 42), and fits none of the allowed lines (a) to (c), yet row 4 now covers a Step-named Copilot Studio push in Dev. (b) `docs/HANDOFF.md:26`, the Power BI row: "Every service write (publish, refresh) needs a yes". Row 15 lists `:20, :23, :25` only, and `tests/PowerOps.Cli.Tests/HandoffDocTests.cs:149` pins "needs a yes" in that row, but the test file is in no step's files. (c) `docs/PLANNING-SKILLS.md:182`, the Amendments bullet: "`powerops-implement` stays stopped", which row 4 replaces with "go on only with steps that don't depend on it". Row 15 changes only that page's `powerops-implement` row and ceiling line.
- Suggested fix: Add `CLAUDE.md:43` to row 13 (the push in Dev covered when a Step names it; evaluations, publishing and chats after the person's yes for a stop-list item or a deployment, or as R2 settles). Add `:26` to row 15 and `HandoffDocTests.cs` to step 3's files, with the pin changed to the new wording and the reason in the test. Add `PLANNING-SKILLS.md:182` to row 15. Add all three to the expected Done-grep result in Constraints.
- Resolution: fixed: CLAUDE.md:43 (row 13), HANDOFF.md:26 with HandoffDocTests (rows 9, 15), PLANNING-SKILLS.md:182 (row 15).

#### R12. The new `plan status` warning is not documented
- Area: Goal (Done line 4); AGENTS.md "Add a command" recipe practice
- Finding: Row 1 adds a warning to `plan status`, but no row updates the pages that list its warnings: `docs/COMMANDS.md:880-884` (the write journal and the warnings `plan status` prints) and `docs/PLANNING-SKILLS.md:271` ("warns, without a violation, on a sign-off or an acceptance ..."). Row 15 touches `COMMANDS.md` only in `deploy apply`'s "Agents".
- Suggested fix: Add to row 15 (or row 1): `COMMANDS.md` after `:884` and `PLANNING-SKILLS.md:271` name the warning "`write '<#>' has no Authority`" for a table with an `Authority` column, and say that a table without that column is not checked.
- Resolution: fixed: row 15 documents the warning in COMMANDS.md plan status and PLANNING-SKILLS.md:271.

### Low

#### R13. Existing pins the rewrite breaks are not named
- Area: 9 Testability
- Finding: Row 3 folds "For a missing one, ask the person to sign in or to go on without it, and wait." into the preflight message, and row 4 replaces "stay stopped, with no further step". Both are pinned word for word (`tests/PowerOps.Cli.Tests/SkillsCommandTests.cs:317` and `:361`), but row 9 names only the `Implement_skill_asks_for_a_yes_...`, `..._the_import_paths_that_need_a_yes`, `Implement_skill_checks_the_final_saved_version_live...` and Fabric pins. An executor may find the other two only when step 3's test run fails, and change them without the reason AGENTS.md asks for.
- Suggested fix: Name both pins in row 9 (c) and (d), with their replacement text and a one-line reason in the test.
- Resolution: fixed: row 9 lists each existing pin kept or replaced, :317 and :361 included.

#### R14. The warning's format differs from the checker's other line warnings
- Area: 9 Testability; consistency
- Finding: Row 1 sets `<writes.md path>:<line>: write '<first cell>' has no Authority`. The checker's other line warnings read `<what> (<file>:<line>): <why>` with the file as `Display` gives it (relative, never a full path), for example "sign-off answered by an agent (decisions.md:13): ...". The plan doesn't say whether the path is the full path or the displayed one.
- Suggested fix: Use the existing form, for example `write '<#>' has no Authority (writes.md:<line>): name the plan step or the person's yes`, with the path from `Display`, and pin it in row 9 (a).
- Resolution: fixed: row 1 uses the (file:line) form of AgentAnswers.

#### R15. Row 4 drops the target sentence of the old rule 7, and the preflight leaves out the environment name
- Area: 13 Buildability
- Finding: Rule 7 today says "Use only the environment the person named (step 3), or the plan's `environment` when it holds a name, as the target." Row 4 says "Rule 7, rewritten" and quotes three sub-bullets that don't hold the sentence, so it is unclear whether the sentence stays. Row 3's preflight lists tools, sign-ins, licences, test accounts and connections, but not the question step 3 asks when `environment` is null or a role, so that question may come as a second message after the "ask once" batch.
- Suggested fix: Keep the sentence as the first line of the sign-off bullet in row 4, and add "the name of the Dev environment, when the plan's `environment` is null or a role" to the preflight list of row 3.
- Resolution: fixed: the target sentence stays verbatim (row 5) and the preflight asks the environment name (row 3).

#### R16. A Step-named evaluation or test run that uses credits is neither stopped nor named as a cost
- Area: Consent model
- Finding: `powerops-copilot-studio.md:56` says an evaluation run or chat "use the tenant and credits". Under row 4, a Copilot Studio evaluation or a flow run that a Step's check names is covered with no question, and stop item (4) covers buying or starting a licence, trial or capacity, not using credits. Rule 6 stops a choice that changes cost, but a Step-named run is not a choice. This is probably intended, but the plan doesn't say so, and row 14 rewrites line 56.
- Suggested fix: Say in row 4 or row 14 whether a credit-consuming run a Step names is covered (and the plan's cost line is its consent), or is a stop-list item.
- Resolution: fixed: a Copilot Studio evaluation or chat in Dev that a Step names is covered; buying credits or capacity is class 4.

#### R17. The checks of step 6 name tools without where they are
- Area: 12 Tools
- Finding: Step 6 and Done line 9 run "`powerops plan status`" and "the leak scan". The `powerops` on PATH is a stale install that doesn't hold row 1's warning, and the leak scan (`reports/leakscan.sh`) lives in the main checkout's ignored `reports/` folder, not in this worktree. Assumptions names neither.
- Suggested fix: Add to Assumptions: `plan status` runs from the worktree's build (`dotnet src/PowerOps.Cli/bin/Debug/net10.0/powerops.dll`), and the leak scan runs from the main checkout's `reports/leakscan.sh` against this worktree.
- Resolution: fixed: Constraints names the worktree build for plan status and the main checkout's leak scan.
