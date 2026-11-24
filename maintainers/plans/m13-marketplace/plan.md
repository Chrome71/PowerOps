---
status: done
review: done
signed-off: 2026-10-25
signed-off-hash: 91b018f47230f86d39b05736208753db24fe70cb9097df081c9c55a06bf63ca8
environment: null
depth: quick
supersedes: null
---

# M13: plugin marketplace

## Goal

Finish BUILD-PLAN milestone M13 (build-order task 33, release v0.13.0), the last milestone of Phase 3b: PowerOps installs as a plugin from its own repo in Claude Code and in Codex, the same way Microsoft's power-platform-skills and Dataverse-skills do, so the three PowerOps skills are listed beside Microsoft's. `powerops skills install` stays the default for teams.

Done when:

- `.claude-plugin/marketplace.json` and `.agents/plugins/marketplace.json` exist at the repo root, each listing one plugin, `powerops`, with its plugin folder under `plugins/`.
- `tests/PowerOps.Cli.Tests/PluginManifestTests.cs` passes: it parses both marketplaces and both plugin manifests, every `source` and `skills` path exists, each skill's `name` equals its folder, the Claude copy of every skill equals `SkillsCommand.ForClaude` of the shipped file (so the review skill carries `context: fork`, `background: false`, `model: opus`), the Codex copy equals the shipped file, the descriptions say the `powerops` CLI must be on PATH, no plugin declares an MCP server, and each manifest `version` follows the release.
- `claude plugin validate --strict` passes on the repo root and on the Claude plugin folder.
- One manual run in Claude Code (marketplace add with a local path, then install) and one in Codex, each in a throwaway config folder that also has one of Microsoft's plugins installed from GitHub: the three skills are listed beside Microsoft's (Environments and route). Recorded in the BUILD-PLAN verification record.
- One Claude Code session with the plugin loaded lists the three namespaced skills and picks `powerops:powerops-review-plan` for a plan review. A Codex session with the plugin needs the user's own login and setup, so it runs only with the user's yes in this session; without it, the verification record says "not run" with the reason.
- `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` and `powerops plan status` are clean.

## Users

- A person who wants the PowerOps skills in their own Claude Code or Codex setup, next to Microsoft's plugins, without copying files into each repo.
- Agents that install plugins.
- Teams keep `powerops skills install`: the skills then live in the repo, so every contributor and CI sees the same gate.

## Data

No new runtime data. New files, all checked in:

