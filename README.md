# PowerOps

A free, open-source, cross-platform toolkit that brings software-engineering discipline (analysis, diffing, testing, repeatable deployment) to Microsoft Power Platform. It works alongside `pac`; it does not replace it.

**Status:** early development, not yet released. Built so far: `analyze`, `baseline`, `rules`, `diff`, `settings` (`generate`, `validate`, `apply`), `drift`, `plan` and `skills`.

## Install

```sh
dotnet tool install --global PowerOps
powerops --version
```

Until the first release is on NuGet, install from a clone instead:

```sh
dotnet pack src/PowerOps.Cli -c Release -o artifacts && dotnet tool install --global --add-source artifacts PowerOps
```

## Quick start

In any CI system that has the .NET SDK, one `run:` line writes a SARIF report and fails the build on errors:

```sh
dotnet tool install --global PowerOps && powerops analyze --format sarif --out powerops.sarif
```

On GitHub Actions, use the action. It writes a job summary and annotations, and uploads the findings to code scanning:

```yaml
permissions:
  contents: read
  security-events: write   # for the SARIF upload

steps:
  - uses: actions/checkout@v7
  - uses: Chrome71/PowerOps@v1
    with:
      path: solutions        # default: .
      fail-on: error         # error | warning | none
      # baseline: powerops-baseline.json
```

Until `v1` is tagged, use `Chrome71/PowerOps@main` (the action builds PowerOps from source). Pull requests from forks cannot upload SARIF (their token has no `security-events` permission); they still get the job summary and the annotations.

## Usage

```sh
powerops analyze                      # find solutions under . (unpacked XML folders and exported .zip files)
powerops analyze path/to/solution.zip path/to/unpacked --format json --out report.json
powerops rules                        # list every rule with its severity, title and fix (add --json for machine output)
powerops diff old.zip new.zip         # compare two versions component by component (zip or unpacked folder; --format text|json|markdown, --out <file>)
powerops settings generate path/to/solution --out deploymentsettings.json   # pac deployment settings with empty values to fill in
powerops settings validate path/to/solution --settings deploymentsettings.json   # missing or extra entries, empty connection ids and values
powerops settings apply solution.zip --settings deploymentsettings.json   # dry run: the target, the pac command, each cloud flow new or existing
powerops settings apply solution.zip --settings deploymentsettings.json --apply --target "Contoso Dev"   # import through pac, then check the flows are on
powerops drift --solution MySolution --env <dev> --env <test>   # compare one solution across environments or saved snapshot files (--save <dir>, --format text|json|markdown, --out <file>)
powerops drift --solution MySolution --env <dev> --env <test> --save snapshots/   # the same, and write snapshots/MySolution.<environment name>.json for each environment
powerops drift --solution MySolution --env "snapshots/MySolution.Contoso Dev.json" --env "snapshots/MySolution.Contoso Test.json"   # compare saved snapshots, offline
powerops baseline                     # accept today's findings: writes powerops-baseline.json
powerops analyze --baseline powerops-baseline.json   # report only findings that are not in the baseline
```

`analyze` is offline and read-only. Options: `--format text|json|sarif|markdown` (default `text`), `--out <file>` (write the report to a file instead of stdout), `--fail-on error|warning|none` (default `error`), `--baseline <file>` (drop findings listed in a baseline file before reporting and before the exit code; the summary counts them as `baselined`). It reads unpacked XML folders (`Other/Solution.xml`), exported zips and the YAML layout that Dataverse Git integration commits: point it at the folder that holds `solutions/`, and each `solutions/<name>/solution.yml` is reported as its own solution, with its components picked by its `solutioncomponents.yml`. Canvas app `.pa.yaml` formulas are read only in the YAML layout (PO014). Every finding carries a one-line `fix` and a `helpUri` to its docs page in JSON and Markdown output, and SARIF rules carry the fix as `help`. Every JSON output carries a `schemaVersion`; the schemas are in [docs/schemas/](docs/schemas/).

`baseline [path...] [--out <file>]` runs the same analysis and writes the findings it found to a JSON file (default `powerops-baseline.json`). A finding matches the baseline on solution unique name, rule id, component and message, not on file, so a baseline made from an exported zip also works for the unpacked folder. An unreadable or invalid baseline file is a usage error (exit `2`).

Exit codes: `0` no findings at or above `--fail-on`, `1` findings at or above it, `2` usage error (bad option, missing path, no solutions found), `3` unreadable solution or internal error (set `POWEROPS_DEBUG=1` for a stack trace).

