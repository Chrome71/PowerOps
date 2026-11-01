plan-hash: 09926f1103a73e23c0aaf7e7856aeaf8e47358ee5ef8a17603acb4f00ed613e0
model: claude-sonnet-5-5
fresh-context: yes

The plan-hash was updated after the text fixes for R1 to R17 below, as in the M7 to M20 reviews. Reviewed hash: fc4f7fbd22cbc6ff6b7212d4f79e7c014c60542cef1fd092cee9876693410ad9.

The reviewer (powerops-reviewer, Sonnet, fresh context) could not write files, so the planning session wrote this file from its reply and added the resolutions.

The fixes changed these parts of the plan:
- Goal: the done-when per kind of fix.
- Data: the M22 to M31 rows and their place, and two new subsections, "Probe shapes" and "IDs merged or dropped".
- Automation: the theory wording, `DocsDriftTests` and the new Compatibility list.
- Constraints: M21 and release PR 4.
- Open questions, Questions for your admin and Steps.

`decisions.md` gained Q11 and Q12, and Q6 and Q8 were amended. The BUILD-PLAN Phase 6 section and decisions 30, 33 and 35 were brought in line.

The reviewer contacted no tenant and ran no `pac` or `az`. It ran offline probes with the built CLI (`analyze @<file>`, a corrupt zip in a folder, a baseline holding `\ud800`) and read-only `gh` calls.

Checklist items 1 to 8 (solution and publisher, connection references, licences, DLP, route, managed or unmanaged, volume, roles) don't apply: this is a phase plan for a CLI, with no solution and no tenant work, and the plan says so in "Solution and publisher" and "Environments and route". Items 9 and 10 drive the findings. No URL, GUID, email or person's name is in the plan folder; the BUILD-PLAN and FOUNDATION additions hold only public documentation links. Open questions at review: 7 (2 technical, 5 for the admin); after the fixes, 8 (one admin question added by R6).

## Findings

### High

No findings.

### Medium

#### R1. C-4 in M25 makes `diff` and `test` exit 1, and C-3 only comes in M27
- Area: 9 (done-when checkable) and ordering
- Finding: once C-4 throws for a zip that can't be opened, `DiffCommand.cs:38` and `TestCommand.cs:60` call `Find` outside their `try`, so the exception reaches System.CommandLine's default handler and exits 1 ("findings"), not the promised 3. The existing test `SolutionDiscoveryTests.Finds_upper_case_zip_and_skips_non_solution_and_corrupt_zips` asserts the skip and isn't named. Any `*.zip` that isn't a zip (a Git LFS pointer, an empty placeholder) would fail every run.
- Suggested fix: ship C-4 with C-3, add a done-when for `diff` and `test`, name the test to rewrite, and count only files with the zip signature.
- Resolution: fixed. C-4, R-11 and R-13 moved to M27 with C-3. Only a `*.zip` starting with `PK` exits 3, and LFS pointers and placeholders are still skipped. The test to rewrite is named, and M27's done-when covers `analyze`, `diff` and `test` (decisions Q6, BUILD-PLAN decision 33).

#### R2. The plan's Done-when promises a failing-before test for fixes that are CI, settings and docs changes
- Area: 9
- Finding: settings (A-4, A-1, A-18), CI (A-2, A-3, A-5, P-1) and docs fixes (I-1, D-3, D-5 to D-9) have no test that fails before the fix; `DocsDriftTests` comes only in M31.
- Suggested fix: name the check per kind of fix, and give I-1 a test in M22.
- Resolution: fixed. Goal now names the check per kind of fix: a test for code; the API reads in the verification record for settings; a passing run or a test over the workflow file for CI; the named test for docs. M22 gets a test that fails on a `dotnet tool install` of `PowerOps` without `--source`.

#### R3. I-1's fix leaves two README lines that install `PowerOps` by name from nuget.org, and its done-when contradicts them
- Area: 9
- Finding: `README.md:10` and `:25` install by name, so the done-when can't be met. Release PR 4 would change I-1 and D-3 if merged.
- Suggested fix: say what those lines become, narrow the done-when, and add a constraint on PR 4.
- Resolution: fixed. The NuGet line goes until the first release, and the Quick start builds from a clone. The done-when is the install-line test. Constraints say PR 4 isn't merged during Phase 6 unless the user decides to publish, and what changes if it is.

