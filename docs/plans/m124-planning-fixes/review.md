plan-hash: b91592cc6249bd0e7e9dc3472bd7641031a314dc866f99cae312caa4df752c3b
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: the request did not say the user confirmed that `pac` is signed in, and the plan is offline (`environment: null`). No `pac`, `az` or tenant call was made.

Tools checked with `command -v`: `claude`, `jq`, `python3`, `google-chrome-stable`, `dotnet`, `bash`, `wc` and `gh` are on PATH; `powerops` is not on PATH here and was run from a built `powerops.dll`. The leak scan and the chain files the plan names live in the git-ignored `reports/` folder of the main checkout, not in this worktree (R11). The Fable reviewer and `powerops-review-sketch` were not started; their presence was read from the agent and skill lists.

This plan changes PowerOps itself, not a Power Platform solution. Checklist items 1 (solution and publisher), 2 (connection references and environment variables), 3 (licences: the plan builds no connector, table, app, flow, site, report or agent, so no `### Licences` heading is needed), 4 (DLP), 5 and 6 (the Dev to Test to Prod route and managed or unmanaged; the repo route is branch, PR, CI and merge in step 8), 7 (data volume and delegation), 8 (security roles; read here as who can satisfy the sketch gate, R3 and R6) and 11 (size: no table, list, flow or screen; `Won't build` is present) do not apply as written. Item 10: 0 open questions and 0 admin questions; `decisions.md` has no `## Round` sections, because the owner's answers under Authority settle the scope.

## Findings

### High

