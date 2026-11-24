plan-hash: 692154058c3ed167e13fc1a4742f7819ae67d528554dff87085ee5a56b076711
model: claude-sonnet-5-5 (Sonnet 5.5)
fresh-context: yes

Environment facts were not read: the plan names no environment and the request did not confirm `pac`. Checklist items 1 to 4 (solution, connection references, licence, DLP), 6 (managed), 7 (delegation) and 8 (roles) do not apply: no tenant, no solution. Items 5 (route), 9 (testability), 10 (unanswered areas) and 11 (size) were applied. `plan.md` still says `review: pending`: the caller asked that no other file change, so the header line is left for the parent session.

## Findings

### High

#### R1. "Screens names anything" cannot be decided as written, and this plan fails its own rule
- Area: 9 Testability, 11 Size
- Finding: Data row 1 treats `## Screens` as empty only when it is empty or "only None". The repo's convention is "None." plus a reason. This plan's own Screens is "None. The prototype rules are for the plans...", and the eval plans use "None. Forms are unchanged...". Under the written rule, M70 itself names screens and would be refused at sign-off, as would every such plan. The text also has no defined shape: bullets, a `| Screen |` table, "Portal pages:" prose and a `### Theme` block all occur. Design.md puts the Theme block inside `Screens`, so a Theme alone would make a no-screen plan "name something". A plan with no `## Screens` heading is not covered either.
- Suggested fix: Define it in the Data table: a missing heading, an empty body, or a first non-blank line that starts with "None" means no prototype is needed. Say what happens to a `### Theme` block. Add tests for "None.", "None" with a reason, a missing heading, a table, bullets and a Theme-only body.
- Resolution: fixed: Goal line 1 defines "names nothing" (no heading, empty body, first non-blank line starting "None") and counts a Theme block alone; row 1 lists the six tests.

#### R2. `prototype: not needed (<reason>)` is an unconditional bypass
- Area: 9 Testability, 10 Open areas
- Finding: Goal line 1 accepts `not needed (<reason>)` with no condition and no mark of who answered. An agent can write it for a plan full of screens, and no check compares it with `Screens`. Q1 says only a plan with no screens logs `not needed`. The Data row tests "allowed on not needed" but has no "refused when Screens names something and an agent wrote it" case. The code-app `not needed (mock-data slice)` has the same problem: the checker cannot tell a code app from a canvas app.
- Suggested fix: Accept `not needed` only when Screens is empty or "None" (R1), or when the reason is a fixed token such as `mock-data slice` and the person answered. Otherwise require `approved` or a person's `skipped`. Add a refusal test for each.
- Resolution: fixed: `not needed` no longer passes a plan with screens; a code app logs a person's `skipped (mock-data slice)`; row 2 adds the refusal tests.

#### R3. The prototype file is read without the link, size and echo protections the folder scan has
- Area: 9 Testability (security of the check)
- Finding: Row 1 says nothing about how `mockups/prototype.html` is opened. `ReadScannable` returns null with a warning for a symbolic link, a binary file or one over 1 MB. The prototype check needs its own length test (40,960 bytes), and the file is under the 1 MB cap, so both caps apply. If the check calls `File.ReadAllText` on a link, it reads outside the plan folder, which AGENTS.md and `RefuseLink` forbid. "One problem per broken rule, with the line" may echo an HTML line containing a URL or an email. `ScanTenantData` states "the matched text is never included". Also, `ScanTenantData` flags any URL scheme and any email-like text in the same file, so the skill's fake data (a person's email) and an inline `<svg>` with its namespace attribute fail sign-off as tenant data. Plan row 3 does not warn the skill about either.
- Suggested fix: State that the check reads through `ReadScannable` or the same link refusal, treats a null read as a failing problem (in sign-off the `warnings` list is only printed, not an error), reports the rule name and line number only, and that the skill writes no `@`, `http`, GUID or `<svg>` with a namespace. Add a link test and an email-in-fake-data test.
- Resolution: fixed: row 1 reads through `ReadScannable`'s refusals and fails on a null read; Goal line 2 reports rule and line only; Goal line 3 keeps `@`, `http`, GUIDs and `<svg>` out; link and email tests in row 1.

