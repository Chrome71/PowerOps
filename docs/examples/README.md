# Example: promote a solution from Dev to Test

[promote-dev-to-test.yml](promote-dev-to-test.yml) promotes the solution `ContosoApp` from a Dev environment to a Test environment with a plan that is reviewed in the pull request. Read this page first: the workflow moves a solution into an environment, and some of its choices are about who can do that.

How it runs:

1. On a pull request, the `plan` job signs in to Dev with GitHub OIDC (no stored credential), exports `ContosoApp` as managed, builds the Test settings file, runs `powerops deploy plan` against Test, and posts the plan as one PR comment. It keeps the exported zip and the plan file as the artifact `powerops-plan`.
2. After the PR is merged, the `find` job looks up the newest unexpired `powerops-plan` artifact made for the PR's last commit and writes its run id, artifact id and plan hash to the job summary. Compare them with the PR comment.
3. The `apply` job runs in the GitHub environment `test`, which has required reviewers, so it waits for an approval. Then it downloads that artifact and runs `powerops deploy apply --apply`. That command plans again against Test, refuses if anything differs from the reviewed plan, imports the zips in plan order and reports each cloud flow's state.

The workflow runs only for pull requests into `main` (`branches: [main]`), so a PR merged into a scratch branch never promotes. Change it if your release branch has another name.

There is no rollback. `deploy apply` refuses (exit 1) when it can, and stops at the first failed import, but an import that has run stays.

All values below are fake: `contoso` stands for your own.

## Use a private repository if you can

The artifact holds Dev's managed export. A canvas app's `.msapp` caches the URL of the environment it was made in, and flow definitions can hold hard-coded values (rule PO006). In a public repository, anyone signed in to GitHub can download an artifact while it is kept (30 days here).

For a public repository the example encrypts the zip before the upload (`openssl enc -aes-256-cbc -pbkdf2 -salt`), with a key stored as the secret `POWEROPS_ZIP_KEY` in both GitHub environments, and the apply job decrypts it. Make the key with `openssl rand -base64 32` and pipe it into `gh secret set`, so it is never printed. A private repository can drop the encrypt step, the decrypt step and the `.enc` in the upload path.

Encryption does not make a promoted value safe. Set `fail-on: warning` in the plan job (with a baseline for findings you accept) to stop hard-coded values (PO006) and shipped current values (PO008) from being promoted.

## Two app registrations

Use two Microsoft Entra app registrations:

- A **plan app**: a federated credential for the GitHub environment `test-plan` only. Its application user has the role an export needs in Dev and a role that can read in Test.
- An **apply app**: a federated credential for the GitHub environment `test` only. Its application user is in Test only, with the role an import needs.

The reason: a `pull_request` run uses the workflow file from the PR, so anyone who can push a branch can run code as the `test-plan` identity and read the secrets of `test-plan`. Those secrets must therefore be configuration only (URLs, ids, values, the zip key), never a credential, and the plan app must not be able to import. The `test` environment's reviewers decide who can use the apply app.

Also:

- Protect `.github/workflows/` (and the settings script) with CODEOWNERS or a repository ruleset, so a branch cannot change what runs in `test-plan` unreviewed.
- Turn on "Prevent self-review" on the `test` environment, so the person who pushed cannot approve their own apply.
- Make the `plan` job a required status check. Otherwise a PR can be merged with no plan for its last commit, and `find` fails.

For a quick trial in a repository only you can push to, one app registration with both federated credentials works. Do not do that where others can push branches.

## OIDC sign-in

`azure/login` and `pac auth create --githubFederated` use the job's OIDC token. The federated credential on the app registration must match the token's subject, which names the repository and the GitHub environment. Repositories created after 2026-07-15 present the immutable form:

```
repo:contoso-org@123456/powerops-demo@789012:environment:test
```

Read the form your repository uses with:

```sh
gh api repos/contoso-org/powerops-demo/actions/oidc/customization/sub
```

Add each app as an application user in each environment it needs, then give it a role:

```sh
pac admin assign-user --environment <environment id> --user <app id> --application-user --role "<role>"
```

The imported components are owned by the application user. Share the connection that the settings file binds with it, or the import cannot turn the flow on.

## GitHub environments and secrets

Create two environments, `test-plan` (no reviewers) and `test` (required reviewers). Required reviewers are available on public repositories on GitHub Free. Secrets belong to the environment, one secret per value, because log masking matches exact text and can miss parts of a JSON blob. The same names go in both environments:

| Secret | Example (fake) |
|---|---|
| `DEV_URL` | `https://contoso-dev.crm.dynamics.com` |
| `TEST_URL` | `https://contoso-test.crm.dynamics.com` |
| `AZURE_TENANT_ID` | `00000000-0000-0000-0000-000000000000` |
| `AZURE_CLIENT_ID` | the plan app's id in `test-plan`, the apply app's id in `test` |
| `TEST_CONNECTION_ID` | the connection id behind `contoso_sharepoint` |
| `TEST_ENVVAR_CONTOSO_SITEURL` | the Test value of `contoso_SiteUrl` |
| `POWEROPS_ZIP_KEY` | the zip key (public repositories) |

`docs/examples/powerops/build-settings.sh` writes the settings file from `TEST_CONNECTION_ID` and `TEST_ENVVAR_CONTOSO_SITEURL` with `jq` and a fixed filter, and never prints it. Both jobs call the same script and must have the same values, so the bytes, and so the plan hash, are equal. Add one secret and one entry for each environment variable and connection reference of your solution. Prefer single-line values: GitHub masks each line of a multi-line secret on its own, so a line such as `review` is hidden wherever that word appears in the job's log. The workflow calls the script at its path in this folder; if you copy it elsewhere, change both steps.

Fork pull requests get no secrets, so every job skips them.

## When an apply refuses

`deploy apply` plans again against Test and refuses (exit 1, nothing imported) if the new plan's hash differs from the reviewed one. The message names the first input that differs. Causes when nothing was meant to change:

- A secret that differs between `test-plan` and `test`, or that was rotated after the plan.
- The settings script, or the `Chrome71/PowerOps` ref, changed in the PR after the plan ran. The plan file's `tool` must equal the version that applies it, and two builds of one release cannot be told apart, so pin the action to one commit SHA and use the same SHA in both jobs.
- A platform update of a dependency solution in Test (each dependency's version is part of the hash).
- A change made in Test since the plan.

The `apply` job's concurrency group is repository-wide with `cancel-in-progress: false`, so GitHub keeps at most one waiting apply: a third merge cancels the second waiting apply. Re-run the cancelled promotion by hand (re-run its run).

The way back is to plan again: re-run the PR's last plan run (it replaces the artifact), review the new comment, then re-run the apply.

A `--target` that names another environment than the plan's, including a renamed Test environment, exits 2 with the message, and imports nothing.

## DLP and licences

Test's data loss prevention policy must allow the solution's connectors together, or the import can succeed and the flows stay off. In a Managed Environment, the people who run the flows need premium licences. A premium connector in a flow owned by an application user needs a Process or per-flow licence. These are questions for your admin.

## The window before each import, and no rollback

Between the re-plan and each import there is a short window in which someone can change Test. `deploy apply` hashes each zip and settings file again just before its import and stops if one changed, but it cannot see a change made in Test during that window. If an import fails or times out, the run stops, the report names the solutions already imported, and there is no rollback: check the environment's import history, then plan again.

## Plan and apply on the same OS

Run both jobs on the same operating system, as the example does (`ubuntu-latest`). A friendly environment name with non-ASCII characters may be decoded differently on Windows than on Linux, which would change the plan hash.

# Example: an ephemeral environment per pull request

[ephemeral-pr.yml](ephemeral-pr.yml) gives each pull request its own short-lived Power Platform environment. [ephemeral-sweep.yml](ephemeral-sweep.yml) deletes what a run leaves behind. The scripts they call are [ephemeral-env.sh](powerops/ephemeral-env.sh) and [ephemeral-sweep.sh](powerops/ephemeral-sweep.sh). **This recipe was not run end to end.** Its scripts are tested offline with stub `pac` and `gh` programs and its workflows are checked as text, but no environment was ever created or deleted with it. See "Not verified" below. It is opt-in: PowerOps' own CI never creates environments.

All values below are fake: `contoso` stands for your own.

## The risk, before anything else

The recipe signs in as an app that can create and delete environments. Once an Entra app is registered as a Power Platform management application, it acts like a Power Platform administrator in the **whole tenant**. Anyone who can run a workflow with that credential can create and delete any environment, production included.

Recommended:

- Run it in a tenant, or a region, that has no production environments.
- Keep the approval gate: the job runs in the GitHub environment `ephemeral` with required reviewers and "Prevent self-review", so each run waits for an approval, and the federated credential is bound to that environment, not to `pull_request`. Prevent self-review means a second person, not the one who triggered the run, has to approve it.
- Use an Entra app for nothing else, and a private repository.
- Protect `.github/workflows/` and `docs/examples/powerops/` (or your copy of it) with CODEOWNERS or a repository ruleset. A `pull_request` run uses the workflow file from the PR.
- Secrets in the job are configuration (tenant id, app id), never credentials.
- A fork PR gets no token, so the job skips forks. It skips Dependabot runs too, which get no secrets: the job checks the actor and the PR's author, because a person who re-runs a Dependabot PR becomes the actor.

## How it runs

On a pull request that touches `deploy/` (GitHub checks the workflow's `paths` against the whole diff of the PR, so every later push to that PR runs it again, even a push that changes nothing in `deploy/`; a folder of **managed** solution zips that your own build makes, for example `pac solution pack --packagetype Managed` from an unpack made with `--packagetype Both`), the job waits for an approval, then:

1. installs `pac` and signs in with GitHub OIDC, with no environment;
2. deletes this PR's environments from older runs that have finished (`ephemeral-sweep.sh` with `POWEROPS_SWEEP_PR`), so leftovers per PR stay bounded;
3. creates the environment with `pac admin create` (Sandbox by default; the step stops for any type but Sandbox and Trial, so the recipe never creates a production environment);
4. finds it with `ephemeral-env.sh find`, which masks its id and URL before writing them to the step outputs;
5. turns Managed Environments on (`pac admin set-governance-config --protection-level Standard`);
6. makes the app a System Administrator user of the new environment (`pac admin assign-user --application-user`);
7. signs in to the new environment;
8. runs the PowerOps action twice: `deploy plan`, then `deploy apply` with `apply: 'true'` (M9 and M10);
9. in a last step that always runs, signs in again and deletes the environment with `ephemeral-env.sh delete`.

The name of an environment is `<prefix>-<repository id>-pr<number>-<run id>-<attempt>`, with the numeric repository id from GitHub and the prefix from `EPHEMERAL_PREFIX` (`[a-z][a-z0-9-]{1,19}`). The scripts select only a display name that matches that whole pattern. Any other environment, even one an admin named with the prefix, is never touched. `delete` goes by id, and reads the list again just before it: it deletes only when exactly one environment has that id and exactly that name and its `Type` is `Sandbox` or `Trial`, and otherwise fails the job and deletes nothing. It does not need the environment's URL, because an environment still provisioning after a timed-out or cancelled create has none. The sweep applies the same `Type` check: a hand-made Production environment with a matching name is kept, counted as failed and not named.

Pin the PowerOps action to one commit SHA, and use the same one in both calls.

### Only for solutions without connection references

A new environment has no connection, so no settings file can bind a connection reference. `deploy plan` blocks a plan with an unbound reference and `deploy apply` refuses a blocked plan, so the run fails at the first PowerOps step. The plan looks like this (the wording can differ by version):

```
1 ContosoFlows (ContosoFlows_managed.zip): install not installed -> 1.0.0.0, managed
  Block settings contoso_sharepoint: connectionReference missing: in the solution but not in the settings file
plan blocked: 1 blocking item
```

Apps, tables and flows with built-in actions only work. An environment variable with no default needs a `Value` in `deploy/<zip name>.settings.json`, built from repository variables at run time as [build-settings.sh](powerops/build-settings.sh) does (without the connection references); the workflow has a comment where that step goes. Binding connections in a new environment would be its own `powerops` task with a test and a live check; this recipe does not promise it.

## Recommended setup

1. Make the Entra app. An admin signed in as a user registers it as a Power Platform management application with `pac admin application register --application-id <app id>` (Learn: "Create a service principal application"). Ask your admin first; see the questions at the end.
2. Create the GitHub environment `ephemeral` with required reviewers and "Prevent self-review". With self-review prevented, a second person has to approve each run.
3. Add the two federated credentials below, the secrets and the variables.
4. Copy the two workflows and the two scripts (and the settings script, if you need it). Change the paths if you move the scripts, and the `deploy/` folder if yours has another name.
5. Make `deploy/` hold the managed zips for the PR. The recipe does not pack them.

## The two federated credentials

Both belong to the one app, and `azure/login` and `pac auth create --githubFederated` use the job's OIDC token, so each credential's subject must match what its workflow presents:

| Workflow | Subject (name-based) | Subject (immutable, repositories created after 2026-07-15) |
|---|---|---|
| `ephemeral-pr.yml` | `repo:contoso-org/powerops-demo:environment:ephemeral` | `repo:contoso-org@123456/powerops-demo@789012:environment:ephemeral` |
| `ephemeral-sweep.yml` (default branch) | `repo:contoso-org/powerops-demo:ref:refs/heads/main` | `repo:contoso-org@123456/powerops-demo@789012:ref:refs/heads/main` |

Read the form your repository uses with `gh api repos/contoso-org/powerops-demo/actions/oidc/customization/sub`. The sweep has no GitHub environment and no approval, so protect the default branch: whoever can push to it can run code as the sweep's credential.

## Variables and secrets

| Name | Kind | Default | Used for |
|---|---|---|---|
| `AZURE_TENANT_ID` | secret (repository) | none; required | the tenant of the app |
| `AZURE_CLIENT_ID` | secret (repository) | none; required | the app's id |
| `EPHEMERAL_PREFIX` | variable | `powerops-pr` | the name prefix, `[a-z][a-z0-9-]{1,19}` |
| `EPHEMERAL_TYPE` | variable | `Sandbox` | `Sandbox` or `Trial`; any other value stops the step |
| `EPHEMERAL_REGION` | variable | `unitedstates` | `pac admin create --region` |
| `EPHEMERAL_CURRENCY` | variable | `USD` | `pac admin create --currency` |
| `EPHEMERAL_LANGUAGE` | variable | `1033` | `pac admin create --language` |
| `POWEROPS_SWEEP_DELETE` | variable | not set: a dry run | the scheduled sweep deletes only when this is `true`; a manual run ignores it and follows its `delete` input only. The PR workflow's own PR-scoped sweep step deletes that PR's finished leftovers without this variable |

The sweep has no GitHub environment, so the two secrets must be repository secrets. A manual run of the sweep deletes only when its input `delete` is on (default off), whatever the variable says, and the scheduled run only when the variable is `true`. The PR workflow's first sweep step (`POWEROPS_SWEEP_PR`, with deleting on) is separate: it deletes that PR's leftovers from finished runs without the variable. It races the scheduled sweep and an earlier run's delete step, so a failure there is a warning and the run goes on.

The sweep needs `id-token: write`, `actions: read` and `contents: read` and nothing more; the workflow sets `GH_REPO` and `GH_TOKEN`. The PR job needs the same three.

## The sweep

A cancel (a new push, or a person) during `pac admin create` ends `pac` while the service goes on creating the environment. The delete step may find nothing yet, so the environment is deleted by the next sweep, or by the PR's next run (step 2), not by the cancelled run. No cancelled run was tried.

The sweep reads one `pac admin list --json`, keeps the names that match this repository's pattern, and asks `gh run view` about each run id. It deletes an environment only when `gh` reports that run as completed (a finished run should already have deleted it), whatever the state of the PR. A run in progress keeps its environment; a `gh` failure keeps it and turns the sweep red, so GitHub notifies you. It prints counts and the names of the recipe's own environments, never the list. The scheduled sweep is a dry run until `POWEROPS_SWEEP_DELETE` is `true`; a manual run deletes only with `delete` on.

Runs are kept for 90 days (the default retention), and a schedule that GitHub disabled after 60 days without activity lets leftovers age past that. A leftover whose run no longer exists makes `gh run view` fail, so the sweep keeps it and fails every time. Delete it by hand: find its id with `pac admin list`, check the name and that it is yours, then run `pac admin delete --environment <environment id>`.

GitHub disables a schedule after 60 days without activity in a public repository, and then leftovers build up with nothing red anywhere. Look at `pac admin list` for the prefix by hand now and then.

A deleted environment is soft-deleted and can be recovered for 7 days (Learn: "How do I recover a recently deleted environment?").

## Costs

- **Database capacity** for every environment that exists at once: runs in progress and leftovers, not open PRs. A sandbox needs 1 GB of free database capacity (Learn: "Create and manage environments"). Learn says recovering a deleted environment may need free capacity, which suggests a deleted one does not hold it, but does not say so. Nothing limits how many runs happen at once across the repository; the concurrency group is per PR. Add a repository-wide group that queues if you need a ceiling.
- **Premium licences** for every user who runs an app or flow in a managed environment (Learn: "Managed Environments licensing").
- **An admin-level app**: the management-app registration is the risk described at the top.

## DLP

A new environment falls under the tenant's DLP policies. A policy can suspend a flow, and `deploy apply` then reports a flow that is not in its expected state. The recipe runs no tests in the environment.

## What the recipe checks, and what it does not

It checks that the managed zips import into an empty managed environment and that the cloud flows reach the state the plan expected (`deploy apply`). It does not run tests, does not check data, and does not deploy a solution with a connection reference. Every `pac` and `gh` call's output goes to a file and is printed only on failure with debug logging on, because it can name other environments: do not turn on debug logging in a public repository.

## Not verified

No environment was created or deleted for this recipe (decision of the master session, 2026-10-23; the maintainer's tenant has no capacity for a sandbox). Nothing below was run.

| Step | What is assumed | Where it comes from | Status |
|---|---|---|---|
| Create | A service principal can create an environment once it is a management application | Learn: "Create a service principal application", `pac admin create` | not run |
| `pac` flags | `--json`, `--async`, `--protection-level`, `--application-user`, `--githubFederated`, and the value forms `--region unitedstates`, `--currency USD`, `--language 1033` | Learn's current `pac admin` reference, which names no pac version | not checked against the pinned pac 2.14.1 |
| `pac admin list --json` | A JSON-only stdout with the fields `DisplayName`, `EnvironmentId` and `EnvironmentUrl` | Observed with pac 2.13.1 on 2026-10-23 in a read of a tenant; Learn documents neither | not checked on the pinned version |
| Sign-in without an environment | `pac auth create --githubFederated` with no environment serves the `pac admin` commands | Learn's `pac auth create` reference | not run |
| Application user | `assign-user --application-user` works for the app that created the environment, and the role `System Administrator` exists | Learn's `pac admin assign-user` reference | not run |
| Sign-in to the new environment | The app signs in to the environment it just created (`pac auth create --environment`), and `az account get-access-token` works for it | none; the open question of FOUNDATION section 8 | not run |
| Import | An import works right after `create` returns, and `deploy plan` of an empty environment holds only installs | M9 and M10 against existing environments only | not run |
| Cancel | A cancelled run's environment is deleted by the sweep or the PR's next run | GitHub's cancellation rules and the scripts' tests with stubs | not run; no cancelled run tried |
| Delete | `pac admin delete --async` returns within GitHub's 5-minute window after a cancel | Learn: `delete` waits by default, `--async` does not | not run |
| Name | `pac admin create --name X` gives the environment the `DisplayName` X | Learn's `pac admin create` reference documents `--name` | not run |
| Run status | `gh run view <id>` reports the latest attempt of that run | none read | not run |
| Timeout | An `if: always()` step runs after a job-level timeout | GitHub's rules for `always()` and cancellation; not read for a timeout | not run |
| Provisioning | `pac admin list` lists environments that are still provisioning or being deleted | Learn is silent | not run |
| Capacity | A soft-deleted environment does not hold database capacity | Learn is silent | not verified |
| The sweep | The schedule, the second federated credential, `gh run view` with the job's token and the deletes | the script's tests with stubs | not run |

The offline tests (`tests/PowerOps.Cli.Tests/EphemeralScriptTests.cs` and `ExampleWorkflowTests.cs`) run the scripts against stub `pac` and `gh` programs and check the workflows' text. They show what the scripts select, mask and delete given the list the stubs print; they do not show that real `pac` prints that list.

## A safe first use

1. A throwaway private repository, a tenant or region with no production environments **and no environment whose name matches the pattern** (the PR workflow's own sweep step deletes this PR's finished leftovers without the variable), a dedicated app, a second person to approve each run (Prevent self-review), and a `ContosoTables`-like solution with no connection reference in `deploy/`.
2. Run the sweep by hand with `delete` off. It must list nothing, or only what you expect.
3. Open one PR, approve it, and watch the run. Check with `pac admin list` that the environment was created, deployed into and deleted.
4. Cancel one run during the create step, then run the sweep with `delete` off, then on, and note what happened.
5. Only then set `POWEROPS_SWEEP_DELETE` to `true` and use it for real.

## Questions for your admin

- May a GitHub app be registered as a Power Platform management application (it then acts like a Power Platform administrator), and in which tenant should it run?
- Who may create environments in the tenant (the "Control environment creation" setting), how much capacity is free, and who pays for it?
- Which DLP policies apply to a new environment?
- Does the application user, as the owner of the imported flows, need a Process or per-flow licence?
