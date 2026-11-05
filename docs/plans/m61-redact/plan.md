---
status: approved
review: done
signed-off: null
signed-off-hash: null
environment: null
depth: quick
supersedes: null
---

# M61: opt-in `--redact`, a public-output policy, and storage guidance

## Goal

Close three findings of the live test of 2026-11-04 (its record is kept outside the repo because it quotes a private test run). This is M61 of the improvement plan written after that test, with decision D7 answered "opt-in everywhere": `--redact` is opt-in on every output, SARIF and the Action's job summary included, and the public-output policy is written guidance. No default changes.

- CX-4: PO006 and PO014 put the URL they found into the finding's message and component, and `diff` prints an environment variable's `defaultValue` before and after. Those texts reach the job summary, the annotations, the job log, code scanning and the PR comment. There is no way to leave them out.
- C-8 (the confidentiality half, F2 of the run): the Playwright example's README gives no file mode for the saved sign-in `.auth/state.json`, and no Windows equivalent.
- A-8: the run's scratch repo committed a canvas `.msapp` and both solution zips, which hold the environment URL. HANDOFF step 3 warns about the unpacked folder only.

The milestone splits into four items: (a) `--redact` on `analyze` and `diff`, plus the policy; (b) `docs/COMMANDS.md` says which outputs quote values; (c) the Playwright README's file permissions; (d) storage guidance in HANDOFF step 3.

Done when:

1. `powerops analyze <path> --redact` replaces every URL (a scheme such as `https` followed by `://`) in each finding's message and component, and in each not-checked note, with `[redacted URL]`, in the `text`, `json`, `sarif` and `markdown` formats, on stdout and through `--out`. Help links stay: the JSON `helpUri`, SARIF's `helpUri`, `informationUri` and `$schema`, and the rule links of the Markdown are not touched. Every line `analyze` writes to stderr with `--redact` has its URLs replaced the same way, except a usage error that the option parser writes before the command runs, which quotes only the command line. A baseline is matched against the findings before they are redacted, so `--baseline` keeps working with `--redact`.
2. `powerops diff <old> <new> --redact` prints a changed `defaultValue` as `defaultValue added`, `defaultValue removed` or `defaultValue changed`, the wording current values already get, and replaces URLs in each change's key, name and detail, in each not-compared line and in every stderr line (with the same parser exception), in the `text`, `json` and `markdown` formats.
3. Without `--redact` the output of both commands is byte for byte what it is at `0fb8f3d`: every golden, and the eval baselines in `tests/eval/roles/baseline/`, stay as they are.
4. The Action gains an input `redact`, default `'false'`. `'true'` adds `--redact` to each of its `analyze` runs (job summary, annotations, SARIF, the apply gate and the final fail-on run) and to the PR diff of `action/diff.sh`. Nothing else in the Action changes.
5. `docs/COMMANDS.md` documents `--redact` for `analyze` and `diff` and has a short "Values in outputs" section that says which outputs quote values. `docs/GITHUB-ACTION.md` documents the `redact` input and holds the public-output policy as guidance.
6. `docs/examples/playwright/README.md` tells the reader to make `.auth` private before saving the sign-in and the file after it: `chmod 700 .auth` and `chmod 600 .auth/state.json` on Linux, and on Windows one `icacls` command that removes the inherited entries of `.auth` and grants only the reader's account, inherited by the files in it. It says how to check both. This part is docs only; M63 checks the Windows command by hand.
7. `docs/HANDOFF.md` step 3 says, for the files that step makes, where they may be kept: the unpacked folder's `.msapp`, the two exported zips (which hold the same `.msapp`) and, by pointing at step 6, the settings file. It names the exact files to keep out of a public repository and does not suggest a blanket ignore.
8. `tests/PowerOps.Core.Tests/Output/OutputHygieneTests.cs` plants a sentinel URL and a sentinel default value, and checks that with redaction on neither appears in the messages, components, values or stderr of any writer (`TextReport`, `JsonReport`, `SarifReport`, `MarkdownReport`, `DiffReport` text, JSON and Markdown, and the stderr note writers), and that the help links are still there. Each test also checks that without redaction the sentinel does appear, so it is planted where an output would show it.
9. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan are clean, and the PR is green on all eight checks.

### Won't build

- Redaction by default, for SARIF and the Action's summary or anywhere else (D7).
- Automatic `.gitignore` edits, or blanket `*.zip` or `*.msapp` ignores (the improvement plan rejected both).
- `--redact` on `baseline`, `test`, `drift`, `deploy`, `settings` or the MCP tools (decisions Q6).
- Redaction of names, paths, versions, GUIDs or email addresses (decisions Q1).
- A note for a canvas app whose `.msapp` the solution lacks (the coverage half of C-8; not in M61).
- A `docs/BUILD-PLAN.md` section or numbered decision.

