#!/usr/bin/env bash
# Runs `powerops deploy plan` for the action. Inputs come only from the environment:
# PO_DLL, PO_FOLDER, PO_TARGET, PO_COMMENT, GITHUB_OUTPUT, GITHUB_STEP_SUMMARY and, optionally, RUNNER_DEBUG and RUNNER_TEMP.
# When it has written the plan to the job summary it leaves $RUNNER_TEMP/powerops-plan-in-summary, so comment.sh does not write it again.
# It records the exit code in the step output and always exits 0; the last step of the action fails the job.
set -euo pipefail

mark_summary() {
  if [ -n "${RUNNER_TEMP:-}" ]; then : > "$RUNNER_TEMP/powerops-plan-in-summary"; fi
}

# Clear a marker from an earlier use of the action in this job.
if [ -n "${RUNNER_TEMP:-}" ]; then rm -f "$RUNNER_TEMP/powerops-plan-in-summary"; fi

code=0
if [ -z "${PO_FOLDER:-}" ]; then
  echo "::error::deploy-folder is required when target is set"
  code=2
else
  stderr="$(mktemp)"
  trap 'rm -f "$stderr"' EXIT
  dotnet "$PO_DLL" deploy plan "$PO_FOLDER" --target "$PO_TARGET" --save "$PO_FOLDER/powerops-plan.json" --format markdown --out "$PO_COMMENT" --planned-by action 2>"$stderr" || code=$?
  # Text forwarded from pac, az and the Web API can name tenant ids and the environment URL, and the log may be public.
  # PowerOps' own error:, warning: and note: lines are shown too; forwarded text carries a '  | ' prefix and stays hidden.
  if [ "${RUNNER_DEBUG:-}" = "1" ]; then
    cat "$stderr"
  else
    grep -E '^(error|warning|note):' "$stderr" || true
  fi
fi

echo "exit-code=$code" >> "$GITHUB_OUTPUT"

case "$code" in
  0 | 1)
    if [ -f "$PO_COMMENT" ]; then
      cat "$PO_COMMENT" >> "$GITHUB_STEP_SUMMARY"
      mark_summary
    fi
    if [ -f "$PO_FOLDER/powerops-plan.json" ]; then
      echo "plan-file=$PO_FOLDER/powerops-plan.json" >> "$GITHUB_OUTPUT"
    fi
    ;;
  *)
    printf '## PowerOps deploy plan\n\nThe plan could not run (exit code %s). See the job log.\n' "$code" > "$PO_COMMENT"
    cat "$PO_COMMENT" >> "$GITHUB_STEP_SUMMARY"
    mark_summary
    ;;
esac
