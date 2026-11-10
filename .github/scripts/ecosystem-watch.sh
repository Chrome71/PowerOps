#!/usr/bin/env bash
# Usage: ecosystem-watch.sh [--inventory <file>] [--update]
# The ecosystem watch (M84, docs/ecosystem/README.md). Reads the inventory (default docs/ecosystem/sources.json in this clone), asks each
# automated source's public endpoint once and prints a Markdown report: new, not checked (with the reason), unchanged, and the sources checked by
# hand (with the ones due). It is the one validator of the inventory's format and refuses a bad one (exit 2, one error: line) before any request.
# Only a version, tag, sha or commit date that passes a strict ASCII pattern is read from a response, and each is printed inside a code span:
# release names and bodies, commit messages and HTML never reach the report, as pin-drift.sh keeps commit subjects out. The only links are built
# here from validated parts. It writes no file but its temp files unless --update is given; --update then records each value read in lastSeen and
# today (UTC) in lastChecked, through a temp file and mv, and refuses a symbolic link. GITHUB_TOKEN, when set, goes to api.github.com only,
# through a curl config on stdin, so it is never in argv. Exit 0 whenever a report is printed, also with sources not checked; 2 on a usage error.
# Runtime: a request takes at most 3 tries of 30 s plus 3 s of waits, and none starts after 420 s, so a run ends within about 8.6 minutes, under
# the workflow's 10-minute timeout; a source not asked in time is not checked, with that reason.
# Patterns are anchored with \A and \z in jq, because jq's $ also matches before a final newline.
set -euo pipefail

budget=420

die() {
  echo "error: $1" >&2
  exit 2
}

inventory=""
update=0
while [ $# -gt 0 ]; do
  case "$1" in
    --inventory)
      [ $# -ge 2 ] || die "--inventory needs a file"
      inventory="$2"
      shift 2
      ;;
    --update)
      update=1
      shift
      ;;
    *) die "unknown option; usage: ecosystem-watch.sh [--inventory <file>] [--update]" ;;
  esac
done

[ -n "${BASH_VERSION:-}" ] || die "run this script with bash"
for tool in curl jq; do
  command -v "$tool" > /dev/null || die "$tool is not on PATH"
done

if [ -z "$inventory" ]; then
  top="$(git rev-parse --show-toplevel 2> /dev/null)" || die "not in a git clone; pass --inventory <file>"
  inventory="$top/docs/ecosystem/sources.json"
fi
[ -f "$inventory" ] || die "$inventory: no such file"
if [ "$update" = 1 ] && [ -L "$inventory" ]; then
  die "$inventory is a symbolic link; --update writes only a regular file"
fi

# The inventory's format (row 1 of docs/plans/m84-ecosystem-watch/plan.md): prints the first problem, or nothing.
validate='
def m($p): type == "string" and test("\\A(?:" + $p + ")\\z");
def realdate: if m("[0-9]{4}-[0-9]{2}-[0-9]{2}") then (strptime("%Y-%m-%d") | mktime | strftime("%Y-%m-%d")) == . else false end;
def kinds: ["nuget", "npm", "github-release", "github-commits", "manual"];
def areanames: ["pac", "alm", "dataverse", "apps", "automate", "pages", "copilot-studio", "power-bi", "mcp", "agent-skills", "agent-clients", "governance"];
def repo: "[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+";
def targetpatterns: {"nuget": "[a-z0-9][a-z0-9._-]*", "npm": "(@[a-z0-9._-]+/)?[a-z0-9._-]+", "github-release": repo, "github-commits": repo,
  "manual": "https://[A-Za-z0-9._~/?#&=%+-]+"};
def valuepatterns: {"nuget": "[0-9A-Za-z][0-9A-Za-z.+_-]{0,63}", "npm": "[0-9A-Za-z][0-9A-Za-z.+_-]{0,63}",
  "github-release": "[0-9A-Za-z][0-9A-Za-z.+_-]{0,63}", "github-commits": "[0-9a-f]{40}"};
