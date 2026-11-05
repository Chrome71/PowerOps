---
status: approved
review: done
environment: null
depth: standard
supersedes: null
signed-off: 2026-11-04
signed-off-hash: 71c00e0c0086591816c5e6354da3df38ac6d6759ac01dcacfe248a7ee946ca7b
---

# M55: The review roles get diff limits and a report header, frozen once through an evaluation

## Goal

Stop the two kept review roles, `powerops-verify` and `powerops-review-security`, from calling a component unchanged on the word of a `diff` that never compared it, and give their reports a header that names the solution they read. This is M55 of the improvement plan written after the live test of 2026-11-04 (P0 block). That plan, its reviews and the person's answers D1 to D9 are kept outside the repo, because they quote a private test run. In that run a role read `diff`'s "no changes" as "roles and app unchanged", where `diff` does not read either.

D2 is answered by the person: "Approve, decide later (Recommended)". The rerun of both roles is approved at a provisional USD 30 to 40 and about a day. Keep or change is decided on the results, by the person only.

Both text changes land in one edit, so the roles are evaluated once. The AGENTS.md rule on the skill text of a kept role applies: a change to a check, command, input or report line reruns the pass on all its cases and both families, recomputes the keep rules and renews the person's decision before it ships.

Done when:

1. `powerops manifest <solution>` prints a folder's file inventory with a SHA-256 per file and one digest, or a zip's SHA-256. `powerops manifest <old> <new>` compares two unpacked folders file by file. It is offline and read-only, documented in `docs/COMMANDS.md` and the README's command table, and tested in Core and CLI.
2. Both skills say that `diff` compares only what `COMMANDS.md#diff` lists. They state "unchanged" for a security role, table, column, form, view or app role map only after the file-by-file `manifest` comparison, and otherwise re-read the component and say so. Both reports start with six header lines, ending `input:` and `solution-hash:`. Both forms of the command are in the skills' lists, and `run.sh`'s allowlist covers them.
3. `plan status` reads `input:` and `solution-hash:` from the first six lines of `security-solution.md` and `verify.md`. A report without a valid `solution-hash:` reads `unknown`. The JSON gains `input` and `solutionHash` for each report, and the schema, golden and docs say so.
4. Two new solution cases, `solution-07` keyed for the Verifier and `solution-08` for the Security reviewer, each change a role's privileges and an app's role map while the root components stay the same. Their key entries and baselines exist, and `RoleEvalTests` holds for the larger set. Cases `solution-01` to `solution-06`, their key entries and their baselines don't change.
5. Both roles ran on all 8 solution cases on Claude Code and Codex, from one freeze commit. The scoring was fixed before the first run. `report.md` in this folder records every result as run, with the keep rules recomputed, and no valid poor run was replaced.
6. The person's keep or change decision is logged in `decisions.md` with `(answered by: person)`. Only after it do the plugin copies change, the pinned blob ids go into Data, and the AGENTS.md pointer moves to this plan.
7. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` (no violation) and the leak scan (0 hits) are clean, and the PR is green on all eight checks.

### Won't build

- Staleness detection from the new header lines, in `plan status` or `deploy plan`. That is M62's (decisions Q5).
- JSON output, an MCP tool, or a per-entry listing of a zip for `manifest`. The roles read text, M62 will call Core directly, and a zip is compared by its SHA-256 only (decisions Q1).
- A `diff` that reads roles, tables, forms, views or app modules (decisions Q1).
- Any change to cases `solution-01` to `solution-06`, the plan cases, the pilot, their key entries or their baselines. The frozen set stays comparable with M38 and M39 (decisions Q6).
- A rerun of `powerops-review-plan`, whose text doesn't change.
- A section or numbered decision in `docs/BUILD-PLAN.md`.

## Users

- A maker whose solution the two roles review before promotion, and who needs "unchanged" to mean compared.
- The person who reads `verify.md` and `security-solution.md` and decides on promotion.
- M62, which will compare a report's `solution-hash:` with the solution now on disk.
- A contributor who reruns the set after a later change to a role.

## Data

Line numbers are those of `431df9b`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | Core: `public static class Manifest` in `PowerOps.Core.Readers`, with `record ManifestEntry(string Path, string Sha256)`, `record SolutionManifest(string Kind, IReadOnlyList<ManifestEntry> Files, string Digest)` (`Kind` is `folder` or `zip`) and `record ManifestComparison(IReadOnlyList<string> Added, IReadOnlyList<string> Removed, IReadOnlyList<string> Changed, int Same)`. `Read(string path, ICollection<string>? skippedLinks = null)`: a folder lists every file through `SolutionDiscovery.EnumerateFiles` (links aren't followed and go to `skippedLinks`; `.git`, `node_modules`, `bin` and `obj` aren't entered, as in the readers), under a `ReadBudget` (file count, each file through `BoundedRead.ReadAllBytes` at `MaxFileBytes`, total bytes). Paths are relative, with `/`, in ordinal order. A file with no zero byte is hashed with each CRLF replaced by LF, and any other file as its bytes, so a Windows checkout and a Linux checkout of one commit give the same digest (decisions Q2). The digest is the SHA-256 of the UTF-8 lines `<sha256>  <path>\n`, in that order, in lowercase hex. A file whose name ends in `.zip` (any case) is read through `BoundedRead.ReadAllBytes` at `BoundedRead.MaxZipBytes`, `Files` is empty, and its digest is the SHA-256 of its bytes, which is the hash `deploy plan` records. A path that is a symbolic link, that does not exist, or that is a file of another name throws `InvalidDataException` whose message starts with the path. `Compare(old, new)` takes two folder manifests and returns the paths added, removed and changed, each in ordinal order, and the count of the same | `src/PowerOps.Core/Readers/Manifest.cs` (new) | `tests/PowerOps.Core.Tests/Readers/ManifestTests.cs` (new): inventory order and separators; CRLF and LF give one hash; a file with a zero byte is hashed raw; a link inside is skipped and reported; a link as the path, and a missing path, throw; the zip digest equals `DeployPlan.Sha256Hex` of its bytes; the digest of a known two-file folder; the over-cap and file-count limits; `Compare` on added, removed, changed and same |
| 2 | Text report: `ManifestReport.WriteText(TextWriter, string display, SolutionManifest)` prints `<display>: folder, <n> files, digest <hex>` (or `1 file`), then one line `<sha256>  <path>` per file. For a zip it prints `<display>: zip, digest <hex>` only. `WriteComparison` prints both first lines, then `added <path>`, `removed <path>` and `changed <path>` lines (in that order of kind, each list in ordinal order), then `<n> files the same`. Paths are cleaned with `TextReport.Clean` | `src/PowerOps.Core/Output/ManifestReport.cs` (new) | `tests/PowerOps.Core.Tests/Output/ManifestReportTests.cs` (new) |
| 3 | Command: `powerops manifest <path> [<path>]` in `ManifestCommand.Create(TextWriter output, TextWriter error)`, registered in `Program.Root` after `diff`. One path prints the manifest and exits `0`. Two paths must both be folders: it prints the comparison and exits `0` when nothing differs, `1` when something does. The command checks before `Read`, as `DiffCommand` does: a missing path, a file that isn't a `.zip`, a link as the path (its link target), or a zip in the two-path form, exits `2` with an `error:` line. `InvalidDataException` and I/O errors from `Read` (an unreadable path, a file over the cap, too many files) exit `3`. Every path in an `error:` or `note:` line goes through `TextReport.Clean`. The goldens are made from relative paths in a known working folder, or with the repo root scrubbed, so they hold no machine path. The docs say to give the solution's own folder, not a folder that holds `docs/plans/`, because a report written there would change the digest. Each skipped link prints `note: <path>: a symbolic link, not read` on stderr. Nothing is written, so there is no `--out` and no `SafeWrite` | `src/PowerOps.Cli/ManifestCommand.cs` (new), `src/PowerOps.Cli/Program.cs:56`; `docs/COMMANDS.md`: a usage line and a `## manifest` section with the format, the hashing rule and the exit codes; a row in `README.md`'s command table | `tests/PowerOps.Cli.Tests/ManifestCommandTests.cs` (new): each exit code, on `tests/fixtures/powerops-seed/v1` and `v2`; text goldens `manifest-seed-v1.txt` and `manifest-seed-v1-v2.txt` |
| 4 | `plan status`. `PlanReportFile` gains `string? Input` and `string? SolutionHash`. `ReadReports` reads its head from the first six trimmed lines, not four, for every report. For `security-solution.md` and `verify.md`: `SolutionHash` is the `solution-hash:` value when it is 64 lowercase hex characters, and otherwise `unknown`, which covers a report from before M55. `Input` is the `input:` value when it is a locator, and otherwise null. A locator is 1 to 260 characters, after a trailing `/` is dropped, with no control character, no `:` and no `\`. It doesn't start with `/`, and no segment between `/` is empty, `.` or `..`. A present `input:` that isn't a locator warns `<file> has an input: line that is not a relative path`, so a report from before M55 whose `input:` was free text warns on every run, and the docs say so. For `review.md` both stay null. JSON: each report gets `input` and `solutionHash` after `stale`. The `plan-status.json` golden's fixture gains a `verify.md` with the six lines, so the golden shows a real hash and locator. Text: the line of a solution report gains `, input '<value>'` (or `input null`) and `, solution-hash <value>`, and the `review.md` line doesn't change. The schema marks both required, and `schemaVersion` stays 1, as for an added field | `src/PowerOps.Core/Plans/PlanChecker.cs:26`, `:249-306`, `src/PowerOps.Core/Plans/PlanStatusReport.cs:29`, `:69`, `docs/schemas/plan-status.schema.json:26-36`, `tests/golden/plan-status.json`, `docs/COMMANDS.md:122` | `PlanCheckerTests`: a six-line report, a legacy four-line report (`unknown`, and its `input:` on line 5 or 6), a malformed hash, each locator rule, `review.md` null; `PlanStatusReportTests`, `PlanCommandTests`, `McpCommandTests`, `OutputSchemaTests` |
| 5 | `skills/powerops-verify/SKILL.md`. After line 34, commands C1 and C2V (below the table). Verify step 1 becomes ``Run `analyze`, `test`, `diff`, `settings validate` and both `manifest` commands with the paths from the request.`` After step 2, paragraphs A and B become steps 3 and 4, and steps 3 to 5 become 5 to 7. Line 51 says "The first six lines of `verify.md`", the block gains lines H5 and H6 after `state:`, and line 60 says "first six lines". Line 73, the `input:` line that nothing parses, goes. The `## Checks` order gains `manifest` after `diff`, where the zip skip of C2V is recorded | `skills/powerops-verify/SKILL.md:30-75` | `RoleEvalTests` (six header lines, commands exist and are allowed), and the rerun |
| 6 | `skills/powerops-review-security/SKILL.md`. Line 16 runs `analyze`, `settings validate`, and `diff` and `manifest` of the solution against the previous version. After line 27, commands C1 and C2S. After line 45, paragraphs A and B, word for word as in row 5. Line 73 says "The first six lines are exactly:", the block gains H5 and H6 after `state:`, and line 82 says "first six lines". Line 95 goes. A skipped comparison is noted under the header lines, as a skipped `diff` is | `skills/powerops-review-security/SKILL.md:16-95` | as row 5 |
| 7 | `run.sh`: the Claude allowlist gains `'Bash(powerops manifest *)'`. The prompts don't change | `tests/eval/roles/run.sh:193-199` | `RoleEvalTests.Every_command_the_drafts_list_is_in_the_run_scripts_allowlist` |
| 8 | Two solution cases, written by an agent that neither reads nor changes the two skills, nor this plan's rows 5 and 6. In both, `solution/previous` is `tests/fixtures/powerops-seed/v1` plus one security role in `Roles/`, one model-driven app in `AppModules/` (and its site map in `AppModuleSiteMaps/` if the writer adds one), and their root components in `Other/Solution.xml`, at version `1.1.0.0`. `solution/current` is at `1.2.0.0` and carries its plan's own change to an existing component, so `Other/Solution.xml` lists the same root components. **`solution-07`** (keyed `verify`): the plan adds a column to an existing view, which `diff` doesn't compare, so `diff` prints only the version change, as close to the live run's "no changes" as a build with a raised version gets. Its roles and app stay as they were. The current solution also changes the role's privileges and the app's role map, which no step builds: two flaws, point `plan checklist`. **`solution-08`** (keyed `security-solution`): the plan changes an environment variable's default and the flow that uses it, so `diff` prints compared changes and none about roles or the app. It bounds the role to a narrow privilege and depth, and limits the app to named roles. The current solution widens the role, and its role map adds a role that the plan keeps out of the app: two flaws, points `security roles` and `SE:05`. All four are `baseline: not-shown`. `analyze` and `settings validate` report nothing on either current solution. The key gains F43 to F46. Baselines `solution-07.txt` and `solution-08.txt` are the output of the four commands that `BaselineCommands` lists. `NOTICE.md` says how the two cases' `solution/previous` differs from the seed. Then an agent that didn't write them checks that each keyed flaw is in its case and nowhere else | `tests/eval/roles/cases/solution-07/`, `solution-08/` (new), `tests/eval/roles/key.json`, `tests/eval/roles/baseline/`, `tests/eval/roles/NOTICE.md` | the `RoleEvalTests` of row 9 |
| 9 | `RoleEvalTests`: `MainSolutions` is 1 to 8. `CountProblems` and `KeyChecks` take an optional per-case count that overrides `perCase` for named cases, and the key check passes 2 for 07 and 08, with 6 flawed solution cases and 8 security and 8 verify flaws. The case and baseline checks expect 9 solution folders with the pilot. `SolutionCaseProblems` compares the current version with the previous one's, not the seed's. For 07 and 08 it requires every seed file other than `Other/Solution.xml` to be byte-equal in `solution/previous`, and no other extra file outside `Roles/`, `AppModules/` and `AppModuleSiteMaps/`. It also requires the `RootComponent` elements of both solutions to be equal, in order. `HeaderLineProblems` requires six lines: `plan-hash: `, `model: `, `fresh-context: `, `state: `, `input: ` and `solution-hash: `. The name of the not-shown test drops its "of 6". That `analyze`, `test` and `settings validate` report nothing on 07 and 08 is held by their baseline files, which the baseline test compares with live output | `tests/PowerOps.Cli.Tests/RoleEvalTests.cs:156-205`, `:291-353`, `:448-455`, `:484-533`, `:560-568` | the good and bad samples at `:738-854` gain a six-line header, per-case counts and a root-component mismatch |
| 10 | After the person's decision, with keep: the plugin copies through "Update goldens". This section gains a table of the evaluated text's blob ids: both `SKILL.md` files and `run.sh` at the freeze commit, on their LF text. `AGENTS.md`'s rule says both roles' evaluated text is pinned in this plan's Data section. `docs/PLANNING-SKILLS.md:217` says six header lines | `plugins/*/skills/powerops-verify/`, `plugins/*/skills/powerops-review-security/`, this file, `AGENTS.md:40`, `docs/PLANNING-SKILLS.md:217` | `PluginManifestTests`, `DocsDriftTests` |

