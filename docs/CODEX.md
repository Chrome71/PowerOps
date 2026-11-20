# PowerOps with Codex

PowerOps supplies the same seven core skills and CLI-backed MCP tools to Claude Code and Codex. `powerops-specialists` adds the six domain procedures that previously shipped only as Claude agents. Equivalent procedures do not establish equal model performance, installed connectors or harness enforcement.

## Use the skills

Install the CLI and choose either the plugin or repository copies as described in [Use from an agent](USE-WITH-AGENTS.md). For repository copies, run `powerops skills install --agent codex`, or add `--force` when deliberately replacing older copies. They go to `.agents/skills/`. Start a new session if the client has not refreshed its skill list. Do not install both forms in one project unless you want duplicate entries.

In this source checkout, Codex may read `skills/<name>/SKILL.md` directly even when those files are not in its discovered skill list. This gives it the procedure, not an installed plugin or a connected MCP server. From a built checkout the CLI is `dotnet src/PowerOps.Cli/bin/Debug/net10.0/powerops.dll`; installing an older global CLI does not install these new sources.

Example: "Use powerops-specialists for workflow-logic plan-advice on docs/plans/orders, tenant: offline. Review the approval state model and return proposed plan lines." Read [the specialist skill](../skills/powerops-specialists/SKILL.md), then only the reference it selects. It works in one session unless delegation was requested. In Claude, the six named plugin agents remain available as well.

## Capability comparison

| Capability | Claude Code | Codex | Evidence or limit |
|---|---|---|---|
| Plan, prototype, sketch judge, plan review, implement, verify, security review | Seven core skills | The same seven skill bodies | Generated-copy tests; only Claude's fork/model frontmatter differs |
| Apps, Automate, BI, design, workflow logic, Copilot Studio advice | Six native agents or portable specialist skill | Portable specialist skill with the same six domain procedures | Generated from the Claude sources, with relative reference checks; advisory, not evaluated |
| CLI and MCP checks, dry runs and deploy gate | `powerops` / `powerops mcp` | Same executable and server | Shared command and MCP tests; live credentials are separate |
| Fresh-context review | Installed review skills fork | Worker with no inherited history, or a new session | Never report fresh context from a planning chat or a full-history fork |
| Model choice | Native specialists pin Opus; maintenance agents use Sonnet/Haiku | Portable skill keeps the selected session/parent model | No fixed model alias or account tier required; no claim of equal accuracy |
| Questions | Client question UI | Available question tool, otherwise chat | The person still answers approvals and sign-off; do not auto-answer |
| Tenant confirmation and blocking hooks | Client-specific | Client-specific | [HARNESS.md](HARNESS.md) records differences; skill instructions do not install hooks |
| Microsoft product tools | Separately installed servers/skills | Separately installed servers/skills | Discover actual tools; absent means unavailable, not an invented fallback |
| PowerOps repository maintenance | Five repo agents and four JavaScript workflows | The procedures below, using Codex tools | No Claude workflow runtime or native agent registration is implied |

When a core skill requires a fresh review and delegation is authorized, pass only its file, the plan folder, required artifacts and objective to a worker with no chat history (`fork_turns: "none"` when the tool exposes it). Otherwise ask the person to run the given request in a new session and wait. Keep the evaluated `powerops-review-sketch`, `powerops-verify` and `powerops-review-security` files unchanged. A specialist review cannot substitute for any of those reports.

Discover tools before live work. A tool named in a Claude `tools:` list does not mean it is connected in Codex, and that list is not a Codex permission control. The PowerOps plugin starts only its own MCP server. Never copy `.claude/settings.local.json`, sign-ins or tenant-specific configuration into a shared Codex setup. Use the existing [hand-off and gate setup](HANDOFF.md) for a user's own connections.

## Repository maintenance procedures

These apply to contributing to PowerOps itself, not to building a customer's Power Platform solution. Read the matching file below for its task procedure; ignore its Claude YAML model/tool configuration. Use Codex's available tools under the current sandbox and approval policy. A read-only instruction is not a separate sandbox. The user's request and `AGENTS.md` take precedence, including authorization to commit.

