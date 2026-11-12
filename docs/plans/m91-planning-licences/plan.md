---
status: approved
review: pending
signed-off: null
signed-off-hash: null
environment: null
depth: standard
supersedes: null
---

# M91: Planning asks fewer, shorter questions, and settles licences in round 1

## Goal

Owner findings B and C of trial 3 (`reports/trial-3/fix-plan.md`, section M91). In the trial, planning took 36 questions in 14 rounds with 129 options, some a paragraph long; rounds 12 to 14 each confirmed one assumption a review had found; and the one licensing question's recommended option stated licences the owner's account did not hold, which nothing checked until the step that needed Power BI. The owner asked for "more user-friendly options given among the choices to avoid too much information overload" and for "a license check at the start of the project before doing work".

Done when:

1. `skills/powerops-plan/SKILL.md` section 6 says an option is a label of at most six words and one sentence of at most 20 words on what changes, with sources and detail in the plan, and that a recommended option never states a fact about the person's tenant, licences or data (Data row 3).
2. Items with a safe default are written under Assumptions as `default: <choice>` and confirmed in one question, "Keep these N defaults?" ("Keep all", "Change some"), at approval; under `quick` and in the fast path the approval keeps them. Review findings that only need an assumption confirmed are asked the same way, in one question, before the sign-off menu (Data rows 3, 4, 6).
3. Under `standard` the skill aims for five rounds or fewer before approval; after round five each question names the plan line it unblocks; the progress note "Round <k> of about <n>" goes in the chat message only, and `decisions.md` keeps its `## Round <n>` headings (Data row 3).
4. Round 1 asks the cost tier, "Which licences should the plan assume?", with three options: "Not sure: standard first" (recommended), "Standard licences only", "Premium where needed". `Constraints` carries a `### Licences` table `| Need | Product or feature | Who needs it | Standard alternative | Held |`; the tier's rules decide which route the plan takes (Data rows 2, 3).
5. Under the condition `plan facts` has (the person named the environment) plus the person saying `az` is signed in, the skill reads the signed-in account's licence part numbers once, read-only, through `az rest` on Microsoft Graph's `me/licenseDetails`, maps them through `licensing.md`, writes product names only, and reads a part number the mapping doesn't know as `unknown`, never held. Without the condition, or when the read fails, it asks neutrally and writes `person says`. Test users' licences are always the person's answer. The fast path reads nothing and writes `unknown` with a check step (Data rows 1, 5).
6. A new reference `skills/powerops-plan/references/licensing.md` is the one home for which feature needs which licence, its part numbers and its standard alternative, each line sourced from Learn with its checked date (Data row 7); `data.md` gate 7 points to it (row 8).
7. `skills/powerops-review-plan/SKILL.md` item 3 makes a plan with no `### Licences` table, or a row whose `Held` is `unknown` with no step that checks it before first use, a Medium finding (row 9).
8. `docs/PLANNING-SKILLS.md` and `docs/ecosystem/compatibility.md` say the same (rows 10, 11); `SkillsCommandTests` pins the wording (row 12); the smoke script checks the licensing question and the `### Licences` table (row 13); the plugin copies are regenerated and their diff holds only these changes (row 14).
9. One smoke run, `tests/smoke/skills-smoke.sh claude`, on the changed skills, with its result in the Log (step 5).
10. `dotnet build` (0 warnings), `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m91-planning-licences` with no violation, and the leak scan are clean.

### Won't build

- A `plan status` warning for a plan with no `### Licences` table: every plan already under `docs/plans/` would warn (fix plan, "Rejected").
- A `powerops` command or MCP tool that reads licences: the skill calls `az` once; a command would be new code nobody asked for.
- A licence read in the fast path, or of test users: the fast path is offline, and only the person knows the test accounts.
- Parsing of `## Round` headings or option lengths in code: `PlanChecker` and `PlanDocument` parse neither, and nothing here needs them to.
- The build's licence re-check before its first step: that is M94's preflight.
- Edits to the kept roles (`powerops-verify`, `powerops-review-security`), `tests/eval/`, the version files or the changelog.

