# Decisions: m38-agent-team

## Round 1

### Q1. How many agent roles should PowerOps have?
1. Four roles plus the person: two exist and two are new, and only one writes (recommended)
2. Two roles: the Lead and one reviewer that covers everything
3. One role per stage, six or more
4. In your own words
Answer: Four roles plus the person: two exist and two are new, and only one writes

### Q2. Should PowerOps build on DeepSeek's dsh-superagent prototype?
1. Borrow its ideas, and take no code (recommended)
2. Fork it and adapt it
3. Run it beside PowerOps as a helper for this repo
4. In your own words
Answer: Borrow its ideas, and take no code

### Q3. Which of its ideas go in?
1. All five: fixed report states, scope fixed at approval, record-only first, reduced tools per role, stop by impact (recommended)
2. Only the states and record-only first
3. In your own words
Answer: All five: fixed report states, scope fixed at approval, record-only first, reduced tools per role, stop by impact

## Log

- 2026-11-01: plan started at the user's request, after research on team size, client support and Microsoft's skills. The team shape and the prototype's ideas were agreed in chat.
- why four roles: no study shows a best number. Multi-agent setups gained 80.8% on work that splits cleanly and lost 70.0% on sequential planning across 260 configurations (https://arxiv.org/abs/2512.08296). In a benchmark of 50 web apps, 1 of 4 team comparisons was significant, at 1.8 to 5.1 times the cost (https://www.vals.ai/blogs/multi-agent-vibe-code-bench). Microsoft advises the lowest complexity that works, and names distinct security boundaries as a reason to split (https://learn.microsoft.com/azure/architecture/ai-ml/guide/ai-agent-design-patterns). <!-- plan-status: allow -->
- why measure first: evidence for a second reviewer is narrow and mixed. One fresh and one cross-model review matched 56.7% of planted errors against 42.7% for two fresh reviews, without separating model difference from reviewer capability (https://arxiv.org/abs/2610.01471), and a reviewer that couldn't run tests lowered one model's pass rate (https://arxiv.org/abs/2607.21656). <!-- plan-status: allow -->
- why ideas only from the prototype: it is MIT licensed, but it is a bundle for DeepSeek Harness, which ties the team to one client, and its README calls it a research prototype for reference only. It is an always-on bot for one GitHub repository. Its two roles, an orchestrator and a worker that reviews its own work, have no independent reviewer (https://github.com/deepseek-ai/dsh-superagent-prototype). No code or prompt text is copied. <!-- plan-status: allow -->
- read on 2026-11-01: AGENTS.md; `powerops-plan`, `powerops-review-plan` and `powerops-implement`; the M37 plan (layout); `PlanChecker`, `SkillsCommand`, `McpCommand` and their tests; `docs/PLANNING-SKILLS.md`; the skills smoke test and its workflow; the Agent Skills specification (https://agentskills.io/specification); the Power Platform Well-Architected Security checklist (https://learn.microsoft.com/power-platform/well-architected/security/checklist); the agent files of microsoft/power-platform-skills and microsoft/skills-for-copilot-studio; the skill list of microsoft/power-cat-skills. <!-- plan-status: allow -->
- depth: standard
- areas not asked: users, data, automation, environments and ALM, and constraints. The change is to this repo, and the plan answers each or marks it not applicable.
- 2026-11-01: two reviewers on different model families read the first draft independently, then discussed it. Both said the role design holds and the measurement half needed rework. They agreed to split the work into M38, which measures, and M39, which builds, because step 6 of the first draft removed rows after sign-off, and a body edit voids a sign-off. They also agreed to cut `powerops mcp --read-only`, keep the existing completion rule, read Microsoft's power-cat-skills output without running those skills, freeze the keep rules, and describe portability as a stated limit. The draft was changed to match, and the rest of the first draft moved to `m39-outline.md`.
- 2026-11-01: approved by the person, who accepted the reviewers' changes: the split into M38 and M39, the cut of `--read-only`, and "any client" as an aim, not a claim.
- approved without: users, data, automation, environments and ALM, constraints
- fresh review (general-purpose subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-11-01: 0 High, 7 Medium, 9 Low (R1 to R16). R1 to R15 fixed in `plan.md`, R16 accepted, with the reasons in `review.md`.
- the same reviewer re-read the fixed plan 2026-11-01: R6 reopened, and 8 new Low (R17 to R24). All fixed in `plan.md`, with the reasons in `review.md`.
- the same reviewer checked those nine fixes 2026-11-01: all hold, no new High or Medium.
- 2026-11-01: signed off by the person with `powerops plan sign-off`.
- checked on 2026-11-01 with Claude Code 2.1.316 and Codex CLI 0.166.0, on a throwaway skill in a temp folder. Claude's `--setting-sources project --strict-mcp-config` dropped the user's instructions, skills and MCP servers. Its `--tools` list with `Skill` still started the project skill, and the allowlist refused a `powerops` command outside it. Codex's `--ignore-user-config` still loaded the user's `AGENTS.md` and skills, and a throwaway `HOME` with a linked sign-in file did not.
