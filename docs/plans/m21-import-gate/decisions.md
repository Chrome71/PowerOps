# Decisions: m21-import-gate

## Round 1

### Q1. What is the command, and where does it live?
1. `powerops deploy gate <plan-file>`, beside `deploy plan` and `deploy apply`: it reads the hook input on stdin and calls `deploy apply`'s dry run in-process; the hook runs it for every shell command, with no `grep` in front (recommended)
2. A top-level `powerops hook` command that takes the event name
3. In your own words
Answer: `powerops deploy gate <plan-file>`, beside `deploy plan` and `deploy apply`: it reads the hook input on stdin and calls `deploy apply`'s dry run in-process; the hook runs it for every shell command, with no `grep` in front. The check is `deploy apply`'s, so it belongs under `deploy`, and the dry run's code is shared rather than run through a second process. With the markers in PowerOps, one place holds them and its tests; the cost is a .NET start per shell command, which Step 1 measures.

### Q2. Which import does the gate let through?
1. Only the plan's first import as `deploy apply` would run it: the same zip and settings file bytes, `--stage-and-upgrade` exactly when the plan stages a managed upgrade, the environment the dry run resolves to the plan's target, and no flag other than `--async` and `--max-async-wait-time` (recommended)
2. Any zip of the plan, with any flags
3. In your own words
Answer: Only the plan's first import as `deploy apply` would run it: the same zip and settings file bytes, `--stage-and-upgrade` exactly when the plan stages a managed upgrade, the environment the dry run resolves to the plan's target, and no flag other than `--async` and `--max-async-wait-time`. The reviewed plan predicts `deploy apply`'s import. A flag such as `--force-overwrite`, `--activate-plugins` or `--import-as-holding` changes what the import does, so the plan doesn't cover it. A later zip would import out of order, and after the first import the target has changed, so the next call fails the dry run anyway. Dataverse-skills' documented form carries `--activate-plugins` and a relative path, so it is blocked, and the block line says what to drop.

### Q3. What does the gate do with a call it can't read?
1. Block it (exit 2), with a line that says what to write instead: one plain `pac solution import` command, literal absolute paths, the environment by id or https URL. The routes whose zip or target is not on the command line always block (recommended)
2. Let it through with a warning on stderr
3. In your own words
Answer: Block it (exit 2), with a line that says what to write instead: one plain `pac solution import` command, literal absolute paths, the environment by id or https URL. The routes whose zip or target is not on the command line always block. The user asked that PowerOps review each use, and a call it can't read can't be reviewed. Paths must be absolute because Codex doesn't send the shell tool's `workdir`, and Claude Code's `cwd` follows `cd`. power-pages' `import-solution` sends the zip in base64, `deploy-pipeline` imports through a pipeline stage, and create-site imports a template no plan holds with `--force-overwrite`, so these three are always blocked, and the HANDOFF section says to turn the hooks off for create-site. PowerShell imports are blocked too, because the gate reads POSIX syntax only. Changed after the review (R6, R7): model-apps' `solution-settings.js import`, `pac package deploy` and `pac solution upgrade` always block as well. The model-apps script imports through a request that a vendored SDK builds, which was not read, so its options can't be checked; and only `pac solution import` is left to parse.

## Round 2

### Q4. Which exit codes, and what about a slow check?
1. 0 allows and 2 blocks, for everything else too (usage, unreadable input, internal errors); a deadline of 300 seconds blocks a check that runs too long (recommended)
2. 0/1/2/3 as the other commands, with 1 for a block
3. In your own words
Answer: 0 allows and 2 blocks, for everything else too (usage, unreadable input, internal errors); a deadline of 300 seconds blocks a check that runs too long. Both clients let a call through on any other exit code and on a timeout; Claude Code blocks those only with `onFailure: "block"`, and Codex has no such setting (`codex-rs/hooks/src/events/pre_tool_use.rs` at `rust-v0.163.0`). Codex also needs a non-empty stderr with exit 2. Each tenant read has a 30-second limit, but a plan of several solutions makes many reads, so a slow target could pass the clients' 600-second default. The deadline is half of it. AGENTS.md's 0/1/2/3 doesn't fit the hook protocol, so BUILD-PLAN gets decision 30.

### Q5. Does `powerops-implement` step 7 change?
1. No. Step 7 already asks before every import and sends promotion through `deploy plan` and `deploy apply`; the gate enforces that in repos with the hooks, and its block line tells the agent not to retry another way; `SkillsCommandTests` stay as they are (recommended)
2. Add a sentence that names `powerops deploy gate`, pinned in `SkillsCommandTests`
3. In your own words
Answer: No. Step 7 already asks before every import and sends promotion through `deploy plan` and `deploy apply`; the gate enforces that in repos with the hooks, and its block line tells the agent not to retry another way; `SkillsCommandTests` stay as they are. The gate is a guard for the cases where a Microsoft tool runs anyway, not a second route. The skill is installed in repos without the hooks too, where a sentence about the gate would mislead.

