# Decisions: m63-live-rerun

Answered by the building session under the person's delegation of 2026-10-22 (product and route choices within the brief), each with the recommended option. None of them is a sign-off of the scratch plan, an acceptance of a High finding or of a security gap, or a yes to a tenant write: those stay the person's, in the run.

## Round 1

### Q1. Which yes does the import use?
1. The MCP `deploy_apply` form, answered by the person in an interactive client (recommended)
2. The Action's apply job in the test repository's protected `test` environment
Answer: 1. The form is the route no live run has reached: the first run used the CLI, and M52's live check used the Action and lists the form as not checked. The Action route ran in M52, and its finding (one app registration holds federated credentials for both `test` and `test-plan`, so the apply credential isn't bound to the protected environment) needs an Entra change in the person's tenant, not a rerun. D1 names both routes; this run checks the one with no evidence.

### Q2. Which client shows the form?
1. A new Code-tab session of the desktop app in the scratch repo; then `claude` in a terminal with its folder trust prompt answered first; if neither shows the form, stop and ask the person (recommended)
2. `claude` in a terminal only
3. This orchestrating session
Answer: 1. HARNESS.md lists interactive Claude Code (form shown, M20) but not the desktop app, which is the client the person uses; trying it first records its support status, and the terminal client is the documented fallback. This session is not used: its PowerOps server started from the old build before the reinstall, and a server the agent started for itself is not what a user runs. The folder trust prompt is answered first because it stopped the form in M19 and held the first run up for over two hours. The Action route is not a planned fallback (review R7): it would need the deploy folder pushed to the test repository with the settings built from repository variables, and it carries M52's credential and one-account findings; if no client shows the form, the person decides between that route, with those costs named, and recording the form as not reached.

### Q3. What does the run build?
1. A new small solution in Dev, with the existing `pops` publisher: one table, environment variables, one cloud flow with a Dataverse connection reference, a model-driven app and one custom role; no canvas app; the planning child shapes it (recommended)
2. A version 2 of the first run's `EquipmentCheckout`, upgrading Test
3. The first run's product again, canvas app included
Answer: 1. A new solution exercises the documented route from a plain request and doesn't depend on whether the person has removed the first run's leftovers. Option 2 needs those leftovers and inherits their known role findings. The canvas app's known gaps (Canvas Authoring can't add a data source, a compile isn't a save) are unchanged by P0, cost the most hand help in the first run, and canvas play is blocked by the person's expired Power Apps trial, so option 3 adds cost and no P0 evidence. The request the planning child gets: "a small room-booking app for the office team: staff book a room for a date range; a booking without an end date gets one N days after its start, where N is a setting; staff use a model-driven app; build it in Dev and move it to Test". M61's `--redact` check runs on an offline copy of the export with fake URLs added, not on the product (review R6, R20): no rule quotes a variable's default, PO006 quotes a URL in a flow, and a URL default in the product would give the review roles a PO007 finding the product doesn't need.

### Q4. How are Microsoft's skills loaded?
1. With `--plugin-dir` for each child session, from the person's local clones of power-platform-skills and Dataverse-skills, at their recorded commits (recommended)
2. Installed at user scope from their marketplaces
3. Through the person's router skill, which reads them without registering them
Answer: 1. The clones hold the plugins in their published layout (`.claude-plugin/plugin.json`, skills, hooks, MCP servers), so `--plugin-dir` runs them as plugins, as their READMEs' install does, for that session only. Option 2 changes the person's Claude Code configuration for every session. Option 3 is not how a user installs them, and its skills' frontmatter is not enforced. The record names the commits and says the install differs from the README in scope only. Their telemetry is the person's choice (preflight).

### Q5. How are the `powerops` tool and plugin installed?
1. The tool from a clone of `main` at a recorded commit with the README's lines, and the plugin reinstalled at user scope (uninstall, then install), after the person's yes (recommended)
2. The children load the clone's `plugins/claude` with `--plugin-dir`, and the person's install stays as it is
Answer: 1. The installed tool predates M56 (`--version` prints no commit) and the installed plugin is the first run's commit, so both must move for the run to test P0. `plugin update` keeps a `0.0.0` build (M32), so reinstall. Option 2 would load two plugins with one name, and the form session the person opens would still run the old skills. No release exists yet, so the commit is the build's identity (M56).

