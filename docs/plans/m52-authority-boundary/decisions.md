# Decisions: m52-authority-boundary

## Round 1

### Q1. Where is the yes to an import held? (D1 of the improvement plan)
1. In the MCP form in an interactive client and in the Action's protected GitHub environment; the local CLI stays person-run and unenforced (recommended)
2. Also a signed approval file that `deploy apply --apply` requires
3. In your own words
Answer: 1, answered by the person on 2026-11-04 ("Confirm both (Recommended)"), not by an agent. A file an agent can write authenticates nothing, so the yes is held where the credential is: the form is answered by the person in the client, and the Action's apply credential is reachable only from a job that a required reviewer let run. The docs say that nothing checks who ran a local `--apply`.

### Q2. What does the apply report record?
1. `appliedAt`, `executor` (`cli`, `mcp`, `action`) and `approval` (null without `--apply`, else `mode` `cli`, `form` or `action` and an opaque `receipt`), as the caller reports them (recommended)
2. Also an approver identity: the `pac auth who` account, the OS user, the typed name or the GitHub actor
3. In your own words
Answer: 1. The joint plan says provenance, not authentication. An identity powerops reads or is told would look like proof and prove nothing: the deploying account is not the approving person, and a typed name is the environment's, not the person's. The Action's receipt is its run and attempt (and its environment), which point at GitHub's own record of who approved. `executor` and `approval` are kept apart because a dry run has an executor and needs no approval.

### Q3. How does `deploy apply` learn its route?
1. `--github-run <text>` from the Action, an in-process `mcp` flag from the MCP server, and the approved hash the form already passes (recommended)
2. Environment variables (`GITHUB_ACTIONS`, `GITHUB_RUN_ID`)
3. One `--executor` option for every caller
Answer: 1. Process-wide variables would make every test depend on the test runner's environment, and a person who runs the CLI by hand inside a workflow is not the Action. The MCP server builds its own argument list, so no caller of a tool can add `--github-run`, and only the in-process entry point can pass the approved hash, which is what makes `form` true. `--github-run` is self-reported like the rest, and the docs say so; it is capped at 200 characters with no control character because it lands in a report.

### Q4. How does the Action know the job declares a GitHub environment?
1. From the `environment` claim of the job's OIDC token, requested with an audience of powerops' own (recommended)
2. An input the workflow sets
3. The deployments API, matched to the job
Answer: 1. GitHub has no context or default variable with the job's environment (the OIDC reference documents the claim, and no `job.environment` exists). An input only repeats what the workflow says. The deployments API needs `deployments: read` and matching a deployment status to the job, which is fragile. The token's claim is set by GitHub, and the protected route needs `id-token: write` anyway for its federated sign-in. The audience `powerops-environment-check` means an Azure federated credential (which expects `api://AzureADTokenExchange`) does not accept the token, and the script never prints it. The claim proves only that the job named an environment, not that it has reviewers or that the credential is bound to it; the live check records those (plan, "Environments and route").

### Q5. How strict is the `path` and `plan-file` binding?
1. The same folder after resolving both with `cd` and `pwd -P` (recommended)
2. `path` may be any folder that contains the plan file's folder
Answer: 1. The gate step analyzes `path` with the real `fail-on`, and `deploy apply` imports the zips of the plan file's folder, so anything but the same folder gates other bytes than those imported. `realpath` is not used because the Windows runner's Git Bash gets `D:\a\_temp/deploy` spellings; `cd` takes those and `pwd -P` gives one form. A zip or a missing folder as `path` fails the `cd` and is refused with the same message.

### Q6. How is `plannedBy` set?
1. `--planned-by agent|person|action` on `deploy plan`, null when not given; the MCP `deploy_plan` passes `agent` and the Action `action` (recommended)
2. `person` by default on the CLI
Answer: 1. An agent can run the CLI through a shell, so a default of `person` would be a false record. The field is outside the hash, so marking a plan doesn't void an approval, and nothing reads it to decide anything. Skills that should pass `agent` from the CLI are M54's actor marking.

