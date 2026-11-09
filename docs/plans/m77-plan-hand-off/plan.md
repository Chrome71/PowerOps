---
status: implementing
review: done
environment: null
depth: quick
supersedes: null
signed-off: 2026-11-08
signed-off-hash: dee7dc1166f202d7a96c9a9a3c1028e8afad2e1e5943f2675168d088d53293f2
---

# M77: The build hands plan changes back to the plan, and what `plan sign-off` and `plan status` say

## Goal

Fix four findings of the PowerOps 1.0 trial (2026-11-08), as section 3 of the agreed 1.0 fix plan sets out (Fable and Astra, six rounds). The person put F44 in the 1.0 gate (decisions Q3):

| Id | Finding | This plan |
|---|---|---|
| F44 | `powerops-implement` step 6 already says a choice that changes the plan stops, goes back to `powerops-plan` and needs a new sign-off. The build logged the choice in `decisions.md`, which the same step says first, and went on: the order of the step hid the hand-off | step 6 is reordered so the hand-off comes before the log, and `powerops-plan` gets an amendment path (rows 1, 2) |
| F8 | `plan sign-off` signed over a stale `review.md` without a word | sign-off prints the report warnings `plan status` gives (row 3) |
| F14 | `plan status` counted the questions for the admin as open questions, and printed `review.md: state null` | the two counts are apart, and `review.md`'s line leaves out the state it never has (rows 4, 5) |
| F3 | The plan header's `environment:` held a Developer environment's default name, which carries its owner's name | plans hold the environment's role, the build asks the person for the target, and the tenant-data scan catches the default name (rows 6, 7) |

Done when:

1. `powerops-implement` step 6 reads in this order: stop with one question; when the answer changes a plan line (or a step can't be done as written), hand to `powerops-plan` section 10, which edits the line, sets `status: approved`, runs a fresh review for a change of scope, data, security or cost, has the person sign off with `powerops plan sign-off` and logs `amendment: <line> (answered by: person)`; resume only once the header's `signed-off-hash` differs from the one noted before the hand-off; the `decisions.md` log of the question comes last. Section 3 step 1 resumes whenever a step of the plan is already committed, so an amended plan doesn't start its steps again.
2. `powerops plan sign-off` on a plan whose `review.md` was written for another body prints `warning: review.md was written for a different version of the plan (stale review)` on stderr, signs, and exits `0`; with a current `review.md` it prints no such warning.
3. `powerops plan status` prints `<n> open questions, <m> admin questions` and its JSON has `adminQuestions` beside `openQuestions`, each counting its own section, printed with the existing plural rule (`1 admin question`); `schemaVersion` stays 1. The text line of a `review.md` with no `state:` line has no `state`; `verify.md` and `security-solution.md` still print `state null`.
4. A plan folder file with a line such as `environment: Jane Doe's (1) environment`, or `Jane Doe's Environment` anywhere, fails `plan status` and `plan sign-off` with `looks like a personal environment name`, without the name; `environment: Dev`, `the user's environment` and the product possessives already in committed plans pass. <!-- plan-status: allow -->
5. `powerops-plan` writes the environment's role (Dev, Test or Prod) in the header, in the rounds and in the fast path; `powerops-implement` asks the person to name the environment when the header holds one of those roles or null, and gives the build skills that name, never the role.
6. The docs (`COMMANDS.md`, `PLANNING-SKILLS.md`), the schema, the `plan-status.json` golden and the plugin copies of the two skills are current; the plan skill stays within its 2,783-word ceiling; the smoke script has a scenario for the amendment.
7. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m77-plan-hand-off`, the built `powerops plan status .` over the whole repo (0 violations, as at `6c7355d`) and the leak scan are clean; committed locally on the session's branch.

### Won't build

- A sign-off that refuses over a stale review. The warning tells the person; a refusal would block the small edits that don't need a new review (fix plan, rejected).
- A `-v2` folder per amendment. An amendment mid-build edits the line in place and is re-signed; a new version after implementation still uses `-vN` (fix plan, rejected).
- A `plan status` check that an amended plan has an `amendment:` line, or that an agent didn't answer one. The line is the skill's record; the sign-off already warns when an agent signed.
- Reading the header's `environment` in any command. No code reads it today, and none starts to.
- Any change to `powerops-review-plan` or the two kept roles. `review-plan` counts both sections itself (its check 10).
- Running the smoke script here. It calls the agent CLIs and costs model credits; it runs weekly in CI.

## Users

- A person building from a signed-off plan, through an agent that follows `powerops-implement`, who must see every plan change before the build goes on.
- A person signing off a plan, who must know when the review read another version of it.
- A person reading `plan status`, who forwards the admin questions and answers the open ones.
- A maker whose Developer environment carries their name, which must not land in a committed plan.

## Data

Line numbers are those of `6c7355d`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | Implement step 6 keeps its first three sentences (stop by impact, one question, nothing until the answer, no person means stop). Then, in this order: if the answer changes a plan line, or a step can't be done as written, the build stays stopped, notes the header's `signed-off-hash`, and hands to `powerops-plan` section 10 (row 2), which makes the edit: the build still never edits the plan body. A change to scope, data, security or cost gets a fresh `powerops-review-plan` before that sign-off; any other amendment may keep the old review, which the sign-off then names as stale (row 3). The build resumes only once the header's `signed-off-hash` differs from the noted one and `plan status` shows no violation for the plan. Last, the question and its answer are logged under `## Log` with the existing `(answered by: ...)` wording. The sentence on an agent's answer gains "it never changes the plan". Section 3 step 1 keys its resume on committed steps, not on the status: "Skip this if it already says `implementing`. If it does, or a step of this plan is already committed (after an amendment), read the commit messages and `writes.md` (step 10): don't redo a step that is already committed ..." | `skills/powerops-implement/SKILL.md:36, 52` | `SkillsCommandTests.Implement_skill_stops_by_impact_and_keeps_the_plan_body_closed` (`:341`) also pins the hand-off sentence, the noted hash, "never edits the plan body", the fresh-review sentence, and that the hand-off comes before the log sentence (index order); `:321`'s `don't redo a step` stays and the step 1 resume sentence is pinned; `Both_skills_mark_who_answered...` (`:423`) passes unchanged |
| 2 | Plan skill section 10 gets an **Amendment** paragraph: a line `powerops-implement` hands back (its section 3 step 6) is edited in place, even mid-build, `status: approved`, reviewed as that step says (row 1 holds the rule, since the implement skill has no word ceiling), signed off (section 9), then `amendment: <line> (answered by: person)` is logged. "New version" says "not an amendment". Section 9 step 2 also shows `review.md`'s `stale` value and both question counts; section 7 step 1 shows both counts. Words: a draft of rows 2 and 7 adds 36 words and pays them with these cuts, ending at 2,780: section 3 steps 7 to 9 merged into one (row 7); section 8 step 3 drops "A URL or an id stays out", which a role makes moot; section 10's first paragraph drops the stale-review sentence, now in section 9; section 3 step 3 says "nowhere, not even in chat"; the Gemini CLI and GitHub Copilot lines of Per agent become one. No pinned phrase is cut | `skills/powerops-plan/SKILL.md:152, 186, 193-203` | `SkillsCommandTests`: a new test pins the amendment paragraph's key phrases (`edit it in place, even mid-build`, `status: approved`, `review as that step says`, `amendment: <line> (answered by: person)`) and the stale value of section 9 step 2; `Plan_skill_stays_under_its_word_ceiling` (`:438`) passes with the ceiling unchanged (the person set it in M41, Q3) |
| 3 | `PlanSignOff.Run`, on success, also prints the warnings that `plan status` gives for the folder's reports: `PlanChecker.ReadReports` (made `internal`) runs with the body hash, after the tenant scan, into the same `warnings` list. So a stale or hashless `review.md`, a stale `verify.md` or `security-solution.md` and an unknown state are named; the exit code stays `0` and nothing more is written. A refused sign-off prints only its errors, as today | `src/PowerOps.Core/Plans/PlanSignOff.cs:36-37`; `src/PowerOps.Core/Plans/PlanChecker.cs:396` | `PlanCommandTests`: sign-off with a `review.md` whose `plan-hash` is another body exits `0`, writes the header and prints the stale-review warning on stderr; with the current `plan-hash`, stderr has no warning |
| 4 | `PlanDocument` counts `OpenQuestions` (items under `## Open questions`) and a new `AdminQuestions` (items under `## Questions for your admin`) apart, by the same rules (top-level items, a section ends at the next level 1 or 2 heading). `PlanReport` gains `AdminQuestions` after `OpenQuestions`. The text line reads `<path>: <status>, review <review>, <n> open questions, <m> admin questions`, both through the existing `Plural`, so one reads `1 admin question`; the JSON has `adminQuestions` after `openQuestions`. The schema adds `adminQuestions` (integer, required), keeps `openQuestions`' description, which the code now matches, and changes `environment`'s description to "The header's environment: its role (Dev, Test or Prod), or a name in a plan written before M77; null when not set"; `schemaVersion` stays 1 (an added field) | `src/PowerOps.Core/Plans/PlanDocument.cs:28, 43-58, 93-113`; `PlanChecker.cs:9-23, 128, 189`; `PlanStatusReport.cs:25, 68`; `docs/schemas/plan-status.schema.json:15, 21, 24` | `PlanDocumentTests.Counts_top_level_list_items_under_the_two_headings_only` (`:113`) splits: 4 open, 1 admin; `PlanCheckerTests` (`:124`) asserts both; `PlanCommandTests` (`:56`, `:123`) text and JSON, with one admin question in the text case (`1 admin question`); the text expectations of `PlanStatusReportTests` (`:48, 74, 78, 81`) and `PlanCommandTests` (`:63, 349`) gain `, 0 admin questions`; the `plan-status.json` golden by the recipe; `OutputSchemaTests` validates it |
| 5 | Only the text line of `review.md` leaves out `state ...`, and only when its `State` is null: `report review.md: stale (plan) <true|false|null>`. A `review.md` that does have a `state:` line prints it as today; `verify.md` and `security-solution.md` always print their state, `null` included, because a missing state there is a defect to see; the JSON is unchanged | `src/PowerOps.Core/Plans/PlanStatusReport.cs:29` | `PlanStatusReportTests.Text_prints_one_line_per_report...` (`:36`): `  report review.md: stale (plan) null`; a review with a `state:` line keeps it; a `verify.md` with no `state:` line still prints `state null` |
| 6 | `ScanTenantData` gains the kind `personal environment name`, matched per line, case-sensitive (no `IgnoreCase`), by `['’]s(?: \(\d+\))? Environment\b|^\s*environment:.*['’]s(?: \(\d+\))? environment\b`: the capital-E form anywhere, and the lower-case form on an `environment:` line, where F3 put it. Prose such as "the user's environment", a plural (`environments`) and the product possessives in committed plans ("Codex's environment" and two more) are quiet: a form that matched any capitalized word before the lower-case `'s environment` hit those three lines, this one hits none of the 59 plans at `6c7355d`. The violation reads `<file>:<line>: looks like a personal environment name; remove it, or add <!-- plan-status: allow --> to the line`, never the name. Sign-off refuses on it, as on the other kinds | `src/PowerOps.Core/Plans/PlanChecker.cs:572-597, 858-865` | `PlanCheckerTests.Tenant_data_is_reported_with_file_and_line_but_never_the_value` (`:176`) gains `environment: Jane Doe's Environment`, `environment: Jane Doe’s environment`, `environment: Jane Doe's (1) environment` and `see Jane Doe's Environment` (fire, the value `Jane` absent); a new quiet theory: `environment: Dev`, `the user's environment`, `Codex's environment`, `Each maker's environment`, `environment: Jane Doe's environments`. <!-- plan-status: allow --> |
| 7 | Plan skill section 3: steps 7 to 9 become one: once the user confirms the environment, write its role in the header (`environment: Dev`), never its name, which can hold a person's name, and in `decisions.md` "the environment found by plan facts"; refer to every environment by role (Dev, Test, Prod), and offer no other target unless the user names it. Section 8 step 3 keeps "unless the request names an environment" and records its role, `Dev` unless the request says another, not its friendly name. Implement `:41`: when the plan's `environment` is null or one of Dev, Test, Prod (any case), ask the person to name that environment before any tenant read or live check, and give the build skill that name, never the role text; any other value is an older plan's name, used as written (a role outside the three, such as UAT, is taken as a name: the limit is in Assumptions). Implement `:53`: "Use only the environment the person named, or the plan's `environment` when it holds a name, as the target" | `skills/powerops-plan/SKILL.md:46-47, 171`; `skills/powerops-implement/SKILL.md:41, 53` | `SkillsCommandTests.Implement_skill_names_the_dataverse_skills...` (`:301`): its two environment pins change to the new sentences, and `ask the person to name that environment` and `never the role` are pinned; a plan-skill pin for "write its role"; `PlanFactsSkillTests.The_fast_path_takes_an_environment_the_request_names_instead_of_always_leaving_it_null` (`tests/PowerOps.Cli.Tests/PlanFactsSkillTests.cs:66`) keeps its first three asserts and swaps `friendly name` for `then record its role`; the other `PlanFactsSkillTests` pass unchanged |
| 8 | `docs/PLANNING-SKILLS.md`: the plan files hold the target's role (`:35`, `:127`, `:146`, `:183`); the sign-off row shows the stale review and both counts (`:70`); the JSON line counts each section apart (`:271`); a new **Amendments** bullet after **Versions** (`:177`) gives the order of row 1 and row 2. `docs/COMMANDS.md`: the scan's kinds add the personal environment name, sign-off prints the report warnings on success, the text line has both counts and `review.md`'s line has no state (`:146, 148`) | `docs/PLANNING-SKILLS.md`; `docs/COMMANDS.md` | `DocsDriftTests` and `HandoffDocTests` pass unchanged |
| 9 | Smoke scenario 8 (`amendment`), after scenario 7: a signed-off plan (loans, two steps), and a fake `tenant` command that logs each call with the plan's `signed-off-hash` at that moment. `create table Loan` fails while no `Keeper` table exists ("Loan needs a lookup to a Keeper table, which the plan doesn't have"); every other create succeeds. The simulated user owns the plan, says yes to adding a Keeper table and to every write and sign-off. Done when the header's `signed-off-hash` changed and `decisions.md` has an `- amendment:` line. Checks: both, `plan status` has no violation, and no tenant call after the first failed one carries the old hash | `tests/smoke/skills-smoke.sh` | `bash -n tests/smoke/skills-smoke.sh`, and `shellcheck` where installed; a full run is the weekly job's |
| 10 | The plugin copies of `powerops-plan` and `powerops-implement` and the `plan-status.json` golden, by the recipe "Update goldens" | `plugins/*/skills/`; `tests/golden/plan-status.json` | `SkillsCommandTests` copy checks; the golden diff is read |

## Automation

None. Nothing new runs on its own.

## Screens

None.

## Environments and route

None. Two skills, three plan classes, their tests and docs change; no tenant read or write.

## Solution and publisher

None.

## Constraints

- AGENTS.md: the smallest change, new logic with tests, no new write, Conventional Commits, no version or changelog edits, no tenant URLs, ids, emails, user names or environment names in commits. Test values are fake (`Jane Doe`).
- The kept roles (`powerops-verify`, `powerops-review-security`), their plugin copies, `powerops-review-plan` and `tests/eval` stay untouched.
- The plan skill stays at or under its 2,783-word ceiling (`SkillsCommandTests.cs:445`, set in M41) and plain ASCII: the new text is paid for by the cuts row 2 names, which keep every rule and every pinned phrase other than those rows 2 and 7 change.
- M73 and M81 edit `powerops-implement` `:41` and `:53` after this lands (fix plan section 4): this plan changes only the environment sentences there.
- `plan status` JSON keeps `schemaVersion` 1: one field is added, none changes type.

## Assumptions

- A Developer environment's default name holds its owner's name. Learn's *Create a developer environment* (read 2026-11-08) says the environment "uses your name", for example `John Doe's environment`, and a second one `John Doe's (1) environment`; *Develop a tenant environment strategy* (read 2026-11-08) gives `<user name>'s Environment` with a capital E. Learn doesn't say which casing the product uses, so the scan matches both (row 6). The name can be changed in the admin center (*Edit properties of an environment*), so the scan is a net for the default, not a proof. <!-- plan-status: allow -->
- The same Learn page says that from 2026-11-15 the Developer Plan sign-up no longer creates a developer environment. Existing ones keep their names, so the scan still matters.
- Every stop of step 6 is about scope, data, security or cost, so most amendments get a fresh review; an amendment that only rewords a step whose route failed may keep the old review, which the sign-off now names as stale.
- An agent following the skills reads the header's `signed-off-hash` itself: `plan status --json` prints `signedOff` and `bodyHash`, not the hash.
- The role list is closed (Dev, Test, Prod), as section 3 of the plan skill already names it. A plan whose header holds another word, such as UAT, gets it passed to the build skill as a name; the plan skill writes only the three.
- "Signed in to the plan's `environment:`" in plan skill section 7 step 7 and in `powerops-review-plan` now means the environment with that role, as the person named it in the session; both texts stay, since the confirmation is the person's either way.
- The scan misses the lower-case form outside an `environment:` line, a name broken over two lines, a word between the name and `environment` ("Jane Doe's Dev environment") and a renamed environment. <!-- plan-status: allow -->

## Open questions

## Questions for your admin

## Steps

1. This plan and `decisions.md`, the claim to `powerops-researcher`, a fresh-context review into `review.md`, fixes, and the person's sign-off.
2. Rows 4 and 5, tests first.
3. Rows 3 and 6, tests first.
4. Rows 1, 2 and 7, with their pinned-wording tests first.
5. Rows 8 and 9, then row 10 by the recipe "Update goldens".
6. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m77-plan-hand-off` and the leak scan; commit locally.
