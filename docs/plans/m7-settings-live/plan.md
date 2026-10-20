---
status: approved
review: pending
signed-off: null
signed-off-hash: null
environment: null
depth: quick
supersedes: null
---

# M7: settings (live half)

## Goal

`powerops settings apply` imports an exported solution zip with a deployment settings file through `pac`, so the connection references are bound and the cloud flows come back on, then reports the state of each of the solution's cloud flows. Without `--apply` it is a dry run that writes nothing. This is the live half of the BUILD-PLAN M7 row ("bind connection references and turn flows on after import, behind an explicit `--apply` flag"). The offline half (`settings generate`, `settings validate`) is the signed-off plan `docs/plans/m7-settings/`. Release v0.8.0 together with it.

How the live half meets the row, from Microsoft Learn (Sources under Constraints): `pac` has no command that binds a connection reference or turns a cloud flow on. `pac solution import --settings-file` binds the connection references at import, and the import restores each flow to the state it had at export: a flow that was on comes back on when its connection references get connections. So powerops does the binding and turning on through `pac`, and checks the result. It does not turn on a flow that `pac` left off (Q1).

Done when:

- Offline: the tests under Steps pass with a fake `pac` (no tenant), the JSON matches `docs/schemas/settings-apply.schema.json` and its golden, and `dotnet build`, `dotnet test` and `dotnet format --verify-no-changes` are clean.
- Live (BUILD-PLAN M7 done-when), run by the user in the developer test environment before it is suspended on 2026-12-15 (Q5):
  1. The dry run against the seed zip prints the target environment, the `pac` command and the flows, exits 0, and changes nothing in the environment.
  2. `--apply` imports the seed exported with its flow on, using a real settings file kept outside the repo; the report shows the flow on and exits 0.
  3. An import whose flow stays off (the seed exported with its flow off) reports that flow as off and exits 1.

## Users

- Makers and CI pipelines that deploy an exported solution with a deployment settings file and want one checked step: validate, import, confirm the flows are on.
- Agents in the user's Claude Code or Codex that run `powerops settings apply ... --format json`, first without `--apply` to show the user what will happen, then with it after the user says yes.

## Data

Inputs: an exported solution zip (read through the existing `SolutionLoader`) and one deployment settings file. Nothing is written to disk. The settings file is passed to `pac` by path and never copied or printed.

From the zip, powerops uses the connection references and environment variables (for validate, as in the offline half) and the cloud flows: workflows with `Category` 5, with their `Id`, `Name` and export `StateCode`. The model already holds these (`Workflow` in `src/PowerOps.Core/Model/Solution.cs`). No model change.

From `pac` (all read through the existing `PacRunner`):

- `pac org who`: the target environment's friendly name, as `powerops plan facts` reads it. Only the name is reported, never its URL or id.
- `pac power-automate list-cloud-flows --json`: the environment's cloud flows. powerops matches them to the solution's cloud flows by workflow id and reads each one's state. The command is Preview and Learn does not document its JSON fields (Q4), so Step 1 captures one real sample first.

The report never prints connection ids, environment variable values, environment URLs or ids, or user names. Flow and connection reference names come from the solution.

## Automation

`powerops settings apply <zip> --settings <file> [--apply] [--format text|json]` runs these steps in order and stops at the first that fails (Q3). There is no rollback: `pac` has no undo for an import, and rerunning is the recovery.

1. **validate**: `DeploymentSettings.Validate` on the zip and the file, the same check as `settings validate`. Any problem stops here: exit 1, nothing else runs.
2. **target**: `pac org who` for the environment name. A failure stops here: exit 3, with the `pac` sign-in hint `plan facts` already prints.
3. **plan** (dry run, the default): report the target environment, the exact argument list of the import (`pac solution import --path <zip> --settings-file <file>`), and each cloud flow with its export state and what Learn says will happen to it: on at export means "expected on after import", off at export means "will stay off". Exit 0. Nothing more runs.
4. **import** (only with `--apply`): run `pac solution import --path <zip> --settings-file <file>` with no other options. A non-zero exit or a timeout stops here: exit 3, and the report includes the last lines of `pac`'s output. The timeout is 60 minutes, matching `pac`'s own default wait.
5. **flows** (only with `--apply`): run `pac power-automate list-cloud-flows --json` and report each of the solution's cloud flows as `on`, `off`, `suspended` (the state Learn gives a flow that breaks a DLP policy, Q6) or `missing` (not found by id). Exit 0 when every flow is on, 1 when any is not. Output `pac` printed that cannot be read exits 3.

