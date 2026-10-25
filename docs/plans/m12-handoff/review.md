plan-hash: c5a78f6a56ee6a3421da14b5825edb1cc5c9aa83b281d33ca2dc75e2e1dc4669
model: claude-sonnet-5-5
fresh-context: yes

The plan-hash was updated after the text fixes for R1 to R13 below (the practice of the M7 to M11 reviews). Reviewed hash: 48df0c8c22754bb61fc452838adbb1b3a9a411acbe19d2e5425d2f788af42b87. The fixes changed Goal (done-when), Data (the files on the route), Automation (the command test, the stray-file check, the router commit, bash 3.2), Environments and route, Constraints, Assumptions and Steps 1 to 6.

Scope: `plan.md` and `decisions.md` of `m12-handoff` (approved, depth quick, review `pending`, 0 open questions), read against `AGENTS.md`, `docs/BUILD-PLAN.md` (the M12 row, the Phase 3b conflict checks, build-order task 32, decision 16, the verification record), `README.md` (Planning and Usage), `skills/powerops-implement/SKILL.md`, `skills/powerops-plan/SKILL.md`, `tests/smoke/skills-smoke.sh`, `src/PowerOps.Cli/Program.cs` and the tests in `tests/PowerOps.Cli.Tests/`.

What I ran: `powerops plan status`, `powerops <command> --help` for every command the page will name, `deploy nope --help`-style experiments on the built CLI, and the existing smoke script under bash 3.2.57 in a container (`bash -n`, the `set -u` empty-array case, the menu and `done_when` expressions). I did not run `pac`, `az` or `powerops plan facts`, and nothing touched a tenant. I read the public files of the pinned `power-platform-skills` commit (a network read, no tenant): `canvas-app`, `app-builder`, `genpage`, `build-flow`, `debug-flow`, `manage-flows`, `create-site`, `import-solution`, `deploy-pipeline`, `create-code-app` and model-apps' `scripts/solution-settings.js` exist at that commit, `manage-flows` enables a flow with `publish_flow`, and `solution-settings.js` has `template` and `import`. The Dataverse-skills `dv-solution` name was not rechecked.

