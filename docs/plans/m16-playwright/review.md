plan-hash: 6403382323c0941e34edf9085a6b8a691cec303b3a64b6fc89ae7572edcfc00b
model: claude-sonnet-5-5
fresh-context: yes

The plan-hash was updated after the text fixes for R1 to R14 below (the practice of the M7 to M15 reviews). Reviewed hash: 8acdddc8b42e29989f3c207fb6f37d924e14e4556191dd5b308ce71618e8ec3b. The reviewer (powerops-reviewer, Sonnet, fresh context) could not write files, so the planning session wrote this file from its reply and added the resolutions. The fixes changed Goal (done-when), Data, Automation, Environments and route, Constraints, Assumptions and Steps 2 and 6; no decision changed.

## Findings

Checklist items 1, 2, 4, 6 and 7 barely apply: the starter adds no solution, connector, environment variable or query, and reads through the apps' own data sources. The findings are about testability (item 9), the route of the live check (item 5), licences and roles of the person who signs in (items 3 and 8), and unanswered areas (item 10).

### High

No findings.

### Medium

#### R1. One storage state for both apps has no fallback
- Area: 9 (testability) and 5 (route)
- Finding: The plan assumes one `.auth/state.json`, saved from one `playwright open` window that visited both apps, covers both. The samples keep one state per domain and Learn's authentication guide has a separate step for the model-driven domain. The plan says the live check shows whether it holds, but not what happens if it does not, and does not say it departs from the samples.
- Suggested fix: Decide the fallback now: two state files and `test.use({ storageState })` per test, with two `playwright open` runs; note that the samples keep two states.
- Resolution: fixed (Environments and route step 6 is the fallback: `.auth/canvas.json` and `.auth/model-driven.json`, `test.use` in a `test.describe` per test, README sign-in and Credit changed to match, committed as a fix, and the live check runs again; Assumptions says the samples keep one state per domain)

