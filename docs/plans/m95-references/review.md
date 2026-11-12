plan-hash: 9b0ec81331a6438dd37d919a0cba02f2293f59b96930217a6d8dc8219319a698
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: the request did not say that `pac` is signed in to the plan's environment, and the plan's `environment:` is null (this milestone changes PowerOps itself and calls no tenant; tenant: offline, no `pac` or `az` call was made).

`plan status` was run with the worktree's build (`dotnet src/PowerOps.Cli/bin/Debug/net10.0/powerops.dll`, built at `969f68a`), because the `powerops` on PATH is at `b065f23`, before M94 changed `src/PowerOps.Core/Plans/`; both give the same `bodyHash`. Status `approved`, review `pending`, 0 open questions, 0 admin questions, no violation, no warning.

Tools (item 12): `dotnet` (10.0.114), `git` and `grep` were found with `command -v`; `powerops` too. `reports/leakscan.sh` exists in the main checkout only (ignored there by `.git/info/exclude`), as step 5 says. The `fable-reviewer` agent and the "Update goldens" recipe (`AGENTS.md`) exist. No step names an MCP tool or a Microsoft skill.

Read besides the plan: `skills/powerops-implement/SKILL.md` sections 4 and 5, `skills/powerops-plan/references/copilot-studio.md`, `canvas.md`, `power-bi.md` and `licensing.md`, `.claude/agents/powerops-workflow-logic.md` and `powerops-copilot-studio.md:43`, `docs/HANDOFF.md` steps 3 to 8, `docs/GITHUB-ACTION.md`, `docs/examples/README.md`, `promote-dev-to-test.yml` and `ephemeral-pr.yml`, `docs/COMMANDS.md:98-101`, `docs/ecosystem/changes.md` (last id ECO-0019, status done) and the four rows of `compatibility.md`, `tests/fixtures/NOTICE.md`, `SolutionParser.cs` (`ReadMsapps`, `MsappPath`), `SolutionDiff.cs:92`, `TestCommand.cs`, `PluginManifestTests.cs:207-211`, `SkillsCommandTests.cs:555`, `tests/smoke/skills-smoke.sh`, `docs/plans/m94-build-autonomy/plan.md:40` and `docs/plans/m44-canvas-reader/plan.md:32`, and `git grep -i msapp` at `87a31dd`. The claims of Data rows 6 to 8, 9 (b) and 10 and of the last Assumptions line hold at `87a31dd` (`docs/PLANNING-SKILLS.md` has no "the trade-offs"; ECO-0020 is the next free id; the `:209` pattern is `Workflow_logic_enforces_a_valid_record_at_the_write_not_in_a_flow_after_it`). The Microsoft facts of A1 to A13 were not re-checked on Learn.

Not applicable to a PowerOps milestone with no Power Platform solution: item 1 (solution and publisher), 2 (connection references and environment variables), 3 (licences: the plan names connectors, agents and reports only as reference text, and builds none), 4 (DLP), 6 (managed or unmanaged), 7 (delegation) and 8 (security roles). Item 5's route is Git: a branch, local commits, the chain's merge lock; nothing goes to a tenant. Item 11 has no table, list, flow or screen; R9 covers a row with no Goal line.

## Findings

### High

