plan-hash: 40a2d46c79b8054c29941208b522b81d3bebaebe53c8b8a2dc60c48e2a78871a
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: nobody confirmed a `pac` sign-in, and the plan names no environment (`environment: null`). Checklist items 1 to 8 (solution and publisher, connection references, licences, DLP, route, managed or unmanaged, delegation, security roles) don't apply: the plan changes PowerOps itself and touches no tenant, and its `Environments and route` and `Solution and publisher` sections say so with a reason.

Tools (item 12): `pac` (2.14.1), `zip` (3.0), `dotnet`, `claude`, `codex`, `bash` and `git` are on `PATH` (`command -v`, 2026-11-09). The Fable and Sonnet subagents and the Codex model `gpt-6-astra` can't be checked with `command -v`; not checked, no finding.

Checked against the code at `fca4c57`: the cited lines of `PlanChecker.cs`, `DeployReviews.cs`, `DeployCommand.cs` (`:44`, `:59`, `:64`, `WarnUnreviewed`, `DryRun`, the gate), `McpCommand.cs:249`, `WriteSitesTests.cs` (its scan matches `Directory.CreateTempSubdirectory` and `Directory.Delete`), `RoleEvalTests.cs`, `run.sh`, both kept roles' `SKILL.md`, `powerops-implement` section 4, `HANDOFF.md`, `COMMANDS.md`, `USE-WITH-AGENTS.md:69`, `PLANNING-SKILLS.md:221` and `AGENTS.md:33, 40`. `Manifest.Read` gives a zip's SHA-256 as its digest and normalizes CRLF in text files. Run locally: two `pac solution unpack --packagetype Managed` runs of `ColumnLevelSecuritySampleSolution_managed.zip` into different folders gave the same 27-file digest. `--packagetype Unmanaged` on it exited 1 and left an empty folder behind.

Second pass, 2026-11-09, at `83ed2b4`: each answer was checked against the revised plan and the code at `fca4c57`; R10 is new, caused by R2's answer. Third pass, at `50493f7`: R10's answer and the two edits checked; no new finding.

Fourth pass, 2026-11-10, at `d936680` with the build in the tree. The plan is `approved` again because row 9 added the pin table to Data. Checked:

- **Pins:** the three rows equal `git rev-parse 6b67efb:<path>` for both `SKILL.md` files and `run.sh`, and the same ids are at `825ba70`, `HEAD` and in the working tree (`git hash-object`). `RoleEvalTests` reads them from this plan (`Assert.Equal(3, pinned.Count)` passes), and `AGENTS.md:40` points at this folder.
- **Rows 1 to 3:** `PlanReportFile.DeployHash` is read from line 7 only, and `UnknownHash` when the report can't be read. `ZipUnpack`, `NeedsUnpack` and `Cover` exist, `Recorded` counts `SolutionHash` only, and the fault order matches row 2. `Unpack` uses the zip's own package type, a timeout of the smaller of 120 seconds and the time left less 10 seconds on `RunGate`'s `Stopwatch`, and the five failure sentences (an unpack with no file is unreadable), and deletes its temp folder in `finally` with a `shortcut:` comment. The `deploy plan`, `--apply` and MCP `deploy_plan` texts are changed. `WriteSitesTests` gained the two entries and `AGENTS.md:33` the exception.
- **Rows 4 to 7:** R1V, R1S, C3 and H7 appear verbatim in the shipped skills, with seven-line wording and Verify step 1 as row 4 says. `run.sh` has (a) to (f), with the `|| true` guard, and passes `bash -n`.
- **Rows 8 and 9:** COMMANDS, HANDOFF, implement, USE-WITH-AGENTS, PLANNING-SKILLS and GITHUB-ACTION carry the route. The text includes the five failure sentences, the review folder in HANDOFF's ignore guidance, the recovery into a new or emptied folder, and the runner's `pac` version. The codex plugin copies equal `skills/`, and the claude copies differ only by the generated fork front matter. After the freeze, nothing under `src/` and no evaluated file changed (`git diff --stat 6b67efb d936680`).
- **Checks run here:** `dotnet build`; `dotnet test` (4,672 tests: 4,666 passed, 6 skipped, 0 failed); `dotnet format --verify-no-changes` (clean); `powerops plan status` (no violation). No `powerops-unpack-` folder was left in the system temp folder. The eval was not rerun, and the CI checks were not seen.
- **Outside the plan's body:** `decisions.md` records that a debugging run called `pac org who --environment x`, which `pac` refused locally. That is a deviation from the no-tenant limit, recorded honestly, and not a plan finding.

