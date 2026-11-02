plan-hash: 577f4d8ed8943b75250e6b61a7e89b01f4bb929c26ec88e5d9ba9c1f3a73160d
model: claude-sonnet-5-5
fresh-context: yes

Environment facts were not read: the request carried no confirmation about `pac`, and `powerops plan facts` was not run. The plan has no environment and touches no tenant, so the facts would not have changed this review.

Read: `plan.md`, `decisions.md` and `m39-outline.md` of the plan folder, and the repo at `fb7e0cc`. The planning chat was not read. This review ran on claude-sonnet-5-5, not on the top model, which principle 6 of `docs/PLANNING-SKILLS.md` asks for; the user may want a second pass on the top model.

Repo statements checked against the code and found correct: the `tests/eval/` folder does not exist yet; the `skills/**` embed is at `PowerOps.Cli.csproj:33`; `SkillsCommandTests.cs:9` and `:212`; `PluginManifestTests.cs:132`; `SkillsCommand.cs:12` and `:60-73`; `PlanChecker.cs:144-155`; `skills/powerops-review-plan/SKILL.md:49-57`; `skills/powerops-plan/SKILL.md:148-153`, `:149`, `:150` and `:176-178`; `skills/powerops-implement/SKILL.md:50` and `:56-60`; `docs/PLANNING-SKILLS.md:122`, principle 6, the Review section and the Agent support table; `docs/HANDOFF.md:16`; the three-skill lines of `README.md` and `docs/USE-WITH-AGENTS.md`; both `plugin.json` descriptions; `docs/schemas/plan-status.schema.json`; the fixture folders `powerops-seed/v1`, `v2`, `settings` and `flowtests/seed`; `tests/smoke/skills-smoke.sh`; `Tools.Require("bash")`; the M37 section of `docs/BUILD-PLAN.md` with items up to 65; the 39-flaw and 72-run arithmetic; SE:01 to SE:10 in the Well-Architected Security checklist. `DocsDriftTests`, `InstallLineTests`, `ReportTests` and CI scan only `skills/`, `docs/` and `tests/fixtures`, so `tests/eval/` does not change them. `powerops plan status` discovers only `docs/plans/*/plan.md`, so the case plans under `tests/eval/` are not found by `powerops plan status .`. The external claims in `decisions.md` and `m39-outline.md` (papers, the Microsoft and power-cat-skills repos) were not checked, except the Well-Architected checklist.

## Findings

### High

No findings.

### Medium

#### R1. The two "clean" solution cases are not clean as copied
- Area: 9. Testability (and the false-alarm keep rule)
- Finding: Row 4 builds all six solution cases from `powerops-seed/v1` and `v2`, `tests/fixtures/settings` and `tests/fixtures/flowtests/seed`, and says only that flaws are planted by editing. Running the four baseline commands on those fixtures at HEAD shows that `v1` is clean, but `v2` is not. `analyze` gives PO005 (an unused connection reference) and PO006 (the flow hard-codes a SharePoint URL). `settings validate` of `v2` with the only settings file there (written for `v1`) reports a missing connection reference. `diff` of `v1` and `v2` lists two changes. So a clean case carries two warnings and a settings problem, a reviewer's true finding on it counts as a false alarm, and each flawed case carries real defects that the key does not list. Row 6 counts only keyed flaws, so nothing notices.
- Suggested fix: Say, per case, which seed defects are removed (clean cases) or keyed as flaws (flawed cases), and which settings file goes with `v2`. Add to row 6 a check that the four commands on each clean case give the recorded output (no findings), and that each flawed case's baseline output is stored with it.
- Resolution: fixed. Row 4 gives every solution case one clean base: `v1`, its valid settings file and the `active` flow test. `v2` isn't a base, and neither is the other flow test, which also fails on `v1`. Row 7 checks the clean cases.

