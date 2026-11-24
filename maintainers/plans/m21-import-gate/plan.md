---
status: done
review: done
signed-off: 2026-10-28
signed-off-hash: a36bebe57258710c95661a13985b195321d39303d03f9f8fd5139e1c1e967867
environment: null
depth: quick
supersedes: null
---

# M21: `powerops deploy gate`, the check before Microsoft's imports

## Goal

Build a new milestone, M21 (build-order item 46, release label v0.20.0): `powerops deploy gate <plan-file>`, the command that a client `PreToolUse` hook runs before pac's MCP tool `solution_import` or a shell command that imports a solution. On 2026-10-28 the user said that Microsoft's import tools may be needed, but PowerOps must review each use first. The hooks in the HANDOFF section "Gate Microsoft's imports" (commit `6b7ccfb`) run the dry run of `deploy apply`, so they prove that the reviewed plan is current. They can't see which zip or which environment the call imports, so they can't tell an import into Dev from one into Test.

The gate reads the hook's JSON on stdin and finds the zip, the settings file and the environment that the call names. It allows the call (exit 0) only when the call is the plan's first import, as `deploy apply` would run it, into the environment the plan was made for, and the plan still matches: the same dry run as `powerops deploy apply` without `--apply`. Otherwise it blocks the call (exit 2) and writes the reason on stderr. Both clients give that stderr to the agent as the reason for the block.

A pass shows that the call matches the plan file at the hook's path and the live target. It is not a record that someone reviewed that file, and it is not the user's yes to the import: AGENTS.md and `powerops-implement` step 7 still require that yes, and the client still asks before the tool call unless the user turned its prompts off (decisions Q7).

Done when:

- `powerops deploy gate <plan-file>` reads the hook input on stdin, writes nothing to stdout, and exits 0 or 2 only. Every other outcome, an exception and the deadline included, gives exit 2 with a reason.
- The offline tests listed under Automation pass. They cover the parser over both clients' input shapes and every Microsoft command form in Data, the command with fakes that answer per environment value, and the built program as a child process. In no test does the gate run an import.
- The HANDOFF section's hooks run `powerops deploy gate ... || exit 2`, the section says what the gate checks and what it doesn't, `HandoffDocTests` check the hook commands, and the example ran in Claude Code with a toy server (Step 7).
- `dotnet build --no-incremental` (0 warnings), `dotnet test`, `dotnet format --verify-no-changes`, `dotnet pack src/PowerOps.Cli -c Release -o artifacts` and `powerops plan status .` are clean. No tenant is contacted, and nothing is imported.

## Users

- Repo owners who set the hooks from the HANDOFF page, so that an agent's call to a Microsoft import tool is checked against the plan before it runs.
- Agents in Claude Code and Codex. They see the block reason, which says how to write a readable import, or to import with `powerops deploy apply` after the user's yes.

## Data

**Hook input (verified 2026-10-28).** JSON on stdin, read as UTF-8 bytes, at most 1,048,576 bytes. The gate reads only `tool_name` (a string) and `tool_input` (an object). It doesn't use `cwd`, because Codex doesn't send the shell tool's `workdir` (see below), so every path must be absolute.

| Client | `tool_name` | `tool_input` | Source |
|---|---|---|---|
| Claude Code, shell | `Bash`, and `PowerShell`, a separate tool with the same fields. On Windows without Git Bash, `Bash` is not registered | `command` (string), `description`, `timeout`, `run_in_background` | Claude Code hooks docs, "PreToolUse input" |
| Claude Code, MCP | `mcp__<server>__solution_import`, so `mcp__pac-cli__solution_import` for the server name in Learn's example; a plugin's server is `mcp__plugin_<plugin>_<server>__solution_import` | The call's arguments. From v2.1.274 the input also carries `mcp_server` (`name`, `source`); the docs don't say whether it sits in `tool_input` or beside it, so the gate ignores that key in `tool_input` (Step 1 records where it is) | Claude Code hooks docs, "PreToolUse input" |
| Codex 0.163.0, shell | `Bash`. Only `exec_command` builds a hook payload | `{"command": <string>}`. The tool's `workdir` argument is not sent | `codex-rs/core/src/tools/handlers/unified_exec/exec_command.rs`, `codex-rs/core/src/tools/hook_names.rs` at `rust-v0.163.0` |
| Codex, MCP | `mcp__pac_cli__solution_import`: every character of the server name other than a letter, a digit or `_` becomes `_` | The parsed arguments; `{}` when there are none; a JSON string when they don't parse | `codex-rs/core/src/tools/handlers/mcp.rs`, `codex-rs/codex-mcp/src/connection_manager_tests.rs` |
| Codex, other | `apply_patch` with `{"command": <patch text>}`, `spawn_agent` | Not imports: allowed | `codex-rs/core/src/tools/handlers/apply_patch.rs` |

