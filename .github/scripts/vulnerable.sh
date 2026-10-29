#!/usr/bin/env bash
# Usage: vulnerable.sh <vulnerable.json>
# Takes the output of `dotnet list package --vulnerable --include-transitive --format json` and exits 1 (printing the file) when it lists a vulnerable package.
# dotnet list package exits 0 even when it finds one, so jq decides; a jq error (a restore failure has no `projects`) fails too.
set -euo pipefail

jq -e '[.projects[].frameworks[]? | .topLevelPackages[]?, .transitivePackages[]? | select(.vulnerabilities)] == []' "$1" > /dev/null || { cat "$1"; exit 1; }
