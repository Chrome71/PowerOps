# GitHub Action

The PowerOps action analyzes solutions in a GitHub Actions workflow. It can also run flow tests as a [merge gate](#merge-gate-flow-tests) and post a [pull request comment](#pull-request-comment) with a deploy plan or a diff.

The analysis writes a job summary and annotations, and uploads the findings to code scanning:

```yaml
permissions:
  contents: read
  security-events: write   # for the SARIF upload

steps:
  - uses: actions/checkout@v7
  - uses: Chrome71/PowerOps@b4abce4cb00421c7017076de7b53e53a7ce063c7 # main on 2026-10-28; pin the newest commit of main
    with:
      path: solutions        # default: .
      fail-on: error         # error | warning | none
      # baseline: powerops-baseline.json
      # tests: flowtests     # *.flowtest.json files to run; see "Merge gate: flow tests"
```

No release is tagged yet, so these snippets pin a commit of `main` (the action builds PowerOps from source). Replace it with the full SHA of the newest commit of `main`. Pull requests from forks cannot upload SARIF (their token has no `security-events` permission); they still get the job summary and the annotations.

## Merge gate: flow tests

With `tests` set to a folder of `*.flowtest.json` files ([Flow tests](FLOW-TESTS.md)), the action runs `powerops test <path> --tests <folder>`, writes the report to the job summary, adds an error annotation for each test file that did not pass, and its last step fails the job when the exit code is not `0`: a test `failed`, is `unmocked` or `unsupported`, or no test file or solution was found. It needs no tenant and no secret, so it runs on pull requests from forks too. GitHub shows at most 10 error annotations per step; the job summary lists every test. The annotations show on the check run, and beside the file in "Files changed" only when the test file is in the PR's diff.

Run the tests on committed files only, never on an export from an environment: the report quotes values from the solution and the test files. It gates only the flows that have test files, and only when `tests` is set: with `tests` empty the action runs no tests, and the job summary has no "Flow tests" section. A commit of the action from before the `tests` input was added ignores it with a warning.

A failing test blocks a merge only when the job is a required status check. A workflow for it:

```yaml
name: PowerOps flow tests
on: pull_request   # no filter: see below. With a merge queue, add merge_group.

permissions:
  contents: read

jobs:
  powerops-flow-tests:   # the name of the required check; no other workflow may have a job of this name
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1
      - uses: Chrome71/PowerOps@b4abce4cb00421c7017076de7b53e53a7ce063c7 # main on 2026-10-28; pin the newest commit of main
        with:
          path: solutions
          tests: flowtests
          fail-on: none          # the check fails on flow tests only; remove this line to gate the analysis too
          upload-sarif: 'false'  # so the job needs only contents: read
```

Run it once, so GitHub knows the check. Then, in the repository's settings, add a ruleset (Rules, Rulesets, New branch ruleset) for the default branch, or a branch protection rule, with "Require status checks to pass" and the check `powerops-flow-tests` from GitHub Actions. GitHub's docs give the reasons for these rules:

- The check is named after the job (its `name:`, else its id). The same job name in several workflows makes status check results ambiguous and can block pull requests, so give it a name no other workflow uses.
- Put no path, branch or commit-message filter on the workflow: a workflow skipped by one leaves its required check pending, and the PR cannot be merged. Put no `if:` on the job either: a job skipped by a condition reports success, so the PR could merge untested.
- With a merge queue, add `merge_group` to `on:`. Otherwise the required check is never reported for the queued merge, and the merge fails.

A pull request runs its own workflow file and test files, so a PR could remove the gate: protect `.github/workflows/` and the tests folder with CODEOWNERS and required code-owner reviews, or with a ruleset. Workflows of a fork PR may wait for a maintainer's approval (the repository's Actions setting for fork pull request workflows); until then the check is pending.

## Pull request comment

With `comment: 'true'` on a `pull_request` event, the action posts one pull request comment and edits it on each later run (it finds its own comment by a hidden marker and the `github-actions[bot]` author, so a PR keeps one PowerOps comment). It is off by default, so existing workflows do not start needing `pull-requests: write`. What the comment holds depends on `target`:

- With `target` (and `deploy-folder`) set, it holds the `deploy plan` of the zips in `deploy-folder` against that environment, and the plan file is written to `deploy-folder/powerops-plan.json` (the `plan-file` output names it). `pac` and `az` must be signed in by earlier steps; the action signs in to nothing. The step records the plan's exit code, writes the comment, and then a last step fails the job when the plan is blocked or could not run. If `deploy plan` fails (exit `2` or `3`), the comment says only that the plan could not run and gives the exit code; the log shows only powerops' own `error:` lines, because text forwarded from `az` and the Web API can name tenant ids and the environment URL. Set the runner's debug logging on (`RUNNER_DEBUG`) to see the forwarded text.
- Without `target`, it holds the `diff` of the PR's base and head solution (`path` must name one solution, relative to the repository root). The diff reads the first parent of the checked-out commit, so it needs the default `actions/checkout` of a pull request, which checks out the merge commit, with the default `persist-credentials` (the script fetches the base commit). If the solution is new in the PR, `path` holds several solutions, or the fetch fails, the comment says that the diff could not run and which step failed.
- A PR from a fork gets no sign-in and no secrets, and its token cannot comment. For a fork PR the action skips the plan and writes the diff to the job summary only; the run passes.
- With `plan-file` set (the path of a plan file written by an earlier `deploy plan`; its folder holds the zips and settings files), the action runs `powerops deploy apply` against `target` instead of planning. `apply: 'true'` adds `--apply` and imports; any other value is a dry run that imports nothing. `target` is required, and the step fails with an error on a fork pull request or on `pull_request_target`. The log and the job summary show the report; as for the plan, only powerops' own `error:` lines are logged unless debug logging is on, and a non-zero exit fails the job with one message per case (1 refused or a flow not as expected, 2 usage error or `--target` mismatch, 3 failed, with no rollback). The report steps run first; then `fail-on` gates the apply: the action analyzes `path` again with the real `fail-on` (and `baseline`) before `deploy apply`, and a failure there fails the job before anything is imported. With `tests` set too, a failing flow test also stops the apply.
- A comment over 65,000 bytes is replaced by a short note, and the full report goes to the job summary. If the comment cannot be posted (a read-only token, for example on a Dependabot run), the action writes a warning and the job summary and does not fail the job.
- The plan Markdown always goes to the job summary. The comment goes there instead of the PR only for a fork PR, a body over the size cap, or a failed post. The comment is public in a public repository: it shows the target's friendly name, solution, file and flow names and versions, never a setting's value or a connection id.
- The scripts use `gh` and `git`, which GitHub-hosted runners have installed; a self-hosted runner needs both. `pull_request_target` is not supported.

An example workflow. The plan job runs in a GitHub environment (`test-plan`) whose environment secrets hold the target's URL, the tenant id, the app id and one secret for each value the settings file needs. The settings file is built at run time with `jq` under `$RUNNER_TEMP`, never printed, and left out of the artifact. Sign-in and plan run only for same-repository PRs. Fake values:

```yaml
name: Deploy plan
on: pull_request

permissions:
  contents: read
  pull-requests: write   # the comment
  id-token: write        # the OIDC sign-in

concurrency:
  group: powerops-plan-${{ github.event.pull_request.number }}   # two quick pushes must not both create a comment
  cancel-in-progress: true

jobs:
  plan:
    runs-on: ubuntu-latest
    environment: test-plan
    env:
      SAME_REPO: ${{ github.event.pull_request.head.repo.full_name == github.repository }}
    steps:
      - uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1
      - uses: actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68 # v6.0.0
        with:
          dotnet-version: 10.0.x

      # Masking matches exact text only, so every form of the target's host is masked before any sign-in.
      - name: Mask the target URL's forms
        if: env.SAME_REPO == 'true'
        env:
          URL: ${{ secrets.TEST_ENVIRONMENT_URL }}
        run: |
          host="${URL#*://}"; host="${host%%/*}"
          for v in "https://$host" "https://$host/" "$host" "${host,,}" "${host^^}" "${host%%.*}"; do echo "::add-mask::$v"; done

      # The deploy folder holds the zips and, beside each, its settings file.
      - name: Build the deploy folder
        if: env.SAME_REPO == 'true'
        env:
          SITE_URL: ${{ secrets.CONTOSO_SITE_URL }}
          CONNECTION_ID: ${{ secrets.CONTOSO_SHAREPOINT_CONNECTION_ID }}
        run: |
          mkdir "$RUNNER_TEMP/deploy"
          cp exports/*.zip "$RUNNER_TEMP/deploy/"
          jq -n --arg site "$SITE_URL" --arg conn "$CONNECTION_ID" '{
            EnvironmentVariables: [{SchemaName: "contoso_SiteUrl", Value: $site}],
            ConnectionReferences: [{LogicalName: "contoso_sharepoint", ConnectionId: $conn, ConnectorId: "/providers/Microsoft.PowerApps/apis/shared_sharepointonline"}]
          }' > "$RUNNER_TEMP/deploy/ContosoApp_managed.settings.json"

      - name: Install pac
        if: env.SAME_REPO == 'true'
        run: |
          dotnet tool install --global Microsoft.PowerApps.CLI.Tool
          echo "$HOME/.dotnet/tools" >> "$GITHUB_PATH"

      - uses: azure/login@<full commit SHA> # use the latest release of azure/login
        if: env.SAME_REPO == 'true'
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          allow-no-subscriptions: true   # the app has no Azure subscription, only the environment

      - name: Sign in pac
        if: env.SAME_REPO == 'true'
        env:
          TENANT_ID: ${{ secrets.AZURE_TENANT_ID }}
          CLIENT_ID: ${{ secrets.AZURE_CLIENT_ID }}
          ENVIRONMENT_URL: ${{ secrets.TEST_ENVIRONMENT_URL }}
        run: |
          # pac's sign-in output stays out of the public log unless debug logging is on.
          if ! pac auth create --githubFederated --tenant "$TENANT_ID" --applicationId "$CLIENT_ID" --environment "$ENVIRONMENT_URL" > "$RUNNER_TEMP/pac-auth.log" 2>&1; then
            echo "::error::pac auth create failed; its output is printed only with debug logging on"
            if [ "${RUNNER_DEBUG:-}" = 1 ]; then cat "$RUNNER_TEMP/pac-auth.log"; fi
            exit 1
          fi

      - uses: Chrome71/PowerOps@b4abce4cb00421c7017076de7b53e53a7ce063c7 # main on 2026-10-28; pin the newest commit of main
        with:
          path: solutions/ContosoApp        # the unpacked solution: analyzed, and diffed on a fork PR
          tests: flowtests                  # flow tests of that solution, also on a fork PR
          comment: 'true'
          deploy-folder: ${{ runner.temp }}/deploy
          target: ${{ env.SAME_REPO == 'true' && secrets.TEST_ENVIRONMENT_URL || '' }}

      # The zips and the plan file, for a later apply job; never the settings files.
      - uses: actions/upload-artifact@<full commit SHA> # use the latest release of actions/upload-artifact
        if: always() && env.SAME_REPO == 'true'
        with:
          name: powerops-plan
          path: |
            ${{ runner.temp }}/deploy/*.zip
            ${{ runner.temp }}/deploy/powerops-plan.json
```

A complete Dev to Test promotion (a plan on the PR, then an apply in an environment with required reviewers) is in [docs/examples/](examples/README.md); read its README for the setup and the risks.

An opt-in recipe that gives each pull request its own short-lived environment, with a sweep for leftovers, is in the same folder. It was not run end to end: read [its section](examples/README.md#example-an-ephemeral-environment-per-pull-request) first, which opens with the risk.

Pin every action to a full commit SHA, as above. In a public repository, anyone who can push a branch can run code as the identity the plan job signs in with, so give the app registration of the plan job a read-only role in the target, not an administrator role.
