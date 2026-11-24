plan-hash: 155d6386e2d0f830b795263caf03007f2a6059b013bc13e82d2c0cfc6dcc2a2a
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: the plan's `environment:` is null and nobody confirmed a signed-in `pac`, so `powerops plan facts` was not run. Second pass, 2026-11-09: the revised plan of `7b86f30` (commits `1d9df0a` and `7b86f30`) was checked against the repo; its cited lines are those of `278b136`, and only the plan folder differs. Each first-pass finding's Resolution is set against the revised plan, and R9 is new. Checked by running the built gate (`src/PowerOps.Cli/bin/Debug/net10.0/powerops.dll`, with no `pac` on `PATH`) on the README's probe command form with an absolute stand-in path: exit 2, `error: deploy gate: --environment is missing`. Third pass, 2026-11-09: only R9's answer in row 6 (commit `8910fd3`) was checked, with any new problem it causes; no new finding. Open questions: 0; questions for your admin: 0.

## Findings

### High

#### R1. The warning prints the deploy folder as given, which breaks the "folder path never printed" rule and an existing test
- Area: 9. Testability (and the output rule the plan cites)
- Finding: Row 1 prints `<folder>` "as given on the command line" and in the same sentence says "The full path is never printed (`:140`)". Those conflict: `DeployCommand.cs:140` says the folder's full path is never printed, and a folder given as a full path (as agents and the tests give it) would be printed whole. `DeployCommandTests.The_target_value_and_the_folder_path_appear_in_no_output_and_no_file` passes `Folder` as a full temp path, saves with `--save` into the sibling `Out` folder, and asserts that stderr doesn't contain the temp root, so that test would fail with the warning as specified. The Constraints line "no golden or schema moves" doesn't cover it, and the plan doesn't list any existing test that must change.
- Suggested fix: Name no path in the warning (for example `warning: the plan file is not saved in the deploy folder; deploy apply reads ...`), or at most the folder's last name through `Clean`. Keep the hygiene test as it is, and add a row 1 test case that the warning holds neither the temp root nor the target value.
- Resolution: resolved (2nd pass): row 1 and Done-when 1 now write a fixed line that names no path, cite the rule at `:140`, and keep `The_target_value_and_the_folder_path_appear_in_no_output_and_no_file` unchanged and green.

### Medium

#### R2. A marker left from an earlier probe makes a second probe read "inactive"
- Area: 9. Testability
- Finding: Row 5 step (f) reads "no `gate-probe.marker`" as "the hook is active", and tells the person to restart and "probe again" when it isn't. The stand-in appends to the marker and nothing removes it, so after a failed first probe the marker is still there on the second probe even if the hook is now active. Step (h) deletes the stand-in but not the marker.
- Suggested fix: In the README, delete `gate-probe.marker` before each probe (step (e)) and read the result from the call's own output (the `error: deploy gate:` lines against the `gate probe:` line), with the marker only as the second record. Alternatively, compare the marker's line count before and after the call.
- Resolution: resolved (2nd pass): row 5 step (d) deletes `gate-probe.marker` before each probe, step (f) reads the verdict from the call's own output, and the marker's absence is only the second record (g).

#### R3. Row 4 states the wrong reason for the block and doesn't pin what the probe depends on
- Area: 9. Testability
- Finding: The probe call `pac solution import --path <absolute path>` has no `--environment`, so `ImportCall.Finish` (`src/PowerOps.Core/Deploy/ImportCall.cs:458`) blocks it as unreadable (`--environment is missing`, the reading guidance) before the plan file, the zip or any target read. Row 4's claim that it is blocked "against a plan file that doesn't exist" is not why it gets exit 2. That matters because the session is started with the stand-in first on `PATH` and the hook process inherits it: the probe's "no marker means active" holds only while the gate blocks this input without starting `pac`. If the gate started `pac` for it, the stand-in would write the marker even with an active hook. Row 4 asserts only exit 2 and "a reason".
- Suggested fix: Correct row 4's wording. Make the test assert the exact `--environment is missing` line, and that the fake `pac` and the target read get no call. Add a sentence in the README that the probe relies on the gate blocking this call before it runs `pac`.
- Resolution: resolved (2nd pass): row 4 now gives the real reason (`--environment is missing`, `ImportCall.cs:458`, before the plan file, zip or target is read), and its test pins that exact line and no `pac` or target call. Confirmed by running the built gate on the README's command form.

### Low

#### R4. Row 1's one-folder-up test case is spelled relative to the test runner's folder
- Area: 9. Testability
- Finding: `--save ../p.json` resolves against the process's current folder (the test output folder), not the deploy folder, so taken literally the test writes a file outside its temp root.
- Suggested fix: Spell the case as `Path.Combine(Folder, "..", "p.json")`, which stays in the test's temp root.
- Resolution: resolved (2nd pass): the case is spelled `Path.Combine(Folder, "..", "p.json")`, inside the test's temp root.