#### R2. The "baseline doesn't show it" denominators are decided after the freeze
- Area: 9. Testability; 10. Unanswered areas
- Finding: The keep rules for the Security solution pass and the Verifier count only the solution flaws that the baseline does not show. The key (row 5) holds flaw id, case, pass, severity and a sentence, with no field for this. Step 4 freezes the key, and step 5 runs the baseline afterwards, so who decides what "shows" means happens after the freeze and after results are in sight. The word is also unclear: a `diff` line that lists a changed component does not flag it, a `test` pass says nothing, and an `analyze` warning on another line of the same file may or may not count.
- Suggested fix: Run the baseline on the cases in step 3, store its output with each case, add a `baseline: shown|not-shown` field to every solution flaw in `key.json`, and define "shown" as a finding, failure or problem line that names the flaw's component or value. Let row 6 check that the field is present on every solution flaw.
- Resolution: fixed. Row 5 adds the `baseline` field and defines shown. Step 3 runs the baseline before those fields are written, and the freeze covers it.

#### R3. The run script as described cannot support the transcript checks it relies on
- Area: 9. Testability
- Finding: Row 5 starts each session "as `tests/smoke/skills-smoke.sh` does", and the constraints search each transcript for the repo path and the key's folder to find reads outside the temp folder. The smoke script runs `claude -p --output-format json` and keeps only `session_id` and `result`, and for Codex only the last message (`-o`) and the thread id. Neither shows which files a session read or which commands it ran. As written, the leak check, the cost and time figures, and the check that a role wrote only its own report cannot be done. The plan also names no permission mode or tool allowlist; the smoke script allows only `Bash(powerops *)`, which would deny any other command a draft's own command list permits.
- Suggested fix: Specify the capture in row 5: for Claude Code a stream of tool calls with results (`--output-format stream-json --verbose`), for Codex the JSON event stream (`--json`), both saved with exit code, start and end time and the reported usage. State the permission mode and allowlist, and have row 6 check that each draft's command list fits inside it.
- Resolution: fixed. Row 6 saves the event stream, exit code, times and report, and names the permission mode and allowlist. Row 7 checks that the drafts' commands fit it.

#### R4. The 72 sessions inherit the user's configuration, and model and effort are not pinned
- Area: 5. Dev to Test to Prod route (here: how the runs are isolated); 8. Security roles
- Finding: A `claude -p` or `codex exec` session in a temp folder normally still loads the user-level instructions, hooks, MCP servers and installed skills. For this user that includes tenant-capable MCP servers, so "no step reads or writes a tenant" (Environments and route) is asserted, not enforced; a same-named user-level skill could also win over the project copy. Neither model nor effort is pinned: the smoke script sets Codex effort but no Claude model, so the 72 runs depend on the account default on the day. The shipped review skill runs on Claude Code in a fork on the `opus` alias (`SkillsCommand.cs:73`), while row 5 copies the source skill without those lines, so the Claude baseline is not the shipped configuration. The row 5 wording "unchanged and without the `ForClaude` lines" is also confusing: the source has none, and `powerops skills install` would add them.
- Suggested fix: Pin model and effort per client for all runs and record them in `report.md`. Start the sessions with no MCP servers and without user-level instructions, or record how each client was isolated. Say in Assumptions that the Claude baseline differs from the shipped fork on the top model, and reword row 5 to say it copies the source file as it is.
- Resolution: fixed. Row 6 pins model and effort and isolates both clients, with flags tried on 2026-11-01. Assumptions state the difference from the shipped fork.

#### R5. No pilot, no pre-freeze check of the cases, and the leak scan comes after the freeze
- Area: 9. Testability (a way to test before Prod)
- Finding: The key and skills are frozen by commit in step 4 before any run. The pilot is the 72 runs themselves, and the coordinating session reads the diff and runs its leak scan in step 10, after the runs and after the frozen commit exists. A run script bug, a draft skill that cannot start, a wrong command in a draft, or a leaked value in a case would then show only after quota is spent. The replacement rule covers infrastructure failures, not a defect in `run.sh` or a draft, and the plan does not say what happens then: refreezing voids the runs already made, and continuing scores a broken role as a poor one. Nobody other than the author checks that each keyed flaw is really present and that each clean case is really clean until after the runs.
- Suggested fix: Before the freeze commit: run one throwaway case per pass and client (outside the 18) end to end, including scoring; have someone other than the author check the cases against the key; and run the leak scan. State the rule for a defect found after the freeze: fix it, make a new freeze commit, and rerun every pass it affects.
- Resolution: fixed. Step 4 adds an 8-run pilot, a case check by a second agent and the leak scan before the freeze. Constraints give the rule for a defect after it.

