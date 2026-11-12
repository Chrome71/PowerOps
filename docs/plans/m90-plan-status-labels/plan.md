---
status: signed-off
review: done
signed-off: 2026-11-11
signed-off-hash: 6654dd41a63de2d187d8b1ecaed0c25542bbd1286ced62b6eb9fe7854dfa9d83
environment: null
depth: quick
supersedes: null
---

# M90: `plan status`: a UI label is not a personal environment name

## Goal

In trial 3 (record P3, E60), `powerops plan status` raised the violation "looks like a personal environment name" on a line of a review report that quoted a UI label, the dialog's "Environment security roles" section. The tenant-data scan's `PersonalEnvironmentRegex` (`src/PowerOps.Core/Plans/PlanChecker.cs`) matches any `'s Environment` in prose, so an ordinary possessive before a capitalised UI word blocks a plan on a false violation. <!-- plan-status: allow --> This plan turns the prose check into an exclusion: a line stays quiet when the word right before `'s` starts with a lower-case letter, as in ordinary prose. Every name caught today is still caught, except, in prose, a name whose last word starts with a lower-case letter; on an `environment:` line a lower-case name is caught too. This is milestone M90 of the trial-3 fix plan (tier 1.0).

Done when:

1. `PersonalEnvironmentRegex` has two alternatives, case-sensitive, with no `RegexOptions`. The prose one is `(?<!\b\p{Ll}[\p{L}\p{M}.-]*)['’]s(?: \(\d+\))? Environment\b`: today's prose pattern, quiet when the word before `'s` starts with a lower-case letter. The `environment:` line one is today's, with its last word widened to `[Ee]nvironment`; the key `environment:` stays lower case.
2. The tests of Data row 2 were run before the change and the new cases failed as row 2 says; after the change every case of both theories passes, and every existing case passes unchanged.
3. The code comment above the regex states the rule and its limits: in prose, a lower-case word before `'s` is read as ordinary prose, so a name whose last word is lower case is caught only on an `environment:` line; a capitalised word before a UI label (`Dev's Environment variables`) still matches, and the allow marker is the way out. <!-- plan-status: allow -->
4. The `plan status` paragraph of `docs/COMMANDS.md` says the same in one clause: in prose, a lower-case word before `'s` is not read as a name.
5. No committed plan gains a violation: `dotnet run --project src/PowerOps.Cli -- plan status .` from the repo root, built from `b065f23` and from the branch, gives the same set of `personal environment name` lines apart from this plan folder's (compared with `diff`, the result in the Log).
6. `dotnet build` (0 warnings), `dotnet test`, `dotnet format --verify-no-changes`, `dotnet run --project src/PowerOps.Cli -- plan status docs/plans/m90-plan-status-labels` (the branch's build, not the installed CLI) with no violation, and the leak scan (`bash reports/leakscan.sh origin/main <branch>`, run from the main checkout, where the git-ignored `reports/` folder lives) are clean; no file under `skills/`, `plugins/`, `.claude/agents/`, `tests/eval/` or `tests/golden/` changed.

### Won't build

- A list of known UI labels to skip, or a dictionary of names. The rule the plan uses, a lower-case word before `'s`, needs neither.
- Telling a capitalised product or UI word before `'s` from a name (`Dev's Environment variables` still fires; the allow marker is the way out). <!-- plan-status: allow -->
- Catching a prose name with a lower-case `environment` (Learn's "John Doe's environment" form) outside an `environment:` line. The prose check is case-sensitive today so that "the user's environment" stays quiet, and this plan doesn't widen it.
- Any change to the URL, GUID or email checks, the allow marker, or the violation text.

## Users

- A maker or agent who runs `powerops plan status` on a plan folder whose review reports quote UI labels: the label no longer blocks the plan.
- The same person when a plan file holds a real Developer environment name: it is still reported, without the name.

## Data

Line numbers are those of `b065f23`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | `PersonalEnvironmentRegex` becomes the two alternatives of Done when 1, in one `[GeneratedRegex]` with no options. The existing comment above it is kept and gains the rule and limits of Done when 3. Nothing else in `ScanTenantData` changes | `src/PowerOps.Core/Plans/PlanChecker.cs:976-978` | row 2 |
| 2 | Tests, written first, in the two existing theories. `Tenant_data_is_reported_with_file_and_line_but_never_the_value` gains, with kind `personal environment name`: the prose `see Ana's (1) Environment` (value `Ana`); the prose `see Željka's Environment` (value `Željka`, a non-ASCII capital); the header line `environment: jane's environment` (value `jane`); the header line `environment: jane doe's Environment` (value `jane`, which the prose alternative no longer catches); the prose `see Jane Doe (Contoso)'s Environment` (value `Jane`, a name ending in a bracket); the prose `see Doe2's Environment` (value `Doe2`, ending in a digit); the prose `see 李雷's Environment` (value `李雷`, a script with no case). `Prose_and_product_possessives_are_not_a_personal_environment_name` gains `the dialog's Environment security roles`, `the user's Environment` and `Environment: the customer's environment is offline` (a capitalised label is not the header key). Expected before the change: the first two quiet cases fail (each reports a violation); the third quiet case and the seven fires cases already pass. Expected after: all pass, and no existing `InlineData` is edited or removed. Run: `dotnet test --project tests/PowerOps.Core.Tests -- --filter-class "*PlanCheckerTests"` (140 passed, 2 skipped at `b065f23`) | `tests/PowerOps.Core.Tests/Plans/PlanCheckerTests.cs:176-206` | itself | <!-- plan-status: allow -->
| 3 | `docs/COMMANDS.md`, the `plan status` paragraph: after the example names, add Done when 4's clause, "in prose, a lower-case word before `'s` is not read as a name". No other sentence changes | `docs/COMMANDS.md:824` | none reads the wording; `DocsDriftTests` checks flags and line length; the work review reads the sentence |

## Automation

None.

## Screens

None.

## Environments and route

None. `plan status` is offline, and nothing reads or writes a tenant.

## Solution and publisher

None.

## Constraints

- AGENTS.md: the smallest change; new logic with tests, written first; Conventional Commits (`fix:`, since `plan status` output changes); no version or changelog edit; no tenant data in Git. Test names are made up.
- The plan folder is itself scanned by `plan status`, so each line of these files that quotes a matching name carries the allow marker.
- The kept roles, `skills/`, `plugins/`, `.claude/agents/`, `tests/eval/` and `tests/golden/` stay untouched, so the "Update goldens" recipe does not run.
- The fix plan's literal regex is not used: decision Q2 gives why.
- `docs/COMMANDS.md` is shared with M88 and M89 (other sections); it is rebased under the chain's merge lock, never hand-merged blindly.

## Assumptions

Checked on 2026-11-11 by `powerops-researcher` (Learn only; decisions, Log):

- A Developer environment's default name is the user's name followed by `'s Environment` or `'s environment`: Learn shows both casings on two pages, and a second one as `'s (1) environment`. Learn says the name comes from the user's name and shows only capitalised examples; it states no case rule. So a capitalised first letter is the normal case, and a name whose last word is lower case is the known limit (Done when 3). <!-- plan-status: allow -->
- Learn says sign-up stops creating unmanaged Developer environments on 2026-11-15, and doesn't say how environments made by environment routing are named. Plans written before then can still quote such names, so the check stays.

## Open questions

## Questions for your admin

## Steps

1. This plan and `decisions.md`; the Fable plan review and a fresh-context `powerops-review-plan` into `review.md`; fixes; sign-off by the building session on the person's instruction "Start the build" (2026-11-11).
2. Row 2's tests, run and seen to fail as row 2 expects (executor: Sonnet).
3. Rows 1 and 3 until they pass (executor: Sonnet); one `fix:` commit.
4. The Fable work review; then the checks of Done when 5 and 6, the plan's `status: done`, and the chain's merge protocol.
