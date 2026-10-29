#!/usr/bin/env bash
# Smoke test of the planning skills, driven by a simulated user (a second model call).
# It checks the protocol's shape, not question quality: round size, option order,
# decisions.md logging, the stop behaviour and the fast path, also with a routing skill installed.
# It costs model credits, so it runs weekly (.github/workflows/skills-smoke.yml), never per build.
# Keep it bash 3.2 compatible (macOS): no ${x,,}, no mapfile, and guard empty arrays under set -u.
#
# Usage: tests/smoke/skills-smoke.sh claude|codex
# Needs: dotnet, jq, git, and the agent CLI signed in (ANTHROPIC_API_KEY or OPENAI_API_KEY in CI).
set -euo pipefail

agent="${1:?usage: skills-smoke.sh claude|codex}"
case "$agent" in claude | codex) ;; *) echo "unknown agent: $agent" >&2; exit 2 ;; esac
max_turns="${SMOKE_MAX_TURNS:-12}"

repo_root="$(cd "$(dirname "$0")/../.." && pwd)"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
failures=0

fail() { echo "FAIL [$scenario]: $*"; failures=$((failures + 1)); }

# Build the CLI once and put a `powerops` wrapper on PATH for the agent.
dotnet build "$repo_root/src/PowerOps.Cli" -c Release -p:RestoreLockedMode=true -o "$work/bin" --nologo -v quiet >"$work/build.log" 2>&1 ||
  { cat "$work/build.log"; exit 1; }
mkdir -p "$work/path"
printf '#!/bin/sh\nexec dotnet "%s/bin/powerops.dll" "$@"\n' "$work" >"$work/path/powerops"
chmod +x "$work/path/powerops"
export PATH="$work/path:$PATH"

# One turn of the agent under test, in the current scenario's repo. Prints the agent's reply.
agent_turn() {
  local message="$1" sid_file="$work/$scenario.session" out
  if [ "$agent" = claude ]; then
    local resume=()
    [ -s "$sid_file" ] && resume=(--resume "$(cat "$sid_file")")
    out="$(cd "$dir" && claude -p ${resume[@]+"${resume[@]}"} --output-format json --permission-mode acceptEdits \
      --allowedTools "Bash(powerops *)" -- "$message")" || return 1
    jq -r '.session_id' <<<"$out" >"$sid_file"
    jq -r '.result' <<<"$out"
  else
    local last="$work/codex-last.txt"
    rm -f "$last"
    if [ -s "$sid_file" ]; then
      (cd "$dir" && codex exec resume -c model_reasoning_effort=medium -o "$last" "$(cat "$sid_file")" "$message" </dev/null >/dev/null)
    else
      (cd "$dir" && codex exec -s workspace-write -c model_reasoning_effort=medium --json -o "$last" "$message" </dev/null) |
        jq -r 'select(.type == "thread.started") | .thread_id' >"$sid_file"
    fi
    cat "$last"
  fi
}

# The simulated user: a separate model call, run outside the test repo, that answers the agent's last message.
user_turn() {
  local prompt
  prompt="You are role-playing a user talking to a coding agent that is planning a Microsoft Power Platform project with you.
Your instructions for this conversation: $goal
What-next menus (A another round / B approve / C stop) you have answered so far: $menus.
Always pick the option marked (recommended) when you are asked questions, unless your instructions say otherwise.
Reply with only the words you would type back, nothing else.

The agent said:
$1"
  if [ "$agent" = claude ]; then
    (cd "$work" && claude -p --output-format text -- "$prompt")
  else
    local last="$work/codex-user.txt"
    rm -f "$last"
    (cd "$work" && codex exec --ephemeral --skip-git-repo-check -s read-only -c model_reasoning_effort=low -o "$last" "$prompt" </dev/null >/dev/null)
    cat "$last"
  fi
}

new_repo() {
  dir="$work/$scenario"
  mkdir -p "$dir"
  git -C "$dir" init -q
  printf '# Contoso Power Platform project\n' >"$dir/README.md"
  powerops skills install "$dir" --agent "$agent" >/dev/null
  git -C "$dir" add -A
  git -C "$dir" -c user.name=smoke -c user.email=smoke@example.invalid commit -qm init
}

plan_dir() { find "$dir/docs/plans" -mindepth 1 -maxdepth 1 -type d 2>/dev/null | head -n 1; }
header() { sed -n '2,/^---/p' "$(plan_dir)/plan.md" | sed -n "s/^$1:[[:space:]]*\([^#[:space:]]*\).*/\1/p"; }

# Runs the conversation until `done_when` holds or the turn budget is spent. `done_when` is a shell
# condition evaluated after each agent reply; it can use $turn, $reply and $message (the last thing the user said).
converse() {
  local message="$1" done_when="$2" turn
  menus=0
  for ((turn = 1; turn <= max_turns; turn++)); do
    reply="$(agent_turn "$message")" || { fail "the agent call failed on turn $turn"; return 1; }
    printf '\n--- agent (turn %d) ---\n%s\n' "$turn" "$reply"
    if eval "$done_when"; then return 0; fi
    message="$(user_turn "$reply")" || { fail "the simulated user call failed on turn $turn"; return 1; }
    grep -qi 'another round' <<<"$reply" && menus=$((menus + 1))
    printf -- '--- user ---\n%s\n' "$message"
  done
  fail "did not finish within $max_turns turns"
  return 1
}