#### R1. Done line 4 limits the plugin-copy diff to rows 1 to 5, but rows 6, 8 and 10 also change copied files
- Area: 13 Buildability (two plan lines that can't both hold); 9 Testability
- Finding: Done line 4 says the regenerated plugin copies' "diff holds only the changes of rows 1 to 5". Row 6 changes `skills/powerops-implement/SKILL.md` section 4 (copied to `plugins/claude/skills/` and `plugins/codex/skills/`), row 8 changes `.claude/agents/powerops-workflow-logic.md` (copied to `plugins/claude/agents/`), and row 10 changes `skills/powerops-plan/references/licensing.md` (copied to both plugins). The recipe must regenerate those copies (`PluginManifestTests` fails on a stale copy), so Done line 4 can't hold once rows 6, 8 and 10 are built. Step 4's done-condition ("only step 3's changes to `skills/` and `.claude/agents/powerops-workflow-logic.md`") is the correct one and contradicts Done line 4.
- Suggested fix: Done line 4: "their diff holds only the changes of rows 1 to 6, 8 and 10 to `skills/` and `.claude/agents/powerops-workflow-logic.md`", matching step 4.
- Resolution: resolved (round 2): Done line 4 now matches step 4: the plugin diff holds only step 3's changes to `skills/` and the workflow-logic agent.

### Medium

#### R2. Step 5 diffs against a moving `origin/main` and leaves out the files of Done line 5
- Area: 9 Testability
- Finding: Step 5 checks `git diff --stat origin/main -- skills/powerops-verify ... tests/eval src tests/golden` for empty. That two-dot form compares `origin/main`'s tree with the branch. M100, M106 and M107 merge beside this milestone; once one of them changes `src/` or `tests/golden/` on `origin/main`, the check shows their change reversed and fails although M95 changed nothing there (or passes after a rebase that hides an M95 change in the same file). The check also leaves out the version files and the changelog that Done line 5 names (`Directory.Build.props`, `.release-please-manifest.json`, both `plugin.json` files, `CHANGELOG.md`).
- Suggested fix: Diff against the merge base: `git diff --stat origin/main...HEAD -- <paths>` plus `git status --short -- <paths>` for uncommitted files (or `git diff --stat 87a31dd -- <paths>` before any rebase), and add the version files and the changelog to the path list.
- Resolution: resolved (round 2): Done line 5 and step 5 use `git diff --stat origin/main...HEAD` plus `git status --short`, and name the version files and the changelog.

#### R3. "`pac solution pack` needs each `.msapp`" has no source
- Area: 13 Buildability; Constraints ("Each Microsoft fact carries its Learn source and the date it was checked")
- Finding: Row 6 writes into `HANDOFF.md` that "the solution can't be packed back from Git alone (`pac solution pack` needs each `.msapp`)". No Assumptions line gives a source for it, and the repo records a pack route that doesn't need the `.msapp` in Git: `docs/plans/m44-canvas-reader/plan.md:32` names `--processCanvasApps` (`CanvasApps/src/<app>/`), which Learn calls retired, and `tests/fixtures/alm-sample/CanvasApps/src/` holds such a source. The pin of row 9 (c) would then fix an unsourced claim in place.
- Suggested fix: Add an Assumptions line with the `pac solution pack` reference page, what it says about canvas apps, and the date; or word the sentence as the route's own fact ("step 3's unpack keeps no canvas source, so a rebuild exports again from Dev") without stating what `pac` can't do.
- Resolution: resolved (round 2): the pac claim is gone; row 6 cites A14 (Learn: a YAML-layout pack with a missing canvas `.msapp` exits 0 and leaves the app out; silent on the XML layout) and says a rebuild exports again from Dev.

#### R4. Row 1 states as fact what A1 says Learn doesn't say
- Area: 13 Buildability; the Data rule "What only trial 3 showed is written 'observed in one run (2026-11-11)', never as a general fact"
- Finding: Row 1's bullet opens "Copilot Studio has a new experience, which builds agents on the harness Learn now calls the Microsoft Copilot harness". A1 says "Learn doesn't say the home page's agent is that harness by default: written as observed". Row 1 gives the writer no "observed in one run" clause for that part, so the executor must choose between the row and A1.
- Suggested fix: In row 1, write the default as observed: "observed in one run (2026-11-11), the home page's new experience created an agent on the Microsoft Copilot harness", and keep the Learn facts of A1 (Other ways to build, New experience off, the session-only switch, the new name) as sourced.
- Resolution: resolved (round 2): row 1 writes the home page's harness as observed in one run (2026-11-11), and Learn's own statement (the harness is chosen at creation) beside it.

#### R5. With the zips out of Git, the CI routes that read the reviewed zip from the repo have no input
- Area: 5 Route; 9 Testability
- Finding: Row 7 found what assumes a tracked package with `git grep -i msapp`, which doesn't find text that assumes a committed zip. `HANDOFF.md` step 7 runs `powerops deploy plan deploy --target <test>` "on the pull request", and step 8 and `powerops-implement` section 4 (`SKILL.md:87`) say the Action imports a zip only when both reports are linked to it by its SHA-256 (`deploy-hash:`). Under the new default, `deploy/*.zip` is ignored, so a CI checkout holds no zip, and a zip exported again in CI has another hash, which no report records. Row 7 handles the ephemeral recipe and the Action's `path`, but not this: a person following steps 7 and 8 in CI meets a refused import with no sentence saying why.
- Suggested fix: Add to row 6 or 7 one sentence for `HANDOFF.md` step 7 (or 8): in CI, the zip comes from an export in CI, as in `docs/examples/`, and the report-linked import then runs where the reviewed zip is (a local `deploy apply`), or the workflow's environment reviewers are the approval (`require-reviews: 'false'`, as `promote-dev-to-test.yml` does). Add a matching pin to row 9 (c). Or list it under Won't build with the reason.
- Resolution: resolved (round 2): row 6 adds a step-7 sentence (the review-linked import runs where the reviewed zip is, or CI exports with `require-reviews: 'false'` and required reviewers), row 7 lists steps 7 and 8, `SKILL.md:87` and `GITHUB-ACTION.md:80` and the `cp exports/*.zip` step, and the sweep now also greps for committed zips.

#### R6. The `.gitignore` line covers the XML layout only, but the Goal says every canvas `.msapp`
- Area: 9 Testability; Goal
- Finding: The Goal keeps "every canvas `.msapp` out of Git by default", and row 6 calls the block "the default for every repository". `**/CanvasApps/*.msapp` matches only a package directly under a folder named `CanvasApps` (case-sensitive on Linux). The Dataverse Git integration YAML layout keeps the canvas app under `canvasapps/<name>/` (`tests/fixtures/powerops-seed-yaml/*/canvasapps/pops_workitems_2af60/canvasapp.yml`), which that line doesn't match. The line is also more complex than it needs to be.
- Suggested fix: Use `*.msapp` (it matches at any depth and needs no folder name), and pin that line in row 9 (c) instead of `**/CanvasApps/*.msapp`; or say in row 6 that the block covers step 3's XML folder only, and that a Git integration repository is out of scope.
- Resolution: resolved (round 2): the block's first line is `*.msapp` (decisions Q1), which covers the YAML layout's `canvasapps/<name>/`.

### Low

#### R7. Two forms of the observation phrase, and a check that also matches other dates
- Area: 9 Testability
- Finding: The Data preamble, Constraints and step 3 use "observed in one run (2026-11-11)". Row 4's theme clause and row 5's base-theme clause use "(observed in one run, 2026-11-11)". Step 3's check `git grep -n "2026-11-11" -- skills .claude docs/HANDOFF.md` already matches `skills/powerops-plan/references/licensing.md:65` ("Checked on 2026-11-11:"), which is not an observation.
- Suggested fix: Use one form in rows 4 and 5, and make step 3's check `git grep -n "observed in one run" -- skills .claude docs/HANDOFF.md`, with each 2026-11-11 match accounted for.
- Resolution: resolved (round 2): one form, "observed in one run (2026-11-11)", throughout; step 3 greps for that phrase, not the date.

#### R8. Row 3's confirmation route rests on the standard harness's setting, unscoped
- Area: 13 Buildability
- Finding: A4 found **Ask the end user before running** on the standard harness's page and says "Not checked: whether the new harness's tool settings have the same option". Row 3's bullet doesn't say which harness it applies to, and row 1 makes the new harness a choice the reference describes.
- Suggested fix: Row 3: "on the standard harness, plan the question through the tool's own setting ...; for the Microsoft Copilot harness, not checked".
- Resolution: resolved (round 2): row 3 scopes the setting to the standard harness and has a plan on the other harness write an `unproven:` line with a probe.

#### R9. Row 10 traces to no Goal line
- Area: 11 Size
- Finding: The Goal names the five things trial 3 hit; row 10 (the `licensing.md` link label, an M91 and M93 follow-up) is none of them, so removing it fails no Goal line. AGENTS.md asks for the smallest change.
- Suggested fix: Add one clause to the Goal ("and closes the M91 and M93 follow-up on the licensing FAQ's link label"), or move row 10 to a later chore under Won't build.
- Resolution: resolved (round 2): the Goal names the `licensing.md` title and the register and BUILD-PLAN records; Done line 6 checks them.

#### R10. The `analyze` follow-up is "logged" by no step
- Area: 10 Unanswered areas
- Finding: Won't build and decisions Q2 say the `analyze` note for a missing `.msapp` is "logged as a follow-up for after M96", but no step names where it is written (the BUILD-PLAN M95 section of row 12, the chain's follow-up list or an issue). Under the new default that note is the normal case for a folder from Git, so a lost follow-up matters.
- Suggested fix: Row 12 or step 5 names the place and the wording of the follow-up.
- Resolution: resolved (round 2): step 5 writes the follow-up into the done hand-over; Won't build and decisions Q2 say so.

