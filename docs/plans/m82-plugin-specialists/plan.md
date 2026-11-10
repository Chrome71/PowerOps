---
status: implementing
review: done
environment: null
depth: quick
supersedes: null
signed-off: 2026-11-09
signed-off-hash: 5e2b0ba7a1ca4662222d08bb9568eaf95cc76fa4f205040853c2afd59103024f
---

# M82: Ship the six specialists in the Claude plugin, self-contained

## Goal

Fix finding F5 of the PowerOps 1.0 trial (2026-11-08), as section 3 of the agreed 1.0 fix plan sets out (amendment A12), under the person's answer to Q4 ("Yes, advisory", Round 0 in `decisions.md`):

| Id | Finding | This plan |
|---|---|---|
| F5 | The six Power Platform specialists exist only in `.claude/agents/` of this repo. Their shared rules are the "Power Platform specialist team" section of the repo's `CLAUDE.md`, which names repo-only agents, saved workflows and private research notes, so a plugin user has none of them | The Claude plugin ships `plugins/claude/agents/powerops-{apps,automate,bi,design-lead,workflow-logic,copilot-studio}.md`, each written by a generator in the test project from its repo file plus one shared-rules block taken from `CLAUDE.md`, with every repo-only reference rewritten (rows 1 to 3). Each copy says it is advisory and record-only, and claims no effectiveness |

Done when:

1. `plugins/claude/agents/` holds exactly the six files, each equal to what the generator makes from `.claude/agents/<name>.md` and `CLAUDE.md`. `POWEROPS_UPDATE_GOLDEN=1 dotnet test` writes them (and deletes any other file there); a normal run fails, naming the file, when a copy is missing, extra or different.
2. No copy mentions `docs/research`, `.claude/`, `CLAUDE.md`, `AGENTS.md`, `this repo`, `M38`, a saved workflow command (`/review-pr`, `/implement-feature`, `/design-panel`, `/verify-research`) or a repo-only agent (every `.claude/agents/*.md` name other than the six). Every `docs/` in a copy is `docs/plans/` or follows the repository URL from `Directory.Build.props` plus `/blob/main/`, and every `skills/` or `references/` path is a `${CLAUDE_PLUGIN_ROOT}/…` path to a file or folder that exists under `plugins/claude/`. A test checks this on the six shipped files, and every rewrite of rows 2 and 3 except (j) throws when its target is missing from a source it applies to, so a reworded source fails the run instead of shipping a half-rewritten copy.
3. Each copy's frontmatter keeps the source's fields and values, except that `description` gains the sentence `Advisory and record-only (its findings never block).` Each body ends with a `## Shared rules` section: a fixed lead-in (row 3) for the specialist reading it, then the reply order, the tenant rules and the routing table, the record-only rule with `No evaluation has measured what these specialists find`, and `subagent_type: "powerops:<name>"`. Nothing claims how well they work.
4. `claude plugin validate plugins/claude` passes with no error for the agents (run locally; CI has no `claude`).
5. `plugins/claude/.claude-plugin/plugin.json`'s description names the six advisory specialists; `version` is unchanged. `docs/USE-WITH-AGENTS.md` says what ships, how to start one, that they are advisory and unmeasured, and what is not checked. `AGENTS.md` (layout and "Update goldens") and the intro of `CLAUDE.md`'s "Claude Code extras" name the generated copies.
6. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m82-plugin-specialists` and the leak scan are clean.

### Won't build

- Specialists in the Codex plugin, or in `powerops skills install`. Codex plugin agents are not checked (the fix plan's scope); `skills install` writes skills only.
- A specialist shipped as a skill. `CLAUDE.md`'s rule keeps that behind an M38-style eval (Q4's answer covers the plugin's agents only).
- Any claim that the specialists find what the baseline review misses. No eval has measured them.
- A change to what the repo's own agents say or do. The generator rewrites only the copies; the sources keep their repo hand-offs (researcher, implementer, reviewer, research notes).
- Shipping anything from `docs/research/agent-team-2026-11/` (private, git-ignored).
- A change to `plugin.json`'s `version`, the Codex manifest, the kept roles, their plugin copies or `tests/eval`.
- Trimming the copies' `tools` lists to the MCP servers a plugin user is likely to have. A listed tool of a server the user lacks is not documented either way (Assumptions); the second trial's clean install is the check.

## Users

- A Claude Code user who installed the `powerops` plugin and asks for Power Platform planning or review help; their main session sees the six agents as `powerops:<name>`.
- A maintainer of this repo who edits a specialist or `CLAUDE.md`'s specialist section and regenerates the copies by the "Update goldens" recipe.

## Data

Line numbers are those of `278b136`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | The generator, in the test project: `PluginAgents.Specialists` (the six names, in one array) and `PluginAgents.ForPlugin(string name, string source, string claudeMd)`, a pure function of the two texts. The caller reads both with `\r\n` replaced by `\n`, as `Files` does for the shipped side (a Windows checkout may give CRLF). It splits the source at its frontmatter (throws when there is none), appends ` Advisory and record-only (its findings never block).` to the `description:` line (throws when there is no such line), applies the body rewrites of row 2, and appends `\n## Shared rules\n\n` plus `SharedRules(claudeMd)` (row 3). Every rewrite except (j) goes through one helper `Replace(text, old, new, file)` that throws `InvalidOperationException` naming the file and the missing text when `old` is absent; a rewrite named for some sources applies to those sources only | new `tests/PowerOps.Cli.Tests/PluginAgents.cs` | `PluginManifestTests` (row 4) |
| 2 | Body rewrites, each through `Replace` (row 1) except (j): (a) ``Follow `AGENTS.md` and the "Power Platform specialist team" section of `CLAUDE.md`, which hold the rules every specialist shares.`` → `Follow the shared rules at the end of this file, which every specialist shares.`; (b) the sentence ``Sources for the checklist are in `docs/research/agent-team-2026-11/<file>.md`.`` with the space before it is removed, the file being the one each source names (apps, automate, bi, design, workflow, copilot); (c) in the "Hand off to" list, the one line that starts `- powerops-researcher` or ``- `powerops-researcher` `` (each source has exactly one; throws otherwise) gets `The person` for the name and ends after its first `unsourced.`; a list line that starts with `powerops-implementer` or `powerops-reviewer` (with or without backticks) is removed: apps, automate and workflow-logic have such lines and the removal throws there when none is found; bi, design-lead and copilot-studio name both on the researcher line, which the cut after `unsourced.` already drops, so the removal is not applied to them; (d) ``The brief's fields and the reply format are in the "Power Platform specialist team" section of `CLAUDE.md`.`` → `The brief's fields and the reply format are in the shared rules below.`; (e) ` (AGENTS.md rule)` is removed in each source that has it (all six); (f) design-lead: `` You are not `powerops-designer`, which designs code changes to PowerOps itself.`` is removed, and ``Read `AGENTS.md`, `skills/powerops-plan/SKILL.md` and `skills/powerops-plan/references/route.md` first`` → ``Read `skills/powerops-plan/SKILL.md` and `skills/powerops-plan/references/route.md` first``; (g) copilot-studio: ``step 11 of `docs/HANDOFF.md` `` → `step 11 of PowerOps' HANDOFF guide (<RepositoryUrl>/blob/main/docs/HANDOFF.md) ` with the URL from `Directory.Build.props` (`main`, because the plugin's own version is `0.0.0` until the first release and has no tag to link); (h) automate: ``the flow rules in `docs/rules/` `` → ``the flow rules (`powerops rules`) ``; (i) bi: ``(`powerbi-authoring-local`, registered with `--readonly`, which is what blocks its writes)`` → ``(`powerbi-authoring-local`, which blocks writes only when it is registered with `--readonly`; if it isn't, or you can't tell, say so under Not checked and use only the reads listed here)``; (j) last, in every source, a replace-all that never throws (apps and automate have no such path; `RepoOnlyReferences` is its backstop, since it flags any bare `skills/` or `references/`): each `` `skills/`` → `` `${CLAUDE_PLUGIN_ROOT}/skills/`` and each `` `references/`` → `` `${CLAUDE_PLUGIN_ROOT}/skills/powerops-plan/references/`` (every such reference in the six is to the `powerops-plan` skill: design-lead's `route.md`, `SKILL.md` and `canvas.md`, bi's `power-bi.md`, workflow-logic's `data.md`, copilot-studio's `copilot-studio.md`); the bare names `route.md` and `canvas.md` that follow a full path in design-lead stay | `PluginAgents.cs` | row 4 |
| 3 | `SharedRules(claudeMd)`: first a fixed lead-in paragraph (quoted here with its own code spans): "These are the team's rules, written for the main session that briefs you: where they say "you", they mean that session. Read them for the brief you should get and the reply you owe. A brief with no `tenant:` line means `tenant: offline`, and one with no `authority` line grants none. A brief that names no phase or no plan folder gets `state: blocked`, naming what is missing. The `powerops-plan` skill's files are in `${CLAUDE_PLUGIN_ROOT}/skills/powerops-plan/`, its reference files in `${CLAUDE_PLUGIN_ROOT}/skills/powerops-plan/references/`."; then the lines of `CLAUDE.md` after the `### Power Platform specialist team` heading and before the line that starts `Saved workflows in` (throws when either is missing), with each of these rewrites through `Replace`: the first paragraph's last two sentences (from `Their research and sources are private notes`) → `No research notes ship with the plugin: a specialist lists each claim it can't source on Microsoft Learn under hand-offs, for the person to check, or marks it unsourced.`; the paragraph `The specialists load this file too, …` removed (the lead-in takes its place); the routing table's row `A Microsoft claim to check` → a row with `A Microsoft claim to check`, `the person, from the specialist's hand-offs` and `none`, and its row `A change to PowerOps itself` removed; `` `subagent_type: "<name>"` `` → `` `subagent_type: "powerops:<name>"` ``; in the reply format, `each claim for powerops-researcher` → `each claim for the person to check`; `In this repo the routing table` → `Under PowerOps the routing table`; the whole `Record-only until measured` bullet → `- Record-only until measured: specialist findings never block a plan, a build or an import. No evaluation has measured what these specialists find; treat their findings as advice for the person.`; and after it one added bullet: "- Never run `powerops deploy apply --apply` or `powerops settings apply --apply`, or call the MCP tool `deploy_apply` with `apply: true`: only the person says yes to an import, in the tool's form or by running the command." | `PluginAgents.cs` | row 4 |
| 4 | Tests, first. `Plugin_agents_are_the_generated_specialists`: in update mode it deletes other files in `plugins/claude/agents/` and writes the six; then `StaleAgentCopies(dir)` must be empty. `StaleAgentCopies(dir)` returns, in ordinal order, each name that is missing, extra or different from `ForPlugin`'s text. `A_changed_missing_or_extra_agent_copy_is_stale`: a temp copy of the folder with one file changed by a character, one deleted and one extra file gives exactly those three names. `Plugin_agents_ship_no_repo_only_reference` iterates `PluginAgents.Specialists` and reads each shipped file (a missing file fails): `RepoOnlyReferences(text)` must be empty, the body has one `## Shared rules` heading, `Record-only until measured`, `No evaluation has measured`, `subagent_type: "powerops:<name>"` and the `deploy_apply` bullet; frontmatter `name:` equals the file name, and every other frontmatter line equals the source's except `description:`, which is the source's plus the sentence. `RepoOnlyReferences(text)` lists each hit of: `docs/research`, `.claude/`, `CLAUDE.md`, `AGENTS.md`, `this repo`, `M38`, the four workflow commands (from `.claude/workflows/*.js` file names, with a leading `/`), a repo-only agent name (`.claude/agents/*.md` names minus the six, matched as whole names); a `docs/` not followed by `plans/` once `<RepositoryUrl>/blob/main/docs/` is taken out; a `skills/` or `references/` not inside a `${CLAUDE_PLUGIN_ROOT}/` path; and a `${CLAUDE_PLUGIN_ROOT}/<path>` with no such file or folder under `plugins/claude/`. `Repo_only_references_are_found`: on crafted strings it finds a bare `docs/HANDOFF.md`, a bare `references/data.md`, `powerops-researcher` and a `${CLAUDE_PLUGIN_ROOT}/skills/nope.md`, and finds nothing in the URL form or `docs/plans/x`. `ForPlugin` throws for a source without frontmatter and for a source whose rewrite target is missing (one sentence of (a) changed), and `SharedRules` throws for a `CLAUDE.md` without the section | `tests/PowerOps.Cli.Tests/PluginManifestTests.cs` | the tests themselves fail before rows 1 to 3 exist |
| 5 | The copies, written only by `POWEROPS_UPDATE_GOLDEN=1 dotnet test`, then read in full once by the building session and the work review | `plugins/claude/agents/*.md` (new) | row 4; `claude plugin validate plugins/claude` |
| 6 | `plugin.json` description: after the sentence on `powerops-review-security`, `It also ships six advisory Power Platform specialist agents (apps, automate, BI, design lead, workflow logic, Copilot Studio) that plan and review alongside the skills; their findings never block.` `AssertManifest` is unchanged (the Codex manifest doesn't get the sentence); `Claude_marketplace_lists_the_powerops_plugin` asserts the Claude description contains `specialist agents` | `plugins/claude/.claude-plugin/plugin.json`; `PluginManifestTests.cs` | the existing manifest tests and `Plugin_version_is_the_release_version` |
| 7 | Docs. `USE-WITH-AGENTS.md`: a subsection "Specialist agents (Claude Code plugin)" after the plugin install text: the six and what each covers (one line each, from their descriptions); how to start one: as `powerops:<name>` (the Agent tool's `subagent_type`, or `@agent-powerops:powerops-apps`, the plugin mention form in Claude Code's plugin docs), with a brief that names the phase (plan-advice, build-brief, review or post-import), the plan folder, the sections it owns, one objective, the inputs and a `tenant:` line (no line means offline); they run on Opus at high effort (`model: opus`, `effort: high`), so a session on a smaller model pays Opus rates for them; advisory and record-only, not measured by any eval; they read and propose, and never write to a tenant; their `tools` lists name Microsoft Learn's MCP server and the product servers (Canvas Authoring, FlowAgent, Power BI Authoring local, Copilot Studio, the model-apps and Power Pages Playwright servers) and those tools are absent without the servers; `powerops-bi` relies on the Power BI Authoring local server being registered with `--readonly` to block writes; not checked: a `tools` list naming a server the user lacks, and Codex. `AGENTS.md`: the `plugins/` layout line and step 1 of "Update goldens" name `plugins/claude/agents/` as generated from the six specialists and `CLAUDE.md`'s specialist section. `CLAUDE.md`: one sentence in the "Claude Code extras" intro (outside the extracted section): the Claude plugin ships generated copies of the six in `plugins/claude/agents/`, so editing a specialist or the section below means regenerating them by the recipe, and in this repo the project agents (bare names) are the ones to spawn | `docs/USE-WITH-AGENTS.md`; `AGENTS.md`; `CLAUDE.md` | read in the work review; after this step a normal `dotnet test` shows the copies unchanged (the sentence is outside the section); `DocsDriftTests` and `InstallLineTests` pass |

## Automation

None. Nothing new runs on its own.

## Screens

None.

## Environments and route

None. Plugin files, tests and docs change in this repo; no tenant read or write runs in this milestone.

## Solution and publisher

None.

## Constraints

- AGENTS.md: the smallest change, new logic with tests, Conventional Commits, no version or changelog edits, no tenant values. The generator lives in the test project and writes only in update mode, as the skill copies' writer does; nothing in `src/` changes, so `WriteSitesTests` is unaffected.
- `plugin.json`: the description only, never `version`.
- Nothing from `docs/research/` ships or is read by the generator.
- The kept roles, their plugin copies and `tests/eval` stay untouched.
- `docs/USE-WITH-AGENTS.md` is also edited by M80 in parallel: the merge rebases on `main` under the lock.

## Assumptions

Checked 2026-11-09 by a `claude-code-guide` subagent against code.claude.com and locally:

- Claude Code loads `agents/*.md` at a plugin's root with no `plugin.json` field, and names each `<plugin>:<name>`, here `powerops:powerops-apps` (code.claude.com/docs/en/plugins/manifest-reference, plugins/components). Project agents in `.claude/agents/` rank above plugin agents for one name (sub-agents, "Choose the subagent scope"); the two names differ here, so in this repo both load.
- Plugin agents support `name`, `description`, `model`, `effort`, `tools` and `disallowedTools`; `permissionMode`, `hooks`, `mcpServers` and `initialPrompt` are ignored for them (plugins/components). The six sources use none of the ignored fields.
- `tools` takes MCP wildcards such as `mcp__microsoft-learn__*` (sub-agents). What happens when some listed tools resolve and others (a server the user doesn't have) don't is not documented; when none resolve, Claude Code refuses to start the agent. Each copy lists `Read`, `Grep`, `Glob` and `Bash`, which always resolve. Observed: in this repo's sessions the project specialists, whose lists name servers that load only after a sign-in, appear in the agent list.
- `${CLAUDE_PLUGIN_ROOT}` is substituted anywhere in a plugin agent's Markdown body, but is not in the environment of the agent's Bash commands (plugins/manifest-reference, "Where each variable resolves").
- A subagent also loads the CLAUDE.md hierarchy of the main conversation (sub-agents, "Manage subagent context"), so in this repo a plugin copy reads both its shared rules and `CLAUDE.md`; they agree, except the copy's routing table names the person where the repo names `powerops-researcher`.
- `claude plugin validate` (2.1.319) validates agent frontmatter: a temp plugin with the six source files passed, and a broken frontmatter failed with "YAML frontmatter failed to parse".
- Codex plugin agents: not checked (out of scope).
- Risk: `powerops-bi`'s `tools` grant every tool of `powerbi-authoring-local`; on this machine it is registered with `--readonly`, which blocks its writes, but a plugin user's registration may not be. Rewrite (i) makes the copy say so and fall back to the listed reads, and `USE-WITH-AGENTS.md` states the requirement; the prose is the only guard where the server is not read-only.
- A plugin user's main session sees only each agent's `description`, never the shared rules, so it learns the brief's fields from `USE-WITH-AGENTS.md` or not at all; the lead-in of row 3 makes a specialist treat a missing `tenant:` line as offline and block on a missing phase or plan folder.
- The plan and `decisions.md` name public documentation URLs (code.claude.com, the repository's GitHub URL); they hold no tenant value.

## Open questions

## Questions for your admin

## Steps

1. This plan and `decisions.md`, the Fable plan review and a fresh-context `powerops-review-plan` review into `review.md`, fixes, and the sign-off.
2. Rows 1 to 5: the tests of row 4 first (seen failing), then `PluginAgents.cs`, then the copies by the recipe "Update goldens", and `claude plugin validate plugins/claude`.
3. Rows 6 and 7, after step 2, by the same single writer (both edit `PluginManifestTests.cs`, and `CLAUDE.md` is a generator input): `plugin.json`'s description with its assertion, and the three docs.
4. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m82-plugin-specialists` and the leak scan (`bash reports/leakscan.sh origin/main <branch>`, from the main checkout); commit locally.
