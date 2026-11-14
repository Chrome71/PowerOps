# Decisions: m115-own-imports

Q1 is the owner's. Q2 to Q6 were decided by the building session (2026-11-14) inside Q1 and the brief, under the owner's direction for this work to decide routine choices and log them ("as autonomous as possible", 2026-11-11, in `reports/trial-3/chain/chain-rules.md`); none needed the owner.

## Round 1 (planning)

### Q1. Does `settings apply --apply` leave the agent's deployment yes, and does the gate block PowerOps's own unguarded imports?
(a) yes: the yes text drops `settings apply --apply`, and a gate rule blocks an agent's `settings apply --apply` always and `deploy apply --apply` without `--require-reviews`; (b) give `settings apply` its own `--require-reviews`; (c) keep.

Decided: (a), by the owner. Asked in chat "Take the recommended option on all eight decisions?" of the review-enforcement audit's decision packet (`reports/review-enforcement-audit-2026-11-13/decision-packet.md`, git-ignored), the owner answered "All recommended" (2026-11-13); decision 3(a) there is this milestone. Asked "Start M115 and M96 now, since usage has dropped to 26%?", the owner answered "Start both" (2026-11-14).

### Q2. What exactly does the yes text say?
(a) the agent never runs `settings apply --apply`, with the reason (it checks no review) and that it is the person's own command; the yes covers the import with `deploy apply` and the plan's named steps; every agent `deploy apply --apply` names `--require-reviews` (plan Data row 5); (b) drop the words only, with no reason.

Decided: (a). A reader who meets "the person's own command" with no reason would ask why; one clause says it. Decision 16(c) keeps `settings apply` for a person outside a gated flow, word for word, and the new decision 71 points to it.

### Q3. How are forms the gate doesn't read handled: `dotnet`, `env`, `sh -c`, variables, PowerShell, mentions?
(a) one alternative in `Markers()`, so they block as unreadable exactly as a pac import marker does today, dry runs and mentions included; (b) read `dotnet` forms as calls too; (c) a narrower marker that needs `--apply` in the same text.

Decided: (a). No existing route reads `dotnet`, and the brief asks for the spellings the existing routes cover. (c) is weaker than (a) on the same forms (`--app""ly` inside `sh -c` hides its `--apply`), and the existing marker rule already blocks a mention such as `grep "pac solution import" docs` and tells the agent to reword. (a) is not complete either: a quote inside a nested string (`bash -c 'powerops settings ap""ply ...'`), a variable in place of a word, a shell function or a script file passes, as the same forms of a pac import pass today (`docs/HANDOFF.md:156`); the docs say so and one test pins the nested-quote miss (review R5, Fable F4). The cost: dry runs that aren't plain literal commands block, and so do mentions such as `terraform -chdir=deploy apply`; the docs say so. PowerShell keeps its branch: any import in PowerShell is blocked today, and `deploy apply` there is blocked the same way, its guarded and dry-run forms too; the docs send the agent to the MCP tool or a Bash command.

### Q4. Does the `powerops-bi` specialist's "not applicable" line change with the implement skill's (C-2)?
(a) yes, the same words, one line and its generated copy; (b) only the implement skill.

Decided: (a). The specialist gives the same instruction for the same checks; leaving it would contradict the skill it hands to.

### Q5. When does a `deploy apply ... --apply` count as guarded?
(a) read every word strictly, as decision 41's `ReadCli` reads a pac import: only the options the command defines, each once, no value after a switch, no option word as a value, no redirection but `2>&1` or `>&2`, no comment, every word literal; anything else is unreadable (plan Data row 3 (vi)); (b) the exact word `--require-reviews` present, not followed by `true` or `false` (revision 1).

Decided: (a). Both plan reviews broke (b) (`review.md` R1 to R3, Fable F1 and F2): the parser pools a repeated option, trims `" false"`, lets `--out` take `--require-reviews` as its value, and the shell drops a comment or a redirection's target that (b) read as the guard. (a) relies on none of the parser's value rules, and a Cli test keeps the gate's option tables equal to the commands', so a new option can't open a pass.

### Q6. What guidance does an unreadable `powerops` call get?
(a) the reading guidance gains one sentence: write a `settings apply` or `deploy apply` call as one plain `powerops` command in Bash with literal words, and an agent adds `--apply` only to `deploy apply`, with `--require-reviews`, after the user's yes (plan Data row 2); (b) unchanged, which tells the agent only to write a plain `pac solution import`.

