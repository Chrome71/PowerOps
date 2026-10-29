---
status: done
review: done
signed-off: 2026-10-29
signed-off-hash: c298d1cec8db40d1ef28a6354ae5eada527b3bbe4ac006b4a05c7b8b22bba3e6
environment: null
depth: quick
supersedes: null
---

# M24: `deploy_apply` and MCP inputs

## Goal

Build BUILD-PLAN milestone M24 (build-order item 49, label v0.20.2). The spec is the Phase 6 plan's rows S-2, S-3, S-5, S-4, S-8, C-9 and D-15c, its Accepted section, its Open questions S-3 and S-8, decision 34 and the M24 row of BUILD-PLAN, whose done-when is the bar. This plan settles how, and records what it found when it checked the points the rows left open. It is based on main at `c703306`, the squash of M21 (#56): M21 made `DeployCommand.DryRun` the one dry run of `deploy apply` and `deploy gate`, and gave the `deploy apply` import a re-hash of each zip before its import.

Done when:

- A test swaps a consistent plan file and zip after the form's yes, and nothing is imported. The import gets the hash the user approved inside the process (no new CLI option) and refuses, with status `refused`, when the re-plan's hash differs from it.
- S-3's answer and its source are in the verification record, and the residual is in the MCP section of `docs/USE-WITH-AGENTS.md`. No tool input changes for it (Data).
- A string with a control character (NUL included) or an unpaired surrogate, a string over 4,096 characters, more than 50 `paths` or more than 10 `envs` is a tool error with an `error:` line, and nothing runs. Each refusal has a test.
- `drift --env` refuses, with exit 2, a value that holds a path separator and is neither a URL nor an existing file, and reads no tenant for it.
- The descriptions of `drift`, `deploy_plan` and `plan_facts` say to use them only on an environment the user named, and `tests/golden/mcp-tools.txt` holds them.
- The S-8 test passes, and the text on a stopped server in `docs/USE-WITH-AGENTS.md` says what the test shows. The unanswered-form hang at revision 2025-06-18 is named there and in the M24 row as a residual (decisions Q2).
- `dotnet build --no-incremental` (0 warnings), `dotnet test`, `dotnet format --verify-no-changes` and `powerops plan status .` are clean. No version, no changelog and no CLI option, command or JSON schema changes.

## Users

- A person who lets an agent use PowerOps' MCP server, and whose `deploy_apply` import must be the plan they approved in the form.
- An agent whose tool call fails: it gets an `error:` line it can act on instead of a protocol error or a command's text.
- The maintainer, who reads the verification record for S-3 and S-8.

## Data

Evidence is marked observed (seen in this session, on this machine or in a source read) or inferred.

**S-2: where the approved hash goes.** Observed in the code: `McpCommand.DeployApply` reads the plan file (`Read()`), shows the form with `plan.Hash`, reads the file again after the yes and compares its hash and target, and then calls `Exec(Args(true))`, which runs `deploy apply --apply` through `Program.Run`. That run reads the file a third time (`DeployCommand.DryRun`), re-plans against the target, and its only hash comparison is the file's `reviewed.Hash` against the re-plan's `plan.Hash`. A plan file and zips swapped as a consistent pair after the second read give a file hash equal to the re-plan's, so the comparison passes and the import runs. The hash is SHA-256 over the target environment id, the zip hashes, the settings hashes, the snapshot hashes and the dependencies (`DeployPlan`), so a changed zip or settings file changes it; the target's friendly name is not in it.

**S-3: Learn's answer** (read 2026-10-29, Microsoft Learn; nothing was run against a tenant):