### Q6. One plan file, or one per environment?
1. One plan file, the one the hook names: an import into any environment other than the plan's target is blocked, so Dev imports through a Microsoft tool need the hooks off or a plan for Dev in that file (recommended)
2. The gate takes several plan files and allows an import that any of them covers
3. In your own words
Answer: One plan file, the one the hook names: an import into any environment other than the plan's target is blocked, so Dev imports through a Microsoft tool need the hooks off or a plan for Dev in that file. That is the brief (`deploy/powerops-plan.json`), and it is what makes an import into Dev differ from one into Test. Several plan files would be a later change if users ask for it. After the review (R11): the HANDOFF section says to add the hooks at step 7 and remove them when building in Dev again, and the plan's open question asks the user whether a later option should let imports into a listed Dev environment through. The user answered it on 2026-10-28: keep Dev blocked (Q8).

## Round 3

### Q7. Does a pass need proof that the plan file was reviewed, or the user's yes?
1. No: a pass shows that the call matches the plan file at the hook's path and the live target; the user's yes stays required (AGENTS.md, `powerops-implement` step 7, the client's prompt), and the missing tie to a review is an accepted risk (recommended)
2. The gate also requires the plan file to be tracked in git and unchanged from `HEAD`
3. In your own words
Answer: No: a pass shows that the call matches the plan file at the hook's path and the live target; the user's yes stays required (AGENTS.md, `powerops-implement` step 7, the client's prompt), and the missing tie to a review is an accepted risk. Raised by the review (R2). An agent that can write and commit files can also commit a new plan file, so option 2 adds a rule without adding a control. The gap the gate closes is that Microsoft's tools skipped the plan check entirely; the human step for an import stays the user's yes, as it is for `deploy apply --apply`. The Goal, the HANDOFF section and Constraints say so.

## Round 4

### Q8. Should imports into a listed Dev environment pass without a plan?
1. Keep Dev blocked: the hooks are on for steps 7 to 9 only, and no `--allow` option is built (recommended)
2. Add an `--allow <environment id>` option that lets imports into a listed Dev environment through, compared by id or host, with its tests
3. In your own words
Answer: Keep Dev blocked: the hooks are on for steps 7 to 9 only, and no `--allow` option is built. Asked of the user in this session after the review (R11), and answered by the user.

## Log

- plan started 2026-10-28 from the brief of the session that wrote the HANDOFF section "Gate Microsoft's imports" (commit `6b7ccfb`): a command that a `PreToolUse` hook calls, which matches the call's zip and target to the reviewed plan; build nothing yet; ready for sign-off when reviewed
- `powerops plan facts` not run: the build needs no environment; `environment` stays null
- depth: quick (from `m19-deploy-apply`)
- the user delegated their parts on 2026-10-22: this session answers with the recommended options. A run on the user's Codex login still needs their yes (Step 1). Sign-off is left to the session that asked for the plan, or the user
- read on 2026-10-28, by two powerops-researcher agents (Sonnet) and this session: the Claude Code hooks docs ("Common input fields", "PreToolUse input", "Exit code output", `onFailure`); Codex at `rust-v0.163.0` (`hooks/src/schema.rs`, `hooks/src/events/pre_tool_use.rs`, `hooks/src/engine/command_runner.rs`, `hooks/src/engine/discovery.rs`, `config/src/hook_config.rs`, `core/src/tools/hook_names.rs`, `core/src/tools/handlers/mcp.rs`, `core/src/tools/handlers/apply_patch.rs`, `core/src/tools/handlers/unified_exec/exec_command.rs`); power-platform-skills at `0a7bf47` (model-apps `scripts/solution-settings.js` and the genpage skill; power-pages `import-solution`, `deploy-pipeline`, `create-site`, `scripts/encode-solution-file.js`, `scripts/import-template-solution.js`); Dataverse-skills at `108370e` (`dv-solution`, `templates/deploy.yml`); the Learn pages "pac solution" and "pac pipeline"; and the input schema of `solution_import` as pac's MCP server lists it to Claude Code. The researchers had no clone, so line numbers are approximate, and the sweep of the other skills in both repos was not complete
- not verified: whether Claude Code's `mcp_server` sits in `tool_input` or beside it (Step 1), how power-pages sends its POSTs, and whether a Codex shell tool other than `exec_command` runs without a hook payload
- approved without: users, constraints (the brief covers them)
- approved (2026-10-28)
- fresh review (general-purpose agent following `powerops-review-plan`, Sonnet, fresh context) 2026-10-28: 3 High, 9 Medium, 5 Low. All 17 fixed in the plan text, with resolutions in `review.md`. Answers changed: Q3 (the model-apps script, `pac package deploy` and `pac solution upgrade` always block) and Q6 (the hooks are on for steps 7 to 9 only, and an open question for the user); Q7 added (R2). The reviewer contacted no tenant
- re-check of the fixes (powerops-reviewer, Sonnet, fresh context) 2026-10-28: R1 to R17 fixed (R10 partly, closed by R23); new: 0 High, 3 Medium, 6 Low (R18 to R26), all fixed in the plan text. No answer changed. No High or Medium finding is left open. Ready for sign-off; the plan has one open question for the user (imports into Dev)
- 2026-10-28: the user answered the open question (Q8): keep imports into Dev blocked. The open question was removed from `plan.md`; no other text changed. Open questions: 0. Ready for sign-off
- signed off 2026-10-28 by `powerops plan sign-off`, on the user's yes in this session, with no finding open and 0 open questions
- 2026-10-29: build approved by the user in the master session ("Yes, both": start M21's build, bring in 6b7ccfb, and "the Codex hook check has my yes"); relayed to this session. Branch rebased onto `main` at 0293c1f with no conflict; `plan status` kept the sign-off (same hash). Status `implementing`
- 2026-10-29, details beyond the plan text, from `main` having moved on: Phase 6 took BUILD-PLAN decisions 30 to 38, so M21's decision (a command that a hook runs exits only 0 or 2) is 39, and Phase 6's numbers stay; the command's usage lines go to `docs/COMMANDS.md`, where AGENTS.md now keeps command detail, not to the README; `main`'s short README already links the hand-off page, so `6b7ccfb`'s README sentence was left out of the cherry-pick, and Step 5 has no README line to change
- 2026-10-29, Step 1 done: Claude Code 2.1.312 (`claude -p`, haiku, per-run `--mcp-config`, `--strict-mcp-config`, `--settings`, `--no-session-persistence`) and Codex 0.163.0 on the user's login (`codex exec`, read-only sandbox, `--ephemeral`, per-run `-c` for the toy server and the hooks, and `--dangerously-bypass-hook-trust`, which ran only that per-run test hook) each sent pac's MCP call and a Bash call to a hook that recorded stdin and exited 2; the toy server got no `tools/call`. Claude Code's `mcp_server` (`name`, `source`) sits beside `tool_input`, not in it, and `tool_input` is exactly the call's arguments; its Bash `tool_input` held `command` and `description`. Codex sent `mcp__pac_cli__solution_import` with the bare arguments and `Bash` with `{command}` only. `powerops --version` starts in about 25 ms. The user's config was not changed
- 2026-10-29, Step 2 done: `6b7ccfb` cherry-picked as its own commit (README left out, see above)
- 2026-10-29, Step 3 done (`b9a69cf`): the M21 row, item 46 and decision 39 in `docs/BUILD-PLAN.md`
- 2026-10-29, Step 4 done (`8a8ebff`), written by a powerops-implementer agent (Sonnet) and checked by this session. Details beyond the plan text: `deploy apply` and the gate share one dry-run method; the test fakes moved to `DeployFakes.cs` (`DeployTestBase`) so both command test classes use them; the call's zip and settings file are read whole within `BoundedRead`'s caps and hashed, not streamed; a duplicate JSON property anywhere in the input blocks; an unclosed quote blocks only in a command that holds an import marker; a `pac` command whose subcommand is not `solution import` but holds a marker (`pac --verbose solution import`) blocks; two block lines more than the plan names (`<flag> must be a string`, `--stage-and-upgrade must be "true"`). This session's check found that the word splitter took a backslash before `\r\n` as a line join and a carriage return as a blank, so an `--environment` on the next line was read by the gate but would not reach pac, and that `$'pac'` was not read as `pac`; both fixed in the same commit, with tests
- 2026-10-29, Step 5 done (`323814f`): the HANDOFF section's hooks run the gate in both clients' form, `HandoffDocTests` checks them, and the "pac package" Learn page was added to its Sources
- 2026-10-29, Step 6 done: `dotnet build --no-incremental` (0 warnings), `dotnet test` (2642 tests, 0 failed, 4 skipped), `dotnet format --verify-no-changes`, `dotnet pack` and `powerops plan status .` (0 violations) are clean
- 2026-10-29, Step 7 done, with the packed CLI, the toy server and a missing zip, so nothing could be imported: Claude Code 2.1.312 and Codex 0.163.0 ran a plain shell command and blocked a Bash `pac solution import` and the MCP `solution_import` with the gate's reason, and blocked with "command not found" when `powerops` was off PATH. Codex ran the hooks with bash even with `SHELL` set to fish, so the hook line was also run directly in sh, bash, zsh and fish: exit 0, 2 with the reason, and 2 off PATH in each. Details in the BUILD-PLAN verification record. Not verified: the allow path in a client (it needs a tenant read), Windows and macOS
- 2026-10-29, Step 8: BUILD-PLAN marks M21 built and records Steps 1 and 7; status `done`
- 2026-10-29: rebased onto `main` at 89c1540 (M29, Dependabot #32 and #34) with no conflict; `plan status` kept the sign-off
- 2026-10-29, fixes after the master session's two independent reviews of `0db4d9b` (correctness and security, Sonnet; 1 High, 6 Medium, several Low). Each is a correction beyond the plan text, which stays as signed off. The code was changed by a powerops-implementer agent (Sonnet) and checked by this session, which wrote the docs:
  - a FIFO or a device passed as the call's zip or settings file (High): the gate hashes only a regular file (seekable, the bytes read equal its length, then end of file), and `deploy apply`'s check before each import uses the same read
  - the zip and settings file are hashed again just before the gate allows a call, and a change blocks. `pac` opens the file after the gate exits, so this is a speed bump against an agent that runs as the same user, not a guarantee; HANDOFF says so
  - in the MCP form `--async` is only `"true"` or `"false"`, and a path with a space, a control character or a leading `-` blocks, because how pac's MCP server builds its command line is not known. A Windows path with a space is blocked there; the shell form still takes it in quotes
  - an `--environment` URL must be the https URL of a Dataverse host (the check that drift's Web API reads use), so a call can't make `pac org who` contact another host
  - Claude Code's `Monitor` tool is read like `Bash` (its `ws` form runs no shell) and gets a hook group in HANDOFF
  - added to Q3's always-blocked routes: `pac pcf push`, `pac application install`, `pac code push`, and `Import-CrmSolution` as a marker
  - a Bash command with a `$'...'` word that holds a backslash blocks as unreadable, whatever else it holds, because the escapes can spell any command
  - a plan-file argument that starts with `-` blocks, and the gate's `-h`, `--help` and `-?` no longer print help with exit 0 (a hook with them would let every call through): alone they exit 2, after a plan file they are ignored. So the gate exits only 0 or 2 with nothing on stdout, and `deploy gate --help` prints no help; COMMANDS.md has it
  - `--path=/x.zip` now says to give the flag and its value as separate words
  - HANDOFF's hooks use `$CLAUDE_PROJECT_DIR` instead of `$(git rev-parse --show-toplevel)`. In a run of Claude Code 2.1.312 (`claude -p`, haiku, a per-run hook that only logged), the hook ran in the folder of the agent's last `cd`, and `git rev-parse` followed a `git init` in a subfolder, while `$CLAUDE_PROJECT_DIR` stayed the start folder. In a run of Codex 0.163.0 (`codex exec`, read-only, `--ephemeral`, the same logging hook), hooks ran in the folder Codex started in, after a `cd` and with another `workdir` alike, and `CLAUDE_PROJECT_DIR` was empty, so HANDOFF tells Codex users to write the plan file's full path
  - `HandoffDocTests` reads the page with CRLF turned into LF, so a Windows checkout passes
  - tests: the `codex-bash` case sends Codex's real input shape, and a gate test shows the first import comes from the fresh plan, not the plan file's `solutions` array. The FIFO test runs on Linux only, because opening a FIFO read-write without blocking is Linux behaviour
- 2026-10-29, fixes after the master session's security re-review of `14e5882` (three small gaps):
  - MCP `--path` and `--settings-file` values hold only letters, digits and `/ \ : . _ - ~`, and don't start with `-`, so `;`, `$(...)` and quotes block. `~` is added to the reviewer's list for Windows short names such as `RUNNER~1` (the path is fully qualified, so it can't lead, as in the shell form); letters include non-ASCII ones
  - `pac catalog install` and `pac app-management install-application-package` (both listed by pac 2.13.1) are added to Q3's always-blocked routes. The gate blocks the import routes it knows: a new pac command that installs or imports passes until the gate gets a route for it. An allow-list of pac subcommands would block everyday commands (`solution export`, `unpack`, `check`) and change the signed-off design, so it was not done; HANDOFF and COMMANDS.md say this
  - the gate's help aliases `/h` and `/?` exit 2 too
  - HANDOFF's "What it doesn't do" names `printf` with escapes piped to `sh`
