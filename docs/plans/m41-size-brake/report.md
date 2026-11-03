# M41 report: item 11 on two plan cases

Evaluated revision: `dab424253f5730e1857c05e203f806a9c69f5dcd`, round 3. A squash merge leaves it off `main`, so the text that was evaluated is pinned by git blob id, on its LF text: `skills/powerops-review-plan/SKILL.md` is `9016b849ae305b0932668d593024ab032cb0f2fd`. The cases (`plan-13` plan `c32b9c4cdecbe0abe6160bd5098a25c5bbac88ea`, `plan-14` plan `f94b756bcdbb4146697d551cbc58529dbb142880`), `key.json` (`9cc7cb09b2b9de9f812eeaa45f508f3b42b04929`) and `run.sh` (`6225d97f6fcc1a8f1a8f0937286b7b85cf371e9b`, unchanged since M39) are the same in all three rounds; only item 11's wording changed between them.

The result is provisional, as in M38 and M39: one run per case, client and round.

## Result

| Round | Commit | Item 11 text | `plan-13`: keyed problems named by item 11 | `plan-14`: item 11 Highs | Reading |
|---|---|---|---|---|---|
| 1 | `d181c13` | as built | Claude 3 of 3, Codex 3 of 3 | Claude 0, Codex 0 | Pass |
| 2 | `f067726` | Reuse, Extend or Create asked of tables and lists only (decisions Q9) | Claude 3 of 3, Codex 3 of 3 | Claude 1, Codex 1 | Miss on `plan-14` |
| 3 | `dab4242` | scoped to what a plan has and doesn't need (decisions Q10) | Claude 3 of 3, Codex 3 of 3 | Claude 0, Codex 0 | Pass |

Round 3's text is the one that ships, so M41's done-when is met on both clients: item 11 names the per-depot copies, the title-only lists and the list no Goal line needs on `plan-13`, and raises no High on `plan-14`.

How the rounds came about:

- **Round 1** passed. Codex also raised a Medium on both cases' screens and flows for not being marked Reuse, Extend or Create, which item 11 asked of every artefact while the plan skill marks only tables and lists. That would fire on every plan the skill writes, so item 11's line was narrowed (Q9) and all four runs repeated, so that the report describes the text that ships.
- **Round 2** missed on `plan-14` on both clients. Codex filed a High under item 11 that the Equipment list's On loan column repeats the loan's Open column with no rule to keep the two in step. Claude filed a High under items 2 and 11 that nothing maps a team to its lead's mailbox. Both scorers found both supported: they are real gaps in `plan-14`, but neither is the plan being too big. That was the first miss, so the plan's one revision of item 11's wording followed (Q10): the item covers what a plan has and doesn't need, something missing or a column that repeats another belongs under the other items, and the "same concept" line names tables, lists, flows and screens.
- **Round 3** passed. Claude filed the team-lead gap under items 2 and 10 and the route gap under items 5 and 8; Codex raised no High on `plan-14`.

## Setup

| | Claude Code | Codex |
|---|---|---|
| Client version | 2.1.316 | codex-cli 0.166.0 |
| Model requested | `opus` | `gpt-6-astra` |
| Model the client reported | `claude-opus-5-5` in all 6 streams and reports | The stream names no model; the reports said `unknown` |
| Effort requested | high | high |

These are M38's and M39's versions, models and effort. Every run's `meta.txt` shows its round's commit and `limit-seconds: 1800`. The two clients ran side by side, each one run at a time: round 1 from 05:12 to 05:16 UTC on 2026-11-03, round 2 from 05:18 to 05:24, round 3 from 05:31 to 05:36. `run.sh` ran from this branch's worktree, clean in `tests/eval`, `src` and `skills` at each start.

## Scoring

Neutral copies kept each finding's severity, Area and text, with new ids and no header lines, shuffled; the mapping stayed outside the scorers' folder. Two blind scorers per round, a Sonnet subagent and Codex at `medium`, each had the copies, the two plans and the three key sentences, and recorded for each keyed problem the finding that names it and its Area, and for each High its Area and, on `plan-14`, whether the plan really has that defect.

- They agreed on every keyed problem in all three rounds, 18 of 18, and on every High's Area and support but one: in round 2 the Sonnet scorer listed a third High on Claude's `plan-14` copy, which the copy shows as Medium. Settled from the copy as Medium.
- In round 3 the Sonnet scorer was also asked whether item 11 was the first item named; every item 11 finding on `plan-13` names it alone, and neither `plan-14` High names it at all.
- Every keyed problem in every round was named by a finding whose Area is item 11, so no hit was counted from another item.

