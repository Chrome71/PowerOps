---
status: signed-off
review: done
signed-off: 2026-10-28
signed-off-hash: 03b18f888e3569e5914c61123d48c36f5e534acd84a287f8f0cbdf401f0fd8e7
environment: null
depth: quick
supersedes: null
---

# M20: agents register the server

## Goal

Build BUILD-PLAN milestone M20 (build-order item 45, release label v0.19.0): both M13 plugins register `powerops mcp`, the README gets a section "Use from an agent (MCP)", and a manual run checks the Phase 5 done-when in an interactive Claude Code session and an interactive Codex session. The spec is the BUILD-PLAN section "Phase 5: agents (M18–M20)": the M20 row, decisions 27 and 28, the research basis (Claude Code and Codex) and the verification records of M18 and M19. This plan settles five things the row leaves open: whether a Codex plugin's server entry takes `tool_timeout_sec`, whether a bare `powerops` from PATH works as a plugin server command in Claude Code, how the manual run installs the plugins and puts the packed CLI on PATH, why M19's Codex form answer never reached the server, and the exact steps of the manual run.

Done when:

- `plugins/claude/.mcp.json` and `plugins/codex/.mcp.json` each declare one server, `powerops`, that runs `powerops` with the one argument `mcp`; the Codex entry also sets `tool_timeout_sec`. `plugins/codex/.codex-plugin/plugin.json` has `"mcpServers": "./.mcp.json"`. Both plugin descriptions mention the server. No `version` field changes.
- `PluginManifestTests` checks those files instead of the absence of a server, and passes.
- `claude plugin validate --strict` passes on the repo root and on `plugins/claude`.
- The README has the section "Use from an agent (MCP)" (Automation).
- The manual run (Environments and route) is done and recorded in the BUILD-PLAN verification record, or its tenant steps are marked not verified with the reason (decision 28). Nothing is imported, and no `--apply` runs.
- `dotnet build --no-incremental` (0 warnings), `dotnet test`, `dotnet format --verify-no-changes`, `dotnet pack src/PowerOps.Cli -c Release -o artifacts` and `powerops plan status .` are clean.

## Users

- Agents in Claude Code and Codex, which get the server's 12 tools when the plugin is enabled.
- The person at the keyboard, who installs the plugin and the CLI, answers the clients' approval prompts, and is the only one who can type the target's name into `deploy_apply`'s form.

## Data

**The server files.** Both are new, at the plugin root, in the shape both clients read (`{"mcpServers": {<name>: <entry>}}`):

```json
{
  "mcpServers": {
    "powerops": {
      "command": "powerops",
      "args": ["mcp"]
    }
  }
}
```

`plugins/codex/.mcp.json` is the same plus `"env_vars": ["DBUS_SESSION_BUS_ADDRESS", "XDG_RUNTIME_DIR"]` and `"tool_timeout_sec": 3600` in the entry. Nothing else: no `type` (both clients default to stdio), no `env`, no `cwd` (every path input is absolute, M18), no `${CLAUDE_PLUGIN_ROOT}` (the server is the installed CLI, decision 27). Codex starts a stdio server with only HOME, LOGNAME, PATH, SHELL, USER, LANG, LC_ALL, TERM, TMPDIR and TZ from its own environment, plus the entry's `env_vars` (`create_env_for_mcp_server`, `rmcp-client/src/utils.rs`). The tenant tools run `pac` and `az`, so on 2026-10-28 this session started the packed `powerops`, `pac auth list` and `az account show` (local commands, no tenant call) under `env -i` with just those variables: all three ran. The manual run showed that this is not enough on Linux (amended 2026-10-28): in Codex, `deploy_plan` failed with `pac org who` exit 1. This session then ran `pac org who` against Test with the full environment (exit 0) and under `env -i` with Codex's list (exit 1, asking for interactive sign-in); adding either `DBUS_SESSION_BUS_ADDRESS` or `XDG_RUNTIME_DIR` made it run, so `pac` reaches its saved sign-in through the session bus (inferred from that). `az account get-access-token` ran without them. So the Codex entry passes both names in `env_vars`; Codex copies them from its own environment and skips one that is unset. The Claude entry needs none: Claude Code passes its whole environment.

