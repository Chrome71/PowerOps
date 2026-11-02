---
status: approved
review: done
signed-off: null
signed-off-hash: null
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

- `powerops skills install` and both plugins ship the five skills, and `powerops-verify` is byte for byte the text evaluated at `b254179`.
- `powerops-review-security` has only its solution pass, its 12 reruns are in `report.md` with the keep rules recomputed, and the person renewed the decision.
- `powerops plan status` lists each report with its state and whether it is stale.
- `powerops-implement` stops by impact and runs both roles in section 4.
- Claude Code and Codex are verified, and Antigravity CLI, Copilot CLI and local Qwen are each recorded as passed, failed or skipped, in the verification record and the Agent support table.
- `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` and `powerops plan status .` are clean, and the work is committed on its own branch.

## Users

- A maker who builds a Power Platform solution with an agent, and gets two reports before promotion.
- The person who decides on promotion and reads those reports.
- A contributor who reruns the set after changing a role.

## Data

Line numbers are those of `b254179`, the commit M38 evaluated. `main` has since moved lines in `README.md`, `docs/USE-WITH-AGENTS.md` and `docs/BUILD-PLAN.md`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | Move `powerops-review-security` and `powerops-verify` from `tests/eval/roles/skills/` to `skills/`. That folder goes away, and the run script and the tests read `skills/`. The embedded-resource glob picks up the new folders. The shipped-skill tests list five names, and `plugins/*/skills/` is regenerated through the recipe "Update goldens" | `skills/`, `tests/eval/roles/run.sh:54-55`, `tests/eval/roles/NOTICE.md:3`, `tests/PowerOps.Cli.Tests/RoleEvalTests.cs:11-13`, `:481`, `tests/PowerOps.Cli.Tests/SkillsCommandTests.cs:9`, `:212`, `plugins/*/skills/` | `SkillsCommandTests`, `RoleEvalTests`, `PluginManifestTests` |
| 2 | `powerops-review-security` keeps only its solution pass. The `pass` input, the plan-pass branches and `security-plan.md` go: the description (line 3), line 10, The request (14-19), the commands (25-28), Check the status (33-35), and lines 74, 96 and 122. Every check, command and report line of the solution pass stays word for word. The skill keeps its name and `security-solution.md`. `run.sh` drops the `security-plan` pass and the words `Pass: solution` from its prompt | `skills/powerops-review-security/SKILL.md`, `tests/eval/roles/run.sh:6`, `:14`, `:34`, `:53`, `:164-165` | `RoleEvalTests`, and the rerun under Automation |
| 3 | `plan status` adds a `reports` list to each plan, in this order when present: `review.md`, `security-solution.md`, `verify.md`. It is empty when there is none. Each entry has `file`, `state` (string or null) and `stale` (boolean or null). `state` comes from the report's `state:` line, and a missing line gives null, so `review.md` shows null. A value outside the table under Automation warns. `stale` compares the report's `plan-hash:` with the plan body's hash, and is null when either is missing. A report's state is never a violation, and neither is a stale report. The tenant-data scan, the link refusals and the existing review checks don't change. `schemaVersion` stays 1, as for other additive fields. The text report prints one line per report | `src/PowerOps.Core/Plans/PlanChecker.cs:9`, `:144-155`, `src/PowerOps.Core/Plans/PlanStatusReport.cs`, `docs/schemas/plan-status.schema.json`, `tests/golden/plan-status.json` | `PlanCheckerTests`, `PlanStatusReportTests`, `PlanCommandTests`, `OutputSchemaTests` |
| 4 | `ForClaude` covers `powerops-review-plan`, `powerops-review-security` and `powerops-verify`, not one skill, so each installed Claude Code copy forks on the top-tier model alias | `src/PowerOps.Cli/SkillsCommand.cs:12`, `:60-73`, `tests/PowerOps.Cli.Tests/PluginManifestTests.cs:132` | `PluginManifestTests`, `SkillsCommandTests` |
| 5 | `powerops-implement` stops by impact: a choice inside the plan goes on, and one that changes scope, data, security or cost stops with one concrete question. The rule against editing the plan body stays | `skills/powerops-implement/SKILL.md:50` | `SkillsCommandTests` |
| 6 | `powerops-implement` section 4. Steps 1 to 4 keep their text. After step 3, start the Verifier in a fresh context by the three routes of `powerops-plan` section 7 step 6, fix each finding inside the plan and rerun, at most three runs in all. A finding that needs a plan change stops under rule 6 of section 3. Then start `powerops-review-security` on that build the same way. Then apply step 4 as it stands. Step 5's message lists the open findings of both reports by number. Neither report gates anything. A build that changes the solution after this runs the solution pass again before `deploy plan` | `skills/powerops-implement/SKILL.md:56-60` | `SkillsCommandTests` |
| 7 | Docs. `docs/USE-WITH-AGENTS.md:7` and `README.md:80` say five skills. `docs/PLANNING-SKILLS.md` gets principle 6, the workflow (`:52`, `:65-66`), the Skills table (`:87-89`), the Review section, the Agent support table and Limits. `docs/COMMANDS.md:120` gets the `reports` list. `docs/HANDOFF.md:17` and `:29` get the two roles. Both `plugin.json` descriptions, and the Codex `longDescription`, name them. `AGENTS.md:12` says what `skills/` holds, and its Rules gain the rule on skill text from Constraints. The docs state the limits under Constraints | as listed | `dotnet test` |
| 8 | `docs/BUILD-PLAN.md` gains an M39 section after M38, with a milestone row (Release `v0.23.0`) and the next build-order items, and its verification record gains a line "M39 (date)" | as listed | `dotnet test` |

