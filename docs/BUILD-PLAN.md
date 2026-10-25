# PowerOps build plan: v0.2 to v0.13

This plan turns [FOUNDATION.md](FOUNDATION.md) Phases 1–3b (including the planning skills, Phase 1b) into ordered, testable milestones. Phase 3 is in [its own section](#phase-3-environment-aware-m8m11), and so is [Phase 3b](#phase-3b-works-with-microsofts-skills-and-mcp-servers-m12m13). Last updated 2026-10-24.

## Where PowerOps fits

Teams that use Dataverse Git integration, or an export → unpack → commit workflow, **already have the unpacked solution in Git**. A pull-request check therefore needs no export, no sign-in and no secrets: just `checkout` plus one PowerOps step.

Today nothing fills that slot well:

- **Solution Checker** runs as an online service, and you can't add your own rules to it.
- **Pipelines** checks only for missing dependencies.
- **`pac solution create-settings`** generates a deployment settings file but never checks it.
- **Microsoft's model-apps skill** now writes one as well (`scripts/solution-settings.js template`, since model-apps 2.15.0) and imports with it (`import`). Its check that each filled entry names one of the package's variables or connection references runs only during that import, and it says "An entry left empty overrides nothing". Nothing in the files read checks the file offline or in CI (read at power-platform-skills `aed70bf`, 2026-10-24).
- **Existing diff tools** don't read the Git-integration YAML layout or canvas `.pa.yaml` files.

The first release therefore answers one question offline, in seconds: **"will this solution import and run cleanly?"**

## Product shape (applies to every milestone)

- **One command does the useful thing with zero configuration.** `powerops analyze` auto-detects every solution under the current folder, in all three layouts:
  - an exported `.zip`
  - an XML unpack (`Other/Solution.xml`)
  - the YAML layout (`solutions/*/solution.yml`)
- **Output formats:** text for people, `--format json|sarif|markdown` for machines, and `--out <file>`.
- **Exit codes:**

  | Code | Meaning |
  |---|---|
  | 0 | Clean |
  | 1 | Findings at or above `--fail-on` (default `error`) |
  | 2 | Usage error |
  | 3 | Internal error |

- **Suppression without a config file.** `powerops baseline` writes the current findings to a file. Pass it back with `--baseline <file>` and only new findings count.
- **Stable rule IDs** (`PO001`…), each with a docs page (`docs/rules/PO001.md`), which is also the SARIF `helpUri`.
- **Agents are the main users.** PowerOps runs inside the user's Claude Code or Codex, where agents call it. Every JSON output carries a `schemaVersion` and has a published schema; every finding says how to fix it; and agents can list the rules with `powerops rules --json` instead of reading docs (M4).
- **Read-only and offline.** It never changes the solution and makes no network calls, except `drift`'s Web API reads (below). The other exceptions go through `pac`: `powerops plan facts` (M3) reads and prints names only, and `powerops settings apply` (M7) reads the target and its flow states and imports a solution only with `--apply --target <name>`. From M8, `powerops drift` reads environments (read-only): `pac org who` gives the name and URL, and Dataverse Web API GET requests, with a token from `az`, give the solution's rows (decision 15). And `powerops deploy apply` (M10) imports only a reviewed plan with `--target <name>`.

## Architecture

```
src/PowerOps.Core/          library: no console, no network
  Model/                    Solution, Component(type, id, schemaName, files), Publisher, Dependency
  Readers/                  ZipReader, XmlUnpackReader, YamlReader → one normalized Solution model
  Flows/                    cloud-flow clientdata parser (connection refs, env-var parameters, state, child flows)
  Rules/                    IRule { Id, Severity, Title, Fix, Evaluate(Solution) → Finding* }, one file per rule
  Output/                   Text, Json, Sarif, Markdown writers
src/PowerOps.Cli/           System.CommandLine commands: analyze, baseline, plan, skills, rules (M4) (later: diff, settings); PacRunner (M3)
tests/PowerOps.Core.Tests/  unit + golden-file tests
tests/PowerOps.Cli.Tests/   end-to-end tests on the packed tool
tests/fixtures/             licensed sample solutions + NOTICE.md
docs/schemas/               JSON Schemas for every JSON output (M4)
action.yml                  composite GitHub Action at the repo root
```

**Dependencies:**
- the .NET base class library (zip, XML, JSON)
- System.CommandLine
- YamlDotNet, from milestone M5 only. The plan header (M3) is plain `key: value` lines read by a small fixed reader, not parsed as YAML.

SARIF is written by hand with System.Text.Json; the Sarif SDK is used only in tests to validate the output. The `powerops` CLI doesn't call `pac` or the Dataverse SDK until M3, and then only read-only: `powerops plan facts` (in `PowerOps.Cli`, through `PacRunner`) runs `pac org who` and `pac solution list` and prints names only. The first write through `pac` is M7's `powerops settings apply`, which runs `pac solution import --settings-file` only with `--apply --target <name>`.

## First rule set

Errors are reserved for things that break deployment. Everything else is a warning.

| ID | Rule | Severity | Milestone |
|---|---|---|---|
| PO001 | A flow uses a connection reference that isn't defined in the solution | error | M1 |
| PO002 | A required environment variable has no default and no value | error | M1 |
| PO003 | A missing dependency has no owning solution (the import will fail) | error | M1 |
| PO004 | A child flow isn't in the solution or its declared dependencies | error | M1 |
| PO005 | A connection reference isn't used by any flow or app | warning | M1 |
| PO006 | A flow contains a hard-coded environment or SharePoint URL | warning | M1 |
| PO007 | An environment variable's default looks environment-specific (URL, GUID, email) | warning | M1 |
| PO008 | An environment variable's current value is shipped inside the solution | warning | M1 |
| PO009 | Component names don't use the publisher prefix | warning | M1 |
| PO010 | The solution uses a default or auto-generated publisher (`new`, `cr###`) | warning | M1 |
| PO011 | A flow was exported turned off | warning | M1 |
| PO012 | A Dataverse "row updated" trigger has no filter columns | warning | M2 |
| PO013 | A component listed in `rootcomponents.yml` has no source folder (pac drops it silently) | error | M5 |
| PO014 | A canvas app formula in a `.pa.yaml` file contains a hard-coded environment or SharePoint site URL | warning | M5 |

Plugin-step and web-resource JavaScript rules are left out, because Solution Checker already covers them.

## Milestones

Each milestone ends in a release that someone can use. "Done when" is checked by tests or a run, not by reading the code.

| Milestone | Release | Scope | Done when |
|---|---|---|---|
| **M0. Test foundation** | none | <ul><li>Move to xunit.v3 on Microsoft.Testing.Platform</li><li>Golden-file helper (`POWEROPS_UPDATE_GOLDEN=1` rewrites the expected files)</li><li>Add `PowerOps.Core` and the core test project</li><li>Fixtures plus `NOTICE.md`</li><li>Fix FOUNDATION.md: Solution Checker does allow rule exclusions, and Git integration went GA on 2025-04-22</li></ul> | CI is green on all three operating systems, and fixtures load in a test |
| **M1. `analyze` on XML and zip** | v0.2.0 | <ul><li>Readers for the zip and XML layouts</li><li>Flow parser</li><li>Rules PO001–PO011</li><li>Text and JSON output</li><li>Exit codes</li></ul> | <ul><li>Each rule has a passing and a failing test</li><li>The CoE fixtures report the expected real hits: env-var default URL (ALM Accelerator for Makers), prefix mismatch, and the declared dependency on Core Components (AuditLogs)</li><li>Golden output matches</li></ul> |
| **M2. CI integration** | v0.3.0 | <ul><li>SARIF output (validated against the 2.1.0 schema)</li><li>`baseline` and `--baseline`</li><li>Markdown output</li><li>PO012</li><li>`action.yml` (job summary and annotations, optional SARIF upload)</li><li>Rule docs pages</li><li>README quick start: one `run:` line, or `uses: Chrome71/PowerOps@v1`</li></ul> | <ul><li>A test repo shows the findings in GitHub code scanning and in the job summary</li><li>A fork pull request still gets the summary, and a refused SARIF upload does not fail the job</li></ul> |
| **M3. Planning skills** | v0.4.0 | <ul><li>`powerops-plan`, `powerops-review-plan` and `powerops-implement` skills in `SKILL.md` format, with plain-text fallbacks</li><li>Plan files under `docs/plans/<name>/` with a plain `key: value` header (five states, `review` field, body hash)</li><li>`powerops plan status` (validates every plan: header, enums, hash, review, tenant patterns; exit codes 0/1/2/3; `--json`) and `powerops plan sign-off <plan>` (writes the date and body hash)</li><li>`powerops plan facts`: names-only environment facts through read-only `pac org who` and `pac solution list`; failure is `{"available": false, ...}` with exit 0</li><li>`powerops skills install`</li><li>A weekly scheduled smoke test with a simulated user (not in per-build CI)</li><li>Supported agents: Claude Code and Codex. Gemini CLI and Copilot are provisional, untested</li><li>Design: [PLANNING-SKILLS.md](PLANNING-SKILLS.md)</li></ul> | <ul><li>Offline tests pass for: header parse and enum errors; hash match and mismatch; review skipped; missing `review.md`; a tenant-pattern hit; `plan facts` unavailable and timed out (recorded fake `pac` output); install idempotency; install refusing to overwrite differing files without `--force`</li><li>One manual run in Claude Code and one in Codex: a one-line idea becomes a signed-off plan through question rounds, and asking to implement without a plan (fast path) produces a drafted plan with route options, not code</li></ul> |
| **M4. Agent base** | v0.5.0 | <ul><li>Versioned output contract: a `schemaVersion` integer at the root of every JSON output (`analyze`, `plan status`, `plan facts`, `rules`); a JSON Schema for each in `docs/schemas/`; additive changes keep the version, removals and renames bump it</li><li>Fix hints: each rule gets a one-line fix; every finding carries `fix` and `helpUri` in JSON, SARIF and Markdown</li><li>`powerops rules [--json]`: every rule's ID, severity, title, fix and `helpUri`</li><li>Plan continuity: optional plan header keys `depth: quick|standard|deep` (set by the plan skill from how engaged the user is, changed when the user changes, reused by later sessions) and `supersedes: <plan>` (a new version builds on the previous plan instead of starting over); `plan status` checks both; PLANNING-SKILLS.md and the skills updated</li><li>Recipes in AGENTS.md for adding a rule, a reader, a command and a fixture: the files to touch, the tests, the golden update and the docs</li><li>Dated sources: every rule doc gets a Sources line with links and a `verified:` date</li></ul> | <ul><li>Every JSON output in the golden tests validates against its schema</li><li>A test checks that every rule has a fix, a docs page and a dated Sources line, and that `rules --json` lists exactly the rules with docs pages</li><li>`plan status` tests: an unknown `depth`, and a `supersedes` that names a missing plan</li><li>One manual run in Codex adds a throwaway rule from the AGENTS.md recipe alone and passes build, test and format (not committed)</li></ul> |
| **M5. YAML layout** | v0.6.0 | <ul><li>YamlReader that produces the same model, for the Dataverse Git integration layout (`solutions/*/solution.yml`)</li><li>PO013</li><li>Basic `.pa.yaml` reading and PO014 (hard-coded URLs in canvas app formulas); PO006 and PO014 share one fix</li></ul> | <ul><li>YAML fixture v1 gives the same findings as the same solution in the XML layout (none)</li><li>YAML fixture v2 gives the same findings as the XML layout (PO005, PO006) plus PO014</li></ul> |
| **M6. `diff`** | v0.7.0 | <ul><li>Component-level semantic diff of two solution versions</li><li>Ignores noise (timestamps, ordering, volatile IDs)</li><li>Risk labels: a managed upgrade deletes removed components; flow on/off changed; connection reference or env var added or removed</li><li>Markdown output for pull-request comments</li><li>JSON output that follows the M4 contract (`schemaVersion` and a schema in `docs/schemas/`)</li></ul> | <ul><li>Diffing the managed/unmanaged sample pair and two versions of a live export shows only meaningful changes</li><li>The output fits in a pull-request comment</li></ul> |
| **M7. `settings`** | v0.8.0 | <ul><li>`settings generate` and `settings validate` offline: compare a `deploymentsettings.json` with the solution's connection references and env vars</li><li>Then the first live feature, behind an explicit `--apply` flag: `settings apply` ([plan](plans/m7-settings-live/plan.md)) runs `pac solution import --settings-file`, which binds the connection references and restores each flow to its export state. It reports every cloud flow's state after the import and exits 1 when one is not on. `pac` has no command that turns a flow on, so powerops cannot do that itself</li></ul> | <ul><li>Validate catches missing and extra entries in fixtures</li><li>A live import round-trip in the test environment leaves the flows on (passed, PR 18, 2026-10-21)</li></ul> |

### Verification record

Evidence for "Done when" items that tests do not cover.

- **M2** (checked 2026-10-21 with `gh`): in a public scratch repo on 2026-10-19, a push run of the action uploaded SARIF, and code scanning shows PO005 and PO006 alerts from the tool "PowerOps". A pull request from a fork owned by another account ran the action successfully. A run with a forced SARIF upload failure still succeeded.
- **M3** (2026-10-21, on `714f7da`): `tests/smoke/skills-smoke.sh` passed all checks in both Codex and Claude Code: question rounds to a signed-off plan, stopping after one round, and the fast path (asking to build with no plan drafts one with route options and changes no other files). The weekly workflow still needs its API key secrets.
- **M4**: the manual Codex recipe run is recorded in PR 13.
- **M7**: the live import check is recorded in PR 18.
- **M12** (2026-10-24, on `163449c`): `tests/smoke/skills-smoke.sh` cases 3 and 4 passed in Codex 0.163.0 and Claude Code 2.1.303, run from a temporary copy without cases 1 and 2. Case 4 puts a fake routing skill beside the PowerOps skills and asks to build a model-driven app without naming a skill: each agent drafted a plan on `powerops-plan`'s fast path and changed no other file. A control run with only the fake router made each agent write the app's files under `src/`. Both agents also had user-scope Power Platform skills on that machine. Two earlier Claude Code runs failed on the test's scripted prefix answer and its menu pattern, both fixed in `163449c` (the [plan](plans/m12-handoff/decisions.md) log has each run). The manual run (a signed-off plan's build step handed to `app-builder` in Dev, then the import into Test through `deploy apply --apply`) was not run: it writes to Dev and imports into Test, and AGENTS.md needs the user's yes to that environment in the session that runs it. The building session asked on 2026-10-24.

