# M63 Windows runbook

For the person, on their Windows machine, in PowerShell 7, right after the Linux run's import into Test and inside Test's term ([plan](plan.md), "Windows"). It writes nothing in a tenant: it reads Test, and the one form it opens is declined on a plan that imports nothing. Replace `<Test>` and `<Dev>` with each environment's URL or id, as `pac` takes it, and `<model-driven app link in Test, with its appid>` with the app's link; the Linux run's own values are filled in below.

Filled in by the Linux run:

- Commit: `707c577574ff00f6456c63e275b58e12b6d0e542` (the build the Linux run installed)
- Solution: `RoomBooking`; managed zip `RoomBooking_managed.zip`
- Table `pops_room`, grid column `Name`, form field `Name` (for Playwright; a text column, which the starter's form check needs)
- Expected lines: under each step, from the Linux run

Paste back, for each step: the command, its exit code (`$LASTEXITCODE`) and its output. Before you paste, replace your account name, machine name, every URL and every id with `<account>`, `<machine>`, `<url>` and `<id>`.

## 0. Versions

```powershell
[Environment]::OSVersion.VersionString; $PSVersionTable.PSVersion.ToString()
dotnet --list-sdks; git --version; node --version; claude --version
pac; az version --output json
```

`pac` with no arguments prints its version in its first lines.

## 1. Install the tool from the same commit

Close every Claude Code and Codex session first: each runs a `powerops mcp` server from the tool's folder, and the uninstall fails while one runs (M32). `Get-Process powerops -ErrorAction SilentlyContinue` must print nothing. In your PowerOps clone:

```powershell
git fetch origin
git checkout --detach 707c577574ff00f6456c63e275b58e12b6d0e542
git status --short
dotnet tool uninstall --global PowerOps
dotnet pack src/PowerOps.Cli -c Release -o artifacts
dotnet tool install --global --source artifacts PowerOps
```

`git status --short` must print nothing. Then, in a new PowerShell window: `powerops --version`. Expected: `0.0.0+707c577574ff00f6456c63e275b58e12b6d0e542`.

## 2. Reinstall the plugin

```powershell
claude plugin marketplace update powerops
claude plugin uninstall powerops@powerops --scope user
claude plugin install powerops@powerops --scope user
(Get-Content "$HOME\.claude\plugins\installed_plugins.json" | ConvertFrom-Json).plugins.'powerops@powerops' | Select-Object version, gitCommitSha
git diff --stat 707c577574ff00f6456c63e275b58e12b6d0e542 <the gitCommitSha above> -- plugins/claude
```

The last command must print nothing (the plugin is the same as at that commit), or paste what it prints.

## 3. Copy the run's files

Make `C:\powerops-m63` and copy into it, from the Linux scratch repo, by your own means (a USB stick or a direct copy between your machines), never through a shared or public place, because the settings file holds Test's connection id:

- `solutions\RoomBooking\` (the unpacked export)
- `deploy\RoomBooking_managed.zip` and `deploy\RoomBooking_managed.settings.json`
- `flowtests\`
- `scratch\upgrade\` (the copy with its version raised; it is planned, never imported)

## 4. Offline commands

```powershell
cd C:\powerops-m63
powerops analyze solutions\RoomBooking; $LASTEXITCODE
powerops analyze deploy\RoomBooking_managed.zip --format json | Select-String notChecked; $LASTEXITCODE
powerops test solutions\RoomBooking --tests flowtests; $LASTEXITCODE
powerops diff solutions\RoomBooking deploy\RoomBooking_managed.zip; $LASTEXITCODE
powerops settings validate solutions\RoomBooking --settings deploy\RoomBooking_managed.settings.json; $LASTEXITCODE
```

Expected, as on Linux:

- `analyze` of the folder: `1 solution, 0 errors, 0 warnings`, exit 0
- `analyze` of the zip: one `"notChecked": []` line, exit 0
- `test`: `4 tests: 4 passed, 0 failed, 0 unmocked, 0 unsupported`, exit 0
- `diff`: `changed solution managed: unmanaged → managed [risk: same-version]`, exit 1
- `settings validate`: `no problems`, exit 0

## 5. Read Test

`pac auth list` and `az account show --query user.type`. If either isn't signed in, sign in yourself (`pac auth create`, `az login`; not with a device code).

```powershell
powerops deploy plan C:\powerops-m63\deploy --target <Test> --planned-by person --save C:\powerops-m63\deploy\powerops-plan-windows.json; $LASTEXITCODE
powerops deploy plan C:\powerops-m63\scratch\upgrade --target <Test> --save C:\powerops-m63\scratch\upgrade\powerops-plan-upgrade.json; $LASTEXITCODE
```

Expected: the first `skip` (Test holds that version), `plan ok`, exit 0; the second `upgrade` with a `components-not-checked` item, exit 0. `<Test>` is Test's URL or id, as `pac` takes it.

## 6. The gate's allow path with `C:\` paths

Nothing is imported: the gate reads the call and exits; the call itself never runs.

```powershell
$zip = 'C:\powerops-m63\scratch\upgrade\RoomBooking_managed.zip'
$set = 'C:\powerops-m63\scratch\upgrade\RoomBooking_managed.settings.json'
$plan = 'C:\powerops-m63\scratch\upgrade\powerops-plan-upgrade.json'
function Gate($envName) {
  $cmd = "pac solution import --environment $envName --path `"$zip`" --settings-file `"$set`" --async 2>&1"
  @{ hook_event_name = 'PreToolUse'; tool_name = 'Bash'; tool_input = @{ command = $cmd }; cwd = 'C:\powerops-m63' } |
    ConvertTo-Json -Compress | powerops deploy gate $plan
  "exit $LASTEXITCODE"
}
Gate '<Test>'
Gate '<Dev>'
```

Expected: `exit 0` for Test, and for Dev one `error: deploy gate: ...` line naming the other environment and `exit 2`.

## 7. Playwright, with the `icacls` hand check

Copy `docs\examples\playwright` from the clone to `C:\powerops-m63\playwright`, and in the copy's `tests\apps.spec.ts` set `table`, `gridColumn` and `formField` at the top to `pops_room`, `Name` and `Name`, and delete the canvas test and the `canvasGallery` line (this product has no canvas app). Then:

```powershell
cd C:\powerops-m63\playwright
npm ci
npx playwright install chromium
New-Item -ItemType Directory -Force .auth | Out-Null
icacls .auth /inheritance:r /grant:r "$(whoami):(OI)(CI)F"
$env:MODEL_DRIVEN_APP_URL = '<model-driven app link in Test, with its appid>'
npx playwright open --save-storage=.auth/state.json "$env:MODEL_DRIVEN_APP_URL"
```

Sign in yourself in the window, wait for the app's grid, close the window. Then:

```powershell
Test-Path .auth\state.json
icacls .auth
icacls .auth\state.json
npx playwright test; $LASTEXITCODE
```

Expected: `True`; each `icacls` lists one entry, your account, with `(F)` (the folder with `(OI)(CI)(F)`, the file inherited `(I)(F)`), and no `BUILTIN\Users`, `Everyone` or `Authenticated Users` line; the test passes, exit 0. Paste both `icacls` outputs whole, with your account name replaced by `<account>`.

## 8. The form, declined

Open `claude` in a terminal in `C:\powerops-m63` (answer the folder trust prompt first; on Linux the desktop app's Code tab declined the form without showing it, so use the terminal) and ask: "Call the powerops tool deploy_apply with plan file C:\powerops-m63\deploy\powerops-plan-windows.json, target <Test>, apply true. I will decline the form." When it asks for your yes in chat, say yes; when the form opens, choose **Decline**. This plan only skips, so even an accept imports nothing, but decline it.

Expected: the form opens, and the tool's result says the apply was declined and nothing was imported. Paste the result, and say whether the form showed as a dialog. If no form appears, paste what the tool returned instead: the run records the form on Windows as not reached.

## 9. After

Delete `C:\powerops-m63` when the Linux session has your pasted output: it holds Test's connection id and your saved sign-in (`playwright\.auth`). Nothing in Test needs cleaning up from this runbook.
