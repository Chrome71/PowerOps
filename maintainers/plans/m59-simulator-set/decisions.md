# Decisions: m59-simulator-set

## Round 1

### Q1. How do `addHours`, `addMinutes` and `addSeconds` read a timestamp?
1. Exactly as `addDays`: one adder with the unit as its only difference (recommended)
2. Only a timestamp ending in `Z`, Learn's examples; everything else `unsupported`
Answer: 1. Learn gives the four sections the same parameter table and the same format text; the capture showed how the service reads `addDays`'s timestamp (`A06`: no zone stays none; `A11`: an offset converted, written `+00:00`), and its record reads as the service's timestamp parsing, not as something `addDays` does alone (`capture.md`, "What the values show together"). Option 2 would make `addHours(x, 1)` and `addDays(x, 1)` disagree on the same `x`. Listed under "Not on Learn".

### Q2. `startOfDay` and `dayOfWeek` of a timestamp with no zone, or with an offset?
1. No zone: the date as written, and `startOfDay` keeps no zone; an offset: `unsupported` (recommended)
2. Both converted to UTC first
Answer: 1. A timestamp with no zone has one date, and `startOfDay(convertFromUtc(utcNow(), '...'))`, the start of a local day, needs it. For an offset, the day itself depends on the zone it is taken in: `2018-03-15T01:00:00+02:00` is the 15th on its own clock and the 14th in UTC, and the capture's `+00:00` reading is for adding time, where the instant doesn't change. Neither Learn nor the capture shows it.

### Q3. `startOfDay` with a format?
Answer: as `addDays` with a format: the timestamp must end in `Z` (M58 decisions Q3, amended). One rule for every date function with a format.

### Q4. Which time-zone names?
1. Windows ids as Windows writes them, recognised by ICU's Windows-to-IANA mapping both ways, and found by `TimeZoneInfo` (recommended)
2. Anything `TimeZoneInfo.FindSystemTimeZoneById` finds
3. Only the names Learn's examples use (`UTC`, `Pacific Standard Time`)
Answer: 1. Learn names Windows names. `FindSystemTimeZoneById` also takes IANA names and any casing, on both platforms, and the service is documented with neither, so option 2 would pass names the service may refuse. ICU's mapping is one data set on both platforms, so the check decides the same on Windows and Linux. Option 3 is too narrow to use. Learn's convertTimeZone note "you might have to remove any punctuation" is not followed: the page it links to writes `W. Europe Standard Time` with its dot and uses it in its own DISM example, and an id without its punctuation (`W Europe Standard Time`) is a name neither Windows nor ICU knows. Listed under "Not on Learn". A name no data knows is `unsupported`, not an error: Learn doesn't say, and the capture found an unknown locale was no error where Learn says it is (`F18`).

### Q5. Where do the time-zone rules come from, and how do Windows and Linux agree?
1. The computer's data through `TimeZoneInfo`, from 2007 on, with the tests pinning 16 zones on both CI platforms (recommended)
2. A rules table in PowerOps
Answer: 1. On Windows `TimeZoneInfo` reads the registry, on Linux the IANA database through ICU's mapping. Learn says the registry "does not necessarily contain historic time zone data" and that most zones with daylight saving time have "only one or two predefined adjustment rules", while the IANA database keeps every past rule, so an old date can convert differently on the two. An instant before 2007 (the year the United States, whose zone is Learn's example, last changed its rules, and the year of Windows Vista, which Learn names as the Windows that keeps per-period rules) is `unsupported`. From 2007 on both hold the current rules; the tests check them for 16 zones, three of them with rules changed since 2019, on Windows and Linux. A database that one computer has not updated after a country changes its rules can still differ; `docs/FLOW-TESTS.md` says so. Option 2 is a database to maintain. After 9998 is `unsupported` too, because .NET's conversion would clamp at the end of the calendar instead of failing.

### Q6. A timestamp ending in `Z` with a source zone other than `UTC`, or with an offset?
Answer: `unsupported`. The text's zone and the source zone disagree, and Learn doesn't say which wins (.NET refuses the call). With no zone, the timestamp is a time on the source zone's clock, as the function's description says.

### Q7. A source clock time that daylight saving skips or repeats?
Answer: `unsupported`. .NET throws for a skipped time and takes standard time for a repeated one; the service's choice is not on Learn.

### Q8. `UTC` as the destination?
1. `unsupported` with the `o` format, the default; any other format as usual (recommended)
2. Written with `Z`, as .NET does
3. Written with no offset, as Learn's convertFromUtc text says of every result
Answer: 1. Learn's convertFromUtc says its result has no offset, convertTimeZone's says only "converted to the target time zone", and neither shows `UTC` as the destination; .NET's conversion to UTC gives a UTC time, which `o` writes with `Z`. A format without a zone (`'yyyy-MM-ddTHH:mm:ss'`) gives the same text either way.