## Phase 3: environment-aware (M8–M11)

FOUNDATION Phase 3 is done when **a Dev→Test promotion runs from GitHub Actions with a plan reviewed in the PR**. M8–M10 get there; M11 adds ephemeral environments as an opt-in recipe. Each milestone is planned with `powerops-plan` and signed off before it is built, as M7's live half was.

**Environments (decision 9 below).** Phase 3 runs in the maintainer's own tenant:
- **Dev**: the existing unmanaged developer environment (suspended from 2026-12-15).
- **Test**: a new environment that expires 30 days after it was created. No Dataverse capacity for a sandbox and no developer environment were left (master session, 2026-10-22).
- A Managed Environment needs a premium licence for every user who runs an app or flow there; the Phase 3 plans assume no particular environment type.
- One Entra app with a GitHub federated credential, added as an application user in both environments (M10).

The user sets these up before M8's capture step; Claude does not create environments. The seed solution is unmanaged in Dev and managed in Test. Environment names and ids live only in environment variables or gitignored files.

**What PowerOps reads and writes.** Drift and plans only read:
- `pac org who` (drift): the environment's name and URL
- Dataverse Web API GET requests with FetchXML (drift), with a token from `az account get-access-token`, on the `solution`, `solutioncomponent`, `workflow`, `environmentvariabledefinition` (with `environmentvariablevalue`) and `connectionreference` tables, and on `msdyn_componentlayer` once its read is built. `pac env fetch` is not used (decision 15)
- `pac solution list` (`plan facts`; not read by drift)
- `pac power-automate list-cloud-flows` (M7; not read by drift, which reads flow states from the `workflow` table)

