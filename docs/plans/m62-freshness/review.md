plan-hash: 748fc2a0db33bc75b496579a6781b888cc2520c7532bace3881088cc94961cae
model: claude-sonnet-5-5
fresh-context: yes

Environment facts were not read: the plan names no environment (`environment: null`) and changes this repo's own code, not a Power Platform solution. Checklist items 1 to 9 and 11 (solution boundaries, connection references, licences, DLP, route, managed state, delegation, security roles, tables and flows) do not apply and are not reported. Item 10 (open questions: 0) and the URL/GUID/email/name scan do apply; the plan and `decisions.md` hold none. `plan.md` was not edited (`review: pending` stays), because this request allowed no change outside `review.md`.

## Findings

### High

No findings.

### Medium

#### R1. `DeployCommandTests` is not in the "deploy apply" collection, so the row 4 tests have no safe way to set the current folder
- Area: tests (Data row 4)
- Finding: Row 4 says `DeployCommandTests` is "in the `deploy apply` collection, which runs alone, because it sets the current folder". It is not: `tests/PowerOps.Cli.Tests/DeployCommandTests.cs:11` is a plain `public class` with no `[Collection]`. The classes that carry `[Collection("deploy apply")]` are `DeployApplyCommandTests.cs:16`, `DeployGateCommandTests.cs:11`, `FolderCapCommandTests.cs:6` and `PacRunnerResolveTests.cs:3`, and `DeployApplyCommandTests.cs:1133` is where one sets `Environment.CurrentDirectory`. The new tests must put a `docs/plans` under the current folder (row 4 reads `"."`), so as written they either set the process-wide current folder from a class that runs in parallel with others (for example `WritePathsTests.cs:354,549` read it), or never see a `docs/plans`. Only the no-warning case holds today, by accident of the test bin folder having no `docs/plans`.
- Suggested fix: either give `ReadyHashes` its root as a parameter that `Run` fills with the current folder, and let the tests pass a temp folder (no cwd change, no collection), or say the new tests live in a class in the "deploy apply" collection and move them there. Correct the sentence in row 4.
- Resolution: Fixed (decisions Q12). The root is found from the deploy folder, so the tests build a temp root and change no current folder; row 4 says so.

#### R2. Existing tests that the change breaks are not all named
- Area: tests (Data rows 1, 3, 6)
- Finding: Besides the named additions, these existing assertions change. (a) `PlanCheckerTests.A_six_line_report_gives_its_input_and_solution_hash_and_review_md_gives_none` (`tests/PowerOps.Core.Tests/Plans/PlanCheckerTests.cs:222-232`) writes `input: solutions/demo` and `out/Demo.zip` with a valid `solution-hash:` and no solution on disk, and asserts `Assert.Empty(reports.Warnings)`; after row 1 it gets two "was not found" warnings and fails. (b) `PlanCommandTests.cs:159` and `PlanStatusReportTests.cs:50-55` assert `stale true`/`stale null` in the text, which becomes `stale (plan) ...` plus `, stale (solution) ...`. (c) `PlanStatusReportTests.Json_matches_golden` (`:11-30`) records `solutionHash` of 64 `c` characters for a missing `solutions/demo`; the plan's new folder needs a digest computed in the test, not written by hand, and the `trouble` plan's `Assert.Single(plans[2].Warnings)` at `:27` holds only if that folder exists. (d) `McpCommandTests.cs:266-283` gains a "not found" warning (harmless, nothing asserts warnings there) but its `solutionStale` assertion in row 6 should say it is null there. Done when item 4 names `PlanCommandTests` but no Data row gives it a test.
- Suggested fix: list (a) to (d) in rows 1, 3 and 6 and add a `PlanCommandTests` row. For (a), give the test a real folder or use a hash and input that make it a legacy report.
- Resolution: Fixed. Rows 1, 3 and 6 name each test that changes, row 8 is `PlanCommandTests`; (a) holds unchanged because not found no longer warns (Q11), and the golden's digest is computed in the test.

#### R3. A report whose solution is not committed warns on every clone and in CI
- Area: Data row 1, decisions Q3
- Finding: A report's `input:` is often a build output such as `out/Demo.zip` (the plan's own test and `PlanCheckerTests.cs:227` use one). Build folders are normally ignored by git, so on a fresh clone or in CI every such report gets `input '<input>' was not found, so the solution was not compared` on every run of `plan status`. Done when item 5 requires `powerops plan status .` clean. The warning cannot be cleared without committing the artefact, and a person who sees it on every run stops reading warnings (the same noise Q3 avoids for legacy reports).
- Suggested fix: make "not found" read `unknown` without a warning (the other three cases keep theirs), or keep the warning and say in `COMMANDS.md` that a report about an uncommitted artefact warns in every clone, and in Done when item 5 that the repo's own plans hold no such report.
- Resolution: Fixed (decisions Q11). Not found reads `unknown` with no warning; the other cases keep theirs.

