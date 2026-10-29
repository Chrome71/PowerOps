#!/usr/bin/env bash
# Appends the Markdown on stdin to the job summary. GitHub drops a summary over 1 MiB with an error, so a longer report is cut at a
# line end, with a last line that says how much was cut. A first line longer than the limit is cut inside the line, at a UTF-8 character
# boundary. Inputs come only from the environment: GITHUB_STEP_SUMMARY.
set -euo pipefail

# Count and cut bytes, whatever the locale (BSD tools reject a cut that ends inside a character).
export LC_ALL=C

limit=1048576
report="$(mktemp)"
cut="$(mktemp)"
trap 'rm -f "$report" "$cut"' EXIT
cat > "$report"

total="$(wc -c < "$report" | tr -d ' ')"
if [ "$total" -le "$limit" ]; then
  cat "$report" >> "$GITHUB_STEP_SUMMARY"
else
  # 512 bytes stay free for the last line.
  size=$((limit - 512))
  head -c "$size" "$report" > "$cut"
  if [ "$(tr -cd '\n' < "$cut" | wc -c | tr -d ' ')" -gt 0 ]; then
    # sed drops the line the cut ended in, which may be partial.
    kept="$(sed '$d' "$cut" | tee -a "$GITHUB_STEP_SUMMARY" | wc -c | tr -d ' ')"
  else
    # One line fills the cut. Drop a character the cut split: the trailing continuation bytes (128-191) and the lead byte (192 and up) before them.
    drop=0
    for b in $(tail -c 4 "$cut" | od -An -v -tu1 | awk '{ for (i = NF; i >= 1; i--) print $i }'); do
      if [ "$b" -ge 128 ]; then drop=$((drop + 1)); fi
      if [ "$b" -lt 128 ] || [ "$b" -ge 192 ]; then break; fi
    done
    kept="$(head -c $((size - drop)) "$cut" | tee -a "$GITHUB_STEP_SUMMARY" | wc -c | tr -d ' ')"
  fi
  printf '\n_The report was cut to fit the 1 MiB job summary limit: %s of %s bytes are shown._\n' "$kept" "$total" >> "$GITHUB_STEP_SUMMARY"
fi
