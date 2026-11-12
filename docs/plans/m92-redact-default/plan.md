---
status: approved
review: pending
environment: null
depth: standard
supersedes: null
---

# M92: Redaction by default in `analyze`, `diff`, their MCP tools and the Action

## Goal

Trial 3's blind grade rated it High (C4, blind C-5) that `analyze` (PO006, PO014) and `diff` print the URLs and environment variable default values they find unless `--redact` is given, and that the MCP `analyze` and `diff` tools have no way to hide them. The owner answered the fix plan's Q2 "Hide by default" (2026-11-11), which reverses their own D7 of 2026-11-04 (built as M61, opt-in `--redact`). This milestone makes redaction the default and keeps the baseline as the one stated exception.

Done when:

1. `powerops analyze` with no redaction option replaces each URL in every finding's message and component, every not-checked note and every stderr line with `[redacted URL]`, in all four formats and in the `--out` file, exactly as `--redact` does today (the Markdown writer escapes the placeholder as `\[redacted URL\]`, so tests look for `redacted URL`, as M61's do). `--redact` is still accepted and gives byte-for-byte the default output. `--show-values` gives today's unredacted output; with both options, `--show-values` wins.
2. `powerops diff` with no redaction option prints a changed `defaultValue` as `defaultValue added`, `removed` or `changed` and replaces URLs in keys, names, details, not-compared lines and stderr, as `--redact` does today; `--redact` equals the default; `--show-values` prints both values and the URLs; with both options, `--show-values` wins.
3. The MCP `analyze` and `diff` tools always redact (they call the commands without `--show-values`, and have no input to change that); their descriptions say so. The MCP `baseline` tool and `powerops baseline` keep the URLs, and the MCP `baseline` description says why.
4. `--baseline` drops the same findings by default as with `--show-values`, with a baseline file that holds the URL.
5. The Action's `redact` input defaults to `'true'`; `'false'` adds `--show-values` to every `analyze` run and to the PR diff; any other value, the empty string included, adds nothing, so it redacts.
6. `docs/COMMANDS.md` ("Values in outputs", `analyze` options, `diff` options, the usage block), `docs/GITHUB-ACTION.md` ("Public outputs", the example's commented input) and `action.yml`'s input description say the new default, the baseline exception, the one-time SARIF fingerprint change, and that two findings that differ only in their URL now share one fingerprint by default (so code scanning can show them as one alert). `docs/rules/PO006.md` and `PO014.md` say, in "What it finds", that `analyze` prints the URL as `[redacted URL]` unless `--show-values` is given, so two findings can read the same. `docs/BUILD-PLAN.md` gains an M92 section and one decision that names D7 as reversed, quotes the owner's answer, names the baseline as the exception and says that SARIF alerts of findings that held a URL close and reopen once.
7. `dotnet test --project tests/PowerOps.Cli.Tests --filter "AnalyzeCommandTests|DiffCommandTests|McpCommandTests|ActionScriptTests|ActionYmlTests|BaselineCommandTests|RoleEvalTests"` passes (exit 0; a solution-level filter exits 8 because the Core project matches nothing); the branch's own build, `dotnet src/PowerOps.Cli/bin/Debug/net10.0/powerops.dll analyze tests/fixtures/powerops-seed-yaml/v2` (the `powerops` on PATH is an older install) (the fixture whose PO006 finding holds a `contoso` SharePoint URL) prints `[redacted URL]` and not the host; `dotnet build` (0 warnings), `dotnet test`, `dotnet format --verify-no-changes`, `powerops.dll plan status docs/plans/m92-redact-default` (the branch build, 0 violations) and the leak scan are clean. The leak scan runs from the main checkout, where the git-ignored `reports/` folder is: `cd "<main checkout>" && bash reports/leakscan.sh origin/main <this branch>`, 0 hits and the control found.

### Won't build

- Redaction of `test` reports, of `baseline` and the MCP `baseline` tool, of `drift`, `deploy plan` or `deploy apply` (the last three print no values today). The baseline must match unredacted findings (M61 Q6).
- A `show_values` input on the MCP tools (Q2 (a) says they always redact).
- A change to `Redaction` (the pattern, the placeholder, the writer) or to `SolutionDiff.Compare`'s Core default (`redact = false`): `DeployReviews` compares unredacted internally, and the CLI decides.
- A usage error for `--redact --show-values` (Q1 below).
- Edits to `skills/`, `plugins/`, `.claude/agents/`, `tests/eval/`, the kept roles, the version files or the changelog. The one eval baseline line this change reaches (`tests/eval/roles/baseline/solution-08.txt`, `defaultValue: New → Triage`) stays as recorded; M96 regenerates it (Q7).

## Users

- A person or CI job that runs `powerops analyze` or `diff` and reads, uploads or posts the output.
- An agent that calls the MCP `analyze` or `diff` tool, whose output goes into a model's context.
- A workflow that uses the Action, with or without its `redact` input.
- A maintainer reading the release notes, who needs to know that code scanning alerts of URL findings reopen once.

## Data

Code is named by member; the fix plan's line numbers are from `b065f23`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | `analyze`: a new `Option<bool>("--show-values")` ("Print the URLs in findings, notes and stderr; by default each is replaced with a placeholder."). `--redact` stays, its description "The default: replace URLs with a placeholder. Kept so scripts that pass it keep working." `Execute` computes `redact = !showValues`; the rest of `Execute` and `Run` is unchanged | `src/PowerOps.Cli/AnalyzeCommand.cs` (`Create`, `Execute`) | row 6 (a) |
| 2 | `diff`: the same pair of options. `--show-values`: "Print a changed defaultValue and the URLs in the report and stderr; by default a changed defaultValue is printed as added, removed or changed and each URL is replaced with a placeholder." `--redact`: "The default: print a changed defaultValue only as added, removed or changed, and replace URLs with a placeholder. Kept so scripts that pass it keep working." `redact = !showValues` is passed on as today | `src/PowerOps.Cli/DiffCommand.cs` (`Create`, `Execute`) | row 6 (b) |
| 3 | MCP: no change to the calls (they pass no `--show-values`, so they redact). The `analyze` tool description gains: "URLs in findings and notes are replaced with [redacted URL], as the command does by default." The `diff` tool description gains: "A changed defaultValue is printed only as added, removed or changed, and URLs are replaced with [redacted URL], as the command does by default." The `baseline` description gains: "Its findings keep their URLs, because a baseline must match the unredacted findings." | `src/PowerOps.Cli/McpCommand.cs` (the `analyze`, `baseline`, `diff` entries) | row 6 (c); golden `mcp-tools.txt` |
| 4 | Action: `action.yml` input `redact` gets `default: 'true'` and the description "'false' adds `--show-values` to every `analyze` run and to the PR diff, so URLs in findings and environment variable defaults are shown in the job summary, annotations, code scanning, the job log and the comment. Any other value hides them. Default 'true'." `action/analyze.sh`: `if [ "$PO_REDACT" = "false" ]; then args+=(--show-values); fi` in place of the `--redact` line, and its header comment. `action/diff.sh`: the same with `${PO_REDACT:-}` | `action.yml`, `action/analyze.sh`, `action/diff.sh` | row 6 (d) |
| 5 | `Redaction`'s summary comment: "Opt-in" becomes the default of `analyze` and `diff` since M92, `--show-values` turns it off | `src/PowerOps.Core/Output/Redaction.cs` (comment only) | none |
| 6 | Tests, written first and seen failing before rows 1 to 4. (a) `AnalyzeCommandTests`: the M61 theory becomes, per format: the default output holds no fixture URL host and holds `redacted URL` (no brackets: Markdown escapes them); `--redact` output equals the default; the `--out` file equals the default; `--show-values` holds the fixture URL; `--redact --show-values` equals `--show-values`; SARIF keeps `helpUri`. Stderr: `analyze <a URL-shaped missing path>` with no option prints `path not found: [redacted URL]`, and with `--show-values` prints the URL. Baseline: a baseline written by `powerops baseline` holds the URL, and `analyze --baseline` with no option and with `--show-values` drop the same findings. Any other test that asserts a URL in default `analyze` output gains `--show-values`. (b) `DiffCommandTests`: the M61 theory becomes: the default prints `defaultValue changed` and not the sentinel, equal to `--redact` and to the `--out` file; `--show-values` holds both values; `--redact --show-values` equals `--show-values`; stderr as in (a). (c) `McpCommandTests`: the `analyze` tool over the URL fixture returns no fixture URL host and holds `[redacted URL]` (JSON keeps the brackets); the `diff` tool over the sentinel pair returns no sentinel; the `baseline` tool's text still holds the URL. (d) `ActionScriptTests`: `analyze.sh` with `PO_REDACT` `false` passes `--show-values`; with `true`, empty and `yes` passes neither `--show-values` nor `--redact`; `diff.sh` likewise. `ActionYmlTests`: the input's default is `true`, and the scripts hold the new `--show-values` lines. (e) `RoleEvalTests`: no new test; the existing `Every_solution_case_has_a_baseline_file_that_matches_the_live_output` fails after rows 1 and 2 (seen once) and passes after row 6a | `tests/PowerOps.Cli.Tests/AnalyzeCommandTests.cs`, `DiffCommandTests.cs`, `McpCommandTests.cs`, `ActionScriptTests.cs`, `ActionYmlTests.cs`, `BaselineCommandTests.cs` if needed | itself |
| 6a | Bridge for the frozen eval baseline: in `LiveBaseline`, the `diff` command of `BaselineCommands` runs with `--show-values` added, while the recorded `$ powerops diff solution/previous solution/current` header stays as the args list prints it. A comment: `shortcut: tests/eval/roles/baseline/*.txt were recorded before M92 hid values by default; M96 regenerates them and drops --show-values here`. Nothing under `tests/eval/` changes | `tests/PowerOps.Cli.Tests/RoleEvalTests.cs` (`LiveBaseline`) | row 6 (e) |
| 7 | `docs/COMMANDS.md`. Usage block (`powerops analyze ... --redact` line): becomes `--show-values` with "the same findings, with the URLs in them and on stderr printed, not replaced by [redacted URL]". "Values in outputs": PO006 and PO014 find URLs; `analyze` and `diff` replace them (and `diff` the `defaultValue` text) by default, and `--show-values` prints them; the MCP `analyze` and `diff` tools always replace them; the baseline file and the MCP `baseline` tool keep them, because a baseline must match the unredacted findings, so treat a committed baseline file as holding those URLs. The `analyze` option `--redact` becomes `--show-values` with the same scope text reversed, plus "`--redact` is accepted and changes nothing (the default since M92)" and the fingerprint sentences: a fingerprint is taken over the text printed, so the first upload after M92 closes the code scanning alerts of findings that held a URL and opens new ones, once; and by default two findings that differ only in their URL read the same and share one fingerprint, so code scanning can show them as one alert (`--show-values` keeps them apart). The `diff` option likewise. The headings "Values in outputs" and "analyze and baseline" stay (link targets), and no line passes 1,500 characters | `docs/COMMANDS.md` | `DocsDriftTests` |
| 7a | `docs/rules/PO006.md` and `PO014.md`, "What it finds": one sentence after "Each distinct URL ... is one finding": `analyze` prints the URL as `[redacted URL]` unless it is run with `--show-values`, so two findings can read the same; run it with `--show-values` on committed files to see which URL each one is | `docs/rules/PO006.md`, `docs/rules/PO014.md` | `RuleDocsTests` |
| 8 | `docs/GITHUB-ACTION.md`: the example's commented input becomes `# redact: 'false'   # show URLs in findings and the diff; see "Public outputs"`; "Public outputs" (heading kept): the outputs hide URLs and changed defaults by default; `redact: 'false'` shows them, for committed files only; the fingerprint sentences (alerts close and reopen once when the default changes and each time the input is switched, and by default two findings that differ only in their URL can show as one alert); the third bullet keeps its content with `redact` | `docs/GITHUB-ACTION.md` | `ActionYmlTests`, `DocsDriftTests` |
| 9 | `docs/BUILD-PLAN.md`: an `## M92` section in M88's form (one paragraph, one table row, a build-order line) and one decision, numbered at merge under the merge lock: "**Outputs hide URLs and default values by default; the baseline keeps them.**" with D7 named as reversed, the owner's answer quoted, the baseline exception and its reason, the one-time SARIF fingerprint change for findings that held a URL and the shared fingerprint of findings that differ only in their URL, and the owner's Q7 answer (the Verifier joins M96's rerun, cap USD 40) | `docs/BUILD-PLAN.md` | none (prose) |