### Medium

#### R4. The Log line is not parsed to a defined rule
- Area: 9 Testability
- Finding: The plan says "finds the Log's `prototype:` line" but gives no rule for: two or more lines (an old `not chosen` then a new `approved`), a line in a Q&A answer outside `## Log`, a path other than `mockups/prototype.html` (`../x.html`), `prototype: skipped` with an empty reason, or the M42 forms `skipped` and `not chosen` (still logged in drafts in progress). `AgentAnswers` is private and keeps its Log walk inline; reuse needs a refactor the Data table does not list. The skip mark is also ambiguous: Q-answers carry `(answered by: person)`; the check should require that exact mark, and treat a missing mark as agent, as the skills already do ("if you cannot tell, write `agent`").
- Suggested fix: Say: only `## Log` items; the last `prototype:` line wins; the approved path must equal `mockups/prototype.html`; `skipped` needs a non-empty reason and `(answered by: person)`; the old forms count as missing. Name the extracted helper in row 1, and add a test for two lines and for a line outside the Log.
- Resolution: fixed: Goal line 1 takes the last `prototype:` item under `## Log`, the exact path, a non-empty reason, the person's mark on a skip, and counts the old forms as missing; row 1 names the shared Log helper and the two tests.

#### R5. `data-screen` matching, the four states and "link off the page" are not defined
- Area: 9 Testability
- Finding: Screens is free text ("- Vehicle form: ...", table rows, "Portal pages:"), so "a `data-screen` name found in `## Screens`" needs a rule (case, punctuation, substring vs. whole word). A substring match passes a screen named "a". The `data-state` values are named only as "empty, loading, error, failed save" with no spelling (`failed-save`?). Four states are required on every screen, but route.md gives Power BI "one page, a box per visual" and design.md allows "none" for a state; a read-only report or Pages page has no failed save. "A link off the page" is not defined: `href="#x"` is fine, but `javascript:`, `mailto:`, `//host/x`, `<form action>`, `<meta http-equiv=refresh>`, `<base>`, `<object>`, `<embed>`, `srcset`, `<svg><use href>` and CSS `image-set(` are not on the written list, and `<link rel=icon>` is not a "style sheet link".
- Suggested fix: Pin each rule in the Data row: match is the screen heading text, case-insensitive, whole phrase; the state attribute values; a state block may say "none" (design.md); every `href` must start with `#`, and `src`, `srcset`, `action`, `<meta>`, `<base>`, `<link>`, `<object>`, `<embed>`, `<svg>` are refused. Add a quiet test for a state block that says "none".
- Resolution: fixed: Goal line 2 pins the whole-words match, the four `data-state` values, "none" states, `href` starting `#`, and the refused tags, attributes and CSS.

#### R6. Forbidden-content and "Sketch" checks are text matches with false positives and evasions, and no parser is named
- Area: 9 Testability
- Finding: "A visible 'Sketch' label" and "a `route:` header comment" need a rule: a "Sketch" inside an HTML comment or `display:none` is not visible, and the word in prose ("sketch" in a comment) passes a bare search. Matching `url(` or `on*=` as text flags fake data like "url(s)" and the word "onboarding=" in prose; a case-insensitive search for `<script` is easy, but `<SCRIPT`, a tag split across lines and an attribute without quotes are not stated. The plan also forbids "font" and "image" but `@font-face` with `local()` and `<picture>` are not named.
- Suggested fix: Say the checker removes comments first, then scans tags with a tolerant regex (case-insensitive, whitespace across lines), requires "Sketch" in body text outside a comment, and takes the `route:` line only from the first comment. State that a text-level check catches the honest case, as `AgentAnswers` does. Add one quiet and one firing test per rule.
- Resolution: fixed: Goal line 2 removes comments first, reads `route:` from the first comment, matches tags across spaces and lines, needs "Sketch" in body text, and states the checks catch the honest case.

