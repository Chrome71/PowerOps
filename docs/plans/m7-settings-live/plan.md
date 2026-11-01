---
status: done
review: done
signed-off: 2026-11-01
signed-off-hash: fcea03e0cc1858d0ffa68eae8e0df18e03fe219888e1e0f65bab2847a3de99db
environment: null
depth: quick
supersedes: null
---

# M7: settings (live half)

## Goal

`powerops settings apply` imports an exported solution zip with a deployment settings file through `pac`, so the connection references are bound and the cloud flows come back on, then reports the state of each of the solution's cloud flows. Without `--apply` it is a dry run that writes nothing. This is the live half of the BUILD-PLAN M7 row ("bind connection references and turn flows on after import, behind an explicit `--apply` flag"). The offline half (`settings generate`, `settings validate`) is the signed-off plan `docs/plans/m7-settings/`. Release v0.8.0 together with it.

How the live half meets the row, from Microsoft Learn (Sources under Constraints): `pac` has no command that binds a connection reference or turns a cloud flow on. `pac solution import --settings-file` binds the connection references at import. A flow that is new in the target comes back in the state it had at export, so a flow that was on comes back on when its connection references get connections. A flow that already exists in the target keeps its current state. So powerops does the binding and turning on through `pac`, predicts the result in the dry run, and checks it after the import. It does not turn on a flow that `pac` left off (Q1).

Done when:

- Offline: the tests under Steps pass with a fake `pac` (no tenant), the JSON matches `docs/schemas/settings-apply.schema.json` and its goldens, and `dotnet build`, `dotnet test` and `dotnet format --verify-no-changes` are clean.
- Live (BUILD-PLAN M7 done-when), run by the user in the developer test environment, in this order (Q5, Q8). Before items 2 and 4 the user removes the seed: the seed is unmanaged there, and deleting an unmanaged solution leaves its components, so the user deletes the solution and each of its components (flow, canvas app, table, environment variable, connection reference). The user keeps two exports of the seed made beforehand: one with its flow on, one with it off.
  1. With the seed removed, the dry run against the flow-on zip prints the target environment, `managed: false`, the `pac` command and the flow as `new`, expected `on`, and exits 0. `pac solution list` run by the user before and after shows the same solutions and versions.
  2. `--apply --target <name>` imports the flow-on zip with a real settings file kept outside the repo; the report shows the flow `new` and now `on` (the prediction of item 1), exit 0.
  3. Run again over the installed seed: the dry run reports the flow `exists` and `on`, and `--apply` reports its state unchanged (`on`), exit 0.
  4. With the seed removed again, the dry run against the flow-off zip predicts `new`, expected `off`; `--apply` imports it and the report shows the flow `off`, exit 1.
  5. Starting from the seed installed by item 4, `--apply` with a settings file whose `ConnectionId` is fake. Learn does not say how an unknown connection id is handled, so two outcomes are accepted and the one seen is recorded: the import fails (exit 3, the report holds `pac`'s exit code and none of its text), or it imports and the flow stays `off` (exit 1). The second outcome only records that `pac` does not reject an unknown connection id at import; it shows nothing about the flow, which already existed and was off. If the import does not fail, the exit-3 path is covered only by the offline tests with a fake `pac`.

## Users

- Makers and CI pipelines that deploy an exported solution with a deployment settings file and want one checked step: validate, import, confirm the flows are on.
- Agents in the user's Claude Code or Codex that run `powerops settings apply ... --format json`, first without `--apply` to show the user the target and what will happen, then with `--apply --target <name>` after the user says yes.

## Data

Inputs: an exported solution zip (read through the existing `SolutionLoader`) and one deployment settings file. Nothing is written to disk. The settings file is passed to `pac` by path and never copied or printed.

From the zip, powerops uses the connection references and environment variables (for validate, as in the offline half), the `Managed` flag, and the cloud flows: workflows with `Category` 5, with their `Id`, `Name` and export `StateCode`. The model already holds these (`Solution` and `Workflow` in `src/PowerOps.Core/Model/Solution.cs`). No model change.

From `pac` (all through the existing `PacRunner`):

- `pac org who`: the target environment's friendly name and environment id. `PlanFacts` already reads the name from the `Friendly Name:` line; the id comes from the `Environment ID:` line of the same output. Only the name is reported. The id is held in memory and passed to `pac` as `--environment`, never printed or stored.
- `pac power-automate list-cloud-flows --environment <id> --workflow-id <flow id> --json`, once per cloud flow of the solution: that flow's state in the target, or nothing when it does not exist there. The command is Preview and Learn does not document its JSON fields (Q4), so Step 1 captures real samples first.

The report never prints connection ids, environment variable values, environment URLs or ids, user names, or `pac`'s own output text. Paths are printed as the user typed them. Flow and connection reference names come from the solution.

## Automation

`powerops settings apply <zip> --settings <file> [--apply --target <name>] [--format text|json]` runs these steps in order and stops at the first that fails (Q3). There is no rollback: `pac` has no undo for an import, and rerunning is the recovery.

1. **validate**: `DeploymentSettings.Validate` on the zip and the file, the same check as `settings validate`. Any problem stops here: exit 1, nothing else runs.
2. **target**: `pac org who` for the environment name and id. A failure (pac not found, not signed in, timeout, no name or id in the output) stops here: exit 3, with the sign-in hint `plan facts` already prints. With `--apply`, `--target` must equal the name, compared ordinally; a different name exits 2 before anything is written (Q7). Every later `pac` call gets `--environment <id>`, so a profile switch during the run cannot change where it writes.
3. **plan**: for each cloud flow, read its current state with `list-cloud-flows --workflow-id` and report it as `exists` (with its state, which the import keeps) or `new` (expected to come back in its export state: `on` if it was on at export, else `off`). `new` also means "not visible to this account", which the README says. The report also shows the target name, `managed` from the zip, and the argument list of the import with the id replaced by the placeholder `<environment id>` and the paths as typed. Without `--apply`, exit 0 here; nothing more runs.
4. **import** (only with `--apply`): `pac solution import --environment <id> --path <zip> --settings-file <file>`, with no other options. The import is synchronous: no `--async`, so `pac` waits for the import itself. The 60-minute timeout is powerops' own limit (Learn documents no limit for a synchronous import); the short reads keep 30 seconds. The other `pac` defaults apply: unmanaged customisations are not overwritten (no `--force-overwrite`), changes are not published (no `--publish-changes`), and there is no upgrade (no `--stage-and-upgrade`). A non-zero exit stops here: exit 3; the report holds `pac`'s exit code and a fixed hint, and `pac`'s own output, its stdout and then its stderr, goes unchanged to powerops' stderr (Q9). A timeout stops here too, exit 3, with the message that the import may still be running in the environment and must be checked before a rerun; `PacRunner` keeps no output on a timeout, so nothing of `pac`'s is forwarded.
5. **flows** (only with `--apply`): read each flow again as in step 3 and report it as `on`, `off`, `suspended` (the state Learn gives a flow that breaks a DLP policy, Q6) or `missing` (no result for its id). Exit 0 when every flow is on, 1 when any is not. A solution with no cloud flows has an empty `flows` list and exits 0 after a successful import. Output from `pac` that cannot be read exits 3.

Usage errors come first, before validate and before any `pac` call, with exit 2: a missing path, a folder instead of a zip, a missing settings file, `--apply` without `--target`, and `--target` without `--apply`. Only the name comparison waits for step 2.

Exit codes: `0` ok (dry run done, or every flow on), `1` validate problems or a flow not on, `2` usage error (bad option, path not found, a folder instead of a zip, `--apply` without `--target`, or a `--target` that does not match), `3` `pac` not found, not signed in, import failed or timed out, unreadable `pac` output, or an unreadable solution or settings file.

`PacResult` gains the stderr text (so a failed import's whole output can be passed to powerops' stderr). `PacRunner.Run` keeps its signature and its constructor timeout. The `apply` command takes a runner function of type `Func<IReadOnlyList<string>, TimeSpan, PacResult>`, which `Program` builds as `(args, timeout) => new PacRunner(timeout: timeout).Run(args)`, so one function serves the 30-second reads and the 60-minute import. `PlanFacts.Run`, `PlanCommand` and the `PlanFactsTests` fakes keep their one-argument type and do not change.

`--format json` follows the M4 contract: `schemaVersion` first, `docs/schemas/settings-apply.schema.json`, its row in `docs/schemas/README.md`, a `Map` entry in `OutputSchemaTests` and goldens. The report holds `applied` (true only with `--apply`), the environment name, `managed`, the import arguments, a `steps` list (name and `ok`, `failed`, `skipped` or `planned`), the validate problems in the `settings validate` shape, `pacExitCode` when the import failed, and a `flows` list (name, export state, `exists`, state before, expected state, state after or `null`).

Out of scope (not asked for): turning on flows that the import leaves off, binding through the Web API, picking another environment than the pac profile's (the standing decision prefers the current environment), `--stage-and-upgrade` and the other import options, folders as input (`pac solution import` takes a zip), and judging whether a managed or unmanaged zip suits the target.

## Screens

None. Command-line only. With `--apply` in text mode the first line is the target environment name, printed before the import starts. In JSON mode the report is one document written at the end, and the same target line goes to stderr before the import starts. Then one line per step (`<step>: <status>`), one line per flow (`flow <name>: <state>`), and a summary line.

## Environments and route

- Target: the environment selected in the user's pac profile when the command runs, pinned for the run by its id and confirmed by `--target <name>` (Q7). The plan names no environment; `environment` stays null because `powerops plan facts` was not run (planning must not contact the tenant).
- Route for the tool's use: it imports whatever zip it is given into the confirmed target. Learn's route (unmanaged in Dev, managed to Test and Prod) is the user's to follow; powerops shows `managed` in the dry run and does not enforce it. Who may run `--apply` against Test or Prod is an admin question below. Agents follow the README rule: dry run first, show the user the target line, run `--apply --target <name>` only after a yes, and never against an environment the user did not name.
- Live verification: the user's developer test environment. The user runs every live step (Q5) and enters all credentials; an agent runs the dry run only, and `--apply` only after the user says yes.
- Auth: the pac profile only (Q1). powerops signs in to nothing itself.
- Route of the code: the normal PR to main and the v0.8.0 release, as for the offline half.

## Solution and publisher

None created. The live check imports the `PowerOpsSeed` solution (prefix `pops`), exported as unmanaged zips by the user from the test environment, where it was built. A managed export cannot be imported there (Learn: not into the environment that holds the originating unmanaged solution), so the live check uses unmanaged zips. The zips used live are not committed.

## Constraints

- Read-only by default: only `--apply --target <name>` writes to a tenant, and only through `pac solution import`. `pac` is a subprocess with arguments as a list (the existing `PacRunner`); powerops reimplements nothing.
- No new dependencies, no new options beyond `--settings`, `--apply`, `--target` and `--format`.
- Tests never call a real `pac`: the command takes the `pac` runner as a function, the way `PlanFacts.Run` does, and tests pass a fake.
- Fixtures and goldens hold fake values only (fake environment name and id, the seed's own component ids, relative paths, no connection ids or URLs). The captured `pac` samples are scrubbed before commit and described in `tests/fixtures/NOTICE.md`.
- The import turns a solution's flows off and on again while it runs (Learn), so a running flow is briefly interrupted. The README says so.
- An `--apply` run can take up to an hour. The README tells agents to run it in the background or with a long command timeout; killing powerops does not stop an import already running in the environment.
- Licensing: the seed flow uses the Dataverse connector, which is premium in Power Automate; a user needs a premium entitlement (or the flow a Process licence) to run it. Assumed covered by the developer environment's plan for the test (Q6).
- DLP: a flow that breaks a data policy is suspended and does not run; powerops reports it as `suspended`, not as an import failure (Q6).
- Activation caveat: a flow turns on only if the importing user owns or can use every connection in its connection references; otherwise it stays off (Learn names the error `ConnectionAuthorizationFailed`). After import, the importing user owns the components. powerops cannot create or share connections, so it reports such a flow as `off` and the README names the cause.
- Sources, checked on Microsoft Learn on 2026-10-20 (`pac` 2.13.1 help read locally):
  - "pac solution" (learn.microsoft.com/power-platform/developer/cli/reference/solution): `import --settings-file`, `--environment`, `--activate-plugins` ("plug-ins and workflows", not documented for cloud flows), no exit codes documented.
  - "Pre-populate connection references and environment variables for automated deployments" (learn.microsoft.com/power-platform/alm/conn-ref-env-variables-build-tools): the settings file sets connection references and environment variables at import; connections must be owned by or shared with the connection reference owner.
  - "Import a solution" (learn.microsoft.com/power-automate/import-flow-solution): import restores a new flow to its export state; a flow that was on comes back on when its connection references get connections; an update of an existing flow does not change its state; flows are turned off and on during import.
  - "Use a connection reference in a solution" (learn.microsoft.com/power-apps/maker/data-platform/create-connection-reference): the activating user must own or have access to all the flow's connections; `ConnectionAuthorizationFailed`.
  - "Create a solution", section "Delete a solution" (learn.microsoft.com/power-apps/maker/data-platform/create-solution) and "Solution concepts" (learn.microsoft.com/power-platform/alm/solution-concepts-alm): deleting an unmanaged solution leaves its components; a managed solution cannot be imported into the environment holding its originating unmanaged solution.
  - "Microsoft Power Platform CLI command groups", "pac connection", "pac env", "pac power-automate" (learn.microsoft.com/power-platform/developer/cli/reference/): no command binds a connection reference or turns a flow on; `pac env fetch` has no JSON output; `pac power-automate list-cloud-flows` (Preview) has `--workflow-id` and `--environment`; `--json` appears only in the `--columns` text ("Use --json for the complete response"), here and in `pac` 2.13.1's local help.
  - "Work with cloud flows using code" (learn.microsoft.com/power-automate/manage-flows-with-code) and "Process (Workflow) table" (learn.microsoft.com/power-apps/developer/data-platform/reference/entities/workflow): category 5 is a cloud flow; statecode 0 off, 1 on, 2 suspended (statuscode 3 `CompanyDLPViolation`).
  - "Impact of data policies on apps and flows" (learn.microsoft.com/power-platform/admin/dlp-impact-policies-apps-flows): a flow that violates a policy is suspended.
  - "Types of Power Automate licenses" (learn.microsoft.com/power-platform/admin/power-automate-licensing/types): standard and premium connector entitlements.

## Assumptions

- `pac solution import` exits non-zero when the import fails. Learn does not document it; live item 5 checks it.
- `pac power-automate list-cloud-flows --workflow-id <id> --json` accepts `--json` and returns the flow's state in a field Step 1 finds, and returns no flow when the id does not exist. If it does not, the plan goes back to Q4 (the `pac env fetch` option) before Step 2.
- The import user can run `list-cloud-flows`. Learn describes `pac power-automate` as an admin tool; the user is the admin of the developer environment, and live items 1 to 4 confirm it. For other users it is an admin question below. A user who cannot see the flow would get `missing`, and the README says so.
- The workflow id in the exported zip is the id of the flow after import, so matching by id works. Learn does not state it; live items 2 and 3 check it.
- `pac org who` prints an `Environment ID:` line (the existing `PlanFactsTests` fake has one); Step 1 confirms it on the real output. If it does not, the plan goes back to Q7 before Step 2.
- Environment facts were not available: `powerops plan facts` was not run during planning.
- The import uses `pac`'s defaults (no `--stage-and-upgrade`), so components removed from the solution are not deleted in the target; that is the offline `diff` command's concern, not this one.

## Open questions

## Questions for your admin

- Does the developer environment's licence let the importing user run cloud flows with premium connectors (the Dataverse connector), or does the flow need a Process licence?
- Is a DLP policy applied to the developer environment that puts the Dataverse connector in a group that would suspend the seed flow?
- Which account or service principal runs `--apply` in Test and Prod, with which security role, who owns the connections it binds, and can that account run `pac power-automate list-cloud-flows`?

## Steps

1. Capture (user, live, read-only, with the user's yes): with the seed installed, run `pac org who` and `pac power-automate list-cloud-flows --workflow-id <seed flow id> --json` once with the flow on and once with it off, and once with an id that does not exist. Scrub each (environment ids and URLs, user ids and names, connection ids become fake values; the seed's own workflow id stays) into `tests/fixtures/pac/` and describe them in `tests/fixtures/NOTICE.md`. Record which fields hold the id and the state, and which stream carries `pac`'s errors. The `suspended` shape is not observed; its test uses a hand-made sample, and the NOTICE says so.
2. `feat(core):` the flow-state reading in `src/PowerOps.Core/Settings/` (parse one `list-cloud-flows` result, map it to `on`, `off`, `suspended`, `missing`; predict `new` and `exists` results) and the `settings apply` report in `src/PowerOps.Core/Output/` (text and JSON). Core tests, each written to fail first: every state, the predictions for `new` on and off and for `exists`, unreadable JSON.
3. `refactor(cli):` `PacResult` gains a trailing `string Stderr = ""` parameter, so existing constructor calls compile unchanged, and `PacRunner.Run` fills it. `PacRunner.Run`, `PlanFacts` and their tests keep their signatures.
4. `feat(settings):` the `apply` subcommand in `src/PowerOps.Cli/SettingsCommand.cs` with the steps and exit codes under Automation. The seam: `SettingsCommand.Create(output, error, pac)` takes the runner function; `Program.Run` passes the real `PacRunner`, and a public `Program.Run(args, output, error, pac)` overload lets CLI tests go through the parser with a fake (no `InternalsVisibleTo` needed). Test zips are built in a temp folder with `SolutionZip.Create` from `tests/fixtures/powerops-seed/v1`, and the flow-off zip from a temp copy with the flow's `StateCode` set to 0; no zip is committed. CLI tests with a fake `pac`: the dry run makes no import call; validate problems stop before any `pac` call; `pac` not found, sign-in failure and timeout at the target step (exit 3, no import call); `--apply` without `--target` and with a wrong name (exit 2, no import call); every later call carries `--environment` with the id from `pac org who`; import failure (exit 3, `pac`'s stdout and stderr text on powerops' stderr only, not in the report); a fake `org who` with a known environment id, asserting the id appears nowhere in stdout, the report or powerops' own stderr lines and import timeout (exit 3, the "may still be running" message); all flows on (0); a flow off (1); suspended (1); missing (1); a folder instead of a zip (2); a missing settings file (2) and an unreadable one (3); usage errors before validate (`--apply` without `--target` and with validate problems gives 2); `--target` without `--apply` (2); a solution with no cloud flows (0); paths printed as typed; the target line on stderr before the import in JSON mode.
5. `test:` the JSON schema `docs/schemas/settings-apply.schema.json`, its README row, the `OutputSchemaTests` `Map` entry, and goldens `settings-apply-dry-run.txt`, `settings-apply-dry-run.json`, `settings-apply.json` (all on), `settings-apply-off.json` (a flow off and one suspended) and `settings-apply-import-failed.json`.
6. `docs:` README usage block and a paragraph: dry run first, `--apply --target <name>` writes to the pac profile's current environment, the agent rule under Environments and route, the exit codes, the activation caveat, that a new flow off at export stays off and an existing flow keeps its state, that the import briefly cycles the flows, the one-hour run time, the import defaults (no overwrite of unmanaged customisations, no publish, no upgrade), that a user who cannot list flows sees `new` before and `missing` after, that a solution with a flow off at export always exits 1 (by design: the command checks that flows are on), that the flow check makes two `pac` reads per flow and a read slower than 30 seconds ends the run with exit 3, and that environment friendly names are not unique, so the user checks the pac profile (`pac auth list`) before saying yes to a target. Add the agent rule to `AGENTS.md` under Rules ("never run `powerops settings apply --apply` unless the user said yes to that environment in this session") and to the `powerops-implement` skill, which runs `powerops` commands. No technical gate beyond `--target` is added (Q7).
7. Mark the M7 row in `docs/BUILD-PLAN.md` live half built when this merges, and update its line saying writes through `pac` wait until M7, plus any README sentence saying the CLI never writes through `pac`.
8. Live check (user, each run with the user's yes): the five live done-when items under Goal, in order. The user writes the results in the PR description by hand, with no tenant data and no `pac` output text. This also confirms the Assumptions marked as checked live.
