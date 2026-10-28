# Decisions: m19-deploy-apply

## Round 1

### Q1. Does `apply` stay, given what each client does with the form?
1. Keep `apply`: both clients send a form to the user in an interactive session, and the headless paths cancel or decline it (recommended)
2. Leave `apply` out and keep `deploy_apply` a dry run (decision 26's fallback)
3. In your own words
Answer: Keep `apply`: both clients send a form to the user in an interactive session, and the headless paths cancel or decline it. Claude Code 2.1.303 (revision 2026-07-28, `elicitation: {form, url}`) documents a dialog in interactive sessions, and in `claude -p` it answered the toy form with `cancel` while the model saw only the tool's text. Codex 0.163.0 (revision 2025-06-18, `elicitation: {form, url}`) sends the form to its TUI, which shows a string property as a text field. It declines under the `never` policy and auto-cancels in `codex exec`, and its reviewer and full-access auto-accept send `content: {}`, which carries no name. Decision 26's fallback applies only if neither client shows the form to the user.

### Q2. What does the form ask, and what passes?
1. One required string field `environment` with no `default`, a message that names the plan file, target, hash and zip names (never the `target` input or the folder), and a pass only for `accept` with exactly the plan's target (recommended)
2. A boolean "I confirm" field
3. In your own words
Answer: One required string field `environment` with no `default`, a message that names the plan file, target, hash and zip names (never the `target` input or the folder), and a pass only for `accept` with exactly the plan's target. A boolean or an empty form can be accepted without typing: Codex auto-accepts an empty form with `{}` under full access, and its reviewer accepts with `{}`. A `default` would let `{}` pass, because the SDK's `ElicitResult.WithDefaults` fills a missing accepted field from it. The zip names come from `inputs.zips`, which the hash covers; the file's `solutions` array is not covered, so it is not shown.

### Q3. How does the server know the client can elicit?
1. `context.Server.ClientCapabilities?.Elicitation?.Form` on the request's own server, checked before the plan file is read; null means refuse (recommended)
2. Call `ElicitAsync` and treat its `InvalidOperationException` as "no elicitation"
3. In your own words
Answer: `context.Server.ClientCapabilities?.Elicitation?.Form` on the request's own server, checked before the plan file is read; null means refuse. At 2026-07-28 the capabilities come with each request, so only the request's server has them (the SDK's XML docs for `McpServer.ClientCapabilities`). The explicit check gives its own message, and the catch-all around `ElicitAsync` (a failed form) stays for errors on the way.

## Round 2

### Q4. Where does the one-import guard sit?
1. A `SemaphoreSlim(1, 1)` per server around the `--apply` run only, with an early check before the form (recommended)
2. Take the guard before the form and hold it to the end of the import
3. In your own words
Answer: A `SemaphoreSlim(1, 1)` per server around the `--apply` run only, with an early check before the form. At 2026-07-28 the SDK suspends a handler that waits on a form until the client retries, with no timeout (`McpServerImpl.WrapHandlerWithMrtr` at v2.3.0). A guard held across the form would stay taken for the session if the client never retries. Two forms may be open at once; the second accepted one is refused while the first imports, and a later retry plans again and meets the hash check.

### Q5. How are the interactive checks run?
1. Ask the user in this session, once the build is done, to answer the toy form in an interactive Claude Code session and an interactive Codex session started with per-run config; without a yes, both are recorded as not verified and M20's manual run covers them (recommended)
2. Skip them and rely on the docs, the code and the headless runs
3. In your own words
Answer: Ask the user in this session, once the build is done, to answer the toy form in an interactive Claude Code session and an interactive Codex session started with per-run config; without a yes, both are recorded as not verified and M20's manual run covers them. The master session's brief says a check that needs an interactive client, or a run on the user's Codex login, goes through the user. The decision in Q1 rests on the docs, the code and the headless runs, so the build doesn't wait for them.

## Log

- plan started 2026-10-28 from the master session's brief (the BUILD-PLAN Phase 5 section: the M19 row, the tools table's `deploy_apply` row, "Results and errors", the research basis and decision 26; build-order item 44)
- `powerops plan facts` not run: the build needs no environment; `environment` stays null
- depth: quick (from `m18-mcp`)
- the user delegated their parts on 2026-10-22 (relayed by the master session): this session answers with the recommended options and signs the plan off itself after a fresh review with no High or Medium finding left; a run on the user's Codex login and an interactive check still need their yes in this session (Q5)
- read on 2026-10-28: the `ModelContextProtocol.Core` 2.3.0 XML docs (`ElicitAsync`, `ClientCapabilities`, `IsMrtrSupported`, `InputRequiredResult`) and its source at tag `v2.3.0` (`McpServer.Methods.cs`: `ElicitAsync` and its capability check; `McpServerImpl.cs`: `WrapHandlerWithMrtr`, `AwaitMrtrHandlerAsync`; `ElicitResult.WithDefaults`; `ElicitationCapability`'s converter, which reads `{}` as form); Codex at `rust-v0.163.0` (commit 83086188): `codex-mcp/src/elicitation.rs`, `core/src/session/mcp.rs` (the reviewer), `exec/src/lib.rs`, `tui/src/bottom_pane/mcp_server_elicitation.rs`, `tui/src/chatwidget/tool_requests.rs`, `core/src/elicitation.rs`; the Claude Code docs `mcp.md` ("Respond to MCP elicitation requests", "Elicitation modes Claude Code declares", tool timeouts) and `hooks.md` (`Elicitation`)
- elicitation check on 2026-10-28, outside the repo: a console app on `ModelContextProtocol.Core` 2.3.0 alone with one tool whose low-level handler elicits a form of one string field (required, no default) and logs the revision, client, capabilities and answer. Claude Code 2.1.303 (`claude -p --mcp-config <file> --strict-mcp-config --no-session-persistence`, model haiku, run twice): revision 2026-07-28, client `claude-code/2.1.303`, `elicitation: {form: {}, url: {}}`, the form answered `cancel` with no content within 15 ms, and the model saw only the tool's text. Codex 0.163.0 (`codex exec` with `-c mcp_servers.toy...` and a throwaway `CODEX_HOME`, no login, deleted afterwards): revision 2025-06-18, client `codex-mcp-client/0.163.0`, `elicitation: {form: {}, url: {}}` at `tools/list`; no call (401 without a login, as expected). The user's Claude Code and Codex config were not changed
- approved without: users, data, constraints (the BUILD-PLAN Phase 5 section covers them)
- approved (2026-10-28)
- fresh review (powerops-reviewer, Sonnet) 2026-10-28: 0 High, 4 Medium, 7 Low; all Medium and six Low fixed in the plan text, R5 (two environments with one friendly name) accepted with a reason. One answer changed: Q2's form message no longer names the plan file, whose name the agent chooses (R6); the target name must be readable and typeable, or the call is refused before the form. No user decision needed. The reviewer contacted no tenant
- signed off 2026-10-28 by this session under the user's delegation (see above): the review left no High or Medium finding open, the condition the user set on 2026-10-22 for agents signing off plans themselves
- 2026-10-28: built step 3 (`6d5ed54`) by a powerops-implementer (Sonnet) from this plan, checked by this session: `dotnet build --no-incremental` (0 warnings), `dotnet test` (2165 passed, 102 in `McpCommandTests`), `dotnet format --verify-no-changes`, `dotnet pack src/PowerOps.Cli -c Release -o artifacts` and `powerops plan status .` are clean, and the golden recipe leaves no diff (only `mcp-tools.txt` changed in the build). The implementer broke each safety rule once and saw a test fail; this session repeated it for the dry run without `--apply` (three tests failed). Two details beyond the plan text: the tool checks `File.Exists` before its plan file read, so a path the read cannot open goes to the dry run; and test 11(b) waits for the handler's token to be cancelled before it answers the form, because the SDK handles the cancel and the answer concurrently (if the accept arrives first, the user's typed yes stands and step 8 is the only check left). `PackedToolTests` now expects 12 tools. No tenant was contacted. Status `implementing` until Q5 is answered
- 2026-10-28: the master session's review of the build (independent, Sonnet): 0 High, 1 Medium, 5 Low. Plan amended (status back to `approved`, then signed off again). The Medium: the plan hash covers the environment id, not the file's `target` text, so an agent that can write the deploy folder could swap only that text between the tool's reads and the CLI's; now the tool runs the dry run before the form and goes on only when it passes with the plan's target and hash, so the typed name is the live name of the environment that is imported into. Lows fixed: a lone surrogate in the plan file is refused as not a plan file (CLI and tool), `Zl`, `Zp` and `Co` characters refused in the target name, tests for a malformed and a symlinked plan file and a url-only client, and a two-sentence description. Accepted: a client or hook that answers the form automatically (decision 26; M20's README says not to). Also: FOUNDATION §8's M18 item to be ticked
- signed off again 2026-10-28 by this session under the user's delegation, with no High or Medium finding open
