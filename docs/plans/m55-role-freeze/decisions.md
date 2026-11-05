# Decisions: m55-role-freeze

## Round 1

### Q1. Where does the canonical manifest come from?
1. A new offline command, `powerops manifest`: one path prints a folder's inventory with a SHA-256 per file and a digest, or a zip's SHA-256; two folders print a file-by-file comparison (recommended)
2. A `--files` option on `diff`
3. `sha256sum` or `Get-FileHash`, run by the role
Answer: 1. The roles may run nothing but `powerops` commands, and the same command must work on Windows and Linux, which rules out option 3. Option 2 would put a file comparison into `diff`'s JSON schema and Markdown, which go to PR comments, for a need only the roles have. The two-path form compares in the tool, not in the model, because comparing two long lists of hashes by reading is the kind of error this milestone exists to stop. There is no JSON, MCP tool or zip entry listing: the roles read text, M62 will call `Manifest.Read` in Core, and a zip is compared by its SHA-256. (answered by: agent m55-role-freeze)

### Q2. How is a file hashed?
1. A file with no zero byte has each CRLF replaced by LF; any other file is hashed as its bytes (recommended)
2. Raw bytes for every file
Answer: 1. With `core.autocrlf`, one commit gives CRLF on a Windows checkout and LF on Linux, and Windows is the primary platform. Raw bytes would make a report written on one look changed on the other, which M62 would read as stale. The zero-byte test is Git's own text heuristic, close enough for XML, JSON and YAML, and a binary file (an `.msapp`) keeps its bytes. A zip is always hashed as its bytes, because its SHA-256 is what `deploy plan` records. (answered by: agent m55-role-freeze)

### Q3. Where do the new header lines go?
1. Six lines: the four as they are, then `input:` and `solution-hash:` (recommended)
2. `solution-hash:` second, after `plan-hash:`
Answer: 1. Every parser and reader that takes `state:` from the first four lines keeps working, and a legacy report reads as it did. `input:` moves up from under the header into it, because it is now parsed. (answered by: agent m55-role-freeze)

### Q4. What is a parsed locator?
1. The current solution's path relative to the repository root, with `/` between names: no `:`, no `\`, no leading `/`, no empty, `.` or `..` segment, at most 260 characters (recommended)
2. A typed value such as `folder:<path>` or `zip:<path>`
Answer: 1. The kind is in the path: a `.zip` name or a folder. A relative path keeps a user name out of a committed report, which an absolute path could carry. A value that breaks a rule reads as null and warns once, and a missing line reads as null without a warning, as a legacy report has none. (answered by: agent m55-role-freeze)

### Q5. What does `plan status` do with the two lines?
1. Reads and prints them, for the two solution reports only; `review.md` stays null; no staleness check (recommended)
2. Also compare `solution-hash:` with the solution on disk
Answer: 1. The improvement plan gives all staleness detection to M62, which needs the header first. A malformed hash reads `unknown` with no warning, so M62 decides how to treat it. (answered by: agent m55-role-freeze)

### Q6. What do the new cases look like?
1. Two cases, `solution-07` for the Verifier and `solution-08` for the Security reviewer, each with two flaws (the role's privileges, the app's role map) and the same root components in both versions; their previous solution is the seed plus the role and the app (recommended)
2. One case holding all four flaws
3. Change an existing case
Answer: 1. The brief asks for one new case per role. The old cases stay as they are, so the rerun's numbers on them compare with M38's and M39's. The seed has no role or app, so a case whose root components stay the same needs a previous solution that already holds both. `RoleEvalTests` is changed to allow that for these two cases only, and to check that their root components match. (answered by: agent m55-role-freeze)

### Q7. What does the rerun cost?
1. 32 runs (2 roles, 8 cases, 2 families), with a fixed cap of USD 30 at Claude's list price; Codex recorded in tokens (recommended)
Answer: 1. The figures are from M38's and M39's runs, plus 30% for the new command and re-reads, plus four replacements and the Sonnet scorer: about USD 22. The cap of USD 30 covers the Claude runs, their replacements and the Sonnet scorer, and it is below the USD 40 the person approved under D2, so no new approval is needed. The runs stop and ask if that spend reaches USD 30. (answered by: agent m55-role-freeze)