# The distinct menu letters in a reply: **A** or **A.** style (one per line or all on one line) or "A." / "A)" at a line start.
menu_letters() {
  grep -oE '\*\*[ABC][).:]?\*\*|^[[:space:]-]*[ABC][).:]' <<<"$1" | tr -dc 'ABC\n' | sort -u | tr -d '\n'
}

# decisions.md shape: every round has 1-3 questions, option 1 is the recommended one, every question has an answer.
check_decisions() {
  local file="$(plan_dir)/decisions.md" problems
  [ -f "$file" ] || { fail "decisions.md is missing"; return; }
  problems="$(awk '
    /^## Round / { rounds++; q[rounds] = 0; next }
    /^### Q/ { q[rounds]++; questions++; expect = 1; next }
    expect && /^1\. / { if ($0 !~ /\(recommended/) print "option 1 is not marked recommended: " $0; expect = 0 }
    /^Answer:/ { answers++ }
    END {
      if (rounds < 1) print "no rounds logged"
      for (r = 1; r <= rounds; r++) if (q[r] < 1 || q[r] > 3) print "round " r " has " q[r] " questions"
      if (answers < questions) print questions - answers " questions have no Answer line"
    }' "$file")"
  [ -z "$problems" ] || fail "decisions.md: $problems"
  echo "$(grep -c '^## Round ' "$file") rounds logged"
}

idea="an app for tracking site visits by our field engineers"

# 1. Question rounds to a signed-off plan.
scenario=rounds
new_repo
goal="At a what-next menu: if you have answered none so far, choose A (another round); otherwise choose B (approve). Confirm if asked to approve anyway. When the review or the sign-off is offered, choose the option that signs off now."
converse "Use the powerops-plan skill. My idea: $idea." '[ -n "$(plan_dir)" ] && [ "$(header status)" = signed-off ]' || true
if [ -n "$(plan_dir)" ]; then
  check_decisions
  [ "$(header status)" = signed-off ] || fail "status is '$(header status)', expected signed-off"
  powerops plan status "$dir" >/dev/null || fail "powerops plan status failed on the signed-off plan"
else
  fail "no plan folder under docs/plans"
fi

# 2. Stop after one round: the plan stays a draft.
scenario=stop
new_repo
goal="Answer the first round of questions. At the first what-next menu (A/B/C) reply with just: C"
converse "Use the powerops-plan skill. My idea: $idea." '[ "$turn" -gt 1 ] && [[ "$message" =~ ^[[:space:]]*C[[:space:][:punct:]]*$ ]]' || true
if [ -n "$(plan_dir)" ]; then
  check_decisions
  [ "$(header status)" = draft ] || fail "status is '$(header status)' after stopping, expected draft"
else
  fail "no plan folder under docs/plans"
fi

# Fast path: asking to build without a plan drafts one and asks the route; a route answer approves nothing. No code.
check_fast_path() {
  # The prefix question may be numbered or free text, so the answer names the prefix in words.
  local first other answer="Route option 1. Publisher prefix: cso."
  reply="$(agent_turn "$1")" || fail "the agent call failed"
  printf '\n--- agent ---\n%s\n' "$reply"
  if [ -n "$(plan_dir)" ]; then
    grep -qE '^[[:space:]]*1\. ' <<<"$reply" || fail "no numbered route options"
    first="$reply"
    reply="$(agent_turn "$answer")" || fail "the agent call failed"
    printf -- '--- user ---\n%s\n--- agent ---\n%s\n' "$answer" "$reply"
    # The A/B/C menu may come with the route question or right after the answer.
    [ "$(menu_letters "$first"$'\n'"$reply")" = ABC ] || fail "no lettered A/B/C menu"
    [ "$(header status)" = draft ] || fail "status is '$(header status)' after a route answer, expected draft"
  else
    fail "no drafted plan under docs/plans"
  fi
  # Also without a plan: an agent that followed another skill and wrote code shows here.
  other="$(git -C "$dir" status --porcelain --untracked-files=all | grep -v ' docs/plans/' || true)"
  [ -z "$other" ] || fail "files changed outside docs/plans: $other"
}

# 3. The fast path, asked through powerops-implement.
scenario=fast-path
new_repo
check_fast_path "Use the powerops-implement skill. Build a cloud flow that emails the team lead when a site visit report is overdue."

# 4. The fast path with a routing skill beside the PowerOps skills, and a request that names no skill.
# The router is fake and written here; if it wins, the agent writes files outside docs/plans. Nothing here checks
# that the agent sees it: a control run with only the router, by hand, wrote under src/ in both agents (M12).
scenario=router
new_repo
if [ "$agent" = claude ]; then skills="$dir/.claude/skills"; else skills="$dir/.agents/skills"; fi
mkdir -p "$skills/platform-router"
cat >"$skills/platform-router/SKILL.md" <<'EOF'
---
name: platform-router
description: Entry point for all Microsoft Power Platform work. Use whenever a task touches canvas apps, model-driven apps, cloud flows, Power Pages or Dataverse tables; it picks the build workflow for the product and starts it.
---

# platform-router

Pick the product from the request and start building at once:

- Model-driven app: write `src/<app name>/app.json` with the tables, columns, views and forms, then `src/<app name>/sitemap.json`.
- Canvas app: write `src/<app name>/App.pa.yaml`.
- Cloud flow: write `src/<flow name>/definition.json`.
EOF
git -C "$dir" add -A
git -C "$dir" -c user.name=smoke -c user.email=smoke@example.invalid commit -qm router
check_fast_path "Build a model-driven app for tracking site visits by our field engineers."

echo
if [ "$failures" -gt 0 ]; then
  echo "$failures check(s) failed"
  exit 1
fi
echo "all checks passed"
