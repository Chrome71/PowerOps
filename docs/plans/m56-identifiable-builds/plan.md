---
status: approved
review: pending
environment: null
depth: quick
supersedes: null
---

# M56: Identifiable builds

## Goal

Make every PowerOps build say which build it is, and ship releases as files a reader can check. This is M56 of the improvement plan written after the live test of 2026-11-04 (requirement R1), part of its P0 block. That plan, its reviews and the person's answers to its decisions are kept outside the repo, because they quote a private test run. The live test found that the installed tool and a rebuilt one report the same version (`Directory.Build.props:11-12` turns the commit off and holds `0.0.0`), so a plan file's `tool` (`tool: 0.0.0`) cannot name the build that made it (CX-7), and the `tool` check that `deploy apply` runs passes between any two commits (the M10 review said the same). It also found that the apply report's tool version is all an auditor has to tell builds apart (C-5, B-4's tool half).

D4 is answered by the person: "GitHub Releases only (Recommended)". The channel is GitHub Releases, with no NuGet. release-please stamps the version, the release workflow attaches the tool package and a checksum, and the Action downloads a pinned, checksummed release.

Done when:

1. A build from a git checkout prints `<version>+<full commit>` for `powerops --version`, and the same text is the `tool` of its plan and apply files and the MCP server's version. A release build prints the plain version, which is its tag without the `v`.
2. The release workflow, on the release that release-please creates, packs at the tag, refuses to go on unless the packed tool's `--version` equals the tag's version, writes `SHA256SUMS`, attests the package's provenance, and attaches `PowerOps.<version>.nupkg` and `SHA256SUMS` to that GitHub Release. Nothing goes to NuGet.
3. `action.yml` holds a release pin (a version and the SHA-256 of its package). With a pin it downloads that release's package, refuses it unless the SHA-256 matches, and runs it. With an empty pin, as now, before the first release, it builds from source in locked mode as before.
4. `pin-drift.yml` warns when the release pin is not the latest release that `.release-please-manifest.json` records.
5. The version files keep changing only through a release: a test holds the props version, the manifest and both `plugin.json` versions equal and stamped by release-please.
6. The docs say how to install a release and check it, what the Action runs, and how a maintainer cuts a release and moves the pin. `DocsDriftTests` stays green.
7. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan are clean; the PR is green on all eight checks.

### Won't build

- A NuGet package, a NuGet push or Trusted Publishing setup (D4). The NuGet steps of `release.yml` are removed.
- A tag, a release or a pre-release made by this milestone, a merge or edit of release PR #4, or any version edit by hand. The first release is the person's step (decisions Q8).
- A workflow that moves the Action's pin by itself after a release (decisions Q5).
- A check, in the Action, of the release's attestation: the pinned SHA-256 binds the bytes (decisions Q4).
- A self-contained or single-file build, or builds per operating system: the package is the framework-dependent tool that `dotnet tool install` takes.
- Draft releases for GitHub's immutable releases, which are off in this repository (decisions Q7).
- A `dirty` mark for a build with uncommitted changes (decisions Q2).

## Users

- A person who installs PowerOps and wants to know, and check, which build they run.
- A reviewer or auditor who reads a plan file or an apply report and wants to know which build made it.
- A workflow author who pins the Action by commit and wants the commit to fix the bytes it runs.
- The maintainer who cuts a release and moves the pin.

## Data

Line numbers are those of `b8ad904`.

