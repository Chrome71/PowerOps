#!/usr/bin/env bash
# Runs `powerops analyze` through powerops.sh for the action. The first argument is the --fail-on value; the others (--format, --out)
# come after it. Inputs come from the environment: PO_DLL, PO_PATH, PO_BASELINE (empty means none) and PO_REDACT ('false' adds --show-values).
# powerops' stdout and exit code pass through.
set -euo pipefail

args=(analyze "$PO_PATH" --fail-on "$1")
shift
if [ -n "$PO_BASELINE" ]; then args+=(--baseline "$PO_BASELINE"); fi
if [ "$PO_REDACT" = "false" ]; then args+=(--show-values); fi
exec bash "$(dirname "${BASH_SOURCE[0]}")/powerops.sh" "${args[@]}" "$@"
