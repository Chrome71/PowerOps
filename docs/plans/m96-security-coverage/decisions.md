# Decisions: m96-security-coverage

Round 0 holds the owner's answers that start this milestone, quoted from the chain rules (`reports/trial-3/chain/chain-rules.md`, git-ignored), the fix plan after trial 3 (`reports/trial-3/fix-plan.md`, section 5) and the review-enforcement audit's decision packet (`reports/review-enforcement-audit-2026-11-13/decision-packet.md`, section 8). Round 1 was settled by the building session inside those answers, under the owner's direction for this work to decide routine choices and log them ("as autonomous as possible", 2026-11-11).

## Round 0 (the owner)

- Q4 of the fix plan, "Rerun the security role's eval before trial 4 (M96)?": "Rerun it" (answered by: person, 2026-11-11).
- "What cost cap should the security role's eval rerun have?": "USD 30" (answered by: person, 2026-11-11). Raised the same day to M92's question: "Add to M96, cap USD 40 (Recommended)", so M96 reruns the Verifier pass too, regenerates the role baselines and drops M92's `--show-values` bridge (answered by: person, 2026-11-11; recorded in `m92-done-handover.md`).
- For the fix plan's milestones, after its plan review: "Start the build" (answered by: person, 2026-11-11). Standing instructions under which sessions sign off their own plans after clean reviews ("Sessions sign off", 2026-11-09) and merge their own green pull requests ("Sessions merge", 2026-11-09).
- "Take the recommended option on all eight decisions?" of the audit's packet: "All recommended" (answered by: person, 2026-11-13). Decision 2(a): a `settings-hash:` line in both kept roles, inside M96's rerun and its USD 40 cap, stop and ask if the estimate passes it. Decision 5(a): `analyze` says when a canvas app's `.msapp` is missing; amends M44 Q3. The packet's orchestrator moved 5(a) into M96 after the answers (packet section 8).
- "May the milestone sessions for this work (M112, M114, M115, M116 and the two additions to M96) sign off their own plans after clean reviews and merge their own green pull requests, as in the M86 to M111 chains?": "Yes, same as the chains" (answered by: person, 2026-11-13).
- "Start M115 and M96 now, since usage has dropped to 26%?": "Start both" (answered by: person, 2026-11-14). This replaces "M96 after reset" (2026-11-13).
- The keep decision after the rerun is the owner's: this session stops and asks before the pins and the merge.

## Round 1 (the plan)

### Q1. How does a role get the settings file's hash?
1. `settings validate`, which both roles already run on that file, prints a last line `settings-hash: <hex>` and a JSON field `settingsSha256` (recommended)
2. `powerops manifest <settings file>`, widening `manifest` to a JSON file
3. The role hashes the file with an OS tool such as `sha256sum`

Answer: 1 (answered by: agent m96-security-coverage). The hash is of the file the role validated, so it can't be of another one. 2 widens `Manifest.Read`, which the deploy check also uses to digest an `input:` path. 3 is outside the roles' command list and differs on Windows.

### Q2. Which settings file does a role hash, and how is it told?
1. The one the request names; the request names the settings file the deployment imports, `<zip name>.settings.json` beside the zip (`powerops-implement` section 4 says so) (recommended)
2. The role finds the deploy folder and picks the file itself

Answer: 1 (answered by: agent m96-security-coverage). The roles use only the inputs the request gives; `deploy plan` already pairs a zip with `<zip name>.settings.json` (`DeployCommand.cs:1050`).

### Q3. What does a deployment with no settings file need, and what does an old report cover?
1. A role told of no settings file writes `settings-hash: none`. A report with `none` or with no `settings-hash:` line (written before M96) covers only a deployment with no settings file; `unknown` covers no deployment; a hash covers only a deployment whose settings file has that SHA-256 (recommended)
2. An old report covers any deployment, as before

Answer: 1 (answered by: agent m96-security-coverage), as the packet's "Inside M96" says ("old reports cover only deployments with no settings file"). It fails closed: a report that can't show which settings it saw doesn't cover settings it may not have seen.

### Q4. Where in the fault chain does the settings check go?
1. Last in `Ready`, after M114's open-High check, so M114's sentences and their order stay (recommended)
2. Next to the hash links, before the state

Answer: 1 (answered by: agent m96-security-coverage). The brief keeps M114's order.

### Q5. Where does the header line go, and how many lines are read?
1. Eighth line, after `deploy-hash:`, read from the first eight lines; the other fields are read as today (recommended)
2. Before `deploy-hash:`

Answer: 1 (answered by: agent m96-security-coverage). Adding at the end keeps every existing line on its line number, so `plan status`, its schema and M83's seven-line readers stay true.

### Q6. Which canvas apps get the missing-package note, and what does it say?
1. An app whose metadata `DocumentUri` names a `CanvasApps/<file>.msapp` the solution doesn't hold, in the layouts that can open packages; one `notChecked` entry through PO018's `NotChecked`, naming the app and the path, worded like the no-`Src` entry (recommended)
2. Every canvas app in a folder layout

