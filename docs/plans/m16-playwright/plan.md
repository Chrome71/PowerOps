---
status: implementing
review: done
signed-off: 2026-10-27
signed-off-hash: 6403382323c0941e34edf9085a6b8a691cec303b3a64b6fc89ae7572edcfc00b
environment: null
depth: quick
supersedes: null
---

# M16: Playwright starter

## Goal

Build BUILD-PLAN milestone M16 (build-order item 35; docs only, no release): `docs/examples/playwright/`, a small TypeScript Playwright project that checks the seed canvas app and the M12 model-driven app in a browser, run by a person after `powerops deploy apply` to Test and before any further promotion. It follows microsoft/power-platform-playwright-samples (MIT, commit `0b41791`), keeps no secret anywhere, and adds no `powerops` code.

Done when:

- `docs/examples/playwright/` holds exactly `package.json`, `package-lock.json`, `tsconfig.json`, `playwright.config.ts`, `tests/apps.spec.ts`, `.gitignore` and `README.md`. No `node_modules`, storage state, report or trace is committed.
- In that folder, with no environment variable set, no browser installed and no sign-in: `npm ci --ignore-scripts`, `npx tsc --noEmit` and `npx playwright test --list` pass, and `--list` shows exactly two tests, one per app type (a local check).
- `npm ls --all` lists exactly `@playwright/test`, `playwright`, `playwright-core`, `typescript`, `@types/node` and `undici-types`, and each installed `package.json` says Apache-2.0 or MIT.
- No tenant value in the new files: in the example folder, the `docs/examples/README.md` section, the `ci.yml` job and this plan folder, a search for `https?://` finds only the public links of the README (Playwright, the samples repository, Learn) and the lockfile's `resolved` values, which all start with the public npm registry; a search for GUIDs and for email addresses finds nothing.
- `.github/workflows/ci.yml` has a job `playwright-example` (ubuntu, no secret, no browser install) that runs those three commands. That it passes in CI is pending the master session's push: the job is green on the PR.
- `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` and `powerops plan status` stay clean. No repo test lists the examples folder: `ExampleWorkflowTests` and `EphemeralScriptTests` read named files only, so the new folder needs no test change.
- Live, once, in Test only, after the user's yes in this session (Environments and route): the user signs in themselves in the headed browser, both tests pass, a run with the grid label changed exits 1 with only the model-driven test failed, and a run with the gallery name changed exits 1 with only the canvas test failed. Afterwards `git status --porcelain` shows no `.auth`. Recorded in `decisions.md` (Log) and the BUILD-PLAN verification record with no tenant URL, environment id, app id, email or user name. Without the yes, or once Test is gone, both say "not run" and why.

## Users

- Makers and testers whose solution `powerops deploy apply` has just imported into Test, who want a browser check of the apps before promoting further.
- People who copy the starter for their own apps and change its control names, labels and table name.

## Data

- **Inputs**: two environment variables, read inside each test, never at load time, so the type check and `--list` need neither. A missing one fails that test with a message that names it.
  - `CANVAS_APP_URL`: the seed canvas app's play link (in the maker portal the app's Details show it as "Web link"; the address bar while the app plays shows it too, as the samples' `.env.example` says). It holds the environment id, app id and tenant id. Same name as the samples' option B.
  - `MODEL_DRIVEN_APP_URL`: the M12 app's link as the browser shows it when the app is open. It holds the environment URL and the `appid` parameter. Same name as the samples.
- **Storage state**: `.auth/state.json` in the example folder, written by `npx playwright open --save-storage` when the person closes the window, read by the config's `use.storageState`. It holds the person's cookies and local storage for the sign-in, canvas and environment domains, so it is a credential: gitignored, never shared or uploaded, deleted when no longer needed. The README says so. `--save-storage` writes relative to the current folder, so the README runs it from the example folder, and the root `.gitignore` also gets `.auth/`, so a file written from the repository root is not offered for a commit either.
- **Output**: Playwright's `test-results/` (on a failure it writes an error context with a page snapshot) and `playwright-report/` (only with the HTML reporter) can show tenant data, and failure messages print the URL a test opened. Both folders are gitignored; the repo's root `.gitignore` already has `node_modules/` and `test-results/`, and the example's own `.gitignore` repeats them with `.auth/` and `playwright-report/`, so a copied folder stays safe. The README says not to paste failure output into a public issue.
- No new file format, no schema, nothing in `powerops`.

