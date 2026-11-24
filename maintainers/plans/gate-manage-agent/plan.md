---
status: done
review: done
signed-off: 2026-11-04
signed-off-hash: 72f984d0de8e3d220b363cddb108b384509c3c68c4353b9f1e65c0bfb8fc6ddd
environment: null
depth: quick
supersedes: null
---

# Gate route for the `manage-agent` skill's push and publish

## Goal

Block a Copilot Studio skill's push and publish in `powerops deploy gate`. M49 found that the `manage-agent` skill of microsoft/skills-for-copilot-studio (MIT, read at `9c42d3a612c6e06389b6e5b4eed579aaed1a8b63`) runs them in Bash as `node ${CLAUDE_SKILL_DIR}/../../scripts/manage-agent.bundle.js push` or `publish`. The gate reads that call, has no route for it and lets it through, and `docs/HANDOFF.md` and `docs/COMMANDS.md` say so (M49 decisions Q8). This is the follow-up Q8 named. It comes after M49 (merged as `4c9ceee`), because both change `ImportCall`. Decision 41 keeps the gate a deny-list, and this plan adds one route to it.

Done when:

1. `deploy gate` blocks `node <path>/manage-agent.bundle.js push` and `... publish` (also `node.exe`, and a Windows path) with one route line that asks for no rewrite, and exits only 0 or 2 (decision 39).
2. The bundle's seven read-only verbs pass when they are the word after the script: `pull`, `clone`, `changes`, `validate`, `list-agents`, `list-envs` and `auth`.
3. Any other form of a call to the bundle blocks as unreadable once the bundle's file name is written out, as `solution-settings.js` does today. That covers an option or an empty word before the verb, a variable or a wildcard in the verb, an unknown verb, `sh -c`, an option given to `node`, and a mention (decisions Q2). A variable or a wildcard in the bundle's own name hides the call, as `$PA push` does (decision 41), and the docs say so.
4. A read-only call to the bundle, or `solution-settings.js template`, that holds another marker after the script blocks as unreadable, so an import inside it in backticks or in a double-quoted `$(...)` can't pass (decisions Q3).
5. `docs/HANDOFF.md` and `docs/COMMANDS.md` list the route among the always-blocked writes, and no longer say that the skill's push passes.
6. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan are clean, and one `feat:` PR is open.

### Won't build

- Routes for the plugin's other bundles. `eval-api.bundle.js start-run` starts an evaluation, and `chat-with-agent.bundle.js` sends chat messages. Neither changes an agent, and evaluations are M17's.
- Reading the bundle's options (`--workspace`, `--environment-url`, `--agent-id`). The route blocks without a plan check, so it needs only the verb. Options are the part of an experimental bundle that is most likely to change (decisions Q1).
- A hook matcher change. No MCP tool runs the bundle, and the `Bash` and `Monitor` groups already send the call to the gate.
- Routes for Apply in the VS Code extension and the Copilot Studio MCP server's `manage_agents`: they run no shell command and no pac MCP tool, so the gate can't see them, and the docs keep saying so.
- Reading the bundle's name through a variable or a wildcard (`node "$S" push`): the gate reads names as written, and decision 41 accepts the same miss for `$PA push`. The docs name it.
- A BUILD-PLAN milestone row. This is a follow-up of M49, not a numbered milestone; the plan folder and the PR are its record.

## Users

- An agent or user with the documented hooks on: a skill's push or publish is now blocked, and so is `run-eval`'s push at the end of its evaluation loop. With the hooks on (step 7, the import), the user runs the push, or turns the hooks off for that step after saying yes to it, as for `pac copilot push`.
- The same agent, running the skill's reads (`pull`, `clone`, `changes`, `validate`, `list-agents`, `list-envs`, `auth`): no change, unless a word after the script spells a marker (an option value such as `"my copilot push env"`), which blocks as a mention does. Those calls block today too. A `solution-settings.js template` call with such a word passed and now blocks: reword it.

## Data

