plan-hash: 03b18f888e3569e5914c61123d48c36f5e534acd84a287f8f0cbdf401f0fd8e7
model: claude-sonnet-5-5
fresh-context: yes

The plan-hash was updated after the text fixes for R1 to R13 below (the practice of the M7 to M19 reviews). Reviewed hash: 3739ebfc3ba6a60657c534bb154f004b6eabfec3123cda5a4ad894ad220cb99b. The reviewer (powerops-reviewer, Sonnet, fresh context) could not write files, so the planning session wrote this file from its reply and added the resolutions. The fixes changed Data (Codex's reduced environment, the descriptions), Automation (the test, the Usage paragraph, the README points), Environments and route (the setup checks, the scripts, the run steps, the evidence, the clean-up) and Step 7. The reviewer contacted no tenant and ran no client.

The plan-hash was updated again on 2026-10-28 for the manual run's finding: Codex's reduced environment kept `pac` from its saved sign-in on Linux, so the Codex entry passes `DBUS_SESSION_BUS_ADDRESS` and `XDG_RUNTIME_DIR` in `env_vars` (R7's suggested change, made after the run showed it was needed). Signed-off hash before that change: 13915f6d9416fdd5200697c32270464365a5351ef4ff7fd886f81f9fe11e2d26.

Most of the Power Platform checklist does not apply: this is a CLI and plugin milestone with no solution. Items 1 and 2 (solution boundary, publisher, connection references, environment variables), 3 and 4 (connectors, licences, DLP), 5 (route: only the manual run reads Test, and it imports nothing), 6 (managed or unmanaged: the run reuses the managed seed zip of M9 and M10), 7 (volume, delegation) and 8 (security roles: the run uses the user's own `pac` and `az` sign-in). Items 9 and 10 drive the findings. Open questions: 0. No URL, GUID, email or person's name in the plan or `decisions.md`.

## Findings

### High

No findings.

### Medium

#### R1. The run's start line breaks in fish
- Area: 9 (testability of the manual run)
- Finding: Step 1 used `env PATH=<scratch>/tools:$PATH claude ...`. In fish, the user's Terminal shell, an unquoted `x:$PATH` expands to one word per PATH entry, so `env` gets many `PATH=` words and the client or `powerops` is not found; this machine's PATH also holds a folder with a space.
- Suggested fix: Lines that work in fish and bash (quoted), a note that the sheet is for fish, and a `command -v powerops` check first.
- Resolution: fixed (the run starts two bash scripts with `bash <scratch>/run-claude.sh` and `run-codex.sh`, which set PATH, print `command -v powerops` and start the client; Setup 5 and run step 1)

#### R2. The permission prompt and existing config can turn the run into a persistent allow or a pre-answered form
- Area: 9 (safety of the manual run)
- Finding: Claude Code's "always allow" writes a rule for `deploy_apply` into settings the user keeps, and Codex has "don't ask again". The plan didn't check that nothing answers the form or allows a tool before the run.
- Suggested fix: One-time Allow only; check the settings for `Elicitation` hooks and `mcp__` rules and the plugins' hooks before the run; check for new rules after.
- Resolution: fixed (run step 4: allow once, never "always allow" or "don't ask again"; Setup 6: the settings and plugin hooks checked on 2026-10-28, none found, hashes taken before and compared after; Clean-up)

#### R3. The Codex steps depend on the approval mode picked at first run, and "cancelled" is weak evidence
- Area: 9 (evidence for the done-when in Codex)
- Finding: A fresh `CODEX_HOME` asks for its mode; a `never` policy declines the form unseen. In Codex "declined" can only come from policy, and "cancelled" also comes from an interrupted turn or a cancelled call.
- Suggested fix: Name the mode; record a Codex "declined" as "not shown"; have the user note that the form was on screen, that they pressed Esc once, and that no turn was interrupted; keep Codex's log until the record is written.
- Resolution: fixed (run steps 1 and 5, Clean-up)

#### R4. The offline comparison names the wrong reference
- Area: 9 (testability)
- Finding: `drift-seed.json` comes from fake live reads, not the snapshot files, and `analyze-fixtures.json` holds relative paths, so the goldens are not the reference for the run's inputs.
- Suggested fix: Run the packed CLI on the same absolute inputs before asking the user, and put the counts in the prompt sheet.
- Resolution: fixed (run step 3 lists the CLI's own results on the same inputs, run on 2026-10-28: 11 solutions, 0 errors, 36 warnings; 1 added and 1 changed; 4 of 4 passed; drift in 3 items)

#### R5. "In Claude Code both would run" contradicts the Claude Code docs, and the run does not check the server name
- Area: 9 (README accuracy and the run's evidence)
- Finding: Claude Code connects a server defined in more than one place once, from the highest-precedence source, and matches plugin servers by endpoint, so with both the user's own entry wins and the plugin's server isn't started. An existing own entry would hide `--plugin-dir` in the run.
- Suggested fix: Reword the README; check that no own `powerops` entry exists; require `/mcp` to show `plugin:powerops:powerops`.
- Resolution: fixed (README point 3 and the README; Setup 6: no own `powerops` entry at any scope; run step 2 requires `plugin:powerops:powerops` and `mcp__plugin_powerops_powerops__<tool>`)

#### R6. The claim about Codex's reviewer and full-access mode answering the form does not match the code at the pinned tag
- Area: 9 (README and plan accuracy)
- Finding: `can_auto_accept_elicitation` accepts only a form without properties, and the reviewer handles only forms whose `_meta` marks an approval request, so `deploy_apply`'s form goes to the user; a `never` policy declines it. The plan, the draft README and the M19 record said otherwise.
- Suggested fix: Reword without the mechanism; correct the M19 record line in Step 7.
- Resolution: fixed (README point 5 and the README say not to set up anything that answers forms and what Codex 0.163.0 does; this session read both functions; Step 7 corrects the M19 line and the research basis)

#### R7. Codex starts plugin servers with a reduced environment
- Area: 9 (testability of the Codex tenant steps)
- Finding: `create_env_for_mcp_server` passes a stdio server only a short list of variables, plus `env_vars`; `pac` and `az` may need more, so the tenant tools could fail in Codex only.
- Suggested fix: Say how a `pac` or `az` failure in the Codex run is read and what would change; optionally a cheap check before the tenant steps.
- Resolution: fixed (Data: this session ran the packed `powerops`, `pac auth list` and `az account show` under `env -i` with only those variables, and all three ran; a failure in the run is checked against this first, and the change would be an `env_vars` list)

### Low

#### R8. Claude Code moves long MCP calls to the background
- Area: 9 (run evidence) and the README Timeouts paragraph
- Finding: A main-conversation MCP call still running after two minutes becomes a background task whose result arrives later; a call waiting on a form is not moved. Step 5 could call such a call hanging.
- Suggested fix: Look in `/tasks` before calling it hanging; one README sentence.
- Resolution: fixed (run step 5; README point 6 and the README)

#### R9. Mismatches between the draft and the plan
- Area: 9 (plan versus build)
- Finding: the Usage paragraph kept fewer lines than the plan said; the Codex test asserted the literal path instead of resolving it; the README's first sentence said every tool returns JSON; the marketplace description still says "planning skills".
- Suggested fix: Align each, and record the marketplace choice.
- Resolution: fixed (the plan says the Usage paragraph keeps stdio and exit codes and the rest moves to the section; the test resolves the manifest's value against the plugin folder; the README sentence names the file text of `baseline` and `settings_generate`; the marketplace description stays, recorded in Data)

#### R10. The run leaves the Test identifier in session history
- Area: 9 (safety of the manual run)
- Finding: The Claude Code session keeps its history, which will hold the Test URL and the plan's results; Codex's log goes with the throwaway home.
- Suggested fix: Tell the user and offer to remove the transcript; keep Codex's log until the record is written.
- Resolution: fixed (Clean-up)

#### R11. README claims that go further than what was checked
- Area: 9 (README accuracy)
- Finding: the server starts only where `powerops` is on the client process's PATH; Codex's paused clock was read in code, not observed; Codex runs `plan_facts` and `drift` against an environment without asking.
- Suggested fix: One sentence on PATH and `/mcp`; "Codex 0.163.0 stops..."; say Codex doesn't ask before the tenant-reading read-only tools.
- Resolution: fixed (README points 2, 4 and 6 and the README)

#### R12. Run steps with unverified assumptions or no negative check
- Area: 9 (testability)
- Finding: `claude plugin details` listing the server was assumed; the M9 and M10 files' presence wasn't checked; no step shows that `deploy_plan` without `save` writes nothing.
- Suggested fix: Mark `details` as expected and use `/mcp`; confirm the files before asking; add the no-`save` check.
- Resolution: fixed (Setup 2 records the observed `details` and `claude mcp list` output; the deploy folder holds the 1.0.0.2 zip and its settings file, checked on 2026-10-28; run step 4 starts with `deploy_plan` without `save` and a listing check)

#### R13. Areas the decisions log never asked about
- Area: 10 (unanswered areas)
- Finding: Windows and macOS (the bare command, the Windows shim, Codex's Windows variable list), Codex's startup timeout (30 seconds in its code, 10 in the research basis), and Claude Code's per-server `timeout` key and its idle window while a form is open are not raised.
- Suggested fix: A Log line saying they are not verified and not changed, with the reason; correct the startup default in Step 7.
- Resolution: fixed (Log line in `decisions.md`; Step 7 adds Codex's defaults from its code to the research basis)
