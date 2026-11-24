plan-hash: 431b7ba645abc4e5b40daedec574467a61a7d32fc9315046dc25d1a6470a733b
model: claude-sonnet-5-5
fresh-context: yes

The plan-hash was updated after the text fixes for R1 to R13 below (the practice of the M7 to M16 reviews). Reviewed hash: d16edf6cbc911958d980de78881a47408ad7049546a5df9a6e4d125b4f351303. The reviewer (powerops-reviewer, Sonnet, fresh context) could not write files, so the planning session wrote this file from its reply and added the resolutions. The fixes changed Goal (done-when), Automation (Files, the `mcp` command, Tools, Descriptions, Input checks, Results, `save`, Concurrency, Tests), Constraints, Assumptions and Steps 3, 4 and 6; no decision in `decisions.md` changed.

The plan-hash was updated again on 2026-10-27 after the master session's review of the build (an independent Sonnet review: 0 High, 2 Medium, 5 Low), which changed Files, Input checks 4 and 5, Results, Tests 4 to 8, Assumptions and Steps: UNC and device paths are refused (R7 below, now fixed), `deploy_plan` honours a cancelled call before it writes `save`, `drift` and `deploy plan` forward text after `  | `, Windows device names are refused in `save`, and four tests now fail when a safety rule breaks. Signed-off hash before that change: 47e8816f4f661bff6d317665f028253b8acf151ec4261e5d414105729709b7db.

While testing, the reviewer ran `powerops drift` once with a live `--env` value, which ran one read-only `pac org who --environment` call; it failed (exit 1). No other tenant call was made, and the scratch folder it made in the worktree was deleted.

## Findings

Most of the 10 checklist items (publisher prefix, connection references, licences, DLP, Dev to Test to Prod, managed or unmanaged, delegation, security roles) do not apply: this is a .NET CLI feature, not a Power Platform app. Only item 9 (testability) and item 10 (open questions) apply. Item 10: `openQuestions` is 0 and "Questions for your admin" is empty. `plan.md` and `decisions.md` hold no URL, GUID, email or person's name. The findings cover spec match, whether each step is buildable and checkable, and what the build would hit.

### High

No findings.

### Medium

#### R1. Refusal tests cannot show that "no command runs"
- Area: Testability (item 9), Tests 5 and 6
- Finding: Fakes that throw are caught by `SettingsCommand.Guard` (exit 3) or by the SDK (a generic tool error), so the call is `isError: true` whether or not the command ran. `deploy plan` has no `--apply` option, so a response file holding only `--apply` fails to parse either way. The case that matters, a response file holding `--out <path>` or `--save <path>`, which would overwrite a file, is not tested.
- Suggested fix: Count the fakes' calls and assert zero after each refusal; assert the result is the server's own `error: <field>: <problem>` lines; put `--out` and `--save` with temp paths in the response file and assert the paths are not created; add a positive control.
- Resolution: fixed (Tests 5: counting fakes asserted zero, the exact server lines, `@<file>` as `target` and as `folder` with `--save <temp path>`, `--out <temp path>` and `--apply` and both temp paths checked, a positive control that calls the fakes once)

