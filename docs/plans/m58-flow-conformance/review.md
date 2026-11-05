plan-hash: 16ba0c81bb3e640f89c50891938d2f3735de3602f952591c06bcc8885d0f2ccb
model: claude-sonnet-5-5
fresh-context: yes

Environment facts were not read: `pac` is not confirmed signed in, so `powerops plan facts` was not run and no tenant call was made. The plan was reviewed from the repo (code at `b8ad904`), Microsoft Learn (function reference sections formatDateTime, addDays, int, utcNow; the date-time page; the expression cookbook) and the plan files. Open questions: 0.

## Findings

### High

#### R1. Choices on Learn's silence can make a test pass where the cloud differs
- Area: 9 Testability (the rule: what cannot be simulated is `unsupported`, never a pass)
- Finding: Q3 converts a timestamp with an offset to UTC before formatting. Learn's `formatDateTime` and `addDays` text says the default format `o` "preserves time zone information", which reads against the conversion: `formatDateTime('2018-03-15T10:30:00+02:00', 'HH:mm')` would give `08:30` in the simulator, and the cloud may give `10:30`. A test written from the simulator's output passes locally and is wrong in the cloud. Q3's own option 2 (`unsupported` until the capture) was the rule-conforming choice; "addDays already does this" copies an unchecked M14 choice instead of fixing it. Same family: `addDays` reads a zoneless timestamp as UTC (`Z` appended) while `formatDateTime` keeps no zone, so `addDays('2018-03-15', 0, 'o')` and `formatDateTime('2018-03-15')` disagree in the simulator and Learn's example only supports the second. The plan also notes "Not on Learn" items 1, 3 and 4 (offset, `addDays` culture, `int(number)`) but still returns values for them.
- Suggested fix: make a timestamp with an offset `unsupported` in `formatDateTime` (and in `addDays` when a format is given) until part 2 settles it; make `addDays` with a zoneless timestamp and a format `unsupported` for the same reason, since it and `formatDateTime` would disagree. Keep a passing value only where Learn states it.
- Resolution: fixed: an offset timestamp is `unsupported` in `formatDateTime` and in `addDays` with a format; `addDays` with a format also refuses a timestamp with no zone (plan rows 1 and 3, Not on Learn; decisions Q3 amended)

### Medium

#### R2. `int` string rules are invented and presented as Learn's
- Area: 9 Testability
- Finding: Learn's `int` section says only: "The minimum and maximum values are the same as the limits for the integer data type" and gives `int('10')`. Row 4 states a 64-bit range, an error for `' 1'`, `'007'` as 7 and `'+5'` as 5, and an error for `'99999999999999999999'`. The 64-bit range is not on Learn and is missing from "Not on Learn". A test asserting an error for `int(' 1')` or an out-of-32-bit value passes locally while the cloud may succeed. Q1's `int(14)` is a guess about the Number environment variable (the plan says "most likely").
- Suggested fix: keep only Learn's `int('10')`, plain digit strings with an optional minus, and the cookbook's decimal-string error. Make whitespace, `+`, leading zeros and values outside the 32-bit range `unsupported`, and list the 64-bit/32-bit question under "Not on Learn".
- Resolution: fixed: `int` reads only an optional `-` and digits with no leading zero within 32 bits; spaces, `+`, leading zeros, exponents and 33-bit values are `unsupported`; the 32/64-bit question is under Not on Learn (row 4, decisions Q12). `int` of an integer number is kept, within 32 bits, and Done when 2 says it is the simulator's reading (decisions Q1 kept)