`review: done` was not set in `plan.md`, because this review's request allowed writing only `review.md`. The session that asked for the review sets it.

## Findings

### High

No findings.

### Medium

#### R1. The frozen skill text doesn't say what the role does not check about the zip
- Area: 9 (testability), and the keep rules under Automation
- Finding: R1V, R1S, C3 and H7 tell the role that the current solution is the zip's unpacked folder and that it should hash the zip. They don't say that the role does not check the folder against the zip, or read, `analyze` or `diff` the zip. In the eval the zip is a plain `zip` of `solution/current`, not a `pac` export, and `Bash(powerops analyze *)` lets a role run `analyze deploy/current.zip`. That fails, because the zip has the unpacked layout, and a role may then raise a finding that the deployed zip is unreadable or its provenance is unconfirmed. On the clean cases `solution-02` and `solution-06`, a High of that kind counts as a false High. One of those per role fails a keep rule and stops the milestone after the paid runs. In real use the previous solution is an unmanaged export and the current one a managed unpack (decisions Q2). Nothing in the role text says that the managed-flag change in `diff` and the forms renamed `_managed` are expected, so the Verifier's "anything built outside the plan is a finding" can fire on them.
- Suggested fix: Before the freeze, add one sentence to C3 (or to R1V and R1S): "Only hash the zip: don't analyze, diff or read it, and don't check the folder against it; the deploy routes check that. With a managed zip, the managed flag and forms named `{id}_managed.xml` differ from an unmanaged previous solution by design." In step 3's stub check, add that the prompt and skill text carry the sentence.
- Resolution: resolved (2nd pass): C3 now says to run nothing else on the zip, not to check the folder against it, and that the managed flag and `{id}_managed.xml` forms differ by design. Step 3's stub check confirms that the copied skill holds the whole sentence. C3's first code span is still `powerops manifest <deployed zip>`, so `ListedCommands` and the allowlist test are unaffected.

#### R2. The gate's deadline path leaves the temp unpack and a running `pac` behind
- Area: 9 (testability), Constraints (writes)
- Finding: `RunGate` runs the check on `Task.Run`, and when `check.Wait(deadline)` times out it returns 2 while the check keeps running. The process then exits, so the `finally` that deletes the temp folder never runs, and the `pac` child may still be writing. Each zip a report names by deploy-hash gets its own unpack with a 120-second timeout, so three such zips can pass the gate's 300-second deadline. The plan says "the temp folder is gone after the run, on the success and every failure path", but its tests don't name this path, or a Ctrl+C during `deploy apply`. The folder left behind is a full unpack of the solution, `.msapp` files included, which cache the environment URL (HANDOFF step 3).
- Suggested fix: Bound the unpacks by the gate's remaining time. For example, pass a deadline into `WarnUnreviewed`, give each `pac` call `min(120 s, time left)`, and fail closed with no new unpack once the time is short. Add a gate test with a short deadline and a `FakePac` that blocks, which asserts that no temp folder is left. If a leftover is accepted instead, write it as a `shortcut:` comment and name it in COMMANDS.
- Resolution: resolved (2nd pass): row 3 passes the gate's deadline down. Each unpack's timeout is the smaller of 120 seconds and the time left less 10 seconds, and no unpack starts with under 10 seconds left (`no time was left to unpack it`). A short-deadline gate test asserts that no temp folder is left. Watch at build time: the time left must be counted from the clock `RunGate` starts for `check.Wait`, not from the start of `DryRun`. A Ctrl+C during `deploy apply` still skips the `finally`; that is minor and not a new finding.

