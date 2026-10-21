plan-hash: eba24ff54074fdeadece218a9486c029a93622f7745783f0ce921e030728b3f2
model: claude-sonnet-5-5
fresh-context: yes

Scope: `plan.md` and `decisions.md` of `m8-drift`, `AGENTS.md`, the M8 row, the Phase 3 section (environments, reads, research basis), decisions 9 to 14 and build-order tasks 22 to 24 of `docs/BUILD-PLAN.md`, and the code the plan names (`Program.cs`, `SettingsCommand.cs`, `PacRunner.cs`, `OutputSchemaTests.cs`, `Settings/FlowState.cs`, the `tests/fixtures/pac` samples, the seed fixture, the `--format` and `--out` options of the other commands). `powerops plan status` reports `approved`, `openQuestions` 2 (the two admin questions; the `Open questions` section is empty), `review: pending`, no violations or warnings. `powerops plan facts` was NOT run and `pac` was never called: contacting the tenant is forbidden for this review, so there are no environment facts and the overlap check of item 1 could not be done. No Microsoft Learn page was re-read; where a finding rests on Dataverse behavior that the plan's Sources do not state, it says so and asks Step 1 to settle it. Tenant data: no URL with a scheme, GUID, email or person's name in `plan.md` or `decisions.md` (the fake name `Contoso Dev` is not a finding).

Checklist items that do not apply: 1 (no solution or publisher is created; the existing seed `PowerOpsSeed`, prefix `pops`, is only read), 2 (no hard-coded value: environments come in as `--env`, and values are hashed, see R9 and R10), 3 and 4 (no connector or table is added; the premium-licence and DLP questions are the same two admin questions as `m7-settings-live` and are not raised again), 5 and 6 (the route is the user's Dev to Test and the tool only reads; managed in Test is shown as information by Q3), 7 (no app or delegation; the row limit is the 5,000-row page, see R3), 8 (no roles are created; the reading account's role is the first admin question). Items 9 (testability) and 10 (unanswered areas) apply and carry the findings. Beyond the checklist the plan was checked against the BUILD-PLAN row, AGENTS.md (read-only by default, JSON contract, `pac` as a subprocess with list arguments, smallest change, tests) and the existing code. The deviations from the row that the plan does state (no `--show-values`, flows from `workflow`, no `pac solution list`) match decisions Q2 to Q4 and are not findings; R4 is one that it does not state.

## Findings

### High

No findings.

### Medium

#### R1. Components are matched by type and id, which the seed may not satisfy, and variables, flows and references are reported twice
- Area: 9. Testability
- Finding: The seed's root components are a table (type 1), a flow (29) and a canvas app (300), plus the environment variable and connection reference that come with it. The plan's only evidence for identical ids across environments is "Learn states it only for flows" (Assumptions), and its only remedy is to go back to planning after Step 1. Table and attribute components are keyed by metadata ids, and value rows (type 381) are created per environment, not carried by a managed import with a settings file. Separately, a missing flow, variable or reference is also a missing component (types 29, 380 and the reference type), so one real difference gives `component-missing` and `flow-missing` (or `variable-missing`) for the same thing, and the summary count (`<n> drift items`) double counts. The plan has no rule for either.
- Suggested fix: Decide now which component types are compared as components: exclude the types that have their own list (29, 380, 381, the reference type) from `component-missing` and `component-extra`, and say what happens to metadata types whose ids may differ (compare them, or exclude them and say so in the README). Add the component type numbers of the seed in Dev and Test, not only the ids, to the Step 1 record, and list "ids or types differ for a root component" among the go-back Assumptions with the chosen fallback.
- Resolution: fixed (types with their own kind are left out of the component kinds; ids and types of every seed root component compared in Step 1, go-back with a named fallback)

