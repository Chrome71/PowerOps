plan-hash: 211ba8bfea036ea2b90a0c1d2a96d9ad16d24e103971512c7746673464c4ead1
model: claude-opus-5-5
fresh-context: yes

Round 3 (the plan as revised after round 2, body hash above). The planning chat is not in this context. This round read `plan.md`, `decisions.md` (its Log included; no `mockups/`), rounds 1 and 2 below, and in the repo at `1dea7e7` (= `origin/main`): `PluginManifestTests.cs`, `PluginAgents.cs`, the theory in `SkillsCommandTests.cs`, `CLAUDE.md`, `AGENTS.md` lines 17 and 99, `plugin.json`, `docs/USE-WITH-AGENTS.md` lines 26 to 66 and 98 to 101, `docs/BUILD-PLAN.md`'s section headings and the lines open entries cite, every `Status` and `Affects` line of `docs/ecosystem/changes.md`, `EcosystemDocsTests` and `docs/ecosystem/README.md`. `powerops plan status --json` (installed CLI): `approved`, review `done`, 0 open questions, 0 admin questions, 0 violations; its one warning was round 2's stale `plan-hash`. Environment facts were not read: `tenant: offline`, no confirmation that `pac` is signed in, and `environment:` is `null`. No URL, GUID, email, person's name or home-folder path is in `plan.md` or `decisions.md`.

R1 to R11 keep their resolutions; R8 to R11 were checked against the plan as it stands:
- R8 holds in content: Data row 7 names ECO-0032 (`docs/USE-WITH-AGENTS.md:100`) and ECO-0012 (`docs/BUILD-PLAN.md:696`) and a catch-all for any other open pointer into the four docs. The full list of open pointers into those docs is ECO-0009 (`BUILD-PLAN.md:14`), ECO-0010, ECO-0012, ECO-0013 (`AGENTS.md:37`, `:38`), ECO-0023 and ECO-0025 (`CLAUDE.md:42`), ECO-0032 and ECO-0042 (`BUILD-PLAN.md:115`, `:124`, `:355`). With the M121 section after M118's (line 663), `AGENTS.md` line 17 and `CLAUDE.md` line 5 kept one line each, only `:696` and `:100` move, as row 7 says. When the recount runs, and what checks it, is a new finding (R12).
- R9 holds for Done line 8 (no absolute path; `reports/leakscan.sh` exists in the main checkout and is git-ignored) and for step 5's hand-over, now marked git-ignored. The chain rules in Constraints are still not marked (R13).
- R10 holds: after the edits, `git grep` of Done line 6 can hit only ECO-0010's Why (it names `PluginAgents.cs`), `docs/plans/` and the new test's message, all allowed; `docs/ecosystem/compatibility.md` and `tests/golden/` name neither string today.
- R11 holds: step 5 is one ordered list.

Also checked: the sentence of Data row 5 is in `plugin.json` word for word and the rest of the description holds no "specialist"; `plugin.json` has no `agents` key; `System.Text` stays used (line 118) once the generator goes, so only the `Regex` using goes, as row 4 says; `EcosystemDocsTests` checks only the shape of `Affects`, so step 2's deletion of `PluginAgents.cs` before row 7's narrowing breaks no test; no test pins the `AGENTS.md`, `CLAUDE.md` line 5 or `USE-WITH-AGENTS.md` text that changes; A2's source sentence is `docs/USE-WITH-AGENTS.md` line 30; the specialist sources name only Microsoft's plugins, so nothing under `.claude/agents/` goes stale. Tools (item 12): `dotnet`, `git`, `powerops` and `claude` (with `plugin validate`) are on PATH, and `reports/leakscan.sh` and the chain rules exist in the main checkout. Two new findings, R12 and R13, both Low.

Round 2:

Round 2 (the plan as revised after round 1, body hash above). The planning chat is not in this context. This round read `plan.md`, `decisions.md` (its Log included; no `mockups/`), round 1's findings below, and in the repo at `1dea7e7` (= `origin/main`): `tests/PowerOps.Cli.Tests/PluginManifestTests.cs`, `PluginAgents.cs`, the theory in `SkillsCommandTests.cs`, `CLAUDE.md` lines 1 to 13, `AGENTS.md` lines 17 and 99, `plugins/claude/.claude-plugin/plugin.json`, `docs/USE-WITH-AGENTS.md` lines 40 to 75, every `Affects` line of `docs/ecosystem/changes.md` that cites a line of a file this plan edits, the tests that read `AGENTS.md`, `CLAUDE.md` or `docs/USE-WITH-AGENTS.md` (`EcosystemDocsTests`, `DocsDriftTests`), and `reports/leakscan.sh` in the main checkout. `powerops plan status --json` (installed CLI): `approved`, review `pending`, 0 open questions, 0 admin questions, 0 violations; its one warning was round 1's stale `plan-hash`. Environment facts were not read: `tenant: offline`, no confirmation that `pac` is signed in, and `environment:` is `null`.

