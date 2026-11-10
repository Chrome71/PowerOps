# Decisions: m76-drift-names

## Round 0 (the 1.0 fix chain)

The person's instruction of 2026-11-09, quoted in the chain rules the session that runs the 1.0 fix chain wrote: "Once 75 is finished with its PR continue with the other M sessions in new sessions each until you wrap up M83 autonomously." Two of its three answers apply to this milestone:

### Q1. For M76 and M79–M83, who signs off each milestone's plan before it is built?
1. Sessions sign off (recommended): each session signs its own plan once the Fable plan review approves it and `powerops-review-plan` leaves no finding open, and records that it did so on the person's instruction of 2026-11-09
2. The person signs off each plan

Answer: 1, "Sessions sign off (Recommended)" (answered by: person, 2026-11-09).

### Q2. Who merges each milestone's PR once all 8 CI checks pass?
1. Sessions merge (recommended): each session rebases on main, reruns its checks, opens its PR and squash-merges it when green, one PR at a time
2. The person merges

Answer: 1, "Sessions merge (Recommended)" (answered by: person, 2026-11-09).

## Log

- 2026-11-09: the session that runs the 1.0 fix chain started this session for M76, in parallel with M79, M80, M81 and M82, on `main` `278b136` (M74, M77, M73, M78 and M75 merged). The fix plan's section 3 ("M76"), read with sections 4 and 5, is the base.
- read on 2026-11-09: the fix plan (sections 1 to 7) and its rounds 0 to 5 on A6 and F-7; the trial record's F40 row; `DriftCompare.cs`, `DriftParsers.cs`, `DriftQueries.cs`, `Snapshot.cs`, `SnapshotFile.cs`, `DriftLiveReader.cs`, `DriftCommand.cs`, `DriftReport.cs`, `DeployPlan.cs` (`AddUpgrade`, `Inputs`); the drift tests, captures, samples and `tests/fixtures/NOTICE.md`; `docs/COMMANDS.md` (`drift`, `deploy plan`), `docs/schemas/` (`drift`, `snapshot`, README) and `docs/examples/README.md` (the plan app's role)
- depth: quick: the fix plan gives the design
- areas not asked: users, automation, screens, solution and publisher, environments and ALM; the change is to this repo, and the plan answers each or marks it not applicable
- 2026-11-09: `powerops-researcher` (Opus, Learn and open sources, offline) on the fix plan's claims: `bot` and `botcomponent` columns and entity sets supported by their table references; `botcomponent.schemaname` uniqueness not stated; `botcomponent_connectionreference` is a table with entity set `botcomponent_connectionreferenceset`; settings are solution components, with no reference page for `appsetting` or `settingdefinition`; "CDS Bot ID" doesn't transfer on import (*Export and import agents using solutions*), agent components not covered; System Administrator and System Customizer read both agent tables organization-wide, Environment Maker its own; 5,000 rows a page. `solutioncomponentdefinition` and the per-environment numbers: no Learn page. The session then found them in Microsoft's open-source `microsoft/power-platform-skills` (MIT, `app-theme.js:210-219`, `app-theme-reads.js:64`, commit `da8ea7a`): "The appsetting solution component type differs per environment, so it is read", through `solutioncomponentdefinitions` with `solutioncomponenttype` and `primaryentityname`. The plan's Assumptions cite both.
- 2026-11-09: refinements to the fix plan's text, each kept to what it names:
  1. The `bot` read of drift is filtered by the solution link (as reads 4 to 6), not by M73's list of schema names: drift has no zip to take the names from. It adds `botid`, so an agent component can carry its schema name.
  2. The new reads are made only when needed: read 9 when the solution holds a type of 10000 or above that is not one of its connection references, reads 10 and 11 when it holds an agent or agent components. A solution without them costs what it costs today.
  3. `deploy plan`'s target read keeps today's reads, and the `components-not-checked` inventory (`DeployPlan.AddUpgrade`, lines 362-370 at `278b136`) is not changed. Reason: connection references are a type above 10000, so a definitions read there would make `deploy plan` need read on `solutioncomponentdefinition` for nearly every solution, which the read-only role tested live on 2026-11-04 (`docs/examples/README.md`) doesn't list and no source covers. The inventory is information only.
  4. Read state: `Snapshot.Agents` null means names and agents were not read; an empty list means they were read, or none was needed, and there is no agent. A failed read ends the run with exit `3` as today (the fix plan's "denied definitions read fails as today"), so no snapshot ever records a failed read.
  5. The fallback line in "not compared" appears per pair compared by number that keys a component of type 10000 or above after the left-out rules, so pairs where names change nothing keep today's output.
  6. `agent-published` is a pair difference (the published flag differs from the first environment), not a line for every published agent: an agent is usually published in Dev too.
  7. A `bot` component with a schema name is compared as an agent only, not again as a component; one without (not returned, as for another maker's agent under Environment Maker) is compared by id and counted in its own "not compared" line.
- 2026-11-09: first plan reviews, run in parallel on `d903298`. `powerops-review-plan` (a general-purpose subagent following the skill, Opus, fresh context): 0 High, 4 Medium, 5 Low (R1 to R9, in `review.md`). Fable plan review (`fable-reviewer`, Fable): REJECTED with seven findings (six Medium, one Low), overlapping R1 to R9; it accepted refinement 3 on condition that the PR body names the deviation. Answered in the revised plan:
  - read 9 only for a type of 10000 or above that is not a connection reference (Fable 1, R1); the capture companions and the `DriftCaptureTests` change are dropped;
  - "not compared" defined per pair, by name or by number; the fallback line only for a by-number pair that keys a type of 10000 or above after the left-out rules, so the existing 10087 tests stay; the family line says ids "can change" (Fable 2, R4, R7);
  - agents and agent components the read didn't return: compared by id and counted in a "not compared" line, not a refusal, because an Environment Maker legitimately sees only its own agents (Fable 3, R3);
  - every agent link table Learn lists (`bot_botcomponent` and `botcomponent_*`) left out by name and counted, with a `botcomponent_workflow` test (Fable 4, R2);
  - each step has its check and expected-red list, and the existing tests that change are named (Fable 5);
  - a test holds the schema's `kind` list equal to `DriftKind.Order`; a reader test for a type with no definition row; the version-2 duplicate `(name, schemaName)` rejection; a CLI test of a version-1 file against a version-2 file in both orders (Fable 6, R6);
  - schema README paragraphs, the `JsonOutput` comment, the snapshot schema's description, member names instead of line numbers (Fable 7, R8, R9);
  - R5 and Fable's note taken: `SnapshotFile.Write` keeps the version-1 text when no names were read, so a deploy target's digest never moves. Plan files made before this change therefore still match, and `deploy plan`'s plan hashes and goldens don't change. This supersedes the fix plan's risk line and the chain brief's "plans made before this change stop matching": `COMMANDS.md` and the PR body say that plan hashes are unchanged instead;
  - Fable's note on one flag taken: `ReadSolution`'s `otherSolutions` becomes `drift`; and its note on read 9 taken: a definition row with no name is skipped, not refused.
- the same reviewer's second pass 2026-11-09 on `80a1157`: R1 to R5 and R7 to R9 answered, R6 in part; two new: R10 (Medium: the version-2 duplicate rule on `name` and `schemaName` would reject two app settings) and R11 (Low: the version-1 text needs its own component shape; `SnapshotFileTests.cs:46` doesn't change). Answered in row 5: the duplicate rule is on row 6's comparison key, with a test that two `appsetting` components load; the version-1 component shape is named; the `:46` line corrected. Fable's two slips fixed: refinement 3's citation, and the bad-version test now listed under step 2 only
- plan review under the model routing, second pass 2026-11-09 on `80a1157`: APPROVED; step 2 to Opus, steps 3 and 4 to Sonnet, with WATCH notes for each step (kept with the build briefs)
