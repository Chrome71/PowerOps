# Use PowerOps from an agent

PowerOps works with Claude Code, Codex and other agents in two ways: planning skills that keep a build behind a signed-off plan, and an MCP server whose tools run the `powerops` commands.

## Planning skills

PowerOps ships three agent skills that turn an idea into a reviewed, signed-off plan before any code or solution change is made:

- `powerops-plan` asks short question rounds (at most three questions, recommended answer first), logs every answer in `decisions.md`, drafts `plan.md`, and signs it off with you. Asked to build without a plan, it drafts one and asks only the route.
- `powerops-review-plan` reviews an approved plan in a fresh context against a Power Platform checklist and writes `review.md`. It never asks you questions.
- `powerops-implement` builds only from a signed-off plan whose body still matches its sign-off.

Run `powerops skills install` in your repo to add them. In a workspace whose own tooling rewrites `.claude/skills/`, run it in each child project, not at the workspace root, and start the agent there: the next rewrite removes skills installed at the root. Supported: Claude Code and Codex. Gemini CLI and GitHub Copilot are provisional and untested. The skills are also in [skills/](../skills/) for `npx skills add` (that CLI has telemetry on by default). Design: [docs/PLANNING-SKILLS.md](PLANNING-SKILLS.md).

For your own Claude Code or Codex setup, the skills also install as a plugin from this repo, beside Microsoft's plugins. Install the `powerops` CLI first ([Install](../README.md#install)): the plugin's skills call it and need it on PATH.

```sh
claude plugin marketplace add Chrome71/PowerOps && claude plugin install powerops@powerops   # Claude Code (in a session: /plugin marketplace add Chrome71/PowerOps)
codex plugin marketplace add Chrome71/PowerOps && codex plugin add powerops@powerops         # Codex
```

