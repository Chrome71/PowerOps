#!/usr/bin/env bash
# Usage:
#   protected-paths.sh
#       In the Protected paths workflow, in its checkout of main: reads pull request PR_NUMBER of GITHUB_REPOSITORY with `gh api`
#       (GH_TOKEN) and checks it at PR_HEAD_SHA, PR_BASE_REF and PR_BASE_SHA, the head and base the event named; PR_OWNER is the login
#       that may apply tier-L. It fetches the base and head commits from origin when the checkout lacks them (the head from
#       refs/pull/PR_NUMBER/head), with the token handed to git by a temporary askpass, and never checks either out.
#   protected-paths.sh --repo <dir> --pr-json <file> --expected-head-sha <sha> --expected-base-ref <ref> --expected-base-sha <sha>
#                      [--events-json <file> --owner <login>]
#       The same check on a saved pull request (and its issue events, as one array or a stream of pages) and a git repository that
#       holds both commits, with no network call. A pull request with the tier-L label fails without both of the last two.
#
# Exits 0 when the pull request may merge, 1 when it may not or when an input cannot be read (fail closed), 2 on a usage error.
# The change is git's diff, with renames, from the one merge base of the base and head commits to the head commit, as GitHub shows a
#   pull request. Commits are immutable, so no cut list, missing patch or later push changes what was read. No merge base fails, and
#   so do two or more (a criss-cross merge), whose diff is ambiguous.
# Rule 1: a change to a protected path, by its name or by the old name of a rename, needs the label tier-L (AGENTS.md, "Change tiers").
#   The label counts only when the pull request has it on both reads and the latest labeled or unlabeled event of tier-L, read after
#   both, is the owner's labeled event; a label someone else applied, or applied again after the owner, fails the check, whatever the files.
# Rule 2: a line the change adds to a file under docs/ or skills/, to README.md, or to a file named docs or skills, may not hold
#   /home/ or file://. A new file, a file renamed in from an unscanned path and a binary file are scanned whole; a submodule there, or
#   a diff whose added lines differ from git's count, cannot be scanned and fails. tier-L lifts neither.
# Every tier also needs the head and base to be the expected ones on both reads of the pull request, and the base to be main.
# Pull request text is untrusted: the output holds only this script's own words, counts, and file names cut to [A-Za-z0-9._/@+-].
set -euo pipefail
export LC_ALL=C
# Git reads each object as committed: no local refs/replace substitutes other bytes for a sha.
export GIT_NO_REPLACE_OBJECTS=1

usage() {
  echo "usage: protected-paths.sh [--repo <dir> --pr-json <file> --expected-head-sha <sha> --expected-base-ref <ref>" \
    "--expected-base-sha <sha> [--events-json <file> --owner <login>]]" >&2
  exit 2
}

fail() {
  echo "::error::protected-paths: $1"
  exit 1
}

repo="" pr_arg="" events_arg="" expected="" base_ref="" base_sha="" owner="" offline=0 head_given=0 base_given=0 owner_given=0
while [ $# -gt 0 ]; do
  [ $# -ge 2 ] || usage
  case "$1" in
    --repo) repo="$2"; offline=1 ;;
    --pr-json) pr_arg="$2"; offline=1 ;;
    --events-json) events_arg="$2"; offline=1 ;;
    --expected-head-sha) expected="$2"; head_given=1 ;;
    --expected-base-ref) base_ref="$2"; base_given=1 ;;
    --expected-base-sha) base_sha="$2"; base_given=1 ;;
    --owner) owner="$2"; owner_given=1 ;;
    *) usage ;;
  esac
  shift 2
done

if [ "$offline" = 1 ]; then
  [ -n "$repo" ] && [ -n "$pr_arg" ] && [ -n "$expected" ] && [ -n "$base_ref" ] && [ -n "$base_sha" ] || usage
else
  [ "$head_given$base_given$owner_given" = 000 ] || usage
  [ -n "${PR_NUMBER:-}" ] || usage
  [[ "$PR_NUMBER" =~ ^[1-9][0-9]*$ ]] || fail "PR_NUMBER is not a pull request number"
  [[ "${GITHUB_REPOSITORY:-}" =~ ^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$ ]] || fail "GITHUB_REPOSITORY is not owner/name"
  [ -n "${GH_TOKEN:-}" ] || fail "GH_TOKEN is not set"
  repo=.
  expected="${PR_HEAD_SHA:-}"
  base_ref="${PR_BASE_REF:-}"
  base_sha="${PR_BASE_SHA:-}"
  owner="${PR_OWNER:-}"
fi

