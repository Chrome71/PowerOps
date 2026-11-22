---
status: done
review: done
environment: null
depth: quick
supersedes: null
signed-off: 2026-11-22
signed-off-hash: 211ba8bfea036ea2b90a0c1d2a96d9ad16d24e103971512c7746673464c4ead1
---

# M121: Specialists out of the shipped plugin

## Goal

M82 ([plan](../m82-plugin-specialists/plan.md)) made the Claude Code plugin ship generated copies of the six Power Platform specialists in `plugins/claude/agents/`. The M119 specialist eval measured them and the owner decided "Keep none; PR 171 waits (Recommended)" (2026-11-22, recorded in `docs/plans/m119-specialist-eval/decisions.md` on branch `test/m119-specialist-eval`, commit `54e1230`, not yet on `main`). They run on Opus and fill every plugin user's agent list. The agreed debate plan (Phase 1 item 2, `reports/three-way-debate-2026-11-22/AGREED-PLAN.md`, git-ignored) takes them out of the plugin and keeps them in `.claude/agents/`, with `CLAUDE.md`'s routing, for maintainers of this repo.

Done when:

1. `plugins/claude/agents/` is gone from the tree, and `tests/PowerOps.Cli.Tests/PluginAgents.cs` with it.
2. A new test, `PluginManifestTests.Claude_plugin_ships_no_agents`, fails while `plugins/claude/agents/` exists (seen failing first) and passes after step 3.
3. `plugins/claude/.claude-plugin/plugin.json`'s description no longer names specialist agents; `Claude_marketplace_lists_the_powerops_plugin` asserts it does not contain "specialist" (was: contains "specialist agents"). `version` unchanged.
4. Only copy-related tests are deleted (Data row 2); the M93 and M95 tests that read `.claude/agents/` and `CLAUDE.md` stay and still pass, now reading the sources directly.
5. `AGENTS.md` (Layout, "Update goldens"), `CLAUDE.md` line 5 (the lead-in sentence of "Claude Code extras", above the specialist section), `docs/USE-WITH-AGENTS.md` and `docs/BUILD-PLAN.md` say the plugins ship no agents and why, citing M119's "Keep none" in words (Data row 6). Every relative link in the changed lines resolves in the tree.
6. The two open ecosystem entries that name what goes, ECO-0010 and ECO-0025, have their `Affects` lines narrowed (Data row 7). Then `git grep -n -E "plugins/claude/agents|PluginAgents" -- . ':!docs/plans'` returns only the lines Data row 7 lists.
7. `git diff --stat origin/main -- .claude/agents tests/eval skills/powerops-verify skills/powerops-review-security plugins/claude/skills plugins/codex` is empty, and `RoleEvalTests` passes.
8. `dotnet build` (0 warnings), `dotnet test`, `dotnet format --verify-no-changes`, `claude plugin validate plugins/claude`, `powerops plan status docs/plans/m121-specialists-out` with no violation, the leak scan (`reports/leakscan.sh origin/main <branch>`, run from the main checkout, because `reports/` is git-ignored and not in the worktree; 0 hits, control found), and the "Update goldens" recipe with `tests/golden/selftest.txt` restored, `plugins/claude/agents/` not recreated, and `git diff origin/main -- tests/golden plugins` holding only the deletion and the `plugin.json` sentence; the test count is the base's total minus 5 facts and 1 theory case, plus 1 new fact (5,766 → 5,761 at `1dea7e7`), or the difference is explained in the Log; CI all green; merged by the chain's lock protocol in one squash commit typed `fix(plugin):`.

### Won't build

- No change under `.claude/agents/` (the six specialists and the five maintainer agents stay byte-for-byte).
- No change to `CLAUDE.md`'s routing table or shared rules, only the section's opening paragraph.
- No skill, command, MCP tool or Codex plugin change; the Codex plugin never shipped agents.
- No install, uninstall or update of the plugin in the owner's own Claude Code.
- PR #171 and `test/m119-specialist-eval` are not touched; M119's plan folder is not copied in.
- Historical records stay as written: `docs/plans/m82-*`, `m93-*`, `m94-*` and the Why text of ecosystem entries (they describe what was done then). Only the `Affects` lines of the two open entries change (Data row 7).
- No new check on the three specialist sources (`powerops-apps`, `-bi`, `-copilot-studio`) that no test reads once the generator goes; the build plan allows one new test.

## Users

- A plugin user: after a reinstall, their agent list no longer holds six Opus `powerops:` agents.
- A maintainer of this repo: still spawns the six by bare name from `.claude/agents/`, with no copy to regenerate.

