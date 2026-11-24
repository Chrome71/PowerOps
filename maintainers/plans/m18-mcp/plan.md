---
status: done
review: done
signed-off: 2026-11-01
signed-off-hash: 431b7ba645abc4e5b40daedec574467a61a7d32fc9315046dc25d1a6470a733b
environment: null
depth: quick
supersedes: null
---

# M18: `powerops mcp`, the read-only tools

## Goal

Build BUILD-PLAN milestone M18 (build-order items 42 and 43, release label v0.17.0): `powerops mcp`, a stdio MCP server inside the existing tool whose 11 tools run the CLI's own commands in-process and return their JSON. The spec is the BUILD-PLAN section "Phase 5: agents (M18–M20)": its tools table, "Results and errors", the M18 row and decisions 23 to 29. This plan settles what the row leaves open: the spike, each tool's exact input schema, and the argument mapping. `deploy_apply` is M19 and is left out.

Done when:

- `powerops mcp` serves exactly these tools, in this order: `analyze`, `rules`, `baseline`, `diff`, `test`, `settings_generate`, `settings_validate`, `plan_status`, `plan_facts`, `drift`, `deploy_plan`. `tools/list` equals the golden `tests/golden/mcp-tools.txt` (names, descriptions, input schemas, all four annotations).
- Offline, each tool's text on the fixtures equals the CLI's stdout for the same arguments and validates against its schema in `docs/schemas/` (`baseline` and `settings_generate` return a file's text, which has no schema: they equal the file the CLI writes or prints).
- Exit codes 2 and 3 give `isError: true`; a malformed plan's `plan_status` result still holds its JSON. A value that starts with `-`, an `@` value naming a file that holds `--apply`, an empty value, a relative path, a wrong type, a missing field and an unknown field are each refused with `isError: true`, and no command runs. A positional value such as `/h` is a value, never the help option.
- `drift` runs offline on snapshot files; `drift`, `plan_facts` and `deploy_plan` run with fake `pac`, live-read and target functions. `deploy_plan` with `save` writes only that file in `folder`, and refuses an existing file, a name with a folder in it, a `.settings.json` name and a name that does not end in `.json`.
- An end-to-end test starts the packed tool's `powerops mcp` over real stdio, lists the 11 tools, calls `rules`, closes stdin, and sees exit 0, an empty stderr and only JSON-RPC lines on stdout.
- `dotnet build` (0 warnings), `dotnet test`, `dotnet format --verify-no-changes`, `dotnet pack src/PowerOps.Cli -c Release -o artifacts` and `powerops plan status` are clean. No tenant is contacted.

## Users

- Agents in Claude Code and Codex (the main users of PowerOps), which call the tools instead of running the CLI through a shell.
- People who register the server by hand before M20 ships it in the plugins.

## Data

- **Input**: each tool's `arguments` object, checked by the server before any command runs (Automation, "Input checks").
- **Output**: one `CallToolResult` per call, text content only. No `outputSchema` and no `structuredContent` (decision 24). The JSON is the CLI's `--format json` (or `--json`) output byte for byte, with `schemaVersion` first.
- **Files written**: only `deploy_plan`'s `save`, one new file in `folder` (Automation). `baseline` and `settings_generate` return the text instead of writing it. Nothing else writes.
- **Paths in the output**: `analyze` and `plan_status` print a path relative to the current folder when it is under it, else absolute, as the CLI does. Under MCP the current folder is the one the client starts the server in, so an agent reads paths as given; the tests run the tool and the CLI from the same folder.
- No new schema: `powerops mcp` prints protocol messages, not a report.

## Automation

**Files.**

- `src/PowerOps.Cli/McpCommand.cs`: `public static class McpCommand`, with `Create(TextWriter error, pac, driftLive, deployTarget)` returning the `mcp` command, and `internal static McpServerOptions Options(pac, driftLive, deployTarget)`, which holds the tool table, the input checks and the call handler. Tests build a server from `Options` over in-process pipes. The mapping from exit code and output to a result is an `internal static` function, so its edge cases are tested directly. The recipe puts report logic in `PowerOps.Core/Output/`; this has no report, and the MCP code stays in `PowerOps.Cli` so that Core gets no new dependency (decision 23).
- `src/PowerOps.Cli/PowerOps.Cli.csproj`: `PackageReference` `ModelContextProtocol.Core` 2.3.0. `PowerOps.Core` gets no new dependency.
- `src/PowerOps.Cli/Program.cs`: `Root` resolves `driftLive` and `deployTarget` once (the given delegate, else one over its lazy `DriftLiveReader`) and passes them to `drift`, `deploy` and `mcp`. A tool call runs `Program.Run(args, out, err, pac, driftLive, deployTarget)` with those same delegates, so the inner command tree never builds its own reader: the session has one live reader, one `HttpClient` handler, built on the first live read. `PlanCommand.Create` gets the `pac` function too.
- `src/PowerOps.Cli/DriftCommand.cs` and `DeployCommand.cs`: `drift` and `deploy plan` write forwarded `pac`, `az` and Dataverse text after `  | `, as `deploy apply` already does (one shared helper), so a forwarded line can never look like powerops' own `error:` line.
- `src/PowerOps.Cli/PlanCommand.cs`: `plan facts` runs `PlanFacts.Run(a => pac(a, 30 s))` instead of `new PacRunner().Run` (the same 30 s default as today).
- `README.md`: one line in the usage block and a short paragraph under it. M20 adds the "Use from an agent (MCP)" section.

**The `mcp` command.** No options and no HTTP transport. An async `SetAction((parse, cancellationToken) => ...)`: `McpServer.Create(new StdioServerTransport("powerops"), Options(...))`, then `RunAsync(cancellationToken)` until the client closes stdin (or System.CommandLine cancels the token on Ctrl+C or SIGTERM): exit 0. `Program.Run`'s `parse.Invoke` runs an async action to its end. An exception gives one `error:` line on stderr and exit 3, as `SettingsCommand.Guard` writes it. No logger factory is passed, so the SDK logs nothing; stderr gets only that line. Server info: name `powerops`, version `AnalyzeCommand.ToolVersion()`. `ProtocolVersion` is left unset, so the server takes every revision the SDK supports (2024-11-05 to 2025-11-25 over `initialize`, and 2026-07-28). The low-level `McpServerHandlers.ListToolsHandler` and `CallToolHandler` serve the tools, with hand-written input schemas generated from one field table, so the schema the client sees and the checks the server runs come from the same list.

**Tools.** Every path input must be absolute (`Path.IsPathFullyQualified`). "Read-only" is `readOnlyHint: true`, `destructiveHint: false`, `idempotentHint: true`, `openWorldHint: false`; every tool sets all four. Each argument list is the subcommand, then the options with their values, then `--`, then the positional values. After `--` System.CommandLine reads every token as a value: `/h`, `/?` and `-?` are help aliases that a positional `/h` or `/?` would otherwise trigger (they pass the absolute-path check on Linux and macOS; `powerops analyze /h` prints help and exits 0), and an `@file` after `--` is not expanded (observed with the built tool on 2026-10-27). An option's value is read as a value either way (`--baseline /?` gives "baseline file not found").

| Tool | Input (required unless optional) | Runs | Annotations |
|---|---|---|---|
| `analyze` | `paths`: list of absolute paths, at least 1; `baseline`: absolute path, optional | `analyze --format json [--baseline <baseline>] -- <paths...>` | read-only |
| `rules` | none | `rules --json` | read-only |
| `baseline` | `paths`: list of absolute paths, at least 1 | `AnalyzeCommand.Run(paths, null, err, r => { Baseline.Write(r, text); return 0; })`; returns `text` | read-only |
| `diff` | `old`, `new`: absolute paths | `diff --format json -- <old> <new>` | read-only |
| `test` | `path`: absolute path; `tests`: absolute path, optional | `test --format json [--tests <tests>] -- <path>` | read-only |
| `settings_generate` | `solution`: absolute path | `settings generate -- <solution>` | read-only |
| `settings_validate` | `solution`, `settings`: absolute paths | `settings validate --settings <settings> --format json -- <solution>` | read-only |
| `plan_status` | `path`: absolute path | `plan status --json -- <path>` | read-only |
| `plan_facts` | none | `plan facts` | read-only, `openWorldHint: true` |
| `drift` | `solution`: string; `envs`: list of strings, at least 2 | `drift --solution <solution> --env <e1> --env <e2> ... --format json` | read-only, `openWorldHint: true` |
| `deploy_plan` | `folder`: absolute path; `target`: string; `save`: file name, optional | `deploy plan --target <target> --format json -- <folder>`, then the server writes the result to `<folder>/<save>` | `readOnlyHint: false`, `destructiveHint: false`, `idempotentHint: false`, `openWorldHint: true` |

Each input schema is `{"type": "object", "properties": {...}, "required": [...], "additionalProperties": false}`. A path or a string is `{"type": "string", "description": ...}`; a list is `{"type": "array", "items": {"type": "string"}, "minItems": N, "description": ...}`. `rules` and `plan_facts` have `"properties": {}`. Field descriptions are one short sentence: `paths` "Absolute paths of solution folders or exported zips; folders are searched recursively."; `envs` "At least two: an entry ending in .json is the absolute path of a snapshot file, any other is an environment id, URL or name (as pac --environment)."; `save` "Optional: a new file name (letters, digits, '.', '_', '-', ending in .json, not .settings.json) for the plan file in folder; an existing file is never replaced."; and so on for each field, as the golden pins them.

**Descriptions.** One or two sentences each: what the tool checks, that it is read-only or what it writes, and its schema file. `baseline` and `settings_generate` say they return the file's text and write nothing (the baseline format has its own `version`; the settings file is pac's format). `plan_facts`, `drift` and `deploy_plan` say they read a tenant through pac, az and the Dataverse Web API, which tells an agent what must be signed in. `deploy_plan` says it writes only the file `save` names, as a new file in `folder`, and that a reviewed plan is promoted with `powerops deploy apply`, not with an import tool. M19 changes that clause to name `deploy_apply` when the tool exists. Decision 16(b) is about the tools of Microsoft's MCP servers (pac's and Dataverse's): no description names one of them. Naming the CLIs the command runs is not that.

