---
status: signed-off
review: done
signed-off: 2026-10-30
signed-off-hash: 7b9c3e0e0a6edfe21fad1f37440733e0f05025929f5928abef48a3353d2f724f
environment: null
depth: quick
supersedes: null
---

# M34: Fixes from the Windows live run of 2026-10-30

## Goal

Fix what a live test on Windows 11 showed, where the fix needs no tenant. The test ran PowerOps's planning skills with Microsoft's skills up to `deploy plan`, and its handover was reviewed on 2026-10-30 by two independent reviewers who agreed a fix list. This milestone is that list's "fix now" part: 11 fixes, all on both Linux and Windows. The report is kept outside the repo, because it quotes a private test run.

Done when:

- Each fix below is made as its row says, with a test where the row names one.
- The fix PR is green on Linux, Windows and macOS and squash-merged. Then a second PR moves every PowerOps pin in the docs to that merge commit, records the CI results and marks this plan done.
- `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` and `powerops plan status .` are clean before each push.

## Users

- An agent that follows `powerops-plan`, `powerops-review-plan` or `powerops-implement`, and the person who approves its work.
- A person who uses Microsoft's skills with PowerOps through `docs/HANDOFF.md`.
- A person who wants a first `drift` snapshot of one environment.
- A maker who follows PO012's fix.

## Data

Line numbers are those of `4988088`.

| # | Severity | Change | Where | Test |
|---|---|---|---|---|
| 1 | medium | Section 4 gains a step of live checks of the final saved version in Dev, made through Microsoft's skill for the product or by asking the user, never with `pac` or `az`. A read-only check needs no go-ahead; a check that runs a flow or writes data follows section 3 step 7. A check that can't run is reported as not verified. `status: done` needs every check passed, or the user accepting each gap by name; otherwise `status` stays `implementing`. The last step reports differences from the plan body | `skills/powerops-implement/SKILL.md:47-50`; one sentence in `docs/HANDOFF.md` step 4; `docs/PLANNING-SKILLS.md:66,89` | new assertions in `SkillsCommandTests` |
| 2 | low | Name the Dataverse skills; `code-apps-preview` replaces `code-apps` as the plugin name; check prerequisites first; ask the user in chat about each sign-in and never run `pac` or `az` to find out; pass the plan's environment and solution; stop when a tool can't target that solution | `skills/powerops-implement/SKILL.md:38`; `docs/HANDOFF.md:17` | new assertions in `SkillsCommandTests` on the new names and on not running `pac` or `az`; `HandoffDocTests` stays green |
| 3 | low | PO012's page gains Learn's three caveats on filter columns; its `Fix` and message stay | `docs/rules/PO012.md` | `RuleDocsTests` |
| 4 | low | Say that a live target read does read layers | `docs/COMMANDS.md:82` | none, docs only |
| 5 | low | Replace "refuses when anything changed" with the inputs the plan covers, as `docs/COMMANDS.md:84` lists them | `docs/HANDOFF.md:31`; `src/PowerOps.Cli/DeployCommand.cs:56`; `src/PowerOps.Cli/McpCommand.cs:253`; `docs/examples/promote-dev-to-test.yml:7` | `tests/golden/mcp-tools.txt` regenerated |
| 6 | low | Warn that an unpacked folder holds `.msapp` files that cache the environment URL | `docs/HANDOFF.md:18` | none, docs only |
| 7 | low | A request that names an environment is enough to write `environment:` in the fast path, with a Log line in `decisions.md`. The fast path still reads no facts, and whether `pac` is signed in stays unconfirmed | `skills/powerops-plan/SKILL.md:44,161,163`; `docs/PLANNING-SKILLS.md:166` | a new assertion in `PlanFactsSkillTests` that fails on the old "Leave `environment: null`" wording; its fast-path guard still holds |
| 8 | low | The review checklist's Testability item asks about ways out of a form | `skills/powerops-review-plan/SKILL.md:42`; `docs/PLANNING-SKILLS.md:189-198` | a new assertion in `SkillsCommandTests` that pins the phrase |
| 9 | low | Say `emptyConnectionId` checks presence, not that the connection exists. Correct the claims that nobody has run PowerOps on Windows: the FOUNDATION item's checkbox stays open, and it says the run reported three of its points working in Claude Code (the plugin starting, the real `az.cmd` and `pac`, and `pac`'s sign-in from a client-started server). The install from a clone as the README writes it was done before the run and not observed, so it stays unverified with C-10b, `cmd.exe`, Codex and the Action's scripts, as does all of macOS | `docs/COMMANDS.md:56,74`; `docs/HANDOFF.md:29`; `docs/schemas/settings-validate.schema.json:21`; `README.md:29`; `docs/FOUNDATION.md:160` | none, docs only; no golden holds the schema's description |
| 10 | low | Runbook step 1 writes down `git rev-parse HEAD` of the clone, because `--version` can't tell two builds apart | `docs/BUILD-PLAN.md`, Windows and macOS runbook | none, docs only |
| 11 | low | `drift` accepts one `--env` when `--save` is given (decisions Q1, Q2). The error becomes `give at least two --env, or one with --save` | `src/PowerOps.Cli/DriftCommand.cs:17,45-48`; `docs/schemas/drift.schema.json:5,14`; `docs/COMMANDS.md:17-19,66,74`, including one sentence that `no drift` with one environment means its own checks found nothing, not that anything was compared; `docs/USE-WITH-AGENTS.md` | new cases in `DriftCommandTests` and one new golden named `drift-*.json`, which `OutputSchemaTests` validates |

