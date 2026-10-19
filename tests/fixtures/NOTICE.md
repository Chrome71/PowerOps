# Fixture notice

Unmodified copies of public Microsoft sample data, used as test inputs. Both source repositories are MIT licensed (read from each repo's `LICENSE`).

| Fixture | Source repo | Path in repo | Commit |
|---|---|---|---|
| `coe-auditlogs/` | https://github.com/microsoft/coe-starter-kit | `CenterofExcellenceAuditLogs/SolutionPackage/src` | `d4ceb8b718883d841ef9680dc344494ebd68d3b7` |
| `alm-sample/` | https://github.com/microsoft/coe-starter-kit | `ALMAcceleratorSampleSolution/SolutionPackage/src` | `d4ceb8b718883d841ef9680dc344494ebd68d3b7` |
| `alm-makers/` | https://github.com/microsoft/coe-starter-kit | `ALMAcceleratorForMakers/SolutionPackage` | `d4ceb8b718883d841ef9680dc344494ebd68d3b7` |
| `zips/ColumnLevelSecuritySampleSolution.zip`, `zips/ColumnLevelSecuritySampleSolution_managed.zip` | https://github.com/microsoft/PowerApps-Samples | `dataverse/webapi/PS/ColumnLevelSecurity/ExportedFiles/` | `8e42f87026f4c93d7e468f28416f5852375d325a` |
| `zips/polymorphiclookupexamplesolution.zip` | https://github.com/microsoft/PowerApps-Samples | `dataverse/webapi/PS/PolymorphicLookup/` | `8e42f87026f4c93d7e468f28416f5852375d325a` |
| `zips/Contoso_sample_1_0_0_1_managed.zip` | https://github.com/microsoft/PowerApps-Samples | `dataverse/orgsvc/CSharp/SolutionStagingAndImport/` | `8e42f87026f4c93d7e468f28416f5852375d325a` |

## Licence

MIT License. Copyright lines from each repo's `LICENSE`:

- coe-starter-kit: "Copyright (c) Microsoft Corporation."
- PowerApps-Samples: "Copyright (c) Microsoft Corporation. All rights reserved."

## Trimmed

- `coe-auditlogs/`: nothing removed.
- `alm-sample/`: removed `PluginAssemblies/PCATPlugins-7CCFB007-8A61-40C2-9D5F-0667DEB40302/PCATPlugins.dll` (binary). It is still referenced by `PCATPlugins.dll.data.xml` and by the type 91 root component in `Other/Solution.xml`.
- `alm-makers/`: trimmed: only `Other/`, `environmentvariabledefinitions/` and `Workflows/` kept; `Entities/`, `OptionSets/`, `Roles/` and `CanvasApps/` (about 11 MB) dropped, so components that live only there are referenced but absent.
- `zips/`: nothing removed.

File contents are not modified. Names, URLs and IDs in the files (for example `powercattools.sharepoint.com`) are Microsoft's public sample data.
