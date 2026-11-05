# Decisions: m58-flow-conformance

## Round 1

### Q1. What does `int` do with a value that is not a string?
1. An integer number gives itself; any other non-string value is `unsupported` (recommended)
2. Every non-string value is an error, as Learn's parameter type (String) suggests
3. Every non-string value is `unsupported`
Answer: 1. Learn types the argument as a String and says nothing about a number. The run's expression is `int(parameters('...'))`, and in the run `addDays` took the same parameter directly as its Integer days, so the parameter most likely gave a number; option 3 would leave the run's own expression `unsupported`, and option 2 would fail it. Returning the number is the simulator's choice, listed under "Not on Learn" and checked by the capture (`int(14)` and `int` of the Number environment variable). A decimal number, a boolean, null, an object or an array is `unsupported`, never a guess: Learn's conversion intro mentions turning a Boolean into an integer without saying how `int` does it.

### Q2. How does `formatDateTime` read a timestamp with no time zone?
1. It keeps no zone, so the default `o` format ends without `Z` (recommended)
2. As UTC, as `addDays` reads it
Answer: 1. Learn's own example: `formatDateTime('03/15/2018')` gives `'2018-03-15T00:00:00.0000000'`, with no `Z`. .NET's parse with `AdjustToUniversal` gives exactly that and never consults the machine's time zone.

### Q3. And a timestamp with an offset?
1. Converted to UTC before formatting (recommended)
2. `unsupported` until the capture shows it
Answer: 1. `addDays` already does this (`ExpressionTests.cs:767`, an M14 choice), so `formatDateTime(addDays(...))` and `formatDateTime(...)` agree. Not on Learn; the capture checks it.

### Q4. Which culture formats `addDays`'s format argument?
1. `en-us`, as `formatDateTime`'s default (recommended)
2. The invariant culture
Answer: 1. Learn's `utcNow` examples use the same format argument and show en-US text (`'d'` gives `9/25/2025`, `'D'` gives `Thursday, September 25, 2025`); the invariant culture would give `09/25/2025` and `Thursday, 25 September 2025`. Not on Learn for `addDays` itself; the capture checks `'D'`.

### Q5. ICU writes U+202F (a narrow no-break space) before AM/PM in en-US times. What does the simulator write?
1. A plain space (recommended)
2. Whatever the platform's culture data gives
Answer: 1. Learn's examples (`'g'` gives `6/15/2009 1:45 PM`, `utcNow('t')` gives `5:18 AM`) show a space, and a test file written from them would fail on Linux with option 2, and pass or fail on Windows depending on its ICU. The replacement also turns a U+202F typed into a format string into a space; that case is unlikely and carries a `shortcut:` comment. The capture checks `'g'`.

### Q6. `U` and the `z` formats on a timestamp with no zone?
1. `unsupported` (recommended)
2. Format as if the zone were UTC
Answer: 1. .NET converts such a value from the machine's time zone for `U` and prints the machine's offset for `z`, `zz` and `zzz`, so the result would change with the computer that runs the test. What the service does is not on Learn; the capture records it, and a later `fix(core):` can follow it. A literal `z` inside quotes in a custom format is refused too; the check is a plain `contains`, which can only refuse more, never pass a wrong value.

### Q7. An empty format or locale?
1. `unsupported` (recommended)
2. .NET's meaning: `''` as format is `G`, `''` as locale is the invariant culture
Answer: 1. Learn says the format defaults to `o` and the locale to `en-us` when unspecified, and doesn't say whether an empty string counts as unspecified. .NET's meanings differ from both defaults. The capture can settle it.

### Q8. Does `addDays` keep reading a timestamp with no zone as UTC?
1. Yes, for now; the capture checks `addDays('2018-03-15', 1)` (recommended)
2. Change it now to keep no zone, as `formatDateTime` does
Answer: 1. Learn's `formatDateTime` example suggests the service keeps no zone, so `addDays` may too, but that is an inference about another function, and changing a shipped M14 behaviour is outside M58's brief. Part 2's capture decides; a difference becomes a `fix(core):` change with its test.

