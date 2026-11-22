#!/usr/bin/env bash
# Usage: release-check.sh <version> <folder>
# The release job's gate, after release-assets.sh. Refuses (::error:: and exit 1) unless <version> is major.minor.patch, <folder> holds
# exactly the five release files (PowerOps.<version>.nupkg, powerops-linux-x64, powerops-win-x64.exe, powerops-sample.zip, SHA256SUMS),
# SHA256SUMS names exactly the other four and checks, the package installs into a new tool folder from <folder> alone, and both the
# installed tool and powerops-linux-x64 print exactly <version> for --version. It writes nothing in <folder> and does not read the tag.
set -euo pipefail

version="${1:-}"
folder="${2:-}"
package="PowerOps.$version.nupkg"
assets=("$package" powerops-linux-x64 powerops-win-x64.exe powerops-sample.zip)

fail() {
  echo "::error::$1"
  exit 1
}

[[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || fail "the release version is not major.minor.patch"
[ -f "$folder/$package" ] || fail "$folder has no $package"
[ "$(cd "$folder" && ls -A | sort)" = "$(printf '%s\n' "${assets[@]}" SHA256SUMS | sort)" ] ||
  fail "$folder must hold exactly ${assets[*]} and SHA256SUMS"
[ "$(cut -d ' ' -f 3- "$folder/SHA256SUMS" | sort)" = "$(printf '%s\n' "${assets[@]}" | sort)" ] ||
  fail "SHA256SUMS does not name exactly ${assets[*]}"
(cd "$folder" && sha256sum --quiet -c SHA256SUMS > /dev/null 2>&1) || fail "SHA256SUMS does not match the files"

tool="$(mktemp -d)"
trap 'rm -rf "$tool"' EXIT
dotnet tool install --tool-path "$tool/bin" --source "$folder" PowerOps --version "$version" > /dev/null || fail "$package does not install"
printed="$("$tool/bin/powerops" --version)" || fail "the packed tool does not run"
[ "$printed" = "$version" ] || fail "the packed tool prints $printed, not $version"
printed="$("$folder/powerops-linux-x64" --version)" || fail "powerops-linux-x64 does not run"
[ "$printed" = "$version" ] || fail "powerops-linux-x64 prints $printed, not $version"
