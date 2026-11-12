plan-hash: c6656af28cda37864863c9d641dcb5040ae1e37048109c5a10bdcb49551d5afc
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: the request did not say that the person confirmed a signed-in `pac`, so `powerops plan facts` was not run. No `pac`, `az` or tenant call was made. `powerops` was run as `dotnet run --project src/PowerOps.Cli --`. Tools the steps name were found: `dotnet`, `claude`, `jq`; the leak scan is `reports/leakscan.sh` in the main checkout (git-ignored, not in this worktree). HEAD is `bb21731`; it differs from `b065f23` only in this plan folder, so the Data rows' line numbers were checked against the files at HEAD. They match: `SKILL.md:36-46`, `:87`, `:117-145`, `:130`, `:149-162`, `:168-169`, `:184-188`; `references/data.md:33` (gate 7); `powerops-review-plan/SKILL.md:37` (item 3); `docs/PLANNING-SKILLS.md:153`, `:157-174`, `:178`, `:186`, `:249`; smoke `:249`. Checklist items 1, 2, 4, 5, 6, 7 and 8 do not apply: the plan changes skill text, docs and tests, and names no solution, connector, environment or role.

## Findings

### High

No findings.

### Medium

#### R1. New steps in sections 7 and 9 renumber the steps other lines point to
- Area: 9. Testability
- Finding: Row 4 adds a step after step 2 of section 7, and row 6 one after step 2 of section 9. That moves section 7's steps 3, 4 and 6 and section 9's step 4, which `SKILL.md:170` ("section 7 step 6"), `:173` ("section 7, from step 3"), `:174` ("section 7 step 4", "section 7 step 6", "section 9 step 4") and `:208` ("section 7 step 6") name. `SkillsCommandTests.cs:621` pins "Ask nothing else, except the sketch question (section 7 step 6)", so a stale number would keep passing. The fast path's A ("section 7, from step 3") would then run the defaults question, but Goal 2 says the fast path keeps the defaults with the approval. Rows 4 and 5 say how `quick` keeps them ("say so beside B" in section 6's menu), but say nothing about the fast path's own A/B menu (section 8 step 5).
- Suggested fix: Say in rows 4 and 6 that the new text is a sentence inside an existing step (for example, section 7 step 2 and section 9 step 2), so no number moves. Otherwise list each cross-reference and the pinned string at `:621` to update. Add to row 5 that the fast path's A and B keep the `default:` lines and log `defaults kept: <N>`, and pin that in row 12 (e).
- Resolution: open

#### R2. Under `quick`, the fallback Held question has no round to go in
- Area: 9. Testability
- Finding: Row 1's fallback asks "in the next round" which products the account holds. Under `quick` there is one round, and the cost tier and environments questions are already in it, so the fallback either opens a second round or goes unasked. Smoke scenario 5 fails both ways: `skills-smoke.sh:248` ("quick asked more than one round"), or `:251` if the question joins round 1 as a third question that names neither licences nor environments, alongside data question 1. Goal 5 covers the fast path (`unknown` plus a check step), but not `quick`.
- Suggested fix: In row 1, say that under `quick` the Held question is not asked: each row is written as `unknown`, with the check step, the same as in the fast path. Pin this in row 12 (a).
- Resolution: open

#### R3. Where the two batch questions are logged, and what confirming one changes, is not stated
- Area: 9. Testability
- Finding: Neither "Keep these N defaults?" (row 4) nor "Confirm these N assumptions?" (row 6) says how it goes into `decisions.md`. Section 5 says questions go under `## Round` headings and everything else goes under `## Log`. `check_decisions` (`skills-smoke.sh:123-138`) counts every `### Q` against the last `## Round`, fails a round with more than three, and needs option 1 marked recommended. If the agent writes a `### Q` after a full round, scenarios 1 and 6 fail. Row 6 sets findings to `Resolution: fixed` but adds no Log line with an `(answered by: ...)` mark, which section 9's "Log each acceptance" and section 5 expect. It doesn't exclude High findings, though only a person accepts a High. It also doesn't say whether the confirmed Assumptions line is rewritten. A rewrite changes the body hash and makes the review stale.
- Suggested fix: State the form in rows 4 and 6. For example, both are Log lines: `defaults kept: <N>` or `defaults changed: <labels>`, and `assumptions confirmed: R<n>, ...`, each with an `(answered by: ...)` mark and no `### Q`. Also say that no High finding goes into the batch, and that a confirmation leaves the plan body unchanged (or, if it changes it, that the sign-off will name the review as stale). Pin the Log forms in row 12 (d).
- Resolution: open

