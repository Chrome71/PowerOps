# Landscape research: Power Platform, DevOps, software engineering

Snapshot as of 2026-10-19. Sources are Microsoft Learn, GitHub (star counts and activity checked against the API), and blogs.
**(U)** marks a claim that is unverified or comes from a single secondary source. Check it before building on it.

## 1. What Microsoft ships today

### Changes that shape this project

| Change | Status | Source |
|---|---|---|
| **Power Apps Test Engine** and `pac test run` | Deprecated since April 2026. The replacement is the Power Platform Playwright *samples* (a sample repo with no releases). | [important changes](https://learn.microsoft.com/power-platform/important-changes-coming), [pac test](https://learn.microsoft.com/power-platform/developer/cli/reference/test) |
| **CoE Starter Kit** | Repo archived 2026-05-15. Its features are moving into the admin center UI. | [coe-starter-kit](https://github.com/microsoft/coe-starter-kit) |
| **ALM Accelerator** | Deprecated: no new features, and issues are no longer reviewed. Microsoft points to Pipelines. | [ALM Accelerator](https://learn.microsoft.com/power-platform/guidance/alm-accelerator/overview) |
| **`pac canvas pack/unpack`** | Deprecated. Microsoft points to Dataverse Git integration and `.pa.yaml`. | [pac canvas](https://learn.microsoft.com/power-platform/developer/cli/reference/canvas) |
| **`pac code`** | Being replaced by the npm-based `pa` CLI for code apps. | [code apps CLI](https://learn.microsoft.com/power-apps/developer/code-apps/reference/cli) |
| **Developer Plan / unmanaged developer environments** | Can no longer be created after 2026-11-15. Deleted on 2027-01-15. | [important changes](https://learn.microsoft.com/power-platform/important-changes-coming) |
| **Managed Environments for Pipelines targets** | Enforced from October 2026, with a 30-day approval window and one 30-day extension. | [pipelines](https://learn.microsoft.com/power-platform/alm/pipelines) |

### Power Platform CLI (`pac`, a .NET tool, version 2.x)
- `pac solution` is GA. Commands: init, clone, sync, export, import (`--settings-file`, `--stage-and-upgrade`), pack, unpack, check, upgrade, version (`--strategy gittags`), create-settings.
- The new **YAML solution format** from Git integration is only supported by `pack`. The `unpack`, `clone` and `sync` docs contradict each other on this (U). Multi-solution YAML repos need SolutionPackager directly.
- `pac solution check` wraps the Checker service. It returns SARIF v2 and supports `--ruleLevelOverride`.
- `pac pipeline` has only `deploy` and `list`.
- New in 2026:
  - Auto-generated **Power Platform API command groups** with 200+ commands, in preview since 2026-09-17.
  - `pac auth token`.
  - `pac copilot mcp --run`, a local MCP server that needs .NET 10.
  - `pac copilot-studio` evaluation commands (preview).
  - [blog post](https://devblogs.microsoft.com/powerplatform/power-platform-cli-%e2%9d%a4%ef%b8%8f-power-platform-api/)
- Windows-only: `pac tool`, and parts of `pac package deploy` and `pac data`.

### Native ALM
- **Pipelines in Power Platform** (GA):
  - Deploys solutions together with their connection references and environment variables, with approvals, delegated deployment and redeploy of earlier versions.
  - Limits: upgrade-only, one solution per deployment, no cross-tenant deployment, managed solutions only.
- **Dataverse Git integration** (GA, date not found (U)):
  - Stores solutions in YAML, with canvas apps as `.pa.yaml`. Works with Azure DevOps and GitHub.
  - Binds to one branch at a time and requires Managed Environments.
  - Merge conflicts have to be resolved by hand in Git.
  - Code apps are not supported.
  - [FAQ](https://learn.microsoft.com/power-platform/alm/git-integration/faqs)
- **Deployment settings files:** `pac solution create-settings` generates them and `import --settings-file` consumes them. Most of the time they are written by hand.
- **Layering:** the top layer wins. Patches are not recommended. A failed upgrade can leave a stuck `_Upgrade` holding solution. [solution layers](https://learn.microsoft.com/power-platform/alm/solution-layers-alm)

### CI/CD wrappers (thin layers over pac)
- [microsoft/powerplatform-actions](https://github.com/microsoft/powerplatform-actions): v1.9.2 (2026-05), MIT, about 306 stars, 60 open issues.
- [Power Platform Build Tools for Azure DevOps](https://github.com/microsoft/powerplatform-build-tools): v2.0.158 (2026-10-09). Recommends workload identity federation.
- [microsoft/powerplatform-cli-wrapper](https://github.com/microsoft/powerplatform-cli-wrapper): the shared core behind both.

### Quality and testing
- **Solution Checker / Checker API** is GA and returns SARIF. When enforced through Managed Environments it allows **no rule overrides or exclusions**. The PowerShell module needs Windows PowerShell. [Checker API overview](https://learn.microsoft.com/power-platform/alm/checker-api/overview)
- **Power Automate** has **no unit-test framework or CLI**. Testing is limited to Flow Checker, the manual Test pane, and static results (mocked outputs) in the designer. [test guidance](https://learn.microsoft.com/power-automate/guidance/coding-guidelines/test-cloud-flows)
- **Copilot Studio evaluation** is in preview, with a cap of 100 test cases per set and results kept for 89 days.
- **Playwright samples** cover canvas, model-driven, custom pages and Gen UX in TypeScript. It is a sample to fork, not a versioned package. [samples](https://github.com/microsoft/power-platform-playwright-samples)

### Governance and infrastructure as code
- [Terraform provider](https://github.com/microsoft/terraform-provider-power-platform): v4.2.1 (2026-10), active. Covers environments, managed environments, solutions, DLP and tenant settings. GA status unclear (U).
- A Pulumi provider exists and supports federated credentials. [Pulumi registry](https://www.pulumi.com/registry/packages/powerplatform/)
- The Power Platform API is the "API-first" management plane, with .NET and Python SDKs. Parts of it are still in preview.

### AI and agents (a crowded space; not a differentiator on its own)
- **Dataverse MCP server:** remote at `/api/mcp`, GA 2025-12 (U on the exact date). Billed per Copilot Credit when used outside Copilot Studio. [docs](https://learn.microsoft.com/power-apps/maker/data-platform/data-platform-mcp)
- **microsoft/power-platform-skills:** about 1,025 stars. **microsoft/Dataverse-skills:** about 246 stars. Both are agent skill packs for Claude Code and Copilot CLI.
- The pac team already treats `pac` as the execution harness for coding agents.

## 2. Community and third-party tools

| Tool | What it does | License | Status (2026) |
|---|---|---|---|
| [XrmToolBox](https://github.com/MscrmTools/XrmToolBox) | Windows desktop host for 30+ plugins (layers, dependencies, FetchXML, SQL 4 CDS) | GPL-3.0 | Active, ~653★ |
| [Power Platform ToolBox](https://github.com/PowerPlatformToolBox/desktop-app) | Cross-platform successor to XrmToolBox; GUI only | GPL-3.0 | Very active, ~65★ |
| [PowerDocu](https://github.com/modery/PowerDocu) | Generates documentation from flows, apps and solutions | MIT | Active, ~651★ |
| [microsoft/PowerApps-Tooling](https://github.com/microsoft/PowerApps-Tooling) | C# library for `.msapp` / `.pa.yaml` | MIT | Active, ~445★ |
| [FakeXrmEasy v2](https://github.com/DynamicsValue/fake-xrm-easy) | Dataverse fake for plugin unit tests | RPL-1.5 / PolyForm NC / commercial | Active. Most business use needs a paid licence. |
| [Fake4Dataverse](https://github.com/ALM4Dataverse/Fake4Dataverse) | Free replacement for FakeXrmEasy | MIT | Small, ~3★ |
| [Rnwood.Dataverse.Data.PowerShell](https://github.com/ALM4Dataverse/Rnwood.Dataverse.Data.PowerShell) | Successor to the stale Microsoft.Xrm.Data.PowerShell | MIT | Very active |
| [xrm-ci-framework](https://github.com/WaelHamze/xrm-ci-framework) | Legacy Azure DevOps tasks | MIT | Alive, ~194★ |
| [EasyRepro](https://github.com/microsoft/EasyRepro) | Selenium UI tests | MIT | Slow (last activity 2024-10) |
| spkl, Daxif, Microsoft.Xrm.Data.PowerShell | Plugin deployment and scripting | various | Stale or dormant |
| [dvdrift](https://github.com/jakobhoeg/dvdrift) | Component-level diff of solution exports | MIT | Tiny (2★), but shows demand |
| [PowerPlatformChecker](https://github.com/autosysops/PowerShell_PowerPlatformChecker) | Offline static analysis of solution exports | MIT (the PSGallery listing says "All rights reserved") | Tiny |

**Closest competitors to watch:**
- [ALM4Dataverse](https://github.com/ALM4Dataverse/ALM4Dataverse): an ALM framework for Azure DevOps and GitHub. One maintainer, early stage, committed to daily, licence unclear.
- [TALXIS/tools-cli](https://github.com/TALXIS/tools-cli): an agent-driven Dataverse CLI. MIT, ~20★.

**Commercial:** no credible paid ALM vendor for Dataverse was found. Gearset and Copado showed no Dataverse support.

## 3. Recurring pain points (with evidence)
1. **Connection references and environment variables.**
   - After a service-principal import, flows arrive switched off.
   - Settings files drift from the environments they target.
   - Issues: powerplatform-actions #517, #650, #679; build-tools #540, #764, #905, #997, #1346.
2. **Solution layers and dependency failures.**
   - Upgrades and uninstalls fail even when the dependency report is clean (MS known issue KA-24343).
   - Removing an unmanaged layer cannot be undone, and the only tool that does it is destructive.
3. **Canvas source diffing and merging.** `.pa.yaml` conflicts are resolved by hand, `unpack` has no YAML support, and large apps hit size limits.
4. **No way to test flows in CI.**
5. **Environment drift** across Dev, Test and Prod. No dedicated tool exists now that the CoE kit is archived.
6. **Tooling churn.** Test Engine, ALM Accelerator, the CoE kit, `pac canvas` and `pac code` have all been deprecated or moved within about 18 months, so there is no stable community default.

## 4. DevOps and software-engineering practice to apply
- **DORA 2025** ([report](https://cloud.google.com/blog/products/ai-machine-learning/announcing-the-2025-dora-report)):
  - AI amplifies existing team strengths and weaknesses. It raises throughput but can hurt stability.
  - There are now five metrics; the new one is **deployment rework rate**.
  - The fixed performance tiers were dropped.
- **Ephemeral environments per PR:** a standard pattern elsewhere, with **no Power Platform guidance**. It could be built on `pac admin create` or the Power Platform API. Fork PRs cannot get secrets, so this needs a two-stage workflow.
- **Low-code test pyramid** (my synthesis):
  - Base: offline lint and rules over unpacked source.
  - Middle: import plus API and flow smoke tests in an ephemeral environment.
  - Top: a few Playwright UI journeys.
- **CI authentication:** an Entra app with a GitHub OIDC federated credential, registered as an Application User in each environment. No stored secrets.
- **Agent-friendly CLI design:**
  - Non-interactive.
  - Stable `--json`/SARIF output and exit codes.
  - Idempotent commands with a `--plan`/dry-run mode.
  - Read-only by default.
  - A thin MCP server over the same core. [speakeasy](https://speakeasy.com/blog/mcp-vs-cli)
- **Supply chain:**
  - NuGet and npm trusted publishing (OIDC).
  - GitHub artifact attestations (SLSA Build L2+).
  - A CycloneDX SBOM.
  - OpenSSF Scorecard.
  - Actions pinned by SHA.
  - Dependabot.
  - Secret scanning with push protection.

### Stack comparison for a Dataverse-aware CLI

| | Official Dataverse SDK | Notes |
|---|---|---|
| **.NET** | `Microsoft.PowerPlatform.Dataverse.Client` (mature; v1.2.26, 2026-07) | Same runtime as pac. Can be a global tool. |
| TypeScript | None (Web API only) | Fits a VS Code extension and the Playwright samples |
| Python | `PowerPlatform-Dataverse-Client` (preview since 2025-12) | Good for scripting; the SDK may still have breaking changes |
| Go | None | Single binary, but every Dataverse call is hand-written |
