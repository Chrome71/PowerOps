# PowerOps build plan: v0.2 to v0.8

This plan turns [FOUNDATION.md](FOUNDATION.md) Phases 1–2 (including the planning skills, Phase 1b) into ordered, testable milestones. Last updated 2026-10-20.

## Where PowerOps fits

Teams that use Dataverse Git integration, or an export → unpack → commit workflow, **already have the unpacked solution in Git**. A pull-request check therefore needs no export, no sign-in and no secrets: just `checkout` plus one PowerOps step.

Today nothing fills that slot well:

- **Solution Checker** runs as an online service, and you can't add your own rules to it.
- **Pipelines** checks only for missing dependencies.
- **`pac solution create-settings`** generates a deployment settings file but never checks it.
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
- **Read-only and offline.** It never changes the solution and makes no network calls. The exceptions go through `pac`: `powerops plan facts` (M3) reads and prints names only, and `powerops settings apply` (M7) reads the target and its flow states and imports a solution only with `--apply --target <name>`.

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
