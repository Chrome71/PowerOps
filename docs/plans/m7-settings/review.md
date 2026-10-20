plan-hash: d27344acf963bca2663625d7b2f1c645741d8baec3b6c84806b2cffb6ab5311e
model: claude-opus-5-5
fresh-context: yes

The plan-hash was updated after the text fixes for R1 and R2 below. They changed only the Automation count and Steps 1 and 3 in the plan body, plus one line in `tests/fixtures/NOTICE.md`; nothing else in the body changed.

Scope of this review: `plan.md`, `decisions.md`, and, because the plan was written after the build, the code and tests it describes (`DeploymentSettings.cs`, `SettingsReport.cs`, `SettingsCommand.cs`, `Program.cs`, the Core and CLI settings tests, `tests/fixtures/settings/`, `tests/fixtures/NOTICE.md`, the `settings-*` goldens, the schema and its README row, `OutputSchemaTests`, `README.md` and the last nine commits). `powerops plan facts` was not run and `pac` was never called: this review did not touch any environment, so checklist item 1's overlap check against tenant solutions was not done (the plan creates no solution). `dotnet test` passes for `PowerOps.Core.Tests` (255) and then `PowerOps.Cli.Tests` (104), run one after the other; the `analyze-fixtures.*` goldens are unchanged. `powerops plan status` reports no violations, and the plan and `decisions.md` contain no URL, GUID, email or person's name.

Checklist items 3 (licences), 4 (DLP), 5 (route), 7 (data volume) and 8 (security roles) do not apply to two offline, read-only commands; the plan says so and defers them to the live `--apply` plan. Item 6 (managed exports) is an accepted known gap (Q4). Neither is raised as a finding.

Previous review (written for an older plan hash): all six findings are now resolved. R1 is fixed in code: `NoConnectionId` treats any non-string or blank `ConnectionId` as empty, with Core tests for a number and an object, and a quiet test for a number `Value`. R2 is settled by Q3 (the four build choices confirmed) and Q2 (the fifth, duplicates). R3 is built as the `duplicate` kind in 67bddfc: code, Core test, schema enum and description, a different-case duplicate in the problems fixture, both `settings-validate.*` goldens and the README sentence. R4 is accepted as a known gap by Q4 and recorded under Assumptions. R5 is fixed: the valid fixture is the observed `pac` 2.13.1 output with fake values, including `Description` and `CopilotAgents` (one stale word in `NOTICE.md` remains, see R2 below). R6 is answered by Q1 (keep reporting optional variables), the plan's Automation section says so, and the open question is gone.

Every other plan claim checked holds: the five problem kinds and their conditions, case-insensitive matching with the first entry checked, the exit codes (parse errors return 2 in `Program.Run`, a missing settings file returns 2, a bad settings file or `--out` into a missing folder returns 3), stdout by default, the text and JSON formats, the generate golden skipped by `OutputSchemaTests`, the README usage lines and paragraph, and the zip test.

## Findings

### High

No findings.

### Medium

No findings.

### Low

#### R1. The plan text lags the build in two places
- Area: 9. Testability (plan claims against the code)
- Finding: Automation says `validate` "reports four kinds of problem" and then lists five (`missing`, `extra`, `duplicate`, `emptyConnectionId`, `emptyValue`); the count was not updated when Step 8 added `duplicate`. The Steps also never name commit 463861d ("count a non-string ConnectionId as empty and use pac's real output as the valid fixture"), which changed the Step 1 code and replaced the Step 3 fixture, so a reader matching steps to commits misses it.
- Suggested fix: Change "four kinds" to "five kinds", and add 463861d to Steps 1 and 3 (or as its own done step). Both edits change the body hash, so do them before sign-off and re-run the review or mark it skipped.
- Resolution: fixed (Automation now says "five kinds"; Step 1 names 463861d by its subject, and `git show --stat 463861d` confirms that it changed `DeploymentSettings.cs`, its tests, the valid fixture and `NOTICE.md`; Step 3 says that the valid file was hand-written at first, which matches the fixture in 0d70483, and that Step 8 adds the duplicate)

#### R2. `NOTICE.md` still calls the settings fixtures hand-written
- Area: 9. Testability (fixture description)
- Finding: The `../settings/` line in `tests/fixtures/NOTICE.md` opens with "hand-written deployment settings files" and then says `powerops-seed-v1.valid.json` is the output of `pac solution create-settings`. Only the problems file is hand-written; the opening word was left over from before the R5 fix.
- Suggested fix: Reword the opening to "deployment settings files" and keep the per-file sentences, in a `docs:` or `test:` change. No plan change needed.
- Resolution: fixed (the line now opens with "deployment settings files" and calls only `powerops-seed-v1.problems.json` hand-written, which matches both fixtures)
