# Planning skills: design

**One line:** agent skills, shipped with PowerOps, that turn a vague Power Platform idea into a reviewed, signed-off plan before any code or solution change is made.

Status: design, 2026-10-19. Build starts after M2 ([BUILD-PLAN.md](BUILD-PLAN.md), milestone M3).

## Why

Most Power Platform work starts as a sentence: "an app for tracking site visits", "automate the approvals". Agents are good at building from a clear plan and bad at noticing that the plan is not clear yet. The planning stage is where the expensive mistakes happen:

- the wrong solution boundary
- a missing publisher prefix
- premium connectors nobody licensed
- no route from Dev to Prod

What exists today:

| Project | Licence | Covers | Gap for us |
|---|---|---|---|
| [microsoft/power-platform-skills](https://github.com/microsoft/power-platform-skills) | MIT | Building: canvas and code apps, model-driven apps, flows, Power Pages, PCF | No planning, review or sign-off step; no ALM checks |
| [github/spec-kit](https://github.com/github/spec-kit) | MIT | Generic spec-driven development: specify, plan, tasks | Knows nothing about solutions, environments, connectors or `pac` |
| [anthropics/skills](https://github.com/anthropics/skills), [openai/skills](https://github.com/openai/skills) | Per skill (no repo-wide licence) | General skills and the skill format itself | Not Power Platform specific |
| Power CAT kits (Creator Kit, Copilot Studio Kit) | MIT | Components and agent testing | Not a planning flow |

PowerOps fills the gap between "I have an idea" and "the agent may start building". It works alongside the Microsoft build skills; it does not replace them.

## Principles

1. **The user's words or the user's choices.** Every question offers choices (recommended first) and always accepts an answer in the user's own words.
2. **The user decides when it is enough.** The agent keeps refining until the user stops it. It never declares the plan finished on its own.
3. **Planning is its own session.** Plans live in files, not in chat history, so a fresh session (or a different agent) can pick them up.
4. **No implementation without a signed-off plan.** If the user skips planning, the agent drafts the plan itself (the fast path, below). It does not refuse.
5. **Review in a fresh context on the provider's top model.** Only the review step uses the expensive model.
6. **Read-only towards the tenant.** Planning may call `pac` to read facts. It never writes to an environment.
7. **One skill, every agent.** Skills use the open `SKILL.md` format and fall back to plain text where an agent lacks a feature.
8. **Zero configuration.** `powerops skills install` and the defaults must be enough.

## Workflow

```
idea ──► plan (question rounds until the user says "enough")
              │
              ▼
         draft approved by the user
              │
              ▼
         review (fresh session, top model) ──► findings in review.md
              │
              ▼
         user resolves findings ──► signs off (or explicitly skips review)
              │
              ▼
         implement (only from a signed-off plan) ──► powerops analyze
```

Each arrow is a change to the plan's `status` field, so the state is visible in the files and checkable in CI.

## Skills

| Skill | Does | Writes |
|---|---|---|
| `powerops-plan` | Refines the idea through question rounds, reads environment facts with `pac`, drafts the plan and optional HTML mockups | `plan.md`, `decisions.md`, `mockups/*.html` |
| `powerops-review-plan` | Reviews a draft-approved plan in a fresh context on the top model, against a Power Platform checklist | `review.md` |
| `powerops-implement` | Checks the gate, then implements from the plan, step by step; runs `powerops analyze` at the end | code and solution changes |

`powerops-implement` is a gate, not a builder. Once the gate passes, it can hand the work to the Microsoft build skills where they are installed.

## Plan files

```
docs/plans/<plan-name>/
  plan.md        the plan, with a status header
  decisions.md   every question, the options offered and the answer, in order
  review.md      review findings and how each was resolved
  mockups/       optional static HTML mockups (no external scripts)
```

`plan.md` starts with a small front matter block:

```yaml
status: draft            # draft | approved-draft | in-review | reviewed | signed-off
review: pending          # pending | done | skipped-by-user
signed-off: null         # date, set only by the user's explicit sign-off
environment: null        # optional: environment name from `pac org who`, never a URL or ID
```

Plans are committed with the code. They contain no tenant URLs, IDs or credentials.

## Questioning protocol

Each round:

1. Ask at most four questions. Each has 2–4 choices with the recommended one first, plus "in your own words".
2. Use the agent's choice UI if it has one. Otherwise, list numbered options in plain text.
3. Append the questions and answers to `decisions.md` straight away, so a lost session loses nothing.
4. Show a three-line summary of what changed in the plan.
5. Ask whether to go another round, approve the draft, or stop for now.

Question order: outcome and users, then data, then automation, then environments and ALM, then constraints (licensing, DLP, deadlines). Questions the environment can answer (existing tables, solutions, publisher) are answered from `pac`, not asked.

## Fast path

If the user asks to implement without a signed-off plan:

1. The agent drafts `plan.md` from the request, the repo and read-only `pac` facts.
2. It presents two or three routes, each with its trade-offs.
3. The user picks one, iterates, or signs off.

The plan can be one exchange long, but it always exists, and sign-off is always the user's.

## Review

- **Fresh context.** The reviewer reads only the plan files, not the planning chat.
- **Top model of the same provider.** Users often have one provider only. The skill names a tier ("top"), not a model ID, and resolves it where the agent allows:
  - a model field in the skill or subagent settings
  - a model flag on the command line
  - otherwise, the skill asks the user to switch models for this step, and continues if they don't.
- **Checklist (Power Platform):**
  - solution boundaries and publisher prefix
  - connection references and environment variables instead of hard-coded values
  - premium connector and licence impact
  - DLP policy fit
  - Dev → Test → Prod route
  - managed vs unmanaged
  - data volume and delegation limits
  - security roles
  - testability
- **Output:** `review.md` with findings by severity. The user resolves or accepts each one before sign-off. Skipping the review is allowed only as an explicit choice at sign-off, and it is recorded as `review: skipped-by-user`.

## Pairing with `pac` and `powerops`

**Read-only `pac` calls during planning:**

| Command | Answers |
|---|---|
| `pac auth who`, `pac org who` | Which tenant and environment this plan targets (name only in the plan) |
| `pac env list` | Which environments exist for the Dev → Test → Prod route |
| `pac solution list` | Existing solutions and publishers to reuse or avoid |

If `pac` is missing or not signed in, the skill notes the assumption in `plan.md` and carries on.

**New `powerops` commands** (same offline, read-only rules as `analyze`):

- `powerops skills install [--agent claude|codex|...]` copies the skills into the agent's skill folder, or into `AGENTS.md` for agents without one.
- `powerops plan status [path]` prints each plan's status and exits non-zero if implementation started without a sign-off. This lets the gate run in CI, not just in the agent.

## Agent support

| Agent | Skill format | Choice UI | Model per step | Install target |
|---|---|---|---|---|
| Claude Code | `SKILL.md` | yes | yes | `.claude/skills/` |
| Codex CLI | `SKILL.md` | to verify | flag or profile | to verify |
| GitHub Copilot, Gemini CLI | to verify | to verify | to verify | to verify |
| Chat-only (web) | pasted instructions | numbered text | user switches | none |

The "to verify" cells are filled from the skills research before the build starts.

## Reuse

- **Reference, don't copy, by default.** We link to the Microsoft build skills and suggest installing them next to ours.
- **MIT sources** (power-platform-skills, spec-kit, the CAT kits) may be adapted, with credit in a `NOTICE` file.
- **Per-skill licences** (anthropics/skills, openai/skills) are checked one skill at a time. Anything without a clear permissive licence is reference only.
- **Open question:** should plan files be compatible with spec-kit's layout, so teams already using it can adopt ours without converting?

## Limits

- A skill cannot open a new session or force a choice UI. The file-based status is the real gate.
- Model selection is best effort. Where an agent cannot switch models from a skill, the user has to.
- Question quality cannot be proven in CI. CI can lint the skill files and run scripted smoke tests; the rest needs dogfooding.
- HTML mockups are visual sketches, not canvas app source. Generating `.pa.yaml` from a plan is out of scope for the first version.

## Open questions

1. spec-kit compatibility (above).
2. Whether `powerops plan status` should also link a plan to the `analyze` results of the solution it produced.
3. Exact install targets and question tools for Codex, Copilot and Gemini (skills research).
