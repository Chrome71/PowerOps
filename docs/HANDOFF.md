# Hand-off to Microsoft's skills

Microsoft's [power-platform-skills](https://github.com/microsoft/power-platform-skills) and [Dataverse-skills](https://github.com/microsoft/Dataverse-skills) build apps, flows, sites and tables. PowerOps plans the work, checks what they built, plans the import and imports it. When both are installed, an agent follows the steps below: Microsoft's skill builds in Dev, and PowerOps does everything after that. In a flow that PowerOps gates, `powerops deploy apply` is the only import. `powerops settings apply` stays the one-solution import without a plan, for use outside such a flow.

## Set up

- Install the `powerops` CLI ([Install](../README.md#install)). The skills call it.
- Add the PowerOps skills to the repo with `powerops skills install` ([Planning skills](USE-WITH-AGENTS.md#planning-skills)). In a workspace whose own tooling rewrites `.claude/skills/`, for example a sync script that deletes and recreates the folder, run it in each child project, not at the workspace root, and start the agent there: the next rewrite removes skills installed at the root.
- Install Microsoft's skills as their READMEs say.
- Sign in `pac` with `pac auth create`. PowerOps is tested with `pac` 2.13.1, and the example workflows pin 2.14.1; older builds are untested (`pac` prints its version). `drift`, `deploy plan` and `deploy apply` also need the Azure CLI signed in with `az login` ([Command reference](COMMANDS.md#drift)).

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
4. **Analyze and test.** `powerops analyze src/ContosoApp` reports what would break the import or a run, and `powerops test src/ContosoApp --tests flowtests` runs the flow tests that `powerops-implement` wrote or updated for each flow the plan changed. Fix errors and failing tests in Dev and export again. In CI, the Action's `tests` input runs the same tests as a required check ([Merge gate](GITHUB-ACTION.md#merge-gate-flow-tests)).
5. **Diff.** `powerops diff <old> src/ContosoApp --format markdown` compares the base branch's copy of the solution with the new one, for the pull request. The PowerOps Action puts this diff in its PR comment only when `target` is not set; with `target` (step 7) the comment holds the plan, so the diff then needs its own step.
6. **Settings.** One tool writes the deployment settings file: `powerops settings generate src/ContosoApp`, or model-apps' `solution-settings.js template`. Fill in each connection id and value, save it beside the managed zip as `deploy/ContosoApp_managed.settings.json` (the `<zip name>.settings.json` that `deploy plan` reads), and check it with `powerops settings validate src/ContosoApp --settings deploy/ContosoApp_managed.settings.json`. To keep a variable's default or current value, remove its entry instead of leaving `"Value": ""`, which `pac solution import` rejects. The file holds the target's connection ids and values, so keep it out of Git and build it in CI from repository variables, as [docs/examples/](examples/README.md) does.
7. **Plan the import in CI.** `powerops deploy plan deploy --target <test> --save deploy/powerops-plan.json` runs on the pull request (the Action's `deploy-folder` and `target` inputs write the same plan file), so the reviewer sees the import order, installs, upgrades, deletions, settings problems and flow states before approving.
8. **Import.** After approval, `powerops deploy apply deploy/powerops-plan.json --target <test> --apply`. It plans again and refuses when anything changed since the reviewed plan. An agent runs it only after the user said yes to that environment in the session; a pull request approval is not that yes. Don't import with a skill's or an MCP tool's import: pac's MCP server (`solution_import`), model-apps' `solution-settings.js import`, power-pages' `import-solution` or `deploy-pipeline`, or Dataverse-skills' `dv-solution`. None of them checks a reviewed plan. Where you need one of them, run it only behind the hooks in [Gate Microsoft's imports](#gate-microsofts-imports).
9. **Confirm.** `powerops drift --solution ContosoApp --env <dev> --env <test>` compares the solution in both environments.
10. **A flow is off.** `deploy apply` reports each cloud flow's state after the import, but it can't turn a flow on, because `pac` can't. Use power-automate's `debug-flow` to find out why and `manage-flows` to turn the flow on (its publish operation). That is a write in that environment, so ask first. Then run `drift` again.

## Gate Microsoft's imports

The PowerOps skills import only with `deploy apply`, but that is an instruction to the agent: nothing in the client stops pac's MCP tool `solution_import` or the import script of a Microsoft skill. Where you need one of them, a `PreToolUse` hook makes it wait for PowerOps. Before the call, the hook runs the dry run of step 8, `powerops deploy apply` without `--apply`, which plans again and fails when the plan is blocked or anything changed since the reviewed plan. The hook then exits 2. Claude Code and Codex both block a call when a hook exits 2, and give its stderr (here the PowerOps report) to the agent as the reason.

In Claude Code, add the hooks to the repo's `.claude/settings.json`, with your target in place of `<test>`:

```json
{
  "hooks": {
    "PreToolUse": [
      {
        "matcher": "mcp__pac-cli__solution_import",
        "hooks": [{ "type": "command", "onFailure": "block", "command": "powerops deploy apply \"$(git rev-parse --show-toplevel)/deploy/powerops-plan.json\" --target <test> >&2 || exit 2" }]
      },
      {
        "matcher": "Bash",
        "hooks": [{ "type": "command", "onFailure": "block", "command": "grep -qE 'pac +solution +import|solution-settings\\.js.* import |ImportSolution|encode-solution-file\\.js|pac +pipeline +deploy|DeployPackageAsync' || exit 0; powerops deploy apply \"$(git rev-parse --show-toplevel)/deploy/powerops-plan.json\" --target <test> >&2 || exit 2" }]
      }
    ]
  }
}
```

- The first matcher is pac's import tool. Its name carries the name you gave pac's MCP server: `mcp__pac-cli__solution_import` for `pac-cli`, the name in Learn's `claude mcp add-json` example. For a server that a plugin provides, it is `mcp__plugin_<plugin>_<server>__solution_import`.
- The second runs before every shell command, but checks only a command whose text matches what Microsoft's skills run to import: `pac solution import` (Dataverse-skills' `dv-solution`), `solution-settings.js ... import` (model-apps), `encode-solution-file.js` and `ImportSolutionAsync` (power-pages' `import-solution`), and `pac pipeline deploy` and `DeployPackageAsync` (power-pages' `deploy-pipeline`).
- `onFailure: "block"` (Claude Code 2.1.295 or later) also blocks the call when the hook can't start or times out (after 600 seconds by default). Without it, both let the call through.

Codex reads the same hooks from `.codex/hooks.json` in the repo, or from `~/.codex/hooks.json`, with two changes. In its tool names, every character of the server name other than a letter, a digit or `_` becomes `_`, so the first matcher is `mcp__pac_cli__solution_import`. And it has no `onFailure`, so leave it out: a hook that can't start or times out lets the call through. Codex runs a new or changed hook only after you trust it in `/hooks`, and a repo's `.codex/` hooks only in a trusted project.

What the hooks don't do:

- The check proves that the reviewed plan still matches the zips, the settings files and the target. It doesn't see what the Microsoft tool imports, into which environment or in which order, so with the hooks on, an import into Dev needs a current reviewed plan too.
- An import that changes the target makes the next check refuse until a new plan is reviewed, so import a plan of several solutions with `deploy apply`.
- The shell hook reads only the command text. A script that runs `pac` itself, or a request written another way, gets past it, and so does power-pages' `create-site`, which imports its template solutions during the build through `import-template-solution.js`. Both clients warn about this: Claude Code's Bash rules are not a security boundary, and Codex calls hooks "a useful guardrail, not a complete enforcement boundary". To stop pac's import outright, deny it instead: `"permissions": { "deny": ["mcp__pac-cli__solution_import"] }` in Claude Code's settings, or `disabled_tools = ["solution_import"]` in the `[mcp_servers.pac-cli]` table of Codex's `config.toml`.

Sources: [Claude Code hooks](https://code.claude.com/docs/en/hooks), [Claude Code permissions](https://code.claude.com/docs/en/permissions), [Codex hooks](https://learn.chatgpt.com/docs/hooks), [Codex MCP](https://learn.chatgpt.com/docs/extend/mcp), [Use the PAC CLI MCP server](https://learn.microsoft.com/power-platform/developer/howto/use-mcp), Codex's code at [`rust-v0.163.0`](https://github.com/openai/codex/tree/rust-v0.163.0) (`codex-rs/hooks/src/events/pre_tool_use.rs`, `codex-rs/hooks/src/engine/command_runner.rs`, `codex-rs/config/src/hook_config.rs`, `codex-rs/codex-mcp/src/connection_manager_tests.rs`), the skills at power-platform-skills [`0a7bf47`](https://github.com/microsoft/power-platform-skills/tree/0a7bf475552a79ee26f54500ff715b047455caf8) and Dataverse-skills [`108370e`](https://github.com/microsoft/Dataverse-skills/tree/108370ef5d61358835af476c22c555dadefc55d0), and the tool list of pac's MCP server as Claude Code showed it. verified: 2026-10-28

## Sign-in state

pac's MCP server uses the same `pac` auth profiles, and its tools can select another environment or profile in the middle of a session. `drift`, `deploy plan` and `deploy apply` pass the environment on every `pac` call, so a changed selection doesn't move them. `plan facts` and `settings apply` read pac's current environment, and `settings apply` refuses when it no longer matches `--target`. A switched auth profile changes the identity of every `pac` call, and the `az` account that `drift`, `deploy plan` and `deploy apply` use can differ from it.
