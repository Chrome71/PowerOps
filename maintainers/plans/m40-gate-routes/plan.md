---
status: done
review: done
signed-off: 2026-11-02
signed-off-hash: e9942659a9492232f6f631e8ba5aa1f880f4de1f6fcf1983c12c32cddc979f54
environment: null
depth: quick
supersedes: null
---

# M40: Gate routes, hook matchers and the PO005 fix

## Goal

Close the tenant writes that pass `powerops deploy gate` today, make the documented hooks send the matching MCP calls to the gate, and stop two false PO005 warnings on agent solutions. This is M40 of Phase 7 ([plan](../phase7-power-platform/plan.md), build-order item 75, release label v0.23.1). Decision 41 keeps the gate a deny-list.

Done when:

- `deploy gate` blocks, each with its own route line, `pa app push` (run as `pa`, from `node_modules/.bin/pa`, or through `npx` with or without its options, and the retired `power-apps push`), `pac pages upload`, `pac pages upload-code-site`, `pac pages migrate-datamodel` (also under the aliases `powerpages` and `paportal`), `pac model genpage upload`, `pac copilot push` and `pac copilot publish`; a form it can't read (`sh -c`, a variable, a mention) blocks as unreadable, as the existing pac routes do.
- `deploy gate` blocks pac's MCP tools `pages_upload`, `pages_upload-code-site`, `copilot_publish` and `model_genpage_upload` under any server name, in Claude Code's and Codex's spelling of the tool name, without reading the plan or the target.
- The hook examples in `docs/HANDOFF.md` name the five pac MCP tools in one matcher per client, `HandoffDocTests` pins both, and one line tells existing hook users to replace the matcher.
- PO005 counts a connection reference as used when `Assets/botcomponent_connectionreferenceset.xml` maps it to an agent component.
- The docs lines below are in, and the plugin copies of `powerops-implement` are regenerated.
- The toy-server check of M21 Step 7 ran again by hand with the new matchers in Claude Code and Codex, no blocked call reached `tools/call`, and the result is in BUILD-PLAN's verification record.
- `dotnet build`, `dotnet test` (with `RoleEvalTests` unchanged and passing), `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan (`bash reports/leakscan.sh origin/main <this branch>`, from the main checkout's untracked `reports/` folder, which the worktree doesn't have) are clean, and one `fix:` PR is open.

## Users

- A maker whose agent has the hooks on for steps 7 to 9 of `docs/HANDOFF.md`, and who must not see a site, page, app or agent written to Test or Prod outside a reviewed plan.
- Someone who already copied the hooks from an older `docs/HANDOFF.md`.
- A CI user or agent who runs `analyze` on a solution that holds Copilot Studio agents.

## Data

Line numbers are those of `80dd43a`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | Seven route lines, added to `Routes` so a block asks for the plan, not a rewrite: `pa app push publishes an app that no plan holds`; `pac pages upload uploads a site that no plan holds`; `pac pages upload-code-site uploads a site that no plan holds`; `pac pages migrate-datamodel migrates a site's data model, which no plan holds`; `pac model genpage upload publishes a page that no plan holds`; `pac copilot push changes a live agent that no plan holds`; `pac copilot publish publishes an agent that no plan holds` | `src/PowerOps.Core/Deploy/ImportCall.cs:19-36` | below |
| 2 | In the `pac` switch: `pages upload`, `pages upload-code-site` and `pages migrate-datamodel`, each also under `powerpages` and `paportal` (Learn: both aliases "will continue to work"); `copilot push`; `copilot publish`; and `model genpage` when the fourth word is `upload` (a fourth word that isn't literal blocks as unreadable, as the first three do) | `ImportCall.cs:241-284` | `Blocks()` rows for each route and alias with the route line, and `The_new_always_blocked_routes_ask_for_the_plan_not_a_rewrite` gains each |
| 3 | A `pa` branch beside the `node` branch: the program's file name (so `node_modules/.bin/pa` too) is `pa` followed by `app push`, or `power-apps` followed by `push`; when the program is `npx`, the words that start with `-` after it are skipped first (`npx --no-install pa`, `npx -y pa`). The segment is recognised and gets `RoutePaPush`. npm's Windows shims count as the same programs (`pa.cmd`, `power-apps.cmd`, `npx.cmd`) | `ImportCall.cs:287` | a row per spelling: `pa app push`, `pa app push --solution-id <guid>`, `node_modules/.bin/pa app push`, `./node_modules/.bin/pa app push`, `npx pa app push`, `npx --no-install pa app push --solution-id <guid>`, `power-apps push`, `npx power-apps push` |
| 4 | `Markers()` gains bare words, as the existing routes' markers are: `app push` and `apps push`, `pages`, `powerpages` or `paportal` before `upload` or `migrate-datamodel`, `genpage upload`, `copilot push` and `copilot publish`, inside the existing `(?<!\w)` group and with no word character after them (so `pushed`, `uploads` and `publisher` don't match). So a form the gate can't read (`sh -c 'pa app push'`, `npx -p <package> pa app push`, `pac --verbose pages upload`, and `$PA app push`, which Microsoft's code-apps skills write after `shared/cli-binary.md` at power-platform-skills `3fb3604` sets `PA`), a mention (`git commit -m "... pa app push"`, `grep "pages upload" docs`) and the same words in the `PowerShell` tool block, as for the existing routes. The bare words also block everyday commands while the hooks are on, as `code push` does today (decisions Q4) | `ImportCall.cs:62` | `Blocks()` rows with `CantRead` for each form above, route rows for `pa.cmd app push`, `node_modules/.bin/power-apps.cmd push` and `npx.cmd --no-install pa app push`, and pinned as intended: `git commit -m "feat: add app push notifications"`, `git -C apps push origin main`, `mv pages upload`; `A_pac_or_import_command_in_the_powershell_tool_is_blocked` gains `pa app push`; `Monitor_runs_a_shell_command_like_Bash` gains `npx pa app push` |
| 5 | The forms that still pass: `pa app run`, `pa app list`, `pa auth login`, `npx pa app init`, `npm run build`, `pac pages download`, `pac pages list`, `pac pages download-code-site`, `pac model genpage list`, `pac model genpage download`, `pac copilot pull`, `pac copilot list`, `pac copilot pack`, `npm install @microsoft/power-apps`, `npm install --save-dev @microsoft/power-apps-cli`, `echo the app pushed`, `cp -r public/pages uploads/`, `git commit -m "copilot publisher"`, `ls power-apps/src`, `git add power-apps/`; and two known misses, pinned so a change to them is deliberate: `$PA push` and `PA="npx --no-install power-apps"; $PA push`, where a variable holds the retired flat CLI and `push` alone can't be a marker because of `git push` (decisions Q6) | none | `A_shell_command_without_an_import_is_not_one` gains them |
| 6 | MCP: in `ReadInput`, before the `solution_import` check, a tool name that starts with `mcp__` and ends with `__pages_upload`, `__pages_upload-code-site`, `__pages_upload_code_site` (Codex turns `-` in a tool name into `_`), `__copilot_publish` or `__model_genpage_upload` is blocked with the matching route line from row 1, and its `tool_input` isn't read | `ImportCall.cs:133-138` | a theory over the four tools in Claude Code's input (`Claude(...)`, name `mcp__pac-cli__<tool>`), Codex's (`Codex(...)`, name `mcp__pac_cli__<tool with _>`) and a plugin server's name, each blocked with its line and `Unreadable` false; `mcp__pac-cli__pages_download`, `mcp__pac-cli__pages_list`, `mcp__pac-cli__model_genpage_list` and `mcp__pac-cli__copilot_publish_x` are not imports. In `DeployGateCommandTests`, the four tools through the command exit 2 with the route line and the import guidance, with no plan file and no target read |
| 7 | `docs/HANDOFF.md`, Claude Code: the first group's matcher becomes `mcp__pac-cli__solution_import\|mcp__pac-cli__pages_upload\|mcp__pac-cli__pages_upload-code-site\|mcp__pac-cli__copilot_publish\|mcp__pac-cli__model_genpage_upload`. Its docs: a matcher of only letters, digits, `_`, `-` and `\|` is a list of exact names. Codex: the prose names `mcp__pac_cli__solution_import\|mcp__pac_cli__pages_upload\|mcp__pac_cli__pages_upload_code_site\|mcp__pac_cli__copilot_publish\|mcp__pac_cli__model_genpage_upload`; at `rust-v0.166.0` `codex-rs/hooks/src/engine/matcher.rs` treats a matcher of letters, digits, `_` and `\|` as exact names split at `\|` (the same at `rust-v0.163.0`), and `codex-rs/codex-mcp/src/mcp/mod.rs` turns every other character of the tool name into `_`. The bullet on the first matcher says that each name carries the server's name. A new line for existing hook users: replace the old one-tool matcher, or the four tools run unchecked, and in Codex trust the changed hook again; the Codex paragraph says why: its trust hash covers the matcher (`hook_hash` in `codex-rs/hooks/src/engine/discovery.rs` at `rust-v0.166.0`), so an edited hook doesn't run until it is trusted again, and `solution_import` runs unchecked meanwhile | `docs/HANDOFF.md:48,73,76` | `HandoffDocTests.The_hooks_run_the_gate_and_block_when_it_fails` expects the new first matcher; a new test checks that the page holds the Codex matcher, which is the Claude Code one with every `-` turned into `_`, that it holds the line for existing hook users, and that `ImportCall.Read` blocks each name of both matchers |
| 8 | `docs/HANDOFF.md` "What the gate blocks always" gains the routes of rows 1 to 6, and a sentence: no plan check covers them, so with the hooks on the user runs such a command, or turns the hooks off for that step after saying yes to it. "What it doesn't do": the routes bullet says the other tenant writes have no route (deletes and settings changes, `pac model genpage add` and `remove`, `pac copilot create` and `init --environment`, pac's MCP admin, environment-settings and managed-identity tools) and that `powerops-implement` asks before each; a new bullet says there is no route for Fabric writes (publish, refresh, delete) and the gate doesn't see writes that run no shell command and no pac MCP tool (Apply in the Copilot Studio extension for VS Code, a skill's push through the Copilot Studio language server, the Copilot Studio MCP's `manage_agents`); the variables example gains `$PA push`; the deny paragraph says the four write tools can be denied the same way. The Sources line adds the pac pages, model, copilot and Power Apps CLI references and Codex's files read at `rust-v0.166.0`, and its `verified:` date becomes 2026-11-02 | `docs/HANDOFF.md:78-87` | `HandoffDocTests` (commands named exist) |
| 9 | `docs/COMMANDS.md` `deploy gate`: the sentence on what is not an import names the four MCP tools, with the sentence on what to do instead from row 8, and the list of routes that can't be checked gains the routes of rows 1 to 6 | `docs/COMMANDS.md:97,105` | none, docs only |
| 10 | `skills/powerops-implement/SKILL.md` step 7: the tenant writes that need the go-ahead each time also cover the baseline `pa app push` of `create-code-app`, which that skill marks as pre-approved (its Step 6, and line 123 of its `SKILL.md` at power-platform-skills `3fb3604`): ask anyway, push only to the plan's Dev environment, and pass `--solution-id` with the plan's solution, or the app lands in a solution the CLI picks; and Fabric writes (publish, refresh, delete). Plugin copies regenerated by "Update goldens". The skill isn't pinned | `skills/powerops-implement/SKILL.md:51` | `SkillsCommandTests`, `PluginManifestTests` |
| 11 | PO005: `Solution` gains `AgentConnectionReferences` (optional, default empty, as `CanvasFormulas`), the logical names in the `connectionreferenceid.connectionreferencelogicalname` attribute of each `botcomponent_connectionreference` element of `Assets/botcomponent_connectionreferenceset.xml`, read through `LoadXml` (so the depth, size and DTD limits hold) when the file is there. A malformed file makes the solution unreadable (`InvalidDataException`, exit 3), as a malformed workflow file does (decisions Q5). Both the unpacked and the zip layout keep the file at that path (observed in Microsoft's public order-management sample, in the vscode-copilotstudio test data and in pnp samples). PO005 adds them to `used`. Title, message and fix line stay, so no golden changes | `src/PowerOps.Core/Model/Solution.cs:17`, `src/PowerOps.Core/Readers/SolutionParser.cs:89-107`, `src/PowerOps.Core/Rules/PO005UnusedConnectionReference.cs:17` | `WarningRuleTests`: a `B.Empty()` solution with the reference in `AgentConnectionReferences` is quiet, in another case is quiet, and a different name fires. A loader test writes a solution folder with `TempFolder` (manifest, a customizations file with two references, and the mapping file naming one of them), zips it with `SolutionZip.Create` and runs `Analyzer` on both: the mapped reference gets no PO005 and the other does. Without the file, both fire, and a malformed file is an `InvalidDataException` naming the folder and the file |
| 12 | `docs/rules/PO005.md`: "What it finds" says that a mapping to an agent component in `Assets/botcomponent_connectionreferenceset.xml` counts as a use; that Learn documents only the `botcomponent_connectionreference` table, while the file's place (the same in a folder and a zip) is observed in Microsoft's public samples and reading a mapping as a use is the rule's choice; that a malformed file makes the solution unreadable; and that the YAML source layout is read without the mapping. The Sources line adds Learn's table reference, with a new date. The rules README row reads "flow, canvas app or agent" | `docs/rules/PO005.md`, `docs/rules/README.md:11` | `RuleDocsTests` |
| 13 | `docs/BUILD-PLAN.md`: M40 marked built in the Phase 7 table and in item 75, with the plan link, and an M40 line in the verification record, after its last entry (M28, line 143) | `docs/BUILD-PLAN.md:143,518,535` | none, docs only |

## Automation

The toy-server check, by hand, outside the repo in the session's scratch folder, as M21 Step 7 ran it: a scratch git repo with no plan file; the packed `powerops` on PATH from a temporary tool path; a toy stdio MCP server named `pac-cli` whose five tools (`solution_import`, `pages_upload`, `pages_upload-code-site`, `copilot_publish`, `model_genpage_upload`) only log to a file; the HANDOFF hooks set per run only. `claude -p` with `--mcp-config`, `--strict-mcp-config`, `--settings` and `--no-session-persistence`, and `codex exec` with the server and the hooks set by `-c`, `--ephemeral`, a read-only sandbox and `--dangerously-bypass-hook-trust` for that per-run hook, asked to call each of the five tools. Each call must be blocked with the gate's line and no `tools/call` may reach the toy log. The user's client config is not changed. Record the client versions and results; anything not run is marked not verified. The bypass flag means the check can't show Codex's re-trust step after a matcher edit; that rests on the code reading in row 7.

## Screens

None.

## Environments and route

None. No step reads or writes a tenant, runs `pac` against an environment, or runs any `--apply`. The toy server has no `pac`.

## Solution and publisher

None.

## Constraints

- AGENTS.md: no dependency, every write through `SafeWrite` (this change adds none), fake values only, no version or changelog edits, Conventional Commits, `dotnet format` clean.
- The gate keeps exiting only 0 or 2 (decision 39) and stays a deny-list (decision 41).
- No change to the text of `powerops-verify` or `powerops-review-security`, and no eval baseline change: no eval case holds an agent or the mapping file, and no title or message changes.
- Hooks are off while building in Dev, so the new routes don't slow the Dev loop.

## Assumptions

- `pac pages migrate-datamodel --checkMigrationStatus` and `--siteCustomizationReportPath` only read, but the route blocks every form of the command, because the gate reads no options here and the hooks are on only for steps 7 to 9.
- Codex's hook `tool_name` for an MCP tool is the model-visible name, so `-` becomes `_`: read in `codex-rs/core/src/tools/handlers/mcp.rs` (`hook_tool_name`) and `codex-rs/codex-mcp/src/tools.rs` at `rust-v0.166.0`. The toy-server check confirms it.
- A spelling outside rows 2 and 3 that holds the route's words (`pa.exe app push`, `npx -p <package> pa app push`, `$PA app push`) blocks through the markers as unreadable, not with the route line. `$PA push`, with `PA` holding the retired flat `power-apps`, and `node <path>/power-apps.js push` pass: they hold no route word (decisions Q6). Microsoft's `create-code-app` writes that CLI's push literally (`power-apps push`), which is a route.
- The docs' route lists (rows 8 and 9) are checked by eye, as the existing ones are; no test ties them to `Routes`.
- The YAML source layout reader is not changed: no YAML layout sample with an agent was read.
- Not covered by M40, as the Phase 7 plan says: an allow-list, and routes for deletes, settings changes, `pa app share`, `pa connection create` and `pac copilot init --environment`; nor `pac model genpage add` and `remove`, `pac copilot create`, or pac's MCP admin, environment-settings and managed-identity tools, which HANDOFF names (row 8).
- Environment facts were not read: this plan changes this repo and targets no environment.

## Open questions

## Questions for your admin

## Steps

1. This plan, a review in a fresh context, fixes, sign-off.
2. Rows 1 to 6, test first: the `ImportCallTests` rows and the gate command test fail, then pass.
3. Row 11, test first, then row 12.
4. Rows 7 to 10, with the `HandoffDocTests` changes; then "Update goldens" for the plugin copies, and read the diff.
5. `dotnet pack`, then the toy-server check (Automation) in both clients.
6. Row 13, with the check's result.
7. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan (`bash reports/leakscan.sh origin/main <this branch>` from the main checkout); commit, push and open one `fix:` PR. The master session merges.