### Q7. When is `appliedAt` set, and how do the goldens stay stable?
1. When an `--apply` run passed its checks and started importing (status `imported` or `failed`), in UTC to the second; the goldens' `Scrub` replaces it with a fixed valid time, and one test checks the time against the test's own clock (recommended)
2. Also for dry runs and refusals; a clock seam through `Program.Run`
Answer: 1. A refusal imported nothing, so there is no apply time to record. A fixed value in `Scrub` keeps the goldens valid against the schema's pattern without threading a clock through five signatures.

### Q8. Does the text report show the fields?
1. Yes: `Executor:` always, `Approval:` and `Applied at:` when set (recommended)
2. JSON only
Answer: 1. The Action's job summary shows the text report, and the person who runs the CLI reads text.

### Q9. Does a schema version move?
1. No: new fields keep `schemaVersion` 1 (recommended)
2. Bump `deploy-apply` and `deploy-plan` to 2
Answer: 1. `docs/schemas/README.md`: adding a field keeps the version, and the schemas don't forbid unknown properties. The plan file's version check stays as it is.

### Q10. Which live route is recorded, and what does one GitHub identity limit?
1. The Action in the test repository, pinned to this branch, because it runs the new checks; the MCP form only if the Action route is blocked (recommended)
2. The MCP form first
Answer: 1. The plugin's MCP server runs the installed `powerops`, not this branch, so its report would not have the new fields without installing the branch build over the person's tool. The Action builds the pinned commit. In a repository where the person and the agent share one GitHub identity, "Prevent self-review" refuses every approval of a run that identity started, and an administrator can bypass protection unless that is turned off. So the record states which identity started each run and who approved, and the docs say an agent must not hold a reviewer's credential.

### Q11. Does `docs/BUILD-PLAN.md` get an M52 section?
1. No: the improvement plan (M50 to M68) is not in the repo yet; the session that brings it in adds the section (recommended)
2. Yes, one row
Answer: 1. A row now would cite a plan that the repo doesn't hold, and the brief lists the doc edits this milestone makes.

## Log

- 2026-11-04: plan started at the request of the session that started this one: build M52 of the live-test improvement plan, with D1 as the person answered it
- read on 2026-11-04: AGENTS.md; the M52 section of the improvement plan, its "Not doing" list, D1 to D9 and the answers; the decisions note; the M10 live results; the M37 and M49 plan folders (layout); `DeployApply`, `DeployApplyReport`, `DeployPlanFile`, `DeployPlanReport`, `DeployCommand` (plan, apply, the dry run), `Program`, the MCP `deploy_plan` and `deploy_apply`, `PlanSignOff`; `action.yml`, `action/apply.sh`, `action/plan.sh`, `action/powerops.sh`; the two schemas and their README; `docs/HANDOFF.md` (gate), `docs/USE-WITH-AGENTS.md` (MCP), `docs/GITHUB-ACTION.md` (apply), `docs/examples/README.md` and `promote-dev-to-test.yml`; the tests `DeployApplyCommandTests`, `DeployGateCommandTests`, `McpCommandTests` (form), `ActionYmlTests`, `ActionScriptTests`, `DocsDriftTests`
- GitHub docs read 2026-11-04 through docs.github.com's article API: "OpenID Connect reference", "Deployments and environments", "REST API endpoints for workflow runs"; a web search found no documented context for a job's environment
- Learn checked 2026-11-04 with the Learn MCP tools; the pages are in Assumptions
- depth: quick: the improvement plan gives the design and D1 the routes
- questions answered by the building session under the user's delegation of 2026-10-22 (autonomous milestone mode), except Q1, which the person answered
- areas not asked: users, screens, solution and publisher; the change is to this repo, and the plan answers each or marks it not applicable
- approved 2026-11-04 by the building session under the user's delegation of 2026-10-22, for a fresh-context review