#### R4. BUILD-PLAN says "Planned and signed off", but the plan header has `status: approved`, `signed-off: null`
- Area: 9 and consistency
- Finding: the Status line claimed a sign-off that hadn't happened.
- Suggested fix: write the true state, and update it after sign-off.
- Resolution: fixed. The line reads "Planned, reviewed and signed off". It is committed only together with the sign-off that makes it true, after this review's findings are resolved.

#### R5. M28's write helper (decision 35) breaks device outputs and misdescribes `deploy plan --save`
- Area: 9 and scope
- Finding: temp-and-move breaks `--out /dev/null` and `/dev/stdout`, changes mode, owner and links, and needs folder write access. The CLI's `--save` (`DeployCommand.cs:104`) overwrites; only the MCP `save` uses `FileMode.CreateNew`.
- Suggested fix: write devices and pipes in place, say what is preserved, correct `--save`, and re-count the sites after M21.
- Resolution: fixed.
  - A target that resolves to a device or a pipe is written in place, and the move keeps an existing file's Unix mode.
  - The CLI's `--save` keeps overwriting, and only the MCP `save` refuses an existing file.
  - M28 counts the sites again after M21 merges, and its done-when checks `/dev/null` and `/dev/stdout`.
  - M28's own plan settles the rest (owner, links), as before (decisions Q8, BUILD-PLAN decision 35).

#### R6. D-9 narrows an AGENTS.md rule to fit the code, with no recorded decision
- Area: 10 and scope
- Finding: editing the rule is a policy choice that no question covered.
- Suggested fix: ask it, or record it as a decision the user can overrule.
- Resolution: fixed. Q12 records it. The rule came in with M8 (`f428e87`) in the change that parses `pac org who`, and `plan facts` parsed `pac` text from M3 on, so the edit fixes the wording, not the policy. It is also under "Questions for your admin", with the default stated.

#### R7. S-3's fallback fix is a behaviour change, a new feature surface, and another undocumented `pac` parse
- Area: 9 and scope
- Finding: narrowing the tools to environments that `pac auth list` shows would change documented inputs and parse undocumented text, and "not documented" is the likely answer.
- Suggested fix: decide the fallback now; "not documented" keeps the suffix check and documents the residual.
- Resolution: fixed. Q11: if Learn says the token is refused elsewhere, or says nothing, the suffix check stays and the residual goes under Accepted and in the README. Only if Learn says such a token works elsewhere does the master session ask the user. No runtime `pac auth list` call.

#### R8. A-1's ruleset with required CI checks would block release-please pull requests
- Area: 10
- Finding: release-please opens its PRs with `GITHUB_TOKEN`, which starts no CI, so required checks block them; the check names may change; A-18 bundles four settings.
- Suggested fix: say how release PRs merge, list the check names, revisit after M32, and ask the four settings one by one.
- Resolution: fixed. The ruleset lists the six check names and makes the owner a bypass actor for release-please's PRs, and it is revisited after M29 and M32. A-18's four settings are asked one at a time (plan, BUILD-PLAN M23).

#### R9. C-6's fix in `Main` does not reach `Program.Run`, tests or MCP handlers
- Area: 9
- Finding: setting the culture in `Main` changes only the main thread, and a Swedish-culture test needs ICU data.
- Suggested fix: set the default thread cultures in one place that `Main` and `Run` both call, and skip the test without the culture.
- Resolution: fixed. One method, called by both, sets `DefaultThreadCurrentCulture`, `DefaultThreadCurrentUICulture` and the current thread's culture. The test sets the culture itself and skips where it isn't available.

#### R10. R-7's "report files over 1 MB" would flag legitimate plan assets
- Area: 9
- Finding: a mockup image over 1 MB would fail a valid plan.
- Suggested fix: skip binary and large files, and add a done-when with a 2 MB image.
- Resolution: fixed. Binary files and files over 1 MB are skipped with a warning, and text files are scanned as today. M26's done-when includes a plan folder with a 2 MB image.