#### R6. "Infrastructure failure" and "valid run" are not defined before the runs
- Area: 9. Testability; 10. Unanswered areas
- Finding: A failed run is replaced, but a valid poor result never is, and the line between them is left to judgement after the result is seen. Cases the plan does not settle: no report written, a report with a wrong header, a permission denial, a timeout, a run cut short by quota, a CLI error after some tool calls. There is no cap on replacements, no per-run time or turn limit, and no rule for what counts as zero flaws found versus no result. Each choice changes a role's denominator, and the results sit near a floor with one run per cell.
- Suggested fix: Add to Constraints, frozen with the key: infrastructure failure means a CLI, auth, rate-limit or network error before a report exists; a missing or malformed report is a role failure and scores zero; one replacement at most; a time and turn limit per run.
- Resolution: fixed. Constraints now pause on a quota or rate-limit failure and rerun the same run after the reset, without using a replacement, and a stop at the limit with retries in the stream is an infrastructure failure.

#### R7. Scoring rules leave room for bias at the floors
- Area: 9. Testability
- Finding: Three gaps. A "false High" is not defined: a High on a clean case that is true but not in the key (see R1) is a false alarm by the key and correct by the facts. The scorer is not stated to be blind to role and family, and both families' reviews are scored by agents of one of them. The coordinating session rechecks each match it counts, so a missed match, which lowers a role, is never rechecked.
- Suggested fix: Define a false High as a High on a clean case that the scorer cannot support from the case files, and record true unkeyed findings separately, not as flaws. Strip role and family labels before scoring. Recheck non-matches as well as matches, or recheck a fixed sample of both.
- Resolution: fixed. Constraints define a false High and keep supported findings apart. Step 8 uses two blind scorers, one from each family, and the coordinating session settles every difference, so misses get a second reading too.

### Low

#### R8. The floors are small counts and the flaws come from the reviewer's own checklist
- Area: 7. Data volume (sample size); 9. Testability
- Finding: With one run per case, pass and family, the 12 solution flaws are split between two passes (the split is not stated) and shrink further once baseline-shown flaws are removed, so 15% is about one flaw per client. The plan-pass floor compares against flaws the Plan reviewer missed, and the plan flaws are written from the Security list in row 1 plus the Plan reviewer's checklist, so a Security pass finding some flaws the Plan reviewer's checklist does not name is close to certain. The plan calls the result provisional, which is fair, but it does not say how small a denominator is too small.
- Suggested fix: State the floors as counts as well as percentages, say how the 12 solution flaws split by pass, require `report.md` to show every denominator, and treat a denominator below a stated minimum as inconclusive, as a zero one already is.
- Resolution: fixed in part. Row 4 splits the solution flaws, a pooled denominator below 5 is inconclusive, and `report.md` shows every count. That the plan-pass floor is a low bar is accepted in Assumptions.

#### R9. The seed solution is too small to exercise most Security solution points
- Area: 8. Security roles
- Finding: The seed is a table, an environment variable, a connection reference, a cloud flow and a canvas app (`tests/fixtures/NOTICE.md`). It has no security role, field security profile or Power Pages site, and the `Solution` model does not read such components. Data policies, app users and sharing are tenant settings that no export shows. So the 12 solution flaws will cluster on connection references, environment variables and flow content, and most of the points in row 1 will be recorded as not applicable or not verified in nearly every run.
- Suggested fix: List in `report.md` which of the row 1 points have at least one keyed flaw and which have none, and say that the keep decision says nothing about the uncovered points. Add one clean case where the right answer for tenant-side points is "not verified", to test that the role does not infer them from a clean export.
- Resolution: fixed in part. `report.md` lists which points had a keyed flaw, and Assumptions say the decision covers only those. No new case: the two clean solution cases already test this, and step 8 records any tenant claim drawn from an export.