The evaluated text, kept by the person on 2026-11-04. `850db2812901370e1fdea442c915eb73d057e918` is the commit the rerun evaluated. A squash merge leaves that commit off `main`, so the evaluated text is pinned by git blob id, on its LF text:

| File at `850db28` | Blob id |
|---|---|
| `skills/powerops-verify/SKILL.md` | `b88426069ada63900f2cf049669a8edfd32f564a` |
| `skills/powerops-review-security/SKILL.md` | `69e5aa659a4a525495af160a2d963595c1a7a568` |
| `tests/eval/roles/run.sh` | `bb50db68218b751d0fe32a84b65102e9725e5212` |

The skill text of rows 5 and 6, as it is frozen:

- **C1:** ``- `powerops manifest <current solution>`: gives the digest for the report's `solution-hash:` line.``
- **C2V** (Verifier): ``- `powerops manifest <previous solution> <current solution>`: compares two unpacked solution folders file by file. Skip it when either solution is a zip, and say so in `## Checks`.``
- **C2S** (Security reviewer): ``- `powerops manifest <previous solution> <current solution>`: only with a previous solution, and only when both solutions are unpacked folders. Otherwise skip it and write under the header lines of the report why it was skipped.``
- **A:** `` `diff` compares only what the PowerOps command reference lists for it (`docs/COMMANDS.md#diff`): the solution's unique name, version, managed flag and publisher, root components, missing dependencies, connection references, environment variables, cloud flows, canvas apps, and the instructions and tools of Copilot Studio agents. It compares classic workflows and business rules by name, category and state only. It does not compare security roles, tables, columns, forms, views or app modules and their role maps, so its `no changes` says nothing about them, and a part that one of its `note:` lines names was not compared. ``
- **B:** ``Write that a security role, table, column, form, view or app role map is unchanged only when the file-by-file `manifest` comparison lists none of its files as added, removed or changed. In an unpacked solution a security role is `Roles/<name>.xml`, an app module and its role map are `AppModules/<name>/AppModule.xml`, and a table with its columns, forms and views is the folder `Entities/<table>/`. A changed `Other/Customizations.xml` counts as a change to all of them. Otherwise read that component's files in the current solution, check them against the plan, and say that you read them and did not compare them. When either solution is a zip, the comparison can't run and its files can't be read: say that the component was not verified.``
- **H5:** `input: <the current solution's path as the request gives it, relative to the repository root, with / between folders>`
- **H6:** `solution-hash: <the digest that the manifest command prints for the current solution, else unknown>`

