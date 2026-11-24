---
status: done
review: done
environment: null
depth: quick
supersedes: null
signed-off: 2026-11-05
signed-off-hash: 0da2806e629672ed4756a5ea9ccff5cf9d2a3078851f06b0a21303145099e933
---

# M63: Golden-path live rerun

## Goal

Walk the hand-off route ([HANDOFF](../../HANDOFF.md)) again in a live tenant, from a plain request to an import into Test, with the P0 fixes in place, and check each fix where the route reaches it. This is M63 of the improvement plan written after the live test of 2026-11-04 (P1 block; it closes R3, the golden-path rerun, and the finding that the form route was never reached), kept outside the repo because it quotes a private test run. That run reached every step but the yes it was meant to test: the MCP `deploy_apply` form was never answered (the person chose the CLI route), and the M52 live check used the Action route, so no person has yet typed a target's name into the form. This rerun makes the form the yes, runs on Linux here and on the person's Windows machine through a runbook, and records what each step did.

Done when:

1. The Linux route has run end to end, as listed in "Environments and route": install, plan, build in Dev with Microsoft's skills, export, `analyze`, `test`, `diff`, settings, the two review roles, `plan status`, `deploy plan`, the gate's allow path on the real zip, the import into Test answered in the form by the person, `drift`, the Playwright model-driven test, and the plan's last step in Test.
2. Each P0 fix the route reaches has a scenario outcome (pass, partial, fail, not reached) with its evidence: the matrix under "Data".
3. `writes.md` of the scratch plan is reconciled against the read-only inventory of step 10: every write in the journal is found, and every object the inventory finds since the run started is in the journal or explained.
4. `windows-runbook.md` in this folder is written, the person has run it inside Test's term, and its output is in the record, including `icacls .auth\state.json` for the Playwright session file; or the record says it was not reached and why.
5. The record holds the client used for each step and its support status, the cost per model and how each was measured, and the leftovers, listed and not deleted.
6. Scrubbed docs fixes the run finds, if any, are in this PR, and this folder's `decisions.md` logs them.

### Won't build

- No code change. A bug the run finds is recorded with steps to reproduce and goes to its own milestone.
- No canvas app, no Copilot Studio agent build, no second import into Test (decisions Q3, Q6, Q9).
- No fix of the Action route's shared federated credential that the M52 live check found: it is an Entra change in the person's tenant (decisions Q1).

## Users

- The person: owner of the tenant, Dev and Test, and both machines, and an administrator in both environments. Does every sign-in, answers every yes, signs off the scratch plan, answers the form, runs the Windows runbook. Their steps are in "What the person does".
- This session: the orchestrator on Linux. Starts the child agent sessions, plays the requester for product choices only (marked `agent`), runs the read-only checks, writes the record.
- Child sessions: `claude -p --model sonnet --output-format json` in the scratch repo, running `powerops-plan` and `powerops-implement`, which hand the build to Microsoft's skills. A child that asks a question ends its turn; the session answers or relays it and resumes the child with `--resume`, so the child keeps its context, and adds each turn's `total_cost_usd`.

## Data

The scratch repo is new, local and never pushed: `~/Documents/powerops-livetest-<date>/`, outside the PowerOps repo. It holds the product plan folder, the unpacked export, the deploy folder (managed zip, git-ignored settings file, plan file), the flow tests, the Playwright copy, the hooks config and a git-ignored `scratch/` folder for the offline copies below. The first run's scratch repo is read only. The first run's two real agent exports (before and after one instruction edit) are in this session group's notes folder outside the repo, in `agent-exports-2026-11-04/`.

Scenario matrix (what the route checks per fix):

