#!/usr/bin/env bash
# Finds or deletes the ephemeral environment of this run (docs/examples/ephemeral-pr.yml). Usage: ephemeral-env.sh <find|delete>
# Not run end to end; see docs/examples/README.md.
#
# Inputs come only from the environment: EPHEMERAL_PREFIX, REPO_ID, PR_NUMBER, RUN_ID, RUN_ATTEMPT, RUNNER_TEMP and
#   find:   GITHUB_OUTPUT, and optionally EPHEMERAL_FIND_TRIES (default 5) and EPHEMERAL_FIND_WAIT (seconds, default 20)
#   delete: optionally ENV_ID, the id that `find` wrote
# pac must be signed in already, with no environment selected.
#
# The environment is the one whose display name is exactly <prefix>-<repository id>-pr<number>-<run id>-<attempt>.
# find:   reads `pac admin list --json` until exactly one environment has that name (two or more: an error at once; none, a failed
#         call or output that is not one JSON array of objects: tried again, then an error after the retries), checks that its id is
#         unique across the list and is a GUID, its exact Type is Sandbox or Trial, and its URL is on a documented Dataverse host,
#         registers the masks, and only then writes `id` and `url` to GITHUB_OUTPUT.
# delete: takes ENV_ID, or looks the name up (none found: nothing to do; a failed lookup or two: an error and nothing is deleted;
#         the URL is not needed, because an environment still provisioning has none), reads the list again, and deletes only when it
#         holds exactly one environment with that id across the list, exactly one with that name, both on the same row, and its exact
#         Type is Sandbox or Trial (Production, Default, or Developer environments, including pre-provisioned Managed Developer
#         Environments, are kept and the job fails; widening the deletable types is a human-only decision).
# Raw pac output is kept in temporary files and never printed to workflow logs (even when RUNNER_DEBUG=1), so unrelated tenant
# environment names or ids are never disclosed.
set -euo pipefail

fail() {
  echo "::error::$*"
  exit 1
}

# bash 3.2 (macOS) has no ${x,,} or ${x^^}.
lower() { printf '%s' "$1" | tr '[:upper:]' '[:lower:]'; }
upper() { printf '%s' "$1" | tr '[:lower:]' '[:upper:]'; }

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

# Never print raw pac output to workflow logs (even under RUNNER_DEBUG=1), because it can name unrelated tenant environments.
pac_failed() {
  echo "::error::$1; raw pac output is suppressed to avoid disclosing unrelated tenant environments"
  exit 1
}

# Reads the list into $list; requires exactly one top-level JSON array of objects (rejects concatenated multi-document streams).
try_list() {
  if ! pac admin list --json > "$work/list.out" 2> "$work/list.err"; then
    list_error="pac admin list failed"
    return 1
  fi
  mv "$work/list.out" "$list"
  if ! jq -e -s 'length == 1 and (.[0] | type == "array") and all(.[0][]; type == "object")' "$list" > /dev/null 2>&1; then
    list_error="pac admin list --json did not print a single JSON array of environments"
    return 1
  fi
}

list_environments() {
  try_list || pac_failed "$list_error"
}

count_with_name() {
  jq -s --arg name "$name" '[.[0][] | select(.DisplayName == $name)] | length' "$list"
}

# Verifies that $list holds exactly one row matching $name, exactly one row matching $id_lower across the entire list, both on the
# same row, and that its exact Type (compared inside jq so trailing newlines cannot be stripped by command substitution) is Sandbox
# or Trial. Uses generic refusal messages so protected environment names are never disclosed.
verify_target() {
  local id_lower="$1"
  local same same_id
  same="$(jq -s --arg name "$name" --arg id "$id_lower" '[.[0][] | select(.DisplayName == $name and (.EnvironmentId | type == "string") and ((.EnvironmentId | ascii_downcase) == $id))] | length' "$list")" \
    || fail "cannot verify the target environment in the list; nothing was changed"
  same_id="$(jq -s --arg id "$id_lower" '[.[0][] | select((.EnvironmentId | type == "string") and ((.EnvironmentId | ascii_downcase) == $id))] | length' "$list")" \
    || fail "cannot verify the target environment in the list; nothing was changed"
  if [ "$same" != 1 ] || [ "$same_id" != 1 ] || [ "$(count_with_name)" != 1 ]; then
    fail "the environment list does not hold a single unique matching environment for the expected id and name; nothing was changed"
  fi
  # Never widen allowed types without explicit human authorization; Developer (including Managed Developer) is never allowed here.
  if ! jq -e -s --arg name "$name" --arg id "$id_lower" \
    '[.[0][] | select(.DisplayName == $name and (.EnvironmentId | type == "string") and ((.EnvironmentId | ascii_downcase) == $id))]
     | length == 1 and (.[0].Type == "Sandbox" or .[0].Type == "Trial")' "$list" > /dev/null 2>&1; then
    fail "the matching environment is not a Sandbox or a Trial; nothing was changed"
  fi
}