R1 to R7 keep their resolutions; each was checked against the plan as it stands:
- R1 holds: Data row 6 fixes the citation form (words only, a link only if M119's folder is on `origin/main` at the rebase), and Done line 5 requires the changed lines' relative links to resolve. No file links to the anchor of the subsection that goes, so no inbound link breaks.
- R2 holds: Data row 7 names both open entries, and their `Affects` lines are as it says (ECO-0010 holds the three targets that go; ECO-0025 holds `docs/USE-WITH-AGENTS.md:61`). Line 61 is the "Not checked." bullet and line 60 the "Tools." bullet, so row 7's "the 'Tools.' bullet" fits ECO-0010's `:60`, not ECO-0025's `:61`; both lines are in the subsection that goes, so the fix is the same. The shifts this edit causes in other open entries are a new finding (R8).
- R3 holds for the leak scan, whose command Done line 8 now gives, but that fix wrote the main checkout's absolute path (R9). The chain rules in Constraints and the hand-over in step 5 are still not marked as git-ignored files of the main checkout (part of R9's fix).
- R4 holds: line 10 is the class comment and line 193 the M93 comment; `Regex` is used only at lines 263 and 287 to 302, inside a test and a helper that go.
- R5 holds: line 5 of `CLAUDE.md` is the lead-in that names the copies, above `### Power Platform specialist team`.
- R6 and R7 hold: Data row 6's reinstall sentence names the release, the commit type is `fix(plugin):`, and Data row 4 adds `Assert.Null(manifest["agents"])`; `plugin.json` has no `agents` key now.

Also checked: the five deleted tests in Data row 2 are all `[Fact]`, and the theory has two cases, so "minus 5 facts and 1 theory case, plus 1 new fact" matches the arithmetic of 5,766 to 5,761 (the base total itself was not counted: no `dotnet test` run in this review). With the sentence of Data row 5 gone, the description holds no "specialist", so Done line 3's assertion can pass. The M93 strings are in the section between its heading and "Saved workflows in". `.claude-plugin/marketplace.json`, `README.md`, `docs/HANDOFF.md`, `docs/INSTALL.md` and the Codex plugin name no specialist agent. `DocsDriftTests` reads `docs/USE-WITH-AGENTS.md` for the MCP tool names and the `HARNESS.md` link, neither in the subsection that goes. Tools (item 12): `dotnet`, `powerops` and `claude` (with `plugin validate`) are on PATH; `reports/leakscan.sh` exists in the main checkout; the `fable-reviewer` agent is available. Four new findings, R8 to R11.

Round 1:

Environment facts were not read: the request gave `tenant: offline` and no confirmation that `pac` is signed in, and the plan's `environment:` is `null`. `powerops plan status --json` (installed CLI): `approved`, review `pending`, 0 open questions, 0 admin questions, 0 violations, 0 warnings.

Tools (item 12): `dotnet`, `powerops` and `claude` (with a `plugin validate` subcommand) are on PATH. The leak scan is not in this worktree (`reports/` is git-ignored); it exists in the main checkout (see R3). The `fable-reviewer` agent and the `powerops-review-plan` skill are available.

Checklist items that do not apply, because the plan changes repo files only and no Power Platform tenant: 1 (solution and publisher), 2 (connection references and environment variables), 3 (licences: the plan names no connector, table, app, flow, site, report or Copilot Studio agent; the "agents" it removes are Claude Code subagents), 4 (DLP), 6 (managed or unmanaged), 7 (data volume and delegation), 8 (security roles) and 11 (size: no table, list, flow or screen; `Won't build` is present). Item 5 holds for this repo: the route is branch, PR, CI and squash merge, with the owner's recorded "Yes to all three" as the merge authority. Items 9, 10, 12 and 13 are checked below. No URL, GUID, email or person's name was found in `plan.md` or `decisions.md`.