## Data

Row 1. Removed files: `plugins/claude/agents/` (6 files), `tests/PowerOps.Cli.Tests/PluginAgents.cs`.

Row 2. Tests deleted (copy-related only), all in `PluginManifestTests.cs`: `Plugin_agents_are_the_generated_specialists` (the generator test that writes the copies), `A_changed_missing_or_extra_agent_copy_is_stale`, `Plugin_agents_ship_no_repo_only_reference`, `Repo_only_references_are_found`, `The_generator_throws_when_a_source_no_longer_matches`, with their helpers `AgentsDir`, `ExpectedAgents`, `StaleAgentCopies`, `SplitFrontmatter` and `RepoOnlyReferences`. In `SkillsCommandTests.cs`, the case `[InlineData("plugins/claude/agents")]` of `Automate_specialist_names_inventory_and_the_flow_rules_by_what_they_find`; the test becomes a `[Fact]` on `.claude/agents` alone.

Row 3. Tests kept and changed: `Specialist_rules_mark_a_proposed_line_with_no_source_as_unproven` and `Specialist_rules_run_tenant_writes_under_the_signed_off_plan_or_a_yes_for_a_stop_or_a_deployment` (M93) assert the same strings on the "Power Platform specialist team" section of `CLAUDE.md` (from its heading to "Saved workflows in"), read by a small helper in the test class, in place of `PluginAgents.SharedRules`. Kept unchanged: `Workflow_logic_enforces_a_valid_record_at_the_write_not_in_a_flow_after_it` (M93) and `Workflow_logic_reads_the_build_standard_in_its_step_2` (M95).

Row 4. Test added: `Claude_plugin_ships_no_agents`: `Assert.False(Directory.Exists(<repo>/plugins/claude/agents))`, with a message naming M121, and `Assert.Null(manifest["agents"])` on `plugin.json`, the other route by which a plugin ships agents. Cleanups in `PluginManifestTests.cs`: the class comment (line 10) and the M93 comment (line 193) no longer name the copies, and the `System.Text.RegularExpressions` using goes if nothing else uses it. The section helper of row 3 throws when the heading or "Saved workflows in" is missing, and never falls back to the whole file. `Plugin_skills_are_the_shipped_skills` is untouched.

Row 5. `plugin.json` description: the sentence "It also ships six advisory Power Platform specialist agents (apps, automate, BI, design lead, workflow logic, Copilot Studio) that plan and review alongside the skills; their findings never block." is removed; the rest stays word for word.

