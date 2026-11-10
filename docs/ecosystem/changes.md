# Ecosystem changes: the register

What was found in Microsoft's ecosystem, what was decided and why. The procedure is in [README.md](README.md). `EcosystemDocsTests` holds this file to its format.

One section per finding, `## ECO-<4 digits>: <title>`, newest last. Ids rise by one from `ECO-0001` and are never renumbered or deleted. Each entry has these one-line fields, in this order: `Priority` (P0 to P3), `Disposition` (implement now, plan for later, monitor, no action), `Status` (open, done, closed), `Release status` (announced, preview, GA, deprecated, retired, unverified), `Found` (date and mode), `Verified` (date), `Affects` (paths or none), `Sources` (https links, each with its published, updated or observed date), `Evidence` (documented, tested, documented and tested), `Why`, `Link` (plan, commit, issue, FOUNDATION item or none) and, last and optional, `Supersedes`.

The review log below has one line per review: `- <date>, mode <A|B|C>: <n> automated read, <n> not checked, <n> by hand; <ids or "no entries">`. A line may go on, after a `.` or `;`, to name the cheap-path observation ids and the sources that had no baseline.

## ECO-0001: The watch's pa-cli source read the SDK package, not the CLI
- Priority: P2
- Disposition: implement now
- Status: done
- Release status: GA
- Found: 2026-11-10, mode A
- Verified: 2026-11-10
- Affects: docs/ecosystem/sources.json, docs/ecosystem/compatibility.md
- Sources: [npm: @microsoft/power-apps-cli](https://registry.npmjs.org/@microsoft/power-apps-cli) (observed 2026-11-10), [npm: @microsoft/power-apps 1.6.0](https://registry.npmjs.org/@microsoft/power-apps/1.6.0) (observed 2026-11-10), [Create a code app from scratch](https://learn.microsoft.com/power-apps/developer/code-apps/how-to/create-an-app-from-scratch) (updated 2026-08-20)
- Evidence: documented
- Why: `@microsoft/power-apps` 1.6.0 is the Power Apps SDK and has no `bin`, so the 5a observation `pa-cli@1.6.0` said nothing about the `pa` the gate routes; the CLI is `@microsoft/power-apps-cli` (`bin` `pa`, node 22 or later, `latest` 1.4.1 on 2026-11-04). Fixed in this review: the source's target is the CLI package and `compatibility.md` has a Power Apps CLI row. `tests/PowerOps.Core.Tests/Deploy/ImportCallTests.cs` already names the CLI package. The CLI's own changelog was not found, so what 1.4.x changed beyond ECO-0005 is unverified.
- Link: docs/plans/m84-ecosystem-watch/plan.md (step 5c, commit `64588ea`)

## ECO-0002: The public mirror of the flow limits page lags the live page
- Priority: P2
- Disposition: implement now
- Status: done
- Release status: GA
- Found: 2026-11-10, mode A
- Verified: 2026-11-10
- Affects: docs/ecosystem/sources.json, docs/ecosystem/README.md
- Sources: [Limits of automated, scheduled, and instant flows](https://learn.microsoft.com/power-automate/limits-and-config) (updated 2026-10-29), [MicrosoftDocs/power-automate-docs commits](https://github.com/MicrosoftDocs/power-automate-docs/commits/main) (observed 2026-11-10)
- Evidence: documented
- Why: The live page's `git_commit_id` is `c617807` (2026-10-28), but the public repository's `main` has had no commit since 2026-04-28, so the github-commits source would never have seen this or any later edit; for the Power Platform and Power Apps pages the live and public commits match. PO016's and PO017's values (500 actions, 8 levels, Until 60 and 5,000) still match the live page (return 3). A branch or ref field would be a new source format, so instead this review made `docs-flow-limits` a manual source read every 30 days (done in `sources.json`). Left for step 6, inside `docs/ecosystem/` only: README line 43 says 17 GitHub calls a run (now 16), and line 122 maps Power Automate pages to a repository the collector can watch; say that its `main` stopped syncing and its pages are manual. Acceptance: both lines say so; check: `EcosystemDocsTests` and `DocsDriftTests`. Fixed in step 6: README says 16 calls (14 `github-commits` and 2 `github-release` sources) and that Power Automate pages are manual sources because that repository's `main` has had no commit since 2026-04-28.
- Link: docs/plans/m84-ecosystem-watch/plan.md (step 6, commit `a29f8f7`)

## ECO-0003: Learn's release plans are archived; the roadmap replaces them
- Priority: P3
- Disposition: implement now
- Status: done
- Release status: deprecated
- Found: 2026-11-10, mode A
- Verified: 2026-11-10
- Affects: docs/ecosystem/sources.json
- Sources: [Power Platform release plans](https://learn.microsoft.com/power-platform/release-plan/) (updated 2026-09-03), [GitHub support for source code integration (release plan)](https://learn.microsoft.com/power-platform/release-plan/2026wave1/power-platform-governance-administration/github-support-source-code-integration-power-platform) (updated 2026-09-03), [Connect to GitHub (preview)](https://learn.microsoft.com/power-platform/alm/git-integration/connecting-to-github) (updated 2026-09-01)
- Evidence: documented
- Why: Both release-plan pages carry the banner that the content is archived as of October 2026 and point to the Microsoft 365 roadmap, which is already the `m365-roadmap` source, so this review removed the `release-plans` source. `docs/FOUNDATION.md:156` cites the archived plan for "public preview since 2026-08-31"; the claim still holds through the live "Connect to GitHub (preview)" page, so no edit. `skills/powerops-plan/references/model-driven.md:36` already labels its release plan archived.
- Link: docs/plans/m84-ecosystem-watch/plan.md (step 5c, commit `64588ea`)

## ECO-0004: The Dataverse MCP server lists 16 tools and renamed or removed some
- Priority: P2
- Disposition: implement now
- Status: done
- Release status: GA
- Found: 2026-11-10, mode A
- Verified: 2026-11-10
- Affects: docs/FOUNDATION.md:21, docs/ecosystem/compatibility.md
- Sources: [Dataverse MCP server](https://learn.microsoft.com/power-apps/maker/data-platform/data-platform-mcp) (updated 2026-11-04), [Dataverse MCP server FAQ](https://learn.microsoft.com/power-apps/maker/data-platform/data-platform-mcp-faq) (observed 2026-11-10), [MicrosoftDocs/powerapps-docs 69cf4c7](https://github.com/MicrosoftDocs/powerapps-docs/commit/69cf4c7a9213db15b66ae961757de7120093163f) (published 2026-11-04), [Roadmap item 576752](https://www.microsoft.com/microsoft-365/roadmap?id=576752) (updated 2026-10-16)
- Evidence: documented
- Why: The page's tool table now has 16 tools (`invoke_api` added; `describe_table`, `list_tables` and `fetch` replaced by `describe`; the data `search` renamed `search_data` and a metadata `search` added; `upsert_skill` and `delete_skill`, for preview business skills), and it says clients that allow or deny tools by name must review their lists. `git grep` found no Dataverse MCP tool name in any hook, the gate, `docs/HANDOFF.md`, `skills/` or `.claude/agents/`, so nothing breaks; the only counts are "15 tools" at `docs/FOUNDATION.md:21` and the compatibility row (fixed in this review). `FOUNDATION.md:21` also says the tools are charged from 2025-12-15 for agents built outside Copilot Studio and that the pages show no date; the page now gives only the Copilot credit rates, the FAQ keeps the 2025-12-15 note. Step 6: say "16 tools (`search_data` only where Dataverse search is on)", name both billing pages and the read date 2026-11-10. Acceptance: the line names 16 tools and the date; check: `DocsDriftTests`. About 2 lines. Fixed in step 6: the line says 16 tools (`search_data` only where Dataverse search is on), names the main page's Copilot credit rates and the FAQ's 2025-12-15 note, and is read on 2026-11-10 with the main page's date 2026-11-04 in place of "no update date"; the compatibility row says FOUNDATION says 16. Roadmap 576752 (a separate management MCP server, in development, GA December 2026) is not the same server.
- Link: docs/plans/m84-ecosystem-watch/plan.md (step 6, commit `7127715`)

## ECO-0005: pa app add data-source can create a connection; HANDOFF's list of writes without a route omits it
- Priority: P2
- Disposition: implement now
- Status: done
- Release status: GA
- Found: 2026-11-10, mode A
- Verified: 2026-11-10
- Affects: docs/HANDOFF.md:108, docs/COMMANDS.md:136, tests/PowerOps.Cli.Tests/HandoffDocTests.cs:172
- Sources: [power-platform-skills 2eb182a](https://github.com/microsoft/power-platform-skills/commit/2eb182a8b7a2faa65bb2363139763291c920fbf3) (published 2026-11-05), [code-apps shared-instructions.md at 7322abb](https://github.com/microsoft/power-platform-skills/blob/7322abba73b4859e1c0032c7223db65444d42396/plugins/code-apps/shared/shared-instructions.md) (observed 2026-11-10), [Roadmap item 576782](https://www.microsoft.com/microsoft-365/roadmap?id=576782) (updated 2026-10-16), [pac power-pages](https://learn.microsoft.com/power-platform/developer/cli/reference/power-pages) (updated 2026-09-11), [pac copilot-studio](https://learn.microsoft.com/power-platform/developer/cli/reference/copilot-studio) (updated 2026-10-23), [pac environment-management](https://learn.microsoft.com/power-platform/developer/cli/reference/environment-management) (updated 2026-09-11)
- Evidence: documented and tested
- Why: Three researchers found it. The code-apps plugin says that on CLI 1.3.0 or later `pa app add data-source` "can create a connection with the user's account when the connector has none, without asking first"; Learn's CLI reference (updated 2026-10-02) does not say so, so the claim rests on Microsoft's plugin and the roadmap item. Running the gate (built from this branch) on `pa app add data-source --connector shared_x --non-interactive` exits 0: it has no route, as for `pa connection create`. `HANDOFF.md:108` names `pa app share` and `pa connection create` but not this command, nor the writes of the preview groups `pac power-pages`, `pac copilot-studio` and `pac environment-management` (create and delete website, delete agent, copy, reset and delete environment and others, read on Learn). Step 6: add `pa app add data-source` (which can create a connection) and those three groups to that sentence, and add `` `pa app add data-source` `` to the list in `HandoffDocTests` line 172. Acceptance: the sentence names both; check: `HandoffDocTests`. About 4 lines. A gate route for them is ECO-0008. Fixed in step 6: the sentence names `pa app add data-source`, sourced to the plugin, and the three groups, its Sources line gains the plugin file and the three Learn pages, `docs/COMMANDS.md:136` (which repeats the list) names the command, and `HandoffDocTests` holds it.
- Link: docs/plans/m84-ecosystem-watch/plan.md (step 6, commit `a12eb52`)

## ECO-0006: Deprecation wording: developer environment creation and the Test Engine repository
- Priority: P3
- Disposition: implement now
- Status: done
- Release status: deprecated
- Found: 2026-11-10, mode A
- Verified: 2026-11-10
- Affects: docs/FOUNDATION.md:91, docs/BUILD-PLAN.md:279
- Sources: [Important changes (deprecations) coming](https://learn.microsoft.com/power-platform/important-changes-coming) (updated 2026-11-02), [microsoft/PowerApps-TestEngine](https://github.com/microsoft/PowerApps-TestEngine) (observed 2026-11-10)
- Evidence: documented
- Why: Learn says unmanaged developer environments can't be created "starting November 15, 2026"; `FOUNDATION.md:91` says "after 2026-11-15", a day off (`BUILD-PLAN.md:194` already says "from"). Learn says Test Engine's docs and repository "will be removed in a future release", with no date; `BUILD-PLAN.md:279` says the repo "is removed on 2026-11-01", a date Learn does not give, and the repository answered 404 on 2026-11-10. The suspension and deletion dates (2026-12-15, 2027-01-15) and the Dataverse for Teams note at `FOUNDATION.md:192` hold. Step 6: "after" to "from" at line 91, and at line 279 "Learn says its repo will be removed, with no date; it answered 404 on 2026-11-10". Acceptance: both lines match the page; check: `DocsDriftTests`. About 4 lines. Fixed in step 6: `FOUNDATION.md:91` says "from 2026-11-15" and `BUILD-PLAN.md:279` says Learn gives no removal date for the Test Engine docs and repository and that the repository answered 404 on 2026-11-10 (one observation). `skills/powerops-plan/references/data.md:99` gives no dates, so it needs nothing.
- Link: docs/plans/m84-ecosystem-watch/plan.md (step 6, commit `6be2c92`)

## ECO-0007: Claude Code 2.1.314: a Bash hook also runs on Monitor's commands
- Priority: P3
- Disposition: implement now
- Status: done
- Release status: GA
- Found: 2026-11-10, mode A
- Verified: 2026-11-10
- Affects: docs/HANDOFF.md:93
- Sources: [Claude Code changelog](https://github.com/anthropics/claude-code/blob/main/CHANGELOG.md) (observed 2026-11-10), [Claude Code hooks](https://code.claude.com/docs/en/hooks) (observed 2026-11-10)
- Evidence: documented
- Why: The 2.1.314 entry reads "Fixed hooks matching Bash not running on shell commands started by the Monitor tool; such hooks can block or ask, but not approve or rewrite those commands". The gate blocks by exit 2, so it still works there; with the separate `Monitor` group of `HANDOFF.md:77-80` it runs twice on 2.1.314 or later (harmless, it only reads), and the group still covers the smoke pin 2.1.303. Step 6: add to line 93 that from 2.1.314 the `Bash` group also covers Monitor and the `Monitor` group keeps older builds covered. Acceptance: the line names 2.1.314; check: `HandoffDocTests` and `DocsDriftTests`. About 2 lines. Fixed in step 6: line 93 says that from 2.1.314 a `Bash` matcher also runs on Monitor's commands (documented in the hooks reference and changelog, not tested by PowerOps) and that the `Monitor` group keeps older builds covered; its Sources line names the changelog. Return 6 also said the hooks docs allow spaces and commas in an exact-name matcher (line 95); not checked in this review.
- Link: docs/plans/m84-ecosystem-watch/plan.md (step 6, commit `54c4b13`)

## ECO-0008: The gate misreads two Microsoft forms and has no route for the preview pac groups
- Priority: P3
- Disposition: plan for later
- Status: done
- Release status: preview
- Found: 2026-11-10, mode A
- Verified: 2026-11-10
- Affects: src/PowerOps.Core/Deploy/ImportCall.cs, tests/PowerOps.Core.Tests/Deploy/ImportCallTests.cs, tests/PowerOps.Cli.Tests/DeployGateCommandTests.cs, tests/PowerOps.Cli.Tests/HandoffDocTests.cs, docs/HANDOFF.md, docs/COMMANDS.md
- Sources: [pac power-pages](https://learn.microsoft.com/power-platform/developer/cli/reference/power-pages) (updated 2026-09-11), [power-platform-skills 4076781](https://github.com/microsoft/power-platform-skills/commit/4076781a3c6862896ad98bd51f32e180d9ddd4fd) (published 2026-11-05), [set-cli-origin.js at 4076781](https://github.com/microsoft/power-platform-skills/blob/4076781a3c6862896ad98bd51f32e180d9ddd4fd/plugins/code-apps/hooks/set-cli-origin.js) (published 2026-11-05), [cli-origin.test.js at 4076781](https://github.com/microsoft/power-platform-skills/blob/4076781a3c6862896ad98bd51f32e180d9ddd4fd/plugins/code-apps/scripts/tests/cli-origin.test.js) (published 2026-11-05)
- Evidence: tested
- Why: Run on 2026-11-10 with the gate built from this branch: `pac power-pages upload-certificate --path cert.pfx` and `PA_CLI_ORIGIN=plugin/claude-code pa app push --solution-id x` (the code-apps plugin's hook adds that prefix since 4076781) both exit 2 with "the command holds an import that the gate can't read", because the first hits the `pages upload` marker and the second starts with an assignment. Both fail closed, so nothing is unsafe, but the message is wrong for a certificate upload and for a plain app push. Writes of the preview groups (ECO-0005) pass with exit 0. A fix changes the gate's routes and messages, a public output, so it needs its own milestone: a route or marker exception for `upload-certificate`, an assignment prefix read as the environment of `pa`, and an owner decision on routes for the preview groups. Correction, M85: the hook at 4076781 (`plugins/code-apps/hooks/set-cli-origin.js:67-71`) does not add a `PA_CLI_ORIGIN=` prefix. It prepends a line `if [ "${PA_CLI_ORIGIN+x}" != x ]; then export PA_CLI_ORIGIN='plugin/claude-code'; fi`, which the gate already read with the `pa app push` reason (run 2026-11-10). The form the gate misread is the inline `PA_CLI_ORIGIN=inline/custom <call>`, which Microsoft's own hook tests run as an explicit user value (`cli-origin.test.js:184`). Fixed in M85: an inline assignment before `pa` is read with the route's reason, the program is never taken from an assignment word (an import or route after one is unreadable, decision Q3), and `pac power-pages upload-certificate` passes like its group in a Bash or Monitor command. The routes of the preview groups stay unrouted by the owner's decision, with ECO-0019.
- Link: docs/plans/m85-gate-routes/plan.md (step 2, commit `58e21b0`; step 4, commit `16e7a5a`)

## ECO-0009: model-apps 2.21.0 to 2.25.0 declares components and adds retire and destructive options
- Priority: P2
- Disposition: plan for later
- Status: open
- Release status: GA
- Found: 2026-11-10, mode A
- Verified: 2026-11-10
- Affects: skills/powerops-implement/SKILL.md:39, docs/HANDOFF.md:35, docs/HANDOFF.md:52, skills/powerops-plan/references/model-driven.md:8, docs/BUILD-PLAN.md:14
- Sources: [model-apps CHANGELOG at 7322abb](https://github.com/microsoft/power-platform-skills/blob/7322abba73b4859e1c0032c7223db65444d42396/plugins/model-apps/CHANGELOG.md) (observed 2026-11-10), [power-platform-skills da8ea7a](https://github.com/microsoft/power-platform-skills/commit/da8ea7ab7b07cb752c6596fffabb934c7807fff1) (published 2026-11-03)
- Evidence: documented
- Why: 2.21.0 adds `reconcile: "declared"` (environment variables, connection references, Custom APIs, duplicate rules and `solution.version` through `--deployment-settings`; rules and flows create or adopt only; removed roots can retire), `--authorize <key>` for destructive changes, `--allow-destructive`, and `data-fix.js`, which writes data. This may make the claim at `SKILL.md:39` (no skill is verified to create an environment variable or connection reference in a solution) stale, and a spec that retires components conflicts with "never deletes" (`HANDOFF.md:35`); `HANDOFF.md:52`'s one settings writer may need a qualifier. The schema page still says forms are never deleted, so `model-driven.md:8` holds. Untested; the claim needs a Dev trial and an owner decision on whether a plan may name a retire or destructive spec, and the edits are under `skills/` (excluded here). A milestone: trial model-apps 2.25.0 in Dev, then update the skill and HANDOFF.
- Link: none

## ECO-0010: Power BI modeling MCP v1.0.1: is the new refresh and permission work covered by --readonly
- Priority: P2
- Disposition: plan for later
- Status: open
- Release status: GA
- Found: 2026-11-10, mode A
- Verified: 2026-11-10
- Affects: .claude/agents/powerops-bi.md:32, docs/HANDOFF.md:103, docs/HANDOFF.md:144, docs/ecosystem/compatibility.md
- Sources: [powerbi-modeling-mcp v1.0.1](https://github.com/microsoft/powerbi-modeling-mcp/releases/tag/v1.0.1) (published 2026-11-06), [skills-for-fabric c13604a](https://github.com/microsoft/skills-for-fabric/tree/c13604ae6f4fb727ac7bc74784eeff4332433c63/plugins/powerbi-authoring) (observed 2026-11-10)
- Evidence: documented and tested
- Why: v1.0.1 adds typed TOM execution, refresh and permission features; skills-for-fabric's plugin starts `@microsoft/powerbi-modeling-mcp@latest`, so users get it without a pin. The README says `--readonly` prevents writes to the semantic model, but not whether refresh or permission changes count. `powerops-bi.md:32` relies on `--readonly` to block writes. Settling it needs a local test against a throwaway PBIP (and, for permissions, a workspace, so the person's yes), and the agent file is excluded here. The compatibility row records the version and the open question. Tested on 2026-11-10, locally, with no workspace and no sign-in, on npm `@microsoft/powerbi-modeling-mcp` 1.0.1 with its `linux-x64` package 1.0.1 (`--version` prints 1.0.1), the build the owner's `powerbi-authoring-local` server is pinned to, started over stdio with `--start --readonly --accepteula` and, to compare, with `--start --accepteula`. `tools/list` is the same in both modes (22 tools, the same descriptions and annotations), so the list shows nothing: the flag works per operation, and each tool's `Help` lists fewer operations in read-only mode. Against a throwaway TMDL model (one table, one measure, one role with a table permission) opened with `ConnectFolder`, read-only mode refused each of these with "is not permitted: The MCP Server is currently running in read-only mode": measure `Create`, `Update` and `Delete`; table `Delete`; role `Create` and `Delete`, `UpdatePermissions` and `DeletePermissions`; `RefreshWithXMLA` and `RefreshWithAPI` on the model, a table and a partition; transaction `Begin`; database `Update`; model `Rename`; and `DeployToFabric`. The model's files kept their hashes. In read-write mode the same `RefreshWithXMLA` went on to the model and failed there ("A disconnected object is read only and cannot be refreshed"), so the refusal comes from the flag. So `--readonly` covers the start of a refresh and every change to roles and table permissions, which is what the permission feature is (`security_role_operations`); the server lists no tool that changes access to a workspace, and no tool or operation named for the typed TOM execution of the release notes. What read-only mode does not block: `ExportToTmdlFolder` and `ExportToBimFile` write files at any path given, and an export into the folder of a second model replaced that model's table file (the server's audit log records it as `ReadByPowerBIMCP`); `database_operations` `Create` makes a new offline model in memory; and `Help` still lists `CancelRefreshWithAPI`, `CheckStatusOfRefreshWithAPI`, DAX `ClearCache`, every `trace_operations` operation and `ConnectFabric`. Not tested, because each needs a workspace or a running engine: `CancelRefreshWithAPI`, `ClearCache` and the trace operations (on the offline model the last two failed because an offline connection does not support them, not with the read-only message), and any call after `ConnectFabric`. So `powerops-bi.md:32` is too strong where it says the flag "is what blocks its writes": the flag blocks changes to the model, not file exports. `HANDOFF.md:103` and `HANDOFF.md:144` do not name the flag and still hold. Still open: the owner's go-ahead for a corrected sentence in `powerops-bi.md:32` (an edit there needs the goldens recipe) and for naming `--readonly` in `HANDOFF.md:103`; and whether read-only mode lets `CancelRefreshWithAPI` stop a running refresh, which needs a workspace and the person's yes.
- Link: none

## ECO-0011: Smoke pins are behind Claude Code and Codex; what a move must test
- Priority: P2
- Disposition: plan for later
- Status: open
- Release status: GA
- Found: 2026-11-10, mode A
- Verified: 2026-11-10
- Affects: .github/workflows/skills-smoke.yml:22, .github/workflows/skills-smoke.yml:24, tests/smoke/skills-smoke.sh, docs/HARNESS.md, docs/ecosystem/compatibility.md
- Sources: [Claude Code changelog](https://github.com/anthropics/claude-code/blob/main/CHANGELOG.md) (observed 2026-11-10), [Codex rust-v0.166.0](https://github.com/openai/codex/releases/tag/rust-v0.166.0) (published 2026-10-29), [Codex rust-v0.167.0](https://github.com/openai/codex/releases/tag/rust-v0.167.0) (published 2026-11-06)
- Evidence: documented
- Why: The pins are Claude Code 2.1.303 and Codex 0.163.0; npm showed 2.1.322 (stable 2.1.311) and 0.167.0 on 2026-11-10. Return 6 found no change that breaks a claim, but several: some `claude -p` failures that exited 0 now exit 1; Codex 0.166.0 keeps approval settings on resume (`codex exec resume` in `skills-smoke.sh` passes no `-s`); 0.166.0 and 0.167.0 share eligible stdio MCP connections across chats. A pin moves only with a test of the new version, so a milestone: the full smoke run on each client, a hook-input recording, the gate probe, and a check of the MCP revision, whether two chats share one `powerops mcp` process and which chat gets the `deploy_apply` form.
- Link: none

## ECO-0012: pac 2.13.1 to 2.14.1 documents no change to the commands PowerOps calls
- Priority: P3
- Disposition: monitor
- Status: open
- Release status: GA
- Found: 2026-11-10, mode A
- Verified: 2026-11-10
- Affects: docs/examples/ephemeral-pr.yml, docs/HANDOFF.md:10, docs/BUILD-PLAN.md:696, docs/ecosystem/compatibility.md
- Sources: [Microsoft.PowerApps.CLI.Tool 2.14.1](https://www.nuget.org/packages/Microsoft.PowerApps.CLI.Tool/2.14.1) (published 2026-10-21)
- Evidence: documented
- Why: The 2.14 release notes list only dependency updates and 2.13 adds US government cloud sign-in; nothing touches `pac org who`, `pac solution list`, `pac power-automate list-cloud-flows --json`, `pac admin list --json` or `pac solution import`. PowerOps is still tested with 2.13.1 and the examples pin 2.14.1. Look again when NuGet shows a version after 2.14.1, or before the "tested with 2.13.1" line moves, which needs those five commands observed on the new version.
- Link: none

## ECO-0013: pac documents --json on env who and solution list, while PowerOps parses their text
- Priority: P3
- Disposition: plan for later
- Status: open
- Release status: GA
- Found: 2026-11-10, mode A
- Verified: 2026-11-10
- Affects: AGENTS.md:37, AGENTS.md:38, docs/examples/README.md:253, tests/fixtures/pac/org-who.txt
- Sources: [pac env](https://learn.microsoft.com/power-platform/developer/cli/reference/env) (updated 2026-09-11), [pac solution](https://learn.microsoft.com/power-platform/developer/cli/reference/solution) (updated 2026-04-02)
- Evidence: documented
- Why: Not a change since the baseline (both pages are older than 2.13.1's release), but first read here: `--json` is documented with no field names, so a switch would need a capture of each command's JSON on 2.14.1 against a tenant, which needs the person's yes; decision 15 ties the `org who` parse to `--env`. `docs/examples/README.md:253` says Learn documents neither the JSON stdout nor the fields of `pac admin list --json`; the flag is documented, the fields are not. A milestone: capture and switch, or record why not.
- Link: none

## ECO-0014: pac MCP server: both local builds list the five tools the gate names, and dnx is a second launch route
- Priority: P3
- Disposition: no action
- Status: closed
- Release status: GA
- Found: 2026-11-10, mode A
- Verified: 2026-11-10
- Affects: docs/HANDOFF.md:95, docs/ecosystem/compatibility.md
- Sources: [Use the Power Platform CLI MCP server](https://learn.microsoft.com/power-platform/developer/howto/use-mcp) (updated 2026-10-26), [Microsoft.PowerApps.CLI.Tool 2.14.1](https://www.nuget.org/packages/Microsoft.PowerApps.CLI.Tool/2.14.1) (published 2026-10-21)
- Evidence: documented and tested
- Why: Learn shows a second launch route, `dnx Microsoft.PowerApps.CLI.Tool --yes copilot mcp --run`, and the 2.14.1 package declares the `McpServer` package type; the gate matches tool names, so the route changes nothing. On 2026-11-10 `pac copilot mcp --run` was asked only `initialize` and `tools/list` over stdio, from a temp folder, with no sign-in and no tenant call: `pac` 2.14.1 (serverInfo 2.14.0.0) listed 284 tools and `pac` 2.11.2 (serverInfo 2.11.0.0) listed 70, and both list all five names `HANDOFF.md:95` matches (`solution_import`, `pages_upload`, `pages_upload-code-site`, `copilot_publish`, `model_genpage_upload`). An earlier note that a 2.11.2 server listed none of them was wrong: a session's tool list hides those five because local permission rules deny them. The tools 2.14.1 adds that no matcher names are ECO-0019.
- Link: none

## ECO-0015: MCP 2026-07-28 replaces server-initiated elicitation, which the deploy_apply form uses
- Priority: P3
- Disposition: monitor
- Status: open
- Release status: GA
- Found: 2026-11-10, mode A
- Verified: 2026-11-10
- Affects: src/PowerOps.Cli/McpCommand.cs:131, docs/FOUNDATION.md:189, docs/ecosystem/compatibility.md
- Sources: [MCP specification 2026-07-28 changelog](https://modelcontextprotocol.io/specification/2026-07-28/changelog) (observed 2026-11-10), [MCP release 2026-07-28](https://github.com/modelcontextprotocol/modelcontextprotocol/releases/tag/2026-07-28) (published 2026-07-28)
- Evidence: documented
- Why: The baseline was revision 2025-06-18 (the oldest the repository cites); 2025-11-25 and 2026-07-28 followed. `McpCommand.cs` already stamps `resultType` for 2026-07-28. Under 2026-07-28 a server's `elicitation/create` request gives way to multi round-trip requests; `FOUNDATION.md:189` records the form shown over 2026-07-28 with Claude Code 2.1.312, so it works today through the SDK. Roots, sampling, logging and dynamic client registration are deprecated; PowerOps uses none. Look again when Dependabot moves `ModelContextProtocol.Core` or a client pin moves (ECO-0011).
- Link: none

## ECO-0016: Power Pages web roles to be merged with Dataverse security roles
- Priority: P2
- Disposition: monitor
- Status: open
- Release status: announced
- Found: 2026-11-10, mode A
- Verified: 2026-11-10
- Affects: docs/rules/PO021.md, docs/rules/PO022.md, docs/rules/PO023.md, skills/powerops-plan/references/power-pages.md
- Sources: [Roadmap item 576750](https://www.microsoft.com/microsoft-365/roadmap?id=576750) (updated 2026-10-16)
- Evidence: documented
- Why: Found by the review writer among the 26 roadmap items cut from brief 7: "merges Power Pages web roles and site users with Dataverse security roles and system users", each web role also a security role and each site user also a system user, with no migration; in development, preview July 2026 and GA November 2026 on the item. PO021 to PO023 reason about table permissions and web roles, so their wording or logic may need to follow. Only the roadmap says so; look again when a Learn page documents the unified model or the item shows launched.
- Link: none

## ECO-0017: Power Pages agent skills: a traditional-site plugin is announced and a governance skill is disabled
- Priority: P3
- Disposition: monitor
- Status: open
- Release status: announced
- Found: 2026-11-10, mode A
- Verified: 2026-11-10
- Affects: skills/powerops-plan/references/power-pages.md, docs/HANDOFF.md:21, docs/HANDOFF.md:108
- Sources: [Roadmap item 577900](https://www.microsoft.com/microsoft-365/roadmap?id=577900) (updated 2026-10-23), [power-platform-skills fc1625b](https://github.com/microsoft/power-platform-skills/commit/fc1625b) (published 2026-11-03)
- Evidence: documented
- Why: The roadmap announces plugin support in GitHub Copilot CLI and Claude Code to create and edit traditional (non-SPA) sites and upload them with `pac` (in development); `power-pages.md` and HANDOFF route only code sites. The power-pages plugin added a `manage-governance` skill that applies tenant governance policies, shipped as `SKILL.disabled.md` pending product approval. Look again when the roadmap item launches or `plugins/power-pages/skills/manage-governance/SKILL.md` exists; either would need a HANDOFF route and possibly a gate route.
- Link: none

## ECO-0018: The weekly watch does not see inside the cited power-platform-skills plugins
- Priority: P3
- Disposition: plan for later
- Status: open
- Release status: GA
- Found: 2026-11-10, mode A
- Verified: 2026-11-10
- Affects: docs/ecosystem/sources.json
- Sources: [power-platform-skills compare aed70bf...7322abb](https://github.com/microsoft/power-platform-skills/compare/aed70bf...7322abb) (observed 2026-11-10)
- Evidence: documented
- Why: This review read the plugin folders once, from `aed70bf` to `7322abb` (32 commits in 17 days): code-apps (3 commits) and model-apps (12 or 13) touched files the repository cites and gave ECO-0005, ECO-0008 and ECO-0009; canvas-apps (0), pcf (7), power-automate (2), power-pages (6 or 7) and mcp-apps (1) touched none. The `marketplace.json` source saw none of it. Folder-level watching is worth it, but narrowed to the cited files of code-apps and model-apps (for example `plugins/model-apps/CHANGELOG.md` and `plugins/code-apps/shared/shared-instructions.md`) with the existing github-commits kind, because whole folders would report on most weeks. Not milestone-sized: an inventory change after step 7 measures the first weekly rate.
- Link: none

## ECO-0019: pac 2.14.1's MCP server has write tools that no hook matcher names
- Priority: P2
- Disposition: plan for later
- Status: done
- Release status: preview
- Found: 2026-11-10, mode A
- Verified: 2026-11-10
- Affects: docs/HANDOFF.md:70, docs/HANDOFF.md:95, docs/HANDOFF.md:108, src/PowerOps.Core/Deploy/ImportCall.cs:63, docs/ecosystem/compatibility.md
- Sources: [Use the Power Platform CLI MCP server](https://learn.microsoft.com/power-platform/developer/howto/use-mcp) (updated 2026-10-26), [Microsoft.PowerApps.CLI.Tool 2.14.1](https://www.nuget.org/packages/Microsoft.PowerApps.CLI.Tool/2.14.1) (published 2026-10-21), [pac power-pages](https://learn.microsoft.com/power-platform/developer/cli/reference/power-pages) (updated 2026-09-11), [pac copilot-studio](https://learn.microsoft.com/power-platform/developer/cli/reference/copilot-studio) (updated 2026-10-23), [pac environment-management](https://learn.microsoft.com/power-platform/developer/cli/reference/environment-management) (updated 2026-09-11)
- Evidence: documented and tested
- Why: The `tools/list` probe of ECO-0014 on `pac` 2.14.1 lists `app-management_install-application-package` ("(Preview) Installs an app package by unique name into a target environment"). The gate blocks the Bash form of an application package install (`docs/COMMANDS.md:136`), but the hook matcher at `HANDOFF.md:70` and the gate's `McpRoutes` name only the five tools of ECO-0014, so through pac's MCP server the install runs without the gate. The same build lists 97 tools in the preview `power-pages` (40), `copilot-studio` (16) and `environment-management` (41) groups, writes among them (`power-pages_upload-certificate`, website create and delete, agent delete, environment copy, reset and delete); pac 2.11.2 lists none of these. P2, not P1: `HANDOFF.md:108` already says a new pac MCP tool that installs, imports, uploads or publishes passes until the gate gets a route, so no route, rule or doc claim is wrong, and the tools are preview; but the gap is now real, not hypothetical. Plan for later, in ECO-0008's gate milestone: an MCP route and a matcher name for `app-management_install-application-package`, the HANDOFF list of unrouted MCP groups, and an owner decision on routes for the preview groups. Correction, M85: 222 of the 284 tools are preview, in 16 groups, 12 with writes (`environment-management`, `licensing`, `power-pages`, `governance`, `copilot-studio`, `authorization`, `workflows-agent`, `managed-identity`, `analytics`, `dynamics`, `app-management`, `user-management`) and 4 read-only (`connectivity`, `power-automate`, `power-apps`, `resource-query`), not 97 in three. Fixed in M85: the gate routes and the hook matcher names `app-management_install-application-package`, and `docs/HANDOFF.md` lists the groups and gives deny rules to copy, by the owner's decision. The Claude Code list was tested on 2026-11-10 on Claude Code 2.1.322 with `pac` 2.14.1 (server 2.14.0.0), a headless session that called no tool and contacted no tenant: with no deny rules it listed 282 `mcp__pac-cli__` tools, with all six gated names and 207 tools in the 11 groups; with the list it listed 69, none of the six and none of the 11 groups, and `auth_who` and `connectivity_list-connectors` remained. Claude Code listed 282 of the 284 tools the server offers (`licensing_create-isv-contract` and `licensing_update-isv-contract` were in neither run; cause not confirmed). Codex's `enabled_tools` is documented and was not tried in a client.
- Link: docs/plans/m85-gate-routes/plan.md (step 2, commit `58e21b0`; step 4, commit `16e7a5a`)

## Review log

- 2026-11-10, mode A: 19 automated read, 0 not checked, 10 by hand; ECO-0001, ECO-0002, ECO-0003, ECO-0004, ECO-0005, ECO-0006, ECO-0007, ECO-0008, ECO-0009, ECO-0010, ECO-0011, ECO-0012, ECO-0013, ECO-0014, ECO-0015, ECO-0016, ECO-0017, ECO-0018, ECO-0019. Cheap path: dataverse-skills@v1.18.0, power-platform-skills@7558c8e974b199747ce8e3c0c922ec415ed48585, skills-for-copilot-studio@9c42d3a612c6e06389b6e5b4eed579aaed1a8b63, copilot-studio-plugin@4014ae689d1228d86cb3aaab938b1e66f8e2e10d, skills-for-fabric@c13604ae6f4fb727ac7bc74784eeff4332433c63, power-cat-overflow@1f81439b98d7aa5fa389a6d4fcc2b261de073afc, power-cat-overpage@f01f9ca893596fe5fb9f127c49736a2fa03b48fc, pa-persistence@0.8.2735.117-preview, docs-important-changes@2cc849531308d84aaf81cb03f5756f19a579f6db, docs-alm-yaml@62494850aa43e90630db7eb82672faeb0bfcae76, docs-alm-settings@8ad877ec951e492c0a1a3aed01765f21c3113df8, docs-git-integration-faqs@da4eb304bd15d1e8ee2330104f35b9d2eb065bdf, docs-pac-mcp@76e446549b0c94831c50ef2e48df14e885faa519, docs-pa-cli@8e42d75ad9f2151be916b75dafede418f7ef3e76, docs-canvas-yaml@6d1e70ba261f80bddd70a03fdfaf830187f1f59a. In entries: pac-cli@2.14.1 (ECO-0012), pa-cli@1.4.1 (ECO-0001), mcp-spec@2026-07-28 (ECO-0015), docs-dataverse-mcp@69cf4c7a9213db15b66ae961757de7120093163f (ECO-0004); the 5a observations pa-cli@1.6.0 and docs-flow-limits@82f95a85c3d523e03c52a06b50d5daac1a20a4a0 went with their sources' changes (ECO-0001, ECO-0002). No baseline (first observation, value recorded): pa-cli as the CLI package, m365-roadmap, power-bi-latest, fabric-roadmap. By hand: copilot-studio-whats-new, copilot-studio-mcp, power-bi-authoring-mcp, power-bi-latest, programmability, claude-code, codex, docs-flow-limits, release-plans (archived, removed, ECO-0003) and m365-roadmap (all 66 Power Platform items modified 2026-10-13 or later read by title and status, 10 opened). Not read: fabric-roadmap, whose page renders only through script (a fetch gets "Loading features"); it keeps its date and stays due. Folder-level watching of the power-platform-skills plugins: worth it, narrowed to the cited files of code-apps and model-apps (ECO-0018), after step 7 measures the weekly rate.
- 2026-11-10, mode A: 0 automated read, 0 not checked, 1 by hand; no entries. By hand: fabric-roadmap, read in a browser and through the page's own feed (see step 5 of the procedure; the feed has no modified date, so there was no reading window): Power BI (104 items, 17 planned) and Fabric Developer Experiences (97 items, 2 planned) read by title, status and date, 8 opened in full; in the other 13 products, the planned items and those dated Q3 2026 or later were searched for the CI/CD, Git integration, deployment pipeline, PBIP, PBIR, TMDL, semantic model, MCP, CLI and Power Platform terms (Data Factory and Data Warehouse by pattern, their feeds not being valid JSON). Nothing contradicts `skills/powerops-plan/references/power-bi.md`, `docs/HANDOFF.md` or `.claude/agents/powerops-bi.md`: "Developer Mode GA" (PBIR on by default for everyone) is still listed as planned for Q3 2026, while Learn's report folder page, read the same day, says PBIR is generally available and the default, so the claim keeps its Learn source; no item says deployment pipelines support PBIR reports; "Deployment Plan" (public preview, Q4 2026) adds an order and pre- and post-deploy actions to Git integration and deployment pipelines, and is no route PowerOps names; the Fabric CLI's `deploy` (GA, Q1 2026), the bulk item definition API (GA, Q3 2026) and the Fabric Core MCP Server (GA, Q3 2026) are Fabric writes, for which `docs/HANDOFF.md` already says the gate has no route; "Developer mode (PBIP) external changes reload" (GA planned Q1 2027) and "Organizational themes for Power BI reports" (GA planned Q4 2026) touch no claim. This supersedes "Not read: fabric-roadmap" in the line above.
