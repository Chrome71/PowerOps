---
status: approved
review: pending
signed-off: null
signed-off-hash: null
environment: null
depth: standard
supersedes: null
---

# M124: Planning quick fixes: quick for newcomers, no menu after each round, a person's pass as the sketch gate

## Goal

Item 4 of Phase 1 of the three-way debate plan (`reports/three-way-debate-2026-11-22/AGREED-PLAN.md`, git-ignored), step 5 of its build plan: "Remove cross-plan depth inheritance; drop the "what next?" menu after each round; under `quick` (the newcomer default) one sketch of the main screen or a stated skip, three options only on request; the person's Pass is the gate and the sketch judge runs only on request (change `PlanChecker` accordingly ...); update the planning smoke scenarios for both paths. Accept a Plans in Power Apps document, a Cowork `/app` brief or a Copilot Studio app description through the existing "draft of their own" path; generated choices keep their origin until the person endorses them."

Done when:

1. `skills/powerops-plan/SKILL.md` (Data row 1): the "newest-modified" depth clause is gone and the "plan it supersedes" clause stays; a new plan starts at `quick` (the header template says `depth: quick`) unless it supersedes a plan or the person asks for a depth; no lettered what-next menu follows a round; under `quick` the plan assumes standard licences unless the licence read shows a premium product held; the draft path names the three Microsoft sources and keeps generated choices marked until the person endorses them; under `quick` the sketch is one sketch of the main screen, three options and the judge only on request. `wc -w` is 3,865 or lower, and `Plan_skill_stays_under_its_word_ceiling` is lowered to the new count.
2. `skills/powerops-prototype/SKILL.md` (Data row 2) has a `main` mode: one `prototype.html` of the screen that carries the most Goal lines, with its states and map, rendered, no options, judged only when the request asks for it. `options`, `pick` and `change` are unchanged under `standard` and `deep`.
3. `PlanChecker` (Data row 3): the last `prototype:` line `approved mockups/prototype.html ... (answered by: person)` satisfies the prototype gate without a judge file and without option files; a person's `skipped (<reason>)` satisfies it as today and needs no judge file; a skip with no reason is refused; an approval without that mark still needs three passing options, a named passing option and a current judge verdict. New tests in `PlanCheckerPrototypeTests.cs` for each, plus sign-off tests through `PlanSignOff.Run`.
4. `tests/smoke/skills-smoke.sh` (Data row 4): scenarios 1, 2, 5, 6 and 9 rewritten, and scenario `draft`'s stop wording; scenario 5 tests that a new plan does not inherit an earlier plan's depth. `SMOKE_ONLY` takes a list of scenario names; `SMOKE_MAX_USD` stops the Claude run once the summed reported cost passes it; each scenario logs its minutes and cost.
5. One local Claude run of scenarios 1, 2, 5, 6 and 9, with `draft` (also changed), ends "all checks passed", with a summed reported cost of USD 10 or less (`SMOKE_MAX_USD=10`). If the guard trips first, or a run would pass the cap, the milestone stops and reports instead of merging.
6. `docs/PLANNING-SKILLS.md`, `docs/BUILD-PLAN.md` (decision 52 amended in place, an M124 section, a build-order item) and `docs/COMMANDS.md` (the `plan sign-off` prototype text) describe the new gate; `.github/workflows/skills-smoke.yml`'s timeout is set by Data row 5.
7. `powerops plan status` on every folder under `docs/plans/` gives the same output after the change as before it, except this plan's own folder (compared against a capture taken at `6a3f3b6` before any edit).
8. Unchanged, by `git diff origin/main`: the skip path's log form, the round-1 cost-tier question and its options, `PlanSignOff`'s hash, `SelfJudgedWarning`, `skills/powerops-review-sketch/`, `skills/powerops-plan/references/baseline.md`, `skills/powerops-verify`, `skills/powerops-review-security`, their plugin copies and `tests/eval/`. `RoleEvalTests` passes.
9. `dotnet build` (0 warnings), `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m124-planning-fixes` with no violation, and the leak scan (0 hits, control found) are clean; CI is green.

