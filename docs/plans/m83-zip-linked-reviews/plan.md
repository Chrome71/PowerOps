---
status: draft
review: pending
environment: null
depth: standard
supersedes: null
---

# M83: The kept roles review the managed zip's unpacked form, bound to the zip with checked provenance

## Goal

Close the gap that M75 left open (finding F39 of the PowerOps 1.0 trial, 2026-11-08; fix plan section 3 "M83", amendments A3 and F-3, question Q1). After M75 a report covers a zip only when it records the zip's SHA-256 as its `solution-hash:`, which a review of an unpacked folder never does, and a review of the zip itself can't read the roles, tables, forms or views inside it. M83 lets the two kept roles review the folder that `pac solution unpack` makes from the managed zip that will be deployed, bind the report to that zip by its SHA-256, and have `deploy plan` and `--require-reviews` check that the reviewed folder really is that zip's unpack instead of taking the report's word for it.

Done when:

1. Both kept roles (`powerops-verify`, `powerops-review-security`) take the managed zip that will be deployed when the request names it, review its unpacked folder as the current solution, and start their reports with seven header lines, the seventh `deploy-hash:`, the digest that `powerops manifest <zip>` prints, else `unknown`.
2. `plan status` reads `deploy-hash:` from the first seven lines of `verify.md` and `security-solution.md` into `PlanReportFile.DeployHash` (64 lowercase hex, else `unknown`; null for `review.md`). Its text and JSON output don't change.
3. `deploy plan`, `deploy apply --require-reviews` (so the MCP tool `deploy_apply`, `deploy gate` and the Action) count a report as linked to an imported zip when its `solution-hash:` is the zip's SHA-256 (M75's link, unchanged), or when its `deploy-hash:` is the zip's SHA-256 and its `solution-hash:` equals the digest of a fresh `pac solution unpack` of that zip into a temp folder. An unpack that can't run, fails or gives another digest leaves the report unlinked: `deploy plan` warns, and `--require-reviews` refuses before any import. `deploy plan` prints one `note:` naming the plan folder for each zip it links this way.
4. `skills/powerops-implement/SKILL.md` (section 4, after step 3), `docs/HANDOFF.md` steps 7 and 8, `docs/COMMANDS.md`, `docs/USE-WITH-AGENTS.md` and `docs/PLANNING-SKILLS.md` describe the route and the seven lines.
5. The M55 procedure ran: both `SKILL.md` edits and the `tests/eval/roles/run.sh` edit in one freeze commit; both roles on `solution-01` to `solution-08` on Claude Code and Codex from a clean clone of it; scored blind; the keep rules recomputed in `report.md` in this folder.
6. If every keep rule holds for both roles and both families: the keep decision is logged in `decisions.md` as renewed on the person's instruction of 2026-11-09; the evaluated blob ids are pinned in this plan's Data; `AGENTS.md` and `RoleEvalTests` point at this plan; the plugin copies are regenerated. If any rule fails: no pin and no merge, and the person decides.
7. `dotnet build` (0 warnings), `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` (no violation) and the leak scan (0 hits) are clean, and the PR is green on all eight checks.

### Won't build

- PowerOps unpacking or reading a zip's component files itself (rejected in the fix plan): `pac` does the unpack, as a subprocess, and `Manifest.Read` hashes the folder it leaves.
- Showing `deploy-hash` in `plan status` text or JSON, or any staleness check of it there. `deploy plan` is where a zip exists to compare with.
- A second unpack per check to prove byte-stability (decisions Q1).
- Dropping M75's link (a report whose `solution-hash:` is the zip's SHA-256): it stays a cover (decisions Q3).
- Any change to the eval cases, `key.json`, the baselines, the plan cases or the pilot; any change to `powerops-review-plan`.
- A Windows or PowerShell variant of `run.sh`.

## Users

- The build agent and the person who run the two roles before a promotion, and need a review that covers what ships.
- MCP `deploy_apply`, `deploy gate` and the Action, which refuse an import no linked review covers.
- The trial's graders, who check that both reports carry the zip's `deploy-hash` and that `deploy plan` names the link.
- A contributor who reruns the role set after a later change.

## Data

