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
