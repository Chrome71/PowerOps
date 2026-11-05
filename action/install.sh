#!/usr/bin/env bash
# Installs the powerops the action runs and writes its dll to GITHUB_OUTPUT as dll=<path>. It runs in the action's folder (the step's
# working-directory). Inputs come only from the environment: PO_RELEASE and PO_RELEASE_SHA256 (the release pin in action.yml),
# GITHUB_ACTION_PATH, RUNNER_TEMP and GITHUB_OUTPUT.
# Both empty, as before the first release is pinned: it builds src/PowerOps.Cli in locked mode. The runner unpacks an action pinned by
# commit into a folder named after that commit, without .git, so the SDK can't find the commit; the folder's name is passed instead.
# Both set: it downloads PowerOps.<version>.nupkg from the release PowerOps-v<version> (release-please's tag), refuses it unless its SHA-256 is the
# pinned one, and unpacks its tools/ folder. That needs curl, sha256sum and unzip, which GitHub's Linux images and Git Bash on its Windows images have.
set -euo pipefail

release="${PO_RELEASE:-}"
sha="${PO_RELEASE_SHA256:-}"

fail() {
  echo "::error::$1"
  exit 1
}

if [ -z "$release" ] && [ -z "$sha" ]; then
  folder="${GITHUB_ACTION_PATH//\\//}"
  folder="${folder%/}"
  revision=()
  if [[ "${folder##*/}" =~ ^[0-9a-f]{40}$ ]]; then revision=("-p:SourceRevisionId=${folder##*/}"); fi
  dotnet build src/PowerOps.Cli -c Release -p:RestoreLockedMode=true "${revision[@]}"
  echo "dll=$GITHUB_ACTION_PATH/src/PowerOps.Cli/bin/Release/net10.0/powerops.dll" >> "$GITHUB_OUTPUT"
  exit 0
fi

if [ -z "$release" ] || [ -z "$sha" ]; then fail "action.yml pins half a release: set both PO_RELEASE and PO_RELEASE_SHA256, or neither"; fi
[[ "$release" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || fail "PO_RELEASE in action.yml is not major.minor.patch"
[[ "$sha" =~ ^[0-9a-f]{64}$ ]] || fail "PO_RELEASE_SHA256 in action.yml is not 64 lowercase hex digits"

name="PowerOps.$release.nupkg"
dir="$RUNNER_TEMP/powerops-$release"
mkdir -p "$dir"
# Relative names from here on: sha256sum escapes a line whose file name holds a backslash, as a Windows path does.
cd "$dir"
curl --fail --silent --show-error --location --retry 3 --proto '=https' --proto-redir '=https' --output "$name" \
  "https://github.com/Chrome71/PowerOps/releases/download/PowerOps-v$release/$name" || fail "could not download $name from the release PowerOps-v$release of Chrome71/PowerOps"

actual="$(sha256sum < "$name" | cut -d ' ' -f 1)"
[ "$actual" = "$sha" ] || fail "$name has SHA-256 $actual, but action.yml pins $sha; it was not unpacked or run"

unzip -q -o "$name" 'tools/*' -d package || fail "$name could not be unpacked"
shopt -s nullglob
dlls=(package/tools/*/any/powerops.dll)
[ "${#dlls[@]}" -eq 1 ] || fail "$name must hold exactly one tools/*/any/powerops.dll, and holds ${#dlls[@]}"
echo "dll=$dir/${dlls[0]}" >> "$GITHUB_OUTPUT"
