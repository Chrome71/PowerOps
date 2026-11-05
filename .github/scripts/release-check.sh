#!/usr/bin/env bash
# Usage: release-check.sh <version> <folder>
# The release job's gate. Refuses (::error:: and exit 1, before SHA256SUMS exists) unless <version> is major.minor.patch, <folder> holds
# PowerOps.<version>.nupkg, that package installs into a new tool folder from <folder> alone, and the installed tool's --version prints
# exactly <version>. Then it writes SHA256SUMS in <folder>. It does not read the release tag.
set -euo pipefail

version="${1:-}"
folder="${2:-}"
package="PowerOps.$version.nupkg"

fail() {
  echo "::error::$1"
  exit 1
}

[[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || fail "the release version is not major.minor.patch"
[ -f "$folder/$package" ] || fail "$folder has no $package"

tool="$(mktemp -d)"
trap 'rm -rf "$tool"' EXIT
dotnet tool install --tool-path "$tool/bin" --source "$folder" PowerOps --version "$version" > /dev/null || fail "$package does not install"
printed="$("$tool/bin/powerops" --version)" || fail "the packed tool does not run"
[ "$printed" = "$version" ] || fail "the packed tool prints $printed, not $version"

cd "$folder"
sha256sum "$package" > SHA256SUMS