### Q8. Do the keep rules change?
1. No: M38's rules, recomputed on this rerun's runs; the new case results and every "unchanged" statement are recorded beside them, not as rules (recommended)
2. Add a keep rule for the new cases
Answer: 1. The rules were frozen in M38 and renewed in M39. A new rule on two cases would have a denominator of 2 per role, below M38's floor of 5 for a conclusion. The measures of this milestone's purpose are defined before the run and shown beside the rules: each role's result on its new case, each unchanged statement judged false, unsupported or supported against the case files and the run's own comparison, and the six header lines. A missed floor sends that role's text back to its M39 pin unless the person answers otherwise, and drops no role. The person sees the new results and decides. (answered by: agent m55-role-freeze)

### Q9. When do the plugin copies and the pins change?
1. After the person's decision, as the brief says; the freeze commit fails only the plugin-copy checks of the two changed skills, by design, and the Data edit is re-signed after a second review pass (recommended)
Answer: 1. A plugin copy is what ships, and AGENTS.md says the decision comes before shipping. The pinned ids go into this plan's Data section. That changes the plan's body, so the plan goes back to `approved`, the review runs a second pass, and the building session signs off again. Then `signed-off-hash` equals `review.md`'s `plan-hash`. (answered by: agent m55-role-freeze)

### Q10. Do the skills point at `COMMANDS.md#diff` or spell out the list?
1. Both: the sentence names the reference and lists what `diff` compares (recommended)
2. Only the reference
Answer: 1. A role in an evaluation run, or in a maker's repository without PowerOps's docs, can't open `docs/COMMANDS.md`. The list is taken from `COMMANDS.md#diff` as it stands at `431df9b`. (answered by: agent m55-role-freeze)

## Round 2: the fresh review (R1 to R14)

Each finding of `review.md` was answered in the plan and, where it is skill text, in the skills, before the freeze. (answered by: agent m55-role-freeze)

