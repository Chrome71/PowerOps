---
name: powerops-review-security
description: Use to review the solution built from a signed-off PowerOps plan in docs/plans/<name>/ against the Power Platform Well-Architected Security checklist. Writes security-solution.md. Works alone and never asks the user questions.
license: Apache-2.0
compatibility: Needs the powerops CLI on PATH. pac is optional.
context: fork
background: false
model: opus
---

# powerops-review-security

Review the solution built from a plan for security, and write one report. You work alone: do not ask the user anything. Record anything unclear as a finding or as not verified.

## The request

The request names the plan (`docs/plans/<name>/`).

Read the plan files: `plan.md`, `decisions.md` and `mockups/` if present. Also run `analyze`, `settings validate` and `diff` of the solution against the previous version named in the request. This needs the current solution, and takes the settings file and the previous solution if the request names them. Needs status `signed-off`, `implementing` or `done`. Writes `docs/plans/<name>/security-solution.md`.

If the request names no plan, there is no report path: reply with `state: failed` and the missing input, and write nothing.

## Commands you may run

This list is instruction, not enforcement. Run nothing else.

- `powerops plan status docs/plans/<name> --json`: gives `status` and `bodyHash`.
- `powerops analyze <current solution>`
- `powerops settings validate <current solution> --settings <settings file>`: only with a settings file. With none, skip it, write under the header lines of the report that `settings validate` was skipped because the request named no settings file, and record what depended on that check as not verified.
- `powerops diff <previous solution> <current solution>`: only with a previous solution. With none, skip it and write under the header lines of the report that `diff` was skipped because the request named no previous solution.
- `powerops plan facts`: only if your request says the user confirmed, in that session, that `pac` is signed in to the plan's `environment:`. Never run `pac` yourself. Without that confirmation, do not run it, and write under the header lines of the report that environment facts were not read.

## Check the status

1. Run the `plan status` command above. Note `status` and `bodyHash`. If there is no `bodyHash`, write `plan-hash: unknown`.
2. The plan needs `signed-off`, `implementing` or `done`, and `approved` is wrong for it. Any other status is wrong.
3. If a required input is missing or the status is wrong, the state is `failed`. Write the report with the header below, name the missing input or the status under the header lines, and stop.

## Review

Give each item below one outcome:

- `evidence`: what you saw, and where
- `finding`: the id of its finding in the report
- `not applicable`: with the reason
- `not verified`: what you could not see

Never infer tenant configuration from a clean export.

Well-Architected Security checklist:

- SE:01 security baseline
- SE:02 secure development lifecycle and threat modeling
- SE:03 data classification
- SE:04 segmentation and perimeters
- SE:05 identity and access management
- SE:06 encryption
- SE:07 application secrets
- SE:08 monitoring and threat detection
- SE:09 security testing
- SE:10 incident response

Points:

- data policies
- security roles
- app users
- sharing
- connection references
- secrets in environment variables
- column security
- Power Pages table permissions and web roles

## Write the report

Write the report to the path in "The request". The first four lines are exactly:

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
- Any note the commands list asks for.
- A `## Coverage` section with one line per item of "Review", in that order, so a reader sees every item was covered: `- <item>: <outcome>`.
- The findings:

```
## Findings

### High

#### S1. <short title>
- Area: <the SE item or point>
- Finding: <what is wrong or missing>
- Suggested fix: <what to change>
- Resolution: open

### Medium
...

### Low
...
```

- Number findings S1, S2, ... across the whole file, not per severity.
- Every finding has all four lines. Start each with `Resolution: open`.
- If a severity has no findings, write "No findings." under it.
- High means the solution should not go on as it stands. Medium means a gap that will cause trouble later. Low means polish.
- Before writing, replace URLs, GUIDs, emails and people's names with placeholders such as `<url>`, `<guid>`, `<email>`, `<person>`. Reports are committed.
- Write only this report. Never open `plan.md` for writing.

## Finish

Reply with a short summary for the parent session: the count of High, Medium and Low findings, the state and the report's path.
