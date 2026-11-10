# Decisions: m82-plugin-specialists

The answer below came before this milestone's plan was written: it is one of the six open questions (section 6) of the PowerOps 1.0 fix plan that Fable and Astra agreed on 2026-11-08, asked of the person in one round by the session that began the fixes. The plan that follows starts its own rounds after this one.

## Round 0 (the 1.0 fix plan)

### Q4. Ship the six specialists in the Claude plugin?
1. Yes, self-contained and advisory (M82); the eval rule covers shipping as a skill (recommended by both reviewers)
2. Wait for an M38-style eval

Answer: 1, "Yes, advisory" (answered by: person, 2026-11-08).

## Log

- 2026-11-08: asked together with the fix plan's other five questions, in one round, recommended option first where the plan recommends one; where the plan states two positions (Q1, Q6), both were shown as the plan words them, neither marked recommended.
- 2026-11-09: the session that runs the 1.0 fix chain started this milestone's session (M82, wave A with M76, M79, M80 and M81), on `main` `278b136`. The fix plan's section 3 ("M82"), read with sections 4 and 5, is the base. The person's answers that apply here, quoted from the chain's rules (2026-11-09): to "For M76 and M79–M83, who signs off each milestone's plan before it is built?", "Sessions sign off (Recommended)": each session signs its own plan once the Fable plan review approves it and `powerops-review-plan` leaves no finding open; to "Who merges each milestone's PR once all 8 CI checks pass, starting with M75's open PR #128?", "Sessions merge (Recommended)": each session rebases on main, reruns its checks, opens its PR and squash-merges it when green, one PR at a time.
- read on 2026-11-09: the fix plan (sections 1 to 7); the six `.claude/agents/powerops-*.md` specialists; `CLAUDE.md`; `AGENTS.md`; `PluginManifestTests.cs`; `SkillsCommand.ForClaude`; `plugins/claude/.claude-plugin/plugin.json`; `docs/USE-WITH-AGENTS.md`; the M75 plan as the format.
- 2026-11-09: the fix plan's claim (plugins load `agents/*.md` with this frontmatter) checked by a `claude-code-guide` subagent against code.claude.com, and `claude plugin validate` run locally on a temp plugin holding the six sources: results in Assumptions. Codex plugin agents stay not checked.
- depth: quick: the fix plan gives the design; no question for the person (Q4 settles scope).
- areas not asked: users, automation, screens, solution and publisher, environments and ALM; the change is to this repo's plugin, tests and docs, and the plan answers each or marks it not applicable.
- 2026-11-09: refinements to the fix plan's text: the generator is a test-project class beside the skill-copy writer, not inline in the theory; the shared block is extracted from `CLAUDE.md` with fixed rewrites, so a change to that section moves the copies and the test fails until they are regenerated; design-lead's paths to the plan skill's files use `${CLAUDE_PLUGIN_ROOT}`, which Claude Code substitutes in a plugin agent's body; copilot-studio's `docs/HANDOFF.md` becomes the repository URL; the test also forbids `CLAUDE.md`, `AGENTS.md`, `.claude/` and any `docs/` path outside `docs/plans/`.
- fresh review (a general-purpose subagent following `powerops-review-plan`, Opus, fresh context) 2026-11-09: 0 High, 3 Medium, 7 Low (R1 to R10), all answered in `plan.md` or here (R9 accepted: public documentation URLs only)
- plan review under the model routing (a `fable-reviewer` subagent, Fable, fresh context) 2026-11-09: REJECTED with five findings, all answered in `plan.md`: the `docs/` test exempts the repository URL (1, as R1); every rewrite throws when its target is missing, and the content test iterates the six and asserts the record-only text, the spawn name and the `deploy_apply` bullet (2); every plan-skill path gets `${CLAUDE_PLUGIN_ROOT}` (3, as R2); a fixed lead-in for the shared rules and the brief's fields in the docs (4, as R3); `powerops-bi`'s `--readonly` premise becomes a condition, and the docs state facts about the MCP servers instead of "work best" (5)
