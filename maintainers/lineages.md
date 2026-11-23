# Fixture and evaluation lineages

Assign a split to the entire `lineage_id`. A managed zip, an unpack, a mutation, a run capture and a case built on the same solution all inherit that assignment. Each path below includes its tracked descendants. The provenance column describes the artifact at that path. The final tally counts each split unit once.

## Public solution exports

These are MIT-licensed Microsoft sample files. Trimming removed whole files and preserved the contents of the files kept. Source revisions, paths, trim lists and licences are recorded in the [fixture notice](../tests/fixtures/NOTICE.md).

| lineage_id | Tracked fixture | Provenance | Repository, commit and source path |
|---|---|---|---|
| `coe-auditlogs` | `tests/fixtures/coe-auditlogs/` | exported | microsoft/coe-starter-kit @ `d4ceb8b718883d841ef9680dc344494ebd68d3b7`, `CenterofExcellenceAuditLogs/SolutionPackage/src`. Complete. |
| `alm-sample` | `tests/fixtures/alm-sample/` | exported | microsoft/coe-starter-kit @ `d4ceb8b718883d841ef9680dc344494ebd68d3b7`, `ALMAcceleratorSampleSolution/SolutionPackage/src`. Plugin assembly DLL removed. |
| `alm-makers` | `tests/fixtures/alm-makers/` | exported | microsoft/coe-starter-kit @ `d4ceb8b718883d841ef9680dc344494ebd68d3b7`, `ALMAcceleratorForMakers/SolutionPackage`. Kept Other, environment variable definitions and workflows. |
| `alm-makers` | `tests/fixtures/flowtests/alm-makers/` | hand-written | PowerOps M14 (2026-10-26), M78 timeout variant (2026-11-09). Five tests for BuildRequestCancellation and ReadEnvironmentVariable, with fake inputs. |
| `core-portal` | `tests/fixtures/core-portal/` | exported | microsoft/gov-apptemplates @ `64dabc2652a13a2423ee3fb1d6f4bfe8f33fe1d9`, `portals/core-portal/src`. Removed ten web-file bodies. |
| `column-level-security` | `tests/fixtures/zips/ColumnLevelSecuritySampleSolution.zip`, `tests/fixtures/zips/ColumnLevelSecuritySampleSolution_managed.zip` | exported | microsoft/PowerApps-Samples @ `8e42f87026f4c93d7e468f28416f5852375d325a`, `dataverse/webapi/PS/ColumnLevelSecurity/ExportedFiles/`. Two packages of one solution, intact. |
| `polymorphic-lookup` | `tests/fixtures/zips/polymorphiclookupexamplesolution.zip` | exported | microsoft/PowerApps-Samples @ `8e42f87026f4c93d7e468f28416f5852375d325a`, `dataverse/webapi/PS/PolymorphicLookup/`. Intact. |
| `contoso-staging` | `tests/fixtures/zips/Contoso_sample_1_0_0_1_managed.zip` | exported | microsoft/PowerApps-Samples @ `8e42f87026f4c93d7e468f28416f5852375d325a`, `dataverse/orgsvc/CSharp/SolutionStagingAndImport/`. Intact. |

## The PowerOps seed and its derivatives

Every row in this table has lineage_id `powerops-seed`. The source is the maintainer's test-environment export, added on 2026-10-19 in `18f6af2`. Its component IDs identify the same solution across layouts, environments and evaluations. The fixture and case notices document the transformations. [Fixture notice](../tests/fixtures/NOTICE.md) · [Role-case notice](../tests/eval/roles/NOTICE.md)