- R1: the Security skill's "The first four lines are exactly:" says six; row 6 names line 73.
- R2: paragraph B maps each kind to its files in the unpacked layout and counts a changed `Other/Customizations.xml` as a change to all of them.
- R3: paragraph B says a zip leaves the component not verified. The Verifier records the skip in `## Checks` (C2V), and the Security reviewer under the header lines, as it does for `diff`.
- R4: paragraph A adds classic workflows and business rules (name, category and state only), says "cloud flows", and says a part named in a `note:` line was not compared.
- R5: `solution-07` was reworked by its writer. Its plan's own change is now a column in an existing view, so `diff` prints only the version change. `solution-08` keeps compared changes, so the set has both shapes. Its settings file now sets the plan's new value, after the independent case check found it still set the old one.
- R6: row 9 has the right ranges, the per-case count override, the renamed not-shown test, and the baseline files as the proof that `analyze`, `test` and `settings validate` are clean on 07 and 08. The case writer was told the marker words.
- R7: Automation defines before the run how each unchanged statement is judged (false, unsupported, supported), how it is read, and what a missed floor does (that role's text goes back to its M39 pin unless the person answers otherwise, and no role is dropped).
- R8: Automation records the six header lines, the hash against the freeze build's digest, and whether `plan status` reads both values.
- R9: one number, USD 30, covering the Claude runs, their replacements and the Sonnet scorer, with a stop and a question at it.
- R10: row 3 says the command checks before `Read`, maps the exceptions, and cleans every path. The build does so.
- R11: the CLI tests scrub the repo root, so the goldens hold `powerops-seed/v1` and `v2`.
- R12: `COMMANDS.md#manifest` says to give the solution's own folder. The skills use the request's current solution, so their text doesn't change for it.
- R13: (a) Done when 2 says both forms; (b) row 4 cites `:69`; (c) row 4 and `COMMANDS.md` say a legacy free-text `input:` warns; (d) the golden has a `verify.md` with a hash and a locator; (e) step 3 says not to run the golden update until step 7.
- R14: Automation says a finding from the unkeyed role on 07 or 08 is unkeyed, neither a hit nor a false High.

## Log

- 2026-11-04: plan started at the request of the session that started this one: build M55 of the live-test improvement plan, with D2 as the person answered it (answered by: person, through that session's brief)
- read on 2026-11-04: AGENTS.md; the M55 section of the improvement plan and D2 with the answers; the M38 and M39 plan folders (`plan.md`, `report.md`); `tests/eval/roles/` (cases, `key.json`, baseline, `run.sh`, `NOTICE.md`); `RoleEvalTests`; `PlanChecker`, `PlanStatusReport`; the M50 and M54 plan folders; `docs/COMMANDS.md` (diff, plan status); `SolutionDiscovery`, `XmlUnpackReader`, `ReadBudget`, `BoundedRead`; both role skills, whose blob ids at `431df9b` match M39's pins
- depth: standard: the rerun is a quota spend and a decision for the person
- questions Q1 to Q10 answered by the building session under the user's delegation of 2026-10-22 (autonomous milestone mode); D2 by the person
- areas not asked: users, screens, solution and publisher; the change is to this repo, and the plan answers each or marks it not applicable
- approved 2026-11-04 by the building session under the user's delegation of 2026-10-22, for a fresh-context review (answered by: agent m55-role-freeze)
- fresh review (a general-purpose subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-11-04 at `e588815`: 0 High, 9 Medium, 5 Low (R1 to R14); answered in Round 2
- the code of rows 1 to 4 built by an implementer subagent (Sonnet) in its own worktree from `431df9b`, brought in as one commit; the building session then dropped a locator's trailing `/` and added two docs lines (R12, R13c)
- the cases of row 8 written by a subagent (Sonnet) that read neither skill nor this plan; checked by another (Sonnet) with the same limits, which confirmed F43 to F46 and found one stray value in `solution-08`'s settings file, fixed by the writer
- the same reviewer's second pass 2026-11-04 with the build in the tree, at plan body `71c00e0c`: R1 to R14 hold in the plan, the skills, the code, the docs, the tests and the cases; it ran each test project and the CLI on scratch folders and on both new cases; 2 Low new (R15, R16), 0 High, 0 Medium
- R15 fixed with a test-only guard before the freeze; R16 accepted, Low and not a security finding, with the reason in `review.md` (answered by: agent m55-role-freeze)
- signed off 2026-11-04 by the building session, outside the skills, under the user's delegation of 2026-10-22, with no High or Medium finding open (`powerops plan sign-off`; `signed-off-hash` equals `review.md`'s `plan-hash`, `71c00e0c`) (answered by: agent m55-role-freeze)
- frozen 2026-11-04 at `850db28`: a fresh clone built with 0 warnings, kept format clean and failed only the two `PluginManifestTests` the plan names; leak scan 0 hits
- run 2026-11-04 (2026-11-05 01:36 to 02:20 UTC): 32 runs, both roles on solution-01 to solution-08 on Claude (`opus`) and Codex (`gpt-6-astra`), effort high, through `run.sh` from a clean clone of the freeze; all valid, none replaced
- scored blind by a Sonnet subagent and by Codex in a read-only sandbox: 63 of 64 pairs agreed, the one difference settled by the building session; tenant claims and unchanged statements settled by the building session; results and keep rules in `report.md`
- 2026-11-04: the person kept both roles' new text, the Verifier's and the Security reviewer's, after reading the results and the keep rules in chat (answered by: person)
- row 10 with keep: the plugin copies through "Update goldens", the blob ids of the evaluated text in Data, `AGENTS.md`'s pointer to this plan, `docs/PLANNING-SKILLS.md` six lines; the Data edit changed the plan body, so status went back to `approved` for a second fresh-context pass
- third pass of the same review (a general-purpose subagent, Sonnet, fresh context) 2026-11-04 at `c4e22f6`, plan body `b57a808f`: row 10 holds, the shipped text equals the evaluated blobs, 0 new findings
- signed off again 2026-11-04 by the building session, outside the skills, under the user's delegation of 2026-10-22, with no High or Medium finding open (`powerops plan sign-off`; `signed-off-hash` equals `review.md`'s `plan-hash`, `b57a808f`) (answered by: agent m55-role-freeze)