### Q9. The cookbook says `addDays` "also accepts fractional values". Does the simulator follow it?
1. No: keep the reference's Integer and the existing error for `1.5`; the capture checks fractional days (recommended)
2. Yes, now
Answer: 1. The function reference, the primary page, types `days` as Integer, and the cookbook is marked as written with AI assistance. The existing test (`ExpressionTests.cs:775`) stays until the capture shows the service's behaviour.

### Q10. Where does the acceptance case run through `powerops test`?
1. A CLI test on a temporary copy of the seed fixture whose `Update_a_row` holds the run's expression, plus a Core test with an `Int` parameter (recommended)
2. A new fixture solution in `tests/fixtures/`
Answer: 1. The seed already has an environment variable bound to a flow parameter, and `TestCommandTests` already edits temporary copies (`CopyOf`, `Edit`). A new fixture would change the `analyze-fixtures.*` goldens for no gain. The seed's parameter is a String, so the CLI case gives `int` a string; the Core case gives it a number, which is what an Int parameter gives.

### Q11. How is part 2 split from part 1?
1. Two PRs: part 1 offline now; part 2, the capture, after the person's yes, based on `main` once part 1 has merged (recommended)
Answer: 1, as the master session's brief asks. Part 1 changes no tenant and can merge alone; the capture may show differences that need `fix(core):` changes in the same PR as the fixture that shows them.

## Round 2 (from the review)

### Q3, amended (R1)
A timestamp with an offset other than `Z` is `unsupported` in `formatDateTime` and in `addDays` with a format. Learn says the default format "preserves time zone information", which reads against converting to UTC, and the conversion was an unchecked M14 choice; a test written from the simulator's `08:30` could pass where the service gives `10:30`. `addDays` without a format keeps its M14 reading (Q8). `addDays` with a format also refuses a timestamp with no zone: with it, `addDays('2018-03-15', 0, 'o')` and `formatDateTime('2018-03-15')` would disagree. The run's expression is not affected: `utcNow()` gives text ending in `Z`.

### Q6, amended (R10)
`u`, `r`, `R` and formats holding `K` are refused on a timestamp with no zone too: they label the value as UTC or print nothing, and the service's label is not on Learn. `o` stays, because Learn's `formatDateTime('03/15/2018')` example shows it.

### Q1, kept (R2)
`int` of an integer number still gives itself, now only within 32 bits. Without it, the run's original expression with a Number environment variable is `unsupported`, the F4 finding this milestone exists to close. It is the one value part 1 gives that no Microsoft page states; the plan's Done when 2 says so, and the capture runs `int` on the Number environment variable.

### Q12. Which strings does `int` read? (R2)
1. Only an optional `-` and digits with no leading zero, within 32 bits; a decimal string or a string with no number is an error; anything else `unsupported` (recommended)
2. .NET's 64-bit integer parse with a leading sign
Answer: 1. Learn's section gives `int('10')` and "the limits for the integer data type" with no numbers; the cookbook gives the decimal-string error. Spaces, `+`, leading zeros, exponents and the width of the integer type are not on Learn, so they are refused. A word (`'abc'`) has no integer version at all, so it is an error.

### Q13. An invalid format in `formatDateTime`? (R4)
1. `unsupported`; an error only in `addDays`, whose section says so (recommended)
2. An error in both
Answer: 1. `formatDateTime`'s format row lacks the sentence "If the format isn't a valid value, an error is generated" that `addDays` and `utcNow` have.

### Q14. Which locales? (R5)
1. `en-us`, `fr-fr` and `es-es`, as Learn shows them; a name .NET doesn't know is an error; any other known name `unsupported`; no culture data at all (invariant globalization) `unsupported` (recommended)
2. Every locale .NET knows
Answer: 1. Culture data comes from ICU on both platforms and from the service's own data in the cloud; a culture Learn doesn't show could give other text, and a pass would then be a guess. An unknown name is an error because Learn says so. Invariant mode used to break the type's static initializer; now it gives `unsupported` with the reason.

