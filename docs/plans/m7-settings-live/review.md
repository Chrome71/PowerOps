plan-hash: 78de9726ad9379af3fda73d3c11b39f4c69465d87dc5c1e88b27caab6e6d0aa1
model: claude-sonnet-5-5
fresh-context: yes

Scope of this review: `plan.md` and `decisions.md` of `m7-settings-live`, the offline half `docs/plans/m7-settings/plan.md`, `AGENTS.md`, the M7 row of `docs/BUILD-PLAN.md`, and the code the plan names (`SettingsCommand.cs`, `PacRunner.cs`, `PlanFacts.cs`, `Solution.cs`, `Settings/DeploymentSettings.cs`, `SettingsReport.cs`, `SolutionParser.cs`, `OutputSchemaTests.cs`, the seed fixture and `docs/schemas/README.md`). `powerops plan status` reports `approved`, `openQuestions` 2, `review: pending`, no violations. `powerops plan facts` was NOT run and `pac` was never called: contacting the tenant is forbidden for this review, so there are no environment facts (item 1's overlap check against tenant solutions was not done; the plan creates no solution). Two Microsoft Learn pages were re-read (import a solution; the `pac power-automate` reference) because findings R1, R3 and R6 depend on them.

Checklist items that do not apply: 1 (no solution or publisher is created; the seed `pops` solution is reused), 2 (the plan keeps connection ids in a settings file outside the repo and environment variable values in the same file; nothing is hard-coded), 7 (no app, no row counts; the one volume point is the flow list, see R6). Items 3 and 4 (licence, DLP) are handled the way the plan chose in Q6: assumed for the test, left to the two admin questions; not raised again.

What checked out: the offline half exists and `Validate` returns `SettingsProblem` lists that step 1 can reuse; `PlanFacts.Run` already takes the pac runner as a function and reads `Friendly Name:` from `pac org who`, so the "runner as a function" test pattern is real; `Workflow` already holds `Id` (normalised to lower case without braces), `Name`, `Category` and `StateCode`, so "no model change" holds; the seed flow is `StateCode` 1 in the v1 export; the `OutputSchemaTests` `Map` prefix `settings-apply` does not collide with `settings-validate`; `docs/schemas/README.md` has the table the plan says to extend; exit codes 0/1/2/3 match AGENTS.md and the validate command.

## Findings

### High

#### R1. Live done-when items 2 and 3 cannot pass as written if the seed is already installed
- Area: 9. Testability, 5. Route
- Finding: The plan's own Learn source says that when a flow already exists in the target, importing an update does not change its state, and that the import restores the export state only for a flow that is new. The live check imports the seed into "the developer test environment", where the seed was built and is probably still installed. Then item 3 (seed exported with its flow off, imported over an installed, running flow) leaves the flow on, so powerops reports `on` and exits 0, not the 1 the done-when expects; and item 2 proves nothing about the import turning a flow on, because it was on already. Step 7 and the Goal never say that the seed must be removed first or that a second environment is used. The page also warns that a solution cannot be imported into an environment where it already exists in the maker portal, so what `pac solution import` does on an existing solution (update, upgrade, error) is also unstated.
- Suggested fix: Add to Goal and Step 7 how each live run starts: the user deletes the seed solution from the test environment (or uses a second, empty environment) before items 2 and 3, and say what is run in what order (item 3 first, then item 2, or reinstall between). Add the update-over-existing case as its own live check with the expected result (state unchanged), since that is what real users hit.
- Resolution: fixed (Q8: live items 2 to 4 start from a removed seed, components included; the re-import over an installed seed is item 3)

#### R2. Nothing ties `--apply` to the environment the user approved, and the route to Test and Prod is not stated
- Area: 5. Dev to Test to Prod route, 6. Managed or unmanaged, agent safety
- Finding: The target is whatever environment the pac profile has selected when the command runs; the plan rules out `--environment`. A dry run shows the environment name, the user says yes, and the later `--apply` call re-reads `pac org who`. If the profile was switched in between (another terminal, another agent, a CI step), `--apply` imports into a different environment, possibly Prod, with no check and no gate. The report is also only written at the end, so during a 60-minute import nothing tells the agent or the user where it is writing. The plan has a "Route of the code" but no route for the tool's own use: who may point `--apply` at Test or Prod, who approves, and the plan never asks the Dev/Test/Prod question (not in the decisions log). The only agent rule is a sentence in Users and Environments ("`--apply` only after the user says yes"), which nothing enforces or documents for agents (README, AGENTS.md, the skills).
- Suggested fix: Decide it in the plan and add a Step for it. The smallest option inside AGENTS.md's "no unrequested options": `--apply` prints the target environment name as its first line, before the import starts (not at the end), and the plan adds a rule to the README paragraph and the `powerops-implement`/agent docs: dry run, show the environment line to the user, `--apply` only after a yes, never against an environment the user did not name. If that is judged too weak, record the alternative (an `--environment` name that must equal `pac org who`, or passing the resolved environment to every pac call) as a decision in `decisions.md`.
- Resolution: fixed (Q7: `--apply` needs `--target <name>` and every later pac call is pinned by environment id; agent rule in Environments and route and Step 6)

### Medium

#### R3. The dry-run flow predictions are wrong for updates, and the import cycles the flows
- Area: 9. Testability, 5. Route
- Finding: Automation step 3 says "on at export means expected on after import". Per the same Learn page that holds only for a flow that does not yet exist in the target. For an update the flow keeps its target state, and the page also says an import turns a solution's flows off and on again, so a running flow is interrupted. The dry run therefore can tell an agent "expected on" when the result will be whatever the target has, and the agent has no warning that a live flow is about to restart.
- Suggested fix: Make step 3 read what is already there with a read-only pac call (the plan already uses `pac solution list` in `plan facts`; `pac power-automate list-cloud-flows --workflow-id <id>` per flow is another). Report per flow "new: expected <export state>" or "exists: state unchanged, now <state>". Add the flow-restart note to the README paragraph. Add tests for both cases.
- Resolution: fixed (the dry run reads each flow and reports `exists` or `new` with the expected state; flow cycling noted in Constraints and the README step)

#### R4. A failed import's pac output can put tenant data into the report, against the plan's own rule
- Area: privacy of committed and shared output
- Finding: Automation step 4 includes "the last lines of `pac`'s output" in the report on failure. The plan's Data section says the report never prints environment URLs or ids or user names. pac error text routinely names the environment, user or connection (the existing `PlanFactsTests` fake error shows a sign-in message that carries an email address). The report is JSON for agents and Step 7 says results go into the PR description, so a failed live run (which Step 7 forces on purpose, with a fake `ConnectionId`) will paste that text into a public repo. `PacResult` also carries stdout only, so if pac writes its errors to stderr there is nothing to show.
- Suggested fix: State what is shown: only the pac exit code and a fixed hint, or the last lines after redacting URLs, GUIDs and email-like text (with a test that proves it). Say that Step 7 results in the PR description are written by hand without that text. If stderr is wanted, `PacResult` and `PacRunner` need a `Stderr` field (an additive change to a class the plan says is reused unchanged).
- Resolution: fixed (Q9: exit code and fixed hint only; pac text to stderr; PR results written by hand)

#### R5. `PacRunner` cannot do the 30-second reads and the 60-minute import through one runner, and a timeout kills pac mid-import
- Area: 9. Testability, agent safety
- Finding: `PacRunner` takes one timeout per instance (default 30 s), and the command takes a single `Func<IReadOnlyList<string>, PacResult>`, as `PlanFacts.Run` does. The plan needs a 60-minute wait for step 4 and short waits for steps 2 and 5, but names no mechanism. On timeout `PacRunner` kills the process tree; killing the `pac` client does not cancel the import running in the service, so the plan's "rerunning is the recovery" can meet an import still in progress or half done, and the report should say that rather than "failed". Separately, an agent that calls `powerops settings apply --apply` from a shell tool will usually hit the tool's own command timeout long before 60 minutes, and a kill of powerops at that point leaves the same state with no report.
- Suggested fix: Add to Step 3 how the timeout is chosen (a second `PacRunner` for the import, or a timeout argument on the delegate) and what the timeout message says ("the import may still be running in the environment; check before rerunning"). Add a README line telling agents to run `--apply` in the background or with a long timeout. Add a CLI test for the timeout wording.
- Resolution: fixed (runner function takes a timeout; 60 minutes for the import; "may still be running" message; README line for agents)

#### R6. The flow check rests on a Preview command that is documented as an admin tool, and the plan reads the whole list
- Area: 8. Security roles, 7. Data volume, 9. Testability
- Finding: Learn describes `pac power-automate` as inspecting flows "as an admin". The plan does not say what role the importing user needs for `list-cloud-flows`, and a maker without that role may get an empty or partial list, which powerops would report as `missing` for every flow (exit 1, a false alarm). The command also accepts `--workflow-id` and `--environment`, which the plan does not use: reading the whole environment's flow list returns every user's flows (more tenant data than needed, and large in a big environment, with no mention of paging), while one call per solution flow with `--workflow-id` returns only what is needed. Step 1 (a live capture by the user) gates Steps 2 to 6, and it records only one flow with its state on; the shapes for off, suspended and several flows are then invented in the Core tests rather than seen.
- Suggested fix: Name the role needed for the check in Constraints and in the README. Use `--workflow-id` per solution flow (and say so in Automation and the tests) so a partial list cannot look like `missing`, or say how `missing` is told apart from "cannot see". Step 1 should capture the flow list once with the flow on and, when the user can, once with it off, and say that the suspended shape is unobserved and tested only with a hand-made sample.
- Resolution: fixed (`--workflow-id` per flow; capture of on, off and unknown id; suspended sample hand-made; role as an assumption and an admin question)

#### R7. Managed or unmanaged, and the import's own defaults, are not stated
- Area: 6. Managed or unmanaged
- Finding: The plan imports "an exported solution zip" with no other options, and says nothing about whether the zip is managed or unmanaged, in which environment each kind is right, or what pac's default does to existing unmanaged customisations or to components removed from the solution (only the last is mentioned, in Assumptions). `SolutionLoader` already knows `Managed`; the dry run could show it. An unmanaged import into Test or Prod is the usual mistake an ALM tool should flag, and an agent has no way to see it before `--apply`.
- Suggested fix: Add `managed` (from `Solution.Managed`) to the dry-run report and the JSON, and say in the plan which kind the live check uses and why. Record in Constraints that powerops does not judge the kind per environment (or add a warning), and list the pac import defaults that matter (overwrite, publish) in the README paragraph.
- Resolution: fixed (`managed` in the dry run and JSON; live zips unmanaged and why; powerops does not judge the kind)

#### R8. The first live done-when ("changes nothing in the environment") has no way to be checked
- Area: 9. Testability
- Finding: Item 1 says the dry run "changes nothing", but neither the offline tests nor Step 7 say how that is shown. The offline test ("no import call made") proves the code path with a fake; live, nothing compares the environment before and after. Also unlisted: a test that a pac failure in step 2 (not found, not signed in, timeout) gives exit 3 with no import call, a test for an unreadable settings file (exit 3) versus a missing one (exit 2), and a JSON golden for the failed-import and `suspended` cases (the Steps list only a dry run and a post-import golden).
- Suggested fix: In Step 7 item 1, have the user run `pac solution list` (names and versions only) before and after the dry run and record "unchanged" in the PR. Add the missing CLI tests and the failed-import and `suspended` goldens to Steps 3 and 4.
- Resolution: fixed (`pac solution list` before and after the dry run; the missing CLI tests and goldens added to Steps 4 and 5)

### Low

#### R9. The dry run prints absolute paths, which can carry a user name
- Area: privacy of output
- Finding: The dry run prints "the exact argument list of the import", which contains the zip and settings file paths. Those are absolute paths in many agents' work (a home folder with the user's name) and the plan says the report never prints user names. A golden with such a path would also break on another machine.
- Suggested fix: Print the paths as the user typed them, state that in the plan, and use relative fixture paths in the goldens (as the offline golden does).
- Resolution: fixed (paths printed as typed; relative paths in goldens)

#### R10. Who may import and who owns the flows is not named
- Area: 8. Security roles
- Finding: The plan notes that the importing user owns the imported components and must be able to use the connections, but names no role (importing needs an administrator or customiser role) and does not say whether CI runs this under a service principal, whose profile then owns the flows and would need the connections shared with it. The Questions for your admin cover licence and DLP but not role or ownership.
- Suggested fix: Add a line under Constraints for the role the pac profile needs, and one admin question: which account or service principal runs the import in Test and Prod, and who owns the connections.
- Resolution: fixed (owner after import in Constraints; account, role and connection owner as an admin question)

#### R11. The plan lists Learn pages as URL paths
- Area: tenant data in committed files
- Finding: The Sources list holds public Microsoft Learn page paths. They are not tenant data and match the house style of the rule docs, so this is noted only because the checklist asks for every URL to be reported. No tenant URL, GUID, email or person's name was found in `plan.md` or `decisions.md`.
- Suggested fix: None needed; keep tenant values out of Step 7 results as the plan says.
- Resolution: accepted (public Learn paths, not tenant data)