#### R3. The plan doesn't say whether the in-repo managed unpack is committed or ignored
- Area: 9 (testability), 2 (hard-coded values)
- Finding: Row 8 puts the reviewed folder in the repository (such as `review/ContosoApp_managed`), because the roles' `input:` must be a path relative to the repo, but it never says whether that folder is committed. Committed: the Action's `path` input defaults to `.` and is searched recursively, so `analyze` and the `tests` input see a second, managed copy of the solution, and the folder carries the same `.msapp` environment-URL caching that HANDOFF step 3 warns about for the unmanaged folder. Not committed: `plan status` on any other checkout warns that the input was not found. HANDOFF step 3's `.gitignore` guidance covers only the two zips and `src/ContosoApp`.
- Suggested fix: In row 8, choose one. A likely choice: the folder is git-ignored; HANDOFF step 3 adds it to the paths to ignore, with the `.msapp` reason; and COMMANDS and HANDOFF say that `plan status` elsewhere warns that the input was not found, which is expected. If it is committed instead, say how the Action's `path` keeps it out of `analyze` and `tests`.
- Resolution: resolved (2nd pass): row 8 keeps the review folder out of Git like the two zips, adds it to HANDOFF step 3's ignore list with the `.msapp` reason, and says that `plan status` in another checkout reports the input as missing, which is expected. The Action's checkout then has no second copy.

#### R4. On the primary platform, and with the Action's `pac`, the route is not checked
- Area: 9 (testability), Assumptions
- Finding: Windows is the primary platform, and the Action checks on an Ubuntu runner with whatever `pac` its earlier steps install. The plan lists cross-OS and cross-version digests as not checked. Its one real-`pac` test skips on Windows, because `Tools.Missing(["pac"], PATH)` checks a bare `pac` with no extension (`Tools.cs:10-13`), so the Windows path of the unpack never runs. When the Action does the check, the recovery in row 8 ("unpack again with ... the `pac` that runs `deploy plan`") can't be followed from a Windows machine, so in that case the route falls back to M75's every time. This is fail-closed, so it is not a release blocker, but Done when 1 to 3 can pass while the main route doesn't work for the primary user.
- Suggested fix: Add a step, or a Done-when line, that runs the managed unpack and `powerops manifest` on the Windows VM from the M63 runbook for one seed zip and compares the digest with Linux's. Or make the hand-over name this as the second trial's first check, with a stated outcome. Have COMMANDS and the Action docs say that the runner's `pac` version must equal the reviewer's for the link to hold.
- Resolution: resolved (2nd pass), as a stated deferral: Assumptions make the Windows-`pac` against runner-`pac` digest comparison on one managed seed zip the second trial's first check, with its outcome recorded. They also say that M75's route may be the one that works until then, and that the real-`pac` test skips on Windows. The recovery names the runner's `pac` version. The gap is still unverified, but it is named, owned and fails closed.

### Low

#### R5. The rows' Where columns miss tests that quote the changed sentences
- Area: 9 (testability)
- Finding: The new "no report records" sentence and the new fault sentence break `NoneRecords` in `DeployGateCommandTests.cs:690`, `DeployApplyCommandTests.cs:1448` and `DeployReviewWarningTests.cs:8`, and the "records another solution-hash" cases in `DeployReviewsTests.cs:32, 43, 56`. Row 2's Where names only `DeployReviews.cs` and `DeployReviewsTests.cs`, and row 3's doesn't name `DeployGateCommandTests.cs`.
- Suggested fix: List those files in rows 2 and 3.
- Resolution: resolved (2nd pass): rows 2 and 3 name `DeployReviewsTests.cs:32, 43, 56`, `DeployReviewWarningTests.cs:8`, `DeployApplyCommandTests.cs:1448` and `DeployGateCommandTests.cs:690`, which match the lines at `fca4c57`.

#### R6. An unpack that writes no file is taken as a digest
- Area: 9 (testability)
- Finding: A `pac` that exits 0 but writes nothing gives `Manifest.Read` an empty folder, whose digest is the SHA-256 of empty input. The failed `Unmanaged` unpack run in this review left exactly such an empty folder, though with exit 1. A report that claims that digest would link. This is unlikely, but the plan's failure sentences don't cover it.
- Suggested fix: Treat an unpack with 0 files as "the unpacked folder could not be read", and test it with `FakePac`.
- Resolution: resolved (2nd pass): row 3 counts an unpack that holds no file as `the unpacked folder could not be read`, and its tests include a `pac` that exits 0 and writes nothing.

