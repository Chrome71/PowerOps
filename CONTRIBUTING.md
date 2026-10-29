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

Every project has a `packages.lock.json`. After you add or change a `PackageReference`, run `dotnet restore --force-evaluate` and commit the lock files. CI and the Action restore in locked mode, so a stale lock file fails with NU1004. Dependabot may leave the lock file of a project that gets the package only through a `ProjectReference` (here PowerOps.Cli.Tests, through PowerOps.Cli) unchanged ([dependabot-core#13950](https://github.com/dependabot/dependabot-core/issues/13950)), so such a PR fails CI with NU1004 until someone runs `dotnet restore --force-evaluate` on its branch and commits.

CI also fails on a package with a known vulnerability, direct or transitive; a local build only warns (NU1901 to NU1904 stay warnings, see `Directory.Build.props`).

## Commits

Use [Conventional Commits](https://www.conventionalcommits.org/): `feat`, `fix`, `docs`, `chore`, `refactor`, `test`, `ci`. Mark breaking changes with `feat!` (or `fix!`, etc.). Releases and the changelog are generated from these by release-please.

## Pull requests

- The PR title is a Conventional Commit.
- New logic comes with tests.
- `dotnet format --verify-no-changes` passes.
- Keep the change focused; open an issue first for anything large.

By participating you agree to the [Code of Conduct](CODE_OF_CONDUCT.md).
