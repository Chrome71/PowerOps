plan-hash: dee7dc1166f202d7a96c9a9a3c1028e8afad2e1e5943f2675168d088d53293f2
model: claude-sonnet-5-5
fresh-context: yes

Environment facts were not read: `powerops plan facts` was not run, because nobody confirmed that `pac` is signed in (the header says `environment: null` anyway). No `pac` or `az` command was run and nothing reached a tenant. The plan is `approved`, `review` was `pending`, and it has 0 open questions. The repo was read at commit 6c7355d: the Data rows' files, the two skills, the pinned tests, the docs and the schema, and the design source (section 3, entry M77, of the 1.0 fix plan).

Departures from the fix plan's M77 entry, all declared in `decisions.md` except the last two: sign-off prints every report warning, not only the stale review; the scan also matches the lower-case and numbered forms; section 10 "New version" is carved out for amendments; implement section 3 step 1 resumes after an amendment; a step that cannot be done as written is added as a second trigger of the hand-off; the Won't build list adds three items (no amendment-line check in `plan status`, no reading of the header's environment, no smoke run here). The last two are consistent with the entry. No other departure found.

## Findings

### High

#### R1. Row 6's scan makes five committed plan files fail `plan status`
- Area: Testability (checklist 9); Data row 6
- Finding: Row 6 matches a capitalized word, then apostrophe-s, then `environment` after "anything", with no word boundary stated at the end. Run against `docs/plans` today (case-sensitive form of the row's rule), it hits five committed files that pass `plan status` now: `m11-ephemeral-env/plan.md` and `m11-ephemeral-env/review.md` (the possessive of "PR" followed by `environments`), `m32-platforms/plan.md` (the possessive of "Codex"), `m52-authority-boundary/review.md` (the possessive of "Action") and `m57-recovery-docs/review.md` (the possessive of "Code"). Once the row lands, each of them reports a violation, and a plan folder that is signed-off or done would stop passing. The plan has no step to find or fix them and no test that scans the committed plans. The assumptions admit false matches ("cleared with the allow marker") but do not count them.
- Suggested fix: End the pattern with a word boundary after `environment`, so the plural is quiet. Then decide the remaining hits: either add the allow marker to those three lines (a step in Steps, and a test that `plan status` is clean over every folder in `docs/plans`), or narrow the lower-case form so a single capitalized word is not enough (for example two capitalized words, or the numbered form only). Say in Data row 6 which choice is made, and record the count of committed hits in Assumptions.
- Resolution: fixed: row 6 now gives a case-sensitive regex with an end word boundary whose lower-case form applies only on an `environment:` line; it hits none of the 59 committed plans (the three product possessives are quiet theory cases), and Done when 7 runs the built `plan status .` over the whole repo

### Medium

#### R2. The plan skill is at its word ceiling with no stated cuts
- Area: Size and constraints (checklist 11); Data rows 2 and 7, Constraints
- Finding: `wc -w skills/powerops-plan/SKILL.md` gives 2783, and `Plan_skill_stays_under_its_word_ceiling` allows 2783, so there is zero headroom. Rows 2 and 7 add an Amendment paragraph, a stale-review clause and two counts in sections 7 and 9, and a longer section 3 step 7 and section 8 step 3. Constraints say the new text is "paid for by shorter wording in the same skill", but no row names which text is cut, and many sentences are pinned by `SkillsCommandTests` (the ledger, product-reference and sign-off tests). An implementer will hit the ceiling late and either trim a pinned phrase or raise the ceiling, which the plan forbids.
- Suggested fix: Name the cuts in row 2 and row 7 (which sentences shrink, by about how many words), or put the Amendment procedure in a file under `skills/powerops-plan/references/` that the core skill points to, as the data rules already are, and keep only a short pinned pointer sentence in the core text. State the budget (words added, words removed) in the row.
- Resolution: fixed: row 2 names the cuts; a draft of rows 2 and 7 ends at 2,780 words with no pinned phrase cut, and the ceiling (the person's M41 answer) stays

#### R3. Row 5 does not say the no-state rule is for `review.md` only
- Area: Data row 5
- Finding: The code at `PlanStatusReport.cs:29` is one line for every report. Row 5's heading says "the text line of `review.md`", but "when its `State` is null" can be implemented for all three reports. `verify.md` and `security-solution.md` are the kept roles' reports and always write a `state:`; a missing one is a defect that the current `state null` makes visible, and the code comment says null "is printed as it is". The fix plan asks only for `review.md` without a state.
- Suggested fix: Write in row 5 that only the `review.md` line drops `state`, and add a test case that a `verify.md` with no `state:` line still prints `state null`.
- Resolution: fixed: row 5 and Done when 3 say only `review.md` drops the state; a test keeps `state null` for a `verify.md` without one

#### R4. The step 6 hand-off still leaves the resume condition and the status path ambiguous
- Area: Testability (checklist 9); Data rows 1 and 2, Done-when 1
- Finding: Three gaps for an agent that follows the text. (a) "Resume only on the new `signed-off-hash`" needs the old value, but nothing tells the agent to read and keep it before handing over, and Assumptions admit `plan status --json` does not print the hash. (b) After the amendment `plan sign-off` sets `status: signed-off`, but implement section 3 step 1 skips the "read the commit messages" branch unless the status already says `implementing`; row 1's "as for `implementing`" needs wording that keys on committed steps, not on the status, or a fresh signed-off plan and an amended one are told apart by nothing. (c) Step 6 keeps its opening sentence "Never edit the plan body", while the new path has the same session edit a line through `powerops-plan`; the text should say that the plan skill edits it, not the build. The pinned test for that opening sentence stays, so the wording must be added, not swapped.
- Suggested fix: In row 1, add "note the header's `signed-off-hash` before handing over"; word step 1 as "when any step is already committed (after an amendment or a resumed build), read the commit messages and `writes.md` and don't redo a committed step"; add "the edit is made through `powerops-plan` section 10, not by the build" to step 6. Pin each in the new test.
- Resolution: fixed: row 1 notes the hash before the hand-off and resumes when it differs, says the build still never edits the plan body (the plan skill does), and step 1 keys its resume on committed steps; each is pinned

#### R5. Row 7 cannot tell a role from an environment name
- Area: Data row 7, Done-when 5
- Finding: "A role (Dev, Test, Prod)" is a closed list in the skill text, but a plan may hold `Sandbox`, `UAT`, `Staging` or a real friendly name that equals `Dev`. The implement sentence also says "an older plan's name is used as written", so a name and a role are told apart only by guessing. In the fast path, section 8 step 3 now records "its role" when a request names a friendly name, but the text does not say how the agent gets the role (ask, or infer from the route question). Also `powerops-plan` section 7 step 7 and the untouched `powerops-review-plan` and `powerops-review-security` ask whether `pac` is signed in "to the plan's `environment:`", which is now a role; the person has to name the environment each time and the text does not say so.
- Suggested fix: In row 7 define the rule as "when the header is null or one of Dev, Test, Prod, ask; otherwise use it as written", and say that any other value is treated as a name, so a non-listed role still gets used as a target (state this limit). In section 8 step 3 say the role is the one the route question named. In section 7 step 7 say the confirmation names the environment with that role, as the person said it in this session.
- Resolution: fixed: row 7 makes the role list closed (Dev, Test, Prod, any case), any other value a name, the fast path's role `Dev` unless the request says another; the UAT limit and the `environment:` wording of section 7 step 7 and review-plan are in Assumptions

#### R10. The Amendment paragraph and step 6 each leave the fresh-review condition to the other
- Area: Testability (checklist 9); Data rows 1 and 2 (second pass)
- Finding: The draft Amendment paragraph says "review as that step says", pointing at implement section 3 step 6. Row 1's description of step 6 (stop, note the hash, hand to `powerops-plan` section 10, resume on a new hash, log last) has no sentence on a review, though Done when 1 says the hand-off "runs a fresh review for a change of scope, data, security or cost". So neither text states when a fresh `powerops-review-plan` is needed, and an agent that follows them can skip it. Row 2's test pins "fresh review for scope, data, security or cost" in the Amendment paragraph, but the draft paragraph does not contain those words, so the pin as written would fail or be weakened.
- Suggested fix: State the condition once, in the Amendment paragraph (for example "start a fresh `powerops-review-plan` when the change touches scope, data, security or cost"), drop "as that step says", and check the word budget again (the draft is 2,780 of 2,783, so three words are left).
- Resolution: fixed: row 1 puts the fresh-review rule in implement step 6 (no word ceiling there) and pins it; the plan skill keeps `review as that step says`, which row 2 now pins instead

#### R11. Row 7's fast-path rewording breaks an existing pinned test that no row lists
- Area: Testability (checklist 9); Data row 7 (second pass)
- Finding: `PlanFactsSkillTests.The_fast_path_takes_an_environment_the_request_names_instead_of_always_leaving_it_null` asserts the fast-path text contains "Leave `environment: null` unless the request names an environment" and "friendly name". The draft section 8 step 3 reads "unless the request names the target" and drops "friendly name", so that test fails. Row 7 lists only `SkillsCommandTests` pins and its Steps and test column do not mention `PlanFactsSkillTests`. The same test file holds the check that the fast path reads no facts, which the draft keeps.
- Suggested fix: Add `PlanFactsSkillTests` (that test) to row 7's Test column and change its two assertions to the new wording, or keep "names an environment" and "friendly name" in the sentence (the word budget has three words left, so changing the test is cheaper).
- Resolution: fixed: row 7 keeps "unless the request names an environment" (the same word count, 2,780 in all) and lists `PlanFactsSkillTests:66`, whose `friendly name` assert becomes `then record its role`

### Low

#### R6. Changed meaning of `openQuestions` and `environment` is not carried through the schema text or the reviewer
- Area: Unanswered areas and the open-question count (checklist 10); Data rows 4 and 7
- Finding: `docs/schemas/plan-status.schema.json:24` is updated, but the description of `environment` ("Target environment name") is not, though the header now holds a role. `powerops-review-plan` step 3 says to note `openQuestions`, and its reply reports an "open-question count", which now excludes the admin questions; the plan keeps that skill untouched because its check 10 counts both sections. That is right for the checklist, but the count in the reviewer's reply shrinks without a word.
- Suggested fix: Add the `environment` description to row 4's schema edit ("role or name, or null"). Log in `decisions.md` that the reviewer's reply count now means open questions only, and that this is accepted.
- Resolution: fixed: row 4 changes the schema's `environment` description; the review-plan reply count is accepted and logged in decisions.md

#### R7. Row 3 can print one warning twice
- Area: Data row 3
- Finding: `ReadScannable` adds a warning for a binary or oversized file. `ScanTenantData` and `ReadDecisions` already call it with the same list, and `ReadReports` would call it for each report with that list again, so one oversized `review.md` is named twice at sign-off. `plan status` has the same list sharing today, so the duplication is not new, but sign-off prints stderr to the person.
- Suggested fix: Add an item to the row 3 test: an oversized `review.md` is named once. If that is costly, accept it and say so.
- Resolution: accepted: `ReadScannable` adds a warning only when the list doesn't hold it yet, so an oversized `review.md` is named once already; no change

#### R8. Row 4 does not say how singular counts print
- Area: Data row 4
- Finding: The current line uses `Plural(n, "open question")`, which prints "1 open question". Row 4 writes the new text as `<n> open questions, <m> admin questions` and lists only expectations with 0. An implementer may hard-code the plural or add a second `Plural` call, and the existing `PlanCommandTests` case at `:63` expects "1 open question".
- Suggested fix: Say that both parts use `Plural`, so one is "1 admin question", and add a case with one admin question to the text test.
- Resolution: fixed: row 4 prints both counts through `Plural`, with a `1 admin question` test case

#### R9. Row 6 does not give the regex, so case and line-break behaviour is open
- Area: Data row 6
- Finding: The rule is described, not written. If it is built with `RegexOptions.IgnoreCase`, as the sibling `Sign-off` regexes are, the capital-letter condition is lost and every lower-case "the user's environment" fires; the quiet theory catches that only if it keeps the exact cases. Also a name split over two lines, a non-ASCII capital first letter (`[A-Z]` misses it), and a name followed by something else before `environment` ("Jane Doe's Dev environment") are missed. The plan states the first two classes only as an assumption about scripts with no capitals.
- Suggested fix: Write the regex in row 6 (case-sensitive, a Unicode upper-case letter class, a word boundary at the end, the optional ` (<n>)`), add a quiet case for the plural and for sentence-initial prose such as "Each maker's environment", and note the line-break and extra-word misses in Assumptions as known limits.
- Resolution: fixed: row 6 writes the regex (case-sensitive, end word boundary, optional ` (<n>)`), the quiet cases add the plural and sentence-initial prose, and the misses are in Assumptions

### Notes (no finding)

- Row 3: `ReadReports` is `private static` and called with a null hash at `PlanChecker.cs:288`; making it `internal` and passing the body hash from sign-off reaches both callers correctly.
- Row 4 reaches every reader of `OpenQuestions` found in `src`, the schema, the golden and the tests; no other consumer (action scripts, MCP, hooks) reads the field.
- The kept roles, `tests/eval` and `powerops-review-plan` are untouched by every row. The schema version stays 1 for an added field.
- Row 9 depends on a model failing a create and then asking; a weekly run may not reproduce it, which the plan accepts by not running it here.
