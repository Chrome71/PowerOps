plan-hash: 2f9b98a6ef994528219eac78492e69c84388585d1dab57c951c97b637e6eacf8
model: claude-sonnet-5-5
fresh-context: yes

Environment facts were not read: `pac` is not confirmed signed in, so `powerops plan facts` was not run and no tenant call was made. The plan was reviewed from the repo (`ExpressionFunctions.cs`, `ExpressionTests`, `docs/FLOW-TESTS.md`, the M58 plan, decisions and capture record at `a511881`), Learn's expression functions reference (sections float, addHours, addMinutes, addSeconds, startOfDay, dayOfWeek, convertFromUtc, convertTimeZone, read from its published source) and .NET's `TimeZoneInfo` API remarks. Open questions: 0. Checklist items 1 to 8 (solutions, connection references, licences, DLP, route, managed or unmanaged, delegation, security roles) are not applicable: the plan changes only the flow simulator, its tests and one doc. The plan's quotes of Learn were checked and are accurate (parameter texts, the format-error sentence in all four date sections, the convertFromUtc and convertTimeZone return texts, the punctuation note, all examples and the six `float` "unexpectedly valid inputs"). No URL, GUID, email or person's name was found in the plan or `decisions.md`.

## Findings

### High

#### R1. Past instants: "from 2007 on both hold the current rules" is not shown, and the cloud follows Windows data
- Area: 9 Testability (the rule: what Learn doesn't settle is `unsupported`, never a value that lets a test pass where the cloud differs)
- Finding: Decisions Q5 and row 5 take any instant from 2007 to 9998 and say both platforms "hold the current rules" from 2007. Nothing supports that. The .NET page the plan cites says the Windows registry "does not necessarily contain historic time zone data" and most DST zones have only one or two predefined rules, while the IANA data keeps every past rule. Several zones changed rules after 2007 (Russia 2011 and 2014, Brazil 2019, Mexico 2022, Egypt 2023; three of them are in row 7's list). Row 7 checks only 2026, so a flow test with `convertFromUtc('2012-07-01T12:00:00Z', 'Russian Standard Time')` or a 2015 Brazil date passes or fails by the computer's data, not by anything Learn or the capture states. The service itself is documented only through Windows names, so on Linux the simulator can give the IANA answer where the cloud gives another. Instants far in the future (rules not yet known, up to 9998) are the same risk: both platforms extrapolate their latest rule.
- Suggested fix: make the supported window explicit and pinned, not 2007 to 9998. Either raise the floor to after the latest change among the zones the plan accepts (and say which change fixed it), or add past-instant pinned cases (a date in each rule era for the zones that changed) that must pass on both CI platforms, and refuse an instant outside the years the tests pin. Cap the upper end at a year or two past the current one, and say in `docs/FLOW-TESTS.md` that results outside it are `unsupported`. Reword Q5's sentence so it states only what is checked.
- Resolution: fixed: only 24 zones are taken, each from the first full year after its last rule change through 2030; a test pins their January and July offsets in every year of each window and the 2026 changes of four zones, on both CI platforms; what can still differ is named (plan rows 4, 5, 7, Not on Learn; decisions Q5 amended)

### Medium

#### R2. Names with punctuation are taken in `convertTimeZone`, against its own Learn text
- Area: 9 Testability
- Finding: Learn's convertTimeZone section says "you might have to remove any punctuation from the time zone name". Row 4 and Q4 decide not to follow it, because the Default Time Zones page writes `W. Europe Standard Time` and a DISM example (a different tool) uses it. That is a reading of Learn's silence in the other direction: for convertTimeZone Learn points the opposite way for names with a dot or brackets (`W. Europe Standard Time`, `Central Standard Time (Mexico)`), and row 4's tests take them in both functions. A test that passes with the dotted name in the simulator may fail in the cloud (or the cloud may need the stripped form, which row 4 calls a name nobody knows).
- Suggested fix: for `convertTimeZone` make a name with punctuation `unsupported` (the dotted name stays accepted in `convertFromUtc`, whose section only points at the Default Time Zones page), or make both functions refuse it until a capture settles it. Add the case to the "Not on Learn" list under what a capture should check.
- Resolution: fixed: `convertTimeZone` refuses a name with punctuation; `convertFromUtc`, whose section has no such sentence, takes it; both listed under Not on Learn (plan row 5; decisions Q4 amended)

#### R3. "Taken on both platforms or neither" is not guaranteed, and two tests depend on ICU's data version
- Area: 9 Testability
- Finding: Row 4 says both mappings "read the same ICU data on Windows and Linux", so a name is taken on both or neither. The .NET remarks say `TryConvertWindowsIdToIanaId` and `TryConvertIanaIdToWindowsId` depend on whichever ICU library the app uses: Linux's system library and Windows' own `icu.dll` are different versions with different CLDR data. The third condition, `FindSystemTimeZoneById`, reads the registry on Windows and ICU/tzdata on Linux, so a name the registry lacks (older Windows image) or Linux tzdata lacks fails on one side only. Concretely, the test that `Türkiye Standard Time` is `unsupported` flips as soon as one platform's CLDR learns that name (the plan notes ICU already knows `Turkey Standard Time`), and the round-trip rule can map a different name back on another ICU version. Result: a CI job fails on a runner image update, or a supported-name set differs between Windows and Linux with no test showing it.
- Suggested fix: reword to what is guaranteed (the check uses each platform's ICU and zone store); drop the `Türkiye Standard Time` and other CLDR-version-sensitive names from the "must be unsupported" tests (assert only that a name that no data knows, `Bogus`, is `unsupported`); keep the positive list to names present on every Windows version CI uses and in Learn's Default Time Zones table, and add a test that every name in row 7's 16-zone list is accepted, so a platform that cannot take one fails loudly.
- Resolution: fixed: the ICU round trip is gone; names are the 24 of a table, compared exactly; the `Türkiye Standard Time` test is dropped; every listed name is accepted in row 7's tests on both platforms (plan rows 4, 7; decisions Q4 amended)

#### R4. `string(float('14'))` gives `14.0` in a test, a text Learn doesn't state
- Area: 9 Testability
- Finding: Row 6 asserts `string(float('14'))` gives `14.0` and the "Not on Learn" list admits the service's text is not on Learn. A test of an unsettled value is the false pass the rule forbids: a flow comparing `string(float(...))` or `concat('Total ', float('14'))` with a text passes in the simulator and may give `14` in the cloud. The `.0` trick of Q12 also protects only a direct use: any path that copies the node and re-reads it (`json(string(...))` and similar) turns it back into an integer. Row 6 also asserts an error for `'abc'`, `''` and `'1,000.5.5'`; Learn states an error only for an invalid locale, not for an invalid value (an error is the safe outcome for a flow that cannot work either way, but it is still a choice).
- Suggested fix: make `string`, `concat` and any text conversion of a float with no fraction `unsupported` (a float with a fraction, such as `10000.333`, is Learn's own example), or drop the `14.0` test and the claim. Add `'abc'` and `''` to "Not on Learn" as the simulator's choice.
- Resolution: fixed: no test asserts the text of a whole float, and the plan says its text is the simulator's; `float('abc')` and `float('')` as errors are listed as a choice (plan row 6, Not on Learn; decisions Q12 amended). Not adopted: refusing every text conversion of a whole float, because `ToText` can't tell `float`'s `14.0` from any other JSON `14.0`; a copy of the value keeps `14.0`, so `json(string(...))` doesn't make it an integer

#### R5. Nothing shows the conversions are independent of the computer's own time zone
- Area: 9 Testability
- Finding: CI runners are on UTC, so every test runs with `TimeZoneInfo.Local` equal to UTC. `ConvertTimeFromUtc` and `ConvertTime` set the result's `Kind` from the destination (`Utc`, `Local` or `Unspecified`), and a `Local` result written with `o` carries an offset; a timestamp with an offset is read with `Kind` `Local`, which `.ToUniversalTime()` converts by the machine's zone. A user running `powerops test` on a machine set to Pacific time can get a different string (an offset in the result, or a destination equal to the machine's zone taking another path) than CI saw. This is the same class of risk as the unsupported `Z` and `U` formats, and the plan adds a lot of new `DateTime` handling.
- Suggested fix: say in rows 1 to 5 that every result is formatted from a `DateTime` set to `Unspecified` (or a `Utc` one for `Z`), never from `Local`, and add a test run that sets the process time zone (`TZ=America/Los_Angeles` on Linux; on Windows set it through the test's own call, or assert on a `Local`-free path) for a destination equal to the machine's own zone and for `convertFromUtc` to `UTC`.
- Resolution: fixed: every conversion result is made `Unspecified` before it is written, and a Linux test with `TZ=America/Los_Angeles` in a non-parallel collection asserts the results don't change (plan rows 5, 7; decisions Q15)

#### R6. Row 7's expected values come from the code under test, and the plan lists none
- Area: 9 Testability
- Finding: Row 7 says the tests assert "each result" but lists no value; the expected values are "the zones' published 2026 rules as this Linux machine's IANA data gives them". Values taken from the same data the simulator reads are not an independent check, so a wrong rule in that data is copied into the expectation, and a reader of the plan cannot tell whether a row is testable as written (32 conversions and 12 boundary cases, none shown).
- Suggested fix: put the expected offsets in the plan's Data section (zone, instant, offset), taken from the zones' published rules (for example, Egypt's DST span of 2026, the no-DST rule of Mexico and Brazil), and say the expectation was written by hand from those rules, not generated by running the code.
- Resolution: fixed: row 7 lists every expected offset and change time, written by hand from each zone's published rule, and says this machine's data gives each and the Windows job checks Windows' (decisions Q5 amended)

### Low

#### R7. Zoneless timestamps in `startOfDay` and the conversions pass a value Learn doesn't state
- Area: 9 Testability
- Finding: The plan lists these under "Not on Learn", so they are known choices: `startOfDay` of a timestamp with no zone returns one with no zone (the capture shows this for `addDays` only), and a zoneless timestamp in `convertFromUtc` is UTC and in `convertTimeZone` a time on the source clock. Learn's descriptions ("from UTC", "from the source time zone") support the last two; nothing but analogy supports the first. It is also the main path of a real flow (`startOfDay(convertFromUtc(utcNow(), ...))`), so a wrong choice is not rare.
- Suggested fix: keep them, but name them in `docs/FLOW-TESTS.md` as choices a Dev capture should check first, and if the capture of Q13 is ever taken, put these three in it before anything else.
- Resolution: fixed: the four choices on a real flow's main path come first under Not on Learn and in `docs/FLOW-TESTS.md` (decisions Q16)

#### R8. Done when 1 says each function is tested with its section's examples, but one example is tested differently
- Area: 9 Testability
- Finding: Learn's second convertTimeZone example has `T80:00:00` and gives a result; the plan tests an error for it and runs the result with `T08`. That is a reasonable reading of a typo, but the test asserts an outcome Learn contradicts, and Done when 1 and the Assumptions say "each tested with the examples of its section".
- Suggested fix: reword Done when 1 to "all examples but one, whose `T80` hour is a typo (decisions Q12)"; make `T80` `unsupported` instead of an error unless a capture shows the cloud's error.
- Resolution: fixed in part: Done when 1 names the `T80` example as the exception; it stays an error, as every unreadable timestamp is (decisions Q12 amended)

#### R9. `float` limits and Learn's "custom parameters only" note are not carried into the plan
- Area: 9 Testability
- Finding: Learn's float section says the function can be used "only when passing custom parameters to an app" and that its limits are those of the float data type (no numbers given). The plan maps this to .NET's `double` (a stated choice) but neither mentions the first sentence nor says what happens for a value a single-precision float cannot hold (`'3.4e39'`, `'16777217'` as a fractional string). A test asserting such a value passes in the simulator and may round in the cloud.
- Suggested fix: add the "custom parameters" sentence to `docs/FLOW-TESTS.md` and to "Not on Learn", and make a string with more than 7 significant digits or past the single-precision range `unsupported`, or state that the double reading is a capture case.
- Resolution: fixed: the double reading and the "custom parameters" sentence are under Not on Learn and in the docs, with the double as a capture case (decisions Q10 amended). Not adopted: refusing strings past seven significant digits, which would refuse Learn's own `'.12345678'` and `'12,3,4,5678'`

## Second pass

Run against the revised `plan.md` and `decisions.md` Round 2 (status `approved`, 0 open questions) by the same reviewer with a fresh read; the first-pass text above is unchanged and the `plan-hash` line now names the revised plan. Only the plan was reviewed: an implementer was changing the code at the same time, so the code and tests were not read. Still no `powerops plan facts`, `pac`, `az`, tenant call or GitHub write. I re-checked every value in rows 4 and 7 against each zone's published rules: the 2026 change dates and times (second Sunday of March and first Sunday of November in the United States, last Sundays of March and October in Europe at 01:00 UTC, first Sunday of April and October in south-east Australia, last Friday of April 00:00 and last Thursday of October 24:00 in Egypt), all 24 January and July offsets, and the 24 first years against each zone's last rule change. All hold. The conversions of each change into UTC minutes are right.

What held:
- R1: holds. The window is now the 24 zones, each from the first full year after its last rule change through 2030, and every year's two offsets are pinned in the plan for both CI platforms. The first years are right: 2008 for the United States (2007 change) and New Zealand (rule change of September 2007, so January 2008 is already on the new rule), 2009 for AUS Eastern (conservative: its January and July offsets do not differ in 2008), 2015 for Russia (October 2014), 2020 for Brazil (DST was still in force in January 2019), 2023 for Mexico (July 2022 was DST), 2024 for Egypt (the 2023 return began in April, so January 2023 would also pass; 2024 is the conservative first full year). The zones with no change since before 2008 are correctly given 2008. The sentence about what can still differ is honest.
- R2: holds. `convertTimeZone` refuses a name with punctuation; `convertFromUtc` takes it; both are in "Not on Learn". See R13 for an incomplete example list.
- R3: holds. The ICU round trip and the `Türkiye Standard Time` test are gone, names are compared exactly against the table, and every name must be accepted in row 7 on both platforms.
- R4: holds in part. No test asserts a whole float's text, and Q12 says so. The residual is a flow that asserts `concat('Total ', float('14'))` from the simulator's output: it can pass here and differ in the cloud. The plan's reason for not refusing it (the text conversion cannot tell this `14.0` from another) is accepted; see R14 for the doc line that closes the gap.
- R5: holds, with R11 and R10 for how the test is specified. The `Unspecified` rule and the Linux `TZ` test answer the finding; Windows is covered by construction only, and the plan says so.
- R6: holds. Every offset and change time is now written in the plan and written by hand; I checked each of them (see above).
- R7: holds. The four main-path choices come first in "Not on Learn" and in the docs row.
- R8: holds in part, accepted. Done when 1 now names the exception, and keeping `T80` an error (an hour 80 is no time, like any unreadable timestamp) is a reasoned choice.
- R9: holds. The double and the "custom parameters" sentence are under "Not on Learn". Refusing strings past seven significant digits would refuse Learn's own `'.12345678'` and `'12,3,4,5678'`, so not adopting it is right.

### High

No findings.

### Medium

#### R10. Row 7 (a) and (b) convert to `UTC` with the default format, which row 5 makes `unsupported`
- Area: 9 Testability
- Finding: Row 7 (a) runs one `Theory` row per zone of row 4, `UTC` included, with `convertFromUtc` and no format, and asserts the clock time. Row 5 makes the destination `UTC` with the `o` format (the default) `unsupported`. So the `UTC` row cannot pass as written. Row 7 (b) says the spring clock times of `Pacific Standard Time` and `AUS Eastern Standard Time` "convert back to their UTC times with `convertTimeZone`", which has the same problem: the destination is `UTC` and no format is given.
- Suggested fix: say in row 7 that every conversion to `UTC` carries a format without a zone, `'yyyy-MM-ddTHH:mm:ss'` (row 5's test list already uses it), and give the format for all rows of (a) so they compare text of one shape.
- Resolution: fixed: row 7 gives every conversion of (a) and (b) the format `'yyyy-MM-ddTHH:mm:ss'`, which the `UTC` destination needs; the tests already did

### Low

#### R11. Row 7 (c) test details that would make it fail or leak into other tests
- Area: 9 Testability
- Finding: (c) "checks the computer's offset is now -08:00". `TimeZoneInfo.Local.GetUtcOffset` is -07:00 in summer, so a check at the current time fails for half of the year. The plan restores `TZ` after but does not say it clears .NET's time-zone cache again; without that, every test that follows in the process reads Pacific time. "A non-parallel collection" is also the one setting xUnit needs (`DisableParallelization`) for this to hold; a plain collection still runs beside others.
- Suggested fix: check the offset at a fixed January instant; clear the cache on restore, in a `finally`; name `DisableParallelization` for the collection.
- Resolution: fixed: row 7 (c) checks `BaseUtcOffset`, names `DisableParallelization`, and restores `TZ` and clears the cache again in a `finally`; the test does each

#### R12. The bounds of a zone's window are not stated as UTC instants
- Area: 9 Testability
- Finding: Row 5 says the instant "must lie from 1 January of each zone's first year ... through 2030". Its own tests take `2008-01-01T00:00:00Z` for `Pacific Standard Time` (that is 31 December 2007 on Pacific's clock) and refuse `2007-12-31T23:59:59Z`. Harmless for every zone listed (no rule changed between the two), but "1 January" is read as UTC in the test and could be read on the zone's clock by the code; a UTC+13 zone shows the difference. For a zoneless source clock time in `convertTimeZone`, which instant is meant is also unsaid.
- Suggested fix: say the bound is on the UTC instant, for the destination and the source alike (for a zoneless timestamp, the instant it denotes on the source zone's clock), and test one zoneless time on each side of a bound.
- Resolution: fixed: row 5 states the bounds as UTC instants, the later of the two zones' first years, with the Pacific example; two tests put a clock time with no zone on each side of the first bound (`2007-12-31T16:00:00` and `15:59:59` on Pacific's clock)

#### R13. The punctuation rule refuses four names, and the plan names three
- Area: 9 Testability
- Finding: Row 5 lists `W. Europe Standard Time`, `Central Standard Time (Mexico)` and `UTC-11` as the names `convertTimeZone` refuses. `E. South America Standard Time` has a dot too, so it is refused as well (the rule is "a character other than a letter or a space"). A reader of the plan, the docs row or a test list gets the wrong count, and no test shows the fourth.
- Suggested fix: list all four in row 5, in the docs bullet, and in the `unsupported` tests (one `convertTimeZone` to `E. South America Standard Time`).
- Resolution: fixed: row 5 and `docs/FLOW-TESTS.md` name all four refused names, and a test refuses `convertTimeZone` to `E. South America Standard Time`

#### R14. The docs should say a whole float's text is the simulator's, in so many words
- Area: 9 Testability
- Finding: After the R4 resolution the only remaining false-pass path is a flow test whose expected text came from the simulator (`concat('Total ', float('14'))` gives `Total 14.0`). The plan says the text is "not Learn's" in `Not on Learn` and Q12, but row 9 lists only "`float`'s styles, locales and `.0`" for the docs, so a maker reading `docs/FLOW-TESTS.md` may not learn it.
- Suggested fix: add to row 9 one docs sentence: do not assert the text of a float with no fraction; the simulator writes `14.0` and the service's text is not on Learn.
- Resolution: fixed: `docs/FLOW-TESTS.md` says to compare a whole float as a number and not assert its text, and names the four choices a capture should check first; row 9 says so

## Third pass

Run against HEAD `046ee27` by the same reviewer with a fresh read, this time with the code and tests (`git diff a511881 HEAD` over `src`, `tests` and `docs/FLOW-TESTS.md`). The first- and second-pass text above is unchanged; the `plan-hash` line now names the current plan. The coordinator's `git` slip put the worktree on `0fb8f3d` for a moment while I worked; I re-checked that HEAD is `046ee27`, rebuilt, and re-ran everything below on it. Still no `powerops plan facts`, `pac`, `az`, tenant call or GitHub write; nothing committed.

What I ran: `dotnet test --project tests/PowerOps.Core.Tests` (2,786 tests, 2,784 passed, 2 skipped, 0 failed), and a probe of about 60 expressions in a scratch project outside the repo that calls `Expression.Evaluate` on the built library (zone extremes, formats, `float` strings, Egypt clock times, extreme dates). No .NET exception escaped in any of them; every refusal was an `ExpressionException` or `UnsupportedException`.

What held:
- R10: holds. The `UTC` row of the 24-zone `Theory` and the convert-back cases give every conversion to `UTC` the format `'yyyy-MM-ddTHH:mm:ss'`; row 7 says so; `UTC` with `o` or `O` and no format stays `unsupported` (tested).
- R11: holds. The test checks `BaseUtcOffset`, sits in a `DisableParallelization` collection, and restores `TZ` and clears the cache in a `finally`; it is skipped off Linux through `Assert.SkipUnless`. The six expressions are the ones row 7 (c) lists.
- R12: holds. The code compares the UTC instant with 1 January 00:00 UTC of the later first year and with the end of 2030; two tests put a zoneless Pacific clock time on each side of the first bound; the probe confirms.
- R13: holds. The code refuses any name with a character other than an ASCII letter or a space in `convertTimeZone`; all four names (`W. Europe`, `E. South America`, `Central Standard Time (Mexico)`, `UTC-11`) have a test; `convertFromUtc` takes them.
- R14: holds. `docs/FLOW-TESTS.md` says to compare a whole float as a number, with the `Total 14.0` example, and names the four capture-first choices.

The code does what rows 1 to 9 say and nothing more: the shared `Add` for the four adders, `startOfDay` and `dayOfWeek` refusing an offset, the 24-name table with exact comparison, the UTC-instant window, `Unspecified` results, `float` with its styles and locales and `.0`, and the 47 names. The tests cover every case the rows list, with the exceptions in R15. I found no unneeded code. The errors-versus-`unsupported` split matches the plan in all probed cases, including `float('0x10')` and `float('$5')` (errors) and `float(' 1')` and `float('1e999')` (`unsupported`). The Core tests changed for `addHours` as the unsupported example now use `dayOfYear`, and the goldens follow.

### High

No findings.

### Medium

No findings.

### Low

#### R15. Two cases row 5 lists are not in the tests, and the hostile list has no negative-offset extreme
- Area: 9 Testability
- Finding: Row 5's `unsupported` list has `UTC` as the destination with `'zzz'` and with `'u'`. The tests refuse `'zzz'` and `'u'` only with `Pacific Standard Time` as the destination, and `UTC` only without a format and with `o` or `O`. The code does refuse both (`FormatAs` on a result with no zone; the probe confirms), so this is a gap in the tests, not in the code. `Hostile_input_never_escapes_as_a_dotnet_exception` covers `Tokyo Standard Time` and `UTC-11` at year 1 and year 9999 but no zone with a negative offset at 9999 or a positive one at 0001 for `convertFromUtc` (the probe shows `Pacific Standard Time` at 9999 and at year 1 and `New Zealand Standard Time` at year 1 give `unsupported`, so the code is right).
- Suggested fix: add the two `UTC`-destination cases and `convertTimeZone('9999-12-31T23:59:59', 'Pacific Standard Time', 'UTC', 'yyyy')` and the year 1 case for `New Zealand Standard Time` to the existing lists.
- Resolution: fixed: tests refuse `convertFromUtc` to `UTC` with `'zzz'` and `'u'`; the hostile list adds New Zealand at year 1 and Pacific at year 9999

#### R16. Egypt's clock-time checks are not tested, and the registry stores its end 1 ms off the IANA rule
- Area: 9 Testability
- Finding: Egypt's daylight saving ends at 24:00 on the last Thursday of October. The IANA data states 24:00; Windows' registry states Thursday 23:59:59.999. Row 7 pins the instants (20:59Z and 21:00Z), which both agree on, but the `IsInvalidTime` and `IsAmbiguousTime` checks that decide `unsupported` for a clock time are tested only for Pacific and AUS Eastern. The probe on Linux gives the expected results (`2026-10-29T22:30:00` converts, `23:30:00` is repeated, `2026-04-24T00:30:00` is skipped, `2026-04-23T23:30:00` converts), but nothing makes the Windows job check them, and the two data sets can differ by that millisecond at the edge of the ambiguous range.
- Suggested fix: add the four Egypt clock times above to the repeated-or-skipped test (two `unsupported`, two converted to `19:30` and `21:30` UTC) so the Windows job checks the registry's reading; say in `docs/FLOW-TESTS.md` that a clock time within a millisecond of a change can differ.
- Resolution: fixed: a skipped (`2026-04-24T00:30:00`) and a repeated (`2026-10-29T23:30:00`) Egypt clock time are tested as `unsupported`; both lie inside the change on either data's reading, so the Windows job checks them

#### R17. `Zone` can let a .NET exception through when the zone data can't be read
- Area: 9 Testability
- Finding: `Zone` catches `TimeZoneNotFoundException` and `InvalidTimeZoneException`. The `FindSystemTimeZoneById` page lists a `SecurityException` for a process without permission to read the registry key, and on Linux reading the zone file can fail with an I/O or access error. Those would escape an expression as .NET exceptions, which the contract (only `ExpressionException`, `UnsupportedException` and `UnmockedException`) forbids. It needs a locked-down machine, so it is unlikely, but a sandboxed CI job is where the Action runs.
- Suggested fix: catch `SecurityException`, `UnauthorizedAccessException` and `IOException` beside the two and refuse as `unsupported` with the same message ("the computer's zone data can't be read").
- Resolution: fixed: `Zone` also turns a `SecurityException`, `UnauthorizedAccessException` or `IOException` from reading the zone data into `unsupported`

#### R18. A `float` over 15 significant digits is rounded and written with an exponent, with no test showing it
- Area: 9 Testability
- Finding: `float('123456789012345678')` gives `1.2345678901234568E+17` and a number of 30 digits gives an `E+29` form. The plan names the double's precision as a capture-first choice, so this is known; but no test shows what such a value looks like, and a flow test that expects the exact digits fails (a safe direction) while one that copies the simulator's output passes and may differ in the cloud. The probe also shows `float('-0')` gives `-0.0` and `float(-0)` gives `0.0` in the expression language (no negative literal), which is harmless.
- Suggested fix: add one `Float` case for a 17-digit string with its rounded value, and say in `docs/FLOW-TESTS.md`'s float line that digits past the 15th are rounded.
- Resolution: fixed: a test shows `float('123456789012345678')` gives `1.2345678901234568E+17`; the double's precision is already one of the four capture-first choices in the plan and the docs
