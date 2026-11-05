---
status: signed-off
review: done
signed-off: 2026-11-04
signed-off-hash: ea6ee94facc18fd7fbe8253a26b9e8e0b8f7d43d0b804fe976aaada077fb3b19
environment: null
depth: quick
supersedes: null
---

# M53: `deploy plan` names the deletions it can't check; two `diff` correctness fixes

## Goal

Close three gaps the live test of 2026-11-04 found, kept outside the repo because it quotes a private test run. This is M53 of the improvement plan written after that test. The plan splits it into three separable items:

- (a) CX-2: `deploy plan` compares a managed upgrade's tables and canvas apps by count only. When table A is replaced by table B, the count stays equal and the plan says nothing, though the upgrade deletes A and its data.
- (b) CX-5: `diff` removes every `operationMetadataId` key at any depth of a flow's JSON. A Compose input holding that key is real data, and a change to it shows as "no changes".
- (c) B-4 (visibility): three exports at one version held different flows. `diff` says nothing about it, and `deploy plan` would `skip` such a zip.

The milestone asks for the item "whenever the target solution holds any table or canvas app" and lists "a non-upgrade action" among its test cases. This plan gives the item only to a managed zip's upgrade, because an install, a skip and an unmanaged zip's upgrade delete nothing (decisions Q2). The non-upgrade case is tested as the absence of the item.

Done when:

1. `deploy plan` adds one `components-not-checked` information item for each of the two types (tables, canvas apps) that the target's solution holds, for a managed zip's upgrade. The item names how many the target holds and says that their deletion is checked by count only. The `deletes` lower bound stays as it is. The `deploy-plan-seed.*` goldens gain one `components-not-checked 1 table(s)` item and no table `deletes` item, because the seed zip names as many tables as the target holds; the exit code and the plan hash don't change. The unit test over the same recorded target with no root components gives both `components-not-checked 1 table(s)` and `deletes 1 table(s)`.
2. `diff` ignores `operationMetadataId` only in the `metadata` object of an action or a trigger of `properties.definition`, nested actions included. Anywhere else the key is compared. `SolutionDiffTests`: a changed `operationMetadataId` inside a Compose input is a change; one under an action's `metadata`, at any nesting, is not.
3. `diff` puts the risk `same-version` on every change when the two solutions have the same unique name, carry equal versions and differ. `powerops diff tests/fixtures/powerops-seed/v1 tests/fixtures/powerops-seed/v2` prints `[risk: connection-reference, same-version]` on the added connection reference and `[risk: same-version]` on the changed flow, and exits `1`. `same-version` does not appear for identical inputs, for inputs whose version changed, or for two different unique names. The `diff` JSON is at `schemaVersion` 3.
4. `DeployPlanTests`: a same-count replacement, a lower count, a target with no tables, and each non-upgrade action (install, skip, the upgrade of an unmanaged zip) give the expected `components-not-checked` items, checked apart from the `deletes` items.
5. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan are clean, and the PR is green on all eight checks.

### Won't build

