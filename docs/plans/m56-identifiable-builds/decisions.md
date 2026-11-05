# Decisions: m56-identifiable-builds

## Round 1

### Q1. Through which channel do releases ship? (D4 of the improvement plan)
1. GitHub Releases only: release-please stamps the version, the release carries the package and a checksum, and the Action downloads a pinned, checksummed release (recommended)
2. GitHub Releases and NuGet
3. In your own words
Answer: 1, answered by the person on 2026-11-04 ("GitHub Releases only (Recommended)"), not by an agent. NuGet's steps leave `release.yml`: they would fail the first release (no Trusted Publishing policy exists), and the name `PowerOps` is not ours on nuget.org.

### Q2. How does a build name itself?
1. The SDK's default: a build from a git checkout prints `<version>+<full commit>`; the release build turns the commit off and prints the plain version, its tag without the `v` (recommended)
2. The commit always, in the release too
3. A build number or date of our own
Answer: 1. Learn documents the default (`IncludeSourceRevisionInInformationalVersion` is `true`, and Source Link fills `SourceRevisionId` with the commit), so the change is one deleted line and one property on the release's pack. The plain version then means one thing: the package of that release, which a reader can check against `SHA256SUMS` and the attestation. Known limits: a tree without `.git` (a source archive) prints the plain version, and uncommitted changes add no mark; neither is a release package, and the docs say the release is the checked artifact.

### Q3. Is the stricter `tool` check a breaking change?
1. No: the documented rule stays "plan and apply must run the same version"; it now tells two commits apart, which the M10 review asked for. The PR title is `feat:` and the PR body says what a person sees (recommended)
2. Yes: `feat!:`
Answer: 1. No input, output field, exit code or flag changes. A person who reinstalls a build of another commit between plan and apply gets the existing refusal, "plan again with this version", which is what the check was for. The Action plans and applies with one pin, so its workflows are unaffected. No release exists, so no released behaviour changes.

### Q4. What binds the Action to the bytes it runs?
1. The SHA-256 of the package, pinned in `action.yml` next to the version (recommended)
2. `gh attestation verify` at run time
3. The release's own `SHA256SUMS`
Answer: 1. A commit of the Action then fixes the bytes it runs: a replaced asset fails the check, and the workflow author's pin by commit covers both values. The release's `SHA256SUMS` can be replaced with the asset, so it proves nothing at run time. Attestation checks need `gh`, a token and Sigstore at every run; they are the maintainer's check when the pin moves (CONTRIBUTING, Releases).

### Q5. How does the pin move after a release?
1. By a pull request that sets both values, after the maintainer checks the release; `pin-drift` warns until it is done (recommended)
2. A step of the release job opens that pull request
3. release-please bumps the version in `action.yml`
Answer: 1. Option 3 can't work: the package's hash does not exist when the release commit is made, so the tag's `action.yml` would pin a version with a stale hash. Option 2 needs a token that may open pull requests, and a pull request opened with `GITHUB_TOKEN` starts no CI, so a person handles it anyway. Between a release and the pin's move, the Action keeps running the previous pin (or the source build), which works.

### Q6. What does the Action do with an empty pin?
1. Build from source in locked mode, as it does today (recommended)
2. Fail
Answer: 1. No release exists yet, and the Action on `main` must keep working until the person cuts one. A fork without releases keeps working too. The source build is the same command as before, and ci's `The Action's build, in locked mode` step still checks it on both systems.

### Q7. When are the assets attached?
1. After release-please creates the published release, in the `publish` job; immutable releases stay off in this repository (recommended)
2. release-please makes a draft, `publish` attaches the assets, then publishes it
Answer: 1. Immutable releases are off (checked 2026-11-04). With them on, the upload fails with a 422 and the release has no assets, which is loud; then option 2 (release-please's `draft` with `force-tag-creation`) is a change of its own. The upload has no `--clobber`, so a rerun can't replace an asset, and the Action's pinned hash fails on a replaced one in any case.

### Q8. Who cuts the first release?
1. The person, by merging release PR #4 after this PR; this milestone creates no tag or release and does not touch PR #4 (recommended)
2. This session
Answer: 1. The brief forbids a tag, a release and any publish, and asks for the step to be the person's. Plan, "Environments and route", lists the three steps: merge PR #4, check the release, move the pin.

### Q9. How does the Action unpack the package?
1. `unzip` of the package's `tools/` folder, and the dll at the package's own path (recommended)
2. `dotnet tool install --tool-path`, and the dll under the tool store's `.store/powerops/<v>/...`
Answer: 1. The dll's path in a tool package is the package's documented layout; the tool store's folder layout is the SDK's internal one. Git Bash on the Windows runner and Ubuntu runners have `unzip`. A test builds a zip in that layout, so the check runs without a real release. The package's bytes are checked before it is unpacked.

### Q10. How does the release job check the version it ships?
1. Install the package from `artifacts` only and require `--version` to print the tag's version (recommended)
2. Check the file name only
Answer: 1. `--version` is what Done when 1 promises, so the job checks that output, not a file name. The file name check stays as the first half, so an empty `artifacts` fails with a clear line.

### Q11. One release job with `contents: write`, or a separate upload job?
1. One job: the write token reaches the upload step only through its `GH_TOKEN`, and the pinned `checkout` and `attest-build-provenance` actions, which take `github.token` as a default input (recommended)
2. A build job with `contents: read` that hands the files to an upload job with `contents: write`
Answer: 1. The pack's own code (the repo at the tag and locked packages) is not given the token: GitHub sets no token variable for a `run` step unless the workflow passes one, and the checkout keeps no credentials. Option 2 adds an artifact upload and download for the same files. The job already holds `id-token: write` for the attestation.

### Q12. Does `docs/BUILD-PLAN.md` get an M56 section?
1. No: the improvement plan (M50 to M68) is not in the repo; the one runbook line that would be wrong after this change (`:622`) is corrected (recommended)
2. Yes
Answer: 1. As in M52 (its decisions Q11).

## Record

- questions answered by the building session under the user's delegation of 2026-10-22 (autonomous milestone mode), except Q1, which the person answered
- areas not asked: users, screens, solution and publisher; the change is to this repo's build and release, and the plan answers each or marks it not applicable
- approved 2026-11-04 by the building session under the user's delegation of 2026-10-22, for a fresh-context review
