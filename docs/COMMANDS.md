# Command reference

Every `powerops` command with its options, output and exit codes. Install PowerOps first: [Install](../README.md#install).

## Usage

```sh
powerops analyze                      # find solutions under . (unpacked XML folders and exported .zip files)
powerops analyze path/to/solution.zip path/to/unpacked --format json --out report.json
powerops analyze path/to/solution.zip --redact   # the same findings, with each URL in them and on stderr replaced by [redacted URL]
powerops rules                        # list every rule with its severity, title and fix (add --format json for machine output; --json is the older spelling, and wins over --format text)
powerops diff old.zip new.zip         # compare two versions component by component (zip or unpacked folder; --format text|json|markdown, --out <file>)
powerops manifest path/to/unpacked    # list a solution's files with a SHA-256 for each and one digest (a .zip prints its SHA-256); with two unpacked folders, compare them file by file; --format json prints the same as JSON
powerops test path/to/solution --tests tests/flows   # run offline flow tests (*.flowtest.json) against the cloud flows, with mocked connector actions (--format text|json|markdown, --out <file>)
powerops settings generate path/to/solution --out deploymentsettings.json   # pac deployment settings with empty values to fill in
powerops settings validate path/to/solution --settings deploymentsettings.json   # missing or extra entries, empty connection ids and values
powerops settings apply solution.zip --settings deploymentsettings.json   # dry run: the target, the pac command, each cloud flow new or existing
powerops settings apply solution.zip --settings deploymentsettings.json --apply --target "Contoso Dev"   # import through pac, then check the flows are on
powerops drift --solution MySolution --env <dev> --env <test>   # compare one solution across environments or saved snapshot files (--save <dir>, --format text|json|markdown, --out <file>)
powerops drift --solution MySolution --env <dev> --env <test> --save snapshots/   # the same, and write snapshots/MySolution.<environment name>.json for each environment
powerops drift --solution MySolution --env <dev> --save snapshots/   # one environment: write its snapshot and report its own checks (the first snapshot)
powerops drift --solution MySolution --env "snapshots/MySolution.Contoso Dev.json" --env "snapshots/MySolution.Contoso Test.json"   # compare saved snapshots, offline
powerops deploy plan exports/ --target <test>   # what importing the solution zips in exports/ would do to the target: order, install, upgrade or skip, deletions, layers, settings, flow states (reads the target only)
powerops deploy plan exports/ --target <test> --save exports/powerops-plan.json --format markdown --out plan.md   # the same, with the Markdown for a PR and the plan file from one read
powerops deploy apply exports/powerops-plan.json --target <test>   # dry run: plan again against the target, refuse if it differs from the reviewed plan, say what would be imported
powerops deploy apply exports/powerops-plan.json --target <test> --apply   # import the reviewed plan through pac, then check each cloud flow
powerops deploy apply exports/powerops-plan.json --target <test> --require-reviews   # the same dry run, refused unless both review reports of one plan folder cover each zip the plan imports
powerops deploy gate exports/powerops-plan.json   # a PreToolUse hook: reads the hook's JSON on stdin; exit 0 allows the call, exit 2 blocks it and says why on stderr
powerops baseline                     # accept today's findings: writes powerops-baseline.json
powerops analyze --baseline powerops-baseline.json   # report only findings that are not in the baseline
powerops mcp                          # run an MCP server over stdio for agents: its tools run the commands above and return their JSON
```

### Writing files

Every file PowerOps writes (`--out`, `--save`, the baseline file, the plan that `plan sign-off` edits, the skill files of `skills install`) goes through one rule.
A target that is a symbolic link is refused with exit `2` and an `error:` line, before the command does its work, and the file behind the link is not touched; for `skills install` that includes a link at a skill file or at a folder on the way down from the repo folder (the repo folder itself may be a link), so `.agents/skills` as a link to `.claude/skills` is refused too.

A target that is a file the command was given as an input is refused the same way: an `analyze` or `baseline` zip, `--baseline`, the two solutions of `diff`, the path of `test`, the solution of `settings generate`, an `--env` snapshot file of `drift`, the plan file of `deploy apply`.

The files a scan found count too: the zips and `solution.yml` files that `analyze`, `baseline`, `diff` and `test` found under a folder, the `*.flowtest.json` files `test` found, and the zips and settings files in the folder of `deploy plan` or beside the plan file of `deploy apply`; each command lists them before the refusal, which stays before its work and before any read of a tenant.
The files a reader opens inside an unpacked solution folder do not count, so a report can still be written into the folder it scanned, as the Action does with `powerops-plan.json`.
`plan sign-off` through a linked `plan.md` exits `3`, as it does for a linked plan it reads.

A target is taken as .NET takes every path on every OS: `..` is collapsed first, so `--out dl/../x.json` with `dl` a link to `sub/deep` names the `x.json` beside `dl` (a shell would name `sub/x.json`), which is also the file the commands' readers open for that spelling; then it is resolved once through every link on the way, one name at a time as `realpath` does (a `..` inside a link's target is taken in the link's target), and that one path is checked and written.
Each input is taken the same way and the results are compared, with case ignored on Windows and macOS.
A link loop, or more than 40 links on the way, at the target or at a named input is refused with exit `2`.
So `--out /proc/self/cwd/in.zip` and `--out self/in.zip` with `self` a link to `.` are refused when `in.zip` is an input.
It does not see 8.3 names, `\\?\` paths or Unicode normalization, and a link swapped in after the check needs write access to the folder.

An empty name or an existing folder as the target is refused with exit `2` too, and `deploy apply --out` checks before any import that a file can be created in its folder (a device such as `/dev/null` is written in place, so it skips that check), so the report of imports that ran is not lost to a read-only folder.

A regular file is written to a temp file in its folder (`.powerops-<hex>.tmp`) and moved over the target, so a crash leaves the old file whole, and an existing file keeps its Unix mode (its owner, ACL, extended attributes and hard-link count are not kept).
A regular file whose length cannot be set (a filesystem that refuses it) is not written in place: the write exits `3`.
A killed process can leave the temp file behind.
`/dev/null`, `/dev/stdout`, `/dev/stderr`, `NUL` on Windows and a pipe (FIFO) named by its own path are written in place, as before; a link to a device or a pipe is refused like any link, so name the device itself.

Known limits, each now exit `3` where an in-place write worked:

- a single file bind-mounted into a container as `--out`,
- a folder that allows writing the file but not creating a file,
- and a sticky world-writable folder such as `/tmp` where a rename over another user's file is refused.

The MCP `deploy_plan` `save` is the exception: it refuses a file or a link that exists.

### Values in outputs

Some outputs quote values from the solution.
In `analyze`, in every format and in the `baseline` file, PO006 and PO014 put the URL they found into the finding's message and component.
The other rules quote names (of flows, apps, tables, connection references and other components), counts and file paths, and no finding quotes an environment variable's default or current value.

`diff` prints an environment variable's `defaultValue` before and after (cut to 100 characters), and never its current value, an agent's instructions or a flow's definition.
`test` reports quote values from the solution and the test files.
`drift` prints no variable value (its snapshot files hold hashes), and `deploy plan` and `deploy apply` print no setting value or connection id.

`--redact` on `analyze` and `diff` replaces the URLs and the `defaultValue` text.
No other command has it, a baseline file is never redacted, because it must match the findings, and the MCP `analyze` and `diff` tools return the same text as the commands without `--redact`.
In a public repository, treat what the Action writes as public ([Public outputs](GITHUB-ACTION.md#public-outputs)).

## analyze and baseline

`analyze` is offline and read-only.

### Options

Options:

- `--format text|json|sarif|markdown` (default `text`),
- `--out <file>` (write the report to a file instead of stdout),
- `--fail-on error|warning|none` (default `error`),
- `--baseline <file>` (drop findings listed in a baseline file before reporting and before the exit code; the summary counts them as `baselined`),
- `--redact` (replace each URL, that is text that starts with a scheme and `://` such as `https://`, in every finding's message and component, every not-checked note and every line on stderr (but a usage error about the options, which quotes only the command line) with `[redacted URL]`, in all four formats and in the `--out` file; punctuation right after a URL goes with it, and two findings that differed only in their URL then read the same and share one SARIF fingerprint, so code scanning can show them as one alert; the help links that come from the rule list, `helpUri` and the Markdown's rule links, stay, and names, paths and versions are not changed; the baseline is matched before the findings are redacted, so `--baseline` drops the same findings; a SARIF finding's `poFinding/v1` fingerprint is taken over the redacted text, so switching `--redact` on or off for a code scanning upload closes the alerts of the findings it changes and opens new ones, once).

### What it does

It reads unpacked XML folders (`Other/Solution.xml`), exported zips and the YAML layout that Dataverse Git integration commits: point it at the folder that holds `solutions/`, and each `solutions/<name>/solution.yml` is reported as its own solution, with its components picked by its `solutioncomponents.yml`.

Canvas app formulas are read from the `Src/*.pa.yaml` files: in the YAML layout from the folder, and in the XML layouts from inside each `.msapp`, in memory (PO014, PO018, PO036).
An app whose `.msapp` has no `Src` (an older app, until it is resaved in Studio) prints a `note:` line, and so does an `.msapp` that isn't a zip, such as an empty file or a Git LFS pointer, which is not opened; an app whose `.msapp` the solution doesn't hold is not read and gets no note.

- Power Pages components are read from the `powerpagecomponents/` folder of the enhanced data model in a solution, unpacked or in a zip, and not from the `.powerpages-site/` layout (PO019 to PO023).
- Model-driven app modules and site maps are read from the `AppModules/` and `AppModuleSiteMaps/` folders of an unpacked solution and from the same elements of a zip's `customizations.xml`, not from the YAML layout; generative pages (`uxagentprojects/`) are not read yet, so a site map entry for one prints a `note:` line (PO024, PO025).
- The tables and connectors a code app depends on are read from its canvas app metadata (`CdsDependencies`) in an unpacked folder or a zip, not from the YAML layout (PO026).
- Security roles and the role maps of app modules are read from the `Roles/` folder of an unpacked solution and from the same elements of a zip's `customizations.xml`, not from the YAML layout (PO032 to PO034).
- One-to-many relationships are read from `Other/Relationships/` of an unpacked folder, a zip's `customizations.xml` and the YAML layout's `entityrelationships/` (PO035).

Copilot Studio agents are read from the `bots/` and `botcomponents/` folders of an unpacked solution or a zip, not from the YAML layout.
An agent's instructions are read from its single Custom GPT component, or, when it has none, from the `agentSettings.instructions.segments` of its `configuration.json` (the layout of Copilot Studio's new authoring UI); its tools are read from its `TaskDialog` components and, in that layout, its `McpTool` components, and its Work IQ workloads from its `IQCapability` components.
In an agent's YAML every key is read as a name, so keys that start with `@` or `#` (such as the `"@odata.id"` of a Dataverse tool's output schema) are read; only a key that is not plain text, or is empty, leaves a tool file unread.
The agent rules read only agents on the standard harness, only their `TaskDialog` tools and Custom GPT instructions, and any other agent prints a `note:` line (PO027 to PO031).
A Copilot Studio workspace in `.mcs.yml` form is read after `pac copilot pack` makes it a solution.

