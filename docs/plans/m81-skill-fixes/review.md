plan-hash: 14825ced1ebbba8893ae7612257ab72d5072346c43b6da4426c6d6005086be31
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: the request did not say the user confirmed a `pac` sign-in, so `powerops plan facts` and `pac` were not run. The plan sets `environment: null` and changes only this repo's skill text, tests, smoke script and docs.

Checklist items 1 to 8 (solution and publisher, connection references and environment variables, premium licences, DLP, Dev to Test to Prod route, managed or unmanaged, delegation, security roles) are not applicable: the plan builds no Power Platform artefact and runs no tenant read or write. Item 11 (size) is not applicable beyond the `Won't build` list, which is present. Item 10: `openQuestions` is 0, `Questions for your admin` is empty, and `decisions.md` logs why the five areas were not asked. The review checked the proposed skill sentences against the source milestone (section 3, "M81", of the 1.0 fix plan), the current skills on `f67f61c`, `docs/PLANNING-SKILLS.md`, `SkillsCommandTests` and `tests/smoke/skills-smoke.sh`.

## Findings

### High

#### R1. The quoted text breaks the plan skill's new word ceiling
- Area: 9. Testability (rows 4, 5 and 7; Done-when 2)
- Finding: Row 7 raises `Plan_skill_stays_under_its_word_ceiling` by 50, from 2,783 to 2,833, and the skill has 2,780 words today. Counted the way the test counts (a split on whitespace, as `wc -w`): row 4 replaces a 20-word sentence with 30 words (+10), and row 5 adds a 45-word bullet (+45), so the skill would have 2,835 words and the test fails. The plan quotes both texts word for word, so the build can't pass row 7 without changing a plan line (an amendment under `powerops-implement` rule 6). The planned comment ("plus 50 for the callable-tool rule") also leaves out row 4's 10 words.
- Suggested fix: Before sign-off, either raise the ceiling by 55 or more (for example by 60, to 2,843, with a comment such as "M81: plus 60 for the engagement rule and the callable-tool rule"), or shorten rows 4 and 5 until they add 50 words at most, and give the count in row 7.
- Resolution: open

### Medium

#### R2. The probe drops the list of five sign-ins the old text named
- Area: 9. Testability, and completeness against the old text (row 1)
- Finding: The sentence row 1 replaces names five sign-ins that don't cover each other: Dataverse, `pac`, `az`, FlowAgent's connection management and Canvas Authoring. The new text says "each sign-in the plan's routes need" and gives `pac`, `az`, Canvas Authoring and Dataverse as examples, but not FlowAgent. The agent must now work out which sign-ins a route needs. FlowAgent's connection management, which a cloud-flow route needs, is the one the agent is most likely to miss, and the agent would then find it only when a connection fails. Row 7's test can pin the sentences, but nothing pins which sign-ins are probed.
- Suggested fix: Keep the list in the new text, for example: "Then probe each of these sign-ins that the plan's routes need: Dataverse, `pac`, `az`, FlowAgent's connection management and Canvas Authoring. Use a read-only status call and nothing else: ...". Add the list to row 7's pinned wording.
- Resolution: open

#### R3. Row 1 doesn't say what happens after "ask the person to sign in, and wait"
- Area: 9. Testability (rows 1 and 2)
- Finding: Row 1 doesn't say whether the agent probes again once the person says they have signed in. Row 2 then tells section 4 step 2 to "use what the sign-in probe found about `pac`". After a wait, that result is out of date unless the probe runs again. Also, `pac` is optional: the skill's `compatibility` line says so, and section 4 step 2 has a fallback to the Web API's `ExportSolution`. A plain "wait" leaves the person no stated way to go on without `pac`, so a build could stall on a sign-in it doesn't need. The source milestone says only "a missing one asks the person and waits", so the plan has to decide this.
- Suggested fix: Say both in row 1, for example: "For a missing one, ask the person to sign in or to go on without it, and wait. Probe it again once they say it is done." Make row 2's wording point to the latest probe, and pin both in row 7.
- Resolution: open

#### R4. It is unclear how the reviewer applies checklist item 12
- Area: 9. Testability (row 6)
- Finding: Item 12 asks the reviewer whether each tool is "callable here". The reviewer runs in a forked context or a subagent, and in `tests/eval/roles/run.sh` with a narrow tool list, so its "here" may not be the session that plans or builds. A tool that isn't installed where the review runs would get a false Medium, and a tool installed only there would hide a gap. Item 12 also needs `command -v` or `Get-Command`. The skill's section 1 ("Read only these: ... the repo ...") doesn't list those commands, and the item doesn't say what to do when the reviewer can't run them. "A step that rests on a tool that is neither" doesn't cover the case where the reviewer couldn't check.
- Suggested fix: Make item 12 check the plan rather than the reviewer's own session: each tool a step names is either covered by an Assumptions line (not callable, or not checked) or treated as callable by the plan. A Medium goes to a named tool the plan doesn't mention, plus one the reviewer finds missing with `command -v` or `Get-Command`, when it can run them. Otherwise it writes "not checked" for that tool. If the reviewer should run those commands, add them to section 1's list.
- Resolution: open

