plan-hash: 12431c7c70981986e8ef2e82a937c8464411bff6c30b46e3be4eb215543a343a
model: claude-sonnet-5-5
fresh-context: yes

Reviewer notes (not findings):
- Fourth pass, on the plan at commit 7e7e065. The earlier passes gave R1 to R6 (hash 2f52ac0a...), R7 (hash 6408df55...) and R8 (hash 2d52442a...), all Low. Their status is below. The only change to the plan body since the last pass is Done when 6 (the guard-test exception); the rest was re-read and holds as before.
- Environment facts were not read: the plan's `environment:` is `null` and no sign-in confirmation was given. Tenant: offline.
- This plan changes the PowerOps CLI, not a Power Platform solution. Items 1 to 8 (solution, connection references, licences, DLP, route, managed state, delegation, roles) are not applicable: the plan says None for Automation, Screens, Environments and Solution, and builds no Power Platform artefact. Item 12: `powerops` and `dotnet` were found on PATH; the leak scan script exists in the main checkout only, as the plan's Assumptions say.
- Checked in the third pass: Step 2's split of red and guard tests against the current code (the tests listed as red fail today: no `stays` exists, the plan file is already version 2 so the rewritten version-2 apply case is not refused, and a version-2 file is accepted so the changed `InlineData` case is not a refusal; the tests listed as guards pin behaviour that exists today and pass); `m98-done-handover.md` and its siblings sit in the `reports/trial-3/chain/` folder of the main checkout, where Step 5 puts the M106 note.
- Checked in the second pass: `OutputSchemaTests` holds `Drift_schema_kinds_equal_DriftKind_Order` with the same JSON path the new deploy-plan test would use, and `PlanKind.Order` is public; `tests/golden/mcp-tools.txt` exists; the two `JsonOutput` XML comments say "Only `diff` is at 3" and "`deploy plan` is at 2" as the plan describes; the existing CLI refusal test changes `tool`, not `schemaVersion`, as the plan says; Q6 and the Q4 rewording are in `decisions.md`.
- `plan.md` was not edited (`review: done` was not set), because the request said to change no file but this one. The parent session sets it.

## Findings

### High

No findings.

### Medium

No findings.

### Low

#### R1. The shared-loop change is guarded for only one managed case
- Area: 9 (testability)
- Finding: a managed zip over an unmanaged install was not tested, and a wrong condition in the shared loops would turn `deletes` into `stays`.
- Suggested fix: add that case to row 4.
- Resolution: closed. Row 4 now has (h), a managed zip over an unmanaged install: `managed-conflict`, today's three `deletes` with today's messages, no `stays`, asserted as the full list of kinds; (f) asserts the full list of kinds too; Data row 1 keeps the three messages byte-identical and writes the condition as `!solution.Managed && snap.Managed == false`.

#### R2. The tests of one refusal branch are written three times
- Area: 11 (size)
- Finding: a separate version-2 `DeployPlanFileTests` case repeated the changed theory case, and the MCP case repeated the CLI golden and schema test.
- Suggested fix: fold the file test into the theory case; drop or justify the MCP case.
- Resolution: closed. The separate file case is folded into the `InlineData` change, and the apply case is kept as the user-visible effect of the bump. The MCP case stays, and the reason is on record in `decisions.md` (the upgrade plan's section 8 and the milestone brief require a `deploy_plan` MCP case that returns `stays`); that is a requirement from outside the plan, so it is accepted.

#### R3. The claim the message makes is only partly sourced, and the docs don't say so
- Area: 13 (buildability)
- Finding: the docs and the decision didn't name the pages or say the claim is not observed.
- Suggested fix: cite the pages by title with the read date and say it is documented, not observed.
- Resolution: closed. Done when 5(d) names *Upgrade or update a solution* and *Solution concepts* with `read 2026-11-12`, says it is documented and reported and not observed, and says Learn is silent on solution membership; the BUILD-PLAN decision must say the same. Done when 5(c) adds that other component types also stay and aren't listed.

#### R4. The `unproven:` line doesn't follow the plan's form
- Area: 13 (buildability)
- Finding: the probe was an orphan with no step and no place it was recorded.
- Suggested fix: say no step depends on it and name where the probe is recorded.
- Resolution: closed. The line now says no build step or message depends on it, and names the record (a follow-up for the next live run). See R7 for the one gap left in how that record is made.

#### R5. The authority is a file outside the repo
- Area: 10 (unanswered areas)
- Finding: the plan named an untracked file as its authority.
- Suggested fix: say Done when 1 to 7 are the whole requirement.
- Resolution: closed. Goal now says Done when 1 to 7 are the whole requirement this milestone is built and reviewed against.

#### R6. An information item hides under "plan ok"
- Area: 10 (unanswered areas)
- Finding: no recorded reason for `info` or for the unchanged summary line.
- Suggested fix: record the choice.
- Resolution: closed. Q6 records `info` and `plan ok`, with the reason that the import changes nothing about these components, and Done when 5 has the decision say the summary line is left alone.

#### R7. Two small gaps in the steps (raised in the second pass)
- Area: 9 (testability), 10 (unanswered areas)
- Finding: (1) Done when 6 and Step 2 want every test of row 4 "seen to fail on their assertions", but `Deploy_plan_schema_kinds_equal_PlanKind_Order` passes at the baseline (the enum and `PlanKind.Order` both hold 11 values), so it can't be seen to fail first; an executor will either skip the rule or bend the test. (2) Assumptions say the `unproven:` probe is "recorded as a follow-up in `m106-done-handover.md`", but no step writes that handover (Step 5 ends at the merge), so the record exists only if the executor remembers it.
- Suggested fix: (1) say in Done when 6 or row 4 that the enum-equality test is a guard that passes at the baseline and must fail if either side is changed alone (check it once by editing one side). (2) add to Step 5 that the build report or the handover records the probe.
- Resolution: closed. Step 2 now names which tests are red first ((a), (c), (g), the order test, the version assertions, the CLI golden case, the MCP case, the version-2 apply case) and which are guard tests that pass before the change ((b), (d), (e), (f), (h) and the enum-equality test), and the report says so; Step 5 writes `m106-done-handover.md` with the pull request, the merge commit, what wasn't checked and the probe.

#### R8. Done when 6 still says every test is seen to fail first (raised in the third pass)
- Area: 9 (testability)
- Finding: Done when 6 reads "The tests of Data row 4 are written first and seen to fail on their assertions, then pass", while Step 2 now says six of them are guard tests that pass before the change. A work reviewer who holds the build to Done when 6 literally would fail a correct build, or an executor would bend a guard test to turn it red.
- Suggested fix: in Done when 6, add "except the guard tests Step 2 names, which pass before the change and are reported as such".
- Resolution: closed. Done when 6 now excepts the guard tests Step 2 names, which pass before the change and are reported as such; the two lines agree.
