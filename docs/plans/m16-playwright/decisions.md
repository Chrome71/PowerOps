# Decisions: m16-playwright

## Round 1

### Q1. How does a person sign in and save the storage state?
1. Playwright's own `npx playwright open --save-storage=.auth/state.json <canvas link>`: the person signs in in that headed window, opens the model-driven link in the same window, and closes it; Playwright writes the gitignored file on close. No sign-in code in the starter (recommended)
2. A sign-in setup test in its own Playwright project that waits for both apps and calls `storageState`, run with `--project` and `--headed`
3. In your own words
Answer: Playwright's own `npx playwright open --save-storage=.auth/state.json <canvas link>`: the person signs in in that headed window, opens the model-driven link in the same window, and closes it; Playwright writes the gitignored file on close. No sign-in code in the starter. A setup project would add a third test to `--list`, a project the default run must skip, and code to maintain; the built-in command does the same with nothing to type-check, and the person, not code, decides when both apps have loaded.

### Q2. What do the two tests check, and do they write?
1. Read only: the canvas test waits for `Gallery1` inside the `fullscreen-app-host` frame; the model-driven test opens the `pops_sitevisit` grid and checks the "Visit Date" column header, then opens a new-record form and checks the "Visit" text box, saving nothing (recommended)
2. Create, check and delete a record in each app, as the samples' CRUD tests do
3. In your own words
Answer: Read only: the canvas test waits for `Gallery1` inside the `fullscreen-app-host` frame; the model-driven test opens the `pops_sitevisit` grid and checks the "Visit Date" column header, then opens a new-record form and checks the "Visit" text box, saving nothing. The seed canvas app has no form to write with, a saved record in Test would need its own cleanup, and the BUILD-PLAN row asks for a grid and a form, not a save. The README says to keep any test that saves on Test only.

### Q3. How do the offline check and the live check run?
1. Offline: a `ci:` job `playwright-example` (ubuntu, Node 24) runs `npm ci --ignore-scripts`, `npx tsc --noEmit` and `npx playwright test --list`, with no browser, secret or variable. Live: once, in Test only, after the user's yes in this session; the user signs in themselves, then both tests run, then once with a changed label (recommended)
2. No CI job; the type check runs only locally, and the live check is the only proof
3. In your own words
Answer: Offline: a `ci:` job `playwright-example` (ubuntu, Node 24) runs `npm ci --ignore-scripts`, `npx tsc --noEmit` and `npx playwright test --list`, with no browser, secret or variable. Live: once, in Test only, after the user's yes in this session; the user signs in themselves, then both tests run, then once with a changed label. The BUILD-PLAN row's done-when asks for the `ci:` job, and build-order item 35 makes it its own commit.

## Log

- plan started 2026-10-27 from the master session's brief (the BUILD-PLAN M16 row, build-order item 35, the Phase 4 environment notes, decision 22)
- `powerops plan facts` not run: the build needs no environment, and the live check targets Test by role; `environment` stays null
- depth: quick (from `m15-merge-gate`)
- the user delegated their parts on 2026-10-22 (relayed by the master session): this session answers with the recommended options and signs the plan off itself after a clean fresh review; the live check in Test still needs the user's yes in this session (Q3), and the user types every password, code and MFA answer
- read on 2026-10-27: microsoft/power-platform-playwright-samples at `0b41791` (cloned into a scratch folder outside the repo, nothing run), Learn's playwright-samples migration guide and authentication guide, and the M12 export kept outside Git (app "Site Visits", table `pops_sitevisit`, default view and main form labels)
- prototype on 2026-10-27, outside the repo: the planned files with the three pinned packages type-check (`tsc` exit 0) and `npx playwright test --list` lists two tests with no variable set and no browser installed
- approved without: users, data, constraints (the BUILD-PLAN M16 row and the Phase 4 text cover them; licensing is in Constraints)
- approved (2026-10-27)
- fresh review (powerops-reviewer, Sonnet) 2026-10-27: 0 High, 5 Medium, 9 Low; all fixed in the plan text, R9 in part (the sign-in host check accepted as not needed); no decision changed and no user decision needed
- signed off 2026-10-27 by this session under the user's delegation (see above), after every review finding was fixed or accepted
- 2026-10-27: built steps 2 to 4 (`docs(examples)` the starter, the examples README section and the root `.gitignore` line; `ci:` the `playwright-example` job). Offline checks pass: `npm ci --ignore-scripts`, `npx tsc --noEmit`, `npx playwright test --list` (two tests) with no variable, browser or sign-in; `npm ls --all` holds the six planned packages, Apache-2.0 or MIT; the tenant-value search finds only the README's public links and the npm registry; `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` and `powerops plan status` are clean. The live check in Test waits on the user's yes in this session
