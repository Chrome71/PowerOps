---
status: signed-off
review: done
environment: null
depth: standard
supersedes: null
signed-off: 2026-11-22
signed-off-hash: d054518110cc438f30a02cd7ef169ec38312bbb658d345dc1a1380e3fb00a602
---

# M125: `powerops doctor` and the README front door

## Goal

Phase 1 item 3 of the agreed plan (`reports/three-way-debate-2026-11-22/AGREED-PLAN.md`, git-ignored), as step 6 of its build plan: a newcomer gets one command that says whether their machine is ready, and a README that starts with two first runs instead of a 19-row table. The owner set the newcomer to standard licences by default (2026-11-22), so `doctor` reads licences only when asked and never assumes premium.

Done when:

1. `powerops doctor` prints one line per check, offline: the `powerops` version; `pac` and `az` found on PATH (a PATH lookup only, no process started); the PowerOps plugin and the Microsoft plugins installed in Claude Code, with their versions (Data row 2). It starts no process and makes no network call. The test seam is Data row 6: tests pass their own host, never set an environment variable. A test runs it with `pac` and `az` runners that throw when called and a lookup that finds nothing, and it exits as Data row 4 says; a second test puts fake `pac` and `az` files in a temp folder given as the PATH, and they are found while the throwing runners stay uncalled.
2. `powerops doctor --live` runs the plain checks, then the three calls of Data row 3, with exactly the argument lists written there: `pac org who` (pac's signed-in environment, its `Friendly Name:` line), `az account show`, and, only after that probe passed, one `az rest` GET of Microsoft Graph `me/licenseDetails`, all through the existing runners (`PacRunner` for both, as `drift` uses). On success the licence line holds only product names from `licensing.md` section 3 and a count of part numbers that table doesn't list (C6); an `az` error, a timeout, an empty list or a line holding a control character prints only the failure sentence of Data row 1. Nothing a probe prints is repeated except the environment's friendly name, which `plan facts` already prints, and it, the plugin ids and the versions go through `TextReport.Clean`. The environment type line is `not read` (A1). Tests with fake runners assert the exact three argument lists, at most one Graph call, the part numbers `Microsoft_365_E3_(no_Teams)` and `O365_w/o Teams Bundle_M3` alone (`none in PowerOps' table (2 other)`) and with `POWERAPPS_PER_USER` (`Power Apps Premium; 2 not in PowerOps' table`), a control-character line as a failed read, CRLF output read like LF output, none when the `az` probe is not `ok`, the case "probe passes, read fails" (A5), a control character in the friendly name, and that no probe's stdout or stderr reaches the output on any path, the timeout included.
3. `--format text|json` (default text); the JSON has `schemaVersion` first (`JsonOutput.SchemaVersion`), `live`, and `checks` (Data row 1), with `id` and `status` in the repo's kebab-case spelling. `docs/schemas/doctor.schema.json` (`status` a closed enum; `id` an open string, so a later check, the environment type's read for one, adds no `schemaVersion` bump), its row in `docs/schemas/README.md`, a `("doctor", "doctor.schema.json")` entry in `OutputSchemaTests.Map`, and goldens `doctor.txt` and `doctor.json` (plain, with a `missing` and a `not-installed` line) and `doctor-live.json` (every live check), from a fixed host whose version is a fixed string. Exit codes per Data row 4. No MCP tool is added; `McpCommandTests.ToolNames` is unchanged.
4. `docs/COMMANDS.md`: `doctor` in the usage block and its own section (synopsis, options, what it does, output, exit codes, agents: an agent runs `--live` only under `powerops-plan`'s condition for the licence read, the person named the environment and said `az` is signed in; without it, the skill's fallback question stands). It says what `az sign-in` proves and doesn't (A5), and that the `pac` and `az` lines may be different accounts. The `az` flags it names are added to `DocsDriftTests`' known list, the check itself unchanged; no line over 1,500 characters.
5. `AGENTS.md`: exactly two edits, the offline-first rule names `doctor --live` beside `drift`'s live reads, and the `pac org who` exception list names `doctor --live` (its `Friendly Name:` line). `docs/ecosystem/compatibility.md`: the `az` and Microsoft Graph `licenseDetails` rows name `doctor --live` where they are used; a row for Claude Code's plugin record (Data row 2).
6. `README.md` (Data row 5): two first runs, "Check this solution" (on your export, or on the sample, with the sample README's expected result) and "Plan an app"; the command table in four groups, Check, Ship, Agent plumbing and Advanced, with `manifest`, `baseline`, `settings *` and `deploy gate` under Advanced, and `doctor` added; the Dataverse Git YAML layout named; "What you need" and the "read an environment" sentence name `doctor --live`; the install section's M120 wording kept ("until the first release, install from a clone"). Two new tests: the README holds the two result lines `.github/scripts/sample-README.md` holds, and every leaf command of `Program.Root` has a row in the README's command table.
7. `docs/BUILD-PLAN.md`: the M125 section, its build-order item and decision (numbers taken at merge).
8. `dotnet build` (0 warnings), `dotnet test` (the schema test and `DocsDriftTests` included), `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m125-doctor-front-door` with no violation, the leak scan from the main checkout (`bash reports/leakscan.sh origin/main <branch>`, 0 hits, control found); `git diff --stat origin/main` shows nothing under `skills/`, `plugins/`, `tests/eval/`, `DeployCommand.cs`, the gate's tests or the version files; Fable work review PASS; CI green.

### Won't build

- No `--env` option: `--live` reads pac's selected environment only, and lists no other environment.
- No read of the environment type (A1), Codex's plugins, `pac`'s or `az`'s version (each would start a process), or another person's licences.
- No change to `skills/` (the planning skill keeps its licence read and fallback question word for word; M124 and M126 own that text), the deploy chain, the gate, a kept role or `tests/eval/`.
- No MCP tool, no `init`, no fix-it actions: `doctor` reads and reports, and writes nothing.
- The README's first-run commands are not run against a release install here: no release exists. Build plan step 9 runs them.
- The README's agent-install prompt keeps its step 5 (`pac` and `az` on PATH) word for word: the prompt is M120's install text, and pointing it at `doctor` is a later change.

## Users

- A newcomer who has installed PowerOps and wants to know what is still missing before a first run.
- An agent that runs `doctor` (plain) to choose a route, and `doctor --live` only under the planning skill's condition.
- M126, whose entry skill starts "check" with plain `doctor`.

## Data

Row 1. Checks, in this order. Each has an `id` (JSON), a label (text), a `status` and a `detail`. Status is one of `ok`, `missing`, `not-installed`, `failed`, `not-read` (text: spaces for hyphens). Every non-`ok` line carries one fixed sentence: what it blocks and what to do, reusing the existing texts of `PlanFacts` and `DriftLiveReader`.

| id (label) | when | ok detail | other statuses and their sentence |
|---|---|---|---|
| `powerops` (powerops) | always | the informational version, as `--version` prints | none |
| `pac` (pac) | always | `found on PATH` | `missing`: needed by `settings apply`, `drift`, `deploy` and `plan facts`; "Install pac (dotnet tool install --global Microsoft.PowerApps.CLI.Tool) and add the dotnet tools folder to PATH." |
| `az` (az) | always | `found on PATH` | `missing`: needed by `drift`, `deploy` and the licence read; "Install the Azure CLI and sign in with az login." |
| `powerops-plugin` (powerops plugin) | always | each distinct installed version and scope, `0.1.0 (user)`, "installed in Claude Code" | `not-installed`: "not installed in Claude Code. claude plugin marketplace add Chrome71/PowerOps, then claude plugin install powerops@powerops; or powerops skills install in the repo"; `not-read`: "Claude Code's plugin record has a shape PowerOps doesn't read" |
| `microsoft-plugins` (Microsoft plugins) | always | `<plugin>@<marketplace> <version>` for each, comma-separated | `not-installed`: "none of Microsoft's plugins in Claude Code; the build steps use them (docs/HANDOFF.md)"; `not-read`: as above |
| `pac-sign-in` (pac sign-in) | `--live` | the environment's friendly name | `failed`: `PlanFacts.SignInFix`; `not-read`: "pac is missing" |
| `az-sign-in` (az sign-in) | `--live` | "an account is selected (no token requested)" | `failed`: "Sign in with az login."; `not-read`: "az is missing" |
| `licences` (licences) | `--live` | product names, then `; <n> not in PowerOps' table` when any; with no listed product, `none in PowerOps' table (<n> other)` | `not-read` (C7): "the licence read failed; the planning skill asks which products you hold instead", or "needs the az sign-in" |
| `environment-type` (environment type) | `--live` | none | `not-read`: "no documented read; the plan's environments are named by you, and the admin center shows each one's type" |

Without `--live` the text ends with one line naming `--live` and what it reads.

Row 2. The plugin record: Claude Code's `installed_plugins.json` in its plugins root, which is `CLAUDE_CODE_PLUGIN_CACHE_DIR` when set, else the `plugins` folder of `CLAUDE_CONFIG_DIR` when set, else `~/.claude/plugins` (A2). Claude Code documents the file's place and that each install records `scope`, `installPath` and `version` under its plugin id, in a file that declares a format version; the shape read is the one observed on 2026-11-22 (Claude Code 2.1.326 on this machine): `{"version": 2, "plugins": {"<plugin>@<marketplace>": [{"scope", "version", "installPath", ...}]}}`. Another `version`, or another shape, is `not-read`. Duplicate entries (this machine has 13 for one local plugin) collapse to distinct version and scope. The PowerOps plugin is the id `powerops@powerops`. A Microsoft plugin is any id whose marketplace is `power-platform-skills`, `dataverse-skills`, `skills-for-copilot-studio` or `fabric-collection` (A3), printed as its full id with its version, because `dataverse` is a plugin in two of them. A missing file is `not-installed`. The file is read through `BoundedRead.ReadAllText(path, 1 MB)` (Core), which counts the bytes it reads; its `InvalidDataException`, and any IO or JSON error, is `not-read`. Variables set in a Claude Code settings `env` block are not seen. The record says what is installed, not what is turned on or for which project: a plugin turned off, or installed for another folder only, still reads as installed, and COMMANDS says so; no `installPath` or `projectPath` is printed.

Row 3. Live calls, fixed arguments only, 30 s each, through `PacRunner` (the `az` one by `DriftLiveReader.AzExecutable`): `pac org who`; `az account show -o none`; `az rest --method get --url https://graph.microsoft.com/v1.0/me/licenseDetails --query value[].skuPartNumber -o tsv`. No argument comes from input, so on Windows `cmd.exe` parses only these constants (no `&`, `|`, `<`, `>`, `^`, `%` or `"` in them). The output is split into lines on LF, a trailing CR dropped (`az` on Windows prints CRLF). Each non-empty line is a part number when it holds no control character (tab included) and is at most 256 characters; Learn's part-number list has ones with spaces, `(`, `)`, `/` and `,`, so no tighter pattern. Any other line makes it a failed read. Nothing from the read is echoed: names come from the table, the rest is a count. The part-number table is section 3 of `skills/powerops-plan/references/licensing.md`, read from the copy embedded in the CLI (`SkillsCommand.Shipped()`), so `doctor` and the skill map through one table (C2); a test checks that the parse finds every row. <!-- plan-status: allow -->

Row 4. Exit codes: `0` when no check is `missing` or `failed`; `1` when one is (a missing `pac` or `az`, a failed sign-in); `2` usage error; `3` internal error. `not-installed` and `not-read` never change the exit code: the plugins are for agents, PowerOps runs without them, and a refused licence read is the expected case for a standard-licence account (C7).

Row 5. README. "Check this solution": `powerops analyze <your export>`; with no export, the sample: a release's `powerops-sample.zip` with the sample README's two commands, and, until the first release, the same files in a clone of this repository, run from its root as `powerops analyze tests/fixtures/powerops-seed/v2` and `powerops test tests/fixtures/powerops-seed/v2 --tests tests/fixtures/flowtests/seed` (same two result lines, run 2026-11-22 by the plan review); the two result lines quoted once. No `git clone` fence comes before the "Run it in CI" one, which `QuickStartProblems` reads first. "Plan an app": install the skills, then ask the agent to plan the app (the `powerops-plan` skill); nothing is built before sign-off. The command table's groups: Check (`doctor`, `analyze`, `rules`, `diff`, `inventory`, `test`, `drift`); Ship (`deploy plan`, `deploy apply`, `plan status`, `plan sign-off`); Agent plumbing (`plan facts`, `skills install`, `mcp`); Advanced (`manifest`, `baseline`, `settings generate`, `settings validate`, `settings apply`, `deploy gate`). The layouts sentence names unpacked XML folders, exported zips and the YAML layout Dataverse Git integration commits. Kept as they are, because existing tests read them: the "Run it in CI" clone snippet and its pin, the links to `docs/RECOVERY.md` and `docs/HARNESS.md`, and the `#install` heading.

Row 6. The test seam: a `DoctorHost` record (the version string, the PATH and PATHEXT strings, the plugin record's path, and the `az` runner), so a test runs the shipped lookup, `PacRunner.Resolve`, on a temp folder given as PATH; passed as a new optional parameter at the end of `Program.Run` and `Program.Root`; the default host reads PATH, PATHEXT, the home folder and the two Claude Code variables; the checks call `PacRunner.Resolve` and `DriftLiveReader.AzExecutable`, never a copy. The `pac` runner stays `Program.Run`'s existing one. Existing overloads and callers don't change.

## Automation

None.

## Screens

None.

## Environments and route

Offline: no tenant call while building. The tests fake `pac` and `az`. `doctor --live` itself is never run against a tenant in this milestone.

## Solution and publisher

None.

## Constraints

- Plain `doctor` starts no process and opens no connection.
- `--live` prints no account, tenant, URL, account, tenant or environment id, or part number, and nothing a failed call printed.
- The licence read is not widened: one GET, the signed-in account only, the same `--query`, product names only.
- JSON additive under AGENTS.md's `schemaVersion` rules; no existing output changes.
- `tests/eval/`, the kept roles, their plugin copies and `skills/` stay byte-for-byte.

## Assumptions

- A1. Environment type: Learn documents the Dataverse function `RetrieveCurrentOrganization`, whose `OrganizationDetail` has an `OrganizationType` (values such as Customer, Secondary, CustomerTest, Default, Developer, Trial), and the preview `pac environment-management get-environment-by-id-for-user`, backed by a Power Platform API call that needs another token. Learn's sample response of the function leaves `OrganizationType` out, and no page maps its values to the admin center's environment types (Production could be Customer or Secondary). With no capture to show the field and no documented mapping, `doctor` prints `not read`; a later milestone may add the read after a live capture (`decisions.md`, follow-up). Checked by `powerops-researcher` on Learn, 2026-11-22.
- A2. The plugins root and its three variables, and the fields of `installed_plugins.json`, are in Claude Code's docs (*Plugin loading*, *Environment variables*); the file's exact shape is observed, not documented, so a changed shape reads as `not read`, never as `not installed`. Checked 2026-11-22.
- A3. The four marketplace names are the `name` of each repo's `.claude-plugin/marketplace.json` on `main`: microsoft/power-platform-skills, microsoft/Dataverse-skills, microsoft/skills-for-copilot-studio, microsoft/skills-for-fabric. Checked 2026-11-22.
- A4. Graph `me/licenseDetails` reads the signed-in user's own licences, `skuPartNumber` is a field of each item, and for a work account the page names `LicenseAssignment.Read.All` as least privileged and a directory role (such as Directory Readers) for delegated reads, so a user without one may be refused: then the line is `not-read` with its failure sentence (C7). Unchanged since the compatibility row of 2026-11-11; re-checked 2026-11-22.
- A6. `pac org who` is parsed as text once more, for its `Friendly Name:` line only, as `plan facts` does; open entry ECO-0013 (pac documents `--json` with no fields) covers this parse with the others, and a switch to `--json` would change `doctor` with them.
- A5. `az account show -o none` exits non-zero when no account is signed in (run 2026-11-22 with an empty `AZURE_CONFIG_DIR`: `ERROR: Please run 'az login' to setup account.`, exit 1), and exits 0 when an account is selected without proving a token can be issued: the project's Windows run recorded it passing while no token could be issued (`docs/BUILD-PLAN.md`, the Windows run of 2026-10-30, `AADSTS135010`). So the line says "an account is selected (no token requested)", the licence read is the first token request, and a test covers the probe passing and the read failing. Learn's `az account` reference documents the command, not its exit code; the signed-in case was seen only in that run.

## Open questions

None.

## Questions for your admin

None.

## Steps

1. **Research** (main session, done 2026-11-22). `powerops-researcher` on A1 to A4; results in `decisions.md` and Assumptions.
2. **Plan review** (main session). Fable plan review until approved; `powerops-review-plan` in a fresh context; every finding answered.
3. **Sign-off** (main session). `powerops plan sign-off docs/plans/m125-doctor-front-door`, logged with the owner's "Yes to all three" (2026-11-22).
4. **Command, test first** (opus). `DoctorCommandTests` (no-runner, found-on-PATH, each live outcome, plugin record shapes, the licensing table parse, exit codes, goldens), seen failing; then `DoctorCommand.cs` and its registration, the schema, its README row and map entry. Done: Done lines 1 to 3.
5. **Docs** (opus). COMMANDS, AGENTS, compatibility, README, BUILD-PLAN. Done: Done lines 4 to 7; `DocsDriftTests` pass.
6. **Checks, work review, merge** (main session). Done line 8; merge by the chain's protocol; `status: done`; hand-over note.
