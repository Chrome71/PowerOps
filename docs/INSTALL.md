# Install PowerOps

The quick paths are in the [README](../README.md#install). This page has the detail. The release binaries need nothing else; the tool package and a clone need the .NET 10 SDK.

## From a release

PowerOps is not on NuGet: it ships as [GitHub Releases](https://github.com/Chrome71/PowerOps/releases). No release is published yet: until the first release, install from a clone (next section). From the first release on, each release carries five files:

- `powerops-linux-x64` and `powerops-win-x64.exe`: the whole CLI in one file each, with the .NET runtime inside, so they need no .NET SDK. They are for x64 Linux (glibc) and x64 Windows.
- `PowerOps.<version>.nupkg`: the same CLI as a .NET tool package, for `dotnet tool install` (needs the .NET 10 SDK).
- `powerops-sample.zip`: a small solution and two flow tests to try PowerOps on; its `README.md` names the result to expect.
- `SHA256SUMS`: the SHA-256 of the other four.

On Linux, download `powerops-linux-x64` and `SHA256SUMS` into an empty folder, then in that folder:

```sh
sha256sum -c --ignore-missing SHA256SUMS
chmod +x powerops-linux-x64
mkdir -p ~/.local/bin && mv powerops-linux-x64 ~/.local/bin/powerops
powerops --version
```

`--ignore-missing` checks the files you downloaded and skips the rest of the list. If `powerops --version` is not found, add `~/.local/bin` to your PATH. Keep the name `powerops`: the plugins and skills call it by that name. Like any .NET program on Linux it needs the ICU library (`libicu`), which most distributions install.

On Windows, download `powerops-win-x64.exe` and `SHA256SUMS`, compare `(Get-FileHash powerops-win-x64.exe).Hash` with the file's line in `SHA256SUMS` (it prints upper case), rename the file `powerops.exe` and move it to a folder on your PATH. It is not code-signed, so SmartScreen may warn the first time it runs.

The tool package instead: download it and `SHA256SUMS` into an empty folder, check it, and install it from that folder only:

```sh
sha256sum -c --ignore-missing SHA256SUMS
dotnet tool install --global --source . PowerOps --version <version>
```

`gh attestation verify <file> --repo Chrome71/PowerOps` checks that the release workflow built a file; it works for each of the four. A release prints its plain version for `powerops --version`; a build from a clone prints the version and its commit, `<version>+<commit>`, so a plan or report names the build that made it.

## From a clone

Until the first release, install from a clone. It needs the .NET 10 SDK:

```sh
dotnet pack src/PowerOps.Cli -c Release -o artifacts &&
  dotnet tool install --global --source artifacts PowerOps
powerops --version
```

`--source` makes the install use only `artifacts`. Leave it out and the install looks the name `PowerOps` up on nuget.org, where it is not registered, so it could install whatever package someone registers under it.

PowerShell 7 runs that block as written. Windows PowerShell 5.1 has no `&&`, so the block doesn't parse there: run its two commands one at a time, the second only if the first succeeded.

## Update

Update a release binary by replacing the file with the new release's, checked the same way. Update a build from a clone: uninstall, then pack and install again. Its package keeps the version of the last release (`0.0.0` until the first), whatever the commit, so installing or updating over the installed tool keeps the old build. Run this as its own command, not chained to the install: uninstalling fails when the tool is not installed.

```sh
dotnet tool uninstall --global PowerOps
dotnet pack src/PowerOps.Cli -c Release -o artifacts &&
  dotnet tool install --global --source artifacts PowerOps
```

On Windows the uninstall also fails, with `Access to the path ... is denied`, while a `powerops mcp` server runs from the tool's folder, and each Claude Code or Codex session that uses PowerOps starts one. Close those sessions first. Codex's daemon can keep a server running after its session ends: `codex app-server daemon stop` ends it, and `Get-Process powerops` shows whether any is left. The plugin updates separately: see [Use from an agent](USE-WITH-AGENTS.md#planning-skills).

After an update, compare the commit that `powerops --version` prints (see above) with `git rev-parse HEAD` in the clone. The version alone proves nothing, because every build between two releases has the same one.

## Platforms

PowerOps supports Windows and Linux. CI builds and tests both on every pull request, and the hand runbook has been run on Windows ([what is not verified](FOUNDATION.md#8-research-topics-still-open)). macOS is not supported or tested: it may work, but it is out of scope.
