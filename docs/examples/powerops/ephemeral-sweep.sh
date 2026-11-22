#!/usr/bin/env bash
# Deletes the leftovers of docs/examples/ephemeral-pr.yml (docs/examples/ephemeral-sweep.yml). Not run end to end; see docs/examples/README.md.
#
# Inputs come only from the environment: EPHEMERAL_PREFIX, REPO_ID, GH_REPO (owner/name), GH_TOKEN (read by gh), RUNNER_TEMP and,
# optionally, POWEROPS_SWEEP_DELETE and POWEROPS_SWEEP_PR. pac must be signed in already, with no environment selected.
#
# From `pac admin list --json` it keeps only the environments whose display name fully matches
# <prefix>-<repository id>-pr<number>-<run id>-<attempt>; every other name, another repository's included, is never touched or printed.
# For each, `gh run view <run id>` says whether the creating run has finished: a finished run should have deleted its environment, so
# this one is a leftover (whatever the state of its pull request). A run still going keeps its environment. A gh failure keeps it too and
# turns the run red; so does a leftover whose run no longer exists (GitHub deletes runs after a retention period, 90 days by default).
# An environment whose exact Type is not Sandbox or Trial (such as Production, Default, or Developer including pre-provisioned Managed
# Developer Environments) is never deleted: it is kept, counted as failed, and its name is not printed. Widening deletable types
# is a human-only decision.
# It deletes only when POWEROPS_SWEEP_DELETE is exactly `true`; otherwise it prints what it would delete (a dry run).
# Immediately before each destructive delete it re-reads `pac admin list --json` and revalidates unique id, exact name, and exact
# Type (Sandbox or Trial), minimizing the stale-snapshot window (a residual race remains until the service accepts the delete).
# With POWEROPS_SWEEP_PR set it handles only that pull request's environments.
# Raw pac and gh output is kept in temporary files and never printed to workflow logs (even when RUNNER_DEBUG=1), so unrelated tenant
# environment names or ids are never disclosed. It exits 1 when a matching environment could not be classified or deleted.
set -euo pipefail

fail() {
  echo "::error::$*"
  exit 1
}

# bash 3.2 (macOS) has no ${x,,}.
lower() { printf '%s' "$1" | tr '[:upper:]' '[:lower:]'; }

prefix_re='^[a-z][a-z0-9-]{1,19}$'
number_re='^[0-9]+$'
guid_re='^[0-9a-fA-F]{8}(-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}$'
repo_re='^[A-Za-z0-9._-]+/[A-Za-z0-9._-]+$'

[[ ${EPHEMERAL_PREFIX:-} =~ $prefix_re ]] || fail "EPHEMERAL_PREFIX must match [a-z][a-z0-9-]{1,19}"
[[ ${REPO_ID:-} =~ $number_re ]] || fail "REPO_ID must be a number"
[[ ${GH_REPO:-} =~ $repo_re ]] || fail "GH_REPO must be owner/name"
only_pr="${POWEROPS_SWEEP_PR:-}"
if [ -n "$only_pr" ]; then [[ $only_pr =~ $number_re ]] || fail "POWEROPS_SWEEP_PR must be a number"; fi
delete=false
if [ "${POWEROPS_SWEEP_DELETE:-}" = true ]; then delete=true; fi

work="$(mktemp -d "${RUNNER_TEMP:?RUNNER_TEMP is not set}/ephemeral-sweep.XXXXXX")"
trap 'rm -rf "$work"' EXIT
list="$work/list.json"

# Reads a single top-level JSON array of environment objects into $1; rejects concatenated multi-document JSON streams.
read_env_list() {
  local out="$1" err="$2"
  if ! pac admin list --json > "$out" 2> "$err"; then
    return 1
  fi
  if ! jq -e -s 'length == 1 and (.[0] | type == "array") and all(.[0][]; type == "object")' "$out" > /dev/null 2>&1; then
    return 2
  fi
  return 0
}

rc=0
read_env_list "$list" "$work/list.err" || rc=$?
if [ "$rc" -eq 1 ]; then
  echo "::error::pac admin list failed; raw pac output is suppressed to avoid disclosing unrelated tenant environments"
  exit 1
elif [ "$rc" -ne 0 ]; then
  echo "::error::pac admin list --json did not print a single JSON array of environments"
  exit 1
fi

