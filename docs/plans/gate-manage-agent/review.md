plan-hash: 72f984d0de8e3d220b363cddb108b384509c3c68c4353b9f1e65c0bfb8fc6ddd
model: claude-sonnet-5-5
fresh-context: yes

Environment facts were not read: the request gave no confirmation that `pac` is signed in, and this plan targets no environment. `powerops plan status` from the installed tool and from the built `powerops.dll` in the worktree agreed (status approved, 0 open questions).

Checklist items 1 to 8 do not apply (no solution, publisher, table, screen, licence or role; the change is one gate route and two doc pages). Item 9 (testability) is met by row 4. Item 11 (size): every row maps to a Done-when line; see R6 for the one row that goes beyond the new route.

How this was checked. The upstream skill and bundle source were read through the GitHub API at the plan's commit and never run. The gate was exercised on two scratch copies outside the worktree, built from `4c9ceee`: one as is, one with the plan's rows 1 to 3 applied. About 100 commands went through `deploy gate` on both (`echo '{...}' | dotnet .../powerops.dll deploy gate /nonexistent/plan.json`). The upstream claims in the plan's Assumptions all held, including the verb list, `parseArgs` (an empty argument does not take the verb's place) and which verbs write to a tenant (only `push` and `publish`). No other file in the upstream repo runs a write through the bundle in a form the plan misses: `run-eval` allows `push` and `pull`, `new-topic` and `validate` run `validate`, and `directline-chat` allows a `detect-mode` verb that the bundle does not have (it would exit "Unknown command").

## Findings

### High

No findings.

### Medium

#### R1. Q3 says a `$(...)` is already safe; it is not, and the tests don't pin it
- Area: 9 (testability), Assumptions
- Finding: decisions Q3 says "A `$(...)` is already safe, because `(` ends the segment". At `4c9ceee` that is false inside double quotes. `node <path>/solution-settings.js template --env "$(pac solution import ...)"` exits 0, and so do the same word with `pac copilot push` and `pac pages upload`. Only an unquoted `$(...)` is split off and read. The fix (Done-when 4) does close the quoted form (exit 2 in the scratch copy), but row 4 lists only the backtick form, so nothing pins it.
- Suggested fix: correct the Q3 sentence and the Assumptions bullet, and add to row 4 the double-quoted `$(...)` word after `template` and after a read-only verb, each expecting the can't-read line.
- Resolution: fixed. Q3 and the Assumptions bullet now say a double-quoted `$(...)` stays in the word; row 4 and `ImportCallTests` pin it for `template` and for `pull`, and Done-when 4 names it.

#### R2. Done-when 3 promises more than the marker can deliver for the script name
- Area: 9 (testability)
- Finding: Done-when 3 says any other form blocks as unreadable and lists "a variable, a wildcard". That holds for the verb word (`"$V"` and `p?sh` both exit 2). It does not hold for the script's own name: `node <path>/manage-agent.bundl?.js push`, `manage-agent.bundl[e].js push` and `node "$S" push` all exit 0, before and after the change, because `FileName` and the `manage-agent\.bundle` marker match the literal text only. This is the same deny-list miss the docs already accept for `$PA push` (decision 41). A test written from Done-when 3 as worded ("a wildcard") would fail, and rows 5 and 6 say nothing about it.
- Suggested fix: word Done-when 3 as "once the script name is literal", add the glob and variable script-name forms to the known-misses theory (`ImportCallTests.cs:90-123`, the `$PA push` group), and have the HANDOFF bullet at line 102 (which already lists variable-built commands) say the new route reads the literal name too.
- Resolution: fixed. Done-when 3 reads "once the bundle's file name is written out", Won't build names the miss, row 4 and the NotImport theory pin `node "$S" push` and `bundl?.js` as passing, and the HANDOFF variables bullet names them.

### Low

#### R3. A marker-like word in an option value now blocks read-only calls
- Area: 9 (testability), Users
- Finding: with the `clean` condition, any marker text in a word after the script blocks the call, for example `list-agents --environment-name "my copilot push env"` or `... "Solution Import"`. It exits 2 today as well, since nothing recognizes the call; the plan keeps that, and it fails closed. But for `solution-settings.js template` it turns a pass into a block, and the plan's Users section says the reads see "no change".
- Suggested fix: in Users and in the HANDOFF mention sentence, say that a read-only call blocks when an option value spells a marker such as `copilot push`, and that the user rewords it.
- Resolution: fixed in the plan. Users now says an option value spelling a marker blocks, and that `template` changes from pass to block. HANDOFF's mention sentence was left as it is; it already says a mention of an import is blocked and now also names `manage-agent.bundle`, which covers it.