## Users

- A person planning a Power Platform solution with `powerops-plan`, who answers fewer and shorter questions and learns in round 1 which parts need a licence they may not hold.
- The fresh-context `powerops-review-plan` reviewer, which checks the licence table.
- M93 and M94, which build on these files (M94's preflight re-checks the table).

## Data

Line numbers are those of `b065f23`. Quoted text is the new wording; the executor may shorten it but keeps every pinned phrase of row 12.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | Section 3 step 1: "Ask, as a question of round 1, which environment the plan targets, whether `pac` is signed in to it, and whether `az` is signed in to its tenant." New step 8 "**Licences.**": only after the user has named the environment and said `az` is signed in, run once `az rest --method get --url <https, then graph.microsoft.com/v1.0/me/licenseDetails> --query "value[].skuPartNumber" -o tsv` (the `--query` keeps everything but the part numbers out of the context). An error, a refusal or an empty list is a failed read. Map each part number through `references/licensing.md`. `Held` is `yes (read)` when a mapped product covers the row; `no (read)` when every part number read is in the mapping and none covers it; otherwise `unknown`. A part number the mapping doesn't list never counts as held. Write product names only, never the part numbers or account details. If the condition doesn't hold or the read fails, say so in one line and ask in the next round, neutrally, which of the table's products the account holds; write `person says yes` or `person says no`. Test users' licences are always the person's answer. | `skills/powerops-plan/SKILL.md:36-46` | row 12 (a); `PlanFactsSkillTests` unchanged and passing |
| 2 | Section 5: `:87` "`Constraints` holds licensing, DLP and deadlines." becomes "`Constraints` holds DLP, deadlines and a `### Licences` table from `references/licensing.md`, one row per need: `\| Need \| Product or feature \| Who needs it \| Standard alternative \| Held \|`. A row whose `Held` is `unknown` gets a step that checks it before its first use." | `SKILL.md:87` | row 12 (b) |
| 3 | Section 6. Step 1 gains: "An option is a label of at most six words and one sentence of at most 20 words on what changes. Sources and detail go into the plan, not the option. A recommended option never states a fact about the person's tenant, licences or data: read it (section 3) or ask it neutrally." `:130` "Ask one licensing question in round 1 or 2." becomes the cost tier: "In round 1, ask the cost tier, "Which licences should the plan assume?": **Not sure: standard first** (recommended: the plan takes the standard alternative wherever the table has one), **Standard licences only** (a need with no standard alternative becomes a question), **Premium where needed** (premium rows stay; `Held` is still read or asked)." A paragraph "**Defaults.**": an item with a safe default is not asked; it goes under Assumptions as `default: <choice>`, confirmed at approval (section 7). A paragraph "**Budget.**": under `standard`, aim for five rounds or fewer before approval; after round five each question names the plan line it unblocks; start each round's message with "Round <k> of about <n>", a note for the chat only, while `decisions.md` keeps its `## Round <k>` headings. The `quick` line names the cost tier as its licensing question | `SKILL.md:117-145` | row 12 (c); smoke `check_decisions` and `:249` |
| 4 | Section 7, a step after step 2: "If Assumptions holds `default:` lines, ask one question, "Keep these N defaults?", listing them by label: **Keep all** (recommended) or **Change some**; then ask only about those changed. Under `quick`, the approval keeps them: say so beside B, and log `defaults kept: <N>`." | `SKILL.md:149-162` | row 12 (d) |
| 5 | Section 8 step 1 gains: "Write the `### Licences` table with every `Held` `unknown`, `default: Not sure: standard first` under Assumptions, and a first step that checks the held licences." Step 2's "each with its trade-off in a few words" becomes "each a short label with one sentence on what changes" | `SKILL.md:168-169` | row 12 (e); `The_fast_path_reads_no_environment_facts` unchanged and passing |
| 6 | Section 9, after step 2: "Findings that only need an assumption confirmed: ask them in one question, "Confirm these N assumptions?" (**Confirm all**, **Change some**), before the sign-off menu, and set each confirmed one to `Resolution: fixed`." | `SKILL.md:184-188` | row 12 (d) |
| 7 | New `skills/powerops-plan/references/licensing.md`: `# Licences: what needs which, and the standard alternative`; a lead naming SKILL.md sections 3, 5 and 6; `## 1. The table` (the columns, the `Held` values and the tier rules of rows 1 to 3); `## 2. Needs` with one row per need (premium connectors, HTTP and custom connectors; Dataverse in a canvas app; a model-driven app; a Copilot Studio agent; Power BI sharing; a Power Pages site; a Managed Environment; and the standard ones: SharePoint lists under `data.md`'s gates, standard connectors with Microsoft 365; a note that Dataverse for Teams is no alternative and that the Developer Plan creates no developer environment from 2026-11-15), each `\| Need \| Licence \| Standard alternative \|` with a source; `## 3. Part numbers`, `\| Part number \| Product \|` for the products the researcher confirmed, with "A part number not in this table is `unknown`."; `## Sources` with "Checked on 2026-11-11" and the Learn pages | new file | row 12 (f) |
| 8 | `data.md` gate 7 gains one sentence: "Which licence covers what, and the standard alternative: `licensing.md`." | `references/data.md:33` | row 12 (f) |
| 9 | Review-plan item 3 gains: "A plan with no `### Licences` table under `Constraints`, or a row whose `Held` is `unknown` with no step that checks it before its first use: Medium." | `skills/powerops-review-plan/SKILL.md:37` | row 12 (g) |
| 10 | `docs/PLANNING-SKILLS.md`: "Questioning protocol" (`:157-174`) gets the option format, the cost tier in place of "one licensing question in round 1 or 2", the defaults batch and the budget; "Depth and versions" (`:178`) names the cost tier in `quick`; "Fast path" item 1 (`:186`) the `unknown` table and its check step; "Plan files" (`:153`) the new word ceiling; "Pairing with `pac` and `powerops`" gains a paragraph "**Licences.**" after the `plan facts` rules (`:249`): the skill runs the one `az rest` read under row 1's condition, keeps the part numbers out of the plan, maps them through `licensing.md`, and asks neutrally otherwise | `docs/PLANNING-SKILLS.md` | read in the work review |
| 11 | `docs/ecosystem/compatibility.md`: a row `Azure CLI (az)`, version "not pinned", where: `skills/powerops-plan/SKILL.md` section 3 (the licence read) and the CLI's token reads; evidence: 2.92.0 current on 2026-11-11, `az rest` GA and its token resource taken from `--url`; limits: the Graph scopes of its default token are not documented, and Conditional Access or mandatory MFA can refuse the sign-in; a row `Microsoft Graph licenseDetails` (v1.0, hosted), limits: `LicenseAssignment.Read.All` least privileged, `User.Read` enough, and a directory role with `users/licenseDetails/read` named as also needed; the skill reads `skuPartNumber` only; both `Verified` 2026-11-11 | `docs/ecosystem/compatibility.md` | `EcosystemDocsTests` |
| 12 | `SkillsCommandTests`, written first and seen failing: (a) the step 8 condition sentence, `me/licenseDetails`, `never counts as held`, `person says`, `Test users' licences are always the person's answer`; (b) `` `### Licences` `` and the five-column header; (c) `at most six words`, `at most 20 words`, `never states a fact about the person's tenant, licences or data`, the three tier labels, `five rounds or fewer`, `` `## Round <k>` ``; (d) `"Keep these N defaults?"`, `"Confirm these N assumptions?"`; (e) the fast path's `unknown` line; (f) `licensing.md` ships (the install test's reference list gains it), has its four headings and `Checked on 2026-11-11`, and `data.md` names it; (g) the review item's sentence. `Plan_skill_stays_under_its_word_ceiling` gets the new count with an "M91: plus N" note | `tests/PowerOps.Cli.Tests/SkillsCommandTests.cs` | `dotnet test --filter "SkillsCommandTests\|PlanDocumentTests"` |
| 13 | Smoke: `:249`'s licensing check stays (the tier question's heading holds "licen"); scenarios 5 and 6 gain `grep -q '^### Licences' "$(plan_dir)/plan.md" \|\| fail "no ### Licences table in plan.md"` | `tests/smoke/skills-smoke.sh` | the smoke run of step 5 |
| 14 | `plugins/*/skills/` regenerated by the "Update goldens" recipe | `plugins/claude/skills/`, `plugins/codex/skills/` | `SkillsCommandTests`, the read diff |

