plan-hash: c0d00c608af5f751cedb710b9e054e78bc33957daade1bc32984dd43048f5dc7
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: `tenant: offline`, so `powerops plan facts` was not run. Reviewed from the repo at `a8d4a03`, the upgrade plan's section "M107" and its section 11 WATCH notes, and a scratch count over `tests/fixtures/` (Python, counts and names only; the zips were listed, not counted for flows).

Tools checked: `powerops`, `pac` and `dotnet` are on PATH; `reports/leakscan.sh` exists in the main checkout; the `powerops-researcher` and `fable-reviewer` agents exist. `powerops plan status docs/plans/m107-inventory`: approved, review pending, 0 open questions, 0 admin questions, 0 violations.

Not applicable to this plan, which changes the PowerOps CLI rather than a Power Platform solution: items 1 (solution and publisher), 2 (connection references and environment variables), 3 (licences: the `### Licences` heading is present and the work uses none), 4 (DLP), 5 and 6 (route, managed), 7 (delegation), 8 (security roles), 11 (no table, list, flow or screen; `Won't build` is present). Item 9: every step has a done condition and a check. Item 10: no open questions; the Log names the areas not asked. Item 13: each driver's Microsoft claim has a dated Learn source in Assumptions. The plan and `decisions.md` hold no URL, GUID, email or person's name.

## Findings

### High

