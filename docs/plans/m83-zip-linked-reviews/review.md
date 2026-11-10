plan-hash: 9e8abf4e273c301da1dde0341b35c14e26a1bdba65547c6628ab342d1106336d
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: nobody confirmed a `pac` sign-in, and the plan names no environment (`environment: null`). Checklist items 1 to 8 (solution and publisher, connection references, licences, DLP, route, managed or unmanaged, delegation, security roles) don't apply: the plan changes PowerOps itself and touches no tenant, and its `Environments and route` and `Solution and publisher` sections say so with a reason.

Tools (item 12): `pac` (2.14.1), `zip` (3.0), `dotnet`, `claude`, `codex`, `bash` and `git` are on `PATH` (`command -v`, 2026-11-09). The Fable and Sonnet subagents and the Codex model `gpt-6-astra` can't be checked with `command -v`; not checked, no finding.

Checked against the code at `fca4c57`: the cited lines of `PlanChecker.cs`, `DeployReviews.cs`, `DeployCommand.cs` (`:44`, `:59`, `:64`, `WarnUnreviewed`, `DryRun`, the gate), `McpCommand.cs:249`, `WriteSitesTests.cs` (its scan matches `Directory.CreateTempSubdirectory` and `Directory.Delete`), `RoleEvalTests.cs`, `run.sh`, both kept roles' `SKILL.md`, `powerops-implement` section 4, `HANDOFF.md`, `COMMANDS.md`, `USE-WITH-AGENTS.md:69`, `PLANNING-SKILLS.md:221` and `AGENTS.md:33, 40`. `Manifest.Read` gives a zip's SHA-256 as its digest and normalizes CRLF in text files. Run locally: two `pac solution unpack --packagetype Managed` runs of `ColumnLevelSecuritySampleSolution_managed.zip` into different folders gave the same 27-file digest. `--packagetype Unmanaged` on it exited 1 and left an empty folder behind.

`review: done` was not set in `plan.md`, because this review's request allowed writing only `review.md`. The session that asked for the review sets it.

## Findings

### High

No findings.

### Medium

#### R1. The frozen skill text doesn't say what the role does not check about the zip
- Area: 9 (testability), and the keep rules under Automation
- Finding: R1V, R1S, C3 and H7 tell the role that the current solution is the zip's unpacked folder and that it should hash the zip. They don't say that the role does not check the folder against the zip, or read, `analyze` or `diff` the zip. In the eval the zip is a plain `zip` of `solution/current`, not a `pac` export, and `Bash(powerops analyze *)` lets a role run `analyze deploy/current.zip`. That fails, because the zip has the unpacked layout, and a role may then raise a finding that the deployed zip is unreadable or its provenance is unconfirmed. On the clean cases `solution-02` and `solution-06`, a High of that kind counts as a false High. One of those per role fails a keep rule and stops the milestone after the paid runs. In real use the previous solution is an unmanaged export and the current one a managed unpack (decisions Q2). Nothing in the role text says that the managed-flag change in `diff` and the forms renamed `_managed` are expected, so the Verifier's "anything built outside the plan is a finding" can fire on them.
- Suggested fix: Before the freeze, add one sentence to C3 (or to R1V and R1S): "Only hash the zip: don't analyze, diff or read it, and don't check the folder against it; the deploy routes check that. With a managed zip, the managed flag and forms named `{id}_managed.xml` differ from an unmanaged previous solution by design." In step 3's stub check, add that the prompt and skill text carry the sentence.
- Resolution: open

#### R2. The gate's deadline path leaves the temp unpack and a running `pac` behind
- Area: 9 (testability), Constraints (writes)
- Finding: `RunGate` runs the check on `Task.Run`, and when `check.Wait(deadline)` times out it returns 2 while the check keeps running. The process then exits, so the `finally` that deletes the temp folder never runs, and the `pac` child may still be writing. Each zip a report names by deploy-hash gets its own unpack with a 120-second timeout, so three such zips can pass the gate's 300-second deadline. The plan says "the temp folder is gone after the run, on the success and every failure path", but its tests don't name this path, or a Ctrl+C during `deploy apply`. The folder left behind is a full unpack of the solution, `.msapp` files included, which cache the environment URL (HANDOFF step 3).
- Suggested fix: Bound the unpacks by the gate's remaining time. For example, pass a deadline into `WarnUnreviewed`, give each `pac` call `min(120 s, time left)`, and fail closed with no new unpack once the time is short. Add a gate test with a short deadline and a `FakePac` that blocks, which asserts that no temp folder is left. If a leftover is accepted instead, write it as a `shortcut:` comment and name it in COMMANDS.
- Resolution: open

