---
status: done
review: done
environment: null
depth: quick
supersedes: null
signed-off: 2026-11-11
signed-off-hash: c06fe8dd8b57b527349b13420f611c1ae99302baad985edb424057e8d26d9f3d
---

# M87: PO007 also reads environment variable defaults held in a flow

## Goal

In trial 3 (record S37, P4; blind C-2), a FlowAgent `edit_flow` saved an environment variable's current value, a person's address, as the `defaultValue` of the flow parameter bound to that variable. `analyze` exited 0. PO006 skips literals under `definition.parameters` because PO007 owns them, and PO007 reads only the environment variable definitions (`solution.EnvironmentVariables`). This plan makes `FlowDefinition` keep each env-var-bound parameter's string default, and PO007 check it with its existing URL, GUID and email patterns. It fires unless that default is identical to the variable definition's own default in the solution, which the definition's finding already reports. The finding never holds any part of the value. This is milestone M87 of the fix plan after trial 3.

Done when:

1. `FlowDefinition.EnvironmentVariableDefaults` lists, for each parameter under `definition.parameters` that `EnvironmentVariableSchema` maps to a schema name and whose `defaultValue` is a JSON string, a `(SchemaName, Value)` pair, in key order. `$connections`, `$authentication`, an unbound parameter, a missing default and a non-string default give none.
2. PO007 fires on every fires case of Data row 4 and is quiet on every quiet case, with the message `Flow '<flow>' holds a default for environment variable '<schema>' that looks like <kind>.`, the workflow's file and the schema name as component, and the same id, severity, title and fix text. Its findings on definitions are unchanged.
3. No field of a flow finding (message, file, component), and no line of `analyze` output in `text`, `json`, `sarif` or `markdown`, holds the value or any part of it (Data row 4, the last two tests).
4. The `analyze-fixtures.*` goldens are updated by the recipe, and their diff holds only the five new PO007 findings of `coe-auditlogs` (Data row 6), their summary counts, and nothing else.
5. `docs/rules/PO007.md` says what it now finds and what isn't read; "Why it matters" and "How to fix" each have a paragraph for the flow case (Data row 5); its Sources line carries the date all its pages were reread.
6. For the paths `tests/eval skills/powerops-verify skills/powerops-review-security plugins/*/skills/powerops-verify plugins/*/skills/powerops-review-security`, both `git diff $(git merge-base HEAD origin/main) -- <paths>` and `git status --porcelain -- <paths>` are empty.
7. `dotnet test --project tests/PowerOps.Core.Tests --filter PO007` passes with the new cases, and `dotnet build` (0 warnings), `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m87-po007-flow-defaults` and the leak scan are clean.

### Won't build

- A new rule. The fix plan rejected it: the same check on the same kind of value stays one rule.
- Flagging any non-empty default. Numbers such as `30` and words such as `Days` are legitimate defaults (the fix plan rejected it).
- A non-string `defaultValue` (an object or array of a JSON-type variable, a number, a boolean). Not read; `PO007.md` says so. No export read so far holds an object or array default bound to a variable; bound number defaults exist (`alm-sample`, `coe-auditlogs`, `flowtests/capture`) and can't match a URL, GUID or address pattern.
- Redacting PO006 or the definition's own PO007 messages. That is M92 (Q2, "Hide by default"); this plan only makes the new finding print no value.
- Changing PO006. It keeps skipping `definition.parameters`, which PO007 now reads.
- A `schemaVersion` change: the finding has the existing shape.

## Users

- A maker or an agent who edits flows (with FlowAgent or in the designer) and runs `powerops analyze --fail-on warning` before an import. The re-trial check: a flow parameter whose default is an address, bound to a variable whose definition has none, fails it with PO007.
- A reviewer who reads the finding and clears the default in the flow's file, as `PO007.md`'s "How to fix" says, or accepts it with `powerops baseline` when the platform writes it back.

## Data

