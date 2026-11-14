plan-hash: 9bd73bfd2191608af15c39fce05510dabd566922c8dc595c1744a84a7715aeca
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: the plan's `environment:` is null and the request said `tenant: offline`, so `powerops plan facts` was not run and no tenant was called.

Fourth pass, on the body after the keep (`8a588c4`), the second fresh pass step 6 calls for. The planning chat is not in this context. This pass ran `powerops plan status` (status `approved`, `review: done`, 0 open questions, 0 admin questions; warnings: this review was stale, and "sign-off answered by an agent" at `decisions.md:78`), read `plan.md`, `decisions.md` (no `mockups/`), the earlier findings below and `git diff 0011bdd 8a588c4` of the plan folder: the Data section gains the pin table and its paragraph, step 7's two placeholder paths are quoted, the header goes back to `approved`, and `decisions.md` gains three Log lines (the first freeze's failed work review, the eval, the owner's "Keep both"). The unchanged parts of the plan were not re-reviewed in depth.

The pin table: `b7f9f098676d13633475edbfb63a3262246893cd` is the commit `b7f9f09`; the three blob ids equal `git rev-parse b7f9f09:<path>` and the blob of each file in this worktree; the rows match `RoleEvalTests.PinnedRow` and its header row doesn't, so the test reads exactly three pins; `The_kept_roles_text_is_the_pinned_or_a_recorded_blob` now reads this folder, and `AGENTS.md:40` points at it. `dotnet test --project tests/PowerOps.Cli.Tests --filter "RoleEvalTests|PluginManifestTests"` passed here (36 of 36), which is Done-when 3's test check. Tools (item 12): `powerops`, `zip`, `claude`, `codex`, `sha256sum`, `gh`, `dotnet` and `git` are on `PATH`, as in the second pass; sign-in was not checked.

R20 is resolved by the step 7 edit. R1 to R19 are unchanged by this revision and keep their resolutions; none reopens. One new finding, R21.

Third pass, on revision 3 (`d771bff`). The planning chat is not in this context. This pass read `plan.md` and `review.md`, `git diff 6abbb6b d771bff` of the plan folder (`decisions.md` is unchanged), and at `a442b17` the line ranges revision 3 cites in `skills/powerops-review-security/SKILL.md`, `skills/powerops-verify/SKILL.md` and `skills/powerops-implement/SKILL.md`, plus the leak-scan lines of the M88 and M92 plans. The unchanged parts of the plan were not re-reviewed in depth, and the second pass's tool check (item 12) was not run again.

Each of R14 to R19 was checked against the plan as it stands; all six are resolved and keep their resolutions. Remarks: R14 holds, `:87` at `a442b17` is the import-routes bullet and row 10 now names it; R15 matches M92's Goal 7; R16 holds, every line number rows 5 and 6 give (`:26`, `:52`, `:54`, `:80`, `:81-89`, `:92`; `:35`, `:57`, `:58-66`, `:69`) is the line it names at `a442b17`, and P1's place is now one place; R17 holds; R18 holds but its placeholder lost the quotes M88 and M92 put around it (R20); R19 holds, with one extra blank line still left under `## Constraints` (plan line 124 to 125), which Markdown ignores. R1 to R13 are unchanged by revision 3. One new finding, R20.

Second pass, on revision 2 (`6abbb6b`). The planning chat is not in this context. This pass read `plan.md`, `decisions.md` (no `mockups/`), the first pass's findings below, `git diff 10a160f 6abbb6b` of the plan, and the repo at `6abbb6b`: `PlanChecker.cs`, `PlanStatusReport.cs`, `DeployReviews.cs`, `DeployCommand.cs` (the review check, `Load`, the `--require-reviews` help), `McpCommand.cs`, `SettingsCommand.cs`, `SolutionParser.cs` (`ReadMsapps`), `PO018CanvasForAllPatch.cs`, `SolutionDiff.cs`, `RoleEvalTests.cs`, `HandoffDocTests.cs`, `SkillsCommandTests.cs`, the two kept roles, `powerops-implement`, `run.sh`, `key.json`, `action.yml`, `global.json`, `.gitignore`, the seed and eval cases' canvas metadata, and `reports/leakscan.sh` in the main checkout.

Each of R1 to R13 was checked against the plan as it stands; all thirteen are resolved and keep their resolutions. Remarks: R6 holds because Assumptions now names sources for the claim, which meets item 13, though its `unproven:` line says "probed by none"; R7 leaves the exact `diff` lines to the build report, with the test "exactly the version change and the canvas app's change"; R11 doesn't say how the stub clients are put on `PATH`, which Automation's "as M83" covers (M83 step 3); R12's fix brought in R18. New findings start at R14.