#### R2. The canvas test can fail or pass for the wrong reason, and the negative run only covers the model-driven test
- Area: 9
- Finding: The bare `[data-control-name="Gallery1"]` locator could hit strict mode; the gallery's size comes from the control default, unverified; container visibility passes without data; and the negative run changes only "Visit Date".
- Suggested fix: Choose between a gallery item and the container with `.first()`, and add a second negative run that changes the gallery name.
- Resolution: fixed (the container with `.first()`, because Test may hold no Work Items row; the test and plan say it checks that the app drew its screen, not its data, and a comment points to the samples' gallery-item signal; Assumptions states the default-size judgement; Goal and Environments and route step 5 add the gallery-name negative run with only the canvas test failing)

#### R3. Licence and privileges of the signing-in person are not addressed
- Area: 3 (licence) and 8 (security roles)
- Finding: Both apps are Dataverse-backed, so the person needs a Power Apps licence (the maintainer's Plan 2 trial end date is unchecked), the apps shared with them, Read on the tables and Create on `pops_sitevisit` for the new-record form. The plan names none of it.
- Suggested fix: One line each in Constraints and the README; a licence check before the live run, recorded as "not run, licence" if it fails.
- Resolution: fixed (Constraints and the README's Setup bullet name the licence, sharing and roles; Environments and route step 3 stops on a licence prompt or missing role and records "not run" with the reason)

#### R4. The saved sign-in is gitignored only inside the example folder, and its path depends on the working directory
- Area: 10 and FOUNDATION section 4
- Finding: `--save-storage=.auth/state.json` writes relative to the current folder, and the root `.gitignore` has no `.auth/`, so a run from the repository root would leave a credential that Git offers to commit.
- Suggested fix: Add `.auth/` to the root `.gitignore` or make the command independent of the folder; check `git status --porcelain` after the live run.
- Resolution: fixed (the root `.gitignore` gets `.auth/`; the README and the live check run the command from the example folder; Goal and step 7 check `git status --porcelain`)

#### R5. Done-when has no check that the new files hold no tenant value
- Area: 2 and the rule against committing tenant URLs
- Finding: Nothing checks the content of the new files for URLs, GUIDs or emails, and a lockfile made through a private mirror would carry its host.
- Suggested fix: A search of the new files and the plan folder for `https?://`, GUIDs and emails, and a check that every `resolved` value starts with the public npm registry; repeat it in the BUILD-PLAN step.
- Resolution: fixed (Goal has the search; Steps 2 and 6 run it)

### Low

#### R6. Which view and which form the model-driven test sees is not pinned
- Area: 9
- Finding: Without `viewid` the grid may open a personal default view, and the table has more than one form; whether "Visit" is the text box's accessible name in each is unverified.
- Suggested fix: Say the grid test needs the default view, or pass a `viewid`; word the form claim as a form with the Visit field; record which form opened.
- Resolution: fixed (the plan and README say the test reads the default view and a personal default view can differ; the form claim now says every form of the table has Visit; the live check records which form opened; no `viewid` constant, since it would be a tenant id in a committed file or a third variable)

#### R7. The `fsevents` claim is wrong for the pinned version
- Area: licences and 9
- Finding: Playwright 1.64.1 has no `fsevents` dependency; `npm ls --all` lists six packages.
- Suggested fix: Drop it from Goal and Constraints.
- Resolution: fixed

#### R8. The README's sign-in command works only in bash
- Area: 9
- Finding: `"$CANVAS_APP_URL"` does not expand in PowerShell.
- Suggested fix: Give the PowerShell form, or have the person paste the link into the window.
- Resolution: fixed (the README gives `"$env:CANVAS_APP_URL"` beside the bash form)

#### R9. An expired sign-in fails slowly with a generic message; timeouts have no margin
- Area: 9
- Finding: An expired sign-in fails after the 60 s wait with a generic locator error, and the model-driven test's two 60 s waits fill the 120 s test timeout.
- Suggested fix: Optionally detect the sign-in host after each navigation; raise the test timeout to 180 s.
- Resolution: fixed in part (the test timeout is 180 s; the README says that a test waiting on the sign-in page and timing out means signing in again); accepted for the sign-in host check, which would add code that names Microsoft's sign-in host to a two-test starter, while Playwright's error already shows the page the test was on

#### R10. Failure output can contain tenant data and the plan understates it
- Area: 10
- Finding: Playwright writes an error context with a page snapshot into `test-results/` on a failure, and failure messages print the URL a test opened; nothing tells a copier not to paste them into an issue.
- Suggested fix: One README line under Never commit.
- Resolution: fixed (Data and the README's Never commit section)

#### R11. Two done-when items cannot be checked by the build as written
- Area: 9
- Finding: The CI pass depends on a push this session does not do, and the CI job does not check the number of tests.
- Suggested fix: Mark the CI item pending the push; drop "exactly" from the CI claim or assert the count.
- Resolution: fixed (Goal marks the CI pass as pending the master session's push, green on the PR; the count of two tests is a local check, and the Automation section says the job does not check it)

#### R12. The live-check mechanics leave a few things unstated
- Area: 5
- Finding: How the links are read, the Chromium download in the ask, the links in the transcript, and `git restore` before the file is committed.
- Suggested fix: Say the user supplies the links; name the download in the ask; restore only after Step 2's commit; say the transcript holds the links.
- Resolution: fixed (Environments and route: the links come from the user in chat; the ask comes after Step 2's commit and names Test, the download and the user's own sign-in; the transcript is named)

#### R13. Dependency upkeep and browser licences are not stated
- Area: 10
- Finding: Dependabot has no npm entry for the example, and the downloaded Chromium is under other licences.
- Suggested fix: One line each in Constraints.
- Resolution: fixed (Constraints: no Dependabot entry, re-pinned when copied, the lockfile still read by dependency alerts; the browser is the user's download, not a repository dependency, never committed)

#### R14. The "Web link" source is attributed to Learn but is not on the pages read
- Area: 10
- Finding: Neither Learn page nor the samples name the Details "Web link"; the samples say to copy the play URL from the browser.
- Suggested fix: Cite it, or word it as "the play link (Details, Web link, or the address bar when the app is playing)".
- Resolution: fixed (Data and the README give both: the Details "Web link", and the address bar while the app plays, as the samples' `.env.example` says)
