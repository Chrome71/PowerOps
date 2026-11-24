# Decisions: gate-manage-agent

## Round 1

### Q1. Does the bundle's experimental status argue against a route?
1. No: add the route, reading only the script's file name and the verb (recommended)
2. Yes: keep the docs line that says the push passes, until the bundle is stable
Answer: 1. M49 left the route out (its Q8) because the M49 section named only `pac copilot init`, and because the bundle is experimental with no stable argument list. That second reason is about the options. The route reads none of them, because it blocks without a plan check. What it reads is the command form and the verb. Those are the most stable part: the skill, `run-eval`'s `allowed-tools`, `clone-agent` and `validate` all write `node …/manage-agent.bundle.js <verb>`, and the bundle's usage line lists the nine verbs. If a later version renames the bundle or a verb, the route misses it and the call passes, as any new write passes a deny-list until it gets a route (decision 41). A new verb in the read-only position blocks as unreadable (Q2), which fails closed. Leaving the route out costs more: publish makes the agent live for every user it is shared with, and it runs through the Bash tool, which the hooks already send to the gate.

### Q2. How does the gate read the verb?
1. Only the word after the script: `push` or `publish` is the route, a read-only verb passes, and anything else blocks as unreadable through a `manage-agent\.bundle` marker (recommended)
2. Only the word after the script, with no marker: `push` or `publish` is the route and anything else passes
3. Mimic the bundle's parser: skip `--` options and the values of the options it knows, and take the first word left
Answer: 1. The bundle takes the first argument that doesn't start with `--` and isn't an option's value, and an empty argument doesn't hold the place. So `--workspace /x push` and `"" push` run push, and option 2 would pass them. Option 3 would have to track which options take a value. A new value option in a later bundle would shift the verb, and the gate would read the wrong word and pass. Option 1 is how `solution-settings.js` is read today: `import` is a route, `template` passes, and the marker blocks every other form. It costs two things, the same as the other markers. A Bash command that only names the bundle (`grep`, `ls`, a commit message) blocks while the hooks are on, and in PowerShell every call to the bundle blocks. The skill allows only Bash, and it tells the agent not to touch the scripts.

### Q3. Must a pass hold no other marker?
1. Yes, for the seven read-only verbs and for `solution-settings.js template` (recommended)
2. Yes, for the read-only verbs only
3. No
Answer: 1. A recognized segment's text is blanked before the marker scan, and the tokenizer keeps a backtick inside a word. So a recognized pass hides a command substitution inside it. On `4c9ceee`, ``node …/solution-settings.js template --env "`pac solution import …`"`` exits 0 from `deploy gate`, and the same word after `manage-agent.bundle.js pull` exits 2, because no case recognizes that call yet. Option 3 would turn that 2 into a 0. Option 2 keeps the template hole, which is in the same branch and is closed by the same one-word condition. An unquoted `$(…)` is safe, because `(` ends the segment and the inner command is read on its own; a `$(…)` inside double quotes stays in the word, so it passes after `template` today, and the same condition closes it (amended in Round 2, R1). A pac call through the `default` case already checks its own segment for markers. Single quotes would block too, though nothing runs inside them, which fails closed.

### Q4. One route line or two?
1. One line for both verbs (recommended)
2. One line for each verb, as `pac copilot push` and `publish` have
Answer: 1. Both verbs change an agent that no plan holds, and the guidance after the line is the same, so one constant says it. `pac copilot` has two lines because pac's MCP server also has a `copilot_publish` tool that needs its own.

## Round 2 (from the review)

No new question. The review's findings amended Q3 (R1: a double-quoted `$(…)` is not safe today, and the condition closes it too); the rest changed `plan.md`, the tests and the docs (R2 to R5, R7). R6 is answered here: the `template` condition stays in this change. The repo squash-merges, so a commit of its own would not survive the merge. The PR body names the fix instead, so whoever reverts the PR sees that the revert reopens the `template` hole.

## Log

- 2026-11-04: plan started at the request of the session that started this one, after PR 88 (M49) merged as `4c9ceee`: add a `deploy gate` route for the `manage-agent` skill's push and publish, with the read-only verbs still passing
- read on 2026-11-04: AGENTS.md; BUILD-PLAN decisions 39 and 41 and the M40 and M49 rows; the M40 and M49 plan folders (Q8 of M49); `ImportCall`, `ImportCallTests`, `DeployGateCommandTests`; `docs/HANDOFF.md` (the gate section) and `docs/COMMANDS.md` (`deploy gate`)
- skills-for-copilot-studio at `9c42d3a612c6e06389b6e5b4eed579aaed1a8b63` (MIT), read through the GitHub API for the command form and verbs only: `skills/manage-agent/SKILL.md`, `skills/run-eval/SKILL.md`, `skills/clone-agent/SKILL.md`, `skills/validate/SKILL.md`, `agents/copilot-studio-manage.md`, and the verb lists of `scripts/src/manage-agent.js`, `eval-api.js` and `chat-with-agent.js`
- the `template` hole and the `pull` block were checked with the built CLI at `4c9ceee` on 2026-11-04 (Assumptions)
- depth: quick: one route in an existing branch, and its docs
- questions answered by the building session, under the user's delegation of 2026-10-22 (autonomous milestone mode), with the brief of the session that started this one
- areas not asked: users, automation, environments and ALM, and constraints; the change is to this repo, and the plan answers each or marks it not applicable
- approved 2026-11-04 by the building session under the user's delegation of 2026-10-22, for a fresh-context review
- the code and row 4's tests were written alongside the review by the building session, test first (18 `ImportCallTests` cases and the gate's write-route row failed before rows 1 to 3; the review's R1 and R2 tests were added after, and pin what the code already gives, so no red run was seen for those). Step 2 started before sign-off, as M49's did (R7)
- fresh review (a subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-11-04: 0 High, 2 Medium, 5 Low (R1 to R7); answered in `plan.md`, the tests, the docs and here (Round 2: Q3 amended). It also found that upstream `directline-chat` names a `detect-mode` verb the bundle doesn't have; that call would block as unreadable and fails upstream anyway, so nothing changes
- the same reviewer's second pass 2026-11-04, on a copy of the working tree outside the repo: R1 to R5 fixed, R6 and R7 accepted, no new finding; `PowerOps.Core.Tests` and `PowerOps.Cli.Tests` pass and `dotnet format --verify-no-changes` is clean in the copy
- signed off 2026-11-04 by the building session under the user's delegation of 2026-10-22, with no finding open (`powerops plan sign-off`); status set to done with the build in this branch, before the PR's CI
- local checks 2026-11-04: `dotnet build` (0 warnings), `dotnet test` (3,397 tests, 5 skipped), `dotnet format --verify-no-changes`, `powerops plan status .`, and the leak scan
