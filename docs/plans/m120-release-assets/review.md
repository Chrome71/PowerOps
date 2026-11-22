plan-hash: 0a96b781d60d6e83dfafaf24b1c9dbbf3031018505a91284ad25b49fac8ad882
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: no `pac` sign-in was confirmed, and the plan's `environment:` is null (offline; no tenant call made).
This is a maintainer milestone for PowerOps itself, not a Power Platform solution. Checklist items 1 (solution and publisher), 3 (licences), 4 (DLP), 6 (managed or unmanaged), 7 (data volume and delegation) and the per-product parts of 11 are not applicable. Item 5 is read as the pull request, release-please and owner-yes route; item 8 as the workflows' token permissions.
Tools checked with `command -v` on this machine: `powerops`, `dotnet`, `jq`, `sha256sum`, `unzip`, `zip`, `gh`, `pac` all found. Not checkable here: the GitHub runner images (Git Bash's `unzip`, `jq`, `zip` on `ubuntu-latest` and `windows-latest`), and the `fable-reviewer` and `powerops-researcher` agents beyond their presence in the agent list.

## Findings

### High

No findings.

### Medium

#### R1. The Windows exact version check will likely see a trailing carriage return
- Area: 13 Buildability
- Finding: Done 4 has the release `windows` job fail unless `powerops-win-x64.exe --version` prints exactly `$VERSION`, under `shell: bash` (Data row 4). On Windows, .NET's `Console.WriteLine` ends lines with CR LF, and bash command substitution strips only the trailing LF, so a plain `[ "$printed" = "$VERSION" ]` compares `<version>\r` with `<version>` and fails. The plan names no source or probe for how Git Bash captures a native Windows program's output. CI's `windows-binary` job (Done 3) runs `--version` without comparing it, so the compare is first run at the real release cut, where a failure leaves a release with no assets.
- Suggested fix: State that the Windows compare strips the CR (`tr -d '\r'`) or add an `unproven:` line probed before step 9, and have CI's `windows-binary` job run the same compare against the version CI builds (for example by passing the release's version property in CI too), with a `WorkflowTests` assertion on it.
- Resolution: open

#### R2. Jobs that read `needs.release-please` must list it in `needs`
- Area: 13 Buildability
- Finding: Done 4 gives `assets`, `windows` and `publish` the condition `needs.release-please.outputs.release_created == 'true'` and `windows` reads `$VERSION` from `env`, but only `publish` is said to need "all three". The `needs` context holds only a job's direct dependencies: if `windows` lists only `assets`, its `if` reads null, the job is skipped, `publish` is skipped with it, and the release run ends without an error and with no assets. Done 5's tests assert the `if` text, which would still pass.
- Suggested fix: Say in Done 4 that each of `assets`, `windows` and `publish` lists `release-please` in `needs`, and add that to Done 5's `WorkflowTests` assertions.
- Resolution: open

#### R3. The documented `sha256sum -c SHA256SUMS` breaks once the file names four assets
- Area: 9 Testability
- Finding: `docs/INSTALL.md` ("From a release") and `CONTRIBUTING.md` ("To check a release") tell the reader to download the package and `SHA256SUMS` only, then run `sha256sum -c SHA256SUMS`. With `SHA256SUMS` over four files (Done 1, Data row 3), that command reports the three missing files and exits 1. Done 7 adds `--ignore-missing` only to the new Linux binary steps; it does not say the package route and the release check change too, and no step runs them.
- Suggested fix: Add to Done 7 that every `sha256sum -c` in `README.md`, `docs/INSTALL.md` and `CONTRIBUTING.md` for a partial download uses `--ignore-missing`, and run the package route's lines in the same word-for-word check.
- Resolution: open

### Low

#### R4. Package build code runs before the lock comparison
- Area: 2 Hard-coded values and supply chain
- Finding: Data row 1 restores unlocked inside `dotnet publish` and compares the fresh lock with the committed one afterwards, so any MSBuild targets a restored package carries run before the content hashes are checked. The job holds only `contents: read` and refuses before `SHA256SUMS`, so the impact is small.
- Suggested fix: Restore first (`dotnet restore -r <rid>` with the same lock path), compare with `jq`, then publish with `--no-restore`.
- Resolution: open

#### R5. Recovery omits a cause in the tagged files
- Area: 5 Route
- Finding: Data row 5 says "fix the cause and re-run", but a re-run uses the tagged commit's workflow and scripts, and the rebuild route runs the tag's `release-assets.sh`. A cause in those files (R1 is one) can be fixed by neither. `CONTRIBUTING.md` states this today; the plan's Done 7 does not say the line stays.
- Suggested fix: Add to Data row 5 and Done 7 that a cause in the files at the tag needs a fix on `main` and a new release, and what is done with the release left without assets.
- Resolution: open

#### R6. Third-party notices are a hand-over note, not a gate
- Area: 10 Unanswered areas
- Finding: The binaries redistribute the .NET runtime and the application packages, whose licences ask for their notices; `Won't build` defers this to the owner in the hand-over, with no step that blocks the release cut on an answer.
- Suggested fix: Name the owner's answer on notices as a precondition of step 9 in the hand-over, not only a flag.
- Resolution: open

#### R7. The Windows install steps are not run
- Area: 9 Testability
- Finding: Done 7 runs INSTALL's Linux steps word for word and reports the Windows steps as not run; CI's `windows-binary` job runs the exe but not the documented `Get-FileHash` compare or the PATH install a Windows newcomer follows.
- Suggested fix: Either have `windows-binary` run INSTALL's Windows lines, or keep them reported as not run in the pull request body and the hand-over.
- Resolution: open

#### R8. URLs in decisions.md
- Area: URLs, GUIDs, emails and names
- Finding: The `## Log` of `decisions.md` holds `<url>` links (Microsoft Learn and GitHub pages), marked to pass `plan status`.
- Suggested fix: Keep the sources by page title and date, or keep the allow marker on purpose and note it in the log.
- Resolution: open