#### R1. The default output prints GUIDs from the fixtures, so Done when 5 and Data row 8 cannot hold
- Area: 13 Buildability (two plan lines that can't both hold); redaction (upgrade plan section 4 and its WATCH note)
- Finding: Data row 2 prints connector ids (the last `/`-segment of `ConnectorId`) and connection reference logical names, and Data row 3 redacts only `scheme://` URLs (`Redaction.Text`). The fixtures hold GUIDs in exactly those fields: two connection references (in `alm-sample` and `coe-auditlogs`) have a `connectorid` that is a bare GUID (the custom connector's component id), so `connectors[].id` and `notClassified[].id` are GUIDs; the agent connection references of `agents/new-ui/*.zip` have logical names ending in a hyphenated GUID or a 32-hex-digit run, which `references` prints. Real exports name agent connection references this way. So Done when 5 ("by default none holds ... a GUID") fails on the plan's own golden, and Data row 8's "any GUID ... in the goldens stops the build" stops it predictably. Done when 5 also cites "tests on every fixture", but Data row 7 tests only "standard" on the fixtures; the GUID, host and address check on the fixtures is a one-time grep in step 4.
- Suggested fix: Decide and write in Data rows 2 and 3: a connector id that is a GUID is matched (braces removed, ignoring case) to a root component or missing dependency of type 372 and printed as that custom connector's schema name, else as a fixed text; GUID-shaped runs in printed names (hyphenated and 32-hex) are replaced with a placeholder unless `--show-values`, and always in the MCP tool. Add a Data row 7 test that runs the command on `tests/fixtures` in all three formats, by default, and asserts no hyphenated or 32-hex GUID, no email address and no URL other than the Learn sources. Predict the replaced names in Data row 8.
- Resolution: open

### Medium

#### R2. Custom connectors are not matched to the ids that flows, apps and references use
- Area: 13 Buildability; correctness of `notClassified`
- Finding: Data row 2 leaves a connector out of `notClassified` only when its id equals a custom connector's name, ignoring case. In the fixtures the connector of a custom connector is named `shared_` + the schema name in lower case with `_` turned into `-` + `-<hash>` (for example `admin_5Foffice-20365-20management-20api` is used as `shared_admin-5foffice-20365-20management-20api-5ff18d20b7ec3dbffe` in `coe-auditlogs`, and `cat_5Fenvironmentvariableconnector` as `shared_cat-5fenvironmentvariableconnector-5f354898fb9b10ca45` by `alm-sample`'s canvas app), or the component's GUID (R1). Neither equals the schema name, so both fixtures' custom connectors are listed as "tier not classified" next to a `custom-connector` driver. The Data row 7 case "a custom connector used by a flow is not [in notClassified]" uses made-up names that match exactly, so it passes while the golden shows the opposite.
- Suggested fix: Specify the match in Data row 2: the GUID id (R1), and the `shared_<schema lower, _ to ->-<suffix>` api name form. Add Data row 7 cases built from the `coe-auditlogs` and `alm-sample` shapes, and say in Data row 8 that neither fixture's custom connector is in `notClassified`.
- Resolution: open

#### R3. Data row 8's expected golden counts are incomplete, so its stop rule fires on lines that can be predicted
- Area: 9 Testability; the golden counts (WATCH note "Golden counts")
- Finding: The rows checked against `tests/fixtures/` miss: `dataverse-in-canvas-app` in `alm-sample` (its canvas app connects to `shared_commondataserviceforapps`), in `powerops-seed/v1` and `v2` (their canvas app's `CdsDependencies` names a type-1 table), and in `powerops-seed-yaml/v1` and `v2` if the YAML reader fills `Tables` from the same text; `model-driven-app` in `equipment-checkout` (an `AppModules/` entry). `requestTriggers` lists only `alm-makers` and `alm-sample`, while the Log counts one `Button` each in `coe-auditlogs`, `flow-limits`, `flow-navigation`, `flow-secrets` and two in `flowtests`. "flowtests" is two solutions: `flowtests/capture/solution` (one `Button`) and `flowtests/skipped-body` (one `Button`, the one literal HTTP action). The HTTP counts themselves are right (`alm-makers` 11: 4 literal, 7 expression; one literal each in `flow-limits`, `flow-secrets`, `flowtests/skipped-body`).
- Suggested fix: Recount and list Data row 8 per solution display path (as the goldens show it), for every section and driver, including the drivers above and the R1 and R2 effects; keep the stop rule for anything not listed.
- Resolution: open

#### R4. Done when 5 claims more about names than the plan's redaction does or its tests can prove
- Area: 13 Buildability; redaction and the WATCH note "No output contains 'standard'"
- Finding: Names of flows, actions, triggers, apps, tools, connectors and references are printed as the export spells them, with only `scheme://` URLs replaced (Data row 3). A name holding "Standard", a bare host such as `api.contoso.com`, or an email address is printed, so "No output ... holds the word 'standard'" and "none holds a host ... or an email address" are false for such an export. The tests (Data row 7) prove the claims only for the fixtures and for URL parts the reader never keeps.
- Suggested fix: Either narrow Done when 5 and Constraints to the command's own text (headings, notes, `need` lines, field names, the schema) and the values the reader never keeps, saying that names are printed verbatim apart from URLs and GUIDs (R1), or filter names too. In both cases, add a made-up Data row 7 case with a flow and an action whose names hold "Standard", a bare host and an address, and assert the chosen behaviour.
- Resolution: open

#### R5. `PackedToolTests` asserts 12 MCP tools and is not among the files the change must reach
- Area: 9 Testability; AGENTS.md "Add a command"
- Finding: `tests/PowerOps.Cli.Tests/PackedToolTests.cs` line 120 asserts the packed server lists 12 tools. It runs in a plain `dotnet test` (skipped only with `POWEROPS_SKIP_E2E=1`), so a 13th tool fails it. Step 4 and Data row 7 name `McpCommandTests.cs` but not this file, and do not say where `inventory` goes in `McpCommandTests.ToolNames` and the tool list (the order is asserted and fixed in `mcp-tools.txt`).
- Suggested fix: Add `PackedToolTests.cs` (12 to 13) to step 4's files, and say where `inventory` sits in `McpCommand.Tools` (for example after `analyze`, among the offline tools) and in `ToolNames`.
- Resolution: open

### Low

#### R6. Error output: stderr redaction and the solution path in a flow parse error
- Area: 9 Testability; AGENTS.md "Add a reader" step 4 (errors start with the path)
- Finding: Data row 6 copies `analyze`'s exit codes and notes but does not say that stderr goes through `Redaction.Writer` unless `--show-values`, as `analyze` does. And if the report parses flows (through `RuleHelpers.CloudFlows`) after the shared load loop, a bad flow's message starts with the flow's file, not the solution path that `RunFound` adds around `Analyzer.Run`.
- Suggested fix: In Data row 6, say that stderr is redacted unless `--show-values`, and that an unreadable flow's message starts with the solution path, as in `analyze`; test both in `InventoryCommandTests`.
- Resolution: open

#### R7. Custom pages are not addressed
- Area: 13 Buildability
- Finding: Data row 4 fires `dataverse-in-canvas-app` on "a canvas app, not a code app", and Data row 2 lists any non-code app as a `canvasApp` user. A custom page (`CanvasAppType` 2) lives in a model-driven app and would fire that driver and be named a canvas app.
- Suggested fix: Say how type 2 is treated (for example its own user kind, `customPage`, and no `dataverse-in-canvas-app`, since its app is covered by `model-driven-app`), and add a Data row 7 case.
- Resolution: open

#### R8. Five of the drivers' Learn pages are quoted but not watched
- Area: Ecosystem (upgrade plan section 4, "M107's licence drivers add their pages to sources.json")
- Finding: Data row 9 adds two pages to `docs/ecosystem/sources.json` (license-designation and the Types page). The output also cites the Power Apps licensing FAQs, the Power Platform licensing FAQs, the Power Automate licensing FAQ, the Desktop flows connector reference, the failed licence check page and the HTTP Webhook training unit, which nothing watches.
- Suggested fix: Add them as `manual` sources, or record in the Log why two pages are enough.
- Resolution: open

#### R9. The "Windows and macOS" runbook in `docs/BUILD-PLAN.md` says the server lists 12 tools
- Area: Docs
- Finding: The runbook's steps 4 and 5 tell a person to expect 12 tools in `/mcp`; after M107 there are 13.
- Suggested fix: Add `docs/BUILD-PLAN.md`'s runbook lines to Data row 9, or word them without the count.
- Resolution: open
