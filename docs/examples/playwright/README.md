# Playwright starter

A small TypeScript [Playwright](https://playwright.dev/) project that checks two apps in a browser after they were imported into Test: the seed solution's canvas app and the model-driven app from the hand-off run (M12). It is an example to copy, not part of `powerops`. Change the names at the top of [tests/apps.spec.ts](tests/apps.spec.ts) for your own apps.

The two tests only read:

- **Canvas app**: opens the app's play link and waits for its `Gallery1` control inside the player's `fullscreen-app-host` frame.
- **Model-driven app**: opens the `pops_sitevisit` grid and checks the "Visit Date" column header, then opens a new record's form and checks the "Visit" text box. It saves nothing. For your own app, pick a text column for `formField`: a date, lookup or choice column is a combobox on the form, not a text box, so the check doesn't find it.

**The tests were not run against the seed apps yet.** CI type-checks them and lists them. The model-driven test ran live once, in the M63 run, against another app with its names changed: the grid check passed, and the form check passed on a text column and failed on a date column. Still unconfirmed: the selectors (`Gallery1`, the "Visit Date" column header, the "Visit" text box), and that one saved sign-in covers both apps. See the [verification record](../../BUILD-PLAN.md#verification-record).

## Where it runs

Run it after `powerops deploy apply` has imported the solution into Test, and before you promote it any further: step 8 of the [hand-off](../../HANDOFF.md), or the `apply` job of the [promotion example](../README.md#browser-tests-after-the-apply). Run it against Test only. Whatever a test clicks happens in that environment, so a test you add that saves a record writes there.

## Set up

You need Node.js 20 or later (the CI check uses 24). Run every command below from this folder.

```sh
cd docs/examples/playwright
npm ci
npx playwright install chromium
```

The last command downloads Chromium for Playwright; it is your download, not a dependency of this repository.

The account that signs in needs a Power Apps licence that covers Dataverse apps, both apps shared with it, and a security role with Read on both tables and Create on `pops_sitevisit`: the form test opens a new record's form, though it saves nothing.

## Sign in once

The tests use your own sign-in, saved by Playwright. Set the two links first:

- `CANVAS_APP_URL`: the canvas app's play link: **Web link** on the app's **Details** in the maker portal, or the address bar while the app plays.
- `MODEL_DRIVEN_APP_URL`: the model-driven app's link, copied from the address bar while the app is open. It must hold the `appid` parameter.

```sh
export CANVAS_APP_URL='<canvas app web link>'
export MODEL_DRIVEN_APP_URL='<model-driven app link>'
```

In PowerShell: `$env:CANVAS_APP_URL = '<canvas app web link>'`, and the same for the other.

Make the `.auth` folder private before the sign-in is saved into it, so that only your account can read it:

```sh
mkdir -p .auth && chmod 700 .auth
```

In PowerShell:

```powershell
New-Item -ItemType Directory -Force .auth | Out-Null
icacls .auth /inheritance:r /grant:r "$(whoami):(OI)(CI)F"
```

The `icacls` command removes the entries `.auth` inherits from the folders above it and gives full access to your account alone. Every file Playwright writes into the folder inherits that entry. `whoami` names your account with its domain or computer name, which `icacls` needs for a work or school account. Afterwards only your account can open the folder, so backup or antivirus software that runs as the system cannot read it; `icacls .auth /reset /t` gives the folder and its files their inherited entries back.

Then open a browser that saves your sign-in when you close it:

```sh
npx playwright open --save-storage=.auth/state.json "$CANVAS_APP_URL"
```

In PowerShell, end the command with `"$env:CANVAS_APP_URL"` instead.

It opens a Chromium window with the Playwright Inspector beside it. Sign in in that Chromium window, not in your usual browser.

1. Sign in yourself in that window, with your password, code and MFA as usual.
2. Wait until the app shows its gallery.
3. Paste the model-driven app's link into the same window's address bar and wait until the app shows its grid.
4. Close the window. Playwright writes `.auth/state.json`. Check that the file exists: Playwright says nothing if it could not write it.
5. On Linux, make the file private too, after each save: `chmod 600 .auth/state.json`. Check with `ls -ld .auth .auth/state.json`: the folder shows `drwx------` and the file `-rw-------`. On Windows, check with `icacls .auth\state.json`: it lists one entry, your account.

`.auth/state.json` holds your session cookies and tokens for the sign-in page and both apps. Treat it like a password: never commit it (`.gitignore` excludes it), never share or upload it, and delete it when you are done. When the sign-in expires, a test waits on the sign-in page until it times out: do these steps again.

## Run

```sh
npx playwright test
```

Exit code 0 means both tests passed, 1 that a test failed. Add `--headed` to watch the browser. A missing link or a missing `.auth/state.json` fails the test with its name. The grid test reads the table's default view; if you set a personal default view without the "Visit Date" column, it fails.

## Why CI does not run these tests

Microsoft's samples sign in for CI with a password or a certificate file (their `playwright-ms-auth` package). Either one would be a stored secret in CI, and PowerOps keeps no stored secret in its examples ([FOUNDATION](../../FOUNDATION.md) section 4). The saved sign-in above is a person's credential and stays on their machine. CI only type-checks the project and lists its tests (`npx tsc --noEmit` and `npx playwright test --list`), with no browser and no sign-in.

## Never commit

`.auth/`, the two links, `test-results/` and `playwright-report/`. The links hold your environment's address and ids, and the results can show your data. `.gitignore` covers the folders; keep the links in your shell. For the same reason, don't paste a failure's output into a public issue: it shows the page the test opened.

## Credit

This starter follows [microsoft/power-platform-playwright-samples](https://github.com/microsoft/power-platform-playwright-samples) (MIT licence, Copyright (c) Microsoft Corporation), read at commit `0b41791`: the player frame selector, the `data-control-name` locators and the model-driven grid and form links come from it. No file is copied. Microsoft's [migration guide from Power Apps Test Engine](https://learn.microsoft.com/power-platform/developer/playwright-samples/migration-guide) points to these samples. Test Engine is deprecated, and the [PowerApps-TestEngine repository](https://github.com/microsoft/PowerApps-TestEngine) says it will be removed on 2026-11-01.

Left out of the samples:

- `playwright-ms-auth`: its password and certificate sign-in would need a stored secret (above).
- `@axe-core/playwright`: MPL-2.0. PowerOps takes only MIT and Apache-2.0 dependencies.
- Their toolkit package: part of their workspace, not published on npm. Don't `npm install power-platform-playwright-toolkit`, the name the migration guide imports: on npm that name is a security holding package (`0.0.1-security`), not Microsoft's code.
- `dotenv`: the links are set in the shell, so no `.env` file can be committed by mistake.

The dependencies are `@playwright/test` (Apache-2.0), `typescript` (Apache-2.0) and `@types/node` (MIT).
