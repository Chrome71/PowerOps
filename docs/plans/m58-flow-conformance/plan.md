---
status: approved
review: pending
environment: PowerOps Test
depth: quick
supersedes: null
---

# M58: Pilot-flow conformance: the run's missing functions and a recorded cloud comparison

## Goal

Let `powerops test` run the main path of the flow the live test of 2026-11-04 built, and compare the simulator with a real run for the first time. This is M58 of the improvement plan written after that test (P0). The plan, its reviews and the person's answers to its decisions D1 to D9 are kept outside the repo, because they quote a private test run. The test found that a Microsoft-built flow's first action, `formatDateTime(addDays(utcNow(), int(parameters('...'))), 'yyyy-MM-dd')`, made 2 of 3 flow tests `unsupported` (F4: `formatDateTime` and `int` are not among the 37 functions), that the rewrite with `addDays(utcNow(), parameters('...'), 'yyyy-MM-dd')` was `unsupported` too (F5: the format argument), and that `docs/FLOW-TESTS.md` names `addDays` as supported without saying its third argument is not (`docs/FLOW-TESTS.md:30`). The live flow ran that rewrite on 2026-11-04 with the environment variable's 14 days and wrote 2026-11-18 (the run's record, TST-008). The simulator itself has never been compared with a cloud run (`docs/FLOW-TESTS.md:27`): item 34 of the M14 plan, the capture, never ran (`docs/plans/m14-flowtest/plan.md:125-126`).

The milestone has two parts, each its own PR: part 1 is offline; part 2 is the capture in Dev ("PowerOps Test" in `pac`, the developer environment, not the trial Test environment), after the person's yes in the session that runs it, and after part 1 has merged.

The rule from M14 holds: what Learn doesn't settle is `unsupported`, never a guessed value. Part 1 returns a value only where Learn states it or where the run's own expression needs it (decisions Q1); everything else Learn leaves open is `unsupported` until the capture shows the service's behaviour.

Done when:

1. Part 1: `formatDateTime` (timestamp, optional format, optional locale), `int` and the format argument of `addDays` are simulated where Learn's function reference, the Power Automate date-time page or the cookbook state a result, each tested against those examples (the date-time page's `hh` example as .NET's format reference gives it, see Assumptions), and every form they leave open is `unsupported`, as listed under "Not on Learn" below and in `decisions.md`.
2. Part 1: two CLI tests on copies of the seed fixture pass `powerops test` with `utcNow` `2026-11-04T09:00:00Z` and 14 days, asserting the input `item/pops_name` of `Update_a_row` is `2026-11-18`: the run's original expression `formatDateTime(addDays(utcNow(), int(parameters('x'))), 'yyyy-MM-dd')`, and the rewrite with the format argument. The rewrite's value is the cloud's (the run wrote 2026-11-18); the original never ran in the cloud, and its `int` of a number is the simulator's reading (decisions Q1) until part 2 runs it. A Core test runs both with an `Int` parameter, so `int` gets a number as well as a string.
3. Part 1: `docs/FLOW-TESTS.md` lists every supported function with every argument form it takes, and says which forms are `unsupported`; a test pins that list against `ExpressionFunctions.Names`.
4. Part 1: `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan are clean; the PR is green on all eight checks.
5. Part 2, before 2026-12-15: the capture flow ran once in Dev; its export and its run's inputs, outputs and statuses, scrubbed, are in `tests/fixtures/flowtests/capture/` with a `tests/fixtures/NOTICE.md` entry; a test runs the capture's flow tests and compares, action by action, the status, the evaluated inputs and outputs, and for a failed action its error code (not the message text, which differs between the service and the simulator) with the run history, an invalid conversion and a date-boundary case included; the cases the simulator can't run (its Table actions, and every form part 1 makes `unsupported`) are asserted `unsupported`, not matched, and their cloud values are recorded in `capture.md`; every other difference is fixed in the same PR with its test, or listed with its reason; `docs/FLOW-TESTS.md` says what was compared; the leftovers in Dev are listed in `capture.md` in this folder. If the person does not say yes, part 2 is reported as waiting on that yes and nothing is created.

### Won't build

- The wider date set (`addHours`, `addMinutes`, `addSeconds`, `convertTimeZone`, `convertFromUtc`, `startOfDay`, `dayOfWeek`) and `float`: M59. `utcNow` with a format argument stays `unsupported` (Learn gives it the same argument as `addDays`; M59 can take it with the others).
- `parseDateTime`, `formatNumber`, `formatTimeSpan`: not in the run, not in M59.
- A change to how `addDays` without a format reads a timestamp without a time zone, or to its refusal of fractional days: both are M14 choices that the capture checks (decisions Q8, Q9). A capture that shows another behaviour is a `fix(core):` change in part 2.
- Deleting anything in Dev. Leftovers are listed, not removed.

## Users

- A flow maker or an agent who writes flow tests for a flow the designer or FlowAgent built, and runs `powerops test` locally or as the Action's merge gate.
- A reviewer who reads `docs/FLOW-TESTS.md` to know whether a flow's expressions can be simulated before writing tests for it.

## Data

Line numbers are those of `b8ad904`.

| # | Severity | Change | Where | Test |
|---|---|---|---|---|
| 1 | medium | `formatDateTime(timestamp, format?, locale?)`. The timestamp is a string read with the invariant culture and `DateTimeStyles.RoundtripKind`: text ending in `Z` is UTC, text with no zone keeps none (Learn: `'03/15/2018'` gives `'2018-03-15T00:00:00.0000000'`), text with any other offset (`+00:00` included) is `unsupported`, because Learn says the default format "preserves time zone information" without saying what other formats do with the offset. Format defaults to `o`. Locale defaults to `en-us`; `en-us`, `fr-fr` and `es-es` (any case) are accepted, as Learn shows them; a name .NET doesn't know is an error (Learn: "If locale isn't a valid value, an error is generated"); any other name .NET knows is `unsupported` | `src/PowerOps.Core/Flows/ExpressionFunctions.cs`, the `Table` (`:22-73`) and helpers | `ExpressionTests`: the six examples of Learn's `formatDateTime` section, the date-time page's examples (`g`, `D`, the three custom ones), the cookbook's `MMMM dd, yyyy`; a `Z` timestamp with `o`, `zzz` and `U`; errors: a non-string or unreadable timestamp, an unknown locale (`xx-YY`), 0 or 4 arguments; `unsupported`: an offset (`+02:00`, `+00:00`), a known locale Learn doesn't show (`de-DE`), an empty format or locale, an invalid format (`X`), and the zone formats of row 2 on a timestamp with no zone |
| 2 | medium | One formatting helper for `formatDateTime` and `addDays`: `DateTime.ToString(format, culture)`. An invalid format (`FormatException`) is an error in `addDays` (Learn: "If the format isn't a valid value, an error is generated") and `unsupported` in `formatDateTime`, whose section has no such sentence. An empty format is `unsupported`. On a timestamp with no zone, `U`, `u`, `r`, `R` and any format holding `z` or `K` are `unsupported`: .NET takes the machine's zone for `U` and `z`, and the service's label for the others is not on Learn (`o` stays, Learn's example). ICU's narrow no-break space (U+202F) before AM/PM becomes a space, as Learn's examples show (decisions Q5). On a .NET without culture data (invariant globalization) every call that needs a culture is `unsupported`, instead of the type failing to load. The result goes through `CheckLength` and `ChargeCopy`, as `concat` does | `ExpressionFunctions.cs`, next to `Format` (`:369`) | covered by rows 1 and 3, and one case that `g`, `t`, `T` and `U` have no U+202F; invariant mode checked by hand once (a test process can't switch it) |
| 3 | medium | `addDays(timestamp, days, format?)`: with a format, the timestamp must end in `Z` (read as in row 1; no zone or an offset is `unsupported`, because `addDays` without a format reads no zone as UTC, an M14 choice that row 1's reading would contradict); the result is formatted with row 2 and the `en-us` culture (decisions Q4). Without a format, nothing changes (`:374-395`) | `ExpressionFunctions.cs` `AddDays` | `ExpressionTests`: `addDays('2018-03-15T00:00:00Z', 10, 'yyyy-MM-dd')`, a year end (`'2026-12-31T23:00:00Z', 1`), a leap day (`'2028-02-28T00:00:00Z', 1`), `'D'`, `'t'`, an invalid format (error), a non-string format (error); `unsupported`: no zone, an offset, an empty format; the test at `ExpressionTests.cs:784` that expects `unsupported` for any format is replaced |
| 4 | medium | `int(value)`: a string of an optional `-` and digits with no leading zero that fits 32 bits gives that number (Learn's `int('10')`; 32 bits because Learn names only "the limits for the integer data type"); a decimal string or a string with no number in it is an error (the cookbook: "`int()` on a decimal string throws an error; it doesn't truncate"); any other string (spaces, `+`, leading zeros, an exponent, past 32 bits) is `unsupported`; an integer number within 32 bits gives itself (decisions Q1); any other value is `unsupported` | `ExpressionFunctions.cs` `Table`, Conversion | `ExpressionTests`: `int('10')`, `'42'`, `'-7'`, `'0'`, `'2147483647'`, `int(14)`; errors `'3.14'`, `'-3.14'`, `'abc'`, `''`, `'-'`; `unsupported` `' 1'`, `'+5'`, `'007'`, `'1e3'`, `'2147483648'`, `int(2147483648)`, `int(1.5)`, `int(true)`, `int(null)`, an array, an object |
| 5 | low | The pinned list: `Names_are_exactly_the_37_functions_sorted` becomes 39 with `formatDateTime` and `int`; the summary at `ExpressionFunctions.cs:8` says 39 | `tests/PowerOps.Core.Tests/Flows/ExpressionTests.cs:789-801`, `ExpressionFunctions.cs:8` | the test itself |
| 6 | high | The run's expression through the CLI (Done when 2): copies of `tests/fixtures/powerops-seed/v2` whose `Update_a_row` sets `item/pops_name` to `@formatDateTime(addDays(utcNow(), int(parameters('Default Status (pops_DefaultStatus)'))), 'yyyy-MM-dd')`, and to `@addDays(utcNow(), int(parameters('Default Status (pops_DefaultStatus)')), 'yyyy-MM-dd')`; a test file with `utcNow` `2026-11-04T09:00:00Z` and `pops_DefaultStatus` `"14"` asserts `runStatus` `Succeeded` and the input `["parameters", "item/pops_name"]` of `Update_a_row` equal to `2026-11-18`, exit 0. The seed's parameter is a String, so `int` gets a string there; the Core test uses an `Int` parameter, with `addDays(utcNow(), parameters(...), 'yyyy-MM-dd')` exactly as the cloud ran it | `tests/PowerOps.Cli.Tests/TestCommandTests.cs`, `tests/PowerOps.Core.Tests/Flows/FlowTestTests.cs` | the two tests |
| 7 | medium | `docs/FLOW-TESTS.md`: "Only 37 functions" (`:30`) points to a new section, "Supported functions", that lists the 39 by group with every argument form (for example `addDays(timestamp, days, format?)`, `utcNow()`, `guid()`, `substring(text, start, length?)`), and a paragraph with the rules of rows 1 to 4; a pointer to this plan's "Not on Learn" next to M14's | `docs/FLOW-TESTS.md:27-32` | a new `ExpressionTests` case next to the pinned list: the names that start a code span in that section are exactly `ExpressionFunctions.Names` |
| 8 | medium | Part 2, the capture (see "Environments and route"): its unpacked export and the scrubbed run record in `tests/fixtures/flowtests/capture/`, with `NOTICE.md`; flow tests in that folder, with the run's outputs of the List rows action as its mock, assert each action's status, each Compose's outputs and each failed action's error code as the run gave them; the cases the simulator can't run sit in `capture/unsupported/` and a test asserts each is `unsupported`; the `analyze-fixtures.*` goldens change by the recipe | `tests/fixtures/flowtests/capture/`, `tests/fixtures/NOTICE.md`, `tests/PowerOps.Cli.Tests/TestCommandTests.cs`, `tests/golden/analyze-fixtures.*` | the capture test, `ReportTests`, `OutputSchemaTests` |
| 9 | medium | Part 2 docs: `docs/FLOW-TESTS.md:27` says what the capture compared and on which date, and that the rest of the simulator is still from Learn alone; each "Not on Learn" item here and in M14's list is marked confirmed, corrected or still open | `docs/FLOW-TESTS.md:27`, this plan, `docs/plans/m14-flowtest/plan.md:116` | the capture test |

### Not on Learn

Part 1's choices, each one a case in the part 2 capture:

- `int` of an integer number gives that number (decisions Q1). Everything else Learn leaves open about `int` is `unsupported` (row 4), including whether "the integer data type" is 32 or 64 bits.
- `addDays`'s format uses the `en-us` culture, as `utcNow`'s format examples on Learn show (`'d'` gives `9/25/2025`, `'D'` gives `Thursday, September 25, 2025`).
- U+202F before AM/PM is written as a space.
- What part 1 makes `unsupported` and the capture records: a timestamp with an offset; `U`, `u`, `r`, `R`, `z` and `K` formats on a timestamp with no zone; an invalid `formatDateTime` format; an empty format or locale; a known locale other than `en-us`, `fr-fr`, `es-es`; `int` of a padded, signed, zero-led, exponent or 33-bit string, and of a decimal number, a boolean or null.
- Culture data: .NET reads it from ICU on Linux and on Windows 10 1903 and later. Learn's `en-us`, `fr-fr` and `es-es` examples match on .NET 10 with this machine's ICU, once U+202F is replaced; CI runs the same tests on Windows.

## Automation

None new. `powerops test` and the Action's merge gate run the new functions with no change to their interface.

## Screens

None.

## Environments and route

Part 1 reads and writes no tenant; the PR's CI is the route.

Part 2 runs in Dev, before 2026-12-15, when unmanaged developer environments are suspended. Before it creates, changes or runs anything there, the session asks the person in chat, naming exactly what it will create, and asks them to confirm the licence, DLP and role points below. The person does every sign-in (never a device code). The session deletes nothing in the tenant.

Before the yes, read-only checks: `pac org who` names Dev; `pac solution list` shows the seed solution, so the `pops` publisher and the `pops_workitems` table exist; the Power Automate tools list an existing Dataverse connection of the person's (the session creates no connection; with none, it asks). Licence, DLP and role: on 2026-11-04 a Dataverse-triggered flow with Dataverse actions ran in Dev under the person's account (the run's record), so the Dataverse connector is licensed for the person there and allowed by Dev's DLP policy, and the person, who owns Dev, can read `pops_workitems`; the yes prompt asks the person to confirm nothing changed.

What it creates, once the person says yes:

- An unmanaged solution `PowerOpsCapture` with the seed's publisher (prefix `pops`), holding one cloud flow, "PowerOps capture", manually triggered; one Number environment variable `pops_CaptureDays` with default 14; one Dataverse connection reference. The session writes the solution as an unpacked folder outside the repo, packs it with `pac solution pack`, and imports it with `pac solution import` and a deployment settings file that binds the connection reference to the existing connection, with flows activated. Nothing else in Dev changes.
- The flow, as M14 item 34 designed it, every action after the previous on `Succeeded`, `Failed`, `Skipped` and `TimedOut`, and the actions inside each scope likewise, so all of them run, and writing nothing: a Dataverse List rows (`$top` 3 on the seed's `pops_workitems` table, `$select` of the name column only), a condition on the row count, an Apply to each over the rows with a variable, a Try scope with a Compose that fails and a Catch scope that runs after it on `Failed`, an Until with `limit.count` 2 whose condition never holds, a Switch on a number, a Parse JSON of a JSON string, a Table in CSV and one in HTML, one Compose per item under "Not on Learn" in the M14 plan and in this plan, and these M58 cases: the run's expression with `parameters('pops_CaptureDays')`, with and without `int`; the examples of rows 1 and 3; `formatDateTime` of a timestamp with an offset, with no zone and `'o'`, and with no zone and `'zzz'`, `'u'` and `'K'`; `int` of a decimal string, of a word, of `' 1'`, `'+5'`, `'007'` and `'2147483648'`; `addDays` of a timestamp with no zone with and without a format, with fractional days, and across a year end with a format. Every case meant to fail takes its input from a Compose output, so the expression is evaluated at run time: Learn's cookbook says a constant invalid `int` fails when the flow is saved (`InvalidTemplate`), and the same holds for `json('not json')`, an invalid format and an unknown locale.
- One run, from the designer's Test pane by the person, or by the session through the Power Automate tools after the person's yes. The capture is complete when every action of the definition has a status in the run history, whatever the run's own status (it is expected to be `Failed`).
- The run record: the trigger outputs and each action's status, inputs, outputs and error code, read through the Power Automate tools or the run page, scrubbed before anything is written into the repo: no URL or host (the `@odata.context` and `@odata.nextLink` values carry the organisation's host), environment, tenant, flow, run, connection or tracking id, user name, email, or real timestamp of activity; the List rows rows are replaced with three fixed rows of fake names and keys `00000000-0000-0000-0000-00000000000n`, owner and created-by ids and `@odata.etag` values replaced with fake ones, and response headers dropped. The workflow object's fields are replaced the same way.
- Exports with `pac solution export`, unmanaged, unpacked with `pac solution unpack`, scrubbed by the "Add a fixture" recipe; a second export with a static result on one action (set by an update of that action's definition, the person's yes covering it) shows whether an export keeps it, the M14 question. The seed and M12 solutions are exported from Dev too, unmanaged and managed, and kept outside Git, as M14 asked.
- `capture.md` in this folder: the date, what was created, the run's statuses, the comparison's result, the cloud values of the cases the simulator refuses, and every leftover in Dev (solution, flow, environment variable, connection reference, runs).

Before the second PR: the leak scan over the branch, and a read of the whole diff of `tests/fixtures/flowtests/capture/` for anything the scrub missed.

## Solution and publisher

None in part 1. Part 2: the unmanaged `PowerOpsCapture` in Dev, publisher prefix `pops`, so its export is a fixture of its own.

## Constraints

- AGENTS.md: smallest change, new logic with tests, Conventional Commits, no version or changelog edits, no tenant URLs, ids, emails or user names in commits or fixtures. No write is added, so `SafeWrite` is not touched.
- Open sources only: Learn's function reference (`azure/logic-apps/expression-functions-reference`), the Power Automate date-time page and expression cookbook, .NET's format-string pages that Learn links to. No decompiling, no Logic Apps test SDK.
- The pinned roles `powerops-verify` and `powerops-review-security` and `tests/eval` are untouched.
- Windows and Linux. Culture data comes from ICU on both; the tests use `en-us`, `fr-fr`, `fr-FR` and `es-es` only, with Learn's examples.
- Other P0 sessions run in parallel (M50 diff/analyze/drift, M54 skills, M56 releases); this milestone keeps to `Flows/`, its tests, `FLOW-TESTS.md`, the seed copy in a temp folder and the new fixture folder.

## Assumptions

- Learn, read 2026-11-04 with the Learn MCP tools. Function reference: `formatDateTime('<timestamp>', '<format>'?, '<locale>'?)`; format "either a single format specifier or a custom format pattern", default `o`, which "preserves time zone information"; locale default `en-us`, invalid is an error; examples `formatDateTime('03/15/2018')` gives `'2018-03-15T00:00:00.0000000'`, `'03/15/2018 12:00:00', 'yyyy-MM-ddTHH:mm:ss'` gives `'2018-03-15T12:00:00'`, `'01/31/2016', 'dddd MMMM d'` gives `'Sunday January 31'`, with `'fr-fr'` and `'fr-FR'` `'dimanche janvier 31'`, with `'es-es'` `'domingo enero 31'`. `addDays('<timestamp>', <days>, '<format>'?)`, days an Integer, format as above, "If the format isn't a valid value, an error is generated". `int('<value>')`, a String, "the integer version for the specified string", example `int('10')` gives `10`; the limits are "the limits for the integer data type", with no numbers. `utcNow` format examples: `'d'` gives `9/25/2025`, `'t'` gives `5:18 AM`.
- Power Automate "Customize or format date and time values": `'g'` gives `6/15/2009 1:45 PM`, `'D'` gives `Monday, June 15, 2009`, `'M/dd/yyyy h:mm tt'` gives `6/15/2009 1:45 PM`, `'M/dd/yyyy HH:mm:ss tt'` gives `6/15/2009 13:45:30 PM`. Its `'yyyy/MM/dd hh:mm:ss tt'` example shows `2009/06/15 1:45:30 PM`, but .NET's custom format reference, which Learn links to for the format, defines `hh` as two digits, so the test expects `01:45:30 PM` and the capture checks it. Expression cookbook: `int()` on a decimal string is an error, a constant one at save time; `formatDateTime('2026-03-18T14:30:00Z', 'MMMM dd, yyyy')` gives `March 18, 2026`; `addDays()` "also accepts fractional values" (the reference says Integer; decisions Q9). The cookbook says it was written with AI assistance.
- .NET 10 on this machine (ICU) gives every value above, except that `g`, `t`, `T` and `U` put U+202F before AM/PM, and that `U` and `z` formats on a timestamp with no zone take the machine's time zone. Checked 2026-11-04 with a scratch program.
- The run's record (outside the repo): the flow's environment variable had the default 14 in Dev (its type is not recorded); the rewritten flow, `addDays(utcNow(), parameters('...'), 'yyyy-MM-dd')`, wrote 2026-11-18 on a run of 2026-11-04; a Dataverse-triggered flow ran in Dev that day.

## Open questions

## Questions for your admin

## Steps

1. This plan and `decisions.md`, a fresh-context review into `review.md`, fixes, and sign-off under the delegation of 2026-10-22 once no High or Medium finding is open.
2. Part 1, test first: rows 1 to 5, then row 6, then row 7.
3. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .`, the leak scan; push, PR, CI until all eight checks pass. Not merged by this session.
4. Part 2, only after the person's yes in chat: the read-only checks, the capture in Dev, `capture.md`; then rows 8 and 9, the leak scan and a read of the fixture diff, and a second PR based on `main` once part 1 has merged.
