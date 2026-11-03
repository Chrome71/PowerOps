---
name: powerops-review-plan
description: Use to review an approved PowerOps plan in docs/plans/<name>/ against a Power Platform checklist, in a fresh context. Writes findings to review.md and never asks the user questions.
license: Apache-2.0
compatibility: Needs the powerops CLI on PATH. pac is optional.
---

# powerops-review-plan

Review an approved plan and write `review.md`. The input is the plan name. You work alone: do not ask the user anything. Anything unclear becomes a finding.

## 1. Read only the plan

Read only these:

- `docs/plans/<name>/plan.md`, `decisions.md` and `mockups/` if present
- the repo, for solutions, publishers and existing patterns
- the output of `powerops plan facts`, only if your request says the user confirmed, in that session, that `pac` is signed in to the plan's `environment:` (never run `pac` yourself)

Without that confirmation, do not run `powerops plan facts`: review from the repo and write in `review.md`, under the header lines, that environment facts were not read.

Do not read the planning chat. If it is in your context, say so in `fresh-context` below.

## 2. Check the status

1. Run `powerops plan status docs/plans/<name> --json`. If `powerops` is not found, say so and stop. If the plan is not found or the header is malformed, say so and stop.
2. If `status` is not `approved`, say "the plan is `<status>`, a review needs `approved`", and stop. Write nothing.
3. Note `bodyHash`, `openQuestions` and the plan's `review` value.

## 3. Review against the checklist

Check the plan for each item. Report a gap, a risk or an unanswered area as a finding.

1. Solution boundaries and publisher prefix: one clear solution per deployable unit, a named publisher and prefix, no overlap with solutions from `plan facts` (only if you read the facts).
2. Connection references and environment variables instead of hard-coded values (URLs, IDs, mailboxes, connection names).
3. Premium connector and licence impact: which connectors and tables need which licence, and who pays.
4. DLP fit: the connectors used must be allowed together. If nobody checked, say so.
5. Dev to Test to Prod route: where it is built, how it moves, who approves.
6. Managed or unmanaged: right for each environment.
7. Data volume and delegation limits: row counts, filters and sorts that may not delegate.
8. Security roles: who can read, write and administer, and whether the plan names roles.
9. Testability: can each step in `Steps` be checked, is there a way to test before Prod, and does each form have a way out (cancel, back, a failed save).
10. Unanswered areas and the open-question count: areas never asked (see the `## Log` in `decisions.md`), items under `Open questions` and `Questions for your admin`.
11. Size: for each table, list, flow and screen, ask which line of `Goal` would fail if it were removed. This item is about what the plan has and doesn't need; something missing, or a column that repeats another, belongs under the other items. Write one finding per problem, listing every artefact it covers.
    - No Goal line named, or none would fail: High.
    - Two tables, lists, flows or screens hold the same concept, or one shape is copied per year, team or site: High.
    - A SharePoint list fails a gate below with no stated reason: High.
    - A table or list not marked Reuse, Extend or Create, or a Create that doesn't name what it checked first: Medium.
    - A table or list holding only a name, code or status, with no attributes, owner, security or business editing of its own: Medium.
    - No `Won't build` list under `Goal`, and no "Nothing was cut.": Medium.
    - A flow over 50 actions or 4 levels of nesting with no stated reason: Medium.

    The SharePoint gates: under 5,000 rows at three years, or every view and query filters on an indexed column and returns at most 5,000 items, with the indexes added before 20,000 items; every app query delegates; few lookups (at most 12 joins in a query); small flow volume (the connector allows 600 calls per connection a minute); site or list permission is enough; no native offline; a real licence saving under Managed Environment licensing; an owner for each list's schema.

If the plan or `decisions.md` contains a URL, GUID, email or person's name, report it as a finding. Do not repeat the value.

## 4. Write review.md

Write `docs/plans/<name>/review.md`. The first three lines are exactly:

```
plan-hash: <bodyHash>
model: <the model you run on, as your agent reports it, else unknown>
fresh-context: yes|no
```

Write `fresh-context: yes` only if this review runs in a context that does not contain the planning conversation. Be honest. If unsure, write `no`.

Then:

```
## Findings

### High

#### R1. <short title>
- Area: <checklist item>
- Finding: <what is wrong or missing>
- Suggested fix: <what to change in the plan>
- Resolution: open

### Medium
...

### Low
...
```

- Number findings R1, R2, ... across the whole file, not per severity.
- Every finding has all four lines. Start each with `Resolution: open`.
- If a severity has no findings, write "No findings." under it.
- High means the plan should not be signed off as it stands (wrong boundary, missing route, unlicensed premium use, hard-coded tenant values). Medium means a gap the build would hit. Low means polish.
- Before writing, replace URLs, GUIDs, emails and people's names with placeholders such as `<url>`, `<guid>`, `<email>`, `<person>`. Reviews are committed.

## 5. Finish

1. In `plan.md`, set `review: done` in the header, one line only. Do not change the body: that would change the hash and make this review stale.
2. Reply with a short summary for the parent session: the count of High, Medium and Low findings, the open-question count, and the path `docs/plans/<name>/review.md`. Say that the next step is sign-off in `powerops-plan`.

## Per agent

- **Claude Code:** the installed copy of this skill runs in a forked context on the top model, so `fresh-context` is `yes`.
- **Codex and other agents:** a subagent with no chat history gives `fresh-context: yes`. A review run in the planning session gives `no`.