`baseline [path...] [--out <file>]` runs the same analysis and writes the findings it found to a JSON file (default `powerops-baseline.json`).
A finding matches the baseline on solution unique name, rule id, component and message, not on file, so a baseline made from an exported zip also works for the unpacked folder.
An unreadable or invalid baseline file is a usage error (exit `2`).

### Output

In SARIF a file under the current folder has a relative URI, and one outside it (an absolute path, or one through `..`) has an absolute `file:` URI, which code scanning makes relative to the repository it checked out.

A rule whose input is outside the export, such as a view or a parent permission that is not in the solution, prints a `note:` line on stderr instead of a finding, and the exit code doesn't change; at most 20 such lines are printed, as with the notes about links inside a solution, then one line gives the count of the rest and the rules they belong to, such as `note: and 7 more checks not done (PO022, PO023)`.
The `note:` lines of rules that did not check something, not the notes about links, are also all in the JSON as `notChecked` on each solution, in SARIF as `toolExecutionNotifications` (level `note`) of the run's one invocation, and in the Markdown as a `Not checked:` list under each solution.

Every finding carries a one-line `fix` and a `helpUri` to its docs page in JSON and Markdown output, and SARIF rules carry the fix as `help`.
Every JSON output carries a `schemaVersion`; the schemas are in [docs/schemas/](schemas/).

### Exit codes

Exit codes: `0` no findings at or above `--fail-on`, `1` findings at or above it, `2` usage error (bad option, missing path, no solutions found), `3` unreadable solution or internal error (set `POWEROPS_DEBUG=1` for a stack trace).

In a scanned folder, a `*.zip` that starts with the zip signature but can't be opened, or that can't be read at all, exits `3` and names the file; a readable `*.zip` without the signature (a Git LFS pointer, an empty placeholder) or with no `solution.xml` is skipped, and so is a file that vanishes during the scan.
A zip with two entries of the same name, and a flow file with a duplicate JSON key, are refused (exit `3`).

## rules

The rules, with a page for each: [docs/rules/](rules/README.md).

## diff

### Synopsis

`diff <old> <new>` is offline and read-only.

### Options

Options:

- `--format text|json|markdown` (default `text`),
- `--out <file>`
- and `--redact` (print a changed `defaultValue` as `defaultValue added`, `defaultValue removed` or `defaultValue changed`, as current values always are, and replace each URL in a change's key, name and detail, in each not-compared line and on stderr with `[redacted URL]`, as `analyze --redact` does).

### What it does

It loads both solutions and compares the solution unique name, version, managed flag and publisher, root components, missing dependencies, connection references, environment variables, flows, canvas apps, and Copilot Studio agents (their instructions and Work IQ workloads) and agent tools (their action, MCP flag and mode, and for an `McpTool` of the new authoring UI also its connection reference and operation id), read as `analyze` reads them (so keys that start with `@` or `#` in an agent's YAML are read as names, and only a key that is not plain text or is empty leaves a tool file unread and its tool uncompared).

For an agent it compares its instructions (the text of its single Custom GPT component, or of `configuration.json`'s segments when it has no such component), its tools, and its Work IQ workloads (`IQCapability` components, matched by schema name and compared by capability and access, inside the agent's change as `IQCapability <schema name>: <capability> <access> → <capability> <access>`; a workload change carries no risk label).
The logical name of an `McpTool`'s connection reference is printed when it changes.

File paths, ordering, flow JSON key order and whitespace, `operationMetadataId` in an action's or a trigger's `metadata` under `properties.definition` (nested actions included; it is compared anywhere else, and in a flow of any other layout), and XML whitespace in canvas app metadata are not differences, so a folder and its exported zip compare equal.
Classic workflows and business rules are compared by name, category and state only (their XAML is not read).
Environment variable current values are reported as changed, added or removed but never printed.
A canvas app whose `AppVersion` timestamp changed is reported as republished, since an edit changes the app package and the timestamp.

### Output

Some changes carry a risk label:

- `removed-component` (a managed upgrade deletes components that are no longer in the new version, [per Microsoft Learn](https://learn.microsoft.com/en-us/power-platform/alm/solution-concepts-alm)),
- `flow-state` (a cloud flow was turned on or off),
- `connection-reference` and `environment-variable` (added or removed),
- `agent-tool` (an agent tool was added or removed, or its mode, whose credentials it uses, changed),
- `agent-instructions` (an agent's instructions changed; the text is never printed)
- and `same-version` (on every change when the two solutions have the same unique name, ignoring case, and equal versions, compared as `deploy plan` compares them: `deploy plan` would `skip` a zip built from the new one, so raise the version; the label belongs to the pair, so it is on every change of such a diff and a consumer may read it from any one).

Markdown is cut at a whole row below 60,000 characters, with a note on how many changes were left out, so it fits in a pull request comment.
JSON follows [docs/schemas/diff.schema.json](schemas/diff.schema.json).
Its `schemaVersion` is 3 (`deploy plan` and `drift` are at 2, and every other output is at 1): version 2 added the `agent` and `agentTool` components and the two agent risk labels, version 3 added the `same-version` risk label, and neither removed or renamed anything, so a consumer that checks the version accepts 3, and one that validates the output takes the new schema ([Versioning](schemas/README.md#versioning)).

### Exit codes

Exit codes: `0` no differences, `1` differences found, `2` usage error (bad option, missing path, or a folder that holds several solutions: pass one of them), `3` unreadable solution or internal error (set `POWEROPS_DEBUG=1` for a stack trace).

### Limits

It does not compare an agent's authentication mode, recognizer, settings other than the instructions (such as the model and web search), components of a type other than topics and tools, a tool's or workload's name, description or state, or an `McpTool`'s `connectorId` (its connection reference is compared).
An empty `notCompared` therefore means the instructions, tools and Work IQ workloads were read, not that the agent is equal.

An agent whose instructions it can't read in one of the two versions (no single Custom GPT component whose instructions are text, and no `configuration.json` segments it reads: only `StaticSegment` items with text are read), an agent whose Custom GPT component sits beside `configuration.json` instructions that differ from it or can't be read (only the component's are compared), or whose type-9 components are of a kind it doesn't read (any kind but `TaskDialog`, `McpTool` and `IQCapability` with the fields it reads, topics (`AdaptiveDialog`) and child agents (`AgentDialog`), such as the other tool kinds of the GitHub Copilot harness), is listed as not compared: one `note:` line on stderr per item (at most 20, then a count), the JSON's `notCompared` and a `Not compared:` list in the Markdown above the table.
The exit code doesn't change, so `no changes` beside such a note means those parts were not compared, not that they are equal.

It does not compare security roles, tables, columns, forms, views or app modules and their role maps, or other components it does not read yet, so its `no changes` says nothing about them.

## manifest

### Synopsis

`manifest <path>` and `manifest <old> <new>` are offline and read-only, and write nothing, so there is no `--out`.

### Options

`--format json` prints the same facts as JSON (`--format text` is the default): `manifests` holds one entry for one path and two for a comparison, the old one first, each with `path`, `kind` (`folder` or `zip`), `digest` and `files` (empty for a zip), and `comparison` (`added`, `removed`, `changed`, `same`) is present only with two folders.
The paths are as given, and the schema is [manifest.schema.json](schemas/manifest.schema.json).
The exit codes and the stderr notes do not change.

### What it does

`diff` compares only what its section lists; `manifest` is the check for the rest of a solution (security roles, tables, forms, views, app modules), because it says whether any file of it differs.

### Output

With one path it prints the inventory of an unpacked folder, or the SHA-256 of an exported `.zip`.
Give the solution's own folder (the one that holds `Other/Solution.xml`), not a folder that also holds `docs/plans/`: a report written there would change the digest it records.

```
<path>: folder, <n> files, digest <hex>
<sha256>  <path inside the folder>
...
```

For a zip the output is the first line only, `<path>: zip, digest <hex>`; the digest is the SHA-256 of the zip's bytes, the same hash `deploy plan` records for it, and the zip's entries are not listed.
The file lines are in ordinal order of their paths, which use `/` on every OS.
The digest is the SHA-256 of the lines `<sha256>  <path>` (each ended by a line feed) in that order, in lowercase hex, so one number stands for the whole folder.

A file with no zero byte is hashed with each CRLF replaced by LF, so one commit gives the same hash on a Windows checkout (CRLF with `core.autocrlf`) and a Linux one; any other file, such as an `.msapp`, is hashed as its bytes.
`.git`, `node_modules`, `bin` and `obj` folders are not entered, and a symbolic link inside is not followed: it is not listed, and a `note: <path>: a symbolic link, not read` line is printed on stderr for it.

With two paths, both must be unpacked folders.
It prints the first line of each, then `added <path>`, `removed <path>` and `changed <path>` lines (in that order of kind, each list in ordinal order), then `<n> files the same`.
`changed` means the hash differs.

### Exit codes

Exit codes: `0` for one path, or for two folders with no difference; `1` when two folders differ; `2` for a usage error: a path that does not exist, is a symbolic link or is a file that isn't a `.zip`, or a zip in the two-path form; `3` for an unreadable path, a file over a cap or more than 100,000 files.

### Limits

The read has the caps of `analyze` (100,000 files, 50 MB for a file, 256 MB in all, 100 MB for a zip).

## test

`test` runs offline flow tests: [Flow tests](FLOW-TESTS.md).

## settings generate and settings validate

### Synopsis

`settings generate <solution> [--out <file>]` prints a `deploymentsettings.json` with the solution's environment variables and connection references, sorted by name, with empty `Value` and `ConnectionId` to fill in (stdout, or a file with `--out`).

`settings validate <solution> --settings <file> [--format text|json]` reports

- entries missing from the file,
- entries not in the solution,
- names listed more than once,
- connection references without a `ConnectionId` (it checks that a `ConnectionId` is present, not that the connection exists),
- environment variables with no value in the file and no default or current value in the solution,
- and any `"Value": ""`, which `pac solution import` rejects.

### What it does

To keep a variable's default or current value, leave its entry out of the file (observed with `pac` 2.13.1); `settings generate` writes every variable with an empty value, so fill each one in or remove it.
Fields the solution does not need (the extras `pac` writes) are ignored.

### Exit codes

Exit codes: `0` clean, `1` problems, `2` usage error, `3` unreadable solution or invalid settings file.

## settings apply

### Synopsis

`settings apply <zip> --settings <file> [--apply --target <name>] [--format text|json]` deploys an exported solution zip with a deployment settings file through `pac`.

### Options

`--apply --target <name>` imports into the pac profile's current environment, and only when `<name>` equals that environment's friendly name; every later `pac` call is pinned to that environment's id, which is never printed.
Friendly names are not unique, so check the profile with `pac auth list` before saying yes to a target.

### What it does

Without `--apply` it is a dry run that writes nothing: it runs `settings validate`, reads pac's current environment with `pac org who`, reads each cloud flow's state there with `pac power-automate list-cloud-flows`, and prints the target environment name, whether the zip is managed, the `pac solution import` arguments (the environment id shows as `<environment id>`) and each flow as `new` (with the state the import should give it) or `exists` (with its state, which the import keeps).

The import is `pac solution import --environment <id> --path <zip> --settings-file <file>` with pac's defaults: unmanaged customisations are not overwritten, changes are not published and there is no upgrade, so components removed from the solution stay in the target.

Afterwards each flow is reported `on`, `off`, `suspended` (for example, a data policy blocks it) or `missing`.
`pac` cannot turn a flow on, so powerops does not either: a new flow comes back in its export state (on only if it was on at export and its connection references get connections), an existing flow keeps its state, and a flow stays off when the importing user does not own or have access to every connection it uses (`ConnectionAuthorizationFailed`).
A solution with a flow that was off at export therefore always exits `1`; that is by design, because the command checks that the flows are on.
A user who cannot list flows sees every flow as `new` before and `missing` after.

The import turns the solution's flows off and on again while it runs, so a running flow is briefly interrupted.

### Output

When the import fails, the report holds pac's exit code and pac's own output goes to stderr.
In text mode with `--apply`, the first line is the target environment, printed before the import starts; with `--format json` that line goes to stderr and the report ([docs/schemas/settings-apply.schema.json](schemas/settings-apply.schema.json)) is written at the end.

### Exit codes

Exit codes:

- `0` dry run done or every flow on,
- `1` validate problems or a flow not on,
- `2` usage error (bad option, path not found, a folder instead of a zip, `--apply` without `--target` or the reverse, or a `--target` that does not match),
- `3` `pac` not found, not signed in, a failed or timed-out import, `pac` output it cannot read, or an unreadable solution or settings file.

### Limits

It can take up to an hour (powerops waits 60 minutes): run it in the background or with a long command timeout; stopping powerops does not stop an import already running in the environment.
The flow check makes two `pac` reads per flow, and a read slower than 30 seconds ends the run with exit `3`.
There is no rollback: on a failure, fix the cause and run again.

### Agents

Agents: run the dry run first and show the user the target line; run `--apply --target <name>` only after the user says yes to that environment, and never against an environment the user did not name.

## drift

### Synopsis

`drift --solution <name> --env <env or file> --env <env or file> [--env ...] [--save <dir>] [--format text|json|markdown] [--out <file>]` compares one solution across two or more environments and reports where they differ.

### Options

An `--env` is an environment (an id or URL, as `pac` takes it) or a snapshot file (a path that ends in `.json`, or an existing file), so live and saved environments can be mixed.
A value with a `/` or `\` that is neither a URL nor an existing file is refused, because it would reach `pac` as an environment name; give the environment's id or URL.

`--save <dir>` writes `<solution>.<environment name>.json` for each live environment, after every read has succeeded.
It creates the folder, overwrites a file of the same name, and replaces every character outside letters, digits, `-`, `_`, `.` and space in the name with `-`.
Snapshot files you pass in are not written again.
A later run can compare the saved files offline.

With `--save`, one live environment is enough: it writes that environment's snapshot and reports only the checks that run on one environment (unmanaged layers, unbound connection references, components that are also in another solution), so `no drift` then means those checks found no drift, not that anything was compared.

### What it does

It compares the version, the components, unmanaged layers on a managed solution's components, environment variable values, connection reference connectors and bindings, and cloud flow states.
Each environment is compared with the first.

Each difference is an item with a kind.

These kinds are drift and make the exit code `1`:

- `solution-missing` (installed in one environment of a pair and not the other; nothing else is compared for that pair),
- `version`,
- `component-missing`,
- `component-extra`,
- `agent-missing` (an agent that only one environment of a pair holds),
- `unmanaged-layer`,
- `variable-missing`,
- `variable-value`,
- `reference-missing`,
- `reference-connector`,
- `reference-unbound`,
- `flow-missing`
- and `flow-state`.

Four kinds are information only and are marked `(info)`:

- `managed` (the managed flags differ; Dev unmanaged and Test managed is the normal route),
- `agent-published` (the same agent is published in one environment of a pair and not in the other),
- `layers-not-read`
- and `also-in-solution`.

`also-in-solution` gives one item for each component of the solution and each other solution in the same environment that holds it, and the report names that other solution by its unique name.
A component can sit in two solutions on purpose, so this never changes the exit code.
Only other solutions that are unmanaged and visible are looked at, and the system solutions are left out.
A saved snapshot holds none of these items, so they appear for a live environment only.

A flow that is off in every environment is not drift.
A flow, variable or reference that is missing is reported once under its own kind, not again as a component.

Tables (type 1), canvas apps (300), model-driven apps (80) with their sitemaps (62), environment variables (380 and 381) and connection references get new ids when they are imported, so they are compared by name through their own kinds or not at all, and only other components are compared by id.
App settings get new ids too: type 10087 is left out by id only when a snapshot has no type names (a version-1 file), and with names an app setting is recognized by its definition name (see below).

`unmanaged-layer`, `reference-unbound` and `also-in-solution` are checked in every environment, the first one included.
Connection ids are not compared, because a connection belongs to one environment.
Default values of environment variables are not compared either.

A variable whose value differs by design makes every run exit `1`, and there is no option to accept it; with JSON output, filter `items` by `kind`.
Right after an import with a deployment settings file, a variable whose value the file sets, and that value's record, show as drift against an environment the file didn't set them in.
That's the settings file at work, not a change to undo.

What a run does not compare is listed in the JSON's `notCompared`, as `note:` lines on stderr and as a `Not compared:` list in the Markdown:

- a line when only one environment was read, or when no environment has the solution installed together with the first;
- each type above that is left out by id and compared under no kind of its own (1, 62, 80 and 300, and 10087 in a pair where one side has no type names) when a compared environment holds one, which is most runs;
- each kind left out by name (below);
- each environment compared with the first by number and id because it or the first has no type names;
- each environment with agents or agent components the read did not return;
- and each managed environment whose layers were not read, which is also a `layers-not-read` item.

The exit code doesn't change.

Types of 10000 and above are defined per environment, so the same kind of component can have another type number in each, and an imported agent and its components get new ids.
A live read of a solution that holds a component of type 10000 or above that is not one of its connection references therefore also reads the environment's component definitions (read 9, `solutioncomponentdefinition`: the type number and its name).
When the solution holds an agent (a component whose definition is named `bot`) it also reads its agents (read 10, `bot`: schema name and whether `publishedon` is set), and when it holds agent components (`botcomponent`) it reads those (read 11, `botcomponent`: schema name).
Each is one read of one page, 5,000 rows at most, and a solution that holds none of these components makes none of them, so its cost and the role it needs are as before.

When both environments of a pair hold names (a live read, or a snapshot file of version 2), a component of type 10000 or above is matched by its definition name and its schema name, or by its id when it has no schema name, ignoring case, and its subject reads `<name or type number> <schema name or id>`; two components that share a number but have different definition names never match.
Agents are compared as agents, by schema name, and a `bot` component that has a schema name is not compared as a component: an agent that only one environment holds is `agent-missing`, and one published in only one is `agent-published`.
An agent component that only one environment holds is `component-missing` or `component-extra` named `botcomponent <schema name>`.

The kinds whose ids can change on import are left out by name and counted, not compared: `appsetting`, `bot_botcomponent` and every `botcomponent_*` link table (`botcomponent_connectionreference` included), each with a line `component type <name> not compared: its ids can change on import, so it is counted, not compared by id (<position> <environment>: <count>, ...)` that counts it in every environment of the pairs compared by name.
The app setting and the agent's connection reference link were seen to get new ids; the other link tables are left out because Microsoft Learn lists them as the agent component's link tables, and their ids were not observed to change.
A component of any other kind whose ids change shows as drift.

A pair where one side has no names (a snapshot file of version 1) is compared as before, by type number and id, with type 10087 left out by id, and when that pair still compares a component of type 10000 or above (other than 10087 and the connection references) the report says `<position> <environment>: compared with the first by type number and id, because it or the first has no type names (a snapshot from an older powerops), so agents were not compared`.
An agent or agent component that the read did not return (the account may not see it) is compared by id, and the report says `<position> <environment>: <n> agent(s) or agent component(s) were not returned by the read (the account may not see them), so they were compared by id`.
A name read from the environment is compared and printed, never put into a query.
These three reads have not run against a tenant yet.

`drift` only reads.
For each live environment it runs `pac org who --environment <env>` for the friendly name and the environment URL, asks `az account get-access-token` for a token for that URL, then sends Web API GET requests with FetchXML.
It does not parse `pac env fetch`, because Microsoft Learn documents no output format for it.
The token is held in memory and sent only in the `Authorization` header.
It is never printed or saved, and neither is the URL. powerops sends the token only to documented Dataverse hosts (`*.dynamics.com`, `*.crm.microsoftdynamics.us`, `*.crm.appsplatform.us`, `*.crm.dynamics.cn`); an environment URL on any other host exits `2` before a token is requested.

Prerequisites: `pac` signed in (`pac auth create`), and the Azure CLI on `PATH` and signed in with `az login`.
If `az` is installed elsewhere, add its folder to `PATH`, for example `fish_add_path <folder>` or your shell's equivalent.
The two sign-ins can be different accounts: `pac org who` runs as the pac profile, and the rows are read as the `az` account.
An environment in a different tenant from the `az` sign-in is not supported.

The reading account needs to be able to read the solution, solution component, workflow, environment variable and connection reference tables; the minimum security role was not tested.
Read 9 also needs read on `solutioncomponentdefinition`, for which Microsoft Learn gives no source.
Reads 10 and 11 need read on `bot` and `botcomponent` at organization level, which System Administrator and System Customizer have (*Secure your Copilot Studio projects*), while Environment Maker reads only its own agents: an agent another maker owns is then not returned, and is compared by id with a not-compared line, or shows as `agent-missing` when only one side returns it, and the side that did not return it also shows its `bot <id>` component as `component-missing` or `component-extra`, because that component is compared by id.
In CI, `drift` needs an `az` sign-in beside `pac`'s.

### Output

Output names an environment by its position (`1`, `2`, ...) and its friendly name, because friendly names are not unique.

The snapshot of a live environment where the solution is installed is written at version 2 of the file format, also when no type names were read (its `name` and `schemaName` fields are then null and `agents` is empty); where the solution is not installed it is written at version 1, as before.
Version-1 files still load.
A powerops from before this change refuses a version-2 file with exit `3`, so compare saved files with the version that wrote them or a later one.

Values are never printed.
A snapshot file holds a SHA-256 of each variable's current value, never the value.
A hash hides a value from a reader, but not from someone who guesses a short or known value, so keep reports and snapshot files in private folders and private CI artifacts.
Friendly names are printed and saved, which is another reason.
`layers not read` appears for a managed solution whose layer reads were skipped (see the cost below).

When a call fails, the message names the `--env` by position and the read.
Text forwarded from `pac`, `az` or the Web API goes to stderr only, never to the report or a file, and may repeat an `--env` value or the environment URL.

JSON follows [docs/schemas/drift.schema.json](schemas/drift.schema.json), and snapshot files follow [docs/schemas/snapshot.schema.json](schemas/snapshot.schema.json).

### Exit codes

Exit codes:

- `0` no drift,
- `1` drift,
- `2` usage error (no `--solution` or a name that is not a solution unique name, fewer than two `--env` without `--save`, an `--env` ending in `.json` with no file, an `--env` with a path separator that is neither a URL nor an existing file, `--save` naming a file or used with no live environment, a snapshot for another solution, two environments that would be saved to the same file name, a solution installed in none of the environments, or an environment URL on a host that is not a documented Dataverse host),
- `3` a `pac` or `az` call or a Web API read failed, timed out or returned output it cannot read, `pac` or `az` not found, or an unreadable snapshot file.

### Limits

Each live environment costs one `pac` call, one `az` call and five Web API reads, each with a 30 second limit, plus one more read for every 50 components of the solution to find `also-in-solution` items.
A read returns one page of at most 5,000 rows, so those extra reads number at most 100; a solution with that many components was not tested.
A solution that holds a component of type 10000 or above that is not a connection reference costs one more read (the definitions), and one more for each of the agents and the agent components when it holds any.

The default solution is left out by its friendly name, in the two spellings Microsoft Learn uses, so an environment that stores another name for it shows the default solution as an `also-in-solution` item.

For a managed solution it also makes one layer read per component that is a table, flow, canvas app, environment variable definition or connection reference (the layer table takes one component per query; about 0.4 s each in the capture), to find unmanaged layers; other component types are not checked for layers.
More than 50 such components skip the layer reads, and the report says `layers-not-read`.
A read returns one page of at most 5,000 rows; more than that exits `3`.

Tested with `pac` 2.13.1 and Azure CLI 2.90.0 on Linux, live on 2026-10-22 against an unmanaged and a managed install in two test environments in the maintainer's tenant, with an account whose role covered the reads; the minimum role for the reads was not tested.
A live run on Windows 11 was reported working on 2026-10-30 (`pac` 2.14.1, Azure CLI 2.91.0) and has not been repeated.

### Agents

Agents: run `drift` only against environments the user named in the session.

## deploy plan

### Synopsis

`deploy plan <folder> --target <environment> [--save <plan file>] [--planned-by agent|person|action] [--format text|json|markdown] [--out <file>]` says, before anything is imported, what importing the exported solution zips in `<folder>` into the target would do.

### Options

`--target` is an environment (an id or URL, as `pac` takes it and as `drift`'s `--env`); it is never printed, and the report names the target by its friendly name.

When `--save` names a file whose folder is not `<folder>` (one folder up, a subfolder or elsewhere), `deploy plan` prints `warning: the plan file is not saved in the deploy folder; deploy apply reads the zips and settings files from the plan file's own folder` on stderr and still writes the file; stdout, the plan file, its hash and the exit code are the same as without the line.
It warns because `deploy apply` would not find the zips beside a plan file saved elsewhere.
The folders are compared as text, so two spellings of one folder through a link may warn.

`--save <file>` writes the JSON report to a file whatever `--format` is, so one run gives the Markdown for the PR and the plan file.

`--planned-by` puts `agent`, `person` or `action` in the JSON's `plannedBy` (null without it): who made the plan, as the caller says.
The MCP tool `deploy_plan` sets `agent` and the Action sets `action`.
Nothing checks it, it is not a hash input, and `deploy apply` doesn't read it.

### What it does

It reads the target and writes nothing to it; its only file writes are `--out` and `--save`, and a temp folder for the unpack of a zip that a review links by its deploy-hash (below), which it deletes.

Every file directly in the folder (not in subfolders) whose name ends in `.zip`, in any case, is one solution.
Beside a zip, `<zip name without .zip>.settings.json` (the exact name, at most 1 MB) is its deployment settings file in `pac`'s format, as in `settings validate`.
A zip or settings file name may hold only letters, digits, `.`, `_` and `-`.

The target is read the way `drift` reads it (one `pac org who`, one `az` token and the same Web API reads for each solution in the folder, except the read of components that are also in another solution, which `deploy plan` does not make; for each other solution a zip depends on, only whether it is installed and its version), so the same limits apply: documented Dataverse hosts only, 5,000 rows per read, and text forwarded from `pac`, `az` and the Web API goes to stderr only.
Its target read takes none of the type-name, agent and agent-component reads of `drift`, so its target snapshots, and the plan hashes made from them, are unchanged by them.

The account needs read on every table those reads touch, including the table of each component whose layers are read: in a live run, a role without `prvReadCanvasApp` got no layer rows for a canvas app instead of an error, and the plan exited `3` with "no layer rows".
[The example's setup](examples/README.md#the-plan-apps-role-in-test) lists a read-only role that worked in a live run.

The plan lists the solutions in import order: a topological sort on each zip's dependencies on the other zips in the folder (from its `<MissingDependencies>`; ties in name order).
Each solution gets an action: `install` (not installed in the target), `upgrade` (the zip's version is higher) or `skip` (the same or a lower version, as `pac solution import --skip-lower-version`).
Versions must have four numeric parts.

For an install or upgrade it checks the settings file with `settings validate`; a zip with no settings file is checked as if the file were `{}`, so a connection reference, or a variable with no default or current value, is a problem, because `pac solution import` binds references only from a settings file.
It also checks the dependencies and gives each cloud flow's expected state after the import (`settings apply`'s prediction, against the flow's state in the target).

Every finding is an item with a kind and a severity.

These block the plan (exit `1`):

- `cycle` (a dependency cycle among the zips),
- `missing-dependency` (a required solution that is neither installed in the target nor earlier in the order),
- `managed-conflict` (a managed zip over an unmanaged install, or the other way round, for any action)
- and `settings`.

These are warnings:

- `dependency-not-checked` (a `<Required>` that names no solution),
- `deletes`,
- `unmanaged-layer`
- and `agent`.

These are information: `layers-not-read`, `components-not-checked` and `same-version-differs`.

An `agent` item names each Copilot Studio agent (by its schema name) in a zip whose action is `install` or `upgrade`; a skipped zip gives none.
The import may leave the agent published in the target: one live run did, and Learn describes publishing as a separate step (*Export and import agents using solutions*, read 2026-11-09).
So a plan that forbids a published agent there should not be imported until the person has said yes to that and the plan is amended.
The item does not block, and the summary line then reads `plan ok, but the import holds <n> Copilot Studio agent(s) it may leave published` instead of `plan ok`.
Its JSON is at `schemaVersion` 2 for the new kind, and `deploy apply` reads only plan files of version 2, so plan again after an upgrade.

The deletion check applies to the upgrade of a managed zip.
It names the cloud flows, environment variables and connection references of the target's solution that the zip no longer has, which the upgrade deletes.
Tables and canvas apps are compared by count only, because the target names them by id: when the target's solution holds more of them than the zip's root components, one item says the upgrade deletes at least that many (a deleted table loses its data).
Other component types are counted and listed as not checked, so their deletion is not checked.
For the upgrade of a managed zip, a `components-not-checked` item also names how many tables and how many canvas apps the target's solution holds (one item for each type it holds any of), because their deletion is checked by count only and a table replaced under another name is not seen.

`unmanaged-layer` names components of a managed target solution that have an unmanaged layer, which the import does not override; the live read reads the layers of a managed target solution the way `drift` does (one read for each of its components that is a table, flow, canvas app, environment variable definition or connection reference).
When that solution has more than 50 such components, no layer is read for it, unmanaged layers are not checked, and the plan says `layers-not-read`.

`same-version-differs` applies to a `skip` whose version equals the installed version, compared as numbers; a lower version gets no such item.
It names each cloud flow (by id; other workflows are not compared), environment variable (by schema name, ignoring case) and connection reference (by logical name, ignoring case) that only the zip or only the target's solution has.
It comes from what the plan already reads, so it costs no extra read, it does not block, and it does not change the plan hash.
It sees only which of these exist: it cannot see a changed flow action, a renamed flow, a changed value or connector, or any other component type, so no item does not mean that the zip equals the target.
To import a zip that did change, raise its version: the plan then gives an `upgrade`.

`deploy plan` also says which zips no review covers.
It looks for `docs/plans` in the deploy folder and then in each folder above it, no further than the folder that holds `.git`, and takes the first it finds as the repository's.
It checks only the zips the plan imports; a zip the plan skips is not imported, so it gets no line.

For each of them, in plan order, it prints `warning: <zip>: <sentence>` on stderr unless one plan folder covers the zip: that folder's `verify.md` and `security-solution.md`

- both exist,
- both have `state: ready`,
- both carry the plan's current hash as `plan-hash:`,
- and each is linked to the zip ([plan status](#plan-and-skills) reads the same lines).

A report is linked when its `solution-hash:` is the zip's SHA-256, or when its `deploy-hash:` is the zip's SHA-256 and its `solution-hash:` equals the digest of a fresh `pac solution unpack` of that zip.
A `verify.md` of one plan and a `security-solution.md` of another make no cover.

The sentence is either `no verify.md or security-solution.md under docs/plans records this zip's SHA-256 as its deploy-hash or its solution-hash`, or, when a report of some plan folder does record it, `docs/plans/<name>: ` for the first such folder in name order, then what is wrong with its reports, joined with `; `:

- `<file> is missing`,
- `<file> records another deploy-hash and solution-hash`,
- `<file> records this zip as its deploy-hash, but the zip's unpack could not be checked: <failure>`,
- `<file>'s solution-hash is not the digest of this zip's unpack, so it reviewed another folder`,
- `<file> has no state` or `<file> is <state>` (such as `blocked`),
- or `<file> was not written for the current plan`.

With no such `docs/plans` it says nothing about reports, and when that folder can't be read it prints `warning: docs/plans could not be read, so the zips were not matched with review reports`.
The check reads only those reports: it changes neither the report, the plan file, the hash nor the exit code.

For a zip that no report records, it also prints `note: <zip>: ...` lines as evidence, never as a cover.
For each ready report whose `input:` (a folder or a zip under the repository root) is of a solution with the zip's unique name and still has the digest the report recorded, one note says `docs/plans/<name>: reviewed '<input>' (digest current); diff against the zip: <n> changes besides the managed flag; not compared by diff: security roles, tables, columns, forms, views, app modules and their role maps; this is evidence, not a link` (reviews of one input share a note, which names up to three plan folders and then `and <m> more`; `; diff could not read <k> parts` follows the count when `diff` left a part unread).

When no such input is current but ready reports of that solution name an input that changed since, one line says `<c> ready reports under docs/plans reviewed this solution, each in an input that changed since the review; none was compared with the zip`.
An input that is missing, is or passes through a symbolic link, leaves the repository or can't be read gives no note and no error.
A note shows that the reviewed input is unchanged and how many changes `diff` finds between it and the zip; it does not show that the zip equals the input in anything `diff` doesn't compare, and it does not make the review cover the zip.

`deploy apply` prints the warning lines, not the notes ([deploy apply](#deploy-apply)).
The MCP tool `deploy_plan` returns the lines with the JSON, and the Action's job log shows them; the PR comment does not.

The unpack runs only for a zip that some `verify.md` or `security-solution.md` records as its `deploy-hash:`, once for each such zip: `pac solution unpack --zipfile <zip> --folder <folder> --packagetype Managed` (`Unmanaged` for a zip that isn't managed), with a timeout of 120 seconds, into a new folder under the system temp folder.
That temp folder is the only thing it writes, and it is deleted afterwards.
`pac`'s own output is never printed, because it holds full paths.

The `<failure>` is one of

- `pac was not found`,
- `pac solution unpack timed out`,
- `pac solution unpack exited <code>`,
- `the unpacked folder could not be read` (also when the unpack holds no file)
- or `no time was left to unpack it` (in `deploy gate`, when 10 seconds or less of its deadline were left).

For a zip whose covering plan folder links both reports through their deploy-hash, `deploy plan` also prints `note: <zip>: docs/plans/<name>: both reports record this zip's SHA-256 as their deploy-hash, and the digest of its pac solution unpack is their solution-hash`.

A report is written for the managed zip's unpack when the review request names the zip and the unpack folder ([HANDOFF step 3](HANDOFF.md#the-steps)): the role records the zip's digest from `powerops manifest <zip>` as its `deploy-hash:` and the folder's digest as its `solution-hash:`.

A zip that changed after the review has another SHA-256, so no `deploy-hash:` matches it and no unpack is run.
A digest that differs although the folder is the zip's unpack comes from another `pac` version, another operating system (not checked), or another unpack option (`--localize`, `--map`, `--sourceLoc`, or what a skill adds).

To recover, unpack again into a new or emptied folder with exactly the command above and the `pac` version that runs `deploy plan` (in the Action, the runner's), and run both roles again.
Failing that, review the zip itself (both roles with the zip as the current solution: its `solution-hash:` is the zip's SHA-256, and a role can't read files inside a zip, so those reports mark roles, tables, forms and views as not verified), or leave the import to the person's own `deploy apply`, which warns.

### Output

The plan has a hash (SHA-256, lowercase hex) over its inputs: the target's environment id, each zip's bytes, each settings file's bytes (a change of line ending is a change), the target snapshot of each solution in the folder, and the installed version of each dependency read from the target.
The same inputs give the same hash.
The hash covers the inputs, not the findings.

The plan file holds the tool version (`tool`), the hash, the target's friendly name, and `inputs`: for each zip the SHA-256 of the zip, of its settings file and of the target snapshot, and the dependency versions, so a later step can say which input changed.
It holds no environment id or URL, no folder path, no connection id and no variable value.

The Markdown report stops at whole rows before 60,000 characters and says how many rows it left out.
JSON follows [docs/schemas/deploy-plan.schema.json](schemas/deploy-plan.schema.json).

### Exit codes

Exit codes:

- `0` plan ok,
- `1` blocked (the report and the plan file are still written),
- `2` usage error (no folder, no zip, a file name outside the rule, a symbolic link, two zip names that differ only in case, two zips with the same unique name, a settings file with no zip of that exact name, no `--target`, an `--out` or `--save` that names a folder, an `--out` and `--save` that name the same file, or a URL on a host that is not a documented Dataverse host),
- `3` an unreadable zip, settings file, version or flow id, a failed, timed-out or unreadable `pac`, `az` or Web API read, or `pac` or `az` not found.

### Limits

A block is a rule of the plan, not of `pac`: when one is wrong for your case, fix the input, or import with `pac` directly, outside PowerOps.
The plan has no waiver.

### Agents

Agents: run `deploy plan` only against environments the user named in the session, and show the user the plan before asking for approval.

## deploy apply

### Synopsis

`deploy apply <plan file> --target <environment> [--apply] [--require-reviews] [--github-run <text>] [--format text|json] [--out <file>]` imports a reviewed plan.
The plan file is the one `deploy plan --save` wrote; its folder is the deploy folder, so the zips and settings files are read from there.
Without `--apply` it is a dry run that writes nothing but the temp folder of the review check's unpack (below).

### Options

`--target` is the environment's id or URL, as `pac --environment` takes it.
The plan file's `target` is the environment's display name, which `--target` doesn't take: when the read of the target fails on that name, one more error line says so.

`--require-reviews` makes the review check of `deploy plan` a condition, for the dry run as well as for `--apply`.
It runs after the plan was made again and passed its hash check and is not blocked, on the zips that the fresh plan imports, because the plan file's own actions are not covered by its hash: a plan file edited to mark an uncovered zip `skip` is still refused.
A report covers a zip as in `deploy plan`: its `solution-hash:` is the zip's SHA-256, or its `deploy-hash:` is and its `solution-hash:` equals the digest of a fresh `pac solution unpack` of the zip (the unpack, its temp folder, its failure sentences and the recovery are described under [`deploy plan`](#deploy-plan)).

It writes the same `warning:` lines as `deploy plan` (no notes) to stderr, then refuses with exit `1` and status `refused`, before any import, in three cases, each with a `message` that ends in `nothing was imported.`:

- no `docs/plans` at or above the plan file's folder (`no docs/plans folder at or above the plan file's folder, so no review covers the zips; nothing was imported.`),
- a `docs/plans` that can't be read (`docs/plans could not be read, so the zips were not matched with review reports; nothing was imported.`),
- and a zip that no plan folder covers (`no review covers <zip>: <sentence>; nothing was imported.` for the first such zip in plan order, with ` (<n> zips in all; the warning lines name each)` before the semicolon when more than one is uncovered).

A plan that imports nothing, because every zip is a `skip`, passes without looking for `docs/plans`.
Without the option the same `warning:` lines are written and the run goes on and imports; a folder with no `docs/plans` is silent, as for `deploy plan`.

The check reads what the reports say and doesn't prove that a role wrote them, so the form, the person's yes or the GitHub environment's approval stays the authorization.
The MCP tool `deploy_apply` and `deploy gate` always require the reviews, and the Action does unless its `require-reviews` input is `false` ([the Action](GITHUB-ACTION.md)).

### What it does

It refuses before reading the target when the plan file is blocked or was made by another powerops version (exit `1`); the version is what `powerops --version` prints, which for a build from a clone includes its commit, so a build of another commit counts as another version.
Then it plans again against the target with `deploy plan`'s code, and exits `2` when the target's friendly name is not the plan's, and refuses (exit `1`, nothing imported) when the new plan's hash differs from the file's, naming the first input that changed, or when the new plan is blocked.

Otherwise the dry run lists what it would import, and the Copilot Studio agents in those zips, and with `--apply` it runs `pac solution import` for each `install` or `upgrade` in plan order (`--settings-file` when the zip has one, `--stage-and-upgrade` for the upgrade of a managed zip), hashing each file again just before its import.
When every solution is a `skip`, nothing is imported, and the report says `imported` with the message `Nothing was imported: every solution was skipped.`: nothing changed.

It stops at the first failed import and names the solutions already imported: there is no rollback.
An import that times out leaves its row `unknown` (it may have finished or still be running, so check the solution's import history in the environment) and still exits `3`.

Afterwards it reads the target again and reports each planned cloud flow's state; exit `1` when a flow is not in the state the plan expected.

It also reads the agents of every solution whose import went through (`ok`), once, also when the plan has no flows and after a later import failed: reads of the Dataverse `bot` table by schema name (one per 50 distinct names), with its own `pac org who` (which must name the environment imported into) and `az` token.
Each agent is `found`, `missing` or `not-read`, and a found one has `publishedOn`, which says only that the row's `publishedon` is set (*Agent (bot) table/entity reference*, read 2026-11-09).

`missing` means no row came back, which also happens when the reading account can't see that agent: Environment Maker sees only the agents it owns or that are shared with it (*Secure your Copilot Studio projects*, read 2026-11-09).
This read has not yet run against a tenant, so `missing` is a prompt to check in Copilot Studio, not proof.
Nothing is read before the import, so for an upgrade, or an agent the target already held, `publishedOn: true` doesn't show what the import did.

`not-read` means the read failed (a `warning: the agent read-back failed: ...` line is on stderr) or the name is not one powerops sends (letters, digits and underscores, at most 100 characters).
An agent of a solution whose import timed out is not read.
The agent read changes no `status`, `message`, solution row or exit code.

### Output

The target's id, its URL, connection ids, setting values and `pac`'s own text never reach the report; `pac`'s output goes to stderr with every line prefixed `  | `.

The report also records how the run came about, as its caller reports it.
`executor` is `action` with `--github-run`, `mcp` for the MCP tool `deploy_apply`, else `cli`.
`approval` is null for a dry run; with `--apply` it is `form` when the MCP form was answered, `action` with the `--github-run` text as its `receipt`, else `cli`.
`appliedAt` is the UTC time at which an `--apply` run passed its checks and started importing, and null for a dry run or a refusal.

These fields are claims, not proof: none names who approved, and `--github-run` is whatever the caller passes (1 to 500 characters, with no control, format, separator or private-use character; anything else exits `2`), so a report alone can't tell the Action from someone who typed the flag.
The Action passes its repository, run, attempt and GitHub environment; an auditor opens that run, where GitHub records who approved the job.
A refused `--apply` run keeps its `approval`, so read `status` and `appliedAt` first.
A local `--apply` records `cli`, and nothing checks who ran it or whether anyone said yes.

JSON follows [docs/schemas/deploy-apply.schema.json](schemas/deploy-apply.schema.json).

### Exit codes

Exit codes: `0` applied (or a dry run that would apply), `1` refused or a flow not as expected, `2` usage error or a `--target` mismatch, `3` an unreadable plan file, zip or settings file, a failed `pac`, `az` or Web API call, a file changed before its import, or a failed import.

### Agents

Agents: run `deploy apply` without `--apply` freely against an environment the user named.
The yes to an import is held in two places an agent can't answer for: the form of the MCP tool `deploy_apply` with `apply: true`, where the user types the environment's name (interactive clients only), and the Action's apply job in a GitHub environment with required reviewers ([the Action](GITHUB-ACTION.md)).
Call the tool with `apply: true` only after the user said yes to that environment in the session.
A local `deploy apply --apply` is the user's own command, and nothing enforces who runs it: leave it to the user, never as a way around the form.
The gate's message to a blocked agent says the same.

## deploy gate

### Synopsis

`deploy gate <plan file>` is the command that a `PreToolUse` hook of Claude Code or Codex runs before Microsoft's import tools.
It reads the hook's JSON on stdin (UTF-8, at most 1 MB), writes nothing to stdout, and exits `0` or `2` only, because a hook blocks only on `2`.
It never imports anything and never writes a file.

### What it does

A call that is not an import (any tool but `Bash`, `Monitor`, `PowerShell` and the MCP tools named `mcp__<server>__solution_import`, `__pages_upload`, `__pages_upload-code-site` (Codex: `__pages_upload_code_site`), `__copilot_publish` and `__model_genpage_upload`, and a shell command with no import in it) exits `0` without reading the plan or the target.
The four MCP tools other than `solution_import` are always blocked, without reading their arguments, the plan or the target.
No plan check covers them or the shell routes below, so with the hooks on the user runs such a command, or turns the hooks off for that step after saying yes to it.

For an import it allows the call only when all of these hold:

- the call is pac's MCP `solution_import`, or one plain `pac solution import` in a Bash command, with a literal absolute `--path`, an `--environment` that is an id or the https URL of a Dataverse host (no user info, port or path), and optionally `--settings-file` and `--stage-and-upgrade`.
  The options that change only how `pac` waits (`--async`, `--max-async-wait-time`) and `2>&1` are allowed; any other option, a variable, a wildcard, a relative path, a network path or any other redirection blocks.
  In the MCP form every argument is a string, `--async` is `"true"` or `"false"`, a path holds only letters, digits and `/ \ : . _ - ~`, and an unknown key blocks;
- the dry run of `deploy apply` passes with the call's `--environment` as `--target`: the plan file is readable, not blocked and made by this version, the target has the plan's friendly name, and the hash of the zips, settings files, target and dependencies is the plan's, and each zip the plan imports is covered by both review reports of one plan folder, as `deploy apply --require-reviews` checks it (the block then reads `the dry run did not pass: no review covers <zip>: ...`, after the `warning:` lines);
- the call's zip is the plan's first import (the same bytes), with the settings file of that step (the same bytes, or none when the step has none), and `--stage-and-upgrade` exactly when the step stages and upgrades.
  Both files must be regular files, and they are hashed again just before the gate allows the call: a file that changed during the dry run blocks.

Everything else blocks:

- another zip,
- a later or skipped zip,
- another environment (Dev when the plan is for Test),
- a plan of skips only,
- and the routes that cannot be checked:
  - model-apps' `solution-settings.js import`,
  - power-pages' `import-solution` and `deploy-pipeline`,
  - create-site's template import,
  - `pac package deploy`,
  - `pac solution upgrade`,
  - `pac pcf push`,
  - `pac application install`,
  - `pac code push`,
  - `pac catalog install`,
  - `pac app-management install-application-package`,
  - `pa app push` (also run through `npx`, with or without its options, from `node_modules/.bin`, or as a `.cmd` shim, and the retired `power-apps push`),
  - `pac pages upload`, `pac pages upload-code-site` and `pac pages migrate-datamodel` (also as `pac powerpages` or `pac paportal`, and every `migrate-datamodel` form, its status check too),
  - `pac model genpage upload`,
  - `pac copilot push`,
  - `pac copilot publish`,
  - `pac copilot init` (with or without `--environment`),
  - `manage-agent.bundle.js push` and `publish` (the `copilot-studio` plugin's `manage-agent` skill; its other verbs pass when they are the word after the script, and any other call to the bundle is unreadable),
  - `Import-CrmSolution`,
  - any import in PowerShell,
  - a command that holds an import or a route the gate cannot read (`sh -c`, `eval`, a variable, or a mention such as `grep "pac solution import" docs` or `git commit -m "... pa app push"`, or one that names the bundle, such as `ls scripts/manage-agent.bundle.js`),
  - and a Bash command with a `$'...'` word that holds an escape.

A plan-file argument that starts with `-` blocks too.
The gate has no help: `-h`, `/h`, `--help`, `-?` and `/?`, with or without a plan file, write one `error: deploy gate has no help` line and exit `2`, because help would exit `0` and let the call through.

The check has a deadline of 300 seconds; past it the gate blocks.
An exception blocks too, so a failure never lets a call through.

### Output

Each block writes `error: deploy gate: ...` lines on stderr and ends with one line that says what to do next.
A block after the dry run passed (not a zip of the plan, another zip first, a skipped zip, a settings file or `--stage-and-upgrade` that differs, a file that changed during the check) also writes one `warning: deploy gate: <solution>: ...` line for each `agent` item of the fresh plan, between the reasons and that last line.
A block before the dry run passed has no plan, and a plan that imports nothing has no agent item, so neither has the line.
A call the gate lets through shows nothing.
The environment value, folder paths and the command text are never printed.

### Exit codes

The exit codes are `0` allowed and `2` blocked, for every other outcome.

### Limits

The gate blocks the routes it knows: a new pac command or MCP tool that installs, imports, uploads or publishes passes until the gate gets a route for it.
For agents, it doesn't see Apply in the Copilot Studio extension for VS Code, the Copilot Studio MCP server's `manage_agents` and `manage_workflows`, FlowAgent's `copy_flow`, `publish_flow` and `update_flow`, or the Power BI Authoring MCP server, so those pass, and it has no route for `pa app share`, `pa connection create`, `pac copilot quarantine` or `pac copilot merge-translation` ([Hand-off](HANDOFF.md#gate-microsofts-imports)).

The gate does not see powerops' own commands: an agent's own `powerops deploy apply --apply` passes through no gate, gets the warnings of the review check only, and goes on.
A pass shows that the call matches the plan file at the given path and the live target.
It is not a record that anyone reviewed that file, and it is not the user's yes to the import.

## plan and skills

### Synopsis

Plans live in `docs/plans/<name>/plan.md`, with a header between `---` lines (`status`, `review`, `signed-off`, `signed-off-hash`, `environment`, `depth` and `supersedes`).

```sh
powerops plan status                  # check every docs/plans/*/plan.md under . (offline, read-only; add --format json for machine output; --json is the older spelling, and wins over --format text)
powerops plan sign-off docs/plans/site-visits   # write status, date and body hash into the header
powerops plan facts                   # environment and solution names as JSON; read-only, needs pac signed in, prints names only
powerops skills install               # copy the agent skills into .claude/skills/ and/or .agents/skills/ (--agent claude|codex|gemini|copilot, --force to overwrite)
```

What the planning skills do, and how to install them as a plugin: [Planning skills](USE-WITH-AGENTS.md#planning-skills).

### What it does

`plan status` takes a repo folder (the default is `.`), a plan folder or a `plan.md`.
Only a folder directly inside a `docs/plans` folder counts as a plan folder; any other folder is a repo folder, even when it holds a `plan.md` of its own, so a `plan.md` at the repo root does not hide the plans under `docs/plans`.

`plan status` checks the header, that a signed-off plan still matches its recorded hash, that the review is done (with `review.md`) or skipped, and that no file in the plan folder holds a URL, GUID, email or personal environment name, the default name of a Developer environment (`Jane Doe's Environment` or `Jane Doe's (1) environment`; the lower-case form is caught on an `environment:` line; add `<!-- plan-status: allow -->` to a line to allow it).
The name is never printed.

`plan sign-off` refuses unless `status` is `approved`, the review is done or skipped, and the scan is clean; it edits only the header.
On success it also prints, on stderr, the report warnings `plan status` gives for the folder (a stale or hashless `review.md`, a stale `verify.md` or `security-solution.md`, an unknown state) and still exits `0`.
When `Screens` names anything, it also refuses until the last `prototype:` line under `## Log` in `decisions.md` is `approved mockups/prototype.html`, with a file that passes the sketch checks beside at least three options (`mockups/option-*.html`) that pass them and differ, one of which the line names as `(option <letter>: <why>)`, or `skipped (<reason>)` ending `(answered by: person)`.
`plan status` shows the same prototype problems as warnings on a `draft` or `approved` plan, and warns when an `approved` line is not marked `(answered by: person)`.

### Output

The text line of a plan reads `<path>: <status>, review <review>, <n> open questions, <m> admin questions`, and the JSON has `openQuestions` and `adminQuestions`, each counting the list items under its own heading (`## Open questions`, `## Questions for your admin`).

`plan status` also lists the reports in each plan folder: `review.md`, `security-solution.md` and `verify.md`, in that order, when present.
In the JSON, `reports` holds one entry for each, with `file`, `state`, `stale`, `input`, `solutionHash` and `solutionStale`, and is empty when there is none.
The text output prints one line for each: `report <file>: state <state>, stale (plan) <true|false|null>` (the line of `review.md` has no `state` when the file has no `state:` line), and for `security-solution.md` and `verify.md` also `, input '<path>'` (or `, input null`), `, solution-hash <value>` and `, stale (solution) <true|false|unknown>`.

`state` is the value of the `state:` line in the report's first six lines, or null when there is none (`review.md` has none).
`input` and `solutionHash` come from the `input:` and `solution-hash:` lines in the same six lines of the two solution reports, and are null for `review.md`.

`input` is the path of the solution the report read, without a trailing `/`, when the value is a relative path: 1 to 260 characters after one trailing `/` is dropped, with `/` between names, no control character, no `:` or `\`, not starting with `/`, and no empty, `.` or `..` name; otherwise it is null, and a present `input:` line that is not such a path adds the warning `<file> has an input: line that is not a relative path`, so a report written before these lines existed, whose `input:` was free text, warns on every run.

`solutionHash` is the digest from `powerops manifest` when the `solution-hash:` value is exactly 64 lowercase hex characters, and `unknown` otherwise (no line, a malformed value, a report written before the line existed, or a file that was not read), without a warning.

`stale` is true when the report's `plan-hash:` differs from the plan body's hash: the plan body changed since the report was written.
It is null when the report or the plan has no hash.

`solutionStale` says whether the solution changed since the report was written.
`plan status` reads the solution that `input` names, under the repository root (the folder that holds `docs/plans/<name>/`, whatever the current folder), by `manifest`'s rules (the digest of an unpacked folder, the SHA-256 of a `.zip`), and compares the digest with `solutionHash`.
`solutionStale` is true when they differ, which adds the warning `<file> was written for a different version of the solution in '<input>' (stale solution)`, and false when they match.

It is null (`unknown` in the text) when nothing was compared:

- always for `review.md`;
- with no warning when `input` is null or `solutionHash` is `unknown`, as for a report written before these lines existed, or when the solution is not there, as for a build output a clone doesn't hold;
- and with a warning that says why when the plan folder is not in `docs/plans`, a name on the way to the solution is a symbolic link, or the solution can't be read (a file that isn't a `.zip`, over one of `manifest`'s caps, or, on Windows, a name such as `CON` or one that ends in `.` or a space, which is not opened).

`input` is matched as written, so on Linux its case must match the folder's; only line endings are evened out, as in `manifest`, and each solution is read once per plan.
As `manifest` says, the solution's own folder is the one to name: a folder that also holds the plan folder gets a new digest whenever a report is written there.

A `state` other than `ready`, `blocked`, `failed` or `dropped`, and a stale report of either kind, each add a warning.
Neither is a violation, so neither changes the exit code.
A report that is a link, binary or over 1 MB keeps the warning it had and shows null for `state`, `stale`, `input` and `solutionStale`, and `unknown` for `solutionHash` (null for `review.md`).

`plan status` also lists the plan's write journal, `writes.md`, where `powerops-implement` adds a table row for each tenant write it attempts.
In the JSON, `writes` is null when the folder has none, and otherwise holds `entries`, the table's rows without header and separator lines, and `openCleanup`, the rows whose last cell is neither `none` nor starts with `done`; both are null when the file was not read.
The text output prints `writes writes.md: <n> entries, <n> open cleanups`, or `not read`.

It warns on each item under `## Log` in `decisions.md` that is marked `answered by: agent` and names a sign-off (`sign-off answered by an agent`) or an acceptance (`acceptance answered by an agent`), with its file and line.
The agent writes the marker itself, so the warning catches honest marking only.
Neither changes the exit code.

### Exit codes

Exit codes: `0` clean, `1` violations or a refused sign-off, `2` usage error or malformed header, `3` internal error.

### Agents

Agents: run `plan facts` only when the environment `pac` is signed in to is one the user named in the session.

## mcp

### Synopsis

`mcp` runs a Model Context Protocol server over stdio until the client closes stdin (exit `0`, or `3` on an internal error).

### What it does

A tool input is refused, with an `error:` line and nothing run, when a string holds a control character or an unpaired surrogate or is longer than 4,096 characters, or when `paths` has more than 50 entries or `envs` more than 10.
The tool `deploy_apply` always passes `--require-reviews`, for its dry run, the dry run before its form and the import, so a report that turns `blocked` while the form is open stops the import; it has no way to turn the check off.
Its tools, what they write, and how Claude Code and Codex start it: [Use from an agent (MCP)](USE-WITH-AGENTS.md#mcp-server).
