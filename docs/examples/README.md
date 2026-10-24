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