Checked in the repo: every name in Data row 2 exists in `tests/PowerOps.Cli.Tests/PluginManifestTests.cs` and `SkillsCommandTests.cs`; `PluginAgents` is used only there; both strings the M93 tests pin are in the raw "Power Platform specialist team" section of `CLAUDE.md` between its heading and "Saved workflows in"; the sentence of Data row 5 is in `plugin.json` word for word, and the rest of the description has no "specialist"; no file outside the ones the plan lists names `plugins/claude/agents/` except history and `docs/ecosystem/changes.md` (see R2); the M119 commit `54e1230` exists locally on `test/m119-specialist-eval`.

## Findings

### High

No findings.

### Medium

#### R1. The docs cite an M119 record that `main` may not have
- Area: 13 (buildability), 5 (route)
- Finding: Done line 5 and Data row 6 make `docs/USE-WITH-AGENTS.md`, `CLAUDE.md`, `AGENTS.md` and `docs/BUILD-PLAN.md` cite M119's "Keep none" as the reason. That decision is recorded only in `docs/plans/m119-specialist-eval/decisions.md` on the unpushed branch `test/m119-specialist-eval`. Constraints say M119 "merges separately" in either order, so if M121 lands first, user-facing docs cite (or link) a record that is not in the tree, and nothing in the plan says what happens if M119 never merges. No test checks that relative doc links resolve, so CI would not catch a dead link.
- Suggested fix: Pick one and write it in Constraints or Steps: merge M121 only after M119 is on `main`; or cite the decision by date and the owner's answer with no link to M119's folder, and add the link in whichever of the two lands second; or quote the evidence the docs rely on in this plan's `decisions.md` so the citation has a committed source.
- Resolution: fixed: Data row 6 now fixes the citation form (M119 cited in words only, no link to its folder; a link only if the folder is on `origin/main` at the rebase, else a hand-over follow-up), and Done line 5 requires every relative link in the changed lines to resolve.

#### R2. An open ecosystem entry names the files this plan deletes
- Area: 10 (unanswered areas), 13 (buildability)
- Finding: `Won't build` treats every `docs/ecosystem/changes.md` entry that names the old copy as history. ECO-0010 (Power BI modeling MCP `--readonly` coverage) is `Status: open`, and its `Affects` line lists `plugins/claude/agents/powerops-bi.md:32`, `tests/PowerOps.Cli.Tests/PluginAgents.cs` and `docs/USE-WITH-AGENTS.md:60`, which is the "Tools" bullet with the `--readonly` advice that Data row 6 removes. After the change, an open work item points at two deleted files and at a line that no longer holds its text, and A1's "no ecosystem row applies" does not account for it. `AGENTS.md` ("Ecosystem changes") asks a plan that changes text describing an external MCP server to read its open entries. The `EcosystemDocsTests` check only the shape of `Affects`, so no test fails.
- Suggested fix: Add a Data row and step line for ECO-0010: narrow its `Affects` to the paths that remain (`.claude/agents/powerops-bi.md:32`, `docs/HANDOFF.md:103`, `docs/HANDOFF.md:144`, `docs/ecosystem/compatibility.md`), or say in the Log why it stays as written. Also say whether plugin users still get the `--readonly` advice somewhere (for example `docs/HANDOFF.md`) once the bullet is gone.
- Resolution: fixed: Data row 7 and Done line 6 narrow the `Affects` lines of ECO-0010 and of ECO-0025 (which names the removed "Tools." bullet, line 61), keep their Why text and compatibility row 25, and say `docs/HANDOFF.md` keeps the `--readonly` advice; Won't build no longer calls open entries history.

#### R9. Done line 8 commits the main checkout's absolute path, with the local account's home folder
- Area: URLs, GUIDs, emails and names in the plan; 12 (tools)
- Finding: R3's fix wrote the leak scan's command with the main checkout's absolute path, which holds the local account's home folder name. The plan folder is committed, no plan on `main` holds such a path, and two earlier reviews (M109's R15, M115's) flagged the same thing and had it rewritten. The leak scan itself prints any home path in the diff for a human look, so the plan's own line would show up in the scan Done line 8 runs. The chain rules (Constraints) and the hand-over (step 5) still don't say they are git-ignored files of the main checkout.
- Suggested fix: Write the leak scan as earlier plans do: `reports/leakscan.sh` in the main checkout (git-ignored), run from there as `bash reports/leakscan.sh origin/main <branch>`, with no absolute path; say the same of `reports/trial-3/chain/` in Constraints and step 5. Check that no file of the plan folder holds a home path.
- Resolution: fixed: Done line 8 names `reports/leakscan.sh` run from the main checkout, with no absolute path; no plan file holds a home path now.

