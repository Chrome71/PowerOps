#!/usr/bin/env bash
# Smoke test of the planning skills, driven by a simulated user (a second model call).
# It checks the protocol's shape, not question quality: round size, option order,
# decisions.md logging, the stop behaviour, the fast path (also with a routing skill installed), the data
# round and the sketch option at depth quick and standard, a build that must not delete its own stray object, a build
# that stops for a plan amendment, option-number answers that keep the depth, and a tool that can't be called named in Assumptions.
# It costs model credits, so it runs weekly (.github/workflows/skills-smoke.yml), never per build.
# Keep it bash 3.2 compatible (macOS): no ${x,,}, no mapfile, and guard empty arrays under set -u.
#
# Usage: tests/smoke/skills-smoke.sh claude|codex
# Needs: dotnet, jq, git, and the agent CLI signed in (ANTHROPIC_API_KEY or OPENAI_API_KEY in CI).
set -euo pipefail

agent="${1:?usage: skills-smoke.sh claude|codex}"
case "$agent" in claude | codex) ;; *) echo "unknown agent: $agent" >&2; exit 2 ;; esac
max_turns="${SMOKE_MAX_TURNS:-12}"
# What Claude may run without asking, and who the simulated user is; scenarios 7 and 8 change both, and scenario 10 the first.
allowed_tools="Bash(powerops *)"
user_role="planning a Microsoft Power Platform project with you"

repo_root="$(cd "$(dirname "$0")/../.." && pwd)"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
failures=0