### Q6. How are M50 and M51 checked for agents?
1. Offline, on the first run's two real agent exports, kept in this session group's notes folder (`agent-exports-2026-11-04/`), and an offline copy of the first with the MCP tool's folder removed, as the first run made it; a new agent build only if the person asks (recommended)
2. Build a new agent in Copilot Studio and export it twice
Answer: 1. Those exports are the bytes on which the first run's `diff` said "no changes"; running the merged `diff` and `analyze` on them checks the fix on real input at no tenant cost. A new agent build is a browser-only route that P0 didn't change and costs the person's time; it stays optional.

### Q7. How fine is the person's yes for tenant writes?
1. One yes per build step, naming the objects the step creates or changes; a write outside that list needs a new yes; deletes never (recommended)
2. A yes for every single API call
3. One yes for all Dev writes, as in the first run
Answer: 1. The brief asks for a yes before every tenant write. Per call would be dozens of prompts for one table, which trains the person to say yes without reading. One blanket yes is what led the first run's build agent to delete two of its own drafts unasked, and its orchestrator to answer for the person. A step's list is what the person can check, and `writes.md` records which yes covered each write.

### Q8. Who signs off the scratch plan, and who answers its questions?
1. The person signs off, and accepts any High finding or security gap; the session answers product choices only, marked `agent`, and relays everything else word for word (recommended)
Answer: 1. D3: an agent never signs off, accepts a High finding or a security gap. The first run's orchestrator did all three in the person's voice; M54 now marks each answer, and `plan status` warns on an agent's sign-off, which this run must not trigger.

### Q9. Is there a second import (a version 2)?
1. No: a `deploy plan`, not saved and never imported, of an offline copy of the managed zip with its version raised by one shows M53's coverage item, and the unmanaged folder against the managed zip shows `same-version` (recommended)
2. Build a version 2 in Dev and upgrade Test
Answer: 1. The coverage item is added only to the upgrade of a managed zip (`DeployPlan.AddUpgrade`), so it needs a higher version than Test holds, planned after the import (step 9); the plain re-plan of the imported zip is a `skip` (review R1). A copy with a raised version is planned against the real target, so the item is read from Test's real state, and the copy is never imported, so it costs no write. `same-version` needs two inputs of one version that differ, which the route already has. A second import is another Test write and another form for the same evidence.

### Q10. Where does the record live?
1. `reports/m63-live-rerun/` in this worktree (git-ignored), with a copy of the scrubbed record in the session notes folder outside the repo (recommended)
2. Only in the worktree
Answer: 1. `reports/` is git-ignored, as the brief asks. A worktree cleanup once lost live captures because ignored files went with it, so a second copy lives outside the repo. Only the scrubbed record goes there; the raw exports and transcripts stay in the scratch repo.

