plan-hash: ca77618e0db8cbe58fa7ef20dc8c1a59fae1067887bc806fa48941d81ac942c0
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: no `pac` sign-in was confirmed for this review, so `powerops plan facts` was not run, and the plan's `environment:` is null (offline, no tenant).

Tools checked: `powerops`, `dotnet`, `git` and `gh` are on PATH; the leak scan `reports/leakscan.sh` and the chain rules exist in the main checkout (git-ignored, not in this worktree); the `fable-reviewer` subagent and `powerops-review-plan` are available. Nothing a step names was found missing.

Checked against the repo at `1dea7e7`: applying Data rows 1 and 2 to `skills/powerops-implement/SKILL.md` in a scratch copy gives 5,392 words (`str.split`, as `wc -w`), ASCII only, with no "in one run" left, which matches Done lines 1 and 3. All five clauses trace to `docs/plans/m35-live-checks/report.md` (lines 74, 94 to 96, 98, 108, 111), so Row 3's "the M35 run of 2026-10-30 and 2026-10-31" holds for each. The csproj embeds `skills/**/*`, so the new `known-issues.md` ships and reaches `SkillsCommand.Shipped()` and the plugin copies with no code change. No other test or doc quotes the moved text: only lines 353 to 355 of `SkillsCommandTests.cs` pin it. The stale "3,615 words" in `docs/PLANNING-SKILLS.md` (line 154) is there for step 4.

Checklist items 1 to 8 and 11 have nothing to check: the plan names no solution, connector, table, flow, screen or role, and says so (`None` sections, offline route). No URL, GUID, email or person's name is in `plan.md` or `decisions.md`.

## Findings

### High

No findings.

### Medium

No findings.

### Low

#### R1. The pointer's "these warnings" doesn't say which warnings
- Area: 9 Testability
- Finding: Data row 2 puts the pointer after clause 3's paragraph and says `known-issues.md` "records what was seen in the run behind these warnings". Section 3, step 3 holds many other warnings above it (the flow-save membership, the undocumented `MSCRM.SolutionName` header, the business-rule maker-portal step) that the file doesn't back. An agent can't tell from the pointer which warnings the file covers.
- Suggested fix: Name them in the pointer, for example "behind the compile, gallery and table warnings above", or accept the wording as it stands.
- Resolution: accepted. The pointer follows the three paragraphs it backs, and each bullet of `known-issues.md` names its rule (decisions.md Log).

#### R2. "Cut at the end of its paragraph" is unclear for a bullet
- Area: 9 Testability
- Finding: Done line 2 bounds the `because` check on "the gallery bullet in `known-issues.md`", cut "at the end of its paragraph so it can't read into a neighbour". Row 3 lays the file out as consecutive bullets. If the bullets have no blank line between them, a cut at the next blank line runs on into the bullets after it, which is the read into a neighbour the line rules out.
- Suggested fix: Say the bullet's span ends at its own line end (the next `\n`), or that bullets are separated by blank lines.
- Resolution: accepted. The `known-issues.md` span is cut at the end of the bullet's own line and the `SKILL.md` span at the end of its paragraph; the work review checked both (decisions.md Log).

#### R3. The gallery recovery doesn't say the crashed compile is lost
- Area: 9 Testability
- Finding: The new rule in Data row 1, clause 2 says to compile the screen first and the gallery alone after it if Studio crashes. The M35 report (Crash bullet) says what was compiled was lost and the app reopened on its last save. The rule doesn't say that, so an agent may retry the split on top of work it thinks was compiled. This is the plan's own new wording; the old text didn't say it either.
- Suggested fix: Optionally add "reopen the app from its last save" before the split, and recount Done lines 1 and 3. Or accept it as written, since "look at Studio after each compile" and the saved-time check already cover it.
- Resolution: accepted. Left by decision: "look at Studio after each compile" covers seeing the loss, and the account in `known-issues.md` says what was lost (decisions.md Log).