def required($k): ["id", "title", "kind", "target", "areas", "affects", "lastChecked"] + (if $k == "manual" then ["everyDays"] else ["lastSeen"] end);
def allowed($k): required($k) + (if $k == "github-commits" then ["path"] else [] end);
def problem:
  if type != "object" then "is not an object"
  else .kind as $k
    | if (.id | m("[a-z0-9][a-z0-9-]{1,40}")) | not then "has no id, or a bad one"
      elif ($k | IN(kinds[])) | not then "has an unknown kind"
      elif (required($k) - keys) != [] then "lacks " + (required($k) - keys | join(", "))
      elif (keys - allowed($k)) != [] then "has a field its kind does not take: " + (keys - allowed($k) | join(", "))
      elif (.title | m("[A-Za-z0-9 ,.:;()&/+\u0027-]{1,100}")) | not then "has a bad title (letters, digits, spaces and ,.:;()&/+-\u0027 only)"
      elif (.target | m(targetpatterns[$k])) | not then "has a bad target for its kind"
      elif ($k | startswith("github-")) and (.target | split("/") | any(. == "." or . == "..")) then "has a bad target for its kind"
      elif has("path") and ((.path | m("[A-Za-z0-9_./-]+")) and (.path | contains("..") | not) | not) then "has a bad path"
      elif (.areas | type) != "array" or (.areas | length) == 0 or (.areas | all(IN(areanames[])) | not) then "has an unknown area, or none"
      elif (.affects | m("[ -~]+")) | not then "has a bad affects line (one line of printable ASCII)"
      elif .lastChecked != null and (.lastChecked | realdate | not) then "has a bad date in lastChecked"
      elif $k == "manual" and ((.everyDays | type) != "number" or .everyDays != (.everyDays | floor) or .everyDays < 1) then "has a bad everyDays"
      elif $k != "manual" and .lastSeen != null and (.lastSeen | m(valuepatterns[$k]) | not) then "has a lastSeen that does not match its kind"
      else empty end
  end;
if length != 1 then "must hold one JSON value"
else .[0]
  | if type != "object" or keys != ["sources"] or (.sources | type) != "array" then "must be one object holding only a sources array"
    else ([.sources | to_entries[] | .key as $i | .value | problem | "source \($i + 1) " + .] | first)
      // ([.sources[].id] | group_by(.) | map(select(length > 1)) | first | if . then "repeats the id \(.[0])" else empty end)
      // empty
    end
end'

problem="$(jq -rs "$validate" "$inventory" 2> /dev/null)" || die "$inventory: not valid JSON"
[ -z "$problem" ] || die "$inventory: $problem"

# Reads the value from a response body; prints "<value>" or "<sha> <date>", or "!<reason>".
extract='
def m($p): type == "string" and test("\\A(?:" + $p + ")\\z");
def checked($p): if m($p) then . else "!the value read does not match the pattern for \($kind)" end;
def version: "[0-9A-Za-z][0-9A-Za-z.+_-]{0,63}";
if length != 1 then error("one JSON value expected") else .[0] end
| if $kind == "nuget" then .versions[-1] | checked(version)
  elif $kind == "npm" then .latest | checked(version)
  elif $kind == "github-release" then .tag_name | checked(version)
  elif type != "array" then error("a list expected")
  elif length == 0 then "!the commit list is empty, so the path may be renamed or removed"
  else (.[0].commit.committer.date | if m("[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}Z") then " " + . else "" end) as $date
    | .[0].sha | checked("[0-9a-f]{40}") | if startswith("!") then . else . + $date end
  end'

tmp="$(mktemp -d)"
pending=""
trap 'rm -rf "$tmp"; if [ -n "$pending" ]; then rm -f "$pending"; fi' EXIT
: > "$tmp/seen"

# Asks one URL; prints the HTTP status and returns curl's exit code. The token goes in a config on stdin, never in argv, and only to GitHub's API.
fetch() {
  local args=(-sS -L --proto '=https' --proto-redir '=https' --connect-timeout 10 --max-time 30 --retry 2 -o "$2" -w '%{http_code}')
  case "$1" in
    https://api.github.com/*)
      args+=(-H 'Accept: application/vnd.github+json' -H 'X-GitHub-Api-Version: 2022-11-28')
      if [ -n "${GITHUB_TOKEN:-}" ]; then
        printf 'header = "Authorization: Bearer %s"\n' "$GITHUB_TOKEN" | curl "${args[@]}" -K - "$1"
        return
      fi
      ;;
  esac
  curl "${args[@]}" "$1" < /dev/null
}