| # | Severity | Change | Where | Test |
|---|---|---|---|---|
| 1 | medium | Delete `<IncludeSourceRevisionInInformationalVersion>false</...>`. The SDK's default (`true`) appends `+<SourceRevisionId>` to `InformationalVersion`, and Source Link, in the SDK since .NET 8, sets it to the full commit of a git checkout. `--version`, `AnalyzeCommand.ToolVersion()` (the plan's and apply report's `tool`, SARIF's driver version) and the MCP `serverInfo.version` all read that attribute, so no code changes. The version line (`:12`) is not touched | `Directory.Build.props:11` | `ProgramTests`: a new case, skipped when the repo root has no `.git`: `--version` of the built CLI is `<Version of Directory.Build.props>+<git rev-parse HEAD>`. The existing `DeployCommandTests:450` and `DeployApplyCommandTests:1155` keep `tool` equal to `ToolVersion()` |
| 2 | medium | The release build turns the commit off: `release.yml`'s pack gains `-p:IncludeSourceRevisionInInformationalVersion=false`, so a release prints its plain version | `.github/workflows/release.yml:51` | `WorkflowTests`: the release pack has `-p:RestoreLockedMode=true` and `-p:IncludeSourceRevisionInInformationalVersion=false`. `PackedToolTests` packs with the same property, then asserts that the installed tool's `--version` and the MCP `initialize` result's `serverInfo.version` are exactly the package's version |
| 3 | medium | `release.yml`'s `publish` job: the NuGet login and push go, and so do header steps 1 and 2 (NuGet setup); step 3 (release-please's PR permission) stays. Its permissions become `contents: write` (the upload), `id-token: write` and `attestations: write`. Steps after the pack, in order: `Check the version` installs the package into a tool folder from `artifacts` only (`--source`) and fails unless `artifacts/PowerOps.<v>.nupkg` exists and the tool's `--version` prints `<v>`, where `<v>` is the tag without its `v`; `Checksum` runs `sha256sum PowerOps.<v>.nupkg > SHA256SUMS` in `artifacts`; the existing provenance attestation of the package; `Attach to the release` runs `gh release upload "$TAG" artifacts/PowerOps.<v>.nupkg artifacts/SHA256SUMS` with `GH_TOKEN` from `github.token` and `TAG` from the job's needs, both through `env`, and no `--clobber`, so a rerun cannot replace a published asset. The tag reaches every run text through `env` only | `.github/workflows/release.yml:1-6,33-61` | `WorkflowTests`: `The_release_jobs_have_exactly_the_permissions_they_need` takes the new `publish` set; a new case: the four steps come after the pack in that order, the upload has no `--clobber` and names both files, and no step `uses` or `run`s anything with `nuget` in it. `No_run_text_holds_an_expression` already covers `release.yml` |
| 4 | high | `action.yml`: the `Build PowerOps` step becomes `Install PowerOps` (id `powerops`, `working-directory: ${{ github.action_path }}`), which runs a new `action/install.sh` with two env values that are the pin, `PO_RELEASE` and `PO_RELEASE_SHA256`, each a single-quoted string, both `''` in this change. Every `PO_DLL` (9 steps) becomes `${{ steps.powerops.outputs.dll }}`. The script: both empty, it runs `dotnet build src/PowerOps.Cli -c Release -p:RestoreLockedMode=true` and writes `dll=$GITHUB_ACTION_PATH/src/PowerOps.Cli/bin/Release/net10.0/powerops.dll` (the path the steps used before); exactly one empty, `::error::` and exit 1; both set, it makes `$RUNNER_TEMP/powerops-<version>`, changes into it, downloads `PowerOps.<version>.nupkg` from the release `v<version>` of `Chrome71/PowerOps` with `curl --fail --silent --show-error --location --retry 3 --proto =https --proto-redir =https`, takes its SHA-256 from `sha256sum` on stdin (a file name with `\`, as on Windows, would make `sha256sum` escape its line), and when that is not the pin prints `::error::` with both hashes and exits 1 before anything is unpacked; else it unpacks the package's `tools/` with `unzip -q -o` into `package`, exits 1 when `package/tools/net10.0/any/powerops.dll` is missing, and writes `dll=$RUNNER_TEMP/powerops-<version>/package/tools/net10.0/any/powerops.dll`. A failed download is `::error::` and exit 1. `setup-dotnet` stays first: the package needs the .NET 10 runtime | `action.yml:52-55,63,75,88,111,121,143,157,170,188`, new `action/install.sh` | `ActionYmlTests`: `Install PowerOps` comes right after `setup-dotnet` and before every step with a `PO_DLL`, has id `powerops`, the working directory and the script; every `PO_DLL` is the step's output; the raw text of `action.yml` has exactly one `PO_RELEASE: '<x>'` line and one `PO_RELEASE_SHA256: '<y>'` line, with both empty or `<x>` a `major.minor.patch` and `<y>` 64 lowercase hex. The `PO_DLL` assertion at `ActionYmlTests.cs:76` takes the output. `WorkflowTests.The_build_job_builds_in_locked_mode_and_checks_the_actions_own_build_on_every_system`: ci's `The Action's build, in locked mode` line is a line of `install.sh`. `ActionScriptTests` with stub `curl` and `dotnet` first on `PATH`: no pin builds with that line, calls no `curl` and writes the source `dll`; a pin whose package (a zip the test makes, holding `tools/net10.0/any/powerops.dll`) matches calls `curl` once with the release URL and `--proto =https`, and writes a `dll` that exists and holds the zip's bytes; a wrong SHA-256 exits 1 with both hashes in the `::error::` line, writes no output and leaves no `package` folder; a failing `curl` exits 1 with no output; a half pin exits 1 with no `curl` call; a package without the dll exits 1 with no output |
| 5 | medium | `pin-drift.sh` gains the release pin. When `action.yml` and `.release-please-manifest.json` exist, it reads the pin from `PO_RELEASE: '<digits and dots>'` and the latest release from the manifest's `"."` (digits and dots only, so no other text reaches a warning), and when the latest release is not `0.0.0` and the pin differs, warns: the Action runs release `<pin>` (or "a build from source" when empty), the latest release is `<latest>`, pin it with its SHA-256 (the CONTRIBUTING section of row 6). It still exits 0 | `.github/scripts/pin-drift.sh` | `WorkflowTests` with the existing `PinDrift` repo: manifest `0.0.0` and an empty pin is quiet; `0.1.0` and an empty pin warns with "a build from source" and `0.1.0`; `0.2.0` and pin `0.1.0` warns with both; `0.1.0` and pin `0.1.0` is quiet; the existing cases, which have no `action.yml`, stay as they are |
| 6 | medium | Docs. `README.md`'s Install "By hand": install from a GitHub Release (download `PowerOps.<version>.nupkg` and `SHA256SUMS`, `sha256sum -c` or PowerShell's `Get-FileHash`, then `dotnet tool install --global --source <folder> PowerOps --version <version>`), said to apply from the first release on, with the clone route kept; `:49`: a build from a clone prints the version and its commit, but its package has the version of the last release, so installing over the tool keeps the old build. `docs/GITHUB-ACTION.md:22`: what the Action runs (the pinned release, checked by SHA-256, or a source build while no release is pinned), that the pin moves with a commit of `main`, and that the download needs `curl`, `sha256sum` and `unzip` (GitHub's Linux and Windows images have them). `CONTRIBUTING.md`: a "Releases" section: merging the release PR cuts a release; what the release job checks and attaches; how to check a release (`sha256sum -c`, `gh attestation verify`); and how to move the pin (one PR that sets both values). `docs/USE-WITH-AGENTS.md:26`: `plugin.json` carries the release version, the same for every build between two releases. `docs/BUILD-PLAN.md:622`: the runbook's "`--version` prints `0.0.0`" becomes the version and the commit. No page names a release tag (`DocsDriftTests.No_page_names_a_powerops_release_or_an_action_tag`) | `README.md:39-55`, `docs/GITHUB-ACTION.md:22`, `CONTRIBUTING.md:35`, `docs/USE-WITH-AGENTS.md:26`, `docs/BUILD-PLAN.md:622` | `DocsDriftTests` (flags, pins, no release named, the quick start), `InstallLineTests` |
| 7 | low | Version files: a test that `Directory.Build.props`'s `Version` line carries the `x-release-please-version` marker, that `release-please-config.json` lists `Directory.Build.props` as a `generic` extra file, and that `.release-please-manifest.json`'s `"."` equals the props version. `PluginManifestTests.Plugin_version_is_the_release_version` already ties both `plugin.json` files to the props version and to release-please | `release-please-config.json`, `.release-please-manifest.json`, `Directory.Build.props:12` (read only) | `BuildPropsTests`: the new case |

## Automation

None. No flow, plug-in or job in Dataverse.

## Screens

None.

## Environments and route

No tenant. Every test runs offline: the release download against a stub `curl` and a zip the test makes, the release workflow by reading its YAML. No tag, release or pre-release is created, and nothing is published.

The first release is the person's step, after this PR is merged (decisions Q8):

1. Merge release PR #4, which release-please keeps up to date. release-please tags `v` plus the version, creates the GitHub Release, and the `publish` job attaches the package and `SHA256SUMS`.
2. Check it: download both files, `sha256sum -c SHA256SUMS`, `gh attestation verify` on the package.
3. Move the pin: one PR that sets `PO_RELEASE` and `PO_RELEASE_SHA256` in `action.yml` to the release and its `SHA256SUMS` line. CI's `action` job then runs the downloaded release, and `pin-drift` stops warning.

## Solution and publisher

None.

## Constraints

- AGENTS.md: smallest change, new logic with tests, Conventional Commits, no version or changelog edits by hand, no tenant URLs, ids, emails or user names in commits. No new file write in `src`, so `SafeWrite` is not involved.
- The pinned roles `powerops-verify` and `powerops-review-security` and `tests/eval` are untouched.
- Platforms are Windows and Linux. `install.sh` runs in bash on both runners (Git Bash on Windows); `ActionScriptTests` run on Linux only, as today. The script changes into its folder and hashes from stdin, so no Windows path reaches `sha256sum` or `unzip` as an argument.
- Other P0 lanes run in parallel (M50, M54, M58). This milestone's files are the release workflow, `action.yml`, `action/install.sh`, `pin-drift.sh`, `Directory.Build.props:11` and the docs above; the docs edits stay in the lines named.
- From this change on, a plan file made by a build of one commit is refused by a build of another (`the plan was made by powerops <version>+<commit>; this is ...`). That is the check the M10 review asked for; the Action plans and applies with one pin, so its runs are unaffected (decisions Q3).

## Assumptions

- Learn, checked 2026-11-04 with the Learn MCP tools. "Source Link included in the .NET SDK" (.NET 8 breaking changes): from .NET 8, `InformationalVersion` includes `SourceRevisionId`, and setting `IncludeSourceRevisionInInformationalVersion` to `false` turns it off. "MSBuild reference for .NET SDK projects": the property defaults to `true`; the SDK fills `SourceRevisionId` with the commit hash when Source Link is present. Observed here with the SDK of `global.json`: a build with the property on prints `0.0.0+b8ad9049cc52d8dcb2284f581d6f470384e0120e`, the full `HEAD`.
- Learn, checked 2026-11-04 with the Learn MCP tools. Power Platform Well-Architected, "Recommendations for safe deployment practices" (OE:10), the source the improvement plan cites for R1, general recommendation 1: "Implement versioning across your build artifacts to help ensure that you can roll back and roll forward when necessary." "Recommendations for securing a development lifecycle", "Keep versioned artifacts": keep a catalog of all deployed assets and their versions.
- GitHub, checked 2026-11-04: immutable releases are off in this repository (`GET /repos/Chrome71/PowerOps/immutable-releases` gives `enabled: false`). With them on, assets can't be added to a published release, which is when `publish` uploads (decisions Q7). The repository is public, so a release asset downloads without a token.
- Git for Windows ships `unzip` (not `zip`), `sha256sum` and `curl`, so Git Bash on the Windows runner has the three tools `install.sh` needs. This was not run on a Windows runner here; the first Windows run of the Action with a pin is the check.
- release-please (`release-please-config.json`): `include-v-in-tag` is true, so the tag is `v<version>`; the `generic` updater rewrites the line marked `x-release-please-version`, and the two `json` updaters the `plugin.json` versions. Release PR #4 changes the manifest, the changelog, `Directory.Build.props` and both `plugin.json` files.

## Open questions

## Questions for your admin

## Steps

1. This plan and `decisions.md`, a fresh-context review into `review.md`, fixes, and sign-off under the delegation of 2026-10-22 once no High or Medium finding is open.
2. Rows 1, 2 and 7, test first; then rows 3, 4 and 5.
3. Row 6.
4. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .`, the leak scan; push, PR, CI.