#### R10. The drafts' command lists are not checked by any test
- Area: 9. Testability
- Finding: `DocsDriftTests` scans `skills/` only, so a wrong command or flag in `tests/eval/roles/skills/` passes row 6 and fails in the runs. `DocsDriftTests.UnknownCommands` and `UnknownFlags` are `internal static` and can be reused in the same test project. Other facts the drafts rely on, such as `settings validate` needing `--settings` and `test` needing `--tests` for a zip, are not checked either.
- Suggested fix: Add to row 6 a call to those two helpers on both drafts, so the commands and flags they name must exist.
- Resolution: fixed. Row 7 calls both helpers on the drafts.

#### R11. "Writes only its own report" is not checked in the runs
- Area: 8. Security roles
- Finding: The roles table says each draft writes only its own report and never opens `plan.md` for writing, but the plan says the command list is instruction, not enforcement, and nothing records what a run changed. The existing Plan reviewer legitimately edits one header line, which makes a change to `plan.md` by a new role hard to spot by eye.
- Suggested fix: Have `run.sh` hash the copied case before the session and list the changed files after it, and have the scorer record any file other than the role's own report (and `review: done` for the Plan reviewer).
- Resolution: fixed. Row 6 saves a `diff -rq`, and step 8 records stray writes.

#### R12. Row 6: bash gating and "test-first" cover less than they say
- Area: 9. Testability
- Finding: `Tools.Require("bash")` skips on Windows always, not only where bash is missing. Only the `run.sh --list` check needs bash; the key, case-count, `PlanDocument` and draft header checks do not, but the row reads as one test run with bash. The draft header and front matter checks also pass at once, because the drafts exist from step 2, so they are not test-first.
- Suggested fix: Split row 6 into a part that runs everywhere and a part that needs bash, and say which assertions are written before their subject exists.
- Resolution: fixed. Row 7 is two tests, and only the second needs bash. Step 3 says which checks come first.

#### R13. The `state` field is never scored, and the status gates are incomplete
- Area: 9. Testability
- Finding: The four states are part of both drafts, but no keep rule checks them, so a role could set `ready` beside an open High with no effect on the result. Row 1 says the plan pass needs `approved` and the solution pass needs `signed-off`, `implementing` or `done`, but not what a draft does when the status is wrong (the Plan reviewer writes nothing; `failed` and `dropped` are other choices). Row 2 names no status gate for the Verifier.
- Suggested fix: Score `state` against the case in the same pass (a clean case gives `ready`, a case with a High finding gives `blocked`), or drop it from the drafts. State the behaviour for a wrong status and the Verifier's gate.
- Resolution: fixed. Rows 1 and 2 give the wrong-status behaviour and the Verifier's gate, and step 8 records a wrong `state`. It stays outside the keep rules.

#### R14. No cost estimate or ceiling for 72 runs
- Area: 3. Premium connector and licence impact (here: who pays)
- Finding: The plan says the runs cost model quota and run by hand, and `report.md` records cost afterwards, but gives no estimate, no per-client ceiling and no point to stop at. The person pays from a subscription, and the cost depends on the model and effort that R4 leaves open.
- Suggested fix: Estimate tokens per run from the pilot (R5), state a ceiling per client, and say that the person decides whether to continue when the ceiling is reached.
- Resolution: fixed. Step 5 shows the pilot's estimate, and the person says go before the 72 runs. There is no fixed ceiling, because nothing gives a number yet.

#### R15. Commit hygiene and what the freeze covers
- Area: 5. Dev to Test to Prod route
- Finding: The working tree has untracked files outside this plan (`.mimosa/` and `newhash.json`), and steps 4 and 11 say "commit" without naming what to stage. The freeze names the key and the skills, but the cases, `run.sh` and the exact prompt for each pass and client decide the result as much. `run.sh` is said to put "a build of this commit" on `PATH` but does not record the commit or refuse a tree that differs from it.
- Suggested fix: Stage by path in steps 4 and 11. Say the freeze covers the cases, `run.sh` and the prompts (which must not claim a `pac` sign-in), and have `run.sh` record `git rev-parse HEAD` and stop on a dirty tree under `tests/eval/` and `src/`.
- Resolution: fixed. The steps commit by path, the freeze covers the cases, `run.sh` and its prompts, and row 6 records `HEAD` and refuses a changed tree.