## Automation

None.

## Screens

None.

## Environments and route

None. Nothing in this milestone calls a tenant: the licence read is text in a skill, and the smoke script's `pac` is a fake that is never signed in and allows only `powerops` commands, so the read never runs there.

## Solution and publisher

None.

## Constraints

- AGENTS.md: the smallest change that fully solves the task; tests first; `dotnet format --verify-no-changes` clean; no version or changelog edits; no tenant values in any file. The skill text stays ASCII (`Plan_skill_stays_under_its_word_ceiling`).
- The chain rules (`reports/trial-3/chain/chain-rules.md`): no tenant call, the kept roles and `tests/eval/` frozen, the merge lock, sign-off by this session on the owner's standing instruction.
- `PlanFactsSkillTests`: every line naming `powerops plan facts` keeps its condition on the line; the fast path names no "environment facts" read. The new step 8 does not name `powerops plan facts`.
- The smoke script's `check_decisions` needs every `## Round` to hold one to three `### Q`, option 1 marked recommended; scenario 5 needs one round under `quick`, with a licensing and an environments question and at most one other. So the defaults question is asked at approval only under `standard` and `deep`, and under `quick` B keeps them.
- Shared files with later milestones: `SkillsCommandTests.cs` and `plugins/` (M93 to M97 follow). Goldens are regenerated, never hand-merged, after each rebase.
- Ecosystem rule: the `az` and Microsoft Graph claims were verified through `powerops-researcher` on 2026-11-11 and are written in Assumptions and the two compatibility rows; what stays unsupported is said in both.