**Input checks**, in this order, all reported at once as `error: <field>: <problem>` lines (field names only, never a value, because a value can be a URL or a path):

1. Unknown field: refused (the schema says `additionalProperties: false`).
2. Missing required field, wrong JSON type (a string where a list is expected, a number, a list with a non-string item), a list below its `minItems`. JSON `null` for an optional field means the field is absent; for a required field it is missing.
3. Any string, in a list or not, that is empty or only white space, or that starts with `-` or `@`: refused. An empty `envs` entry would be a live environment named "" and run `pac`. A leading `-` would be read as a flag such as `--apply` (option values come before `--`); a leading `@` as a response file, which System.CommandLine expands into arguments (observed with the built tool on 2026-10-27: `powerops rules @<file>` read the file's `--format json` as two arguments). Every argument list starts with the subcommand name, so a `[name]` value after it is a plain value, not a directive (observed the same day).
4. A path field, or any `drift` `envs` entry, that starts with `\\` or `//` is refused on every OS: on Windows it is a UNC or device path (`\\host\share`, `\\.\`, `\\?\`), and opening one can send the user's NTLM credentials to another host, while a read-only tool may run without a prompt; on Unix `//x` is `/x`, so nothing is lost. A path field that is not absolute: refused. In `drift`'s `envs`, an entry that ends in `.json` (any case) is a path and must be absolute; any other entry is an environment, but one that names an existing file is refused, because `drift` would read it as a snapshot file relative to the server's folder.
5. `save`: must match `^[A-Za-z0-9._-]+\.json$`, must not end in `.settings.json` (any case), must not be a Windows device name (the part before the first `.` is `CON`, `PRN`, `AUX`, `NUL`, `COM0` to `COM9` or `LPT0` to `LPT9`, any case; refused on every OS, as `FileStream` throws `NotSupportedException` for one on Windows), and `<folder>/<save>` must not exist as a file or folder.

A refused call runs no command and reads no tenant.

**Results.** After the input checks, the tool runs its command with a `StringWriter` for stdout and one for stderr. The content is a text block with stdout when it is not empty, then a text block with powerops' own `error:` lines from stderr, as the BUILD-PLAN table says, when there are any. Other stderr lines are dropped: on a failed live read `drift` and `deploy plan` forward `pac`, `az` or Dataverse text there (after `  | `), which can hold an environment URL or an account name, and `POWEROPS_DEBUG=1` adds a stack trace; both are for a person's terminal, not for the model's context. Exit codes 0 and 1 give `isError: false`, 2 and 3 `isError: true`. Two cases are errors whatever the code says: exit 1 with an empty stdout (System.CommandLine's own handler returns 1 for an exception a command does not catch; every real exit 1 prints a report), and a code other than 0 to 3. A tool error with no `error:` line gets `error: the command failed (exit code N)`. Today no command writes to stderr on exit 0 or 1, so a success is one block. An unknown tool name, or a call with no params, makes the handler throw `McpProtocolException` with `McpErrorCode.InvalidParams`, which the client gets as a JSON-RPC error: with the low-level handler every name reaches it (seen in a scratch project on 2026-10-27), and a plain exception would become a generic tool error. The handler passes the call's cancellation token to `deploy_plan` only: the commands take none, so a cancelled call still finishes its read, but once the client has cancelled the call (`notifications/cancelled`, which the SDK turns into that token) the plan file is not written, and the result is an error with `error: save: the call was cancelled; the plan file was not saved`. Otherwise a client that timed out (Codex's default is 60 s) and retried would find its own file and be refused. Seen in a scratch project on 2026-10-27: the server cancels the handler's token on `notifications/cancelled` at 2025-06-18 and 2025-11-25, and the SDK's own client sends none when its call is cancelled.

