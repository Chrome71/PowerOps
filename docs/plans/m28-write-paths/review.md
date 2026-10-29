plan-hash: 1a2f4515500349139027d6fec306d40f50ffafa81fb19a19d5ff20e09b2f46cf
model: claude-sonnet-5-5
fresh-context: yes

Two fresh reviews (powerops-reviewer, Sonnet, each in a context without the planning conversation). The plan-hash was updated after the text fixes below (the practice of the M7 to M32 reviews). First review: hash 2cf640ba64be9a5e10d2c9a7bbe78e96fe4903afa769b329981806e7036b3e5d, 1 High, 5 Medium, 5 Low (R1 to R11). Its High was a design flaw, so after the fixes, and after the coordinating session made the refusal one up-front rule for every command, a second fresh review checked the revised helper: hash 0218588565c7eb8af0bcf677d80ad6235da101de4f751d6f8df1232ea447791e, 0 High, 5 Medium, 6 Low (R12 to R22). The coordinating session relayed each review's findings to the planning agent, which fixed the plan, and wrote this file from the reviewers' reports with the resolutions. Both reviewers ran throwaway probes on Linux outside the repo; the second replayed the first High and every planted link it could make (to `/proc/self/root`, `/proc/self/cwd`, `/dev/shm`, `/dev/null`, `/dev/stdout`, `/dev/fd/1`, a folder, dangling, a loop) against a reimplementation of the revised design, and all were refused. Neither contacted a tenant.

Most of the Power Platform checklist does not apply: there is no solution, publisher, connector, environment variable, licence question, DLP policy, route, managed state, volume or security role (items 1 to 8). Items 9 (testability) and 10 (open questions) apply. Open questions: 3, each answered by the PR's CI on Windows or macOS. No URL that is not a public docs or GitHub page (each carries the `plan status` allow marker), GUID, email or person's name in the plan or `decisions.md`.

## Findings

### High

#### R1. The `/dev/` and `/proc/` path rule let a planted link through
- Area: security (Automation, the helper; Assumptions)
- Finding: The draft wrote a target in place when its resolved path started with `/dev/` or `/proc/`. A link to `/proc/self/root<abs path>`, `/proc/self/cwd/...` or `/dev/shm/x` resolves to such a string, and `File.WriteAllText` through the first overwrote the file behind it (reproduced). A pull request could add `report.json` as such a link and `analyze --out report.json` in CI would write outside the clone, which is what R-9 is about. A link to a FIFO outside `/dev` was also neither refused nor written correctly.
- Suggested fix: Do not trust the resolved name; treat a link as special only for known names, or probe through the link itself, and add refused tests for the three planted links.
- Resolution: fixed (the path rule is gone; every link at the target is refused before any open, except the typed names (R12); an existing target that is not a link is probed once on its own handle; the three planted links are rows of the link theory, each refused)

### Medium

#### R2. The FIFO probe opened the pipe twice
- Area: correctness (Automation, `WriteText`)
- Finding: The probe opened the FIFO for write and closed it, then the write opened it again. A reader (`cat f > out`) saw end of file after the first close, and the second open blocked.
- Suggested fix: Open once; probe and write on the same handle.
- Resolution: fixed (one open; a device or pipe is written in place on the probed handle)

#### R3. `--out /dev/stdout` with stdout redirected to a regular file
- Area: compatibility (Compatibility; Assumptions; tests)
- Finding: `/dev/stdout` resolves to the redirected file, a regular file, so the draft refused it on Linux, against the done-when; the macOS text disagreed with the rule.
- Suggested fix: Allow the process's own stdout by exact name, or document the refusal; add a child-process test with stdout redirected to a file.
- Resolution: fixed (typed names are written in place as today; the child-process tests run with a pipe and with a redirected file)

