#!/usr/bin/env bash
# Runs one pass of the role evaluation on one case, with one client, in a fresh session, and keeps what happened.
# It costs model quota, so it runs by hand, never per build. The set is described in docs/plans/m38-agent-team/plan.md.
# Keep it bash 3.2 compatible (macOS): no case-changing expansions, no array-reading builtins, and guard empty arrays under set -u.
#
# Usage: tests/eval/roles/run.sh claude|codex review-plan|security-solution|verify <case folder>
#        tests/eval/roles/run.sh --list <case folder>     (prints the files of the case, and starts nothing)
# Needs EVAL_MODEL and EVAL_EFFORT in the environment, a clean tests/eval, src and skills, dotnet, git, and the client signed in.
# Results go in ${EVAL_OUT:-$HOME/.local/state/powerops-role-eval}/<client>-<pass>-<case>-<UTC time>/.
# EVAL_LIMIT_SECONDS only exists to test the stop at the limit; meta.txt records the limit that was used.
set -euo pipefail

usage() {
  echo "usage: run.sh claude|codex review-plan|security-solution|verify <case folder>" >&2
  echo "       run.sh --list <case folder>" >&2
  exit 2
}

here="$(cd "$(dirname "$0")" && pwd -P)"
repo="$(cd "$here/../../.." && pwd -P)"

# ---- --list: no model, no git, no build ----
if [ "${1:-}" = "--list" ]; then
  [ "$#" -eq 2 ] && [ -d "$2" ] || usage
  (cd "$2" && find . -type f | LC_ALL=C sort | sed 's|^\./||')
  exit 0
fi

[ "$#" -eq 3 ] || usage
client="$1"
pass="$2"
case_arg="$3"
case "$client" in claude | codex) ;; *) usage ;; esac
case "$pass" in review-plan | security-solution | verify) ;; *) usage ;; esac
[ -d "$case_arg" ] || { echo "error: case folder not found: $case_arg" >&2; exit 2; }
case_dir="$(cd "$case_arg" && pwd -P)"
case_name="$(basename "$case_dir")"

# ---- stop before anything else when the setup is not the frozen one ----
[ -n "${EVAL_MODEL:-}" ] || { echo "error: EVAL_MODEL is not set" >&2; exit 2; }
[ -n "${EVAL_EFFORT:-}" ] || { echo "error: EVAL_EFFORT is not set" >&2; exit 2; }
dirty="$(git -C "$repo" status --porcelain -- tests/eval src skills)" || { echo "error: git status failed in $repo" >&2; exit 2; }
if [ -n "$dirty" ]; then
  echo "error: tests/eval, src or skills has changes; commit or undo them first:" >&2
  echo "$dirty" >&2
  exit 2
fi
command -v "$client" >/dev/null 2>&1 || { echo "error: $client is not on PATH" >&2; exit 2; }
command -v dotnet >/dev/null 2>&1 || { echo "error: dotnet is not on PATH" >&2; exit 2; }

case "$pass" in
  review-plan) skill_name=powerops-review-plan; skill_file="$repo/skills/powerops-review-plan/SKILL.md"; report=review.md ;;
  security-solution) skill_name=powerops-review-security; skill_file="$repo/skills/powerops-review-security/SKILL.md"; report=security-solution.md ;;
  verify) skill_name=powerops-verify; skill_file="$repo/skills/powerops-verify/SKILL.md"; report=verify.md ;;
esac
[ -f "$skill_file" ] || { echo "error: the skill is missing: $skill_file" >&2; exit 2; }