#### R11. Row 7 fixes one of three Action snippets that analyze the folder from Git
- Area: 9 Testability
- Finding: Row 7 adds a sentence under `docs/GITHUB-ACTION.md`'s full example (`path: solutions/ContosoApp`). The first snippet (`path: solutions`, line 16) and the merge-gate snippet (line 58) analyze the folder from Git too, so the canvas checks don't run there either, with no sentence saying so.
- Suggested fix: Put the sentence once, after the first snippet, so it covers all three, or say in row 7 why only the full example needs it.
- Resolution: resolved (round 2): row 7 covers the snippets at `GITHUB-ACTION.md:16` and `:58` with the full example's paragraph.

#### R12. The plan holds URLs
- Area: The skill's rule on URLs, GUIDs, emails and names
- Finding: Assumptions A1 to A4, A6, A7, A10, A11 and A13 give page and repository addresses (without a scheme). They are public Microsoft sources, not tenant values, and no GUID, email or person's name was found in `plan.md` or `decisions.md`.
- Suggested fix: Keep the page titles and dates and drop the addresses, or accept them as public sources in `decisions.md`.
- Resolution: accepted (round 2): the addresses are public Learn and GitHub pages, which every reference file and plan in this repo cites; no tenant value is in the plan.

#### R13. M107's text is checked only at `87a31dd`
- Area: 10 Unanswered areas
- Finding: Won't build leaves `docs/COMMANDS.md`, `docs/USE-WITH-AGENTS.md` and `README.md` to M107, "beside this milestone", and row 7 checked them only at `87a31dd`. M107's new text could tell a reader to run the canvas checks on the folder from Git, which the new default makes wrong.
- Suggested fix: Step 5 (or the merge) greps those three files on `origin/main` for `msapp` and for `analyze` on the source folder, and logs the result.
- Resolution: resolved (round 2): step 5 reruns row 7's two greps over the new `main` at the merge-lock rebase.