#### R4. The write-site scan was narrower than the recount
- Area: testability (Tests, `WriteSitesTests`)
- Finding: The scan's list missed forms the recount covered, and exempted `McpCommand.cs` as a whole.
- Suggested fix: Use the recount's pattern minus the reads, and narrow the exemption.
- Resolution: fixed, then cut under R20 (one tree scan of the three forms that write, with `SafeWrite.cs` and the `CreateNew` line of `TrySave` exempt)

#### R5. Windows devices were dismissed without a source
- Area: compatibility (Automation, Windows)
- Finding: `--out NUL` works today; decision 35 is not limited to Unix; the draft would make a temp file and move it onto `NUL`, untested.
- Suggested fix: Write reserved names in place, or record the regression.
- Resolution: fixed (the same probe applies on Windows, where the runtime source shows `SetLength` throws `NotSupportedException` for a handle that is not a disk file; a Windows-only `--out NUL` test checks it in CI; an open question until that run)

#### R6. The order of checks in `skills install` was unspecified
- Area: correctness (exit codes; where each command checks)
- Finding: `Install` reads and classifies files through links before any link check, so a linked folder showed as "directory" or "conflict" (exit 1) and was read first; whether unchanged files are refused was open.
- Suggested fix: Run `Refusal` on each file first and decide the unchanged case.
- Resolution: fixed (`Refusal` runs on every file in the install plan, created, changed or unchanged, before any read; a linked `.agents/skills -> .claude/skills` layout is refused, with its own test row)

### Low

#### R7. The race claim was too broad
- Area: accuracy (Automation, the race paragraph)
- Finding: "Nothing is written through a link even if one is swapped in after the check" holds only for the last name; a folder swapped for a link after the re-check is followed.
- Suggested fix: Say so and name the residual.
- Resolution: fixed (reworded again under R22)

#### R8. `drift --save` is not checked before any work
- Area: accuracy (exit codes)
- Finding: Its file names come from the live reads; `--out` over one of its own `--save` files was unstated.
- Suggested fix: Reword and state the residual.
- Resolution: fixed

#### R9. Message hygiene
- Area: correctness (exit codes and messages)
- Finding: Refusal lines must go through `TextReport.Clean`; a failed write would name the temp file.
- Suggested fix: Say both, and test them.
- Resolution: fixed (a hygiene test; the rewrite of the temp path is R16)

#### R10. Wording slips
- Area: accuracy (Done when; Steps; Constraints)
- Finding: `plan sign-off` through a link stays exit 3, not 2; the Step 1 device check; two places for the M24 rebase; the recount base.
- Suggested fix: Fix each.
- Resolution: fixed