#### R7. A failed deploy-hash link also hides M75's evidence notes
- Area: 10 (unanswered areas)
- Finding: Row 2 makes `Recorded`, and so `Evidence`, count a report whose `DeployHash` is the zip's. When the provenance check fails (an OS or `pac` mismatch, R4), `deploy plan` no longer prints the diff evidence notes for that zip. That is the case where the person, about to fall back to their own `deploy apply`, needs them most. The plan states the rule but not why.
- Suggested fix: Either let `Evidence` run for a zip whose only record is a deploy-hash link that failed, or record the reason for dropping the notes in decisions.
- Resolution: resolved (2nd pass): row 2 keeps the public `Recorded` on `SolutionHash` only and counts both hashes only when it picks the first folder for the fault sentence. A zip whose deploy-hash link fails is still in `WarnUnreviewed`'s open list and isn't `Recorded`, so `Evidence` gives its notes. A zip covered through its deploy-hash has no problem and gets none, as today.

#### R8. The recovery step doesn't say to unpack into a new or emptied folder
- Area: 9 (testability)
- Finding: Row 8's recovery is "unpack again with exactly the command above". Unpacking into the existing review folder without `--allowDelete` keeps files that the new zip doesn't have, so the digest still differs and the check refuses again. The route itself says "a new folder"; the recovery doesn't.
- Suggested fix: In COMMANDS and HANDOFF, say "unpack again into a new or emptied folder".
- Resolution: resolved (2nd pass): COMMANDS and HANDOFF's recovery now says to unpack again into a new or emptied folder.

#### R9. `run.sh`'s digest guard doesn't run when `powerops manifest` fails
- Area: 9 (testability)
- Finding: Under `set -euo pipefail`, if `powerops manifest` fails, the assignment `zip_digest="$(... | sed -n ...)"` exits the script with powerops' code before the `error: no digest for the deployed zip` check runs. Step 3's stub check only tests a missing `zip`.
- Suggested fix: Add `|| true` inside the substitution, or `|| zip_digest=""`, so the guard prints its message and exits 3. Add that case to the stub check.
- Resolution: resolved (2nd pass): row 6 (c) puts `|| true` inside the command substitution, so under `set -euo pipefail` a failing `powerops manifest` leaves `zip_digest` empty and the guard prints its error and exits 3. The stub check covers that case.

#### R10. Row 8 still says "four failure sentences"
- Area: 9 (testability)
- Finding: R2's answer added a fifth unpack failure sentence to row 3, `no time was left to unpack it`, and R6's answer widened `the unpacked folder could not be read`. Row 8 still tells the COMMANDS writer to document "its four failure sentences", so the writer of row 8 (a different executor from row 3's) may leave the gate's new sentence out of COMMANDS.
- Suggested fix: In row 8, say "its five failure sentences", or list them, including `no time was left to unpack it` and the empty-unpack case.
- Resolution: resolved (3rd pass, `50493f7`): row 8 now says five failure sentences and names `no time was left to unpack it` and the empty-folder case of `the unpacked folder could not be read`. That matches row 3's five: `pac was not found`, `pac solution unpack timed out`, `pac solution unpack exited <code>`, `the unpacked folder could not be read` and `no time was left to unpack it`. Row 3's new "counted from the clock `RunGate` starts" agrees with R2's watch note and causes no new problem.

#### R11. The keep decision rests on the building session's settlement of five split Highs
- Area: 10 (unanswered areas), Automation (keep rules)
- Finding: `report.md` (line 25 and "Clean-case Highs") says that all five Highs on the clean cases came from Codex and that the two scorers split on every one. The building session settled them as supported. Under the Sonnet scorer's reading the Verifier has 2 false Highs and misses the at-most-one rule, so the keep holds only on that settlement. Two of the five ("deployment input is unmanaged") come from the eval's own design under decisions Q5: a plain zip of an unmanaged folder, which the new skill text calls "the managed zip that will be deployed". A later rerun inherits that. All of this follows the plan's scoring rule and is disclosed, and the person delegated the keep. But the plan never says that a split on a deciding number goes back to the person, or into the hand-over.
- Suggested fix: Have step 7's hand-over and the PR body quote `report.md`'s line 25 for the person, and record in `decisions.md` that the eval inputs of Q5 provoke the "unmanaged deployment input" finding, so the next rerun knows. No change to the pins.
- Resolution: open
