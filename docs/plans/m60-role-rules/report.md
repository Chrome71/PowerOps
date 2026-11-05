# M60 report: the rerun of both review roles on the new baselines

Evaluated revision: `bdb3a2e5da4d05e95bd14f73ff2e8002e84a46fd`, the freeze commit of step 8. The skill text and `run.sh` there are the ones M55 measured and pinned. Their blob ids are unchanged:

| File at `bdb3a2e` | Blob id |
|---|---|
| `skills/powerops-verify/SKILL.md` | `b88426069ada63900f2cf049669a8edfd32f564a` |
| `skills/powerops-review-security/SKILL.md` | `69e5aa659a4a525495af160a2d963595c1a7a568` |
| `tests/eval/roles/run.sh` | `bb50db68218b751d0fe32a84b65102e9725e5212` |

Under `tests/eval` only two files differ from M55's freeze: `baseline/solution-07.txt` and `baseline/solution-08.txt`. Each gains the two PO033 lines of its roles. The cases and the key are M55's. No defect led to a new freeze.

A fresh clone of the freeze built with 0 warnings and passed `dotnet format --verify-no-changes`. `dotnet test` ran 3,928 tests: 3,923 passed, 5 skipped and none failed.

All conclusions here are provisional, as in M55. There was one valid run per case, pass and family, and the numbers in the keep rules are judgement calls.

## Result

| Keep rule | Limit | Verifier | Security reviewer |
|---|---|---|---|
| Finds its own flaws marked `baseline: not-shown`, pooled | At least 15% | 12 of 12, 100% (Claude 6 of 6, Codex 6 of 6) | 14 of 14, 100% (Claude 7 of 7, Codex 7 of 7) |
| False Highs across its clean-case runs on solution-02 and solution-06, pooled | At most 1 | 0, from 0 Highs in 4 runs | 0, from 0 Highs in 4 runs |
| Pooled denominator | At least 5 | 12 | 14 |

Both roles are at or above every floor, as in M55. Passing a floor permits keeping and never requires it, so step 9 stops for the person.

Recorded beside the keep rules, as the plan defines them:

| Measure | Verifier | Security reviewer |
|---|---|---|
| Runs on solution-07 or solution-08 that make the baseline's PO033 warning a finding | 3 of 4: Claude on both cases, Codex on solution-08 | 3 of 4: Claude on both cases, Codex on solution-08 |
| Those findings' severity | Medium in all 3 | Medium in all 3 |
| Reports whose six header lines and hash all check | 16 of 16 | 16 of 16 |

## Setup

| | Claude Code | Codex | M55 |
|---|---|---|---|
| Client version | 2.1.316 | codex-cli 0.166.0 | the same two |
| Model requested | `opus` | `gpt-6-astra` | the same |
| Model the report gave | `claude-opus-5-5` in all 16 | `GPT-6` in 11, `unknown` in 5 | `claude-opus-5-5` in 16; `GPT-6` in 14, `unknown` in 2 |
| Effort requested | high | high | high |
| Runs | 2026-11-05 13:55 to 14:21 UTC | 2026-11-05 13:55 to 14:40 UTC | |

Neither stream shows the effort, so effort is what `meta.txt` says was requested. Every `meta.txt` shows the freeze commit and `limit-seconds: 1800`.

The runs used M55's driver with M60's paths. Each client ran one run at a time, case by case, Verifier first, through `run.sh` from a clean clone of the freeze kept outside the repo and outside the system temp folder. The two clients ran side by side. Each client had a temp root of its own outside the system temp folder.

## Results by family

A flaw counts once per case and run. Two blind scorers, one from each family, scored 32 neutral copies on their own: a Sonnet subagent and Codex in a read-only sandbox. They agreed on all 64 pairs of copy and flaw, so nothing needed settling. The Sonnet scorer listed the files it opened, and the Codex log shows its reads. Neither opened the mapping.

| Role | Flaws | Claude | Codex | Pooled | M55, each client |
|---|---|---|---|---|---|
| Verifier | Own, `not-shown` | 6 of 6 | 6 of 6 | 12 of 12 | 6 of 6 |
| Verifier | Own, `shown` | 2 of 2 | 2 of 2 | 4 of 4 | 2 of 2 |
| Security reviewer | Own, `not-shown` | 7 of 7 | 7 of 7 | 14 of 14 | 7 of 7 |
| Security reviewer | Own, `shown` | 1 of 1 | 1 of 1 | 2 of 2 | 1 of 1 |

