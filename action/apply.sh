#!/usr/bin/env bash
# Runs `powerops deploy apply` for the action. Inputs come only from the environment:
# PO_DLL, PO_PLAN_FILE, PO_TARGET, PO_APPLY, PO_EVENT, PO_HEAD_REPO, GITHUB_REPOSITORY, GITHUB_OUTPUT, GITHUB_STEP_SUMMARY and, optionally, RUNNER_DEBUG.
# PO_APPLY must be exactly `true` to import; anything else is a dry run.
# Only push, workflow_dispatch, merge_group and a pull_request whose head repository (PO_HEAD_REPO) is this repository (GITHUB_REPOSITORY) may run it.
# A pull request with no head repository (a deleted fork) is refused, as is every other event.
# It records the exit code in the step output and always exits 0; the last step of the action fails the job.
set -euo pipefail

# An event that can run code or text from someone else (a fork, workflow_run, issue_comment, pull_request_target, ...) is refused.
# $1 is the event, $2 the head repository of a pull request, $3 this repository.
allowed() {
  case "$1" in
    push | workflow_dispatch | merge_group) return 0 ;;
    pull_request) [ -n "$2" ] && [ "$2" = "$3" ] ;;
    *) return 1 ;;
  esac
}

code=0
report="$(mktemp)"
stderr="$(mktemp)"
trap 'rm -f "$report" "$stderr"' EXIT

if [ -z "${PO_TARGET:-}" ]; then
  echo "::error::target is required when plan-file is set"
  code=2
elif ! allowed "${PO_EVENT:-}" "${PO_HEAD_REPO:-}" "${GITHUB_REPOSITORY:-}"; then
  echo "::error::plan-file is supported only on push, workflow_dispatch, merge_group and same-repository pull_request events, not on ${PO_EVENT:-an unknown event}, a fork pull request or a pull request whose head repository is missing; nothing was imported"
  code=2
else
  args=(deploy apply "$PO_PLAN_FILE" --target "$PO_TARGET")
  if [ "${PO_APPLY:-}" = "true" ]; then args+=(--apply); fi
  set +e
  dotnet "$PO_DLL" "${args[@]}" 2>"$stderr" | tee "$report"
  code="${PIPESTATUS[0]}"
  set -e
  # Text forwarded from pac can name tenant ids and the environment URL, and the log may be public. Forwarded lines
  # start with two spaces and a bar, so they never pass the error: filter.
  if [ "${RUNNER_DEBUG:-}" = "1" ]; then
    cat "$stderr"
  else
    grep '^error:' "$stderr" || true
  fi
fi

echo "exit-code=$code" >> "$GITHUB_OUTPUT"

if [ -s "$report" ]; then
  # A fence one backtick longer than the longest run in the report (at least three) cannot be closed by the report.
  longest="$( (grep -o '`\+' "$report" || true) | awk '{ if (length($0) > m) m = length($0) } END { print m + 0 }')"
  width=3
  if [ "$longest" -ge 3 ]; then width=$((longest + 1)); fi
  fence="$(printf '%*s' "$width" '' | tr ' ' '`')"
  {
    printf '## PowerOps deploy apply\n\n%s\n' "$fence"
    cat "$report"
    # The fence must start a line.
    if [ -n "$(tail -c 1 "$report")" ]; then printf '\n'; fi
    printf '%s\n' "$fence"
  } >> "$GITHUB_STEP_SUMMARY"
else
  printf '## PowerOps deploy apply\n\nThe apply produced no report (exit code %s). See the job log.\n' "$code" >> "$GITHUB_STEP_SUMMARY"
fi
