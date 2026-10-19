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

## PowerOps fixtures

`powerops-seed/` was made for this project and is licensed under the repository's Apache-2.0 licence. It is a small seed solution (a table, an environment variable, a connection reference, a cloud flow and a canvas app) built in a test environment, exported with `pac solution export` and unpacked with `pac solution unpack`.

- `v1/`: the clean solution. `powerops analyze` reports no findings.
- `v2/`: the same solution broken on purpose: the flow gains a Compose step with a hard-coded SharePoint URL (PO006), and an unused connection reference `pops_SpareDataverse` is added (PO005).

Scrubbed before committing:

- Removed the canvas app binaries (`*_DocumentUri.msapp`, `*_BackgroundImageUri`, `*_AdditionalUris0_identity.json`). The `.msapp` cached the environment URL. `pops_workitems_2af60.meta.xml` is kept and still references them.
- Renamed the publisher display name to "PowerOps Fixtures".
- `contoso.sharepoint.com` is a placeholder. The files contain no tenant URLs, environment IDs or user names; the remaining GUIDs are component IDs.

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

## Licence text

### microsoft/coe-starter-kit

```text
    MIT License
    Microsoft Power Platform Center of Excellence (CoE) Starter Kit
    Copyright (c) Microsoft Corporation.

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE
```

### microsoft/PowerApps-Samples

```text
    MIT License

    Copyright (c) Microsoft Corporation. All rights reserved.

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE
```