- "Use OAuth authentication with Microsoft Dataverse": the scope names the environment URL (its sample sets the scope to that URL plus `/user_impersonation`).
- "Troubleshoot access token signature validation errors" says the audience claim of a token is set by the `scope` sent when the token is requested. "Secure applications and APIs by validating claims" lists the audience among what an API must validate; it says "The appropriate audience is specified for the token." "Access tokens in the Microsoft identity platform" says a web API must accept only tokens whose `aud` claim is one of its own AppId URIs, and that its rules do not apply to tokens for Microsoft-owned APIs (Dataverse is one; inferred).
- "Authenticate with Microsoft Dataverse web services", "Use single-tenant server-to-server authentication" and "Use multi-tenant server-to-server authentication" say nothing about what Dataverse does with a token whose audience is another organization's URL. Searches for a 401 or an audience mismatch on Dataverse found no page that says it.
- So Learn gives no `aud` value for Dataverse and says nothing on cross-org use. It does not say such a token works elsewhere, so the stop condition (decisions Q11) is not met, and the S-3 row's rule applies: the host suffix check (`DriftParsers.IsDataverseHost`, M21's rule, unchanged) stays, the answer is recorded, the residual is documented, and no tool input changes. The residual: a `target` or `envs` value the caller chooses that resolves to any documented Dataverse host gets a token from the user's `az` sign-in, requested for that host; whether that token would work at another org is not known, and neither is what `pac org who --environment` does with a URL on a non-Dataverse host. Both would need a live read, which this plan does not make.
- Sources:
  - Use OAuth authentication with Microsoft Dataverse: https://learn.microsoft.com/power-apps/developer/data-platform/authenticate-oauth <!-- plan-status: allow -->
  - Authenticate with Microsoft Dataverse web services: https://learn.microsoft.com/power-apps/developer/data-platform/authentication <!-- plan-status: allow -->
  - Use single-tenant server-to-server authentication: https://learn.microsoft.com/power-apps/developer/data-platform/use-single-tenant-server-server-authentication <!-- plan-status: allow -->
  - Use multi-tenant server-to-server authentication: https://learn.microsoft.com/power-apps/developer/data-platform/use-multi-tenant-server-server-authentication <!-- plan-status: allow -->
  - Secure applications and APIs by validating claims: https://learn.microsoft.com/entra/identity-platform/claims-validation <!-- plan-status: allow -->
  - Access tokens in the Microsoft identity platform: https://learn.microsoft.com/entra/identity-platform/access-tokens <!-- plan-status: allow -->
  - Troubleshoot access token signature validation errors: https://learn.microsoft.com/troubleshoot/entra/entra-id/app-integration/troubleshooting-signature-validation-errors <!-- plan-status: allow -->

**S-8: what the SDK does when stdin closes.** Read and run on 2026-10-29: the C# SDK's source at v2.3.0 (the version `PowerOps.Cli` references) and a throwaway probe server built on the same package, in the session's scratchpad, not in the repo. `powerops mcp` uses `StdioServerTransport`, which is a `StreamServerTransport` over the console streams, and the tests use `StreamServerTransport` over pipes, so one behavior covers both.