name_re="^$EPHEMERAL_PREFIX-$REPO_ID-pr([0-9]+)-([0-9]+)-([0-9]+)$"
# jq selects with \A and \z, checks exact Type inside jq (so trailing newlines cannot be stripped), and validates unique id count.
rows=()
while IFS= read -r row; do rows+=("$row"); done < <(jq -r -s \
  --arg re "\\A$EPHEMERAL_PREFIX-$REPO_ID-pr([0-9]+)-([0-9]+)-([0-9]+)\\z" \
  --arg guid "\\A[0-9a-fA-F]{8}(-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}\\z" \
  '.[0] as $all
   | $all[]
   | select((.DisplayName | type) == "string" and (.DisplayName | test($re)))
   | (.Type == "Sandbox" or .Type == "Trial") as $type_ok
   | ((.EnvironmentId | type) == "string" and (.EnvironmentId | test($guid))) as $id_ok
   | (if $id_ok then (.EnvironmentId | ascii_downcase) else "" end) as $id_low
   | (if $id_ok then ([$all[] | select((.EnvironmentId | type) == "string" and ((.EnvironmentId | ascii_downcase) == $id_low))] | length) else 0 end) as $id_count
   | (if $id_ok then .EnvironmentId else "" end) as $id_val
   | [.DisplayName, (if $type_ok then "yes" else "no" end), (if $id_ok then "yes" else "no" end), ($id_count | tostring), $id_val]
   | @tsv' "$list")

if [ "$delete" = true ]; then echo "sweep: deleting the leftovers"; else echo "sweep: dry run, nothing is removed (POWEROPS_SWEEP_DELETE is not true)"; fi

matched=0 leftovers=0 kept=0 failed=0
for row in ${rows[@]+"${rows[@]}"}; do
  IFS=$'\t' read -r environment_name type_ok id_ok id_count environment_id <<< "$row"
  [[ $environment_name =~ $name_re ]] || continue
  pr="${BASH_REMATCH[1]}"
  run_id="${BASH_REMATCH[2]}"
  if [ -n "$only_pr" ] && [ "$pr" != "$only_pr" ]; then continue; fi
  matched=$((matched + 1))

  # Check exact Type FIRST (before printing any name or checking id): never widen deletable types without explicit human authorization;
  # Developer (including Managed Developer), Production, Default, and non-string or trailing-newline types are never deleted or named.
  if [ "$type_ok" != yes ]; then
    echo "failed: a matching environment is not a Sandbox or a Trial, so it is kept"
    failed=$((failed + 1))
    continue
  fi

  if [ "$id_ok" != yes ] || ! [[ $environment_id =~ $guid_re ]]; then
    echo "failed: a matching environment has a missing or invalid id, so it is kept"
    failed=$((failed + 1))
    continue
  fi

  if [ "$id_count" != 1 ]; then
    echo "failed: a matching environment id appears more than once in the list, so it is kept"
    failed=$((failed + 1))
    continue
  fi

  if ! gh run view "$run_id" --repo "$GH_REPO" --json status,conclusion > "$work/run.out" 2> "$work/run.err"; then
    echo "kept $environment_name: could not read its run"
    failed=$((failed + 1))
    continue
  fi
  status="$(jq -r -s 'if length == 1 and (.[0] | type == "object") and (.[0].status | type == "string") and (.[0].status | length > 0) then (if .[0].status == "completed" then "completed" else "other" end) else "" end' "$work/run.out" 2> /dev/null || true)"
  if [ -z "$status" ]; then
    echo "kept $environment_name: could not read its run"
    failed=$((failed + 1))
    continue
  fi
  if [ "$status" != completed ]; then
    echo "kept $environment_name: its run has not finished"
    kept=$((kept + 1))
    continue
  fi

  if [ "$delete" != true ]; then
    echo "would delete $environment_name"
    leftovers=$((leftovers + 1))
    continue
  fi

  # Re-read and revalidate immediately before the destructive delete so a renamed or changed environment is never deleted from a stale snapshot.
  recheck="$work/recheck.json"
  if ! read_env_list "$recheck" "$work/recheck.err"; then
    echo "failed: could not revalidate the environment list before deletion, so it is kept"
    failed=$((failed + 1))
    continue
  fi
  id_lower="$(lower "$environment_id")"
  if ! jq -e -s --arg name "$environment_name" --arg id "$id_lower" \
    '.[0] as $all
     | ([$all[] | select(.DisplayName == $name)] | length) == 1
       and ([$all[] | select((.EnvironmentId | type) == "string" and ((.EnvironmentId | ascii_downcase) == $id))] | length) == 1
       and ([$all[] | select(.DisplayName == $name and (.EnvironmentId | type == "string") and ((.EnvironmentId | ascii_downcase) == $id) and (.Type == "Sandbox" or .Type == "Trial"))] | length) == 1' \
    "$recheck" > /dev/null 2>&1; then
    echo "failed: the environment list no longer matches a unique Sandbox or Trial for the expected id and name, so it is kept"
    failed=$((failed + 1))
    continue
  fi

  if pac admin delete --environment "$environment_id" --async > "$work/delete.out" 2> "$work/delete.err"; then
    echo "deleted $environment_name"
    leftovers=$((leftovers + 1))
  else
    echo "failed $environment_name: pac admin delete failed"
    failed=$((failed + 1))
  fi
done

echo "sweep: matched $matched, leftovers $leftovers, kept $kept, failed $failed"
if [ "$failed" -gt 0 ]; then fail "$failed matching environment(s) could not be classified or deleted; see the lines above"; fi
