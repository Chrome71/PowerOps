plan-hash: 06932db5d88f756c24cd7027ae29a1878394ced1f0970cb248e148149401e6fd
model: Opus 5.5 (claude-opus-5-5)
fresh-context: yes

Environment facts were not read: nobody confirmed a `pac` sign-in, and the plan's `environment:` is null (no tenant call of any kind was made).
Tools the steps name, found by `command -v`: `dotnet`, `bash`, `claude`, `git`, `wc`, `jq`, `cksum`; `reports/leakscan.sh` exists in the main checkout; the `fable-reviewer` agent file exists. Whether `claude` is signed in can't be checked from here; Assumptions states it.
This is a PowerOps milestone plan (skill text, tests, docs), not a Power Platform solution. Checklist items 1 (solution and publisher), 2 (connection references), 3 (licences), 4 (DLP), 6 (managed or unmanaged), 7 (volume and delegation) and 11 (size: no table, list, flow or screen; `Won't build` is present) don't apply. Item 5 is the branch, pull request and merge protocol of the chain rules, which Steps name. Item 8 applies only to the content the plan writes (R7). Open questions: 0; questions for your admin: 0. The four question areas were answered by the building session on the owner's standing direction, as the Log says.
Scope (fix plan, section M93): all three changes and every listed file are covered. The deviations are stated with reasons: the neutral defaults come from the prototype skill, not `look.md`; logic.md names a plug-in and column security instead of the fix plan's longer list; SharePoint gets a Medium; section 11 stays. One addition goes beyond the scope (R9).

## Findings

### High

No findings.

### Medium

#### R1. The pick is logged as approved before the person has approved the picked file
- Area: 9. Testability (the sketch lifecycle in row 5)
- Finding: Row 5 tells the agent to run `pick <letter>` on the answer and "log the `prototype:` line under `## Log` at once". Only after that does it ask approve or changes in the following round, whenever the pick had changes or `prototype.html` shows more screens. So `prototype: approved mockups/prototype.html (option <letter>: <why>) (answered by: person)` is recorded before the person has seen that file. Nothing says to log again when the following round brings changes, and `PlanChecker` reads the last `prototype:` item, so a stale "approved" line stands. Section 7 step 6, which row 4 keeps word for word, logs only after the approve-or-changes answer, so the two paths record the same event in different orders.
- Suggested fix: In row 5, log the `prototype:` line at once only when the pick had no changes and `prototype.html` adds no screens. Otherwise log it on the following round's answer: `approved` after an approve, and after the `change` run and its approve when changes are asked. Keep row 11's `logged=1` check, which still holds while the plan is a draft. Pin the new wording in row 10 (c).
- Resolution: fixed: row 5 logs the `prototype:` line at once only for a pick with no changes and no added screens; otherwise after the approve-or-changes answer, in the following round or at approval when no round follows (Goal 1, pinned in row 10 (c)).

#### R2. The options can be drawn before the route is settled, and a later route change gets only a redone pick
- Area: 10. Unanswered areas (when the early options run may start)
- Finding: The options start once `Users` and one `Screens` line exist, at the latest by the first data question. The route (canvas, model-driven, Power Pages and so on) is a `Screens` line that `route.md` section 1 derives partly from data answers: records in Dataverse lead to a model-driven form, external users lead to Power Pages. The prototype skill requires every direction to be "buildable in the plan's route", and model-driven directions differ in metadata, not layout. If the data round changes the product after the pick, the plan's only remedy is "redo `pick <letter>`… one file, not three new options", which carries a direction drawn for the wrong product. The fix plan has the same gap.
- Suggested fix: In rows 3 and 5, make a route line in `Screens` (the product, with its reason) a condition of the early options run, with neutral defaults otherwise as now. In row 5, add that a route change after the pick runs `options` again and asks the pick again, instead of redoing `pick`. Add a short pin for it in row 10 (c).
- Resolution: fixed: the options wait for the route as well as `Users` and a `Screens` line, and a route change after the pick runs `options` again and asks the pick again (rows 3, 5, 13, Goal 1; ceiling 3,507).

### Low

