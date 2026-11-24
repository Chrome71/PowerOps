plan-hash: e7818b388d9415a0257419fc9729c2d34d20ed519a17a92132c827264958ba5d
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: nobody confirmed in this session that `pac` is signed in to an environment, and the plan's `environment:` is null, so `powerops plan facts` and `pac` were not run. `plan status` was run with the build at `src/PowerOps.Cli/bin/Debug/net10.0/powerops.dll` (status `approved`, 0 open questions, 0 admin questions). The fix plan of 2026-11-08 that the Goal cites is not in the repository and was not read; this review reads `plan.md`, `decisions.md` and the code and tests at `4ec4668`.

This is the third pass; it checks only R13's answer. The first pass (R1 to R12, against plan-hash `0342e4de…`) is kept below with each finding's resolution, checked against the revised plan; the revised plan's line citations were spot-checked against the code (`apply.sh:47`, `ActionScriptTests.cs:240-246`, `HANDOFF.md:35, 50, 51`, `USE-WITH-AGENTS.md:51`, `PLANNING-SKILLS.md:330`, `RECOVERY.md:20`, `deploy-apply.schema.json:16`, `powerops-implement` step 7) and hold. `PlanDocument.Parse` gives no body hash for a `plan.md` with no closing fence, so row 1's "`Stale` null" case holds. `DeployApply.Imports` throws only for a zip missing from the list it is given, so row 4 calling it in `deploy plan` with the same zips that built the plan cannot fail there.

Checklist items that do not apply, because this plan changes the PowerOps CLI and builds no Power Platform solution:

- 1 Solution boundaries and publisher prefix: not applicable; no solution is built, and the tests use the existing seed fixture and `SolutionZip`.
- 2 Connection references and environment variables: not applicable; no solution is built. The plan and `decisions.md` hold no URL, GUID, email or person's name.
- 3 Premium connectors and licences: not applicable; no connector or table is used.
- 4 DLP fit: not applicable; no connector is used.
- 5 Dev to Test to Prod route: not applicable; the change ships through the repo's own branch and pull request, and step 7 commits locally only.
- 6 Managed or unmanaged: not applicable as a packaging choice; the managed flag is only a subject of the evidence note (row 3).
- 7 Data volume and delegation: not applicable; no data source. The evidence reads each distinct input at most once per run (row 3).
- 8 Security roles: not applicable as Dataverse roles; who can get around the check is reviewed instead (R1, R7).
- 11 Size: no table, list, flow or screen. Each Data row maps to a line of the Goal table or a Done line, and `Won't build` is present.

## Findings

### High

No findings.

### Medium

#### R1. An agent can still import through powerops' own CLI, which the gate doesn't see
- Area: 8 Security roles (way around the check), 10 Unanswered areas
- Finding: an agent's shell call of `powerops deploy apply --apply` or `powerops settings apply --apply` passes the gate as not an import and gets only the warnings.
- Suggested fix: name it in Won't build and the docs, or ask the person for a gate rule.
- Resolution: answered. Won't build names the route and the instruction that covers it; row 9 has `deploy gate` say it doesn't see powerops' own commands; a gate rule is a recorded follow-up for the person.

#### R2. The Action's job log drops the new `note:` lines and, in the apply step, the `warning:` lines
- Area: 9 Testability, 10 Unanswered areas
- Finding: `plan.sh` passed only `error:` and `warning:`, and `apply.sh` only `error:`.
- Suggested fix: widen both filters, with tests.
- Resolution: answered. Row 4 lets `note:` through `plan.sh`, row 8 sets `apply.sh` to `^(error|warning):` with a test that forwarded text stays hidden, and Assumptions name the read-back warning that now shows too.

#### R3. The `NotComparedByDiff` docs test can't pass without editing a frozen role
- Area: 9 Testability
- Finding: "column security" was not in the verify skill's step 3, and the `diff` section of `COMMANDS.md` was not in row 9.
- Suggested fix: drop "column security" and add the `diff` section to row 9.
- Resolution: answered. The constant is the six parts of step 3, and row 9 rewrites `COMMANDS.md:53-56` to name them.

