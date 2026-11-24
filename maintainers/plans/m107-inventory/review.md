plan-hash: cb373a94eeacd91f59b3080d79067ed9ffc280afda1f5d52b6a888a181065f80
model: claude-opus-5-5
fresh-context: yes

Third pass, on revision 3 (`0be112f`). Earlier passes: revision 1 (R1 to R9) and revision 2 (R10 to R12). This pass checked the changes from revision 2: Done when 7, Data rows 2a, 3, 7 and 8, Constraints, Assumptions and steps 3 and 4. Environment facts were not read: `tenant: offline`, so `powerops plan facts` was not run. Reviewed from the repo, the upgrade plan's section "M107" and its section 11 WATCH notes, and a scratch count over `tests/fixtures/` with the real readers (a `dotnet run` file app on `SolutionDiscovery.Find`, `SolutionLoader.Load` and `FlowDefinition.Parse`, applying Data rows 2, 2a and 4 as written; it printed counts, connector ids and section sizes only). The reviewer saw its own first pass and the coordinator's note that the plan was revised, not the planning chat.

Tools checked: `powerops`, `pac` and `dotnet` are on PATH; `reports/leakscan.sh` exists in the main checkout; the `powerops-researcher` and `fable-reviewer` agents exist; the test files the plan names exist (`EcosystemDocsTests.cs`, `EcosystemWatchTests.cs`, `PackedToolTests.cs`, `Readers/TempFolder.cs`, `Output/ReportTests.cs`). `powerops plan status docs/plans/m107-inventory`: approved, review pending, 0 open questions, 0 admin questions, 0 violations; after this review is written, 0 warnings.

Data row 8 against the recount: every line holds. `alm-makers` http-action, 11 HTTP actions, 14 request triggers, 7 connectors all unclassified, child flows; `alm-sample` custom-connector, dataverse-in-canvas-app and model-driven-app, 6 connectors and 5 unclassified; `coe-auditlogs` custom-connector, 3 connectors and 2 unclassified, 1 trigger; `equipment-checkout` model-driven-app and no connector; `powerops-seed/v1` and `v2` dataverse-in-canvas-app and 1 connector; the YAML seeds no canvas app read, no driver, 1 connector; `flow-limits`, `flow-secrets` and `flowtests/skipped-body` http-action with 1 HTTP action and 1 trigger; `flow-navigation` and `flowtests/capture/solution` 1 trigger and no driver; `agents/dataverse-tools` 1 connector; each `agents/new-ui` zip 2 connectors, no driver, and 2 reference names that match the GUID or 32-hex pattern; `Contoso_sample` 1 connector; the other zips and `core-portal` nothing. No desktop flow call, no `variable` endpoint and no canvas app with null `Connections` or `Tables` in any fixture.

Data row 2a against the fixtures: form (b) holds for both. `alm-sample`'s canvas app connection and `coe-auditlogs`' connection references and flows use `shared_` + the lower-cased name with `_` as `-` + `-5f` + a hex suffix, and both resolve. Deleting the GUID form changes no count: it matched nothing in the second-pass recount. The three new notes check out: the longest name wins on a match, the patterns run in linear time with a long-name test, and names are made distinct before redaction (Data row 7 tests the first two). No text about the deleted form remains in the plan.

Not applicable, as in the first pass (a CLI change, not a Power Platform solution): items 1 to 8 and 11. Item 9: every step has a done condition and a check. Item 10: no open questions; the Log names the areas not asked. Item 13: each driver's claim has a dated Learn source. The plan and `decisions.md` hold no URL, GUID, email or person's name.

## Findings

### High