#### R16. URLs in `decisions.md`
- Area: 10. Unanswered areas (the URL, GUID, email and name check)
- Finding: Four log lines of `decisions.md` hold public documentation and repository URLs (seven in all). Each line carries the `plan-status: allow` marker, so `powerops plan status` reports no violation. They are public sources, not tenant data. No GUID, email or person's name was found in `plan.md`, `decisions.md` or `m39-outline.md`.
- Suggested fix: Keep the marker on those lines, or cite the sources by title. No other change.
- Resolution: accepted. They are public sources on lines with the allow marker.

#### R17. Nobody is assigned to find an outside read, and the order has no place for the replacement
- Area: 9. Testability
- Finding: Assumptions say the scorer searches each transcript for the repo path and the key's folder, and Constraints replace a run whose transcript shows a read outside the temp folder. Step 8 lists what its two scorers record (a wrong `state`, stray writes, tenant claims) and leaves that search out; the scorers also see reports with role and family removed. Step 7 runs and step 8 scores, so a replacement has no step. A run that read the key counts as valid until someone notices.
- Suggested fix: Have `run.sh` search the saved event stream for the repo path and the key's and baseline's folder names right after each run and flag a hit, so the replacement happens inside step 7. Name the same search in step 8 as a second check.
- Resolution: fixed. Row 6 searches the event stream right after each run and flags a hit, and step 7 settles it.

#### R18. Details of `run.sh` that a building agent would get wrong
- Area: 9. Testability; 5. Dev to Test to Prod route (the run route)
- Finding: (a) The "differs from `HEAD`" check names `tests/eval/` and `src/`, but the baseline Plan reviewer is copied from `skills/powerops-review-plan/SKILL.md`, which is not checked, and a plain `git diff` misses new untracked files. (b) Done-when asks for the client versions, but the list of what the script saves has none. (c) Row 7's second test starts `run.sh --list` with the bash that `Tools.Bash` picks, which is the system bash 3.2 on the macOS runner, so that path must be bash 3.2 compatible, as the header of `tests/smoke/skills-smoke.sh` already requires; the row does not say so. (d) Codex's throwaway `HOME` holds a link to the person's sign-in file. If Codex refreshes its token by replacing the file, the link goes with the folder and the real file may keep a token that was rotated out; the plan does not check for that.
- Suggested fix: Add `skills/` to the check and use `git status --porcelain` for it; save each client's `--version`; state bash 3.2 for the `--list` path; after the pilot, confirm that the person's Codex sign-in still works and that the sign-in file is unchanged or still valid, and say what the script does when the link was replaced.
- Resolution: fixed. Row 6 uses `git status --porcelain` and adds `skills/`, saves the client's `--version`, and stays bash 3.2 compatible. It also moves a refreshed sign-in file back over the person's, instead of checking after the pilot.

