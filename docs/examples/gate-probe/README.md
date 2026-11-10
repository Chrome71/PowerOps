# Gate probe: is the deploy gate active in this session?

Run this probe at the start of the deployment session, before the first import. It shows whether the `deploy gate` hook runs on the agent's `Bash` calls, without calling a tenant.

[pac](pac) is a stand-in for `pac`. It is a bash script that never calls a host. It appends one line to `gate-probe.marker` beside itself, prints one line that starts with `gate probe:` on stderr and exits 1, whatever its arguments.

The probe calls the stand-in by its absolute path. The gate reads the program by its file name, so it reads the call as an import by pac and blocks it, because the call has no `--environment`. It blocks before it reads the plan file, a zip or the target.

What it proves:

- A block means the hook is active in this session for Claude Code's `Bash` tool. That is the pass.
- The `gate probe:` line means no hook blocked the call, so the gate is not active.

What it doesn't prove:

- That an import of the reviewed plan passes the gate. The probe stops at the first check.
- Anything about the MCP matcher (pac's MCP `solution_import`), `Monitor`, `PowerShell` or Codex. The probe covers Claude Code's `Bash` tool only.
- Windows. The stand-in needs bash; on Windows it is untested.

No outcome can reach a tenant. The stand-in is never on `PATH`, and the probe never names the real `pac`.

## Steps

The folder below is a placeholder: use any folder outside the repository.

- (a) Copy [pac](pac) into a folder outside the repository, for example `/home/<you>/powerops-gate-probe/pac`, and make it executable: `chmod +x /home/<you>/powerops-gate-probe/pac`.
- (b) Put the hooks of HANDOFF's [Gate Microsoft's imports](../../HANDOFF.md#gate-microsofts-imports) into `.claude/settings.json` before the deployment session starts.
- (c) Start the deployment session.
- (d) Delete `/home/<you>/powerops-gate-probe/gate-probe.marker` if it is there. Do this before each probe.
- (e) Make this the session's first command, through the agent's `Bash` tool:

  ```sh
  /home/<you>/powerops-gate-probe/pac solution import --path /nonexistent/powerops-gate-probe.zip
  ```

  Run it exactly so. Don't add `--environment` and don't reword it. Don't retry it after a block.
- (f) Read the result from the call's own output:
  - `error: deploy gate:` lines: the hook is active. This is the pass.
  - The `gate probe:` line: the hook is not active, and the session must not import. Restart the session with the hooks in place and probe again from step (d).
  - Neither line: the call was blocked, but not by the gate (for example, the hook's `|| exit 2` fired because `powerops` is not on `PATH`). That is not a pass: fix the hook and probe again from step (d).
- (g) Record the call's output. As the second record, check that `gate-probe.marker` is absent in the stand-in's folder: the stand-in never ran.