| Fix | Where on the route | Pass when |
|---|---|---|
| M50 coverage | `analyze --format json` and `--format sarif` on the export and on the first run's agent exports; `drift` after the import | `notChecked` (analyze) and `notCompared` (drift) are present and name what they skip; SARIF carries the same list |
| M51 agent layout | `diff` on the first run's two real agent exports, and on the first export against an offline copy of it with the MCP tool's `botcomponents/` folder removed, as the first run made it (decisions Q6) | instructions edit: `agent-instructions`, exit 1; removed tool: `agent-tool`, exit 1; both argument orders |
| M52 authority | the import | the apply report has `executor`, `approval.mode: form` and `appliedAt`; the saved plan has `plannedBy: agent` (a claim the caller makes, not proof); the person typed the name; nothing imported before that |
| M53 coverage and diff | in step 9, after the import: `deploy plan` (not saved, never imported) of an offline copy of the managed zip whose `solution.xml` version is raised by one, with its settings file, in `scratch/upgrade/`; `diff` of the unmanaged folder against the managed zip | the copy's action is `upgrade` with the `components-not-checked` item naming the table; the diff has the `same-version` risk. The plain re-plan of the imported zip is expected to be `skip`, with at most `same-version-differs` (decisions Q9) |
| M54 journal | the build child | `writes.md` has a row per tenant write with actor, authority, operation, outcome and cleanup; no delete call in the child transcripts; `decisions.md` marks each answer person or agent; `plan status` gives no agent sign-off warning |
| M55 report header | `verify.md` and `security-solution.md`, written by the roles on the managed zip | `state: ready`, and a six-line header with `input:` naming the zip and `solution-hash:` equal to its SHA-256 |
| M56 builds | `powerops --version`, the plan file's `tool` | `0.0.0+<commit>` of the installed commit in both; a release pin is not checked (no release exists) |
| M57 recovery | the apply report | its exit code and statuses are in RECOVERY's tables; the runbook is followed only if a row is not `ok` |
| M58, M59 functions | `powerops test` on the child's flow tests | no `unsupported` for a function the real flow uses, or the function is named in the record |
| M61 output hygiene | `analyze` and `diff` with and without `--redact` on `scratch/redact/`, an offline copy of the unpacked folder in which one flow action input holds a fake SharePoint site URL and one variable's default is changed to another fake URL (decisions Q3); the Playwright session file | `analyze` (PO006 quotes the flow's URL) and `diff` (the changed default) show the URLs plain and a placeholder in their place with `--redact`; Linux `drwx------` and `-rw-------`, checked after each save; Windows `icacls` lists one entry, the person's account |
| M62 freshness | `plan status`; `deploy plan` | with the reports `ready`: `stale (solution) false`; `true` while the deploy folder's zip is swapped for a copy that differs only in its zip comment; `false` again with the original back (its SHA-256 checked); `deploy plan` of the deploy folder prints no "no ready ... report" warning, and of `scratch/upgrade/` (step 9) prints it for the copy |

The record (`reports/m63-live-rerun/` in this worktree, git-ignored, never committed) has these parts: per-step evidence (command, exit code, the line that shows the result, scrubbed); the matrix with outcomes; the `writes.md` reconciliation table; the clients and their support status; the cost per model and how each was measured; the gate's allow path (the hook input's shape, exit code, plan hash); the Windows runbook's output with the `icacls` lines; the docs fixes; the leftovers. Scrubbed: no tenant URL, environment, solution component, connection or user id, no email or user name, no tenant deadline date. Only the scrubbed record is copied to the session notes folder (decisions Q10).

## Automation