| Work | Procedure source | Codex adaptation |
|---|---|---|
| Check Microsoft claims | [.claude/agents/powerops-researcher.md](../.claude/agents/powerops-researcher.md) | Official documentation tools or web search; answer, release status/date, sources, confidence and unverified items; no edits |
| Design a PowerOps change | [.claude/agents/powerops-designer.md](../.claude/agents/powerops-designer.md) | Read-only inspection, smallest complete approach, affected files, tests, risks and deferrals |
| Implement a bounded change | [.claude/agents/powerops-implementer.md](../.claude/agents/powerops-implementer.md) | Tests first for logic; build/test/format; commit only when requested; new branches use `codex/` |
| Review a diff | [.claude/agents/powerops-reviewer.md](../.claude/agents/powerops-reviewer.md) | Inspect connected code, return substantiated file/line findings; no edits or git mutations |
| Mechanical chores | [.claude/agents/powerops-chores.md](../.claude/agents/powerops-chores.md) | Touch only named files; stop on ambiguous steps; no implicit commit |

Use one agent by default. If the user explicitly asks for one of these multi-agent workflows, carry out the equivalent with Codex's available subagent tools; do not execute the Claude JavaScript files with Node. Inherit model/effort unless the user requests otherwise, respect available concurrency, and wait for all required results. A missing worker result is a failed or incomplete check, never approval.

- **Design panel:** three independent read-only proposals (smallest solution, risk, contributor/user experience), then two independent reviewers score each proposal 1–10. Return proposals, scores, reasons and missing responses. Source: [.claude/workflows/design-panel.js](../.claude/workflows/design-panel.js).
- **Review PR:** review the named base (default `origin/main`, fallback `main` with disclosure), committed and uncommitted changes, through correctness, security and simplicity; at most five findings per lens. Independently try to refute each finding, deduplicate and report survivors, failed checks and empty diffs. Source: [.claude/workflows/review-pr.js](../.claude/workflows/review-pr.js).
- **Implement feature:** at most five supplied tasks in order, one implementer and one independent reviewer per task. The commit-per-task form requires a clean working tree and an explicit request to commit; otherwise implement the requested bounded change without commits. Build, test and format must pass. Review the actual diff or commit, stop on High/Medium findings or missing review, and mark remaining tasks skipped. Never push. Source: [.claude/workflows/implement-feature.js](../.claude/workflows/implement-feature.js).
- **Verify research:** answer each supplied question from sources, then use two independent attempts to refute it from current sources. Mark confirmed only when both ran and neither refuted, and explain that agreement is not proof. Report missing checks and unresolved contradictions. Source: [.claude/workflows/verify-research.js](../.claude/workflows/verify-research.js).

If independent workers are unavailable, offer a sequential pass and label it as such; do not claim it reproduced an independent panel.

## Verification and scope of this adaptation

Reviewed on 2026-11-20: source skills, plugin manifests and generated copies, `SkillsCommand`, specialist sources, maintenance agents/workflows, MCP/harness docs, and ecosystem compatibility/open entries. The Microsoft rows were checked on 2026-11-10, within 90 days. This adaptation preserves their domain procedures; it adds no Microsoft API, service or CLI route. ECO-0011 (client smoke pins) and ECO-0015 (MCP confirmation protocol) remain open; this work does not move a pin or establish new live harness guarantees.

The owner's request authorizes portable specialist packaging. This replaces the previous packaging-only restriction in `CLAUDE.md`; the specialists remain unmeasured and advisory. Frozen report roles, their evaluation pins and the calibrated sketch judge are unchanged. Generated procedure comparisons, clean temporary installs for both clients, portable frontmatter and reference checks validate packaging. They do not measure model behavior, an installed marketplace update, live Microsoft operations, the desktop confirmation form or hook enforcement.

Official OpenAI documentation read on 2026-11-12 supports converting reusable Claude agent procedures into skills and using Codex subagents with inherited model settings: [Claude plugin adaptation](https://developers.openai.com/plugins/guides/submit-claude-plugin), [Codex subagents](https://developers.openai.com/codex/subagents). These sources establish available mechanisms, not a live PowerOps integration test.
