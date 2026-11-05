# M55 report: the rerun of both review roles

Evaluated revision: `850db2812901370e1fdea442c915eb73d057e918`, the freeze commit of step 3. A squash merge leaves that commit off `main`, so the evaluated text is pinned by git blob id, on its LF text:

| File at `850db28` | Blob id |
|---|---|
| `skills/powerops-verify/SKILL.md` | `b88426069ada63900f2cf049669a8edfd32f564a` |
| `skills/powerops-review-security/SKILL.md` | `69e5aa659a4a525495af160a2d963595c1a7a568` |
| `tests/eval/roles/run.sh` | `bb50db68218b751d0fe32a84b65102e9725e5212` |

The cases, the key and the baselines are as they stand at that commit. No defect led to a new freeze.

A fresh clone of that commit built with 0 warnings and passed `dotnet format --verify-no-changes`. `dotnet test` ran 3,715 tests: 3,708 passed, 5 skipped and 2 failed. The two failures are the ones the plan names: `PluginManifestTests.Plugin_skills_are_the_shipped_skills` for `claude` and for `codex`, because the plugin copies change only after the person's decision. That run holds `Every_solution_case_has_a_baseline_file_that_matches_the_live_output`, so the baselines match the freeze.

All conclusions here are provisional, as in M38 and M39. There was one valid run per case, pass and family, and the numbers in the keep rules are judgement calls.

## Result

| Keep rule | Limit | Verifier | Security reviewer |
|---|---|---|---|
| Finds its own flaws marked `baseline: not-shown`, pooled | At least 15% | 12 of 12, 100% (Claude 6 of 6, Codex 6 of 6) | 14 of 14, 100% (Claude 7 of 7, Codex 7 of 7) |
| False Highs across its clean-case runs on solution-02 and solution-06, pooled | At most 1 | 0, from 1 High in 4 runs | 0, from 0 Highs in 4 runs |
| Pooled denominator | At least 5 | 12 | 14 |

Both roles are at or above both floors. Passing a floor permits keeping and never requires it, so step 6 stops for the person.

Recorded beside the keep rules, as the plan defines them:

| Measure | Verifier | Security reviewer |
|---|---|---|
| Its own new case, both flaws | Claude 2 of 2, Codex 2 of 2 on solution-07 | Claude 2 of 2, Codex 2 of 2 on solution-08 |
| Unchanged statements judged false | 0 | 0 |
| Unchanged statements unsupported on a strict reading of paragraph B | 5, in 4 runs | 9, in 4 runs |
| Reports whose six header lines, hash and locator all check | 16 of 16 | 16 of 16 |

The set again scored at the ceiling, as in M38 and M39, so it shows that the new text lost nothing these cases can see and that both roles caught the new trap. It says little about harder flaws.

## Setup

| | Claude Code | Codex |
|---|---|---|
| Client version | 2.1.316 | codex-cli 0.166.0 |
| Model requested | `opus` | `gpt-6-astra` |
| Model the report gave | `claude-opus-5-5` in all 16 | `GPT-6` in 14, `unknown` in 2 |
| Effort requested | high | high |
| Runs | 2026-11-05 01:56 to 02:20 UTC | 2026-11-05 01:36 to 02:00 UTC |

These are M38's and M39's models and effort. Neither stream shows the effort, so effort is what `meta.txt` says was requested. Every `meta.txt` shows the freeze commit and `limit-seconds: 1800`. Each client ran one run at a time, case by case, Verifier first, through `run.sh` from a clean clone of the freeze kept outside the repo and outside the system temp folder. Codex started 20 minutes before Claude, because the Claude CLI sign-in had expired and waited for the person, and the two overlapped for the last 4 Codex runs. Each run's temp folder was made under a temp root of its own for each client, outside the system temp folder, so the scorers' files and the coordinating session's subagent transcripts were not in the folders a run could search.

## Results by family

A flaw counts once per case and run. Two blind scorers, one from each family, scored 32 neutral copies on their own: a Sonnet subagent and Codex in a read-only sandbox. They agreed on 63 of the 64 pairs of copy and flaw, and neither opened the mapping. The difference was F04 in the Codex Security reviewer's copy of solution-03, a Verifier flaw, which is unkeyed for that run. The Sonnet scorer marked it not found and Codex found. The copy's finding on the test says that the test asserts the recipient and the message, and it doesn't say that the message is wrong or that the test fails, so it was settled as not found.