Checklist items that do not apply: 1 (no solution or publisher is created; the page uses a fake `ContosoApp`), 2 (no flow or app is built; the only hard-coded values in play are the page's examples, which are fake), 3 and 4 (no connector or table is added; the page does not choose connectors), 7 (no data volume), 8 (no role is created or assigned). Items 5 and 6 apply to the page's route text (R10), item 9 is R1 to R3, R7 and R8, and item 10 is clean (0 open questions, nothing under `Questions for your admin`, the Log lists the areas approved without). The plan and `decisions.md` hold no URL, GUID, email or person's name (`plan status` reports none either).

## Findings

### High

#### R1. The command test passes for a command that does not exist, and the plan's own negative test would fail
- Area: 9. Testability
- Finding: Automation says the test runs `Program.Run([..words, "--help"])`, expects exit `0`, and that "an unknown subcommand is a parse error there (exit `2`)", with a second test that `deploy nope` exits `2`. Checked on the built CLI: that is false. With `--help` present, System.CommandLine 2.0.13 does not report the parse error. `powerops deploy nope --help`, `plan nope --help`, `deploy apply nope --help` and `nope --help` all print a help page and exit `0`; the same words without `--help` exit `2`. The Assumptions check ("a known path followed by `--help` exits 0") proved only the known-path half. So a renamed or removed command still passes the test, which is the one thing the done-when item exists to catch, and the planned `deploy nope` test fails as written. Running the words without `--help` is not an option, because it would execute the command (`skills install` would write into the current folder).
- Suggested fix: Keep `--help` and the no-change-to-Program rule, but decide existence from the help text, whose layout is stable (`deploy --help` lists `apply` and `plan` under `Commands:`, indented two spaces). For each word list, walk from the root: run `Program.Run([..prefix, "--help"])`; if the output has a `Commands:` section, the next word must appear in it as a command name, else fail; if it has none, the remaining words are arguments and the walk stops. Test the helper on inline text: a page with `powerops deploy nope` must fail, one with `powerops deploy apply <plan file>` must pass. Correct the Assumptions bullet and Q2's wording ("the parser decides"), or take Q2's option 2 (expose the root command).
- Resolution: fixed (Q2 changed to option 2: `Program.Root` exposes the tree the CLI already builds and the test walks its subcommands; Automation says why `--help` cannot be used; the helper's negative cases are tests)

### Medium

#### R2. The manual run has no step, the done-when is weakened, and the BUILD-PLAN row would still claim it
- Area: 9. Testability; 5. Dev to Test to Prod route
- Finding: The BUILD-PLAN M12 row requires one manual run in Claude Code (a signed-off plan's build step goes to `app-builder` in Dev, the import goes through `deploy apply`), recorded in the verification record. The plan's done-when accepts "as run, or as not run with the reason", but Steps has no step that asks the user for the Dev and Test yes (Q1 says "this session asks") or that performs the run; step 4 only "records" it. It also gives no pass criteria and does not say whether Microsoft's skills are installed on the machine that would run it. If the answer is no, M11's precedent was to edit the milestone row, its decision and the build-order task to say "not verified" (see the M11 plan); step 4 here only adds a verification-record line and marks task 32 built, so the M12 row would keep claiming a run that did not happen.
- Suggested fix: Add a step before step 4: ask the user, by role (Dev, Test), for a yes to the build and to the import. On yes, run it with stated criteria (the build step is handed to `app-builder`, the only import is `deploy apply --apply`, no Microsoft import tool is used) and record the skills' commits and the `pac` version. On no, step 4 also edits the M12 row's third done-when bullet and task 32 to read "not run" with the reason, as for M11, and the plan's Goal says the milestone is built with the manual run not done, not done.
- Resolution: fixed (Steps 4 asks the user by role and states the run and what is recorded; without a yes, Steps 5 marks the M12 row, task 32 and the verification record "not run", as M11; Goal's done-when says so)

#### R3. Smoke case 4 passes even if the fake router is never loaded
- Area: 9. Testability
- Finding: Case 4 passes whenever the agent drafts a plan and changes nothing outside `docs/plans/`. If the router is not discovered (wrong folder for one agent, a front-matter error, Codex not reading it, or the agent skipping it), case 4 is case 3 with another sentence and still passes, and the pass says nothing about PowerOps beating a router. The plan also never shows that the router, alone, makes an agent write under `src/`: "a request the router takes shows up as files outside `docs/plans/`" is an untested assumption.
- Suggested fix: In step 3, add a one-time control in each agent: the same request in a repo with only the router (no PowerOps skills) must write files under `src/`. In the script, assert that the router file is in the committed tree and that its `name` equals its folder. Record both results in `decisions.md`.
- Resolution: fixed (Steps 3 adds a control run per agent with only the router installed, which must write outside `docs/plans/`, recorded in `decisions.md`; no in-script assert, because the script writes and commits the router itself and a failed commit stops it under `set -e`)

#### R4. The `pac` version check from the Phase 3b conflict checks is missing
- Area: 9. Testability (the M12 row's conflict checks); 5. route
- Finding: BUILD-PLAN lists "`pac` versions" among the conflict checks each milestone plan rechecks (an agent's `pac` can be older than any PowerOps was tested on; decision 16(d); FOUNDATION section 8). The plan rechecks the import paths, the sign-in state and the unpack rule, but not this one, and the Set up list says only "`pac` signed in". Microsoft's model-apps accepts `pac` newer than 2.10.0; PowerOps is tested with 2.13.1 and the example workflows pin 2.14.1.
- Suggested fix: Add one line to Set up: tested with `pac` 2.13.1, the examples pin 2.14.1, older builds untested, run `pac` to see the version. Add to Assumptions that the check was redone.
- Resolution: fixed (Set up names the tested and pinned `pac` versions; Assumptions record that the conflict checks were redone)

#### R5. Steps 6 to 8 do not say how the settings file and the plan file reach the next step
- Area: 9. Testability; 5. Dev to Test to Prod route
- Finding: Step 6 ends with a settings file checked by `settings validate <solution> --settings <file>`. Step 7 runs `deploy plan` and step 8 `deploy apply <plan file>`. But `deploy plan` reads a settings file only as `<zip name>.settings.json` beside each zip in the folder it is given, and `deploy apply` needs a plan file written by `deploy plan --save` (the Action writes `powerops-plan.json` in `deploy-folder`). The steps say neither, and not where the managed zips in that folder come from (the local export of step 3, or the CI export in `docs/examples/`). A reader who follows the page gets a settings file the plan ignores and no plan file.
- Suggested fix: In steps 6 and 7 name the file `<zip name>.settings.json` beside the managed zip. Add `--save <plan file>` to step 7, or say the Action writes it. Say where the zips come from. The page should also say that settings files hold connection ids and values and stay out of the repository.
- Resolution: fixed (Data names the files and where the managed zips come from; steps 6 to 8 use `<zip name>.settings.json`, `--save` and the plan file; settings files stay out of Git, built in CI as `docs/examples/` does)

#### R6. Step 5 says the Action's PR comment gives the diff, but with `target` set it gives the plan
- Area: 9. Testability (a claim the README contradicts)
- Finding: Step 5 offers "or the Action's PR comment" for the diff, and step 7 uses the Action's `deploy-folder` and `target`. README (Pull request comment): with `target` set the comment holds the `deploy plan`; the comment holds the `diff` only without `target`, for a single solution `path`; for a fork PR the diff goes to the job summary. A repository set up as step 7 describes gets no diff comment.
- Suggested fix: Reword step 5: run `powerops diff`, or run the Action without `target` for the comment. In step 7 add that with `target` the comment is the plan, so the diff needs its own step or run.
- Resolution: fixed (step 5 says the comment holds the diff only without `target`)

### Low

#### R7. The test's assertions and scan rules leave it loose
- Area: 9. Testability
- Finding: "Asserts the page names at least one command" passes if the page loses nine of its ten commands, and the done-when item "`README.md` links to it" has no check. The scan rules are not fixed: the match must be case-sensitive (the page's prose will say "PowerOps plans, checks"), limited to backtick spans and fenced blocks (a lowercase `powerops deploy apply is the only import` in plain prose would give a word list with prose words in it), and must not cross a line break. The plan also does not say the page is read through `Golden.RepoRoot` and `Path.Combine`, as AGENTS.md asks.
- Suggested fix: Assert the expected word lists are all found: `analyze`, `diff`, `settings generate`, `settings validate`, `deploy plan`, `deploy apply`, `drift`, `plan status`, `plan sign-off`, `skills install`. Assert that `README.md` contains `docs/HANDOFF.md`. State the scan rules in Automation.
- Resolution: fixed (Automation states the scan rules and the path helpers; the test asserts the flow's commands and the README link)

#### R8. The stray-file check is skipped exactly when a router wins
- Area: 9. Testability
- Finding: In the current script the "files changed outside docs/plans" check sits inside `if [ -n "$(plan_dir)" ]`. If the router wins and writes under `src/` with no plan drafted, the only message is "no drafted plan", and the stray files, the signal case 4 exists to catch, are not shown. The plan moves the checks into `check_fast_path` without saying where this one goes.
- Suggested fix: Run the `git status` check outside the `if` and print the files whenever any exist.
- Resolution: fixed (the `git status` check runs whether or not a plan was drafted)

#### R9. Two script details that would make runs fail or lie
- Area: 9. Testability
- Finding: (a) `new_repo` commits right after the install. The router must be written before that commit (a parameter or a second commit); written later it is untracked and shows as a change outside `docs/plans/`, so case 4 would fail every run for the wrong reason. (b) The script finds the repository two levels above `$0`, so the temporary copy of step 3 must sit in `tests/smoke/`, not in a scratch folder, and must be deleted before the commit. On bash 3.2: the existing script passes `bash -n` under 3.2.57, the unguarded `"${resume[@]}"` fails under `set -u` and the plan's `${resume[@]+"${resume[@]}"}` works, and I found no other bash 4 construct, so the plan's list is complete for the current script.
- Suggested fix: Say in Automation how the router is written before the commit, and in step 3 where the temporary copy lives.
- Resolution: fixed (the router gets its own commit after `new_repo`; the temporary copy lives in `tests/smoke/` and is deleted before any commit)

#### R10. Two page claims are wider than the docs
- Area: 5. Dev to Test to Prod route; 6. Managed or unmanaged
- Finding: (a) "`powerops deploy apply` is the only import": decision 16(c) limits this to a flow PowerOps gates, and README documents `settings apply --apply` as a one-solution import with no plan. (b) Step 3 says managed "for Test and Prod", but steps 7 to 10 name only Test, and Phase 3 and the examples cover Dev to Test.
- Suggested fix: Say "in a flow PowerOps gates" and name `settings apply` as the command for use outside it. For Prod, say the same steps repeat with its own plan and approval, or limit the page to Dev to Test.
- Resolution: fixed (the page says "in a flow PowerOps gates" and names `settings apply` for use outside it; the page covers Dev to Test and says Prod repeats the steps with its own approval)

#### R11. Set up and the sign-in note leave out parts of what the docs say
- Area: 9. Testability
- Finding: (a) "install the PowerOps skills with `powerops skills install`" assumes the CLI is on the path; the skills need it (`compatibility`) and README Install says how. (b) The install note says to run the install in each child project but not to start the agent there; an agent started at the workspace root may not load them (not checked offline). (c) The Sign-in state note leaves out two points of the conflict check: `plan facts` and `settings apply` read pac's current environment, and the `az` account that `drift` and the deploy commands use can differ from pac's.
- Suggested fix: Add a link to README Install, one sentence on where to start the agent, and the two sign-in points.
- Resolution: fixed (Set up links README Install and says to start the agent in the child project; the sign-in note adds `plan facts`, `settings apply` and the `az` account)

#### R12. One passing run each is a thin and machine-dependent sample
- Area: 9. Testability
- Finding: Case 4 tests a model's choice between skills, and the done-when needs one passing run per agent. The Assumptions say a user-scope router in Claude Code on this machine "only adds to what case 4 tests", unchecked. The weekly runner and other machines have none, so a local pass is not what CI would see; and a real router can load further skills, whose tool calls are denied by `--allowedTools` in these runs.
- Suggested fix: In `decisions.md` record, per run, the agent versions and whether a user-scope router was loaded. If a run fails, record the reply before any rerun.
- Resolution: fixed (Assumptions: `decisions.md` records each run's agent version, whether a user-scope router was present, and a failed reply before any rerun)

#### R13. The FOUNDATION section 8 items that M12 answers are not updated
- Area: 10. Unanswered areas
- Finding: FOUNDATION section 8 has two Phase 3b items tied to this milestone: which skill an agent loads when two match ("M12's smoke-test case ... checks it"), and what an empty settings entry does ("until then the M12 hand-off flow says to remove an entry"). Step 4 touches only BUILD-PLAN, so after the build the first item still says "no run has checked that it wins".
- Suggested fix: Add to step 4: annotate the first item with the case 4 result (or leave it open with the reason if the run did not pass), and point the second to the page's step 6.
- Resolution: fixed (Steps 5 updates both FOUNDATION section 8 items)
