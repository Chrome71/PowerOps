plan-hash: c13657d570c799524f1b78838602dd8bd41212f97d657c71f70b90cf2e37c66a
model: claude-sonnet-5-5
fresh-context: yes

The plan-hash was updated after the text fixes for R1 to R11 below (the practice of the M7 to M18 reviews). Reviewed hash: f31b6dd11306a28c81e45aaa84b345e1985076ef37435b6965f116a238845c5f. The reviewer (powerops-reviewer, Sonnet, fresh context) could not write files, so the planning session wrote this file from its reply and added the resolutions. The fixes changed Goal (done-when), Data, Automation (Files, the elicitation summary, the form, the call order, the refusal lines, the guard, the tests), Assumptions and Steps 3 and 5; Q2's form message lost the plan file's name (R6), logged in `decisions.md`. The reviewer contacted no tenant.

The plan-hash was updated again on 2026-10-28 after the master session's review of the build (an independent Sonnet review: 0 High, 1 Medium, 5 Low), which changed Data, Files, the call order (a new step 6, the dry run before the form, so the typed name is the live name of the environment the `target` input names; the plan's hash doesn't cover the file's `target` text), step 5's characters, the refusal lines, the description (two sentences), Assumptions, the tests and Steps. One Low (a client or hook that answers the form automatically) was accepted as decision 26's design. Signed-off hash before that change: 62b2cd27a96c62a76e059d731a9f11ac671496594651ebffc2ae859b0c103b29.

Most of the Power Platform checklist (items 1 to 8) does not apply: the plan is a .NET CLI/MCP feature, has no solution, publisher, connector, DLP, environment-variable or security-role content, and `environment` is null. Items 9 and 10 apply and drive the findings. Open questions: 0. No URL, GUID, email or person's name in the plan or `decisions.md`.

The reviewer checked against the code and found right: per-request `ClientCapabilities` at 2026-07-28 and session-wide before it; `{}` read as form mode; `WithDefaults` filling only fields with a `default`; `ElicitAsync` on `context.Server` taking the input-required path at 2026-07-28 and `elicitation/create` before it; `McpClientOptions.ProtocolVersion` and an `ElicitationHandler` that declares form mode; and Codex's empty-schema auto-accept, text field and reviewer behaviour at `rust-v0.163.0`. No path reaches `--apply` without a matching `accept` if the code follows the plan's order.

## Findings

### High

No findings.

### Medium

#### R1. No test can see `--apply` leaking into a path that has no form
- Area: 9 (testability)
- Finding: For a blocked plan, a plan from another version or a missing file, `deploy apply` returns before it calls `pac` (`DeployCommand.cs:204-212`), so "no `pac` call" cannot show that the step 4 dry run was built without `--apply`. In production the same mistake would import without a form if the file became valid between the tool's read and the command's.
- Suggested fix: Build the dry-run and import argument lists apart, append `--apply` only in step 9, add a test seam that sees every argument list, and assert on it.
- Resolution: fixed (call order: the dry-run list is built without `--apply`, which step 9 alone appends; Files: `Options` gets an optional `ran` seam called with each argument list; tests 2, 3, 4, 10 and 12 assert that no list holds `--apply` or that none ran, test 5 that exactly one holds it)

#### R2. Test 4's throwing handler cannot be written at 2026-07-28 with the SDK client
- Area: 9 (testability)
- Finding: At 2026-07-28 the SDK client resolves the form inside `CallToolAsync` (`McpClientImpl.cs:187-222`, `718-721`), so a throwing handler throws on the client and no retry is sent. No test shows that the 2026-07-28 run took the input-required path at all.
- Suggested fix: Restrict the throwing-handler row to 2025-06-18; add a raw-line test at 2026-07-28 that sees the input-required result and sends a retry without `inputResponses`; say that the suspended handler is cancelled when the server is disposed.
- Resolution: fixed (test 4: the throwing handler at 2025-06-18 only; test 11(a): the raw-line 2026-07-28 test; the form paragraph says the suspended handler is cancelled on dispose)

#### R3. The plan does not name what M18's plumbing needs for a boolean and an async tool
- Area: 9 (build steps checkable)
- Finding: `Check`, the `entries` pass, `Describe` and `Values` assume strings (`McpCommand.cs:34-40`, `317`, `380-390`, `405-407`); `ToolDef.Run` is synchronous and gets no `McpServer`; the handler wraps `Call` in `ValueTask.FromResult` (`McpCommand.cs:97`); step 4 needs `isError: true` over an exit-1 report; the test fixture's client is fixed at connect, and `ToolNames`, test 1's `switch` and M18 test 8's count change.
- Suggested fix: List these in Files.
- Resolution: fixed (Files lists the `Kind.Flag` changes to `Check`, `Describe` and `Values`, the asynchronous run with `context.Server`, the result helper, the `ran` seam, and the test helper and fixture changes)

