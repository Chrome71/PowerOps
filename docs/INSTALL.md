# Install PowerOps

The quick paths are in the [README](../README.md#install). This page has the detail. Everything here needs the .NET 10 SDK.

## From a clone

PowerOps is not on NuGet: it ships as GitHub Releases. Until the first release is published, install it from a clone:

```sh
dotnet pack src/PowerOps.Cli -c Release -o artifacts &&
  dotnet tool install --global --source artifacts PowerOps
powerops --version
```

`--source` makes the install use only `artifacts`. Leave it out and the install looks the name `PowerOps` up on nuget.org, where it is not registered, so it could install whatever package someone registers under it.

PowerShell 7 runs that block as written. Windows PowerShell 5.1 has no `&&`, so the block doesn't parse there: run its two commands one at a time, the second only if the first succeeded.

## From a release

From the first release on, each [release](https://github.com/Chrome71/PowerOps/releases) carries the tool package `PowerOps.<version>.nupkg` and `SHA256SUMS`. Download both into an empty folder, check the package, and install it from that folder only:

```sh
sha256sum -c SHA256SUMS
dotnet tool install --global --source . PowerOps --version <version>
```

On Windows without `sha256sum`, compare `(Get-FileHash PowerOps.<version>.nupkg).Hash` with the package's line in `SHA256SUMS` (it prints upper case). `gh attestation verify PowerOps.<version>.nupkg --repo Chrome71/PowerOps` checks that the release workflow built it. A release prints its plain version for `powerops --version`; a build from a clone prints the version and its commit, `<version>+<commit>`, so a plan or report names the build that made it.

## Update

Update a build from a clone: uninstall, then pack and install again. Its package keeps the version of the last release (`0.0.0` until the first), whatever the commit, so installing or updating over the installed tool keeps the old build. Run this as its own command, not chained to the install: uninstalling fails when the tool is not installed.

```sh
dotnet tool uninstall --global PowerOps
dotnet pack src/PowerOps.Cli -c Release -o artifacts &&
  dotnet tool install --global --source artifacts PowerOps
```

On Windows the uninstall also fails, with `Access to the path ... is denied`, while a `powerops mcp` server runs from the tool's folder, and each Claude Code or Codex session that uses PowerOps starts one. Close those sessions first. Codex's daemon can keep a server running after its session ends: `codex app-server daemon stop` ends it, and `Get-Process powerops` shows whether any is left. The plugin updates separately: see [Use from an agent](USE-WITH-AGENTS.md#planning-skills).

After an update, compare the commit that `powerops --version` prints (see above) with `git rev-parse HEAD` in the clone. The version alone proves nothing, because every build between two releases has the same one.

## Platforms

PowerOps supports Windows and Linux. CI builds and tests both on every pull request, and the hand runbook has been run on Windows ([what is not verified](FOUNDATION.md#8-research-topics-still-open)). macOS is not supported or tested: it may work, but it is out of scope.