plan_name=""
plans=0
for d in "$case_dir"/docs/plans/*/; do
  [ -f "${d}plan.md" ] || continue
  plan_name="$(basename "$d")"
  plans=$((plans + 1))
done
[ "$plans" -eq 1 ] || { echo "error: the case must hold exactly one plan under docs/plans" >&2; exit 2; }
if [ "$pass" = security-solution ] || [ "$pass" = verify ]; then
  [ -d "$case_dir/solution/current" ] || { echo "error: the case has no solution/current" >&2; exit 2; }
fi

# The person's Codex sign-in file, found before HOME changes; a link in the way is followed to the file itself.
real_auth=""
if [ "$client" = codex ]; then
  codex_dir="${CODEX_HOME:-$HOME/.codex}"
  case "$codex_dir" in /*) ;; *) codex_dir="$PWD/$codex_dir" ;; esac
  real_auth="$codex_dir/auth.json"
  hops=0
  while [ -L "$real_auth" ] && [ "$hops" -lt 10 ]; do
    target="$(readlink "$real_auth")"
    case "$target" in /*) real_auth="$target" ;; *) real_auth="$(dirname "$real_auth")/$target" ;; esac
    hops=$((hops + 1))
  done
  [ -f "$real_auth" ] || { echo "error: no Codex sign-in file at $real_auth" >&2; exit 2; }
fi

head_sha="$(git -C "$repo" rev-parse HEAD)"
client_version="$("$client" --version 2>&1 | head -n 1)" || client_version="unknown"
out_root="${EVAL_OUT:-$HOME/.local/state/powerops-role-eval}"
limit="${EVAL_LIMIT_SECONDS:-1800}"

utc() { date -u +%Y-%m-%dT%H:%M:%SZ; }

# ---- a build of this commit, once ----
build="$out_root/builds/$head_sha"
if [ ! -x "$build/path/powerops" ] || [ ! -f "$build/bin/powerops.dll" ]; then
  mkdir -p "$out_root/builds"
  scratch="$(mktemp -d "$out_root/builds/tmp.XXXXXX")"
  if ! dotnet build "$repo/src/PowerOps.Cli" -c Release -p:RestoreLockedMode=true -o "$scratch/bin" --nologo -v quiet >"$scratch/build.log" 2>&1; then
    cat "$scratch/build.log" >&2
    rm -rf "$scratch"
    echo "error: the build of $head_sha failed" >&2
    exit 3
  fi
  mkdir -p "$scratch/path"
  printf '#!/bin/sh\nexec dotnet "$(cd "$(dirname "$0")/.." && pwd)/bin/powerops.dll" "$@"\n' >"$scratch/path/powerops"
  chmod +x "$scratch/path/powerops"
  if [ -e "$build" ]; then rm -rf "$scratch"; else mv "$scratch" "$build"; fi
fi

# ---- the temp folder of the run: only this case, and the skill ----
work="$(mktemp -d)"
case "$work" in "$repo"/*) echo "error: the temp folder $work is inside the repo" >&2; rmdir "$work"; exit 2 ;; esac
home_dir=""
client_pid=""
out=""
cleanup() {
  local sign_in keep=0 swap
  if [ -n "$client_pid" ] && kill -0 "$client_pid" 2>/dev/null; then stop_tree "$client_pid"; fi
  if [ -n "$home_dir" ]; then
    sign_in="$home_dir/.codex/auth.json"
    # A client that refreshed its token replaced the link with a file: put that file where the person's sign-in file is, atomically.
    if [ ! -L "$sign_in" ] && [ -f "$sign_in" ]; then
      swap="$(dirname "$real_auth")/.auth.json.$$.tmp"
      if (umask 077; cp "$sign_in" "$swap" && chmod 600 "$swap" && mv -f "$swap" "$real_auth") 2>/dev/null; then
        rm -f "$sign_in"
      else
        rm -f "$swap"
        keep=1
        echo "error: the refreshed Codex sign-in could not be moved back; it stays in $home_dir/.codex" >&2
      fi
    fi
    [ "$keep" -eq 1 ] || rm -rf "$home_dir"
  fi
  rm -rf "$work"
}

# pid and every process below it
descend() {
  local p="$1" c
  echo "$p"
  for c in $(ps -A -o pid= -o ppid= | awk -v p="$p" '$2 == p { print $1 }'); do descend "$c"; done
}

stop_tree() {
  local pids
  pids="$(descend "$1")"
  # shellcheck disable=SC2086
  kill -TERM $pids 2>/dev/null || true
  sleep 5
  # shellcheck disable=SC2086
  kill -KILL $pids 2>/dev/null || true
}

trap cleanup EXIT
trap 'exit 130' INT TERM

cp -R "$case_dir/." "$work/"
if [ "$client" = claude ]; then skills_dir="$work/.claude/skills/$skill_name"; else skills_dir="$work/.agents/skills/$skill_name"; fi
mkdir -p "$skills_dir"
cp "$skill_file" "$skills_dir/SKILL.md"

case "$pass" in
  review-plan) prompt="Use the powerops-review-plan skill to review the plan $plan_name." ;;
  security-solution) prompt="Use the powerops-review-security skill. Plan: $plan_name. Current solution: solution/current. Previous solution: solution/previous. Settings file: settings.json." ;;
  verify) prompt="Use the powerops-verify skill. Plan: $plan_name. Current solution: solution/current. Previous solution: solution/previous. Settings file: settings.json. Test folder: flowtests. There is no live evidence." ;;
esac

# ---- where the results go ----
stamp="$(date -u +%Y%m%dT%H%M%SZ)"
out="$out_root/$client-$pass-$case_name-$stamp"
mkdir -p "$out_root"
n=1
while ! mkdir "$out" 2>/dev/null; do
  n=$((n + 1))
  [ "$n" -le 50 ] || { echo "error: cannot create a results folder in $out_root" >&2; exit 3; }
  out="$out_root/$client-$pass-$case_name-$stamp-$n"
done
{
  echo "head: $head_sha"
  echo "client: $client"
  echo "client-version: $client_version"
  echo "model: $EVAL_MODEL"
  echo "effort: $EVAL_EFFORT"
  echo "pass: $pass"
  echo "case: $case_name"
  echo "limit-seconds: $limit"
  echo "prompt: $prompt"
} >"$out/meta.txt"

# ---- the client, in a new session, from inside the temp folder, with stdin from /dev/null ----
export PATH="$build/path:$PATH"
if [ "$client" = claude ]; then
  # Nothing inherited from a Claude Code session that started this script may outrank the flags.
  allowed=(
    'Bash(powerops analyze *)'
    'Bash(powerops diff *)'
    'Bash(powerops test *)'
    'Bash(powerops settings validate *)'
    'Bash(powerops plan status *)'
  )
  started="$(utc)"
  (
    cd "$work" && exec env -u CLAUDE_EFFORT -u CLAUDE_CODE_EFFORT_LEVEL -u CLAUDE_CODE_SUBAGENT_MODEL -u ANTHROPIC_MODEL \
      claude -p --setting-sources project --strict-mcp-config --tools Bash,Read,Write,Edit,Glob,Grep,Skill \
      --permission-mode acceptEdits --output-format stream-json --verbose --model "$EVAL_MODEL" --effort "$EVAL_EFFORT" \
      --allowedTools "${allowed[@]}" -- "$prompt"
  ) </dev/null >"$out/events.jsonl" 2>"$out/stderr.txt" &
  client_pid=$!
else
  home_dir="$(mktemp -d)"
  mkdir -p "$home_dir/.codex"
  ln -s "$real_auth" "$home_dir/.codex/auth.json"
  started="$(utc)"
  (
    cd "$work" && exec env HOME="$home_dir" CODEX_HOME="$home_dir/.codex" \
      codex exec --ephemeral --skip-git-repo-check -s workspace-write --json -m "$EVAL_MODEL" -c "model_reasoning_effort=$EVAL_EFFORT" "$prompt"
  ) </dev/null >"$out/events.jsonl" 2>"$out/stderr.txt" &
  client_pid=$!
fi
echo "$started" >"$out/started"

# The watchdog writes to nothing the client's output goes through; it ends by itself when the client does.
(
  waited=0
  while kill -0 "$client_pid" 2>/dev/null; do
    if [ "$waited" -ge "$limit" ]; then
      : >"$out/.stopped"
      stop_tree "$client_pid"
      break
    fi
    sleep 1
    waited=$((waited + 1))
  done
) >/dev/null 2>&1 </dev/null &
watchdog=$!

code=0
wait "$client_pid" || code=$?
if [ -f "$out/.stopped" ]; then
  # The watchdog is still putting down what the client left behind; let it finish.
  wait "$watchdog" 2>/dev/null || true
  code=124
  rm -f "$out/.stopped"
else
  kill "$watchdog" 2>/dev/null || true
  wait "$watchdog" 2>/dev/null || true
fi
client_pid=""
echo "$(utc)" >"$out/ended"
echo "$code" >"$out/exit-code"

# ---- what the run left ----
[ -f "$work/docs/plans/$plan_name/$report" ] && cp "$work/docs/plans/$plan_name/$report" "$out/$report"
[ -f "$work/docs/plans/$plan_name/plan.md" ] && cp "$work/docs/plans/$plan_name/plan.md" "$out/plan.md"
diff -rq "$case_dir" "$work" >"$out/case.diff" || true

# A hit means the run may have seen what it should not; it is recorded, and the run is not stopped.
if grep -n -o -F -e "$repo" -e "tests/eval/roles" -e "key.json" -e "baseline/" "$out/events.jsonl" >"$out/flagged.txt" 2>/dev/null; then
  echo "flagged: the event stream names the repo path, tests/eval/roles, key.json or baseline/; see $out/flagged.txt" >&2
else
  rm -f "$out/flagged.txt"
fi

echo "$out"