sha='^[0-9a-f]{40}([0-9a-f]{24})?$'
[[ "$expected" =~ $sha ]] || fail "the expected head is not a commit sha"
[[ "$base_sha" =~ $sha ]] || fail "the expected base is not a commit sha"
[ "$base_ref" = main ] || fail "the pull request's base branch in the event is not main"

tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT

if [ "$offline" = 1 ]; then
  [ -f "$pr_arg" ] && [ -r "$pr_arg" ] || fail "cannot read an input file"
  cp "$pr_arg" "$tmp/pr.json"
else
  api="repos/$GITHUB_REPOSITORY/pulls/$PR_NUMBER"
  gh api "$api" > "$tmp/pr.json" || fail "cannot read the pull request"
fi

# A pull request read: no fallback stands in for a missing or wrong-typed field, so false, null or a missing field fails.
check_pr() {
  jq -e 'type == "object" and (.head | type == "object") and (.head.sha | type == "string") and (.base | type == "object")
    and (.base.ref | type == "string") and (.base.sha | type == "string") and (.labels | type == "array")
    and all(.labels[]; type == "object" and (.name | type == "string"))' "$tmp/$1" > /dev/null 2>&1 \
    || fail "the pull request JSON is not a pull request with a head sha, a base ref and sha, and labels"
  [ "$(jq -r '.head.sha' "$tmp/$1")" = "$expected" ] \
    || fail "the pull request head is not $expected, the commit this run is for; a newer push runs the check again"
  # A retarget is an edited event, which runs the check again with the new base.
  [ "$(jq -r '.base.ref' "$tmp/$1")" = main ] || fail "the pull request's base branch is not main"
  [ "$(jq -r '.base.sha' "$tmp/$1")" = "$base_sha" ] \
    || fail "the pull request base is not $base_sha, the base this run is for; a newer event runs the check again"
}
check_pr pr.json

# Git runs with no hooks, no fsmonitor and no submodules, and never checks a commit out. A diff never hides a submodule.
git_safe=(git -C "$repo" -c core.hooksPath=/dev/null -c core.fsmonitor=false -c submodule.recurse=false -c core.quotepath=false
  -c diff.renames=true -c diff.mnemonicPrefix=false -c color.ui=false -c diff.ignoreSubmodules=none)
# g never fetches: a partial clone's missing object fails the read. Only fetch, in the live mode, reaches origin.
g() { GIT_NO_LAZY_FETCH=1 "${git_safe[@]}" "$@"; }
has_commit() { g cat-file -e "$1^{commit}" 2> /dev/null; }

g rev-parse --git-dir > /dev/null 2>&1 || fail "the repository is not a git repository"
if [ "$offline" = 0 ]; then
  # The token reaches git through the environment and this askpass, never an argument or the git config.
  cat > "$tmp/askpass.sh" <<'EOF'
#!/bin/sh
case "$1" in
  Username*) echo x-access-token ;;
  *) printf '%s\n' "$GH_TOKEN" ;;
esac
EOF
  chmod 700 "$tmp/askpass.sh"
  fetch() {
    GIT_ASKPASS="$tmp/askpass.sh" GIT_TERMINAL_PROMPT=0 "${git_safe[@]}" -c credential.helper= \
      fetch --quiet --no-tags --no-recurse-submodules origin "$1" > /dev/null 2>&1 || fail "cannot fetch the $2 commit from origin"
  }
  has_commit "$base_sha" || fetch "$base_sha" base
  has_commit "$expected" || fetch "refs/pull/$PR_NUMBER/head" head
fi
has_commit "$base_sha" || fail "the base commit $base_sha is not in the repository"
has_commit "$expected" || fail "the head commit $expected is not in the repository; a newer push runs the check again"

bases="$(g merge-base --all "$base_sha" "$expected" 2> /dev/null)" || fail "the base and head commits have no merge base"
n_bases=0 merge_base=""
while IFS= read -r line; do
  [[ "$line" =~ $sha ]] || fail "cannot read the merge base of the base and head commits"
  merge_base="$line"
  n_bases=$((n_bases + 1))
done <<< "$bases"
[ "$n_bases" = 1 ] || fail "the base and head commits have $n_bases merge bases, so the pull request's diff is ambiguous"

