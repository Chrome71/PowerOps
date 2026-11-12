---
status: signed-off
review: done
environment: null
depth: quick
supersedes: null
signed-off: 2026-11-11
signed-off-hash: 16fe843eeed1dc889e3db6af8c0778a3df635d9a406ca14a49e234fc4665420f
---

# M89: `deploy apply` says when it read the agents; long imports and this client's limits

## Goal

In trial 3, the third end-to-end trial (2026-11-11), the apply report said an imported agent was `found` with `publishedOn: false`, and the agent's row, read later, carried a published time 40 seconds after the import ended (trial record P8, N3). The report gave no time for that read, and `docs/COMMANDS.md` doesn't say the value can change after it. The same run needed a detached process for the import, because Claude Code's Bash tool waits at most 10 minutes for a foreground command and `deploy apply` waits up to 60 per import; `COMMANDS.md` gives the background advice only under `settings apply` (P9, D18). `docs/HARNESS.md` marks this client's blocking hooks and its timeout "Not checked", though the gate's hooks blocked in that run (P10). This is milestone M89 of the trial-3 fix plan.

Done when:

1. The apply report has `agentsReadAt`: the UTC time, to the second and in `appliedAt`'s format, at which the agent read-back returned. It is set only when at least one agent came back `found` or `missing`, and null otherwise: a dry run, a refusal, a failed or timed-out first import, a run with no agent to read, a read-back that failed, and agents whose names are none that powerops sends (all `not-read`, so no call is made). `deploy-apply.schema.json` declares it (required, the same pattern as `appliedAt`), and `schemaVersion` stays 1. In the JSON it is the last property, after `agents`.
2. When `agentsReadAt` is set, the text report has the line `note: agents read at <time>; an agent can still become published after this read (40 seconds after the import in one run): check in Copilot Studio, or with drift.` after the agent lines and before the summary; otherwise no such line.
3. `COMMANDS.md` `deploy apply` says what `agentsReadAt` is, carries the same sentence with its sources, and says: "It can take up to an hour (powerops waits 60 minutes for each import): a client's shell tool may stop waiting for a foreground command sooner (Claude Code's Bash tool waits at most 10 minutes by default, and 2 when no timeout is passed; *Tools reference*, read 2026-11-11), so run it in the background or with a long command timeout and wait for its exit." (decisions Q2) Its sentence that the read "has not yet run against a tenant" says it has run once (trial 3), and the schema's `agents` description the same. "Trial 3" in the public docs links to this plan's `decisions.md`, whose Evidence section holds what the trial observed (R2).
4. `HARNESS.md`'s row for the desktop Code tab: Blocking hooks "exit 2 blocks (2.1.322 on Linux, trial 3)"; Long-call timeout: the MCP call's timeout is not checked; a foreground Bash command has a 10-minute ceiling by default (`BASH_MAX_TIMEOUT_MS`, its docs), so trial 3 (2.1.322 on Linux) ran the CLI import detached and waited for its exit (decisions Q2). `HARNESS.md:3`, which lists the kinds of evidence a client fact may name, adds "the third end-to-end trial of 2026-11-11 (trial 3)", linked to this plan's `decisions.md`.
5. The tests of Data row 6 are written first and fail, then pass. `dotnet test --filter "DeployApplyCommandTests|OutputSchemaTests"` passes, and the regenerated `deploy-apply*.json` goldens hold `"agentsReadAt": null`.
6. `dotnet build` (0 warnings), `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m89-apply-report` and the leak scan (`bash reports/leakscan.sh origin/main <branch>`, run from the main checkout, where it is an untracked local tool) are clean.

### Won't build

- Polling the agent until it is published, or a wait before the read. The fix plan rejected it: the read stays one read.
- A second read, a read before the import, or a change to what `publishedOn` means.
- A `schemaVersion` bump: an added field keeps it (`docs/schemas/README.md`, Versioning).
- Changes to the MCP form's text (`McpCommand.AgentFormNote`), to `drift`, to `docs/FOUNDATION.md` or to any skill or agent file.

## Users

- The person or agent who reads the apply report after an import and decides whether to check the agent's state again.
- An agent in a client whose shell tool has a time limit, which runs `deploy apply --apply` by the docs.
- A reader of `HARNESS.md` who chooses a client for a deployment session.

## Data

