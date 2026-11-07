# M63 Windows run: hand-over to Claude on the Windows machine

For the person: the [runbook](windows-runbook.md) split so that Claude Code on the Windows machine runs most of it. It writes nothing in a tenant.

Two parts. Do Part 1 yourself in PowerShell 7, with every Claude Code and Codex window closed: each one runs the `powerops mcp` server from the tool's folder, and the uninstall fails while that server is running. Then start `claude` and paste Part 2 into it.

## Part 1: you, in PowerShell 7, with no Claude Code or Codex open

Replace `<clone>` with the path of your PowerOps clone, then run:

```powershell
Start-Transcript "$HOME\Documents\m63-part1.txt"
[Environment]::OSVersion.VersionString; $PSVersionTable.PSVersion.ToString()
dotnet --list-sdks; git --version; node --version; claude --version
pac; az version --output json
Get-Process powerops -ErrorAction SilentlyContinue
cd <clone>
git fetch origin
git checkout --detach 707c577574ff00f6456c63e275b58e12b6d0e542
git status --short
dotnet tool uninstall --global PowerOps
dotnet pack src/PowerOps.Cli -c Release -o artifacts
dotnet tool install --global --source artifacts PowerOps
claude plugin marketplace update powerops
claude plugin uninstall powerops@powerops --scope user
claude plugin install powerops@powerops --scope user
(Get-Content "$HOME\.claude\plugins\installed_plugins.json" | ConvertFrom-Json).plugins.'powerops@powerops' | Select-Object version, gitCommitSha
Stop-Transcript
```

`Get-Process powerops` and `git status --short` must print nothing. If `dotnet tool uninstall` says the tool isn't installed, that's fine.

Next, make `C:\powerops-m63` and copy these into it from the Linux run's scratch repo, keeping the folder layout. Copy them directly between your machines (a shared folder of the VM, `scp`, or a USB stick), never through GitHub or another shared or public place, because the export is unscrubbed and the settings file holds Test's connection id:

- `solutions\RoomBooking\`
- `deploy\RoomBooking_managed.zip` and `deploy\RoomBooking_managed.settings.json`
- `flowtests\`
- `scratch\upgrade\`

Then open a **new** PowerShell 7 window (so `powerops` is on PATH), run `cd C:\powerops-m63`, and start `claude` in that terminal. Use the terminal: the desktop app's Code tab declined the form without showing it on Linux. Answer the folder trust prompt, then paste Part 2. Before you paste, put your clone's path in place of `<clone>`, and the two environments' display names in place of `<Test name>` and `<Dev name>` (the Linux session gives you both).

## Part 2: paste this into `claude`

```text
Please run the M63 Windows runbook of the PowerOps repo for me, on this Windows machine, and write a scrubbed record of its output. I am the person the runbook talks about. I'm here to sign in, to approve tool calls, and to decline the form.

Inputs
- My PowerOps clone is at <clone>. It is checked out at 707c577574ff00f6456c63e275b58e12b6d0e542. Don't change its checkout, and don't commit, push or edit anything in it.
- Read the runbook with: git -C <clone> fetch origin, then git -C <clone> show origin/claude/stoic-pasteur-0c643d:docs/plans/m63-live-rerun/windows-runbook.md (if that branch is gone, use origin/main). Follow its steps 3 to 8 exactly, and compare each result with its "Expected" lines.
- I already ran steps 0 to 2 with a transcript at $HOME\Documents\m63-part1.txt. Read it and take steps 0 to 2's results from it. Run step 2's last check yourself: git -C <clone> diff --stat 707c577574ff00f6456c63e275b58e12b6d0e542 <the gitCommitSha in the transcript> -- plugins/claude
- I copied the run's files into C:\powerops-m63 (step 3). Check that they are all there before step 4.
- <Test> is the environment named "<Test name>". <Dev> is the one named "<Dev name>". Find each one's URL with pac env list, a read. Use the URL wherever the runbook says <Test> or <Dev>, and for the deploy_apply target.
- For step 7, ask me for the model-driven app link in Test.

Rules
- Write nothing in any tenant. The only tenant reads allowed are pac auth list, pac env list, az account show, and the two powerops deploy plan commands of step 5. Never run pac solution import, powerops deploy apply, powerops settings apply, or any pac or az command that creates, changes or deletes something. Step 6 pipes a JSON description of an import into powerops deploy gate; it never runs that import.
- If pac or az isn't signed in to the right account, stop and ask me to sign in myself (pac auth create, az login, never a device code). Don't sign in for me.
- In step 7, I sign in in the Playwright window, wait for the app's grid, and close it. Wait until I tell you I've done that.
- In step 8, call the powerops MCP server's deploy_apply tool yourself with planFile C:\powerops-m63\deploy\powerops-plan-windows.json, target set to Test's URL, and apply true. I'll approve the tool call, and then decline the form. Record whether the form appeared as a dialog, and what the tool returned. If no form appears, record step 8 as "not reached". Never call it again, and never try to get an accept.
- Record what happens; don't fix PowerOps or work around a failure. If a result differs from Expected, record the difference and go on to the next step, unless that step needs the failed step's output.
- Change nothing outside C:\powerops-m63, except the one output file below. Don't delete anything.

Output
- Write $HOME\Documents\m63-windows-output.md. For each step 0 to 8, give the commands, each exit code ($LASTEXITCODE), the output lines that show the result (all of them for errors), the Expected lines, and "matches" or "differs: <how>". Include both icacls outputs of step 7 in full.
- Scrub before you write: replace my account name and user name with <account>, the machine name with <machine>, every URL with <url>, every GUID or other id with <id>, every email with <email>, the tenant's name with <tenant>, and the two environment names with <Test> and <Dev>. Leave RoomBooking, pops_room, the commit sha and the version strings as they are. Read the finished file once more and fix anything you missed.
- When you're done, tell me: the output file's path; that $HOME\Documents\m63-part1.txt is unscrubbed and I should delete it once the output file is safe; and that I should delete C:\powerops-m63 once the Linux session has the output file, because it holds Test's connection id and my saved sign-in (playwright\.auth). Don't delete either yourself.
```

## Bringing it back

Copy `$HOME\Documents\m63-windows-output.md` to the Linux machine by the same direct means, and paste it, or its path, into the Linux Claude session. That session checks the scrub again, adds the output to the M63 record, logs the result, marks the plan `done`, and merges PR #106 once CI is green. Then delete `C:\powerops-m63` and `m63-part1.txt`. The Linux session copies the scrubbed output into its record, outside Git.