#### R4. `deploy plan` looks for reports only under the current folder, so the warning is silent where the people are
- Area: Data row 4, decisions Q6 (does it reach them)
- Finding: The root is `"."`. The MCP server is started as `powerops mcp` (`plugins/claude/.mcp.json`) with whatever folder the client picked, and the tool takes an absolute deploy folder; a person who runs `deploy plan deploy` from `deploy/` or a subfolder gets no `docs/plans` and no output, which looks the same as "every zip is reviewed". The plan states this as an assumption but nothing tells the user the check did not run.
- Suggested fix: find the root from the deploy folder as well: the nearest ancestor of the deploy folder (else of the current folder) that holds `docs/plans`, using the plan's own `docs/plans` rule. If that is too much, document in `COMMANDS.md` and the tool description that the check runs only from the repository root.
- Resolution: Fixed (decisions Q12). The nearest folder at or above the deploy folder that holds `docs/plans`; silence only when there is none.

#### R5. Schema text still says `stale` says nothing about the solution
- Area: Data row 6
- Finding: Row 6 edits only `solutionHash`'s description. `docs/schemas/plan-status.schema.json:35` (the `stale` property) still ends "It says nothing about the solution", which stays true of `stale` but now misleads without a pointer to `solutionStale`; the `reports` description (`:28`) says a report's staleness is never a violation and should cover both fields.
- Suggested fix: add "see solutionStale for the solution" to the `stale` description and mention `solutionStale` in the `reports` description.
- Resolution: Fixed. Row 6 changes the `stale` and `reports` descriptions too.

### Low

#### R6. The warning is only in the job log, and never at the approval point
- Area: Data rows 4 and 5, decisions Q5
- Finding: In the Action the line is in the plan step's log, not in the PR comment or `GITHUB_STEP_SUMMARY` (both come from the Markdown), and `deploy apply` (the MCP form, the Action's apply job) never repeats it, though that is where the import is approved. A plain `warning:` line is not a GitHub annotation, so it does not show on the run summary page.
- Suggested fix: accept and say so in the docs, or print `::warning::` for the line in `action/plan.sh` (the zip name is limited to letters, digits, `.`, `_` and `-`, so it is safe in a workflow command).
- Resolution: Accepted as it is and documented (decisions, other findings): job log and MCP result, not the PR comment; `deploy apply` doesn't repeat it.

#### R7. Locator names can hit Windows device names and trimmed names
- Area: Data row 1, Windows vs Linux
- Finding: `IsLocator` (`PlanChecker.cs:258-265`) allows segments such as `CON`, `NUL.zip`, `aux`, `demo.` and `demo ` . Row 1 joins them under the root and calls `Manifest.Read`, whose `File.Exists`/`BoundedRead.ReadAllBytes` (`Manifest.cs:35-46`) open the path. On Windows a reserved device name can open a device, and a trailing dot or space names another file than on Linux. The report is repository content, which a fork can supply.
- Suggested fix: in the new walk, skip (null with the "could not be read" warning) a segment that is a reserved device name or ends in `.` or a space when `OperatingSystem.IsWindows()`; add a test for `CON.zip`.
- Resolution: Fixed (decisions Q13). On Windows the walk refuses reserved device names and names ending in `.` or a space before opening anything.

#### R8. The same solution is hashed twice, and `ReadyHashes(".")` can return one plan
- Area: Data rows 1 and 2
- Finding: `verify.md` and `security-solution.md` normally name the same `input:`; `Manifest.Read` of a folder reads up to 256 MB, and row 1 does it once per report on every `plan status` run (and every plan, in `plan status .`). Separately, `ReadyHashes` calls `Discover(root)`, which returns just `./plan.md` when the current folder holds a `plan.md` (`PlanChecker.cs:57-60`) instead of `docs/plans/*`.
- Suggested fix: cache the digest by `input` within one `Check` call; in `ReadyHashes` read `docs/plans` directly or note that a `plan.md` in the current folder makes it the only plan.
- Resolution: Fixed (decisions Q14). One read per distinct input in a `Check`; `ReadyHashes` uses the `docs/plans` enumeration without `Discover`'s single-plan shortcut.

#### R9. The cross-OS promise covers line endings only
- Area: Constraints, docs (Data row 7)
- Finding: Constraints say a report written on one OS "checks as fresh on the other". That holds for CRLF in text files. It does not hold for the case of `input:` (a path `Solutions/Demo` written on Windows is "not found" on Linux), for a rebuilt zip (new bytes), or for a folder with a file type that `Manifest` hashes as bytes.
- Suggested fix: narrow the sentence and add one line to the `plan status` text in `COMMANDS.md`.
- Resolution: Fixed. Constraints name what is evened out and what isn't; `docs/COMMANDS.md` says the case of `input:` must match on Linux.

#### R10. Build plan row and report are not in the steps
- Area: Steps
- Finding: Earlier milestones add a row to `docs/BUILD-PLAN.md` (for example M39 at `docs/BUILD-PLAN.md:498`) and keep a `report.md`; Steps 3 and 4 name neither.
- Suggested fix: add the BUILD-PLAN row to step 3 (docs only; no version or changelog edit).
- Resolution: Not done (decisions, other findings): M50 Q12 left BUILD-PLAN rows for M50 to M68 out, and parallel sessions would collide on that file.

