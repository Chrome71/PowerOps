plan-hash: fe68e2238200d4160762663fa68effb13374da6135ad006bda80bacacb539294
model: claude-sonnet-5-5
fresh-context: yes

Environment facts were not read: the plan names no environment (`environment: null`), changes this repo's own code, and the request did not confirm a `pac` sign-in; `powerops plan facts` and `pac` were not run. Checklist items 1 to 9 and 11 (solution boundaries, connection references, licences, DLP, route, managed state, delegation, security roles, size of tables and flows) do not apply to a change to this repo's code and are not reported. Item 10 applies: `openQuestions` is 0 and nothing stands under `Questions for your admin`. The URL, GUID, email and person-name scan found none in `plan.md` or `decisions.md`. `plan.md` was not edited (`review: pending` stays), because this request allowed no change outside `review.md`.

The review is mostly a check of the plan against the code at `a75673e` (`DeployPlan`, `SolutionDiff`, `FlowDefinition.WalkActions`, `FlowSimulator.Blocks`, `DiffReport`, `JsonOutput`, both schemas, `docs/schemas/README.md`, `docs/COMMANDS.md`, the named tests and the goldens), against the M53 section and the CX-2 and CX-5 sections of the improvement and review notes, and against Microsoft Learn. Checked and found right: the line references of `AddUpgrade` (`:289`, loop at `:328-339`, the other-types item at `:341-350`), `ParseVersion`/`Compare` (`:430-442`), `SameJson` and `WithoutOperationMetadataIds`; that the plan hash is taken over the inputs only, so no item changes it; that `deploy-plan.schema.json` already lists `components-not-checked`; that `SolutionDiff.Compare` has one caller (`DiffCommand`) and `DiffReport`'s Markdown legend picks up a new label through `Risk.Describe`; that the holders the plan lists for flow actions (`actions`, `else.actions`, `default.actions`, `cases.*.actions`) are exactly those of `WalkActions` and `Blocks`; that every one of the 133 `operationMetadataId` keys in the 36 JSON files of the repo that hold one (fixtures and `tests/eval`, counted with a scratch script) sits at `properties.definition.(triggers|actions).<name>[.<holder>.<name>]*.metadata`, so no existing equal pair becomes a change and the eval baselines do not move; that every `tests/eval` case pair has two different versions, so `same-version` never appears in them; that no skill text, `action/` script or `action.yml` names a `diff` risk label or a plan kind, so the pinned roles, `tests/eval`, `PlanChecker` and `plugins/*/skills/` need no change; and that `powerops diff` on the seed pair prints the two lines the plan quotes and exits `1` today. The Learn claims were checked with the Learn search tool: "Upgrade or update a solution" says to increment the version when exporting a managed solution, says an update must have a higher major, minor, build or revision number, says Upgrade deletes components not in the newer version and says Update leaves them; "Solution concepts", section "Solution lifecycle", says "Solution upgrades delete components that existed but are no longer included in the upgraded version". All three match the plan's quotes. One nuance the quotes leave out: the same Learn page says a managed component is removed by an upgrade only if no other component in the target depends on it.

## Findings

### High

No findings.

### Medium

