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
- `../settings/`: deployment settings files (`deploymentsettings.json` format) for `v1/`. `powerops-seed-v1.valid.json` is the output of `pac solution create-settings --solution-folder v1` (pac 2.13.1, offline) with `Value` and `ConnectionId` filled in with fake values, and is clean; `powerops-seed-v1.problems.json` is hand-written: it misses a connection reference, lists `pops_DefaultStatus` twice (in different case), and has an extra environment variable and an extra connection reference. Made for this project, fake values only.
- `../drift/`: snapshot files for `powerops drift` (`seed-dev.json`, an unmanaged `Contoso Dev`, and `seed-test.json`, a managed `Contoso Test` with an unmanaged layer on the flow and a different environment variable value). Hand-written for this project, in the format `powerops drift --save` writes; fake values only (made-up ids and hashes, except the seed flow's id from `v1/`). The goldens of `drift` now come from the captures in `pac/drift/capture/`; these files stay for the snapshot-file tests.

Scrubbed before committing:

- Removed the canvas app binaries (`*_DocumentUri.msapp`, `*_BackgroundImageUri`, `*_AdditionalUris0_identity.json`). The `.msapp` cached the environment URL. `pops_workitems_2af60.meta.xml` is kept and still references them.
- Renamed the publisher display name to "PowerOps Fixtures".
- `contoso.sharepoint.com` is a placeholder. The files contain no tenant URLs, environment IDs or user names; the remaining GUIDs are component IDs.

`powerops-seed-yaml/` is the same `PowerOpsSeed` solution in the Dataverse Git integration YAML layout, made for this project under the same licence. The solution was bound to an Azure DevOps repo with a solution binding (folder `seed`), and v1 and v2 were committed from the test environment. The files are the working tree of each commit under `seed/PowerOpsSeed/`, without the Git history. Expected findings: none for `v1/`; PO005 and PO006, as in `powerops-seed/v2`, plus PO014 for `v2/`, where the canvas app gains a button whose `OnSelect` is `Launch("https://contoso.sharepoint.com/sites/ops")`. The current CLI does not read this layout yet.

Scrubbed and trimmed before committing:

- Removed from the canvas app folder: `*_BackgroundImageUri`, `*_AdditionalUris0_identity.json`, and from `*_DocumentUri/` the `Controls/*.json`, `References/*.json`, `Header.json`, `Properties.json`, `Resources/PublishInfo.json` and `AppCheckerResult.sarif`. Only `Src/*.pa.yaml` is kept. `canvasapp.yml` still references the removed files.
- Removed the `Readme.md` that Git integration creates in the binding folder.
- Renamed the publisher display name to "PowerOps Fixtures". The contact fields in `publisher.yml` were already empty.
- `connectionreference.yml` holds no connection IDs. The files contain no tenant URLs, environment IDs or user names; the remaining GUIDs are component IDs, the same as in `powerops-seed/`.

`pac/` holds `pac` 2.13.1 output captured for this project, under the same licence, by the maintainer in the test environment that holds `PowerOpsSeed` (2026-10-21). `stdout` was saved apart from `stderr`; every capture left `stderr` empty.

- `org-who.txt`: `pac org who`. The environment id is on the `Environment ID:` line.
- `list-cloud-flows-on.json`, `list-cloud-flows-off.json`: `pac power-automate list-cloud-flows --environment <id> --workflow-id <seed flow id> --json`, exit 0, with the seed flow on and then off (checked in the portal before and after). The output is `{"value":[...]}`; `workflowId` holds the id, and `stateCode` holds the state: `Published` when on, `Draft` when off.
- `list-cloud-flows-missing.json`: the same command with an id that does not exist: `null`, exit 0.
- `list-cloud-flows-bad-id.txt`: the same command with `--workflow-id not-a-guid`: exit 1, and `pac` prints its error on `stdout`, not `stderr`.
- `list-cloud-flows-suspended.json` is hand-made, not observed: `list-cloud-flows-on.json` with `stateCode` and `statusCode` set to `Suspended`, the third value of the `WorkflowStateCode` enum on Microsoft Learn.

Scrubbed before committing: the environment and organisation ids, the unique name, the org URL, the user's email and user id, the business unit id, the flow's `resourceId` and the error's correlation id became fake values (`00000000-0000-0000-0000-00000000000a` to `...0f`, `admin@contoso.com`, `https://contoso-dev.crm.dynamics.com/`), and the environment name became "Contoso Dev". The seed's own workflow id and the ids inside its flow definition are kept; they are component ids, the same as in `powerops-seed/`.

`pac/drift/sample/` is hand-written for this project, not captured: it is shaped like the Dataverse Web API responses that Microsoft Learn documents for FetchXML queries (`{"@odata.context":...,"value":[...]}`, raw values, no property for a null column, `v.value` for the outer-linked column, the `morerecords` annotation, the `{"error":{"code","message"}}` body), with fake values only (the `contoso` hosts, ids `00000000-0000-0000-0000-0000000000xx`, the seed's flow id, and a made-up variable value with a space, a comma and a line break). `dev-read2.json` to `dev-read6.json` and `test-read2.json` to `test-read6.json` are reads 2 to 6 of `drift` for `PowerOpsSeed` (Test equal to Dev except `ismanaged` and the fake connection id); `not-installed-read2.json` is read 2 with no row; `morerecords.json` has the annotation true; `error-400.json` is an error body. They stay as hand-written edge cases (`morerecords.json`, the error body); the Step 1 captures are in `capture/`.

`pac/drift/capture/` holds the Step 1 captures of the plan `m8-drift`, made for this project with `pac` 2.13.1 and Azure CLI 2.90.0 by the maintainer on 2026-10-22, against their Dev (an unmanaged install of `PowerOpsSeed`) and Test (a managed install of it), read-only: `pac org who` and Web API GETs with FetchXML, nothing written. Under the same licence as `pac/`.

- `dev-read1.txt`, `test-read1.txt`: `pac org who --environment <env>`.
- `dev-read2.json` to `dev-read6.json` and `test-read2.json` to `test-read6.json`: reads 2 to 6 of `drift` (solution, components, flows, environment variables, connection references). `dev-read2-missing.json` is read 2 for a solution that is not installed; `dev-malformed.json` is the error body (HTTP 400) of a query naming a column that does not exist.
- `test-clean-read7-<type>-<n>.json` and `test-edited-read7-<type>-<n>.json`: read 7 (the component's layers), one query per component, in Test right after the import (clean) and after the seed flow was edited in the maker portal (edited: an extra row with `msdyn_solutionname` "Active" above the managed `PowerOpsSeed` row). `<type>` is the component's `componenttype`, `<n>` counts the components of that type.

Probes that are not stored as files: the batched layer query (an `in` condition on several ids) and the query without `msdyn_solutioncomponentname` both returned an empty value list, not an error; the layer table answers only for one component id together with its exact component name. The queries are in `pac/drift/queries/`.

Scrubbed before committing (by a script kept outside the repository; the raw files are not committed): the environment names became "Contoso Dev" and "Contoso Test", the URLs `https://contoso-dev.crm.dynamics.com/` and `https://contoso-test.crm.dynamics.com/`, the unique names, emails (`admin@contoso.com`) and publisher names fake values. Every id (organisation, environment, user, solution, component, solution component, connection reference, environment variable definition, layer) went through one real-to-fake mapping across all files (`00000000-0000-0000-0000-0000000000xx`), so a component read twice keeps one fake id; only the seed flow's workflow id is kept. Connection ids became `fake-connection-<n>` (set or absent is kept). Dropped: every `@odata.*` property and `msdyn_changes` and `msdyn_componentjson` of the layer rows (component data), and the error text of the malformed query (now `<error message>`). Kept: `componenttype` numbers, `statecode`, booleans, versions, schema and logical names (`pops_*`), connector ids, the layer names, and the made-up variable value `Open, then` and `review` on two lines. In the captures the ids of tables, canvas apps, environment variable definitions and connection references differ between Dev and Test; only the flow's id is the same.

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
