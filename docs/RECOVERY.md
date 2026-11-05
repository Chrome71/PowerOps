# Recovery after `deploy apply`

`powerops deploy apply --apply` imports a reviewed plan's solutions in plan order and stops at the first import that fails or times out. There is no rollback: an import that ran stays in the environment. Microsoft's safe-deployment guidance names three ways to recover from a bad deployment: roll back to the last known working configuration, roll forward with a fix, or deploy the last known working configuration again; and it warns that rolling back data and schema changes can be complex. For a solution import, PowerOps supports rolling forward: a new plan, reviewed and approved like the first.

This page says what to do after a run that stopped: read the report, check the environment's solution history, and repair forward. It ends with what no import brings back. Every write it names, in Dev or in the target, is a person's decision: an agent asks first, and never deletes in a tenant.

## First steps

1. Keep the report: the JSON of `--format json` (or the file of `--out`), the MCP tool's result, or the Action's job summary. It names the target, so keep it private.
2. Don't run `deploy apply --apply` again, with this plan or a new one, until you know where each solution of the report stands. A plan whose target changed is refused anyway, but the hash covers what the target shows, and can't tell you that an import is still running.
3. Read the report ([below](#read-the-report)), then the [solution history](#check-the-solution-history).

## Read the report

Start with the exit code, then the report's `status` and `message`, then each solution row's `import`. JSON follows [docs/schemas/deploy-apply.schema.json](schemas/deploy-apply.schema.json).

| Exit code | Report `status` | What happened | Next |
|---|---|---|---|
| `0` | `planned` or `imported` | A dry run that would import, or every import ran and each planned flow is in the state the plan expected | Nothing to recover. Confirm with `drift` ([HANDOFF step 9](HANDOFF.md#the-steps)) |
| `1` | `refused` or `imported` | `refused`: nothing was imported. The plan file is blocked or from another powerops version, the new plan's hash differs from the reviewed one (the `message` names the first input that changed), the plan is not the one the form approved, or the new plan is blocked. `imported`: every import ran, and a flow's `stateAfter` is not its `expectedState` | `refused`: [plan again](#forward-repair-by-a-new-plan). A flow: [HANDOFF step 10](HANDOFF.md#the-steps) |
| `2` | no report | A usage error, or `--target` names another environment than the plan's. Nothing was imported | Fix the command |
| `3` | no report, `failed` or `imported` | No report: the plan file, a zip or a settings file can't be read, or a read of the target failed, before any import. `failed`: the run stopped at a row that is `failed` or `unknown` (an import failed or timed out, `pac` was not found, or a file changed before its import). That may be the first row, so `failed` doesn't mean that anything was imported: the `message` says there is no rollback and names the solutions imported before the stop, or says that nothing was. `imported` with a `message` that starts "The imports succeeded.": every import ran, then reading the target again failed, so each flow's `stateAfter` is null | No report: fix the cause, then run the dry run (`deploy apply` without `--apply`) before the same plan again. `failed`: read the rows below. `imported`: check each listed flow's state in the target ([HANDOFF step 10](HANDOFF.md#the-steps)) |

| Row `import` | Meaning | Next |
|---|---|---|
| `ok` | Imported. It stays: there is no rollback | Find it in the solution history when a later row failed |
| `failed` | With a `pacExitCode`: `pac solution import` ran and exited with that code. `-1` means `pac` printed more than 16 MB and `deploy apply` stopped it. Its output went to stderr, each line prefixed `  \| ` (in the Action, only with debug logging on). With `pacExitCode` null: `pac` did not run for this row, because it was not found or the zip or settings file changed after the re-plan read it | With a code, `-1` included: `pac` may have started the import, so check the solution history first, as for `unknown`, then read `pac`'s output. Without one: fix the cause; nothing ran for this row |
| `unknown` | The import timed out after 60 minutes. `deploy apply` stopped `pac`, but the import may have finished or may still be running in the environment | [An import that timed out](#an-import-that-timed-out) |
| `not-run` | Nothing was imported for it: the run stopped before it, or was refused | Plan again once the rows before it are settled |
| `planned` | A dry run would import it | Nothing to recover |
| `skipped` | Its action is `skip`: the target has the same or a higher version. Nothing was imported | Nothing to recover |

## Check the solution history

PowerOps doesn't read the environment's import history. In Power Apps, open the target environment, select **Solutions**, select the solution, and select **See history** on the command bar. Each row is an import, export or uninstall with its start and end time, version, operation and result (Success or Failure); a failure has an error code and an exception message under **More details**. The system deletes rows older than 180 days, so read it soon and keep what you found with the report.

For each report row that is `ok`, `failed` or `unknown`, find the import of the zip's version that started at or after the report's `appliedAt`, and note its result. `appliedAt` is UTC, so convert it to the time zone the history shows.

Then read the target with PowerOps, from the same deploy folder. This reads the target only:

```sh
powerops deploy plan deploy --target <test>
```

A solution that imported shows `skip`, because the target has the zip's version, and one that didn't shows `install` or `upgrade`. If that disagrees with the history, stop and ask your admin before importing anything.

## An import that timed out

`deploy apply` waits 60 minutes for each import, then stops `pac` and marks the row `unknown`. The rows after it are `not-run`, and no flow is read. Stopping `pac` may not stop an import that the environment is running.

- Wait until the solution history shows that import with an end time and a result. Don't plan or import again before then: a new plan reads the target as it is at that moment.
- Success: treat the row as `ok`. Failure: treat it as `failed`, and read its error details.
- If the history shows no import of that version, ask your admin before importing again: PowerOps can't tell whether one is running.

## Partial success

When a run stops after one or more `ok` rows, those solutions are in the environment at their new versions, and the rest are at their old versions or absent. The plan imports each solution after the solutions in the folder that it depends on, so an imported solution doesn't need one that was not run. The other direction can break: a `not-run` solution that depends on an imported one may use a component that the imported upgrade deleted. Check the reviewed plan's `deletes` items for the imported solutions against the solutions that did not run. The environment's users meet that mix until a repair lands, so decide with the solution's owner how soon it must.

If **Solutions** shows a second solution named `<name>_Upgrade` beside the base solution, an upgrade was staged and not applied, or did not complete. While it is there, a new upgrade or patch of that solution fails ("Holding solution ... exists"), so a forward repair waits for it. **Apply Solution Upgrade** completes it and deletes the components that the new version no longer has; deleting the `_Upgrade` solution instead may lose data that came in through its new tables and relationships. Either is the person's decision, after reading the plan's `deletes` items. PowerOps has not been run beside such a solution: resolve it before you plan again.

## Forward repair by a new plan

1. Find the cause: the history's error details, `pac`'s output from the run, and `powerops analyze` on the zip.
2. Fix it where it belongs. A fault in the solution is fixed in Dev and exported again with a higher version, because an update needs a higher version than the installed one. A wrong connection id or value is fixed in the settings file. A problem in the target, such as a missing dependency, a licence or a data policy, is your admin's, and each change there is a write in that environment: ask first.
3. Plan again against the target from the folder with the new files: `powerops deploy plan deploy --target <test> --save deploy/powerops-plan.json`, or re-run the plan job in CI. Solutions that already imported show `skip` unless their zip has a higher version.
4. Review the new plan as you reviewed the first, and give a new yes by the same route: the MCP form, the Action's apply job, or your own `powerops deploy apply deploy/powerops-plan.json --target <test> --apply`.

An older zip is not a way back. `deploy plan` makes a zip whose version is the same as or lower than the target's a `skip`, as `pac solution import --skip-lower-version` does, so it imports nothing. To undo a change, undo it in Dev and ship that as a higher version.

## What no import brings back

- A managed upgrade deletes the components of the installed version that the new zip doesn't have. Learn says that a table or column deleted by hand loses its data, and that only a restore from before the delete recovers it. The pages read don't say this of a removal by upgrade, so treat a `deletes` item for a table as data loss. `deploy plan` warns with `deletes` items before the import, so read them before you approve ([deploy plan](COMMANDS.md#deploy-plan) says which component types it compares).
- Deleting a managed solution deletes all its components and their related data. Never uninstall a solution as a way back. PowerOps has no command that deletes one, and `powerops-implement` never deletes in a tenant.
- To recover the data of a deleted table or column, Learn names one way: restore the environment from a backup taken before the delete. That is your admin's decision, outside PowerOps. Backups are kept seven days by default, up to 28 for a production managed environment, so an older restore point may be gone. A restore overwrites the target environment; a backup restores only to a sandbox, developer or Teams environment, so a production environment is switched to sandbox first; afterwards the solution flows are off and connection references need new connections. PowerOps has no command for it, and an agent never starts one.

## By route

- **The Action.** A non-zero exit fails the apply job with one message per exit code, and the report is in the job summary. `pac`'s output reaches the log only with debug logging on (`RUNNER_DEBUG`). When nothing in the repository changed, re-run the pull request's last plan run, review the new comment, then re-run the apply ([When an apply refuses](examples/README.md#when-an-apply-refuses)). A fix to the solution goes through a new pull request and its plan.
- **The MCP form.** The tool returns the same report. If the client ended the call while an import ran, the import still runs to its end, and another `apply` call to that server is refused until then; run `deploy_apply` without `apply` to see where the target stands ([Timeouts](USE-WITH-AGENTS.md#mcp-server)). An agent reports the rows to the user and stops: the next import needs a new plan and the user's new answer in the form.
- **Your own command.** After a repair, run the dry run (`deploy apply` without `--apply`) first: it lists what would be imported, and refuses when the plan no longer matches the target.

What a client must provide for the form and for a long import is in [Agent clients](HARNESS.md).

## Sources

Read 2026-11-04:

- [Recommendations for safe deployment practices](https://learn.microsoft.com/power-platform/well-architected/operational-excellence/safe-deployments): the three ways to recover, and that rolling back data and schema changes can be complex.
- [View the history of a solution](https://learn.microsoft.com/power-apps/maker/data-platform/solution-history): **See history**, its fields, the error details and the 180 days.
- [Upgrade or update a solution](https://learn.microsoft.com/power-apps/maker/data-platform/update-solutions): an upgrade deletes components the new version doesn't have, the `_Upgrade` solution and **Apply Solution Upgrade**, an update needs a higher version, and deleting a managed solution deletes its data.
- [Solution upgrade fails due to a previously pending upgrade](https://learn.microsoft.com/troubleshoot/power-platform/dataverse/working-with-solutions/upgrade-fails-pending-upgrade): a pending `_Upgrade` solution blocks a new upgrade or patch, and deleting it may lose data.
- [Delete columns](https://learn.microsoft.com/power-apps/maker/data-platform/delete-fields) and [Delete a table](https://learn.microsoft.com/power-apps/maker/data-platform/create-edit-entities-solution-explorer#delete-a-table): the data of a column or table deleted by hand is lost, and only a restore from before the delete recovers it.
- [Back up and restore environments](https://learn.microsoft.com/power-platform/admin/backup-restore-environments): how long backups are kept, that a restore overwrites the target, the environment types it restores to, and the flows and connection references afterwards.
