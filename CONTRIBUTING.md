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
- The tests of the scripts under `action/` and `docs/examples/` need `bash`, and some need `jq` or `git` too. They skip on Windows and when a program is not on `PATH`. CI sets `POWEROPS_REQUIRE_TOOLS=1` on Linux and macOS, so a missing program fails there instead of skipping; you can set it too to see every script test run.
- A few tests skip where the file system has no symbolic links or no line breaks in file names, or where a culture is not installed, and a few others skip on Windows or where a file lock does not apply.

## Lock files

PowerOps.Cli and PowerOps.Core, the projects the Action builds, each have a `packages.lock.json`; the test projects have none. After you add or change a `PackageReference`, run `dotnet restore --force-evaluate` and commit the lock files. CI and the Action restore in locked mode, so a stale lock file fails with NU1004. A Dependabot NuGet PR needs `dotnet restore --force-evaluate` run on its branch and the lock files committed before it can merge, because Dependabot may leave a lock file stale (for example PowerOps.Cli's when the bump is in PowerOps.Core) ([dependabot-core#13950](https://github.com/dependabot/dependabot-core/issues/13950)).

CI also fails on a package with a known vulnerability, direct or transitive; a local build only warns (NU1901 to NU1904 stay warnings, see `Directory.Build.props`).

- The step cannot tell a clean result from a source with no vulnerability data (nuget.org has it, and a network failure fails the step).
- There is no allow-list. When an advisory is on a transitive package and no update of the package that brings it in fixes it, pin the transitive package by hand with a direct `PackageReference` to a fixed version.

## Commits

Use [Conventional Commits](https://www.conventionalcommits.org/): `feat`, `fix`, `docs`, `chore`, `refactor`, `test`, `ci`. Mark breaking changes with `feat!` (or `fix!`, etc.). Releases and the changelog are generated from these by release-please.

## Pull requests

- The PR title is a Conventional Commit.
- New logic comes with tests.
- `dotnet format --verify-no-changes` passes.
- Keep the change focused; open an issue first for anything large.

By participating you agree to the [Code of Conduct](CODE_OF_CONDUCT.md).