Answer: 1 (answered by: agent m96-security-coverage), as audit decision 5(a) and the first answer of M44 Q3. This amends M44 Q3's answer 2: the rerun M44 avoided is this one.

### Q7. Does any output need a `schemaVersion` bump?
1. No: `analyze` adds `notChecked` strings (content, not shape); `settings validate` JSON gains one optional property; `plan status` doesn't change (recommended)
2. Bump `settings validate`

Answer: 1 (answered by: agent m96-security-coverage). Decision 48 bumps for a new value of a closed enum; an added optional property breaks no consumer that validates against the published schema, which the schema change keeps valid.

### Q8. The new eval case: what does it hold?
1. `solution-09`: the seed with the canvas package present, a fake host in `Properties.json` and `References/DataSources.json`, a plan that changes only the canvas app, one Security reviewer flaw (F47) and none for the Verifier (recommended)
2. Add the package to an existing case

Answer: 1 (answered by: agent m96-security-coverage). An existing case's keyed flaws and baselines stay comparable with M83; a case clean for the Verifier also shows whether it raises a false High on a package it can't open.

### Q9. Who scores the reports?
1. An Opus subagent and Codex read-only, as M83 with Opus for Sonnet (recommended)
2. Sonnet and Codex, as M83

Answer: 1 (answered by: agent m96-security-coverage). The person's model routing (`~/.claude/CLAUDE.md`) allows Fable, Opus and Haiku only.

## Log

- 2026-11-14: plan started by the M96 session (worktree `m96-role-rerun-9271cf`, branch `claude/m96-role-rerun-9271cf`) on `a442b17`, with M115 running in another session.
- 2026-11-14: powerops-researcher (Opus), Learn and microsoft/PowerApps-Tooling only: a `.msapp` is a zip (confirmed); it holds `Properties.json`, `References/DataSources.json` and `Src/*.pa.yaml` (confirmed); a Dataverse source records the environment's address (partly: the legacy unpacker models `instanceUrl` and `InstanceUrl`, no open sample shows a value); Microsoft says only `Src/` is for source control and doesn't name environment values. So P1's last sentence says "can hold" (plan, Assumptions).
- 2026-11-14: plan review round 1. Fable REJECTED (2 High: the case's package stored uncompressed, the scoring copy undefined; 3 Medium: the absent-package finding on the old cases, the cover-condition texts under `src/` and in the Action docs, P1 widened beyond SE:07; 3 Low). Fresh `powerops-review-plan` 0 High, 5 Medium, 8 Low (`review.md`). All answered in revision 2 of `plan.md`.
- 2026-11-14: plan review round 2. Fable APPROVED revision 2 (`6abbb6b`) with step assignments: steps 2, 3 and 6 Opus; step 4 run by the session with a Haiku watcher for cost; step 5 an Opus scorer and Codex (`gpt-6-astra`) read-only; step 7 the session with a Haiku CI watcher. Second fresh `powerops-review-plan` pass: R1 to R13 resolved, R14 (Medium) and R15 to R19 (Low) new, answered in revision 3 (`d771bff`). Third pass: R14 to R19 resolved, R20 (Low, quote the placeholder path) accepted for the step 6 revision. No finding open.
- 2026-11-14: signed off with `powerops plan sign-off` by the building session, on the owner's standing instruction for the chain ("Start the build", 2026-11-11, for M96 itself) and, for the two additions and the early start, "All recommended" (2026-11-13), "Yes, same as the chains" (2026-11-13) and "Start both" (2026-11-14), all quoted in `reports/trial-3/chain/chain-rules.md` (answered by: agent m96-security-coverage, on the person's instructions above)
- 2026-11-14: first freeze `b3698eb` failed the Fable work review (an unplanted Verifier flaw in solution-09: a label "Open work items" over a count of all rows); corrected in the freeze `b7f9f09`, which passed the work review and the stub check before any paid run. No run used `b3698eb`.
- 2026-11-14: the eval ran 19:25 to 20:00 UTC on `b7f9f09`, 36 runs, all valid, none replaced; Claude $10.60. Both roles hold both keep rules as settled (Verifier 12 of 12 and Security reviewer 16 of 16 own not-shown flaws; 0 false Highs each, from seven clean-case Highs split between the scorers and settled supported by M83's precedent; under the Opus scorer's reading 5 and 2). F47 found on both families. `report.md` in this folder. The keep decision is the owner's: asked, not yet answered.
- 2026-11-14: the owner's keep decision, asked with `report.md` and the recommendation to keep both: "Keep both" (answered by: person, 2026-11-14). Both roles are kept at the text of `b7f9f09`, pinned in the Data section of `plan.md`; rows 11 to 13 follow, and the plan is reviewed and signed off again.
