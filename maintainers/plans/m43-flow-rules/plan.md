---
status: done
review: done
signed-off: 2026-11-03
signed-off-hash: 99bb61ce6fd2e1523d914250329f1ec486c94a12ad8a6b5553e6d5655ed3519c
environment: null
depth: quick
supersedes: null
---

# M43: Flow rules

## Goal

Warn in `analyze` when a cloud flow is big or deeply nested, and when it runs a loop inside a loop. This is M43 of Phase 7 ([plan](../phase7-power-platform/plan.md), build-order item 78, release label v0.26.0). Decision 42 (D3) adopts 50 actions and 4 levels as a warning, with the provenance on the rule page; decision 40 (D1) settles what happens if a rule fires on an eval case.

Done when:

- PO016, warning, fires once per cloud flow with more than 50 actions or more than 4 levels of nesting, and its message carries both counts.
- PO017, warning, fires once per `Foreach` or `Until` that runs inside another `Foreach` or `Until`, at any depth, and names both loops.
- Each rule has a firing and a quiet test in `WarningRuleTests`, is registered in `Analyzer.Rules`, and has a page with a dated Sources line and a row in `docs/rules/README.md`.
- The PO016 page cites Learn's Scopes page for the risk and the 8-level limit and Learn's Limits page for the 500, says that 50 and 4 are the CAT OverFlow skill's review heuristic (MIT) at a pinned commit and not a product limit, and defines how actions and depth are counted.
- The goldens change only as the Data section says, updated by the recipe with the diff read.
- `RoleEvalTests` passes before and after, with no eval case, key or baseline file changed, and `powerops-verify` and `powerops-review-security` keep their blob ids (`69208a2ed044a346bbae9877c8cd74d9e4794df9` and `6a6a6a1fdb71eb16d09cf3ed99406d04a0982a1f`).
- `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan are clean, and one `feat:` PR is open.

### Won't build

- An Error at Learn's hard limits (500 actions, 8 levels): they are the platform's own limits, and a flow over 8 levels doesn't save or run.
- A rule for a missing trigger condition, a direct connection, an own table or a Catch that never fails the run (the Phase 7 plan's "Deliberately doesn't" and "Won't build").
- A size or depth number in `--json` or anywhere but the message, a setting to change 50 or 4, or a count report: the message is enough to baseline on and to see the size.
- A new fixture: six existing fixture flows already cross the PO016 line and one has a nested loop.

## Users

- A maker or agent who runs `analyze` on an export and wants to know which flows to split before review.
- CI: the Action runs `analyze` with `--fail-on error` by default, so two new warnings change no exit code there. A job set to `fail-on: warning` with a baseline, as the examples README suggests for accepted findings, fails on the new findings until they are baselined or fixed.
- A team that baselines today's findings: a PO016 finding re-raises when a flow's size or depth changes, because the message holds both counts, and so does a dismissed code-scanning alert, whose fingerprint uses the same key. The PO016 page says so.

## Data

Line numbers are those of `3597789`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | `FlowDefinition` gains `ActionCount`, `Depth` and `NestedLoops` (a list of `NestedLoop(Outer, Inner)`, action names). `Parse` fills them in one walk of `definition.actions`: every entry is one action; an action's `actions`, `else.actions`, `default.actions` and each `cases.*.actions` hold the next level, whatever the action's type. `Depth` is the number of containers around the deepest action: a top-level action is 0, an action in a Scope is 1, and an action inside a Switch case is one level below the Switch, as an action in a Condition branch is one below the Condition (decisions Q2). The trigger isn't an action. A `Foreach` or `Until` (type compared ignoring case) inside another, at any depth, adds one `NestedLoop` naming its nearest enclosing loop. Every entry under an `actions` object counts; a holder or `actions` value that isn't an object is skipped, so an odd shape that parses today still parses and nothing new throws. The walk reads the document `Parse` already holds, so its depth and size limits apply | `src/PowerOps.Core/Flows/FlowDefinition.cs:15-21,66,117` | rows 4, 5 and 10 |
| 2 | `B.Flow` gains an optional `actions` dictionary that the generated actions are added to, so a test can build nested shapes | `tests/PowerOps.Core.Tests/Rules/Builders.cs:13-40` | rows 4 and 5 |
| 3 | `PO016FlowSizeOrDepth` and `PO017NestedLoop`, both `Warning`, over `RuleHelpers.CloudFlows`, registered in `Analyzer.Rules`. PO016: title `Cloud flow has more than 50 actions or more than 4 levels of nesting`; fix `Move self-contained parts of the flow into child flows, and reduce nested scopes, conditions and loops.`; message `Flow '<name>' has <n> actions and a nesting depth of <d>.`; component the flow's name. PO017: title `Cloud flow runs a loop inside a loop`; fix `Replace the inner loop with one query, filter or bulk action over the whole set, or fetch the related rows with the outer query's expand option and use one loop.`; message `Flow '<name>' runs loop '<inner>' inside loop '<outer>'.`; component the inner loop's name (decisions Q3) | new `src/PowerOps.Core/Rules/PO016FlowSizeOrDepth.cs`, `PO017NestedLoop.cs`; `src/PowerOps.Core/Rules/Analyzer.cs:23` | rows 4 and 5 |
| 4 | PO016 tests: fires at 51 flat actions (depth 0) and at depth 5 (five nested Scopes around one action), each message with both counts; fires at 53 actions spread over a Condition's two branches and a Switch's two cases and default, with depth 1 in the message (so every branch counts and a Switch case adds one level, not two); quiet at exactly 50 actions and depth 4; the trigger isn't counted | `tests/PowerOps.Core.Tests/Rules/WarningRuleTests.cs` | itself |
| 5 | PO017 tests: fires for each pair of `Foreach` and `Until`, and through a Condition between the loops, with the inner loop as component and both names in the message; three loops deep gives two findings; quiet for two sibling loops and for a loop holding only a Scope | same file | itself |
| 6 | `docs/rules/PO016.md` and `PO017.md` in the recipe's sections. PO016: what counts as an action and as a level, with an example that includes a Switch case; Learn's Scopes page says flows with many actions or deeply nested scopes "might experience slower performance", and gives the hard limit of 8 levels, which counts scopes, conditions, switch cases and loops; Learn's Limits page gives 500 actions, says a flow with many actions can be slow to edit even below 500, and says to use child flows; 50 and 4 are the review heuristic of Microsoft's CAT OverFlow skill (MIT) at `f898c77`, which walks the same four holders, states no reason and doesn't define depth, so the counting is this rule's own reading; not a product limit; and its Suppress section adds that a baselined finding comes back when either count changes. PO017: Learn's anti-patterns page says nested For each loops multiply iterations (10 by 10 is 100) and to use OData expansion and one loop; Learn names For each, and the rule also counts Until, which repeats its actions too (60 times by default, Limits page); expansion is the case Learn documents, and other nested loops may need a filter, a bulk action or a reason to keep them. The OverFlow file at its commit is on PO016's Sources line. Sources lines verified on the day the page is written | new `docs/rules/PO016.md`, `PO017.md` | `RuleDocsTests` |
| 7 | Two rows in the rule table | `docs/rules/README.md:21` | `DocsDriftTests` |
| 8 | Goldens by "Update goldens": `rules.txt` and `rules.json` gain both rules; `analyze-fixtures.sarif` gains both descriptors; `analyze-fixtures.txt`, `.json`, `.md` and `.sarif` gain six PO016 results (alm-makers `CreateEnvironment` 28/5, `CreateProject` 76/4, `DeployToProd` 109/5, `DeployToStage` 67/4, `WorkflowCompleteNotification` 72/4; coe-auditlogs `DeprecatedChildAdminSyncLogs` 36/5) and one PO017 result (`DeprecatedChildAdminSyncLogs`), and the summary's warning count grows by seven. Nothing else changes | `tests/golden/` | `ReportTests`, `RulesCommandTests`, `OutputSchemaTests` |
| 9 | `docs/BUILD-PLAN.md`: M43 marked built in the Phase 7 table and item 78, with the plan link, and an M43 line in the verification record after M42's | `docs/BUILD-PLAN.md:146,524,541` | none, docs only |
| 10 | `FlowDefinitionTests`: the coe-auditlogs child flow gives 36 actions, depth 5 and the one loop pair; a flow with no `actions` gives 0, 0 and no pair; odd shapes (`actions` as an array or null, an entry that isn't an object, `else` as a string, `cases` as an array) are counted without throwing; a lower-case `foreach` or `until` is a loop | `tests/PowerOps.Core.Tests/FlowDefinitionTests.cs` | itself |

The counts in row 8 were measured before the build with a script that applies row 1's definition to the 30 flow files under `tests/fixtures` (28 under `Workflows/`, and 2 under the YAML layout's `modernflows/` with one and two actions). The same script over the 21 eval flow files (18 in the cases, 3 in the pilot) found none over 4 actions or depth 1 and no nested loop. The `36/5` of `DeprecatedChildAdminSyncLogs` rests on row 1's Switch reading: its deepest path runs through the only Switch in the fixtures, and counting a case as two levels would give 36/6. No fixture flow sits at exactly depth 4 with 50 actions or fewer, so only the unit tests guard that edge.

