---
status: signed-off
review: done
signed-off: 2026-10-24
signed-off-hash: c5a78f6a56ee6a3421da14b5825edb1cc5c9aa83b281d33ca2dc75e2e1dc4669
environment: null
depth: quick
supersedes: null
---

# M12: hand-off to Microsoft's skills

## Goal

Finish BUILD-PLAN milestone M12 (build-order task 32, release v0.12.0): a page that tells an agent with Microsoft's skills installed the order of the work, with Microsoft's skill building in Dev and PowerOps checking, planning and importing; the install note for workspaces that rewrite `.claude/skills/`; a test that every `powerops` command on the page exists; and a fourth smoke-test case with a routing skill installed beside the PowerOps skills. The skill wording of M12 (`powerops-implement` steps 3 and 7, the `powerops-plan` description) is already done in f3da510 and is not changed here.

Done when:

- `docs/HANDOFF.md` exists with the set-up notes and the ten steps under Steps, and `README.md` links to it.
- `tests/PowerOps.Cli.Tests/HandoffDocTests.cs` passes, and its helper reports a made-up command name.
- `tests/smoke/skills-smoke.sh` has case 4; `bash -n` is clean under bash 3.2 and 5; case 4 (with case 3, which shares its checks) passes in Claude Code and Codex in one local run each, and a control run shows the fake router alone makes each agent write outside `docs/plans/`.
- The M12 manual run is done with the user's yes (Steps 4), or, without that yes, the BUILD-PLAN M12 row, build-order task 32 and the verification record say "not run" with the reason, as M11's did. In that case M12 is built with its manual done-when item not run, not done.
- `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` and `powerops plan status` are clean.

## Users

- Coding agents (Claude Code, Codex) in a repo that has the PowerOps skills and Microsoft's power-platform-skills or Dataverse-skills, and the people who review their pull requests.

## Data

No new data. The page names Microsoft's skills by the names read at power-platform-skills `aed70bf` (2026-10-24): `canvas-app` (canvas-apps), `app-builder` and `genpage` (model-apps), `build-flow`, `debug-flow` and `manage-flows` (power-automate; `manage-flows` turns a flow on with its publish operation, `publish_flow`), `create-site` and `import-solution` and `deploy-pipeline` (power-pages), `create-code-app` (code-apps), and model-apps' `scripts/solution-settings.js` (`template` and `import`). From Dataverse-skills `1966192`: `dv-solution`. From pac: the MCP server's `solution_import`. Example names are fake (`ContosoApp`, `<dev>`, `<test>`).

Files on the page's route: the unmanaged export is unpacked into `src/ContosoApp/` (committed); the managed export goes to `deploy/ContosoApp_managed.zip`, made by the user's own build or the CI export in `docs/examples/`; its settings file is `deploy/ContosoApp_managed.settings.json`, the name `deploy plan` reads beside the zip; `deploy plan --save deploy/powerops-plan.json` writes the plan file that `deploy apply` reads (the Action writes `powerops-plan.json` in `deploy-folder`). A settings file holds connection ids and values of the target, so it is built in CI from repository variables, as `docs/examples/` does, and not committed.

## Automation

**Command test.** `HandoffDocTests` reads `docs/HANDOFF.md` (path from `Golden.RepoRoot` and `Path.Combine`) and `README.md`.

- Scan: every case-sensitive `powerops` followed by one space and one or more lowercase words (`[a-z][a-z-]*`, each after a single space, never across a line break), over the whole page. A word list ends at the first token that is not such a word (an option, a `<placeholder>`, a path). Prose writes "PowerOps", so the lowercase name appears only in commands; `powerops-plan` and other skill names do not match.
- Walk: `Program` gets `internal static RootCommand Root(...)`, the tree that `Run` already builds (moved, not changed; `Run` calls it). For each word list the test walks from the root: while the current command has subcommands, the next word must be one of their names, else the list is reported; at a command without subcommands the remaining words are arguments. A helper returns the reported word list or null.
- Asserts: no word list on the page is reported; the page names each command of the flow (`plan status`, `plan sign-off`, `skills install`, `analyze`, `diff`, `settings generate`, `settings validate`, `deploy plan`, `deploy apply`, `drift`); `README.md` contains `docs/HANDOFF.md`; and the helper reports `deploy nope` and `nope` and accepts `deploy apply`, so the check can fail.