### Q11. What does the Windows runbook cover?
1. Install, `--version`, the offline commands on a copy of the run's export and deploy folder, a new `deploy plan` against Test, the gate's allow path with `C:\` paths, the Playwright set-up with its `icacls` hand check and the model-driven test, and a form on that new plan that the person declines; no tenant write (recommended)
2. The whole route again on Windows, with a second build and import
Answer: 1. It checks what is Windows-specific (install, paths and quoting, the gate on Windows, the ACL step M61 added, the form on Windows) without a second build or import. A declined form checks that a decline imports nothing, which the Linux run doesn't test. The Linux plan file no longer matches Test after the import, so `deploy_apply` would refuse at its dry run and send no form; a plan made on Windows after the import is `skip` only and reaches the form (review R4). The copy holds Test's connection id, so the person moves it by their own means. The runbook is drafted before the import, so the person can run it right after, inside Test's term.

## Log

- 2026-11-05: plan started at the request of the session that started this one: build M63 of the live-test improvement plan, with D8 as the person answered it
- read on 2026-11-05: AGENTS.md; the M63 section of the improvement plan, D1 to D9 with the answers; the first run's record and hand-over; the Fable and Astra reviews (A-3, A-11, C-9, R3 mentions); `docs/HANDOFF.md`, `docs/HARNESS.md`, `docs/RECOVERY.md`, `docs/examples/playwright/README.md`; the M32 plan folder and its Windows run notes; the M52 plan's route and `live.md`; the M58 plan's route; the M62 plan folder (layout)
- read-only local checks on 2026-11-05: the installed `powerops` prints `0.0.0` (no commit), the installed plugin records the first run's commit, no GitHub release exists, Microsoft's plugins are local clones with plugin manifests, hooks (telemetry) and MCP servers
- depth: quick: the improvement plan and D8 give the design
- approved 2026-11-05 by the building session under the person's delegation of 2026-10-22, for a fresh-context review
- the person's answers on 2026-11-05, in the question form: Test's trial "Not yet: run Test now (Recommended)", so the Test part runs inside Test's current term and the extension is not needed for M63; the reinstall "Yes, reinstall both (Recommended)"; Microsoft's telemetry "Turn it off (Recommended)" (answered by: person)
- fresh review (a general-purpose subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-11-05: 2 High, 11 Medium, 4 Low (R1 to R17), about 180k tokens; every finding fixed in `plan.md` (Done when 3 and 4, Users, Data and its matrix, Automation, the route's steps 0 to 10 and Windows, "What the person does", Solution and publisher, the cost table, Assumptions, Steps) and in Q2, Q3, Q6, Q9, Q10 and Q11 here; resolutions in `review.md`
- the same reviewer's second pass 2026-11-05, about 215k tokens: 14 resolutions hold; R1 (the raised copy was planned before the import), R6 (`analyze` quotes no variable default) and R17 (private ids left in this file) rejected; 4 Low new (R18 to R21). Fixed: the copy is planned in step 9; M61 runs on an offline copy with a URL in a flow input and a changed default, and the request no longer seeds a URL (Q3); the private ids are gone from this file; step 10's reads, the Windows plan's file name and its not-reached outcome, and the DLP `show` reads are in `plan.md`
- the same reviewer's third pass 2026-11-05, about 223k tokens: the seven resolutions hold; 1 Low new (R22, the exact URL form PO006 quotes), accepted and handled in the run; `plan-hash` set to the revised plan. In all: 2 High, 11 Medium and 9 Low, none open but R22, accepted
- signed off 2026-11-05 by the building session under the person's delegation of 2026-10-22, with no High or Medium finding open (`powerops plan sign-off`; `signed-off-hash` equals `review.md`'s `plan-hash`)
- 2026-11-05, during the run: the person moved M63 to another tenant of theirs, because the account they chose for the Dataverse CLI sign-in is not in the tenant that holds the earlier Dev and Test (answered by: person). Dev is an existing Developer environment there; Test is a new trial created for this run with `pac admin create`, on the person's yes, so step 1's types hold (Developer, Trial) and Test's term is the new trial's. D8's extension question no longer applies to M63. No write was made in the earlier tenant
- 2026-11-05, during the run: the `pops` publisher does not exist in the new Dev; the person said yes to creating one for the scratch solution, instead of reusing the seed's ("Solution and publisher") (answered by: person)
- 2026-11-05, during the run: the person chose to set up Dataverse-skills (its Python SDK in a venv in the scratch repo, its CLI installed globally, the person's own sign-in) rather than route tables through model-apps (answered by: person)
- 2026-11-05, during the run: Test had no Dataverse connection; the person chose "Agent makes it with FlowAgent" over making it themselves, so this session created one for the person's account in Test with FlowAgent, journaled in the scratch plan's `writes.md` (answered by: person). Route step 6 said the person names or makes it
- 2026-11-05, during the run: the business rule was built by this session in the maker portal, in the Playwright browser the person signed into, after the Maker SDK push failed; the person asked for both. The person's request to remove System Administrator and System Customizer from the app's role list could not be done: the maker portal locks both (answered by: person)
- 2026-11-05, docs fix from the run: `docs/HARNESS.md` gets a row for the desktop app's Code tab, whose `deploy_apply` form came back declined 3 to 4 seconds after the call, on two plans, with no form shown; the interactive Claude Code row records that the form worked in a terminal on Linux in this run. The person imported with `powerops deploy apply --apply` in their own terminal after the first decline, then answered the form for a skip-only plan in the terminal client (decisions Q2's fallback)
- 2026-11-05, during the run: `windows-runbook.md` filled in with the commit the Linux run installed, the solution's names and the Linux run's result lines; its form step says to use `claude` in a terminal
- 2026-11-05, docs fix from the run: the Playwright starter's README says to pick a text column for `formField` (a date, lookup or choice column is a combobox on the form, so the check doesn't find it) and that its model-driven test ran live once in this run; the canvas test and the seed apps' selectors stay unconfirmed. The Windows runbook uses the Room table's names, which passed on Linux
