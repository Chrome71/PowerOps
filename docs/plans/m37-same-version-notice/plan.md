---
status: implementing
review: done
signed-off: 2026-10-31
signed-off-hash: 9b4d129997bca952878726f4e6ed678766c1eecf8bae0a06e1b3910a57255e0e
environment: null
depth: quick
supersedes: null
---

# M37: A notice for a same-version zip that differs

## Goal

Say something when `deploy plan` skips a zip whose contents differ from the target's. The review of the Windows live run of 2026-10-30 listed this as W-4: a zip with the installed version gets `skip`, and a maker who changed the solution without raising its version hears nothing, so the change never reaches the target. The review's advice was an information item at low priority, with no new reads, exit 0 and one new `kind`. Its report is kept outside the repo, because it quotes a private test run.

Done when:

- A zip whose version equals the installed version gets one `same-version-differs` item for each cloud flow, environment variable and connection reference that only the zip or only the target's solution has, and the rest of the plan is as before: the same action, exit code, `blocked` and hash.
- `docs/COMMANDS.md` says what the item compares and that it can't see a changed flow action.
- `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` and `powerops plan status .` are clean, and the work is committed on its own branch.
- When the user asks for a PR: the PR is green on Linux, Windows and macOS and squash-merged, then a second PR moves every PowerOps pin in the docs to that merge commit, records the CI results and marks this plan done.

## Users

- A maker who changed a solution, exported it without raising the version, and runs `deploy plan`.
- A reviewer who reads the plan's text, JSON or Markdown report, or the MCP `deploy_plan` result, before approving an import.

## Data

Line numbers are those of `872389b`.

| # | Severity | Change | Where | Test |
|---|---|---|---|---|
| 1 | low | New kind `same-version-differs`, last in `PlanKind.Order`, so its severity is information through the `_` arm of `SeverityOf`. `Entry` gains `bool SameVersion`, set in `Prepare` from `Compare(version, installed) == 0`, so `1.0.0.0` and `1.0.0.00` count as equal and a lower version doesn't. In `Build`, after the `AddUpgrade` call, `if (e.SameVersion) AddSameVersion(e, Add)`. `AddSameVersion` compares flows by id, from `e.Flows` (category 5, as the snapshot's flows are) against `snap.Flows`, and variables by `SchemaName` and references by `LogicalName`, ignoring case, both ways, with `ExceptBy` and `Except`. Only cloud flows count, on both sides, unlike `deletes`, which checks every workflow of the zip against the target's type 29 components. Each difference is one item whose `Subject` is the flow's name, the variable's schema name or the reference's logical name. Items are added flows first, then variables, then references, and for each type the zip's side before the target's, so a flow re-created under the same name, with a new id, gives its two items in that order. Messages: `The zip has this <noun> and the target's solution does not, and the plan skips the zip because the versions are equal.` and `The target's solution has this <noun> and the zip does not, and the plan skips the zip because the versions are equal.`, where the noun is `cloud flow`, `environment variable` or `connection reference`. `Solutions[].Flows` stays empty for a skip | `src/PowerOps.Core/Deploy/DeployPlan.cs:56-72,89,185-188,207-228` | new cases in `tests/PowerOps.Core.Tests/Deploy/DeployPlanTests.cs`: both directions for each of the three, with kind, severity, subject and message; a name that differs only by case and a flow id in upper case give no item; a flow with the same name and another id gives the zip's item, then the target's; equal contents give no item; equal numeric versions in another spelling (`1.0.0.0` and `1.0.0.00`) give the item; a lower version, an upgrade and an install give none; a classic workflow (category other than 5) in the zip gives none; the items sort by the existing order; `blocked` stays false; the plan's hash equals the hash of `HashText` for the same inputs, and `HashText` doesn't hold the kind. `Kinds_are_in_the_documented_order` gains the kind. `A_skip_checks_no_settings_dependencies_or_flows_but_an_upgrade_does` (line 208) expects no items for a same-version skip whose zip has a reference and a flow the target lacks, which is now a difference: its skip becomes a lower version (`0.9.0.0`), and a new case holds that a same-version skip gives only `same-version-differs` items, with no `settings`, dependency or flow-state output |
| 2 | low | Add `same-version-differs` as the last value of the `kind` enum | `docs/schemas/deploy-plan.schema.json:100` | a new golden `deploy-plan-same-version.json`, which `OutputSchemaTests` validates through the `deploy-plan` prefix: a test in `tests/PowerOps.Cli.Tests/DeployCommandTests.cs` beside `The_seed_upgrade_matches_the_golden` plans `SeedZip` against `SnapshotFile.Load(TestSnapshot) with { Version = "1.0.0.1" }` and expects exit 0 and two items, one for each direction: `pops_SeedDataverse`, which only the zip has, and `pops_sharedoffice365_6a2f1`, which only the snapshot has |
| 3 | low | `docs/COMMANDS.md`: the list of information kinds gains the new kind, and new sentences say that it applies to a `skip` whose version equals the installed one, and not to a lower version; which three component types it compares (cloud flows only, not other workflows), and how each is matched; that it makes no extra read, doesn't block and doesn't change the plan hash; that it sees only which of them exist, so it can't see a changed flow action, a renamed flow, a changed value or connector, or any other component type, and so no item doesn't mean that the zip equals the target; and that raising the zip's version makes the plan an upgrade, which imports it | `docs/COMMANDS.md:83` | none, docs only |
| 4 | low | A saved plan that holds the item still applies: the hash matches on the second read, and the skip stays a skip | none, beyond row 1 | a new case in `tests/PowerOps.Cli.Tests/DeployApplyCommandTests.cs`: `MakePlan` against a target whose installed solution has the zip's version and a variable the zip lacks, the plan file holds the item, and `Apply` without `--apply` exits 0 with `Kept:skipped`, `status` `planned` and no `pac` call |