Row 6. Docs. `AGENTS.md` Layout: the `plugins/` line drops the `plugins/claude/agents/` clause and says the plugins ship no agents (the specialists stay in `.claude/agents/` for maintainers). "Update goldens" step 1 drops the specialist-copy clause. `CLAUDE.md` line 5, the lead-in paragraph of "Claude Code extras" (the only `CLAUDE.md` line that changes; it stays one line, so the line numbers that ECO-0025 cites hold): its sentence on generated copies becomes: the six are for work in this repo and ship in no plugin, since the M119 eval ("Keep none", 2026-11-22); spawn the project agents by their bare names (an older plugin install may still list `powerops:<name>` copies). `docs/USE-WITH-AGENTS.md`: the "Specialist agents (Claude Code plugin)" subsection is replaced by a short paragraph: the plugins ship no agents, with the M119 reason; an install made before this change keeps its six `powerops:` agents until the plugin is uninstalled and installed again as above, because `claude plugin update` changes nothing until a release changes the version string. It claims no install was tried. `docs/BUILD-PLAN.md`: an M121 row (release: the next patch release, from the `fix:` commit) and a build-order line (number taken at merge).
Citation form, in all four docs: M119 is cited in words only (the M119 specialist eval, 2026-11-22, the owner's "Keep none", and one clause of its result), with no link to `docs/plans/m119-specialist-eval/`, because that folder is not on `main`. At the rebase under the lock, if M119's folder is on `origin/main`, the `USE-WITH-AGENTS.md` mention links to its report; otherwise the hand-over lists the link as a follow-up.

Row 7. Ecosystem entries (read per AGENTS.md "Ecosystem changes"): ECO-0010 (open; Power BI modeling MCP `--readonly`) names `plugins/claude/agents/powerops-bi.md:32`, `tests/PowerOps.Cli.Tests/PluginAgents.cs` and `docs/USE-WITH-AGENTS.md:60`; ECO-0025 (open; MCP write tools HANDOFF's deny advice misses) names `docs/USE-WITH-AGENTS.md:61`. Both are the "Tools." bullet or files that go. Their `Affects` lines drop those targets and keep the rest; the format regex still matches, and `docs/ecosystem/README.md` forbids only renumbering or deleting an entry. Their Why text, and the compatibility row 25, stay: they name `.claude/agents/powerops-bi.md`, which stays, and `docs/HANDOFF.md`, which keeps the `--readonly` advice for anyone running the Power BI server. Lines that may keep naming `plugins/claude/agents` or `PluginAgents` after the change: ECO-0010's Why text (history of the 2026-11-10 corrections), `docs/plans/`, and the message of the new test in `PluginManifestTests.cs`. The other text M121 writes (the `BUILD-PLAN.md` row and build-order line, the `AGENTS.md`, `CLAUDE.md` and `USE-WITH-AGENTS.md` sentences) says "the plugin's agents folder" or "the plugin ships no agents", not the literal path. After the rebase, line pointers of open entries into files this diff shifts are recomputed in the same edit: ECO-0032's `docs/USE-WITH-AGENTS.md:100` and ECO-0012's `docs/BUILD-PLAN.md:696`, and any other open entry's pointer into the four changed docs that the diff moves. So the `changes.md` diff is the two `Affects` narrowings plus those line numbers, and no Why text.

## Automation

None.

## Screens

None.

## Environments and route

None: repo files only, `tenant: offline`.

## Solution and publisher

Not applicable.

## Constraints

- AGENTS.md: the smallest change; tests first; Conventional Commits; no version or changelog edit; no tenant value.
- Chain rules (`reports/trial-3/chain/chain-rules.md`, debate section) and the build plan's limits: the kept roles, their plugin copies and `tests/eval/` stay byte-for-byte; `RoleEvalTests` passes; no edit to `powerops-review-sketch` or `baseline.md`; no rename of a skill or command; release PR #4, #171, #172 and #173 untouched.
- WATCH (Fable plan review of the build plan): the M93 tests that read `.claude/agents` and `CLAUDE.md` stay; nothing under `.claude/agents/` changes; no install into the owner's Claude Code.
- Shared files (`AGENTS.md`, `plugins/`, `SkillsCommandTests.cs`, `docs/BUILD-PLAN.md`) are settled under the merge lock by a rebase; M119 merges separately and whichever lands second rebases over the other's `AGENTS.md` Layout edit.

## Assumptions

- A1. Claude Code loads plugin agents from the plugin's `agents/` folder or an `agents` manifest key, so with neither the plugin ships no agents. Source: M82's sourced line (`docs/plans/m82-plugin-specialists/plan.md`, Assumptions); `plugin.json` has no `agents` key, and the new test holds both. `claude plugin validate plugins/claude` shows only that the plugin stays valid (it passes with the agents present too). That an installed plugin then lists no agents is not run (no install, per the WATCH note) and goes under "not checked" in the hand-over and the PR body. No Microsoft claim, so no `powerops-researcher` run; the two ecosystem entries that name the removed files are handled in Data row 7.
- A2. An installed plugin keeps its cached copy until the version string changes (`docs/USE-WITH-AGENTS.md`, already sourced to Claude Code's docs), so existing installs keep the six until a reinstall.

## Open questions

None. The owner's "Keep none; PR 171 waits" and "Approve and start" settle scope.

## Questions for your admin

None.

## Steps

1. **Plan and sign-off** (orchestrator). This folder; Fable plan review; `powerops-review-plan` in a fresh context; `powerops plan sign-off` under the owner's "Yes to all three" (2026-11-22). Done when: both reviews clean and `plan status` shows signed off.
2. **Tests first** (opus: this milestone session, which runs on Opus as the build plan assigns). Rows 2 to 4 in `PluginManifestTests.cs` and `SkillsCommandTests.cs`, and `PluginAgents.cs` deleted. Done when: the project builds and `Claude_plugin_ships_no_agents` and the new marketplace assertion fail on the tree as it is, every other test passes; the failing output is kept for the work review.
3. **Remove and edit** (opus, this session). Rows 1, 5, 6 and 7. Done when: the two failing tests pass and every check of Done line 8 except CI is clean; Done lines 5 to 7 hold.
4. **Work review** (Fable). Done when: PASS.
5. **Merge** (opus, this session). Lock; rebase over M119, M120 and M122 if any has merged (`AGENTS.md`, `docs/BUILD-PLAN.md` build-order number, `SkillsCommandTests.cs`, the line pointers of Data row 7); rerun every check of Done line 8; set `status: done` and commit; push, PR, CI; squash merge; release the lock; hand-over `reports/trial-3/chain/m121-done-handover.md` (git-ignored, outside the tree).