| Role | Flaws | Claude | Codex | Pooled | M38 or M39, each client |
|---|---|---|---|---|---|
| Verifier | Own, `not-shown` | 6 of 6 | 6 of 6 | 12 of 12 | 4 of 4 (M38) |
| Verifier | Own, `shown` | 2 of 2 | 2 of 2 | 4 of 4 | 2 of 2 (M38) |
| Security reviewer | Own, `not-shown` | 7 of 7 | 7 of 7 | 14 of 14 | Claude 5 of 5, Codex 3 of 3 (M39) |
| Security reviewer | Own, `shown` | 1 of 1 | 1 of 1 | 2 of 2 | 1 of 1 (M39) |

Not in the keep rules: each role also found the other's flaws, which are unkeyed for it. The Verifier found all 8 of the Security reviewer's flaws on both clients. The Security reviewer found all 8 of the Verifier's on Claude and 4 of 8 on Codex. M38 and M39 measured the same shape: the Verifier alone reached every solution flaw.

Findings, High/Medium/Low, in 8 runs each:

| | Claude | Codex |
|---|---|---|
| Verifier | 13/10/13 | 13/9/0 |
| Security reviewer | 11/15/16 | 11/14/1 |

## The new cases

solution-07 changes the Verifier's role and app role map behind a `diff` that prints only the version change. solution-08 changes the Security reviewer's role and app role map behind a `diff` that prints compared changes and none about roles or the app. In both, the root components stay the same.

All 8 runs of each role on its own new case found both flaws, and all 8 runs of each role on the other new case found that case's two flaws too. Every run called the file-by-file comparison and named the changed role file and app module from its output.

## Unchanged statements

The coordinating session read every original report for a statement that a security role, table, column, form, view or app role map is unchanged, the same, identical or not modified, in any section. It judged each against the case's files and the run's own comparison, as the plan defines.

- **False:** none, in any run.
- **Where `Other/Customizations.xml` changed** (solution-03, solution-04 and solution-06): no run called any of the six kinds unchanged. Several said that the change counts against all of them, as paragraph B says, and read the components instead.
- **Supported:** the other statements, on solution-01, solution-02, solution-07 and solution-08: the unchanged auditor and contractor roles of the new cases, and the tables of cases whose `Entities/` folder and `Other/Customizations.xml` were the same.
- **Unsupported on a strict reading:** 14 statements, counted one per sentence, in all 8 runs on solution-05 and solution-07: 8 from Claude and 6 from Codex, 5 from the Verifier and 9 from the Security reviewer. They call forms, views, other views or `Entity.xml` unchanged while another file of the same `Entities/<table>/` folder changed: `Entity.xml` on solution-05, and the planned view on solution-07. Each is true file by file, and each run named the file that changed. Codex's two runs on solution-07 also say that the table's folder as a whole changed. Paragraph B says that a table with its columns, forms and views is that folder, so a strict reading counts these as unsupported. The plan didn't settle which reading applies, so both are shown.

## Header lines

All 32 reports open with the six lines in order: `plan-hash:`, `model:`, `fresh-context:`, `state:`, `input:` and `solution-hash:`. Each `solution-hash:` equals the digest that `powerops manifest solution/current` of the freeze build prints for its case. `plan status` of the freeze build read each report's `input:` as the locator `solution/current` and its hash as 64 hex characters, with no warning. Every report says `fresh-context: yes`.

## Replaced or excluded runs

None. All 32 runs exited 0 and wrote a report, no run hit a quota, rate-limit or other infrastructure failure, and `run.sh` flagged no run.

## The check for outside reads

A separate check listed the absolute paths in each run's commands and tool inputs. Apart from paths inside the run's own temp folder, every hit is a glob or an XML fragment, such as `Roles/*.xml` or `/IsSecured>`, or a web address from Codex's web search. One Codex run used `rg` with exclusions for `.git`, `.agents` and `.codex`, and the search named no path, so it ran inside its own folder. No run read outside its temp folder.

Codex's web search ran in 3 of its 16 runs, Security reviewer on solution-03 and solution-05 and Verifier on solution-03, recorded as a difference between the families, as in M38 and M39. Claude had no such tool.

## Wrong states

None. Every `blocked` report has a High open, and every `ready` report has none.

| State | Claude, Verifier | Claude, Security | Codex, Verifier | Codex, Security |
|---|---|---|---|---|
| `blocked` | 6 | 6 | 7 | 6 |
| `ready` | 2 | 2 | 1 | 2 |
| `failed` or `dropped` | 0 | 0 | 0 | 0 |

Codex's Verifier is `blocked` on solution-06 by its V1, the one High on a clean case. Both scorers supported it from the case files: the settings file holds the set's own fake mailbox and connection ids, which the reviewer can't know are the set's. M38 settled the same finding the same way. So it is an unkeyed finding, not a false High.

