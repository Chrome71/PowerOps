# Decisions: m18-mcp

## Round 1

### Q1. Which SDK package and which server API?
1. `ModelContextProtocol.Core` 2.3.0 alone, with `McpServer.Create` over `StdioServerTransport` and the low-level `ListToolsHandler` and `CallToolHandler`; the input schemas are written by hand from one field table that the input checks also use (recommended)
2. Core with `[McpServerTool]` methods, whose input schemas the SDK infers from the C# parameters
3. In your own words
Answer: `ModelContextProtocol.Core` 2.3.0 alone, with `McpServer.Create` over `StdioServerTransport` and the low-level `ListToolsHandler` and `CallToolHandler`; the input schemas are written by hand from one field table that the input checks also use. The spike showed that Core alone hosts a stdio server for both clients, so decision 23's fallback is not needed. Inferred schemas would not carry `minItems`, `additionalProperties: false` or the path rules, and the server would still have to check every value itself.

### Q2. How strict are the input checks, and how is an error returned?
1. Refuse unknown fields too, report every problem at once as `error: <field>: <problem>` lines that never quote a value, and return stdout and stderr as separate text blocks (stdout first, each only when not empty) (recommended)
2. Ignore unknown fields and stop at the first problem
3. In your own words
Answer: Refuse unknown fields too, report every problem at once as `error: <field>: <problem>` lines that never quote a value, and return stdout and stderr as separate text blocks (stdout first, each only when not empty). A misspelt optional field such as `basline` would otherwise be dropped without a word; the CLI's own commands list all their problems at once; a value can be a URL or a path that the CLI never prints. Separate blocks keep the JSON parseable as it is, and on exit 0 or 1 stderr is empty today, so a success is the one block the BUILD-PLAN table describes.

### Q3. How does `deploy_plan` save the plan file?
1. Check the name and that `<folder>/<save>` does not exist before the target is read, run `deploy plan` without `--save`, then write its stdout with `FileMode.CreateNew` on exit 0 or 1; the description names `powerops deploy apply` until M19 adds `deploy_apply` (recommended)
2. Pass `--save <folder>/<save>` to the command, which replaces an existing file
3. In your own words
Answer: Check the name and that `<folder>/<save>` does not exist before the target is read, run `deploy plan` without `--save`, then write its stdout with `FileMode.CreateNew` on exit 0 or 1; the description names `powerops deploy apply` until M19 adds `deploy_apply`. The early check spends no tenant read on a call that would be refused, and `CreateNew` still refuses a file that appears in between. `--save` uses `File.WriteAllText`, which replaces a file (the BUILD-PLAN row rules that out). Naming a tool that M18 does not serve would send an agent looking for it.

## Round 2

### Q4. How does the end-to-end test talk to the packed tool?
1. Hand-written JSON-RPC lines over the process's redirected streams (`initialize` at 2025-11-25, `notifications/initialized`, `tools/list`), inside the existing packed-tool test, checking every stdout line (recommended)
2. The SDK's `StdioClientTransport` in a test of its own, which packs the tool a second time
3. In your own words
Answer: Hand-written JSON-RPC lines over the process's redirected streams (`initialize` at 2025-11-25, `notifications/initialized`, `tools/list`), inside the existing packed-tool test, checking every stdout line. The SDK client would skip or log a stray stdout line instead of failing, and the in-process tests already use the SDK client (and its default revision); the raw lines cover the `initialize` revisions that Codex uses. One pack keeps the test's time as it is.

### Q5. How is the Codex half of the spike checked?
1. List the tools through `codex exec` with a throwaway `CODEX_HOME` and no login (no model call), and run the tool call with the user's Codex login only after their yes in this session; without the yes, the call is recorded as not verified (recommended)
2. Use the user's Codex login without asking
3. In your own words
Answer: List the tools through `codex exec` with a throwaway `CODEX_HOME` and no login (no model call), and run the tool call with the user's Codex login only after their yes in this session; without the yes, the call is recorded as not verified. The master session's brief says a run on the user's Codex login needs that yes; listing alone shows the handshake and the revision.

## Log