today="$(date -u +%Y-%m-%d)"
rows="$(jq -r --arg today "$today" '
  def day: strptime("%Y-%m-%d") | mktime;
  .sources[]
  | [.id, .kind, .target, (.path // ""), (.lastSeen // ""), (.lastChecked // ""), (.everyDays // "" | tostring), .title,
     (if .kind != "manual" then "" elif .lastChecked == null then "true" else ((($today | day) - (.lastChecked | day)) / 86400 > .everyDays | tostring) end)]
  | join("\u001f")' "$inventory")"

new=""
failed=""
same=""
manual=""
n_new=0
n_failed=0
n_same=0
n_manual=0
n_due=0
while IFS=$'\x1f' read -r -u 3 id kind target path last checked every title due; do
  [ -n "$id" ] || continue
  if [ "$kind" = manual ]; then
    n_manual=$((n_manual + 1))
    line="- $id: [$title]($target), every $every days, last checked ${checked:-never}"
    if [ "$due" = true ]; then
      line="$line, due"
      n_due=$((n_due + 1))
    fi
    manual="$manual$line"$'\n'
    continue
  fi

  case "$kind" in
    nuget) url="https://api.nuget.org/v3-flatcontainer/$target/index.json" ;;
    npm) url="https://registry.npmjs.org/-/package/$target/dist-tags" ;;
    github-release) url="https://api.github.com/repos/$target/releases/latest" ;;
    *) url="https://api.github.com/repos/$target/commits?per_page=1${path:+&path=$path}" ;;
  esac
  if [ -n "$last" ]; then old="\`$last\`"; else old="none"; fi

  reason=""
  out=""
  if [ "$SECONDS" -ge "$budget" ]; then
    reason="the run's $budget-second budget was spent before this request"
  else
    rm -f "$tmp/body"
    rc=0
    code="$(fetch "$url" "$tmp/body")" || rc=$?
    [[ $code =~ ^[0-9]{3}$ ]] || code="unknown"
    if [ "$rc" != 0 ]; then
      reason="curl failed with exit code $rc"
    elif [ "$code" = 403 ] || [ "$code" = 429 ]; then
      reason="HTTP $code, possibly a rate limit"
    elif [ "$code" != 200 ]; then
      reason="HTTP $code"
    else
      out="$(jq -rs --arg kind "$kind" "$extract" "$tmp/body" 2> /dev/null)" || out="!the body is not JSON of the expected shape"
      case "$out" in "!"*) reason="${out#!}" ;; esac
    fi
  fi

  if [ -n "$reason" ]; then
    if [ -n "$last" ]; then seen="last seen $old"; else seen="never seen"; fi
    failed="$failed- $id ($kind): $seen, not checked: $reason; this is not \"no change\""$'\n'
    n_failed=$((n_failed + 1))
    continue
  fi

  value="${out%% *}"
  date=""
  if [ "$value" != "$out" ]; then date=" (committed \`${out#* }\`)"; fi
  echo "$id $value" >> "$tmp/seen"
  line="- $id ($kind): $old -> \`$value\`$date, observation \`$id@$value\`"
  if [ "$value" = "$last" ]; then
    same="$same$line"$'\n'
    n_same=$((n_same + 1))
    continue
  fi
  case "$kind" in
    github-release) line="$line, [release](https://github.com/$target/releases/tag/$value)" ;;
    github-commits)
      if [ -n "$path" ]; then
        line="$line, [history](https://github.com/$target/commits/$value/$path)"
      elif [ -n "$last" ]; then
        line="$line, [compare](https://github.com/$target/compare/$last...$value)"
      fi
      ;;
  esac
  new="$new$line"$'\n'
  n_new=$((n_new + 1))
done 3<<< "$rows"

section() {
  if [ -n "$2" ]; then printf '\n## %s\n\n%s' "$1" "$2"; fi
}

{
  echo "# Ecosystem watch, $today UTC"
  echo
  echo "New: $n_new. Not checked: $n_failed. Unchanged: $n_same. Due by hand: $n_due of $n_manual."
  section "New" "$new"
  section "Not checked" "$failed"
  section "Unchanged" "$same"
  section "Checked by hand" "$manual"
} > "$tmp/report"

cat "$tmp/report"
if [ -n "${GITHUB_STEP_SUMMARY:-}" ]; then cat "$tmp/report" >> "$GITHUB_STEP_SUMMARY"; fi
if [ -n "${GITHUB_OUTPUT:-}" ]; then printf 'new=%s\nfailed=%s\ndue=%s\n' "$n_new" "$n_failed" "$n_due" >> "$GITHUB_OUTPUT"; fi

if [ "$update" = 1 ]; then
  pending="$(mktemp "$(dirname "$inventory")/.ecosystem-watch.XXXXXX")"
  jq --arg today "$today" --rawfile seen "$tmp/seen" '
    ($seen | split("\n") | map(select(length > 0) | split(" ") | {(.[0]): .[1]}) | add // {}) as $read
    | .sources |= map(if $read[.id] then .lastSeen = $read[.id] | .lastChecked = $today else . end)' "$inventory" > "$pending"
  mv -f "$pending" "$inventory"
  pending=""
fi