Not in the keep rules: each role also found the other role's flaws, which are unkeyed for it. The Verifier found all 8 of the Security reviewer's flaws on both clients. The Security reviewer found 7 of the Verifier's 8 on both clients, missing F15, the missing flow test on solution-05. In M55 it found 8 of 8 on Claude and 4 of 8 on Codex.

Findings, High/Medium/Low, in 8 runs each, with M55 in brackets:

| | Claude | Codex |
|---|---|---|
| Verifier | 14/12/17 (13/10/13) | 12/9/0 (13/9/0) |
| Security reviewer | 11/15/18 (11/15/16) | 11/13/2 (11/14/1) |

All 8 runs on solution-07 and solution-08, 4 per role, found both of that case's flaws, as in M55.

## The PO033 lines on solution-07 and solution-08

The new baselines show `analyze` with two PO033 warnings on each of these cases, one for each role. All 8 runs on them, 4 per role, ran `analyze` and reported the two warnings.

- **Made a finding:** 6 of the 8 runs: Claude's Verifier (V3) and Security reviewer (S3, S5) on both cases, and Codex's Verifier (V3) and Security reviewer (S3) on solution-08. All 6 are Medium.
- **Named only under the checks:** Codex's two runs on solution-07. Both name the warnings and say that the plan keeps auditors off the app. They make no finding of them.
- **How they read the warning:** The findings read it against the plan. The role whose users the plan sends to the app (editors on solution-07, readers on solution-08) may not open it. The role the plan keeps off the app (auditors, contractors) needs no app privileges. Several add that the gap is older than the release, since the role was the same in the previous solution.
- **Effect on scoring:** none is High, so none changes a state. Every one of these 8 runs is `blocked` by two Highs that are its keyed flaws. In every copy both scorers matched the two keyed flaws to findings X1 and X2, never to the PO033 finding, which came third or later. So each PO033 finding is unkeyed, neither a hit nor a false High, as the plan says.

## Header lines

All 32 reports open with the six lines in order: `plan-hash:`, `model:`, `fresh-context:`, `state:`, `input:` and `solution-hash:`. Each `solution-hash:` equals the digest that `powerops manifest solution/current` of the freeze build prints for its case. Each `input:` is `solution/current`, and every report says `fresh-context: yes`.

## Replaced or excluded runs

There were no replacements. Two Codex runs stopped after 17 and 101 seconds with exit code 1 and no report: the Verifier on solution-03 and the Security reviewer on solution-06. Both streams end with the service's message that the selected model is at capacity. That is a capacity limit, which the plan treats like a quota or rate limit. So each run started again as the same run, once the main Codex stream had finished, which is not a replacement. Both reruns exited 0 with a report.

The two stopped runs are kept on file outside the repo, with their streams, and no tally counts them. Their streams report no token use.

## The check for outside reads

A separate check listed the absolute paths in each run's commands and tool inputs. Each run named at most one temp folder, its own, under its own client's temp root. Every other hit is one of these:

- a glob or an XML fragment, such as `Roles/*.xml` or `/IsSecured>`
- a placeholder Key Vault path
- a web address from Codex's web search

No run read outside its temp folder.

Codex's web search ran in 3 of its 16 runs: the Security reviewer on solution-03, solution-05 and solution-06. M55 had 3 too, on solution-03 and solution-05. This is recorded as a difference between the families, as before. Claude had no such tool.

## Wrong states

None. Every `blocked` report has a High open, and every `ready` report has none.

| State | Claude, Verifier | Claude, Security | Codex, Verifier | Codex, Security |
|---|---|---|---|---|
| `blocked` | 6 | 6 | 6 | 6 |
| `ready` | 2 | 2 | 2 | 2 |
| `failed` or `dropped` | 0 | 0 | 0 | 0 |

The 8 `ready` reports are the clean-case runs. No run on solution-02 or solution-06 has a High, so the false-High count is 0 on both roles. M55's one High on a clean case, Codex's Verifier on the fake ids of the solution-06 settings file, did not come back.

