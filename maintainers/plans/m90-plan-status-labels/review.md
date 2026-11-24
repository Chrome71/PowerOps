plan-hash: 6654dd41a63de2d187d8b1ecaed0c25542bbd1286ced62b6eb9fe7854dfa9d83
model: claude-opus-5-5
fresh-context: yes

Round 2. The plan and `decisions.md` were revised after round 1 (R1 to R7) and the Fable plan review; then reworded once more after the Fable plan review's round 2 (Data row 3, the Goal, the first Won't build line and the first Assumptions line). This review is of the plan as it is now: each earlier finding is answered below, and R8 was raised on the intermediate version and is answered too. This context holds round 1 of this review, not the planning conversation.

Environment facts were not read: the plan's `environment:` is null and the tenant is offline, so `powerops plan facts` was not run.

Checklist items 1 to 8 (solution and publisher, connection references, licences, DLP, the environment route, managed state, data volume, security roles) do not apply: the plan changes only the offline text check of `plan status` in this repo, and its sections say so. Open questions: 0; questions for your admin: 0.

Tools checked: `dotnet`, `git` and `powerops` are on PATH (the installed `powerops` is the base commit `b065f23`, and Done when 6 now uses the branch's build); the `powerops-researcher` and `fable-reviewer` agents and the Sonnet model are available in this session; `reports/leakscan.sh` exists in the main checkout and its usage line takes `<base> <head>`, as Done when 6 calls it.

Verified: both regexes of Done when 1 were run in .NET 10 on every case of Data row 2 and every existing case of both theories. Each result matches what row 2 expects before and after the change (the two new quiet cases fire on `b065f23` and go quiet; the third quiet case and the seven fires cases pass on both). At `b065f23`, `plan status .` reports no `personal environment name` line anywhere in the repo. Not run: the test project, so row 2's count of 140 passed and 2 skipped is not checked.

## Findings

### High

No findings.

### Medium

#### R1. The stated rule and its known limit misdescribe what the regex checks
- Area: 9. Testability (the comment and docs the build writes)
- Finding: Round 1: the plan said the name must start with a capital letter, while the regex tests only the word before `'s`. Round 2: Done when 3 and Done when 4 now describe the word before `'s` (decision Q2), but Data row 3 still tells the build to add "in prose, the name must start with a capital letter" to `docs/COMMANDS.md`. That sentence is now wrong twice over: the exclusion form also catches names that start with no capital (a caseless script, a digit or a bracket before `'s`), and it contradicts Done when 4, so the executor of Step 3 gets two different sentences.
- Suggested fix: Replace row 3's quoted sentence with Done when 4's clause, "in prose, a lower-case word before `'s` is not read as a name".
- Resolution: closed (row 3 now quotes Done when 4's clause, "in prose, a lower-case word before `'s` is not read as a name")

### Low

#### R2. A capitalised product or proper-noun possessive before a capitalised UI word still blocks
- Area: 11. Size and scope (Won't build), 9. Testability
- Finding: Round 1: the class was named nowhere.
- Suggested fix: Name it under Won't build and in the comment.
- Resolution: closed (Won't build and Done when 3 name it, with the allow marker as the way out)

#### R3. Done when 5 ran the installed `powerops`, which cannot show the change
- Area: 9. Testability
- Finding: Round 1: the installed CLI is the base commit.
- Suggested fix: Run the branch's build.
- Resolution: closed (Done when 6 runs `dotnet run --project src/PowerOps.Cli`, the branch's build)

#### R4. Row 3's test did not check the new sentence
- Area: 9. Testability
- Finding: Round 1: `DocsDriftTests` does not read the wording.
- Suggested fix: Say who checks the sentence.
- Resolution: closed (row 3's Test column says nothing reads the wording and the work review reads the sentence)

#### R5. The chain documents are not in the repo
- Area: 12. Tools, 5. Route
- Finding: Round 1 had two parts. The leak scan part is fixed: Done when 6 gives the path, the main checkout it runs from and the call. The other part is not answered: Step 4's "chain's merge protocol", `chain-rules.md`, the trial-3 fix plan and record P3/E60 are still not in the repo, and neither the plan nor the Log says where they live, so a reader of the committed plan cannot check Step 4 or the Goal's source.
- Suggested fix: Add one line to Assumptions or the Log saying where the chain rules and the fix plan live, or that they are private notes kept out of Git.
- Resolution: closed by the building session, 2026-11-11: a Log line in `decisions.md` says the chain rules, the fix plan and the trial record are private notes kept out of Git, and where they live (leak scan part closed in round 2)

#### R6. decisions.md holds URLs
- Area: Tenant data (URLs, GUIDs, emails, names)
- Finding: Round 1: two scheme-less Learn page addresses in the Log.
- Suggested fix: Drop them, or keep them on purpose and say so.
- Resolution: closed (kept on purpose as public pages; accepted in the Log)

#### R7. The plan and decisions.md hold personal names used as test values
- Area: Tenant data (URLs, GUIDs, emails, names)
- Finding: Round 1: sample names as test values; Constraints says they are made up.
- Suggested fix: None needed if the names stay invented.
- Resolution: closed (recorded in the Log, no change)

#### R8. The Goal claims no name is lost, which the exclusion form does not deliver
- Area: 9. Testability (the plan's own claims)
- Finding: The Goal says "Every name caught today is still caught, in prose and on an `environment:` line". Run in .NET 10, three prose lines that fire on `b065f23` go quiet under the new regex: a lower-case name, a name ending in a lower-case particle and a name whose part after a hyphen is lower case (`see jane doe's Environment`, `see Ana van der berg's Environment`, `see Jane Doe-smith's Environment`). Done when 3 states this limit correctly, so the Goal sentence contradicts it. Two smaller lines are stale too: Won't build still calls the design "the capital-letter rule", and Assumptions calls "a lower-case display name" the known limit, where Done when 3 says the last word before `'s`. <!-- plan-status: allow -->
- Suggested fix: Change the Goal sentence to say that every name caught today is still caught unless the word before `'s` starts with a lower-case letter, and that such a name is still caught on an `environment:` line. Word the Won't build and Assumptions lines the way Done when 3 does.
- Resolution: closed (the Goal now names the exception, a prose name whose last word starts with a lower-case letter; Won't build and Assumptions use the word-before-`'s` rule. A residue too small for a finding: a hyphenated last word whose part after the hyphen is lower case also goes quiet, which "last word" covers only loosely)
