# M96 report: the rerun of both review roles

Evaluated revision: `b7f9f098676d13633475edbfb63a3262246893cd`, the freeze commit of step 2 after the Fable work review's one correction (the first freeze, `b3698eb`, carried an unplanted Verifier flaw in the new case and was never run). A squash merge leaves that commit off `main`, so the evaluated text is pinned by git blob id, on its LF text:

| File at `b7f9f09` | Blob id |
|---|---|
| `skills/powerops-verify/SKILL.md` | `601dcddecc02044f338f800bf2b8b0a08dce62d8` |
| `skills/powerops-review-security/SKILL.md` | `443cfdd8168f31daa1e8887d35ab1ee122314ab6` |
| `tests/eval/roles/run.sh` | `ed235cb46ec399ee082cc12373e5de70acc670f7` |

`run.sh` is unchanged since M83. The cases, the key and the baselines are as they stand at that commit: the new case `solution-09` and its flaw F47, and the baselines regenerated from the freeze build (the missing-package note, the `settings-hash:` line, M92's redacted `diff` value; M92's `--show-values` bridge removed).

A clean clone of that commit, at a path with no GUID in it, built with 0 warnings and passed `dotnet format --verify-no-changes`. `dotnet test` ran 5,562 tests: 5,553 passed, 6 skipped and 3 failed, the three the plan names: `PluginManifestTests.Plugin_skills_are_the_shipped_skills` for `claude` and for `codex`, and `RoleEvalTests.The_kept_roles_text_is_the_pinned_or_a_recorded_blob`, because the copies and the pins change only after the keep decision. (A clone under the session's scratch folder also fails `McpCommandTests.Inventory_replaces_the_ids_in_the_agent_zips_reference_names`, because that path holds a GUID; a clone of `a442b17` there fails the same way.) The stub check of step 3 (stub clients, no model, both clients and both passes on `solution-09`) passed every assertion, and the Fable work review passed the freeze before the first paid run.

All conclusions here are provisional, as in M38, M39, M55 and M83: one valid run per case, pass and family, and the numbers in the keep rules are judgement calls.

## Result

| Keep rule | Limit | Verifier | Security reviewer |
|---|---|---|---|
| Finds its own flaws marked `baseline: not-shown`, pooled | At least 15% | 12 of 12, 100% (Claude 6 of 6, Codex 6 of 6) | 16 of 16, 100% (Claude 8 of 8, Codex 8 of 8) |
| False Highs across its clean-case runs on solution-02 and solution-06, pooled | At most 1 | 0, from 5 Highs in 4 runs, as settled below | 0, from 2 Highs in 4 runs, as settled below |
| Pooled denominator | At least 5 | 12 | 16 |

Both roles are at or above both floors as settled. The false-High row rests on the settlement of seven splits between the scorers, the same two kinds M83 settled (see Clean-case Highs). Under the Opus scorer's reading, which judged all seven unsupported, the Verifier has 5 and the Security reviewer 2, and both would miss that floor.

Recorded beside the keep rules, as the plan defines them:

| Measure | Verifier | Security reviewer |
|---|---|---|
| F47 on solution-09 (the package not opened; its own flaw for the Security reviewer) | not keyed for it; 0 of 2 | Claude 1 of 1, Codex 1 of 1 |
| A security report that says it found no secret or host without naming the packages it didn't open (trial 3's C-7) | n/a | 0 of 18 |
| Its Highs on solution-09, which has no Verifier flaw | 3 in 2 runs: 2 supported (the package lacks the gallery screen), 1 split (unmanaged zip, settled supported) | 0 |
| Unchanged statements judged false | 2 (Claude, one run) | 0 |
| Unchanged statements unsupported on a strict reading | 3 | 1 |
| Reports whose eight header lines, `solution-hash:`, `deploy-hash:` and `settings-hash:` all check | 18 of 18 | 18 of 18 |
| Runs whose findings say the folder or the deployed package lacks the canvas `.msapp` (settled supported, unkeyed) | 10 (Claude 7, Codex 3) | 1 (Claude 1, Codex 0) |

