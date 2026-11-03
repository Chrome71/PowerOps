---
status: signed-off
review: done
signed-off: 2026-11-02
signed-off-hash: 527940df7f56c69d2598f657cd5799d64a1f98f9a84498dbecfc1c2f55848769
environment: null
depth: standard
supersedes: null
---

# Phase 7: Power Platform integration (M40–M49)

## Goal

Make PowerOps the plan, check and gate step for everything Microsoft's Power Platform skills and MCP servers can build: canvas apps, model-driven apps with custom and generative pages, Power Pages, Power Automate, Power BI, Code Apps and the choice of data store. Copilot Studio and MCP come second, after stage one is done. The person asked for this on 2026-11-02, after M39 merged at `2b25cc3`.

PowerOps authors nothing, compiles nothing and deploys no Power BI content. What it adds is in four places:

1. **A goal trace and a size brake in the plan.** `powerops-plan` ties each table, list, flow and screen to a Goal line, asks what can be reused, and records what won't be built. `powerops-review-plan` gets a size item. Neither skill is pinned, and this needs no product code.
2. **Readers and a few offline rules,** each with a number a source states, for the products PowerOps can't read today.
3. **Gate routes** for the tenant writes that pass `powerops deploy gate` today, with the hook matchers that make a client send them to the gate.
4. **A hand-off** that names the Microsoft skill or MCP server for each job, and where PowerOps takes over.

Why bloat needs this: PowerOps stops a build going beyond its plan, but nothing questions a plan that is too big. None of the 15 rules measures a flow. SharePoint lists sit outside a solution, so `analyze` and `diff` never see them, and the plan is the only place to catch twenty of them. The research behind this plan (nine reports and a refute pass, kept outside the repo until sign-off) found that Microsoft's skills now build almost everywhere, that none of the skills read ties an artefact to a goal, and that official samples often fail their own guidance.

This plan fixes scope, order and done-when. Each milestone gets its own signed-off plan before it is built, as Phase 6 did. It must follow its own rule and stay as small as the goal allows: every milestone says what it adds, what it deliberately doesn't, and what would stop it, and the cuts are under "Won't build".

Out of scope: M17 (on hold by the person's choice; stage two picks it up only if the person asks), building or deploying Power BI content, a Power BI `pbip check` (a held item outside the sequence, see "Held"), NuGet publishing, and everything under "Won't build".

**Completion.** A milestone is **built** when its own plan's done-when is met, or **deferred** when a fact it depends on isn't available and the person accepts the deferral by name, recorded in BUILD-PLAN with the reason (D11). A stage is done when every milestone in it is built or deferred. A deferred milestone doesn't block the next stage, and its scope stays in BUILD-PLAN as open. A milestone is never partly built silently: anything it can't check says "not checked" in its output and in its plan.

Done when:

- **Stage one, built or deferred by name:** M40 to M47. In particular: `powerops-plan` writes the ledger, the Goal columns and the `Won't build` list, and loads product facts only for the products a plan names; `powerops-review-plan` scores size (item 11). The prototype is offered before approval on both paths where "Screens" lays out content freely, recorded in `decisions.md`, and never checked by the Verifier. `powerops-implement` has a Power BI branch. `deploy gate` blocks the push and upload routes M40 lists, and the documented hooks send the named MCP tools to it. `docs/HANDOFF.md` has a row per product, `pcf`, Power BI and Code Apps included. The flow size rule and the nested-loop rule ship, or D3 has made the size rule a review item. `analyze` reads canvas formulas from an `.msapp` (M44), Power Pages permissions and Web API settings in the solution layout (M45), model-driven app modules and site maps (M46), and resolves code-app dependencies (M47).
- **Stage two, built or deferred by name:** `analyze` reads agents from `bots/` and `botcomponents/` with M48's rules, and `diff` shows added tools and changed instructions under the contract D9 sets. Live checks for agents are marked waiting, because no Copilot Studio environment is available.
- Every milestone is one PR with its own signed-off plan, and passes `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` and `powerops plan status .`, with CI green on Linux, Windows and macOS.
- `RoleEvalTests` passes on every milestone. A milestone whose output would change an eval baseline follows D1 before it is built, and says in its plan whether a rerun is in scope.
- `powerops-verify` and `powerops-review-security` are byte for byte what M39 shipped. If D1 rules that the ledger is an input change, the full pass of both roles and the renewed decision are recorded as M39 did, before M41's skill ships.
- No new dependency. JsonSchema.Net stays at 8.0.5.
- F1 under "Environments and route" is the one required prerequisite: M44 is built after it, or deferred under D11. The other Dev facts are optional evidence or an optional experiment, and no milestone waits for them.

## Users

- A maker who plans and builds a Power Platform solution with an agent, through Microsoft's skills, and wants the plan to be the right size before the build starts.
- The person who signs plans off, answers the named decisions, and gives the yes for every tenant read or write.
- An agent in a supported client that runs the PowerOps skills and `powerops` commands on repos and exports it didn't write.
- CI: the Action on pull requests, which runs `analyze` with the new rules.
- A contributor who adds a rule, a reader or a reference file from the AGENTS.md recipes.

## Data

### How this plan was made

Nine research reports, one per area (canvas, model-driven, Power Pages, Power Automate, Power BI, Code Apps, Dataverse and data choice, design and prototyping, Copilot Studio and MCP), were combined into one standard. A separate refute pass found 6 High, 19 Medium and 11 Low problems in it, which the standard then applied. This plan is drafted from the checked standard, with the reports read where a milestone needed the evidence, and revised after two review rounds (13 findings in the first, 8 open in the second). Nothing was tested in a tenant, no Microsoft skill or MCP was run, and only public sources were read. Where this plan departs from the standard, the milestone says so and `decisions.md` gives the reason.

Evidence levels in this plan: **documented** means a Learn page or a licensed repo states it; **observed** means a researcher read it in a public file or measured it on public samples; **inferred** means read in code only. Counts the researchers measured were not recomputed by the refute pass.

### Who does what

| Job | Owner |
|---|---|
| Scope, size, reuse, store, route, licence fit, what won't be built | PowerOps: `powerops-plan`, `powerops-review-plan` |
| Building and authoring | Microsoft's skills and MCP servers, in Dev only; Power BI locally in a PBIP, with service writes on the person's yes |
| Compiling, App Checker, flow checker, solution checker, Site Checker, `powerbi-report-author validate` | Microsoft, in a tenant or locally. PowerOps names the step in the hand-off |
| Deterministic offline checks, `diff`, the import gate | PowerOps |
| Judging quality, taste and prompts | A person, or Microsoft's review tools |
| Promotion to Test and Prod | `deploy apply` or Pipelines, on the person's yes. Power BI moves by its own route (fabric-cicd, Git sync or deployment pipelines), as a separate lifecycle |

Per product:

| Product | Plans | Builds with | Needs a tenant for | Available to this project |
|---|---|---|---|---|
| Canvas | `powerops-plan` | `canvas-app`, the canvas-authoring MCP | Build, compile, App Checker, save and publish | Dev |
| Model-driven | `powerops-plan` | `app-builder`, `genpage`, `pcf` | Build, solution checker, Playwright | Dev |
| Power Pages | `powerops-plan` | `create-site`, `edit-site`, `setup-datamodel` | Deploy, Site Checker, Security Scan | Yes |
| Power Automate | `powerops-plan` | `build-flow`, `create-flow`, the flowagent MCP | Build, flow checker, run history | Dev |
| Power BI | `powerops-plan` | `powerbi-report-cli`, `semantic-model-authoring` | Desktop or the service for preview and deploy | Pro or Fabric; a PBIP needs Desktop on a Windows machine |
| Code Apps | `powerops-plan` | `create-code-app`, `add-*` | Push, Local Play, the admin toggle | Dev, toggle unconfirmed |
| Dataverse | `powerops-plan` | `dv-metadata`, `dv-solution`, the Dataverse MCP | Everything but the plan | Dev |
| Copilot Studio | Stage two | `skills-for-copilot-studio` | All runtime checks | No, waiting |

Two rules hold across every product. New checks reach clients through `analyze`, so the `powerops` MCP server gets no new tool for a rule; a new tool is only for a new command with its own input and no Microsoft equivalent, and a new command that `powerops-verify` should run is a pinned change, because its command list is closed. The server stays stdio: Copilot Studio needs a remote server, so `powerops mcp` is not offered as an agent tool.

Microsoft's own planning steps stay. `canvas-app` stops at its "Canvas App Plan", `app-builder` has a spec and approval, `genpage` has a planner, and `powerbi-report-cli` has a design gate. A signed-off PowerOps plan comes first and the Microsoft step is the second, product-specific gate. PowerOps adds no third.

### The pinned roles and the eval baselines

AGENTS.md pins the skill text of `powerops-verify` and `powerops-review-security`. No milestone here changes that text. The constraints that bind are these:

- `RoleEvalTests` has a test that fails on any change to the text output of `analyze`, `test`, `diff` or `settings validate` on the seven solution cases under `tests/eval/roles`, and a test floor of 4 of 6 planted flaws not shown, which the Verifier sits on exactly (the Security reviewer has 5 of 6). Both roles run those commands.
- A rule that fires on no eval case changes nothing there. The flow size and nested-loop rules were checked against the 18 eval flows (all at most 4 actions, depth 1) and fire on none. The review confirmed that no eval case holds an app module, an `.msapp`, a site or an agent.
- A rule that fires on an eval case changes its baseline file. Whether that needs a rerun is D1.
- Output in `--json` only never reaches a baseline, because the baselines hold text output.
- The ledger changes the layout of `plan.md`, which **both** pinned roles read: the Verifier compares its sections with the build, and the Security reviewer reads the plan files. All eval cases use the old layout. Whether that counts as an input change is the second half of D1, about the roles' behaviour, not a file path. The eval harness checks key labels and exact CLI output, not model behaviour, so a rerun on the old cases alone wouldn't exercise the ledger; D1 says what would.

Each milestone below says which of these it touches.

### Stage one: Power Platform content

Order: the highest value at the lowest risk first. The standard's order is gate routes, the PO005 fix and the plan skills first, then the flow rules, then anything that touches Data or shows a new flaw last. This plan keeps that order and adds the readers after the flow rules, cheapest reader first. One change: the direct-connection flow rule is dropped rather than placed third (see "Won't build"). M40 to M43 are the first release checkpoint: together they deliver the brief's bloat checks, the prototype and the gate, and none of them waits on a Dev fact.

Release labels order the milestones and don't fix version numbers, as decisions 29 and 38 say. M39 is v0.23.0.

#### M40. Gate routes, hook matchers and the PO005 fix (`fix:`, v0.23.1)

Adds:

- `deploy gate` blocks these routes, by the deny-list it has today: `pa app push` in its spellings (`pa`, `npx pa`, `npx --no-install pa`, `node_modules/.bin/pa`, and the retired `power-apps push`); `pac pages upload`, `pac pages upload-code-site`, `pac pages migrate-datamodel`; `pac model genpage upload`; `pac copilot push`, `pac copilot publish`; and the MCP tools `pages_upload`, `pages_upload-code-site`, `copilot_publish`, `model_genpage_upload`. Each is one route line, one case and tests, as the existing routes are, with its line in `docs/COMMANDS.md`. Hooks are off while building in Dev, so a Dev push or upload is not slowed.
- **The hooks reach the gate.** Recognising a tool name in `ImportCall` isn't enough: the gate reads MCP calls only for a tool name ending in `__solution_import`, and the documented hooks match only `mcp__pac-cli__solution_import`. So M40 widens `ImportCall`'s MCP recognition to the four tool names above, changes the matcher in both clients' hook examples in `docs/HANDOFF.md` to name the five tools (one group per client, in the form each client's matcher accepts), updates `HandoffDocTests`, which pins the matcher list, and adds one line for existing hook users: add the new matcher, or the new tools run unchecked. The M21 toy-server check is repeated for the new matchers in both clients, so a blocked call never reaches `tools/call`, and recorded.
- PO005 counts a connection reference as used when `Assets/botcomponent_connectionreferenceset.xml` maps it to an agent tool. Today `analyze` on Microsoft's public order-management sample reports two false PO005 warnings, and the rule's fix line could lead someone to remove a working reference. The test builds a small solution in a temp folder with `SolutionZip`: the mapping present is quiet, absent fires. No fixture is added. `docs/rules/PO005.md` says what counts as a use.
- Docs: `docs/HANDOFF.md` lists what the gate blocks always, and adds to what it can't see: VS Code Apply, a skill's push through a language server, the Copilot Studio MCP's `manage_agents`, and Fabric writes. `powerops-implement` step 7 names the baseline `pa app push` of `create-code-app` as a tenant write that needs a yes, Dev only, with `--solution-id`, and names Fabric writes (publish, refresh, delete) the same way.