Claude Code also sends `session_id`, `transcript_path`, `cwd`, `permission_mode`, `hook_event_name` and `tool_use_id`, and sometimes `prompt_id`, `effort`, `scratchpad_dir`, `agent_id` and `agent_type`. Codex sends `session_id`, `turn_id`, `transcript_path` (string or null), `cwd`, `hook_event_name`, `model`, `permission_mode` and `tool_use_id`, and sometimes `agent_id` and `agent_type` (`codex-rs/hooks/src/schema.rs`, `PreToolUseCommandInput`). The gate reads none of them.

**The import forms (verified 2026-10-28).** pac's MCP schema is the tool list that pac's MCP server serves to Claude Code; the `pac` parameters are from the Learn pages "pac solution", "pac pipeline" and "pac package"; the skills are power-platform-skills at `0a7bf47` and Dataverse-skills at `108370e`.

| Form | Who runs it | Zip | Environment | Settings | Stage and upgrade | Gate |
|---|---|---|---|---|---|---|
| MCP `solution_import` | pac's MCP server. Every argument is an optional string named after the flag | `--path`. Absent means the current folder is a cdsproj project | `--environment`: "a Guid or absolute https URL". Absent means the selected auth profile's environment | `--settings-file` | `--stage-and-upgrade` | Checked |
| `pac solution import` | Dataverse-skills `dv-solution` (`--path ./solutions/<UniqueName>.zip --environment <url> --async --activate-plugins`), or an agent | `--path` / `-p` | `--environment` / `-env` | `--settings-file` | `--stage-and-upgrade` / `-up` | Checked |
| `node <path>/solution-settings.js import --env <envUrl> --package <zip> --settings <json>` | model-apps (`plugins/model-apps/scripts/solution-settings.js`, the genpage skill) | `--package` | `--env` | `--settings` | Never. It imports through the Web API (`StageSolution`, `ImportSolution`), whose request a vendored SDK builds; the script's comments describe pac's defaults, but the request was not read | Always blocks |
| `node <path>/solution-settings.js template ...` | model-apps | - | - | - | - | Not an import: allowed |
| `node <path>/encode-solution-file.js --zipPath <zip>`, then a POST to `ImportSolutionAsync` with the zip in base64 (after `StageSolution`) | power-pages `import-solution` | Only in the encode step | Only in the POST's URL. The skill doesn't say what sends the POST | - | - | Always blocks |
| `pac pipeline deploy --environment <dev> --solutionName ... --stageId ... --currentVersion ... --newVersion ... --wait`; POSTs to `DeployPackageAsync` and `RetryFailedDeploymentAsync` | power-pages `deploy-pipeline` | None: the pipeline exports from the source | The stage (`--stageId`); `--environment` is the source | In the stage run | - | Always blocks |
| `node <path>/import-template-solution.js --zipPath <zip> --envUrl <url> [--publishChanges true\|false]` | power-pages `create-site` | `--zipPath`, a template it packed into a temp folder | `--envUrl` | None | No; it runs `pac solution import` with `--force-overwrite --activate-plugins --async --max-async-wait-time` | Always blocks |
| `pac package deploy --package <file> --environment <url>` (or `--solution <zip>`) | Not in the skills read; the .NET Full Framework build of `pac` only | `--package` or `--solution` | `--environment` | - | - | Always blocks |
| `pac solution upgrade --solution-name <name>` | Not in the skills read | None: it applies an upgrade staged as a holding solution | `--environment` | - | It is the upgrade | Always blocks |

**The plan file.** The argument, read by the dry run under `deploy apply`'s rules (`DeployCommand.ReadPlanFile`: no symbolic link, at most 1 MB, `DeployPlanFile.Read`). The zips and settings files of its folder are read by the dry run as for `deploy apply`.