Goldens. `tests/golden/analyze-fixtures.*` come from `ReportTests` in Core over unredacted results, so they do not change; the `diff-seed.*` and `diff-agents.*` goldens hold no `defaultValue` change and no URL (grep 2026-11-11), so they do not change; `mcp-tools.txt` changes by the row 3 sentences. The Fable plan review flipped the default in a scratch copy and saw only the M61 `Redact_*` tests and `RoleEvalTests`' baseline match fail (5,109 passed before). The "Update goldens" recipe runs and its diff is read: only `mcp-tools.txt` may change. The docs' Action snippets pin a commit that predates the new default until the usual pins pull request moves them.

## Automation

None.

## Screens

None.

## Environments and route

None. No tenant call; every test runs on fixtures and stubs.

## Solution and publisher

None.

## Constraints

- AGENTS.md: the smallest change that fully solves the task; tests first; `dotnet format --verify-no-changes` clean; fake values only (the existing `contoso` fixture and the M61 sentinel host).
- Reuse `Redaction`, the `redact` flag of `Execute` and `SolutionDiff.Compare`, and M61's tests. No new type, no new placeholder, no schema change: the JSON and SARIF shapes stay, so `schemaVersion` stays.
- Output changes, each pinned by row 6, and no other: the default `analyze` and `diff` output (and so the MCP tools' and the Action's) equals what `--redact` printed before; `--show-values` equals what the default printed before.
- Commit type: `feat!:` for rows 1 to 6 with a `BREAKING CHANGE:` footer (a default output changes), `docs:` for rows 7 to 9 (7a included).
- One writer: the executor writes source, tests, `action*`, `docs/COMMANDS.md`, `docs/GITHUB-ACTION.md` and the two rule pages; the orchestrating session writes this folder and `docs/BUILD-PLAN.md`.
- Chain limits (`reports/trial-3/chain/chain-rules.md`): no tenant call, no `--apply`, nothing under the trial's evidence folder, no version or changelog edit, the frozen roles untouched; push and merge only under the merge lock.