**Settled before this plan** (`decisions.md` Q1 to Q3 and Log; observed on 2026-10-28 with a `dotnet pack` of this branch installed with `--tool-path` into a scratch folder, and prototypes of both plugin folders outside the repo):

- Claude Code 2.1.303 starts a bare `powerops` from PATH. `claude -p --plugin-dir <prototype> --no-session-persistence` with the tool path first on PATH listed the 12 tools as `mcp__plugin_powerops_powerops__<tool>`, the scoped name its docs give plugin servers (`plugin:<plugin>:<server>`). `claude plugin validate --strict` passed on the prototype with the `.mcp.json` above.
- Codex 0.163.0 reads a plugin's server entries with the same `RawMcpServerConfig` as `config.toml` (`codex-mcp/src/plugin_config.rs` `normalize_plugin_mcp_server` at `rust-v0.163.0`), so `tool_timeout_sec` (seconds) works in a plugin `.mcp.json`. In a throwaway `CODEX_HOME`, `codex plugin marketplace add <prototype>`, `codex plugin add powerops@powerops` and `codex mcp get powerops --json` showed the server enabled with the prototype's `tool_timeout_sec: 1800.0`. A user's own `[mcp_servers.powerops]` takes the place of the plugin's entry (`-c mcp_servers.powerops.tool_timeout_sec=120` with the same command gave 120). The per-plugin settings in `config.toml` (`[plugins."powerops@powerops".mcp_servers.powerops]`) cover `enabled`, approval modes and tool lists, not timeouts (`apply_plugin_mcp_server_policy`, `core-plugins/src/loader.rs`).
- Codex reads `.mcp.json` at a plugin root even without `mcpServers` in the manifest (`DEFAULT_MCP_CONFIG_FILE`, `core-plugins/src/loader.rs`), and the manifest's `./.mcp.json` path is read when present (`plugin_mcp_config_paths`). The row's field stays, so the manifest says where the servers are.
- Why 3600 seconds: `deploy apply` waits up to 60 minutes for each import (`SettingsCommand.ImportTimeout`), and the imports run inside the `deploy_apply` call. Codex's default is 300 seconds in its code at `rust-v0.163.0` (`DEFAULT_TOOL_TIMEOUT`, `codex-mcp/src/rmcp_client.rs`; its docs say 60), so a long import could outlast the call, which then ends with an error while the import goes on. Codex stops a call's clock while a form from that server is open (`pause_state.enter()` in `create_elicitation`, `rmcp-client/src/elicitation_client_service.rs`, read by `active_time_timeout` in `rmcp-client/src/rmcp_client.rs`), so the time the user spends on the form doesn't count. Claude Code's default limit is about 28 hours, with a 30-minute idle window for stdio calls (`CLAUDE_CODE_MCP_TOOL_IDLE_TIMEOUT`); the Claude plugin sets nothing, and the README names the variable.

**Codex's form** (settled before this plan; `decisions.md` Q4; read in its code at `rust-v0.163.0`):

- For a schema with one string property, as `deploy_apply` sends, the TUI shows a text field (`tui/src/bottom_pane/mcp_server_elicitation.rs`) whose footer offers submit and "esc to cancel". Enter sends Accept with the typed text, and the TUI refuses a blank required field. Esc, or Ctrl-C on an empty field, sends Cancel. It has no Decline: Decline exists only for a form without properties or one the TUI can't parse. So in Codex the user's no reaches the server as `cancel`, and the tool's result says "the confirmation form was cancelled". Codex's approval prompt before the call (for a tool whose `destructiveHint` is true) is a separate screen.
- The answer goes from the TUI through the app-server and core to the MCP client, which returns it as the result of the server's `elicitation/create` (`ElicitationRequestRouter::resolve`, `codex-mcp/src/elicitation.rs`). The keys are echoed from the request, so there is no name or id mismatch. An answer is dropped, with only a log warning, when Codex has already given up the request: the server cancelled its `elicitation/create` or the connection closed (the `select!` in `create_elicitation`); the form then stays on screen. An interrupted turn resolves an open form as Cancel (`app-server/src/bespoke_event_handling.rs`). Codex sets no time limit on the user's answer.
- Why M19's answer never reached the toy server is not settled. The code has no path that loses an answer to a request the server still waits on; the paths it has are a request the server had already given up, or an answer that was a Cancel (Esc), which the toy's log may not have shown. PowerOps' server waits on its request until the client answers or cancels the call (M19), and each answer gives its own result line ("declined", "cancelled", "the name typed in the form is not the plan's target", "the confirmation form failed"), so M20's run shows which answer arrived.

