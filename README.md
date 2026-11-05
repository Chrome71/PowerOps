# PowerOps

A free, open-source, cross-platform toolkit that brings software-engineering discipline (analysis, diffing, testing, repeatable deployment) to Microsoft Power Platform. It works alongside `pac`; it does not replace it.

It finds what would break a solution's import or a run, compares two versions of a solution, runs flow tests offline with mocked connector actions, and says what importing solution zips into an environment would do before anything is imported. Agents in Claude Code and Codex can run it through its planning skills and its MCP server.

**Status:** early development, not yet released. The [commands](#commands) below are built.

## Install

### With your agent

Paste this into Claude Code, Codex or another coding agent. It installs the CLI and the skills, checks that they run, and tells you what is left for you.

```text
Install PowerOps (https://github.com/Chrome71/PowerOps), a CLI and agent skills for Microsoft
Power Platform. If a step fails, read the Install section of its README.
1. Check that `dotnet --list-sdks` shows a 10.0 SDK (10.0.100 or later). If not, stop and tell me.
2. Clone the repo into a new temporary folder <clone> and install the CLI from that clone only:
   `dotnet pack <clone>/src/PowerOps.Cli -c Release -o <clone>/artifacts`, then
   `dotnet tool install --global --source <clone>/artifacts PowerOps`.
   If `powerops` is already installed, ask me before you uninstall it.
3. Add the skills for the agent you are running in. Claude Code:
   `claude plugin marketplace add Chrome71/PowerOps`, then `claude plugin install powerops@powerops`.
   Codex: `codex plugin marketplace add Chrome71/PowerOps`, then `codex plugin add powerops@powerops`.
   Any other agent: `powerops skills install` in my repo. Use the plugin or the repo copy, not both.
4. Run `powerops --version` and `powerops rules`; both must exit 0.
5. Tell me whether `pac` and `az` are on PATH: only the commands that read an environment need them.
Never ask me for, or handle, passwords, keys or tokens: I sign in to pac and az myself. Write
nothing to a Power Platform environment. Ask me before changing anything outside this install,
such as my PATH or shell profile. Last, report what you installed and what is left for me to do,
such as starting a new agent session so the plugin loads.
```

### By hand

PowerOps is not on NuGet: it ships as GitHub Releases. Until the first release is published, install it from a clone (needs the .NET 10 SDK):

```sh
dotnet pack src/PowerOps.Cli -c Release -o artifacts &&
  dotnet tool install --global --source artifacts PowerOps
powerops --version
```

`--source` makes the install use only `artifacts`. Leave it out and the install looks the name `PowerOps` up on nuget.org, where it is not registered, so it could install whatever package someone registers under it.

PowerShell 7 runs that block as written. Windows PowerShell 5.1 has no `&&`, so the block doesn't parse there: run its two commands one at a time, the second only if the first succeeded.

From the first release on, each [release](https://github.com/Chrome71/PowerOps/releases) carries the tool package `PowerOps.<version>.nupkg` and `SHA256SUMS`. Download both into an empty folder, check the package, and install it from that folder only:

```sh
sha256sum -c SHA256SUMS
dotnet tool install --global --source . PowerOps --version <version>
```

On Windows without `sha256sum`, compare `(Get-FileHash PowerOps.<version>.nupkg).Hash` with the package's line in `SHA256SUMS` (it prints upper case). `gh attestation verify PowerOps.<version>.nupkg --repo Chrome71/PowerOps` checks that the release workflow built it. A release prints its plain version for `powerops --version`; a build from a clone prints the version and its commit, `<version>+<commit>`, so a plan or report names the build that made it.

Update a build from a clone: uninstall, then pack and install again. Its package keeps the version of the last release (`0.0.0` until the first), whatever the commit, so installing or updating over the installed tool keeps the old build. Run this as its own command, not chained to the install: uninstalling fails when the tool is not installed.

On Windows the uninstall also fails, with `Access to the path ... is denied`, while a `powerops mcp` server runs from the tool's folder, and each Claude Code or Codex session that uses PowerOps starts one. Close those sessions first. Codex's daemon can keep a server running after its session ends: `codex app-server daemon stop` ends it, and `Get-Process powerops` shows whether any is left. The plugin updates separately: see [Use from an agent](docs/USE-WITH-AGENTS.md#planning-skills).

```sh
dotnet tool uninstall --global PowerOps
dotnet pack src/PowerOps.Cli -c Release -o artifacts &&
  dotnet tool install --global --source artifacts PowerOps
```

PowerOps supports Windows and Linux. CI builds and tests both on every pull request, and the hand runbook has been run on Windows ([what is not verified](docs/FOUNDATION.md#8-research-topics-still-open)). macOS is not supported or tested: it may work, but it is out of scope.

## Quick start

Run `analyze` where your solutions are. It finds the unpacked XML folders and exported `.zip` files under the current folder and reports what would break the import or a run:

```sh
powerops analyze
```

In any CI system that has the .NET 10 SDK, build PowerOps from a clone, then one command writes a SARIF report and fails the build on errors:

```sh
po="$(mktemp -d)"
git clone https://github.com/Chrome71/PowerOps.git "$po/PowerOps"
git -C "$po/PowerOps" checkout 155b9445663f147d036bd577db88a30db6070024 # main on 2026-10-31; pin the newest commit of main
dotnet pack "$po/PowerOps/src/PowerOps.Cli" -c Release -o "$po/PowerOps/artifacts"
dotnet tool install --global --source "$po/PowerOps/artifacts" PowerOps
powerops analyze solutions --format sarif --out powerops.sarif # solutions: the folder that holds your solutions
```

On GitHub Actions, use the action: it writes a job summary and annotations, and uploads the findings to code scanning. The workflow, the merge gate for flow tests and the pull request comment are in [GitHub Action](docs/GITHUB-ACTION.md).

## Commands

| Command | What it does |
|---|---|
| [`analyze`](docs/COMMANDS.md#analyze-and-baseline) | Finds what would break a solution's import or a run ([rules](docs/rules/README.md)) |
| [`baseline`](docs/COMMANDS.md#analyze-and-baseline) | Accepts today's findings, so `analyze` reports only new ones |
| [`rules`](docs/COMMANDS.md#rules) | Lists every rule with its severity, title and fix |
| [`diff`](docs/COMMANDS.md#diff) | Compares two versions of a solution, component by component |
| [`test`](docs/FLOW-TESTS.md) | Runs flow tests with mocked connector actions |
| [`settings generate`](docs/COMMANDS.md#settings-generate-and-settings-validate) | Writes `pac` deployment settings with empty values to fill in |
| [`settings validate`](docs/COMMANDS.md#settings-generate-and-settings-validate) | Finds missing, extra and empty entries in a settings file |
| [`settings apply`](docs/COMMANDS.md#settings-apply) | Imports a zip with a settings file and checks the flows are on |
| [`drift`](docs/COMMANDS.md#drift) | Compares one solution across environments or saved snapshots |
| [`deploy plan`](docs/COMMANDS.md#deploy-plan) | Says what importing solution zips would do to an environment |
| [`deploy apply`](docs/COMMANDS.md#deploy-apply) | Imports a reviewed plan; refuses if the target or files changed |
| [`deploy gate`](docs/COMMANDS.md#deploy-gate) | A hook check before Microsoft's import tools: allows only the plan's first import |
| [`plan status`](docs/COMMANDS.md#plan-and-skills) | Checks every plan under `docs/plans/` |
| [`plan sign-off`](docs/COMMANDS.md#plan-and-skills) | Writes the status, date and body hash into a plan's header |
| [`plan facts`](docs/COMMANDS.md#plan-and-skills) | Prints environment and solution names as JSON |
| [`skills install`](docs/COMMANDS.md#plan-and-skills) | Copies the agent skills into the repo |
| [`mcp`](docs/COMMANDS.md#mcp) | Runs an MCP server whose tools run these commands |

`settings apply`, `drift`, `deploy plan`, `deploy apply`, `deploy gate` and `plan facts` read an environment, so `pac` must be signed in, and `az` too for `drift` and `deploy` (`drift` can also compare saved snapshot files offline). The others work offline on files. Only `settings apply --apply` and `deploy apply --apply` write to an environment; without `--apply` they are dry runs.

Every option, output format and exit code is in the [command reference](docs/COMMANDS.md).

## Use from an agent

PowerOps ships five agent skills. Three turn an idea into a reviewed, signed-off plan before any code or solution change is made, and two check the build against that plan and read it for security before it is promoted. It also ships an MCP server whose tools run the commands. Add the skills to a repo with `powerops skills install`, or install them as a plugin for Claude Code or Codex: [Use from an agent](docs/USE-WITH-AGENTS.md). With Microsoft's power-platform-skills or Dataverse-skills, follow the [hand-off](docs/HANDOFF.md).

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
- [Recovery after an import that stops](docs/RECOVERY.md)
- [What an agent client must provide](docs/HARNESS.md)
- [Example workflows: Dev to Test promotion, an environment per pull request](docs/examples/README.md)
- [Planning skills design](docs/PLANNING-SKILLS.md)
- [Foundation plan and roadmap](docs/FOUNDATION.md)
- [Contributing](CONTRIBUTING.md)
- [License (Apache 2.0)](LICENSE)
