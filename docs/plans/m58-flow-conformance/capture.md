# M58 capture: the simulator against one cloud run

Part 2 of [the plan](plan.md), run on 2026-11-05 (UTC) by the building session after the person's yes in chat ("Yes, go ahead in Dev"). The person did no sign-in: `pac` and the Power Automate tools were already signed in.

## Where and what

- Dev: "PowerOps Test" in `pac`. `pac admin list` gave its type as Developer; the trial environment ("PowerOps Trial Test") was not touched. `pac solution list` showed the seed (`PowerOpsSeed` 1.0.0.3), so the `pops` publisher and the `pops_workitems` table were there. One existing Dataverse connection, Connected, owned by the person.
- Created, all in one unmanaged solution, `PowerOpsCapture` 1.0.0.0, publisher `powerops` (prefix `pops`):
  - the cloud flow "PowerOps capture", manually triggered, 120 actions (105 at the top level), with a fake workflow id chosen before the import (the one in the fixture's file names);
  - the Number environment variable `pops_CaptureDays`, default 14, no current value; the flow's parameter is `Float`, as the designer wrote it for the live test's Number variable;
  - the connection reference `pops_CaptureDataverse`, bound to the person's existing Dataverse connection by a deployment settings file.
- How: the solution was written as files in the export layout (not `pac solution pack`'s unpacked layout, a small departure from the plan's route), zipped, and imported with `pac solution import --settings-file … --activate-plugins`. The flow came up on.
- The run: one run, started by the session through the Power Automate tools. It took about 6 seconds and ended `Succeeded`, with every action recorded. The List rows action returned no rows, so nothing from the table is in the record.
- The record: every action's status, code, error, inputs and outputs, read through the Power Automate management API with the person's existing `az` sign-in (read-only), scrubbed into `tests/fixtures/flowtests/capture/run.json` (see `tests/fixtures/NOTICE.md`). Two outputs (`M15`, `M19`) were 4 bytes but came back empty when downloaded; they are recorded as `null`, the only 4-byte JSON a Compose can give there.
- The export: `pac solution export`, unmanaged, unpacked with `pac solution unpack` into `tests/fixtures/flowtests/capture/solution/`.
- Then a static result on `List_rows`, set by an update of its definition (`staticResults` and the action's `runtimeConfiguration.staticResult`), and a second export.
- Also exported, managed and unmanaged, and kept outside the repository with the maintainer's notes: `PowerOpsSeed` and `PowerOpsM12Visits`, as M14 asked. `EquipmentCheckout`, the live test's solution, was exported once to scratch, read-only, to see how the designer types a Number environment variable.

## What the test compares

`tests/PowerOps.Core.Tests/Flows/CaptureTests.cs` runs the exported flow through the simulator with the run's List rows outputs as its mock and the run's day as `utcNow`. The 29 actions in `expected-unsupported.json` are taken out first, because the simulator stops a run at the first action it refuses. Then, action by action, it compares:

- the status;
- the error code of every failed or skipped action (not the message: the service's text differs);
- the outputs of every Compose and Parse JSON;
- the List rows action's `parameters`, apart from `accept`, which the service adds;
- for `result('Try')`, each item's name, status and error code.

The inputs and outputs of the other action types are the service's own run-history shapes (`{"foreachItems": []}`, `{"expressionResult": 2}`), so only their status and error code are compared. Separately, each of the 29 actions is run on its own (after `Values`) and must be `unsupported`.

The plan said "flow tests in that folder" (`*.flowtest.json`). A flow test runs the whole flow, and the first refused action would stop it, so the comparison is this data-driven test instead (decisions Q19).

## Results

Matched as the simulator had it:

- Every `formatDateTime` case of Learn, the date-time page and the cookbook (`F01` to `F17`), including the two-digit `hh` (`2009/06/15 01:45:30 PM`) and the plain space before AM/PM.
- Every `addDays` case with a format on a `Z` timestamp (`A01` to `A05`). The invalid format is an error (`A07`), and fractional days are an error (`A10`: "expects its second parameter to be an integer"). The cookbook's "also accepts fractional values" is wrong for this service, and the reference's Integer holds (decisions Q9).
- `int('10')`, `int('-7')`, `int` of the `Float` parameter and of the number 14 (`I01` to `I04`). The decimal strings, a word and an empty string are errors (`I05` to `I07`, `I15`). So decisions Q1 holds.
- The run's expression: `R01` (`formatDateTime(addDays(utcNow(), int(parameters(...))), 'yyyy-MM-dd')`), `R02` (`addDays(..., parameters(...), 'yyyy-MM-dd')`) and `R03` (the live flow's current `substring` form) all gave the run's day plus 14 (`2026-11-19`).
- Of M14's "Not on Learn" list: the slash path in a property read (`M01`, also on a Compose output), `''` in a literal (`M02`), `equals` on strings, ordinal and case-sensitive (`M05`), numbers by value (`M08`), `True` and `False` from `@{...}` and `string()` (`M10`, `M11`), compact JSON for an object (`M12`, `M13`), `string(null)` giving `""` (`M14`), `first` of an empty array giving `null` (`M15`), and Parse JSON's outputs (`M18`). Also, as the test compares them: the Until at its count limit `Succeeded` after 2 rounds, the Switch on a number matched its case, and the Try scope failed and its Catch ran.

Corrected in the simulator, each with a unit test (`fix(core):` commits in the part 2 PR):

| Case | Cloud | Simulator before |
|---|---|---|
| `A06` `addDays('2018-03-15', 1)` | `2018-03-16T00:00:00.0000000`, no zone | `…Z` |
| `A11` `addDays('2018-03-15T10:30:00+02:00', 0)` | `2018-03-15T08:30:00.0000000+00:00` | `…08:30…Z` |
| `F18` locale `xx-YY` | English text, no error | an error (Learn says error); now `unsupported` |
| `M03` `"mail a@@b"` | `mail a@@b` | `mail a@b` (`@@` is now an escape only at the start) |
| `M04` `if(equals(1, 1), 'yes', json('not json'))` | `yes` | failed: every argument was evaluated |
| `M06` `equals(true, 1)` | `false` | `true` (Learn's example says true) |
| `M07` `equals(null, '')` | `false` | `true` (Learn's intro says null converts to `""`) |
| `M09` `?['OBJ']` on `{"obj": …}` | the `obj` value | `null`: names matched case-sensitively |
| `M16` `last('')` | `""` | `null` (`first('')` follows; not captured) |
| `M17` a string variable with no value | `""` | `null` |
| `M19` `outputs()` of a skipped action | `null`, the action `Succeeded` | an error |
| `Empty_inner`, `Append_name`, `Increment_count` | `Skipped`, `ActionConditionFailed` (a Foreach with no items) | no record |
| `Try_next`, `Never_compose`, `Rows_found`, `Was_1`, `Was_default` | `Skipped` with `ActionConditionFailed` or `ActionBranchingConditionNotSatisfied` | no error code |
| `Try` | `Failed`, `ActionFailed` | no error code |
| `Until_step`, `Increment_count` | `IncrementVariable` ran | `unsupported`: the action type was missing; added from Learn |

Refused by the simulator (asserted `unsupported`), with what the service gave. These are candidates for M59 to support from this record:

| Action | Expression | Why refused | Cloud |
|---|---|---|---|
| `U01` | `formatDateTime(offset, 'HH:mm')` with `+02:00` | an offset | `08:30` |
| `U02` | `formatDateTime('…10:30:00+00:00', 'HH:mm')` | an offset | `10:30` |
| `U03` | `formatDateTime(offset)` with `+02:00` | an offset | `2018-03-15T08:30:00.0000000+00:00` |
| `U04` | `formatDateTime('03/15/2018', 'zzz')` | `zzz`, no zone | `+00:00` |
| `U05` | `… 'u'` | `u`, no zone | `2018-03-15 00:00:00Z` |
| `U06` | `… 'yyyy K'` | `K`, no zone | `2018 ` |
| `U07` | `… 'U'` | `U`, no zone | `Thursday, March 15, 2018 12:00:00 AM` |
| `U18` | `… 'r'` | `r`, no zone | `Thu, 15 Mar 2018 00:00:00 GMT` |
| `U08` | format `X` | invalid `formatDateTime` format | error `InvalidTemplate` |
| `U09` | locale `de-DE` | a locale Learn doesn't show | `Sonntag Januar 31` |
| `U11` | empty format | empty | `3/15/2018 12:00:00 AM` |
| `U12` | empty locale with `o` | empty | `2018-03-15T00:00:00.0000000Z` |
| `U13` | `'g'` with `fr-fr` | beyond day, month and year | `15/06/2009 13:45` |
| `U14` | `formatDateTime('10:30')` | partial | `2026-11-05T10:30:00.0000000` (the run's day) |
| `U15` | `formatDateTime('March 15')` | partial | `2026-03-15T00:00:00.0000000` (the run's year) |
| `U16` | `formatDateTime('3/15/2018')` | one-digit month | `2018-03-15T00:00:00.0000000` |
| `U17` | `formatDateTime('2018-03-15 10:30:00')` | space for `T` | `2018-03-15T10:30:00.0000000` |
| `I08` to `I11`, `I13` | `int(' 1')`, `int('+5')`, `int('007')`, `int('2147483648')`, `int('1,000')` | not a plain 32-bit integer | `1`, `5`, `7`, `2147483648`, `1000` |
| `I12`, `I14` | `int('12abc')`, `int(1.5)` | not a plain integer | error `InvalidTemplate` |
| `A08` | `addDays('2018-03-15', 1, 'yyyy-MM-dd')` | no zone, with a format | `2018-03-16` |
| `A09` | `addDays(offset, 0, 'HH:mm')` | an offset, with a format | `08:30` |
| `Table_csv` | Table, CSV | the action type | `a,b\r\n1,"x,y"\r\n2,<z>\r\n` (a comma is quoted; `<` is not escaped) |
| `Table_html` | Table, HTML | the action type | `<table><thead>…<td>&lt;z&gt;</td>…</table>` (`<` is escaped) |

What the values show together: the service reads a timestamp with `.NET`'s `DateTime` parse on a computer whose time zone is UTC. An offset is converted to that zone and labelled `+00:00`, no zone stays none, and `zzz` and `U` on a timestamp with no zone act as UTC. A partial timestamp takes today's date, and `int` reads a 64-bit integer with spaces, a sign, leading zeros and thousands separators allowed.

Other findings:

- An export rewrites a date-time string literal in a flow definition. The imported definition and the run had `"offset": "2018-03-15T10:30:00+02:00"` in the `Values` Compose; both exports have `2018-03-15T08:30:00+00:00`. A definition read from an export is therefore not always the text that runs. The test checks this one value on its own.
- An export keeps an enabled static result: the second export has `staticResults` and the action's `runtimeConfiguration.staticResult`. M14 left a rule for "an action shipped with its static result on" as its own task, if this held. It holds, so the rule is a follow-up.
- The service's run history adds `accept: application/json;odata.metadata=minimal` to a Dataverse action's parameters and replaces `host` with its runtime ids. The comparison of `parameters` leaves out `accept`; M14 assumed `parameters` would be equal.
- Learn's statements that the capture contradicts: `equals(true, 1)` is false, an unknown locale is not an error, and `null` is not `""` for `equals`. The cookbook's fractional `addDays` days is an error. These are listed so a later reader trusts the record over the page.

## M14 and M58 "Not on Learn" items

| Item | State |
|---|---|
| slash path in a property read | confirmed |
| `''` in a string literal | confirmed |
| `@@` in the middle of a string | corrected: kept as `@@` |
| every argument evaluated (`if`, `and`, `or`) | corrected for `if`; `and` and `or` not captured, still evaluate all |
| `equals` on strings, ordinal and case-sensitive | confirmed |
| a boolean equals 1 and 0 | corrected: false |
| `null` equals `""` | corrected: false |
| numbers compared by value | confirmed |
| property names case-sensitive | corrected: an exact match first, else ignoring case |
| `@{...}` and `string()` of a boolean, an object, `null` | confirmed |
| `first` and `last` of an empty array or string | array: confirmed (`null`); string: corrected (`""`) |
| a variable without `value` | corrected for a string (`""`); other types not captured |
| Parse JSON's outputs | confirmed |
| `outputs()` of a skipped action | corrected: `null` |
| `Skipped` records of nested actions and of an empty Foreach | corrected: an empty Foreach's actions are skipped, with error codes |
| Until at its count limit | confirmed (`Succeeded`) |
| Switch case matching | confirmed for a number |
| the block-status rule | confirmed for the two scopes here: `Try` failed (a failed action, the next one skipped), `Mixed` succeeded (a failed action handled by one that ran after it on `Failed`) |
| M58: `int` of an integer number gives itself | confirmed |
| M58: `addDays`'s format uses `en-us` | confirmed (`D`, `t`) |
| M58: U+202F before AM/PM written as a space | confirmed (`g`, `t`) |
| M58: what part 1 refuses | recorded above; still refused |

## Leftovers in Dev

Nothing was deleted. Left in "PowerOps Test":

- the unmanaged solution `PowerOpsCapture` 1.0.0.0;
- the cloud flow "PowerOps capture", on, with a static result enabled on `List_rows` since the second export (a run now gets its static result, not Dataverse rows);
- the environment variable `pops_CaptureDays` (default 14, no current value);
- the connection reference `pops_CaptureDataverse`, bound to the person's existing Dataverse connection;
- the run of 2026-11-05 in the flow's run history;
- the solution export jobs of the five exports.

Outside the tenant: FlowAgent keeps its own backup of the flow before the static-result edit, in its local cache.