### Won't build

- No judge change and no change to the judge's file format: `powerops-review-sketch` and `baseline.md` stay as they are (they would rerun the five-row calibration).
- `standard` and `deep` keep three options in the rounds and the judge loop by default; only `quick` gets one sketch and the judge on request (the build plan's wording, "under `quick`").
- No `plan init`, no skill diet beyond what this change removes (AGREED-PLAN Phase 3).
- No full smoke run: only scenarios 1, 2, 5, 6 and 9 (the owner's USD 10 covers one run of those). The Codex leg is not run.
- No change to the fast path's lettered menu (section 8) or the sign-off menu (section 9): the build plan drops only the menu after each round.

## Users

- A newcomer planning their first app with standard licences: one round, an approval question, one sketch to pass or skip.
- An agent running `powerops-plan` and `powerops-prototype`, which load these texts.
- `powerops plan sign-off` and `plan status`, which enforce the prototype gate.

## Data

**Row 1. `skills/powerops-plan/SKILL.md`.** Edits, by section (line numbers from `6a3f3b6`):

- a. Section 6, "A new plan starts at the `depth` of the plan it supersedes, else of the newest-modified `docs/plans/*/plan.md` that has one, else `standard`." becomes "A new plan starts at the `depth` of the plan it supersedes, else at the depth the person asks for, else `quick`." The section 5 header template's `depth: standard` becomes `depth: quick`.
- b. The what-next menu goes: section 6 step 6 ("Ask what next, with lettered options ...") and the "**A:** run another round. **B:** section 7. **C:** ..." paragraph are removed. Step 5 becomes: show the three change lines, then in the same reply the next round's questions, ending with one plain line, "Or say approve, stop, or sketch the screens first." (the last two words only when `Screens` names anything). When nothing is left to ask, the next question is "Approve the draft?": **Approve** (recommended) or **Another round**; its answer is a `## Log` line (`approval asked: <answer>` with its mark), never a `### Q`, so it adds no round. "approve" goes to section 7; "stop" says where the files are and how to resume, and the status stays `draft` (the old C text, kept); "sketch" does section 7 step 6. "recommend B" in section 6 (twice) becomes "ask "Approve the draft?"". Section 2 step 3's "`draft`: go to the what-next menu (section 6)" becomes "`draft`: go on with the rounds (section 6)". Section 11's "Offer **D** whenever `Screens` names anything. On D, or a request for ... do section 7 step 6 and show the menu again." becomes "On a request for a screen, wireframe or mockup, do section 7 step 6, then go on where you were." The fast path keeps its menu with **D**.
- c. Section 3 step 8: "Under `quick` ask nothing: `Held` stays `unknown`." becomes: under `quick`, `Held` is never asked: it takes `references/licensing.md` section 1's values from the read (`yes (read)`, `no (read)`) or stays `unknown`, and the plan assumes the person holds standard licences only. The person's answer to the round-1 cost tier stands (their "noting otherwise", in the owner's words). The one addition: under `quick`, a row with no standard alternative whose `Held` is not `yes (read)` also goes under Open questions (drop the need, or the person names the premium licence). `licensing.md` section 1 gets the same clause in one sentence, so the two agree. The cost-tier question of round 1 and its three options are unchanged.
- d. Section 2 step 5: "When the person hands you a plan they drafted (a document, notes or pasted text)" names the Microsoft sources too: a Plans document from Power Apps (its PDF export, or pasted text), what they gave Cowork's App skill, or a description given to Apps in Copilot Studio (preview). A choice such a tool generated is logged `- from the draft (generated): <choice>` with no person mark, goes under Assumptions as `generated: <choice>`, and settles nothing until the person endorses it. It never settles the classes that are never defaults (section 6: the cost tier, `Held`, environments, security roles, the data-round questions) nor the route: those are asked as usual. For the rest, ask once, with the first questions, "Keep these N generated choices?": **Keep all** (recommended) or **Change some**, and log `generated kept: <N>` or `generated changed: <labels>` with its mark under `## Log`. In the fast path generated choices stay `generated:` Assumptions and settle nothing, so it still asks nothing beyond the route, the prefix and the sketch question. A licence or tenant fact in a generated draft is `unknown`, never the person's answer.
- e. Section 7 step 6, under `quick` (and in the fast path, which sets `quick`): start `powerops-prototype <name> main` instead of three options, show the sketch's main frame at 1:1 and ask "Does it pass?": **Pass**, **Fail**, or a skip with a reason. Three options (`options` mode, then the pick as today) and the judge (`powerops-review-sketch`) run only when the person asks. On Pass log `prototype: approved mockups/prototype.html (main screen: <why>) (answered by: person)`. The Fail follow-up, the end states and the self-judged rule stay as written.
- f. Word budget: the file ends at 3,865 words or fewer (`wc -w`); the edits above add about 260 words and cut about 45-80 (the plan review's count), so the generated-draft details of edit d (the log form, the batch question, the never-settled classes, the fast-path rule, the licence-fact rule) go into a new on-demand `references/drafts.md`, and section 2 step 5 keeps the three sources, "generated choices settle nothing until the person endorses them", and one sentence telling the agent to read `references/drafts.md` when the draft came from one of the three tools. The executor then tightens wording in the sections it edits (sections 2, 3, 6, 7, 11) without dropping an instruction or a pinned string. The ceiling in `SkillsCommandTests` is lowered to the new count.
- g. `skills/powerops-plan/references/route.md` section 2: "Offer **D** whenever `Screens` names anything." becomes "Offer a sketch whenever `Screens` names anything."; "before any "What next?" menu" becomes "before the next round's questions"; "under `quick` and in the fast path, start it at approval" names the `main` mode (one sketch, three options and the judge only on request); and "Sign-off needs `prototype.html`, its map, a current judge verdict, the three options and a log line that names the pick, or a person's skip with a reason" says a person's pass needs only `prototype.html` and its map, an approval not marked `(answered by: person)` needs the judge verdict and three options as before, and a person's skip a reason. Pinned route.md strings that stay true stay. `references/licensing.md` gets the one sentence of edit c; `references/drafts.md` is new (edit f). No other reference changes. `docs/USE-WITH-AGENTS.md` line 10 ("at approval under `quick` and in the fast path") says one sketch of the main screen there.

**Row 2. `skills/powerops-prototype/SKILL.md`.**

- a. Section 2 gets a fourth mode, **`main`**: write only `prototype.html`, with the one screen of `Screens` that carries the most Goal lines (a phone frame of it is fine), its four states and `prototype.map.md`, in the direction a maker of this product would expect; no options and no side-by-side picture. Render and look (section 6 step 1), then steps 4 to 7; steps 2 and 3 (the judge loop) run only when the request asks for the judge. The banner reads "Sketch: main screen". A later `options` request replaces nothing in `prototype.html` until a `pick`.
- b. The opening paragraph and section 3's judge-file bullet say: sign-off takes a person's pass, logged `(answered by: person)`, without a judge verdict or options; an approval without that mark needs the three options and a current verdict, as today.
- c. The frontmatter description and section 7 (Report) name the `main` mode in a clause each. Sections 3 to 5 (the file rules, design and data) are unchanged.

**Row 3. `src/PowerOps.Core/Plans/PlanChecker.cs`, `PrototypeProblems` and its helpers.** Let `pass` = the deciding line is an approval and `marked` (it ends `(answered by: person)`).

| Case | Today | After |
|---|---|---|
| `pass`, no judge file, no option files | refused: no current verdict; 0 options | satisfied |
| `pass`, judge file stale or odd | refused | satisfied |
| `pass`, current verdict `not at baseline`, no `(below baseline: <why>)` | refused | refused (decision 7 of the design amendment: the reason is still logged) |
| `pass`, `self-judged` verdict | warning | warning, unchanged (`SelfJudgedWarning`) |
| `pass`, the line names `(option <letter>`, that option missing or failing | refused | refused (the record of a pick) |
| `pass`, line names no option, fewer than three options | refused | satisfied |
| `pass`, line names an option that exists and passes, fewer than three options | refused | satisfied (the three-option minimum binds only unmarked approvals) |
| approval not marked | needs three options, a named passing option, a current verdict; warns | unchanged |
| skip with a reason, marked | satisfied | satisfied; a `prototype.html` without a judge file adds no "no current judge verdict" problem |
| skip with no reason, or unmarked | refused (no valid line) | unchanged |
| no line yet (a draft), `prototype.html` without a judge file | warns "no current judge verdict" | no such warning (so `main` mode's `plan status` is clean before the person answers); a judge file that exists but is stale still warns |

The file checks of `prototype.html`, its map and every option file that exists are unchanged. Messages keep their wording; the line problem's hint adds the pass form `prototype: approved mockups/prototype.html (<why>) (answered by: person)`.

**Row 4. `tests/smoke/skills-smoke.sh`.**

- a. `SMOKE_ONLY`: a space-separated list of scenario names (`draft rounds stop quick standard option-depth`); each scenario block runs only when its name is in the list or the list is empty. `SMOKE_ONLY=draft` keeps its meaning.
- b. `SMOKE_MAX_USD` (Claude only): `agent_turn` and `user_turn` run inside `$(...)`, so they append each call's `total_cost_usd` (from `--output-format json`; the user turn moves to json too and prints `.result`) to a file under `$work`, never a shell variable. Before every agent or user call, in the parent shell (`converse`, `check_fast_path` and any direct `agent_turn` caller), a guard sums the file and, when the sum plus the largest single call seen so far would pass the cap, prints "FAIL [<scenario>]: cost guard: USD <sum> spent, the next call could pass the cap of USD <cap>" and exits 1. So the run stops below the cap, not above it. A rerun passes the remainder of the USD 10 as `SMOKE_MAX_USD`. Each scenario prints `cost [<scenario>]: USD <x>` and `time [<scenario>]: <m> minutes` at its end; the summary prints the total.
- c. The simulated user's prompt and `converse`'s menu count drop the what-next menu. `converse` counts replies that show a lettered menu with "another round" (a lettered **A**-**D** item, by `menu_letters`' form, on the same line as "another round"), so the new "Approve the draft?" question's plain "Another round" option doesn't count; scenarios draft, 1, 2, 5, 6 and 9 fail on any. `sketch_offered` and its "no lettered option to sketch the screens" check go from scenarios 5 and 6 (the round menu that carried **D** is gone); `note_sketch` stays only where a check still reads it.
- c2. Scenario `draft`: its goal's "At the first what-next menu (A/B/C) reply with just: C" becomes "After the agent has logged what your draft settles and asked its first questions, reply with just: stop", and its done-condition matches `stop` as scenario 2's does. Its checks are unchanged.
- d. Scenario 1 (`rounds`, depth `quick` by default): approve when asked; skip the sketch with a reason; sign off now. Checks: as today, plus `depth: quick`, a person's `prototype: skipped (` line, no what-next menu.
- e. Scenario 2 (`stop`): answer round 1; to the next message reply just "stop". Checks: status `draft`, no what-next menu.
- f. Scenario 5 (`quick`, rewritten for no inheritance): the seeded earlier plan is `depth: deep` and done; the new plan must be `depth: quick`. One round; the licensing and environments questions; at most one other question. At approval the agent draws one sketch and asks "Does it pass?"; the user answers Pass. Checks: the licence checks as today; `mockups/prototype.html` exists, at most 163,840 bytes, with one distinct `data-screen` name; no `option-*.html`; no `prototype.judge.md`; `prototype.map.md` exists; the last `prototype:` line is `approved mockups/prototype.html ... (answered by: person)`; `plan status` shows no prototype or `mockups/` warning.
- g. Scenario 6 (`standard`): the first message asks for depth standard; the user answers rounds in their own words and says "approve" when asked "Approve the draft?", or in place of a fourth round. Checks: as today (options early, the pick in the rounds before the data questions, `prototype.html`, map and judge file, no option rewrite), plus `depth: standard` and no what-next menu.
- h. Scenario 9 (`option-depth`): the first message asks for depth standard, as scenario 6's does; one round answered with bare digits; then reply "stop". Checks as today: `depth: standard`, no `depth:` change logged; plus status `draft` and no what-next menu.
- i. Scenarios 3, 4 and 10 (the fast path's lettered menu) are untouched.
- j. The header comment's scenario list and the scenario comments say what each now tests.

**Row 5. Workflow timeout.** No per-scenario time was recorded before, so the run measures the six scenarios it runs (T6, minutes: draft, 1, 2, 5, 6 and 9). The six others are unchanged and the six changed ones only lose turns, so the full leg is not expected to take longer than M97's slowest (159 minutes). `timeout-minutes` stays 191 when T6 is 150 minutes or less; above that the milestone stops and reports. The comment says six of twelve scenarios were timed, records T6, the date and the Claude Code version, and that 191 was kept, not measured.

## Automation

None.

## Screens

None.

## Environments and route

Offline. No tenant call of any kind. The smoke run uses the script's fake `pac` and `az`, which are never signed in.

## Solution and publisher

None.

## Constraints

- The kept roles, their plugin copies, `tests/eval/`, `powerops-review-sketch` and `baseline.md` stay byte-for-byte (build-plan limits and WATCH for M124).
- `skills/powerops-plan/SKILL.md` ends at 3,865 words or fewer; its ceiling may only fall (M122).
- Pins in `SkillsCommandTests.cs` follow their text or narrow to the instruction that stays; none is dropped without its text going.
- The smoke run spends at most USD 10 in total, retries included (the owner's "Yes to all three", 2026-11-22).

## Assumptions

- A1. Claims checked by `powerops-researcher` on Microsoft Learn only, 2026-11-22 (page dates not returned by the Learn tool, so unverified): Plans in Power Apps is GA ("Plans is generally available and enabled by default"), stored in a solution, its content generated by agents that the person reviews step by step ("Looks good" or "Edit"), and exported only as PDF (learn.microsoft.com/power-apps/maker/plan-designer/plan-designer, .../create-plan). Learn names no `/app` command or "brief" for Cowork; it documents Copilot Cowork's App skill (Frontier), invoked "when you describe an app you want to build" (learn.microsoft.com/copilot/cowork/cowork-build-apps). Learn calls the Copilot Studio feature "Apps in Microsoft Copilot Studio", preview, "Create an app from a description" (learn.microsoft.com/microsoft-copilot-studio/apps-experience/create-app). Learn shows no mark of which plan content was generated after acceptance, so the "generated" mark is PowerOps's own rule. So the skill names the sources by Learn's names, not the brief's.
- A2. No row of `docs/ecosystem/compatibility.md` covers Plans, Cowork or Copilot Studio apps. Open entry ECO-0046 lists app building in Copilot Cowork as a build route the deploy gate doesn't cover; this milestone only reads a description from it as a draft, adds no route and doesn't touch the gate.
- A3. "The judge only on request" and "three sketches only on request" apply under `quick` (the brief and the build plan say "under `quick`"); the checker change is general, so a person's pass satisfies the gate at any depth. `standard` and `deep` keep the judge loop by default in the skill text.
- A4. `total_cost_usd` in `claude -p --output-format json` is the cost the CLI reports for that call; on a subscription it is a notional figure. The cap is held against that sum.
- A5. A plan status capture of all 99 folders under `docs/plans/` was taken at `6a3f3b6` before any edit (scratchpad `status-before.txt`); Done line 7 compares against it.
- A6. `powerops-implement` compares each built screen with "the same screen" in `prototype.html`; a `main` sketch holds one screen, so the others have no sketch to compare with. No implement edit here; recorded for the hand-over.
- default: the `main` mode's banner wording "Sketch: main screen".

## Open questions

## Questions for your admin

## Steps

1. **Plan review** (main session). Fable plan review until approved; then `powerops-review-plan` in a fresh context; answer every finding.
2. **Sign-off** (main session). `powerops plan sign-off docs/plans/m124-planning-fixes`, logged in `decisions.md` with the owner's answer as authority.
3. **Checker** (executor as assigned). Tests first in `tests/PowerOps.Core.Tests/Plans/PlanCheckerPrototypeTests.cs`: one per row of the Data row 3 table, with sign-off through `PlanSignOff.Run` for pass-without-judge (exit 0), skip-with-reason (exit 0), skip-without-reason (refused) and the old three-option path (unmarked: refused with fewer than three options or no verdict; passes with three and a verdict). Existing tests that rely on the person-marked default to show option or judge problems move to an unmarked approval line. Seen red, then `PlanChecker.cs`. `PlanCheckerTests.cs`'s `Sign_off_is_refused_for_an_approved_sketch_with_fewer_than_three_options_and_a_skip_needs_none` moves its three-option assertion to an unmarked line, and gains the marked case (satisfied). No test is deleted. Files: `PlanChecker.cs`, `PlanCheckerPrototypeTests.cs`, `PlanCheckerTests.cs`. Done: Data row 3; `dotnet test --project tests/PowerOps.Core.Tests` green. Commit.
4. **Smoke script** (executor as assigned; may run beside step 3: no shared file). Data row 4 in `tests/smoke/skills-smoke.sh`, bash 3.2 compatible. Done: `bash -n` passes; an offline run with a stub `claude` on PATH (JSON with `.result`, `.session_id` and `total_cost_usd`) shows that `SMOKE_ONLY` runs only the listed scenarios and that the guard prints its FAIL line and exits 1 before the call that could pass the cap; its output is in the report. No live run. Commit.
5. **Skill text and docs** (executor as assigned, after step 3). Data rows 1 and 2 into `skills/powerops-plan/SKILL.md`, `skills/powerops-plan/references/route.md` and `skills/powerops-prototype/SKILL.md`; `SkillsCommandTests.cs` pins and the lowered ceiling; the "Update goldens" recipe and a read of `git diff plugins tests/golden`; `skills/powerops-plan/references/licensing.md` (one sentence) and the new `references/drafts.md`; `docs/PLANNING-SKILLS.md` (lines 46, 68, 95, 104, 119, 131, 166, 182 at `6a3f3b6`), `docs/USE-WITH-AGENTS.md` line 10, `docs/COMMANDS.md`'s prototype text, `docs/BUILD-PLAN.md` (decision 52 amended in place, M124 section and table row, build-order item numbered at merge). Done: Done lines 1, 2, 6 (less the timeout) and 8; `dotnet test` green. Commit.
6. **Smoke run** (main session, background). `SMOKE_ONLY="draft rounds stop quick standard option-depth" SMOKE_MAX_USD=10 tests/smoke/skills-smoke.sh claude`, log kept outside the repo. Done: Done line 5. A failed check: read the transcript, fix the cause in the skill or script (the step's executor), and rerun only the failing scenarios, the sum of all runs still within USD 10. Then the timeout by Data row 5.
7. **Checks and records** (main session). Done lines 7 to 9; `decisions.md` log; `status: done`.
8. **Work review and PR.** Fable work review; merge lock, rebase, regenerate, rerun the checks; PR; CI green; merge; release the lock; hand-over `reports/trial-3/chain/m124-done-handover.md`.
