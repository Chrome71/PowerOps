# Decisions: m7-settings

## Round 1

### Q1. R6: Should an empty value be reported for an optional environment variable with no default and no current value?
1. Yes, keep reporting it: a flow reading it gets no value (recommended)
2. Only for required variables
3. In your own words
Answer: Yes, keep reporting it: a flow reading it gets no value

### Q2. R3: What should happen when the settings file lists a name more than once?
1. Report it as its own problem kind, `duplicate` (recommended)
2. Keep it silent: the first entry wins
3. In your own words
Answer: Report it as its own problem kind, `duplicate`

### Q3. R2: Confirm the build choices: `emptyValue` ignores `IsRequired` (it only adds "(required)" to the message), names match regardless of case, `generate` writes no `schemaVersion`, and a settings file of the wrong shape exits 3?
1. Confirm all four (recommended)
2. Change some of them
3. In your own words
Answer: Confirm all four

## Round 2

### Q4. R4: No test runs on a managed export. How should that be handled?
1. Accept it as a known gap, noted in the plan (recommended)
2. Add a managed fixture and test now
3. In your own words
Answer: Accept it as a known gap, noted in the plan

## Log

- plan written after the build, at the user's request relayed by the master planning session (2026-10-20); no question rounds were held, so the decisions below were made by the building agent and are open to change at sign-off
- scope: offline half only (`settings generate`, `settings validate`); the live half (`--apply`) gets its own plan
- decided in the build: `generate` writes `pac`'s deployment settings format with only the Learn fields and no `schemaVersion`, because the file is read by `pac` and Pipelines, not by powerops consumers; its golden is skipped in `OutputSchemaTests` with a one-line reason
- decided in the build: `emptyValue` fires only when the solution gives the environment variable no default value and no current value; the solution's `IsRequired` adds "(required)" to the message but does not decide whether it fires (open question in plan.md)
- decided in the build: names are matched case-insensitively and the first duplicate entry in the file wins
- approved without question rounds: users, data, automation, environments, constraints (after-the-fact plan, depth quick)
- approved (2026-10-20)
- fresh review (Opus) 2026-10-20: R1 and R5 fixed in code and fixtures, plan body unchanged; R2, R3, R4 and R6 left open for the user at sign-off
- the user answered R2, R3, R4 and R6 in the master planning session (relayed 2026-10-20): Q1 to Q4 above; R3 built as the `duplicate` kind; open question removed; review now stale
