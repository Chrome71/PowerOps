plan-hash: 98696d4bb5249435d44352044ed919dcece2fb0e8c1a61ac3c0c317dcab4e6d1
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: tenant: offline, and the plan's `environment:` is null, so `powerops plan facts` was not run. Items 1 (overlap with tenant solutions), 4 (DLP), 5 to 8 (route, managed state, delegation, security roles) don't apply: the plan changes this repo's offline `inventory` command and adds a fixture that is never imported. Tools checked: `powerops`, `dotnet`, `git` and `bash` found by `command -v`; `fable-reviewer` and `powerops-researcher` are in the agent list; `reports/leakscan.sh` exists in the main checkout, as Assumptions says. The test command of Data row 4 (`dotnet test --project tests/PowerOps.Core.Tests --filter-class "*InventoryReportTests"`) was run on the existing build: 37 tests ran and passed, so the filter selects tests. Checked in the repo and found as the plan states: `Workflow`'s last optional parameter, `ReadWorkflow` and the YAML reader's call at line 72, the YAML fixture's `ModernFlowType: 0`, `RuleHelpers.CloudFlows` (Category 5 with a definition), the five drivers and their order, the closed-set test and `Everything()`, `Json_has_schema_version_1_...`, the schema's `const` 1 and driver `enum`, `docs/schemas/README.md`'s Versioning rule, `JsonOutput.DriftSchemaVersion`, the golden totals (22 solutions, 76 warnings, `schemaVersion` 1), the golden line forms row 6 predicts, the discovery order, the free fixture ids, `EcosystemDocsTests`' statuses, decision 69 and the `sources.json` ids.

## Findings

### High

No findings.

### Medium

No findings.

### Low

#### R1. The plan holds URLs and GUID-shaped ids
- Area: tenant values and personal data (the rule after item 13)
- Finding: Data rows 2 and 5 and the Assumptions hold URLs (Learn pages and a made-up host) and GUID-shaped made-up ids (`<guid>`). All are public pages or placeholders and the rows carry `plan-status: allow`, so no tenant value is exposed, but the review rule reports every URL and GUID.
- Suggested fix: keep them if the person accepts that they are public Learn pages and made-up ids, or name the pages by title in the plan and leave the URLs to the code and `sources.json`.
- Resolution: accepted: the URLs are public Learn pages and a made-up `contoso` host, the ids made-up `00000000-…` values, each line marked `plan-status: allow`; the code and `sources.json` need the URLs anyway.

#### R2. Data row 9 is split across two steps
- Area: 9 (testability of steps)
- Finding: Step 2 says it builds rows 1 to 4 and 9, but its file list leaves out `docs/schemas/README.md` and `docs/COMMANDS.md`, which row 9 changes and which are listed under step 3. With one writer per set of files, an executor of step 2 may skip row 9's doc lines and one of step 3 may not know they are its job. Row 9 also says "the commit carrying this is `feat(inventory)!:`" without saying which step's commit that is.
- Suggested fix: say in step 3 that it carries row 9's lines in `docs/schemas/README.md` and `docs/COMMANDS.md` (or add both files to step 2), and name the commit that is `feat(inventory)!:`.
- Resolution: accepted: the plan text stands, and the build follows this reading (logged): step 2 writes row 9's code, schema and test parts and its commit is the `feat(inventory)!:` one; step 3 writes row 9's lines in `docs/schemas/README.md` and `docs/COMMANDS.md`.

#### R3. `custom-connector` still names Power Automate Premium for an agent flow
- Area: 3 (licence impact)
- Finding: `custom-connector` fires on the solution's custom connectors, not on a flow, and its need says "a flow needs Power Automate Premium or a Process licence". A custom connector used only by an agent flow still gets that sentence, while row 8's decision text says "an agent flow fires no Power Automate driver". The plan's Won't build doesn't name this; only the Fable notes in the `decisions.md` Log do.
- Suggested fix: add the case to Won't build (deferred, with the ECO-0028 parts), and word the decision and the `docs/COMMANDS.md` sentence as "an agent flow's actions fire neither `http-action` nor `desktop-flow-call`".
- Resolution: accepted: deferred with ECO-0028's other parts; the build words the BUILD-PLAN decision and the `docs/COMMANDS.md` sentence as "an agent flow's actions fire neither `http-action` nor `desktop-flow-call`", and ECO-0028's `Why` names the `custom-connector` sentence as deferred.

#### R4. The scope of row 3's `notRead` line is not stated
- Area: 13 (buildability)
- Finding: Row 2 ties `agent-flow` to `RuleHelpers.CloudFlows`, which takes only Category 5 workflows with a definition. Row 3 doesn't say whether its `notRead` line for a `ModernFlowType` other than 0, 1 or null covers only those flows. A4 says no export with value 2 was seen, so its Category is unproven: such a workflow with another Category, or with no definition file, would get no line and no driver, and the report would be silent about it.
- Suggested fix: state in row 3 that the check runs over `RuleHelpers.CloudFlows` (or over every workflow), and add to A4 that the Category of a value-2 flow is unproven.
- Resolution: accepted: the build runs row 3's check over `RuleHelpers.CloudFlows` (Category 5 with a definition), the same set every driver reads; that a value-2 flow is Category 5 is unproven, and the hand-over names it as not verified.

#### R5. The fixture is hand-written, while ECO-0028 asks for an exported agent flow
- Area: 9 (testability)
- Finding: ECO-0028's `Why` ends "Both need a fixture: an exported agent flow and an AI Builder action". The plan's `flow-credits` is hand-written in the shape of one third-party export (A2), never exported by the project, and row 8's addition to `Why` doesn't say so. The risk is low, because the driver keys on `ModernFlowType`, not on the trigger shape.
- Suggested fix: add to row 8's sentence that the fixture is hand-written in the shape of the Power-CAT export, and that a fixture from the project's own export stays open with the deferred parts.
- Resolution: accepted: ECO-0028's `Why` says the fixture is hand-written in the shape of the Power-CAT kit's export, and that a fixture from an export of the project's own stays open with the deferred parts.