- Matching tables and canvas apps by identity in `deploy plan`. It would need a new read of the target's schema names, and the improvement plan sizes M53 as S. The count stays a lower bound, and the new item says so (decisions Q1).
- A policy option that refuses an upgrade whose deletion check is incomplete (the second half of CX-2's suggested fix). The item is information; whether to refuse stays the reader's call (decisions Q2).
- A `same-version` item in `deploy plan`. It already has `same-version-differs` (M37), and `diff` is where the improvement plan puts the risk.
- A root-level `risks` field in `diff`, or a pseudo-change for the version (decisions Q4).
- Ignoring any other designer-written key in a flow (decisions Q3).
- A `docs/BUILD-PLAN.md` section or numbered decision (decisions Q7).

## Users

- A reviewer who reads a `deploy plan` or `diff` comment on a pull request. They must see that table deletions were checked by count only, and that a change ships at an unchanged version.
- A pipeline or agent that reads the JSON of either command. It needs `components-not-checked` to be present whenever tables or canvas apps go unchecked, and `same-version` in a change's `risks`.

## Data

Line numbers are those of `a75673e`.

| # | Severity | Change | Where | Test |
|---|---|---|---|---|
| 1 | high | (a) In `AddUpgrade`, after the existing `if (!solution.Managed) return;`, the table and canvas-app loop counts the target's components of the type (`held`). When `held > 0` it adds a `components-not-checked` item with subject `<held> table(s)` or `<held> canvas app(s)` and the message `The target solution holds <held> table(s); the zip names them by schema name and the target by id, so their deletion is checked by count only and one replaced under another name is not seen.` (`canvas app(s)` for the other type). The `deletes` item (`gone > 0`) stays unchanged. The `components-not-checked` item for other types (`:341-350`) stays unchanged too, so a plan can hold up to three items of the kind, sorted by subject as every item is. Severity `info`: nothing blocks and the hash does not change. Install, skip and an unmanaged zip's upgrade get none of these items, because `AddUpgrade` runs only for an upgrade and returns before the deletion checks for an unmanaged zip (decisions Q2) | `src/PowerOps.Core/Deploy/DeployPlan.cs:328-339` | `DeployPlanTests`, each case checked apart from the `deletes` items: a same-count replacement (2 tables in the target, 2 other tables in the zip) gives `2 table(s)` and no `deletes`; a lower count (3 and 1) gives `3 table(s)` and `deletes 2 table(s)`; a target with canvas apps and no tables gives only `1 canvas app(s)`; a target with neither gives none; a skip at the same and at a lower version, an install, and an unmanaged zip's upgrade give none; the item has severity `info` and the plan does not block (the hash is taken over the inputs only, and the existing hash tests cover it) |
| 2 | medium | Existing `DeployPlanTests` rewritten: `Equal_or_lower_counts_of_tables_and_canvas_apps_are_quiet` (`:361-368`) becomes a test that equal or lower counts give no `deletes` item and do give the information item; `Components_outside_the_checked_set_give_an_info_item_with_their_count_and_types` (`:372-384`) picks its item by subject `2`; `The_checked_set_alone_and_the_other_actions_give_no_components_not_checked` (`:386-398`) asserts that the checked set gives only the table and canvas-app items; `A_managed_upgrade_over_the_recorded_seed_target_reports_what_the_snapshot_holds` (`:835`) asserts `components-not-checked 1 table(s)` | `tests/PowerOps.Core.Tests/Deploy/DeployPlanTests.cs` | the tests themselves |
| 3 | high | (b) `SameJson` no longer drops the key at every depth. In each parsed side, when the root is an object whose `properties` is an object whose `definition` is an object, it removes `operationMetadataId` from `metadata` (when that is an object) of every object-valued entry of `definition.triggers` and `definition.actions`. For every action, it does the same through the holders that `FlowDefinition.WalkActions` and `FlowSimulator.Blocks` walk: `actions`, `else.actions`, `default.actions` and `cases.*.actions`, to any depth. Keys match ordinally, as JSON keys do. A step whose value is not an object is skipped. The key is compared everywhere else: in `inputs`, in any other property of an action or trigger, in `parameters` and `connectionReferences`, at the root, and in a definition not under `properties.definition`. A text that doesn't parse, or that throws on reading, is still compared as raw text. The walk's depth is bounded by the parser's own limit (`JsonNode.Parse`, 64 levels by default): a deeper text throws `JsonException` and is compared as raw text, as today. A flow whose JSON has another layout keeps the key and is compared with it, which can only show a change, never hide one | `src/PowerOps.Core/Diff/SolutionDiff.cs:231-276` | `SolutionDiffTests`: an `operationMetadataId` changed inside a Compose action's `inputs` object is `definition changed`; inside a trigger's `inputs` too; at the root, and directly under `definition`, too; one changed under the `metadata` of an action nested in a Scope's `actions`, a Condition's `else`, a Switch's `cases` and `default`, and of a trigger, is no change. The existing `Regenerated_operationMetadataIds_in_the_flow_definition_are_not_a_change` (`:162`) stays green. The lone-surrogate test (`:192`) keeps its result, with its comment reworded |
| 4 | high | (c) `Risk.SameVersion = "same-version"`, described as `The two solutions differ but carry the same version number, so importing the new one is not an upgrade: deploy plan skips it; raise the version.` `Compare` appends it to every change's `Risks`, after the risks it already has, when at least one change exists, the unique names are equal ignoring case (as `deploy plan` matches a zip to its target) and the versions are equal. Equal means that both are four numeric parts and they are equal as numbers, as `deploy plan` compares them; two versions that are not four numeric parts are not equal, even as equal text, because `deploy plan` refuses such a zip rather than skipping it. A new `internal static bool DeployPlan.EqualVersions(string?, string?)` holds that test, using the existing `ParseVersion` and `Compare`. So a version written `1.0.0.0` and `1.0.0.00` gives the `version` change, which carries `same-version` | `src/PowerOps.Core/Diff/SolutionDiff.cs:22-42,104`, `src/PowerOps.Core/Deploy/DeployPlan.cs:430-442` | `SolutionDiffTests`: an added flow at equal versions carries `same-version` after its other risks; identical solutions give no change; a changed version (`1.0.0.0` → `1.0.0.1`) with an added flow gives no `same-version`; two unique names at one version give none; `1.0.0.0` → `1.0.0.00` gives the version change with `same-version`; the unique-name match ignores case; two equal versions that are not four numeric parts (`1.0.0`) give no `same-version`. Existing assertions over solutions at one version gain `same-version` (row 6) |
| 5 | medium | Schema and docs. `docs/schemas/diff.schema.json`: `schemaVersion` const 3; `same-version` added to the `risks` enum and its description. `JsonOutput.DiffSchemaVersion = 3`, with its comment naming M53. `docs/schemas/README.md`: `diff` is at 3, plus a `diff from 2 to 3` paragraph (one risk label added; nothing removed or renamed). `docs/COMMANDS.md` `diff`: the new label, and that it belongs to the pair, so it is on every change of a same-version diff and a consumer may read it from any one; that `operationMetadataId` is ignored only in an action's or trigger's `metadata` under `properties.definition`, and is compared in a flow of any other layout; and version 3 in place of 2. `docs/COMMANDS.md` `deploy plan`: the new information item, next to the count sentence. `deploy-plan.schema.json` doesn't change, because the kind is already in its enum | `docs/schemas/diff.schema.json`, `src/PowerOps.Core/Output/JsonOutput.cs`, `docs/schemas/README.md`, `docs/COMMANDS.md:49,83` | `OutputSchemaTests`, `DocsDriftTests`; `DiffCommandTests` (`:64` asserts `"schemaVersion": 3`) |
| 6 | medium | Goldens by the recipe "Update goldens". `deploy-plan-seed.*` gain the `components-not-checked 1 table(s)` item and nothing else. `deploy-plan-blocked.json` doesn't change (its one upgrade, `Conflict`, has a target snapshot with no table or canvas app), and neither does `deploy-plan-same-version.json` (a skip). `diff-seed.*` and `diff-agents.*` gain `same-version` on every change (both pairs are at `1.0.0.0`) and `schemaVersion` 3. Existing tests whose expected output is a change between two solutions at one version gain `same-version` after their other risks: `SolutionDiffTests` (almost every pair is built from `B.Empty()` at `1.0.0.0`), `DiffCommandTests` (`:64` asserts `"schemaVersion": 3`; the risk lines and the risks array at `:90`, `:199`, `:227`, `:232`, `:236`, `:237`) and `DiffReportTests` (`:116`). Their helpers don't change, so every expectation still shows the real output, and the label's own tests (row 4) state their versions | `tests/golden/`, `tests/PowerOps.Core.Tests/`, `tests/PowerOps.Cli.Tests/` | `git diff tests/golden` shows only these lines; `git diff tests` adds `same-version` to existing expectations and changes nothing else in them |

## Automation

None.

## Screens

None.

## Environments and route

None. The build reads and writes no tenant. The tests use the fake target reads and fixtures already in the repo.

## Solution and publisher

None.

## Constraints

- AGENTS.md: smallest change, new logic with tests, no new write, Conventional Commits, no version or changelog edits, and no tenant URLs, ids, emails or user names.
- The pinned roles `powerops-verify` and `powerops-review-security`, `tests/eval`, `PlanChecker` and the skill copies under `plugins/` are untouched. M55 changes them in parallel. No skill text names a `diff` risk label or a plan kind's subject.
- Platforms are Windows and Linux.
- `diff`'s `risks` list is closed, so a new value bumps `schemaVersion` (`docs/schemas/README.md`, Versioning). The PR title is `feat!:`, as M49's was for version 2.

## Assumptions

- Microsoft Learn, "Upgrade or update a solution" (read 2026-11-04 with the Learn MCP tools): "Increment the version number when you export the solution as a managed solution"; "An update must have a higher major, minor, build, or revision number than the parent solution"; and an upgrade deletes "Any components associated to the previous solution version that aren't in the newer solution version". The Update action leaves them in place.
- Microsoft Learn, "Solution concepts", section "Solution lifecycle" (read 2026-11-04): "Solution upgrades delete components that existed but are no longer included in the upgraded version."
- `operationMetadataId` is not on Learn. In every flow JSON under `tests/fixtures`, it sits only under `properties.definition.triggers.*.metadata` and `properties.definition.actions.*.metadata`, through nested `actions` up to five levels. That was counted with a scratch script on 2026-11-04; no `else`, `cases` or `default` nesting occurs in the fixtures. The code already walks those holders as the nesting of a flow (`FlowDefinition.WalkActions`, `FlowSimulator.Blocks`).
- `deploy plan` gives `skip` to a zip whose version equals the installed one, compared as four numbers (`DeployPlan.Prepare`, `Equal_numeric_versions_in_another_spelling_count_as_the_same_version`).

## Open questions

## Questions for your admin

## Steps

1. This plan and `decisions.md`, then a fresh-context review into `review.md`. Fix and review again until no High or Medium finding is open. Then sign off under the delegation of 2026-10-22.
2. Rows 1 and 2, test first.
3. Row 3, test first.
4. Row 4, test first, then rows 5 and 6 by the recipe "Update goldens".
5. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan; then push, open the PR and watch CI.
