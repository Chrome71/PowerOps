# M38 report: two agent roles measured

Evaluated revision: `b254179bcfaa22122e1a4042f7fb74b311ada983`, the freeze commit of step 4. The cases, the key, the baseline, the skills, `run.sh` and its prompts are as they stand there. No defect led to a new freeze.

All conclusions here are provisional. There was one valid run per case, pass and family, and the numbers in the keep rules are judgement calls.

## Result

| Pass or advice | Keep rule | Result | Reading |
|---|---|---|---|
| Security reviewer, plan pass | Finds at least 15% of the plan flaws the same-family Plan reviewer missed | 0 of 0: the Plan reviewer missed none | Inconclusive, because the pooled denominator is below 5. That doesn't justify keeping |
| Security reviewer, solution pass | Finds at least 15% of its own flaws marked `baseline: not-shown` | 10 of 10 (Claude 5 of 5, Codex 5 of 5) | At or above the floor |
| Verifier | Finds at least 15% of its own flaws marked `baseline: not-shown` | 8 of 8 (Claude 4 of 4, Codex 4 of 4) | At or above the floor |
| False alarms | At most one false High per role across its clean-case runs, pooled | Security reviewer 1 of 14 Highs in 10 runs, Verifier 0 of 1 High in 4 runs | Within the limit. The Security reviewer is at it |
| Second-family advice | The mean of AB and BA beats the better of AA and BB by at least 10 percentage points | All four are 100%, so the gain is 0 points | Below the floor, so the advice is dropped |

Passing a floor permits keeping and never requires it. The person's decision is under [Decision](#decision).

## Setup

| | Claude Code | Codex |
|---|---|---|
| Client version | 2.1.316 | codex-cli 0.166.0 |
| Model requested | `opus` | `gpt-6-astra` |
| Model the client reported | `claude-opus-5-5` in all 36 streams and reports | The stream names no model. The reports said `GPT-6` in 28 runs and `unknown` in 8 |
| Effort requested | high | high |

Neither stream shows the effort, so effort is what `meta.txt` says was requested. Every run's `meta.txt` shows the freeze commit and `limit-seconds: 1800`. The two clients ran side by side, each one run at a time, from 2026-11-01 23:18 to 2026-11-02 01:10 UTC.

## Results by pass and family

A flaw counts once per case and run. Two blind scorers, one from each family, scored 72 neutral copies on their own. They agreed on all 156 pairs of copy and flaw. A sample of 8 found pairs was read against the findings, and each one named the planted flaw.

| Pass | Flaws | Claude | Codex | Pooled |
|---|---|---|---|---|
| Plan reviewer | Plan flaws | 27 of 27 | 27 of 27 | 54 of 54, 100% |
| Security reviewer, plan pass | Plan flaws | 23 of 27, 85.2% | 17 of 27, 63.0% | 40 of 54, 74.1% |
| Security reviewer, solution pass | Own, `not-shown` | 5 of 5 | 5 of 5 | 10 of 10, 100% |
| Security reviewer, solution pass | Own, `shown` | 1 of 1 | 1 of 1 | 2 of 2 |
| Verifier | Own, `not-shown` | 4 of 4 | 4 of 4 | 8 of 8, 100% |
| Verifier | Own, `shown` | 2 of 2 | 2 of 2 | 4 of 4 |

Plan flaws the same-family Plan reviewer missed: Claude 0, Codex 0. So the Security plan pass could add nothing that this set can show.

