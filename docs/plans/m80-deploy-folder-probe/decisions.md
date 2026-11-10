# Decisions: m80-deploy-folder-probe

## Round 0 (the 1.0 fix chain)

The person's instruction of 2026-11-09 to the session running the 1.0 fix chain: "Once 75 is finished with its PR continue with the other M sessions in new sessions each until you wrap up M83 autonomously." Two of the three answers that followed apply to this milestone:

### Q1. For M76 and M79–M83, who signs off each milestone's plan before it is built?
1. Sessions sign off: each session signs its own plan once the Fable plan review approves it and `powerops-review-plan` leaves no finding open, and records that it did so on the person's instruction of 2026-11-09 (recommended)

Answer: "Sessions sign off (Recommended)" (answered by: person, 2026-11-09).

### Q2. Who merges each milestone's PR once all 8 CI checks pass, starting with M75's open PR #128?
1. Sessions merge: each session rebases on main, reruns its checks, opens its PR and squash-merges it when green, one PR at a time (recommended)

Answer: "Sessions merge (Recommended)" (answered by: person, 2026-11-09).

## Log

- 2026-11-09: started by the 1.0 fix chain session for M80, wave A beside M76, M79, M81 and M82, on `main` `278b136` (M75 merged). The fix plan's section 3 ("M80"), read with sections 4 and 5, is the base.
- read on 2026-11-09: the fix plan (sections 1 to 7); the chain rules; M75's hand-over; `DeployCommand.cs` (`Run`, `ReadPlanFile`, the gate: `RunGate`, `GateDecide`, `GateCheckCall`); `docs/HANDOFF.md`, `docs/INSTALL.md`, `docs/USE-WITH-AGENTS.md`, `docs/COMMANDS.md` (`deploy plan`, `deploy apply`); `HandoffDocTests`, `DeployGateCommandTests`, `ActionScriptTests` and `Tools` (how script tests run and skip on Windows); `.gitattributes`; `McpCommand.cs` (`deploy_plan`'s `save` is a file name in the folder) and `action/plan.sh` (saves in the folder), so only the CLI can save elsewhere.
- depth: quick: the fix plan gives the design.
- areas not asked: users, automation, screens, solution and publisher, environments and ALM; the change is to this repo, and the plan answers each or marks it not applicable.
- 2026-11-09: the fix plan's Claude Code claims sent to a `claude-code-guide` subagent (Sonnet): the claim that hooks load only at session start is contradicted by the current docs (a file watcher normally picks up hook edits); the `PATH` claim is not documented; `gitCommitSha` is not documented. Recorded in Assumptions; HANDOFF states the trial's observation and the docs' claim instead of a cause.
- 2026-11-09: refinements to the fix plan's text, each kept to what it names: (1) the warning fires when the plan file's folder is not the deploy folder itself, so a subfolder warns too (`deploy apply` reads only the plan file's own folder), and says "not saved in" instead of "saved outside"; (2) it names the settings files with the zips, because `deploy apply` reads both from that folder; (3) the probe's first command is `command -v pac`, and the import runs only if it names the stand-in, because the profile the shell is set up from may put the real `pac` first; (4) the stand-in is deleted after the probe, because `deploy plan` and `deploy apply` call `pac` through the same `PATH`; (5) the marker is written beside the stand-in, not in the current folder, so its place doesn't depend on the agent's `cd`.
- approved 2026-11-09 by the building session for the two plan reviews; sign-off follows Round 0 Q1
- fresh review (a general-purpose subagent following `powerops-review-plan`, Opus, fresh context) 2026-11-09: 1 High, 2 Medium, 5 Low (R1 to R8) in `review.md`, answered in `plan.md`: the warning names no path (R1); the README deletes the marker before each probe and reads the result from the call's own output (R2); row 4 gives the real reason for the block (`--environment is missing`) and its test pins the line and no `pac` or target call (R3); the one-folder-up test is spelled from `Folder` (R4); R5 and R6 are gone with the `PATH` step (the stand-in is called by its absolute path); row 7 adds only the `git rev-parse HEAD` check (R7); Assumptions names the pages, not the host (R8)
- plan review under the model routing (a `fable-reviewer` subagent, Fable, fresh context) 2026-11-09: REJECTED with seven findings, all answered in `plan.md`: the warning names no path and the hygiene test stays (1); the probe calls the stand-in by its absolute path, so the real `pac` is unreachable, and the `PATH` step, the `command -v pac` check, the deletion step, the `PATH` assumption and the `SessionStart` line are dropped; this replaces refinements (3) and (4) above (2); the marker is deleted before each probe, the verdict is read from the call's output, and the README says a block is the pass and the call is not reworded or retried (3); row 4's reason and test (4); `DocsDriftTests` checks no general link, so `HandoffDocTests` and `GateProbeTests` check the new links and the step 7 sentence, and the step order is stated (5); the test copies with `/bin/cp` (6); the guessed cause is removed, the settings page joins HANDOFF's `Sources:`, row 7 adds only the comparison, and row 8 words the `--version` check for a build from a clone (7)
- the same reviewer's second pass 2026-11-09: R1 to R8 resolved; one new Low, R9 (building in Dev "in another session" while the shared hooks are on would block its imports, and turning them off would turn them off for the deployment session too), answered in row 6: Dev building happens before the hooks go in or after they come out, and the probe runs again after a hook edit
- plan review under the model routing, second pass 2026-11-09: APPROVED; step 2 to Sonnet, step 3 to Opus, step 4 to Sonnet after step 3; builds and tests of parallel steps run one at a time in this worktree