### Q15. What does Done when 2 prove? (R6)
Answer: the rewrite `addDays(utcNow(), parameters(...), 'yyyy-MM-dd')` gives the cloud's 2026-11-18 (the Core test runs it exactly so, with an `Int` parameter); the original with `int` gives the same through `powerops test`, on the simulator's reading of `int` (Q1), until part 2 runs it in the cloud. The assertion is the input `item/pops_name` of `Update_a_row`.

### Q16. How is part 2 run and compared? (R7, R8, R9, R11)
Answer: the plan's "Environments and route" now names Dev and the read-only checks before the yes; builds the solution as files, packs and imports it with `pac` and a deployment settings file that binds an existing connection; runs it once; takes every failing case's input from a Compose output so the flow saves; calls the capture complete when every action has a status; scrubs row data, keys, owner ids, etags, `@odata` links, timestamps, workflow fields and headers as well as ids and hosts; compares status, inputs, outputs and the error code of a failed action, not the message text; runs the leak scan and reads the fixture diff before the second PR; and states the licence, DLP and role evidence from the run of 2026-11-04, which the person confirms at the yes.

## Round 3 (from the review's second pass)

### Q17. Which timestamp text is read? (R12)
1. Full ISO 8601 timestamps and `MM/dd/yyyy` with an optional time, as Learn's examples use; other text .NET reads as a date is `unsupported`; text it can't read is an error (recommended)
2. Whatever `DateTime.TryParse` accepts
Answer: 1. .NET fills the missing parts of `'10:30'` or `'March 15'` from today's date, so a test would pass today and fail tomorrow, with a value Learn never states. M14's reader for `addDays` without a format and `utcNow`'s fixed value had the same gap and gets the same check; every timestamp in the repo's tests and fixtures is in one of the accepted forms. The check uses `DateTimeOffset`, so it never converts through the machine's zone.

### Q14, amended (R13)
With `fr-fr` and `es-es`, only custom formats of the day, month and year fields Learn shows them with are accepted. Learn shows these locales only with `'dddd MMMM d'`; their day periods, separators, standard patterns and abbreviations come from ICU here and from the service's own data in the cloud, and they changed between CLDR versions, so `'g'` or `'tt'` with them could pass on one CI platform and fail on the other.

### Q12, amended (R14)
An error only for a decimal string (the cookbook) or a string with no digit, which has no integer version at all. `'12abc'`, `'1,000'` and non-ASCII digits are `unsupported`: the service may read them differently.

### Q18. How does part 2 know it is in Dev? (R15)
The read-only checks before the yes add `pac admin list`, whose type column must say Developer; a trial or any other type stops the session. The CLI test's name no longer claims the cloud's value: the cloud ran only the rewrite without `int`, which the Core test runs.

## Round 4 (from the capture)

### Q19. How is the capture compared, when the simulator stops a run at its first refused action?
1. A data-driven Core test (`CaptureTests`) that takes the refused actions out, runs the rest with the run's List rows outputs as the mock, compares action by action, and runs each refused action alone to check it stays `unsupported` (recommended)
2. Flow test files in `tests/fixtures/flowtests/capture/`, as the plan's Steps say
Answer: 1. A flow test runs the whole flow, so the first of the 29 refused actions would end it as `unsupported` and compare nothing after it. The record is the run itself (`run.json`), which a flow test file can't load. The plan's body is hashed, so the change is recorded here and in `capture.md`.

### Q20. How was the solution built?
Answer: as files in the export layout, zipped and imported with `pac solution import`, instead of `pac solution pack` from the unpacked layout. Same import, one tool fewer; the fixture is the service's own export, unpacked with `pac solution unpack`, so nothing in the repository is hand-written solution XML.

