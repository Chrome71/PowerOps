---
status: approved
review: pending
environment: null
depth: standard
supersedes: null
signed-off: null
signed-off-hash: null
---

# M120: Release assets: two binaries, a sample, checksums; no release

## Goal

Phase 1 item 1 of the agreed plan (`reports/three-way-debate-2026-11-22/AGREED-PLAN.md`, git-ignored), as step 1 of its build plan: a release should carry what a newcomer can run without the .NET SDK. Today `release.yml` packs, attests and attaches only `PowerOps.<version>.nupkg` and `SHA256SUMS`. This milestone makes the release workflow build two self-contained single-file binaries and a sample, runs each binary on its own OS before anything is attached, and makes CI build the same assets on every pull request by the same script. It cuts no release: the release is the main session's step 9, after the owner's yes.

Done when:

1. `.github/scripts/release-assets.sh <folder> [<property>...]`, run from the repo root after `dotnet pack` has put one `PowerOps.<version>.nupkg` in `<folder>`, adds `powerops-linux-x64` and `powerops-win-x64.exe` (self-contained, single file, not trimmed), and `powerops-sample.zip` (Data row 2), then writes `SHA256SUMS` over those four files. It refuses (`::error::`, exit 1, no `SHA256SUMS`) when `<folder>` holds no nupkg or more than one, or when a publish restored application packages other than the committed lock files' (Data row 1). For each runtime it prints the .NET runtime version it bundled. A test runs it with a stub `dotnet` in a temp root (it writes nothing under the repo's `obj/`) and checks the publish arguments, the printed runtime line, the zip's entries, that `SHA256SUMS` names exactly the four files and checks, and that each refusal prints one `::error::` line and leaves no `SHA256SUMS`.
2. `release-check.sh <version> <folder>` keeps its package checks and adds: `<folder>` holds exactly the five release files (Data row 3), `powerops-linux-x64 --version` prints exactly `<version>`, and `SHA256SUMS` names exactly the four assets and `sha256sum -c` passes. It no longer writes `SHA256SUMS`. Its tests cover each refusal.
3. CI's `package` job runs `release-assets.sh` after its pack, then runs `powerops-linux-x64 --version` and `analyze` on the unzipped sample with `--fail-on none`, then keeps the assets as a workflow artifact for one day. A new job `windows-binary` (`windows-latest`, needs `package`, not a required check) downloads them and runs `powerops-win-x64.exe` the same way. The five required checks keep their names exactly: `build (ubuntu-latest)`, `build (windows-latest)`, `package`, `action`, `playwright-example`.
4. `release.yml` has four jobs. `release-please` unchanged. `assets`, `windows` and `publish` each run only `if: needs.release-please.outputs.release_created == 'true'`, so a push to `main` that cuts no release builds nothing. `assets` (Linux, `contents: read`): checkout at the tag, locked pack without the commit in the version, `release-assets.sh` with the same version property, `release-check.sh`, the Linux run, keep as a workflow artifact for 30 days (GitHub's re-run window). `windows` (no permissions): the Windows run on the downloaded files, failing unless `powerops-win-x64.exe --version` prints exactly `$VERSION` (from `env`). `publish` (needs all three; `contents: write`, `id-token: write`, `attestations: write`; no checkout, no build): download, `sha256sum -c SHA256SUMS`, attest every file `SHA256SUMS` lists, upload the five files with no `--clobber`, `GH_TOKEN` only on the upload step. Nothing reaches the release until both binaries ran.
5. `WorkflowTests` assert the asset list (the upload's five files, the script's four checksummed files), the jobs' permissions and `if` conditions, the step order, the Windows version check, the release artifact's 30 days, the required check names, and that neither `release-assets.sh`, the two project files nor `Directory.Build.props` sets `PublishTrimmed`.
6. The sample's `README.md` names its expected result, and a test runs `analyze` and `test` on the source fixtures and finds the README's two result lines in the output.
7. `README.md` and `docs/INSTALL.md` put the release download first and keep "until the first release, install from a clone", so both are true while no release exists; the README's agent paste prompt keeps the clone route. INSTALL's Linux download steps (`sha256sum -c --ignore-missing`, `chmod +x`, installed on PATH as `powerops`) are run word for word in a folder holding only `powerops-linux-x64` and `SHA256SUMS` from a real `release-assets.sh` run, with the output in the hand-over; the Windows steps are reported as not run. `CONTRIBUTING.md`'s release section describes the four jobs, the re-run, and the fallback of Data row 5, and says the runtime packs are outside every lock. `docs/BUILD-PLAN.md` gets the M120 decision (number taken at merge).
8. CI green on the pull request, with both binaries' runs read in the job logs; `dotnet build` (0 warnings), `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m120-release-assets` with no violation, and the leak scan clean.

### Won't build

- No release, no tag, nothing on release PR #4; no edit to `release-please-config.json`, `action.yml` (its `PO_RELEASE` pin is step 9's own pull request), the version files or the changelog.
- No macOS, ARM or other runtime; no trimming, ReadyToRun or AOT; no code signing.
- No install script or package manager entry; no change to `skills/`, `plugins/`, `.claude/agents/`, `tests/eval/` or any command's output.
- No README front door or first-run section (M125 writes that, with this sample).
- No third-party notices file among the assets for the runtime and packages the binaries carry: flagged for the owner before the release cut, in the hand-over.

## Users

- A newcomer who downloads a binary from a release and runs it, with no .NET SDK.
- A maintainer who cuts a release (step 9) or finishes a failed one by hand (`CONTRIBUTING.md`).
- M125, which points the README's first run at `powerops-sample.zip`.

## Data

Row 1. The publish, per runtime `linux-x64` and `win-x64`: `dotnet publish src/PowerOps.Cli -c Release -r <rid> --self-contained -p:PublishSingleFile=true -p:EnableSingleFileAnalyzer=false -p:NuGetLockFilePath=obj/<rid>.lock.json -o <temp>/<rid> <properties>`. The committed lock files have no runtime section, and a locked restore with `-r` fails (NU1004, seen 2026-11-22 on SDK 10.0.114). Adding `RuntimeIdentifiers` to the projects would fix the lock but makes every restore, the Action's source build included, download four runtime packs and two apphost packs (seen: a fresh-cache restore fetched them). So the publish restores unlocked and writes its own lock under `obj/`; the script then requires, for both projects, that the fresh lock's `net10.0` section equals the committed one (`jq`) and that its runtime section is empty, so the application packages in the binaries have the committed content hashes. The runtime and apphost packs (`Microsoft.NETCore.App.Runtime.<rid>`, `Microsoft.NETCore.App.Host.<rid>`) are download dependencies that NuGet writes to no lock, committed or fresh; their version follows the SDK that `global.json` allows (`10.0.100`, `latestFeature`), and they come from nuget.org under Microsoft's signature, the same trust root as the SDK itself. So no lock holds the .NET runtime, most of each 78 MB file; the script prints the version it bundled (from the build's `powerops.runtimeconfig.json`) so a release's log names it. `EnableSingleFileAnalyzer=false` keeps the analyzer's implicit `Microsoft.NET.ILLink.Tasks` reference, whose version follows the SDK, out of the restore; the binaries are run instead (Done 3, 4). Only the executable is kept; the `.pdb` files are not assets.

