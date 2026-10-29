#!/usr/bin/env bash
# Runs `powerops` with the given arguments for the action. Inputs come from the environment (PO_DLL) and the arguments.
# Its stdout and exit code pass through. Only its error: and note: lines of stderr reach the log: a solution can put any text in a
# message, and a log line that starts with :: is a workflow command. powerops keeps each message on its own line.
# An exit code of 3 or more with no error: line (a crash, "You must install .NET") would show nothing, so one error: line says the code.
set -uo pipefail

stderr="$(mktemp)"
trap 'rm -f "$stderr"' EXIT

code=0
dotnet "$PO_DLL" "$@" 2>"$stderr" || code=$?
grep -E '^(error|note):' "$stderr" >&2 || true
if [ "$code" -ge 3 ] && ! grep -q '^error:' "$stderr"; then
  echo "error: powerops exited with code $code" >&2
fi
exit "$code"