## Stray writes

None. No run changed a file in its case besides its own report, and no run wrote outside its temp folder.

Claude's allowlist refused 34 commands across its 16 runs, and every one of those runs went on. Of them, 22 were `powerops` commands in a shell loop or joined with `;` and `echo`, as in M38 and M39. The other 12 were `cat` of the case's own plan files, or loops over its own flow files, which the run then read with its Read tool.

## Tenant claims

The Codex scorer listed 11 findings, all from Claude's runs. The Sonnet scorer listed none. The coordinating session settled each against "drawn from an export", from the plan text and the earlier settlements. No keep rule counts them.

| Run | Finding, in short | Settled |
|---|---|---|
| Claude, Security, solution-05 | Supervisors stay locked out until someone assigns the profile by hand | A claim, as M38 and M39 settled the same point |
| Claude, Verifier, solution-04 | The team lead gets no access in this release, from a role missing in the export | A claim, as M38 settled the same point |
| Claude, Security, solution-05 | About 40 staff can read and change the confidential notes | Not a claim: the plan gives the number and says they use the table, as M39 settled |
| Claude, Security, solution-03 | A work item in any business unit posts its name | Not a claim: the trigger's organization scope is in the export, and the rest is a forecast |
| Claude, Verifier, solution-03 | Changes outside the one business unit reach the channel | Not a claim: as above |
| Claude, Verifier, solution-07 | The role map gives the 3 auditors the app | Not a claim: the plan gives the number and the role, and the role map is in the export |
| Claude, Security, solution-08 | The 8 contractors can now open the app | Not a claim: as above |
| Claude, Security, solution-08 | The contractors update work items | Not a claim: the plan says so |
| Claude, Verifier, solution-08 | Deployed environments never use the no-value path | Not a claim: the settings file sets the value for every environment |
| Claude, Verifier, solution-04 | The flow runs on a maker's connection from Dev | Not a claim: the export binds a named connection instead of the reference, and the plan names Dev as the source |
| Claude, Security, solution-04 | The flow sends mail through one maker's connection captured at export | Not a claim: as above |

That makes 2 claims, against the skills' rule never to infer tenant configuration from a clean export. M38's Security pass had 1 of 3, M39's had 1, and M38's Verifier had 2, all on the same points.

## Cost and time

Tokens: uncached input (for Claude, input plus cache writes), cached reads, and output. Dollars are Claude Code's list-price figure, not a bill. Codex reports no cost.

| Client | Pass | Runs | Time | Uncached in | Cached | Output | Cost |
|---|---|---|---|---|---|---|---|
| Claude | Verifier | 8 | 640 s | 285,999 | 1,767,649 | 61,171 | $3.86 |
| Claude | Security reviewer | 8 | 826 s | 291,875 | 1,847,209 | 76,055 | $4.22 |
| Codex | Verifier | 8 | 603 s | 267,368 | 1,629,696 | 18,724 | not reported |
| Codex | Security reviewer | 8 | 849 s | 320,594 | 1,887,616 | 27,435 | not reported |

| | Planned | Actual |
|---|---|---|
| Claude, 16 runs | about $16 | $8.09 |
| Replacements | up to $4 | none |
| Sonnet scorer | about $2 | 115,759 tokens, no dollar figure from the subagent |
| Cap | $30 | not reached |
| Codex, runs and scorer | about 5M tokens | 4.2M tokens in the runs, 67,003 in the scorer |
| Time | about an hour of runs, half an hour of scoring | 24 minutes of runs on each client; the scorers started at 02:21 UTC, 20 seconds after the last run, and the settled tally was ready by 02:27 UTC |

The longest run took 133 seconds, against the limit of 1,800. Runs were about as long as M38's and M39's, and cheaper on Claude, about $0.51 a run against $0.69 and $0.83.

## Limits

- The set scored at the ceiling again, for both roles and both families, so this shows that the new text lost nothing these cases can see and that the new trap was caught. It says little about harder flaws.
- Every case has a previous solution and is an unpacked folder, so no run took the branches for a zip or for no previous solution (review R16). Paragraph B names only the zip.
- The two new cases share one table, one app and two roles, and their XML is hand-written, close to what `pac solution unpack` writes. A real export has more files in `Other/Customizations.xml` and more reasons for it to change, which sends a role to re-read more.
- The strict and the plain reading of paragraph B's folder sentence give different counts on solution-05 and solution-07. Neither changes a keep rule.
- A temp folder of its own is a mitigation, not proof that a reviewer was blind. Codex's sandbox restricts writes, not reads.

## Decision

Waiting for the person, at step 6.
