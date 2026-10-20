# PowerOps

A free, open-source, cross-platform toolkit that brings software-engineering discipline (analysis, diffing, testing, repeatable deployment) to Microsoft Power Platform. It works alongside `pac`; it does not replace it.

**Status:** early development, Phase 0. The only command so far is `powerops analyze`.

## Install

```sh
dotnet tool install --global PowerOps
powerops --version
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

Pull requests from forks cannot upload SARIF (their token has no `security-events` permission); they still get the job summary and the annotations.

## Usage

```sh
powerops analyze                      # find solutions under . (unpacked XML folders and exported .zip files)
powerops analyze path/to/solution.zip path/to/unpacked --format json --out report.json
powerops rules                        # list every rule with its severity, title and fix (add --json for machine output)
powerops settings generate path/to/solution --out deploymentsettings.json   # pac deployment settings with empty values to fill in
powerops settings validate path/to/solution --settings deploymentsettings.json   # missing or extra entries, empty connection ids and values
powerops baseline                     # accept today's findings: writes powerops-baseline.json
powerops analyze --baseline powerops-baseline.json   # report only findings that are not in the baseline
```

`analyze` is offline and read-only. Options: `--format text|json|sarif|markdown` (default `text`), `--out <file>` (write the report to a file instead of stdout), `--fail-on error|warning|none` (default `error`), `--baseline <file>` (drop findings listed in a baseline file before reporting and before the exit code; the summary counts them as `baselined`). The YAML source layout is not supported yet. Every finding carries a one-line `fix` and a `helpUri` to its docs page in JSON and Markdown output, and SARIF rules carry the fix as `help`. Every JSON output carries a `schemaVersion`; the schemas are in [docs/schemas/](docs/schemas/).

`baseline [path...] [--out <file>]` runs the same analysis and writes the findings it found to a JSON file (default `powerops-baseline.json`). A finding matches the baseline on solution unique name, rule id, component and message, not on file, so a baseline made from an exported zip also works for the unpacked folder. An unreadable or invalid baseline file is a usage error (exit `2`).

Exit codes: `0` no findings at or above `--fail-on`, `1` findings at or above it, `2` usage error (bad option, missing path, no solutions found), `3` unreadable solution or internal error (set `POWEROPS_DEBUG=1` for a stack trace).

`settings generate <solution> [--out <file>]` prints a `deploymentsettings.json` with the solution's environment variables and connection references, sorted by name, with empty `Value` and `ConnectionId` to fill in (stdout, or a file with `--out`). `settings validate <solution> --settings <file> [--format text|json]` reports entries missing from the file, entries not in the solution, connection references without a `ConnectionId`, and environment variables with no value in the file and no default or current value in the solution. Fields the solution does not need (the extras `pac` writes) are ignored. Exit codes: `0` clean, `1` problems, `2` usage error, `3` unreadable solution or invalid settings file.

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