**Descriptions.** The `description` of both manifests and the Codex `interface.longDescription` get one sentence before the PATH sentence: the plugin also starts the PowerOps MCP server (`powerops mcp`), whose tools check, diff, test and plan a deploy, and import only after the user types the target's name. `interface.shortDescription` and the marketplace files stay: the marketplace `metadata.description` still says "planning skills", which stays true, and the plugin's own description names the server.

## Automation

**Files.**

- `plugins/claude/.mcp.json`, `plugins/codex/.mcp.json`: new (Data).
- `plugins/codex/.codex-plugin/plugin.json`: `"mcpServers": "./.mcp.json"` after `"skills"`; the description and `interface.longDescription`.
- `plugins/claude/.claude-plugin/plugin.json`: the description. No `mcpServers` key: the servers are in `.mcp.json` only, so they are defined once.
- `tests/PowerOps.Cli.Tests/PluginManifestTests.cs`: `AssertManifest` drops its three no-server checks and checks that the description contains `powerops mcp`. A new helper reads a plugin's `.mcp.json` and checks that `mcpServers` has exactly one entry, `powerops`, whose `command` is `powerops` and whose `args` is exactly `["mcp"]`, with no other keys except, for Codex, `env_vars` equal to those two names and `tool_timeout_sec` equal to 3600. The Codex test also checks that `interface.longDescription` equals `description`. The Claude test checks that `plugin.json` has no `mcpServers`. The Codex test checks that `mcpServers` is `./.mcp.json` and resolves that value against the plugin folder to the file the helper reads, which must exist.
- `README.md`: the section "Use from an agent (MCP)" after "Planning" (below). The `mcp` paragraph under Usage keeps the command's own lines (stdio, exit codes) and links to the section; the tool list, what writes, the absolute paths and the refusal of `-` and `@` values move into the section, so they are written once. One line in Planning, after the plugin install lines, says the plugin also starts the server.
- `docs/BUILD-PLAN.md`, `docs/FOUNDATION.md` and this plan's status, for the record (Steps 5 and 7).

**README section "Use from an agent (MCP)".** In this order, each point a sentence or two:

