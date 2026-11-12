plan-hash: 2f52ac0aca4def3cd4a7b75a505de7d67cb9944a541760ed08cee52348d808a8
model: claude-sonnet-5-5
fresh-context: yes

Reviewer notes (not findings):
- Environment facts were not read: the plan's `environment:` is `null` and no sign-in confirmation was given. Tenant: offline.
- This plan changes the PowerOps CLI, not a Power Platform solution. Items 1, 2, 4 to 7 and 8 (solution, connection references, DLP, route, managed state, delegation, roles) are not applicable: the plan says None for Automation, Screens, Environments and Solution. Item 3 (licences) is not applicable for the same reason: the flows, variables and references the plan names are subjects of a report and test data, nothing is built. Item 12: `powerops` and `dotnet` were found on PATH; the leak scan script was found in the main checkout only (the plan says so under Assumptions); the Fable reviewer and the researcher are named agents, listed as available.
- Checked against the code at the plan's commit: `AddUpgrade`'s early return and the three name-level loops; `PlanKind.Order` and `SeverityOf`; `JsonOutput.DeployPlanSchemaVersion` (2) and its two users; `DeployPlanFile.ReadFields` (refuses another version before reading anything else, never reads `items`); the schema's `kind` enum and `schemaVersion` const; the "Each output's version" line, the "from 1 to 2" paragraph and the `diff` aside; the seed fixture and the existing same-version test (the one expected `stays` item for the seed follows from them); decision 65 is the last decision, so 66 is the next number; `DeployCommand`, the Action scripts and `DeployPlanReport` hold no per-kind logic beyond `block` and `agent`. Each claim of Done when 1 to 5 held.
- `plan.md` was not edited (`review: done` was not set), because the request said to change no file but this one. The parent session sets it.

## Findings

### High

No findings.

### Medium

No findings.

### Low

#### R1. The shared-loop change is guarded for only one managed case
- Area: 9 (testability)
- Finding: Data row 1 moves the three `deletes` loops so that one code path now serves two kinds, chosen by the zip's `Managed` flag. Row 4 tests a managed zip only over a managed install (d). A managed zip over an unmanaged install (its `managed-conflict` plus today's `deletes` items) is the case where a wrong condition (the install's flag instead of the zip's) would silently turn `deletes` into `stays` or the reverse, and no test pins it.
- Suggested fix: add to (d) or (f) a managed zip over an unmanaged install: `managed-conflict` and the three `deletes` items as today, no `stays`.
- Resolution: open

#### R2. The tests of one refusal branch are written three times
- Area: 11 (size)
- Finding: Row 4 changes the `schemaVersion` theory case to `2`, adds a `DeployPlanFileTests` case for a file at version 2 that asserts the same `Plan` null and the same refusal text, and adds a `DeployApplyCommandTests` case for the same file through `deploy apply`. All three go through the one `version != DeployPlanSchemaVersion || tool != tool` branch, which the plan does not change. The existing tool-mismatch tests already cover the apply path of that branch. The MCP `deploy_plan` test restates what the CLI golden and schema test already show, since the plan itself says the MCP tools pass the CLI's JSON through.
- Suggested fix: keep the changed theory case, the constant assertions and the apply case (the user-visible effect of the bump); drop the separate version-2 `DeployPlanFileTests` case; either drop the MCP case or say which line of Goal fails without it.
- Resolution: open

#### R3. The claim the message makes is only partly sourced, and the docs don't say so
- Area: 13 (buildability)
- Finding: `decisions.md` records that Learn supports "an update does not delete" for managed solutions and an on-premises page states "additive" for unmanaged ones; no page names flows, variables or references. The message tells every reader "An unmanaged import does not delete this", and Done when 5 has `COMMANDS.md` and the new decision state what `stays` is, without naming the pages or their date, as the neighbouring `agent` paragraph does (a page title and a read date).
- Suggested fix: add to Done when 5 that the `COMMANDS.md` paragraph and the decision cite the Learn pages by title with `read 2026-11-12` and say the claim rests on documentation for update and on practitioner reports, not on an observed run.
- Resolution: open

#### R4. The `unproven:` line doesn't follow the plan's form
- Area: 13 (buildability)
- Finding: Assumptions has `unproven: ...; probe: ...` but no `probed by step <n>`. No step probes it, because the probe needs a tenant and the plan is offline. The line argues that no step depends on it, which holds, but then the probe is an orphan.
- Suggested fix: reword to "unproven: whether the target solution still lists the component after the import; no step depends on it; probe left to the next live trial" and name where it is recorded (for example a follow-up in `docs/ecosystem/changes.md` or the build report), so it isn't lost.
- Resolution: open

#### R5. The authority is a file outside the repo
- Area: 10 (unanswered areas)
- Finding: Goal says "the plan's M106 section is the authority for the change". That file is `reports/practitioner-research/upgrade-plan.md`, which is untracked and not in the worktree. A later reader (the Fable review, a future session) cannot check the plan against it, and Fable's approval of the upgrade plan is cited from it.
- Suggested fix: add one sentence saying Done when 1 to 7 are the whole requirement and the upgrade plan is context, or put the M106 section's wording into `decisions.md`.
- Resolution: open

#### R6. An information item hides under "plan ok"
- Area: 10 (unanswered areas)
- Finding: Done when 2 keeps the summary line unchanged, so a plan with `stays` items still reads `plan ok`. The `agent` item gets its own summary wording; `stays` exists to surface things nobody notices, and a reader of the summary line (the text report, the Action's bold line) sees nothing. `decisions.md` has no question on severity (`info` against `warning`) or on the summary line, and says only that severity comes from the `_` arm.
- Suggested fix: add a Q to `decisions.md` that records the choice and its reason (for example: the items are in every format, the summary stays one line, `info` like `same-version-differs`), or amend the summary.
- Resolution: open
