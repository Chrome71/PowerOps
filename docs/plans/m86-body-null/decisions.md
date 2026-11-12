# Decisions: m86-body-null

The fix plan after trial 3 (`reports/trial-3/fix-plan.md`, section M86, git-ignored) settled the change; its plan review assigned the code and tests to Sonnet. Every decision below was made by the building session (Opus) inside that plan, and none needed the person.

## The person's instructions this plan runs under

- 2026-11-11, after the fix plan's review and their answers to its four questions: "Start the build". None of the four answers (Q1 "One yes per deployment", Q2 "Hide by default", Q3 "Keep out of Git", Q4 "Rerun it") applies to this milestone.
- Standing: sessions sign off their own milestone plans after clean reviews ("Sessions sign off", 2026-11-09), and merge their own verified pull requests when all checks pass ("Sessions merge", 2026-11-09).

## Round 1 (planning)

### Q1. How far does `body()` read through null?
1. Optional only for null outputs: `Expression.Read(o, "body", o is null)` (recommended)
2. The full `outputs(x)?['body']`

Decided: 1, as the fix plan says. Option 2 would also give null for outputs that exist with no `body` property, which no run settled, and would change the existing test `Body_is_outputs_body_and_errors_when_there_is_none`.

### Q2. The fixture: a new hand-written flow, or a test against an existing fixture flow?
1. A new hand-written solution under `tests/fixtures/flowtests/skipped-body/`, holding the flow and its tests (recommended)
2. A test against an `alm-makers` flow

Decided: 1. The fix plan's Done line names a new folder that holds both, and no existing fixture flow reads `body()` of an action that may be skipped. The flow uses an HTTP action, which a mock answers and which needs no connection reference, so the fixture adds as few `analyze` findings as it can.

## Research

Checked 2026-11-11 by `powerops-researcher`, Learn only, no tenant.

- Skipped: Learn gives no return value. The function reference describes `body()` and `outputs()` only for an action that ran (*Reference guide to workflow expression functions* (learn.microsoft.com/azure/logic-apps/expression-functions-reference), sections body and outputs). *Troubleshoot cloud flow errors* (learn.microsoft.com/power-automate/troubleshoot-flow-errors#action-errors) lists "Referencing dynamic content from a step that didn't run" as a cause of an expression error, without saying whether the error comes from `body()` or from a property read after it without `?`. This plan reads it as the second, which it keeps: after row 1, `body('A')['x']` of a skipped `A` still throws "Cannot read a property or item of null.", and only `body('A')?['x']` gives null. The trial's live run (P5) is the evidence for null, and it used the `?` form.
- Failed: no direct statement. *Handle workflow errors and exceptions* (learn.microsoft.com/azure/logic-apps/error-exception-handling) shows an action after a failed one reading `body(...)?['Terms']`, and a `result()` example where a failed action's outputs hold a body. So a `Failed` mock with `outputs` keeps its body (row 3 (c)); a `Failed` mock without `outputs` giving null is the simulator's choice.
- TimedOut: not covered on the pages checked; Learn lists it only as a `runAfter` status. Null for a `TimedOut` mock without `outputs` is the simulator's choice.
- Outputs without a `body` property: Learn defines `body()` as shorthand for `actions('<name>').outputs.body`, and the schema's `?` operator section says `?` gives null where plain access fails. That fits keeping today's error.

The researcher suggests captures of the failed and timed-out cases. Not in this milestone: the chain is tenant-offline, and the fix plan makes both the simulator's choice, said in `docs/FLOW-TESTS.md` (row 2). A capture is a follow-up for the handover.

## Plan review

- 2026-11-11: Fable plan review, round 1 (`fable-reviewer`, Fable 5.2): REJECTED, five findings: `SolutionDiscoveryTests` missing; nothing in `dotnet test` ran the fixture; "nothing else changes" understated which actions have null outputs; the red and guard test sets not named; the frozen eval set guarded only as "files untouched". All five answered in `plan.md` (Done when 1, 2, 4, 5; rows 3 to 5; Assumptions).
- 2026-11-11: Fable plan review, round 2: APPROVED, with five text corrections, made: the undo check moved to step 5, after the fixture exists; the before and after runs use the worktree's build; the null-outputs list marked "for example" and completed (Wait, an expression error, Parse JSON, containers); `analyze-fixtures.sarif` expected unchanged; a guard test for `body(x)['code']`. Assignments: steps 2 to 4 Sonnet, step 5 Opus (the building session). Fable ran the plan in a scratch copy: the eight eval outputs unchanged, the draft fixture 3 passed with row 1 and 2 failed without it, `analyze` of it no finding.
- 2026-11-11: the eval "before" outputs taken with the worktree's build at `b065f23`, before any code change: all eight cases exit 0 but `solution-03` (exit 1, as at `b065f23`).
- 2026-11-11: `powerops-review-plan` in a fresh context (Opus), first pass: no High, two Medium (R1 no stop rule for a failing mutant; R2 the 2026-11-11 run filed under "Settled by the capture"), three Low (R3 the doc line claimed more than the run showed; R4 the leak scan, merge protocol and build not located; R5 Learn addresses in this file). R1 to R4 fixed in `plan.md`; R5 kept on purpose (public sources, house style).
- 2026-11-11: `powerops-review-plan`, second pass in a fresh context, of the revised plan: no High, no Medium, three Low (R1 the fixture's address and flow state not pinned, which could fire PO006 or PO011; R2 row 2 said "gives null" where Assumptions says "no error"; R3 the Learn addresses again). R1 and R2 fixed in `plan.md` (row 4: `api.contoso.com`, `StateCode` 1; row 2: "gives no error"), R3 kept. These two text fixes are the only change after the second pass, so `plan status` shows its review as stale; no finding is open.

## Sign-off

- 2026-11-11: signed off by the building session (Opus) with `powerops plan sign-off`, on the person's standing instruction that sessions sign off their own milestone plans after clean reviews (2026-11-09, "Sessions sign off") and their instruction for this chain, "Start the build" (2026-11-11). Fable approved in round 2 and `powerops-review-plan` has no finding open.