#### R11. The input check was optimistic
- Area: scope (the helper, the input check)
- Finding: A per-folder link walk with a hop limit was more than "about 15 lines" and still missed 8.3 names, `\\?\` paths and Unicode normalization.
- Suggested fix: Full paths plus resolving the last name's link, labelled a guard for common spellings.
- Resolution: fixed (the `Links.Hard` helper and its test went with it)

#### R12. The typed-name exemption was wider than asked and skipped every guard
- Area: security (the helper, step 1; Done when)
- Finding: `/dev/fd/3` and `/proc/self/fd/3` with `3<in.zip` truncated and rewrote the input; only `/dev/null` and `/dev/stdout` are in the spec, and a regex match has edge cases.
- Suggested fix: Keep `/dev/stdout` (and `/dev/stderr`), matched with an ordinal string comparison; drop the fd forms.
- Resolution: fixed (Medium in the second review; only `/dev/stdout` and `/dev/stderr`, by `string.Equals(..., Ordinal)`; the fd forms are links and refused)

#### R13. The plan narrows the fixed spec without saying where
- Area: scope (Compatibility; decisions Q1; Steps 6)
- Finding: Decision 35, the M28 row and the Phase 6 plan's R-9 and Q8 say a target that resolves to a device or a pipe is written in place; the plan refuses every link, so `ln -s /dev/null out.json` exits 2, and Step 6 did not update the spec text.
- Suggested fix: Get the coordinator's explicit decision, and edit the spec text and `COMMANDS.md`.
- Resolution: fixed (Medium in the second review; the coordinating session kept the narrowing: the up-front check must not open the target, and a link's target name proves nothing. Step 6 edits BUILD-PLAN decision 35 and the M28 row and lists it under "Accepted:"; `COMMANDS.md` says to name the device itself; the signed-off Phase 6 plan is not edited, and the master session is told so it can raise it with the user)

#### R14. The temp file was created wider than the target
- Area: security (the helper, the temp file)
- Finding: A temp file made with `CreateNew` under a `022` umask is `0644` while written, so a `0600` target's new contents were readable before the mode was copied.
- Suggested fix: Create it with `UnixCreateMode` set to the target's mode, keep `SetUnixFileMode` for the umask, and test the temp's mode.
- Resolution: fixed (Medium in the second review; a test checks the temp's mode in the `beforeMove` hook)

#### R15. The probe's classification and the untried systems were under-specified
- Area: correctness (Data, the probe; the helper; Open questions)
- Finding: An exception from the open itself (a loop, a sharing violation) must not count as a device; a device on which `SetLength` succeeds would get a temp file moved over its node, which as root destroys it; macOS `/dev/null` was not an open question.
- Suggested fix: Classify only `SetLength`'s exception, guard the replace branch for a path directly under `/dev/`, and add the macOS cases to the open questions.
- Resolution: fixed (Medium in the second review)

#### R16. Errors from a failed temp file named the temp, and the probe touches the mtime
- Area: correctness (the helper; tests)
- Finding: A read-only folder gave an error naming `.powerops-<hex>.tmp`; only the move was tested; the same-length `SetLength` changed the old file's modification time.
- Suggested fix: Describe the catch and rethrow and test a read-only folder; say what "unchanged" means.
- Resolution: fixed (Medium in the second review; the rethrow keeps the base type and puts the target's path in the message; "unchanged" means the bytes and the mode)

#### R17. The `finally` could delete a file this call did not create
- Area: correctness (the helper, the temp file)
- Finding: If `CreateNew` threw because the name existed, the `finally` still deleted it.
- Suggested fix: Create the stream before the `try`.
- Resolution: fixed

#### R18. A bind-mounted file and a sticky folder now fail
- Area: compatibility (Assumptions; Compatibility)
- Finding: A single file bind-mounted as `--out` fails the rename with EBUSY (reproduced), and a rename over another user's file in `/tmp` is refused; neither was in the docs.
- Suggested fix: List them in `COMMANDS.md`, or fall back to the probed handle.
- Resolution: fixed (listed as known limits in Compatibility and `COMMANDS.md`, with no fallback)

#### R19. `Refusal` threw on an input that is a link loop
- Area: correctness (the helper, the input check)
- Finding: `ResolveLinkTarget(true)` throws `IOException` on a loop, so `Refusal` exited 3 and printed a path.
- Suggested fix: Count it as no match, and test it.
- Resolution: fixed

#### R20. The write-site scan was bigger than the spec
- Area: scope (Tests, `WriteSitesTests`)
- Finding: A deny-list of about 25 forms with 9 planted-form tests and an "exactly one `File.Delete`" rule would break on benign refactors; the spec asks for no scan.
- Suggested fix: One tree scan of the forms that write, about 2 tests.
- Resolution: fixed (2 tests; Done when names the three forms)

#### R21. Tests missing for the plan's own claims
- Area: testability (Tests)
- Finding: No test that a link swapped in after `Refusal` is replaced and not followed, that a race at the write exits 3, that `/dev/./stdout` is not exempt, or of a relative `root` for `skills install`.
- Suggested fix: Add them and redo the counts.
- Resolution: fixed (the counts total +72)

#### R22. The race sentence was false for the in-place branch
- Area: accuracy (the helper, the race paragraph)
- Finding: In the in-place branch the open follows a link swapped in between the re-check and the open.
- Suggested fix: Reword it.
- Resolution: fixed (named as a residual: .NET has no `O_NOFOLLOW`)