#### R7. Row 5 leaves D undecided, and four tests and the smoke script pin text it changes
- Area: 9 Testability
- Finding: The Goal says the prototype is "not an option", but row 5 does not say whether the **D** menu entries (sections 6 and 8) stay, go, or become "preview early". `Plan_skills_carry_the_product_references_the_sketch_and_the_power_bi_branch` asserts `**D** sketch the screens first` exactly twice (and the last after `## 8. Fast path`), the three log strings, `Keep visual detail out of Screens and Steps`, and route.md's `at most 6 screens and 15 KB`. The plan names only "the log-line test" and a "route text" test. The word cap is workable (2,782 of 2,783, and section 11's three bullets and 85 words of file rules move to the new skill, so the net change is negative), but the plan should say the count test is the gate, and that the D test, the log test and the route test are rewritten together. In `skills-smoke.sh`, scenario 5 requires `prototype: not chosen` and a D option, scenario 6 sends "D" and greps `approved ... prototype.html`, and line 267 checks 15,360 bytes; row 9 names only "the D offer".
- Suggested fix: Decide D in row 5 (recommended: drop D, since the gate starts the skill at approval), list the four pinned assertions, and list the smoke edits: both scenarios, the byte limit, and the new `skipped (...)` / `approved` lines.
- Resolution: fixed: D kept as "sketch now" (Q10, answered by the person); rows 5, 6 and 9 list the pinned assertions and the smoke edits.

#### R8. The reversal of Phase 7's "no extra gate" is stated but its consequences for the fast path are not
- Area: 5 Route, 10 Open areas
- Finding: Phase 7 M42 offered the sketch "without an extra gate" and "no new question, so the fast path's 'ask nothing else' holds". M70 adds an approve-or-skip question at approval, and the fast path's **B** ("approve, skip the review and sign off now") makes approval and sign-off one answer. The fast path text (`SKILL.md` section 8) still says "Ask nothing else". The plan does not say the fast path's rule is changed, whether B waits for the sketch, or whether the prototype skill's wait delays the sign-off in the same turn. The same reading applies to eval: `tests/eval/roles/cases/plan-*` are `approved` plans with Screens, so `plan status` will now print prototype warnings for them. Eval is frozen but the review-plan role reads that output; say the roles are unaffected or note the rerun cost.
- Suggested fix: Add a Constraints line that names the fast-path change, and one for the eval plans (warnings only, no exit-code change, no rerun needed) or the rerun if it is needed.
- Resolution: fixed: two Constraints lines, one for the fast path's B and "Ask nothing else", one for the eval plans and the frozen roles.

#### R9. An agent's approval of a sketch, and a person's skip, are asymmetric without an answer behind them
- Area: 10 Open areas
- Finding: The Users section says an orchestrating agent "may approve a sketch, but only a person may skip one". No answer in decisions.md says this, and Q1 and Q2 talk about the person's reason. If an agent authored the sketch and an agent approves it, the gate is met with no person involved, which is the failure the gate is meant to stop. `AgentAnswers` warns only for sign-off and acceptance lines.
- Suggested fix: Ask the person (one question) or require `(answered by: person)` on the `approved` line as well, with `agent` marks reported as a warning in `plan status`.
- Resolution: fixed: asked as Q9; anyone may approve, and `plan status` warns on an approval without the person's mark (Goal line 1, row 2, Users).

#### R10. Edits to `plan.md` after the sketch is approved, and after review, can leave the sketch stale
- Area: 9 Testability
- Finding: The review runs after the sketch is made (section 7 step 6), and sign-off checks the file only for the rules in Goal line 2. The body hash does not cover `mockups/`, so a sketch edited, or a `Screens` list changed, after the approval line is not detected beyond "each `data-screen` name is in Screens". `powerops-review-plan` item 11 still checks "a screen that isn't in Screens" by hand, so the same rule is now checked twice with different strictness.
- Suggested fix: Say in Constraints that a mockup edit after the line is accepted (as Phase 7 stated), and edit item 11's sketch sentence or state that the review keeps only the modern-control half. If the review text changes, note that no frozen role is touched.
- Resolution: fixed: a Constraints line accepts mockup edits after approval and keeps review-plan item 11 as it is.