## Automation

None of the product's automation changes. This is the rerun, fixed before the first run.

| Pass | Cases | Runs per client |
|---|---|---|
| Verifier | `solution-01` to `solution-08` | 8 |
| Security reviewer | `solution-01` to `solution-08` | 8 |

That is 32 runs, through `run.sh` from a clean clone of the freeze commit. Claude Code runs with `EVAL_MODEL=opus` and Codex with `gpt-6-astra`, both at `EVAL_EFFORT=high`, as in M38 and M39. Client versions are recorded. The two clients run side by side, one run at a time each, case by case, Verifier first, in the same order on both.

**Freeze.** One commit holds rows 1 to 9. A clean clone of it builds, passes `dotnet format --verify-no-changes`, and passes `dotnet test` except the `PluginManifestTests` checks of the two changed skills' copies, which change only after the decision. `report.md` names that commit and records the failing test names. A defect found after the freeze gets a new freeze commit, and every pass it touches reruns on both clients. `report.md` records it.

**Scoring, as M38 and M39 did it.** The coordinating session writes a neutral copy of every report: only the finding text, with new ids, no header lines and no notes. Two agents that ran no review score every copy against the key on their own, one from each family: a Sonnet subagent and Codex in a read-only sandbox. Each pair of copy and keyed flaw of that case is found or not. A flaw counts once per case and run. Both scorers also list any claim about tenant configuration drawn from an export. The coordinating session settles each difference and records it. From the original reports and saved files it records each wrong `state`, each stray write, each read outside the temp folder, and whether the run called `powerops manifest`.

