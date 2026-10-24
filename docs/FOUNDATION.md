# PowerOps: foundation plan

**One line:** a free, open-source, cross-platform toolkit that brings software-engineering discipline (analysis, diffing, testing, repeatable deployment) to Microsoft Power Platform. It works alongside `pac`; it does not replace it.

Last updated 2026-10-21.

## 1. Why now

Over the last 18 months Microsoft has stepped back from several pieces of pro-developer ALM tooling. Power Apps Test Engine is deprecated, the CoE Starter Kit is archived, ALM Accelerator is deprecated, and `pac canvas pack/unpack` is deprecated. Meanwhile the platform has moved to Git integration (GA since [2025-04-22](https://www.microsoft.com/power-platform/blog/power-apps/git-integration-is-generally-available/)), a YAML solution format, Pipelines, and an API-first admin plane.

What Microsoft ships is in-product (Pipelines, Git integration) or a thin wrapper over `pac` (Actions, Build Tools). The space in between has no maintained, cross-platform, CI-native owner. That space covers knowing what a change does before you deploy it, testing it, and proving environments match.

## 2. Positioning

| PowerOps is | PowerOps is not |
|---|---|
| A CLI and library that runs headless in CI on Linux, macOS and Windows | Another desktop GUI (XrmToolBox and Power Platform ToolBox already cover that) |
| Offline-first: most commands read an exported or unpacked solution, with no tenant needed | A replacement for `pac`, which it calls for pack, import and check |
| Machine-readable output (JSON, SARIF, Markdown for PR comments) | A low-code maker tool |
| Agent-friendly: stable output, a dry-run plan mode, read-only by default, planning skills that gate implementation on a signed-off plan ([design](PLANNING-SKILLS.md)), and an MCP server later | A replacement for Microsoft's skills ([power-platform-skills](https://github.com/microsoft/power-platform-skills)). Those focus on building, and their one planner covers Power Pages ALM. Ours plan any Power Platform project, add an independent review and a CI check, work across agents, and work alongside Microsoft's. |

**Who uses it.** The main users are agents: PowerOps hooks into the user's own Claude Code or Codex, and the agents those tools run call it. So every feature has to be fast for an agent to read, check and repeat, and easy for the person behind the agent to follow. In practice:

- **A stable contract.** Versioned JSON with published schemas, and findings that say how to fix themselves, so an agent can analyze, fix and recheck without reading prose.
- **Talk at the user's pace.** The planning skills go as deep or stay as brief as the user's answers and engagement ask for, and remember that level for the next session.
- **Low-friction versions.** Each version is planned, reviewed and signed off, and a new plan builds on the previous one instead of starting over, so iteration stays quick without lowering the bar.
- **Repeatable recipes.** Adding a rule, reader, command or fixture follows written steps any agent can run, and claims about Microsoft's platform carry dated sources that are rechecked each milestone.

## 3. The four pillars, mapped to real gaps

| Pillar | First capabilities | Gap it fills |
|---|---|---|
| **Solution analysis** | `powerops analyze`: custom rules over unpacked solutions (naming, hard-coded URLs, missing connection references, unmanaged leaks, dependency and layer risk), SARIF output, a baseline file | Solution Checker runs as an online service and can't take custom rules or per-component suppressions. When it is enforced in Managed Environments you can exclude rules, but not add your own. The only OSS alternative is a 1★ PowerShell module. |
| **ALM** | `powerops diff`: component-level semantic diff of two solution versions, rendered as Markdown for PRs. `powerops settings`: generate, validate and diff deployment settings per environment. | Unpack noise, `.pa.yaml` merges done by hand, and more than 9 open issues on connection references and environment variables |
| **Testing** | Flow test harness (mock connector outputs, assert on run results). A Playwright starter that wraps Microsoft's samples. | Test Engine is deprecated, and no flow test framework exists anywhere |
| **Deployment automation** | `powerops deploy --plan`: a dependency-ordered multi-solution deploy that binds connection references and turns flows back on. `powerops drift`: compares Dev, Test and Prod. A composite GitHub Action. | Pipelines deploy one solution at a time, upgrade-only. Flows arrive switched off after a service-principal import. Nothing detects drift since the CoE kit was archived. |

## 4. Technical baseline

- **Language:** C# on **.NET 10 (LTS)**, shipped as a `dotnet` global tool.
  - It has the only mature official Dataverse SDK (`Microsoft.PowerPlatform.Dataverse.Client`).
  - It runs on the same runtime as `pac`.
  - It can reuse `microsoft/PowerApps-Tooling` (MIT) for `.pa.yaml`.
- **Shape:**
  - A core library, with a thin CLI on top and later a thin MCP server on top of that.
  - `pac` is called as a subprocess. It is never re-implemented.
  - Playwright testing lives in a separate TypeScript package if we build it.
- **Distribution, in order:**
  1. NuGet tool with trusted publishing.
  2. A composite GitHub Action.
  3. Self-contained binaries and a container image.
  4. An Azure DevOps task and a VS Code extension only if people ask for them.
- **CI auth:** an Entra app with a GitHub OIDC federated credential. No stored secrets anywhere in examples.
- **Repo hygiene from day one:**
  - LICENSE, README, CONTRIBUTING, CODE_OF_CONDUCT, SECURITY.md (private vulnerability reporting), issue and PR templates.
  - Conventional Commits with release-please.
  - Actions pinned by SHA with least-privilege tokens.
  - Dependabot, and secret scanning with push protection.
  - OpenSSF Scorecard, a CycloneDX SBOM, and build attestations.

## 5. Roadmap

The ordered milestones for Phases 1–3 are in [BUILD-PLAN.md](BUILD-PLAN.md).

Each phase ends in something releasable. Phases 0 and 1 need **no Power Platform tenant**, which keeps contribution easy.

| Phase | Deliverable | Done when |
|---|---|---|
| **0. Foundation** | Repo scaffolding, CI (build, test, lint), release pipeline, docs skeleton, fixture solutions | `dotnet tool install` of a v0.1.0 that prints `--version`, published from CI |
| **1. Offline analysis and diff** | `analyze` (about 10 rules, SARIF, baseline), `diff` (Markdown output), GitHub Action that comments on PRs | Running it on a real exported solution finds real issues, and the PR comment is readable |
| **1b. Planning skills** | `plan`, `review-plan` and `implement` skills for Claude Code, Codex and other agents; `powerops skills install`; `powerops plan facts` (names-only environment facts); `powerops plan status` | A vague idea becomes a reviewed, signed-off plan, and CI fails if a plan was signed off by hand or edited after sign-off |
| **1c. Agent base** | Versioned JSON output with schemas; fix hints on every finding; `powerops rules --json`; plan `depth` and `supersedes`; AGENTS.md recipes; dated sources in rule docs | An agent adds a rule from the recipe alone, and every JSON output validates against its schema |
| **2. Settings and connections** | `settings generate/validate/diff`, and turning flows on after import | Replaces a hand-written `deploymentsettings.json` workflow end to end |
| **3. Environment-aware** | `drift`, `deploy --plan`, pre-deploy layer and impact check, ephemeral environment per PR | A Dev→Test promotion runs from GitHub Actions with a plan reviewed in the PR |
| **4. Testing** | Flow test harness, Playwright starter, a Copilot Studio eval gate | A failing flow assertion blocks a merge |
| **5. Agents** | MCP server over the same core | An agent can run analyze, diff and plan, but never writes without an explicit flag |

## 6. Risks

- **Microsoft churn.** Anything built on a preview API can break.
  - Mitigation: depend on GA surfaces (`pac solution`, the Web API, the SARIF format), and isolate everything else behind adapters.
- **Test tenants are getting harder to get.** [Unmanaged developer environments](https://learn.microsoft.com/power-platform/important-changes-coming), including those from the free Developer Plan, can't be created after 2026-11-15. Existing ones are suspended from 2026-12-15 and deleted from 2027-01-15. Only managed developer environments remain, and each user can create one.
  - Mitigation: offline-first design and committed fixture solutions. Phase 3's ephemeral environments must be managed environments. We still need to find out what contributors can use (see §8).
- **Competitors.**
  - ALM4Dataverse is an actively developed ALM framework.
    - Since [v1.18](https://github.com/ALM4Dataverse/ALM4Dataverse/releases/tag/v1.18.0) (2026-10) it also deploys organization and Power Platform settings, which overlaps with Phase 2.
    - It has no diff, SARIF or drift features.
  - TALXIS/tools-cli is an agent-driven CLI.
  - Mitigation: differentiate on offline analysis, diff and drift, plus composability. Revisit this before Phase 2.
- **Licence contamination.**
  - XrmToolBox and Power Platform ToolBox are GPL-3, and FakeXrmEasy v2 is RPL or commercial.
  - Mitigation: don't copy their code, and don't take them as dependencies.
- **Agent harness churn.** Skill formats, question tools and model selection differ between agents and change often.
  - Mitigation: one `SKILL.md` per skill with plain-text fallbacks; the gate lives in plan files and `powerops plan status`, not in any one agent.
- **Scope creep across four pillars.**
  - Mitigation: phases are strictly ordered, and Phase 1 alone must be useful.

## 7. Decisions

| Date | Decision | Why |
|---|---|---|
| 2026-10-19 | License: **Apache-2.0** | Permissive, with an explicit patent grant for users and contributors |
| 2026-10-19 | Stack: **C# on .NET 10 (LTS)**, shipped as the `PowerOps` dotnet tool with the command `powerops` | It has the only mature official Dataverse SDK and runs on the same runtime as `pac` |
| 2026-10-19 | CI: **GitHub Actions first**. An Azure DevOps task comes only if people ask for it. | One wrapper to maintain until there is demand for a second |
| 2026-10-19 | Releases: Conventional Commits, **release-please**, NuGet trusted publishing (OIDC) with build provenance, starting at 0.1.0 | No long-lived publish keys, and changelogs are generated from commit history |
| 2026-10-19 | **Planning skills** ship to PowerOps users, built after M2: question rounds until the user stops, review in a fresh context on the provider's top model, and no implementation without a signed-off plan (the agent drafts one if the user skips planning). See [PLANNING-SKILLS.md](PLANNING-SKILLS.md). | Agents build well from clear plans; most Power Platform mistakes are made before building starts |
| 2026-10-20 | **Agents are the main users.** The agent base (Phase 1c, BUILD-PLAN M4) comes before the YAML layout and `diff` | PowerOps runs inside the user's Claude Code or Codex. A versioned contract, fix hints and recipes are cheapest to add before the first NuGet release and before more outputs exist |

## 8. Research topics still open

To verify before or during the phase that depends on them:

- [ ] **(Phase 1)** YAML solution format support in `pac`. Checked 2026-10-21, with pac 2.13.1 installed locally (observed offline; nothing contacted a tenant):
  - YAML needs pac 2.4.1 or later ([pac solution reference](https://learn.microsoft.com/power-platform/developer/cli/reference/solution)).
  - `pack`: reads the YAML layout. It auto-detects `solutions/*/solution.yml`, and there is no flag. Observed: packing `tests/fixtures/powerops-seed-yaml/v1` printed "YAML format" and exited 0. Multi-solution repos need SolutionPackager ([YAML format reference](https://learn.microsoft.com/power-platform/alm/solution-source-control-yaml-format)).
  - `unpack`: writes the XML format only. Observed: a zip unpacked to `Other/Solution.xml` and no `.yml` file. Unpacking into an existing YAML folder with `--allowWrite --allowDelete` added XML files beside the YAML and deleted 49 "unnecessary" files. Never point `unpack` at a YAML folder.
  - A component listed in `rootcomponents.yml` without source files is left out of the zip. M5 turned this into rule PO013. Observed: the seed fixture packed with "root components are not defined in customizations" for 3 components, and exit 0.
  - Still open: `clone` and `sync`, which need a tenant. The pages conflict. [FAQs about source code integration](https://learn.microsoft.com/power-platform/alm/git-integration/faqs) (updated 2026-10-14, the newest) says `unpack`, `clone` and `sync` don't currently support YAML. The [YAML format reference](https://learn.microsoft.com/power-platform/alm/solution-source-control-yaml-format) (updated 2026-08-24) says `clone` and `sync` produce it. The `sync` help of pac 2.13.1 still describes `--solution-folder` as the folder with `Other/Solution.xml` (observed), which fits the FAQ. Treat `clone` and `sync` output as unverified. PowerOps reads both layouts, so nothing in the code depends on it.
- [ ] **(Phase 1b)** Whether a read-only `pac` query can return publisher prefixes. Checked in docs on 2026-10-21:
  - `pac env fetch --xml "<fetch>…</fetch>"` (or `--xmlFile`) exists in the [pac env reference](https://learn.microsoft.com/power-platform/developer/cli/reference/env). It runs FetchXML against the selected environment.
  - The [publisher table](https://learn.microsoft.com/power-apps/developer/data-platform/reference/entities/publisher) allows reading `uniquename` and `customizationprefix`.
  - Not documented: the output format of `env fetch`, and that it is read-only (FetchXML only reads, but the page doesn't say so). `pac solution list --json` has no documented output fields.
  - A solution on disk already has the prefix (`Publisher/CustomizationPrefix` in `Other/Solution.xml`, observed in a fixture; `publisher.yml` in the YAML layout), so no query is needed there.
  - Still open: run `pac env fetch` once against a real environment and record the output. Until then `powerops plan facts` asks for the prefix.
  - `powerops drift` does not wait for this. It reads through the Dataverse Web API instead, because `pac env fetch` has no documented output format (decision 15 in [BUILD-PLAN.md](BUILD-PLAN.md#decisions-phase-3-2026-10-21), Q6 of [plans/m8-drift](plans/m8-drift/plan.md)). This item stays open for `plan facts`.
- [ ] **(Phase 1b)** Whether Claude Code, Codex, Gemini CLI and Copilot all ignore `SKILL.md` front matter fields they don't know. Checked 2026-10-21:
  - Claude Code: yes, per its [skills docs](https://code.claude.com/docs/en/skills). Unknown fields are ignored. Claude.ai uploads and the Skills API reject keys outside the spec's six fields.
  - Codex and Gemini CLI: docs are silent. Their source ignores unknown keys ([Codex parser.rs](https://github.com/openai/codex/blob/main/codex-rs/skills/src/parser.rs), [Gemini skillLoader.ts](https://github.com/google-gemini/gemini-cli/blob/main/packages/core/src/skills/skillLoader.ts); read on `main`, not a pinned commit).
  - Copilot: not documented, and no source found ([VS Code](https://code.visualstudio.com/docs/copilot/customization/agent-skills), [GitHub](https://docs.github.com/en/copilot/how-tos/copilot-cli/customize-copilot/add-skills)). Unverified.
  - So the rule stays: use only the [spec's portable fields](https://agentskills.io/specification). Keep `name` equal to the folder name (lowercase, hyphens, at most 64 characters) and `description` at most 1024 characters, since Copilot in VS Code and the spec require the match.
  - Still open: Copilot's behavior on unknown fields.
- [x] **(Phase 1)** Whether `microsoft/PowerApps-Tooling` can parse `.pa.yaml` well enough for a semantic diff. Answered 2026-10-21: not as a dependency today, so M5 and M6 keep reading `.pa.yaml` with YamlDotNet.
  - [`Microsoft.PowerPlatform.PowerApps.Persistence` on NuGet](https://www.nuget.org/packages/Microsoft.PowerPlatform.PowerApps.Persistence): four versions, all preview. The latest is 0.8.2735.117-preview from 2024-06-28, net8.0 only, MIT, about 1,000 downloads.
  - The [repo](https://github.com/microsoft/PowerApps-Tooling) is active (last push 2026-10-13) and its `master` targets net8.0, net10.0 and net48. Nothing newer than the 2024 preview is released.
  - The library can deserialize `.pa.yaml` into records, with each property as a Power Fx formula wrapper. Not checked: what that wrapper exposes, and whether it round-trips.
  - The [Learn page](https://learn.microsoft.com/power-apps/maker/canvas-apps/power-apps-yaml) says the schema is "in active development" and the files are read-only. Microsoft has made no statement on supporting the library.
  - Revisit when a stable or net10.0 build is published.
- [ ] **(Phase 1)** SARIF rule IDs and severities used by Solution Checker, so results line up with ours. Checked in docs on 2026-10-21:
  - The checker returns a zip of SARIF files. The [checker API overview](https://learn.microsoft.com/power-platform/alm/checker-api/overview) says "version two of the OASIS standard" (SARIF 2.0).
  - Rule IDs are lowercase kebab-case, for example `meta-remove-dup-reg` and `il-avoid-specialized-update-ops` (from the `--ruleLevelOverride` example in the [pac solution reference](https://learn.microsoft.com/power-platform/developer/cli/reference/solution)). The rule list is in [Use solution checker](https://learn.microsoft.com/power-apps/maker/data-platform/use-powerapps-checker). Web resource IDs probably carry a `web-` prefix, unconfirmed.
  - Severities are Critical, High, Medium, Low and Informational.
  - Not documented: how severity maps to SARIF `level`, where category and severity sit in the file, the file names in the zip, and an example. The ruleset GUID differs between two Learn pages.
  - Still open: needs one real checker result, which means uploading a solution to the checker service. Keep the mapping out of the code until then.
- [ ] **(Phase 2)** Web API calls to bind connection references and turn flows on as a service principal.
- [x] **(Phase 3)** Power Platform API calls to create and delete environments (the ephemeral-environment pattern), plus cost and capacity limits. Starting point: environment provisioning and Reset Environment in the [programmability changelog](https://learn.microsoft.com/power-platform/admin/programmability-whats-new-changed). Ephemeral environments must be managed environments. Answered 2026-10-21 in the [BUILD-PLAN Phase 3 research basis](BUILD-PLAN.md#phase-3-research-basis): `pac admin create|delete` wait by default, a sandbox needs at least 1 GB of database capacity, and managed environments need premium licences. So M11 is an opt-in recipe and not part of our CI. Still open: whether a service principal can sign in to an environment it just created. Stays open: the M11 recipe was built as docs and not run, so it does not settle it; see the "Not verified" table in [docs/examples/README.md](examples/README.md#example-an-ephemeral-environment-per-pull-request).
- [x] **(Phase 3)** How Pipelines and Git integration interact with external CI, and whether we should trigger Pipelines rather than import directly. Git integration gained [GitHub support and public APIs](https://learn.microsoft.com/power-platform/release-plan/2026wave1/power-platform-governance-administration/github-support-source-code-integration-power-platform) (public preview since 2026-08-31), which may let PowerOps trigger or inspect a Git sync. Answered 2026-10-21 ([research basis](BUILD-PLAN.md#phase-3-research-basis)): import directly through `pac`. Pipelines deploys one solution per run, with a fixed import mode, and can't carry a plan (BUILD-PLAN decision 13).
- [x] **(Phase 2)** The licence of ALM4Dataverse, and how far its settings deployment overlaps with ours. Answered 2026-10-21:
  - [ALM4Dataverse/ALM4Dataverse](https://github.com/ALM4Dataverse/ALM4Dataverse) (owner rnwood, created 2026-01-02) still has no licence: the GitHub API returns `license: null` and there is no `LICENSE` file. All rights are reserved by default, so do not copy its code.
  - It is a PowerShell pipeline framework for Azure DevOps and GitHub Actions, and it is active (releases v1.18.0 to v1.19.1 on 2026-10-19 to 20).
  - Overlap is partial. It takes per-environment values from CI variables, activates flows under a configured service-account user, and uses a service principal or OIDC. It documents no deployment settings file, offline validation or `pac`-style generation, which is where PowerOps differs.
  - Not read: its pipeline code, so how it binds connection references is unverified.
- [ ] **(Phase 4)** Flow run-history and static-results APIs, to find the best mocking approach.
- [x] **(Phase 4)** GA status of Copilot Studio evaluation. [Answered](https://learn.microsoft.com/microsoft-copilot-studio/whats-new):
  - Agent evaluations are GA (March 2026).
  - Running them through the Power Platform API is in preview (April 2026).
  - The `pac copilot-studio` evaluation commands are still preview.
- [x] **(All)** Contributor tenant options now that unmanaged developer environments are going away. Answered 2026-10-21 in [Important changes coming](https://learn.microsoft.com/power-platform/important-changes-coming#deprecation-of-unmanaged-developer-environments-and-the-power-apps-developer-plan): no new ones from 2026-11-15, suspended from 2026-12-15, deleted from 2027-01-15. The Developer Plan stays free but creates no environment after 2026-11-15. One managed developer environment per user remains. It needs an admin to enable Managed Environments and a premium licence to run apps and flows ([developer plan](https://learn.microsoft.com/power-platform/developer/plan)).
  - What a contributor can use: a 30-day [trial environment](https://learn.microsoft.com/power-platform/admin/trial-environments) (throwaway, work or school account recommended); a [Microsoft 365 Developer Program](https://learn.microsoft.com/office/developer-program/microsoft-365-developer-program-faq) sandbox tenant (free, but only for Visual Studio subscribers, eligible partners and Premier/Unified Support customers; it does not document a Dataverse environment); or an environment in a paid tenant. Dataverse for Teams is deprecated too.
  - Service principals: documented for Dataverse environments in general ([pac admin create-service-principal](https://learn.microsoft.com/power-platform/developer/cli/reference/admin#pac-admin-create-service-principal)), not for trial or managed developer environments specifically.
  - Plan: contributors need no tenant. Offline fixtures are the default, live tests stay opt-in and read connection details from environment variables, and CI live tests run in a maintainer-owned sandbox with a service principal.
- [ ] **(Release)** NuGet trusted publishing setup, including the reported expiry of legacy API keys on 2026-11-01 (U).
- [ ] **(Release)** Trademark check for the name "PowerOps". The GitHub repo name and the NuGet package ID were both free on 2026-10-19. Checked 2026-10-21, and it is not a clearance:
  - The USPTO and TMview search pages need JavaScript and could not be read. EUIPO, WIPO and UK IPO were not tried. A person has to run those searches.
  - Collision: a PowerShell module named [PowerOps](https://www.powershellgallery.com/packages/PowerOps) "to manage the Power Platform APIs" is published by Microsoft's Customer Architecture Team ("(c) Microsoft", version 0.2.0 from 2023-01-16, 12,104 downloads). Its project URL `github.com/Microsoft/PowerOps` returned 404 on 2026-10-21.
  - A "PowerOps" Power Platform managed-service offer is listed on the [Microsoft marketplace](https://marketplace.microsoft.com/nl-be/product/ebecsltd1579875407689.powerops-powerplatform_enabler) (listing not opened).
  - [Microsoft's trademark page](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks) says not to combine Microsoft names with other terms and not to imply affiliation (read as a summary, not in full). It says nothing about "Power" alone.
  - Still open: register searches in the software classes (9 and 42), and whether to rename or ask Microsoft (trademarks@microsoft.com) given the existing module.
