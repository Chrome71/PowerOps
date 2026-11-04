# Decisions: m49-agent-diff

## Round 1

### Q1. Which component kinds and risk labels does `diff` add?
1. Two kinds, `agent` (its instructions) and `agentTool` (each tool), and two labels, `agent-instructions` and `agent-tool` (recommended)
2. One kind, `agent`, with added and removed tools in its detail
3. In your own words
Answer: 1. `SolutionDiff` compares one list per kind with `Keyed`, so a tool row can be added, removed or changed on its own and carry its own label, as a connection reference does. The labels follow the existing names: a noun for what changed, hyphenated. Option 2 would hide an added tool inside a changed agent's detail, where a reviewer scanning the risk column doesn't see it.

### Q2. What identifies an agent and a tool?
1. The agent's schema name, and the tool component's schema name, matched ignoring case like every other key (recommended)
2. The tool's schema name joined with its agent's
Answer: 1. Both are the names M48's rules print, and a tool component's schema name already names its agent in the observed exports (`<agent>.topic.<name>`). A tool that moves to another agent under the same schema name shows no change; that case is unobserved. `Name` stays empty: it is the display name when the key is an id, and neither key is one.

### Q3. What does a change of instructions look like?
1. `instructions changed`, never the text, when the two values differ by ordinal compare; a missing value (null) counts as a value (recommended)
2. The old and new text, cut to 100 characters
Answer: 1. Instructions run to 8,000 characters, so the text can't be shown usefully in a row, and a flow's changed definition is reported the same way ("definition changed"). Null means M48's reader found no single Custom GPT component with text, so a change to or from null is a real change in what the export holds. The compare is exact: a trailing line break that differs is a change, since the stored text changed.

### Q4. Do other agent fields go in the detail?
1. No: instructions for an agent; action, MCP flag and mode for a tool, with no change risk (recommended)
2. Also authentication, generative orchestration and the recognizer
Answer: 1. The M49 section asks for added tools and changed instructions. `analyze` judges the end state of the other three fields (PO027, PO029, the harness note), which is where a risky value is caught. The tool fields are the three the model holds, shown so that a tool moved to Maker mode isn't silent; PO030 already warns on the result.

### Q5. How does `diff` alone move to `schemaVersion` 2, and what else reads it?
1. A second constant, `JsonOutput.DiffSchemaVersion = 2`, used by `DiffReport` only; `JsonOutput.SchemaVersion` stays 1 for every other output and for the plan file (recommended)
2. Raise `JsonOutput.SchemaVersion` to 2 for every output
3. A constant inside `DiffReport`
Answer: 1. D9 moves `diff` only; option 2 would break every other consumer and the saved plan files that `DeployPlanFile` checks against the shared constant. Beside the shared constant, a reader of `JsonOutput` sees both. The version names the contract, so it is 2 for every `diff` output, with or without agent lines. Every place that reads or checks `diff`'s JSON or version, and what it needs:
- `docs/schemas/diff.schema.json`: `const: 2` and the four values (row 6).
- `docs/schemas/README.md`: the enum rule, the per-output versions and the migration line (row 14).
- `OutputSchemaTests`: no code change; it validates `diff-seed.json` and the new `diff-agents.json` against the schema, and fails either without `schemaVersion`.
- The goldens: `diff-seed.json`'s version line, and the new `diff-agents.*` (row 8).
- `DiffCommandTests`: the `--out` assertion goes to 2 (row 7).
- `action/`: `diff.sh` runs `diff --format markdown`; `annotate.sh` reads `analyze`'s JSON, `test.sh` `test`'s, `plan.sh` writes `deploy plan`'s Markdown, and `comment.sh` posts a file; none reads `diff`'s JSON or checks a version, so no change. The PR comment gains agent rows and their risk legend for a solution with agents.
- The MCP server's `diff` tool: it runs the CLI with `--format json` and returns the text, and its description names the schema file, not a version; no change, and the tool-list golden doesn't change.
- The skills: `powerops-verify` and `powerops-review-security` run `diff` as text; no skill reads its JSON; no change.
- `tests/eval`: the baselines hold `diff` text, and no case holds an agent; no change.
- `DeployPlanFile` and `SnapshotFile` keep their own checks at 1.

### Q6. Stop rules: is D9 answered, and can a new line reach an eval baseline?
1. D9 is answered and no eval case holds an agent, so M49 goes on (recommended)
Answer: 1. The person took D9's first option on 2026-11-02 (Phase 7 decisions Q9). `find tests/eval tests/fixtures -path '*bots*' -o -path '*botcomponents*'` finds nothing at `d113fc9`, and the baselines hold text output, so the agent blocks yield nothing on any eval case. `RoleEvalTests` is run after the build to show it.