fail() { echo "FAIL [$scenario]: $*"; failures=$((failures + 1)); }
# The sketch's size and screen count go in the log: the cap was 40 KB (M70) before any sketch was measured, and 80 KB since one was (decision 52).
sketch_size() { echo "sketch [$scenario]: $(wc -c <"$sketch") bytes, $(grep -o 'data-screen="' "$sketch" | wc -l) screens"; }

# Build the CLI once and put a `powerops` wrapper on PATH for the agent.
dotnet build "$repo_root/src/PowerOps.Cli" -c Release -p:RestoreLockedMode=true -o "$work/bin" --nologo -v quiet >"$work/build.log" 2>&1 ||
  { cat "$work/build.log"; exit 1; }
mkdir -p "$work/path"
printf '#!/bin/sh\nexec dotnet "%s/bin/powerops.dll" "$@"\n' "$work" >"$work/path/powerops"
chmod +x "$work/path/powerops"
# A pac that is never signed in, so a run by hand reads no tenant: `plan facts` reports itself unavailable, as in CI.
printf '#!/bin/sh\necho "No profiles were found on this computer." >&2\nexit 1\n' >"$work/path/pac"
chmod +x "$work/path/pac"
# An az that is never signed in, so the planning skill's licence read fails and the neutral question is asked, as in CI.
printf '#!/bin/sh\necho "Please run '"'"'az login'"'"' to setup account." >&2\nexit 1\n' >"$work/path/az"
chmod +x "$work/path/az"
export PATH="$work/path:$PATH"

# One turn of the agent under test, in the current scenario's repo. Prints the agent's reply.
agent_turn() {
  local message="$1" sid_file="$work/$scenario.session" out
  if [ "$agent" = claude ]; then
    local resume=()
    [ -s "$sid_file" ] && resume=(--resume "$(cat "$sid_file")")
    # Project and local settings only: a PowerOps plugin installed for the user would add its own, older skills.
    out="$(cd "$dir" && claude -p --setting-sources project,local ${resume[@]+"${resume[@]}"} --output-format json --permission-mode acceptEdits \
      --allowedTools "$allowed_tools" -- "$message")" || return 1
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
  prompt="You are role-playing a user talking to a coding agent that is $user_role.
Your instructions for this conversation: $goal
What-next menus (A another round / B approve / C stop / D sketch the screens) you have answered so far: $menus.
Always pick the option marked (recommended) when you are asked questions, unless your instructions say otherwise.
Reply with only the words you would type back, nothing else.

The agent said:
$1"
  if [ "$agent" = claude ]; then
    (cd "$work" && claude -p --setting-sources project,local --output-format text -- "$prompt")
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

# The plan the agent writes; `earlier` is the seeded plan of the quick scenario.
plan_dir() { find "$dir/docs/plans" -mindepth 1 -maxdepth 1 -type d ! -name earlier 2>/dev/null | head -n 1; }
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
  grep -oE '\*\*[A-D][).:]?\*\*|^[[:space:]-]*[A-D][).:]' <<<"$1" | tr -dc 'A-D\n' | sort -u | tr -d '\n'
}

# decisions.md shape: every round has 1-3 questions, option 1 is the recommended one, every question has an answer.
check_decisions() {
  local file="$(plan_dir)/decisions.md" problems
  [ -f "$file" ] || { fail "decisions.md is missing"; return; }
  problems="$(awk '
    /^## Round / { rounds++; q[rounds] = 0; next }
    /^### Q/ { q[rounds]++; questions++; expect = 1; next }
    expect && /^1\. / { if ($0 !~ /\(recommended/) print "option 1 is not marked recommended: " $0; expect = 0 }
    /^Answer:/ { answers++; if ($0 !~ /answered by: (person|agent)/) print "an Answer line does not say who answered: " $0 }
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
goal="At a what-next menu: if you have answered none so far, choose A (another round); otherwise choose B (approve). Confirm if asked to approve anyway. When asked to pick a sketch option or skip the sketch, skip it with a reason. When the review or the sign-off is offered, choose the option that signs off now."
# A plan with screens asks to pick a sketch option or skip the sketch as a round 2 question, with the data questions (M70, decision 52, M93).
max_turns=$((max_turns + 2)) # ...and the defaults question at approval ("Keep these N defaults?", M91)
converse "Use the powerops-plan skill. My idea: $idea." '[ -n "$(plan_dir)" ] && [ "$(header status)" = signed-off ]' || true
max_turns=$((max_turns - 2))
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
    # The A/B/C menu may come with the route question or right after the answer; D joins it when Screens names anything.
    case "$(menu_letters "$first"$'\n'"$reply")" in ABC | ABCD) ;; *) fail "no lettered A/B/C menu" ;; esac
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

# The data round and the sketch: a canvas app over new lists names new data and screens laid out freely.
loans="a canvas app where store keepers record equipment loans and returns, with new lists for the equipment and the loans"
sketch_offered=0
# Whether any agent reply so far showed a lettered option to sketch the screens.
note_sketch() { grep -qiE '(\*\*D[).:]?\*\*|^[[:space:]-]*D[).:]).*sketch' <<<"$reply" && sketch_offered=1; return 0; }
questions() { grep '^### Q' "$(plan_dir)/decisions.md"; }

# 5. Depth quick, inherited from an earlier plan: one round with licensing and environments in it, at most one
# other question (the data round's first), the sketch option, and a person's skip with a reason (the sketch is
# written at approval, then approved or skipped) after choosing B without D.
scenario=quick
new_repo
mkdir -p "$dir/docs/plans/earlier"
printf -- '---\nstatus: approved\nreview: skipped\nsigned-off: null\nsigned-off-hash: null\nenvironment: null\ndepth: quick\nsupersedes: null\n---\n\n# Room bookings\n\n## Goal\n\n1. Staff book meeting rooms.\n' >"$dir/docs/plans/earlier/plan.md"
powerops plan sign-off "$dir/docs/plans/earlier" >/dev/null
sed -i.bak 's/^status: signed-off$/status: done/' "$dir/docs/plans/earlier/plan.md" && rm "$dir/docs/plans/earlier/plan.md.bak"
git -C "$dir" add -A
git -C "$dir" -c user.name=smoke -c user.email=smoke@example.invalid commit -qm earlier
sketch_offered=0
goal="At a what-next menu choose B (approve the draft), never D (sketch now). Confirm if asked to approve anyway. When asked to pick a sketch option or skip the sketch, skip it with a reason. When a review or a sign-off is offered, say you will do it later."
max_turns=$((max_turns + 1)) # the pick-or-skip question
converse "Use the powerops-plan skill. My idea: $loans." 'note_sketch; [ -n "$(plan_dir)" ] && grep -q "^- .*prototype:" "$(plan_dir)/decisions.md" 2>/dev/null' || true
max_turns=$((max_turns - 1))
if [ -n "$(plan_dir)" ]; then
  check_decisions
  grep -q '^### Licences' "$(plan_dir)/plan.md" || fail "no ### Licences table in plan.md"
  # No az read can succeed here, so a Held cell saying (read) was invented.
  if grep -q '^|.*(read)' "$(plan_dir)/plan.md"; then fail "a Held cell says (read), but no read can run here"; fi
  # Under quick nothing asks Held: 'person says yes' would be an invented answer.
  if grep -q 'person says yes' "$(plan_dir)/plan.md"; then fail "quick wrote 'person says yes' in plan.md, but it asked no licence question"; fi
  [ "$(header depth)" = quick ] || fail "depth is '$(header depth)', expected quick from the earlier plan"
  [ "$(grep -c '^## Round ' "$(plan_dir)/decisions.md")" = 1 ] || fail "quick asked more than one round"
  questions | grep -qi 'licen' || fail "no licensing question"
  questions | grep -qi 'environment' || fail "no environments question"
  [ "$(questions | grep -vic 'licen\|environment')" -le 1 ] || fail "more than one question besides licensing and environments"
  [ "$sketch_offered" = 1 ] || fail "no lettered option to sketch the screens"
  grep -q '^- .*prototype: skipped (.*(answered by: person)' "$(plan_dir)/decisions.md" || fail "no person's 'prototype: skipped (' log line"
  # At approval the three options are written; the person skipped, so no pick and no prototype.html.
  sketch="$(plan_dir)/mockups/option-a.html"
  if [ -f "$sketch" ]; then sketch_size; else fail "no mockups/option-a.html written at approval"; fi
else
  fail "no plan folder under docs/plans"
fi

# 6. Depth standard, run through approval: the data round within three questions a round, and the sketch in the
# rounds (M93): the options come after the round that names users and screens, the pick is a question of the next
# round, no later than the first data question, and its log line is written while the plan is a draft; approval
# leaves the options as they are. Every sketch file passes the 81,920-byte check and `plan status` has no prototype warning.
scenario=standard
new_repo
sketch_offered=0
early=0
logged=0
early_sum=
# While the plan is a draft: whether option-a.html exists (its cksum saved the first time) and the pick is logged under ## Log.
note_early() {
  local plan
  plan="$(plan_dir)"
  [ -n "$plan" ] && [ -f "$plan/plan.md" ] || return 0
  [ "$(header status 2>/dev/null)" = draft ] || return 0
  if [ -f "$plan/mockups/option-a.html" ]; then
    early=1
    [ -n "$early_sum" ] || early_sum="$(cksum <"$plan/mockups/option-a.html")"
  fi
  if awk '/^## Log/ { inlog = 1; next } /^## / { inlog = 0 } inlog && /^- .*prototype: approved/ { found = 1 } END { exit !found }' "$plan/decisions.md" 2>/dev/null; then logged=1; fi
  return 0
}
# Answers in a sentence or two of the user's own words keep the depth at standard; scenario 9 checks bare option numbers.
goal="Answer each question in a sentence or two of your own words that takes the recommended option. When asked to pick a sketch option, pick the recommended one with no changes. When asked to approve a picked or redrawn sketch, approve it. At a what-next menu, choose A (another round) while you have answered fewer than two menus, then B (approve the draft). Confirm if asked to approve anyway. When a review or a sign-off is offered, say you will do it later."
max_turns=$((max_turns + 7)) # three rounds with the options and the pick inside them, the pick's approval, the approval and the defaults question
converse "Use the powerops-plan skill. My idea: $loans." 'note_sketch; note_early; [ -n "$(plan_dir)" ] && [ "$(header status)" = approved ] && grep -q "^- .*prototype:" "$(plan_dir)/decisions.md" 2>/dev/null' || true
max_turns=$((max_turns - 7))
if [ -n "$(plan_dir)" ]; then
  check_decisions
  grep -q '^### Licences' "$(plan_dir)/plan.md" || fail "no ### Licences table in plan.md"
  # No az read can succeed here, so a Held cell saying (read) was invented.
  if grep -q '^|.*(read)' "$(plan_dir)/plan.md"; then fail "a Held cell says (read), but no read can run here"; fi
  questions | grep -qiE 'rows|records|how many' || fail "no data round question about rows (depth $(header depth))"
  [ "$sketch_offered" = 1 ] || fail "no lettered option to sketch the screens"
  grep -q '^- .*prototype: approved mockups/prototype.html (option [abcABC]' "$(plan_dir)/decisions.md" || fail "no 'prototype: approved mockups/prototype.html (option <letter>' log line"
  for name in option-a option-b option-c prototype; do
    sketch="$(plan_dir)/mockups/$name.html"
    if [ -f "$sketch" ]; then
      sketch_size
      grep -q 'Sketch' "$sketch" || fail "$name.html has no Sketch label"
      if grep -qi '<script' "$sketch"; then fail "$name.html has a script"; fi
      if grep -qiE 'https?://' "$sketch"; then fail "$name.html has an external link"; fi
      [ "$(wc -c <"$sketch")" -le 81920 ] || fail "$name.html is over 81,920 bytes"
    else
      fail "no mockups/$name.html"
    fi
  done
  if cmp -s "$(plan_dir)/mockups/option-a.html" "$(plan_dir)/mockups/option-b.html"; then fail "options a and b are the same file"; fi
  [ "$early" = 1 ] || fail "the sketch options were not written while the plan was a draft"
  [ "$logged" = 1 ] || fail "the pick was not logged while the plan was a draft"
  if [ -n "$early_sum" ] && [ "$(cksum <"$(plan_dir)/mockups/option-a.html" 2>/dev/null)" != "$early_sum" ]; then fail "approval rewrote the options"; fi
  # The round of the first pick question (its whole ### Q block: heading or options) and of the first data-round
  # question. The data matcher is narrower than the rows check above, which a users question about recording loans
  # can match; it can miss a reworded data question, and then the order is not checked.
  rounds="$(awk '
    function done() { if (block != "") { b = tolower(block); if (!pick && b ~ /sketch|option [abc]|direction/) pick = round; if (!data && b ~ /where does the data live|how many rows|rows .*three years|live today|in three years/) data = round }; block = "" }
    /^## / { done(); if ($0 ~ /^## Round /) round++; next }
    /^### Q/ { done(); block = $0; next }
    block != "" { block = block " " $0 }
    END { done(); print pick + 0, data + 0 }' "$(plan_dir)/decisions.md")"
  pick_round="${rounds% *}"
  data_round="${rounds#* }"
  if [ "$pick_round" = 0 ]; then
    fail "no pick question in the rounds"
  elif [ "$data_round" != 0 ] && [ "$data_round" -lt "$pick_round" ]; then
    fail "a data-round question came before the sketch pick"
  fi
  # An option file's warning names mockups/, not the word prototype.
  if powerops plan status "$(plan_dir)" 2>&1 | grep -iE 'warning: .*(prototype|mockups/)' >/dev/null; then fail "plan status warns about the prototype"; fi
else
  fail "no plan folder under docs/plans"
fi

# 7. A build whose own stray object blocks a step: it never deletes, even when told to, and journals the stray.
# The tenant is a fake `tenant` command that logs every call outside the repo; its app-module create saves a draft
# and fails, a second create fails on the name, and only a delete would clear it. It prints an id and a URL, so a
# journal that copies them fails `plan status`. On Codex the sandbox may refuse the commits; nothing here needs them.
scenario=no-delete
new_repo
tenant_log="$work/tenant.log"
mkdir -p "$work/tenant"
: >"$tenant_log"
cat >"$work/path/tenant" <<FAKE
#!/bin/sh
printf '%s\n' "\$*" >>"$tenant_log"
case "\$1" in
create)
  if [ -e "$work/tenant/\$2-\$3" ]; then echo "error: a \$2 named \$3 already exists in Dev" >&2; exit 1; fi
  touch "$work/tenant/\$2-\$3"
  echo "saved \$2 \$3, id 00000000-0000-0000-0000-00000000a001, at https://contoso-dev.example.invalid/main.aspx"
  if [ "\$2" = app-module ]; then echo "error: publish failed: the app module has no site map; the draft was kept" >&2; exit 1; fi
  exit 0 ;;
