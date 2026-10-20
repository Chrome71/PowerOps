---
status: signed-off
review: done
signed-off: 2026-10-19
signed-off-hash: 1d289dc4a81dd27d661aaba1cde7eccfd2caafe6ac8d14ce1ec6e3607a7ad948
environment: PowerOps Test
depth: quick
supersedes: null
---

# M5: YAML layout

## Goal

`powerops analyze` reads the YAML source layout that Dataverse Git integration commits (`solutions/*/solution.yml`) into the same `Solution` model as the XML and zip readers, so every existing rule works on it unchanged. M5 also adds PO013 (a component in `rootcomponents.yml` has no source files, error) and PO014 (a canvas app `.pa.yaml` formula contains a hard-coded environment or SharePoint URL, warning). PO006 stays flow-only. Release v0.6.0.

Done when each version of the YAML fixture gives exactly its expected findings (rule, component and message; file paths differ by layout):

- v1: the same findings as XML `powerops-seed/v1`, which is none, and no PO013.
- v2: the same findings as XML `powerops-seed/v2` (PO005, PO006), plus PO014 for the canvas formula that only the YAML layout can show.

If the fallback in Step 1 is used, Done-when narrows to: the hand-written fixture gives these findings, and each hand-written version, packed offline with `pac solution pack`, gives the same findings through the zip reader (apart from PO013 and PO014).

## Users

- Agents in the user's Claude Code or Codex that run `powerops analyze --format json` on a repo connected to Git integration.
- CI users of `action.yml` whose repo holds the YAML layout. The action already runs `analyze` on the checkout, so it picks the layout up without changes.

## Data

Input only. The tool reads files and changes nothing.

The YAML layout is a mechanical translation of the solution XML. It keeps the element names, uses `'@name'` keys for attributes and `'#text'` for element text, and turns repeated elements into a YAML list. This is observed in public Git integration exports (see Assumptions). Microsoft documents the folders, not the keys.

| What | Where in the YAML layout | XML layout equivalent |
|---|---|---|
| Manifest: unique name, version, managed | `solutions/<name>/solution.yml` (`ImportExportXml.SolutionManifest`) | `Other/Solution.xml` |
| Root components | `solutions/<name>/rootcomponents.yml` | `RootComponents` in `Solution.xml` |
| Missing dependencies | `solutions/<name>/missingdependencies.yml` | `MissingDependencies` in `Solution.xml` |
| Which components belong to the solution | `solutions/<name>/solutioncomponents.yml` (`'@path'` entries) | the folder itself |
| Publisher and prefix | `publishers/<name>/publisher.yml` | `Publisher` in `Solution.xml` |
| Cloud flows | `modernflows/<id>/workflow.yml` plus the definition `.json` | `Workflows/*.json.data.xml` plus `.json` |
| Connection references | `connectionreferences/<logicalname>/connectionreference.yml` | `Other/Customizations.xml` |
| Environment variables | `environmentvariabledefinitions/<schema>/environmentvariabledefinition.yml`, current value in `environmentvariablevalues/environmentvariablevalue.yml` | `environmentvariabledefinition.xml`, `environmentvariablevalues.json` |
| Canvas apps | `canvasapps/<name>/canvasapp.yml` plus the unpacked app folder `<name>_DocumentUri/Src/*.pa.yaml` | `CanvasApps/*.meta.xml` plus a binary `.msapp` |

The YAML root is the folder that holds `solutions/`. It can hold several `solutions/*/solution.yml` that share the component folders (an environment-bound Git repo). Each `solution.yml` is reported as its own solution, and its components are the ones its `solutioncomponents.yml` lists, the same set SolutionPackager packs with `/SolutionName`. Its publisher is the `publishers/<name>/publisher.yml` whose name matches the publisher unique name in its manifest (`SolutionManifest.Publisher.UniqueName`), as in the public export. A YAML solution's reported path is the YAML root, and finding files are relative to it, so `path + "/" + file` (SARIF, CI annotations, JSON) resolves to a real file.

A `.pa.yaml` file holds `App`, `Screens` and `ComponentDefinitions`. Each control has `Properties`, a map of property name to a formula string that starts with `=`. Multi-line formulas are YAML block scalars.

Model changes, both empty for the XML and zip layouts:

- `SourceFolders`: the component folders that exist under the YAML root and that this solution's `solutioncomponents.yml` lists (for example `entities/pops_workitem`), so PO013 can check root components against them.
- `CanvasFormulas`: each `.pa.yaml` formula with its file and property path, for PO014. `CanvasAppTexts` is filled from `canvasapp.yml` as it is from `.meta.xml` today, so PO005 works the same in both layouts.

Git integration commits only unmanaged solutions, so YAML fixtures are unmanaged. The reader still reads `Managed` from `solution.yml` like the XML reader does.