protected() {
  case "$1" in
    bench | bench/* | .github | .github/* | CODEOWNERS | AGENTS.md | docs/schemas | docs/schemas/* \
      | src/PowerOps.Core/Deploy | src/PowerOps.Core/Deploy/* | src/PowerOps.Core/Output/SafeWrite.cs \
      | skills/powerops-verify | skills/powerops-verify/* | skills/powerops-review-security | skills/powerops-review-security/* \
      | plugins/claude/skills/powerops-verify | plugins/claude/skills/powerops-verify/* \
      | plugins/claude/skills/powerops-review-security | plugins/claude/skills/powerops-review-security/* \
      | plugins/codex/skills/powerops-verify | plugins/codex/skills/powerops-verify/* \
      | plugins/codex/skills/powerops-review-security | plugins/codex/skills/powerops-review-security/* \
      | tests/PowerOps.Cli.Tests/RoleEvalTests.cs | tests/PowerOps.Cli.Tests/PluginManifestTests.cs \
      | docs/examples/ephemeral-* | docs/examples/powerops/ephemeral-*) return 0 ;;
  esac
  return 1
}
scanned() {
  case "$1" in
    docs | docs/* | skills | skills/* | README.md) return 0 ;;
  esac
  return 1
}

unsafe='[^A-Za-z0-9._/@+-]'
record() { printf '%s\t%s\n' "$1" "${2//$unsafe/?}" >> "$tmp/found.txt"; }
: > "$tmp/found.txt"

# A whole blob: NUL bytes split lines, so a binary or NUL-disguised file is read too.
scan_blob() {
  local rc=0
  g cat-file blob "$1" > "$tmp/blob" 2> /dev/null || fail "cannot read a changed file's content"
  tr '\000' '\n' < "$tmp/blob" | awk 'index($0, "/home/") || index($0, "file://") { found = 1 } END { exit found ? 3 : 0 }' || rc=$?
  case "$rc" in
    0) ;;
    3) record leak "$2" ;;
    *) fail "cannot scan a changed file's content" ;;
  esac
}

# The added lines of a text diff of two blobs, which must be exactly as many as git counts.
scan_diff() {
  local stat adds rc=0
  stat="$(g diff --no-ext-diff --no-textconv --no-color --numstat "$1" "$2" 2> /dev/null)" || fail "cannot diff a changed file"
  adds="${stat%%$'\t'*}"
  if [ "$adds" = - ]; then
    scan_blob "$2" "$3"
    return
  fi
  [[ "$adds" =~ ^[0-9]+$ ]] || fail "cannot read git's count of a changed file's added lines"
  g diff --no-ext-diff --no-textconv --no-color -U0 "$1" "$2" > "$tmp/patch" 2> /dev/null || fail "cannot diff a changed file"
  # A line that begins with + once a hunk has started is an added line, +++ too.
  tr '\000' ' ' < "$tmp/patch" | awk -v want="$adds" '
    /^@@ / { in_hunk = 1; next }
    in_hunk && substr($0, 1, 1) == "+" { added++; if (index(substr($0, 2), "/home/") || index(substr($0, 2), "file://")) leak = 1 }
    END { if (added + 0 != want + 0) exit 4; if (leak) exit 3; exit 0 }' || rc=$?
  case "$rc" in
    0) ;;
    3) record leak "$3" ;;
    4) record unscannable "$3" ;;
    *) fail "cannot scan a changed file's diff" ;;
  esac
}

g diff --no-ext-diff --no-textconv --no-color --ignore-submodules=none --no-abbrev --raw -z -M "$merge_base" "$expected" > "$tmp/raw" 2> /dev/null \
  || fail "cannot diff the base and head commits"

# Each record is ":oldmode newmode oldoid newoid status", NUL, its path, NUL, and for a rename or copy its new path, NUL.
header_re='^:([0-7]{6}) ([0-7]{6}) ([0-9a-f]{40}([0-9a-f]{24})?) ([0-9a-f]{40}([0-9a-f]{24})?) ([ACDMRT])[0-9]*$'
zero='^0+$'
count=0
while IFS= read -r -d '' -u 3 header || [ -n "$header" ]; do
  [[ "$header" =~ $header_re ]] || fail "cannot read git's list of changed files"
  oldmode="${BASH_REMATCH[1]}" newmode="${BASH_REMATCH[2]}" oldoid="${BASH_REMATCH[3]}" newoid="${BASH_REMATCH[5]}" letter="${BASH_REMATCH[7]}"
  IFS= read -r -d '' -u 3 oldpath && [ -n "$oldpath" ] || fail "cannot read git's list of changed files"
  newpath="$oldpath"
  case "$letter" in
    R | C) IFS= read -r -d '' -u 3 newpath && [ -n "$newpath" ] || fail "cannot read git's list of changed files" ;;
  esac
  count=$((count + 1))

  ! protected "$oldpath" || record protected "$oldpath"
  ! protected "$newpath" || record protected "$newpath"

  [ "$letter" != D ] && scanned "$newpath" || continue
  if [ "$newmode" = 160000 ]; then
    record unscannable "$newpath" # a submodule: its content is another repository's
  elif [ "$oldoid" = "$newoid" ] && [ "$oldmode" != 160000 ] && scanned "$oldpath"; then
    : # the same blob, already in a scanned file: a rename inside docs/ or skills/, or a mode change
  elif [[ "$oldoid" =~ $zero ]] || [ "$oldmode" = 160000 ] || ! scanned "$oldpath"; then
    scan_blob "$newoid" "$newpath" # new here: added, renamed in from an unscanned path, or a submodule made a file
  else
    scan_diff "$oldoid" "$newoid" "$newpath"
  fi
done 3< "$tmp/raw"

list() { awk -F '\t' -v kind="$1" '$1 == kind { print $2 }' "$tmp/found.txt" | sort -u; }
protected="$(list protected)"
unscannable="$(list unscannable)"
leaks="$(list leak)"

has_tier_l() { jq -e 'any(.labels[]; .name == "tier-L")' "$tmp/$1" > /dev/null 2>&1; }

# The second read: the head, the base and the label must not have moved while the commits were read.
if [ "$offline" = 1 ]; then
  cp "$tmp/pr.json" "$tmp/pr-after.json"
else
  gh api "$api" > "$tmp/pr-after.json" || fail "cannot read the pull request"
fi
check_pr pr-after.json
before=no after=no
! has_tier_l pr.json || before=yes
! has_tier_l pr-after.json || after=yes
[ "$after" = "$before" ] || fail "the tier-L label changed during the run; the label event runs the check again"

# The label counts only when the owner applied it last: anyone with triage access can apply a label. The events are read last, after
# both reads of the pull request, so a removal and a new labeled event by someone else during the run is the latest event here.
labelled=no
if [ "$after" = yes ]; then
  [[ "$owner" =~ ^[A-Za-z0-9-]+$ ]] || fail "the pull request has the tier-L label and no owner login to check who applied it"
  if [ "$offline" = 1 ]; then
    [ -n "$events_arg" ] && [ -f "$events_arg" ] && [ -r "$events_arg" ] \
      || fail "the pull request has the tier-L label and no issue events to check who applied it"
    cp "$events_arg" "$tmp/events-pages.json"
  else
    gh api --paginate "repos/$GITHUB_REPOSITORY/issues/$PR_NUMBER/events?per_page=100" > "$tmp/events-pages.json" \
      || fail "cannot read the pull request's issue events"
  fi
  jq -e -s 'length >= 1 and all(.[]; type == "array") and (add | all(.[]; type == "object" and (.event | type == "string")
      and (if .event == "labeled" or .event == "unlabeled" then (.label | type == "object") and (.label.name | type == "string")
        and (.actor == null or ((.actor | type == "object") and (.actor.login == null or (.actor.login | type == "string"))))
      else true end)))' "$tmp/events-pages.json" > /dev/null 2>&1 \
    || fail "the issue events JSON is not a list of events"
  # The API lists issue events oldest first, and the pages follow on.
  if jq -e -s --arg owner "$owner" '[add[] | select((.event == "labeled" or .event == "unlabeled") and .label.name == "tier-L")] | last
      | . != null and .event == "labeled" and (.actor | type == "object") and (.actor.login | type == "string")
        and ((.actor.login | ascii_downcase) == ($owner | ascii_downcase))' \
    "$tmp/events-pages.json" > /dev/null 2>&1; then
    labelled=yes
  else
    fail "the tier-L label was not applied by the owner, or someone applied it again after the owner; only the owner applies it"
  fi
fi

status=0
if [ -n "$leaks" ]; then
  echo "::error::protected-paths: an added line holds /home/ or file:// (the tier-L label does not lift this) in:"
  printf '%s\n' "$leaks" | awk '{ print "  " $0 }'
  status=1
fi
if [ -n "$unscannable" ]; then
  echo "::error::protected-paths: these files cannot be scanned for /home/ or file:// (a submodule, or added lines git counts" \
    "differently; the tier-L label does not lift this):"
  printf '%s\n' "$unscannable" | awk '{ print "  cannot scan: " $0 }'
  status=1
fi
if [ -n "$protected" ] && [ "$labelled" = no ]; then
  echo "::error::protected-paths: this pull request changes protected paths and has no tier-L label:"
  printf '%s\n' "$protected" | awk '{ print "  protected: " $0 }'
  status=1
fi

n_protected=0
[ -z "$protected" ] || n_protected="$(printf '%s\n' "$protected" | wc -l | tr -d ' ')"
result=pass
[ "$status" = 0 ] || result=fail
echo "protected-paths: $count changed file(s), $n_protected protected path(s), tier-L label: $labelled, result: $result"
exit "$status"