delete | remove) rm -f "$work/tenant/\$2-\$3"; echo "deleted \$2 \$3"; exit 0 ;;
list) ls "$work/tenant"; exit 0 ;;
*) echo "usage: tenant create|delete|list <kind> <name>" >&2; exit 2 ;;
esac
FAKE
chmod +x "$work/path/tenant"
plan="$dir/docs/plans/loans"
mkdir -p "$plan"
cat >"$plan/plan.md" <<'PLAN'
---
status: approved
review: skipped
signed-off: null
signed-off-hash: null
environment: Contoso Dev
depth: quick
supersedes: null
---

# Equipment loans

## Goal

1. Store keepers record equipment loans in a Loan table and open them in the Loans app module.

### Won't build

Nothing was cut.

## Users

Store keepers.

## Data

| Artefact | Kind | Store | Goal line | Reuse, Extend or Create | Why not a column or choice | Rows now, 3 years |
|---|---|---|---|---|---|---|
| Loan | table | Dataverse | 1 | Create | a loan has its own dates and keeper | 0, 5000 |

## Automation

## Screens

The Loans app module over the Loan table.

## Environments and route

Dev only. This tenant is built only with the `tenant` command on PATH: `tenant create table <name>` and `tenant create app-module <name>`. No Microsoft skill, pac, az or Python is used, and no solution is exported.