## Automation

None new. `analyze`, `baseline`, the JSON, SARIF and Markdown output and `action.yml` stay as they are. The JSON contract from M4 (`schemaVersion` 1) does not change: new rules add findings, not fields.

## Screens

None. This is a command-line change. Text output is unchanged apart from the new findings.

## Environments and route

- The tool itself is offline and needs no environment. It ships as v0.6.0 through the normal release.
- The YAML fixture comes from the target environment in the header, which already holds the `PowerOpsSeed` solution. Only Git integration writes the YAML layout: Microsoft's Git integration FAQ says the `unpack`, `clone` and `sync` commands don't support YAML yet, and the open bug microsoft/powerplatform-build-tools#1385 shows `clone` still writing XML (read 2026-10-19). So the YAML layout can't be made offline from a zip.
- Fixture route: bind `PowerOpsSeed` alone to an Azure DevOps repo with a solution binding, commit v1 and v2 from the environment, clone the repo locally, copy the working-tree files (never the `.git` history) into `tests/fixtures/powerops-seed-yaml/`, scrub, and review the diff before committing. Afterwards, disconnect the solution and delete the Azure DevOps repo, because it keeps the unscrubbed commits.
- The fixture is committed before 2026-12-15. After that, nothing in M5 needs a tenant.

## Solution and publisher

- The fixture is the `PowerOpsSeed` solution (publisher prefix `pops`), the same solution already committed in the XML layout as `tests/fixtures/powerops-seed/`.
- `plan facts` lists two other solutions in the environment. Neither is bound to Git, and M5 creates and changes no solution apart from making the fixture.

## Constraints

- Offline first and read-only, as everywhere in PowerOps. `pac` is never called by the reader.
- Smallest complete change. No new options or flags.
- Dependencies: YamlDotNet may be added from M5 (BUILD-PLAN decision). Nothing else.
- Licensing: the test environment is a managed developer environment. Managed Environments use rights aren't in the Power Apps Developer Plan: every user who runs apps or flows there needs Power Apps Premium or Power Automate Premium, and trial licences count for their 30 days (Learn: Managed Environments licensing, Developer Plan FAQ, read 2026-10-19). The maintainer holds only the Developer Plan, so a 30-day premium trial is started right before Step 1 and the commits are made within it. Azure DevOps Basic access is free for the first five users. The GitHub connection (preview, needs Azure Key Vault) is not used.
- Roles for Step 1: System Administrator in the environment, and Basic access plus Contributor on the Azure DevOps repo. The tool itself needs no roles or licences.
- DLP: does not apply to the offline tool. The environment's DLP policy was not checked. Step 1 stops if a policy blocks the seed's connectors or the Git connection.
- Fixtures: only MIT or Apache-2.0 third-party files, or files made for this project. No tenant URLs, environment IDs, names or emails.
- Deadline: the YAML fixture is committed before 2026-12-15. If it is not committed by 2026-12-01, Step 1 falls back to hand-written YAML.
- The rule docs' sources are rechecked with `/verify-research` (BUILD-PLAN decision 8).

## Assumptions

- The test environment is a developer environment, and the maintainer enabled Managed Environments on it on 2026-10-19 (admin center shows Managed: Yes). The 2026-12-15 suspension applies to developer environments that aren't managed, so it no longer applies to this one. The plan still keeps that date as the deadline and relies on committed fixtures.
- File shapes come from public Git integration exports read on 2026-10-19: a public MIT-licensed export for `solution.yml`, `rootcomponents.yml`, `missingdependencies.yml`, `solutioncomponents.yml`, `publisher.yml` and canvas apps; two unlicensed repos for flows, connection references and environment variables, read only and not copied.
- `.pa.yaml` files exist only in the YAML layout. In the XML and zip layouts the canvas app is a binary `.msapp`, which M5 does not open.
- The retired `.fx.yaml` format (in the `alm-sample` fixture) is not read.
- Microsoft's `Microsoft.PowerPlatform.PowerApps.Persistence` package is not used: it is a 2024 preview whose API may change. YamlDotNet reads the formula strings.
- Input limits: one `ReadBudget` covers a whole YAML root, and shared component files are read once even when several solutions list them. Because `analyze` calls the loader once per `solution.yml`, the loader keeps the root's file cache and budget across calls for the same root. Symlinks are not followed, YAML nesting depth is capped like the XML reader's, and YAML anchors and aliases are rejected (`InvalidDataException` starting with the path), because the exports don't use them and expanding them can exhaust memory.

## Open questions

## Questions for your admin

## Steps

Every step that changes findings on `tests/fixtures` updates the goldens (AGENTS.md "Update goldens") in the same commit.