This plan changes PowerOps itself and deploys nothing, so checklist items 1, 2 and 4 to 8 (solution boundary, connection references, DLP, route, managed state, delegation, security roles) don't apply and have no findings. Item 3: the `### Licences` heading now says no licence is needed and why (see R19 for its placement). Item 11 (size): the plan has a `Won't build` list, and each of rows 1 to 13 serves a Goal line; no finding.

Tools (item 12): `powerops`, `zip`, `claude`, `codex`, `sha256sum`, `gh`, `dotnet` and `git` are on `PATH`; `reports/leakscan.sh` and `reports/ci-watch.sh` exist in the main checkout; the agents `powerops-researcher` and `fable-reviewer` and the skill `powerops-review-plan` are available to this session. Whether the Claude CLI and Codex are signed in was not checked: the plan checks it right before the run.

## Findings

### High

No findings.

### Medium

#### R1. The freeze needs new goldens, but "Update goldens" also rewrites the frozen roles' plugin copies
- Area: 13 (buildability), Constraints (kept-role rule)
- Finding: Rows 1 and 4 change goldens before the freeze (`settings-validate.*`, `analyze-fixtures.*`, and the MCP golden if it quotes the output). The only way the repo regenerates goldens, `POWEROPS_UPDATE_GOLDEN=1 dotnet test`, also rewrites `plugins/*/skills/`, which would put the changed copies of both kept roles and `powerops-implement` into the freeze commit. The Constraints say those copies wait for the owner's keep decision, and the Freeze paragraph expects `PluginManifestTests.Plugin_skills_are_the_shipped_skills` to fail at the freeze. M83 avoided this by not running the update until its step 6, but M83's freeze changed no golden. Step 2 doesn't say how M96's freeze gets its goldens without the plugin copies.
- Suggested fix: In step 2, say that the executor runs the "Update goldens" recipe, then `git checkout plugins` (and `tests/golden/selftest.txt`), runs a normal `dotnet test`, and checks that `git diff --stat plugins` is empty before the freeze commit.
- Resolution: fixed: step 2 runs the recipe, then `git checkout plugins tests/golden/selftest.txt`, a normal `dotnet test`, and checks `git diff --stat plugins` is empty before the freeze.

#### R2. Two help texts in `src/` list the cover conditions in full, and no row before the freeze changes them
- Area: 13 (buildability), 9 (testability)
- Finding: The `--require-reviews` option description (`src/PowerOps.Cli/DeployCommand.cs:70`) and the MCP `deploy_apply` tool description (`src/PowerOps.Cli/McpCommand.cs:261`) each give the full list of what a covering pair of reports needs ("ready, written for the current plan, list no open High finding and record ..."). After row 3 that list is incomplete. Row 3 names only `DeployCommand.cs:99-160`. Row 11 updates `docs/COMMANDS.md`, but rows 11 and 12 "change ... no file under `src/`", so the CLI help and the tool description an agent reads would stay wrong after the merge. If an MCP tools golden quotes the description, it would also need regenerating at the freeze.
- Suggested fix: Add both strings to row 3's Where column, with a test or golden that shows the settings condition in each, so they change in the freeze commit.
- Resolution: fixed: row 3 names `DeployCommand.cs:70`, `McpCommand.cs:261` and `mcp-tools.txt`, changed in the freeze commit.

#### R3. An uncompressed `.msapp` in `solution-09` lets a text scan see the host, which a real export doesn't
- Area: 9 (testability of the eval)
- Finding: Row 8 stores the case's package "with no compression ... so its text is scanned" by `RoleEvalTests`' fake-value scans. A Security reviewer that greps or reads the folder would then see `Properties.json`'s host as plain text inside the `.msapp`. That satisfies F47 through "or names the host" without P1 doing anything, and it weakens the C-7 measure recorded beside the keep rules. Trial 3's failure happened because a real package is deflated and a folder scan can't see inside it, so the case doesn't reproduce the failure M96 is meant to fix.
- Suggested fix: Store the package deflated, as a real export would. Have the fake-value scan in `RoleEvalTests` open the zip and scan each entry, which `System.IO.Compression` can do. Otherwise, if the package stays uncompressed, score F47 only on "names the package as not opened" and say why in `key.json`'s sentence.
- Resolution: fixed: row 8 writes the package with deflate; row 7 opens each package with `ZipArchive`, asserts deflate and scans each entry.

#### R4. "Before any `pac` call" can't hold for reports linked by `deploy-hash:`, the route `powerops-implement` uses, and no `[R-C]` test covers that link
- Area: 13 (buildability), 9 (testability)
- Finding: Goal 2 says every guarded route refuses "before any `pac` call", and row 3's `[R-C]` test expects "zero `FakePac` calls". `WarnUnreviewed` runs `pac solution unpack` for every zip whose reports record it as their `deploy-hash:` (`DeployReviews.NeedsUnpack`, `DeployCommand.cs:127`) before it checks cover. That is the link that `powerops-implement` section 4 and the eval's prompts ("Deployed zip: deploy/current.zip") produce. So the `[R-C]` test can pass only with reports linked by `solution-hash:`, and the route agents actually use is left without a settings test.
- Suggested fix: In Goal 2, say "before any import". Add a second `[R-C]` case in row 3 whose reports are linked by `deploy-hash:`: one unpack call, no import call, and the settings sentence in the refusal.
- Resolution: fixed: Goal 2 says before any import; Goal 4 and row 3 add the `deploy-hash:` `[R-C]` case (one unpack, no import); Won't build keeps M83's order.

#### R5. No `### Licences` heading under Constraints
- Area: 3 (licences)
- Finding: The plan names a canvas app (`Work Items`, in the seed fixture and `solution-09`) and flows (the case's flow tests), and `Constraints` has no `### Licences` heading. Nothing is deployed, so no licence is needed, but the checklist asks for the heading whenever a plan names an app or a flow.
- Suggested fix: Add `### Licences` under Constraints with the line "No need beyond Microsoft 365's standard rights." Or add a line saying the named app and flows are offline test fixtures that are never imported.
- Resolution: fixed: Constraints gains `### Licences`: offline fixtures, never imported.

#### R14. `powerops-implement` still tells the build agent that only a changed zip needs the roles run again
- Area: 13 (buildability), 9 (testability)
- Finding: Row 10 changes only the request bullet of `skills/powerops-implement/SKILL.md` (`:84`). The bullet three lines below (`:87`, "Neither report changes the plan's status, but the import routes read them") lists what the import routes check (ready, current plan, linked to the zip) and says "a build that changes the zip needs both roles run again". After row 3 a changed settings file is refused too, so an agent that follows the skill, edits `<zip name>.settings.json` and goes on to `deploy plan` and the import meets a refusal that the skill never told it to expect or how to clear. This is the skill text agents act on, like the help strings R2 added to row 3, and no row changes it (rows 11 and 12 touch only docs and `AGENTS.md`).
- Suggested fix: Add `:87` to row 10: the routes also need each report's `settings-hash:` to be the SHA-256 of the settings file the deployment imports, and a build that changes the zip or its settings file needs both roles run again. It lands in the freeze commit with row 10, and its plugin copies wait for step 6 as row 10's do.
- Resolution: fixed: row 10 adds line 87 (a changed settings file needs both roles run again), in the freeze commit.

### Low

#### R6. The claim about a package's contents is not written as an `unproven:` line, and the research result isn't recorded
- Area: 13 (buildability)
- Finding: P1's last sentence goes into frozen text and rests on a claim: a `.msapp` holds `Properties.json` and `References/DataSources.json`, and a Dataverse data source there names the instance's host. Assumptions sends this to powerops-researcher "before the build" and names a fallback, but it doesn't use the form `unproven: <capability>; probed by step <n>`. `decisions.md` has no log line with the researcher's answer, although step 1 puts that check before sign-off.
- Suggested fix: Write it as `unproven: ...; probed by step 1`, and log the researcher's answer and its sources in `decisions.md` before sign-off, with P1's final wording if the fallback is used.
- Resolution: fixed: Assumptions records the researcher's answer of 2026-11-14 with sources and an `unproven:` line; P1 says "can hold"; decisions.md Log records it.

#### R7. Row 8's test says "nothing beyond the version change", but it's unclear what `diff` should show for the new screen
- Area: 9 (testability)
- Finding: `SolutionDiff` compares a canvas app only through its meta XML, and reports a changed `AppVersion` as "republished". If `solution/current` changes the meta for the new screen, `diff` reports the canvas app, which isn't the solution's version change. If the meta doesn't change, `diff` shows nothing of the change the case's plan describes, and the Verifier then has a plan change that no check shows.
- Suggested fix: Say in row 8 whether the meta's `AppVersion` changes, and list the exact `diff` lines the baseline for `solution-09` is expected to hold.
- Resolution: fixed: row 8 says `AppVersion` moves and `diff` gives exactly the version change and the canvas app's change, listed in the build report.

#### R8. Row 3's "the settings sentence comes after an open-High sentence for the same report" can't be seen in one output
- Area: 9 (testability)
- Finding: `Ready` returns only the first fault of a report, so the two sentences never appear together for the same report. The test can only show which one wins.
- Suggested fix: Reword: "with both an open High finding and a settings mismatch, the report's sentence is the open-High one".
- Resolution: fixed: row 3 says the open-High sentence wins when both apply.

#### R9. Row 1's test compares with `sha256sum`, which CI on Windows doesn't have
- Area: 9 (testability)
- Finding: "The line and field equal `sha256sum` of a fixture file" could be read as a test that runs `sha256sum`. CI runs on Windows as well as Linux, where that test would fail.
- Suggested fix: Say that the expected hex is a constant, computed once with `sha256sum` and written into the test (for the plain file and for the BOM and CRLF file).
- Resolution: fixed: row 1 tests against a constant hex computed once.

#### R10. Row 10 and the steps disagree on which step regenerates `powerops-implement`'s plugin copies
- Area: 9 (testability)
- Finding: Row 10 says "step 7's 'Update goldens' run", while row 13 and step 6 run that update in step 6. Step 7 only regenerates the copies after a rebase.
- Suggested fix: Change row 10 to "step 6's 'Update goldens' run (again in step 7 after a rebase)".
- Resolution: fixed: row 10 says step 6 (again in step 7 after a rebase).

#### R11. "`run.sh`'s stub check" is never defined in this plan
- Area: 9 (testability)
- Finding: The Freeze paragraph and step 3 need the stub check to pass before the first paid run, but only M83's step 3 says what it is. M96 adds a case with a package and a settings line.
- Suggested fix: Define it in step 3, either as M83's check run on `solution-09` or with what M96 adds. For example: the deploy zip holds the `.msapp`, the prompt names `Settings file: settings.json`, and a stub report's `settings-hash:` is checked against the case's file.
- Resolution: fixed: step 3 defines the stub check on `solution-09`.

#### R12. The leak scan and `ci-watch.sh` aren't in this worktree
- Area: 12 (tools)
- Finding: Goal 5 and step 7 run "the leak scan" and `ci-watch.sh`. Both are in the main checkout's `reports/` folder, which git excludes, so they are missing from this worktree and from the clean clone the freeze checks use.
- Suggested fix: Name them as `reports/leakscan.sh` and `reports/ci-watch.sh` in the main checkout, and say they run from there.
- Resolution: fixed: step 7 runs both scripts from the main checkout by path.

#### R13. The plan contains a host name
- Area: values in the plan
- Finding: Rows 8 and F47 and the Constraints name a host (`<host>`) on the reserved example domain. It is a fake value that the case needs, but the checklist reports every URL or host in a plan.
- Suggested fix: Keep it if it is intended as the case's planted fake value, and say so in Constraints. Otherwise refer to it as "the case's fake Dataverse host".
- Resolution: kept: Constraints names `dev.contoso.example` as the case's planted fake host.

#### R15. Done-when 3's test filter runs at solution level, where the repo's test runner exits 8
- Area: 9 (testability)
- Finding: Done-when 3 checks `dotnet test --filter "RoleEvalTests|PluginManifestTests"`. `global.json` selects Microsoft.Testing.Platform, and M92's plan records that a solution-level filter exits 8 because the Core project matches nothing. Both classes are in `tests/PowerOps.Cli.Tests`, so the check as written fails when everything is right.
- Suggested fix: Write `dotnet test --project tests/PowerOps.Cli.Tests --filter "RoleEvalTests|PluginManifestTests"`, as M92's Goal 7 does.
- Resolution: fixed: Done-when 3 adds `--project tests/PowerOps.Cli.Tests`.

#### R16. Row 5's placement of P1 and its line numbers don't point at one place in the frozen text
- Area: 9 (testability)
- Finding: Row 5 puts P1 "at the end of 'Review', before the checklist", but the Well-Architected checklist and the Points list are inside `## Review` (`skills/powerops-review-security/SKILL.md:54-76`), so "the end of Review" and "before the checklist" are two different places, and row 5's `64-66` falls in the middle of the checklist (SE:07 to SE:09). Its other line numbers also miss: the `settings validate` bullet that S1 replaces is at `:26`, not `:24`, and "These are the first seven lines" is at `:92` in the security role and `:69` in the Verifier, outside the ranges `79-90` and `55-66` row 5 and row 6 give. The text is frozen and pinned by blob id, so where it lands is part of what the rerun evaluates.
- Suggested fix: Say "after the paragraph that starts 'Write that a security role, table, column, form, view or app role map is unchanged', before `Well-Architected Security checklist:`" (or the end of the Points list, whichever is meant), and correct the line numbers to `:26, 52-54, 80-92` and `:35, 57-69`.
- Resolution: fixed: rows 5 and 6 and P1 name the place (after the "Write that a security role" paragraph, before the checklist) and the lines at `a442b17`.

#### R17. Step 7 names the pull request and `ci-watch.sh` twice
- Area: 9 (testability)
- Finding: Step 7 reads "pull request, `bash .../ci-watch.sh <PR>`, pull request, `ci-watch.sh`, squash merge". Revision 2 added the full command in front of the old words without removing them, so the step reads as two pull requests and two watches.
- Suggested fix: Drop the second "pull request, `ci-watch.sh`".
- Resolution: fixed: step 7 names them once.

#### R18. Step 7 holds an absolute path with the owner's home folder name
- Area: values in the plan
- Finding: R12's fix wrote the leak scan and `ci-watch.sh` commands with the main checkout's absolute path (`<path>`), whose home folder is the owner's account name. The plan is committed to a public repository, and `reports/leakscan.sh` prints that pattern among the identifier-shaped strings of the diff for a human look. M88 and M92 wrote the same command with a placeholder.
- Suggested fix: Write `cd "<main checkout>" && bash reports/leakscan.sh origin/main <this branch>` and `bash "<main checkout>/reports/ci-watch.sh" <PR>`, as M88 and M92 do.
- Resolution: fixed: step 7 uses `<main checkout>`.

#### R19. The rest of Constraints now sits under the `### Licences` heading
- Area: 3 (licences), polish
- Finding: R5's fix put `### Licences` at the top of `## Constraints`, followed by a blank line too many and then the AGENTS.md, kept-role, output-shape and eval bullets, with no heading of their own. They read as licence lines.
- Suggested fix: Move `### Licences` and its line to the end of `## Constraints`, or put a heading such as `### Rules` before the bullets, and drop the extra blank line.
- Resolution: fixed: `### Licences` moved to the end of Constraints, after its bullets.

#### R20. Step 7's `<main checkout>` placeholder is unquoted, and the real path has a space
- Area: 9 (testability), 12 (tools)
- Finding: R18's fix writes `cd <main checkout> && bash reports/leakscan.sh ...` and `bash <main checkout>/reports/ci-watch.sh <PR>` with no quotes. The main checkout's path holds a space, so an executor that puts the path in place of the placeholder as written gets a `cd` that fails and a `bash` that can't find the script. Both fail loudly rather than silently, but they stop step 7. M88 and M92 write `cd "<main checkout>"`, and R18's suggested fix had the quotes.
- Suggested fix: Write `cd "<main checkout>" && bash reports/leakscan.sh origin/main claude/m96-role-rerun-9271cf` and `bash "<main checkout>/reports/ci-watch.sh" <PR>`.
- Resolution: fixed: step 7 quotes both, `cd "<main checkout>"` and `bash "<main checkout>/reports/ci-watch.sh" <PR>` (checked in the fourth pass).

#### R21. Step 6's second sign-off doesn't say whose answer it rests on, and `plan status` warns on the first one
- Area: 10 (unanswered areas), 9 (testability)
- Finding: `powerops plan status` warns "sign-off answered by an agent (`decisions.md:78`): only a person signs off a plan". Step 6 says only "sign off again" after this pass, and the Log line of the keep says "the plan is reviewed and signed off again" without naming the answer it rests on. The owner's "Keep both" decides the keep, not the changed body (the pins, rows 11 to 13), so a second agent sign-off logged like line 78 gives the same warning, and nothing in the plan says that warning is expected or where it is accepted. Goal 5 counts only violations, so nothing fails, but a reader of `plan status` at merge sees an agent sign-off the plan never explains for this body.
- Suggested fix: In step 6 (or the Log line that records the second sign-off), name the owner's answers it rests on ("Keep both", 2026-11-14, with the standing instructions line 78 quotes) and say that `plan status`'s agent-sign-off warning is expected for this chain and is not a violation.
- Resolution: accepted: the Log line of the second sign-off names the owner's answers it rests on and says the warning is expected (`decisions.md`, Log, 2026-11-14)
