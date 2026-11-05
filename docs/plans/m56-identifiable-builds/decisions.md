# Decisions: m56-identifiable-builds

## Round 1

### Q1. Through which channel do releases ship? (D4 of the improvement plan)
1. GitHub Releases only: release-please stamps the version, the release carries the package and a checksum, and the Action downloads a pinned, checksummed release (recommended)
2. GitHub Releases and NuGet
3. In your own words
Answer: 1, answered by the person on 2026-11-04 ("GitHub Releases only (Recommended)"), not by an agent. NuGet's steps leave `release.yml`: they would fail the first release (no Trusted Publishing policy exists), and the name `PowerOps` is not ours on nuget.org.

### Q2. How does a build name itself?
1. The SDK's default: a build from a git checkout prints `<version>+<full commit>`; the release build turns the commit off and prints the plain version (recommended)
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
1. Install the package from `artifacts` only and require `--version` to print release-please's version (recommended)
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

## Round 2 (from the review)

### Q2, amended (R5)
The Action's own source build is a build of a commit too, but the runner unpacks an action pinned by commit into a folder named after its ref, with no `.git`, so the SDK finds no commit. `install.sh` passes `-p:SourceRevisionId=<ref>` when that folder's name is 40 lowercase hex, and nothing otherwise: a branch or tag ref gets the plain version, as before, and the docs already say to pin a full commit. The folder layout is the runner's, not a documented contract; if it changes, the build prints the plain version again, which is today's behaviour.

### Q3, amended (R6)
"The Action's runs are unaffected" held only within one run or one Action ref. The promotion example plans on the pull request and applies after the merge, in another run; if the Action's ref (or its release pin) moves between them, the apply is refused with "plan again with this version". That is the check working, not a regression: a plan from another build was never meant to apply. The docs say a plan is valid only for the build that made it, and CONTRIBUTING says not to move the pin while a promotion waits for its apply.

### Q13. How is a failed `publish` repaired? (R3)
1. "Re-run failed jobs" on that run, after fixing a cause outside the repo; a cause in the files at the tag needs a fix on `main` and a new release (recommended)
2. A `workflow_dispatch` trigger with a tag input that runs `publish` for an existing release
Answer: 1. GitHub's docs: a re-run of failed jobs reuses the outputs of the jobs that passed, so `publish` sees `release_created` and the same tag and version. A dispatch route is a second way to publish, with its own input to check and its own test, for a case that re-running covers. A release whose files at the tag can't pass is a broken release either way; the person decides what to do with it.

### Q14. What is the release tag? (R1)
1. release-please's tag, as it is configured now: `PowerOps-v<version>` (`include-component-in-tag` defaults to true, and the component is the package name). `publish` takes `tag_name` and `version` from release-please's outputs and never parses the tag; the Action builds the download URL from `PowerOps-v<pinned version>` (recommended)
2. Set `include-component-in-tag` to false, so the tag is `v<version>`
Answer: 1. Option 2 changes how release-please names the release, and so the title it gives PR #4 and perhaps its branch, which this milestone must not touch; it would also need checking against a real release. With option 1 the release job holds whatever the tag is, and the one place that assumes the form is the Action's URL, which the pin's pull request tests: CI's `action` job downloads the pinned release there. The first version's plan said "the tag without the `v`"; that was wrong and is gone.

### Q15. Does ci's `action` job get a Windows leg? (R10)
1. No: a matrix renames the required `action` check, which the ruleset requires by name; `install.sh`'s Windows leg first runs in a workflow with a pin, and the follow-ups list it (recommended)
2. Yes, now
Answer: 1. The brief keeps the eight checks. `ActionScriptTests` cover the script's logic on Linux; on Windows, it avoids the known traps (relative names after `cd`, the hash from stdin).

### Q16. Does `pin-drift` drop `src` from the doc-pin paths once a release is pinned? (R8)
1. No: the README's quick start clones the pinned commit and builds from `src`, and both pins must name one commit, so a change under `src` still makes that pin stale (recommended)
2. Yes
Answer: 1. After a release, the docs' pins should move to a commit that pins the release, so the warning is the reminder.

### Q17. What stops the Action's scripts from needing a newer CLI than the pinned release? (R4)
1. The rule, in CONTRIBUTING: a script change that needs a newer command or flag waits for the release that has it and its pin; and `pin-drift` warns when commits under `action/` came after the pinned release's tag (recommended)
2. A test that the flags the scripts use exist in `Program.Root`
Answer: 1. Option 2 checks the source, not the pinned release, so it can't see the gap. The warning fires in exactly the window of risk and goes quiet when a newer release is pinned. With a pin, CI's `action` job runs the pinned release with the pull request's scripts for `analyze` and `test`; the deploy steps need a tenant, so the rule covers them.

## Round 3 (from the review's second pass)

### Q13, amended (R12, R13)
GitHub's "Re-run workflows and jobs" page does not say that a re-run of failed jobs reuses the outputs of the jobs that passed; it says a re-run keeps the commit and ref, within 30 days and 50 re-runs. So "Re-run failed jobs" stays the first try, described as observed behaviour, and CONTRIBUTING gives a manual route that needs no re-run: check out the tag, the same pack command, `release-check.sh`, then `gh release upload` of both files. A half attach can't be finished by a new pack, because it has another hash and no upload replaces a file. When the package is attached and `SHA256SUMS` is not, the maintainer writes `SHA256SUMS` from the attached package and uploads it alone, which keeps the package's attestation (review R17); when only `SHA256SUMS` is attached, they remove it on the release page, then re-run or take the manual route. A file attached by hand has no attestation. Still no dispatch route: the manual route covers the same case without a second trigger to secure.

### Q17, amended (R15)
`action.yml`'s own steps call the CLI too (`analyze` through `powerops.sh`), so the rule covers them, and CONTRIBUTING says so. The warning counts commits under `action/` only, because the pin's own pull request edits `action.yml`; it does not see a change in `action.yml`'s calls, and CONTRIBUTING says that as well.

## Record

- questions answered by the building session under the user's delegation of 2026-10-22 (autonomous milestone mode), except Q1, which the person answered
- areas not asked: users, screens, solution and publisher; the change is to this repo's build and release, and the plan answers each or marks it not applicable
- approved 2026-11-04 by the building session under the user's delegation of 2026-10-22, for a fresh-context review
- fresh review (a general-purpose subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-11-04: first pass 1 High, 5 Medium, 5 Low (R1 to R11); answered in `plan.md` and in Round 2 here (Q2 and Q3 amended, Q13 to Q17 added). R8 and R10(a) accepted with reasons, the rest fixed
- the same reviewer's second pass: R1 to R11 hold except R3's basis; 1 Medium and 3 Low new (R12 to R15), answered in Round 3 (Q13 and Q17 amended). Third pass: R12 to R15 hold; 2 Low new (R16, R17), fixed. Fourth pass: R16 and R17 hold, no new finding. In all: 1 High, 6 Medium and 10 Low, every one fixed but R8 and R10(a), with no High or Medium open
- the code (rows 1 to 5 and 7) built by an implementer subagent (Sonnet, `powerops-implementer`) in this worktree; the docs (row 6) by the building session; read by the building session before commit
- signed off 2026-11-04 by the building session under the user's delegation of 2026-10-22, with no High or Medium finding open (`powerops plan sign-off`; `signed-off-hash` equals `review.md`'s `plan-hash`)
