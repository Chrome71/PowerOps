---
status: done
review: done
signed-off: 2026-10-23
signed-off-hash: a3e41bfad6fd209414a104f5ac9e886423d33e8660f26ae44069c3bf46624e40
environment: null
depth: quick
supersedes: null
---

# M11: ephemeral environment per PR (opt-in recipe)

## Goal

An opt-in GitHub Actions recipe that gives a pull request its own short-lived Power Platform environment: it creates the environment, turns Managed Environments on, deploys the PR's managed solution zips into it with `powerops deploy plan` and `deploy apply --apply` (M9, M10), and deletes the environment in an `if: always()` step. A scheduled sweep deletes the recipe's leftovers: environments of this repository whose creating run has finished. It is documentation only: `docs/examples/ephemeral-pr.yml`, `docs/examples/ephemeral-sweep.yml`, two scripts under `docs/examples/powerops/`, and a section in `docs/examples/README.md`. PowerOps' own CI never creates environments (BUILD-PLAN decision 10). This is the BUILD-PLAN M11 row (no release; build-order task 30).

The recipe works only for solutions without connection references: a new environment has no connections, so no settings file can bind one, and M9 blocks a plan with an unbound reference (a `settings` item), which M10 refuses to apply. An environment variable with no default needs a value in a settings file built from repository variables. Binding connections in a new environment would be its own `powerops` task with a test and a live check; the docs do not promise it.

Done when:

- Offline: `tests/PowerOps.Cli.Tests/EphemeralScriptTests.cs` runs the two scripts with stub `pac` and `gh` first on `PATH` (skipped on Windows and where `bash` or `jq` is missing, as `ActionScriptTests`) and checks every case under Steps; `tests/PowerOps.Cli.Tests/ExampleWorkflowTests.cs` (M10's) reads both workflows with YamlDotNet and checks the items under Steps. `dotnet build`, `dotnet test` and `dotnet format --verify-no-changes` are clean.
- No live run. The master session decided on 2026-10-23 that no environment is created or deleted for M11, and the user's tenant has no Dataverse capacity for a sandbox (the reason decision 9 changed). The BUILD-PLAN M11 row is marked "built as docs, not verified", each of its live done-when items "not run" with this reason; decision 10 and build-order task 30 are edited to say the same; FOUNDATION section 8's open question on a service principal signing in to an environment it just created stays open, with a pointer here. The docs carry a "Not verified" table and the first comment line of both workflows says "not run end to end".

## Users

- Teams that want each PR's solutions imported into a clean environment before review, in a tenant (or region) without production environments, and can pay for it: database capacity for every environment that exists at once, premium licences in a managed environment, and an admin-level service principal.

## Data

The recipe takes managed solution zips from one folder of the repository (`deploy/` in the example, built by the user's own build, for example `pac solution pack --packagetype Managed` from an unpack made with `--packagetype Both`), with each zip's optional `<zip name>.settings.json` built from repository variables at run time (M9's layout). Managed, because that is what the route ships; M9's managed-over-unmanaged block cannot occur in a fresh environment. A solution with a connection reference blocks the plan (see Goal); the README shows the blocked message.

**Environment name**: `<prefix>-<repository id>-pr<number>-<run id>-<attempt>`, with `github.repository_id` (numeric, unique per repository), the prefix a repository variable (default `powerops-pr`) checked against `\A[a-z][a-z0-9-]{1,19}\z`. Selection is by an anchored full match of the display name, `\A<prefix>-<repository id>-pr([0-9]+)-([0-9]+)-([0-9]+)\z`; any other name, including one an admin gave the prefix by hand, is never touched.

**`pac admin list --json`**: the scripts read `DisplayName`, `EnvironmentId` and `EnvironmentUrl`. These field names and a stdout that is JSON only were observed with pac 2.13.1 on 2026-10-23 in this session's live setup (a read of the user's tenant; decisions.md log); Learn documents neither. The scripts parse with `jq -e`, stop with an error when a field is missing, and require the id to be a GUID and the URL an `https` URL on a documented Dataverse host (M8's list).

## Automation

Two scripts, both `set -euo pipefail`, inputs only from environment variables, no `set -x`, every `pac` and `gh` call's output kept in a file under `$RUNNER_TEMP` and never printed (only `error:` lines, and everything when `RUNNER_DEBUG` is 1, as M9):

- `docs/examples/powerops/ephemeral-env.sh <find|delete>` for the PR workflow. `find`: run `pac admin list --json` up to 5 times, 20 seconds apart, until exactly one environment has the run's exact display name; zero after the retries or two or more: an `::error::`, exit 1. Then validate the id and URL, register `::add-mask::` for the id (lower and upper case), the URL's forms (with and without the scheme and a trailing slash, lower and upper case) and the host's first label, and only then write `id` and `url` to `$GITHUB_OUTPUT`. `delete`: the id comes from `find`'s output when it ran, else from an exact-name lookup as in `find`; a lookup that fails or finds two deletes nothing and fails the job; none found does nothing (the create never got that far). Just before `pac admin delete --environment <id> --async`, the list is read again and must hold exactly one environment with that id and exactly the expected name.
- `docs/examples/powerops/ephemeral-sweep.sh` for the sweep. From one `pac admin list --json`, keep the names that match this repository's anchored pattern. For each, `gh run view <run id> --repo "$GH_REPO" --json status,conclusion`: delete only when the call succeeds and the run is `completed` (a finished run should have deleted its environment, so this one is a leftover, whatever the PR state); a run in progress is kept; a `gh` failure keeps the environment and marks the sweep failed. It deletes only when `POWEROPS_SWEEP_DELETE` is `true`; otherwise it prints the names it would delete (a dry run, the default). It prints counts and the recipe's own matching names only, never the list. It exits 1 (the run turns red, so GitHub notifies) when any matching environment could not be classified. With `POWEROPS_SWEEP_PR` set it handles only that PR's environments (used by the PR workflow's first step).

`docs/examples/ephemeral-pr.yml`, on `pull_request` (`opened`, `synchronize`, `reopened`) with a `paths:` filter on the deploy folder, one job:

- Job guard (a job `if`, not `run:` text): the PR is from this repository and the actor is not `dependabot[bot]` (both get no secrets). `environment: ephemeral` (required reviewers and "Prevent self-review", so each run waits for an approval and the federated credential is bound to that environment, not to `pull_request`). `concurrency` per PR, cancelling the older run. `timeout-minutes: 150`. `permissions: id-token: write, contents: read, actions: read`. Every action pinned by a full commit SHA with a version comment, except the PowerOps action, which uses the same ref as M10's example with the same comment.
- Steps:
  1. Checkout; install `pac` with the pinned version (M10); `pac auth create --githubFederated --tenant --applicationId` with no environment, output kept out of the log.
  2. The PR's leftovers from older finished runs: the sweep script with `POWEROPS_SWEEP_PR` set and `POWEROPS_SWEEP_DELETE` true (bounds the leftovers per PR).
  3. `pac admin create --type <type> --name <name> --region <region> --currency <currency> --language <language>` with `timeout-minutes: 75` on the step, output kept out of the log. Type, region, currency and language from repository variables with defaults in the YAML (`${{ vars.X || '<default>' }}`: `Sandbox`, `unitedstates`, `USD`, `1033`); the type must be `Sandbox` or `Trial`, else the step stops (a production environment is never created by the recipe).
  4. `ephemeral-env.sh find`.
  5. `pac admin set-governance-config --environment <id> --protection-level Standard` (Managed Environments on).
  6. `pac admin assign-user --environment <id> --user <app id> --role "System Administrator" --application-user` (Learn does not say the creating app becomes a user of the new environment; System Administrator in a throwaway environment).
  7. `azure/login` with OIDC; `pac auth create --githubFederated ... --environment <url>`.
  8. The PowerOps action with `path: deploy`, `upload-sarif: 'false'`, `deploy-folder: deploy`, `target: <url>` (the plan, saved in the deploy folder; `comment` off: the plan of an empty environment holds only installs), then again with `path: deploy`, `upload-sarif: 'false'`, `fail-on: none`, `plan-file: deploy/powerops-plan.json`, `apply: 'true'`, `target: <url>` (M10).
  9. Last step, `if: always()`, no `continue-on-error`: sign in again (`pac auth create --githubFederated` with no environment: the first sign-in may be hours old), then `ephemeral-env.sh delete`. `pac admin delete --async` returns at once, so the step fits GitHub's 5-minute cancellation window; the environment is soft-deleted and can be recovered for 7 days (Learn).

A cancel during step 3 ends `pac` while the service goes on creating; the delete step may find nothing yet. Such a leftover is deleted by the next sweep or by the PR's next run (step 2), not by the cancelled run; the docs say so.

`docs/examples/ephemeral-sweep.yml`, on `schedule` (daily) and `workflow_dispatch` (input `delete`, default false), on the default branch only: `permissions: id-token: write, actions: read, contents: read`; `GH_REPO` and `GH_TOKEN` set; a second federated credential for the default branch's subject (protect the default branch); the sweep script with `POWEROPS_SWEEP_DELETE` true only when the dispatch input is true or the repository variable `POWEROPS_SWEEP_DELETE` is `true`. GitHub disables a schedule after 60 days without activity in a public repository; the docs tell the reader to check `pac admin list` for the prefix by hand now and then.

No new `powerops` code. If the recipe needs a change in `powerops`, it becomes its own task with a test (BUILD-PLAN row).

## Screens

None.

## Environments and route

- None touched. No environment is created or deleted for M11 (the master session, 2026-10-23); the user's tenant has no capacity for one.
- Auth in the recipe: a dedicated Entra app used for nothing else, registered as a Power Platform management application by an admin signed in as a user (`pac admin application register --application-id`, Learn); it then acts like a Power Platform administrator in the whole tenant. Two federated credentials: the `ephemeral` GitHub environment's subject (PR runs, after an approval) and the default branch's (the sweep); the docs show how to read the subject form (name-based or immutable) as M10's README does.
- The README section opens with the risk before any YAML: anyone who can run a workflow with that credential can create and delete any environment in the tenant, production included. Recommended: a tenant or region without production environments; the approval gate on `ephemeral`; a private repository; CODEOWNERS or a ruleset on `.github/workflows/`; secrets in the job are configuration, never credentials; a fork PR gets no token and is skipped. A safe first use: a throwaway repository, the sweep as a dry run, one PR.
- Route of the docs: built on top of M10's branch after M10 is merged and rebased onto main; the README's M11 section follows M10's.

## Solution and publisher

None created. The docs use a fake reference-free solution `ContosoTables`.

## Constraints

- No new `powerops` code or package. The workflows call `pac` directly, because `microsoft/powerplatform-actions`' environment actions take only a client secret or a password, not OIDC (checked 2026-10-22).
- Every action except PowerOps pinned by a full 40-character commit SHA with a version comment (the SHAs M10's example uses, looked up with `gh` on 2026-10-22); `${{` never appears in `run:` text (everything through `env:`); fake values only.
- Costs, in the docs: database capacity for every environment that exists at once (running PR runs and leftovers; Learn says recovering a deleted environment may need free capacity, which suggests a deleted one does not hold it, but does not say so); premium licences for every user who runs an app or flow in a managed environment; a management-app registration whose app acts like a Power Platform administrator.
- The target's DLP: a new environment falls under the tenant's DLP policies, which can suspend a flow; `deploy apply` then reports a flow not in its expected state. The recipe runs no tests in the environment; it checks that the zips import and the flows reach their expected state.
- Sources, checked on 2026-10-22 and 2026-10-23 on Microsoft Learn, docs.github.com and open repos only:
  - "pac admin" (learn.microsoft.com/power-platform/developer/cli/reference/admin): `create` (`--type` required, waits by default up to 60 minutes, `--json`), `set-governance-config --protection-level Standard`, `delete` (waits by default, `--async`), `list` (`--name` is a contains match, `--json` with undocumented fields), `assign-user --application-user`, `application register`. Read from the current reference, which names no pac version; not checked against the pinned version.
  - "Create and manage environments" (learn.microsoft.com/power-platform/admin/create-environment): 1 GB of free database capacity for a sandbox. "How do I recover a recently deleted environment?" (learn.microsoft.com/power-platform/admin/recover-environment): soft delete for 7 days; recovery may need free capacity.
  - "Create a service principal application" (learn.microsoft.com/power-platform/admin/powershell-create-service-principal): the management-app registration by a user admin; service principals work for environment management.
  - "Enable Managed Environments" and "Managed Environments licensing" (learn.microsoft.com/power-platform/admin/managed-environment-enable, .../managed-environment-licensing).
  - "Control environment creation" (learn.microsoft.com/power-platform/admin/control-environment-creation): a tenant setting can restrict creation to admins.
  - "Expressions" and "Workflow cancellation" (docs.github.com): `always()` runs after a cancel; the server ends what remains after 5 minutes. "Events that trigger workflows": `schedule` runs on the default branch and is disabled after 60 days without activity in a public repository.
  - microsoft/powerplatform-actions `delete-environment/action.yml`: client secret or user and password only.

## Assumptions

- Not verified (no live run), and listed in the README's "Not verified" table with where each claim comes from: the service principal can create an environment once it is a management application; `pac admin list --json` keeps the observed fields and a JSON-only stdout in the pinned pac version; `pac auth create --githubFederated` with no environment serves the `pac admin` commands; `assign-user --application-user` works for the creating app; the app signs in to the new environment and `az account get-access-token` works for it (FOUNDATION section 8); an import works right after `create` returns; a cancelled run's environment is cleaned up by the sweep; `delete --async` returns within the cancel window; the sweep as a whole.
- A solution without connection references deploys into a new environment with a settings file that holds only environment variable values, or none.
- Environment facts were not available: `powerops plan facts` was not run during planning.

## Open questions

## Questions for your admin

- May a GitHub app be registered as a Power Platform management application (it then acts like a Power Platform administrator), and in which tenant should it run?
- Who may create environments in the tenant (the "Control environment creation" setting), how much capacity is free, and who pays for it?
- Which DLP policies apply to a new environment?
- Does the application user, as the owner of the imported flows, need a Process or per-flow licence (M10's question)?

## Steps

1. `docs:` `docs/examples/ephemeral-pr.yml`, `docs/examples/ephemeral-sweep.yml`, `docs/examples/powerops/ephemeral-env.sh` and `docs/examples/powerops/ephemeral-sweep.sh` (first comment line of each workflow: "not run end to end; see docs/examples/README.md"), and their section in `docs/examples/README.md` (the risk first; the recommended setup; the two federated credentials; the variables and secrets with their defaults in one table; costs; DLP; what the recipe checks and does not; the "Not verified" table; a safe first use); a README line pointing to it; `docs/BUILD-PLAN.md`: the M11 row built as docs and not verified, decision 10 and build-order task 30 edited, FOUNDATION section 8's item kept open with a pointer. `test:` `EphemeralScriptTests` with fixtures of `pac admin list --json` (the observed field names, fake values) and these cases: this repository's leftover of a completed run deleted; a run in progress kept; another repository's id kept; a name for attempt 1 next to one for attempt 10; a name that does not parse kept and not printed; a `gh` failure keeps and exits 1; the dry run deletes nothing and prints the names; `POWEROPS_SWEEP_PR` limits to one PR; `find` with zero, one and two matches and the retry; the masks registered before the outputs are written; `delete` with an id from `find`, by name, none found, and a re-read that disagrees; no output line holds another environment's name, id or URL. `ExampleWorkflowTests` extended: no `${{` in any `run:` text in the three example workflows; the delete step is the last step of the create step's job, `if` holds `always()`, no `continue-on-error`; the job guard covers forks and Dependabot; every action except PowerOps pinned by a 40-character SHA with a comment; the type check allows only `Sandbox` and `Trial`.
2. Offline checks and a leak scan of the diff, both YAML files included, as in M9.