#### R4. `run-eval` also pushes; Users and the docs don't say so
- Area: 5 (route), docs rows 5 and 6
- Finding: upstream `run-eval` allows `manage-agent.bundle.js push *` and its loop ends with "offer to push and re-run". With the hooks on, that step now blocks too. The plan's Users bullet names only "a skill's push or publish". Separately, `directline-chat` names a `detect-mode` verb that the bundle lacks; it would block as unreadable and fail upstream anyway (no action needed, only worth one line in decisions).
- Suggested fix: name `run-eval`'s push in Users, so the HANDOFF line tells the user the evaluation loop needs the same yes.
- Resolution: fixed. Users and the HANDOFF list name `run-eval`'s push; the `detect-mode` note is in decisions.

#### R5. Row 6 is thinner than row 5
- Area: docs
- Finding: row 5 changes three HANDOFF places (the list, the mention sentence, the "doesn't see" bullet). Row 6 only says the list gains the route and the agents sentence changes. The COMMANDS.md paragraph at line 105 also has the mention sentence (`grep "pac solution import" docs`, `git commit -m "... pa app push"`), and nothing in row 6 adds `manage-agent.bundle` there or says the seven read verbs pass. No doc test pins any of this (`HandoffDocTests` and `DocsDriftTests` do not read the route list), so a missed sentence would ship unnoticed.
- Suggested fix: spell out row 6 as three edits (route in the list, `manage-agent.bundle` in the mention examples, one clause that the read-only verbs pass), matching row 5.
- Resolution: fixed. Row 6 lists three edits and COMMANDS.md has the route with the read-only clause, the `ls scripts/manage-agent.bundle.js` mention example and the trimmed agents sentence.

#### R6. The `template` fix is beyond the new route
- Area: 11 (size)
- Finding: Done-when 4's second half (`solution-settings.js template` must hold no other marker) is not needed by the manage-agent route; it closes an existing hole in the same branch with one condition and is cheap. It also changes an existing pass into a block, under a `feat:` PR.
- Suggested fix: keep it (Q3 reasons it well), but put it in its own commit, or say it in the PR title line, so a revert of the route does not reopen the hole.
- Resolution: accepted. Decisions Round 2 keeps the condition in this PR (squash merge would lose a separate commit) and has the PR body name it. Reasonable.

#### R7. Code and tests already exist in the worktree before sign-off
- Area: 10 (process)
- Finding: `git status` shows uncommitted edits to `ImportCall.cs`, `ImportCallTests.cs` and `DeployGateCommandTests.cs`, but Step 1 puts review, fixes and sign-off before Step 2. This review judged the plan against `4c9ceee` and the rows as written; the edit to `ImportCall.cs` matches rows 1 to 3 and behaved as the plan says on every row-4 case I ran. Fixes from R1 and R2 change the tests, so the existing work needs those added.
- Suggested fix: apply R1 and R2 to the tests before sign-off, or note in the log that Step 2 started early.
- Resolution: accepted. The decisions Log records that Step 2 started before sign-off and which tests were added after the code.

## Second pass

Checked against the working tree on the new hash. I copied the tree (without `.git`) to a scratch folder outside the repo and ran there: `dotnet test` for `PowerOps.Core.Tests` (2178 passed, 2 skipped, 0 failed) and `PowerOps.Cli.Tests` (1214 passed, 3 skipped, 0 failed), and `dotnet format --verify-no-changes` (clean). I read the diffs of `ImportCall.cs`, both test files, `HANDOFF.md` and `COMMANDS.md` against rows 1 to 6; they match the plan. The first-pass gate probes (about 100 commands) still stand, since `ImportCall.cs` is unchanged since then.

No new findings. Open: 0 High, 0 Medium, 0 Low. Two findings (R6, R7) are accepted, not fixed. Nothing was run against a tenant, and `plan.md` was not touched in this pass.
