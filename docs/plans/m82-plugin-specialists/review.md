plan-hash: 715e14a294b32b21c6599194a5828425b3435022a13f42ca2d874d1e43d031e1
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: the plan's `environment:` is `null`, and no sign-in to an environment was confirmed, so `powerops plan facts` was not run. No tenant call of any kind was made.

Read for this review: `plan.md`, `decisions.md` (no `mockups/`), the six `.claude/agents/powerops-*.md` sources, `CLAUDE.md`, `AGENTS.md`, `tests/PowerOps.Cli.Tests/PluginManifestTests.cs`, `plugins/claude/` (manifest, `.mcp.json`, skill copies), `.claude-plugin/marketplace.json`, `Directory.Build.props`, `.gitattributes`, `docs/USE-WITH-AGENTS.md`, step 11 of `docs/HANDOFF.md`, `skills/powerops-plan/references/`. `powerops plan status` reported `approved`, review `pending`, 0 open questions.

Second pass (2026-11-09, same skill, fresh context): read the revision in c9634d1 (`plan.md`, `decisions.md`) against the six sources, `CLAUDE.md` and `plugins/claude/skills/powerops-plan/`. `powerops plan status` reports `approved`, review `done`, 0 open questions. Each resolution below was checked against the revised plan and ends with the result; R11 is new.

Final pass (2026-11-09, same skill, fresh context): read the revision in ab57f71 (rows 1 and 2 of `plan.md`, `decisions.md`) against the six sources. `powerops plan status` reports `approved`, review `done`, 0 open questions. R11's resolution was checked; R12 is new.

## Checklist items not applicable

The plan changes this repo (a Claude Code plugin folder, a test-project generator, tests and docs), not a tenant solution.

1. Solution boundaries and publisher prefix: not applicable; no solution or publisher (the plan says None).
2. Connection references and environment variables: not applicable to a tenant. The one environment-like value, the repository URL in rewrite (g), is read from `Directory.Build.props`, not hard-coded (but see R1).
3. Premium connector and licence impact: not applicable; see R7 for the analogous cost to plugin users.
4. DLP fit: not applicable; no connectors are used.
5. Dev to Test to Prod route: not applicable to a tenant. The repo route (branch, local commit, PR, CI, merge by the session under the 2026-11-09 answer in `decisions.md`) is stated.
6. Managed or unmanaged: not applicable.
7. Data volume and delegation: not applicable.
8. Security roles: not applicable to a tenant. The copies keep their sources' `tools` lists and Never lists; plugin agents ignore `permissionMode`, so the user's own permission prompts apply.
9. Testability: every Data row names a test; findings R1, R3, R4, R11 and R12 below.
10. Unanswered areas: 0 open questions, `Questions for your admin` empty; the Log lists the areas not asked and why. See R10.
11. Size: no tables, lists, flows or screens. Each artefact (generator, six copies, tests, manifest sentence, three doc edits) traces to F5 in `Goal`, and `Won't build` is present. No finding.

## Findings

### High

No findings.

### Medium