Both F47 reports name `CanvasApps/pops_workitems_2af60_DocumentUri.msapp` in a note under the header lines, say it wasn't opened, and add on the SE:07 Coverage line that it is not verified for secrets and hosts; Codex's adds that a canvas package can hold an environment address for its Dataverse data source. Neither opened the package or named the host.

## Setup

| | Claude Code | Codex |
|---|---|---|
| Client version | 2.1.325 | codex-cli 0.167.0 |
| Model requested | `opus` | `gpt-6-astra` |
| Model the report gave | `claude-opus-5-5` in all 18 | `GPT-6` in 10, `unknown` in 8 |
| Effort requested | high | high |
| Runs | 2026-11-14 19:25 to 20:00 UTC | 2026-11-14 19:25 to 19:55 UTC |

Each client ran one run at a time, case by case, Verifier first, through `run.sh` from a clean clone of the freeze commit outside the repo (`~/.local/state/powerops-m96-eval/clone`), side by side, with its own `EVAL_OUT` and `TMPDIR` under that folder. Every `meta.txt` shows the freeze commit, `limit-seconds: 1800` and a `zip-digest:`. All 36 runs exited 0; none hit the limit, none was replaced. The marker `reports/trial-3/chain/m96-eval-started` was created before the first run.

## Results by family

A flaw counts once per case and run. An Opus subagent and Codex (`gpt-6-astra`) in a read-only sandbox, neither of which ran a review, scored 36 neutral copies on their own (Part A: the notes under the header lines, Coverage and Checks; Part B: the findings; new ids, no header lines, model and client names masked). F1 to F46 counted only from Part B, as in M83; F47 from either part. Neither scorer opened the mapping. They agreed on every pair of copy and keyed flaw.

| Role | Flaws | Claude | Codex | Pooled | M83, each client |
|---|---|---|---|---|---|
| Verifier | Own, `not-shown` | 6 of 6 | 6 of 6 | 12 of 12 | 6 of 6 |
| Verifier | Own, `shown` | 2 of 2 | 2 of 2 | 4 of 4 | 2 of 2 |
| Security reviewer | Own, `not-shown` (F47 included) | 8 of 8 | 8 of 8 | 16 of 16 | 7 of 7 |
| Security reviewer | Own, `shown` | 1 of 1 | 1 of 1 | 2 of 2 | 1 of 1 |

Not in the keep rules: the Verifier also found 8 of the Security reviewer's 9 flaws on each client (all but F47); the Security reviewer found all 8 of the Verifier's on Claude and 7 of 8 on Codex (missed F15 on solution-05), as in M83.

| State | Verifier Claude | Verifier Codex | Security Claude | Security Codex |
|---|---|---|---|---|
| `blocked` | 8 | 9 | 6 | 7 |
| `ready` | 1 | 0 | 3 | 2 |

## Clean-case Highs

Seven Highs on solution-02 and solution-06, each split between the scorers in the same way: the Codex scorer judged all seven supported, the Opus scorer all seven unsupported (none false).

- **"The deployed solution is unmanaged"** (Verifier: Claude solution-02, Codex solution-02 and solution-06; Security reviewer: Codex solution-06). `solution/current/Other/Solution.xml` says `Managed` 0, the request names `deploy/current.zip` as the zip that will be deployed and its folder as that zip's unpack, and the plan's route is managed in Test and Prod. True on the inputs the run was given; the eval's zip is a plain zip of an unmanaged folder. Settled supported, unkeyed, as M83 settled the same finding ("Deployment input is unmanaged"). The Opus scorer read the plan as the pipeline making the managed build.
- **"The settings are placeholders"** (Verifier: Codex solution-02 and solution-06; Security reviewer: Codex solution-06). The case's `settings.json` holds `fake-connection-id` values. Settled supported by M38's, M55's and M83's precedent. The Opus scorer read the file as the values the plan asks for.

On solution-09 both Verifier runs raised a High that the package holds no gallery screen for the new screen's Back button to return to. Both scorers judged it supported: the case's package holds only `Src/App.pa.yaml` and the new `Src/SummaryScreen.pa.yaml`, where a real app's package would hold every screen. It is an artifact of how the case was made, recorded under Limits; solution-09 is not one of the keep rule's clean cases. Codex's Verifier also raised the unmanaged High there, settled as above.