Deliberately doesn't: an allow-list (D2); routes for `pac solution delete`, `pac solution publish`, `pac admin delete`, `pac env update-settings`, `pac connection delete`, `pac managed-identity delete` and `pac copilot delete`, which are tenant writes but not imports, and which `powerops-implement` step 7 already puts behind a yes; `pa app share` and `pa connection create`, for the same reason; a route for `pac copilot init --environment`, which M48's plan settles. The gate keeps exiting only 0 or 2 (decision 39).

Stops if: D2 chooses an allow-list. Then M40 keeps only the PO005 fix and the docs, and the gate gets its own plan.

Touches: no pinned skill text, no eval baseline (the eval cases hold no agent), `HandoffDocTests`, `docs/COMMANDS.md`'s tests.

#### M41. The size brake (`feat:`, v0.24.0)

Adds, in `powerops-plan` and `powerops-review-plan` only:

- **The ledger.** `Data` gains one row per table and list: `Artefact | Kind | Store | Goal line | Reuse, Extend or Create | Why not a column or choice | Rows now, 3 years`. `Automation` and `Screens` gain a Goal column only. A `Won't build` list goes under `Goal`, not in a new section, so the plan's section list and header don't change. An empty cut is written as "nothing was cut", so the review can tell silence from a cut.
- **The data round**, asked only when the plan names new data, recommended answer first: where the data lives today (connect, don't copy); rows in the largest table now and in three years, and the widest filter; who sees which rows; which existing table, list or standard table covers each thing named; offline and Managed Environment; what is out of this version. Under `standard` and `deep` they fill successive rounds of at most three questions each, skipping anything the request or the facts already supplied. Under `quick`, whose one round must already hold licensing and environments, only the first question joins that round, and the ledger's other cells are written from the request or as Assumptions, with unknowns under Open questions. The fast path asks none and does the same.
- **The store decision.** First, every constraint the data carries, before any choice: it can't move; SQL is mandated; it needs a model-driven app, Power Pages, native offline, row-level roles, column security or enforced relationships. Then the route. Data with a system of record stays there, through an integration that meets the constraints: a connector, a virtual table for external data that stays external, or a copy with a stated reason. New data under a SQL mandate goes to Azure SQL with Entra sign-in and server-side row security; under any other constraint, Dataverse; under none, a SharePoint list only when every gate holds, else Dataverse. Constraints that conflict (a SQL mandate and native offline, say) go under Open questions for the person, not to the first branch. The eight SharePoint gates with their documented numbers, as the standard words them after the refute pass: under 5,000 rows at three years, or every view and query filters on an indexed non-lookup column and returns at most 5,000, with indexes made before the list reaches 20,000 items; every app query delegates; few relationships; small flow volume, with the note that a filtered Get items without pagination can return nothing; site or list permission is enough; no native offline; the licence saving is real under Managed Environment licensing; the list schema has an owner. Elastic only for high-volume non-relational data, virtual only for external data that stays external, Dataverse for Teams never.
- **Table design.** Reuse, then extend, then create; a local choice unless shared; a lookup table only when values have attributes, an owner, security or business editing; normalize facts, then stop; state ownership per table, because it can't change later; keep primary names non-sensitive; one publisher and prefix before any metadata; audit and retention per table.
- **The flow card**, one per flow under `Automation`: Goal line and reuse; trigger with expected runs a day; rows per run and filter; numbered steps with an estimated action count and depth; each tool with premium, connection reference and rate limit; failure path; retry and concurrency if not default; how it is tested. The cost-per-day formula is a reference line, not a required one.
- **Review item 11, Size,** in `powerops-review-plan`: for each table, list, flow and screen, if this were removed, which Goal line would fail? No Goal line named, or none would fail: High. Two artefacts hold the same concept, or one shape is copied per year, team or site: High. A SharePoint list fails a store gate with no stated reason: High. Not marked Reuse, Extend or Create, or a Create that doesn't name what it checked first: Medium. A table or list holding only a name, code or status with no attributes, owner or security of its own: Medium. No `Won't build` list and no "nothing was cut": Medium. A flow over 50 actions or 4 levels with no reason: Medium (this is the review's copy of the M43 rule, and it stands whatever D3 decides).
- **Size of the skill.** `powerops-plan` loads on every plan and is 2,483 words. The core skill grows by the ledger shape, the data round and a pointer, and stays within about 300 words of today. The store decision, the gates and table design go in `skills/powerops-plan/references/data.md`, read only when a plan names new data.
- **The eval cases** (D6, if yes): two plan cases, `plan-13`, an oversized plan with many one-column lists and one shape per team, and `plan-14`, a compact plan of the right size, each with its key entries. The existing `review-plan` pass of `run.sh` runs `powerops-review-plan` on both, once per supported client. It passes when item 11 names the planted lists on `plan-13` and raises no High on `plan-14`. This measures `powerops-review-plan`, not the pinned roles: the oversized case shows whether the ledger catches anything, the compact one whether it accuses wrongly.

Deliberately doesn't: a tool that parses the ledger or compares it with a build (`analyze` takes no plan, lists aren't in an export, and a new command would change the pinned Verifier); a count rule on tables or lists (no source gives a number); any change to `plan status`, the header, the hash or the pinned roles; product facts (M42).

Stops if: D1 rules that the ledger is an input change to the pinned roles. Then M41 adds, before the skill ships, the full pass of both roles on the six solution cases on both supported clients (24 runs, scored and settled as M39 did), with one solution case's plan files rewritten in the ledger layout so the new layout is exercised too, the keep rules recomputed without weakening the floors or relabelling flaws, and the person's renewed decision. M41 is otherwise unblocked.

Touches: skill text of two unpinned skills, the plugin copies, `SkillsCommandTests` and `PluginManifestTests`; with D6, `RoleEvalTests`' key and case counts.

#### M42. Product references, the prototype and the Power BI branch (`feat:`, v0.25.0)

Adds, in the skills only:

- **Reference files** beside `powerops-plan/SKILL.md`, each read only when a plan names that product. `route.md`: form and view for records; generative page for overviews, queues and guided work; custom page only for a canvas control or a generally available connector; PCF only for a reusable field or dataset control; Code App only when all users are in the tenant and premium, a dev team owns TypeScript and CI, and the UI needs npm libraries or automated tests; anonymous or external users go to Power Pages. `canvas.md`: personas and form factor, rows per source, theme, the first step creates the blank app inside the named solution with coauthoring on, a person saves and publishes because a compile isn't a save, and the "doesn't map" list below. `model-driven.md`: `Screens` lists each form's tabs and sections and each page's type and reason; plan removals, because a rebuild never removes dropped artefacts; the mobile form sizes (5 tabs, 75 columns, 10 subgrids) as a review question only when the plan names phones or tablets; each persona's roles, taken together, read the tables its tasks use (roles may split access, personas may differ on purpose), and a dashboard of at most 20 controls, both as plan lines the review checks. `power-pages.md`: site type and what it gives up, enhanced data model only, one row per table (web role, scope, privileges, reason), Global scope only for read-only public reference data, the Bootstrap version read from the site and stop if unknown, no local login in production, a load test for thousands of users a month. `code-apps.md`: the route row and why, every user's licence, the admin toggle confirmed, each data source, the target solution by id, each CSP exception, nothing sensitive in app assets, and the baseline push needs a yes. `power-bi.md`: efficiency (one topic per model with named questions; storage mode with a reason, Import by default; star schema with each table a dimension or a fact; relationships filter one way; auto date/time off; a numeric type that fits each column's precision and range, fixed decimal for money and floating point where its range or precision is needed; only columns that serve reporting; incremental refresh for large growing facts; licence fit, with Pro's 1 GB, 8 refreshes a day, no XMLA write and no deployment pipelines; the security route; PBIP in Git with one named deploy route; the solution holds only the embed and its environment variable) and design (five tests: Purpose, each page answers one stated question; Hierarchy, one element dominates; Consistency, type, colours and slicer positions match across pages; Restraint, every visual adds a fact nothing else shows; Access, alt text, 4.5:1 contrast, reading tab order, colour never the only signal). The visual-count review threshold is 12 data visuals a page unless D4 says otherwise.
- **The prototype offer**, in `powerops-plan`: when `## Screens` names content the maker lays out freely, the what-next menu and the fast-path menu each carry one lettered option, "sketch the screens first", before approval, so the person gets the offer on both paths without an extra gate; one `## Log` line in `decisions.md`, `prototype: approved`, `skipped` or `not chosen`, where `not chosen` is written only after the option was shown; the file rules under Screens. No new question, so the fast path's "ask nothing else" holds.
- **The Power BI branch** in `powerops-implement`, short: a step that builds Power BI has no Dataverse solution, so the solution rules (membership, `analyze`, the solution roles) don't apply and are marked "not applicable", not "not verified". The build is local PBIP work through `semantic-model-authoring` and `powerbi-report-cli`; the workspace or capacity is a named target only when the step deploys; every service write (publish, refresh, delete, pipeline) needs the person's yes; the finish runs `powerbi-report-author validate` and records the five design tests against screenshots the person provides, or says which weren't checked. A plan with both a solution and Power BI finishes each part by its own rule. The pinned roles run on the solution part only.
- **Review additions** to item 11: several personas in one app, a source above 500 rows with no delegation note, a new component where a library exists (canvas); page type not justified, removals not planned, a persona whose roles together can't read a table its tasks use (model-driven; roles that split access across personas are quiet); Global scope beyond read-only reference data (Power Pages); "does this need a code app at all" (Code Apps); and one advisory item, Medium at most, for a prototype screen that isn't in `Screens` or a "doesn't map" element with no workaround.
- **Hand-off lines**: `powerops-implement` step 3 passes the prototype's path, not a screenshot, in the first request to the build skill, except for a generative page, where the person attaches a screenshot of the sketch in the page builder, which takes images; and adds `pcf` and the two Power BI skills to its product list. `docs/HANDOFF.md` gets rows for `pcf`, Power BI (with its separate deploy route and "needs a yes") and Code Apps.
- **Two scenarios** in `tests/smoke/skills-smoke.sh`, the weekly simulated-user test, each naming new data and screens: a `quick` plan gets the licensing and environments questions, at most one data question and the sketch option; a `standard` plan gets the data round, held to the test's existing three-questions-a-round check. Both write the log line. The word-count test (M41) stays, but these are the check of the interaction.

Deliberately doesn't: a control map or `data-control` names in the sketch, theme token work beyond the pasteable canvas theme YAML, or any claim that a build skill honours the sketch, until D8's one build has run; a chart library; a Figma or image pipeline; a `pbip check` (held); copying Microsoft's reference text into the skills (point at it instead); a Power BI row in `analyze`.

Stops if: nothing blocks it. D4 and D8 change one line each.

Touches: three unpinned skills, the plugin copies, `SkillsCommandTests`, `PluginManifestTests`, `HandoffDocTests`, the smoke test.

#### M43. Flow rules (`feat:`, v0.26.0)

Adds:

- **PO016, warning: a flow with more than 50 actions or more than 4 levels of nesting.** Actions are every entry under `actions`, recursively through `actions`, `else.actions`, `cases.*.actions` and `default.actions`; containers count and the trigger doesn't. Depth is the number of containers around the deepest action, a Switch case adding one. Learn gives only the hard limits (500 actions, 8 levels), and a fraction of those catches nothing: the largest public flow measured has 214 actions. The numbers come from Microsoft's CAT OverFlow skill (MIT), a review heuristic with no stated reason. The rule page cites Learn's Scopes page ("flows with a high number of actions or deeply nested scopes might experience slower performance") on its Sources line, says that 50 and 4 are that team's heuristic at a pinned commit and not a product limit, and defines depth. The message carries both counts, so a baselined finding re-raises when the size changes. Measured noise: 37.4% of CoE Starter Kit flows, 4.4% of PnP samples, 6 of 28 fixture flows.
- **PO017, warning: a loop inside a loop** (`Foreach` or `Until` inside either). Learn's anti-patterns page: ten by ten is a hundred iterations; use `$expand` and one loop. 36 of 235 CoE flows, 1 of 45 PnP.
- Both are warnings, and `analyze` defaults to `--fail-on error`, so no exit code changes. The `analyze-fixtures.*` goldens change for the six fixture flows over the line, updated with the recipe and the diff read.

Deliberately doesn't: an Error at 500 or 8 (the platform refuses those); a rule for a missing trigger condition (0 of 235 CoE flows have one, and Learn doesn't ask for it everywhere); the direct-connection and own-table rules (see "Won't build"); a Catch-never-fails rule (the only source is a Logic Apps page).

