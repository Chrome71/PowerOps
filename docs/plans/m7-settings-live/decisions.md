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

### Q5. Live done-when, in the developer test environment before 2026-12-15, with the user running every live step?
1. Dry run writes nothing; `--apply` imports the seed exported with its flow on and reports it on (exit 0); an import whose flow stays off reports it (exit 1) (recommended)
2. The round-trip only; the flow-left-off case is covered offline with a fake pac
3. In your own words
Answer: Dry run writes nothing; `--apply` imports the seed exported with its flow on and reports it on (exit 0); an import whose flow stays off reports it (exit 1)

### Q6. Licensing and DLP: the seed flow uses the Dataverse connector, which is premium in Power Automate.
1. Assume the developer environment's plan covers it for the test; list licence and DLP under Questions for your admin; report a DLP-suspended flow as its own state (recommended)
2. Check the licence and DLP policy before the plan is approved
3. In your own words
Answer: Assume the developer environment's plan covers it for the test; list licence and DLP under Questions for your admin; report a DLP-suspended flow as its own state

## Log

- plan started 2026-10-20 from the master session's brief; the offline half is `docs/plans/m7-settings/`
- `powerops plan facts` not run: the brief forbids contacting the tenant while planning (it runs `pac org who`); `environment` stays null
- research re-verified on Microsoft Learn on 2026-10-20 (sources in plan.md); pac 2.13.1 help read locally, no tenant contacted
- auth was not asked as its own question: the pac-only route (Q1) means the pac profile is the only auth
- depth: quick (from `m7-settings`); two rounds were held because route, failure handling and the live done-when all blocked the plan
- approved without: users
- approved (2026-10-20)
