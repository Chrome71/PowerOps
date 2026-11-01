plan-hash: 46489ceb6d28af93692950c5a12b12f951343e10ebf01e5b3265e0a650e9e763
model: claude-sonnet-5-5
fresh-context: yes

The plan-hash was updated after the text fixes for R1 to R11 below (the practice of the M7 to M32 reviews). Reviewed hash: 899eaed2c4962214502454c74764550fb32548a1bdf86015845d395f71249439. The reviewer (powerops-reviewer, Sonnet, fresh context) found 0 High, 3 Medium and 8 Low; the coordinating session relayed the findings to the planning agent, which fixed the plan, and wrote this file from the reviewer's report with the resolutions. The reviewer re-ran the planner's S-5 and S-8 probes and its own, outside the repo, read the SDK source at v2.3.0, the MCP 2026-07-28 schema and tools page, and the Learn pages, and contacted no tenant.

Confirmed by the review: the S-2 mechanism (the approved hash is a C# parameter of the in-process entry point, so no argument list can supply it, and `onRan` fires after the last recheck, so the swap test is buildable), the S-5 filter as the only route that reaches a lone surrogate, the S-8 host wait (`McpSessionHandler.cs` waits for in-flight handlers; sends are dropped once disconnected), C-9's rule and exit code, and no conflict with M21's Dataverse-host rule or its rules on MCP strings.

Most of the Power Platform checklist does not apply: there is no solution, publisher, connector, environment variable, tenant, DLP policy, route, managed state, delegation or security role (items 1 to 8). Item 2 applies only through the plan's URLs, which are public Learn, GitHub and specification pages, each with the `plan status` allow marker. Open questions: 0. No GUID, email or person's name in the plan or `decisions.md`.

## Findings

### High

No findings.

### Medium

#### R1. The filter's result has no `resultType`, and the planned test could not catch it
- Area: testability (Data, S-5; Assumptions; decisions Q1)
- Finding: The plan left the missing `resultType` as a risk that a test at 2026-07-28 would reveal, but the SDK's client accepts a result without it, so no such test could fail. The 2026-07-28 schema makes the field required on `Result` for servers of that revision (clients treat an absent one as "complete"), and the SDK stamps it on handler results only at 2026-07-28 or later, because earlier revisions reject the key. A result sent from a filter bypasses that stamp. The SDK's metadata filter runs before user filters, so the revision is known inside ours.
- Suggested fix: Stamp `resultType: "complete"` in the filter at 2026-07-28 or later and not before, reading the revision the way the SDK does, and replace the sentence with a raw-pipe test that asserts the key at 2026-07-28 and its absence at 2025-06-18.
- Resolution: fixed (Data, "S-5: `resultType`": the stamp, the public members in 2.3.0 that give the revision, and a post-review probe that showed the key at 2026-07-28 only; the six lone-surrogate cases are raw-pipe tests that assert it)

#### R2. `Serve` cancels every running handler at end of input, beyond what S-8 asks
- Area: scope (Automation, `Serve`; decisions Q2)
- Finding: Cancelling the run token when the transport's reader completes also cancels the token of every handler still running (the reviewer's probe: `ct cancelled: True`). A `deploy_plan` with `save` still running at end of input would skip its save, and a `deploy_apply` between the yes and the import would take the cancelled-call refusal. The S-8 row asks for a test, and a guard only if the host does not wait; it does. The hang `Serve` fixed (an unanswered form at 2025-06-18 keeps the server running after its input closes) is real: the reviewer reproduced it.
- Suggested fix: Either keep `Serve` with a test and a sentence for the `deploy_plan` side effect, or drop it and document the hang.
- Resolution: fixed (the coordinating session chose to drop it: `Serve` and its two tests are gone, decisions Q2 is answered with option 2 and says why, the S-8 import test stays, and Step 6 and the Step 7 "Accepted:" bullet name the hang as a residual that a later change could end)

