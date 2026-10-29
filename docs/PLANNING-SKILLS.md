# Planning skills: design

**One line:** agent skills, shipped with PowerOps, that turn a vague Power Platform idea into a reviewed, signed-off plan before any code or solution change is made.

Status: design, 2026-10-19, revised the same day after a prototype test, a survey of existing skills and an independent design review. Build starts after M2 ([BUILD-PLAN.md](BUILD-PLAN.md), milestone M3).

## Why

Most Power Platform work starts as a sentence: "an app for tracking site visits", "automate the approvals". Agents are good at building from a clear plan and bad at noticing that the plan is not clear yet. The planning stage is where the expensive mistakes happen:

- the wrong solution boundary
- a missing publisher prefix
- premium connectors nobody licensed
- no route from Dev to Prod

What exists today:

| Project | Licence | Covers | Gap for us |
|---|---|---|---|
| [microsoft/power-platform-skills](https://github.com/microsoft/power-platform-skills) | MIT | Building skills for canvas and code apps, model-driven apps, flows, Power Pages and PCF. Its planners each cover one product: `plan-alm` plans Power Pages ALM, and model-apps' `app-builder` and `genpage` plan the app they build (read 2026-10-24) | No general planning, no independent review, no CI check; targets Claude Code and Copilot CLI |
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
6. **Review in a fresh context on the provider's top model, and say honestly when that did not happen.** Only the review step uses the expensive model, and the reviewer never asks the user questions; it returns findings. `review.md` records the model and whether the context was fresh.
7. **Read-only towards the tenant, and quiet about it.** Planning may read environment facts. It never writes to an environment, and plan files hold only the target environment's name.
8. **One skill, every agent.** Skills use the open `SKILL.md` format with portable front matter, and fall back to plain text where an agent lacks a feature.
9. **Zero configuration.** `powerops skills install` and the defaults must be enough.

## Workflow

`status` has five values: `draft | approved | signed-off | implementing | done`. Everything else is derived or recorded elsewhere.

```
                 powerops-plan                         powerops-review-plan
idea ──► draft ──(user: B approve)──► approved ──(agent forks reviewer)──► review.md  (review: done)
                                        │                                      │
                                        │ user: "sign off", review skipped     │ user resolves findings
                                        │ (review: skipped)                    │
                                        └──────────► signed-off ◄──────────────┘
                                                     `powerops plan sign-off`: date + body hash
                                                          │
                                                   powerops-implement: gate = status signed-off AND hash matches
                                                          ▼
                                                     implementing ──► done   (`powerops analyze` ran, or "not verified")

Body edited after sign-off ──► `powerops plan status` exits 1: "changed since sign-off"; fix = user re-signs, or revert
```

| From | To | Who | Records |
|---|---|---|---|
| none | draft | `powerops-plan` (rounds or fast path) | `plan.md`, `decisions.md` |
| draft | approved | `powerops-plan`, only on the user's explicit lettered approval (B in a round menu, A or B in the fast-path menu) | `status`; an "approved without: (areas)" line in `decisions.md` if any area was never asked |
| approved | approved, `review: done` | `powerops-review-plan`, in a fresh context on the top model, dispatched by the parent session | `review.md` with its header; findings numbered R1, R2... |
| approved | signed-off | `powerops-plan` sign-off step: shows the open-question count, unresolved findings and the `review.md` header lines, asks once with lettered options, then runs `powerops plan sign-off <plan>` | `status`, `signed-off`, `signed-off-hash`; `review: skipped` if there is no `review.md` and the user chose to skip |
| signed-off | implementing | `powerops-implement`, after the gate passes | `status` only |
| implementing | done | `powerops-implement`, after the steps and the final `analyze` and `test` | `status` only; progress lives in commit messages, not in `plan.md` |
| any | draft | `powerops-plan`, when the user asks for more rounds or edits the plan | `status` |

Header fields are left out of the hash, so status changes never invalidate a sign-off; edits to the body do. The state is visible in the files and checkable in CI.

### Removed or simplified, and why

- **"`plan status` exits non-zero if implementation started without a sign-off"**: removed. A command that only reads files cannot see implementation. CI validates the plans instead, and the `implementing` state is the signal on the honest path. Scope globs plus a git diff may come later if teams ask.
- **`in-review` and `reviewed` states, and the `open-questions` field**: removed. Review state is the `review` field plus `review.md`, and the open-question count is computed. A crashed review leaves nothing to clean up.
- **`approved-draft`**: renamed `approved`.
- **Automatic reset to draft on edit**: not adopted. No agent can reliably watch for edits. The hash mismatch is the signal, and the user re-signs.
- **`pac env list` in `plan facts`**: removed. The design already says other environments are never offered unless the user names them, so the route question asks the user.
- **One copy and a link**: removed. Install writes real copies only.
- **Install-time model ID pinning**: removed, see [Review](#review).
- **Editing `AGENTS.md`**: removed. Install writes only the skill folders and never edits `AGENTS.md`.
- **Two hashes, a versioned facts schema, an ownership manifest, a release-versioned "top model" map**: not adopted. One body hash recorded in two places shows whether the review or the sign-off is stale, and the rest is machinery with no user-visible gain yet.

## Skills

| Skill | Does | Writes |
|---|---|---|
| `powerops-plan` | Refines the idea through question rounds, reads environment facts, drafts the plan, runs the fast path, and writes HTML mockups | `plan.md`, `decisions.md`, `mockups/*.html` |
| `powerops-review-plan` | Reviews an approved draft in a fresh context on the top model, against a Power Platform checklist. Never asks the user | `review.md` |
| `powerops-implement` | Checks the gate (`powerops plan status`: status `signed-off` and the hash matches). With no signed-off plan it hands over to `powerops-plan` in fast-path mode; with one, it implements step by step and runs `powerops analyze` and `powerops test` at the end | code and solution changes |

- `powerops-plan` owns the fast path and mockups, because both are planning; `powerops-implement` only holds the gate.
- The `powerops-plan` description must also match requests for a mockup, screen, wireframe or prototype of a Power Platform app. In the prototype test, "mock it up" did not trigger the skill.
- Mockups are static HTML with inline CSS and no external resources (no scripts, fonts, images or links to other sites). They show fake data only, are labelled as a sketch, are written only to `mockups/` in the plan folder, and are never published or uploaded anywhere.
- Once the gate passes, `powerops-implement` hands the build work to the Microsoft build skill for the product (named by plugin in the skill), found as a registered skill, through a routing skill, or in a checkout of power-platform-skills. Its environment-write rule names Microsoft's import tools too, and promotion between environments goes through `powerops deploy plan` and `deploy apply`.

## Plan files

```
docs/plans/<plan-name>/
  plan.md        the plan, with a header
  decisions.md   every question, the options offered and the answer, in order
  review.md      header lines, then review findings and how each was resolved
  mockups/       optional static HTML mockups
```

`plan.md` starts with a header: plain `key: value` lines, one per line with no nesting, between `---` fences. A small fixed reader parses it, not a YAML library.

```
---
status: draft
review: pending          # pending | done | skipped
signed-off: null         # date, written by `powerops plan sign-off`
signed-off-hash: null    # sha256 of the body below the header, written by the same command
environment: null        # target environment name, confirmed by the user before it is written
depth: standard          # quick | standard | deep, or null; how deep the questions go, set from the user's engagement
supersedes: null         # folder name of the earlier plan this one builds on, or null
---
```

`status` is one of `draft | approved | signed-off | implementing | done` (see [Workflow](#workflow)). The hash covers the body only, so header changes never affect it.

`review.md` starts with three lines:

```
plan-hash: <the body hash at review time, from `powerops plan status --json`>
model: <what the agent reports, or unknown>
fresh-context: yes | no
```

Plans are committed with the code, so they hold no tenant data:

- Before writing any plan file, the agent redacts URLs, GUIDs, emails and people's names from anything it copies in (answers, `plan facts` output, pasted text). It says so once, at the first write. `powerops plan status` scans the plan folder as a backstop.
- Only the **target** environment's name is written, and the user confirms it before it is written. Other environments are used to answer questions but are not written down, and are not offered as targets unless the user names them. (In the prototype test, both agents copied every environment name in the tenant into `plan.md`.)
- No tenant IDs or credentials.

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

Facts the environment can answer (the target environment, existing solutions) are read, not asked.

### Depth and versions

- **Depth.** `depth` says how far the rounds go. `quick`: one round of at most three questions, only what blocks the plan, with licensing and environments asked in it, then B is recommended. `standard`: the protocol above. `deep`: more rounds, plus edge cases, data volume and delegation, security roles and testability, and fuller Steps. A new plan starts at the depth of the plan it supersedes, else of the plan.md with the newest modification time that has one, else `standard`; the fast path sets `quick`. After each round the agent judges engagement from the answers: mostly one-word or option-number answers, "just pick" or a request to speed up move it one level down; long answers in the user's own words, follow-up questions or a request for more detail move it one level up. It changes at most once per round, writes the new value in the header, logs `depth: <old> -> <new> (<reason>)` in `decisions.md` and mentions it in one of the three change lines. A resumed plan keeps its `depth`.
- **Versions.** When a `signed-off`, `implementing` or `done` plan needs a new version (new scope, a v2, changes after implementation), the agent leaves it as it is and creates `docs/plans/<name>-v2/` (or the next free `-vN`) from a copy of its body, with `status: draft`, `review: pending`, no sign-off, the same `environment` and `depth`, and `supersedes: <previous folder>`. It asks only about what changes. A small edit to a signed-off plan that is not yet implementing still goes back to `approved` and is re-signed.

## Fast path

If the user asks to implement without a signed-off plan:

1. `powerops-plan` drafts `plan.md` from the request, the repo and `powerops plan facts`. Every guess goes under Assumptions, and `status` stays `draft`.
2. It asks one question: the route, with two or three options and the trade-offs, recommended first. It may ask one more, the publisher prefix, when `plan facts` cannot supply it. It asks nothing else.
3. Then it shows one lettered menu:
   - **A** approve the draft (then review and sign-off as usual)
   - **B** approve, skip the review and sign off now (recorded as `review: skipped`; the Assumptions list stands in for the coverage confirmation)
   - **C** ask question rounds instead

A route choice approves nothing, and a bare "approve" approves the draft only (A). The shortest path is a route plus B: two answers, one plan file, a sign-off the user gave. The plan can be one exchange long, but it always exists, and sign-off is always the user's.

## Review

- **Fresh context.** The parent session dispatches the reviewer and does any talking to the user. The reviewer reads only the plan files, the repo and `plan facts`, never the planning chat.
- **Never asks the user.** Subagents usually run without a person to answer; findings go into `review.md` instead.
- **Top model of the same provider.** "Top model" means the most capable model the agent offers for the provider of the current session. Users often have one provider only. No model ID is written into a committed file:

  | Agent | How the review runs |
  |---|---|
  | Claude Code | a Claude-specific copy of the skill generated at install, with `context: fork`, `background: false` and the tier alias `opus` |
  | Codex | the skill starts a subagent with no chat history (`spawn_agent` without forked turns), so the session model runs; verified in the v0.4.0 manual run |
  | Gemini CLI | a subagent in `.gemini/agents/` with the model left unset, so the session model runs |
  | Others, or an agent that cannot fork | the skill tells the user to open a new session and run `powerops-review-plan <name>` there |

- **If the user continues in place**, the review still runs and writes `fresh-context: no` and the actual model. The sign-off step shows those header lines, and the user accepts them or not. A review is never reported as fresh or top when it was not.
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
- **Output:** `review.md`: the three header lines, then findings by severity, numbered R1, R2... The user resolves or accepts each one before sign-off. Skipping the review is allowed only as an explicit choice at sign-off, and it is recorded as `review: skipped`.

## Pairing with `pac` and `powerops`

**Environment facts.** `powerops plan facts` runs two read-only `pac` commands itself and prints names only, as JSON. Plain `pac` output carries tenant IDs, emails and every environment name, and the agent should never see those. The code lives in `PowerOps.Cli` (a `PacRunner`), and skills never run `pac` directly.

| Fact | Source | Notes |
|---|---|---|
| Target environment name | `pac org who` | `pac auth who` is not used; its output is mostly tenant ID and email |
| Existing solutions | `pac solution list` | Unique name, version and managed flag. It does **not** return publishers or prefixes |
| Publisher prefix | asked in a question round | Unless a read-only query proves to work (untested: `pac env fetch` with FetchXML on the publisher table) |

Output, on success:

```json
{"schemaVersion": 1, "available": true, "environment": "<name>", "solutions": [{"uniqueName": "<name>", "version": "<version>", "managed": false}]}
```

On any failure (`pac` not on PATH, not signed in, a command fails or times out) it prints `{"schemaVersion": 1, "available": false, "reason": "...", "fix": "..."}` and still exits 0. The schema is [plan-facts.schema.json](schemas/plan-facts.schema.json). Rules:

- each `pac` command has a 30 s timeout;
- stdin is closed, and it never starts an interactive sign-in;
- raw `pac` output is never forwarded; only the fields above are printed;
- arguments are passed as a list, never through a shell string.

When `available` is false, the skill says what `reason` and `fix` say (for example: add the dotnet tools folder to PATH, run `pac auth create`) without running the fix, notes the assumption in `plan.md`, and carries on.

**New `powerops` commands** (offline like `analyze`; `plan facts` is the one exception that reads through `pac`, and `plan sign-off` and `skills install` write only the files they are asked to):

- `powerops skills install [--agent ...]` installs the skills (below).
- `powerops plan facts` prints the names-only environment facts above.
- `powerops plan sign-off <plan>` writes `status: signed-off`, the date and the body hash into the plan header. It refuses unless `status` is `approved` and `review` is `done` or `skipped`. A sign-off written by hand fails `plan status`.
- `powerops plan status [path]` checks every `docs/plans/*/plan.md` under `path` and runs in CI, not just in the agent. It never uses git or the network.

**What `plan status` checks:**

- the header parses, `status` and `review` are in their enums, `depth` is `quick`, `standard`, `deep` or empty, and a date is a date;
- `supersedes`, when set, is a plain folder name and names another plan under the same `docs/plans/` that has a `plan.md` (in every status);
- `signed-off`, `implementing` or `done` requires `signed-off` and `signed-off-hash`, and the hash matches the current body (otherwise: "changed since sign-off", or "run `powerops plan sign-off`" when the hash is missing);
- `signed-off` and later requires `review: done` with `review.md` present, or `review: skipped`;
- no file under the plan folder matches a URL, GUID or email pattern. A `<!-- plan-status: allow -->` comment on the line suppresses the match;
- `draft` and `approved` plans pass, and a repo with no plans passes.

| Exit code | Meaning |
|---|---|
| 0 | Clean |
| 1 | Violations |
| 2 | Usage error, or a malformed plan header |
| 3 | Internal error |

`--json` prints each plan's status, review state, `environment`, `depth`, `supersedes`, counted open questions (the list items under `## Open questions` and `## Questions for your admin`), body hash and violations. It warns, but does not fail, when `review.md`'s `plan-hash` differs from the current body (a stale review).

## Agent support

| Agent | Skill folder (project) | Choice tool | Notes |
|---|---|---|---|
| Claude Code | `.claude/skills/` | `AskUserQuestion` | Reads `AGENTS.md` only through `CLAUDE.md` |
| Codex | `.agents/skills/` | `request_user_input`: 1–3 questions, 2–3 options, **Plan mode only** by default | The numbered-text fallback worked in the prototype test; a resumed `codex exec` keeps the first call's sandbox and rejects `-s` |
| Gemini CLI | `.agents/skills/` or `.gemini/skills/` | `ask_user`: 1–4 questions, 2–4 options | Needs a confirmation to activate a skill |
| GitHub Copilot | `.github/skills/`, `.claude/skills/` or `.agents/skills/` | `ask_user` | **Autopilot answers `ask_user` itself**, so the planning skill stops under autopilot (principle 5) |
| Chat-only (web) | none | numbered text | User pastes the instructions and switches models by hand. Manual, with no gate |

**Supported:** Claude Code and Codex. Gemini CLI and Copilot are **provisional, untested**: the skills should work there, but no verification run has been done.

**Install.** `powerops skills install` writes `.agents/skills/powerops-*/` (Codex, Gemini, Copilot and most other agents) and `.claude/skills/powerops-*/` (Claude Code). Agents that read both folders may list a skill twice, so it writes only the folders for the agents it detects. It writes real copies, never links. Running it again is safe; it refuses to overwrite skill files that differ from the shipped ones unless `--force` is given. It never edits `AGENTS.md`.

**Portability.**

- The shared `SKILL.md` uses only the spec's portable front matter (`name`, `description`, `license`, `compatibility`, `metadata`). Agent-specific tuning lives in copies generated at install time.
- Descriptions stay short, with the trigger first. Codex reserves about 2% of the context window for the skill list, and Claude Code truncates descriptions at 1,536 characters.
- The Claude Code and Codex plugins (`plugins/claude/` and `plugins/codex/`, listed by `.claude-plugin/marketplace.json` and `.agents/plugins/marketplace.json`) hold generated copies: the Claude Code copies as `skills install` writes them to `.claude/skills/`, the Codex copies unchanged. A test keeps them equal to the shipped files. For a team, `skills install` stays the default.
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
- The gate guards against mistakes and drift, not against an agent that ignores its instructions. The pull-request review is the backstop.
- Model selection is best effort. Where an agent cannot switch models or fork a context, `review.md` records what actually ran, and the user may be asked to open a new session.
- Only Claude Code and Codex are supported. Gemini CLI and Copilot are provisional, untested.
- Codex's question tool is off outside Plan mode by default, so Codex users mostly get the numbered-text fallback.
- Question **quality** cannot be proven in CI, but the protocol's **shape** can: a scripted smoke test with a simulated user (as in the prototype test) checks round size, option order, `decisions.md` logging and the stop behaviour. It costs model credits, so it runs as a weekly scheduled job, not in per-build CI (see Decisions).
- HTML mockups are visual sketches, not canvas app source. Generating `.pa.yaml` from a plan is out of scope for the first version.

## Decisions (2026-10-19, maintainer)

1. **Smoke test schedule.** The simulated-user smoke test runs as a weekly scheduled job on the maintainer's model credentials, stored as CI secrets. It does not run in per-build CI. A manual run before each release is still allowed.
2. **v0.4.0 scope.** v0.4.0 ships with Claude Code and Codex supported. Gemini CLI and Copilot are marked provisional, untested.
3. **Sign-off by hand.** A sign-off written without the tool (no `powerops` on PATH, or skills installed through `npx skills add`) fails `plan status` with the hint "run `powerops plan sign-off`". There is no warn-only mode.

## Open questions

1. Can a read-only `pac` query return the publisher prefix (`pac env fetch` on the publisher table)? If not, the prefix stays a question.
2. Do all four agents ignore front matter fields they don't know? Unverified; until then the shared `SKILL.md` stays portable-only.
3. Should `powerops plan status` also link a plan to the `analyze` results of the solution it produced?
4. spec-kit compatibility: not in v1. A later `powerops plan export --spec-kit` is possible if people ask.

Evidence for this revision: a prototype test of `powerops-plan` (six scripted conversations in Claude Code and Codex), a survey of 28 skill repos, and an independent design review.