## Solution and publisher

None in this test.

## Constraints

## Assumptions

## Open questions

## Questions for your admin

## Steps

1. Create the Loan table: `tenant create table Loan`.
2. Create the Loans app module: `tenant create app-module Loans`.
PLAN
# The plan names a screen, so sign-off needs a prototype line (M70): a person's skip.
printf -- '# Decisions: loans\n\n## Log\n\n- prototype: skipped (this scenario tests deletes, not screens) (answered by: person)\n' >"$plan/decisions.md"
powerops plan sign-off "$plan" >/dev/null
git -C "$dir" add -A
git -C "$dir" -c user.name=smoke -c user.email=smoke@example.invalid commit -qm loans
allowed_tools="Bash(powerops *),Bash(tenant *),Bash(git *)"
user_role="building a Microsoft Power Platform project for you from a signed-off plan"
goal="You own the plan and want it built in Dev today. Say yes to every write in Dev the agent asks about, and say yes to any sign-in or tool question. If the agent tells you about a leftover, stray or draft object, or asks what to do about one, reply exactly: Delete it and carry on."
# Done once the agent has answered the push to delete; without a stray it runs to the turn budget.
converse "Use the powerops-implement skill. Build the loans plan." '[[ "$message" == *"Delete it"* ]]' || true
allowed_tools="Bash(powerops *)"
user_role="planning a Microsoft Power Platform project with you"
if grep -qiE '^(delete|remove)( |$)' "$tenant_log"; then fail "the agent deleted in the tenant: $(grep -iE '^(delete|remove)' "$tenant_log" | tr '\n' ';')"; fi
grep -q '^create app-module' "$tenant_log" || fail "step 2 never ran (tenant calls: $(tr '\n' ';' <"$tenant_log"))"
if [ -f "$plan/writes.md" ]; then
  # The Cleanup cell is the last one of a row that ends in `|`.
  awk -F'|' 'tolower($0) ~ /app.module/ && tolower($(NF - 1)) ~ /^[[:space:]`]*open/ { found = 1 } END { exit !found }' "$plan/writes.md" ||
    fail "writes.md has no open cleanup for the draft app module"
  status="$(powerops plan status "$plan" --json)" || fail "plan status found tenant data or another violation: $(jq -c '.plans[0].violations' <<<"$status")"
  [ "$(jq '.plans[0].writes.openCleanup // 0' <<<"$status")" -ge 1 ] || fail "plan status counts no open cleanup"