- Observed (probe): with a handler blocked for 5 s on a synchronous call, as `Exec(Args(true))` is, and stdin closed after 1 s, the process exited about 4 s later, after the handler returned, and wrote nothing to stdout. So the host does wait for a running handler, and no report comes back. The import case needs no fix; the S-8 row's "wait for the one-import guard before exiting" is not needed.
- Observed (probe): with the server waiting on a form the client never answers, under revision 2025-06-18 (where the form is a server-to-client request), the process was still running 12 s after stdin closed, until killed. Under revision 2026-07-28 the form is a result that asks for input, nothing is outstanding, and the server exited at once. The cause is that the run loop ends only when its token is cancelled or the handlers finish. A server that ignores end of input is against the MCP rule that servers should exit promptly when input closes (the stdio transport page of revision 2026-07-28); a client then waits and kills the process.
- Observed (probe), not adopted: cancelling the run token when the transport's reader completes (`transport.MessageReader.Completion`) ends the unanswered-form case at once. But it cancels every handler still running (`ct cancelled: True` for the blocked handler, whose import still ran to its end because `Exec` ignores the token). A `deploy_plan` with `save` still running at end of input would then skip its save, and a `deploy_apply` between the yes and the import would take the cancelled-call refusal. The S-8 row asks only for a test of what the host does, and the host waits, so this plan adds no such fix and documents the hang as a residual (decisions Q2). A later change could end the form's wait alone at end of input.
- Inferred, not run: what a client does after the grace period. The spec says to close stdin, wait, then send `SIGTERM` and `SIGKILL` (or `TerminateProcess` on Windows). A killed `powerops mcp` leaves its `pac` child importing and returns no report, as the docs already say. Not tested: a test cannot kill its own process.
- Sources, read 2026-10-29:
  - `StdioServerTransport.cs` at v2.3.0: https://github.com/modelcontextprotocol/csharp-sdk/blob/v2.3.0/src/ModelContextProtocol.Core/Server/StdioServerTransport.cs <!-- plan-status: allow -->
  - `StreamServerTransport.cs` at v2.3.0: https://github.com/modelcontextprotocol/csharp-sdk/blob/v2.3.0/src/ModelContextProtocol.Core/Server/StreamServerTransport.cs <!-- plan-status: allow -->
  - `McpSessionHandler.cs` at v2.3.0: https://github.com/modelcontextprotocol/csharp-sdk/blob/v2.3.0/src/ModelContextProtocol.Core/McpSessionHandler.cs <!-- plan-status: allow -->
  - `McpServerImpl.cs` at v2.3.0: https://github.com/modelcontextprotocol/csharp-sdk/blob/v2.3.0/src/ModelContextProtocol.Core/Server/McpServerImpl.cs <!-- plan-status: allow -->
  - the stdio transport page of revision 2026-07-28 (shutdown): https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/basic/transports/stdio.mdx <!-- plan-status: allow -->

**S-5: the mechanism in the row does not work for a lone surrogate.** Observed (probe, both revisions): a `tools/call` whose arguments hold a lone-surrogate escape in a value, a list item or a key gets JSON-RPC error -32603 ("An error occurred.") from the SDK before any handler runs, because the SDK cannot turn the request's parameters back into JSON text. So the row's "catch `InvalidOperationException` from `GetString()`" is not reachable for it; `Check` never sees the string. The same probe with an incoming message filter (`McpServerOptions.Filters.Message.IncomingFilters`, public in v2.3.0) that tries `Params.ToJsonString()` and, when it throws, sends a result with `isError: true` and one `error:` line for the request's id, gave a tool error for a value, a list item and a key. The filter catches `InvalidOperationException` from `Params.ToJsonString()`, the one exception the probe saw (for a value, a list item and a key), and nothing else; anything else still gets the SDK's -32603, as today.