## Automation

**`docs/examples/playwright/`**:

- `package.json`: `private: true`, `license: Apache-2.0` (the repository's), no scripts (the README uses `npx`), and exact `devDependencies`: `@playwright/test` 1.64.1 (released 2026-10-14), `typescript` 6.0.3 (the last release with no dependencies; 7.x pulls in a platform package per OS) and `@types/node` 24.19.2 (CI's Node 24). `package-lock.json` from `npm install`.
- `tsconfig.json`: `target` es2022, `module` nodenext, `strict`, `noEmit`, `types: ["node"]`, including the config and `tests/`.
- `playwright.config.ts`: `testDir: 'tests'`, test timeout 180 s (the model-driven test has two waits of up to 60 s plus two page loads), `expect` timeout 60 s (the migration guide's 60 s for Dataverse galleries), `use.storageState` = `.auth/state.json` next to the config (`path.join(__dirname, ...)`, so the run works from any folder). Chromium, Playwright's defaults otherwise: one worker runs one file's tests in order, no retries.
- `tests/apps.spec.ts`: an `env(name)` helper that throws "Set <name> first (see README.md)" when it is empty, the expected names as constants at the top (so the negative live check changes one), and two tests:
  1. `canvas app shows its work items gallery`: `page.goto(CANVAS_APP_URL)`, then `page.frameLocator('iframe[name="fullscreen-app-host"]')` as the samples' canvas fixture does, and `expect(...locator('[data-control-name="Gallery1"]').first()).toBeVisible()` (`.first()`, so a name the player repeats can't break strict mode). The seed app's Screen1 has `Gallery1` over the Work Items table (`tests/fixtures/powerops-seed-yaml/v1`, Screen1.pa.yaml). It checks that the app loaded and drew its screen, not its data: Test may hold no Work Items row, and the samples' ready signal, a gallery item (`[data-control-part="gallery-item"]`), needs one. A comment in the test says to use an item for an app whose data is known.
  2. `model-driven app shows its grid and form`: from `MODEL_DRIVEN_APP_URL` take the origin and `appid` (a link without `appid` fails with a message). Grid: `main.aspx?appid=<appid>&pagetype=entitylist&etn=pops_sitevisit`, the URL the samples' `navigateToGridView` builds, which opens the table's default view, then the column header "Visit Date" is visible (`getByRole('columnheader')`). Form: `pagetype=entityrecord&etn=pops_sitevisit`, a new-record form as `navigateToFormView` opens it, then the text box "Visit" (exact name) is visible. The labels come from the M12 solution: the default public view "Active Site Visits" shows Visit, Visit Date and Visit Status (the quick find, advanced find, associated and lookup views show only Visit and Created On); every form of the table has the Visit field, and the main form "Site Visit" also has Visit Date, Visit Status and Notes. The live check records which form opened. Nothing is saved: neither test writes to the environment.
- `.gitignore`: `node_modules/`, `.auth/`, `test-results/`, `playwright-report/`. The root `.gitignore` gets `.auth/` too (Data).
- `README.md`, sections in this order:
  - What it is: a starter, not PowerOps code, two read-only tests for the seed apps; copy it and change the names for your apps.
  - Where it runs in the route: after `powerops deploy apply` to Test and before any further promotion (the hand-off page's step 8, the promotion example's apply job); against Test only, because a test you add that clicks Save writes there.
  - Setup: Node.js 20 or later (CI uses 24), `npm ci`, `npx playwright install chromium` (it downloads Chromium, which is not a repository dependency). The account that signs in needs a Power Apps licence that covers Dataverse apps, both apps shared with it, and a security role with Read on both tables and Create on `pops_sitevisit` (the form test opens a new-record form; nothing is saved).
  - Sign in once: set the two variables (bash `export`, PowerShell `$env:`), then, from the example folder, run `npx playwright open --save-storage=.auth/state.json "$CANVAS_APP_URL"` (bash) or `"$env:CANVAS_APP_URL"` (PowerShell), sign in yourself in that window, wait for the gallery, paste the model-driven link into the same window's address bar, wait for the grid, then close the window; check that `.auth/state.json` exists (Playwright ignores a failed save). When the sign-in expires, a test waits on the sign-in page and times out: sign in again.
  - Run: `npx playwright test` (exit 0 all passed, 1 a test failed); `--headed` to watch; a missing variable or storage state fails with its name. The grid test reads the table's default view; if you set a personal default view, the column may differ.
  - Why no CI job runs the tests: the samples sign in with a password or a certificate file (`playwright-ms-auth`), either of which would be a stored secret in CI, which FOUNDATION §4 rules out; the saved storage state is a person's credential and stays on their machine. CI only type-checks and lists the tests (`playwright-example`).
  - What is left out of the samples and why: `playwright-ms-auth` (password and certificate sign-in, above), `@axe-core/playwright` (MPL-2.0; the repo takes MIT and Apache-2.0 only), the samples' toolkit (a Rush workspace package, not on npm), `dotenv` (environment variables are set in the shell).
  - Credit: microsoft/power-platform-playwright-samples, MIT, Copyright (c) Microsoft Corporation, commit `0b41791`; the frame selector and the model-driven URLs follow it, no file is copied. Microsoft's migration guide from Test Engine (deprecated; its repository is removed on 2026-11-01) points to those samples.
  - Never commit: `.auth/`, the two links, `test-results/` and `playwright-report/`; and don't paste failure output into a public issue, since it holds your environment's address and data.
