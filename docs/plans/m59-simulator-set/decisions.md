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

## Log

- Learn read 2026-11-05: the function reference from its published source on GitHub (`MicrosoftDocs/azure-docs`, `articles/logic-apps/expression-functions-reference.md`, `ms.date` 07/21/2026), the Default Time Zones page, and .NET's `TimeZoneInfo` and ICU pages through the Learn MCP tools; quoted in the plan's Assumptions
- .NET's behaviour checked 2026-11-05 with a scratch program on this machine (Linux, ICU, IANA data 2026f)
- depth: quick: the improvement plan gives the scope, the brief gives the functions and the Windows and Linux requirement
- questions answered by the building session under the user's delegation of 2026-10-22 (autonomous milestone mode)
- areas not asked: users, screens, solution and publisher, environments; the milestone is a change to this repo with no tenant
- approved 2026-11-05 by the building session under the user's delegation of 2026-10-22, for a fresh-context review
