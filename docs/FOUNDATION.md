# PowerOps: foundation plan

**One line:** a free, open-source, cross-platform toolkit that brings software-engineering discipline (analysis, diffing, testing, repeatable deployment) to Microsoft Power Platform. It works alongside `pac`; it does not replace it.

Draft of 2026-10-19. The research behind it is in [research/landscape-2026-10.md](research/landscape-2026-10.md).

## 1. Why now

Over the last 18 months Microsoft has stepped back from several pieces of pro-developer ALM tooling. Power Apps Test Engine is deprecated, the CoE Starter Kit is archived, ALM Accelerator is deprecated, and `pac canvas pack/unpack` is deprecated. Meanwhile the platform has moved to Git integration, a YAML solution format, Pipelines, and an API-first admin plane.

What Microsoft ships is in-product (Pipelines, Git integration) or a thin wrapper over `pac` (Actions, Build Tools). The space in between has no maintained, cross-platform, CI-native owner. That space covers knowing what a change does before you deploy it, testing it, and proving environments match.

## 2. Positioning

| PowerOps is | PowerOps is not |
|---|---|
| A CLI and library that runs headless in CI on Linux, macOS and Windows | Another desktop GUI (XrmToolBox and Power Platform ToolBox already cover that) |
| Offline-first: most commands read an exported or unpacked solution, with no tenant needed | A replacement for `pac`, which it calls for pack, import and check |
| Machine-readable output (JSON, SARIF, Markdown for PR comments) | A low-code maker tool |
| Agent-friendly: stable output, a dry-run plan mode, read-only by default, with an MCP server added later | Built around an "AI skill". Microsoft already ships skill packs with more than 1k stars. |

## 3. The four pillars, mapped to real gaps

| Pillar | First capabilities | Gap it fills (evidence in research §3) |
|---|---|---|
| **Solution analysis** | `powerops analyze`: custom rules over unpacked solutions (naming, hard-coded URLs, missing connection references, unmanaged leaks, dependency and layer risk), SARIF output, a baseline file | Solution Checker can't take custom rules or overrides when enforced. The only OSS alternative is a 1★ PowerShell module. |
| **ALM** | `powerops diff`: component-level semantic diff of two solution versions, rendered as Markdown for PRs. `powerops settings`: generate, validate and diff deployment settings per environment. | Unpack noise, `.pa.yaml` merges done by hand, and more than 9 open issues on connection references and environment variables |
| **Testing** | Flow test harness (mock connector outputs, assert on run results). A Playwright starter that wraps Microsoft's samples. | Test Engine is deprecated, and no flow test framework exists anywhere |
| **Deployment automation** | `powerops deploy --plan`: a dependency-ordered multi-solution deploy that binds connection references and turns flows back on. `powerops drift`: compares Dev, Test and Prod. A composite GitHub Action. | Pipelines deploy one solution at a time, upgrade-only. Flows arrive switched off after a service-principal import. Nothing detects drift since the CoE kit was archived. |

## 4. Proposed technical baseline

These need your approval; see the open decisions in §7.

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
- **Test tenants are getting harder to get.** The Developer Plan can't be used to create environments after 2026-11-15.
  - Mitigation: offline-first design and committed fixture solutions. We need to find out what contributors can use instead (see research topics).
- **Competitors.**
  - ALM4Dataverse is a framework with one maintainer and daily commits. TALXIS/tools-cli is an agent-driven CLI.
  - Mitigation: differentiate on offline analysis, diff and drift, plus composability. Revisit this before Phase 3.
- **Licence contamination.**
  - XrmToolBox and Power Platform ToolBox are GPL-3, and FakeXrmEasy v2 is RPL or commercial.
  - Mitigation: don't copy their code, and don't take them as dependencies.
- **Scope creep across four pillars.**
  - Mitigation: phases are strictly ordered, and Phase 1 alone must be useful.

## 7. Open decisions (yours)

1. **License:** Apache-2.0 (explicit patent grant; recommended) or MIT (simplest, and what Microsoft's tooling uses)?
2. **Stack:** confirm .NET 10, or do you prefer TypeScript or Python?
3. **Name check:** confirm "PowerOps" is free on GitHub, NuGet and npm, and has no trademark conflict.
4. **Your tenant:** do you have a Power Platform environment for Phase 2+ testing, and what licence does it use?
5. **CI target:** GitHub Actions only at first, or Azure DevOps from the start too?

## 8. Research topics still open

To verify before or during the phase that depends on them:

- [ ] **(Phase 1)** Exact YAML solution format support in `pac solution unpack/clone/sync`. The docs contradict themselves.
- [ ] **(Phase 1)** Whether `microsoft/PowerApps-Tooling` can parse `.pa.yaml` well enough for a semantic diff.
- [ ] **(Phase 1)** SARIF rule IDs and severities used by Solution Checker, so results line up with ours.
- [ ] **(Phase 2)** Web API calls to bind connection references and turn flows on as a service principal.
- [ ] **(Phase 3)** Power Platform API calls to create and delete environments (the ephemeral-environment pattern), plus cost and capacity limits.
- [ ] **(Phase 3)** How Pipelines and Git integration interact with external CI, and whether we should trigger Pipelines rather than import directly.
- [ ] **(Phase 3)** The licence of ALM4Dataverse and how far it overlaps with us.
- [ ] **(Phase 4)** Flow run-history and static-results APIs, to find the best mocking approach.
- [ ] **(Phase 4)** GA status of Copilot Studio evaluation.
- [ ] **(All)** Contributor tenant options after the Developer Plan change on 2026-11-15.
- [ ] **(Release)** NuGet trusted publishing setup, including the reported expiry of legacy API keys on 2026-11-01 (U).