`--help` does not work for this: System.CommandLine 2.0.13 reports no parse error when `--help` is present, so `powerops deploy nope --help` exits `0` (checked on the built CLI); running the words without `--help` would execute them.

**Smoke case 4.** In `tests/smoke/skills-smoke.sh`:

- The checks of case 3 move into a function, `check_fast_path <request>`: the first reply drafts a plan under `docs/plans/` and has numbered route options; after the answer `1 1` there is an A/B/C menu (in either reply) and `status` is still `draft`. The `git status` check for files outside `docs/plans/` runs whether or not a plan was drafted, so a router that wins and writes files is reported with the files. Case 3 calls it with its current request.
- Case 4 (`router`): `new_repo` (PowerOps skills, first commit), then the script writes a fake routing skill, `platform-router`, into the agent's skill folder (`.claude/skills/` for Claude Code, `.agents/skills/` for Codex) and commits it, so it is part of the tree and not a change. Its text is our own, not Microsoft's or any workspace's: the description claims every Power Platform build request ("Use whenever a task touches canvas apps, model-driven apps, cloud flows, Power Pages or Dataverse tables"), and the body says to start building at once by writing the app's definition files under `src/`, so a request that the router takes shows up as files outside `docs/plans/`. The request names no skill: "Build a model-driven app for tracking site visits by our field engineers." Then `check_fast_path` runs.
- Bash 3.2 (macOS): the empty `resume` array in `agent_turn` is expanded as `${resume[@]+"${resume[@]}"}` (under `set -u`, bash before 4.4 treats an empty array as unbound; checked under bash 3.2.57, where the current script fails on the first Claude Code call). No `${x,,}`, no `mapfile`, no `declare -A`. The header comment says so.

The weekly workflow is unchanged; its API key secrets are still not set (the smoke test runs locally or by hand).

## Screens

None.

## Environments and route

- The build touches no environment. The smoke runs make model calls only; the agents may run `powerops plan facts`, which reads pac's current environment (names only), as in the M3 runs.
- The manual run from the M12 done-when needs a write to Dev (`app-builder` builds a model-driven app there) and an import into Test through `powerops deploy apply --apply`. AGENTS.md allows `deploy apply --apply` only after the user said yes to that environment in this session, and `powerops-implement` step 7 asks before any environment write. This session asks the user by role (Dev for the build, Test for the import); without a yes it records the run as not run (decisions Q1).
- The page covers Dev to Test; Prod repeats the plan, review and apply steps with Prod as the target and its own approval.

## Solution and publisher

None created. The page uses a fake `ContosoApp`.

## Constraints

- Open sources only: Microsoft Learn, the MIT repos at the commits above, and PowerOps' own observed output. No decompiling. No text copied from Microsoft's skills into the fake router.
- No tenant URLs, environment ids, emails or user names in any file.
- No new `powerops` option, command or dependency. No change to the skills. `Program` changes only by moving the tree into `Root`.
- `skills-smoke.sh` stays bash 3.2 compatible.

## Assumptions

