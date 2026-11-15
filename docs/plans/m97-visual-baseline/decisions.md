# Decisions: m97-visual-baseline

The owner's answers that apply to this milestone. Asked 2026-11-11 "Take the recommended answer on all seven design decisions?" (`reports/design-research/amendment.md`, section 7), the owner answered "Yes, all recommended". So:

1. The loop runs by default on `prototype.html`, not on the three lead frames.
2. The judge is a new skill, `powerops-review-sketch`.
3. Inline SVG is allowed in sketches, with the limits of the amendment's section 4.
4. The samples ship in the plugins.
5. The sketch size cap rises to 163,840 bytes.
6. Imagery stays drawn as inline SVG in the sketch, and the plan's Theme block names the real source (subject, style, where it comes from).
7. Only a person may pass a sketch below the baseline, logged with the reason.

The authority to build: "Start the build" (the owner, 2026-11-11, for M86 to M97). The brief that started this session also relays the owner's "Finish M97, M109, M115" (before trial 4); the brief dates it 2026-11-15 while this machine's date is 2026-11-14, so it is quoted from the brief, not dated here. The fix plan's other answers (Q1 "One yes per deployment", Q2 "Hide by default", Q3 "Keep out of Git", Q4 "Rerun it") were built by M92 to M96 and change nothing here. The routine choices below were made by the building session under the owner's direction of 2026-11-11 ("as autonomous as possible") and the standing instruction that sessions sign off their own milestone plans.

## Round 1 (planning)

### Q1. Where do the amendment's calibration fixtures go?
1. `tests/sketch-eval/`, beside `tests/eval/` and not inside it (recommended)
2. `tests/eval/sketch/`, as the amendment says
3. Inside the judge skill, so they ship
Answer: 1. `tests/eval/` is frozen for every milestone but M96 (chain rules), and `RoleEvalTests` reads it; a new folder there would change a frozen path. The fixtures measure the judge, not the kept roles, so they need no place in that folder. They don't ship: the trial sketch and the failed site version are evidence, not a picture of the level. (answered by: agent m97 session, on the owner's standing direction)

### Q2. What is the judge's measuring script (amendment section 6, layer 2)?
1. `measure.py`: Python 3 standard library, run against a temp copy of the sketch with a measuring script added, through Chrome or Chromium headless `--dump-dom`; a `--self-test`, and an xUnit test that runs it where `python3` and a Chrome are on PATH (recommended)
2. A Node script with Playwright
3. A JavaScript file the judge pastes into whatever browser tool it has
Answer: 1. It adds no dependency (Playwright is not installed here and would be a new one), and Chrome is on the machines that render sketches and on GitHub's Linux and Windows runners. The test skips where either is missing and says so; the self-test is the check that holds everywhere a judge runs. Not measured by it: whether two numbers with one label agree (gate 2): a text match can't pair a number with its label reliably, so that stays with the judge's eyes (layer 3). (answered by: agent m97 session, on the owner's standing direction)

