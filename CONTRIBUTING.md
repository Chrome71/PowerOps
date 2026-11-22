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

Merging the release pull request cuts the release: release-please tags it `PowerOps-v<version>` and creates the GitHub Release. Three jobs of `.github/workflows/release.yml` then run, each only when a release was created:

- `assets` (read-only token) checks out the tag, packs the tool in locked mode without the commit in its version, and runs `.github/scripts/release-assets.sh`, which publishes the self-contained single-file binaries `powerops-linux-x64` and `powerops-win-x64.exe` (not trimmed), zips `powerops-sample.zip`, and writes `SHA256SUMS` over those and the package. Each runtime is restored first and its restore held to the committed lock files before anything builds; the .NET runtime and apphost packs are in no lock file, so the script prints the runtime version each binary bundles. `.github/scripts/release-check.sh` then stops the job unless the folder holds exactly those five files, `SHA256SUMS` checks, and the packed tool and the Linux binary both print that version. The job runs the Linux binary on the sample and keeps the five files as the run's `release-assets` artifact for 30 days.
- `windows` (no token) downloads them and runs the Windows binary: its `--version` must print the release version, and it analyzes the sample.
- `publish`, the only job that can write, builds nothing: it downloads the files, checks `SHA256SUMS`, records one build provenance attestation for the four files it lists, and attaches all five to the release, never replacing a file already there.

If a job fails before anything is attached, fix the cause and use "Re-run failed jobs" on that run, within 30 days: in practice GitHub gives the re-run the outputs of the jobs that passed, so the failed jobs run again for the same release, though GitHub's page on re-runs doesn't say so. If the upload stopped part-way, a re-run fails on the first file already attached; finish it from the run's own files, which are the attested ones:

```sh
gh run download <run-id> -n release-assets -D artifacts
(cd artifacts && sha256sum -c SHA256SUMS)
gh release upload PowerOps-v<version> <each missing file, from artifacts/>
```

If the re-run skips the jobs, or the artifact is gone, and nothing is attached yet, build and attach the five files by hand from a clone, on Linux with `jq` and `zip`, as below. Files attached this way have no attestation, and their Windows binary was not run, so run it on Windows before you announce the release. If some files are already attached and the artifact is gone, stop: a new build has other hashes, so it can't finish a half attach.

```sh
git checkout PowerOps-v<version>
dotnet pack src/PowerOps.Cli -c Release -o artifacts -p:RestoreLockedMode=true -p:IncludeSourceRevisionInInformationalVersion=false
bash .github/scripts/release-assets.sh artifacts -p:IncludeSourceRevisionInInformationalVersion=false
bash .github/scripts/release-check.sh <version> artifacts
gh release upload PowerOps-v<version> artifacts/PowerOps.<version>.nupkg artifacts/powerops-linux-x64 artifacts/powerops-win-x64.exe artifacts/powerops-sample.zip artifacts/SHA256SUMS
```

A cause in the files at the tag can't be fixed this way; it needs a fix on `main` and a new release.

To check a release, download its files from its page, then, for each file you downloaded:

```sh
sha256sum -c --ignore-missing SHA256SUMS
gh attestation verify <file> --repo Chrome71/PowerOps
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
