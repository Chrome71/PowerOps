plan-hash: af129aed433687f3aa3f160aaf673f583a96ba761a7900b5ce317a5b74b32bdb
model: claude-sonnet-5-5
fresh-context: yes

Environment facts were not read: the plan names no environment (`environment: null`), changes this repo's own code and docs, and the request did not confirm a `pac` sign-in; `pac` and `powerops plan facts` were not run. The recomputed body hash equals the one given. `powerops plan status` was not run (the header says `approved`, `openQuestions` is 0, nothing stands under `Questions for your admin`). Checklist items 1 to 9 and 11 (boundaries, connection references, licences, DLP, route, managed state, delegation, roles, size) do not apply to a change to this repo's code. Item 10 applies and found nothing open.

The review checks the plan against the code at `dd3215e` (the plan commit; code as of `0fb8f3d`). Checked and found right: only PO006 and PO014 put a value into a finding (all 31 `new Finding(` calls were read; the rest quote names, counts or paths); the PO006/PO014 URL pattern is inside the plan's redaction pattern (it stops at the quote, `?`, `#` and `/` the pattern also stops at or passes), so no part of such a URL is left over; in `diff` only `defaultValue` prints a value; `JsonReport` and `MarkdownReport` print no model field the plan leaves out (the Markdown report does not print the component); `analyze` and `diff` write all their stderr through one `error` writer inside `Execute`/`Run`, and `Run` writes the not-checked notes before `emit`, so a wrapper covers them; the five `analyze` steps of `action.yml` are exactly the five the plan lists; `ActionYmlTests` and `DocsDriftTests` need only the `redact` input to be documented and `--redact` to exist in the command tree, and neither breaks; the baseline is applied before `emit`; `BaselineCommand` and the MCP `analyze` and `diff` tools go through `Run` or the CLI without the flag and do not change; the `icacls` switch the plan uses (`/inheritance:r`, with `(OI)(CI)F`) matches the OpenSSH page, and `chmod 700`/`600` is right for Linux. The URL, GUID, email and person-name scan is under R7.

## Findings

### High

#### R1. The SARIF fingerprint suffix changes `analyze-fixtures.sarif` and the output without `--redact`
- Area: 9 (testability; Done when 3, "no default changes")
- Finding: Row 5 appends `|2`, `|3` to the second and later equal fingerprint keys of every SARIF run, and says "a key seen once is hashed as today, so no golden changes". The golden `tests/golden/analyze-fixtures.sarif` already holds equal `poFinding/v1` values (4 values appear more than once, from the same rule, component and message in different fixture solutions with one unique name). With the row as written, the second and later of those get a new fingerprint, the golden changes, Done when 3 ("every golden stays as it is, byte for byte") fails, and every existing code scanning user with such a duplicate gets alerts closed and opened without having asked for `--redact`. That is a default change D7 rules out. Q4's reasoning (equal keys that redaction creates) covers only the redacted case. A first pass at the code in the worktree has the same effect.
- Suggested fix: apply the suffix only when the result was redacted. Give `SarifReport.Write` a `bool redacted = false` (or a count-suffix flag) that `AnalyzeCommand` sets from `--redact`, and test both ways: without the flag a duplicate pair keeps today's equal values (the existing golden); with it the second gets `|2`. Say in Q4 and row 5 that the suffix is part of the `--redact` output only, and that switching `--redact` on or off changes the fingerprints of the findings with a URL once (already in row 7).
- Resolution: open

### Medium

#### R2. `action/diff.sh` and `ActionScriptTests` are not in the plan's test or change list
- Area: 9 (testability; every place the change must reach)
- Finding: `tests/PowerOps.Cli.Tests/ActionScriptTests.cs` runs `diff.sh` with a stub `dotnet` and asserts the exact call (`diff <base>/sol sol --format markdown --out <comment>`), with an environment that sets no `PO_REDACT`. `diff.sh` runs under `set -euo pipefail`, so reading `$PO_REDACT` unquoted and unset stops the script and breaks the existing cases (and any caller outside the action). Row 6 names only `ActionYmlTests` and `DocsDriftTests`. Also, the `Diff` step's `run` is `bash diff.sh`, so the "adds `--redact` when `true`" check of `ActionYmlTests` cannot be made on the YAML for that step; it needs the script.
- Suggested fix: read it as `${PO_REDACT:-}` in `diff.sh`; add `--redact` to the `diff` call only when it equals `true`, leaving the call otherwise byte-identical; add a case to `ActionScriptTests` with `PO_REDACT=true` that checks the recorded call and one with it unset that keeps today's assertion; list `ActionScriptTests` in row 6 and let `ActionYmlTests` check only that the `Diff` step sets `PO_REDACT`.
- Resolution: open