Exit codes: `0` ok (dry run done, or every flow on), `1` validate problems or a flow not on, `2` usage error (bad option, path not found, or a folder instead of a zip), `3` `pac` not found, not signed in, import failed or timed out, unreadable `pac` output, or an unreadable solution or settings file.

`--format json` follows the M4 contract: `schemaVersion` first, `docs/schemas/settings-apply.schema.json`, its row in `docs/schemas/README.md`, a `Map` entry in `OutputSchemaTests` and goldens. The report holds `applied` (true only with `--apply`), the environment name, a `steps` list (name and `ok`, `failed`, `skipped` or `planned`), the validate problems in the `settings validate` shape, and a `flows` list (name, export state, state after import or `null` in a dry run).

Out of scope (not asked for): turning on flows that the import leaves off, binding through the Web API, an `--environment` option (the pac profile's current environment is the target, as the standing decision on environments prefers), `--stage-and-upgrade` and the other import options, and folders as input (`pac solution import` takes a zip).

## Screens

None. Command-line only. Text output is one line per step (`<step>: <status>`), then one line per flow (`flow <name>: <state>`), then a summary line.

## Environments and route

- Target: the environment selected in the user's pac profile when the command runs. The plan names no environment; `environment` stays null because `powerops plan facts` was not run (planning must not contact the tenant).
- Live verification: the user's developer test environment, before it is suspended on 2026-12-15. The user runs every live step (Q5) and enters all credentials; an agent runs the dry run only, and `--apply` only after the user says yes.
- Auth: the pac profile only (Q1). powerops signs in to nothing itself.
- Route of the code: the normal PR to main and the v0.8.0 release, as for the offline half.

## Solution and publisher

None created. The live check imports the committed `PowerOpsSeed` solution (prefix `pops`), re-exported as a zip by the user from the test environment. The zips used live are not committed.

## Constraints

- Read-only by default: only `--apply` writes to a tenant, and only through `pac solution import`. `pac` is a subprocess with arguments as a list (the existing `PacRunner`); powerops reimplements nothing.
- No new dependencies, no new options beyond `--settings`, `--apply` and `--format`.
- Tests never call a real `pac`: the command takes the `pac` runner as a function, the way `PlanFacts.Run` does, and tests pass a fake.
- Fixtures and goldens hold fake values only (fake environment name, the seed's component ids, no connection ids or URLs). The captured `list-cloud-flows` sample is scrubbed before commit and described in `tests/fixtures/NOTICE.md`.
- Licensing: the seed flow uses the Dataverse connector, which is premium in Power Automate; a user needs a premium entitlement (or the flow a Process licence) to run it. Assumed covered by the developer environment's plan for the test (Q6).
- DLP: a flow that breaks a data policy is suspended and does not run; powerops reports it as `suspended`, not as an import failure (Q6).
- Activation caveat: a flow turns on only if the importing user owns or can use every connection in its connection references; otherwise it stays off (Learn names the error `ConnectionAuthorizationFailed`). powerops cannot create or share connections, so it reports such a flow as `off` and the docs name the cause.
- Deadline: live verification before 2026-12-15.
- Sources, checked on Microsoft Learn on 2026-10-20 (`pac` 2.13.1 help read locally):
  - "pac solution" (learn.microsoft.com/power-platform/developer/cli/reference/solution): `import --settings-file`, `--activate-plugins` ("plug-ins and workflows", not documented for cloud flows), no exit codes documented.
  - "Pre-populate connection references and environment variables for automated deployments" (learn.microsoft.com/power-platform/alm/conn-ref-env-variables-build-tools): the settings file sets connection references and environment variables at import; connections must be owned by or shared with the connection reference owner.
  - "Import a solution" (learn.microsoft.com/power-automate/import-flow-solution): import restores flows to their export state; a flow that was on comes back on when its connection references get connections; an update of an existing flow does not change its state.
  - "Use a connection reference in a solution" (learn.microsoft.com/power-apps/maker/data-platform/create-connection-reference): the activating user must own or have access to all the flow's connections; `ConnectionAuthorizationFailed`.
  - "Microsoft Power Platform CLI command groups", "pac connection", "pac env", "pac power-automate" (learn.microsoft.com/power-platform/developer/cli/reference/): no command binds a connection reference or turns a flow on; `pac env fetch` has no JSON output; `pac power-automate list-cloud-flows` has `--json` (Preview).
  - "Work with cloud flows using code" (learn.microsoft.com/power-automate/manage-flows-with-code) and "Process (Workflow) table" (learn.microsoft.com/power-apps/developer/data-platform/reference/entities/workflow): category 5 is a cloud flow; statecode 0 off, 1 on, 2 suspended (statuscode 3 `CompanyDLPViolation`).
  - "Impact of data policies on apps and flows" (learn.microsoft.com/power-platform/admin/dlp-impact-policies-apps-flows): a flow that violates a policy is suspended.
  - "Types of Power Automate licenses" (learn.microsoft.com/power-platform/admin/power-automate-licensing/types): standard and premium connector entitlements.

## Assumptions

- `pac solution import` exits non-zero when the import fails. Learn does not document it; Step 7 checks it live.
- `pac power-automate list-cloud-flows --json` returns each flow's workflow id and state in fields that Step 1 finds. If it does not, the plan goes back to Q4 (the `pac env fetch` option) before Step 3.
- The workflow id in the exported zip is the id of the flow after import, so matching by id works. Learn does not state it; Step 7 checks it.
- Environment facts were not available: `powerops plan facts` was not run during planning.
- The import uses `pac`'s defaults (no `--stage-and-upgrade`), so components removed from the solution are not deleted in the target; that is the offline `diff` command's concern, not this one.

## Open questions

## Questions for your admin

- Does the developer environment's licence let the importing user run cloud flows with premium connectors (the Dataverse connector), or does the flow need a Process licence?
- Is a DLP policy applied to the developer environment that puts the Dataverse connector in a group that would suspend the seed flow?

## Steps

1. Capture (user, live, read-only, with the user's yes): run `pac power-automate list-cloud-flows --json` once in the test environment with the seed installed and its flow on. Scrub it (environment ids and URLs, user ids and names, connection ids become fake values; the seed's own workflow id stays) into `tests/fixtures/pac/list-cloud-flows.json` and describe it in `tests/fixtures/NOTICE.md`. Record which fields hold the workflow id and the state.
2. `feat(core):` a `SettingsApply` report in `src/PowerOps.Core/Output/` (text and JSON) and the flow-state reading in `src/PowerOps.Core/Settings/`: parse the captured JSON, match by id, map the state to `on`, `off`, `suspended`, `missing`. Core tests, each written to fail first: every state, a flow not found, unreadable JSON.
3. `feat(settings):` the `apply` subcommand in `src/PowerOps.Cli/SettingsCommand.cs` with the five steps and exit codes under Automation, taking the `pac` runner as a function. CLI tests with a fake `pac`: dry run (no import call made), validate problems stop before `pac`, `pac` not found, sign-in failure, import failure and timeout (exit 3, no flow check), all flows on (0), a flow off (1), a flow suspended (1), a folder instead of a zip (2).
4. `test:` the JSON schema `docs/schemas/settings-apply.schema.json`, its README row, the `OutputSchemaTests` `Map` entry, and goldens `settings-apply-dry-run.txt`, `settings-apply.json` (after import) and `settings-apply-dry-run.json`.
5. `docs:` README usage block and a paragraph: dry run first, `--apply` writes to the pac profile's current environment, the exit codes, the activation caveat and that a flow off at export stays off.
6. Mark the M7 row in `docs/BUILD-PLAN.md` live half built when this merges.
7. Live check (user, before 2026-12-15, each run with the user's yes): the three live done-when items under Goal, plus one import with a settings file whose `ConnectionId` is fake, to see that a failed import exits 3. Record the results (no tenant data) in the PR description. This also confirms the first and third Assumptions.