They never export from a target: managed solutions can't be exported, and an unmanaged export leaves out unpublished changes, while publishing writes. The only write is `deploy apply` (M10), which runs `pac solution import` for a reviewed plan. Environments are created and deleted by `pac admin`, in the user's own workflow (M11), never by `powerops`.

| Milestone | Release | Scope | Done when |
|---|---|---|---|
| **M8. `drift`** | v0.9.0 | <ul><li>Built ([plan](plans/m8-drift/plan.md)). Reads, all read-only: `pac org who` (the name and URL; `--env` is what `pac` takes), then Web API FetchXML reads on `solution`, `solutioncomponent`, `workflow` (cloud flows and their states), `environmentvariabledefinition` and `connectionreference`, with a token from `az account get-access-token` (decision 15). `pac solution list` and `pac power-automate list-cloud-flows` are not read. The SDK is not added</li><li>Capture step (2026-10-22): the read-only reads ran in Dev and Test once, scrubbed into `tests/fixtures/pac/drift/capture/`; `sample/` keeps hand-written edge cases shaped as Learn documents them. Component ids change on import for tables, canvas apps, environment variable definitions and connection references (only the flow kept its id), so those are compared by name through their own kinds or not at all</li><li>Layers: on a managed solution, one `msdyn_componentlayer` read per table, flow, canvas app, environment variable definition and connection reference (the table needs the component's id and name; batched or nameless queries return no rows), at most 50; an unmanaged change shows as the `Active` layer. Over the cap, or for other component types, layers are not read, and a skipped solution shows `layers not read` (information, not drift) in place of the earlier "limited to installed unmanaged"</li><li>An environment snapshot model in Core. Per solution: version, managed, components, unmanaged layers where read, env var current values (as SHA-256 hashes), connection reference bindings and flow states</li><li>`powerops drift --solution <name> --env <A> --env <B> [--env <C>]` compares every environment with the first. `--save <dir>` writes the snapshots, and snapshot files can be passed instead of `--env`, so the comparison runs offline</li><li>Settings drift (the folded `settings diff`, decision 12): env var values and connection reference bindings that differ, are missing or are unbound. Values are never printed and there is no `--show-values` (Q2); a differing value is drift. Connection ids are never compared or printed</li><li>Text, JSON (`schemaVersion`, `docs/schemas/drift.schema.json`, and `docs/schemas/snapshot.schema.json` for the saved files) and Markdown output. Exit codes: 0 no drift, 1 drift, 2 usage (also a solution installed in no environment), 3 a `pac`, `az` or Web API read failed or timed out, `pac` or `az` not found, or an unreadable snapshot file</li></ul> | <ul><li>Offline tests on the fixtures in `tests/fixtures/pac/drift/`, with fake `pac`, `az` and web functions: equal snapshots exit 0; a version change, an extra component, an unmanaged layer, a changed env var value, an unbound connection reference and a flow that is off each exit 1 and are named in the output; a failed or timed-out `pac`, `az` or Web API read exits 3; the JSON golden validates against its schema</li><li>A live run on Dev and Test with the seed installed in both reports no drift. After the flow is edited in Test, a second run reports the unmanaged layer and exits 1. Done 2026-10-22: no drift (exit 0), the saved pair offline gave the same text and exit code, the edit gave `unmanaged-layer` (exit 1), and after the edit was removed a last run gave no drift</li></ul> |
| **M9. `deploy plan` and the PR comment** | v0.10.0 | <ul><li>Built ([plan](plans/m9-deploy-plan/plan.md)). Live check passed on 2026-10-23 in the test repo: a PR got one plan comment against Test, a second push edited it, a broken sign-in replaced it with "could not run" and failed the job, and a fork PR got the diff in the job summary with no secrets. A local run and the CI run gave the same plan hash. The changes from this row are in the last bullet</li><li>`powerops deploy plan <folder> --target <env>`: the folder holds exported zips, each with an optional `<zip name>.settings.json` beside it. Read-only</li><li>Order: a topological sort on each zip's dependencies on the other solutions in the folder. A cycle, or a dependency that neither the target nor an earlier zip provides, blocks the plan</li><li>Action per solution, from the target snapshot (M8): install, upgrade (newer version), or skip (same or lower version, as `pac solution import --skip-lower-version`). Managed over unmanaged, or unmanaged over managed, blocks the plan</li><li>Impact and layer check: components that a managed upgrade deletes (M6's risk label, against the target's component list); unmanaged layers on the target's components, which the import does not override; `settings validate` (M7) for each settings file; and the flow states expected after import (M7's prediction)</li><li>A plan file in JSON (`schemaVersion`, `docs/schemas/deploy-plan.schema.json`) with a hash over the zips, the settings files, the target id and the target snapshot. Markdown output for the PR</li><li>Exit codes: 0 plan ok, 1 blocked, 2 usage, 3 unreadable input or `pac` failure</li><li>The GitHub Action posts one PR comment and updates it on each run: the plan when a target is configured, otherwise the offline `diff` of the PR's base and head solutions (the folded Phase 1 item, decision 11). On fork PRs, where the token can't comment, it writes the job summary instead</li><li>Changes from this row: `--target` is an environment passed to `pac` as `drift`'s `--env` is, and is never printed. `--save <file>` also writes the JSON plan file, so one read gives both the Markdown and the plan file. A settings file is `<zip name>.settings.json` beside its zip, and zip and settings file names may hold only letters, digits, `.`, `_` and `-`. The deletion check names workflows, environment variables and connection references; tables and canvas apps are compared by count only, and other component types are counted as not checked (Q2). A missing settings file is checked as `{}`, so a solution with a connection reference or a variable without a value needs one. The settings, dependency and flow checks run for install and upgrade only, and the deletion and layer checks for an upgrade; a skip imports nothing and blocks only on a cycle or a managed conflict. A `<Required>` that names no solution gives a `dependency-not-checked` warning, not a block. Deletions and unmanaged layers are warnings; settings problems, missing dependencies, cycles and managed conflicts block. The plan file holds `tool` (the powerops version) and `inputs` (the hash of each zip, settings file and snapshot, and the dependency versions), so M10 can say which input changed. The PR comment is opt-in (`comment: 'true'`, Q3), so existing workflows need no new permission</li></ul> | <ul><li>Offline tests on fixture zips and recorded snapshots: dependency order, cycle, missing dependency, install, upgrade, skip, managed/unmanaged conflict, a deleted component, an unmanaged layer, a settings error; the same inputs give the same plan hash; goldens validate against the schema</li><li>In a test repo, a PR that changes the seed solution gets one plan comment against Test; a second push edits that comment instead of adding one; a fork PR gets the job summary and its run passes</li></ul> |
| **M10. `deploy apply` from GitHub Actions** | v0.11.0 | <ul><li>Built ([plan](plans/m10-deploy-apply/plan.md)). Live check passed on 2026-10-23 in the test repo with the example workflow: a PR got a plan comment against Test made from a managed export of Dev; after the merge and the `test` reviewer's approval the workflow promoted the seed from Dev to Test, signed in with OIDC only; the flow was in its expected state and a `drift` run found no drift apart from the managed state. A second PR whose plan was followed by turning the flow off in Test made `deploy apply` refuse (exit 1, nothing imported). The changes from this row are in the last bullet</li><li>`powerops deploy apply <plan.json> --target <name>`: re-plans against the target and refuses (exit 1, nothing imported) if the plan hash differs, because the target, a zip or a settings file changed since the review. `--target` must match the plan's target and `pac`'s current environment, as in `settings apply`</li><li>Imports in plan order with `pac solution import --settings-file`, adding `--stage-and-upgrade` for upgrades. It stops at the first failure and reports which solutions were imported (there is no rollback). Afterwards it reports the flow states, as in M7. JSON report with `docs/schemas/deploy-apply.schema.json`</li><li>An example promotion workflow in `docs/examples/`, Dev→Test:<ul><li>On a PR: export the solution as managed from Dev with `pac solution export --managed`, then run `deploy plan` against Test and comment.</li><li>After merge, in a GitHub environment with required reviewers: run `deploy apply` with the reviewed plan artifact.</li><li>Auth is `pac auth create --githubFederated` (OIDC), with no stored secret.</li></ul></li><li>`settings apply` stays as the one-solution command without a plan</li><li>Changes from this row: without `--apply` the command is a dry run that re-plans, runs the same checks and imports nothing (`--apply` imports), like `settings apply`. `--target` is an environment as for `deploy plan`; the check against the plan is on the friendly name `pac org who` prints for it, and a different name exits 2 (it is not compared with pac's current environment). A plan file that is blocked, or was made by another powerops version (`tool`) or schema, is refused (exit 1) before any read of the target. The plan file's folder is the deploy folder: the zips and settings files are read from beside it. After the imports the flows are checked against the state the plan expected, not against `on`, and exit 1 when one differs. The Action gains the inputs `plan-file` and `apply` (`apply.sh`, a dry run unless `apply` is `'true'`). The example workflow has a `find` job that picks the reviewed artifact of the PR's last commit and shows its run id, artifact id and plan hash to the approver, one secret per settings value, and an artifact that is encrypted in a public repository because it holds Dev's export (`docs/examples/README.md`)</li></ul> | <ul><li>Offline tests with a fake `pac`: hash mismatch refuses and runs no import; the import order follows the plan; the first failure stops the run, and the report names the imported solutions; `--target` mismatch exits 2</li><li>Live, in the user's tenant (the Phase 3 done-when): a PR in the test repo gets a plan comment; after review and merge the workflow promotes the seed from Dev to Test with no stored secret; the flows are on; and a `drift` run afterwards finds no drift between Dev and Test, apart from the managed state</li><li>Live negative: a change made in Test between plan and apply makes `deploy apply` refuse</li></ul> |
| **M11. Ephemeral environment per PR (opt-in recipe)** | none (docs) | <ul><li>Built as docs, not verified ([plan](plans/m11-ephemeral-env/plan.md)): the scripts and the workflows have offline tests only (stub `pac` and `gh`; the workflows as text). No live run, by decision of the master session on 2026-10-23, and the maintainer's tenant has no Dataverse capacity for a sandbox (the reason decision 9 changed). The README section lists what is not verified ([docs/examples/README.md](examples/README.md#example-an-ephemeral-environment-per-pull-request))</li><li>`docs/examples/ephemeral-pr.yml`, an opt-in workflow. Our own CI never creates environments (decision 10)</li><li>It runs: `pac admin create --type Sandbox` (a repository variable, `Sandbox` or `Trial` only), then Managed Environments on with `pac admin set-governance-config --protection-level Standard`, then `deploy plan` and `deploy apply` into it, then `pac admin delete` in an `if: always()` step</li><li>Environment names are `<prefix>-<repository id>-pr<number>-<run id>-<attempt>`, matched in full, and a scheduled sweep (a dry run until a variable says otherwise; a manual run follows its own input only) deletes the leftovers of finished runs, with two federated credentials, one for the PR runs and one for the sweep. The PR workflow's own PR-scoped sweep step deletes that PR's finished leftovers without the variable, so a first use starts in a tenant with no environment names that match the pattern</li><li>The docs page opens with the risk of an admin-level service principal and lists what it costs: at least 1 GB of database capacity for every environment that exists at once, and premium licences. It works only for solutions without connection references</li><li>No new `powerops` code. If the recipe needs a change in `powerops`, it becomes its own task with a test</li></ul> | <ul><li>Done offline: tests of both scripts with stub `pac` and `gh`, and of both workflows as text</li><li>Not run (no live run; no capacity for a sandbox): one manual run in the user's tenant: the sandbox is created; the plan shows only installs; the apply succeeds; the environment is deleted; `pac admin list` shows no leftover</li><li>Not run (same reason): a cancelled run deletes its environment. A cancel during the create step is cleaned up by the sweep or the PR's next run, not by the cancelled run</li><li>Not run (same reason): a service principal creating the environment and then signing in to it is not documented. The recipe does not settle it; it is in the README's "Not verified" table, and FOUNDATION section 8 keeps the question open</li></ul> |

### Phase 3 research basis

Checked on Microsoft Learn on 2026-10-21 by the powerops-researcher agent. The Learn tools show no page-updated dates, so the date is the day each page was read. Each milestone's plan rechecks the claims it builds on.

- **Environments.** `pac admin create|delete|reset|copy` have no preview label and wait for completion by default (`--async`, `--max-async-wait-time`). `pac environment-management` is preview. Sources: [pac admin](https://learn.microsoft.com/power-platform/developer/cli/reference/admin), [pac environment-management](https://learn.microsoft.com/power-platform/developer/cli/reference/environment-management). verified: 2026-10-21
- **Capacity and licences.** A sandbox needs at least 1 GB of free database capacity. Every user who runs an app or flow in a managed environment needs a premium licence. Sources: [Create an environment](https://learn.microsoft.com/power-platform/admin/create-environment), [Managed Environments licensing](https://learn.microsoft.com/power-platform/admin/managed-environment-licensing). verified: 2026-10-21
- **Service principals.** They work for environment management. Whether one can create a developer environment, or sign in to an environment it just created, is not documented. Source: [service principal for the Power Platform API](https://learn.microsoft.com/power-platform/admin/powerplatform-api-create-service-principal). verified: 2026-10-21
- **GitHub OIDC.** `pac auth create --githubFederated` takes `--tenant` and `--applicationId`. The admin GitHub actions document only a client secret, so the recipes call `pac` directly. Sources: [pac auth](https://learn.microsoft.com/power-platform/developer/cli/reference/auth), [GitHub Actions with OIDC](https://learn.microsoft.com/power-platform/alm/tutorials/github-actions-oidc-fic), [admin GitHub actions](https://learn.microsoft.com/power-platform/alm/devops-github-available-administrative-actions). verified: 2026-10-21
- **Pipelines.** One solution per run. The import mode is fixed at upgrade without overwrite. Targets must be managed environments, a host environment is needed, and there is no cross-tenant deploy. `pac pipeline deploy` exists. A pre-deployment step can gate a run, but it can't carry a plan (not documented). Sources: [Pipelines](https://learn.microsoft.com/power-platform/alm/pipelines), [Extend pipelines](https://learn.microsoft.com/power-platform/alm/extend-pipelines), [pac pipeline](https://learn.microsoft.com/power-platform/developer/cli/reference/pipeline). verified: 2026-10-21
- **Git integration.** It has Web API actions (`ConnectToGit`, `CommitToGit`, `PullChangesFromGit`). GitHub support is in public preview, and the integration is for development environments only. Source: [Git integration API](https://learn.microsoft.com/power-platform/alm/git-integration/git-api). verified: 2026-10-21
- **Import.** These flags exist: `--stage-and-upgrade`, `--skip-lower-version`, `--settings-file`, `--force-overwrite` and `--async`. No import dry run is documented. `StageSolution` writes a staging row, so it is not a read-only check. Sources: [pac solution](https://learn.microsoft.com/power-platform/developer/cli/reference/solution), [async solution operations](https://learn.microsoft.com/power-platform/alm/solution-async). verified: 2026-10-21
- **Reads for drift.** Managed solutions can't be exported. `msdyn_componentlayer` has `msdyn_order`, `msdyn_solutionname` and `msdyn_componentid`, but no "unmanaged" column, and its required filters are not documented. The `solutioncomponent`, `environmentvariablevalue` and `connectionreference` columns are documented. `pac env fetch` documents only `--environment`, `--xml` and `--xmlFile`, and no output format, so its text is not parsed (decision 15). The Dataverse Web API documents FetchXML in `?fetchXml=`: JSON with raw values, no property for a null column, 5,000 rows per page with a `morerecords` annotation, and an `error` body. A token for the environment URL comes from `az account get-access-token --resource`. Not documented: whether the Web API can read the `msdyn_componentlayer` virtual table and which filter it needs, `az` with Dataverse specifically, and the fields of `pac solution list --json`. M8's capture step settles these. No built-in environment compare is documented. Sources: [Use FetchXml to retrieve data](https://learn.microsoft.com/power-apps/developer/data-platform/fetchxml/retrieve-data), [Page results using FetchXml](https://learn.microsoft.com/power-apps/developer/data-platform/fetchxml/page-results), [az account](https://learn.microsoft.com/cli/azure/account), [Export solutions](https://learn.microsoft.com/power-apps/maker/data-platform/export-solutions), [msdyn_componentlayer](https://learn.microsoft.com/power-apps/developer/data-platform/reference/entities/msdyn_componentlayer), [solutioncomponent](https://learn.microsoft.com/power-apps/developer/data-platform/reference/entities/solutioncomponent), [environmentvariablevalue](https://learn.microsoft.com/power-apps/developer/data-platform/reference/entities/environmentvariablevalue), [connectionreference](https://learn.microsoft.com/power-apps/developer/data-platform/reference/entities/connectionreference), [pac env](https://learn.microsoft.com/power-platform/developer/cli/reference/env). verified: 2026-10-21
- **Unmanaged developer environments.** No new ones from 2026-11-15, suspended from 2026-12-15, deleted from 2027-01-15. Source: [Important changes coming](https://learn.microsoft.com/power-platform/important-changes-coming). verified: 2026-10-21

### Build order, Phase 3 (continues the list below)

22. `test:` scrubbed captures of the read-only `pac` reads in Dev and Test, in `tests/fixtures/pac/` with NOTICE.md entries (the user runs the commands)
23. `feat(core):` environment snapshot model and the parsers for those outputs
24. `feat(cli):` `drift` (live reads, `--save`, offline snapshots, settings drift, text/JSON/Markdown, schema) → **v0.9.0**
25. `feat(core):` deploy plan: dependency order, actions, impact and layer findings, settings checks, plan hash
26. `feat(cli):` `deploy plan` with text/JSON/Markdown output, schema and goldens
27. `feat(action):` one updated PR comment (the plan, or the offline `diff`), with the job-summary fallback on forks → **v0.10.0**
28. `feat(cli):` `deploy apply` (re-plan and hash check, ordered imports, flow states, schema)
29. `docs:` Dev→Test promotion workflow example with OIDC, plus the README → **v0.11.0**
30. `docs:` ephemeral-environment recipe and its sweep, built as docs and not verified (no manual run; the README lists what is not verified)

## Phase 3b: works with Microsoft's skills and MCP servers (M12–M13)

PowerOps is the check-and-plan step between a Microsoft skill that builds or exports a solution and the import ([FOUNDATION §2](FOUNDATION.md#2-positioning), decision 16). Phase 3b is done when **an agent with Microsoft's skills installed builds with theirs, and checks, plans and imports with PowerOps, with one tool per write**. Each milestone is planned with `powerops-plan` and signed off before it is built.

The basis is a read-only review on 2026-10-24 of a local workspace with Microsoft's skills and MCP servers installed (observations only, no tenant data and nothing run against a tenant), plus the repos and Learn pages cited in FOUNDATION §2. Skill and file names are from power-platform-skills `aed70bf` and Dataverse-skills `1966192`; each milestone's plan rechecks them.

**Conflict checks** (each milestone's plan rechecks them):
- **One writer per flow.** Microsoft's skills and MCP tools can import with no plan or hash check: pac's MCP import tool, model-apps `solution-settings.js import`, power-pages `import-solution` (through the Web API) and `deploy-pipeline`, and Dataverse-skills' `dv-solution`. In a flow that PowerOps gates, the only import is `powerops deploy apply` with the reviewed plan. One tool writes the deployment settings file (`powerops settings generate`, or `solution-settings.js template`), and `powerops settings validate` checks it either way.
- **`pac` versions.** An agent's `pac` can be older than any PowerOps was tested on (decision 16(d), FOUNDATION §8).
- **Sign-in state.** pac's MCP server uses the same `pac` auth profiles, and its tools can select another environment or profile mid-session. `drift`, `deploy plan` and `deploy apply` pass the environment on each `pac` call, so a changed selection doesn't move them. `settings apply` and `plan facts` read pac's current environment, and `settings apply` refuses when it no longer matches `--target`. A switched auth profile changes the identity of every `pac` call, and the `az` account that `drift` uses can differ from it (README).
- **Unpack into XML only.** `dv-solution` unpacks with `pac solution unpack` and doesn't mention the YAML layout. Pointed at a YAML-layout folder with `--allowWrite --allowDelete`, `unpack` added XML files and deleted others (FOUNDATION §8).

| Milestone | Release | Scope | Done when |
|---|---|---|---|
| **M12. Hand-off to Microsoft's skills** | v0.12.0 | <ul><li>Built 2026-10-24 ([plan](plans/m12-handoff/plan.md)): [the hand-off page](HANDOFF.md), the install note in the README, `HandoffDocTests` and smoke case 4. The manual run was not run (see the done-when)</li><li>Done 2026-10-24, before the milestone plan: `powerops-implement` step 3 names the Microsoft skill for each kind of build (`canvas-app` in canvas-apps, `app-builder` or `genpage` in model-apps, `build-flow` in power-automate, `create-site` in power-pages, `create-code-app` in code-apps), and falls back to a routing skill or the plugin's `SKILL.md` in a checkout of microsoft/power-platform-skills. Step 7 lists Microsoft's import paths that have no plan check (the list above) and says to promote with `powerops deploy plan` and `deploy apply`. The `powerops-plan` description says to run it before any Microsoft build skill. `SkillsCommandTests` pin the wording. The descriptions name the hand-off and claim none of Microsoft's build or deploy triggers for themselves</li><li>A hand-off page in `docs/`, linked from the README:<ol><li>`powerops-plan`, review, sign-off</li><li>Build with the Microsoft skill for the product, in Dev only</li><li>Export, then unpack only into an XML folder, never into a YAML-layout folder</li><li>`powerops analyze`</li><li>`powerops diff` of the old and new solution for the PR</li><li>Settings: `powerops settings generate` or `solution-settings.js template`, filled in, then `powerops settings validate`; remove an entry instead of leaving `"Value": ""`</li><li>`powerops deploy plan` in CI, reviewed in the PR</li><li>After approval, `powerops deploy apply --apply`, not a skill's or MCP tool's import</li><li>`powerops drift` to confirm</li><li>If a flow is off, Microsoft's `debug-flow` or `manage-flows`</li></ol></li><li>An install note: in a workspace whose own tooling rewrites `.claude/skills/`, run `powerops skills install` in each child project, not at the workspace root</li></ul> | <ul><li>A test checks that every `powerops` command on the hand-off page exists</li><li>A fourth case in `tests/smoke/skills-smoke.sh`, with a routing skill installed beside the PowerOps skills: asking to build a model-driven app in a repo with no plan reaches `powerops-plan`'s fast path and changes no other files, in Claude Code and Codex</li><li>One manual run in Claude Code with Microsoft's skills available: a signed-off plan's build step goes to `app-builder` in Dev, and the import goes through `deploy apply`, not a Microsoft import. Recorded in the verification record. Not run (2026-10-24): see the verification record</li></ul> |
| **M13. Plugin marketplace** | v0.13.0 | <ul><li>`.claude-plugin/marketplace.json` at the repo root, in the format of power-platform-skills (`name`, `owner`, and `plugins` entries with `name` and `source`), with one plugin, `powerops`, whose `.claude-plugin/plugin.json` serves the skills in `skills/`. Then `/plugin marketplace add Chrome71/PowerOps` installs them beside Microsoft's</li><li>Codex: a marketplace file and a `.codex-plugin/plugin.json`, in the format of Dataverse-skills (`.agents/plugins/marketplace.json` with a local `source` path; `name`, `version`, `skills` and `interface` in the plugin file)</li><li>The review skill: `skills install` adds `context: fork`, `background: false` and `model: opus` to the Claude Code copy of `powerops-review-plan` (`SkillsCommand.ForClaude`). A plugin serves its files unchanged, so the Claude plugin needs that copy too</li><li>The skills still need the `powerops` CLI on PATH (`compatibility`), and the plugin descriptions say so. No MCP server in the plugin until Phase 5</li><li>The manifests' `version` is never edited by hand: the M13 plan picks release-please or no version field</li><li>`powerops skills install` stays the default for teams: the skills then live in the repo, so every contributor and CI sees the same gate. The plugin is for a person's own setup and for agents that install plugins</li></ul> | <ul><li>A test parses both manifests: every plugin `source` and `skills` path exists, each skill's `name` equals its folder, and the Claude copy of the review skill equals `ForClaude`'s output</li><li>One manual run in Claude Code (`/plugin marketplace add` with a local path, then install) and one in Codex: the three skills are listed beside Microsoft's. Recorded in the verification record</li></ul> |

### Build order, Phase 3b (continues the list below)

31. `docs(skills):` hand the build to Microsoft's skills: steps 3 and 7 of `powerops-implement` and the `powerops-plan` description, with tests (done 2026-10-24)
32. `docs:` the hand-off page and the install note, with the test of its commands, and the smoke-test case with a routing skill (built 2026-10-24, [plan](plans/m12-handoff/plan.md); the manual run not run, see the verification record) → **v0.12.0**
33. `feat(skills):` plugin marketplace manifests for Claude Code and Codex, with the Claude copy of the review skill → **v0.13.0**

## Testing

| Layer | What | Runs |
|---|---|---|
| Unit | Readers, flow parser, each rule, built from small inline inputs | every build |
| Golden files | Text, JSON, SARIF and Markdown output over the fixtures, compared with `tests/golden/*`. Volatile fields are scrubbed first. | every build |
| SARIF schema | Output validated against SARIF 2.1.0 | every build |
| End-to-end | The packed tool installed into a temp folder and run against the fixtures, checking exit codes and output | CI on Linux |
| `pac` interop | Offline `pac solution unpack` of a fixture zip, then `analyze` on the result | only when `pac` is found |
| Live | Against a real environment | only when `POWEROPS_TEST_ENV` is set; skipped in CI |
| Output contract (M4) | Every JSON output validated against its schema in `docs/schemas/` | every build |
| Planning-skills smoke test (M3) | The skills driven by a simulated user; checks round size, option order, `decisions.md` logging and the stop behaviour | weekly scheduled job on the maintainer's model credentials (CI secrets), not per build; a manual run before each release is allowed |

**Fixtures** come from MIT-licensed Microsoft repos:
- coe-starter-kit: AuditLogs and ALMAcceleratorSampleSolution
- PowerApps-Samples: a managed/unmanaged zip pair and small zips
- PowerApps-Tooling: small `.msapp` files

Each is trimmed and credited in `tests/fixtures/NOTICE.md`. Real hits that are already known become regression tests.

## Live testing runbook

This starts once M1 passes locally.

1. **Local check:**
   1. `dotnet build && dotnet test`
   2. Pack the tool and run it from a temp folder.
   3. Run `powerops analyze` on the fixtures.
2. **`pac` on this machine:**
   1. Put `~/.dotnet/tools` on PATH.
   2. Update `pac` to the current version.
   3. Run `pac auth create --name powerops-test --deviceCode` against the test environment only.
3. **Seed solution (once, in the in-app browser).** You sign in on make.powerapps.com, then create:
   - a publisher (prefix `pops`)
   - a solution
   - one table
   - one environment variable
   - one connection reference
   - a cloud flow that uses them
   - a small canvas app

   Bind the connection and turn the flow on. Claude can guide this and read the page state, but you enter all credentials.
4. **Export and analyze:**
   1. `pac solution export` (unmanaged and managed), then `pac solution unpack`.
   2. Run `powerops analyze` on the zip and on the unpacked folder.
   3. Compare the findings with what you know is in the solution.
5. **Break it on purpose:**
   1. In the browser, hard-code a URL in the flow and add an unused connection reference.
   2. Re-export.
   3. Confirm the new findings appear (and, from M6, that `powerops diff` shows exactly those changes).
6. **Keep the result:**
   1. Scrub the export (fake publisher name, no tenant URLs).
   2. Commit it as a new fixture so the live case becomes a permanent offline test.

**Guardrails:**
- Use a dedicated `powerops-test` profile and environment.
- Never touch a work tenant unless you say so.
- Tenant URLs and IDs live only in environment variables or gitignored files.

## Build order (tasks for `/implement-feature`)

1. `test:` migrate to xunit.v3 + Microsoft.Testing.Platform, add the golden-file helper
2. `chore:` add PowerOps.Core and PowerOps.Core.Tests projects
3. `test:` add licensed fixtures with NOTICE.md
4. `docs:` correct FOUNDATION.md (Solution Checker exclusions, Git integration GA date) and link this plan
5. `feat(core):` normalized solution model + XmlUnpackReader
6. `feat(core):` ZipReader (streamed, zip-slip safe)
7. `feat(core):` cloud-flow parser
8. `feat(analyze):` rule engine + PO001–PO004 (errors)
9. `feat(analyze):` PO005–PO011 (warnings)
10. `feat(cli):` `analyze` command with text/JSON output and exit codes → **v0.2.0**
11. `feat(output):` SARIF writer + schema test
12. `feat(cli):` `baseline` and `--baseline`
13. `feat(output):` Markdown output + `action.yml` + rule docs → **v0.3.0**
14. `feat(plan):` `powerops plan status` and `plan sign-off` (header reader, body hash, exit codes, tenant-pattern scan)
15. `feat(plan):` `powerops plan facts` (`PacRunner`, names only, failure JSON, timeout)
16. `feat(skills):` `powerops skills install` (idempotent, refuses to overwrite differing files without `--force`)
17. `feat(skills):` the three planning skills and the weekly smoke test → **v0.4.0**
18. `feat(output):` `schemaVersion` in every JSON output, schemas in `docs/schemas/` and the schema test
19. `feat(analyze):` a fix line per rule, `fix` and `helpUri` on every finding, and `powerops rules [--json]`
20. `feat(plan):` `depth` and `supersedes` header keys in `plan status`, the skills and PLANNING-SKILLS.md
21. `docs:` AGENTS.md recipes, and dated Sources lines in every rule doc with the test that checks them → **v0.5.0**

## Decisions (2026-10-19)

1. **Release order:** `analyze` (v0.2), then CI/SARIF (v0.3), the planning skills (v0.4), the agent base (v0.5), the YAML layout (v0.6), `diff` (v0.7) and `settings` (v0.8). FOUNDATION lists `analyze` and `diff` together in Phase 1. This order puts `diff` after the YAML layout so it can also cover the formats no other tool reads.
2. **Test stack:** xunit.v3 with Microsoft.Testing.Platform, and plain golden files instead of a snapshot library.
3. **Live environment:** a dedicated developer environment in a tenant where the maintainer has admin rights, used only for PowerOps testing (a sandbox needs spare tenant database capacity).
   - Developer environments that aren't managed are suspended from 2026-12-15. The seed solution is already exported and committed as the `powerops-seed` fixture, so offline tests don't depend on the environment.
4. **Planning skills come before the YAML layout.** They ship to PowerOps users as M3 (v0.4). Every later milestone is planned with them, which dogfoods them early.
5. **Planning-skills smoke test runs weekly, not per build.** The simulated-user smoke test is a scheduled job on the maintainer's model credentials, stored as CI secrets. A manual run before each release is still allowed.
6. **v0.4.0 supports Claude Code and Codex.** Gemini CLI and Copilot ship marked provisional, untested.
7. **No warn-only sign-off.** A sign-off written by hand without `powerops plan sign-off` fails `plan status` with the hint "run `powerops plan sign-off`".
8. **The agent base comes before the YAML layout and `diff` (2026-10-20).** The main users are agents in the user's Claude Code or Codex. Versioning the JSON output and adding fix hints is cheapest before the first NuGet release and before `diff` adds a new output; later rules and commands are then built to the same contract and recipes. Each milestone also rechecks the rule docs' sources with `/verify-research`, because the platform changes fast.

## Decisions, Phase 3 (2026-10-21)

9. **Environments: two environments in the maintainer's own tenant.** Dev is the existing unmanaged developer environment, suspended from 2026-12-15. Test is a new environment that expires 30 days after it was created. The first plan was a managed developer environment for Dev and a sandbox for Test, but the tenant had no Dataverse capacity for a sandbox and no developer environment was left, so the master session changed it on 2026-10-22. Phase 3's live checks must finish before Test expires (about 2026-11-14) and before 2026-12-15.
10. **Order: `drift`, then `deploy plan`, then `deploy apply`, then ephemeral environments.** Drift is read-only and the lowest risk. It also builds the snapshot reader that the plan needs. Ephemeral environments are an opt-in recipe, built as docs and not verified: no live run, by decision of the master session on 2026-10-23, and the tenant has no capacity for a sandbox. PowerOps' own CI never creates environments, because each PR's environment would cost at least 1 GB of database capacity and premium licences.
11. **The GitHub Action that comments `diff` on PRs is folded into M9, not dropped.** M9 needs a PR comment for the plan anyway. Without a target, the same comment shows the offline `diff` of the PR's base and head, which is the Phase 1 item. Until then, the action writes the analysis to the job summary, annotations and code scanning.
12. **`settings diff` is folded into `drift` (M8). The offline diff of two settings files is dropped.** Settings files are meant to differ between environments, and `settings validate` already checks each file against the solution. The useful question is whether an environment's env var values and connection bindings match what was deployed, and only a read of the environment answers it. Drift does that read.
13. **Import directly through `pac`, not through Pipelines.** Pipelines deploys one solution per run, with a fixed import mode. It needs a host environment and can't carry a plan into its gate (see the research basis). `pac pipeline deploy` can be added later as another way to apply a plan.
14. **`deploy apply` runs only a reviewed plan.** It plans again against the target and refuses on any hash mismatch, so what runs is what was reviewed in the PR.
15. **`drift` reads the solution's rows through the Dataverse Web API, with a token from `az`, not through `pac env fetch` (2026-10-21).** Microsoft Learn documents no output format for `pac env fetch`, so its text cannot be parsed against documented behaviour. The Web API's JSON, raw values, errors and paging are documented. `pac org who` still turns `--env` into the environment's name and URL, so `--env` takes what `pac` takes. A token for that URL comes from `az account get-access-token`, run as a subprocess like `pac`. No new package. The costs: the Azure CLI is a prerequisite beside `pac`, and the `az` account can differ from the pac profile. See Q6 of the [M8 plan](plans/m8-drift/plan.md).

## Decisions, Phase 3b (2026-10-24)

16. **Coexistence with Microsoft's skills and MCP servers.** PowerOps is the check-and-plan step between a Microsoft skill's build or export and the import. It doesn't build, and it doesn't replace their tools (FOUNDATION §2).
    - (a) PowerOps keeps calling the `pac` binary as a subprocess with an argument list. It doesn't wrap or call pac's MCP server (`pac copilot mcp --run`, which Learn calls "designed for local development and testing purposes"). The binary's flags and exit codes are what PowerOps is tested against.
    - (b) The Phase 5 MCP server exposes only what `pac`, pac's MCP server and the Dataverse MCP server don't have: `analyze`, `rules`, `baseline`, `diff`, `settings generate` and `validate`, `plan status` and `facts`, `drift` and `deploy plan`. These are read-only, and they return their output instead of writing files, except that `deploy plan` can save the plan file that `deploy apply` reads, behind an explicit flag. It also exposes `deploy apply`, as a dry run unless an explicit flag is set. It leaves out export, import, environment, auth and record tools. Its tool descriptions say to promote a reviewed plan with `deploy apply`, not with an import tool. It needs neither Microsoft server and names none of their tools: the Dataverse server's tools have changed once already (Learn lists `describe_table`, `list_tables` and `fetch` as removed, and `search` renamed to `search_data`; read 2026-10-24).
    - (c) In a flow that PowerOps gates, `deploy apply` is the only import, and it needs a reviewed plan file. Microsoft's import paths (the Phase 3b conflict checks) aren't used for the same solutions in that flow, and `powerops-implement` step 7 names them and asks for a yes before any of them. `settings apply` stays the one-solution command without a plan (M10), for use outside such a flow.
    - (d) `pac` versions: PowerOps is tested with 2.13.1, and the example workflows pin 2.14.1. Microsoft's model-apps asks for `pac` newer than 2.10.0. Older builds are untested (FOUNDATION §8), and the pins stay.