| Round | Client | F40 copies (High) | F41 title-only lists (Medium) | F42 no Goal line (High) | Other item 11 Highs on `plan-13` |
|---|---|---|---|---|---|
| 1 | Claude | High | Medium | High | 1: On loan repeats the open loan |
| 1 | Codex | High | Medium | High | 0 |
| 2 | Claude | High | Medium | High | 0 |
| 2 | Codex | High | Medium | High | 0 |
| 3 | Claude | High | Medium | High | 0 |
| 3 | Codex | High, split into the lists and the flows | Medium | High | 0 |

## `plan-14`'s Highs

`plan-14` has no keyed problem. Every High raised on it, by round, all supported by both scorers from the plan text:

| Round | Client | Area | Finding |
|---|---|---|---|
| 1 | Claude | 5, 8 | The route imports under the pipeline's service principal, but Constraints says there is no application user and no secret |
| 2 | Claude | 5, 8 | The same |
| 2 | Claude | 2, 11 | Nothing maps a team to its lead's mailbox |
| 2 | Codex | 11, 9 | On loan and Open hold one fact, with no rule to keep them in step |
| 3 | Claude | 5, 8 | The same route gap |
| 3 | Claude | 2, 10 | The same team-lead gap |

So `plan-14` carries three real gaps outside item 11's scope, which the case was not written to hold. They stay in the case, because changing it would void these runs; a later change to the evaluation set can close them.

M38's plan-reviewer counts were taken on `plan-01` to `plan-12`, before item 11 existed, and don't compare with these.

## Replaced or excluded runs

None. All 12 runs exited 0 and wrote a report, and each changed only its plan's `review:` line. `run.sh`'s own search flagged no run.

## The check for outside reads

The commands and tool inputs of each run were listed for absolute paths outside its temp folder. Codex tested for `/AGENTS.md` and `/tmp/AGENTS.md` in five runs; neither exists and nothing came back, so under M38's rule these aren't reads. No Claude run reached outside its temp folder, and no run used web search.

## Stray writes

Two Codex runs wrote outside their temp folder: round 1 on `plan-13` saved a copy of the plan as `/tmp/equipment-loans-plan-before-review.bin`, and round 3 on `plan-14` one as `/tmp/equipment-loans-plan-before-review.md`, each to check afterwards that its review hadn't changed the plan body. The building session deleted both after the runs. No Claude run wrote outside its temp folder.

## Cost and time

Tokens: uncached input (for Claude, input plus cache writes), cached reads, and output. Dollars are Claude Code's list-price figure, not a bill. Codex reports no cost.

| Client | Round | Runs | Time | Uncached in | Cached | Output | Cost |
|---|---|---|---|---|---|---|---|
| Claude | 1 | 2 | 265 s | 51,308 | 250,054 | 24,790 | $0.96 |
| Claude | 2 | 2 | 313 s | 55,321 | 274,870 | 29,553 | $1.09 |
| Claude | 3 | 2 | 279 s | 51,160 | 249,509 | 24,992 | $0.96 |
| Claude | all | 6 | 857 s | 157,789 | 774,433 | 79,335 | $3.00 |
| Codex | 1 | 2 | 223 s | 21,788 | 184,832 | 7,243 | not reported |
| Codex | 2 | 2 | 219 s | 17,661 | 190,080 | 6,353 | not reported |
| Codex | 3 | 2 | 314 s | 29,568 | 220,800 | 7,641 | not reported |
| Codex | all | 6 | 756 s | 69,017 | 595,712 | 21,237 | not reported |

The longest run took 184 seconds, against the limit of 1,800. The plan expected four runs; the two revisions made it twelve. Scoring came on top: six scorer runs of about 105,000 tokens each for the Sonnet subagent, and three Codex scorer runs.

## Limits

- One run per case, client and round. Rounds 1 and 2 differ by one line of item 11 that neither `plan-14` High touched, so the change between them is run-to-run variance as much as text. Round 3's pass is one sample.
- The pass rule counts item 11's Highs only. A reviewer that files a real gap under item 11 fails it, as round 2 showed, so the rule measures where item 11 draws its line as much as whether it accuses wrongly.
- Two cases of one domain, both SharePoint. Nothing here measures item 11 on Dataverse tables, on a plan with no lists, or on a plan written before the ledger.
- Whether `powerops-plan` writes a ledger like the cases' is not measured; the cases were written by hand in its layout.
- A temp folder is a mitigation, not proof that a reviewer was blind.
