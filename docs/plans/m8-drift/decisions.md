# Decisions: m8-drift

## Round 1

### Q1. Snapshot files and `--save`: what shape?
1. A powerops JSON snapshot with `schemaVersion` and its own schema; `--save <dir>` writes `<dir>/<solution>.<environment name>.json`; `--env` takes either an environment or a path to a snapshot file, so live and saved can be mixed in one run (recommended)
2. The same JSON file, passed with its own `--snapshot <file>` option instead of through `--env`
3. In your own words
Answer: A powerops JSON snapshot with `schemaVersion` and its own schema; `--save <dir>` writes `<dir>/<solution>.<environment name>.json`; `--env` takes either an environment or a path to a snapshot file, so live and saved can be mixed in one run

### Q2. Environment variable values: does `--show-values` exist at all?
1. No: values are never printed; snapshots store a SHA-256 of each value, so a difference shows as `differs` and snapshot files are safe to keep as CI artifacts; no `--show-values` option (recommended)
2. Keep `--show-values` as the BUILD-PLAN row says: snapshots hold the values, and output prints them only with the option
3. In your own words
Answer: No: values are never printed; snapshots store a SHA-256 of each value, so a difference shows as `differs` and snapshot files are safe to keep as CI artifacts; no `--show-values` option

### Q3. Managed and unmanaged: what counts as drift?
1. A managed/unmanaged difference is shown but is not drift (Dev unmanaged to Test managed is the normal route); unmanaged layers count only where the solution is managed; a version difference is drift, as the row says (recommended)
2. Any managed/unmanaged difference is drift (exit 1); the live "no drift" check would then need the seed managed in both environments
3. In your own words
Answer: A managed/unmanaged difference is shown but is not drift (Dev unmanaged to Test managed is the normal route); unmanaged layers count only where the solution is managed; a version difference is drift, as the row says

## Round 2

### Q4. Learn does not document whether `pac env fetch` can read `msdyn_componentlayer`, or how the unmanaged layer is named there. If the capture step shows it cannot be read, what then?
1. Mark layers "not read": the snapshot and the output say so for that environment, no layer drift is reported from it and the exit code is unaffected; the README says so; the live "edit the flow in Test" item is recorded as not supported in the PR; no SDK (recommended)
2. Also capture whether a component row's `ismanaged` or `overwritetime` changes after an edit in Test, and use that signal if it does (undocumented, observed only)
3. Pause M8 after the capture and take a new decision on reading layers through the Web API or the Dataverse SDK
4. In your own words
Answer: Mark layers "not read": the snapshot and the output say so for that environment, no layer drift is reported from it and the exit code is unaffected; the README says so; the live "edit the flow in Test" item is recorded as not supported in the PR; no SDK

## Log

- plan started 2026-10-21 from the master session's brief: the BUILD-PLAN Phase 3 section (M8 row, research basis, decisions 9 to 12, build-order tasks 22 to 24)
- `powerops plan facts` not run: the brief forbids contacting the tenant while planning (it runs `pac org who`); `environment` stays null
- depth: quick (from `m7-settings-live`, the newest plan)
- research re-verified on Microsoft Learn on 2026-10-21 by powerops-researcher (sonnet); local `pac` 2.13.1 usage text read, no tenant contacted
- round 2 held although depth is quick: the layer fallback blocked the plan
- approved without: users, environments and ALM, constraints (BUILD-PLAN decision 9 and the Phase 3 research basis cover them)
- approved (2026-10-21)
- fresh review (Sonnet) 2026-10-21: 0 High, 7 Medium, 9 Low; all sixteen fixed in the plan text (no user decision needed: each fix follows Q1 to Q4 or the BUILD-PLAN); review now stale