1. What the tools return (the command's JSON, or the file's text for `baseline` and `settings_generate`), then the tools (the list from the Usage paragraph): which only read, which read a tenant through `pac` and `az`, and the two that write: `deploy_plan` writes its plan file when `save` names a new file, and `deploy_apply` with `apply: true` imports a reviewed plan only after the user types the target's name in the server's form. Without `apply` it is a dry run. Then the absolute paths and the refused `-` and `@` values.
2. The plugin (the install lines in Planning) starts the server in each session where it is enabled. It runs `powerops mcp` from PATH, so the server is the installed CLI's version, and `powerops` must be on the PATH of the agent's process; `/mcp` shows a server that failed to start. Claude Code names it `plugin:powerops:powerops`.
3. Without the plugin: `claude mcp add powerops -- powerops mcp` (local scope by default; `--scope project` writes a `.mcp.json` that the team shares; `--scope user` for every project), or `codex mcp add powerops -- powerops mcp`. With both, Claude Code connects the same command once and prefers the user's own entry (its docs: duplicates of a plugin server are matched by endpoint, and local, project and user scope rank above plugins), so the tools are `mcp__powerops__<tool>`; in Codex the user's own `[mcp_servers.powerops]` replaces the plugin's, and on Linux needs the same `env_vars` (Data).
4. What asks first: Claude Code asks before each tool call unless a permission rule allows it (`mcp__plugin_powerops_powerops__<tool>`, or `mcp__powerops__<tool>` for your own entry); Codex in its default mode asks before `deploy_plan` and `deploy_apply` and runs the read-only tools without asking, `plan_facts` and `drift` included, which read a tenant. Allow `deploy_apply` per call, not in a rule: the form is the gate, and an agent asks in chat before it sets `apply` (AGENTS.md).
5. Whoever answers the form gives the yes, so don't set up anything that answers forms, such as a Claude Code `Elicitation` hook or a Codex reviewer; an answer without the exact name is refused. In Codex 0.163.0 the reviewer passes this form to the user (it reviews only forms whose `_meta` marks an approval request, `core/src/session/mcp.rs`), the full-access auto-accept takes only forms without properties (`can_auto_accept_elicitation`, `codex-mcp/src/elicitation.rs`), and the `never` policy declines it. Its form has no Decline; Esc cancels. `claude -p` and `codex exec` cancel the form, so `apply` imports only in an interactive session; elsewhere use `powerops deploy apply --apply`.
6. Timeouts: `deploy apply` waits up to 60 minutes for each import. The Codex plugin sets `tool_timeout_sec = 3600`; with your own Codex entry, set it in `config.toml`, because Codex's default (60 seconds in its docs, 300 in its code) can end a call while an import runs. Codex 0.163.0 stops that clock while its form is open (read in its code, not observed). Claude Code ends a stdio call after 30 minutes without a response (`CLAUDE_CODE_MCP_TOOL_IDLE_TIMEOUT`), and moves a call still running after two minutes to a background task (`/tasks`) whose result arrives when it ends; a call waiting on the form is not moved. A call that the client gives up on does not stop an import that has started; it ends on its own, another `apply` call to that server is refused until then, and a client that kills the server leaves `pac` running with no report (M19's Assumptions). Then `deploy_apply` without `apply` shows where the target stands.

**Tests.** `PluginManifestTests` as above. The skill copies don't change, so the golden recipe should leave no diff; it runs anyway (Step 4). No new code under `src/`, so no other test changes.

## Screens

None. The clients' own approval prompts and form dialog are the only UI.

## Environments and route

**Setup, by this session, before the user is asked** (all outside the repo, deleted after the run):

1. The CLI: `dotnet pack src/PowerOps.Cli -c Release -o <scratch>/pack`, then `dotnet tool install PowerOps --tool-path <scratch>/tools --add-source <scratch>/pack --version 0.0.0`. The sessions start with `<scratch>/tools` first on PATH; nothing is installed globally.
2. Claude Code, installed as M13's was: with `CLAUDE_CONFIG_DIR` set to a new empty folder, `claude plugin marketplace add "<worktree>"`, `claude plugin install powerops@powerops`, `claude plugin details powerops` (observed on 2026-10-28: "MCP servers (1) powerops" beside the three skills), `claude mcp list` with the tool path on PATH ("plugin:powerops:powerops: powerops mcp - Connected"; without it, "Failed to connect") and `claude plugin validate --strict` on the repo root and `plugins/claude`. The interactive session can't use that folder without a new sign-in, so it loads the same plugin folder for one session with `--plugin-dir "<worktree>/plugins/claude"` on the user's own login (M13's session check did the same). It starts in the worktree, which Claude Code already trusts, so M19's stop at the folder-trust prompt doesn't recur. Nothing is installed in the user's Claude Code config; the session keeps its usual history.
3. Codex has no session-only plugin flag, and its login lives in `CODEX_HOME`. So the run uses a throwaway `CODEX_HOME` outside the system temp folder (Codex warns about PATH aliases there, M13 review R2): `codex plugin marketplace add "<worktree>"`, `codex plugin add powerops@powerops`, and `codex mcp get powerops --json` shows the server with `tool_timeout_sec: 3600`. The user signs in there with `codex login` (they type it), and trusts the worktree when the TUI asks. Afterwards `codex logout` and the folder is deleted; the user's own `~/.codex` is not touched.
4. The deploy folder for the tenant steps: a scratch folder with `PowerOpsSeed_managed.zip` 1.0.0.2 and its settings file, the files M9 and M10 used (kept outside the repo since then). Test held PowerOpsSeed 1.0.0.1 managed after M10, so the plan is an upgrade and `deploy_apply` gets as far as the form. `pac` and `az` are signed in as the user, as for M9's and M10's local runs; if either needs a sign-in, the user does it. Nothing here contacts the tenant before the user's yes.
5. A prompt sheet in the scratch folder with the prompts below, the paths filled in, so the user copies each line. The Test URL goes in it only after the user's yes, and the sheet is deleted with the folder. Beside it, two bash scripts start the clients (`run-claude.sh`, `run-codex.sh`): each puts the tool path first on PATH, prints `command -v powerops`, which must be the scratch tool, and starts the client in the worktree. The user's Terminal runs fish, where an unquoted `x:$PATH` splits into one word per entry, so the run starts the scripts with `bash`, never a typed `env PATH=...` line.
6. Nothing answers a form or allows a tool in advance: this session checked on 2026-10-28 that the user's Claude Code settings (user, user-local and the worktree's local settings) hold no `hooks` and no `mcp__` rule, that no installed plugin declares an `Elicitation` hook, and that the user's Claude Code config has no own `powerops` server entry at any scope (so `--plugin-dir` is not hidden by a duplicate). The settings files' hashes are taken before the run and compared after it.

