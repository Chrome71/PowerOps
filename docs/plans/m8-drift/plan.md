---
status: approved
review: done
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

- Offline: the tests under Steps pass on recorded `pac` output with a fake `pac` (no tenant). Equal snapshots exit 0. Each of these exits 1 and is named in the output: a version change, an extra component, an unmanaged layer (on a recorded capture if Step 1 shows layers can be read, else on a hand-written snapshot), a changed environment variable value, an unbound connection reference and a flow that is off. A failed or timed-out `pac` read exits 3. The JSON goldens validate against `docs/schemas/drift.schema.json` and `docs/schemas/snapshot.schema.json`. `dotnet build`, `dotnet test` and `dotnet format --verify-no-changes` are clean.
- Live (BUILD-PLAN M8 done-when), in the user's Dev and Test (BUILD-PLAN decision 9), each run read-only and with the user's yes:
  1. With the seed installed in both (unmanaged in Dev, managed in Test, the same environment variable value, its connection reference bound, its flow on), `drift --solution PowerOpsSeed --env <Dev> --env <Test> --save <dir>` reports no drift and exits 0. The managed difference is shown as information (Q3).
  2. `drift --solution PowerOpsSeed --env <dir>/<Dev file> --env <dir>/<Test file>`, offline, gives the same items, the same text output and the same exit code (0). Only the JSON `source` fields differ.
  3. After the user edits the seed flow in Test (in the maker portal; powerops writes nothing), a live run reports the unmanaged layer on that flow and exits 1. The edit must leave the flow on, so no `flow-state` item is expected. If Step 1 shows that layers cannot be read, that the flow cannot be edited in Test, or that the edit leaves no unmanaged layer, this item is recorded as not supported in the PR instead (Q4).

## Users

- Agents in the user's Claude Code or Codex that run `powerops drift ... --format json` to answer "does Test still match Dev?" before a deployment, and `--save` the snapshots for M9's plan.
- Makers and CI jobs that want a Markdown drift report in a PR or a job summary.

## Data

**Inputs.** `--solution <unique name>` and two or more `--env` values. Each `--env` is either a path to a snapshot file (when a file exists at that path) or an environment, passed unchanged to `pac` as `--environment` (an environment id or URL, as `pac` takes them). A value is a snapshot path when it ends in `.json` (ignoring case) or names an existing file; one that ends in `.json` and names no file is a usage error, so a mistyped snapshot path is never sent to `pac`. Anything else, an environment URL with its `/` included, is an environment. powerops never prints the value of an `--env`, because it may be a URL. Output names each environment by its position (`1`, `2`, ...) and its friendly name, because friendly names are not unique.

**Reads per live environment** (all read-only, each with `--environment <value>`, each through the existing `PacRunner` with the 30-second read timeout):

1. `pac org who`: the friendly name (the `Friendly Name:` line, read as `settings apply` reads it). Nothing else of its output is kept.
2. `pac env fetch --xml <FetchXML>` on `solution`, filtered on `uniquename`: `solutionid`, `version`, `ismanaged`. No row means the solution is not installed; the other reads are skipped.
3. On `solutioncomponent`, filtered on that `solutionid`: `componenttype`, `objectid`.
4. On `workflow`, the solution's workflows with `category` 5 (cloud flows): `workflowid`, `name`, `statecode` (0 off, 1 on, 2 suspended, mapped to the same `on`, `off`, `suspended` names as `FlowState`).
5. On `environmentvariabledefinition` with an outer link to `environmentvariablevalue`, for the solution's definitions: `schemaname` and the current `value`.
6. On `connectionreference`, for the solution's references: `connectionreferencelogicalname`, `connectorid`, and whether `connectionid` is set.

Reads 4 to 6 select "the solution's" rows by a link to `solutioncomponent` (`objectid` equal to the row's id, `solutionid` equal to the solution's id), the relation read 3 already uses, not by a `solutionid` column on the row.
7. On `msdyn_componentlayer`, only where the solution is managed (Q3): which of the solution's components have an unmanaged layer. If Step 1 shows the table cannot be read through `pac env fetch`, this read is dropped and the snapshot says `layersRead: false` (Q4). If the table needs a filter per component, the read is batched (one query per component type, with an `in` condition on the ids) when the table accepts that. If it accepts only one component per query, at most 50 component reads are made; a managed solution with more components gets `layersRead: false`, as under Q4, and the README says so. The table's component name for each `componenttype` number (if a filter needs it) is a small map in Core, taken from Step 1's capture, with a test.