### Low

#### R3. The leak scan has no path, and it is not in the worktree
- Area: 12 (tools)
- Finding: Done line 7 requires "the leak scan (0 hits, control found)" but names no script. The script is `reports/leakscan.sh`, which is git-ignored, so it is missing from this worktree, where the checks of step 3 run; it exists only in the main checkout. The same goes for `reports/trial-3/chain/chain-rules.md` (Constraints, the lock protocol of Done line 7 and step 5) and the debate plan, which a reader of the committed plan cannot open.
- Suggested fix: Name the script and how it runs from the worktree (for example, its path in the main checkout with the `origin/main HEAD` arguments earlier plans used), and add an Assumptions line that the chain rules and debate plan are git-ignored files outside the worktree.
- Resolution: fixed: Done line 8 gives the leak scan's command from the main checkout and says why it is not in the worktree.

#### R4. Leftovers in `PluginManifestTests.cs` the plan doesn't name
- Area: 9 (testability)
- Finding: Data row 2 lists the deleted tests and helpers, but not what they leave behind: the class comment on line 10 ("the Claude plugin generated copies of the six specialists"), the comment on line 193 (the M93 tests are carried "into the copies" by a deleted test), and `using System.Text.RegularExpressions`, which only the deleted tests use. The build will not fail on the unused `using` under the current `.editorconfig`, but the stale comments would mislead the next reader.
- Suggested fix: Add to Data row 2: update the comments on lines 10 and 193 and drop the unused `using`.
- Resolution: fixed: Data row 4 lists the two comments and the unused using.

#### R5. Which `CLAUDE.md` paragraph changes is named two ways
- Area: 13 (buildability)
- Finding: Done line 5 says "the opening of `CLAUDE.md`'s specialist section" and `Won't build` says "only the section's opening paragraph", while Data row 6 edits "the opening paragraph of 'Claude Code extras'". The sentence on generated copies is in the paragraph above the `### Power Platform specialist team` heading, not in the section. An executor following Done line 5 could edit the section's first paragraph ("Six specialists plan, advise and review..."), which the routing and shared rules sit under.
- Suggested fix: Word Done line 5 and `Won't build` as Data row 6 does: the opening paragraph of "Claude Code extras", above the specialist section, which stays unchanged.
- Resolution: fixed: Done line 5 and Data row 6 both name `CLAUDE.md` line 5, the lead-in of "Claude Code extras"; it is the only `CLAUDE.md` line that changes.

#### R6. The reinstall sentence and the release are not tied together
- Area: 9 (testability)
- Finding: Data row 6 has `docs/USE-WITH-AGENTS.md` say an older install keeps its six agents "until the plugin is uninstalled and installed again (the version string does not change)". That holds only until release-please's next release changes `version` (now `0.0.0`); after that, an update also drops them. The plan also names no commit type, and the `BUILD-PLAN.md` row's release column depends on it (`fix:` makes a patch release, `docs:` or `chore:` none), so whether users ever get this change through a release is undecided.
- Suggested fix: State the commit type in Constraints and fill the row's release column from it, and word the sentence as "until the plugin is installed again, or until a release changes the version string".
- Resolution: fixed: Data row 6 ties the reinstall sentence to the release ("until a release changes the version string"), and Done line 8 and the BUILD-PLAN row name the commit type `fix(plugin):` and the next patch release.

#### R7. The new test doesn't cover the manifest key from A1
- Area: 9 (testability)
- Finding: A1 says Claude Code also loads agents named by an `agents` key in `plugin.json`. `Claude_plugin_ships_no_agents` (Data row 4) checks only that the folder is absent, so a later `agents` key would ship agents again with the test still green. `claude plugin validate` checks the manifest's shape, not whether it names agents.
- Suggested fix: Add `Assert.Null(manifest["agents"])` to `Claude_marketplace_lists_the_powerops_plugin` or to the new test.
- Resolution: fixed: Data row 4 adds `Assert.Null(manifest["agents"])` to the new test.

