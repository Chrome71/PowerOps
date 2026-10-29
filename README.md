# PowerOps

A free, open-source, cross-platform toolkit that brings software-engineering discipline (analysis, diffing, testing, repeatable deployment) to Microsoft Power Platform. It works alongside `pac`; it does not replace it.

It finds what would break a solution's import or a run, compares two versions of a solution, runs flow tests offline with mocked connector actions, and says what importing solution zips into an environment would do before anything is imported. Agents in Claude Code and Codex can run it through its planning skills and its MCP server.

**Status:** early development, not yet released. The [commands](#commands) below are built.

## Install

PowerOps is not on NuGet yet. Install it from a clone (needs the .NET 10 SDK):

```sh
dotnet pack src/PowerOps.Cli -c Release -o artifacts && dotnet tool install --global --source artifacts PowerOps
powerops --version
```

`--source` makes the install use only `artifacts`. Leave it out and the install looks the name `PowerOps` up on nuget.org, where it is not registered, so it could install whatever package someone registers under it.

## Quick start

Run `analyze` where your solutions are. It finds the unpacked XML folders and exported `.zip` files under the current folder and reports what would break the import or a run:

```sh
powerops analyze
```

In any CI system that has the .NET 10 SDK, build PowerOps from a clone, then one command writes a SARIF report and fails the build on errors:

```sh
git clone https://github.com/Chrome71/PowerOps.git
git -C PowerOps checkout b4abce4cb00421c7017076de7b53e53a7ce063c7 # main on 2026-10-28; pin the newest commit of main
dotnet pack PowerOps/src/PowerOps.Cli -c Release -o PowerOps/artifacts
dotnet tool install --global --source PowerOps/artifacts PowerOps
powerops analyze --format sarif --out powerops.sarif
```

On GitHub Actions, use the action: it writes a job summary and annotations, and uploads the findings to code scanning. The workflow, the merge gate for flow tests and the pull request comment are in [GitHub Action](docs/GITHUB-ACTION.md).

## Commands

| Command | What it does |
|---|---|
| [`analyze`](docs/COMMANDS.md#analyze-and-baseline) | Checks solutions (unpacked folders, exported zips, the YAML layout) against the [rules](docs/rules/README.md); offline and read-only |
| [`baseline`](docs/COMMANDS.md#analyze-and-baseline) | Accepts today's findings, so `analyze --baseline` reports only findings that are not in the baseline |
| [`rules`](docs/COMMANDS.md#rules) | Lists every rule with its severity, title and fix |
| [`diff`](docs/COMMANDS.md#diff) | Compares two versions of a solution component by component; offline and read-only |
| [`test`](docs/FLOW-TESTS.md) | Runs flow tests against the cloud flows, with mocked connector actions; offline and read-only |
| [`settings generate`](docs/COMMANDS.md#settings-generate-and-settings-validate) | Writes `pac` deployment settings with empty values to fill in |
| [`settings validate`](docs/COMMANDS.md#settings-generate-and-settings-validate) | Reports missing or extra entries, empty connection ids and values in a settings file |
| [`settings apply`](docs/COMMANDS.md#settings-apply) | Imports a solution zip with its settings file through `pac`, then checks the flows are on; a dry run without `--apply` |
| [`drift`](docs/COMMANDS.md#drift) | Compares one solution across environments or saved snapshot files; only reads |
| [`deploy plan`](docs/COMMANDS.md#deploy-plan) | Says what importing the solution zips in a folder would do to the target; reads the target only |
| [`deploy apply`](docs/COMMANDS.md#deploy-apply) | Plans again against the target, refuses if that differs from the reviewed plan, then imports through `pac`; a dry run without `--apply` |
| [`plan status`](docs/COMMANDS.md#plan-and-skills) | Checks every plan in `docs/plans/`; offline and read-only |
| [`plan sign-off`](docs/COMMANDS.md#plan-and-skills) | Writes the status, date and body hash into a plan's header |
| [`plan facts`](docs/COMMANDS.md#plan-and-skills) | Prints environment and solution names as JSON; needs `pac` signed in |
| [`skills install`](docs/COMMANDS.md#plan-and-skills) | Copies the planning skills into the repo |
| [`mcp`](docs/COMMANDS.md#mcp) | Runs an MCP server over stdio whose tools run these commands |

`settings apply`, `drift`, `deploy plan`, `deploy apply` and `plan facts` read an environment, so `pac` must be signed in, and `az` too for `drift` and `deploy` (`drift` can also compare saved snapshot files offline). The others work offline on files. Only `settings apply --apply` and `deploy apply --apply` write to an environment.

Every option, output format and exit code is in the [command reference](docs/COMMANDS.md).

## Use from an agent

PowerOps ships three agent skills that turn an idea into a reviewed, signed-off plan before any code or solution change is made, and an MCP server whose tools run the commands. Add the skills to a repo with `powerops skills install`, or install them as a plugin for Claude Code or Codex: [Use from an agent](docs/USE-WITH-AGENTS.md). With Microsoft's power-platform-skills or Dataverse-skills, follow the [hand-off](docs/HANDOFF.md).

## Build from source

Requires the .NET 10 SDK.

```sh
dotnet build
dotnet test
```

## Documentation

- [Command reference](docs/COMMANDS.md)
- [GitHub Action: merge gate and pull request comment](docs/GITHUB-ACTION.md)
- [Flow tests](docs/FLOW-TESTS.md)
- [Rules](docs/rules/README.md)
- [JSON schemas](docs/schemas/README.md)
- [Use from an agent: planning skills, plugin and MCP server](docs/USE-WITH-AGENTS.md)
- [Hand-off to Microsoft's skills](docs/HANDOFF.md)
- [Example workflows: Dev to Test promotion, an environment per pull request](docs/examples/README.md)
- [Planning skills design](docs/PLANNING-SKILLS.md)
- [Foundation plan and roadmap](docs/FOUNDATION.md)
- [Contributing](CONTRIBUTING.md)
- [License (Apache 2.0)](LICENSE)