Line numbers are those of `b065f23`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | New record `EnvironmentVariableDefault(string SchemaName, string Value)` with a one-line summary, and the init property `FlowDefinition.EnvironmentVariableDefaults` (default `[]`), like `DataverseActions`, so no constructor call changes | `src/PowerOps.Core/Flows/FlowDefinition.cs` | `tests/PowerOps.Core.Tests/FlowDefinitionTests.cs` |
| 2 | In `Parse`'s loop over `definition.parameters` (`:104-111`), where a schema is found, a `defaultValue` that is a JSON string is added with that schema. The schema name and the value are kept as written; nothing is trimmed here | `FlowDefinition.cs` | `FlowDefinitionTests`: a bound string default by `metadata.schemaName` and by the label suffix; `$connections` with a default; an unbound parameter; a number default; an object default; no default: only the first two give entries |
| 3 | PO007 keeps its loop over `solution.EnvironmentVariables` and adds a loop over `RuleHelpers.CloudFlows(solution)`: for each entry, the trimmed value is classified by the existing patterns (one shared classify function, so both loops say the same kind); a kind fires unless the solution has a definition whose schema name equals the entry's ignoring case and whose trimmed default equals the trimmed value (ordinal). Message `Flow '<w.Name>' holds a default for environment variable '<schema>' that looks like <kind>.`, file `w.File`, component the schema name | `src/PowerOps.Core/Rules/PO007EnvironmentSpecificDefaultValue.cs` | row 4 |
| 4 | Tests, written first, with fake values only. Each fake value shares no part with the flow name, the flow id, the schema name or any path, and the GUID case uses a GUID other than the flow's id (`B.Flow`'s default id is in the file path). Fires: a parameter with `defaultValue` an address at `contoso.com` and `metadata.schemaName` `demo_Mail`, the definition's default empty; the same with the definition's default a different address; a URL default with no definition in the solution; a GUID default bound by the label suffix; an address default with leading and trailing spaces and no definition. Quiet: an empty default; a whitespace default; a default identical to the definition's (the definition's own finding fires once, the flow's not at all); the same with the definition's schema name in another case; a padded default equal to the definition's after trimming; `30` as a string and as a JSON number; a default on `$connections`; an unbound parameter with a URL default. Leak: for each fires case, the finding's message, file and component hold neither the value nor its local part, host or domain. A CLI test runs `analyze --fail-on warning` with each `--format` on a temp unpacked solution holding one such flow, whose names and paths share no part with the value: it exits 1, each output holds the PO007 finding (rule id, flow name, schema name), and neither stdout nor stderr holds the value or any part of it | `tests/PowerOps.Core.Tests/Rules/WarningRuleTests.cs`; `tests/PowerOps.Cli.Tests/AnalyzeCommandTests.cs` | `dotnet test --filter PO007` |
| 5 | `PO007.md`: "What it finds" adds the flow parameter default and when it is left to the definition's finding; it lists what isn't read (non-string defaults; parameters not bound to a variable); it says the finding names the flow and the variable, never the value. "Why it matters" adds, marked as from observed exports: the value sits in the exported flow file, so it reaches source control and every environment the solution is imported into; unmodified Microsoft exports hold such defaults too, often the definition's own. "How to fix" adds: the value is the `defaultValue` of the parameter bound to the named variable under `properties.definition.parameters` in the file the finding names; empty it there (or set a value valid everywhere) before packing; it is not verified whether a later save in the designer writes a value back, and if it does, accept the finding with `powerops baseline`. The rule's one-sentence `Fix` stays as it is (the fix plan pins it). The Sources line keeps its two pages, adds *Use environment variables in Power Automate solution cloud flows*, and gets `verified: 2026-11-11` only after all three pages are reread that day (else the troubleshooting page keeps the line's old date in the Log and the line's date is the oldest) | `docs/rules/PO007.md` | `RuleDocsTests` |
| 6 | Goldens by the recipe "Update goldens". The fixture `coe-auditlogs` (a public CoE Starter Kit sample, unmodified) has flow parameter defaults bound to `admin_TenantID` (a GUID), `admin_AdminMail` (an address) and `admin_PowerAutomateEnvironmentVariable` (a URL) in `DeprecatedAdminSyncAuditLogs`, and the last two in `DeprecatedChildAdminSyncLogs`; none of the three has a definition in the fixture. So five new PO007 warnings. The diff is read: it holds only those five, the warning counts and nothing else | `tests/golden/analyze-fixtures.{txt,md,json,sarif}` | `ReportTests` |