#### R3. Step 6's sentence on a stopped server contradicts Data
- Area: testability (Steps, 6)
- Finding: It said the server "ends the import it started and exits without a report". Data and the probe show it waits for the running import to finish, then exits with no report. The done-when needs the docs to match what the test shows, and "ends" reads as cancelled.
- Suggested fix: Write "finishes the import it is running, then exits without a report".
- Resolution: fixed (Step 6)

### Low

#### R4. S-3's wording was stronger than the sources
- Area: sources (Data, S-3; decisions Q3)
- Finding: The plan said Learn "says a token's audience is one URL". The pages read show the scope naming the environment URL, the troubleshooting page tying the audience to the scope (with a non-Dataverse example), and the claims page describing the audience as the API's client ID or app ID URI; none gives Dataverse's `aud` or says what Dataverse validates. The conclusion holds: nothing says such a token works at another org, so decision Q11's stop condition is not met.
- Suggested fix: Say that the scope names the environment URL and that Learn gives no `aud` value for Dataverse and says nothing on cross-org use; add the Learn page on access tokens as a source.
- Resolution: fixed (Data, S-3, with "Access tokens in the Microsoft identity platform" added and read on 2026-10-29)

#### R5. C-9 could give one entry two messages
- Area: correctness (Data, C-9)
- Finding: `DriftCommand.Run` already reports a `.json` entry that does not exist; the new rule would also fire for `a/b.json`.
- Suggested fix: Make the new rule an `else if` after the existing problem.
- Resolution: fixed (Data, C-9)

#### R6. Step 3's test count
- Area: testability (Steps, 3; Tests)
- Finding: The Tests section lists 4 refused and 2 not-refused cases in `DriftCommandTests` and 1 MCP case, 7 in all; Step 3 said +6.
- Suggested fix: Make Step 3 +7.
- Resolution: fixed

#### R7. Stale base commit
- Area: accuracy (Goal; decisions Log)
- Finding: Both named `fe58ae5`; the base is main `c703306`, the squash of M21, which differs from it only by 7 lines of `ImportCallTests.cs`, so the code claims hold.
- Suggested fix: Name `c703306`.
- Resolution: fixed

#### R8. The D-15c sentence does not fit `plan_facts`
- Area: correctness (Data, D-15c)
- Finding: `plan_facts` has no environment input; it reads the environment `pac` is signed in to. `docs/COMMANDS.md` carries the agents' rule only for `drift` and `deploy plan`, so the plan's "repeats the rule" was not true for `plan facts`.
- Suggested fix: Word `plan_facts` on its own and add the missing rule to COMMANDS.md, and make the description test check each tool's own sentence.
- Resolution: fixed (Data, D-15c; Step 6 adds the COMMANDS.md line; the test checks each tool's sentence)

#### R9. The filter's catch set was not named
- Area: correctness (Data, S-5)
- Finding: The plan said only "when it throws". A catch-all would label an unrelated failure as an unpaired surrogate.
- Suggested fix: Name the exceptions caught.
- Resolution: fixed (the filter catches `InvalidOperationException` from `Params.ToJsonString()`, the only one the probes saw, and nothing else; the coordinating session dropped `ArgumentException`, which no probe raised)

#### R10. Step 5's order and the S-8 test's mechanics
- Area: testability (Steps, 5; Tests)
- Finding: The form test could not be seen to fail before `Serve`, because the tests call `Serve` and would not compile without it; "input completed" must mean completing `clientToServer.Writer` directly, not disposing the client, which cancels the call.
- Suggested fix: Add `Serve` as a pass-through first; complete the pipe's writer directly.
- Resolution: fixed (the form tests went with `Serve` under R2; the S-8 import test completes `clientToServer.Writer` directly, and Step 5 is a `test(cli):` commit)

#### R11. The S-2 refusal reports the swapped file's hash
- Area: correctness (Automation, `DeployCommand`)
- Finding: The planned refusal carries `reviewed.Hash`, the new file's hash, not the one the user approved.
- Suggested fix: Report the approved hash, or keep it and say so.
- Resolution: fixed (the refusal's message names both the approved hash and the one found; the JSON `hash` field stays `reviewed.Hash`, as for the other refusals, and the plan says so)
