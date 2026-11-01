---
status: done
review: done
signed-off: 2026-11-01
signed-off-hash: 09926f1103a73e23c0aaf7e7856aeaf8e47358ee5ef8a17603acb4f00ed613e0
environment: null
depth: standard
supersedes: null
---

# Phase 6: hardening (M22–M32)

## Goal

Refine and secure what Phases 0 to 5 built, and add no feature. The user asked for this phase on 2026-10-28, after Phase 5 was merged at `b4abce4`. This plan holds the read-only audit that found the gaps, ranks them by risk, and splits the fixes into the eleven milestones of the BUILD-PLAN section "Phase 6: hardening (M22–M32)". Its build-order items are 47 to 57.

Out of scope: M17 (on hold by the user's choice), M21 (`powerops deploy gate`, planned in another session; see Constraints), NuGet publishing (the user said to ignore it) and the rename (the name stays PowerOps).

Done when:

- Every finding below rated high or medium is fixed, or listed under "Accepted" with its reason. Each low finding is fixed in its milestone or accepted the same way.
- Each fix has a check that fails without it:
  - a code fix has a test;
  - a settings change has the API reads in the BUILD-PLAN verification record;
  - a CI change has a passing run of the changed workflow, or a test over the workflow file;
  - a docs fix has the test that M22 or M31 names.
- Commands, options, JSON fields, schemas and documented exit codes stay as they are. What changes is that hostile or broken input is refused (exit 2 or 3, or a tool error) where today it is read, crashes or exhausts memory, and that the two wrong exit codes the table names (R-5, C-3) follow AGENTS.md. "Compatibility" under Automation lists every change a CI user can see.
- No new dependency. Lock files and a licence test are not dependencies.
- Each milestone's PR passes `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` and `powerops plan status .`, with CI green on Linux, Windows and macOS.
- M32's manual steps on Windows and macOS are run and recorded, or marked not verified with the reason.

## Users

- Agents in Claude Code and Codex that run `powerops`, or call `powerops mcp`, on repos and exports they didn't write: a clone, a pull request's head, an export from another team.
- The person behind the agent, who installs the tool and is the only one who answers `deploy_apply`'s form.
- CI: the Action on pull requests, fork pull requests included.
- Someone who reports a vulnerability through SECURITY.md.

## Data

### How the audit ran

Six read-only agents (Sonnet) audited `main` at `b4abce4` on 2026-10-28:

- **Readers and file inputs.** A general-purpose agent read the code and ran the built CLI on crafted inputs in a scratch folder, under a 2 GB heap cap (`DOTNET_GCHeapHardLimit`).
- **MCP inputs and subprocesses.** The `powerops-reviewer` agent.
- **Action and supply chain.** A general-purpose agent, with `gh` reads only.
- **Correctness and exit codes.** A general-purpose agent that read the code and probed it.
- **Docs drift.** A general-purpose agent comparing every doc with `--help` and the code.
- **Open research items.** The `powerops-researcher` agent, on Microsoft Learn and NuGet.

This session then re-ran S-1, R-1, R-5, C-4 and C-7 itself, and read D-2 and S-2 in the code.

The Evidence column says how each finding is known:

- **Observed:** a probe, a test or an API read showed it.
- **Inferred:** read in code or docs only.

Paths are under `src/PowerOps.Cli/` (Cli) or `src/PowerOps.Core/` (Core) unless given in full. Line numbers are at `b4abce4`.

### Findings by milestone

Severity: **high** means code runs, or the tenant is written, without the user's intent; **medium** means data loss, a wrong result in CI, or a small input that exhausts a machine; **low** means a wrong message, a misleading log line, or a gap that only matters with another bug.

#### M22. Quick security fixes

| ID | Sev | Evidence | Where | What breaks | Fix |
|---|---|---|---|---|---|
| S-1 | high | Observed (this session: a `pac` script in the current folder ran when PATH held only a folder with a different `pac`, and when PATH held no `pac` at all) | Cli `PacRunner.cs:15`, `Program.cs:20,29` | `Process.Start("pac")` and `("az")` look in the current folder before PATH. A clone with an executable `pac` or `az` at its root runs it with the user's sign-in when `powerops` or `powerops mcp` starts there | Resolve each name once to an absolute path by scanning only PATH's absolute entries (skip empty and relative ones; PATHEXT order on Windows) and start that path; not found keeps today's "not found" result |
| I-1 | high | Observed: `PowerOps` returns 404 on nuget.org (2026-10-28). Risk inferred | `README.md:10,17,25`; `skills/powerops-plan/SKILL.md:26`; `skills/powerops-implement/SKILL.md:14`; `.github/workflows/ci.yml:38`; `tests/PowerOps.Cli.Tests/PackedToolTests.cs:39` | The package ID is unregistered. The skills tell the user to run `dotnet tool install --global PowerOps`, which would install whatever package someone registers under the ID; so do the README's NuGet line (`:10`) and its Quick start one-liner (`:25`). The README's from-clone line uses `--add-source artifacts` with no version, so a higher version on nuget.org would win too | The README's NuGet line goes until the first release, which adds it back. The Quick start builds from a clone. Every install of `PowerOps` in the README, CI and the test uses `--source artifacts`, which replaces all sources, and the skills point at the README's Install section. A test over README.md, CONTRIBUTING.md, `docs/` (without `docs/plans` and BUILD-PLAN) and `skills/*/SKILL.md` fails on a `dotnet tool install` of `PowerOps` without `--source` |
| D-2 | medium | Observed (text); risk inferred | `skills/powerops-implement/SKILL.md:42` | Step 7 names only `settings apply --apply` as needing the user's yes for that environment, while it tells the agent to promote with `deploy apply`. The general "ask each time" in the same step still applies | Name `powerops deploy apply --apply` and the MCP tool `deploy_apply` with `apply: true`, as AGENTS.md does. Pin the sentence in `SkillsCommandTests`, and regenerate the plugin copies |
| C-7 | low | Observed (this session: `analyze @<file>` read the file's lines as arguments, exit 0) | Cli `Program.cs` (the parser configuration) | System.CommandLine expands `@file` response files by default. The MCP layer already refuses a leading `@`; the CLI doesn't | Turn response files off (`ResponseFileTokenReplacer = null`), with a test |
| A-3 | medium | Inferred | `Directory.Build.props:7`; `action.yml:55` | `TreatWarningsAsErrors` turns NuGet audit warnings NU1901 to NU1904 into errors, so a new advisory for any dependency breaks every consumer's Action build at once | Keep those four warnings (`WarningsNotAsErrors`), with a test over `Directory.Build.props`. The CI step that fails on a vulnerable package is A-3b in M29 |
| D-3 | medium | Observed: no tags or releases exist | `README.md:37,51,67,172`; `docs/PLANNING-SKILLS.md:262,295` | Snippets use `Chrome71/PowerOps@v1`, which doesn't exist, and version numbers that release-please doesn't control | The full SHA of an already merged commit in every snippet, with a comment to pin the newest; no version numbers. A README can't name the commit that holds it |
| D-5 | medium | Observed: `skills install --help` has no such option | `docs/PLANNING-SKILLS.md:80,264` | `--agents-md` is documented; an agent passing it gets exit 2 | Remove it |

#### M23. Repo settings (the user's yes)

| ID | Sev | Evidence | Where | What breaks | Fix |
|---|---|---|---|---|---|
| A-4 | medium | Observed: the API returns `{"enabled":false}` for private vulnerability reporting | `SECURITY.md:8-9`; `.github/ISSUE_TEMPLATE/config.yml:3-5` | SECURITY.md sends reporters to private vulnerability reporting, which is off, and says not to open a public issue. A reporter has no private route | The user turns private vulnerability reporting on. If the user declines, SECURITY.md is changed in M31 to say so |
| A-1 | medium | Observed: `branches/main/protection` returns 404, rulesets `[]`, no tags | repo settings; `.github/CODEOWNERS:1` | `main` takes force pushes and unreviewed merges, and CODEOWNERS is advisory. Consumers who pin `@main` (README) run whatever lands there inside jobs that hold OIDC deploy credentials | A ruleset on `main`: a pull request, no force push, no deletion, and the six CI checks `build (ubuntu-latest)`, `build (windows-latest)`, `build (macos-latest)`, `package`, `action` and `playwright-example` required. The owner is a bypass actor, because release-please's PRs are opened with `GITHUB_TOKEN`, which starts no CI (PR 4 has no checks). A ruleset on `v*` tags: no update, no deletion. The check list is revisited after M29 and M32, which may rename jobs |
| A-18 | medium | Observed: Dependabot alerts off, security updates off, CodeQL default setup not configured, `sha_pinning_required` false | repo settings | A known-vulnerable dependency raises no alert or PR; nothing scans the C# or the workflows | Four separate settings, each asked on its own: Dependabot alerts, Dependabot security updates, CodeQL default setup (C# and Actions), and required SHA pinning (every `uses:` is already pinned) |

#### M24. `deploy_apply` and MCP inputs

| ID | Sev | Evidence | Where | What breaks | Fix |
|---|---|---|---|---|---|
| S-2 | medium | Inferred; the test `A_plan_file_rewritten_while_the_form_is_open_is_refused` covers only a change during the form | Cli `McpCommand.cs:333-345`; `DeployCommand.cs:206` | After the user's yes, the server reads the plan file again and compares its hash, then runs `deploy apply --apply`, which reads the folder a third time and trusts the hash it finds. Whatever can write to the deploy folder can swap in another consistent plan and its zips in that gap, and that plan imports | The import gets the hash the user approved inside the process (no new CLI option) and refuses, importing nothing, when the re-plan's hash differs. The residual swap of a zip between its last hash and `pac` reading it is accepted (see Accepted) |
| S-3 | medium | Inferred; tests cover only a refused non-Dataverse host | Cli `DriftLiveReader.cs:96-111`; Core `Drift/DriftParsers.cs:25` | `target` and `envs` are free text. A value the caller chooses that resolves to any documented Dataverse host gets a token from the user's `az` sign-in, requested for that host, and sent there. Whether that token works at any other org is not known | Research first, on Learn: is a token's audience one org's URL, and does Dataverse refuse it at another org? If Learn says it is refused, or says nothing (the likely answer), keep the host suffix check, record the answer, and add the residual to Accepted and to the README's MCP section; no tool input changes. Only if Learn says such a token works at another org does the master session ask the user before any input is narrowed (decisions Q11) |
| S-5 | low | Observed: a lone-surrogate escape gave JSON-RPC -32603 instead of a tool error; a path holding `\n` forged an `error:` line in the result | Cli `McpCommand.cs:554,571,581-598` | Bad strings reach the commands, or the SDK's generic error, instead of the tool error the spec asks for | Refuse any string with a control character (NUL included) or an unpaired surrogate as a tool error, and catch `InvalidOperationException` from `GetString()` |
| S-4 | low | Inferred | Cli `McpCommand.cs:516,521-613`; Core `Readers/SolutionDiscovery.cs` | Lists and strings have no length cap. Each `envs` entry costs a `pac`, an `az` and several Web API reads. `paths: ["/"]` walks the whole disk. Only `deploy_plan` gets the call's cancellation. A huge `target` makes the start fail with E2BIG, which `PacRunner` reports as "pac was not found on PATH" | Cap `paths` and `envs` at 50 and 10 entries and each string at 4,096 characters, as tool errors; the string cap also keeps a `target` from reaching E2BIG. The walk's cap is R-4 |
| S-8 | low | Inferred, not run | Cli `McpCommand.cs:345` | If the client closes stdin during an import, the server may exit while `pac` keeps importing, and no report comes back. The README says so | Check with a test whether the host waits for the running handler. If it doesn't, wait for the one-import guard before exiting |
| C-9 | low | Observed (an audit agent's `drift --env bad/emptydir` ran one read-only `pac org who`; it failed, exit 1) | Cli `DriftCommand.cs:32` | An `--env` value that doesn't end in `.json` and isn't a file counts as a live environment, so a mistyped snapshot path reads a tenant | Refuse (exit 2) a value that holds a path separator and is neither a URL nor an existing file |
| D-15c | low | Observed | Cli `McpCommand.cs:189-201` | The README tells agents to run `drift`, `deploy plan` and `plan facts` only against environments the user named. The tools' own descriptions, which the agent reads, don't say it | Add that sentence to the three descriptions, and update `tests/golden/mcp-tools.txt` |

#### M25. Analyze and diff input limits

| ID | Sev | Evidence | Where | What breaks | Fix |
|---|---|---|---|---|---|
| R-1 | medium | Observed (this session: a 192 KB flow file took `analyze` to 2,085 MB peak memory, exit 0; 80,000 leaves gave `OutOfMemoryException` under the 2 GB cap) | Core `Flows/FlowDefinition.cs:137,145,150`; `Rules/PO006HardCodedEnvironmentUrl.cs:21` | The flow parser keeps a full JSON path string for every string literal, which PO006 only tests with `StartsWith("$.parameters")`. A small deep file amplifies memory about 10,000 times | Keep a flag (or the first path segment) instead of the path |
| R-2 | medium | Observed: depth 25,000, 50,000 and 100,000 took 1.0, 4.0 and 16.8 s; depth 2 million timed out at 90 s | Core `Diff/SolutionDiff.cs:208` | `diff` loads canvas `*.meta.xml` files without the 200-level depth scan that `SolutionParser.cs:221-230` runs, and `XDocument.Load` is quadratic in depth | Load them through the same depth-checked reader |
| R-3 | medium | Observed: an 80 KB value took 5.3 s, time growing 4 times per doubling | Core `Rules/PO007EnvironmentSpecificDefaultValue.cs:23` | The email pattern backtracks on an environment variable's default value | `RegexOptions.NonBacktracking`, or skip values longer than 320 characters |
| R-4 | medium | Observed: a 46 MB file of empty elements peaked at 894 MB; 229 MB over five files ran out of memory under the 2 GB cap | Core `Readers/SolutionParser.cs:223`, `ZipReader.cs`, `XmlUnpackReader.cs`, `SolutionDiscovery.cs` | The byte caps (50 MB a file, 256 MB a solution) allow about 20 times more memory for XML. Zip entry counts, folder file counts and the discovery walk have no cap | Each limit is an `InvalidDataException` naming the path (exit 3): 5 million nodes, counted in the existing depth scan; 100,000 zip entries; 100,000 files in a solution folder; and 100,000 folders walked by discovery, whose message says to narrow the path. The probes put 500,000 entries at 312 MB, so 100,000 stay near 60 MB, and Learn's 95 MB limit for a solution file leaves real exports far below every cap |

#### M26. Plan, settings, baseline and simulator limits

| ID | Sev | Evidence | Where | What breaks | Fix |
|---|---|---|---|---|---|
| R-7 | medium | Observed: a symlink to `/dev/zero` in a plan folder ran out of memory; an 800 MB file took 5.3 GB; a link to an outside file was scanned | Core `Plans/PlanChecker.cs:28,51,61,150-153` | `plan status` and `plan sign-off` follow links and read whole files. A pull request can stop a plan-status check, and learn which lines of a runner's file match the tenant patterns (never their text) | Skip reparse points (`AttributesToSkip = ReparsePoint`, `IgnoreInaccessible`), and skip, with a warning, binary files and files over 1 MB, so a large mockup image doesn't fail a valid plan. Text files are scanned as today |
| C-2 | medium | Observed: a Foreach over N trigger items that reads `triggerBody()` took 0.6, 1.6, 7.5 and 35 s for N = 5k to 40k, and N = 200k (a 600 KB test) was still running at 100 s; 20k appends of 1,000 characters took 25 s and 1.1 GB | Core `Flows/FlowSimulator.cs:151,157,670` | `triggerOutputs()` and `workflow()` clone their whole value on every read, and the string append copies the whole string. With a 100,000-tick cap but no work cap, a small test hangs `powerops test` and the merge gate | Drop the clones that `Evaluate` already makes, and count copied bytes against the existing 256 MB budget so a run stops with a clear failure |
| R-10 | low | Observed: a 100 MB settings file was read whole; `/dev/zero` as a snapshot or settings path ran out of memory | Cli `SettingsCommand.cs:85,159`, `DeployCommand.cs:509`; Core `Output/Baseline.cs:28`, `Plans/PlanSignOff.cs:19`, `Drift/SnapshotFile.cs:43` | Some file inputs have no size cap, and the snapshot's cap reads `FileInfo.Length`, which is 0 for special files | One bounded read that counts bytes read, used at these sites, with the caps the other inputs already have (1 MB for settings and plan files, the deploy caps for zips) |
| R-8 | low | Observed: 20,000 duplicate pairs (1.4 MB) took 3.4 s, 40,000 took 12.7 s | Core `Settings/DeploymentSettings.cs:150-152` | Duplicate detection is quadratic | A case-insensitive dictionary from name to the first spelling |
| C-10a | low | Observed | Core `Flows/Expression.cs:236` | A parse error repeats the whole expression, so a 5 MB expression gives a 5 MB report line | Cut the echoed text at 200 characters |

#### M27. Errors, exit codes and refusals

| ID | Sev | Evidence | Where | What breaks | Fix |
|---|---|---|---|---|---|
| R-5 | medium | Observed (this session: a baseline holding `"\ud800"` gave "Unhandled exception: System.InvalidOperationException" and exit 1) | Core `Output/Baseline.cs:94`; Cli `AnalyzeCommand.cs:106-111` | Exit 1 means "findings" in CI, and the README says an invalid baseline exits 2 | Turn `InvalidOperationException` in `Baseline.Load` into the existing invalid-baseline error (exit 2) |
| R-6 | low | Observed: a lone surrogate in a flow literal, env-var values, a flow test or `json()`, and a control character in a canvas app, each gave exit 3 with a message that names no file | Core `Readers/SolutionLoader.cs:23`, `Output/AnalysisResult.cs:62`, `Flows/FlowDefinition.cs:133,150,158`, `Flows/FlowTest.cs:66`, `Flows/ExpressionFunctions.cs:298`, `Readers/YamlReader.cs:67`, `Drift/SnapshotFile.cs`; Cli `SettingsCommand.cs:87,161` | AGENTS.md says an error message starts with the path. A bad literal aborts the whole run, and the simulator, documented as never throwing, throws | Widen the catch filters at these points to `InvalidOperationException` and `ArgumentException`, rethrown as `InvalidDataException` with the path, as `DeployPlanFile.cs:38` already does |
| C-3 | medium | Inferred, not reproduced | Cli `DiffCommand.cs:38`, `TestCommand.cs:53,60`, `Program.cs:69` | An exception outside a command's `try` reaches System.CommandLine's default handler, which exits 1: "findings" | Turn the default handler off and map any escaped exception to exit 3 with one `error:` line |
| C-4 | medium | Observed (this session: a folder with a good solution and a truncated `corrupt.zip` gave exit 0 and no word of the zip; the zip named directly gave exit 3) | Core `Readers/SolutionDiscovery.cs:77` | A zip that can't be opened is skipped while scanning, so CI passes without reading it. A folder holding only a truncated export says "no solutions found" (exit 2) | A `*.zip` that starts with the zip signature (`PK`) and can't be opened exits 3 and names it. Any other `*.zip` (a Git LFS pointer, an empty placeholder) and a readable zip with no solution are skipped, as today. `Find` is called outside the `try` in `diff` and `test`, so this ships with C-3, which makes those exit 3 too, not 1. `SolutionDiscoveryTests.Finds_upper_case_zip_and_skips_non_solution_and_corrupt_zips` is rewritten |
| R-11 | low | Observed: `Solution.xml` and `solution.xml` both present, one silently wins | Core `Readers/ZipReader.cs:24` | Duplicate entry names, exact or differing in case, are dropped silently, so the analysis may read another file than the import | Refuse duplicates as `InvalidDataException` |
| R-13 | low | Observed: a duplicated `definition` key hid a PO006 hit in the first copy | Core `Flows/FlowDefinition.cs:51` | Duplicate JSON keys in a flow are accepted, and only the last one is analyzed | `AllowDuplicateProperties = false`, as `FlowTest` already does |
| C-6 | low | Observed: under `LC_ALL=sv_SE.UTF-8` an error said "index −1" with U+2212 | Cli `Program.cs` | The machine's culture leaks into messages, so a test on `result()` text differs between machines | One method, called by both `Main` and `Program.Run`, sets `CultureInfo.DefaultThreadCurrentCulture`, `DefaultThreadCurrentUICulture` and the current thread's culture to invariant. Setting it in `Main` alone wouldn't reach the MCP handlers or the tests, which call `Program.Run`. The test sets a Swedish culture itself and skips when that culture isn't available |
| C-10b | low | Inferred | Cli `Program.cs:14` | Setting `Console.OutputEncoding` can throw on Windows when no console is attached (an MCP server started by a GUI client) | Guard it |

M27 also adds the seeded mutation test (Automation).

#### M28. Write paths

| ID | Sev | Evidence | Where | What breaks | Fix |
|---|---|---|---|---|---|
| R-9 | medium | Observed: `baseline --out` wrote through a link; `settings generate zip --out zip` overwrote the input zip (exit 0); `plan sign-off` wrote through a linked `plan.md`; `skills install` with a linked `.claude` folder wrote outside the repo (exit 0) | Cli `AnalyzeCommand.cs:69`, `BaselineCommand.cs:31`, `DiffCommand.cs:74`, `DriftCommand.cs:143,170`, `DeployCommand.cs:105,110,196`, `SettingsCommand.cs:47`, `TestCommand.cs:96`, `SkillsCommand.cs:131`; Core `Plans/PlanSignOff.cs:81` | Every write follows links and truncates in place, so a crash mid-write leaves a cut-off plan or baseline, and a clone can point a write at a file outside it | One write helper, whose details M28's own plan settles: <ul><li>A target that is a link to a regular file is refused, and so is a link at a folder on the way for `skills install`.</li><li>A target that resolves to a device or a pipe (`/dev/null`, `/dev/stdout`) is written in place, as today.</li><li>A target that is one of the command's inputs is refused.</li><li>A regular file is written to a temp file in the same folder and moved over the target, keeping an existing file's Unix mode.</li><li>Commands still replace their own output file, as today. That includes the CLI's `deploy plan --save` (`DeployCommand.cs:104`), which overwrites; only the MCP `save` uses `FileMode.CreateNew`, and it keeps it.</li><li>The write sites are counted again after M21 merges.</li></ul> |

#### M29. Supply chain and CI

| ID | Sev | Evidence | Where | What breaks | Fix |
|---|---|---|---|---|---|
| A-2 | medium | Observed: no `packages.lock.json`, no `nuget.config` | `action.yml:55`; `Directory.Build.props` | Every consumer's job builds PowerOps from source and restores from nuget.org with no content hash check, in jobs that may hold deploy credentials. Versions are exact and nuget.org versions are immutable, so the risk is a compromised feed or cache | `RestorePackagesWithLockFile` and the lock files committed. CI and the Action's build restore in locked mode (`-p:RestoreLockedMode=true` on the build, since `dotnet build` has no `--locked-mode`). The Action's build runs in locked mode in CI on all three systems, because a lock mismatch would fail every consumer's build |
| A-3b | medium | Inferred | `.github/workflows/ci.yml` | With A-3's warnings kept as warnings (M22), nothing fails when a dependency has a known vulnerability | A CI step that fails on `dotnet list package --vulnerable --include-transitive` |
| A-5 | medium | Observed: `.github/dependabot.yml:7-10` blocks only JsonSchema.Net majors and minors | CI | No check stops a dependency, or a new version of one, whose licence isn't MIT or Apache-2.0 (the reason JsonSchema.Net stays at 8.0.5) | An offline xunit test that reads each `project.assets.json` and the nuspec of every package in it from the NuGet packages folder, and fails on a licence expression outside MIT and Apache-2.0 |
| A-12 | low | Observed | `.github/workflows/release.yml:30-53`, `ci.yml`, `skills-smoke.yml` | `release.yml` has no top-level `permissions:`, interpolates the NuGet key into `run:`, and no workflow sets `concurrency` or `timeout-minutes`; CI checkouts keep their token | Top-level `permissions: {}` in `release.yml`, the key through `env:`, a timeout on every job, `persist-credentials: false` on checkouts that don't push |
| A-14 | low | Observed: no root `.gitattributes`. Inferred: that `grep -o '\+'` fails on macOS's grep | repo root; `action/apply.sh:39` | A Windows checkout with `core.autocrlf` can give the scripts CRLF; `\+` is a GNU extension in a basic regex | `*.sh text eol=lf` in a root `.gitattributes`; `grep -oE` |
| A-11 | low | Observed: PR 34's title is not a Conventional Commit; no `npm` entry | `.github/dependabot.yml` | Dependabot PR titles break the commit convention, and the Playwright example's lock file is never updated | `commit-message.prefix: chore(deps)` for both ecosystems, and an `npm` entry for `docs/examples/playwright` |
| A-17 | low | Observed: the package is in the restore closure; that it sends anything is inferred | `tests/Directory.Build.props` (xunit.v3 pulls in `Microsoft.Testing.Extensions.Telemetry`) | Test runs in CI may send telemetry | `TESTINGPLATFORM_TELEMETRY_OPTOUT=1` in CI |

Dependabot PRs 32 (codeql-action `upload-sarif` 4.39.0 to 4.39.1) and 34 (System.CommandLine 2.0.13 to 2.0.14) were checked and are safe to merge (Observed): the new pin in 32 is the upstream commit of tag v4.39.1, 34 changes one line and both versions are MIT, and CI is green on both. The master session merges them before M29, so the lock files start from the new versions, and retitles 34 `chore(deps):` first.

#### M30. Output and Action hygiene

| ID | Sev | Evidence | Where | What breaks | Fix |
|---|---|---|---|---|---|
| S-6 | low | Inferred | Cli `SettingsCommand.cs:261-262` | `settings apply` writes `pac`'s text to stderr unprefixed, so in Actions a line starting with `::` runs as a workflow command. `deploy` already prefixes every line with `  \| ` | Use `DeployCommand.Forward` |
| A-7 | low | Observed | Cli `DiffCommand.cs:41`; `action/diff.sh`; `action.yml:79-85` | `diff` prints found folder names without `TextReport.Clean`, so a name holding a newline adds a log line; the analyze and diff steps pass stderr unfiltered; the annotation escaping (correct today) has no test | `Clean` there; filter stderr to `error:` lines as `test.sh` does; move the jq into `action/annotate.sh` with a test |
| A-8 | low | Observed | Core `Output/MarkdownReport.cs:32` | `Escape` leaves `@name` mentions, `#123` references and bare links, so names from a solution in a same-repo pull request can ping people in the PR comment | Put a zero-width space after `@` and `#`, and break `://` |
| A-9 | low | Inferred | `action/apply.sh:16` | The apply guard refuses fork `pull_request` and `pull_request_target` only; `workflow_run` and `issue_comment` pass | Allow `push`, `workflow_dispatch`, `pull_request` from the same repo and `merge_group` only |
| A-10 | low | Inferred | `action.yml:68` | A job summary over 1 MiB is dropped with an error | Cap the Markdown at 1 MiB with a last line that says how much was cut |
| S-7 | low | Inferred | Cli `PacRunner.cs:38-39` | `pac` and `az` output is read whole, with no cap; the Web API reads already stop at 16 MB | Stop and fail past 16 MB a stream |
| S-9 | low | Inferred | Cli `DriftLiveReader.cs:20,111` | On Windows `az.cmd` runs through `cmd.exe`'s parsing, where .NET's quoting isn't an escape. The only variable argument is the URL, safe only because `DriftParsers.cs:11-18` allows `[A-Za-z0-9.-]` in the host, far from the call | Check the URL against that pattern again right before the call, with a test that a host holding `&` or `"` never reaches `az` |
| R-14 | low | Observed: U+202E and U+2028 from a snapshot's environment name reached the text output | Core `Output/TextReport.cs:38` | `Clean` strips control characters but keeps bidi controls and line separators, so a name can reorder or split a log line | Also strip Unicode format characters (category Cf) and U+2028 and U+2029 |
| R-12 | low | Observed: a symlinked `Other/Customizations.xml` holding a connection reference gave exit 0 and no findings | Core `Readers/XmlUnpackReader.cs:9`, `YamlRoot.cs:12` | Links inside a solution are skipped, correctly, but silently, so a check passes with less read than the user thinks | One stderr note per skipped link inside a found solution, naming it. Links passed by the discovery walk get no note, which would be noise. The exit code doesn't change |

#### M31. Docs drift

| ID | Sev | Evidence | Where | What breaks | Fix |
|---|---|---|---|---|---|
| D-6 | medium | Observed: no workflow runs `skills-ref` | `docs/PLANNING-SKILLS.md:272` | The doc says CI validates the skills with `skills-ref validate` | Remove the claim (`PluginManifestTests` and `SkillsCommandTests` are the real checks) |
| D-7 | medium | Observed | `docs/FOUNDATION.md:35,37,74,75` | It promises turning flows on after import and a `settings diff`, and writes `deploy --plan`; `pac` can't turn a flow on, decision 12 dropped `settings diff`, and the command is `deploy plan` | Say what exists |
| D-8 | medium | Observed: no Scorecard workflow, no SBOM | `docs/FOUNDATION.md:55-60` | "Repo hygiene from day one" lists a Scorecard and a CycloneDX SBOM, which don't exist, and private vulnerability reporting, which is off (A-4) | Mark each item done or not done, after M23 |
| D-9 | medium | Observed | `AGENTS.md:30`; Cli `PlanFacts.cs:21-58`, `SettingsCommand.cs:203,293`, `DriftLiveReader.cs:93` | The rule says reads that `pac` prints in no documented format go through the Web API, but the code parses `pac org who`, `pac solution list` and `list-cloud-flows --json` | Name those reads in the rule as the exceptions that decision 15 (`pac org who` resolves `--env`) and the M3 and M7 plans chose, each with where its format was observed. The rule came in with M8, in the change that parses `pac org who`, so this fixes its wording, not its policy. The user can overrule it (Questions for your admin, decisions Q12) |
| D-10 | low | Observed | `AGENTS.md:5-10` | The layout omits `src/PowerOps.Core`, `tests/PowerOps.Core.Tests`, `skills/`, `action/` and `tests/Shared`, which every recipe uses | Add them |
| D-11 | low | Observed | `README.md:5,36,226,331`; `action.yml:21` | "Built so far" omits `test` and `mcp`; a snippet uses `actions/checkout@v7` against the README's own pinning rule; `diff`'s exit 2 for a folder with several solutions is missing; the plan header list has five of seven keys; the `category` input is undocumented | Fix each |
| D-12 | low | Inferred | `docs/rules/PO005.md:7`; `docs/rules/PO013.md` | PO005's page claims canvas apps record references only for implicitly shared connections, with no source; PO013 is an error, but its page says packing succeeds | Source the claim or mark it observed; say why PO013 is an error (the component is missing after import) |
| D-13 | low | Observed | `docs/PLANNING-SKILLS.md:5`; `docs/FOUNDATION.md:20,21,71` | Stale status: "Build starts after M2", the MCP server "later", "about 10 rules" (there are 14) | Update |
| D-14 | low | Observed | `CONTRIBUTING.md:3` | It says only Phases 0 and 1 need no tenant (every test is offline), and doesn't mention `POWEROPS_SKIP_E2E` or the tests that skip without `bash` or `jq` | Update |
| D-15 | low | Observed | `AGENTS.md:27,30` | "Writes need an explicit flag", but `baseline` writes `powerops-baseline.json` by default; "arguments as a list" is only partly true for `az.cmd` on Windows (S-9) | Name both exceptions |

M31 also records the research results below in FOUNDATION §8 and adds `DocsDriftTests` (Automation). It stays one PR: it changes only docs and one test, and the test checks the result.

#### M32. Windows and macOS

| ID | Sev | Evidence | Where | What breaks | Fix |
|---|---|---|---|---|---|
| P-1 | medium | Observed: CI runs `dotnet test` on all three systems, and `PackedToolTests` runs the packed tool and `powerops mcp` over stdio by full path there; nothing else has run on Windows or macOS | `.github/workflows/ci.yml:12-24` | Not run by anyone on Windows or macOS: the clients starting a bare `powerops` from PATH, `pac` sign-in seen from a server a client started, `az.cmd`'s argument path, and the S-1 resolver's PATHEXT order | CI: the resolver's tests, plus a fake `pac.cmd` and `az.cmd` that echo their arguments to check the exact arguments PowerOps passes, on `windows-latest`; check that `ActionScriptTests` really run under macOS's bash 3.2. A manual runbook for a person with Windows or macOS; if nobody runs it, README and FOUNDATION mark those points not verified |

### Probe shapes

The audit's probe files were in a scratch folder outside the repo. Each M25 to M27 test builds its input again from these shapes, so the tests become the record:

- R-1: flow JSON 240 levels deep, with 100-character keys and 40,000 leaf strings (185 KB); 80,000 leaves for the failing case.
- R-2: a canvas `*.meta.xml` nested 25,000 to 100,000 deep.
- R-3: an env-var default value `a@` followed by `.a` repeated 40,000 times, then `@`.
- R-4: a 46 MB file of `<a/>`; a zip with 500,000 entries; a folder with 300,000 files.
- R-5: a baseline `{"version":1,"findings":[{"solution":"\ud800","ruleId":"PO001","message":"m"}]}`.
- R-6: `"\ud800"` in a flow literal, in the env-var values JSON, in a flow test's `trigger` and in `@json('"\ud800"')`; a `\x01` in a canvas `.pa.yaml`.
- R-7: a link to `/dev/zero` in a plan folder; a link loop; a file over 1 MB.
- R-8: a settings file with 20,000 pairs of duplicate names.
- R-11: a zip holding both `Other/Solution.xml` and `Other/solution.xml`.
- R-13: a flow with two `definition` keys, the hard-coded URL in the first.
- C-2: a Foreach over 200,000 trigger items calling `length(triggerBody()?['items'])` each time; 20,000 appends of 1,000 characters to a string variable.
- C-4: a folder with a good solution and the first 3,000 bytes of a fixture zip as `corrupt.zip`.
- C-6: `@substring('abc',-1,1)` under a Swedish culture.
- S-1: an executable script named `pac` in the current folder that writes a marker file.
- S-5: `\ud800`, and `/tmp/x` + NUL + `y` + newline + `error: forged`, in an MCP string.

### The open research items (FOUNDATION §8)

Read on 2026-10-28 by the researcher, on Microsoft Learn and NuGet; sources are in the BUILD-PLAN Phase 6 research basis.

- **`pac` older than 2.13.1.** The NuGet release notes date only `--stage-and-upgrade` (1.28) and `env fetch` (1.21.13, as `org fetch`). They date none of `--settings-file`, `solution list`, `org who` or `power-automate list-cloud-flows` (preview, an admin command), so the lowest version with all of them is not documented. Learn documents reading the version from the second line of a bare `pac` run, by one example, with no promise of format. Decision: no runtime version check (decisions Q5); the item stays open.
- **Empty settings entries.** Learn never says an empty entry overrides nothing. Its Build Tools page calls the generated `""` values "missing" and says to fill them. `ImportSolutionRequest.ComponentParameters` "overwrite" values, with nothing on empty ones. So `settings validate` keeps reporting empty values and connection ids, and HANDOFF step 6 keeps "remove the entry". The item stays open for the SDK path, which needs a tenant.
- **Which skill wins.** Neither the Claude Code nor the Codex docs describe a tie-break or a way to order skills; the model chooses by description. Both cap the listing: 1,536 characters for a skill's description in Claude Code, with the least-used skills' descriptions dropped first, and 2% of the context in Codex. Nothing to build; the descriptions keep their trigger words first. The item stays open for Copilot and real routing skills.
- **Limits.** Learn gives 95 MB as the maximum solution file size, without saying compressed or not, and a 60-minute default for `--max-async-wait-time` with no maximum. `deploy apply` already waits 60 minutes.

### Checked and not a gap

- XML: DTDs are prohibited at both parse sites (`SolutionParser.cs:218`, `SolutionDiff.cs:208`); billion-laughs and external-entity probes exit 3. No `XmlDocument`, `XmlSerializer` or resolver anywhere.
- Zip-slip: entries are never written to disk; unsafe names are skipped and backslashes normalised (`ZipReader.cs:19,34`). The per-file and per-solution byte caps count bytes read, so a lying declared size doesn't help; a 1 GB bomb exits 3.
- Symlinks are skipped in every solution walk and in the flow-test walk; `deploy` refuses a linked zip or plan file.
- YAML aliases and anchors are refused and depth is capped at 200; JSON depth is capped (64, and 256 for flows).
- The expression parser caps nesting at 100; 200,000 nested calls, a 30 MB string and 5 million unclosed parentheses return in 0.2 s. The functions that could overflow (`add`, `mul`, `div`, `range`) aren't implemented and report `unsupported`; child flows need a mock, so recursion can't happen.
- About 10,600 byte mutations of the fixtures (readers) and 1,700 structural mutants and random expressions (simulator) gave no exit code outside 0 to 3 and no stack trace.
- Exit codes: every System.CommandLine parse error maps to 2 (`Program.cs:58-66`), checked for over 40 cases; usage errors leave stdout empty with `--format json`.
- MCP: the `-` and `@` refusal covers every string and list element; empty values, wrong types, missing and unknown fields are refused (`additionalProperties: false`); values after `--` are positional; UNC and device paths are refused; `--apply` appears only after every gate; the form has no default, compares the typed name ordinally and fails closed; the one-import guard holds; the plan hash covers the environment id; `save` takes a plain name and `FileMode.CreateNew`.
- Subprocesses: `UseShellExecute = false`, `ArgumentList`, stdin closed, timeouts kill the process tree, and only `PacRunner` starts processes. The Web API has a 30-second timeout, a 16 MB cap and no redirects, and parses error bodies instead of forwarding them. The `az` token goes only into the Authorization header (tested). Only `error:` lines reach an MCP result (tested).
- The Action: no `${{ }}` inside any `run:`; no `eval`; expansions are quoted; no `pull_request_target` or `workflow_run` anywhere; the annotation escaping covers `%`, `\r`, `\n`, `:` and `,`; the comment is capped at 65,000 bytes; `GH_TOKEN` is scoped to the comment step.
- Workflows: all nine distinct action SHAs match their upstream tags; `ci.yml` and `skills-smoke.yml` set `contents: read`; secret scanning and push protection are on.
- Licences: all 27 packages in the restore closure (the CLI, Core and the test projects) are MIT or Apache-2.0 by their nuspec licence expressions. The Playwright example's npm packages weren't checked.
- SARIF output is validated against the 2.1.0 schema (`SarifReportTests`), and every golden JSON against its schema (`OutputSchemaTests`).
- Docs: the README usage block, rule table and MCP section match the code; every path, class and helper the AGENTS.md recipes name exists; the Codex skill copies match `skills/`, and the Claude copy of `powerops-review-plan` differs only by the three lines `SkillsCommand.ForClaude` adds; no relative link or anchor in the docs is broken.

### Accepted (not fixed)

- **`plan status` exits 2 for a malformed header (C-8).** It is the documented contract, and the planning skills act on that exit 2.
- **Schemas without `additionalProperties: false`.** Deliberate (`docs/schemas/README.md`): additive fields keep the schema version, so a leaked extra field isn't caught by a schema. The goldens catch it for the fixtures.
- **A zip swapped between its last hash and `pac` reading it (rest of S-2, and R-10's hash-then-parse).** Closing it means importing a private copy of every zip. The window is the moment before `pac` opens the file, and the folder is the user's.
- **What the form can't show (S-10).** It shows the friendly name, which isn't unique, and the server can't tell a person from a hook that answers forms; the README warns about both. Showing the environment id would be a new field.
- **Release hardening (A-6).** An environment gate for the publish job, the attestation of the served package, an SBOM and a notices file serve NuGet publishing, which is out of scope. `release.yml` gets only A-12's permission and key fixes.
- **The SARIF upload path in CI (A-16).** Running it needs `security-events: write` in CI, for a line Dependabot updates and whose pin is checked. The Action's tests cover the inputs.
- **Text output with CRLF on Windows.** Only text output; JSON always uses `\n`, and tests trim `\r`.
- **Invalid UTF-8 replaced with U+FFFD.** That is what the decoder does for every reader; a file's meaning doesn't depend on a byte that isn't text.
- **Example workflows outside Dependabot's scan.** Dependabot reads `.github/workflows` and `action.yml`; `ExampleWorkflowTests` keeps the examples pinned, and M29's A-11 doesn't change that.

### IDs merged or dropped

The agents' IDs that have no row above:

- C-1 is merged into R-7, C-5 into R-6, D-1 into A-4, and D-4 and A-15 into I-1.
- A-13 is split: its unpinned `actions/checkout@v7` snippet is in D-11. Its claim that `upload-sarif` needs `actions: read` in private repos is dropped, because the agent gave it from memory with no source.
- A-6, A-16, C-8 and S-10 are under Accepted.
- C-10 is split into C-10a (M26) and C-10b (M27). Its CRLF point is under Accepted.
- Its claim that SARIF output has no schema test is dropped as wrong (`SarifReportTests`).
- R-14's invalid UTF-8 point is under Accepted, and its bidi point is the R-14 row.

## Automation

Each milestone is one PR, built in the order of Steps. A milestone whose rows above give the fix exactly is built from this plan. A milestone with a design choice left gets its own short plan first (`depth: quick`, signed off after a fresh review), per decision Q2: M24 (the shape of the approved-hash check, S-3's research, S-8's check), M28 (the write helper and which commands it covers) and M32 (the runbook).

Tests:

- Every fixed finding gets a test that fails without the fix. Where a fix is a limit, the test builds a small hostile input in a temp folder (`TempFolder`, `SolutionZip`) and asserts the exit code or the `InvalidDataException`, the path prefix, and a time limit generous enough for slow CI runners (10 seconds where a fix makes the input take milliseconds).
- M25, M26 and M27 collect the probes (Probe shapes) into one table-driven theory per area: about 25 hostile inputs with their expected exit codes. A limit is proven by an input just over it, not by a large one: a 1 MB cap needs a file of 1 MB and a byte, not 800 MB. Decision Q4 settles the form; no fuzzing library is added.
- M27 adds one seeded mutation test (about 60 lines, no dependency). It copies the fixtures to a temp folder, mutates them with a fixed seed (byte flips, plus structural swaps of a JSON string for `\ud800` or a control character, duplicate keys and deep nesting) and asserts that `SolutionLoader.Load` and `FlowSimulator.Run` either succeed or fail with a path-prefixed `InvalidDataException`.
- M29's licence test reads `obj/project.assets.json` of each project after restore, and each package's nuspec from the NuGet packages folder (`NUGET_PACKAGES`, or `~/.nuget/packages`).
- M31's `DocsDriftTests` (about 80 lines) extends `HandoffDocTests.Unknown` to README, AGENTS.md, CONTRIBUTING.md, PLANNING-SKILLS.md, FOUNDATION.md and `skills/*/SKILL.md`, and also checks each `--flag` in the same code span exists on that command. It also compares the README rule table with `Analyzer.Rules`, the `action.yml` input names with the README, and the README's MCP tool names with `tests/golden/mcp-tools.txt`, and fails on `@v<number>` or `v<number>.<number>.<number>` outside `docs/plans` and BUILD-PLAN until a matching tag exists. Its own unit tests feed it planted wrong text (a flag that doesn't exist, a rule row with the wrong severity, an unknown input or tool name) and expect each to fail.
- Tests that split CLI output trim `\r`; scripts stay bash 3.2 compatible.

Goldens: only M24 (the three tool descriptions in `mcp-tools.txt`) and M30 (if a Markdown golden holds an `@`) are expected to change a golden. Each is updated with the AGENTS.md recipe and its diff read.

Compatibility. These are the changes a CI user or an agent can see. Each milestone's PR lists the ones it brings in its description. No option relaxes a cap: a cap that a real input reaches is raised in a fix (Assumptions).

- M22: `@<file>` arguments are read as paths.
- M24: new MCP tool errors (control characters, the list and string caps), and `drift --env` refuses a path that doesn't exist (exit 2).
- M25: new exit-3 refusals past the node, entry, file and folder caps.
- M26: new limits on settings, baseline, plan and zip reads, and on the simulator's copied bytes (exit 3, or a failed test). `plan status` skips links, binaries and files over 1 MB, with a warning.
- M27:
  - a broken baseline exits 2, not 1;
  - an escaped exception exits 3, not 1;
  - a zip that can't be opened in a scanned folder exits 3, not 0;
  - duplicate zip entries and duplicate flow keys exit 3;
  - messages start with the path.
- M28: a write through a link, or over an input, is refused.
- M30: Markdown cells defuse `@`, `#` and `://`, and the apply step refuses more events.

## Screens

None. Only error lines and refusals change, and the three tool descriptions in M24.

## Environments and route

None. No milestone reads or writes a tenant, and every test uses fakes. S-3 is answered from Learn. M32's manual steps run offline tools only and list `pac` profiles with `pac auth list`, which reads only the local profile store. No `--apply`, no import, no `pac` or `az` call against an environment.

## Solution and publisher

None: no solution changes.

## Constraints

- AGENTS.md: offline first, read-only by default, arguments as a list, the smallest change that fully solves each finding, tests for new logic, `dotnet format` clean, Conventional Commits, no secrets or tenant data, no hand-edited versions.
- No new dependency: CsCheck (Apache-2.0) was considered and isn't needed; FsCheck is BSD-3-Clause and is ruled out. Licences of anything added later must be MIT or Apache-2.0.
- Open sources only: Microsoft Learn, the clients' docs, MIT or Apache repos and observed output. No decompiling.
- M21 (`powerops deploy gate`) is planned in another session on its own branch. Only M24 depends on it: M24 changes `deploy apply`'s import path and `deploy_apply`, which M21 may read, so M24 is built after M21 merges, or rebased on it, after re-reading M21's plan. M28 counts the write sites again after M21 merges. The other milestones don't wait for M21. When both are merged, check:
  - that the BUILD-PLAN decision numbers (30 to 38 here) and build-order items (47 to 57) don't collide with M21's, and renumber if they do;
  - that M21's new write sites and inputs are covered by M25 to M28.

  No Phase 6 milestone changes the plan-file format.
- Release PR 4 (release-please, 0.1.0) isn't merged during Phase 6 unless the user decides to publish. If it is merged, it publishes `PowerOps` and creates the first tag. Then I-1 becomes "keep the README's NuGet line", D-3's snippets can pin that tag's SHA, and the question of reserving the ID goes away.
- The master session pushes, opens the PRs and merges; repo settings (M23) change only after the user's yes.
- Deadlines: none from the platform. Phase 6 needs no tenant, so Test's expiry doesn't bind it.

## Assumptions

- The `PowerOps` ID stays unregistered on NuGet during Phase 6, since publishing is out of scope; I-1 fixes the instructions instead of reserving the ID.
- M21 takes build-order item 46 and the label v0.20.0. If its plan takes more, the Phase 6 numbers shift when both are merged.
- The master session merges Dependabot PRs 32 and 34 before M29.
- Dependabot updates NuGet lock files in its PRs. M29 checks this on its first Dependabot PR; if it doesn't, the lock files are regenerated in that PR by hand.
- No real export reads more than 50 MB in one file or 256 MB in all, given Learn's 95 MB limit for a solution file. The caps count only the XML, JSON and YAML that are read, not binaries. If a real export hits a cap, the cap is raised in a fix.
- The probes' timings come from one Linux machine; the tests' time limits allow for slower runners.

## Open questions

- (S-3) Whether Dataverse refuses a token issued for one org's URL at another org. M24's plan answers it from Learn; the S-3 row gives the fix for each answer.
- (S-8) Whether the MCP SDK's stdio host waits for a running handler when stdin closes. M24 checks it with a test.

## Questions for your admin

- Turn on private vulnerability reporting for the repo (A-4)? SECURITY.md promises it. Without a yes, M31 changes SECURITY.md to say there is no private route yet and to open an issue that asks for one, without details.
- Add a ruleset on `main` (a pull request, the six CI checks, no force push, no deletion, the owner as bypass actor for release-please's PRs) and one on `v*` tags (A-1)? Without a yes, nothing changes and FOUNDATION §4 says `main` is unprotected.
- Turn on, each on its own: Dependabot alerts; Dependabot security updates; CodeQL default setup; required SHA pinning for actions (A-18)? Without a yes, each stays off and FOUNDATION §4 says so.
- Keep AGENTS.md's rule on undocumented `pac` output, narrowed to name the reads it already allows (D-9, decisions Q12)? Without an answer, M31 narrows it as planned.
- Reserve the `PowerOps` package ID on NuGet now? This is for the user only, since publishing is out of scope, and no milestone depends on it: the docs install only from a local build until then (I-1).
- Is a Windows or a macOS machine available for M32's manual steps? Without one they are marked not verified.

## Steps

1. M22, build-order item 47 (`fix:`): S-1, I-1 (with its install-line test), D-2, C-7, A-3, D-3 and D-5, each with its check; regenerate the plugin skill copies.
2. M23, item 48 (no PR): the master session asks the user about the repo settings in "Questions for your admin", one setting at a time; with a yes, it makes that change and records the API reads in the BUILD-PLAN verification record.
3. M24, item 49 (`fix:`): its own quick plan first (Automation), then S-2, S-3, S-5, S-4, S-8, C-9 and D-15c, built after M21 merges or rebased on it.
4. M25, item 50 (`fix:`): R-1, R-2, R-3 and R-4, with the readers' hostile-input theory.
5. M26, item 51 (`fix:`): R-7, C-2, R-10, R-8 and C-10a, with the plan, settings and simulator theory.
6. M27, item 52 (`fix:`): R-5, R-6, C-3 with C-4, R-11, R-13, C-6 and C-10b, and the seeded mutation test.
7. M28, item 53 (`fix:`): its own quick plan, then R-9.
8. M29, item 54 (`ci:`): after Dependabot PRs 32 and 34 are merged, A-2, A-3b, A-5, A-12, A-14, A-11 and A-17.
9. M30, item 55 (`fix:`): S-6, A-7, A-8, A-9, A-10, S-7, S-9, R-14 and R-12.
10. M31, item 56 (`docs:`): D-6 to D-15, SECURITY.md after M23's answer, the research results in FOUNDATION §8, and `DocsDriftTests`.
11. M32, item 57 (`ci:` and `docs:`): its own quick plan, then the Windows CI checks, the runbook, and its run or the not-verified marks.
12. After each milestone, mark it built in BUILD-PLAN (row and build-order item), as Phases 3 to 5 did; after the last, set the Phase 6 status and the FOUNDATION roadmap row.