## Assumptions

- No Microsoft tool, API or skill is described, so the ecosystem rule does not apply and no `powerops-researcher` check is needed. The code scanning behaviour (alerts keyed by fingerprint close and reopen when it changes) is M61's existing statement in `docs/COMMANDS.md`, unchanged in substance.
- A GitHub Action input that a workflow does not set takes its `default` (M61 used the same mechanism with `'false'`).
- Both kept roles run `analyze` and `diff` on the eval cases `solution-01` to `solution-08`. This change reaches one recorded line: `tests/eval/roles/baseline/solution-08.txt`, where `diff` printed `defaultValue: New → Triage` and now prints `defaultValue changed` (no other baseline file holds a URL or a `defaultValue` change, grep 2026-11-11; no `key.json` flaw is about a default value; solution-08's two flaws are the security pass's). Decision 40 says a change that reaches a baseline needs a rerun: the security pass reruns in M96 (Q4), and the owner added the Verifier pass to M96 with the cap raised to USD 40 (Q7, 2026-11-11). Until then row 6a keeps `RoleEvalTests` comparing the recorded line with `--show-values` output. Their skill text is not touched here.
- The Action runs the CLI built from its own commit (`action.yml` builds from source), so the script's `--show-values` and the CLI always come from one commit.

## Open questions

