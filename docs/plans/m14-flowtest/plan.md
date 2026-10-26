---
status: implementing
review: done
signed-off: 2026-10-25
signed-off-hash: e487b01b6ce17943c398d5c8acffbb06d2bf832f0ff7c6e62f7b9b510f6a6528
environment: null
depth: quick
supersedes: null
---

# M14: `powerops test`, offline flow tests

## Goal

Build BUILD-PLAN milestone M14 (build-order items 34 and 37 to 39, release v0.14.0): `powerops test` runs `*.flowtest.json` files against the cloud flow definitions of the solutions under a path, offline, with mocked connector actions, and exits 1 when a test does not pass. M15 then makes that exit code a required check, which is Phase 4's done-when ("a failing flow assertion blocks a merge").

Done when:

- `tests/PowerOps.Core.Tests/Flows/` has offline tests for: the expression parser (`@`, `@@`, `@{...}`, string literals, `?`, `.name`, `[...]`); each of the 37 functions under Automation against each example in its section of Learn's function reference that uses only those functions and plain arguments (`decodeBase64` from the `base64ToString` section it points to; `triggerOutputs` and `triggerBody` from their descriptions, which have no example), and the argument forms that are `unsupported`; each supported action type and branch; `runAfter` on `Failed` and `Skipped`; a Foreach over a mocked array with a list mock; Terminate; the inputs of a mocked action on one call and on every call; an unmocked connector action (`unmocked`); an unknown function and an unknown action type (`unsupported`).
- `tests/PowerOps.Cli.Tests/TestCommandTests.cs`: the fixture test files in `tests/fixtures/flowtests/` pass (the seed flow and two flows of the CoE ALM Accelerator for Makers, `BuildRequestCancellation` and `ReadEnvironmentVariable`), with exit 0. A copy of `alm-makers` in a temporary folder with the `BuildRequestCancellation` condition changed fails, and the report names the failed assertion; an unmocked action and an unknown function each exit 1; every usage and unreadable-input path exits 2 or 3. The report goldens validate against `docs/schemas/test.schema.json`, and every fixture test file validates against `docs/schemas/flow-test.schema.json`.
- `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` and `powerops plan status` are clean.
- Pending, not part of the offline build: build-order item 34, the capture in Dev, planned for 2026-11-21 (at the latest 2026-12-15, when Dev is suspended). Until it runs, the BUILD-PLAN verification record marks the simulator's check against a real run as pending, and the behaviours listed under "Not on Learn" are the simulator's choices, not verified ones. If 2026-12-15 passes without the capture, the record says "not verified", as for M11's live checks, and the README note on it stays.

## Users

