plan-hash: b0b29dff6a55989838e7f3d6f852494995a7dfcaf834d6fad38e352bd71ffa4f
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: the request is `tenant: offline` and confirms no `pac` sign-in, so `powerops plan facts` was not run.
Read besides the plan folder and the repo, as the request directed: the amendment, its samples and `_build/` files (read-only), the chain rules, the fix plan's M97 section, and the trial folder's tracked `mockups/` file list and the size of its `prototype.html` (not its `evidence/`).
Tools the steps name, checked with `command -v`: `python3` (3.14), `python`, `google-chrome-stable`, `dotnet`, `cmp`, `diff`, `git`, `gh`, `claude` and `bash` are on PATH, and `reports/leakscan.sh` and `reports/ci-watch.sh` exist; `py`, `google-chrome`, `chromium`, `chromium-browser`, `chrome` and `msedge` are not (the script tries them in order, so this is not a gap). `powerops-researcher` and the Fable reviewer are in this session's agent list. Not checkable here: whether a forked skill can start another forked skill (the plan's own unproven line), Chrome on the CI runners, and anything on Windows.
Checklist items 1 to 8 and 11 (solution, publisher, connection references, licences, DLP, Dev to Prod, managed state, delegation, security roles, table and screen size) do not apply: the plan changes PowerOps itself, builds no solution and names no table, flow or screen. The verification run's sample plan is drawn offline and never built.
Checked against the repo and the samples at `69b339a`: the sample claims of rows 10 and 13 hold (each sample has exactly one raw hex outside `:root`, `.btn[disabled]` `#BDBDBD`; no banned tag inside an `svg`, no `xmlns`, no `url(`, no `on...`, `src` or `xlink:href`; every `data-screen` element has class `frame` and a size class; at most one `btn primary` per frame; no `data-lead` or `data-for`; `kit.css` has no `.frame.browser` rule); the cited lines of `PlanChecker.cs`, `SkillsCommandTests.cs`, `PluginManifestTests.cs`, `skills-smoke.sh` and the docs say what the plan says; the CLI embeds every file under `skills/` (`skills/**/*`), so the samples, `kit.css` and `measure.py` ship.
This review replaces the one written for revision 3; it reviews revision 4. Status of the revision-3 findings: R1 (no `data-lead` in the calibration files) is answered by row 2's `lead: not marked`. R2 (the Python name) is answered by row 2 and step 5. R3 (the smoke) is answered for the accepted warning and the rerun limit; its turn budget is not, which is R6 below. R4 (web addresses by host and path) is kept on purpose by the plan and is R3 below. R5 (the Power Pages map's full link) is answered by row 10's `NOTICE.md` list. R6 (plugin descriptions) is answered by step 8 and Done line 6. R7 is answered for Chrome's network flags; the runner probe is not, which is R7 below. R8 is answered (step 9's "no" route, step 6 after step 5's probe, the workflow file in step 8's Files).

## Findings

### High

No findings.

### Medium

#### R1. The pick in the question rounds, the default path, keeps the old flow
- Area: 9 (testability); 10
- Finding: Row 15 changes only `powerops-plan` section 7 step 6, and step 6 limits that skill's edits to "section 7 step 6 only". But under `standard` and `deep`, the depths most plans use, the pick is asked in a question round, by `powerops-plan` section 6 (line 134) and `references/route.md` section 2. Route.md asks it as "three numbered options, each labelled with its option letter and direction", with no side-by-side picture. After a pick with no changes and "no more screens" it logs the `prototype:` line at once, and otherwise asks "approve or changes". It has no judge loop, no "Does it pass?" with the frames at 1:1, and none of row 15's end states. Row 19 changes only two other sentences of that section. On that path the person never sees what amendment 5.2 asks for. The last paragraph of route.md section 2 also says "A control map and control names in the sketch wait until one canvas build has tested whether `canvas-app` uses a sketch", which contradicts row 13(h)'s map, which holds the control per element. Step 4 owns route.md, so the fix lands in a step that exists.
- Suggested fix: Add route.md section 2's pick paragraph and its control-map sentence to row 19 and step 4's done-condition: the round's pick shows the side-by-side picture and asks "Which direction?", and the pass question, end states and log lines of row 15 follow the judge loop wherever the pick was asked. Let step 6 change `powerops-plan` section 6's sketch line too, or say why it needs no change. Add a pin for the new route.md wording in step 8.
- Resolution: fixed in revision 5 (row 19's route.md pick paragraph and control-map sentence, plan section 6's sketch line in step 6, step 4's done-condition)

#### R2. The calibration's three passed samples can fail gates they predate, and the plan has no route for that
- Area: 13; 9
- Finding: Done line 7 needs the three passed samples judged `at baseline`. The samples are byte-identical, and each scratch plan holds "only `Screens` lines" (row 16). Gate 3 fails a main frame that draws something the map marks "not confirmed", "not native" or "preview" "without the plan recording that choice". The shipped canvas map lists items under "Not confirmed, drawn anyway" (the grey count in each tab label, the grey fill of two time-used bars, the close icon on the banner), and the desktop queue draws them. With nothing recorded in the scratch plan, a judge that reads gate 3 as written fails the canvas sample. The research judge never applied gate 3: it was written after those rounds. Separately, Done line 3 lets a `measure.py` fault on a sample be "confirmed in the picture". A confirmed gate 6 fault (clipped or overlapping text, text under the floor) on a sample can't be fixed, because the sample is frozen, so Done line 7 can't then hold. Step 7 allows one judge revision that must not be "tuned to the five files", then stops the milestone. The likeliest miss has no planned answer.
- Suggested fix: In row 16, have each sample row's scratch `plan.md` record the choices its map makes (for example, one Assumptions line accepting the "drawn anyway" items), or say in `key.md` and `README.md` how gate 3 reads in calibration. In Done lines 3 and 7, say what a confirmed gate fault on a passed sample does: stop and report it to the person, since the samples are the level the owner passed, rather than revise the judge.
- Resolution: fixed in revision 5 (row 16: an Assumptions line in each sample's scratch plan; Done 3: a confirmed gate fault on a sample stops and is reported)

### Low

#### R3. The plan names web pages by host and path
- Area: URLs, GUIDs, emails and names
- Finding: The second paragraph of Assumptions and row 2 give Learn locations as a host and path with no scheme. A11 names a GitHub repository by owner and name. `plan status` doesn't flag them, and the decisions log keeps this form on purpose (the map must name pages the same way), but they are still web addresses.
- Suggested fix: None if the plan's reason is accepted. Otherwise name the pages by slug only, as A1 to A10 mostly do.
- Resolution: accepted (the map must name pages by path; decisions log, round 1)

#### R4. Some lines go stale and no row or check names them
- Area: 13; 9
- Finding: The SVG chart rule (rows 6 and 8) leaves three contradictions. The Power BI row of route.md section 2's table says "each visual drawn in CSS". The opening sentence of `power-bi.md` section 3 says "each visual drawn in CSS ... and no chart library", and row 8 replaces only the "Boxes" line. Route.md's "Each rule is written once" list says how a screen must look is in `look.md`, while row 5 moves the scale and gates to `baseline.md`. Separately, `docs/USE-WITH-AGENTS.md` line 7 says "PowerOps ships six agent skills", and row 19 names only that file's line 10. Step 4's contradiction check covers only `look.md` against `baseline.md`.
- Suggested fix: Add these lines to rows 8 and 19. Widen step 4's done-condition so that no line of the product files or route.md contradicts `baseline.md`, with the executor listing each replaced sentence.
- Resolution: fixed in revision 5 (rows 8 and 19; step 4 checks the product files and route.md too)

#### R5. Two departures from the amendment have no decision to cite
- Area: 10
- Finding: Amendment section 4 says "a missing judge file is a warning for plans signed off before this change". Row 13 instead runs no check on signed-off plans (the existing `PlanChecker.cs:175` gate). It also adds a route the amendment doesn't have: an approved plan with old sketches gets warnings and is refused at sign-off. Done line 1 marks a departure as "applied with a difference (decision Qn)", but no Q in `decisions.md` covers either one, so `applied.md` has nothing to cite for row 13.
- Suggested fix: Log one Q for both, with the reason row 13 gives (signed-off plans are left as they were; approved ones take a new options run or a person's skip), and cite it in row 13.
- Resolution: fixed in revision 5 (decisions Q13, cited in row 13)

#### R6. Scenario 6's turn budget isn't changed for the new pass question
- Area: 9 (testability)
- Finding: Scenario 6 runs with `max_turns + 7`, counted for "the pick's approval, the approval and the defaults question". Row 15 adds "Does it pass?" after the judge loop, and possibly a Fail follow-up. Row 19 changes the scenario's goal, file checks, byte checks and accepted warning, but not its turn count. A run that ends at "did not finish within N turns" fails the Claude leg, and step 8's two reruns would hit the same limit.
- Suggested fix: Say in row 19 how many turns the new flow adds to scenario 6 (and to any other scenario that reaches the pick), and raise its `max_turns` offset to match.
- Resolution: fixed in revision 5 (row 19: scenario 6's offset from 7 to 9)

#### R7. Two claims about where `measure.py` runs are not probed
- Area: 13; 12
- Finding: `SketchMeasureTests` skips only when no Python 3 or no Chrome is found. On the CI runners (Linux and Windows), a Chrome that is found but can't start headless (a sandbox restriction, for example) gives a red test. No step runs the script on a runner before the pull request, so the first CI run is the probe, and the plan doesn't say so. Row 2 also says the script "runs under any Python 3.8 or later", but the only interpreter here is 3.14, and no step checks the older syntax and library.
- Suggested fix: Add an `unproven:` line that the first CI run probes Chrome on both runners. Have the test skip with Chrome's message when the browser starts but `--self-test` can't render, not only when it is missing. Narrow the version claim to what is tested, or add a check (for example `ast.parse` with `feature_version=(3, 8)` in the self-test).
- Resolution: fixed in revision 5 (an unproven line for CI Chrome with exit 77 and a skip; the Python 3.8 check in the self-test)

#### R8. Gate 6 counts disabled-control text, and the kit styles it at about 1.6:1
- Area: 13
- Finding: Row 4 writes gate 6 as written: "any text under 4.5:1". The kit every sketch starts from (row 10) sets `.btn[disabled]` text to `#BDBDBD` on `--fill` `#F0F0F0`, about 1.6:1, and step 2 keeps that colour as a token. WCAG 1.4.3 exempts inactive controls, but the gate doesn't. So a sketch that draws a disabled button from the kit fails a literal judge, and `measure.py`'s contrast check reports it. No sample draws one, so the calibration doesn't show this.
- Suggested fix: Log a Q in `decisions.md` (the plan's Won't build asks that for a choice the amendment leaves open): gate 6 and `measure.py` skip text of a disabled control, as WCAG does, or the kit's disabled text token meets 4.5:1.
- Resolution: fixed in revision 5 (decisions Q14; rows 2 and 4)
