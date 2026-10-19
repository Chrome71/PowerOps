# Planning skills: design

**One line:** agent skills, shipped with PowerOps, that turn a vague Power Platform idea into a reviewed, signed-off plan before any code or solution change is made.

Status: design, 2026-10-19, revised the same day after a prototype test and a survey of existing skills. Build starts after M2 ([BUILD-PLAN.md](BUILD-PLAN.md), milestone M3).

## Why

Most Power Platform work starts as a sentence: "an app for tracking site visits", "automate the approvals". Agents are good at building from a clear plan and bad at noticing that the plan is not clear yet. The planning stage is where the expensive mistakes happen:

- the wrong solution boundary
- a missing publisher prefix
- premium connectors nobody licensed
- no route from Dev to Prod

What exists today:

| Project | Licence | Covers | Gap for us |
|---|---|---|---|
| [microsoft/power-platform-skills](https://github.com/microsoft/power-platform-skills) | MIT | Building skills for canvas and code apps, model-driven apps, flows, Power Pages and PCF. Its one planner, `plan-alm`, plans Power Pages ALM | No general planning, no independent review, no CI check; targets Claude Code and Copilot CLI |
| [github/spec-kit](https://github.com/github/spec-kit) | MIT | Generic spec-driven development: specify, clarify, plan, tasks | Knows nothing about solutions, environments, connectors or `pac`; branch-coupled layout and scripts |
| [mattpocock/skills](https://github.com/mattpocock/skills) | MIT | A "grilling" loop: keep asking, a recommended answer per question, stop when nothing is left to ask | Not Power Platform specific |
| [obra/superpowers](https://github.com/obra/superpowers) | MIT | Brainstorm, plan and review skills; per-agent tool maps; reviews in a fresh subagent | Not Power Platform specific |

PowerOps fills the gap between "I have an idea" and "the agent may start building" for any Power Platform project. It adds an independent review and a CI check, and it works across agents. It works alongside the Microsoft build skills and does not replace them.

## Principles

1. **The user's words or the user's choices.** Each question offers two or three choices (recommended first) and always accepts free text. Plain text is the default; an agent's choice picker is an upgrade where it exists.
2. **The user decides when it is enough.** The agent keeps refining until the user stops it. When it has nothing left to ask, it says so and recommends approving, but the user decides.
3. **Planning is its own session.** Plans live in files, not in chat history, so a fresh session (or a different agent) can pick them up.
4. **No implementation without a signed-off plan.** If the user skips planning, the agent drafts the plan itself (the fast path, below). It does not refuse.
5. **Interactive only.** The planning skill needs a person to answer. Under an autopilot or non-interactive mode it says so and stops, rather than answering its own questions.
6. **Review in a fresh context on the provider's top model.** Only the review step uses the expensive model, and the reviewer never asks the user questions; it returns findings.
7. **Read-only towards the tenant, and quiet about it.** Planning may read environment facts. It never writes to an environment, and plan files hold only the target environment's name.
8. **One skill, every agent.** Skills use the open `SKILL.md` format with portable front matter, and fall back to plain text where an agent lacks a feature.
9. **Zero configuration.** `powerops skills install` and the defaults must be enough.

## Workflow

```
idea ──► plan (question rounds until the user says "enough")
              │
              ▼
         draft approved by the user (open questions shown)
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
| `powerops-plan` | Refines the idea through question rounds, reads environment facts, drafts the plan, runs the fast path, and writes HTML mockups | `plan.md`, `decisions.md`, `mockups/*.html` |
| `powerops-review-plan` | Reviews an approved draft in a fresh context on the top model, against a Power Platform checklist. Never asks the user | `review.md` |
| `powerops-implement` | Checks the gate. With no signed-off plan it hands over to `powerops-plan` in fast-path mode; with one, it implements step by step and runs `powerops analyze` at the end | code and solution changes |

- `powerops-plan` owns the fast path and mockups, because both are planning; `powerops-implement` only holds the gate.
- The `powerops-plan` description must also match requests for a mockup, screen, wireframe or prototype of a Power Platform app. In the prototype test, "mock it up" did not trigger the skill.
- Mockups are static HTML with no external scripts. They are written only to `mockups/` in the plan folder and are never published or uploaded anywhere.
- Once the gate passes, `powerops-implement` can hand the build work to the Microsoft build skills where they are installed.

## Plan files

```
docs/plans/<plan-name>/
  plan.md        the plan, with a status header
  decisions.md   every question, the options offered and the answer, in order
  review.md      review findings and how each was resolved
  mockups/       optional static HTML mockups
```

`plan.md` starts with a small front matter block:

```yaml
status: draft            # draft | approved-draft | in-review | reviewed | signed-off
review: pending          # pending | done | skipped-by-user
open-questions: 0        # count of unresolved items, shown by `powerops plan status`
signed-off: null         # date, set only by the user's explicit sign-off
environment: null        # target environment name only
```

Plans are committed with the code, so they hold no tenant data:

- Only the **target** environment's name is written. Other environments are used to answer questions but are not written down, and are not offered as targets unless the user names them. (In the prototype test, both agents copied every environment name in the tenant into `plan.md`.)
- No URLs, environment or tenant IDs, emails or credentials.

Questions only someone else can answer (licensing and DLP often sit with an admin) go into a "Questions for your admin" list in `plan.md`, so the user can forward them.

## Questioning protocol

Each round:

1. Ask at most **three** questions. Each has two or three choices with the recommended one first, plus "in your own words". (Three is the common limit: Codex's question tool allows at most three questions of two or three options.)
2. Use the agent's choice tool if it has one. Otherwise, list numbered options in plain text.
3. Append the questions and answers to `decisions.md` straight away, so a lost session loses nothing.
4. Show **exactly three lines**, one per change to the plan. Assumptions and open items go into `plan.md`, not into this message.
5. Ask what next, with **lettered** options so they never clash with numbered answers: **A** another round, **B** approve the draft, **C** stop for now.

Question order: outcome and users, then data, then automation, then environments and ALM, then constraints (licensing, DLP, deadlines). Two rules keep the late areas from being skipped:

- Ask one licensing question in round 1 or 2, and the environments question by round 3 at the latest.
- Before approving, show how many questions are open and which of the five areas were never asked, and ask for one confirmation if any area is unasked. An approval without them is recorded in `decisions.md`, for example "approved without: environments, constraints". (In the prototype test, Codex approved after three rounds with environments, licensing and DLP unasked.)

An approved draft may still have open questions. The review and the sign-off both see the count.

Facts the environment can answer (existing tables, solutions) are read, not asked.

## Fast path

If the user asks to implement without a signed-off plan:

1. The agent drafts `plan.md` from the request, the repo and environment facts. It asks **no questions except the route choice**.
2. It presents two or three routes, each with its trade-offs. Every guess goes under Assumptions, and `status` stays `draft`.
3. The user picks a route, asks for question rounds, or approves.

The plan can be one exchange long, but it always exists, and sign-off is always the user's.

## Review

- **Fresh context.** The reviewer reads only the plan files, not the planning chat.
- **Never asks the user.** Subagents usually run without a person to answer; findings go into `review.md` instead.
- **Top model of the same provider.** Users often have one provider only. The skill names a tier ("top"), not a model ID. `powerops skills install` pins it where the agent supports that:

  | Agent | How the review runs on the top model |
  |---|---|
  | Claude Code | a Claude-specific copy of the skill with `context: fork` and `model` in its front matter, generated at install |
  | Codex | a custom agent in `.codex/agents/` with `model` and `model_reasoning_effort` |
  | Gemini CLI | a subagent in `.gemini/agents/` with a `model` field |
  | Others | the skill asks the user to switch models for this step, and continues if they don't |

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
  - unanswered areas and the open-questions count
- **Output:** `review.md` with findings by severity. The user resolves or accepts each one before sign-off. Skipping the review is allowed only as an explicit choice at sign-off, and it is recorded as `review: skipped-by-user`.

## Pairing with `pac` and `powerops`

**Environment facts.** The skill calls `powerops plan facts`, which runs the read-only `pac` commands itself and prints names only, as JSON. Plain `pac` output carries tenant IDs, emails and every environment name, and the agent should never see those.

| Fact | Source | Notes |
|---|---|---|
| Target environment name | `pac org who` | `pac auth who` is not used; its output is mostly tenant ID and email |
| Other environments | `pac env list` | Names only, used for the Dev → Test → Prod question, never written to the plan |
| Existing solutions | `pac solution list` | Unique name, version and managed flag. It does **not** return publishers or prefixes |
| Publisher prefix | asked in a question round | Unless a read-only query proves to work (untested: `pac env fetch` with FetchXML on the publisher table) |

If `pac` is not on PATH or not signed in, the skill says exactly that, names the fix (add the dotnet tools folder to PATH, run `pac auth create`) without running it, notes the assumption in `plan.md`, and carries on.

**New `powerops` commands** (same offline, read-only rules as `analyze`; `plan facts` is the one exception that reads through `pac`):

- `powerops skills install [--agent ...]` installs the skills (below).
- `powerops plan facts` prints the names-only environment facts above.
- `powerops plan status [path]` prints each plan's status and open-questions count, and exits non-zero if implementation started without a sign-off. This lets the gate run in CI, not just in the agent.

## Agent support

| Agent | Skill folder (project) | Choice tool | Notes |
|---|---|---|---|
| Claude Code | `.claude/skills/` | `AskUserQuestion` | Reads `AGENTS.md` only through `CLAUDE.md` |
| Codex | `.agents/skills/` | `request_user_input`: 1–3 questions, 2–3 options, **Plan mode only** by default | The numbered-text fallback worked in the prototype test; a resumed `codex exec` keeps the first call's sandbox and rejects `-s` |
| Gemini CLI | `.agents/skills/` or `.gemini/skills/` | `ask_user`: 1–4 questions, 2–4 options | Needs a confirmation to activate a skill |
| GitHub Copilot | `.github/skills/`, `.claude/skills/` or `.agents/skills/` | `ask_user` | **Autopilot answers `ask_user` itself**, so the planning skill stops under autopilot (principle 5) |
| Chat-only (web) | none | numbered text | User pastes the instructions and switches models by hand |

**Install.** `powerops skills install` writes `.agents/skills/powerops-*/` (Codex, Gemini, Copilot and most other agents) and `.claude/skills/powerops-*/` (Claude Code). Agents that read both folders may list a skill twice, so it writes only the folders for the agents it detects, or uses one copy and a link. Agents without skills support get a short marked block in `AGENTS.md` that points at `docs/plans/` and states the gate.

**Portability.**

- The shared `SKILL.md` uses only the spec's portable front matter (`name`, `description`, `license`, `compatibility`, `metadata`). Agent-specific tuning lives in copies generated at install time.
- Descriptions stay short, with the trigger first. Codex reserves about 2% of the context window for the skill list, and Claude Code truncates descriptions at 1,536 characters.
- The repo layout `skills/<name>/SKILL.md` also lets people install with `npx skills add`. That CLI has telemetry on by default, which the docs will say.
- CI validates the skills with `skills-ref validate` from [agentskills](https://github.com/agentskills/agentskills) (Apache-2.0).

## Reuse

Rule: copy nothing that is not clearly MIT or Apache-2.0 at file level. Adapting ideas is fine; adapted text gets a `NOTICE` entry.

| Source | Licence | Use |
|---|---|---|
| mattpocock/skills, grilling loop | MIT | Adapt: recommended answer per question, "nothing left to ask" stop rule, a questionnaire for questions the user cannot answer alone |
| gemini-cli-extensions/conductor, interaction wording | Apache-2.0 | Adapt the "choices plus Other" wording (keep its notice, state the changes) |
| obra/superpowers | MIT | Borrow per-agent tool-map reference files and the no-history review dispatch |
| microsoft/power-platform-skills | MIT | Reference and recommend installing alongside; learn from `plan-alm`'s status writer and gate. Don't use its installer (telemetry) or per-skill `model: opus` |
| github/spec-kit | MIT | Reference only in v1; use its `clarify` taxonomy as a checklist for question order |
| agentskills/agentskills | Apache-2.0 | Use `skills-ref` in CI |
| anthropics/skills | no repo-wide licence; some skills source-available | Do not copy |
| openai/skills | no repo-wide licence; deprecated | Do not copy |
| awesome-claude-code (CC BY-NC-ND), spec-workflow-mcp (GPL-3.0), pilot-shell (proprietary) | — | Do not copy |

## Limits

- A skill cannot open a new session or force a choice UI. The file-based status is the real gate.
- Model selection is best effort. Where an agent cannot switch models from a skill, the user has to.
- Codex's question tool is off outside Plan mode by default, so Codex users mostly get the numbered-text fallback.
- Question **quality** cannot be proven in CI, but the protocol's **shape** can: a scripted smoke test with a simulated user (as in the prototype test) checks round size, option order, `decisions.md` logging and the stop behaviour.
- HTML mockups are visual sketches, not canvas app source. Generating `.pa.yaml` from a plan is out of scope for the first version.

## Open questions

1. Can a read-only `pac` query return the publisher prefix (`pac env fetch` on the publisher table)? If not, the prefix stays a question.
2. Do all four agents ignore front matter fields they don't know? Unverified; until then the shared `SKILL.md` stays portable-only.
3. Should `powerops plan status` also link a plan to the `analyze` results of the solution it produced?
4. spec-kit compatibility: not in v1. A later `powerops plan export --spec-kit` is possible if people ask.

Evidence for this revision: a prototype test of `powerops-plan` (six scripted conversations in Claude Code and Codex) and a survey of 28 skill repos, both kept as local research notes.
