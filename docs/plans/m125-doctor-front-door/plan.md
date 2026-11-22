---
status: draft
review: pending
environment: null
depth: standard
supersedes: null
---

# M125: `powerops doctor` and the README front door

## Goal

Phase 1 item 3 of the agreed plan (`reports/three-way-debate-2026-11-22/AGREED-PLAN.md`, git-ignored), as step 6 of its build plan: a newcomer gets one command that says whether their machine is ready, and a README that starts with two first runs instead of a 19-row table. The owner set the newcomer to standard licences by default (2026-11-22), so `doctor` reads licences only when asked and never assumes premium.

Done when:

1. `powerops doctor` prints one line per check, offline: the `powerops` version; `pac` and `az` found on PATH (a PATH lookup only, no process started); the PowerOps plugin and the Microsoft plugins installed in Claude Code, with their versions (Data row 2). It starts no process and makes no network call. A test runs it with `pac` and `az` runners that throw when called and with a PATH that holds neither, and it exits as Data row 4 says; a second test with fake `pac` and `az` on a temp PATH shows them found while the throwing runners stay uncalled.
2. `powerops doctor --live` runs the plain checks, then (Data row 3): `pac org who` (pac's signed-in environment, its `Friendly Name:` line), `az account show -o none`, and, only after that probe passed, one `az rest` GET of Microsoft Graph `me/licenseDetails` with `--query "value[].skuPartNumber"`, all through the existing runners (`PacRunner` for both, as `drift` uses). On success the licence line holds only product names from `licensing.md` section 3 and a count of part numbers that table doesn't list; an error, a timeout or an empty list prints only `the licence read failed`. Nothing a probe prints is repeated except the environment's friendly name, which `plan facts` already prints. The environment type line is `not read` (Assumptions A1). Tests with fake `pac` and `az` runners cover each outcome.
3. `--format text|json` (default text); the JSON has `schemaVersion` first (`JsonOutput.SchemaVersion`), `live`, and the `checks` list (Data row 1). `docs/schemas/doctor.schema.json`, its row in `docs/schemas/README.md`, a `("doctor", "doctor.schema.json")` entry in `OutputSchemaTests.Map`, and goldens `doctor.json` and `doctor.txt` (from a fixed host, with the version masked). Exit codes per Data row 4. No MCP tool is added.
4. `docs/COMMANDS.md`: `doctor` in the usage block and its own section (synopsis, options, what it does, output, exit codes, agents: an agent runs `--live` only under `powerops-plan`'s condition for the licence read, the person named the environment and said `az` is signed in; without it, the skill's fallback question stands).
5. `AGENTS.md`: the offline-first rule names `doctor --live` beside `drift`'s live reads, and the `pac org who` exception list names `doctor --live` (its `Friendly Name:` line). `docs/ecosystem/compatibility.md`: the `az` and Microsoft Graph `licenseDetails` rows name `doctor --live` where they are used; a row for Claude Code's plugin record (Data row 2).
6. `README.md` (Data row 5): two first runs, "Check this solution" (on your export, or on `powerops-sample.zip` from a release, with the sample README's expected result) and "Plan an app"; the command table in three groups, Check, Ship and Agent plumbing, with `manifest`, `baseline`, `settings *` and `deploy gate` under Advanced, and `doctor` added; the Dataverse Git YAML layout named; the install section's M120 wording kept ("until the first release, install from a clone").
7. `docs/BUILD-PLAN.md`: the M125 section, its build-order item and decision (numbers taken at merge).
8. `dotnet build` (0 warnings), `dotnet test` (the schema test and `DocsDriftTests` included), `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m125-doctor-front-door` with no violation, the leak scan clean; Fable work review PASS; CI green.

### Won't build

- No `--env` option: `--live` reads pac's selected environment only, and lists no other environment.
- No read of the environment type (A1), Codex's plugins, `pac`'s or `az`'s version (each would start a process), or another person's licences.
- No change to `skills/` (the planning skill keeps its licence read and fallback question word for word; M124 and M126 own that text), the deploy chain, the gate, a kept role or `tests/eval/`.
- No MCP tool, no `init`, no fix-it actions: `doctor` reads and reports, and writes nothing.
- The README's first-run commands are not run against a release install here: no release exists. Build plan step 9 runs them.

## Users

- A newcomer who has installed PowerOps and wants to know what is still missing before a first run.
- An agent that runs `doctor` (plain) to choose a route, and `doctor --live` only under the planning skill's condition.
- M126, whose entry skill starts "check" with plain `doctor`.

## Data

Row 1. Checks, in this order, each `{name, status, detail?}`. Status is one of `ok`, `missing`, `not installed`, `failed`, `not read`.

| name | when | ok detail | other statuses |
|---|---|---|---|
| `powerops` | always | the informational version, as `--version` prints | none |
| `pac` | always | `found on PATH` | `missing` |
| `az` | always | `found on PATH` | `missing` |
| `powerops plugin` | always | each installed copy's version and scope, `0.1.0 (user)` | `not installed`; `not read` when the record is unreadable |
| `Microsoft plugins` | always | `<plugin>@<marketplace> <version>` for each, comma-separated | `not installed`; `not read` |
| `pac sign-in` | `--live` | the environment's friendly name | `failed`; `not read` when `pac` is missing |
| `az sign-in` | `--live` | `signed in` | `failed`; `not read` when `az` is missing |
| `licences` | `--live` | product names, then `; <n> more not in the table` when any | `failed`; `not read` unless `az sign-in` is `ok` |
| `environment type` | `--live` | none | `not read` |

Without `--live` the text ends with one line naming `--live` and what it reads.

Row 2. The plugin record: Claude Code's `installed_plugins.json` in its plugins root, which is `CLAUDE_CODE_PLUGIN_CACHE_DIR` when set, else the `plugins` folder of `CLAUDE_CONFIG_DIR` when set, else `~/.claude/plugins` (A2). Claude Code documents the file's place and that each install records `scope`, `installPath` and `version` under its plugin id, in a file that declares a format version; the shape read is the one observed on 2026-11-22 (Claude Code on this machine): `{"version": 2, "plugins": {"<plugin>@<marketplace>": [{"scope", "version", "installPath", ...}]}}`. Another `version`, or another shape, is `not read`. The PowerOps plugin is the id `powerops@powerops`. A Microsoft plugin is any id whose marketplace is `power-platform-skills`, `dataverse-skills`, `skills-for-copilot-studio` or `fabric-collection` (A3), printed as its full id with its version, because `dataverse` is a plugin in two of them. A missing file is `not installed`; a file over 1 MB, unparsable, or of another shape is `not read`. Read through `File.ReadAllText` with the size checked first; a symbolic link is read like a file (it is read-only).

Row 3. Live calls, fixed arguments only, 30 s each, through `PacRunner` (the `az` one by `DriftLiveReader.AzExecutable`): `pac org who`; `az account show -o none`; `az rest --method get --url https://graph.microsoft.com/v1.0/me/licenseDetails --query value[].skuPartNumber -o tsv`. No argument comes from input, so on Windows `cmd.exe` parses only these constants (no `&`, `|`, `<`, `>`, `^`, `%` or `"` in them). The part-number table is section 3 of `skills/powerops-plan/references/licensing.md`, read from the copy embedded in the CLI (`SkillsCommand.Shipped()`), so `doctor` and the skill map through one table; a test checks that the parse finds every row. <!-- plan-status: allow -->

Row 4. Exit codes: `0` when no check is `missing` or `failed`; `1` when one is (a missing `pac` or `az`, a failed sign-in or licence read); `2` usage error; `3` internal error. `not installed` and `not read` never change the exit code: the plugins are for agents, and PowerOps runs without them.

Row 5. README. "Check this solution": `powerops analyze <your export>`; with no export, download `powerops-sample.zip` from a release, unzip, and run the sample README's two commands, with its two result lines quoted. "Plan an app": install the plugin, then ask the agent to plan the app (the `powerops-plan` skill). The command table's groups: Check (`doctor`, `analyze`, `rules`, `diff`, `inventory`, `test`, `drift`); Ship (`deploy plan`, `deploy apply`, `plan status`, `plan sign-off`); Agent plumbing (`plan facts`, `skills install`, `mcp`); Advanced (`manifest`, `baseline`, `settings generate`, `settings validate`, `settings apply`, `deploy gate`). The layouts sentence names unpacked XML folders, exported zips and the YAML layout Dataverse Git integration commits, linked to COMMANDS' `analyze` section.

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
- `--live` prints no account, tenant, URL, id or part number, and nothing a failed call printed.
- The licence read is not widened: one GET, the signed-in account only, the same `--query`, product names only.
- JSON additive under AGENTS.md's `schemaVersion` rules; no existing output changes.
- `tests/eval/`, the kept roles, their plugin copies and `skills/` stay byte-for-byte.

## Assumptions

- A1. Environment type: Learn documents the Dataverse function `RetrieveCurrentOrganization`, whose `OrganizationDetail` has an `OrganizationType` (values such as Customer, Secondary, CustomerTest, Default, Developer, Trial), and the preview `pac environment-management get-environment-by-id-for-user`, backed by a Power Platform API call that needs another token. Learn's sample response of the function leaves `OrganizationType` out, and no page maps its values to the admin center's environment types (Production could be Customer or Secondary). With no capture to show the field and no documented mapping, `doctor` prints `not read`; a later milestone may add the read after a live capture (`decisions.md`, follow-up). Checked by `powerops-researcher` on Learn, 2026-11-22.
- A2. The plugins root and its three variables, and the fields of `installed_plugins.json`, are in Claude Code's docs (*Plugin loading*, *Environment variables*); the file's exact shape is observed, not documented, so a changed shape reads as `not read`, never as `not installed`. Checked 2026-11-22.
- A3. The four marketplace names are the `name` of each repo's `.claude-plugin/marketplace.json` on `main`: microsoft/power-platform-skills, microsoft/Dataverse-skills, microsoft/skills-for-copilot-studio, microsoft/skills-for-fabric. Checked 2026-11-22.
- A4. Graph `me/licenseDetails` reads the signed-in user's own licences, `skuPartNumber` is a field of each item, and for a work account the page names `LicenseAssignment.Read.All` as least privileged and a directory role (such as Directory Readers) for delegated reads, so a user without one may be refused: then the line is `failed`, as the skill already treats it. Unchanged since the compatibility row of 2026-11-11; re-checked 2026-11-22.

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