#### R2. `/h` and `/?` positional values become the help option
- Area: Input checks
- Finding: `powerops analyze /h`, `diff /? /x` and `settings generate /?` print help and exit 0 (verified with the built tool). Both values pass the absolute-path check on Linux and macOS, so a tool would return help text as `isError: false`.
- Suggested fix: Put options first, then `--`, then the positional values (after checking that `--` makes them plain arguments), or refuse the help aliases; add `/h` and `/?` to the tests.
- Resolution: fixed (Tools: every argument list is the subcommand, the options, `--`, then the positional values; checked with the built tool on 2026-10-27 that `/h`, `/?`, `-?` and `@file` after `--` are plain values and that option values are values either way; the table's Runs column shows the order; Goal and Tests 4 add `/h` and `/?`)

#### R3. An unknown tool name is the handler's job, not the SDK's
- Area: Results, Test 5
- Finding: With a hand-written `CallToolHandler` and no `McpServerTool` collection, every name reaches the handler. It must throw `McpProtocolException` with `McpErrorCode.InvalidParams`; a plain exception becomes a generic `isError: true` result. Null `Params` needs the same.
- Suggested fix: Say so in the plan; have the test assert a JSON-RPC error, not an `isError` result.
- Resolution: fixed (Results names the throw for an unknown name and for a call with no params; the planning session had seen the same in a scratch project; Tests 5 asserts `McpProtocolException` on the client)

#### R4. The stderr block can pass pac and az text to the model
- Area: Results, Constraints
- Finding: The spec returns "powerops' own `error:` lines"; the plan returned all of stderr. On a failed live read, `drift` and `deploy plan` forward pac, az or Dataverse text, which may repeat the `--env` value, the org URL or an account name.
- Suggested fix: Keep only the `error:` lines; test with a fake that throws `DriftReadException` with forwarded text.
- Resolution: fixed (Results keeps only `error:` lines and says why, the `POWEROPS_DEBUG` stack trace included; Tests 7 covers the mapping and a `drift` call with forwarded text)

#### R5. The descriptions rule contradicts itself
- Area: Descriptions
- Finding: The plan says three descriptions name pac and az, and also that no description names a Microsoft tool (decision 16(b)), which is about the tools of Microsoft's MCP servers.
- Suggested fix: Pick one: drop pac and az, or state that 16(b) means the MCP servers' tool names only.
- Resolution: fixed (Descriptions states that 16(b) is about the tools of Microsoft's MCP servers and that naming the CLIs the command runs, which tells an agent what must be signed in, is not that)

#### R6. Blocking the transport and the concurrency claim are not tested
- Area: Concurrency, Assumptions
- Finding: Commands are synchronous and can run for tens of seconds. If the SDK ran the handler inline on its read loop, one slow call would block `ping`, `tools/list` and other calls. The SDK docs don't say, and the spike didn't test it.
- Suggested fix: Run the command with `Task.Run` in the handler, and test that `tools/list` and a second call answer while a slow call runs.
- Resolution: fixed (Concurrency records the scratch-project run of 2026-10-27, where two calls to a handler that blocked for 500 ms started together, so no `Task.Run` is added; Tests 8 holds a `deploy_plan` call in a blocking fake target while `tools/list` and `rules` answer)

### Low

#### R7. Windows UNC and device paths pass the absolute-path check
- Area: Input checks, security
- Finding: `Path.IsPathFullyQualified` accepts `\\host\share\x` and `\\?\...` on Windows; a steered agent could make the server open a network path.
- Suggested fix: On Windows, refuse a path that starts with `\\` or `//`; test it as a pure function.
- Resolution: fixed (first accepted, then fixed after the master session's review rated it Medium: a read-only tool may run with no prompt, so the agent's own shell access does not cover it. Input checks 4 refuses any path value and any `envs` entry that starts with `\\` or `//`, on every OS, tested as a function and through the server)

#### R8. Empty, whitespace and `null` values are not defined
- Area: Input checks 2 and 3
- Finding: `envs: ["", ""]` passes and runs `pac org who --environment ""`; `null` for an optional field is undefined.
- Suggested fix: Refuse empty or white-space strings; define `null` for optional fields and test it.
- Resolution: fixed (Input checks 2 and 3: an empty or white-space string is refused; `null` is absent for an optional field and missing for a required one; Tests 5 covers both)

#### R9. Results with no text, or exit 1 without output
- Area: Results
- Finding: A tool error could have no content; System.CommandLine's default handler turns an uncaught exception into exit 1 with a stack trace, which the plan would return as `isError: false`.
- Suggested fix: Add `error: the command failed (exit code N)` when there is no text; treat exit 1 with an empty stdout as an error; test it.
- Resolution: fixed (Results: exit 1 with an empty stdout and any code other than 0 to 3 are errors, and a tool error with no `error:` line gets that line; the mapping is an `internal static` function tested directly, Tests 7)

#### R10. The packed-tool test is weaker than its claim
- Area: Test 7 (now 9)
- Finding: No `tools/call`, so it can't show that a command run leaves stdout clean; a blocking `ReadLine` can hang; Windows stdin may add a BOM.
- Suggested fix: Add a `tools/call` of `rules`, read with a timeout, set `StandardInputEncoding` to UTF-8 without BOM.
- Resolution: fixed (Tests 9 and Goal)

#### R11. A failed `save` write can leave a partial file
- Area: `deploy_plan`'s `save`
- Finding: A write that fails after `CreateNew` created the file leaves a partial plan file; the error line echoes the name.
- Suggested fix: Delete the file this call created on a failed write; test it.
- Resolution: fixed in the plan text (the server deletes only the file this call created, and the error line no longer names the file); no test, because a write that fails after the file is created (a full disk) cannot be made portably in a unit test

#### R12. Dependency and spike follow-ups
- Area: Constraints, Step 1
- Finding: The SDK's licence is in an MIT-to-Apache transition and gets no Dependabot rule; the Codex tool call is not verified (Q5).
- Suggested fix: Say that Dependabot updates of the SDK get a licence check; name the Codex call as unverified in the verification record if it is.
- Resolution: fixed (Constraints and Step 6)

#### R13. How the `mcp` action blocks is not stated
- Area: the `mcp` command, AGENTS.md recipe
- Finding: Async or sync over async, the cancellation token, and why the recipe's `PowerOps.Core/Output` step is skipped are not stated.
- Suggested fix: State that `SetAction` is async with `RunAsync(cancellationToken)`, and why the MCP code stays in Cli.
- Resolution: fixed (the `mcp` command paragraph and Files)
