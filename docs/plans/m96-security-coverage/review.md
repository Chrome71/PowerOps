plan-hash: 64712bec9abb2a7175ac59b819283f5c4b39314ff9aa1c804ff443ee43bc6584
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: the plan's `environment:` is null and the request said `tenant: offline`, so `powerops plan facts` was not run and no tenant was called.

The planning chat is not in this context. The review read `plan.md`, `decisions.md` (no `mockups/`), and the repo at `10a160f`: `DeployCommand.cs`, `DeployReviews.cs`, `PlanChecker.cs`, `SettingsCommand.cs`, `McpCommand.cs`, `SolutionParser.cs`, `ZipReader.cs`, `XmlUnpackReader.cs`, `SolutionDiff.cs`, `PO018CanvasForAllPatch.cs`, `RoleEvalTests.cs`, `HandoffDocTests.cs`, the two kept roles, `powerops-implement`, `run.sh`, the eval `NOTICE.md`, the seed `v2` canvas metadata and `docs/schemas/settings-validate.schema.json`.

This plan changes PowerOps itself and deploys nothing, so checklist items 1, 2 and 4 to 8 (solution boundary, connection references, DLP, route, managed state, delegation, security roles) don't apply and have no findings. Item 11 (size): the plan has a `Won't build` list, and each of rows 1 to 13 serves a Goal line; no finding.

Tools (item 12): `powerops`, `zip`, `claude`, `codex`, `sha256sum` and `gh` are on `PATH`; the agents `powerops-researcher` and `fable-reviewer` and the skill `powerops-review-plan` are available to this session. `reports/ci-watch.sh` and `reports/leakscan.sh` exist in the main checkout only (see R12). Whether the Claude CLI and Codex are signed in was not checked: the plan checks it right before the run.

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
