# Contributing to PowerOps

Thanks for helping out. Phases 0 and 1 of the [roadmap](docs/FOUNDATION.md) need no Power Platform tenant, so you can build, test and contribute with just the .NET SDK.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (see `global.json`)

## Build, test, format

```sh
dotnet build
dotnet test
dotnet format            # fix formatting
dotnet format --verify-no-changes   # what CI runs
```

## Commits

Use [Conventional Commits](https://www.conventionalcommits.org/): `feat`, `fix`, `docs`, `chore`, `refactor`, `test`, `ci`. Mark breaking changes with `feat!` (or `fix!`, etc.). Releases and the changelog are generated from these by release-please.

## Pull requests

- The PR title is a Conventional Commit.
- New logic comes with tests.
- `dotnet format --verify-no-changes` passes.
- Keep the change focused; open an issue first for anything large.

By participating you agree to the [Code of Conduct](CODE_OF_CONDUCT.md).