**The run, by the user, in the Terminal panel** (this session asks once, with these steps, and waits). For each client, Claude Code first:

1. Start: `bash <scratch>/run-claude.sh`; for Codex `bash <scratch>/run-codex.sh`, which runs `codex login` first when the throwaway home has no login. On Codex's first start in that home, the user trusts the worktree, which gives the default mode (approvals on request); never full access or a `never` policy, which would decline the form unseen.
2. `/mcp`: in Claude Code the server is `plugin:powerops:powerops`, connected, and its tools are `mcp__plugin_powerops_powerops__<tool>`; in Codex it is `powerops`, with 12 tools. Then the agent lists the tools (prompt 1).
3. Offline prompts, each naming the tool and absolute paths: `analyze` on `tests/fixtures`; `diff` of `tests/fixtures/powerops-seed/v1` and `v2`; `test` on `tests/fixtures/alm-makers` with `tests/fixtures/flowtests/alm-makers`; `drift` of `PowerOpsSeed` on `tests/fixtures/drift/seed-dev.json` and `seed-test.json`. The row says `tests/fixtures/pac/drift/`, but that folder holds Web API captures; the snapshot files `drift` reads are in `tests/fixtures/drift/`, and Step 7 amends the row. Each result is compared with the packed CLI's output on the same absolute inputs, run by this session before the user is asked: `analyze` reads 11 solutions with 0 errors and 36 warnings (exit 0); `diff` finds 1 added and 1 changed component (exit 1); `test` passes 4 of 4 (exit 0); `drift` reports drift in 3 items (exit 1). The prompt sheet lists these numbers.
4. Tenant prompts, only after the user's yes to Test in this session (each prompt is also the user's yes to Test in that client's session): `deploy_plan` with `folder` the deploy folder and `target` Test, without `save`, after which this session checks that the folder's listing is unchanged; `deploy_plan` again with `save` `m20-<client>.json`; `deploy_apply` on that file, without `apply`; then `deploy_apply` with `apply: true`. The client asks before each call that isn't read-only (Codex for `deploy_plan` and `deploy_apply`, Claude Code for every tool): the user allows it once, never with "always allow" or "don't ask again", which would write a rule. The form shows the target, hash and zip names. In Claude Code the user chooses Decline without typing. Codex's text form has no Decline (Data, Codex's form), so there the user presses Esc once, which sends Cancel.
5. Evidence: the `apply` call's result says "the confirmation form was declined; nothing was imported" in Claude Code and "the confirmation form was cancelled; nothing was imported" in Codex. Each text exists only in the server's branch for that answer, so it shows the answer reached the server. In Codex "cancelled" also comes from an interrupted turn, so the user notes that the form was on screen, that they pressed Esc once, and that no turn was interrupted; a "declined" in Codex can only come from a policy, so it is recorded as "form not shown". A failed form gives "the confirmation form failed". If no result comes back within a few minutes of the answer, the user looks in Claude Code's `/tasks` first; a call still open there or in Codex is recorded as hanging, and the user stops the session. Codex's log stays in the throwaway home until the record is written. Afterwards this session runs `powerops deploy apply <plan file> --target <Test>` without `--apply` (a dry run) and checks that it still plans the upgrade from 1.0.0.1, so nothing was imported.

