---
status: implementing
review: done
signed-off: 2026-11-03
signed-off-hash: aaefe52777306060e53f92574c3036be657e88274950b34540d435a414f0a1ad
environment: null
depth: standard
supersedes: null
---

# M41: The size brake

## Goal

Make a plan say why each table, list, flow and screen exists, and make the review question one that is too big. This is M41 of Phase 7 ([plan](../phase7-power-platform/plan.md), build-order item 76, release label v0.24.0). It changes the text of `powerops-plan` and `powerops-review-plan` only, with no product code. Decision 40 rules that the ledger is not an input change to the pinned roles, and decision 45 adds two plan cases to the evaluation set.

Done when:

- `powerops-plan` writes the ledger: numbered Goal lines and a `Won't build` list at the end of `Goal` (or the line "Nothing was cut."), one row per table and list in `Data` with the seven columns of the Phase 7 plan, and a Goal line column in `Automation` and `Screens`.
- `powerops-plan` reads `references/data.md` when a plan names new data or a flow, on both the question-round and the fast path, and asks the data round when a plan names new data, with the `quick` and fast-path rules.
- `skills/powerops-plan/references/data.md` holds the store decision, the eight SharePoint gates with the numbers Learn states, table design, the flow card and a dated Sources list.
- `powerops-review-plan` has item 11, Size.
- A test holds `powerops-plan/SKILL.md` at or under 2,783 words and ASCII-only, and a test shows that `skills install` writes `powerops-plan/references/data.md` beside its `SKILL.md` for both targets.
- `plan-13` (oversized, three keyed problems) and `plan-14` (compact, none) are in `tests/eval/roles`, `RoleEvalTests` passes with the new counts, and the `review-plan` pass of `run.sh` ran on both cases with Claude Code and with Codex. On each client measured, item 11 names the three keyed problems on `plan-13` and raises no High on `plan-14`. The client versions, cost, time and scores are in `report.md` in this folder, and a client that can't be measured under the replacement rule is written there and in BUILD-PLAN as not measured.
- `powerops-verify` and `powerops-review-security` keep their blob ids (`69208a2ed044a346bbae9877c8cd74d9e4794df9` and `6a6a6a1fdb71eb16d09cf3ed99406d04a0982a1f`), and no eval baseline file changes.
- The plugin copies are regenerated, `docs/PLANNING-SKILLS.md` and `docs/BUILD-PLAN.md` say what changed, and `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan are clean, with one `feat:` PR open.

### Won't build

- A tool that parses the ledger or compares it with a build: `analyze` takes no plan, SharePoint lists aren't in an export, and a new command would be a pinned change to the Verifier.
- A count rule on tables or lists: no source gives a maximum.
- Any change to `plan status`, the plan header, the hash, or the text of the two pinned roles.
- Product reference files, the prototype and the Power BI branch: M42.
- New `skills-smoke.sh` scenarios for the data round: M42 adds them, as the Phase 7 plan says.
- A rerun of the pinned roles: decision 40.
- The "non-lookup" qualifier on indexed columns, which the Phase 7 plan's gate text carries: no Learn page found states it.

## Users

- A maker who plans a Power Platform solution with an agent and wants the plan sized to its goal before a Microsoft build skill starts.
- The person who signs plans off, and reads item 11's findings.
- A contributor who reruns the evaluation set after changing `powerops-review-plan`.

## Data

Line numbers are those of `1c3a2ce`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | Section 5 gains four bullets before the `Constraints` one. `Goal` writes its outcomes as numbered Goal lines, then ends with a `### Won't build` list, one `- ` item per thing left out of this version and why, or the line "Nothing was cut.". `Data` has one row per table and list: `\| Artefact \| Kind \| Store \| Goal line \| Reuse, Extend or Create \| Why not a column or choice \| Rows now, 3 years \|`, whose Goal line is the number of the line that would fail without it. `Automation` and `Screens` give each flow and screen its Goal line. When the plan names new data or a flow, read `references/data.md` beside this file. Section 5 is the file format both the rounds and the fast path write, so the pointer reaches both | `skills/powerops-plan/SKILL.md:83` | wording test (row 6) |
| 2 | Section 6 gains a **Data round** paragraph after the two round rules: when the plan names new data, ask, recommended answer first and skipping what the request or facts answer: where the data lives today (connect, don't copy); rows in the largest table now and in three years, and the widest filter; who sees which rows; which existing table, list or standard table covers each thing named; offline and Managed Environment; what is out of this version. Under `standard` and `deep` they fill successive rounds of at most three. A data question waits a round when the licensing or environments question is due. Under `quick` only the first joins its one round; the ledger's other cells come from the request or go under Assumptions, and unknowns under Open questions. The fast path asks none and does the same | `skills/powerops-plan/SKILL.md:122-127` | wording test |
| 3 | New `skills/powerops-plan/references/data.md`: when to read it; the store decision (every constraint first, then a system of record stays where it is through a named integration, then new data under a SQL mandate to Azure SQL with Entra sign-in and server-side row security, under any other constraint to Dataverse, under none to a SharePoint list only when every gate holds, else Dataverse; conflicting constraints go under Open questions); the eight gates with Learn's numbers, gate 3 worded as SharePoint's list view lookup threshold and gate 4 with the connector's 600 calls per connection every 60 seconds; elastic, virtual and Dataverse for Teams; table design; what each ledger column takes; the flow card, with the action-count formula of the Power Automate licensing FAQ as a reference line; Sources with Learn links and the date checked. It names no `powerops` command, so the drift tests that read only `SKILL.md` files miss nothing | new file | install test (row 7), wording test |
| 4 | Section 3 gains item 11, Size, after item 10: for each table, list, flow and screen, which Goal line would fail if it were removed, with the seven severities of the Phase 7 plan (three High, four Medium; the title-only rule also excuses business editing, as `data.md`'s table design does), the gates in one sentence so the review needs no other file, and one finding per problem that lists every artefact it covers | `skills/powerops-review-plan/SKILL.md:43` | wording test |
| 5 | `Plan_skill_stays_under_its_word_ceiling`: the shipped `powerops-plan/SKILL.md` is ASCII-only, so a split on whitespace counts as `wc -w` does, and its words are at most 2,783 (today's 2,483 plus 300). If the text runs over, the wording of the six data questions is shortened first, never the `quick` rule. M42 adds to the same skill; its plan sets a new ceiling if it needs one, and records why | `tests/PowerOps.Cli.Tests/SkillsCommandTests.cs` | itself |
| 6 | `Plan_skills_carry_the_ledger_the_data_round_and_item_11`: the plan skill holds the ledger header row, "Nothing was cut.", the `quick` and fast-path lines and the pointer to `references/data.md`, the pointer before section 6; the reference holds the eight gates, the flow card and Sources; the review skill holds "11. Size" and the gate sentence | same file | itself |
| 7 | `Install_writes_the_plan_reference_beside_its_skill`: after `skills install` into an empty folder, `powerops-plan/references/data.md` exists under `.claude/skills/` and `.agents/skills/`, equal to the shipped text, and the output names it as created. `Rerun_reports_unchanged_and_touches_nothing` counts twice the shipped files instead of a fixed 10, which the reference would break | same file | itself |
| 8 | `cases/plan-13` and `cases/plan-14`, each `docs/plans/<name>/plan.md` and `decisions.md` in the layout rows 1 and 2 write, status `approved`, review `pending`, no environment, and nothing else in the case. They meet `RoleEvalTests`' rules: no URL, GUID or email at all (so `PlanChecker` is clean on the clean case and `FakeValueProblems` has nothing to check), none of the words `Marker()` rejects (flaw, flawed, planted, seeded, eval, m38, key, keys, baseline) and no model name. `plan-13` is a loans plan with one list per depot of the same shape (each with its own reminder flow), four lists that hold only a title, and a list no Goal line needs, and nothing else item 11 would flag: numbered Goal lines that every artefact serves, "Nothing was cut.", every row marked Reuse, Extend or Create with what was checked, and every gate stated. `plan-14` is the same goal at its right size, with every gate stated as holding and every screen and flow serving a numbered Goal line | `tests/eval/roles/cases/` | `RoleEvalTests` |
| 9 | `key.json` gains F40 to F42, all `plan-13`, pass `plan`, point `plan checklist`: the per-depot copies of the list and its reminder flow (High), the title-only lists (Medium), the list no Goal line needs (High). `plan-14` has no entry | `tests/eval/roles/key.json` | `RoleEvalTests` |
| 10 | `RoleEvalTests`: `MainPlans` 12 to 14, `flawedPlan` 9 to 10, the plan-case count 13 to 15, the clean plan cases 3 to 4. `NOTICE.md`: "the 18 cases" becomes 20, and one line says that `plan-13` and `plan-14` use the ledger layout and the other plans the earlier one | `RoleEvalTests.cs:495,502,541,552`, `tests/eval/roles/NOTICE.md:7,12` | itself |
| 11 | The plugin copies of the two skills and the reference, by "Update goldens" | `plugins/claude/skills/`, `plugins/codex/skills/` | `PluginManifestTests` |
| 12 | `docs/PLANNING-SKILLS.md`: a paragraph on the ledger and the data round under "Plan files", and item 11 in the review checklist | `docs/PLANNING-SKILLS.md:146,199-209` | none, docs only |
| 13 | `docs/BUILD-PLAN.md`: M41 marked built in the Phase 7 table and item 76 with the plan link, and an M41 line in the verification record with the evaluation result | `docs/BUILD-PLAN.md:520,537` and the record | none, docs only |
| 14 | `report.md` in this folder: the runs, as M39's report records them | new file | none |

## Automation

The evaluation, by hand, after rows 1 to 11 are committed, because `run.sh` refuses a dirty `tests/eval`, `src` or `skills`:

1. `run.sh claude review-plan` and `run.sh codex review-plan` on `cases/plan-13` and `cases/plan-14`, one run at a time per client, with the setup M38 and M39 used: Claude Code `opus` and Codex `gpt-6-astra`, effort `high`, limit 1,800 seconds. Four runs.
2. M38's replacement rule, decided before scoring: a run that searched outside its temp folder and got file names back is replaced once; if the replacement does the same, that case leaves that client's count, and a client with a case out is written as not measured on that case.
3. Neutral copies of the reports: findings with severity, area and text, new ids, no header lines, shuffled; the mapping kept outside the scorers' folder.
4. Two blind scorers on their own, one per family (a Sonnet subagent and Codex at `medium`), each given the copies, the case files and the three key sentences, never the mapping. For each copy and keyed problem: found or not, the finding that names it, and that finding's Area. For each copy: every High and its Area. A disagreement is settled from the case files and recorded.
5. `report.md` gets the client versions, the models the clients reported, each run's time, tokens and Claude Code's list-price cost, the scores, the Highs on `plan-14`, any replaced run, and a line that M38's plan-reviewer counts predate item 11 and don't compare with these.

It passes when, on each client measured, findings whose Area is item 11 name the per-depot copies, the title-only lists and the list no Goal line needs on `plan-13`, and no item 11 finding on `plan-14` is High. A keyed problem named only under another item is reported apart and doesn't count. Highs on `plan-14` from items 1 to 10 are recorded, not scored.

If a client misses: one revision of item 11's wording only (not the cases, the key or the pass rule), committed, then all four runs again on that commit, so the report describes the text that ships; both rounds go in `report.md`. If the second round also misses, the build stops before the PR and reports to the master session, which decides between a deferral under D11 and shipping item 11 with the miss recorded.

## Screens

None.

## Environments and route

None. No step reads or writes a tenant or runs `pac`. The evaluation runs the two clients on copies of the cases in temp folders.

## Solution and publisher

None.

## Constraints

- AGENTS.md: no dependency, no product code, fake values only, no version or changelog edits, Conventional Commits, `dotnet format` clean.
- No change to the text of `powerops-verify` or `powerops-review-security`, and no eval baseline change. The new cases are plan cases, which have no baseline.
- The evaluation costs model quota: four runs, about what four of M39's runs cost, plus two scorers; eight runs if one revision is needed.

## Assumptions

- The gates' numbers are those a Learn page states, checked on 2026-11-03: the 5,000-item view threshold and filtered views of at most 5,000 items; 20,000 items as the limit for adding or removing an index; a canvas data row limit of 500 by default and up to 10,000; 12 joins per query as SharePoint's list view lookup threshold (cited on the Power Query connector page); Get items at 100 by default and up to 5,000; a filtered Get items without Pagination returning nothing when no match is in the first 5,000 items; the SharePoint connector's 600 calls per connection every 60 seconds; no SharePoint in canvas offline mode; Managed Environment licensing for every user from February 2027; and "SharePoint doesn't fully support ALM scenarios" on the troubleshooting page for data source environment variables. The Phase 7 plan's "non-lookup" index qualifier was not found on Learn, so it is not written.
- `run.sh` copies only `SKILL.md` into a run, so the review skill carries the gates itself and never reads the plan skill's reference.
- `skills install` and the plugin copies already carry every file under `skills/` (the csproj embeds `skills/**/*`, and `Plugin_skills_are_the_shipped_skills` writes subfolders), so the reference needs no code.
- A plan written before the ledger has no Goal line column, so item 11 gives it one High that lists the artefacts naming no Goal line. That is intended: a plan should say why each artefact exists. It reaches drafts written before this ships, approved plans not yet reviewed, and a "New version" that copies an older body.
- Whether an agent follows the pointer and reads `references/data.md` is not tested in M41; M42's smoke scenarios are the first check of the interaction.
- One run per case and client, as in M38 and M39, so the result is provisional.
- Environment facts were not read: this plan changes this repo and targets no environment.

## Open questions

## Questions for your admin

## Steps

1. This plan, a review in a fresh context, fixes, sign-off.
2. Rows 5 to 7 first, and see them fail; then rows 1 to 4 until they pass.
3. Rows 8 to 10, with `RoleEvalTests` passing.
4. Row 11 by "Update goldens", and read the diff.
5. Commit, then the evaluation (Automation) and row 14.
6. Rows 12 and 13.
7. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan (`bash reports/leakscan.sh origin/main <this branch's commit>` from the main checkout); push and open one `feat:` PR. The master session merges.
