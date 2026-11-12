plan-hash: 31c24059be0620e42b72b53cb09cb9750a9e2090556519956adc1037b1efc2a5
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: the request did not say that `pac` is signed in to the plan's environment, and the plan's `environment:` is null (this milestone changes PowerOps itself and calls no tenant; tenant: offline, no `pac` or `az` call was made).

This review replaces the review of the round-2 plan (plan-hash `a7fa884`), which `plan status` reported stale. Its findings R1 to R9 were read against the current plan (`47c1c43`): R1 to R4 and R6 to R8 are closed by it, R9 is raised again below as the skill requires, and R5 is closed for `docs/HANDOFF.md` but not for `powerops-implement` (R3 below). The numbers below are new.

`plan status` was run with the worktree's build (`dotnet src/PowerOps.Cli/bin/Debug/net10.0/powerops.dll`; `src/` is unchanged from `87a31dd` to `47c1c43`): status `approved`, review `pending`, 0 open questions, 0 admin questions, no violation; one warning, the stale review this file replaces.

Tools (item 12): `dotnet`, `git`, `grep`, `gh` and `powerops` were found with `command -v`. `reports/leakscan.sh`, `reports/ci-watch.sh` and `reports/trial-3/chain/` exist in the main checkout, as step 5 says. The `fable-reviewer` agent, `powerops-researcher` and the "Update goldens" recipe (`AGENTS.md`) exist. No step names an MCP tool or a Microsoft skill.

Read besides the plan: `docs/HANDOFF.md` (`:18-26`, steps 3 to 8); `docs/GITHUB-ACTION.md` (`:16`, `:23`, `:29-40`, `:58`, `:76-80`, `:126-182`); `action.yml` (`path` input) and `action/analyze.sh`; `docs/examples/README.md` (`:155`, `:189`), `ephemeral-pr.yml` and `promote-dev-to-test.yml`; `docs/COMMANDS.md:98-101`, `:227`; `skills/powerops-implement/SKILL.md` sections 4 and 5; the four product references' headings, Sources blocks and link labels; `.claude/agents/powerops-copilot-studio.md` (`:9`, `:28-33`, `:43`, `:60`) and `powerops-workflow-logic.md` (`:32`, `:56`); `docs/plans/m94-build-autonomy/plan.md:40`; `tests/PowerOps.Cli.Tests/PluginAgents.cs` (its `Replace` lines), `HandoffDocTests.cs` (`Fence`, the product-table and step checks), `DocsDriftTests.cs` (`Pages()`), `SkillsCommandTests.cs` (`:430-441`, `:548-555`, `:850-889`); `tests/fixtures/powerops-seed-yaml/*/canvasapps/` and `tests/fixtures/NOTICE.md` (`:28`, `:40-47`); `src/PowerOps.Core/Readers/SolutionParser.cs:163`; `docs/ecosystem/changes.md` (last id ECO-0019); `docs/BUILD-PLAN.md` M94. Claims that hold at `87a31dd`: every line the rows quote from `HANDOFF.md`, `GITHUB-ACTION.md`, `examples/README.md`, the specialist files and `powerops-implement`; the three `licensing.md` uses of the stale label; the keep-guards of row 9 (d); no test pins the HANDOFF step 3 wording that goes; none of the new pin phrases exists yet, so each pin starts red; the plugin ships `build-standard.md`, and no `PluginAgents` replacement touches the lines rows 2 and 8 edit; `diff` reads the `.meta.xml`. The Microsoft facts of A1 to A14 and the GitHub heads of A11 and A12 were not re-checked (offline review).

Not checked: how GitHub code scanning shows SARIF results of row 7's second use, whose `path` is a folder under the runner's temp folder outside the repository (`action.yml` accepts any path; the locations may not map to repository files). No finding.

Not applicable to a PowerOps milestone with no Power Platform solution: item 1 (solution and publisher), 2 (connection references and environment variables), 3 (licences: the plan names connectors, agents and reports only as reference text, and builds none), 4 (DLP), 6 (managed or unmanaged), 7 (delegation) and 8 (security roles). Item 5's route is Git: a branch, local commits, the chain's merge lock; nothing goes to a tenant. Item 11 has no table, list, flow or screen; each row maps to a Goal line, and `Won't build` is present.

## Findings

### High

No findings.

### Medium