Decided: (a). Without it an agent blocked on `dotnet run -- deploy apply ...` is pointed at a pac import, which the implement skill forbids. Revision 1's clause told a blocked dry run to run the import itself and didn't say "in Bash" (Fable F3, review R7); revision 2 keeps the yes and names Bash.

## Log

- 2026-11-14: started by the M115 milestone session (Opus 5.5 orchestrating; the user's routing names Fable as orchestrator, so the session said so and kept the Fable plan and work reviews) in worktree branch `claude/m115-own-imports-9b5a50`, reset to `origin/main` `a442b17`. Depth `standard`.
- read on 2026-11-14: `AGENTS.md`; `CLAUDE.md`; `reports/trial-3/chain/chain-rules.md` (whole); `m112-done-handover.md`; `m114-done-handover.md`; the decision packet's first paragraphs, B-1, B-2, B-4, C-2, sections 4, 5 (M115) and 8; `evidence/repro/D/gate-routes.txt`; `evidence/lanes/D.md` (D-1, D-2); `docs/ecosystem/compatibility.md` (Claude Code and Codex rows) and the open entries of `docs/ecosystem/changes.md` (ECO-0011 is the one that names both clients; it doesn't touch the hook input); `src/PowerOps.Core/Deploy/ImportCall.cs`; `src/PowerOps.Cli/DeployCommand.cs` (the gate); the docs lines of plan Data rows 5 and 6; `docs/plans/m85-gate-routes/` and `docs/plans/m114-review-check/` for the format. The shared notes folder held no sibling notes.
- 2026-11-14: parser probe (plan Assumptions) on the worktree build with `PATH=/usr/bin:/bin`, no `--apply`, a plan file that does not exist: `--require-reviews false` and `--require-reviews:false` parse; `-- --require-reviews` is refused as an unrecognized argument.
- 2026-11-14: no Microsoft claim; no `powerops-researcher` pass (chain rule step 4).
- 2026-11-14: plan review round 1. Fresh-context `powerops-review-plan` (Opus, `review.md`): 4 High, 2 Medium, 2 Low (R1 to R8). Fable plan review (`fable-reviewer`, Fable): REJECTED with 2 High, 2 Medium (F1 to F4), overlapping R1 to R5 and R7. Applied in `plan.md` revision 2: Data row 3 reads a `settings apply` or `deploy apply` call strictly (R1 to R4, F1, F2), row 2's guidance sentence keeps the yes and names Bash (F3, R7), row 6 and decision 71 say what is blocked, what the gate misses and what it costs (R5, F4), the script paths and line numbers (R8); Q3, Q5 and Q6 rewritten. R6 (the `node` and `pa` branches' program word) deferred under Won't build to M108 by the building session.
- 2026-11-14: plan review round 2, fresh `powerops-review-plan` second pass: R1 to R8 hold, four new (R9 and R10 High, R11 Medium, R12 Low), all applied in revision 3: the dry-run test treats a redirection word with text before its operator as an argument (R9); the strict read removes `2>&1` and `>&2` before pairing options (R10); no home path in steps 5 and 6 (R11); row 6 (c) names a shell function and decision 71 names the misses (R12).
- 2026-11-14: plan review round 3, fresh `powerops-review-plan` third pass: R9 to R12 hold; one new Low, R13 (an attached redirection target with a backtick in a dry run), fixed in revision 4 by a character check on the target.
- 2026-11-14: plan review round 4. Fable plan review: APPROVED revision 4 (body `6642bb3d…`; steps 2 to 4 Opus, one agent in order; step 5's command runs Haiku; WATCH notes kept for the work review and carried into the executor's brief: the option tables may land in step 2, `--apply` "first" means first among the options, the table test compares kind as well as name, and rule (i)'s cost to a non-literal program word goes in the report). Fresh `powerops-review-plan` fourth pass: R13 fixed, R1 to R12 hold, nothing new; no finding open.
- 2026-11-14: signed off with `powerops plan sign-off` (worktree build) by the building session, on the owner's answers quoted in `reports/trial-3/chain/chain-rules.md`, last section: "All recommended" (to "Take the recommended option on all eight decisions?", 2026-11-13), "Yes, same as the chains" (to whether milestone sessions sign off their own plans after clean reviews and merge their own green pull requests, 2026-11-13), and "Start both" (to "Start M115 and M96 now, since usage has dropped to 26%?", 2026-11-14).
