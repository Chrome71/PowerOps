plan-hash: 3a8e01cca6889c6131d2c7a8c1e06ba0bc19f7bb740084fede799c268172c6d5
model: claude-opus-5-5
fresh-context: yes

Run 3. This review ran in a subagent whose context holds neither the planning conversation nor runs 1 and 2: it read run 2's `review.md` from the folder only to see which findings were answered. It read `plan.md`, `decisions.md` (no `mockups/`) and the repo at `d72765d`.
Environment facts were not read: no `pac` sign-in was confirmed, and the plan's `environment:` is null (tenant: offline; no `pac`, `az` or other tenant call was made).
This is a maintainer milestone for PowerOps itself (a read-only CLI command and README changes), not a Power Platform solution. Checklist items 1 (solution and publisher), 2 (connection references and environment variables), 3 (licences), 4 (DLP), 6 (managed or unmanaged), 7 (data volume and delegation) and the per-product parts of 11 are not applicable. Item 5 is read as the pull request, CI and the owner's merge authority in `decisions.md`; item 8 as what `doctor --live` may read and print.
Tools checked with `command -v` on this machine: `powerops`, `pac`, `dotnet`, `git`, `gh` and `claude` (2.1.326) found; `reports/leakscan.sh` is in the main checkout. The `fable-reviewer` and `powerops-researcher` agents are in this session's agent list. Not checked: what Claude Code's docs say `CLAUDE_CODE_PLUGIN_CACHE_DIR` points at (A2 cites the docs; no web page was read here).
Repo facts the plan rests on were found: `PacRunner.Resolve(name, path, windows, pathExt)` (no process started), `DriftLiveReader.AzExecutable`, `PlanFacts.SignInFix`, `SettingsCommand.Field` (the `Friendly Name:` parse), `TextReport.Clean`, `BoundedRead.ReadAllText`, `SkillsCommand.Shipped()`, section 3 of `licensing.md` (`POWERAPPS_PER_USER` is Power Apps Premium; neither of Done 2's two bundle part numbers is listed), `OutputSchemaTests.Map` (a `doctor` prefix clashes with none and matches both `doctor.json` and `doctor-live.json`), `DocsDriftTests.KnownFlags`, `QuickStartProblems` (the first fence holding `git clone`), `McpCommandTests.ToolNames`, the 19 leaf commands of `Program.Root` (with `doctor`, the 20 of Row 5), and an `installed_plugins.json` of version 2 with the fields Data row 2 names. With the installed CLI, `powerops analyze tests/fixtures/powerops-seed/v2` and `powerops test tests/fixtures/powerops-seed/v2 --tests tests/fixtures/flowtests/seed` exited 0 and printed the sample README's two result lines.
Run 2's findings are answered in the plan: R1 by A4 and C5 now saying `not-read` (C7), R2 by Row 5's clone-root commands and the fence order, R3 by Rows 1 and 2 and Done 4, and R4 is kept with the allow marker (R7 below). Step 2's Fable plan review has no approval logged yet; that step is still running, so it is not a finding. Open questions: 0; questions for your admin: 0.

## Findings

### High

No findings.

### Medium

#### R1. On Windows every licence line ends in a carriage return, which Data row 3 makes a failed read
- Area: 13 Buildability, 9 Testability
- Finding: Data row 3 says a line is a part number only when it "holds no control character", and any other line makes the read fail. `az` on Windows (the primary platform) is Python writing text-mode stdout, which ends lines with CRLF; split on `\n` alone, as `SettingsCommand.Field` and most of the repo split `pac`'s output, each line keeps a `\r`, so a Windows account holding Power Apps Premium would read as `not-read`. The fake-runner tests of Done 2 use LF output only, so they pass on the Windows CI leg and miss it.
- Suggested fix: In Data row 3, say the output is split into lines on CRLF or LF (`ReadLines` or `ReplaceLineEndings`) before the control-character check, and add to Done 2's test list the same part numbers with CRLF line ends, read as a success.
- Resolution: open

### Low

#### R2. The three argument lists are written two ways
- Area: 9 Testability
- Finding: Done 2 asks tests to "assert the exact three argument lists", but gives the Graph read as `--query "value[].skuPartNumber"` with shell quotes and no `-o tsv`, while Data row 3 has `--query value[].skuPartNumber -o tsv`. Without `-o tsv` `az` prints JSON, which the line rule of Row 3 can't read; with the quotes taken literally, the JMESPath query is a string.
- Suggested fix: Write the three lists once, in Data row 3, as list elements (for example `["rest", "--method", "get", "--url", "<url>", "--query", "value[].skuPartNumber", "-o", "tsv"]`), and have Done 2 point to Row 3.
- Resolution: open

#### R3. "`--live` prints no ... id" while it prints plugin ids
- Area: 10 Unanswered areas (wording)
- Finding: Constraints says `--live` prints no account, tenant, URL, id or part number; `--live` runs the plain checks, which print plugin ids such as `<plugin>@<marketplace>` (Data row 1, Done 2). The intent is environment and tenant ids, but a reviewer of the build could read the line literally.
- Suggested fix: Say "no account, tenant or environment id, URL or part number".
- Resolution: open

#### R4. The found-on-PATH test can pass without the shipped lookup
- Area: 9 Testability
- Finding: Data row 6 puts "a PATH lookup" in `DoctorHost` and tests pass their own host. Done 1's second test then gives a temp folder "as the PATH", but if the test's host supplies its own lookup function, the default host's lookup (PATH, PATHEXT, `az.cmd` on Windows, the execute bit on Linux) is never run by a test.
- Suggested fix: In Data row 6, make the host hold the PATH and PATHEXT strings (or build its lookup from them through the same factory the default host uses), so the second test of Done 1 runs the shipped lookup over the temp folder, with an executable file on Linux and `az.cmd` and a PATHEXT name on Windows, as `PacRunnerResolveTests` does.
- Resolution: open

#### R5. The ecosystem record misses ECO-0013 and the Claude Code version the plugin record was observed on
- Area: 10 Unanswered areas (AGENTS.md "Ecosystem changes")
- Finding: The plan adds a third reader of `pac org who`'s text and edits the AGENTS.md lines that open entry ECO-0013 (`pac` documents `--json` while PowerOps parses text, "plan for later") names as affected, but neither the plan nor `decisions.md` names ECO-0013. Data row 2 and A2 say the record's shape was observed on "Claude Code on this machine" without its version, which the new compatibility row (Done 5) needs for its Evidence cell.
- Suggested fix: Add to A2 the Claude Code version the shape was observed with, and a line (A2 or Done 5) that `doctor --live` reuses `SettingsCommand.Field`, so ECO-0013's switch, when it comes, covers it; name `doctor` in ECO-0013's Affects only if the entry's procedure asks for it.
- Resolution: open

#### R6. The newcomer's install prompt still asks for a manual PATH check
- Area: 10 Unanswered areas
- Finding: The Goal is "one command that says whether their machine is ready", and the README's "With your AI agent" prompt, the newcomer's first path, ends its step 5 with "Tell me whether `pac` and `az` are on PATH". Row 5 doesn't say whether that step becomes `powerops doctor`, and Won't build doesn't exclude it.
- Suggested fix: In Row 5, either have step 5 run `powerops doctor` and report its lines, or list the install prompt under Won't build with the reason.
- Resolution: open

#### R7. A URL and an owner handle in the plan
- Area: URLs, GUIDs, emails and names
- Finding: Data row 3 holds a `<url>` (the Microsoft Graph endpoint of the licence read), kept on purpose with the `plan-status: allow` marker as in runs 1 and 2; Data row 1's `not-installed` hint holds the repository owner's handle in `claude plugin marketplace add <owner>/PowerOps`, the command the README already prints.
- Suggested fix: None needed if both are kept on purpose.
- Resolution: open