#### R2. A clean managed Test may already carry unmanaged layers from the setup, which would make live item 1 exit 1
- Area: 9. Testability
- Finding: Live item 1 needs exit 0 with the seed managed in Test, its reference bound and its flow on. `unmanaged-layer` is drift for any component of a managed solution with an unmanaged layer, and Q3 and the Solutions-layers source say all unmanaged changes share one "Active" layer. Binding a connection reference or changing a flow's state in the target is such a change on some components, and the plan does not say whether the setup it requires (settings-file import, activation) leaves a layer. Step 1 reads layers in Test "before and after editing the flow", but the go-back Assumptions do not include "a clean Test already shows an unmanaged layer", so the build could reach Step 3 with a rule that fails its own live done-when. This rests on Dataverse behavior the Sources do not state; Step 1 settles it.
- Suggested fix: Add to Step 1: read layers in Test right after the import and the binding, before any edit, and record which components show an unmanaged layer. Add to the Assumptions: if a clean Test shows one, the plan says which component types are exempt (for example the connection reference), or live item 1 is recorded as not supported, before Step 3.
- Resolution: fixed (Step 1 reads layers in a clean Test; go-back Assumption with exempt types or live item 1 not supported)

#### R3. The layer read can cost one `pac` call per component, and the plan has no decision for that case
- Area: 7. Data volume
- Finding: Constraints say that if the layer table must be queried per component, a solution with many components takes one read per component, each a new `pac` process of up to 30 seconds, for each managed environment, and Step 1 "records which". Q4 only covers "cannot be read at all". For a table with attributes, views and forms the component count can be in the hundreds, so a run takes many minutes with no option to skip it, and one slow call fails the whole run (exit 3). Also, per-component layer reads need a component name for each `componenttype` number, which the snapshot does not hold; the plan does not say where that mapping comes from. The 5,000-row stop is also only stated for the one-page reads.
- Suggested fix: Add a rule for the per-component case: a cap (above N components the layer read is dropped and the snapshot says `layersRead: false`, as for Q4) or a batched filter, and name where the type-to-name mapping lives (Core, with a test). Ask Step 1 to record the number of layer reads for the seed and the time per read.
- Resolution: fixed (batched per type, else at most 50 component reads then layersRead false; component-name map in Core with a test; Step 1 records count and time)

#### R4. The offline done-when names an unmanaged-layer case on "recorded `pac` output" even though Q4 may make it impossible, and the row's fallback is replaced without saying so
- Area: 9. Testability
- Finding: The offline done-when lists "an unmanaged layer" among the cases exited 1 "on recorded `pac` output". If Step 1 shows the layer table cannot be read, there is no recorded layer output, and Q4 makes layers "not read" with no effect on the exit code. The Core tests can still cover it with a hand-written snapshot (`unmanagedLayer: true`), but then the done-when is not met as worded and the layer parser test of Step 2 has no capture. The BUILD-PLAN row says the fallback is "limited to installed unmanaged"; the plan's fallback (Q4 option 1) is different (no layer drift at all) and only the live item mentions the change.
- Suggested fix: Word the offline item as "unmanaged layer, on a recorded capture if layers can be read, else on a hand-written snapshot", and list the changed fallback with the others in the Step 6 update of the M8 row.
- Resolution: fixed (offline item allows a hand-written snapshot; Step 5 lists the changed fallback in the BUILD-PLAN row update)

#### R5. A solution installed in none of the environments gives "no drift" and exit 0
- Area: 9. Testability
- Finding: `solution-missing` fires only when the solution is installed in one environment and not the other. If it is missing in all of them (a mistyped `--solution` that still matches the name pattern is the usual cause), every list is empty, no item is produced and the run exits 0 with "no drift". An agent or CI job would read that as a pass. No test is listed for it.
- Suggested fix: Treat "not installed in any environment" as an error: exit 3 (or 1) with a clear message and no `--save`, and add the CLI test. Say which in Automation.
- Resolution: fixed (installed nowhere exits 2 with a message; CLI test added)

#### R6. A failed read forwards only `pac`'s stderr, a mistyped snapshot path is sent to `pac` as an environment, and `pac`'s own text may echo the value
- Area: 9. Testability
- Finding: Automation step 2 forwards "`pac`'s stderr" unchanged, "as in `settings apply`". `settings apply` forwards both streams for a failed import (`error.Write(import.Stdout); error.Write(import.Stderr)`), and `m7-settings-live` review R3 recorded that `pac` prints most errors on stdout. Copying only stderr leaves the user a bare "failed (exit code N)". Also, an `--env` is a snapshot file only if a file exists at that path, so a mistyped snapshot path is passed to `pac` as `--environment` and fails with a text that probably echoes it. The plan says the `--env` value "is never printed: it may be a URL", but the forwarded `pac` text can contain exactly that, and the planned test checks only powerops' own lines.
- Suggested fix: Say which streams are forwarded (stdout then stderr, as `settings apply`), that a timeout forwards nothing, and that the value may appear in `pac`'s own text (the README says so, or the text is not forwarded and Step 1 records what the error says). Treat an `--env` that ends in `.json` or contains a path separator and does not exist as a usage error (exit 2), not as an environment. Add the sign-in hint (`PlanFacts.SignInFix`) for a failed first read.
- Resolution: fixed (stdout then stderr forwarded, nothing on timeout, README notes the value may appear; a missing .json or path-like --env is exit 2; sign-in hint on the first read)

