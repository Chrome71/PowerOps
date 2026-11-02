# M39 report: the Security reviewer's rerun

Evaluated revision: `7b51d80911c2a9e65fdd152a948da004aa275e26`, the commit of step 2. `skills/powerops-review-security/SKILL.md`, `tests/eval/roles/run.sh`, the cases and the key are as they stand there. No defect led to a new freeze.

A fresh clone of that commit built clean, passed `dotnet test` (2,814 tests: 2,809 passed, 5 skipped, 0 failed) and passed `dotnet format --verify-no-changes`. That test run holds `Every_solution_case_has_a_baseline_file_that_matches_the_live_output`, so the baseline still matches the freeze.

All conclusions here are provisional, as in M38. There was one valid run per case and family, and the numbers in the keep rules are judgement calls.

## Result

| Keep rule | Limit | Result | Reading |
|---|---|---|---|
| Finds its own flaws marked `baseline: not-shown` | At least 15% | 8 of 8 (Claude 5 of 5, Codex 3 of 3) | At or above the floor |
| False Highs across the clean-case runs on solution-02 and solution-06, pooled | At most 1 | 0, from 1 High in the 4 runs | Within the limit |

Passing a floor permits keeping and never requires it, so step 4 stops for the person.

Two results need reading with those numbers:

