plan-hash: 354983bcb31c89477b94de8d9cfc00ef5444772775a62c0a76bd9b23e624068d
model: claude-opus-5-5
fresh-context: yes

This review ran in a subagent whose context holds no planning conversation. It read `plan.md`, `decisions.md` (no `mockups/`) and the repo at `6fbdcb4`.
Environment facts were not read: no `pac` sign-in was confirmed, and the plan's `environment:` is null (tenant: offline; no `pac`, `az` or other tenant call was made).
This is a maintainer milestone for PowerOps itself (a read-only CLI command and README changes), not a Power Platform solution. Checklist items 1 (solution and publisher), 2 (connection references and environment variables), 3 (licences), 4 (DLP), 6 (managed or unmanaged), 7 (data volume and delegation) and the per-product parts of 11 are not applicable. Item 5 is read as the pull request, CI and the owner's merge authority in `decisions.md`; item 8 as what `doctor --live` may print and read.
Tools checked with `command -v` on this machine: `powerops`, `dotnet`, `git`, `gh`, `jq`, `pac`, `az` and `claude` all found. The leak scan is `reports/leakscan.sh` in the main checkout (git-ignored, absent from this worktree). Not checkable here beyond their presence in the agent list: the `fable-reviewer` and `powerops-researcher` agents.
Repo facts the plan rests on were found: `PacRunner` and its `Resolve` PATH lookup, `DriftLiveReader.AzExecutable`, `SkillsCommand.Shipped()` with `skills/powerops-plan/references/licensing.md` embedded, its section 3 table, `OutputSchemaTests.Map`, `DocsDriftTests`, `.github/scripts/sample-README.md` with its two commands and result lines, and an `installed_plugins.json` of the shape Data row 2 describes. Open questions: 0; questions for your admin: 0.

## Findings

### High

No findings.

### Medium

#### R1. The test seams for PATH, the plugin record and the `az` runner are not named
- Area: 9 Testability, 13 Buildability
- Finding: Done 1 to 3 need tests "with a PATH that holds neither", "fake `pac` and `az` on a temp PATH", fake `az` runners and goldens "from a fixed host", but `Program.Run` and `Program.Root` take one `pac` runner and build the `az` runner inside `DriftLiveReader`'s lazy reader, and `PacRunner.Run` reads the process's `PATH` and `PATHEXT`. The plan names no seam for an `az` runner, for `PATH`/`PATHEXT`, for `CLAUDE_CODE_PLUGIN_CACHE_DIR`, `CLAUDE_CONFIG_DIR` and the home folder, or for the masked version. Without them a test must set process environment variables, which races other xUnit tests running in parallel, and GitHub's runner images ship `az`, so a test that reads the real `PATH` would see it found.
- Suggested fix: In Done 1 to 3 or step 4, name the seams: an `az` runner parameter beside `pac` on `Program.Run`/`Root` (defaulting to the current `PacRunner(DriftLiveReader.AzExecutable(...))`), and an environment lookup (or values) for `PATH`, `PATHEXT`, the two plugin variables and the home folder passed to `DoctorCommand.Create`; and say tests never call `Environment.SetEnvironmentVariable`.
- Resolution: open

#### R2. The `az account show -o none` probe has no source
- Area: 13 Buildability
- Finding: Done 2 and Data row 3 treat a zero exit of `az account show -o none` as `az sign-in: ok` and gate the licence read on it. A4 sources the Graph read and the `pac org who` line has AGENTS.md's capture, but nothing in the plan sources the probe's exit behaviour when no account is signed in, and there is no `unproven:` line. The repo already uses the same probe in `skills/powerops-implement/SKILL.md`, which is a pattern, not a source.
- Suggested fix: Add the source (Learn's `az account show` reference, with its date) to A4 or a new assumption, or an `unproven: az account show exits non-zero with no signed-in account; probed by step 4` line with a fake-runner test of both outcomes and the follow-up noted as unrun against a real `az`.
- Resolution: open

### Low

#### R3. The README's quoted sample results can drift from the sample README
- Area: 9 Testability
- Finding: Data row 5 has the README quote the two result lines of `.github/scripts/sample-README.md`. No test ties the README's copy to that file, so a later change to the sample's expected output leaves the README wrong.
- Suggested fix: Add a check (in `DocsDriftTests` or `WorkflowTests`) that each quoted result line appears in `sample-README.md`, or link the sample README's "Expected result" instead of quoting it.
- Resolution: open

#### R4. Only the plain output has a golden, so the live checks' JSON is never schema-checked
- Area: 9 Testability
- Finding: Done 3 names goldens `doctor.json` and `doctor.txt` from plain `doctor`. `OutputSchemaTests` validates only goldens, so the four `--live` rows (`pac sign-in`, `az sign-in`, `licences`, `environment type`) and their statuses never meet `doctor.schema.json`.
- Suggested fix: Add a `doctor-live.json` golden from the fake runners (covered by the `doctor` prefix in `Map`).
- Resolution: open

#### R5. The plugin record's size check can be passed through a symbolic link or a growing file
- Area: 8 Security
- Finding: Data row 2 checks the size first, then reads through `File.ReadAllText`, and reads a symbolic link like a file. On Windows a `FileInfo` of a symbolic link reports the link's own length, not the target's, and any file can grow between the check and the read, so the 1 MB cap is not a cap.
- Suggested fix: Read through a stream capped at 1 MB plus one byte (as `PacRunner` caps its output), and treat an over-cap read as `not read`.
- Resolution: open

#### R6. The licence line and the `pac` line can be about different accounts, and a refused read sets exit 1
- Area: 8 Security roles, 10 Unanswered areas
- Finding: `pac sign-in` reports pac's selected environment and `licences` reports `az`'s signed-in account, which may be another account or tenant; the output, which prints no account by design, doesn't say so. A4 also expects a basic user without a directory role to be refused, which is `failed` and so exit 1 (Data row 4, C5) for the plan's own target user, though nothing on the machine needs fixing.
- Suggested fix: In COMMANDS' `doctor` section and the `--live` text, say the licence line is `az`'s account, not necessarily pac's; and state in C5 that a refused licence read sets exit 1 on purpose (agents read the per-check status in the JSON), or leave the licence read out of the exit code.
- Resolution: open

#### R7. The leak scan is not named by path
- Area: 12 Tools
- Finding: Done 8 says "the leak scan clean" without naming it. The script is `reports/leakscan.sh` in the main checkout, git-ignored, so it is not in this worktree.
- Suggested fix: Name the script and that it runs from the main checkout against the worktree.
- Resolution: open

#### R8. A URL in the plan
- Area: URLs, GUIDs, emails and names
- Finding: Data row 3 holds a `<url>` (the Microsoft Graph endpoint the licence read calls), kept on purpose with the `plan-status: allow` marker; it is the same constant the planning skill uses.
- Suggested fix: None needed if kept on purpose.
- Resolution: open
