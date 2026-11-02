# M39 report: the Security reviewer's rerun

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