- `powerops plan facts` was not run: M12 creates no solution, so there is nothing for facts to answer; `environment` stays null.
- The Phase 3b conflict checks were redone for the page: one writer per flow (step 8 names the import paths without a plan check, and one tool writes the settings file), `pac` versions (Set up: tested with 2.13.1, the examples pin 2.14.1, older builds untested), sign-in state (the page's note), and unpack into XML only (step 3).
- The install note is generic: it names no local workspace or script. It comes from a read of a local workspace whose sync script deletes and recreates `.claude/skills/` at its root (BUILD-PLAN Phase 3b basis); child projects keep their own `.claude/skills/`. The page also says to start the agent in the child project (where an agent started at the workspace root looks for skills was not checked).
- In Claude Code on this machine a user-scope router skill is also loaded during the smoke runs; the weekly runner has none. `decisions.md` records, per run, the agent version and whether that router was present, and a failed run's reply before any rerun.

## Open questions

## Questions for your admin

## Steps

1. Write `docs/HANDOFF.md`, "Hand-off to Microsoft's skills": one paragraph on who does what (Microsoft's skill builds in Dev; PowerOps plans, checks, plans the import and imports; in a flow PowerOps gates, `powerops deploy apply` is the only import, and `settings apply` stays the one-solution import without a plan for use outside such a flow), a "Set up" list (the CLI from README Install; the PowerOps skills with `powerops skills install`, and the install note: in a workspace whose own tooling rewrites `.claude/skills/`, run it in each child project and start the agent there, not at the workspace root, because the next rewrite removes skills installed at the root; Microsoft's skills as their READMEs say; `pac` signed in, tested with 2.13.1, the examples pin 2.14.1, older builds untested; `az` for `drift`, `deploy plan` and `deploy apply`), then the ten steps:
   1. Plan: `powerops-plan`, a review with `powerops-review-plan`, `powerops plan sign-off docs/plans/<name>`; `powerops plan status` shows the state. A build request without a signed-off plan drafts one first.
   2. Build in Dev only, through `powerops-implement`, which hands each step to the Microsoft skill for the product (the list under Data).
   3. Export from Dev (unmanaged for the source, managed for Test) and unpack the unmanaged zip into an XML folder (`Other/Solution.xml`) with `pac solution unpack --zipfile ... --folder ...`; never into a YAML-layout folder (`solutions/*/solution.yml`), with the FOUNDATION section 8 observation; `dv-solution` unpacks with `pac` too. The files as under Data.
   4. `powerops analyze` on the folder; fix errors in Dev and export again.
   5. `powerops diff <old> <new> --format markdown` for the PR. The Action's PR comment holds this diff only when `target` is not set; with `target` (step 7) the comment is the plan, so the diff then needs its own step.
   6. Settings: `powerops settings generate` or model-apps' `solution-settings.js template` (one tool writes the file), filled in, saved as `<zip name>.settings.json` beside the managed zip and kept out of Git, then `powerops settings validate`; remove an entry instead of leaving `"Value": ""`, which `pac solution import` rejects.
   7. `powerops deploy plan deploy --target <test> --save deploy/powerops-plan.json` in CI, the plan reviewed in the PR (the Action's `deploy-folder` and `target`, and the example in `docs/examples/`).
   8. After approval, `powerops deploy apply deploy/powerops-plan.json --target <test> --apply`; not pac's MCP `solution_import`, model-apps `solution-settings.js import`, power-pages `import-solution` or `deploy-pipeline`, or `dv-solution`, which check no reviewed plan.
   9. `powerops drift --solution ContosoApp --env <dev> --env <test>` to confirm.
   10. A flow that is off after the import: `deploy apply` reports it and cannot turn it on; use `debug-flow` to find why and `manage-flows` to turn it on (a write in that environment: ask first), then `drift` again.
   Then a "Sign-in state" note from the Phase 3b conflict checks: pac's MCP server shares pac's auth profiles and can switch environment or profile mid-session; `drift`, `deploy plan` and `deploy apply` pass the environment on every call; `plan facts` and `settings apply` read pac's current environment, and `settings apply` refuses when it no longer matches `--target`; a switched profile changes the identity of every call; the `az` account can differ from pac's. Link the page from `README.md` (the Planning section, with the install note in one sentence, and the Documentation list).
2. Move the command tree into `Program.Root` and add `tests/PowerOps.Cli.Tests/HandoffDocTests.cs` (Automation). See it fail with a wrong command name on the page, then pass.
3. Change `tests/smoke/skills-smoke.sh` (Automation): `check_fast_path`, case 4, the empty-array guard, the header comment. Run `bash -n` under bash 3.2 (the local `bash:3.2` image, no network) and 5. Then, from a temporary copy in `tests/smoke/` without cases 1 and 2 (the script finds the repo from its own folder; the copy is deleted before any commit): run cases 3 and 4 in Claude Code and in Codex, and once per agent a control with only the router installed (no PowerOps skills) and the same request, which must write files outside `docs/plans/`. Record each run in `decisions.md`.
4. Ask the user in this session for a yes to the manual run: the build in Dev and the import into Test, named by role. On yes: a signed-off plan for a small model-driven app, its build step handed to `app-builder` in Dev, export, `analyze`, `deploy plan`, then `deploy apply --apply` into Test as the only import, with no Microsoft import tool; record the skills' commits and the `pac` version. Without a yes, record it as not run (Step 5).
5. Record M12: in the BUILD-PLAN verification record (the smoke results, and the manual run as run or not run with the reason); mark build-order task 32 built with a link to this plan, and the M12 row's manual done-when item "not run" with the reason if so; in FOUNDATION section 8, add the case 4 result to the "which skill an agent loads" item (it stays open for Copilot and for real routing skills), and point the "empty settings entry" item to the page's step 6.
6. Run `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `bash -n tests/smoke/skills-smoke.sh` and `powerops plan status`. Commit in Conventional Commits; no push.