#### R5. Smoke scenario 10 passes with or without the fix
- Area: 9. Testability (row 8)
- Finding: The request in scenario 10 names `contoso-forms` itself. The fast path (plan skill section 8 step 1) already says "Put every guess under Assumptions", so the draft may mention `contoso-forms` in Assumptions (for example "the contoso-forms CLI is installed") whether row 5 exists or not. The check "the plan's `## Assumptions` section names `contoso-forms`" can't tell those cases apart. Also, the fast path doesn't write `Steps` by itself: section 7 step 3 drafts them at approval, and the fast path's B skips that step. The row-5 rule is tied to `Steps`, so it may not run before the first reply that the scenario checks.
- Suggested fix: Make the check look for an Assumptions line that names `contoso-forms` and says it wasn't found or isn't callable (for example a case-insensitive match on `contoso-forms` and `not` with `callable|found|installed|PATH` on the same line). Either check after approval, or have row 5 also cover the tools that the fast path's draft names. Write in the row that the scenario was not run before the fix.
- Resolution: open

### Low

#### R6. PLANNING-SKILLS still gives the old word ceiling
- Area: 10. Unanswered areas (row 9)
- Finding: `docs/PLANNING-SKILLS.md`, in "The ledger", says "(a test holds it at 2,783 words)". Row 7 changes that number, and row 9 doesn't update the sentence.
- Suggested fix: Add to row 9: update that sentence to the new ceiling.
- Resolution: open

#### R7. Row 3's exception contradicts the sentence before it
- Area: 10. Unanswered areas (row 3)
- Finding: The PLANNING-SKILLS "Environment facts" paragraph says plain `pac` output carries tenant ids, emails and every environment name, "and the agent should never see those". Row 3 adds an exception to the paragraph's last sentence only. The plan's own Assumptions note that `pac auth list` puts the account and environment in the agent's context. As planned, the paragraph would claim the agent never sees what the probe shows it.
- Suggested fix: Reword that sentence too, for example: "the agent should never repeat or write those; only `powerops-implement`'s sign-in probe, `pac auth list`, shows them to the agent, which never repeats them".
- Resolution: open

#### R8. Section 4 still says the Security reviewer hears of a `pac` sign-in only from the user
- Area: Consistency with the rest of the skill (rows 1 and 2)
- Finding: In `powerops-implement` section 4, the bullet on the reviewers' request says "Tell the Security reviewer that `pac` is signed in only if the user said so in this session." After row 1, the agent learns about `pac` from the probe, not from the user, and the plan doesn't say whether this line stays. Keeping it is probably right: the frozen role reads facts only on the user's confirmation that `pac` is signed in to the plan's environment, which a probe doesn't establish. An executor who doesn't know that might "fix" the line.
- Suggested fix: Say in row 2 that this line stays as it is, and why: a probe shows that a profile exists, not that the user confirmed it targets the plan's environment, and the reviewer roles are frozen.
- Resolution: open

#### R9. An "available" `pac` doesn't say which environment an export reaches
- Area: 5. Route (rows 1 and 2)
- Finding: `pac auth list` shows that a profile exists, but its active profile can point at another environment. Row 2 lets section 4 step 2 export with `pac` on the strength of the probe. Row 1 says "A probe never picks the target" but doesn't say that the export must name the person's environment. The old text had the same gap, and now the agent has more reason to rely on `pac`.
- Suggested fix: In row 2, add that the export targets the environment the person named in section 3 step 3, never the active profile's environment, or that the agent asks the person which profile to use when that isn't certain.
- Resolution: open

#### R10. The assumption about smoke scenarios 7 and 8 misreads them
- Area: 9. Testability (Assumptions)
- Finding: Assumptions says that in scenarios 7 and 8 "the probe reports `pac` missing". Both scenarios' plans say "No Microsoft skill, pac, az or Python is used", so a probe of "each sign-in the plan's routes need" should run nothing. In Claude, `allowed_tools` there doesn't allow `pac` either. A run that probes `pac` would show the probe isn't limited to the routes, which is a failure, not an expected extra turn.
- Suggested fix: Correct the assumption. Optionally, have the fake `pac` log its calls and make scenarios 7 and 8 fail when it was called, which tests that the probe is limited to the routes.
- Resolution: open

#### R11. decisions.md contains URLs
- Area: Personal and tenant data (the URL rule)
- Finding: The researcher's `## Log` line in `decisions.md` contains two URLs, written without a scheme (`<url>`, `<url>`). Both are public Microsoft documentation pages, with no tenant value, but the checklist reports any URL.
- Suggested fix: Keep them if public documentation sources are acceptable in a committed record, as in other plans' Sources lines, and accept this finding. Otherwise cite the page titles only.
- Resolution: open