Row 2. `powerops-sample.zip` holds one folder, `powerops-sample/`: `solution/` (a byte copy of `tests/fixtures/powerops-seed/v2/`), `tests/` (the two files of `tests/fixtures/flowtests/seed/`) and `README.md` (from `.github/scripts/sample-README.md`). Both fixtures were made for this project under Apache-2.0 with fake values (`tests/fixtures/NOTICE.md`). Expected result, run on 2026-11-22: `powerops analyze solution` exits 0 with PO005 and PO006 and the canvas-package note, last line `1 solution, 0 errors, 2 warnings`; `powerops test solution --tests tests` exits 0, last line `2 tests: 2 passed, 0 failed, 0 unmocked, 0 unsupported`.

Row 3. The release files: `PowerOps.<version>.nupkg`, `powerops-linux-x64`, `powerops-win-x64.exe`, `powerops-sample.zip`, and `SHA256SUMS` over the first four. The attestation covers the four through `subject-checksums: artifacts/SHA256SUMS`.

Row 4. Actions added, pinned by commit like the rest (Dependabot keeps them current): `actions/upload-artifact` v7.0.2 `cf430e030ddbb5b0abf93d22962f4752f3646cd9`, `actions/download-artifact` v8.0.2 `9000827ccba6bdab643e8b6fd33ac0654aef8333` (each tag's commit, read with `gh api` on 2026-11-22). The Windows runs use `shell: bash` (Git Bash, `-eo pipefail`; the runner's Git Bash has `unzip`, as `action/install.sh` already relies on) with relative paths. The Linux run happens in the job that built the binary, because artifacts lose the executable bit.

