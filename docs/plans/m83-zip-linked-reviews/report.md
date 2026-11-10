# M83 report: the rerun of both review roles

Evaluated revision: `6b67efb60fbb2ece1106011d2bc9fcc923a57d4c`, the freeze commit of step 3. A squash merge leaves that commit off `main`, so the evaluated text is pinned by git blob id, on its LF text:

| File at `6b67efb` | Blob id |
|---|---|
| `skills/powerops-verify/SKILL.md` | `bcdedd451b04a2bf159f41df617d90e327523934` |
| `skills/powerops-review-security/SKILL.md` | `c2855570f488048bbfaabbc3bf0c84399de942e7` |
| `tests/eval/roles/run.sh` | `ed235cb46ec399ee082cc12373e5de70acc670f7` |

The cases, the key and the baselines are as they stand at that commit, unchanged since M55. No defect led to a new freeze.

A clean clone of that commit, outside the repo, built with 0 warnings and passed `dotnet format --verify-no-changes`. `dotnet test` ran 4,671 tests: 4,662 passed, 6 skipped and 3 failed, the three the plan names: `PluginManifestTests.Plugin_skills_are_the_shipped_skills` for `claude` and for `codex`, and `RoleEvalTests.The_kept_roles_text_is_the_pinned_or_a_recorded_blob`, because the copies and the pins change only after the keep decision. The real-`pac` test ran there (pac 2.14.1 on Linux); it skips where `pac` is not on `PATH`, so it doesn't run in CI or on Windows. The stub check of step 3 (stub clients, no model) passed its 21 assertions, and the Fable work review of rows 1 to 7 at the freeze passed before the first paid run.

All conclusions here are provisional, as in M38, M39 and M55: one valid run per case, pass and family, and the numbers in the keep rules are judgement calls.

## Result

| Keep rule | Limit | Verifier | Security reviewer |
|---|---|---|---|
| Finds its own flaws marked `baseline: not-shown`, pooled | At least 15% | 12 of 12, 100% (Claude 6 of 6, Codex 6 of 6) | 14 of 14, 100% (Claude 7 of 7, Codex 7 of 7) |
| False Highs across its clean-case runs on solution-02 and solution-06, pooled | At most 1 | 0, from 4 Highs in 4 runs, as settled below | 0, from 1 High in 4 runs, as settled below |
| Pooled denominator | At least 5 | 12 | 14 |

Both roles are at or above both floors, so the person's answer of 2026-11-09 ("Keep if rules hold") applies. The false-High row rests on the settlement of one split between the scorers; under the other reading the Verifier has 2 false Highs and misses that floor (see Clean-case Highs).

Recorded beside the keep rules, as the plan defines them:

| Measure | Verifier | Security reviewer |
|---|---|---|
| Its own new case, both flaws | Claude 2 of 2, Codex 2 of 2 on solution-07 | Claude 2 of 2, Codex 2 of 2 on solution-08 |
| Unchanged statements judged false | 1 (Claude) | 1 (Claude) |
| Unchanged statements unsupported on a strict reading | 5 | 5 |
| Reports whose seven header lines, `solution-hash:` and `deploy-hash:` all check | 16 of 16 | 16 of 16 |

## Setup

| | Claude Code | Codex |
|---|---|---|
| Client version | 2.1.319 | codex-cli 0.166.0 |
| Model requested | `opus` | `gpt-6-astra` |
| Model the report gave | `claude-opus-5-5` in all 16 | `GPT-6` or `unknown` |
| Effort requested | high | high |
| Runs | 2026-11-10 03:56 to 04:23 UTC | 2026-11-10 03:56 to 04:22 UTC |

Each client ran one run at a time, case by case, Verifier first, through `run.sh` from the clean clone, side by side, with its own `EVAL_OUT` and temp root outside the system temp folder. Every `meta.txt` shows the freeze commit, `limit-seconds: 1800` and a `zip-digest:`.

## Results by family

A flaw counts once per case and run. A Sonnet subagent and Codex in a read-only sandbox scored 32 neutral copies on their own (finding text only, new ids, no header lines). Neither opened the mapping. They agreed on all 64 pairs of copy and keyed flaw.

| Role | Flaws | Claude | Codex | Pooled | M55, each client |
|---|---|---|---|---|---|
| Verifier | Own, `not-shown` | 6 of 6 | 6 of 6 | 12 of 12 | 6 of 6 |
| Verifier | Own, `shown` | 2 of 2 | 2 of 2 | 4 of 4 | 2 of 2 |
| Security reviewer | Own, `not-shown` | 7 of 7 | 7 of 7 | 14 of 14 | 7 of 7 |
| Security reviewer | Own, `shown` | 1 of 1 | 1 of 1 | 2 of 2 | 1 of 1 |

Not in the keep rules: the Verifier also found all 8 of the Security reviewer's flaws on both clients; the Security reviewer found all 8 of the Verifier's on Claude and 7 of 8 on Codex (M55: 4 of 8).

## Clean-case Highs

Every High in a run on solution-02 or solution-06 came from Codex: its Verifier on solution-02 (2) and solution-06 (2), and its Security reviewer on solution-06 (1). Claude raised none there. The scorers split on all five: the Sonnet scorer marked them unsupported, the Codex scorer supported. The coordinating session settled them:

- **Placeholder settings** (Verifier V2 on both cases, Security S1 on solution-06): the settings file holds the set's own fake connection ids and an `.invalid` mailbox, which the reviewer can't know are the set's. Settled supported, an unkeyed finding, as M55 and M38 settled the same finding on solution-06.
- **"Deployment input is unmanaged"** (Verifier V1 on both cases): `solution/current/Other/Solution.xml` says `Managed` 0, the request named `deploy/current.zip` as the zip that will be deployed and its folder as that zip's unpack, the new skill text calls it "the managed zip that will be deployed", and the plan's route is managed in Test and Prod. The finding is true on the inputs the run was given, and both runs said it rests on the supplied relationship. Settled supported, an unkeyed finding. It is caused by the eval's zip, which is a plain zip of an unmanaged folder (decisions Q5); with a managed export, as the route has it, it would not arise. The Sonnet scorer read the plan's "moves to Test and Prod through the pipeline" as the pipeline making the managed package, which would make V1 a false High in two runs and fail the Verifier's floor.

## Header lines

All 32 reports open with the seven lines in order: `plan-hash:`, `model:`, `fresh-context:`, `state:`, `input:`, `solution-hash:` and `deploy-hash:`. Each `solution-hash:` equals the digest that `powerops manifest solution/current` of the freeze build prints for its case, and each `deploy-hash:` equals that run's `zip-digest:`. Every report says `input: solution/current` and `fresh-context: yes`. Every run called `powerops manifest deploy/current.zip`. No run's stream shows another command that reads the zip (no `analyze`, `diff`, `test` or unzip of it); many listed the `deploy` folder with `ls`, and one Codex search excluded the zip by name; Codex's Verifier on solution-03, 07 and 08 wrote in its report that the zip was only hashed.

## Unchanged statements

A Sonnet subagent read every original report for a statement that a security role, table, column, form, view or app role map is unchanged, the same, identical or not modified, and judged each against the case files and the run's own comparison, as the plan defines. Every run called the two-path `manifest`. Of 43 statements:

| | False | Unsupported (strict) | Supported |
|---|---|---|---|
| Claude, Verifier | 1 | 3 | 4 |
| Claude, Security | 1 | 0 | 12 |
| Codex, Verifier | 0 | 2 | 4 |
| Codex, Security | 0 | 5 | 11 |

- **False:** Claude's Verifier on solution-04 says no app module was added or changed, and Claude's Security reviewer on solution-06 says a table's `Entity.xml` was among the files the same. In both cases `Other/Customizations.xml` changed, which paragraph B counts as a change to all six kinds, so rule B didn't hold in those two runs. Both reports name the changed `Customizations.xml` elsewhere. M55 had none.
- **Unsupported on a strict reading:** solution-05 and solution-07, as in M55: forms, views or the rest of a table called unchanged while another file of the same `Entities/<table>/` folder changed.

## Replaced or excluded runs

None. All 32 runs exited 0 and wrote a report; no run hit a quota, rate-limit or other infrastructure failure.

## Outside reads and stray writes

`run.sh` flagged no run (no stream names the repo path, `tests/eval/roles`, `key.json` or `baseline/`). Each run's `case.diff` lists only the skill folder and its own report: `run.sh` removes the zip before the comparison, and no run wrote anything else.

## Wrong states

None found: every `blocked` report has a High open, and every `ready` report has none.

| State | Claude, Verifier | Claude, Security | Codex, Verifier | Codex, Security |
|---|---|---|---|---|
| `blocked` | 6 | 6 | 8 | 7 |
| `ready` | 2 | 2 | 0 | 1 |

## Tenant claims

The Sonnet scorer listed 2 findings, the Codex scorer 9, all from Claude's Security runs. Settled against "drawn from an export", as M55 did: 2 claims, both listed by both scorers (solution-05, supervisors locked out until someone assigns the profile by hand; solution-04, mail sent through a maker's connection), the same points M38, M39 and M55 settled as claims. The other 7 say a flow with no failure branch fails unnoticed; that is a forecast from the export, not a claim about configuration, and was settled as not a claim.

## Cost and time

Dollars are Claude Code's list-price figure from each run's `total_cost_usd`, not a bill. Codex reports no cost.

| Client | Pass | Runs | Time | Uncached in | Cached | Output | Cost |
|---|---|---|---|---|---|---|---|
| Claude | Verifier | 8 | 702 s | 268,889 | 1,755,392 | 60,705 | $3.72 |
| Claude | Security reviewer | 8 | 922 s | 320,035 | 2,095,185 | 79,348 | $4.57 |
| Codex | Verifier | 8 | 667 s | 263,797 | 1,669,376 | 21,293 | not reported |
| Codex | Security reviewer | 8 | 870 s | 298,094 | 2,028,160 | 29,382 | not reported |

| | Planned | Actual |
|---|---|---|
| Claude, 16 runs | about $11 | $8.28 |
| Replacements | up to $3 | none |
| Sonnet scorer and the unchanged-statement judge | about $2 | 137,833 and 248,704 subagent tokens, no dollar figure |
| Cap | $30 | not reached |
| Codex, runs and scorer | about 4.5M tokens | 4.6M in the runs; 481,574 in and 4,500 out for the scorer |
| Time | about an hour | 27 minutes of runs on each client |

## Limits

- The set scored at the ceiling again, for both roles and both families, so this shows that the new text lost nothing these cases can see. It says little about harder flaws, and nothing about a real managed export: the eval's zip is a plain zip of each case's unmanaged folder, which the roles only hash.
- The false-High floor of the Verifier rests on the settlement above.
- Two false unchanged statements, against none in M55, both through the `Other/Customizations.xml` sentence of paragraph B, which the roles' text didn't change.
- Every case has a previous solution and is an unpacked folder; no run took the branches for a zip or for no previous solution.
- A temp folder of its own is a mitigation, not proof that a reviewer was blind. Codex's sandbox restricts writes, not reads.
- Nothing ran on Windows.
