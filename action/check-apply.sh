#!/usr/bin/env bash
# Checks the apply of the action before the gate and before anything is imported. Inputs come only from the environment:
# PO_PATH, PO_PLAN_FILE, PO_APPLY, GITHUB_OUTPUT and, when PO_APPLY is `true`, ACTIONS_ID_TOKEN_REQUEST_URL and ACTIONS_ID_TOKEN_REQUEST_TOKEN;
# OS is read too (Windows_NT on a Windows runner, where `\` is a path separator).
# 1. PO_PATH must be the folder of PO_PLAN_FILE, an existing file: the gate analyzes PO_PATH and the apply imports the zips beside the plan file.
#    A `..` segment in either is refused. This holds for a dry run too.
# 2. With PO_APPLY exactly `true` the job must declare a GitHub environment. The job's OIDC token (audience powerops-environment-check) has
#    an `environment` claim only then. The name goes to the step output `environment`. The token and the response are never printed.
# It exits 2 on a refusal, so the job stops; a dry run needs no environment and no token.
set -euo pipefail

p="${PO_PATH:-}"
f="${PO_PLAN_FILE:-}"
if [ "${OS:-}" = "Windows_NT" ]; then
  p="${p//\\//}"
  f="${f//\\//}"
fi

# The absolute, symlink-free folder of $1, or nothing when it is empty or not a folder (a zip, or a missing path).
resolve() { if [ -n "$1" ]; then (cd -- "$1" 2>/dev/null && pwd -P) || true; fi; }

folder="."
if [[ $f == */* ]]; then
  folder="${f%/*}"
  if [ -z "$folder" ]; then folder="/"; fi
fi

here=""
there=""
if [[ "/$p/" != */../* && "/$f/" != */../* && -f "$f" ]]; then
  here="$(resolve "$p")"
  there="$(resolve "$folder")"
fi
if [ -z "$here" ] || [ "$here" != "$there" ]; then
  echo "::error::path must be the folder that holds plan-file, so that the gate analyzes the zips that are imported; nothing was imported"
  exit 2
fi

if [ "${PO_APPLY:-}" != "true" ]; then
  exit 0
fi

if [ -z "${ACTIONS_ID_TOKEN_REQUEST_URL:-}" ] || [ -z "${ACTIONS_ID_TOKEN_REQUEST_TOKEN:-}" ]; then
  echo "::error::apply: 'true' needs permissions: id-token: write, in a job that declares a GitHub environment (jobs.<job_id>.environment); nothing was imported"
  exit 2
fi

# curl's own message is dropped: nothing from the request is printed.
response="$(curl -sS --fail --max-time 30 -H "Authorization: bearer $ACTIONS_ID_TOKEN_REQUEST_TOKEN" "$ACTIONS_ID_TOKEN_REQUEST_URL&audience=powerops-environment-check" 2>/dev/null)" || response=""
if [ -z "$response" ]; then
  echo "::error::apply: 'true' could not get the job's OIDC token to read its environment; nothing was imported"
  exit 2
fi

# The payload is the second part of the JWT, in base64url without padding. Any failure leaves the name empty, which is refused below.
payload="$(printf '%s' "$response" | jq -r '.value // empty' 2>/dev/null | cut -d. -f2 | tr '_-' '/+')" || payload=""
while [ $((${#payload} % 4)) -ne 0 ]; do payload="$payload="; done
# Only a string with none of the characters `deploy apply --github-run` refuses (control, format, separator, private use): a name it
# refuses would fail the apply after the approval.
environment="$(printf '%s' "$payload" | base64 -d 2>/dev/null | jq -r '.environment | if type == "string" and (test("[\\p{C}\\p{Zl}\\p{Zp}]") | not) then . else empty end' 2>/dev/null)" || environment=""
if [ -z "$environment" ] || [[ $environment =~ [[:cntrl:]] ]]; then
  echo "::error::apply: 'true' runs only in a job that declares a GitHub environment (jobs.<job_id>.environment), with required reviewers; nothing was imported"
  exit 2
fi

echo "environment=$environment" >> "$GITHUB_OUTPUT"