- **Codex's count is 3 of 3, not 5 of 5.** Its solution-01 left the denominator under the replacement rule, which takes 2 `not-shown` flaws with it (see [Replaced or excluded runs](#replaced-or-excluded-runs)). Counted as missed, they would make Codex 3 of 5 and the pool 8 of 10, 80%, still above the floor.
- **The one High is close to the finding M38 settled as false.** Codex's S1 on solution-02 makes the same point this time, and both scorers supported it from the case files, so the rule doesn't count it (see [Clean cases](#clean-cases)). Read as M38 read it, that is 1 false High, still within the limit.

## Setup

| | Claude Code | Codex |
|---|---|---|
| Client version | 2.1.316 | codex-cli 0.166.0 |
| Model requested | `opus` | `gpt-6-astra` |
| Model the client reported | `claude-opus-5-5` in all 6 streams and reports | The stream names no model. The 5 valid reports said `GPT-6` in 3 runs and `unknown` in 2 |
| Effort requested | high | high |

These are M38's versions, models and effort. Neither stream shows the effort, so effort is what `meta.txt` says was requested. Every run's `meta.txt` shows the freeze commit and `limit-seconds: 1800`. The two clients ran side by side, each one run at a time, from 2026-11-02 14:38 to 15:02 UTC, with `run.sh` run from a clean clone at the freeze.

## Results by family

A flaw counts once per case and run. Two blind scorers, one from each family, scored 12 neutral copies on their own: a Sonnet subagent and Codex. They agreed on all 24 pairs of copy and flaw, and neither touched the mapping. The 10 found pairs of its own flaws in the valid runs were read against the findings, and each one named the planted flaw.

| Flaws | Claude | Codex | Pooled | M38, each client |
|---|---|---|---|---|
| Own, `not-shown` | 5 of 5 | 3 of 3 | 8 of 8, 100% | 5 of 5 |
| Own, `shown` | 1 of 1 | 1 of 1 | 2 of 2 | 1 of 1 |

Not in the keep rules: the pass also found the Verifier's flaws, which are unkeyed for it, 6 of 6 on Claude and 2 of 5 on Codex. M38 measured 6 of 6 and 2 of 6.

Findings, High/Medium/Low: Claude 7/23/18 in 6 runs, Codex 5/12/0 in its 5 valid runs.

## Clean cases

solution-02 and solution-06 have no keyed flaw. The 4 runs on them raised 1 High: Codex's S1 on solution-02. Claude's two runs and Codex's run on solution-06 raised none.

S1 says that the flow gives the service desk a new way to read work item names from every business unit, and that nothing classifies those names or says who may read the log. Both scorers supported it from the case files, one calling its High rating generous, so it is recorded as an unkeyed finding, not a false High. Its point is close to M38's Codex S1 on the same case. There the scorers split, and it was settled as false because the plan records the choice on purpose and the finding shows no defect. That reasoning would fit this finding too, so 0 is the generous count and 1 the strict one.

## Replaced or excluded runs

No run hit a quota, rate-limit or other infrastructure failure, and all 14 runs exited 0. Everything below was settled before any report was scored, under M38's rule: a search that returned file names from outside the temp folder is a read, and it gets one replacement.

| Case | First run | Replacement | Outcome |
|---|---|---|---|
| Codex, solution-01 | Searched the system temp folder for `AGENTS.md`, and 4 file names came back | Did the same, and 4 file names came back | Failed the same way, so the case leaves Codex's denominator. It held 2 `not-shown` flaws and 1 Verifier flaw |
| Codex, solution-04 | Searched the system temp folder for `AGENTS.md`, and 4 file names came back | Clean | The replacement counts, and the first run is kept on file |

Each returned list held file names only, 2 of them in the run's own throwaway home folder and 2 in other sessions' temp folders, and no run opened one of those files. `run.sh`'s own search flagged no run. The excluded run's copy was scored with the others, and its result is in no count here. All Claude runs and the other Codex runs had no such search.

## The check for outside reads

A separate check listed the absolute paths in each run's commands and tool inputs that lie outside its temp folder. Besides the searches above:

- Codex's replacement on solution-04 tested for `/tmp/.agents` and `/tmp/.codex`. Neither exists and nothing came back, so under M38's rule this isn't a read, and the run stays valid.
- Claude's two hits on solution-01 are search globs, not paths.

Codex's web search ran 5 times in the valid runs and 7 times in all 8 of its runs, recorded as a difference between the families, as in M38. Claude had no such tool.

## Wrong states

None. In the 11 valid reports, and the 3 kept on file, every report follows the table: `blocked` with a High open, and `ready` with none. Each has the four header lines and `fresh-context: yes`.

| State | Claude | Codex, valid |
|---|---|---|
| `blocked` | 4 | 4 |
| `ready` | 2 | 1 |
| `failed` or `dropped` | 0 | 0 |

## Stray writes

None. No run changed a file in its case besides its own report, and no run wrote outside its temp folder.

Claude's allowlist refused 5 commands in 3 runs, and those runs went on. Four were a `powerops` command with `; echo` added, as in M38. One was a `diff -r` of the two solution folders, which the skill's list doesn't name.

## Tenant claims

The scorers listed 10 findings between them, all from Claude's runs, and each was settled against "drawn from an export". No keep rule counts them. These were settled by the coordinating session from the plan text, with no third reader.

| Run | Finding | Scorers | Settled |
|---|---|---|---|
| Claude, solution-05 | S3 says nobody in the team gets access until someone assigns the profile, from a profile that names no team | One | A claim, as M38 settled the same point |
| Claude, solution-01 | S1 says what all 6 supervisors could do | Both | Not a claim: the plan gives the number, and the role's privileges are in the export |
| Claude, solution-05 | S1 says the unsecured column reaches about 40 other staff | Both | Not a claim: the plan gives the number and says they use the table |
| Claude, solution-02 | S7 says the flow runs on an identity that also writes | Both | Not a claim: the export shows both flows on one reference, and the plan chose that |
| Claude, solution-06 | S7 says a read-only flow runs with more access than it needs, on the same service account | Both | Not a claim: as above, and the plan names the service account |
| Claude, solution-02 | S1 says the log's readers have no Dataverse access | One | Not a claim: the plan says so |
| Claude, solution-02 | S2 says the data policy grouping lets other flows post anywhere | One | Not a claim: the plan gives the grouping, and the finding says the policies weren't seen |
| Claude, solution-03 | S1 says the disclosure starts on import | One | Not a claim: the flow's state is in the export, and the rest is a forecast |
| Claude, solution-04 | S6 is on the service account's reach | One | Not a claim: it is conditional, and says the privileges aren't shown |
| Claude, solution-06 | S6 is on the service account's reach | One | Not a claim: it is conditional, and says the roles weren't visible |

That makes 1 claim, against the skill's rule never to infer tenant configuration from a clean export. M38's pass had 1 of its 3 here, on the same point of the same case.

## Cost and time

Tokens: uncached input (for Claude, input plus cache writes), cached reads, and output. Dollars are Claude Code's list-price figure, not a bill. Codex reports no cost.

| Client | Runs | Time | Uncached in | Cached | Output | Cost |
|---|---|---|---|---|---|---|
| Claude | 6 | 1,004 s | 321,135 | 1,306,563 | 106,138 | $4.95 |
| Codex, valid | 5 | 826 s | 165,742 | 1,160,704 | 17,908 | not reported |
| Codex, all | 8 | 1,377 s | 257,528 | 1,678,720 | 28,806 | not reported |

| | Expected | Actual, Claude | Expected | Actual, Codex, all 8 runs |
|---|---|---|---|---|
| All tokens | 1.8M | 1.73M | 1.6M | 1.97M |
| Time, one run at a time | 18 min | 16.7 min | 10 min | 23 min |
| List price | $5.29 | $4.95 | not reported | not reported |

Codex took about a minute longer a run than in M38, and its 3 extra runs add to its total. The longest run took 218 seconds, against the limit of 1,800. Claude's weekly window read 14% used at the end, and its stream never refused a run.

Scoring came on top: the Sonnet scorer used 134,000 tokens in 2.5 minutes, and the Codex scorer 693,000 input tokens, 660,000 of them cached, and 4,500 output.

## Limits

- The pass scored at the ceiling again, so the rerun shows that narrowing the skill lost nothing this set can see. It says little about harder flaws.
- All 6 cases hold a settings file and a previous solution, so these 12 runs never took the new branch where the settings file is missing. The first-build client check at step 8 is the only run of it.
- Codex's solution-01 is out of these counts, so its 2 `not-shown` flaws were measured on one client only.
- Both Codex searches came from its hunt for an `AGENTS.md` file in a shared temp folder, which `run.sh` can't prevent. M38 had the same on 2 of its 6 Security solution runs.
- A temp folder is a mitigation, not proof that a reviewer was blind.

## What changed against the evaluated text

Checked at step 2, on LF text:

- `skills/powerops-verify/SKILL.md` hashes to its evaluated blob id `69208a2ed044a346bbae9877c8cd74d9e4794df9`, so it moved byte for byte.
- `skills/powerops-review-security/SKILL.md` and `tests/eval/roles/run.sh` differ from their blobs as below, and only as rows 1 and 2 say.

Row 2 lists line 74 of the Security skill. That line names no pass and still points at the right path, so it stays as it was.

`skills/powerops-review-security/SKILL.md` against blob `3ff2d68576056756b3b4003110b8d7919f8e4616`:

```diff
--- a/SKILL.md
+++ b/SKILL.md
@@ -1,37 +1,36 @@
 ---
 name: powerops-review-security
-description: Use to review an approved PowerOps plan, or the solution built from a signed-off one, in docs/plans/<name>/ against the Power Platform Well-Architected Security checklist. Writes security-plan.md or security-solution.md. Works alone and never asks the user questions.
+description: Use to review the solution built from a signed-off PowerOps plan in docs/plans/<name>/ against the Power Platform Well-Architected Security checklist. Writes security-solution.md. Works alone and never asks the user questions.
 license: Apache-2.0
 compatibility: Needs the powerops CLI on PATH. pac is optional.
 ---
 
 # powerops-review-security
 
-Review a plan, or the solution built from it, for security, and write one report. You work alone: do not ask the user anything. Record anything unclear as a finding or as not verified.
+Review the solution built from a plan for security, and write one report. You work alone: do not ask the user anything. Record anything unclear as a finding or as not verified.
 
 ## The request
 
-The request names the plan (`docs/plans/<name>/`) and the pass, `plan` or `solution`.
+The request names the plan (`docs/plans/<name>/`).
 
-- **plan pass:** reads the plan files: `plan.md`, `decisions.md` and `mockups/` if present. Needs status `approved`. Writes `docs/plans/<name>/security-plan.md`.
-- **solution pass:** reads the plan files too, and also runs `analyze`, `settings validate` and `diff` of the solution against the previous version named in the request. Needs the current solution and the settings file, and takes the previous solution if the request names one. Needs status `signed-off`, `implementing` or `done`. Writes `docs/plans/<name>/security-solution.md`.
+Read the plan files: `plan.md`, `decisions.md` and `mockups/` if present. Also run `analyze`, `settings validate` and `diff` of the solution against the previous version named in the request. This needs the current solution, and takes the settings file and the previous solution if the request names them. Needs status `signed-off`, `implementing` or `done`. Writes `docs/plans/<name>/security-solution.md`.
 
-If the request names no plan or no pass, there is no report path: reply with `state: failed` and the missing input, and write nothing.
+If the request names no plan, there is no report path: reply with `state: failed` and the missing input, and write nothing.
 
 ## Commands you may run
 
 This list is instruction, not enforcement. Run nothing else.
 
-- `powerops plan status docs/plans/<name> --json`: both passes. Gives `status` and `bodyHash`.
-- `powerops analyze <current solution>`: solution pass.
-- `powerops settings validate <current solution> --settings <settings file>`: solution pass.
-- `powerops diff <previous solution> <current solution>`: solution pass, only with a previous solution. With none, skip it and write under the header lines of the report that `diff` was skipped because the request named no previous solution.
+- `powerops plan status docs/plans/<name> --json`: gives `status` and `bodyHash`.
+- `powerops analyze <current solution>`
+- `powerops settings validate <current solution> --settings <settings file>`: only with a settings file. With none, skip it, write under the header lines of the report that `settings validate` was skipped because the request named no settings file, and record what depended on that check as not verified.
+- `powerops diff <previous solution> <current solution>`: only with a previous solution. With none, skip it and write under the header lines of the report that `diff` was skipped because the request named no previous solution.
 - `powerops plan facts`: only if your request says the user confirmed, in that session, that `pac` is signed in to the plan's `environment:`. Never run `pac` yourself. Without that confirmation, do not run it, and write under the header lines of the report that environment facts were not read.
 
 ## Check the status
 
 1. Run the `plan status` command above. Note `status` and `bodyHash`. If there is no `bodyHash`, write `plan-hash: unknown`.
-2. The plan pass needs `approved`. The solution pass needs `signed-off`, `implementing` or `done`, and `approved` is wrong for it. Any other status is wrong for both.
+2. The plan needs `signed-off`, `implementing` or `done`, and `approved` is wrong for it. Any other status is wrong.
 3. If a required input is missing or the status is wrong, the state is `failed`. Write the report with the header below, name the missing input or the status under the header lines, and stop.
 
 ## Review
@@ -93,7 +92,7 @@
 
 Under the header lines:
 
-- Solution pass only: a line `input: <solution path or zip file name>`. Nothing parses it.
+- A line `input: <solution path or zip file name>`. Nothing parses it.
 - Any note the commands list asks for.
 - A `## Coverage` section with one line per item of "Review", in that order, so a reader sees every item was covered: `- <item>: <outcome>`.
 - The findings:
@@ -119,7 +118,7 @@
 - Number findings S1, S2, ... across the whole file, not per severity.
 - Every finding has all four lines. Start each with `Resolution: open`.
 - If a severity has no findings, write "No findings." under it.
-- High means the plan or solution should not go on as it stands. Medium means a gap that will cause trouble later. Low means polish.
+- High means the solution should not go on as it stands. Medium means a gap that will cause trouble later. Low means polish.
 - Before writing, replace URLs, GUIDs, emails and people's names with placeholders such as `<url>`, `<guid>`, `<email>`, `<person>`. Reports are committed.
 - Write only this report. Never open `plan.md` for writing.
 
```

`tests/eval/roles/run.sh` against blob `56f4363bfefd4c28c730f4cd216231895aeca900`:

```diff
--- a/run.sh
+++ b/run.sh
@@ -3,7 +3,7 @@
 # It costs model quota, so it runs by hand, never per build. The set is described in docs/plans/m38-agent-team/plan.md.
 # Keep it bash 3.2 compatible (macOS): no case-changing expansions, no array-reading builtins, and guard empty arrays under set -u.
 #
-# Usage: tests/eval/roles/run.sh claude|codex review-plan|security-plan|security-solution|verify <case folder>
+# Usage: tests/eval/roles/run.sh claude|codex review-plan|security-solution|verify <case folder>
 #        tests/eval/roles/run.sh --list <case folder>     (prints the files of the case, and starts nothing)
 # Needs EVAL_MODEL and EVAL_EFFORT in the environment, a clean tests/eval, src and skills, dotnet, git, and the client signed in.
 # Results go in ${EVAL_OUT:-$HOME/.local/state/powerops-role-eval}/<client>-<pass>-<case>-<UTC time>/.
@@ -11,7 +11,7 @@
 set -euo pipefail
 
 usage() {
-  echo "usage: run.sh claude|codex review-plan|security-plan|security-solution|verify <case folder>" >&2
+  echo "usage: run.sh claude|codex review-plan|security-solution|verify <case folder>" >&2
   echo "       run.sh --list <case folder>" >&2
   exit 2
 }
@@ -31,7 +31,7 @@
 pass="$2"
 case_arg="$3"
 case "$client" in claude | codex) ;; *) usage ;; esac
