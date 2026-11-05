plan-hash: d81aa63ae65f68771dc7188eacac52c4590cbf7b02f04bcc7f4c92c8450dc2e8
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

## Second pass

Run against HEAD `fc427a1` (plan and decisions Round 2; the code is `d6a4d6a`, `86b79e6` and `b7e28e2`) by the same reviewer with a fresh read. The first-pass text above is unchanged; the `plan-hash` line now names the revised plan. Still no `powerops plan facts`, `pac`, `az`, tenant call or GitHub write.

What I ran: `dotnet test` of the Core `Flows` tests (140 pass) and the CLI `TestCommandTests` (24 pass) in the worktree, and a probe of about 40 expressions in a scratch copy outside the repo (`git archive HEAD`).

What held (each resolution checked in the plan, `decisions.md`, the code, the tests and `docs/FLOW-TESTS.md`):
- R1: `formatDateTime` refuses any offset (`Kind.Local`, `+00:00` too); `addDays` with a format needs a `Z` timestamp; a test covers `+02:00`. The probe confirms.
- R2: `int` reads `-` and digits with no leading zero within 32 bits; `' 1'`, `'+5'`, `'007'`, `'1e3'`, `'2147483648'`, `Infinity` and `NaN` are `unsupported`; a decimal string and a word are errors. The Q1 guess is limited to 32 bits and named in Done when 2. Holds, with R14 below.
- R3: the `hh` example is handled as the plan says, and the test expects `01:45:30 PM`.
- R4: an invalid `formatDateTime` format (`X`, `/`) is `unsupported`; `addDays` keeps the error.
- R5: only `en-us`, `fr-fr`, `es-es` are accepted (any case); `fr`, `en_us` and `de-DE` are `unsupported`; invariant mode gives `unsupported`. The decision not to add an ICU-mismatch test is sound: the Learn-example tests fail anyway. Holds for the locale choice; see R13 for the format side.
- R6: Done when 2 now says what is the cloud's value and what is the simulator's reading; the Core test runs the cloud's exact rewrite with an `Int` parameter; the CLI tests assert the `item/pops_name` input.
- R7, R8, R9, R11: the scrub list, the read-only pre-checks, the build route (`pac solution pack` and `import` with a settings file), Compose-sourced failing cases, "complete = a status for every action", error-code comparison, the licence and DLP evidence and the leak scan in Step 4 are all in the plan.
- R10: `u`, `r`, `R` and `K` are refused on a zoneless timestamp (code and docs agree).

### High

No findings.

### Medium

#### R12. A partial timestamp is read against the machine's clock
- Area: 9 Testability (machine-dependent result; a value Learn does not state)
- Finding: `ReadZoned` uses `DateTime.TryParse`, which accepts strings that are not full dates and fills the rest from today. Probe: `formatDateTime('10:30')` gives `2026-11-04T10:30:00.0000000` (today's date), `formatDateTime('March 15')` gives `2026-03-15T00:00:00.0000000` (this year), `formatDateTime('March 2018', 'o')` gives `2018-03-01T00:00:00.0000000`, and `formatDateTime('15 Mar 2018 10:00:00 GMT')` gives a `Z` result. A test written today passes with those values and fails on another day, and Learn states none of them (its example is `'03/15/2018'`). The same parser serves `addDays` with a format. The docs say "a timestamp ending in `Z` or with no time zone", which hides this.
- Suggested fix: read with `DateTime.TryParseExact` against a short list (the ISO forms `yyyy-MM-ddTHH:mm:ss[.fffffff]` and `yyyy-MM-dd`, with optional `Z`, and Learn's `MM/dd/yyyy[ HH:mm:ss]`); treat any other text that `TryParse` would accept as `unsupported`, and unreadable text as an error. Add tests for `'10:30'` and `'March 15'`.
- Resolution: fixed: both readers accept only full ISO 8601 timestamps and `MM/dd/yyyy` with an optional time; other text .NET reads as a date is `unsupported` (plan rows 1 and 3, decisions Q17); tests for `'10:30'`, `'March 15'`, `'3/15/2018'` and `'2018-03-15 10:30:00'`

#### R13. `fr-fr` and `es-es` with any format still depend on the machine's ICU data
- Area: 9 Testability (platform-dependent result)
- Finding: R5 limited the locale names, but any format is accepted with them. Probe on this machine: `formatDateTime('2018-03-15T13:00:00', 'g', 'es-es')` gives `15/3/2018 13:00`, with `'g'` and `'fr-fr'` `15/03/2018 13:00`, `'tt'` with `fr-fr` gives `PM`. Learn shows these locales only with `'dddd MMMM d'`. Day-period text, separators and patterns of these cultures changed between CLDR versions (and Windows and Linux CI use different ICU builds), so a test using them can pass here and fail on the other platform, or differ from the service. The docs say only "follow .NET's culture data".
- Suggested fix: for `fr-fr` and `es-es` accept only formats made of the day and month specifiers Learn demonstrates (`d`, `dd`, `ddd`, `dddd`, `M`, `MM`, `MMM`, `MMMM`, `yyyy`, `yy` and literal separators); make every other format `unsupported`. State this in the docs and add a test.
- Resolution: fixed: with `fr-fr` and `es-es` only custom day, month and year formats are accepted; `'g'`, `'D'`, `'tt'`, `'HH:mm'`, abbreviations and `/` are `unsupported` (row 1, decisions Q14 amended)

### Low

#### R14. Some `int` errors are not the ones Learn states, and the docs overstate them
- Area: 9 Testability
- Finding: The plan, the docs and decisions Q12 say a decimal string or "a string with no number in it" is an error. The code also errors for strings that contain digits: probe `int('12abc')`, `int('1,000')` and full-width digits `int('３')` all throw `ExpressionException`. The cookbook supports only the decimal-string error. The cloud may read `'1,000'` differently.
- Suggested fix: error only for decimal strings and for strings with no digit; make the rest `unsupported`. Change the docs sentence to match whichever code is kept.
- Resolution: fixed: errors only for a decimal string or a string with no digit; `'12abc'`, `'1,000'` and non-ASCII digits are `unsupported`; docs match (row 4, decisions Q12 amended)

#### R15. Two small mismatches
- Area: 9 Testability, 5 Route
- Finding: (a) The header `environment: PowerOps Test` and the Goal text "the developer environment, not the trial Test environment" use the word Test for both; a session that reads only the header can target the trial environment. (b) The CLI test `The_live_runs_date_expression_passes_with_the_value_the_cloud_gave` runs both the original with `int` and a rewrite that has `int` too, which the cloud never ran; the name claims the cloud's value for both, while the plan says only the Core test's rewrite is the cloud's.
- Suggested fix: (a) have the pre-check name the environment type and URL host class from `pac org who` and stop if it is a trial environment. (b) Rename the test to say "the date expression gives 2026-11-18" and keep the cloud claim on the Core test.
- Resolution: fixed: (a) the pre-check reads the type from `pac admin list` and stops unless it is Developer; (b) the CLI test is renamed and says the cloud ran only the rewrite without `int` (decisions Q18)