`diff <old> <new>` is offline and read-only. It loads both solutions and compares the solution unique name, version, managed flag and publisher, root components, missing dependencies, connection references, environment variables, flows and canvas apps. File paths, ordering, flow JSON key order and whitespace, and XML whitespace in canvas app metadata are not differences, so a folder and its exported zip compare equal. Classic workflows and business rules are compared by name, category and state only (their XAML is not read), and components it does not read yet (tables, views, forms and so on) are not compared. Environment variable current values are reported as changed, added or removed but never printed. A canvas app whose `AppVersion` timestamp changed is reported as republished, since an edit changes the app package and the timestamp. Some changes carry a risk label: `removed-component` (a managed upgrade deletes components that are no longer in the new version, [per Microsoft Learn](https://learn.microsoft.com/en-us/power-platform/alm/solution-concepts-alm)), `flow-state` (a cloud flow was turned on or off), `connection-reference` and `environment-variable` (added or removed). Options: `--format text|json|markdown` (default `text`) and `--out <file>`. Markdown is cut at a whole row below 60,000 characters, with a note on how many changes were left out, so it fits in a pull request comment. JSON follows [docs/schemas/diff.schema.json](docs/schemas/diff.schema.json). Exit codes: `0` no differences, `1` differences found, `2` usage error (bad option, missing path), `3` unreadable solution or internal error (set `POWEROPS_DEBUG=1` for a stack trace).
`settings generate <solution> [--out <file>]` prints a `deploymentsettings.json` with the solution's environment variables and connection references, sorted by name, with empty `Value` and `ConnectionId` to fill in (stdout, or a file with `--out`). `settings validate <solution> --settings <file> [--format text|json]` reports entries missing from the file, entries not in the solution, names listed more than once, connection references without a `ConnectionId`, environment variables with no value in the file and no default or current value in the solution, and any `"Value": ""`, which `pac solution import` rejects. To keep a variable's default or current value, leave its entry out of the file (observed with `pac` 2.13.1); `settings generate` writes every variable with an empty value, so fill each one in or remove it. Fields the solution does not need (the extras `pac` writes) are ignored. Exit codes: `0` clean, `1` problems, `2` usage error, `3` unreadable solution or invalid settings file.

`settings apply <zip> --settings <file> [--apply --target <name>] [--format text|json]` deploys an exported solution zip with a deployment settings file through `pac`. Without `--apply` it is a dry run that writes nothing: it runs `settings validate`, reads pac's current environment with `pac org who`, reads each cloud flow's state there with `pac power-automate list-cloud-flows`, and prints the target environment name, whether the zip is managed, the `pac solution import` arguments (the environment id shows as `<environment id>`) and each flow as `new` (with the state the import should give it) or `exists` (with its state, which the import keeps). `--apply --target <name>` imports into the pac profile's current environment, and only when `<name>` equals that environment's friendly name; every later `pac` call is pinned to that environment's id, which is never printed. Friendly names are not unique, so check the profile with `pac auth list` before saying yes to a target. The import is `pac solution import --environment <id> --path <zip> --settings-file <file>` with pac's defaults: unmanaged customisations are not overwritten, changes are not published and there is no upgrade, so components removed from the solution stay in the target. Afterwards each flow is reported `on`, `off`, `suspended` (for example, a data policy blocks it) or `missing`. `pac` cannot turn a flow on, so powerops does not either: a new flow comes back in its export state (on only if it was on at export and its connection references get connections), an existing flow keeps its state, and a flow stays off when the importing user does not own or have access to every connection it uses (`ConnectionAuthorizationFailed`). A solution with a flow that was off at export therefore always exits `1`; that is by design, because the command checks that the flows are on. A user who cannot list flows sees every flow as `new` before and `missing` after. The import turns the solution's flows off and on again while it runs, so a running flow is briefly interrupted. It can take up to an hour (powerops waits 60 minutes): run it in the background or with a long command timeout; stopping powerops does not stop an import already running in the environment. The flow check makes two `pac` reads per flow, and a read slower than 30 seconds ends the run with exit `3`. There is no rollback: on a failure, fix the cause and run again. When the import fails, the report holds pac's exit code and pac's own output goes to stderr. In text mode with `--apply`, the first line is the target environment, printed before the import starts; with `--format json` that line goes to stderr and the report ([docs/schemas/settings-apply.schema.json](docs/schemas/settings-apply.schema.json)) is written at the end. Exit codes: `0` dry run done or every flow on, `1` validate problems or a flow not on, `2` usage error (bad option, path not found, a folder instead of a zip, `--apply` without `--target` or the reverse, or a `--target` that does not match), `3` `pac` not found, not signed in, a failed or timed-out import, `pac` output it cannot read, or an unreadable solution or settings file.

Agents: run the dry run first and show the user the target line; run `--apply --target <name>` only after the user says yes to that environment, and never against an environment the user did not name.

`drift --solution <name> --env <env or file> --env <env or file> [--env ...] [--save <dir>] [--format text|json|markdown] [--out <file>]` compares one solution across two or more environments and reports where they differ. It compares the version, the components, unmanaged layers on a managed solution's components, environment variable values, connection reference connectors and bindings, and cloud flow states. Each environment is compared with the first. Output names an environment by its position (`1`, `2`, ...) and its friendly name, because friendly names are not unique. An `--env` is an environment (an id or URL, as `pac` takes it) or a snapshot file (a path that ends in `.json`, or an existing file), so live and saved environments can be mixed. `--save <dir>` writes `<solution>.<environment name>.json` for each live environment, after every read has succeeded. It creates the folder, overwrites a file of the same name, and replaces every character outside letters, digits, `-`, `_`, `.` and space in the name with `-`. Snapshot files you pass in are not written again. A later run can compare the saved files offline.

Each difference is an item with a kind. These kinds are drift and make the exit code `1`: `solution-missing` (installed in one environment of a pair and not the other; nothing else is compared for that pair), `version`, `component-missing`, `component-extra`, `unmanaged-layer`, `variable-missing`, `variable-value`, `reference-missing`, `reference-connector`, `reference-unbound`, `flow-missing` and `flow-state`. Two kinds are information only and are marked `(info)`: `managed` (the managed flags differ; Dev unmanaged and Test managed is the normal route) and `layers-not-read`. A flow that is off in every environment is not drift. A flow, variable or reference that is missing is reported once under its own kind, not again as a component. Tables (type 1), canvas apps (300), environment variables (380 and 381) and connection references get new ids when they are imported, so they are compared by name through their own kinds or not at all, and only other components are compared by id. `unmanaged-layer` and `reference-unbound` are checked in every environment, the first one included. Connection ids are not compared, because a connection belongs to one environment. Default values of environment variables are not compared either. A variable whose value differs by design makes every run exit `1`, and there is no option to accept it; with JSON output, filter `items` by `kind`.

Values are never printed. A snapshot file holds a SHA-256 of each variable's current value, never the value. A hash hides a value from a reader, but not from someone who guesses a short or known value, so keep reports and snapshot files in private folders and private CI artifacts. Friendly names are printed and saved, which is another reason. `layers not read` appears for a managed solution whose layer reads were skipped (see the cost below).

`drift` only reads. For each live environment it runs `pac org who --environment <env>` for the friendly name and the environment URL, asks `az account get-access-token` for a token for that URL, then sends Web API GET requests with FetchXML. It does not parse `pac env fetch`, because Microsoft Learn documents no output format for it. The token is held in memory and sent only in the `Authorization` header. It is never printed or saved, and neither is the URL. powerops sends the token only to documented Dataverse hosts (`*.dynamics.com`, `*.crm.microsoftdynamics.us`, `*.crm.appsplatform.us`, `*.crm.dynamics.cn`); an environment URL on any other host exits `2` before a token is requested. Prerequisites: `pac` signed in (`pac auth create`), and the Azure CLI on `PATH` and signed in with `az login`. If `az` is installed elsewhere, add its folder to `PATH`, for example `fish_add_path <folder>` or your shell's equivalent. The two sign-ins can be different accounts: `pac org who` runs as the pac profile, and the rows are read as the `az` account. An environment in a different tenant from the `az` sign-in is not supported. The reading account needs to be able to read the solution, solution component, workflow, environment variable and connection reference tables; the minimum security role was not tested. In CI, `drift` needs an `az` sign-in beside `pac`'s.

Each live environment costs one `pac` call, one `az` call and five Web API reads, each with a 30 second limit. For a managed solution it also makes one layer read per component that is a table, flow, canvas app, environment variable definition or connection reference (the layer table takes one component per query; about 0.4 s each in the capture), to find unmanaged layers; other component types are not checked for layers. More than 50 such components skip the layer reads, and the report says `layers-not-read`. A read returns one page of at most 5,000 rows; more than that exits `3`. When a call fails, the message names the `--env` by position and the read. Text forwarded from `pac`, `az` or the Web API goes to stderr only, never to the report or a file, and may repeat an `--env` value or the environment URL. Tested with `pac` 2.13.1 and Azure CLI 2.90.0 on Linux, live on 2026-10-22 against an unmanaged developer environment and a managed install in a trial environment, with an account that holds the System Administrator role there; the minimum role for the reads was not tested, and Windows is not tested. JSON follows [docs/schemas/drift.schema.json](docs/schemas/drift.schema.json), and snapshot files follow [docs/schemas/snapshot.schema.json](docs/schemas/snapshot.schema.json). Exit codes: `0` no drift, `1` drift, `2` usage error (no `--solution` or a name that is not a solution unique name, fewer than two `--env`, an `--env` ending in `.json` with no file, `--save` naming a file or used with no live environment, a snapshot for another solution, two environments that would be saved to the same file name, a solution installed in none of the environments, or an environment URL on a host that is not a documented Dataverse host), `3` a `pac` or `az` call or a Web API read failed, timed out or returned output it cannot read, `pac` or `az` not found, or an unreadable snapshot file.

Agents: run `drift` only against environments the user named in the session.

| Rule | Severity | Finds |
|---|---|---|
| PO001 | error | Flow uses a connection reference that is not defined in the solution or its dependencies |
| PO002 | error | Required environment variable has neither a default nor a current value |
| PO003 | error | Missing dependency that no solution owns |
| PO004 | error | Flow calls a child flow that is not in the solution or its dependencies |
| PO005 | warning | Connection reference not used by any flow or canvas app |
| PO006 | warning | Flow contains a hard-coded environment or SharePoint URL |
| PO007 | warning | Environment variable default looks environment-specific (URL, GUID, email) |
| PO008 | warning | Environment variable current value is shipped in the solution |
| PO009 | warning | Component name uses a prefix other than the publisher's |
| PO010 | warning | Default or generated publisher |
| PO011 | warning | Cloud flow is turned off |
| PO012 | warning | Dataverse row-updated trigger has no filter columns |
| PO013 | error | Root component in the YAML layout has no source folder |
| PO014 | warning | Canvas app formula contains a hard-coded environment or SharePoint URL |

Each rule has a page with what it finds, how to fix it and how to suppress it: [docs/rules/](docs/rules/).

## Planning

PowerOps ships three agent skills that turn an idea into a reviewed, signed-off plan before any code or solution change is made:

- `powerops-plan` asks short question rounds (at most three questions, recommended answer first), logs every answer in `decisions.md`, drafts `plan.md`, and signs it off with you. Asked to build without a plan, it drafts one and asks only the route.
- `powerops-review-plan` reviews an approved plan in a fresh context against a Power Platform checklist and writes `review.md`. It never asks you questions.
- `powerops-implement` builds only from a signed-off plan whose body still matches its sign-off.

Run `powerops skills install` in your repo to add them. Supported: Claude Code and Codex. Gemini CLI and GitHub Copilot are provisional and untested. The skills are also in [skills/](skills/) for `npx skills add` (that CLI has telemetry on by default). Design: [docs/PLANNING-SKILLS.md](docs/PLANNING-SKILLS.md).

Plans live in `docs/plans/<name>/plan.md`, with a header between `---` lines (`status`, `review`, `signed-off`, `signed-off-hash`, `environment`).

```sh
powerops plan status                  # check every docs/plans/*/plan.md under . (offline, read-only; add --json for machine output)
powerops plan sign-off docs/plans/site-visits   # write status, date and body hash into the header
powerops plan facts                   # environment and solution names as JSON; read-only, needs pac signed in, prints names only
powerops skills install               # copy the planning skills into .claude/skills/ and/or .agents/skills/ (--agent claude|codex|gemini|copilot, --force to overwrite)
```

`plan status` checks the header, that a signed-off plan still matches its recorded hash, that the review is done (with `review.md`) or skipped, and that no file in the plan folder holds a URL, GUID or email (add `<!-- plan-status: allow -->` to a line to allow it). `plan sign-off` refuses unless `status` is `approved`, the review is done or skipped, and the scan is clean; it edits only the header. Exit codes: `0` clean, `1` violations or a refused sign-off, `2` usage error or malformed header, `3` internal error.

## Build from source

Requires the .NET 10 SDK.

```sh
dotnet build
dotnet test
```

## Documentation

- [Foundation plan and roadmap](docs/FOUNDATION.md)
- [Contributing](CONTRIBUTING.md)
- [License (Apache 2.0)](LICENSE)