#### R1. The `docs/` test contradicts rewrite (g)
- Area: 9. Testability
- Finding: Data row 2 (g) rewrites copilot-studio's `docs/HANDOFF.md` to the repository URL followed by `/blob/main/docs/HANDOFF.md`. Data row 4's `Plugin_agents_ship_no_repo_only_reference` requires every `docs/` occurrence in a shipped file to be followed by `plans/`, and done-condition 2 says every `docs/` path in a copy starts `docs/plans/`. The generated copilot-studio copy fails the test as specified. The link also points at `main`, which can drift from the installed plugin version.
- Suggested fix: Say how the test treats the URL: exempt occurrences that follow the repository URL from `Directory.Build.props` (and say so in done-condition 2), or link the HANDOFF guide by its name only and leave the path out. If the link stays, consider whether `main` is the intended ref.
- Resolution: answered in `plan.md`: done-condition 2 and `RepoOnlyReferences` (row 4) exempt a `docs/` that follows `<RepositoryUrl>/blob/main/`, and `Repo_only_references_are_found` shows a bare `docs/HANDOFF.md` is still found; (g) says why it links `main` (the plugin's version is `0.0.0` until the first release, with no tag). Checked (second pass): holds; done-condition 2 and row 4 exempt only the URL form, and the crafted-string test keeps a bare `docs/` caught.

#### R2. Paths into the plan skill's references stay relative in four copies
- Area: 9. Testability
- Finding: Only design-lead's opening read line gets `${CLAUDE_PLUGIN_ROOT}/skills/powerops-plan/...` (rewrite f). The other references to the plan skill's files stay bare and resolve to nothing in a plugin user's repo: design-lead (`references/canvas.md`, `route.md` in "You own" and "How you work"), bi (`references/power-bi.md`), copilot-studio (`references/copilot-studio.md`) and workflow-logic ("the `powerops-plan` skill's `references/data.md`"). The row 4 test does not catch them, since none starts `docs/`.
- Suggested fix: Add one rewrite that prefixes these skill-relative references with `${CLAUDE_PLUGIN_ROOT}/skills/powerops-plan/`, or one sentence in the shared rules saying the plan skill's files are under that folder; add a test assertion that no bare `references/` path survives.
- Resolution: answered in `plan.md`: rewrite (j) prefixes every `` `skills/`` and `` `references/`` path with `${CLAUDE_PLUGIN_ROOT}/…/powerops-plan/`, the row 3 lead-in names the skill's folders, and `RepoOnlyReferences` flags a bare `skills/` or `references/` and a `${CLAUDE_PLUGIN_ROOT}/` path with no file or folder under `plugins/claude/`. Checked (second pass): holds for coverage; every `skills/` and `references/` target in the six is listed in (j), and each `${CLAUDE_PLUGIN_ROOT}` path named exists under `plugins/claude/skills/powerops-plan/`. See R11 for how (j) meets the throwing `Replace`.

#### R3. The brief contract reaches only the subagent, not the main session that writes the brief
- Area: 10. Unanswered areas
- Finding: The extracted shared rules are mostly addressed to the main session (spawn with the Agent tool, the brief's fields and `tenant:` line, up to three at once, merge their lines, route hand-offs). In the plugin they live only inside each agent file, which a plugin user's main session never reads (it sees the `description` only), and no shipped skill names the specialists. So a plugin user's main session will brief without phase, plan folder or `tenant:` line, and the copies tell themselves to "Spawn with the Agent tool", which a subagent cannot do. The plan doesn't say whether this is accepted or how the main session learns the contract.
- Suggested fix: Either put the brief fields and routing in the row 7 `USE-WITH-AGENTS.md` subsection (and one line in each description pointing to it), or state in `Won't build` / the doc's not-checked list that the main session gets no brief contract and that a brief without a `tenant:` line means offline. Say in the shared rules which bullets are for the main session.
- Resolution: answered in `plan.md`: row 3's fixed lead-in says the rules are written for the main session, that a missing `tenant:` line means offline and a missing phase or plan folder gets `state: blocked`; row 7's doc subsection lists the brief's fields; Assumptions names the gap (the main session sees only the description). Checked (second pass): holds; the lead-in, the row 7 brief fields and the Assumptions line are all in `plan.md`.

#### R11. Rewrites with no target in some sources conflict with the throwing `Replace`
- Area: 9. Testability
- Finding: Row 1 says every rewrite goes through `Replace`, which throws when its text is absent, and done-condition 2 says every rewrite throws when its target is missing. Two rewrites have no target in some sources: (j) runs "in every source", but apps and automate contain no `` `skills/`` or `` `references/`` path; the removal of a list line starting `powerops-implementer` or `powerops-reviewer` in (c) has a target only in apps, automate and workflow-logic (bi, design-lead and copilot-studio carry those names inside the researcher line, which the truncation handles). Built as written, `ForPlugin` throws for five of the six sources, or the builder silently makes these two conditional, which the plan doesn't say.
- Suggested fix: Name per source which rewrites apply (as (e) does with "all six"), for example (j) in bi, design-lead, workflow-logic and copilot-studio and the implementer/reviewer removal in apps, automate and workflow-logic, and have `Replace` throw only for a named source whose target is missing; or state the expected count per source for these two and throw on a mismatch.
- Resolution: answered in `plan.md`: row 2 (c) names the sources the implementer and reviewer removal applies to (apps, automate, workflow-logic; it throws there when none is found), and (j) is a replace-all that never throws, with `RepoOnlyReferences` as its backstop; row 1 says (j) is the one rewrite outside `Replace`. Checked (final pass): holds; the removal in (c) has targets in exactly apps, automate (two lines each) and workflow-logic (one), and (j) can no longer throw for apps and automate. The text elsewhere still says every rewrite throws: see R12.

### Low

#### R4. Line-ending normalisation of the generator's inputs is not stated
- Area: 9. Testability
- Finding: Row 1 calls `ForPlugin` "a pure function of the two texts with LF line endings", but `.gitattributes` fixes only `*.sh`, so a Windows checkout (the primary platform, with Windows CI) may give CRLF sources and `CLAUDE.md`. The existing `Files` helper normalises only the shipped side. If the test passes raw CRLF text, the frontmatter split, the string rewrites and the comparison can fail on Windows only.
- Suggested fix: Say in row 1 or row 4 that the test reads both inputs with `\r\n` replaced by `\n` before calling `ForPlugin`, as `Files` does.
- Resolution: answered in `plan.md`: row 1 says the caller replaces `\r\n` with `\n` in both inputs, as `Files` does. Checked (second pass): holds.

#### R5. The new description sentence adds `: ` to four plain YAML scalars, and CI never validates agent frontmatter
- Area: 9. Testability
- Finding: `Advisory and record-only: its findings never block.` brings a colon-space into the descriptions of apps, automate, bi and workflow-logic, which have none today. The Assumptions' `claude plugin validate` run used the unmodified sources (design-lead and copilot-studio already contain `: ` and passed, which suggests a lenient parser, but that is inferred). Validation is local-only (done-condition 4), so later regenerations are not validated in CI.
- Suggested fix: Use a sentence without a colon (for example `Advisory and record-only; its findings never block.`), or keep it and record in the work review that validate passed on the generated copies.
- Resolution: answered in `plan.md`: the sentence is now `Advisory and record-only (its findings never block).`, with no `: `; `claude plugin validate plugins/claude` runs on the generated copies in step 2 (locally; CI has no `claude`). Checked (second pass): holds; row 1 and done-condition 3 use the new sentence.

#### R6. In this repo, a maintainer with the plugin installed sees twelve near-identical specialists
- Area: 10. Unanswered areas
- Finding: The Assumptions note that project and plugin copies both load here because their names differ, but not the effect: Claude picks agents from descriptions, and the plugin copies (no researcher, implementer or reviewer hand-offs) can be picked for repo work.
- Suggested fix: Add to the row 7 `CLAUDE.md` sentence that in this repo the project names (`powerops-<name>`, not `powerops:powerops-<name>`) are the ones to spawn.
- Resolution: answered in `plan.md`: row 7's `CLAUDE.md` sentence says that in this repo the project agents (bare names) are the ones to spawn. Checked (second pass): holds; the sentence is outside the extracted section, so the copies don't change.

#### R7. The doc subsection does not say the specialists run on Opus at high effort
- Area: 3. Licence impact (cost to plugin users)
- Finding: Every copy keeps `model: opus` and `effort: high`, so a user's session on a smaller model pays Opus rates for each specialist run. `USE-WITH-AGENTS.md` already discloses this for the plugin's role skills; row 7's subsection omits it.
- Suggested fix: Add one line to row 7: the six run on Opus at high effort whatever the session model.
- Resolution: answered in `plan.md`: row 7's doc subsection says they run on Opus at high effort and what that costs a session on a smaller model. Checked (second pass): holds.

#### R8. The `@agent-` mention syntax in row 7 is unsourced
- Area: 10. Unanswered areas
- Finding: Row 7's example `@agent-powerops:powerops-apps` is not covered by the Assumptions, which source only the `<plugin>:<name>` naming.
- Suggested fix: Add it to the Assumptions with its source and date, or check it in the work review, or drop the example and keep `powerops:<name>`.
- Resolution: answered in `plan.md`: the form is from Claude Code's plugin docs (code.claude.com/docs/en/plugins/components, checked 2026-11-09 by a `claude-code-guide` subagent: invoke with `@agent-<plugin>:<name>`), and row 7 cites it. Checked (second pass): holds as a cited claim; the source was not re-read here (offline review).

#### R9. The plan and decisions name a web address
- Area: Values in the plan
- Finding: The Assumptions and the `decisions.md` Log name a documentation host and path (`<url>`). It is public documentation, not a tenant value, but the review rule reports any URL.
- Suggested fix: Keep it if public documentation hosts are accepted in plans, or cite the pages by title and date only.
- Resolution: accepted: the URLs are public documentation (code.claude.com) and the repository's public GitHub URL, read from `Directory.Build.props`; no tenant value. Assumptions says so. Checked (second pass): accepted risk recorded in Assumptions; Low, so no sign-off decision is needed.

#### R10. The opening of `decisions.md` is stale
- Area: 10. Unanswered areas
- Finding: `decisions.md` still opens with "This milestone's plan is not written yet", although the plan is written and approved.
- Suggested fix: Reword it to say that Round 0 came before the plan was written.
- Resolution: answered in `decisions.md`: the opening paragraph now says the answer came before the plan. Checked (second pass): holds.

#### R12. Done-condition 2 and row 2's lead-in still say every rewrite throws
- Area: 9. Testability
- Finding: Row 1 now makes (j) the one rewrite outside `Replace`, a replace-all that never throws. Done-condition 2 still says "every rewrite of rows 2 and 3 throws when its target is missing from its source", and row 2 still opens "Body rewrites, each through `Replace` (row 1)". A builder or work review reading those lines can treat the non-throwing (j) as a miss. The behaviour is safe, since `RepoOnlyReferences` backstops (j); only the wording disagrees.
- Suggested fix: Add "except (j)" to done-condition 2 and to row 2's opening phrase.
- Resolution: answered in `plan.md`: done-condition 2 and row 2 now say "except (j)"
