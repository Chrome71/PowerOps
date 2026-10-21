---
status: approved
review: pending
signed-off: null
signed-off-hash: null
environment: null
depth: quick
supersedes: null
---

# M8: drift

## Goal

`powerops drift` compares one solution across two or more Dataverse environments and reports where they differ: the version, the components, unmanaged layers on a managed solution's components, environment variable values, connection reference bindings and cloud flow states. It only reads, through `pac`. It can save what it read as snapshot files, and a later run can compare those files offline. This is the BUILD-PLAN M8 row (release v0.9.0, build-order tasks 22 to 24), and the folded `settings diff` (BUILD-PLAN decision 12). M9's `deploy plan` reuses its snapshot model.

Done when:

- Offline: the tests under Steps pass on recorded `pac` output with a fake `pac` (no tenant). Equal snapshots exit 0. Each of these exits 1 and is named in the output: a version change, an extra component, an unmanaged layer, a changed environment variable value, an unbound connection reference and a flow that is off. A failed or timed-out `pac` read exits 3. The JSON goldens validate against `docs/schemas/drift.schema.json` and `docs/schemas/snapshot.schema.json`. `dotnet build`, `dotnet test` and `dotnet format --verify-no-changes` are clean.
- Live (BUILD-PLAN M8 done-when), in the user's Dev and Test (BUILD-PLAN decision 9), each run read-only and with the user's yes:
  1. With the seed installed in both (unmanaged in Dev, managed in Test, the same environment variable value, its connection reference bound, its flow on), `drift --solution PowerOpsSeed --env <Dev> --env <Test> --save <dir>` reports no drift and exits 0. The managed difference is shown as information (Q3).
  2. `drift --solution PowerOpsSeed --env <dir>/<Dev file> --env <dir>/<Test file>`, offline, prints the same report and exits 0.
  3. After the user edits the seed flow in Test (in the maker portal; powerops writes nothing), a live run reports the unmanaged layer on that flow and exits 1. If Step 1 shows that layers cannot be read, this item is recorded as not supported in the PR instead (Q4).

## Users

- Agents in the user's Claude Code or Codex that run `powerops drift ... --format json` to answer "does Test still match Dev?" before a deployment, and `--save` the snapshots for M9's plan.
- Makers and CI jobs that want a Markdown drift report in a PR or a job summary.

## Data

**Inputs.** `--solution <unique name>` and two or more `--env` values. Each `--env` is either a path to a snapshot file (when a file exists at that path) or an environment, passed unchanged to `pac` as `--environment` (an environment id or URL, as `pac` takes them). The value of an `--env` is never printed: it may be a URL. Output names each environment by its friendly name.

**Reads per live environment** (all read-only, each with `--environment <value>`, each through the existing `PacRunner` with the 30-second read timeout):

1. `pac org who`: the friendly name (the `Friendly Name:` line, read as `settings apply` reads it). Nothing else of its output is kept.
2. `pac env fetch --xml <FetchXML>` on `solution`, filtered on `uniquename`: `solutionid`, `version`, `ismanaged`. No row means the solution is not installed; the other reads are skipped.
3. On `solutioncomponent`, filtered on that `solutionid`: `componenttype`, `objectid`.
4. On `workflow`, the solution's workflows with `category` 5 (cloud flows): `workflowid`, `name`, `statecode` (0 off, 1 on, 2 suspended).
5. On `environmentvariabledefinition` with an outer link to `environmentvariablevalue`, for the solution's definitions: `schemaname` and the current `value`.
6. On `connectionreference`, for the solution's references: `connectionreferencelogicalname`, `connectorid`, and whether `connectionid` is set.
7. On `msdyn_componentlayer`, only where the solution is managed (Q3): which of the solution's components have an unmanaged layer. If Step 1 shows the table cannot be read through `pac env fetch`, this read is dropped and the snapshot says `layersRead: false` (Q4).