## Questions for your admin

## Steps

1. **Plan.** This plan and `decisions.md`, the Fable plan review, a fresh-context `powerops-review-plan` into `review.md`, fixes, and the sign-off. Files: `docs/plans/m92-redact-default/`. Done when: Fable approved, the review has no open finding, and `powerops plan sign-off` ran. Check: `dotnet src/PowerOps.Cli/bin/Debug/net10.0/powerops.dll plan status docs/plans/m92-redact-default`.
2. **Code and tests, tests first.** Rows 1 to 6. Files: `src/PowerOps.Cli/AnalyzeCommand.cs`, `DiffCommand.cs`, `McpCommand.cs`, `src/PowerOps.Core/Output/Redaction.cs`, `action.yml`, `action/analyze.sh`, `action/diff.sh`, the tests of row 6, `tests/PowerOps.Cli.Tests/RoleEvalTests.cs` (row 6a), `tests/golden/mcp-tools.txt`. Depends on: 1. Done when: the row 6 tests were seen failing before rows 1 to 4 (project-scoped runs, with counts), then the filter of Done when 7 and the whole `dotnet test` pass. Check: the failing and passing runs in the executor's report.
3. **Docs.** Rows 7, 7a and 8. Files: `docs/COMMANDS.md`, `docs/GITHUB-ACTION.md`, `docs/rules/PO006.md`, `docs/rules/PO014.md`. Depends on: 2. Done when: no text in either page still says redaction is opt-in or that the MCP tools return unredacted text; the baseline exception and the fingerprint change are stated; `DocsDriftTests`, `RuleDocsTests` and `ActionYmlTests` pass. Check: `grep -n -- "--redact\|show-values\|redact" docs/COMMANDS.md docs/GITHUB-ACTION.md` and the diff.
4. **Checks and record.** Row 9 at merge; the checks of Done when 7; the "Update goldens" recipe and its diff; the Fable work review; the `decisions.md` Log; `status: done` by hand. Files: `docs/BUILD-PLAN.md`, this folder. Depends on: 2, 3. Done when: every check is clean and the Fable work review passed. Check: the Log lines.