#### R11. The link walk and the existence checks in `Compare` are outside the `try`
- Area: code, `PlanChecker.cs:231-246` (second pass)
- Finding: Only `Manifest.Read` is inside the `try`. The loop's `new FileInfo(path).LinkTarget` and the `File.Exists`/`Directory.Exists` calls are not. `IsLocator` lets `*`, `?`, `<`, `>`, `|` and `"` through, and a name that is too long or sits in a folder the user can't read is also possible. I did not run it, so whether `LinkTarget` throws for these on Windows or on a protected folder on Linux is unverified; if it does, `plan status` ends with exit 3 and no report for any plan, for text a fork put in `input:`. `RefuseLink` has the same shape, but it only sees paths the user passed.
- Suggested fix: move the walk and the existence test into the same `try` and give it the "could not be read" outcome, with one test (a name of 300 characters, or `a?b` on Windows).
- Resolution: Fixed (decisions Round 3). The walk and the existence test are inside the `try`; row 1 says so.

#### R12. `deploy plan` walks up without a limit, so a stray `docs/plans` above the repository is taken as its reports
- Area: code, `DeployCommand.cs` `WarnUnreviewed` (second pass), decisions Q12
- Finding: The loop goes from the deploy folder to the filesystem root and stops at the first folder holding `docs/plans`. A deploy folder outside any repository (for example `~/Downloads/exports` with a `~/docs/plans` folder) or a repository without plans inside a larger one gets that folder's reports, and every zip warns for a review nobody could have meant. The same walk makes `No_docs_plans_at_or_above_the_deploy_folder_prints_nothing` (`DeployReviewWarningTests.cs`) depend on the machine: it fails when any parent of the system temp folder holds `docs/plans`.
- Suggested fix: stop the walk at the first folder that holds `.git` (a folder or a file, as a worktree has) after testing that folder for `docs/plans`, and add a test with a `.git` between the deploy folder and a `docs/plans` above it. State the stop in Q12 and `COMMANDS.md`.
- Resolution: Fixed (decisions Q15). The walk stops after the folder holding `.git`; row 4 and Done when 3 say so, a test covers it, and the no-`docs/plans` test makes its own `.git`.

#### R13. Some new branches have no test
- Area: tests (second pass)
- Finding: Not covered: (a) Q14, one read per distinct `input` (the only two-report test uses a link, which never reaches `Manifest.Read`); (b) the `warning: docs/plans could not be read...` line of `WarnUnreviewed` (a `docs` or `docs/plans` link) and `ReadyHashes` throwing for a link; (c) `deploy plan --format json`/`markdown` keeping stdout identical with the warning on stderr (only the text format is compared); (d) the MCP `deploy_plan` result carrying the line as a second block (`McpCommand.Result` forwards it, but `McpCommandTests` has no case); (e) the Windows reserved names are tested only as a pure function on Linux, and the one end-to-end test is skipped there.
- Suggested fix: add (a) with a counting seam or by deleting the folder between the two reports (the second must still read the cached digest), (b) as a Linux-only link test, and (c) as one `InlineData` more. (d) and (e) can be accepted and noted.
- Resolution: Fixed in part (decisions Round 3): a test for the could-not-be-read line; the cache, the stdout of other formats, MCP forwarding and the Windows-only path are left as recorded there.

## Second pass: do the resolutions hold?

R1 to R9 hold in the revised plan and in the code. R10 holds as "not done": `docs/BUILD-PLAN.md` has no M50 to M58 row and M50 decisions Q12 is the cited reason. Checked against the code: R1 (new `DeployReviewWarningTests` builds a temp root, no current-folder change), R2 (the named tests are updated, the golden digest is computed in the test), R3 (not found gives no warning; test present), R4 (walk from the deploy folder; the `a/b/deploy` case is tested; see R12 for its limit), R5 (schema text), R6 (documented in `COMMANDS.md`), R7 (`Compare` refuses the names before any file is touched on Windows; see R13e for the test limit), R8 (cache and `PlansUnder` present; R13a for the missing test), R9 (Constraints and `COMMANDS.md`). The code matches the revised plan and the docs I compared; I could not run the tests, since `dotnet test` was running in the worktree.

## Third pass

R11 holds: `Compare` (`PlanChecker.cs:231-249`) has the link walk, the existence test and `Manifest.Read` inside one `try`. R12 holds: `WarnUnreviewed` tests each folder for `docs/plans` first and stops after the first folder that holds `.git` (a folder or a file, so a worktree is covered); `COMMANDS.md`, Q15 and row 4 say so. R13 holds as "fixed in part": the docs-link test and the `.git` tests exist; the cache, other-format stdout, MCP forwarding and Windows-only path stay untested, as recorded in Round 3, and I accept that. Run here: `DeployReviewWarningTests` 7 passed; `PlanCheckerTests` 109 passed, 1 skipped (Windows-only). No new findings.
