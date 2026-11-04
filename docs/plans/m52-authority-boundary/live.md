# M52 live check (2026-11-04)

Done when 5, run in the test repository `Chrome71/powerops-action-test` with its promotion workflow (a copy of `docs/examples/promote-dev-to-test.yml` for the seed solution), pinned to this branch at `f18ec0d` by the test repository's PR 8. The person said yes in chat, before each step, to: changing the `test` environment's rules, repinning the workflow, and an apply into Test that the person approves. The person did every approval click; this session approved nothing. No sign-in was needed: both jobs sign in with GitHub OIDC. All ids of the tenant, its environments and the app are left out.

## The `test` environment

| Setting | Before | After |
|---|---|---|
| Required reviewers | one user, the person | unchanged |
| Prevent self-review | off | on |
| Administrators can bypass | on | off |
| Deployment branch rule | none | none (see below) |

The person also allowed a `main`-only branch rule. It was not set: GitHub matches the rule against `GITHUB_REF`, which is `refs/pull/<n>/merge` for the merged-PR run this workflow applies from, so the rule would block every apply. `docs/GITHUB-ACTION.md` says so (decisions log).

## The apply credential (read with `az ad app federated-credential list`)

One app registration has two federated credentials, with the subjects `repo:<owner>/powerops-action-test:environment:test` and `repo:<owner>/powerops-action-test:environment:test-plan` (GitHub's immutable form, ids left out). So the credential that imports into Test is not bound to `test` alone: `test-plan` has no reviewers, and a `pull_request` run of a branch in this repository uses the workflow file from that branch. Anyone who can push a branch here, an agent with the person's GitHub credential included, could import into Test without the reviewer. This is the "quick trial" setup that `docs/examples/README.md` allows only for a repository only you can push to. **In this repository, the yes of the `test` environment is not yet an authority boundary.** The fix is the README's two app registrations: a plan app for `test-plan` that can't import, and an apply app for `test` only. That is an Entra and Dataverse change in the person's tenant, not done here.

## Runs

| Run (attempt) | What happened | Imported |
|---|---|---|
| `41928857146` (1) | The test PR's merge started it as the person's account. The apply job waited for `test`. `pending_deployments` said `current_user_can_approve: false` for this session's credential (the person's account). This session's approval through the REST API was refused: HTTP 422, "No pending deployment requests to approve or reject". The person then turned Prevent self-review off and approved in the GitHub UI (asked to reject; the click approved, and the person's yes to an apply into Test covered it, so the import was left to finish). | yes |
| `41928857146` (2) | Prevent self-review back on (the person). This session re-ran the apply job: it waited for `test`, `current_user_can_approve` was `false`, and the approval through the API was refused again with the same 422. This session cancelled the run; the apply job ran 0 steps. | no |

Run 1's apply job, from its log:

- `Check the apply` passed in 0.3 s: `path` and `plan-file` named the same folder, and the job's OIDC token carried `environment: test`. Neither the token nor the response is in the log (no `eyJ`, no `bearer`).
- The report: `Executor: action`; `Approval: action, receipt Chrome71/powerops-action-test run 41928857146 attempt 1, environment test`; `Applied at: 2026-11-04T22:58:29Z`; the seed solution `upgrade, ok` (1.0.0.1 to 1.0.0.2, plan hash `29414e7c…`, the one the PR's plan comment showed); its flow `expected on, now on`; `applied`.
- GitHub's approvals list for the run shows one `approved` review for `test` by the reviewer account, with no comment.

What this shows: with Prevent self-review on and the administrators' bypass off, the account that started a run can't approve it, through the UI or the API, so an agent that works under that account can't approve its own deploy. With one GitHub account for the person and the agent, nobody else can approve either, so the person had to turn Prevent self-review off to approve run 1. The record of who approved is GitHub's; the report's receipt only points at it.

## Not checked

- `check-apply.sh` on a Windows runner (both jobs run on `ubuntu-latest`).
- A refusal by `Check the apply` itself: no run here lacked an environment or bound another folder. The script's tests cover those cases offline.
- The MCP form route (not needed: the Action route ran).

## Left behind (nothing deleted)

- Test: the seed solution at 1.0.0.2, managed, its flow on.
- The test repository: `main` has the merge of PR 8, so the promotion workflow pins `f18ec0d`; the branch `m52-live`; the `test` environment with the rules "After" above. The `test-plan` environment is unchanged.
- The app registration with both federated credentials, as before.