## Automation

None changes. `deploy apply` reads only the plan's hash, target, `blocked` and inputs from the file (`DeployPlanFile.cs:10`), so the item never reaches it. `deploy gate` works from the dry run of `deploy apply`, and an information item doesn't change either.

## Screens

None. The item reaches the text, JSON and Markdown reports through their existing item output, and the MCP `deploy_plan` result through the JSON.

## Environments and route

None. No step reads or writes a tenant, and no step runs `pac`, `az`, `plan facts` or any `--apply`. The tests use the fake target read.

## Solution and publisher

None.

## Constraints

- AGENTS.md: new logic comes with tests, `dotnet format --verify-no-changes` stays clean, Conventional Commits, no version or changelog edits.
- No new read: `ReadTarget` already reads the flows, variables and references of every solution in the folder, for a skip too, and those lists feed the hash.
- The plan's hash covers the inputs, not the items (`DeployPlan.Render`, line 362, and the `hash` field of the schema), so a new item changes no hash, and a plan a user approved stays approved. `blocked` looks only at `block` items. `tool` already refuses a plan file from another powerops version, so an old plan never meets the new kind.
- `Items` sort by order, kind and subject, and LINQ's sort is stable, so the same input gives the same report.
- `schemaVersion` stays 1 when the `kind` enum gains a value: nothing is released, so no consumer exists (as in M33 decisions Q1 and M34).

## Assumptions

- Only the three component types that both the zip and the snapshot name are compared. Tables and canvas apps can't be matched by name, because the target names them by id, and M9 decided the same for `deletes`.
- A connection reference's connector, a flow's name or state and a variable's value are left out, so the item keeps to what the review asked for (decisions Q3).
- About 300 of these items can push later rows out of the Markdown report, as any long list of items can. The report says how many rows it left out, its summary still counts the blocking items, and the text and JSON reports are complete, so nothing changes in `WriteMarkdown`.

## Open questions

## Questions for your admin

## Steps

1. This plan, a fresh review, fixes, sign-off.
2. Rows 1, 2 and 4, test-first, by an implementer subagent; goldens by the recipe "Update goldens".
3. Row 3, and an M37 section in `docs/BUILD-PLAN.md` after M36: the section with one milestone row (Release `v0.22.0`, the next minor after M36's) and build-order item 64, and an M37 line in the verification record. Item 65 comes with the pin PR, because it names the merge commit.
4. The coordinating session reads the diff, and re-runs `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` and `powerops plan status .`, and the leak scan.
5. Commit on `feat/m37-same-version-notice`. No push or PR unless the user asks.