#### R11. M25's caps are not all specified, and its size makes it hard to build as one PR
- Area: 9
- Finding: three of R-4's caps had no number. M25, M30, M31 and M24 looked large for one PR.
- Suggested fix: give each cap a number and a reason, and split M25 and M31, or say why not.
- Resolution: fixed.
  - The caps: 5 million nodes; 100,000 zip entries, files in a solution folder and folders walked by discovery. Each has a reason: the probes' cost per entry and Learn's 95 MB limit.
  - M25 is now R-1 to R-4 only: C-4, R-11 and R-13 went to M27 (R1), and R-12 went to M30.
  - M31 stays one PR, because it changes only docs and one test, and the plan says so.
  - M24 gets its own plan. M30's items are each a few lines.
  - No renumbering was needed.

### Low

#### R12. Evidence behind the findings lives outside the repo, and some finding IDs are missing
- Area: 10
- Finding: the reports are outside the repo, and C-1, C-5, A-13, A-15, D-1 and D-4 appear nowhere.
- Suggested fix: list the merged or dropped IDs, or add the probe inputs.
- Resolution: fixed. "Probe shapes" gives each probe's input, and the tests become the record. "IDs merged or dropped" accounts for every missing ID.

#### R13. Open questions and unasked areas
- Area: 10
- Finding: only A-4 had a default if declined. The effect of the new refusals on CI users, and a flag to relax caps, were never asked. The NuGet question touches an out-of-scope area.
- Suggested fix: a default per admin question, a compatibility note per milestone, and say no milestone depends on NuGet.
- Resolution: fixed.
  - Every admin question states what happens without a yes.
  - The Compatibility list under Automation names each visible change by milestone, and each PR lists its own. No option relaxes a cap; a cap a real input reaches is raised in a fix.
  - The NuGet question is marked for the user only, with no milestone depending on it.

#### R14. Wrong line references in the plan
- Area: 9
- Finding: R-8, S-6 and D-12 cited the wrong lines.
- Suggested fix: correct them.
- Resolution: fixed. The references are now `DeploymentSettings.cs:150-152`, `SettingsCommand.cs:261-262` and `docs/rules/PO005.md:7`, each checked in the code.

#### R15. BUILD-PLAN housekeeping and coordination with M21
- Area: 9 and consistency
- Finding: the title still said v0.19, the decision and item numbers could collide with M21's, M21 is absent from BUILD-PLAN, and the plan didn't say which milestones wait for M21.
- Suggested fix: update the title, note that decisions shift too, state the dependencies and the merge-time checks.
- Resolution: fixed. The title says "v0.2 to v0.20". The build-order note and Constraints say items and decisions 30 to 38 shift if M21's do, and list the checks at merge. Only M24 waits for M21; M28 re-counts the write sites after it; the others don't wait. That M21 has no BUILD-PLAN section yet is accepted: its own session adds it.

#### R16. Cheap user-facing fixes sit late in the order
- Area: 9
- Finding: D-3, D-5 and A-3 are cheap and user-facing but sat in M29 and M31.
- Suggested fix: move them into M22, or say why not.
- Resolution: fixed. A-3 (the four warnings, with a test over `Directory.Build.props`), D-3 and D-5 moved into M22. The CI vulnerable-package step stays in M29 as A-3b.

#### R17. Mechanics and small corrections in a few rows
- Area: 9
- Finding:
  - A-2: `dotnet build` has no `--locked-mode`.
  - A-4: rated high against the plan's own definition.
  - A-14: the `\+` failure was labelled observed.
  - S-4: "could not start" touches `PacResult` and a golden.
  - D-3: a full SHA in a README can't be its own commit's.
  - R-12: a note per link would be noisy.
  - M26's 800 MB test is unneeded.
  - M31's "checked once by hand" could be a unit test.
- Suggested fix: apply each in its row.
- Resolution: fixed.
  - A-2 uses `-p:RestoreLockedMode=true`, and the Action's build runs in locked mode on all three systems in CI.
  - A-4 is medium.
  - A-14 splits its evidence into observed and inferred.
  - S-4 drops "could not start", since the string cap keeps `target` from reaching E2BIG, so no golden changes.
  - D-3 pins an already merged commit's SHA.
  - R-12 notes only links inside a found solution.
  - Limits are proven by an input one past the cap.
  - `DocsDriftTests` has unit tests over planted wrong text.