## Automation

None of the product's automation changes.

A report's state, as M38 set it. `plan status` knows these four:

| State | Meaning |
|---|---|
| `ready` | The role finished and no High finding is open |
| `blocked` | A High finding is open, or a question only a person can answer |
| `failed` | A required check could not run, and the report says which |
| `dropped` | Nothing for this role to do, and the report says why |

**The rerun.** Removing the plan pass changes an input and a stop rule of the Security skill, so its solution pass reruns. The Verifier's text doesn't change, so it doesn't. The new path in `run.sh` changes the harness, not the skill.

| Pass | Cases | Runs per client |
|---|---|---|
| Security reviewer, solution pass | The 6 solution cases | 6 |

That is 12 runs on Claude Code and Codex, through `run.sh`, with M38's model and effort where the client still offers them, and with the client versions recorded. Scoring and settling follow M38: neutral copies, two blind scorers, one from each family, the same key and the same replacement rule. The keep rules don't change:

- It finds at least 15% of its own flaws marked `baseline: not-shown`, which are 5 per client. M38 measured 10 of 10.
- At most one false High across its clean-case runs, pooled. Those are now the 4 runs on solution-02 and solution-06, and M38 found 1 false High there.

Passing a floor permits keeping and never requires it, so step 4 stops for the person.

Expected use, from M38's runs of this pass: Claude 1.8M tokens, 18 minutes, $5.29 at list price. Codex 1.6M tokens, 10 minutes. Scoring comes on top.

**The client checks.** Each client works in its own temp folder outside the repo, on copies of one plan case and one solution case from `tests/eval/roles/cases/`, with the skills of this build installed by `powerops skills install`.

| Client | How it runs | Result |
|---|---|---|
| Claude Code | `claude` | Must pass |
| Codex | `codex` | Must pass |
| Antigravity CLI | `agy` in print mode | Passed, failed or skipped |
| Copilot CLI | `copilot`, after the person installs it and signs in | Passed, failed or skipped |
| Local Qwen | Through Pi, on the model the person loaded | Passed, failed or skipped |