| Tracked path | Provenance | Origin and transformations |
|---|---|---|
| `tests/fixtures/powerops-seed/` | exported | XML unpacks of clean v1 and deliberately broken v2. Removed canvas binaries and related metadata that cached the environment, renamed the publisher and kept component IDs. The v2 flow has a placeholder SharePoint URL and an unused connection reference. |
| `tests/fixtures/powerops-seed-yaml/` | exported | M5, 2026-10-20. Git integration working trees of the same v1 and v2. The latter adds a canvas button. Kept canvas source YAML, removed generated app internals and binding README, renamed publisher. |
| `tests/fixtures/settings/powerops-seed-v1.valid.json` | exported+hand-modified | M7, 2026-10-20. Generated offline from v1 with pac 2.13.1, then filled in fake values and connection IDs. |
| `tests/fixtures/settings/powerops-seed-v1.problems.json` | hand-written | M7, 2026-10-20. Missing, duplicate and extra settings for v1. |
| `tests/fixtures/drift/` | hand-written | M8, 2026-10-22. Dev and Test snapshots with invented IDs, hashes and values, retaining the seed flow ID. |
| `tests/fixtures/flowtests/seed/` | hand-written | M14, 2026-10-26. Active and default status tests for the v2 flow. Fake IDs and connector data. |
| `tests/fixtures/pac/` top-level files except the suspended response | captured | M7 live, 2026-10-21. pac 2.13.1 org identity and cloud-flow on, off, missing and bad-ID output. Replaced environment, user, host, connection and diagnostic identifiers. |
| `tests/fixtures/pac/list-cloud-flows-suspended.json` | hand-written | M7 live. The on capture with two status fields set to Suspended. |
| `tests/fixtures/pac/drift/capture/` | captured | M8, 2026-10-22. Dev/Test identity and reads 2–7, missing solution, malformed query, clean and edited layers. Replaced IDs consistently, removed OData metadata and layer contents. M36 read 8 on 2026-10-31 kept one scrubbed row from another Dev environment. |
| `tests/fixtures/pac/drift/sample/`, `tests/fixtures/pac/drift/queries/` | hand-written | M8, M36 and M73/M76 (2026-11-09). FetchXML queries, including the malformed and batched probes, and synthetic responses for reads, pagination and errors. Bot, definition and component samples use invented IDs and schema names. The M76 definition columns follow microsoft/power-platform-skills @ `da8ea7a`, `plugins/model-apps/scripts/lib/app-theme.js`. |
| `tests/eval/roles/cases/solution-01` | exported+hand-modified | M38, 2026-11-02. Stale-item escalation. |
| `tests/eval/roles/cases/solution-02` | exported+hand-modified | M38. Intake webhook. |
| `tests/eval/roles/cases/solution-03` | exported+hand-modified | M38. Reassignment notice. |
| `tests/eval/roles/cases/solution-04` | exported+hand-modified | M38. Daily summary mail. |
| `tests/eval/roles/cases/solution-05` | exported+hand-modified | M38. Work-item notes. |
| `tests/eval/roles/cases/solution-06` | exported+hand-modified | M38. Reviewer reminder. |
| `tests/eval/roles/cases/solution-07` | exported+hand-modified | M55, 2026-11-05. Modified column. Seed plus two roles and an app role map in the previous solution. |
| `tests/eval/roles/cases/solution-08` | exported+hand-modified | M55. Triage default. Seed plus two roles and an app role map in the previous solution. |
| `tests/eval/roles/cases/solution-09` | exported+hand-modified | M96, 2026-11-14. Summary screen with a hand-built canvas package, fake host and two YAML sources. |
| `tests/eval/roles/pilot/solution-pilot` | exported+hand-modified | M38 pilot. Status-page ping, one planted flaw. |
| `tests/eval/roles/baseline/` | captured | CLI outputs from each of the nine solution cases and the solution pilot. M38 baseline extended at M55 and M96. Each numbered text file and the pilot text belong to the corresponding case. |

The ten solution cases include their plans, decision logs, previous/current folders, settings and flow tests. Changed components use patterned fake IDs, invented connection IDs, example hosts and mock tokens. The unmodified parts retain seed IDs. Repackaging one of these folders for a later role evaluation preserves its lineage.

## Other PowerOps fixtures

These project fixtures are Apache-2.0. Each synthetic flow in the final six rows has its own scenario. For split assignment they share one conservative group because the later fixtures successively reuse the earlier file layouts. The notice distinguishes observed layouts from tenant exports. [Fixture notice](../tests/fixtures/NOTICE.md)

