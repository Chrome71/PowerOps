---
status: done
review: done
environment: null
depth: quick
supersedes: null
signed-off: 2026-11-07
signed-off-hash: d4f915ac8bd163d5de4c4fe17828044ba53014b1f1723f0467e3b74f49ac245f
---

# M71: Fixes from the M63 live rerun

## Goal

Fix what the M63 live rerun (Linux 2026-11-05, Windows 2026-11-07) found in PowerOps but left out of its PR. Its record lists seven follow-ups, and the record stays outside Git because it is built from tenant data. Five of them are PowerOps' to fix, one is a note about Microsoft's tools, and one is dropped (decisions Q2):

| # | Follow-up | This plan |
|---|---|---|
| 1 | The plan file's `target` is the environment's display name, but `deploy apply --target` and the MCP `deploy_apply` take an id or URL. An agent passed the display name and got "`pac org who` failed … Sign in with pac auth create", which sent it the wrong way | a hint when the read fails on the plan's own name, and both descriptions say it (rows 1, 2) |
| 2 | In Claude Code, the plugin's copies of the three role skills fork to Opus whatever model the session uses, so a Sonnet build session pays about half its cost at Opus rates. The cost notes don't say so | a sentence in both cost notes (row 4) |
| 3 | Right after a clean import, `drift` reports the value a settings file set in Test, and that value's record, as two drift items with exit 1. That's expected, but a person reading "2 drift items" can't tell | a sentence in `drift`'s section (row 5) |
| 4 | Microsoft's build routes didn't reach a business rule (the Maker SDK push failed with HTTP 400). No route can remove the default admin roles from a model-driven app: the maker portal locks them | a note in `powerops-implement` (row 6) |
| 5 | A person couldn't tell the `playwright open` window from their other browser windows | a sentence in the Playwright starter's README (row 7) |
| 6 | An accepted `deploy_apply` on a plan that only skips reports `"applied": true` and `"status": "imported"`, although nothing was imported | a message, no new status (row 3, decisions Q1) |
| 7 | PowerShell's `Start-Transcript` lost native commands' output in a Terminal-panel tab | dropped: no PowerOps doc uses a transcript (decisions Q2) |

Done when:

1. `deploy apply` (the CLI and the MCP tool) prints, after the failed read's lines, one more line, `error: --target takes the environment's id or URL, as pac --environment does; '<name>' is the plan's display name for it`. It does this only when the read of the target fails and `--target` equals the reviewed plan's `target`, ignoring case. The exit code stays the read's own (`3`, or `2` for a value the reader refuses), and every other failure prints what it prints today.
2. An accepted apply in which every solution is `skip` ends with `status` `imported`, `applied` true, and the `message` `Nothing was imported: every solution was skipped.` The text report says the same, and the schema, `COMMANDS.md` and RECOVERY say so. An apply that imports at least one solution reports what it reports today.
3. The docs rows 2 and 4 to 7 are in, and the plugin copies of `powerops-implement` are regenerated.
4. `DeployApplyCommandTests` cover rows 1 and 3, `DeployCommandTests` covers `deploy plan`'s side of row 1, the `deploy gate` tests pass unchanged, and the goldens and schema tests are current.
5. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m71-m63-follow-ups` and the leak scan are clean, and the PR is green on all eight checks.

### Won't build

- A new `status` value such as `skipped`. It is a closed list in `docs/schemas/deploy-apply.schema.json`, which the Action, RECOVERY and any caller read (decisions Q1).
- Accepting the plan's display name as `--target`. Turning a name into an id needs a read of `pac env list`'s text, which AGENTS.md doesn't list among the allowed `pac` text parses. A name isn't unique either: `drift` names environments by position for that reason.
- Changing the model the role skills fork to. `powerops-verify` and `powerops-review-security` are kept roles whose evaluated setup is pinned (AGENTS.md; `docs/plans/m55-role-freeze/plan.md`), and `powerops-review-plan` runs the same way; this plan only documents the cost.
- A `drift` option that leaves settings-file values out. The two items are true differences between the environments, and the docs sentence is enough to read them.
- Follow-up 7 (decisions Q2).

## Users

- A person or an agent that promotes a reviewed plan with `deploy apply` or the MCP `deploy_apply`, and reads the plan file for the target.
- A person reading an apply report to tell whether anything changed in the target.
- A maker choosing a model for a build session, who needs to know what the three role skills cost.
- A person reading `drift` right after a promotion.
- An agent following `powerops-implement` that plans the steps Microsoft's skills can't do.

## Data

Line numbers are those of `15b8d9d`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | `Prepare` gets one more parameter, the reviewed plan's `target`, and prints the hint of Done when 1 in its `DriftReadException` catch, after the `error: --target: …` line and the forwarded text, when that name is not null and equals `target` (`StringComparison.OrdinalIgnoreCase`). The name goes through `Clean`, as in the mismatch line at `:276`. `DryRun` gets the same parameter and passes it on; only `Apply` (`:378`) passes the plan's target. `deploy plan` and `deploy gate` (`:636`) pass null, so the gate, which has no `--target` and exits only `0` or `2`, prints what it prints today | `src/PowerOps.Cli/DeployCommand.cs:241` (`DryRun`), `:268` (its `Prepare` call), `:378` (`Apply`'s `DryRun` call), `:738-798` (`Prepare` and the catch) | `DeployApplyCommandTests`: an apply whose fake read fails, with `--target` the plan's `target` in another case, prints the hint and keeps the read's exit code; `A_failed_re_plan_exits_as_deploy_plan_does_and_imports_nothing` (`:311`, `--target` a URL) still prints exactly what it prints today; `DeployCommandTests`: `deploy plan` with a failing read prints no hint; the `DeployGateCommandTests` stderr tests (`:380`) pass unchanged |
| 2 | The `target` descriptions say the plan file's `target` field won't work: "… an id or URL (as pac --environment). The plan file's `target` is the environment's display name, which this doesn't take." | `src/PowerOps.Cli/McpCommand.cs:258` (`deploy_apply`), `src/PowerOps.Cli/DeployCommand.cs:56` (`deploy apply --target`); `docs/COMMANDS.md:117` (`deploy apply`); `docs/HANDOFF.md:49` (step 8), whose `--target <test>` gets "(Test's id or URL, not the plan file's `target`)" | the golden `tests/golden/mcp-tools.txt:295`, which holds the MCP text |
| 3 | When `imports` is empty (every row is `skip`) and the run is an accepted `--apply`, the return with no planned flow (`:430`) gives the message `Nothing was imported: every solution was skipped.` where today it gives null. That is the only return a skip-only apply reaches: `DeployPlan.Build` gives a skipped solution no planned flows (`DeployPlan.cs:162-183`), so `flowRefs` is empty. The text report then prints `applied: Nothing was imported: every solution was skipped.` (`DeployApplyReport.cs:66-69`, unchanged). The schema's `status` description ("imported: the imports ran") and `message` description, the `deploy apply` section of `COMMANDS.md` and RECOVERY's exit-`0` row say that `imported` with this message means nothing changed. `schemaVersion` stays 1, because no field or value is added | `src/PowerOps.Cli/DeployCommand.cs:428-431`; `docs/schemas/deploy-apply.schema.json:16-18`; `docs/COMMANDS.md:117`; `docs/RECOVERY.md:19` | `DeployApplyCommandTests`: `A_plan_of_only_skips_imports_nothing_reads_nothing_again_and_exits_0` (`:866`) also checks the message in the JSON and the text report; an apply that imports one solution with no flow still gives a null message |
| 4 | The cost notes add: "In Claude Code, the plugin's copies of the three role skills run on Opus whatever model the session uses (`model: opus` in their `SKILL.md`), so a session on a smaller model still pays Opus rates for them. Codex's copies name no model." | `docs/PLANNING-SKILLS.md:333`, `docs/USE-WITH-AGENTS.md:48` | none (prose) |
| 5 | `drift` adds: "Right after an import with a deployment settings file, a variable whose value the file sets, and that value's record, show as drift against an environment the file didn't set them in. That's the settings file at work, not a change to undo." | `docs/COMMANDS.md:91` (`drift`) | none (prose) |
| 6 | `powerops-implement` adds, after the sentence about environment variables and connection references: "In a live run (M63) no Microsoft skill created a business rule (the Maker SDK push failed with HTTP 400), so propose a maker-portal step for one. Don't plan to keep a model-driven app from System Administrator, System Customizer or Environment Maker: they can open every app in the environment whatever roles the app has (a link to Learn's page *Privileges required to view and access apps*, under power-apps/maker/model-driven-apps). Plan the app's access by the roles of the people who use it." The plugin copies are regenerated by the recipe "Update goldens" | `skills/powerops-implement/SKILL.md:39`; `plugins/*/skills/powerops-implement/` | none (prose); the recipe regenerates the copies |
| 7 | The Playwright starter adds, after the `playwright open` command: "It opens a Chromium window with the Playwright Inspector beside it. Sign in in that Chromium window, not in your usual browser." | `docs/examples/playwright/README.md:62` | none (prose) |

## Automation

None. Nothing new runs on its own.

## Screens

None.

## Environments and route

None. Rows 1 and 3 change what `deploy apply` prints and returns. Neither adds a tenant read or write, and the tests use fake reads.

## Solution and publisher

None.

## Constraints

- AGENTS.md: the smallest change, new logic with tests, no new write, Conventional Commits, no version or changelog edits, and no tenant URLs, ids, emails, user names or environment names in commits.
- The kept roles `powerops-verify` and `powerops-review-security`, their plugin copies and `tests/eval` stay untouched. Row 6 changes only `powerops-implement`, which is not a kept role.
- `deploy apply`'s exit codes, the schema's fields and values, and the gate don't change.
- Platforms are Windows and Linux. The hint's case-insensitive match holds on both.

## Assumptions

- The plan file's `target` holds the friendly name that `deploy plan` printed. It is the `reviewed.Target` that `DeployCommand` already compares at `:274`.
- On a skip-only plan, `imports` and `flowRefs` are empty, and nothing reaches `pac solution import` (the M63 report showed `pacExitCode: null`; `DeployPlan.cs:162-183` plans no flow for a skip).
- The Chromium-and-Inspector description of `playwright open` comes from the M63 run on both systems.

## Open questions

## Questions for your admin

## Steps

1. This plan and `decisions.md`, a fresh-context review into `review.md`, fixes, and the person's sign-off.
2. Rows 1 and 3, test first, then row 2.
3. Rows 4 to 7, then the goldens by the recipe "Update goldens".
4. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m71-m63-follow-ups` and the leak scan; then push, the PR and CI.
