---
status: approved
review: pending
environment: null
depth: quick
supersedes: null
---

# M52: The authoring agent cannot approve its own deploy

## Goal

Hold the yes to an import where the credential is, not in a file an agent can write, and record by which route each apply ran. This is M52 of the improvement plan written after the live test of 2026-11-04, the first milestone of its P0 block. That plan, its two reviews and the person's answers to its decisions D1 to D9 are kept outside the repo, because they quote a private test run. The test found that the gate's plan file proves nobody reviewed it (`docs/HANDOFF.md:100`), that the apply report has no time and no route (C-5), that the MCP form can't be used outside an interactive client (C-9), and that the Action analyzes `path` but imports from the folder of `plan-file` without binding them (CX-3).

D1 is answered by the person: "Confirm both (Recommended)". The two supported yes routes are (1) the MCP form in an interactive client, where the person types the environment's name (built in M19, `src/PowerOps.Cli/McpCommand.cs:290-378`), and (2) the GitHub Action in a protected GitHub environment with required reviewers and "Prevent self-review", whose OIDC federated credential exists only in that environment. The local `deploy apply --apply` stays person-run and is documented as unenforced.

Done when:

1. The apply report (JSON and text) records `appliedAt`, `executor` and `approval` as the caller reports them, with no identity claim, and the plan file records `plannedBy` the same way. Neither is in the plan's hash.
2. The Action refuses, before the gate step and before anything is imported, a `path` that is not the folder of `plan-file`, and `apply: 'true'` in a job whose OIDC token has no `environment` claim. A dry run needs neither an environment nor an approval.
3. The schemas, goldens, `docs/COMMANDS.md`, `docs/HANDOFF.md`, `docs/USE-WITH-AGENTS.md`, `docs/GITHUB-ACTION.md` and `docs/examples/README.md` say what each route proves and what it doesn't, and that the local CLI is unenforced.
4. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan are clean; the PR is green on all eight checks.
5. Live, before 2026-11-21: one Action run refused before approval and one approved apply to Test are recorded in `live.md` in this folder, with the environment's protection rules and the credential's scope; or the same record through the MCP form in an interactive session; or `live.md` lists the check as still to do and why.

### Won't build

- An approval that the CLI checks (a signed approval file, `deploy approve --by`): the joint plan rejected it, because a file an agent can write authenticates nothing. Further authenticated CLI routes are out of scope (decisions Q1).
- Identity claims in the report: no OS user, typed name, `pac auth who` account or GitHub actor. The receipt points at GitHub's own record of who approved (decisions Q2).
- A check that `plannedBy` differs from the approver, or any read of `plannedBy` by `deploy apply` or `deploy gate`: it is provenance, as the caller reports it (decisions Q6).
- Skill changes that pass `--planned-by agent` from the CLI: M54's actor marking.
- A check of the GitHub environment's protection rules from the Action: the claim proves only that the job named an environment. The live check records the rules (decisions Q4).
- `plannedBy` in the text and Markdown plan reports; a `docs/BUILD-PLAN.md` section for M50 to M68 (decisions Q11).

## Users

- A person who promotes a reviewed plan with the Action, and the reviewer of the GitHub environment who approves the job.
- A person who answers the MCP form in Claude Code or Codex.
- A person who runs `deploy apply --apply` themselves.
- An auditor who reads the apply report afterwards and wants to know when, by which route and with which receipt it ran.

## Data

Line numbers are those of `8d7e304`.