## Automation

None. The rule reads exported flow definitions offline, and this plan adds no flow.

## Screens

None.

## Environments and route

None. `analyze` stays offline and read-only, and nothing reads or writes a tenant.

## Solution and publisher

None. The tests build solutions in memory with fake `demo_` names; no new fixture.

## Constraints

- AGENTS.md: the smallest change; new logic with tests; no new write; Conventional Commits; no version or changelog edit; no tenant data in Git. Trial 3's names and values are not used anywhere.
- Fix plan section 4: the `analyze-fixtures.*` goldens are shared with M86 and M92. They are regenerated by the recipe after the final rebase under the chain's merge lock, never hand-merged, and the diff is read again then.
- The kept roles `powerops-verify` and `powerops-review-security`, their plugin copies and `tests/eval/` stay untouched (Done when 6). If an eval case would change, the build stops and reports.
- Behaviour change: `analyze --fail-on warning` now exits 1 on a solution whose flows hold such defaults, where it exited 0. That is the point of the milestone, and ordinary exports fire too, not only agent edits: the unmodified CoE sample `coe-auditlogs` gives five. `--baseline` with a file from `powerops baseline` accepts one; `PO007.md` and the goldens show it.
- Platforms are Windows and Linux. Nothing here depends on the platform.

## Assumptions

Sources checked on 2026-11-11 by `powerops-researcher` (Learn only; decisions, Log):

- The current value of an environment variable is used when set, else its default (*Environment variables for Power Platform overview*). That sentence is about the definition's default. Whether a default held in the flow is ever what the flow runs with is not verified, and the plan doesn't rely on it; PowerOps' own simulator doesn't use it.
- Flows cache a variable's value until they are saved or turned off and on again (*Use environment variables in Power Automate solution cloud flows*). Learn doesn't say that a value saved into the flow's definition travels with the export.
- Learn says definitions belong in the solution and values are given per target environment at deployment (the overview's FAQ). It doesn't say in one sentence that defaults must not be environment-specific, and one guidance page uses a development URL as a default. PO007's "Why it matters" stays a warning, not an error.
- Observed in exports, not on Learn: a flow's `definition.parameters` holds one entry per variable it uses, with `metadata.schemaName` and a `defaultValue` (the CoE Starter Kit flows in `tests/fixtures/coe-auditlogs`, `alm-sample`). Learn's Logic Apps *Workflow Definition Language schema* documents `parameters` with `type` and `defaultValue`, not `schemaName`.
- Observed in exports, not verified: the platform seems to write a value into that `defaultValue` when a flow is saved. Where a variable has a definition in the fixtures (`coe-auditlogs`, both `powerops-seed` versions), the flow holds that definition's exact default; in `alm-sample` it holds another value, with no current value exported. So clearing it in the file may not survive a later save in the designer, and `PO007.md` says so (row 5).
- The leak scan is `reports/leakscan.sh` in the main checkout (git-ignored, so not in this worktree), run from there as `bash reports/leakscan.sh origin/main claude/m87-po007-params-265ec4`.
- `docs/ecosystem/`: no row of `compatibility.md` covers environment variables or flow definitions; the open entries ECO-0009 and ECO-0012 touch neither this rule nor the flow JSON.

## Open questions

## Questions for your admin

## Steps

1. This plan and `decisions.md`; the Fable plan review and a fresh-context `powerops-review-plan` into `review.md`; fixes; sign-off by the building session on the person's instruction of 2026-11-11.
2. Row 1's record and the empty property first (so the test project builds), then row 4's tests and row 2's parse tests. Done: each new fires and parse test fails on its own assertion, the new quiet tests pass, and the 9 existing PO007 tests still pass.
3. Rows 2 and 3 until every test passes.
4. Rows 5 and 6, the goldens by the recipe, reading the diff, and the Fable work review.
5. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m87-po007-flow-defaults`, Done when 6, and the leak scan; a local commit; then the chain's merge protocol.
