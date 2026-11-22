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
- Why: v1.0.1 adds typed TOM execution, refresh and permission features; skills-for-fabric's plugin starts `@microsoft/powerbi-modeling-mcp@latest`, so users get it without a pin. The README says `--readonly` prevents writes to the semantic model, but not whether refresh or permission changes count. `powerops-bi.md:32` relies on `--readonly` to block writes. Settling it needs a local test against a throwaway PBIP (and, for permissions, a workspace, so the person's yes), and the agent file is excluded here. The compatibility row records the version and the open question. Tested on 2026-11-10, locally, with no workspace and no sign-in, on npm `@microsoft/powerbi-modeling-mcp` 1.0.1 with its `linux-x64` package 1.0.1 (`--version` prints 1.0.1), the build the owner's `powerbi-authoring-local` server is pinned to, started over stdio with `--start --readonly --accepteula` and, to compare, with `--start --accepteula`. `tools/list` is the same in both modes (22 tools, the same descriptions and annotations), so the list shows nothing: the flag works per operation, and each tool's `Help` lists fewer operations in read-only mode. Against a throwaway TMDL model (one table, one measure, one role with a table permission) opened with `ConnectFolder`, read-only mode refused each of these with "is not permitted: The MCP Server is currently running in read-only mode": measure `Create`, `Update` and `Delete`; table `Delete`; role `Create` and `Delete`, `UpdatePermissions` and `DeletePermissions`; `RefreshWithXMLA` and `RefreshWithAPI` on the model, a table and a partition; transaction `Begin`; database `Update`; model `Rename`; and `DeployToFabric`. The model's files kept their hashes. In read-write mode the same `RefreshWithXMLA` went on to the model and failed there ("A disconnected object is read only and cannot be refreshed"), so the refusal comes from the flag. So `--readonly` covers the start of a refresh and every change to roles and table permissions, which is what the permission feature is (`security_role_operations`); the server lists no tool that changes access to a workspace, and no tool or operation named for the typed TOM execution of the release notes. What read-only mode does not block: `ExportToTmdlFolder` and `ExportToBimFile` write files at any path given, and an export into the folder of a second model replaced that model's table file (the server's audit log records it as `ReadByPowerBIMCP`); `database_operations` `Create` makes a new offline model in memory; and `Help` still lists `CancelRefreshWithAPI`, `CheckStatusOfRefreshWithAPI`, DAX `ClearCache`, every `trace_operations` operation and `ConnectFabric`. Not tested, because each needs a workspace or a running engine: `CancelRefreshWithAPI`, `ClearCache` and the trace operations (on the offline model the last two failed because an offline connection does not support them, not with the read-only message), and any call after `ConnectFabric`. So `powerops-bi.md:32` and `USE-WITH-AGENTS.md:60` were too strong where they said the flag "is what blocks its writes": the flag blocks changes to the model, not file exports. `HANDOFF.md:103` and `HANDOFF.md:144` did not name the flag and still hold. Corrected on 2026-11-10, after the owner's yes in chat: both sentences now say what the flag refuses and that it does not refuse the file exports, the agent is told never to call the two exports, and the plugin copy was regenerated by the goldens recipe with its rewrite in `PluginAgents.cs` changed to match. `HANDOFF.md:103` names the flag and this test since the owner's second yes the same day. Still open: whether read-only mode lets `CancelRefreshWithAPI` stop a running refresh, which needs a workspace and the person's yes. Tested further on 2026-11-10, on the same build and still with no workspace and no sign-in: against an offline TMDL model, `CheckStatusOfRefreshWithAPI` and `CancelRefreshWithAPI` with a made-up request id, on the model, a table and a partition, returned the same error in read-only and in read-write mode ("only supported for Fabric cloud connections"), while `RefreshWithAPI` in the same read-only run got the read-only refusal and in read-write mode got the Fabric-only error. So the read-only check comes before the connection check and does not cover the status call or the cancel: the flag does not refuse `CancelRefreshWithAPI`, which agrees with `Help` listing it in read-only mode. Still untested, because no workspace on Premium, Premium per user or Embedded capacity was at hand: that the call then stops a refresh running in a workspace, DAX `ClearCache` and the trace operations on a live connection, and any call after `ConnectFabric`; the owner left the live test for a later run on Windows. Corrected on 2026-11-10, after the owner's yes in chat: `powerops-bi.md:32` now names `CancelRefreshWithAPI` beside the file exports as not refused and tells the agent never to call it, the plugin copy was regenerated by the goldens recipe with its rewrite in `PluginAgents.cs` changed to match, and `HANDOFF.md:103` and `USE-WITH-AGENTS.md:60` say the flag does not refuse the cancel.
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
- Affects: docs/examples/ephemeral-pr.yml, docs/HANDOFF.md:10, docs/BUILD-PLAN.md:720, docs/ecosystem/compatibility.md
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

## ECO-0020: Copilot Studio's new experience, and Learn's new name for the harness
- Priority: P2
- Disposition: implement now
- Status: done
- Release status: unverified
- Found: 2026-11-12, mode B
- Verified: 2026-11-12
- Affects: skills/powerops-plan/references/copilot-studio.md
- Sources: [Microsoft Copilot harness overview](https://learn.microsoft.com/microsoft-copilot-studio/agents-experience/overview) (observed 2026-11-12), [Access standard harness agents and agent flows](https://learn.microsoft.com/microsoft-copilot-studio/agents-experience/switch-experiences) (updated 2026-11-06), [copilot-studio-plugin](https://github.com/microsoft/copilot-studio-plugin) (observed 2026-11-12)
- Evidence: documented
- Why: Learn now names the harness that `copilot-studio-plugin` builds for the Microsoft Copilot harness, and says the harness is chosen when an agent is created: **Other ways to build** on the home page, or **New experience** turned off on Home, creates a standard-harness agent, and **Open classic experience** applies to the session only. Trial 3 met it (S23): observed in one run (2026-11-11), the new experience's home page built a new agent on that harness from its Agent card. Learn's pages were not read for a release label. Done for the references: `skills/powerops-plan/references/copilot-studio.md` section 1 maps the new name to the old one and says how to create a standard-harness agent (M95). The old name stays, by decision Q3 of the M95 plan, in the reference's other lines, `.claude/agents/powerops-copilot-studio.md`, `docs/COMMANDS.md`, `docs/HANDOFF.md`, `docs/rules/PO027.md` and a code comment in `src/PowerOps.Core/Rules/Agents.cs`: it still matches Learn's URL slugs and the plugin's README ("GHCP Harness"), so a reader of the mapped reference isn't misled. A rename across those files is left for a milestone that changes them.
- Link: docs/plans/m95-references/plan.md

## ECO-0021: Ubuntu 24.04 and later keep the old IANA names that .NET asks for in tzdata-legacy
- Priority: P1
- Disposition: implement now
- Status: done
- Release status: GA
- Found: 2026-11-14, mode C
- Verified: 2026-11-14
- Affects: src/PowerOps.Core/Flows/ExpressionFunctions.cs
- Sources: [Ubuntu 24.04 LTS release notes: tzdata package split](https://discourse.ubuntu.com/t/ubuntu-24-04-lts-noble-numbat-release-notes/39890) (observed 2026-11-14), [dotnet/runtime#110488](https://github.com/dotnet/runtime/issues/110488) (updated 2025-02-11), [actions/runner-images toolset-2604.json](https://github.com/actions/runner-images/blob/main/images/ubuntu/toolsets/toolset-2604.json) (observed 2026-11-14)
- Evidence: documented and tested
- Why: On 2026-11-14 `ubuntu-latest` moved to `ubuntu-26.04`, and the `build (ubuntu-latest)` job of CI failed `convertFromUtc` for `India Standard Time` and `Nepal Standard Time` (PR #161, run 43632699469). Tested in containers with .NET 10: on Linux, .NET maps the Windows id through ICU to `Asia/Calcutta` and `Asia/Katmandu` (ICU 74 on 24.04, ICU 78 on 26.04), and the Ubuntu `tzdata` of 24.04 and 26.04 ships those names only in `tzdata-legacy`; without that package the lookup throws `TimeZoneNotFoundException`, while `Asia/Kolkata` and `Asia/Kathmandu` resolve. The other 22 supported zones map to names in `tzdata`. Microsoft's .NET runtime images install `tzdata-legacy`, and the runner images' toolsets list only `tzdata` for both versions, so what installed `tzdata-legacy` on the 24.04 image is not known. Users on such distros were hit too, not only CI, so the fix is in code rather than a pinned `runs-on` or a CI install: the simulator tries the Windows id first and falls back to the current IANA name for these two zones. The existing `Every_supported_zone_gives_its_offset_from_its_first_year_to_2030` rows are the regression test: they failed with a zoneinfo folder without the two names (`TZDIR`) and pass with the fix, and CI's ubuntu job now runs without `tzdata-legacy`. `compatibility.md` has no row for GitHub runner images.
- Link: https://github.com/Chrome71/PowerOps/pull/162

## ECO-0022: The Developer Plan covers rows only in an unmanaged developer environment, and has run and database limits
- Priority: P2
- Disposition: implement now
- Status: done
- Release status: deprecated
- Found: 2026-11-15, mode B
- Verified: 2026-11-15
- Affects: skills/powerops-plan/references/licensing.md, skills/powerops-review-plan/SKILL.md
- Sources: [Power Apps Developer Plan](https://learn.microsoft.com/power-platform/developer/plan) (updated 2026-10-26), [Deep dive on specific licenses](https://learn.microsoft.com/power-platform/admin/power-automate-licensing/deep-dive-on-specific-license) (updated 2026-10-17), [Important changes coming](https://learn.microsoft.com/power-platform/important-changes-coming) (updated 2026-11-02), [Licensing requirements for managed environments](https://learn.microsoft.com/power-platform/admin/managed-environment-licensing) (updated 2026-08-31)
- Evidence: documented
- Why: Briefed as: the Developer Plan covers Dataverse and premium connectors in a developer environment, not for others, without dataflows, business process flows or desktop flows, with 750 flow runs a month and 2 GB of database. Learn holds the coverage, the gaps and the numbers, and changes three things. The plan is for development and test only, and lets the maker share apps and add coworkers as makers for them, so "not for others" becomes "not production". From 2026-11-15 no new unmanaged developer environment can be created, existing ones are suspended from 2026-12-15 and deleted from 2027-01-15, and running apps and flows in the managed developer environments that remain needs a premium licence on top of the plan, so the plan covers rows only in an unmanaged one. The plan page doesn't say whether the limits hold in a managed developer environment. Learn says nothing on Copilot Studio or Power Pages under the plan. Done (M109): `licensing.md` gives `POWERAPPS_DEV` what Learn says it covers and says how `Held` reads it, its notes carry the dates and the limits, and `powerops-review-plan` item 3 makes a developer environment's flows passing 750 runs a month a Medium finding.
- Link: docs/plans/m109-agents-use-tools/plan.md

## ECO-0023: msagent, the Copilot Studio CLI, deploys, publishes, promotes and deletes agents with no gate route
- Priority: P1
- Disposition: plan for later
- Status: open
- Release status: preview
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: docs/HANDOFF.md:154, docs/HANDOFF.md:155, src/PowerOps.Core/Deploy/ImportCall.cs:90, CLAUDE.md:42, skills/powerops-plan/references/copilot-studio.md:22, docs/COMMANDS.md:913
- Sources: [Deploy and promote a Copilot Studio agent (preview)](https://learn.microsoft.com/microsoft-365/copilot-managed-runtime/developer/agents/cli/deploy) (updated 2026-11-19), [msagent CLI command reference](https://learn.microsoft.com/microsoft-365/copilot-managed-runtime/developer/agents/cli/command-reference) (observed 2026-11-21), [Microsoft.Agents.Hosting.Cli](https://www.nuget.org/packages/Microsoft.Agents.Hosting.Cli) (observed 2026-11-21)
- Evidence: documented and tested
- Why: `msagent` (NuGet Microsoft.Agents.Hosting.Cli 0.2.154-beta, .NET 10, Windows and macOS) uploads a `.mcs.yml` workspace with `agent deploy`, publishes with `--publish`, promotes with `--deployment-name staging|production` outside solutions and `deploy apply`, and deletes with `agent delete`; `agent eval run`, `connection update` and `deployment update` also act on the environment; it runs on the user's sign-in (service principals are on the roadmap). Tested offline on 2026-11-21: the gate exits 0 on `msagent agent deploy --publish`, `msagent agent deploy --deployment-name production` and `msagent agent push`. HANDOFF:154-155 and CLAUDE.md:42 name only pac, pa and MCP routes; CLAUDE.md:42's general rule and the specialists' Bash limit still hold, so nothing is unsafe today, but Microsoft now documents a Prod promotion route the harness doesn't name. A milestone: name the writes in HANDOFF and CLAUDE.md, and an owner decision between a gate marker and a person-runs-it rule.
- Link: none

## ECO-0024: pac 2.16.1's MCP server adds two preview groups, and the group list goes stale with each pac release
- Priority: P2
- Disposition: plan for later
- Status: open
- Release status: preview
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: docs/HANDOFF.md:115, docs/HANDOFF.md:154, docs/ecosystem/compatibility.md:22, tests/PowerOps.Cli.Tests/HandoffDocTests.cs, docs/ecosystem/sources.json
- Sources: [Microsoft.PowerApps.CLI.Tool 2.16.1](https://www.nuget.org/packages/Microsoft.PowerApps.CLI.Tool/2.16.1) (published 2026-11-20), [Power Platform CLI and the Power Platform API](https://devblogs.microsoft.com/powerplatform/power-platform-cli-%e2%9d%a4%ef%b8%8f-power-platform-api/) (published 2026-09-17)
- Evidence: documented and tested
- Why: Asked only `initialize` and `tools/list` over stdio with no sign-in on 2026-11-21, `pac` 2.16.1 (serverInfo 2.16.0.0) lists 288 tools, 226 preview in 18 groups (2.14.1: 284, 222, 16); the new groups are `monitoring-hub` (`export-metrics`, described as a read but annotated not read-only, and the trigger `on-alert-triggered`) and `notification-service` (`get-active-notifications`, and `update-notification-status`, a user-level write that dismisses the caller's notifications). HANDOFF's "16 preview groups", its read-only list and HandoffDocTests' 16 names are stale and no deny glob covers the new write; the six gated names, the 11 globs (209 tools) and Codex's allow-list still hold. The groups come from the monthly Power Platform API spec and the release notes don't name them, so the pac-cli triage should run this probe on each version and keep its name list as the next baseline. Annotations sit in `inputSchema.annotations.readOnlyHint` on 277 of 288 tools, not in the standard field.
- Link: none
- Supersedes: ECO-0019

## ECO-0025: MCP write tools that HANDOFF's deny advice misses: FlowAgent, Fabric Core and the hosted Power BI Authoring server
- Priority: P2
- Disposition: plan for later
- Status: open
- Release status: GA
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: docs/HANDOFF.md:113, docs/HANDOFF.md:155, docs/ecosystem/compatibility.md:25, CLAUDE.md:42
- Sources: [power-platform-skills power-automate plugin at 5185663](https://github.com/microsoft/power-platform-skills/tree/51856637/plugins/power-automate) (published 2026-10-27), [Fabric Core MCP Server tools](https://learn.microsoft.com/rest/api/fabric/articles/mcp-servers/core-remote/tools-core-mcp-server) (observed 2026-11-21), [Power BI Authoring MCP server](https://learn.microsoft.com/power-bi/developer/mcp/power-bi-authoring-mcp) (updated 2026-11-17)
- Evidence: documented
- Why: FlowAgent v3.2.10 registers 62 tools, 39 read-only; HANDOFF names only `copy_flow`, `publish_flow` and `update_flow`, and misses `create_flow`, `edit_flow`, `disable_flow`, `delete_flow`, `restore_backup`, `run_flow`, `resubmit_run`, `cancel_run`, `cancel_all_runs`, `run_desktop_flow`, `smoke_test`, `pick_or_create_connection`, `fix_connection`, `delete_connection` and `invoke_operation`. The Fabric Core MCP Server (GA, `api.fabric.microsoft.com/v1/mcp/core`) has 25 write tools by Learn's Read-only column (workspaces, roles, items including `update_item_definition`, folders, OneLake shortcuts, data access roles and settings), separate from its 16 reads, so an exact-name deny list keeps the reads; the local `@microsoft/fabric-mcp` has `--read-only`. The hosted Power BI Authoring server (preview, Learn's recommended option) has no `--readonly`, so HANDOFF:113's flag control misses it, while Build-only permission still applies. Name all three in HANDOFF:155 and give deny lists in HANDOFF:113.
- Link: none

## ECO-0026: Advanced connector policies replace data-group checks with a default-deny allow-list and can block MCP servers
- Priority: P2
- Disposition: plan for later
- Status: open
- Release status: GA
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: skills/powerops-review-plan/SKILL.md:38, docs/examples/README.md:120, docs/examples/README.md:239, docs/examples/README.md:281, skills/powerops-review-security/SKILL.md:71
- Sources: [Advanced connector policies](https://learn.microsoft.com/power-platform/admin/advanced-connector-policies) (updated 2026-11-20), [Manage advanced connector policies programmatically](https://learn.microsoft.com/power-platform/admin/programmability-tutorial-manage-advanced-connector-policies) (updated 2026-11-21)
- Evidence: documented
- Why: ACP blocks every certified and MCP connector unless allowed (new ones too), can allow or block single actions, can block a whole MCP server but not single tools, and in ACP-only mode ignores classic data policies; custom and HTTP connectors stay under classic policies. It is GA per workload as design-time enforcement ships (Power Automate, then Copilot Studio, then Power Apps), runtime-only before. PowerOps' DLP checks assume the classic "allowed together" model and never mention ACP, so a plan can pass review and then fail in Test on one action not on the allow-list. `powerops-review-security` is a kept role: any change there follows the M96 procedure.
- Link: none

## ECO-0027: PO037 counts the deprecated mobile notification as telling someone
- Priority: P2
- Disposition: plan for later
- Status: open
- Release status: deprecated
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: src/PowerOps.Core/Rules/PO037CatchTellsNoOne.cs, docs/rules/PO037.md
- Sources: [Important changes coming](https://learn.microsoft.com/power-platform/important-changes-coming) (updated 2026-11-18), [Notifications connector](https://learn.microsoft.com/connectors/flowpush/) (updated 2026-09-22)
- Evidence: documented
- Why: Since 2026-08-31 the Power Automate mobile app no longer receives "Send me a mobile notification" (`flowpush` `SendNotification`), and the whole Notifications connector is now marked deprecated. PO037 counts any connector action as outward by type, so a catch path whose only notice is a flowpush action passes. Fix by excluding `shared_flowpush` from the outward count or by a rule for its deprecated actions; a fixture should confirm the exported action type first.
- Link: none

## ECO-0028: Agent flows and AI Builder actions bill in Copilot Credits, and inventory reads them as Power Automate licence needs
- Priority: P2
- Disposition: plan for later
- Status: open
- Release status: GA
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: src/PowerOps.Core/Readers/SolutionParser.cs, src/PowerOps.Core/Model/Solution.cs, src/PowerOps.Core/Output/InventoryReport.cs, src/PowerOps.Core/Rules/PO039RareTrigger.cs, skills/powerops-plan/references/licensing.md, .claude/agents/powerops-automate.md
- Sources: [Agent flows overview](https://learn.microsoft.com/microsoft-copilot-studio/flows-overview) (updated 2026-11-03), [Workflow table reference](https://learn.microsoft.com/power-apps/developer/data-platform/reference/entities/workflow) (observed 2026-11-21), [End of AI Builder credits](https://learn.microsoft.com/ai-builder/endofaibcredits) (updated 2026-10-02)
- Evidence: documented
- Why: Agent flows bill each action in Copilot Credits, skip request limits and stop running with no credits (`InsufficientMcsCredits`); `ModernFlowType` (0 cloud, 1 Copilot Studio, 2 M365 Copilot) tells them apart, but PowerOps reads only Category, so inventory gives agent flows Power Automate drivers. Since 2026-11-01 no new AI Builder add-ons or seeded credits; existing credits last to contract end, then AI Builder uses Copilot Credits and is blocked when neither is left (the licence types page still lists 5,000 a month: sources disagree); inventory has no AI Builder driver. Both need a fixture: an exported agent flow and an AI Builder action. Partly closed by M123: `inventory` reads `ModernFlowType` and has `agent-flow` and `ai-builder-action`, and `licensing.md` covers both. Deferred: PO039 (a change moves `analyze` output) and `.claude/agents/powerops-automate.md`. Correction: end of sales of the add-ons was 2025-11-01; 2026-11-01 is their end of life and the seeded credits'. Also deferred: `custom-connector`'s sentence that a flow needs Power Automate Premium or a Process licence still prints for a custom connector that only an agent flow uses. The fixture `tests/fixtures/flow-credits/` is hand-written in the shape of the Power-CAT Copilot Studio Kit's export; a fixture from an export of the project's own stays open with the deferred parts.
- Link: docs/plans/m123-cost-credibility/plan.md

## ECO-0029: The request-limit transition ends from April 2027, and one owner's flows then share one daily limit
- Priority: P2
- Disposition: plan for later
- Status: open
- Release status: announced
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: skills/powerops-plan/references/data.md:73
- Sources: [Requests limits and allocations](https://learn.microsoft.com/power-platform/admin/api-request-limits-allocations) (updated 2026-11-10)
- Evidence: documented
- Why: After a phased rollout from April 2027, flows running under one user share that user's limit across environments (40k a day Premium, 6k seeded) instead of a per-flow limit; data.md:73 checks the cost line per flow. Cloud flows that agents trigger share a 250,000-a-day tenant pool, and agent flows are metered in credits. Look again when the rollout starts.
- Link: none

## ECO-0030: model-apps 2.26.0 to 2.28.0 add a shared-page stop, stable configuration rows and off-disk secrets
- Priority: P2
- Disposition: plan for later
- Status: open
- Release status: GA
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: skills/powerops-plan/references/model-driven.md:18, skills/powerops-implement/SKILL.md:41, docs/ecosystem/compatibility.md
- Sources: [model-apps CHANGELOG](https://github.com/microsoft/power-platform-skills/blob/main/plugins/model-apps/CHANGELOG.md) (observed 2026-11-21), [power-platform-skills 238f8ad](https://github.com/microsoft/power-platform-skills/commit/238f8ad9b5133d3fb3e70b243f01c44f74a8524a) (published 2026-11-16)
- Evidence: documented
- Why: 2.28.0's `/genpage` asks before editing a page other apps share (an unattended run stops), and creates configuration rows once at stable ids; 2.27.0 keeps variable values, secret references and connection ids off disk, and a pending protected variable needs a retire-and-delete (stop list); 2.26.0 downloads declared variables and connection references. This widens ECO-0009: the Dev trial should use 2.28.0, and a plan touching a shared page must name the confirmation.
- Link: none
- Supersedes: ECO-0009

## ECO-0031: code-apps plugin: an env attribution form, a coming rename and a second push
- Priority: P2
- Disposition: plan for later
- Status: open
- Release status: preview
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: docs/HANDOFF.md:23, docs/HANDOFF.md:35, docs/HANDOFF.md:111, docs/HANDOFF.md:159, docs/COMMANDS.md:876, docs/plans/m85-gate-routes/decisions.md:36, skills/powerops-implement/SKILL.md:40, skills/powerops-plan/references/code-apps.md:15, tests/PowerOps.Cli.Tests/SkillsCommandTests.cs:321
- Sources: [power-platform-skills 01d846c](https://github.com/microsoft/power-platform-skills/commit/01d846cbe2f16abcd22b3ccb1fc7b50e28811fdc) (published 2026-11-13), [power-platform-skills 28c209c](https://github.com/microsoft/power-platform-skills/commit/28c209c9eec2b7dedd598b3e211104a0b04e94c7) (published 2026-11-20), [shared-instructions.md](https://github.com/microsoft/power-platform-skills/blob/main/plugins/code-apps/shared/shared-instructions.md) (observed 2026-11-21)
- Evidence: documented and tested
- Why: 01d846c deleted the `set-cli-origin.js` hook that HANDOFF:159 cites; `cli-binary.md` now prefixes CLI calls with `env -u PA_PLUGIN_REPORT_AGENT PA_CLI_ORIGIN=...`, and applied to the deploy skill's `$PA app push` the gate blocks it with "the command holds an import that the gate can't read" instead of the route line (tested 2026-11-21; fails closed); M85 decision Q4 rested on Microsoft not using `env`. 28c209c prepares a rename to `code-apps` with no date, which HANDOFF, the implement skill and a test pin. `create-code-app` now has a pre-approved Step 7 push and a confirmed Step 10 push, while code-apps.md:15 names one. Trigger for the rename: `plugins/code-apps/plugin.json` `name` becomes `code-apps`.
- Link: none

## ECO-0032: Smoke pins are 27 Claude Code builds and 4 Codex minors behind, and codex exec now needs project trust
- Priority: P2
- Disposition: plan for later
- Status: open
- Release status: GA
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: .github/workflows/skills-smoke.yml:22, .github/workflows/skills-smoke.yml:24, tests/smoke/skills-smoke.sh, tests/eval/roles/run.sh:227, docs/USE-WITH-AGENTS.md:87, docs/HARNESS.md:23, docs/HANDOFF.md:109, docs/ecosystem/compatibility.md
- Sources: [Claude Code changelog](https://github.com/anthropics/claude-code/blob/main/CHANGELOG.md) (observed 2026-11-21), [Codex rust-v0.169.0](https://github.com/openai/codex/releases/tag/rust-v0.169.0) (published 2026-11-13), [Codex rust-v0.171.0](https://github.com/openai/codex/releases/tag/rust-v0.171.0) (published 2026-11-20), [openai/codex#60041](https://github.com/openai/codex/pull/60041) (published 2026-11-07)
- Evidence: documented
- Why: Pins are Claude Code 2.1.303 and Codex 0.163.0; npm shows 2.1.330 (stable 2.1.323) and GitHub 0.171.0. Codex 0.169.0 keeps project configuration and MCP servers off in `codex exec` in untrusted folders even with `workspace-write`, so the smoke and role eval may need `--trust` or run without skills and hooks (untested). The move's test list adds: exec trust, the Guardian review of recorded MCP calls (0.168.0) against the `deploy_apply` form, shared MCP launch directories (0.171.0) and a real Code Mode session; none of the read changelog lines breaks a claim.
- Link: none
- Supersedes: ECO-0011

## ECO-0033: Power BI reference: Report is GA in Fabric Git integration and the List slicer is GA
- Priority: P2
- Disposition: plan for later
- Status: open
- Release status: GA
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: skills/powerops-plan/references/power-bi.md:17, skills/powerops-plan/references/power-bi.md:66, skills/powerops-plan/references/power-bi.md:75, skills/powerops-plan/references/power-bi.md:104, skills/powerops-plan/references/power-bi.md:120, docs/HANDOFF.md:26
- Sources: [Overview of Fabric Git integration](https://learn.microsoft.com/fabric/cicd/git-integration/intro-to-git-integration) (updated 2026-11-19), [fabric-docs 1a013ed](https://github.com/MicrosoftDocs/fabric-docs/commit/1a013ede0cd1964ecd0102a48709e7c23356e563) (published 2026-11-19), [November 2026 Power BI update](https://learn.microsoft.com/power-bi/fundamentals/desktop-latest-update) (updated 2026-11-17)
- Evidence: documented
- Why: fabric-docs removed "(preview)" from Report on the Git integration and deployment pipelines lists; Semantic model stays preview, so "reports and semantic models as preview" is half stale (deployment pipelines still don't support PBIR reports, so the route rule holds; Microsoft's deployment-pipelines skill lags Learn). The November update lists the List slicer with dropdown as GA, and the reference's preview mark keeps it out of sketches under gate 3. The edit touches a sketch-gate reference, so check AGENTS.md's sketch-eval rerun rule.
- Link: none

## ECO-0034: Copilot Studio plans should name Work IQ, the primary model and its retirement, and re-test prompts after auto-migration
- Priority: P2
- Disposition: plan for later
- Status: open
- Release status: GA
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: skills/powerops-plan/references/copilot-studio.md:18, skills/powerops-plan/references/copilot-studio.md:19, skills/powerops-plan/references/copilot-studio.md:20, .claude/agents/powerops-copilot-studio.md:14, .claude/agents/powerops-copilot-studio.md:42, .claude/agents/powerops-copilot-studio.md:43, .claude/agents/powerops-copilot-studio.md:51
- Sources: [Work IQ in Copilot Studio](https://learn.microsoft.com/microsoft-copilot-studio/agents-experience/add-work-iq) (updated 2026-11-12), [Select a primary AI model](https://learn.microsoft.com/microsoft-copilot-studio/authoring-select-agent-model) (updated 2026-11-20), [Prompt model availability](https://learn.microsoft.com/microsoft-copilot-studio/prompt-model-availability) (updated 2026-11-13)
- Evidence: documented
- Why: Work IQ joins every new Microsoft Copilot harness agent with all workloads and writes on by default, billed in credits, so a plan's tool list misses it. The standard harness defaults to the "GPT Chat Latest" alias, models retire on dated schedules, and a managed agent whose model can't be edited moves to the default without an import (`drift` doesn't see it); external providers need two admin settings and Anthropic is off in the EU/EFTA and UK. Prompts move to new models by 2026-12-02 and responses may differ, so flows parsing them need a re-test. A plan line each; specialist and plugin copies regenerate by the goldens recipe.
- Link: none

## ECO-0035: Copilot Studio harness pages: the toggle route, the upgrade URL, trigger credentials and Copilot Studio MCP 1.0.4
- Priority: P3
- Disposition: plan for later
- Status: open
- Release status: preview
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: skills/powerops-plan/references/copilot-studio.md:7, skills/powerops-plan/references/copilot-studio.md:11, skills/powerops-plan/references/copilot-studio.md:17, skills/powerops-plan/references/copilot-studio.md:53, skills/powerops-plan/references/copilot-studio.md:59, .claude/agents/powerops-copilot-studio.md:31, .claude/agents/powerops-copilot-studio.md:49, docs/ecosystem/compatibility.md:24, src/PowerOps.Core/Rules/Agents.cs:8
- Sources: [Access standard harness agents and agent flows](https://learn.microsoft.com/microsoft-copilot-studio/agents-experience/switch-experiences) (updated 2026-11-12), [Copilot Studio quickstart](https://learn.microsoft.com/microsoft-copilot-studio/fundamentals-get-started) (observed 2026-11-21), [Upgrade agents to the Microsoft Copilot harness](https://learn.microsoft.com/microsoft-copilot-studio/upgrade-to-microsoft-copilot-from-standard-harness) (updated 2026-11-12), [Add triggers (Microsoft Copilot)](https://learn.microsoft.com/microsoft-copilot-studio/agents-experience/authoring-add-event-trigger) (updated 2026-11-18), [@microsoft/copilot-studio-mcp](https://registry.npmjs.org/@microsoft/copilot-studio-mcp) (published 2026-11-10)
- Evidence: documented
- Why: switch-experiences names no New experience toggle while the standard-harness quickstart still says to turn it off, so line 11 should cite both and lead with Other ways to build, not drop the toggle. The upgrade page's slug is now `upgrade-to-microsoft-copilot-from-standard-harness`, weakening ECO-0020's slug reason for the old name. Copilot-harness triggers can run on each user's credentials, so line 17 is standard-harness only. Copilot Studio MCP is 1.0.4 (no changelog), called public preview on the blog with no label on Learn, and adds `get_copilot_studio_skills`. GA of the harness rests on blogs only (announcement only); Learn has no banner and pages disagree on the harness count.
- Link: none
- Supersedes: ECO-0020

## ECO-0036: Canvas Copilot answer can't be added to new apps, and canvas.md lists it without a caveat
- Priority: P3
- Disposition: plan for later
- Status: open
- Release status: deprecated
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: skills/powerops-plan/references/canvas.md:17, .claude/agents/powerops-apps.md:23
- Sources: [Copilot answer control](https://learn.microsoft.com/power-apps/maker/canvas-apps/copilot-answer-control-overview) (updated 2026-11-18), [Add a Copilot control](https://learn.microsoft.com/power-apps/maker/canvas-apps/add-ai-copilot) (observed 2026-11-21), [Microsoft Copilot in canvas apps](https://learn.microsoft.com/power-apps/maker/canvas-apps/microsoft-365-copilot-canvas-app) (updated 2026-10-13)
- Evidence: documented
- Why: From 2026-02-02 neither Copilot answer nor the Copilot control can be added to new canvas apps, and existing apps keep working for a limited time; Learn's modern controls list still shows Copilot answer, so canvas.md:17's dated record is accurate but needs a caveat. The replacement is preview and needs a Microsoft 365 Copilot licence per user, Dataverse search and the tenant's Dataverse-in-Copilot setting.
- Link: none

## ECO-0037: The power-pages migrate-datamodel skill, and its restart call that the gate doesn't see
- Priority: P3
- Disposition: plan for later
- Status: open
- Release status: preview
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: docs/HANDOFF.md:21, docs/HANDOFF.md:155, .claude/agents/powerops-apps.md:15, skills/powerops-plan/references/power-pages.md:8
- Sources: [power-platform-skills 42a9422](https://github.com/microsoft/power-platform-skills/commit/42a94220bad33ffc815c3b8fb9e7229e65ad3542) (published 2026-11-18)
- Evidence: documented
- Why: The skill migrates a site from the standard to the enhanced data model with approval gates; its `pac pages migrate-datamodel`, `pac pages upload` and `pac application install` are already always blocked (HANDOFF:111), but the HANDOFF:21 row doesn't name the skill, and `scripts/restart-site.js --approved` sends a Power Platform API restart POST with no shell `pac`, a write for HANDOFF:155's list. PowerOps refuses standard-model sites and names no migration route.
- Link: none

## ECO-0038: PO030's why and fix miss two new facts: a sharing block for maker credentials, and computer use
- Priority: P3
- Disposition: plan for later
- Status: open
- Release status: preview
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: docs/rules/PO030.md, skills/powerops-plan/references/copilot-studio.md:17, src/PowerOps.Core/Rules/Agents.cs
- Sources: [Control sharing agents with embedded maker credentials](https://learn.microsoft.com/power-platform/admin/security/control-sharing-agents-with-embedded-maker-credentials) (published 2026-11-04), [Computer use](https://learn.microsoft.com/microsoft-copilot-studio/computer-use) (updated 2026-10-21)
- Evidence: documented
- Why: A PPAC setting (Security > Threat detection, recommended "Don't let makers share") can refuse to share or publish an agent whose connection embeds maker credentials, so an agent PO030 flags may import and then fail to publish. Computer use defaults to maker-provided credentials, and whether it exports as a `TaskDialog` that PO030 reads needs an export.
- Link: none

## ECO-0039: Power CAT's real skills router against smoke scenario 4's fake one
- Priority: P3
- Disposition: plan for later
- Status: open
- Release status: GA
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: tests/smoke/skills-smoke.sh
- Sources: [power-cat-skills 7814307](https://github.com/microsoft/power-cat-skills/commit/7814307) (published 2026-10-28)
- Evidence: documented
- Why: `powercat-skills-router` routes Power Platform requests across the Power CAT and Power Platform marketplaces and installs plugins after approval; scenario 4 checks that the plan-first skills win only against a router written in the script. A run with the real router, or a recorded reason not to, settles it.
- Link: none

## ECO-0040: skills-for-fabric's 12-visual warning file is unchanged since before the cited commit
- Priority: P3
- Disposition: implement now
- Status: open
- Release status: GA
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: docs/ecosystem/compatibility.md
- Sources: [anti-patterns.md history](https://api.github.com/repos/microsoft/skills-for-fabric/commits?path=plugins/powerbi-authoring/skills/powerbi-report-cli/references/design/anti-patterns.md) (observed 2026-11-21), [anti-patterns.md at 6456326](https://github.com/microsoft/skills-for-fabric/blob/64563263c7a5eaf7e93fe0a48b5f4dd636746766/plugins/powerbi-authoring/skills/powerbi-report-cli/references/design/anti-patterns.md) (published 2026-11-19)
- Evidence: documented
- Why: The file last changed at ec8d7ec (2026-09-24), before the cited 4c3986d, and at 6456326 still warns at more than 12 visuals; the plugin's `skills/` folder last changed at 2e4e559 (2026-10-01). The open sentence in the skills-for-fabric row of compatibility.md can close in the review commit.
- Link: none

## ECO-0041: HANDOFF says the gate passes guarded commands only as one plain command, but it reads a cd prefix
- Priority: P3
- Disposition: implement now
- Status: open
- Release status: preview
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: docs/HANDOFF.md:101, docs/HANDOFF.md:111
- Sources: [Claude Code Code Mode](https://code.claude.com/docs/en/code-mode.md) (observed 2026-11-21)
- Evidence: tested
- Why: Claude Code's Code Mode (experimental, 2.1.323 or later) can hand a hook `cd '<dir>' && <command>`. Tested offline on 2026-11-21 with `deploy gate`: `cd '/tmp/x' && powerops deploy apply plan.json --apply --require-reviews` exits 0 like the plain form, `cd … && pac solution import` gets the same checks as the plain form, and `--apply` without `--require-reviews` still exits 2; ImportCallTests already expects the `cd /work && …` import to be readable. So routes keep working in Code Mode and the docs are stricter than the code: reword "one plain" to allow a leading `cd` segment, or tighten the code (owner's call).
- Link: none

## ECO-0042: BUILD-PLAN still says PowerOps.Cli references ModelContextProtocol.Core 2.3.0
- Priority: P3
- Disposition: implement now
- Status: open
- Release status: GA
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: docs/BUILD-PLAN.md:115, docs/BUILD-PLAN.md:124, docs/BUILD-PLAN.md:355
- Sources: [csharp-sdk v2.4.0](https://github.com/modelcontextprotocol/csharp-sdk/releases/tag/v2.4.0) (published 2026-11-07)
- Evidence: documented
- Why: Dependabot's #124 (`f420117`, merged 2026-11-10) moved `ModelContextProtocol.Core` from 2.3.0 to 2.4.0 in the csproj and lock file; BUILD-PLAN.md:124's "the version `PowerOps.Cli` references" has been false since, and lines 115 and 355 are dated history that want a pointer. A dated note on each line.
- Link: none

## ECO-0043: pac 2.15.1 and 2.16.1 document no change to the commands PowerOps calls
- Priority: P3
- Disposition: monitor
- Status: open
- Release status: GA
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: docs/HANDOFF.md:10, docs/examples/ephemeral-sweep.yml, docs/examples/promote-dev-to-test.yml, docs/ecosystem/compatibility.md
- Sources: [Microsoft.PowerApps.CLI.Tool 2.16.1](https://www.nuget.org/packages/Microsoft.PowerApps.CLI.Tool/2.16.1) (published 2026-11-20), [pac CLI reference](https://learn.microsoft.com/power-platform/developer/cli/reference/) (observed 2026-11-21)
- Evidence: documented
- Why: 2.15.1 (2026-11-12) fixes only `pac admin reset` language and currency lookups and 2.16.1 only updates Microsoft.Identity.Client; the reference pages for `org who`, `solution list/export/import/unpack/pack/check`, `power-automate list-cloud-flows` and `copilot mcp` were not edited in the window. Release notes alone don't show the text formats unchanged: look again before the "tested with 2.13.1" line or the examples' 2.14.1 pin moves, which needs those commands observed on the new version against a tenant (the person's yes).
- Link: none
- Supersedes: ECO-0012

## ECO-0044: Power Pages enhanced authorization is documented as preview, opt-in per site
- Priority: P3
- Disposition: monitor
- Status: open
- Release status: preview
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: docs/rules/PO021.md, docs/rules/PO022.md, docs/rules/PO023.md, skills/powerops-plan/references/power-pages.md
- Sources: [Enable enhanced authorization (preview)](https://learn.microsoft.com/power-pages/security/unify-pages-security-with-dataverse) (updated 2026-07-09), [Roadmap item 576750](https://www.microsoft.com/microsoft-365/roadmap?id=576750) (updated 2026-10-16)
- Evidence: documented
- Why: Learn now documents ECO-0016's merged model: a per-site checkbox (Power Pages Core 1.1.2605.258 or later) maps contacts to system users and web roles to security roles, Dataverse checks permissions as the site user, and Web API column permissions give way to column security profiles. Web roles and table permissions still define access, so PO021 to PO023 hold; a plan should say whether a site turns it on. Look again when roadmap 576750 launches (GA November CY2026 on the item).
- Link: none
- Supersedes: ECO-0016

## ECO-0045: MCP: no revision after 2026-07-28 and SDK 2.4.0 changes no elicitation; ECO-0015's re-check trigger fired
- Priority: P3
- Disposition: monitor
- Status: open
- Release status: GA
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: src/PowerOps.Cli/McpCommand.cs, src/PowerOps.Cli/PowerOps.Cli.csproj, docs/FOUNDATION.md:189, docs/ecosystem/compatibility.md
- Sources: [MCP draft changelog](https://modelcontextprotocol.io/specification/draft/changelog) (observed 2026-11-21), [csharp-sdk v2.4.0](https://github.com/modelcontextprotocol/csharp-sdk/releases/tag/v2.4.0) (published 2026-11-07), [MCP extension support matrix](https://modelcontextprotocol.io/extensions/client-matrix.md) (observed 2026-11-21)
- Evidence: documented
- Why: Dependabot moved `ModelContextProtocol.Core` to 2.4.0 on 2026-11-10, ECO-0015's trigger. Re-checked: `schema/` still ends at 2026-07-28; the draft only restores optional `_meta` on the elicitation types and reports invalid tool arguments as an `isError` result; SDK 2.4.0 adds an experimental Skills extension package and nothing on elicitation; the form's round trip stays unit-tested. The Skills extension (SEP-2640, Final) would let `powerops mcp` serve its skills, but no client PowerOps supports implements it. Look again at the next MCP revision, the next SDK move, a client pin move (ECO-0032) or Skills support in Claude Code or Codex.
- Link: none
- Supersedes: ECO-0015

## ECO-0046: Microsoft build routes outside the gate, in preview or announced
- Priority: P3
- Disposition: monitor
- Status: open
- Release status: preview
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: docs/HANDOFF.md:154, docs/HANDOFF.md:155, .claude/agents/powerops-apps.md
- Sources: [Copilot Managed Runtime](https://learn.microsoft.com/microsoft-365/copilot-managed-runtime/overview) (observed 2026-11-21), [mobile-apps README](https://github.com/microsoft/power-platform-skills/blob/main/plugins/mobile-apps/README.md) (observed 2026-11-21), [Canvas authoring agent](https://learn.microsoft.com/power-apps/maker/canvas-apps/canvas-authoring-agent) (updated 2026-10-27), [MCP and A2A channels](https://learn.microsoft.com/microsoft-copilot-studio/agents-experience/publication-channels-mcp-agent-to-agent) (updated 2026-11-12), [Roadmap item 576752](https://www.microsoft.com/microsoft-365/roadmap?id=576752) (updated 2026-10-16)
- Evidence: documented
- Why: Each is a write or build route no PowerOps route names: Copilot Managed Runtime's `ms app deploy` and its Claude Code plugin; the mobile-apps plugin (private preview: `pa app push`, Web API table writes, `PublishAllXml`); the canvas authoring agent (US only, a tenant setting, prompts to GitHub); the MCP and A2A channels (early-release environments); app building in Copilot Cowork; and, announcement only, the Dataverse management MCP server (GA December CY2026 on the roadmap). Look again when any reaches GA or a plan names it.
- Link: none

## ECO-0047: Organisations can now govern the local agent and its plugins that run PowerOps
- Priority: P3
- Disposition: monitor
- Status: open
- Release status: preview
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: docs/HARNESS.md, docs/PLANNING-SKILLS.md, .claude-plugin/marketplace.json
- Sources: [Control where GitHub Copilot customizations come from](https://github.blog/changelog/2026-11-16-control-where-github-copilot-customizations-come-from) (published 2026-11-16), [What's new in Microsoft Agent 365, Ignite 2026](https://techcommunity.microsoft.com/blog/agent-365-blog/whats-new-in-microsoft-agent-365-ignite-2026/4571753) (published 2026-11-17)
- Evidence: documented
- Why: GitHub Copilot's `strictPluginOnlyCustomization` can block standalone skills, agents, hooks and MCP servers (what `powerops skills install` writes), enterprise plugin controls went GA on 2026-10-28, and Copilot CLI reads `.claude-plugin/marketplace.json`, where hooks and forms are unchecked (HARNESS.md: provisional). Agent 365 and Entra Agent ID reach local coding agents including Claude Code and Copilot CLI (announcement only: identities, DLP, Defender and Intune blocking). Look again when Learn documents the Agent 365 local-agent controls or Copilot CLI leaves "provisional".
- Link: none

## ECO-0048: Copilot Studio agent identity, agent accounts and evaluations: new controls a plan may need to name
- Priority: P3
- Disposition: monitor
- Status: open
- Release status: preview
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: docs/HANDOFF.md:67, skills/powerops-plan/references/copilot-studio.md:16, skills/powerops-plan/references/copilot-studio.md:17, .claude/agents/powerops-copilot-studio.md:40, .claude/agents/powerops-copilot-studio.md:42, .claude/agents/powerops-copilot-studio.md:46, .claude/agents/powerops-copilot-studio.md:50
- Sources: [Give your agent an account (Frontier)](https://learn.microsoft.com/microsoft-copilot-studio/agents-experience/authoring-agent-account) (updated 2026-10-16), [Manage Entra Agent IDs](https://learn.microsoft.com/microsoft-copilot-studio/admin-use-entra-agent-identities) (updated 2026-11-17), [Create a single response test set](https://learn.microsoft.com/microsoft-copilot-studio/analytics-agent-evaluation-create) (updated 2026-10-06), [Evaluate agent behavior with msagent](https://learn.microsoft.com/microsoft-365/copilot-managed-runtime/developer/agents/cli/evaluate) (observed 2026-11-21)
- Evidence: documented
- Why: Agent accounts (Frontier preview, early-release environments, E5 plus Agent 365 or E7, a licence per published agent) give an agent its own Entra account and mailbox, tools default to authenticating as it, and shared memory has no privacy boundary between users; a public preview is announced for December 2026 (announcement only). Every new agent gets an Entra Agent ID (GA, no opt-out) whose connector permissions Conditional Access can target, so a target tenant's policies can block an imported agent's connectors; what a solution import does to that identity is undocumented, and the blueprint app must stay enabled for copy and restore. Evaluations keep checklist 14's limits (100 cases, CSV of 100 questions), add a content safety method, and `msagent agent eval run --from-csv` (preview) spends credits and can run tools that change data. Look again at the agent-account public preview, when Learn documents Agent ID on import, or when the Microsoft Copilot harness evaluation page is read.
- Link: none

## ECO-0049: Microsoft's skills turn telemetry on by default: generative pages and the Power Automate plugin
- Priority: P3
- Disposition: monitor
- Status: open
- Release status: preview
- Found: 2026-11-21, mode A
- Verified: 2026-11-21
- Affects: skills/powerops-plan/references/model-driven.md:12, docs/HANDOFF.md:22, .claude/agents/powerops-automate.md:9, .claude/agents/powerops-automate.md:31
- Sources: [Generate a page using natural language](https://learn.microsoft.com/power-apps/maker/model-driven-apps/generative-pages) (updated 2026-09-28), [power-platform-skills 3a01772](https://github.com/microsoft/power-platform-skills/commit/3a017729c414d7d0295ad201e2bf29be3304217c) (published 2026-11-16), [Power Automate plugin telemetry](https://github.com/microsoft/power-platform-skills/blob/main/plugins/power-automate/references/telemetry.md) (observed 2026-11-21)
- Evidence: documented
- Why: Published generative pages can report page views, exceptions, `dataApi` calls and navigation to Application Insights once an admin sets up the export (preview), and the genpage skill's `custom-telemetry` flag now defaults to on (`GENPAGE_ENABLE_CUSTOM_TELEMETRY=0` turns it off for a run). The Power Automate plugin v3.2.x adds `PreToolUse` (Skill) and `UserPromptSubmit` hooks that send telemetry by default, which can include tenant, org and environment GUIDs and the user's Entra object ID but no prompt text (opt-out `POWER_PLATFORM_SKILLS_TELEMETRY_POWER_AUTOMATE_OPTOUT=1`). A plan should say whether pages send telemetry and where, for the security review, and HANDOFF could name the plugin's opt-out. Not run. Look again at genpage telemetry GA or the next Power Automate plugin release.
- Link: none

## Review log

- 2026-11-10, mode A: 19 automated read, 0 not checked, 10 by hand; ECO-0001, ECO-0002, ECO-0003, ECO-0004, ECO-0005, ECO-0006, ECO-0007, ECO-0008, ECO-0009, ECO-0010, ECO-0011, ECO-0012, ECO-0013, ECO-0014, ECO-0015, ECO-0016, ECO-0017, ECO-0018, ECO-0019. Cheap path: dataverse-skills@v1.18.0, power-platform-skills@7558c8e974b199747ce8e3c0c922ec415ed48585, skills-for-copilot-studio@9c42d3a612c6e06389b6e5b4eed579aaed1a8b63, copilot-studio-plugin@4014ae689d1228d86cb3aaab938b1e66f8e2e10d, skills-for-fabric@c13604ae6f4fb727ac7bc74784eeff4332433c63, power-cat-overflow@1f81439b98d7aa5fa389a6d4fcc2b261de073afc, power-cat-overpage@f01f9ca893596fe5fb9f127c49736a2fa03b48fc, pa-persistence@0.8.2735.117-preview, docs-important-changes@2cc849531308d84aaf81cb03f5756f19a579f6db, docs-alm-yaml@62494850aa43e90630db7eb82672faeb0bfcae76, docs-alm-settings@8ad877ec951e492c0a1a3aed01765f21c3113df8, docs-git-integration-faqs@da4eb304bd15d1e8ee2330104f35b9d2eb065bdf, docs-pac-mcp@76e446549b0c94831c50ef2e48df14e885faa519, docs-pa-cli@8e42d75ad9f2151be916b75dafede418f7ef3e76, docs-canvas-yaml@6d1e70ba261f80bddd70a03fdfaf830187f1f59a. In entries: pac-cli@2.14.1 (ECO-0012), pa-cli@1.4.1 (ECO-0001), mcp-spec@2026-07-28 (ECO-0015), docs-dataverse-mcp@69cf4c7a9213db15b66ae961757de7120093163f (ECO-0004); the 5a observations pa-cli@1.6.0 and docs-flow-limits@82f95a85c3d523e03c52a06b50d5daac1a20a4a0 went with their sources' changes (ECO-0001, ECO-0002). No baseline (first observation, value recorded): pa-cli as the CLI package, m365-roadmap, power-bi-latest, fabric-roadmap. By hand: copilot-studio-whats-new, copilot-studio-mcp, power-bi-authoring-mcp, power-bi-latest, programmability, claude-code, codex, docs-flow-limits, release-plans (archived, removed, ECO-0003) and m365-roadmap (all 66 Power Platform items modified 2026-10-13 or later read by title and status, 10 opened). Not read: fabric-roadmap, whose page renders only through script (a fetch gets "Loading features"); it keeps its date and stays due. Folder-level watching of the power-platform-skills plugins: worth it, narrowed to the cited files of code-apps and model-apps (ECO-0018), after step 7 measures the weekly rate.
- 2026-11-10, mode A: 0 automated read, 0 not checked, 1 by hand; no entries. By hand: fabric-roadmap, read in a browser and through the page's own feed (see step 5 of the procedure; the feed has no modified date, so there was no reading window): Power BI (104 items, 17 planned) and Fabric Developer Experiences (97 items, 2 planned) read by title, status and date, 8 opened in full; in the other 13 products, the planned items and those dated Q3 2026 or later were searched for the CI/CD, Git integration, deployment pipeline, PBIP, PBIR, TMDL, semantic model, MCP, CLI and Power Platform terms (Data Factory and Data Warehouse by pattern, their feeds not being valid JSON). Nothing contradicts `skills/powerops-plan/references/power-bi.md`, `docs/HANDOFF.md` or `.claude/agents/powerops-bi.md`: "Developer Mode GA" (PBIR on by default for everyone) is still listed as planned for Q3 2026, while Learn's report folder page, read the same day, says PBIR is generally available and the default, so the claim keeps its Learn source; no item says deployment pipelines support PBIR reports; "Deployment Plan" (public preview, Q4 2026) adds an order and pre- and post-deploy actions to Git integration and deployment pipelines, and is no route PowerOps names; the Fabric CLI's `deploy` (GA, Q1 2026), the bulk item definition API (GA, Q3 2026) and the Fabric Core MCP Server (GA, Q3 2026) are Fabric writes, for which `docs/HANDOFF.md` already says the gate has no route; "Developer mode (PBIP) external changes reload" (GA planned Q1 2027) and "Organizational themes for Power BI reports" (GA planned Q4 2026) touch no claim. This supersedes "Not read: fabric-roadmap" in the line above.
- 2026-11-12, mode B: 0 automated read, 0 not checked, 4 by hand; ECO-0020. By hand, for the M95 plan: the power-platform-skills, skills-for-copilot-studio, copilot-studio-plugin and skills-for-fabric rows of `compatibility.md`, read against each repository's `main` (`cb07d81`, `4014ae6` and `e31422e` for the three that changed evidence), and the open entries (ECO-0009 to ECO-0013, ECO-0015 to ECO-0018), none of which covers the new experience, the canvas references or the Power BI validator; the Learn pages are those of the plan's Assumptions A1 to A14.
- 2026-11-14, mode C: 0 automated read, 0 not checked, 3 by hand; ECO-0021. By hand: the Ubuntu 24.04 release notes, dotnet/runtime#110488 and the runner images' Ubuntu 24.04 and 26.04 toolsets and software lists.
- 2026-11-15, mode B: 0 automated read, 0 not checked, 6 by hand; ECO-0022. By hand, for the M109 plan: the Power Apps Developer Plan page, Deep dive on specific licenses, Important changes coming, Licensing requirements for managed environments, the environments overview and the licensing service plan reference, read on Learn by `powerops-researcher`; the Fable plan review re-read the first three and its work review the fourth the same day.
- 2026-11-21, mode A: 20 automated read, 0 not checked, 11 by hand; ECO-0023, ECO-0024, ECO-0025, ECO-0026, ECO-0027, ECO-0028, ECO-0029, ECO-0030, ECO-0031, ECO-0032, ECO-0033, ECO-0034, ECO-0035, ECO-0036, ECO-0037, ECO-0038, ECO-0039, ECO-0040, ECO-0041, ECO-0042, ECO-0043, ECO-0044, ECO-0045, ECO-0046, ECO-0047, ECO-0048, ECO-0049. Cheap path: pa-cli@1.4.2, dataverse-skills@v1.19.0, skills-for-fabric@64563263c7a5eaf7e93fe0a48b5f4dd636746766, docs-important-changes@c77ff362f7340f1c15b0ba924dd76743378528be, docs-pa-cli@9caf0f190087b49417a05067ba9833707037fdb5, docs-dataverse-mcp@8a635ff8c2b4cb76570a22629e44f518ed6d80b4, power-platform-skills@7558c8e974b199747ce8e3c0c922ec415ed48585, skills-for-copilot-studio@9c42d3a612c6e06389b6e5b4eed579aaed1a8b63, copilot-studio-plugin@4014ae689d1228d86cb3aaab938b1e66f8e2e10d, power-cat-overflow@1f81439b98d7aa5fa389a6d4fcc2b261de073afc, power-cat-overpage@f01f9ca893596fe5fb9f127c49736a2fa03b48fc, pa-persistence@0.8.2735.117-preview, docs-alm-yaml@62494850aa43e90630db7eb82672faeb0bfcae76, docs-alm-settings@8ad877ec951e492c0a1a3aed01765f21c3113df8, docs-git-integration-faqs@da4eb304bd15d1e8ee2330104f35b9d2eb065bdf, docs-pac-mcp@76e446549b0c94831c50ef2e48df14e885faa519, docs-canvas-yaml@6d1e70ba261f80bddd70a03fdfaf830187f1f59a, docs-license-designation@cc40846ddae025054d572df3a1d075e246bb8467. In entries: pac-cli@2.16.1 (ECO-0024, ECO-0043), mcp-spec@2026-07-28 (ECO-0045). By hand, none due: copilot-studio-whats-new, copilot-studio-mcp, power-bi-latest, power-bi-authoring-mcp, programmability, claude-code, codex, docs-flow-limits, docs-power-automate-license-types, docs-developer-plan (its commits caf484d and 3e569d8) and m365-roadmap (148 Power Platform and Copilot items modified 2026-09-21 or later read by title and status, 13 opened); beyond the watch, the 20 power-platform-skills commits cb07d81..main by plugin, power-cat-skills outside its watched paths, the Power Automate plugin's commits since 2026-09-21 and the MCP spec repo's commits since 2026-09-21. Not read: fabric-roadmap (not due), docs-powerapps-licensing-faq, docs-desktop-flows-connector, docs-desktop-flow-license-check and docs-http-webhook-training (not due); they keep their dates.
- 2026-11-22, mode B: 0 automated read, 0 not checked, 4 by hand; no entries. By hand, for WP G1 (Managed Developer Environment examples): re-checked Learn's Important changes coming, Power Apps Developer Plan, Licensing requirements for Managed Environments, and `pac admin` reference (`create`, `reset`, `set-governance-config`), plus entries ECO-0006 and ECO-0022 (done) and ECO-0043 (open); confirmed 2026-11-15 / 2026-12-15 / 2027-01-15 dates and kept `PAC_VERSION` at 2.14.1 and deletable types restricted to `Sandbox` and `Trial`.