| # | Severity | Change | Where | Test |
|---|---|---|---|---|
| 1 | medium | `DeployApplyResult` gains `string? AppliedAt`, `string Executor` and `Approval? Approval`, with `record Approval(string Mode, string? Receipt)` and constants: executors `cli`, `mcp`, `action`; modes `cli`, `form`, `action`. `DeployApplyReport.WriteJson` writes `appliedAt`, `executor` and `approval` after `message`. `WriteText` adds `Executor: <executor>` after the `Plan hash:` line, then `Approval: <mode>` (with `, receipt <receipt>` when there is one) when `approval` is not null, then `Applied at: <time>` when `appliedAt` is not null, each through `TextReport.Clean` | `src/PowerOps.Core/Deploy/DeployApply.cs:12-15`, `src/PowerOps.Core/Output/DeployApplyReport.cs:23-47,66-68` | `DeployApplyReportTests`: the three JSON fields in order and with nulls; the text lines for a dry run (executor only), a CLI apply, a form apply and an Action apply with a receipt; a receipt with a line break is cleaned in text |
| 2 | medium | `deploy apply` gains `--github-run <text>`: 1 to 200 characters with no control character, else exit 2 before anything is read. `executor` is `action` with `--github-run`, `mcp` when the in-process caller says so (row 4), else `cli`. `approval` is null without `--apply`; with it, `form` when the in-process caller passed the approved hash, `action` with `--github-run` as its `receipt`, else `cli`, each other receipt null. `appliedAt` is the UTC time, to the second (`yyyy-MM-ddTHH:mm:ssZ`), at which an `--apply` run passed its checks and started importing: set for `imported` and `failed`, null for `planned` and `refused`. `Program.Run`, `Program.Root` and `DeployCommand.Create` gain `bool mcp = false`, passed on like `approvedHash` | `src/PowerOps.Cli/DeployCommand.cs:28-63,261-337`, `src/PowerOps.Cli/Program.cs:39-77` | `DeployApplyCommandTests`: a dry run is `cli` with no approval and no time; `--apply` is `cli`/`cli` with a time between the test's before and after; `--github-run` gives `action`, and with `--apply` the receipt; the approved-hash entry gives `form`; a refusal with `--apply` keeps its approval and has no time; an empty, 201-character or line-break `--github-run` exits 2 with no read and no `pac` call. The goldens' `Scrub` sets `appliedAt` to `2000-01-01T00:00:00Z` |
| 3 | low | `deploy plan` gains `--planned-by agent\|person\|action` (`AcceptOnlyFromAmong`). `DeployPlanReport.WriteJson` gains `string? plannedBy = null` and writes `plannedBy` after `tool`, so the plan file and the JSON report carry it; null when the option is not given. It is not a hash input, and `DeployPlanFile.Read` ignores it | `src/PowerOps.Cli/DeployCommand.cs:33-45,69-131`, `src/PowerOps.Core/Output/DeployPlanReport.cs:47-51` | `DeployCommandTests`: `--planned-by agent` reaches stdout and the `--save` file; no option gives null; the hash is the same with and without it; another value exits 2. `DeployPlanReportTests`: the field's place and its null. `DeployApplyCommandTests`: a plan file whose `plannedBy` is edited still applies |
| 4 | medium | MCP: `deploy_plan` adds `--planned-by agent` to its arguments; `deploy_apply`'s `Approved` passes `mcp: true` for the dry runs and the apply. The form, its refusals and the dry run before it are unchanged | `src/PowerOps.Cli/McpCommand.cs:176-181,442-469` | `McpCommandTests`: the saved plan file has `plannedBy` `agent`; a `deploy_apply` dry run reports `executor` `mcp` and no approval; an accepted form reports `mcp`, `form` with no receipt, and a time. Already covered and kept: a declined or cancelled form, a client with no form capability, and a dry run with no form |
| 5 | medium | Action: a new step `Check the apply` (id `check`), after `Deploy plan` and before `Gate the apply`, with `if: inputs.plan-file != ''`, no `always()` and no `continue-on-error`, runs `action/check-apply.sh` with `PO_PATH`, `PO_PLAN_FILE` and `PO_APPLY`. The script: (a) resolves `path` and the folder of `plan-file` with `cd` and `pwd -P` in a subshell, and when either fails or they differ prints `::error::` and exits 2; (b) when `apply` is exactly `true`, asks for the job's OIDC token with the audience `powerops-environment-check` through `ACTIONS_ID_TOKEN_REQUEST_URL` and `ACTIONS_ID_TOKEN_REQUEST_TOKEN`, reads the payload's `environment` claim with `base64 -d` and `jq`, and refuses (exit 2) when either variable is unset (no `id-token: write`), the request fails, or the claim is missing, empty or holds a control character; otherwise it writes `environment=<name>` to `GITHUB_OUTPUT`. It never prints the token or the response. `Deploy apply` gets `PO_ENVIRONMENT: ${{ steps.check.outputs.environment }}`, and `apply.sh` passes `--github-run "run $GITHUB_RUN_ID attempt $GITHUB_RUN_ATTEMPT"`, with `, environment <name>` when there is one. `plan.sh` passes `--planned-by action`. The input descriptions of `path`, `plan-file` and `apply` say the two rules | `action.yml:6-8,33-38,116-152`, `action/apply.sh:33-35`, `action/plan.sh:14`, new `action/check-apply.sh` | `ActionYmlTests`: the step's place before `Gate the apply` and `Deploy apply`, its `if`, `env` and script, no `always()` or `continue-on-error`, and `Deploy apply`'s `PO_ENVIRONMENT`. `ActionScriptTests` with a stub `curl` first on `PATH`: an equal folder passes in two spellings (a relative path and an absolute one with a trailing `/`); another folder, a missing folder and a zip as `path` each exit 2 with `::error::` and no `curl` call; a dry run passes with no token variables and no `curl` call; `apply: true` without the variables, with a failing `curl`, with a token whose payload has no `environment` or an empty one, and with one whose name holds a line break each exit 2 and write no output; a token with `"environment":"test"` exits 0 and writes `environment=test`; the token never reaches stdout or stderr. `apply.sh` passes the receipt with and without an environment; `plan.sh` passes `--planned-by action` |
| 6 | low | Schemas, `schemaVersion` kept at 1 (adding fields keeps it, `docs/schemas/README.md`). `deploy-apply`: `appliedAt` (string with the pattern of row 2, or null), `executor` (enum), `approval` (null, or an object with `mode` (enum) and `receipt` (string or null)), all three required. `deploy-plan`: `plannedBy` (enum `agent`, `person`, `action`, or null), required. Goldens rewritten by the recipe: `deploy-apply*.json`, `deploy-apply*.txt`, `deploy-plan-seed.json`, `deploy-plan-blocked.json`, `deploy-plan-same-version.json` | `docs/schemas/deploy-apply.schema.json`, `docs/schemas/deploy-plan.schema.json` | `OutputSchemaTests` validates every golden; `git diff tests/golden` shows only the new lines |
| 7 | low | `deploy gate`: a plan file whose hash was edited after planning blocks on the hash (a plan regenerated from the same inputs has the same hash, so it is no tamper case) | none, the dry run already refuses it | `DeployGateCommandTests.BlockNames` gains `plan-file-hash-edited`; the tamper cases of `deploy apply` (a hash that no longer matches, another target, a changed zip or settings file) are `A_changed_input_is_refused_naming_it_and_nothing_is_imported` and `A_target_whose_friendly_name_differs_from_the_plans_exits_2_and_imports_nothing`, which already assert that `pac` is never called |
| 8 | medium | Docs. `docs/COMMANDS.md`: the usage lines and the `deploy plan` and `deploy apply` sections give the two options, the four fields, that they are provenance and not authentication (no field proves who approved; the Action's receipt points at GitHub's record of the approval), that neither field is a hash input, and, in the Agents line, the two yes routes and that a local `--apply` is checked by nothing. `docs/HANDOFF.md`: "What it doesn't do" leads with the deny-lists as the control that stops an import, with the gate as defence in depth, and the self-plan line names the two yes routes. `docs/USE-WITH-AGENTS.md`: the `deploy_apply` bullet and "Answer the form yourself" say the form is the yes in an interactive client, and that outside one the yes is the Action's protected environment, not an agent running the CLI. `docs/GITHUB-ACTION.md`: the apply paragraph gives the two refusals, `id-token: write`, the receipt, and what the environment must have: required reviewers, "Prevent self-review", administrators' bypass off, the deployment branch rule, and the apply credential bound to that environment only; and that an agent holding a reviewer's GitHub credential can approve through the API. `docs/examples/README.md`: the same setup points, the one-person repository limit, and that the example already passes both checks | `docs/COMMANDS.md:21-24,81-93`, `docs/HANDOFF.md:98-104`, `docs/USE-WITH-AGENTS.md:58,77`, `docs/GITHUB-ACTION.md:69`, `docs/examples/README.md:23-45` | `DocsDriftTests` (flags and Action inputs the docs name exist), `HandoffDocTests`, `ExampleWorkflowTests` |