- plan started 2026-10-27 from the master session's brief (the BUILD-PLAN Phase 5 section, its tools table, "Results and errors", the M18 row, the Phase 5 research basis and decisions 23 to 29; build-order items 42 and 43)
- `powerops plan facts` not run: the build needs no environment; `environment` stays null
- depth: quick (from `m16-playwright`)
- the user delegated their parts on 2026-10-22 (relayed by the master session): this session answers with the recommended options and signs the plan off itself after a fresh review with no High or Medium finding left; a run on the user's Codex login still needs their yes in this session (Q5)
- read on 2026-10-27: the `ModelContextProtocol.Core` 2.3.0 package (its XML docs for `McpServer`, `McpServerOptions`, `McpServerHandlers`, `StdioServerTransport`, `StreamServerTransport`, `StreamClientTransport`, `McpClient` and `McpServerOptions.ProtocolVersion`, and the nuspec licences and net10.0 dependencies); the CLI's commands, `PacRunner`, `DriftLiveReader` and the tests' fakes in this branch
- observed with the built tool on 2026-10-27: `powerops rules @<file>` expands the file into arguments; `powerops analyze "[diagram]"` and `"[env:POWEROPS_DEBUG=1]"` are plain path values after the subcommand
- spike on 2026-10-27, outside the repo: a console app on `ModelContextProtocol.Core` 2.3.0 alone (no host package) with one read-only tool, `McpServer.Create` over `StdioServerTransport` and the low-level handlers, logging each request's negotiated revision and client to a file. Piped JSON-RPC lines: `initialize`, `tools/list` and `tools/call` answered, nothing else on stdout, nothing on stderr, exit 0 when stdin closed (a request still in flight when stdin closes gets no answer). Claude Code 2.1.303 (`claude -p --mcp-config <file> --strict-mcp-config --no-session-persistence`, model haiku): listed and called the tool, revision 2026-07-28, client `claude-code/2.1.303`. Codex 0.163.0 (`codex exec` with `-c mcp_servers.spike.command=...` and a throwaway `CODEX_HOME`, no login, deleted afterwards): listed the tool, revision 2025-06-18, client `codex-mcp-client/0.163.0`; its model request failed with 401 as expected, so no call. The Codex call waits on the user's yes (Q5)
- approved without: users, data, constraints (the BUILD-PLAN Phase 5 section covers them; licensing is in Constraints)
- approved (2026-10-27)
- fresh review (powerops-reviewer, Sonnet) 2026-10-27: 0 High, 6 Medium, 7 Low; all Medium and six Low fixed in the plan text, R7 (Windows UNC paths) accepted with a reason, R11 fixed without a test; no decision changed and no user decision needed. R2 was checked with the built tool before the fix: positional values after `--` are plain values
- the reviewer ran one read-only `pac org who --environment` call while testing a value (it failed, exit 1); no other tenant call was made during planning
- signed off 2026-10-27 by this session under the user's delegation (see above): the review left no High or Medium finding open, the condition the user set on 2026-10-22 for agents signing off plans themselves
- 2026-10-27: built steps 3 and 4 (`f30c532` the offline tools, `60fa6f4` the tools that read a tenant), each by a powerops-implementer (Sonnet) from this plan and checked by this session. `dotnet build` (0 warnings), `dotnet test` (2111 passed), `dotnet format --verify-no-changes`, `dotnet pack src/PowerOps.Cli -c Release -o artifacts` (the package holds `ModelContextProtocol.Core` and its three MIT dependencies only) and `powerops plan status` are clean; the golden recipe leaves no diff. Claude Code 2.1.303 listed the built server's 11 tools and called `diff` through a per-run `--mcp-config`. Not covered by a test: a write that fails after `save` created its file. The Codex tool call waits on Q5 (the user's yes); status stays `implementing` until then
- 2026-10-27: Q5 answered: the user said yes in this session to the one Codex tool call. Codex 0.163.0, on the user's login, with the spike server set for that run only (`codex exec --ephemeral -c mcp_servers.spike.command=...`; `~/.codex/config.toml` unchanged), listed the tool and called it over revision 2025-06-18 (client `codex-mcp-client/0.163.0`), with no approval prompt (the tool is read-only), 14,180 tokens. Step 1 is complete, so the status is `done`
- 2026-10-27: the master session's review of the build (independent, Sonnet): 0 High, 2 Medium, 5 Low. Plan amended (status back to `approved`, then signed off again): UNC and device paths refused on every OS (review R7, accepted before, now fixed), `deploy_plan` skips `save` once the call is cancelled, `drift` and `deploy plan` forward text after `  | `, Windows device names refused in `save`, and tests for `/h` and `/?` in two more positional fields, a real `baseline`, `plan.Settings.json` and the UNC refusal. Accepted without change: no `maxItems` on `envs`; no in-process test at revision 2026-07-28 beyond the SDK client's default (the in-process tests already negotiate it)
- seen in a scratch project on 2026-10-27: the server cancels a call's token on `notifications/cancelled` (2025-06-18 and 2025-11-25); the SDK's client sends none when its own call is cancelled, so the cancellation test writes raw JSON-RPC lines
- signed off again 2026-10-27 by this session under the user's delegation, with no High or Medium finding open
