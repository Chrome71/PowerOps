#!/usr/bin/env bash
# Usage:
#   protected-paths.sh
#       In the Protected paths workflow: reads pull request PR_NUMBER of GITHUB_REPOSITORY with `gh api` (GH_TOKEN), its files
#       through every page, and, when it has the tier-L label, its issue events through every page. It checks it at PR_HEAD_SHA,
#       PR_BASE_REF and PR_BASE_SHA, the head and base the event named, and takes PR_OWNER as the login that may apply tier-L.
#   protected-paths.sh --pr-json <file> --files-json <file> --expected-head-sha <sha> --expected-base-ref <ref> --expected-base-sha <sha>
#                      [--events-json <file> --owner <login>]
#       The same check on saved API answers (the pull request, its files and its issue events, each as one array or a stream of
#       pages), with no network call. A pull request with the tier-L label fails without both of the last two.
#
# Exits 0 when the pull request may merge, 1 when it may not or when an input cannot be read (fail closed), 2 on a usage error.
# Rule 1: a change to a protected path, by its name or by the old name of a rename, needs the label tier-L (AGENTS.md, "Change tiers").
#   The label counts only when the pull request has it and the latest labeled or unlabeled event of tier-L is the owner's labeled
#   event; a label someone else applied, or applied again after the owner, fails the check, whatever the files.
# Rule 2: an added line of a file under docs/ or skills/, or of README.md, may not hold /home/ or file://, and such a file with added
#   lines but no patch to scan (too large for the API, or a binary file the API counts lines for) fails too. tier-L lifts neither.
# Every tier also needs the live head and base to be the expected ones, the base to be main, and the API's file list to be whole,
#   so a push, a retarget or a cut list after the event cannot pass on a run that read other files.
# Pull request text is untrusted: the output holds only this script's own words, counts, and file names cut to [A-Za-z0-9._/@+-].
set -euo pipefail

usage() {
  echo "usage: protected-paths.sh [--pr-json <file> --files-json <file> --expected-head-sha <sha> --expected-base-ref <ref>" \
    "--expected-base-sha <sha> [--events-json <file> --owner <login>]]" >&2
  exit 2
}

fail() {
  echo "::error::protected-paths: $1"
  exit 1
}

pr_arg="" files_arg="" events_arg="" expected="" base_ref="" base_sha="" owner="" offline=0 head_given=0 base_given=0 owner_given=0
while [ $# -gt 0 ]; do
  [ $# -ge 2 ] || usage
  case "$1" in
    --pr-json) pr_arg="$2"; offline=1 ;;
    --files-json) files_arg="$2"; offline=1 ;;
    --events-json) events_arg="$2"; offline=1 ;;
    --expected-head-sha) expected="$2"; head_given=1 ;;
    --expected-base-ref) base_ref="$2"; base_given=1 ;;
    --expected-base-sha) base_sha="$2"; base_given=1 ;;
    --owner) owner="$2"; owner_given=1 ;;
    *) usage ;;
  esac
  shift 2
done

tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT

if [ "$offline" = 1 ]; then
  [ -n "$pr_arg" ] && [ -n "$files_arg" ] && [ -n "$expected" ] && [ -n "$base_ref" ] && [ -n "$base_sha" ] || usage
  for f in "$pr_arg" "$files_arg"; do
    [ -f "$f" ] && [ -r "$f" ] || fail "cannot read an input file"
  done
  cp "$pr_arg" "$tmp/pr.json"
  cp "$pr_arg" "$tmp/pr-after.json"
  cp "$files_arg" "$tmp/pages.json"
else
  [ "$head_given$base_given$owner_given" = 000 ] || usage
  [ -n "${PR_NUMBER:-}" ] || usage
  [[ "$PR_NUMBER" =~ ^[1-9][0-9]*$ ]] || fail "PR_NUMBER is not a pull request number"
  [[ "${GITHUB_REPOSITORY:-}" =~ ^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$ ]] || fail "GITHUB_REPOSITORY is not owner/name"
  [ -n "${GH_TOKEN:-}" ] || fail "GH_TOKEN is not set"
  expected="${PR_HEAD_SHA:-}"
  base_ref="${PR_BASE_REF:-}"
  base_sha="${PR_BASE_SHA:-}"
  owner="${PR_OWNER:-}"
  api="repos/$GITHUB_REPOSITORY/pulls/$PR_NUMBER"
  # The pull request is read before and after its files, and both heads and bases must be the expected ones.
  gh api "$api" > "$tmp/pr.json" || fail "cannot read the pull request"
  gh api --paginate "$api/files?per_page=100" > "$tmp/pages.json" || fail "cannot read the changed files"
  gh api "$api" > "$tmp/pr-after.json" || fail "cannot read the pull request"
fi

[[ "$expected" =~ ^[0-9a-f]{40}([0-9a-f]{24})?$ ]] || fail "the expected head is not a commit sha"
[[ "$base_sha" =~ ^[0-9a-f]{40}([0-9a-f]{24})?$ ]] || fail "the expected base is not a commit sha"
[ "$base_ref" = main ] || fail "the pull request's base branch in the event is not main"

