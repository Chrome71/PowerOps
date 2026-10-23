---
status: implementing
review: done
signed-off: 2026-10-23
signed-off-hash: 29f0dd6494b27e0effbbc1b8602a535a6e3b365cd00532705dfab116236cbe3b
environment: null
depth: quick
supersedes: null
---

# M10: deploy apply from GitHub Actions

## Goal

`powerops deploy apply <plan file> --target <environment> --apply` imports a reviewed M9 plan into the target. It plans again against the target with the same rules. It refuses when the new plan's hash differs from the reviewed one (the target, a zip or a settings file changed since the review) or when the new plan is blocked. Then it imports the zips in plan order through `pac solution import` and reports each cloud flow's state afterwards. Without `--apply` it is a dry run: it re-plans, runs the same checks, says what it would import and writes nothing (Q1). An example workflow in `docs/examples/` promotes a solution from Dev to Test: on a PR it exports the solution as managed from Dev, plans against Test and comments; after the PR is merged, a job in a GitHub environment with required reviewers applies the reviewed plan. Sign-in is GitHub OIDC (`azure/login`, `pac auth create --githubFederated`), with no stored credential. This is the BUILD-PLAN M10 row (release v0.11.0, build-order tasks 28 and 29) and the FOUNDATION Phase 3 done-when.

Done when:

- Offline: the tests under Steps pass with a fake `pac` and a fake target read (no tenant, no network). A blocked plan file, a blocked re-plan, another powerops version and a hash mismatch refuse (exit 1) and run no import. The imports follow the plan order with the right arguments (`--stage-and-upgrade` only for a managed upgrade, `--settings-file` only with a settings file, no import for a skip). The first failure stops the run and the report names the imported solutions. A `--target` whose friendly name differs from the plan's target exits 2. The dry run imports nothing. The JSON goldens validate against `docs/schemas/deploy-apply.schema.json`. A test reads the example workflow and checks what Automation says it must hold. `dotnet build`, `dotnet test` and `dotnet format --verify-no-changes` are clean.
- Live (BUILD-PLAN M10 done-when), in the user's tenant and the test repo `Chrome71/powerops-action-test`, with the example workflow:
  1. A PR that changes the seed gets a plan comment against Test, made from a managed export of Dev.
  2. After the PR is merged and the `test` GitHub environment's reviewer approves, the workflow promotes the seed from Dev to Test, signed in with OIDC only (no client secret anywhere).
  3. The seed's flows are in their expected state in Test afterwards (the apply report says so and the job passes).
  4. `powerops drift --solution PowerOpsSeed --env <Dev> --env <Test>` afterwards finds no drift apart from the managed state (information). Precondition: Test's settings values equal Dev's current values (the M8 live check left them equal); a difference would be named as expected drift.
  5. Negative: a change made in Test between the plan and the apply (the seed flow turned off) makes `deploy apply` refuse with exit 1 and import nothing. The flow is turned back on afterwards.

## Users

- CI jobs that promote a reviewed plan from a GitHub environment with required reviewers.
- Agents in the user's Claude Code or Codex that run the dry run to check that a reviewed plan still applies, and `--apply` only after the user said yes to that environment in the session (the AGENTS.md rule for `settings apply --apply`, extended to `deploy apply`).

## Data

**Inputs.** `<plan file>`: a plan file written by `deploy plan --save` (M9's JSON report). Its folder, resolved with `Path.GetFullPath`, is the deploy folder: the zips and settings files are read from it, as `deploy plan` read them (M9's Action saves the plan file there, so the folder is one artifact). `--target`: an environment as `pac` takes it, as for `deploy plan` (M9 Q1), never printed.

**The plan file** is read and checked before any read of the target. It must be a regular file (not a symbolic link) of at most 1 MB and valid JSON. Then, in this order: `schemaVersion` equal to `JsonOutput.SchemaVersion` and a string `tool` equal to this powerops' version (`powerops --version`), else a refusal (Automation 3); then a 64-character lowercase hex `hash`, a non-empty `target`, a boolean `blocked`, and `inputs` as M9 writes it (per zip: `zip`, `zipSha256`, `settings`, `settingsSha256`, `snapshotSha256`; per dependency: `name`, `version`), else unreadable input (exit 3), with a message naming the field, never its value. Every name taken from the file passes M9's safe-name rule and `TextReport.Clean` before it is printed: the file is an artifact and so untrusted. Only `schemaVersion`, `tool`, `hash`, `target`, `blocked` and `inputs` are used. The order and the actions of the imports are those of the new plan; they equal the reviewed plan's when the hashes match, because the hash covers every input of the plan.