Line numbers are those of `fca4c57`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | `PlanReportFile` gains `string? DeployHash = null` (after `SolutionStale`). `ReadReports` takes the head from the first seven trimmed lines, not six. For `verify.md` and `security-solution.md`, `DeployHash` is the `deploy-hash:` value when it is 64 lowercase hex characters, else `UnknownHash`; for `review.md` it stays null. Nothing else reads it here, and `PlanStatusReport` doesn't print it | `src/PowerOps.Core/Plans/PlanChecker.cs:26-27, 432-490` | `PlanCheckerTests`: a seven-line report reads its hash; a six-line report reads `unknown`; a malformed value reads `unknown`; `review.md` null; `input:` and `solution-hash:` still read from lines 5 and 6. `plan-status` goldens unchanged |
| 2 | `DeployReviews`. New `public sealed record ZipUnpack(string? Digest, string? Failure)` (one of the two is set). `NeedsUnpack(plans, sha256)`: true when some `verify.md` or `security-solution.md` records `sha256` as its `DeployHash`. `Problem(plans, sha256, ZipUnpack? unpack = null)` and a new `Cover(plans, sha256, unpack)` (the first covering plan folder, ordinal, or null) share one per-report rule. A report is linked when its `SolutionHash` is `sha256`, or when its `DeployHash` is `sha256` and `unpack.Digest` equals its `SolutionHash`. Its fault, in this order: `<file> is missing`; when neither hash is `sha256`, `<file> records another deploy-hash and solution-hash`; when its `DeployHash` is `sha256` but `unpack` is null or has no digest, `<file> records this zip as its deploy-hash, but the zip's unpack could not be checked: <failure>`; when the digest differs, `<file>'s solution-hash is not the digest of this zip's unpack, so it reviewed another folder`; then state and plan hash as today. `Recorded` (and so `Evidence`) also counts a report whose `DeployHash` is `sha256`. The sentence for a zip no report records becomes `no verify.md or security-solution.md under docs/plans records this zip's SHA-256 as its deploy-hash or its solution-hash` | `src/PowerOps.Core/Deploy/DeployReviews.cs:17-48` | `Core.Tests/Deploy/DeployReviewsTests.cs`: linked by deploy-hash with a matching digest; each fault sentence, in order; M75's direct link still covers with no unpack; a `verify.md` linked by deploy-hash and a `security-solution.md` linked directly in one folder cover; reports of two folders still don't; `NeedsUnpack` true and false; `Recorded` counts a deploy-hash |
| 3 | `DeployCommand`. A private `Unpack(string zipPath, bool managed, pac)`: makes a folder with `Directory.CreateTempSubdirectory("powerops-unpack-")`, runs `pac` with the argument list `solution unpack --zipfile <zip full path> --folder <temp>/solution --packagetype Managed` (or `Unmanaged`, from the zip's own `Solution.Managed`) and a 120-second timeout, then returns `Manifest.Read(<temp>/solution).Digest`. A failure gives one of `pac was not found`, `pac solution unpack timed out`, `pac solution unpack exited <code>` or `the unpacked folder could not be read`; pac's output is never printed, because it holds full paths. The temp folder is deleted in `finally`; a delete that fails is left (a `shortcut:` comment). `WarnUnreviewed` gets `pac` and, for each imported zip with `NeedsUnpack`, unpacks it once and passes the result to `Problem`. With `evidence` (`deploy plan`), each zip that `Cover` links through a deploy-hash prints `note: <zip>: docs/plans/<name>: both reports record this zip's SHA-256 as their deploy-hash, and the digest of its pac solution unpack is their solution-hash`. `pac` is passed to `Run`, `DryRun` and `Gate`, as `Apply` already has it. The `--require-reviews` description and the MCP `deploy_apply` description say "record that zip's SHA-256 as their solution-hash, or as their deploy-hash with a solution-hash that equals the digest of the zip's pac solution unpack" | `src/PowerOps.Cli/DeployCommand.cs:31-52, 64, 91-133, 136-176, 282-357, 569-700`; `src/PowerOps.Cli/McpCommand.cs:258`; `tests/golden/mcp-tools.txt` | `Cli.Tests/DeployReviewWarningTests.cs` and `DeployApplyCommandTests.cs`, with `FakePac` taught `solution unpack` (it records the call and copies a given folder into `--folder`): linked through deploy-hash, `deploy plan` prints the note and no warning, and `--require-reviews` passes with the import calls made; the unpack's arguments, with `Managed` for a managed zip and `Unmanaged` for another; a changed zip (deploy-hash not the zip's) warns and runs no unpack; a changed reviewed folder (the unpack's digest differs) and a security-only change (a `Roles/` file the unpack has and the reviewed folder doesn't, which `diff` wouldn't see) warn, and `--require-reviews` refuses with zero import calls; pac not found, a non-zero exit and a timeout each give their sentence and refuse; no report with a deploy-hash runs no unpack, so the M75 tests stand; the temp folder is gone after the run; `deploy gate` and MCP `deploy_apply` refuse the changed-folder case. One test runs the real `pac` on `tests/fixtures/zips/ColumnLevelSecuritySampleSolution_managed.zip`, with reports whose hashes come from its own `pac solution unpack`, and skips when `pac` is not on `PATH` (`Tools.Require("pac")`) |
| 4 | `skills/powerops-verify/SKILL.md` (frozen; exact text below). The request list gains R1V after "the current solution and the previous solution". The commands gain C3 after the two `manifest` lines. Verify step 1 says ``Run `analyze`, `test`, `diff`, `settings validate` and the `manifest` commands with the paths from the request.`` "The first six lines of `verify.md`" and "These are the first six lines" say seven, and the block gains H7 after `solution-hash:` | `skills/powerops-verify/SKILL.md:14-21, 30-37, 45, 55-66` | `RoleEvalTests`, and the rerun |
| 5 | `skills/powerops-review-security/SKILL.md` (frozen). Line 16's sentence ``This needs the current solution, and takes the settings file and the previous solution if the request names them.`` becomes R1S. The commands gain C3 after the two `manifest` lines. "The first six lines are exactly:" and "These are the first six lines" say seven, and the block gains H7 | `skills/powerops-review-security/SKILL.md:16, 24-30, 79-90` | as row 4 |
| 6 | `tests/eval/roles/run.sh` (frozen). For `security-solution` and `verify` only: after the case is copied, `zip` must be on `PATH` (exit 2 otherwise, like the other checks); `(cd "$work/solution/current" && zip -q -r -X "$work/deploy/current.zip" .)`; `meta.txt` gains `zip-digest: <hex>`, the digest that the run's own `powerops manifest "$work/deploy/current.zip"` prints. Both prompts gain ``Deployed zip: deploy/current.zip; the current solution is its unpacked folder.`` after the current solution. Before `diff -rq` the script removes `deploy/current.zip` and then `deploy/` if empty, so `case.diff` shows only what the run wrote. The header comment names `zip` among the needs | `tests/eval/roles/run.sh:8, 156-165, 177-187, 255` | `RoleEvalTests.Every_command_the_drafts_list_is_in_the_run_scripts_allowlist` (C3 is a `powerops manifest` call); `bash -n` |
| 7 | `RoleEvalTests`: `HeaderLineProblems` requires seven lines, ending `solution-hash: ` and `deploy-hash: `; the test named for "six header lines" says seven; the good sample gains H7 and a bad sample drops it and one swaps it with `solution-hash:` | `tests/PowerOps.Cli.Tests/RoleEvalTests.cs:505-511, 678-686, 855-895` | the samples |
| 8 | The route. `powerops-implement` section 4, the bullets after step 3: the request also names the managed zip that will be deployed when there is one in the deploy folder, and the current solution is then that zip unpacked into a fresh folder in the repository with `pac solution unpack --zipfile <zip> --folder <folder> --packagetype Managed`, run by `dv-solution` or by the person (the skill never runs `pac` itself, apart from its sign-in probe); the previous solution stays the last export before this plan. The "Neither report changes the plan's status" bullet says the routes link a report to a zip by `deploy-hash:` and check that its `solution-hash:` is the digest of the zip's unpack, and drops the advice to rerun both roles on the zip. HANDOFF step 3 gains the managed unpack command beside the unmanaged one; steps 7 and 8 say the same link and drop "Until the roles can bind an unpacked folder to a zip ...". COMMANDS `deploy plan` (`:114`), `deploy apply` (`:122`): the link, the unpack, its four failure sentences, the note, the new warning sentences, that the unpack writes only a temp folder it deletes, and that a different `pac` version can give another digest and so refuses. USE-WITH-AGENTS `:69`; PLANNING-SKILLS `:221` seven lines with `deploy-hash`. `GITHUB-ACTION.md` only where it states the link. Plugin copies of `powerops-implement` through "Update goldens" | `skills/powerops-implement/SKILL.md:70-79`, `docs/HANDOFF.md:36-51`, `docs/COMMANDS.md:114, 120-124`, `docs/USE-WITH-AGENTS.md:69`, `docs/PLANNING-SKILLS.md:221`, `docs/GITHUB-ACTION.md`, `plugins/*/skills/powerops-implement/SKILL.md` | `DocsDriftTests`, `HandoffDocTests`, `PluginManifestTests` as they stand; a `HandoffDocTests` case that step 7 names `deploy-hash` |
| 9 | After the eval, with keep: this section gains a table of the evaluated text's blob ids (both `SKILL.md` files and `run.sh` at the freeze commit, on their LF text), in the form `RoleEvalTests.PinnedRow` reads. `RoleEvalTests.The_kept_roles_text_is_the_pinned_or_a_recorded_blob` reads the pins from this plan and the recorded comparisons from this folder's `decisions.md`. `AGENTS.md`'s rule names this plan's Data section and this folder's `decisions.md`. The plugin copies of both roles through "Update goldens" | this file, `tests/PowerOps.Cli.Tests/RoleEvalTests.cs:939-955`, `AGENTS.md:40`, `plugins/*/skills/powerops-verify/`, `plugins/*/skills/powerops-review-security/` | `RoleEvalTests`, `PluginManifestTests` |

The skill text of rows 4 and 5, as it will be frozen:

- **R1V** (Verifier, a new request bullet): ``- the managed zip that will be deployed, if any. The current solution is then that zip unpacked into a folder``
- **R1S** (Security reviewer, replacing the line 16 sentence that row 5 quotes): ``This needs the current solution, and takes the settings file, the previous solution and the managed zip that will be deployed if the request names them. With the zip, the current solution is that zip unpacked into a folder.``
- **C3** (both): ``- `powerops manifest <deployed zip>`: only when the request names the zip that will be deployed. Gives the digest for the report's `deploy-hash:` line.``
- **H7** (both): `deploy-hash: <the digest that the manifest command prints for the deployed zip, else unknown>`

## Automation

None of the product's automation changes. This is the rerun, fixed before the first run, as M55's (`docs/plans/m55-role-freeze/plan.md`, Automation), with the differences named here.

| Pass | Cases | Runs per client |
|---|---|---|
| Verifier | `solution-01` to `solution-08` | 8 |
| Security reviewer | `solution-01` to `solution-08` | 8 |

That is 32 runs, through `run.sh` from a clean clone of the freeze commit outside the repo, with `EVAL_OUT` and a temp root outside the system temp folder for each client, as in M55. Claude Code runs with `EVAL_MODEL=opus` and Codex with `gpt-6-astra`, both at `EVAL_EFFORT=high`. Client versions are recorded. The two clients run side by side, one run at a time each, case by case, Verifier first. The eval runs from this session only, in the background.

**Freeze.** One commit holds rows 1 to 7 and this signed plan. A clean clone of it builds with 0 warnings, passes `dotnet format --verify-no-changes`, and passes `dotnet test` except the checks that wait for the decision: `PluginManifestTests` for the two changed skills' copies and `RoleEvalTests.The_kept_roles_text_is_the_pinned_or_a_recorded_blob`. `report.md` names the commit and the failing test names. A defect in rows 4 to 7 found after the freeze gets a new freeze commit, and every pass it touches reruns on both clients. Rows 8 and 9 and docs land after the freeze; they change no evaluated text and no file under `src/`.

**Scoring, as M55.** The coordinating session writes a neutral copy of every report (finding text only, new ids, no header lines, no notes). A Sonnet subagent and Codex in a read-only sandbox, neither of which ran a review, score every copy against `key.json` on their own; each pair of copy and keyed flaw is found or not; both list any claim about tenant configuration drawn from an export. The coordinating session settles each difference and records it, and records from the originals each wrong `state`, stray write, read outside the temp folder, and whether the run called `powerops manifest` on the zip.

**Keep rules, M38's, unchanged:**

- Each role finds at least 15% of its own flaws marked `baseline: not-shown`, pooled over both families; each family is shown.
- At most one false High per role across its clean-case runs on `solution-02` and `solution-06`, pooled.
- A pooled denominator below 5 is inconclusive, and inconclusive doesn't justify keeping.
- One valid run per case, pass and family; conclusions are provisional.

**Recorded beside the keep rules, defined now:** each role's result on its own new case (`solution-07` Verifier, `solution-08` Security reviewer); unchanged statements judged false, unsupported or supported as M55 defines them; and the header: whether each report's first seven lines carry the seven keys in order, whether `solution-hash:` equals the digest `powerops manifest solution/current` of the freeze build prints for the case, and whether `deploy-hash:` equals that run's `zip-digest:` in `meta.txt`. Each failure is listed. A role that misses a floor, or a header that fails in more than one report of a role, is a failed rule for the stop below.

**Failures and replacements, as M55.** An infrastructure failure (client, sign-in, quota, rate limit, network, or the 30-minute stop with retries in the stream) gets one replacement; a quota or rate-limit pause restarts the same run. A valid poor run is never replaced. A family that can't run (missing CLI, sign-in or credits) is a stop.

**Stop.** If any keep rule fails for either role or family, or the eval can't run for a family: no pin, no plugin copies, no merge; the merge lock is released if held; what exists is committed locally; and the report says which rule failed, with its numbers. The person decides.

**Cost.** M55 measured $8.09 at Claude Code's list price for its 16 Claude runs ($0.51 a run). The seventh line and one more `manifest` call add little; with 30% margin that is about $11, up to four replacements about $3, and the Sonnet scorer about $2: **about $16 expected**. The cap is the person's **USD 30** for the Claude runs, their replacements and the Sonnet scorer together, counted from each run's `total_cost_usd`. If the running total would pass USD 30, the runs stop and the person is asked. Codex reports no cost and is recorded in tokens, about 4.5M expected with its scorer.

## Screens

None.

## Environments and route

None. No step reads or writes a tenant. `pac solution unpack` runs locally (no `--environment`, no sign-in; Learn "pac solution", read 2026-11-09), in the tests and in `deploy plan`. No step runs `plan facts`, `az`, a `pac` command that reaches a host, or any `--apply`.

## Solution and publisher

None. The tests use the seed zips and folders and `DeployFakes`.

## Constraints

- AGENTS.md: smallest change, new logic with tests, `pac` as a subprocess with an argument list, offline, Conventional Commits, no version or changelog edits, no tenant values. The unpack writes only a temp folder that `deploy plan` deletes; no file is written through `File.WriteAll*`, `FileMode.Create*` or `new StreamWriter(`.
- The AGENTS.md rule on a kept role's skill text: rows 4 to 6 ship only after the rerun and the keep decision; their plugin copies and pins wait for it.
- This is the one chain milestone allowed to edit the kept roles, their copies and `tests/eval/`, and only rows 4, 5, 6 and 9.
- The eval runs by hand, from one place, never per build.
- No planted value goes into `report.md`. Eval inputs are repo fixtures only.

## Assumptions

- `pac solution unpack --packagetype Managed` of both managed seed zips (`ColumnLevelSecuritySampleSolution_managed.zip`, `Contoso_sample_1_0_0_1_managed.zip`), pac 2.14.1 on Linux, 2026-11-09: three runs each, about ten minutes apart, into folders of different names, gave the same `powerops manifest` digest (27 and 6 files). `analyze` reads the folders. The layout equals the unmanaged unpack's except that form files are `FormXml/<type>/{id}_managed.xml`, so a `manifest` comparison with an unmanaged previous folder lists the forms as removed and added and `Other/Solution.xml` as changed, and the roles then re-read them (rule B), which is safe. Learn documents neither determinism nor the suffix; it lists `--useUnmanagedFileForMissingManaged` for AppModuleSiteMap, AppModuleMap and FormXml files (powerops-researcher, "pac solution" and "SolutionPackager tool", read 2026-11-09).
- An unpack of a managed zip with `--packagetype Unmanaged` fails ("Solution package type did not match requested type"), although Learn's SolutionPackager page says the type is read from the zip; so the call passes the zip's own type.
- Byte-stability needs no second unpack per check: the reviewer's folder came from its own unpack, so an unstable or different `pac` gives another digest and the check refuses. It can only fail closed (decisions Q1).
- `pac solution unpack` takes about a second for the seed zips; a large solution may take longer, and the 120-second timeout and the gate's 300-second deadline both fail closed.
- The roles in an eval run can't read `docs/`, so the skills carry their own text; the eval's zip is a plain `zip` of `solution/current`, not a `pac` export, which the roles only hash.
- `zip` 3.0 is on this machine's `PATH`; the Claude CLI is signed in (`claude auth status`, 2026-11-09) and Codex through ChatGPT (`codex login status`).

## Open questions

## Questions for your admin

## Steps

1. This plan and `decisions.md`, the Fable plan review and `powerops-review-plan` in a fresh context, fixes, then the building session's sign-off on the person's instruction of 2026-11-09.
2. Rows 1 to 3, tests first.
3. Rows 4 to 7 in one edit. `dotnet build`, `dotnet test` (only the freeze's known failures), `dotnet format --verify-no-changes`. Don't run `POWEROPS_UPDATE_GOLDEN=1` until step 6. Commit: this is the freeze. Check it from a clean clone.
4. The 32 runs in the background, then scoring, then `report.md`, under Automation. Row 8 may be written while the runs go, by another writer, and touches no frozen file.
5. Keep decision: if every rule holds, log it in `decisions.md` as renewed on the person's instruction of 2026-11-09; otherwise stop under Automation's Stop.
6. Row 9 and the plugin copies. The Data edit changes this plan's body, so set `status: approved`, run a second fresh-context pass of `powerops-review-plan`, and sign off again. The Fable work review.
7. The checks of Done when 7, `status: done`, then the merge protocol of `chain-rules.md`.