**S-5: `resultType`.** The 2026-07-28 schema makes `resultType` required on results: its `Result` says servers implementing that revision "MUST include this field" (read 2026-10-29: https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/schema/2026-07-28/schema.ts), and the SDK adds `"resultType":"complete"` to handler results only at 2026-07-28 or later, because earlier revisions reject the key (SDK issue #1721; `McpServerImpl.cs`, `IsJuly2026OrLaterProtocolRequest`). A result sent from a filter bypasses that, so the filter stamps it itself: `refusal.ResultType = "complete"` when `request.Context?.ProtocolVersion ?? context.Server.NegotiatedProtocolVersion` is 2026-07-28 or later by ordinal string comparison, as the SDK does, and not before. Public in 2.3.0 (read in the source, and compiled in the probe): `JsonRpcRequest.Context` (a `JsonRpcMessageContext` with `ProtocolVersion`), `McpServer.NegotiatedProtocolVersion` (the filter reaches the server as `MessageContext.Server`) and the settable `Result.ResultType`. The SDK's own filter that reads `_meta` runs before user filters, so `Context.ProtocolVersion` is set when ours runs. Observed (probe): with this, the reply carried `resultType` at 2026-07-28 and no such key at 2025-06-18, and the tool error came back for a lone surrogate in a value, a key, a list item and inside `_meta` itself. The pair of surrogates that spells one character (an emoji) is valid text and passes. <!-- plan-status: allow -->

**S-4 and the rest of S-5: where the checks go.** All in `McpCommand.Check`, whose loop over `entries` already visits every string and every list item with its label (`paths 2`, `envs 1`, `target`) and keeps the `refused` set so that a value is refused once. The order in that loop stays: empty, then the new length and control-character rules, then M21's `-`/`@` and network-path rules. Every refusal is a tool error: `Call` already returns `isError` with the `error:` lines when `Check` finds problems. Messages hold the field label and never the value.

| Rule | Where | Message |
|---|---|---|
| a control character: any UTF-16 unit for which `char.IsControl` is true (U+0000 to U+001F, U+007F to U+009F; NUL, LF, CR, TAB, DEL, NEL) | `Check`, per string | `error: <label>: must not hold a control character` |
| a string longer than 4,096 UTF-16 units | `Check`, per string | `error: <label>: longer than 4096 characters` |
| more than 50 `paths`, more than 10 `envs` | `Check`, per list, before its items are read | `error: paths: give at most 50`, `error: envs: give at most 10` |
| a string with an unpaired surrogate | the incoming filter, in `McpCommand.Options`, so every server built from the options has it, the tests' too | `error: arguments: a string holds an unpaired surrogate` (the field is not known: the arguments cannot be read) |

The caps sit in two constants in `McpCommand`. They are not added to the tools' input schemas: the schema is in the golden and the row asks for tool errors, so an agent that goes over reads the limit in the error. The 4,096 cap keeps a `target` from E2BIG: one `pac` call carries one such value, far below the 32,767-character command line of `CreateProcess` (Learn, "CreateProcessW function": https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw, read 2026-10-29). <!-- plan-status: allow --> The Unix limit on one argument was not read.

**C-9.** `DriftCommand.IsSnapshotPath` treats a value that does not end in `.json` and is not an existing file as a live environment, so `bad/emptydir` reached `pac org who`. The new rule is an `else if` after the existing `--env N: no snapshot file at that path` problem in `DriftCommand.Run`, so an entry gets one message at most: a value that holds `/` or `\` (either, on every system), is not an absolute `http` or `https` URI (`Uri.TryCreate` with `UriKind.Absolute` and a scheme check, because on Unix the same call accepts `/tmp/x` as a `file` URI) and is not an existing file is a usage problem: `error: --env <n>: not a snapshot file or an environment; a value with a path separator must be a URL or an existing file`. The value is not printed. Exit 2, and the `live` function is not called. An environment name or unique name that holds a slash would be refused too; none was seen (inferred, Assumptions). The MCP `drift` tool needs no code: its exit 2 already becomes the tool's error lines, and M21's `CheckEnvs` still handles `.json` entries.

**D-15c.** One sentence appended to each of the three descriptions. `drift` and `deploy_plan` take an environment, so they get `Use it only on an environment the user named in the session.` `plan_facts` has no environment input; it reads the one `pac` is signed in to, so it gets `It reads the environment pac is signed in to; use it only when that is an environment the user named in the session.` They repeat the rule for agents in `docs/COMMANDS.md`, which has it for `drift` and `deploy plan` and lacks it for `plan facts` (Step 6 adds it). A test pins that each of the three descriptions ends with its own sentence and no other tool has either, and the golden holds the text.

## Automation

**Files.**

- `src/PowerOps.Cli/DeployCommand.cs`: `DryRun`, `Apply(...)` and `Create` take a trailing `string? approvedHash = null`. After the existing hash comparison in `DryRun`, a second check returns the refusal `Stopped(1, plan.Target, reviewed.Hash, ApplyStatus.Refused, $"the plan is not the one that was approved (approved {approvedHash}, found {plan.Hash}); nothing was imported. Plan again and review the new plan.", Rows(plan, _ => ImportStatus.NotRun))` when `approvedHash is not null && plan.Hash != approvedHash`. The `message` of the report names both hashes (they are hashes of inputs, shown in the form already, not values); its `hash` field stays `reviewed.Hash`, as for the other refusals. `deploy gate` calls `DryRun` without it. M21's per-import re-hash of each zip (`Changed`, `HashRegularFile`) is untouched: it guards between imports, this guards before the first.
- `src/PowerOps.Cli/Program.cs`: `Root` and `Run` get the same trailing `string? approvedHash = null` and pass it to `DeployCommand.Create`. It is a parameter of the in-process entry point and not an option, so no argument list a person or agent writes can supply it.
- `src/PowerOps.Cli/McpCommand.cs`:
  - `Tools` gives `DeployApply` a `cli` that takes the hash as well; for the `Args(true)` run it passes `plan.Hash`, the one the form showed. The other tools pass null.
  - `Check`: the control-character, length and list-cap rules (Data), and `Field` gets a cap per list kind.
  - `Options`: the incoming filter for an unpaired surrogate, with the `resultType` stamp (Data). The `mcp` action is unchanged.
  - Three descriptions get their D-15c sentences.
- `src/PowerOps.Cli/DriftCommand.cs`: the C-9 problem (Data).
- `tests/golden/mcp-tools.txt`: the three descriptions, by `POWEROPS_UPDATE_GOLDEN=1 dotnet test` and the steps of AGENTS.md "Update goldens" (the update run rewrites `selftest.txt` and the skill copies; only the intended lines may change).
- `docs/USE-WITH-AGENTS.md`, `docs/COMMANDS.md`, `docs/BUILD-PLAN.md`: Steps 6 and 7.

**What M24 reuses from M21 and earlier.** `DeployCommand.DryRun`, the one dry run of `deploy apply` and `deploy gate`, with its `Stopped` and `Rows`; `DeployApply`'s form, guard, `Read()` recheck and `Refuse` helpers; `DriftParsers.IsDataverseHost` and `EnvironmentUrl` as the one host rule (S-3 changes nothing there); `McpCommand.Check`'s `entries` loop, labels and `refused` set, and `IsNetworkPath`; `TextReport.Clean` for names in messages; and in `McpCommandTests` the helpers `Connect` (with `onRan`), `SavedPlan`, `ImportFakes`, `ChangedTarget`, `EditPlan`, `ApplyArgs`, `Forms` and `AssertNoApply`, and `DeployTestBase` in the CLI tests.

**Tests.** Each is written first and seen to fail before its code, except the S-8 import test, which is meant to pass on the first run because it pins what the SDK already does.

- S-2, `DeployApplyCommandTests`: `Program.Run` with a different `approvedHash` is refused (exit 1, status `refused`, the message above) and `pac` is not called; with the plan's own hash it imports as before.
- S-2, `McpCommandTests`, at both revisions: the swap. `onRan` fires for the `--apply` call before `Program.Run` starts, which is after the last recheck of the plan file; there the test replaces the zip in the folder with another version of the seed, deletes the plan file and writes a new one for the new folder with `deploy plan --save` against the same fake target. File and re-plan now agree, so before the fix the import runs. After it: status `refused`, `_fakes.Pacs` is 0 and no import was recorded.
- S-5 and S-4, `McpCommandTests`: control characters (NUL, LF forging an `error:` line in a path, CR, TAB, DEL, NEL in a `drift` solution, LF in an `envs` entry, LF in a `target`); 4,097 characters refused and 4,096 not refused by the cap; 51 `paths` and 11 `envs` refused and 50 and 10 not refused by the cap; an emoji passes; and a lone surrogate in a value, a list item and a key, at both revisions, comes back as a tool error and no JSON-RPC error, with `resultType` present at 2026-07-28 and absent at 2025-06-18. These six are raw-pipe tests on the `Raw` helper (`McpCommandTests.cs`, the 2026-07-28 and 2025-06-18 form tests), which gets a `SendRaw(string line)`, because a client library cannot write a lone surrogate and `JsonNode` would not serialize one. Each refusal asserts that nothing reached the fakes.
- C-9, `DriftCommandTests`: `bad/emptydir`, `bad\emptydir`, `./x` and an existing folder are refused with exit 2 and `live` is not called; a URL, and separately an existing file without `.json`, are not refused by the new rule. `McpCommandTests`: the `drift` tool with `bad/emptydir` is a tool error and nothing is read.
- D-15c: one test that each of the three descriptions ends with its own sentence and no other tool's description has either; the golden test holds the full text.
- S-8, `McpCommandTests`, the import test only: `pac` blocks inside the `--apply` import, the test completes `clientToServer.Writer` directly (not by disposing the client, which would cancel the call), the server's run has not ended 500 ms later, and it ends once `pac` is released with the import recorded once (the negative half is a wait, because the point is that nothing happens). `Pair` gets the pipe's writer and its run task for it. It pins what SDK 2.3.0 does; the unanswered-form hang has no test, because it is documented and not fixed.

## Screens

None.

## Environments and route

None. No tenant is read or written and none is needed: S-3 was answered from Microsoft Learn and the S-8 probe used a server of its own. The tests use the fakes of `McpCommandTests` and `DeployTestBase` (a fake target read, a fake `pac`), and this session never runs `pac`, `az`, `powerops plan facts`, `drift` against an environment or any `--apply`.

## Solution and publisher

None.

## Constraints

- AGENTS.md: the smallest change that fully solves each point, tests for new logic, `dotnet format --verify-no-changes` clean, Conventional Commits (`fix:` and `docs:`), no attribution lines, no secrets, tenant URLs or credentials in code, tests or docs, no edit of a version or the changelog, no push or pull request from this session.
- Commands, options, exit codes and JSON schemas are unchanged. The MCP refusals are tool errors, the `drift --env` refusal is exit 2, and the S-2 refusal is exit 1 with status `refused`, as the existing hash-mismatch refusal in `DryRun` is; `docs/schemas/deploy-apply.schema.json` already allows `refused` with a message.
- No new dependency. No change to `PacRunner`, the drift reads or the plan hash.
- Nothing is accepted into the Phase 6 plan's Accepted section by this plan: that plan is signed off and read-only. The residuals it names for M24 (S-3's token, the rest of S-2) and the unanswered-form hang go into `docs/USE-WITH-AGENTS.md` and the verification record, and the master session decides whether the Phase 6 plan gets a new sign-off for them.
- The deploy gate and its hooks (M21) are not changed: `deploy gate` passes no approved hash.
- The rest of S-2, a zip swapped between its last hash and `pac` opening it, is accepted by decision 34 and stays.
- S-4's cancellation point (only `deploy_plan` gets the call's cancellation) is not in the row's fix and is not changed; the walk's cap is R-4.

## Assumptions

- The SDK's behavior in Data is that of `ModelContextProtocol.Core` 2.3.0, which `PowerOps.Cli` references. A new SDK version can change it; the S-8 import test and the surrogate tests are what would notice.
- Inferred: the filter's stamp gives every client the result it expects. Observed only with the probe's raw lines and the SDK's own client, not with Claude Code or Codex.
- Inferred: no real environment name, unique name or friendly name holds `/` or `\`. If one does, the person gives the environment's id or URL, as the refusal says.
- Inferred: refusing `char.IsControl` strings breaks no real input. A path, a solution name, an environment id or URL and a plan file name hold none. A file name with a control character cannot be given to a tool, which is the point.
- Observed in the probe, inferred for the real server: the real `mcp` action behaves like the probe, since both run `McpServer.RunAsync` over `StdioServerTransport`; the tests run the same server over pipes. Nothing here runs the real `powerops mcp` against a client.
- `SIGTERM`, `SIGKILL` and Windows `TerminateProcess` on a running import are not tested; the docs already say a killed server leaves the import running with no report.
- S-3's residual is the cost of keeping the tool inputs free text: a caller who can choose `target` can have the user's `az` sign-in request a token for any documented Dataverse host. The user is asked in the form before an import, but not before a `drift` or `deploy_plan` read; the descriptions (D-15c) and `docs/COMMANDS.md` tell the agent to use only named environments.
- Test counts are given as differences. The suite's current total is read from the run before Step 1.

## Open questions

None open. The three questions of `decisions.md` are answered.

## Questions for your admin

## Steps

Each step ends with `dotnet build --no-incremental` (0 warnings), `dotnet test` and `dotnet format --verify-no-changes`. The test-count change is the number of new test cases (a theory case counts once). Each new refusal test is seen to fail before its code is written (the S-8 test is the exception).

1. `fix(cli):` S-2. The approved hash through `Program.Run`, `DeployCommand.Create`, `Apply` and `DryRun`, the MCP tool passing `plan.Hash`, and the refusal. Tests: the two CLI tests and the swap at both revisions. Check: the swap imports before the fix and is refused after it; `deploy gate` tests unchanged. +4 test cases.
2. `fix(cli):` S-5 and S-4. The control-character, length and list-cap rules in `Check`, and the incoming filter for an unpaired surrogate, with their tests. Check: the lone-surrogate tests get -32603 before the filter and a tool error after it, with `resultType` at 2026-07-28 only. +21 test cases (8 control characters, 2 lengths, 4 list caps, 6 surrogates, 1 emoji).
3. `fix(cli):` C-9. The `--env` refusal in `DriftCommand`, its CLI tests and the MCP test. Check: `live` is never called for a refused value. +7 test cases (4 refused and 2 not refused by the new rule in `DriftCommandTests`, 1 in `McpCommandTests`).
4. `fix(cli):` D-15c. The sentences in the three descriptions, the golden by the steps of "Update goldens", and the test. Check: `git diff tests/golden plugins` shows only the three description lines. +1 test case.
5. `test(cli):` S-8. The import test and the `Pair` change it needs; no code under `src/`. Check: it passes on its first run (it pins the SDK). +1 test case.
6. `docs:` the MCP section of `docs/USE-WITH-AGENTS.md`: the caps and refusals beside "Every path must be absolute", S-3's residual, and the "Timeouts" paragraph's last sentences for a stopped server, which say: if the client closes the server's input, the server finishes the import it is running, then exits without a report; if the client kills the server, `pac` keeps importing and no report comes back (not tested). The same paragraph names the residual: under revision 2025-06-18 a server waiting on an unanswered form keeps running after its input closes, until the client kills it, and a later change could end the form's wait at end of input. `docs/COMMANDS.md`: the `drift` paragraph (a value with a path separator must be a URL or an existing file), the `mcp` paragraph (the caps), and the "plan and skills" section, which lacks the rule for `plan facts` ("Agents: run `plan facts` only when the environment `pac` is signed in to is one the user named in the session."). Check by hand that each sentence matches Data; `DocsDriftTests` must still pass.
7. `docs:` mark M24 built in BUILD-PLAN: the M24 row with a link to this plan, build-order item 49, the Phase 6 Status line, and verification-record lines for S-3 (the answer and its sources) and S-8 (what the import test shows), each with its date. The row's Built bullet ends with "Accepted:" and the unanswered-form hang at 2025-06-18, with one line that a later change could end the form's wait at end of input. This plan's status.

The release label is v0.20.2; versions are not edited here. The commit types are `fix:`, `test:` and `docs:`.