#### R1. Data row 2 says section 3 of the prototype skill both changes and stays unchanged
- Area: 13 (buildability: two plan lines that can't both hold)
- Finding: Row 2b changes section 3's judge-file bullet, and row 2a gives `main` mode the banner "Sketch: main screen". Row 2c says "Sections 3 to 5 (the file rules, design and data) are unchanged". Section 3's Label bullet allows only "Sketch, option A of 3: <direction>" in an option and "Sketch, from option <letter>: <direction>" in `prototype.html`, so a `main` sketch's banner breaks a file rule that row 2c keeps. Done line 2 can't be met as the rows are written.
- Suggested fix: In row 2c, say that sections 3 to 5 are unchanged apart from the judge-file bullet (row 2b) and the Label bullet, which gets the `main` banner "Sketch: main screen" for `prototype.html` drawn in `main` mode. Add a pin for the new Label text.
- Resolution: fixed: row 2c names the judge-file and Label bullets as the two section 3 changes, with a pin for the Label text.

### Medium

#### R2. Under `quick`, a Fail still starts the judge through `change` mode
- Area: 9 (testability), 13 (buildability)
- Finding: Edit 1e keeps the Fail follow-up, which runs `change prototype.html`. In the prototype skill, "A changed `prototype.html` goes through section 6 again from step 1", and that includes steps 2 and 3 (the judge loop). Its Per agent line also tells Claude Code to start `powerops-review-sketch`. Row 2a skips the judge only for `main` mode, and row 2 says only that `options`, `pick` and `change` are "unchanged under `standard` and `deep`". So under `quick`, a Fail on the main sketch runs the judge the person never asked for, which contradicts "the judge only on request". No smoke scenario covers this: scenario 5 answers Pass.
- Suggested fix: In row 2a, say that `change prototype.html` on a sketch drawn in `main` mode (or under `quick`) runs section 6 steps 1 and 4 to 7, and runs steps 2 and 3 only when the request asks for the judge. Make the Per agent line conditional to match. Name the case in Done line 2.
- Resolution: fixed: rows 1e and 2a: under `quick` a `change` of a `main` sketch runs no judge unless the person asks.

#### R3. Editing the sketch removes the need for a below-baseline reason
- Area: 8 (who can satisfy the gate), 9 (testability)
- Finding: In the Data row 3 table, a marked pass with a current `not at baseline` verdict and no `(below baseline: <why>)` is refused, citing decision 7 ("the reason is still logged"). A marked pass whose judge file is "stale or odd" is satisfied. So any edit to `prototype.html` after a `not at baseline` verdict makes the verdict stale, and the pass then needs no reason. The rule the table keeps can be avoided by a one-byte change.
- Suggested fix: Pick one rule and state it in row 3. Either (a) for a marked pass, the last `verdict:` in an existing judge file counts whether or not its hash matches, so `not at baseline` still needs `(below baseline: <why>)`; or (b) record that a stale verdict is ignored on purpose, and log that as a person's acceptance. Add a test for the chosen case.
- Resolution: fixed: row 3 now refuses a person's pass without `(below baseline: <why>)` whenever the judge file's last verdict is `not at baseline`, current or stale.

#### R4. The cost guard rests on `total_cost_usd`, with no source and no probe
- Area: 13 (buildability)
- Finding: Row 4b and A4 assume that `claude -p --output-format json` returns a numeric `total_cost_usd` for each call, for both the agent call and the user call. The plan gives no source and no `unproven:` line. Step 4's stub `claude` proves only the script's arithmetic, never the real field. If the field is missing or null, `jq` yields `null`, the sum stays at 0, and the guard never trips, so the owner's USD 10 cap (Constraints, Done line 5) is not held.
- Suggested fix: Add a source for the field, or `unproven: claude -p json reports total_cost_usd per call; probed by step 6` with a first single call that checks the field is a number before any scenario runs. Make the guard fail closed: a missing or non-numeric cost stops the run with a FAIL line. Add that case to the stub test in step 4.
- Resolution: fixed: an `unproven:` line with a probe before the run (step 6); the guard fails closed on a call with no numeric `total_cost_usd`.

#### R5. The guard can still pass the USD 10 cap
- Area: 3 (cost), 9 (testability)
- Finding: The guard stops when the sum plus "the largest single call seen so far" would pass the cap. The very first call has no earlier call to compare with, and any call larger than all earlier ones can pass the cap. Scenario 6's judge rounds and long rounds are likely to be the largest calls. Row 4b nonetheless says "So the run stops below the cap, not above it", and Constraints says "at most USD 10 in total, retries included". The guard is a heuristic, not the bound the owner's authority sets.
- Suggested fix: Reserve a margin. For example, compare against max(largest call seen, a floor such as USD 1.50), or run with `SMOKE_MAX_USD` set below 10 by that margin. Also reword row 4b so it does not claim a guarantee. Alternatively, use a per-call budget flag of the CLI if one exists, recorded as `unproven:` with a probe.
- Resolution: fixed: row 4b passes `--max-budget-usd` with the remainder on every call when `SMOKE_MAX_USD` is set, beside the sum guard.

### Low

#### R6. The checker change is wider than "under `quick`", and the docs should say so
- Area: 10 (unanswered areas), 13
- Finding: The standard-path log line already ends `(answered by: person)` (plan skill section 7 step 6: "Each ends with an `(answered by: ...)` mark"). So after this change, every person-approved plan at `standard` or `deep` meets the gate without three options or a current verdict. Only agent-marked approvals keep the machine check, and the self-judged refusal ("self-judged, so the approval must end (answered by: person)") can no longer trigger for a pass. A3 records the change as deliberate. Even so, Won't build's "`standard` and `deep` keep three options ... and the judge loop by default" can be read as still enforced.
- Suggested fix: In Won't build (or A3), say that at `standard` and `deep` the skill text and smoke scenario 6 hold the three options and the judge, and the checker does not. Have Done line 6's `docs/COMMANDS.md` text say the same.
- Resolution: fixed: a Constraints line says the checker change is depth-independent; `docs/COMMANDS.md` and decision 52's amendment say so.

#### R7. Menu letters left over in the plan skill
- Area: 9 (testability)
- Finding: Row 1b removes the round menu, but the section heading "## 7. Approve (B)" still names letter B, and row 1b does not list it. Section 11's new text drops "On D", while the fast path (section 8 step 5) keeps **D** "sketch the screens first". The fast path's D is covered only by the general "a request for a screen".
- Suggested fix: Add to row 1b: the heading becomes "## 7. Approve", and section 8's **D** says "do section 7 step 6".
- Resolution: fixed: row 1b renames the heading "## 7. Approve" with its pins, and keeps section 11's handler for the fast path's **D**.

#### R8. Smoke rows leave the done-conditions implicit and the shared harness partly untested
- Area: 9 (testability)
- Finding: Scenarios 2 and 9 end today on `message =~ C`. Rows 4e and 4h change the reply to "stop" but don't say their `done_when` changes. Row c2 says draft's condition "matches `stop` as scenario 2's does", which points at a line that must itself change first. The changes to `user_turn` (json), the simulated user's prompt, `converse`'s count and the guard are shared by scenarios 3, 4, 7, 8, 10 and 11 and by the Codex leg, which this milestone doesn't run.
- Suggested fix: State the new `done_when` for scenarios 2, 9 and draft. In step 4's offline stub run, also run one untouched scenario (for example `fast-path`) through the changed functions. Otherwise record under Assumptions that the weekly run is the first test of the six other scenarios and of Codex.
- Resolution: fixed: row 4i states the stop-condition change and runs every scenario once against the stub; the live run of the others is the weekly job's (recorded as not checked).

#### R9. No pins are named for the new instructions
- Area: 9 (testability)
- Finding: Constraints covers pins that follow their text. Step 5 says only "`SkillsCommandTests.cs` pins". No new pin is named for the `main` mode, "Approve the draft?" and its log line, the closing "Or say approve, stop, or sketch the screens first.", the Pass log form `(main screen: <why>)`, or `references/drafts.md` (its rules and its install beside the skill, which `Install_writes_the_plan_references_beside_their_skill` lists by name).
- Suggested fix: List the new pins in step 5, including `drafts.md` in the install test's reference list.
- Resolution: fixed: step 5 names the new pins and `drafts.md` in the shipped and installed lists.

#### R10. The plan holds URLs
- Area: plan hygiene (URL, GUID, email or name)
- Finding: Assumption A1 cites three Microsoft Learn pages by host and path.
- Suggested fix: Cite the pages by title only, and keep the addresses in the private research notes.
- Resolution: accepted: the three are Learn paths without a scheme, the sources of A1; `plan status` passes them.

#### R11. Files the plan names are not in this worktree
- Area: 12 (tools)
- Finding: Done line 9's "leak scan", the chain rules (`reports/trial-3/chain/chain-rules.md`), the debate plan and the hand-over path are all under `reports/`. That folder is git-ignored and exists only in the main checkout, and the worktree where the steps run has no `reports/` folder. Done line 9 names the leak scan without a path.
- Suggested fix: Give the leak scan's path (the main checkout's `reports/` script) and say that step 7 and step 8 run it, and write the hand-over, from the main checkout.
- Resolution: fixed: Done line 9 gives the leak scan command and path; the chain files are read from the main checkout by design (chain-rules: write nothing else under the main checkout).
