#!/usr/bin/env bash
# Finds or deletes the ephemeral environment of this run (docs/examples/ephemeral-pr.yml). Usage: ephemeral-env.sh <find|delete>
# Not run end to end; see docs/examples/README.md.
#
# Inputs come only from the environment: EPHEMERAL_PREFIX, REPO_ID, PR_NUMBER, RUN_ID, RUN_ATTEMPT, RUNNER_TEMP and
#   find:   GITHUB_OUTPUT, and optionally EPHEMERAL_FIND_TRIES (default 5) and EPHEMERAL_FIND_WAIT (seconds, default 20)
#   delete: optionally ENV_ID, the id that `find` wrote
# and, optionally, RUNNER_DEBUG. pac must be signed in already, with no environment selected.
#
# The environment is the one whose display name is exactly <prefix>-<repository id>-pr<number>-<run id>-<attempt>.
# find:   reads `pac admin list --json` until exactly one environment has that name (two or none after the retries: an error),
#         checks its id and URL, registers the masks, and only then writes `id` and `url` to GITHUB_OUTPUT.
# delete: takes ENV_ID, or looks the name up (none found: nothing to do; a failed lookup or two: an error and nothing is deleted),
#         reads the list again, and deletes only when it holds exactly one environment with that id and exactly that name.
# Every pac call's output stays in a file that is printed only with debug logging on, because it can name other environments.
set -euo pipefail

fail() {
  echo "::error::$*"
  exit 1
}

mode="${1:-}"
if [ "$mode" != find ] && [ "$mode" != delete ]; then
  echo "::error::usage: ephemeral-env.sh <find|delete>"
  exit 2
fi

prefix_re='^[a-z][a-z0-9-]{1,19}$'
number_re='^[0-9]+$'
guid_re='^[0-9a-fA-F]{8}(-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}$'
# A documented Dataverse host (the list of PowerOps drift): https, a host name, no port, user info, path or query.
url_re='^https://[a-z0-9-]+(\.[a-z0-9-]+)*\.(dynamics\.com|crm\.microsoftdynamics\.us|crm\.appsplatform\.us|crm\.dynamics\.cn)/?$'

[[ ${EPHEMERAL_PREFIX:-} =~ $prefix_re ]] || fail "EPHEMERAL_PREFIX must match [a-z][a-z0-9-]{1,19}"
for var in REPO_ID PR_NUMBER RUN_ID RUN_ATTEMPT; do
  [[ ${!var:-} =~ $number_re ]] || fail "$var must be a number"
done
tries="${EPHEMERAL_FIND_TRIES:-5}"
wait_seconds="${EPHEMERAL_FIND_WAIT:-20}"
[[ $tries =~ $number_re && $wait_seconds =~ $number_re ]] || fail "EPHEMERAL_FIND_TRIES and EPHEMERAL_FIND_WAIT must be numbers"

name="$EPHEMERAL_PREFIX-$REPO_ID-pr$PR_NUMBER-$RUN_ID-$RUN_ATTEMPT"
work="$(mktemp -d "${RUNNER_TEMP:?RUNNER_TEMP is not set}/ephemeral.XXXXXX")"
trap 'rm -rf "$work"' EXIT
list="$work/list.json"

# Prints the saved output of the failed call only with debug logging on.
pac_failed() {
  echo "::error::$1; its output is printed only with debug logging on"
  if [ "${RUNNER_DEBUG:-}" = 1 ]; then cat "$work"/*.out "$work"/*.err 2> /dev/null || true; fi
  exit 1
}

list_environments() {
  if ! pac admin list --json > "$work/list.out" 2> "$work/list.err"; then pac_failed "pac admin list failed"; fi
  mv "$work/list.out" "$list"
  jq -e 'type == "array" and all(.[]; type == "object")' "$list" > /dev/null 2>&1 || pac_failed "pac admin list --json did not print a JSON array of environments"
}

count_with_name() {
  jq --arg name "$name" '[.[] | select(.DisplayName == $name)] | length' "$list"
}

# Reads the id and URL of the one environment with the name, and stops unless both have the shape of a Dataverse environment.
read_match() {
  match_id="$(jq -er --arg name "$name" '[.[] | select(.DisplayName == $name)][0].EnvironmentId | select(type == "string" and length > 0)' "$list")" || fail "the environment named $name has no EnvironmentId in the list"
  match_url="$(jq -er --arg name "$name" '[.[] | select(.DisplayName == $name)][0].EnvironmentUrl | select(type == "string" and length > 0)' "$list")" || fail "the environment named $name has no EnvironmentUrl in the list"
  [[ $match_id =~ $guid_re ]] || fail "the id of the environment named $name is not a GUID"
  lower_url="${match_url,,}"
  [[ $lower_url =~ $url_re ]] || fail "the URL of the environment named $name is not an https URL on a documented Dataverse host"
}

# Masking matches exact text only, so every form of the id and URL is registered. Nothing else is printed before this.
mask_id() {
  echo "::add-mask::$1"
  echo "::add-mask::${1,,}"
  echo "::add-mask::${1^^}"
}

mask_url() {
  local host="${1#*://}" h v
  host="${host%%/*}"
  for h in "$host" "${host,,}" "${host^^}"; do
    for v in "$h" "$h/" "https://$h" "https://$h/"; do echo "::add-mask::$v"; done
    echo "::add-mask::${h%%.*}"
  done
}

if [ "$mode" = find ]; then
  : "${GITHUB_OUTPUT:?GITHUB_OUTPUT is not set}"
  count=0
  for ((attempt = 1; attempt <= tries; attempt++)); do
    list_environments
    count="$(count_with_name)"
    if [ "$count" -ge 2 ]; then fail "more than one environment is named $name; nothing was written"; fi
    if [ "$count" -eq 1 ]; then break; fi
    if [ "$attempt" -lt "$tries" ]; then sleep "$wait_seconds"; fi
  done
  if [ "$count" -ne 1 ]; then fail "no environment is named $name after $tries reads of pac admin list"; fi
  read_match
  mask_id "$match_id"
  mask_url "$match_url"
  {
    echo "id=$match_id"
    echo "url=$match_url"
  } >> "$GITHUB_OUTPUT"
  exit 0
fi

# delete
if [ -n "${ENV_ID:-}" ]; then
  [[ $ENV_ID =~ $guid_re ]] || fail "ENV_ID is not a GUID"
  id="$ENV_ID"
  mask_id "$id"
else
  list_environments
  count="$(count_with_name)"
  if [ "$count" -eq 0 ]; then
    echo "No environment is named $name; nothing to delete."
    exit 0
  fi
  if [ "$count" -ge 2 ]; then fail "more than one environment is named $name; nothing was deleted"; fi
  read_match
  id="$match_id"
  mask_id "$id"
  mask_url "$match_url"
fi

# The delete goes by id, so the list is read again just before it: exactly one environment with this id and exactly this name.
list_environments
same="$(jq --arg name "$name" --arg id "${id,,}" '[.[] | select(.DisplayName == $name and ((.EnvironmentId // "" | tostring | ascii_downcase) == $id))] | length' "$list")"
if [ "$same" -ne 1 ] || [ "$(count_with_name)" -ne 1 ]; then
  fail "the environment list no longer holds exactly one environment with the expected id and the name $name; nothing was deleted"
fi

if ! pac admin delete --environment "$id" --async > "$work/delete.out" 2> "$work/delete.err"; then pac_failed "pac admin delete failed for $name"; fi
echo "Deletion of $name requested."
