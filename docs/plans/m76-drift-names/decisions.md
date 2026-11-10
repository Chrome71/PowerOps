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
  2. The new reads are made only when needed: read 9 when the solution holds a type of 10000 or above, reads 10 and 11 when it holds an agent or agent components. A solution without them costs what it costs today.
  3. `deploy plan`'s target read keeps today's reads, and the `components-not-checked` inventory (`DeployPlan.cs:349-356`) is not changed. Reason: connection references are a type above 10000, so a definitions read there would make `deploy plan` need read on `solutioncomponentdefinition` for nearly every solution, which the read-only role tested live on 2026-11-04 (`docs/examples/README.md`) doesn't list and no source covers. The inventory is information only. The plan hash still changes, because the snapshot file's format moved.
  4. Read state: `Snapshot.Agents` null means names and agents were not read; an empty list means they were read and there is no agent. A failed read ends the run with exit `3` as today (the fix plan's "denied definitions read fails as today"), so no snapshot ever records a failed read.
  5. The fallback line in "not compared" appears only when a snapshot without names meets a component of type 10000 or above, so pairs where names change nothing keep today's output.
  6. `agent-published` is a pair difference (the published flag differs from the first environment), not a line for every published agent: an agent is usually published in Dev too.
  7. A `bot` component with a schema name is compared as an agent only, not again as a component; one without (not read, as for another maker's agent under Environment Maker) is compared by id.
