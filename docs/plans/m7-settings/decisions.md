# Decisions: m7-settings

## Log

- plan written after the build, at the user's request relayed by the master planning session (2026-10-20); no question rounds were held, so the decisions below were made by the building agent and are open to change at sign-off
- scope: offline half only (`settings generate`, `settings validate`); the live half (`--apply`) gets its own plan
- decided in the build: `generate` writes `pac`'s deployment settings format with only the Learn fields and no `schemaVersion`, because the file is read by `pac` and Pipelines, not by powerops consumers; its golden is skipped in `OutputSchemaTests` with a one-line reason
- decided in the build: `emptyValue` fires only when the solution gives the environment variable no default value and no current value; the solution's `IsRequired` adds "(required)" to the message but does not decide whether it fires (open question in plan.md)
- decided in the build: names are matched case-insensitively and the first duplicate entry in the file wins
- approved without question rounds: users, data, automation, environments, constraints (after-the-fact plan, depth quick)
- approved (2026-10-20)