**Keep rules, as M38 set them.** The numbers are judgement calls:

- Each role finds at least 15% of its own flaws marked `baseline: not-shown`, pooled over both families. Each family is shown too. After this milestone that is 6 flaws per client for the Verifier and 7 for the Security reviewer.
- At most one false High per role across its clean-case runs on `solution-02` and `solution-06`, pooled. A false High is a High the scorers can't support from the case files. One they can support is an unkeyed finding.
- A pooled denominator below 5 is inconclusive, and inconclusive doesn't justify keeping.
- Passing a floor permits keeping and never requires it, so step 6 stops for the person. A missed floor permits no keeping, and its counts are shown.
- One valid run per case, pass and family. `report.md` calls its conclusions provisional.

**Recorded beside the keep rules, defined now.** The person sees these with the keep rules, and none of them changes a count above:

- Each role's result on its own new case: both flaws, one or none.
- Unchanged statements. The coordinating session reads every original report for a statement that a security role, table, column, form, view or app role map is unchanged, the same, not modified or untouched, in any section. Each is `false` when the case's files show a change to that component between `solution/previous` and `solution/current`. It is `unsupported` when it is true but the run's stream shows no two-path `manifest` call, or shows one that listed a file of that component. Otherwise it is `supported`. The counts per role and family are shown, with this reading: one false statement means rule B didn't hold in that run.
- The header. For each valid report: whether its first six lines carry the six keys in order; whether `solution-hash:` equals the digest that `powerops manifest solution/current` of the freeze build prints for that case; and whether `plan status` reads its `input:` as a locator and its hash as 64 hex characters. Each failure is listed.
- A finding on `solution-07` or `solution-08` from the role that case isn't keyed for is unkeyed. It is neither a hit nor a false High, because the false-High rule covers only `solution-02` and `solution-06`.