### Q21. Where are the "Not on Learn" items marked (row 9)?
Answer: in `capture.md`, "M14 and M58 Not on Learn items". Marking them in `plan.md` or M14's plan would change a signed-off body and its hash.

### Q22. What happens to the cases the simulator refuses?
1. They stay `unsupported`; their cloud values are recorded in `capture.md` and `expected-unsupported.json` for M59 (recommended)
2. Support them now from the record
Answer: 1. One run shows one value per case, and several (partial timestamps, `int`'s 64-bit reading) depend on the run's day or on parsing rules wider than the case; supporting them is new behaviour, outside part 2's brief. The test fails if one of them starts to run, so M59 has to look at the record.

### Q8, Q14 and Q12, settled by the capture
- Q8: `addDays` without a format keeps the timestamp's zone: none stays none, and an offset is converted and written as `+00:00` (`A06`, `A11`). Changed in a `fix(core):` commit.
- Q14: an unknown locale name gave English text, not an error, so it is now `unsupported` (`F18`).
- Q12: the cloud reads `int(' 1')`, `int('+5')`, `int('007')`, `int('1,000')` and `int('2147483648')`; they stay `unsupported` (Q22). Decimal strings, a word and `''` are errors there too, so the errors stand.
- Q1, Q4, Q5 and Q9 hold: `int(14)` and `int` of the Number variable gave 14, `D` and `t` gave en-US text with a plain space, and fractional days were an error.

## Log

- Learn checked 2026-11-04 with the Learn MCP tools (a researcher subagent, Sonnet, quoting the pages); the pages are in the plan's Assumptions
- .NET's behaviour checked 2026-11-04 with a scratch program on this machine (Linux, ICU)
- depth: quick: the improvement plan gives the design, the brief gives the split
- questions answered by the building session under the user's delegation of 2026-10-22 (autonomous milestone mode)
- areas not asked: users, screens, solution and publisher; part 1 is a change to this repo, and the plan answers each for part 2
- approved 2026-11-04 by the building session under the user's delegation of 2026-10-22, for a fresh-context review
- fresh review (a general-purpose subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-11-04: 1 High, 8 Medium, 2 Low (R1 to R11); answered in `plan.md` (header `environment`, Goal, Done when 1, 2 and 5, rows 1 to 4, 6 to 9, Not on Learn, Environments and route, Assumptions, Steps) and here (Round 2)
- part 1's code built by an implementer subagent (Sonnet, `powerops-implementer`) in this worktree from rows 1 to 7, test first (54 new tests failed before the code), then changed to the review's rules
- the same reviewer's second pass 2026-11-04 at `fc427a1`: R1 to R11 hold; 2 Medium and 2 Low new (R12 to R15); answered in `plan.md` (rows 1, 3, 4, Not on Learn, Environments and route) and here (Round 3); the code changed by the same implementer
- the same reviewer's third pass 2026-11-04 at `4475468`: R12 to R15 hold; the full suite passed (3,565, 5 skipped); 0 High, 0 Medium, 1 Low new (R16), fixed in `docs/FLOW-TESTS.md`. In all: 1 High, 10 Medium and 5 Low, every one fixed
- signed off 2026-11-04 by the building session under the user's delegation of 2026-10-22, with no High or Medium finding open (`powerops plan sign-off`; `signed-off-hash` equals `review.md`'s `plan-hash`). Status stays signed-off: part 2, the capture, waits on the person's yes
- the person said yes to part 2 in Dev in the M58 session on 2026-11-04; the read-only checks passed first (`pac org who`, `pac admin list` type Developer, `pac solution list` with the seed, one Connected Dataverse connection)
- the capture ran 2026-11-05 (UTC): import, one run, the run history read with the person's existing `az` sign-in, two exports and the seed and M12 exports; nothing deleted; what was left is in `capture.md`
- the capture's differences fixed by the same implementer (Sonnet, `powerops-implementer`) in `fix(core):` commits, each with a unit test; `CaptureTests` passes with nothing excluded but the 29 refused actions (Round 4)
