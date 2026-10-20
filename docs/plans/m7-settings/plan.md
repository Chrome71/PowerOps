---
status: approved
review: done
signed-off: null
signed-off-hash: null
environment: null
depth: quick
supersedes: null
---

# M7: settings (offline half)

## Goal

`powerops settings generate` and `powerops settings validate` work offline on an unpacked solution folder or an exported zip. They create and check the deployment settings file (`deploymentsettings.json`) that `pac solution import --settings-file` and Pipelines read to set connection references and environment variables at import. Release v0.8.0 together with the live half.

This plan was written after the build (commits "feat(settings): add settings generate and validate" and "test(settings): cover generate --out into a missing folder and the zip layout"). It covers only the offline half of the BUILD-PLAN M7 row. The live half (`--apply`) gets its own plan; see Automation.

Done when (offline half of the M7 done-when):

- `settings validate` catches missing and extra entries using the settings files in `tests/fixtures/settings/`.
- `settings generate` on `tests/fixtures/powerops-seed/v1` matches the golden `settings-generate.json`, and the same output with values filled in validates clean against that solution.
- The README usage block lists both commands. `dotnet build`, `dotnet test` and `dotnet format --verify-no-changes` are clean.

## Users

- Makers and CI pipelines that deploy a solution with `pac solution import --settings-file` or Pipelines, and want the file checked before import instead of failing during it.
- Agents in the user's Claude Code or Codex that run `powerops settings validate --format json`.

## Data

Input only: a solution (folder or zip, read through the existing `SolutionLoader`) and, for `validate`, one settings file. Nothing is written except the `--out` file.

The settings file format is the one in Learn "Pre-populate connection references and environment variables for automated deployments": a root object with `EnvironmentVariables` (`SchemaName`, `Value`) and `ConnectionReferences` (`LogicalName`, `ConnectionId`, `ConnectorId`).

Observed on 2026-10-20 (`pac` 2.13.1, `pac solution create-settings --solution-folder` on `powerops-seed/v1`, offline): `pac` writes `Value` and `ConnectionId` as empty strings, adds descriptive fields to each environment variable (`DefaultValue`, `Name`, `TypeId`, `Description`, `IsRequired`) and adds a top-level `CopilotAgents` array. `validate` ignores fields and arrays it does not use, so a file made by `pac` passes.

The solution model already holds what both commands need: connection references (logical name, connector id) and environment variables (schema name, required flag, default value, current value). No model change.

## Automation

Offline half (built):

- `settings generate <solution> [--out <file>]` prints a settings file with exactly the Learn fields, `Value` and `ConnectionId` empty, `ConnectorId` from the solution, each array sorted by name (ordinal). Stdout by default; a file only with `--out`.
- `settings validate <solution> --settings <file> [--format text|json]` reports five kinds of problem:
  - `missing`: an environment variable or connection reference in the solution but not in the file.
  - `extra`: an entry in the file whose name is not in the solution.
  - `duplicate`: a name listed more than once in the file (compared regardless of case). It is reported once per name, and the first entry is the one checked.
  - `emptyConnectionId`: a connection reference in both whose `ConnectionId` is missing, null or blank.
  - `emptyValue`: an environment variable in both whose `Value` is missing, null or empty, when the solution gives it no default value and no current value. This fires for optional variables too (user decision), because a flow reading such a variable gets no value.
- Exit codes: `0` clean, `1` problems, `2` usage error (bad option, solution path or settings file not found), `3` unreadable solution, a settings file that is not valid JSON or has the wrong shape, or an internal error such as `--out` into a folder that does not exist.
- `--format json` follows the M4 contract: `schemaVersion` first, `docs/schemas/settings-validate.schema.json`, a golden checked by `OutputSchemaTests`.

Live half (later plan, not built): `--apply` binds connection references and turns flows on after import. Research so far, kept locally in the main checkout's `.claude/long-task/sources/task.md` (not committed) and to be verified again in that plan:

- Binding a connection reference is an update of its `connectionid` on the Dataverse Web API `connectionreference` table. Turning a cloud flow on is an update of `statecode` and `statuscode` on the `workflow` table (cloud flows are category 5).
- Activation caveat: turning a flow on fails (`ConnectionAuthorizationFailed`) unless the user who activates it has access to every connection its connection references use.
- Connection access caveat: the connection ids in the settings file must belong to connections that user can use in the target environment; `--apply` cannot create or share connections.
- The live done-when ("a live import round-trip in the test environment leaves the flows on") moves to that plan. It writes to a tenant, so it needs the user's yes before any run.