#### R3. A stderr path the wrapper misses: exceptions that leave `DiffCommand.Execute`
- Area: 9 (a path that `--redact` would miss)
- Finding: In `DiffCommand.Execute`, the call to `SolutionDiscovery.Find` (before the `try`) catches only `InvalidDataException`; any other exception (for example an `IOException` or `UnauthorizedAccessException` whose message holds a path or a link) reaches `Program.Run`'s catch, which writes to the original, unwrapped `error`. Likewise System.CommandLine parse errors (an argument that is a URL is echoed in `error: ...`) are written by `Program.Run` to the original writer. Done when 2 says "every stderr line" for `diff`. The `analyze` path is covered (its `Find` call has `catch (Exception)`).
- Suggested fix: in `Execute` of both commands, create the wrapper first and widen the `Find` catch to `Exception` routed through the same `Fail`-style line (as `analyze` does), and say in the plan and in "Values in outputs" that usage and parse errors that echo an argument are not redacted (an argument is the user's own input). Add a test that makes `Find` throw a non-`InvalidDataException` with the sentinel in its message, or drop the claim to "every line the command writes".
- Resolution: open

### Low

#### R4. The `Redaction.Writer` contract is thinner than the claim "every write"
- Area: 9 (whether the `TextWriter` wrapper catches every write)
- Finding: Only `Write(string)` is redacted; `Write(char)`, `Write(char[], int, int)` and `Write(ReadOnlySpan<char>)` pass through, so a URL written in pieces or as a span is not caught. Today's code writes only strings and `WriteLine(string)`, so nothing leaks now, but the plan states it as "every string". The plan also leaves open the `Encoding` it reports, `NewLine` (a wrapper has its own `CoreNewLine`, so on Windows it can differ from the inner `Console.Error`'s), `Flush`, and not disposing the inner writer. The assumption cites "the open runtime source" with no version.
- Suggested fix: state that the wrapper overrides `Encoding`, `Write(string)`, `Write(char)`, `Write(char[],int,int)` (for these two, buffer by line or say plainly that they are not redacted), forwards `NewLine`/`Flush`, and never disposes the inner writer; add a test that `WriteLine` with a format argument is redacted; name the .NET version the assumption was read at.
- Resolution: open

#### R5. Details of the URL pattern
- Area: 9 (leaks and over-redaction)
- Finding: `[^\s'"<>]*` takes trailing punctuation with the URL (a `)`, `.` or `,` after it disappears into the placeholder), and two PO014 URLs in one app give two lines with the same message and component, so the reader cannot tell them apart (the SARIF case is handled; text, JSON and Markdown are not). The pattern, with a `\b` and an unbounded scheme run, backtracks quadratically on a very long run of letters with no `://` (a long flow or app name); `Regex` has no timeout here.
- Suggested fix: accept the punctuation case and say so in the docs; say in "Values in outputs" that equal-looking lines are different findings; give the generated regex a match timeout (or bound the scheme at 32 characters) and test a 100,000-letter string.
- Resolution: open

#### R6. Windows and Linux advice: add the way back and the side effect
- Area: 9 (Windows `icacls` and Linux `chmod` advice)
- Finding: Learn's `icacls` page documents `/inheritancelevel:r` ("removes only inherited ACEs") and not `/inheritance:r`; the OpenSSH page uses the latter, and the plan already says M63 runs it. `/inheritance:r` leaves the folder without entries for SYSTEM and Administrators, so backup, antivirus and an administrator's cleanup cannot read it until access is restored; the README does not say that, nor how to undo it. On Linux, `chmod 600` after the save is right, but a `umask 077` before the sign-in covers a re-save too.
- Suggested fix: one sentence in the README that only your account can open the folder afterwards, and `icacls .auth /reset /T` to return to the inherited entries before deleting it; keep the "M63 checks it" note; optionally mention `umask 077`.
- Resolution: open

#### R7. URLs in the plan folder
- Area: scan (URLs, GUIDs, emails, names)
- Finding: `plan.md` holds three public Microsoft Learn links (Assumptions) and the fake sentinel URL `https://m61sentinel.crm.dynamics.com`; `decisions.md` holds none. None is from a tenant, no GUID, email or person's name was found. Reported because the skill asks for any URL; the values are not repeated beyond the fake sentinel, which is a `contoso`-style fake.
- Suggested fix: none needed for the Learn links (public documentation, as in other plans); keep the sentinel fake.
- Resolution: open

#### R8. Handoff and agents: say what the policy does not cover
- Area: 9 and 10 (places the change must reach)
- Finding: "Users" names "an agent's transcript" as a place where a report is shared, but the MCP `analyze` and `diff` tools, which agents use, return unredacted JSON (Q6, settled). "Values in outputs" (row 7) lists `--redact` as `analyze`/`diff` only but does not say the MCP tools are outside. For HANDOFF step 3 (row 10), a `.gitignore` entry does not remove a file already tracked or committed; the text says only to add the paths.
- Suggested fix: add one line to "Values in outputs" that the MCP tools return the same unredacted text as the CLI without `--redact`; in step 3 add that a `.gitignore` entry does not untrack a file that is already committed (`git rm --cached`), and that a file once pushed stays in history.
- Resolution: open
