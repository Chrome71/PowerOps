# Flow tests

A flow test runs a cloud flow offline, with the results of its connector actions given in a test file, and checks the run against assertions.

`test [path] [--tests <dir>] [--format text|json|markdown] [--out <file>]` runs every `*.flowtest.json` file under `--tests` (default: the path) against the cloud flows of the solutions under `path` (default `.`). It is offline and read-only: it reads files, makes no network call and does not call `pac`. A test file names one flow, gives the trigger, the results of the connector actions (mocks) and any fixed values, and lists assertions about the run: its status, an action's status, inputs or outputs, a variable, the response. The simulator runs the flow's actions in order, evaluates its expressions and answers every connector action from the mocks; a connector action without a mock stops the test as `unmocked`. Environment variables take the test's value, else the solution's current value, else its default value.

```json
{
  "$schema": "https://raw.githubusercontent.com/Chrome71/PowerOps/main/docs/schemas/flow-test.schema.json",
  "flow": "Stamp work item status",
  "trigger": { "body": { "pops_workitemid": "00000000-0000-0000-0000-000000000001" } },
  "mocks": { "Update_a_row": { "status": "Succeeded" } },
  "environmentVariables": { "pops_DefaultStatus": "Active" },
  "assertions": [
    { "kind": "runStatus", "equals": "Succeeded" },
    { "kind": "inputs", "action": "Update_a_row", "path": ["parameters", "item/pops_name"], "equals": "Active" }
  ]
}
```

The file format is [docs/schemas/flow-test.schema.json](schemas/flow-test.schema.json); the report (`--format json`) is [docs/schemas/test.schema.json](schemas/test.schema.json). Working examples against the fixture flows are in [tests/fixtures/flowtests/](../tests/fixtures/flowtests/).

In GitHub Actions, the action's `tests` input runs them and can block a merge: see [Merge gate: flow tests](GITHUB-ACTION.md#merge-gate-flow-tests).

Exit codes: `0` every test passed, `1` a test `failed`, `unmocked` or `unsupported`, `2` usage error (path or tests folder not found, the tests location is a file so a zip path needs `--tests`, no test file, no solution), `3` a test file or a solution that cannot be read (every file and solution is read before any test runs, so one bad one blocks them all; only the error is printed, and `POWEROPS_DEBUG=1` gives the stack trace). `unmocked` and `unsupported` fail on purpose: what PowerOps cannot simulate must not pass.

Limits. The simulator is written from Microsoft Learn's pages on the Workflow Definition Language, the action and function references and error handling. It has been compared with one real run: a capture flow of 120 actions, run in a developer environment on 2026-11-05. Its export and run history are in [tests/fixtures/flowtests/capture/](../tests/fixtures/flowtests/capture/), and a test replays them. Every action the simulator runs gave the run's status, its outputs (for Compose and Parse JSON) and its error code. The [capture record](plans/m58-flow-conformance/capture.md) says what was compared and what the capture changed. It is one run of one flow, so a pass here is still not a guarantee about the cloud, and behaviours that neither Learn nor the capture covers are the simulator's own choices: the lists are "Not on Learn" in [the M14 plan](plans/m14-flowtest/plan.md) and [the M58 plan](plans/m58-flow-conformance/plan.md).

- Not simulated: retry policies, concurrency (a Foreach runs one item at a time), timeouts and secure inputs and outputs. `TimedOut` never occurs.
- Only the functions below and the action types listed in the plan are supported; anything else, such as `addHours`, `Select` or `Table`, is `unsupported`. Functions and action types are checked when the run reaches them, so a path the run does not take is not checked: a test of one path can pass while another path holds something unsupported.
- `if` evaluates only the branch it returns, as the capture showed, so `if(empty(x), '', x['a'])` works when `x` is empty. `and` and `or` evaluate every argument (the capture didn't check them), so `and(not(empty(x)), equals(x['a'], 1))` fails when `x` is empty. Use `x?['a']` or a separate action.
- Settled by the capture: `equals` compares a boolean only with a boolean and null only with null, so `equals(true, 1)` and `equals(null, '')` are false; a property name that matches no name exactly matches ignoring case; `@@` is an escape only at the start of a string (`'a@@b'` stays `a@@b`, and `@@{` later in a string is `unsupported`); `first` and `last` of an empty string give `""`; a string variable initialized with no value reads as `""`; `outputs()` of a skipped action gives `null`; a skipped action carries the error code `ActionConditionFailed` (or `ActionBranchingConditionNotSatisfied` in a branch that didn't run), and the actions in a Foreach with no items are skipped; a Scope that fails because of its actions has the error code `ActionFailed`.

## Supported functions

- Workflow: `outputs(action)`, `body(action)`, `triggerOutputs()`, `triggerBody()`, `variables(name)`, `parameters(name)`, `items(loop)`, `item()`, `workflow()`, `result(scope)`
- String: `concat(value, value, ...)`, `substring(text, start, length?)`, `replace(text, old, new)`, `split(text, separator)`, `toLower(text)`, `trim(text)`, `guid()`
- Collection: `contains(collection, value)`, `empty(value)`, `first(collection)`, `last(collection)`, `length(collection)`
- Logical: `equals(value, value)`, `greater(value, value)`, `greaterOrEquals(value, value)`, `lessOrEquals(value, value)`, `if(test, valueIfTrue, valueIfFalse)`, `and(test, ...)`, `or(test, ...)`, `not(test)`
- Conversion: `json(value)`, `string(value)`, `int(value)`, `decodeBase64(text)`, `createArray(value, ...)`
- Math: `min(number, number, ...)`, `min(array)`
- Date and time: `utcNow()`, `formatDateTime(timestamp, format?, locale?)`, `addDays(timestamp, days, format?)`

Forms not listed are `unsupported`, such as `utcNow(format)` and `guid(format)`. The format and locale arguments follow .NET's standard and custom date and time format strings and its culture data, which Learn's reference links to. ICU writes a U+202F before AM/PM in some formats; the simulator writes a space, as Learn's examples show.

- `formatDateTime` takes a timestamp ending in `Z` or with no time zone; a timestamp with an offset is `unsupported`. `addDays` with a format needs a timestamp ending in `Z`. Without a format it keeps the timestamp's zone, as the capture showed: none stays none, `Z` stays `Z`, and an offset is converted to UTC and written as `+00:00`.
- A timestamp must be a full ISO 8601 timestamp (`yyyy-MM-ddTHH:mm`, with optional seconds, fraction and zone, or `yyyy-MM-dd`) or `MM/dd/yyyy` with an optional `HH:mm:ss`. Other text that reads as a date is `unsupported`, in every date function: a space instead of `T` (`2026-11-04 09:00:00`), a one-digit month or day (`3/15/2018`), a lower-case `z` and more than seven fraction digits included.
- The locales are `en-us` (the default), `fr-fr` and `es-es`. With `fr-fr` and `es-es` only day, month and year formats are accepted. Any other locale is `unsupported`, a name .NET does not know included: Learn says that is an error, but the capture's service returned English text.
- An invalid format is an error in `addDays` and `unsupported` in `formatDateTime`.
- These are `unsupported`: the formats `U`, `u`, `r`, `R` and any format with `z` or `K` on a timestamp with no time zone, an empty format, an empty locale, and a .NET that runs without culture data.
- `int` takes a string of digits with an optional `-` and no leading zero, or an integer number, within 32 bits. A decimal string or a string with no digit is an error. Other strings are `unsupported`.

The simulator's other choices are under "Not on Learn" in [the M14 plan](plans/m14-flowtest/plan.md) and [the M58 plan](plans/m58-flow-conformance/plan.md).
