# PowerOps: foundation plan

**One line:** a free, open-source, cross-platform toolkit that brings software-engineering discipline (analysis, diffing, testing, repeatable deployment) to Microsoft Power Platform. It works alongside `pac`; it does not replace it.

Last updated 2026-10-19.

## 1. Why now

Over the last 18 months Microsoft has stepped back from several pieces of pro-developer ALM tooling. Power Apps Test Engine is deprecated, the CoE Starter Kit is archived, ALM Accelerator is deprecated, and `pac canvas pack/unpack` is deprecated. Meanwhile the platform has moved to Git integration (GA since [2025-04-22](https://www.microsoft.com/power-platform/blog/power-apps/git-integration-is-generally-available/)), a YAML solution format, Pipelines, and an API-first admin plane.

What Microsoft ships is in-product (Pipelines, Git integration) or a thin wrapper over `pac` (Actions, Build Tools). The space in between has no maintained, cross-platform, CI-native owner. That space covers knowing what a change does before you deploy it, testing it, and proving environments match.

## 2. Positioning

| PowerOps is | PowerOps is not |
|---|---|
| A CLI and library that runs headless in CI on Linux, macOS and Windows | Another desktop GUI (XrmToolBox and Power Platform ToolBox already cover that) |
| Offline-first: most commands read an exported or unpacked solution, with no tenant needed | A replacement for `pac`, which it calls for pack, import and check |
| Machine-readable output (JSON, SARIF, Markdown for PR comments) | A low-code maker tool |
| Agent-friendly: stable output, a dry-run plan mode, read-only by default, with an MCP server added later | Built around an "AI skill". Microsoft already ships skill packs with more than 1k stars. |

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

The ordered milestones for Phases 1–2 are in [BUILD-PLAN.md](BUILD-PLAN.md).

Each phase ends in something releasable. Phases 0 and 1 need **no Power Platform tenant**, which keeps contribution easy.

| Phase | Deliverable | Done when |
|---|---|---|
| **0. Foundation** | Repo scaffolding, CI (build, test, lint), release pipeline, docs skeleton, fixture solutions | `dotnet tool install` of a v0.1.0 that prints `--version`, published from CI |
| **1. Offline analysis and diff** | `analyze` (about 10 rules, SARIF, baseline), `diff` (Markdown output), GitHub Action that comments on PRs | Running it on a real exported solution finds real issues, and the PR comment is readable |
| **2. Settings and connections** | `settings generate/validate/diff`, and turning flows on after import | Replaces a hand-written `deploymentsettings.json` workflow end to end |
| **3. Environment-aware** | `drift`, `deploy --plan`, pre-deploy layer and impact check, ephemeral environment per PR | A Dev→Test promotion runs from GitHub Actions with a plan reviewed in the PR |
| **4. Testing** | Flow test harness, Playwright starter, a Copilot Studio eval gate | A failing flow assertion blocks a merge |
| **5. Agents** | MCP server over the same core, AGENTS.md, a skill | An agent can run analyze, diff and plan, but never writes without an explicit flag |

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
- **Scope creep across four pillars.**
  - Mitigation: phases are strictly ordered, and Phase 1 alone must be useful.

## 7. Decisions

| Date | Decision | Why |
|---|---|---|
| 2026-10-19 | License: **Apache-2.0** | Permissive, with an explicit patent grant for users and contributors |
| 2026-10-19 | Stack: **C# on .NET 10 (LTS)**, shipped as the `PowerOps` dotnet tool with the command `powerops` | It has the only mature official Dataverse SDK and runs on the same runtime as `pac` |
| 2026-10-19 | CI: **GitHub Actions first**. An Azure DevOps task comes only if people ask for it. | One wrapper to maintain until there is demand for a second |
| 2026-10-19 | Releases: Conventional Commits, **release-please**, NuGet trusted publishing (OIDC) with build provenance, starting at 0.1.0 | No long-lived publish keys, and changelogs are generated from commit history |

## 8. Research topics still open

To verify before or during the phase that depends on them:

- [ ] **(Phase 1)** YAML solution format support in `pac`. Partly answered by the [pac solution reference](https://learn.microsoft.com/power-platform/developer/cli/reference/solution):
  - YAML needs pac 2.4.1 or later.
  - `pack` auto-detects `solutions/*/solution.yml`.
  - Multi-solution repos need SolutionPackager.
  - A component listed in `rootcomponents.yml` without source files is silently left out. This is a candidate `analyze` rule.
  - Still to check by hand: how `unpack`, `clone` and `sync` behave with YAML.
- [ ] **(Phase 1)** Whether `microsoft/PowerApps-Tooling` can parse `.pa.yaml` well enough for a semantic diff.
- [ ] **(Phase 1)** SARIF rule IDs and severities used by Solution Checker, so results line up with ours.
- [ ] **(Phase 2)** Web API calls to bind connection references and turn flows on as a service principal.
- [ ] **(Phase 3)** Power Platform API calls to create and delete environments (the ephemeral-environment pattern), plus cost and capacity limits. Starting point: environment provisioning and Reset Environment in the [programmability changelog](https://learn.microsoft.com/power-platform/admin/programmability-whats-new-changed). Ephemeral environments must be managed environments.
- [ ] **(Phase 3)** How Pipelines and Git integration interact with external CI, and whether we should trigger Pipelines rather than import directly. Git integration gained [GitHub support and public APIs](https://learn.microsoft.com/power-platform/release-plan/2026wave1/power-platform-governance-administration/github-support-source-code-integration-power-platform) (public preview since 2026-08-31), which may let PowerOps trigger or inspect a Git sync.
- [ ] **(Phase 2)** The licence of ALM4Dataverse (none stated as of 2026-10-19), and how far its settings deployment overlaps with ours.
- [ ] **(Phase 4)** Flow run-history and static-results APIs, to find the best mocking approach.
- [x] **(Phase 4)** GA status of Copilot Studio evaluation. [Answered](https://learn.microsoft.com/microsoft-copilot-studio/whats-new):
  - Agent evaluations are GA (March 2026).
  - Running them through the Power Platform API is in preview (April 2026).
  - The `pac copilot-studio` evaluation commands are still preview.
- [ ] **(All)** Contributor tenant options now that unmanaged developer environments are going away: no new ones from 2026-11-15, suspended from 2026-12-15, deleted from 2027-01-15. Only one managed developer environment per user remains.
- [ ] **(Release)** NuGet trusted publishing setup, including the reported expiry of legacy API keys on 2026-11-01 (U).
- [ ] **(Release)** Trademark check for the name "PowerOps". The GitHub repo name and the NuGet package ID were both free on 2026-10-19.
