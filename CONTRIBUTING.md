# Contributing to PowerOps

Thanks for helping out. Every test is offline: none contacts a Power Platform tenant, and none needs `pac` or `az`, so you can build, test and contribute with just the .NET SDK. The [roadmap](docs/FOUNDATION.md) lists the commands that do read a tenant when you run them yourself.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (see `global.json`)

## Build, test, format

```sh
dotnet build
dotnet test
dotnet format            # fix formatting
dotnet format --verify-no-changes   # what CI runs
```

Some tests skip themselves instead of failing:

- `PackedToolTests` packs and installs the real tool, which is slow. Set `POWEROPS_SKIP_E2E=1` to skip it.
- The tests of the scripts under `action/` and `docs/examples/` need `bash`, and some need `jq` or `git` too. They skip on Windows and when a program is not on `PATH`. CI sets `POWEROPS_REQUIRE_TOOLS=1` on Linux, so a missing program fails there instead of skipping; you can set it too to see every script test run.
- A few tests skip where the file system has no symbolic links or no line breaks in file names, or where a culture is not installed, and a few others skip on Windows or where a file lock does not apply.

## Lock files

PowerOps.Cli and PowerOps.Core, the projects the Action builds, each have a `packages.lock.json`; the test projects have none. After you add or change a `PackageReference`, run `dotnet restore --force-evaluate` and commit the lock files. CI and the Action restore in locked mode, so a stale lock file fails with NU1004. A Dependabot NuGet PR needs `dotnet restore --force-evaluate` run on its branch and the lock files committed before it can merge, because Dependabot may leave a lock file stale (for example PowerOps.Cli's when the bump is in PowerOps.Core) ([dependabot-core#13950](https://github.com/dependabot/dependabot-core/issues/13950)).

CI also fails on a package with a known vulnerability, direct or transitive; a local build only warns (NU1901 to NU1904 stay warnings, see `Directory.Build.props`).

- The step cannot tell a clean result from a source with no vulnerability data (nuget.org has it, and a network failure fails the step).
- There is no allow-list. When an advisory is on a transitive package and no update of the package that brings it in fixes it, pin the transitive package by hand with a direct `PackageReference` to a fixed version.

## Commits

Use [Conventional Commits](https://www.conventionalcommits.org/): `feat`, `fix`, `docs`, `chore`, `refactor`, `test`, `ci`. Mark breaking changes with `feat!` (or `fix!`, etc.). Releases and the changelog are generated from these by release-please.

## Releases

Releases are GitHub Releases only; PowerOps is not published to NuGet. release-please keeps a release pull request open on `main` and stamps the version into `Directory.Build.props`, `.release-please-manifest.json`, both `plugin.json` files and the changelog. Never edit those versions by hand.

Merging the release pull request cuts the release: release-please tags it `PowerOps-v<version>` and creates the GitHub Release. The `publish` job of `.github/workflows/release.yml` then packs the tool at the tag in locked mode, without the commit in its version, and `.github/scripts/release-check.sh` stops the job unless the packed tool's `powerops --version` prints that version; it writes `SHA256SUMS`. The job records a build provenance attestation for the package and attaches `PowerOps.<version>.nupkg` and `SHA256SUMS` to the release, never replacing a file already there. If the job fails before it attaches anything, fix the cause and use "Re-run failed jobs" on that run, within 30 days: in practice GitHub gives the re-run the outputs of the jobs that passed, so `publish` runs again for the same release, though GitHub's page on re-runs doesn't say so. If the re-run skips `publish`, attach both files by hand from a clone, as below; a package attached this way has no attestation. Nothing here replaces a file, and a new pack has another hash, so a half attach is finished differently: with the package attached and `SHA256SUMS` missing, download the package, run `sha256sum PowerOps.<version>.nupkg > SHA256SUMS` and upload that file alone, which keeps the package's attestation; with only `SHA256SUMS` attached, remove it on the release page, then re-run or attach both by hand.

```sh
git checkout PowerOps-v<version>
dotnet pack src/PowerOps.Cli -c Release -o artifacts -p:RestoreLockedMode=true -p:IncludeSourceRevisionInInformationalVersion=false
bash .github/scripts/release-check.sh <version> artifacts
gh release upload PowerOps-v<version> artifacts/PowerOps.<version>.nupkg artifacts/SHA256SUMS
```

A cause in the files at the tag can't be fixed this way; it needs a fix on `main` and a new release.

To check a release, download both files from its page, then:

```sh
sha256sum -c SHA256SUMS
gh attestation verify PowerOps.<version>.nupkg --repo Chrome71/PowerOps
```

The GitHub Action runs the release that `action.yml` pins: `PO_RELEASE` (the version) and `PO_RELEASE_SHA256` (the hash on the package's line of `SHA256SUMS`: its first 64 characters, not the whole line), both quoted, in its `Install PowerOps` step. With both empty, as before the first release, it builds from source. After a release, check it as above, then open one pull request that sets both values; CI's `action` job then runs the downloaded release. The `Pin drift` workflow warns while the pin is not the latest release.

- Don't move the pin while a promotion's plan waits for its apply: a plan file applies only with the build that made it, so the apply would be refused until the plan runs again.
- The action's scripts in `action/`, and the `powerops` calls in `action.yml`'s own steps, run against the pinned release, not the code beside them. A change that needs a newer command or flag waits for the release that has it, and the pin that names that release. `Pin drift` warns when `action/` changed after the pinned release; it does not watch `action.yml`, which the pin's own pull request edits.

## Pull requests

- The PR title is a Conventional Commit.
- New logic comes with tests.
- `dotnet format --verify-no-changes` passes.
- Keep the change focused; open an issue first for anything large.

By participating you agree to the [Code of Conduct](CODE_OF_CONDUCT.md).