## Automation

The Action gains one step and one script (row 5). `deploy gate` keeps its rules: it reads only the dry run, which ignores the new fields.

## Screens

None. The MCP form's text is unchanged.

## Environments and route

The build reads and writes no tenant: every test uses the fakes. The live check of Done when 5 runs in the test repository `Chrome71/powerops-action-test`, whose promotion workflow M10 ran against Test, pinned to this branch's head. Before it, the session asks the person in chat, separately, before it: creates or changes a GitHub environment or its protection rules; triggers a run with `apply: 'true'` or calls `deploy_apply` with `apply: true` (naming Test); or does anything that costs money. The person does every sign-in and every approval; the session approves nothing. The record notes:

- the `test` environment's required reviewers, "Prevent self-review", administrators' bypass and deployment branch rule, and the apply app's federated credential subject (environment `test` only), each scrubbed of ids;
- run 1: refused before approval (the job waits; the session's own approval through the API is refused by "Prevent self-review", or the person rejects it), with nothing imported;
- run 2: approved by the person, with the report's `executor`, `approval` and `appliedAt`, and GitHub's approval record for the run;
- who besides the person can approve `test`, as the environment's settings show it;
- leftovers in Test and in the repository, listed, not deleted.

With one GitHub identity shared by the person and the agent, "Prevent self-review" stops every approval of a run that identity started. The record says which identity started run 2 and what that shows (decisions Q10).

## Solution and publisher

None. The live check promotes the existing seed solution from M10.

## Constraints

- AGENTS.md: smallest change, new logic with tests, every write through `SafeWrite` (no new write here), Conventional Commits, no version or changelog edits, no tenant URLs, ids, emails or user names in commits.
- The pinned roles `powerops-verify` and `powerops-review-security` and `tests/eval` are untouched.
- Platforms are Windows and Linux. The Action's scripts run in bash on both runners (Git Bash on Windows); `ActionScriptTests` run on Linux only, as today.
- The plan hash covers the inputs, so a plan approved before M52 stays approved; `tool` already refuses a plan file from another powerops version.

## Assumptions

- GitHub, read 2026-11-04 (docs.github.com, "OpenID Connect reference"): the ID token has an `environment` claim with the job's environment, and its subject includes `environment:<name>` only when the job references an environment; a job requests it with `ACTIONS_ID_TOKEN_REQUEST_TOKEN` at `ACTIONS_ID_TOKEN_REQUEST_URL`, which exist only with `id-token: write`. No context or default variable holds the job's environment (decisions Q4).
- GitHub, read 2026-11-04 ("Deployments and environments"): required reviewers (up to six; one approval lets the job run; on Free, Pro and Team only for public repositories); "Prevent self-review": "users who initiate a deployment cannot approve the deployment job, even if they are a required reviewer"; by default administrators can bypass the protection rules, which can be turned off; environment secrets reach a job only after approval. The REST API approves a pending deployment (`POST /repos/{owner}/{repo}/actions/runs/{run_id}/pending_deployments`) and lists a run's approvals (`GET .../approvals`).
- Learn, checked 2026-11-04 with the Learn MCP tools. Recommendations for identity and access management (Power Platform Well-Architected, SE:05): any action must be traceable to prevent repudiation attacks. Overview of pipelines in Power Platform, FAQ: approvals for deployments come with delegated deployments. Deploy pipelines as a service principal or pipeline owner, FAQ: a delegated deployment of type service principal may only be approved by that service principal, and one of type owner only by the stage owner, so the yes sits with the deploying identity, not with the maker who asked.
- M10 approved the `test` deployment through the API with the person's GitHub credential (its live results, kept outside the repo): an agent that holds a reviewer's credential can approve, so the docs say not to give it one.

## Open questions

## Questions for your admin

## Steps

1. This plan and `decisions.md`, a fresh-context review into `review.md`, fixes, and sign-off under the delegation of 2026-10-22 once no High or Medium finding is open.
2. Rows 1 to 4 and 7, test first; then rows 5 and 6, goldens by the recipe "Update goldens".
3. Row 8.
4. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .`, the leak scan; push, PR, CI.
5. The live check of Done when 5, after the person's yes for each step that needs one; `live.md` here.