## Automation

None changes. The `drift` report gains no new field.

## Screens

None. Only CLI text, JSON and docs change.

## Environments and route

None. No step reads or writes a tenant, and no step runs `pac`, `az`, `plan facts` or any `--apply`.

## Solution and publisher

None.

## Constraints

- AGENTS.md: new logic comes with tests, `dotnet format --verify-no-changes` stays clean, Conventional Commits, no version or changelog edits. A skill edit is followed by the "Update goldens" recipe, so `plugins/*/skills/` regenerate.
- Section numbers and step numbers of `powerops-implement` section 3 stay, because `docs/BUILD-PLAN.md` refers to them: the new text in step 3 is a paragraph of that step.
- `docs/HANDOFF.md` keeps its ten numbered steps and its hook block, which `HandoffDocTests` pins.
- PO012's `Fix` and message stay: the `Fix` is in five goldens, the message is in baselines, and Learn supports both.
- The MCP `drift` tool has no save input (`docs/BUILD-PLAN.md` decision 25) and keeps its minimum of two environments (`McpCommandTests.cs:614`).
- New text in `docs/HANDOFF.md` writes `powerops-<name>` for a skill and `powerops <command>` only for a real command, starts no line with a number and a bold title, and leaves step 4's `powerops test ` in place: `HandoffDocTests` checks all three.
- `DriftCompare`, `DriftReport` and the snapshot format don't change: with one environment, the report holds only the checks on that environment.

## Assumptions

- A skill's text can tell an agent to check and stop, but can't make it. Whether agents follow the new text needs a tenant run, which is a separate step after this milestone.
- The Microsoft claims in the skill text rest on the open-source skills at `aed70bf` and `1966192` and on Learn, read 2026-10-30. The reported event itself, a flow joining the maker's preferred solution, is unverified, so the skill text is conditional.
- `schemaVersion` stays 1 when `environments` may hold one entry: nothing is released, so no consumer exists (as in M33 decisions Q1).

## Open questions

None.

## Questions for your admin

None.

## Steps

1. This plan, a fresh review, fixes, sign-off.
2. Fix 11, with its tests, schema and docs, by an implementer subagent, then reviewed.
3. Fixes 1 to 10 by an implementer subagent, then reviewed; `plugins/*/skills/` regenerated.
4. The checks and the leak scan; the coordinating session reads the diff and re-runs one snapshot file with and without `--save` offline (both exit 2, with the new messages). The success path is covered by `DriftCommandTests`, which use a fake reader; a live one-environment run belongs to the tenant run after this milestone.
5. The fix PR; CI green on Linux, Windows and macOS; squash-merge.
6. The pin PR: every PowerOps pin to the merge commit of step 5, the CI results recorded, this plan marked done; CI green; merge.

The fix PR also adds an M34 section to `docs/BUILD-PLAN.md` after M33 (one milestone row and build-order items 60 and 61), and two lines in the verification record: M34, and the run of 2026-10-30 as reported evidence, not repeated and not the M32 runbook, with its commit, Windows, Claude Code, `pac` and Azure CLI versions and the commands it ran.
