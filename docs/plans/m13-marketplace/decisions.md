# Decisions: m13-marketplace

## Round 1

### Q1. How do the plugins serve the skills, given the Claude Code copy of `powerops-review-plan` differs from the shipped file?
1. Two plugin folders, `plugins/claude/` and `plugins/codex/`, each with its own manifest and real copies of the skills (the `ForClaude` copies for Claude Code, the shipped files for Codex), written by `PluginManifestTests` in `POWEROPS_UPDATE_GOLDEN=1` mode and compared in every normal run (recommended)
2. One plugin at the repo root (`source` `./`) that serves `skills/` itself, with only the Claude review copy kept elsewhere
3. In your own words
Answer: Two plugin folders, `plugins/claude/` and `plugins/codex/`, each with its own manifest and real copies of the skills (the `ForClaude` copies for Claude Code, the shipped files for Codex), written by `PluginManifestTests` in `POWEROPS_UPDATE_GOLDEN=1` mode and compared in every normal run. Option 2 doesn't work: Claude Code's `skills` key adds to the default `skills/` scan, so the plain review skill would load beside the Claude copy, and both agents would copy the whole repo into their plugin cache. One folder with both manifests (review R6) was weighed after the review and rejected: Codex would get the Claude copy of the review skill, and it still needs a second skills folder.

### Q2. How is the manifests' `version` kept?
1. release-please bumps both plugin manifests through `extra-files` (`type` `json`, `jsonpath` `$.version`); it starts at the current `0.0.0`, and a test checks the version equals `Directory.Build.props` and that the config lists both files (recommended)
2. No `version` field: Claude Code then tracks the commit SHA and Codex uses `local`
3. In your own words
Answer: release-please bumps both plugin manifests through `extra-files` (`type` `json`, `jsonpath` `$.version`); it starts at the current `0.0.0`, and a test checks the version equals `Directory.Build.props` and that the config lists both files. The BUILD-PLAN M13 row asks for `version` in the Codex manifest (Dataverse-skills' format), and a pinned version keeps the skills with the released CLI they call.

### Q3. Do the manual installs run?
1. Ask the user in this session for a yes, because they change the user's own Claude Code and Codex setup; offer to remove both afterwards; without a yes, record them as not run (recommended)
2. Run them now under the delegation of 2026-10-22
3. In your own words
Answer: Ask the user in this session for a yes, because they change the user's own Claude Code and Codex setup; offer to remove both afterwards; without a yes, record them as not run. The master session's brief says so. Changed after review R2: the marketplace and plugin installs run in throwaway `CLAUDE_CONFIG_DIR` and `CODEX_HOME` folders, which touch nothing of the user's, with one of Microsoft's plugins installed there too (R1); the Claude Code session check loads the plugin for one session with `--plugin-dir`; only the Codex session check, which needs the user's own Codex setup, still asks for the yes.

## Log

- plan started 2026-10-25 from the master session's brief (the BUILD-PLAN M13 row and build-order task 33)
- `powerops plan facts` not run: M13 creates no solution; `environment` stays null
- depth: quick (from `m12-handoff`)
- the user delegated their parts on 2026-10-22 (relayed by the master session); this session answers with the recommended options and signs the plan off itself after a clean fresh review; changes to the user's own agent setup still need the user's yes in this session (Q3)
- formats checked on 2026-10-25 against the Claude Code plugin docs, power-platform-skills `aed70bf`, Dataverse-skills `1966192` (both still main), openai/codex `rust-v0.163.0` and release-please's `docs/customizing.md`; notes in the shared side-session notes (`m13-sources.md`)
- approved without: users, constraints (the BUILD-PLAN M13 row and Phase 3b text cover them)
- approved (2026-10-25)
- fresh review (Sonnet) 2026-10-25: 0 High, 4 Medium, 10 Low; all fixed in the plan text or accepted with a reason in review.md (R1, R2 and R4 changed Q3); no user decision needed
- signed off 2026-10-25 by this session under the user's delegation (see above), after every review finding was fixed