## Assumptions

- Verified on 2026-11-11 by `powerops-researcher` (Learn and the Azure CLI repository only, no tenant call; the Log has each source):
  - The read is **unsupported, not refuted**, so it stays with its fallback. `az rest` derives the token's resource from `--url`, so a Microsoft Graph URL gets a Graph token (Learn's `az rest` reference, updated 2026-11-03; azure-cli `dev` at `51552fd`, `util.py` `send_raw_request`). Learn's "List licenseDetails" page names `LicenseAssignment.Read.All` as least privileged and `User.Read` as enough, neither needing admin consent, but adds that the signed-in user must hold a directory role with `users/licenseDetails/read`; whether `/me` is exempt is not documented. Which Graph scopes the Azure CLI app's default token holds is not documented. Conditional Access and mandatory MFA can refuse the sign-in. So a refused, failed or empty read falls back to the neutral question (row 1), and M94's preflight re-checks.
  - `az` 2.92.0 (2026-11-03) is current, and `az rest` is GA.
  - Part numbers from Learn's "Product names and service plan identifiers for licensing" CSV (page updated 2026-09-29): `POWER_BI_PRO`, `PBI_PREMIUM_PER_USER`, `POWER_BI_STANDARD`, `POWERAPPS_PER_USER`, `POWERAPPS_PER_APP_NEW`, `POWERAPPS_DEV`, `POWERAPPS_VIRAL`, `POWERAUTOMATE_ATTENDED_RPA`, `FLOW_PER_USER`, `Power_Automate_per_process`, `FLOW_BUSINESS_PROCESS`, `FLOW_FREE`, `VIRTUAL_AGENT_USL`, `CCIBOTS_PRIVPREV_VIRAL`, and the Power Pages authenticated-user packs. Which part number is the Copilot Studio credit pack is not documented, so `licensing.md` lists none for it and such a tenant reads `unknown`.
  - Each need of row 7, from the Power Platform licensing FAQ (2026-10-08), the Fabric licences page (updated 2026-11-10), Copilot Studio billing (updated 2026-11-05) and Managed Environment licensing (updated 2026-09-01): premium, custom and gateway connectors and the HTTP action need Power Apps or Power Automate Premium (or per app, Process); Dataverse and model-driven apps are premium; Dataverse for Teams is deprecated (no new environments from 2026-11-15), so it is no alternative; Power BI sharing needs Pro or PPU unless the content is on F64 or larger, or a P SKU; Power Pages is licensed by capacity per site per month; every user in a Managed Environment needs a premium licence, enforced from February 2027; Microsoft 365 covers canvas apps and cloud flows with standard connectors.