**A missed floor.** It permits no keeping of that role's new text. The person still decides. Without another answer from them, that role's skill text goes back to its M39 pin, and M55 ships the command, the `plan status` reader and the other role. No role is dropped by this milestone.

**Failures and replacements, as M38 set them.** An infrastructure failure is a client, sign-in, quota, rate-limit or network error that ended a run before it wrote a report, or a stop at the 30-minute limit with retries or a rate-limit message in the stream. A quota or rate-limit failure pauses the runs, and the same run starts again after the limit resets, which is not a replacement. Any other infrastructure failure, and a stream that shows a read outside the temp folder, gets one replacement. If the replacement fails the same way, the case leaves that role's denominator. Every other run is valid and is scored as it stands, and a valid poor run is never replaced. Excluded runs are kept on file.

**Cost, fixed.** Claude, at Claude Code's list price: M38's $0.69 a Verifier run and M39's $0.83 a Security run, plus 30% for the comparison and re-reads, is about $16 for 16 runs. Up to four replacements add about $4 and the Sonnet scorer about $2, so the expected total is $22. The cap is **USD 30** at list price, for the Claude runs, their replacements and the Sonnet scorer together, and it is below the approved 40, so no extra approval is needed. If that spend reaches USD 30 before the runs and scoring end, they stop and the person is asked. Codex reports no cost. It is recorded in tokens, about 5M expected with its scorer. Time: about an hour of runs side by side, and about half an hour of scoring.