#### R4. The smoke turn budgets and the simulated user's goals do not cover the new questions
- Area: 9. Testability
- Finding: Under `standard`, scenario 1 (`skills-smoke.sh:145-149`, a budget of 12 plus 1, ending at sign-off) and scenario 6 (`:265-270`, plus 6, "three rounds, the sketch options, the pick, the approval and the sketch question") each gain one defaults turn at approval. Scenario 1 may also gain the assumptions batch before the sign-off menu, and the round for the neutral Held question. Row 13 adds only the `### Licences` grep. A run that ends with "did not finish within N turns" would be a failure that step 5 has to explain.
- Suggested fix: In row 13, add one turn to each budget comment for the defaults question in scenarios 1 and 6, and say whether scenario 1's goal answers the batch questions (recommended is the default). Or record in step 5 that a budget overrun caused by these questions is fixed in the script, not logged as unrelated.
- Resolution: open

#### R5. The recommended tier doesn't say what happens to a need with no standard alternative
- Area: 3. Premium connector and licence impact
- Finding: Row 3 defines "Not sure: standard first" only by "the standard alternative wherever the table has one". It doesn't say what happens to a need that has none (Dataverse-only features, a Copilot Studio agent, Power Pages). "Standard licences only" turns such a need into a question, and "Premium where needed" keeps it, so the recommended tier is the only one with no rule for this case. The executor would have to invent one in both `SKILL.md` and `licensing.md` section 1.
- Suggested fix: Add the rule to row 3. For example: "a need with no standard alternative keeps its premium row, with `Held` read or asked, and becomes an Open question if `Held` is no." Pin it in row 12 (c).
- Resolution: open

#### R6. "Safe default" is not defined
- Area: 10. Unanswered areas and the open-question count
- Finding: Row 3's "**Defaults.**" says that an item with a safe default is not asked, but nothing says what makes a default safe. An agent could default the very things M91 stops it assuming, such as licences held, data volume, security roles, or the environment and route. That would contradict Goal 1's rule that a recommended option never states a fact about the tenant, licences or data. It would also leave an area "never asked", so section 7 step 2 would add a confirmation of its own. Section 6's data round, which asks six questions under `standard`, does not say how it interacts with the rule either.
- Suggested fix: In row 3, add one sentence that defines a safe default (reversible, and no change to cost, security or data) and names what is never defaulted: the cost tier under `standard` and `deep`, `Held`, the environments question, security roles and the data round's questions. Pin the sentence in row 12 (c).
- Resolution: open

#### R7. The Developer Plan note in row 7 contradicts the repo's own record
- Area: 3. Premium connector and licence impact (ecosystem rule)
- Finding: Row 7 has `licensing.md` say that "the Developer Plan creates no developer environment from 2026-11-15". `docs/FOUNDATION.md:91` and ECO-0006 in `docs/ecosystem/changes.md` say that only unmanaged developer environments stop being created from that date, and that managed developer environments remain, one per user. The claim is also missing from the researcher's verified list in Assumptions. A wrong licence statement in the new single home for licence facts is the failure this milestone is meant to fix.
- Suggested fix: Reword the note to "unmanaged developer environments, including the Developer Plan's, can't be created from 2026-11-15; managed developer environments remain", source it to the same Learn page as ECO-0006, and add it to the verified list in Assumptions.
- Resolution: open

### Low

