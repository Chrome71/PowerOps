#!/usr/bin/env bash
# Appends the Markdown on stdin to the job summary. GitHub drops a summary over 1 MiB with an error, so a longer report is cut at a
# line end, with a last line that says how much was cut. Inputs come only from the environment: GITHUB_STEP_SUMMARY.
set -euo pipefail

limit=1048576
report="$(mktemp)"
trap 'rm -f "$report"' EXIT
cat > "$report"

total="$(wc -c < "$report" | tr -d ' ')"
if [ "$total" -le "$limit" ]; then
  cat "$report" >> "$GITHUB_STEP_SUMMARY"
else
  # 512 bytes stay free for the last line; sed drops the line the cut ended in, which may be partial.
  kept="$(head -c $((limit - 512)) "$report" | sed '$d' | tee -a "$GITHUB_STEP_SUMMARY" | wc -c | tr -d ' ')"
  printf '\n_The report was cut to fit the 1 MiB job summary limit: %s of %s bytes are shown._\n' "$kept" "$total" >> "$GITHUB_STEP_SUMMARY"
fi