### Q9. Which culture formats the conversions' format, and is an invalid format an error?
Answer: `en-us`, as `addDays`'s format (M58 decisions Q4, confirmed by the capture for `addDays`); an invalid one is an error, as each M59 date section says. Zone formats on the result, which has no zone, are `unsupported` (M58 decisions Q6).

### Q10. Which strings and locales does `float` read?
1. The styles Learn's examples need (sign before or after, decimal point, thousands separators), no locale as en-US, `en-US` and `de-DE` as locales; what only a wider style reads `unsupported`; what no style reads an error (recommended)
2. .NET's widest number style with any culture .NET knows
Answer: 1. Learn's six "unexpectedly valid inputs" show a trailing sign and thousands separators anywhere before the decimal point, which .NET's `double.Parse` reads with those styles; whether the service also reads spaces, exponents or parentheses is not on Learn. The invariant culture and en-US have the same separators, so the default locale gives Learn's first example either way. `de-DE` is Learn's own example; other cultures' separators come from ICU here and from the service's data in the cloud. A locale name no data knows is `unsupported`, as for `formatDateTime` after the capture's `F18`. `NaN` and infinity, which .NET reads, cannot be JSON numbers.

### Q11. `float` of a value that is not a string?
1. A number gives itself as a float; anything else `unsupported` (recommended)
2. Every non-string `unsupported`
Answer: 1, as M58 decisions Q1 did for `int`, which the capture confirmed (`I03`, `I04`): `float(parameters('Amount'))` with a Number environment variable gives a number. Listed under "Not on Learn".

### Q12. How is a float with no fraction written, and what of Learn's `T80`?
Answer: as `14.0`. `System.Text.Json` writes the double `14.0` as `14`, which the simulator then takes as an integer: `addDays(x, float('14'))` would pass, while the capture's `A10` shows the service checks the type ("The provided value is of type 'Float'") and `R02` shows the integer `14` passes. Built as `14.0`, it is a number that equals `14` and that `addDays` and `int` refuse. The text `string(float('14'))` gives is `14.0`; the service's is not on Learn ("Not on Learn"). Learn's second convertTimeZone example has the timestamp `2018-01-01T80:00:00.0000000Z`, with an hour 80; read as written it is an error, so the test uses `T08`, which the first example has, for its `'D'` result.

### Q13. A new capture in Dev?
1. Not in this milestone; the "Not on Learn" list names what a later capture should check (recommended)
2. Extend the M58 capture flow now
Answer: 1. The capture holds no call to an M59 function, so there is no cloud-matched case to add. A new run needs the person's yes in Dev and is optional for M59; the brief allows skipping it. Every value M59 gives is one Learn states or one of the choices listed under "Not on Learn".

### Q14. The tests that use `addHours` as their unsupported example?
Answer: they use `dayOfYear('2026-01-01T00:00:00Z')`, a function M59 doesn't add, and the `test-failures.*` goldens change by the recipe. `docs/FLOW-TESTS.md`'s example becomes `convertToUtc`.

## Round 2 (from the review)

### Q5, amended (R1, R6)
1. The 24 zones of plan row 4, each from the first full year after its last rule change through 2030, every year pinned by a test on both platforms (recommended)
2. Every Windows id from one floor year
Answer: 1. Nothing showed that Windows and the IANA database agree from 2007 for every zone: Russia, Brazil, Mexico and Egypt changed their rules since, and other zones change every year or two. A zone is now taken only where a test pins its January and July offsets for every year of its window and both CI platforms pass it, so the claim covers exactly what is checked. The first years follow the zones' rule history in the IANA database; the upper end is 2030, because a later date is more likely to fall under rules not yet published, and Learn's 2018 examples stay inside the window. The expected offsets and change times are written in the plan by hand from each zone's published rule, not taken from a run of the code; this machine's data gives each one, and Windows' registry is checked by the Windows job. What can still differ is named in the plan: the change times of other years, and a rule change before 2030 that one computer's data has and another's lacks.

### Q4, amended (R2, R3)
The names are the 24 ids of the table, compared exactly, and found by `TimeZoneInfo`; the ICU round trip is gone, because Windows' and Linux's ICU versions can differ and a name could then be taken on one side only. In `convertTimeZone`, a name with punctuation is `unsupported`, after that section's sentence "you might have to remove any punctuation from the time zone name"; `convertFromUtc`'s section has no such sentence, and the page both link to writes the dots, so `convertFromUtc` takes them. The test of `Türkiye Standard Time` is dropped: it would flip when one platform's data learns the name. Every name of the table is accepted in the tests of plan row 7, so a platform that lacks one fails.

