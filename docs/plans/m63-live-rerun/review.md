plan-hash: 0da2806e629672ed4756a5ea9ccff5cf9d2a3078851f06b0a21303145099e933
model: claude-sonnet-5-5
fresh-context: yes

This is the third pass: the seven resolutions of R1, R6, R17 and R18 to R21 hold against the revised files and the code, one new Low finding was added (R22), and no High or Medium is open, so the plan-hash is the current one. The earlier second pass: of the 17 resolutions 14 hold, 3 do not (R1, R6 and R17 stay open), and 4 new Low findings were added (R18 to R21). Environment facts were not read (`plan facts` is not run: no tenant contact). Reviewed from the repo at the commit the installed binary was built from. `review: done` was not written into `plan.md`, because this review was asked to change no other file; the owner sets it.

Checklist items applied: 5 (environments and route), 3 and 4 (licence and DLP assumptions, as they bear on the run), 8 (security roles the run needs), 9 (testability: each check's "pass when"), 10 (open questions), and the URL/GUID/email/name scan (clean: `plan status` reports no violation for the folder as it stands). Skipped, because this plan builds no solution of its own: 1 (solution boundaries), 2 (the scratch product's connection references and variables are the build child's), 6 (managed for Test, unmanaged for Dev is the route itself), 7 (delegation and data volume: the product is one table), 11 (size: nothing is built by this milestone), and the per-product items.

Checked against the repo: `docs/HANDOFF.md`, `docs/HARNESS.md`, `docs/RECOVERY.md`, `docs/COMMANDS.md`, `docs/USE-WITH-AGENTS.md`, `docs/examples/playwright/README.md`, the M52 plan and `live.md`, `DeployPlan.cs`, `SolutionDiff.cs` and `McpCommand.cs`. Checked against the milestone's brief: the M63, P0, M61, M62 and decisions sections of the private improvement plan (D1, D3, D8). No tenant detail from the private notes is repeated here.

## Findings

### High

#### R1. The M53 re-plan check cannot pass as written
- Area: 9 (testability: "pass when" must test the fix)
- Finding: the matrix says `deploy plan` again after the import must show the `components-not-checked` item. In `DeployPlan.cs` that item is added only inside `AddUpgrade`, which runs only when the zip's action is `upgrade` and the zip is managed (`COMMANDS.md`, "For the upgrade of a managed zip"). After the import, Test holds the same version, so the re-plan of the same zip is a `skip`. A `skip` gets only `same-version-differs` (and only when flows, variables or references differ). The run would therefore report M53 as a fail, or not reached, when the code is right. The first plan (action `install`) does not show the item either. Done-when 2 and Q9's "no second import" both lean on this check.
- Suggested fix: keep Q9 (no second import) but plan, without `--save` and without applying, a scratch copy of the managed zip whose `Version` is raised by one (copy the zip and its settings file into a separate folder; the copy is never imported and never goes to the form), against Test. Pass when that plan's action is `upgrade` and it carries the `components-not-checked` item naming the table. Or mark the item "covered by unit tests, not reached live" in the matrix. Also say that the plain re-plan is expected to show `skip` and, at most, `same-version-differs`.
- Resolution: Fixed after the second pass: the copy is planned in step 9, after the import, where Test holds the solution, so its action is `upgrade` (matrix M53 row, route steps 7 and 9, decisions Q9). Earlier: Second pass: the copy and its expected result hold (a version raised by one makes the action `upgrade`, and `AddUpgrade` then adds the item for a managed zip), but the plan runs that `deploy plan` in route step 7, before the import. At that point Test does not hold the solution, so the action is `install` and no `components-not-checked` item appears; Q9 itself says the copy needs a version higher than Test holds. Move the M53 plan of `scratch/upgrade/` to step 9, after the import (the M62 warning check on that copy can stay in step 7).

#### R2. The M62 stale-solution test edits a different input than the reports read
- Area: 9 (testability: "pass when" must test the fix)
- Finding: step 5 has the two review roles read the managed zip, so each report's `input:` names the zip and its `solution-hash:` is the zip's SHA-256. `plan status` compares the digest of the solution that `input:` names (`COMMANDS.md`, plan and skills). The M62 edit test changes "one committed file in the unpacked folder". That leaves the zip untouched, so `stale (solution)` stays `false` and the "true after a reverted test edit" half of the pass condition cannot occur. The matrix would show M62 as failing, or the session would improvise an edit under time pressure. The second half ("`deploy plan` warns only for a zip no ready report names") also needs the reports to be `state: ready`, which is not stated.
- Suggested fix: make the edit test touch the thing a report names. For a zip report: replace the zip in the plan's folder with a one-byte-different copy for the `plan status` read, then restore it (and re-check its SHA-256). Or have one role run on the unpacked folder as well and edit that. State that the reports must be `ready` for the `deploy plan` warning check, and what the warning is expected to say on the zip nobody reviewed.
- Resolution: Fixed. The M62 row swaps the deploy folder's zip for a copy that differs only in its zip comment, reads `plan status`, then restores the original and checks its SHA-256; the reports must be `ready`; the warning is expected for the raised copy in `scratch/upgrade/` and not for the reviewed zip. Second pass: checked, holds, with one note: the zip swap changes the zip's SHA-256 only (a comment is ignored by the reader), so `stale (solution)` flips as the row says.

### Medium

#### R3. `plannedBy` will be null on the plan the run saves
- Area: 9 (testability)
- Finding: the M52 pass condition says "the plan file has `plannedBy`". Step 7 runs the CLI `deploy plan ... --save`, and `plannedBy` is only set by `--planned-by` (the MCP `deploy_plan` and the Action set it themselves). The plan file would hold `plannedBy: null`, so the condition fails or is read loosely.
- Suggested fix: run step 7's plan with `--planned-by agent` (the agent made it), or make it with the MCP `deploy_plan` and `save`. State the expected value in the matrix. Also record that the field is a claim, not proof.
- Resolution: Fixed. Step 7 saves with `--planned-by agent`; the M52 row expects `plannedBy: agent` and says it is a claim, not proof. Second pass: checked, holds; `--planned-by agent` is a documented option and the field is not a hash input.

#### R4. The Windows runbook's declined form and its timing are not workable as described
- Area: 5 (route), 9 (testability)
- Finding: (a) The runbook copies the deploy folder, which holds the Linux plan file. After the import the target holds the solution, so the plan's hash no longer matches: `deploy_apply` refuses at the dry run and sends no form (`McpCommand.cs`: "the dry run did not pass, so no form was sent"). The decline check would never reach the form. A Windows-made plan after the import is a `skip`-only plan, which does reach the form, and a decline then imports nothing in any case. The plan does not say which. (b) The runbook is written after step 10, then run by the person on another machine. Test's term is fixed (the plan's own rule: every Test check finishes inside it), and the term chosen "now" is about two weeks. Nothing schedules the runbook inside it, or says what happens if the person cannot run it in time. (c) The runbook needs `pac` and `az` signed in on Windows, an interactive Claude Code session with the reinstalled plugin there for the form, and a Playwright sign-in and model-driven link: all person steps, none listed.
- Suggested fix: say that the runbook's step makes a new plan on Windows (after the import, so it is `skip`-only) and that the form is declined on that plan, with the expected refusal or decline text. Draft the runbook before step 7 (with placeholders) so the person can run it right after step 9, while Test is surely live, and state the deadline: the Test reads of the runbook finish inside the chosen term, or the Windows result is recorded as not reached. List the Windows sign-ins and the Windows client as person steps.
- Resolution: Fixed. The runbook makes a new plan on Windows after the import (`skip` only) and the person declines the form on it; it is drafted before step 7 and run right after step 9, inside Test's term, or recorded as not reached (Done when 4); the Windows sign-ins, reinstall and client are in "What the person does"; decisions Q11 updated. Second pass: checked, holds, except two small points (R19).

#### R5. The first run's real agent exports are not where the plan says
- Area: 9 (testability), 5 (route)
- Finding: M50 and M51 are to be checked "on the first run's agent exports, read from the session notes folder outside the repo". The private notes folder holds the review and plan documents only: no zip or agent export. The first run's scratch repo holds only the equipment solution's zips. The only copies of those agent exports in reach are the scrubbed fixtures already committed under `tests/fixtures/agents/new-ui/`, which M51's own tests already run. Checking them again live adds no real input, and the plan's argument for Q6 (the bytes that showed the first failure) does not hold for them.
- Suggested fix: at preflight, name where the real exports are or confirm they are gone. If gone, say in the matrix that M50 and M51 are re-run on the scrubbed fixtures (a repeat of the unit tests on the installed build, which still checks the installed binary and the plugin) and that real-input confirmation is not reached, or ask the person whether to build a new agent (Q6 option 2). Do not claim "real input".
- Resolution: Fixed. The two real agent exports are in this session group's notes folder (`agent-exports-2026-11-04/`), not the plan's; Data and decisions Q6 name the place, and the removed-tool case is an offline copy of the first, as the first run made it. Second pass: checked, holds: the exports are in the named folder (a v1 and a v2 zip, with a README that marks them unscrubbed), and an offline copy of v1 without the tool's folder reproduces the removed-tool case.

#### R6. The M61 redaction check can pass with nothing to redact
- Area: 9 (testability)
- Finding: the pass condition is "no URL in the redacted output". The scratch product (a table, a variable, one flow, a model-driven app) may hold no URL at all, so the unredacted output also has none and the check proves nothing. M61 redacts URLs and `defaultValue` text.
- Suggested fix: require that the planning child's brief or a seeded edit gives the export a URL (for example a variable default value) before `analyze` and `diff`. Pass when the unredacted output shows it and the redacted output shows the placeholder, and `diff` of two versions prints `defaultValue changed` without the value. Record both outputs scrubbed.
- Resolution: Fixed after the second pass: M61 runs on `scratch/redact/`, an offline copy with a fake SharePoint URL in a flow action input (PO006 quotes it) and a variable default changed to a fake URL (`diff` prints it); the product's request seeds no URL (decisions Q3). Earlier: Second pass: the fake URL as a variable default shows in `diff` (it prints a variable's `defaultValue`), so that half holds. It does not show in `analyze`: no rule quotes a variable default (PO007 fires without quoting it), and PO006 and PO014 quote only a URL written in a flow or a canvas formula. The plan's request makes the address a setting, so `analyze` plain and redacted output would be the same and the check proves nothing for `analyze`. Either seed a literal URL in an offline copy of the flow (PO006 pattern, a copy of the export, never imported) and compare that, or say the `analyze` half is not reached.

#### R7. The Action fallback for the yes is not worked out
- Area: 5 (route), security
- Finding: step 8's last resort is the Action route in the test repository. The scratch repo is "never pushed", and the Action needs the plan file, zips and settings in that repository, so something would have to be pushed, including a settings file that holds a connection id (git-ignored here for that reason). It also inherits the shared federated-credential finding from M52's live check (named in Won't build) and the one-account self-review problem that M52 hit: with one GitHub account the person had to switch protection off to approve. None of this is in the plan, and `AGENTS.md` says never to push unless asked.
- Suggested fix: state the fallback's route as a person decision at the time: what is pushed (and that the settings file is supplied as the example does, not committed), which protection settings the person accepts, and that the credential gap is known. Or drop the fallback and record the run as "form not reached" if no client shows it.
- Resolution: Fixed. The Action route is no longer a planned fallback: if neither client shows the form, the session stops and the person decides, with that route's costs named (decisions Q2). Second pass: checked, holds.

#### R8. The person's steps are scattered and some are missing
- Area: 5, 3, 4, 8
- Finding: the brief asks the plan to state every step that needs the person. They appear across Users, the route and the Windows paragraph, with no single list. Missing or only implied: the Test trial extension itself (who clicks it, and the answer if it is not offered: Q is in the plan, the action is not); creating a Dataverse connection in Test, if none exists; assigning the custom role and the app to the Playwright account in Test, if that account is not the person's own admin account, which is a tenant write (the starter needs Read on the tables and Create on the main one) and is not in the "objects a step creates" list; the Power Apps licence of that account for model-driven play (the assumption covers only "can open model-driven apps"); and Test's own DLP policy for the Dataverse connector (the assumption names Dev's only, and step 9 needs the flow on in Test).
- Suggested fix: add one table, step by step, of what the person does, with the tenant writes marked, and add Test's DLP and the account's role and licence to Assumptions or to preflight reads (`pac admin list` and a role read), stopping with a question if not met.
- Resolution: Fixed. A "What the person does" table lists every person step with tenant writes and machine changes marked; step 1 reads DLP policies and the person's roles; Assumptions cite the first run for DLP in both environments and model-driven play. Second pass: checked, holds, with the DLP point in R21.

#### R9. The read-only inventory has no scope or method
- Area: 9 (testability)
- Finding: done-when 3 needs "every object the inventory finds since the run started" reconciled with `writes.md`. Step 10 names "Web API reads with an `az` token, `pac solution list`", but not which tables, which time filter, or how to see what the Web API does not hold: connections (they live in the Power Platform APIs, not Dataverse), flow runs, and objects in the default solution. The first run's own leftover list had items of exactly those kinds (connections and references Studio made in the default solution). A thin inventory will report "all found" without having looked.
- Suggested fix: list the reads (solutions, solution components, workflows, connection references, environment variable definitions and values, app modules, roles, the model's table and its rows, and connections via the documented read), use `createdon` and `modifiedon` since the run's start, and say what the inventory cannot see, so the record states it. Run it in Dev after the build and in Test after step 9, and note that a row's change by the session's own reads is none.
- Resolution: Fixed. Step 10 lists the reads, the time filter, `pac connection list` for connections, `flowruns`, the auditing fields, when it runs in each environment, and that what it can't see is named in the record. Second pass: checked, holds, with the entity-name points in R18.

#### R10. Cost lines have no basis except the Sonnet children
- Area: 5 (route), cost
- Finding: only the Sonnet children line has a basis (the first run's figure). The subagent line ("USD 2-6 equivalent", "4 review passes"), the Haiku line, and the Opus line ("8-15 points of the weekly meter") have none. The meter reading "at the start and the end" is not tied to a way to read it. The cost of the form session (a person's interactive client), of the Windows session, and of each child's `total_cost_usd` (needs `--output-format json`) is not covered or collected.
- Suggested fix: give each line a basis (a number of passes times a measured pass cost, or a stated guess marked as one), say how the meter and each child's cost are read, and add the two interactive sessions as "not measured / on the person's plan".
- Resolution: Fixed. The cost table gives each line a basis (measured, or a guess marked as one), how it is measured, and the person's interactive sessions as not measured. Second pass: checked, holds.

#### R11. The build children can write to whichever environment `pac` has selected
- Area: 5, 3 (security of the route)
- Finding: the gate hooks go on at step 7, so in step 3 nothing stops a Microsoft skill's import or write from reaching Test. Those skills use the `pac` profile's selected environment, and pac's MCP tools can switch it mid-session (see HANDOFF, "Sign-in state"). The plan's control is the session asking the person before each step. A headless `claude -p` child cannot ask mid-step, so the only check is what the child does inside a step.
- Suggested fix: before each build step, pin and read `pac org who` (Dev), and run the step with the pac profile selection set to Dev only. Say in the plan that a `pac` selection change during a build step is a stop. Record the check per step.
- Resolution: Fixed. Step 1 sets the person's `pac` selection to Dev and adds the deny rules for pac's MCP write tools; step 3 reads `pac org who` before and after each build step and stops on a change. Second pass: checked, holds.

#### R12. The Playwright starter is not usable on this product without changes the plan does not name
- Area: 9 (testability)
- Finding: the starter's two tests are written for the seed solution (a canvas app test that fails without `CANVAS_APP_URL`, and a model-driven test on the seed's table, column and field). The scratch product has no canvas app, and its table and field names differ. The README says to change the names at the top of the spec. The plan says only "set up as its README says". The first run needed the same change. The README also states the starter's selectors were never confirmed live.
- Suggested fix: say that the copy's names are changed to the product's, the canvas test is skipped or removed in the copy (the record says so), and that a failure from the starter's unconfirmed selectors is recorded as a starter finding, not a PowerOps one. Also note the person pastes the model-driven link into the sign-in window (an agent cannot drive that window).
- Resolution: Fixed. Step 9 names the Playwright edits (names at the top of `apps.spec.ts`, the canvas test removed in the copy), the person's paste of the link, and that an unconfirmed selector failing is a starter finding. Second pass: checked, holds.

#### R13. A committed `windows-runbook.md` will trip `plan status` if it holds a URL
- Area: URL/GUID/email scan
- Finding: `plan status` fails any file in the plan folder that holds a URL, GUID or email, and CI runs it. A runbook with an install step normally holds a clone URL or a doc link, and `<tenant URL>`-style lines are fine only as placeholders. The plan says "with the commit and placeholders" but not that URLs are avoided or allowed line by line.
- Suggested fix: use placeholders for every URL, or add `<!-- plan-status: allow -->` on lines that quote the public repository, and run `plan status` on the folder before committing the runbook.
- Resolution: Fixed. The Windows paragraph says the runbook uses no URL, and Steps 3 runs `plan status` with the runbook in the folder. Second pass: checked, holds; the runbook has no URL and the plan's own scan is clean.

### Low

#### R14. The plan sign-off is run by the session
- Area: 10 (the person's yes)
- Finding: step 2 lets the session run `powerops plan sign-off` on the person's chat yes and log the quote. That matches D3 only if the log says `answered by: person` with the quote; the command itself is still run by an agent, which is what M54's warning looks for in a marker, not in the command.
- Suggested fix: have the person run the one command, or record the quote with its time and that the session ran it on that yes.
- Resolution: Fixed. Step 2 and decisions Q8: the session logs the person's quote, its time, and that the session ran the command on that yes. Second pass: checked, holds.

#### R15. The gate check's hook input shape is not stated
- Area: 9 (testability)
- Finding: step 7 says "fed the hook input of the planned `pac solution import`" but not the JSON shape, and the plan relies on literal absolute paths and Test's address, which the session must build from private values.
- Suggested fix: name the shape (Claude Code's Bash input as recorded in M21, with `tool_name` and `tool_input.command`), say that the command text is built in a scratch file outside the repo, and that only its shape, the exit code and the plan hash go into the record.
- Resolution: Fixed. Step 7 names the input shape, the scratch file outside the repo, and what the record keeps. Second pass: checked, holds; the shape named (`hook_event_name`, `tool_name`, `tool_input.command`, `cwd`) matches what M21 recorded.

#### R16. Child sessions run headless, and the plan does not say how questions are answered
- Area: 9
- Finding: `claude -p` ends when a skill asks a question. The plan says the session answers product choices and relays others, but not that the child is resumed turn by turn (the first run did this) and that each turn's cost is summed.
- Suggested fix: say it, and that `--resume` carries the child's context.
- Resolution: Fixed. Users says children run with `--output-format json`, end a turn on a question, and are resumed with `--resume`, their costs summed. Second pass: checked, holds.

#### R17. Smaller points
- Area: 10, 5
- Finding: (a) "the `pops` publisher (decisions Q3)": Q3 does not mention a publisher, and the prefix may already exist in Dev from earlier work; the plan should say reuse or create. (b) The plan and decisions cite private finding ids (A-3, A-11, F1, C-9) and "row 19" that a reader of the repo cannot resolve; a short clause each would do. (c) The step 5 leak scan names `control: found (ok)` but not where the script is; it is not in this worktree's `reports/`. (d) The Linux Playwright `chmod` pass condition covers the folder and the file but not that the check is made after each save, as the README says. (e) The record's second copy goes to the session notes folder; say that only the scrubbed record goes there.
- Suggested fix: fix each in one line.
- Resolution: Fixed after the second pass: the private ids are removed from `decisions.md` (Q1, Q6, Q7, Q8), each replaced by what it stood for. Earlier: Second pass: (a), (c), (d), (e) hold (the leak scan script is in the main checkout's `reports/`, as the plan says). (b) does not: `plan.md` is clean, but `decisions.md` still cites private ids (A-3, A-11, C-9, F6, row 19) without explaining them, although the resolution says it keeps them only where it does. Add a clause for each or drop the id.

#### R18. Step 10 names entity sets and fields that do not exist or cannot be filtered
- Area: 9 (testability)
- Finding: "role assignments" and "the auditing fields of `organizations`" are not Web API entity sets or a way to list changes, and the table's metadata (`EntityDefinitions`) has no `createdon` to filter on. `flowruns` is right as an entity set, but it keeps run history only for a limited time and needs read on that table.
- Suggested fix: name `systemuserroles` for assignments, drop the `organizations` read (or say what it is for), use the metadata change query for the table, and note the retention and privilege of `flowruns`.
- Resolution: Fixed: step 10 reads the users of each new role through its `systemuserroles_association`, reads the custom tables' metadata and `organizations`' `isauditenabled` whole and filters them in the script, and says `flowruns` is read on the day.

#### R19. Windows plan file name and the skip-only form are not yet shown to work
- Area: 9 (testability)
- Finding: the copied deploy folder already holds the Linux plan file, and the MCP `deploy_plan` `save` refuses to overwrite an existing file, so the Windows plan needs a new file name; the plan does not say so. That a skip-only plan reaches the form was read in `McpCommand.cs` (the dry run must exit 0 with status `planned` and an equal hash) but was not shown by a test, so the first Windows attempt is also the first proof.
- Suggested fix: name the new plan file in the runbook and say that, if the form is not shown for a skip-only plan, the record says "not reached" rather than a pass.
- Resolution: Fixed: the Windows plan is saved under a new file name, and a form that doesn't appear is recorded as not reached, not passed (route, Windows).

#### R20. The seeded URL default raises PO007 in the product's own checks
- Area: 9 (testability)
- Finding: a variable whose default is a SharePoint address triggers PO007 (a warning), and the review roles read the same solution. The M55 and M62 rows need the reports to be `ready`; a role may mark the build blocked for a warning it counts as a gap, which would fail those rows for a reason that belongs to the seed.
- Suggested fix: say that PO007 on the seeded default is expected and recorded, and that a `ready` state is judged with that finding in the report; or seed the URL only in the offline copy used for M61.
- Resolution: Fixed with R6: no URL default goes into the product.

#### R21. `pac admin dlp-policy list` alone does not say which policy covers an environment
- Area: 4 (DLP fit)
- Finding: step 1 says the command "shows the policies that apply to each" environment. A list names the tenant's policies; which connectors each puts in which group, and whether it covers Dev or Test, needs `show` for each policy.
- Suggested fix: add `dlp-policy show` for each listed policy, and the stop rule if the Dataverse connector is blocked or grouped apart from the connectors the flow uses.
- Resolution: Fixed: step 1 runs `pac admin dlp-policy show` for each listed policy.

#### R22. The seeded flow URL must match PO006's pattern
- Area: 9 (testability)
- Finding: the M61 row says the flow action input holds "a fake SharePoint site URL". PO006 matches only a Dataverse host (`<name>.crm<n>.dynamics.com`) or a SharePoint address with `/sites/<name>` or `/teams/<name>` after the host (`RuleHelpers.EnvironmentUrl`), and it ignores literals under the flow's `parameters`. A bare tenant address, or one placed in a parameter default, would give no finding and the `analyze` half of the check would be quiet again.
- Suggested fix: say the seeded value has the `/sites/<name>` form, sits in an action input outside `parameters`, and that the check first confirms PO006 appears in the plain output.
- Resolution: Accepted, handled in the run without a plan change: `scratch/redact/` uses a fake SharePoint address on the `contoso` tenant name with a `/sites/<name>` path in an action input outside `parameters`, and the record first shows PO006 in the plain output; if it doesn't appear, the `analyze` half is recorded as not reached.