## Tenant claims

The Opus scorer listed 2, the Codex scorer 11, all from Claude's runs. Settled against "drawn from an export", as M55 and M83 did: 2 claims, listed by both scorers (solution-05, read access comes from roles outside the solution; solution-09, access for supervisors and staff is granted outside the solution), both from Claude's Security reviewer. The other 9 are forecasts from the export (the mail would go out on a maker's connection; supervisors will see the column locked), statements of the plan's own environments, or consequences of a keyed flaw, and were settled as not claims.

## Unchanged statements

Judged by an Opus subagent under M55's rule (`docs/plans/m55-role-freeze/plan.md`). Every run made the two-path `manifest` call.

| Client | Pass | False | Unsupported | Supported |
|---|---|---|---|---|
| Claude | Verifier | 2 | 2 | 10 |
| Claude | Security reviewer | 0 | 0 | 20 |
| Codex | Verifier | 0 | 1 | 10 |
| Codex | Security reviewer | 0 | 1 | 23 |

Both false statements are in Claude's Verifier run on solution-04: it said the `Entities/pops_WorkItem/` files and the roles are unchanged after citing the manifest, though `Other/Customizations.xml` changed (paragraph B's rule, which the roles' text didn't change); the run had read both versions and the components are in fact unchanged apart from the new connection reference. M83 also had 2. The four unsupported ones are true statements about a view's filter and sort, or a role, whose file the manifest listed as changed, each after reading both files.

## Cost and time

Dollars are Claude Code's list-price figure from each run's `total_cost_usd`, not a bill. Codex reports no cost.

| Client | Pass | Runs | Time | Uncached in | Cached | Output | Cost |
|---|---|---|---|---|---|---|---|
| Claude | Verifier | 9 | 846 s | 312,216 | 2,182,569 | 78,250 | $4.50 |
| Claude | Security reviewer | 9 | 1,234 s | 416,183 | 2,583,370 | 112,827 | $6.10 |
| Codex | Verifier | 9 | 738 s | 273,201 | 1,814,016 | 25,432 | not reported |
| Codex | Security reviewer | 9 | 1,050 s | 397,308 | 2,447,872 | 35,794 | not reported |

| | Planned | Actual |
|---|---|---|
| Claude, 18 runs | about $12 | $10.60 |
| Replacements | up to $2 | none |
| Opus scorer and the unchanged-statement judge | about $4 | 239,499 and 169,916 subagent tokens, no dollar figure (about $3 to $5 at Opus list prices) |
| Cap | $40 | not reached (about $14 to $16 in all) |
| Codex, runs and scorer | about 5.5M tokens | 5.5M in the runs; 118,928 for the scorer |
| Time | about an hour | 35 minutes of runs on each client |

## Not measured

- The `settings-hash: none` branch of both roles: `run.sh` always names `settings.json`.
- A zip inside a solution: no case holds one.
- A real Studio export's canvas package: the case's package is hand-made, deflated, with the fake host in `Properties.json` and `References/DataSources.json`.

## Limits

- The set scored at the ceiling again for both roles and both families, so this shows that the new text lost nothing these cases can see. F47 shows the new paragraph works on one hand-made package, on one run per family; neither run opened it, so whether a role that opens a package would report the host is not shown.
- The false-High floor of both roles rests on the settlement of seven splits above; under the Opus scorer's reading the Verifier has 5 and the Security reviewer 2.
- On solution-01 to solution-08, `analyze` now says the canvas package is not in the folder; 11 runs raised it as a finding (settled supported, unkeyed, before the run). A real managed export holds the package.
- solution-09's package holds only the new screen, so both Verifier runs raised a supported, unplanned High about the missing gallery screen.
- Every case has a previous solution and is an unpacked folder; no run took the branches for a zip or for no previous solution.
- A temp folder of its own is a mitigation, not proof that a reviewer was blind. Codex's sandbox restricts writes, not reads.
- Nothing ran on Windows.