#### R1. `*.msapp` is said to cover "any other layout", but the Git integration layout in this repo holds no `.msapp`
- Area: 13 Buildability; 10 Unanswered areas
- Finding: Row 6 (and decisions Q1's reason) says `*.msapp` "covers step 3's XML unpack (`CanvasApps/`) and any other layout (Learn keeps the YAML layout's package under `canvasapps/<name>/`, A14)", and makes the out-of-Git default hold "for every repository". The repo's own Dataverse Git integration export (`tests/fixtures/powerops-seed-yaml/*/canvasapps/pops_workitems_2af60/`, `NOTICE.md:40-44`) holds the canvas app as an unpacked `<name>_DocumentUri/` folder (`Src/*.pa.yaml`, and before the scrub `Controls/*.json`, `References/*.json`, `Properties.json`, `Resources/PublishInfo.json`), not an `.msapp`, and `docs/COMMANDS.md:100` reads that layout's formulas from the folder. In such a repository `*.msapp` matches nothing, the commits are made by the service rather than by a local `git add`, and neither the plan nor a source says whether those loose files cache the environment's address or whether Git integration honours `.gitignore`. No `unproven:` line covers either.
- Suggested fix: Scope the default, its `*.msapp` sentence and decisions Q1's reason to the XML unpack of step 3 (and any `.msapp` a person exports), drop "any other layout", and say for a repository Dataverse Git integration writes what is not checked; or add an `unproven:` line with a probe (a scrub-list read of the Git integration canvas folder) before the sentence is written.
- Resolution: resolved (round 4): row 6 says the default covers step 3's export route only, names the Git integration layout (an unpacked `<name>_DocumentUri/` folder, commits by the service) as not reached and unproven, and logs a follow-up (decisions Q1).

#### R2. Step 7's "runs on the pull request" loses its zips under the new default
- Area: 5 Route; 13 Buildability
- Finding: `HANDOFF.md` step 7 runs `powerops deploy plan deploy --target <test> --save deploy/powerops-plan.json` on the pull request (the Action's `deploy-folder` and `target`), and step 8 applies `deploy/powerops-plan.json` with the zips of its folder. Under row 6's `.gitignore` (`/deploy/*.zip`), the CI checkout has no zips, so the PR plan has nothing to plan, and a plan file CI makes over a fresh export names zips that aren't the reviewed ones. Row 6's added sentence answers only "the import that checks the reports" (where it runs); it doesn't say that `deploy plan --save` must run there too, nor what the PR's plan in CI then plans. Row 9 (e) pins only `require-reviews: 'false'`.
- Suggested fix: In row 6's step 7 sentence, say that on the review-linked route the plan file is made where the reviewed zip is (the deploy folder of step 3), next to the import, and that a plan on the pull request needs an export in the job, as `promote-dev-to-test.yml` does, and is then the CI route with `require-reviews: 'false'`; pin the phrase in row 9 (e).
- Resolution: resolved (round 4): step 7's sentence says the plan file is made, and the import runs, where the reviewed zip is, and that a plan in CI needs an export in the job.

### Low

#### R3. `powerops-implement`'s new step 1 sentence and the last Assumptions line still overstate "only from the `.msapp`"
- Area: 9 Testability; the last Assumptions line
- Finding: The round-2 R5 was fixed in `HANDOFF.md` ("in the XML layout of step 3"), but row 6 still gives `powerops-implement` section 4 step 1 "a canvas app's formulas are checked on the exported zip, because the folder in Git holds no `.msapp`", and the last Assumptions line says `analyze` reads formulas "only from its `.msapp`". `docs/COMMANDS.md:100` reads the YAML layout's formulas from the folder, a layout `powerops-implement` meets.
- Suggested fix: Scope both to the XML unpack ("in an XML unpack, formulas are read only from the `.msapp`, which the folder in Git doesn't hold").
- Resolution: resolved (round 4): the skill sentence and the Assumptions line are limited to the XML layout.

#### R4. The stand-alone `.gitignore` sentence has no way out for a repository that keeps deploy zips on purpose
- Area: 9 Testability
- Finding: Row 6 has `powerops-implement` add each missing line, `/deploy/*.zip` included, and treat a tracked zip as one to report with `git rm --cached`. Row 7 makes the ephemeral recipe, which commits zips in `deploy/`, the exception whose users leave that line out. An agent in such a repository would add the line back and report the recipe's zips as a leak; the skill ships without `docs/examples/README.md`, so it can't know.
- Suggested fix: When `git ls-files` shows zips already tracked in `deploy/`, tell the person and add no zip line; add the zip lines only when none is tracked.
- Resolution: resolved (round 4): the skill sentence keeps a repository's deliberate choice to commit deploy zips (the ephemeral recipe) and adds no zip line there; only an `.msapp` or settings file already tracked is reported.

#### R5. Two wording slips
- Area: 10 Unanswered areas (clarity)
- Finding: Decisions Q4 says "its checklist item 7 (...) is right and names no route, and line 43 (M94's "Won't build") is re-read and holds", as if they were two lines; line 43 of `.claude/agents/powerops-copilot-studio.md` is checklist item 7 (`docs/plans/m94-build-autonomy/plan.md:40`). Step 2's file list has no comma between `HandoffDocTests.cs` and `DocsDriftTests.cs`.
- Suggested fix: In Q4, say "checklist item 7 (line 43, which M94 kept)"; add the comma.
- Resolution: resolved (round 4): decisions Q4 names item 7 as line 43; the comma is added.

#### R6. The plan holds URLs
- Area: The skill's rule on URLs, GUIDs, emails and names
- Finding: Assumptions A1 to A4, A6, A7, A10, A11, A13 and A14 give page and repository addresses (without a scheme). They are public Microsoft sources, not tenant values; no GUID, email or person's name was found in `plan.md` or `decisions.md`.
- Suggested fix: Keep the page titles and dates and drop the addresses, or accept them again as public sources (the round-1 R12 and round-2 R9 were accepted on that ground).
- Resolution: accepted (round 4): public Learn and GitHub addresses only; no tenant value.
