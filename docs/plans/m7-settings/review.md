plan-hash: 2506c3b31a36f8579b9bc4c04a0065c4ccb5b6f1fe8c6970af459ed9cdc5f0d2
model: claude-opus-5-5
fresh-context: yes

Scope of this review: `plan.md`, `decisions.md`, and, because the plan was written after the build, the code and tests it describes (`DeploymentSettings.cs`, `SettingsReport.cs`, `SettingsCommand.cs`, the Core and CLI settings tests, `tests/fixtures/settings/`, the `settings-*` goldens, the schema, `README.md` and the last four commits). `powerops plan facts` was not run and `pac` was never called: this review did not touch any environment, so checklist item 1's overlap check against tenant solutions was not done (the plan creates no solution). `dotnet test` passes for both `PowerOps.Core.Tests` (251) and `PowerOps.Cli.Tests` (104), which confirms the `analyze-fixtures.*` goldens are unchanged. The plan and `decisions.md` contain no URL, GUID, email or person's name.

Checklist items 3 (licences), 4 (DLP), 5 (route), 7 (data volume) and 8 (security roles) do not apply to two offline, read-only commands; the plan says so and defers them to the live `--apply` plan. That is accepted here and not raised as findings.

## Findings

### High

No findings.

### Medium

#### R1. A non-string `ConnectionId` counts as set, not as empty as the plan says
- Area: 9. Testability (plan claim not supported by the code)
- Finding: Assumptions says "A `ConnectionId` that is not a string counts as empty". `DeploymentSettings.Blank` only treats a missing property, `null`, or a blank string as empty; any other kind (number, object, array, boolean) counts as set. Checked with the built CLI against `powerops-seed/v1`: a file with `"ConnectionId": 123` or `"ConnectionId": {}` reports `no problems` and exits 0, though such a file cannot bind a connection at import. Neither this case nor the matching "a non-string `Value` counts as set" assumption has a test.
- Suggested fix: Make the code match the plan: for `ConnectionId`, treat any value that is not a non-blank string as empty (`emptyConnectionId`). Add Core test cases for `ConnectionId` as a number and an object (fires) and for `Value` as a number (quiet). Keep the plan text as it is.
- Resolution: fixed (non-string `ConnectionId` now fires `emptyConnectionId`; Core tests for a number and an object, and a quiet test for a number `Value`; plan text unchanged)

#### R2. Every decision was taken by the building agent without question rounds
- Area: 10. Unanswered areas
- Finding: The `## Log` says no question rounds were held and that users, data, automation, environments and constraints were "approved without question rounds". The build choices (`emptyValue` ignores `IsRequired`, case-insensitive name matching, the first duplicate wins, `generate` writes no `schemaVersion`, exit 3 for a wrong-shape settings file) were never put to the user, and the log itself says they are "open to change at sign-off".
- Suggested fix: At sign-off, confirm or change each of these five choices explicitly and record the answer as a line in the `## Log`. Changing any of them after release changes the CLI contract.
- Resolution: open

### Low

#### R3. Duplicate entries in the settings file are ignored silently
- Area: 9. Testability
- Finding: When a name appears twice, the first entry counts and the second is never reported. Checked with the built CLI: an empty first `ConnectionId` followed by a filled duplicate gives `emptyConnectionId`; the reverse order passes clean. How `pac` import treats duplicates is undocumented (the plan says the same of missing and extra entries), so a duplicate is likely a mistake the user should see.
- Suggested fix: Decide at sign-off whether a duplicate is a problem. If yes, add a `duplicate` kind (schema enum, text and JSON golden, one Core test). If no, say "duplicates: the first wins" in the README paragraph so users know.
- Resolution: open

#### R4. Managed exports are not covered
- Area: 6. Managed or unmanaged
- Finding: The plan and tests use only the unmanaged seed (the folder and a zip made from it). The plan does not say whether `generate` and `validate` on a managed export should give the same entries, for example whether current values are present in a managed zip and so suppress `emptyValue`.
- Suggested fix: Add one line to Data stating the expectation for managed exports, and a test on a managed zip that has environment variables or connection references if such a fixture exists; otherwise list it under Open questions.
- Resolution: open

#### R5. The "pac file passes" claim rests on a hand-written fixture
- Area: 9. Testability
- Finding: Data says a file made by `pac solution create-settings` passes `validate`, and Step 3 calls `powerops-seed-v1.valid.json` the file "with `pac`'s extra fields". The fixture is hand-written (per `NOTICE.md`) and lacks the `Description` field that Data says `pac` adds, so no committed test runs against real `pac` output.
- Suggested fix: Replace the valid fixture with the scrubbed output of the observed `pac solution create-settings` run (values filled with fake ones), or reword Step 3 and Data to say the fixture imitates `pac`'s extra fields.
- Resolution: fixed (`powerops-seed-v1.valid.json` is now the real `pac` 2.13.1 output with fake values filled in, including `Description`; NOTICE.md says so)

#### R6. The open question on optional environment variables is unresolved
- Area: 10. Open questions
- Finding: One open question remains: whether `emptyValue` should fire for an optional environment variable with no default and no current value. Today it fires, and the schema description and README match that behavior, so a later change would narrow the output.
- Suggested fix: Answer it at sign-off. If the answer is "required only", change the condition in `DeploymentSettings.Validate`, the README sentence and the schema description in one commit, with a quiet test for an optional variable.
- Resolution: open