### Q7. Which `pac copilot init` calls does the gate block?
1. Every `pac copilot init`, with or without `--environment` (recommended)
2. Only one with `--environment` or `-env`
Answer: 1. Learn's `pac copilot` page says `init --environment` imports a solution, and the flag's own description says the active auth profile's environment is used when it isn't given (M48 decisions Q11). The gate reads only the hook's JSON and never runs `pac`, so it can't tell whether a profile is active. A local scaffold is a build step in Dev, where the hooks are off. The route text says "can import", so it is true in both cases. pac's MCP server has no `copilot_init` tool, so no MCP route or matcher change is needed; the `Bash`, `Monitor` and `PowerShell` groups already send the command to the gate. Like the other route words, a mention such as `git commit -m "pac copilot init"` is blocked while the hooks are on.

### Q8. What do the docs say about skill pushes?
1. Correct the line: a skill's push or publish runs `node …/manage-agent.bundle.js` in Bash, which the gate reads and has no route for, so it passes; add no route (recommended)
2. Add a route for `manage-agent.bundle.js push` and `publish`
Answer: 1. `docs/HANDOFF.md` says the gate doesn't see "a write that runs no shell command", listing a skill's push through the language server. The `manage-agent` skill of skills-for-copilot-studio runs `node ${CLAUDE_SKILL_DIR}/../../scripts/manage-agent.bundle.js push` through Bash, so the gate does see the call and lets it through. The M49 section names only the `init` route, the bundle is experimental (its skill says so), and D2's deny-list adds routes milestone by milestone. A route for it is a follow-up. Apply in VS Code and the Copilot Studio MCP server's `manage_agents` run no shell command and no pac MCP tool, so those stay as written.

### Q9. Where does the post-import checklist go?
1. Step 11 of `docs/HANDOFF.md`, "An agent was imported", beside step 10 for a flow that is off, with the agent row pointing to it (recommended)
2. In the agent row's cell
Answer: 1. Step 10 is the precedent for a product's after-import step, and a table cell can't hold five items readably. `HandoffDocTests` counts the steps, so it moves to 1 to 11; no step is renumbered.

### Q10. Where are the agent live checks marked waiting?
1. In step 11, and as an open item in FOUNDATION section 8 (recommended)
2. In BUILD-PLAN only
Answer: 1. Step 11 is where a user meets them, and FOUNDATION section 8 is the list of open facts that a later milestone picks up, as the on-hold evaluation item is. The BUILD-PLAN row points at both.

### Q11. What does the PR title say about the contract change?
1. `feat!:`, with a `BREAKING CHANGE:` note in the body naming the migration line (recommended)
2. `feat:` with the note in the body only
Answer: 1. `release-please-config.json` sets `bump-minor-pre-major`, so a breaking change on 0.x bumps the minor version, the same v0.32.0 that a `feat:` gives, and the changelog lists it under breaking changes, where a consumer that validates the schema looks. No version file is edited.

## Log

- 2026-11-04: plan started at the request of the session that started this one: build M49 of Phase 7, build-order item 84, from the M49 section of `docs/plans/phase7-power-platform/plan.md`, D9 and the "Stage two" section, with the version design settled first
- read on 2026-11-04: AGENTS.md; the Phase 7 plan's stage two section, M48 and M49, D1, D2, D9, the Automation and Compatibility paragraphs and Steps 11 and 12; its decisions Q9; the M48 plan folder (layout, Q11) and its build hand-over; the BUILD-PLAN M49 row, item 84 and decision 48; the Phase 7 Copilot Studio and MCP research report; `SolutionDiff`, `DiffReport`, `JsonOutput`, `DiffCommand`, the MCP `diff` tool, `ImportCall`, `DeployPlanFile`, `SnapshotFile`, `diff.schema.json`, the schemas README, `OutputSchemaTests`, `DiffCommandTests`, `SolutionDiffTests`, `ImportCallTests`, `DeployGateCommandTests`, `HandoffDocTests`, `action/diff.sh`, `release-please-config.json`, `docs/HANDOFF.md`, `docs/COMMANDS.md` (`diff`, `deploy gate`) and `skills/powerops-plan/references/copilot-studio.md`
- Learn facts checked 2026-11-04 with the Learn MCP tools; the pages are in Assumptions
- the `copilot-studio` plugin and its `manage-agent` skill read through the GitHub API at `9c42d3a612c6e06389b6e5b4eed579aaed1a8b63` (MIT), for names and the command form only
- `tests/eval` and `tests/fixtures` hold no `bots/` or `botcomponents/` path (checked with `find`)
- depth: quick, as M48's was: the M49 section gives the change, and D9 the version
- questions answered by the building session, under the user's delegation of 2026-10-22 (autonomous milestone mode), with the Phase 7 plan's scope and the master session's brief
- areas not asked: users, automation, environments and ALM, and constraints; the change is to this repo, and the plan answers each or marks it not applicable
- approved 2026-11-04 by the building session under the user's delegation of 2026-10-22, for a fresh-context review; approved without asking: users, automation, environments and ALM, constraints
