#!/usr/bin/env bash
# Usage: release-assets.sh <folder> [<msbuild property>...]
# Run from the repo root after `dotnet pack` has put one PowerOps.<version>.nupkg in <folder>. Adds the release assets beside it:
# powerops-linux-x64 and powerops-win-x64.exe (self-contained single files, not trimmed) and powerops-sample.zip, then writes
# SHA256SUMS over those four files. The properties go to each `dotnet publish` (the release passes the one that keeps the commit out of
# the version). Refuses (::error:: and exit 1, before SHA256SUMS exists) when <folder> holds no package or more than one, or when a
# runtime's restore holds other application packages than the committed lock files. Prints the .NET runtime each binary bundles.
set -euo pipefail

fail() {
  echo "::error::$1"
  exit 1
}

folder="${1:-}"
[ -n "$folder" ] && [ -d "$folder" ] || fail "usage: release-assets.sh <folder> [<msbuild property>...]"
shift
folder="$(cd "$folder" && pwd)"

shopt -s nullglob
packages=("$folder"/PowerOps.*.nupkg)
shopt -u nullglob
[ "${#packages[@]}" -eq 1 ] || fail "$folder must hold exactly one PowerOps.<version>.nupkg"
package="$(basename "${packages[0]}")"

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

for rid in linux-x64 win-x64; do
  # A locked restore for a runtime fails (the lock files have no runtime section, and adding one makes every restore fetch the runtime
  # packs), so each runtime restores into its own lock under obj/, and the check holds it to the committed one before anything builds.
  # The runtime and apphost packs are in no lock; their version is printed below. The single-file analyzer is off because it adds
  # Microsoft.NET.ILLink.Tasks, whose version follows the SDK, to the restore.
  props=(-p:PublishSingleFile=true -p:EnableSingleFileAnalyzer=false "-p:NuGetLockFilePath=obj/$rid.lock.json")
  rm -f src/PowerOps.Cli/obj/"$rid".lock.json src/PowerOps.Core/obj/"$rid".lock.json
  dotnet restore src/PowerOps.Cli -r "$rid" -p:SelfContained=true "${props[@]}" > "$work/$rid.log" || { cat "$work/$rid.log"; fail "the $rid restore failed"; }
  for project in src/PowerOps.Cli src/PowerOps.Core; do
    jq -e --slurpfile committed "$project/packages.lock.json" --arg rid "net10.0/$rid" \
      '.dependencies["net10.0"] == $committed[0].dependencies["net10.0"] and .dependencies[$rid] == {}' "$project/obj/$rid.lock.json" > /dev/null ||
      fail "the $rid restore of $project holds other packages than $project/packages.lock.json"
  done
  dotnet publish src/PowerOps.Cli -c Release -r "$rid" --self-contained --no-restore "${props[@]}" -o "$work/$rid" "$@" >> "$work/$rid.log" ||
    { cat "$work/$rid.log"; fail "the $rid publish failed"; }
  runtime="$(jq -r '.runtimeOptions.includedFrameworks[]? | select(.name == "Microsoft.NETCore.App") | .version' \
    "src/PowerOps.Cli/bin/Release/net10.0/$rid/powerops.runtimeconfig.json")"
  [ -n "$runtime" ] || fail "the $rid build names no Microsoft.NETCore.App version"
  echo "$rid bundles Microsoft.NETCore.App $runtime"
done
cp "$work/linux-x64/powerops" "$folder/powerops-linux-x64"
cp "$work/win-x64/powerops.exe" "$folder/powerops-win-x64.exe"

sample="$work/sample/powerops-sample"
mkdir -p "$sample/solution" "$sample/tests"
cp -R tests/fixtures/powerops-seed/v2/. "$sample/solution/"
cp tests/fixtures/flowtests/seed/*.flowtest.json "$sample/tests/"
cp .github/scripts/sample-README.md "$sample/README.md"
(cd "$work/sample" && zip -qrX "$folder/powerops-sample.zip" powerops-sample)

cd "$folder"
sha256sum "$package" powerops-linux-x64 powerops-win-x64.exe powerops-sample.zip > SHA256SUMS