The exact FetchXML for each read is written in Step 1, run by the user in the capture, and the build uses the same text. The solution name goes into FetchXML only after it matches `^[A-Za-z0-9_]+$` (unique names allow only letters, digits and underscores); anything else is a usage error. The solution id goes in as a parsed GUID.

Two changes from the BUILD-PLAN row's read list, both smaller: flow states come from the `workflow` table in one `env fetch` (read 4), not from `pac power-automate list-cloud-flows` once per flow, which is Preview, cannot tell a cloud flow from another workflow, and gives no name. `pac solution list` is not read: the solution id is needed for reads 3 to 7, so read 2 replaces it.

**Snapshot** (a Core record, and the snapshot file, Q1): `schemaVersion`, the environment's friendly name, the solution's unique name, `installed`, `version`, `managed`, `layersRead`, and four sorted lists:

- components: `type` (the `componenttype` number), `id` (`objectid`), `unmanagedLayer` (true, false, or null when layers were not read or the solution is unmanaged);
- environment variables: `schemaName`, `valueHash` (the SHA-256 of the current value's UTF-8 bytes, in lowercase hex, or null when there is no current value) (Q2);
- connection references: `logicalName`, `connectorId`, `bound`;
- cloud flows: `id`, `name`, `state` (`on`, `off`, `suspended`).

A snapshot never holds an environment id or URL, a connection id, a value, a user name or `pac`'s own text. The ids it holds are the solution's own component ids, which are in every export of the solution anyway.

**Snapshot files** (Q1). `--save <dir>` creates the folder if needed and writes one file per live `--env`, `<dir>/<solution>.<environment name>.json`, with characters that are invalid in a file name replaced by `-`. An existing file is overwritten. Snapshot-file inputs are not written again. Files are written only after every read succeeded. A snapshot file passed as `--env` must parse, have the current `schemaVersion` and name the same solution (compared ignoring case, as Dataverse compares unique names); otherwise it is unreadable input (exit 3) or, for another solution, a usage error (exit 2).

## Automation

`powerops drift --solution <name> --env <env or file> --env <env or file> [--env ...] [--save <dir>] [--format text|json|markdown] [--out <file>]`

1. **Usage** (exit 2, before any `pac` call): a missing `--solution` or one that fails the name pattern, fewer than two `--env`, `--save` naming an existing file, and a snapshot file for another solution.
2. **Read**: every `--env` in order: a snapshot file is loaded; an environment is read with reads 1 to 7. The first failed, timed-out or unreadable read stops the run with exit 3. The message names the `--env` by its position (`--env 2`) and the `pac` command, never the value; `pac`'s stderr goes unchanged to powerops' stderr, as in `settings apply`. Nothing is saved and no report is written. A read that returns 5,000 rows (one FetchXML page) also exits 3, with "more than one page of results is not supported", because `pac`'s paging is not documented. With `--save`, two live environments with the same friendly name exit 2 before anything is written.
3. **Compare** each environment with the first (BUILD-PLAN row). The result is a list of items, each with the environment, a kind, a subject and whether it is drift:

| Kind | Drift | When |
|---|---|---|
| `solution-missing` | yes | the solution is installed in the first environment and not in this one, or the other way round; nothing else is compared for this pair |
| `version` | yes | the versions differ (the item shows both) |
| `managed` | no | the managed flags differ (Q3); shown as information |
| `component-missing` / `component-extra` | yes | a component (matched by type and id) is in the first environment and not this one, or in this one only |
| `unmanaged-layer` | yes | a component of a managed solution has an unmanaged layer; checked in every environment, the first included |
| `layers-not-read` | no | the layer read was dropped (Q4) and the solution is managed there |
| `variable-missing` | yes | the definition is in the first environment and not this one, or the other way round |
| `variable-value` | yes | the current values differ, including a current value on one side only; the output says `differs` and never shows a value or a hash (Q2) |
| `reference-missing` | yes | as for variables |
| `reference-connector` | yes | the connector differs |
| `reference-unbound` | yes | the reference has no connection; checked in every environment, the first included |
| `flow-missing` | yes | as for variables |
| `flow-state` | yes | the states differ (the item shows both) |

Connection ids are never compared: connections belong to one environment, so a reference bound in both is bound to different ids by design. Default values of environment variables are not compared: they ship in the solution, so a version match covers them.

4. **Report**: items sorted by environment order, then by the kind order of the table, then by subject (ordinal). The subject is the name for flows, variables and references, and `<type> <id>` for other components (powerops reads no display names for them). With `--save`, the files are written now. Exit 0 when no item is drift, 1 when any is.

Exit codes: `0` no drift, `1` drift, `2` usage error, `3` a `pac` read failed, timed out or printed output powerops cannot read, `pac` not found, or an unreadable snapshot file.

`--format json` follows the M4 contract: `schemaVersion` first, `docs/schemas/drift.schema.json`, its row in `docs/schemas/README.md`, a `Map` entry in `OutputSchemaTests` and goldens. The report holds `solution`, `drift` (true or false), `environments` (name, `source` `live` or `snapshot`, `installed`, `version`, `managed`, `layersRead`) and `items` (environment, kind, subject, `drift`, and `expected`/`actual` for `version`, `managed`, `reference-connector` and `flow-state`, else null). The snapshot file has its own schema, `docs/schemas/snapshot.schema.json` (prefix `snapshot`, so it does not clash with the `drift` prefix in `Map`).

The model, the parsers of `pac`'s output, the comparison and the reports live in Core (`src/PowerOps.Core/Drift/` and `src/PowerOps.Core/Output/DriftReport.cs`). `src/PowerOps.Cli/DriftCommand.cs` only runs `pac` and writes files. It takes the existing `Func<IReadOnlyList<string>, TimeSpan, PacResult>` runner that `Program.Run` already passes to `settings`, so tests use a fake. The `Friendly Name:` reader in `SettingsCommand` becomes `internal` and is reused.

Out of scope (not asked for): `--show-values` (Q2), display names of components other than flows, variables and references, comparing default values, comparing each environment with every other one, reading more than one solution per run, paging past 5,000 rows, the Dataverse SDK or Web API (Q4), and any write to an environment.

## Screens

None. Command-line only. Text output: one line per environment (`<name>: <version>, managed|unmanaged[, layers not read]` or `<name>: not installed`), one line per item (`<environment>: <kind> <subject>[: <expected> -> <actual>]`, information items marked `(info)`), and a summary line (`no drift` or `<n> drift items`). Markdown: an environments table and an items table, for a PR comment.

## Environments and route

- Environments: the user's Dev (a managed developer environment) and Test (a sandbox), set up by the user before Step 1 (BUILD-PLAN decision 9). The plan names neither; `environment` stays null because `powerops plan facts` was not run (planning must not contact the tenant). Their ids and URLs live only in the user's shell or gitignored files.
- The seed moves into Dev before Step 1: the user imports the seed (unmanaged) into Dev, then exports it as managed from Dev and imports that into Test. Both imports are the user's, with or without `settings apply`.
- Auth: the user's pac profile. powerops signs in to nothing; `--environment` picks the environment per call, so no `pac org select` is needed.
- Agents run `drift` only against environments the user named in the session. It reads only, so it needs no `--apply`-style flag.
- Route of the code: the normal PR to main and the v0.9.0 release.

## Solution and publisher

None created. The live check reads the existing `PowerOpsSeed` solution (prefix `pops`). No capture or snapshot of the live environments is committed except the scrubbed fixtures of Step 1.

## Constraints

- Read-only: every `pac` call reads. The only file writes are `--save` and `--out`, both explicit. `pac` is a subprocess with arguments as a list; the FetchXML is one argument.
- No new dependencies. SHA-256 comes from the base library.
- Tests never call a real `pac`.
- Fixtures and goldens hold fake values only (environment names such as `Contoso Dev`, fake ids for anything outside the seed, no connection ids, no URLs). Step 1's captures are scrubbed before commit and described in `tests/fixtures/NOTICE.md`.
- A SHA-256 hides a value from a reader, not from someone who guesses short values (a flag, a known URL). Snapshot files are for the user's own folders and private CI artifacts, not public places; the README says so.
- Run time: about six `pac` reads per environment, plus the layer read, each up to 30 seconds. If the layer table must be queried per component, a solution with many components takes one read per component; Step 1 records which, and the README gives the cost.
- Licensing: reading needs no licence beyond access to the environments. In Managed Environments, every user who runs an app or flow needs a premium licence (BUILD-PLAN research basis); the seed flow runs in both, which the user's tenant covers (admin question below).
- Sources, checked on Microsoft Learn on 2026-10-21 (local `pac` 2.13.1 usage text read; no tenant contacted):
  - "pac env" (learn.microsoft.com/power-platform/developer/cli/reference/env): `fetch` takes `--xml`, `--xmlFile` and `--environment` (an id or URL); no output format, paging or virtual-table support is documented; `pac org` is the old name of `pac env`, and `who` takes `--environment`.
  - "pac solution" (learn.microsoft.com/power-platform/developer/cli/reference/solution): no compare command; `list --json` is documented but absent from 2.13.1's usage.
  - "Page results using FetchXML" (learn.microsoft.com/power-apps/developer/data-platform/fetchxml/page-results): 5,000 rows per page unless paged.
  - "Solution", "Solution Component", "Process (Workflow)", "Environment Variable Definition", "Environment Variable Value", "Connection Reference" and "Component Layer (msdyn_componentlayer)" table references (learn.microsoft.com/power-apps/developer/data-platform/reference/entities/): the columns named under Data; component types 29 workflow, 380 environment variable definition, 381 value, none listed for connection reference; workflow `statecode` 0 Draft, 1 Activated, 2 Suspended; `msdyn_componentlayer` is a virtual table with no documented required filter and no unmanaged flag column.
  - "Solution layers" (learn.microsoft.com/power-apps/maker/data-platform/solution-layers and learn.microsoft.com/power-platform/alm/solution-layers-alm): all unmanaged changes share one unmanaged layer on top of the managed layers, typically shown as the Active layer.
  - "Work with cloud flows using code" (learn.microsoft.com/power-automate/manage-flows-with-code): category 5 is a cloud flow; `workflowid` is the same across imports.
  - "Use environment variables for Azure Key Vault secrets" (learn.microsoft.com/power-apps/maker/data-platform/environmentvariables-azure-key-vault-secrets): a secret variable holds a reference to the vault, not the secret.
  - "Use a connection reference in a solution" (learn.microsoft.com/power-apps/maker/data-platform/create-connection-reference): a connection is chosen per reference at import.

## Assumptions

- `pac env fetch` prints output that can be parsed reliably, including a value with spaces, commas or a line break. Learn does not document the format; Step 1 checks it. If it cannot be parsed reliably, the plan goes back to planning before Step 2.
- `pac env fetch` can read `msdyn_componentlayer`, and its rows show which layer is the unmanaged one. Not documented; Step 1 checks it. If not, Q4 applies.
- Component ids (`objectid`) are the same in Dev and Test after an import. Learn states it only for flows; Step 1 compares the seed's component ids in Dev and Test. If they differ, the plan goes back to planning before Step 2.
- The connection reference's component type number is taken from Step 1's capture: Learn does not list it.
- `pac` exits non-zero when a FetchXML query fails. Step 1 checks it with a malformed query.
- A connection reference with an empty `connectionid` is unbound.
- Environment facts were not available: `powerops plan facts` was not run during planning.

## Open questions

## Questions for your admin

- Which security role lets the reading account query the `solution`, `solutioncomponent`, `workflow`, environment variable, `connectionreference` and `msdyn_componentlayer` tables in Test (and later Prod) without write rights?
- Do the users who run the seed flow in Dev and Test have the premium licence that Managed Environments require?

## Steps

1. Capture (user, live, read-only, each with the user's yes; build-order task 22). Claude first writes the seven reads as exact commands with their FetchXML. With the seed installed as under Environments and route, the user runs them in Dev and in Test, saving stdout and stderr apart. Also: read 2 for a solution that is not installed; read 7 in Test before and after editing the seed flow there; one malformed query (exit code and stream). If the seed's variable has no current value with a space, a comma and a line break, the user sets one in Dev first (the user's edit). Claude scrubs the output (environment ids and URLs, user names and ids, connection ids become fake values; the seed's own component ids stay) into `tests/fixtures/pac/drift/` and describes it in `tests/fixtures/NOTICE.md`. Record: the output format, how a value with a space and a line break comes out, whether the layer table can be read and how the unmanaged layer appears, whether it needs a per-component filter, the connection reference's component type, whether component ids match across Dev and Test, and `pac`'s exit code on a failed query. Check the three go-back Assumptions before Step 2.
2. `feat(core):` the snapshot model and the parsers in `src/PowerOps.Core/Drift/` (one parse function per read, from `pac`'s text to the snapshot parts; the snapshot file read and write with its schema version). Core tests on the Step 1 captures, each written to fail first: each read parses; not installed; layers read and not read; a value with a space and a line break gives the right hash; an empty `connectionid` is unbound; output with another shape, and a 5,000-row result, raise `InvalidDataException` without quoting the output; a snapshot file round-trips; a wrong `schemaVersion` is rejected (build-order task 23).
3. `feat(core):` the comparison and `DriftReport` (text, JSON, Markdown) in `src/PowerOps.Core/Drift/` and `src/PowerOps.Core/Output/DriftReport.cs`. Core tests for each kind in the table under Automation, a fires case and a quiet case each: equal snapshots give no items; information items do not count as drift; `solution-missing` stops that pair's comparison; checks done in every environment fire for the first one too; three environments compare with the first; the sort order; no value or hash in any output.
4. `feat(cli):` `src/PowerOps.Cli/DriftCommand.cs`, registered in `Program.cs` (build-order task 24). CLI tests with a fake `pac` through `Program.Run(args, output, error, pac)`: equal environments (0); each drift kind from the done-when list (1); usage errors before any `pac` call (one `--env`, no `--solution`, a bad name, `--save` on a file, a snapshot for another solution: 2); `pac` not found, a failed read and a timeout (3, no report, no files saved); a snapshot with a wrong `schemaVersion` (3); every call carries `--environment` with the `--env` value; the value appears nowhere in stdout, the report or powerops' own stderr lines; `--save` writes one file per live environment and none for snapshot inputs; two live environments with the same name and `--save` (2); a mixed run, one live and one snapshot file; a saved pair compared offline gives the same report as the live run; `--out`.
5. `test:` `docs/schemas/drift.schema.json` and `docs/schemas/snapshot.schema.json`, their rows in `docs/schemas/README.md`, two `Map` entries in `OutputSchemaTests`, and goldens `drift-seed.txt`, `drift-seed.json`, `drift-seed.md` (Dev and Test from the captures, with drift), `drift-none.json` and `snapshot-seed.json`.
6. `docs:` README usage block and a paragraph: what is compared and with which environment, the information items, what counts as drift, that values are never printed and snapshots hold hashes (and where not to keep them), that connection ids are not compared, `layers not read` (Q4), the run time and the 5,000-row limit, the tested `pac` version, and that agents run it only against environments the user named. Update the M8 row in `docs/BUILD-PLAN.md` when this merges: built, no `--show-values` (Q2), flows from the `workflow` table, `pac solution list` not read, and the layer outcome of Step 1.
7. Live check (user, each run with the user's yes): the three live done-when items under Goal, in order. The results go in the PR description, written by hand, with no tenant data and no `pac` output text.
