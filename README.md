# PowerOps

A free, open-source, cross-platform toolkit that brings software-engineering discipline (analysis, diffing, testing, repeatable deployment) to Microsoft Power Platform. It works alongside `pac`; it does not replace it.

**Status:** early development, Phase 0. There are no commands yet beyond `--version` and `--help`.

## Install

```sh
dotnet tool install --global PowerOps
powerops --version
```

## Build from source

Requires the .NET 10 SDK.

```sh
dotnet build
dotnet test
```

## Documentation

- [Foundation plan and roadmap](docs/FOUNDATION.md)
- [Contributing](CONTRIBUTING.md)
- [License (Apache 2.0)](LICENSE)