The plugin also starts the PowerOps MCP server: see [MCP server](#mcp-server).

For a team, `powerops skills install` stays the default: the skills then live in the repo, so every contributor and CI sees the same gate. Use one or the other in a repo; with both, each skill is listed twice.

With Microsoft's power-platform-skills or Dataverse-skills installed too, their skills build in Dev and PowerOps checks, plans and imports: the steps are in [docs/HANDOFF.md](HANDOFF.md).

## MCP server

`powerops mcp` is a Model Context Protocol server over stdio for Claude Code, Codex and other agents. Its tools run the [commands](COMMANDS.md) in-process and return what the command prints: its JSON, which follows its schema in [docs/schemas/](schemas/), or for `baseline` and `settings_generate` the file's text. A second text block, when there is one, holds PowerOps's own `error:` lines, then its `note:` and `warning:` lines (for example a link inside a solution that was not read); the text `pac`, `az` and the Web API print never reaches the agent.

- `analyze`, `rules`, `baseline`, `diff`, `test`, `settings_generate`, `settings_validate` and `plan_status` read only files. `baseline` and `settings_generate` return the file's text instead of writing it.
- `plan_facts`, `drift` (for an environment, not a snapshot file), `deploy_plan` and `deploy_apply` read a tenant through `pac` and `az`, as the commands do, so both must be signed in.
- `drift` needs at least two `envs`. Making a first snapshot from one environment (`powerops drift --env <env> --save <dir>`) is CLI-only: the tool has no save input.
- Two tools write. `deploy_plan` writes its plan file into the deploy folder when `save` names a new file, never over an existing one. `deploy_apply` is a dry run unless the call sets `apply: true`. Then the server plans again, and only when that passes shows a form with the plan's target, hash and zips; it imports only after the user types the target's name there. A decline, a cancel or another name imports nothing, and a client that can't show the form gets a refusal and the CLI command to run.

Every path must be absolute and local (no `\\host` or `//host` paths), and a value that starts with `-` or `@` is refused.

A string with a control character (a NUL, a line break, a tab), a string of more than 4,096 characters, more than 50 `paths` and more than 10 `envs` are refused too, and so is a string with an unpaired surrogate (half of a character written as `\ud800`). Each refusal is a tool error with an `error:` line, and nothing runs. The limits are not in the tools' input schemas; an agent that goes over reads the limit in the error. A `drift` entry with a `/` or `\` that is neither a URL nor an existing file is refused by `drift` with exit 2.

**What a caller can reach.** A `target` or `envs` value the caller chooses reaches any documented Dataverse host: `powerops` asks `az` for a token for that host, whatever the user named. Learn says the token's scope names the environment URL, gives no audience (`aud`) value for Dataverse and says nothing on using such a token at another organization's environment (read 2026-10-29), so whether a token for one environment would work at another is not known, and neither is what `pac org who --environment` does with a URL on a host that is not a Dataverse host. Nothing here changes the tool inputs for it. The user is asked in a form before an import, but not before `plan_facts`, `drift` or `deploy_plan` read a tenant, so the descriptions of those three, and of `deploy_apply` (which reads the target the same way without `apply`), tell the agent to use them only on an environment the user named, and a client that asks before each tool call (Claude Code does by default) is the check that remains.

**Register it.** The plugin ([Planning skills](#planning-skills)) starts the server in each session where it is enabled. It runs `powerops mcp` from PATH, so the server is the CLI you installed, at its version, and `powerops` must be on the PATH of the agent's process; `/mcp` in either agent shows a server that failed to start. Without the plugin:

```sh
claude mcp add powerops -- powerops mcp   # Claude Code, local scope; --scope project writes a .mcp.json the team shares, --scope user covers all your projects
codex mcp add powerops -- powerops mcp    # Codex; then set env_vars and tool_timeout_sec (below)
```

Use the plugin or your own entry. With both, and when your entry runs the same command, `powerops mcp`, Claude Code connects it once and prefers your own entry ([its docs](https://code.claude.com/docs/en/mcp#scope-hierarchy-and-precedence)), so the server runs as `powerops`, not as the plugin's `plugin:powerops:powerops`, and its tools are named `mcp__powerops__<tool>`. In Codex your own `[mcp_servers.powerops]` takes the place of the plugin's (seen with `codex mcp get` in Codex 0.163.0). What is not verified on Windows and macOS, including whether Claude Code and Codex start the plugin's `powerops` there, is listed in [FOUNDATION §8](FOUNDATION.md#8-research-topics-still-open).

**What asks first.** Claude Code asks before a tool call unless a permission rule allows it, such as `mcp__plugin_powerops_powerops__analyze` for the plugin's server or `mcp__powerops__analyze` for your own entry. Codex, in its default approval mode, asks before `deploy_plan` and `deploy_apply`. In the M20 run (Codex TUI 0.163.0 with app-server 0.165.0) it ran `analyze`, `diff`, `test` and `drift` on snapshot files without asking. Its code at `rust-v0.163.0` skips the question for every read-only tool, so `plan_facts`, and `drift` against an environment, which read a tenant, run without asking too (read in the code, not run). Don't allow `deploy_apply` in a rule: an agent asks you in chat before it sets `apply`, and the form is where you say yes.

**Answer the form yourself.** Whoever answers `deploy_apply`'s form gives the yes, so don't set up anything that answers forms for you, such as a Claude Code `Elicitation` hook or a Codex reviewer. An answer without the exact name is refused. In the M20 run, Claude Code 2.1.312 showed the form as a dialog with Accept and Decline, and Codex (TUI 0.163.0 with app-server 0.165.0) as one text field with no Decline: Esc cancels it, which imports nothing too. Codex's code at `rust-v0.163.0` passes this form to you even with its reviewer on, declines it under the `never` approval policy and cancels it in `codex exec` (read in the code, not run). `claude -p` cancels forms (seen in M19), so `apply` imports only in an interactive session; elsewhere run `powerops deploy apply --apply` yourself.

**Codex's environment.** Codex starts a server with only a few of its own environment variables, such as `HOME`, `PATH` and `USER` (`create_env_for_mcp_server` in its code at `rust-v0.163.0`). In the M20 run on Linux, `pac` then couldn't reach its saved sign-in and asked for a new one, so the Codex plugin also passes `DBUS_SESSION_BUS_ADDRESS` and `XDG_RUNTIME_DIR`. With your own Codex entry, add `env_vars = ["DBUS_SESSION_BUS_ADDRESS", "XDG_RUNTIME_DIR"]` under `[mcp_servers.powerops]` in `config.toml`.

**Timeouts.** `deploy apply` waits up to 60 minutes for each import. The Codex plugin sets `tool_timeout_sec = 3600`; with your own Codex entry, add it under `[mcp_servers.powerops]` in `config.toml`, because Codex's default can end the call while an import runs: 60 seconds in [its docs](https://developers.openai.com/codex/mcp), 300 in its code at `rust-v0.163.0` (`DEFAULT_TOOL_TIMEOUT` in [`codex-rs/codex-mcp/src/rmcp_client.rs`](https://github.com/openai/codex/blob/rust-v0.163.0/codex-rs/codex-mcp/src/rmcp_client.rs)). That code also stops the clock while its form is open (read in the code, not run). Claude Code's docs say it [ends a stdio call after 30 minutes](https://code.claude.com/docs/en/mcp#time-limits-on-mcp-tool-calls) with no response or progress; for a longer import, raise `CLAUDE_CODE_MCP_TOOL_IDLE_TIMEOUT` (milliseconds). In the M20 run its form stayed open for more than 34 minutes and the call didn't end. A Claude Code call still running after two minutes [moves to a background task](https://code.claude.com/docs/en/mcp#automatic-backgrounding-of-long-tool-calls) (`/tasks`), and its result arrives when it ends; a call waiting on the form stays in front. A call the client gives up on doesn't stop an import that has started: it runs to its end, and another `apply` call to that server is refused until then. If the client closes the server's input, the server finishes the import it is running, then exits (a test shows the server finishes the running import before it exits; that the client gets no report was seen in a probe, not in a test). If the client kills the server, `pac` keeps importing and no report comes back (not tested). Either way, run `deploy_apply` without `apply` to see where the target stands before trying again. One case is not fixed: under revision 2025-06-18 a server that waits on a form the client never answers keeps running after its input closes, until the client kills it, because that revision's form has no timeout, so a client that closes the input but never kills the process leaves the server running indefinitely; a later change could end the form's wait at end of input.