for f in pr.json pr-after.json; do
  jq -e 'type == "object" and (.head.sha | type == "string") and (.base.ref | type == "string") and (.base.sha | type == "string")
    and ((.labels // []) | type == "array") and all((.labels // [])[]; type == "object" and (.name | type == "string"))' \
    "$tmp/$f" > /dev/null 2>&1 \
    || fail "the pull request JSON is not a pull request with a head sha, a base ref and sha, and labels"
  [ "$(jq -r '.head.sha' "$tmp/$f")" = "$expected" ] \
    || fail "the pull request head is not $expected, the commit this run is for; a newer push runs the check again"
  # A retarget is an edited event, which runs the check again with the new base.
  [ "$(jq -r '.base.ref' "$tmp/$f")" = main ] || fail "the pull request's base branch is not main"
  [ "$(jq -r '.base.sha' "$tmp/$f")" = "$base_sha" ] \
    || fail "the pull request base is not $base_sha, the base this run is for; a newer event runs the check again"
done

jq -e -s 'length >= 1 and all(.[]; type == "array") and (add | all(.[]; type == "object"
    and (.filename | type == "string" and length > 0) and (.status | type == "string")
    and ((.previous_filename // "") | type == "string") and ((.patch // "") | type == "string")
    and ((.additions // 0) | type == "number")))' "$tmp/pages.json" > /dev/null 2>&1 \
  || fail "the changed files JSON is not a list of files"
jq -s 'add' "$tmp/pages.json" > "$tmp/files.json"

count="$(jq 'length' "$tmp/files.json")"
# The files API stops at 3000 files; a list shorter than the pull request's count was cut, so it cannot pass in any tier.
if [ "$offline" = 1 ]; then counted='.changed_files == null or .changed_files == $n'; else counted='.changed_files == $n'; fi
jq -e --argjson n "$count" "$counted" "$tmp/pr-after.json" > /dev/null 2>&1 \
  || fail "the API listed $count changed file(s), not the pull request's count"

classify='
def norm: sub("^(\\./)+"; "");
def safe: gsub("[^A-Za-z0-9._/@+-]"; "?");
def protected: norm as $p | $p == "bench" or ($p | startswith("bench/")) or $p == ".github" or ($p | startswith(".github/"))
  or $p == "CODEOWNERS" or $p == "AGENTS.md" or ($p | startswith("docs/schemas/")) or ($p | startswith("src/PowerOps.Core/Deploy/"))
  or $p == "src/PowerOps.Core/Output/SafeWrite.cs" or ($p | startswith("skills/powerops-verify/"))
  or ($p | startswith("skills/powerops-review-security/")) or $p == "tests/PowerOps.Cli.Tests/RoleEvalTests.cs"
  or ($p | startswith("plugins/claude/skills/powerops-verify/")) or ($p | startswith("plugins/claude/skills/powerops-review-security/"))
  or ($p | startswith("plugins/codex/skills/powerops-verify/")) or ($p | startswith("plugins/codex/skills/powerops-review-security/"))
  or $p == "tests/PowerOps.Cli.Tests/PluginManifestTests.cs"
  or ($p | startswith("docs/examples/ephemeral-")) or ($p | startswith("docs/examples/powerops/ephemeral-"));
def scanned: norm as $p | ($p | startswith("docs/")) or ($p | startswith("skills/")) or $p == "README.md";
# The added lines of a patch. A +++ line counts too once a hunk has started: it is an added line that begins with ++.
def added: [foreach ((.patch // "") | split("\n"))[] as $l (false; . or ($l | startswith("@@"));
  if ($l | startswith("+")) and (. or ($l | startswith("+++") | not)) then $l else empty end)];
.[] | (([.filename, (.previous_filename // empty)] | map(select(protected)) | .[] | "protected\t" + (norm | safe)),
  (select(.filename | scanned) | .filename as $f
    | if added | any(.[]; contains("/home/") or contains("file://")) then "leak\t" + ($f | norm | safe)
      elif .status != "removed" and .patch == null and (.additions // 1) > 0 then "unscannable\t" + ($f | norm | safe)
      else empty end))
'
jq -r "$classify" "$tmp/files.json" > "$tmp/found.txt" || fail "cannot classify the changed files"

list() { awk -F '\t' -v kind="$1" '$1 == kind { print $2 }' "$tmp/found.txt" | sort -u; }
protected="$(list protected)"
unscannable="$(list unscannable)"
leaks="$(list leak)"

# The label counts only when the owner applied it last: anyone with triage access can apply a label.
labelled=no
if jq -e 'any((.labels // [])[]; .name == "tier-L")' "$tmp/pr-after.json" > /dev/null; then
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
      and (if .event == "labeled" or .event == "unlabeled" then (.label | type == "object" and (.name | type == "string"))
        and (.actor == null or (.actor | type == "object" and ((.login // "") | type == "string"))) else true end)))' \
    "$tmp/events-pages.json" > /dev/null 2>&1 \
    || fail "the issue events JSON is not a list of events"
  # The API lists issue events oldest first, and the pages follow on.
  if jq -e -s --arg owner "$owner" '[add[] | select((.event == "labeled" or .event == "unlabeled") and .label.name == "tier-L")] | last
      | . != null and .event == "labeled" and ((.actor.login // "") | ascii_downcase) == ($owner | ascii_downcase)' \
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
  echo "::error::protected-paths: these files have added lines but no patch to scan for /home/ or file:// (the tier-L label does not lift this):"
  printf '%s\n' "$unscannable" | awk '{ print "  no patch to scan: " $0 }'
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