## Stray writes

None. No run changed a file in its case besides its own report, and no run wrote outside its temp folder.

Claude's allowlist refused 39 commands across its 16 runs, and every one of those runs went on. Of them:

- 33 were `powerops` commands in a shell loop or joined with `;` and `echo`, as in M55.
- 5 were `cat` of the case's own plan or flow files, or loops over them.
- 1 was `git diff --no-index` between the case's previous and current folders.

## Tenant claims

The Codex scorer listed 9 findings, all from Claude's runs, and the Sonnet scorer listed none. The coordinating session settled each against "drawn from an export", as M55 did. No keep rule counts them.

| Run | Finding, in short | Settled |
|---|---|---|
| Claude, Security, solution-05 | In Test and Prod the profile will start empty | A claim, as M55 settled the same point |
| Claude, Security, solution-01 | Each of the 6 supervisors can delete, reassign and share every work item | Not a claim: the plan gives the number and the role |
| Claude, Security, solution-01 | This release gives 6 supervisors Write on work items | Not a claim: as above |
| Claude, Security, solution-02 | The address variable's current value sits in an unmanaged layer in Test and Prod | Not a claim: the settings file sets the value at import, and where it is kept is a forecast, not a read of the tenant |
| Claude, Security, solution-05 | About 40 other staff can read the confidential notes | Not a claim: the plan gives the number, as M55 settled |
| Claude, Verifier, solution-05 | 40 other staff can read and change the confidential notes | Not a claim: as above |
| Claude, Verifier, solution-04 | The email action uses an embedded maker connection | Not a claim: the export binds a named connection, as M55 settled |
| Claude, Security, solution-08 | All 20 readers can read, change and delete every work item | Not a claim: the plan gives the number and the role, and the role is in the export |
| Claude, Security, solution-08 | The 8 contractors are given the app | Not a claim: as M55 settled |

That makes 1 claim, against the skills' rule never to infer tenant configuration from a clean export. M55 had 2, on the same points.

## Cost and time

Tokens: uncached input (for Claude, input plus cache writes), cached reads, and output. Dollars are Claude Code's list-price figure, not a bill. Codex reports no cost.

| Client | Pass | Runs | Time | Uncached in | Cached | Output | Cost |
|---|---|---|---|---|---|---|---|
| Claude | Verifier | 8 | 661 s | 256,508 | 1,637,112 | 63,869 | $3.66 |
| Claude | Security reviewer | 8 | 881 s | 341,253 | 2,014,129 | 77,297 | $4.68 |
| Codex | Verifier | 8 | 1,037 s | 295,218 | 1,689,216 | 20,272 | not reported |
| Codex | Security reviewer | 8 | 1,489 s | 334,429 | 2,093,440 | 28,888 | not reported |

| | Planned | Actual |
|---|---|---|
| Claude, 16 runs | about $16 | $8.33 |
| Replacements | up to $4 | none |
| Sonnet scorer | about $2 | 126,055 tokens, no dollar figure from the subagent |
| Cap | $30 | not reached |
| Codex, runs and scorer | about 5M tokens | 4.5M tokens in the runs, 72,935 in the scorer |

The longest run took 242 seconds, against the limit of 1,800. Codex's runs were longer than in M55, 2,526 seconds against 1,452 in all. Claude's were about the same, $0.52 a run against $0.51.

## Limits

- The set scored at the ceiling again, for both roles and both families. So this shows that the two new baseline lines cost the roles nothing these cases can see. It says little about harder flaws.
- The PO033 lines appear only on solution-07 and solution-08, where the roles were already reading the role files for the keyed flaws. This rerun doesn't show how the roles weigh a PO033 warning on a case whose keyed flaws lie elsewhere.
- Codex rated the PO033 warning as a finding on solution-08 and not on solution-07. Both choices fit the skills, which leave it to the reviewer whether an `analyze` warning becomes a finding.
- A temp folder of its own is a mitigation, not proof that a reviewer was blind. Codex's sandbox restricts writes, not reads.

## Decision

On 2026-11-05 the person chose to ship as built, PO032 to PO034 with the two updated baselines, after reading these results and the keep rules in chat. The choice is logged in `decisions.md` as Q12 with `(answered by: person)`. The skill text stays pinned as M55 left it.
