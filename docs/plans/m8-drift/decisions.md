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

## Round 3

### Q5. Review R6: environment variable values often differ between Dev and Test on purpose. Should a differing value count as drift (exit 1)?
1. Drift, as the BUILD-PLAN row says: a differing value exits 1; the README says such a solution always exits 1 and JSON users can filter `items` by kind; no new option (recommended)
2. Information only: `variable-value` does not change the exit code; a missing variable is still drift
3. In your own words
Answer: Drift, as the BUILD-PLAN row says: a differing value exits 1; the README says such a solution always exits 1 and JSON users can filter `items` by kind; no new option

## Round 4

### Q6. Build review: Learn documents no output format for `pac env fetch`, so its text cannot be parsed against documented behaviour (go-back Assumption 1). How does `drift` read the solution's rows?
1. The Dataverse Web API: FetchXML in `?fetchXml=` with `HttpClient`, documented JSON with raw values, errors and paging; a token from `az account get-access-token --resource <environment URL>` (Azure CLI, MIT) as a subprocess; `pac org who` still resolves `--env` to the name and URL; no new package (recommended)
2. Keep `pac env fetch` and parse its text grid (undocumented format; values with line breaks, null columns and localized labels are at risk)
3. Stop M8 and revise the BUILD-PLAN row
4. In your own words
Answer: The Dataverse Web API: FetchXML in `?fetchXml=` with `HttpClient`, documented JSON with raw values, errors and paging; a token from `az account get-access-token --resource <environment URL>` (Azure CLI, MIT) as a subprocess; `pac org who` still resolves `--env` to the name and URL; no new package. Picked by the master session under the user's rule to rely only on documented and openly licensed sources (an undocumented `pac` mode was not considered), and accepted by the user at the re-sign-off on 2026-10-21.

## Log

- plan started 2026-10-21 from the master session's brief: the BUILD-PLAN Phase 3 section (M8 row, research basis, decisions 9 to 12, build-order tasks 22 to 24)
- `powerops plan facts` not run: the brief forbids contacting the tenant while planning (it runs `pac org who`); `environment` stays null
- depth: quick (from `m7-settings-live`, the newest plan)
- research re-verified on Microsoft Learn on 2026-10-21 by powerops-researcher (sonnet); local `pac` 2.13.1 usage text read, no tenant contacted
- round 2 held although depth is quick: the layer fallback blocked the plan
- approved without: users, environments and ALM, constraints (BUILD-PLAN decision 9 and the Phase 3 research basis cover them)
- approved (2026-10-21)
- fresh review (Sonnet) 2026-10-21: 0 High, 7 Medium, 9 Low; all sixteen fixed in the plan text (no user decision needed: each fix follows Q1 to Q4 or the BUILD-PLAN); review now stale
- second fresh review (Sonnet) 2026-10-21: 1 High, 4 Medium, 9 Low; all fourteen fixed in the plan text (no user decision needed); review now stale
- Q2 wording narrowed by the reviews: hashes hide values from a reader, not from guessing, so snapshot files belong in private folders and private CI artifacts; the README and schema do not call them safe
- third fresh review (Sonnet) 2026-10-21: 0 High, 4 Medium, 9 Low; round 3 asked the user R6 (Q5, kept as the row says); the other twelve fixed in the plan text; review now stale
- fourth fresh review (Sonnet) 2026-10-21: 0 High, 2 Medium, 8 Low; all ten fixed in the plan text (no user decision needed); review.md plan-hash updated to the final body with a note, as for the M7 plans
- build 2026-10-21: steps 2 (without parsers), 3 and 4 (command with the live read behind a seam) committed before Step 1; the capture had not run
- amendment 2026-10-21 (Q6): reads 2 to 7 move to the Dataverse Web API with an `az` token; `pac env fetch`'s text output rejected (no documented format); status back to approved for a fresh review and re-sign-off (section 10, small edit)
- Q6 research by powerops-researcher (Sonnet) on Microsoft Learn and open sources only, 2026-10-21; link alias naming and the error body re-checked on Learn the same day
- re-signed off by the user 2026-10-21 (committed by the master session); Q6 answer recorded; build resumed (status implementing)
- post-sign-off fix 2026-10-21 (master review, Medium): snapshots keep each connection reference's id (read 6 selects connectionreferenceid) and the comparison leaves reference components out by id; no componenttype number is assumed for connection references
- post-sign-off tightening 2026-10-21 (master review, Low, security): the token goes only to documented Dataverse hosts (*.dynamics.com, *.crm.microsoftdynamics.us, *.crm.appsplatform.us, *.crm.dynamics.cn; Learn "Datacenter regions" and "Dynamics 365 US Government", verified 2026-10-21); any other host exits 2 before a token is requested
- Step 1 capture 2026-10-22: component ids differ between Dev and Test for tables, canvas apps, environment variable definitions and connection references (only the flow kept its id); Assumption 2's fallback applied (those types left out of component-missing/extra); layer read built per component (the batched and nameless queries return no rows); a clean managed Test shows no Active layer (Assumption 3 holds)
- environments 2026-10-22 (the user said yes in the build session): a sandbox could not be created ("needs at least 1 GB of database capacity") and the developer environment limit (3 per user) was reached, so BUILD-PLAN decision 9 is replaced for M8: Dev is the existing unmanaged developer environment that already held the seed, without Managed Environments (its licensing is unclear and M8 to M10 do not need it); Test is a new trial environment (no capacity needed), created 2026-10-22, which expires 30 days later. Dev is suspended from 2026-12-15. All Phase 3 live checks must finish before both dates
- seed setup 2026-10-22: in Dev the made-up value (a space, a comma and a line break) was set and the flow turned on through the Web API; the seed was exported managed from Dev and imported into Test with `settings apply --apply` and a settings file outside the repo (flow on); a Dataverse connection was created in Test without a sign-in prompt
- live checks 2026-10-22: item 1 no drift (exit 0, the managed difference as information), item 2 the saved pair offline gave the same text and exit code (only `source` differs in JSON), item 3 an edit of the flow description in Test gave `unmanaged-layer` on the flow (exit 1, the flow stayed on); `pac solution import --force-overwrite` did not remove the Active layer, the documented `RemoveActiveCustomizations` function did; a last run gave no drift