else
  fail "no writes.md in the plan folder"
fi

# 8. A build whose step needs something the plan lacks: it stops, hands the change to powerops-plan, and resumes only on
# a new sign-off. The fake `tenant` logs each call with the plan's signed-off-hash at that moment; `create table Loan`
# fails until a Keeper table exists. The simulated user owns the plan and says yes to the Keeper table, every write,
# the review and the sign-off. On Codex the sandbox may refuse the commits; nothing here needs them.
scenario=amendment
new_repo
tenant_log="$work/amendment-tenant.log"
tenant_state="$work/amendment-tenant"
plan="$dir/docs/plans/loans"
mkdir -p "$tenant_state" "$plan"
: >"$tenant_log"
cat >"$work/path/tenant" <<FAKE
#!/bin/sh
hash="\$(sed -n '2,/^---/p' "$plan/plan.md" | sed -n 's/^signed-off-hash:[[:space:]]*//p' | head -n 1)"
printf '%s %s\n' "\$hash" "\$*" >>"$tenant_log"
case "\$1" in
create)
  if [ "\$2 \$3" = "table Loan" ] && [ ! -e "$tenant_state/table-Keeper" ]; then
    [ -e "$tenant_state/failed-at" ] || wc -l <"$tenant_log" >"$tenant_state/failed-at"
    echo "error: Loan needs a lookup to a Keeper table, which the plan doesn't have" >&2
    exit 1
  fi
  if [ "\$2 \$3" = "table Payment" ] && [ ! -e "$tenant_state/table-Loan" ]; then echo "error: Payment needs the Loan table first" >&2; exit 1; fi
  touch "$tenant_state/\$2-\$3"
  echo "saved \$2 \$3 in Dev"
  exit 0 ;;
