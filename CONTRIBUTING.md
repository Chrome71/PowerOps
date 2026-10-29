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
- The tests of the scripts under `action/` and `docs/examples/` need `bash`, and some need `jq` or `git` too. They skip on Windows and when a program is not on `PATH`.
- A few tests skip where the file system has no symbolic links or no line breaks in file names, or where a culture is not installed, and a few others skip on Windows or where a file lock does not apply.

## Commits

Use [Conventional Commits](https://www.conventionalcommits.org/): `feat`, `fix`, `docs`, `chore`, `refactor`, `test`, `ci`. Mark breaking changes with `feat!` (or `fix!`, etc.). Releases and the changelog are generated from these by release-please.

## Pull requests

- The PR title is a Conventional Commit.
- New logic comes with tests.
- `dotnet format --verify-no-changes` passes.
- Keep the change focused; open an issue first for anything large.

By participating you agree to the [Code of Conduct](CODE_OF_CONDUCT.md).
