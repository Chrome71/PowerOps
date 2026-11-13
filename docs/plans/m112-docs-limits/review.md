plan-hash: 19520f7f5f5285475334e434c4a398a70bb79255aaf0f5197a15149591b8277f
model: claude-opus-5-5
fresh-context: yes

Second pass, on revision 2 (`fd55ee8`). The first pass reviewed plan-hash `de4d7a34131fb40de15c9fb602805df46cdb352902c194e5758cb9ad22ac27ac`. This pass gives R1 to R9 their status and adds R10 and R11.
Review state: open. R1 to R9 are resolved; R10 (Medium) and R11 (Low) are open.
Environment facts were not read: the request gave no confirmation that `pac` is signed in to the plan's `environment:` (it is `null`; tenant offline), so `powerops plan facts` was not run.
Tools: `powerops` and `dotnet` were found with `command -v`. The leak scan named in Done when 2 exists at that path in the main checkout, and its usage line is `<base> <head>`. The Fable plan review is a session-level reviewer, not a tool on this machine's path, and was not checked.
Checked at `ccf9833` (`origin/main`): `PlanChecker.ReadReports`, `Reviews`, `PlansUnder` (no status filter) and `ReviewProblems`; `DeployReviews.Fault`, `Ready` and `Cover`; `action/check-apply.sh`; both example workflows (four `Chrome71/PowerOps@cbc31e7` pins, "main on 2026-11-10"); `SkillsCommand.cs`; `skills/powerops-implement/SKILL.md`; `plugins/` (no hook file, no `hooks` key); the anchors `#deploy-plan` (its `### Output`), `#sign-in-state`, `#the-steps`, `#gate-microsofts-imports` and `#github-environments-and-secrets`; and each target line of rows 1 to 11 (`docs/COMMANDS.md` 722 and 723, `docs/examples/README.md` 9, 21, 44, 87, 108, 148 and 271), with the text around it.
Checklist items 1 to 8 and 11 (solution, connections, licences, DLP, route, managed state, delegation, roles, size) don't apply: the plan changes this repo's docs and names no Power Platform artefact. `Won't build` is present. The plan and `decisions.md` hold no URL, GUID, email address or person's name.

## Findings

### High

No findings.

### Medium

#### R1. Row 1 doesn't say that the plan's status and sign-off are not read
- Area: 13. Buildability (Done when 1); 10. Unanswered areas
- Finding: Row 1 covered the open finding of E-1, not that the cover never reads `plan.md`'s header or `review.md`.
- Suggested fix: Add a sentence that the check reads neither the plan's status, nor its sign-off, nor its `review.md`.
- Resolution: resolved (revision 2). Row 1 now says "It doesn't read the plan's `status:`, its sign-off or its `review.md` either: only the plan body's hash." This holds at `ccf9833`: `PlanChecker.Reviews` passes only the body hash, and `PlansUnder` filters no status. `decisions.md` records the E-2 scope choice.

#### R2. Row 1 uses "plan hash" for the deploy plan, beside `plan-hash:` for the PowerOps plan
- Area: 13. Buildability
- Finding: "the plan hash covers it" could be read as the hash of `plan.md`.
- Suggested fix: Name the deploy plan's hash.
- Resolution: resolved (revision 2). The text is now "the plan file's hash covers it ([deploy plan](#deploy-plan), Output)". `deploy apply` already calls the `deploy plan --save` output "the plan file", and `### Output` exists under `## deploy plan`.

#### R3. Row 3 says more than its basis about what keeps a builder from editing a settings file
- Area: 13. Buildability
- Finding: "only their instructions keep a builder from editing ..." made a claim about every client.
- Suggested fix: Claim only what PowerOps adds, and name the client's settings file.
- Resolution: resolved (revision 2). Row 3 now says "PowerOps adds nothing that keeps a builder from editing a report, a hook or the client's settings file", and says that the client's own prompts and deny rules are not checked. R10 covers a new clause in the same sentence.

#### R4. Row 4's Prod route has no approval in a private repository without required reviewers
- Area: 5. Route; 13. Buildability
- Finding: Prod was sent through "the Action's apply job" with no condition.
- Suggested fix: Require reviewers on that job's environment, and give a fallback.
- Resolution: resolved (revision 2). The route is now "an Action apply job whose GitHub environment has required reviewers", or, where the reader's GitHub plan gives none, "an account no agent runs as". Its link `#sign-in-state` exists in `docs/HANDOFF.md`. R11 covers the word "account".

