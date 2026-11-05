---
status: approved
review: pending
environment: null
depth: quick
supersedes: null
---

# M58: Pilot-flow conformance: the run's missing functions and a recorded cloud comparison

## Goal

Let `powerops test` run the main path of the flow the live test of 2026-11-04 built, and compare the simulator with a real run for the first time. This is M58 of the improvement plan written after that test (P0). The plan, its reviews and the person's answers to its decisions D1 to D9 are kept outside the repo, because they quote a private test run. The test found that a Microsoft-built flow's first action, `formatDateTime(addDays(utcNow(), int(parameters('...'))), 'yyyy-MM-dd')`, made 2 of 3 flow tests `unsupported` (F4: `formatDateTime` and `int` are not among the 37 functions), that the rewrite with `addDays(utcNow(), parameters('...'), 'yyyy-MM-dd')` was `unsupported` too (F5: the format argument), and that `docs/FLOW-TESTS.md` names `addDays` as supported without saying its third argument is not (`docs/FLOW-TESTS.md:30`). The live flow ran that rewrite on 2026-11-04 with the environment variable's 14 days and wrote 2026-11-18 (the run's record, TST-008). The simulator itself has never been compared with a cloud run (`docs/FLOW-TESTS.md:27`): item 34 of the M14 plan, the capture, never ran (`docs/plans/m14-flowtest/plan.md:125-126`).

The milestone has two parts, each its own PR: part 1 is offline; part 2 is the capture in Dev, after the person's yes in the session that runs it, and after part 1 has merged.

Done when:

1. Part 1: `formatDateTime` (timestamp, optional format, optional locale), `int` and the format argument of `addDays` are simulated as Learn's function reference describes them, each tested against every example in its section, with the choices Learn doesn't make listed under "Not on Learn" below and in `decisions.md`.
2. Part 1: the run's expression, `formatDateTime(addDays(utcNow(), int(parameters('x'))), 'yyyy-MM-dd')`, passes `powerops test` (a CLI test on a copy of the seed fixture) with `utcNow` on 2026-11-04 and 14 days, giving `2026-11-18`, the value the cloud gave; the rewrite with the format argument gives the same.
3. Part 1: `docs/FLOW-TESTS.md` lists every supported function with every argument form it takes, and says which forms are `unsupported`; a test pins that list against `ExpressionFunctions.Names`.
4. Part 1: `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan are clean; the PR is green on all eight checks.
5. Part 2, before 2026-12-15: the capture flow ran once in Dev; its export and its run's raw inputs, outputs and statuses, scrubbed, are in `tests/fixtures/flowtests/capture/` with a `tests/fixtures/NOTICE.md` entry; a test runs the capture's flow tests and compares evaluated inputs, outputs, errors and statuses with the run history, an invalid conversion and a date-boundary case included; the cases the capture marks unsupported (its Table actions, and any action type or function the simulator lacks) are asserted `unsupported`, not matched; every difference found is fixed in the same PR with its test, or listed with its reason; `docs/FLOW-TESTS.md` says what was compared; the leftovers in Dev are listed in `capture.md` in this folder. If the person does not say yes, part 2 is reported as waiting on that yes and nothing is created.

### Won't build

- The wider date set (`addHours`, `addMinutes`, `addSeconds`, `convertTimeZone`, `convertFromUtc`, `startOfDay`, `dayOfWeek`) and `float`: M59. `utcNow` with a format argument stays `unsupported` (Learn gives it the same argument as `addDays`; M59 can take it with the others).
- `parseDateTime`, `formatNumber`, `formatTimeSpan`: not in the run, not in M59.
- A change to how `addDays` reads a timestamp without a time zone, or to its refusal of fractional days: both are M14 choices that the capture checks (decisions Q8, Q9). A capture that shows another behaviour is a `fix(core):` change in part 2.
- Deleting anything in Dev. Leftovers are listed, not removed.

## Users

- A flow maker or an agent who writes flow tests for a flow the designer or FlowAgent built, and runs `powerops test` locally or as the Action's merge gate.
- A reviewer who reads `docs/FLOW-TESTS.md` to know whether a flow's expressions can be simulated before writing tests for it.

## Data

Line numbers are those of `b8ad904`.

| # | Severity | Change | Where | Test |
|---|---|---|---|---|
| 1 | medium | `formatDateTime(timestamp, format?, locale?)`. The timestamp is a string, read with the invariant culture and `DateTimeStyles.AdjustToUniversal`: text with `Z` or an offset becomes UTC, text with neither keeps no zone, so `'03/15/2018'` gives Learn's `'2018-03-15T00:00:00.0000000'` with no `Z`. Format defaults to `o`; locale defaults to `en-us` and is looked up with `CultureInfo.GetCultureInfo(name, predefinedOnly: true)`, an unknown name being an error (Learn: "If locale isn't a valid value, an error is generated") | `src/PowerOps.Core/Flows/ExpressionFunctions.cs`, the `Table` (`:22-73`) and a helper | `ExpressionTests`: the six examples of Learn's `formatDateTime` section, the date-time page's examples (`g`, `D`, `M/dd/yyyy h:mm tt`, `M/dd/yyyy HH:mm:ss tt`, `yyyy/MM/dd hh:mm:ss tt`), the cookbook's `MMMM dd, yyyy`; errors: a non-string or unreadable timestamp, an invalid format (`'X'`), an unknown locale, 0 or 4 arguments; `unsupported`: an empty format or locale, `U` or a `z` format on a timestamp with no zone |
| 2 | medium | One formatting helper for `formatDateTime` and `addDays`: `DateTime.ToString(format, culture)`; a `FormatException` is an `ExpressionException` naming the format (Learn's `addDays`: "If the format isn't a valid value, an error is generated"); an empty format is `unsupported`; on a timestamp with no zone, `U` and any format with `z` are `unsupported`, because .NET takes the machine's time zone for them; ICU's narrow no-break space (U+202F) before AM/PM becomes a space, as Learn's examples and the cloud show it (decisions Q5); the result goes through `CheckLength` and `ChargeCopy`, as `concat` does | `ExpressionFunctions.cs`, next to `Format` (`:369`) | covered by rows 1 and 3, and one case that `g` has a plain space on every platform |
| 3 | medium | `addDays(timestamp, days, format?)`: the third argument formats the result with the row 2 helper and the `en-us` culture (decisions Q4); the timestamp and days are read as today (`:374-395`) | `ExpressionFunctions.cs` `AddDays` | `ExpressionTests`: `addDays('2018-03-15T00:00:00Z', 10, 'yyyy-MM-dd')`, a year end (`'2026-12-31T23:00:00Z', 1`), a leap day (`'2028-02-28T00:00:00Z', 1`), `'D'`, an invalid format (error), a non-string format (error); the test at `ExpressionTests.cs:784` that expects `unsupported` is replaced |
| 4 | medium | `int(value)`: a string of an optional sign and digits that fits a 64-bit integer gives that number; any other string is an error (Learn's cookbook: "`int()` on a decimal string throws an error; it doesn't truncate"); an integer number gives itself (not on Learn, decisions Q1); any other value (a decimal number, a boolean, null, an object, an array) is `unsupported` | `ExpressionFunctions.cs` `Table`, Conversion | `ExpressionTests`: `int('10')` (Learn), `int('42')`, `int('-7')`, `int(14)`; errors `int('3.14')`, `int('abc')`, `int('')`, `int(' 1')`, `int('99999999999999999999')`; `unsupported` `int(1.5)`, `int(true)`, `int(null)` |
| 5 | low | The pinned list: `Names_are_exactly_the_37_functions_sorted` becomes 39 with `formatDateTime` and `int`; the summary at `ExpressionFunctions.cs:8` says 39 | `tests/PowerOps.Core.Tests/Flows/ExpressionTests.cs:789-801`, `ExpressionFunctions.cs:8` | the test itself |
| 6 | high | The run's expression through the CLI: a copy of `tests/fixtures/powerops-seed/v2` whose `Update_a_row` sets `item/pops_name` to `@formatDateTime(addDays(utcNow(), int(parameters('Default Status (pops_DefaultStatus)'))), 'yyyy-MM-dd')`, and a second copy with `@addDays(utcNow(), int(parameters('Default Status (pops_DefaultStatus)')), 'yyyy-MM-dd')`; a test file with `utcNow` `2026-11-04T09:00:00Z` and `pops_DefaultStatus` `"14"` asserts the input `2026-11-18`, exit 0. The seed's parameter is a String, so `int` gets a string; a Core test runs the same expression with an `Int` parameter, so `int` gets a number | `tests/PowerOps.Cli.Tests/TestCommandTests.cs`, `tests/PowerOps.Core.Tests/Flows/FlowTestTests.cs` | the two tests |
| 7 | medium | `docs/FLOW-TESTS.md`: the limits paragraph (`:30`) lists the 39 functions by group with each argument form (for example `addDays(timestamp, days, format?)`, `utcNow()` only, `guid()` only, `substring(text, start, length?)`), says the format and locale arguments follow .NET's format strings and culture data, which Learn links to, and that a U+202F before AM/PM is written as a space; a pointer to this plan's "Not on Learn" next to M14's | `docs/FLOW-TESTS.md:27-32` | a new `ExpressionTests` case next to the pinned list: every name in `ExpressionFunctions.Names` starts an entry of the list in code format, and no other entry does |
| 8 | medium | Part 2, the capture (see "Environments and route"): its unpacked export and the scrubbed raw outputs in `tests/fixtures/flowtests/capture/`, with `NOTICE.md`; flow tests in that folder, built from the run's raw outputs as mocks, assert each action's status and each Compose's outputs as the run gave them; the expected-unsupported cases sit in `capture/unsupported/` and a test asserts each is `unsupported`; the `analyze-fixtures.*` goldens change by the recipe | `tests/fixtures/flowtests/capture/`, `tests/fixtures/NOTICE.md`, `tests/PowerOps.Cli.Tests/TestCommandTests.cs`, `tests/golden/analyze-fixtures.*` | the capture test, `ReportTests`, `OutputSchemaTests` |
| 9 | medium | Part 2 docs: `docs/FLOW-TESTS.md:27` says what the capture compared and on which date, and that the rest of the simulator is still from Learn alone; each "Not on Learn" item the capture confirmed or corrected is marked here and in M14's list | `docs/FLOW-TESTS.md:27`, this plan | `DocsDriftTests` if it covers the page |

### Not on Learn

The simulator's choices in part 1, each one a case in the part 2 capture:

- `int` of an integer number gives that number: in the run, `addDays` took `parameters('...')` directly as its Integer days, so the environment variable most likely gave `int` a number. `int` of any other non-string value is `unsupported`. A string is read by .NET's 64-bit integer parse with only a leading sign allowed, so `' 1'` is an error, `'007'` is 7 and `'+5'` is 5.
- A timestamp with an offset is converted to UTC before formatting, as `addDays` already does (`ExpressionTests.cs:767`).
- `addDays`'s format uses the `en-us` culture, as `utcNow`'s format examples on Learn show (`'d'` gives `9/25/2025`, `'D'` gives `Thursday, September 25, 2025`).
- U+202F before AM/PM is written as a space.
- An empty format or locale, and `U` or `z` formats on a timestamp with no zone, are `unsupported`.
- Culture data: .NET reads it from ICU on Linux and Windows; a locale whose ICU data differs from the service's can give other text. Learn's `fr-fr` and `es-es` examples match on .NET 10 with the ICU of this machine.

## Automation

None new. `powerops test` and the Action's merge gate run the new functions with no change to their interface.

## Screens

None.

## Environments and route

Part 1 reads and writes no tenant; the PR's CI is the route.

Part 2 runs in Dev, the unmanaged developer environment (not the trial Test environment), before 2026-12-15, when unmanaged developer environments are suspended. Before it creates, changes or runs anything there, the session asks the person in chat, naming exactly what it will create. The person does every sign-in (never a device code). The session deletes nothing in the tenant. What it creates, once the person says yes:

- An unmanaged solution `PowerOpsCapture` with the seed's publisher (prefix `pops`), holding: one cloud flow, "PowerOps capture", manually triggered; one Number environment variable `pops_CaptureDays` with default 14; one Dataverse connection reference, using an existing Dataverse connection of the person's (the session creates no connection; if none exists, it asks).
- The flow, as M14 item 34 designed it, every action after the previous on `Succeeded` and `Failed` so all of them run, and writing nothing: a Dataverse List rows (`$top` 3 on the seed's `pops_workitems` table), a condition on the row count, an Apply to each over the rows with a variable, a Try scope with a Compose that fails (`json('not json')`) and a Catch scope that runs after it on `Failed`, an Until with `limit.count` 2 whose condition never holds, a Switch on a number, a Parse JSON of a JSON string, a Table in CSV and one in HTML, one Compose per item under "Not on Learn" in the M14 plan and in this plan, and these M58 cases: the run's expression with `parameters('pops_CaptureDays')`; the examples of rows 1 and 3; `formatDateTime` of a timestamp with an offset, with no zone and `'o'`, and with no zone and `'zzz'`; `int` of a decimal string and of a word, each taken from a Compose output so the error comes at run time (the cookbook: a constant `int('3.14')` fails when the flow is saved); `addDays` of a timestamp with no zone, with fractional days from a Compose, and across a year end with a format.
- One run from the designer's Test pane by the person, or by the session through the Power Automate tools after the person's yes; its trigger outputs and each action's raw inputs and outputs are read, scrubbed (no URL, environment or tenant id, user name, email, connection id, tracking id or run id; fake GUIDs `00000000-0000-0000-0000-00000000000n`), and committed.
- Exports with `pac solution export`, unmanaged, unpacked with `pac solution unpack`; a second export with a static result on one action shows whether an export keeps it (the M14 question). The seed and M12 solutions are exported from Dev too, unmanaged and managed, and kept outside Git, as M14 asked.
- `capture.md` in this folder: the date, what was created, the run's statuses, the comparison's result, and every leftover in Dev (solution, flow, environment variable, connection reference, runs).

## Solution and publisher

None in part 1. Part 2: the unmanaged `PowerOpsCapture` in Dev, publisher prefix `pops`, so its export is a fixture of its own.

## Constraints

- AGENTS.md: smallest change, new logic with tests, Conventional Commits, no version or changelog edits, no tenant URLs, ids, emails or user names in commits or fixtures. No write is added, so `SafeWrite` is not touched.
- Open sources only: Learn's function reference (`azure/logic-apps/expression-functions-reference`), the Power Automate date-time page and expression cookbook, .NET's format-string pages that Learn links to. No decompiling, no Logic Apps test SDK.
- The pinned roles `powerops-verify` and `powerops-review-security` and `tests/eval` are untouched.
- Windows and Linux. Culture data comes from ICU on both; the tests use `en-us`, `fr-fr`, `fr-FR` and `es-es` only, with Learn's examples.
- Other P0 sessions run in parallel (M50 diff/analyze/drift, M54 skills, M56 releases); this milestone keeps to `Flows/`, its tests, `FLOW-TESTS.md`, the seed copy in a temp folder and the new fixture folder.

## Assumptions

- Learn, read 2026-11-04 with the Learn MCP tools. Function reference: `formatDateTime('<timestamp>', '<format>'?, '<locale>'?)`; format "either a single format specifier or a custom format pattern", default `o`; locale default `en-us`, invalid is an error; examples `formatDateTime('03/15/2018')` gives `'2018-03-15T00:00:00.0000000'`, `'03/15/2018 12:00:00', 'yyyy-MM-ddTHH:mm:ss'` gives `'2018-03-15T12:00:00'`, `'01/31/2016', 'dddd MMMM d'` gives `'Sunday January 31'`, with `'fr-fr'` and `'fr-FR'` `'dimanche janvier 31'`, with `'es-es'` `'domingo enero 31'`. `addDays('<timestamp>', <days>, '<format>'?)`, days an Integer, format as above, "If the format isn't a valid value, an error is generated". `int('<value>')`, a String, "the integer version for the specified string", example `int('10')` gives `10`; the limits are "the limits for the integer data type". `utcNow` format examples: `'d'` gives `9/25/2025`, `'t'` gives `5:18 AM`.
- Power Automate "Customize or format date and time values": `'g'` gives `6/15/2009 1:45 PM`, `'D'` gives `Monday, June 15, 2009`, and the three custom examples of row 1. Expression cookbook: `int()` on a decimal string is an error, a constant one at save time; `formatDateTime('2026-03-18T14:30:00Z', 'MMMM dd, yyyy')` gives `March 18, 2026`; `addDays()` "also accepts fractional values" (the reference says Integer; decisions Q9). The cookbook says it was written with AI assistance.
- .NET 10 on this machine (ICU) reproduces every example above, except that `g`, `t`, `T` and `U` put U+202F before AM/PM, and that `U` and `z` formats on a timestamp with no zone take the machine's time zone. Checked 2026-11-04 with a scratch program.
- The run's record (outside the repo): the flow's environment variable had the default 14 in Dev (its type is not recorded); the rewritten flow, `addDays(utcNow(), parameters('...'), 'yyyy-MM-dd')`, wrote 2026-11-18 on a run of 2026-11-04.

## Open questions

## Questions for your admin

## Steps

1. This plan and `decisions.md`, a fresh-context review into `review.md`, fixes, and sign-off under the delegation of 2026-10-22 once no High or Medium finding is open.
2. Part 1, test first: rows 1 to 5, then row 6, then row 7.
3. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .`, the leak scan; push, PR, CI until all eight checks pass. Not merged by this session.
4. Part 2, only after the person's yes in chat: the capture in Dev, then rows 8 and 9 in a second PR based on `main` once part 1 has merged; `capture.md`.