#### R4. The guard has an uncovered failure mode at step 9
- Area: 9 (testability)
- Finding: The plan does not say that `Release` runs only after a `Wait(0)` that returned true; a release after a failed wait would free the other call's hold and let a third import start. No test covers the lost `Wait(0)`, and its line is not named.
- Suggested fix: Specify `if (!guard.Wait(0)) refuse; try { … } finally { guard.Release(); }`, and test two accepted forms with a third call during the import.
- Resolution: fixed (step 9 spells out the pattern; the refusal lines name the guard line for steps 2 and 9; test 8 holds two forms open, accepts both, checks the step 9 refusal, a third call's step 2 refusal and a fourth call's import after the release)

### Low

#### R5. The gate binds the environment by friendly name only
- Area: 9
- Finding: Two environments with the same friendly name look the same in the form, and the user never sees the `target` input.
- Suggested fix: Record it as a known limit (and in M20's README).
- Resolution: accepted (Assumptions: the form confirms a friendly name; the hash binds the plan to the environment id behind the agent's `target`, and the user reviewed that plan. The spec keeps the `target` input out of the form, and the plan file holds no id to show)

#### R6. The form's text comes from untrusted input and is only partly cleaned
- Area: 9
- Finding: `TextReport.Clean` replaces only control characters (`TextReport.cs:38`), so a `target` with a newline, white space at either end, format characters or great length can't be typed back, and the plan file's name is the agent's choice.
- Suggested fix: Refuse before the form a target over a length cap, with control or format characters or white space at either end; cap the file name; test it.
- Resolution: fixed (call order step 5 and its refusal line: at most 200 characters, no `Cc` or `Cf` characters, no white space at either end; the plan file's name is no longer in the form, so it needs no cap; test 10 covers a zero-width character, a trailing space and 201 characters)

#### R7. The form confirms the environment, not the contents; the TOCTOU window is not stated
- Area: 9
- Finding: A writer to the deploy folder can change the plan file and its zips together, and the command's hash check passes against the new pair.
- Suggested fix: State that a writer to the deploy folder is outside the threat model.
- Resolution: fixed (Assumptions)

#### R8. Step 7 and test 11 do not test what they claim
- Area: 9
- Finding: On the 2025-06-18 path a cancel ends `ElicitAsync` at once (`McpServer.Methods.cs:361-367`), so the cancellation-token check never runs there; the plan says nothing about a client that exits during an import.
- Suggested fix: Call the check defence in depth, or test it at 2026-07-28; describe the client-exit behaviour.
- Resolution: fixed (call order step 8 is defence in depth for a cancel of the 2026-07-28 retry; test 11(b) claims only that nothing is imported after a cancel; Assumptions describe a closed stdin and a killed process, for M20's README)

#### R9. Record two deviations from the spec, and do not close the FOUNDATION item early
- Area: 10
- Finding: The M19 row says the form names the "solutions"; the plan shows zip names. Step 5 closed FOUNDATION §8's item while the interactive checks are pending.
- Suggested fix: Amend the M19 row; close the FOUNDATION item only after Q5's checks.
- Resolution: fixed (Data and Step 5: the M19 row is amended; FOUNDATION's item is closed only if Q5's interactive checks run)

#### R10. Codex's 60 s tool timeout may also cover the form
- Area: 9
- Finding: If Codex doesn't pause its timer while a form is open, the user has about 60 s to type the name.
- Suggested fix: State it as unverified and give M20 the check for the form wait too.
- Resolution: fixed (Assumptions)

#### R11. Small wording gaps
- Area: 9
- Finding: (a) No refusal line for step 4 or the lost guard; (b) "case N" mixed step and test numbers; (c) the SDK's `IsAccepted` ignores case (`ElicitResult.cs:43`); (d) `_meta` not mentioned.
- Suggested fix: Add the lines, use "step" and "test" throughout, say ordinal `accept` and no `_meta`.
- Resolution: fixed (refusal lines per step; "step" and "test" throughout; the form says an ordinal `accept`, not `IsAccepted`, and no `_meta`; test 4 adds the action `Accept`, test 5 checks there is no `_meta`)