### Q3. How does a text check know what is inside a frame or an `svg`?
1. A light tag stack over start and end tags: void elements and `/>` don't open, an end tag closes back to its match (recommended)
2. Check only the `data-screen` element's own attributes
Answer: 1. Rows 13 (a), (d) and (k) need nesting (a banned child inside `svg`, one `data-lead` and one filled button per frame, the frame class on the element or its parent). The sketches are hand-written and well formed; a stray end tag closes back to its match as a browser would for these cases. The rule stays "a text check that catches the honest mistake", as M70 set it. (answered by: agent m97 session, on the owner's standing direction)

### Q4. Which elements are banned inside an `svg`?
1. The amendment's list (`script`, `foreignObject`, `image`, `a`, `style`, `use` with a non-`#` href, `on...`, `url(`), plus `animate`, `set`, `animateMotion` and `animateTransform`, and `xlink:href` held to the `href` rule (recommended)
2. The amendment's list only
Answer: 1. A sketch already draws nothing animated (section 3's last rule), and an animation element can rewrite another element's attributes, which is a way around the `href` rule. `xlink:href` is the older spelling of the same reference. This is a safety rule, not a visual one. (answered by: agent m97 session, on the owner's standing direction)

### Q5. Where does "tokens only, no raw hex outside `:root`" apply?
1. To CSS declaration values outside every `:root` block (a file may hold several), in the `style` element and in `style` attributes; selectors such as `#id` never match. SVG presentation attributes (`fill`, `stroke`, `stop-color`) may hold hex (recommended)
2. Everywhere, SVG attributes included
Answer: 1. A presentation attribute can't take `var()`, so option 2 would force every drawn shape into a `style` attribute and double the size of the illustration the owner asked for on a public site (decision 6). The shipped `kit.css` loses its one raw hex outside `:root`. (answered by: agent m97 session, on the owner's standing direction)

### Q6. The amendment's layer 1 lists "the number of distinct `font-size` values used inside each frame against S4". Text check or measured?
1. Measured, by `measure.py`, from computed styles per frame (recommended)
2. A text check of `font-size` declarations
Answer: 1. Which rules apply inside a frame depends on the cascade; a text count of declarations would count rules no frame uses. The amendment's layer 2 row names S4 too. (answered by: agent m97 session, on the owner's standing direction)

### Q7. The judge file's format?
1. Header lines `judge: fresh context` or `judge: self-judged`; the six parts; the file's last `verdict:` line decides. Each round adds a `## Round <n>` section, so the scores per round stay in one file (recommended)
2. One file per round
Answer: 1, with the hash of Q11. The checker reads one file; the report shows the rounds. "Self-judged" is how an agent without subagents says it (amendment 2.2), and the tool warns and then requires a person's approval. A file with no `judge:` line, or a `verdict:` with other words, counts as no current verdict. (answered by: agent m97 session, on the owner's standing direction)

### Q8. Do the shipped samples change?
1. No: byte-identical to the owner-passed files, with a `NOTICE.md` for the Fluent icon paths in their sprites; only `kit.css` loses one raw hex (recommended)
2. Add `data-for`, `data-lead` and a `route:` comment so they pass the new checks
Answer: 1. They are what the owner passed; they are "the level, not a template", and the file rules are in `powerops-prototype` section 3. A test runs them through the checks and asserts their exact problem list: none is a banned tag, attribute, CSS, `href`, `url(` or size problem, which proves the SVG rules accept a real sample; each has one token problem, the embedded kit's `.btn[disabled]` `color:#BDBDBD`, and the markers that came after them (`route:`, `data-lead`, `data-for`, the map, the judge file) are expected problems; `NOTICE.md` names all of these. `kit.css` is the starting point of a new file, not a passed sample, so it gains the Power Pages sample's `.frame.browser` rules (it had none) and loses its one raw hex. (answered by: agent m97 session, on the owner's standing direction)

### Q9. Who starts the judge when `powerops-prototype` itself runs forked?
1. The prototype starts `powerops-review-sketch` if it can; where it can't start a fresh context, it ends with "judge needed: <file>", and `powerops-plan` starts the judge and passes its file back with `change prototype.html` for the next round (recommended)
2. Always `powerops-plan`
Answer: 1. Whether a forked skill can start another forked skill in Claude Code is unproven (plan Assumptions); the text works either way, and the author never judges its own file. (answered by: agent m97 session, on the owner's standing direction)

### Q10. Does the M93 follow-up "a run that sets the Theme after the pick" belong here?
1. Yes, as the verification run: options with no Theme block, then the Theme, then `pick` (recommended)
2. A new smoke scenario
Answer: 1. The verification run is needed anyway and exercises the redone pick at no extra cost; a smoke scenario would add a judge loop to the weekly job's time, which is not measured. (answered by: agent m97 session, on the owner's standing direction)

### Q11. Is the verdict tied to the file it judged?
1. Yes: the judge writes `sketch-hash:`, the SHA-256 of the judged file's text with CRLF made LF that `measure.py measure` prints (the checker hashes it the same way, as the plan body hash is, so a Windows checkout doesn't break it), and the checker treats a mismatch as no current verdict (recommended)
2. No: `docs/COMMANDS.md` says the check doesn't prove the verdict is of the current file, and the skill reruns the judge after any edit
Answer: 1 (the Fable plan review's finding 6). Steps 4 to 7 of the prototype's section 6 and `change <file>` can edit the sketch after its last verdict; a hash makes any edit need a new round, which the skill text says. The hash binds the file, not the judge's reasoning, and `docs/COMMANDS.md` says so. (answered by: agent m97 session, on the owner's standing direction)

