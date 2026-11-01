---
name: powerops-verify
description: Use to verify a built Power Platform solution against its signed-off PowerOps plan in docs/plans/<name>/ with the offline powerops checks. Writes verify.md. Works alone and never asks the user questions.
license: Apache-2.0
compatibility: Needs the powerops CLI on PATH. pac is optional.
---

# powerops-verify

Verify a built solution against its plan and write `verify.md`. You work alone: do not ask the user anything. Record anything unclear as a finding or as not verified.

## The request

The request gives:

- the plan (`docs/plans/<name>/`)
- the current solution and the previous solution
- the settings file
- the test folder
- the Lead's evidence for live checks, which is optional

Use those as given. Never invent an input. A required input the request leaves out is a failed check. An input that does not apply is recorded as not applicable.

The report is `docs/plans/<name>/verify.md`. If the request names no plan, there is no report path: reply with `state: failed` and the missing input, and write nothing.

## Commands you may run

This list is instruction, not enforcement. Run nothing else. Never fire a flow or write to a tenant.

- `powerops plan status docs/plans/<name> --json`: gives `status` and `bodyHash`.
- `powerops analyze <current solution>`
- `powerops test <current solution> --tests <test folder>`
- `powerops diff <previous solution> <current solution>`
- `powerops settings validate <current solution> --settings <settings file>`

## Check the status

1. Run the `plan status` command above. Note `status` and `bodyHash`. If there is no `bodyHash`, write `plan-hash: unknown`.
2. The plan needs status `signed-off`, `implementing` or `done`. A wrong status gives `state: failed`: write the report with the header below, name the status under the header lines, and stop.

## Verify

1. Run `analyze`, `test`, `diff` and `settings validate` with the paths from the request.
2. Compare the result with the plan's `Steps`, `Data`, `Automation` and `Screens`. Anything built outside the plan is a finding.
3. Live checks use the Lead's evidence. With none, record them as not verified. That does not make the state `failed`.
4. If `<zip>.findings.json` lies beside the exported zip, cite it as evidence. It is the output of `powercat-overflow` or `powercat-overpage` from microsoft/power-cat-skills.
5. Run nothing from power-cat-skills: its skills ask questions and upload to a hosted viewer.

## Write the report

The first four lines of `verify.md` are exactly:

```
plan-hash: <bodyHash>
model: <the model you run on, as your agent reports it, else unknown>
fresh-context: yes|no
state: ready|blocked|failed|dropped
```

These are the first four lines of the report. Write `fresh-context: yes` only if this runs in a context that does not contain the planning conversation. Be honest. If unsure, write `no`.

| State | Meaning |
|---|---|
| `ready` | The role finished and no High finding is open |
| `blocked` | A High finding is open, or a question only a person can answer |
| `failed` | A required check could not run, and the report says which |
| `dropped` | Nothing for this role to do, and the report says why |

Choose `blocked` before `failed` before `ready`. An optional check that can't run isn't a failure.

Under the header lines:

- A line `input: <solution path or zip file name>`. Nothing parses it.
- A `## Checks` section with one line per check, in this order: `analyze`, `test`, `diff`, `settings validate`, the comparison with the plan, the live checks. Each line is `- <check>: <outcome>`, where the outcome is `done`, `failed` (say which check could not run and why), `not applicable` (say why) or `not verified` (say why).
- The findings:

```
## Findings

### High

#### V1. <short title>
- Area: <the check or the plan section>
- Finding: <what is wrong or missing>
- Suggested fix: <what to change>
- Resolution: open

### Medium
...

### Low
...
```

- Number findings V1, V2, ... across the whole file, not per severity.
- Every finding has all four lines. Start each with `Resolution: open`.
- If a severity has no findings, write "No findings." under it.
- High means the solution should not go on as it stands. Medium means a gap that will cause trouble later. Low means polish.
- Before writing, replace URLs, GUIDs, emails and people's names with placeholders such as `<url>`, `<guid>`, `<email>`, `<person>`. Reports are committed.
- Write only this report. Never open `plan.md` for writing.

## Finish

Reply with a short summary for the parent session: the count of High, Medium and Low findings, the state and the report's path.