### Low

#### R11. Docs and test lists the Data table misses
- Area: 11 Size (scope)
- Finding: Besides rows 8 and 10: `README.md` line 120 says "ships five agent skills. Three... and two..." and must become six; `SkillsCommandTests` has `Skills` (five names), a test named "all five skills", and the fork `Theory`/`ForClaude` `InlineData` lists; `PluginManifestTests` has the skill list in its fork loop (lines 132 and 138); `docs/PLANNING-SKILLS.md` has a skills table (line 96) and a diagram, and says `powerops-plan` writes the mockups (the writer changes); `docs/USE-WITH-AGENTS.md` lists the skills; both plugin descriptions carry the skill list (and the Codex `longDescription` repeats it). `.claude-plugin/marketplace.json` and `.agents/plugins/marketplace.json` carry no skill list, so they need no change. `tests/golden/plan-status.json` has no Screens, so goldens do not change (only the plugin skill copies do). No JSON schema or exit code changes: `plan-status.schema.json` is not touched, since problems go into `warnings`.
- Suggested fix: Add these files and test names to rows 3, 8 and 10.
- Resolution: fixed: rows 3, 4, 8 and 10 list the README line, the test lists, PLANNING-SKILLS, USE-WITH-AGENTS, the plugin descriptions, and the unchanged marketplaces and goldens.

#### R12. A prototype of 8 screens is a lot of top-model output for a gate, and the stated limits have no evidence
- Area: 11 Size
- Finding: 8 screens and 40 KB are the person's answer (Q3), so this is not re-opened. But Phase 7 recorded 6 screens and 15 KB as "a limit drawn from two small samples". The plan does not say the bigger limit comes from a trial. The cost line says one run per plan with screens; four states times eight screens may reach the new cap.
- Suggested fix: Note in Assumptions that 40 KB is the person's choice and unmeasured, and that the smoke run records the size.
- Resolution: fixed: an Assumptions line says 40 KB is unmeasured and the smoke run records the size.

#### R13. `Won't build` contains one item that is a decision, not a cut
- Area: 11 Size
- Finding: "Rechecking plans already signed off" is a decision (Q5) and is also stated in Goal line 1. It repeats there without cost.
- Suggested fix: Keep it once.
- Resolution: fixed: the item is gone from Won't build; Goal line 1 keeps it.

#### R14. "Matches a phrase of the Screens text" lets a one-word screen name pass
- Area: 9 Testability
- Finding: Goal line 2 matches each `data-screen` value as whole words against "a phrase" of `Screens`. A value such as "list" or "form" matches almost any plan, so the check cannot catch a screen that is not in `Screens`. It also does not say how a table row or a bullet's "Name: text" is split into phrases.
- Suggested fix: Match against the screen's name as written (the text before the first colon or in the first table cell), or require the whole `data-screen` value to appear as one phrase, and add a fires case for a generic word.
- Resolution: accepted: real `Screens` text has no fixed name shape ("- Home and sign-in.", "- Capture invoice (canvas app ...)", nested bullets), so a stricter rule would refuse sound plans; the whole-words match catches the honest case (Goal line 2), and `powerops-review-plan` item 11 checks the screens by hand.

#### R15. Smoke scenario 5's simulated user has no answer for the new approve-or-skip question
- Area: 9 Testability
- Finding: Row 9 changes scenario 5's expected log lines, but its goal text ("choose B, never the sketch") is not updated. At approval the skill now asks the person to approve or skip, so the run stalls or answers at random, and the `skipped (` line needs the person's mark the simulated user must supply.
- Suggested fix: Add to row 9 the new goal text for scenarios 5 and 6: which of approve or skip the simulated user picks, with a reason, and how `max_turns` grows for the extra turn.
- Resolution: fixed: row 9 gives scenario 5 one more turn and a goal line to skip with a reason, so it tests the skip path and scenario 6 the approve path.
