plan-hash: 91b018f47230f86d39b05736208753db24fe70cb9097df081c9c55a06bf63ca8
model: claude-sonnet-5-5
fresh-context: yes

The plan-hash was updated after the text fixes for R1 to R14 below (the practice of the M7 to M12 reviews). Reviewed hash: 94647a9dccfac53e9c1d1e82565b10b370149a833160d061f03b0725180d55bb. The fixes changed Goal (done-when), Data, Automation, Environments and route, Constraints, Assumptions and Steps 2 to 6, and decisions Q1 and Q3.

## Findings

I read only `plan.md`, `decisions.md` and the repo. The plan creates no solution, so most of the Power Platform checklist does not apply; those items are listed at the end. I also built a prototype of the plan's layout outside the repo (in the session scratchpad, with isolated `CLAUDE_CONFIG_DIR` and `CODEX_HOME`) and ran the real tools on it: `claude plugin validate` passed on the marketplace root and on the plugin folder, also with `--strict`; `claude plugin marketplace add`, `install` and `details` listed the three skills; `codex plugin marketplace add`, `plugin add` and `plugin list` showed `powerops@powerops` installed. So the layout in Data works. The findings below are about what the plan leaves open.

### High

No findings.

### Medium

#### R1. "Listed beside Microsoft's" cannot be met as written
- Area: 9 Testability
- Finding: Done-when says the three skills are listed beside Microsoft's in both agents. "Environments and route" softens this to "if they are installed". On the build machine neither Claude Code (user scope) nor Codex had any Microsoft plugin installed when this review ran (checked `installed_plugins.json` and the plugin sections of the Codex config). Step 5 asks only for a yes to adding PowerOps, so as written the criterion either fails or is silently dropped. Adding Microsoft's marketplaces is a second change to the user's setup and a clone from the network.
- Suggested fix: Pick one. Either drop "beside Microsoft's" from Done-when and record "Microsoft's plugins not installed in the test setup", or say in Step 5 that the run also adds Microsoft's two marketplaces (to the isolated config of R2) and ask for the yes for that too. Make Done-when and "Environments and route" say the same thing.
- Resolution: fixed (Done-when and Environments and route agree: the manual runs install one of Microsoft's plugins from GitHub, model-apps in Claude Code and dataverse in Codex, in the same throwaway config, so the skills are listed beside them)

#### R2. The manual runs change the user's real setup, when an isolated one gives the same evidence
- Area: 5 Route and 9 Testability
- Finding: Step 5 adds a marketplace and a plugin to the user's own Claude Code and Codex, using the worktree as a local path. Three problems: (a) both CLIs honour `CLAUDE_CONFIG_DIR` and `CODEX_HOME`, and in my prototype the whole add, install, details and list sequence worked against a throwaway directory, so no yes about the real setup is needed for that part; (b) the worktree path contains a space, so every command in "Environments and route" needs the path quoted; (c) the marketplace entry records the worktree's absolute path, so if the worktree is cleaned up before the user answers the removal offer, the real setup keeps a marketplace that points nowhere. Codex warned about PATH aliases when its home was under the system temp directory, so use another location. The only part that needs the real home is a `codex exec` that lists skills (it needs the user's login).
- Suggested fix: Run the marketplace and plugin install, `claude plugin details powerops` and `codex plugin list` against throwaway `CLAUDE_CONFIG_DIR` and `CODEX_HOME` folders, and delete them afterwards. Keep the user's yes (Q3) for anything that touches the real home, such as the Codex `exec` listing. Quote the worktree path.
- Resolution: fixed (Q3 changed: the marketplace and plugin installs run in throwaway `CLAUDE_CONFIG_DIR` and `CODEX_HOME` folders, deleted afterwards; the worktree path is quoted; only the Codex session check touches the user's own setup and asks for the yes)

#### R3. The plugin is advertised before the CLI it needs can be installed
- Area: Other: claim check against README and the skills
- Finding: The README says "not yet released" and "Until the first release is on NuGet, install from a clone instead". When `powerops` is missing, `powerops-plan` section 2 tells the user to run `dotnet tool install --global PowerOps`, which fails until a NuGet release exists. Step 4 only adds "the plugin needs the `powerops` CLI on PATH". A person who installs the plugin from the README line first gets skills that stop at a failing command. The plan's Assumptions note the `0.0.0` pin but not this.
- Suggested fix: In the README plugin paragraph, link the Install section, say the CLI must be installed first, and say that until the first release that means building from a clone. Add a line to Assumptions that the first-run message in `powerops-plan` names the NuGet install, which is wrong until release (the skills are out of scope for this milestone, so a docs sentence is enough).
- Resolution: fixed (Steps 4: the README plugin paragraph says to install the CLI first and links Install; Assumptions records the skills' NuGet hint until the first release)

#### R4. Nothing exercises the skills through the plugin, and the cross-skill name is an unchecked assumption
- Area: 9 Testability
- Finding: `powerops-plan` section 7 tells the agent to "invoke the `powerops-review-plan` skill". Plugin skills are namespaced (`powerops:powerops-review-plan`); the plan assumes the agent maps the bare name and checks only a listing. Section 7 has fallbacks, so a miss degrades instead of crashing, but the milestone's whole purpose is that the plugin works. The existing smoke script installs through `skills install`, never through the plugin.
- Suggested fix: Either run one smoke case against the plugin (a scratch repo with `claude --plugin-dir plugins/claude`, or the plugin installed in the isolated config of R2) and record it, or write in the verification record that the runtime flow through the plugin was not run and name the namespacing assumption there.
- Resolution: fixed (a Claude Code session check with `--plugin-dir` asks for the skills and for the one it would pick to review a plan; the Codex one needs the user's yes; Done-when and Steps 5 say so)

### Low

#### R5. Regeneration never removes stale copies
- Area: 9 Testability
- Finding: Update mode writes `ForClaude(rel, shipped)` and the shipped text for every key of `Shipped()`. If a skill file is renamed or deleted, the old file stays under `plugins/*/skills/`, and the "files are exactly the keys of `Shipped()`" assertion then keeps failing after the documented regeneration. The failure message only says to rerun with the update switch, which cannot fix it.
- Suggested fix: In update mode, delete files under `plugins/claude/skills/` and `plugins/codex/skills/` that are not in `Shipped()` (and empty folders), or make the message say to delete the named file.
- Resolution: fixed (update mode first deletes files under the plugin skills folders that are not in `Shipped()`)

#### R6. Two plugin folders and two sets of copies: a shared folder was not weighed
- Area: 1 Boundaries
- Finding: Q1 compares two folders against a plugin at the repo root. A third shape was not considered: one plugin folder holding both `.claude-plugin/plugin.json` and `.codex-plugin/plugin.json` over one `skills/` folder with the `ForClaude` copies. Dataverse-skills (the repo the plan copies the Codex format from, at the cited commit) keeps both manifests side by side in one plugin folder. Codex's front-matter struct at `rust-v0.163.0` (`codex-rs/skills/src/parser.rs`) has no `deny_unknown_fields`, so it should ignore `context`, `background` and `model`. That would halve the copies and the test's skill checks. The cost is that the Codex plugin would carry three Claude-only front-matter lines, which differs from `skills install`'s Codex output. I did not run a Codex session to confirm the keys are ignored at load.
- Suggested fix: Record in decisions.md why the shared folder was rejected (for example "Codex files must equal the shipped files byte for byte"), or take it.
- Resolution: fixed (Data and decisions Q1 say why one folder with both manifests was rejected: Codex would get the Claude copy, and it saves no copy because the two agents' `skills` keys differ)

#### R7. Version pin: "moves with the released CLI" holds only for updates
- Area: 5 Route
- Finding: The plan says a set `version` keeps the plugin's skills moving with the released CLI. That is true for updates. A fresh `/plugin marketplace add` of the GitHub repo takes the default branch's head and labels it with the pinned version, so a new install can be ahead of any released CLI. Until the first release the value is `0.0.0` and there is no update path at all.
- Suggested fix: In the README, tell users to add the marketplace at a release tag once releases exist (both CLIs accept a ref: Claude Code `owner/repo@ref` or `#ref`, Codex `owner/repo@ref` or `--ref`), or state the skew in Assumptions.
- Resolution: fixed (Automation and Assumptions say the pin holds updates, and a fresh install takes the default branch as it is then)

#### R8. M13 row in BUILD-PLAN will describe a layout that was not built
- Area: Other: claim check against `docs/BUILD-PLAN.md`
- Finding: The M13 row says the plugin's `.claude-plugin/plugin.json` "serves the skills in `skills/`" and that "a plugin serves its files unchanged". The plan builds `plugins/claude/` and `plugins/codex/` with copies. Step 6 only says to mark the row built and link the plan.
- Suggested fix: Step 6 also rewrites those two bullets to the real layout, so the row and the repo agree.
- Resolution: fixed (Steps 6 rewrites the M13 row's scope bullets to the layout built)

#### R9. The "never edit the manifest version by hand" rule is only in the plan
- Area: Other: claim check against `AGENTS.md`
- Finding: `AGENTS.md` Rules say not to edit the version in `Directory.Build.props`, `.release-please-manifest.json` or the changelog. Step 4 adds `plugins/` to Layout and a goldens note but not the two manifests to that rule. Future agents read `AGENTS.md`, not this plan. The test catches a manifest that differs from `<Version>`, not an agent bumping both.
- Suggested fix: Add the two `plugin.json` files to that `AGENTS.md` rule in Step 4.
- Resolution: fixed (Steps 4 adds the two `plugin.json` files to the AGENTS.md rule on versions)

#### R10. `npx skills add` was not checked against the new files
- Area: Other: claim check against the README
- Finding: The README and PLANNING-SKILLS say `skills/` also works for `npx skills add`. After M13 the repo has `.claude-plugin/marketplace.json` and three copies of each skill, two of which differ for the review skill. That CLI's docs say it also discovers skills declared in `.claude-plugin` manifests and are silent on duplicate names. Our marketplace entry declares no `skills` key and `plugins/` is not one of its standard folders, so a clash is unlikely, but I could not run it.
- Suggested fix: Run `npx skills add <local path> --list` once with telemetry off and note the result in the verification record, or leave that sentence in the README unchanged only after it is checked.
- Resolution: fixed (checked in vercel-labs/skills `a80e4c9`, `src/skills.ts`: the root `skills/` is searched before manifest-declared folders and a name already found is skipped, so `npx skills add` keeps the plain copies; stated in Data)

#### R11. Validate without `--strict`; the validator also advises a README line
- Area: 9 Testability
- Finding: Step 3 runs `claude plugin validate` without `--strict`. On my prototype both runs passed, so `--strict` costs nothing and also fails on unknown fields. The validator printed advice (verdict unchanged) that the README has no line installing `powerops` from a marketplace, and suggested the one-step form `/plugin install powerops --marketplace <owner>/<repo>` (Claude Code 2.1.275 or later). Inside a session, `/plugin install name@marketplace` opens a panel rather than installing, so the plan's two-line README form is the slower path.
- Suggested fix: Run validate with `--strict` and record the output. In the README give the shell form (`claude plugin marketplace add`, then `claude plugin install powerops@powerops`) or the one-step form with a minimum version.
- Resolution: fixed (validate runs with `--strict` and its output is recorded; the README gives the shell form for both agents)

#### R12. `git diff` in Step 2 does not show new files
- Area: 9 Testability
- Finding: Step 2 says to read `git diff` to check that only `plugins/` changed and no golden did. Everything under `plugins/` and both marketplace files is untracked at that point, so `git diff` shows only the golden side.
- Suggested fix: Use `git status --short` for the new files and `git diff tests/golden` for the goldens.
- Resolution: fixed (Steps 2 uses `git status --short` for new files and `git diff tests/golden` for goldens)

#### R13. Licensing, DLP and deadlines never asked; Constraints is silent
- Area: 10 Unanswered areas, and items 3 and 4
- Finding: `decisions.md` logs "approved without: users, constraints". The skill says Constraints holds licensing, DLP and deadlines, and `quick` depth asks licensing and environments in its one round; Q1 to Q3 covered layout, versions and manual runs. The plan never says that no licence or DLP impact exists (true here: repo files only, the skills call the CLI under the user's own licences). Open questions: 0. Questions for your admin: none.
- Suggested fix: One line in Constraints: no premium connector, table, licence or DLP impact, and no deadline other than the milestone.
- Resolution: fixed (Constraints: no licence, premium connector, table or DLP impact, no deadline beyond the milestone)

#### R14. A GitHub handle in the plan, and the manifest URLs are not pinned
- Area: Other: tenant data and consistency
- Finding: Step 4's install lines contain the repository owner's GitHub handle (`owner/repo` form). It is the public repo address already in `Directory.Build.props` and the BUILD-PLAN, and `plan status` does not flag it, but the skill asks to report names. Separately, the plan says `homepage`, `repository` and `websiteURL` equal `RepositoryUrl`, but `PluginManifestTests` does not assert it, so a typo would pass.
- Suggested fix: Accept the handle as the public repo address, or write `<owner>/PowerOps` in the plan. Add one assertion that the three URL fields equal `<RepositoryUrl>` from `Directory.Build.props`.
- Resolution: fixed (the plan writes `<owner>/PowerOps`; the README keeps the public repo address, as `Directory.Build.props` does; the test checks `homepage`, `repository` and `websiteURL` equal `<RepositoryUrl>`)

## Checked and found fine

- Claude Code: the manifest `skills` key adds to the default `skills/` scan, `version` in `plugin.json` wins over the entry and pins updates, a marketplace added from a local path loads relative plugins in place, symlinks leading outside a plugin are rejected, and `CLAUDE.md` at a plugin root draws a warning (docs, plus my prototype).
- Codex `rust-v0.163.0`: `.agents/plugins/marketplace.json` is read before `.claude-plugin/marketplace.json`; a manifest `skills` path replaces the default; `policy` values `AVAILABLE` and `ON_INSTALL`, `local` sources and `./` paths match the code; `version` is optional.
- Both Microsoft marketplace files match what the plan says about their format at the cited commits.
- release-please: the `extra-files` entry shape (`type` `json`, `path`, `jsonpath`) is documented, and its JSON updater replaces the version inside the matched string.
- `ForClaude` adds exactly `context: fork`, `background: false` and `model: opus`; `Shipped()` keys are `<skill>/SKILL.md` with LF; `Golden.RepoRoot` and the update switch work as the plan describes.
- All CLI commands named in Steps and "Environments and route" exist in Claude Code 2.1.303 and Codex 0.163.0 (`claude plugin details`, `validate`, `marketplace add`/`remove`; `codex plugin add`/`list`/`remove`, `marketplace add`/`remove`).
- Checklist items 1 to 8 (solution boundary and prefix, connection references and environment variables, premium licence, DLP, Dev to Test to Prod route, managed or unmanaged, delegation, security roles) do not apply: no solution, no environment, no tenant data. `powerops plan facts` was not run for that reason.

## Not checked

- Codex "copies every file, `.git` included" when a plugin sits at the repo root (not read from source).
- Whether either agent, in a live session, loads and runs the skills from the plugin (see R4); whether Codex ignores the Claude-only keys at load (R6).
- release-please's `jsonStringify` helper (indentation and trailing newline are not shown in the source I read) and what the first release does to the two manifests.
- `npx skills add` behaviour (R10).
- The new test on Windows and macOS CI (line endings after checkout); it is described as CRLF-insensitive.
- The planning chat and the master session's brief are not in this context, so Q3's reasoning is judged from `decisions.md` alone.