## Screens

None.

## Environments and route

None. No step reads or writes a tenant, and no step runs `pac`, `az`, `plan facts` or any `--apply`. The runs hold to this through `run.sh`'s setup, and their prompts claim no `pac` sign-in.

## Solution and publisher

None.

## Constraints

- AGENTS.md: smallest change, new logic with tests, offline and read-only (the new command writes nothing), Conventional Commits, no version or changelog edits, and no tenant URLs, ids, emails or user names in commits. Windows and Linux only.
- The AGENTS.md rule on a kept role's skill text: both text changes ship only after the rerun and the person's decision. The plugin copies and the pinned ids wait for that decision.
- Only this milestone changes the pinned roles, their plugin copies and `tests/eval`. Other P0 lanes run in parallel: M51 (agent reader), M57 (docs) and M58 part 2 (flow simulator). This milestone also touches `PlanChecker.cs`, `PlanStatusReport.cs`, the plan-status schema, `Program.cs`, `docs/COMMANDS.md` and `README.md`. Any edit there that another lane also makes is kept minimal and named in the PR body.
- The runs cost model quota. They run by hand, never per build.
- No planted value goes into `report.md`.
- No numbered decision goes into `docs/BUILD-PLAN.md`. Decisions are in this folder's `decisions.md`.

## Assumptions

- `diff` compares only the items that `docs/COMMANDS.md#diff` lists (line 49 at `431df9b`). Tables, views, forms and so on are not compared, and neither are roles or app modules, which are absent from the list. M50 decided against a static list of uncompared kinds in the output (its decisions Q1), so the skills carry the list.
- A security role in an unpacked solution is `Roles/<name>.xml`, and a model-driven app's role map is the `AppModuleRoleMaps` element of `AppModules/<name>/AppModule.xml`, as in `tests/fixtures/alm-sample`. Both are root components of the solution in `Other/Solution.xml`.
- The roles in an evaluation run can't read `docs/COMMANDS.md`, because the temp folder holds only the case. That is why the skills spell out the list.
- Learn, Power Platform Well-Architected, "Recommendations for security testing" (SE:09), is the improvement plan's source for this milestone: test the change, not an assumption about it.
- The nested `claude -p` sign-in was expired on 2026-11-04, and `claude auth status` reports no sign-in. The person signs in. The runs wait for that, and nobody else does it.
- Codex is signed in through the person's ChatGPT account.
- M50 and M54, which this plan builds on, are merged in `431df9b`. The baselines of `solution-01` to `solution-06` still match there, because `RoleEvalTests` passes on `main`.

## Open questions

## Questions for your admin

## Steps

1. This plan and `decisions.md`, a fresh-context review into `review.md`, and fixes. The building session signs off, outside the skills, under the person's delegation of 2026-10-22, once no High or Medium finding is open. The sign-off is logged with `(answered by: agent m55-role-freeze)`.
2. Rows 1 to 4 with their tests and goldens.
3. Rows 5 to 7 in one edit. Row 8 by its own agent, then row 9, then the check of row 8 by another agent. Run `dotnet build`, `dotnet test` (only the known plugin-copy failures) and `dotnet format --verify-no-changes`. Don't run `POWEROPS_UPDATE_GOLDEN=1` from here until step 7, because it would copy the frozen text into `plugins/`. Commit: this is the freeze.
4. Check the freeze from a clean clone. If the Claude sign-in is missing, ask the person in chat and wait.
5. The 32 runs, then scoring, then `report.md`, under Automation.
6. Stop. Show the person `report.md` with the keep rules. The person keeps or changes each role, and the decision is logged in `decisions.md` with `(answered by: person)`. No later step starts without it. With change, ship what the person decides, which changes this plan.
7. With keep: row 10. The Data edit changes this plan's body, so set `status: approved`. Run a second fresh-context pass of the review with the build in the tree, then sign off again, so `signed-off-hash` equals `review.md`'s `plan-hash`.
8. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan. Then push, open a PR with a Conventional Commits title, and wait for CI until all eight checks pass. Don't merge.
