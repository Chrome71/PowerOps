plan-hash: 8a5aaf4bd471ee09ca8538f02ef3a8566e9009ec9b5975cd23a3e1efb675b853
model: claude-sonnet-5-5
fresh-context: yes

The plan-hash was updated after the text fixes for R1 to R17 below (the practice of the M7 to M19 reviews). Reviewed hash: 074c2d2ea518b72fe92a5ae4c1b0d4e93bd2fbbaa40acc97ceb38dc7726f175a. The reviewer (a general-purpose agent following this skill, Sonnet, fresh context) wrote the findings; the planning session wrote the resolutions. The fixes changed the Goal, Users, Data (the import forms table, Output), Automation (Files, what is an import, the reading rules, the flag table, the check order, exit codes, the deadline, block lines, tests, the HANDOFF section), Constraints, Assumptions, Open questions (one, for the user: R11) and Steps (new Steps 2 and 7). Answers changed in `decisions.md`: Q3 and Q6, and Q7 was added. The reviewer contacted no tenant.

## Findings

### High

#### R1. The hook command drops the `|| exit 2` guard, so a Codex hook that fails to run allows the import
- Area: exit codes and fail-open paths (not a Power Platform item; closest is 10)
- Finding: Q4 and "Exit codes" correctly say both clients let a call through on any exit code but 2. But the HANDOFF hook command in the plan is `powerops deploy gate "<plan file>"` alone, with `onFailure: "block"` as the only backstop, and Codex has no `onFailure`. On Codex, `powerops` not on PATH (127), a missing .NET runtime, a crash before the gate's own catch, or any bug that returns 1 or 3 all let the import through. The plan accepts "a missing .NET runtime lets the call through there" as a known limit, but it need not be one: commit 6b7ccfb's hooks end in `>&2 || exit 2`, and its `HandoffDocTests` assert `EndsWith(">&2 || exit 2")`. Step 4 replaces those tests and the guard goes with them. "Exits 0 or 2 only" is a promise about the gate process; only the hook's shell line can turn every other outcome into 2. On Claude Code the hooks page (spot-checked) says `onFailure: "block"` covers a hook that can't start, an exit other than 0 or 2, and a timeout, so the gap is Codex only.
- Suggested fix: keep a guard in every hook command, for example `powerops deploy gate "$(git rev-parse --show-toplevel)/deploy/powerops-plan.json" || exit 2` (or `|| { echo 'powerops deploy gate failed' >&2; exit 2; }`, so Codex gets a non-empty stderr when the shell prints nothing). Keep `onFailure: "block"` for Claude Code. Make `HandoffDocTests` pin the suffix, and reword the Codex paragraph so only a hook timeout or a shell that can't start lets a call through.
- Resolution: fixed (every hook command ends in `|| exit 2`; `HandoffDocTests` pin it; the Codex paragraph says only a timeout or a shell that can't start lets a call through; Step 7 checks the missing-command case; an assumption covers the empty-stderr case)

#### R2. A pass proves neither that anyone reviewed the plan file nor that the user said yes
- Area: 5 (route and approval) and 8 (who can change what)
- Finding: Goal, Users and Q2 call the plan "reviewed", but the gate only compares the call with the file at the hook's path and with the live target. Nothing ties that file to a person's review. `powerops deploy plan <folder> --target <env> --save deploy/powerops-plan.json` is read-only, and AGENTS.md restricts only `--apply` and `deploy_apply` with `apply: true`. So an agent can write a fresh plan for any zip in `deploy/` and any environment it can read, then run one `pac solution import` with the absolute paths the block lines teach it, and the gate allows it. Exit 0 adds no confirmation, so where the client does not prompt (Claude Code in bypass-permissions mode, Codex with approvals off) the import runs with no human step. That is weaker than `deploy_apply` (the typed environment name) and than the AGENTS.md rule for `deploy apply --apply`. The plan never says whether a pass counts as the user's yes. Q5 leaves `powerops-implement` step 7 unchanged, which is an instruction and not a control. "What it doesn't" in the HANDOFF text omits this, and decisions.md never asked it.
- Suggested fix: say in the Goal and the HANDOFF section that a pass shows the call is consistent with the plan file and the target, not that the file was reviewed or the import agreed, and that the user's yes stays required by AGENTS.md and step 7. Reword "an agent's use of a Microsoft import tool goes through PowerOps first" to match. Record a decision on binding the file to a review, for example requiring the plan file to be tracked and unmodified in git, or taken from a CI artifact at a path the agent does not write. If it is not bound, list it under Constraints as an accepted risk.
- Resolution: fixed (decision Q7: a pass shows that the call matches the plan file and the live target, not a review or the user's yes; said in the Goal, Users and the HANDOFF section; the missing tie to a review is an accepted risk under Constraints)

#### R3. No listed test can fail if the gate passes the wrong environment to the dry run
- Area: 9 (testability)
- Finding: The gate exists to tell an import into Dev from one into Test. Tests 4 and 5 use `FakeTarget` from `DeployApplyCommandTests`, whose `Read(target, ...)` returns the same name and environment id whatever `target` is. "Another environment (the fake target's friendly name differs)" only makes the fake always answer with another name. That case passes whether the gate sends the call's environment, the plan's target, the zip path or a constant as `--target`. Test 4 says "the fake target was read once", not with which value, although the fake already records it in `Solutions[i][0]`. A build that reads the environment from the wrong flag in one of the three forms (`--environment`, `--env`, MCP `--environment`) passes every listed test. There is also no case for the same friendly name with another environment id, which only the hash catches.
- Suggested fix: in tests 4 and 5 assert that the value the fake received equals the call's environment text in each form, for an id and an https URL. Use a fake that answers by that value with three environments: Test (the plan's), Dev (another name and id) and one with Test's name and another id. The same plan file and zip must pass for the first and block for the other two, in the CLI, script and MCP forms.
- Resolution: fixed (test 4: a fake that answers by value with Test, Dev and a twin with Test's name and another id; each case asserts the value the fake got, in the CLI and MCP forms, as a GUID and an https URL)

### Medium

#### R4. Duplicate flags are not specified per alias, so a second spelling could change the zip or environment after the gate checked it
- Area: 9 (reading rules, wrong-zip path)
- Finding: "A flag given twice" blocks, but the flag table pairs aliases (`--path` with `-p`, `--environment` with `-env`, `--stage-and-upgrade` with `-up`, `--async` with `-a`, `--max-async-wait-time` with `-wt`), and the plan does not say the duplicate check folds aliases. A command like `--path /a.zip -p /b.zip` has two zip values. If the gate keeps the first and `pac` keeps the last, the gate hashes the reviewed zip and `pac` imports another. The same holds for `--environment` with `-env`. The test list says only "a flag twice".
- Suggested fix: state that duplicates are detected on the canonical flag, with aliases folded, for every role. Add tests for `--path` with `-p`, `--environment` with `-env` and `-up` with `--stage-and-upgrade`, in both orders, each expecting the "is given twice" line.
- Resolution: fixed (rule 4: a flag counts as given twice under any of its names; test 3 covers each pair in both orders)

#### R5. Test 5's common assertion cannot pass for several of its cases and is too weak for the rest
- Area: 9 (testability)
- Finding: Test 5 requires "a stderr that starts with `error: deploy gate:`". The Block lines section says a dry-run failure prints the dry run's own `error:` lines first, then `error: deploy gate: the dry run did not pass`. Plan file missing, symbolic link, another version, a target mismatch and a read that throws all print an own line first. Several of those cases cannot satisfy the assertion as written. The assertion also does not name the reason each case must give. Only test 4 shows that the passing path works, so a gate that blocks every case for one wrong reason (for example a zip hash mismatch from test setup) passes all of test 5. Test 5 has no `DoesNotContain` for the environment value or the folder path, which the "Never printed" promise needs and `DeployApplyCommandTests` already checks with `_root`.
- Suggested fix: make test 5 table-driven (case, expected reason line), assert that the last lines are the guidance line, and for dry-run cases assert the dry run's own message. Add `DoesNotContain` checks for the environment value, the temp folder and the command text.
- Resolution: fixed (test 6 is a table of case and expected line, asserts the right guidance line last, the dry run's own lines for its cases, and that stderr holds no environment value, temp folder or command text)

#### R6. Import routes left out of the markers and of "What it doesn't"
- Area: 5 (route) and 10 (areas never asked)
- Finding: (a) Learn's `pac package deploy` page says it deploys a package to Dataverse. It takes `--package` and `--solution` (a solution zip) and `--environment`, and is available only in the .NET Full Framework build of `pac`. It has no marker and is not an always-block route, so it passes the gate. `pac solution upgrade --solution-name` applies the holding upgrade that a blocked `--import-as-holding` would have staged. It is not an import but belongs in the same note. The Log says the sweep of the skills was not complete, and nothing says the same of `pac` itself. (b) The shell matchers cover `Bash` and `PowerShell` only. Other tools that run a command string, such as an MCP terminal tool or Claude Code's background-task tools, are not matched, and the plan does not mention them. (c) The gate accepts any `mcp__*__solution_import` but the example hooks match one server name, `mcp__pac-cli__...`. The plan does not say a differently named server is silently ungated.
- Suggested fix: add `pac package deploy` as an always-block marker (`package` then `deploy`, same shape as `pipeline deploy`), with a test, or list it under "What it doesn't". In "What it doesn't" also list other command-running tools, `pac solution upgrade`, and the server-name dependence of the first matcher. Consider a regex matcher for the MCP group, and say how to check the name.
- Resolution: fixed (`pac package deploy` and `pac solution upgrade` are always-blocked markers with tests; the HANDOFF section lists other command-running tools and says a server with another name needs that name in the matcher)

#### R7. The script form is allowed on the strength of a comment in the script
- Area: 9 and 6 (flags the plan says change the import)
- Finding: The plan blocks `--force-overwrite`, `--activate-plugins` and `--skip-dependency-check` because they change what an import does, but allows `solution-settings.js import` as if it ran `pac solution import` with none of them. At `0a7bf47` the script does not build the `ImportSolution` request; a vendored SDK does, and the plan did not read it. The script's header claims pac-like defaults ("overwrites no unmanaged customizations", "activates no plug-ins or workflows", "skips no dependency check", "not a holding solution or an upgrade"), but that is a comment, not the request. The script form also has the least use: in Dev the plan check blocks it anyway, and Test and Prod imports are for `deploy apply`.
- Suggested fix: either read the SDK's request defaults at that commit and put them in Data as verified (Step 1), or make the script form an always-block route like `import-solution`. That also drops one parser path, one flag table and its test cases.
- Resolution: fixed (the model-apps `solution-settings.js import` always blocks; the script parser path and its flag table are gone; Q3's answer changed)

#### R8. Nothing tests the real process boundary, and the exception path may use `Guard`, which returns 3
- Area: 9 (testability)
- Finding: The gate's value is its exit code. All tests call `Program.Run` in-process with a `StringReader`. Not covered: `Main`'s UTF-8 reader over the real stdin, the `Console.IsInputRedirected` block, empty stdout from the real process, and exit 2 out of `Main`. Every sibling command wraps its action in `SettingsCommand.Guard`, which returns 3 on any exception, and 3 allows the import. The plan says exceptions give 2 but not that `Guard` must not be used, and test 5 throws only one exception type, from the fake read. Other throwing points are the JSON reading, the zip hashing (an unreadable zip, a path with an embedded NUL from `\u0000` in the MCP form) and the stdin read. Where the `IsInputRedirected` check lives is also unspecified. In the command it makes the tests depend on the test host's stdin; it belongs in `Main`, for the production reader only. A process-level pattern exists (`ProgramTests` runs the built assembly as a child process).
- Suggested fix: say the gate's whole action is in its own try/catch that returns 2 (not `Guard`), and that `Main` also catches for this command. Put the redirect check in `Main`. Add child-process tests: a piped non-import call gives exit 0 with empty output; a piped import call with a missing plan file gives exit 2, empty stdout and a non-empty stderr; empty stdin gives exit 2. Add in-process cases with a path holding a NUL and an unreadable zip.
- Resolution: fixed (the gate's action has its own try/catch that returns 2, not `SettingsCommand.Guard`; the stdin check is in `Main`'s input function; `ProgramTests` run the built program as a child process; NUL and unreadable-zip cases added)

#### R9. The shipped hook example is never run in a client
- Area: 9 (way to test before use)
- Finding: Step 1 runs before the build, with a hook that only saves stdin. After Step 3 and Step 4 nothing runs the final example config with the built `powerops` in Claude Code or Codex. `HandoffDocTests` check the JSON as strings. A wrong matcher, an unexpanded `$(git rev-parse --show-toplevel)` in a client's hook shell, or a Codex `hooks.json` shape or trust problem leaves the hook unmatched or failing, and an unmatched hook allows the call. "No live run" is right for the tenant read, but the block paths and the non-import path need no tenant.
- Suggested fix: add a step after Step 4 using a toy MCP server and a scratch repo with the final example config: a non-import `Bash` command runs; a `Bash` `pac solution import ...` with no plan file is blocked and the reason reaches the model; the toy `solution_import` is blocked. Run it in Claude Code, and in Codex only with the user's yes as in Step 1. Record the result, and what is not verified, in the BUILD-PLAN verification line.
- Resolution: fixed (Step 7 runs the HANDOFF example in `claude -p` with a toy server, a scratch repo and the packed `powerops`, and with `powerops` off PATH; Codex after the user's yes)

#### R10. The guidance line contradicts the reading blocks, and mentions of an import will be blocked with the wrong advice
- Area: 9 and 10 (Q3's promise)
- Finding: The guidance line ends every block with "Do not retry this import another way". The reading blocks say "write it as one plain pac solution import command". An agent gets opposite orders from one block. Q3 promised "a line that says what to write instead". Separately, the markers match command text anywhere, so `git commit -m "..."`, `grep`, or a heredoc that writes docs mentioning `pac solution import` is blocked, and the reading line sends the agent to rewrite a command that was never an import. Agents working in a repo that has these hooks will write about imports. Test 5 asserts "ends with the guidance line", which pins the contradiction.
- Suggested fix: use two guidance lines. For reading blocks: "If this is an import, write one plain `pac solution import` with literal absolute paths; if it only mentions one, reword it." For plan, dry-run and route blocks: the `deploy apply` text and "do not retry another way". Update the tests to match.
- Resolution: fixed (two guidance lines: input and reading blocks say how to write a readable import or to reword a mention; every other block gives the `deploy apply` line and "do not retry". After the re-check (R23), a route line is given only for a recognized command segment; a marker anywhere else, a mention included, gets the reading guidance, and so does a PowerShell block)

#### R11. The Dev build phase of the HANDOFF flow is broken by the hooks, and the way out is not specified
- Area: 5 (route from Dev to Test to Prod)
- Finding: HANDOFF step 2 builds in Dev with Microsoft's skills, which import into Dev (model-apps' `solution-settings.js import`, `dv-solution`). With the hooks on, every such import is blocked, because the one plan file is for Test. The plan's answer is "the hooks off, or a plan for Dev in that file". The second does not work with the fixed hook path: the file holds one environment, so a Dev plan replaces the Test plan. The first is not described. The plan does not say how to turn off one gate for the build phase and back on for steps 7 to 9, or whether Claude Code can switch off one repo hook without `disableAllHooks` (not checked here). Q6's consequence for the build phase was answered by the planning session from the recommended option, and the user did not see it. So the gate protects only the phase where the flow already says to use `deploy apply`.
- Suggested fix: put the question to the user as an open question. If one plan file stays, the HANDOFF section must say exactly how to enable the hooks only for steps 7 to 9 and what that does to other hooks. Otherwise reopen Q6 option 2 (several plan files) or a per-environment hook path.
- Resolution: fixed (the HANDOFF section says to add the hooks at step 7 and remove them when building in Dev again; an open question asks the user about a later `--allow` option for Dev; Q6's answer notes it)

#### R12. The listed test inputs fail on the Windows leg of CI
- Area: 9 (testability)
- Finding: CI runs on windows-latest. `ImportCallTests` use `/work/deploy/PowerOpsSeed.zip`. `Path.IsPathFullyQualified` is false for that path on Windows, so every "readable form" case blocks there and the tests fail. The "pure, no file access" class depends on the operating system for this check. A Git Bash path such as `/c/work/x.zip` blocks on Windows for the same reason, and `C:\work\x.zip` is blocked unless single-quoted, because a backslash outside quotes is an escape in the plan's splitter. The plan says none of this.
- Suggested fix: build absolute test paths per OS (`Path.GetFullPath` or `Path.GetTempPath()`, JSON-escaped), or give `ImportCall` an injected path check. Add a Windows note to the HANDOFF section: write paths as single-quoted `C:\...`, or accept the block.
- Resolution: fixed (test paths built per OS; Windows cases for a quoted `C:\...` path and a blocked `/c/...` path; a Windows note in the HANDOFF section)

### Low

#### R13. The `--max-async-wait-time` value is not checked, so an allowed import line can carry a command substitution
- Area: 9 (reading rules)
- Finding: Rule 5 requires only the zip, settings and environment values to be literal. The value of `--max-async-wait-time` or `-wt` is accepted as whatever word follows. Because `(` and `)` end segments, `-wt $(cmd)` becomes the value `$` followed by a separate segment `cmd`, and the shell still runs `cmd`. This is not an import hole, since an unmarked command passes anyway and a marked one is caught as a second import segment or a leftover marker. But the line the gate allows can run code the gate did not read. The MCP form takes "any string" for the same key.
- Suggested fix: require a literal non-negative integer (minutes, per Learn) for this value in all forms. Test `-wt '$(x)'`, a backtick value and a non-numeric value.
- Resolution: fixed (`--max-async-wait-time` and `-wt` take digits only, in both forms; rule 5 now requires every value in the import segment to be literal; tests added)

#### R14. Deadline mechanics: a shared writer, a leaked check, and a timeout the plan can't pin
- Area: 9
- Finding: (a) After the deadline the check keeps running and can write to the same `error` writer while the main thread writes the deadline line. `StringWriter` is not thread-safe, and a late dry run can print after the deadline line. (b) The deadline test's fake read must block on an event the test releases, not a long sleep, and must assert that the reason is the deadline line and not an unrelated error. (c) "Half of the clients' default hook timeout of 600 seconds" holds only while both defaults stay.
- Suggested fix: give the check its own buffered writer and copy it only if it finishes first. Block the fake on an event. Set an explicit `timeout` in both hook examples, above 300 seconds, and pin it in `HandoffDocTests`.
- Resolution: fixed (the check writes to its own buffer, copied only if it finishes first; the deadline test's fake waits on an event; the hook examples set `timeout: 600`, pinned in `HandoffDocTests`)

#### R15. No step brings commit 6b7ccfb into the branch
- Area: 9 (steps checkable)
- Finding: Constraints say Step 4 needs 6b7ccfb in the build branch first, and Step 4 replaces "the two hook tests", which exist only in that commit. No step moves it. It sits directly on the current head (bec64c9), so a cherry-pick is clean. `HandoffDocTests` also reads the first ```json block of the page, so the page must keep exactly one.
- Suggested fix: add the cherry-pick as its own step before Step 4, kept as its own commit.
- Resolution: fixed (Step 2 brings `6b7ccfb` into the build branch as its own commit)

#### R16. Stdin limit and wiring are loose
- Area: 9
- Finding: "At most 1 MB" does not say bytes or characters. A bounded read of a `TextReader` counts characters, so 1 M characters can be 4 MB of bytes. Over the limit the gate blocks every tool, including a non-import `Bash` command with a large heredoc. That is fail-closed by design but is not stated for users. `Main` passes a reader over stdin to `Run` for every command, including `powerops mcp`, which owns stdin for JSON-RPC.
- Suggested fix: read the stream with a byte cap and decode, say the oversize rule in the HANDOFF section, and pass a lazy reader factory so only `deploy gate` opens stdin.
- Resolution: fixed (the input is a `Func<Stream?>` that only `deploy gate` calls, capped at 1,048,576 bytes; the oversize rule is in the check order and the HANDOFF section)

#### R17. Small consistency points
- Area: 9
- Finding: (a) "Never printed: the environment value" cannot hold for dry-run failures, because `Forward` prints `pac`'s text, which can echo the value. The agent supplied the value, so the impact is small, but the promise should say "not printed by powerops". (b) `deploy apply` passes no `--async` or `--max-async-wait-time` (`SettingsCommand.ImportArguments`), while the plan says the allowed flags leave the import "as `deploy apply` would run it". That is true in effect but should be said. (c) AGENTS.md's "Add a command" recipe gives 0/1/2/3 as the rule, and the exception is recorded only in BUILD-PLAN decision 30. (d) The shared dry-run method should include `DeployApply.FlowIds`, which sits in the same try/catch as `Imports` in `Apply`, so the two commands cannot diverge.
- Suggested fix: reword (a) and (b) in Data and Output. Add one line to AGENTS.md or to the command's description for (c). State (d) in the Files list.
- Resolution: fixed ((a) Output says powerops never prints the value, and `pac`'s forwarded text may; (b) the flag paragraph says what `deploy apply` passes and that `--async` and `--max-async-wait-time` change only the wait; (c) AGENTS.md's recipe gets a clause, Step 4; (d) the shared method includes `DeployApply.FlowIds`)

## Checklist coverage

- `powerops plan facts` was not run: `environment` is null and the build contacts no tenant. No `pac` or `az` command was run.
- Items 1, 3, 4 and 7 do not apply (no solution, publisher, licence, connector or data volume). Item 2 applies lightly: the plan and decisions hold no URL, GUID, email or person's name (the test host is a fake Contoso name). Item 6 applies narrowly: stage-and-upgrade follows `DeployApply.Imports` (an upgrade of a managed zip), as the plan says. Item 8 applies as who can write the plan file and hook config (R2). Items 5 (R2, R6, R11), 9 (R3 to R5, R8, R9, R12 to R16) and 10 apply.
- Open questions: 0. Questions for your admin: none. All six answers in decisions.md were given by the planning session from the recommended options (the user had delegated). The areas never asked are whether a pass counts as the user's yes (R2), the Dev build phase (R11) and other import commands (R6).
- Claims checked against the code and docs and found right: `ReadPlanFile`'s rules, the plan hash covering the environment id, the hash covering zip and settings bytes, `Imports` giving stage-and-upgrade only for an upgrade of a managed zip, `Program.Run` returning 2 on parse errors, `FakeTarget` recording the target value, the 30-second limits in `DriftLiveReader`, `powerops-implement` step 7 and `SkillsCommandTests` staying as they are, the `pac solution import` flags and aliases and the `pac env who --environment` parameter on Learn, and the hooks page on `onFailure`, the 600-second default and exit-code behavior.

## Re-check of the fixes

A second fresh-context agent (powerops-reviewer, Sonnet, read-only) checked R1 to R17 against the changed plan on 2026-10-28: 16 fixed, R10 partly (closed by R23 below). It found no High, 3 Medium and 6 Low new findings, numbered on here. The planning session wrote this section from its reply and fixed each one in the plan text; the plan-hash above is the body after those fixes.

### Medium

#### R18. Quote-split words get past the marker scan
- Area: 9 (reading rules)
- Finding: Markers were matched on the raw text, so `pac sol""ution import ...` held none, and the shell joins the pieces. A command word such as `$X/pac` was not required to be literal.
- Suggested fix: scan the markers on the de-quoted words too; require the import segment's first word to be literal.
- Resolution: fixed (markers are looked for in the raw text and in each segment's de-quoted words; every segment whose de-quoted command word is `pac` is read whether or not it holds a marker, and its first three words must be literal; tests for `pac sol""ution import`, `pac solution im''port`, `pac $s import` and `$X/pac solution import`; the HANDOFF section says a command built from variables gets past)

#### R19. The test fakes the plan reuses are private
- Area: 9 (steps buildable)
- Finding: `FakeTarget`, `FakePac` and the plan helpers are private to `DeployApplyCommandTests`, which runs in the non-parallel `deploy apply` collection.
- Suggested fix: move them to a shared helper and say which collection the gate tests join.
- Resolution: fixed (Files: `tests/PowerOps.Cli.Tests/DeployFakes.cs` holds them, `DeployApplyCommandTests.cs` is listed as touched, and `DeployGateCommandTests` joins the `deploy apply` collection)

#### R20. UNC and device paths are accepted
- Area: 9 (input checks)
- Finding: `Path.IsPathFullyQualified` is true for `\\host\share\x.zip`, `\\?\...` and `\\.\...`, so the gate would open an agent-chosen share or device.
- Suggested fix: reuse `McpCommand`'s refusal of `\\` and `//` paths and refuse device names; add Windows tests.
- Resolution: fixed (Data and the path rules refuse `McpCommand.IsNetworkPath` paths and any path for which `File.Exists` is false; its own reading line; tests for `//host/x.zip`, and on Windows `\\host\share\x.zip`, `\\?\C:\x.zip` and `\\.\CON`)

### Low

#### R21. The `|| exit 2` guard doesn't cover shells that can't parse the line
- Area: 10
- Finding: fish before 3.4 and Windows PowerShell 5.1 can't parse the hook line, and Codex then lets the call through; Codex on Windows uses `cmd.exe /C`.
- Suggested fix: state the shell versions.
- Resolution: fixed (Exit codes and the HANDOFF Codex bullet name sh, fish 3.4 or later, the two shells that fail, and a full plan path for `cmd.exe`)

#### R22. Call-derived text is echoed unsanitised
- Area: 9
- Finding: flag words and file names from the call reach stderr with no `Clean` and no length cap.
- Suggested fix: apply `TextReport.Clean` and a cap; test it.
- Resolution: fixed (Output: cleaned and cut to 100 characters; test 3 has a 300-character flag with a control character)

#### R23. R10 residue: mentions get a route line, and `2>&1` blocks
- Area: 9 and 10
- Finding: `grep ImportSolution src/` got a route line and "do not retry"; the common `2>&1` was a blocking redirection.
- Suggested fix: give marker-only mentions the reword guidance; say how `2>&1` is handled.
- Resolution: fixed (route lines only for a recognized segment, everything else the reading guidance; `2>&1` and `>&2` are dropped from the import segment, other redirections block; tests and the HANDOFF section say so)

#### R24. The oracles can't see the allow path
- Area: 9
- Finding: the `ProgramTests` missing-plan case would fail at the zip check, and Step 7 sees only blocks.
- Suggested fix: write a real zip in that test; record that the allow path in a client is not verified.
- Resolution: fixed (the test writes a zip, so the block is the dry run's; Step 8 records the allow path in a client as not verified, since it needs a tenant read)

#### R25. A stale HANDOFF step 8 sentence and BUILD-PLAN leftovers
- Area: 9
- Finding: HANDOFF step 8 and the README line from `6b7ccfb` say Microsoft's imports may run behind the hooks; renaming the Phase 5 heading breaks its anchor; the title's range and the labels are stale.
- Suggested fix: edit them in the steps.
- Resolution: fixed (Step 5 changes HANDOFF step 8 and the README line; Step 3 keeps the Phase 5 heading and anchor, adds decision 30 with the v0.20.0 label, and moves the title's range to v0.20)

#### R26. Small gaps
- Area: 9
- Finding: the check's background thread, the page's single `json` block, and duplicate JSON keys in `tool_input` were not stated.
- Suggested fix: state them.
- Resolution: fixed (Files: the check runs on `Task.Run`; the HANDOFF section keeps exactly one `json` block; a key that appears twice anywhere in the hook input blocks, with a test)