#### R5. A relative `PATH` entry puts the marker wherever the agent last changed folder
- Area: 9. Testability
- Finding: The stand-in writes beside `$(dirname "$0")`. With a relative `PATH` entry (for example `PATH="probe:$PATH" claude`), `$0` is relative and the marker lands under the agent's current folder. That undoes the decisions log's refinement (5), "so its place doesn't depend on the agent's `cd`". Row 5 steps (a) and (c) don't say the folder must be absolute.
- Suggested fix: In steps (a) and (c), require an absolute folder, for example `PATH="$(realpath <folder>):$PATH" claude`, and say why.
- Resolution: resolved (2nd pass), moot: the `PATH` step is gone. Step (a) copies the stand-in to an absolute folder, step (e) calls it by that absolute path, and `$(dirname "$0")` is then absolute.

#### R6. Programs that start `pac` at session start under the stand-in `PATH` are not covered
- Area: 10. Unanswered areas
- Finding: The session runs with the stand-in first on `PATH`. Any MCP server or tool that the session starts by running `pac` from `PATH` at startup could start the stand-in and fail for the whole session, even after step (h) deletes it. This would affect pac's MCP server, if it is configured that way, which HANDOFF names as an import route behind the gate. The README's "doesn't cover" list names the MCP matcher but not this effect. How pac's MCP server is started was not checked here. PowerOps' own `PacRunner` resolves `pac` on each call, so it is not affected.
- Suggested fix: Add a line to the README: after step (h), check that the session's MCP servers are connected, or restart the session (without the stand-in on `PATH`, and with the hooks kept), before importing through an MCP tool.
- Resolution: resolved (2nd pass), moot: the stand-in is never on `PATH`, so no MCP server or tool started with the session can start it (`Won't build`, first bullet).

#### R7. Row 7 repeats a sentence INSTALL.md already has
- Area: 11. Size
- Finding: `docs/INSTALL.md:28` already says a build from a clone prints `<version>+<commit>` for `powerops --version`. Row 7 restates that before adding the new part, which is comparing it with `git rev-parse HEAD`.
- Suggested fix: Add only the check (compare the commit with `git rev-parse HEAD` in the clone; the version alone proves nothing), and point back to the existing sentence.
- Resolution: resolved (2nd pass): row 7 now adds only the comparison with `git rev-parse HEAD` and points to `:28`.

#### R8. The plan names a documentation host
- Area: URLs, GUIDs, emails and names in the plan
- Finding: Assumptions names a public documentation host (`<url>`) as the source that was checked. It holds no tenant value, but the rule reports every URL.
- Suggested fix: Name the pages as the rest of the line does ("the settings page", "the hooks page", "the tools reference") and drop the host, or put the links in the docs where they will live.
- Resolution: resolved (2nd pass): Assumptions names the pages, and neither `plan.md` nor `decisions.md` holds a URL, host, GUID or email.

#### R9. "Build in Dev in another session without them" can be read as running both sessions at once
- Area: 5. Dev to Test to Prod route (where Dev building stops), and 9. Testability
- Finding: Row 6 rewrites the HANDOFF hooks bullet to "put the hooks into `.claude/settings.json` before you start the session that runs steps 7 and 8, and build in Dev in another session without them". The hooks sit in the repo's shared project settings file, and the plan's own assumption says edits to that file are normally picked up while a session runs. A Dev session running at the same time as the deployment session would therefore get the hooks too, so its Dev imports would be blocked. Taking the hooks out for that session would also take them out of the deployment session, possibly without notice, which is the F43 failure again. The old wording ("turn them on when you start step 7 and off when you go back to building in Dev") was sequential.
- Suggested fix: Word it as sequential: build in Dev before the hooks are added, or after they are removed, never while the deployment session runs. If the hooks are edited while the deployment session runs, run the probe again before the next import.
- Resolution: resolved (3rd pass): row 6 now says the settings file is shared by every session in the repository, so Dev building happens before the hooks go in or after they come out, never while the deployment session runs, and the probe runs again before the next import if the hooks were edited during it. This matches the rest of the bullet, which stays (Dev imports blocked with the hooks on).

## Checklist items not applicable

This plan changes the PowerOps CLI and its docs. It builds no Power Platform solution and makes no tenant read or write.

1. Solution boundaries and publisher prefix: not applicable (no solution; `plan facts` not read).
2. Connection references and environment variables: not applicable. No hard-coded tenant value was found; the gate-probe paths are placeholders.
3. Premium connectors and licences: not applicable.
4. DLP fit: not applicable (no connector is used).
5. Dev to Test to Prod route: not applicable to a solution. The code's route is the PR and merge in `decisions.md` Round 0 Q2. When Dev building happens is settled in R9 (resolved).
6. Managed or unmanaged: not applicable.
7. Data volume and delegation: not applicable.
8. Security roles: not applicable. The stand-in calls no host and writes only its marker.
9. Testability: R1 to R5 and R9, all resolved. Every step in `Steps` has a check, and nothing reaches Prod.
10. Unanswered areas: the Log lists the areas not asked and why; 0 open questions, 0 admin questions. R6 is resolved (moot).
11. Size: no tables, lists, flows or screens. Each new file traces to F42, F43, F1 or F2 in `Goal`, and `Won't build` is present. R7 is resolved.
