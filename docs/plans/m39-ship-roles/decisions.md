# Decisions: m39-ship-roles

## Round 1

### Q1. When should the Lead run the Security solution pass?
1. Every build (recommended)
2. Only sensitive changes
3. In your own words
Answer: Every build

### Q2. Should powerops-review-plan gain the `state:` line from the outline?
1. No, leave it (recommended)
2. Yes, add it
3. In your own words
Answer: No, leave it

### Q3. Which clients does M39 try, beyond Claude Code and Codex?
1. Antigravity and Qwen (recommended)
2. Add Copilot CLI too
3. Claude and Codex only
4. In your own words
Answer: Antigravity Copilot CLI and Qwen

## Log

- 2026-11-02: plan started at the user's request ("Yes, draft M39"), as M38 step 10, from `report.md` and `m39-outline.md` of M38. It names only the two passes the person kept.
- Q1 means that the solution pass runs before every `deploy plan` and gates nothing.
- Q2 means that outline row 2 is out: `review.md` keeps its three header lines, `powerops-review-plan` doesn't change, and `plan status` shows its state as null. So the evaluated baseline ships as it was.
- Q3 means that M39 tries Antigravity CLI, Copilot CLI and local Qwen through Pi, and records each as passed, failed or skipped. On 2026-11-02 `command -v copilot` found nothing, so the person installs Copilot CLI and signs in. An agent never loads Qwen.
- followed from the keep decision, so not asked: outline row 5 is out, because no second reviewer runs at plan stage and the second-family wording is dropped, so `powerops-plan` doesn't change. The `reports` list has no `security-plan.md`. `ForClaude` covers three skills.
- followed from the outline's rule on skill text: removing the plan pass changes an input and a stop rule, so the solution pass reruns on all 6 cases on both clients, 12 runs, and the person renews the decision before the skill ships. The Verifier ships byte for byte, so it isn't rerun.
- checked on 2026-11-02: each of the 26 solution-pass and Verifier reports of M38, put in its case's plan folder, leaves `plan status` at exit 0, because both drafts replace tenant values with placeholders. So a report written before the next role runs doesn't turn that role's status read into a wrong `failed`, and `powerops-implement` needs no rule for it.
- read on 2026-11-02: AGENTS.md; `report.md` and `m39-outline.md` of M38; both draft skills; `powerops-plan` and `powerops-implement`; `PlanChecker`, `PlanStatusReport`, `SkillsCommand`, `run.sh`, `RoleEvalTests`, `SkillsCommandTests`, `PluginManifestTests`; `docs/PLANNING-SKILLS.md`. The outline's line references were checked against `b254179`, and the plan cites that commit's lines.
- depth: standard