#### R3. The plan doesn't say whether the in-repo managed unpack is committed or ignored
- Area: 9 (testability), 2 (hard-coded values)
- Finding: Row 8 puts the reviewed folder in the repository (such as `review/ContosoApp_managed`), because the roles' `input:` must be a path relative to the repo, but it never says whether that folder is committed. Committed: the Action's `path` input defaults to `.` and is searched recursively, so `analyze` and the `tests` input see a second, managed copy of the solution, and the folder carries the same `.msapp` environment-URL caching that HANDOFF step 3 warns about for the unmanaged folder. Not committed: `plan status` on any other checkout warns that the input was not found. HANDOFF step 3's `.gitignore` guidance covers only the two zips and `src/ContosoApp`.
- Suggested fix: In row 8, choose one. A likely choice: the folder is git-ignored; HANDOFF step 3 adds it to the paths to ignore, with the `.msapp` reason; and COMMANDS and HANDOFF say that `plan status` elsewhere warns that the input was not found, which is expected. If it is committed instead, say how the Action's `path` keeps it out of `analyze` and `tests`.
- Resolution: open

#### R4. On the primary platform, and with the Action's `pac`, the route is not checked
- Area: 9 (testability), Assumptions
- Finding: Windows is the primary platform, and the Action checks on an Ubuntu runner with whatever `pac` its earlier steps install. The plan lists cross-OS and cross-version digests as not checked. Its one real-`pac` test skips on Windows, because `Tools.Missing(["pac"], PATH)` checks a bare `pac` with no extension (`Tools.cs:10-13`), so the Windows path of the unpack never runs. When the Action does the check, the recovery in row 8 ("unpack again with ... the `pac` that runs `deploy plan`") can't be followed from a Windows machine, so in that case the route falls back to M75's every time. This is fail-closed, so it is not a release blocker, but Done when 1 to 3 can pass while the main route doesn't work for the primary user.
- Suggested fix: Add a step, or a Done-when line, that runs the managed unpack and `powerops manifest` on the Windows VM from the M63 runbook for one seed zip and compares the digest with Linux's. Or make the hand-over name this as the second trial's first check, with a stated outcome. Have COMMANDS and the Action docs say that the runner's `pac` version must equal the reviewer's for the link to hold.
- Resolution: open

### Low

#### R5. The rows' Where columns miss tests that quote the changed sentences
- Area: 9 (testability)
- Finding: The new "no report records" sentence and the new fault sentence break `NoneRecords` in `DeployGateCommandTests.cs:690`, `DeployApplyCommandTests.cs:1448` and `DeployReviewWarningTests.cs:8`, and the "records another solution-hash" cases in `DeployReviewsTests.cs:32, 43, 56`. Row 2's Where names only `DeployReviews.cs` and `DeployReviewsTests.cs`, and row 3's doesn't name `DeployGateCommandTests.cs`.
- Suggested fix: List those files in rows 2 and 3.
- Resolution: open

#### R6. An unpack that writes no file is taken as a digest
- Area: 9 (testability)
- Finding: A `pac` that exits 0 but writes nothing gives `Manifest.Read` an empty folder, whose digest is the SHA-256 of empty input. The failed `Unmanaged` unpack run in this review left exactly such an empty folder, though with exit 1. A report that claims that digest would link. This is unlikely, but the plan's failure sentences don't cover it.
- Suggested fix: Treat an unpack with 0 files as "the unpacked folder could not be read", and test it with `FakePac`.
- Resolution: open

#### R7. A failed deploy-hash link also hides M75's evidence notes
- Area: 10 (unanswered areas)
- Finding: Row 2 makes `Recorded`, and so `Evidence`, count a report whose `DeployHash` is the zip's. When the provenance check fails (an OS or `pac` mismatch, R4), `deploy plan` no longer prints the diff evidence notes for that zip. That is the case where the person, about to fall back to their own `deploy apply`, needs them most. The plan states the rule but not why.
- Suggested fix: Either let `Evidence` run for a zip whose only record is a deploy-hash link that failed, or record the reason for dropping the notes in decisions.
- Resolution: open

#### R8. The recovery step doesn't say to unpack into a new or emptied folder
- Area: 9 (testability)
- Finding: Row 8's recovery is "unpack again with exactly the command above". Unpacking into the existing review folder without `--allowDelete` keeps files that the new zip doesn't have, so the digest still differs and the check refuses again. The route itself says "a new folder"; the recovery doesn't.
- Suggested fix: In COMMANDS and HANDOFF, say "unpack again into a new or emptied folder".
- Resolution: open

#### R9. `run.sh`'s digest guard doesn't run when `powerops manifest` fails
- Area: 9 (testability)
- Finding: Under `set -euo pipefail`, if `powerops manifest` fails, the assignment `zip_digest="$(... | sed -n ...)"` exits the script with powerops' code before the `error: no digest for the deployed zip` check runs. Step 3's stub check only tests a missing `zip`.
- Suggested fix: Add `|| true` inside the substitution, or `|| zip_digest=""`, so the guard prints its message and exits 3. Add that case to the stub check.
- Resolution: open