#### R1. The default output prints GUIDs from the fixtures, so Done when 5 and Data row 8 cannot hold
- Area: 13 Buildability (two plan lines that can't both hold); redaction (upgrade plan section 4 and its WATCH note)
- Finding: Names printed as the export spells them carried GUIDs into the goldens: the `agents/new-ui` connection reference names end in a hyphenated GUID or a 32-hex run. The first pass also named GUID `connectorid` values in `alm-sample` and `coe-auditlogs`. That part was wrong: the reader takes the direct `connectorid` child, which is the `shared_…` path there.
- Suggested fix: Replace GUID-shaped runs and addresses in names by default, and test the fixtures' default output for them.
- Resolution: fixed in revision 2. Data row 3 replaces hyphenated GUIDs (with or without braces), 32-hex runs and email addresses in names by default and always in the MCP tool. Data row 7 adds a test that the default output on `tests/fixtures`, in all three formats, holds none of them. Data row 8 predicts the `[id]` names in the `agents/new-ui` zips, and the recount confirms two per zip.

#### R10. Two Constraints lines still state the old, wider claim and contradict Done when 5
- Area: 13 Buildability (two plan lines that can't both hold)
- Finding: Constraints say "no output, golden or MCP result holds a host, header value, GUID or address by default" and "The word 'standard' never appears in any output". Done when 5, `Won't build` and Data row 7 now say the opposite for names: a name holding a bare host or the word "standard" prints as written, and a test pins that ("a flow named with the word Standard prints it as written"). The build can satisfy only one of each pair.
- Suggested fix: Reword the two Constraints lines to Done when 5's terms: no host of an endpoint and no header, query or id value by default, with GUIDs, addresses and URLs in names replaced; the command's own text never says "standard", while names print as written.
- Resolution: fixed in revision 3. Both Constraints lines are restated "as Done when 5 states it": no value of a URL, header, parameter or id is read; a host only with `--show-values`; names redacted by default; and the command's own text and the goldens never hold "standard", while a maker's name prints as written.

### Medium

#### R2. Custom connectors are not matched to the ids that flows, apps and references use
- Area: 13 Buildability; correctness of `notClassified`
- Finding: The id used for a custom connector didn't equal its schema name, so both fixtures' custom connectors would have been listed as "tier not classified".
- Suggested fix: Specify the observed id forms and test them.
- Resolution: fixed in revision 2. Data row 2a form (b) resolves both fixtures' custom connectors in the recount, so neither is in `notClassified`. Data row 7 tests the forms; Data row 8 says a `notClassified` entry for a custom connector stops the build.

#### R3. Data row 8's expected golden counts are incomplete
- Area: 9 Testability; the golden counts
- Finding: Drivers in `alm-sample`, `powerops-seed`, `equipment-checkout` and the request triggers of six solutions were missing, and "flowtests" was two solutions.
- Suggested fix: Recount per solution display path.
- Resolution: fixed in revision 2. Data row 8 is a per-solution table, and the recount with the real readers matches it line by line.

#### R4. Done when 5 claims more about names than the plan's redaction does or its tests can prove
- Area: 13 Buildability; redaction and the WATCH note on "standard"
- Finding: Names holding "standard", a bare host or an address were printed, against Done when 5's wording.
- Suggested fix: Narrow the claim or filter names, and pin the behaviour with a test.
- Resolution: fixed in revision 2 for Done when 5, `Won't build` and Data row 7: addresses are replaced, and "standard" and bare hosts in names are printed and pinned. The Constraints section was not updated: see R10.

#### R5. `PackedToolTests` asserts 12 MCP tools and is not among the files the change must reach
- Area: 9 Testability; AGENTS.md "Add a command"
- Finding: `PackedToolTests.cs` asserts 12 tools, and the tool's position was not given.
- Suggested fix: Name the file, and place the tool.
- Resolution: fixed in revision 2. Step 4 and Data row 7 name `PackedToolTests.cs` (13 tools); Data rows 6 and 7 place `inventory` after `diff` in `McpCommand.Tools`, `ToolNames` and the golden.

### Low

#### R6. Error output: stderr redaction and the solution path in a flow parse error
- Area: 9 Testability; AGENTS.md "Add a reader" step 4
- Finding: stderr redaction and the solution path prefix of a flow error were not specified.
- Suggested fix: Specify both and test them.
- Resolution: fixed in revision 2 (Data row 6, with tests in Data row 7).

#### R7. Custom pages are not addressed
- Area: 13 Buildability
- Finding: A custom page would have fired `dataverse-in-canvas-app` and been named a canvas app.
- Suggested fix: Say how type 2 is treated.
- Resolution: fixed in revision 2. Custom pages are a `customPage` user with no `dataverse-in-canvas-app` (Data rows 2 and 4), tested in Data row 7.

#### R8. Five of the drivers' Learn pages are quoted but not watched
- Area: Ecosystem
- Finding: Only two cited pages were added to `sources.json`.
- Suggested fix: Add the rest, or record why not.
- Resolution: fixed in revision 2. The drivers' sources are trimmed to six pages, and Data row 9 adds all six (one `github-commits`, five `manual`).

#### R9. The "Windows and macOS" runbook in `docs/BUILD-PLAN.md` says the server lists 12 tools
- Area: Docs
- Finding: The runbook's tool count goes stale.
- Suggested fix: Update it.
- Resolution: fixed in revision 2 (Done when 8, Data row 9).

#### R11. Assumptions say custom connector GUIDs are observed as `connectorid` in both fixtures; the reader sees neither
- Area: 13 Buildability (a stated observation that doesn't hold)
- Finding: The Assumptions line on custom connector ids says form (c) of Data row 2a, a type-372 component's GUID as a `connectorid`, is observed in `alm-sample` and `coe-auditlogs`. Neither reaches the report:
  - In `alm-sample`, the GUID is in the custom connector's own definition (`Connectors/*.xml`), not in a connection reference.
  - In `coe-auditlogs`, it sits under `customconnectorid/connectorid`, while `ReadConnectionReference` reads only the direct `connectorid` child, which is the `shared_…` path.

  Form (c) is harmless and is tested with made-up data, but it rests on no observed export.
- Suggested fix: In Assumptions, say that only form (b) is observed through the reader and that (c) is kept as a guard for a bare GUID `connectorid`, unobserved. Or drop (c) if no export is known to need it.
- Resolution: fixed in revision 3. Form (c) and its test are deleted. Assumptions now say that only the `shared_<name>-5f<hex>` form reaches the reader in any fixture, and that `coe-auditlogs`' GUID sits under `customconnectorid`, unread.

#### R12. Which test writes the `inventory.*` goldens is stated two ways
- Area: 9 Testability
- Finding: Done when 7 says the goldens are "the command on `tests/fixtures`", and Data row 7 lists "the goldens" among the `InventoryCommandTests` cases. Data row 8 says a Core test writes them with `Golden.RepoRoot` as the base of the display paths, as `ReportTests` does. Two tests writing one golden would fight in update mode, and paths relative to the current folder differ from paths relative to `Golden.RepoRoot`.
- Suggested fix: Name one writer, presumably `InventoryReportTests` as in Data row 8. Word Done when 7 to match, and take "the goldens" out of the `InventoryCommandTests` list, or make that test compare without writing.
- Resolution: fixed in revision 3. Done when 7 and Data row 8 name `InventoryReportTests` as the one writer; Data row 7 says `InventoryCommandTests` has no golden of its own; step 4 adds the golden test, and step 3 leaves it to step 4.
