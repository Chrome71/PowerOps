# Decisions: m7-settings-live

## Round 1

### Q1. Route for the live step? pac has no command to bind a connection reference or turn a cloud flow on, but `pac solution import --settings-file` binds the references and restores flows to their export state.
1. pac only: import with `--settings-file`, then read flow state with a read-only pac command and report any flow left off; auth is the pac profile (recommended)
2. pac for the import plus the Dataverse Web API to bind references and turn flows on, with a second sign-in (MSAL, a new dependency)
3. In your own words
Answer: pac only: import with `--settings-file`, then read flow state with a read-only pac command and report any flow left off; auth is the pac profile

### Q2. Command shape and dry run?
1. `powerops settings apply <zip> --settings <file>` is a dry run by default (validates, shows the target environment and the exact pac command, writes nothing); only `--apply` imports; it refuses when validate finds problems (recommended)
2. `--apply` on `settings validate` itself
3. In your own words
Answer: `powerops settings apply <zip> --settings <file>` is a dry run by default (validates, shows the target environment and the exact pac command, writes nothing); only `--apply` imports; it refuses when validate finds problems

### Q3. Failure and partial apply?
1. Stop at the first failed step, no rollback; the report lists each step and each flow's state; exit 0 all flows on, 1 a flow left off, 3 pac or import failed; rerunning is the recovery (recommended)
2. Try to restore the previous state when the flow check fails
3. In your own words
Answer: Stop at the first failed step, no rollback; the report lists each step and each flow's state; exit 0 all flows on, 1 a flow left off, 3 pac or import failed; rerunning is the recovery

## Round 2

### Q4. How does powerops read each flow's state after import?
1. `pac power-automate list-cloud-flows --json`, matched by workflow id; Preview with undocumented field names, so the build starts with one captured, scrubbed sample (recommended)
2. `pac env fetch` with FetchXML on the workflow table, parsing pac's table text
3. In your own words
Answer: `pac power-automate list-cloud-flows --json`, matched by workflow id; Preview with undocumented field names, so the build starts with one captured, scrubbed sample

### Q5. Live done-when, in the developer test environment, with the user running every live step?
1. Dry run writes nothing; `--apply` imports the seed exported with its flow on and reports it on (exit 0); an import whose flow stays off reports it (exit 1) (recommended)
2. The round-trip only; the flow-left-off case is covered offline with a fake pac
3. In your own words
Answer: Dry run writes nothing; `--apply` imports the seed exported with its flow on and reports it on (exit 0); an import whose flow stays off reports it (exit 1)

### Q6. Licensing and DLP: the seed flow uses the Dataverse connector, which is premium in Power Automate.
1. Assume the developer environment's plan covers it for the test; list licence and DLP under Questions for your admin; report a DLP-suspended flow as its own state (recommended)
2. Check the licence and DLP policy before the plan is approved
3. In your own words
Answer: Assume the developer environment's plan covers it for the test; list licence and DLP under Questions for your admin; report a DLP-suspended flow as its own state

## Round 3

### Q7. Review R2: the dry run and a later `--apply` both read the pac profile's current environment. How is `--apply` tied to the environment the user approved?
1. `--apply` needs `--target <environment name>` equal to the name from `pac org who` (else exit 2), and powerops passes that environment's id to every later pac call; the id is read at run time, never printed or stored (recommended)
2. `--apply` prints the environment name first, and the README tells agents to run it only after a yes; no new option
3. In your own words
Answer: `--apply` needs `--target <environment name>` equal to the name from `pac org who` (else exit 2), and powerops passes that environment's id to every later pac call; the id is read at run time, never printed or stored

### Q8. Review R1 and R3: an import over an existing flow keeps its current state. How do the live runs start?
1. The user deletes the seed before each clean import; one extra check re-imports over the installed seed and expects the state unchanged; the dry run reads existing flows and says new or existing (recommended)
2. Use a second developer environment that never had the seed
3. In your own words
Answer: The user deletes the seed before each clean import; one extra check re-imports over the installed seed and expects the state unchanged; the dry run reads existing flows and says new or existing

### Q9. Review R4: pac's error text can hold URLs, emails or names. What goes in the report when the import fails?
1. The report holds pac's exit code and a fixed hint; pac's own output goes unchanged to powerops' stderr; live results in the PR are written by hand (recommended)
2. pac's last lines in the report with URLs, GUIDs and emails masked
3. In your own words
Answer: The report holds pac's exit code and a fixed hint; pac's own output goes unchanged to powerops' stderr; live results in the PR are written by hand

## Log

- plan started 2026-10-20 from the master session's brief; the offline half is `docs/plans/m7-settings/`
- `powerops plan facts` not run: the brief forbids contacting the tenant while planning (it runs `pac org who`); `environment` stays null
- research re-verified on Microsoft Learn on 2026-10-20 (sources in plan.md); pac 2.13.1 help read locally, no tenant contacted
- auth was not asked as its own question: the pac-only route (Q1) means the pac profile is the only auth
- depth: quick (from `m7-settings`); two rounds were held because route, failure handling and the live done-when all blocked the plan
- approved without: users
- approved (2026-10-20)
- fresh review (Sonnet) 2026-10-20: 2 High, 6 Medium, 3 Low; round 3 asked the user R2, R1/R3 and R4; plan revised for all eleven findings; review now stale
- found while fixing R1: deleting an unmanaged solution leaves its components (Learn), so the live reset deletes the components too; the live zips are unmanaged because a managed export cannot be imported into the environment holding the unmanaged original
- second fresh review (Sonnet) 2026-10-20: 0 High, 5 Medium, 7 Low; all twelve fixed in the plan text (no user decision needed: each fix follows Q1 to Q9); review now stale
- third fresh review (Sonnet) 2026-10-20: 0 High, 3 Medium, 5 Low; all eight fixed in the plan text (no user decision needed); review.md plan-hash updated to the final body with a note, as for the offline M7 plan
- 2026-11-01: personal and tenant facts removed from the plan body, with no decision changed; re-signed with `powerops plan sign-off`, and `review.md`'s `plan-hash` moved to the new body
