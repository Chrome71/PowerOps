# Decisions: phase6-hardening

## Round 1

### Q1. How is Phase 6 split?
1. By risk, one PR per milestone, with the cheapest high-value fixes first: quick security fixes, repo settings, `deploy_apply` and MCP inputs, reader limits, other input limits, exit codes, write paths, supply chain, output hygiene, docs, platforms (recommended)
2. One milestone per audit area (readers, MCP, subprocesses, Action, supply chain, docs), in the order the audit ran
3. In your own words
Answer: By risk, one PR per milestone, with the cheapest high-value fixes first. The two high code findings (S-1, I-1) and D-2 each take under an hour and close a path to running code or importing without the user's intent, so they go first as M22. The repo settings (M23) are the user's toggles and don't wait on code. `deploy_apply` comes before the resource limits because it is the only path that writes to a tenant. Splitting by audit area would put the cwd hijack (S-1) and the CI-breaking advisory build (A-3) behind low findings of the same area.

### Q2. Does each milestone get its own plan before it is built?
1. Only the milestones with a design choice left (M24, M28, M32) get a quick plan of their own; the rest are built from this plan's rows, which give the place and the fix (recommended)
2. Every milestone gets its own plan, as in Phases 3 to 5
3. In your own words
Answer: Only M24, M28 and M32 get a quick plan of their own. The other rows name the file, the line and the fix, so a second plan would repeat them, and the FOUNDATION promise of quick iteration without lowering the bar is kept by the fresh review of this plan and the PR review of each milestone. M24 has to choose how the approved hash reaches the import and depends on S-3's research and on M21; M28's helper touches twelve write sites with different overwrite rules; M32's runbook depends on which machine exists.

### Q3. Which release labels?
1. Patch labels after M21's v0.20.0 (v0.20.1 to v0.20.7) for the code milestones, and none for the settings, CI and docs milestones (recommended)
2. Minor labels, v0.21.0 onward, one per milestone
3. In your own words
Answer: Patch labels after M21's v0.20.0 for the code milestones, and none for the settings, CI and docs milestones. Every code milestone is a `fix:`, which release-please releases as a patch; decision 29 already says the labels order milestones and don't fix version numbers. M23 has no commit, and M29, M31 and M32 change only CI and docs.

## Round 2

### Q4. Fuzz or property tests on the readers and the expression parser?
1. No fuzzing library: a table-driven theory of the hostile inputs per area, and one seeded mutation test over the fixtures in xunit, with no dependency (recommended)
2. Property tests with CsCheck (Apache-2.0) on the parser and the readers
3. A coverage-guided fuzzer (SharpFuzz with AFL)
Answer: No fuzzing library: a table-driven theory of the hostile inputs per area, and one seeded mutation test over the fixtures in xunit. About 10,600 seeded byte mutations of the fixtures and 1,700 structural mutants and random expressions found nothing; every gap was found by hand-written probes (deep paths, quadratic copies, lone surrogates, duplicates). The probes become the theory. The mutation test adds the structural swaps that byte flips rarely produce. CsCheck isn't needed for that, FsCheck is BSD-3-Clause, and SharpFuzz needs instrumented assemblies and runs on Linux only.

### Q5. What does PowerOps do with a `pac` older than 2.13.1?
1. No runtime check: the docs keep the tested versions (2.13.1, and 2.14.1 in the examples), and FOUNDATION §8 records what Learn and NuGet do and don't date (recommended)
2. Warn when the bare `pac` run's version line shows a version below 2.13.1
3. Refuse below 2.13.1
Answer: No runtime check. Learn and the NuGet notes date only `--stage-and-upgrade` (1.28) and `env fetch` (1.21.13); `--settings-file`, `solution list`, `org who` and `list-cloud-flows` are undated, so no documented minimum exists to check against. The version line is documented by one example with no promise of format, and reading it costs one more `pac` start per command, which is slow. A warning or a refusal would be a new behaviour built on an undocumented format.