#### R1. Done when 1 and row 6 describe the `deploy-plan-seed` goldens wrongly
- Area: 10 (unanswered areas; acceptance that cannot be met as written)
- Finding: Done when 1 says that in the `deploy-plan-seed.*` goldens `components-not-checked 1 table(s)` "sits beside `deletes 1 table(s)`". The goldens hold no `deletes` table item: their only `deletes` item is a connection reference. The target snapshot of that case holds one table and the seed zip names at least one, so `gone` is 0 and the upgrade gains only the `components-not-checked 1 table(s)` item (text, JSON and Markdown). The pairing with `deletes 1 table(s)` is that of the unit test `A_managed_upgrade_over_the_recorded_seed_target_reports_what_the_snapshot_holds` (`:835`, which sets `RootComponents = []`), not of the CLI goldens. An implementer who writes the golden to the plan, or who reads the diff against "only these lines", will be misled. Row 6 also says `deploy-plan-blocked.json` and `deploy-plan-same-version.json` change "if" their solutions are managed upgrades over a table or canvas app; they do not (the same-version case is a `skip`, and the blocked case's seed zip is an install over the default fake target), so both stay as they are.
- Suggested fix: state in Done when 1 that the golden gains one `components-not-checked 1 table(s)` item and no `deletes` table item, keep the `deletes` pairing for the unit test only, and in row 6 say that the other two deploy-plan goldens do not change.
- Resolution: Fixed. Done when 1 says the seed goldens gain only the information item, and keeps the pairing with `deletes` for the unit test; row 6 says the blocked and same-version goldens don't change.

#### R2. The list of existing tests that change under (c) is incomplete
- Area: 9 (testability; every place the change must reach)
- Finding: Row 6 names `DiffCommandTests:64` and says "any other test whose expected output is a diff at one version" is updated, but names none. Concretely, with `same-version` on every change at equal versions: `DiffCommandTests` asserts exact text lines with a risk (`:90`, `:199`, `:227`, `:236`, `:237`) and an exact risks array (`:232`, `["agent-instructions"]`) over zips of one version; `DiffReportTests:116` asserts `[risk: connection-reference]` on a pair headed `1.0.0.0 → 1.0.0.0`; and `SolutionDiffTests` builds almost every pair from `B.Empty()` at one version, so most of its expectations that list a change (about 97 asserts in the file) gain `same-version` or a `(same-version)` suffix. Those that must stay free of the label need a version change in the pair, which is a test-design decision the plan leaves open. The build will find these by running the tests, but the plan's own check ("`git diff tests/golden` shows only these lines") does not cover them.
- Suggested fix: list `DiffCommandTests`, `DiffReportTests` and `SolutionDiffTests` in row 6 and say how `SolutionDiffTests` keeps its existing cases (for example the `Diff` helper gives the new side another version where the case is not about the label, and the label's own tests use an explicit equal version), so the diff of those tests stays reviewable.
- Resolution: Fixed. Row 6 names `SolutionDiffTests`, `DiffCommandTests` (with its lines) and `DiffReportTests`. The helpers stay as they are, and expectations gain `same-version` after their other risks (decisions Round 2).

### Low

#### R3. The "hash is equal with and without the item" test cannot fail
- Area: 9 (testability)
- Finding: Row 1 asks for a test that "the item does not block and the hash is equal with and without it". `Build` computes the hash from the inputs (zip bytes, snapshot, dependencies) before and apart from the items, so there is no "without" run to compare; as written the hash half of the test passes whatever the code does.
- Suggested fix: keep the severity-`info` and not-blocked assertions, and for the hash assert that two targets whose snapshots give different item counts but hash the same inputs are not needed; or drop the hash half and rely on the unchanged `deploy-plan-seed` golden hash (scrubbed) and the existing hash-input tests.
- Resolution: Fixed. Row 1's test asserts severity `info` and that the plan doesn't block; the hash half is dropped.

#### R4. `EqualVersions` is not specified for text that is not four numbers
- Area: 9 (edge cases)
- Finding: "Equal means equal text, or four numeric parts each, equal as numbers". Two versions that are equal text but not four numbers (for example a three-part `1.0.0`, or an empty string) therefore count as equal and carry `same-version`, although `deploy plan` throws (exit 3) for such a zip rather than skipping it, so the label's text "deploy plan skips it" is wrong for them. The loader may or may not allow such a version; the plan does not say. The other direction is fine: one side numeric and the other not is "not equal".
- Suggested fix: either define equality as "both parse to four numbers and are equal as numbers" (equal non-numeric text gets no label, which keeps the label true), or add one test and one sentence saying what is expected for equal non-numeric text.
- Resolution: Fixed. Equal versions now means both are four numeric parts and equal as numbers, with a test for equal `1.0.0` (rows 4, decisions Q5 and Round 2).

#### R5. The wording of the `same-version` description is ambiguous
- Area: 9 (testability of the user-facing text)
- Finding: "The two versions differ but carry the same version number" can be read as a contradiction (two versions that differ). The solutions' contents differ and the version number does not. The text appears in the Markdown legend and in `diff.schema.json`.
- Suggested fix: reword to "The two solutions differ but carry the same version number, so importing the new one is not an upgrade: deploy plan skips it; raise the version."
- Resolution: Fixed with the suggested wording (row 4).

#### R6. Recursion depth and the stripped-versus-compared contract are not stated for the new walk
- Area: 9 (edge cases)
- Finding: The new walk follows `actions` holders "to any depth". It is bounded in practice by the parser's own depth limit (a deeper document throws `JsonException` and is compared as raw text, which the plan already says), but the plan does not say so, and a reader of the code cannot tell whether an unbounded recursion was meant. Also, the plan keeps comparing `operationMetadataId` in a flow whose JSON does not have the `properties.definition` layout (said), which turns a re-saved flow of another layout into a false "definition changed"; that is the safe direction (never a false "no changes"), and it is worth one sentence in `docs/COMMANDS.md` so the reader knows what is stripped and what is not.
- Suggested fix: add a sentence to row 3 that depth is bounded by the parser limit, and keep the `docs/COMMANDS.md` sentence of row 5 to say that a flow of any other layout is compared with the key.
- Resolution: Fixed. Row 3 states the parser's depth bound and the other-layout case; row 5 puts the latter in `docs/COMMANDS.md`.

#### R7. The item for a non-upgrade action deviates from the source milestone's wording and is recorded only in decisions
- Area: 10 (unanswered areas)
- Finding: The source milestone says to add the item "whenever the target solution holds any table or canvas app" and its acceptance lists "a non-upgrade action" among the cases that "give the expected coverage item". The plan gives no item to an install, a skip or an unmanaged zip's upgrade (decisions Q2, with a sound reason: nothing can be deleted by them, and an item there would claim an unchecked deletion that cannot happen). The reasoning is right, but the Goal and Done when never say that this departs from the source's wording, so a reader who checks the plan against the milestone sees a missing case.
- Suggested fix: add one sentence to the Goal or the Won't build list: "A skip, an install and an unmanaged zip's upgrade get no item (nothing is deleted); the source milestone's 'non-upgrade action' case is tested as the absence of the item."
- Resolution: Fixed. The Goal states the deviation and how the case is tested.

#### R8. `same-version` is attached to every row, which will teach readers to ignore `risks` for teams that version at build time
- Area: 10 (design choice, decisions Q4)
- Finding: Q4 chose option 1 knowingly and records the cost. The choice is consistent with how risks live on changes today and works for a consumer that reads only `changes[].risks`, so it is a defensible choice, not a gap in the build. The remaining risk is that for a team that never bumps the version in source, every row of every PR comment carries the label, and the real per-change risks (`removed-component`, `agent-tool`) sit beside a label that is always there. The Markdown legend explains it once, which limits the harm.
- Suggested fix: no change to the build. Optionally, say in `docs/COMMANDS.md` that the label is a property of the pair, not of the row, and that a consumer may read it from any row.
- Resolution: Accepted as Q4 chose. Row 5 adds the suggested `docs/COMMANDS.md` sentence.
