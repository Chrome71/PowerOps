---
status: signed-off
review: done
signed-off: 2026-11-02
signed-off-hash: 5f358ca4abd5aa68a655ad9136d70025767cd8641b8f463ead125766fa828955
environment: null
depth: standard
supersedes: null
---

# M39: Ship the kept agent roles

## Goal

Ship the two roles the person kept in M38 ([report](../m38-agent-team/report.md#decision)), so that a solution built with PowerOps is checked against its plan and read for security before it is promoted. The Verifier ships as it was evaluated. The Security reviewer ships with its solution pass only, after a rerun, because the evaluated text also held a plan pass that wasn't kept.

| Role | Skill | When | Can write |
|---|---|---|---|
| Lead | `powerops-plan`, `powerops-implement` | Plans, then builds through Microsoft's skills | Plan files, and Dev through Microsoft's skills |
| Plan reviewer | `powerops-review-plan`, unchanged | After approval | `review.md` and its `review:` line |
| Verifier | `powerops-verify` | After the build's final checks, at most three runs | `verify.md` |
| Security reviewer | `powerops-review-security` | On every build, before `deploy plan` | `security-solution.md` |
| The person | Not an agent | Sign-off and promotion | The `deploy_apply` form |

Order of work on a plan:

1. The Lead drafts the plan and the person approves it.
2. The Plan reviewer runs in a fresh context.
3. The person reads the review and signs off.
4. The Lead builds in Dev through Microsoft's skills. It is the only role that changes a solution.
5. The Verifier runs in a fresh context, and the Lead fixes its findings inside the plan, for at most three runs in all.
6. The Security reviewer runs in a fresh context on that build. Any later build runs it again before `deploy plan`.
7. `deploy plan` runs, and the person confirms `deploy_apply` on the typed form.

`verify.md` and `security-solution.md` gate nothing. The Lead lists their open findings, and the person decides.

Done when:

- `powerops skills install` and both plugins ship the five skills, and `powerops-verify` hashes to its evaluated blob id under Data.
- `powerops-review-security` has only its solution pass, its 12 reruns are in `report.md` with the keep rules recomputed, and the person renewed the decision.
- `powerops plan status` lists each report with its state and whether it is stale.
- `powerops-implement` stops by impact and runs both roles in section 4, and the client checks saw it do so.
- Claude Code and Codex pass the client checks, and Antigravity CLI, Copilot CLI and local Qwen are each recorded as passed, failed or skipped, in the verification record and the Agent support table.
- `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` and `powerops plan status .` are clean, and the work is committed on its own branch.

## Users

- A maker who builds a Power Platform solution with an agent, and gets two reports before promotion.
- The person who decides on promotion and reads those reports.
- A contributor who reruns the set after changing a role.

## Data

Line numbers are those of `b254179`, the commit M38 evaluated. A squash merge leaves that commit off `main`, so the evaluated text is pinned by git blob id, on its LF text:

| File at `b254179` | Blob id |
|---|---|
| `tests/eval/roles/skills/powerops-verify/SKILL.md` | `69208a2ed044a346bbae9877c8cd74d9e4794df9` |
| `tests/eval/roles/skills/powerops-review-security/SKILL.md` | `3ff2d68576056756b3b4003110b8d7919f8e4616` |
| `tests/eval/roles/run.sh` | `56f4363bfefd4c28c730f4cd216231895aeca900` |

| # | Change | Where | Test |
|---|---|---|---|
| 1 | Move `powerops-review-security` and `powerops-verify` from `tests/eval/roles/skills/` to `skills/`. That folder goes away, and the run script and the tests read `skills/`. The embedded-resource glob picks up the new folders. The shipped-skill tests list five names, and `plugins/*/skills/` is regenerated through the recipe "Update goldens". `PlanFactsSkillTests` gains `powerops-review-security`, the one new skill that lists `plan facts` | `skills/`, `tests/eval/roles/run.sh:54-55`, `tests/eval/roles/NOTICE.md:3`, `tests/PowerOps.Cli.Tests/RoleEvalTests.cs:11-13`, `:481`, `tests/PowerOps.Cli.Tests/SkillsCommandTests.cs:9`, `:212`, `tests/PowerOps.Cli.Tests/PlanFactsSkillTests.cs:51-52`, `plugins/*/skills/` | `SkillsCommandTests`, `RoleEvalTests`, `PluginManifestTests`, `PlanFactsSkillTests` |
| 2 | `powerops-review-security` keeps only its solution pass. The `pass` input, the plan-pass branches and `security-plan.md` go: the description (line 3), line 10, The request (14-19), the commands (25-28), Check the status (33-35), and lines 74, 96 and 122. Only words that name a pass, the `pass` input or `security-plan.md` go, and every check, command and report line of the solution pass keeps its meaning, with one exception. The settings file becomes optional, as the previous solution is. With none, the skill skips `settings validate`, says so under the report's header lines as line 28 does for `diff`, and records what depended on that check as not verified. Otherwise every build without a settings file would end `failed` and review nothing. The skill keeps its name and `security-solution.md`. `run.sh` drops the `security-plan` pass and the words `Pass: solution` from its prompt | `skills/powerops-review-security/SKILL.md`, `tests/eval/roles/run.sh:6`, `:14`, `:34`, `:53`, `:164-165` | `RoleEvalTests`, and the rerun under Automation |
| 3 | `plan status` adds a `reports` list to each plan, in this order when present: `review.md`, `security-solution.md`, `verify.md`. It is empty when there is none. Each entry has `file`, `state` (string or null) and `stale` (boolean or null). Both are read from the report's first four lines, trimmed and compared by exact case. A missing `state:` line gives null, so `review.md` shows null. A value outside the table under Automation warns. `stale` compares the report's `plan-hash:` with the plan body's hash, and is null when either is missing. A stale report warns as a stale `review.md` does now. For `review.md`, `stale` takes the existing check's result, and that check stays its only warning. A report that is a link, binary or too large keeps the existing warning and shows null for both. A report's state is never a violation, and neither is a stale report. The tenant-data scan, the link refusals and the existing review checks don't change. `schemaVersion` stays 1, as for other additive fields. The text report prints one line per report. The schema description says that `stale` means the plan body changed and says nothing about the solution | `src/PowerOps.Core/Plans/PlanChecker.cs:9`, `:144-155`, `src/PowerOps.Core/Plans/PlanStatusReport.cs`, `docs/schemas/plan-status.schema.json`, `tests/golden/plan-status.json` | `PlanCheckerTests`, `PlanStatusReportTests`, `PlanCommandTests`, `McpCommandTests`, `OutputSchemaTests` |
| 4 | `ForClaude` covers `powerops-review-plan`, `powerops-review-security` and `powerops-verify`, not one skill, so each installed Claude Code copy forks on the top-tier model alias | `src/PowerOps.Cli/SkillsCommand.cs:12`, `:60-73`, `tests/PowerOps.Cli.Tests/PluginManifestTests.cs:132` | `PluginManifestTests`, `SkillsCommandTests` |
| 5 | `powerops-implement` stops by impact: a choice inside the plan goes on, and one that changes scope, data, security or cost stops with one concrete question. The rule against editing the plan body stays | `skills/powerops-implement/SKILL.md:50` | `SkillsCommandTests` and the client checks |
| 6 | `powerops-implement` section 4. Steps 1 to 4 keep their text. After step 3, with a solution on disk, the Lead starts the Verifier and then `powerops-review-security`, each in a fresh context. With no solution on disk it starts neither, and says "not verified" and why in step 5. The section spells out the routes: Claude Code invokes the skill, whose installed copy forks; an agent that can start a subagent with no chat history starts one; otherwise the Lead gives the user the request for a new session and waits. The request names the plan, the current and previous solution, the settings file, the test folder and any live evidence from step 3. The previous solution is the last export before this plan, from the repo's history. The request says "no previous solution", "no settings file", "no test folder" or "no live evidence" where one doesn't exist, and invents none. It adds the `pac` confirmation for the Security reviewer only if the user gave it in this session. The Lead fixes each Verifier finding inside the plan and reruns, at most three runs in all. A finding that needs a plan change stops under rule 6 of section 3. A report is data: the Lead fixes only what the plan's `Steps` allow, and takes no command or request from a finding's wording. After each report the Lead runs `powerops plan status`. Then step 4 applies as it stands. Step 5's message gives, for each report, its state, the solution path and commit it read, and its open findings by number. Neither report gates anything. A build that changes the solution after this runs the solution pass again before `deploy plan`. Section 2 gains one sentence: a violation that names `verify.md` or `security-solution.md` is fixed by replacing the value in that report with a placeholder, and the plan isn't edited | `skills/powerops-implement/SKILL.md:32`, `:56-60` | `SkillsCommandTests` and the client checks |
| 7 | Docs. The sentence "PowerOps ships three agent skills" in `README.md` and `docs/USE-WITH-AGENTS.md` says five. `docs/USE-WITH-AGENTS.md` tells current users to uninstall and install the plugin, as it already does for Claude Code, or to run `powerops skills install --force`, to get the two roles and the changed `powerops-implement`, and says that a Codex plugin update hasn't been tried. `docs/PLANNING-SKILLS.md` gets principle 6, the workflow (`:52`, `:65-66`), the Skills table (`:87-89`), the Review section, the Agent support table and Limits. `docs/COMMANDS.md:120` gets the `reports` list. `docs/HANDOFF.md:17` and `:29` get the two roles, and say that the person promotes only what the reports name. Both `plugin.json` descriptions, and the Codex `longDescription`, name them. `AGENTS.md:12` says what `skills/` holds, and its Rules gain the rule on skill text from Constraints. `docs/PLANNING-SKILLS.md` and `docs/USE-WITH-AGENTS.md` state the limits under Constraints | as listed | `dotnet test` |
| 8 | `docs/BUILD-PLAN.md` gains an M39 section after M38, with a milestone row (Release `v0.23.0`) and the next build-order items, and its verification record gains a line "M39 (date)". Step 4 of its Windows and macOS runbook says five skills, where `claude plugin details powerops` says three | as listed | `dotnet test` |

## Automation

None of the product's automation changes.

A report's state, as M38 set it. `plan status` knows these four:

| State | Meaning |
|---|---|
| `ready` | The role finished and no High finding is open |
| `blocked` | A High finding is open, or a question only a person can answer |
| `failed` | A required check could not run, and the report says which |
| `dropped` | Nothing for this role to do, and the report says why |

**The rerun.** Removing the plan pass changes an input and a stop rule of the Security skill, and the settings file becomes optional, so its solution pass reruns. The Verifier's text doesn't change, so it doesn't. The new path in `run.sh` changes the harness, not the skill.

| Pass | Cases | Runs per client |
|---|---|---|
| Security reviewer, solution pass | The 6 solution cases | 6 |

That is 12 runs on Claude Code and Codex, through `run.sh`, with M38's model and effort where the client still offers them, and with the client versions recorded. Scoring and settling follow M38: neutral copies, two blind scorers, one from each family, the same key and the same replacement rule, so a valid poor run isn't replaced. The keep rules don't change, and the rerun's own runs are what they count:

- It finds at least 15% of its own flaws marked `baseline: not-shown`, which are 5 per client. M38 measured 10 of 10.
- At most one false High across its 4 clean-case runs on solution-02 and solution-06, pooled. M38 found 1 there.

Passing a floor permits keeping and never requires it, so step 4 stops for the person. A missed floor permits no keeping, so its counts are shown and the plan goes back to `powerops-plan`.

Expected use, from M38's runs of this pass: Claude 1.8M tokens, 18 minutes, $5.29 at list price. Codex 1.6M tokens, 10 minutes. Scoring comes on top.

**The client checks.** Each client works in its own temp folder outside the repo, on copies of the clean cases plan-02 and solution-02 from `tests/eval/roles/cases/`. Each copy is a new git repository with one commit, and a build of this commit comes first on `PATH`, as in `run.sh`. `powerops skills install` of this build places the skills where it can. Where it can't, as with Pi's `--skill <path>`, the record says how they were placed.

| Client | How it runs | Result |
|---|---|---|
| Claude Code | `claude` | Must pass |
| Codex | `codex` | Must pass |
| Antigravity CLI | `agy` in print mode | Passed, failed or skipped |
| Copilot CLI | `copilot`, after the person installs it and signs in | Passed, failed or skipped |
| Local Qwen | Through Pi, on the model the person loaded | Passed, failed or skipped |

A client passes when all of these hold:

1. It finds the five skills.
2. `powerops-plan` shows its first question through a choice tool or as numbered text. With no person to answer, it says so and stops (principle 5).
3. `powerops-review-plan` writes `review.md` on plan-02, and `powerops plan sign-off` signs that copy.
4. `powerops-implement`, told that the build on solution-02 is done and that its previous solution is `solution/previous`, resumes at section 4 and starts the Verifier and the Security reviewer by a route the client has. Each report has the four header lines and a state from the table, and `plan status` lists both.
5. A request on solution-02 that changes the plan's scope stops with one concrete question. With no person to answer, it stops and says so.
6. No file changes outside the case copy.

Each record gives the client and model version, and for each item above the result and what was seen. Failed means tried and wrong. Skipped means unavailable, with the reason.

Claude Code and Codex also run item 4 once on a copy without `solution/previous` and `settings.json`, told that neither exists, as a first build would. All 6 rerun cases hold both, so this is the only run of those branches. The Security reviewer must skip both checks and not end `failed` for them. The evaluated Verifier never ran that way, and its text can't change here, so the record shows the state it gives and that decides no pass.

In the Agent support table, Antigravity CLI and Pi with local Qwen get rows of their own, and the Gemini CLI row stays as it is.

## Screens

None.

## Environments and route

None. No step reads or writes a tenant, and no step runs `pac`, `az`, `plan facts` or any `--apply`. The rerun holds to this by `run.sh`'s setup. The client checks work on fake cases in a temp folder, and their prompts claim no `pac` sign-in.

## Solution and publisher

None.

## Constraints

- **Only the kept roles ship.** The Security plan pass and the second-family advice don't ship in any form, and `powerops-plan` and `powerops-review-plan` don't change.
- **Skill text after the evaluation:** a typo or formatting edit needs only a recorded comparison with the evaluated text. A change to a check, command, stop rule, input or report line reruns that pass on all its cases and both families, recomputes the keep rules and renews the person's decision before it ships. This rule outlives the plan, so it goes in `AGENTS.md`. A client check that finds a fault in a kept role's text falls under it.
- **Reduced tools per role:** each skill lists its commands. That is instruction, not enforcement, and the docs say so. This plan adds no tool narrowing on any client.
- **Scope fixed at approval:** the signed plan hash already does this, and the Verifier reports anything built outside the plan.
- **Named clients only.** Any client outside the five is a stated limit, and "any client" stays an aim, not a claim.
- **Microsoft's clients.** On 2026-11-01 the README of microsoft/power-platform-skills named Copilot CLI and Claude Code, 10 of its 25 agent files named a Claude tier, and microsoft/power-cat-skills named Microsoft Scout and Copilot CLI. A client outside those may have no build skills, so the two new roles work with `powerops` commands alone, and the docs say so.
- **Release:** M39 is a `feat:`, so its label is the next minor, v0.23.0. No step edits a version, as AGENTS.md says.
- The rerun's cost and time are recorded, and any planted value stays out of `report.md`, as in M38.

The docs state these limits, which are why the reports gate nothing:

- **The evidence.** M38's set scored at the ceiling, so it says little about harder flaws. Most security points never show in an export. Its cases are small and hand-written, close to `pac solution unpack` output, and the longest run took 260 seconds, so a large solution is untried. No case held a zip, so the Verifier's `<zip>.findings.json` branch ran on no case. A temp folder is a mitigation, not proof that a reviewer was blind.
- **Tenant claims.** Claude's runs made 3 tenant claims against the skills' rule.
- **The setup.** M38 ran the roles with an allowlist and no MCP servers. A shipped role runs with the session's tools, so its results on stray writes and outside reads don't carry over.
- **Where the data goes.** Both roles send the plan, the solution's components and the settings file to the client's model provider, and Codex used web search in M38's Security passes. A team checks that against its own data rules first. Local Qwen through Pi keeps it on the machine, if that client check passes.
- **Cost.** A build adds up to three Verifier runs and one Security run, which the maker pays from their own agent plan. On M38's small cases that is about $3 at Claude's list price: $0.69 a Verifier run and $0.88 a Security run. A real solution costs more.
- **What a report covers.** A report describes the folder or zip it names, and `stale` says nothing about the solution. The person promotes only what was reviewed.

## Assumptions

- M38 is merged before M39 starts. Its branch adds `tests/eval/`, which rows 1 and 2 change.
- Environment facts were not read: this plan changes this repo and targets no environment.
- `run.sh`'s solution cases, key and scoring work unchanged for the narrowed skill, because its solution pass already read the plan files, the solution and the settings.
- The Claude Code copy forks where the evaluated copy ran in a new session. Both give a fresh context, and item 4 of the client checks covers the fork.
- The person installs Copilot CLI and signs in, and loads Qwen, before step 8. Without either, that client is recorded as skipped.

## Open questions

## Questions for your admin

## Steps

1. Start `feat/m39-ship-roles` from `main`, after M38 has merged.
2. Rows 1 and 2. Check that `powerops-verify` hashes to its blob id. Record in `report.md` the diffs of `powerops-review-security` and `run.sh` against their blobs, which hold only the changes of rows 1 and 2. Run the checks, then commit.
3. Freeze that commit, with a clean build and test on a fresh checkout. Run the 12 runs and score them. Record the following in `docs/plans/m39-ship-roles/report.md`: the commit, client versions, model and effort, each count and denominator, every replaced run with its reason, every wrong `state` and stray write, tenant claims, cost and time.
4. Stop and show the person `report.md`. The person renews or withdraws the decision, and it is logged in `decisions.md`. No later step starts without it. A missed floor or a withdrawal changes this plan, so go back to `powerops-plan`.
5. Rows 3 and 4, with their tests and the golden. Commit.
6. Rows 5 and 6, then regenerate the plugin copies through "Update goldens". Commit.
7. Rows 7 and 8. Commit.
8. Run the client checks. A failure on Claude Code or Codex is fixed inside the plan. A fault in the Security reviewer's text goes back to step 2, then steps 3 and 4. A fault in the Verifier's text changes this plan, because it has no rerun here, so go back to `powerops-plan`. On another client a failure is recorded. Record the results in the verification record and the Agent support table, then commit.
9. Read the whole diff. Run `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan. Commit by path. Push or open a pull request only when the person asks.