### Q6. What happens to a zip that can't be opened, found while scanning a folder?
1. Exit 3 and name it, as for any unreadable input; a readable zip with no solution in it is still skipped (recommended)
2. A note on stderr, with the exit code unchanged
3. In your own words
Answer: Exit 3 and name it, but only for a file that starts with the zip signature (`PK`). AGENTS.md says unreadable input is exit 3, and the same zip named directly already exits 3. A note would leave CI green on an export it never read, which is the false negative C-4 found. A readable zip that holds no solution is still skipped, and so is a `*.zip` without the signature (a Git LFS pointer, an empty placeholder), so other zips in a repo don't fail the run. Amended after the review (R1): it ships with C-3 in M27, because `diff` and `test` call the folder scan outside their `try`, where an exception exits 1.

## Round 3

### Q7. How is the gap between the user's yes and the import (S-2) closed?
1. The import gets the hash the user approved, inside the process, and refuses when the re-plan's hash differs; no new CLI option (recommended)
2. Copy the deploy folder to a private temp folder before the form, and plan and import from the copy
3. Accept it and document it
Answer: The import gets the hash the user approved, inside the process, and refuses when the re-plan's hash differs. It closes the swap of a whole consistent plan with a few lines and no option an agent could pass. A private copy would also close the zip swap, but it copies up to the deploy caps per call and changes which paths `pac` sees; that residual stays accepted in the plan. M24's own plan settles the shape.

### Q8. What does a write do when its target is a link, or an existing file?
1. Refuse a link at the target (and, for `skills install`, at a folder on the way), refuse a target that is one of the command's inputs, write a temp file and move it over the target, and keep replacing a command's own output file as today (recommended)
2. Refuse to overwrite any existing file without `--force`
3. In your own words
Answer: Refuse a link at the target (and, for `skills install`, at a folder on the way), refuse a target that is one of the command's inputs, write a temp file and move it over the target, and keep replacing a command's own output file. It closes the observed cases (a write through a link, the input zip overwritten, `skills install` outside the repo, a cut-off plan after a crash). Option 2 would change every `--out` and `baseline` run, which users rerun on purpose, and add a flag to commands that don't have one. Amended after the review (R5): a target that resolves to a device or a pipe (`/dev/null`, `/dev/stdout`) is written in place as today; the move keeps an existing file's Unix mode; and the CLI's `deploy plan --save` keeps overwriting, as it does today, while only the MCP `save` refuses an existing file.

### Q9. How are the repo settings changed?
1. A milestone of the user's yes: the master session asks, and makes each change only after that yes; SECURITY.md waits on the answer (recommended)
2. Change only SECURITY.md to drop private vulnerability reporting, and leave the settings
3. In your own words
Answer: A milestone of the user's yes. The settings change the public repo's behaviour for everyone, so they are the user's call, not a code change. Option 2 leaves no private route at all: a committed contact address would break the rule against emails in the repo.

## Round 4

### Q10. How is the Windows and macOS gap covered?
1. CI covers what a runner can (the resolver, a fake `pac.cmd` and `az.cmd` on Windows, the scripts under bash 3.2 on macOS); a manual runbook covers the clients and `pac` sign-in, run when the user has a machine, otherwise marked not verified (recommended)
2. Block Phase 6 until the clients have been run by hand on both systems
3. In your own words
Answer: CI covers what a runner can, and a manual runbook covers the clients and `pac` sign-in. A hosted runner can't sign in to the clients or `pac` without stored secrets, which FOUNDATION §4 rules out. The rest of Phase 6 doesn't depend on the manual run, so it doesn't block.

## Round 5

### Q11. What is S-3's fix if Learn doesn't say whether Dataverse refuses a token issued for another org?
1. Keep the host suffix check, record the answer, and list the residual under Accepted and in the README's MCP section; ask the user only if Learn says such a token works elsewhere (recommended)
2. The MCP tools take only environments that `pac auth list` or `pac env list` shows
3. In your own words
Answer: Keep the host suffix check, record the answer, and list the residual. Option 2 narrows the tools' documented inputs, against the plan's goal, and needs a runtime parse of `pac` text that has no documented format. The token is requested for the named host and the tools already send it only to documented Dataverse hosts. Asked after the review (R7).