#### R8. The Held and tier rules have two homes, and the word ceiling has no budget
- Area: 11. Size
- Finding: Rows 1 and 3 write the `Held` values and the tier rules into `SKILL.md`, and row 7 writes them again into `licensing.md` section 1 ("the tier rules of rows 1 to 3"). Goal 6 calls `licensing.md` the one home. `SKILL.md` already has 2,869 words against a ceiling of 2,873 (`SkillsCommandTests.cs:462`). The quoted text of rows 1 to 6 adds roughly 500 words, and row 12 leaves the new ceiling as "plus N" with no N. `docs/PLANNING-SKILLS.md:153` says the references exist "so the core skill stays small".
- Suggested fix: Keep the `Held` mapping and the tier rules only in `licensing.md` section 1, with `SKILL.md` pointing to it (as `data.md` is pointed to), and pin them there. Name a word budget for N in row 12.
- Resolution: open

#### R9. The smoke script fakes `pac` but not `az`
- Area: 9. Testability
- Finding: The Environments and route section says the read never runs in the smoke because `pac` is a fake. The read uses `az`, though, and a real `az` is on PATH on this machine. Claude's `--allowedTools` (`skills-smoke.sh:18`) blocks it. The Codex run (`-s workspace-write`, `:57`) runs whatever its sandbox allows, and the scenarios that widen `allowed_tools` (`:385`, `:499`) don't name `az`, so they are safe today only by omission.
- Suggested fix: In row 13, add a fake `az` that is never signed in, next to the fake `pac` (`:35-37`), and correct the sentence in Environments and route.
- Resolution: open

#### R10. Docs that describe `az` use and planning reads are not in row 10
- Area: 10. Unanswered areas
- Finding: `README.md:21` and `:109` say `az` is needed only for `drift` and `deploy`. `docs/PLANNING-SKILLS.md:35` (principle 7) says planning may read environment facts, but the licence read is of the signed-in account, which is not an environment fact. Row 10 changes neither line.
- Suggested fix: Add both lines to row 10: `az` is optional for the planning licence read, and principle 7 names the one account read.
- Resolution: open

#### R11. The Graph row in row 11 has no "Where it is pinned" cell
- Area: 9. Testability
- Finding: `EcosystemDocsTests` requires six cells in every row of `compatibility.md` (`EcosystemDocsTests.cs:184-199`). Row 11 gives the `Azure CLI (az)` row its "where" cell, but gives the `Microsoft Graph licenseDetails` row only a version, limits and a date.
- Suggested fix: Name its "where" cell, for example ``skills/powerops-plan/SKILL.md` section 3, through `az rest``.
- Resolution: open

#### R12. The Power Pages part numbers are not named
- Area: 3. Premium connector and licence impact
- Finding: Assumptions lists every part number by name except "the Power Pages authenticated-user packs", so the executor would have to look them up again for `licensing.md` section 3. The plan doesn't say that this lookup was verified.
- Suggested fix: Name those part numbers in Assumptions from the researcher's CSV read, or leave them out and let such a tenant read as `unknown`, as the plan already does for the Copilot Studio credit pack.
- Resolution: open

#### R13. Row 12 (f) says "four headings", but row 7 names five
- Area: 9. Testability
- Finding: Row 7 lists `# Licences: ...`, `## 1. The table`, `## 2. Needs`, `## 3. Part numbers` and `## Sources`. Row 12 (f) pins "its four headings" and doesn't say which four.
- Suggested fix: List the pinned headings in row 12 (f).
- Resolution: open

#### R14. Step 5 runs "on the committed skills", but no step commits
- Area: 9. Testability
- Finding: Steps 2 to 4 don't commit. The smoke script builds from the working tree (`skills-smoke.sh:30`), so "committed" is either a missing commit step or a word too many.
- Suggested fix: Say which step commits, and how (Conventional Commits, local only), or drop "committed".
- Resolution: open

#### R15. The plan and decisions.md hold URLs
- Area: Tenant data
- Finding: Data row 1 in `plan.md` spells out a host and path for the Graph call. The `## Log` line on research in `decisions.md` holds about ten host-and-path references to public Learn and GitHub pages. They are public documentation, not tenant values, and `plan status` reports no violation, but this role reports every URL.
- Suggested fix: Accept as public sources, or replace them with the pages' titles and keep the paths in the compatibility rows and `licensing.md`'s Sources.
- Resolution: open
