plan-hash: 55f202c82c6d820d33a7ab23c288a79bc2c621454669356f18b0a9e42ca1e5aa
model: claude-sonnet-5-5
fresh-context: yes

Environment facts were not read: the request did not say that `pac` is signed in to a plan environment, and the plan names none (`environment: null`). The review is against the repo at commit `d113fc9` (read with `git show`, not the working tree), the Phase 7 plan, and Microsoft Learn (checked with the Learn MCP tools on 2026-11-04). The plan has 0 open questions. Checklist items 1 to 10 do not apply (no solution, connector, table, role or screen is built); item 11 is met (the plan has a `Won't build` list and each row traces to a Goal line).

Checked and found sound: `JsonOutput.SchemaVersion` is read in code by `DeployPlanFile` (the plan-file version check) and by every other report's `Doc`; only `DiffReport` moves. `SnapshotFile` has its own constant. No test, action script, skill, MCP description or eval baseline reads `diff`'s JSON or its version, apart from `DiffCommandTests` line 64, which the plan changes. `OutputSchemaTests` maps `diff-` to the schema by prefix, so `diff-agents.json` is picked up. No `bots/` or `botcomponents/` path exists under `tests/eval` or `tests/fixtures` at `d113fc9`. `Risk.Describe` feeds only the Markdown legend. `release-please-config.json` has `bump-minor-pre-major`, so `feat!:` on 0.x gives the same minor bump. The `ImportCall` exit codes stay 0 or 2: a route and an unreadable call both block, and `Routes` decides only the unreadable flag.

## Findings

### High

No findings.

### Medium