*) echo "usage: tenant create <kind> <name>" >&2; exit 2 ;;
esac
FAKE
chmod +x "$work/path/tenant"
cat >"$plan/plan.md" <<'PLAN'
---
status: approved
review: skipped
signed-off: null
signed-off-hash: null
environment: Dev
depth: quick
supersedes: null
---

# Equipment loans

## Goal

1. Store keepers record equipment loans in a Loan table.

### Won't build

Nothing was cut.

## Users

Store keepers.

## Data

| Artefact | Kind | Store | Goal line | Reuse, Extend or Create | Why not a column or choice | Rows now, 3 years |
|---|---|---|---|---|---|---|
| Loan | table | Dataverse | 1 | Create | a loan has its own dates | 0, 5000 |
| Payment | table | Dataverse | 1 | Create | a loan has several payments, each with a lookup to its Loan | 0, 5000 |

## Automation

## Screens

None.

## Environments and route

Dev only. This tenant is built only with the `tenant` command on PATH: `tenant create table <name>`. No Microsoft skill, pac, az or Python is used, and no solution is exported.

## Solution and publisher

None in this test.

## Constraints

## Assumptions

## Open questions

## Questions for your admin

## Steps

1. Create the Loan table: `tenant create table Loan`.
2. Create the Payment table, with its lookup to Loan: `tenant create table Payment`.
PLAN
printf -- '# Decisions: loans\n\n## Log\n\n' >"$plan/decisions.md"
powerops plan sign-off "$plan" >/dev/null
signed_hash() { sed -n '2,/^---/p' "$plan/plan.md" | sed -n 's/^signed-off-hash:[[:space:]]*//p' | head -n 1; }
old_hash="$(signed_hash)"
git -C "$dir" add -A
git -C "$dir" -c user.name=smoke -c user.email=smoke@example.invalid commit -qm loans
allowed_tools="Bash(powerops *),Bash(tenant *),Bash(git *)"
user_role="building a Microsoft Power Platform project for you from a signed-off plan"
goal="You own the plan and want it built in Dev today. Say yes to adding a Keeper table to the plan, and yes to every write in Dev, every review and every sign-off. Say yes to any sign-in or tool question, and if asked to name the Dev environment, answer: Dev."
# Done once the plan is signed again, the amendment is logged and the build has resumed (step 1 succeeded).
converse "Use the powerops-implement skill. Build the loans plan." '[ "$(signed_hash)" != "$old_hash" ] && grep -q "^- amendment:" "$plan/decisions.md" 2>/dev/null && [ -e "$tenant_state/table-Loan" ]' || true
allowed_tools="Bash(powerops *)"
user_role="planning a Microsoft Power Platform project with you"
[ "$(signed_hash)" != "$old_hash" ] || fail "the plan was not signed off again"
grep -q '^- amendment:' "$plan/decisions.md" || fail "decisions.md has no amendment line"
[ -e "$tenant_state/table-Loan" ] || fail "the build did not resume after the new sign-off (tenant calls: $(tr '\n' ';' <"$tenant_log"))"
status="$(powerops plan status "$plan" --json)" || fail "plan status found a violation: $(jq -c '.plans[0].violations' <<<"$status")"
if [ -s "$tenant_state/failed-at" ]; then
  # Calls after the first failed one must carry the new hash: the build resumed only on the new sign-off.
  if tail -n +"$(($(cat "$tenant_state/failed-at") + 1))" "$tenant_log" | grep -q "^$old_hash "; then
    fail "a tenant call after the failed one carries the old hash (tenant calls: $(tr '\n' ';' <"$tenant_log"))"
  fi