#### R3. decisions.md holds URLs
- Area: Committed values (URLs, GUIDs, emails, names)
- Finding: The research line under `## Log` lists Learn page addresses (`<url>`, ten of them, without a scheme). `plan status` raises no violation, and the earlier pass accepted them as public hosts. They are still URLs in a plan file.
- Suggested fix: Keep only the page titles in the Log line, since `logic.md`'s Sources will carry the links, or record the acceptance again.
- Resolution: accepted: public Learn hosts and paths written without a scheme, not tenant values; `plan status` shows no violation (as M91's R15).

#### R4. The smoke's order check misses data question 1 and passes when no pick question is found
- Area: 9. Testability (row 11)
- Finding: The order check compares the pick's round with "the existing `rows|records|how many` match". That matches data question 2. Data question 1 ("Where does the data live today?") matches neither pattern, yet the plan's rule is "no later than the first data question". Row 11 also doesn't say what happens when no `### Q` matches `sketch|option [abc]|direction` (for example "Which layout do you want?"), and the pattern is given without case-insensitivity.
- Suggested fix: Add `live today|where does the data` to the data match. Fail with "no pick question in decisions.md" when no pick `### Q` is found. Use `grep -iE` for both patterns.
- Resolution: fixed: row 11 uses a case-insensitive data-round matcher that includes data question 1, and fails when no pick question is found.

#### R5. The smoke never exercises the redone pick at approval
- Area: 9. Testability (rows 4, 5, 11)
- Finding: Scenario 6 stops as soon as `status` is `approved` and a `prototype:` line exists. The early line already satisfies that, so the run ends in the approval turn. Any approval of a redrawn file (Theme block or `Screens` changed), and the `prototype:` line logged again for it, is never answered or checked. Only the `cksum` check guards that path, and it shows the options weren't rewritten. It doesn't show that the redo wrote one file or that the redo was approved.
- Suggested fix: Either extend the stop condition until the last `prototype:` item comes after the approval line whenever `prototype.html` changed after approval, or record in Assumptions that the redo-at-approval path is checked only by the pins.
- Resolution: accepted: the redo needs a Theme or `Screens` change after the pick that no scenario's user makes; its wording is pinned, and Assumptions says it is not exercised live.

#### R6. Row 15's timeout is the shared matrix job's, and the Codex smoke is skipped with no reason
- Area: 9. Testability (step 5, row 15)
- Finding: `.github/workflows/skills-smoke.yml` has one `timeout-minutes: 60` for a matrix that runs both `claude` and `codex`, so there is no separate "Claude job" to raise. The change starts a subagent inside the rounds on Codex too. Assumptions says only "The Codex smoke is not run", with no reason and no follow-up, so the first Codex check would be the weekly job after the merge.
- Suggested fix: In row 15, say the matrix job's timeout rises for both agents, or add a per-agent value. In Assumptions, give the reason Codex isn't run and name the follow-up (the next weekly run, read by the handover).
- Resolution: fixed: row 15 says the value applies to both agents' runs, and step 5 gives the reason the Codex smoke is not run (cost; the weekly job runs it).

#### R7. Item 8's wording can let a form-only rule pass
- Area: 8. Security roles (row 7's new item 8 text)
- Finding: "A rule that keeps a record valid, enforced only by a flow or a form after the record is stored" reads as if the form check also runs after the save. A form or app check runs before the save, on the client, and doesn't hold for an API call or a flow (`logic.md` section 2 already says so). A reviewer could read a form-only rule as outside the High.
- Suggested fix: Word it as "enforced only by a flow after the record is stored, or only in a form or an app", keeping the pinned phrase "which columns it can set when it creates a row".
- Resolution: fixed: item 8 reads "enforced only by a flow after the record is stored or only on a form or in an app".

#### R8. The pick question's letters can be confused with option numbers and the menu letters
- Area: 9. Testability (row 5, the pick as a `### Q`)
- Finding: Row 5 lists "options A, B and C by direction, the recommended one first". `decisions.md` numbers options (`1.` is the recommended one), the answer format is "`1 2 1`", and the what-next menu uses A to D. When the prototype recommends option b, the answer "1" means B and an answer "A" may be read as "another round".
- Suggested fix: In row 5, say that each option is written as `<n>. Option <letter>: <direction> (recommended)`, and that the answer names the number or the option letter. Log the letter in the `prototype:` line.
- Resolution: fixed: row 5 makes the pick three numbered options, each labelled with its option letter and direction.

#### R9. Row 12 carries an edit outside M93's scope
- Area: Scope fit (fix plan, section M93)
- Finding: Row 12 changes the fast path's route item in `docs/PLANNING-SKILLS.md` (`:189`, "M91's follow-up") to "each a short label with one sentence on what changes". The fix plan's M93 section doesn't name it, and neither Goal nor `Won't build` says why it rides here.
- Suggested fix: Give it a line under Goal 5 with its reason (the doc no longer matches the skill since M91), or move it to its own docs commit or to M95.
- Resolution: fixed: row 12 says why: M91's hand-over names it for M93, and it is one phrase in a file M93 already edits.