If the user says no to Test, or Test is gone, steps 4 and 5 are marked not verified (decision 28) and the offline steps still run. If a client doesn't show the form, or the decline doesn't reach the server, the result line it gives is recorded as observed, and nothing was imported either way (M19: every other path refuses).

**Clean-up.** The settings files' hashes are compared with those taken before the run, and `git status` shows no new file. The tool path, the pack, the throwaway `CLAUDE_CONFIG_DIR` and `CODEX_HOME` (after `codex logout`, with Codex's log), the deploy folder with its plan files, the scripts, the prompt sheet and the Codex source checkout are deleted. The Terminal tabs are closed. The Claude Code session's transcript, in the user's own history, holds the Test URL and the plan's results: this session tells the user, and the user decides whether to delete it.

## Solution and publisher

None.

## Constraints

- No new package and no code under `src/`. Dependencies stay as they are (Apache-2.0 and MIT).
- AGENTS.md: no `--apply`, and `deploy_apply` with `apply: true` only after the user's yes, with the user declining the form. This session never answers the form. Tenant work only after the user's yes in this session.
- No version or changelog edits (the plugin manifests' `version` included); no push or pull request from this session. Conventional Commits, no attribution lines. No environment URL, id, email or user name in the repo, the plan or the record.
- Deadline: Test expires about 2026-11-21.

## Assumptions

- Claude Code's interactive session and `claude -p` start plugin servers the same way; the PATH check ran with `-p`. The interactive run confirms it.
- The marketplace install and `--plugin-dir` load the same plugin folder in place (a local-path marketplace loads plugins in place, M13 sources), so the interactive run with `--plugin-dir` checks what an install registers.
- Test still holds PowerOpsSeed 1.0.0.1 managed, as M10 left it. If it holds 1.0.0.2, the plan has nothing to import and `deploy_apply` with `apply` stops before the form ("the dry run did not pass"); the record then says so, and the decline is not verified.
- The M9 settings file still matches Test's connection references. If it doesn't, the plan or the dry run reports it, and the record says which.

## Open questions

## Questions for your admin

## Steps

1. Research and prototypes, outside the repo: Codex's code at `rust-v0.163.0` (the plugin server config, the timeout, the form's path back to the server), the Claude Code and Codex prototypes (Data). Done on 2026-10-28 (`decisions.md`, Log).
2. `docs:` this plan, its decisions and review, signed off.
3. `feat(plugins):` item 45: both `.mcp.json` files, the Codex manifest's `mcpServers`, the descriptions, `PluginManifestTests`, and the README section.
4. Run `dotnet build --no-incremental` (0 warnings), `dotnet test`, `dotnet format --verify-no-changes`, `dotnet pack src/PowerOps.Cli -c Release -o artifacts`, `powerops plan status .` and `claude plugin validate --strict` on `.` and `plugins/claude`; run the golden recipe and read the diff.
5. The manual run's setup (Environments and route), then ask the user once, with the steps; the run; the clean-up.
6. If the run finds a fault in the plugin files or the README, a `fix(plugins):` commit with its test. The run found one: the Codex entry's `env_vars` (Data), with its test and a README paragraph.
7. `docs:` BUILD-PLAN: the M20 row marked built with a link to this plan, and amended (the snapshot folder, the Codex timeout in the plugin, Codex's default of 300 seconds in its code); item 45 (`feat(plugins):`); the M20 line of the verification record (each client's steps, what is not verified); in the M19 line and the research basis, the claim that Codex's reviewer and full-access auto-accept answer the form with an empty `content` corrected (for a form with properties, the reviewer passes it to the user and the auto-accept doesn't apply), and Codex's defaults in its code at `rust-v0.163.0` added (startup 30 seconds, tool 300 seconds; its docs say 10 and 60). FOUNDATION §8's two Phase 5 items closed or marked as far as the run settled them, and its Phase 5 status. This plan's status.
