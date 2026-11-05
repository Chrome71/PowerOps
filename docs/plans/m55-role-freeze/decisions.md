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
Answer: 1. The figures are from M38's and M39's runs, plus 30% for the new command and re-reads, plus four replacements and the Sonnet scorer: about USD 22. The cap is below the USD 40 the person approved under D2, so no new approval is needed. The runs stop and ask if the Claude spend reaches USD 40. (answered by: agent m55-role-freeze)

### Q8. Do the keep rules change?
1. No: M38's rules, recomputed on this rerun's runs; the new case results and every "unchanged" statement are recorded beside them, not as rules (recommended)
2. Add a keep rule for the new cases
Answer: 1. The rules were frozen in M38 and renewed in M39. A new rule on two cases would have a denominator of 2 per role, below M38's floor of 5 for a conclusion. The person sees the new results and decides. (answered by: agent m55-role-freeze)

### Q9. When do the plugin copies and the pins change?
1. After the person's decision, as the brief says; the freeze commit fails only the plugin-copy checks of the two changed skills, by design, and the Data edit is re-signed after a second review pass (recommended)
Answer: 1. A plugin copy is what ships, and AGENTS.md says the decision comes before shipping. The pinned ids go into this plan's Data section. That changes the plan's body, so the plan goes back to `approved`, the review runs a second pass, and the building session signs off again. Then `signed-off-hash` equals `review.md`'s `plan-hash`. (answered by: agent m55-role-freeze)

### Q10. Do the skills point at `COMMANDS.md#diff` or spell out the list?
1. Both: the sentence names the reference and lists what `diff` compares (recommended)
2. Only the reference
Answer: 1. A role in an evaluation run, or in a maker's repository without PowerOps's docs, can't open `docs/COMMANDS.md`. The list is taken from `COMMANDS.md#diff` as it stands at `431df9b`. (answered by: agent m55-role-freeze)

## Log

- 2026-11-04: plan started at the request of the session that started this one: build M55 of the live-test improvement plan, with D2 as the person answered it (answered by: person, through that session's brief)
- read on 2026-11-04: AGENTS.md; the M55 section of the improvement plan and D2 with the answers; the M38 and M39 plan folders (`plan.md`, `report.md`); `tests/eval/roles/` (cases, `key.json`, baseline, `run.sh`, `NOTICE.md`); `RoleEvalTests`; `PlanChecker`, `PlanStatusReport`; the M50 and M54 plan folders; `docs/COMMANDS.md` (diff, plan status); `SolutionDiscovery`, `XmlUnpackReader`, `ReadBudget`, `BoundedRead`; both role skills, whose blob ids at `431df9b` match M39's pins
- depth: standard: the rerun is a quota spend and a decision for the person
- questions Q1 to Q10 answered by the building session under the user's delegation of 2026-10-22 (autonomous milestone mode); D2 by the person
- areas not asked: users, screens, solution and publisher; the change is to this repo, and the plan answers each or marks it not applicable
- approved 2026-11-04 by the building session under the user's delegation of 2026-10-22, for a fresh-context review (answered by: agent m55-role-freeze)
