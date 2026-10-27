# Hand-off to Microsoft's skills

Microsoft's [power-platform-skills](https://github.com/microsoft/power-platform-skills) and [Dataverse-skills](https://github.com/microsoft/Dataverse-skills) build apps, flows, sites and tables. PowerOps plans the work, checks what they built, plans the import and imports it. When both are installed, an agent follows the steps below: Microsoft's skill builds in Dev, and PowerOps does everything after that. In a flow that PowerOps gates, `powerops deploy apply` is the only import. `powerops settings apply` stays the one-solution import without a plan, for use outside such a flow.

## Set up

- Install the `powerops` CLI ([Install](../README.md#install)). The skills call it.
- Add the PowerOps skills to the repo with `powerops skills install` ([Planning](../README.md#planning)). In a workspace whose own tooling rewrites `.claude/skills/`, for example a sync script that deletes and recreates the folder, run it in each child project, not at the workspace root, and start the agent there: the next rewrite removes skills installed at the root.
- Install Microsoft's skills as their READMEs say.
- Sign in `pac` with `pac auth create`. PowerOps is tested with `pac` 2.13.1, and the example workflows pin 2.14.1; older builds are untested (`pac` prints its version). `drift`, `deploy plan` and `deploy apply` also need the Azure CLI signed in with `az login` ([Usage](../README.md#usage)).

## The steps

The steps go from Dev to Test. For Prod, repeat steps 7 to 9 with Prod as the target, with its own plan and approval.

1. **Plan.** The `powerops-plan` skill turns the request into a plan in `docs/plans/<name>/`, `powerops-review-plan` reviews it in a fresh context, and `powerops plan sign-off docs/plans/<name>` signs it off. `powerops plan status` shows where each plan stands. Asked to build without a signed-off plan, the skills draft one and ask only the route, and the publisher prefix if the repo doesn't show it.
2. **Build in Dev.** `powerops-implement` does the plan's steps in order and hands the build work to the Microsoft skill for the product: `canvas-app` (canvas-apps), `app-builder` or `genpage` (model-apps), `build-flow` (power-automate), `create-site` (power-pages) or `create-code-app` (code-apps). Build in Dev only, never in Test or Prod.
3. **Export, and unpack into XML only.** Export the solution from Dev, unmanaged for the source and managed for Test, and unpack the unmanaged zip into an XML folder (`Other/Solution.xml`) that you commit:

   ```sh
   pac solution export --environment <dev> --name ContosoApp --path ContosoApp.zip
   pac solution export --environment <dev> --name ContosoApp --path deploy/ContosoApp_managed.zip --managed
   pac solution unpack --zipfile ContosoApp.zip --folder src/ContosoApp
   ```

   Never unpack into a folder in the YAML layout that Dataverse Git integration commits (`solutions/*/solution.yml`). `pac solution unpack` writes XML only, and pointed at a YAML folder with `--allowWrite --allowDelete` it added XML files beside the YAML and deleted others ([FOUNDATION section 8](FOUNDATION.md#8-research-topics-still-open)). Dataverse-skills' `dv-solution` unpacks with `pac` too, so the same rule applies to it. The managed zip can come from this export or from an export in CI, as in [docs/examples/](examples/README.md).
4. **Analyze and test.** `powerops analyze src/ContosoApp` reports what would break the import or a run, and `powerops test src/ContosoApp --tests flowtests` runs the flow tests that `powerops-implement` wrote or updated for each flow the plan changed. Fix errors and failing tests in Dev and export again. In CI, the Action's `tests` input runs the same tests as a required check ([Merge gate](../README.md#merge-gate-flow-tests)).
5. **Diff.** `powerops diff <old> src/ContosoApp --format markdown` compares the base branch's copy of the solution with the new one, for the pull request. The PowerOps Action puts this diff in its PR comment only when `target` is not set; with `target` (step 7) the comment holds the plan, so the diff then needs its own step.
6. **Settings.** One tool writes the deployment settings file: `powerops settings generate src/ContosoApp`, or model-apps' `solution-settings.js template`. Fill in each connection id and value, save it beside the managed zip as `deploy/ContosoApp_managed.settings.json` (the `<zip name>.settings.json` that `deploy plan` reads), and check it with `powerops settings validate src/ContosoApp --settings deploy/ContosoApp_managed.settings.json`. To keep a variable's default or current value, remove its entry instead of leaving `"Value": ""`, which `pac solution import` rejects. The file holds the target's connection ids and values, so keep it out of Git and build it in CI from repository variables, as [docs/examples/](examples/README.md) does.
7. **Plan the import in CI.** `powerops deploy plan deploy --target <test> --save deploy/powerops-plan.json` runs on the pull request (the Action's `deploy-folder` and `target` inputs write the same plan file), so the reviewer sees the import order, installs, upgrades, deletions, settings problems and flow states before approving.
8. **Import.** After approval, `powerops deploy apply deploy/powerops-plan.json --target <test> --apply`. It plans again and refuses when anything changed since the reviewed plan. An agent runs it only after the user said yes to that environment in the session; a pull request approval is not that yes. Don't import with a skill's or an MCP tool's import: pac's MCP server (`solution_import`), model-apps' `solution-settings.js import`, power-pages' `import-solution` or `deploy-pipeline`, or Dataverse-skills' `dv-solution`. None of them checks a reviewed plan.
9. **Confirm.** `powerops drift --solution ContosoApp --env <dev> --env <test>` compares the solution in both environments.
10. **A flow is off.** `deploy apply` reports each cloud flow's state after the import, but it can't turn a flow on, because `pac` can't. Use power-automate's `debug-flow` to find out why and `manage-flows` to turn the flow on (its publish operation). That is a write in that environment, so ask first. Then run `drift` again.

## Sign-in state

pac's MCP server uses the same `pac` auth profiles, and its tools can select another environment or profile in the middle of a session. `drift`, `deploy plan` and `deploy apply` pass the environment on every `pac` call, so a changed selection doesn't move them. `plan facts` and `settings apply` read pac's current environment, and `settings apply` refuses when it no longer matches `--target`. A switched auth profile changes the identity of every `pac` call, and the `az` account that `drift`, `deploy plan` and `deploy apply` use can differ from it.