#### R19. The key and the script use different pass names, and the key cannot say which point a flaw tests
- Area: 9. Testability
- Finding: Row 5's `pass` values are `plan`, `security-solution` and `verify`. Row 6's `<pass>` values are `review-plan`, `security-plan`, `security-solution` and `verify`. Nothing says that a `plan` flaw is scored for both `review-plan` and `security-plan`, and row 7's "every case and pass has its expected flaw count" can be read against either set. Done-when and the fix for R9 need `report.md` to list which of row 1's points had a keyed flaw, but a key entry has no field for the point; only the one-sentence text could hold it.
- Suggested fix: State the mapping in row 5. Add a `point` field to each key entry (one of row 1's points, an SE number, or "plan checklist"), and have row 7 check that it is present.
- Resolution: fixed. Row 5 maps `plan` flaws to both plan passes and adds `point`. Row 7 checks it.

#### R20. Step 8 does not say who strips role and family, or how
- Area: 9. Testability
- Finding: Step 8 hides role and family from the two scorers, but a report shows both in its file name, the `model:` line, the `state:` line, the id prefix (R, S or V) and, for the solution passes, the `input:` line. The baseline Plan reviewer's report has no `state:` line at all, which marks it out. No row builds the stripping, and nothing says who does it or checks that it removed everything.
- Suggested fix: Add to step 8 a small step that writes neutral copies (finding text with neutral ids, no header lines) and keeps the mapping with the coordinating session, or say that only the family is hidden and the role stays visible.
- Resolution: fixed. In step 8 the coordinating session writes neutral copies and keeps the mapping. It also records `state`, which the copies leave out.

#### R21. Row 7 does not tie the stored baseline or the case plans to the cases
- Area: 9. Testability
- Finding: Row 7 checks that each solution case has a baseline file, but runs the commands live only on the two clean cases. Step 4 lets a case be fixed after the baseline and the key were written, so a flawed case can end with a stale `baseline:` field. A baseline file made by running with absolute paths would also hold the maintainer's machine paths in a committed file. Nothing checks that each solution-case plan still has status `implementing` and a body that matches its sign-off hash; a later edit makes `plan status` report a violation, which the Security solution pass and the Verifier would raise on a clean case.
- Suggested fix: Make the baseline commands run from a fixed folder with relative paths, let the first test compare live output with each stored baseline file, and run `PlanChecker.Check` over the solution-case plans expecting no violation.
- Resolution: fixed. Row 5 runs the baseline with relative paths, and row 7 compares it with live output and runs `PlanChecker.Check` on the solution-case plans.

#### R22. `input: <zip file name>` has nothing to name in the cases
- Area: 9. Testability
- Finding: Row 3 gives a solution-pass report and `verify.md` a body line `input: <zip file name>`. The solution cases are unpacked folders (copies of the seed `v1`), and the plan makes no zip, so the line has no value in any of the 18 runs. The `<zip>.findings.json` branch of row 2 is never exercised either.
- Suggested fix: Write the line as `input: <solution path or zip file name>`, and list the findings-file branch in `report.md` as not exercised.
- Resolution: fixed. Row 3 takes a path or a zip name, and Assumptions list the unexercised branch.

#### R23. The two clients run in different harnesses, and the baseline model need not be the shipped one
- Area: 8. Security roles (the run's tool access); 9. Testability
- Finding: Claude sessions get a fixed tool list and a Bash allowlist. Codex sessions get a workspace-write shell with no allowlist that can read the whole disk, and row 7 can check the allowlist only for Claude. So "family" in the results means client, harness and model together. `EVAL_MODEL` is not required to be the tier the shipped review would use (the shipped Claude copy runs on the `opus` alias), so the Plan reviewer baseline may be weaker than the shipped one, and the Security gain would look larger than it is. The second-family advice mixes all three.
- Suggested fix: Say in Assumptions that "family" here means client with its harness and model. Require `EVAL_MODEL` to be the tier the shipped review would use, or say why not, and have `report.md` state it.
- Resolution: fixed. Constraints pin one model and effort per client, with Claude on the shipped tier. Assumptions say what a family means here.

#### R24. A fix after the last pilot run is frozen without a run
- Area: 9. Testability
- Finding: Step 4 runs the 8 pilot runs, then says to fix what breaks, check the cases and run the leak scan, and the last commit is the freeze. A change made after the last pilot run (to `run.sh`, a prompt or a draft) reaches the freeze with no run behind it, which is the case the pilot exists to catch.
- Suggested fix: Add to step 4 that after any change to `run.sh`, a prompt or a draft, the pilot cells it touches are rerun before the freeze commit.
- Resolution: fixed. Step 4 reruns the pilot runs a change touches.

## Checklist items judged not applicable here

1. Solution boundaries and publisher prefix: no solution and no publisher; the plan says so.
2. Connection references and environment variables: nothing is deployed; fake values are named and listed in `NOTICE.md`.
3. Premium connector and licence: no connector; the only cost is model quota (R14).
4. DLP fit: no connector is used by the work; the case solutions are static files. Nobody checked a policy, and none applies.
5. Dev to Test to Prod route: no tenant; the repo route is a local branch with no push (R15).
6. Managed or unmanaged: not applicable.
7. Data volume and delegation: read as sample size (R8).
8. Security roles: read as who may write what in a run (R4, R11) and what the seed can test (R9).