**The re-plan** is `deploy plan`'s code path (M9), unchanged: list the folder (the plan file is ignored, being neither a zip nor a `*.settings.json`), load and hash the zips and settings files, read the target (one `pac org who`, one token, M8's reads), build the plan. M9's `Run` is split so that `plan` and `apply` share one method that does the folder's usage checks, loads the zips and settings files, reads the target and builds the plan; it returns the plan, the loaded `DeployZip`s and the `DeployTarget` (its environment id is needed for the imports), or an exit code. `plan` keeps its behaviour, tests and goldens.

**Imports**, in plan order, for each `install` or `upgrade` (a `skip` imports nothing):

```
pac solution import --environment <environment id> --path <zip> [--settings-file <settings file>] [--stage-and-upgrade]
```

`--settings-file` only when the zip has a settings file. `--stage-and-upgrade` only for an `upgrade` of a managed zip (Learn: an upgrade deletes the components the new version no longer has, which M9's `deletes` items predicted). The environment id is the one `pac org who` printed for `--target` (M9's `Environment ID:` line). The paths are absolute (so no name can be read as an option), passed as list arguments. Just before each import, the zip and its settings file are hashed again and compared with the new plan's `inputs`; a difference stops the run as a failed import (exit 3, nothing more imported). Each import has `settings apply`'s 60-minute timeout. `pac` waits for the import (`--async` is not passed). `SettingsCommand`'s import timeout, environment placeholder, import-argument builder and `Failed` become `internal` and are reused; `settings apply`'s tests stay unchanged.