Each record gives the client and model version, skill discovery, the question fallback, the fresh-session route, report creation and sign-off. In a mode with no person to answer, `powerops-plan` says so and stops (principle 5), and the record says that was the fallback seen. Failed means tried and wrong. Skipped means unavailable, with the reason.

## Screens

None.

## Environments and route

None. No step reads or writes a tenant, and no step runs `pac`, `az`, `plan facts` or any `--apply`. The rerun holds to this by `run.sh`'s setup. The client checks work on fake cases in a temp folder, and their prompts claim no `pac` sign-in.

## Solution and publisher

None.

## Constraints

- **Only the kept roles ship.** The Security plan pass and the second-family advice don't ship in any form, and `powerops-plan` and `powerops-review-plan` don't change.
- **Skill text after the evaluation:** a typo or formatting edit needs only a recorded comparison with the evaluated text. A change to a check, command, stop rule, input or report line reruns that pass on all its cases and both families, recomputes the keep rules and renews the person's decision before it ships. This rule outlives the plan, so it goes in `AGENTS.md`.
- **Reduced tools per role:** each skill lists its commands. That is instruction, not enforcement, and the docs say so.
- **Scope fixed at approval:** the signed plan hash already does this, and the Verifier reports anything built outside the plan.
- **What the reports can claim.** The docs state M38's limits: its set scored at the ceiling, so it says little about harder flaws. Most security points never show in an export. Claude's runs made 3 tenant claims against the skills' rule. No case held a zip, so the Verifier's `<zip>.findings.json` branch ran on no case. This is why the reports gate nothing.
- **Named clients only.** Any client outside the five is a stated limit, and "any client" stays an aim, not a claim.
- **Microsoft's clients.** On 2026-11-01 the README of microsoft/power-platform-skills named Copilot CLI and Claude Code, 10 of its 25 agent files named a Claude tier, and microsoft/power-cat-skills named Microsoft Scout and Copilot CLI. A client outside those may have no build skills, so the two new roles work with `powerops` commands alone, and the docs say so.
- **Release:** M39 is a `feat:`, so its label is the next minor, v0.23.0. No step edits a version, as AGENTS.md says.
- The rerun's cost and time are recorded, and any planted value stays out of `report.md`, as in M38.

## Assumptions

- M38 is merged before M39 starts. Its branch adds `tests/eval/`, which rows 1 and 2 change.
- Environment facts were not read: this plan changes this repo and targets no environment.
- `run.sh`'s solution cases, key and scoring work unchanged for the narrowed skill, because its solution pass already read the plan files, the solution and the settings.
- The Claude Code copy forks where the evaluated copy ran in a new session. Both give a fresh context, and the client check covers the fork.
- The person installs Copilot CLI and signs in, and loads Qwen, before step 8. Without either, that client is recorded as skipped.

## Open questions

## Questions for your admin

## Steps

1. Start `feat/m39-ship-roles` from `main`, after M38 has merged.
2. Rows 1 and 2. Compare both skills with the text at `b254179`, and record in `report.md` that `powerops-verify` is identical and that `powerops-review-security` differs only as row 2 says. Run the checks, then commit.
3. Freeze that commit, with a clean build and test on a fresh checkout. Run the 12 runs and score them. Record the following in `docs/plans/m39-ship-roles/report.md`: the commit, client versions, model and effort, each count and denominator, every replaced run with its reason, every wrong `state` and stray write, tenant claims, cost and time.
4. Stop and show the person `report.md`. The person renews or withdraws the decision, and it is logged in `decisions.md`. No later step starts without it. A withdrawal changes this plan, so go back to `powerops-plan`.
5. Rows 3 and 4, with their tests and the golden. Commit.
6. Rows 5 and 6, then regenerate the plugin copies through "Update goldens". Commit.
7. Rows 7 and 8. Commit.
8. Run the client checks. A failure on Claude Code or Codex is fixed inside the plan. On another client it is recorded. Record the results in the verification record and the Agent support table, then commit.
9. Read the whole diff. Run `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan. Commit by path. Push or open a pull request only when the person asks.
