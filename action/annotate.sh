#!/usr/bin/env bash
# Turns the JSON of `powerops analyze --format json` on stdin into one workflow annotation per finding on stdout.
# Names and messages come from the solution, so the property and data escapes of the workflow command syntax apply to them:
# % first, then CR and LF, and for a property also : and ,.
set -euo pipefail

jq -r '
  def data: gsub("%"; "%25") | gsub("\r"; "%0D") | gsub("\n"; "%0A");
  def prop: data | gsub(":"; "%3A") | gsub(","; "%2C");
  .solutions[] | .path as $p | .findings[]
  | (if ($p | endswith(".zip")) then $p elif $p == "." then .file else $p + "/" + .file end) as $file
  | "::\(if .severity == "error" then "error" else "warning" end) file=\($file | prop),title=\(.ruleId | prop)::\(.message | data)"' | tr -d '\r'