### Q12, amended (R4, R8)
No test asserts the text of a whole float (`string(float('14'))`); its `14.0` is the simulator's, listed under "Not on Learn" as a capture case. The `.0` stays, because it keeps `addDays(x, float('1'))` an error, as the capture's `A10` shows the service checks the type; a copy of the value (`json(string(...))`) keeps `14.0`. `float('abc')` and `float('')` being errors is listed as the simulator's choice. Learn's `T80` example stays an error: an hour 80 is no time, and every unreadable timestamp is an error (M58 plan row 1). Done when 1 says this example is the exception.

### Q15. Does the computer's own time zone change a result? (R5)
Answer: no, and a test shows it. Every conversion result is made a `DateTime` of kind `Unspecified` before it is written, and the readers take an offset only to convert it or to refuse it. On Linux a test sets `TZ` to `America/Los_Angeles`, clears .NET's time-zone cache, and asserts the new functions and `formatDateTime`'s `zzz` give what they give in UTC; it runs in a collection of its own that no other test runs beside, because the setting is the whole process's. On Windows `TZ` doesn't set the zone, so the test is skipped there.

### Q10, amended (R9)
The double stands for Learn's "float data type" and is listed under "Not on Learn" as a capture case: a string with more digits than a single-precision float holds keeps them here. Learn's sentence that `float` can be used "only when passing custom parameters to an app" is not modelled; the simulator runs it wherever an expression is, and the plan says so.

### Q16. Which choices does a capture check first? (R7)
Answer: the four on a real flow's main path: no zone in `startOfDay`, no zone in either conversion, a whole float's `.0`, and the double's precision. The plan's "Not on Learn" list and `docs/FLOW-TESTS.md` name them first.

## Round 3 (from the review's second pass)

### Q5, amended again (R10, R12)
The window's bounds are UTC instants: from 1 January 00:00 UTC of the later of the source's and the destination's first years through the end of 2030 in UTC. A clock time with no zone counts by the instant it is on the source zone's clock. Every conversion to `UTC` in the tests carries `'yyyy-MM-ddTHH:mm:ss'`, because `UTC` with `o` is `unsupported` (Q8).

### Q15, amended (R11)
The machine-zone test checks `BaseUtcOffset`, which doesn't change with the season, runs in a collection with `DisableParallelization`, and restores `TZ` and clears the cache in a `finally`.

### Q4, amended again (R13)
`convertTimeZone` refuses four of the 24 names: `W. Europe Standard Time`, `E. South America Standard Time`, `Central Standard Time (Mexico)` and `UTC-11`.

### Q12, amended again (R14)
`docs/FLOW-TESTS.md` tells a test writer to compare a whole float as a number, not by its text.

## Log

- Learn read 2026-11-05: the function reference from its published source on GitHub (`MicrosoftDocs/azure-docs`, `articles/logic-apps/expression-functions-reference.md`, `ms.date` 07/21/2026), the Default Time Zones page, and .NET's `TimeZoneInfo` and ICU pages through the Learn MCP tools; quoted in the plan's Assumptions
- .NET's behaviour checked 2026-11-05 with a scratch program on this machine (Linux, ICU, IANA data 2026f)
- depth: quick: the improvement plan gives the scope, the brief gives the functions and the Windows and Linux requirement
- questions answered by the building session under the user's delegation of 2026-10-22 (autonomous milestone mode)
- areas not asked: users, screens, solution and publisher, environments; the milestone is a change to this repo with no tenant
- approved 2026-11-05 by the building session under the user's delegation of 2026-10-22, for a fresh-context review
- fresh review (a general-purpose subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-11-05 at `a511881`: 1 High, 5 Medium, 3 Low (R1 to R9); answered in `plan.md` (Done when 1 and 2, Won't build, rows 4 to 7 and 9, Not on Learn, Assumptions) and here (Round 2)
- the code built by an implementer subagent (Sonnet, `powerops-implementer`) in this worktree from rows 1 to 9, test first (147 new tests failed before the code), then changed to Round 2's rules
- the same reviewer's second pass 2026-11-05: R1 to R9 hold (R4 and R8 in part, accepted); 0 High, 1 Medium, 4 Low new (R10 to R14); answered in `plan.md` (rows 5, 7, 9), `docs/FLOW-TESTS.md`, two tests, and here (Round 3)
- the same reviewer's third pass 2026-11-05 at `046ee27`, plan, code and tests: R10 to R14 hold; the Core tests passed (2,786, 2 skipped) and about 60 probe expressions let no .NET exception out; 0 High, 0 Medium, 4 Low new (R15 to R18), fixed in the code and tests. In all: 1 High, 6 Medium and 11 Low, every one fixed or answered
- signed off 2026-11-05 by the building session under the user's delegation of 2026-10-22, with no High or Medium finding open (`powerops plan sign-off`; `signed-off-hash` equals `review.md`'s `plan-hash`)