#### R3. "Every Learn example reproduces" is not true for one page example
- Area: 9 Testability
- Finding: Assumptions say .NET reproduces every example; Done when 1 says each is tested. The date-time page shows `formatDateTime('2009-06-15T13:45:30', 'yyyy/MM/dd hh:mm:ss tt')` as `2009/06/15 1:45:30 PM`, but `hh` is a two-digit 12-hour field in .NET, so the output is `2009/06/15 01:45:30 PM`. The page's `utcNow()` examples ("June 15, 2009, 16:50") are illustrations with no fixed output.
- Suggested fix: say in the plan that the page's `hh` example is an illustration that .NET's own format-string page contradicts, test `01:45:30 PM` and name .NET as the source; limit "every example" to the reference sections.
- Resolution: fixed: Done when 1 and Assumptions say the page's `hh` example contradicts .NET's format reference, which Learn links to; the test expects `01:45:30 PM`, citing .NET; the capture checks it

#### R4. An error for an invalid `formatDateTime` format is not on Learn
- Area: 9 Testability
- Finding: Row 1 tests an invalid format (`'X'`) as an error, citing row 2. Only `addDays` and `utcNow` carry "If the format isn't a valid value, an error is generated"; `formatDateTime`'s format row has no such sentence, only the locale row does. Most strings are valid custom patterns in .NET, so which formats the cloud rejects is not known.
- Suggested fix: make an invalid format in `formatDateTime` `unsupported` (or list it under "Not on Learn" and have the capture include it); keep the error for `addDays`.
- Resolution: fixed: an invalid `formatDateTime` format is `unsupported`; `addDays` keeps the error Learn states (rows 1 and 2, decisions Q13)

#### R5. Locale and culture data can differ between machines
- Area: 9 Testability
- Finding: Any predefined locale is accepted, so output depends on ICU data (and on whether ICU is present at all): a non-Gregorian default calendar (`th-th`, `ar-sa`), empty AM/PM designators (`fr-fr` with `tt`), date and time separators per culture. The plan itself says an ICU that differs "can give other text" and only verifies `en-us`, `fr-fr`, `es-es`. Windows falls back to NLS without ICU, and a Linux runner without libicu or with invariant globalization makes `GetCultureInfo(name, predefinedOnly: true)` throw, which the plan would report as "unknown locale" (an `ExpressionException`, a fail) instead of an environment problem. Nothing in the repo sets or tests for either mode.
- Suggested fix: allow only the locales Learn demonstrates (`en-us`, `fr-fr`, `es-es`, case-insensitive) and make every other locale `unsupported`; detect invariant-globalization mode and throw `UnsupportedException` with that reason; add a test that fails loudly if the machine's ICU does not produce Learn's `fr-fr` and `es-es` text.
- Resolution: fixed: only `en-us`, `fr-fr` and `es-es` are accepted; another name .NET knows is `unsupported`, an unknown name an error; invariant globalization gives `unsupported` instead of a failing type initializer (rows 1 and 2, decisions Q14). Not adopted: a test that fails when ICU differs; the Learn-example tests already fail then, on both CI platforms

#### R6. Done when 2 claims more than the evidence shows
- Area: 9 Testability
- Finding: Done when 2 says the run's expression "giving `2026-11-18`, the value the cloud gave". The cloud only ran the rewrite without `int` (Assumptions, last bullet); the original expression with `int(...)` never ran in the cloud, and the variable's type is "not recorded". With a fixed `utcNow` the test proves the simulator's own arithmetic and formatting, which Learn's examples already cover.
- Suggested fix: reword to "the rewrite equals the cloud's value; the form with `int` is the simulator's reading (Q1) until part 2"; keep the two CLI tests and the Int-parameter Core test, and say what each proves. State the assertion kind (inputs of `Update_a_row` at `item/pops_name`).
- Resolution: fixed: Done when 2 says the rewrite equals the cloud's value and the form with `int` is the simulator's reading until part 2, and names the assertion (row 6, decisions Q15)