The FetchXML for each read is built in Core (`src/PowerOps.Core/Drift/DriftQueries.cs`). Step 1 runs the same text: reads 3 to 7 are templates filled with each environment's solution id from read 2. The solution name goes into FetchXML only after it matches `^[A-Za-z0-9_]+$` (unique names allow only letters, digits and underscores); anything else is a usage error. The solution id goes in as a parsed GUID.

Changes from the BUILD-PLAN row's read list: flow states come from the `workflow` table (read 4), not from `pac power-automate list-cloud-flows`. One `env fetch` gives every cloud flow's name and state, with the same parser and filter as the other reads and no Preview command. `pac solution list` is not read: reads 3 to 7 need the solution id, so read 2 replaces it. `environmentvariabledefinition` and `pac org who` are added.

**Snapshot** (a Core record, and the snapshot file, Q1): `schemaVersion` (the snapshot file's own constant, starting at 1, which moves only when the snapshot format changes, not with `JsonOutput.SchemaVersion`; `docs/schemas/README.md` says so), the environment's friendly name, the solution's unique name, `installed`, `version`, `managed`, `layersRead`, and four lists. `layersRead` is false only when the layer read was dropped for a managed solution (Q4 or the 50-read cap). It is true when layers were read, and also when the solution is unmanaged or not installed, because no read was needed. The lists are sorted by their match key, ordinal:

- components: `type` (the `componenttype` number), `id` (`objectid`), `unmanagedLayer` (true, false, or null when layers were not read or the solution is unmanaged); matched by type and id;
- environment variables: `schemaName`, `valueHash` (the SHA-256 of the current value's UTF-8 bytes, in lowercase hex, or null when there is no current value) (Q2); matched by schema name, ignoring case;
- connection references: `logicalName`, `connectorId`, `bound`; matched by logical name, ignoring case;
- cloud flows: `id`, `name`, `state` (`on`, `off`, `suspended`); matched by id (the same across imports, Learn), shown by name.

A snapshot never holds an environment id or URL, a connection id, a value, a user name or `pac`'s own text. The ids it holds are the solution's own component ids, which are in every export of the solution anyway.

**Snapshot files** (Q1). `--save <dir>` creates the folder if needed and writes one file per live `--env`, `<dir>/<solution>.<environment name>.json`. Every character outside letters, digits, `-`, `_`, `.` and space becomes `-`, so the name is the same on every operating system. An existing file is overwritten. Snapshot-file inputs are not written again, and `--save` with no live `--env` is a usage error. Files are written only after every read succeeded. A snapshot file passed as `--env` must parse, have the current `schemaVersion` and name the same solution (compared ignoring case, as Dataverse compares unique names). Otherwise it is unreadable input (exit 3), or a usage error (exit 2) for another solution.

## Automation

`powerops drift --solution <name> --env <env or file> --env <env or file> [--env ...] [--save <dir>] [--format text|json|markdown] [--out <file>]`

1. **Usage and snapshot files** (before any `pac` call): a missing `--solution` or one that fails the name pattern, fewer than two `--env`, an `--env` that ends in `.json` but names no file, `--save` naming an existing file or used with no live `--env`: exit 2. Then every snapshot-file `--env` is loaded and checked: unreadable or a wrong `schemaVersion` is exit 3, another solution is exit 2. The first problem found, in `--env` order, decides the exit code.
2. **Read**: the live `--env` values in order, each with reads 1 to 7. The first failed, timed-out or unreadable read stops the run with exit 3. The message names the `--env` by its position (`--env 2`) and the `pac` command, never the value, and the first read's failure adds the sign-in hint `plan facts` prints. For a failed read, `pac`'s stdout and then its stderr go unchanged to powerops' stderr, as in `settings apply`. A timeout forwards nothing. `pac`'s own text may repeat the `--env` value; it goes only to stderr, never to the report, and the README says so. Nothing is saved and no report is written. A failed layer read (read 7, for example for lack of access to the table) is exit 3 like any other. A read that returns 5,000 rows (one FetchXML page) also exits 3, with "more than one page of results is not supported", because `pac`'s paging is not documented. A solution that is installed in none of the environments exits 2 ("solution `<name>` is not installed in any environment"): it is almost always a mistyped name, and "no drift" would read as a pass. With `--save`, two live environments whose file names collide (compared ignoring case) exit 2 after the reads and before anything is written.
3. **Compare** each environment with the first (BUILD-PLAN row). The result is a list of items, each with the environment, a kind, a subject and whether it is drift:

| Kind | Drift | When |
|---|---|---|
| `solution-missing` | yes | the solution is installed in the first environment and not in this one, or the other way round; nothing else is compared for this pair |
| `version` | yes | the versions differ (the item shows both) |
| `managed` | no | the managed flags differ (Q3); shown as information |
| `component-missing` / `component-extra` | yes | a component (matched by type and id) is in the first environment and not this one, or in this one only. Type 29 rows whose id is in the cloud-flow list, environment variable definitions and values (types 380 and 381) and connection references have their own kinds below, so they are left out here and one difference is reported once. Other workflows (business rules, classic workflows, desktop flows) stay in |
| `unmanaged-layer` | yes | a component of a managed solution has an unmanaged layer; checked in every environment, the first included |
| `layers-not-read` | no | the layer read was dropped (Q4) and the solution is managed there |
| `variable-missing` | yes | the definition is in the first environment and not this one, or the other way round |
| `variable-value` | yes | the current values differ, including a current value on one side only; the output says `differs` and never shows a value or a hash (Q2) |
| `reference-missing` | yes | as for variables |
| `reference-connector` | yes | the connector differs |
| `reference-unbound` | yes | the reference has no connection; checked in every environment, the first included |
| `flow-missing` | yes | as for variables |
| `flow-state` | yes | the states differ (the item shows both) |

The checks of a single environment (`unmanaged-layer`, `reference-unbound`) run for every environment where the solution is installed, whatever the pair comparison found. Connection ids are never compared: connections belong to one environment, so a reference bound in both is bound to different ids by design. Default values of environment variables are not compared: they ship in the solution, so a version match covers them.

4. **Report**: items sorted by environment position, then by the kind order of the table, then by subject (ordinal). The subject is the name for flows, variables and references, and `<type> <id>` for other components (powerops reads no display names for them). With `--save`, the files are written now. Exit 0 when no item is drift, 1 when any is.

Exit codes: `0` no drift, `1` drift, `2` usage error (including a solution installed nowhere), `3` a `pac` read failed, timed out or printed output powerops cannot read, `pac` not found, or an unreadable snapshot file.

`--format json` follows the M4 contract: `schemaVersion` first, `docs/schemas/drift.schema.json`, its row in `docs/schemas/README.md`, a `Map` entry in `OutputSchemaTests` and goldens. The report holds `solution`, `drift` (true or false), `environments` (position, name, `source` `live` or `snapshot`, `installed`, `version`, `managed`, `layersRead`) and `items` (environment position and name, kind, subject, `drift`, and `expected`/`actual` for `version`, `managed`, `reference-connector` and `flow-state`, else null). The snapshot file has its own schema, `docs/schemas/snapshot.schema.json` (prefix `snapshot`, so it does not clash with the `drift` prefix in `Map`).

The model, the parsers of `pac`'s output, the comparison and the reports live in Core (`src/PowerOps.Core/Drift/` and `src/PowerOps.Core/Output/DriftReport.cs`). `src/PowerOps.Cli/DriftCommand.cs` only runs `pac` and writes files. It takes the existing `Func<IReadOnlyList<string>, TimeSpan, PacResult>` runner that `Program.Run` already passes to `settings`, so tests use a fake. The private helpers of `SettingsCommand` that drift needs become `internal` and are reused, not copied: `Field` (the `Friendly Name:` reader), `Failed` (the failure text), `NotFound` and `Guard` (exceptions to exit 3); `PlanFacts.SignInFix` is already `internal`. `SettingsCommandTests` stay unchanged.

Out of scope (not asked for): a mode that prints positions only, without friendly names (names are printed and saved; the README says the reports and files are for private places), an option to accept environment variables whose values differ by design (such a variable always exits 1; JSON users can filter `items` by `kind`, and the README says so), `--show-values` (Q2), display names of components other than flows, variables and references, comparing default values, comparing each environment with every other one, reading more than one solution per run, paging past 5,000 rows, the Dataverse SDK or Web API (Q4), and any write to an environment.

## Screens

None. Command-line only. Text output: one line per environment (`<position> <name>: <version>, managed|unmanaged[, layers not read]` or `<position> <name>: not installed`; `layers not read` appears only for a managed solution with `layersRead: false`), one line per item (`<position> <name>: <kind> <subject>[: <expected> -> <actual>]`, information items marked `(info)`), and a summary line (`no drift` or `<n> drift items`). Markdown: an environments table and an items table, for a PR comment.

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
- A SHA-256 hides a value from a reader, not from someone who guesses short values (a flag, a known URL). Snapshot files are for the user's own folders and private CI artifacts, not public places. The README and the snapshot schema's description say so and do not call the files "safe" (this narrows the wording of Q2's answer).
- Run time: about six `pac` reads per environment, plus the layer reads, each up to 30 seconds. Layer reads are one per component type when batched, or up to 50 when the table takes one component per query (Data, read 7). Step 1 records which, and the time per read, and the README gives the cost.
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

The first four are go-back Assumptions: if Step 1 refutes one, the plan goes back to planning before Step 2, with the fallback named.

- `pac env fetch` prints output that can be parsed reliably: a value with spaces, commas or a line break is neither cut nor wrapped, and a state and a boolean print as values the parser can map (numbers or labels). Learn does not document the format; Step 1 checks it. Fallback: `--xmlFile` changes only the input, and no other output option is documented, so M8 stops and the BUILD-PLAN row is revised (a new decision).
- Component types and ids (`componenttype`, `objectid`) are the same in Dev and Test after an import, for every root component of the seed (the table, the flow and the canvas app) and their subcomponents. Learn states it only for flows; Step 1 compares them. If only some types differ, the fallback is to leave those types out of `component-missing` and `component-extra` and say so in the README.
- A clean managed Test, right after the import that binds the connection reference and with the flow on, shows no unmanaged layer. If Step 1 shows one, the fallback is to name the component types exempt from `unmanaged-layer` (for example the connection reference), or to record live item 1 as not supported.
- `pac org who --environment <id or URL>` and `pac env fetch --environment <id or URL>` read the named environment, not the profile's current one (Learn and the 2.13.1 usage text; never run). Fallback: none within this plan; `pac org select` before each environment writes pac's settings and needs a new decision, so M8 stops and the BUILD-PLAN row is revised.
- `pac env fetch` can read `msdyn_componentlayer`, and its rows show which layer is the unmanaged one. Not documented; Step 1 checks it. If not, Q4 applies (not a go-back).
- The connection reference's component type number is taken from Step 1's capture: Learn does not list it.
- `pac` exits non-zero when a FetchXML query fails. Step 1 checks it with a malformed query.
- A connection reference with an empty `connectionid` is unbound.
- Environment facts were not available: `powerops plan facts` was not run during planning.

## Open questions

## Questions for your admin

- Which security role lets the reading account query the `solution`, `solutioncomponent`, `workflow`, environment variable, `connectionreference` and `msdyn_componentlayer` tables in Test (and later Prod) without write rights?
- Do the users who run the seed flow in Dev and Test have the premium licence that Managed Environments require?

## Steps

1. Capture: the first thing to do, before any code (user, live, read-only, each with the user's yes; build-order task 22). Claude first writes the seven reads as commands. Reads 2 and 1 are fixed. Reads 3 to 7 are templates from `DriftQueries`' FetchXML, filled by hand with each environment's solution id from read 2. With the seed installed as under Environments and route, the user runs them in Dev and in Test, saving stdout and stderr apart. Also: read 2 for a solution that is not installed; read 7 in Test right after the import and binding (before any edit) and again after editing the seed flow there; one malformed query; and, if read 7 needs one component per query, one batched query with an `in` condition. Before the capture the user sets the seed variable's current value in Dev to a made-up test value with a space, a comma and a line break (the user's edit). Claude scrubs the output into `tests/fixtures/pac/drift/`: environment ids, URLs and friendly names, user names and ids, emails, connection ids, solution ids and any variable value other than the made-up one become fake values, and the error text of failed queries is replaced by a placeholder. Component ids stay only for the components of the seed's exported solution (the allow list), others become fake. Then Claude runs a grep over the folder for URLs, emails and GUIDs not on the allow list, which must find nothing, and the user reads the scrubbed files before the `test:` commit. The files are described in `tests/fixtures/NOTICE.md`. Record:
   - the output format, and how a value with a space, a comma and a line break comes out (cut, wrapped or whole);
   - whether `statecode`, `ismanaged` and `componenttype` print as numbers or labels;
   - that `--environment` read the named environment on `org who` and `env fetch` (the friendly name and the solution rows match that environment);
   - the component types and ids of the seed in Dev and in Test, side by side, and the connection reference's type number;
   - whether the layer table can be read, which filter it needs (per component or batched), the component names a filter needs, how the unmanaged layer appears, whether a clean Test shows any, whether the seed flow can be edited in Test and the edit leaves an unmanaged layer with the flow still on, and the number of layer reads for the seed and the time per read;
   - `pac`'s exit code and output stream on a failed query.

   Then check the four go-back Assumptions before Step 2.
2. `feat(core):` in `src/PowerOps.Core/Drift/`, written test-first, with tests on the Step 1 captures (build-order task 23):
   - `DriftQueries`: the FetchXML text of each read, from a solution name (checked against the pattern) or a parsed solution id. Tests: the exact text of each query; a name outside the pattern is refused; the id goes in as a GUID.
   - The snapshot model, the parsers (one per read, from `pac`'s text to the snapshot parts) and the snapshot file read and write with its schema version. Only the layer mode Step 1 found is built (batched, per component with the 50-read cap, or not read), with its tests; the other modes and their README lines are not written. Tests: each read parses; not installed; layers as Step 1 found them (and, only for the per-component mode, the cap and the component-name map); a value with a space and a line break gives the right hash; an empty `connectionid` is unbound; output with another shape, and a 5,000-row result, raise `InvalidDataException` without quoting the output; a snapshot file round-trips with its lists sorted; a wrong `schemaVersion` is rejected.
3. `feat(core):` the comparison and `DriftReport` (text, JSON, Markdown) in `src/PowerOps.Core/Drift/` and `src/PowerOps.Core/Output/DriftReport.cs`. Core tests, one fires case and one quiet case for each kind in the table under Automation. Also:
   - equal snapshots give no items, and information items do not count as drift;
   - `solution-missing` stops that pair's comparison;
   - a flow, variable or reference missing gives one item, not a component item too; a type 29 component that is not a cloud flow, missing in the second environment, gives `component-missing`;
   - checks done in every environment fire for the first one too;
   - three environments compare with the first, and two with the same name stay apart by position;
   - the sort order;
   - no value or hash appears in the text, JSON or Markdown report (the snapshot file holds hashes by design).
4. `feat(cli):` `src/PowerOps.Cli/DriftCommand.cs`, registered in `Program.cs`, with the `SettingsCommand` helpers made `internal` (build-order task 24). The same commit adds the M4 contract pieces: `docs/schemas/drift.schema.json` and `docs/schemas/snapshot.schema.json`, their rows in `docs/schemas/README.md`, and two `Map` entries in `OutputSchemaTests`. It also adds the goldens `drift-seed.txt`, `drift-seed.json`, `drift-seed.md` (Dev and Test from the captures, with drift), `drift-none.json` and `snapshot-seed.json`. Each golden is checked with `Golden.Verify` in `DriftCommandTests` and updated with the AGENTS.md "Update goldens" recipe. CLI tests with a fake `pac`, through `Program.Run(args, output, error, pac)`:
   - exit 0 for equal environments, and exit 1 for each drift kind in the done-when list;
   - exit 2 before any `pac` call for: one `--env`, no `--solution`, a bad name, a missing path that ends in `.json`, `--save` on a file, `--save` with only snapshot inputs, and a snapshot for another solution;
   - exit 3 with no `pac` call for a snapshot with a wrong `schemaVersion`;
   - exit 3, with no report and no files saved, when `pac` is not found, a read fails (`pac`'s stdout and stderr on powerops' stderr, the sign-in hint for the first read) or a read times out (nothing forwarded);
   - exit 2 for a solution installed in no environment;
   - a URL-shaped `--env` (with `/`) reaches the fake `pac` as `--environment` and exits 0;
   - every call carries `--environment` with the `--env` value, and the value appears nowhere in stdout, the report or powerops' own stderr lines;
   - `--save` writes one file per live environment and none for snapshot inputs, and two live environments whose file names collide give exit 2 with no files written;
   - a mixed run, one live and one snapshot file;
   - a saved pair compared offline gives the same items, text and exit code as the live run;
   - `--out`.
5. `docs:` README usage block and a paragraph covering:
   - what is compared, with which environment, and how environments are named (position and name);
   - the information items, and what counts as drift;
   - that a variable whose value differs by design always exits 1, and that JSON users can filter by `kind`;
   - that values are never printed and snapshots hold hashes, and where not to keep them;
   - that connection ids are not compared;
   - `layers not read` (Q4 and the 50-read cap);
   - the run time and the 5,000-row limit;
   - that `pac`'s forwarded error text may repeat an `--env` value;
   - the tested `pac` version, and the privilege the reads need (from the admin answer);
   - that friendly names are printed and saved, so reports and snapshot files belong in private places;
   - that agents run it only against environments the user named.

   Update the M8 row in `docs/BUILD-PLAN.md` when this merges: built, plus the changes from the row. These are no `--show-values` (Q2), flows from the `workflow` table, `environmentvariabledefinition` and `pac org who` read, `pac solution list` and `list-cloud-flows` not read, and the layer fallback "not read" (Q4) in place of "limited to installed unmanaged". Add the layer outcome of Step 1.
6. Live check (user, each run with the user's yes): the three live done-when items under Goal, in order. The results go in the PR description, written by hand, with no tenant data and no `pac` output text.