#### R1. The `pac copilot init` route blocks the local scaffold, against the Learn page and the M49 section
- Area: Dev to Test to Prod route (checklist 5); Learn claims (Assumptions)
- Finding: Learn's `pac copilot` page says `init` runs in two modes decided by `--environment`. Without it, "local scaffold": it writes template files, creates nothing in Dataverse and needs no sign-in. With it, "bootstrap": it packages and imports a solution. The plan's Assumptions and Q7 rest on the generic text of the `--environment` parameter ("when not specified, the active organization ... will be used") and conclude that a bare `init` "can import". The page's own mode text contradicts that, so the route line "can import a solution that no plan holds" is not true for a bare `init`. The M49 section asked for a route for `pac copilot init --environment` only if M48 found it writes to a tenant. The plan widens it to every `init`, so with the hooks on it blocks the local authoring step that the `copilot-studio` plugin and `docs/rules/PO028.md` describe, and the new `Markers` word also blocks any command that merely mentions `copilot init` (a `grep`, a commit message). Q7 answers this with "hooks are off in Dev", but the HANDOFF text only says the gate runs while the hooks are on, in any environment. Q7 never records the mode text.
- Suggested fix: Re-decide Q7 against the mode text. Option A: route only when `--environment` or `-env` is present (a bare `init` passes; `--environment=x`, a variable word and a `-env` hidden before the subcommand already end in the can't-read line). Option B: keep the broad block, but state the Learn mode text in Assumptions, say in Q7 that the block is deliberately wider than the M49 section and why, and word the route as "may import" or "can import (with --environment)". Either way fix the Assumptions line and add a test for the bare form that matches the choice (blocked, or passing).
- Resolution: Fixed by option B. Assumptions now give Learn's two `init` modes (a local scaffold without `--environment`, an import with it) beside the flag's description; Q7 says the block is deliberately wider than the M49 section and why (the gate can't see the profile or which reading `pac` follows, the hooks are on only for the import step, and option A would have to read every spelling of the flag); Done when 3 says so; the route text is now "may import", and the bare `pac copilot init` has its test (blocked, not unreadable).

#### R2. The tests of rows 3 and 7 do not pin several edge cases that the Data design relies on
- Area: Testability (checklist 9)
- Finding: Row 3 lists the cases to test, but it leaves out: (a) a removed or added agent that has tools. The agent block gives one line and each tool then gives its own `agent-tool` line (a removed one gives `removed-component,agent-tool`); the expected lines are not stated, and row 7's golden would show them only by accident. (b) Two agents, or two tools, with the same key in one solution (`Keyed` keeps the first, silently). (c) Agent key case (only tool key case is listed). (d) `Instructions` of `""` against null, which the design treats as a change. (e) Instructions that differ only in line endings: the plan calls the compare exact, but nothing says that M48's reader normalizes them, and an export unpacked on Windows against one on Linux is the likely source of noise. (f) A tool moved to another agent under the same schema name (documented as unobserved, but not pinned).
- Suggested fix: Add (a), (c) and (d) to row 3 with their expected lines, and (b) and (e) either as tests or as a sentence in Q3 that says what the reader gives. Pin (f) with one test so the documented limit cannot change unnoticed.
- Resolution: Fixed. Row 3 now lists (a) an added and a removed agent with two tools and their exact lines, (b) duplicate tool and agent keys (the first counts), (c) agent key case, (d) `""` against null and (f) a tool moved between agents (no change), each with a test in `SolutionDiffTests`; (e) is a reader test in `AgentReaderTests` that pins what a `\r\n` GPT file gives against a `\n` one.

### Low

#### R3. Row 10's gate test cannot be a new case in the existing theory
- Area: Testability (checklist 9)
- Finding: In `DeployGateCommandTests` the write-route theory builds its Bash input as the fixed command `pa app push`, so a row cannot add `pac copilot init --environment <id>` to it without changing the theory (a command per row, or a separate test). Row 12 (`HandoffDocTests`) is also not in Steps 2's list of tests to write first.
- Suggested fix: Say in row 10 that the theory gains a command parameter (or that a second test is added). Add rows 12 and 13's tests to Steps 2.
- Resolution: Fixed. Row 10 says the write-route theory gains a command parameter (the implementer gave it an optional `command`), and Steps 2 now names rows 12's tests among those written first.

#### R4. The Learn attribution for "channel details are empty" is wrong
- Area: Unanswered areas (checklist 10); Learn claims (Assumptions)
- Finding: The Assumptions line puts "channel details are empty" under "Export and import agents using solutions". The standard-harness page says to configure user authentication again and that the imported agent must be published before it can be shared. The empty channel details and missing icon are on the Customer Conversation (preview) export page. The other claims match: the ALM guidance lists the five non-solution-aware items, Variables overview has the republish rule and the secret exception, and `init --environment` imports a solution (but see R1).
- Suggested fix: Move the "channel details are empty" claim to its real page, or drop it. Keep the checklist item for deployed channels from the ALM page.
- Resolution: Fixed. "Channel details are empty" is dropped from Assumptions; the deployed-channels item rests on the ALM guidance page, and `docs/HANDOFF.md` step 11 never used the claim.

#### R5. A published schema URL points at `main`, so the migration line needs a pin hint
- Area: Release and compatibility (Done when 6)
- Finding: `diff.schema.json` has an `$id` on the `main` branch. A consumer that fetches it by that URL moves to version 2 the moment the PR merges, with no release between. The migration line in the README and COMMANDS says what changed but not how to stay on version 1 (use the schema file from the v0.31.0 tag).
- Suggested fix: Add one clause to the migration line: pin the schema from a release tag to stay on version 1.
- Resolution: Fixed, with a change: no release is tagged yet (`git ls-remote --tags` is empty), so the migration line in `docs/schemas/README.md` says the `$id` points at `main` and to keep a copy of the schema from an earlier commit to stay on version 1.

#### R6. BUILD-PLAN item 84 and the Phase 7 plan still say `feat:`
- Area: Scope (Done when 8, 9)
- Finding: Q11 makes the PR title `feat!:`, but BUILD-PLAN item 84 and the Phase 7 plan's M49 heading label the milestone `feat:`. Row 16 edits the item (marks it built) without saying the label changes, so the records would disagree with the PR.
- Suggested fix: Row 16 marks item 84 as `feat!:` (or says why the label stays).
- Resolution: Fixed. BUILD-PLAN item 84 now reads `feat!:`, and row 16 says so. The Phase 7 plan's M49 heading keeps `feat:`: its body is signed off, and an edit would void the hash; Q11 records why.

#### R7. A tool's change to Maker mode or to MCP has no risk label
- Area: Security roles (checklist 8)
- Finding: Q4 and row 2 give a changed tool one detail line and no risk. A reviewer who reads only the risk column of a PR comment will not see an Invoker to Maker change or a tool turned into an MCP tool. PO030 warns on the end state, but only when `analyze` runs and its finding is read. This is a deliberate choice, and the M49 section asks only for added tools, so it is Low.
- Suggested fix: Keep the choice, but add one sentence to Q4 that names this gap and PO030 as the place the end state is judged, or label a change to Maker mode with `agent-tool`.
- Resolution: Accepted, as suggested: Q4 now names the gap (a change to Maker mode or to MCP shows in the detail and has no risk label) and PO030 as the place the end state is judged.

## Counts

- High: 0
- Medium: 2
- Low: 5
- Open questions in the plan: 0