- `docs/examples/README.md`: a short section "Browser tests after the apply" with one paragraph: after the `apply` job has imported into Test, run the Playwright starter (link) against Test before promoting further; it needs a person to sign in, so it is not part of the workflow.

**CI** (a `ci:` commit): a job `playwright-example` in `.github/workflows/ci.yml`, `runs-on: ubuntu-latest`, `defaults.run.working-directory: docs/examples/playwright`, steps: checkout (the pinned SHA the other jobs use), `actions/setup-node` pinned to the SHA of v7.1.0 (released 2026-10-08) with `node-version: 24`, then `npm ci --ignore-scripts`, `npx tsc --noEmit`, `npx playwright test --list`. No secret, no browser, no environment variable. The job name is unique across the repo's workflows. It does not check the number of tests; that is the local check under Goal.

## Screens

None. The apps are the seed's and M12's, unchanged.

## Environments and route

- The build: offline. CI runs the type check and the list on ubuntu.
- The live check: Test only, after the user's yes in this session (Q3), asked after Step 2's commit. The ask names Test, the Chromium download (`npx playwright install chromium`, from Playwright's download host) and that the user signs in themselves. Test still holds `PowerOpsSeed` 1.0.0.1 and `PowerOpsM12Visits` 1.0.0.0, managed, from the M10 and M12 runs; nothing is imported or changed for this check.
  1. The user gives the two links in chat. They go only into the environment of the commands this session runs, never into a file in the repo or the notes folder; they stay in the session transcript, which is not committed.
  2. `npx playwright install chromium` in the example folder.
  3. This session starts `npx playwright open --save-storage=.auth/state.json <canvas link>` from the example folder, in a headed window on the user's machine. The user signs in and does the steps of the README's sign-in section; this session never types a credential, code or MFA answer. A licence prompt or a missing role instead of the app stops the check, recorded as "not run" with the reason.
  4. `npx playwright test`: both tests pass, exit 0.
  5. The `gridColumn` constant is changed to a label the view does not have: `npx playwright test` exits 1 with the model-driven test failed and the canvas test passed. Then the `canvasGallery` constant is changed to a name the app does not have: exit 1 with the canvas test failed and the model-driven test passed. After each run the committed file is restored with `git restore`.
  6. If the model-driven test lands on a sign-in page or an error with the single saved state while the canvas test passes, the starter switches to the samples' layout, one saved state per domain: two `playwright open` runs that save `.auth/canvas.json` and `.auth/model-driven.json`, and `test.use({ storageState })` in a `test.describe` per test, with the README's sign-in section and Credit changed to match. That change is committed as a fix and the live check runs again from step 3.
  7. Afterwards: `.auth/` is deleted, `test-results/` removed, and `git status --porcelain` shows neither. The result (pass or fail per run, exit codes, which form opened, Playwright 1.64.1, Chromium, the date) goes into `decisions.md` (Log) and the BUILD-PLAN verification record, with no URL, id, email or name.
- Test expires about 2026-11-21 (one extension to about 2026-12-21). If the live check misses it, it is recorded as not run (decision 22, option C).

## Solution and publisher

None new. The tests use, as installed in Test: `PowerOpsSeed` (canvas app "Work Items", `pops_workitems_2af60`, Screen1's `Gallery1`) and `PowerOpsM12Visits` (model-driven app "Site Visits", table `pops_sitevisit`, its default view and main form). Prefix `pops`. The M12 labels were read on 2026-10-27 from the M12 export that is kept outside Git.

## Constraints

- Licensing: dependencies Apache-2.0 or MIT only. Checked on 2026-10-27 in the installed `package.json` files: `@playwright/test`, `playwright`, `playwright-core` 1.64.1 Apache-2.0; `typescript` 6.0.3 Apache-2.0; `@types/node` 24.19.2 MIT; `undici-types` 7.24.6 MIT. The samples are MIT; the starter follows their patterns and copies no file. The browser that `npx playwright install` downloads is the user's download, not a repository dependency, and is never committed.
- The person who signs in needs a Power Apps licence that covers Dataverse apps (theirs; CI is not involved), both apps shared with them, and Read on both tables and Create on `pops_sitevisit`. The maintainer's licence is a Power Apps Plan 2 trial whose end date nobody has checked (BUILD-PLAN Phase 4); a lapsed licence stops the live check (Environments and route, step 3).
- No Dependabot entry for the example's npm packages: it is a starter with exact pins, re-pinned when copied. GitHub's dependency alerts still read its lockfile.
- No secret: no password, certificate, token or client secret in any file, CI job or variable the starter reads. The storage state is a person's credential that stays local and gitignored (FOUNDATION §4).
- The live tests run against Test only, and only read.
- Deadline: Test expires about 2026-11-21.
- Sources: Learn "Migrate from Power Apps Test Engine" (data-control-name selectors, the `fullscreen-app-host` frame, 60 s gallery waits, `CANVAS_APP_URL`) and the playwright-samples "Authentication guide" (password, certificate or Key Vault sign-in; a separate step for the model-driven domain), read 2026-10-27; the samples repository at `0b41791` (`packages/e2e-tests` canvas fixture, `playwright.config.ts`, `.env.example`; the toolkit's `navigateToGridView`, `navigateToFormView` and grid locators); Playwright 1.64.1's `open --help` and source (`--save-storage` writes on close, creates the folder, ignores a failed write). The Test Engine deprecation and removal date are from the BUILD-PLAN's Phase 4 research.
- No new `powerops` code. If the starter needs a change in `powerops`, this session stops and reports it as its own task. No version or changelog edits; no push or PR from this session.

## Assumptions

- The canvas player renders each control with `data-control-name` inside `iframe[name="fullscreen-app-host"]` (Learn, samples), and `Gallery1` is visible with no rows: Screen1.pa.yaml sets no `Width` or `Height`, so it has the control's default size, which is not zero (judgement; the live check shows it).
- In the model-driven grid each column header has the role `columnheader` with the column's display name, and a text field's input has its label as accessible name. The live check confirms both; a mismatch is fixed in the test.
- One storage state covers both apps: the person opens both in the same `playwright open` window before closing it, and cookies and local storage of every visited origin are saved. If an app keeps its tokens only in session storage, the saved sign-in cookies let it sign in again silently. The samples keep one state per domain and sign in to the model-driven domain in a separate step; the live check shows whether one file is enough, and step 6 of Environments and route is the fallback.
- The user's account can open both apps in Test, as it did in the M10 and M12 runs.

## Open questions

## Questions for your admin

## Steps

1. `docs:` this plan, its decisions and review, signed off (`docs/plans/m16-playwright/`).
2. `docs:` the example folder, the `docs/examples/README.md` section and the root `.gitignore` line; check the licences with `npm ls --all` and the installed `package.json` files; run `npm ci --ignore-scripts`, `npx tsc --noEmit` and `npx playwright test --list` with no variables set; run the tenant-value search under Goal.
3. `ci:` the `playwright-example` job.
4. Run `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` and `powerops plan status`.
5. Ask the user for a yes to the live check in Test. On yes, run it (Environments and route) and record it; without it, record it as not run.
6. `docs:` BUILD-PLAN: the M16 row marked built with a link to this plan, build-order item 35, the M16 verification-record line (the live result, or "not run" and why), the "Last updated" date; this plan's status. Run the tenant-value search again over the changed files.