#### R7. No step owns the FetchXML text, its name guard or the capture commands' dependency on the solution id
- Area: 9. Testability
- Finding: Data says the exact FetchXML is "written in Step 1, run by the user in the capture, and the build uses the same text", but Steps 2 to 4 name only parsers, the comparison, reports and the command. Nothing says where the seven query texts are built (Core, so tests can reach them) or which test asserts the `^[A-Za-z0-9_]+$` guard and the parsed GUID. `DriftCommand` is described as only running `pac`, so the queries end up inline in the CLI, untested apart from "every call carries `--environment`". Also reads 3 to 7 take the solution id from read 2, which differs per environment; "Claude first writes the seven reads as exact commands" cannot be a fixed script, only templates with a placeholder, and the plan does not say that.
- Suggested fix: Add the query builders to Step 2 (a Core class, for example under `src/PowerOps.Core/Drift/`) with tests for the guard, the GUID and the exact text, and say in Step 1 that reads 3 to 7 are templates filled with the id from read 2 in each environment.
- Resolution: fixed (DriftQueries in Core in Step 2 with tests; Step 1 says reads 3 to 7 are templates filled with each environment's solution id)

### Low

#### R8. "The same report" offline and live is not true for JSON and Markdown if `source` is shown
- Area: 9. Testability
- Finding: Live item 2 says the offline run "prints the same report", and Step 4 tests that "a saved pair compared offline gives the same report as the live run". The JSON `environments` entries carry `source` (`live` or `snapshot`), which differs by design.
- Suggested fix: Say the items, the drift value and the exit code are the same (and the text output, which shows no source), or drop `source` from the comparison in the test.
- Resolution: fixed (done-when and test compare items, text and exit code; only JSON source differs)

#### R9. Per-environment values that are meant to differ always exit 1, and the plan has no way to accept them
- Area: 2. Environment variables
- Finding: Environment variable values commonly differ between Dev and Test on purpose (a site URL, a mailbox). `variable-value` is always drift, there is no ignore list and no link to the settings file, so a real Dev to Test run exits 1 whenever such a variable exists. The done-when avoids it by using the same value, and the BUILD-PLAN row has the same shape, but the plan does not say it is deliberate or what users do about it. M10's done-when ("no drift, apart from the managed state") has the same limit.
- Suggested fix: Add one sentence under Out of scope (an ignore option is not built) and one in the README step (a variable that differs by design will exit 1; read `items[].kind` in JSON to filter). No code change.
- Resolution: fixed (Out of scope and README sentence added)

#### R10. The "safe to keep as CI artifacts" wording in Q2 is stronger than the Constraints allow
- Area: 2. Environment variables
- Finding: Q2's chosen answer says hashing makes snapshot files "safe to keep as CI artifacts". Constraints say a plain SHA-256 only hides a value from a reader and that short, guessable values (a URL, a flag) can be recovered, and that snapshots belong in private folders. The README step repeats the second statement, so the decision text is the outlier.
- Suggested fix: Keep the Constraints wording in the README and the schema description; do not repeat "safe" in either. No code change.
- Resolution: fixed (Constraints, README and schema description avoid 'safe'; noted as narrowing Q2's wording)

#### R11. Environment names are the only key in the report, and the duplicate-name check happens after the reads
- Area: 9. Testability
- Finding: Items name an environment by friendly name, which is not unique (see M7 review R8). Two live or snapshot inputs with the same name make the report ambiguous even without `--save`, which the plan only blocks for two live environments with `--save`, after both were read although it is exit 2. The file name `<solution>.<name>.json` has more collisions than the name check catches: two different names that become the same after the invalid characters are replaced by `-`, names that differ only in case on a case-insensitive file system, and an invalid-character set that differs by operating system.
- Suggested fix: Put the environment's position (`--env 1`) in each item and each environment entry, check name collisions on the final file name, and say exit 2 comes after the reads in that case.
- Resolution: fixed (position in every environment and item; file-name collisions checked ignoring case after the reads, exit 2)

#### R12. Snapshot files are said to be checked before any `pac` call and also loaded in order during the reads
- Area: 9. Testability
- Finding: Automation item 1 puts "a snapshot file for another solution" among the usage errors before any `pac` call, while item 2 loads each snapshot "in order" with unreadable ones as exit 3. For `--env <live> --env <bad snapshot>` the order decides whether a slow live read happens first, and which exit code (2 or 3) a file with both problems gets.
- Suggested fix: Say all snapshot files are loaded and checked first (before any `pac` call), then the live reads follow in order; list which of exit 2 and exit 3 wins.
- Resolution: fixed (snapshot files are loaded and checked before any pac call; first problem in --env order decides)

#### R13. Some snapshot and match rules are not defined
- Area: 9. Testability
- Finding: (a) `layersRead` for an unmanaged solution: the Data text implies true or false, but the `layers-not-read` row and the text line "`, layers not read`" make Dev (unmanaged) look unread. (b) The sort key of the four snapshot lists is not named, though goldens and round trips depend on it. (c) Flows, variables and references are matched by what: flow id or name, variable and reference by schema or logical name; a renamed flow would show as missing on both sides. (d) `--save` with only snapshot inputs silently writes nothing.
- Suggested fix: One line each in Data or Automation: `layersRead` is false only when a read was dropped (and the text note appears only for managed solutions), the list sort keys, the match key per list (flows by id, subject by name), and `--save` with no live environment is a usage error or a stated no-op.
- Resolution: fixed (layersRead, sort keys, match keys and --save with no live env defined)

#### R14. Reusing the helpers of `SettingsCommand` is described too narrowly
- Area: 9. Testability
- Finding: The plan makes only the `Friendly Name:` reader `internal`. The failure text (`Failed`), the `Guard` mapping, `NotFound` and `PlanFacts.SignInFix` are also needed by `DriftCommand` and are private in `SettingsCommand`, so the build either copies them or makes more of them internal. That is the shared-code choice AGENTS.md's "smallest change" leaves to the plan.
- Suggested fix: Name the helpers to make `internal` (or move them to one small shared class) in Step 4, and say the existing `SettingsCommandTests` must stay unchanged.
- Resolution: fixed (Field, Failed, NotFound and Guard become internal; SettingsCommandTests unchanged)

#### R15. The goldens in Step 5 have no named test, and Step 4 can leave `OutputSchemaTests` red
- Area: 9. Testability
- Finding: AGENTS.md's "Add a command" recipe checks the JSON with `Golden.Verify` in the command tests. Step 4 lists CLI tests but none that verifies a golden, and Step 5 (`test:`) adds the goldens and schemas without a test that reads them. If a CLI test writes `drift-seed.json` in Step 4, `OutputSchemaTests` fails until Step 5 adds the schema and the `Map` entries.
- Suggested fix: Move the schemas, the `Map` entries and the golden checks into the same commit as the CLI tests (Step 4), or name in Step 5 the test that calls `Golden.Verify` for each golden. Also say that goldens are updated with the recipe in AGENTS.md ("Update goldens").
- Resolution: fixed (schemas, Map entries and Golden.Verify goldens move into the CLI step)

#### R16. The Step 1 capture list leaves out what `pac` prints for state and for `--environment`
- Area: 9. Testability
- Finding: Data gives `statecode` as 0, 1 and 2 and `ismanaged` as a flag, but `pac`'s own commands print labels (`Published`, `Draft`, `Suspended` in the existing `list-cloud-flows` samples), and the plan does not say whether `env fetch` prints numbers or labels, or the formatted value for booleans. It also relies on `pac org who --environment` and `pac env fetch --environment` working with an id or URL (cited from Learn and the 2.13.1 usage text, never run). Neither is in the go-back Assumptions. Step 1 gates all later steps and needs the user's yes before the build session can start.
- Suggested fix: Add to the Step 1 record: numbers or labels for state and `ismanaged`, a value truncated by the table width, and that `--environment` works on both commands; add them to the Assumptions with the go-back rule. Say Step 1 is the first thing to do.
- Resolution: fixed (Step 1 records number/label output and --environment; go-back Assumptions; Step 1 is first)
