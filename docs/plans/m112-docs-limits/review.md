plan-hash: de4d7a34131fb40de15c9fb602805df46cdb352902c194e5758cb9ad22ac27ac
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: the request gave no confirmation that `pac` is signed in to the plan's `environment:` (it is `null`; tenant offline), so `powerops plan facts` was not run.
Tools: `powerops` and `dotnet` were found with `command -v`; `powerops-review-plan` and `powerops-researcher` are in this repo. The Fable plan review was not checked (a session-level reviewer, not a tool on this machine's path).
Checked at `ccf9833` (`origin/main`): `PlanChecker.ReadReports` and `Reviews`, `DeployReviews.Fault`, `Ready` and `Cover`, `action/check-apply.sh`, both example workflows, `SkillsCommand.cs`, `RoleEvalTests`, `plugins/` (no hook file, no `hooks` key), and each target line of rows 1 to 11 with the text around it.
Checklist items 1 to 8 and 11 (solution, connections, licences, DLP, route, managed state, delegation, roles, size) don't apply: the plan changes this repo's docs and names no Power Platform artefact. `Won't build` is present.

## Findings

### High

No findings.

### Medium

#### R1. Row 1 doesn't say that the plan's status and sign-off are not read
- Area: 13. Buildability (Done when 1: the line states what `main` does); 10. Unanswered areas
- Finding: Row 1 is filed under E-1, and the Assumptions name E-1 as "an open High, an unsigned plan", but the new text covers only the open finding. At `ccf9833` the cover never reads `plan.md`'s header or `review.md`: `PlanChecker.Reviews` takes only the body hash, and `DeployReviews.Cover` uses state, staleness and the two hashes. Next to "What a cover binds is ... the plan body (by `plan-hash:`)", a reader can take the cover to need an approved, reviewed and signed-off plan, which is the misreading this milestone exists to remove.
- Suggested fix: Add to row 1's text, after the sentence on `ready`: "Nor does it read the plan's status, its sign-off or its `review.md`: reports written for a draft or unsigned plan cover the zip as long as `plan-hash:` is the body's current hash." Keep the M114 hand-over note.
- Resolution: open

#### R2. Row 1 uses "plan hash" for the deploy plan, beside `plan-hash:` for the PowerOps plan
- Area: 13. Buildability (one line, two meanings)
- Finding: The sentence before the list says the cover binds "the plan body (by `plan-hash:`)", the PowerOps plan. The first bullet then says "the settings file: the plan hash covers it", which means the hash of the `deploy plan` file (`docs/COMMANDS.md`, deploy plan, Output: "each settings file's bytes"). In the same page, the `deploy apply` agent text ("a changed plan hash (the `signed-off-hash` of the PowerOps plan behind the build)") uses "plan hash" for the PowerOps plan. A reader can conclude that `plan.md`'s hash covers the settings file, which is false.
- Suggested fix: In row 1's first bullet, write "the deploy plan's hash covers it (`deploy plan`, Output), so a plan file made before ...".
- Resolution: open

#### R3. Row 3 says more than its basis about what keeps a builder from editing a settings file
- Area: 13. Buildability (a claim with no source)
- Finding: "only their instructions keep a builder from editing a report, a hook or a settings file" is a claim about every client and every user setup. The plan's own basis line says it makes "No claim about a client's settings-file protection". A client can ask before or refuse an edit to its own settings files, and a person can add deny rules for edits (the same `docs/HARNESS.md` table names deny rules as a control). What the basis does support is that PowerOps adds none (`docs/USE-WITH-AGENTS.md`: "PowerOps adds no tool narrowing on any client"). "A settings file" is also ambiguous next to the deployment settings files the other docs mean by that phrase.
- Suggested fix: Reword the second sentence to "... so PowerOps adds nothing that keeps a builder from editing a report, a hook or the client's settings, and the gate doesn't block such an edit: only the roles' instructions, and any deny rule you add yourself, do."
- Resolution: open

#### R4. Row 4's Prod route has no approval in a private repository without required reviewers
- Area: 5. Route (the reader's Prod route); 13. Buildability
- Finding: Row 4 tells the reader to "import into Prod through the Action's apply job". Rows 5, 6 and 8 say that on GitHub Free, Pro and Team in a private repository the apply job's environment has no required reviewers, that it then imports with no approval, and that the reader should "drop the `apply` job and run the import yourself". Together, the reader with a private repository on those plans gets either a Prod apply job with no approval or a Prod import from their own shell, which `decisions.md` (B-8) says is not shown to be out of the agent's reach. The example linked from row 4 also promotes to Test only, so it shows no Prod job.
- Suggested fix: Make row 4's last clause "and import into Prod through an Action apply job whose environment has required reviewers ([the example](examples/README.md)); where your plan gives none, run the Prod import yourself from a shell the agent doesn't run in", or say that no route the docs show keeps Prod's sign-in from the agent there.
- Resolution: open

#### R5. Row 6's remedy covers Pro and Team, not Free with a private repository
- Area: 13. Buildability; 9. Testability
- Finding: Row 6 (line 87) states that Free "can't configure environments for a private repository at all", then gives one remedy: "without reviewers, drop the `apply` job and run the import yourself". The `plan` job also runs in an environment (`environment: test-plan`), reads its environment secrets (`DEV_URL`, `TEST_URL`, `AZURE_CLIENT_ID` and the rest) and signs in with a federated credential bound to `test-plan`. On Free with a private repository, that environment and its secrets aren't available either, so dropping the `apply` job alone doesn't make the example run there.
- Suggested fix: Extend the line: "On Free, a private repository can't hold the `test-plan` environment or its secrets either, so the example doesn't run there as written." This states only what the Assumptions record. Or limit the remedy sentence to Pro and Team.
- Resolution: open

### Low

#### R6. The same stale pin date stays in `docs/examples/README.md`
- Area: 13. Buildability (Done when 1)
- Finding: Row 9 corrects "the pin ... `main` as of 2026-10-29" in `docs/GITHUB-ACTION.md`, but `docs/examples/README.md` line 108 ("the example pins `main` as of 2026-10-29") still says it. The workflow pins `cbc31e7`, "main on 2026-11-10" (`promote-dev-to-test.yml` line 94). Rows 5 to 7 already edit that file.
- Suggested fix: Add a row that changes line 108's date to 2026-11-10, or record in `Won't build` why it stays.
- Resolution: open

#### R7. The example workflows' comments still say the environments have required reviewers
- Area: 13. Buildability
- Finding: Row 5 corrects the README's "`test`, which has required reviewers, so it waits for an approval". The workflow comments a reader copies still say the same: `promote-dev-to-test.yml` line 6 ("in a GitHub environment with required reviewers") and line 162 ("required reviewers: the job waits here for an approval"), and `ephemeral-pr.yml` line 28. Done when 3 doesn't allow those files.
- Suggested fix: Either add a row that makes those comments conditional ("add required reviewers to this environment: without them the job doesn't wait"), or list them under `Won't build` with the reason.
- Resolution: open

#### R8. The leak scan is named only as "chain rule step 9", with no path
- Area: 12. Tools; 9. Testability
- Finding: Done when 2 and Step 3 rely on "the leak scan of chain rule step 9". No plan line names its path or the chain-rules file. The script is not in this worktree: `reports/` is excluded by `.git/info/exclude` and exists only in the main checkout.
- Suggested fix: In Step 3, name the script by its path relative to the main checkout, and say that it is git-excluded and so not in the worktree.
- Resolution: open

#### R9. The plan holds URLs
- Area: Values in the plan
- Finding: The first Assumptions line gives the two GitHub documentation pages as addresses (`<url>`, `<url>`). They are public documentation, not tenant values. Row 6 already replaces them with placeholders.
- Suggested fix: Name the pages by title only in Assumptions, as row 6 does, and let the executor take the addresses from the pages.
- Resolution: open
