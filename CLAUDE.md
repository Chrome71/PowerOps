@AGENTS.md

## Claude Code extras (optional)

Subagents for this repo are in `.claude/agents/`: researcher, designer, implementer, reviewer and chores for work on PowerOps itself, and the six Power Platform specialists below. Claude picks one from its description. The six are for work in this repo and ship in no plugin, since the M119 eval of 2026-11-22 (the owner's answer: "Keep none"); spawn the project agents by their bare names, not as `powerops:<name>`, which an older plugin install may still list.

Read the [specialist guide](.claude/agents/README.md) only when spawning a specialist. It contains the routing table, brief fields, shared rules, and reply format.

Saved workflows in `.claude/workflows/` chain those agents together. On the Pro plan, turn on "Dynamic workflows" in `/config` first. Each workflow asks for approval before it runs.

| Command | Example | Agents per run |
|---|---|---|
| `/review-pr` | `/review-pr` or `/review-pr main` | up to 18 |
| `/implement-feature` | `/implement-feature add a --json flag to the list command` | 2 per task, max 5 tasks |
| `/design-panel` | `/design-panel add a "powerops diff" command` | 5 |
| `/verify-research` | `/verify-research "Does pac solution unpack support YAML?"` | about 3 per question |

None of the workflows ever push. `/implement-feature` commits locally on your current branch, one commit per task; if you are on `main`, it creates a `feat/` branch first.