The build child writes one cloud flow (plan-driven). PowerOps adds none. From step 1 the scratch repo's `.claude/settings.json` denies pac's five MCP write tools ([Gate Microsoft's imports](../../HANDOFF.md#gate-microsofts-imports), the deny rules); the gate hooks go in at step 7 and stay on to the end.

## Screens

None built by this milestone. The product's model-driven app is the build child's. Playwright reads its grid and new-record form in Test.

## Environments and route

Two environments in the person's tenant: Dev (developer) and Test (trial). Their names, URLs and ids stay out of this folder.

**Timing.** Before any tenant step the session asked the person whether Test's trial is extended. The extension is offered only in the trial's last 7 days; if it is not offered when the person tries, the session asks for a replacement environment before the Test part (D8). The person's answer is in `decisions.md` (log): the Test part runs now, inside Test's current term. Every check that needs Test, the Windows runbook's included, finishes inside that term; the Dev part finishes before Dev's suspension date. Both dates are in the person's private notes.

**Rules for every tenant step.** The session asks in chat before each tenant write, naming what the write creates or changes (decisions Q7), and before any spend. It never runs `deploy apply --apply` or `settings apply --apply`, and never calls `deploy_apply` with `apply: true` before the person said yes to Test in this session. It never deletes anything in a tenant or a repo; it lists leftovers. The person does every sign-in, in their own browser or Playwright's Chrome (not the desktop app's browser pane, whose maker-portal sign-in loops; no device code).

**The route, Linux:**

0. Preflight, no tenant. With the person's yes (decisions Q5), install the `powerops` tool from a clone of `main` at a recorded commit with the README's lines, and reinstall the plugin `powerops@powerops` at user scope (uninstall, install), because `plugin update` keeps a `0.0.0` build (M32). Read `powerops --version`, the plugin's commit and the five skills. Record the versions of .NET, `pac`, `az`, Claude Code, the desktop app and Node, and the commits of the person's local clones of power-platform-skills and Dataverse-skills (decisions Q4). Turn off the telemetry of each Microsoft plugin the children load with its own `telemetry-config.js --action off`, as the person chose, and record its status. `pac auth list` and `az account show` read the local sign-ins; if one has expired, the person signs in. Read the account's usage meter (cost).
1. Scratch repo: `git init`, `powerops skills install`, the deny rules, a `.gitignore` for `*.settings.json`, `scratch/`, the unpacked `.msapp` paths and `playwright/.auth/`. Read only: `pac org who --environment` names Dev and Test; `pac admin list` gives their types (Developer, Trial), and the session stops if a type differs; `pac admin dlp-policy list`, then `pac admin dlp-policy show` for each policy, gives the policies that cover each; the person's roles in both read through the Web API. The person's `pac` selection is set to Dev.
2. Plan: a child gets the request in decisions Q3 and loads the PowerOps skills from the plugin and Microsoft's from `--plugin-dir` (decisions Q4). The session answers product choices (marked `agent`), relays every question about sign-off, a High finding, a security gap or anything outside the brief to the person word for word, and records the person's answers (marked `person`). A fresh review comes before the sign-off. The person signs off by saying so in chat; the session runs `powerops plan sign-off` and logs the quote, its time and that the session ran the command on that yes (decisions Q8).
3. Build in Dev: a child runs `powerops-implement` on the signed-off plan, which hands the table, role and app to Dataverse-skills and model-apps and the flow to power-automate. Before each build step the session asks the person, naming the objects that step creates (decisions Q7), and reads `pac org who` (Dev) before and after it; a selection that changed during a step is a stop. The flow's Dataverse connection is an existing one of the person's in Dev, or the person makes it. Then the plan's live checks in Dev (the flow's real trigger with synthetic rows), with a yes for the rows.
4. Export from Dev (unmanaged and managed; an export changes no component) and unpack into XML, as HANDOFF step 3.
5. Offline checks: `analyze` (four formats; with and without `--redact`), `test` on the child's flow tests, `diff` (folder against its own zip; unmanaged folder against managed zip; folder against `scratch/redact/`, with and without `--redact`), `analyze` of `scratch/redact/` with and without `--redact`, `settings generate` and `validate`, the two review roles on the managed zip in fresh contexts (started by the build child), `plan status` and the M62 zip swap. M50 and M51 on the first run's agent exports.
6. Settings for Test: the person names the Test connection the reference binds to (an existing one, or one the person makes). `settings validate` passes.
7. `deploy plan deploy --target <Test> --planned-by agent --save deploy/powerops-plan.json`. Hooks on. The gate's allow path, before the import: the session writes Claude Code's `PreToolUse` input for a Bash call (`hook_event_name`, `tool_name: "Bash"`, `tool_input.command`, `cwd`) into a scratch file outside the repo, its command the planned `pac solution import` with Test, literal absolute paths, `--async` and `2>&1`, and feeds it to `powerops deploy gate` on stdin (nothing runs; exit 0); the same naming Dev (exit 2). The record keeps the shape, the exit codes and the plan hash only.
8. The import, by the form (decisions Q1, Q2). The person opens an interactive Claude Code session in the scratch repo: a new Code-tab session of the desktop app, or `claude` in a terminal with the folder trust prompt answered first. In it the person asks for the apply of the plan file to Test; the agent there asks in chat, the person says yes, the agent calls `deploy_apply` with `apply: true`, and the person types Test's name in the form. If the desktop app shows no form, the session records the refusal and the person uses the terminal. If neither shows it, the session stops and asks the person (decisions Q2).
9. After the import: `drift` Dev against Test; `deploy plan` of the deploy folder again (expected `skip`); the M53 and M62 `deploy plan` of `scratch/upgrade/` (not saved; Test now holds the solution, so the copy is an `upgrade`); the Playwright model-driven test against the app in Test; the scratch plan's last step in Test (its flow with synthetic rows), with the person's yes for those rows. Playwright: the starter is copied into the scratch repo, the names at the top of `apps.spec.ts` changed to the product's table, column and field, and the canvas test removed in the copy (no canvas app); the person signs in in the `playwright open` window and pastes the model-driven link there; a failure of a selector the starter never confirmed live is a starter finding, not a PowerOps one.
10. Read-only inventory, in Dev after step 4 and in Test after step 9, of what changed since the run started, read with `createdon` or `modifiedon` at or after the start: through the Web API with an `az` token, `solutions`, the new solution's `solutioncomponents`, `workflows`, `connectionreferences` (in any solution, the default one included), `environmentvariabledefinitions` and `environmentvariablevalues`, `appmodules`, `sitemaps`, `roles` and the users of each new role (its `systemuserroles_association`), `savedqueries`, `systemforms`, the table's rows, `flowruns` of the run's flows (kept for a limited time, so read on the day); read whole and filtered in the script, the custom tables' metadata by its `CreatedOn` and `ModifiedOn`, and `organizations`' `isauditenabled`, compared with the first read; outside Dataverse, `pac connection list` for connections. What it can't see (changes in the maker portal's own settings, Power Automate state outside Dataverse) is named in the record. Reconciled against `writes.md`.

**Windows** (decisions Q11). `windows-runbook.md` in this folder, drafted before step 7 with placeholders and the commit, run by the person in PowerShell 7 right after step 9, inside Test's term; if it can't be, the record says not reached. It writes nothing in a tenant: install from the same commit in the person's clone (the plugin's server stopped first, as M32 found), `--version`, `analyze`, `test` and `diff` on a copy of the scratch repo's export and deploy folder (moved by the person's own means, never through a shared or public place, because it holds Test's connection id), a new `deploy plan` against Test saved under a new file name (after the import: `skip`), the same of the raised copy (`upgrade`, with the M53 item), the gate's allow path with quoted `C:\` paths (on the raised copy's plan, whose first action imports), the Playwright set-up with `icacls` and the model-driven test, and `deploy_apply` with `apply: true` on that new plan in an interactive Claude Code session, where the person declines the form (expected: declined, nothing imported; if no form appears, the record says not reached, not passed). It uses no URL: every link is a placeholder. The person pastes the output back; the session scrubs it into the record.

**What the person does** (W: a tenant write; L: a change on their machine):

| When | What |
|---|---|
| Before the run | Answered the Test question; said yes to the reinstall (L) and to turning Microsoft's telemetry off (L) |
| Step 0 | Signs `pac` or `az` in again if a sign-in expired |
| Step 2 | Answers questions the session relays; signs the scratch plan off in chat; accepts or refuses any High finding or security gap |
| Step 3 | A yes per build step in Dev, naming its objects (W); a Dataverse connection in Dev if none exists (W); a yes for the synthetic rows (W) |
| Step 6 | Names the Test connection, or makes one (W) |
| Step 8 | Opens the interactive session; says yes to Test in chat; types Test's name in the form (W, the import) |
| Step 9 | Signs in in Playwright's window and pastes the model-driven link; a yes for the synthetic rows in Test (W) |
| Windows | Signs `pac` and `az` in there if needed; reinstalls the tool and plugin there (L); runs the runbook; declines the form; pastes the output |
| After | Deletes the leftovers the record lists, if they want them gone |

## Solution and publisher

The scratch plan's solution: a new unmanaged solution in Dev, named by the planning child, with the existing `pops` publisher, reused, not created (the seed's); its managed export goes to Test. No PowerOps fixture comes from it.

## Constraints

- Nothing tenant-specific is committed: this folder holds no environment name, URL, id, email, user name or tenant deadline date, and the record stays in `reports/` (git-ignored).
- The run's exports, transcripts and record are unscrubbed until written into the record, so none of it goes to Codex, Gemini, Pi or any non-Claude model.
- Pinned roles: `skills/powerops-verify`, `skills/powerops-review-security`, their plugin copies and `tests/eval` are not touched.
- Every subagent gets an explicit model; children run on Sonnet.
- Windows and Linux only.

**Cost estimate per model** (the session stops and asks before going over a line):

| Model | Used for | Estimate and basis | Measured by | Stop at |
|---|---|---|---|---|
| Claude Sonnet 5.5, `claude -p` children | planning, build, the two roles | USD 10–20 API-equivalent; the first run's children came to USD 9.09, and this build is no larger | the sum of each turn's `total_cost_usd` | USD 25 |
| Claude Sonnet 5.5, subagents | this plan's review passes, record checks | about 180k tokens a pass (this plan's first pass), 2 or 3 passes, plus one record check | each subagent's reported tokens | 4 review passes |
| Claude Haiku 5.5 | background waits, mechanical chores | a guess: under 100k tokens in all | each subagent's reported tokens | 300k tokens |
| Claude Opus 5.5, this session | orchestration, checks, the record | a guess: 8–15 points of the account's weekly usage meter | the meter before and after, which other sessions share, so an upper bound; and this session's context size | 20 points |
| The person's interactive sessions | the form session, the Windows session | small; on the person's plan | not measured | none |
| Codex, Gemini, Pi | none: the data is unscrubbed | 0 | not used | not used |
| Tenant | no purchase, licence or capacity | 0 | none | any spend asks first |

## Assumptions

- The person's `pac` and `az` sign-ins reach Dev and Test, and the DLP policies of both allow the Dataverse connector: in the first run a Dataverse flow ran in each. Step 1 reads the policies, and the session asks the person if one has changed.
- The person's account opens model-driven apps in Test: in the first run it did, twice, though its Power Apps trial had expired, which blocks only canvas play.
- The desktop app's Code tab may or may not show MCP forms; HARNESS.md doesn't list it, so step 8 finds out.

## Open questions

None.

## Questions for your admin

None: the person is the admin.

## Steps

1. `docs:` this folder: plan, decisions, a fresh review, sign-off. Check: `powerops plan status docs/plans/m63-live-rerun` is clean; `signed-off-hash` equals `review.md`'s `plan-hash`.
2. The route's steps 0 to 6 on Linux, each recorded as it runs; `windows-runbook.md` drafted; then steps 7 to 10.
3. The person runs the runbook; its output goes into the record. `plan status` on this folder is clean with the runbook in it.
4. `docs:` the scrubbed docs fixes the run finds, each logged in `decisions.md`.
5. The record; the leak scan `bash reports/leakscan.sh origin/main HEAD` from the main checkout (`control: found (ok)`, 0 hits); the PR; CI.