#### R7. Part 2 scrub list misses the data the capture reads
- Area: 9 Testability / safety
- Finding: List rows returns real Dev rows. The scrub list names URLs, environment and tenant ids, user names, emails, connection, tracking and run ids, but not row values, primary keys, owner and created-by ids, `@odata.etag` and `@odata.context` (carries the organisation host), timestamps of real activity, `workflow` object fields, or response headers. The leak scan (`reports/leakscan.sh`) is in Step 3 for part 1 only, not for the capture PR.
- Suggested fix: list those fields in the scrub rules, replace row values with fixed fake text and keys with `00000000-0000-0000-0000-00000000000n`, drop headers, and add the leak scan and a manual diff read to Step 4 before the second PR.
- Resolution: fixed: the scrub rules name row values, keys, owner and created-by ids, `@odata.etag`, `@odata.context` and `@odata.nextLink`, activity timestamps, workflow fields and headers; Step 4 adds the leak scan and a read of the fixture diff

#### R8. Part 2 is not complete enough to execute
- Area: 5 Route, 9 Testability
- Finding: (a) Header says `environment: null` while part 2 targets Dev, and no way is given to select or confirm Dev (the name, `pac org who`, sign-in state); `pac` is not confirmed signed in. (b) The plan does not say how the flow and solution are created (portal, FlowAgent tools, import of written JSON) or how the run is started; each has different tenant effects. (c) The seed's publisher and the `pops_workitems` table must already exist in Dev; this is assumed, not checked. (d) The cookbook and error reference say constant invalid expressions (`int('abc')`) fail at save time as InvalidTemplate. Only the two `int` cases are said to come from a Compose; `json('not json')`, an invalid format (`'X'`), an unknown locale and the other error cases have no such rule, so the flow may not save. (e) Success of the run is not defined if a failing case stops the next action (`runAfter` on `Succeeded` and `Failed` is stated, but not for scopes).
- Suggested fix: name the environment in the header or in the Step 4 prompt, say how the flow is built and run, add a pre-check list (publisher, table, an existing Dataverse connection), and require every error case to take its input from a variable or Compose output.
- Resolution: fixed: header `environment: PowerOps Test`; read-only checks before the yes (`pac org who`, the seed solution, an existing Dataverse connection); build as files, `pac solution pack` and `pac solution import` with a deployment settings file; the run's trigger; every failing case reads a Compose output; completeness is a status for every action; `runAfter` on every status, inside scopes too

#### R9. "Compares errors" has no comparison rule
- Area: 9 Testability
- Finding: Done when 5 compares "evaluated inputs, outputs, errors and statuses". The cloud's error text and codes (`ExpressionEvaluationFailed`, a message with the action and expression) differ from the simulator's messages, so an exact comparison cannot pass and a loose one is undefined.
- Suggested fix: say that errors are compared by action status and error code only, and that message text is only checked for containing the function name.
- Resolution: fixed: errors are compared by action status and error code, not message text (Done when 5, row 8)

### Low

#### R10. Zone-labelled formats on a zoneless timestamp
- Area: 9 Testability
- Finding: Only `U` and `z` are refused for a timestamp with no zone. `u` (appends `Z`), `K`, `r`/`R` (append `GMT`) also label the value as UTC or print nothing, and the service's behaviour is not on Learn. Q6 chose `contains('z')` as a refusal that "can only refuse more"; the same argument covers these.
- Suggested fix: add `u`, `K`, `r` and `R` to the unsupported set for a timestamp with no zone, or list them for the capture.
- Resolution: fixed: `u`, `r`, `R` and formats with `K` are refused on a timestamp with no zone too (row 2, decisions Q6 amended)

#### R11. Licence, DLP and roles for part 2 are not stated
- Area: 3 Licence, 4 DLP, 8 Security roles
- Finding: Part 2 uses the Dataverse connector in a flow, a connection reference and a read of `pops_workitems`. The plan does not say which licence covers the Dataverse connector in Dev, whether Dev's DLP policy allows it with the other connectors the flow uses, or which role the person's connection needs to read the table (a missing read privilege is a known cause of empty results).
- Suggested fix: add one line each in "Environments and route", or mark them as checked by the person at the yes prompt.
- Resolution: fixed: Environments and route states the licence, DLP and role evidence from the run of 2026-11-04 and has the person confirm it at the yes