Row 5. Recovery of a failed release (`CONTRIBUTING.md`). Nothing attached: fix the cause and "Re-run failed jobs" within 30 days; `assets`' artifact is kept that long. Some files attached (the upload stopped part-way, and a re-run would fail on the first name already there): `gh run download <run> -n release-assets -D artifacts`, `sha256sum -c SHA256SUMS` in it, then `gh release upload` of the missing names only; those are the attested bytes. Artifact gone or the attestation step never ran: rebuild from the tag by the four commands in `CONTRIBUTING.md` (pack, `release-assets.sh`, `release-check.sh`, upload) only when nothing is attached yet, and the files carry no attestation; with some attached, stop and report, because a rebuild has other hashes.

## Automation

The two workflows above. No flow or tenant automation.

## Screens

None.

## Environments and route

Offline. No tenant call of any kind.

## Solution and publisher

None.

## Constraints

- The five required check names stay exactly as they are; the Windows run is a new job that the `main` ruleset does not require.
- In `release.yml` the only job with a write token runs no checkout and no build: it downloads, checks the sums, attests and uploads.
- Every new `run:` text holds no `${{ }}` expression and every job has a timeout (existing `WorkflowTests`).
- `README.md` and `docs/INSTALL.md` stay true with no release published.
- `tests/eval/`, the kept roles and their plugin copies stay byte-for-byte.

## Assumptions

- A1. A single-file, self-contained `win-x64` publish runs on Linux and gives a Windows executable (done locally on 2026-11-22: `powerops.exe`, 78 MB); the CI Windows job is the proof that it runs. Sources: `powerops-researcher`, recorded in `decisions.md`.
- A2. `EnableSingleFileAnalyzer` and `NuGetLockFilePath` behave as Data row 1 says (seen locally on 2026-11-22; sources in `decisions.md`).
- A3. `attest-build-provenance` v4 accepts `subject-checksums` (its `action.yml` at v4.2.2 lists it beside `subject-path`), and artifact upload and download drop the executable bit, so each binary runs in the job that built it or after a fresh download on Windows, where the bit does not matter.

## Open questions

None.

## Questions for your admin

None.

## Steps

1. **Plan review** (main session). Fable plan review until approved; `powerops-review-plan` in a fresh context; answer every finding.
2. **Sign-off** (main session). `powerops plan sign-off docs/plans/m120-release-assets`, logged in `decisions.md` with the owner's "Yes to all three" (2026-11-22) as authority.
3. **Scripts, test first** (opus). Tests for `release-assets.sh` (stub `dotnet`) and the extended `release-check.sh` in `WorkflowTests`, seen failing; then the scripts and `sample-README.md`; the sample README test. Done: Done lines 1, 2, 6.
4. **Workflows** (opus). `ci.yml` and `release.yml` as Done lines 3 and 4; `WorkflowTests` for them, seen failing first. Done: Done lines 3 to 5; a local run of `release-assets.sh` and `release-check.sh 0.0.0` on a real pack, both binaries' `--version`, and the Linux binary's `analyze` and `test` on the unzipped sample give Data row 2's lines.
5. **Docs** (opus). README, INSTALL, CONTRIBUTING; BUILD-PLAN decision. Done: Done line 7; `DocsDriftTests` and `InstallLineTests` pass.
6. **Checks, work review, merge** (main session). Done line 8's local checks; Fable work review; merge lock, rebase, push, PR; CI green, both binaries' runs read in the logs; `status: done`; merge by the chain's protocol; hand-over note.