Stops if: D3 says no. Then PO016 stays a review item (M41 already has it) and M43 ships PO017 alone.

Touches: `Analyzer.Rules`, two rule pages, `docs/rules/README.md`, the README rule table (`DocsDriftTests`), goldens. No eval baseline: the milestone proves it by running `RoleEvalTests` before and after.

#### M44. Canvas reader (`feat:`, v0.27.0)

Adds:

- The solution readers open each `.msapp` read-only as a zip and read `Src/*.pa.yaml` from it, feeding the same canvas formula parse the YAML layout uses, so PO014 and PO005's canvas shortcut cover the XML layout that `docs/HANDOFF.md` commits. Entry names match either path separator and any case. No `Src` gives one `note:` line ("older app, resave in Studio"), never a finding, as Learn says older files need a resave (D10). Reads stay under `ReadBudget`, with the entry and byte caps the outer zip reader has, and no entry is written to disk.
- **PO018, warning: `ForAll` around `Patch`, or a `ForAll` inside a `ForAll`.** Learn's code optimization page. Fix: loop once and pass the loop's result to a single `Patch`.
- Tests build their `.msapp` with `SolutionZip`, because AGENTS.md bars new `*.msapp` fixtures. The repo's only `.msapp` (2023) has no `Src`, and a 2026 sample shows `Src` beside a newer `.msapr`.

