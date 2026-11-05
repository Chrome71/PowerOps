# Decisions: m62-freshness

## Round 1

### Q1. Where does `plan status` look for the solution a report's `input:` names?
1. Under the repository root, the folder that holds `docs/plans/<name>/`, whatever the current folder (recommended)
2. Under the current folder
3. Under the path given to `plan status`
Answer: 1. The roles write `input:` relative to the repository root, and the plan folder's place fixes that root, so the answer doesn't change with where the command runs or which of its three path forms it gets. Option 2 gives another answer from a subfolder; option 3 has no root when it is given a plan folder or a `plan.md`. A plan folder outside `docs/plans` has no such root, so its reports read `unknown` with a warning instead of a guess.

### Q2. How do the JSON and the text show the new staleness?
1. A new `solutionStale` field, boolean or null, beside `stale`, which keeps its meaning; the text says `stale (plan)` where it said `stale`, and adds `stale (solution) <true|false|unknown>` (recommended)
2. Turn `stale` into a string such as `plan`, `solution`, `both` or `fresh`
Answer: 1. Adding a field keeps `schemaVersion` 1 (`docs/schemas/README.md`), and every reader of `stale` keeps working. Option 2 changes a field's type and meaning, which bumps the version for every consumer. The text isn't versioned, and naming both kinds in it is what makes them distinct. In the text, null prints `unknown`, the word M55 uses for a hash it can't trust.

### Q3. When is the solution not compared, and does that warn?
1. A report with no valid `solution-hash:` or no valid `input:` reads `unknown` with no new warning; a report with both valid whose solution can't be read (plan folder outside `docs/plans`, a link on the way, not found, not a folder or `.zip`, over a cap) reads `unknown` and warns why (recommended)
2. Warn for every `unknown`
Answer: 1. A legacy report already warns when its `input:` is free text (M55), and M55 made a malformed hash read `unknown` without a warning; a second warning on every run of an old plan says nothing new. A report that names a solution which can't be read is a new fact the person should see. No warning holds a full path. (Amended in Round 2, Q11: a solution that is not found reads `unknown` with no warning.)

### Q4. Settings and test-folder digests?
1. Not built: deferred until a milestone that may change the roles' header (recommended)
2. Add `settings-hash:` and `tests-hash:` lines to the header
Answer: 1. The improvement plan says "reads settings and test-folder digests from the header when present". The M55 header holds no such line, so there is nothing to read. Adding one changes the pinned text of `powerops-verify` and `powerops-review-security`, which reruns their eval and needs the person's decision (AGENTS.md); this milestone may not touch them. Deferred: a later milestone that changes the header adds the lines and the comparison together.

### Q5. How does `deploy plan` say a zip wasn't reviewed?
1. A `warning:` line on stderr for each such zip (recommended)
2. A plan item of a new kind, such as `not-reviewed`
3. A new JSON field and Markdown line
Answer: 1. The MCP server forwards `warning:` lines to the agent with the JSON (`McpCommand.Result`), the CLI shows them, and the Action logs them (row 5). Option 2 adds a value to the closed `kind` list, which bumps the `deploy-plan` schema version; `deploy apply` reads that file, so it becomes a breaking change for an S milestone. Option 3 puts a repository fact (which reports exist) into a plan file that otherwise holds only the zips and the target. The PR comment doesn't show the line; that is the cost of this option, recorded here.

### Q6. Where does `deploy plan` find the reports?
1. `docs/plans/*/` under the current folder; nothing is said when there is no `docs/plans` folder (recommended)
2. A new `--plans <folder>` option
3. Relative to the deploy folder
Answer: 1. The Action and the docs run `deploy plan` from the repository root, and the MCP server starts there. A repo that keeps no plans doesn't use the review roles, and a warning on every zip of every plan would only be noise for it. Option 2 is an option nobody asked for; option 3 guesses a layout. (Amended in Round 2, Q12: the root is found from the deploy folder, not the current folder.)

### Q7. Which report counts as having reviewed a zip?
1. Any `verify.md` or `security-solution.md` with `state: ready` whose `solution-hash:` is the zip's SHA-256, in any plan, stale (plan) or not (recommended)
2. Both roles, in the same plan
3. Only a report that isn't stale (plan)
Answer: 1. The hash names the exact bytes the role read, which is the fact the warning is about; whether the plan text changed since is `plan status`'s business. Option 2 needs a way to know which plan a zip belongs to, which nothing records. The line names the two files, so the reader knows what to look for.

