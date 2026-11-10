plan-hash: 5e620def4bb6188272ffae97b4f91095b6de2b0ae300ba0eb7d646e7c3a7898b
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: the plan's `environment:` is null and nobody confirmed a signed-in `pac`, so `powerops plan facts` was not run. First pass, 2026-11-10, against `plan.md` and `decisions.md` as of `cbc31e7` plus the uncommitted plan folder, with the CLI built from source. Checked in the repo: `WorkflowTests.cs:52` (the `Workflows` theory list) and `:208` (the end of the pin-drift test), `Tools.Require` (skips on Windows), `release.yml`'s `issues: write`, `ci.yml`'s checkout SHA, `skills-smoke.yml`'s two pins, `global.json`, the three `PAC_VERSION: 2.14.1` lines in `docs/examples/`, `DocsDriftTests` (flags are checked only inside `powerops ...` units, so the script's flags in the new pages are safe), `reports/leakscan.sh` and the `reports/` exclusion. Tools (item 12): `bash`, `curl`, `jq`, `gh` (signed in), `flock`, `git` and `dotnet` found with `command -v`; `shellcheck` not found; the `powerops-researcher` and `fable-reviewer` agents and the `verify-research` workflow are in this session's lists. Open questions: 0; questions for your admin: 0.

Second pass, 2026-11-10: I re-read the whole revised `plan.md` (body hash `52f73bf`) and `decisions.md` (Q4 and Q6 revised, Q12 added, and a Log line answering R1 to R13). Each first-pass finding's Resolution is set against the revised plan, and R14 is new. I counted the narrowed inventory: 20 automated sources (17 of them GitHub calls) and 10 manual sources, which matches row 8, Constraints and Assumptions. I did not request the narrowed sources' new endpoints myself; the Log says each was checked on 2026-11-10.

Third pass, 2026-11-10: I re-read the whole revised `plan.md` (body hash `5e620de`) and the new Log line (Fable's re-check, three edits). R14 is set against it, and R13's note covers row 1's new URL pattern. I checked the three edits for new problems and found none. Row 3 links a path source to that path's history at the new sha, and case (a) asserts both link forms. Row 1 holds `lastSeen` to null or its kind's value pattern and a manual `target` to an `https` pattern with no `(`, `)`, backtick or space, and case (j) adds the three refusals. Each of the ten manual targets in the inventory table fits that pattern. Case (i) keeps response values inside code spans or script-built links.

## Findings

### High

No findings.

### Medium

#### R1. The repo-head sources will report "new" almost every week, so the issue rarely closes and mode A becomes a weekly review
- Area: 10. Unanswered areas (and the Goal's "light")
- Finding: Four sources watch a whole repository's head (`power-platform-skills`, `power-cat-skills`, `skills-for-copilot-studio`, `copilot-studio-plugin`), and nine watch MicrosoftDocs folders or pages. The Log shows `power-platform-skills` and `power-cat-skills` both moved on the day of planning and the `alm` folder four days before. Any of them moving makes `NEW` non-zero, so the workflow (row 6) reopens or keeps the issue open most Mondays, and closing it needs a mode A review plus an `--update` commit merged to `main` each time. Q4 and Done-when 3 present the self-closing issue as the normal state, and nobody estimated how often a week will be clean.
- Suggested fix: Either narrow the busy repository sources to the folders PowerOps cites (the `path` field already exists for `github-commits`) or use releases where a repository has them, or state in Goal and Q4 that a weekly open issue is expected and how much triage a week takes. Have step 7's Log record how many sources moved between 5c's `--update` and the final runs, as the first measure of the weekly rate.
- Resolution: resolved (2nd pass): Q12 narrows each automated source to what PowerOps cites (whole-repository heads become cited paths, the roadmap feed, Claude Code, Codex and the programmability page become manual), the four-week rate per source is in the inventory table (17 changes, about four a week), Assumptions says an issue is expected on most Mondays, row 8 adds the cheap path, and step 7 record (v) logs the first measured rate.

#### R2. `shellcheck` is not installed here, and Assumptions doesn't say so
- Area: 12. Tools
- Finding: Step 2's done-condition runs `shellcheck` "if it is installed". `command -v shellcheck` finds nothing on this machine, and Assumptions doesn't mention it. The step won't fail, but the check it describes won't run, and nothing records that.
- Suggested fix: Add an Assumption saying `shellcheck` was not found on 2026-11-10, so step 2's report states it was skipped. Or install it before step 2.
- Resolution: resolved (2nd pass): Assumptions says `shellcheck` was not found and no workflow runs it, step 2 reports it as skipped, and `Won't build` adds no CI job.

### Low

#### R3. Row 1 doesn't say which fields each kind must have
- Area: 9. Testability
- Finding: Case (j) refuses "a missing field" and "an extra field", but row 1 gives only per-field notes ("automated kinds only", "`manual` only", "optional"). It doesn't say whether a manual source must leave out `lastSeen` or hold it as null, or whether `lastSeen` is required (null) on an automated source. Steps 2 (the validator) and 4 (the README's field list) run in parallel from row 1 "as written here", so they can settle this differently.
- Suggested fix: Add a per-kind list of required and optional fields to row 1, for example: always `id`, `title`, `kind`, `target`, `areas`, `affects`, `lastChecked`; automated kinds add `lastSeen`; `github-commits` may add `path`; `m365-roadmap` adds `products`; `manual` adds `everyDays` and has no `lastSeen`.
- Resolution: resolved (2nd pass): row 1 now gives the fields of each kind, and case (j) tests a missing `lastSeen` or `everyDays` and an extra `lastSeen` or `path`.

#### R4. A roadmap value earlier than `lastSeen` fits no part of the report
- Area: 9. Testability
- Finding: Row 2 says a `m365-roadmap` value is new when it is later than `lastSeen` and unchanged when it is equal. If the newest matching item is removed or re-tagged, the greatest `modified` can go back, and the plan doesn't say where that lands. Done-when 2 says every source falls into one of four parts.
- Suggested fix: Say that an earlier value is reported as unchanged (or new), and add it to case (b) or (f).
- Resolution: resolved (2nd pass), moot for the roadmap: the `m365-roadmap` kind is gone, and row 2 makes any difference from `lastSeen` new, including an older version or another sha, which case (b) tests.

#### R5. The `pac` row holds two versions, and the test compares "the version" with `PAC_VERSION`
- Area: 9. Testability
- Finding: Row 10's `pac` row records "tested 2.13.1, the examples pin 2.14.1", but its test says the versions in the row equal the `PAC_VERSION` of the examples. It doesn't say which cell or which value the test reads, so a correct row containing 2.13.1 could fail the test, or a test could pass on the wrong value.
- Suggested fix: Put the pin (2.14.1) in `Version` and the tested 2.13.1 in `Evidence` or `Limits`, and have the test read `Version` only.
- Resolution: resolved (2nd pass): `Version` holds the 2.14.1 pin, `Evidence` the tested 2.13.1, the test reads `Version` only, and step 4 plants the wrong cell.

#### R6. Row 12's baseline rule names files that `affects` doesn't hold, and an "oldest" that needs dates
- Area: 9. Testability
- Finding: The fallback baseline is "the newest `verified:` date in the files its `affects` names". In the inventory, `affects` is prose ("every `pac` call and gate route", "deprecations across Power Platform"), not paths. The rule also takes "the oldest" of several cited commits, and two of the five `power-platform-skills` commits the repository cites are short SHAs. Telling which is oldest needs their dates from the API or a clone, and 5a doesn't budget for that.
- Suggested fix: Let 5a's table name, for each source, the files it used for the `verified:` fallback. Say "oldest by commit date, read with one GitHub call per cited commit", or take the first cited commit found by `git log -S` in the repository.
- Resolution: resolved (2nd pass): row 12 takes the oldest cited commit by date, with one GitHub call per commit (short SHAs included), finds the `verified:` files with `git grep`, and has 5a's table name where each baseline came from.

#### R7. The issue-step test runs the script under a different shell than the runner
- Area: 9. Testability
- Finding: Row 7 runs the step's `run:` text under `bash -e`. A step with no `shell:` on `ubuntu-latest` runs under `bash --noprofile --norc -eo pipefail {0}`, so a pipe whose first command fails (for example `gh issue list ... | jq ...`) passes the test but fails the real step, or the reverse.
- Suggested fix: Run the extracted text as `bash --noprofile --norc -eo pipefail <file>` in the test, or set `shell: bash` on the step and name the shell in row 6.
- Resolution: resolved (2nd pass): row 6 sets no `shell:`, row 7 asserts that and runs the extracted text with `bash --noprofile --norc -eo pipefail`.

#### R8. A manual source that falls due never opens the issue
- Area: 9. Testability (and row 6)
- Finding: The issue step acts only on `NEW` and `FAILED`. Row 3 counts manual sources under "Due by hand", which neither number includes. A week where only manual sources are due closes the issue (or opens none), so the 30-day and 90-day reads depend on some other source moving.
- Suggested fix: Either export `due=<n>` and treat it like `NEW` in the four cases, or say in row 6 and the README that due manual sources are read only during a mode A review that something else started.
- Resolution: resolved (2nd pass): `due=<n>` joins the outputs, `DUE` counts as something in all four cases, and case (k) and row 7 test it. See R14 for what clears it.

#### R9. Step 5c's check expects every source unchanged, and step 7 allows sources that moved
- Area: 9. Testability
- Finding: Step 5c's check is "a rerun of the collector shows every source it can read as unchanged", and Done-when 2 and 8 say the same. Step 7 accepts that a source may move upstream after the `--update` and only asks for it to be named. With repositories that move daily (R1), 5c's check can fail through no fault of the step.
- Suggested fix: Word 5c's check as step 7's is: every source unchanged, or named as moved upstream since the `--update`, with its new observation id.
- Resolution: resolved (2nd pass): 5c's check and Done-when 8 now match step 7: New 0, except sources named as moved upstream with their new observation id.

#### R10. Assumptions doesn't name that GitHub turns off scheduled workflows in quiet public repositories
- Area: 9. Testability (the watch's lasting run)
- Finding: The repository is public. GitHub disables a scheduled workflow after 60 days without repository activity, and then the watch reports nothing, which looks the same as "nothing new". The Goal asks for a lasting watch.
- Suggested fix: Add the assumption, and have the README say how to see that the workflow is disabled and how to turn it back on (the Actions page, or a `workflow_dispatch` run).
- Resolution: resolved (2nd pass): Assumptions names the 60-day stop, and row 8's README says how to see it (`disabled_inactivity`) and turn it back on.

#### R11. Step 7 doesn't set the plan to `done`
- Area: 5. Route (where the milestone's record ends)
- Finding: Step 7's files are `decisions.md` and `docs/BUILD-PLAN.md`. Nothing moves `plan.md`'s `status` from `signed-off` or `implementing` to `done`. M78's plan needed a follow-up commit (#135) for the same gap.
- Suggested fix: Add `plan.md` (header only, `status: done`) to step 7's files and done-condition, with `plan status` showing `done`.
- Resolution: resolved (2nd pass): step 7 sets the header's `status: done` (header only), and Done-when 8 and step 7 check `plan status` shows `done`.

#### R12. How the work reaches `main` is not a step
- Area: 5. Dev to Test to Prod route
- Finding: The workflow's only real run, the issue closing and step 7's record (ii) all depend on "the merge", and the README tells reviews to "commit on a branch". But no step says who opens the pull request, what checks it must pass, or who merges it.
- Suggested fix: Add one line to step 7 or Constraints: one PR from `feat/m84-ecosystem-watch`, CI green, merged by the person or by the session under the repository's milestone rules.
- Resolution: resolved (2nd pass): Constraints says no step pushes, opens a PR or merges; the push, the one PR (CI green) and the merge each wait for the owner's yes, and step 7 record (ii) lists them.

#### R13. The plan and decisions.md name hosts and paths
- Area: URLs, GUIDs, emails and names in the plan
- Finding: Rows 2, 3 and 8, the inventory table and the Log name public hosts and paths (`<url>`) for the registries, the GitHub API, Learn, the roadmap feed and the Fabric roadmap. None holds a tenant value, no scheme is written, and `plan status` shows no warning, but the rule reports every URL. No GUID, e-mail or person's name was found.
- Suggested fix: Keep them, since the collector's specification needs the endpoints and they are public, and say so in the Log; or move the endpoint list to `sources.json` and the README, where they will live.
- Resolution: accepted (2nd pass): the planner keeps the public hosts and paths because the collector's specification needs them, with the reason in the Log; none is a tenant value, no scheme is written, and `plan status` shows no warning. The person decides at sign-off. Third pass: row 1 now writes the manual-URL pattern with its scheme, on a line that carries the `plan-status: allow` marker, and row 8 names the roadmap's JSON feed path again. Neither holds a tenant value, so the acceptance stands.

#### R14. Mode A doesn't say to record `lastChecked` for the manual sources it reads, so `DUE` never clears
- Area: 9. Testability (the R8 answer)
- Finding: Since R8's answer, a due manual source keeps the issue open, and `DUE` clears only when that source's `lastChecked` on `main` is newer than its `everyDays`. Row 4 leaves manual sources untouched by `--update`. Only step 5c says to "set `lastChecked` of each manual source read". Row 8's mode A steps say "read the manual sources that are due" but not to record the date. A later review that follows the README leaves `DUE` above 0, and the issue stays open after that review merges.
- Suggested fix: Add to row 8's mode A steps: for each manual source read, set its `lastChecked` to today in `sources.json` by hand (the script validates the edit on its next run), in the same commit as the `--update`. Have `EcosystemDocsTests` check that the README says so.
- Resolution: resolved (3rd pass): row 8's mode A now sets `lastChecked` to today by hand for each manual source read, in the review's commit; a source that couldn't be read keeps its date, stays due and is named in the review-log line with the reason. Step 5c's done-condition requires `Due by hand: 0` on the rerun, or each still-due source named. The README-string test was not added: the plan reviewer (Fable) ruled that 5c's rerun is the check (decisions Log). That check covers the first review only; later reviews rely on the README text.

## Checklist items not applicable

This plan adds development tooling and docs to PowerOps itself. It builds no Power Platform solution and makes no tenant read or write.

1. Solution boundaries and publisher prefix: not applicable (no solution; `plan facts` not read).
2. Connection references and environment variables: not applicable. No hard-coded tenant value; the only credential is `github.token`, passed on stdin and sent only to the GitHub API.
3. Premium connectors and licences: not applicable.
4. DLP fit: not applicable (no connector is used).
5. Dev to Test to Prod route: not applicable to a solution. The code goes branch, PR and merge, each after the owner's yes (R12, resolved); step 7 sets `done` (R11, resolved).
6. Managed or unmanaged: not applicable.
7. Data volume and delegation: not applicable. With the roadmap feed gone, every read is small; the GitHub budget (17 calls a run, token in CI, spaced runs locally) is stated.
8. Security roles: not applicable to Dataverse. The workflow's permissions are least-privilege (`contents: read` at workflow level, `issues: write` on the one job) and are tested in row 7.
9. Testability: every step has a done-condition and a check; the workflow can run only after the merge, which step 7 records as the remaining action. R3 to R10 and R14 are resolved.
10. Unanswered areas: Q1 to Q12 were decided by the planner within the brief; 0 open questions, 0 admin questions. R1 (the weekly rate) is resolved by Q12 and the Assumptions line.
11. Size: no tables, lists, flows or screens. Each of the four files, the script, the workflow and the three test classes traces to a Done-when line, and `Won't build` is present and long. No finding.
12. Tools: R2, resolved. The second pass adds `gh workflow list` and `gh workflow enable` to the README only; `gh` is installed and signed in.