**`deploy_plan`'s `save`.** The command runs without `--save`. On exit 0 or 1 (a plan exists, blocked or not, as `--save` writes it), the server writes the stdout text, which is `DeployPlanReport.WriteJson`'s output as `--save` writes it, to `Path.Combine(folder, save)` through a `FileStream` with `FileMode.CreateNew` (UTF-8, no BOM), so a file that appeared since the check is not replaced either. If opening the file fails, the result is `isError: true` with the JSON and `error: save: the plan file was not saved (it exists, or the folder is not writable)`. If the write fails after this call created the file, the server deletes that file, and only that one, and returns the same error. On exit 2 or 3 nothing is written.

**Concurrency.** Claude Code runs read-only calls in parallel. The SDK runs each request on its own task: in a scratch project on 2026-10-27, two calls to a handler that blocked for 500 ms started together, so a slow `drift` does not hold up `tools/list` or another call. The commands share no mutable state: `DriftLiveReader` holds only its three functions, its web function shares one `HttpClient`, and no command writes to `Console` or changes the current folder (read in the code on 2026-10-27). `PacRunner` redirects the child's stdin, stdout and stderr, so a `pac` or `az` child never reads the MCP stream or writes to it.

**Tests** (`tests/PowerOps.Cli.Tests/McpCommandTests.cs`), with the SDK's `McpClient` and `McpServer` over two `System.IO.Pipelines` pipes (`StreamClientTransport`, `StreamServerTransport`), the server built from `McpCommand.Options` with fake functions:

1. `tools/list`: the tools serialized as indented JSON equal `mcp-tools.txt`; every tool has all four annotations set.
2. A theory over the offline tools on the fixtures: the tool's text equals the stdout of `Program.Run` with the mapped arguments, and validates against its schema (`OutputSchemaTests`' loader and validator, made `internal`): `analyze` on `tests/fixtures`, `rules`, `diff` on `powerops-seed/v1` and `v2`, `test` on `alm-makers` with its tests folder, `settings_validate` on seed v1 with the valid settings fixture, `plan_status` on a temporary plan folder, `drift` on `tests/fixtures/drift/seed-dev.json` and `seed-test.json`. `settings_generate` equals the CLI's stdout; `baseline` equals the file `powerops baseline --out <temp file>` writes.
3. With fakes, each equal to the CLI run with the same fake and valid against its schema: `plan_facts` (a fake `pac` that prints `tests/fixtures/pac/org-who.txt` and a solution table); `drift` with one live environment (a fake live read) and one snapshot file; `deploy_plan` on a folder with the seed v1 zip and a fake target, as `DeployCommandTests` does.
4. Errors: `analyze` on an empty temporary folder (exit 2) and `diff` on a file that is not a zip (exit 3) give `isError: true` with `error:` text; `plan_status` on a malformed plan gives `isError: true` with the plan-status JSON in the first block. `analyze` with `paths` `["/h"]`, `diff` with `old` `"/?"`, `settings_validate` with `solution` `"/h"` and `deploy_plan` with `folder` `"/?"` give the CLI's not-found error, not help text. `analyze` with a real `baseline` (written by the `baseline` tool) equals the CLI run with `--baseline` and lists fewer findings than without it.
5. Refusals, each `isError: true` with exactly the server's `error: <field>: <problem>` lines (not a CLI or SDK message), with `pac`, live-read and target fakes that count their calls, asserted zero afterwards: `analyze` with `--help` in `paths`; `deploy_plan` with `target` `@<file>` and with `folder` `@<file>`, where the file holds `--save <temp path>`, `--out <temp path>` and `--apply`, and neither temp path exists afterwards; `diff` with a relative `old`; `drift` with an env entry starting with `-`, an empty entry, a relative `.json` entry, and a non-`.json` entry that names an existing file; a string for `paths`; a number for `old`; a missing `new`; `null` for the required `new`; an unknown field on `rules`; `\\host\share\x` and `//host/share/x` as an `analyze` path and as a `drift` entry (and the check itself as a function). A positive control: the same `drift` and `deploy_plan` calls with clean values do call the fakes once. `null` for an optional field (`baseline`, `tests`, `save`) runs the tool as if the field were absent. An unknown tool name gives a JSON-RPC error (`McpProtocolException` on the client), not an `isError` result.
6. `deploy_plan` with `save`: the folder holds exactly one new file, whose text equals the result; an existing file is refused before the target is read and keeps its content; `sub/plan.json`, `..\plan.json`, `plan.settings.json`, `Plan.Settings.JSON`, `plan.Settings.json`, `plan.txt`, `CON.json`, `nul.plan.json` and `Lpt1.json` are refused before the target is read.
7. The result mapping as a function: exit 1 with an empty stdout is an error; exit 3 with no `error:` line gets `error: the command failed (exit code 3)`; stderr lines without the `error:` prefix are dropped. Through the server: `drift` with a live-read fake that throws `DriftReadException` with forwarded text gives `isError: true` without that text, and so does a forwarded line that starts with `error:` and holds a URL, through `drift` and through `deploy_plan`.
8. Concurrency: a `deploy_plan` call whose fake target blocks until the test releases it; while it blocks, `tools/list` and a `rules` call answer; then the release lets the first call finish. Cancellation: over raw JSON-RPC lines on the pipes (the SDK's client sends no cancellation), a `deploy_plan` call with `save` whose fake target blocks, then `notifications/cancelled` for it; once the handler's token is cancelled the target is released, and after the handler returns (the test wraps the handler to see both), `folder` holds no new file.
9. `PackedToolTests`: after the install, start `powerops mcp` with redirected streams (stdin as UTF-8 without BOM), write `initialize` (revision 2025-11-25), `notifications/initialized`, `tools/list` and a `tools/call` of `rules` as JSON-RPC lines, read each response with a timeout, then close stdin: exit 0 within the timeout, 11 tools, the `rules` text equal to `powerops rules --json`'s, an empty stderr, and every stdout line a JSON object with `"jsonrpc": "2.0"`. It runs inside the existing test, so the tool is packed once.

## Screens

None.

## Environments and route

- The build and every test: offline. No tenant, `pac` or `az` is called; the tools that read a tenant run with fakes (decision 28).
- The spike (Step 1) ran outside the repo on 2026-10-27, with no change to the user's Claude Code or Codex config: Claude Code through `claude -p --mcp-config <file> --strict-mcp-config --no-session-persistence`, and Codex through `codex exec -c mcp_servers.spike.command=...` with a throwaway `CODEX_HOME` that holds no login (deleted afterwards). A Codex tool call needs a model, so it needs the user's Codex login, and that needs the user's yes in this session (Q5).

## Solution and publisher

None.

## Constraints

- Licensing: dependencies MIT or Apache-2.0 only. `ModelContextProtocol.Core` 2.3.0 is Apache-2.0 (nuspec); on net10.0 it brings only `Microsoft.Extensions.AI.Abstractions` 10.11.0, `Microsoft.Extensions.Logging.Abstractions` 10.0.13 and `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.13, all MIT (nuspecs and `project.assets.json` read on 2026-10-27). JsonSchema.Net stays at 8.0.5 in the tests.
- Read-only by default (AGENTS.md): the one write is `save`, an explicit input, and it never replaces a file. No tool takes `--apply`, `--out` or `drift --save`.
- stdout carries only MCP messages (the MCP stdio transport).
- No version or changelog edits; no push or pull request from this session. Commits use Conventional Commits with no attribution lines.
- The SDK's licence file is moving from MIT to Apache-2.0 (Phase 5 research basis), both allowed. A Dependabot update of `ModelContextProtocol.Core` gets the licence check that JsonSchema.Net's did: the new version's nuspec licence and its net10.0 dependencies are read before it is merged.
- Deadline: none for M18 itself; M20's manual run needs Test, before it expires.

## Assumptions

- The SDK keeps running each request on its own task and turning an exception thrown by a handler into a generic tool error with no detail, as it did in the scratch project; test 8 checks the first. The handler catches nothing beyond what the commands already catch.
- Clients pass `arguments` as a JSON object; a missing `arguments` is read as an empty object.
- A path that is absolute on the server's OS is what agents send; a Windows path on Linux is refused as relative, which is correct for a local stdio server.

## Open questions

## Questions for your admin

## Steps

1. Spike, outside the repo: a server on `ModelContextProtocol.Core` 2.3.0 alone that Claude Code and Codex list and call over stdio, with the revision each uses written down. Done on 2026-10-27 before this plan was reviewed (`decisions.md`, Log); the Codex call waits on Q5.
2. `docs:` this plan, its decisions and review, signed off.
3. `feat(cli):` item 42: the package reference, `McpCommand` with the offline tools (`analyze`, `rules`, `baseline`, `diff`, `test`, `settings_generate`, `settings_validate`, `plan_status`), the input checks, `Program.Root` passing one set of live functions, the README line and paragraph, tests 1, 2 (offline tools), 4, 5 (the offline cases) and 7 (the mapping function), `OutputSchemaTests`' loader and validator made `internal`, and `mcp-tools.txt` with those eight tools.
4. `feat(cli):` item 43: `plan_facts` with `PlanCommand`'s injected `pac`, `drift` and `deploy_plan` with `save`, tests 2 (drift), 3, 5 (the cases with fakes), 6, 7 (the forwarded text), 8 and 9, the README paragraph's tool list, and `mcp-tools.txt` with all 11 tools.
5. Run `dotnet build` (0 warnings), `dotnet test`, `dotnet format --verify-no-changes`, `dotnet pack src/PowerOps.Cli -c Release -o artifacts` and `powerops plan status`; update goldens with the recipe and read the golden diff.
6. `fix(cli):` the master session's review of 2026-10-27: UNC and device paths, cancellation of `deploy_plan`'s `save`, forwarded text after `  | `, Windows device names in `save`, and the tests above that fail when a safety rule breaks.
7. `docs:` BUILD-PLAN: the M18 row marked built with a link to this plan, items 42 and 43, the M18 verification-record line (the spike's revisions, the checks, and what is not verified, naming the Codex tool call if Q5 got no yes); this plan's status.