## Automation

None. The rules run inside `analyze`, with no new command, option or tool.

## Screens

None.

## Environments and route

None. No step reads or writes a tenant or runs `pac`.

## Solution and publisher

None.

## Constraints

- AGENTS.md: the "Add a rule" recipe, tests first; no dependency; no write site; fake values only; no version or changelog edits; Conventional Commits; `dotnet format` clean.
- The rules are warnings and `analyze` defaults to `--fail-on error`, so no exit code changes unless a user passes `--fail-on warning`.
- No eval baseline may change (the Phase 7 plan's Data, "The pinned roles"). If either rule fires on an eval case, the build stops and this plan records what decision 40 requires (a rerun, costed) before it goes on.
- No change to `powerops-verify` or `powerops-review-security`.

## Assumptions

- Learn's Scopes page (checked 2026-11-03): at most 8 nested levels "including scopes, conditions, switch cases, and apply-to-each loops", and flows "with a high number of actions or deeply nested scopes might experience slower performance". Learn's Limits page (checked 2026-11-03): 500 actions per workflow, 8 levels of nesting, Until 60 iterations by default and 5,000 at most. Learn's anti-patterns page (checked 2026-11-03): nested For each loops, 10 by 10 is 100 iterations, use OData expansion and one loop.
- OverFlow's `SKILL.md` at `f898c77aef968121800d41825357340c05a48024` in microsoft/power-cat-skills (MIT, read 2026-11-03) has "Total action count over 50, or nesting depth over 4 → high", with no reason and no definition of depth.
- Learn doesn't say how the 500 counts nested actions, or whether a Switch case is one level or two. Row 1's reading (containers count, a case is one level) is the Phase 7 research's; it found 19 CoE Starter Kit flows at exactly 8 by this count and none over, which fits the limit but doesn't prove it.
- The noise the Phase 7 research measured (37.4% of 235 CoE Starter Kit flows over either line, 4.4% of 45 PnP samples; nested loops in 36 of 235 and 1 of 45) is a researcher's count, not recomputed here.
- A flow whose definition isn't valid JSON already makes the solution unreadable through `RuleHelpers.CloudFlows`; the new rules add no new failure.
- Environment facts were not read: this plan changes this repo and targets no environment.

## Open questions

## Questions for your admin

## Steps

1. This plan, a review in a fresh context, fixes, sign-off.
2. Rows 2, 4, 5 and 10, and see them fail to compile or fail; then rows 1 and 3 until they pass.
3. Rows 6 and 7; `RuleDocsTests` and `DocsDriftTests` pass.
4. Row 8 by "Update goldens", reading the diff; `RoleEvalTests` passes with no eval file changed.
5. Row 9.
6. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan (`bash reports/leakscan.sh origin/main <this branch's commit>` from the main checkout); push and open one `feat:` PR whose body names the two new warnings, the `fail-on: warning` and baseline effects (Users) and the seven new fixture findings. The master session merges.