### Q12. Which element is "the frame" of a screen?
1. The `data-screen` element when its class holds `frame`, else its parent element; a screen name may repeat across frames, and the 1 to 8 limit counts distinct names (recommended)
2. The `data-screen` element only
Answer: 1. It is the amendment's "or its nearest ancestor" read as the parent, which a text check can test without guessing; the samples put `data-screen` on the frame itself. Repeated names let one screen have a desktop and a phone frame with one set of states (`data-for`). (answered by: agent m97 session, on the owner's standing direction)

### Q13. Which plans do the new sketch checks reach?
1. As today, only a plan whose status is `draft` or `approved` (`PlanChecker.cs:175`): signed-off, implementing and done plans get no prototype check, and an approved plan with sketches from before this change takes a new options run or a person's `prototype: skipped (sketched before the baseline)` (recommended)
2. The amendment's words: "a missing judge file is a warning for plans signed off before this change"
Answer: 1. A signed-off plan already gets no prototype check, so for it the amendment's warning would be a new check, not a softer one; the result the amendment wants (old plans are not blocked) holds. An approved plan not yet signed off is still being planned, so it meets the new bar, with decision 52's escape for a person extended to it. `applied.md` marks row 13 "applied with a difference (decision Q13)". (answered by: agent m97 session, on the owner's standing direction)

### Q14. Does gate 6 count the text of a disabled control?
1. No: gate 6 and `measure.py` skip it, as WCAG 1.4.3 exempts text of an inactive component (recommended)
2. Yes, and the kit's disabled text token rises to 4.5:1
Answer: 1. The gate is the WCAG floor written for a sketch; WCAG doesn't hold disabled controls to it, and a disabled button drawn at 4.5:1 reads as enabled. The kit's `#BDBDBD` stays (as a token). (answered by: agent m97 session, on the owner's standing direction)

### Q15. Do avatar initials count under a site's 14 px text floor (gate 6)?
Asked of the person (relayed by the session that started this one), after step 5 found the passed Power Pages sample's avatar initials ("MS", beside "Maria S.") at 12 px: "May avatar initials shown beside the person's name go under the 14 px site text floor?"
Answer: "Yes, exempt them" (2026-11-15). Gate 6's size floor exempts the initials inside an avatar shown beside the person's name, because they repeat that name; their contrast still counts. Logged like Q14's disabled-control exemption; the passed Power Pages sample stays `at baseline` in the calibration key. (answered by: person)

### Q16. How does M97 go on after the judge failed two passed samples?
Asked of the person (relayed by the session that started this one) after calibration round 1, where the judge failed the canvas and Power Pages samples the owner passed on four confirmed faults (`calibration.md`): "How should M97 handle the four faults in the passed samples?"
Answer: "Fix samples, rerun" (2026-11-15). Fix the four faults in the canvas and Power Pages samples, reword gate 1 to "cover the title", as S1 does, and rerun all five rows; before the merge, the person sees the changed samples beside the originals, with the prototype verification output. Q15's exemption still covers only initials beside the name: the sample's phone header is fixed, and the exemption is not widened. This is the calibration's one revision (plan step 7). (answered by: person)

### Q17. How does M97 go on after the second calibration miss?
Asked of the person (relayed by the session that started this one) after calibration round 2, where both passed samples failed gate 1 on a full-width docked phone button and the canvas sample failed gate 4 on parts round 1's judge passed: "How should M97 go on after the second calibration miss?"
Answer: "Rule it, one more round" (2026-11-15). Gate 1 rules, narrowly, that one docked full-width primary action at the bottom of a phone frame is the second look and does not compete with the lead, an exception beside Q14's and Q15's; the canvas "Scheduled" badge tint is fixed; one more round of all five rows runs. On one condition: if round 3 again finds faults on parts an earlier round's judge passed and no fix touched, the judge is not chased further: the remaining misses are recorded in `tests/sketch-eval/key.md` as `not at baseline` with the reasons, the PR is finished and shipped, and judge consistency (with the skill-text gaps the round 2 judges named: gate 3 with no map, an ellipsis counted twice, a sketch of several products, a first look on a tab, what earns Identity 3) is a follow-up in the hand-over; the milestone does not stop again for that case. A miss caused by a fault a fix did touch still goes to the person. Before the merge the person sees the changed samples. (answered by: person)

## Log

- 2026-11-14: started by the M97 building session (Opus) in its own worktree at `69b339a` (`origin/main`). Depth `standard`: skill and reference text, a new skill with a script, sketch checks in `PlanChecker.cs`; no tenant.
- 2026-11-14: research by `powerops-researcher` (Opus, Learn and Microsoft's public GitHub only, no tenant call), recorded in `plan.md` Assumptions A1 to A11.
- 2026-11-14: plan review round 1. Fable: REJECTED, 13 findings; the fresh `powerops-review-plan`: 1 High, 5 Medium, 6 Low (review.md). All answered in the plan's revision 2: Data rows 1, 2, 10, 13, 15, 16, 18 and 19, Done lines 1, 3, 7, 9 and 10, Steps (reordered, the runs before the work review), Assumptions, and Q7, Q8, Q11, Q12 here; R7 (a Learn path without a scheme in the plan) is kept: `plan status` reads it as no URL, and the plan names pages that way on purpose, as the map must.
- 2026-11-14: plan review round 2. Fable: REJECTED, 4 findings (the samples' one token problem, two not-at-baseline rows not three, the hash with CRLF made LF, three step contracts: the pins' step, the smoke timeout's owner, the leak scan's timing). All fixed in revision 3; the smoke grants `measure.py` by path, per its WATCH.
- 2026-11-14: fresh review round 2 (review.md): 0 High, 3 Medium, 5 Low, all fixed in revision 4 (no `data-lead` in older files, the Python name on Windows, the smoke's accepted warning and rerun limit, the plugin descriptions, Chrome's network flags, the order of steps 5 and 6, a "no" that the skill caused). R1 of round 1 (Learn pages by path) stays as answered.
- 2026-11-14: plan review round 3. Fable: APPROVED revision 4, executors assigned (steps 2 to 8 on Opus; Haiku only to run the smoke leg). Fresh `powerops-review-plan` on revision 4: 0 High, 2 Medium, 6 Low; all answered in revision 5 (R1 the in-round pick, R2 the samples' gate 3 and a confirmed fault on a sample, R4 stale lines, R5 Q13, R6 scenario 6's turns, R7 the CI Chrome probe and the Python version check, R8 Q14), R3 accepted as before. The revision 5 changes are narrow wording on lines both reviews had read, so no fourth review round ran; `review.md` reads stale against revision 5 for that reason.
- 2026-11-14: signed off by the building session (agent m97, Opus) with `powerops plan sign-off`, on the owner's standing instruction that sessions sign off their own milestone plans after clean reviews (2026-10-22, repeated 2026-11-09 as "Sessions sign off") and the owner's "Start the build" (2026-11-11) for M86 to M97. Fable approved round 3; the fresh review has no finding open (each resolved in revision 5 or accepted). `plan status`: 0 violations, 1 warning (the stale review, as logged above).
- 2026-11-14: step 6 started beside step 5 rather than after its probe report: row 2 fixes the `measure.py` commands (`measure`, `shoot`, `side-by-side`, `--self-test`), and the probe can change only how they work inside, not their names; a change to them would be passed on by the main session. (answered by: agent m97 session, on the owner's standing direction)
- 2026-11-14: step 5 found a confirmed gate 6 fault on the passed Power Pages sample (12 px avatar initials beside the name, under the 14 px site floor). By Done line 3 it went to the person (relayed through the session that started this one); the calibration run waits on the answer. Steps 2 to 6 committed: efe18cb, 7497732, ed29ee0, 53dc663, 0211957.
- 2026-11-14: the verification run's judge round 1 showed that `powerops-prototype` section 5 ("a name such as \"Sample Person A\"") contradicted baseline S8 (plausible names of varied length) and the passed samples; the line now asks for a made-up first name and initial (commit after `f599fc2`). The plugin copies follow at the next goldens run.
- 2026-11-15: the person's answer to the gate 6 question, quoted in Q15; the calibration runs after the exemption is in `baseline.md` and `measure.py`.
- 2026-11-15: calibration round 1 (`calibration.md`, `cbf8921`) matched rows 1, 3 and 4 and missed rows 2 and 5 on confirmed faults in the passed samples; by Done line 3 it went to the person, whose answer is quoted in Q16.
- 2026-11-15: calibration round 2 (`67a6e08`) missed rows 2 and 5 again; the milestone stopped by step 7 and the person answered, quoted in Q17.
- 2026-11-15: calibration round 3 at `1f12d3f`: rows 1 and 4 matched; rows 2, 3 and 5 failed only on parts an earlier round passed and no fix touched, so by Q17's condition the key records them as `not at baseline` and judge consistency is a follow-up.