## Users

- A maker or reviewer who runs `analyze` or `diff` on an export that is not public, and posts or shares the report: a pull request, an issue, a chat. An agent that calls the MCP tools gets unredacted JSON (decisions Q6); one that runs the CLI can pass `--redact`.
- A team that runs the Action in a public repository on files the job builds or downloads, not only on committed ones.
- A person who saves a Playwright sign-in on a shared or backed-up machine.

## Data

Line numbers are those of `0fb8f3d`.

| # | Severity | Change | Where | Test |
|---|---|---|---|---|
| 1 | high | A new `public static partial class Redaction` in Core. `Placeholder = "[redacted URL]"`. `Text(string)` replaces each match of `[a-z][a-z0-9+.-]*://[^\s'"<>]*` (ignoring case, after a word boundary, with `RegexOptions.NonBacktracking` as PO007 uses, because the backtracking engine is quadratic on a long run of letters with no `://`) with the placeholder. Punctuation right after a URL (`.`, `,`, `)`) goes into the placeholder with it. `Apply(AnalysisResult)` returns a copy whose findings have `Message` and `Component` run through `Text`, and whose `NotChecked` lines are run through `Text`; the solution's path, unique name, version, publisher and `Requires` stay. `Redaction.Writer` is a `TextWriter` over another one. It overrides `Write(string)`, `Write(char[], int, int)` and `Write(ReadOnlySpan<char>)` to redact each chunk before passing it on, passes a single `Write(char)` on as it is, reports the inner writer's `Encoding`, copies its `NewLine`, flushes it on `Flush`, and never disposes it. `analyze` and `diff` write each stderr line with one `WriteLine(string)`, which reaches the override as one `Write(string)`; a URL split over several writes would not be caught, and no code writes one that way | `src/PowerOps.Core/Output/Redaction.cs` (new) | `OutputHygieneTests`: `Text` on a URL in quotes, a URL with a path and a query, two URLs in one string, an `http` and a `file` URL, and text with no URL (unchanged); the writer redacts `WriteLine` and `Write` of a string and of a character array, and keeps the inner writer's line ending; a string of 100,000 letters with no `://` comes back unchanged and fast |
| 2 | high | `SolutionDiff.Compare(Solution old, Solution @new, bool redact = false)`. With `redact`, the environment variable block reports `defaultValue` as `defaultValue added`, `removed` or `changed` instead of the `defaultValue: <old> → <new>` part of `Differences`; the current-value expression becomes one helper that both use. At the end, with `redact`, each change's `Key`, `Name` and `Detail` and each `NotCompared` line go through `Redaction.Text`. Without `redact` nothing changes | `src/PowerOps.Core/Diff/SolutionDiff.cs:52,72-80` | `SolutionDiffTests`: a default value added, removed and changed gives the three words with `redact` and the values without; the other fields of the block (`type`, `required`, `currentValue`) read as before |
| 3 | high | `analyze --redact`: a `bool` option "Replace URLs in findings, notes and stderr with a placeholder." The error writer handed to `Run` is a `Redaction.Writer` when the flag is set, so every stderr line of the command is redacted (not-checked notes, skipped links, errors, the debug stack trace). The `emit` step applies `Redaction.Apply` to the result it is given, after `Run` has applied the baseline, and before it picks the format. `BaselineCommand` passes no flag and is unchanged | `src/PowerOps.Cli/AnalyzeCommand.cs:16-60` | `AnalyzeCommandTests`: on a fixture with PO006 and PO014 findings, each format with `--redact` has no `contoso.sharepoint.com` on stdout, in the `--out` file or on stderr, and has the placeholder; the JSON still has `helpUri`; without `--redact` the URL is there; with `--baseline` made from the same folder the findings are still dropped |
| 4 | high | `diff --redact`: the same option text. The error writer is a `Redaction.Writer` when it is set, and `Compare` gets `redact`. The `SolutionDiscovery.Find` call keeps its `InvalidDataException` catch and gains a `catch (Exception)` after it that writes what `Program.Run`'s handler writes today (the stack trace with `POWEROPS_DEBUG=1`, else `error: <message>`) and returns `3`, so an exception there goes through the wrapped writer and the output without `--redact` is unchanged | `src/PowerOps.Cli/DiffCommand.cs:13-20,69` | `DiffCommandTests`: two temp folders whose environment variable's default changes from one sentinel to another give `defaultValue changed` and neither sentinel in `text`, `json` and `markdown`, nor on stderr; without the flag both values are printed |
| 5 | medium | SARIF does not change. A finding's `poFinding/v1` fingerprint is the hash of `uniqueName|ruleId|component|message` as the writer gets them, so with `--redact` it is taken over the redacted text and holds no hash of a URL. Two findings that differ only in a URL then share one fingerprint. Unredacted output already has such pairs (`tests/golden/analyze-fixtures.sarif` holds four values more than once), so this is the behaviour SARIF users already get (decisions Round 2, R1) | `src/PowerOps.Core/Output/SarifReport.cs:37-46` (unchanged) | the acceptance test of row 11 runs `SarifReport` over redacted findings; `git diff tests/golden` is empty |
| 6 | medium | Action input `redact`, description "'true' adds `--redact` to every `analyze` run and to the PR diff, so URLs in findings and environment variable defaults are replaced with placeholders in the job summary, annotations, code scanning, the job log and the comment. Default 'false'." Each of the five steps that run `analyze` gets `PO_REDACT: ${{ inputs.redact }}` in its `env` and `if [ "$PO_REDACT" = "true" ]; then args+=(--redact); fi` after its baseline line. The `Diff` step gets the same variable, and `action/diff.sh` reads it as `${PO_REDACT:-}` (it runs under `set -u`, and its tests and any other caller may leave it unset) and adds `--redact` to its `diff` call only when it is `true`, leaving the call otherwise as it is; its header comment lists `PO_REDACT` among its inputs | `action.yml:5-38,64-100,144-155,171-178,190-200`, `action/diff.sh:3,30` | `ActionYmlTests`: every step whose `run` calls `analyze` or `diff.sh` has `PO_REDACT` set to `${{ inputs.redact }}`, and every `run` that calls `analyze` adds `--redact` when it is `true`; `ActionScriptTests`: `diff.sh` with `PO_REDACT=true` records a call that ends in `--redact`, and with it unset records today's call; `DocsDriftTests.The_action_inputs_match_the_docs` passes |
| 7 | medium | `docs/COMMANDS.md`: the `analyze` options gain `--redact` (what it replaces, that help links stay, that the baseline is matched first, and that fingerprints of a redacted SARIF differ from an unredacted one's, so switching it on or off once makes code scanning close the old alerts and open new ones); the `diff` options gain `--redact`; the usage block gains one `analyze --redact` line. The stderr claim excludes a usage error of the option parser, which quotes only the command line. A new section "Values in outputs" after "Writing files" (b): PO006 and PO014 quote the URL in message and component in every `analyze` format and in the `baseline` file; other findings quote names and paths and no finding quotes a variable's default or current value; `diff` prints `defaultValue` before and after (cut to 100 characters) and never current values, agent instructions or flow definitions; `test` reports quote values from the solution and the test files; `drift`, `deploy plan` and `deploy apply` print no setting values, as their sections say; `--redact` covers `analyze` and `diff` only, and the MCP `analyze` and `diff` tools return the same unredacted text as the CLI without it; and two redacted findings can read the same, because each was a different URL | `docs/COMMANDS.md:5-31,32-34,38,50` | `DocsDriftTests` |
| 8 | medium | `docs/GITHUB-ACTION.md`: the `redact` input beside `baseline` in the first example (commented out) and one paragraph "Public outputs", the policy (guidance, not enforced): in a public repository, treat the job summary, annotations, job log, code scanning results and the PR comment as public; run the Action on committed files where you can; when it analyzes or diffs files that are not public (an export the job downloads, a folder built in the job), set `redact: 'true'`; the deploy plan and the flow test report are not redacted, the plan prints no setting values, and the flow tests should run on committed files only, as their section says | `docs/GITHUB-ACTION.md:7-21` and a new paragraph after `:24` | `DocsDriftTests` |
| 9 | medium | (c) `docs/examples/playwright/README.md`: before the `npx playwright open` command, make the folder private, `mkdir -p .auth && chmod 700 .auth` on Linux, and in PowerShell `New-Item -ItemType Directory -Force .auth` then `icacls .auth /inheritance:r /grant:r "$(whoami):(OI)(CI)F"`; after step 4, `chmod 600 .auth/state.json` on Linux (the Windows file inherits the folder's entry). Check with `ls -ld .auth .auth/state.json` (`drwx------` and `-rw-------`) or `icacls .auth\state.json`, which should list only your account. One sentence says that afterwards only your account can open the folder (backup or antivirus software that runs as the system cannot read it), and that `icacls .auth /reset /t` gives the folder and its files the inherited entries back. `whoami` gives the domain-qualified account name, which `icacls` resolves for a domain or Microsoft Entra account where a bare user name may not map | `docs/examples/playwright/README.md:44-58` | none (docs only; M63 checks the Windows command by hand) |
| 10 | medium | (d) `docs/HANDOFF.md` step 3, after the `.msapp` sentence: the two exported zips hold the same `.msapp`, so they hold the URL too; keep `ContosoApp.zip` and `deploy/ContosoApp_managed.zip` out of a public repository, for example by adding those two paths to `.gitignore` yourself; the settings file of step 6 holds the target's connection ids and values and stays out of Git; a commit that is never pushed exposes nothing, but pushing the branch later publishes every commit on it, so keep these files out of commits in a repository that may become public; a `.gitignore` entry does not stop Git tracking a file it already tracks (`git rm --cached <file>` stops it and keeps the file on disk), and a file once pushed stays in the history that was pushed | `docs/HANDOFF.md:42` | `HandoffDocTests` passes |
| 11 | high | Acceptance tests in `OutputHygieneTests` (Done when 8): a solution built with the test builders holds a cloud flow whose definition has a fake environment URL whose host starts with `m61sentinel` (PO006) and a canvas formula with the same URL (PO014); `Analyzer.Run` then `Redaction.Apply`; each of the four `analyze` writers' output has no `m61sentinel` and has the placeholder; JSON and SARIF still hold `helpUri`. A pair of solutions whose variable's default goes from `m61sentinel-old` to `m61sentinel-new`, and whose flow name holds the sentinel URL, through `Compare(..., redact: true)` and the three `DiffReport` writers has no `m61sentinel`. `TextReport.WriteNotChecked`, `WriteNotes` and `WriteNotCompared` through a `Redaction.Writer`, fed lines with the sentinel URL, write none. The same inputs without redaction show the sentinel | `tests/PowerOps.Core.Tests/Output/OutputHygieneTests.cs` | the tests themselves |

## Automation

None.

## Screens

None.

## Environments and route

None. The build reads and writes no tenant.

## Solution and publisher

None.

## Constraints

- AGENTS.md: smallest change, new logic with tests, writes only through `SafeWrite` (no new write: `--out` already goes through it), Conventional Commits, no version or changelog edits, no tenant URLs, ids, emails or user names. The sentinel and fixture URLs are fake (`contoso`, `m61sentinel`).
- The pinned roles `powerops-verify` and `powerops-review-security`, their `plugins/*/skills/` copies and `tests/eval` are untouched. The eval baselines record the output of `analyze`, `test`, `diff` and `settings validate` without `--redact`, so Done when 3 keeps them; if one changes, the build stops and reports instead.
- No output shape changes: no schema, no `schemaVersion` bump.
- Platforms are Windows and Linux.

## Assumptions

- Learn's "Recommendations for protecting application secrets" (Power Platform Well-Architected, SE:07) asks to review and sanitize application logs so that no secrets are recorded, and says that settings which are not secrets, such as API URLs, should be kept apart from code and secrets, and that compliance can make such settings secret. An environment URL is such a setting, so the redaction is optional, not a default. https://learn.microsoft.com/power-platform/well-architected/security/application-secrets (checked 2026-11-05). <!-- plan-status: allow -->
- Learn's `icacls` page documents `/grant[:r] <sid>:<perm>` (`:r` replaces earlier explicit grants), the `(OI)` and `(CI)` inheritance rights and `F` for full access, and lists the inheritance switch as `/inheritancelevel:e|d|r`. Learn's "OpenSSH Server configuration for Windows" runs `icacls.exe <file> /inheritance:r /grant "Administrators:F"`, the spelling `icacls /?` prints. The README uses `/inheritance:r`; M63 runs the command on Windows and records its output. https://learn.microsoft.com/windows-server/administration/windows-commands/icacls, https://learn.microsoft.com/windows-server/administration/openssh/openssh-server-configuration (checked 2026-11-05). <!-- plan-status: allow -->
- `TextWriter.WriteLine(string)` calls `Write(string)` and then writes the new line, so a writer that overrides `Write(string)` sees each line's text in one call (.NET base class behaviour, read from the open source of `System.Private.CoreLib`'s `TextWriter` in .NET 10).
- Only PO006 and PO014 put a value into a finding: every other rule's message quotes names, counts or paths (the `new Finding(` calls under `src/PowerOps.Core/Rules/` at `0fb8f3d`). In `diff`, only `defaultValue` prints a value.

## Open questions

## Questions for your admin

## Steps

1. Test first: the `Redaction` unit tests and the acceptance tests of row 11, then the class (row 1). Run them red, then green.
2. `SolutionDiff` (row 2) with its tests.
3. The two flags (rows 3 and 4) with their command tests.
4. The Action input and `diff.sh` (row 6) with the `ActionYmlTests` and `ActionScriptTests` cases.
5. Docs (rows 7 to 10).
6. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`; `git diff tests/golden tests/eval plugins` is empty.
7. Leak scan, push, PR, CI.