#### R4. No test proves the check uses the fresh plan's actions, not the plan file's
- Area: 9 Testability
- Finding: a plan file edited to mark an uncovered zip `skip` was not tested.
- Suggested fix: add that test to rows 5 and 6.
- Resolution: answered. Rows 5 and 6 test it, and both rows check the zips of `DeployApply.Imports` on the fresh plan.

### Low

#### R5. A changed input refuses while a missing input passes, and both have the same zip bytes
- Area: 9 Testability
- Finding: the input-digest condition protected nothing the zip's SHA-256 match didn't.
- Suggested fix: drop it.
- Resolution: answered. Dropped; Won't build gives the reason, and `Problem` reads no file.

#### R6. `InputPath` returns one null for three cases, so `Compare` can't keep its two messages
- Area: 9 Testability
- Finding: a bare null couldn't carry `Compare`'s reasons.
- Suggested fix: one private walk that returns the path or the reason.
- Resolution: answered. Row 1 says so, and the existing `plan status` tests stay unchanged.

#### R7. The check trusts report headers that anyone who writes to the repo can make
- Area: 8 Security roles
- Finding: two files of eight lines pass the check, for a plan of any status.
- Suggested fix: say it in Assumptions and the docs; optionally require a signed-off plan.
- Resolution: answered in part, by choice. Won't build and row 9 say the check proves no author and that the form, the person's yes or the environment's approval stays the authorization; no check of the plan's status is added.

#### R8. `DeployReviews.Covered` has no caller
- Area: 11 Size
- Finding: nothing called it.
- Suggested fix: drop it.
- Resolution: answered. Dropped.

#### R9. Docs and comments that still say the reports gate nothing are not all listed, and some line numbers are off
- Area: 9 Testability
- Finding: HANDOFF line numbers were off, and several spots were missing.
- Suggested fix: fix the numbers and add the spots.
- Resolution: answered in part, by choice. Numbers fixed; `HANDOFF.md:35`, `PLANNING-SKILLS.md:330`, `USE-WITH-AGENTS.md:51`, the schema's `refused` description, `RECOVERY.md:20` and the `PlanChecker.cs:397` comment are added. The `deploy apply` command description stays; the new option's own description carries the change.

#### R10. A few test cases are missing or miscounted
- Area: 9 Testability
- Finding: wrong test count, a dropped root `plan.md` case, no multi-zip refusal test, no one-note-per-input test, and an unclear order of the no-`docs/plans` and skips-only cases.
- Suggested fix: correct and add them.
- Resolution: answered. Rows 1, 3 and 5 add each case, and row 5 says a plan that imports nothing passes before `docs/plans` is looked for.

#### R11. Nobody has seen the roles reach `ready` on a managed zip
- Area: 10 Unanswered areas
- Finding: the agent and CI routes depend on an unmeasured outcome.
- Suggested fix: an Assumption and a re-trial check.
- Resolution: answered. Assumptions name it and what happens if it fails; it is a recorded follow-up for the second trial.

#### R12. A plan file outside the checkout can never pass the check
- Area: 9 Testability, 10 Unanswered areas
- Finding: the input's description didn't say where the plan file must sit.
- Suggested fix: say it in the input and `GITHUB-ACTION.md`.
- Resolution: answered. Row 8's description and row 9's `GITHUB-ACTION.md` text say it.

#### R13. The `Users` line still says an agent behind the gate is refused, without the exception Won't build now names
- Area: 10 Unanswered areas
- Finding: `Users` says "An agent on the MCP route or behind the gate, which is refused until both reports cover the zip." Won't build now says that an agent's own shell call of `powerops deploy apply --apply` gets the warnings only. A reader of `Users` alone, or a doc sentence written from it, would claim more than the plan builds.
- Suggested fix: add "when it imports through the MCP tool or a gated Microsoft import tool; its own `powerops deploy apply` call only warns" to that line.
- Resolution: answered. The `Users` line now names the MCP tool and gated Microsoft import tools, and says the agent's own `powerops deploy apply` call only warns (Won't build). Rest of `plan.md` reread on this pass: unchanged since the second pass.