Line numbers are those of `4c9ceee`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | `ImportCall`: `RouteManageAgent = "copilot-studio's manage-agent push and publish change an agent that no plan holds"`, added to `Routes` | `src/PowerOps.Core/Deploy/ImportCall.cs:37,44` | row 4 |
| 2 | `Markers()` gains `manage-agent\.bundle`, so a call that the node branch doesn't recognize blocks as unreadable. The pattern also catches the name without `.js`, which `node` resolves to the same file | `src/PowerOps.Core/Deploy/ImportCall.cs:80` | row 4 |
| 3 | The `node` branch of `ReadShell`. A `clean` flag is true when the words after the script hold no marker. `manage-agent.bundle.js` with the word after it `push` or `publish` (ordinal, as the bundle's `switch` compares) gives row 1's line. With the word after it one of the seven read-only verbs and `clean`, the call is recognized and passes. Any other call is not recognized, so row 2's marker blocks it. `solution-settings.js template` passes only when `clean` too (decisions Q3) | `src/PowerOps.Core/Deploy/ImportCall.cs:331-351` | row 4 |
| 4 | `ImportCallTests`. Pass: each read-only verb in the skill's own form (`node ${CLAUDE_SKILL_DIR}/../../scripts/manage-agent.bundle.js <verb> --workspace /x/agent`), `pull ... \| jq .status`, and `pull ... 2>&1`. Route: the skill's push and publish lines, `node.exe "C:\x\manage-agent.bundle.js" push`, and `push --force`. Route, not unreadable, added to the fact for always-blocked routes. Can't-read: `--workspace /x push`, `"" push`, `"$V"`, `Push`, no verb, `delete`, `node --no-warnings <path> push`, `X=1 node <path> push`, `sh -c 'node <path> push'`, `node <path>/manage-agent.bundle push`, `grep -r manage-agent.bundle.js .`, ``pull --workspace "`pac solution import ...`"``, ``solution-settings.js template --env "`pac solution import ...`"``, and the same two with a double-quoted `$(...)` (`pac solution import` after `template`, `pac copilot push` after `pull`). Known misses, which pass: `node "$S" push` and `node x/manage-agent.bundl?.js push`. PowerShell: `node <path>/manage-agent.bundle.js pull` blocks with the PowerShell line. A Bash case of the skill's push in the gate's write-route theory: exit 2, the route line and the import guidance on stderr, and the target not read | `tests/PowerOps.Core.Tests/Deploy/ImportCallTests.cs:90-123,236-380,611-631,633-640`; `tests/PowerOps.Cli.Tests/DeployGateCommandTests.cs:199-216` | itself |
| 5 | `docs/HANDOFF.md`. The always-blocked writes gain the `copilot-studio` plugin's `manage-agent` push and publish (`node …/manage-agent.bundle.js push` or `publish`, which `run-eval` runs too). Its other verbs pass, and any other form of the call is blocked as unreadable. The mention sentence gains `manage-agent.bundle`. The "doesn't see" bullet drops its last sentence, which says the skill's push passes. The bullet on commands built from variables names the bundle's name in a variable or with a wildcard | `docs/HANDOFF.md:96,102,104` | none, docs only |
| 6 | `docs/COMMANDS.md`, `deploy gate`, three edits: the always-blocked list gains the same route, with one clause that the read-only verbs pass when they are the word after the script; the mention examples gain `ls scripts/manage-agent.bundle.js`; and the agents sentence keeps only Apply in VS Code and `manage_agents` | `docs/COMMANDS.md:105` | none, docs only |

## Automation

None. `deploy gate` gains a route, with no new command, option or tool.

## Screens

None.

## Environments and route

None. No step reads or writes a tenant, and no `pac`, `az` or `node` call is made against an environment. The bundle is read as text through the GitHub API and never run.

## Solution and publisher

None.

## Constraints

- AGENTS.md: tests first, the smallest change, no dependency, fake values in tests, no version or changelog edits, Conventional Commits, `dotnet format` clean.
- A hook command exits only 0 or 2 (decision 39). The new route is one more always-blocked line, and an unreadable call already exits 2.
- No skill text changes, so `powerops-verify` and `powerops-review-security` keep their blob ids, and no golden or eval baseline moves.
- Open sources only: the skill and the bundle's source are MIT, and they are read for the command form and the verb list only.

## Assumptions

- The MIT `skills-for-copilot-studio` repo at `9c42d3a612c6e06389b6e5b4eed579aaed1a8b63`, read on 2026-11-04. `skills/manage-agent/SKILL.md` runs every verb as `node ${CLAUDE_SKILL_DIR}/../../scripts/manage-agent.bundle.js <verb> --option value ...`, with the verb as the first word after the script, and allows only `Bash(node *manage-agent.bundle.js *)`. `skills/run-eval/SKILL.md` allows `manage-agent.bundle.js push` and `pull` the same way, and `skills/clone-agent` and `skills/validate` run `clone`, `list-envs`, `list-agents` and `validate`. The skill says the scripts are "under active development".
- The bundle's source, `scripts/src/manage-agent.js` at that commit. `parseArgs` takes as the verb the first argument that doesn't start with `--` and isn't the value of a known option. An empty argument doesn't hold the place, so `--workspace /x push` and `"" push` both run push. `main` matches the verb with a case-sensitive `switch` over nine verbs: `auth`, `push`, `pull`, `clone`, `changes`, `publish`, `validate`, `list-agents` and `list-envs`. Any other verb exits with "Unknown command". `push` calls the language server's `syncPush`, and `publish` calls Dataverse's `PvaPublish`. The skill says publishing makes the agent live for every user it is shared with. The other seven verbs read, or write local files or the local token cache.
- The tokenizer of `ImportCall` keeps a backtick inside a word, and inside double quotes it keeps a `$(...)` in the word too; only an unquoted `(` ends the segment. A recognized segment's text is blanked before the marker scan. So on `4c9ceee`, ``solution-settings.js template --env "`pac solution import ...`"`` and the same with `"$(pac solution import ...)"` exit 0 from `deploy gate`, and `manage-agent.bundle.js pull` with either word exits 2, because nothing recognizes that call. Both were checked with the built CLI on 2026-11-04 (the `$(...)` form by the review, R1).
- The PowerShell tool blocks any command that matches a marker, so with row 2 every call to the bundle in PowerShell blocks. The skill allows only Bash.
- Environment facts were not read: this plan changes this repo and targets no environment.

## Open questions

## Questions for your admin

## Steps

1. This plan, a review in a fresh context, fixes, sign-off.
2. Row 4 and see it fail; then rows 1 to 3 until it passes.
3. Rows 5 and 6.
4. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan (`bash reports/leakscan.sh origin/main <this branch's commit>` from the main checkout); push and open one `feat:` PR whose body names the route, the read-only verbs that pass, and the `template` fix.
