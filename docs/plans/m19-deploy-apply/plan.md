---
status: signed-off
review: done
signed-off: 2026-10-28
signed-off-hash: c13657d570c799524f1b78838602dd8bd41212f97d657c71f70b90cf2e37c66a
environment: null
depth: quick
supersedes: null
---

# M19: `deploy_apply` behind the user's yes

## Goal

Build BUILD-PLAN milestone M19 (build-order item 44, release label v0.18.0): the MCP tool `deploy_apply`. Without `apply` it runs `deploy apply` as a dry run. With `apply: true` it imports only after an elicitation form in which the user types the plan's target name. The spec is the BUILD-PLAN section "Phase 5: agents (M18–M20)": the M19 row, the tools table's `deploy_apply` row, "Results and errors", the research basis and decision 26. This plan settles four things the row leaves open: whether each client shows the form to the user, the form's schema and message, how the server knows that the client can elicit, and the guard that allows one import at a time.

Done when:

- `tools/list` serves 12 tools, `deploy_apply` last, and equals the golden `tests/golden/mcp-tools.txt`. `deploy_apply` has `readOnlyHint: false`, `destructiveHint: true`, `idempotentHint: false` and `openWorldHint: true`. Both `deploy_plan` and `deploy_apply` say that a reviewed plan is promoted with `deploy_apply`, not with an import tool.
- The offline tests listed under Automation pass. They use a fake `pac`, a fake target read and the SDK's in-process client, at revision 2026-07-28 (Claude Code's) and 2025-06-18 (Codex's), with one client that declares form elicitation and one that doesn't. No path passes `--apply` without an ordinal `accept` that carries exactly the plan's target name, and the tests see every argument list the server runs.
- AGENTS.md's `--apply` rule, the README's agent line under `deploy apply` and its `mcp` paragraph name `deploy_apply` with `apply: true`.
- `dotnet build --no-incremental` (0 warnings), `dotnet test`, `dotnet format --verify-no-changes`, `dotnet pack src/PowerOps.Cli -c Release -o artifacts` and `powerops plan status .` are clean. No tenant is contacted, and nothing is imported.

## Users

- Agents in Claude Code and Codex, which call the tool after the user said yes to the environment in chat.
- The person at the keyboard, who types the target's name into the client's form. The form is the gate (decision 26).

## Data

- **Input**: `planFile` (an absolute path), `target` (an environment id or URL, as for the CLI), and `apply` (a boolean, optional, false when absent or `null`).
- **Plan file**: read with `DeployPlanFile.Read`, under the same checks the command makes (no symbolic link, at most 1 MB). The form shows `target` (the friendly name `pac org who` printed when the plan was made), `hash`, and the zip names from `inputs.zips`. The BUILD-PLAN row says "its solutions". The zip names are covered by the hash, which `deploy apply` checks against its new plan, so they are what can be imported. The file's `solutions` array is not covered by the hash, so the form doesn't show it, and step 5 of Steps amends the row to say so. Zip names are plain file names (`DeployPlanFile` refuses others). The hash doesn't cover the `target` text either, so before the form the tool runs the dry run, whose report carries the live friendly name of the environment the `target` input names (call order step 6). A string in the file that is not valid Unicode (a lone surrogate) makes `DeployPlanFile.Read` refuse the file as not a plan file, for the CLI and the tool alike.
- **Output**: one `CallToolResult`, as in M18: the deploy-apply JSON (`docs/schemas/deploy-apply.schema.json`) when the command ran, then powerops' own `error:` lines. A refusal from the server is `isError: true` with its `error:` lines only.
- **Never shown**: the `target` input, the plan file's path or name, the text typed in the form, and `pac`'s own text (the command forwards it to stderr after `  | `, and the server drops it).

## Automation

**Files.**

- `src/PowerOps.Cli/McpCommand.cs`. M18's code assumes string fields and a synchronous call, so these change:
  - a `Kind.Flag` field: `Check` stores a JSON `true` or `false` as a `bool` before the string checks, and those checks skip it; `Describe` writes `{"type": "boolean"}`; `Values` gets `Flag(name)`;
  - `ToolDef` gets an optional asynchronous run, `Func<Values, McpServer, CancellationToken, ValueTask<CallToolResult>>`, which `Call` awaits instead of `Run`. The handler becomes `(context, ct) => Call(tools, context, ct)`, so `deploy_apply` gets `context.Server`, the request's own server;
  - a result helper that marks a command's result `isError: true` and appends a line (steps 4 and 6 of the call order);
  - `Options` gets an optional `Action<string[]>? ran = null`, called with each argument list a tool passes to `Program.Run`. It is a test seam: production passes nothing;
  - the `deploy_apply` tool, its guard, and the `deploy_plan` description change.
- `src/PowerOps.Core/Deploy/DeployPlanFile.cs`: `Read` turns the `InvalidOperationException` that `GetString` throws for a lone surrogate into its `InvalidDataException` ("not a plan file: ..."), with a test in `DeployPlanFileTests`.
- `src/PowerOps.Cli/DeployCommand.cs`: the plan file read (the symbolic-link check, the 1 MB cap and `DeployPlanFile.Read`, with their `error:` lines) moves into one `internal static` method that `deploy apply` and the tool both call, so both read the file under the same rules.
- `tests/PowerOps.Cli.Tests/McpCommandTests.cs`: a helper that builds a second server and client pair from the same `Fakes` with given `McpClientOptions`, disposed in the test (the fixture's client declares no elicitation, and capabilities are fixed at connect); `ToolNames`, test 1's annotation `switch` and M18 test 8's tool count gain `deploy_apply`. `tests/golden/mcp-tools.txt`.
- `AGENTS.md`, `README.md`, and then `docs/BUILD-PLAN.md`, `docs/FOUNDATION.md` and this plan's status, for the record.

**Elicitation in the clients (settled before this plan; `decisions.md` Q1 and Log).** Both clients show a form to the user in an interactive session. Headless runs cancel it, and no automatic path types the name, except a Claude Code `Elicitation` hook that the user wrote.

- Claude Code 2.1.303 uses revision 2026-07-28 and declares `elicitation: {form: {}, url: {}}`. In an interactive session its docs say it shows a dialog with the form's fields. In `claude -p` the form came back `cancel` 15 ms after the call, and the model saw only the tool's text.
- Codex 0.163.0 uses revision 2025-06-18 and declares `elicitation: {form: {}, url: {}}`. Its code at `rust-v0.163.0` sends the form to the TUI, which shows a string property as a text field. It declines under the `never` approval policy and auto-cancels in `codex exec`. Its full-access auto-accept takes only a form with no properties, with `content: {}`, and its reviewer's accept is `content: {}` as well, so neither carries a name.

So `apply` stays. Decision 26's fallback (a dry run only) is not needed.

**Capability.** The server reads `context.Server.ClientCapabilities?.Elicitation?.Form` on the request's own server: for revision 2026-07-28 the SDK sets the capabilities per request, and for the earlier revisions from `initialize`. The SDK reads a bare `elicitation: {}` as form mode. When `Form` is null, the call is refused before the plan file is read or a form is sent.

**The form.** `ElicitAsync` is called on `context.Server`, with the call's cancellation token and no `_meta` (Codex routes forms with approval keys in `_meta` differently). At 2026-07-28 the SDK returns the form as an input-required result and resumes the handler on the client's retry; the suspended handler is cancelled when the server is disposed. At earlier revisions it sends `elicitation/create`. Tests run both paths, and a raw-line test checks that 2026-07-28 takes the input-required path.

- Mode `form`. Message (the target passes through `TextReport.Clean`, the zip names are plain file names):

  ```
  powerops deploy_apply wants to import a reviewed plan into an environment. There is no rollback.

  Target environment: <plan target>
  Plan hash: <hash>
  Solution zips: <zip 1>, <zip 2>, ...

  To import, type the target environment's name exactly as shown above. Decline or cancel to import nothing.
  ```

- Schema: `{"type": "object", "properties": {"environment": {"type": "string", "title": "Environment name", "description": "Type the target environment's name exactly as shown above."}}, "required": ["environment"]}`. The field has no `default`: the SDK's `ElicitResult.WithDefaults` fills a missing accepted field from its default, so with a default, `content: {}` would pass. No `minLength` or `format`, so every client can render it (Codex's TUI turns a form it cannot parse into a bare approval prompt).
- It passes only when the action is exactly `accept` (ordinal; not the SDK's `IsAccepted`, which ignores case) and `content.environment` is a JSON string equal (ordinal, no trimming) to the plan file's `target`. `deploy apply` itself still checks that name against the environment (exit 2 when it differs).

**Call order with `apply: true`.** The dry-run argument list is built without `--apply`, and `--apply` is appended in step 10 only.

1. M18's input checks, plus `apply`: a JSON `true` or `false`, else `error: apply: must be true or false`.
2. An import running in this server (`CurrentCount == 0`): refused.
3. No form capability: refused.
4. The plan file is read. If it is unreadable, missing, from another version or blocked, no form is sent: the tool runs the dry run, which reports why (none of these cases reads the tenant), and returns its result as `isError: true` with its line appended.
5. The plan's `target` must be one a user can read and type: at most 200 characters, no control, format, line-separator, paragraph-separator or private-use characters (Unicode `Cc`, `Cf`, `Zl`, `Zp` and `Co`), no leading or trailing white space. Else refused, with no form.
6. The dry run, `deploy apply --target <target> --format json -- <planFile>`. The call goes on only when it exits 0 with status `planned` and its report's `target` and `hash` equal the plan's from step 4. Else the tool returns the dry run's result as `isError: true` with its line, and no form is sent. The report's `target` is the live friendly name of the environment the `target` input names (`deploy apply` exits 2 when it differs from the file's), so the name the user types is that environment's. A later change of only the file's `target` text can't redirect the import: `deploy apply` refuses unless the name still matches the live one. And the user is never asked to confirm an import that the CLI would refuse.
7. The form. If `ElicitAsync` throws (the client returns an error, the call is cancelled, the retry has no answer, or the connection closes), the call is refused.
8. A `decline`, a `cancel` or any other action, a missing or non-string `environment`, or a name that differs: refused.
9. The call's cancellation token has fired: refused. On the 2025-06-18 path a cancel already ends the form wait (step 7), so this is defence in depth for a cancel of the 2026-07-28 retry.
10. The plan file is read again; if its `hash` or `target` differs from the form's, it is refused. Then `if (!guard.Wait(0))` refuses, else `try { deploy apply --target <target> --format json --apply -- <planFile> } finally { guard.Release(); }`, so a call releases only a guard it took.

**Refusal lines.** Each server refusal is the whole result, `isError: true`, and runs nothing; steps 4 and 6 return the dry run's result with their line after it.

- Steps 2 and 10 (guard): `error: apply: an import is already running in this server; nothing was imported`
- Step 3: `error: apply: this client cannot show the confirmation form; nothing was imported`
- Step 4: `error: apply: the plan file cannot be applied; nothing was imported`
- Step 5: `error: planFile: the plan's target name cannot be shown in the form (too long, control or format characters, or white space at either end); nothing was imported`
- Step 6: `error: apply: the dry run did not pass, so no form was sent; nothing was imported`
- Step 7: `error: apply: the confirmation form failed; nothing was imported`
- Step 8: `error: apply: the confirmation form was declined; nothing was imported`, `error: apply: the confirmation form was cancelled; nothing was imported` (also for any other action), or `error: apply: the name typed in the form is not the plan's target; nothing was imported`
- Step 9: `error: apply: the call was cancelled; nothing was imported`
- Step 10 (plan file): `error: planFile: the plan file changed while the form was open; nothing was imported`

Steps 3, 5, 7 and 8 add a second line, with placeholders only: `error: apply: to import without the form, the user runs: powerops deploy apply <plan file> --target <environment id or URL> --apply`.

**One import at a time.** One `SemaphoreSlim(1, 1)` per server, created in `Options`. A stdio client starts one server process, so this is one import at a time per client session. Step 2 refuses early, so no form is shown while an import runs, and step 10's `Wait(0)` refuses a second accepted call. The guard covers the import only, not the form. At 2026-07-28 a handler that waits on a form is suspended until the client retries, with no timeout in SDK 2.3.0 (it ends with the session). A guard held across the form could stay taken for the rest of the session. Two forms may be open at once, but only one import runs, and a retry of the refused one plans again: the hash check refuses it once the first import has changed the target. A cancelled or timed-out call doesn't stop an import that has started: the command takes no cancellation, and the guard stays taken until the import ends.

**Dry run.** No `apply`, `apply: false` or `apply: null`: `deploy apply --target <target> --format json -- <planFile>`, and M18's result mapping. It is allowed while an import runs.

**Descriptions.**

- `deploy_apply` (two sentences, as M18's row asks): "Promote a reviewed plan file from deploy_plan's `save` with this tool, not with an import tool: it plans again against the target and returns the JSON of `powerops deploy apply --format json` (docs/schemas/deploy-apply.schema.json), as a dry run that imports nothing unless `apply` is true. With `apply: true` it imports the zips in plan order through pac only after the user types the target's name into the server's confirmation form, and refuses when anything changed since the plan."
- `deploy_plan`: its last clause becomes "promote a reviewed plan with deploy_apply, not with an import tool."
- Fields: `planFile` "Absolute path of a plan file saved by deploy_plan's save; the zips and settings files are read from its folder."; `target` "The environment the plan was made for: an id or URL (as pac --environment)."; `apply` "Optional, default false: true imports after the user types the target's name into the confirmation form; false is a dry run."

**Tests** (`McpCommandTests`). Each elicitation test builds its own server and client from the same `Fakes`, with `McpClientOptions`: an `ElicitationHandler`, which records each request and answers as the test says and so declares form mode, and `ProtocolVersion` 2026-07-28 or 2025-06-18. Each plan file is written by `deploy plan --save` with the fakes, beside the seed zip and its settings file. Every test records the `ran` argument lists.

1. `tools/list` equals the golden. `deploy_apply` is last, with its four annotations, and `deploy_plan`'s description names `deploy_apply`.
2. Dry run (no `apply`, `false` and `null`): the text equals the CLI's stdout for the same arguments and validates against `deploy-apply.schema.json`. `pac` is never called, no form is sent (even from the client that can elicit), and no argument list holds `--apply`.
3. `apply: true` from the fixture's client, which declares no elicitation, and from a client that declares only `elicitation: {url: {}}`: the step 3 lines, no form, no `pac` call, no target read, no argument list run.
4. A theory over both revisions: a decline, a cancel, an action `Accept` (wrong case), a wrong name (`contoso test`, `Contoso Test ` and an empty string), an accept with no content, an accept with `{}`, and an accept with a number. Each gives its step 8 lines and `isError: true`, with no `pac` call and no argument list that holds `--apply`. At 2025-06-18, a client whose handler throws gives the step 7 lines.
5. A theory over both revisions: an accept with the exact name runs the imports in plan order with `--settings-file`, as the fake `pac` records. The result equals the CLI's `--apply` JSON for the same fakes and validates against the schema. Exactly one argument list holds `--apply`. The recorded form has one required string property `environment` with no `default` and no `_meta`, and its message holds the target name, the hash and the zip name, but not the `target` input, the plan file's name or its folder.
6. Before the form: a target that changed since the plan (the fake target returns another snapshot) and a live friendly name that differs from the plan's each fail the step 6 dry run, so no form is sent, and the result is `isError: true` with the dry run's report or `error:` line and the step 6 line, with no `pac` call. While the form is open (the handler changes the fake target): another snapshot gives status `refused` after the yes (exit 1, `isError: false`), and another live name gives `isError: true` (exit 2), each with no `pac` call.
7. A failed import (the fake `pac` fails): `isError: true`, with the report whose status is `failed`.
8. The guard, at both steps. Two `apply` calls whose forms the handler holds open. The first form is accepted, and its import blocks in the fake `pac`. Then the second form is accepted, and the call gets the guard line from step 10. A third `apply` gets the line from step 2 without a form, and a dry run answers. After the release, the first call finishes, and a fourth `apply` imports, so the guard was released once, by the call that took it.
9. The plan file is rewritten while the form is open (the handler changes its hash, or only its `target` text, then accepts the right name): the step 10 plan-file line, and no `pac` call.
10. With `apply: true`, a blocked plan file, a plan file from another version, a malformed one, a symbolic link to a plan file (skipped where the OS refuses to make one) and a plan file with a lone surrogate in a string: no form, `isError: true`, the dry run's report or `error:` line, then the step 4 line, and no argument list holds `--apply`. A plan file whose `target` has a zero-width character, a trailing space, 201 characters, U+2028, U+2029 or a private-use character: the step 5 lines, no form, and no argument list run.
11. Raw JSON-RPC lines, as in M18's test 8, since the SDK's client sends no cancellation and resolves forms inside `CallToolAsync`. (a) At 2026-07-28: the first `tools/call` response is an input-required result whose input request is `elicitation/create`, and a retry with its `requestState` but no `inputResponses` gives the step 7 lines. (b) At 2025-06-18: the server sends `elicitation/create`, the test sends `notifications/cancelled` for the call and then answers the form with an accept and the right name, and `pac` is never called.
12. Input refusals, as M18's: `planFile` relative, starting with `-`, `@<file>` (holding `--apply`), or `\\host\share\x`; `target` `@<file>`; `apply` `"true"` (a string); an unknown field. Each gives exactly the server's `error:` lines, no fake is called, and no argument list runs.

## Screens

None. The form is the client's own dialog.

## Environments and route

- The build and every test: offline, with fakes. No tenant, `pac` or `az` call, and nothing is imported (decision 28).
- The elicitation check ran outside the repo on 2026-10-28, with per-run config only: `claude -p --mcp-config <file> --strict-mcp-config --no-session-persistence`, and `codex exec` with a throwaway `CODEX_HOME` and no login (deleted afterwards). The user's own config was not changed. An interactive check in each client needs the user, so this session asks them (Q5). M20's manual run checks the real server's form in both clients either way.

## Solution and publisher

None.

## Constraints

- No new package: `ModelContextProtocol.Core` 2.3.0's `ElicitAsync`, `ClientCapabilities` and `McpClientOptions` cover the tool and the tests. Licensing as in M18 (Apache-2.0 and MIT).
- AGENTS.md: never pass `--apply` without the user's yes to that environment in this session. Neither this session nor a test runs a live import.
- No version or changelog edits; no push or pull request from this session. Conventional Commits, no attribution lines.
- Deadline: M20's manual run needs Test, which expires about 2026-11-21.

## Assumptions

- The SDK keeps resuming a suspended handler on the client's retry at 2026-07-28 and sending `elicitation/create` at earlier revisions (tests 4, 5 and 11 run both).
- The form confirms an environment by its friendly name. If two environments share a name, it cannot tell them apart, and the `target` input (an id or URL) is not shown. The hash binds the plan to the environment id the agent's `target` resolves to, and the plan was reviewed against it.
- The form binds the environment and the plan file as they are at that moment. The step 6 dry run binds the typed name to the live name of the environment the `target` input names, so changing only the file's `target` text can't redirect the import. Anyone who can write the deploy folder can still change the plan file and its zips together outside the window step 10 and the command's read leave; such a writer is outside this tool's threat model.
- Claude Code's idle timeout for a stdio call is 30 minutes with no response or progress (its docs). The default Codex tool timeout is 60 s. Codex's code says an open form pauses tool-result delivery (`core/src/elicitation.rs`), but whether its timeout stops while the user types is not verified. A long form or a long import can outlast either client's limit. The client then reports an error, a cancelled form imports nothing, and an import that has started finishes while the guard keeps a second one out. M20 checks the timeouts and says in the README how to raise them, for the form and the import.
- When the client closes stdin during an import, the SDK waits for in-flight handlers before the server exits (read in its session handler, not run); a client that kills the server process leaves `pac` running with no report. M20's README says so.
- A user can read the 64-character hash in the client's dialog. The deploy plan text and JSON show the same hash.

## Open questions

## Questions for your admin

## Steps

1. Elicitation check, outside the repo: a toy Core-only server that elicits, `claude -p` and `codex exec` (listing only) with per-run config, and Codex's code at `rust-v0.163.0`. Done on 2026-10-28 (`decisions.md`, Log). The interactive checks wait on Q5.
2. `docs:` this plan, its decisions and review, signed off.
3. `feat(cli):` item 44: the plan file read shared with `DeployCommand`, the `deploy_apply` tool (the flag field, the asynchronous run, the `ran` seam, the capability check, the form, the guard), the `deploy_plan` description, tests 1 to 12, the `mcp-tools.txt` golden, and the AGENTS.md and README lines.
4. Run `dotnet build --no-incremental` (0 warnings), `dotnet test`, `dotnet format --verify-no-changes`, `dotnet pack src/PowerOps.Cli -c Release -o artifacts` and `powerops plan status .`; update goldens with the recipe and read the golden diff.
5. `docs:` BUILD-PLAN: the M19 row marked built with a link to this plan, and amended to say the form lists the zip names (covered by the hash) rather than the file's `solutions`; item 44; the M19 line of the verification record (the check's findings and what is not verified). FOUNDATION §8's elicitation item: closed if Q5's interactive checks ran, else marked settled for the headless paths and the code, with the interactive form left to M20's manual run. This plan's status.
6. `fix(cli):` the master session's review of 2026-10-28: the step 6 dry run before the form, the lone-surrogate refusal in `DeployPlanFile.Read`, `Zl`, `Zp` and `Co` in step 5, the two-sentence description, and tests 3, 4, 6, 9 and 10 as amended.
7. `docs:` the fix in the M19 row and the verification record, and FOUNDATION §8's M18 item ticked (M18's spike settled it).