#### R8. The edit moves line numbers that two other open entries cite
- Area: 10 (unanswered areas), 13 (buildability)
- Finding: Data row 7 narrows the two open entries that name what goes, and Data row 6 keeps `CLAUDE.md` line 5 one line so ECO-0023's and ECO-0025's `CLAUDE.md:42` hold. Two other open entries cite lines that this plan's edits move. ECO-0032 cites `docs/USE-WITH-AGENTS.md:100` (the "Answer the form yourself" paragraph), which moves up when lines 45 to 62 become a short paragraph. ECO-0012 cites `docs/BUILD-PLAN.md:696`, which moves down when the M121 section goes in after M118's (line 663). `EcosystemDocsTests` checks only the shape of `Affects`, so no test fails, and each entry then points at the wrong line.
- Suggested fix: Add both to Data row 7: after the edits, renumber `docs/USE-WITH-AGENTS.md:100` in ECO-0032 and `docs/BUILD-PLAN.md:696` in ECO-0012 to the lines that hold the same text (the README forbids renumbering entries, not their cited lines), and redo it at the rebase if M119, M120 or M122 moved them again. Or grep every open entry's `Affects` for the four edited files and recheck each cited line, as a line of Done line 6.
- Resolution: fixed: Data row 7 recomputes, after the rebase, the line pointers of open entries into the files this diff shifts (ECO-0032, ECO-0012, and any other into the four changed docs), and says the `changes.md` diff is the two narrowings plus those numbers.

#### R10. Done line 6's grep forbids the new docs from naming the folder they remove
- Area: 9 (testability), 13 (buildability)
- Finding: Done line 6 allows `plugins/claude/agents` or `PluginAgents` only in ECO-0010's Why and `docs/plans/`. Data row 6 adds an M121 row and build-order line to `docs/BUILD-PLAN.md` and new text to `AGENTS.md`, `CLAUDE.md` and `docs/USE-WITH-AGENTS.md`, and nothing tells the writer that this new text must not name `plugins/claude/agents/` (a BUILD-PLAN row of a removal milestone naturally names what it removes; no BUILD-PLAN line names it today). Written that way, Done lines 5 and 6 can't both hold, and the writer finds out only at the grep.
- Suggested fix: In Data row 6, say the new text names the removed copies in words ("the plugin's copies of the six specialists") and not by path; or add the M121 row and build-order line to Data row 7's list of lines allowed to name it.
- Resolution: fixed: Data row 7 adds the new test's message to the allowed lines and says the other new text names no literal path.

#### R11. Step 5 says to rerun the checks twice
- Area: 9 (testability)
- Finding: Step 5 reads "rerun every check, rerun the checks", which says the same thing twice and leaves unclear whether a second run is meant (for example after CI).
- Suggested fix: Write it once: "rerun every check of Done line 8".
- Resolution: fixed: step 5 now reads as one ordered list: rebase, rerun the checks of Done line 8, `status: done` committed, then push, PR, CI and merge.

#### R12. The line-pointer recount waits for a rebase, and nothing checks it
- Area: 9 (testability), 13 (buildability)
- Finding: Data row 7 says the line pointers of open entries are recomputed "after the rebase", and step 5 lists them only under the rebase over M119, M120 and M122 "if any has merged". Step 3 carries row 7 but its done-condition (Done lines 5 to 7) has no line on pointers, so if nothing else has merged, ECO-0012's `docs/BUILD-PLAN.md:696` and ECO-0032's `docs/USE-WITH-AGENTS.md:100` can reach the PR pointing at the wrong lines, and `EcosystemDocsTests` checks only the shape of `Affects`.
- Suggested fix: In Data row 7 and step 3, recompute the pointers after the edits, and again after the rebase; add to Done line 6 that each recomputed `<file>:<line>` of an open entry holds the same text it held at `origin/main` (for example by comparing `git show origin/main:<file>` at the old line with the file at the new one).
- Resolution: accepted, no body change: settled in `decisions.md` (2026-11-22): the pointers are recomputed in step 3 and again after any rebase, and each new pointer is checked to hold the same text as the old one on `origin/main`; the work review checks it.

#### R13. The chain rules in Constraints are not marked as a git-ignored file of the main checkout
- Area: 12 (tools); the rest of R9's fix
- Finding: Constraints cite `reports/trial-3/chain/chain-rules.md` with no note that `reports/` is git-ignored and exists only in the main checkout, while Done line 8 (the leak scan), step 5 (the hand-over) and the Goal (the debate plan) now say so of theirs. A reader of the committed plan, or an executor in the worktree, looks for it in the tree and does not find it.
- Suggested fix: Add "(git-ignored; in the main checkout)" after the chain rules' path in Constraints.
- Resolution: accepted, no body change: `reports/` is git-ignored, as Done line 8 already says for the leak scan; the chain rules live in the main checkout's `reports/trial-3/chain/`.
