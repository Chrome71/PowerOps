# M39 outline: build the kept roles

This isn't a plan and nothing here is signed off. It keeps the design M38 was split from, so that step 10 of M38 can write a real M39 plan from it. That plan names only the roles the person kept, with no conditional rows. Line numbers are those of `fb7e0cc`.

## Order of work on a plan

1. The Lead drafts the plan and the person approves it.
2. The Plan reviewer and the Security reviewer's plan pass run in fresh contexts. Each writes only its own report, and only `powerops-review-plan` sets `review: done`, so they can run at the same time.
3. The person reads both reports and signs off.
4. The Lead builds in Dev through Microsoft's skills. It is the only role that changes a solution.
5. The Verifier runs in a fresh context, and the Lead fixes its findings inside the plan, for at most three runs in all.
6. The Security reviewer's solution pass runs before promotion.
7. `deploy plan` runs, and the person confirms `deploy_apply` on the typed form.

## Changes

| # | Change | Where |
|---|---|---|
| 1 | Move the kept draft skills from `tests/eval/roles/skills/` to `skills/`, then update the shipped-skill tests and regenerate the plugin copies through the recipe "Update goldens" | `skills/`, `tests/PowerOps.Cli.Tests/SkillsCommandTests.cs:9`, `:212`, `plugins/*/skills/` |
| 2 | `powerops-review-plan` gains line 4, `state:` | `skills/powerops-review-plan/SKILL.md:49-57`, `docs/PLANNING-SKILLS.md:122` |
| 3 | `plan status` adds a `reports` list, in this order when present: `review.md`, `security-plan.md`, `security-solution.md`, `verify.md`. Each entry has `file`, `state` (string or null) and `stale` (boolean or null). `stale` compares the plan body only. A missing state is null, and an unknown state warns. A report's state is never a violation. The tenant-data scan, the link refusals and the existing review checks don't change. `schemaVersion` stays 1, as for other additive fields | `src/PowerOps.Core/Plans/PlanChecker.cs:144-155`, `docs/schemas/plan-status.schema.json`, the `plan status` text report and its golden |
| 4 | `ForClaude` covers `powerops-review-plan` and each kept review skill, not one | `src/PowerOps.Cli/SkillsCommand.cs:12`, `:60-73`, `tests/PowerOps.Cli.Tests/PluginManifestTests.cs:132` |
| 5 | `powerops-plan` section 7 step 6 starts both reviewers, and section 9 shows the header lines of each report, not "the first three lines". Model wording becomes a need, best effort: the strongest model available, and another family for one reviewer if M38 kept that advice. Lines 149 and 150 move under "Per agent" | `skills/powerops-plan/SKILL.md:148-153`, `:176-178` |
| 6 | `powerops-implement` stops by impact: a choice inside the plan goes on, and one that changes scope, data, security or cost stops with one concrete question | `skills/powerops-implement/SKILL.md:50` |
| 7 | `powerops-implement` section 4: step 4 is unchanged. After step 3, run the Verifier, fix each finding inside the plan and rerun, at most three runs in all. Then apply step 4 as it stands, and list the Verifier's open findings by number in step 5's message. `verify.md` gates nothing, and neither does any other report of a new role. The solution pass runs before `deploy plan` | `skills/powerops-implement/SKILL.md:56-60` |
| 8 | Docs: `docs/USE-WITH-AGENTS.md`, `docs/PLANNING-SKILLS.md` principle 6, the Review section and `:122`, `docs/COMMANDS.md`, the three-skill line in `README.md`, `docs/HANDOFF.md:16`, both `plugin.json` descriptions, `docs/BUILD-PLAN.md` | as listed |

## Rules to carry over

- **Reduced tools per role:** each skill lists its commands. That is instruction, not enforcement, and the docs say so. A client that runs reviewers over MCP could enforce it later.
- **Scope fixed at approval:** the signed plan hash already does this, and the Verifier reports anything built outside the plan.
- **Skill text after the evaluation:** a typo or formatting edit needs only a recorded comparison with the evaluated text. A change to a check, command, stop rule, input or report line reruns that pass on all its cases and both families, recomputes the keep rules and renews the person's decision before it ships.
- **Done means named clients:** verified on Claude Code and Codex, and recorded as passed, failed or skipped on Copilot CLI, Antigravity CLI and one local model. Each record gives the client and model version, skill discovery, the question fallback, the fresh-session route, report creation and sign-off. Failed means tried and wrong. Skipped means unavailable, with the reason. Results go in the verification record and the Agent support table of `docs/PLANNING-SKILLS.md`. Any other client is a stated limit.
- **Microsoft's clients:** the plan must cite dated evidence. On 2026-11-01 the README of microsoft/power-platform-skills named Copilot CLI and Claude Code, 10 of its 25 agent files named a Claude tier, and microsoft/power-cat-skills named Microsoft Scout and Copilot CLI. A client outside those may have no build skills, so the review roles work with `powerops` commands alone.
- **Release:** M39 is a `feat:`, so its label is the next minor, v0.23.0.
