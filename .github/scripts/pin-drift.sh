#!/usr/bin/env bash
# Usage: pin-drift.sh   (run in a clone with its full history)
# Finds the PowerOps pins the docs tell readers to copy: the README's `git -C PowerOps checkout <sha>` line and every `Chrome71/PowerOps@<sha>`
# in README.md, docs/GITHUB-ACTION.md and docs/examples/*.yml. Warns (a ::warning:: line and a job-summary line) when the pins differ, when a pin is not
# an ancestor of HEAD, or when files the Action builds from changed after a pin. It is a warning job, not a check: it exits 0 unless the call is broken.
# bash 3.2 (macOS) has no mapfile, associative arrays or ${var,,}; none are used.
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"
paths="src action action.yml skills global.json Directory.Build.props"

warn() {
  echo "::warning::$1"
  echo "- $1" >> "${GITHUB_STEP_SUMMARY:-/dev/null}"
}

files=""
for f in README.md docs/GITHUB-ACTION.md docs/examples/*.yml; do
  if [ -f "$f" ]; then files="$files $f"; fi
done

# shellcheck disable=SC2086  # $files is a list of paths without spaces
pins="$( { grep -hEo 'Chrome71/PowerOps@[0-9a-f]{40}|git -C [^ ]*PowerOps[^ ]* checkout [0-9a-f]{40}' $files || true; } | grep -Eo '[0-9a-f]{40}$' | sort -u || true)"

if [ -z "$pins" ]; then
  warn "pin-drift: no PowerOps pin found in README.md, docs/GITHUB-ACTION.md or docs/examples/*.yml; the pattern this script looks for may no longer match"
  exit 0
fi

if [ "$(echo "$pins" | wc -l | tr -d ' ')" -gt 1 ]; then
  warn "The PowerOps pins in the docs name different commits: $(echo "$pins" | cut -c1-7 | paste -sd ' ' -). Pin one commit everywhere."
fi

for pin in $pins; do
  short="$(echo "$pin" | cut -c1-7)"
  if ! git cat-file -e "$pin^{commit}" 2> /dev/null || ! git merge-base --is-ancestor "$pin" HEAD; then
    warn "The PowerOps pin $short is not an ancestor of HEAD. Pin a commit of main."
    continue
  fi
  # shellcheck disable=SC2086  # $paths is a list of paths without spaces
  count="$(git rev-list --count "$pin..HEAD" -- $paths)"
  if [ "$count" -gt 0 ]; then
    # shellcheck disable=SC2086
    # Hashes only: a commit subject is untrusted (Markdown for the summary, a literal %0A for the runner), so no subject is printed.
    list="$(git log --format=%h -n 10 "$pin..HEAD" -- $paths | paste -sd ' ' -)"
    warn "The PowerOps pin $short is $count commit(s) behind on the files the Action builds from (src, action, action.yml, skills, global.json, Directory.Build.props), newest first: $list. Pin the newest commit of main."
  fi
done