Line numbers are those of `b065f23`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | `DeployApplyResult` gains `string? AgentsReadAt = null` as its last parameter, with its summary line; the default keeps the existing positional calls (`DeployApplyReportTests.cs:76` and its `Result` helper) and their exact-text tests unchanged | `src/PowerOps.Core/Deploy/DeployApply.cs:18-21` | row 6 |
| 2 | `Apply` keeps `string? agentsReadAt = null` beside `appliedAt` (`:489`); `Finish` passes it. `ReadAgents` takes the time from the same clock and format as `appliedAt` (`DateTime.UtcNow`, `yyyy-MM-dd'T'HH:mm:ss'Z'`, `:537`) right after the `deployAgents` call returns, and sets `agentsReadAt` to it only when the result of `AgentsAfter` holds an agent `found` or `missing`; the catch leaves it null. One rule covers the failed read and the all-unsendable names (`DriftLiveReader.ReadAgents`, `:52-59`, returns `[]` with no call then). Each `Finish(..., ReadAgents())` call evaluates `ReadAgents()` before `Finish` runs, so the value is set when the report is built. One local function gives the time to both | `src/PowerOps.Cli/DeployCommand.cs:489-494, 537, 593-615` | row 6 (a) to (d) |
| 3 | `WriteJson` writes `agentsReadAt` after `agents` (`Doc` gains the field last). `WriteText` appends the note line of Done when 2 after the agent lines when `AgentsReadAt` is not null, with the time through `TextReport.Clean` | `src/PowerOps.Core/Output/DeployApplyReport.cs:48-60, 64-68, 88-90` | row 6 (e) |
| 4 | Schema: `agentsReadAt` in `required` (after `agents`) and in `properties`, `["string","null"]` with `appliedAt`'s pattern, description: the UTC time, to the second, from the clock of the machine that ran it, at which the agent read-back returned; null when no agent came back found or missing (a dry run, a refusal, no agent in an imported zip, a failed read, or names powerops doesn't send). An agent can still become published after this time. `agents`'s description: "It has run against a tenant once (2026-11-11)" replaces "It has not yet run against a tenant" | `docs/schemas/deploy-apply.schema.json:8, 70-83` | `OutputSchemaTests` with the goldens |
| 5 | `COMMANDS.md` `deploy apply`: after the `publishedOn` paragraph (`:685-689`), "`agentsReadAt` in the report is the UTC time at which this read returned, null when no agent came back `found` or `missing`; the text report then adds a `note:` line with it." and "An agent can still become published after this read: in one run ([trial 3](plans/m89-apply-report/decisions.md#evidence)) its row read published 40 seconds after the import ended, though Learn says to publish an imported agent before sharing it (*Export and import agents using solutions*, read 2026-11-11). Check in Copilot Studio, or run `drift` between the source environment and the target, which reports `agent-published` while the agent is published in only one." The sentence at `:688` becomes "This read has run against a tenant once ([trial 3](plans/m89-apply-report/decisions.md#evidence), an agent `found`), so `missing` is still a prompt to check in Copilot Studio, not proof." After the paragraph on a timed-out import ("An import that times out leaves its row `unknown`", `:679-680`), the hour sentence of Done when 3. `HARNESS.md:3` and `:20` as Done when 4 | `docs/COMMANDS.md:679-689`; `docs/HARNESS.md:3, 20` | none (prose) |
| 6 | Tests, written first. `DeployApplyCommandTests` (the agents section, `:1196` on), each asserting inside one output only, never comparing times across two runs (Fable 1): (a) a JSON apply whose agent is read: `agentsReadAt` matches the pattern and `appliedAt <= agentsReadAt <= the time after the run`; a text apply: the whole note line by regex, its time not earlier than the same output's `Applied at:`, the line after the agent lines and before the summary; (b) the dry run with an agent: `agentsReadAt` null and no `note: agents read` line; (c) a read-back that throws: null, no note, status, stderr and exit code as today; (d) a partial failure that reads the agents of the solution that went through: set; a failed first import: null; (e) an agent zip whose schema name powerops doesn't send, through the real reader (`LiveAgents`): `not-read`, `agentsReadAt` null, no note, no web call. `DeployApplyReportTests`: a result with a fixed time prints the note once, exactly, after the agent lines, with a hostile time cleaned (`:76`'s case), and with null prints none; `Json_writes_the_agents_after_the_flows_…` (`:148-156`) now asserts `agentsReadAt` as the last property, once with a value and once null (Fable 3). The `deploy-apply*.json` goldens gain `"agentsReadAt": null` by the recipe "Update goldens"; the `.txt` goldens don't change. `Scrub` and `McpCommandTests.WithoutTime` stay as they are: no golden and no CLI-against-MCP comparison reads an agent (R5) | `tests/PowerOps.Cli.Tests/DeployApplyCommandTests.cs`; `tests/PowerOps.Core.Tests/Output/DeployApplyReportTests.cs`; `tests/golden/deploy-apply*.json` | itself |

## Automation

None.

## Screens

None.

## Environments and route

None. Nothing in this milestone reads or writes a tenant; the tests use the existing fakes.

## Solution and publisher

None.

## Constraints

- AGENTS.md: the smallest change, new logic with tests, writes only through `SafeWrite` (unchanged here), no version or changelog edit, no tenant value in Git, Conventional Commits.
- The chain rules: no tenant call; the kept roles, their plugin copies and `tests/eval/` untouched; merged one pull request at a time under the merge lock. `docs/COMMANDS.md` is shared with M88 (`drift`) and M92: this milestone edits only the `deploy apply` section, and rebases.
- The output never holds the target's id or URL: the new field is a time from the local clock.
- Platforms are Windows and Linux; nothing here depends on the platform.

## Assumptions

- The leak scan is `reports/leakscan.sh` in the main checkout, an untracked local tool; it runs against the committed branch from there (R3).
- The 40 seconds and the 10 minutes are what trial 3 observed (record P8, P9, D18; Claude Code 2.1.322 on Linux), not limits Learn documents: Learn is silent on when a publication completes, and says imported agents aren't published automatically. The docs say "in one run" for that reason. The 10-minute ceiling is in Claude Code's docs (decisions Q2). The claims research is in `decisions.md` (Log).
- A time to the second is enough: the observed gap was 40 seconds, and `appliedAt` uses the same resolution.

## Open questions

## Questions for your admin

## Steps

1. This plan and `decisions.md`, the claims research, the Fable plan review and a fresh-context `powerops-review-plan` into `review.md`, fixes, and the sign-off.
2. Row 6's tests, run and seen to fail.
3. Rows 1 to 4 until they pass; then the goldens by the recipe "Update goldens", reading the diff.
4. Row 5's docs.
5. The checks of Done when 5 and 6, the Fable work review, and the merge under the chain's protocol.
