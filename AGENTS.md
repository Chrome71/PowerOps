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

## Recipes

Every recipe ends with the same checks, so run them last: `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`. Test paths are built with `Path.Combine` segments and `Golden.RepoRoot` (`tests/Shared/Golden.cs`).

### Add a rule

1. Pick the next free id (`POxxx`: one after the highest in `docs/rules/`). Severity `Error` means the import or a run breaks; `Warning` means risk.
2. Add a test first in `tests/PowerOps.Core.Tests/Rules/ErrorRuleTests.cs` or `WarningRuleTests.cs`: build a `Solution` (the model is `src/PowerOps.Core/Model/Solution.cs`) with the helpers in `Rules/Builders.cs` (`B.Empty()`, `B.Flow(...)`, `B.Ref(...)`, `B.Env(...)`) and assert on `rule.Evaluate(s)`. Add a fires case and a quiet case. Run it and see it fail; `dotnet test --project tests/PowerOps.Core.Tests` runs only that project.
3. Create `src/PowerOps.Core/Rules/POxxx<Name>.cs`: `public sealed class` implementing `IRule` (`Id`, `Severity`, `Title`, `Fix`, `Evaluate(Solution)`; see `PO011FlowOff.cs`). `Fix` is one imperative plain sentence, with no Markdown and no URL. Each `Finding` is `(RuleId, Severity, Message, File, Component)`.
4. Register it in `Analyzer.Rules` in `src/PowerOps.Core/Rules/Analyzer.cs`.
5. Create `docs/rules/POxxx.md` with the same sections as the other pages, in this order: the `# POxxx: <Title>` heading, `Severity:`, `## What it finds`, `## Why it matters`, `## How to fix`, `## Sources`, `## Suppress` (copy the Suppress text from `PO011.md`). The Sources section is one line: `Sources: [Page title](https://learn.microsoft.com/<path>). verified: yyyy-MM-dd`, with the date you checked the page. State only what the linked pages support; say where a behavior comes from observed exports instead.
6. Add a row to the rule table in `README.md`. If a fixture should trigger it, see "Add a fixture".
7. Update goldens (see "Update goldens" below). `RuleDocsTests` and `RulesCommandTests` fail if the docs page, its Sources line or the rule list is missing.

### Add a reader

1. Write a test in `tests/PowerOps.Core.Tests/Readers/` first. `TempFolder` builds a small folder layout from inline text; `tests/Shared/SolutionZip.cs` builds a zip.
2. Readers turn files into the `Solution` record (`src/PowerOps.Core/Model/Solution.cs`). `XmlUnpackReader` (unpacked folders) and `ZipReader` (exported zips) only list and open files, then call `SolutionParser.Parse(SolutionFiles)` in `src/PowerOps.Core/Readers/`. A new input layout is a new reader class that builds a `SolutionFiles` (path list, read function, manifest path, customizations path) the same way; a new kind of component is a new parse step in `SolutionParser.cs` plus a new field on `Solution`.
3. Wire the reader into `SolutionLoader.Load` and `SolutionDiscovery.Find` so `powerops analyze` finds it. Keep reads capped through `ReadBudget` and never follow symlinks or paths outside the solution (zip-slip).
4. Errors must be `InvalidDataException` whose message starts with the path (see `SolutionLoaderTests`).
5. Update goldens if findings change, and the `README.md` text if the supported layouts change.

### Add a command

1. Create `src/PowerOps.Cli/<Name>Command.cs`: `public static class <Name>Command` with `Create(TextWriter output, ...)` returning a `System.CommandLine` `Command` with `SetAction` (see `RulesCommand.cs`). Return the exit code: `0` ok, `1` findings or violations, `2` usage error, `3` unreadable input or internal error. Writes happen only behind an explicit flag.
2. Register it in `src/PowerOps.Cli/Program.cs` with `root.Subcommands.Add(<Name>Command.Create(...))`.
3. Put report-building logic in `src/PowerOps.Core/Output/`, so the command stays thin and Core tests cover it.
4. If the command prints JSON: add `schemaVersion` first using `JsonOutput.SchemaVersion` (`src/PowerOps.Core/Output/JsonOutput.cs`), create `docs/schemas/<name>.schema.json`, add a row to `docs/schemas/README.md`, add a `(prefix, schema)` entry to `Map` in `tests/PowerOps.Cli.Tests/OutputSchemaTests.cs`, and check a golden of the output with `Golden.Verify(output, "<name>.json")`. `OutputSchemaTests` validates every `tests/golden/*.json` against its schema and fails for a golden or a schema without a match.
5. Tests: add `tests/PowerOps.Cli.Tests/<Name>CommandTests.cs` that calls `Program.Run(args, outWriter, errWriter)` and checks the exit code, output and golden (see `RulesCommandTests.cs`).
6. Add the command to the `README.md` usage block, and update goldens.

### Add a fixture

1. Fixtures live in `tests/fixtures/<name>/`: an unpacked solution (`Other/Solution.xml`), or a zip in `tests/fixtures/zips/`. Public Microsoft samples stay unmodified; your own export is made with `pac solution export`, then `pac solution unpack`.
2. Scrub before committing: no tenant or environment URLs, environment ids, user names, emails, secrets or binary app files (`*.msapp` cache the environment URL). Use fake values such as `contoso.sharepoint.com`, and rename the publisher display name.
3. Describe it in `tests/fixtures/NOTICE.md`: source repo, path and commit with its licence text for third-party files, or "made for this project" with the scrub list for your own. Say what was trimmed.
4. Add a test in `tests/PowerOps.Core.Tests/Rules/FixtureRuleTests.cs` (or `tests/PowerOps.Core.Tests/FixtureTests.cs` for structure) that loads it with `SolutionLoader.Load(Path.Combine(Golden.RepoRoot, "tests", "fixtures", ...))` and asserts the findings.
5. `ReportTests` analyzes the whole `tests/fixtures` folder, so a new fixture changes the `analyze-fixtures.*` goldens: update them.

### Update goldens

1. `POWEROPS_UPDATE_GOLDEN=1 dotnet test` rewrites `tests/golden/*`. The golden self-test fails in this mode; that is expected.
2. `git checkout tests/golden/selftest.txt`: update mode wrongly rewrites it, and its test needs the original.
3. Run a normal `dotnet test`. This is needed because the schema test lists the golden files before the update run writes new ones.
4. Read `git diff tests/golden` and check that only the intended lines changed.