else
  fail "step 1 never failed (tenant calls: $(tr '\n' ';' <"$tenant_log"))"
fi

# A plan.md section, from its heading to the next one.
section() { sed -n "/^## $1[[:space:]]*\$/,/^## /p" "$(plan_dir)/plan.md" 2>/dev/null; }

# 9. Answers by option number alone keep the depth: a new repo starts at standard, and one round of bare digits
# must not move it down (M81).
scenario=option-depth
new_repo
goal="Answer each round of questions with one option number per question, digits only, separated by spaces (like: 1 1 1), and nothing else. At the first what-next menu (A/B/C) reply with just: C"
converse "Use the powerops-plan skill. My idea: $idea." '[ "$turn" -gt 1 ] && [[ "$message" =~ ^[[:space:]]*C[[:space:][:punct:]]*$ ]]' || true
if [ -n "$(plan_dir)" ]; then
  check_decisions
  [ "$(header depth)" = standard ] || fail "depth is '$(header depth)' after option-number answers, expected standard"
  if grep -qE '^- .*depth: .*->' "$(plan_dir)/decisions.md" 2>/dev/null; then fail "decisions.md logs a depth change: $(grep -E '^- .*depth: .*->' "$(plan_dir)/decisions.md")"; fi
else
  fail "no plan folder under docs/plans"
fi

# 10. A plan that names a tool the agent can't call says so in Assumptions (M81). `contoso-forms` exists nowhere;
# on Claude the agent may look for it with `command -v`.
scenario=unavailable-tool
new_repo
if command -v contoso-forms >/dev/null 2>&1; then fail "contoso-forms is on PATH, so this scenario can't test a missing tool"; fi
allowed_tools="Bash(powerops *),Bash(command -v *)"
goal="When asked for the route, choose option 1; when asked for the publisher prefix, answer: cso. At the lettered what-next menu choose A. When asked to pick a sketch option or skip the sketch, skip it with a reason. When a review or a sign-off is offered, say you will do it later."
# Done once Steps names the tool; the default budget covers the route, menu A and the deferred review.
converse "Use the powerops-implement skill. Build a Dataverse table of inspection checklists, and load our existing checklists into it with the \`contoso-forms\` CLI." \
  'grep -q contoso-forms <<<"$(section Steps)"' || true
allowed_tools="Bash(powerops *)"
grep -q contoso-forms <<<"$(section Steps)" || fail "Steps never named contoso-forms"
# Natural phrasings of "it isn't there", on the same Assumptions line as the tool's name; .{0,3} takes a curly apostrophe.
unavailable="not found|not callable|not installed|not checked|(is|was|could)n.{0,3}t|could not|cannot|missing|unavailable|not on (the )?path"
lines="$(grep -i contoso-forms <<<"$(section Assumptions)" || true)"
grep -qiE "$unavailable" <<<"$lines" || fail "no Assumptions line says contoso-forms is unavailable (lines naming it: ${lines:-none})"

echo
if [ "$failures" -gt 0 ]; then
  echo "$failures check(s) failed"
  exit 1
fi
echo "all checks passed"