## Screens

None. Command-line only. Text output is one line per problem (`<kind> <componentType> <name>: <message>`) and a summary line (`no problems`, `1 problem`, `N problems`).

## Environments and route

- The offline half needs no environment; `environment` stays null. It ships through the normal release as part of v0.8.0.
- The live half's environment and route are decided in its own plan.

## Solution and publisher

None created or changed. Tests use the committed `PowerOpsSeed` fixture (prefix `pops`) and two hand-made settings files next to the fixtures.

## Constraints

- Offline first and read-only. `pac` is never called by either command.
- Smallest complete change: no options beyond `--out`, `--settings` and `--format`; no new dependencies.
- Fixtures hold fake values only (no tenant URLs, environment or connection ids, names or emails) and are described in `tests/fixtures/NOTICE.md`.
- Licensing and DLP do not apply to the offline commands. They apply to the live half and are asked in its plan.
- How `pac` import treats missing or extra entries is not documented, so the docs and messages make no claim about it. Whether Secret or data source environment variable types appear in the file is unverified; `validate` treats every type the same.

## Assumptions

- Dataverse schema and logical names are matched case-insensitively between the file and the solution (user decision).
- A settings entry without its name field (`SchemaName` or `LogicalName`), or with a name that is not a string, makes the whole file invalid (exit 3).
- A `ConnectionId` that is not a string counts as empty; a non-string `Value` counts as set.
- The solution's `IsRequired` flag, not the file's, is used, and only to add "(required)" to the `emptyValue` message (user decision).
- Known gap (accepted by the user): no test runs on a managed export. The tests use the unmanaged seed folder and a zip made from it.

## Open questions

## Questions for your admin

## Steps

Steps 1 to 6 and 8 are done; 7 is open.

1. `feat(core):` `DeploymentSettings.Generate` and `DeploymentSettings.Validate` in `src/PowerOps.Core/Settings/`, with `SettingsReport` (text and JSON) in `src/PowerOps.Core/Output/`. Core tests in `tests/PowerOps.Core.Tests/Settings/` fire and stay quiet for each problem kind, ignore unknown fields, and reject wrong shapes. Done in "feat(settings): add settings generate and validate"; the `ConnectionId` check was corrected in "fix(settings): count a non-string ConnectionId as empty and use pac's real output as the valid fixture" (first review R1).
2. `feat(settings):` the thin `settings` command with `generate` and `validate` in `src/PowerOps.Cli/SettingsCommand.cs`, registered in `Program.cs`, with the exit codes under Automation. Done, same commit.
3. `test:` fixtures `tests/fixtures/settings/powerops-seed-v1.valid.json` and `powerops-seed-v1.problems.json` (one missing connection reference, one extra environment variable, one extra connection reference; Step 8 adds a duplicate), described in `NOTICE.md`; `analyze` does not pick them up, so the `analyze-fixtures.*` goldens are unchanged. Done, same commit. The valid file was hand-written at first; the fix commit named in Step 1 replaced it with `pac` 2.13.1's real `create-settings` output, values filled with fake ones (first review R5).
4. `test:` CLI tests and goldens `settings-generate.json`, `settings-validate.txt` and `settings-validate.json`; the JSON schema, its row in `docs/schemas/README.md` and its `Map` entry in `OutputSchemaTests`. The generate golden is skipped by `OutputSchemaTests` because it is `pac`'s file format, not a powerops output. Done, same commit.
5. `docs:` README usage block and a paragraph with the exit codes. Done, same commit.
6. `test:` `generate --out` into a missing folder exits 3 and writes nothing; `generate` on the zipped seed matches the unpacked folder and the valid settings file validates clean against the zip. Done in "test(settings): cover generate --out into a missing folder and the zip layout".
7. Open items before merge:
   1. The README conflicts with M6 (`diff`): both branches add a usage line and a paragraph at the same two places. Resolve by keeping both.
   2. The README "Status" line still says `analyze` is the only command. It was already out of date before M7; fix it in a separate `docs:` change.
   3. The BUILD-PLAN M7 row keeps both halves; mark the offline half done when this merges.
8. `feat(settings):` the `duplicate` problem kind (review R3, user decision): a Core test that failed first, the schema's `kind` enum and description, a duplicate in different case in `powerops-seed-v1.problems.json`, updated `settings-validate.*` goldens and README sentence. Adding an enum value keeps `schemaVersion` 1. Done in "feat(settings): report a name listed more than once as a duplicate problem".
