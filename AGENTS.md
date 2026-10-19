# AGENTS.md

PowerOps is an open-source .NET 10 CLI toolkit for Microsoft Power Platform: analysis, diffing, testing and repeatable deployment. It works alongside `pac`. Plan and principles: [docs/FOUNDATION.md](docs/FOUNDATION.md).

## Layout

- `src/PowerOps.Cli/`: the `powerops` command-line tool
- `tests/PowerOps.Cli.Tests/`: xUnit tests
- `docs/`: plan and roadmap

## Commands

Requires the .NET 10 SDK (see `global.json`).

```sh
dotnet build
dotnet test
dotnet format --verify-no-changes   # what CI runs
dotnet format                       # fix formatting
dotnet pack src/PowerOps.Cli -c Release -o artifacts
```

## Rules

- Offline first: commands work on exported or unpacked solutions, with no tenant, wherever possible.
- Read-only by default. Anything that writes to a tenant or to files outside the output path needs an explicit flag.
- Machine-readable output: stable JSON or SARIF (Markdown for PR comments), and meaningful exit codes.
- Call `pac` as a subprocess. Do not reimplement it. Pass arguments as a list, never through a shell string.
- Make the smallest change that fully solves the task. No unrequested features, options or dependencies.
- New logic comes with tests.
- Keep `dotnet format --verify-no-changes` clean.
- Use Conventional Commits (`feat`, `fix`, `docs`, `chore`, `refactor`, `test`, `ci`).
- Never commit secrets, tokens, tenant URLs or credentials. Use fake values in fixtures and examples.
- Do not edit the version in `Directory.Build.props`, `.release-please-manifest.json` or the changelog. Releases manage them.
- Never push, publish or open a pull request unless asked. Pull request titles are Conventional Commits too. For anything large, suggest opening an issue first.