-case "$pass" in review-plan | security-plan | security-solution | verify) ;; *) usage ;; esac
+case "$pass" in review-plan | security-solution | verify) ;; *) usage ;; esac
 [ -d "$case_arg" ] || { echo "error: case folder not found: $case_arg" >&2; exit 2; }
 case_dir="$(cd "$case_arg" && pwd -P)"
 case_name="$(basename "$case_dir")"
@@ -50,9 +50,8 @@
 
 case "$pass" in
   review-plan) skill_name=powerops-review-plan; skill_file="$repo/skills/powerops-review-plan/SKILL.md"; report=review.md ;;
-  security-plan) skill_name=powerops-review-security; skill_file="$here/skills/powerops-review-security/SKILL.md"; report=security-plan.md ;;
-  security-solution) skill_name=powerops-review-security; skill_file="$here/skills/powerops-review-security/SKILL.md"; report=security-solution.md ;;
-  verify) skill_name=powerops-verify; skill_file="$here/skills/powerops-verify/SKILL.md"; report=verify.md ;;
+  security-solution) skill_name=powerops-review-security; skill_file="$repo/skills/powerops-review-security/SKILL.md"; report=security-solution.md ;;
+  verify) skill_name=powerops-verify; skill_file="$repo/skills/powerops-verify/SKILL.md"; report=verify.md ;;
 esac
 [ -f "$skill_file" ] || { echo "error: the skill is missing: $skill_file" >&2; exit 2; }
 
@@ -161,8 +160,7 @@
 
 case "$pass" in
   review-plan) prompt="Use the powerops-review-plan skill to review the plan $plan_name." ;;
-  security-plan) prompt="Use the powerops-review-security skill. Plan: $plan_name. Pass: plan." ;;
-  security-solution) prompt="Use the powerops-review-security skill. Plan: $plan_name. Pass: solution. Current solution: solution/current. Previous solution: solution/previous. Settings file: settings.json." ;;
+  security-solution) prompt="Use the powerops-review-security skill. Plan: $plan_name. Current solution: solution/current. Previous solution: solution/previous. Settings file: settings.json." ;;
   verify) prompt="Use the powerops-verify skill. Plan: $plan_name. Current solution: solution/current. Previous solution: solution/previous. Settings file: settings.json. Test folder: flowtests. There is no live evidence." ;;
 esac
 
```