### Q12. What happens to AGENTS.md's rule that undocumented `pac` output goes through the Web API (D-9)?
1. Name, in the rule, the reads the code already parses and the decisions that chose them (decision 15 for `pac org who`, the M3 and M7 plans), and let the user overrule it (recommended)
2. Keep the rule as written, and move those reads to the Web API in a later phase
3. In your own words
Answer: Name, in the rule, the reads the code already parses and the decisions that chose them. The rule came in with M8 (`f428e87`), in the same change that parses `pac org who` under decision 15, and `plan facts` parsed `pac` text from M3 (`170ae2e`) on. So the wording is narrower than what its own change did, and this is a wording fix, not a new policy. Moving the reads would be new work outside a hardening phase. It is also under "Questions for your admin", so the user can overrule it. Asked after the review (R6).

## Log

- 2026-10-28: plan started at the master session's request. The user asked for a phase that refines and secures what exists, with no new feature. Out of scope: M17 (on hold), M21 (another session), NuGet publishing, the rename
- 2026-10-28: the audit: six read-only agents (Sonnet) on `b4abce4`: readers and file inputs, MCP inputs and subprocesses (`powerops-reviewer`), the Action and supply chain, correctness and exit codes, docs drift, and the open research items (`powerops-researcher`). The readers and correctness agents probed the built CLI with crafted inputs in a scratch folder outside the repo.
- 2026-10-28: this session re-ran S-1 (a fake `pac` in the current folder ran before the one on PATH), R-1 (192 KB, 2,085 MB peak), R-5 (exit 1 with an unhandled exception), C-4 (exit 0) and C-7 (a response file read), with offline probes and fake executables only, and read D-2 and S-2 in the code. It also found that the correctness agent's claim of no SARIF schema test was wrong (`SarifReportTests` validates against SARIF 2.1.0)
- 2026-10-28: two audit agents each ran one read-only `pac org who` by mistake: the MCP agent with `drift --env /h`, and the correctness agent with `drift --env bad/emptydir`, a value the CLI takes as a live environment (that is finding C-9). Both failed with exit 1. No `az` call, no other `pac` call and no write to a tenant were made
- 2026-10-28: not run: two further audit skills. The agents' briefs covered their checklists, and running them in this session would have repeated the audit
- 2026-10-28: the planning session answered Q1 to Q10 itself, under the user's delegation of 2026-10-22 (agents plan and sign off milestones), as the master session's brief asked
- depth: quick -> standard (a phase plan over eleven milestones, with four rounds)
- approved without: users (from FOUNDATION §2 and the audit, not a question round)
- approved (2026-10-28)
- fresh review (powerops-reviewer, Sonnet) 2026-10-28: 0 High, 11 Medium, 6 Low; all fixed in the plan text, `decisions.md` (Q11, Q12, and Q6 and Q8 amended) and the BUILD-PLAN section. The reviewer re-ran C-7, C-4 and R-5 offline and confirmed them, and found three wrong line references (R14), each checked again by this session in the code
- signed off 2026-10-28 by this session under the user's delegation of 2026-10-22: after the fixes no High or Medium finding is open, which is the condition the user set for agents signing off plans themselves
- 2026-10-28: M25 review fix: the node cap is 15 million, not the 5 million the plan names. Whitespace and end-element nodes count and the fixtures average 17.8 bytes per node, so 5 million is about 90 MB of XML, below the 256 MB byte cap, and a large real solution that read fine before could have exited 3; 256 MB / 17.8 is about 14.4 million, so nothing under the byte cap at fixture density reaches 15 million, and memory stays near 1.2 GB at about 78 bytes per node. Attributes now count as nodes too.
- 2026-11-01: personal and tenant facts removed from the plan body, with no decision changed; re-signed with `powerops plan sign-off`, and `review.md`'s `plan-hash` moved to the new body