#### R5. Row 6's remedy covers Pro and Team, not Free with a private repository
- Area: 13. Buildability; 9. Testability
- Finding: "drop the `apply` job" didn't make the example run on Free with a private repository.
- Suggested fix: Say that the example doesn't run there, or limit the remedy to Pro and Team.
- Resolution: resolved (revision 2). Row 6 adds "So on Free the example doesn't run in a private repository as written" and limits the remedy to "on Pro or Team without reviewers". A new bullet for line 271 extends the reviewer condition to the ephemeral recipe's prerequisites.

#### R10. Row 3 says the roles' instructions forbid editing a report, but no instruction does
- Area: 13. Buildability (Done when 1: each line states only what `main` does)
- Finding: Revision 2's row 3 says that PowerOps adds nothing that keeps a builder from editing "a report, a hook or the client's settings file: the roles' instructions say not to". For hooks and settings this holds: `skills/powerops-implement/SKILL.md` says "never edit a hook, a permission rule or a setting to get past it". For reports it doesn't. No instruction tells the builder not to edit `verify.md` or `security-solution.md`, and the same skill tells it to edit one: "A violation that names `verify.md` or `security-solution.md` is fixed by replacing the value in that report with a placeholder". The clause gives the reader a protection the skills don't contain, on the very point (a builder editing a report the cover trusts) that the paragraph exists to make clear.
- Suggested fix: Make the clause true as written: "the builder's instructions say never to edit a hook, a permission rule or a setting to get past the gate, and nothing in them stops it from editing a report; the gate doesn't block such an edit, ...". Or drop "the roles' instructions say not to" and keep the rest of the sentence.
- Resolution: open

### Low

#### R6. The same stale pin date stays in `docs/examples/README.md`
- Area: 13. Buildability
- Finding: Line 108 said "as of 2026-10-29".
- Suggested fix: Add it to row 9.
- Resolution: resolved (revision 2). Row 9 now changes line 108 to 2026-11-10. The basis is the four `uses:` lines of the two example workflows, checked.

#### R7. The example workflows' comments still say the environments have required reviewers
- Area: 13. Buildability
- Finding: The `.yml` comments (`promote-dev-to-test.yml` lines 6 and 162, `ephemeral-pr.yml` line 28) were outside Done when 3.
- Suggested fix: Add a row for them, or list them under `Won't build` with a reason.
- Resolution: resolved (revision 2). They are under `Won't build`, with the reason that they describe the setup the README asks for, which row 5 now says is the reader's step.

#### R8. The leak scan is named only as "chain rule step 9", with no path
- Area: 12. Tools; 9. Testability
- Finding: Neither Done when 2 nor Step 3 gave the script's path.
- Suggested fix: Name the path, and say it is git-ignored.
- Resolution: resolved (revision 2). Done when 2 now gives `bash reports/leakscan.sh origin/main <branch>`, run from the main checkout, and the script's usage line matches.

#### R9. The plan holds URLs
- Area: Values in the plan
- Finding: Assumptions gave the two GitHub pages as addresses.
- Suggested fix: Name the pages by title only.
- Resolution: resolved (revision 2). Assumptions names the pages by title. A scan of `plan.md` and `decisions.md` finds no URL, GUID or email address.

#### R11. Row 4's "account" can be read as a Microsoft account, which undoes the advice
- Area: 13. Buildability
- Finding: Row 4 says the agent "can use every `pac` and `az` sign-in of the account it runs as", and it sends the Prod import to "an account no agent runs as". The account meant is the operating-system user whose `pac` auth profiles and `az` profile the agent reads (`decisions.md`: "pac's auth profiles are shared"). In a page about tenant sign-ins, "account" reads naturally as an Entra or GitHub account. A reader who signs `pac` in to Prod with a second Microsoft account, under the same operating-system user, follows the words and still leaves Prod's sign-in in the agent's reach.
- Suggested fix: Write "the operating-system user it runs as" (or "the computer account it runs as") in both places, for example "... run the Prod import as an operating-system user no agent runs as".
- Resolution: open
