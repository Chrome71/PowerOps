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

Limits. The simulator is written from Microsoft Learn's pages on the Workflow Definition Language, the action and function references and error handling. It has not been compared with a real run yet, so a pass here is not a guarantee about the cloud, and some behaviours Learn does not describe are the simulator's own choices: the list is "Not on Learn" in [the plan](plans/m14-flowtest/plan.md).

- Not simulated: retry policies, concurrency (a Foreach runs one item at a time), timeouts and secure inputs and outputs. `TimedOut` never occurs.
- Only 37 functions and the action types listed in the plan are supported; anything else, such as `addHours`, `Select` or `Table`, is `unsupported`. Functions and action types are checked when the run reaches them, so a path the run does not take is not checked: a test of one path can pass while another path holds something unsupported.
- Every function argument is evaluated, `if`, `and` and `or` included. A guard such as `if(empty(x), '', x['a'])` fails when `x` is empty. Use `x?['a']` or a separate action.