- Coding agents that change a cloud flow in a repo and need a check that the change does what the plan says, before a PR (M15 makes `powerops-implement` write these tests).
- Makers and reviewers who read the report in a PR (Markdown, from M15's Action).
- CI: the exit code is the gate.

## Data

No tenant data. New files:

**Test files** (`*.flowtest.json`, UTF-8 JSON, at most 4 MB each). One test per file. Properties:

- `$schema` (optional, ignored): the schema URL, for editors.
- `flow` (required): the cloud flow's display name, or its workflow id (a GUID, braces and case ignored).
- `trigger` (optional): `{ "headers": {...}, "body": ... }`, what `triggerOutputs()` returns; `triggerBody()` returns its `body`.
- `mocks` (optional): action name → one mock, or a list of mocks, one per call in order (for an action in a Foreach or Until). A mock has the static-result shape: `status` (`Succeeded` or `Failed`, the two Learn documents for static results), optional `outputs` (`statusCode`, `headers`, `body`; a run's "Show raw outputs" pastes in unchanged) and, only with `Failed`, optional `error` (`code`, `message`).
- `environmentVariables` (optional): environment variable schema name → JSON value. A key that is no environment variable parameter of the flow fails the test, so a typo can't fall back to the solution's value.
- `utcNow` (optional): the timestamp every `utcNow()` call returns. `guid` (optional): the GUID every `guid()` call returns. `workflow` (optional): the object `workflow()` returns.
- `assertions` (required, at least one): a list. Each has `kind` and `equals` (any JSON value), and by kind:
  - `runStatus`: the run's status (`Succeeded`, `Failed`, `Cancelled`, or `Skipped` when a trigger condition is false).
  - `actionStatus`, with `action` and optional `call`: an action's status (`Succeeded`, `Failed`, `Skipped`; a simulated run never times out, and mocks are `Succeeded` or `Failed` only, so `TimedOut` never occurs and a `runAfter` on `TimedOut` never matches).
  - `inputs` and `outputs`, with `action`, optional `call` and optional `path`: what the action got (its `inputs` with every expression evaluated, which is what the flow would send) or returned.
  - `variable`, with `name` and optional `path`: the variable's value at the end of the run.
  - `response`, with optional `path`: the first Response action that ran, as `{ "statusCode", "headers", "body" }`.

  `call` is a 0-based index into the action's calls; without it the assertion must hold on every call, and an action with no call fails it ("did not run"); a `Skipped` call has no inputs or outputs, so an `inputs` or `outputs` assertion on it fails the same way. `path` is a list of segments, strings for properties and integers for array items, so keys with a slash (`item/pops_name`) need no escaping; without it the whole value is compared. Values are compared as JSON, numbers by value.

Any other property, a wrong type, a missing `flow` or `assertions`, or a `call` or `path` on a kind that has none makes the file unreadable (exit 3, the message starts with the path). The public `FlowTestFile.Parse` checks this in code; `docs/schemas/flow-test.schema.json` describes the same format, closed with `additionalProperties: false` (the one schema that is, because it is an input the code rejects unknown keys in), and a test keeps them in step (Automation). Only the code checks the 4 MB limit.

**Report** (`--format json`, `docs/schemas/test.schema.json`): `schemaVersion` (`JsonOutput.SchemaVersion`), `tests` (one per file, sorted by path: `file` relative to the tests folder with forward slashes, `flow` and `solution` when found, `result` (`passed`, `failed`, `unmocked`, `unsupported`), `runStatus` when the run finished, `message` for anything but `passed` that is not an assertion failure, and `assertions`: `index`, `description` such as `status of Delete_Environment` or `inputs of Update_a_row at ["parameters","item/pops_name"]`, `passed`, `expected`, `actual` when there is one, and `message` such as "did not run" or "path not found"), and `summary` (a count per result). No solution path, tenant value or time appears in it, so it is stable for goldens.

**Fixture test files** in `tests/fixtures/flowtests/` (made for this project, fake values only, described in `tests/fixtures/NOTICE.md`):

- `seed/`: the `powerops-seed/v2` flow `Stamp work item status`: the trigger body's `pops_workitemid` and a mock for `Update_a_row`; one test takes `pops_DefaultStatus` from the solution's default value (`New`) and checks the `Update_a_row` inputs and the `Compose` output, one sets the variable in the test.
- `alm-makers/`: `BuildRequestCancellation`: the trigger body's `cat_deploymentstatus` and `cat_projectid`. A cancelled request: the `List_records` mock returns one row with `cat_buildrequestid` and `cat_buildenvironment`, and the `$filter` of `List_records`, the update's `recordId` and the deletion's `environment` are checked, each on its call. Any other status: Terminate with `Succeeded`, and `List_records` is `Skipped`.
- `alm-makers/`: `ReadEnvironmentVariable`: the trigger header `X-MS-APIM-Tokens` (the base64 of a made-up `{"$ConnectionKey":"fake"}`), the trigger body's `text`, `guid` (the flow calls `guid()` inside an `if()`, and every argument is evaluated), and list mocks for `GetDefinitions_2` (one row with an `environmentvariabledefinitionid` and a `defaultvalue`) and `Get_Value_2`. No current value (`Get_Value_2` returns no row): the response body's `envvalue` is the definition's default value; a current value: it wins; in both the 409 Response is `Skipped`. `issuccess` is not asserted: it is `@{true}`, whose text is a "Not on Learn" choice.

No solution fixture is added, so the `analyze-fixtures.*` goldens don't change. The Core tests build their flows inline (JSON text in the test), written from Learn's action and function references.

## Automation

The code lives in `src/PowerOps.Core/Flows/` (expressions and the simulator), `src/PowerOps.Core/Output/TestReport.cs` and a thin `src/PowerOps.Cli/TestCommand.cs`. No new package: `System.Text.Json.Nodes` holds the values (JSON `null` is C# `null`).

**Expressions** (item 37; `Flows/Expression.cs`, `Flows/ExpressionFunctions.cs`). From Learn's WDL schema page, Expressions section: a string that starts with `@` is one expression and returns its value's own type; `@{...}` inside a string is replaced by its value as text, and the result is always a string; `@@` is a literal `@` (Learn shows it at the start and before `{`; the simulator applies it anywhere in the string), and a lone `@` not followed by `{` is literal; the scanner that finds the end of `@{...}` skips single-quoted literals; string literals are in single quotes, `'` doubled inside one; `?` returns `null` instead of failing when the value before it is null or lacks the property or item; `.name` and `['name']` read a property, `[n]` an item. A property read with a slash in the name (`?['body/value']`), when the object has no property of that exact name, reads one segment at a time with the same `?` rule (observed in exported flows, not on Learn). Literals: strings, integers, decimals, `true`, `false`, `null`. Every conversion to and from text uses the invariant culture. Parsing the definition's strings happens when they are evaluated; text the parser can't read makes the test `unsupported`, quoting the expression, because an exported flow's expressions are valid. Nesting deeper than 100 is `unsupported`.

The 37 functions: the 34 the fixture flows call in expressions, plus `not` and `lessOrEquals`, which they use only as keys of an `If` condition object, plus `and`, which the designer writes around every condition it creates, so item 34's capture flow will have it (the BUILD-PLAN's 33 was an earlier count). Each is written from its section of Learn's expression functions reference (`azure/logic-apps/expression-functions-reference`, which covers Power Automate too), with that section's examples as tests:

- Workflow: `outputs`, `body`, `triggerOutputs`, `triggerBody`, `variables`, `parameters`, `items`, `item`, `workflow`, `result`
- String: `concat`, `substring`, `replace`, `split`, `toLower`, `trim`, `guid`
- Collection: `contains`, `empty`, `first`, `last`, `length`
- Logical: `equals`, `greater`, `greaterOrEquals`, `lessOrEquals`, `if`, `and`, `or`, `not`
- Conversion: `json`, `string`, `decodeBase64`, `createArray`
- Math: `min`
- Date and time: `utcNow`, `addDays`

Any other name is `unsupported`, naming the function (so `json(xml(...))` is, through `xml`). The fixture flows call `utcNow`, `addDays` and `guid` without a format argument, so a format argument is `unsupported` too, with a test for each; the default text is Learn's `o` format with `Z` for UTC. A test pins the list of names and these argument forms. Every argument is evaluated, left to right, before the function runs (Learn says only "evaluated from left to right"; see Not on Learn). An evaluation error (a missing property without `?`, a wrong type, `substring` past the end, which Learn says is an error) fails the action that holds the expression, with error code `InvalidTemplate`. The workflow functions read from a small interface that the simulator implements and the expression tests fake. `utcNow()`, `guid()`, `workflow()`, `triggerOutputs()` and `triggerBody()` without their value in the test, and `parameters()` of a parameter with no value, make the test `unmocked`, naming what to set: the run needs a value only the test can give, like a connector call.

**Simulator** (item 38; `Flows/FlowSimulator.cs`). Input: the `definition` object of the flow's `clientdata`, the parameter values, and the test's trigger, mocks and fixed values. A definition with more than one trigger, or a trigger with `splitOn`, is `unsupported`. It runs the trigger's `conditions` first: if one is false the run doesn't start and its status is `Skipped` (Learn: the workflow doesn't run; the pages differ on what history shows). Then:

- **Order and `runAfter`** (Learn, "Handle errors and exceptions"): in each block (the top level, a Scope, a branch, a loop iteration) an action is ready when every action in its `runAfter` has finished; an empty `runAfter` is ready at the block's start. After each action the simulator runs the first ready action in definition order, so parallel branches run one after the other in a fixed order. An action runs when each predecessor ended in one of the statuses listed for it; otherwise it is `Skipped`, and its own successors follow the same rule. A `runAfter` that names an action outside its block, a status other than `Succeeded`, `Failed`, `Skipped` and `TimedOut`, or a cycle (actions left that can never be ready) is `unsupported`.
- **What gets a `Skipped` record**: an action whose `runAfter` is not met, every action nested at any depth in a container that is `Skipped`, every action in a branch not taken (Learn, If action) and nested in it, and every action Terminate leaves unstarted, nested ones included. A Foreach over an empty array leaves its actions with no call ("did not run").
- **Block and run status**: a block is `Failed` when an action in it ended `Failed` and no action in the same block that lists `Failed` for it in its `runAfter` ran (a skipped handler handles nothing); otherwise `Succeeded`. A Scope, an `If` or `Switch`, and a Foreach (over all iterations) take their block's status; an Until takes its last iteration's. The run takes the top block's status unless Terminate set one. This follows Learn's run-status page (a failure is handled when "a subsequent action" handles it) and the scopes page's example (run `Succeeded`, scope `Failed`); the two scope pages differ on a scope whose last action handles an earlier failure, and this rule takes the error-handling page's side.
- **Actions**:
  - `Compose`: outputs are its evaluated inputs.
  - `InitializeVariable`, `SetVariable`, `AppendToStringVariable`, `AppendToArrayVariable` (Learn's variables page): types `string`, `integer`, `float`, `boolean`, `array`, `object` in any case. A value of another type than declared is `unsupported` (Learn doesn't say what a run does), a missing variable fails the action, and a variable without a `value` starts as `null`. Names are case-sensitive.
  - `If`: the expression is a string or a condition object (`{"and": [...]}`, Learn's If action; any key is the function of that name, an array value is its arguments, an object is one argument); it must give a boolean, or the action fails.
  - `Switch`: the first case whose `case` equals the value as JSON, else `default` (not on Learn).
  - `Scope`; `Foreach` (the array must be an array, or the action fails; one item at a time; `items('<loop>')` and `item()`); `Until` (body first, then the condition; stops at `limit.count`, default 60, and is then `Succeeded`, the reading of Learn's loops page; an iteration whose block is `Failed` ends the loop, which is then `Failed`; time doesn't pass, so `limit.timeout` is ignored).
  - `Terminate` (Learn): `runStatus` `Succeeded`, `Failed` or `Cancelled`, `runError` with `Failed`; ends the run.
  - `Response`: outputs are the evaluated `statusCode`, `headers` and `body`; its `schema` isn't evaluated.
  - `ParseJson`: outputs `{ "body": content }`, with a string content parsed as JSON; no schema check.
  - `Query`: `from` filtered by `where`, evaluated per item with `item()`; outputs `{ "body": [...] }`.
  - `Wait`: returns at once.
  - `OpenApiConnection`, `OpenApiConnectionWebhook`, `Http` and `Workflow` never run. Their inputs are evaluated and recorded, then the mock gives the status, outputs and error; without one the test stops as `unmocked`, naming the action. A list mock with fewer entries than calls is `unmocked` on the first call it lacks.
  - Only those four types take a mock, and so does an action of any type whose `runtimeConfiguration.staticResult.staticResultOptions` is `Enabled`: it needs one (`unmocked` without), and the mock replaces the action's work as for a connector, until item 34 shows whether an export keeps static results. A mock on any other action, or on a name that is no action, fails the test with a message.
  - Any other type (`Table`, the `Expression` kinds `AddToTime` and `GetPastTime`, `Select`, ...) and any action with `operationOptions` is `unsupported`, naming it. `Table` is in the BUILD-PLAN's list, but Learn shows its CSV rows only for default columns, with no escaping rule, and its HTML only rendered, so it waits for item 34's capture.
  - Action types, `operationOptions` and functions are checked when the run reaches them, like expressions: a branch the run doesn't take isn't checked, so a test of one path can pass while another path of the flow holds something unsupported. The README says so.
- **Outputs and records**: `outputs('X')` is X's outputs, `body('X')` their `body`. Every call of every action is recorded: status, evaluated inputs, outputs, error. `outputs('X')` of an action that hasn't run or was `Skipped` is an evaluation error, and of an action inside a loop the caller isn't in is `unsupported` (Learn doesn't give the shape). `result('<scope>')` lists the scope's top-level actions' records in run order, each `{ "name", "inputs", "outputs", "status" }` plus `error` when there is one (the fields Learn's error-handling page shows, without times and tracking ids); `result()` of a Foreach or Until is `unsupported`.
- **Not simulated**: retry policies, concurrency (Foreach runs one item at a time, so a flow whose iterations race on a variable can differ), timeouts, secure inputs. A run longer than 100 000 action calls and loop iterations together is `unsupported`, and so is a value nested deeper than 64 levels.

**Parameters**: each definition parameter is resolved by the test runner before the run. An environment variable parameter (schema name from `metadata.schemaName` or the `(schema)` label suffix, as `FlowDefinition` reads it) takes the test's value, else the solution's current value, else its default value, else the parameter's `defaultValue`; a solution value is text, converted by the parameter's `type` (`String`, `SecureString`, `Int`, `Float`, `Bool`, `Object`, `Array`), and one that doesn't convert is `unsupported`. Other parameters (`$connections`, `$authentication`) take their `defaultValue`.

**Test runner** (item 39; `Flows/FlowTest.cs`): finds the test files (`*.flowtest.json` under `--tests`, default the path, recursively, skipping `bin`, `obj`, `.git` and `node_modules` and not following links, as `SolutionDiscovery` does), reads and checks all of them and loads every solution under the path with `SolutionLoader` before any test runs, then for each test finds the flow among their cloud flows (category 5) by name (ordinal) or id. No match, more than one match (in one solution or several, for example `powerops-seed/v1` and `v2`, so point at one of them; the message names each solution's folder relative to the path), or a flow without a definition fails the test with a message. Then it resolves the parameters, runs the simulator, and checks the assertions in order, all of them, after a finished run; `unmocked` and `unsupported` stop the run and skip the assertions.

**Command** (item 39; `TestCommand.cs`, registered in `Program.cs`): `powerops test [path] [--tests <dir>] [--format text|json|markdown] [--out <file>]`, `path` `.` by default. Its description says the simulator is written from Microsoft Learn and not yet compared with a real run. Exit codes: `0` every test passed; `1` a test `failed`, `unmocked` or `unsupported` (an `unsupported` test failing the gate is intended: what PowerOps can't simulate must not pass); `2` usage: the path or the tests folder not found, the tests folder a file (so a zip path needs `--tests`), no test file found, no solution found; `3` an unreadable solution or test file, found before any test runs, so only the error is printed and no report (one unreadable solution under the path blocks every test, as in `analyze`). Text: one line per test (`PASS`, `FAIL`, `UNMOCKED`, `UNSUPPORTED`, the file and the flow), each failed assertion indented under its test with its index, description, expected and actual, then a count line. Markdown: a summary line and a table, names escaped with `MarkdownReport.Escape`.

**README**: the usage line and a short "Flow tests" section: an example test file, the exit codes, and the limits: written from Learn and not yet compared with a real run (until item 34), the "Not simulated" list, that paths the run doesn't take aren't checked, that every function argument is evaluated (so a guard such as `if(empty(x), '', x['a'])` fails where `x` is empty; use `?` or a separate action), and a pointer to "Not on Learn" in this plan.

**Schemas and `OutputSchemaTests`**: `docs/schemas/test.schema.json` is an output schema, mapped to the goldens `test-*.json` in `Map`. `docs/schemas/flow-test.schema.json` is the schema of an input file, so no golden uses it. `OutputSchemaTests` gets an `Inputs` list, `("flow-test.schema.json", "*.flowtest.json")`: a theory validates every matching file under `tests/fixtures` against it, `Every_schema_is_used_by_a_golden_file` counts an input schema as used when at least one fixture file matches it, and a theory feeds a set of invalid test files, inline strings in `OutputSchemaTests` (not files, which the runner and the first theory would find), to both the schema and `FlowTestFile.Parse` and expects both to reject each one. `docs/schemas/README.md` gets a row for each, the second marked as a file format with a closed schema.

**Goldens**: `test-fixtures.txt`, `.json` and `.md` from `test tests/fixtures/alm-makers --tests tests/fixtures/flowtests/alm-makers` (all pass); `test-failures.txt` and `.json` from a temporary tests folder with fixed file names: a changed expectation (`failed`), a missing mock (`unmocked`) and a flow copy that calls an unknown function, `addHours` (`unsupported`), against a temporary copy of `alm-makers`.

**Not on Learn** (the simulator's choices; each one goes into item 34's capture flow so a real run checks it): the slash path in a property read; `''` inside a string literal; `@@` in the middle of a string; that every argument is evaluated (`if`, `and`, `or` included); `equals` on strings (ordinal, case-sensitive; Learn shows only that `equals(true, 1)` is true, so a boolean equals 1 and 0, and `null` equals `""` per the logical-functions intro), numbers compared by value; property names case-sensitive; `@{...}` and `string()` of a boolean (`True`/`False`, .NET's text), an object (compact JSON) and `null` (`""`, Learn's `string(null)`); `first` and `last` of an empty array or string (`null`); a variable without `value` (`null`); ParseJson's outputs; `outputs()` of a skipped action (an error); the `Skipped` records of nested actions and of an empty Foreach; Until at its count limit (`Succeeded`) and after a failed iteration (`Failed`, the loop stops); Switch case matching; and the block-status rule where Learn's two pages differ. A capture that shows another behaviour is a `fix(core):` change with its test.

## Screens

None. CLI output only.

## Environments and route

- Items 37 to 39: none. They run and are tested offline; the PR's CI is the route.
- Item 34, the capture: Dev, the unmanaged developer environment, planned for 2026-11-21 (at the latest 2026-12-15, when Dev is suspended). Not run in this build: it needs the user's yes to Dev in the session that runs it. In Dev, in a new unmanaged solution, a manually triggered flow made for it: a Dataverse List rows (`$top` 3 on the seed's `pops_workitems` table), a condition on the row count, an Apply to each over the rows with a variable, a Try scope with a Compose that fails (`json('not json')`) and a Catch scope that runs after it on `Failed`, an Until with `limit.count` 2 whose condition never holds, a Switch on a number, a Parse JSON of a JSON string, a Table in CSV and one in HTML, and one Compose per behaviour under "Not on Learn", each after the previous on `Succeeded` and `Failed` so all of them run. Its run writes nothing. The user runs it once from the designer's Test pane (or an agent does, after the user's yes to Dev in that session); its trigger outputs and each action's raw outputs are copied by hand, scrubbed, and committed with its export in `tests/fixtures/flowtests/capture/`, which changes the `analyze-fixtures.*` goldens. A second export with one static result on shows whether an export keeps it. Also export the seed and M12 solutions from Dev, unmanaged and managed, and keep the zips outside Git (BUILD-PLAN, Phase 4).
- The capture's check: its test, with the run's raw outputs as mocks, gives the action statuses the run history showed; the List rows action's `inputs.parameters` equal the parameters the history showed (run history shows a different `host` object from the definition's `inputs`, so the check compares `parameters`, an assumption the capture confirms); and one `outputs` assertion per "Not on Learn" Compose expects the value the run produced.

## Solution and publisher

No new solution in items 37 to 39; they only read the existing fixtures. Item 34's flow goes in its own unmanaged solution in Dev with the seed's publisher (prefix `pops`), so its export is a fixture of its own.

## Constraints

- Open sources only: Learn's WDL schema, action, function and error-handling pages, and the exported fixture flows. Not the Logic Apps Standard test SDK (its licence bars redistribution, decision 17), PowerAutomateMockUp or LogicAppUnit code. No new package.
- Offline and read-only: `powerops test` reads files only, makes no network call and doesn't call `pac`.
- No tenant values in fixtures, test files, goldens or this plan; fake GUIDs `00000000-0000-0000-0000-00000000000n`.
- Licensing: none for items 37 to 39. Item 34's flow uses the Dataverse connector, a premium connector, so its run needs the user's premium licence (the Power Apps Plan 2 trial, end date unchecked).
- Deadline: item 34 by 2026-12-15 (Dev suspended); decision 22 stays open with the user.
- Conventional Commits, one commit per build-order item; no version or changelog edits.

## Assumptions

- `Workflow.ClientData` holds the full `clientdata` JSON with `properties.definition`, as `FlowDefinition.Parse` reads it today, for every layout (XML, zip, YAML).
- The behaviours under "Not on Learn" are as chosen until item 34's capture; a test file written against one of them may need a change after the capture.
- Action names are unique within a definition (the designer enforces it), so a mock or an assertion names one action.

## Open questions

## Questions for your admin

## Steps

1. `docs:` this plan, its decisions and review, signed off (`docs/plans/m14-flowtest/`).
2. `feat(core):` item 37, test-first: `tests/PowerOps.Core.Tests/Flows/ExpressionTests.cs` (the parser cases, `@@` mid-string and `}` inside a literal in `@{...}` included; a theory per function with its Learn examples, each citing the section; the error and `unsupported` cases, format arguments included; the pinned list of 37), then `Flows/Expression.cs` and `Flows/ExpressionFunctions.cs`.
3. `feat(core):` item 38, test-first: `tests/PowerOps.Core.Tests/Flows/FlowSimulatorTests.cs` with inline definitions (each action type and branch, `runAfter` on `Failed` and `Skipped`, block and run status with a handled and an unhandled failure, two parallel branches on one variable, a Foreach with a list mock, an empty Foreach, Until at its limit, Terminate and the actions it skips, nested `Skipped` records, a trigger condition false, two triggers, `result()` in a try/catch, mocked inputs on one call and on every call, a mock on a built-in action, an unresolvable `runAfter`, a cycle, `unmocked` and `unsupported` for each cause, the 100 000-call cap), then `Flows/FlowSimulator.cs`.
4. `feat(cli):` item 39, test-first: `FlowTest.cs` (test-file parse and checks, the runner, parameters, assertions) with Core tests, `Output/TestReport.cs`, `TestCommand.cs` and `TestCommandTests.cs` (each usage and unreadable case, a zip path without `--tests`, two flows of one name), the two schemas and their README rows, the `OutputSchemaTests` changes, the fixture test files and their NOTICE.md entry, the goldens, and the README usage line and "Flow tests" section.
5. `docs:` BUILD-PLAN: the M14 row (37 functions with `and`; `Table` moved to item 34; an action with its static result on needs a mock until item 34; the capture flow's extra cases and second export), the research basis's function count, build-order item 34's text, and the M14 verification-record line (offline items built and checked; item 34 not run yet, planned for 2026-11-21, at the latest 2026-12-15; "not verified" if that date passes); and this plan's status.
6. Item 34 (live, Dev), only after the user's yes to Dev in that session. First check that the premium licence is still valid and that a flow with one Dataverse List rows action runs in Dev (licence and data policy), before building the rest; then the capture under Environments and route, a `test:` commit with the capture fixture and its test, and a `fix(core):` commit for each behaviour it shows differs.