### Q8. Which zips are checked?
1. Every zip in the folder, whatever its action (recommended)
2. Only the zips the plan installs or upgrades
Answer: 1. It is the improvement plan's wording, and a zip in the deploy folder is a candidate to ship: a `skip` zip is imported once its version is raised. The line is a warning, so a `skip` zip that warns costs one line.

### Q9. What about the HANDOFF route, where the roles read the unpacked folder and the import takes a managed zip?
1. The warning fires, and the docs say why and how to clear it: a role reviews the zip that ships (its `input:` is that zip, its `solution-hash:` the zip's SHA-256) (recommended)
2. Match a zip with a report by solution name and version
3. Unpack the zip and compare it with the report's folder
Answer: 1. A managed export and the unmanaged folder are different bytes, and nothing offline proves one came from the other, so saying they match would be the false equality this plan exists to stop. Option 2 matches a report about another build of the same version; option 3 needs `pac` or a reimplementation of it. The warning is true: no ready report read the zip about to be imported.

### Q10. Does the Action's plan step show the new lines?
1. Yes: `action/plan.sh` prints PowerOps' `warning:` lines as it prints its `error:` lines (recommended)
2. No
Answer: 1. Without it the Action's job log shows the line only in debug mode. The script hides other stderr because tool text forwarded from `pac`, `az` and the Web API can name the environment; PowerOps writes that text with a `  | ` prefix, so a `warning:` line is PowerOps' own, and the zip names in it are already in the plan.

## Round 2 (from the review)

### Q11. Does a solution that is not found warn? (R3)
1. No: it reads `unknown`, and the text line shows it (recommended)
2. Yes, on every run
Answer: 1. A report's `input:` may name a build output that git ignores, such as a zip in `out/`, so every clone and every CI run would warn about something nobody can fix there, and `plan status .` would never be clean. `stale (solution) unknown` on the report's line already says nothing was compared. A link on the way, a plan folder outside `docs/plans` and a solution that exists but can't be read still warn: each is a fact about the repo the person can act on.

### Q12. Where does `deploy plan` look for `docs/plans`? (R4, amends Q6)
1. In the nearest folder that holds `docs/plans`, starting at the deploy folder and going up through its parents; nothing is said when none is found (recommended)
2. The current folder only
Answer: 1. The MCP server starts in whatever folder the client picked and takes an absolute deploy folder, and a person may run the command from `deploy/`; the deploy folder is inside the repository in every route the docs show, so its nearest `docs/plans` is the repository's. With no `docs/plans` above the deploy folder the repository keeps no plans, so there is no review to match, and Q6's reason for silence holds. It also means no test changes the current folder (R1).

### Q13. Windows names in `input:` (R7)
1. On Windows, a name that is a reserved device name or ends in `.` or a space reads `unknown` with the could-not-be-read warning before anything is opened (recommended)
2. Leave it to `File.Exists`
Answer: 1. The report is repository content that a fork can supply, so `input:` is input at a trust boundary. On Windows such a name can open a device or name another file than on Linux. Linux keeps the names as they are, because a folder called `aux` is an ordinary folder there. The check is a name test, so its test runs on both systems.

### Q14. Reading a solution twice, and a `plan.md` in the root (R8)
1. Read each distinct `input:` once per plan, and give `ReadyHashes` the `docs/plans` enumeration without `Discover`'s single-plan shortcut (recommended)
2. Leave both
Answer: 1. Both roles usually name one solution, and a folder can be up to 256 MB, so a second read doubles the cost of every run for nothing; a dictionary for one `Check` is a few lines. `Discover` takes a `plan.md` in the folder it is given as the one plan, which is right for `plan status <folder>` but would hide every report from `deploy plan` in a repository with a `plan.md` at its root; splitting the enumeration out keeps `Discover` as it is.

### The other findings
- R1: row 4's tests no longer change the current folder (Q12), so they need no test collection.
- R2: rows 1, 3 and 6 name the existing tests that change, and row 8 gives `PlanCommandTests` its own row. (a) holds unchanged because a solution that is not found doesn't warn (Q11).
- R5: row 6 also changes the `stale` and `reports` descriptions.
- R6: accepted as it is, and said in `docs/COMMANDS.md`: the line is in the job log and the MCP result, not in the PR comment, and `deploy apply` doesn't repeat it. A `::warning::` annotation would put the Action's output format into a line the CLI prints for everyone.
- R9: the Constraints sentence now names what is evened out (line endings) and what isn't (the case of `input:`, a rebuilt zip), and `docs/COMMANDS.md` says the case must match on Linux.
- R10: not done. M50 left `docs/BUILD-PLAN.md` rows for M50 to M68 out (M50 decisions Q12), and the sessions building P1 in parallel would collide on that file; this folder is the milestone's record.

## Round 3 (from the second pass)

### Q15. How far up does `deploy plan` look? (R12, amends Q12)
1. To the repository's top folder: the walk stops after the first folder that holds `.git` (recommended)
2. To the file system's root
Answer: 1. A `docs/plans` above the repository, in a home folder for example, belongs to something else, and its reports would make every zip warn. A folder with no `.git` anywhere above it (a downloaded copy) still walks to the root, as before. The test that has no `docs/plans` above it now makes its own `.git`, so it doesn't depend on the machine.

### The other findings
- R11: the link walk and the existence test moved into the `try` around `Manifest.Read`, so a name the file system rejects reads `unknown` with the could-not-be-read warning instead of exiting `3`.
- R13: a test now covers the could-not-be-read line of `deploy plan` (a `docs` that is a link) and the `.git` stop. Not added: the single-read cache has no outcome a test can see apart from timing; the line is on stderr, which no `--format` writes to, and the MCP server's forwarding of `warning:` lines is `McpCommand.Result`'s, already tested; the reserved-name refusal runs on the Windows runners of CI, and its name test runs everywhere.

## Log

- 2026-11-05: plan started at the request of the session that started this one: build M62 of the live-test improvement plan
- read on 2026-11-05: AGENTS.md; the M62 and M55 sections of the improvement plan and D1 to D9 with the answers; the reviews' CX-6 and the R13 mentions; the M50 and M55 plan folders; `PlanChecker`, `PlanStatusReport`, `PlanCommand`, `Manifest`, `DeployCommand` (`Run`, `Prepare`), `McpCommand.Result` and the `plan_status` and `deploy_plan` tools; `action/plan.sh`; `docs/COMMANDS.md` (`manifest`, `deploy plan`, `plan status`); `docs/HANDOFF.md` steps 2 to 8; `docs/USE-WITH-AGENTS.md`; `docs/PLANNING-SKILLS.md`; `docs/schemas/plan-status.schema.json` and the schema README; `PlanCheckerTests`, `PlanStatusReportTests`, `PlanCommandTests`, `DeployCommandTests`, `McpCommandTests`, `ActionScriptTests`
- depth: quick: the improvement plan gives the design
- questions answered by the building session under the user's delegation of 2026-10-22 (autonomous milestone mode); D1 to D9 don't touch M62
- areas not asked: users, screens, solution and publisher; the change is to this repo, and the plan answers each or marks it not applicable
- approved 2026-11-05 by the building session under the user's delegation of 2026-10-22, for a fresh-context review
- fresh review (a general-purpose subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-11-05: 0 High, 5 Medium, 5 Low (R1 to R10); answered in `plan.md` (Done when 2 to 4, rows 1 to 4 and 6 to 8, Constraints) and here (Round 2)
- the code built by a Sonnet implementer subagent while the review ran, then changed for Round 2
- the same reviewer's second pass 2026-11-05, with the build in the tree: R1 to R10 hold; 3 Low new (R11 to R13), fixed in code, tests and `plan.md` (row 4, Done when 3) and recorded here (Round 3)
- the same reviewer's third pass 2026-11-05: R11 to R13 hold (R13 in part, as recorded), no new finding; `plan-hash` set to the revised plan. In all: 0 High, 5 Medium and 8 Low, none open
- signed off 2026-11-05 by the building session under the user's delegation of 2026-10-22, with no High or Medium finding open (`powerops plan sign-off`; `signed-off-hash` equals `review.md`'s `plan-hash`); status then set to done, header only, so the hash holds