| lineage_id | Tracked fixture | Provenance | Origin and transformations |
|---|---|---|---|
| `loan-helper` | `tests/fixtures/agents/new-ui/v1.zip`, `v2.zip` | exported | M51, 2026-11-04. Two Studio exports of Loan Helper, differing by one instruction. Replaced publisher, agent and connection identifiers, removed the icon and rebuilt the zips with fixed timestamps. |
| `loan-helper` | `tests/fixtures/agents/new-ui/v1-no-learn-tool.zip` | exported+hand-modified | M51. Offline deletion of the Learn tool folder from v1. |
| `equipment-checkout` | `tests/fixtures/equipment-checkout/` | exported | M60, 2026-11-04 live test. Exported roles, app module and trimmed manifests. Replaced role IDs and publisher, dropped unrelated artifacts including the canvas app. |
| `flow-conformance` | `tests/fixtures/flowtests/capture/solution/` | exported | M58, 2026-11-05. Custom PowerOpsCapture flow imported, run and exported with pac 2.14.1. Publisher renamed and a fake flow ID chosen before import. |
| `flow-conformance` | `tests/fixtures/flowtests/capture/run.json` | captured | M58. One cloud run. Removed headers, timestamps, tracking and subscription/host data, rounded utcNow to the day. The query returned no customer rows. |
| `flow-conformance` | `tests/fixtures/flowtests/capture/expected-unsupported.json` | hand-written | M58. Simulator expectation list for that same captured flow. |
| `synthetic-dataverse-flows` | `tests/fixtures/agents/dataverse-tools/` | hand-written | M74, 2026-11-08. An agent, tools, tables, role and rule in a trial export's layout. Fake names, IDs and connection reference. One table uses the shape of a renamed seed attributes block. |
| `synthetic-dataverse-flows` | `tests/fixtures/flow-navigation/` | hand-written | M79, 2026-11-09. Expense/cost-centre navigation-case errors. Uses agent-tools and ALM Makers layouts with invented data. |
| `synthetic-dataverse-flows` | `tests/fixtures/flowtests/skipped-body/` | hand-written | M86, 2026-11-11. One synthetic flow with three tests for skipped, timed-out and failed actions, using the navigation fixture layout. |
| `synthetic-dataverse-flows` | `tests/fixtures/flow-limits/` | hand-written | M99, 2026-11-12. Recurrence, static result, silent catch and approval timeout in the skipped-body layout. |
| `synthetic-dataverse-flows` | `tests/fixtures/flow-secrets/` | hand-written | M100, 2026-11-12. Three synthetic variables and one flow in the limits layout. No real secret values. |
| `synthetic-dataverse-flows` | `tests/fixtures/flow-credits/` | hand-written | M123, 2026-11-22. Agent-flow and AI Builder examples in the secrets layout with a fake prompt ID. |

## Plan cases and pilot

Each plan directory contains the project's hand-written plan and decisions with invented organisations, users and numbers. M38 cases and pilots were tracked on 2026-11-02. M41 added the two ledger cases on 2026-11-03. The two depot variants have the same goal. For split assignment they also stay with the earlier equipment-loans plan. [Role-case notice](../tests/eval/roles/NOTICE.md) · [M41 plan](../docs/plans/m41-size-brake/plan.md)

| lineage_id | Tracked case | Provenance | Scenario |
|---|---|---|---|
| `fleet-reminders` | `tests/eval/roles/cases/plan-01` | hand-written | Fleet service reminders |
| `supplier-portal` | `tests/eval/roles/cases/plan-02` | hand-written | External supplier registration |
| `starter-onboarding` | `tests/eval/roles/cases/plan-03` | hand-written | New employee checklists |
| `permit-portal` | `tests/eval/roles/cases/plan-04` | hand-written | Building permits |
| `site-inspections` | `tests/eval/roles/cases/plan-05` | hand-written | Tablet inspections |
| `volunteer-shifts` | `tests/eval/roles/cases/plan-06` | hand-written | Food bank shifts |
| `training-registration` | `tests/eval/roles/cases/plan-07` | hand-written | Employee training |
| `complaint-handling` | `tests/eval/roles/cases/plan-08` | hand-written | Customer complaints |
| `equipment-loans` | `tests/eval/roles/cases/plan-09` | hand-written | Camera and laptop loans |
| `invoice-approval` | `tests/eval/roles/cases/plan-10` | hand-written | Capture through finance |
| `asset-requests` | `tests/eval/roles/cases/plan-11` | hand-written | Staff device requests |
| `lab-samples` | `tests/eval/roles/cases/plan-12` | hand-written | Sample tracking |
| `equipment-loans` | `tests/eval/roles/cases/plan-13` | hand-written | M41 oversized depot variant |
| `equipment-loans` | `tests/eval/roles/cases/plan-14` | hand-written | M41 compact depot variant |
| `desk-booking` | `tests/eval/roles/pilot/plan-pilot` | hand-written | Shared desks, one planted flaw |