# Reads the id of the one environment with the name, and stops unless it is a GUID. find also checks unique id, exact allowed Type
# (Sandbox or Trial), and a documented Dataverse URL before registering masks or writing GITHUB_OUTPUT; delete checks unique id and
# exact allowed Type on its fresh pre-delete list read, and keeps the URL only when valid to mask it.
read_match() {
  match_id="$(jq -er -s --arg name "$name" '[.[0][] | select(.DisplayName == $name)][0].EnvironmentId | select(type == "string" and length > 0)' "$list")" \
    || fail "the matching environment has no EnvironmentId in the list"
  [[ $match_id =~ $guid_re ]] || fail "the id of the matching environment is not a GUID"
  if [ "$mode" = find ]; then
    verify_target "$(lower "$match_id")"
  fi
  match_url="$(jq -r -s --arg name "$name" '[.[0][] | select(.DisplayName == $name)][0].EnvironmentUrl | if type == "string" then . else "" end' "$list")"
  lower_url="$(lower "$match_url")"
  if [[ $lower_url =~ $url_re ]]; then
    return 0
  fi

  if [ "$mode" = find ]; then fail "the matching environment has no EnvironmentUrl, or it is not an https URL on a documented Dataverse host"; fi
  match_url=""
}

# Masking matches exact text only, so every form of the id and URL is registered. Nothing else is printed before this.
mask_id() {
  echo "::add-mask::$1"
  echo "::add-mask::$(lower "$1")"
  echo "::add-mask::$(upper "$1")"
}

mask_url() {
  local host="${1#*://}" h v
  host="${host%%/*}"
  for h in "$host" "$(lower "$host")" "$(upper "$host")"; do
    for v in "$h" "$h/" "https://$h" "https://$h/"; do echo "::add-mask::$v"; done
    echo "::add-mask::${h%%.*}"
  done
}

if [ "$mode" = find ]; then
  : "${GITHUB_OUTPUT:?GITHUB_OUTPUT is not set}"
  count=0
  for ((attempt = 1; attempt <= tries; attempt++)); do
    if try_list; then
      count="$(count_with_name)"
      if [ "$count" -ge 2 ]; then fail "more than one environment matches the expected name; nothing was written"; fi
      if [ "$count" -eq 1 ]; then break; fi
    else
      count=0
      if [ "$attempt" -ge "$tries" ]; then pac_failed "$list_error"; fi
    fi
    if [ "$attempt" -lt "$tries" ]; then sleep "$wait_seconds"; fi
  done
  if [ "$count" -ne 1 ]; then fail "no environment matched after $tries reads of pac admin list"; fi
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
  if [ "$count" -ge 2 ]; then fail "more than one environment matches the expected name; nothing was deleted"; fi
  read_match
  id="$match_id"
  mask_id "$id"
  if [ -n "$match_url" ]; then mask_url "$match_url"; fi
fi

# The delete goes by id, so the list is read again just before it: exactly one environment with this id across the list,
# exactly one with this name, both on the same row, and exact Type Sandbox or Trial.
list_environments
verify_target "$(lower "$id")"

if ! pac admin delete --environment "$id" --async > "$work/delete.out" 2> "$work/delete.err"; then pac_failed "pac admin delete failed for $name"; fi
echo "Deletion of $name requested."