Deliberately doesn't: a Power Fx parser (a token scan with a `shortcut:` note is enough for one pattern); reporting `AppCheckerResult.sarif` from the `.msapp` (undocumented; F1 says whether it is still there, and a later milestone can read it if so); rules for 256,000 characters, more than 2 dependent screens, `TabIndex`, `FluentV8/*` or control counts (see "Won't build"); a library-size rule (the review asks about a new component where a library exists); delegation (can't be proved offline; the plan asks).

Stops if: F1 shows that a fresh export has no `Src`, or that `.msapr` has replaced it with no documented layout. Then M44 is deferred under D11, and PO014 stays YAML-layout only.

Touches: `ZipReader`, `XmlUnpackReader`, `SolutionParser`, one rule, goldens only if a fixture gains a finding (none expected: the fixtures' `.msapp` files were removed or predate `Src`). No eval baseline: the cases hold no `.msapp`.

#### M45. Power Pages reader and boundary rules (`feat:`, v0.28.0)

Adds:

- **One root, the solution layout.** The reader reads `powerpagecomponents/<id>/powerpagecomponent.xml` (type code and JSON content) inside a solution that the existing discovery already finds by its `Other/Solution.xml`, into one new `Solution` field. In an exported zip the same components are entries of the zip; M45's plan states the exact zip paths from a packed copy of the fixture (`pac solution pack` is local and needs no sign-in) before it is signed off, because `SolutionZip` only relocates two files and doesn't reproduce pac's packing. One CLI-level test per layout: the unpacked fixture folder, and the packed zip of it. An unknown component shape is an `InvalidDataException` naming the path. The existing rules keep running on such a solution as they do today; the new field adds findings and changes none.
- Five rules, all warnings, with messages that read "before Production", because the official templates trip several: a `Webapi/<table>/fields` setting of `*` on a table whose `Webapi/<table>/enabled` is `True` (the wildcard is unsupported for all sites from September 2026, so requests fail, but a site can hold a one-time exemption that an export doesn't show, and the Web API is off for a table unless `enabled` is `True`, so neither an Error nor an unconditional check is safe); Web API enabled on a table with neither a fields setting nor `UseFieldsFromView` (Learn says fields are required unless `UseFieldsFromView` is set; with it set and no readable view the rule says "not checked"); a table permission with no web role (it has no effect); a child permission whose role is missing on the parent, or whose parent is inactive; an anonymous role with create, write or delete. Tests cover a disabled API, explicit fields and `UseFieldsFromView` as quiet cases.
- Fixture: gov-apptemplates `core-portal` (MIT, unmodified), the one public unpacked solution site, quiet on these five rules, recorded in `tests/fixtures/NOTICE.md`. Firing cases are built in tests with `TempFolder`.

Deliberately doesn't: the `.powerpages-site/` download layout (a different root with no solution identity, so it needs its own discovery and loading; a later milestone if a user asks); Global scope beyond read (a review item in `power-pages.md`), more than one anonymous or authenticated role, local login, bearer authentication, `*Secret` settings, header and encoding settings (review items or unobserved serialisation); page permissions on non-public pages (which pages are public is a plan fact); Liquid regex checks; `diff` of the boundary (a later milestone, once this reader is in; `diff` output is in the eval baselines); consuming the plugin's `analyze-performance` JSON.

Stops if: the packed copy shows a zip layout the reader can't map to the unpacked one without guessing, or a real site export (F5, optional) shows a shape the fixture doesn't.

Touches: `Solution`, `SolutionParser`, five rules, one fixture, goldens for `analyze-fixtures.*` (one new solution found). No eval baseline: no case holds a site. No schema change: findings only.

#### M46. Model-driven app modules and site maps (`feat:`, v0.29.0)

Adds:

- `SolutionParser` reads app modules and site maps into new `Solution` fields: in an unpacked solution the `AppModules/` and `AppModuleSiteMaps/` folders and `uxagentprojects/`; in an exported zip the `AppModules` and `AppModuleSiteMaps` elements of `customizations.xml`, which the parser already opens for workflows. The `alm-sample` fixture has the first two; M46's plan confirms the zip mapping from a packed copy of it, and has one CLI-level test per layout.
- Two rules: **Error:** a site map `Page` or `GenPageId` target that neither the solution nor a declared dependency holds (not `Entity` targets, which point at tables every environment has, such as `systemuser`), the test PO001 and PO004 apply: a `Page` target fires only when the solution carries `CanvasApps/`, a `GenPageId` target only once F2 shows an export carries `uxagentprojects/`, and otherwise it says "not checked" (D10). **Warning:** an app over 25 custom pages, or whose pages use more than 10 connectors or 20 connection references, read from the `CanvasApps/*.meta.xml` the parser already loads.

Deliberately doesn't: form parsing (no kept rule reads a form); role parsing and a no-read-role rule (the `alm-sample` app module maps two roles whose ids aren't in its `Roles/` folder, so absent role metadata can't be read as absent permission; a plan line and review item in M42 instead); a dashboard-controls Error (no fixture holds a dashboard, the unpacked path is unconfirmed, and Learn gives the maximum of 20 without saying which step refuses more; a plan line in M42 instead); controls in the footer (the source page is deprecated); a 150-business-rules rule; the 5/75/10 form sizes and the 6-tables-per-generative-page limit as rules (review questions in M42); count reports (no named report reads them).

Stops if: nothing. Without F2 the `GenPageId` half of the site-map rule says "not checked" (D10) and the reader skips `uxagentprojects/`; with it, that half is turned on.

Touches: `Solution`, `SolutionParser`, two rules, goldens. No eval baseline: no case holds an app module (confirmed by the review). No schema change.

#### M47. Code app dependencies (`feat:`, v0.30.0)

Adds:

- **One rule, warning:** an app with `CanvasAppType` 4 whose `CdsDependencies` or `DatabaseReferences` names a table or connector that is neither in the solution nor a declared dependency. The message names the app as a code app. Microsoft's own repo records a pipeline failure from stale dependencies after references were renamed. The metadata is in the `CanvasApps/*.meta.xml` the parser already loads.
- Tests build the metadata by hand: the SDK, CLI and Vite plugin are under Microsoft Software License Terms, so no real bundle goes in a fixture, and an exported `iconUri` carried an expired signed URL, so public exports are read for shape only.

Deliberately doesn't: naming code apps as a kind in `analyze` or `diff` output (the `diff` component list is a closed enum in its schema, so that is a contract change under D9, for no check that needs it); a 500 kB chunk rule (Vite's default warning, and Microsoft's own sample raises the limit; M42's review asks instead); `CodeAppPackageUris` against files (what an import does when they disagree is unknown); source checks on `power.config.json` or TypeScript; anything that runs `pa`.

Stops if: the public exports disagree on the metadata shape. F3 (the unpack round-trip in Dev) is wanted but not required; without it M47 says it rests on public exports.

Touches: one rule, goldens. No eval baseline, no schema change.

#### Held: a Power BI `pbip check`

Outside the committed sequence until D5 says yes and a real PBIP exists to test on. If it is ever planned: a separate read-only command over a PBIP folder, at most seven rules (an unresolved field reference as the one Error; data cache not ignored; a literal host in M; the two-way relationship share; a measure without a format; auto date tables; missing alt text; not the visual count, which D4 keeps a review item), a TMDL line scanner with no TOM dependency, its own schema and golden, and no change to `analyze`, so the pinned roles see the same output. Stop rule inside it: if `powerbi-report-author validate` already checks field references, drop that rule, and without it and with no real PBIP user, don't build it. It would never port the 71 BPA rules, judge design by rule, deploy, refresh or publish, or bundle the Power BI Authoring MCP (its EULA bars redistribution).

### Stage two: Copilot Studio and MCP

Held until stage one is done or deferred under D11. M17 stays on hold. "MCP" here means three things the person named: MCP apps on Power Platform, MCP tools for Copilot Studio agents, and the new checks reaching the `powerops` MCP server. The third needs no work: rules reach it through `analyze`. The first gets a route note and a hand-off, not checks: Microsoft documents MCP apps for declarative agents in Copilot Chat, App MCP in model-driven apps (preview, needs a Microsoft 365 Copilot licence) and the Power Apps MCP server (preview), and nothing shows a standard Copilot Studio agent rendering them.

Harness: stage two covers agents on Copilot Studio's standard harness, the rule-based one with topics, as Learn's harness overview names it (https://learn.microsoft.com/microsoft-copilot-studio/harnesses-overview). <!-- plan-status: allow --> A check that can't place an agent in that harness says "not checked".

#### M48. Agent reader and rules (`feat:`, v0.31.0)

Adds:

- `references/copilot-studio.md` in `powerops-plan`: the standard harness first; a route note for MCP apps that hands off to Microsoft's `mcp-apps` plugin; the review items below.
- One reader over `bots/`, `botcomponents/` and `Assets/botcomponent_connectionreferenceset.xml` into a `Solution` field. YamlDotNet is already in Core. A workspace in `.mcs.yml` form reaches it through `pac copilot pack`, which keeps the rule "call `pac`".
- At most five rules: **Error:** an MCP tool (a component with `ModelContextProtocolMetadata`) on an agent whose configuration has generative orchestration off; both facts are in the export. **Warning:** instructions empty or over 8,000 characters. **Warning:** authentication None. **Warning:** a tool connection in Maker mode. **Warning:** more than 30 tools, or "not checked" where an MCP tool is present, because offline can't count a server's tools.
- The fixture per D7.

Deliberately doesn't: a transport rule (Streamable applies to MCP connections only, and the declaration sits in the connector definition, which joins the agent through a connection reference and may not be in the export; a review item instead: each MCP connector names the Streamable protocol); a 128-tool Error (no failure above it is documented); the `.` in a topic name (export already refuses it); knowledge-source counts and description checks (review items); judging instruction quality; schema or language-server validation; running evaluations; generating widgets.

Stops if: F6 shows that `pac copilot pack` writes a layout the sample doesn't match, and D7 gives no fixture. If an eval case ever holds an agent, its rules wait for D1.

Touches: `Solution`, `SolutionParser`, five rules, one fixture, goldens. No eval baseline today. No schema change.

#### M49. Agent `diff`, gate and docs (`feat:`, v0.32.0)

Adds: `diff` lists added tools and changed instructions as risk items, which means new values in the `component` and `risks` enums of `docs/schemas/diff.schema.json` and in the goldens, under the contract D9 sets; a route for `pac copilot init --environment` if M48's plan found it writes to a tenant; `docs/HANDOFF.md` gets the agent row with the post-import checklist (Application Insights, manual authentication, channels, sharing; a republish after an environment variable change); the docs say what the gate can't see for agents (VS Code Apply, skill pushes, the Copilot Studio MCP's `manage_agents`). Live checks (data policy, publish state, connection binding, evaluations) are marked waiting.

Deliberately doesn't: `copilot_*` passthroughs; a remote `powerops mcp`; any evaluation run (M17).

Stops if: D9 isn't answered, or `diff`'s new lines reach an eval baseline (none today).

### Won't build

Cut because the goal doesn't need it, a source doesn't support it, or it belongs to Microsoft. Each can return as its own milestone with its own evidence.

- Writing `.pa.yaml`, FormXML, PBIR or agent YAML. Compiling or type-checking Power Fx. Reimplementing App Checker, the flow checker, Site Checker, solution checker or delegation tables. A connector rate table (Learn holds it). Porting BPA's 71 Power BI rules. Running ESLint, axe or Playwright. Hosting MCP servers, or `powerops mcp` as a remote agent tool. `copilot_*` passthroughs. Judging visual design by rule.
- A rule on table or list counts: no source gives a maximum. A plan-against-build tool that parses the ledger (M41 says why). Data metrics in `analyze` (tables, columns, lookups, ownership, lists per app): no named report reads them, and `IsSecured` or `IsAuditEnabled` in text output would show two planted flaws and drop the Security reviewer below its floor.
- Flow rules for a solution flow with a direct connection and for an update-triggered flow that updates its own table: the first is an ALM risk, not a size or efficiency one, and it fires on an eval case, so it would cost a rerun for a rule the goal didn't ask for; Learn says the product warns about the second on save. A rule for a missing trigger condition. A Catch-never-fails rule.
- Canvas rules for 256,000 characters (a correlation; the worst public apps reach 41,988), more than 2 dependent screens (a runtime figure that is "subject to change"), `TabIndex` (treated as 0), `FluentV8/*` (Learn scopes it to Teams and custom pages) and control counts (five sources, five numbers). `AppCheckerResult.sarif` until a fresh export shows it.
- Model-driven form parsing, role parsing, a no-read-role rule (absent role metadata isn't absent permission), a dashboard-controls Error (no fixture, an unconfirmed layout and little value), footer controls, 150 business rules, the 5/75/10 form sizes and 6 tables per generative page. Any count report with no reader.
- A Power Pages wildcard **Error** (an exemption can't be seen offline, and the API is off by default), the standalone `.powerpages-site/` layout, rules for Global scope, role counts, local login, bearer authentication, `*Secret` settings and header or encoding settings, and Liquid checks. A SharePoint reader. Capacity reports.
- Naming code apps as a kind in `analyze` or `diff` output, a Code Apps 500 kB chunk rule, the package-URI check, and source checks on TypeScript or `power.config.json`. Content scans of built SDK code.
- An agent transport Error, a 128-tool Error and the `.` topic-name rule. A rule or check for MCP apps.
- A `double`-column warning for Power BI, anywhere: the right numeric type depends on precision and range.
- A control map, `data-control` names or token work in the prototype until D8's build has run. A second approval gate of PowerOps's own. A new MCP tool per check. Always-on context in any skill. Pasting Microsoft's text into the skills.
- A `pbip check` unless D5 says yes (Held).
- An allow-list gate, unless D2 chooses it.

### How many rules

Stage one adds 11 rules (2 flows, 1 canvas, 5 Power Pages, 2 model-driven, 1 Code Apps), stage two 5, and the held `pbip check` up to 7 if it is ever built. Before the cuts the research proposed 45 to 50; the first draft of this plan had 19. Each rule still costs a docs page with a dated Sources line, a firing and a quiet test, and golden rows, so a reader milestone ships a rule only when a test or fixture shows it firing on a real shape, and its plan may ship fewer. Stage one has one Error (the site-map target) and stage two one (an MCP tool without generative orchestration).

## Automation

Each milestone is one PR with its own signed-off plan (`depth: quick` where its rows above give the change exactly, `standard` for the readers and the skills), built by the AGENTS.md recipes and checked as the Goal says, plus the leak scan.

Tests:

- **Rules:** a firing and a quiet test in `ErrorRuleTests` or `WarningRuleTests` with the `B.*` builders, written first; `RuleDocsTests` and `RulesCommandTests` for the page and its Sources line; `DocsDriftTests` for the README rule table. A warning page's Sources line cites Learn; where a number comes from a Microsoft skill (PO016), the page says so and cites Learn for the risk. A rule whose input the export may not hold has a "not checked" test (D10).
- **Readers:** `TempFolder` for folder layouts and `SolutionZip` for zips, with the error form `InvalidDataException` whose message starts with the path, and one CLI-level test per real layout (unpacked folder and packed zip) for M45 and M46. A reader milestone also runs the readers' hostile-input theory from Phase 6 on its new input, so a deep or oversized file is refused, not read.
- **Skills:** `SkillsCommandTests` and `PluginManifestTests` keep the shipped text, the front matter and the plugin copies in step; a test counts the words of `powerops-plan/SKILL.md` against its M41 ceiling, so the core skill can't swell quietly; `HandoffDocTests` for the hand-off page and its hook matchers; two scenarios in the weekly `skills-smoke.sh` (M42).
- **The gate:** `ImportCall` tests for each new route and spelling, for each MCP tool name's hook input, and for the forms that still pass (a mention in a commit message is blocked, as today). The toy-server check from M21, repeated by hand for the new matchers and recorded.
- **Eval:** `RoleEvalTests` on every milestone. A milestone that changes a baseline stops and records D1's answer in its plan before the build goes on. With D6, M41 adds `plan-13` and `plan-14` and uses the existing `review-plan` pass of `run.sh`, with the client versions, cost and time recorded as M39 did.
- **Goldens and schemas:** the `analyze-fixtures.*` goldens change in M43 (six flows), M45 (one new solution found) and wherever a fixture gains a finding; each is updated with the recipe and its diff read. Stage one changes no JSON schema. M49 changes the `diff` schema's `component` and `risks` enums under D9, with the goldens and `OutputSchemaTests` updated.

Compatibility, the changes a CI user or an agent can see: new warnings in `analyze` (M43 to M48) that change no exit code under the default `--fail-on error`; one new Error for model-driven apps (M46, a site-map target the export proves missing) and one for agents (M48) that fail CI on a solution with that fault; new `deploy gate` refusals and hook matchers (M40, M49); new `note:` lines on stderr for inputs an export doesn't hold (M44, M46; D10); new findings in `analyze` on solutions holding sites or agents, which can fail a `--fail-on warning` job that passed before; new `diff` enum values (M49, D9). Each milestone's PR lists its changes.

## Screens

The prototype, offered when `## Screens` names content the maker lays out freely:

| Product | Offer | Why |
|---|---|---|
| Canvas | Yes, with a "doesn't map" list | 27 modern controls, 7 of them preview. Accordion, breadcrumb, dialog, drawer, menu, nav, toast, toolbar and tree have no modern control; each names a workaround or goes |
| Custom pages | Yes | The canvas subset, with no modern theme |
| Power Pages, classic | Yes | Read the Bootstrap version first. A hand-written CSS subset with Bootstrap class names, never the bundled stylesheet |
| Power Pages, code site | Yes | Name the UI library |
| Generative pages | Yes; the person attaches a screenshot of it in the page builder | Learn says detailed sketches work best, and the builder takes images |
| Power BI | Yes: one 1920 by 1080 page, a box per visual, no chart library | PBIR stores positions, so a static sketch maps directly, and chart fidelity can't be promised |
| Code Apps | A mock-data first slice instead | The real thing is HTML, so a sketch would be rebuilt. The first slice is Step 1 of the build: hand-written mock data under plain `npm run dev`, no push, recorded in `## Log`. Observed from Microsoft's sample source, not run, and not a documented path; Local Play still needs a tenant |
| Model-driven forms, views, site map | No | Metadata-driven, responsive by default, and `app-builder`'s `preview-form.js` already prints a wireframe. The text list in `## Screens` is the equivalent |
| Flows, Dataverse, agents | No | No screens |

The file: one static `docs/plans/<name>/mockups/prototype.html`, inline CSS, no scripts, fonts, images or external links, fake data, a visible "Sketch" label, at most 6 screens and 15 KB (a limit drawn from two small samples; revisit on a real plan). A header comment names the route and, for canvas, the pasteable theme YAML, the one part that transfers directly. Flex and `gap`, no absolute positions. Contrast from the tokens (4.5:1 text, 3:1 non-text), visible labels, DOM order equal to reading order, no positive `tabindex`. A passing sketch proves intent, not the build.

What keeps it buildable: a short, dated "doesn't map" list for canvas and a Bootstrap note for Pages, in the reference files. The fuller control map waits for D8's one build, because nothing shows a build skill honours it. Whether `create-site` uses an outside prototype at all is untested.

Recording and the roles: one menu option on both paths and one `## Log` line. No line under `## Screens`, no new header key, section or command. Visual detail stays out of `Screens` and `Steps`, and no Step says "match the mockup", so the Verifier, which compares those sections, needs no change. Sign-off hashes only the `plan.md` body, so a mockup edit doesn't void it. The Lead passes the HTML path, not a screenshot, except for a generative page as above.

The milestones themselves change no screens.

## Environments and route

Every milestone is built and tested offline, with fakes. No milestone runs `--apply`, an import, or a `pac` or `az` call against an environment, and none needs a live service for its build. The Dev facts are of three kinds: one **required** prerequisite (F1: M44 is built after it, or deferred under D11); **optional evidence** (F2, F3, F5), which narrows a milestone's scope when absent and never blocks it; and one **optional experiment** (F4), outside every milestone's completion. F6 is offline. Each export is read-only and needs the person's yes in that session; the builds F2 and F4 need first are tenant writes, each approved on its own under `powerops-implement` step 7.

| Fact | Kind | What it needs | Used by |
|---|---|---|---|
| F1. A fresh canvas export's `.msapp` holds `Src/*.pa.yaml`; whether `AppCheckerResult.sarif` and a `.msapr` are there | Required | One canvas export from Dev | M44 |
| F2. An app's export brings its generative pages, and `pac solution unpack` keeps `uxagentprojects/` | Optional evidence | A generative page built in Dev through `genpage`, then one export | M46, the `GenPageId` half |
| F3. A code app round-trips through `pac solution unpack` at the pinned `pac` versions | Optional evidence | A code app in Dev, which needs the admin toggle | M47; without it, public exports only |
| F4. One canvas build with and without a prototype, and whether `canvas-app` uses a passed HTML path (D8) | Optional experiment | A build in Dev through `canvas-app`, and a person who saves and publishes | Later prototype detail, no milestone's completion |
| F5. A real site export matches the public Pages layout | Optional evidence | One site export (Power Pages is available) | M45 |
| F6. `pac copilot pack` on the MIT workspace fixture gives the sample's layout | Offline | Nothing in a tenant: `pack` is local and needs no sign-in | M48 |

Not needed now: how a SharePoint action's `dataset` and `table` appear in a flow export, and how an environment-variable-backed site setting serialises; the items that wanted them are cut.

F1 to F3 are read early, in one session if the person agrees, while M40 to M43 are built, which don't need them; Dev's availability is time-bound.

Live services the person named: Power BI (Pro or Fabric), Power Pages, and none for Copilot Studio. The Windows machine can produce a PBIP, but nobody has, so the held `pbip check` has no user yet (D5).

## Solution and publisher

None: no solution changes. Fixtures that are public Microsoft samples stay unmodified; a fixture made for this project uses fake values and is listed in `tests/fixtures/NOTICE.md`.

## Constraints

- **The pin.** The skill text of `powerops-verify` and `powerops-review-security` isn't changed. A change to a check, command, stop rule, input or report line would rerun that pass on all its cases and both supported clients, recompute the keep rules and renew the person's decision. Every milestone avoids it; D1 settles what counts for both roles.
- **AGENTS.md.** `pac` is called, never reimplemented, with arguments as a list. Offline first. Read-only by default; every write goes through `SafeWrite`. No `settings apply --apply`, `deploy apply --apply` or `deploy_apply` with `apply: true` without the person's yes to that environment in the session. No secrets, tenant URLs or ids in commits; no hand-edited versions or changelog.
- **Rules.** Error only where the import or a run breaks on what the export proves; Warning means risk. A rule needs a number a source states for that scope, low noise on public samples and a real failure or risk; otherwise it is a review item. The flow size rule is the one exception, because the brief orders it, and D3 records that. Conflicting numbers from one family of sources make a review item, not a rule.
- **Incomplete exports (D10).** Evidence the layout doesn't carry says "not checked": one `note:` line on stderr, no finding, no exit-code change. A reference of a kind the layout does carry that neither the solution nor a declared dependency holds is an Error, as PO001 and PO004 are today, and a missing declared file stays a refusal.
- **Open sources only.** Learn, public docs, MIT or Apache-2.0 repos, and the observed output of tools run normally. No decompiling, and nothing licence-restricted, even to inform a choice. The canvas-authoring MCP package, the Power BI Authoring MCP and the Code Apps SDK, CLI and Vite plugin are under Microsoft's own terms: PowerOps reads their public docs and MIT skills and never depends on their internals or bundles them. Fluent fonts and Fabric icons can't be embedded.
- **Dependencies.** None added. MIT or Apache-2.0 only, ever. JsonSchema.Net stays at 8.0.5. YamlDotNet, already in Core, reads the agent YAML.
- **The MCP server.** Stdio only, no new tool for a rule, no `copilot_*` passthroughs.
- **Output contracts.** Stage one adds findings only. A new value in a closed enum of a JSON schema (M49) follows D9.
- **Skill size.** `powerops-plan` stays near its size; product facts live in reference files loaded per product. No always-on context.
- **Stage two** starts only when stage one is done or deferred under D11, and never picks up M17 without the person's word.
- **This plan may become public.** No personal or tenant details, no tenant schedule dates, no paths to private notes, no agent or model names. Fake values in examples.

## Assumptions

- M39 is merged (`2b25cc3`), so the five skills, the `reports` list and the pinned text are what this plan builds on.
- The eval baselines hold text output only, so `--json` additions never reach them. Read in `RoleEvalTests` at `2b25cc3`.
- No eval case holds an `.msapp`, a Power Pages site, a code app, an agent or an app module (the review confirmed the last).
- `skills install` copies every embedded `skills/**` file and the plugin copies regenerate through "Update goldens", so reference files beside `SKILL.md` reach every client; M41 adds a test that one lands.
- Both clients' hook matchers can name several tools in one group; M40's plan confirms the form for each and falls back to one group per tool.
- The Dev environment stays available long enough for F1 to F4, and Power Pages for F5. If not, M44 is deferred under D11 and the rest ship narrower.
- The research's measured counts are the researchers' and weren't recomputed; no milestone's done-when rests on them.
- Hooks are off while building in Dev, as `docs/HANDOFF.md` says, so the new gate routes don't slow the Dev loop.
- Environment facts were not read: this plan changes this repo and targets no environment.

## Open questions

No named decision is open. The person answered all eleven on 2026-11-02, taking the recommendation on each (`decisions.md`, Q1 to Q11). They stay below, the eight from the standard and then three the review added, because the milestones refer to them by number.

**D1. The pinned-role ruling, and whether the ledger is an input change, for both pinned roles.** Proposed ruling: an output change that reaches no eval baseline needs no rerun; one that does needs one, costed in that milestone's plan. Separately: does the ledger's new `plan.md` layout count as an input change to both roles, which read the plan, when every eval case uses the old layout? Decided: adopt the ruling; and treat the ledger as not an input change, with the known risk that neither role's measured behaviour covers a ledger-shaped plan. With "yes", M41's stop rule runs the full pass (24 runs) before the skill ships. Blocks M41's plan, and any rule milestone whose rule fires on an eval case.

**D2. Deny-list or allow-list for `deploy gate`.** A deny-list keeps missing new tools; an allow-list of reads risks blocking normal work, and the gate runs only while hooks are on. Decided: keep the deny-list, add M40's routes and matchers, and keep the docs' list of what the gate can't see. Blocks M40.

**D3. Adopting 50 actions and 4 levels for PO016** from a Microsoft MIT skill whose Learn page states neither number. Decided: adopt, as a warning, with the rule page naming the provenance and defining depth, because Learn gives no soft number and the hard limits catch nothing. Blocks PO016 in M43; with "no" it stays a review item in M41.

**D4. Power BI's visual-count threshold: 12, 15 or 20 data visuals a page.** Learn says to limit visuals and gives no number; Microsoft's skill says 12, a community rule set 15, a tool default 20. Decided: 12, as a review item in `power-bi.md`, never a rule. Doesn't block; M42 writes 12 and changes one line on another answer.

**D5. Whether the held `pbip check` is ever built.** It needs a real PBIP to test on and a user who produces one, and nothing on this project's Linux machine can. Decided: not now; revisit when a PBIP exists, and first run `powerbi-report-author validate` to see whether it already checks field references, which decides the command's one Error rule. Blocks only the held item.

**D6. Whether the oversized and compact plan cases join `tests/eval/roles`.** Decided: yes, in M41, two cases through the existing `review-plan` pass, once per supported client, scored as M41 says. Doesn't block M41's text, but decides its done-when.

**D7. The agent fixture: scrub Microsoft's order-management zip, or build an own one.** Scrubbing breaks "public Microsoft samples stay unmodified", and the zip holds a dev-tunnel host, a default publisher and Maker mode. Decided: neither; pack the MIT workspace fixture from the vscode-copilotstudio repo with `pac copilot pack`, which is local and needs no sign-in, and keep that output unmodified if it holds no tenant values (F6 checks). Firing cases are hand-built in tests. Blocks M48's fixture.

**D8. Whether to run one canvas build with and without a prototype**, the only direct evidence for the prototype's effect, which decides whether a control map or `data-control` names are worth adding. Decided: yes, once, in Dev, through `canvas-app`, after M42 and while Dev is available, with the person saving and publishing (F4). Blocks nothing in M42; blocks any later prototype detail.

**D9. The machine-readable `diff` contract.** The `diff` schema's `component` and `risks` lists are closed enums, and M49's agent lines need new values. A new enum value is not like a new field: the schemas README keeps new fields compatible because the schemas allow extra properties, but a consumer that validates output against the published schema rejects a value the enum doesn't list, which is the kind of change the README's rule bumps the version for. Options: bump `diff`'s `schemaVersion` to 2 with the first new value, so a validating consumer updates its schema once (decided; this plan's first draft said otherwise); keep 1 and widen the enums, which breaks such consumers silently until they refresh the schema and needs a documented migration note; or keep agents out of `diff` and drop that part of M49. Blocks M49.

**D10. Incomplete exports.** Two cases. Evidence the export layout doesn't carry (no `Src` in an old `.msapp`, `uxagentprojects/` when unpack drops it, a mapped role or a connector definition outside the solution): decided, "not checked", one `note:` line on stderr, no finding, no exit-code change, and the rule page names the inputs it needs. A reference of a kind the layout does carry that neither the solution nor a declared dependency holds: a proven missing component, an Error as PO001 and PO004 are today, and a missing declared file stays a refusal. M46's `Page` targets are the second case; its `GenPageId` targets the first until F2 is in. Doesn't block, but M44, M46 and M48 build to it.

**D11. What an accepted deferral means for stage completion.** Decided: the rule under "Completion": built, or deferred by the person's named acceptance recorded in BUILD-PLAN with the reason and the scope left open, and a deferral doesn't block the next stage. Blocks nothing now; decides when stage two may start.

Facts that need no tenant and no decision: whether `powerbi-report-author validate` checks fields against the model (a run of that tool, for the held item); which licences qualify in a Managed Environment (`data.md` states only what the licensing page supports).

## Questions for your admin

- Is "Enable code apps" turned on in the Dev environment? F3 and M47 need it; without it M47 rests on public exports.
- Which Power BI plan applies, Pro or Fabric capacity? M42's licence-fit text states Pro's limits either way; the held item and any deploy route depend on the answer.

## Steps

1. Done on 2026-11-02: the person answered D1 to D11, taking each recommendation.
2. Read the Dev facts F1, F2 and F3 in one session if the person agrees, each export after its own yes, and record the answers in the BUILD-PLAN verification record. The build that F2 needs is a tenant write approved on its own first. This runs alongside steps 3 to 6, which don't need them.
3. M40, build-order item 75 (`fix:`): its quick plan, then the gate routes, the MCP recognition and matchers, the toy-server check, the PO005 fix and the docs lines, each with its tests.
4. M41, item 76 (`feat:`): its plan, then the ledger, the data round with its `quick` rule, `references/data.md`, the flow card, review item 11, the skill-size test and, with D6, the two plan cases through the existing `review-plan` pass; regenerate the plugin copies. With D1 "yes", the full pass of both roles and the renewed decision before the skill ships.
5. M42, item 77 (`feat:`): its plan, then the product reference files, the prototype offer on both paths and its log line, the Power BI branch, the review additions, the hand-off lines, the HANDOFF rows and the smoke-test scenario; regenerate the plugin copies.
6. M43, item 78 (`feat:`): its quick plan, then PO016 (per D3) and PO017, their pages, the goldens, and `RoleEvalTests` run before and after. M40 to M43 are the first release checkpoint.
7. M44, item 79 (`feat:`): its plan, after F1, or deferred under D11 without it; the `.msapp` read, the note, PO018 and the hostile-input cases. F4 runs after M42 if D8 says yes, outside any milestone's completion.
8. M45, item 80 (`feat:`): its plan, with the zip paths from a packed copy of the fixture; the fixture with its NOTICE row, the reader, the five rules, the CLI-level tests per layout, the goldens.
9. M46, item 81 (`feat:`): its plan; the reader for both layouts, the two rules with the `GenPageId` half turned on only if F2 is in by then, the goldens.
10. M47, item 82 (`feat:`): its quick plan; the one rule, the goldens.
11. After stage one's done-when is met under D11: M48, item 83 (`feat:`), and M49, item 84 (`feat:`), each with its own plan, with D7's fixture, F6 and D9 first.
12. After each milestone, mark it built or deferred in BUILD-PLAN (a Phase 7 section with the milestone table, its decisions numbered from 40, and build-order items 75 to 84), as Phases 3 to 6 did; after the last, set the Phase 7 status and the FOUNDATION roadmap row.
