---
name: powerops-implement
description: Use when asked to build, implement or change a Microsoft Power Platform app, flow, table or solution in this repo. Checks for a signed-off PowerOps plan first and, without one, drafts a plan instead of writing code.
license: Apache-2.0
compatibility: Needs the powerops CLI on PATH. pac is optional.
---

# powerops-implement

Build what a signed-off plan says, and nothing else. Until the gate below passes, write no code and change no solution.

## 1. Find the plan

1. If `powerops` is not found, tell the user to install PowerOps as the Install section of its README says (https://github.com/Chrome71/PowerOps#install), and stop.
2. Run `powerops plan status --json` from the repo root. It lists every plan under `docs/plans/`. The exit code covers all plans, so read the entry of the plan you pick, not the exit code.
3. Pick the plan that matches the request. If more than one fits, ask the user which. If none fits, there is no plan.

## 2. The gate

The gate passes only when both are true:

- the plan's `status` is `signed-off`, or `implementing` (resuming);
- its `violations` list is empty.

Warnings do not block. Mention them (for example "review.md is stale").

If the gate fails, write no code and change no solution:

- **`done`**: the plan was already built. For new work on it, hand over to `powerops-plan` to reopen and re-sign the plan (its section 10); do not draft a second plan.
- **No plan, or not signed off** (`draft`, `approved`): hand over to the `powerops-plan` skill in fast-path mode with the user's request. Say what you are doing in one line.
- **A violation "changed since sign-off"**: explain that the plan body was edited after sign-off. The user either re-signs through `powerops-plan` or reverts the edits. Do not choose for them.
- **Any other violation, or a malformed header** (a `malformed header` violation): show the message and tell the user how to fix it. Do not edit the plan yourself.

## 3. Implement

1. Set `status: implementing` in the plan header. Edit that one line only. Skip this if it already says `implementing`. In that case read the commit messages: don't redo a step that is already committed, and when every step is, go to section 4.
2. Read `plan.md` in full, especially `Environments and route`, `Solution and publisher` and `Steps`.
3. Do the `Steps` in order, one at a time. For the build work use Microsoft's power-platform-skills, by what the step builds: Canvas apps `canvas-apps` (`canvas-app`), model-driven apps `model-apps` (`app-builder`, `genpage`), cloud flows `power-automate` (`build-flow`), Power Pages `power-pages` (`create-site`), code apps `code-apps-preview` (`create-code-app`; its folder is `code-apps`). If no such skill is registered, look for a routing skill, or read the plugin's `SKILL.md` from a checkout of microsoft/power-platform-skills. If you find neither, continue without them.
   Tables, columns, publishers and solutions go to `dataverse` (`dv-metadata`, `dv-solution`). No Microsoft skill is verified to create an environment variable or a connection reference inside a solution (model-apps' `create-connection-reference.js` has no solution target), so propose a maker-portal step in the plan's solution for those. For a raw Dataverse Web API call use the documented `MSCRM.SolutionUniqueName` header; the Dataverse skills also show an undocumented `MSCRM.SolutionName`, so check that the component landed in the solution.

   Before the first build step, check what the skill needs and report what is missing instead of switching route: a Python 3 interpreter (`python` or `python3`) with the Dataverse skills' packages, and the Dataverse CLI. Ask the user whether they are signed in to each of Dataverse, `pac`, `az`, FlowAgent's connection management and Canvas Authoring: one sign-in does not cover another, and you do not run `pac` or `az` to find out. If the plan's `environment` is null, ask the user to name the Dev environment before any tenant read or live check. Give the skill the plan's `environment` and its `Solution and publisher`. Confine tenant reads to that environment, and do not list other environments. Stop and report when a tool can't create or save inside that solution. Depending on the environment's setting and the operation, a flow save without a solution context can add the flow to the maker's preferred solution, even one already in another unmanaged solution, so check the component's membership after each save and report when you can't. Do not change the preferred solution.

   A Canvas Authoring compile isn't a save. In one run, after a compile Save stayed greyed out and the status read "All changes are saved" with an older time, and a reload then lost the compile. After each compile, look at the saved time. If it didn't move, change a property and undo it to turn Save on, save, or ask the user to, and confirm that the time moved. Reload only after that, never to check a compile that isn't saved yet.

   In one run, two compiles of a whole screen with its gallery crashed Studio while the tool reported success; the screen first with the gallery alone after it didn't. The cause isn't known. In that run, modern controls also showed as empty blocks after a compile until a reload.

   In one run, the Python client's table create put the table in the preferred solution although `solution=` was passed. Check each table's membership, and ask before moving one.
4. After each step, check it (build, test or inspect, as fits the step). Run the check and the commit as separate commands, so a failed check can't be committed (in one run a single command did both, and the step was committed after its check failed). Then commit with the step number in the message, for example `feat: step 2, add the Visit table (plan site-visits)`. Never push unless asked.
5. Keep progress in commit messages. Do not write it to `plan.md`.
6. Never edit the plan body. If a step cannot be done as written, or the plan must change, stop and go back to `powerops-plan`. A changed plan needs a new sign-off.
7. Never write to an environment (import, deploy, publish, create) without the user's explicit go-ahead for that step. Ask each time. This covers every tool, Microsoft's too: the `pac` MCP server's `solution_import`, model-apps `solution-settings.js import`, power-pages `import-solution` and `deploy-pipeline`, and the Dataverse skills' `dv-solution`. To promote exported solutions to another environment, use `powerops deploy plan` and then `powerops deploy apply`, not those tools. Use the plan's `environment` only as the named target. Never run `powerops settings apply --apply` or `powerops deploy apply --apply`, or call the MCP tool `deploy_apply` with `apply: true`, unless the user said yes to that environment in this session. Run each command without `--apply` first and show the user the target line. `settings apply` takes the name they approved as `--target`. `deploy apply` takes the environment's id or URL as `--target` (in the dry run too) and prints its friendly name on the target line; the MCP tool asks the user to type that name in a form.
8. Do not add features, tables, flows or settings that are not in the plan.

## 4. Finish

1. Run `powerops analyze` on each solution you changed (a path to an unpacked folder or an exported zip). Report the findings. If it could not run, say "not verified" and why.
2. Before you open a pull request, for each cloud flow the plan adds or changes, write or update its tests once the solution is exported and unpacked into the repo, and commit them with that export. To export, follow `dv-solution`, and use what the user told you about `pac`; do not run `pac` to find out. If `pac` isn't available, export with the Web API's `ExportSolution` and keep the zip, which `powerops analyze` and `powerops test` both read, and tell the user it isn't unpacked. Tests are `*.flowtest.json` files in the folder that the repo's PowerOps Action `tests` input names, else `flowtests/` at the repo root: one test for each path through the flow that the plan describes, with a mock for each connector action (see the PowerOps flow tests page, https://github.com/Chrome71/PowerOps/blob/main/docs/FLOW-TESTS.md). Use fake values in triggers and mocks; never paste a real run's raw outputs without replacing tenant URLs, ids, emails and names. Run `powerops test <solution> --tests <folder>` and report the result. A test that does not pass blocks the merge in CI: fix the flow or the test, and never delete or weaken a test to make it pass. If a test is `unsupported`, tell the user and let them decide. If the solution is not in the repo yet, say "not verified" and why.
3. For each cloud flow and app the plan adds or changes, check its final saved version in Dev, through Microsoft's skill for the product or by asking the user; never run `pac` or `az` for these checks. A read-only check needs no go-ahead. A check that runs a flow or writes data follows step 7 of section 3, and you agree the test rows with the user first. Fire each flow's real trigger once with a synthetic input and confirm that a run started and did what the plan says. For a canvas app, say who saves and who publishes, ask before publishing, load the published version fresh and check the planned controls in their states. If the published app shows an old-version banner, select Refresh on it: in one run the first load after a republish was still the old version. Confirm that each changed component is in the plan's solution, and report any other unmanaged solution it is in, apart from the system Default and Active solutions and sharing the plan allows. If a check can't run, say "not verified" and why. A later save or publish voids the checks it affects.
4. Set `status: done` in the plan header, one line only, when steps 1 to 3 passed or the user accepted each remaining gap by name. Otherwise leave `status: implementing` and list the failed checks and the checks that did not run, separately.
5. Tell the user what was built, the commits, the analyze and test results, and anything that differs from the plan body.

## Per agent

- **Claude Code and others:** the steps are the same. Ask the user plainly in chat before any write to an environment, and wait for the answer.
- **Copilot autopilot:** do not take any environment write without a person's yes. If no person can answer, skip that step and report it.