- `--query "value[].skuPartNumber" -o tsv` works on `az rest`, because `--query` and `-o` are global arguments (Learn's global parameters); not run here.
- `claude` is installed and signed in on this machine, so step 5 can run the Claude smoke; its cost is under the owner's approval in the fix plan (E3 estimate USD 6 to 25). The Codex smoke is not run.

## Open questions

## Questions for your admin

## Steps

Each step names its files, when it is done and how that is checked. Steps run one at a time.

1. **Plan.** This plan, `decisions.md`, the researcher's results in Assumptions, a fresh-context `powerops-review-plan` into `review.md`, the Fable plan review, fixes, and the sign-off. Files: `docs/plans/m91-planning-licences/`. Done when: both reviews leave no finding open and `plan status` shows the plan signed off with no violation. Check: `powerops plan status docs/plans/m91-planning-licences`.
2. **Pins first.** Row 12. Files: `tests/PowerOps.Cli.Tests/SkillsCommandTests.cs`. Done when: the new assertions run and fail against the unchanged skills. Check: `dotnet test --project tests/PowerOps.Cli.Tests --filter SkillsCommandTests` shows the expected failures.
3. **Skill text.** Rows 1 to 9. Files: `skills/powerops-plan/SKILL.md`, `skills/powerops-plan/references/licensing.md`, `skills/powerops-plan/references/data.md`, `skills/powerops-review-plan/SKILL.md`. Done when: `dotnet test --filter "SkillsCommandTests|PlanDocumentTests|PlanFactsSkillTests"` passes, apart from the plugin-copy checks that step 4 regenerates. Check: that run.
4. **Docs, smoke checks, goldens.** Rows 10, 11, 13, 14. Files: `docs/PLANNING-SKILLS.md`, `docs/ecosystem/compatibility.md`, `tests/smoke/skills-smoke.sh`, `plugins/*/skills/`. Done when: the "Update goldens" recipe ran, `git diff tests/golden plugins` holds only the skill changes of step 3, `EcosystemDocsTests` passes and `bash -n tests/smoke/skills-smoke.sh` exits 0. Check: those.
5. **Smoke run.** `tests/smoke/skills-smoke.sh claude`, in the background, on the committed skills. Done when: it exits 0, or each failure is read and either fixed in the skill text (then steps 3 and 4 rerun) or logged as unrelated to M91 with its reason. Check: its output, quoted by scenario in the Log.
6. **Checks and record.** Files: `decisions.md` (Log), the header's `status:` line. Done when: `dotnet build` with 0 warnings, `dotnet test` with 0 failed, `dotnet format --verify-no-changes` exit 0, `plan status` with no violation, the leak scan with 0 hits and the control found, and `status: done`. Check: those commands.