**Flow states after the imports**: one more target read (M9's `ReadTarget`, the same reads). Each planned flow is paired with its id: M9 builds `PlannedSolution.Flows` from `FlowState.CloudFlows(zip.Solution)` in that order, for install and upgrade only, so the n-th planned flow has the n-th cloud flow's id. Its `stateAfter` is the second snapshot's state for that id, or `missing`. Flow names are never used to match (they need not be unique). Exit 1 when any planned flow's `stateAfter` differs from its `expectedState` (M9's prediction, so a flow that is meant to stay off does not fail the run).

**Report** (text and JSON, `docs/schemas/deploy-apply.schema.json`): `schemaVersion`, `tool`, `applied` (true only with `--apply`), `target` (friendly name), `hash` (the reviewed one), `status`, `message` (why a run stopped, or null), `solutions` in plan order (`order`, `solution`, `zip`, `action`, `import`: `ok`, `failed`, `not-run`, `planned` or `skipped`, and `pacExitCode` when it failed) and `flows` (`solution`, `name`, `expectedState`, `stateAfter`, null in a dry run). No environment id or URL, no connection id, no setting value, no folder path and none of `pac`'s text is in the report. On an import failure `pac`'s stdout and stderr go to powerops' stderr with every line prefixed by `  | `, so no forwarded line can start with `error:`.

| Case | Exit | `status` | `solutions` and `flows` |
|---|---|---|---|
| Refused before the re-plan (blocked file, another `tool` or `schemaVersion`) | 1 | `refused` | empty |
| Refused after the re-plan (hash mismatch, blocked re-plan) | 1 | `refused` | the new plan's solutions, all `not-run`; `flows` empty |
| Dry run | 0 | `planned` | `planned` or `skipped`; `stateAfter` null |
| Only skips with `--apply` | 0 | `imported` | all `skipped`; `flows` empty |
| Imports ok, every flow as expected | 0 | `imported` | `ok` or `skipped`; `stateAfter` set |
| Imports ok, a flow not as expected | 1 | `imported` | as above |
| Imports ok, the second read failed | 3 | `imported` | as above, `stateAfter` null; message "The imports succeeded." first |
| An import failed, timed out, `pac` missing, or a file changed before its import | 3 | `failed` | `ok` before, `failed`, `not-run` after |

## Automation

`powerops deploy apply <plan file> --target <environment> [--apply] [--format text|json] [--out <file>]`

1. **Usage** (before any read): the plan file argument or `--target` is missing; the plan file does not exist; `--out` names a folder: exit 2. A plan file that fails the field checks under Data exits 3.
2. **Blocked plan file**: `blocked: true` exits 1, status `refused`, "a blocked plan is never applied", before any read of the target.
3. **Version**: `schemaVersion` or `tool` differs from this powerops': exit 1, status `refused`, "the plan was made by powerops <x>; this is <y>; plan again with this version", before any read of the target. `tool` tells releases apart only: builds between two releases report the same version, which is why the example pins the action to one commit for both jobs.
4. **Re-plan** (M9's code path). A failure there exits as `deploy plan` does (2 or 3), with nothing imported.
5. **Target check**: the friendly name `pac org who` printed for `--target` differs from the plan's `target`: exit 2, "the plan was made for <plan target>, --target is <name>; nothing was imported". This is the BUILD-PLAN row's `--target` check. A renamed target environment also exits 2 here; the user plans again.
6. **Hash check**: the new plan's hash differs from the file's: exit 1, status `refused`. The message names the first input that differs, comparing the new plan's `inputs` with the file's in this order: a zip added, removed or changed, a settings file added, removed or changed, the target's state of a solution (`snapshotSha256`), a dependency's version; when every input is equal, "the target environment, or the plan file's hash, differs". Then "nothing was imported. Plan again and review the new plan." The new hash is not printed.
7. **Blocked re-plan**: the new plan is blocked (equal inputs, other rules): exit 1, status `refused`, "the plan is blocked now; nothing was imported. Plan again."
8. **Dry run** (no `--apply`): every `install` and `upgrade` is `planned` (a `skip` is `skipped`), with the `pac` arguments shown, the environment id replaced by `<environment id>` and the paths by file names (as `settings apply` does). Exit 0.
9. **Imports** (`--apply`), in plan order. Before the first import the text report prints the target line, as `settings apply` does, and one line to stdout before and after each import (`importing <order> <solution> (<zip>)`, `imported <solution>`, names only), so a log shows the solution in flight if the job stops. A failure stops the run as the table under Data says. The message says there is no rollback and names the imported solutions; for a timeout it reuses `settings apply`'s text (the import may still be running; check the environment's import history before planning again).
10. **Flows**: the second target read, then the exit code as under Data.

Exit codes: `0` applied (or a dry run that would apply) with every planned flow as expected; `1` refused (blocked plan file or re-plan, another powerops version, hash mismatch) or a flow not as expected; `2` usage error or a `--target` mismatch; `3` an unreadable plan file, zip or settings file, a failed `pac` or `az` call or Web API read, a file changed before its import, or a failed import.

Code: the plan-file reader (`DeployPlanFile.Read`, with the checks under Data), the apply result records, the input comparison and the import list in Core (`src/PowerOps.Core/Deploy/DeployApply.cs`, `DeployPlanFile.cs`), the writers in `src/PowerOps.Core/Output/DeployApplyReport.cs`, and the command as a second subcommand `apply` in `src/PowerOps.Cli/DeployCommand.cs`. `DeployCommand.Create` also takes the `pac` runner, as `SettingsCommand.Create` does. The imports go through that runner (tests pass a fake); the re-plan and the flow read go through M9's `deployTarget` seam (tests pass a fake that can return a changed target on the second call). `settings apply`'s `ApplyResult` and `SettingsApplyReport` are not reused: their shape is one solution's steps.

**The GitHub Action** gains two inputs: `plan-file` and `apply` (default `'false'`). With `plan-file` set, a step "Deploy apply" (id `apply`) runs `action/apply.sh` instead of "Deploy plan": `deploy apply "$PO_PLAN_FILE" --target "$PO_TARGET"`, plus `--apply` only when `apply` is `'true'`; without it the step is the dry run. The script filters stderr to `error:` lines as M9's `plan.sh` does (the forwarded text only when `RUNNER_DEBUG` is 1; forwarded lines start with `  | `, so the filter cannot pass them), prints the text report to the log and the job summary in a code fence longer than any backtick run in it, writes the exit code to a step output, and exits 0. With `plan-file` set and `target` empty, or on a fork PR or `pull_request_target`, the step fails with an `::error::` that says why, instead of skipping. "Apply plan result" fails the job on a non-zero exit of either step, with one message per case: for apply, 1 "refused, or a flow is not in its expected state", 2 "usage error or --target mismatch", 3 "failed; there is no rollback, see the report". The analysis steps run before "Deploy apply" with the inputs given; the example's apply job sets `fail-on: none`, because the PR job gated the same zip bytes (the hash check proves they are the same).

**The example workflow**, `docs/examples/promote-dev-to-test.yml`, for one solution, on `pull_request` (`opened`, `synchronize`, `reopened`, `closed`). All jobs run on `ubuntu-latest` (plan and apply on the same OS, Assumptions) and use the same action ref, pinned to one commit. `pac` is installed with one pinned `--version`, set once in the workflow's `env`. Event values reach `run:` text only through `env:`.

- Job `plan` (not `closed`, and from this repository; `concurrency` per PR, cancelling older runs): `environment: test-plan` (no reviewers), `permissions: contents: read, id-token: write, pull-requests: write`. Steps: mask every form of the Dev and Test URLs (`::add-mask::`, as in M9's live check); install `pac`; `azure/login` with OIDC (`client-id`, `tenant-id`, `allow-no-subscriptions: true`); `pac auth create --githubFederated --tenant --applicationId --environment <Dev URL>` with its output kept out of the log (M9); `pac solution export --managed --name <solution> --path "$RUNNER_TEMP/deploy/<solution>_managed.zip" --environment <Dev URL> --overwrite`; the Test settings file built by `docs/examples/powerops/build-settings.sh` (Q2: one secret per value, `jq` with a fixed filter, never printed); the action with `path: ${{ runner.temp }}/deploy` (the analysis gates the exact zip that will be imported), `upload-sarif: 'false'`, `comment: 'true'`, `deploy-folder: ${{ runner.temp }}/deploy` and `target: <Test URL>`; for a public repository, the zip encrypted (`openssl enc -aes-256-cbc -pbkdf2 -salt -pass env:POWEROPS_ZIP_KEY`, the key an environment secret in both `test-plan` and `test`); `actions/upload-artifact` of only the zip (encrypted in a public repository) and `powerops-plan.json`, never the settings file, as `powerops-plan` with `overwrite: true` (a re-run replaces it) and `retention-days: 30` (at least as long as an approval may wait).
- Job `find` (`closed` and merged, from this repository; no environment): `permissions: actions: read`. It lists the unexpired artifacts named `powerops-plan` (`gh api repos/<repo>/actions/artifacts?name=powerops-plan`), keeps the newest whose run's head commit is the PR's head commit and whose head repository is this repository, downloads it, and writes the run id, the artifact id and the plan file's `hash` to its job summary, so the approver can compare them with the PR comment before approving. No artifact: the job fails and says to re-run the PR's last plan run, then re-run this run. Its output is the artifact id.
- Job `apply` (`needs: find`; one `concurrency` group for the whole repository, not cancelling, so two applies never run at once; `timeout-minutes: 90`): `environment: test` (required reviewers, so it waits for an approval), `permissions: contents: read, id-token: write, actions: read`. Steps: mask the URL forms; download the artifact by the id `find` gave (`gh api repos/<repo>/actions/artifacts/<id>/zip`); decrypt the zip in a public repository; build the settings file with the same script from the `test` environment's secrets (equal values give equal bytes, so the hash holds; Q2); sign in with `--environment <Test URL>`; the action with `path: ${{ runner.temp }}/deploy`, `upload-sarif: 'false'`, `fail-on: none`, `plan-file: ${{ runner.temp }}/deploy/powerops-plan.json`, `apply: 'true'` and `target: <Test URL>`.
- Fork PRs skip every job. Every action is pinned by a full commit SHA. All URLs, ids and values come from environment secrets (`DEV_URL`, `TEST_URL`, `AZURE_TENANT_ID`, `AZURE_CLIENT_ID`, `TEST_CONNECTION_ID`, `TEST_ENVVAR_<NAME>`, `POWEROPS_ZIP_KEY`); the docs show them with fake values.
- `docs/examples/README.md` explains the setup and the risks:
  - A private repository is recommended. The artifact holds Dev's managed export: canvas apps cache the source environment's URL in their `.msapp`, and flow definitions can hold hard-coded values (PO006). In a public repository anyone signed in to GitHub can download an artifact while it is kept, so the example encrypts the zip; the key is a secret in both GitHub environments. Use `fail-on: warning` (with a baseline for accepted findings) to stop hard-coded values (PO006) and current values (PO008) from being promoted.
  - Two app registrations: a plan app (federated credential for `test-plan` only; application user in Dev with the role an export needs and in Test with a role that reads), and an apply app (federated credential for `test` only; application user in Test with the role an import needs). A `pull_request` run uses the workflow file from the PR, so anyone who can push a branch can use the `test-plan` identity and read its secrets: they must be configuration (URLs, connection ids, values, the zip key), never credentials. Protect `.github/workflows/` with CODEOWNERS or a ruleset, turn on "Prevent self-review" on `test`, and make the plan job a required status check (otherwise a PR can be merged with no plan for its head commit, and `find` fails). The live check uses one app for both, in the owner-only test repository; the docs say so.
  - The immutable OIDC subject form and how to read it from a repository (`gh api repos/<owner>/<repo>/actions/oidc/customization/sub`); the application users (`pac admin assign-user --application-user`); the two GitHub environments with one secret per value; required reviewers on public repositories on GitHub Free.
  - What can make an apply refuse though nothing was meant to change: a secret that differs between `test-plan` and `test` or was rotated after the plan; the settings script or the action ref changed in the PR after the plan; a platform update of a dependency solution in Test (its version is in the hash); a change made in Test. The way back is to plan again: re-run the PR's last plan run, then re-run the apply.
  - The target's DLP policy must allow the solution's connectors together, and Managed Environments need premium licences for the people who run the flows.
  - The window between the re-plan and each import, and that there is no rollback.

Out of scope (not asked for): a rollback, retrying a failed import, `--force-overwrite` (the plan reports unmanaged layers; overwriting them is the user's call outside powerops), Pipelines (BUILD-PLAN decision 13), `--async` imports, holding solutions, a Prod stage in the example, and fork PRs.

## Screens

None. Command-line only. Text output: the target line, the hash, one line per solution (`<order> <solution> (<zip>): <action>, <import status>`), one per flow (`flow <solution>/<name>: expected <state>, now <state>`), and a summary line (`applied`, `dry run: would import <n> solutions`, `refused: ...` or `failed: ...`).

## Environments and route

- Dev is the source and Test the target. Decision 9's setup was not possible in the user's tenant (no Dataverse capacity for a sandbox, no developer environment left), so the master session changed it on 2026-10-22: Dev is the existing unmanaged developer environment (suspended from 2026-12-15) and Test a new environment that expires 30 days after it was created; the live checks finish by about 2026-11-14. The example and its docs assume no environment type: Test can be any non-default environment with Dataverse. The seed is unmanaged in Dev and managed in Test. The plan names neither; ids and URLs live in the test repo's environment secrets, the user's shell or gitignored files.
- Auth for the live check: the Entra app from M9's live setup, with GitHub federated credentials for the test repo's `test-plan` and `test` environments (the repository's immutable subject form), added as an application user in Test (M9) and in Dev (this plan, for the export) with System Administrator (Learn's GitHub Actions tutorial uses that role; the minimum roles are an admin question). One app serves both jobs because the test repo is public but only its owner can push; this is test-only, and the docs teach two apps (R1). No client secret is created. The imported components are owned by the application user (Learn).
- GitHub: the test repo is public, so the `test` environment's required reviewers are available on GitHub Free; the reviewer is the repository owner. The `test-plan` and `test` environments hold the same secrets, one per value, set in M9's live setup; this plan adds `DEV_URL` and `POWEROPS_ZIP_KEY` to both. The key is made locally with `openssl rand` and piped into `gh secret set`, never printed. The zip is encrypted in the test repo because it is public (the master session, 2026-10-23).
- Agents run `deploy apply --apply` only against an environment the user said yes to in the session.
- Writes in the user's tenant for the live check, with the master session's go: the Dev application user (one-time setup), raising the seed's version in Dev so the export is an upgrade (`pac solution online-version`), the imports into Test by the workflow, and turning the seed flow off and on again in Test for the negative item.
- Route of the code: the normal PR to main and the v0.11.0 release. The branch is pushed after a leak scan when the live check needs it (the master session's standing permission).

## Solution and publisher

None created. The live check promotes the existing `PowerOpsSeed` (prefix `pops`) from Dev to Test.

## Constraints

- The only writes to an environment are the `pac solution import` calls of `--apply`. The dry run and the reads are M9's. No rollback.
- No new package. The example workflow uses `actions/setup-dotnet`, `actions/upload-artifact` (MIT) and `azure/login` (MIT), each pinned by a full commit SHA, plus `gh`, `jq` and `openssl` from the runner.
- Tests never call a real `pac` or `az`, the network or GitHub.
- Fixtures and goldens hold fake values only; the hash and `tool` in goldens are scrubbed as in M9.
- The plan file, the report, the job summary, the comment and the logs hold names, states, versions and hashes only, never a setting's value, a connection id, an environment id or URL (Q2). The artifact's zip is Dev's export: private repository recommended, encrypted in a public one.
- Licensing: the seed's flow runs in Test; with the application user as its owner, Learn says premium connectors need a Process or per-flow licence (the seed uses a standard connector; admin question). GitHub-hosted runners are free for the public test repo.
- Sources, checked on 2026-10-22 by powerops-researcher (Sonnet) and this session on Microsoft Learn, docs.github.com and open repos only (no tenant contacted):
  - "pac solution" (learn.microsoft.com/power-platform/developer/cli/reference/solution): `import` takes `--path`, `--environment`, `--settings-file`, `--stage-and-upgrade`, `--async` and `--max-async-wait-time` (60 minutes by default); `export` takes `--name`, `--path`, `--managed`, `--environment` and `--overwrite`; `online-version` gets or sets a solution's version. Exit codes are not documented.
  - "Upgrade or update a solution" (learn.microsoft.com/power-apps/maker/data-platform/update-solutions): an upgrade deletes components the new version no longer has.
  - "Import a solution" (learn.microsoft.com/power-automate/import-flow-solution): an update to an existing flow does not change its state; the importing user owns the imported components; flows are turned off and on during an import; connections must be shared with the importing user for it to turn flows on.
  - "pac auth" (learn.microsoft.com/power-platform/developer/cli/reference/auth): `create --githubFederated` with `--tenant`, `--applicationId` and `--environment`.
  - "Workload identity federation: GitHub immutable subjects" (learn.microsoft.com/entra/workload-id/workload-identities-github-immutable-subjects): repositories created after 2026-07-15 present `repo:<owner>@<owner id>/<repo>@<repo id>:...`.
  - "pac admin" (learn.microsoft.com/power-platform/developer/cli/reference/admin): `assign-user` with `--application-user` and `--role`.
  - "Deployments and environments" (docs.github.com/en/actions/reference/workflows-and-actions/deployments-and-environments): required reviewers, "Prevent self-review", environment secrets reach a job only after approval; available for public repositories on Free.
  - "Store and share data with workflow artifacts" and the REST API for artifacts (docs.github.com): artifacts of another run are listed and downloaded with `actions: read`; `upload-artifact`'s `overwrite` and `retention-days` inputs (actions/upload-artifact README, MIT).
  - "Events that trigger workflows" (docs.github.com): `pull_request` `closed` with `merged` true gives the head SHA; fork runs get no secrets.

## Assumptions

- `pac solution import` exits non-zero when an import fails (observed in M7's live check; not documented).
- `pac solution import` without `--async` waits until the import ends (Learn implies it; observed in M7). For a large solution the request can time out while the import goes on; the report then says so, and the next apply refuses on the hash until the user checks the import history and plans again.
- `--stage-and-upgrade` is used only when the solution is installed managed (M9's `upgrade` of a managed zip); its behaviour on a fresh install is not documented and never used.
- An upgrade with a settings file keeps the seed's flow on (Learn: an update doesn't change a flow's state); the live check records it.
- The application user can use the connection behind `TEST_CONNECTION_ID` that the settings file binds (the user made it; Learn says connections must be shared with the importing user to turn flows on). Not checked; if the live import fails on it or leaves the flow off, the live check stops and a new decision is taken (share the connection, or another binding).
- The application user can export from Dev and import into Test (Learn shows application users with security roles; not stated for export).
- One `pac auth create --githubFederated` profile made for Dev also reads and imports into Test with `--environment` (M9's live check read Test with a profile made for Test; the M8 reads use one user profile for both); the live check records it.
- Two reads of an unchanged Test give the same snapshot text (M9's live check: equal hashes from two local runs and from CI under the app's identity).
- `PacRunner` does not set the output encoding, so a friendly name with non-ASCII characters may decode differently on Windows than on Linux; plan and apply run on the same OS (the example uses `ubuntu-latest` for every job; the README says so).
- `openssl enc` on the runner decrypts to the exact bytes it encrypted, so the zip's hash holds.
- Environment facts were not available: `powerops plan facts` was not run during planning.

## Open questions

## Questions for your admin

- Does the application user, as the owner of the imported flow, need a Process or per-flow licence in Test for the seed's connector?
- Which security roles do the export from Dev, the reads of Test and the import into Test need at least? The live check uses System Administrator in the user's test tenant.
- Does the Test environment's DLP policy allow the seed's connectors together?

## Steps

1. `feat(core):` `DeployPlanFile.Read` (the checks under Data, in their order), the apply result records and statuses, the comparison of two plans' `inputs` that names what changed, the import list (zip, settings file, `--stage-and-upgrade`) and the pairing of planned flows with their ids, and `DeployApplyReport` (text, JSON), test-first in `tests/PowerOps.Core.Tests/Deploy/` and `tests/PowerOps.Core.Tests/Output/`. Tests include two flows with the same name in one solution, a hand-built blocked `DeployPlanResult`, every row of the table under Data, and names with a line break and `::error::` from the plan file.
2. `feat(cli):` `deploy apply` in `DeployCommand.cs` (build-order task 28), with `docs/schemas/deploy-apply.schema.json`, its row in `docs/schemas/README.md`, the `Map` entry and goldens `deploy-apply-dry-run.json`, `deploy-apply-dry-run.txt`, `deploy-apply.json`, `deploy-apply.txt`, `deploy-apply-refused.json` (a hash mismatch) and `deploy-apply-failed.json`. CLI tests through `Program.Run` with a fake `pac` and a fake `deployTarget`:
   - every usage and exit path under Automation, and every row of the table under Data;
   - a blocked plan file, another `tool`, another `schemaVersion`, a blocked re-plan and a hash mismatch import nothing (the fake `pac` records no import);
   - a changed zip byte, settings byte or target snapshot between plan and apply refuses, and the message names the changed input;
   - a file changed between the re-plan and its import (the fake `pac` changes the next file on the first import) stops the run with exit 3;
   - the imports in plan order with the exact argument lists, absolute paths, and a zip whose name starts with `-`; a skip imports nothing;
   - the first failure stops the run, later solutions are `not-run`, `pac`'s output is on stderr only, every line prefixed;
   - the timeout, `pac` missing, and the second read failing after good imports;
   - the dry run imports nothing, exits 0 and shows file names, not the folder path;
   - the flows come from the second read by id; exit 1 when one is not as expected, 0 when a flow expected off is off;
   - a plan file given as a bare file name in the current folder;
   - the environment id and the `--target` value never appear in stdout, the report or the JSON;
   - sentinel values for a connection id and a variable value in the settings file appear in no output of `deploy apply` (the master session's rule, Q2);
   - `deploy plan`'s and `settings apply`'s tests and goldens are unchanged.
3. `feat(action):` the `plan-file` and `apply` inputs and `action/apply.sh`, with tests in `ActionScriptTests` like M9's (exit codes, the filtered stderr with a prefixed forwarded line, the report in the log and the summary, the dry-run and `--apply` argument lists, the per-case messages, the error for `plan-file` without `target`). `docs:` `docs/examples/promote-dev-to-test.yml`, `docs/examples/powerops/build-settings.sh` and `docs/examples/README.md` (the outline under Automation, fake values only), with a test that reads the example workflow and checks that both jobs that build a settings file call the same script, every job uses the same PowerOps action ref and `pac` version, every action is pinned by a 40-character SHA, and no `run:` text holds a `${{ github.event` expression. The README (`deploy apply` usage and a paragraph, a link to the example), AGENTS.md (the `--apply` rule names `deploy apply`), and `docs/BUILD-PLAN.md`: the M10 row built, with the changes from the row (`--apply` and the dry run; the target check by friendly name; the `tool` and blocked refusals; the plan file's folder; the flows checked against their expected state; the `apply` input; the `find` job; the encrypted artifact in public repositories), and the Phase 3 environments paragraph and decision 9 as the master session changed them on 2026-10-22.
4. Offline checks and a leak scan, as in M9.
5. Live check, after M9's live items passed (2026-10-23) and with the master session's go for the tenant writes: add the application user in Dev; add `DEV_URL` and `POWEROPS_ZIP_KEY` to both GitHub environments; close the M9 test PRs and replace M9's `powerops-plan.yml` with the example workflow (both post the same marker comment), pointing at `Chrome71/PowerOps@<this branch>`; raise the seed's version in Dev by one; then run the five items under Goal in order, the negative item on a second PR after raising Dev's version again. Unzip the exported seed once and record whether it carries a current value or a URL. Read the run logs for identifiers in every form, as in M9. Record the results in the report to the master session with no tenant data.
