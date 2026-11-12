# Decisions: m89-apply-report

None of the four owner decisions of the trial-3 fix plan (Q1 to Q4, answered 2026-11-11: "One yes per deployment", "Hide by default", "Keep out of Git", "Rerun it") applies to this milestone: it adds a report field and docs, asks for no yes, prints no URL or value, and touches neither a canvas package nor a kept role. Every decision below was made by the building session (Opus) inside the fix plan's M89 entry, under the owner's direction of 2026-11-11 to decide routine choices and log them.

## Round 1 (planning)

### Q1. What time is `agentsReadAt`, and when is it null?
1. The time the read-back returned, set only when at least one agent came back `found` or `missing`; null otherwise (recommended)
2. The time the read started, set also when it failed

Decided: 1. Revised after both plan reviews (Fable 2, R1): `DriftLiveReader.ReadAgents` returns no rows and makes no call when no name is one powerops sends, so "the call returned" would date a read that never happened; the rule on the result covers that case and the failed read alike. A publication after the time the read returned is certainly not in the report, which is what the note warns about; a time for a read that failed would date a result the report doesn't have. The read is one call (`pac org who`, an `az` token, one Web API read per 50 names), so start and end differ by seconds at most, and the report is to the second.

### Q2. The wording of the 10-minute limit
1. "may stop waiting for a foreground command sooner (Claude Code's Bash tool after 10 minutes by default)", and in `HARNESS.md` the ceiling as documented, with trial 3's detached run as what was done (recommended)
2. The fix plan's "may end a foreground command sooner (10 minutes in the desktop Code tab)" and "a foreground Bash command ends after 10 minutes (2.1.322 on Linux, trial 3)"

Decided: 1. Claude Code's docs (research, Log) give the 10-minute default ceiling, `BASH_MAX_TIMEOUT_MS` to raise it, and say a foreground command that reaches its timeout is moved to the background rather than killed. Trial 3 did not let a command reach the limit: it ran the import detached because of it (record D18). So "ends after 10 minutes" would state as observed what was neither observed nor documented. The advice to the reader is the fix plan's: background or a long timeout, then wait for the exit.

### Q3. The wording of the note about publication
1. "an agent can still become published after this read (40 seconds after the import in one run): check in Copilot Studio, or with drift" (recommended)
2. The fix plan's "an import can still publish an agent after this read (40 seconds later in one run)"

Decided: 1. Learn says imported agents aren't published automatically and is silent on when a publication completes (research, Log), while trial 3 saw the row published 40 seconds after the import with no publish by the person or the session (record P8, row 42). The note reports what was seen without naming its cause, as `McpCommand.AgentFormNote` does.

### Q4. Where `agentsReadAt` goes in the JSON
1. Last, after `agents` (recommended)
2. Beside `appliedAt`

Decided: 1. An existing test holds the order of the fields from `status` to `solutions` (`DeployApplyCommandTests.cs:569`), and the field describes the `agents` list.

### Q5. The sentence that the read "has not yet run against a tenant"
1. Correct it in `COMMANDS.md` and in the schema's `agents` description, which this milestone edits anyway (recommended)
2. Leave it