- `.claude-plugin/marketplace.json` (power-platform-skills' format, read at `aed70bf`): `name` `powerops`, `owner.name` `PowerOps contributors`, `metadata.description`, and one entry `{ "name": "powerops", "source": "./plugins/claude" }`.
- `plugins/claude/.claude-plugin/plugin.json` (the model-apps shape): `name` `powerops`, `version`, `description`, `author.name`, `homepage`, `repository`, `license` `Apache-2.0`, `keywords`. No `skills` key: Claude Code scans the plugin's default `skills/`.
- `plugins/claude/skills/<skill>/SKILL.md`: the Claude Code copies, `ForClaude(rel, shipped)` for every file `SkillsCommand.Shipped()` returns.
- `.agents/plugins/marketplace.json` (Dataverse-skills' format, read at `1966192`): `name` `powerops`, `owner.name`, `interface.displayName` `PowerOps`, and one entry with `source` `{ "source": "local", "path": "./plugins/codex" }`, `policy` `{ "installation": "AVAILABLE", "authentication": "ON_INSTALL" }` and `category` `Developer Tools`.
- `plugins/codex/.codex-plugin/plugin.json` (the Dataverse-skills shape): `name`, `description`, `version`, `author`, `homepage`, `repository`, `keywords`, `license`, `skills` `./skills/`, and `interface` with `displayName`, `shortDescription`, `longDescription`, `developerName`, `category`, `capabilities` (`Read`, `Write`: the skills write plan files) and `websiteURL`. No privacy or terms links (PowerOps has none) and no icons.
- `plugins/codex/skills/<skill>/SKILL.md`: the shipped files unchanged.

`homepage`, `repository` and `websiteURL` are the repo's public GitHub address, the same as `RepositoryUrl` in `Directory.Build.props` (the test checks it); no tenant URL, id, email or user name anywhere. The owner part of that address is the public repo owner, already in `Directory.Build.props` and the BUILD-PLAN. Both descriptions say the `powerops` CLI must be on PATH (the skills' `compatibility` line says so too).

Why two folders and copies (decisions Q1):

- Claude Code's manifest `skills` key adds to the default `skills/` scan, it doesn't replace it, so one plugin can't serve the plain review skill to Codex and the `ForClaude` copy to Claude Code from one `skills/` folder.
- Neither agent loads a component from outside the plugin folder, and for a marketplace added from a local path Claude Code skips symlinks that leave the plugin folder. So the plugin folders hold real copies, as `powerops skills install` writes real copies.
- A plugin at the repo root (`source` `./`) would serve `skills/` itself, but both agents would then copy the whole repo into their plugin cache (Codex's `copy_dir_recursive` in `core-plugins/src/store.rs` copies every file and folder of the plugin root), and Claude Code's validator warns about the `CLAUDE.md` at a plugin root.
- One folder with both manifests, as Dataverse-skills has, would give Codex the Claude copy of the review skill. The Codex copy stays the shipped file, the same as `skills install` writes to `.agents/skills/`, so the shared copy keeps to the portable front matter (PLANNING-SKILLS, Portability). Since Codex's `skills` path replaces its default and Claude Code's adds to it, one folder would still need two skills folders, so it saves no copy.

Codex reads `.agents/plugins/marketplace.json` before `.claude-plugin/marketplace.json` (openai/codex `rust-v0.163.0`, `core-plugins/src/marketplace.rs`), so with both files in the repo it uses its own. In Codex a manifest `skills` path replaces the default `skills/` (`core-plugins/src/loader.rs`).

`npx skills add` (vercel-labs/skills `a80e4c9`, `src/skills.ts`) searches the root `skills/` before the folders named in `.claude-plugin/` manifests and skips a skill name it has already found, so it still installs the plain copies from `skills/`.

## Automation

**Generated copies.** The copies under `plugins/*/skills/` are written by the test in update mode, the same switch as the goldens: with `POWEROPS_UPDATE_GOLDEN=1`, `PluginManifestTests` deletes any file under `plugins/claude/skills/` or `plugins/codex/skills/` that is not a key of `Shipped()` (a renamed or removed skill), then writes `ForClaude(rel, shipped)` to `plugins/claude/skills/<rel>` and the shipped text to `plugins/codex/skills/<rel>` (LF, UTF-8 without BOM, as `skills install` writes). In a normal run it compares, and a mismatch names the file and says to rerun with `POWEROPS_UPDATE_GOLDEN=1`. So a change to `skills/` or to `ForClaude` fails the test until the copies are regenerated, and the copies can't drift from `skills install`.

**PluginManifestTests** (`tests/PowerOps.Cli.Tests/`, paths from `Golden.RepoRoot` and `Path.Combine`, JSON read with `System.Text.Json`):

- Claude marketplace: `name` and `owner.name` set; exactly one entry, `name` `powerops`, a `source` string that starts with `./` and has no `..`; the folder exists and holds `.claude-plugin/plugin.json` with the same `name`; the plugin `description` contains "`powerops` CLI" and "PATH"; `homepage` and `repository` equal `<RepositoryUrl>` in `Directory.Build.props`; no `mcpServers` key in the manifest and no `.mcp.json` in the folder.
- Codex marketplace: the same for `.agents/plugins/marketplace.json`, with `source.source` `local` and `source.path`; `policy.installation`, `policy.authentication` and `category` set; `.codex-plugin/plugin.json` with the same `name`, a `skills` path that starts with `./` and exists, `interface.displayName` set, the description check, `homepage`, `repository` and `interface.websiteURL` equal to `<RepositoryUrl>`, and no `mcpServers` key and no `.mcp.json` or `mcp.json` in the folder.
- Skills, for each plugin: the files under its skills folder are exactly the keys of `SkillsCommand.Shipped()`; each equals `ForClaude(rel, text)` (Claude) or `text` (Codex), CRLF-insensitive; each `SKILL.md`'s front-matter `name` equals its folder name. One assertion pins that the Claude review copy contains `context: fork`, so a `ForClaude` that stopped adding fields can't pass by comparing with itself.
- Versions: each plugin manifest's `version` equals `<Version>` in `Directory.Build.props`, and `release-please-config.json` lists each manifest under `extra-files` with `type` `json` and `jsonpath` `$.version`.

**Versions (decisions Q2).** release-please bumps the two plugin manifests: two `extra-files` entries `{ "type": "json", "path": "plugins/claude/.claude-plugin/plugin.json", "jsonpath": "$.version" }` and the same for `plugins/codex/.codex-plugin/plugin.json` (release-please's JSON updater, `docs/customizing.md`). The value starts at the current `<Version>`, `0.0.0`, and from then on only release-please changes it (`AGENTS.md` Rules say so too). A set `version` keeps Claude Code users who installed from GitHub on that version until the next release changes the string, so updates come with releases of the CLI the skills call. A fresh install still takes the default branch as it is then, labelled with the pinned version (Assumptions). A marketplace added from a local path loads in place and ignores the version. The marketplace files get no `version`.

**Validation.** `claude plugin validate --strict .` and `claude plugin validate --strict plugins/claude` (read-only) run once in the build, and the output goes in the verification record. They are not added to CI: CI has no Claude Code.

## Screens

None.

## Environments and route

- The build touches no Power Platform environment, and `powerops plan facts` is not needed.
- The manual runs use a local path (this worktree, quoted, since the path has a space), not the GitHub repo, because the files are not pushed. They don't touch the user's own setup (decisions Q3, changed after review R2):
  - Claude Code, with `CLAUDE_CONFIG_DIR` set to a new empty folder: `claude plugin marketplace add microsoft/power-platform-skills` and `claude plugin install model-apps@power-platform-skills` (a public clone from GitHub), then `claude plugin marketplace add "<worktree>"` and `claude plugin install powerops@powerops`; `claude plugin list` shows both plugins, and `claude plugin details powerops` lists the three skills.
  - Codex, with `CODEX_HOME` set to a new empty folder: `codex plugin marketplace add microsoft/Dataverse-skills` and `codex plugin add dataverse@dataverse-skills`, then `codex plugin marketplace add "<worktree>"` and `codex plugin add powerops@powerops`; `codex plugin list` shows both, and the installed copy under `<CODEX_HOME>/plugins/cache/powerops/powerops/` holds the three shipped skills.
  - Both folders are deleted afterwards, so nothing points at the worktree once it is cleaned up.
- The session checks need the user's login:
  - Claude Code: one `claude -p` in a scratch folder with `--plugin-dir "<worktree>/plugins/claude"`, which loads the plugin for that session only and saves nothing: it lists its skills whose names start with `powerops` and names the skill it would invoke to review an approved PowerOps plan. Expected: `powerops:powerops-plan`, `powerops:powerops-review-plan`, `powerops:powerops-implement`, and `powerops:powerops-review-plan` for the review.
  - Codex has no session-only plugin flag, and its login lives in the user's `CODEX_HOME`. So the same check installs the plugin in the user's own Codex setup and needs the user's yes in this session first; on yes: `codex plugin marketplace add "<worktree>"`, `codex plugin add powerops@powerops`, one `codex exec` with the same question, then, if the user wants, `codex plugin remove powerops@powerops` and `codex plugin marketplace remove powerops`.

## Solution and publisher

None.

## Constraints

- No licence, premium connector, table or DLP impact: the change is repo files, and the skills call the CLI under the user's own access. No deadline beyond the milestone.
- Open sources only: the Claude Code docs, Learn, power-platform-skills `aed70bf`, Dataverse-skills `1966192`, openai/codex `rust-v0.163.0` (Apache-2.0), vercel-labs/skills `a80e4c9` (MIT) and release-please's docs. Nothing copied from Microsoft's manifests but the field names.
- No tenant URLs, ids, emails or user names in any file. No MCP server in the plugins until Phase 5.
- No new dependency, option or command. `SkillsCommand` doesn't change. The embedded skills (`skills/**`) don't change.
- Never edit a manifest `version` by hand after this milestone; don't edit `Directory.Build.props`, `.release-please-manifest.json` or the changelog.
- Don't touch `src/PowerOps.Core/Drift/` (another session is fixing it).

## Assumptions

- `powerops plan facts` was not run: M13 creates no solution; `environment` stays null.
- The Phase 3b conflict checks don't change: the plugin carries the same three skills as `skills install`, with the same import rule; it adds no tool that writes to a tenant.
- In a plugin the skills are namespaced (`powerops:powerops-review-plan` in Claude Code). `powerops-plan` names `powerops-review-plan` without the prefix; the agent is assumed to match it. The session checks ask for the skill it would pick; they are not a full planning run.
- A repo that has both `skills install` copies and the plugin lists each skill twice (once namespaced); the README says to pick one.
- `0.0.0` pins Claude Code users from the GitHub marketplace at that version until the first release bumps it. A fresh install takes the default branch as it is then, which can be ahead of the released CLI.
- Until the first NuGet release, the CLI comes from a clone (README Install). The skills' "not found" message names `dotnet tool install --global PowerOps`, which fails until then; the skills don't change in this milestone, so the README plugin paragraph says to install the CLI first and links Install.

## Open questions

## Questions for your admin

## Steps

1. Write `PluginManifestTests` (Automation) and see it fail: no manifests, no copies.
2. Add the two marketplace files and the two plugin manifests (Data), and the two `extra-files` entries in `release-please-config.json`. Generate the copies with `POWEROPS_UPDATE_GOLDEN=1 dotnet test --project tests/PowerOps.Cli.Tests`, then `git checkout tests/golden/selftest.txt`, then a normal `dotnet test`. Check with `git status --short` that the new files are only the marketplaces and `plugins/`, and with `git diff tests/golden` that no golden changed.
3. Run `claude plugin validate --strict .` and `claude plugin validate --strict plugins/claude`; fix what they report.
4. Docs: in `README.md` Planning, after the `skills install` paragraph, the plugin install lines as shell commands (Claude Code: `claude plugin marketplace add <owner>/PowerOps`, then `claude plugin install powerops@powerops`, or `/plugin marketplace add` in a session; Codex: `codex plugin marketplace add <owner>/PowerOps`, then `codex plugin add powerops@powerops`), that the plugin needs the `powerops` CLI on PATH, installed first (link to Install), that `powerops skills install` stays the default for teams and why, and to use one or the other in a repo. In `AGENTS.md`: `plugins/` in Layout (generated copies), the two `plugin.json` files in the rule on versions, and in "Update goldens" that the update run also rewrites `plugins/*/skills/`. In `docs/PLANNING-SKILLS.md` Portability, one line on the plugin copies.
5. Run the manual runs in throwaway config folders and the Claude Code session check (Environments and route); record the agent versions and Microsoft's commits; delete the folders. Ask the user in this session for a yes to the Codex session check in their own setup; on yes run it and remove the plugin if they want; without a yes record it as not run.
6. Record M13 in `docs/BUILD-PLAN.md`: rewrite the M13 row's scope bullets to the layout built (two plugin folders with generated copies, the release-please version) and mark it built with a link to this plan; build-order task 33; the verification record (the validate output, the manual runs, the session checks as run or not run); and the "Last updated" date.
7. Run `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` and `powerops plan status`. Commit in Conventional Commits; no push.