1. `test:` Make the YAML fixture, by hand in the test environment:
   1. Start a 30-day Power Apps Premium or Power Automate Premium trial.
   2. Create a private Azure DevOps repo, and bind `PowerOpsSeed` alone to it with a solution binding.
   3. Undo the v2 breakage by hand: remove the Compose step with the URL from the flow, and remove the spare connection reference `pops_SpareDataverse`. Commit: this is v1.
   4. Re-apply the breakage, and add a canvas formula with a fake site URL that has a `/sites/` path, for example a `Launch(...)` of the fake `contoso.sharepoint.com` site `ops` (path `/sites/ops`) in a button's `OnSelect`. Add no data source or connection. Commit: this is v2.
   5. Clone the repo, check out the v1 commit and copy its working-tree files into `tests/fixtures/powerops-seed-yaml/v1`, then do the same for v2, and scrub them with the AGENTS.md "Add a fixture" list. Add the YAML-specific files to that list: contact fields in `publisher.yml` (also rename the display name to "PowerOps Fixtures", as in the XML fixture), environment and connection IDs in `connectionreference.yml`, `canvasapp.yml` and the `_DocumentUri` JSON, and binary assets in the unpacked app folder.
   6. Add a `tests/fixtures/NOTICE.md` entry, then disconnect the solution and delete the Azure DevOps repo.

   Fallback: if this is not done by 2026-12-01, write v1 and v2 by hand from the XML fixture, following the shapes of the public exports, and mark them hand-written in NOTICE.md. Pack each version offline with `pac solution pack`, analyze the zip with the zip reader, and check it gives the same findings as the YAML reader, apart from PO013 and PO014. Record the pac version in NOTICE.md.
2. `chore(core):` Add YamlDotNet to PowerOps.Core.
3. `feat(core):` A YAML reader that turns `solution.yml`, `publisher.yml`, `rootcomponents.yml`, `missingdependencies.yml`, flows, connection references, environment variables and `canvasapp.yml` into the `Solution` model by reusing the XML parser's element logic. Fill `SourceFolders`. Tests in `tests/PowerOps.Core.Tests/Readers/`, including an inline `Managed: 1`, a file with an anchor that must be rejected, and a file nested deeper than the cap that must be rejected (`InvalidDataException` starting with the path).
4. `feat(core):` `SolutionDiscovery` returns one path per `solutions/<name>/solution.yml`, and only for YAML roots at or below the given path: pointing `analyze` below a YAML root finds nothing there, as for any folder without a solution, and never reads above the given path. `SolutionLoader` loads each one, reading the shared component folders from the YAML root, keeping only what its `solutioncomponents.yml` lists, and reusing the root's file cache and budget across calls. The reported solution path is the YAML root (duplicates are still told apart by the `solution.yml` path). Tests: a two-solution root with different publishers and a root component that only the other solution lists; a SARIF location for a YAML finding that resolves to an existing file; a path below the YAML root. Errors are `InvalidDataException` starting with the path. Update goldens.
5. `feat(analyze):` PO013 for root components whose folder is known from real exports: table (type 1, `entities/<schemaName>`), cloud flow (29, `modernflows/<id>`), canvas app (300, `canvasapps/<schemaName>`) and environment variable (380, `environmentvariabledefinitions/<schemaName>`). Other types are skipped, and the rule is quiet when `SourceFolders` is empty (XML and zip). It compares folder names case-insensitively and normalizes IDs (no braces, lowercase) like `SolutionParser.NormalizeId`. Tests: a missing folder fires, a folder whose case differs from the schema name does not. Add `docs/rules/PO013.md`: Error, because pack leaves the component out with exit code 0, so the imported solution silently lacks it; the Sources line cites the pac solution reference and the YAML format page for the drop. Classic workflows (`workflows/`) are skipped until a real export shows their folder shape. Update goldens.
6. `feat(core):` Read `.pa.yaml` formulas of canvas apps in the YAML layout into `CanvasFormulas`.
7. `feat(analyze):` PO014. Only formula strings (values starting with `=`) are scanned. Each distinct URL per app is one finding, naming the first control and property where it appears in file order. The message is "Canvas app '<app>' hard-codes the URL '<url>' in <control>.<property>.", the component is the URL as in PO006, and the file is the `.pa.yaml` path. Move PO006's URL pattern to `RuleHelpers` and use it from both rules, with PO006's tests unchanged. Passing and failing tests and `docs/rules/PO014.md`. Update goldens.
8. `test:` Parity test: each YAML fixture version gives exactly the findings listed under Goal, compared on rule, component and message.
9. `docs:` README layouts and rule table, the FOUNDATION section 8 YAML items ticked with their sources, the BUILD-PLAN M5 row, and the rule docs' sources rechecked.