The hand-written M38 harness `tests/eval/roles/run.sh` and subsequent revisions, the main and pilot `key.json` files, and `tests/eval/roles/NOTICE.md` provide shared evaluation infrastructure. The keys span the cases they score. None adds an independent lineage.

## Sketch calibration and shipped samples

M97 recorded these five plans and their artifacts. Row 1 comes from trial 3. Rows 2–5 share the repairs scenario and visual kit, and rows 4 and 5 are successive versions of one site. Keep all four repairs rows and the sample files together. The gate key was refined by M117/M118 (owner decision 2026-11-16). [Calibration procedure](../tests/sketch-eval/README.md) · [Verdicts and gates](../tests/sketch-eval/key.md) · [Sample notice](../skills/powerops-plan/references/samples/NOTICE.md)

| lineage_id | Tracked row, sketch and map | Provenance | Source |
|---|---|---|---|
| `trial3-supplier-onboarding` | `tests/sketch-eval/row-1`, including `mockups/prototype.html` | captured | Picked trial 3 sketch, 2026-11-11, with invented data. One company name replaced with a fictional name of equal length. No map. M97 supplied the hand-written plan excerpt. |
| `repairs-design-samples` | `tests/sketch-eval/row-2`, `skills/powerops-plan/references/samples/canvas-repairs-desk.html` and `canvas-repairs-desk.map.md` | hand-written | Canvas repairs desk, owner-passed 2026-11-11, revised during M97 calibration. |
| `repairs-design-samples` | `tests/sketch-eval/row-3`, `skills/powerops-plan/references/samples/powerbi-repairs-performance.html` and `powerbi-repairs-performance.map.md` | hand-written | Power BI repairs performance, 2026-11-11 owner-passed sample kept byte-for-byte. |
| `repairs-design-samples` | `tests/sketch-eval/row-4`, including `mockups/prototype.html` and `mockups/prototype.map.md` | hand-written | First Power Pages tenant repairs version, failed by the owner. |
| `repairs-design-samples` | `tests/sketch-eval/row-5`, `skills/powerops-plan/references/samples/pages-tenant-repairs.html` and `pages-tenant-repairs.map.md` | hand-written | Second Power Pages version passed by the owner on 2026-11-11, then revised in M97. |

Each row includes its M97 hand-written `plan.md`. The shared hand-written `skills/powerops-plan/references/samples/kit.css` and its `NOTICE.md` belong with the repairs samples. The notice credits Microsoft's MIT-licensed Fluent System Icons paths. The calibration README and key describe the rows and add no separate sample.

## Split accounting

| Primary provenance of split unit | Independent lineage IDs | Count |
|---|---|---:|
| exported | Seven Microsoft solutions and EquipmentCheckout | 8 |
| captured | M58 cloud conformance and the trial 3 sketch | 2 |
| hand-written | Thirteen plan groups including the desk pilot, one synthetic Dataverse fixture group and one repairs design group | 15 |
| exported+hand-modified | PowerOpsSeed with its ten mutated evaluation cases, and Loan Helper with the removed-tool variant | 2 |
| **Total** | **Distinct lineage IDs across the inventory** | **27** |

The tally classifies each group by its benchmark material. For example, the M58 reference answer is a captured run even though its input is an export. All 27 groups are public. Phase D needs at least eight new independent private `val` lineages for a quality-superiority claim, plus fresh private `sealed` lineages disjoint from both development and validation. Additional runs, mutations or alternate packaging of a listed lineage contribute no new independent unit.
