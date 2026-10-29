plan-hash: b8b05a1ae15342c2656b1de7564278aa7fcf0d0211dfc4062311b5dfb80149db
model: claude-sonnet-5-5
fresh-context: yes

One fresh review (powerops-reviewer, Sonnet, in a context without the planning conversation), on the plan as first written (hash 4462fbe4c367cf1af7b19168be0d36a57eef059e8ed41c03fe0004612d5a53d7; the U-1 and F-6 rows gained detail while it ran). The plan-hash above is the body after the fixes below (the practice of the M7 to M32 reviews). The reviewer read source only and tried F-6's update lines in a throwaway tool outside the repo; it contacted no tenant. The coordinating session fixed the plan from the findings and wrote this file from the reviewer's report. The same reviewer then re-read the fixed plan (hash f8e80f8e11cdbdfc8dd4eb8e50f8018e4dba3bbcff42bd96273de0d861bf3855) against the source: R1 to R8 resolved, no new High or Medium, four new Low (R19 to R22), fixed below.

Most of the Power Platform checklist does not apply: there is no solution, publisher, connector, environment variable, licence question, DLP policy, route, managed state, volume or security role. Testability and open questions apply. Open questions: 0. No URL, GUID, email or person's name in the plan folder.

## Findings

### High

None.

### Medium

#### R1. `diff` scans folders too
- Area: U-2, Q2
- Finding: DiffCommand.cs:32-54 resolves a folder through `SolutionDiscovery.Find`, and :58 refuses only against the paths named, so `diff dir new.zip --out dir/x.zip` over the zip found in `dir` is allowed.
- Resolution: fixed (Q2 and the U-2 row include `diff`, with the resolved solution passed)

#### R2. The found files are known only after the refusal runs
- Area: U-2, ordering
- Finding: `deploy plan` refuses before `Prepare`, which lists the zips and then reads the target; `analyze` and `test` refuse before discovery.
- Resolution: fixed (each command lists its files offline first; `deploy plan` adds its refusals to `problems` after `ListZips` and before `read`; a test checks zero target reads)

#### R3. docs/COMMANDS.md:32 would contradict the code
- Area: U-2, docs
- Resolution: fixed (the U-2 row rewrites it, keeping the "report into the folder" sentence)

#### R4. X-1 must not feed notes into the exit-3 fallback
- Area: X-1
- Finding: the generic `error: the command failed` line is added when no `error:` line was kept; notes in the same list would suppress it.
- Resolution: fixed (a second list; the fallback looks at `error:` lines only; the JSON block stays first; a test for `Result(3, "", "note: x")`)

#### R5. Q3 read the tenant first and compared afterwards
- Area: S-2, Q3
- Resolution: fixed (the skill asks for the environment and whether `pac` is signed in to it before the read; the reviewer reads facts only when its parent passes the user's confirmation)

#### R6. The fast path, other skill lines and docs/PLANNING-SKILLS.md
- Area: S-2
- Resolution: fixed (the fast path never runs `plan facts`; SKILL.md :36-43, :157 and review-plan :32, and PLANNING-SKILLS.md :166-167, :177, :204 are in the row)

#### R7. `WorkflowTests` covers a fixed list of workflow files
- Area: F-1
- Resolution: fixed (`pin-drift.yml` is added to `Workflows` and meets its rules)

#### R8. Pin extraction and drift logic unspecified and untested
- Area: F-1
- Resolution: fixed (files and patterns named, `fetch-depth: 0`, a pin not on HEAD's history warns, the logic in `.github/scripts/pin-drift.sh` tested in temp git repos)

### Low

#### R9. F-1's path list is narrow
- Resolution: fixed (`skills`, `global.json` and `Directory.Build.props` added; the repo has no `Directory.Packages.props` or `nuget.config`)

#### R10. Required checks and SHA pinning
- Resolution: fixed (a job name unlike the six required checks, every `uses:` at a full SHA, the warning also in the job summary)

#### R11. S-1's sites were open-ended
- Resolution: fixed (the sites are named, and a source scan fails on an unclean `error:` hole)

#### R12. X-2 follow-through
- Resolution: fixed (schema text and enum, COMMANDS.md, `Import()` returns the kind, the existing assertions, a golden)

#### R13. X-3: where `summary.sh` is found
- Resolution: fixed (from the script's own folder)

#### R14. F-2 needs both paths special-cased
- Resolution: fixed (the parse-error path and `RunGate`; the existing theory asserts the new text)

#### R15. F-3 and F-4 test updates
- Resolution: fixed (OutputHygieneCommandTests.cs:85; a `Program.Run([])` test)

#### R16. F-6: the update route works
- Resolution: fixed (uninstall then install, in its own paragraph; the implementer re-runs it before documenting)

#### R17. F-5 detail
- Resolution: fixed (the comments and docs/examples/README.md:88 reworded; the exact pins named; `ExampleWorkflowTests` keeps one ref)

#### R18. `deploy apply --out`, and no BUILD-PLAN row
- Resolution: fixed (Q2 includes `deploy apply --out`; the fix PR adds an M33 section with a row and build-order items 58 and 59)

#### R19. The U-1 snippet called `mktemp -d` twice
- Resolution: fixed (one variable for the clone, the pack and the install, checked by the docs test)

#### R20. `Prepare` is shared with the apply dry run
- Resolution: fixed (the new refusals run on the plan path only)

#### R21. `analyze` walks the folder twice
- Resolution: fixed (pass the discovery result into the analysis if small; otherwise the second walk is accepted)

#### R22. The skills smoke run is not named
- Resolution: accepted (Assumptions: the weekly smoke run is the check, and its turn limit is raised if a full-plan scenario runs out)