**The call's zip and settings file.** Opened only to hash them (SHA-256 of the bytes, streamed, `DeployPlan.Sha256Hex`'s format). A path that is not fully qualified on the OS the gate runs on, a network or device path (starting with `\\` or `//`, which covers `\\?\` and `\\.\`: `McpCommand.IsNetworkPath`), a path for which `File.Exists` is false (as for a device name), a symbolic link, or a file that can't be read blocks the call. So the gate never opens a network share or a device that an agent chose.

**Output.** stderr only. stdout stays empty in every case, because both clients read stdout that looks like JSON on exit 0. powerops never prints the environment value, a folder path, the command text or any other `tool_input` value. A flag name or a zip or settings file name that comes from the call is printed through `TextReport.Clean` and cut to 100 characters, as `deploy apply` prints file names. `pac`'s own text, which the dry run forwards after `  | ` when a read fails, may echo the environment value the agent passed.

## Automation

**Files.**

- `src/PowerOps.Core/Deploy/ImportCall.cs`: reads the hook JSON into one of three results: not an import (allow), a block with its reasons, or a call to check, `ImportCall(Zip, Environment, Settings, StageAndUpgrade)`. It holds the shell word splitter and the flag tables. It is pure: no file, process or network access. The path check is `Path.IsPathFullyQualified`, so its result follows the OS, and the tests build their paths per OS.
- `src/PowerOps.Cli/DeployCommand.cs`:
  - The part of `Apply` from the plan file read to `DeployApply.Imports` and `DeployApply.FlowIds` moves into one internal method that `deploy apply` and the gate both call. It returns the fresh plan, its zips and the import steps, or the exit code, the report and the `error:` lines that `deploy apply` would have finished with. The dry run's checks then have one copy, and `deploy apply`'s output stays byte for byte the same (its tests and goldens show that).
  - The `gate` subcommand: one argument, `plan-file`, and no options. Its action is its own `try`/`catch` that returns 2 for any exception, not `SettingsCommand.Guard`, which returns 3. The check runs on a background task (`Task.Run`), so the action can return at the deadline. The check order and block lines are below.
  - `Create` gets the input and an internal deadline (a test seam, 300 seconds in production).
- `src/PowerOps.Cli/Program.cs`: `Root` and `Run` get an optional `Func<Stream?>? input`, which only `deploy gate` calls. `Main` passes a function that returns `Console.OpenStandardInput()` when `Console.IsInputRedirected` is true, else null; the gate blocks on null, so a run from a terminal doesn't wait on the keyboard. `powerops mcp` and every other command never open stdin through it.
- `AGENTS.md`: the "Add a command" recipe's exit-code sentence gets one clause: a command that a hook runs exits only 0 or 2 (BUILD-PLAN decision 30).
- `tests/PowerOps.Cli.Tests/DeployFakes.cs` (new, internal): `FakeTarget`, `FakePac` and the plan helpers (`MakePlan`, `SeedTarget`, `Chain`, `SeedZip`) move here from `DeployApplyCommandTests`, where they are private, so both test classes use them. `DeployGateCommandTests` joins the `deploy apply` collection, which runs without parallel tests because one test changes the current folder.
- `tests/PowerOps.Core.Tests/Deploy/ImportCallTests.cs`, `tests/PowerOps.Cli.Tests/DeployGateCommandTests.cs`, `tests/PowerOps.Cli.Tests/DeployApplyCommandTests.cs` (the helpers move out), `tests/PowerOps.Cli.Tests/ProgramTests.cs`, `tests/PowerOps.Cli.Tests/HandoffDocTests.cs`.
- `README.md` (the usage block), `docs/HANDOFF.md` (the section), then `docs/BUILD-PLAN.md` and this plan's status, for the record.

The "Add a command" recipe's JSON step doesn't apply: the gate prints no JSON, so it has no schema and no golden.

**What is an import.**

1. `tool_name` is `Bash` or `PowerShell`: a shell call. `tool_name` starts with `mcp__` and ends with `__solution_import`: an MCP import. Any other tool is not an import, and the gate exits 0 without reading the plan or a tenant.
2. A shell call is an import when its `command` holds a marker. The markers ignore case: `solution` and then `import`, `solution` and then `upgrade`, `pipeline` and then `deploy`, `package` and then `deploy`, each with only white space, quotes or a line continuation between the two words; `solution-settings.js`; `import-template-solution`; `encode-solution-file`; `ImportSolution` (which also matches `ImportSolutionAsync`); `StageAndUpgrade`; `DeployPackageAsync`; `RetryFailedDeploymentAsync`. The gate looks for them in the raw text and again in each segment's words after quote removal, joined by single spaces, so `pac sol""ution im''port` holds a marker. A segment whose first word, after quote removal, has the file name `pac` or `pac.exe` (ignoring case) is read whether or not it holds a marker (rule 2 below), so `pac $s import` can't hide. A command with neither is not an import: exit 0.
3. A `PowerShell` call with a marker or a `pac` command word blocks. The gate reads shell commands only in POSIX syntax.

**Reading a shell command.** Only `pac solution import` can pass.

1. The splitter follows POSIX words. Blanks split words. Single quotes are literal. In double quotes, `\` escapes `$`, a backtick, `"`, `\` and a newline, and is literal before any other character. A backslash and a newline outside quotes join the lines; any other backslash outside quotes is an escape. `;`, `&`, `|`, `&&`, `||`, a newline, `(` and `)` end a segment. A quote that is not closed blocks.
2. Segments are recognized by the file name of their first word after quote removal, ignoring case:
   - `pac` or `pac.exe`. The first three words must be literal (rule 5), or the segment blocks as an import the gate can't read. Then the next two words name the subcommand: `solution import` is the import; `solution upgrade`, `package deploy` and `pipeline deploy` block with their route's line; any other subcommand is not an import.
   - `node` or `node.exe`, with a second word whose file name is `solution-settings.js` followed by `import` (model-apps), `encode-solution-file.js` (power-pages' `import-solution`) or `import-template-solution.js` (create-site): each blocks with its route's line. The script's path may hold variables (the skills write `"${PLUGIN_ROOT}/scripts/..."`), so only its file name is read. `solution-settings.js` followed by `template` is not an import.
3. Every marker must sit in a recognized segment. The gate removes their text and looks for the markers again. A marker left over blocks as an import the gate can't read, with the reading guidance: an import inside `sh -c '...'` or `eval`, a POST to `ImportSolutionAsync` or `DeployPackageAsync`, `X=1 pac ...`, `$X/pac ...`, and a mere mention such as `grep ImportSolution src/` or a commit message. Two import segments in one command block.
4. In the import segment, every word after `import` is a known flag or the value of the flag before it. The words `2>&1` and `>&2` change only where `pac` writes, so they are dropped; any other redirection (`<` or `>`) in the segment blocks, and so does an unknown word or flag or a missing value. A flag counts as given twice when the same flag appears twice under any of its names (`--path` with `-p`, `--environment` with `-env`, `--stage-and-upgrade` with `-up`, `--async` with `-a`, `--max-async-wait-time` with `-wt`), so `pac` can never read a second value the gate didn't check. The gate names a flag it doesn't read, and calls a word that is not a flag "a word the gate doesn't read", without quoting it. Other segments, such as `| tail -5` after the import, are not read.
5. A word is literal only when it has no `$`, no backtick, no backslash escape, no unquoted `*`, `?`, `[` or `{`, and no leading unquoted `~`. Every word of the import segment must be literal, the command word included.

**Flags the gate reads.** These leave the import as `deploy apply` runs it: `deploy apply` passes `--environment`, `--path`, `--settings-file` and, for a managed upgrade, `--stage-and-upgrade` (`SettingsCommand.ImportArguments`). `--async` and `--max-async-wait-time` change only how `pac` waits for the same import. Any other flag blocks, among them `--force-overwrite`, `--activate-plugins`, `--publish-changes`, `--import-as-holding`, `--convert-to-managed`, `--skip-dependency-check`, `--skip-lower-version`, `--activate-flows`, `--update-canvas-app-ownership`, `--import-mode`, `--path=<value>` and `--Path`.

| Form | Zip (required) | Environment (required) | Settings | Stage and upgrade | Also allowed |
|---|---|---|---|---|---|
| CLI | `--path`, `-p` | `--environment`, `-env` | `--settings-file` | `--stage-and-upgrade`, `-up` (no value) | `--async`, `-a` (no value); `--max-async-wait-time`, `-wt` (whole minutes: digits only) |
| MCP | `--path` | `--environment` | `--settings-file` | `--stage-and-upgrade` with the value `"true"` only; any other value blocks | `--async` (any string); `--max-async-wait-time` (digits only). `mcp_server` is ignored |

In the MCP form, a value that is not a string, an unknown key, or a `tool_input` that is not an object blocks. A key that appears twice anywhere in the hook input blocks, because the gate and pac's MCP server could keep different copies.

Both forms: the zip and settings paths must be fully qualified (`Path.IsPathFullyQualified`), hold no NUL, and not be a network or device path (see Data). On Windows a Git Bash path such as `/c/work/x.zip` is not fully qualified, and `C:\work\x.zip` must be quoted, because a backslash outside quotes is an escape. The environment must be a GUID or an absolute `https` URL. It goes to the dry run as `--target`, which passes it to `pac org who --environment`, so it can never start with `-`.

**Check order.**

1. Input. No stdin, more than 1,048,576 bytes, not JSON, not an object, no string `tool_name`, or a shell call with no string `command`: block. A non-import call with an input over the limit is blocked too (the gate can't read its `tool_name`). Not an import: exit 0.
2. The reading rules above. Any problem: block, one line per problem.
3. The call's files. The zip, and the settings file when the call names one, must exist as regular files that are not symbolic links and can be read. Each is hashed.
4. The dry run, through the shared method, with the call's environment as `--target`. It covers the plan file rules, a plan from another version, a blocked plan, a target whose friendly name is not the plan's (another environment: `deploy apply` exits 2), a hash that differs (the zips, the settings files, the target or the dependencies changed, or the environment is another one with the same friendly name: the hash covers the environment id), and a plan that is blocked now. Unless it passes as `deploy apply` would print status `planned`: block, with its `error:` lines and its report's message.
5. The first import step, from the fresh plan (`DeployApply.Imports`):
   - none (a plan of skips only): block;
   - the call's zip hash must equal that step's zip hash. Else the gate names what the call's zip is: a later step ("the plan imports <first> first"), a skip, or no zip of the plan;
   - the step has a settings file: the call must name one with the same hash. The step has none: the call must name none;
   - the call's stage and upgrade must equal the step's (only a managed upgrade stages).
6. All passed: exit 0.

The checks of the call's text (steps 1 to 3) come first, so a call that can never pass costs no tenant read. The plan file's `solutions` array is not covered by its hash, so the first step comes from the fresh plan of step 4, not from the file.

**Exit codes.** 0 allows the call; 2 blocks it, for every other outcome. Both clients let a call through on any exit code other than 2, and on a timeout or a hook that doesn't start. Claude Code blocks those cases only with `onFailure: "block"`, and Codex has no such setting. So the gate never exits 1 or 3. This departs from AGENTS.md's 0/1/2/3 on purpose, because the hook protocol gives only exit 2 the meaning "block". An exception is caught and gives exit 2. Codex needs a non-empty stderr with exit 2, or the call goes through, so every block writes at least one line. The hook command ends in `|| exit 2`, so a gate that can't start (not on PATH, no .NET runtime) or that crashes outside its own `catch` still blocks; the shell or the runtime writes the error on stderr. That needs a shell that can parse the line: sh and its kin, and fish 3.4 or later. fish before 3.4 (`$()` in double quotes) and Windows PowerShell 5.1 (`||`) fail to parse it, and Codex then lets the call through. Codex on Windows runs hooks with `cmd.exe /C`, which doesn't expand `$(...)`, so there the plan file's path is written out in full.

**Deadline.** 300 seconds, half of both clients' default hook timeout of 600 seconds, which the HANDOFF example also sets explicitly. Each `pac` and Web API read has its own 30-second limit, but a plan of several solutions makes many reads. The check runs with its own buffered writer. If it finishes first, its lines are copied to stderr; past the deadline the gate writes only the deadline line and exits 2, and the check's late output is dropped.

**Block lines** (stderr; each block ends with one of the two guidance lines):

- Input: `error: deploy gate: the hook input <is larger than 1 MB | is not JSON | has no tool_name | has no tool_input object | has no command string>`; `error: deploy gate: the hook input is read from stdin`
- Routes (a recognized segment, rule 2): `error: deploy gate: model-apps' solution-settings.js import builds its own import request, so its options can't be checked`; `error: deploy gate: power-pages' import-solution sends the zip in a request body, so its zip and environment can't be checked`; `error: deploy gate: pac pipeline deploy has no zip, and its target is a pipeline stage`; `error: deploy gate: create-site's template import imports a template solution that no plan holds, and overwrites unmanaged customizations`; `error: deploy gate: pac package deploy imports a package that no plan holds`; `error: deploy gate: pac solution upgrade applies a staged upgrade that no plan holds`
- Reading: `error: deploy gate: imports are read only in Bash commands, not in PowerShell`; `error: deploy gate: the command holds an import that the gate can't read`; `error: deploy gate: the command holds more than one import`; `error: deploy gate: <flag> is missing`; `error: deploy gate: the gate doesn't read <flag>`; `error: deploy gate: <flag> is given twice`; `error: deploy gate: the command has a word the gate doesn't read`; `error: deploy gate: <flag> must be a literal absolute path (no $, backtick, escape, ~ or wildcard)`; `error: deploy gate: <flag> must not be a network or device path`; `error: deploy gate: a key appears twice in the hook input`; `error: deploy gate: <flag> must be an environment id or https URL`; `error: deploy gate: <flag> must be a whole number of minutes`; `error: deploy gate: the quotes are not closed`
- Files: `error: deploy gate: <zip or settings file name> does not exist, is a symbolic link, is not a file, or can't be read`
- Dry run: its own `error:` lines, then `error: deploy gate: the dry run did not pass: <its message>`
- Plan: `error: deploy gate: the plan imports nothing`; `error: deploy gate: <name> is not a zip of the plan`; `error: deploy gate: the plan imports <first zip> first`; `error: deploy gate: the plan skips <name>`; `error: deploy gate: the plan imports <zip> with <settings file>`, `... with no settings file`, `... with a different settings file`; `error: deploy gate: the plan <stages and upgrades | doesn't stage and upgrade> <zip>`
- Deadline and errors: `error: deploy gate: the check took longer than 300 seconds`; `error: deploy gate: internal error (<exception type>)`
- Guidance after an input or reading block: `The call was blocked. If it is an import, write it as one plain pac solution import command with literal absolute paths and the environment's id or https URL. If it only mentions an import, reword it.`
- Guidance after every other block: `The import was blocked. To import a reviewed plan, run powerops deploy apply <plan file> --target <environment id or URL> --apply after the user's yes to that environment. Do not retry this import another way.`

**Tests.** Test paths are built per OS (`Path.GetTempPath()` or `Path.GetFullPath`, escaped for JSON), so the Windows leg of CI reads them as fully qualified. Fake values only (a Contoso host, fake GUIDs).

`ImportCallTests` (Core; the hook JSON as text, plus the inputs recorded in Step 1, scrubbed):

1. Not an import: `ls -la`, `git status`, `dotnet test`, the skill's `solution-settings.js template` line, `powerops deploy apply deploy/powerops-plan.json --target <test> --apply`, and the tools `Write`, `Read`, `spawn_agent`, `mcp__other__list`, and `apply_patch` with a patch whose text holds `pac solution import`.
2. Each readable form gives its call (zip, environment, settings, stage and upgrade): Claude Code's `Bash` input with all four fields; Codex's with `command` only and `turn_id` and `model` beside it; the CLI form with long names and with aliases, with line continuations as `dv-solution` writes them, with single and double quotes, with `| tail -5` or `&& pac solution list` after it, with `2>&1`, and with a quoted command word (`"pac"`); the MCP form under `mcp__pac-cli__solution_import` (with `mcp_server` beside `tool_input` and inside it), `mcp__pac_cli__solution_import` and a plugin server's name; `--stage-and-upgrade`, `-up` and `"--stage-and-upgrade": "true"`; `-wt 30` and `"--max-async-wait-time": "30"`; the environment as a GUID and as an https URL. On Windows only: a quoted `C:\...` path.
3. Each block gives its line: a missing zip or environment (CLI and MCP); a relative path (`./solutions/x.zip`, `x.zip`); `$HOME/x.zip`, `"$ZIP"`, a backtick, `~/x.zip`, `*.zip` and an unquoted backslash escape; a path with a NUL (MCP `\u0000`); `//host/x.zip` and, on Windows, `/c/work/x.zip`, `\\host\share\x.zip`, `\\?\C:\x.zip` and `\\.\CON`; quote-split words (`pac sol""ution import`, `pac solution im''port`), `pac $s import` and `$X/pac solution import`; `> log` (while `2>&1` passes, in test 2); a key twice in the MCP `tool_input`; a flag of 300 characters with a control character (printed cleaned and cut to 100); the environment `Test`, an `http` URL and `-env`; `-wt '$(x)'`, a backtick value and `-wt soon`, and the same in MCP; each unread flag of the list above; each flag given twice under each pair of its names, in both orders (`--path` then `-p`, `-p` then `--path`, and so on); a stray word; a redirection; two imports; an import in `sh -c '...'`, in `eval "..."`, after `X=1`, in `echo "pac solution import"`, in `git commit -m "... pac solution import ..."`, in `grep "pac solution import" docs` and in `grep ImportSolution src/` (each with the reading guidance); a quote not closed; in MCP, a number value, a string `tool_input`, an unknown key and `"--stage-and-upgrade": "false"`; a `PowerShell` call with an import; each always-blocked route with the skill's own command line, and `pac package deploy` and `pac solution upgrade`; and `dv-solution`'s documented form (a relative `--path` and `--activate-plugins`, so two lines). Input: not JSON, a JSON array, no `tool_name`, `Bash` with no `command`, and a `command` that is not a string.

`DeployGateCommandTests` (CLI; the plan written by `deploy plan --save` with the fakes and helpers of `DeployApplyCommandTests`; stdin given as a `MemoryStream`):

4. A target fake that answers by the value it gets and records each value, with three environments: Test (the plan's name and id), Dev (another name and id) and a twin (Test's name, another id). The same plan file and zip pass for Test and block for Dev (the dry run's name mismatch) and the twin (the hash), in the CLI and MCP forms, with the environment as a GUID and as an https URL. Each case asserts that the value the fake got equals the call's environment text.
5. Allowed: the call for the plan's first import, in the CLI form and the MCP form (Claude Code's and Codex's tool names), gives exit 0 with empty stdout and stderr. The seed plan stages and upgrades, so its calls carry the flag; an install step of the chain from `Chain()` has none. The fake target was read once, and the fake `pac` never got `solution import`.
6. Blocked, as a table of (case, expected line): a zip of no plan; the chain's second zip; a skipped zip; the folder's zip changed after the plan (the dry run's message); the settings file missing, different or extra; the seed's managed upgrade without `--stage-and-upgrade`, and an install step with it; the target changed (another snapshot: the dry run's message); a blocked plan; the plan file missing, from another version, or a symbolic link (skipped where the OS refuses to make one), each with the dry run's own `error:` line; the call's zip a symbolic link, and a zip that can't be read (skipped on Windows); stdin over the limit, and no stdin (the input function returns null); the fake target read throwing an exception type that `deploy apply` doesn't expect; and the deadline, with a deadline of one second set through the seam and a fake read that waits on an event the test releases after the gate returns. Each case asserts exit 2, empty stdout, the expected line, the right guidance line last, and that stderr holds neither the environment value, the temp folder nor the command text. In none of them does the fake `pac` get `solution import`.
7. A theory over the cases of 4 to 6 checks that the exit code is 0 or 2 and stdout is empty. `deploy gate` with no argument exits 2.

`ProgramTests` (the built assembly as a child process, as the version test does): a non-import call on stdin gives exit 0 and empty stdout and stderr; an import call that names a zip file the test writes, with a plan file that is missing, gives exit 2 with the dry run's line, empty stdout and a stderr that ends with the guidance line; empty stdin gives exit 2.

`HandoffDocTests`: the two hook tests are replaced. The groups' matchers are `mcp__pac-cli__solution_import`, `Bash` and `PowerShell`; each command is `powerops deploy gate "$(git rev-parse --show-toplevel)/deploy/powerops-plan.json" || exit 2`, with `onFailure: "block"` and `timeout: 600`; and the page has no `grep` hook. The test that checks the command names a page uses then sees `deploy gate` in the command tree.

**The HANDOFF section ("Gate Microsoft's imports").**

- The Claude Code example has three groups, one per matcher (`mcp__pac-cli__solution_import`, `Bash`, `PowerShell`), each with the command above, `onFailure: "block"` and `timeout: 600`. There is no `grep` before it: the gate reads every shell command itself, so its markers are tested in one place. The plan file is the one plan the hook names, `deploy/powerops-plan.json`. Separate groups need no regular expression in a matcher, which keeps the Codex copy the same.
- Codex: the same groups, with `mcp__pac_cli__solution_import`, without `onFailure` and without `PowerShell`. `|| exit 2` turns a missing `powerops` or runtime into a block. A hook timeout, a shell that can't start, or a shell that can't parse the line (fish before 3.4, Windows PowerShell 5.1) lets the call through there. On Windows Codex runs hooks with `cmd.exe /C`, so write the plan file's full path instead of `$(git rev-parse ...)`.
- The page keeps exactly one fenced `json` block, the Claude Code example, which `HandoffDocTests` read; the Codex differences stay in prose.
- The first matcher carries the name you gave pac's MCP server. A server with another name needs that name in the matcher, or its `solution_import` runs unchecked; the gate itself checks any server's `solution_import`.
- What a pass means: the call is the plan's first import, with the zip and settings file bytes the plan holds, into the environment the plan was made for, with no flag that changes the import, and the dry run of `deploy apply` passes. It is not the user's yes: ask first, as for `deploy apply --apply`. So an import into Dev is blocked when the plan was made for Test.
- When to turn the hooks on: an import into Dev through a Microsoft tool is blocked too, because the plan is for Test, and so is create-site's template import. Add the hooks when you start step 7, and remove them when you go back to building in Dev. A plan of several solutions imports with `deploy apply`, because the first import changes the target and the next call fails the dry run.
- What it doesn't do: model-apps' `solution-settings.js import`, power-pages' `import-solution` and `deploy-pipeline`, create-site's template import, `pac package deploy`, `pac solution upgrade` and any import in PowerShell are always blocked. A script file that runs `pac` itself, a command built from variables (`$PAC $s import`), another tool that runs commands (an MCP terminal server, Codex's `write_stdin` into a running shell), and a server whose name the matcher doesn't name get past it. A mention of an import in a command, such as `grep "pac solution import" docs` or a commit message, is blocked; reword it. An input over 1 MB is blocked whatever the tool. The only redirection the import may carry is `2>&1` (or `>&2`). On Windows, quote `C:\...` paths. Both clients' warnings about hooks stay, and so does the deny alternative.
- Sources: the existing line, plus `codex-rs/hooks/src/schema.rs`, `codex-rs/core/src/tools/handlers/mcp.rs` and `codex-rs/core/src/tools/handlers/unified_exec/exec_command.rs`, the power-platform-skills scripts `solution-settings.js` and `import-template-solution.js`, and the Learn page "pac package". The date is the build's.

**`powerops-implement` step 7.** It doesn't change, and `SkillsCommandTests` stay as they are. Step 7 already asks before every import and sends promotion through `deploy plan` and `deploy apply`, not through Microsoft's import tools. The gate enforces that in repos that set the hooks, and its block line tells the agent not to retry another way. The skill is also installed in repos without the hooks, where naming the gate would mislead.

## Screens

None.

## Environments and route

- The build and every test: offline, with fakes. No tenant, `pac` or `az` call, and nothing is imported.
- Steps 1 and 7 run the real clients outside the repo, with per-run config only and a toy MCP server that has no `pac`. In Step 1 every hook call blocks; in Step 7 the shell import it sends names a zip that doesn't exist and a fake environment id, so even a hook that let it through would import nothing. A Codex run uses the user's login, so it needs the user's yes in that session.
- There is no live run against a tenant: the gate's tenant read is `deploy apply`'s dry run, whose live check passed in M10.

## Solution and publisher

None.

## Constraints

- No new package. `System.Text.Json`, `System.Security.Cryptography` and `System.CommandLine` cover it.
- AGENTS.md: read-only. The gate never imports and never writes a file. `deploy apply --apply` is never run.
- Accepted risk (decisions Q7): nothing ties the plan file at the hook's path to a person's review. An agent that can run `deploy plan --save` can write a new plan file and then pass the gate with an import that matches it. The gate closes the gap that Microsoft's tools skipped the plan check; the user's yes stays the human step, as for `deploy apply --apply`.
- Commit `6b7ccfb` (the HANDOFF section and its tests) is on branch `claude/optimistic-burnell-6c87ae`, not on `main`. Step 2 brings it into the build branch.
- No version or changelog edits; no push or pull request unless asked. Conventional Commits, no attribution lines.

## Assumptions

- Claude Code's `tool_input` for an MCP tool is the call's arguments, as the docs say for PostToolUse ("`tool_input`, the arguments sent to the tool"). Step 1 checks it and where `mcp_server` sits; the gate ignores the key either way.
- In Codex 0.163.0 only `exec_command` builds a hook payload for a shell. If another shell tool runs without one, its imports get past the gate. Step 1 and the code at `rust-v0.163.0` are the evidence; the HANDOFF section keeps Codex's warning.
- The `pac` flag names compare ordinally, so `--Path` blocks even if `pac` would accept it. A form that `pac` accepts and the gate doesn't is a block, never an allow.
- The bytes of the zip and the settings file can change between the gate's hash and pac's read. The window is short, and anyone who can write the deploy folder is outside the threat model, as in M19.
- Two calls that pass at once (a client running tool calls in parallel) both import the plan's first zip. The second import is the same version of the same zip.
- Starting `powerops` for every shell command adds the .NET start-up time to each one. Step 1 measures it; it is not a reason to put `grep` back in front.
- Every way the gate can fail to run writes something on stderr (the shell's "not found", the runtime's error), which Codex needs for `|| exit 2` to block. Step 7 checks the missing-command case.

## Open questions

## Questions for your admin

## Steps

1. Hook input check, outside the repo: a toy stdio MCP server whose one tool, `solution_import`, only logs; per-run settings with a `PreToolUse` hook that saves stdin to a scratch file and exits 2 with a reason; `claude -p` with `--mcp-config`, `--strict-mcp-config`, `--settings` and `--no-session-persistence`, asked to call the tool and to run a Bash command. Record the `tool_input` shapes, where `mcp_server` sits, and the start-up time of `powerops --version`. Codex with the same toy, only after the user's yes in that session; else marked not verified, and the code reading stands. Scrub ids and paths before the shapes go into tests.
2. Bring commit `6b7ccfb` into the build branch as its own commit (a cherry-pick, unless `main` has it by then).
3. `docs:` this plan, its decisions and review, signed off; BUILD-PLAN gets the M21 row in the Phase 5 table (the heading and its `#phase-5-agents-m18m20` anchor stay; the section's first paragraph says M21 was added on 2026-10-28), build-order item 46, decision 30 (a command that a hook runs exits only 0 or 2, and M21's label is v0.20.0), and the title's range becomes v0.2 to v0.20.
4. `feat(cli):` item 46: the shared dry run in `DeployCommand`, `ImportCall` with tests 1 to 3, `deploy gate` with the input function, the deadline and tests 4 to 7, the `ProgramTests` cases, the AGENTS.md clause and the README usage lines.
5. `docs:` the HANDOFF section and `HandoffDocTests`. HANDOFF step 8's last sentence and the README's line from `6b7ccfb` change with it: pac's MCP `solution_import` and a plain `pac solution import` can run behind the gate, and the other Microsoft imports are always blocked there.
6. Run `dotnet build --no-incremental` (0 warnings), `dotnet test`, `dotnet format --verify-no-changes`, `dotnet pack src/PowerOps.Cli -c Release -o artifacts` and `powerops plan status .`. Run the golden recipe and read the diff: no golden should change, because `deploy apply`'s output stays the same.
7. The HANDOFF example in a client, outside the repo: a scratch git repo with no plan file, the example's hooks in per-run settings, the packed `powerops` on PATH, and the toy server named `pac-cli`. In `claude -p`: a Bash command with no import runs; a Bash `pac solution import` with literal absolute paths (a zip that doesn't exist, a fake environment id) is blocked and its reason reaches the model; the toy `solution_import` is blocked; with `powerops` off PATH, the toy call is blocked. Codex only after the user's yes, as in Step 1.
8. `docs:` BUILD-PLAN: the M21 row marked built with a link to this plan, and its line in the verification record (Steps 1 and 7, and what is not verified, including the allow path in a client, which needs a tenant read); this plan's status.