Decided: 1. Trial 3 ran it (record N3), so the sentence is now false, and both places are already in this change. `docs/FOUNDATION.md:187` says the same and is left for a follow-up (Won't build), being outside the fix plan's files.

## Evidence

What trial 3, the third end-to-end trial of PowerOps (2026-11-11, at `b065f23`), observed for this milestone, taken from its private record (findings P8, P9, P10, deviation D18, case N3); no evidence file of the trial was opened, and no tenant value is copied here.

- Client: the Claude desktop app's Code tab, Claude Code 2.1.322 on Linux.
- Blocking hooks: the `PreToolUse` hooks of `docs/HANDOFF.md` ("Gate Microsoft's imports") ran `powerops deploy gate` in the deployment session, which exited 2 and blocked the gate probe's import.
- The long import: the CLI import (`deploy apply --apply --require-reviews`) ran as a detached child of a foreground command, and the session waited for its exit, because the Bash tool waits at most 10 minutes for a foreground command and `deploy apply` waits up to 60 per import. No command reached the limit.
- The agent read-back: the report said `appliedAt` at the start of the import, one agent `found` with `publishedOn: false`; a later read of the agent's row showed it published 40 seconds after the import ended, with no publish by the person or the session. `drift`, 15 to 29 seconds after the import, said not published. The cause (a publication the import started, finishing after the read) is inferred.

## Log

- 2026-11-11: planned from the M89 entry of the trial-3 fix plan (sections 3, 4 and 5) and the chain rules, in its own worktree at `b065f23`.
- read on 2026-11-11: the fix plan's M89 entry and sections 3 (opening), 4 and 5; the chain rules; AGENTS.md and CLAUDE.md; `DeployCommand.cs` (`Apply`, `Finish`, `ReadAgents`); `DeployApply.cs` (the result records); `DeployApplyReport.cs`; `deploy-apply.schema.json`; `docs/schemas/README.md` (Versioning); `COMMANDS.md` (`settings apply` Limits, `deploy apply`); `HARNESS.md`; `DeployApplyCommandTests.cs` (agents section, goldens and `Scrub`), `McpCommandTests.cs` (`WithoutTime`), `DeployApplyReportTests.cs`; `docs/ecosystem/compatibility.md` (Claude Code and Copilot Studio rows); the trial-3 record, findings P8, P9, P10, D18, N3, N6 and rows 42 and E-row summaries (no evidence file opened).
- 2026-11-11: research (a `powerops-researcher` subagent, Sonnet, open sources only, read 2026-11-11). (1) Claude Code's *Tools reference* and *Environment variables* pages: `BASH_DEFAULT_TIMEOUT_MS` defaults to two minutes and `BASH_MAX_TIMEOUT_MS` to 600000 (10 minutes), the effective ceiling being the larger of the two; `run_in_background: true` starts a background task; a foreground command that reaches its timeout is moved to the background rather than killed, unless it starts with `sleep` or background tasks are off. (2) Copilot Studio: *Configure and deploy agents from the Agent Library* says "Imported agents aren't published automatically" (section After you deploy), and *Export and import agents using solutions* says to publish an imported agent before it can be shared; the *bot* table reference defines `publishedon` as the time the agent was last published. No Learn page says whether a publication can complete after an import returns. Hence Q2 and Q3.
- depth: quick: the fix plan gives the design.
- areas not asked: users, automation, environments and ALM, screens, solution and publisher; the change is to this repo's report and docs, and the plan answers each or marks it not applicable.
- approved 2026-11-11 by the building session under the owner's standing instruction ("Start the build", 2026-11-11), for the Fable plan review and a fresh-context review.
- 2026-11-11: plan reviews of the first version. Fable (PLAN REVIEW, Fable 5.2): REJECTED, 4 findings. Fresh-context `powerops-review-plan` (Sonnet): 0 High, 3 Medium, 5 Low (`review.md`, R1 to R8). Answered in `plan.md`: the rule for `agentsReadAt` is on the read's result, with a test for names powerops doesn't send (Fable 2, R1); test (a) asserts inside one output, never across two runs (Fable 1); the JSON order test of `DeployApplyReportTests` is named (Fable 3); the `COMMANDS.md` anchors, the sources with their read dates, the 2-minute default beside the 10-minute ceiling, and "trial 3" defined in `HARNESS.md:3` and linked to the Evidence section above (Fable 4, R2, R6); the leak scan's path (R3); `AgentsReadAt` defaults to null (R4); the scrubs stay, because nothing compared reads an agent (R5); the documentation host paths dropped from this Log for page titles (R7); the note says "check in Copilot Studio, or with drift" and `COMMANDS.md` says which `drift` run (R8). Fable's WATCH kept Q2 and Q3 as decided.
- 2026-11-11: plan review round 2. Fable: APPROVED, executor sonnet for steps 2 to 4, the orchestrator for 1 and 5; its WATCH note on the Evidence section was applied (the hooks bullet now names only the deployment session's block). Fresh review second pass: R1 to R8 answered, no new finding, 0 open.
- signed off 2026-11-11 by the building session on the person's standing instruction ("Start the build", 2026-11-11; sessions sign off their own milestone plans after clean reviews), with Fable's approval and no finding open (`powerops plan sign-off`).