The Security plan pass found every flaw on a security point in the runs that reviewed the plan: Claude 18 of 18, Codex 16 of 16. Its other misses are all flaws on the Plan reviewer's own checklist: 4 for Claude and 7 for Codex. Codex's total also holds 3 flaws in plan-01, where the run reviewed nothing (see [Wrong states](#wrong-states)). Without that run Codex is 17 of 24, 70.8%, and the pooled figure is 40 of 51, 78.4%. No keep rule changes either way, because the Plan reviewer missed none.

Second-family advice, on the 9 plan cases with flaws and 27 flaws, union recall: AA 100%, BB 100%, AB 100%, BA 100%.

Not in the keep rules: each solution pass also found the other's flaws, which are unkeyed for it. The Verifier found 6 of 6 of the Security solution pass's flaws on both clients. The Security solution pass found 6 of 6 of the Verifier's on Claude and 2 of 6 on Codex. So on these cases the Verifier alone reached all 12 solution flaws on both clients. Findings per pass, High/Medium/Low:

| Pass | Claude | Codex |
|---|---|---|
| Plan reviewer, 12 runs | 56/185/53 | 34/93/3 |
| Security reviewer, plan pass, 12 runs | 28/110/42 | 22/46/0 |
| Security reviewer, solution pass, 6 runs | 7/25/21 | 8/17/1 |
| Verifier, 6 runs | 11/12/13 | 9/5/0 |

## Clean cases

Five cases have no keyed flaw: plan-02, plan-11, plan-12, solution-02 and solution-06. The measured roles raised 15 Highs on them, and each was checked against the case files.

- Security reviewer: 14 Highs, 13 supported by both scorers and recorded as unkeyed findings. They say the plans give no design for a boundary they rely on, such as row access by company or station, or a status column that can be written around an approval. The scorers split on the 14th, Codex's S1 on solution-02. It was settled as a false High: the plan records that choice on purpose, and the finding says it shows no defect.
- Verifier: 1 High, Codex's V1 on solution-06, supported by both scorers from the case files. It objects to the fake values in the settings file, and the reviewer couldn't know those are the set's own. Counting it as false would still be within the limit.
- Plan reviewer, the baseline, which no keep rule covers: 25 Highs. The scorers split on two. One was settled as supported, and Claude's R6 on plan-12 as false, because the plan licenses each environment's account.

All 6 Security plan runs on the clean plans ended `blocked`, so on this set the plan pass's state tells a clean plan from a flawed one in no run.

## Points with a keyed flaw

| Point | Flaws | Pass |
|---|---|---|
| SE:01 | 0 | |
| SE:02, SE:03, SE:05, SE:06, SE:07, SE:10 | 1 each | plan |
| SE:04 | 2 | plan 1, Security solution 1 |
| SE:08 | 2 | plan 1, Security solution 1 |
| SE:09 | 2 | plan 1, Verifier 1 |
| Data policies | 2 | plan |
| Security roles | 2 | plan 1, Security solution 1 |
| App users | 1 | plan |
| Sharing | 1 | plan |
| Connection references | 2 | plan 1, Security solution 1 |
| Secrets in environment variables | 2 | plan 1, Security solution 1 |
| Column security | 2 | plan 1, Security solution 1 |
| Power Pages table permissions and web roles | 1 | plan |
| Plan checklist | 14 | plan 9, Verifier 5 |

The keep decision says nothing about a point with no keyed flaw in a pass. In the Security solution pass that means SE:01 to SE:03, SE:05 to SE:07, SE:09, SE:10, data policies, app users, sharing and Power Pages.

## Replaced or excluded runs

No case left a denominator, and no run hit a quota, rate-limit or other infrastructure failure. All 80 runs exited 0.

Eight Codex runs got one replacement each, because a search returned file names from outside the temp folder while the run looked for an `AGENTS.md` file. Each run's search returned file names only, and no run opened one of those files. This was settled before any report was scored, and the first runs are kept.

| Pass | Case | Search |
|---|---|---|
| Plan reviewer | plan-07 | The system temp folder |
| Plan reviewer | plan-08 | The system temp folder |
| Plan reviewer | plan-10 | The system temp folder |
| Plan reviewer | plan-11 | The system temp folder |
| Security reviewer, plan pass | plan-08 | The system temp folder |
| Security reviewer, plan pass | plan-10 | The system temp folder |
| Security reviewer, solution pass | solution-04 | The system temp folder and the whole file system, which returned names from the person's home folder. `run.sh`'s search didn't flag it, because none of the names held what it looks for |
| Security reviewer, solution pass | solution-06 | The system temp folder |

All eight replacements came back without such a search.

## The check for outside reads

`run.sh`'s own search flagged no run. A separate check listed the absolute paths in each run's commands and tool inputs that lie outside its temp folder. Its results for the 72 valid runs:

- 16 Codex runs (10 Plan reviewer, 5 Security plan, 1 Security solution) tested for an `AGENTS.md` file in the root or the system temp folder, or for `/tmp/.agents`. None of those exists, so nothing came back. Under the rule in `decisions.md` these aren't reads, and each stays valid.
- The other hits aren't paths: search globs such as `/.git`, and text inside a search pattern.
- No Claude run has a hit that reaches outside its temp folder.

Codex's web search ran 18 times in the valid runs, all in Security passes. Claude had no such tool. Those searches are recorded here as a difference between the families, not as reads outside the temp folder.

## Wrong states

One wrong `state` in 48 reports that carry a state. Every other report follows the table: `blocked` with a High open, and `ready` with none.

- Codex, Security plan pass, plan-01: `state: failed`, no findings, and every checklist item `not verified`. `powerops plan status` exits 1 on that plan, because the address planted as flaw F29 trips its check for an email-like value, though the plan's status is `approved`. The check ran, so `failed` is the wrong state. The draft skill doesn't say what a violation from that command means. Claude's run on the same case went on and found all three flaws. The run is valid and scores 0 of 3.

## Stray writes

No run changed a file in its case besides its own report and, for the Plan reviewer, the `review:` line, which all 24 Plan reviewer runs set to `done`.

Three valid Codex Plan reviewer runs (plan-03, plan-04 and the replacement on plan-10) wrote a copy of the plan to the system temp folder before editing it. That is a write outside the temp folder, and `diff -rq` can't see it. No run read those copies.

## Tenant claims

The scorers listed claims about tenant configuration, and each was settled against "drawn from an export".

| Run | Finding | Scorers | Settled |
|---|---|---|---|
| Claude, Verifier, solution-04 | V3 says a person gets no read access, from a role missing in the export | Both | A claim |
| Claude, Security solution, solution-05 | One finding says a team would be locked out, from a profile that names no team | One | A claim |
| Claude, Verifier, solution-05 | The same, in the Verifier's report | One | A claim |
| Claude, Security solution, solution-01 | S4 gives a number of people with a role | One | Not a claim: the plan gives the number |
| Claude, Security solution, solution-06 | One finding gives a number of people with a role | One | Not a claim: the plan gives the number |

That makes 3 claims, all from Claude, against both drafts' rule never to infer tenant configuration from a clean export. No keep rule counts them.

## Cost and time

Tokens: uncached input (for Claude, input plus cache writes), cached reads, and output. Dollars are Claude Code's list-price figure, not a bill. Codex reports no cost. For the 72 valid runs:

| Client | Pass | Runs | Time | Uncached in | Cached | Output | Cost |
|---|---|---|---|---|---|---|---|
| Claude | Plan reviewer | 12 | 2,574 s | 398,987 | 1,566,356 | 258,490 | $8.67 |
| Claude | Security plan | 12 | 2,245 s | 356,469 | 816,336 | 223,245 | $7.48 |
| Claude | Security solution | 6 | 1,069 s | 350,132 | 1,357,742 | 111,136 | $5.29 |
| Claude | Verifier | 6 | 801 s | 279,941 | 1,006,634 | 85,165 | $4.14 |
| Codex | Plan reviewer | 12 | 1,196 s | 106,347 | 1,125,888 | 39,632 | not reported |
| Codex | Security plan | 12 | 1,082 s | 290,860 | 1,872,512 | 37,808 | not reported |
| Codex | Security solution | 6 | 603 s | 204,083 | 1,410,176 | 20,354 | not reported |
| Codex | Verifier | 6 | 399 s | 185,017 | 1,054,720 | 13,748 | not reported |

| | Estimate | Actual, Claude | Estimate | Actual, Codex |
|---|---|---|---|---|
| All tokens | 5.7M to 7.1M | 6.81M | 6.8M to 8.6M | 6.36M, or 7.57M with the 8 replaced runs |
| Time, one run at a time | 1.7 to 2.1 h | 1.86 h | 1.05 to 1.3 h | 0.91 h, or 1.14 h with the 8 replaced runs |
| List price | $22 to $28 | $25.59 | not reported | not reported |

The longest run took 260 seconds, against the limit of 1,800. Claude's weekly window went from 75% to 88% used before it reset, and its stream carried warnings but never refused a run. Claude's allowlist refused 47 commands in 21 runs, mostly a `powerops` command with `; echo` added, and those runs went on.

## Limits

- The Plan reviewer found every plan flaw on both clients, so the set can't show what a second plan reviewer adds. The plan flaws were written from row 1's list and the Plan reviewer's checklist, so that floor was a low bar and the flaws are plain to see. This sets both the Security plan result and the second-family result, and neither says that a second reviewer can't help on subtler flaws.
- The solution passes also scored at the ceiling. With 100% on both, the results can't rank the clients or the two passes, and they say little about flaws harder than these.
- A family here is a client with its own harness and model, and Codex's shell has no allowlist. The second-family advice can't separate the three.
- Nothing in the set checks that a role finds a flaw only a real tenant would show. Most of row 1's points never show in an export.
- No case holds a zip, so the `<zip>.findings.json` branch of the Verifier wasn't exercised.
- The cases were hand-written in the layouts `pac solution unpack` writes, so they are close approximations.
- A temp folder is a mitigation, not proof that a reviewer was blind.

## Decision

Pending: the person keeps or drops each pass and the second-family advice at step 9.
