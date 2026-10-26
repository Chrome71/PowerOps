# Decisions: m14-flowtest

## Round 1

### Q1. How does a test file name its flow, given a path can hold several solutions?
1. By the flow's display name or workflow id among the cloud flows of every solution under the path; a match in more than one solution fails the test, and the fix is to point at one solution (recommended)
2. Add a `solution` property that names the solution's unique name
3. In your own words
Answer: By the flow's display name or workflow id among the cloud flows of every solution under the path; a match in more than one solution fails the test, and the fix is to point at one solution. A `solution` property wouldn't help where it matters most: `powerops-seed/v1` and `v2` share one unique name and one flow id.

### Q2. What does the simulator do where Learn doesn't document the runtime's behaviour?
1. Pick the conservative behaviour, list each choice in the plan ("Not on Learn"), and put each into item 34's capture flow so a real run checks it; keep `Table` unsupported, since Learn shows no exact CSV or HTML text (recommended)
2. Make every undocumented behaviour `unsupported`
3. In your own words
Answer: Pick the conservative behaviour, list each choice in the plan ("Not on Learn"), and put each into item 34's capture flow so a real run checks it; keep `Table` unsupported, since Learn shows no exact CSV or HTML text. Option 2 would make most fixture flows untestable (the slash path `?['body/value']` alone is in nearly every one). `Table` leaves the BUILD-PLAN's supported list until the capture shows its text.

### Q3. Does item 34, the capture in Dev, run in this build?
1. No: the build is offline; item 34 is planned for 2026-11-21 (at the latest 2026-12-15) and runs only after the user's yes to Dev in that session; the BUILD-PLAN records it as not run yet (recommended)
2. Run it now under the delegation of 2026-10-22
3. In your own words
Answer: No: the build is offline; item 34 is planned for 2026-11-21 (at the latest 2026-12-15) and runs only after the user's yes to Dev in that session; the BUILD-PLAN records it as not run yet. The master session's brief says so, and decision 22 is still open with the user.

## Round 2

### Q4. What shape do assertions take?
1. A list of objects, each with `kind` (`runStatus`, `actionStatus`, `inputs`, `outputs`, `variable`, `response`), `equals`, and by kind `action`, `call`, `name` and `path` (a list of segments) (recommended)
2. One object per area (`status`, `actions`, `variables`, `response`) compared whole
3. In your own words
Answer: A list of objects, each with `kind` (`runStatus`, `actionStatus`, `inputs`, `outputs`, `variable`, `response`), `equals`, and by kind `action`, `call`, `name` and `path` (a list of segments). A list lets the report name the failed assertion by index, and a segment list needs no escaping for the slash in OpenApiConnection parameter keys (`item/pops_name`), which a JSON Pointer would need (`~1`).

### Q5. What happens when the flow calls `utcNow()`, `guid()`, `workflow()` or reads the trigger and the test gives no value?
1. The test is `unmocked`, naming the value to set: like a connector call, the run needs something only the test can give (recommended)
2. Fixed defaults (a set date, a zero GUID, an empty object)
3. In your own words
Answer: The test is `unmocked`, naming the value to set: like a connector call, the run needs something only the test can give. Defaults would let a test pass on values nobody chose.

### Q6. How does `OutputSchemaTests` treat `flow-test.schema.json`, an input schema that no golden uses?
1. An `Inputs` list maps it to `*.flowtest.json`; every fixture test file is validated against it, the unused-schema check counts it as used when a fixture file matches, and a set of invalid test files must be rejected by both the schema and `FlowTestFile.Parse` (recommended)
2. Leave the schema out of `docs/schemas/` (no check)
3. In your own words
Answer: An `Inputs` list maps it to `*.flowtest.json`; every fixture test file is validated against it, the unused-schema check counts it as used when a fixture file matches, and a set of invalid test files must be rejected by both the schema and `FlowTestFile.Parse`. The code check stays the one `powerops test` runs (JsonSchema.Net is a test-only package), and the shared invalid set keeps the two in step.

## Log

- plan started 2026-10-25 from the master session's brief (the BUILD-PLAN M14 row, the Phase 4 research basis, build-order items 34 and 37 to 39, decisions 17, 18 and 22)
- `powerops plan facts` not run: items 37 to 39 are offline, and item 34 waits for the user's yes to Dev; `environment` stays null
- depth: quick (from `m13-marketplace`)
- the user delegated their parts on 2026-10-22 (relayed by the master session): this session answers with the recommended options and signs the plan off itself after a clean fresh review; tenant work (item 34) still needs the user's yes in the session that runs it (Q3)
- Learn rechecked on 2026-10-25 by a powerops-researcher (Sonnet) for the runtime behaviours the simulator relies on: WDL schema (Expressions, static results), workflow actions and triggers, handle errors and exceptions, loops, variables, view run history, the expression functions reference, and Power Automate's test-cloud-flows and customize-triggers pages; where Learn is silent, the plan says so under "Not on Learn"
- fixture survey recounted on 2026-10-25: 34 functions in expressions plus `not` and `lessOrEquals` in condition objects (36); condition-object keys `equals`, `greater`, `not`, `contains`, `or`, `lessOrEquals`; no trigger conditions, `splitOn`, static results or `operationOptions` in any fixture flow
- approved without: users, constraints (not asked: both sections were drafted from the BUILD-PLAN M14 row and Phase 4 text; licensing is noted under Constraints)
- approved (2026-10-25)
- fresh review (Sonnet) 2026-10-25: 0 High, 6 Medium, 8 Low; all fixed in the plan text (R1 to R13) or accepted with a reason (R14) in review.md; R9 adds `and` as a 37th function, R3 narrows mocks to the never-run types and actions with a static result on; no user decision needed
- signed off 2026-10-25 by this session under the user's delegation (see above), after every review finding was fixed or accepted
