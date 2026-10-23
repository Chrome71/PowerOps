#!/usr/bin/env bash
# Deletes the leftovers of docs/examples/ephemeral-pr.yml (docs/examples/ephemeral-sweep.yml). Not run end to end; see docs/examples/README.md.
#
# Inputs come only from the environment: EPHEMERAL_PREFIX, REPO_ID, GH_REPO (owner/name), GH_TOKEN (read by gh), RUNNER_TEMP and,
# optionally, POWEROPS_SWEEP_DELETE, POWEROPS_SWEEP_PR and RUNNER_DEBUG. pac must be signed in already, with no environment selected.
#
# From one `pac admin list --json` it keeps only the environments whose display name fully matches
# <prefix>-<repository id>-pr<number>-<run id>-<attempt>; every other name, another repository's included, is never touched or printed.
# For each, `gh run view <run id>` says whether the creating run has finished: a finished run should have deleted its environment, so
# this one is a leftover (whatever the state of its pull request). A run still going keeps its environment. A gh failure keeps it too and
# turns the run red.
# It deletes only when POWEROPS_SWEEP_DELETE is exactly `true`; otherwise it prints what it would delete (a dry run).
# With POWEROPS_SWEEP_PR set it handles only that pull request's environments.
# It prints the names of the matching environments and counts, never the list. It exits 1 when a matching environment could not be
# classified or deleted.
set -euo pipefail

fail() {
  echo "::error::$*"
  exit 1
}

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

# Prints the saved output of a failed call only with debug logging on, because it can name other environments.
show_debug() {
  if [ "${RUNNER_DEBUG:-}" = 1 ]; then cat "$@" 2> /dev/null || true; fi
}

if ! pac admin list --json > "$list" 2> "$work/list.err"; then
  echo "::error::pac admin list failed; its output is printed only with debug logging on"
  show_debug "$work/list.err"
  exit 1
fi
if ! jq -e 'type == "array" and all(.[]; type == "object")' "$list" > /dev/null 2>&1; then
  echo "::error::pac admin list --json did not print a JSON array of environments"
  exit 1
fi

name_re="^$EPHEMERAL_PREFIX-$REPO_ID-pr([0-9]+)-([0-9]+)-([0-9]+)$"
# jq selects with \A and \z, which, unlike $, do not allow a trailing newline.
mapfile -t rows < <(jq -r --arg re "\\A$EPHEMERAL_PREFIX-$REPO_ID-pr([0-9]+)-([0-9]+)-([0-9]+)\\z" \
  '.[] | select((.DisplayName | type) == "string" and (.DisplayName | test($re))) | [.DisplayName, (.EnvironmentId // "" | tostring)] | @tsv' "$list")

if [ "$delete" = true ]; then echo "sweep: deleting the leftovers"; else echo "sweep: dry run, nothing is removed (POWEROPS_SWEEP_DELETE is not true)"; fi

matched=0 leftovers=0 kept=0 failed=0
for row in "${rows[@]}"; do
  environment_name="${row%%$'\t'*}"
  environment_id="${row#*$'\t'}"
  [[ $environment_name =~ $name_re ]] || continue
  pr="${BASH_REMATCH[1]}"
  run_id="${BASH_REMATCH[2]}"
  if [ -n "$only_pr" ] && [ "$pr" != "$only_pr" ]; then continue; fi
  matched=$((matched + 1))

  if ! [[ $environment_id =~ $guid_re ]]; then
    echo "failed $environment_name: its id is missing or is not a GUID"
    failed=$((failed + 1))
    continue
  fi

  if ! gh run view "$run_id" --repo "$GH_REPO" --json status,conclusion > "$work/run.out" 2> "$work/run.err"; then
    echo "kept $environment_name: could not read its run"
    show_debug "$work/run.err"
    failed=$((failed + 1))
    continue
  fi
  status="$(jq -r 'if type == "object" and (.status | type) == "string" then .status else "" end' "$work/run.out" 2> /dev/null || true)"
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
  elif pac admin delete --environment "$environment_id" --async > "$work/delete.out" 2> "$work/delete.err"; then
    echo "deleted $environment_name"
    leftovers=$((leftovers + 1))
  else
    echo "failed $environment_name: pac admin delete failed"
    show_debug "$work/delete.out" "$work/delete.err"
    failed=$((failed + 1))
  fi
done

echo "sweep: matched $matched, leftovers $leftovers, kept $kept, failed $failed"
if [ "$failed" -gt 0 ]; then fail "$failed matching environment(s) could not be classified or deleted; see the lines above"; fi
